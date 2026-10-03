#!/usr/bin/env python3
"""Allow-list lint for an OpenFOAM case, run on the raw text right before any OpenFOAM process touches the case.

Run: python3 cases/tools/foam-dict-lint.py <case-dir>
Exit 0 = clean; exit 3 = refused (every finding printed: "lint: REFUSED <file>:<line> <why>").

Plan docs/plans/fluids-round2.md §4.2 (Ruling 65); security review cycle 1 findings 1, 2, 5, 11 and cycle 2
findings A, B, C applied.
Scope: every file listed in cfdw-manifest.json (the generator's typed-template output) and every unlisted regular
file directly in system/, constant/, 0/, 0.orig/. Integrity of everything else in the case (meshes, OpenFOAM-written
time and processor directories) is the launcher record's job (cases/tools/launcher-record.py), not this lint's.
The text is tokenised first (strings, comments, words, punctuation), so a comment marker inside a string cannot hide
a keyword and a list split across lines is read as one list. Rules, on the token stream:
  1. any '#' token outside a string (#include*, #codeStream, #calc, #eval, the verbatim-code opener '#{', ...);
  2. any string token in keyword position (a quoted/regex keyword);
  3. any word containing 'coded' (any case), any word ending in 'Libs' (functionObjectLibs, motionSolverLibs, ...),
     systemCall, and the global switch blocks InfoSwitches, OptimisationSwitches, DebugSwitches, DimensionSets;
  4. `libs` must be followed by '(' and only bare names from LIBS_ALLOWED up to ')';
  5. `runTimeModifiable` must be present in system/controlDict and false|no|off (no case file is re-read);
  6. every `type` value must be one the typed templates emit (TYPES_ALLOWED);
  7. no '$', '~' or '/' token outside a string, and no string that starts with '/' or '~' or holds '$' or '..'
     (no variable expansion, no path out of the case).
Rule 3 applies to every word token, whatever characters it carries.
The lint never expands a directive: it refuses it. Unlisted dictionaries are refused outright.
"""
import json
import os
import re
import sys

LIBS_ALLOWED = {"forces", "utilityFunctionObjects", "fieldFunctionObjects", "sampling"}
REFUSED_WORDS = {"systemCall", "InfoSwitches", "OptimisationSwitches", "DebugSwitches", "DimensionSets"}
OF_OUTPUT_FIELDS = {"cellDeterminant", "nSurfaceLayers", "thickness", "thicknessFraction", "Cx", "Cy", "Cz", "C",
                    "V", "cellLevel", "pointLevel", "yPlus", "wallShearStress", "cellAspectRatio", "nonOrthoAngle",
                    "skewness", "cellVolume", "faceWeight", "cellShapes", "minTetVolume", "minPyrVolume", "wallDist"}
MESH_DIRS = {"polyMesh", "triSurface", "extendedFeatureEdgeMesh"}
# `type` values the typed templates emit (function objects, sampled surfaces, patches and boundary conditions, snappy
# geometry, topoSet sets, thermo); anything else is refused (security review cycle 2, finding A: e.g. the ungated
# timeActivatedFileUpdate function object can write a file to any path).
TYPES_ALLOWED = {"forceCoeffs", "solverInfo", "yPlus", "surfaces", "wallShearStress", "patch", "wall", "empty",
                 "symmetryPlane", "freestreamVelocity", "freestreamPressure", "noSlip", "zeroGradient", "inletOutlet",
                 "fixedValue", "calculated", "triSurfaceMesh", "box", "cellSet", "hePsiThermo"}
rtm_false = set()
TOKEN = re.compile(r'"(?:[^"\\]|\\.)*"|/\*.*?\*/|//[^\n]*|#\{|#\}|#[A-Za-z_]*|[A-Za-z_][\w.:<>,|*+-]*|-?[\d.][\w.+-]*|\S', re.S)

case = sys.argv[1]
findings = []


def refuse(rel, line, why):
    findings.append(f"lint: REFUSED {rel}:{line} {why}")


def tokens(text):
    line = 1
    pos = 0
    for m in TOKEN.finditer(text):
        line += text.count("\n", pos, m.start())
        pos = m.start()
        t = m.group(0)
        if t.startswith("//") or t.startswith("/*"):
            continue
        yield t, line


def scan(rel, limit=None):
    raw = open(os.path.join(case, rel), "rb").read()
    if limit is not None:  # an OpenFOAM-written field: only the text before its data list
        cut = min([i for i in (raw.find(b"internalField"), raw.find(b"(")) if i >= 0] or [len(raw)])
        raw = raw[:cut]
    toks = list(tokens(raw.decode("latin-1")))
    prev = ";"
    i = 0
    while i < len(toks):
        t, n = toks[i]
        if t.startswith("#"):
            refuse(rel, n, f"directive '{t}' (no '#' directive is allowed)")
        elif t.startswith('"'):
            if prev in ("{", ";", "}"):
                refuse(rel, n, f"quoted keyword {t} (regex keywords are refused)")
            s = t.strip('"')
            if s.startswith(("/", "~")) or "$" in s or ".." in s:
                refuse(rel, n, f"string {t}: absolute, home-relative, parent or variable path")
        elif t in ("$", "~", "/"):
            refuse(rel, n, f"'{t}' outside a string (variable expansion or a path is refused)")
        elif re.match(r"[A-Za-z_]", t):
            if "coded" in t.lower():
                refuse(rel, n, f"keyword '{t}' (coded*) is refused")
            elif re.search(r"Libs(\W|$)", t) or any(w in t for w in REFUSED_WORDS):
                refuse(rel, n, f"keyword '{t}' is refused")
            elif t == "type" and prev in ("{", ";", "}"):
                v = toks[i + 1][0] if i + 1 < len(toks) else ""
                if v not in TYPES_ALLOWED:
                    refuse(rel, n, f"type '{v}' is not one the typed templates emit")
            elif t == "libs" and prev in ("{", ";", "}"):
                if i + 1 >= len(toks) or toks[i + 1][0] != "(":
                    refuse(rel, n, "libs not followed by '('")
                else:
                    j = i + 2
                    names = []
                    while j < len(toks) and toks[j][0] != ")":
                        names.append(toks[j][0])
                        j += 1
                    bad = [x for x in names if x.strip('"') not in LIBS_ALLOWED or x.startswith('"')]
                    if bad or not names or j >= len(toks):
                        refuse(rel, n, f"libs {bad or names or '()'} not all bare names in {sorted(LIBS_ALLOWED)}")
                    i = j
            elif t == "runTimeModifiable" and prev in ("{", ";", "}"):
                v = toks[i + 1][0] if i + 1 < len(toks) else ""
                if v not in ("false", "no", "off"):
                    refuse(rel, n, f"runTimeModifiable {v} (must be false)")
                elif rel == "system/controlDict":
                    rtm_false.add(rel)
        prev = toks[i][0]
        i += 1


manifest_path = os.path.join(case, "cfdw-manifest.json")
manifest = {}
if os.path.isfile(manifest_path):
    manifest = json.load(open(manifest_path))
else:
    refuse("cfdw-manifest.json", 0, "missing: the case was not written by a product template")

for rel in sorted(manifest):
    if not os.path.isfile(os.path.join(case, rel)):
        refuse(rel, 0, "listed in the manifest but missing")
        continue
    scan(rel)

for sub in ("system", "constant", "0.orig", "0"):
    d = os.path.join(case, sub)
    if not os.path.isdir(d):
        continue
    for name in sorted(os.listdir(d)):
        rel = f"{sub}/{name}"
        if os.path.isdir(os.path.join(d, name)):
            if (sub == "constant" and name in MESH_DIRS) or (sub == "0" and name in ("uniform", "polyMesh")):
                continue
            refuse(rel, 0, "unexpected directory")
            continue
        if rel in manifest:
            continue
        if sub == "0" and f"0.orig/{name}" in manifest:
            scan(rel)
            continue
        if sub in ("0", "constant") and name in OF_OUTPUT_FIELDS:
            scan(rel, limit=True)
            continue
        refuse(rel, 0, "unlisted dictionary")
        scan(rel)

if os.path.isfile(os.path.join(case, "system", "controlDict")) and "system/controlDict" not in rtm_false:
    refuse("system/controlDict", 0, "runTimeModifiable false is required (the OpenFOAM default is not relied on)")

for f in findings:
    print(f)
print(f"lint: {'REFUSED' if findings else 'clean'} files_listed={len(manifest)} findings={len(findings)}")
sys.exit(3 if findings else 0)
