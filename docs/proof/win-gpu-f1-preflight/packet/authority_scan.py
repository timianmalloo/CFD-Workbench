#!/usr/bin/env python3
"""Static Ruling 217 command boundary; only this file holds planted negatives."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCAN_SUFFIXES = {".py", ".ps1", ".sh"}
DENIED = (
    ("package-install", re.compile(r"\bdpkg\s+(?:-i\b|--install\b)")),
    ("source-download", re.compile(r"(?:https?://|git@)[^\s'\"]*(?:petsc4foam|petsc[-_/]3\.|petsc\.org[^\s'\"]*(?:download|tar))", re.I)),
    ("compiler-build-case", re.compile(r"^\s*(?:\&\s*)?(?:gcc|g\+\+|gfortran|nvcc|cc|c\+\+|make|cmake|ninja|wmake|simpleFoam|pimpleFoam|mpirun|mpiexec)\b(?:\s|$)", re.I)),
    ("compiler-build-case", re.compile(r"['\"](?:gcc|g\+\+|gfortran|nvcc|make|cmake|ninja|wmake|simpleFoam|pimpleFoam|mpirun|mpiexec)['\"]", re.I)),
    ("source-checkout", re.compile(r"\bgit\s+clone\b", re.I)),
    ("global-wsl-or-resize", re.compile(r"\bwsl(?:\.exe)?\b[^\n]*(?:--shutdown|--manage[^\n]*--resize)", re.I)),
)
APT = re.compile(r"\bapt-get\b[^\n]*\b(install|download)\b", re.I)


def source_lines(path: Path) -> list[str]:
    lines = []
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.lstrip()
        if stripped.startswith("#"):
            continue
        lines.append(line)
    return lines


def findings(line: str) -> list[str]:
    found = [name for name, pattern in DENIED if pattern.search(line)]
    for segment in re.split(r"\s*(?:;|&&|\|\|)\s*", line):
        apt = APT.search(segment)
        if apt and apt.group(1).lower() == "install":
            if "--no-install-recommends" not in segment or not any(
                    marker in segment for marker in ("--simulate", "--print-uris")):
                found.append("package-install")
    return sorted(set(found))


def self_test() -> None:
    # The following fixtures are the sole source-file allowlist for prohibited command text.
    negatives = (
        "dpkg -i a.deb", "dpkg --install a.deb", "apt-get install x",
        "apt-get --download-only install x", "https://petsc.org/release/install/download/petsc-3.tar.gz",
        "https://gitlab.com/petsc/petsc4foam.git", "gcc x.c", "make all",
        "simpleFoam -case one", "wsl.exe --shutdown", "wsl --manage target --resize 8GB",
        "apt-get --simulate --no-install-recommends install x; apt-get install y",
        "subprocess.run(['make', 'all'])", "git clone https://example.invalid/anything",
    )
    for line in negatives:
        if not findings(line):
            raise AssertionError(f"planted forbidden command passed: {line}")
    positives = (
        'apt-get --simulate --no-install-recommends install "${roots[@]}"',
        'apt-get --print-uris --yes --no-install-recommends install "${roots[@]}"',
        'apt-get download "${exact[@]}"', 'dpkg-deb --field x.deb Package',
    )
    for line in positives:
        if findings(line):
            raise AssertionError(f"authorized command rejected: {line}")
    print("authority scan planted-negative self-test: PASS")


def main() -> None:
    self_test()
    errors = []
    for path in sorted(ROOT.rglob("*")):
        if path.suffix not in SCAN_SUFFIXES or path.name == Path(__file__).name:
            continue
        for number, line in enumerate(source_lines(path), 1):
            for finding in findings(line):
                errors.append(f"{path.name}:{number}: {finding}: {line.strip()}")
    if errors:
        raise ValueError("\n".join(errors))
    print("authority scan packet: PASS")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, AssertionError) as error:
        print(f"authority scan: {error}", file=sys.stderr)
        raise SystemExit(5)
