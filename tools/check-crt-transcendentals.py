#!/usr/bin/env python3
"""CRT-TRANSCENDENTAL control (Ruling 156 (3)(c)): a file whose output is a committed byte or hash must not call a C-runtime transcendental.

.NET forwards Math.Sin/Cos/Tan/Atan/Atan2/Pow/Exp/Log and the other transcendentals to the platform C runtime (Apple libm,
UCRT, glibc), which need not agree in the last bit; double.AtanPi/Atan2Pi/PowPi do the same. A byte golden or a recorded hash
taken from such a value is keyed to one platform (docs/lessons/defect-classes.md, CRT-GOLDEN). This lint scans an EXPLICIT
file list (FILES below) for a banned call outside comments and string literals. A line may carry
`crt-allowed: Ruling <n> — <reason>` (a reason is required) when the value never reaches a recorded byte and is compared with a tolerance.
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

# Allowed markers per file. A new escape is a change to this gate, which is reviewed as one.
ALLOWED_COUNT = {
    "src/CfdWorkbench.Core/Catalog.cs": 1,
    "src/CfdWorkbench.Core/Placement.cs": 0,
}

NAMES = (
    "Sin|Cos|Tan|Asin|Acos|Atan|Atan2|SinCos|Sinh|Cosh|Tanh|Asinh|Acosh|Atanh|Exp|Exp2|Exp10|ExpM1|Log|Log2|Log10|LogP1"
    "|Pow|Cbrt|RootN|AsinPi|AcosPi|AtanPi|Atan2Pi"
)
# CosPi, SinPi, SinCosPi and Sqrt are not in NAMES; the trailing \b keeps Cos from matching CosPi.
BANNED = re.compile(r"\b(?:Math|MathF|double)\.(?:" + NAMES + r")\b")
# A bare Cos(x) needs `using static System.Math`, so the using line is the check (a bare name also matches the interface method SinCos).
USING_STATIC = re.compile(r"\busing\s+static\s+System\.(?:Math|MathF|Double)\b")
ALLOWED = re.compile(r"crt-allowed:\s*Ruling\s+\d+\s+[—-]\s*\S")


def split_line(line):
    """(code, comment): string literals blanked first, so a marker or a call inside a string counts for nothing."""
    line = re.sub(r'"(?:\\.|[^"\\])*"', '""', line)
    code, _, comment = line.partition("//")
    return code, comment


def offences(text):
    """(flagged, allowed): flagged is [(line, call)] for banned calls without a valid marker; allowed counts valid markers used."""
    found, allowed = [], 0
    for number, raw in enumerate(text.splitlines(), 1):
        code, comment = split_line(raw)
        match = USING_STATIC.search(code) or BANNED.search(code)
        if not match:
            continue
        if ALLOWED.search(comment):
            allowed += 1
        else:
            found.append((number, match.group(0).strip()))
    return found, allowed


def scan(root, files):
    problems = []
    for relative in files:
        path = root / relative
        if not path.is_file():
            problems.append(f"{relative}: listed file is missing (remove it from FILES or restore it)")
            continue
        flagged, allowed = offences(path.read_text(encoding="utf-8"))
        for number, call in flagged:
            problems.append(f"{relative}:{number}: {call} calls the C runtime; use + - * / sqrt, double.CosPi/SinCosPi, or mark `// crt-allowed: Ruling <n> — <reason>`")
        pinned = ALLOWED_COUNT.get(relative, 0)
        if allowed != pinned:
            problems.append(f"{relative}: {allowed} allowed marker(s), the gate pins {pinned}; a new escape is a change to ALLOWED_COUNT in this gate")
    return problems


def self_test():
    planted = (
        "double a = Math.Cos(Math.PI * i / 80.0);\n"                                    # 1 flagged
        "double b = Math.Atan2(y, x); // crt-allowed: Ruling 156 — display degrees\n"   # 2 allowed
        "// Math.Sin is only mentioned here\n"                                          # 3 comment
        'string c = "Math.Pow";\n'                                                      # 4 string
        "double d = double.CosPi(i / 80.0) + Math.Sqrt(2) + double.SinCosPi(t).Sin;\n"  # 5 clean
        "double e = Math.Pow(2, 3); // crt-allowed:\n"                                  # 6 flagged: no ruling
        "double f = double.Atan2Pi(y, x);\n"                                            # 7 flagged
        "double g = double.Cos(x);\n"                                                   # 8 flagged
        "double h = double.Atan2(y, x);\n"                                              # 9 flagged
        "double i = double.Exp(x);\n"                                                   # 10 flagged
        "double j = double.AcosPi(x);\n"                                                # 11 flagged
        "using static System.Math;\n"                                                   # 12 flagged
        "float l = MathF.Sin(x);\n"                                                     # 13 flagged
        'double m = Math.Cos(x) + "crt-allowed: Ruling 156 — x".Length;\n'              # 14 flagged: marker in a string
        "double n = Math.Cos(x); // crt-allowed: because\n"                             # 15 flagged: no ruling form
    )
    got, allowed = offences(planted)
    expected = [(1, "Math.Cos"), (6, "Math.Pow"), (7, "double.Atan2Pi"), (8, "double.Cos"), (9, "double.Atan2"),
                (10, "double.Exp"), (11, "double.AcosPi"), (12, "using static System.Math"),
                (13, "MathF.Sin"), (14, "Math.Cos"), (15, "Math.Cos")]
    ok = got == expected and allowed == 1
    print("SELFTEST " + ("PASS" if ok else "FAIL") + f" planted shapes: expected {expected}, got {got}, allowed {allowed}")
    with tempfile.TemporaryDirectory() as folder:
        root = Path(folder)
        missing = scan(root, ("absent.cs",))
        found = bool(missing) and "missing" in missing[0]
        print("SELFTEST " + ("PASS" if found else "FAIL") + " a missing listed file is reported")
        (root / "extra.cs").write_text("double b = Math.Atan2(y, x); // crt-allowed: Ruling 156 — one\n", encoding="utf-8", newline="\n")
        pinned = scan(root, ("extra.cs",))
        found_pin = len(pinned) == 1 and "pins 0" in pinned[0]
        print("SELFTEST " + ("PASS" if found_pin else "FAIL") + " an allowed marker beyond the pinned count is reported")
        ok = ok and found and found_pin
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
