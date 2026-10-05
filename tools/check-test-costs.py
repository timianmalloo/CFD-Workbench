#!/usr/bin/env python3
"""Enforce the test-ring cost rules C-2..C-6 (docs/design/area3-analysis.md section 13.4) from tools/run-tests.sh output.

  python3 tools/check-test-costs.py [--dir .tmp-tests] [--jobs Core.part1of2,Core.part2of2,Desktop,Analysis,Cli] [--load <1-minute load>]
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

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DIR = ROOT / ".tmp-tests"
DEFAULT_JOBS = ("Core.part1of2", "Core.part2of2", "Desktop", "Analysis", "Cli")
ANALYSIS_LIMIT_MS = 5000      # C-2
# Ruling 84 (DR-RING-1, OD-2 fallback b in Ruling 81's load-gated shape). The section 13.4 absolute limits (wall 50000 ms,
# Desktop 43000 ms) were not met by the measured base, so C-3 and C-4 are limits on the base plus 2 s:
#   C-3 measures wall.ms - build.ms (net ring time, while Analysis is in the jobs); base 52 s; limit 54000 ms.
#   C-4 measures Desktop.ms; base 51 s (the quiet maximum); limit 53000 ms.
# The bases come from the recorded 3-run quiet baseline BASELINE_CSV. They change only from a new recorded 3-run quiet
# baseline, never raised unmeasured. When Track B2 lands, re-measure quiet; if Desktop <= 43 s and wall <= 50 s, revert to
# the section 13.4 absolute limits and remove the deltas (docs/plans/test-cost.md section 9).
BASELINE_CSV = "docs/proof/ring-oct05/baseline-2026-10-05.csv"
WALL_NET_BASE_MS = 52000
DESKTOP_BASE_MS = 51000
DELTA_MS = 2000
WALL_LIMIT_MS = WALL_NET_BASE_MS + DELTA_MS   # C-3
DESKTOP_LIMIT_MS = DESKTOP_BASE_MS + DELTA_MS  # C-4
# C-3 and C-4 fail only when the 1-minute load at ring end is at or below this; above it, or not recorded, they print
# COST-MISS (Ruling 84). C-2, C-5 and C-6 are never gated.
LOAD_GATE = 24.0
CHECK_LIMIT_MS = 500.0        # C-5
EXEMPT_LIMIT_MS = 1500.0      # C-5, the two named A8.4 exemptions
EXEMPT_CHECKS = ("F1_FlatPlate_RichardsonClAlphaTo2Pi", "F6_ObservedOrder")


def read_ms(directory: Path, name: str) -> int | None:
    try:
        return int((directory / f"{name}.ms").read_text(encoding="utf-8").strip())
    except (OSError, ValueError):
        return None


def check(directory: Path, jobs: tuple[str, ...], load: str = "not-recorded") -> tuple[list[str], list[str]]:
    errors: list[str] = []
    misses: list[str] = []
    readings: dict[str, int] = {}
    for name in (*jobs, "wall", "build"):  # C-6: a reading that is missing is a failure, never a pass
        value = read_ms(directory, name)
        if value is None:
            errors.append(f"C-6 {name}.ms is missing or unreadable: not recorded (run tools/run-tests.sh)")
        else:
            readings[name] = value
    if readings.get("Analysis", 0) > ANALYSIS_LIMIT_MS:
        errors.append(f"C-2 Analysis took {readings['Analysis']} ms, over {ANALYSIS_LIMIT_MS} ms")
    try:
        gated = float(load) <= LOAD_GATE
    except ValueError:
        gated = False  # "not recorded" never passes and never fails a load-gated rule

    def timing(rule: str, ms: int, limit: int, message: str) -> None:
        if ms <= limit:
            return
        if gated:
            errors.append(message)
        else:
            misses.append(f"COST-MISS {rule} {ms} load {load}")

    if "Analysis" in jobs and "wall" in readings and "build" in readings:
        net = readings["wall"] - readings["build"]
        timing("C-3", net, WALL_LIMIT_MS, f"C-3 run-tests net wall {net} ms (wall - build), over {WALL_LIMIT_MS} ms (Ruling 84)")
    if "Desktop" in readings:
        timing("C-4", readings["Desktop"], DESKTOP_LIMIT_MS, f"C-4 Desktop took {readings['Desktop']} ms, over "
               f"{DESKTOP_LIMIT_MS} ms: DR-ANA-10 applies (spread or split the Desktop job)")
    if "Analysis" in jobs:
        try:
            lines = (directory / "Analysis.log").read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            errors.append("C-6 Analysis.log is missing: its COST lines are not recorded")
            return errors, misses
        costs: dict[str, float] = {}
        for line in lines:
            match = re.fullmatch(r"COST (\S+) (\d+(?:\.\d+)?)", line.strip())
            if match:
                costs[match.group(1)] = float(match.group(2))
        for name in (line[5:].strip() for line in lines if line.startswith("PASS ")):
            if name not in costs:
                errors.append(f"C-6 {name} printed PASS but no COST line: not recorded")
        for name, ms in costs.items():
            limit = EXEMPT_LIMIT_MS if name in EXEMPT_CHECKS else CHECK_LIMIT_MS
            if ms > limit:
                errors.append(f"C-5 {name} took {ms} ms, over {limit:.0f} ms (move it to readiness with its cost, or make it cheaper)")
    return errors, misses


def self_test() -> int:
    """Every row of the 13.4 table plus Ruling 84: a green baseline, then each failing input planted alone must turn it red."""
    passes = "".join(f"PASS {name}\nCOST {name} 12.500\n" for name in ("Units_Lbf_KeyUnchanged", "F6_ObservedOrder"))
    good = {"Core.part1of2.ms": "38000", "Core.part2of2.ms": "38000", "Desktop.ms": "40000", "Analysis.ms": "3000",
            "Cli.ms": "1500", "wall.ms": "45000", "build.ms": "1000", "Analysis.log": passes}
    quiet = "5.0"
    # label, files replacing the baseline (None deletes), end load, expected error fragment (None: no error),
    # COST-MISS fragments that must be printed (an empty tuple: none)
    cases = [
        ("baseline is green", {}, quiet, None, ()),
        ("C-2 Analysis.ms 5900", {"Analysis.ms": "5900"}, quiet, "C-2", ()),
        ("C-2 stays strict at high load", {"Analysis.ms": "5900"}, "40.0", "C-2", ()),
        ("C-3 net 55000 (wall 56000 - build 1000) at quiet load", {"wall.ms": "56000"}, quiet, "C-3", ()),
        ("C-3 net 53900 is inside the limit", {"wall.ms": "54900"}, quiet, None, ()),
        ("C-3 reads net: wall 54500 - build 1000 = 53500 is green", {"wall.ms": "54500"}, quiet, None, ()),
        ("C-4 Desktop.ms 53100 at quiet load names DR-ANA-10", {"Desktop.ms": "53100"}, quiet, "DR-ANA-10", ()),
        ("C-3 and C-4 at load 30.2 print COST-MISS and do not fail", {"wall.ms": "56000", "Desktop.ms": "53100"}, "30.2",
         None, ("COST-MISS C-3 55000 load 30.2", "COST-MISS C-4 53100 load 30.2")),
        ("load 24.0 is still gated: C-4 fails", {"Desktop.ms": "53100"}, "24.0", "DR-ANA-10", ()),
        ("load 24.1 is not gated: C-4 is a COST-MISS", {"Desktop.ms": "53100"}, "24.1", None, ("COST-MISS C-4 53100 load 24.1",)),
        ("load not recorded: C-4 is a COST-MISS, never a pass", {"Desktop.ms": "53100"}, "not-recorded", None,
         ("COST-MISS C-4 53100 load not-recorded",)),
        ("C-5 COST Units_Lbf_KeyUnchanged 512.3", {"Analysis.log": passes.replace("Units_Lbf_KeyUnchanged 12.500", "Units_Lbf_KeyUnchanged 512.3")},
         quiet, "Units_Lbf_KeyUnchanged", ()),
        ("C-5 COST F6_ObservedOrder 1612.0", {"Analysis.log": passes + "COST F6_ObservedOrder 1612.0\n"}, quiet, "F6_ObservedOrder", ()),
        ("C-5 stays strict at high load", {"Analysis.log": passes + "COST F6_ObservedOrder 1612.0\n"}, "40.0", "F6_ObservedOrder", ()),
        ("C-5 F6_ObservedOrder 1499.0 is inside its exemption", {"Analysis.log": passes + "COST F6_ObservedOrder 1499.0\n"}, quiet, None, ()),
        ("C-6 Analysis.ms deleted", {"Analysis.ms": None}, quiet, "Analysis.ms", ()),
        ("C-6 wall.ms deleted", {"wall.ms": None}, quiet, "wall.ms", ()),
        ("C-6 build.ms deleted", {"build.ms": None}, quiet, "build.ms", ()),
        ("C-6 Analysis PASS without COST", {"Analysis.log": passes + "PASS NoCost\n"}, quiet, "NoCost", ()),
    ]
    failures = 0
    with tempfile.TemporaryDirectory() as scratch:
        for number, (label, change, load, expected, miss_fragments) in enumerate(cases):
            case = Path(scratch) / f"case{number}"
            case.mkdir()
            for name, text in {**good, **change}.items():
                if text is not None:
                    (case / name).write_text(text, encoding="utf-8", newline="\n")
            errors, misses = check(case, DEFAULT_JOBS, load)
            ok = not errors if expected is None else any(expected in error for error in errors)
            ok = ok and (all(any(fragment in miss for miss in misses) for fragment in miss_fragments) if miss_fragments else not misses)
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else
                  f": expected {expected or 'no error'} and {miss_fragments or 'no miss'}, got {errors or 'no error'} and {misses or 'no miss'}"))
            failures += not ok
    print(f"SELFTEST {len(cases) - failures}/{len(cases)} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    args = list(argv[1:])
    if args == ["--self-test"]:
        return self_test()
    directory, jobs, load = DEFAULT_DIR, DEFAULT_JOBS, "not-recorded"
    while args:
        flag = args.pop(0)
        if flag in ("--dir", "--jobs", "--load") and args:
            value = args.pop(0)
            if flag == "--dir":
                directory = Path(value)
            elif flag == "--load":
                load = value
            else:
                jobs = tuple(part for part in value.split(",") if part)
        else:
            print(__doc__.strip().splitlines()[2], file=sys.stderr)
            return 2
    errors, misses = check(directory, jobs, load)
    for miss in misses:
        print(miss)
    for error in errors:
        print("FAILED: " + error)
    print(f"test costs: {len(errors)} failures, {len(misses)} COST-MISS (load {load})")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
