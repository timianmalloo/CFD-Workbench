#!/usr/bin/env python3
"""Enforce the test-ring cost rules C-2..C-6 (docs/design/area3-analysis.md section 13.4) from tools/run-tests.sh output.

  python3 tools/check-test-costs.py [--dir .tmp-tests] [--jobs Core.part1of3,Core.part2of3,Core.part3of3,Desktop,Analysis.part1of2,Analysis.part2of2,Cli] [--load <1-minute load>] [--load-source proc-gitbash|...] [--host <name>]
  python3 tools/check-test-costs.py --budget <wall s> <budget s> <load>    [--load-source S --host H]    (TEST-BUDGET, Ruling 87: exit 3 or 0)
  python3 tools/check-test-costs.py --resolve-host <hostname>    (prints the baseline key: $CFD_RING_HOST if set, validated [a-z0-9-]{1,32}, else <hostname>; Ruling 168 (3))
  python3 tools/check-test-costs.py --self-test

Reads <name>.ms and wall.ms (C-1, millisecond clocks written by run-tests.sh) and the COST lines of every Analysis log (Analysis.log, or Analysis.part<k>of<n>.log).
Exit 0 every rule holds . 1 a rule failed, or a reading is missing ("not recorded" never passes) . 2 usage.
PARTITION-SKEW <harness> parts=<ms list> skew_ms=<n> (hints stale?): advisory, printed when a harness run in parts (Core, Analysis) has
slowest minus fastest over 15 % of its per-part limit (Analysis 5,000 ms; Core 30,000 ms reference). It never changes the exit code.
Ring: every join (run-tests.sh calls it after its wait loop; join.json runs it again). Cost: under 0.1 s (the skew check adds five file reads).
"""
from __future__ import annotations

import os
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
DEFAULT_JOBS = ("Core.part1of3", "Core.part2of3", "Core.part3of3", "Desktop", "Analysis.part1of2", "Analysis.part2of2", "Cli")
ANALYSIS_LIMIT_MS = 5000      # C-2, per Analysis part (B4: the harness is split like Core; section 13.4's "Analysis <= 5 s" reads per part)
# Ruling 84 (DR-RING-1) stated C-3 and C-4 as deltas on the quiet base while the section 13.4 absolute limits were out of
# reach. Track B2 met its condition 3 (Desktop <= 43 s, wall <= 50 s: docs/proof/ring-b2/profile.md), so the absolute limits
# are back and the deltas, the bases and the baseline path are gone:
#   C-3 measures wall.ms - build.ms (net ring time, while an Analysis job is in the jobs; a cold build is not a test cost); limit 50000 ms.
#   C-4 measures Desktop.ms (DR-ANA-10). Ruling 99 (DR-RING-3) re-based it ONCE, from the recorded quiet baseline
#   docs/proof/ring-b4/baseline-desktop.csv (every run of the series that ended at load <= 24, four: 42,471 / 42,865 / 47,369 / 40,546 ms): limit = max + 2,000 ms.
#   Track B5's CPU cut (plan-canvas fixture, section 9.9) did not bring three quiet runs to <= 41 s, so 43,000 ms does not return.
#   No second re-base: a quiet reading over this limit is a red for the track that caused it (Ruling 99 (2)).
WALL_LIMIT_MS = 50000   # C-3
DESKTOP_BASE_MAX_MS = 47369  # the slowest of the three recorded quiet runs
DESKTOP_LIMIT_MS = DESKTOP_BASE_MAX_MS + 2000  # C-4, 49,369 ms
# C-2, C-3, C-4 and C-5 fail only when the 1-minute load at ring end is at or below this; above it, or not recorded, they print
# COST-MISS (Ruling 84; C-2 joined by Ruling 87, which also gates the TEST-BUDGET check the same way; C-5 joined by Ruling 139).
# C-6 is never gated. The limits and this threshold are Mac-calibrated (sysctl vm.loadavg).
LOAD_GATE = 24.0
# Ruling 139 (b): a host whose load source is /proc/loadavg under Git Bash or Cygwin ("proc-gitbash", named by run-tests.sh) is
# uncalibrated: every cost rule and TEST-BUDGET prints COST-MISS ... host <name> and exits 0, until the 3-run quiet baseline
# docs/proof/ring-<host>/baseline.csv exists. Its form: a first line `gate=<n>` (that host's load threshold, set from the runs),
# then one row per quiet run `<run>,<end load>,<wall ms>`; three rows or more calibrate. The ms limits are not re-based per host.
UNCALIBRATED_SOURCE = "proc-gitbash"
BASELINE_MIN_RUNS = 3
CHECK_LIMIT_MS = 500.0        # C-5
EXEMPT_LIMIT_MS = 1500.0      # C-5, the two named A8.4 exemptions
EXEMPT_CHECKS = ("F1_FlatPlate_RichardsonClAlphaTo2Pi", "F6_ObservedOrder")


# PARTITION-SKEW (track OBS, from ANALYSIS-HARNESS-GROWTH): advisory, never a failure. A harness run in parts is balanced
# when the slowest part minus the fastest stays within this share of the per-part limit; the cost hints that set the parts
# drift as checks are added, and part 1 once sat at 4.9 s against the 5 s limit before anything failed.
SKEW_FRACTION = 0.15
# assume: Core has no per-part limit rule; the 2026-10-09 ring read the Core parts at 31.6 / 31.7 / 24.2 s, so 30,000 ms is the reference.
# Confirmed by: a Core part limit added to this file replaces the constant. Breaks if false: the Core skew threshold is off, advisory only.
CORE_PART_REF_MS = 30000
PART_JOB = re.compile(r"(?P<harness>[A-Za-z]+)\.part(?P<k>\d+)of(?P<n>\d+)")


def is_analysis(name: str) -> bool:
    """The Analysis job, whole or one part of it (Analysis.part1of2)."""
    return name == "Analysis" or name.startswith("Analysis.part")


def read_ms(directory: Path, name: str) -> int | None:
    try:
        return int((directory / f"{name}.ms").read_text(encoding="utf-8").strip())
    except (OSError, ValueError):
        return None


def partition_skew(directory: Path, jobs: tuple[str, ...]) -> list[str]:
    """PARTITION-SKEW lines for each harness run in parts (Core, Analysis) whose slowest part exceeds its fastest by more
    than SKEW_FRACTION of the per-part limit. Advisory: the caller prints them and never fails on them. A harness with a
    part that has no reading is skipped here; C-6 already fails a missing .ms."""
    harnesses: dict[str, list[tuple[int, str]]] = {}
    for name in jobs:
        match = PART_JOB.fullmatch(name)
        if match:
            harnesses.setdefault(match["harness"], []).append((int(match["k"]), name))
    lines: list[str] = []
    for harness, parts in harnesses.items():
        readings = [read_ms(directory, name) for _, name in sorted(parts)]
        known = [value for value in readings if value is not None]
        if len(known) < 2 or len(known) != len(readings):
            continue
        limit = ANALYSIS_LIMIT_MS if harness == "Analysis" else CORE_PART_REF_MS
        skew = max(known) - min(known)
        if skew > SKEW_FRACTION * limit:
            lines.append(f"PARTITION-SKEW {harness} parts={','.join(str(value) for value in known)} skew_ms={skew} (hints stale?)")
    return lines


RING_HOST_KEY = re.compile(r"[a-z0-9-]{1,32}")


def resolve_host(override: str | None, hostname: str) -> str:
    """The baseline key (docs/proof/ring-<key>/baseline.csv). Ruling 168 (3): a set CFD_RING_HOST replaces the machine
    hostname (a Windows hostname can be personal and the repo is public); it must match [a-z0-9-]{1,32}, else ValueError."""
    if override is None:
        return hostname
    if not RING_HOST_KEY.fullmatch(override):
        raise ValueError(f"CFD_RING_HOST={override!r} is refused: use 1-32 characters from a-z, 0-9 and '-' (it names docs/proof/ring-<key>/)")
    return override


def host_baseline_gate(host: str, proof: Path | None = None) -> float | None:
    """The host's recorded load threshold, or None while its 3-run quiet baseline is missing or unreadable."""
    path = (proof or ROOT / "docs" / "proof") / f"ring-{host}" / "baseline.csv"
    try:
        lines = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
        gate = float(lines[0].removeprefix("gate=")) if lines and lines[0].startswith("gate=") else None
        rows = [line for line in lines[1:] if len(line.split(",")) == 3 and float(line.split(",")[1]) >= 0]
    except (OSError, ValueError):
        return None
    return gate if gate is not None and len(rows) >= BASELINE_MIN_RUNS else None


def host_gate(source: str, host: str, proof: Path | None = None) -> tuple[float, bool]:
    """(load threshold, uncalibrated). Only the proc-gitbash source is ever uncalibrated; a baseline sets its threshold."""
    if source != UNCALIBRATED_SOURCE:
        return LOAD_GATE, False
    gate = host_baseline_gate(host, proof)
    return (LOAD_GATE, True) if gate is None else (gate, False)


def load_gated(load: str, gate: float = LOAD_GATE) -> bool:
    """True when the end load is recorded and at or below the gate; "not recorded" never fails a load-gated rule."""
    try:
        return float(load) <= gate
    except ValueError:
        return False


def budget_verdict(wall: int, budget: int, load: str, source: str = "", host: str = "", proof: Path | None = None) -> tuple[int, str | None]:
    """TEST-BUDGET (Ruling 87): wall over budget exits 3 at an end load <= LOAD_GATE; above it, or not recorded, it prints
    TEST-BUDGET-MISS and exits 0. The budget figure is unchanged and is raised only from a measured cost."""
    if wall <= budget:
        return 0, None
    gate, uncalibrated = host_gate(source, host, proof)
    if uncalibrated:
        return 0, f"COST-MISS TEST-BUDGET {wall} s host {host}"
    if load_gated(load, gate):
        return 3, (f"TEST-BUDGET: green, but {wall} s is over the {budget} s budget. "
                   "Find the new cost before raising it (docs/reviews/test-ci-waste.md).")
    return 0, f"TEST-BUDGET-MISS {wall} s load {load}"


def check(directory: Path, jobs: tuple[str, ...], load: str = "not-recorded", source: str = "", host: str = "",
          proof: Path | None = None) -> tuple[list[str], list[str]]:
    gate, uncalibrated = host_gate(source, host, proof)
    errors, misses = check_rules(directory, jobs, load, gate)
    if uncalibrated:  # Ruling 139 (b): every rule is advisory on an uncalibrated host
        return [], [f"COST-MISS {error} host {host}" for error in errors] + misses
    return errors, misses


def check_rules(directory: Path, jobs: tuple[str, ...], load: str, gate_limit: float) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    misses: list[str] = []
    readings: dict[str, int] = {}
    for name in (*jobs, "wall", "build"):  # C-6: a reading that is missing is a failure, never a pass
        value = read_ms(directory, name)
        if value is None:
            errors.append(f"C-6 {name}.ms is missing or unreadable: not recorded (run tools/run-tests.sh)")
        else:
            readings[name] = value
    gated = load_gated(load, gate_limit)

    def timing(rule: str, ms: int, limit: int, message: str) -> None:
        if ms <= limit:
            return
        if gated:
            errors.append(message)
        else:
            misses.append(f"COST-MISS {rule} {ms} load {load}")

    # C-2 takes the same load gate as C-3 and C-4 (Ruling 87): a wall clock of one job under CPU contention. It applies to each
    # Analysis part on its own (B4, ANALYSIS-HARNESS-GROWTH): a split harness is limited per part, as DR-ANA-10 limits Desktop.
    analysis = [name for name in jobs if is_analysis(name)]
    for name in analysis:
        if name in readings:
            timing("C-2", readings[name], ANALYSIS_LIMIT_MS,
                   f"C-2 {name} took {readings[name]} ms, over {ANALYSIS_LIMIT_MS} ms")
    if analysis and "wall" in readings and "build" in readings:
        net = readings["wall"] - readings["build"]
        timing("C-3", net, WALL_LIMIT_MS, f"C-3 run-tests net wall {net} ms (wall - build), over {WALL_LIMIT_MS} ms")
    if "Desktop" in readings:
        timing("C-4", readings["Desktop"], DESKTOP_LIMIT_MS, f"C-4 Desktop took {readings['Desktop']} ms, over "
               f"{DESKTOP_LIMIT_MS} ms: DR-ANA-10 applies (spread or split the Desktop job)")
    if analysis:
        lines: list[str] = []
        for name in analysis:
            try:
                lines += (directory / f"{name}.log").read_text(encoding="utf-8", errors="replace").splitlines()
            except OSError:
                errors.append(f"C-6 {name}.log is missing: its COST lines are not recorded")
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
                if gated:
                    errors.append(f"C-5 {name} took {ms} ms, over {limit:.0f} ms (move it to readiness with its cost, or make it cheaper)")
                else:
                    misses.append(f"COST-MISS C-5 {name} {ms:g} load {load}")
    return errors, misses


def self_test_hosts(good: dict[str, str], passes: str) -> tuple[int, int]:
    """Ruling 139 (b): an uncalibrated host is advisory on every rule and TEST-BUDGET; a recorded 3-run baseline calibrates it."""
    failures = total = 0

    def report(ok: bool, label: str, detail: str) -> None:
        nonlocal failures, total
        total += 1
        failures += not ok
        print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": {detail}"))

    with tempfile.TemporaryDirectory() as proof_scratch, tempfile.TemporaryDirectory() as scratch:
        proof = Path(proof_scratch)
        for host, rows in (("win1", "1,6.0,52000\n2,7.5,51000\n3,5.0,53000\n"), ("win2", "1,6.0,52000\n2,7.5,51000\n")):
            (proof / f"ring-{host}").mkdir()
            (proof / f"ring-{host}" / "baseline.csv").write_text("gate=30\n" + rows, encoding="utf-8", newline="\n")
        budget_cases = [
            ("uncalibrated host: TEST-BUDGET over budget at quiet load is a host COST-MISS", "proc-gitbash", "winx", "5", 0, "COST-MISS TEST-BUDGET 70 s host winx"),
            ("host with two baseline runs is still uncalibrated", "proc-gitbash", "win2", "5", 0, "host win2"),
            ("calibrated host: TEST-BUDGET over budget at load 5 exits 3", "proc-gitbash", "win1", "5", 3, "TEST-BUDGET:"),
            ("calibrated host: its recorded gate (30) applies", "proc-gitbash", "win1", "28", 3, "TEST-BUDGET:"),
            ("sysctl host is never uncalibrated", "sysctl", "mac", "5", 3, "TEST-BUDGET:"),
        ]
        for label, source, host, load, code, fragment in budget_cases:
            got_code, line = budget_verdict(70, 60, load, source, host, proof)
            report(got_code == code and line is not None and fragment in line, label, f"expected exit {code} {fragment}, got {got_code} {line}")
        case = Path(scratch)
        slow = {"Desktop.ms": "49400", "Analysis.part1of2.log": passes.replace("Units_Lbf_KeyUnchanged 12.500", "Units_Lbf_KeyUnchanged 512.3")}
        for name, text in {**good, **slow}.items():
            (case / name).write_text(text, encoding="utf-8", newline="\n")
        errors, misses = check(case, DEFAULT_JOBS, "5.0", "proc-gitbash", "winx", proof)
        report(not errors and any("COST-MISS C-4" in m and "host winx" in m for m in misses) and any("COST-MISS C-5" in m and "host winx" in m for m in misses),
               "uncalibrated host: C-4 and C-5 over limit at quiet load are host COST-MISS, exit 0", f"got {errors} and {misses}")
        errors, misses = check(case, DEFAULT_JOBS, "5.0", "proc-gitbash", "win1", proof)
        report(any("C-4" in e for e in errors) and any("C-5" in e for e in errors), "calibrated host: the same readings fail", f"got {errors} and {misses}")
    return failures, total


def self_test_ring_host() -> tuple[int, int]:
    """Ruling 168 (3): CFD_RING_HOST names the baseline folder ring-<key>; a value outside [a-z0-9-]{1,32} is refused."""
    cases = [
        ("CFD_RING_HOST=pc-win resolves to ring-pc-win", "pc-win", "Tims-PC", "pc-win", None),
        ("unset: the machine hostname is used as before", None, "mac-studio", "mac-studio", None),
        ("override wins over the hostname", "pc-win", "mac-studio", "pc-win", None),
        ("a 32-character key is accepted", "a" * 32, "h", "a" * 32, None),
        ("a 33-character key is refused", "a" * 33, "h", None, "CFD_RING_HOST"),
        ("an uppercase key is refused", "PC-Win", "h", None, "CFD_RING_HOST"),
        ("a dotted (FQDN-like) key is refused", "pc.win.example", "h", None, "CFD_RING_HOST"),
        ("a path-like key is refused", "../x", "h", None, "CFD_RING_HOST"),
        ("an empty key is refused", "", "h", None, "CFD_RING_HOST"),
    ]
    failures = 0
    for label, override, hostname, want, refusal in cases:
        try:
            got, error = resolve_host(override, hostname), None
        except ValueError as exc:
            got, error = None, str(exc)
        ok = got == want and (refusal is None and error is None or refusal is not None and error is not None and refusal in error)
        print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {want or refusal}, got {got or error}"))
        failures += not ok
    return failures, len(cases)


def self_test_skew() -> tuple[int, int]:
    """PARTITION-SKEW: balanced parts print nothing; a skewed harness prints the line; it is advisory, so check() never reports it."""
    balanced = {"Core.part1of3": 29000, "Core.part2of3": 30000, "Core.part3of3": 31000, "Analysis.part1of2": 4500, "Analysis.part2of2": 4900}
    cases = [
        ("skew: balanced parts print nothing", {}, ()),
        ("skew: Analysis 4500 vs 2500 prints the line", {"Analysis.part2of2": 2500},
         ("PARTITION-SKEW Analysis parts=4500,2500 skew_ms=2000 (hints stale?)",)),
        ("skew: Analysis exactly 15% (750 ms) prints nothing", {"Analysis.part1of2": 4150}, ()),
        ("skew: Analysis 751 ms prints the line", {"Analysis.part1of2": 4149}, ("PARTITION-SKEW Analysis parts=4149,4900 skew_ms=751 (hints stale?)",)),
        ("skew: Core 24000 vs 30000 prints the Core line", {"Core.part3of3": 24000},
         ("PARTITION-SKEW Core parts=29000,30000,24000 skew_ms=6000 (hints stale?)",)),
        ("skew: a missing part reading prints nothing", {"Core.part2of3": None}, ()),
    ]
    failures = 0
    with tempfile.TemporaryDirectory() as scratch:
        for number, (label, change, want) in enumerate(cases):
            case = Path(scratch) / f"skew{number}"
            case.mkdir()
            for name, value in {**balanced, **change}.items():
                if value is not None:
                    (case / f"{name}.ms").write_text(str(value), encoding="utf-8", newline="\n")
            got = tuple(partition_skew(case, DEFAULT_JOBS))
            ok = got == want
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {want}, got {got}"))
            failures += not ok
    return failures, len(cases)


def self_test() -> int:
    """Every row of the 13.4 table plus Ruling 84: a green baseline, then each failing input planted alone must turn it red."""
    passes = "".join(f"PASS {name}\nCOST {name} 12.500\n" for name in ("Units_Lbf_KeyUnchanged", "F6_ObservedOrder"))
    good = {"Core.part1of3.ms": "30000", "Core.part2of3.ms": "30000", "Core.part3of3.ms": "30000", "Desktop.ms": "40000", "Analysis.part1of2.ms": "2500",
            "Analysis.part2of2.ms": "2500", "Cli.ms": "1500", "wall.ms": "45000", "build.ms": "1000",
            "Analysis.part1of2.log": passes, "Analysis.part2of2.log": "PASS Other_Check\nCOST Other_Check 3.000\n"}
    quiet = "5.0"
    # label, files replacing the baseline (None deletes), end load, expected error fragment (None: no error),
    # COST-MISS fragments that must be printed (an empty tuple: none)
    cases = [
        ("baseline is green", {}, quiet, None, ()),
        ("C-2 Analysis.ms 5900", {"Analysis.part1of2.ms": "5900"}, quiet, "C-2", ()),
        ("C-2 Analysis.ms 5900 at load 40 is a COST-MISS, not a failure (Ruling 87)", {"Analysis.part1of2.ms": "5900"}, "40.0", None,
         ("COST-MISS C-2 5900 load 40.0",)),
        ("C-2 load 24.0 is still gated", {"Analysis.part1of2.ms": "5900"}, "24.0", "C-2", ()),
        ("C-2 load not recorded is a COST-MISS", {"Analysis.part1of2.ms": "5900"}, "not-recorded", None,
         ("COST-MISS C-2 5900 load not-recorded",)),
        ("C-3 net 51000 (wall 52000 - build 1000) at quiet load", {"wall.ms": "52000"}, quiet, "C-3", ()),
        ("C-3 net 49900 is inside the limit", {"wall.ms": "50900"}, quiet, None, ()),
        ("C-3 reads net: wall 50500 - build 1000 = 49500 is green", {"wall.ms": "50500"}, quiet, None, ()),
        ("C-4 Desktop.ms 49369 (the limit) is green at quiet load", {"Desktop.ms": "49369"}, quiet, None, ()),
        ("C-4 Desktop.ms 49400 at quiet load names DR-ANA-10", {"Desktop.ms": "49400"}, quiet, "DR-ANA-10", ()),
        ("C-3 and C-4 at load 30.2 print COST-MISS and do not fail", {"wall.ms": "52000", "Desktop.ms": "49400"}, "30.2",
         None, ("COST-MISS C-3 51000 load 30.2", "COST-MISS C-4 49400 load 30.2")),
        ("load 24.0 is still gated: C-4 fails", {"Desktop.ms": "49400"}, "24.0", "DR-ANA-10", ()),
        ("load 24.1 is not gated: C-4 is a COST-MISS", {"Desktop.ms": "49400"}, "24.1", None, ("COST-MISS C-4 49400 load 24.1",)),
        ("load not recorded: C-4 is a COST-MISS, never a pass", {"Desktop.ms": "49400"}, "not-recorded", None,
         ("COST-MISS C-4 49400 load not-recorded",)),
        ("C-5 COST Units_Lbf_KeyUnchanged 512.3", {"Analysis.part1of2.log": passes.replace("Units_Lbf_KeyUnchanged 12.500", "Units_Lbf_KeyUnchanged 512.3")},
         quiet, "Units_Lbf_KeyUnchanged", ()),
        ("C-5 COST F6_ObservedOrder 1612.0", {"Analysis.part1of2.log": passes + "COST F6_ObservedOrder 1612.0\n"}, quiet, "F6_ObservedOrder", ()),
        ("C-5 at load 30 is a COST-MISS, not a failure (Ruling 139)", {"Analysis.part1of2.log": passes + "COST F6_ObservedOrder 1612.0\n"}, "30.0", None,
         ("COST-MISS C-5 F6_ObservedOrder 1612 load 30.0",)),
        ("C-5 load not recorded is a COST-MISS", {"Analysis.part1of2.log": passes.replace("Units_Lbf_KeyUnchanged 12.500", "Units_Lbf_KeyUnchanged 512.3")},
         "not-recorded", None, ("COST-MISS C-5 Units_Lbf_KeyUnchanged 512.3 load not-recorded",)),
        ("C-5 load 24.0 is still gated", {"Analysis.part1of2.log": passes + "COST F6_ObservedOrder 1612.0\n"}, "24.0", "F6_ObservedOrder", ()),
        ("C-6 stays ungated at load 40: a missing reading still fails", {"wall.ms": None}, "40.0", "wall.ms", ()),
        ("C-5 F6_ObservedOrder 1499.0 is inside its exemption", {"Analysis.part1of2.log": passes + "COST F6_ObservedOrder 1499.0\n"}, quiet, None, ()),
        ("C-2 is per part: part 2 at 5900 fails alone", {"Analysis.part2of2.ms": "5900"}, quiet, "Analysis.part2of2", ()),
        ("C-2 is per part: two parts of 4900 are green though they sum over 5000", {"Analysis.part1of2.ms": "4900", "Analysis.part2of2.ms": "4900"}, quiet, None, ()),
        ("C-5 reads part 2's log too", {"Analysis.part2of2.log": "PASS Slow_Check\nCOST Slow_Check 612.0\n"}, quiet, "Slow_Check", ()),
        ("C-6 Analysis part 2 log missing", {"Analysis.part2of2.log": None}, quiet, "Analysis.part2of2.log", ()),
        ("C-6 Analysis.part1of2.ms deleted", {"Analysis.part1of2.ms": None}, quiet, "Analysis.part1of2.ms", ()),
        ("C-6 wall.ms deleted", {"wall.ms": None}, quiet, "wall.ms", ()),
        ("C-6 build.ms deleted", {"build.ms": None}, quiet, "build.ms", ()),
        ("C-6 Analysis PASS without COST", {"Analysis.part1of2.log": passes + "PASS NoCost\n"}, quiet, "NoCost", ()),
    ]
    failures = 0
    # TEST-BUDGET planted checks (Ruling 87): label, wall s, budget s, load, expected exit, expected line fragment
    budget_cases = [
        ("TEST-BUDGET at budget is green", 60, 60, "5.0", 0, None),
        ("TEST-BUDGET over budget at load 30 prints TEST-BUDGET-MISS and exits 0", 70, 60, "30", 0, "TEST-BUDGET-MISS 70 s load 30"),
        ("TEST-BUDGET over budget at load 5 exits 3", 70, 60, "5", 3, "TEST-BUDGET:"),
        ("TEST-BUDGET load 24.0 is still gated", 70, 60, "24.0", 3, "TEST-BUDGET:"),
        ("TEST-BUDGET load not recorded is a MISS, never a pass", 70, 60, "not-recorded", 0, "TEST-BUDGET-MISS 70 s load not-recorded"),
    ]
    host_failures, host_total = self_test_hosts(good, passes)
    key_failures, key_total = self_test_ring_host()
    skew_failures, skew_total = self_test_skew()
    failures += host_failures + key_failures + skew_failures
    host_total += key_total + skew_total
    for label, wall, budget, load, code, fragment in budget_cases:
        got_code, line = budget_verdict(wall, budget, load)
        ok = got_code == code and (fragment is None and line is None or fragment is not None and line is not None and fragment in line)
        print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected exit {code} {fragment}, got {got_code} {line}"))
        failures += not ok
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
    total = len(cases) + len(budget_cases) + host_total
    print(f"SELFTEST {total - failures}/{total} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    args = list(argv[1:])
    if args == ["--self-test"]:
        return self_test()
    if len(args) == 2 and args[0] == "--resolve-host":  # run-tests.sh passes its hostname; prints the key or refuses (exit 2)
        try:
            print(resolve_host(os.environ.get("CFD_RING_HOST"), args[1]))
        except ValueError as exc:
            print(str(exc), file=sys.stderr)
            return 2
        return 0
    directory, jobs, load, source, host, budget = DEFAULT_DIR, DEFAULT_JOBS, "not-recorded", "", "", None
    while args:
        flag = args.pop(0)
        if flag == "--budget" and len(args) >= 3:  # --budget <wall s> <budget s> <load>: prints the line, exits per Ruling 87
            budget = (int(args[0]), int(args[1]), args[2])
            del args[:3]
        elif flag in ("--dir", "--jobs", "--load", "--load-source", "--host") and args:
            value = args.pop(0)
            if flag == "--dir":
                directory = Path(value)
            elif flag == "--load":
                load = value
            elif flag == "--load-source":
                source = value
            elif flag == "--host":
                host = value
            else:
                jobs = tuple(part for part in value.split(",") if part)
        else:
            print(__doc__.strip().splitlines()[2], file=sys.stderr)
            return 2
    if budget is not None:
        code, line = budget_verdict(*budget, source, host)
        if line:
            print(line)
        return code
    errors, misses = check(directory, jobs, load, source, host)
    for miss in misses:
        print(miss)
    for skew in partition_skew(directory, jobs):
        print(skew)
    for error in errors:
        print("FAILED: " + error)
    print(f"test costs: {len(errors)} failures, {len(misses)} COST-MISS (load {load})")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
