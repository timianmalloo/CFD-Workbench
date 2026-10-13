#!/usr/bin/env python3
"""Read-only PII audit of every GitHub text in the repository (PII-OUTSIDE-THE-TREE).

Scans issue comments, review comments, PR bodies and review bodies through `gh api --paginate` (GET only), with the
scanner of tools/check-proof-pii.py (imported, never copied; own-host names and CFD_PII_HOSTNAMES included).
Prints only `<kind> <id> on #<n>: <rule> <count>` and a total; never the text. Exit 1 on any hit.

ON DEMAND, NOT IN A RING: it needs the network and `gh` auth. The leader runs it at each join of a PC PR. Only its
self-test (injected fixture pages, no network) runs in the fast ring of tools/check-docs.py.

Usage: scan-github-text.py [--repo OWNER/NAME]      (default: the repository `gh` resolves for this checkout)
       scan-github-text.py --self-test
"""
from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import subprocess
import sys
from collections import Counter
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def load_post():
    spec = importlib.util.spec_from_file_location("post_github_comment", TOOLS / "post-github-comment.py")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


POST = load_post()


def gh_pages(path: str) -> list[dict]:
    """All items of a paginated GET (gh joins the pages into one JSON array per page; --slurp merges them)."""
    out = subprocess.run(["gh", "api", "--paginate", "--slurp", path], capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout
    return [item for page in json.loads(out) for item in (page if isinstance(page, list) else [page])]


def number_from_url(url: str) -> str:
    tail = (url or "").rstrip("/").rsplit("/", 1)[-1]
    return tail if tail.isdigit() else "?"


def collect(repo: str, pages=gh_pages) -> list[tuple[str, object, str, str]]:
    """(kind, id, '#n', body) for every text. `pages` is injectable."""
    texts = []
    for c in pages(f"repos/{repo}/issues/comments?per_page=100"):
        texts.append(("issue-comment", c["id"], "#" + number_from_url(c.get("issue_url", "")), c.get("body") or ""))
    for c in pages(f"repos/{repo}/pulls/comments?per_page=100"):
        texts.append(("review-comment", c["id"], "#" + number_from_url(c.get("pull_request_url", "")), c.get("body") or ""))
    for p in pages(f"repos/{repo}/pulls?state=all&per_page=100"):
        texts.append(("pr-body", p["number"], f"#{p['number']}", p.get("body") or ""))
        for r in pages(f"repos/{repo}/pulls/{p['number']}/reviews?per_page=100"):
            texts.append(("review-body", r["id"], f"#{p['number']}", r.get("body") or ""))
    for i in pages(f"repos/{repo}/issues?state=all&per_page=100"):
        if "pull_request" not in i:
            texts.append(("issue-body", i["number"], f"#{i['number']}", i.get("body") or ""))
    return texts


def scan(texts, own_hosts=None, literals=None) -> tuple[list[str], int]:
    lines, flagged = [], 0
    for kind, ident, num, body in texts:
        counts = Counter(POST.rule_of(h) for h in POST.scan_body(body, own_hosts, literals))
        if counts:
            flagged += 1
            lines.extend(f"{kind} {ident} on {num}: {rule} {n}" for rule, n in sorted(counts.items()))
    return lines, flagged


def run(repo: str, pages=gh_pages, own_hosts=None, literals=None) -> int:
    texts = collect(repo, pages)
    lines, flagged = scan(texts, own_hosts, literals)
    for line in lines:
        print(line)
    print(f"SCAN-GH: scanned {len(texts)} texts; flagged {flagged}")
    return 1 if flagged else 0


def self_test() -> int:
    bs, u = "\\", "Users"
    fake_own = "qx" + "fake" + "box9"
    bad_path = f"C:{bs}{u}{bs}jdoe{bs}x"
    sid = "S-1-5-" + "21-1-2-3"
    fixtures = {
        "issues/comments": [{"id": 11, "issue_url": "https://x/issues/5", "body": f"see {bad_path}"},
                            {"id": 12, "issue_url": "https://x/issues/5", "body": "clean %USERPROFILE%"}],
        "pulls/comments": [{"id": 21, "pull_request_url": "https://x/pulls/6", "body": f"on {fake_own} and {sid}"}],
        "pulls?": [{"number": 6, "body": "clean body"}],
        "reviews": [{"id": 31, "body": f"{bad_path} {bad_path}"}],
        "issues?": [{"number": 5, "body": "clean"}, {"number": 6, "pull_request": {}, "body": f"{sid}"}],
    }

    def pages(path: str):
        for key in ("issues/comments", "pulls/comments", "reviews", "pulls?", "issues?"):
            if key in path:
                return fixtures[key]
        return []

    texts = collect("o/r", pages)
    lines, flagged = scan(texts, own_hosts=[fake_own], literals=[])
    expected = {
        "issue-comment 11 on #5: windows-user-path 1",
        "review-comment 21 on #6: hostname:own-host 1",
        "review-comment 21 on #6: machine-sid 1",
        "review-body 31 on #6: windows-user-path 2",
    }
    bad = 0
    if set(lines) != expected or flagged != 3:
        print(f"SELF-TEST FAIL: expected {sorted(expected)}, got {lines} (flagged {flagged})")
        bad += 1
    if any(s in " ".join(lines) for s in ("jdoe", fake_own, sid, bs + u)):
        print("SELF-TEST FAIL: output prints the text")
        bad += 1
    with contextlib.redirect_stdout(io.StringIO()):
        hit_rc = run("o/r", pages, own_hosts=[fake_own], literals=[])
        empty_rc = run("o/r", lambda p: [], own_hosts=[], literals=[])
    if hit_rc != 1:
        print("SELF-TEST FAIL: hits must exit 1")
        bad += 1
    if empty_rc != 0:
        print("SELF-TEST FAIL: empty repository must exit 0")
        bad += 1
    if not bad:
        print(f"SCAN-GH self-test ok: {len(texts)} fixture texts, {flagged} flagged, counts only, exit 1 on a hit")
    return 1 if bad else 0


if __name__ == "__main__":
    args = sys.argv[1:]
    if "--self-test" in args:
        sys.exit(self_test())
    if "--repo" in args:
        repo_name = args[args.index("--repo") + 1]
    else:
        repo_name = subprocess.run(["gh", "repo", "view", "--json", "nameWithOwner", "-q", ".nameWithOwner"],
                                   capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout.strip()
    sys.exit(run(repo_name))
