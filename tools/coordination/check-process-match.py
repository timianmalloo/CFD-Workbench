"""Fail when a repo script matches a process by command-line text (defect class PROC-MATCH-BY-CMDLINE).

Ring: fast (every join, via tools/check-docs.py). Cost: one read of the files under tools/ and cases/tools/.
Match process names exactly: `pgrep -x NAME`. `pgrep -f` also matches any shell whose command line merely mentions NAME.
"""
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
BAD = re.compile(r"\b(pgrep|pkill)\s+(-[A-Za-z]*f[A-Za-z]*)\b")


def main():
    hits = []
    for base in ("tools", "cases/tools"):
        for path in sorted((ROOT / base).rglob("*")):
            if not path.is_file() or path == pathlib.Path(__file__).resolve():
                continue
            try:
                text = path.read_text(encoding="utf-8")
            except (UnicodeDecodeError, OSError):
                continue
            for number, line in enumerate(text.splitlines(), 1):
                if BAD.search(line):
                    hits.append(f"{path.relative_to(ROOT)}:{number}: {line.strip()}")
    if hits:
        sys.exit("PROC-MATCH-BY-CMDLINE: match by process name (-x), not command line (-f):\n" + "\n".join(hits))
    print("process matching ok: no pgrep/pkill -f under tools/ or cases/tools/")


main()
