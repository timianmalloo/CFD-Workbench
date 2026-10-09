#!/usr/bin/env python3
"""Ruling 183 append-only gate evidence; preparation ring, 120 s outer ceiling.

Sequence: direct portability, configured verify, docs; only then qualification.
No product command, scale mutation, verifier, full ring, or implementation edit.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time

PROOF = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("original_capture", PROOF / "capture-proof.py")
capture = importlib.util.module_from_spec(spec)
spec.loader.exec_module(capture)
ROOT = capture.ROOT
BASELINE = dict(capture.APPROVED)
BASELINE["tools/check-windows-runner.py"] = "8f044473b919558dbedd5a6e2a3ccd520025f81c6c48f31afb83d1501a48aa49"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("gate", choices=("portable", "verify", "docs", "qualification"))
    args = parser.parse_args()
    commands = {
        "portable": ["docs/ai-forward-pack/scripts/verify-portable-text-io.py"],
        "docs": ["tools/check-docs.py"],
        "qualification": ["tools/check-windows-runner.py", "--self-test"],
    }
    configured = json.loads((ROOT / "docs/coordination/join.json").read_text())["gates"][0]
    expected = ["python3", "docs/ai-forward-pack/scripts/run-verify-gates.py", "--skip",
                "verify-application-core.py", "verify-application-adapters.py", "verify-windows-store.py"]
    if configured != expected:
        raise RuntimeError("configured gate changed")
    commands["verify"] = configured[1:]
    hashes = {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in BASELINE}
    if hashes != BASELINE:
        raise RuntimeError("corrected baseline changed")
    paths = [PROOF / ("r183-" + args.gate + suffix) for suffix in (".stdout.txt", ".stderr.txt", ".json")]
    if any(path.exists() for path in paths):
        raise RuntimeError("evidence already retained")
    if args.gate == "qualification":
        for prerequisite in ("portable", "verify", "docs"):
            observed = json.loads((PROOF / ("r183-" + prerequisite + ".json")).read_text())
            if observed["observed_process_exit"] != 0 or observed["timed_out"] or observed["implementation_sha256"] != BASELINE:
                raise RuntimeError("prerequisite not green on corrected bytes")
    before = capture.sources()
    diff_before = capture.git("diff", "--exit-code", "HEAD", "--", "src", "tests").returncode
    if diff_before:
        raise RuntimeError("source baseline differs")
    os.environ["CFD_PII_HOSTNAMES"] = os.environ["COMPUTERNAME"]
    phn_spec = importlib.util.spec_from_file_location("phn", ROOT / "tools/check-proof-pii.py")
    phn = importlib.util.module_from_spec(phn_spec)
    phn_spec.loader.exec_module(phn)
    started = time.perf_counter()
    with tempfile.TemporaryDirectory(prefix="cfd-r183-proof-") as scratch:
        out_path, err_path = Path(scratch) / "stdout.raw", Path(scratch) / "stderr.raw"
        with out_path.open("wb") as output, err_path.open("wb") as error:
            child = subprocess.Popen([sys.executable, *commands[args.gate]], cwd=ROOT, stdout=output, stderr=error)
            timed_out = False
            try:
                child.wait(timeout=120)
            except subprocess.TimeoutExpired:
                timed_out = True
                child.kill()
                child.wait(timeout=5)
        stdout, stderr = out_path.read_bytes(), err_path.read_bytes()
        if phn.scan_text(stdout.decode("utf-8", errors="replace")) or phn.scan_text(stderr.decode("utf-8", errors="replace")):
            raise RuntimeError("PHN rejected producer bytes; publication withheld")
    after = capture.sources()
    diff_after = capture.git("diff", "--exit-code", "HEAD", "--", "src", "tests").returncode
    if after != before or diff_after:
        raise RuntimeError("source changed during capture")
    metadata = {"ruling": 183, "command": ["py", "-3", *commands[args.gate]],
                "head": capture.git("rev-parse", "HEAD").stdout.decode().strip(),
                "observed_process_exit": child.returncode, "timed_out": timed_out,
                "wall_ms": round((time.perf_counter() - started) * 1000, 3),
                "outer_budget_seconds": 120, "implementation_sha256": hashes,
                "source_baseline": before, "source_final": after,
                "source_diff_before_exit": diff_before, "source_diff_after_exit": diff_after,
                "stdout_sha256": hashlib.sha256(stdout).hexdigest(),
                "stderr_sha256": hashlib.sha256(stderr).hexdigest(),
                "publication": "PHN PASS; exact producer bytes; nested text sanitized by producer",
                "scale_change": False, "product_contract_execution": False,
                "store_verifier_execution": False, "full_test_ring_execution": False}
    paths[0].write_bytes(stdout)
    paths[1].write_bytes(stderr)
    paths[2].write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"R183 CAPTURE {args.gate} exit={child.returncode} timeout={timed_out} wall_ms={metadata['wall_ms']} PHN=PASS source_unchanged=true")
    return 124 if timed_out else child.returncode


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    try:
        raise SystemExit(main())
    except Exception as error:
        print("R183 CAPTURE FAIL " + type(error).__name__ + "; publication withheld", file=sys.stderr)
        raise SystemExit(1)
