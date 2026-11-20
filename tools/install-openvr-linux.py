#!/usr/bin/env python3
"""Install the opt-in, pinned OpenVR package without changing XR project settings.

Run only while Unity is closed. The generated embedded package must be gitignored.
This installs project files; SteamVR, ALVR and headset setup are separate.
"""

import argparse
import fcntl
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import struct
import sys
import tarfile
import tempfile
import urllib.request


PACKAGE = "com.valvesoftware.unity.openvr"
VERSION = "1.2.3"
COMMIT = "35e0a1dce5c96d8f4a56312ae84acfa8e4c05dc7"
# Use an immutable source commit, including its checked-in Linux x86_64 provider.
# The v1.2.4 release provider requires GLIBC_2.38 / GLIBCXX_3.4.32; this v1.2.3
# provider needs GLIBC_2.34 / GLIBCXX_3.4.29 and works with Ubuntu 22.04's ABI.
ARCHIVE_URL = "https://codeload.github.com/ValveSoftware/unity-xr-plugin/tar.gz/" + COMMIT
ARCHIVE_SHA256 = "e59c0806f1cc23c67a5537a23ca09853d6f72197b34fd3c910b8a31b9ba78d5b"
ARCHIVE_SIZE = 35640732
PACKAGE_JSON_BEFORE = "a934ab1ac866627f735ed73d971ffe293963300fc50171017e74f6b39d033c21"
PACKAGE_JSON_AFTER = "46ce268d105fc55c1f67a9faca73526fd58bee85cd2a74984baa0300b169c675"
NATIVE = {
    "Runtime/x64/libXRSDKOpenVR.so": "d9fdeb25e6166728d3103ffc6957a40e65edfd0866354c60064687c81c0f222e",
    "Runtime/x64/libopenvr_api.so": "9ba64e5288d450e4b856b7bfbb5de0fa0dcdbdbaba1ae71ccd5a7ae3ae7d6662",
}
IMPORTER_HASHES = {
    "Runtime/x86/libopenvr_api.so.meta": (
        "96a34e1170da68ea806596cfeac6ab005b547abff443b3c3adaafde3ea11a765",
        "962c8a7f02e41bc2ddb8575202bd4189b0397732be0ed1fcb6a167e927019e41"),
    "Runtime/x64/XRSDKOpenVR.dll.meta": (
        "6ce58ec3c56b5395737fdaefc91929f086dddb6c046a8bf5ff04ec0b94524384",
        "d4a419fbf39b717a96dcbd7286cce208d99812fae5c9e13ff01ee4b4d8525b92"),
    "Runtime/x64/openvr_api.dll.meta": (
        "e25c263c476811e5cc35d97f6d847d0b9298c852b145fd015027c0bcf2bf0e1a",
        "91b6e77ec6aa1290a5de44d25d7ca3b1f32e231ebd7e9f99a0d19fb8f9a2bb2b"),
    "Runtime/x86/XRSDKOpenVR.dll.meta": (
        "bdfe8174902414e19331b15c56841197f71207d8157318eb82774f2d30d0753c",
        "3e265103d62d601838fc5dcd52d13230e7149e5e367d985e360a1ccafde6250b"),
    "Runtime/x86/openvr_api.dll.meta": (
        "0ab5304a83351f07b3889aab572f2817bb505e568cbf2bfc52946ec614a16a8e",
        "4be66b26aa6e5f677e35682f6c2959f423466aba8eb5a18eef536b8b26416a2d"),
    "Runtime/x64/libXRSDKOpenVR.so.meta": (
        "ab71af6d3fb60af18bdc5ccb31976b063d81ba0bb514d215559287d09cbb0148",
        "13a1d5c38f178c3a02ccb403ee5c2f3ca712d28ed9e5f8ce78be7afa613edea4"),
    "Runtime/x64/libopenvr_api.so.meta": (
        "e76f1fb5e41b3baacd427fec613c3197e5a4d29b1d1309ff54dc0f81bd2dd391",
        "0ba1bcdfe03d0533b4d945bff28dab58dde4e7f7370e2eb5146ab693b635f629"),
}


def digest(contents):
    return hashlib.sha256(contents).hexdigest()


def patch_native_importers(files):
    # The pinned package enables its ELF32 .so for Any Player and enables PE
    # Windows DLLs for Linux64. Remove these wrong-format/architecture candidates.
    # Editor OS restrictions also keep the same-name PE and ELF plugins separate.
    # The ELF64 Linux64 importer settings and all Windows Player settings stay as-is.
    linux_player_before = (b"      Standalone: Linux64\n    second:\n      enabled: 1\n"
                           b"      settings:\n        CPU: AnyCPU\n")
    linux_player_after = (b"      Standalone: Linux64\n    second:\n      enabled: 0\n"
                          b"      settings:\n        CPU: None\n")
    for name, (before_hash, after_hash) in IMPORTER_HASHES.items():
        contents = files[name]
        if digest(contents) != before_hash:
            raise RuntimeError("OpenVR importer differs from the pinned commit: " + name)
        if name == "Runtime/x86/libopenvr_api.so.meta":
            replacements = [(b"      Any: \n    second:\n      enabled: 1\n",
                             b"      Any: \n    second:\n      enabled: 0\n")]
        elif name.endswith(".dll.meta"):
            replacements = [
                (b"        Exclude Linux64: 0\n", b"        Exclude Linux64: 1\n"),
                (b"        OS: AnyOS\n", b"        OS: Windows\n"),
                (linux_player_before, linux_player_after),
            ]
        else:
            replacements = [(b"        OS: AnyOS\n", b"        OS: Linux\n")]
        for before, after in replacements:
            if contents.count(before) != 1:
                raise RuntimeError("Unexpected OpenVR importer patch context: " + name)
            contents = contents.replace(before, after)
        if digest(contents) != after_hash:
            raise RuntimeError("The OpenVR importer patch failed its checksum check: " + name)
        files[name] = contents


def assert_editor_closed(root):
    lock = root / "Temp/UnityLockfile"
    if not lock.exists():
        return
    # A stale UnityLockfile is harmless. Refuse when another process has it open.
    for descriptor in Path("/proc").glob("[0-9]*/fd/*"):
        try:
            if descriptor.samefile(lock):
                raise RuntimeError("Close this project's Unity Editor before installing OpenVR.")
        except (FileNotFoundError, PermissionError, ProcessLookupError):
            pass


def checked_archive(data):
    if len(data) != ARCHIVE_SIZE or digest(data) != ARCHIVE_SHA256:
        raise RuntimeError("OpenVR archive failed size/SHA256 verification; nothing was installed.")
    return data


def read_archive(path):
    if path.is_symlink() or not path.is_file():
        raise RuntimeError("OpenVR archive must be a regular file: " + str(path))
    if path.stat().st_size != ARCHIVE_SIZE:
        raise RuntimeError("OpenVR archive size does not match the pinned commit archive: " + str(path))
    return checked_archive(path.read_bytes())


def archive_contents(path, root):
    if path is not None:
        return read_archive(path)
    cached = root / "Library/OperaSimVRSetup" / ("unity-xr-plugin-" + COMMIT + ".tar.gz")
    if cached.exists() or cached.is_symlink():
        # Refuse a corrupt/customized cache instead of silently downloading over it.
        return read_archive(cached)
    print("Downloading Valve OpenVR " + VERSION + " (pinned source commit)...", flush=True)
    request = urllib.request.Request(ARCHIVE_URL, headers={"User-Agent": "OperaSimVR-Ubuntu-setup"})
    with urllib.request.urlopen(request, timeout=60) as response:
        data = checked_archive(response.read(ARCHIVE_SIZE + 1))
    # Publish only fully verified bytes. A hard link claims the cache name atomically
    # without overwriting a file another process/user might have created meanwhile.
    cached.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=cached.parent, prefix=".openvr-", delete=False) as output:
            temporary = Path(output.name)
            output.write(data)
        temporary.chmod(0o644)
        try:
            os.link(temporary, cached)
        except FileExistsError:
            return read_archive(cached)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
    return data


def package_files(data):
    files = {}
    prefix = ("unity-xr-plugin-" + COMMIT, "com.valve.openvr")
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        for member in archive:
            relative = PurePosixPath(member.name)
            if relative.is_absolute() or ".." in relative.parts:
                raise RuntimeError("Unsafe path in OpenVR package archive.")
            if relative.parts[:2] != prefix:
                continue
            if member.isdir():
                continue
            if not member.isfile() or member.size > 32 * 1024 * 1024:
                raise RuntimeError("Unsupported entry in OpenVR package archive.")
            name = PurePosixPath(*relative.parts[2:]).as_posix()
            if name in files:
                raise RuntimeError("Duplicate path in OpenVR package archive.")
            contents = archive.extractfile(member).read()
            if len(contents) != member.size:
                raise RuntimeError("Incomplete OpenVR archive entry.")
            files[name] = contents
    original = files["package.json"]
    metadata = json.loads(original)
    if metadata.get("name") != PACKAGE or metadata.get("version") != VERSION or digest(original) != PACKAGE_JSON_BEFORE:
        raise RuntimeError("OpenVR package metadata differs from the pinned commit.")
    for name, expected in NATIVE.items():
        contents = files[name]
        # ELF64, little endian, EM_X86_64. This package is not a native ARM64 provider.
        if digest(contents) != expected or contents[:6] != b"\x7fELF\x02\x01" or struct.unpack_from("<H", contents, 18)[0] != 62:
            raise RuntimeError("OpenVR native library failed x86_64/SHA256 verification: " + name)
    # Upstream's asmdef unconditionally references Unity.InputSystem but omits its UPM
    # dependency. Declare it in this generated copy; leave Active Input Handling alone.
    metadata["dependencies"]["com.unity.inputsystem"] = "1.7.0"
    patched = (json.dumps(metadata, indent=2) + "\n").encode()
    if digest(patched) != PACKAGE_JSON_AFTER:
        raise RuntimeError("The verified OpenVR dependency patch failed its checksum check.")
    files["package.json"] = patched
    patch_native_importers(files)
    return files


def install(root, files):
    packages = root / "Packages"
    if not (packages / "manifest.json").is_file():
        raise RuntimeError("Not a Unity project: Packages/manifest.json is missing.")
    target = packages / PACKAGE
    if target.exists() or target.is_symlink():
        if target.is_symlink() or not target.is_dir():
            raise RuntimeError("Existing OpenVR target is not a regular directory; refusing to replace it.")
        current = {}
        for path in target.rglob("*"):
            if path.is_symlink():
                raise RuntimeError("Existing OpenVR package has a symlink; refusing to overwrite it.")
            if path.is_file():
                current[path.relative_to(target).as_posix()] = path.read_bytes()
        if current != files:
            raise RuntimeError("Existing OpenVR package differs from this pinned copy; no files were changed.")
        print("The pinned OpenVR " + VERSION + " embedded package is already installed.")
        return
    with tempfile.TemporaryDirectory(dir=packages, prefix=".operasimvr-openvr-") as temporary:
        staged = Path(temporary) / PACKAGE
        staged.mkdir()
        for name, contents in files.items():
            path = staged / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(contents)
            path.chmod(0o644)
        assert_editor_closed(root)
        staged.rename(target)
    print("Installed OpenVR " + VERSION + " as a generated embedded package.")
    print("Input System 1.7.0 is declared only by that package; keep Active Input Handling set to Input Manager.")
    print("XR loaders, graphics settings, scenes, SteamVR and ALVR were not configured.")
    print("The provider contains x86_64 libraries; native ARM64 headset support is unverified.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--archive", type=Path, help="use an already downloaded, checksum-verified commit archive")
    args = parser.parse_args()
    root = args.project_root.resolve()
    try:
        if not (root / "Packages/manifest.json").is_file():
            raise RuntimeError("Not a Unity project: Packages/manifest.json is missing.")
        (root / "Temp").mkdir(exist_ok=True)
        with (root / "Temp/OperaSimVRUbuntu.openvr-install.lock").open("a") as lock:
            try:
                fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            except BlockingIOError:
                raise RuntimeError("Another OpenVR installer is already running.")
            assert_editor_closed(root)
            install(root, package_files(archive_contents(args.archive, root)))
    except (OSError, ValueError, RuntimeError, KeyError, tarfile.TarError) as error:
        print("OpenVR setup: " + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
