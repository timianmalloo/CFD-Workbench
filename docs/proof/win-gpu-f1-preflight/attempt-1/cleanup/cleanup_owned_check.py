#!/usr/bin/env python3
"""Read-only residual-process check for the exact R228 attempt."""
from __future__ import annotations

import argparse
import importlib.util
import sys
from pathlib import Path


def load_owned_control(path: Path):
    spec = importlib.util.spec_from_file_location("r229_owned_control", path)
    module = importlib.util.module_from_spec(spec)
    # importlib's direct-source recipe registers before executing definitions.
    # https://docs.python.org/3/library/importlib.html#importing-a-source-file-directly
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def check(evidence_root: Path, expected_run_root: str, run_id: str,
          owned_control_path: Path, kind: str) -> None:
    try:
        module = load_owned_control(owned_control_path)
    except Exception as error:
        raise RuntimeError("F1-OWNED-IMPORT-FAILED") from error
    try:
        # Missing optional records are meaningful only within inspectable evidence.
        list(evidence_root.iterdir())
        recorded = module.read_owner_records(evidence_root, kind)
        rows = module.read_processes()
        groups = module.find_groups(rows, expected_run_root, run_id, recorded, kind)
        if kind == "stage":
            unresolved = [stage for stage in module.STAGES
                          if (evidence_root / f"{stage}.intent.json").exists()
                          and not (evidence_root / f"{stage}.owner.txt").exists()
                          and not (evidence_root / f"{stage}.entry.txt").exists()]
            if unresolved and not groups:
                raise RuntimeError("unresolved ownership acknowledgement")
    except Exception as error:
        raise RuntimeError("F1-OWNED-STATE-UNKNOWN") from error
    if groups:
        raise RuntimeError("F1-OWNED-RESIDUAL")
    print("OWNED-ABSENT")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--expected-run-root", required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--owned-control", type=Path, required=True)
    parser.add_argument("--kind", choices=("stage", "guard"), required=True)
    args = parser.parse_args()
    try:
        check(args.evidence_root, args.expected_run_root, args.run_id,
              args.owned_control, args.kind)
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        raise SystemExit(5) from None


if __name__ == "__main__":
    main()
