#!/usr/bin/env python3
"""Prepare the pinned UnitySensors package for Player builds."""

import argparse
import fcntl
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tempfile
import time
import urllib.request


PACKAGE = "com.frj.unity-sensors"
COMMIT = "b1a50503bb38017f902cb232b489de2de334d62b"
SOURCE = "https://github.com/Field-Robotics-Japan/UnitySensors.git?path=/Assets/UnitySensors#" + COMMIT
ASSEMBLY = Path("Editor/UnitySensorsEditor.asmdef")
BEFORE = "e67979c528915fa70d768f7ec73f28d32e6496ff8aed31b42d1ee8fc710d3833"
AFTER = "ecd614dc60e8e8d4af3ddd27d1a6a3b514d100bc250626d1035b17090147ba53"
LFS_ASSET = Path("Runtime/RawData/LivoxModels/mid-360.obj")
LFS_SHA256 = "ad16b6ac2f88f3a5bf0d33f1a25e5e53438ce3808dab349bca360567910b6f11"
LFS_SIZE = 1680529
LFS_URL = ("https://media.githubusercontent.com/media/Field-Robotics-Japan/UnitySensors/"
           + COMMIT + "/Assets/UnitySensors/" + LFS_ASSET.as_posix())


def restore_lfs_asset(package):
    """Resolve the known upstream LFS pointer without changing Git's filter settings."""
    asset = package / LFS_ASSET
    if not asset.is_file():
        return
    original = asset.read_bytes()
    if not original.startswith(b"version https://git-lfs.github.com/spec/v1"):
        return
    expected = ["version https://git-lfs.github.com/spec/v1", "oid sha256:" + LFS_SHA256,
                "size " + str(LFS_SIZE)]
    if original.decode("ascii").splitlines() != expected:
        raise RuntimeError("UnitySensors model has an unverified LFS pointer; refusing to replace it.")
    print("Downloading the pinned upstream mid-360.obj LFS object...", flush=True)
    try:
        with urllib.request.urlopen(LFS_URL, timeout=30) as response:
            contents = response.read(LFS_SIZE + 1)
    except OSError as error:
        raise RuntimeError("Cannot resolve the pinned UnitySensors LFS object: " + str(error)) from error
    if len(contents) != LFS_SIZE or hashlib.sha256(contents).hexdigest() != LFS_SHA256:
        raise RuntimeError("UnitySensors LFS object failed its size/SHA256 check; original pointer was kept.")
    with tempfile.NamedTemporaryFile(dir=asset.parent, delete=False) as output:
        temporary = Path(output.name)
        output.write(contents)
    temporary.chmod(0o644)
    temporary.replace(asset)
    print("Restored the verified upstream mid-360.obj model.", flush=True)


def checked_assembly(package):
    metadata = json.loads((package / "package.json").read_text(encoding="utf-8"))
    if metadata.get("name") != PACKAGE or metadata.get("version") != "2.0.4":
        raise RuntimeError("This fix requires the verified UnitySensors 2.0.4 source.")
    original = (package / ASSEMBLY).read_bytes()
    digest = hashlib.sha256(original).hexdigest()
    if digest == AFTER:
        return original
    if digest != BEFORE:
        raise RuntimeError("UnitySensors editor assembly was changed; refusing to overwrite it.")
    patched = original.replace(b'"includePlatforms": []', b'"includePlatforms": ["Editor"]', 1)
    if hashlib.sha256(patched).hexdigest() != AFTER:
        raise RuntimeError("UnitySensors editor assembly fix failed its checksum check.")
    return patched


def patch_package(root):
    manifest = json.loads((root / "Packages/manifest.json").read_text(encoding="utf-8"))
    if manifest.get("dependencies", {}).get(PACKAGE) != SOURCE:
        raise RuntimeError("Pin UnitySensors to verified commit " + COMMIT + " before applying this fix.")
    embedded = root / "Packages" / PACKAGE
    if embedded.exists():
        patched = checked_assembly(embedded)
        restore_lfs_asset(embedded)
        if (embedded / ASSEMBLY).read_bytes() != patched:
            with tempfile.NamedTemporaryFile(dir=(embedded / ASSEMBLY).parent, delete=False) as output:
                temporary = Path(output.name)
                output.write(patched)
            temporary.chmod(0o644)
            temporary.replace(embedded / ASSEMBLY)
        print("Embedded UnitySensors editor assembly is restricted to Editor.", flush=True)
        return True
    cache = root / "Library/PackageCache" / (PACKAGE + "@" + COMMIT[:10])
    if not (cache / ASSEMBLY).is_file() or not (cache / "package.json").is_file():
        return False
    patched = checked_assembly(cache)
    with tempfile.TemporaryDirectory(dir=root / "Packages", prefix=".operasimvr-unity-sensors-") as temporary:
        staged = Path(temporary) / PACKAGE
        shutil.copytree(cache, staged)
        if checked_assembly(staged) != patched:
            raise RuntimeError("UnitySensors changed while preparing its embedded copy.")
        (staged / ASSEMBLY).write_bytes(patched)
        restore_lfs_asset(staged)
        staged.rename(embedded)
    print("Embedded pinned UnitySensors with its Editor-only assembly fix; runtime sensors are unchanged.", flush=True)
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--wait", type=float, default=0, metavar="SECONDS")
    args = parser.parse_args()
    if args.wait < 0:
        parser.error("--wait must be nonnegative")
    root = Path(__file__).resolve().parent.parent
    (root / "Temp").mkdir(exist_ok=True)
    with (root / "Temp/OperaSimVRUbuntu.unity-sensors-patch.lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print("A UnitySensors fix watcher is already running.")
            return 0
        deadline = time.monotonic() + args.wait
        try:
            while not patch_package(root):
                if not args.wait:
                    print("Pinned UnitySensors is not downloaded yet; the launcher will watch for it.")
                    return 0
                if time.monotonic() >= deadline:
                    raise RuntimeError("Timed out waiting for pinned UnitySensors; rerun after package import.")
                time.sleep(min(1.0, max(0, deadline - time.monotonic())))
        except (OSError, ValueError, RuntimeError) as error:
            print("UnitySensors build fix: " + str(error), file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
