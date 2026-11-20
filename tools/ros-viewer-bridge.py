#!/usr/bin/env python3
"""Forward actual ZX200 ROS 2 state to a desktop viewer, without ROS publications.

ROS TCP Endpoint 10000 remains dedicated to OperaSimPhysX. This separate,
loopback-only observation endpoint forwards unchanged received CDR bytes.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import select
import signal
import socket
import struct
import threading
import time

import rclpy
from rclpy.node import Node
from rclpy.qos import qos_profile_sensor_data
from rclpy.serialization import deserialize_message
from geometry_msgs.msg import PoseStamped
from sensor_msgs.msg import JointState
from rosgraph_msgs.msg import Clock


TOPICS = {'/zx200/base_link/pose': (PoseStamped, 'geometry_msgs/PoseStamped'),
          '/zx200/joint_state': (JointState, 'sensor_msgs/JointState')}


def packet(topic, payload):
    name = topic.encode()
    return struct.pack('<I', len(name)) + name + struct.pack('<I', len(payload)) + payload


class ViewerBridge(Node):
    def __init__(self, port, output):
        super().__init__('operasimvr_state_observer')
        self.output = output
        self.latest = {}
        self.source_publishers = {topic: [] for topic in TOPICS}
        self.counts = {topic: 0 for topic in TOPICS}
        self.forwarded = {topic: 0 for topic in TOPICS}
        self.last_logged = {topic: 0 for topic in TOPICS}
        self.lock = threading.Lock()
        self.stopping = threading.Event()
        self.client_connected = False
        self.ignored_player_topics = {}
        self.clock = None
        self.error = None
        self.subscriptions_kept = []
        self.source_log = (output / 'source.jsonl').open('a', buffering=1)
        self.wire_log = (output / 'forwarded.jsonl').open('a', buffering=1)
        for topic, (message_type, _) in TOPICS.items():
            self.subscriptions_kept.append(self.create_subscription(
                message_type, topic, self.callback_for(topic), qos_profile_sensor_data, raw=True))
        self.subscriptions_kept.append(self.create_subscription(Clock, '/clock',
            self.receive_clock, qos_profile_sensor_data))
        self.server = socket.socket()
        self.server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server.bind(('127.0.0.1', port))
        self.server.listen(1)
        self.server.settimeout(.2)
        self.port = self.server.getsockname()[1]
        self.thread = threading.Thread(target=self.serve, daemon=True)
        self.thread.start()
        self.create_timer(1.0, self.write_status)
        self.get_logger().info('Actual ROS state -> 127.0.0.1:%d; ROS publishing disabled' % self.port)

    def callback_for(self, topic):
        # This installed Humble executor passes only the serialized message.
        def receive(data):
            self.receive(topic, data)
        return receive

    def receive_clock(self, message):
        self.clock = {'sec': message.clock.sec, 'nanosec': message.clock.nanosec}

    def receive(self, topic, data):
        message = deserialize_message(data, TOPICS[topic][0])
        now = time.monotonic()
        stamp = message.header.stamp
        value = {'utc': datetime.now(timezone.utc).isoformat(),
                 'stamp': {'sec': stamp.sec, 'nanosec': stamp.nanosec},
                 'frame_id': message.header.frame_id,
                 'observed_publisher_gids': [item['gid'] for item in self.source_publishers[topic]],
                 'cdr_sha256': hashlib.sha256(data).hexdigest()}
        if topic.endswith('/pose'):
            p, q = message.pose.position, message.pose.orientation
            value.update(position={'x': p.x, 'y': p.y, 'z': p.z},
                         orientation={'x': q.x, 'y': q.y, 'z': q.z, 'w': q.w})
        else:
            value.update(names=list(message.name), positions=list(message.position),
                         velocities=list(message.velocity))
        with self.lock:
            self.counts[topic] += 1
            value['sequence'] = self.counts[topic]
            self.latest[topic] = (now, bytes(data), value)
        if now - self.last_logged[topic] >= .2:
            self.last_logged[topic] = now
            self.source_log.write(json.dumps({'topic': topic, **value}) + '\n')

    def write_status(self):
        for topic in TOPICS:
            self.source_publishers[topic] = [
                {'gid': bytes(info.endpoint_gid).hex(), 'node_name': info.node_name,
                 'node_namespace': info.node_namespace, 'topic_type': info.topic_type}
                for info in self.get_publishers_info_by_topic(topic)]
        with self.lock:
            latest = {topic: {**entry[2], 'age_seconds': time.monotonic() - entry[0]}
                      for topic, entry in self.latest.items()}
            status = {'pid': os.getpid(), 'ros_domain_id': os.environ.get('ROS_DOMAIN_ID', '0'),
                      'tcp_endpoint': '127.0.0.1:%d' % self.port,
                      'ros_state_or_command_publications': 0, 'synthetic_states': 0,
                      'client_connected': self.client_connected, 'received': dict(self.counts),
                      'forwarded': dict(self.forwarded), 'latest': latest,
                      'ignored_player_topics': dict(self.ignored_player_topics),
                      'source_clock': self.clock, 'error': self.error}
            status['publishers'] = self.source_publishers
        temporary = self.output / 'status.json.tmp'
        temporary.write_text(json.dumps(status, indent=2) + '\n')
        temporary.replace(self.output / 'status.json')

    def serve(self):
        try:
            while not self.stopping.is_set():
                try:
                    client, _ = self.server.accept()
                except socket.timeout:
                    continue
                with client:
                    self.client_connected = True
                    try:
                        self.serve_client(client)
                    except (BrokenPipeError, ConnectionResetError, socket.timeout):
                        pass
                    finally:
                        self.client_connected = False
        except Exception as error:
            if not self.stopping.is_set():
                self.error = repr(error)

    def serve_client(self, client):
        client.settimeout(2)
        handshake = {'version': 'v0.7.0', 'metadata': json.dumps({'protocol': 'ROS2'})}
        client.sendall(packet('__handshake', json.dumps(handshake).encode()))
        subscribed, sent = set(), {}
        buffer = b''
        while not self.stopping.is_set():
            if select.select([client], [], [], .1)[0]:
                chunk = client.recv(65536)
                if not chunk:
                    return
                buffer += chunk
                while len(buffer) >= 4:
                    n = struct.unpack_from('<I', buffer)[0]
                    if n > 65536:
                        raise ValueError('Oversized ROS TCP topic')
                    if len(buffer) < n + 8:
                        break
                    size = struct.unpack_from('<I', buffer, n + 4)[0]
                    if size > 16 * 1024 * 1024:
                        raise ValueError('Oversized ROS TCP payload')
                    if len(buffer) < n + 8 + size:
                        break
                    topic = buffer[4:n+4].decode().rstrip('\0')
                    payload = buffer[n+8:n+8+size]
                    buffer = buffer[n+8+size:]
                    if topic == '__subscribe':
                        request = json.loads(payload.rstrip(b'\0'))
                        name = request['topic']
                        if name in TOPICS and request['message_name'] == TOPICS[name][1]:
                            subscribed.add(name)
                    elif topic == '__topic_list':
                        response = {'topics': list(TOPICS), 'types': [item[1] for item in TOPICS.values()]}
                        client.sendall(packet('__topic_list', json.dumps(response).encode()))
                    elif not topic.startswith('__'):
                        # Deliberately discard Player publications. This node owns no ROS publisher.
                        with self.lock:
                            self.ignored_player_topics[topic] = self.ignored_player_topics.get(topic, 0) + 1
            with self.lock:
                current = {topic: self.latest.get(topic) for topic in subscribed}
            for topic, entry in current.items():
                if entry is None:
                    continue
                received_at, cdr, value = entry
                sequence = value['sequence']
                if sent.get(topic) == sequence or time.monotonic() - received_at > 2:
                    continue
                client.sendall(packet(topic, cdr))
                sent[topic] = sequence
                with self.lock:
                    self.forwarded[topic] += 1
                self.wire_log.write(json.dumps({'topic': topic, **value,
                    'forwarded_utc': datetime.now(timezone.utc).isoformat()}) + '\n')

    def close(self):
        self.stopping.set()
        self.thread.join(timeout=3)
        self.server.close()
        self.write_status()
        self.source_log.close()
        self.wire_log.close()
        self.destroy_node()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=10001)
    parser.add_argument('--output', type=Path, default=Path('Logs/physx-ros-viewer'))
    args = parser.parse_args()
    if not 0 <= args.port <= 65535:
        parser.error('Port must be between 0 and 65535')
    args.output.mkdir(parents=True, exist_ok=True)
    if (args.output / 'status.json').exists():
        parser.error('Choose a fresh output directory to preserve prior observation records')
    rclpy.init(args=[])
    node = None
    def terminate(signum, frame):
        raise KeyboardInterrupt()
    signal.signal(signal.SIGTERM, terminate)
    try:
        node = ViewerBridge(args.port, args.output)
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    except Exception as error:
        if node is not None:
            node.error = repr(error)
        raise
    finally:
        if node is not None:
            node.close()
        if rclpy.ok():
            rclpy.shutdown()


if __name__ == '__main__':
    main()
