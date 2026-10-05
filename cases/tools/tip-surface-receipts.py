#!/usr/bin/env python3
"""S4 tip coupon: B-rep and wing-surface-mesh receipts for one variant (docs/proof/spike-03/tip-coupon/preregistration.md).

Run: PYTHONPATH=/opt/homebrew/Cellar/gmsh/4.15.2/lib uv run --with pyyaml --with numpy --with scipy \
         python3 cases/tools/tip-surface-receipts.py <run-dir> <case.yaml>
Reads <run-dir>/wing.msh (MSH 2.2, physical group "wing") and generator.txt. Prints, by region of the wing surface mesh
(tip_pole, te, root, tip, main; first match wins; tip is z >= b/2 - 1 um, so the V1 and V2 caps count):
  * degenerate-curve "Skipping boundary layer extrusion" lines and the B-rep free-curve line from generator.txt;
  * watertight: surface-mesh edges used by one triangle away from the root plane, edges used by more than two, and node
    pairs closer than the 10 um join tolerance that share no edge (a gap or a duplicate);
  * seam continuity: for every edge shared by two triangles of different B-rep surfaces, the fold angle = 180 degrees
    minus the interior angle between the two triangles (0 = tangent-continuous, 90 = a convex corner);
  * wall nodes: the largest distance of any wing node to the analytic reference surface (v0, v3: the revolve; v1: the
    planar cap at b/2; v2: the revolve, which differs from the flats by at most the flat size);
  * wall-triangle aspect ratio per region: longest edge over the shortest altitude (equilateral = 1.155).
"""
import math
import re
import sys

import gmsh
import numpy as np
import yaml
from scipy.spatial import cKDTree

run, case_yaml = sys.argv[1], sys.argv[2]
case = yaml.safe_load(open(case_yaml))
g = case["geometry"]
chord, half, t_te, variant = float(g["chord"]), float(g["half_span"]), float(g["te_thickness_m"]), g["tip_variant"]


def yt(x):
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


r_te = chord * yt(1.0)
poles = np.array([[0.0, 0.0, half], [chord + r_te, 0.0, half]])
xs = 0.5 * (1 - np.cos(np.linspace(0, math.pi, 20001)))
xs_p, rho_p = chord * xs, chord * np.array([yt(x) for x in xs])
th = np.linspace(math.pi / 2, 0, 2001)
tree = cKDTree(np.vstack([np.column_stack([xs_p, rho_p]), np.column_stack([chord + r_te * np.cos(th), r_te * np.sin(th)])]))


def inside(x, y):
    top = np.interp(x, xs_p, rho_p, left=-1.0, right=-1.0)
    return (np.abs(y) < top) | (((x - chord) ** 2 + y ** 2 < r_te ** 2) & (x > chord))


def ref_distance(P):
    if variant == "v1":
        d2, _ = tree.query(np.column_stack([P[:, 0], np.abs(P[:, 1])]))
        d2 = np.where(inside(P[:, 0], P[:, 1]), 0.0, d2)
        return np.where(P[:, 2] > half, np.hypot(d2, P[:, 2] - half), d2)
    rho = np.where(P[:, 2] <= half, np.abs(P[:, 1]), np.hypot(P[:, 1], P[:, 2] - half))
    return tree.query(np.column_stack([P[:, 0], rho]))[0]


gen = open(f"{run}/generator.txt").read()
print("degenerate-curve 'Skipping boundary layer extrusion' lines:", len(re.findall(r"Skipping boundary layer extrusion of degenerate curve", gen)))
print("B-rep:", re.search(r"brep_surfaces=.*", gen).group(0))

gmsh.initialize()
gmsh.option.setNumber("General.Terminal", 0)
gmsh.open(f"{run}/wing.msh")
wing_tag = [t for d, t in gmsh.model.getPhysicalGroups(2) if gmsh.model.getPhysicalName(2, t) == "wing"][0]
ids, coords, _ = gmsh.model.mesh.getNodes()
index = {int(t): i for i, t in enumerate(ids)}
xyz = coords.reshape(-1, 3)
tris, ent = [], []
for e in gmsh.model.getEntitiesForPhysicalGroup(2, wing_tag):
    et, _, nodes = gmsh.model.mesh.getElements(2, e)
    for typ, nd in zip(et, nodes):
        assert typ == 2, "wing surface holds only triangles"
        t = np.array([index[int(n)] for n in nd]).reshape(-1, 3)
        tris.append(t)
        ent.extend([e] * len(t))
gmsh.finalize()
tris, ent = np.vstack(tris), np.array(ent)
P = xyz[tris]  # n x 3 x 3
cen = P.mean(axis=1)
print(f"wing triangles={len(tris)} nodes_used={len(np.unique(tris))} b-rep surfaces={len(np.unique(ent))}")

# regions
d_c = ref_distance(cen)
dpole = np.min(np.linalg.norm(cen[:, None, :] - poles[None, :, :], axis=2), axis=1)
flags = {"tip_pole": dpole < 2e-3, "te": (cen[:, 0] >= chord - 1e-3) & (d_c < 2.5e-3), "root": cen[:, 2] < 2e-3, "tip": cen[:, 2] >= half - 1e-6}
region = np.full(len(tris), "main", dtype=object)
for name in reversed(list(flags)):
    region[flags[name]] = name
order = ["tip_pole", "tip", "te", "root", "main"]
print("triangles by region: " + " ".join(f"{r}={int((region == r).sum())}" for r in order))

# edges
e3 = np.stack([tris[:, [0, 1]], tris[:, [1, 2]], tris[:, [2, 0]]], axis=1).reshape(-1, 2)
e3s = np.sort(e3, axis=1)
tri_of = np.repeat(np.arange(len(tris)), 3)
key = e3s[:, 0].astype(np.int64) * len(xyz) + e3s[:, 1]
uk, inv, cnt = np.unique(key, return_inverse=True, return_counts=True)
first = np.zeros(len(uk), dtype=np.int64)
np.maximum.at(first, inv, np.arange(len(key)))  # an edge index per unique edge (the last)
mid = 0.5 * (xyz[e3s[:, 0]] + xyz[e3s[:, 1]])
on_root = (xyz[e3s[:, 0], 2] < 1e-5) & (xyz[e3s[:, 1], 2] < 1e-5)
free_mask = (cnt[inv] == 1) & ~on_root
print(f"watertight: edges used once off the root plane = {int(free_mask.sum())}; edges used more than twice = {int((cnt > 2).sum())}")
used = np.unique(tris)
pairs = cKDTree(xyz[used]).query_pairs(1e-5, output_type="ndarray")
edge_set = set(uk.tolist())
a, b = used[pairs[:, 0]], used[pairs[:, 1]]
lo, hi = np.minimum(a, b), np.maximum(a, b)
unconnected = sum(1 for x, y in zip(lo, hi) if int(x) * len(xyz) + int(y) not in edge_set)
print(f"watertight: node pairs closer than 10 um = {len(pairs)}, of which share no mesh edge = {unconnected}")

# seam fold angles (edges shared by two triangles of different surfaces)
two = np.nonzero(cnt == 2)[0]
fold_tip, fold_panel, fold_by = [], [], {}
order_idx = np.argsort(inv, kind="stable")
starts = np.concatenate([[0], np.cumsum(cnt)[:-1]])
for u in two:
    i0, i1 = order_idx[starts[u]], order_idx[starts[u] + 1]
    t0, t1 = tri_of[i0], tri_of[i1]
    if ent[t0] == ent[t1]:
        continue
    pa, pb = xyz[e3s[i0, 0]], xyz[e3s[i0, 1]]
    ax = pb - pa
    ax /= np.linalg.norm(ax)
    vs = []
    for t in (t0, t1):
        third = [p for p in P[t] if not (np.allclose(p, pa) or np.allclose(p, pb))][0]
        v = third - pa
        v = v - ax * np.dot(v, ax)
        vs.append(v / np.linalg.norm(v))
    fold = 180.0 - math.degrees(math.acos(max(-1.0, min(1.0, float(np.dot(vs[0], vs[1]))))))
    z = 0.5 * (pa[2] + pb[2])
    (fold_tip if z >= half - 1e-6 else fold_panel).append(fold)
    fold_by.setdefault((int(ent[t0]), int(ent[t1])), []).append(fold)
for name, f in (("tip seams (z >= b/2 - 1 um)", fold_tip), ("panel seams", fold_panel)):
    f = np.array(f)
    print(f"seam fold angle, {name}: edges={f.size}" + (f" max={f.max():.2f} median={np.median(f):.2f} p95={np.percentile(f, 95):.2f} deg; edges with fold > 30 deg = {int((f > 30).sum())}" if f.size else ""))
big = sorted(((max(v), len(v), k) for k, v in fold_by.items()), reverse=True)[:4]
print("largest fold by surface pair (max deg, edges, surfaces): " + "; ".join(f"{m:.1f} {n} {k}" for m, n, k in big))

# wall nodes against the reference surface
dn = ref_distance(xyz[used])
rn = np.full(len(used), "main", dtype=object)
fn = {"tip_pole": np.min(np.linalg.norm(xyz[used][:, None, :] - poles[None], axis=2), axis=1) < 2e-3,
      "root": xyz[used][:, 2] < 2e-3, "tip": xyz[used][:, 2] >= half - 1e-6}
for name in ("root", "tip_pole", "tip"):
    rn[fn[name]] = name
print(f"wall nodes vs reference: n={len(used)} max={dn.max():.3e} m p99={np.percentile(dn, 99):.3e} nodes > 10 um = {int((dn > 1e-5).sum())}")
for r in ("tip_pole", "tip", "main", "root"):
    s = rn == r
    if s.any():
        print(f"    {r}: n={int(s.sum())} max={dn[s].max():.3e} m nodes > 10 um = {int((dn[s] > 1e-5).sum())}")

# aspect ratio per region
L = np.stack([np.linalg.norm(P[:, 1] - P[:, 0], axis=1), np.linalg.norm(P[:, 2] - P[:, 1], axis=1), np.linalg.norm(P[:, 0] - P[:, 2], axis=1)], axis=1)
A = 0.5 * np.linalg.norm(np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]), axis=1)
ar = L.max(axis=1) ** 2 / (2 * np.maximum(A, 1e-30))
print("wall-triangle aspect ratio (longest edge / shortest altitude):")
for r in order:
    s = region == r
    if s.any():
        print(f"    {r}: n={int(s.sum())} median={np.median(ar[s]):.2f} p95={np.percentile(ar[s], 95):.2f} max={ar[s].max():.1f} min_edge={L[s].min():.2e} m")
