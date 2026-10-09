#!/usr/bin/env python3
"""Record and verify the exact bytes of this track's retained Windows captures."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
PROOF = ROOT / "docs/proof/win-store-implementation"
MANIFEST = PROOF / "capture-manifest.json"


def snapshot() -> list[dict[str, str | int]]:
    files = sorted((*PROOF.glob("*.stdout.txt"), *PROOF.glob("*.stderr.txt")))
    return [
        {
            "path": path.relative_to(ROOT).as_posix(),
            "bytes": len(data := path.read_bytes()),
            "sha256": hashlib.sha256(data).hexdigest(),
        }
        for path in files
    ]


def record() -> int:
    captures = snapshot()
    if not captures:
        print("FAIL no Windows verifier captures found", file=sys.stderr)
        return 1
    MANIFEST.write_text(json.dumps({"schema": 1, "captures": captures}, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"CAPTURE-MANIFEST recorded={len(captures)}")
    return 0


def check() -> int:
    try:
        expected = json.loads(MANIFEST.read_text(encoding="utf-8"))["captures"]
    except (OSError, ValueError, KeyError) as error:
        print(f"FAIL capture manifest unreadable: {error}", file=sys.stderr)
        return 1
    actual = snapshot()
    if actual != expected:
        print("FAIL retained capture byte count or SHA-256 differs from its manifest", file=sys.stderr)
        return 1
    for capture in actual:
        blob = subprocess.run(
            ["git", "show", f":{capture['path']}"], cwd=ROOT,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
        )
        digest = hashlib.sha256(blob.stdout).hexdigest()
        if blob.returncode != 0 or len(blob.stdout) != capture["bytes"] or digest != capture["sha256"]:
            print(f"FAIL committed blob differs: {capture['path']}", file=sys.stderr)
            return 1
        print(f"PASS {capture['path']} bytes={capture['bytes']} sha256={digest} worktree=index")
    print(f"CAPTURE-INTEGRITY PASS captures={len(actual)}")
    return 0


def main() -> int:
    if sys.argv[1:] == ["--record"]:
        return record()
    if sys.argv[1:]:
        print("FAIL usage: capture-integrity.py [--record]", file=sys.stderr)
        return 1
    return check()


if __name__ == "__main__":
    raise SystemExit(main())
