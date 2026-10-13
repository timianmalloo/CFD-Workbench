#!/usr/bin/env python3
"""READER-SHARE guard: a product reader opens a user file with delete sharing.

On Windows a reader opened with FileShare.Read (or by File.ReadAll*, File.OpenRead, File.OpenText, which share
Read only) blocks a concurrent POSIX-style replace of the file (NativeFailure Win32 32, NTSTATUS 0xC0000043,
Ruling 145 (1)). A FILE_SHARE_READ|FILE_SHARE_DELETE reader keeps its old bytes while the replace succeeds.

Fails any src/**/*.cs line that
  * names `FileShare.Read` not followed by `| FileShare.Delete` (FileShare.ReadWrite and
    FileShare.ReadWrite|Delete are different tokens and never match), or
  * calls File.ReadAllBytes / ReadAllText / ReadAllLines / ReadLines (and the Async forms), File.OpenRead or File.OpenText.
The fix is one shared opener, CfdWorkbench.Persistence.UserFile.OpenRead.

Readers vs writers: a FileShare.Read hit is skipped when its statement (the line through the first line ending in
`;`, at most 6 lines) opens for writing -- it names FileAccess.Write or FileMode.Append/Create/CreateNew/Truncate and
does not name FileAccess.Read. A File.Read* / OpenRead / OpenText call is always a reader. Comment lines are skipped.

Ring: fast (every push), run from tools/check-docs.py. Cost: one pass over src/**/*.cs, 0.3 s wall measured
(docs/proof/wsf/red-first.md); the PowerShell pass is in docs/proof/rsx/red-first.md.

PowerShell (Ruling 229, sibling of the same class): docs/proof/**/*.ps1 and tools/**/*.ps1 are scanned too.
Rule chosen: the writer is not decidable statically, so a default-share read is flagged when its path is "live".
Read shapes: [IO.File]::ReadAllText/ReadAllLines/ReadAllBytes/OpenRead/OpenText, [IO.File]::Open(...) without a
FileShare token, and Get-Content. A read is "live" when its line, or the line that assigns a variable it uses,
names a live directory (word "live", any case) or a *.jsonl file. FileStream readers are never flagged (they
choose sharing explicitly). Allow-marker on the same or the previous line: `# reader-share: reviewed <ruling>`.
Report mode: until PS_ENFORCE_FROM (the PC repair of Ruling 229 joins by then) PowerShell hits print as
READER-SHARE-PS REPORT and do not fail; on and after that date they fail. Chosen over markers-only because markers
on the PC scripts would edit frozen proof bytes, which the Mac must not do.

Usage: check-reader-sharing.py [--self-test] [--root DIR] [--enforce-ps]  (--enforce-ps: PowerShell hits fail now)
"""
from __future__ import annotations

import datetime
import re
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

SHARE_READ = re.compile(r"\bFileShare\.Read\b(?!\s*\|\s*FileShare\.Delete\b)")
READ_CALL = re.compile(r"\bFile\.(?:ReadAll(?:Bytes|Text|Lines)(?:Async)?|ReadLines|OpenRead|OpenText)\b")
WRITE_MODE = re.compile(r"\bFileAccess\.Write\b|\bFileMode\.(?:Append|Create|CreateNew|Truncate)\b")
READ_ACCESS = re.compile(r"\bFileAccess\.Read\b")
STATEMENT_LINES = 6

PS_ENFORCE_FROM = datetime.date(2026, 10, 20)  # after the PC repair of Ruling 229 joins; hits then fail
PS_READ = re.compile(
    r"\[(?:System\.)?IO\.File\]::(?:ReadAll(?:Text|Lines|Bytes)|OpenRead|OpenText)\b"
    r"|\[(?:System\.)?IO\.File\]::Open\((?![^)]*FileShare)"
    r"|\bGet-Content\b", re.IGNORECASE)
PS_LIVE = re.compile(r"\blive\b|\.jsonl\b", re.IGNORECASE)
PS_ASSIGN = re.compile(r"^\s*(\$[\w:]+)\s*=(.*)$")
PS_VAR = re.compile(r"\$[\w:]+")
PS_MARKER = re.compile(r"#\s*reader-share:\s*reviewed\s+\S+", re.IGNORECASE)


def _statement(lines: list[str], i: int) -> str:
    chunk = []
    for line in lines[i:i + STATEMENT_LINES]:
        chunk.append(line)
        if line.rstrip().endswith(";"):
            break
    return " ".join(chunk)


def scan_text(text: str) -> list[tuple[int, str]]:
    """Return (1-based line, source line) for each offending line."""
    lines = text.splitlines()
    hits = []
    for i, line in enumerate(lines):
        if line.lstrip().startswith("//"):
            continue
        code = line.split("//", 1)[0]
        if READ_CALL.search(code):
            hits.append((i + 1, line.strip()))
        elif SHARE_READ.search(code):
            stmt = _statement(lines, i)
            if WRITE_MODE.search(stmt) and not READ_ACCESS.search(stmt):
                continue
            hits.append((i + 1, line.strip()))
    return hits


def scan_ps_text(text: str) -> list[tuple[int, str]]:
    """Return (1-based line, source line) for each live default-share PowerShell read."""
    lines = text.splitlines()
    live_vars: set[str] = set()
    hits = []
    for i, line in enumerate(lines):
        if line.lstrip().startswith("#"):
            continue
        m = PS_ASSIGN.match(line)
        if m and PS_LIVE.search(m.group(2)):
            live_vars.add(m.group(1).lower())
        if not PS_READ.search(line):
            continue
        if PS_MARKER.search(line) or (i > 0 and PS_MARKER.search(lines[i - 1])):
            continue
        used = {v.lower() for v in PS_VAR.findall(line)}
        if PS_LIVE.search(line) or used & live_vars:
            hits.append((i + 1, line.strip()))
    return hits


def scan_ps_tree(root: Path) -> dict[str, list[tuple[int, str]]]:
    found = {}
    for sub in ("docs/proof", "tools"):
        for path in sorted((root / sub).rglob("*.ps1")):
            hits = scan_ps_text(path.read_text(encoding="utf-8", errors="replace"))
            if hits:
                found[path.relative_to(root).as_posix()] = hits
    return found


def scan_tree(root: Path) -> dict[str, list[tuple[int, str]]]:
    found = {}
    for path in sorted((root / "src").rglob("*.cs")):
        if any(part in ("bin", "obj") for part in path.parts):
            continue
        hits = scan_text(path.read_text(encoding="utf-8", errors="replace"))
        if hits:
            found[path.relative_to(root).as_posix()] = hits
    return found


def self_test() -> int:
    offenders = {
        "share-read": "using var s = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);",
        "share-read-multiline": "await using var s = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read,\n    bufferSize: 8192);",
        "share-read-then-other": "new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write);",
        "read-all-bytes": "var b = File.ReadAllBytes(p);",
        "read-all-bytes-async": "var b = await File.ReadAllBytesAsync(p);",
        "read-all-text": "var t = File.ReadAllText(p);",
        "read-all-lines": "var t = File.ReadAllLines(p);",
        "open-read": "using var s = File.OpenRead(p);",
        "open-text": "using var s = File.OpenText(p);",
    }
    clean = {
        "read-delete": "new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);",
        "read-delete-spaced": "new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read|FileShare.Delete)",
        "readwrite-append-writer": "new FileStream(p, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);",
        "share-read-writer": "using var s = new FileStream(p, FileMode.Create, FileAccess.Write, FileShare.Read);",
        "share-read-writer-multiline": "using var s = new FileStream(p, FileMode.Append,\n    FileAccess.Write, FileShare.Read);",
        "comment": "// File.ReadAllBytes(p) shares Read only; FileShare.Read",
        "write-all": "File.WriteAllBytes(p, b);",
        "trailing-comment": "var x = 1; // File.ReadAllText(p)",
    }
    ps_offenders = {
        "ps-readalltext-var": "$events = Join-Path $Live 'x-events.jsonl'\n$raw = [IO.File]::ReadAllText($events)",
        "ps-get-content-live": "$s = Get-Content -LiteralPath (Join-Path $Live 'state.json') -Raw",
        "ps-open-default": "$f = [System.IO.File]::Open('C:\\a\\events.jsonl', 'Open')",
    }
    ps_clean = {
        "ps-filestream-share": "$f = New-Object IO.FileStream($p, 'Open', 'Read', 'ReadWrite,Delete') # events.jsonl live",
        "ps-open-share": "$f = [IO.File]::Open($p, 'Open', 'Read', [IO.FileShare]'ReadWrite,Delete') # live",
        "ps-marked-same-line": "$r = [IO.File]::ReadAllText($live)  # reader-share: reviewed Ruling 229",
        "ps-marked-prev-line": "# reader-share: reviewed Ruling 229\n$r = Get-Content $env:LIVE\\events.jsonl",
        "ps-not-live": "$r = [IO.File]::ReadAllText($config)",
        "ps-comment": "# $r = [IO.File]::ReadAllText($live\\events.jsonl)",
    }
    bad = 0
    for label, text in ps_offenders.items():
        if not scan_ps_text(text):
            print(f"SELF-TEST FAIL: PowerShell offender not caught: {label}")
            bad += 1
    for label, text in ps_clean.items():
        if scan_ps_text(text):
            print(f"SELF-TEST FAIL: PowerShell clean text flagged: {label}: {scan_ps_text(text)}")
            bad += 1
    for label, text in offenders.items():
        if not scan_text(text):
            print(f"SELF-TEST FAIL: offender not caught: {label}")
            bad += 1
    for label, text in clean.items():
        if scan_text(text):
            print(f"SELF-TEST FAIL: clean text flagged: {label}: {scan_text(text)}")
            bad += 1
    if not bad:
        print(f"READER-SHARE self-test ok: {len(offenders)}+{len(ps_offenders)}ps offenders caught, "
              f"{len(clean)}+{len(ps_clean)}ps clean passed")
    return 1 if bad else 0


def main(argv: list[str]) -> int:
    if "--self-test" in argv:
        return self_test()
    root = Path(argv[argv.index("--root") + 1]) if "--root" in argv else Path(__file__).resolve().parent.parent
    found = scan_tree(root)
    for rel, hits in found.items():
        for number, line in hits:
            print(f"READER-SHARE: {rel}:{number}: {line}")
    ps_found = scan_ps_tree(root)
    enforcing = "--enforce-ps" in argv or datetime.date.today() >= PS_ENFORCE_FROM
    for rel, hits in ps_found.items():
        for number, line in hits:
            print(f"READER-SHARE-PS {'FAIL' if enforcing else 'REPORT'}: {rel}:{number}: {line}")
    if ps_found and enforcing:
        print("READER-SHARE-PS FAIL: open live files with FileStream(..., FileShare.ReadWrite|Delete), "
              "or mark a reviewed read with `# reader-share: reviewed <ruling>`")
    elif ps_found:
        print(f"READER-SHARE-PS report mode until {PS_ENFORCE_FROM}: {sum(map(len, ps_found.values()))} live "
              "default-share reads (PC repairs Ruling 229); not failing")
    if found:
        print("READER-SHARE FAIL: open user files with CfdWorkbench.Persistence.UserFile.OpenRead "
              "(FileShare.Read | FileShare.Delete)")
        return 1
    if ps_found and enforcing:
        return 1
    print("READER-SHARE ok: 0 readers without delete sharing")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
