#!/usr/bin/env python3
"""Reject $LASTEXITCODE checks immediately after direct PowerShell-script calls."""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BAD = re.compile(
    r"(?im)^[ \t]*&[^\r\n]*\.ps1[^\r\n]*\r?\n"
    r"[ \t]*(?:if[ \t]*\([ \t]*)?\$LASTEXITCODE\b"
)


def problems(files: dict[str, str]) -> list[str]:
    return [name for name, text in files.items() if BAD.search(text)]


def self_test() -> None:
    planted = {"bad.ps1": "& (Join-Path $root 'child.ps1')\nif ($LASTEXITCODE -ne 0) { exit 1 }\n"}
    allowed = {
        "native.ps1": "& git status\nif ($LASTEXITCODE -ne 0) { exit 1 }\n",
        "script.ps1": "& (Join-Path $root 'child.ps1')\nif (-not $?) { exit 1 }\n",
    }
    if problems(planted) != ["bad.ps1"] or problems(allowed):
        raise SystemExit("POWERSHELL-LASTEXITCODE-A self-test failed")
    print("PowerShell exit-code self-test OK")


def tracked_ps1() -> dict[str, str]:
    result = subprocess.run(
        ["git", "ls-files", "*.ps1"], cwd=ROOT, check=True, capture_output=True, text=True, encoding="utf-8"
    )
    files = {}
    for name in result.stdout.splitlines():
        path = ROOT / name
        files[name] = path.read_text(encoding="utf-8")
    return files


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return
    found = problems(tracked_ps1())
    if found:
        for name in found:
            print(f"POWERSHELL-LASTEXITCODE-A: {name}", file=sys.stderr)
        raise SystemExit(1)
    print("PowerShell exit-code boundaries ok")


if __name__ == "__main__":
    main()
