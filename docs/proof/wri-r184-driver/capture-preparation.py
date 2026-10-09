#!/usr/bin/env python3
"""Retain R184 preparation gates; ring: preparation, outer ceiling 120 seconds.

No action selects scale or invokes a product check or store verifier.
"""
from __future__ import annotations
import argparse
from datetime import datetime, timezone
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[3]
PROOF = Path(__file__).resolve().parent
IMPLEMENTATION = ("windows-runner.ps1", "windows-settings-preflight.ps1", "check-windows-runner.py",
                  "test-windows-runner.ps1", "windows-scale-run.ps1", "test-windows-scale-run.ps1")


def git(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, timeout=15, check=True)


def source_state() -> dict:
    paths = git("ls-files", "--", "src", "tests", "global.json", "CFDWorkbench.slnx").stdout.decode().splitlines()
    rows = [{"path": path, "sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest()} for path in paths]
    git("diff", "--exit-code", "HEAD", "--", "src", "tests", "global.json", "CFDWorkbench.slnx")
    if git("ls-files", "--others", "--exclude-standard", "--", "src", "tests", "global.json", "CFDWorkbench.slnx").stdout:
        raise RuntimeError("untracked guarded source")
    return {"count": len(rows), "sha256": hashlib.sha256(json.dumps(rows, sort_keys=True).encode()).hexdigest(), "diff_exit": 0}


def hashes() -> dict:
    return {"tools/" + name: hashlib.sha256((ROOT / "tools" / name).read_bytes()).hexdigest() for name in IMPLEMENTATION}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("gate", choices=("restore-red", "restore-green", "policy", "portable", "verify", "docs", "qualification", "pii", "startup-probe"))
    parser.add_argument("--attempt", choices=("initial", "repair1"), default="initial")
    args = parser.parse_args()
    commands = {
        "restore-red": ["pwsh", "-NoProfile", "-File", "tools/test-windows-scale-run.ps1", "-DriverPath", str(PROOF / "missing-finally.ps1")],
        "restore-green": ["pwsh", "-NoProfile", "-File", "tools/test-windows-scale-run.ps1"],
        "policy": [sys.executable, "tools/check-windows-runner.py"],
        "portable": [sys.executable, "docs/ai-forward-pack/scripts/verify-portable-text-io.py"],
        "docs": [sys.executable, "tools/check-docs.py"],
        "qualification": [sys.executable, "tools/check-windows-runner.py", "--self-test"],
        "pii": [sys.executable, "tools/check-proof-pii.py"],
        "startup-probe": ["pwsh", "-NoProfile", "-File", "docs/proof/wri-r184-driver/probe-startup.ps1"],
    }
    configured = json.loads((ROOT / "docs/coordination/join.json").read_text(encoding="utf-8"))["gates"][0]
    if configured != ["python3", "docs/ai-forward-pack/scripts/run-verify-gates.py", "--skip", "verify-application-core.py", "verify-application-adapters.py", "verify-windows-store.py"]:
        raise RuntimeError("configured gate changed")
    commands["verify"] = [sys.executable, *configured[1:]]
    label = args.gate + ("-repair1" if args.attempt == "repair1" else "")
    paths = [PROOF / (label + suffix) for suffix in (".stdout.txt", ".stderr.txt", ".json")]
    if any(path.exists() for path in paths):
        raise RuntimeError("evidence exists; append-only")
    environment = dict(os.environ)
    environment["CFD_WRI_PYTHON"] = sys.executable
    environment["CFD_PII_HOSTNAMES"] = os.environ.get("COMPUTERNAME") or socket.gethostname()
    os.environ["CFD_PII_HOSTNAMES"] = environment["CFD_PII_HOSTNAMES"]
    spec = importlib.util.spec_from_file_location("phn", ROOT / "tools/check-proof-pii.py")
    phn = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(phn)
    before, initial_hashes = source_state(), hashes()
    started_utc = datetime.now(timezone.utc).isoformat()
    started = time.perf_counter()
    cleanup = "not-needed"
    timed_out = False
    with tempfile.TemporaryDirectory(prefix="cfd-r184-preparation-") as scratch:
        out, err = Path(scratch) / "stdout.raw", Path(scratch) / "stderr.raw"
        with out.open("wb") as output, err.open("wb") as error:
            child = subprocess.Popen(commands[args.gate], cwd=ROOT, env=environment, stdout=output, stderr=error)
            try:
                child.wait(timeout=120)
            except subprocess.TimeoutExpired:
                timed_out = True
                termination = subprocess.run(["taskkill", "/PID", str(child.pid), "/T", "/F"], capture_output=True, timeout=5)
                try:
                    child.wait(timeout=5)
                    cleanup = "root-exited-taskkill-exit-" + str(termination.returncode)
                except subprocess.TimeoutExpired:
                    cleanup = "unresolved"
        stdout, stderr = out.read_bytes(), err.read_bytes()
        if phn.scan_text(stdout.decode("utf-8", errors="replace")) or phn.scan_text(stderr.decode("utf-8", errors="replace")):
            raise RuntimeError("PHN rejected producer bytes; publication withheld")
    after = source_state()
    if after != before or hashes() != initial_hashes:
        raise RuntimeError("source or implementation changed during capture")
    command = commands[args.gate]
    published_command = ["py", "-3", *command[1:]] if command[0] == sys.executable else command
    metadata = {"ruling": 184, "command": published_command, "head": git("rev-parse", "HEAD").stdout.decode().strip(),
                "started_utc": started_utc, "ended_utc": datetime.now(timezone.utc).isoformat(),
                "observed_process_exit": child.returncode, "timed_out": timed_out, "cleanup": cleanup,
                "outer_ceiling_seconds": 120, "wall_ms": round((time.perf_counter() - started) * 1000, 3),
                "implementation_sha256": initial_hashes, "source_baseline": before, "source_final": after,
                "stdout_sha256": hashlib.sha256(stdout).hexdigest(), "stderr_sha256": hashlib.sha256(stderr).hexdigest(),
                "publication": "PHN PASS; exact producer bytes; nested text sanitized by producer",
                "scale_change": False, "product_contract_execution": False, "store_verifier_execution": False}
    paths[0].write_bytes(stdout)
    paths[1].write_bytes(stderr)
    paths[2].write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"R184 CAPTURE {args.gate} numeric_exit={child.returncode} timeout={timed_out} cleanup={cleanup} wall_ms={metadata['wall_ms']} PHN=PASS source_unchanged=true")
    return 124 if timed_out else child.returncode


if __name__ == "__main__":
    # PLAT-A: use the pack-doctor legacy-console guard.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        raise SystemExit(main())
    except Exception as error:
        print("R184 CAPTURE FAIL " + type(error).__name__ + "; publication withheld", file=sys.stderr)
        raise SystemExit(1)
