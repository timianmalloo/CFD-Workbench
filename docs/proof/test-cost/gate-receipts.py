#!/usr/bin/env python3
"""Print the per-step durations a verify-application-core run left in its scratch receipts.

usage: python3 docs/proof/test-cost/gate-receipts.py /tmp/cfd-application-core-20260923-XXXX [...]
Reads receipts/process-<pid>.json (written by tools/verify-application-core.py) in start order.
A step with no duration prints "not recorded", never a guess.
"""
import json
import sys
from pathlib import Path

for scratch in sys.argv[1:]:
    receipts = sorted(Path(scratch, "receipts").glob("process-*.json"), key=lambda p: p.stat().st_mtime)
    total = 0.0
    print(f"== {scratch}")
    for path in receipts:
        data = json.loads(path.read_text(encoding="utf-8"))
        command = data.get("command") or []
        step = " ".join(Path(c).name if "/" in c else c for c in command[:3])
        seconds = data.get("duration_seconds")
        label = f"{seconds:7.1f} s" if isinstance(seconds, (int, float)) else "not recorded"
        if isinstance(seconds, (int, float)):
            total += seconds
        print(f"{label}  umask={data.get('child_umask')}  suite={data.get('suite', '-')}  {step}")
    print(f"{total:7.1f} s  sum of recorded steps")
