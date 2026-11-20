#!/usr/bin/env python3
"""Exercise a built desktop Player with isolated ROS TCP fixtures and real rendering.

Requires the ROS 2 Python message packages and an X11/Xwayland desktop. Nothing
is forwarded to ROS: this process is the Player's loopback-only TCP endpoint.
"""
import argparse
import ctypes as C
import ctypes.util
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import select
import socket
import struct
import subprocess
import threading
import time


class X11:
    """Address only the window belonging to the Player started by this test."""

    def __init__(self):
        self.x = C.CDLL(ctypes.util.find_library("X11"))
        signatures = {
            "XOpenDisplay": ([C.c_char_p], C.c_void_p),
            "XDefaultRootWindow": ([C.c_void_p], C.c_ulong),
            "XInternAtom": ([C.c_void_p, C.c_char_p, C.c_int], C.c_ulong),
            "XQueryTree": ([C.c_void_p, C.c_ulong, C.POINTER(C.c_ulong),
                            C.POINTER(C.c_ulong), C.POINTER(C.POINTER(C.c_ulong)),
                            C.POINTER(C.c_uint)], C.c_int),
            "XGetWindowProperty": ([C.c_void_p, C.c_ulong, C.c_ulong, C.c_long,
                                    C.c_long, C.c_int, C.c_ulong, C.POINTER(C.c_ulong),
                                    C.POINTER(C.c_int), C.POINTER(C.c_ulong),
                                    C.POINTER(C.c_ulong), C.POINTER(C.c_void_p)], C.c_int),
            "XFree": ([C.c_void_p], C.c_int),
            "XSendEvent": ([C.c_void_p, C.c_ulong, C.c_int, C.c_long, C.c_void_p], C.c_int),
            "XFlush": ([C.c_void_p], C.c_int),
            "XCloseDisplay": ([C.c_void_p], C.c_int),
        }
        for name, (args, result) in signatures.items():
            function = getattr(self.x, name)
            function.argtypes, function.restype = args, result
        self.display = self.x.XOpenDisplay(None)
        if not self.display:
            raise RuntimeError("An accessible X11/Xwayland DISPLAY is required.")
        self.root = self.x.XDefaultRootWindow(self.display)

    def atom(self, name):
        return self.x.XInternAtom(self.display, name.encode(), 0)

    def window(self, pid):
        def visit(window):
            actual, fmt, count, remaining, data = (C.c_ulong(), C.c_int(),
                C.c_ulong(), C.c_ulong(), C.c_void_p())
            result = self.x.XGetWindowProperty(self.display, window,
                self.atom("_NET_WM_PID"), 0, 1, 0, 6, C.byref(actual), C.byref(fmt),
                C.byref(count), C.byref(remaining), C.byref(data))
            matches = False
            if result == 0 and data:
                if fmt.value == 32 and count.value == 1:
                    matches = C.cast(data, C.POINTER(C.c_ulong))[0] == pid
                self.x.XFree(data)
            if matches:
                return window
            root, parent, children, number = (C.c_ulong(), C.c_ulong(),
                C.POINTER(C.c_ulong)(), C.c_uint())
            if not self.x.XQueryTree(self.display, window, C.byref(root),
                    C.byref(parent), C.byref(children), C.byref(number)):
                return None
            windows = list(children[:number.value])
            if children:
                self.x.XFree(children)
            for child in windows:
                found = visit(child)
                if found:
                    return found
            return None
        return visit(self.root)

    def close_window(self, window):
        class Data(C.Union):
            _fields_ = [("b", C.c_char * 20), ("s", C.c_short * 10), ("l", C.c_long * 5)]

        class ClientMessage(C.Structure):
            _fields_ = [("type", C.c_int), ("serial", C.c_ulong), ("send_event", C.c_int),
                ("display", C.c_void_p), ("window", C.c_ulong),
                ("message_type", C.c_ulong), ("format", C.c_int), ("data", Data)]

        class Event(C.Union):
            _fields_ = [("client", ClientMessage), ("pad", C.c_long * 24)]

        event = Event()
        event.client.type, event.client.send_event = 33, 1
        event.client.display, event.client.window = self.display, window
        event.client.message_type, event.client.format = self.atom("WM_PROTOCOLS"), 32
        event.client.data.l[0] = self.atom("WM_DELETE_WINDOW")
        self.x.XSendEvent(self.display, window, 0, 0, C.byref(event))
        self.x.XFlush(self.display)

    def close(self):
        self.x.XCloseDisplay(self.display)


class Endpoint:
    def __init__(self):
        from rclpy.serialization import serialize_message
        from geometry_msgs.msg import PoseStamped
        from sensor_msgs.msg import JointState
        self.serialize = serialize_message
        self.pose_type, self.joint_type = PoseStamped, JointState
        self.server = socket.socket()
        self.server.bind(("127.0.0.1", 0))
        self.server.listen(1)
        self.server.settimeout(0.2)
        self.port = self.server.getsockname()[1]
        self.stop = threading.Event()
        self.phase = 0
        self.sent = [0, 0]
        self.subscriptions = set()
        self.message_types = {}
        self.error = None
        self.thread = threading.Thread(target=self.run, daemon=True)

    @staticmethod
    def packet(topic, payload):
        name = topic.encode()
        return struct.pack("<I", len(name)) + name + struct.pack("<I", len(payload)) + payload

    def run(self):
        client = None
        try:
            while not self.stop.is_set():
                try:
                    client, _ = self.server.accept()
                    break
                except socket.timeout:
                    pass
            if client is None:
                return
            client.settimeout(2)
            handshake = {"version": "v0.7.0", "metadata": json.dumps({"protocol": "ROS2"})}
            client.sendall(self.packet("__handshake", json.dumps(handshake).encode()))
            buffer = b""
            while not self.stop.is_set():
                if select.select([client], [], [], 0.1)[0]:
                    chunk = client.recv(65536)
                    if not chunk:
                        break
                    buffer += chunk
                    while len(buffer) >= 4:
                        n = struct.unpack_from("<I", buffer)[0]
                        if len(buffer) < n + 8:
                            break
                        length = struct.unpack_from("<I", buffer, n + 4)[0]
                        if len(buffer) < n + 8 + length:
                            break
                        topic = buffer[4:n + 4].decode().rstrip("\0")
                        payload = buffer[n + 8:n + 8 + length]
                        buffer = buffer[n + 8 + length:]
                        if topic == "__subscribe":
                            request = json.loads(payload.rstrip(b"\0"))
                            self.subscriptions.add(request["topic"])
                            self.message_types[request["topic"]] = request["message_name"]
                phase = self.phase
                pose = self.pose_type()
                pose.header.frame_id = "world"
                pose.pose.position.x, pose.pose.position.z = 58.0 + 2 * phase, 0.9
                pose.pose.orientation.w = 1.0
                joints = self.joint_type()
                joints.name = ["swing_joint", "boom_joint", "arm_joint", "bucket_joint"]
                joints.position = ([0.12, -0.3, 1.2, -0.2] if phase == 0
                                   else [-0.12, -0.45, 1.5, 0.2])
                for topic, message in (("/zx200/base_link/pose", pose),
                                       ("/zx200/joint_state", joints)):
                    if topic in self.subscriptions:
                        expected = ("geometry_msgs/PoseStamped" if topic.endswith("/pose")
                                    else "sensor_msgs/JointState")
                        if self.message_types[topic] != expected:
                            raise RuntimeError("Unexpected ROS message type for " + topic)
                        client.sendall(self.packet(topic, self.serialize(message)))
                self.sent[phase] += 1
        except (BrokenPipeError, ConnectionResetError):
            pass  # Normal when the Player closes its connection on exit.
        except Exception as error:
            self.error = repr(error)
        finally:
            if client:
                client.close()

    def close(self):
        self.stop.set()
        self.thread.join(timeout=3)
        self.server.close()


def samples(path):
    result = []
    if path.exists():
        for line in path.read_text(errors="replace").splitlines():
            if line.startswith("[OperaSimVR Ubuntu runtime] "):
                result.append(json.loads(line.split("] ", 1)[1]))
    return result


def angle_delta(a, b):
    return (b - a + 180) % 360 - 180


def image_status(path):
    from PIL import Image
    with Image.open(path) as image:
        image.load()
        colors = len(set(image.convert("RGB").resize((32, 18)).getdata()))
        return {"width": image.width, "height": image.height,
                "sampled_colors": colors,
                "nonuniform_render": image.width >= 640 and image.height >= 360 and colors > 16}


def classify_exceptions(text):
    blocks = []
    current = None
    for line in text.splitlines():
        if "Exception:" in line:
            current = [line]
            blocks.append(current)
        elif current is not None and (not line or line[0].isspace()):
            current.append(line)
        else:
            current = None
    known, unexpected = [], []
    for block in blocks:
        # Require the first frame of this exact exception, rather than looking
        # for LiDAR anywhere in the log or in an adjacent exception's stack.
        is_lidar = (block[0].startswith("NullReferenceException:") and len(block) > 1
                    and block[1].lstrip().startswith("at UnitySensors.Sensor.LiDAR.LiDARSensor`1[T].Init"))
        (known if is_lidar else unexpected).append("\n".join(block).rstrip())
    return known, unexpected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("Logs/ubuntu-player-verification"))
    parser.add_argument("--timeout", type=float, default=420,
                        help="Maximum wall seconds, including slow emulated startup (default: 420).")
    parser.add_argument("--software-rendering", action="store_true")
    parser.add_argument("--allow-known-lidar-error", action="store_true",
                        help="Record the original unconfigured LiDAR error without failing this smoke test.")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    log, screenshot = output / "player.log", output / "scene.png"
    # Each run must be assessed from fresh evidence.
    if any((output / name).exists() for name in ("player.log", "scene.png", "result.json")):
        parser.error("Choose a new --output directory; existing verification evidence is preserved.")
    x11, endpoint, process = None, None, None
    began = time.monotonic()
    report = {"functional_checks_passed": False, "runtime_clean": False,
              "host_architecture": os.uname().machine, "headset_tested": False,
              "started_utc": datetime.now(timezone.utc).isoformat(),
              "verifier_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              "known_lidar_error_allowed": args.allow_known_lidar_error}
    try:
        x11 = X11()
        endpoint = Endpoint()
        endpoint.thread.start()
        env = {**os.environ, "OPERASIMVR_XR": "desktop", "OPERASIMVR_MODE": "viewer",
               "OPERASIMVR_ROS_IP": "127.0.0.1", "OPERASIMVR_ROS_PORT": str(endpoint.port),
               "OPERASIMVR_DIAGNOSTICS": "1", "OPERASIMVR_SCREENSHOT": str(screenshot)}
        command = [str(root / "operasimvr-player-ubuntu")]
        if args.software_rendering:
            command.append("--software-rendering")
        command += ["--", "-screen-width", "960", "-screen-height", "540",
                    "-screen-fullscreen", "0", "-logFile", str(log)]
        with (output / "launcher.log").open("w") as launcher:
            process = subprocess.Popen(command, cwd=root, env=env, stdout=launcher, stderr=subprocess.STDOUT)
            baseline, matched = None, None
            while time.monotonic() - began < args.timeout:
                if process.poll() is not None:
                    raise RuntimeError("Player exited before verification: " + str(process.returncode))
                if endpoint.error:
                    raise RuntimeError("Loopback fixture failed: " + endpoint.error)
                observed = samples(log)
                if baseline is None and len(observed) >= 3:
                    baseline = observed[-1]
                    endpoint.phase = 1
                    print("First pose/joint state observed; sending the second state.", flush=True)
                if baseline is not None:
                    for item in observed:
                        if item["frame"] <= baseline["frame"]:
                            continue
                        position_delta = item["zx200Position"]["x"] - baseline["zx200Position"]["x"]
                        actual = [angle_delta(baseline[field][axis], item[field][axis])
                                  for field, axis in (("swingEuler", "y"), ("boomEuler", "x"),
                                                      ("armEuler", "x"), ("bucketEuler", "x"))]
                        expected = [math.degrees(v) for v in (0.24, -0.15, 0.3, 0.4)]
                        if (abs(position_delta - 2) < 0.1 and
                                all(abs(a - b) < 0.5 for a, b in zip(actual, expected))):
                            matched = item
                            report.update(position_delta_x=position_delta, joint_delta_degrees=actual)
                            break
                if matched is not None and screenshot.exists() and screenshot.stat().st_size > 1000:
                    window = x11.window(process.pid)
                    if window:
                        print("ROS pose and all four joints followed both states; closing the Player window.", flush=True)
                        x11.close_window(window)
                        process.wait(timeout=45)
                        report.update(player_exit_code=process.returncode,
                            first_state=baseline, second_state=matched,
                            frame_samples=[item["frame"] for item in observed],
                            functional_checks_passed=(process.returncode == 0 and
                                all(item["mode"] == "PlayMode" for item in observed)))
                        break
                time.sleep(1)
            else:
                raise TimeoutError("Rendering/ROS verification did not finish within the wall timeout.")
        text = log.read_text(errors="replace")
        known, unexpected = classify_exceptions(text)
        rendering = image_status(screenshot)
        report.update(known_scene_errors=known, unexpected_exceptions=unexpected,
                      runtime_clean=not known and not unexpected,
                      compute_shaders="Compute shaders: True" in text,
                      screenshot_validation=rendering,
                      subscriptions=sorted(endpoint.subscriptions),
                      message_types=endpoint.message_types,
                      fixture_cycles_per_state=endpoint.sent,
                      screenshot=str(screenshot),
                      passed=(report["functional_checks_passed"] and
                              "Compute shaders: True" in text and not unexpected and
                              rendering["nonuniform_render"] and
                              (not known or args.allow_known_lidar_error)))
    except Exception as error:
        report.update(passed=False, failure=repr(error))
    finally:
        if process and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        if endpoint:
            endpoint.close()
        if x11:
            x11.close()
        report["wall_seconds"] = round(time.monotonic() - began, 2)
        (output / "result.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 0 if report.get("passed") else 1


if __name__ == "__main__":
    raise SystemExit(main())
