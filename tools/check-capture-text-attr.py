#!/usr/bin/env python3
"""CAPTURE-TEXT-ATTR guard (Ruling 229, RAW-CAPTURE-BLOB-MISMATCH recurrence 3): every file a capture-manifest.json
lists must keep its exact bytes, so Git must not normalise its line endings.

For every tracked capture-manifest.json, each listed file that is itself tracked must have the `text` attribute
unset (`-text`) or be `binary` (which unsets it), read through `git check-attr text -- <path>`. Under the repository's
`* text=auto eol=lf` a CRLF capture is committed as LF and the manifest hash no longer matches the blob. A listed
file that is not tracked is skipped (check-capture-manifests.py fails it). Fails with the folder and the
`.gitattributes` line to add.

Allowed besides -text/binary: `text` with an explicit `eol=lf|crlf` ("declared"), e.g. the authored *.py/*.ps1/*.md
of win-gpu-b1/b2/g1/qualification, whose folder .gitattributes set it on purpose and whose manifests hash the
normalised blobs. Residual: a CRLF capture matched by such a rule is still normalised; those folders are not
changed here because their .gitattributes belong to delivered proofs.

  python3 tools/check-capture-text-attr.py [--root DIR]
  python3 tools/check-capture-text-attr.py --self-test

Exit 0 every listed file has text unset (or no manifest exists) . 1 a file would be normalised . 2 usage.
Ring: fast (every push), run from tools/check-docs.py. Cost: two git calls per manifest (`git ls-files`, one
`git check-attr --stdin`); measured in docs/proof/rsx/red-first.md. Stdlib only.
"""
from __future__ import annotations

import json
import subprocess
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
MANIFEST_NAME = "capture-manifest.json"


def git(root: Path, *args: str, stdin: bytes | None = None) -> bytes:
    result = subprocess.run(["git", *args], cwd=root, capture_output=True, input=stdin)
    if result.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {result.stderr.decode('utf-8', 'replace').strip()}")
    return result.stdout


def tracked(root: Path) -> set[str]:
    return set(git(root, "ls-files", "-z").decode("utf-8").split("\0")) - {""}


def listed_files(root: Path, manifest: str) -> list[str]:
    try:
        entries = json.loads((root / manifest).read_text(encoding="utf-8-sig"))["files"]
        return [str(e["path"]) for e in entries]  # repo-relative, as check-capture-manifests.py requires
    except (OSError, ValueError, KeyError, TypeError):
        return []


def text_state(root: Path, paths: list[str]) -> dict[str, str]:
    """path -> 'unset' (exact bytes), 'declared' (text set with an explicit eol=, a reviewed authored file) or the
    raw state ('auto', 'set', 'unspecified')."""
    out = git(root, "check-attr", "-z", "--stdin", "text", "eol", stdin=("\0".join(paths) + "\0").encode("utf-8"))
    fields = out.decode("utf-8").split("\0")
    attrs: dict[str, dict[str, str]] = {}
    for i in range(0, len(fields) - 2, 3):
        attrs.setdefault(fields[i], {})[fields[i + 1]] = fields[i + 2]
    state = {}
    for path, a in attrs.items():
        text, eol = a.get("text", "unspecified"), a.get("eol", "unspecified")
        state[path] = "declared" if text == "set" and eol in ("lf", "crlf") else text
    return state


def check(root: Path) -> list[str]:
    files = tracked(root)
    problems = []
    for manifest in sorted(p for p in files if p.rsplit("/", 1)[-1] == MANIFEST_NAME):
        listed = [p for p in listed_files(root, manifest) if p in files]
        if not listed:
            continue
        state = text_state(root, listed)
        bad = [p for p in listed if state.get(p) not in ("unset", "declared")]
        if bad:
            folder = manifest.rsplit("/", 1)[0] if "/" in manifest else "."
            problems.append(f"CAPTURE-TEXT-ATTR: {folder}: {len(bad)} of {len(listed)} listed files have text "
                            f"'{state.get(bad[0])}' (first: {bad[0]}); add to .gitattributes: {folder}/** -text")
    return problems


def put(path: Path, text: str) -> None:
    path.write_bytes(text.encode("utf-8"))


def self_test() -> int:
    bad = 0
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp)
        git(repo, "init", "-q")
        git(repo, "config", "user.email", "t@example.invalid")
        git(repo, "config", "user.name", "t")
        folder = repo / "cap"
        folder.mkdir()
        (folder / "a.stdout.txt").write_bytes(b"line\r\n")
        put(folder / MANIFEST_NAME, json.dumps({"files": [{"path": "cap/a.stdout.txt", "bytes": 6, "sha256": "0"}]}))
        put(repo / ".gitattributes", "* text=auto eol=lf\n")
        git(repo, "add", "-A")
        if not check(repo):
            print("SELF-TEST FAIL: capture folder without -text was not flagged")
            bad += 1
        elif "cap/** -text" not in check(repo)[0]:
            print("SELF-TEST FAIL: failure does not name the .gitattributes line")
            bad += 1
        put(repo / ".gitattributes", "* text=auto eol=lf\ncap/** -text\n")
        git(repo, "add", "-A")
        if check(repo):
            print(f"SELF-TEST FAIL: capture folder with -text flagged: {check(repo)}")
            bad += 1
        put(repo / ".gitattributes", "* text=auto eol=lf\ncap/** binary\n")
        if check(repo):
            print(f"SELF-TEST FAIL: capture folder with binary flagged: {check(repo)}")
            bad += 1
        put(repo / ".gitattributes", "* text=auto eol=lf\ncap/*.txt text eol=lf\n")
        if check(repo):
            print(f"SELF-TEST FAIL: declared text eol=lf flagged: {check(repo)}")
            bad += 1
        put(repo / ".gitattributes", "* text=auto eol=lf\ncap/a.stdout.txt -text\n")
        (folder / "b.txt").write_bytes(b"x\r\n")
        put(folder / MANIFEST_NAME, json.dumps({"files": [
            {"path": "cap/a.stdout.txt", "bytes": 6, "sha256": "0"}, {"path": "cap/b.txt", "bytes": 3, "sha256": "0"}]}))
        git(repo, "add", "-A")
        if not check(repo):
            print("SELF-TEST FAIL: a second listed file left at text=auto was not flagged")
            bad += 1
    if not bad:
        print("CAPTURE-TEXT-ATTR self-test ok: 2 offenders caught, 2 clean passed")
    return 1 if bad else 0


def main(argv: list[str]) -> int:
    if "--self-test" in argv:
        return self_test()
    root = Path(argv[argv.index("--root") + 1]) if "--root" in argv else ROOT
    problems = check(root)
    for line in problems:
        print(line)
    if problems:
        print(f"CAPTURE-TEXT-ATTR FAIL: {len(problems)} capture folder(s) can be normalised by git")
        return 1
    print("CAPTURE-TEXT-ATTR ok: every capture-manifest file has text unset")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
