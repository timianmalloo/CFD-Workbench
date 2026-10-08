#!/usr/bin/env python3
"""WINDOWS-TEXT-MODE-HASH control: a file that hashes bytes must not write text without newline=.

On Windows, write_text and open(..., "w") turn "\\n" into "\\r\\n" unless newline="\\n" is given, so a hash taken from
the written bytes matches no committed LF input (PR #4). This lint scans tools/**/*.py and cases/tools/**/*.py for a file
that both hashes bytes (hashlib or sha256sum) and calls write_text( or a text-mode open (w, wt, a, at, x) without a
newline= argument. Allowlist entries carry a reason; a stale entry also exits 1, so the list only shrinks.
`--self-test` plants one offender and one clean file.
"""

import ast
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
SCAN_DIRS = ("tools", "cases/tools")
TEXT_WRITE_MODES = {"w", "wt", "w+", "a", "at", "a+", "x", "xt"}

# Tracked exceptions: relative path -> reason. Each must stay true; delete the entry when the file is fixed.
ALLOWLIST = {
    "cases/tools/launcher-record.py":
        "writes only its own record files (tree.json, preflight), which are read back as text; the bytes it hashes "
        "(cfdw-manifest.json) are written by the generators, which carry newline=\"\\n\"",
}


def hashes_bytes(text):
    return "hashlib" in text or "sha256sum" in text


def text_writes(text):
    """Line numbers of write_text/open calls in text mode without newline=."""
    lines = []
    for node in ast.walk(ast.parse(text)):
        if not isinstance(node, ast.Call):
            continue
        if any(keyword.arg == "newline" for keyword in node.keywords):
            continue
        func = node.func
        name = func.attr if isinstance(func, ast.Attribute) else getattr(func, "id", "")
        if name == "write_text":
            lines.append(node.lineno)
        elif name == "open":
            modes = [arg.value for arg in node.args[:2] if isinstance(arg, ast.Constant) and isinstance(arg.value, str)]
            modes += [keyword.value.value for keyword in node.keywords
                      if keyword.arg == "mode" and isinstance(keyword.value, ast.Constant)]
            if any(mode in TEXT_WRITE_MODES for mode in modes):
                lines.append(node.lineno)
    return sorted(lines)


def findings(text):
    return text_writes(text) if hashes_bytes(text) else []


def scan(root):
    results = {}
    for directory in SCAN_DIRS:
        for path in sorted((root / directory).rglob("*.py")):
            if any(part in ("bin", "obj", "__pycache__") for part in path.parts):
                continue
            lines = findings(path.read_text(encoding="utf-8", errors="replace"))
            if lines:
                results[path.relative_to(root).as_posix()] = lines
    return results


def problems(results, allowlist):
    found = []
    for relative, lines in sorted(results.items()):
        if relative not in allowlist:
            found.append(f"WINDOWS-TEXT-MODE-HASH: {relative}: text-mode write without newline= at line(s) "
                         + ", ".join(map(str, lines)) + " in a file that hashes bytes")
    for relative in sorted(set(allowlist) - set(results)):
        found.append(f"TEXT-MODE-ALLOWLIST-STALE: {relative} (fixed or moved; delete the entry)")
    return found


def self_test():
    offender = ("import hashlib\nfrom pathlib import Path\np = Path('x')\n"
                "p.write_text('a')\nopen('y', 'w').write('b')\nh = hashlib.sha256(p.read_bytes())\n")
    assert findings(offender) == [4, 5], findings(offender)
    clean = ("import hashlib\nfrom pathlib import Path\np = Path('x')\n"
             "p.write_text('a', newline='\\n')\nopen('y', 'wb').write(b'b')\nopen('z', 'w', newline='\\n')\n"
             "h = hashlib.sha256(p.read_bytes())\n")
    assert findings(clean) == [], findings(clean)
    no_hash = "from pathlib import Path\nPath('x').write_text('a')\n"
    assert findings(no_hash) == [], findings(no_hash)
    assert len(problems({"a.py": [4]}, {})) == 1
    assert problems({"a.py": [4]}, {"a.py": "why"}) == []
    assert len(problems({}, {"a.py": "why"})) == 1
    print("check-text-mode-hash self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    results = scan(ROOT)
    found = problems(results, ALLOWLIST)
    for line in found:
        print(line)
    if found:
        print('Add newline="\\n" to the write (or write bytes), or allowlist the file with a reason.', file=sys.stderr)
        return 1
    print(f"text-mode hash ok: {len(results)} file(s) allowlisted with a reason, none unfixed", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
