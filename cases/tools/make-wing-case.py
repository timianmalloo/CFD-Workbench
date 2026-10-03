#!/usr/bin/env python3
"""Write an OpenFOAM v2512 snappyHexMesh + simpleFoam case for a SPIKE-03 half wing from one case YAML.

Run: uv run --with pyyaml python3 cases/tools/make-wing-case.py cases/<name>.yaml runs/<timestamp>-<name>
Every knob that changes the mesh or the physics is read from the YAML; nothing here is a second definition.
Prints the sha256 of the generated STL (deterministic: same YAML -> same bytes).
"""
import hashlib
import math
import pathlib
import sys

import yaml

case = yaml.safe_load(pathlib.Path(sys.argv[1]).read_text())
run = pathlib.Path(sys.argv[2])
if run.exists():
    sys.exit(f"refusing to overwrite existing run directory {run}")

g, m, s, ph, d = case["geometry"], case["mesh"], case["mesh"]["settings"], case["physics"], case["decomposition"]
chord, half = float(g["chord"]), float(g["half_span"])
assert g["airfoil"] == "naca0012" and g["twist_deg"] == [0.0, 0.0], "generator covers the untwisted NACA 0012 planform only"
assert d["n_subdomains"] <= 6


def header(cls, obj):
    return ("FoamFile\n{\n    version 2.0;\n    format ascii;\n    class %s;\n    object %s;\n}\n\n" % (cls, obj))


def write(rel, cls, obj, body):
    path = run / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(header(cls, obj) + body)


def vec(v):
    return "(%s)" % " ".join("%.10g" % x for x in v)


# ---- geometry: NACA 0012, closed trailing edge, cosine spacing; root cap 10 mm below the symmetry plane ----
N = 120
xs = [0.5 * (1 - math.cos(math.pi * i / N)) for i in range(N + 1)]


def yt(x):
    return 5 * 0.12 * (0.2969 * math.sqrt(x) - 0.1260 * x - 0.3516 * x ** 2 + 0.2843 * x ** 3 - 0.1036 * x ** 4)


loop = [(x, yt(x)) for x in xs] + [(x, -yt(x)) for x in reversed(xs[1:-1])]  # LE->TE upper, TE->LE lower
loop = [(chord * x, chord * z) for x, z in loop]
y_root = -0.01
n_span = max(2, int(math.ceil((half - y_root) / 0.02)))
stations = [y_root + (half - y_root) * k / n_span for k in range(n_span + 1)]
tris = []
L = len(loop)
for k in range(n_span):
    ya, yb = stations[k], stations[k + 1]
    for i in range(L):
        p0, p1 = loop[i], loop[(i + 1) % L]
        a, b = (p0[0], ya, p0[1]), (p1[0], ya, p1[1])
        c_, e = (p1[0], yb, p1[1]), (p0[0], yb, p0[1])
        tris += [(a, c_, b), (a, e, c_)]
centre = (0.3 * chord, 0.0)
for y, flip in ((y_root, True), (half, False)):  # each cap edge must run opposite to the side triangle sharing it
    cpt = (centre[0], y, centre[1])
    for i in range(L):
        p0, p1 = loop[i], loop[(i + 1) % L]
        a, b = (p0[0], y, p0[1]), (p1[0], y, p1[1])
        tris.append((cpt, a, b) if flip else (cpt, b, a))


def sub(u, v):
    return (u[0] - v[0], u[1] - v[1], u[2] - v[2])


def cross(u, v):
    return (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])


# outward normals <=> positive signed volume (divergence theorem)
vol = sum(a[0] * (b[1] * c_[2] - b[2] * c_[1]) - a[1] * (b[0] * c_[2] - b[2] * c_[0]) + a[2] * (b[0] * c_[1] - b[1] * c_[0])
          for a, b, c_ in tris) / 6.0
if vol < 0:
    tris = [(a, c_, b) for a, b, c_ in tris]
    vol = -vol
# NACA 0012 section area is 0.0820 c^2 (integral of the thickness polynomial); a mis-wound cap breaks this check.
expected = 0.0820 * chord ** 2 * (half - y_root)
assert abs(vol / expected - 1) < 0.01, f"enclosed volume {vol:.4e} != {expected:.4e}: surface is not consistently wound"
lines = ["solid wing"]
for a, b, c_ in tris:
    n = cross(sub(b, a), sub(c_, a))
    mag = math.sqrt(sum(x * x for x in n)) or 1.0
    lines.append("  facet normal %.9e %.9e %.9e" % tuple(x / mag for x in n))
    lines.append("    outer loop")
    for p in (a, b, c_):
        lines.append("      vertex %.9e %.9e %.9e" % p)
    lines.append("    endloop")
    lines.append("  endfacet")
lines.append("endsolid wing")
stl = ("\n".join(lines) + "\n").encode()
(run / "constant" / "triSurface").mkdir(parents=True)
(run / "constant" / "triSurface" / "wing.stl").write_bytes(stl)

# ---- background mesh ----
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
write("system/surfaceFeatureExtractDict", "dictionary", "surfaceFeatureExtractDict", """wing.stl
{
    extractionMethod extractFromSurface;
    includedAngle 150;
    subsetFeatures { nonManifoldEdges no; openEdges yes; }
    writeObj no;
}
""")
pad = float(s["near_box_pad_m"])
wx0, wx1 = s["wake_box_x_m"]
write("system/snappyHexMeshDict", "dictionary", "snappyHexMeshDict", f"""castellatedMesh true;
snap true;
addLayers true;
geometry
{{
    wing.stl {{ type triSurfaceMesh; name wing; }}
    nearBox {{ type box; min ({-pad} 0 {-pad}); max ({chord + pad} {half + pad} {pad}); }}
    wakeBox {{ type box; min ({wx0} 0 {-pad / 2}); max ({wx1} {half + pad / 2} {pad / 2}); }}
}}
castellatedMeshControls
{{
    maxLocalCells 2000000;
    maxGlobalCells 12000000;
    minRefinementCells 10;
    maxLoadUnbalance 0.10;
    nCellsBetweenLevels {s['n_cells_between_levels']};
    features ( {{ file "wing.eMesh"; level {s['feature_level']}; }} );
    refinementSurfaces {{ wing {{ level ({s['surface_level'][0]} {s['surface_level'][1]}); patchInfo {{ type wall; }} }} }}
    resolveFeatureAngle {s['resolve_feature_angle_deg']};
    refinementRegions
    {{
        nearBox {{ mode inside; levels ((1e15 {s['near_box_level']})); }}
        wakeBox {{ mode inside; levels ((1e15 {s['wake_box_level']})); }}
    }}
    locationInMesh {vec(s['location_in_mesh'])};
    allowFreeStandingZoneFaces true;
}}
snapControls
{{
    nSmoothPatch 3;
    tolerance 2.0;
    nSolveIter 50;
    nRelaxIter 5;
    nFeatureSnapIter 10;
    implicitFeatureSnap false;
    explicitFeatureSnap true;
    multiRegionFeatureSnap false;
}}
addLayersControls
{{
    relativeSizes {'true' if s['layers_relative_sizes'] else 'false'};
    layers {{ wing {{ nSurfaceLayers {m['layers']['n']}; }} }}
    expansionRatio {m['layers']['expansion_ratio']};
    {f"finalLayerThickness {s['final_layer_thickness']};" if s['layers_relative_sizes'] else f"thicknessModel firstAndExpansion; firstLayerThickness {m['first_cell_height_m']};"}
    minThickness {s['min_thickness']};
    nGrow 0;
    featureAngle {s['layer_feature_angle_deg']};
    slipFeatureAngle 30;
    nRelaxIter 5;
    nSmoothSurfaceNormals 1;
    nSmoothNormals 3;
    nSmoothThickness 10;
    maxFaceThicknessRatio 0.5;
    maxThicknessToMedialRatio {s['max_thickness_to_medial_ratio']};
    minMedialAxisAngle 90;
    nBufferCellsNoExtrude 0;
    nLayerIter {s['n_layer_iter']};
}}
meshQualityControls {{ #include "meshQualityDict" nSmoothScale 4; errorReduction 0.75; }}
writeFlags ( scalarLevels layerSets layerFields );
mergeTolerance 1e-6;
""")
write("system/meshQualityDict", "dictionary", "meshQualityDict", '#includeEtc "caseDicts/meshQualityDict"\nminFaceWeight 0.02;\n')
write("system/decomposeParDict", "dictionary", "decomposeParDict", f"numberOfSubdomains {d['n_subdomains']};\nmethod {d['method']};\n")

# ---- physics ----
fs = ph["freestream"]
U, alpha = float(fs["U_m_s"]), math.radians(float(case["conditions"]["alpha"]))
nu = float(ph["fluid"]["nu_m2_s"])
Uvec = (U * math.cos(alpha), 0.0, U * math.sin(alpha))
k = 1.5 * (U * float(fs["turbulence_intensity"])) ** 2
nut = float(fs["nut_over_nu"]) * nu
omega = k / nut
write("constant/transportProperties", "dictionary", "transportProperties", f"transportModel Newtonian;\nnu {nu};\n")
write("constant/turbulenceProperties", "dictionary", "turbulenceProperties",
      f"simulationType RAS;\nRAS {{ RASModel {ph['turbulence_model']}; turbulence on; printCoeffs on; }}\n")
fields = {
    "U": ("volVectorField", "[0 1 -1 0 0 0 0]", f"uniform {vec(Uvec)}",
          f"farfield {{ type freestreamVelocity; freestreamValue uniform {vec(Uvec)}; value uniform {vec(Uvec)}; }}\n    wing {{ type noSlip; }}"),
    "p": ("volScalarField", "[0 2 -2 0 0 0 0]", "uniform 0",
          "farfield { type freestreamPressure; freestreamValue uniform 0; value uniform 0; }\n    wing { type zeroGradient; }"),
    "k": ("volScalarField", "[0 2 -2 0 0 0 0]", f"uniform {k:.6g}",
          f"farfield {{ type inletOutlet; inletValue uniform {k:.6g}; value uniform {k:.6g}; }}\n    wing {{ type kqRWallFunction; value uniform {k:.6g}; }}"),
    "omega": ("volScalarField", "[0 0 -1 0 0 0 0]", f"uniform {omega:.6g}",
              f"farfield {{ type inletOutlet; inletValue uniform {omega:.6g}; value uniform {omega:.6g}; }}\n    wing {{ type omegaWallFunction; value uniform {omega:.6g}; }}"),
    "nut": ("volScalarField", "[0 2 -1 0 0 0 0]", f"uniform {nut:.6g}",
            f"farfield {{ type calculated; value uniform {nut:.6g}; }}\n    wing {{ type nutkWallFunction; value uniform 0; }}"),
}
for name, (cls, dims, internal, patches) in fields.items():
    write(f"0.orig/{name}", cls, name,
          f"dimensions {dims};\ninternalField {internal};\nboundaryField\n{{\n    symmetry {{ type symmetryPlane; }}\n    {patches}\n}}\n")
lift = (-math.sin(alpha), 0.0, math.cos(alpha))
drag = (math.cos(alpha), 0.0, math.sin(alpha))
iters = case["numerics"]["max_iterations"]
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
        type forceCoeffs; libs (forces); writeControl timeStep; writeInterval 1; log yes;
        patches (wing); rho rhoInf; rhoInf 1;
        liftDir {vec(lift)}; dragDir {vec(drag)}; CofR ({0.25 * chord} 0 0); pitchAxis (0 1 0);
        magUInf {U}; lRef {chord}; Aref {half * chord};
    }}
    solverInfo1 {{ type solverInfo; libs (utilityFunctionObjects); fields (U p k omega); writeResidualFields no; }}
    yPlus1 {{ type yPlus; libs (fieldFunctionObjects); writeControl writeTime; }}
    wallShearStress1 {{ type wallShearStress; libs (fieldFunctionObjects); patches (wing); writeControl writeTime; }}
}}
""")
write("system/fvSchemes", "dictionary", "fvSchemes", """ddtSchemes { default steadyState; }
gradSchemes { default Gauss linear; grad(U) cellLimited Gauss linear 1; grad(k) cellLimited Gauss linear 1; grad(omega) cellLimited Gauss linear 1; }
divSchemes
{
    default none;
    div(phi,U) bounded Gauss linearUpwindV grad(U);
    div(phi,k) bounded Gauss upwind;
    div(phi,omega) bounded Gauss upwind;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
}
laplacianSchemes { default Gauss linear limited corrected 0.5; }
interpolationSchemes { default linear; }
snGradSchemes { default limited corrected 0.5; }
wallDist { method meshWave; }
""")
write("system/fvSolution", "dictionary", "fvSolution", """solvers
{
    p { solver GAMG; smoother GaussSeidel; tolerance 1e-7; relTol 0.01; }
    "(U|k|omega)" { solver smoothSolver; smoother GaussSeidel; tolerance 1e-8; relTol 0.1; nSweeps 1; }
}
SIMPLE { nNonOrthogonalCorrectors 1; consistent yes; }
relaxationFactors { equations { U 0.7; k 0.5; omega 0.5; } fields { p 0.3; } }
""")
print(f"stl_sha256={hashlib.sha256(stl).hexdigest()} triangles={len(tris)} enclosed_volume_m3={vol:.6e}")
print(f"background={nx}x{ny}x{nz}={nx * ny * nz} cells, base={h} m, y_max={y1:.4f} m")
print(f"freestream U={vec(Uvec)} k={k:.6g} omega={omega:.6g} nut={nut:.6g}")
