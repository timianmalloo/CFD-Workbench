#!/usr/bin/env python3
"""Capture runner qualification or an authorized non-product gate as byte evidence.

Ring: runner preparation. Qualification has a 120 s outer allowance; its runtime
worker has a 60 s deadline. Other gate budgets retain their repository contracts.
This wrapper never selects a scale, builds the product, or runs product checks.
"""
from __future__ import annotations
import argparse
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
APPROVED = {
    "tools/windows-runner.ps1": "db8a8872e503575b8c084d0f30d4c15e635defc0c7043e6a126c472d391c3961",
    "tools/windows-settings-preflight.ps1": "da54b92d6830ae51e545d86b92afa2bce1f57a2a4e12f601333bdd259e5e89a0",
    "tools/test-windows-runner.ps1": "a6e2b9747a91b7284e29410aa026683262eb7b02e31e4adca3e12cab8d09d4a1",
    "tools/check-windows-runner.py": "4d13568b2a5d530da720a38663d142d8ecc3cde1e214f203e1dbc2c1f9c2b925",
}

def git(*arguments: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *arguments], cwd=ROOT, capture_output=True, timeout=30)

def sources() -> dict:
    listing = git("ls-files", "--", "src", "tests")
    if listing.returncode:
        raise RuntimeError("source listing failed")
    rows = [{"path": path, "sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest()}
            for path in listing.stdout.decode().splitlines()]
    return {"count": len(rows), "sha256": hashlib.sha256(json.dumps(rows, sort_keys=True).encode()).hexdigest()}

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("gate", choices=("qualification", "policy", "docs", "pii", "manifests", "verify"))
    args = parser.parse_args()
    commands = {
        "qualification": ["tools/check-windows-runner.py", "--self-test"],
        "policy": ["tools/check-windows-runner.py"],
        "docs": ["tools/check-docs.py"],
        "pii": ["tools/check-proof-pii.py"],
        "manifests": ["tools/check-capture-manifests.py"],
    }
    if args.gate == "verify":
        configured = json.loads((ROOT / "docs/coordination/join.json").read_text())["gates"][0]
        if configured[0] != "python3" or configured[1] != "docs/ai-forward-pack/scripts/run-verify-gates.py":
            raise RuntimeError("verify gate contract changed")
        commands["verify"] = configured[1:]
    hashes = {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in APPROVED}
    if hashes != APPROVED:
        raise RuntimeError("approved implementation hash changed")
    before = sources()
    before_diff = git("diff", "--exit-code", "HEAD", "--", "src", "tests").returncode
    if before_diff:
        raise RuntimeError("source baseline differs from merged HEAD")
    environment = dict(os.environ)
    environment["CFD_PII_HOSTNAMES"] = os.environ.get("COMPUTERNAME") or socket.gethostname()
    spec = importlib.util.spec_from_file_location("phn", ROOT / "tools/check-proof-pii.py")
    phn = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(phn)
    os.environ["CFD_PII_HOSTNAMES"] = environment["CFD_PII_HOSTNAMES"]
    paths = [PROOF / (args.gate + suffix) for suffix in (".stdout.txt", ".stderr.txt", ".json")]
    if any(path.exists() for path in paths):
        raise RuntimeError("capture already exists; evidence is append-only")
    budget = 120 if args.gate == "qualification" else 600
    started = time.perf_counter()
    with tempfile.TemporaryDirectory(prefix="cfd-runner-proof-") as scratch:
        raw_out, raw_err = Path(scratch) / "stdout.raw", Path(scratch) / "stderr.raw"
        with raw_out.open("wb") as output, raw_err.open("wb") as error:
            child = subprocess.Popen([sys.executable, *commands[args.gate]], cwd=ROOT,
                                     env=environment, stdout=output, stderr=error)
            timed_out = False
            try:
                child.wait(timeout=budget)
            except subprocess.TimeoutExpired:
                timed_out = True
                child.kill()
                child.wait(timeout=5)
        stdout, stderr = raw_out.read_bytes(), raw_err.read_bytes()
        if phn.scan_text(stdout.decode("utf-8", errors="replace")) or phn.scan_text(stderr.decode("utf-8", errors="replace")):
            raise RuntimeError("PHN rejected producer bytes; capture publication withheld")
    final = sources()
    final_diff = git("diff", "--exit-code", "HEAD", "--", "src", "tests").returncode
    if final != before or final_diff:
        raise RuntimeError("source changed during capture")
    metadata = {
        "command": ["py", "-3", *commands[args.gate]],
        "head": git("rev-parse", "HEAD").stdout.decode().strip(),
        "observed_process_exit": child.returncode,
        "timed_out": timed_out,
        "outer_budget_seconds": budget,
        "wall_ms": round((time.perf_counter() - started) * 1000, 3),
        "source_baseline": before, "source_final": final,
        "source_diff_before_exit": before_diff, "source_diff_after_exit": final_diff,
        "approved_implementation_sha256": hashes,
        "stdout_sha256": hashlib.sha256(stdout).hexdigest(),
        "stderr_sha256": hashlib.sha256(stderr).hexdigest(),
        "publication": "PHN PASS; exact producer bytes; nested child text is explicitly sanitized by producer",
        "scale_change": False, "product_contract_execution": False, "store_verifier_execution": False,
    }
    paths[0].write_bytes(stdout)
    paths[1].write_bytes(stderr)
    paths[2].write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"CAPTURE {args.gate} numeric_exit={child.returncode} timeout={timed_out} wall_ms={metadata['wall_ms']} PHN=PASS source_unchanged=true")
    return 124 if timed_out else child.returncode

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print("CAPTURE FAIL " + type(error).__name__ + "; raw publication withheld", file=sys.stderr)
        raise SystemExit(1)
