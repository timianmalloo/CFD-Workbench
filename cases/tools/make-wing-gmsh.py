#!/usr/bin/env python3
"""Round-2 SPIKE-03 probe M-2 (DR-F2-5, Gmsh admitted for the probe only): Gmsh 3-D boundary-layer extrusion mesh of
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
assert g["airfoil"] == "naca0012" and g["tip"] == "round" and d["n_subdomains"] <= 6
lay = m["layers"]
n_lay, h1, er = int(lay["n"]), float(m["first_cell_height_m"]), float(lay["expansion_ratio"])


def yt(x):
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


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
r_te = chord * yt(1.0)
l1 = occ.addLine(occ.addPoint(0, 0, 0), occ.addPoint(0, 0, half))
l2 = occ.addLine(occ.addPoint(chord + r_te, 0, 0), occ.addPoint(chord + r_te, 0, half))
occ.fragment([(1, l1), (1, l2)], occ.getEntities(2))
eps = 1e-7
occ.synchronize()
tc = gmsh.model.getEntitiesInBoundingBox(-eps, -eps, half - eps, chord + r_te + eps, chord, half + eps, dim=1)
rev = occ.revolve(tc, 0, 0, half, 1, 0, 0, math.pi / 2)
occ.revolve(rev[0::4], 0, 0, half, 1, 0, 0, math.pi / 2)
occ.fragment(occ.getEntities(2), [])
occ.synchronize()
wing_surfs = gmsh.model.getEntities(2)
for p in gmsh.model.getEntities(0):  # point sizes by chord station
    x, y, z = gmsh.model.getValue(0, p[1], [])
    gmsh.model.mesh.setSize([p], lc(min(max(x / chord, 0.0), 1.0)))

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
fmin = gmsh.model.mesh.field.add("Min")
gmsh.model.mesh.field.setNumbers(fmin, "FieldsList", [ft])
gmsh.model.mesh.field.setAsBackgroundMesh(fmin)
gmsh.option.setNumber("Mesh.MeshSizeExtendFromBoundary", 0)
gmsh.model.mesh.generate(3)
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
    path.write_text("FoamFile\n{\n    version 2.0;\n    format ascii;\n    class %s;\n    object %s;\n}\n\n" % (cls, obj) + body)
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
writeFormat binary;
writePrecision 8;
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
(run / "cfdw-manifest.json").write_text(json.dumps({r: hashlib.sha256((run / r).read_bytes()).hexdigest() for r in written}, indent=1) + "\n")
print(f"msh_sha256={hashlib.sha256((run / 'wing.msh').read_bytes()).hexdigest()} msh_bytes={os.path.getsize(run / 'wing.msh')}")
