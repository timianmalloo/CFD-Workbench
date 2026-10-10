#!/usr/bin/env python3
"""Bind the Ruling 197 G1 proof manifest to clean blobs already committed at HEAD."""
from __future__ import annotations

import hashlib
import json
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
FOLDER = Path(__file__).resolve().parent
MANIFEST = FOLDER / "capture-manifest.json"


def committed_bytes(path: Path) -> bytes:
    relative = path.relative_to(ROOT).as_posix()
    tracked = subprocess.run(
        ["git", "ls-files", "--error-unmatch", "--", relative],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        check=False,
    )
    if tracked.returncode != 0:
        raise RuntimeError(f"proof input is not committed: {relative}")
    clean = subprocess.run(
        ["git", "diff", "--quiet", "HEAD", "--", relative],
        cwd=ROOT,
        check=False,
    )
    if clean.returncode != 0:
        raise RuntimeError(f"proof input differs from HEAD: {relative}")
    return subprocess.check_output(["git", "show", f"HEAD:{relative}"], cwd=ROOT)


def main() -> None:
    paths = sorted(path for path in FOLDER.iterdir() if path.is_file() and path != MANIFEST)
    files = []
    for path in paths:
        raw = committed_bytes(path)
        files.append(
            {
                "path": path.relative_to(ROOT).as_posix(),
                "bytes": len(raw),
                "sha256": hashlib.sha256(raw).hexdigest(),
            }
        )
    document = {
        "algorithm": "SHA-256",
        "capture_scope": "Ruling 197 G1 exact-source and installed-library read-only inspection",
        "files": files,
    }
    MANIFEST.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
