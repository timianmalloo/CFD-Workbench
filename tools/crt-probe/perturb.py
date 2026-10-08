#!/usr/bin/env python3
"""perturb.py ROOT file:line [file:line ...] : on a SCRATCH COPY of the repo, wrap each Math.<transcendental>(...) call on
the named lines in CfdWorkbench.Core.CrtProbe.Up(tag, ...), which returns the value one ulp higher (double.BitIncrement) and
counts the call. On-demand tool, in no ring (Ruling 157); see README.md. It edits files in place, so it refuses a ROOT that has
a .git entry (a real checkout or worktree) and a ROOT with no src/CfdWorkbench.Core/CrtProbe.cs.
"""
import re
import sys
from pathlib import Path

NAMES = "Sin|Cos|Tan|Atan2|Atan|Acos|Exp|Log10|Log|Pow"
PATTERN = re.compile(r"Math\.(?:%s)\(" % NAMES)


def wrap(line_text, path, line_number):
    """(new text, number of calls wrapped) for one source line."""
    out, index, count = "", 0, 0
    while True:
        match = PATTERN.search(line_text, index)
        if not match:
            return out + line_text[index:], count
        depth, end = 1, match.end()
        while depth:
            depth += {"(": 1, ")": -1}.get(line_text[end], 0)
            end += 1
        tag = f"{Path(path).name}:{line_number}"
        out += line_text[index:match.start()] + f'CfdWorkbench.Core.CrtProbe.Up("{tag}", ' + line_text[match.start():end] + ")"
        index = end
        count += 1


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    root = Path(argv[0])
    if (root / ".git").exists():
        print(f"refusing {root}: it has a .git entry; copy the tree to a scratch folder first (see README.md)")
        return 2
    if not (root / "src/CfdWorkbench.Core/CrtProbe.cs").is_file():
        print(f"refusing {root}: copy tools/crt-probe/CrtProbe.cs to src/CfdWorkbench.Core/ first")
        return 2
    for spec in argv[1:]:
        path, line = spec.rsplit(":", 1)
        full = root / path
        lines = full.read_text(encoding="utf-8").split("\n")
        lines[int(line) - 1], count = wrap(lines[int(line) - 1], path, int(line))
        with open(full, "w", encoding="utf-8", newline="") as handle:
            handle.write("\n".join(lines))
        print(spec, count, "calls wrapped:", lines[int(line) - 1].strip()[:150])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
