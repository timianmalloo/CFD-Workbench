#!/usr/bin/env python3
"""Named-test checker for the app-shell build (docs/design/app-shell.md §12.2).

    python3 tools/check-named-tests.py <track>     e.g. C1, P1, D1, D2, D3a, D4
    python3 tools/check-named-tests.py --self-test

The design is the single authority for test names: every backticked test name in §9 or §12.4 belongs to the first
`(<track>` after it on the same line (closed by `)`, `,` or `;`). A track is green only when each of its names printed
`PASS <name>` in `.tmp-tests/*.log` (run `tools/run-tests.sh` first), no log has a `FAIL` line, and its list is not
empty. Names are exact, never globs. For D3a the ported-name column of docs/proof/app-shell-test-inventory.md is read
as well (§12.5).

Exit 0 green · 1 a rule failed · 2 usage.
"""
from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DESIGN = ROOT / "docs" / "design" / "app-shell.md"
LOGS = ROOT / ".tmp-tests"
INVENTORY = ROOT / "docs" / "proof" / "app-shell-test-inventory.md"

NAMED_SECTIONS = ("## 9.", "### 12.4")
TRACK_SECTION = "## 14."
BACKTICK = re.compile(r"`([^`\n]+)`")
# A test-name-shaped token: Method_State_Outcome (D0), including glob characters so a glob is caught, not skipped.
CANDIDATE = re.compile(r"[A-Z][A-Za-z0-9]*_[A-Za-z0-9_*<>…]+")
TRACK = re.compile(r"\(([A-Za-z][A-Za-z0-9]*)\s*[),;]")
TRACK_ROW = re.compile(r"^\|\s*\*\*([A-Za-z][A-Za-z0-9]*)\b")
FAIL_LINE = re.compile(r"FAIL\b")


def section_lines(text: str, prefixes: tuple[str, ...]):
    """Yields (line number, line) for lines under a heading that starts with one of the prefixes."""
    inside = False
    for number, line in enumerate(text.splitlines(), 1):
        if line.startswith("#"):
            inside = line.startswith(prefixes)
            continue
        if inside:
            yield number, line


def known_tracks(text: str) -> set[str]:
    """The track ids of the §14 table (`| **G0 Glue** | …`)."""
    return {match.group(1) for _, line in section_lines(text, (TRACK_SECTION,))
            if (match := TRACK_ROW.match(line))}


def extract(text: str) -> tuple[dict[str, str], list[str]]:
    """Maps each named test to its track, with every rule violation found in the design."""
    names: dict[str, str] = {}
    errors: list[str] = []
    tracks = known_tracks(text)
    for number, line in section_lines(text, NAMED_SECTIONS):
        for match in BACKTICK.finditer(line):
            token = match.group(1)
            if not CANDIDATE.fullmatch(token):
                continue
            if re.search(r"[*<>…]", token):
                errors.append(f"design line {number}: `{token}` is a glob; test names are exact")
                continue
            owner = TRACK.search(line, match.end())
            if owner is None:
                errors.append(f"design line {number}: `{token}` has no (<track>) after it on its line")
                continue
            track = owner.group(1)
            if track not in tracks:
                errors.append(f"design line {number}: `{token}` names track ({track}), which §14 does not define")
            elif names.setdefault(token, track) != track:
                errors.append(f"design line {number}: `{token}` is given to ({track}) and ({names[token]})")
    return names, errors


def inventory_names(path: Path) -> tuple[list[str], list[str]]:
    """The ported names of the D3a inventory, with its errors (§12.5)."""
    # assume: the inventory is a Markdown table with one row per removed control and a column whose header contains
    # "ported" (case-insensitive) holding the backticked new test name. Confirmed when D3a commits the inventory
    # (checkpoint D3a-0). If the header differs, this reports "no ported-name column": loud, never a silent pass.
    if not path.is_file():
        return [], [f"{path.name} is missing; D3a commits it before any source change (§12.5)"]
    lines = path.read_text(encoding="utf-8").splitlines()
    for index in range(len(lines) - 1):
        header, separator = lines[index], lines[index + 1]
        if not (header.startswith("|") and re.match(r"^\|[\s:|-]+\|?\s*$", separator)):
            continue
        cells = [cell.strip() for cell in header.strip().strip("|").split("|")]
        column = next((at for at, cell in enumerate(cells) if "ported" in cell.lower()), None)
        if column is None:
            continue
        rows, ported = 0, []
        for row in lines[index + 2:]:
            if not row.startswith("|"):
                break
            rows += 1
            values = [cell.strip() for cell in row.strip().strip("|").split("|")]
            if column < len(values):
                ported += [token for token in BACKTICK.findall(values[column]) if CANDIDATE.fullmatch(token)]
        if rows and not ported:
            return [], [f"{path.name}: the ported-name column is empty while the inventory lists {rows} removed controls"]
        return ported, []
    return [], [f"{path.name}: no table with a ported-name column"]


def check(track: str, design: str, log_dir: Path, inventory: Path) -> tuple[list[str], list[str]]:
    """Returns (the track's required names, every failure)."""
    names, errors = extract(design)
    required = {name for name, owner in names.items() if owner == track}
    if track == "D3a":
        ported, inventory_errors = inventory_names(inventory)
        required.update(ported)
        errors += inventory_errors
    if not required:
        errors.append(f"({track}) has no named tests in the design; an empty list is never green")
    passed: set[str] = set()
    for log in sorted(log_dir.glob("*.log")):
        for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
            if line.startswith("PASS "):
                passed.add(line[5:].strip())
            elif FAIL_LINE.match(line):
                errors.append(f"{log.name}: {line}")
    errors += [f"no PASS line for {name}" for name in sorted(required - passed)]
    return sorted(required), errors


SELF_DESIGN = """# Planted design
## 9. Failure-mode analysis
| Mode | Test (track) |
|---|---|
| one | `Alpha_Case_Passes` (C1) |
| two | `Beta_Case_Passes`, `Gamma_Case_Passes` (C1); `Delta_Case_Passes` (D1, a note) |
{plant}
## 10. Not named
| three | `Omega_Outside_Ignored` |
### 12.4 Ledgers
Also: `Epsilon_Case_Passes` (C1), `IScreenSource`, `LAYOUT-SCHEMA`.
## 14. Build tracks
| Track | Owns |
|---|---|
| **C1 Core** | x |
| **D1 Shell** | y |
| **D3a Host** | z |
"""
C1_PASSES = "PASS Alpha_Case_Passes\nPASS Beta_Case_Passes\nPASS Gamma_Case_Passes\nPASS Epsilon_Case_Passes\n"
INVENTORY_EMPTY = "| Control | Ported name | Deleted clause |\n|---|---|---|\n| numericBox | | |\n"

# (case, planted design line, extra log text, track, inventory text, expected error fragment or None for green)
SELF_CASES = [
    ("green control", "", "", "C1", None, None),
    ("planted name with no track", "| four | `Orphan_Name_NoTrack` |", "PASS Orphan_Name_NoTrack\n", "C1", None, "no (<track>)"),
    ("planted glob", "| five | `Glob_*_Name` (C1) |", "PASS Glob_*_Name\n", "C1", None, "is a glob"),
    ("FAIL line in a log", "", "FAIL Other_Suite_Broke boom\n", "C1", None, "FAIL Other_Suite_Broke"),
    ("missing PASS", "| six | `Zeta_Case_Missing` (C1) |", "", "C1", None, "no PASS line for Zeta_Case_Missing"),
    ("empty list", "", "", "D3a", "| Control | Ported name |\n|---|---|\n", "an empty list is never green"),
    ("D3a ported column empty", "", "", "D3a", INVENTORY_EMPTY, "ported-name column is empty"),
]


def self_test() -> int:
    failures = 0
    with tempfile.TemporaryDirectory() as scratch:
        for number, (label, plant, extra, track, inventory_text, expected) in enumerate(SELF_CASES):
            case = Path(scratch) / f"case{number}"
            case.mkdir()
            (case / "Core.log").write_text(C1_PASSES + extra, encoding="utf-8")
            inventory = case / "inventory.md"
            if inventory_text is not None:
                inventory.write_text(inventory_text, encoding="utf-8")
            _, errors = check(track, SELF_DESIGN.format(plant=plant), case, inventory)
            if expected is None:
                ok = not errors
            else:
                ok = any(expected in error for error in errors)
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {expected or 'green'}, got {errors or 'green'}"))
            failures += not ok
    print(f"SELFTEST {len(SELF_CASES) - failures}/{len(SELF_CASES)} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(__doc__.strip().splitlines()[2], file=sys.stderr)
        return 2
    if argv[1] == "--self-test":
        return self_test()
    track = argv[1]
    required, errors = check(track, DESIGN.read_text(encoding="utf-8"), LOGS, INVENTORY)
    for error in errors:
        print("FAILED: " + error)
    passed = len(required) - sum(error.startswith("no PASS line") for error in errors)
    print(f"({track}) {passed}/{len(required)} named tests PASS · {len(errors)} failures")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
