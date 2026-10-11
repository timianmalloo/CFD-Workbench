#!/usr/bin/env python3
"""Reject unsafe PowerShell exit boundaries and reserved-variable assignments."""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ROOT = Path(__file__).resolve().parents[1]
BAD = re.compile(
    r"(?im)^[ \t]*&[^\r\n]*\.ps1[^\r\n]*\r?\n"
    r"[ \t]*(?:if[ \t]*\([ \t]*)?\$LASTEXITCODE\b"
)
BAD_RESERVED_ASSIGNMENT = re.compile(
    r"(?im)^[ \t]*\$(?:host|home|pid|pshome)[ \t]*(?:=|\+=|-=|\*=|/=)"
)
BAD_STRING_CONTAINS_COMPARISON = re.compile(
    r"(?is)\.Contains[ \t]*\([^)]*\bStringComparison\b"
)


def problems(files: dict[str, str]) -> list[str]:
    return [
        name
        for name, text in files.items()
        if (
            BAD.search(text)
            or BAD_RESERVED_ASSIGNMENT.search(text)
            or BAD_STRING_CONTAINS_COMPARISON.search(text)
        )
    ]


def self_test() -> None:
    planted = {
        "bad-exit.ps1": "& (Join-Path $root 'child.ps1')\nif ($LASTEXITCODE -ne 0) { exit 1 }\n",
        "bad-reserved.ps1": "$host = @{ status = 'ok' }\n",
        "bad-contains.ps1": "$text.Contains('token', [StringComparison]::Ordinal)\n",
    }
    allowed = {
        "native.ps1": "& git status\nif ($LASTEXITCODE -ne 0) { exit 1 }\n",
        "script.ps1": "& (Join-Path $root 'child.ps1')\nif (-not $?) { exit 1 }\n",
    }
    if problems(planted) != ["bad-exit.ps1", "bad-reserved.ps1", "bad-contains.ps1"] or problems(allowed):
        raise SystemExit("PowerShell boundary self-test failed")
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
            print(f"PowerShell boundary violation: {name}", file=sys.stderr)
        raise SystemExit(1)
    print("PowerShell exit-code boundaries ok")


if __name__ == "__main__":
    main()
