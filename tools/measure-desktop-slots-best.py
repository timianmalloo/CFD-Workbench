#!/usr/bin/env python3
"""Pick the Desktop slot count from a tools/measure-desktop-slots.sh CSV (track DTJ).

  python3 tools/measure-desktop-slots-best.py <csv>        prints one line per N and the best N; exit 0, 1 when none qualifies,
                                                           or 3 (AT-EDGE) when the best is the largest N tested

A slot count qualifies when every rep has verdict `pass` (the harness log classified against the expected-failure manifest,
as tools/run-tests.sh does on Windows; the raw `exit` column is kept but not used) with zero TIMEOUTs. The best is the qualifying N with the lowest median wall.
It reads a CSV and prints; it changes no baseline, cap or budget.
"""
import csv
import statistics
import sys
from collections import defaultdict

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    runs: dict[int, list[dict[str, str]]] = defaultdict(list)
    with open(argv[1], encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            runs[int(row["slots"])].append(row)
    medians: dict[int, float] = {}
    for slots in sorted(runs):
        rows = runs[slots]
        median = statistics.median(int(r["wall_ms"]) for r in rows)
        timeouts = sum(int(r["timeouts"]) for r in rows)
        drains = sum(int(r["drains"]) for r in rows)
        failed = sum(r["verdict"] != "pass" for r in rows)
        ok = timeouts == 0 and failed == 0
        print(f"slots {slots}: median wall {median / 1000:.1f} s over {len(rows)} rep(s), {timeouts} TIMEOUT, {drains} DRAIN, {failed} non-pass verdict"
              + ("" if ok else " (disqualified)"))
        if ok:
            medians[slots] = median
    if not medians:
        print("BEST none: every slot count had a TIMEOUT or a failing run")
        return 1
    best = min(medians, key=lambda n: (medians[n], n))
    if best == max(runs) and len(runs) > 1:
        print(f"AT-EDGE {best} (median wall {medians[best] / 1000:.1f} s): the wall was still falling at the largest N tested, so the "
              "optimum may lie above it. Measure larger N (up to the Desktop mode count) before committing limit.desktop_slots.")
        return 3
    print(f"BEST {best} (median wall {medians[best] / 1000:.1f} s, zero TIMEOUTs); commit limit.desktop_slots={best} with the host's quiet baseline rows")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
