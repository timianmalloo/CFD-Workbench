#!/usr/bin/env python3
"""Validate every cases/*.yaml against schemas/cfd-case.schema.json.

Run: uv run --with pyyaml --with jsonschema python3 cases/tools/validate-cases.py
Exit 0 only when every case validates and its name equals its file stem.
"""
import json
import pathlib
import sys

import jsonschema
import yaml

root = pathlib.Path(__file__).resolve().parents[2]
schema = json.loads((root / "schemas" / "cfd-case.schema.json").read_text())
jsonschema.Draft7Validator.check_schema(schema)
validator = jsonschema.Draft7Validator(schema)
failures = 0
cases = sorted((root / "cases").glob("*.yaml"))
for path in cases:
    doc = yaml.safe_load(path.read_text())
    errors = list(validator.iter_errors(doc))
    if doc.get("name") != path.stem:
        errors.append(f"name {doc.get('name')!r} != file stem {path.stem!r}")
    for error in errors:
        failures += 1
        print(f"FAIL {path.name}: {getattr(error, 'message', error)}")
    if not errors:
        print(f"ok   {path.name}")
print(f"{len(cases)} case(s), {failures} error(s)")
sys.exit(1 if failures or not cases else 0)
