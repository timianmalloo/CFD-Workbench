#!/usr/bin/env python3
"""S6 tip gate for a snappyHexMesh run of the V1 coupon (W2a, W2b): the TIPGATE lines of mesh-locate-tipbl.py for the snappy
frame (x chord, y span, z thickness; root plane y = 0, cap at y = b/2). Same definitions as
docs/proof/spike-03/tip-bl-route/preregistration.md: tip region = centre at span >= b/2 - 2 mm; a full layer column = 20
layers (nSurfaceLayers, 0/nSurfaceLayers boundaryField wing, face order of constant/polyMesh).

Run: uv run --with pyyaml --with numpy --with scipy python3 cases/tools/mesh-locate-snappy.py <run-dir> <case.yaml>
Reads log.checkMesh, constant/{cellShapes,cellVolume,C}, constant/polyMesh/{owner,neighbour,boundary,sets/*}, 0/nSurfaceLayers,
log.snappyHexMesh (the layer table).
"""
import os
import re
import sys

import numpy as np
import yaml

run, case_yaml = sys.argv[1], sys.argv[2]
case = yaml.safe_load(open(case_yaml))
g, m = case["geometry"], case["mesh"]
chord, half = float(g["chord"]), float(g["half_span"])
n_lay = int(m["layers"]["n"])
SPAN = 1
PRISM, PYR, TET, HEX = 5, 6, 7, 3
cdir = os.path.join(run, "constant")


def body(path):
    t = open(path).read()
    k = t.find("internalField")
    if k >= 0:
        t = t[k:]
    else:
        t = t[t.find("}") + 1:]
    mm = re.search(r"\n(\d+)\s*\(", t)
    n = int(mm.group(1))
    start = mm.end()
    end = t.find("\n)", start) if "\n" in t[start:start + 2] else t.find(")", start)
    return n, t[start:end]


def scalars(path):
    n, b = body(path)
    a = np.array(b.split(), dtype=float)
    assert a.size == n, (path, a.size, n)
    return a


def vectors(path):
    n, b = body(path)
    a = np.array(b.replace("(", " ").replace(")", " ").split(), dtype=float).reshape(-1, 3)
    assert a.shape[0] == n, (path, a.shape, n)
    return a


def labels(path):
    if not os.path.exists(path):
        return np.zeros(0, dtype=np.int64)
    n, b = body(path)
    a = np.array(b.split(), dtype=np.int64)
    assert a.size == n, (path, a.size, n)
    return a


log = open(os.path.join(run, "log.checkMesh")).read()


def num(pattern, default=None):
    mm = re.search(pattern, log)
    return float(mm.group(1).rstrip(".")) if mm else default


non_ortho_max = num(r"Mesh non-orthogonality Max: ([\d.eE+-]+)")
n_severe = int(num(r"Number of severely non-orthogonal \(> 70 degrees\) faces: (\d+)", 0))
skew_max = num(r"Max skewness = ([\d.eE+-]+)")
weight_min = num(r"Face interpolation weight : minimum: ([\d.eE+-]+)")
vol_min = num(r"Min volume = ([\d.eE+-]+)")
errors = [ln.strip() for ln in log.splitlines() if ln.lstrip().startswith("***")]
errors_wo_det = [e for e in errors if "determinant" not in e]
snappy = open(os.path.join(run, "log.snappyHexMesh")).read()
tab = re.findall(r"\nwing\s+(\d+)\s+(\d+)\s+([\d.]+)\s+([\d.eE+-]+)\s+([\d.]+)", snappy)
print(f"GATE inputs: max_non_orthogonality={non_ortho_max} faces_gt_70={n_severe} max_skewness={skew_max} "
      f"min_face_weight={weight_min} min_volume={vol_min}")
print(f"  checkMesh *** lines: {errors_wo_det if errors_wo_det else 'none'}")
if tab:
    f_, tgt, avg, thick, pct = tab[-1]
    print(f"  snappy layer table (last): wing faces={f_} target={tgt} mean layers={avg} thickness={thick} m ({pct} % of target)")

shape = scalars(os.path.join(cdir, "cellShapes")).astype(int)
C = vectors(os.path.join(cdir, "C"))
owner = labels(os.path.join(cdir, "polyMesh", "owner"))
neigh = labels(os.path.join(cdir, "polyMesh", "neighbour"))
sets = os.path.join(cdir, "polyMesh", "sets")
bad_faces = labels(os.path.join(sets, "nonOrthoFaces"))
low_w = labels(os.path.join(sets, "lowWeightFaces"))
n_int = neigh.size
types, counts = np.unique(shape, return_counts=True)
print(f"cells={shape.size} by type: " + " ".join(f"{ {PRISM: 'prism', TET: 'tet', PYR: 'pyr', HEX: 'hex'}.get(t, str(t)) }={c}" for t, c in zip(types, counts)))
zt = half - 2e-3


def face_centres(ids):
    o = owner[ids]
    return np.where((ids < n_int)[:, None], 0.5 * (C[o] + C[neigh[np.minimum(ids, n_int - 1)]]), C[o])


def where(title, ids):
    if ids.size == 0:
        print(f"{title}: none")
        return 0
    P = face_centres(ids)
    tip = P[:, SPAN] >= zt
    print(f"{title}: n={ids.size} tip_region={int(tip.sum())} | x/c {P[:, 0].min() / chord:.4f}..{P[:, 0].max() / chord:.4f}, "
          f"span {P[:, SPAN].min():.4f}..{P[:, SPAN].max():.4f} m, tip-region span {P[tip, SPAN].min() if tip.any() else float('nan'):.4f}.."
          f"{P[tip, SPAN].max() if tip.any() else float('nan'):.4f}")
    return int(tip.sum())


n70 = where("faces > 70 deg (nonOrthoFaces)", bad_faces)
nlw = where("low-weight faces (< 0.05)", low_w)
vol_path = os.path.join(cdir, "cellVolume")
if os.path.exists(vol_path):
    vols = scalars(vol_path)
    neg = np.nonzero(vols <= 0)[0]
    nneg_tip, nneg_all = int((C[neg][:, SPAN] >= zt).sum()), int(neg.size)
    if neg.size:
        print(f"negative-volume cells: n={neg.size} types {dict(zip(*np.unique(shape[neg], return_counts=True)))} "
              f"span {C[neg][:, SPAN].min():.4f}..{C[neg][:, SPAN].max():.4f}")
else:
    nneg_tip = nneg_all = None
btxt = open(os.path.join(cdir, "polyMesh", "boundary")).read()
wb = re.search(r"\n\s*wing\s*\{[^}]*?nFaces\s+(\d+);[^}]*?startFace\s+(\d+);", btxt)
w_n, w_s = int(wb.group(1)), int(wb.group(2))
w_own = owner[w_s:w_s + w_n]
nsl_path = os.path.join(run, "0", "nSurfaceLayers")
nsl = None
if os.path.exists(nsl_path):
    t = open(nsl_path).read()
    k = t.index("wing", t.index("boundaryField"))
    mm = re.compile(r"value\s+nonuniform List<scalar>\s*(\d+)\s*\(").search(t, k)
    n = int(mm.group(1))
    nsl = np.array(t[mm.end():t.index(")", mm.end())].split()[:n], dtype=float)
    assert nsl.size == w_n, (nsl.size, w_n)
rows = np.nonzero(C[w_own][:, SPAN] >= zt)[0]
if nsl is None:
    full = None
    print("layers per face: not recorded (0/nSurfaceLayers absent)")
else:
    full = int((nsl[rows] >= n_lay).sum())
    hist = dict(zip(*np.unique(nsl[rows].astype(int), return_counts=True)))
    print(f"wing faces={w_n}; whole coupon faces with {n_lay} layers: {int((nsl >= n_lay).sum())} ({100.0 * (nsl >= n_lay).sum() / w_n:.2f} %), "
          f"mean layers {nsl.mean():.2f}; tip-region layer histogram {hist}")
cov = None if full is None else 100.0 * full / max(1, rows.size)
print(f"TIPGATE faces>70deg={n70} low_weight_faces={nlw} negative_volume_cells_tip={nneg_tip} (whole coupon {nneg_all}) "
      f"wing_faces_tip={rows.size} with_full_layers={full} ({'not recorded' if cov is None else f'{cov:.2f} %'})")
print("TIPGATE checkMesh error lines (whole coupon): " + (" | ".join(errors_wo_det) if errors_wo_det else "none"))
