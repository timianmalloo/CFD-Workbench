#!/usr/bin/env python3
"""Area-weighted y+ distribution on the wing patch from the sampled legacy-ASCII VTK surface (DR-F2-3 measure).

Run: python3 cases/tools/yplus-area.py <run-dir> <chord_m> <half_span_m> [span-z]
Reads the latest postProcessing/yPlusWing/<time>/wing.vtk (OpenFOAM `surfaces` sampling of the yPlus field, face
values, interpolate false). y+ there is OpenFOAM's yPlus function object value: the first cell-centre distance in wall
units. Prints area-weighted p50/p95/max overall and by region (LE 3 %, TE 3 %, tip, main surface).
DR-F2-3 gate: p95 <= 1 and max <= 2, measured on an A4-stationary field; a short solve is a screen, not the gate.
"""
import glob
import math
import os
import sys

run, chord, half = sys.argv[1], float(sys.argv[2]), float(sys.argv[3])
span = 2 if len(sys.argv) > 4 and sys.argv[4] == "span-z" else 1  # Gmsh probe frame: span along z
files = glob.glob(os.path.join(run, "postProcessing", "yPlusWing", "*", "wing.vtk"))
if not files:
    sys.exit("no postProcessing/yPlusWing/<time>/wing.vtk")
path = max(files, key=lambda p: float(os.path.basename(os.path.dirname(p))))
tok = open(path).read().split()
i = tok.index("POINTS")
n = int(tok[i + 1])
pts = [tuple(float(t) for t in tok[i + 3 + 3 * k:i + 6 + 3 * k]) for k in range(n)]
i = tok.index("POLYGONS")
nf = int(tok[i + 1])
pos, polys = i + 3, []
for _ in range(nf):
    m = int(tok[pos])
    polys.append([int(t) for t in tok[pos + 1:pos + 1 + m]])
    pos += 1 + m
j = tok.index("yPlus", pos)
while not tok[j + 1].lstrip("-").replace(".", "", 1).replace("e", "", 1).replace("-", "").replace("+", "").isdigit() or len(tok[j + 1]) < 1:
    j += 1
# legacy FIELD block: "yPlus 1 <n> float" or SCALARS block: "SCALARS yPlus float 1 LOOKUP_TABLE default"
if tok[j - 1] == "SCALARS":
    start = tok.index("default", j) + 1
else:
    start = j + 4
vals = [float(t) for t in tok[start:start + nf]]
assert len(vals) == nf, (len(vals), nf)


def area_centre(poly):
    p0 = pts[poly[0]]
    ax = ay = az = 0.0
    for a, b in zip(poly[1:-1], poly[2:]):
        u = [pts[a][q] - p0[q] for q in range(3)]
        v = [pts[b][q] - p0[q] for q in range(3)]
        ax += 0.5 * (u[1] * v[2] - u[2] * v[1])
        ay += 0.5 * (u[2] * v[0] - u[0] * v[2])
        az += 0.5 * (u[0] * v[1] - u[1] * v[0])
    c = [sum(pts[k][q] for k in poly) / len(poly) for q in range(3)]
    return math.sqrt(ax * ax + ay * ay + az * az), c


def region(c):
    if c[span] > half - 0.01 * chord:
        return "tip_cap_and_edge"
    if c[0] > 0.97 * chord:
        return "trailing_edge_3pct"
    if c[0] < 0.03 * chord:
        return "leading_edge_3pct"
    return "main_surface"


rows = []
for poly, v in zip(polys, vals):
    a, c = area_centre(poly)
    rows.append((v, a, region(c)))


def stats(sel):
    sel = sorted(sel)
    tot = sum(a for _, a, _ in sel)
    out, acc = {}, 0.0
    for v, a, _ in sel:
        acc += a
        for q in (0.5, 0.95):
            if q not in out and acc >= q * tot:
                out[q] = v
    return len(sel), tot, out.get(0.5, float("nan")), out.get(0.95, float("nan")), sel[-1][0]


n_all, tot, p50, p95, mx = stats(rows)
print(f"file={os.path.relpath(path, run)} faces={n_all} area_m2={tot:.6e}")
print(f"yPlus area-weighted p50={p50:.3f} p95={p95:.3f} max={mx:.3f}  gate(p95<=1, max<=2)={'pass' if p95 <= 1 and mx <= 2 else 'fail'}")
for r in sorted({r for _, _, r in rows}):
    n_r, t_r, a50, a95, amx = stats([x for x in rows if x[2] == r])
    print(f"region {r}: faces={n_r} p50={a50:.3f} p95={a95:.3f} max={amx:.3f}")
