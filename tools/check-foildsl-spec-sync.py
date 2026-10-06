#!/usr/bin/env python3
"""BRIEF-FIXTURE-AGAINST-SPEC control: a branch that changes the FoilDSL grammar must change the spec or cite a ruling.

The grammar is the `Grammar` class in src/CfdWorkbench.Core/FoilSource.cs (from its declaration to the end of
the file). A branch (merge-base with main .. HEAD) with a changed line there needs, in the same branch, either a
change to docs/specs/foildsl.md or a commit message that cites `Ruling <n>`. Without one, a repair brief that
asked for an input the spec forbids can widen the parser silently (2026-10-05: `tangents` before `ids`).
Exit 1 when the branch fails; `--self-test` builds throw-away repos for each case.
"""

import os
import re
import subprocess
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

SOURCE = "src/CfdWorkbench.Core/FoilSource.cs"
SPEC = "docs/specs/foildsl.md"
GRAMMAR_RE = re.compile(r"^\s*(?:private |internal |public )?(?:sealed )?class Grammar\b", re.MULTILINE)
HUNK_RE = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@", re.MULTILINE)
RULING_RE = re.compile(r"\bRuling\s+\d+\b", re.IGNORECASE)


def git(cwd, *args):
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout


def grammar_start(cwd):
    """1-based line of the Grammar declaration at HEAD, or None."""
    text = git(cwd, "show", "HEAD:" + SOURCE) if git(cwd, "ls-tree", "HEAD", SOURCE).strip() else ""
    match = GRAMMAR_RE.search(text)
    return text.count("\n", 0, match.start()) + 1 if match else None


def evaluate(cwd):
    """Return (ok, message)."""
    try:
        base = git(cwd, "merge-base", "HEAD", "main").strip()
    except subprocess.CalledProcessError:
        return True, "foildsl spec sync skipped: no main to compare against"
    if not git(cwd, "diff", "--name-only", base, "HEAD", "--", SOURCE).strip():
        return True, "foildsl spec sync ok: " + SOURCE + " unchanged on this branch"
    start = grammar_start(cwd)
    if start is None:
        return False, "FOILDSL-SPEC-SYNC: no `class Grammar` found in " + SOURCE + "; update " + os.path.basename(__file__)
    diff = git(cwd, "diff", "-U0", base, "HEAD", "--", SOURCE)
    touched = [int(m.group(1)) for m in HUNK_RE.finditer(diff)
               if int(m.group(1)) + max(int(m.group(2) or 1) - 1, 0) >= start]
    if not touched:
        return True, "foildsl spec sync ok: no changed line in the Grammar class"
    if git(cwd, "diff", "--name-only", base, "HEAD", "--", SPEC).strip():
        return True, "foildsl spec sync ok: Grammar changed and " + SPEC + " changed in the same branch"
    messages = git(cwd, "log", "--format=%B", base + "..HEAD")
    if RULING_RE.search(messages):
        return True, "foildsl spec sync ok: Grammar changed and a commit cites a ruling"
    return False, ("FOILDSL-SPEC-SYNC: the Grammar class in " + SOURCE + " changed (line " + str(touched[0])
                   + ") with no change to " + SPEC + " and no `Ruling <n>` in a commit message on this branch. "
                   "Change the spec in the same branch, or cite the ruling that widens the grammar.")


def _commit(directory, files, message):
    for name, text in files.items():
        path = os.path.join(directory, name)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    git(directory, "add", "-A")
    git(directory, "commit", "-q", "-m", message)


def _case(root, name, branch_commits):
    directory = os.path.join(root, name)
    os.makedirs(directory)
    git(directory, "init", "-q")
    git(directory, "config", "user.email", "t@example.com")
    git(directory, "config", "user.name", "T")
    git(directory, "checkout", "-q", "-b", "main")
    source = "// header\nclass Other { int a; }\nprivate sealed class Grammar\n{\n    int rule;\n}\n"
    _commit(directory, {SOURCE: source, SPEC: "spec\n"}, "base")
    git(directory, "checkout", "-q", "-b", "feature/x")
    for files, message in branch_commits:
        _commit(directory, files, message)
    return directory


def self_test():
    widened = "// header\nclass Other { int a; }\nprivate sealed class Grammar\n{\n    int rule; int widened;\n}\n"
    outside = "// header2\nclass Other { int a; int b; }\nprivate sealed class Grammar\n{\n    int rule;\n}\n"
    with tempfile.TemporaryDirectory(prefix="foildsl-sync-") as raw:
        root = os.path.realpath(raw)
        red = _case(root, "red", [({SOURCE: widened}, "fix: accept tangents before ids")])
        ok, message = evaluate(red)
        assert not ok and message.startswith("FOILDSL-SPEC-SYNC"), "red case must fail: " + message
        spec = _case(root, "spec", [({SOURCE: widened, SPEC: "spec v2\n"}, "feat: widen")])
        assert evaluate(spec)[0], "spec change must pass"
        ruling = _case(root, "ruling", [({SOURCE: widened}, "feat: widen per Ruling 91")])
        assert evaluate(ruling)[0], "cited ruling must pass"
        away = _case(root, "away", [({SOURCE: outside}, "fix: unrelated to the grammar")])
        assert evaluate(away)[0], "a change outside Grammar must pass"
        untouched = _case(root, "untouched", [({"other.md": "x\n"}, "docs: x")])
        assert evaluate(untouched)[0], "an unrelated branch must pass"
    print("check-foildsl-spec-sync self-test OK", flush=True)


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    root = git(os.getcwd(), "rev-parse", "--show-toplevel").strip()
    ok, message = evaluate(root)
    print(message, flush=True)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
