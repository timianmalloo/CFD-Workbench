#!/usr/bin/env python3
"""Rebuild core-costs.tsv from the COST lines of one or more unpartitioned Core runs (mean ms, every check, registration order).

Usage: CFD_CORE_COST=1 dotnet run -c Release --project tests/CfdWorkbench.Core.Tests > run1.log   (repeat for run2.log)
       python3 tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.py run1.log run2.log
"""
import re, sys
from pathlib import Path

table = Path(__file__).with_name("core-costs.tsv")
order, samples = [], {}
for path in sys.argv[1:]:
    for line in open(path):
        m = re.fullmatch(r"COST (\S+) (\d+(?:\.\d+)?)\n?", line)
        if not m:
            continue
        if m.group(1) not in samples:
            order.append(m.group(1))
            samples[m.group(1)] = []
        samples[m.group(1)].append(float(m.group(2)))
rows = [(name, sum(samples[name]) / len(samples[name])) for name in order]
keep = rows
header = (
    "# Core harness check costs (ms). IdentityTests places each listed check on a ring part: longest first onto the lightest part.\n"
    "# Regenerate: python3 tests/CfdWorkbench.Core.Tests/Fixtures/core-costs.py <run logs made with CFD_CORE_COST=1> (see that file).\n"
    "# A stale or missing row costs balance, never coverage. Detector: the PARTITION-SKEW line of tools/run-tests.sh.\n")
table.write_text(header + "".join(f"{name}\t{ms:.0f}\n" for name, ms in keep))
print(f"checks={len(rows)} kept={len(keep)} total_ms={sum(ms for _, ms in rows):.0f} kept_ms={sum(ms for _, ms in keep):.0f}")
for name, ms in sorted(rows, key=lambda r: -r[1])[:20]:
    print(f"{ms:9.1f}  {name}")
