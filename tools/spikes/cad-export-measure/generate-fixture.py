#!/usr/bin/env python3
"""Generate the SPIKE-CAD-02 external open-and-measure fixture set via CfdWorkbench.Cli.

Generates the 5 benchmark wings (4 docs/design/export.md §3.3 wings + ADR-0010 twisted/blended wing)
in STEP AP203 (Whole/Half x ClosedShell/Untrimmed) and Rhino 3DM (Whole/Half), plus mandatory red-first
planted-defect files (ADR-0010 twist omission), into an out-of-git output directory alongside
expected-measurements.json for Autodesk Fusion, FreeCAD CAM (Path), Vectric VCarve/Aspire, and Rhino 8.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (OSError, ValueError):
            pass

ROOT = Path(__file__).resolve().parents[3]
CLI_PROJECT = ROOT / "src" / "CfdWorkbench.Cli" / "CfdWorkbench.Cli.csproj"

BENCHMARK_SPECS = [
    {
        "id": "example-open-026",
        "label": "Example foil (open TE 0.26 mm)",
        "kind": "example-open",
        "teHalf": 0.001,
        "expectedOpenTe": True,
        "expectedHalfSpanMm": 450.0,
        "expectedRootChordMm": 120.0,
        "expectedTrailingEdgeMm": 0.25573,
    },
    {
        "id": "example-open-036",
        "label": "Example foil (open TE 0.36 mm)",
        "kind": "example-open",
        "teHalf": 0.0014,
        "expectedOpenTe": True,
        "expectedHalfSpanMm": 450.0,
        "expectedRootChordMm": 120.0,
        "expectedTrailingEdgeMm": 0.35802,
    },
    {
        "id": "example-closed",
        "label": "Example foil as shipped (closed TE 0.00 mm)",
        "kind": "example",
        "expectedOpenTe": False,
        "expectedHalfSpanMm": 450.0,
        "expectedRootChordMm": 120.0,
        "expectedTrailingEdgeMm": 0.0,
    },
    {
        "id": "untitled-naca0012-closed",
        "label": "Untitled NACA 0012 wing (closed TE 0.00 mm)",
        "kind": "untitled",
        "expectedOpenTe": False,
        "expectedHalfSpanMm": 450.0,
        "expectedRootChordMm": 120.0,
        "expectedTrailingEdgeMm": 0.0,
    },
    {
        "id": "adr0010-blended-twisted",
        "label": "ADR-0010 twisted/blended wing (+60 mm dihedral, -8 deg twist)",
        "kind": "adr0010-twisted",
        "expectedOpenTe": False,
        "expectedHalfSpanMm": 450.0,
        "expectedRootChordMm": 120.0,
        "expectedTrailingEdgeMm": 0.0,
    },
]

TARGETS = [
    {"id": "autodesk-fusion", "name": "Autodesk Fusion", "format": "step"},
    {"id": "freecad-cam", "name": "FreeCAD CAM (Path)", "format": "step"},
    {"id": "vectric-vcarve", "name": "Vectric VCarve/Aspire", "format": "step"},
    {"id": "rhino-8", "name": "Rhino 8", "format": "3dm"},
]


def apply_open_te(text: str, te_half: float) -> str:
    th = repr(te_half)
    nth = repr(-te_half)
    text = text.replace("(0.9, 0.01), (1, 0)] ids", f"(0.9, 0.01), (1, {th})] ids")
    text = text.replace("(0.9, -0.01), (1, 0)] ids", f"(0.9, -0.01), (1, {nth})] ids")
    text = text.replace('"cv-6", "cv-7"] }\n    }', '"cv-6", "cv-7"] }\n      closure open\n    }')
    text = text.replace('"cv-6", "cv-7"] }\r\n    }', '"cv-6", "cv-7"] }\r\n      closure open\r\n    }')
    return text


def build_adr0010_twisted(blended_text: str) -> str:
    return blended_text.replace(
        "points [(0, 0), (0.1, 0), (0.3, -0.25), (0.5, -0.5), (0.7, -1), (0.9, -1.5), (1, -2)]",
        "points [(0, 0), (0.1, 0), (0.3, -1), (0.5, -2), (0.7, -4), (0.9, -6), (1, -8)]",
    )


def build_planted_twist_omission(twisted_text: str) -> str:
    return re.sub(
        r"twist cv \{[^\}]+\}",
        "twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] "
        "points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] "
        'ids ["tw-0", "tw-1", "tw-2", "tw-3", "tw-4", "tw-5", "tw-6"] }',
        twisted_text,
    )


def run_cli(cli_cmd: list[str], args: list[str]) -> str:
    proc = subprocess.run(
        [*cli_cmd, *args],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=180,
    )
    if proc.returncode != 0:
        raise RuntimeError(
            f"CLI failed (exit {proc.returncode}) for {' '.join(args)}:\n{proc.stdout}\n{proc.stderr}"
        )
    return proc.stdout


def parse_summary_metrics(output: str) -> dict[str, str]:
    metrics: dict[str, str] = {}
    for line in output.splitlines():
        if ":" in line:
            key, val = line.split(":", 1)
            key = key.strip()
            if key in {"Size of the part", "Trailing edge", "Fidelity", "File size", "STEP", "3DM", "Mesh"}:
                metrics[key] = val.strip()
    return metrics


def resolve_cli_command(no_build: bool) -> list[str]:
    if not no_build:
        build = subprocess.run(
            ["dotnet", "build", str(CLI_PROJECT), "-c", "Release", "-v", "minimal"],
            cwd=ROOT,
            capture_output=True,
            text=True,
            encoding="utf-8",
            timeout=180,
        )
        if build.returncode != 0:
            raise RuntimeError(f"dotnet build failed:\n{build.stdout}\n{build.stderr}")
    dll = ROOT / "src" / "CfdWorkbench.Cli" / "bin" / "Release" / "net10.0" / "CfdWorkbench.Cli.dll"
    if dll.exists():
        return ["dotnet", str(dll)]
    return ["dotnet", "run", "-c", "Release", "--no-build", "--project", str(CLI_PROJECT), "--"]


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Generate SPIKE-CAD-02 STEP/3DM open-and-measure fixtures out of git.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=None,
        help="Out-of-git directory for generated .step/.3dm fixtures (default: a new temp directory).",
    )
    parser.add_argument("--no-build", action="store_true", help="Skip dotnet build and reuse Release CLI binary.")
    args = parser.parse_args(argv)

    out_dir = args.out_dir.resolve() if args.out_dir else Path(tempfile.mkdtemp(prefix="cfdw-spike-cad-02-"))
    try:
        out_dir.relative_to(ROOT)
        print(
            "WARNING: --out-dir is inside the repository; do NOT commit binary .step/.3dm files (COMMIT-02).",
            file=sys.stderr,
        )
    except ValueError:
        pass

    out_dir.mkdir(parents=True, exist_ok=True)
    sources_dir = out_dir / "foil-sources"
    sources_dir.mkdir(exist_ok=True)

    example_text = (ROOT / "src" / "CfdWorkbench.Desktop" / "Assets" / "example.foil").read_text(encoding="utf-8")
    blended_text = (ROOT / "tests" / "CfdWorkbench.Core.Tests" / "Fixtures" / "m12b2" / "blended-dihedral.foil").read_text(
        encoding="utf-8"
    )
    untitled_seed = ROOT / "tests" / "CfdWorkbench.Core.Tests" / "Fixtures" / "planform-verbs" / "new-default-10.foil"

    cli_cmd = resolve_cli_command(args.no_build)
    manifest_wings: list[dict[str, object]] = []

    for spec in BENCHMARK_SPECS:
        wing_id = str(spec["id"])
        kind = str(spec["kind"])
        if kind == "example":
            input_arg = "example"
        elif kind == "example-open":
            src_path = sources_dir / f"{wing_id}.foil"
            src_path.write_text(apply_open_te(example_text, float(spec["teHalf"])), encoding="utf-8", newline="\n")
            input_arg = str(src_path)
        elif kind == "untitled":
            input_arg = str(untitled_seed)
        else:
            src_path = sources_dir / f"{wing_id}.foil"
            src_path.write_text(build_adr0010_twisted(blended_text), encoding="utf-8", newline="\n")
            input_arg = str(src_path)

        wing_dir = out_dir / wing_id
        wing_dir.mkdir(exist_ok=True)
        exports: list[dict[str, object]] = []

        for scope in ("whole", "half"):
            for variant in ("closed", "untrimmed"):
                step_out = wing_dir / f"{wing_id}-{scope}-{variant}.step"
                stdout = run_cli(
                    cli_cmd,
                    [
                        "export",
                        input_arg,
                        "--format",
                        "step",
                        "--scope",
                        scope,
                        "--step-variant",
                        variant,
                        "--out",
                        str(step_out),
                    ],
                )
                exports.append(
                    {
                        "format": "step",
                        "scope": scope,
                        "variant": variant,
                        "file": step_out.relative_to(out_dir).as_posix(),
                        "bytes": step_out.stat().st_size,
                        "metrics": parse_summary_metrics(stdout),
                    }
                )

            dm_out = wing_dir / f"{wing_id}-{scope}.3dm"
            stdout = run_cli(
                cli_cmd,
                ["export", input_arg, "--format", "3dm", "--scope", scope, "--out", str(dm_out)],
            )
            exports.append(
                {
                    "format": "3dm",
                    "scope": scope,
                    "file": dm_out.relative_to(out_dir).as_posix(),
                    "bytes": dm_out.stat().st_size,
                    "metrics": parse_summary_metrics(stdout),
                }
            )

        manifest_wings.append(
            {
                "id": wing_id,
                "label": spec["label"],
                "expectedOpenTe": spec["expectedOpenTe"],
                "expectedHalfSpanMm": spec["expectedHalfSpanMm"],
                "expectedRootChordMm": spec["expectedRootChordMm"],
                "expectedTrailingEdgeMm": spec["expectedTrailingEdgeMm"],
                "exports": exports,
            }
        )

    # Planted-defect red-first control: omit -8 deg twist on adr0010-blended-twisted (~16.74 mm discrepancy).
    defect_dir = out_dir / "planted-defect-twist-omission"
    defect_dir.mkdir(exist_ok=True)
    defect_foil = sources_dir / "adr0010-planted-twist-omission.foil"
    defect_foil.write_text(build_planted_twist_omission(build_adr0010_twisted(blended_text)), encoding="utf-8", newline="\n")
    defect_step = defect_dir / "adr0010-defect-half-closed.step"
    defect_3dm = defect_dir / "adr0010-defect-half.3dm"
    run_cli(
        cli_cmd,
        [
            "export",
            str(defect_foil),
            "--format",
            "step",
            "--scope",
            "half",
            "--step-variant",
            "closed",
            "--out",
            str(defect_step),
        ],
    )
    run_cli(
        cli_cmd,
        ["export", str(defect_foil), "--format", "3dm", "--scope", "half", "--out", str(defect_3dm)],
    )

    expected = {
        "schemaVersion": 1,
        "fixture": "SPIKE-CAD-02",
        "tolerances": {
            "units": "mm_unscaled",
            "chordErrorUmMax": 10.0,
            "spanErrorUmMax": 10.0,
            "thicknessErrorUmMax": 10.0,
            "trailingEdgeErrorUmMax": 5.0,
            "repairWarningsMax": 0,
            "volumeErrorPercentMax": 0.1,
            "zeroPiiRequired": True,
        },
        "requiredTargets": TARGETS,
        "plantedDefects": [
            {
                "id": "twist-omission-16.7mm",
                "description": "ADR-0010 blended/twisted wing with -8 deg twist omitted (0 deg); tip Z differs by ~16.74 mm.",
                "stepFile": defect_step.relative_to(out_dir).as_posix(),
                "threeDmFile": defect_3dm.relative_to(out_dir).as_posix(),
            }
        ],
        "wings": manifest_wings,
    }
    expected_path = out_dir / "expected-measurements.json"
    expected_path.write_text(json.dumps(expected, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Generated SPIKE-CAD-02 fixture set ({len(manifest_wings)} wings + planted defect) in: {out_dir}")
    print(f"Expected measurements written to: {expected_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
