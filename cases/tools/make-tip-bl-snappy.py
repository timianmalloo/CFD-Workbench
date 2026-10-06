#!/usr/bin/env python3
"""S6 tip BL route W2 (Ruling 97): the S4 V1 coupon (planar flat cut at b/2, round TE base) as a snappyHexMesh case; a copy of make-wing-r2.py (round-2 half-wing case for OpenFOAM v2512 snappyHexMesh (plan docs/plans/fluids-round2.md §3, Ruling 65).

Run: uv run --with pyyaml python3 cases/tools/make-wing-r2.py cases/<name>.yaml runs/<timestamp>-<name>
Differences from the round-1 generator (make-wing-case.py, kept for round-1 reproducibility):
  * analysis geometry (DR-F2-4): NACA 0012 thickened by a linear ramp to a finite trailing edge of
    geometry.te_thickness_m (chord and S_ref unchanged) with a flat TE base, and a rounded tip cap (the section
    half-thickness swept through a half circle about the chord line);
  * wall-resolved layers (W3): absolute firstAndExpansion sizing, layer-variant controls from the YAML;
  * Spalart-Allmaras, wall-resolved (nuTilda = 0, nut = 0 at the wall) for the y+ screen;
  * typed-template output only: no '#' directive, no regex keyword; every dictionary listed in cfdw-manifest.json
    for the launcher's lint; the y+ field is sampled on the wing as legacy-ASCII VTK for an area-weighted p95.
Prints the STL sha256 and the enclosed-volume check.
"""
import hashlib
import json
import math
import pathlib
import sys

import yaml

case = yaml.safe_load(pathlib.Path(sys.argv[1]).read_text())
run = pathlib.Path(sys.argv[2])
if run.exists():
    sys.exit(f"refusing to overwrite existing run directory {run}")
g, m, s, ph, d = case["geometry"], case["mesh"], case["mesh"]["settings"], case["physics"], case["decomposition"]
chord, half, t_te = float(g["chord"]), float(g["half_span"]), float(g["te_thickness_m"])
assert g["airfoil"] == "naca0012" and g["twist_deg"] == [0.0, 0.0] and g["tip"] == "flat"
assert d["n_subdomains"] <= 6
written = []


def write(rel, cls, obj, body):
    path = run / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("FoamFile\n{\n    version 2.0;\n    format ascii;\n    class %s;\n    object %s;\n}\n\n" % (cls, obj) + body)
    written.append(rel)


def vec(v):
    return "(%s)" % " ".join("%.10g" % x for x in v)


# ---- geometry (metres); x chordwise, y spanwise, z thickness ----
N = 160
xs = [0.5 * (1 - math.cos(math.pi * i / N)) for i in range(N + 1)]


def yt(x):  # half thickness / chord: closed-TE NACA 0012 plus a linear ramp to t_te/2 at the TE (thicken, not truncate)
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4) + 0.5 * (t_te / chord) * x


up = [(chord * x, chord * yt(x)) for x in xs]           # LE -> TE, z >= 0
lo = [(chord * x, -chord * yt(x)) for x in xs]          # LE -> TE, z <= 0
r_te = chord * yt(1.0)
NARC = 24  # TE base: the half circle of radius r_te about (c, 0), as the S4 coupon (round TE base)
arc = [(chord + r_te * math.cos(math.radians(90 - 180 * k / NARC)), r_te * math.sin(math.radians(90 - 180 * k / NARC)))
       for k in range(1, NARC)]
loop = up + arc + list(reversed(lo[1:]))                # LE, upper to TE top, TE half circle, TE bottom, lower back towards the LE
y_root = -0.01
n_span = max(2, int(math.ceil((half - y_root) / 0.02)))
stations = [y_root + (half - y_root) * k / n_span for k in range(n_span + 1)]
tris = []
L = len(loop)
for k in range(n_span):
    ya, yb = stations[k], stations[k + 1]
    for i in range(L):
        p0, p1 = loop[i], loop[(i + 1) % L]
        a, b, c_, e = (p0[0], ya, p0[1]), (p1[0], ya, p1[1]), (p1[0], yb, p1[1]), (p0[0], yb, p0[1])
        tris += [(a, b, c_), (a, c_, e)]
root_c = (0.3 * chord, y_root, 0.0)
for i in range(L):
    p0, p1 = loop[i], loop[(i + 1) % L]
    tris.append((root_c, (p0[0], y_root, p0[1]), (p1[0], y_root, p1[1])))
# the tip of record (Ruling 93): a planar cap at z_span = b/2, fan-triangulated from the same interior point as the root
cap_c = (0.3 * chord, half, 0.0)
for i in range(L):
    p0, p1 = loop[i], loop[(i + 1) % L]
    tris.append((cap_c, (p0[0], half, p0[1]), (p1[0], half, p1[1])))


def key(p):
    return tuple(round(c, 12) for c in p)


tris = [t for t in tris if len({key(p) for p in t}) == 3]  # drop triangles collapsed at the LE (h = 0)


def sub(u, v):
    return (u[0] - v[0], u[1] - v[1], u[2] - v[2])


def cross(u, v):
    return (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])


# consistent winding by propagation over shared edges (manifold check included), then outward by the volume sign
edges = {}
for n, t in enumerate(tris):
    for q in range(3):
        edges.setdefault(frozenset((key(t[q]), key(t[(q + 1) % 3]))), []).append(n)
bad = [e for e, ts in edges.items() if len(ts) != 2]
assert not bad, f"surface is not closed and manifold: {len(bad)} edges without exactly two triangles"
done, stack = {0}, [0]
while stack:
    n = stack.pop()
    t = tris[n]
    for q in range(3):
        a, b = key(t[q]), key(t[(q + 1) % 3])
        for o in edges[frozenset((a, b))]:
            if o in done:
                continue
            u = tris[o]
            dirs = [(key(u[r]), key(u[(r + 1) % 3])) for r in range(3)]
            if (a, b) in dirs:
                tris[o] = (u[0], u[2], u[1])
            done.add(o)
            stack.append(o)
assert len(done) == len(tris), "surface has more than one connected component"
vol = sum(a[0] * (b[1] * c_[2] - b[2] * c_[1]) - a[1] * (b[0] * c_[2] - b[2] * c_[0]) + a[2] * (b[0] * c_[1] - b[1] * c_[0])
          for a, b, c_ in tris) / 6.0
if vol < 0:
    tris = [(a, c_, b) for a, b, c_ in tris]
    vol = -vol
# independent expected volume: section area x prismatic length + half-round tip volume, by fine quadrature
Q = 20000
xq = [(k + 0.5) / Q for k in range(Q)]
area = chord ** 2 * sum(2 * yt(x) for x in xq) / Q
area += 0.5 * math.pi * r_te ** 2  # the TE half circle
expected = area * (half - y_root)
assert abs(vol / expected - 1) < 0.01, f"enclosed volume {vol:.5e} != expected {expected:.5e}"
lines = ["solid wing"]
for a, b, c_ in tris:
    nrm = cross(sub(b, a), sub(c_, a))
    mag = math.sqrt(sum(x * x for x in nrm)) or 1.0
    lines += ["  facet normal %.9e %.9e %.9e" % tuple(x / mag for x in nrm), "    outer loop"]
    lines += ["      vertex %.9e %.9e %.9e" % p for p in (a, b, c_)]
    lines += ["    endloop", "  endfacet"]
lines.append("endsolid wing")
stl = ("\n".join(lines) + "\n").encode()
(run / "constant" / "triSurface").mkdir(parents=True)
(run / "constant" / "triSurface" / "wing.stl").write_bytes(stl)

# ---- background mesh and snappyHexMesh ----
h = float(s["base_cell_m"])
x0, x1 = s["domain_m"]["x"]
z0, z1 = s["domain_m"]["z"]
y1 = half + float(s["domain_m"]["y_beyond_tip"])
nx, ny, nz = (int(round((x1 - x0) / h)), int(round(y1 / h)), int(round((z1 - z0) / h)))
y1 = ny * h
write("system/blockMeshDict", "dictionary", "blockMeshDict", f"""scale 1;
vertices
(
    ({x0} 0 {z0}) ({x1} 0 {z0}) ({x1} {y1} {z0}) ({x0} {y1} {z0})
    ({x0} 0 {z1}) ({x1} 0 {z1}) ({x1} {y1} {z1}) ({x0} {y1} {z1})
);
blocks ( hex (0 1 2 3 4 5 6 7) ({nx} {ny} {nz}) simpleGrading (1 1 1) );
boundary
(
    farfield {{ type patch; faces ( (0 4 7 3) (1 2 6 5) (3 7 6 2) (0 3 2 1) (4 5 6 7) ); }}
    symmetry {{ type symmetryPlane; faces ( (0 1 5 4) ); }}
);
""")
write("system/surfaceFeatureExtractDict", "dictionary", "surfaceFeatureExtractDict", f"""wing.stl
{{
    extractionMethod extractFromSurface;
    includedAngle {s['feature_included_angle_deg']};
    subsetFeatures {{ nonManifoldEdges no; openEdges yes; }}
    writeObj no;
}}
""")
pad = float(s["near_box_pad_m"])
wx0, wx1 = s["wake_box_x_m"]
te_pad = float(s["te_box_pad_m"])
feat = " ".join(f"({dist} {lvl})" for dist, lvl in s["feature_levels"])
shrinker = ""
if s.get("mesh_shrinker") == "displacementMotionSolver":
    shrinker = """
    meshShrinker displacementMotionSolver;
    solver displacementLaplacian;
    displacementLaplacianCoeffs { diffusivity quadratic inverseDistance (wing); }"""
lay = m["layers"]
write("system/snappyHexMeshDict", "dictionary", "snappyHexMeshDict", f"""castellatedMesh true;
snap true;
addLayers true;
geometry
{{
    wing.stl {{ type triSurfaceMesh; name wing; }}
    nearBox {{ type box; min ({-pad} 0 {-pad}); max ({chord + pad} {half + pad} {pad}); }}
    wakeBox {{ type box; min ({wx0} 0 {-pad / 2}); max ({wx1} {half + pad / 2} {pad / 2}); }}
    teBox {{ type box; min ({chord - te_pad} 0 {-te_pad / 2}); max ({chord + te_pad} {half + te_pad / 2} {te_pad / 2}); }}
}}
castellatedMeshControls
{{
    maxLocalCells 3000000;
    maxGlobalCells 15000000;
    minRefinementCells 10;
    maxLoadUnbalance 0.10;
    nCellsBetweenLevels {s['n_cells_between_levels']};
    features ( {{ file "wing.eMesh"; levels ({feat}); }} );
    refinementSurfaces {{ wing {{ level ({s['surface_level'][0]} {s['surface_level'][1]}); patchInfo {{ type wall; }} }} }}
    resolveFeatureAngle {s['resolve_feature_angle_deg']};
    refinementRegions
    {{
        nearBox {{ mode inside; levels ((1e15 {s['near_box_level']})); }}
        wakeBox {{ mode inside; levels ((1e15 {s['wake_box_level']})); }}
        teBox {{ mode inside; levels ((1e15 {s['te_box_level']})); }}
    }}
    locationInMesh {vec(s['location_in_mesh'])};
    allowFreeStandingZoneFaces true;
}}
snapControls
{{
    nSmoothPatch 3;
    tolerance 2.0;
    nSolveIter 100;
    nRelaxIter 5;
    nFeatureSnapIter 15;
    implicitFeatureSnap false;
    explicitFeatureSnap true;
    multiRegionFeatureSnap false;
}}
addLayersControls
{{
    relativeSizes false;
    layers {{ wing {{ nSurfaceLayers {lay['n']}; }} }}
    thicknessModel firstAndExpansion;
    firstLayerThickness {m['first_cell_height_m']};
    expansionRatio {lay['expansion_ratio']};
    minThickness {s['min_thickness_m']};
    nGrow 0;
    featureAngle {s['layer_feature_angle_deg']};
    layerTerminationAngle {s['layer_termination_angle_deg']};
    slipFeatureAngle 30;
    nRelaxIter 5;
    nSmoothSurfaceNormals 1;
    nSmoothNormals 3;
    nSmoothThickness 10;
    maxFaceThicknessRatio 0.5;
    maxThicknessToMedialRatio {s['max_thickness_to_medial_ratio']};
    minMedialAxisAngle 90;
    nBufferCellsNoExtrude {s['n_buffer_cells_no_extrude']};
    nLayerIter {s['n_layer_iter']};
    nRelaxedIter {s['n_relaxed_iter']};
    nOuterIter {s['n_outer_iter']};{shrinker}
}}
meshQualityControls
{{
    maxNonOrtho 65;
    maxBoundarySkewness 20;
    maxInternalSkewness 4;
    maxConcave 80;
    minVol 1e-13;
    minTetQuality {s['min_tet_quality']};
    minArea -1;
    minTwist 0.02;
    minDeterminant 0.001;
    minFaceWeight 0.02;
    minVolRatio 0.01;
    minTriangleTwist -1;
    minEdgeLength -1;
    nSmoothScale 4;
    errorReduction 0.75;
    relaxed {{ maxNonOrtho 75; }}
}}
writeFlags ( scalarLevels layerSets layerFields );
mergeTolerance 1e-6;
""")
write("system/decomposeParDict", "dictionary", "decomposeParDict", f"numberOfSubdomains {d['n_subdomains']};\nmethod {d['method']};\n")
write("system/topoSetDict.det", "dictionary", "topoSetDict", """actions
(
    { name detBelow0001; type cellSet; action new; source fieldToCell; field cellDeterminant; min -1e30; max 0.001; }
    { name detBelow03; type cellSet; action new; source fieldToCell; field cellDeterminant; min -1e30; max 0.3; }
);
""")

# ---- physics: SA, wall-resolved, for the y+ screen ----
fs = ph["freestream"]
U, alpha = float(fs["U_m_s"]), math.radians(float(case["conditions"]["alpha"]))
nu = float(ph["fluid"]["nu_m2_s"])
Uvec = (U * math.cos(alpha), 0.0, U * math.sin(alpha))
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
          f"dimensions {dims};\ninternalField {internal};\nboundaryField\n{{\n    symmetry {{ type symmetryPlane; }}\n    {patches}\n}}\n")
lift = (-math.sin(alpha), 0.0, math.cos(alpha))
drag = (math.cos(alpha), 0.0, math.sin(alpha))
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
writeFormat ascii;
writePrecision 12;
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
        liftDir {vec(lift)}; dragDir {vec(drag)}; CofR ({0.25 * chord} 0 0); pitchAxis (0 1 0);
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
disp_solver = ("\n    cellDisplacement { solver GAMG; smoother GaussSeidel; tolerance 1e-7; relTol 0.01; }"
               if s.get("mesh_shrinker") == "displacementMotionSolver" else "")  # the layer shrinker's motion solve
write("system/fvSolution", "dictionary", "fvSolution", """solvers
{
    p { solver GAMG; smoother GaussSeidel; tolerance 1e-7; relTol 0.01; }
    U { solver smoothSolver; smoother GaussSeidel; tolerance 1e-8; relTol 0.1; nSweeps 1; }
    nuTilda { solver smoothSolver; smoother GaussSeidel; tolerance 1e-8; relTol 0.1; nSweeps 1; }""" + disp_solver + """
}
SIMPLE { nNonOrthogonalCorrectors 1; consistent yes; }
relaxationFactors { equations { U 0.7; nuTilda 0.7; } fields { p 0.3; } }
""")
(run / "cfdw-manifest.json").write_text(json.dumps({r: hashlib.sha256((run / r).read_bytes()).hexdigest() for r in written}, indent=1) + "\n")
print(f"stl_sha256={hashlib.sha256(stl).hexdigest()} triangles={len(tris)} enclosed_volume_m3={vol:.6e} expected={expected:.6e}")
print(f"te_thickness_m={t_te} analysis_tip_extension_m={chord * max(yt(x) for x in xs):.5f} S_ref_half_m2={half * chord:.6f}")
print(f"background={nx}x{ny}x{nz}={nx * ny * nz} cells, base={h} m, y_max={y1:.4f} m")
print(f"freestream U={vec(Uvec)} nuTilda={nuTilda:.6g} nut={nut:.6g}")
