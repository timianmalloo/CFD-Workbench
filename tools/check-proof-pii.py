#!/usr/bin/env python3
"""PROOF-PII guard: no Windows user name, machine SID or machine hostname in a committed text file.

Fails any tracked text file that carries
  * a Windows home path (a drive letter, a Users directory and an account name, in backslash,
    JSON-escaped, forward-slash or /mnt/<drive> form) whose account is not a placeholder
    (%USERPROFILE%, $HOME, <name>, <user>, ...), or
  * a machine SID (S-1-5-21-<n>-<n>-<n>, optional RID) -- write S-1-5-21-<machine>-<RID> instead, or
  * a machine hostname (Ruling 181 (5)): a Windows default name (DESKTOP-/LAPTOP- plus 7, WIN- plus 11),
    COMPUTERNAME=<name>, "MachineName": "<name>", a systeminfo "Host Name:" line, a UNC host prefix, or a
    literal from the run-time environment list CFD_PII_HOSTNAMES (comma-separated, never committed).
    Write <host>, <machine> or %COMPUTERNAME% instead. Hits print the name masked (first two characters).
The macOS home (/Users/<name> with no drive prefix) is out of scope and never fires.

Ring: fast (every push), run from tools/check-docs.py. Cost: one pass over `git ls-files`
(measured and recorded in docs/proof/pii/red-first.md). Git history is not rewritten
(operator decision, Ruling 145 (5)); the guard covers the current tree only.

Usage: check-proof-pii.py [--self-test] [--root DIR]
"""
from __future__ import annotations

import os
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
# Hostname shapes. Case-sensitive on purpose: Windows default names are upper case, so a repo path
# such as CFD-Workbench-win-... never fires. Each pattern starts with a literal, so the regex engine can skip ahead
# (a leading lookbehind made the pass six times slower); BAD_PREV is checked on the character before the match instead.
_TOK = r"(?<![A-Za-z0-9-])"
DEFAULT_NAME = re.compile(r"((?:DESKTOP|LAPTOP)-[A-Z0-9]{7}|WIN-[A-Z0-9]{11})(?![A-Za-z0-9]|-[A-Za-z0-9])")
CNAME_RX = re.compile(r"COMPUTERNAME[\"']?\s*[=:]\s*[\"']?([A-Za-z0-9][^\s\"'`;,\\]*)")
MACHINE_NAME = re.compile(r"MachineName\\?\"\s*:\s*\\?\"([^\"\\]*)")
HOST_NAME = re.compile(r"^[ \t]*Host Name:[ \t]*(\S[^\r\n]*?)[ \t]*$", re.MULTILINE)
# The OpenFOAM log banner line "Host   : <name>" (the PC's solver logs carry it).
FOAM_HOST = re.compile(r"^Host {2,}:[ \t]*(\S+)", re.MULTILINE)
# A UNC host prefix, raw (two backslashes) and JSON-escaped (four). A preceding word, colon, dot or backslash
# rules out a path tail; a name of one character or one starting with a dot is a string escape or a folder.
UNC_RAW = re.compile(r"\\\\([A-Za-z0-9_][\w.-]+)\\(?!\\)")
UNC_ESC = re.compile(r"\\\\\\\\([A-Za-z0-9_][\w.-]+)\\\\")
BAD_PREV_TOKEN = frozenset("-" + "0123456789" + "abcdefghijklmnopqrstuvwxyz" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
BAD_PREV_UNC = frozenset("\\:._0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")
# (rule, regex, cheap substring pre-filter, characters that rule out a match when they precede it)
HOST_RULES = (
    ("default-name", DEFAULT_NAME, ("DESKTOP-", "LAPTOP-", "WIN-"), BAD_PREV_TOKEN),
    ("computername", CNAME_RX, ("COMPUTERNAME",), frozenset()),
    ("machinename", MACHINE_NAME, ("MachineName",), frozenset()),
    ("host-name-line", HOST_NAME, ("Host Name:",), frozenset()),
    ("foam-banner-host", FOAM_HOST, ("Host ",), frozenset()),
    ("unc-host", UNC_RAW, ("\\\\",), BAD_PREV_UNC),
    ("unc-host", UNC_ESC, ("\\\\\\\\",), BAD_PREV_UNC),
)
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

SAFE_NAMES = {"public", "default", "all users", "default user"}
# Hostname placeholders; wsl.localhost is the WSL UNC root, not a machine.
SAFE_HOSTS = {"localhost", ".", "?", "wsl.localhost"}

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


def is_placeholder_host(name: str) -> bool:
    n = name.strip().strip("\"'")
    if n.startswith(("<", "%", "$", "{")):
        return True
    return n.lower() in SAFE_HOSTS


def mask(name: str) -> str:
    return f"{name[:2]}***({len(name)})"


def env_hostnames() -> list[str]:
    return [h.strip() for h in os.environ.get("CFD_PII_HOSTNAMES", "").split(",") if h.strip()]


def scan_text(text: str, literals: list[str] | None = None) -> list[str]:
    hits = []
    for rule, rx, needles, bad_prev in HOST_RULES:
        if not any(n in text for n in needles):
            continue
        for m in rx.finditer(text):
            if m.start() and text[m.start() - 1] in bad_prev:
                continue
            if not is_placeholder_host(m.group(1)):
                hits.append(f"hostname:{rule}:{mask(m.group(1))}")
    for lit in env_hostnames() if literals is None else literals:
        if re.search(_TOK + re.escape(lit) + r"(?![A-Za-z0-9])", text, re.IGNORECASE):
            hits.append(f"hostname:env-literal:{mask(lit)}")
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
    literals = env_hostnames()
    for rel in filter(None, out.split("\0")):
        p = root / rel
        try:
            data = p.read_bytes()
        except OSError:
            continue
        if b"\0" in data[:8192]:
            continue
        hits = scan_text(data.decode("utf-8", "replace"), literals)
        if hits:
            found[rel] = hits
    return found


def host_fixtures() -> tuple[dict[str, str], dict[str, str], list[str]]:
    """Hostname fixtures: invented names only, assembled from parts so this file holds no contiguous offender."""
    bs, jbs = "\\", "\\\\"
    d, l, w = "DESK" + "TOP-", "LAP" + "TOP-", "WI" + "N-"
    name, cn, mn, hn = "zork", "COMPUTER" + "NAME", "Machine" + "Name", "Host" + " Name"
    offenders = {
        "desktop default": f"capture on {d}ZQXJ7K2 today",
        "laptop default": f"{l}A1B2C3D ok",
        "win default": f"{w}QQ1ZZ2XX3YY ok",
        "computername env": f"{cn}={name}",
        "computername json": f'"{cn}": "{name}"',
        "machinename json": f'{{"{mn}": "{name}"}}',
        "machinename escaped": f'{{\\"{mn}\\": \\"{name}\\"}}',
        "systeminfo host name": f"OS Name: x\n{hn}:                 {name}\nOS Version: y",
        "foam banner": f"Build  : v2\nHost   : {name}\nPID    : 1",
        "unc raw": f"{jbs}{name}{bs}share{bs}file.txt",
        "unc json-escaped": f"{jbs}{jbs}{name}{jbs}share{jbs}file.txt",
        "env literal": f"seen on {name}-pc here",
    }
    clean = {
        "repo path": f"C:{bs}Projects{bs}CFD-Workbench-win-fix{bs}src",
        "pipe name": "VBCSCompiler.exe -pipename:4f2a9c0de1b7a8c3 -keepalive:10",
        "host placeholder": f"{cn}=<host> and {cn}=<machine> and {jbs}<host>{bs}x",
        "env var refs": f"%{cn}%  $env:{cn}  {cn}=%{cn}%  {cn}=$env:{cn}",
        "localhost": f"{cn}=localhost and {jbs}localhost{bs}c$",
        "dot and query": f"{jbs}.{bs}pipe{bs}x and {jbs}?{bs}C:{bs}x",
        "machinename placeholders": f'"{mn}": "<machine>" and "{mn}": "."',
        "host name placeholder": f"{hn}: <host>",
        "foam banner placeholder": "Host   : <host>\nHost   : localhost",
        "prose host": "Host : is described here; the Host key is pc-win",
        "lowercase win": "worktree win-abcdefghijk-fix",
        "wsl root": f"{jbs}wsl.localhost{bs}Ubuntu{bs}home",
        "hyphenated class name": f"{d}HARNESS-GROWTH class",
        "string escape": f'newline=\\"{bs}n\\" and %USERPROFILE%{jbs}{jbs}.dotnet{jbs}{jbs}x',
        "path tail":f"C:{bs}Projects{bs}x and C:{jbs}obj{jbs}x",
    }
    return offenders, clean, [name + "-pc"]


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
    host_off, host_clean, host_literals = host_fixtures()
    offenders.update({f"host {k}": v for k, v in host_off.items()})
    clean.update({f"host {k}": v for k, v in host_clean.items()})
    bad = 0
    for label, text in offenders.items():
        if not scan_text(text, host_literals):
            print(f"SELF-TEST FAIL: offender not caught: {label}")
            bad += 1
    for label, text in clean.items():
        if scan_text(text, host_literals):
            print(f"SELF-TEST FAIL: clean text flagged: {label}: {scan_text(text, host_literals)}")
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
