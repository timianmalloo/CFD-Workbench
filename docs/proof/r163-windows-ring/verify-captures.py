#!/usr/bin/env python3
"""Verify receipt-declared capture hashes and sizes against committed Git blobs."""
import argparse
import hashlib
import json
import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parents[3]
MANIFEST = pathlib.Path(__file__).with_name("capture-manifest.json")
PREFIX = "docs/proof/r163-windows-ring/"


def committed_blob(path):
    if not path.startswith(PREFIX) or ".." in pathlib.PurePosixPath(path).parts:
        raise ValueError(f"capture path is outside proof folder: {path}")
    return subprocess.run(
        ["git", "show", f"HEAD:{path}"],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
    ).stdout


def mismatches(records):
    problems = []
    for record in records:
        path = record["path"]
        data = committed_blob(path)
        actual_bytes = len(data)
        actual_sha = hashlib.sha256(data).hexdigest()
        if actual_bytes != record["bytes"]:
            problems.append(
                f"{path}: bytes declared {record['bytes']}, committed {actual_bytes}"
            )
        if actual_sha != record["sha256"]:
            problems.append(f"{path}: SHA-256 declaration differs from committed blob")
    return problems


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8-sig"))
    tested_head = manifest["tested_head"]
    subprocess.run(
        ["git", "merge-base", "--is-ancestor", tested_head, "HEAD"],
        cwd=ROOT,
        check=True,
    )
    records = manifest["files"]
    problems = mismatches(records)
    if problems:
        print("capture integrity FAIL")
        print("\n".join(problems))
        return 1
    if args.self_test:
        planted_bytes = dict(records[0])
        planted_bytes["bytes"] += 1
        planted_sha = dict(records[0])
        planted_sha["sha256"] = "0" * 64
        if not mismatches([planted_bytes]) or not mismatches([planted_sha]):
            print("capture integrity self-test FAIL")
            return 1
        print("capture integrity self-test PASS: planted byte-count and SHA-256 mismatches rejected")
    print(f"capture integrity PASS: {len(records)} committed blob(s) match SHA-256 and byte count")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (KeyError, ValueError, subprocess.CalledProcessError) as exc:
        print(f"capture integrity FAIL: {exc}")
        raise SystemExit(1)
