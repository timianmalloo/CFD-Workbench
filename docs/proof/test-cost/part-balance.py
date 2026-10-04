#!/usr/bin/env python3
"""Compare the interleaved `--part=k/n` rule with a cost-balanced split, from one timestamped full run.

usage: python3 docs/proof/test-cost/part-balance.py <stamped-full-run.txt> [n=2 3 4]
Check order in a full run is registration order, so index i lands in part (i % n) + 1 under the
harness rule. The balanced split is greedy longest-first over the measured per-check costs. Each part
also pays one process start (the first check's gap, measured). A model on measured inputs: Inferred.
"""
import re
import sys

costs, previous = [], 0.0
for line in open(sys.argv[1], encoding="utf-8", errors="replace"):
    match = re.match(r"\s*([0-9.]+) (PASS|FAIL) (\S+)", line)
    if match:
        at = float(match.group(1))
        costs.append(at - previous)
        previous = at
start = costs[0]
body = [0.0] + costs[1:]
for n in [int(a) for a in sys.argv[2:]] or [2, 3]:
    interleaved = [start + sum(c for i, c in enumerate(body) if i % n == k) for k in range(n)]
    balanced = [start] * n
    for c in sorted(body, reverse=True):
        balanced[balanced.index(min(balanced))] += c
    print(f"n={n}: interleaved parts {', '.join(f'{p:.1f}' for p in interleaved)} s (max {max(interleaved):.1f}); "
          f"balanced {', '.join(f'{p:.1f}' for p in sorted(balanced))} s (max {max(balanced):.1f})")
