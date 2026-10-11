#!/usr/bin/env python3
"""Bind every staged B2 proof blob without self-referencing the manifest."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess


PROOF_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PROOF_ROOT.parents[2]
OUTPUT = PROOF_ROOT / "capture-manifest.json"
EXCLUDED_PARTS = {".source-cache", "__pycache__", "download-cache"}


def git_lines(*args: str) -> set[str]:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    return {line for line in result.stdout.splitlines() if line}


def main() -> None:
    proof_relative = PROOF_ROOT.relative_to(REPO_ROOT).as_posix()
    staged_paths = git_lines("ls-files", "--cached", "--", proof_relative)
    untracked = git_lines("ls-files", "--others", "--exclude-standard", "--", proof_relative)
    untracked.discard(OUTPUT.relative_to(REPO_ROOT).as_posix())
    if untracked:
        raise SystemExit(
            "proof files must be staged before manifest generation: " + ", ".join(sorted(untracked))
        )

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
        ).stdout
        files.append(
            {
                "path": relative,
                "bytes": len(blob),
                "sha256": hashlib.sha256(blob).hexdigest(),
            }
        )
    document = {
        "algorithm": "SHA-256",
        "capture_scope": "Rulings 200 and 209 Windows GPU B2 execution proof",
        "files": files,
    }
    OUTPUT.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
