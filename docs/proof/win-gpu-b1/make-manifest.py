#!/usr/bin/env python3
"""Bind the committed-shape B1 proof files without self-referencing the manifest."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path


PROOF_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PROOF_ROOT.parents[2]
OUTPUT = PROOF_ROOT / "capture-manifest.json"
EXCLUDED_PARTS = {".source-cache", "__pycache__"}


def main() -> None:
    files = []
    for path in sorted(PROOF_ROOT.rglob("*")):
        if not path.is_file() or path == OUTPUT or any(part in EXCLUDED_PARTS for part in path.parts):
            continue
        raw = path.read_bytes()
        files.append(
            {
                "path": path.relative_to(REPO_ROOT).as_posix(),
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
