#!/usr/bin/env python3
"""Validate docs/proof/cad-export-measure/cad-open-measure.json against ADR-0013 §Decision 4 and Ruling 273.

Required STEP targets (3): autodesk-fusion, freecad-cam, vectric-vcarve.
Required 3DM target (1): rhino-8.
Tolerances: units=mm unscaled, chord/span/thickness <= 10 um, TE <= 5 um, 0 repair warnings,
volume error <= 0.1 %, single watertight solid (STEP), zero PII, and red-first planted-defect detection.
"""
from __future__ import annotations

import argparse
import copy
import json
import re
import sys
from pathlib import Path
from typing import Any

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (OSError, ValueError):
            pass

ROOT = Path(__file__).resolve().parents[3]
DEFAULT_RECEIPT = ROOT / "docs" / "proof" / "cad-export-measure" / "cad-open-measure.json"

REQUIRED_STEP_TARGETS = ("autodesk-fusion", "freecad-cam", "vectric-vcarve")
REQUIRED_3DM_TARGET = "rhino-8"

MAX_LINEAR_UM = 10.0
MAX_TE_UM = 5.0
MAX_VOLUME_PCT = 0.1

WIN_USER_RX = re.compile(r"(?:[A-Za-z]:|/mnt/[a-z])(?:\\|/)+Users(?:\\|/)+", re.IGNORECASE)
DEFAULT_HOST_RX = re.compile(r"(?:DESKTOP|LAPTOP)-[A-Z0-9]{7}|WIN-[A-Z0-9]{11}")


def _is_finite_number(val: Any) -> bool:
    return isinstance(val, (int, float)) and not isinstance(val, bool)


def validate_receipt_text(raw_text: str) -> tuple[bool, bool, list[str]]:
    """Return (step_unlocked, threedm_unlocked, errors)."""
    errors: list[str] = []
    if WIN_USER_RX.search(raw_text):
        errors.append("PII check failed: Windows user-profile path found in receipt JSON.")
    if DEFAULT_HOST_RX.search(raw_text):
        errors.append("PII check failed: default Windows hostname token found in receipt JSON.")

    try:
        data = json.loads(raw_text)
    except json.JSONDecodeError as exc:
        return False, False, [f"Invalid JSON: {exc}"]

    if not isinstance(data, dict):
        return False, False, ["Receipt root must be a JSON object."]
    if data.get("schemaVersion") != 1:
        errors.append(f"Unsupported schemaVersion: {data.get('schemaVersion')!r} (expected 1).")

    targets_raw = data.get("targets")
    if not isinstance(targets_raw, list):
        errors.append("Missing 'targets' array.")
        return False, False, errors

    by_id: dict[str, dict[str, Any]] = {}
    for entry in targets_raw:
        if isinstance(entry, dict) and isinstance(entry.get("id"), str) and entry["id"]:
            by_id[entry["id"]] = entry

    def check_target(target_id: str, require_solid: bool) -> bool:
        entry = by_id.get(target_id)
        if entry is None:
            errors.append(f"Missing required target receipt: '{target_id}'.")
            return False
        ok = True
        if entry.get("redFirstPassed") is not True:
            errors.append(f"{target_id}: redFirstPassed must be true.")
            ok = False
        if not isinstance(entry.get("redFirstDefect"), str) or not entry["redFirstDefect"].strip():
            errors.append(f"{target_id}: redFirstDefect must name the planted defect.")
            ok = False
        if entry.get("greenPassed") is not True:
            errors.append(f"{target_id}: greenPassed must be true.")
            ok = False
        if entry.get("unitsMmUnscaled") is not True:
            errors.append(f"{target_id}: unitsMmUnscaled must be true.")
            ok = False
        if entry.get("zeroPiiVerified") is not True:
            errors.append(f"{target_id}: zeroPiiVerified must be true.")
            ok = False
        if require_solid and entry.get("singleWatertightSolid") is not True:
            errors.append(f"{target_id}: singleWatertightSolid must be true for STEP.")
            ok = False

        for field in ("chordErrorUm", "spanErrorUm", "thicknessErrorUm"):
            val = entry.get(field)
            if not _is_finite_number(val) or val < 0 or val > MAX_LINEAR_UM:
                errors.append(f"{target_id}: {field}={val!r} outside [0, {MAX_LINEAR_UM}] um.")
                ok = False

        te = entry.get("trailingEdgeErrorUm")
        if not _is_finite_number(te) or te < 0 or te > MAX_TE_UM:
            errors.append(f"{target_id}: trailingEdgeErrorUm={te!r} outside [0, {MAX_TE_UM}] um.")
            ok = False

        vol = entry.get("volumeErrorPercent")
        if not _is_finite_number(vol) or vol < 0 or vol > MAX_VOLUME_PCT:
            errors.append(f"{target_id}: volumeErrorPercent={vol!r} outside [0, {MAX_VOLUME_PCT}] %.")
            ok = False

        warnings = entry.get("repairWarnings")
        if not isinstance(warnings, int) or isinstance(warnings, bool) or warnings != 0:
            errors.append(f"{target_id}: repairWarnings={warnings!r} must be 0.")
            ok = False

        return ok

    pii_clean = not any(e.startswith("PII check failed") for e in errors)
    step_ok = pii_clean and all(check_target(tid, require_solid=True) for tid in REQUIRED_STEP_TARGETS)
    dm_ok = pii_clean and check_target(REQUIRED_3DM_TARGET, require_solid=False)
    return step_ok, dm_ok, errors


def _sample_green_receipt() -> dict[str, Any]:
    def mk(tid: str, name: str, fmt: str) -> dict[str, Any]:
        return {
            "id": tid,
            "name": name,
            "format": fmt,
            "redFirstPassed": True,
            "redFirstDefect": "twist-omission-16.7mm",
            "greenPassed": True,
            "unitsMmUnscaled": True,
            "chordErrorUm": 1.2,
            "spanErrorUm": 0.4,
            "thicknessErrorUm": 1.5,
            "trailingEdgeErrorUm": 0.3,
            "singleWatertightSolid": True,
            "repairWarnings": 0,
            "volumeErrorPercent": 0.02,
            "zeroPiiVerified": True,
        }

    return {
        "schemaVersion": 1,
        "fixture": "SPIKE-CAD-02",
        "targets": [
            mk("autodesk-fusion", "Autodesk Fusion", "step"),
            mk("freecad-cam", "FreeCAD CAM (Path)", "step"),
            mk("vectric-vcarve", "Vectric VCarve/Aspire", "step"),
            mk("rhino-8", "Rhino 8", "3dm"),
        ],
    }


def self_test() -> int:
    base = _sample_green_receipt()
    step_ok, dm_ok, errs = validate_receipt_text(json.dumps(base))
    assert step_ok and dm_ok and not errs, f"Green receipt failed: {errs}"

    def expect_red(label: str, mutator, expect_step: bool = False, expect_dm: bool | None = None) -> None:
        doc = copy.deepcopy(base)
        raw = mutator(doc)
        text = raw if isinstance(raw, str) else json.dumps(doc)
        s_ok, d_ok, m_errs = validate_receipt_text(text)
        assert s_ok == expect_step, f"{label}: step_ok={s_ok}, expected {expect_step}"
        if expect_dm is not None:
            assert d_ok == expect_dm, f"{label}: dm_ok={d_ok}, expected {expect_dm}"
        assert m_errs, f"{label}: expected validation errors, got none"

    expect_red("missing-red-first", lambda d: d["targets"][0].__setitem__("redFirstPassed", False), expect_step=False, expect_dm=True)
    expect_red("chord-over-10um", lambda d: d["targets"][0].__setitem__("chordErrorUm", 10.1), expect_step=False, expect_dm=True)
    expect_red("te-over-5um", lambda d: d["targets"][1].__setitem__("trailingEdgeErrorUm", 5.1), expect_step=False, expect_dm=True)
    expect_red("repair-warning-nonzero", lambda d: d["targets"][2].__setitem__("repairWarnings", 1), expect_step=False, expect_dm=True)
    expect_red("volume-over-0.1pct", lambda d: d["targets"][2].__setitem__("volumeErrorPercent", 0.15), expect_step=False, expect_dm=True)
    expect_red("missing-vectric", lambda d: d["targets"].pop(2), expect_step=False, expect_dm=True)
    expect_red("rhino-zero-pii-false", lambda d: d["targets"][3].__setitem__("zeroPiiVerified", False), expect_step=True, expect_dm=False)
    synth_path = "C:" + ("\\" * 2) + "Us" + "ers" + ("\\" * 2) + "example" + ("\\" * 2) + "leak.step"
    expect_red(
        "planted-pii-path",
        lambda d: json.dumps(d).replace("twist-omission-16.7mm", synth_path),
        expect_step=False,
        expect_dm=False,
    )

    print("verify-open-measure self-test OK: 1 green + 8 red-first planted defects verified", flush=True)
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Verify SPIKE-CAD-02 cad-open-measure.json receipt.")
    parser.add_argument("--self-test", action="store_true", help="Run green + red-first self-test.")
    parser.add_argument("--receipt", type=Path, default=DEFAULT_RECEIPT, help="Path to cad-open-measure.json.")
    args = parser.parse_args(argv)

    if args.self_test:
        return self_test()

    if not args.receipt.exists():
        print(f"GATED (COPY-481): receipt not present at {args.receipt}", file=sys.stderr)
        return 1

    raw = args.receipt.read_text(encoding="utf-8")
    step_ok, dm_ok, errors = validate_receipt_text(raw)
    if errors:
        for err in errors:
            print(f"FAIL: {err}", file=sys.stderr)
        return 1
    print(f"PASS: STEP unlocked={step_ok}, 3DM unlocked={dm_ok} ({args.receipt.name})")
    return 0 if (step_ok and dm_ok) else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
