#!/usr/bin/env python3
"""Bind Ruling 197's observer rows to the installed-linkage capture window."""
from __future__ import annotations

import hashlib
import json
import statistics
from datetime import datetime, timedelta, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
PROOF = Path(__file__).resolve().parent
SAMPLES = ROOT / "docs/proof/win-l3-observer/samples.jsonl"
CAPTURE = PROOF / "capture.json"
OUTPUT = PROOF / "observer-window.json"
MIN_INTERVAL_SECONDS = 540.0
MAX_INTERVAL_SECONDS = 660.0
WINDOW = timedelta(minutes=30)


def parse_utc(text: str) -> datetime:
    return datetime.fromisoformat(text.replace("Z", "+00:00"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def median(values: list[float]) -> float | None:
    return statistics.median(values) if values else None


def main() -> None:
    capture = json.loads(CAPTURE.read_text(encoding="utf-8-sig"))
    linkage = next(record for record in capture["records"] if record["name"] == "installed-linkage")
    step_start = parse_utc(linkage["started_utc"])
    step_end = parse_utc(linkage["ended_utc"])
    rows = [json.loads(line) for line in SAMPLES.read_text(encoding="utf-8").splitlines() if line]
    intervals: list[dict[str, object]] = []
    rates: dict[str, list[float]] = {"before": [], "during": [], "after": []}

    for index, (left, right) in enumerate(zip(rows, rows[1:]), start=1):
        left_utc = parse_utc(left["utc"])
        right_utc = parse_utc(right["utc"])
        item: dict[str, object] = {
            "interval": index,
            "left_utc": left["utc"],
            "right_utc": right["utc"],
            "classification": "excluded",
            "reason": None,
        }
        if left["run_id"] != right["run_id"]:
            item["reason"] = "run identity changed"
        elif not isinstance(left["latest_complete_iteration"], (int, float)) or not isinstance(
            right["latest_complete_iteration"], (int, float)
        ):
            item["reason"] = "iteration not recorded"
        else:
            delta_seconds = float(right["monotonic_s"]) - float(left["monotonic_s"])
            delta_iterations = float(right["latest_complete_iteration"]) - float(left["latest_complete_iteration"])
            item["delta_monotonic_s"] = delta_seconds
            item["delta_iteration"] = delta_iterations
            if delta_seconds <= 0:
                item["reason"] = "nonpositive monotonic interval"
            elif delta_iterations < 0:
                item["reason"] = "iteration counter reset"
            elif not MIN_INTERVAL_SECONDS <= delta_seconds <= MAX_INTERVAL_SECONDS:
                item["reason"] = "outside 600-second schedule tolerance"
            else:
                rate = delta_iterations / delta_seconds
                item["rate_iterations_per_s"] = rate
                if left_utc >= step_start - WINDOW and right_utc <= step_start:
                    classification = "before"
                elif left_utc < step_end and right_utc > step_start:
                    classification = "during"
                elif left_utc >= step_end and right_utc <= step_end + WINDOW:
                    classification = "after"
                else:
                    classification = "outside-window"
                item["classification"] = classification
                item["reason"] = None
                if classification in rates:
                    rates[classification].append(rate)
        intervals.append(item)

    medians = {name: median(values) for name, values in rates.items()}
    baseline = medians["before"]
    during = medians["during"]
    slowdown_pct = None if baseline in (None, 0) or during is None else 100.0 * (during / baseline - 1.0)
    threshold = "not-recorded" if slowdown_pct is None else ("fired" if slowdown_pct <= -5.0 else "not-fired")
    observer_hashes = sorted({row["observer_sha256"] for row in rows})
    run_ids = sorted({row["run_id"] for row in rows})

    document = {
        "schema": "cfdw-ruling197-observer-window/1",
        "analysis_utc": datetime.now(timezone.utc).isoformat(),
        "samples_path": SAMPLES.relative_to(ROOT).as_posix(),
        "samples_sha256": sha256(SAMPLES),
        "capture_path": CAPTURE.relative_to(ROOT).as_posix(),
        "capture_sha256": sha256(CAPTURE),
        "step_record": "installed-linkage",
        "step_started_utc": linkage["started_utc"],
        "step_ended_utc": linkage["ended_utc"],
        "run_ids": run_ids,
        "observer_sha256_values": observer_hashes,
        "scheduled_interval_seconds": 600,
        "accepted_interval_seconds": [MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS],
        "before_window_minutes": 30,
        "after_window_minutes": 30,
        "intervals": intervals,
        "rates_iterations_per_s": rates,
        "median_iterations_per_s": medians,
        "during_vs_before_pct": slowdown_pct,
        "five_percent_slowdown_threshold": threshold,
        "limitation": "Each rate covers a 600-second interval; the 0.919-second linkage probe is only a small part of its during interval.",
    }
    OUTPUT.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
