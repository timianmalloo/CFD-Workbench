#!/usr/bin/env python3
"""Validate and evaluate each fresh L3 observer row for one live session."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import statistics
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ANCHOR_ROWS = 8
THRESHOLD_PERCENT = -5.0
MAX_UTC_AGE_SECONDS = 720.0
MIN_INTERVAL_SECONDS = 540.0
MAX_MONOTONIC_INTERVAL_SECONDS = 660.0
MAX_UTC_INTERVAL_SECONDS = 720.0
SCHEMA = "cfdw-live-observer-guard/2"


def utc_seconds(value: Any) -> float:
    if not isinstance(value, str):
        raise ValueError("utc is not a string")
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        raise ValueError("utc has no timezone")
    return parsed.timestamp()


def complete_lines(raw: bytes) -> list[bytes]:
    lines = raw.splitlines(keepends=True)
    if lines and not lines[-1].endswith(b"\n"):
        lines.pop()
    return lines


def prefix_bytes(raw: bytes, row_count: int) -> bytes:
    lines = complete_lines(raw)
    if len(lines) < row_count:
        raise ValueError(f"need {row_count} complete anchor rows, found {len(lines)}")
    return b"".join(lines[:row_count])


def numeric(row: dict[str, Any], name: str) -> float:
    value = row.get(name)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{name} is not numeric")
    result = float(value)
    if not math.isfinite(result):
        raise ValueError(f"{name} is not finite")
    return result


def validate_row(
    row: Any,
    *,
    row_number: int,
    expected_run_id: str,
    expected_observer_sha256: str,
    now_utc: float,
) -> dict[str, Any]:
    if not isinstance(row, dict):
        raise ValueError("row is not a JSON object")
    if row.get("run_id") != expected_run_id:
        raise ValueError("run identity changed")
    if row.get("observer_sha256") != expected_observer_sha256:
        raise ValueError("observer script identity changed")
    if row.get("unit_active_state") != "active":
        raise ValueError("L3 unit is not active")
    row_utc = utc_seconds(row.get("utc"))
    utc_age = now_utc - row_utc
    if utc_age < -5.0:
        raise ValueError("UTC timestamp is in the future")
    if utc_age > MAX_UTC_AGE_SECONDS:
        raise ValueError(f"UTC timestamp is stale ({utc_age:.3f} seconds old)")
    monotonic = numeric(row, "monotonic_s")
    iteration = numeric(row, "latest_complete_iteration")
    log_bytes = numeric(row, "log_bytes")
    log_mtime = numeric(row, "log_mtime")
    load_average = numeric(row, "loadavg_1m")
    if min(monotonic, iteration, log_bytes, log_mtime, load_average) < 0:
        raise ValueError("a recorded numeric field is negative")
    return {
        "row_number": row_number,
        "utc_s": row_utc,
        "monotonic_s": monotonic,
        "iteration": iteration,
        "utc_age_s": utc_age,
    }


def interval(left: dict[str, float], right: dict[str, float]) -> dict[str, float]:
    delta_iterations = right["iteration"] - left["iteration"]
    delta_utc = right["utc_s"] - left["utc_s"]
    delta_monotonic = right["monotonic_s"] - left["monotonic_s"]
    if delta_iterations < 0:
        raise ValueError("iteration counter reset")
    if not MIN_INTERVAL_SECONDS <= delta_monotonic <= MAX_MONOTONIC_INTERVAL_SECONDS:
        raise ValueError("monotonic interval is outside the frozen 540-660 second freshness bound")
    if not MIN_INTERVAL_SECONDS <= delta_utc <= MAX_UTC_INTERVAL_SECONDS:
        raise ValueError("UTC interval is outside the frozen 540-720 second freshness bound")
    return {
        "from_row": left["row_number"],
        "to_row": right["row_number"],
        "delta_iterations": delta_iterations,
        "delta_utc_s": delta_utc,
        "delta_monotonic_s": delta_monotonic,
        "rate_utc_iterations_per_s": delta_iterations / delta_utc,
        "rate_monotonic_iterations_per_s": delta_iterations / delta_monotonic,
        "utc_to_monotonic_ratio": delta_utc / delta_monotonic,
    }


def read_prior_state(
    events_path: Path, session_id: str, start_row: int
) -> tuple[int, dict[str, Any] | None, dict[str, float] | None, str | None]:
    """Return the session cursor, latest baseline row, and sticky stop reason."""
    if not events_path.exists():
        return start_row - 1, None, None, None
    cursor = start_row - 1
    baseline_row: dict[str, Any] | None = None
    baseline_rates: dict[str, float] | None = None
    stop_reason: str | None = None
    for raw_line in complete_lines(events_path.read_bytes()):
        try:
            event = json.loads(raw_line)
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            raise ValueError(f"event log contains an invalid complete row: {error}") from error
        if not isinstance(event, dict) or event.get("session_id") != session_id:
            continue
        if event.get("start_row") != start_row:
            raise ValueError("session id is already bound to a different start row")
        observed_row = event.get("observed_row")
        if isinstance(observed_row, bool) or not isinstance(observed_row, int):
            raise ValueError("prior event has no valid observed_row")
        cursor = max(cursor, observed_row)
        if event.get("baseline_row") is not None:
            baseline_row = event["baseline_row"]
        if event.get("baseline_rates") is not None:
            baseline_rates = event["baseline_rates"]
        if event.get("verdict") == "stop" and stop_reason is None:
            stop_reason = str(event.get("reason", "prior stop"))
    return cursor, baseline_row, baseline_rates, stop_reason


def evaluate_new_rows(
    raw: bytes,
    *,
    session_id: str,
    start_row: int,
    expected_prefix_sha256: str,
    now_utc: float | None = None,
    prior_cursor: int | None = None,
    prior_baseline_row: dict[str, Any] | None = None,
    prior_baseline_rates: dict[str, float] | None = None,
    prior_stop_reason: str | None = None,
) -> list[dict[str, Any]]:
    if start_row <= ANCHOR_ROWS:
        raise ValueError(f"start_row must be greater than the {ANCHOR_ROWS}-row integrity anchor")
    lines = complete_lines(raw)
    anchor = b"".join(lines[:ANCHOR_ROWS])
    if len(lines) < ANCHOR_ROWS:
        raise ValueError(f"need {ANCHOR_ROWS} complete anchor rows, found {len(lines)}")
    actual_hash = hashlib.sha256(anchor).hexdigest()
    if actual_hash != expected_prefix_sha256:
        raise ValueError(f"integrity anchor changed: expected {expected_prefix_sha256}, got {actual_hash}")
    anchor_row = json.loads(lines[ANCHOR_ROWS - 1])
    if not isinstance(anchor_row, dict):
        raise ValueError("integrity anchor row 8 is not a JSON object")
    expected_run_id = anchor_row.get("run_id")
    expected_observer_sha256 = anchor_row.get("observer_sha256")
    if not isinstance(expected_run_id, str) or not isinstance(expected_observer_sha256, str):
        raise ValueError("integrity anchor has no run or observer identity")

    now = datetime.now(timezone.utc).timestamp() if now_utc is None else now_utc
    cursor = start_row - 1 if prior_cursor is None else prior_cursor
    baseline_row = prior_baseline_row
    baseline_rates = prior_baseline_rates
    sticky_reason = prior_stop_reason
    parsed_rows: list[Any] = []
    output: list[dict[str, Any]] = []
    for index, line in enumerate(lines, start=1):
        if index < start_row or index <= cursor:
            continue
        observed_row = index
        base: dict[str, Any] = {
            "schema": SCHEMA,
            "session_id": session_id,
            "start_row": start_row,
            "observed_row": observed_row,
            "anchor_rows": ANCHOR_ROWS,
            "anchor_sha256": actual_hash,
        }
        try:
            row = json.loads(line)
            parsed_rows.append(row)
            validated = validate_row(
                row,
                row_number=observed_row,
                expected_run_id=expected_run_id,
                expected_observer_sha256=expected_observer_sha256,
                now_utc=now,
            )
            if sticky_reason is not None:
                base.update({"verdict": "stop", "reason": f"sticky stop: {sticky_reason}"})
            elif observed_row == start_row:
                baseline_row = validated
                base.update(
                    {
                        "verdict": "baseline-pending",
                        "reason": "first fresh baseline row",
                        "baseline_row": baseline_row,
                    }
                )
            elif observed_row == start_row + 1:
                if baseline_row is None:
                    raise ValueError("second fresh baseline row arrived without the first")
                fresh_baseline = interval(baseline_row, validated)
                baseline_row = validated
                baseline_rates = {
                    "utc": fresh_baseline["rate_utc_iterations_per_s"],
                    "monotonic": fresh_baseline["rate_monotonic_iterations_per_s"],
                }
                if any(not math.isfinite(rate) or rate <= 0.0 for rate in baseline_rates.values()):
                    raise ValueError("fresh baseline rates must be finite and strictly positive")
                base.update(
                    {
                        "verdict": "baseline-ready",
                        "reason": "fresh baseline formed from the two session rows",
                        "baseline_interval": fresh_baseline,
                        "baseline_rates": baseline_rates,
                        "baseline_row": baseline_row,
                    }
                )
            else:
                if baseline_row is None:
                    raise ValueError("fresh baseline is unavailable")
                latest = interval(baseline_row, validated)
                if baseline_rates is None:
                    raise ValueError("fresh baseline rates are unavailable")
                base_utc = baseline_rates["utc"]
                base_monotonic = baseline_rates["monotonic"]
                latest["utc_change_percent"] = 100.0 * (latest["rate_utc_iterations_per_s"] / float(base_utc) - 1.0)
                latest["monotonic_change_percent"] = 100.0 * (
                    latest["rate_monotonic_iterations_per_s"] / float(base_monotonic) - 1.0
                )
                fired = (
                    latest["utc_change_percent"] <= THRESHOLD_PERCENT
                    or latest["monotonic_change_percent"] <= THRESHOLD_PERCENT
                )
                if fired:
                    sticky_reason = "five-percent threshold fired on at least one clock"
                base.update(
                    {
                        "verdict": "stop" if fired else "continue",
                        "reason": sticky_reason or "threshold not fired",
                        "baseline_rates": baseline_rates,
                        "baseline_row": validated,
                        "latest_interval": latest,
                    }
                )
                baseline_row = validated
        except (KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
            sticky_reason = str(error)
            base.update({"verdict": "stop", "reason": sticky_reason})
        output.append(base)
        cursor = observed_row
    return output


def append_events(path: Path, events: list[dict[str, Any]]) -> None:
    if not events:
        return
    with path.open("ab") as stream:
        for event in events:
            stream.write(json.dumps(event, separators=(",", ":"), sort_keys=True).encode("utf-8") + b"\n")
        stream.flush()


def self_test() -> None:
    session = "self-test-session"
    rows: list[dict[str, Any]] = []
    origin = datetime(2026, 1, 1, tzinfo=timezone.utc).timestamp()
    for index in range(12):
        rows.append(
            {
                "utc": datetime.fromtimestamp(origin + index * 600, tz=timezone.utc).isoformat().replace("+00:00", "Z"),
                "monotonic_s": index * 600.0,
                "run_id": "run-a",
                "latest_complete_iteration": index * 480.0,
                "log_bytes": index * 1000,
                "log_mtime": origin + index * 600,
                "loadavg_1m": 1.0,
                "unit_active_state": "active",
                "observer_sha256": "a" * 64,
            }
        )

    def encoded(items: list[dict[str, Any]], trailing: bytes = b"") -> bytes:
        return b"".join(json.dumps(item, separators=(",", ":")).encode() + b"\n" for item in items) + trailing

    raw = encoded(rows)
    digest = hashlib.sha256(prefix_bytes(raw, ANCHOR_ROWS)).hexdigest()
    baseline_raw = encoded(rows[:10])
    event_rows = evaluate_new_rows(baseline_raw, session_id=session, start_row=9, expected_prefix_sha256=digest, now_utc=origin + 9 * 600)
    if [event["observed_row"] for event in event_rows] != [9, 10]:
        raise RuntimeError(f"did not process every unseen row: {event_rows}")
    if [event["verdict"] for event in event_rows[:2]] != ["baseline-pending", "baseline-ready"]:
        raise RuntimeError(f"fresh baseline readiness failed: {event_rows}")
    if any(event["session_id"] != session for event in event_rows):
        raise RuntimeError("event session binding failed")

    zero_baseline = [dict(row) for row in rows[:10]]
    zero_baseline[9]["latest_complete_iteration"] = zero_baseline[8]["latest_complete_iteration"]
    zero_events = evaluate_new_rows(
        encoded(zero_baseline),
        session_id="zero-baseline",
        start_row=9,
        expected_prefix_sha256=digest,
        now_utc=origin + 9 * 600,
    )
    if zero_events[-1]["verdict"] != "stop":
        raise RuntimeError("zero-progress baseline did not fail closed")

    partial = evaluate_new_rows(baseline_raw + b'{"utc":', session_id=session, start_row=11, expected_prefix_sha256=digest, now_utc=origin + 9 * 600)
    if partial:
        raise RuntimeError("trailing partial row was not ignored")

    slow = [dict(row) for row in rows[:11]]
    slow[-1]["latest_complete_iteration"] = slow[-2]["latest_complete_iteration"] + 450
    stop = evaluate_new_rows(
        encoded(slow),
        session_id=session,
        start_row=9,
        expected_prefix_sha256=digest,
        now_utc=origin + 10 * 600,
        prior_cursor=10,
        prior_baseline_row=event_rows[1]["baseline_row"],
        prior_baseline_rates=event_rows[1]["baseline_rates"],
    )
    if stop[-1]["verdict"] != "stop" or stop[-1]["latest_interval"]["utc_change_percent"] > THRESHOLD_PERCENT:
        raise RuntimeError(f"threshold stop failed: {stop[-1]}")

    invalid = [dict(row) for row in rows[:10]]
    invalid[9]["unit_active_state"] = "inactive"
    stopped = evaluate_new_rows(encoded(invalid), session_id=session, start_row=9, expected_prefix_sha256=digest, now_utc=origin + 9 * 600)
    if stopped[1]["verdict"] != "stop":
        raise RuntimeError("invalid-row stop did not remain sticky")
    sticky = evaluate_new_rows(
        encoded(rows[:11]),
        session_id=session,
        start_row=9,
        expected_prefix_sha256=digest,
        now_utc=origin + 10 * 600,
        prior_cursor=10,
        prior_baseline_row=event_rows[1]["baseline_row"],
        prior_baseline_rates=event_rows[1]["baseline_rates"],
        prior_stop_reason="inactive row",
    )
    if sticky[-1]["verdict"] != "stop":
        raise RuntimeError("sticky stop was cleared by a later valid row")

    cursor_events = evaluate_new_rows(
        encoded(rows[:12]),
        session_id=session,
        start_row=9,
        expected_prefix_sha256=digest,
        now_utc=origin + 11 * 600,
        prior_cursor=10,
        prior_baseline_row=event_rows[1]["baseline_row"],
        prior_baseline_rates=event_rows[1]["baseline_rates"],
        prior_stop_reason=None,
    )
    if [event["observed_row"] for event in cursor_events] != [11, 12]:
        raise RuntimeError(f"event cursor failed: {cursor_events}")
    print("observer_guard self-test: PASS")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--samples", type=Path)
    parser.add_argument("--events", type=Path)
    parser.add_argument("--start-row", type=int)
    parser.add_argument("--session-id")
    parser.add_argument("--expected-prefix-sha256")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    if None in (args.samples, args.events, args.start_row, args.session_id, args.expected_prefix_sha256):
        parser.error("capture mode needs --samples, --events, --start-row, --session-id and --expected-prefix-sha256")
    try:
        cursor, baseline_row, baseline_rates, sticky_reason = read_prior_state(args.events, args.session_id, args.start_row)
        events = evaluate_new_rows(
            args.samples.read_bytes(),
            session_id=args.session_id,
            start_row=args.start_row,
            expected_prefix_sha256=args.expected_prefix_sha256,
            prior_cursor=cursor,
            prior_baseline_row=baseline_row,
            prior_baseline_rates=baseline_rates,
            prior_stop_reason=sticky_reason,
        )
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        events = [
            {
                "schema": SCHEMA,
                "session_id": args.session_id,
                "start_row": args.start_row,
                "observed_row": args.start_row,
                "verdict": "stop",
                "reason": str(error),
            }
        ]
    append_events(args.events, events)
    latest = events[-1] if events else {
        "schema": SCHEMA,
        "session_id": args.session_id,
        "start_row": args.start_row,
        "observed_row": None,
        "verdict": "warm-up",
        "reason": "waiting for the first fresh session row",
    }
    print(json.dumps(latest, sort_keys=True))
    return 5 if latest["verdict"] == "stop" else 0


if __name__ == "__main__":
    sys.exit(main())
