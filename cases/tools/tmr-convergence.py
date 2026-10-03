#!/usr/bin/env python3
"""Summarise one SPIKE-04 run: did residualControl stop it, final residuals, Cl/Cd/Cm and their last-1000 band.

Run: python3 cases/tools/tmr-convergence.py <run-dir>
Reads log.simpleFoam, postProcessing/solverInfo1/0/solverInfo.dat and forceCoeffs1/0/coefficient.dat.
"""
import os
import re
import sys

run = sys.argv[1]
log = open(os.path.join(run, "log.simpleFoam")).read()
m = re.search(r"SIMPLE solution converged in (\S+) iterations", log)
end = re.findall(r"ExecutionTime = (\S+) s\s+ClockTime = (\S+) s", log)


def table(rel):
    path = os.path.join(run, "postProcessing", rel)
    header, rows = None, []
    for line in open(path):
        if line.startswith("#"):
            if "Time" in line:
                header = line[1:].split()
            continue
        if line.strip():
            rows.append(line.split())
    return header, rows


h, rows = table("solverInfo1/0/solverInfo.dat")
last = dict(zip(h, rows[-1]))
res = {k: float(last[k]) for k in ("Ux_initial", "Uy_initial", "p_initial", "nuTilda_initial")}
hc, rc = table("forceCoeffs1/0/coefficient.dat")
col = {name: hc.index(name) for name in ("Cd", "Cl", "CmPitch")}
n = len(rc)
tail = rc[-1000:] if n >= 1000 else rc
print(f"iterations={n} stopped_by_residualControl={'yes at ' + m.group(1) if m else 'no'}")
print("final_initial_residuals " + " ".join(f"{k}={v:.3e}" for k, v in res.items()))
print(f"max_final_initial_residual={max(res.values()):.3e}")
for name, j in col.items():
    vals = [float(r[j]) for r in tail]
    print(f"{name}={float(rc[-1][j]):.9f} last{len(tail)}_band={max(vals) - min(vals):.3e}")
if end:
    print(f"execution_s={end[-1][0]} clock_s={end[-1][1]}")
yp = os.path.join(run, "postProcessing", "yPlus1")
for root, _, files in os.walk(yp):
    for f in files:
        lines = [l for l in open(os.path.join(root, f)) if not l.startswith("#") and l.strip()]
        if lines:
            print("yPlus(time patch min max avg)=" + " ".join(lines[-1].split()))
