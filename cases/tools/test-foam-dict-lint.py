#!/usr/bin/env python3
"""Negative and positive fixtures for foam-dict-lint.py (security review cycle 1, findings 1, 2, 5).

Run: python3 cases/tools/test-foam-dict-lint.py
Each fixture is a one-file case (system/controlDict) with a manifest listing it. A bypass fixture must be REFUSED
(exit 3) and name the expected rule; the clean fixture must pass (exit 0). Exit 0 only when every fixture behaves.
Protects: the lint is the only control in front of OpenFOAM's ungated `libs` load path (plan §4.1.5).
"""
import hashlib
import json
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
HEAD = "FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\nrunTimeModifiable false;\n"
CASES = {
    "string-hides-comment": ('title "//"; libs ("/tmp/x/libevil.dylib");\n', "libs"),
    "string-hides-block-comment": ('title "/*"; libs (evil); title2 "*/";\n', "libs"),
    "libs-split-lines": ("libs (forces\n    cfdwEvil);\n", "libs"),
    "libs-open-paren-alone": ("libs\n(\n    cfdwEvil\n);\n", "libs"),
    "libs-quoted-name": ('libs ("forces");\n', "libs"),
    "libs-no-paren": ("libs forces;\n", "libs"),
    "include": ('#include "x"\n', "directive"),
    "verbatim-code": ("code #{ int x; #};\n", "directive"),
    "codeStream": ("endTime #codeStream { code #{ os << 1; #}; };\n", "directive"),
    "coded-camel": ("f { type scalarCodedSource; }\n", "coded"),
    "coded-plain": ("f { type codedFixedValue; }\n", "coded"),
    "motionSolverLibs": ("motionSolverLibs (fvMotionSolvers);\n", "Libs"),
    "functionObjectLibs": ('functionObjectLibs ("libforces.dylib");\n', "Libs"),
    "systemCall": ("f { type systemCall; }\n", "systemCall"),
    "InfoSwitches": ("InfoSwitches { allowSystemOperations 1; }\n", "InfoSwitches"),
    "OptimisationSwitches": ("OptimisationSwitches { fileModificationSkew 0; }\n", "OptimisationSwitches"),
    "regex-keyword": ('"(U|p)" { relTol 0.1; }\n', "quoted keyword"),
    "runTimeModifiable-true": ("runTimeModifiable true;\n", "runTimeModifiable"),
    "timeActivatedFileUpdate": ("functions { u { type timeActivatedFileUpdate; libs (utilityFunctionObjects); fileToUpdate \"$HOME/Library/LaunchAgents/x.plist\"; } }\n", "type"),
    "unquoted-env-path": ("functions { u { type surfaces; libs (sampling); outputDir $HOME/x; } }\n", "outside a string"),
    "absolute-string": ("functions { u { type surfaces; libs (sampling); outputDir \"/tmp/x\"; } }\n", "absolute"),
    "parent-string": ("functions { u { type surfaces; libs (sampling); outputDir \"../../x\"; } }\n", "parent"),
    "coded-in-template-word": ("f { type CodedX<scalar>; }\n", "coded"),
    "unknown-bc-type": ("b { type uniformFixedValue; }\n", "type"),
}
CLEAN = ("application simpleFoam;\ntitle \"a // b /* c\";\nfunctions\n{\n"
         "    f1 { type forceCoeffs; libs (forces); patches (wing); }\n"
         "    f2 { type surfaces; libs (sampling); surfaces { w { type patch; patches (wing); } } }\n}\n"
         "div(phi,U) bounded Gauss linearUpwind grad(U);\ndiv((nuEff*dev2(T(grad(U))))) Gauss linear;\n")


def lint(body, head=HEAD):
    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, "system"))
        p = os.path.join(d, "system", "controlDict")
        open(p, "w").write(head + body)
        json.dump({"system/controlDict": hashlib.sha256(open(p, "rb").read()).hexdigest()},
                  open(os.path.join(d, "cfdw-manifest.json"), "w"))
        r = subprocess.run([sys.executable, os.path.join(HERE, "foam-dict-lint.py"), d], capture_output=True, text=True)
        return r.returncode, r.stdout


fails = 0
for name, (body, expect) in CASES.items():
    code, out = lint(body)
    ok = code == 3 and expect in out
    fails += not ok
    print(f"{'ok  ' if ok else 'FAIL'} refuse {name}: exit={code}" + ("" if ok else f"\n{out}"))
code, out = lint(CLEAN)
fails += code != 0
print(f"{'ok  ' if code == 0 else 'FAIL'} clean fixture: exit={code}" + ("" if code == 0 else f"\n{out}"))
code, out = lint("application simpleFoam;\n", head="FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\n")
ok = code == 3 and "runTimeModifiable false is required" in out
fails += not ok
print(f"{'ok  ' if ok else 'FAIL'} refuse missing-runTimeModifiable: exit={code}")
print(f"{len(CASES) + 2} fixtures, {fails} failure(s)")
sys.exit(1 if fails else 0)
