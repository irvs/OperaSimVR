#!/usr/bin/env python3
"""Route the verified Unity metadata generator through the private ARM64 .NET.

Run with Unity closed. Requires the existing experimental ARM64 installation.
Preserves the previous netcorerun wrapper and its x86_64 executable fallback.
"""

import argparse
import hashlib
import os
from pathlib import Path
import platform
import shutil
import tempfile


GENERATOR_SHA256 = "2c359f65ec62d486b8e8263ce0dce988851ba51af02fedf3b1da37c2bb95e29c"
WRAPPER_SHA256 = "0104deffd0c9e708ae43be94c84ed3f2657c92bd8d3efc52bd35d5b7c24b18cf"
OLD_CASE = b"  */ScriptCompilationBuildProgram.exe)\n"
NEW_CASE = b"  */ScriptCompilationBuildProgram.exe|*/BuildPlayerDataGenerator.exe)\n"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def prepare(editor, runtime):
    wrapper = editor / "Data/Tools/netcorerun/netcorerun"
    generator = editor / "Data/Tools/BuildPlayerDataGenerator/BuildPlayerDataGenerator.exe"
    backup = wrapper.with_name("netcorerun.before-operasimvr-builddata")
    for path in (wrapper, generator, runtime / "dotnet-arm64/dotnet",
                 runtime / "dotnet-arm64/unity-tools-net6.runtimeconfig.json"):
        if not path.is_file():
            raise RuntimeError("Required file is missing: " + str(path))
    if digest(generator.read_bytes()) != GENERATOR_SHA256:
        raise RuntimeError("Unsupported Unity BuildPlayerDataGenerator; no files changed.")
    current = wrapper.read_bytes()
    original = backup.read_bytes() if backup.exists() else current
    if digest(original) != WRAPPER_SHA256 or original.count(OLD_CASE) != 1:
        raise RuntimeError("Unsupported netcorerun wrapper; no files changed.")
    expected = original.replace(OLD_CASE, NEW_CASE, 1)
    if current == expected:
        print("Native ARM64 Unity metadata generator is ready.")
        return
    if current != original:
        raise RuntimeError("netcorerun differs from its backup; no files changed.")
    if not backup.exists():
        shutil.copy2(wrapper, backup)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=wrapper.parent, prefix="netcorerun.operasimvr.",
                                         delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(expected)
        temporary.chmod(wrapper.stat().st_mode & 0o777)
        os.replace(temporary, wrapper)
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()
    print("Installed native ARM64 Unity metadata generator dispatch. Backup: " + str(backup))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor-root", type=Path,
                        default=Path.home() / "Unity/Hub/Editor/2022.3.4f1/Editor")
    parser.add_argument("--runtime-root", type=Path,
                        default=Path.home() / ".local/opt/operasimvr-unity")
    args = parser.parse_args()
    if platform.machine() not in ("aarch64", "arm64"):
        parser.error("This workaround is only for the experimental Linux ARM64 setup.")
    try:
        prepare(args.editor_root.resolve(), args.runtime_root.resolve())
    except (OSError, RuntimeError) as error:
        parser.exit(1, "ARM64 build tool preparation failed: " + str(error) + "\n")


if __name__ == "__main__":
    main()
