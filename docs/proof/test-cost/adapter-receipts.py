#!/usr/bin/env python3
"""Print per-step durations from verify-application-adapters receipts (receipts/verification.json).

usage: python3 docs/proof/test-cost/adapter-receipts.py <scratch-dir> [...]
The receipt records each step's startUtc but no duration. A step's time is derived as the gap to the
next step's start (the last step ends at the receipt file's mtime), so it includes the gate's own
between-step bookkeeping. A step without a start stamp prints "not recorded", never a guess.
"""
import json
import sys
from datetime import datetime, timezone
from pathlib import Path


def stamp(value):
    try:
        return datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except ValueError:
        return None


for scratch in sys.argv[1:]:
    path = Path(scratch, "receipts", "verification.json")
    if not path.is_file():
        print(f"== {scratch}: no verification.json")
        continue
    data = json.loads(path.read_text(encoding="utf-8"))
    steps = data.get("steps", [])
    ends = [stamp(s.get("startUtc")) for s in steps[1:]]
    ends.append(datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc))
    print(f"== {Path(scratch).name} status={data.get('status')} head={str(data.get('head', ''))[:9]}")
    first = stamp(steps[0].get("startUtc")) if steps else None
    for step, end in zip(steps, ends):
        start = stamp(step.get("startUtc"))
        argv = step.get("argv") or []
        name = " ".join(Path(a).name for a in argv[:3])
        if start is None or end is None:
            print(f"not recorded  {name}")
            continue
        print(f"{(end - start).total_seconds():7.1f} s  {name}")
    if first is not None:
        print(f"{(ends[-1] - first).total_seconds():7.1f} s  first step start to receipt write")
