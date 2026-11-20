#!/usr/bin/env python3
"""Prepare Unity 2022.3.4f1's ILPP Runner for the existing ARM64 .NET runtime.

Run with Unity closed. Requires the private Box64/ARM64 .NET/Roslyn installation
described in docs/UNITY_UBUNTU.md. Does not download or install system packages.
"""

import argparse
import hashlib
import json
from pathlib import Path
import platform
import shlex
import shutil
import subprocess
import tempfile


RUNNER_SHA256 = "3aeb50bc4e997985de393beaee315635f4548955cdf2bfab599f3c2f7f484f20"
DEPS_SHA256 = "30871122aa5ae182f6b8f9887004f795e57542e979290f87e0c98db97fc8e33b"


def prepare(editor, runtime):
    runner = editor / "Data/Tools/ilpp/Unity.ILPP.Runner"
    dotnet = runtime / "dotnet-arm64/dotnet"
    compiler = runtime / "roslyn-arm64/csc.dll"
    framework = runtime / "dotnet-arm64/shared/Microsoft.NETCore.App/6.0.36"
    cecil = editor / "Data/Managed/Unity.Cecil.dll"
    source = Path(__file__).with_name("UnityILPPPortable.cs")
    config = runtime / "dotnet-arm64/unity-tools-net6.runtimeconfig.json"
    for path in (dotnet, compiler, cecil, source, config):
        if not path.is_file():
            raise RuntimeError("Required file is missing: " + str(path))
    if not framework.is_dir():
        raise RuntimeError("The private ARM64 .NET 6.0.36 runtime is required.")
    for name, expected in (("Unity.ILPP.Runner.dll", RUNNER_SHA256),
                           ("Unity.ILPP.Runner.deps.json", DEPS_SHA256)):
        if hashlib.sha256((runner / name).read_bytes()).hexdigest() != expected:
            raise RuntimeError("Unsupported Unity ILPP source: " + name)

    wrapper = runner / "Unity.ILPP.Runner"
    backup = runner / "Unity.ILPP.Runner.box64-wrapper"
    wrapper_text = (
        "#!/usr/bin/env bash\nset -euo pipefail\n"
        "# OperaSimVR: native ARM64 ILPP Runner; original assemblies are preserved.\n"
        "exec " + shlex.quote(str(dotnet)) + " "
        + shlex.quote(str(runtime / "ilpp-arm64-portable/Unity.ILPP.Runner.dll"))
        + ' "$@"\n'
    )
    if not wrapper.is_file():
        raise RuntimeError("Unity ILPP Runner executable is missing.")
    if backup.exists() and wrapper.read_text() != wrapper_text:
        raise RuntimeError("An earlier wrapper backup exists; inspect it before replacing the wrapper.")

    with tempfile.TemporaryDirectory(prefix="ilpp-prepare-", dir=runtime) as temporary:
        work = Path(temporary)
        managed = work / "managed"
        managed.mkdir()
        deps = json.loads((runner / "Unity.ILPP.Runner.deps.json").read_text())
        target = deps["targets"][deps["runtimeTarget"]["name"]]
        for package, assets in target.items():
            # Core runtime/native libraries must come from the ARM64 framework.
            if package.startswith("runtimepack.Microsoft.NETCore.App."):
                continue
            for filename in assets.get("runtime", {}):
                path = runner / Path(filename).name
                if not path.is_file():
                    raise RuntimeError("Missing managed ILPP dependency: " + str(path))
                shutil.copy2(path, managed / path.name)

        shutil.copy2(cecil, work / cecil.name)
        shutil.copy2(config, work / "UnityILPPPortable.runtimeconfig.json")
        tool = work / "UnityILPPPortable.dll"
        subprocess.run([str(dotnet), str(compiler), "-nologo", "-target:exe",
                        "-out:" + str(tool), "-r:" + str(work / cecil.name)]
                       + ["-r:" + str(path) for path in sorted(framework.glob("*.dll"))]
                       + [str(source)], check=True)
        portable = work / "portable"
        subprocess.run([str(dotnet), str(tool), str(managed), str(portable),
                        str(framework)], check=True)
        shutil.copy2(config, portable / "Unity.ILPP.Runner.runtimeconfig.json")
        destination = runtime / "ilpp-arm64-portable"
        destination.mkdir(exist_ok=True)
        for path in portable.iterdir():
            shutil.copy2(path, destination / path.name)

    if not backup.exists():
        shutil.copy2(wrapper, backup)
    wrapper.write_text(wrapper_text)
    wrapper.chmod(0o755)
    print("Installed the native ARM64 ILPP Runner wrapper. Previous wrapper: " + str(backup))


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
    except (OSError, ValueError, RuntimeError, subprocess.CalledProcessError) as error:
        parser.exit(1, "ARM64 ILPP preparation failed: " + str(error) + "\n")


if __name__ == "__main__":
    main()
