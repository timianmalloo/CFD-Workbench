#!/usr/bin/env python3
"""Named-test checker for the app-shell and M1.2b builds (docs/design/app-shell.md §12.2, m12b-points.md §12.2).

    python3 tools/check-named-tests.py <track> [--design <path>] [--track-section <heading>] [--named-sections <h1,h2>]
    python3 tools/check-named-tests.py --self-test

The design is the single authority for test names: every backticked test name in §9 or §12.4 belongs to the first
`(<track>` after it on the same line (closed by `)`, `,` or `;`). A track is green only when each of its names printed
`PASS <name>` in `.tmp-tests/*.log` (run `tools/run-tests.sh` first), no log has a `FAIL` line, and its list is not
empty. Names are exact, never globs. For D3a the ported-name column of docs/proof/app-shell-test-inventory.md is read
as well (§12.5). `--design <path>` reads that design instead of the default `docs/design/app-shell.md` — e.g. the
M1.2b design, `docs/design/m12b-points.md`. `--track-section` names the heading prefix of the track table (default
`## 14.`) and `--named-sections` the comma-separated heading prefixes that hold the names (default `## 9.,### 12.4`),
for a design that keeps them elsewhere — e.g. A3a: `--track-section "### 18.2" --named-sections "### 18.8"`
(docs/design/area3-analysis.md §18.1, P-1).

Exit 0 green · 1 a rule failed · 2 usage.
"""
from __future__ import annotations

import io
import re
import sys
import tempfile
from contextlib import redirect_stdout
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

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


def known_tracks(text: str, track_section: str = TRACK_SECTION) -> set[str]:
    """The track ids of the track table (`| **G0 Glue** | …`)."""
    return {match.group(1) for _, line in section_lines(text, (track_section,))
            if (match := TRACK_ROW.match(line))}


def extract(text: str, named_sections: tuple[str, ...] = NAMED_SECTIONS,
            track_section: str = TRACK_SECTION) -> tuple[dict[str, str], list[str]]:
    """Maps each named test to its track, with every rule violation found in the design."""
    names: dict[str, str] = {}
    errors: list[str] = []
    tracks = known_tracks(text, track_section)
    for number, line in section_lines(text, named_sections):
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
                errors.append(f"design line {number}: `{token}` names track ({track}), which the track table ({track_section}) does not define")
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


def check(track: str, design: str, log_dir: Path, inventory: Path, named_sections: tuple[str, ...] = NAMED_SECTIONS,
          track_section: str = TRACK_SECTION) -> tuple[list[str], list[str]]:
    """Returns (the track's required names, every failure)."""
    names, errors = extract(design, named_sections, track_section)
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

# A second, standalone design (not SELF_DESIGN) used only to prove --design routes through main() end to end: the
# existing SELF_CASES call check() directly, so they would catch a planted unattributed name even if --design were
# never wired into main() at all.
SECOND_DESIGN = """# Second planted design
## 9. Failure-mode analysis
| Mode | Test (track) |
|---|---|
| one | `Theta_Case_Passes` (C1) |
| two | `Unattributed_Name_Planted` |
## 14. Build tracks
| Track | Owns |
|---|---|
| **C1 Core** | x |
"""

# A design that keeps its tracks and names under other headings (the A3a layout, area3-analysis.md §18.2 and §18.8). Read
# with --track-section and --named-sections through main(), PRE is green; read with the defaults, PRE has no names.
SECTIONED_DESIGN = """# Sectioned planted design
## 9. Failure-mode analysis
| one | `Outside_Default_Ignored` (PRE) |
### 18.2 Tracks
| Track | Owns |
|---|---|
| **PRE** contracts | x |
### 18.8 Named-test ledger
| `Iota_Case_Passes` (PRE) ✚ | A | < 1 ms | a planted call |
"""

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
            (case / "Core.log").write_text(C1_PASSES + extra, encoding="utf-8", newline="\n")
            inventory = case / "inventory.md"
            if inventory_text is not None:
                inventory.write_text(inventory_text, encoding="utf-8", newline="\n")
            _, errors = check(track, SELF_DESIGN.format(plant=plant), case, inventory)
            if expected is None:
                ok = not errors
            else:
                ok = any(expected in error for error in errors)
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {expected or 'green'}, got {errors or 'green'}"))
            failures += not ok

        # --design end to end through main(): a second design, on disk, read only because the flag names it.
        design_file = Path(scratch) / "second-design.md"
        design_file.write_text(SECOND_DESIGN, encoding="utf-8", newline="\n")
        global LOGS, INVENTORY
        saved_logs, saved_inventory = LOGS, INVENTORY
        LOGS, INVENTORY = Path(scratch), Path(scratch) / "inventory.md"
        try:
            buffer = io.StringIO()
            with redirect_stdout(buffer):
                exit_code = main(["check-named-tests.py", "C1", "--design", str(design_file)])
        finally:
            LOGS, INVENTORY = saved_logs, saved_inventory
        output = buffer.getvalue()
        fragment = "has no (<track>) after it on its line"
        ok = exit_code == 1 and fragment in output
        label = "--design routes through main()"
        print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": exit {exit_code}, expected {fragment!r} in {output!r}"))
        failures += not ok

        # --track-section and --named-sections end to end through main(): green with the flags; with the defaults the
        # same design yields no names for PRE (its only default-section name sits under ## 9. with an undefined track).
        sectioned = Path(scratch) / "sectioned-design.md"
        sectioned.write_text(SECTIONED_DESIGN, encoding="utf-8", newline="\n")
        logs = Path(scratch) / "sectioned-logs"
        logs.mkdir()
        (logs / "Analysis.log").write_text("PASS Iota_Case_Passes\n", encoding="utf-8", newline="\n")
        LOGS = logs
        try:
            flagged, defaulted = io.StringIO(), io.StringIO()
            with redirect_stdout(flagged):
                flagged_exit = main(["check-named-tests.py", "PRE", "--design", str(sectioned),
                                     "--track-section", "### 18.2", "--named-sections", "### 18.8"])
            with redirect_stdout(defaulted):
                defaulted_exit = main(["check-named-tests.py", "PRE", "--design", str(sectioned)])
        finally:
            LOGS = saved_logs
        ok = (flagged_exit == 0 and "(PRE) 1/1 named tests PASS" in flagged.getvalue()
              and defaulted_exit == 1 and "an empty list is never green" in defaulted.getvalue())
        label = "--track-section/--named-sections route through main()"
        print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else
              f": flagged exit {flagged_exit} {flagged.getvalue()!r}; defaulted exit {defaulted_exit} {defaulted.getvalue()!r}"))
        failures += not ok
        total = len(SELF_CASES) + 2
    print(f"SELFTEST {total - failures}/{total} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    args = list(argv[1:])
    values = {"--design": str(DESIGN), "--track-section": TRACK_SECTION, "--named-sections": ",".join(NAMED_SECTIONS)}
    for name in values:
        if name in args:
            flag = args.index(name)
            if flag + 1 >= len(args) or not args[flag + 1].strip():
                print(__doc__.strip().splitlines()[2], file=sys.stderr)
                return 2
            values[name] = args[flag + 1]
            del args[flag:flag + 2]
    design_path = Path(values["--design"])
    track_section = values["--track-section"]
    named_sections = tuple(part.strip() for part in values["--named-sections"].split(",") if part.strip())
    if len(args) != 1:
        print(__doc__.strip().splitlines()[2], file=sys.stderr)
        return 2
    if args[0] == "--self-test":
        return self_test()
    track = args[0]
    required, errors = check(track, design_path.read_text(encoding="utf-8"), LOGS, INVENTORY, named_sections, track_section)
    for error in errors:
        print("FAILED: " + error)
    passed = len(required) - sum(error.startswith("no PASS line") for error in errors)
    print(f"({track}) {passed}/{len(required)} named tests PASS · {len(errors)} failures")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
