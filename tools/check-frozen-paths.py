#!/usr/bin/env python3
"""check-frozen-paths.py - a merge diff that touches a frozen path must cite a newer ruling that names the path.

THE CLASS: FROZEN-PATH-UNNAMED-IN-BRIEF (Ruling 222). Track OFN edited tools/qualify-windows-runtime.py, which
Ruling 40 froze, and its brief never named the freeze. The registry docs/coordination/frozen-paths.json maps each
frozen path or glob to the ruling that froze it. This check refuses a diff that changes such a path unless, in the
range, a commit message or an ADDED line of a changed file under docs/proof/ or docs/notes/ (the same ruling section)
cites `Ruling N` with N greater than the freezing ruling AND names the path.

USAGE
  check-frozen-paths.py                      the join's merge: HEAD^1..HEAD (HEAD~1..HEAD when HEAD is not a merge)
  check-frozen-paths.py --base A --head B    an explicit range
  check-frozen-paths.py --paths P [P ...]    at a track's start: list the frozen entries under its owned paths (exit 0)
  check-frozen-paths.py --validate           check the registry itself (fields, rulings exist and are ordered)
  check-frozen-paths.py --self-test          throw-away git repos in a temp folder
Exit 0 ok / nothing frozen touched / admitted - 1 refused or registry invalid - 2 usage.
Cost: two git calls plus one per changed note file; about 0.05 s. Ring: every join (docs/coordination/join.json checks).
"""
from __future__ import annotations

import argparse
import fnmatch
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parents[1]
REGISTRY = Path("docs") / "coordination" / "frozen-paths.json"
RULINGS = Path("docs") / "notes" / "rulings.md"
CITATION = re.compile(r"\bRulings?\s+(\d{1,3})\b")
HEADING = re.compile(r"^#{2,3}[ \t]+Ruling[ \t]+\d{1,3}\b", re.M)
NOTE_ROOTS = ("docs/proof/", "docs/notes/")


def git(repo: Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True,
                          encoding="utf-8", errors="replace")


def load_registry(repo: Path) -> list:
    path = repo / REGISTRY
    if not path.is_file():
        return []
    return json.loads(path.read_text(encoding="utf-8")).get("entries", [])


def validate(repo: Path) -> list:
    """Registry problems: missing fields, an admitting ruling not newer than the freeze, a ruling with no heading."""
    problems = []
    text = (repo / RULINGS).read_text(encoding="utf-8") if (repo / RULINGS).is_file() else ""
    defined = {int(n) for n in re.findall(r"^#{2,3}[ \t]+Ruling[ \t]+(\d{1,3})\b", text, re.M)}
    for entry in load_registry(repo):
        name = entry.get("path", "?")
        for field in ("path", "freeze_ruling", "reason", "admitted_by"):
            if field not in entry:
                problems.append(f"{name}: missing field {field}")
        freeze = entry.get("freeze_ruling")
        for number in [freeze, *entry.get("admitted_by", [])]:
            if text and number not in defined:
                problems.append(f"{name}: Ruling {number} has no heading in {RULINGS.as_posix()}")
        for number in entry.get("admitted_by", []):
            if isinstance(freeze, int) and number <= freeze:
                problems.append(f"{name}: admitting Ruling {number} is not newer than freeze Ruling {freeze}")
    return problems


def match(entry_path: str, path: str) -> bool:
    return fnmatch.fnmatchcase(path, entry_path)


def default_range(repo: Path):
    if git(repo, "rev-parse", "--verify", "-q", "HEAD^1").returncode != 0:
        return None
    return "HEAD^1", "HEAD"


def admissions(repo: Path, base: str, head: str):
    """Texts that can admit an edit: each commit message in the range, and each ruling section of the added lines of
    changed files under docs/proof/ or docs/notes/. Returns a list of strings; each is checked on its own."""
    texts = [m for m in git(repo, "log", "--format=%B%x00", f"{base}..{head}").stdout.split("\0") if m.strip()]
    names = git(repo, "diff", "--name-only", base, head).stdout.split()
    for name in names:
        if not name.startswith(NOTE_ROOTS):
            continue
        diff = git(repo, "diff", "-U0", base, head, "--", name).stdout
        added = "\n".join(line[1:] for line in diff.splitlines() if line.startswith("+") and not line.startswith("+++"))
        starts = [m.start() for m in HEADING.finditer(added)]
        if not starts:
            texts.append(added)
            continue
        bounds = [0, *starts, len(added)]
        texts.extend(added[a:b] for a, b in zip(bounds, bounds[1:]) if added[a:b].strip())
    return texts


def check_range(repo: Path, base: str, head: str):
    """-> list of (path, entry) refused."""
    entries = load_registry(repo)
    diff = git(repo, "diff", "--name-only", base, head)
    if diff.returncode != 0:
        raise SystemExit(f"check-frozen-paths: cannot read the diff {base}..{head}: {diff.stderr.strip()}")
    hits = [(p, e) for p in diff.stdout.split() for e in entries if match(e["path"], p)]
    if not hits:
        return []
    texts = admissions(repo, base, head)
    refused = []
    for path, entry in hits:
        admitted = any(path in text and any(int(n) > entry["freeze_ruling"] for n in CITATION.findall(text))
                       for text in texts)
        if not admitted:
            refused.append((path, entry))
    return refused


def report(refused) -> None:
    for path, entry in refused:
        print(f"FROZEN-PATH {path}: frozen by Ruling {entry['freeze_ruling']} ({entry['reason']}). "
              f"Fix: cite a newer ruling that admits the edit (a commit message, or a changed file under docs/proof/ "
              f"or docs/notes/, with `Ruling N`, N > {entry['freeze_ruling']}, and the path {path}).")


def owned_report(repo: Path, owned: list) -> int:
    found = 0
    for entry in load_registry(repo):
        for p in owned:
            prefix = p.rstrip("/") + "/"
            if match(entry["path"], p) or match(p, entry["path"]) or entry["path"].startswith(prefix):
                found += 1
                print(f"FROZEN {entry['path']} (owned path {p}): Ruling {entry['freeze_ruling']}, {entry['reason']}. "
                      f"Admitted so far by Rulings {entry['admitted_by']}. An edit needs a newer ruling that names the path; "
                      f"if your brief does not, stop that part and report.")
                break
    if not found:
        print("check-frozen-paths: none of the owned paths is frozen.")
    return 0


# ---- self-test -------------------------------------------------------------------------------------------------

def _self_test() -> int:
    registry = {"entries": [{"path": "tools/frozen.py", "freeze_ruling": 40, "reason": "six-file lease",
                             "admitted_by": []}]}
    fails = total = 0

    def case(label, ok):
        nonlocal fails, total
        total += 1
        print(("SELFTEST PASS " if ok else "SELFTEST FAIL ") + label)
        fails += 0 if ok else 1

    def run(args, cwd):
        subprocess.run(["git", "-C", str(cwd), *args], check=True, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")

    def build(root: Path, path: str, message: str, note: tuple = None) -> Path:
        repo = root / "r"
        repo.mkdir(parents=True)
        run(["init", "-q"], repo)
        for k, v in (("user.email", "t@t"), ("user.name", "t"), ("commit.gpgsign", "false")):
            run(["config", k, v], repo)
        (repo / "docs" / "coordination").mkdir(parents=True)
        (repo / REGISTRY).write_text(json.dumps(registry), encoding="utf-8", newline="\n")
        (repo / "tools").mkdir()
        (repo / "tools" / "frozen.py").write_text("a\n", encoding="utf-8", newline="\n")
        (repo / "tools" / "free.py").write_text("a\n", encoding="utf-8", newline="\n")
        run(["add", "-A"], repo)
        run(["commit", "-qm", "base"], repo)
        (repo / path).write_text("b\n", encoding="utf-8", newline="\n")
        if note:
            target = repo / note[0]
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(note[1], encoding="utf-8", newline="\n")
        run(["add", "-A"], repo)
        run(["commit", "-qm", message], repo)
        return repo

    with tempfile.TemporaryDirectory(prefix="frozen-paths-") as tmp:
        tmp = Path(tmp)
        repo = build(tmp / "a", "tools/frozen.py", "fix: edit it")
        case("a frozen path edited with no citation is refused", len(check_range(repo, "HEAD^1", "HEAD")) == 1)
        repo = build(tmp / "b", "tools/frozen.py", "fix: edit tools/frozen.py as admitted by Ruling 222")
        case("the same edit with a newer citation in the commit message is allowed",
             check_range(repo, "HEAD^1", "HEAD") == [])
        repo = build(tmp / "c", "tools/frozen.py", "fix: edit tools/frozen.py under Ruling 12")
        case("a citation older than the freeze is refused", len(check_range(repo, "HEAD^1", "HEAD")) == 1)
        repo = build(tmp / "d", "tools/frozen.py", "fix: edit it by Ruling 222")
        case("a newer citation that does not name the path is refused", len(check_range(repo, "HEAD^1", "HEAD")) == 1)
        repo = build(tmp / "e", "tools/frozen.py", "fix: edit it",
                     ("docs/notes/rulings.md", "### Ruling 222 - admits tools/frozen.py\n"))
        case("a newer ruling section in a changed docs/notes file naming the path is allowed",
             check_range(repo, "HEAD^1", "HEAD") == [])
        repo = build(tmp / "f", "tools/frozen.py", "fix: edit it",
                     ("docs/notes/rulings.md", "### Ruling 222 - other\ntext\n### Ruling 20 - tools/frozen.py\n"))
        case("the citation and the path must sit in one ruling section",
             len(check_range(repo, "HEAD^1", "HEAD")) == 1)
        repo = build(tmp / "g", "tools/free.py", "fix: edit an unfrozen path")
        case("an unfrozen path is allowed", check_range(repo, "HEAD^1", "HEAD") == [])
        case("--paths finds the frozen entry under an owned directory",
             subprocess.run([sys.executable, __file__, "--root", str(repo), "--paths", "tools/"], capture_output=True, encoding="utf-8", errors="replace",
                            text=True).stdout.startswith("FROZEN"))
    bad = dict(registry["entries"][0], admitted_by=[39])
    with tempfile.TemporaryDirectory(prefix="frozen-paths-") as tmp:
        repo = Path(tmp)
        (repo / "docs" / "coordination").mkdir(parents=True)
        (repo / REGISTRY).write_text(json.dumps({"entries": [bad]}), encoding="utf-8", newline="\n")
        case("an admitting ruling not newer than the freeze fails validation", len(validate(repo)) == 1)
    print(f"SELFTEST {total - fails}/{total} cases")
    return 1 if fails else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--base")
    parser.add_argument("--head")
    parser.add_argument("--paths", nargs="+")
    parser.add_argument("--validate", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--root", default=str(ROOT))
    args = parser.parse_args()
    if args.self_test:
        return _self_test()
    repo = Path(args.root)
    if args.paths:
        return owned_report(repo, args.paths)
    problems = validate(repo)
    for problem in problems:
        print("REGISTRY " + problem)
    if args.validate:
        print("check-frozen-paths: registry ok." if not problems else "")
        return 1 if problems else 0
    if problems:
        return 1
    if args.base or args.head:
        if not (args.base and args.head):
            print("check-frozen-paths: give both --base and --head.", file=sys.stderr)
            return 2
        base, head = args.base, args.head
    else:
        picked = default_range(repo)
        if picked is None:
            print("check-frozen-paths: no parent commit; nothing to compare.")
            return 0
        base, head = picked
    refused = check_range(repo, base, head)
    report(refused)
    if refused:
        return 1
    print(f"check-frozen-paths: ok ({base}..{head}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
