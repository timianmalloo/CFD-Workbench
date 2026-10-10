#!/usr/bin/env python3
"""Show whether the Windows store admission (Ruling 189) still matches HEAD.

Reads docs/proof/windows-store-admission.json, recomputes each bound value (sha256 of the two files, `git rev-parse
HEAD:<path>` for trees and blobs) and prints one line:

  WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current
  WINDOWS-STORE-EVIDENCE STALE since <first differing path>: <recorded> -> <now>

Both exit 0: staleness is made visible, it never blocks unrelated work (Core changes often). Exit 1 only when the binding
is missing, unreadable or malformed. The one hard fail: a docs/*.md file that claims a current Windows PASS (the marker
"Windows store: PASS (current)") while the binding is stale. A marker quoted as code (inline backtick span or fenced block) is a quotation, not a claim.

  python3 tools/check-windows-admission.py
  python3 tools/check-windows-admission.py --self-test
"""
from __future__ import annotations

import hashlib
import json
import re
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
BINDING = ROOT / "docs" / "proof" / "windows-store-admission.json"
MARKER = "Windows store: PASS (current)"
SHA = re.compile(r"^[0-9a-f]{64}$")
OID = re.compile(r"^[0-9a-f]{40}$")


class BindingError(Exception):
    pass


def load(binding: Path) -> dict:
    try:
        data = json.loads(binding.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise BindingError("binding unreadable: {0}".format(error))
    try:
        for entry in data["files"]:
            if not (isinstance(entry["path"], str) and SHA.match(entry["sha256"])):
                raise BindingError("malformed sha256 for {0}".format(entry["path"]))
        for entry in data["objects"]:
            if not (isinstance(entry["path"], str) and OID.match(entry["id"])):
                raise BindingError("malformed object id for {0}".format(entry["path"]))
        if not isinstance(data["ruling"], str):
            raise BindingError("malformed ruling")
    except (KeyError, TypeError) as error:
        raise BindingError("binding malformed: {0!r}".format(error))
    return data


def object_id(root: Path, path: str) -> str:
    result = subprocess.run(["git", "rev-parse", "HEAD:" + path], cwd=root, capture_output=True, text=True, encoding="utf-8")
    return result.stdout.strip() if result.returncode == 0 else "missing"


def file_sha(root: Path, path: str) -> str:
    try:
        return hashlib.sha256((root / path).read_bytes()).hexdigest()
    except OSError:
        return "missing"


def first_difference(data: dict, root: Path) -> str | None:
    """Return 'path: recorded -> now' for the first changed bound value, or None. Values show 8 characters."""
    for entry in data["files"]:
        now = file_sha(root, entry["path"])
        if now != entry["sha256"]:
            return "{0}: {1} -> {2}".format(entry["path"], entry["sha256"][:8], now[:8])
    for entry in data["objects"]:
        now = object_id(root, entry["path"])
        if now != entry["id"]:
            return "{0}: {1} -> {2}".format(entry["path"], entry["id"][:8], now[:8])
    return None


def status_line(data: dict, root: Path) -> tuple[str, bool]:
    difference = first_difference(data, root)
    if difference is None:
        return "WINDOWS-STORE-EVIDENCE admitted ({0}), current".format(data["ruling"]), False
    return "WINDOWS-STORE-EVIDENCE STALE since {0}".format(difference), True


FENCE = re.compile(r"^(`{3,}|~{3,}).*?^\1[ \t]*$", re.MULTILINE | re.DOTALL)
CODE_SPAN = re.compile(r"`[^`\n]*`")


def unquoted(text: str) -> str:
    """Drop fenced blocks and inline backtick spans: a marker quoted as code is a quotation, not a claim."""
    return CODE_SPAN.sub("", FENCE.sub("", text))


def claims(root: Path) -> list[str]:
    found = []
    for document in sorted((root / "docs").rglob("*.md")):
        try:
            if MARKER in unquoted(document.read_text(encoding="utf-8", errors="replace")):
                found.append(document.relative_to(root).as_posix())
        except OSError:
            pass
    return found


def check(root: Path = ROOT, binding: Path = BINDING) -> int:
    try:
        data = load(binding)
    except BindingError as error:
        print("WINDOWS-STORE-EVIDENCE binding error: {0}".format(error))
        return 1
    line, stale = status_line(data, root)
    print(line)
    if stale:
        for document in claims(root):
            print("FAIL: {0} claims '{1}' but the admission is stale".format(document, MARKER))
            return 1
    return 0


def evidence_line() -> str:
    """One status line for other tools (the readiness wrapper); never raises."""
    try:
        return status_line(load(BINDING), ROOT)[0]
    except BindingError as error:
        return "WINDOWS-STORE-EVIDENCE binding error: {0}".format(error)


def self_test() -> int:
    import contextlib
    import io

    problems = []

    def run(root: Path, binding: Path):
        buffer = io.StringIO()
        with contextlib.redirect_stdout(buffer):
            code = check(root, binding)
        return code, buffer.getvalue()

    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        for command in (["git", "init", "-q"], ["git", "config", "user.email", "t@example.invalid"],
                        ["git", "config", "user.name", "t"]):
            subprocess.run(command, cwd=root, check=True)
        (root / "a.txt").write_text("one\n", encoding="utf-8", newline="\n")
        (root / "sub").mkdir()
        (root / "sub" / "b.txt").write_text("two\n", encoding="utf-8", newline="\n")
        (root / "docs").mkdir()
        (root / "docs" / "keep.md").write_text("x\n", encoding="utf-8", newline="\n")
        subprocess.run(["git", "add", "-A"], cwd=root, check=True)
        subprocess.run(["git", "commit", "-q", "-m", "x"], cwd=root, check=True)
        good = {
            "ruling": "Ruling 189",
            "files": [{"path": "a.txt", "sha256": file_sha(root, "a.txt")}],
            "objects": [{"path": "sub", "id": object_id(root, "sub")}],
        }
        binding = root / "binding.json"
        binding.write_text(json.dumps(good), encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0 or out.strip() != "WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current":
            problems.append("intact binding: {0} {1!r}".format(code, out))
        altered = json.loads(json.dumps(good))
        altered["objects"][0]["id"] = "0" * 40
        binding.write_text(json.dumps(altered), encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0 or "STALE since sub: 00000000 ->" not in out:
            problems.append("altered tree: {0} {1!r}".format(code, out))
        altered = json.loads(json.dumps(good))
        altered["files"][0]["sha256"] = "f" * 64
        binding.write_text(json.dumps(altered), encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0 or "STALE since a.txt: ffffffff ->" not in out:
            problems.append("altered file: {0} {1!r}".format(code, out))
        (root / "docs" / "claim.md").write_text(MARKER + "\n", encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 1 or "claim.md" not in out:
            problems.append("stale plus claim must fail: {0} {1!r}".format(code, out))
        claim = root / "docs" / "claim.md"
        claim.write_text("`" + MARKER + "`\n", encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0:
            problems.append("backtick-quoted marker must pass while stale: {0} {1!r}".format(code, out))
        claim.write_text("```\n" + MARKER + "\n```\n", encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0:
            problems.append("fenced marker must pass while stale: {0} {1!r}".format(code, out))
        claim.write_text("`x` then " + MARKER + "\n", encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 1:
            problems.append("bare marker after a code span must fail while stale: {0} {1!r}".format(code, out))
        claim.unlink()
        code, out = run(root, binding)
        if code != 0 or "STALE since" not in out:
            problems.append("stale with no claim must print STALE and exit 0: {0} {1!r}".format(code, out))
        claim.write_text(MARKER + "\n", encoding="utf-8", newline="\n")
        binding.write_text(json.dumps(good), encoding="utf-8", newline="\n")
        code, out = run(root, binding)
        if code != 0:
            problems.append("current plus claim must pass: {0} {1!r}".format(code, out))
        binding.write_text("{not json", encoding="utf-8", newline="\n")
        if run(root, binding)[0] != 1:
            problems.append("malformed json must exit 1")
        broken = json.loads(json.dumps(good))
        broken["objects"][0]["id"] = "xyz"
        binding.write_text(json.dumps(broken), encoding="utf-8", newline="\n")
        if run(root, binding)[0] != 1:
            problems.append("malformed value must exit 1")
        if run(root, root / "absent.json")[0] != 1:
            problems.append("missing binding must exit 1")
    for problem in problems:
        print("self-test FAIL: " + problem)
    print("self-test OK" if not problems else "self-test FAILED")
    return 1 if problems else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if argv:
        print(__doc__)
        return 2
    return check()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
