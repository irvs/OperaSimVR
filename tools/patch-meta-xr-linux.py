#!/usr/bin/env python3
"""Embed Meta XR 72.0.0 with verified Linux Editor and OpenVR controller fixes."""

import argparse
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys
import tempfile
import time


PACKAGE_NAME = "com.meta.xr.sdk.core"
VERSION = "72.0.0"
ORIGINAL_SHA256 = "9d4926bc5e34c8e334cb28f76edce4d7e15522862e5cb7f29174bed3b15a80d5"
PATCHED_SHA256 = "838011b43e9dee5d288b5ad2ba32e2381719e6a5990f2851ccb1bd5e621f6550"
MARKER = b"// OperaSimVR Ubuntu: keep the shared Inspector reference available on Linux."
GUARD = b"#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID\n"
ANCHOR = (
    b"        bool modified = false;\n\n" + GUARD
    + b"        OVRManager manager = (OVRManager)target;\n"
)
REPLACEMENT = (
    b"        bool modified = false;\n\n        " + MARKER
    + b"\n        OVRManager manager = (OVRManager)target;\n" + GUARD
)
ADB_ANCHOR = b"        _adbTool = new OVRADBTool(OVRConfig.GetAndroidSDKPathLocation());"
ADB_REPLACEMENT = b"""#if UNITY_EDITOR_LINUX
        // OperaSimVR Ubuntu: desktop viewing does not require the Android module.
        bool androidAvailable = UnityEditor.BuildPipeline.IsBuildTargetSupported(
            UnityEditor.BuildTargetGroup.Android, UnityEditor.BuildTarget.Android);
        _adbTool = new OVRADBTool(androidAvailable ? OVRConfig.GetAndroidSDKPathLocation(false) : string.Empty);
#else
        _adbTool = new OVRADBTool(OVRConfig.GetAndroidSDKPathLocation());
#endif"""
FIXES = {
    Path("Scripts/Editor/OVRManagerEditor.cs"):
        (ORIGINAL_SHA256, PATCHED_SHA256, ANCHOR, REPLACEMENT),
    Path("Editor/UnityADBToolSingleton.cs"):
        ("77bfe8199b2d12bc9a58916f16726e41d1bd38537760e31f1d3bd37274bd5116",
         "60f087925886339e054f435da392d5aeee2e4e0c39cae336883b65feb13ca4c7",
         ADB_ANCHOR, ADB_REPLACEMENT),
}
PATCHES = Path(__file__).resolve().parent.parent / "patches"
DIFF_FIXES = {
    Path("Scripts/OVRInput.cs"):
        ("fd6272d4e69a9f3e852d9b7d1da61ae56f477427ebf4a1207b3efee75b35799f",
         "d41044bd90ec54c3236bca30c0f96e8d6e14988490d7d1181b4ae17539c0930a",
         PATCHES / "meta-xr-72-linux-openvr-input.patch"),
    Path("Scripts/Util/OVRControllerHelper.cs"):
        ("ceca09d6c7395016aeb40e7e4dc801556f85edfa140c04de84629c1b5406f20f",
         "fbe792986ff6df36d2bd8d21c6e07318b5e361e3144d6be13b2f740a40073f3d",
         PATCHES / "meta-xr-72-linux-controller-helper.patch"),
}


def apply_unified_diff(original, patch, relative_source):
    """Apply one verified-file diff at its exact line numbers, with no offsets/fuzz."""
    patch_lines = patch.splitlines(keepends=True)
    source_name = relative_source.as_posix().encode("utf-8")
    if patch_lines[:2] != [b"--- a/" + source_name + b"\n", b"+++ b/" + source_name + b"\n"]:
        raise RuntimeError("Meta XR patch has unexpected file headers.")
    source_lines = original.splitlines(keepends=True)
    output = []
    source_index = 0
    patch_index = 2
    hunk_count = 0
    while patch_index < len(patch_lines):
        header = re.fullmatch(rb"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@[^\n]*\n", patch_lines[patch_index])
        if header is None:
            raise RuntimeError("Meta XR patch has an invalid hunk header.")
        old_start = int(header[1])
        old_count = int(header[2] or b"1")
        new_start = int(header[3])
        new_count = int(header[4] or b"1")
        old_index = old_start - 1 if old_count else old_start
        new_index = new_start - 1 if new_count else new_start
        if old_index < source_index or old_index > len(source_lines):
            raise RuntimeError("Meta XR patch has an invalid source position.")
        output.extend(source_lines[source_index:old_index])
        source_index = old_index
        if len(output) != new_index:
            raise RuntimeError("Meta XR patch has an inconsistent destination position.")
        patch_index += 1
        removed = added = 0
        while patch_index < len(patch_lines) and not patch_lines[patch_index].startswith(b"@@ "):
            line = patch_lines[patch_index]
            marker, contents = line[:1], line[1:]
            if marker not in (b" ", b"-", b"+") or not contents.endswith(b"\n"):
                raise RuntimeError("Meta XR patch has an unsupported hunk line.")
            if marker in (b" ", b"-"):
                if source_index >= len(source_lines) or source_lines[source_index] != contents:
                    raise RuntimeError("Meta XR patch context does not match the verified source.")
                source_index += 1
                removed += 1
            if marker in (b" ", b"+"):
                output.append(contents)
                added += 1
            patch_index += 1
        if removed != old_count or added != new_count:
            raise RuntimeError("Meta XR patch hunk line counts do not match its header.")
        hunk_count += 1
    if not hunk_count:
        raise RuntimeError("Meta XR patch contains no hunks.")
    output.extend(source_lines[source_index:])
    return b"".join(output)


def checked_sources(package):
    metadata = json.loads((package / "package.json").read_text(encoding="utf-8"))
    if metadata.get("name") != PACKAGE_NAME or metadata.get("version") != VERSION:
        raise RuntimeError("Meta XR package metadata does not match verified version 72.0.0.")
    result = {}
    for relative_source, (before, after, anchor, replacement) in FIXES.items():
        original = (package / relative_source).read_bytes()
        digest = hashlib.sha256(original).hexdigest()
        if digest == after:
            result[relative_source] = original
            continue
        if digest != before or original.count(anchor) != 1:
            raise RuntimeError(str(relative_source) + " differs from the verified source; refusing to modify it.")
        patched = original.replace(anchor, replacement, 1)
        if hashlib.sha256(patched).hexdigest() != after:
            raise RuntimeError("Patched source checksum did not match; no file was changed.")
        result[relative_source] = patched
    for relative_source, (before, after, patch) in DIFF_FIXES.items():
        original = (package / relative_source).read_bytes()
        digest = hashlib.sha256(original).hexdigest()
        if digest == after:
            result[relative_source] = original
            continue
        if digest != before:
            raise RuntimeError(str(relative_source) + " differs from the verified source; refusing to modify it.")
        patched = apply_unified_diff(original, patch.read_bytes(), relative_source)
        if hashlib.sha256(patched).hexdigest() != after:
            raise RuntimeError(str(relative_source) + " patch checksum did not match; no file was changed.")
        result[relative_source] = patched
    return result


def write_source(source, contents):
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=source.parent, prefix=".operasimvr-", delete=False) as output:
            temporary = Path(output.name)
            output.write(contents)
        os.chmod(temporary, source.stat().st_mode & 0o777)
        os.replace(temporary, source)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def patch_package(project_root):
    packages = project_root / "Packages"
    manifest = json.loads((packages / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("dependencies", {}).get(PACKAGE_NAME) != VERSION:
        raise RuntimeError("The project must request Meta XR 72.0.0 before applying this fix.")
    embedded = packages / PACKAGE_NAME
    relative_source = Path("Scripts/Editor/OVRManagerEditor.cs")
    if embedded.exists():
        for relative_path, patched in checked_sources(embedded).items():
            if (embedded / relative_path).read_bytes() != patched:
                write_source(embedded / relative_path, patched)
        print("Embedded Meta XR 72.0.0 Linux Editor and OpenVR controller fixes are ready.", flush=True)
        return True

    cache = project_root / "Library/PackageCache"
    package = cache / (PACKAGE_NAME + "@" + VERSION)
    source = package / relative_source
    manifest = package / "package.json"
    if not source.is_file() or not manifest.is_file():
        alternatives = sorted(cache.glob(PACKAGE_NAME + "@*"))
        if not package.exists() and alternatives:
            raise RuntimeError("Unsupported Meta XR package version; this fix requires 72.0.0.")
        return False

    patches = checked_sources(package)
    # Registry caches are immutable: Unity discards in-place fixes on restart.
    # Prepare an embedded copy atomically, then leave the registry cache intact.
    with tempfile.TemporaryDirectory(dir=packages, prefix=".operasimvr-meta-xr-") as temporary:
        staged = Path(temporary) / PACKAGE_NAME
        shutil.copytree(package, staged)
        if checked_sources(staged) != patches:
            raise RuntimeError("The package changed while preparing its embedded copy.")
        for relative_path, patched in patches.items():
            write_source(staged / relative_path, patched)
        staged.rename(embedded)
    print("Embedded Meta XR 72.0.0 with verified Linux Editor and OpenVR controller fixes.", flush=True)
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--wait", type=float, default=0, metavar="SECONDS",
                        help="wait for the package to be downloaded (one watcher per project)")
    args = parser.parse_args()
    if args.wait < 0:
        parser.error("--wait must be nonnegative")
    root = Path(__file__).resolve().parent.parent
    (root / "Temp").mkdir(exist_ok=True)
    lock = (root / "Temp/OperaSimVRUbuntu.meta-xr-patch.lock").open("a")
    try:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        lock.close()
        print("A Meta XR package fix watcher is already running.")
        return 0
    deadline = time.monotonic() + args.wait
    try:
        while not patch_package(root):
            if not args.wait:
                print("Meta XR 72.0.0 is not downloaded yet; the launcher will watch for it.")
                return 0
            if time.monotonic() >= deadline:
                raise RuntimeError("Timed out waiting for Meta XR 72.0.0; rerun the launcher after package import.")
            time.sleep(min(1.0, max(0.0, deadline - time.monotonic())))
    except (OSError, ValueError, RuntimeError) as error:
        print("Meta XR Linux fix: " + str(error), file=sys.stderr)
        return 1
    finally:
        if lock is not None:
            lock.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
