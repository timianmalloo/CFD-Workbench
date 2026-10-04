#!/usr/bin/env python3
"""Replay DesktopChecks.Spawn's slot schedule from measured SUITE-TIME lines.

usage: python3 docs/proof/test-cost/slot-sim.py <Desktop.log> [slots=8]
Spawn starts modes strictly in list order, each on the first free slot (WorkbenchTests.cs). This replays
that rule with the measured child durations, then replays a longest-first (LPT) order of the same
durations, and prints makespan, the lower bound max(longest, sum/slots) and slot utilisation.
A model on measured inputs: label its outputs Inferred. The durations come from one run.
"""
import heapq
import re
import sys

log = open(sys.argv[1], encoding="utf-8", errors="replace").read()
slots = int(sys.argv[2]) if len(sys.argv) > 2 else 8
modes = [(m, float(s)) for m, s in re.findall(r"SUITE-TIME (--.+?) ([0-9.]+) s$", log, re.M)]
if not modes:
    sys.exit("no SUITE-TIME lines")
# Optional what-if splits: `--properties-cells=2` replaces that mode by 2 parts of d/2 + 0.4 s start-up
# (the measured --shell-model process cost). Optimistic: real parts split unevenly around heavy checks.
for spec in sys.argv[3:]:
    name, count = spec.rsplit("=", 1)
    count = int(count)
    total_named = sum(s for m, s in modes if m.split(" ")[0] == name)
    modes = [m for m in modes if m[0].split(" ")[0] != name]
    modes += [(f"{name} --part={k}/{count}", total_named / count + 0.4) for k in range(1, count + 1)]


def replay(order):
    free = [0.0] * slots
    heapq.heapify(free)
    finish = []
    for name, seconds in order:
        start = heapq.heappop(free)
        end = start + seconds
        finish.append((end, name, start))
        heapq.heappush(free, end)
    return max(finish), finish


total = sum(s for _, s in modes)
bound = max(max(s for _, s in modes), total / slots)
for label, order in (("as listed", modes), ("longest first", sorted(modes, key=lambda m: -m[1]))):
    (span, last, start), _ = replay(order)
    print(f"{label:14} makespan {span:5.1f} s  last={last} (start {start:.1f} s)  "
          f"utilisation {total / (slots * span):.0%}")
print(f"children sum {total:.1f} s over {len(modes)} modes; lower bound at {slots} slots {bound:.1f} s")
