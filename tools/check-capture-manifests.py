#!/usr/bin/env python3
"""CAPTURE-MANIFEST guard (Ruling 167, the class control for Ruling 163 C3): a proof folder's capture-manifest.json must
describe the bytes that are committed.

For every docs/proof/<dir>/capture-manifest.json in HEAD, each entry of "files" ({path, bytes, sha256}) must name a path
inside that same folder, and the committed blob (`git show HEAD:<path>`, never the working tree) must have exactly that byte
count and SHA-256. A missing blob, an unreadable manifest or an entry outside the folder fails.

  python3 tools/check-capture-manifests.py [--root DIR]
  python3 tools/check-capture-manifests.py --self-test

Exit 0 every manifest holds (or none exists) . 1 a mismatch . 2 usage.
Ring: fast (every push), run from tools/check-docs.py. Cost: one `git ls-tree` plus one `git show` per manifest and per entry
(measured in docs/proof/wrb/red-first.md). Stdlib only. The PC's proof-local docs/proof/r163-windows-ring/verify-captures.py
stays as that delivery's proof; this is the generalisation.
"""
from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path, PurePosixPath

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = re.compile(r"docs/proof/[^/]+/capture-manifest\.json")


def git_bytes(root: Path, *args: str) -> bytes | None:
    result = subprocess.run(["git", *args], cwd=root, capture_output=True)
    return result.stdout if result.returncode == 0 else None


def check_manifest(root: Path, manifest_path: str) -> list[str]:
    """The problems of one manifest, judged against the blobs at HEAD."""
    raw = git_bytes(root, "show", f"HEAD:{manifest_path}")
    if raw is None:
        return [f"{manifest_path}: not readable at HEAD"]
    try:
        entries = json.loads(raw.decode("utf-8-sig"))["files"]
        records = [(str(e["path"]), int(e["bytes"]), str(e["sha256"]).lower()) for e in entries]
    except (ValueError, KeyError, TypeError) as exc:
        return [f"{manifest_path}: unreadable manifest ({type(exc).__name__}: {exc})"]
    folder = manifest_path.rsplit("/", 1)[0] + "/"
    problems = []
    for path, declared_bytes, declared_sha in records:
        if not path.startswith(folder) or ".." in PurePosixPath(path).parts:
            problems.append(f"{manifest_path}: {path} is outside {folder}")
            continue
        blob = git_bytes(root, "show", f"HEAD:{path}")
        if blob is None:
            problems.append(f"{manifest_path}: {path} is not committed at HEAD")
            continue
        if len(blob) != declared_bytes:
            problems.append(f"{manifest_path}: {path} bytes declared {declared_bytes}, committed {len(blob)}")
        if hashlib.sha256(blob).hexdigest() != declared_sha:
            problems.append(f"{manifest_path}: {path} SHA-256 differs from the committed blob")
    return problems


def check(root: Path) -> tuple[list[str], int]:
    """(problems, manifests checked) over every capture-manifest.json in HEAD."""
    listing = git_bytes(root, "ls-tree", "-r", "--name-only", "HEAD", "--", "docs/proof")
    manifests = sorted(p for p in (listing or b"").decode("utf-8", errors="replace").splitlines() if MANIFEST.fullmatch(p))
    problems = [problem for manifest in manifests for problem in check_manifest(root, manifest)]
    return problems, len(manifests)


def self_test() -> int:
    payload = b"capture bytes\r\n\x00\xff"
    good_sha = hashlib.sha256(payload).hexdigest()

    def entry(**change: object) -> dict:
        return {"path": "docs/proof/r1/a.log", "bytes": len(payload), "sha256": good_sha, **change}

    # label, entries, working-tree edit after the commit, expected problem fragment (None: clean)
    cases = [
        ("a matching manifest is clean", [entry()], False, None),
        ("byte count off by one fails", [entry(bytes=len(payload) + 1)], False, "bytes declared"),
        ("SHA-256 of another byte fails", [entry(sha256=hashlib.sha256(payload + b"x").hexdigest())], False, "SHA-256 differs"),
        ("an upper-case SHA-256 of the right bytes is clean", [entry(sha256=good_sha.upper())], False, None),
        ("a path in another proof folder fails", [entry(path="docs/proof/r2/a.log")], False, "outside"),
        ("a .. path fails", [entry(path="docs/proof/r1/../r2/a.log")], False, "outside"),
        ("an uncommitted capture fails", [entry(path="docs/proof/r1/missing.log")], False, "not committed"),
        ("the committed blob is judged, not the working tree", [entry()], True, None),
        ("a manifest without files fails by name", None, False, "unreadable manifest"),
    ]
    failures = 0
    for label, entries, dirty, fragment in cases:
        with tempfile.TemporaryDirectory() as scratch:
            repo = Path(scratch)
            (repo / "docs/proof/r1").mkdir(parents=True)
            (repo / "docs/proof/r1/a.log").write_bytes(payload)
            document = {"files": entries} if entries is not None else {"nope": []}
            (repo / "docs/proof/r1/capture-manifest.json").write_text(json.dumps(document), encoding="utf-8")
            for command in (["init", "-q"], ["add", "-A"], ["-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "x"]):
                subprocess.run(["git", *command], cwd=repo, check=True, capture_output=True)
            if dirty:
                (repo / "docs/proof/r1/a.log").write_bytes(payload + b"edited after commit")
            problems, count = check(repo)
            ok = count == 1 and (not problems if fragment is None else any(fragment in p for p in problems))
            print(f"SELFTEST {'PASS' if ok else 'FAIL'} {label}" + ("" if ok else f": expected {fragment or 'clean'}, got {problems} over {count} manifest(s)"))
            failures += not ok
    print(f"SELFTEST {len(cases) - failures}/{len(cases)} cases")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    args = argv[1:]
    if args == ["--self-test"]:
        return self_test()
    root = ROOT
    if len(args) == 2 and args[0] == "--root":
        root = Path(args[1])
    elif args:
        print(__doc__.strip().splitlines()[0], file=sys.stderr)
        return 2
    problems, count = check(root)
    for problem in problems:
        print("CAPTURE-MANIFEST: " + problem)
    if problems:
        return 1
    print(f"check-capture-manifests: {count} manifest(s) match their committed blobs")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
