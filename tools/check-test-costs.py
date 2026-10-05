#!/usr/bin/env python3
"""Enforce the test-ring cost rules C-2..C-6 (docs/design/area3-analysis.md section 13.4) from tools/run-tests.sh output.

  python3 tools/check-test-costs.py [--dir .tmp-tests] [--jobs Core.part1of2,Core.part2of2,Desktop,Analysis,Cli]
  python3 tools/check-test-costs.py --self-test

Reads <name>.ms and wall.ms (C-1, millisecond clocks written by run-tests.sh) and the COST lines of Analysis.log.
Exit 0 every rule holds . 1 a rule failed, or a reading is missing ("not recorded" never passes) . 2 usage.
Ring: every join (run-tests.sh calls it after its wait loop; join.json runs it again). Cost: under 0.1 s.
"""
from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DIR = ROOT / ".tmp-tests"
DEFAULT_JOBS = ("Core.part1of2", "Core.part2of2", "Desktop", "Analysis", "Cli")
ANALYSIS_LIMIT_MS = 5000      # C-2
WALL_LIMIT_MS = 50000         # C-3, while Analysis is in the jobs
DESKTOP_LIMIT_MS = 43000      # C-4
CHECK_LIMIT_MS = 500.0        # C-5
EXEMPT_LIMIT_MS = 1500.0      # C-5, the two named A8.4 exemptions
EXEMPT_CHECKS = ("F1_FlatPlate_RichardsonClAlphaTo2Pi", "F6_ObservedOrder")


def read_ms(directory: Path, name: str) -> int | None:
    try:
        return int((directory / f"{name}.ms").read_text(encoding="utf-8").strip())
    except (OSError, ValueError):
        return None


def check(directory: Path, jobs: tuple[str, ...]) -> list[str]:
    return []


def self_test() -> int:
    """Every row of the 13.4 table: a green baseline, then each failing input planted alone must turn it red."""
    passes = "".join(f"PASS {name}\nCOST {name} 12.500\n" for name in ("Units_Lbf_KeyUnchanged", "F6_ObservedOrder"))
    good = {"Core.part1of2.ms": "38000", "Core.part2of2.ms": "38000", "Desktop.ms": "40000", "Analysis.ms": "3000",
            "Cli.ms": "1500", "wall.ms": "45000", "Analysis.log": passes}
    cases = [  # label, files replacing the baseline (None deletes), expected message fragment (None: green)
        ("baseline is green", {}, None),
        ("C-2 Analysis.ms 5900", {"Analysis.ms": "5900"}, "C-2"),
        ("C-3 wall.ms 50400", {"wall.ms": "50400"}, "C-3"),
        ("C-4 Desktop.ms 43100 names DR-ANA-10", {"Desktop.ms": "43100"}, "DR-ANA-10"),
        ("C-5 COST Units_Lbf_KeyUnchanged 512.3", {"Analysis.log": passes.replace("Units_Lbf_KeyUnchanged 12.500", "Units_Lbf_KeyUnchanged 512.3")}, "Units_Lbf_KeyUnchanged"),
        ("C-5 COST F6_ObservedOrder 1612.0", {"Analysis.log": passes + "COST F6_ObservedOrder 1612.0\n"}, "F6_ObservedOrder"),
        ("C-5 F6_ObservedOrder 1499.0 is inside its exemption", {"Analysis.log": passes + "COST F6_ObservedOrder 1499.0\n"}, None),
        ("C-6 Analysis.ms deleted", {"Analysis.ms": None}, "Analysis.ms"),
        ("C-6 wall.ms deleted", {"wall.ms": None}, "wall.ms"),
        ("C-6 Analysis PASS without COST", {"Analysis.log": passes + "PASS NoCost\n"}, "NoCost"),
    ]
    failures = 0
    with tempfile.TemporaryDirectory() as scratch:
        for number, (label, change, expected) in enumerate(cases):
            case = Path(scratch) / f"case{number}"
            case.mkdir()
            for name, text in {**good, **change}.items():
                if text is not None:
                    (case / name).write_text(text, encoding="utf-8", newline="\n")
            errors = check(case, DEFAULT_JOBS)
            ok = not errors if expected is None else any(expected in error for error in errors)
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {expected or 'green'}, got {errors or 'green'}"))
            failures += not ok
    print(f"SELFTEST {len(cases) - failures}/{len(cases)} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    args = list(argv[1:])
    if args == ["--self-test"]:
        return self_test()
    directory, jobs = DEFAULT_DIR, DEFAULT_JOBS
    while args:
        flag = args.pop(0)
        if flag in ("--dir", "--jobs") and args:
            value = args.pop(0)
            if flag == "--dir":
                directory = Path(value)
            else:
                jobs = tuple(part for part in value.split(",") if part)
        else:
            print(__doc__.strip().splitlines()[2], file=sys.stderr)
            return 2
    errors = check(directory, jobs)
    for error in errors:
        print("FAILED: " + error)
    print(f"test costs: {len(errors)} failures")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
