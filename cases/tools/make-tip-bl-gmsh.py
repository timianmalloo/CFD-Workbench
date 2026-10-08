#!/usr/bin/env python3
"""S6 tip BL route (Ruling 97): S4's make-tip-coupon-gmsh.py (unchanged for v0-v3) plus geometry.tip_variant w4: the V1
planar flat cut with an in-mesh edge fillet of radius mesh.settings.tip_fillet_radius_m on the cap perimeter (OCC fillet
of the extruded section solid); the short cross-fillet curves are split in mesh.settings.tip_fillet_segments. W4 is a
geometry change and needs an operator ruling before adoption. S4's header follows.
S4 tip mesh coupon (docs/plans/tip-handling.md section 4.2): a copy of make-wing-gmsh.py with geometry.tip_variant
v0 (the revolved round tip, unchanged), v1 (planar flat cut at b/2), v2 (K4: short flats at each pole before the revolve)
and v3 (v0 geometry; the stack height comes from mesh.layers). Every other statement is as make-wing-gmsh.py.
Env GEOM_ONLY=1 stops after the B-rep receipts (no mesh).

Round-2 SPIKE-03 probe M-2 (DR-F2-5, Gmsh admitted for the probe only): Gmsh 3-D boundary-layer extrusion mesh of
the AR 8 analysis wing, written as MSH 2.2 for gmshToFoam, plus the OpenFOAM case dictionaries (typed templates).

Run: PYTHONPATH=/opt/homebrew/Cellar/gmsh/4.15.2/lib uv run --with pyyaml python3 cases/tools/make-wing-gmsh.py \
         cases/<name>.yaml runs/<timestamp>-<name>
Gmsh 4.15.2 (Homebrew, GPL) runs in this separate process only (COMMIT-02 process-only). Structure follows Gmsh's own
example examples/api/naca_boundary_layer_3d.py (OCC wing with a revolved round tip, then built-in-kernel
extrudeBoundaryLayer along the surface-mesh normals, symmetry plane with a hole, air box):
  * section = the round-2 analysis section (closed-TE NACA 0012 + linear ramp to t_TE), as in make-wing-r2.py, but the
    TE base is a half circle of diameter t_TE (the example's TE treatment; snappy's case has a flat base);
  * Gmsh frame: x chordwise, y thickness, z spanwise (root at z = 0 on the symmetry plane), alpha by inflow direction;
  * boundary layer: n layers, first height h1, expansion ratio r, recombined (prisms); the coverage is by construction
    and is counted from the mesh (prisms = n x wing triangles), not assumed.
Patches (physical groups): wing, symmetry (plane z = 0 incl. the layer side faces there), farfield.
simplify: the OpenFOAM dictionaries repeat make-wing-r2.py's templates with the span axis moved from y to z; ceiling =
one probe case; upgrade trigger = Gmsh admitted beyond the probe, then one shared template module.
"""
import hashlib
import json
import math
import os
import pathlib
import signal
import sys

import gmsh
import yaml

case = yaml.safe_load(pathlib.Path(sys.argv[1]).read_text())
run = pathlib.Path(sys.argv[2])
if run.exists():
    sys.exit(f"refusing to overwrite existing run directory {run}")
run.mkdir(parents=True)
g, m, s, ph, d = case["geometry"], case["mesh"], case["mesh"]["settings"], case["physics"], case["decomposition"]
chord, half, t_te = float(g["chord"]), float(g["half_span"]), float(g["te_thickness_m"])
variant = g["tip_variant"]
assert g["airfoil"] == "naca0012" and variant in ("v0", "v1", "v2", "v3", "w4") and d["n_subdomains"] <= 6
lay = m["layers"]
n_lay, h1, er = int(lay["n"]), float(m["first_cell_height_m"]), float(lay["expansion_ratio"])


def yt(x):
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


signal.alarm(int(os.environ.get("GMSH_CAP_S", "900")))  # wall cap: SIGALRM ends a runaway mesh (M1a attempt 1)
gmsh.initialize()
gmsh.option.setNumber("General.Terminal", 1)
gmsh.option.setNumber("General.NumThreads", int(s["gmsh_threads"]))
gmsh.option.setNumber("Mesh.Algorithm3D", int(s.get("algorithm_3d_id", 10)))  # 10 HXT, 1 Delaunay
gmsh.option.setNumber("Geometry.OCCBoundsUseStl", 1)
gmsh.model.add("wing")
occ = gmsh.model.occ
NP = 120
xs = [0.5 * (1 - math.cos(math.pi * i / NP)) for i in range(NP + 1)]
lc_le, lc_mid, lc_te = float(s["lc_le_m"]), float(s["lc_mid_m"]), float(s["lc_te_m"])


def lc(x):
    return lc_le + (lc_mid - lc_le) * min(1.0, x / 0.1) if x < 0.5 else lc_mid + (lc_te - lc_mid) * max(0.0, (x - 0.9) / 0.1)


r_te = chord * yt(1.0)
eps = 1e-7
TH = math.radians(30.0)  # V2: the aft arc is cut 30 degrees from the chord line (flat radius r_te sin 30 = 0.125 mm)
x_aft_flat = chord + r_te * math.cos(TH)


def tip_curves(ymin):
    return gmsh.model.getEntitiesInBoundingBox(-eps, ymin, half - eps, chord + r_te + eps, chord, half + eps, dim=1)


def ends(curve):
    return [b[1] for b in gmsh.model.getBoundary([(1, curve)], combined=False, oriented=False)]


def flat_disc(x_flat, radius):
    """A planar half disc closing the swept semicircle at a pole flat: loop = the diameter line + the two arcs."""
    cs = gmsh.model.getEntitiesInBoundingBox(x_flat - 1e-6, -radius - 1e-6, half - 1e-6, x_flat + 1e-6, radius + 1e-6,
                                             half + radius + 1e-6, dim=1)
    assert len(cs) == 3, (x_flat, cs)
    occ.addPlaneSurface([occ.addCurveLoop([c[1] for c in cs])])


if variant == "w4":
    # OCC fillet fails below 0.15 mm on this section (probe, S6 filletprobe), so the round is built as a ruled loft:
    # sections at z = 0, b/2 - r, then n inset sections (inset r (1 - cos th), z = b/2 - r + r sin th, th = k pi/2n); the
    # last section is the planar cap. Each lateral face of the round is one chord of the quarter circle.
    r_fil, n_fseg = float(s["tip_fillet_radius_m"]), int(s["tip_fillet_segments"])
    px = [chord * x for x in xs]
    py = [chord * yt(x) for x in xs]
    nrm = []  # unit outward normal of the upper surface at each profile point, LE -> TE
    for i in range(len(px)):
        a, b = max(i - 1, 0), min(i + 1, len(px) - 1)
        tx, ty = px[b] - px[a], py[b] - py[a]
        nrm.append((-ty / math.hypot(tx, ty), tx / math.hypot(tx, ty)))
    nrm[0], nrm[-1] = (-1.0, 0.0), (0.0, 1.0)

    def loft_wire(delta, z):
        up = [(px[i] - delta * nrm[i][0], py[i] - delta * nrm[i][1]) for i in range(1, len(px))]
        lo = [(x, -y) for x, y in reversed(up)]
        ps = [occ.addPoint(x, y, z, lc(x / chord)) for x, y in lo] + [occ.addPoint(delta, 0, z, lc(0))] + \
             [occ.addPoint(x, y, z, lc(x / chord)) for x, y in up]
        sp = occ.addSpline(ps)
        ce = occ.addPoint(chord, 0, z, lc(1))
        ar = occ.addCircleArc(ps[-1], ce, ps[0])
        return occ.addWire([sp, ar])

    wires = [loft_wire(0.0, 0.0), loft_wire(0.0, half - r_fil)]
    for k in range(1, n_fseg + 1):
        th = k * 0.5 * math.pi / n_fseg
        wires.append(loft_wire(r_fil * (1 - math.cos(th)), half - r_fil + r_fil * math.sin(th)))
    lofted = occ.addThruSections(wires, -1, True, True)
    occ.synchronize()
    root_s = [e for e in gmsh.model.getEntities(2) if gmsh.model.getBoundingBox(2, e[1])[5] < 1e-6]
    assert len(root_s) == 1, root_s
    occ.remove([e for e in lofted if e[0] == 3])
    occ.remove(root_s)
    occ.synchronize()
    cap_curves = [c for c in gmsh.model.getEntities(1) if gmsh.model.getBoundingBox(1, c[1])[2] > half - 1e-9]
elif variant == "v2":
    # section as pieces, no spline through the LE and no arc through the aft point: nose spline (L1, LE, U1), main
    # upper and lower splines, upper / aft / lower arcs; the nose and the aft arc end on planar caps at z = b/2 and the
    # revolve (upper main spline + upper arc, 180 degrees) ends on half discs at x = x1 and x = x_aft_flat
    lower = [occ.addPoint(chord * x, -chord * yt(x), 0, lc(x)) for x in reversed(xs[1:])]
    le = occ.addPoint(0, 0, 0, lc(0))
    upper = [occ.addPoint(chord * x, chord * yt(x), 0, lc(x)) for x in xs[1:]]
    cte = occ.addPoint(chord, 0, 0, lc(1))
    pu = occ.addPoint(x_aft_flat, r_te * math.sin(TH), 0, lc(1))
    pl = occ.addPoint(x_aft_flat, -r_te * math.sin(TH), 0, lc(1))
    pieces = [occ.addSpline([lower[-1], le, upper[0]]), occ.addSpline(upper), occ.addSpline(lower),
              occ.addCircleArc(upper[-1], cte, pu), occ.addCircleArc(pu, cte, pl), occ.addCircleArc(pl, cte, lower[0])]
    occ.extrude([(1, c) for c in pieces], 0, 0, half)
    occ.synchronize()
    x1, rho1 = chord * xs[1], chord * yt(xs[1])
    nose_top = gmsh.model.getEntitiesInBoundingBox(-eps, -rho1 - eps, half - eps, x1 + eps, rho1 + eps, half + eps, dim=1)
    aft_top = gmsh.model.getEntitiesInBoundingBox(x_aft_flat - eps, -r_te - eps, half - eps, chord + r_te + eps, r_te + eps, half + eps, dim=1)
    aft_top = [c for c in aft_top if abs(gmsh.model.getBoundingBox(1, c[1])[1] + r_te * math.sin(TH)) < 1e-6]
    assert len(nose_top) == 1 and len(aft_top) == 1, (nose_top, aft_top)
    for cv in (nose_top[0][1], aft_top[0][1]):  # planar cap at z = b/2: the curve and the chord of its ends
        a, b = ends(cv)
        occ.addPlaneSurface([occ.addCurveLoop([cv, occ.addLine(a, b)])])
    occ.synchronize()
    tc = tip_curves(-eps)  # upper main spline and upper arc (y >= 0)
    assert len(tc) == 2, tc
    rev = occ.revolve(tc, 0, 0, half, 1, 0, 0, math.pi / 2)
    occ.revolve(rev[0::4], 0, 0, half, 1, 0, 0, math.pi / 2)
    occ.synchronize()
    flat_disc(x1, rho1)
    flat_disc(x_aft_flat, r_te * math.sin(TH))
    print(f"v2 flats: LE x={x1:.4e} radius={rho1:.4e}; TE x={x_aft_flat:.6e} radius={r_te * math.sin(TH):.4e}", flush=True)
else:
    # one spline: lower TE -> LE -> upper TE (as the example), then the TE half circle closes it
    lower = [occ.addPoint(chord * x, -chord * yt(x), 0, lc(x)) for x in reversed(xs[1:])]
    le = occ.addPoint(0, 0, 0, lc(0))
    upper = [occ.addPoint(chord * x, chord * yt(x), 0, lc(x)) for x in xs[1:]]
    pts = lower + [le] + upper
    spl = occ.addSpline(pts)
    cte = occ.addPoint(chord, 0, 0, lc(1))
    cir = occ.addCircleArc(pts[-1], cte, pts[0])
    occ.extrude([(1, spl), (1, cir)], 0, 0, half)
    # split the side surfaces at the LE and at the aft end of the TE arc, so the tip curves can be revolved in halves
    l1 = occ.addLine(occ.addPoint(0, 0, 0), occ.addPoint(0, 0, half))
    l2 = occ.addLine(occ.addPoint(chord + r_te, 0, 0), occ.addPoint(chord + r_te, 0, half))
    occ.fragment([(1, l1), (1, l2)], occ.getEntities(2))
    occ.synchronize()
    if variant == "v1":  # planar flat cut at b/2: one planar cap bounded by the four tip section curves
        tc = tip_curves(-chord)
        assert len(tc) == 4, tc
        occ.addPlaneSurface([occ.addCurveLoop([c[1] for c in tc])])
    else:
        tc = tip_curves(-eps)
        rev = occ.revolve(tc, 0, 0, half, 1, 0, 0, math.pi / 2)
        occ.revolve(rev[0::4], 0, 0, half, 1, 0, 0, math.pi / 2)
if variant != "w4":  # the loft faces are already conformal; fragment collapses the 3 um faces (BOPAlgo TooSmallEdge)
    occ.fragment(occ.getEntities(2), [])
occ.synchronize()
wing_surfs = gmsh.model.getEntities(2)
if variant == "w4":
    print(f"w4 round: radius={r_fil:.3e} segments={n_fseg} wing_surfaces={len(wing_surfs)} cap_curves={len(cap_curves)}", flush=True)
# B-rep receipts: wing curves used by one wing surface only, away from the root plane (watertight), and the count
use = {}
for w in wing_surfs:
    for c in gmsh.model.getBoundary([w], combined=False, oriented=False):
        use[abs(c[1])] = use.get(abs(c[1]), 0) + 1
free = [c for c, n in use.items() if n == 1 and gmsh.model.getBoundingBox(1, c)[5] > 1e-6]
print(f"brep_surfaces={len(wing_surfs)} brep_curves={len(use)} brep_free_curves_off_root={len(free)} "
      f"brep_free_curve_lengths_m={[round(occ.getMass(1, c), 9) for c in free]} variant={variant}", flush=True)
if os.environ.get("GEOM_ONLY"):
    gmsh.finalize()
    sys.exit(0)
# the TE arc curves at the root (z = 0), kept to read their segment count from the mesh (round 3, R3-M0)
te_arc_root = [c for c in gmsh.model.getEntitiesInBoundingBox(chord - 1e-7, -r_te - 1e-7, -1e-7, chord + r_te + 1e-7,
                                                               r_te + 1e-7, 1e-7, dim=1)]
# every OCC curve aft of x = c: the TE arc curves, the spanwise TE edges and the tip's revolved TE curves (knob K1)
te_curves = gmsh.model.getEntitiesInBoundingBox(chord - 1e-7, -r_te - 1e-7, -1e-7, chord + r_te + 1e-7, r_te + 1e-7,
                                                half + r_te + 1e-7, dim=1)
for p in gmsh.model.getEntities(0):  # point sizes by chord station
    x, y, z = gmsh.model.getValue(0, p[1], [])
    gmsh.model.mesh.setSize([p], lc(min(max(x / chord, 0.0), 1.0)))
tes = s.get("te_strip")  # round 3 knob K1' (R3-M1b): structured TE arc strip, anisotropic by the fan ratio
if tes:
    strips = gmsh.model.getEntitiesInBoundingBox(chord - 1e-7, -r_te - 1e-7, -1e-7, chord + r_te + 1e-7, r_te + 1e-7,
                                                 half + 1e-7, dim=2)
    n_arc, n_span = int(tes["arc_segments_per_quarter"]), int(round(half / float(tes["span_size_m"])))
    strips = [st for st in strips if (lambda b: b[5] - b[2] > 0.5 * half)(gmsh.model.getBoundingBox(2, st[1]))]
    for st in strips:
        for c in gmsh.model.getBoundary([st], combined=False, oriented=False):
            xmin, ymin, zmin, xmax, ymax, zmax = gmsh.model.getBoundingBox(1, abs(c[1]))
            # arc cell = pi r_TE / 2 / n_arc as before; a V2 arc piece gets as many cells as fit its length
            n_c = max(1, round(occ.getMass(1, abs(c[1])) / (math.pi * r_te / 2 / n_arc)))
            gmsh.model.mesh.setTransfiniteCurve(abs(c[1]), (n_span if zmax - zmin > 0.5 * half else n_c) + 1)
        gmsh.model.mesh.setTransfiniteSurface(st[1], arrangement=tes.get("arrangement", "Left"))
    print(f"te_strip: surfaces={len(strips)} arc_segments_per_quarter={n_arc} span_segments={n_span} "
          f"(arc cell {math.pi * r_te / (2 * n_arc):.3e} m x span cell {half / n_span:.3e} m)", flush=True)

# boundary layer along the surface-mesh normals (built-in kernel, discrete entities)
heights, acc = [], 0.0
for i in range(n_lay):
    acc += h1 * er ** i
    heights.append(acc)
extbl = gmsh.model.geo.extrudeBoundaryLayer(wing_surfs, [1] * n_lay, heights, True)
top = [extbl[i - 1] for i in range(1, len(extbl)) if extbl[i][0] == 3]
bl_vols = [e for e in extbl if e[0] == 3]
gmsh.model.geo.synchronize()
bnd = gmsh.model.getBoundary(top)
cl_hole = gmsh.model.geo.addCurveLoop([c[1] for c in bnd])
x0, x1 = s["domain_m"]["x"]
y0, y1 = s["domain_m"]["thickness"]
z1 = half + float(s["domain_m"]["span_beyond_tip"])
lcf = float(s["lc_far_m"])
geo = gmsh.model.geo
P = [geo.addPoint(x0, y0, 0, lcf), geo.addPoint(x1, y0, 0, lcf), geo.addPoint(x1, y1, 0, lcf), geo.addPoint(x0, y1, 0, lcf)]
Q = [geo.addPoint(x0, y0, z1, lcf), geo.addPoint(x1, y0, z1, lcf), geo.addPoint(x1, y1, z1, lcf), geo.addPoint(x0, y1, z1, lcf)]
Lb = [geo.addLine(P[i], P[(i + 1) % 4]) for i in range(4)]
Lt = [geo.addLine(Q[i], Q[(i + 1) % 4]) for i in range(4)]
Lv = [geo.addLine(P[i], Q[i]) for i in range(4)]
s_sym = geo.addPlaneSurface([geo.addCurveLoop(Lb), cl_hole])
s_top = geo.addPlaneSurface([geo.addCurveLoop(Lt)])
s_sides = [geo.addPlaneSurface([geo.addCurveLoop([Lb[i], Lv[(i + 1) % 4], -Lt[i], -Lv[i]])]) for i in range(4)]
vol = geo.addVolume([geo.addSurfaceLoop([t[1] for t in top] + [s_sym, s_top] + s_sides)])
geo.synchronize()
# size field: fine near the wing, coarse far away
fd = gmsh.model.mesh.field.add("Distance")
gmsh.model.mesh.field.setNumbers(fd, "SurfacesList", [w[1] for w in wing_surfs])  # the OCC wing (the layer tops are discrete until meshed)
ft = gmsh.model.mesh.field.add("Threshold")
gmsh.model.mesh.field.setNumber(ft, "InField", fd)
gmsh.model.mesh.field.setNumber(ft, "SizeMin", float(s["lc_near_m"]))
gmsh.model.mesh.field.setNumber(ft, "SizeMax", lcf)
gmsh.model.mesh.field.setNumber(ft, "DistMin", float(s["near_dist_m"]))
gmsh.model.mesh.field.setNumber(ft, "DistMax", float(s["far_dist_m"]))
fields_min = [ft]
ter = s.get("te_refine")  # round 3 knob K1: size at the TE arc, graded back to lc_near over the last part of the chord
if ter:
    fte = gmsh.model.mesh.field.add("Distance")
    gmsh.model.mesh.field.setNumbers(fte, "CurvesList", [c[1] for c in te_curves])
    gmsh.model.mesh.field.setNumber(fte, "Sampling", int(ter["sampling"]))
    ftt = gmsh.model.mesh.field.add("Threshold")
    gmsh.model.mesh.field.setNumber(ftt, "InField", fte)
    gmsh.model.mesh.field.setNumber(ftt, "SizeMin", float(ter["size_m"]))
    # The ramp reaches lc_near at dist_max_m and keeps rising to lc_far, so outside the TE band the Min falls back to
    # the wing field (a SizeMax of lc_near here would floor the whole domain at lc_near: M1a attempt 1, void).
    t_min, t_size, t_near = float(ter["dist_min_m"]), float(ter["size_m"]), float(s["lc_near_m"])
    t_far_dist = t_min + (float(ter["dist_max_m"]) - t_min) * (lcf - t_size) / (t_near - t_size)
    gmsh.model.mesh.field.setNumber(ftt, "SizeMax", lcf)
    gmsh.model.mesh.field.setNumber(ftt, "DistMin", t_min)
    gmsh.model.mesh.field.setNumber(ftt, "DistMax", t_far_dist)
    fields_min.append(ftt)
    print(f"te_refine: curves={len(te_curves)} size={t_size} at <= {t_min} m, lc_near {t_near} at {ter['dist_max_m']} m "
          f"(ramp to lc_far {lcf} at {t_far_dist:.4f} m) sampling={ter['sampling']}", flush=True)
fmin = gmsh.model.mesh.field.add("Min")
gmsh.model.mesh.field.setNumbers(fmin, "FieldsList", fields_min)
gmsh.model.mesh.field.setAsBackgroundMesh(fmin)
gmsh.option.setNumber("Mesh.MeshSizeExtendFromBoundary", 0)
for k, v in (s.get("gmsh_options") or {}).items():  # round 3 knob K3 (e.g. Mesh.OptimizeNetgen, Mesh.Smoothing)
    gmsh.option.setNumber(k, float(v))
    print(f"gmsh option {k}={gmsh.option.getNumber(k):g}")
gmsh.model.mesh.generate(3)
te_arc_segments = sum(sum(len(t) for t in gmsh.model.mesh.getElements(1, c[1])[1]) for c in te_arc_root)
print(f"te_arc_root_curves={len(te_arc_root)} te_arc_root_segments={te_arc_segments} "
      f"Mesh.MinimumCirclePoints={gmsh.option.getNumber('Mesh.MinimumCirclePoints'):g}")
# physical groups (after meshing, so the discrete layer side faces have nodes to locate them)
lateral_sym = []
for e in gmsh.model.getEntities(2):
    if e[1] == s_sym or e in wing_surfs:
        continue
    _, coords, _ = gmsh.model.mesh.getNodes(2, e[1], includeBoundary=True)
    if len(coords) and max(abs(c) for c in coords[2::3]) < float(s["symmetry_tolerance_m"]):
        lateral_sym.append(e[1])
gmsh.model.addPhysicalGroup(2, [w[1] for w in wing_surfs], name="wing")
gmsh.model.addPhysicalGroup(2, [s_sym] + lateral_sym, name="symmetry")
gmsh.model.addPhysicalGroup(2, [s_top] + s_sides, name="farfield")
gmsh.model.addPhysicalGroup(3, [vol] + [v[1] for v in bl_vols], name="fluid")
gmsh.option.setNumber("Mesh.MshFileVersion", 2.2)
gmsh.option.setNumber("Mesh.Binary", 0)
gmsh.write(str(run / "wing.msh"))
wing_tris = 0
for w in wing_surfs:
    et, tg, _ = gmsh.model.mesh.getElements(2, w[1])
    wing_tris += sum(len(t) for t in tg)
prisms = 0
for v in bl_vols:
    et, tg, _ = gmsh.model.mesh.getElements(3, v[1])
    prisms += sum(len(t) for t, typ in zip(tg, et) if typ in (5, 6))
et, tg, _ = gmsh.model.mesh.getElements(3, vol)
core = sum(len(t) for t in tg)
gmsh.finalize()
print(f"gmsh_wing_surface_elements={wing_tris} layer_prisms={prisms} expected={n_lay * wing_tris} "
      f"layer_coverage={100.0 * prisms / max(1, n_lay * wing_tris):.2f}% core_cells={core} symmetry_lateral_surfaces={len(lateral_sym)}")
print(f"layer_total_thickness_m={heights[-1]:.4e} first={h1} ratio={er} n={n_lay}")

# ---- OpenFOAM dictionaries (typed templates; span axis z) ----
written = []


def write(rel, cls, obj, body):
    path = run / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("FoamFile\n{\n    version 2.0;\n    format ascii;\n    class %s;\n    object %s;\n}\n\n" % (cls, obj) + body, newline="\n")
    written.append(rel)


def vec(v):
    return "(%s)" % " ".join("%.10g" % x for x in v)


fs = ph["freestream"]
U, alpha = float(fs["U_m_s"]), math.radians(float(case["conditions"]["alpha"]))
nu = float(ph["fluid"]["nu_m2_s"])
Uvec = (U * math.cos(alpha), U * math.sin(alpha), 0.0)
nuTilda = float(fs["nuTilda_over_nu"]) * nu
chi3 = float(fs["nuTilda_over_nu"]) ** 3
nut = nuTilda * chi3 / (chi3 + 7.1 ** 3)
write("constant/transportProperties", "dictionary", "transportProperties", f"transportModel Newtonian;\nnu {nu};\n")
write("constant/turbulenceProperties", "dictionary", "turbulenceProperties",
      f"simulationType RAS;\nRAS {{ RASModel {ph['turbulence_model']}; turbulence on; printCoeffs on; }}\n")
fields = {
    "U": ("volVectorField", "[0 1 -1 0 0 0 0]", f"uniform {vec(Uvec)}",
          f"farfield {{ type freestreamVelocity; freestreamValue uniform {vec(Uvec)}; value uniform {vec(Uvec)}; }}\n    wing {{ type noSlip; }}"),
    "p": ("volScalarField", "[0 2 -2 0 0 0 0]", "uniform 0",
          "farfield { type freestreamPressure; freestreamValue uniform 0; value uniform 0; }\n    wing { type zeroGradient; }"),
    "nuTilda": ("volScalarField", "[0 2 -1 0 0 0 0]", f"uniform {nuTilda:.6g}",
                f"farfield {{ type inletOutlet; inletValue uniform {nuTilda:.6g}; value uniform {nuTilda:.6g}; }}\n    wing {{ type fixedValue; value uniform 0; }}"),
    "nut": ("volScalarField", "[0 2 -1 0 0 0 0]", f"uniform {nut:.6g}",
            f"farfield {{ type calculated; value uniform {nut:.6g}; }}\n    wing {{ type fixedValue; value uniform 0; }}"),
}
for name, (cls, dims, internal, patches) in fields.items():
    write(f"0.orig/{name}", cls, name,
          f"dimensions {dims};\ninternalField {internal};\nboundaryField\n{{\n    symmetry {{ type {s['symmetry_patch_type']}; }}\n    {patches}\n}}\n")
lift = (-math.sin(alpha), math.cos(alpha), 0.0)
drag = (math.cos(alpha), math.sin(alpha), 0.0)
iters = max(1, case["numerics"]["max_iterations"])
write("system/controlDict", "dictionary", "controlDict", f"""application simpleFoam;
startFrom startTime;
startTime 0;
stopAt endTime;
endTime {iters};
deltaT 1;
writeControl timeStep;
writeInterval {iters};
purgeWrite 0;
writeFormat {s.get('write_format', 'binary')};
writePrecision {int(s.get('write_precision', 8))};
writeCompression off;
timeFormat general;
timePrecision 6;
runTimeModifiable false;
functions
{{
    forceCoeffs1
    {{
        type forceCoeffs; libs (forces); writeControl timeStep; writeInterval 1; log no;
        patches (wing); rho rhoInf; rhoInf 1;
        liftDir {vec(lift)}; dragDir {vec(drag)}; CofR ({0.25 * chord} 0 0); pitchAxis (0 0 1);
        magUInf {U}; lRef {chord}; Aref {half * chord};
    }}
    solverInfo1 {{ type solverInfo; libs (utilityFunctionObjects); fields (U p nuTilda); writeResidualFields no; }}
    yPlus1 {{ type yPlus; libs (fieldFunctionObjects); writeControl writeTime; }}
    yPlusWing
    {{
        type surfaces; libs (sampling); writeControl writeTime; surfaceFormat vtk; fields (yPlus);
        formatOptions {{ vtk {{ format ascii; legacy true; }} }}
        surfaces {{ wing {{ type patch; patches (wing); interpolate false; }} }}
    }}
}}
""")
write("system/fvSchemes", "dictionary", "fvSchemes", """ddtSchemes { default steadyState; }
gradSchemes { default Gauss linear; grad(U) cellLimited Gauss linear 1; grad(nuTilda) cellLimited Gauss linear 1; }
divSchemes
{
    default none;
    div(phi,U) bounded Gauss linearUpwindV grad(U);
    div(phi,nuTilda) bounded Gauss limitedLinear 1;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
}
laplacianSchemes { default Gauss linear limited corrected 0.5; }
interpolationSchemes { default linear; }
snGradSchemes { default limited corrected 0.5; }
wallDist { method meshWave; correctWalls true; }
""")
write("system/fvSolution", "dictionary", "fvSolution", """solvers
{
    p { solver GAMG; smoother GaussSeidel; tolerance 1e-7; relTol 0.01; }
    U { solver smoothSolver; smoother GaussSeidel; tolerance 1e-8; relTol 0.1; nSweeps 1; }
    nuTilda { solver smoothSolver; smoother GaussSeidel; tolerance 1e-8; relTol 0.1; nSweeps 1; }
}
SIMPLE { nNonOrthogonalCorrectors 1; consistent yes; }
relaxationFactors { equations { U 0.7; nuTilda 0.7; } fields { p 0.3; } }
""")
write("system/decomposeParDict", "dictionary", "decomposeParDict", f"numberOfSubdomains {d['n_subdomains']};\nmethod {d['method']};\n")
write("system/topoSetDict.det", "dictionary", "topoSetDict", """actions
(
    { name detBelow0001; type cellSet; action new; source fieldToCell; field cellDeterminant; min -1e30; max 0.001; }
    { name detBelow03; type cellSet; action new; source fieldToCell; field cellDeterminant; min -1e30; max 0.3; }
);
""")
(run / "cfdw-manifest.json").write_text(json.dumps({r: hashlib.sha256((run / r).read_bytes()).hexdigest() for r in written}, indent=1) + "\n", newline="\n")
print(f"msh_sha256={hashlib.sha256((run / 'wing.msh').read_bytes()).hexdigest()} msh_bytes={os.path.getsize(run / 'wing.msh')}")
