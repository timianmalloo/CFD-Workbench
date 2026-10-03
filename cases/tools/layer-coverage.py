#!/usr/bin/env python3
"""Layer coverage and a y+ estimate on the wing patch from snappyHexMesh layer fields (processor*/0).

Run: python3 cases/tools/layer-coverage.py <run-dir> <n_target> <expansion_ratio> <U_m_s> <nu_m2_s> <chord_m>
Reads the binary or ascii 'value' list of boundaryField/wing in nSurfaceLayers and thickness.
y+ estimate (cell centre of the first layer) uses the flat-plate friction law Cf = 0.026 Re_x^(-1/7)
(White, Fluid Mechanics) at x = chord/2 — an estimate, not a measurement.
"""
import glob
import math
import os
import re
import struct
import sys

run, n_target, ratio, U, nu, chord = sys.argv[1], int(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5]), float(sys.argv[6])
# Round 2 (optional): argv[7] = second floor (layers on 100 % of faces, DR-F2-3), argv[8] = design half span (m), so
# a rounded tip cap beyond the half span is classed as tip.
floor2 = int(sys.argv[7]) if len(sys.argv) > 7 else None
half_design = float(sys.argv[8]) if len(sys.argv) > 8 else None


def wing_values(path):
    data = open(path, "rb").read()
    fmt = "binary" if re.search(rb"format\s+binary", data[:2000]) else "ascii"
    start = data.index(b"wing", data.index(b"boundaryField"))
    m = re.compile(rb"value\s+(nonuniform List<scalar>\s*(\d+)\s*\(|uniform\s+([-0-9.eE+]+))").search(data, start)
    if m.group(3) is not None:  # uniform: every face on this processor carries the same value
        boundary = open(os.path.join(os.path.dirname(os.path.dirname(path)), "constant", "polyMesh", "boundary")).read()
        faces = int(re.search(r"wing\s*\{[^}]*?nFaces\s+(\d+);", boundary).group(1))
        return [float(m.group(3))] * faces
    n = int(m.group(2))
    body = m.end()
    if fmt == "binary":
        return list(struct.unpack("<%dd" % n, data[body:body + 8 * n]))
    return [float(t) for t in data[body:data.index(b")", body)].split()[:n]]


layers, thick = [], []
for proc in sorted(glob.glob(os.path.join(run, "processor*"))):
    nl, th = wing_values(os.path.join(proc, "0", "nSurfaceLayers")), wing_values(os.path.join(proc, "0", "thickness"))
    assert len(nl) == len(th), proc
    layers += nl
    thick += th
N = len(layers)
full = sum(1 for v in layers if v >= n_target - 0.5)
none = sum(1 for v in layers if v < 0.5)
ge15 = sum(1 for v in layers if v >= 15 - 0.5)
mean = sum(layers) / N
first = []
for n, t in zip(layers, thick):
    if n >= 0.5 and t > 0:
        k = int(round(n))
        first.append(t * (ratio - 1) / (ratio ** k - 1) if ratio != 1 else t / k)
first.sort()
rex = U * (chord / 2) / nu
cf = 0.026 * rex ** (-1 / 7)
utau = U * math.sqrt(cf / 2)


def yplus(h):
    return 0.5 * h * utau / nu


def pct(p):
    return first[min(len(first) - 1, int(p * len(first)))]


print(f"wing_faces={N} mean_layers={mean:.2f} faces_with_target_{n_target}={full} ({100 * full / N:.1f}%) faces_ge_15={ge15} ({100 * ge15 / N:.1f}%) faces_with_0={none} ({100 * none / N:.1f}%)")
if floor2:
    g2 = sum(1 for v in layers if v >= floor2 - 0.5)
    print(f"faces_ge_{floor2}={g2} ({100 * g2 / N:.1f}%)")
print(f"first_layer_height_m p05={pct(.05):.3e} p50={pct(.5):.3e} p95={pct(.95):.3e}")
print(f"utau_flat_plate={utau:.4f} m/s (Re_x={rex:.3g}, Cf={cf:.5f}); y+_cell_centre_estimate p05={yplus(pct(.05)):.1f} p50={yplus(pct(.5)):.1f} p95={yplus(pct(.95)):.1f}")
centres = []
for proc in sorted(glob.glob(os.path.join(run, "processor*"))):
    if not os.path.exists(os.path.join(proc, "0", "Cx")):
        centres = []
        break
    centres += list(zip(*(wing_values(os.path.join(proc, "0", c)) for c in ("Cx", "Cy", "Cz"))))
if len(centres) == N:
    half = half_design if half_design is not None else max(c[1] for c in centres)

    def region(c):
        if c[1] > half - 0.01 * chord:
            return "tip_cap_and_edge"
        if c[0] > 0.97 * chord:
            return "trailing_edge_3pct"
        if c[0] < 0.03 * chord:
            return "leading_edge_3pct"
        if c[1] < 0.05 * chord:
            return "root_junction"
        return "main_surface"

    tally = {}
    for v, c in zip(layers, centres):
        r = region(c)
        t = tally.setdefault(r, [0, 0, 0, 0])
        t[0] += 1
        t[1] += v >= n_target - 0.5
        t[2] += v < 0.5
        t[3] += floor2 is not None and v >= floor2 - 0.5
    for r, (n, f, z, f2) in sorted(tally.items()):
        print(f"region {r}: faces={n} full={100 * f / n:.1f}% zero={100 * z / n:.1f}%" + (f" ge_{floor2}={100 * f2 / n:.1f}%" if floor2 else ""))
hist = {}
for v in layers:
    hist[int(round(v))] = hist.get(int(round(v)), 0) + 1
print("layers_histogram=" + " ".join(f"{k}:{hist[k]}" for k in sorted(hist)))
