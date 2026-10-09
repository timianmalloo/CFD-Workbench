#!/usr/bin/env python3
"""WSL-INLINE-ARGV control: WSL runs a committed LF script file, never an inline `bash -c` string.

PowerShell -> wsl.exe -> bash re-quotes at each boundary, so an inline command string loses or expands arguments
(PR #11, Ruling 153). This lint fails a tracked .ps1, .py, .sh or .cmd file where a statement names wsl / wsl.exe and
then runs bash or sh with a -c style flag (-c, -lc, -ic, -cl, also behind -e or --exec). Calling a script FILE passes
(`wsl.exe -- bash /mnt/c/.../x.sh arg`, `bash -l x.sh`). Comments and Python triple-quoted strings are ignored. The
wsl token may sit up to two lines above the bash token, which covers a wrapped PowerShell or Python argv list.
Known limit: `bash` and its `-c` flag must share a line.
Pinned historical proof files go in ALLOWLIST (key: file + stripped line, one reason each); a stale entry also exits 1,
so the list only shrinks. `--self-test` plants every shape. Fast ring, no process spawn beyond `git ls-files`.
"""

import re
import subprocess
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
SUFFIXES = (".ps1", ".py", ".sh", ".cmd")
SELF = "tools/check-wsl-inline.py"
WSL = re.compile(r"(?<![\w./-])wsl(?:\.exe)?(?![\w/-])", re.IGNORECASE)
INLINE = re.compile(r"""(?<![\w.-])(?:ba)?sh['"]?[\s,]+['"]?-[A-Za-z]*c[A-Za-z]*['"]?(?=[\s,\])'"]|$)""")
TRIPLE = re.compile(r'"""[\s\S]*?"""|\'\'\'[\s\S]*?\'\'\'')

# (file relative to the repo root, stripped source line) -> reason. Pinned proof files are never edited (an edit breaks
# their capture manifest), so each historical hit is listed here once.
ALLOWLIST = {}


def code_lines(name, text):
    """Source lines with comments and (for .py) triple-quoted strings blanked; line count is preserved."""
    if name.endswith(".py"):
        text = TRIPLE.sub(lambda m: re.sub(r"[^\n]", " ", m.group(0)), text)
    lines = []
    for line in text.splitlines():
        stripped = line.lstrip()
        if name.endswith(".cmd") and re.match(r"(?i)(rem\b|::)", stripped):
            line = ""
        else:
            line = re.sub(r"(^|\s)#.*$", r"\1", line)
        lines.append(line)
    return lines


def findings(name, text):
    """[(line number, stripped line)] for each inline WSL bash command in one file's text."""
    lines = code_lines(name, text)
    found = []
    for number, line in enumerate(lines):
        if not INLINE.search(line):
            continue
        window = "\n".join(lines[max(0, number - 2):number + 1])
        if WSL.search(window):
            found.append((number + 1, text.splitlines()[number].strip()))
    return found


def scan(root):
    listed = subprocess.run(["git", "ls-files", "-z"], cwd=root, capture_output=True, text=True, encoding="utf-8",
                            check=True, timeout=60).stdout.split("\0")
    results = []
    for relative in sorted(listed):
        if not relative.endswith(SUFFIXES) or relative == SELF:
            continue
        path = root / relative
        if not path.is_file():
            continue
        for number, line in findings(relative, path.read_text(encoding="utf-8", errors="replace")):
            results.append((relative, number, line))
    return results


def problems(results, allowlist):
    found = []
    keys = {(relative, line) for relative, _, line in results}
    for relative, number, line in results:
        if (relative, line) not in allowlist:
            found.append(f"WSL-INLINE-ARGV: {relative}:{number}: {line} (run a committed LF script file through WSL)")
    for relative, line in sorted(set(allowlist) - keys):
        found.append(f"WSL-INLINE-ALLOWLIST-STALE: {relative}: {line} (fixed or moved; delete the entry)")
    return found


def self_test():
    cases = [
        ("a.ps1", "wsl.exe --distribution d --exec /bin/bash -lc 'cartesianMesh -help'\n", 1),
        ("a.ps1", "& wsl.exe -d d -- bash -c \"echo $x\"\n", 1),
        ("a.py", "run(['wsl.exe', 'bash', '-lc', cmd])\n", 1),
        ("a.py", "WSL = ['wsl.exe', '--exec']\nrun(WSL + ['bash', '-lc', cmd])\n", 1),
        ("a.py", "subprocess.run([\n    'wsl.exe', '-d', 'x',\n    'sh', '-c', cmd,\n])\n", 1),
        ("a.cmd", "wsl -d d -e bash -c \"echo %X%\"\n", 1),
        ("a.sh", "wsl.exe -- sh -c 'echo $1' x\n", 1),
        ("a.ps1", "wsl.exe -- bash /mnt/c/x/probe.sh arg\n", 0),
        ("a.sh", "wsl.exe -- bash -l /mnt/c/x/probe.sh\n", 0),
        ("a.py", "run(wsl + ['env', '-i', 'bash', '--noprofile', '--norc', script, linux])\n", 0),
        ("a.ps1", "$r = (& $bash -c 'cat /proc/loadavg')\n", 0),  # Git Bash, no WSL
        ("a.ps1", "# wsl.exe bash -lc 'x' is forbidden\n", 0),
        ("a.cmd", "REM wsl -e bash -c x\n", 0),
        ("a.py", '"""Never run wsl.exe bash -lc "cmd" inline."""\n', 0),
        ("a.py", "p = '/mnt/c/Users/x/AppData'  # wsl path\nq = 'sh -c' \n", 0),
        ("a.sh", "echo /usr/lib/wsl/lib/nvidia-smi; bash -c 'true'\n", 0),
    ]
    for name, text, want in cases:
        got = findings(name, text)
        assert len(got) == want, (name, text, got, want)
    results = [("p.ps1", 3, "wsl.exe bash -lc 'x'")]
    assert len(problems(results, {})) == 1
    assert problems(results, {("p.ps1", results[0][2]): "pinned proof of Ruling 0; historical"}) == []
    assert len(problems([], {("p.ps1", "gone"): "r"})) == 1
    print("check-wsl-inline self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    results = scan(ROOT)
    bad = problems(results, ALLOWLIST)
    if bad:
        print("\n".join(bad), file=sys.stderr)
        return 1
    print(f"wsl inline ok: {len(results)} pinned hit(s) allowlisted, no inline `bash -c` through WSL", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
