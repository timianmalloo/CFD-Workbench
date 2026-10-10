#!/usr/bin/env python3
"""Write the proof-local capture manifest after the receipt is final."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
FOLDER = Path(__file__).resolve().parent
MANIFEST = FOLDER / "capture-manifest.json"


def main() -> None:
    paths = sorted(path for path in FOLDER.iterdir() if path.is_file() and path != MANIFEST)
    files = []
    for path in paths:
        raw = path.read_bytes()
        files.append(
            {
                "path": path.relative_to(ROOT).as_posix(),
                "bytes": len(raw),
                "sha256": hashlib.sha256(raw).hexdigest(),
            }
        )
    document = {
        "algorithm": "SHA-256",
        "capture_scope": "Ruling 191 read-only Windows and WSL GPU qualification inventory",
        "files": files,
    }
    MANIFEST.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
