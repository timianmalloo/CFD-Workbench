#!/usr/bin/env python3
"""Stop/check only an exact R217 attempt's Linux process groups; fail on unknown state."""
from __future__ import annotations

import argparse
import hashlib
import os
import re
import signal
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Callable

STAGES = ("probe", "repositories", "simulate", "download", "inspect", "delete-debs")


@dataclass(frozen=True)
class ProcessRow:
    pid: int
    pgid: int
    sid: int
    argv: tuple[str, ...]


def read_processes(run: Callable[[], subprocess.CompletedProcess[str]] | None = None,
                   read_argv: Callable[[int], bytes] | None = None) -> list[ProcessRow]:
    if run is None:
        run = lambda: subprocess.run(["ps", "-e", "-o", "pid=,pgid=,sid="],
                                     check=True, capture_output=True, text=True, encoding="utf-8", timeout=1)
    if read_argv is None:
        read_argv = lambda pid: Path(f"/proc/{pid}/cmdline").read_bytes()
    try:
        result = run()
    except (OSError, subprocess.SubprocessError) as error:
        raise RuntimeError(f"process inspection failed: {error}") from error
    if result.returncode != 0:
        raise RuntimeError(f"process inspection exit {result.returncode}")
    rows = []
    for line in result.stdout.splitlines():
        fields = line.strip().split()
        if len(fields) != 3 or not all(value.isdecimal() for value in fields):
            raise RuntimeError(f"unparseable process row: {line}")
        try:
            raw = read_argv(int(fields[0]))
            if raw and not raw.endswith(b"\0"):
                raise ValueError("unterminated argv")
            argv = tuple(value.decode("utf-8", errors="strict") for value in raw.split(b"\0")[:-1])
        except FileNotFoundError as error:
            # ps may list itself or another process that exits before /proc is read.
            # Only independent PID absence permits omission; denial/unknown is fatal.
            try:
                Path(f"/proc/{fields[0]}").stat()
            except FileNotFoundError:
                continue
            except OSError as inspection_error:
                raise RuntimeError(f"PID inspection failed for {fields[0]}: {inspection_error}") from inspection_error
            raise RuntimeError(f"argv missing for present pid {fields[0]}") from error
        except (OSError, UnicodeError, ValueError) as error:
            raise RuntimeError(f"argv inspection failed for pid {fields[0]}: {error}") from error
        rows.append(ProcessRow(*(int(value) for value in fields), argv))
    return rows


def read_owner_records(run_root: Path, kind: str = "stage") -> dict[str, int]:
    owners = {}
    names = STAGES if kind == "stage" else ("guard",)
    for stage in names:
        canonical = run_root / f"{stage}.owner.txt"
        path = canonical if canonical.exists() else run_root / f"{stage}.entry.txt"
        if not path.exists():
            continue
        absent = run_root / f"{stage}.residual-absent.sha256"
        if kind == "stage" and absent.exists():
            if not canonical.exists():
                raise ValueError(f"absence proof lacks canonical owner: {stage}")
            expected = absent.read_text(encoding="ascii").strip()
            actual = hashlib.sha256(canonical.read_bytes()).hexdigest()
            if expected != actual:
                raise ValueError(f"stage owner changed after absence proof: {stage}")
            continue
        fields = dict(line.split("=", 1) for line in path.read_text(encoding="utf-8").splitlines()
                      if "=" in line)
        values = [fields.get(key, "") for key in ("pid", "pgid", "sid")]
        if not all(value.isdecimal() and int(value) > 0 for value in values) or len(set(values)) != 1:
            raise ValueError(f"partial/invalid stage owner: {stage}")
        owners[stage] = int(values[1])
    legacy = run_root / "current-owner.pid"
    if legacy.exists() and not re.fullmatch(r"[1-9][0-9]*\n?", legacy.read_text(encoding="ascii")):
        raise ValueError("partial current owner PID")
    return owners


def find_groups(rows: list[ProcessRow], run_root: str, run_id: str,
                recorded: dict[str, int], kind: str = "stage") -> set[int]:
    if any(not isinstance(group, int) or group <= 0 for group in recorded.values()):
        raise ValueError("partial owner group")
    # The fixed packet layout is <preflight>/packet and <preflight>/run-1/live.
    packet = PurePosixPath(run_root).parent.parent / "packet"
    script = str(packet / ("preflight.sh" if kind == "stage" else "completed_guard.py"))

    def attempt_argv(row: ProcessRow) -> tuple[str, ...]:
        argv = row.argv
        if argv[:2] == ("/bin/bash", str(packet / "identity.sh")):
            # Atomic entry ownership can precede exec; retain exact launch identity
            # through that seam, rather than accepting an arbitrary shell command.
            if len(argv) < 7 or argv[3] != "--":
                return ()
            command = argv[4:]
            stage = command[3] if kind == "stage" and len(command) > 3 else "guard"
            owner_name = f"{stage}.entry.txt" if kind == "stage" else "guard.owner.txt"
            if argv[2] != str(PurePosixPath(run_root) / owner_name):
                return ()
            return command
        return argv

    def matches(row: ProcessRow) -> bool:
        argv = attempt_argv(row)
        expected_head = ("/bin/bash", script, "--stage") if kind == "stage" else (
            "/usr/bin/python3", script, "watch")
        if argv[:3] != expected_head:
            return False
        if kind == "stage" and (len(argv) < 4 or argv[3] not in STAGES):
            return False
        options = (("--run-root", run_root), ("--run-id", run_id)) if kind == "stage" else (
            ("--session-id", run_id), ("--events", str(PurePosixPath(run_root) / "completed-guard-events.jsonl")))
        for option, value in options:
            positions = [index for index, token in enumerate(argv) if token == option]
            if len(positions) != 1 or positions[0] + 1 >= len(argv) or argv[positions[0] + 1] != value:
                return False
        return True

    groups = set()
    for stage, group in recorded.items():
        # Owner publication requires pid == pgid == sid. If that pid still exists,
        # its exact identity and argv must agree before any signal is allowed.
        leader = next((row for row in rows if row.pid == group), None)
        if leader is not None and (leader.pgid != group or leader.sid != group or
                                   not matches(leader) or
                                   (kind == "stage" and attempt_argv(leader)[3] != stage)):
            raise ValueError(f"recorded owner identity/argv changed: {stage}")
        if any(row.pgid == group for row in rows):
            groups.add(group)
    for row in rows:
        if matches(row):
            if row.pid != row.pgid or row.sid != row.pgid:
                raise ValueError("attempt leader is not session/group leader")
            groups.add(row.pgid)
    return {group for group in groups if any(row.pgid == group for row in rows)}


def send_group_signal(group: int, sig: int) -> None:
    if not hasattr(os, "killpg"):
        raise RuntimeError("POSIX process-group signal unavailable")
    os.killpg(group, sig)


def control(mode: str, run_root: Path, run_id: str, kind: str = "stage") -> None:
    if not re.fullmatch(r"[A-Za-z0-9._-]+", run_id):
        raise ValueError("invalid attempt id")
    records = read_owner_records(run_root, kind)
    rows = read_processes()
    groups = find_groups(rows, str(run_root), run_id, records, kind)
    if kind == "stage":
        unresolved = [stage for stage in STAGES if (run_root / f"{stage}.intent.json").exists() and
                      not (run_root / f"{stage}.owner.txt").exists() and
                      not (run_root / f"{stage}.entry.txt").exists()]
        if unresolved and not groups:
            raise RuntimeError(f"attempt intent has no ownership acknowledgement: {unresolved}")
    if mode == "check":
        if groups:
            raise RuntimeError(f"residual attempt groups: {sorted(groups)}")
        print("OWNED-ABSENT")
        return
    for group in sorted(groups):
        send_group_signal(group, signal.SIGTERM)
    deadline = time.monotonic() + 8.0
    while time.monotonic() < deadline:
        time.sleep(0.2)
        groups = find_groups(read_processes(), str(run_root), run_id, records, kind)
        if not groups:
            print("OWNED-STOPPED-TERM")
            return
    for group in sorted(groups):
        send_group_signal(group, signal.SIGKILL)
    time.sleep(0.1)
    groups = find_groups(read_processes(), str(run_root), run_id, records, kind)
    if groups:
        raise RuntimeError(f"owned groups survived KILL: {sorted(groups)}")
    print("OWNED-STOPPED-KILL")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=("check", "stop"), required=True)
    parser.add_argument("--run-root", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--kind", choices=("stage", "guard"), default="stage")
    args = parser.parse_args()
    control(args.mode, args.run_root, args.run_id, args.kind)


if __name__ == "__main__":
    try:
        main()
    except (OSError, RuntimeError, ValueError, subprocess.SubprocessError) as error:
        print(f"owned-control: {error}", file=sys.stderr)
        raise SystemExit(5)
