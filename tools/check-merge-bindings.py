#!/usr/bin/env python3
"""REGISTER-CLASS-MISMATCH: every merge-driver binding in .gitattributes matches its file's format.

A path bound to merge=coord-register must parse as JSONL at HEAD (blank lines allowed).
A path bound to merge=defect-register must be a markdown file.
Glob patterns expand to the tracked files. Ring: fast (every push, run by check-docs.py).
Cost: one `git ls-files` plus one `git show` per bound file, about 0.1 s.
"""

import json
from pathlib import Path
import subprocess
import sys

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]


def git(*args):
    return subprocess.run(["git", "-C", str(ROOT), *args], capture_output=True, text=True,
                          encoding="utf-8", check=True).stdout


def bindings():
    """[(pattern, driver)] for every `merge=<driver>` line."""
    found = []
    for line in (ROOT / ".gitattributes").read_text(encoding="utf-8").splitlines():
        parts = line.split()
        if len(parts) < 2 or parts[0].startswith("#"):
            continue
        for attr in parts[1:]:
            if attr.startswith("merge="):
                found.append((parts[0], attr[len("merge="):]))
    return found


def jsonl_problem(path):
    for number, line in enumerate(git("show", "HEAD:" + path).splitlines(), start=1):
        if not line.strip():
            continue
        try:
            json.loads(line)
        except ValueError:
            return "{}: bound to merge=coord-register but line {} is not JSON".format(path, number)
    return None


def problems():
    found = []
    for pattern, driver in bindings():
        if driver not in ("coord-register", "defect-register"):
            continue
        files = git("ls-files", "--", ":(glob)" + pattern).splitlines()
        for path in files:
            if driver == "coord-register":
                bad = jsonl_problem(path)
            else:
                bad = None if path.endswith(".md") else "{}: bound to merge=defect-register but is not markdown".format(path)
            if bad:
                found.append(bad)
    return found


def main():
    found = problems()
    if found:
        print("MERGE-BINDING: " + "; ".join(found), file=sys.stderr)
        return 1
    print("merge bindings ok: every coord-register path parses as JSONL, every defect-register path is markdown", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
