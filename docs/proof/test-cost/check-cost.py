#!/usr/bin/env python3
"""Per-check cost from a timestamped harness log (docs/proof/test-cost/stamp.py output).

usage: python3 docs/proof/test-cost/check-cost.py <stamped.txt> [top=15]
A check's cost is the gap from the previous PASS/FAIL line to its own, so set-up printed between
checks is charged to the next check. The first check also carries process start. Prints the top
checks, the share of the suite they hold, and a histogram. Measured on one run, one load level.
"""
import re
import sys

top = int(sys.argv[2]) if len(sys.argv) > 2 else 15
rows, previous = [], 0.0
for line in open(sys.argv[1], encoding="utf-8", errors="replace"):
    match = re.match(r"\s*([0-9.]+) (PASS|FAIL) (\S+)", line)
    if match:
        at = float(match.group(1))
        rows.append((at - previous, match.group(3)))
        previous = at
if not rows:
    sys.exit("no PASS/FAIL lines")
total = sum(cost for cost, _ in rows)
rows.sort(reverse=True)
print(f"{len(rows)} checks, {total:.1f} s from start to last check")
running = 0.0
for cost, name in rows[:top]:
    running += cost
    print(f"{cost:6.2f} s  {running / total:4.0%}  {name}")
bands = [(0, .1), (.1, .5), (.5, 1), (1, 2), (2, 4), (4, 1e9)]
for low, high in bands:
    hits = [c for c, _ in rows if low <= c < high]
    print(f"[{low:>4}, {high if high < 1e9 else 'inf':>4}) s: {len(hits):4} checks, {sum(hits):6.1f} s")
