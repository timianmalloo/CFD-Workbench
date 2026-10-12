#!/usr/bin/env python3
"""Bind and continuously verify the completed protected L3 run."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import subprocess
import sys
import tempfile
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCHEMA = "cfdw-l3-terminal-snapshot/1"
EVENT_SCHEMA = "cfdw-l3-completed-guard/1"
RUN_ID = "20261008T145043Z-win-spike04r3-g2-l3"
RUN_DIR = Path("/root/CFDWorkbench/runs") / RUN_ID
LOG_PATH = RUN_DIR / "log.simpleFoam"
UNIT_NAME = "cfdw-l3-20261008-r1.service"
TAIL_BYTES = 1_048_576
EXPECTED_FINAL_ITERATION = "120000"
MAX_SAMPLES = 732  # 61 minutes at 5 s; the host's 3600 s deadline is authoritative.


def canonical_bytes(value: Any) -> bytes:
    return json.dumps(value, allow_nan=False, separators=(",", ":"), sort_keys=True).encode("utf-8") + b"\n"


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def unit_state() -> dict[str, str]:
    properties = ("Result", "ExecMainCode", "ExecMainStatus", "ActiveState", "SubState")
    command = ["systemctl", "show", UNIT_NAME, "--no-pager"]
    for name in properties:
        command.extend(("--property", name))
    completed = subprocess.run(command, check=True, capture_output=True, text=True, timeout=10)
    parsed: dict[str, str] = {}
    for line in completed.stdout.splitlines():
        name, separator, value = line.partition("=")
        if separator and name in properties:
            parsed[name] = value
    missing = [name for name in properties if name not in parsed]
    if missing:
        raise ValueError(f"systemd state omitted: {','.join(missing)}")
    return parsed


def log_tail(path: Path) -> tuple[os.stat_result, bytes]:
    stat = path.stat()
    with path.open("rb") as stream:
        stream.seek(max(0, stat.st_size - TAIL_BYTES))
        tail = stream.read()
    return stat, tail


def collect_terminal(*, include_log_sha256: bool) -> dict[str, Any]:
    state = unit_state()
    stat, tail = log_tail(LOG_PATH)
    text = tail.decode("utf-8", errors="replace")
    times = re.findall(r"^Time = ([0-9]+(?:[.]0+)?)\s*$", text, flags=re.MULTILINE)
    snapshot: dict[str, Any] = {
        "schema": SCHEMA,
        "run_id": RUN_ID,
        "unit_name": UNIT_NAME,
        "result": state["Result"],
        "exec_main_code": state["ExecMainCode"],
        "exec_main_status": state["ExecMainStatus"],
        "active_state": state["ActiveState"],
        "sub_state": state["SubState"],
        "log_bytes": stat.st_size,
        "log_mtime_ns": stat.st_mtime_ns,
        "log_tail_sha256": sha256_bytes(tail),
        "latest_time": times[-1] if times else "Not recorded",
        "end_marker_count": len(re.findall(r"^End\s*$", text, flags=re.MULTILINE)),
        "finalising_marker_count": len(
            re.findall(r"^Finalising parallel run\s*$", text, flags=re.MULTILINE)
        ),
    }
    if include_log_sha256:
        snapshot["log_sha256"] = sha256_file(LOG_PATH)
    validate_terminal(snapshot, require_log_sha256=include_log_sha256)
    return snapshot


def validate_terminal(snapshot: Any, *, require_log_sha256: bool) -> None:
    if not isinstance(snapshot, dict):
        raise ValueError("terminal snapshot is not an object")
    exact = {
        "schema": SCHEMA,
        "run_id": RUN_ID,
        "unit_name": UNIT_NAME,
        "result": "success",
        "exec_main_code": "0",
        "exec_main_status": "0",
        "active_state": "inactive",
        "sub_state": "dead",
        "latest_time": EXPECTED_FINAL_ITERATION,
    }
    for name, expected in exact.items():
        if snapshot.get(name) != expected:
            raise ValueError(f"{name} changed: expected {expected}, got {snapshot.get(name)}")
    for name in ("log_bytes", "log_mtime_ns", "end_marker_count", "finalising_marker_count"):
        value = snapshot.get(name)
        if isinstance(value, bool) or not isinstance(value, int) or value <= 0:
            raise ValueError(f"{name} is not a positive integer")
    for name in ("log_tail_sha256",):
        value = snapshot.get(name)
        if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
            raise ValueError(f"{name} is not a SHA-256 digest")
    if require_log_sha256:
        value = snapshot.get("log_sha256")
        if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
            raise ValueError("log_sha256 is not a SHA-256 digest")


def runtime_projection(snapshot: dict[str, Any]) -> dict[str, Any]:
    return {name: value for name, value in snapshot.items() if name != "log_sha256"}


def compare_terminal(expected: dict[str, Any], observed: dict[str, Any]) -> str | None:
    projected = runtime_projection(expected)
    if observed == projected:
        return None
    names = sorted(set(projected) | set(observed))
    changed = [name for name in names if projected.get(name) != observed.get(name)]
    return "terminal snapshot changed: " + ",".join(changed)


def write_atomic_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(canonical_bytes(value))
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    except BaseException:
        try:
            os.unlink(temporary)
        except FileNotFoundError:
            pass
        raise


def append_event(path: Path, event: dict[str, Any]) -> None:
    with path.open("ab") as stream:
        stream.write(canonical_bytes(event))
        stream.flush()
        os.fsync(stream.fileno())


def bind_events(args: argparse.Namespace) -> None:
    raw = args.events.read_bytes()
    last_lf = raw.rfind(b"\n")
    if last_lf < 0:
        raise ValueError("completed guard has no complete event rows")
    complete = raw[: last_lf + 1]
    rows = complete.splitlines()
    if not rows:
        raise ValueError("completed guard has no complete event rows")
    for index, row in enumerate(rows, start=1):
        event = json.loads(row)
        if not isinstance(event, dict):
            raise ValueError(f"event row {index} is not an object")
        if event.get("schema") != EVENT_SCHEMA or event.get("session_id") != args.session_id:
            raise ValueError(f"event row {index} changed schema or session identity")
        if event.get("sample_number") != index:
            raise ValueError(f"event row {index} has a non-contiguous sample number")
        if event.get("verdict") not in ("continue", "stop"):
            raise ValueError(f"event row {index} has an invalid verdict")
    args.snapshot.write_bytes(complete)
    binding = {
        "schema": "cfdw-l3-completed-guard-binding/1",
        "session_id": args.session_id,
        "rows": len(rows),
        "bytes": len(complete),
        "sha256": sha256_bytes(complete),
        "trailing_incomplete_bytes_excluded": len(raw) - len(complete),
    }
    write_atomic_json(args.binding, binding)


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def watch(args: argparse.Namespace) -> int:
    expected_bytes = args.expected.read_bytes()
    if sha256_bytes(expected_bytes) != args.expected_sha256:
        raise ValueError("expected terminal snapshot hash changed")
    expected = json.loads(expected_bytes)
    validate_terminal(expected, require_log_sha256=True)
    allowed = sorted(os.sched_getaffinity(0))
    nice = os.getpriority(os.PRIO_PROCESS, 0)
    if not allowed or not set(allowed).issubset({0}) or nice < 10:
        raise ValueError(f"guard affinity/nice mismatch: cpus={allowed} nice={nice}")
    if args.events.exists() or args.state.exists():
        raise ValueError("completed guard output already exists")
    completed_samples = 0
    last_sample_utc = "Not recorded"
    for sample_number in range(1, args.max_samples + 1):
        sampled_utc = utc_now()
        reason: str | None = None
        observed: dict[str, Any] | None = None
        try:
            observed = collect_terminal(include_log_sha256=False)
            reason = compare_terminal(expected, observed)
        except (OSError, subprocess.SubprocessError, ValueError) as error:
            reason = str(error)
        verdict = "stop" if reason is not None else "continue"
        event = {
            "schema": EVENT_SCHEMA,
            "session_id": args.session_id,
            "sample_number": sample_number,
            "utc": sampled_utc,
            "monotonic_s": time.monotonic(),
            "loadavg_1m": os.getloadavg()[0],
            "expected_sha256": args.expected_sha256,
            "observed": observed if observed is not None else "Not recorded",
            "verdict": verdict,
            "reason": reason or "terminal snapshot unchanged",
            "owner": {
                "pid": os.getpid(),
                "pgid": os.getpgid(0),
                "sid": os.getsid(0),
                "cmdline": Path("/proc/self/cmdline").read_bytes().replace(b"\0", b" ").decode("utf-8", "replace"),
                "cpus_allowed": allowed,
                "nice": nice,
            },
        }
        append_event(args.events, event)
        completed_samples = sample_number
        last_sample_utc = sampled_utc
        write_atomic_json(
            args.state,
            {
                "schema": EVENT_SCHEMA,
                "state": "stopped" if reason is not None else "running",
                "pid": os.getpid(),
                "session_id": args.session_id,
                "completed_samples": completed_samples,
                "last_sample_utc": last_sample_utc,
                "expected_sha256": args.expected_sha256,
                "exit_code": 5 if reason is not None else "Not recorded",
            },
        )
        print(json.dumps(event, sort_keys=True), flush=True)
        if reason is not None:
            return 5
        deadline = time.monotonic() + args.interval_seconds
        while time.monotonic() < deadline:
            if args.stop_request.exists():
                write_atomic_json(
                    args.state,
                    {
                        "schema": EVENT_SCHEMA,
                        "state": "stopped",
                        "pid": os.getpid(),
                        "session_id": args.session_id,
                        "completed_samples": completed_samples,
                        "last_sample_utc": last_sample_utc,
                        "expected_sha256": args.expected_sha256,
                        "exit_code": 0,
                    },
                )
                return 0
            time.sleep(min(0.2, max(0.0, deadline - time.monotonic())))
    write_atomic_json(
        args.state,
        {
            "schema": EVENT_SCHEMA,
            "state": "stopped",
            "pid": os.getpid(),
            "session_id": args.session_id,
            "completed_samples": completed_samples,
            "last_sample_utc": last_sample_utc,
            "expected_sha256": args.expected_sha256,
            "exit_code": 5,
        },
    )
    raise ValueError("completed guard reached its bounded sample limit before a stop request")


def self_test() -> None:
    sample = {
        "schema": SCHEMA,
        "run_id": RUN_ID,
        "unit_name": UNIT_NAME,
        "result": "success",
        "exec_main_code": "0",
        "exec_main_status": "0",
        "active_state": "inactive",
        "sub_state": "dead",
        "log_bytes": 10,
        "log_mtime_ns": 20,
        "log_tail_sha256": "a" * 64,
        "latest_time": EXPECTED_FINAL_ITERATION,
        "end_marker_count": 1,
        "finalising_marker_count": 1,
        "log_sha256": "b" * 64,
    }
    validate_terminal(sample, require_log_sha256=True)
    if compare_terminal(sample, runtime_projection(sample)) is not None:
        raise RuntimeError("unchanged terminal snapshot was rejected")
    changed = runtime_projection(sample)
    changed["log_bytes"] += 1
    if compare_terminal(sample, changed) != "terminal snapshot changed: log_bytes":
        raise RuntimeError("changed terminal snapshot was accepted")
    nonfinite_event = {"monotonic_s": float("nan")}
    try:
        canonical_bytes(nonfinite_event)
    except ValueError:
        pass
    else:
        raise RuntimeError("nonfinite JSON control did not fail")
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        events = root / "events.jsonl"
        snapshot = root / "snapshot.jsonl"
        binding = root / "binding.json"
        first = {"schema": EVENT_SCHEMA, "session_id": "bind-test", "sample_number": 1, "verdict": "continue"}
        second = {"schema": EVENT_SCHEMA, "session_id": "bind-test", "sample_number": 2, "verdict": "stop"}
        complete = canonical_bytes(first) + canonical_bytes(second)
        events.write_bytes(complete + b'{"unfinished":')
        bind_events(
            argparse.Namespace(
                events=events,
                snapshot=snapshot,
                binding=binding,
                session_id="bind-test",
            )
        )
        bound = json.loads(binding.read_bytes())
        if snapshot.read_bytes() != complete or bound["trailing_incomplete_bytes_excluded"] != 14:
            raise RuntimeError("complete-prefix binding did not exclude the partial row")
    print("completed_guard self-test: PASS")


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)
    snapshot_parser = subparsers.add_parser("snapshot")
    snapshot_parser.add_argument("--output", type=Path, required=True)
    snapshot_parser.add_argument("--include-log-sha256", action="store_true")
    watch_parser = subparsers.add_parser("watch")
    watch_parser.add_argument("--expected", type=Path, required=True)
    watch_parser.add_argument("--expected-sha256", required=True)
    watch_parser.add_argument("--events", type=Path, required=True)
    watch_parser.add_argument("--state", type=Path, required=True)
    watch_parser.add_argument("--stop-request", type=Path, required=True)
    watch_parser.add_argument("--session-id", required=True)
    watch_parser.add_argument("--interval-seconds", type=float, default=5.0)
    watch_parser.add_argument("--max-samples", type=int, default=MAX_SAMPLES)
    bind_parser = subparsers.add_parser("bind")
    bind_parser.add_argument("--events", type=Path, required=True)
    bind_parser.add_argument("--snapshot", type=Path, required=True)
    bind_parser.add_argument("--binding", type=Path, required=True)
    bind_parser.add_argument("--session-id", required=True)
    subparsers.add_parser("self-test")
    args = parser.parse_args()
    try:
        if args.command == "snapshot":
            write_atomic_json(args.output, collect_terminal(include_log_sha256=args.include_log_sha256))
            return 0
        if args.command == "watch":
            if not math.isfinite(args.interval_seconds) or args.interval_seconds <= 0:
                raise ValueError("interval-seconds must be finite and positive")
            if args.max_samples <= 0 or args.max_samples > MAX_SAMPLES:
                raise ValueError(f"max-samples must be in 1..{MAX_SAMPLES}")
            return watch(args)
        if args.command == "bind":
            bind_events(args)
            return 0
        self_test()
        return 0
    except (OSError, subprocess.SubprocessError, ValueError, json.JSONDecodeError) as error:
        print(f"completed_guard: {error}", file=sys.stderr)
        return 5


if __name__ == "__main__":
    raise SystemExit(main())
