#!/usr/bin/env python3
"""Offline-fast composite conservation; Git/file operations only, no live state.

This proof reconstructs the four accepted reader deltas on exact helper ancestry
in an isolated temporary repository and compares the staged/candidate blobs.
It is an on-demand integration proof, not a new production/test-ring gate.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[3]
BASE = "37b96e4749a13d2fee3a7d73444d66a6a39a6403"
READER = "70d022bf4369c8cb58888f88e6efba00070f35db"
TARGETS = (
    "docs/proof/win-gpu-f1-preflight/packet/Invoke-Preflight.ps1",
    "docs/proof/win-gpu-f1-preflight/packet/SelfTest.ps1",
    "tools/check-docs.py",
    "tools/check-reader-sharing.py",
)
METADATA = {
    "docs/audit/audit-log.jsonl", "docs/audit/audit-data.js",
    "docs/docs-index.js", "docs/lessons/defect-classes.md",
}


def git(*args: str, cwd: Path = ROOT, data: bytes | None = None) -> bytes:
    return subprocess.run(["git", *args], cwd=cwd, input=data,
                          capture_output=True, check=True).stdout


def tree(ref: str) -> dict[str, str]:
    rows = {}
    for raw in git("ls-tree", "-r", "-z", ref).split(b"\0"):
        if raw:
            header, path = raw.split(b"\t", 1)
            rows[path.decode()] = header.split()[2].decode()
    return rows


def index() -> dict[str, str]:
    rows = {}
    for raw in git("ls-files", "--stage", "-z").split(b"\0"):
        if raw:
            header, path = raw.split(b"\t", 1)
            fields = header.split()
            assert fields[2] == b"0", "unmerged index path: " + path.decode()
            rows[path.decode()] = fields[1].decode()
    return rows


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--head", help="committed candidate; default validates staged blobs")
    parser.add_argument("--output", type=Path, help="write canonical LF JSON proof")
    args = parser.parse_args()
    started = time.perf_counter()
    baseline = tree(BASE)
    actual = tree(args.head) if args.head else index()
    patch = git("diff", READER + "^", READER, "--", *TARGETS)
    rows = []
    with tempfile.TemporaryDirectory() as temporary:
        scratch = Path(temporary)
        git("init", "-q", cwd=scratch)
        for path in TARGETS:
            target = scratch / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(git("show", BASE + ":" + path))
        git("apply", "--check", cwd=scratch, data=patch)
        git("apply", cwd=scratch, data=patch)
        for path in TARGETS:
            expected = (scratch / path).read_bytes()
            blob = git("hash-object", "--stdin", data=expected).decode().strip()
            assert actual.get(path) == blob, "accepted-delta blob drift: " + path
            if not args.head:
                assert (ROOT / path).read_bytes() == expected, "worktree drift: " + path
            rows.append({"path": path, "base_blob": baseline[path],
                         "composite_blob": blob, "bytes": len(expected),
                         "sha256": hashlib.sha256(expected).hexdigest()})
    protected = {path: blob for path, blob in baseline.items()
                 if path not in TARGETS and path not in METADATA}
    for path, blob in protected.items():
        assert actual.get(path) == blob, "protected parent blob changed: " + path
    audit_path = "docs/audit/audit-log.jsonl"
    before = [json.loads(line) for line in git("show", BASE + ":" + audit_path).splitlines()]
    after_ref = args.head + ":" + audit_path if args.head else ":" + audit_path
    after = [json.loads(line) for line in git("show", after_ref).splitlines()]
    by_id = {row["id"]: row for row in after}
    assert len(by_id) == len(after), "duplicate audit id"
    assert all(by_id.get(row["id"]) == row for row in before), "parent audit entry lost/changed"
    register_path = "docs/lessons/defect-classes.md"
    register_before = git("show", BASE + ":" + register_path).splitlines()
    register_ref = args.head + ":" + register_path if args.head else ":" + register_path
    register_after = git("show", register_ref).splitlines()
    assert Counter(register_before) <= Counter(register_after), "parent register line lost/changed"
    receipt = {"schema": "cfdw-f1-composite-conservation/1", "outcome": "PASS",
               "helper_base": BASE, "reader_delta_origin": READER,
               "candidate": args.head or "staged",
               "patch_sha256": hashlib.sha256(patch).hexdigest(), "files": rows,
               "protected_parent_blobs": len(protected), "parent_audit_entries": len(before),
               "parent_register_lines": len(register_before),
               "elapsed_seconds": time.perf_counter() - started}
    if args.output:
        args.output.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n",
                               encoding="utf-8", newline="\n")
    print(json.dumps(receipt, sort_keys=True))


if __name__ == "__main__":
    main()
