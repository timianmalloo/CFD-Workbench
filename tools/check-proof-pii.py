#!/usr/bin/env python3
"""PROOF-PII guard: no Windows user name or machine SID in a committed text file.

Fails any tracked text file that carries
  * a Windows home path (a drive letter, a Users directory and an account name, in backslash,
    JSON-escaped, forward-slash or /mnt/<drive> form) whose account is not a placeholder
    (%USERPROFILE%, $HOME, <name>, <user>, ...), or
  * a machine SID (S-1-5-21-<n>-<n>-<n>, optional RID) -- write S-1-5-21-<machine>-<RID> instead.
The macOS home (/Users/<name> with no drive prefix) is out of scope and never fires.

Ring: fast (every push), run from tools/check-docs.py. Cost: one pass over `git ls-files`
(measured and recorded in docs/proof/pii/red-first.md). Git history is not rewritten
(operator decision, Ruling 145 (5)); the guard covers the current tree only.

Usage: check-proof-pii.py [--self-test] [--root DIR]
"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

# A drive-letter or /mnt/<x> prefix is what separates a Windows home from the macOS one.
WIN_USER = re.compile(
    r"(?:\b[A-Za-z]:|/mnt/[a-z])(?:\\\\|\\|/)+Users(?:\\\\|\\|/)+([^\\/\s\"'`<>|*?:;,)\]}]+|<[^>\s]*>)",
    re.IGNORECASE,
)
SID = re.compile(r"S-1-5-21-\d+-\d+-\d+(?:-\d+)?")
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

SAFE_NAMES = {"public", "default", "all users", "default user"}

# Shrink-only: path -> reason. Remove an entry when its file is scrubbed; never add one
# without an operator-visible reason.
ALLOWLIST: dict[str, str] = {
    "docs/ai-forward-pack/scripts/verify-no-machine-paths.py":
        "pack self-test fixture with the dummy name x (not a real account); pack-owned file",
}


def is_placeholder(name: str) -> bool:
    n = name.strip()
    if n.startswith(("<", "%", "$", "{")):
        return True
    return n.lower() in SAFE_NAMES


def scan_text(text: str) -> list[str]:
    hits = []
    for m in WIN_USER.finditer(text):
        if not is_placeholder(m.group(1)):
            hits.append(f"windows-user-path:{m.group(0)}")
    for m in SID.finditer(text):
        hits.append(f"machine-sid:{m.group(0)}")
    return hits


def scan_tree(root: Path) -> dict[str, list[str]]:
    out = subprocess.run(
        ["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True
    ).stdout.decode("utf-8", "surrogateescape")
    found: dict[str, list[str]] = {}
    for rel in filter(None, out.split("\0")):
        p = root / rel
        try:
            data = p.read_bytes()
        except OSError:
            continue
        if b"\0" in data[:8192]:
            continue
        hits = scan_text(data.decode("utf-8", "replace"))
        if hits:
            found[rel] = hits
    return found


def self_test() -> int:
    # Fixtures are assembled from parts so this file never holds a contiguous offender itself.
    bs, jbs, u, who = "\\", "\\\\", "Users", "jdoe"
    sid = "S-1-5-" + "21-1111111111-2222222222-3333333333"
    offenders = {
        "backslash": f"C:{bs}{u}{bs}{who}{bs}AppData{bs}Local",
        "json-escaped": f"C:{jbs}{u}{jbs}{who}{jbs}.dotnet",
        "forward": f"C:/{u}/{who}/.gitconfig",
        "wsl": f"/mnt/c/{u}/{who}/AppData",
        "lowercase": f"c:{bs}users{bs}{who}{bs}x",
        "sid": f"owner {sid}-1001",
        "sid-no-rid": sid,
    }
    clean = {
        "mac home": f"/{u}/mallalieut/projects/CFD-Workbench",
        "userprofile": f"%USERPROFILE%{bs}AppData{bs}Local",
        "home": "$HOME/AppData/Local",  # machine-path-ok: clean-text fixture, a placeholder
        "name placeholder": f"C:{bs}{u}{bs}<name>{bs}x and C:{jbs}{u}{jbs}<user>{jbs}y",
        "sid placeholder": "S-1-5-21-<machine>-1001",
        "public": f"C:{bs}{u}{bs}Public{bs}Documents",
    }
    bad = 0
    for label, text in offenders.items():
        if not scan_text(text):
            print(f"SELF-TEST FAIL: offender not caught: {label}")
            bad += 1
    for label, text in clean.items():
        if scan_text(text):
            print(f"SELF-TEST FAIL: clean text flagged: {label}: {scan_text(text)}")
            bad += 1
    if not bad:
        print(f"PROOF-PII self-test ok: {len(offenders)} offenders caught, {len(clean)} clean passed")
    return 1 if bad else 0


def main(argv: list[str]) -> int:
    if "--self-test" in argv:
        return self_test()
    root = Path(argv[argv.index("--root") + 1]) if "--root" in argv else Path(__file__).resolve().parent.parent
    found = scan_tree(root)
    stale = sorted(p for p in ALLOWLIST if p not in found)
    failing = {p: h for p, h in found.items() if p not in ALLOWLIST}
    for p in stale:
        print(f"PROOF-PII: stale allowlist entry (shrink it): {p}")
    for p in sorted(failing):
        print(f"PROOF-PII: {p}: {len(failing[p])} hit(s), first: {failing[p][0]}")
    if failing or stale:
        print("PROOF-PII FAIL: write %USERPROFILE% / $HOME / <name> and S-1-5-21-<machine>-<RID>")
        return 1
    print(f"PROOF-PII ok: 0 Windows user paths or machine SIDs ({len(ALLOWLIST)} allowlisted)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
