#!/usr/bin/env python3
"""Summarize a closed native worker stream without mistaking thoughts for tools.

This is a qualification aid, not a sandbox or a substitute for a process receipt.
Pass a stable output file only after the process has exited.
"""

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stream", type=Path)
    parser.add_argument(
        "--require-completed-tool",
        action="append",
        default=[],
        metavar="NAME",
        help="fail unless a named tool has a completed update (repeatable)",
    )
    parser.add_argument(
        "--require-success-command",
        action="append",
        default=[],
        metavar="COMMAND",
        help="fail unless an exact terminal command completed with exit code 0",
    )
    args = parser.parse_args()
    data = args.stream.read_bytes()
    rows = []
    for number, line in enumerate(data.splitlines(), start=1):
        try:
            row = json.loads(line)
        except json.JSONDecodeError as error:
            parser.error(f"line {number} is not complete JSON: {error.msg}")
        if not isinstance(row, dict):
            parser.error(f"line {number} is not a JSON object")
        rows.append(row)

    counts = Counter(row.get("type", "<missing>") for row in rows)
    calls = {}
    for row in rows:
        if row.get("type") == "tool_call":
            call_id = row.get("toolCallId")
            if not call_id or call_id in calls:
                parser.error("missing or duplicate toolCallId")
            calls[call_id] = {
                "name": row.get("toolName"),
                "status": row.get("status"),
                "updates": 0,
                "command": (row.get("rawInput") or {}).get("command"),
                "exit_code": None,
            }
        elif row.get("type") == "tool_call_update":
            call_id = row.get("toolCallId")
            if call_id not in calls:
                parser.error(f"update without prior call: {call_id}")
            call = calls[call_id]
            call["updates"] += 1
            if row.get("status") is not None:
                call["status"] = row["status"]
            output = row.get("rawOutput")
            if isinstance(output, dict) and "exit_code" in output:
                call["exit_code"] = output["exit_code"]

    completed = {call["name"] for call in calls.values() if call["status"] == "completed"}
    missing = sorted(set(args.require_completed_tool) - completed)
    succeeded = {
        call["command"]
        for call in calls.values()
        if call["status"] == "completed" and call["exit_code"] == 0
    }
    missing_commands = sorted(set(args.require_success_command) - succeeded)
    result = {
        "stream": str(args.stream),
        "sha256": hashlib.sha256(data).hexdigest(),
        "rows": len(rows),
        "event_counts": dict(sorted(counts.items())),
        "calls": list(calls.values()),
        "missing_required_completed_tools": missing,
        "missing_required_success_commands": missing_commands,
    }
    print(json.dumps(result, indent=2, sort_keys=True))
    return 1 if missing or missing_commands else 0


if __name__ == "__main__":
    sys.exit(main())
