#!/usr/bin/env python3
"""Recount the serial application contract fixture from joined source."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


DEFAULT_ROOT = Path(__file__).resolve().parent.parent
SOURCE_PATHS = (
    "tools/spikes/ApplicationContracts/ApplicationContracts.csproj",
    "tools/spikes/ApplicationContracts/Program.cs",
    "tools/spikes/application-session-contract-vectors.py",
)
REQUIRED_CSHARP_CHECKS = {
    "Decimal_ZeroHugeExponent_Zero",
    "Draft_ExternalRetargetMutation_PreservesOwnedTarget",
    "Reopen_ApplyRetry_DurableExactlyOnce",
    "Reopen_OpenRetry_DurableExactlyOnce",
    "Reopen_UnknownGeometry_Refused",
    "Reopen_UnrelatedCertificate_Refused",
    "Native_LongEscapedId_ApplyLineOverflow_PreflightRefused",
    "Native_LongEscapedId_RecoveryLineOverflow_PreflightRefused",
    "Native_WriterLines_WithinReaderLimit",
    "Reopen_FirstApplyInsteadOfOpen_Refused",
    "SaveRequest_AsyncBoundary_CapturesDefensiveImage",
    "Reopen_DifferentWhitespace_Clean",
}


def run(command: list[str], *, cwd: Path, env: dict[str, str], timeout: int = 180) -> str:
    result = subprocess.run(
        command, cwd=cwd, env=env, capture_output=True, text=True,
        encoding="utf-8", errors="replace", timeout=timeout, check=False,
    )
    if result.returncode:
        raise RuntimeError(
            f"{command[0]} exited {result.returncode}: {(result.stdout + result.stderr)[-2400:]}"
        )
    return result.stdout


def recount(root: Path) -> dict[str, object]:
    proof = (root / "docs/proof/application-contracts.md").read_text(encoding="utf-8")
    source_hashes = {}
    for relative in SOURCE_PATHS:
        digest = hashlib.sha256((root / relative).read_bytes()).hexdigest()
        if digest not in proof:
            raise RuntimeError(f"integrated source SHA-256 absent from proof: {relative} {digest}")
        source_hashes[relative] = digest
    with tempfile.TemporaryDirectory(prefix="cfd-contract-recount-") as directory:
        scratch = Path(directory)
        env = os.environ.copy()
        env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
        env["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false"
        env["DOTNET_CLI_HOME"] = str(scratch / "dotnet-home")
        env["NUGET_PACKAGES"] = str(scratch / "nuget-packages")
        env["PIP_CACHE_DIR"] = str(scratch / "pip-cache")
        project = root / SOURCE_PATHS[0]
        output = scratch / "bin"
        obj = scratch / "obj"
        build = run(
            ["dotnet", "build", str(project),
             f"-p:BaseIntermediateOutputPath={obj}{os.sep}",
             f"-p:OutputPath={output}{os.sep}", "--nologo"],
            cwd=root, env=env,
        )
        if "Build succeeded." not in build or "0 Error(s)" not in build:
            raise RuntimeError("dotnet build exited 0 without a confirmed clean build")
        venv = scratch / "venv"
        run([sys.executable, "-m", "venv", str(venv)], cwd=root, env=env)
        scripts = venv / ("Scripts" if os.name == "nt" else "bin")
        python = scripts / ("python.exe" if os.name == "nt" else "python")
        run(
            [str(python), "-m", "pip", "install", "--disable-pip-version-check",
             "blake3==1.0.8", "rfc8785==0.1.4"],
            cwd=root, env=env,
        )
        dll = output / "ApplicationContracts.dll"
        raw = run(
            [str(python), str(root / SOURCE_PATHS[2]), str(dll)],
            cwd=root, env=env,
        )
        receipt = json.loads(raw)
        csharp = receipt.get("csharpChecks")
        independent = receipt.get("pythonChecks")
        if (
            receipt.get("scope") != "independent-contract-oracle"
            or not isinstance(csharp, list) or len(csharp) != 89
            or not isinstance(independent, list) or len(independent) != 42
            or len(csharp) != receipt.get("csharpCheckCount")
            or len(independent) != receipt.get("pythonCheckCount")
            or len(set(csharp)) != len(csharp)
            or len(set(independent)) != len(independent)
            or not REQUIRED_CSHARP_CHECKS.issubset(csharp)
            or receipt.get("batchVectorCount") != 2505
            or receipt.get("nativeLiveWindows") != "Not assessed"
            or receipt.get("nativeHandlePolicy") != "Not assessed"
            or receipt.get("geometryCertificate") != "fixture authority only"
        ):
            raise RuntimeError("contract oracle receipt does not meet the named design-only floor")
        return {
            "scope": receipt["scope"],
            "runtime": receipt["runtime"],
            "csharp_checks": len(csharp),
            "python_checks": len(independent),
            "batch_vectors": receipt["batchVectorCount"],
            "source_sha256": source_hashes,
        }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=DEFAULT_ROOT)
    args = parser.parse_args()
    try:
        print(json.dumps(recount(args.root.resolve()), sort_keys=True))
        return 0
    except (OSError, ValueError, KeyError, subprocess.TimeoutExpired, RuntimeError) as error:
        print(f"application contract recount failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
