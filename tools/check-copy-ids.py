#!/usr/bin/env python3
"""COPY-ID-COLLISION control: every `| COPY-<n> |` row id in DESIGN.md section 7 is unique.

Two parallel tracks that each append the "next" copy id take the same number. A text merge conflicts and a human
sees it; a union-merged table keeps both rows silently and one id then means two sentences. This lint fails on a
repeated row id. Scope is DESIGN.md only: design notes under docs/ number their own local rows.
Rule that goes with it: a parallel track takes copy ids only at the join, or from a range the Coordinator hands out.
`--self-test` plants a duplicate.
"""

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
ROW = re.compile(r"^\| (COPY-[0-9A-Za-z-]+) \|")


def duplicates(text):
    """[(id, [line numbers])] for each row id that appears more than once."""
    seen = {}
    for number, line in enumerate(text.splitlines(), start=1):
        match = ROW.match(line)
        if match:
            seen.setdefault(match.group(1), []).append(number)
    return [(key, lines) for key, lines in seen.items() if len(lines) > 1]


def self_test():
    clean = "| COPY-1 | a |\n| COPY-2 | b — superseded by COPY-1 |\n| COPY-G10 | c |\n"
    assert duplicates(clean) == [], duplicates(clean)
    planted = clean + "| COPY-2 | a different sentence |\n"
    assert duplicates(planted) == [("COPY-2", [2, 4])], duplicates(planted)
    print("check-copy-ids self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    text = (ROOT / "DESIGN.md").read_text(encoding="utf-8")
    found = duplicates(text)
    for key, lines in found:
        print(f"COPY-ID-COLLISION: {key} is the id of DESIGN.md rows at lines " + ", ".join(map(str, lines)))
    if found:
        print("Take copy ids at the join, or from a range the Coordinator hands out.", file=sys.stderr)
        return 1
    print("copy ids ok: " + str(len(re.findall(r"(?m)^\| COPY-", text))) + " rows, every id unique", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
