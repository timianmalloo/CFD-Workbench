#!/usr/bin/env python3
"""CRT-TRANSCENDENTAL control (Ruling 156 (3)(c)): a file whose output is a committed byte or hash must not call a C-runtime transcendental.

.NET forwards Math.Sin/Cos/Tan/Atan/Atan2/Pow/Exp/Log and the other transcendentals to the platform C runtime (Apple libm,
UCRT, glibc), which need not agree in the last bit; double.AtanPi/Atan2Pi/PowPi do the same. A byte golden or a recorded hash
taken from such a value is keyed to one platform (docs/lessons/defect-classes.md, CRT-GOLDEN). This lint scans an EXPLICIT
file list (FILES below) for a banned call outside comments and string literals. A line may carry
`crt-allowed: <reason>` (a reason is required) when the value never reaches a recorded byte and is compared with a tolerance.
Adding a file to FILES is the review act: list a file when its output reaches a committed hash, never by directory.

Ring: fast (runs inside tools/check-docs.py, every join). Cost: under 0.1 s (reads two source files).
`--self-test` plants a banned call, an allowed line, a comment and a string, and checks each verdict.
"""

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

# Explicit list. Catalog.cs: the generator behind the shipped NACA bytes. Placement.cs: the display spacing and the twist angle behind placement-surface-bits.
FILES = (
    "src/CfdWorkbench.Core/Catalog.cs",
    "src/CfdWorkbench.Core/Placement.cs",
)

BANNED = re.compile(
    r"\b(?:Math\.(?:Sin|Cos|Tan|Atan|Atan2|Asin|Acos|Sinh|Cosh|Tanh|Pow|Exp|Log|Log2|Log10|SinCos|Cbrt)"
    r"|double\.(?:Atan|Atan2|Pow)Pi)\b"
)
ALLOWED = re.compile(r"crt-allowed:\s*\S")


def code_part(line):
    """The line without string literals and // comments (the allow marker is read from the raw line)."""
    line = re.sub(r'"(?:\\.|[^"\\])*"', '""', line)
    return line.split("//", 1)[0]


def offences(text):
    found = []
    for number, raw in enumerate(text.splitlines(), 1):
        match = BANNED.search(code_part(raw))
        if match and not ALLOWED.search(raw):
            found.append((number, match.group(0)))
    return found


def scan(root, files):
    problems = []
    for relative in files:
        path = root / relative
        if not path.is_file():
            problems.append(f"{relative}: listed file is missing (remove it from FILES or restore it)")
            continue
        for number, call in offences(path.read_text(encoding="utf-8")):
            problems.append(f"{relative}:{number}: {call} calls the C runtime; use + - * / sqrt, double.CosPi/SinCosPi, or mark `crt-allowed: <reason>`")
    return problems


def self_test():
    planted = (
        "double a = Math.Cos(Math.PI * i / 80.0);\n"
        "double b = Math.Atan2(y, x); // crt-allowed: display degrees, compared within 1e-12\n"
        "// Math.Sin is only mentioned here\n"
        'string c = "Math.Pow";\n'
        "double d = double.CosPi(i / 80.0) + Math.Sqrt(2);\n"
        "double e = Math.Pow(2, 3); // crt-allowed:\n"
        "double f = double.Atan2Pi(y, x);\n"
    )
    got = offences(planted)
    expected = [(1, "Math.Cos"), (6, "Math.Pow"), (7, "double.Atan2Pi")]
    ok = got == expected
    print("SELFTEST " + ("PASS" if ok else "FAIL") + f" planted shapes: expected {expected}, got {got}")
    with tempfile.TemporaryDirectory() as folder:
        root = Path(folder)
        missing = scan(root, ("absent.cs",))
        found = bool(missing) and "missing" in missing[0]
        print("SELFTEST " + ("PASS" if found else "FAIL") + " a missing listed file is reported")
        ok = ok and found
    return 0 if ok else 1


def main(argv):
    if argv == ["--self-test"]:
        return self_test()
    problems = scan(ROOT, FILES)
    for problem in problems:
        print("CRT-TRANSCENDENTAL " + problem)
    if problems:
        return 1
    print(f"check-crt-transcendentals: {len(FILES)} file(s) clean")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
