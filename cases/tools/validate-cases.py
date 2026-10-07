#!/usr/bin/env python3
"""Validate every cases/*.yaml against schemas/cfd-case.schema.json.

Run: python3 cases/tools/validate-cases.py            (a gate: tools/check-docs.py runs it, with --self-test first)
     python3 cases/tools/validate-cases.py --self-test (planted invalid cases are red, a valid not-produced case is green)
Exit 0 only when every case validates and its name equals its file stem.

Dependencies (jsonschema, pyyaml) are not stdlib. When they are missing this re-runs itself under
`uv run --with jsonschema --with pyyaml` if uv is on PATH; otherwise it FAILS (exit 2) and names the missing package.
It never skips silently.
"""
import json
import os
import pathlib
import shutil
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[2]
REEXEC = "CFD_VALIDATE_CASES_UNDER_UV"


def need_dependencies():
    """Import the third-party modules, or re-run under uv, or exit 2 naming what is missing."""
    missing = []
    for module, package in (("jsonschema", "jsonschema"), ("yaml", "pyyaml")):
        try:
            __import__(module)
        except ImportError:
            missing.append(package)
    if not missing:
        return
    uv = shutil.which("uv")
    if uv and not os.environ.get(REEXEC):
        result = subprocess.run(
            [uv, "run", "--quiet", "--with", "jsonschema", "--with", "pyyaml", "python", str(pathlib.Path(__file__).resolve()), *sys.argv[1:]],
            env={**os.environ, REEXEC: "1"},
        )
        sys.exit(result.returncode)
    print(f"FAIL validate-cases: missing Python package(s) {', '.join(missing)} and "
          + ("uv could not supply them" if uv else "uv is not on PATH")
          + ". Install uv (https://docs.astral.sh/uv/) or `pip install " + " ".join(missing) + "` inside a venv.", file=sys.stderr)
    sys.exit(2)


def errors_for(validator, doc, stem):
    errors = [getattr(error, "message", str(error)) for error in validator.iter_errors(doc)]
    if doc.get("name") != stem:
        errors.append(f"name {doc.get('name')!r} != file stem {stem!r}")
    return errors


def load_validator():
    import jsonschema
    schema = json.loads((root / "schemas" / "cfd-case.schema.json").read_text(encoding="utf-8"))
    jsonschema.Draft7Validator.check_schema(schema)
    return jsonschema.Draft7Validator(schema)


def self_test(validator):
    """Take a real case, plant each invalid shape, and require red; the clean case and a valid not-produced case stay green."""
    import yaml
    base = yaml.safe_load((root / "cases" / "smoke-cavity.yaml").read_text(encoding="utf-8"))
    stem = base["name"]

    def variant(change):
        doc = json.loads(json.dumps(base))
        change(doc["geometry"]["source"], doc)
        return doc

    def make_not_produced(source, doc):
        source.pop("sha256")
        source.update(outcome="not-produced", reason="Gmsh 3-D meshing failed (PLC error), no msh written")

    def rename(source, doc):
        doc["name"] = "other-name"

    not_produced = variant(make_not_produced)
    expectations = (
        ("clean case", base, False),
        ("valid not-produced", not_produced, False),
        ("prose in sha256", variant(lambda s, d: s.update(sha256="none: meshing failed")), True),
        ("not-produced without reason", variant(lambda s, d: (make_not_produced(s, d), s.pop("reason"))), True),
        ("not-produced with a sha256", variant(lambda s, d: (make_not_produced(s, d), s.update(sha256="0" * 64))), True),
        ("produced without sha256", variant(lambda s, d: s.pop("sha256")), True),
        ("name differs from stem", variant(rename), True),
    )
    bad = 0
    for label, doc, should_fail in expectations:
        failed = bool(errors_for(validator, doc, stem))
        print(f"{'ok   ' if failed == should_fail else 'WRONG'} self-test {label}: {'red' if failed else 'green'}")
        bad += failed != should_fail
    print(f"validate-cases self-test: {len(expectations)} shape(s), {bad} wrong")
    return 1 if bad else 0


def main():
    need_dependencies()
    import yaml
    validator = load_validator()
    if "--self-test" in sys.argv[1:]:
        sys.exit(self_test(validator))
    failures = 0
    cases = sorted((root / "cases").glob("*.yaml"))
    for path in cases:
        doc = yaml.safe_load(path.read_text(encoding="utf-8"))
        errors = errors_for(validator, doc, path.stem)
        for error in errors:
            failures += 1
            print(f"FAIL {path.name}: {error}")
        if not errors:
            print(f"ok   {path.name}")
    print(f"{len(cases)} case(s), {failures} error(s)")
    sys.exit(1 if failures or not cases else 0)


if __name__ == "__main__":
    main()
