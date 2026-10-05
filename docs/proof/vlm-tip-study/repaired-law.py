"""Historical, rejected convention (Ruling 88 D1): not a method and not in the tree.

Reduces the 1.1.0 CSV probe to fixed-eta calibration, raw/law flips and falsifiers.
Kept only to reproduce docs/proof/vlm-tip-study/repaired-verdict.md.
"""
import csv
import math
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).parent
ETA = (1 + math.cos(math.pi / 128)) / 2
NS = (16, 32, 64, 128, 256)
C = {16: 2.40, 32: 0.84, 64: 0.20, 128: 0.0, 256: 0.0}
rows = defaultdict(list)
for name in ("repaired-1.1.0.csv", "repaired-falsifiers-1.1.0.csv"):
    with (ROOT / name).open(newline="") as stream:
        for row in csv.DictReader(stream):
            if row["wing"].startswith("P"):
                continue
            key = (row["wing"], float(row["alpha"]), float(row["twist"]), int(row["n"]))
            rows[key].append((float(row["eta"]), float(row["alpha_i_deg"]),
                              float(row["alpha_eff"]), float(row["cl_local"]), float(row["verdict"].split("sweep=")[1])))
for strips in rows.values():
    strips.sort(reverse=True)

def at(strips):
    outer, second = strips[:2]
    if ETA >= outer[0]:
        a, b = outer, second
    else:
        a, b = next((strips[i], strips[i + 1]) for i in range(len(strips) - 1)
                    if strips[i][0] >= ETA >= strips[i + 1][0])
    x = math.log1p(-ETA)
    xa, xb = math.log1p(-a[0]), math.log1p(-b[0])
    return a[1] + (b[1] - a[1]) * (x - xa) / (xb - xa)

cases = sorted({key[:3] for key in rows})
needed = defaultdict(lambda: (0, ""))
for case in cases:
    reference = (at(rows[(*case, 128)]) + at(rows[(*case, 256)])) / 2
    print("CASE", *case, "reference", f"{reference:.9f}")
    for n in NS:
        strips = rows[(*case, n)]
        ai = at(strips)
        gap = abs(strips[0][1] - strips[1][1])
        err = abs(ai - reference)
        required = max(0, (err - .05) / gap) if gap else (math.inf if err > .05 else 0)
        if required > needed[n][0]:
            needed[n] = (required, str(case))
        print("POINT", *case, n, f"ai={ai:.9f}", f"gap={gap:.9f}", f"error={err:.9f}", f"need={required:.9f}")
    raw = []
    law = []
    values = []
    for n in NS:
        strips = rows[(*case, n)]
        outer = strips[0]
        raw.append("OUT" if outer[2] > 10 or outer[3] > 1 or abs(outer[4]) > 30 else "IN")
        ai = at(strips)
        u = C[n] * abs(outer[1] - strips[1][1]) + .05
        # Every strip outboard of eta* has its own twist, recovered as alpha_eff + alpha_i - alpha.
        outboard = [s for s in strips if s[0] >= ETA] or [outer]
        worst = max(abs(s[2] + s[1] - ai) for s in outboard)
        # The 2π rad^-1 section slope gives an independent angle from the strip's own Cl_local.
        # U describes only discretisation at eta*, so a disagreement is provisional.
        cl_implied = abs(outer[3]) / (2 * math.pi) * (180 / math.pi)
        other_outside = outer[3] > 1 or abs(outer[4]) > 30
        verdict = ("OUT" if other_outside else
                   "IN" if worst + u <= 10 else
                   "PROV" if cl_implied <= 10 else
                   "OUT" if worst - u > 10 else "BOUND")
        law.append(verdict)
        values.append(f"{worst:.3f}±{u:.3f}")
    implied = [abs(rows[(*case, n)][0][3]) / (2 * math.pi) * (180 / math.pi) for n in NS]
    print("FLIP", *case, "raw=" + "/".join(raw), "law=" + "/".join(law),
          "Cl_angle=" + "/".join(f"{v:.3f}" for v in implied), "values=" + "/".join(values))
print("CONSTANTS", [(n, needed[n]) for n in NS])
