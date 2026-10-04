#!/usr/bin/env python3
"""Round-3 SPIKE-03 mesh gate (DR-F3-1 option A, Ruling 68) and bad-face locator for one Gmsh wing run (R3-M0..M3).

Run: uv run --with pyyaml --with numpy --with scipy python3 cases/tools/mesh-locate-r3.py <run-dir> <case.yaml>
Reads (all written by the pipeline cases/tools/mesh-gate-r3.sh; the case writes ASCII):
  log.checkMesh                       checkMesh -constant -allGeometry -allTopology -writeSets vtk
                                      -writeFields '(nonOrthoAngle cellDeterminant cellShapes faceWeight)'
  constant/{cellShapes,cellDeterminant,nonOrthoAngle,C}   per-cell fields (C from postProcess writeCellCentres)
  constant/polyMesh/{owner,neighbour}, constant/polyMesh/sets/{nonOrthoFaces,lowWeightFaces,twoInternalFacesCells}
  generator.txt                       layer prisms vs 20 x wing faces, TE arc segment count
Prints the gate (DR-F3-1 A: checkMesh validity set without the determinant test, max non-orthogonality <= 70 deg,
max skewness <= 4 (checkMesh's overall maximum; boundary limit 20), positive volumes, face pyramids OK, face weight
>= 0.05; layer coverage) and the location of every face > 70 deg, every low-weight face and every two-internal-face
cell, by region. cellDeterminant < 0.001 is reported split by layer (prism) and core (tet) cells; it is not gated.
Regions, first match wins (every flag is also counted): tip_pole (within 2 mm of a revolve pole), te (x >= c - 1 mm
and wall distance < 2.5 mm), root (z < 2 mm), interface (a face between a prism and a tet), tip (z > half span),
main. Wall distance is analytic: the distance in the (x, rho) plane to the section profile, rho = |y| on the panel and
rho = sqrt(y^2 + (z - b/2)^2) on the revolved tip (cases/tools/make-wing-gmsh.py builds the tip that way).
"""
import math
import os
import re
import sys

import numpy as np
import yaml
from scipy.spatial import cKDTree

run, case_yaml = sys.argv[1], sys.argv[2]
case = yaml.safe_load(open(case_yaml))
g, m = case["geometry"], case["mesh"]
chord, half, t_te = float(g["chord"]), float(g["half_span"]), float(g["te_thickness_m"])
n_lay, h1, er = int(m["layers"]["n"]), float(m["first_cell_height_m"]), float(m["layers"]["expansion_ratio"])
stack = sum(h1 * er ** i for i in range(n_lay))
tip_cfg = case["geometry"].get("tip_variant", {}) or {}
PRISM, PYR, TET = 5, 6, 7  # cellModel::modelType (src/OpenFOAM/meshes/meshShapes/cellModel/cellModel.H)


def yt(x):  # half thickness / c, the generator's section (closed-TE NACA 0012 + linear ramp to t_TE)
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


r_te = chord * yt(1.0)
poles = np.array([[0.0, 0.0, half], [chord + r_te, 0.0, half]])


def body(path):
    """Return the text between the outer '(' and ')' of an OpenFOAM ASCII list (field internalField or labelList)."""
    t = open(path).read()
    k = t.find("internalField")
    if k >= 0:
        t = t[k:]
        if "nonuniform" not in t.split("\n", 1)[0]:
            raise SystemExit(f"{path}: uniform field")
    else:
        t = t[t.find("}") + 1:]  # past the FoamFile header
    mm = re.search(r"\n(\d+)\s*\(", t)  # "N\n(\n...\n)" or the compact one-line "N(a b c)"
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


# ---------------- gate from log.checkMesh ----------------
log = open(os.path.join(run, "log.checkMesh")).read()


def num(pattern, default=None):
    mm = re.search(pattern, log)
    return float(mm.group(1).rstrip(".")) if mm else default


non_ortho_max = num(r"Mesh non-orthogonality Max: ([\d.eE+-]+)")
n_severe = int(num(r"Number of severely non-orthogonal \(> 70 degrees\) faces: (\d+)", 0))
skew_max = num(r"Max skewness = ([\d.eE+-]+)")
weight_min = num(r"Face interpolation weight : minimum: ([\d.eE+-]+)")
det_min = num(r"Cell determinant \(wellposedness\) : minimum: ([\d.eE+-]+)")
vol_min = num(r"Min volume = ([\d.eE+-]+)")
errors = [ln.strip() for ln in log.splitlines() if ln.lstrip().startswith("***")]
errors_wo_det = [e for e in errors if "determinant" not in e]
gate = {
    "non_orthogonality_le_70": non_ortho_max is not None and non_ortho_max <= 70.0,
    "skewness_le_4_internal_20_boundary": skew_max is not None and skew_max <= 4.0,
    "volumes_positive": "Cell volumes OK" in log and vol_min is not None and vol_min > 0,
    "face_pyramids_ok": "Face pyramids OK" in log,
    "face_weight_ge_0.05": weight_min is not None and weight_min >= 0.05,
    "checkMesh_validity_set_without_determinant": not errors_wo_det,
}
gen = open(os.path.join(run, "generator.txt")).read()
cov = re.search(r"gmsh_wing_surface_elements=(\d+) layer_prisms=(\d+) expected=(\d+) layer_coverage=([\d.]+)%", gen)
arc = re.search(r"te_arc_root_segments=(\d+)", gen)
wing_faces, prisms_bl, expected = (int(cov.group(i)) for i in (1, 2, 3))
gate["layers_20_on_100pct_by_count"] = prisms_bl == expected
print(f"GATE (DR-F3-1 A) {'PASS' if all(gate.values()) else 'FAIL'}: " + " ".join(f"{k}={'ok' if v else 'FAIL'}" for k, v in gate.items()))
print(f"  max_non_orthogonality={non_ortho_max} faces_gt_70={n_severe} max_skewness={skew_max} min_face_weight={weight_min} "
      f"min_volume={vol_min} min_determinant={det_min} (reported, not gated)")
print(f"  checkMesh *** lines: {errors if errors else 'none'}")
print(f"  layers: wing_faces={wing_faces} layer_prisms={prisms_bl} expected={expected} ({cov.group(4)} %); "
      f"te_arc_root_segments={arc.group(1) if arc else 'not recorded'}; stack={stack:.4e} m")

# ---------------- location ----------------
cdir = os.path.join(run, "constant")
shape = scalars(os.path.join(cdir, "cellShapes")).astype(int)
det = scalars(os.path.join(cdir, "cellDeterminant"))
C = vectors(os.path.join(cdir, "C"))
owner = labels(os.path.join(cdir, "polyMesh", "owner"))
neigh = labels(os.path.join(cdir, "polyMesh", "neighbour"))
sets = os.path.join(cdir, "polyMesh", "sets")
bad_faces = labels(os.path.join(sets, "nonOrthoFaces"))
low_w = labels(os.path.join(sets, "lowWeightFaces"))
two_int = labels(os.path.join(sets, "twoInternalFacesCells"))
n_cells = shape.size
types, counts = np.unique(shape, return_counts=True)
print(f"cells={n_cells} by type: " + " ".join(f"{ {PRISM: 'prism', TET: 'tet', PYR: 'pyr', 3: 'hex'}.get(t, str(t)) }={c}" for t, c in zip(types, counts)))

# profile in the (x, rho) plane: upper surface and the TE half circle (upper quarter, centre (c, 0))
xs = 0.5 * (1 - np.cos(np.linspace(0, math.pi, 20001)))
prof = [np.column_stack([chord * xs, chord * np.array([yt(x) for x in xs])])]
th = np.linspace(math.pi / 2, 0, 2001)
prof.append(np.column_stack([chord + r_te * np.cos(th), r_te * np.sin(th)]))
tree = cKDTree(np.vstack(prof))


def wall_distance(P):
    rho = np.where(P[:, 2] <= half, np.abs(P[:, 1]), np.hypot(P[:, 1], P[:, 2] - half))
    d, _ = tree.query(np.column_stack([P[:, 0], rho]))
    return d


def classify(P, is_interface=None):
    d = wall_distance(P)
    dpole = np.min(np.linalg.norm(P[:, None, :] - poles[None, :, :], axis=2), axis=1)
    flags = {
        "tip_pole": dpole < 2e-3,
        "te": (P[:, 0] >= chord - 1e-3) & (d < 2.5e-3),
        "root": P[:, 2] < 2e-3,
        "interface": is_interface if is_interface is not None else np.zeros(len(P), bool),
        "tip": P[:, 2] > half,
    }
    primary = np.full(len(P), "main", dtype=object)
    for name in reversed(list(flags)):
        primary[flags[name]] = name
    return primary, flags, d


def report(title, P, extra=None, is_interface=None):
    if len(P) == 0:
        print(f"{title}: none")
        return
    primary, flags, d = classify(P, is_interface)
    names, cnt = np.unique(primary, return_counts=True)
    print(f"{title}: n={len(P)} primary " + " ".join(f"{a}={b}" for a, b in zip(names, cnt)) +
          " | flags " + " ".join(f"{k}={int(v.sum())}" for k, v in flags.items()) +
          f" | wall distance min/median/max = {d.min():.2e}/{np.median(d):.2e}/{d.max():.2e} m (stack {stack:.2e})")
    if extra is not None:
        for name in names:
            sel = primary == name
            print(f"    {name}: {extra(sel)}")
    return primary


n_int = neigh.size
if bad_faces.size:
    assert bad_faces.max() < n_int, "a > 70 deg face is a boundary face"
    o, nb = owner[bad_faces], neigh[bad_faces]
    Pf = 0.5 * (C[o] + C[nb])
    so, sn = shape[o], shape[nb]
    iface = ((so == PRISM) & (sn == TET)) | ((so == TET) & (sn == PRISM))
    pair = np.array([f"{min(a, b)}-{max(a, b)}" for a, b in zip(so, sn)])
    report("faces > 70 deg (nonOrthoFaces)", Pf, extra=lambda sel: "cell pairs (5 prism, 7 tet) " + " ".join(
        f"{a}:{b}" for a, b in zip(*np.unique(pair[sel], return_counts=True))) +
        f"; x/c {Pf[sel, 0].min() / chord:.4f}..{Pf[sel, 0].max() / chord:.4f}, z {Pf[sel, 2].min():.4f}..{Pf[sel, 2].max():.4f} m",
        is_interface=iface)
else:
    print("faces > 70 deg (nonOrthoFaces): none")
if low_w.size:
    o = owner[low_w]
    P = np.where((low_w < n_int)[:, None], 0.5 * (C[o] + C[neigh[np.minimum(low_w, n_int - 1)]]), C[o])
    report("low-weight faces (< 0.05)", P, extra=lambda sel: f"x {P[sel, 0].min():.5f}..{P[sel, 0].max():.5f} y {P[sel, 1].min():.5f}..{P[sel, 1].max():.5f} z {P[sel, 2].min():.5f}..{P[sel, 2].max():.5f}")
else:
    print("low-weight faces (< 0.05): none")
if two_int.size:
    report("two-internal-face cells", C[two_int], extra=lambda sel: "types " + " ".join(
        f"{a}:{b}" for a, b in zip(*np.unique(shape[two_int][sel], return_counts=True))))
else:
    print("two-internal-face cells: none")

# determinant split (DR-F3-1: reported by layer and core cells)
low = det < 1e-3
for t, name in ((PRISM, "layer (prism)"), (TET, "core (tet)"), (PYR, "pyramid")):
    sel = shape == t
    if sel.any():
        print(f"cellDeterminant < 0.001, {name}: {int((low & sel).sum())} of {int(sel.sum())} "
              f"({100.0 * (low & sel).sum() / sel.sum():.2f} %), min {det[sel].min():.3e}, median {np.median(det[sel]):.3e}")
tet_low = np.nonzero(low & (shape == TET))[0]
if tet_low.size:
    report("core (tet) cells with determinant < 0.001", C[tet_low])
