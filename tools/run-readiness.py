#!/usr/bin/env python3
"""Run the readiness ring from docs/coordination/join.json and record a receipt for HEAD.

The join's fast ring (checks + gates) runs on every join. The readiness ring holds the slow,
GUI-launching and publish-heavy gates and the spike recounts, and runs before a merge to main
or a milestone close (docs/reviews/test-ci-waste.md). The receipt makes that ring checkable:

  python3 tools/run-readiness.py            run the ring on a clean tree, write the receipt
  python3 tools/run-readiness.py --check    exit 0 only if a green receipt names this HEAD
  python3 tools/run-readiness.py --self-test

Exit 0 green · 1 a command failed, or --check found no green receipt for HEAD · 2 usage.
"""
from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import time
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]


def git(root: Path, *arguments: str) -> str:
    return subprocess.run(["git", *arguments], cwd=root, check=True, capture_output=True,
                          text=True, encoding="utf-8", errors="replace").stdout.strip()


def run_ring(root: Path, join: Path, receipt: Path) -> int:
    if git(root, "status", "--porcelain"):
        print("run-readiness: the tree is not clean; commit first so the receipt names what ran")
        return 1
    ring = json.loads(join.read_text(encoding="utf-8")).get("readiness") or []
    if not ring:
        print("run-readiness: {0} has no `readiness` commands".format(join))
        return 2
    head, results = git(root, "rev-parse", "HEAD"), []
    for command in ring:
        argv = [sys.executable if c in ("python3", "python") else c for c in command]
        started = time.monotonic()
        code = subprocess.run(argv, cwd=root).returncode
        results.append({"command": command, "exit": code, "seconds": round(time.monotonic() - started, 1)})
        print("run-readiness: {0} {1:6.1f} s  {2}".format("ok  " if code == 0 else "FAIL",
                                                         results[-1]["seconds"], " ".join(command)))
    green = all(item["exit"] == 0 for item in results)
    receipt.parent.mkdir(parents=True, exist_ok=True)
    receipt.write_text(json.dumps({"head": head, "green": green, "results": results}, indent=2) + "\n",
                       encoding="utf-8", newline="\n")
    print("run-readiness: {0} for {1} (receipt {2})".format("GREEN" if green else "RED", head[:9], receipt))
    return 0 if green else 1


def check(root: Path, receipt: Path) -> int:
    head = git(root, "rev-parse", "HEAD")
    try:
        data = json.loads(receipt.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        print("run-readiness: no readiness receipt; run python3 tools/run-readiness.py")
        return 1
    if data.get("head") != head or data.get("green") is not True:
        print("run-readiness: the receipt is {0} for {1}, not green for HEAD {2}".format(
            "green" if data.get("green") else "red", str(data.get("head"))[:9], head[:9]))
        return 1
    print("run-readiness: green receipt for HEAD {0}".format(head[:9]))
    return 0


def self_test() -> int:
    """A red command makes the ring red, a green ring passes --check, and a new commit
    makes the old receipt stale."""
    problems = []
    with tempfile.TemporaryDirectory() as directory:
        repo = Path(directory)
        for command in (["init", "-q"], ["config", "user.email", "t@example.com"], ["config", "user.name", "T"]):
            git(repo, *command)
        join, receipt = repo / "join.json", repo / ".tmp" / "readiness.json"
        (repo / ".gitignore").write_text(".tmp/\n", encoding="utf-8", newline="\n")
        join.write_text(json.dumps({"readiness": [["python3", "-c", "raise SystemExit(1)"]]}),
                        encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-q", "-m", "red")
        if run_ring(repo, join, receipt) != 1 or check(repo, receipt) != 1:
            problems.append("a red readiness command was not reported red")
        join.write_text(json.dumps({"readiness": [["python3", "-c", "print('ok')"]]}),
                        encoding="utf-8", newline="\n")
        git(repo, "commit", "-q", "-am", "green")
        if run_ring(repo, join, receipt) != 0 or check(repo, receipt) != 0:
            problems.append("a green ring did not pass --check")
        (repo / "later.txt").write_text("x\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-q", "-m", "later")
        if check(repo, receipt) != 1:
            problems.append("a receipt for an older HEAD passed --check")
    for problem in problems:
        print("self-test FAIL: " + problem)
    print("self-test OK" if not problems else "self-test FAILED")
    return 1 if problems else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    receipt = ROOT / ".tmp-tests" / "readiness.json"
    if argv == ["--check"]:
        return check(ROOT, receipt)
    if argv:
        print(__doc__)
        return 2
    return run_ring(ROOT, ROOT / "docs" / "coordination" / "join.json", receipt)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
