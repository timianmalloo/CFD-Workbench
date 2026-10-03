#!/usr/bin/env python3
"""Write an OpenFOAM v2512 simpleFoam or rhoSimpleFoam + Spalart-Allmaras case on a NASA TMR NACA 0012 C-grid (PLOT3D 2-D).

Run: uv run --with pyyaml --with numpy python3 cases/tools/make-tmr-case.py cases/<name>.yaml <run-dir> <grid.p2dfmt.gz>
The C-grid wake cut is detected from coincident j=0 points and merged into internal faces (no stitching step).
Every physics and numerics knob is read from the YAML. Prints the grid sha256, cut indices and mesh counts.
"""
import gzip
import hashlib
import json
import math
import pathlib
import sys

import numpy as np
import yaml

case = yaml.safe_load(pathlib.Path(sys.argv[1]).read_text())
run, grid = pathlib.Path(sys.argv[2]), pathlib.Path(sys.argv[3])
if run.exists():
    sys.exit(f"refusing to overwrite existing run directory {run}")
raw = grid.read_bytes()
digest = hashlib.sha256(raw).hexdigest()
assert digest == case["geometry"]["source"]["sha256"], f"grid sha256 {digest} != case file"
tokens = gzip.decompress(raw).split()
nbl, ni, nj = int(tokens[0]), int(tokens[1]), int(tokens[2])
assert nbl == 1
vals = np.array(tokens[3:3 + 2 * ni * nj], dtype=float)
X = vals[:ni * nj].reshape(nj, ni).T  # X[i, j], Fortran order i fastest
Y = vals[ni * nj:].reshape(nj, ni).T

# ---- wake cut: j=0 points with (i) == (ni-1-i) coincide for i <= ite ----
ite = 0
while ite < ni // 2 and abs(X[ite, 0] - X[ni - 1 - ite, 0]) < 1e-12 and abs(Y[ite, 0] - Y[ni - 1 - ite, 0]) < 1e-12:
    ite += 1
ite -= 1  # last coincident index = lower trailing edge point
iteu = ni - 1 - ite
assert 0 < ite < iteu, "no wake cut found"
assert abs(X[ite, 0] - 1.0) < 1e-6 and abs(Y[ite, 0]) < 1e-6, f"TE point is ({X[ite, 0]}, {Y[ite, 0]})"

# ---- points: merge the upper wake j=0 row onto the lower one ----
pid = np.arange(ni * nj).reshape(nj, ni).T.copy()
for i in range(iteu, ni):
    pid[i, 0] = pid[ni - 1 - i, 0]
used = np.unique(pid)
remap = -np.ones(ni * nj, dtype=np.int64)
remap[used] = np.arange(used.size)
pid = remap[pid]
n2 = used.size
xy = np.stack([X.T.ravel()[used], Y.T.ravel()[used]], axis=1)
dz = float(case["mesh"]["settings"]["span_m"])
points = np.vstack([np.column_stack([xy, np.zeros(n2)]), np.column_stack([xy, np.full(n2, dz)])])

nci, ncj = ni - 1, nj - 1


def cell(i, j):
    return i + j * nci


ccx = 0.25 * (X[:-1, :-1] + X[1:, :-1] + X[:-1, 1:] + X[1:, 1:])
ccy = 0.25 * (Y[:-1, :-1] + Y[1:, :-1] + Y[:-1, 1:] + Y[1:, 1:])
centres = np.zeros((nci * ncj, 3))
for j in range(ncj):
    centres[j * nci:(j + 1) * nci, 0] = ccx[:, j]
    centres[j * nci:(j + 1) * nci, 1] = ccy[:, j]
centres[:, 2] = dz / 2

internal, patches = [], {"airfoil": [], "farfield": [], "frontAndBack": []}


def quad(a, b):  # edge a->b in plane, extruded
    return [a, b, b + n2, a + n2]


for j in range(ncj):
    for i in range(ni):
        f = quad(pid[i, j], pid[i, j + 1])
        if 1 <= i <= ni - 2:
            internal.append((cell(i - 1, j), cell(i, j), f))
        else:
            patches["farfield"].append((cell(0 if i == 0 else ni - 2, j), f))
for i in range(nci):
    for j in range(nj):
        f = quad(pid[i, j], pid[i + 1, j])
        if 1 <= j <= nj - 2:
            internal.append((cell(i, j - 1), cell(i, j), f))
        elif j == nj - 1:
            patches["farfield"].append((cell(i, ncj - 1), f))
        elif ite <= i < iteu:
            patches["airfoil"].append((cell(i, 0), f))
        elif i < ite:
            a, b = cell(i, 0), cell(ni - 2 - i, 0)
            internal.append((min(a, b), max(a, b), f))
for c in range(nci * ncj):
    i, j = c % nci, c // nci
    p0, p1, p2, p3 = pid[i, j], pid[i + 1, j], pid[i + 1, j + 1], pid[i, j + 1]
    patches["frontAndBack"].append((c, [p0, p3, p2, p1]))
    patches["frontAndBack"].append((c, [p0 + n2, p1 + n2, p2 + n2, p3 + n2]))


def oriented(face, own_c, target):
    p = points[face]
    area = np.cross(p[2] - p[0], p[3] - p[1])
    return face if np.dot(area, target - own_c) > 0 else face[::-1]


internal.sort(key=lambda t: (t[0], t[1]))
faces, owner, neighbour = [], [], []
for o, n, f in internal:
    faces.append(oriented(f, centres[o], centres[n]))
    owner.append(o)
    neighbour.append(n)
bounds = []
for name in ("airfoil", "farfield", "frontAndBack"):
    start = len(faces)
    for o, f in patches[name]:
        fc = points[f].mean(axis=0)
        faces.append(oriented(f, centres[o], fc))
        owner.append(o)
    bounds.append((name, len(faces) - start, start))

poly = run / "constant" / "polyMesh"
poly.mkdir(parents=True)


def header(cls, obj, note=""):
    n = f'    note "{note}";\n' if note else ""
    return f"FoamFile\n{{\n    version 2.0;\n    format ascii;\n    class {cls};\n{n}    location \"constant/polyMesh\";\n    object {obj};\n}}\n\n"


note = f"nPoints:{len(points)} nCells:{nci * ncj} nFaces:{len(faces)} nInternalFaces:{len(internal)}"
(poly / "points").write_text(header("vectorField", "points") + f"{len(points)}\n(\n" +
                             "".join("(%.17g %.17g %.17g)\n" % tuple(p) for p in points) + ")\n")
(poly / "faces").write_text(header("faceList", "faces") + f"{len(faces)}\n(\n" +
                            "".join("4(%d %d %d %d)\n" % tuple(f) for f in faces) + ")\n")
(poly / "owner").write_text(header("labelList", "owner", note) + f"{len(owner)}\n(\n" + "\n".join(map(str, owner)) + "\n)\n")
(poly / "neighbour").write_text(header("labelList", "neighbour", note) + f"{len(neighbour)}\n(\n" + "\n".join(map(str, neighbour)) + "\n)\n")
types = {"airfoil": "wall", "farfield": "patch", "frontAndBack": "empty"}
(poly / "boundary").write_text(header("polyBoundaryMesh", "boundary") + f"{len(bounds)}\n(\n" + "".join(
    f"    {n}\n    {{\n        type {types[n]};\n        nFaces {k};\n        startFace {s};\n    }}\n" for n, k, s in bounds) + ")\n")


# ---- case dictionaries (round 2: no regex keywords, no '#' directives; every file listed in cfdw-manifest.json) ----
written = []


def write(rel, cls, obj, body):
    path = run / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(f"FoamFile\n{{\n    version 2.0;\n    format ascii;\n    class {cls};\n    object {obj};\n}}\n\n" + body)
    written.append(rel)


def vec(v):
    return "(%s)" % " ".join("%.12g" % x for x in v)


ph, nm, d = case["physics"], case["numerics"], case["decomposition"]
app = ph["application"]
compressible = app == "rhoSimpleFoam"
alpha = math.radians(float(case["conditions"]["alpha"]))
nut_ratio = float(ph["freestream"]["nuTilda_over_nu"])
if compressible:
    # TMR 2DN00: M 0.15, Re_c 6e6, T 300 K, Sutherland's law, adiabatic wall. p_inf is chosen so rho_inf gives Re_c.
    gas = ph["fluid"]
    R = 8314.462618 / float(gas["mol_weight"])
    gamma = float(gas["Cp"]) / (float(gas["Cp"]) - R)
    T_inf = float(ph["freestream"]["T_K"])
    a_inf = math.sqrt(gamma * R * T_inf)
    U = float(case["conditions"]["mach"]) * a_inf
    mu_inf = float(gas["As"]) * T_inf ** 1.5 / (T_inf + float(gas["Ts"]))
    rho_inf = float(case["conditions"]["reynolds"]) * mu_inf / (U * float(case["conditions"]["reference_length_m"]))
    p_inf = rho_inf * R * T_inf
    nu = mu_inf / rho_inf
else:
    U = float(ph["freestream"]["U_m_s"])
    nu = float(ph["fluid"]["nu_m2_s"])
Uvec = (U * math.cos(alpha), U * math.sin(alpha), 0.0)
nuTilda = nut_ratio * nu
chi3 = nut_ratio ** 3
nut = nuTilda * chi3 / (chi3 + 7.1 ** 3)
if compressible:
    write("constant/thermophysicalProperties", "dictionary", "thermophysicalProperties", f"""thermoType
{{
    type hePsiThermo;
    mixture pureMixture;
    transport sutherland;
    thermo hConst;
    equationOfState perfectGas;
    specie specie;
    energy sensibleInternalEnergy;
}}
mixture
{{
    specie {{ molWeight {gas['mol_weight']}; }}
    thermodynamics {{ Cp {gas['Cp']}; Hf 0; }}
    transport {{ As {gas['As']}; Ts {gas['Ts']}; Pr {gas['Pr']}; }}
}}
""")
else:
    write("constant/transportProperties", "dictionary", "transportProperties", f"transportModel Newtonian;\nnu {nu:.12g};\n")
write("constant/turbulenceProperties", "dictionary", "turbulenceProperties",
      f"simulationType RAS;\nRAS {{ RASModel {ph['turbulence_model']}; turbulence on; printCoeffs on; }}\n")
if compressible:
    fields = {
        "U": ("volVectorField", "[0 1 -1 0 0 0 0]", f"uniform {vec(Uvec)}",
              f"farfield {{ type freestreamVelocity; freestreamValue uniform {vec(Uvec)}; value uniform {vec(Uvec)}; }}\n    airfoil {{ type noSlip; }}"),
        "p": ("volScalarField", "[1 -1 -2 0 0 0 0]", f"uniform {p_inf:.12g}",
              f"farfield {{ type freestreamPressure; freestreamValue uniform {p_inf:.12g}; value uniform {p_inf:.12g}; }}\n    airfoil {{ type zeroGradient; }}"),
        "T": ("volScalarField", "[0 0 0 1 0 0 0]", f"uniform {T_inf:.12g}",
              f"farfield {{ type inletOutlet; inletValue uniform {T_inf:.12g}; value uniform {T_inf:.12g}; }}\n    airfoil {{ type zeroGradient; }}"),
        "alphat": ("volScalarField", "[1 -1 -1 0 0 0 0]", "uniform 0",
                   "farfield { type calculated; value uniform 0; }\n    airfoil { type calculated; value uniform 0; }"),
    }
else:
    fields = {
        "U": ("volVectorField", "[0 1 -1 0 0 0 0]", f"uniform {vec(Uvec)}",
              f"farfield {{ type freestreamVelocity; freestreamValue uniform {vec(Uvec)}; value uniform {vec(Uvec)}; }}\n    airfoil {{ type noSlip; }}"),
        "p": ("volScalarField", "[0 2 -2 0 0 0 0]", "uniform 0",
              "farfield { type freestreamPressure; freestreamValue uniform 0; value uniform 0; }\n    airfoil { type zeroGradient; }"),
    }
fields["nuTilda"] = ("volScalarField", "[0 2 -1 0 0 0 0]", f"uniform {nuTilda:.12g}",
                     f"farfield {{ type inletOutlet; inletValue uniform {nuTilda:.12g}; value uniform {nuTilda:.12g}; }}\n    airfoil {{ type fixedValue; value uniform 0; }}")
fields["nut"] = ("volScalarField", "[0 2 -1 0 0 0 0]", f"uniform {nut:.12g}",
                 f"farfield {{ type calculated; value uniform {nut:.12g}; }}\n    airfoil {{ type fixedValue; value uniform 0; }}")
for name, (cls, dims, internal_value, pbc) in fields.items():
    write(f"0/{name}", cls, name, f"dimensions {dims};\ninternalField {internal_value};\nboundaryField\n{{\n    frontAndBack {{ type empty; }}\n    {pbc}\n}}\n")
lift = (-math.sin(alpha), math.cos(alpha), 0.0)
drag = (math.cos(alpha), math.sin(alpha), 0.0)
rho_entry = f"rho rho; rhoInf {rho_inf:.12g}; pRef {p_inf:.12g};" if compressible else "rho rhoInf; rhoInf 1;"
eqs = "U p e nuTilda" if compressible else "U p nuTilda"
# A4 stop: the harvester (cases/tools/a4-monitor.py) sends SIGUSR1 (30); the bundled global controlDict sets
# OptimisationSwitches stopAtWriteNowSignal 30, so the run writes and stops. runTimeModifiable stays false, so no
# case file is re-read after the lint.
write("system/controlDict", "dictionary", "controlDict", f"""application {app};
startFrom startTime;
startTime 0;
stopAt endTime;
endTime {nm['max_iterations']};
deltaT 1;
writeControl timeStep;
writeInterval {nm['max_iterations']};
purgeWrite 0;
writeFormat binary;
writePrecision 12;
writeCompression off;
timeFormat general;
timePrecision 8;
runTimeModifiable false;
functions
{{
    forceCoeffs1
    {{
        type forceCoeffs; libs (forces); writeControl timeStep; writeInterval 1; log no;
        patches (airfoil); {rho_entry}
        liftDir {vec(lift)}; dragDir {vec(drag)}; CofR (0.25 0 0); pitchAxis (0 0 -1);
        magUInf {U:.12g}; lRef 1; Aref {dz};
    }}
    solverInfo1 {{ type solverInfo; libs (utilityFunctionObjects); fields ({eqs}); writeResidualFields {'yes' if nm.get('write_residual_fields') else 'no'}; }}
    yPlus1 {{ type yPlus; libs (fieldFunctionObjects); writeControl writeTime; }}
    wallP
    {{
        type surfaces; libs (sampling); writeControl writeTime; surfaceFormat raw; fields (p);
        surfaces {{ airfoil {{ type patch; patches (airfoil); interpolate false; }} }}
    }}
}}
""")
sc = nm["schemes"]
if compressible:
    div = f"""    div(phi,U) {sc['div_U']};
    div(phi,e) {sc['div_e']};
    div(phi,K) {sc['div_e']};
    div(phi,Ekp) {sc['div_e']};
    div(phi,nuTilda) {sc['div_nuTilda']};
    div(((rho*nuEff)*dev2(T(grad(U))))) Gauss linear;"""
else:
    div = f"""    div(phi,U) {sc['div_U']};
    div(phi,nuTilda) {sc['div_nuTilda']};
    div((nuEff*dev2(T(grad(U))))) Gauss linear;"""
write("system/fvSchemes", "dictionary", "fvSchemes", f"""ddtSchemes {{ default steadyState; }}
gradSchemes {{ default {sc['grad']}; }}
divSchemes
{{
    default none;
{div}
}}
laplacianSchemes {{ default {sc['laplacian']}; }}
interpolationSchemes {{ default linear; }}
snGradSchemes {{ default {sc['snGrad']}; }}
wallDist {{ method meshWave; correctWalls true; }}
""")
rel = nm["relaxation"]
smooth = "solver smoothSolver; smoother symGaussSeidel; tolerance 1e-14; relTol 0.1; nSweeps 1;"
eq_solvers = "\n".join(f"    {f} {{ {smooth} }}" for f in (("U", "e", "nuTilda") if compressible else ("U", "nuTilda")))
extra_simple = f"\n    pMinFactor {nm['pMinFactor']};\n    pMaxFactor {nm['pMaxFactor']};" if compressible else ""
eq_relax = " ".join(f"{f} {rel[f]};" for f in (("U", "e", "nuTilda") if compressible else ("U", "nuTilda")))
field_relax = f"p {rel['p']}; rho {rel['rho']};" if compressible else f"p {rel['p']};"
write("system/fvSolution", "dictionary", "fvSolution", f"""solvers
{{
    p {{ solver GAMG; smoother GaussSeidel; tolerance 1e-14; relTol 0.05; }}
{eq_solvers}
}}
SIMPLE
{{
    nNonOrthogonalCorrectors 0;
    consistent {nm['consistent']};{extra_simple}
}}
relaxationFactors {{ equations {{ {eq_relax} }} fields {{ {field_relax} }} }}
""")
write("system/decomposeParDict", "dictionary", "decomposeParDict", f"numberOfSubdomains {d['n_subdomains']};\nmethod {d['method']};\n")
(run / "cfdw-manifest.json").write_text(json.dumps({r: hashlib.sha256((run / r).read_bytes()).hexdigest() for r in written + ["constant/polyMesh/boundary"]}, indent=1) + "\n")
print(f"grid={grid.name} sha256={digest} dims={ni}x{nj} te_lower_i={ite} te_upper_i={iteu} airfoil_points={iteu - ite + 1}")
print(f"points={len(points)} cells={nci * ncj} faces={len(faces)} internal={len(internal)} airfoil_faces={bounds[0][1]} farfield_faces={bounds[1][1]}")
print(f"first_wall_spacing_min={np.min(np.hypot(X[ite:iteu + 1, 1] - X[ite:iteu + 1, 0], Y[ite:iteu + 1, 1] - Y[ite:iteu + 1, 0])):.3e}")
print(f"freestream U={vec(Uvec)} nu={nu:.6g} nuTilda={nuTilda:.6g} nut={nut:.6g}" +
      (f" T={T_inf} p={p_inf:.6g} rho={rho_inf:.6g} mu={mu_inf:.6g} a={a_inf:.6g} gamma={gamma:.6g}" if compressible else ""))
