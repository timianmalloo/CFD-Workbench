#!/usr/bin/env python3
"""Fail when Debug and Release builds could run different code (Test Architect condition C1, test-cost L1/L2).

The readiness gates no longer run the full Core and Desktop suites in Debug; the fast ring runs them in
Release every join. That is safe only while no source line and no project property differs by
configuration. This check fails on:

  - `#if DEBUG` (or `#elif DEBUG`, `#if !DEBUG`), `Debug.Assert`, `[Conditional("DEBUG")]` in src/ or tests/ C#;
  - `Optimize`, `CheckForOverflowUnderflow` or `DefineConstants` set per configuration (inside an element whose
    `Condition` names `Configuration`) in any csproj/props/targets under src/, tests/ or the repository root.

Exit 0 clean · 1 a finding (each printed as DEBUG-PARITY <path>:<line> <reason>).
Run by tools/check-docs.py. Cost: a few ms.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
CODE = re.compile(r"^\s*#\s*(?:el)?if\b.*\bDEBUG\b|\bDebug\s*\.\s*Assert\b|Conditional\s*\(\s*\"DEBUG\"\s*\)")
PROPERTY = re.compile(r"<(Optimize|CheckForOverflowUnderflow|DefineConstants)\b")
CONDITION = re.compile(r"<(\w+)\b[^>]*\bCondition\s*=\s*\"[^\"]*Configuration[^\"]*\"[^>]*>")


def code_findings(text: str) -> list[tuple[int, str]]:
    return [(number, "Debug-only code: " + line.strip()) for number, line in enumerate(text.splitlines(), 1)
            if CODE.search(line)]


def project_findings(text: str) -> list[tuple[int, str]]:
    """A configuration-dependent property, whether the Condition sits on it or on an enclosing element."""
    findings, open_conditions = [], []  # stack of element names opened with a Configuration condition
    for number, line in enumerate(text.splitlines(), 1):
        for match in CONDITION.finditer(line):
            if not match.group(0).endswith("/>"):
                open_conditions.append(match.group(1))
        property_match = PROPERTY.search(line)
        if property_match and (open_conditions or CONDITION.search(line)):
            findings.append((number, f"per-configuration <{property_match.group(1)}>: " + line.strip()))
        for name in list(open_conditions):
            if f"</{name}>" in line:
                open_conditions.remove(name)
    return findings


def self_test() -> list[str]:
    """Each pattern is red on a planted sample and green on its look-alike, so a regex edit cannot go blind."""
    red_code = ["#if DEBUG", "  #elif DEBUG && X", "#if !DEBUG", "Debug.Assert(x > 0);",
                "[Conditional(\"DEBUG\")] void F() {}", "System.Diagnostics.Debug.Assert(ok);"]
    green_code = ["// DebugAssert is not a call", "var debugging = true;", "#if NET10_0", "Debugger.Break();"]
    red_project = ['<PropertyGroup Condition="\'$(Configuration)\'==\'Debug\'">\n<Optimize>false</Optimize>\n</PropertyGroup>',
                   '<DefineConstants Condition="\'$(Configuration)\'==\'Release\'">X</DefineConstants>',
                   '<PropertyGroup Condition=" \'$(Configuration)|$(Platform)\' == \'Release|AnyCPU\' ">\n'
                   '<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>\n</PropertyGroup>']
    green_project = ['<PropertyGroup>\n<Optimize>true</Optimize>\n</PropertyGroup>',
                     '<PropertyGroup Condition="\'$(Configuration)\'==\'Debug\'">\n<Foo>1</Foo>\n</PropertyGroup>\n'
                     '<PropertyGroup>\n<DefineConstants>X</DefineConstants>\n</PropertyGroup>']
    problems = [f"missed {s!r}" for s in red_code if not code_findings(s)]
    problems += [f"false positive {s!r}" for s in green_code if code_findings(s)]
    problems += [f"missed {s!r}" for s in red_project if not project_findings(s)]
    problems += [f"false positive {s!r}" for s in green_project if project_findings(s)]
    return problems


def main() -> int:
    problems = self_test()
    for problem in problems:
        print("DEBUG-PARITY self-test " + problem)
    if problems:
        return 1
    findings = []
    for directory in ("src", "tests"):
        for path in sorted((ROOT / directory).rglob("*.cs")):
            if not {"bin", "obj"} & set(path.relative_to(ROOT).parts):
                findings += [(path, n, r) for n, r in code_findings(path.read_text(encoding="utf-8", errors="replace"))]
    projects = [p for d in ("src", "tests") for pattern in ("*.csproj", "*.props", "*.targets")
                for p in (ROOT / d).rglob(pattern) if not {"bin", "obj"} & set(p.relative_to(ROOT).parts)]
    projects += [p for pattern in ("*.props", "*.targets") for p in ROOT.glob(pattern)]
    for path in sorted(projects):
        findings += [(path, n, r) for n, r in project_findings(path.read_text(encoding="utf-8", errors="replace"))]
    for path, number, reason in findings:
        print(f"DEBUG-PARITY {path.relative_to(ROOT).as_posix()}:{number} {reason}")
    if findings:
        print("DEBUG-PARITY: Debug and Release would run different code, and readiness no longer runs the full suites "
              "in Debug (docs/plans/test-cost.md L1/L2). Remove the configuration dependence, or restore the Debug runs.")
        return 1
    print(f"debug parity ok: no Debug-only code or per-configuration compile property in src/ or tests/ "
          f"({len(projects)} project files)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
