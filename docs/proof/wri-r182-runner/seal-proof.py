#!/usr/bin/env python3
"""Seal staged proof bytes; committed-blob verification runs after local commit.

Ring: runner preparation packaging. Cost: one Git read per staged evidence file.
Manifests exclude themselves to avoid a recursive hash dependency.
"""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
FOLDER = "docs/proof/wri-r182-runner"
TOOLS = ["tools/windows-runner.ps1", "tools/windows-settings-preflight.ps1",
         "tools/test-windows-runner.ps1", "tools/check-windows-runner.py"]


def git(*args: str) -> bytes:
    return subprocess.run(["git", *args], cwd=ROOT, check=True,
                          capture_output=True, timeout=30).stdout


def records(paths: list[str]) -> list[dict]:
    result = []
    for path in sorted(paths):
        blob = git("show", ":" + path)
        result.append({"path": path, "bytes": len(blob),
                       "sha256": hashlib.sha256(blob).hexdigest()})
    return result


def main() -> None:
    paths = git("ls-files", "--", FOLDER).decode().splitlines()
    paths = [path for path in paths
             if not path.endswith(("/capture-manifest.json", "/closing-manifest.json"))]
    if not paths:
        raise RuntimeError("no staged proof")
    for name, entries in (("capture", records(paths)),
                          ("closing", records(paths + TOOLS))):
        target = ROOT / FOLDER / (name + "-manifest.json")
        target.write_text(json.dumps({"files": entries}, indent=2) + "\n",
                          encoding="utf-8", newline="\n")
        print(f"SEALED {name} staged_blobs={len(entries)}")


if __name__ == "__main__":
    main()
