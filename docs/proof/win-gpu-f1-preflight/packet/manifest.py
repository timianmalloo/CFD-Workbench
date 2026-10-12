#!/usr/bin/env python3
"""Freeze live R217 evidence and verify every frozen byte in a Git commit."""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

EXCLUDE = {"current-owner.pid", "cancel.request", "completed-guard-stop.request"}


def hash_file(path: Path) -> tuple[int, str]:
    digest = hashlib.sha256()
    size = 0
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
            size += len(block)
    return size, digest.hexdigest()


def freeze(live: Path, frozen: Path) -> None:
    if frozen.exists() or not live.is_dir():
        raise ValueError("frozen destination exists or live source is absent")
    frozen.mkdir(parents=True)
    for source in sorted(live.rglob("*")):
        if source.is_symlink():
            raise ValueError(f"symlink evidence is forbidden: {source}")
        if not source.is_file() or source.name in EXCLUDE:
            continue
        relative = source.relative_to(live)
        target = frozen / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if source.name.endswith("events.jsonl"):
            data = source.read_bytes()
            last = data.rfind(b"\n")
            if last < 0:
                raise ValueError("guard events have no complete row")
            target.write_bytes(data[: last + 1])
            for line in target.read_bytes().splitlines():
                json.loads(line)
        else:
            shutil.copyfile(source, target)
    files = []
    for path in sorted(frozen.rglob("*")):
        if path.is_file():
            size, digest = hash_file(path)
            files.append({"path": path.relative_to(frozen).as_posix(), "bytes": size, "sha256": digest})
    if not files:
        raise ValueError("empty evidence freeze")
    (frozen / "capture-manifest.json").write_text(
        json.dumps({"schema": "cfdw-f1-r217-capture-manifest/1", "files": files},
                   indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    print(f"FREEZE-PASS files={len(files)}")


def verify(repo: Path, frozen: Path, commit: str) -> None:
    manifest = json.loads((frozen / "capture-manifest.json").read_text(encoding="utf-8"))
    files = manifest["files"]
    listed = {entry["path"] for entry in files}
    actual = {path.relative_to(frozen).as_posix() for path in frozen.rglob("*")
              if path.is_file() and path.name != "capture-manifest.json"}
    if listed != actual or len(listed) != len(files):
        raise ValueError("manifest file set differs from frozen evidence")
    for entry in files + [{"path": "capture-manifest.json", "bytes": None, "sha256": None}]:
        path = frozen / entry["path"]
        if path.is_symlink() or not path.is_file():
            raise ValueError(f"missing or linked evidence: {path}")
        size, digest = hash_file(path)
        if entry["bytes"] is not None and (size != entry["bytes"] or digest != entry["sha256"]):
            raise ValueError(f"worktree evidence differs from manifest: {path}")
        relative = path.resolve().relative_to(repo.resolve()).as_posix()
        process = subprocess.Popen(["git", "cat-file", "blob", f"{commit}:{relative}"], cwd=repo,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if process.stdout is None:
            raise ValueError("git did not supply blob stream")
        blob_hash = hashlib.sha256()
        blob_size = 0
        for block in iter(lambda: process.stdout.read(1024 * 1024), b""):
            blob_hash.update(block)
            blob_size += len(block)
        if process.wait(timeout=30) != 0 or blob_size != size or blob_hash.hexdigest() != digest:
            raise ValueError(f"commit blob differs from evidence: {relative}")
    print(f"GIT-BINDING-PASS commit={commit} files={len(files)+1}")


def self_test() -> None:
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        live, frozen = root / "live", root / "frozen"
        live.mkdir()
        (live / "a.txt").write_text("one\n", encoding="utf-8")
        (live / "completed-guard-events.jsonl").write_bytes(b'{"sample_number":1}\n{"partial":')
        freeze(live, frozen)
        assert (frozen / "completed-guard-events.jsonl").read_bytes() == b'{"sample_number":1}\n'
        manifest = json.loads((frozen / "capture-manifest.json").read_text(encoding="utf-8"))
        assert len(manifest["files"]) == 2
        (frozen / "a.txt").write_text("two\n", encoding="utf-8")
        assert hash_file(frozen / "a.txt")[1] != manifest["files"][0]["sha256"]
    print("manifest self-test: PASS")


def main() -> None:
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    frozen = commands.add_parser("freeze")
    frozen.add_argument("--live", type=Path, required=True)
    frozen.add_argument("--frozen", type=Path, required=True)
    verify_parser = commands.add_parser("verify")
    verify_parser.add_argument("--repo", type=Path, required=True)
    verify_parser.add_argument("--frozen", type=Path, required=True)
    verify_parser.add_argument("--commit", required=True)
    commands.add_parser("self-test")
    args = parser.parse_args()
    if args.command == "freeze":
        freeze(args.live, args.frozen)
    elif args.command == "verify":
        verify(args.repo, args.frozen, args.commit)
    else:
        self_test()


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        print(f"manifest: {error}", file=sys.stderr)
        raise SystemExit(5)
