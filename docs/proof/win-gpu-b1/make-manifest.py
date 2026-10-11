#!/usr/bin/env python3
"""Bind staged Git blobs for the B1 proof without self-referencing the manifest."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess


PROOF_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PROOF_ROOT.parents[2]
OUTPUT = PROOF_ROOT / "capture-manifest.json"
EXCLUDED_PARTS = {".source-cache", "__pycache__"}


def main() -> None:
    proof_relative = PROOF_ROOT.relative_to(REPO_ROOT).as_posix()
    listed = subprocess.run(
        ["git", "ls-files", "--cached", "--", proof_relative],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    staged_paths = {line for line in listed.stdout.splitlines() if line}
    filesystem_paths = {
        path.relative_to(REPO_ROOT).as_posix()
        for path in PROOF_ROOT.rglob("*")
        if path.is_file()
        and path != OUTPUT
        and not any(part in EXCLUDED_PARTS for part in path.parts)
    }
    unstaged = sorted(filesystem_paths - staged_paths)
    if unstaged:
        raise SystemExit("proof files must be staged before manifest generation: " + ", ".join(unstaged))

    files = []
    for relative in sorted(staged_paths):
        path = REPO_ROOT / relative
        if path == OUTPUT or any(part in EXCLUDED_PARTS for part in path.parts):
            continue
        blob = subprocess.run(
            ["git", "show", f":{relative}"],
            cwd=REPO_ROOT,
            check=True,
            capture_output=True,
        )
        raw = blob.stdout
        files.append(
            {
                "path": relative,
                "bytes": len(raw),
                "sha256": hashlib.sha256(raw).hexdigest(),
            }
        )
    document = {
        "algorithm": "SHA-256",
        "capture_scope": "Rulings 199-200 Windows GPU B1 blocked proof",
        "files": files,
    }
    OUTPUT.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
