#!/usr/bin/env python3
"""Post a PR or issue comment through `gh`, after the PII scan (PII-OUTSIDE-THE-TREE).

The repository is public. tools/check-proof-pii.py scans committed files only, so a comment body is checked here
before it leaves the machine. The body goes through the same scanner (scan_text, imported, never copied), which
includes the own-host names and CFD_PII_HOSTNAMES. On any hit the tool refuses with exit 3 and prints only the rule id
and a masked form, never the text. A Windows user path or SID prints as the rule id alone.

Usage: post-github-comment.py (--pr N | --issue N) --body-file F [--dry-run] [--repo OWNER/NAME]
       post-github-comment.py --self-test
Exit: 0 posted (or clean under --dry-run), 2 usage or unreadable body, 3 refused for PII, other = gh exit code.

Ring: fast, self-test only (tools/check-docs.py; no network, `gh` is never called, the poster is injected).
Residual: a comment typed in the web UI or sent by a raw `gh pr comment` bypasses this wrapper; the on-demand
tools/scan-github-text.py is the backstop.
"""
from __future__ import annotations

import contextlib
import importlib.util
import io
import subprocess
import sys
import tempfile
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def load_scanner():
    spec = importlib.util.spec_from_file_location("check_proof_pii", TOOLS / "check-proof-pii.py")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


PII = load_scanner()


def rule_of(hit: str) -> str:
    """The rule id of a scan_text hit, with no matched text: hostname:<rule> or the bare rule."""
    parts = hit.split(":")
    return ":".join(parts[:2]) if parts[0] == "hostname" else parts[0]


def masked_of(hit: str) -> str:
    """The rule id plus a masked form. Host hits are already masked by the scanner; paths and SIDs are not shown."""
    parts = hit.split(":", 2)
    return hit if parts[0] == "hostname" else f"{parts[0]}:***"


def scan_body(text: str, own_hosts: list[str] | None = None, literals: list[str] | None = None) -> list[str]:
    return PII.scan_text(text, literals, own_hosts)


def gh_poster(kind: str, number: int, body_file: str, repo: str | None) -> int:
    cmd = ["gh", kind, "comment", str(number), "--body-file", body_file]
    if repo:
        cmd += ["--repo", repo]
    return subprocess.run(cmd).returncode


def main(argv: list[str], poster=gh_poster, own_hosts: list[str] | None = None, literals: list[str] | None = None) -> int:
    def arg(name: str) -> str | None:
        return argv[argv.index(name) + 1] if name in argv and argv.index(name) + 1 < len(argv) else None

    pr, issue, body_file = arg("--pr"), arg("--issue"), arg("--body-file")
    if (pr is None) == (issue is None) or body_file is None or not (pr or issue).isdigit():
        print("usage: post-github-comment.py (--pr N | --issue N) --body-file F [--dry-run] [--repo OWNER/NAME]")
        return 2
    try:
        text = Path(body_file).read_bytes().decode("utf-8", "replace")
    except OSError as e:
        print(f"POST-GH: cannot read body file: {type(e).__name__}")
        return 2
    hits = scan_body(text, own_hosts, literals)
    if hits:
        for h in hits:
            print(f"POST-GH REFUSED: {masked_of(h)}")
        print(f"POST-GH REFUSED: {len(hits)} hit(s); redact to %USERPROFILE% and <host>, then post again")
        return 3
    if "--dry-run" in argv:
        print("POST-GH dry-run ok: body is clean, nothing posted")
        return 0
    return poster("pr" if pr else "issue", int(pr or issue), body_file, arg("--repo"))


def self_test() -> int:
    bs, u = "\\", "Users"
    sid = "S-1-5-" + "21-1111111111-2222222222-3333333333-1001"
    fake_own = "qx" + "fake" + "box9"
    offenders = {
        "windows user path": f"log C:{bs}{u}{bs}jdoe{bs}AppData{bs}Local end",
        "wsl user path": f"/mnt/c/{u}/jdoe/AppData",
        "sid": f"owner {sid}",
        "own host": f"ran on {fake_own} today",
    }
    bad = 0
    calls: list[tuple] = []

    def fake_poster(kind, number, body_file, repo):
        calls.append((kind, number))
        return 0

    with tempfile.TemporaryDirectory() as tmp:
        for label, text in offenders.items():
            f = Path(tmp) / "body.md"
            f.write_text(text, encoding="utf-8", newline="\n")
            out = io.StringIO()
            with contextlib.redirect_stdout(out):
                rc = main(["--pr", "7", "--body-file", str(f)], fake_poster, own_hosts=[fake_own], literals=[])
            shown = out.getvalue()
            if rc != 3:
                print(f"SELF-TEST FAIL: offender not refused with exit 3: {label} (exit {rc})")
                bad += 1
            if any(s in shown for s in ("jdoe", sid, fake_own, f"{bs}{u}")):
                print(f"SELF-TEST FAIL: refusal prints the text: {label}")
                bad += 1
            if "path" in label and "windows-user-path" not in shown:
                print(f"SELF-TEST FAIL: rule id missing: {label}")
                bad += 1
        if calls:
            print("SELF-TEST FAIL: poster called for an offender")
            bad += 1
        clean = Path(tmp) / "clean.md"
        clean.write_text(f"log %USERPROFILE%{bs}AppData on <host>, readiness green", encoding="utf-8", newline="\n")
        with contextlib.redirect_stdout(io.StringIO()):
            dry = main(["--pr", "7", "--body-file", str(clean), "--dry-run"], fake_poster, own_hosts=[fake_own], literals=[])
            dry_calls = list(calls)
            posted = main(["--issue", "9", "--body-file", str(clean)], fake_poster, own_hosts=[fake_own], literals=[])
            both = main(["--pr", "7", "--issue", "9", "--body-file", str(clean)], fake_poster)
        if dry != 0 or dry_calls:
            print("SELF-TEST FAIL: clean dry-run must exit 0 and not post")
            bad += 1
        if posted != 0 or calls != [("issue", 9)]:
            print("SELF-TEST FAIL: clean body not posted to the issue")
            bad += 1
        if both != 2:
            print("SELF-TEST FAIL: --pr with --issue must be a usage error")
            bad += 1
    if not bad:
        print(f"POST-GH self-test ok: {len(offenders)} offenders refused (exit 3, text never printed), clean body posted through the injected poster")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(self_test() if "--self-test" in sys.argv[1:] else main(sys.argv[1:]))
