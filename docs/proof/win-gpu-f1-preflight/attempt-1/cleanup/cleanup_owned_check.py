#!/usr/bin/env python3
"""Read-only residual-process check for the exact R228 attempt."""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path


def load_owned_control(path: Path):
    spec = importlib.util.spec_from_file_location("r229_owned_control", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def check(evidence_root: Path, expected_run_root: str, run_id: str,
          owned_control_path: Path, kind: str) -> None:
    module = load_owned_control(owned_control_path)
    recorded = module.read_owner_records(evidence_root, kind)
    rows = module.read_processes()
    groups = module.find_groups(rows, expected_run_root, run_id, recorded, kind)
    if kind == "stage":
        unresolved = [stage for stage in module.STAGES
                      if (evidence_root / f"{stage}.intent.json").exists()
                      and not (evidence_root / f"{stage}.owner.txt").exists()
                      and not (evidence_root / f"{stage}.entry.txt").exists()]
        if unresolved and not groups:
            raise RuntimeError(f"attempt intent has no ownership acknowledgement: {unresolved}")
    if groups:
        raise RuntimeError(f"residual attempt groups: {sorted(groups)}")
    print("OWNED-ABSENT")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--expected-run-root", required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--owned-control", type=Path, required=True)
    parser.add_argument("--kind", choices=("stage", "guard"), required=True)
    args = parser.parse_args()
    check(args.evidence_root, args.expected_run_root, args.run_id,
          args.owned_control, args.kind)


if __name__ == "__main__":
    main()
