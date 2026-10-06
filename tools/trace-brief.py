#!/usr/bin/env python3
"""Pre-dispatch trace: flag files a track's visible behaviours cross that its brief does not own.

Control for OWNERSHIP-MISSES-DATA-SOURCE (docs/lessons/defect-classes.md). Input: a design's
trace table (the section headed "Trace", columns Visible behaviour | Data | Producing file | Owner),
a track code, and the brief's owned-file list (one path, basename or glob per line, '#' comments).
Output: every file the track's rows name in "Producing file", plus every src/ and tests/ file that
mentions a type named in the rows' "Data" column, that the owned list does not cover.
Exit 0: nothing flagged. Exit 1: flagged files (candidates for the human, not verdicts).

    python3 tools/trace-brief.py --design docs/design/area3-analysis.md --track TGL --owned brief-owned.txt
    python3 tools/trace-brief.py --self-test
"""

import argparse
import contextlib
import fnmatch
import io
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
SOURCE_DIRS = ("src", "tests")
SOURCE_SUFFIXES = (".cs", ".axaml")
FILE_RE = re.compile(r"([A-Za-z0-9_./]+\.(?:cs|axaml))(\(\.cs\))?")
TYPE_RE = re.compile(r"`([A-Z][A-Za-z0-9]{3,})`")
CELL_SPLIT = re.compile(r"(?<!\\)\|")


def trace_rows(design_text):
    """Rows of the section headed 'Trace': (data, producing, owner) cells."""
    rows, inside = [], False
    for line in design_text.splitlines():
        if line.startswith("#"):
            inside = " Trace" in line or line.lstrip("# ").startswith("Trace")
            continue
        if inside and line.startswith("|"):
            cells = [cell.strip() for cell in CELL_SPLIT.split(line.strip().strip("|"))]
            if len(cells) >= 5 and cells[0].isdigit():
                rows.append((cells[2], cells[3], cells[4]))
    return rows


def track_rows(rows, track):
    word = re.compile(r"\b" + re.escape(track) + r"\b")
    return [row for row in rows if word.search(row[2])]


def named_files(producing):
    names = []
    for match in FILE_RE.finditer(producing):
        names.append(match.group(1))
        if match.group(2):
            names.append(match.group(1) + ".cs")
    return names


def source_files(root):
    found = []
    for directory in SOURCE_DIRS:
        base = root / directory
        if base.is_dir():
            found.extend(p for p in base.rglob("*") if p.suffix in SOURCE_SUFFIXES and p.is_file())
    return sorted(found)


def is_owned(path, owned):
    text = path.as_posix()
    return any(
        text == entry or text.endswith("/" + entry) or fnmatch.fnmatch(text, entry)
        or fnmatch.fnmatch(path.name, entry)
        for entry in owned
    )


def flagged(rows, owned, root):
    """Return (producing_not_owned, referencing_not_owned, unresolved_names)."""
    files = source_files(root)
    relative = [p.relative_to(root) for p in files]
    producing, unresolved, types = set(), [], set()
    for data, produce, _owner in rows:
        types.update(TYPE_RE.findall(data))
        for name in named_files(produce):
            hits = [p for p in relative if p.as_posix().endswith("/" + name) or p.as_posix() == name]
            producing.update(hits)
            if not hits:
                unresolved.append(name)
    referencing = set()
    if types:
        pattern = re.compile(r"\b(?:" + "|".join(sorted(types)) + r")\b")
        for path, rel in zip(files, relative):
            if pattern.search(path.read_text(encoding="utf-8", errors="replace")):
                referencing.add(rel)
    return (
        sorted(p for p in producing if not is_owned(p, owned)),
        sorted(p for p in referencing - producing if not is_owned(p, owned)),
        sorted(set(unresolved)),
    )


def read_owned(path):
    lines = Path(path).read_text(encoding="utf-8").splitlines()
    return [line.strip() for line in lines if line.strip() and not line.lstrip().startswith("#")]


def report(track, rows, owned, root):
    if not rows:
        print("trace-brief: no rows name track " + track, file=sys.stderr)
        return 2
    producing, referencing, unresolved = flagged(rows, owned, root)
    for path in producing:
        print("PRODUCER-NOT-OWNED  " + path.as_posix())
    for path in referencing:
        print("DATA-TYPE-REFERENCE-NOT-OWNED  " + path.as_posix())
    for name in unresolved:
        print("note: named file not found under src/ or tests/ (new file?): " + name)
    print("trace-brief: track " + track + ", " + str(len(rows)) + " rows, "
          + str(len(producing) + len(referencing)) + " flagged", flush=True)
    return 1 if producing or referencing else 0


# The TGL case of 2026-10-05, reconstructed: the rows name ShellMode and Selection; the point-press
# path in PlanCanvas reads Selection and no row names it; the brief owned only the producing files.
SELF_TEST_DESIGN = """### 18.5 Trace - every promised behaviour to its data file

| # | Visible behaviour (screen) | Data | Producing file | Owner |
|---|---|---|---|---|
| 1 | CAD \\| Analysis segment (1) | `ShellMode`, `Selection` | `WorkbenchController.cs`, `ModelArea.axaml`(.cs) | TGL |
| 2 | View menu | command rows | `Shell/CommandTable.cs` | TGL (OD-1) |
| 3 | Other track row | `Widget` | `Other.cs` | SVC |
"""


def self_test():
    with tempfile.TemporaryDirectory(prefix="trace-brief-") as directory:
        root = Path(directory)
        layout = {
            "src/App/WorkbenchController.cs": "enum ShellMode {}",
            "src/App/ModelArea.axaml": "<x/>",
            "src/App/ModelArea.axaml.cs": "class ModelArea {}",
            "src/App/Shell/CommandTable.cs": "class CommandTable {}",
            "src/App/PlanCanvas.cs": "void Press() { Selection.Set(); }",
            "src/App/MainWindow.axaml.cs": "if (ShellMode.Analysis) { }",
            "src/App/Unrelated.cs": "class Unrelated {}",
            "src/App/Other.cs": "class Other {}",
        }
        for name, text in layout.items():
            target = root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(text, encoding="utf-8", newline="\n")
        rows = track_rows(trace_rows(SELF_TEST_DESIGN), "TGL")
        assert len(rows) == 2, "owner match must find rows 1 and 2 only, found " + str(len(rows))
        brief = ["WorkbenchController.cs", "ModelArea.axaml*", "Shell/CommandTable.cs"]
        _, referencing, _ = flagged(rows, brief, root)
        assert [p.name for p in referencing] == ["MainWindow.axaml.cs", "PlanCanvas.cs"], referencing
        complete = brief + ["PlanCanvas.cs", "MainWindow.axaml.cs"]
        with contextlib.redirect_stdout(io.StringIO()):
            red, green = report("TGL", rows, brief, root), report("TGL", rows, complete, root)
        assert red == 1, "red case: unowned data readers must fail"
        assert green == 0, "green case: complete brief must pass"
        short = ["ModelArea.axaml*", "Shell/CommandTable.cs"]
        producing, _, _ = flagged(rows, short, root)
        assert [p.name for p in producing] == ["WorkbenchController.cs"], producing
    print("trace-brief self-test OK", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--design")
    parser.add_argument("--track")
    parser.add_argument("--owned")
    parser.add_argument("--root", default=str(ROOT))
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    if not (args.design and args.track and args.owned):
        parser.error("--design, --track and --owned are required")
    rows = track_rows(trace_rows(Path(args.design).read_text(encoding="utf-8")), args.track)
    return report(args.track, rows, read_owned(args.owned), Path(args.root))


if __name__ == "__main__":
    sys.exit(main())
