#!/usr/bin/env python3
"""Recount the architecture spike from source in disposable, task-local caches."""

import contextlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "tools/spikes/ApplicationNativeUi/ApplicationNativeUi.csproj"
ORACLE = ROOT / "tools/spikes/application-contract-vectors.py"


def run(command: list[str], *, env: dict[str, str], cwd: Path = ROOT, timeout: int = 180) -> str:
    result = subprocess.run(
        command,
        cwd=cwd,
        env=env,
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
        timeout=timeout,
        check=False,
    )
    if result.returncode:
        print((result.stdout + result.stderr)[-3000:], file=sys.stderr)
        raise RuntimeError(f"exit {result.returncode}: {command[0]}")
    return result.stdout


@contextlib.contextmanager
def scratch_directory():
    """Temp folder removed with a bounded retry; a file still locked afterwards (Windows holds a loaded DLL) is reported, never left silently."""
    directory = Path(tempfile.mkdtemp(prefix="cfd-architecture-recount-"))
    try:
        yield directory
    finally:
        for attempt in range(5):
            try:
                shutil.rmtree(directory)
                break
            except FileNotFoundError:
                break
            except OSError as error:
                if attempt == 4:
                    raise OSError(f"scratch folder not removed after 5 attempts: {error}") from error
                time.sleep(1)


def main() -> int:
    with scratch_directory() as scratch:
        venv = scratch / "venv"
        env = os.environ.copy()
        env["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false"
        env["DOTNET_CLI_HOME"] = str(scratch / "dotnet-home")
        env["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"] = "false"
        env["NUGET_PACKAGES"] = str(scratch / "nuget-packages")
        env["PIP_CACHE_DIR"] = str(scratch / "pip-cache")
        # Avalonia's build-services collector otherwise outlives the build and holds its DLL in the NuGet cache (Windows: WinError 5 on cleanup).
        env["AVALONIA_TELEMETRY_OPTOUT"] = "1"
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        run([sys.executable, "-m", "venv", str(venv)], env=env)
        scripts = venv / ("Scripts" if os.name == "nt" else "bin")
        python = scripts / ("python.exe" if os.name == "nt" else "python")
        run([str(python), "-m", "pip", "install", "--disable-pip-version-check", "blake3==1.0.8", "rfc8785==0.1.4"], env=env)
        output = scratch / "bin"
        obj = scratch / "obj"
        build = run(
            ["dotnet", "build", str(PROJECT), "--disable-build-servers", "-p:UseSharedCompilation=false", f"-p:BaseIntermediateOutputPath={obj}{os.sep}", f"-p:OutputPath={output}{os.sep}"],
            env=env,
        )
        if "Build succeeded." not in build:
            raise RuntimeError("dotnet exited 0 without the expected build success output")
        native = output / ("ApplicationNativeUi.exe" if os.name == "nt" else "ApplicationNativeUi")
        binding = run([str(native), "--contracts"], env=env)
        if "9 mismatches refused" not in binding:
            raise RuntimeError("native binding oracle did not report nine refusals")
        raw = run([str(python), str(ORACLE), str(scratch), str(native)], env=env)
        report = json.loads(raw)
        checks = report.get("checks")
        if not isinstance(checks, list) or len(checks) != 30 or any(item.get("pass") is not True for item in checks):
            raise RuntimeError("architecture oracle did not pass exactly 30 checks")
        print(json.dumps({"native_mismatch_refusals": 9, "oracle_checks": 30, "oracle_seconds": report.get("seconds"), "scope": "architecture spike only"}))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, subprocess.TimeoutExpired, RuntimeError, ValueError) as error:
        print(f"architecture recount failed: {error}", file=sys.stderr)
        sys.exit(1)
