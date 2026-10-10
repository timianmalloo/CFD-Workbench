#!/usr/bin/env python3
"""Open the wing STLs and 3MFs the app writes in two slicers and measure them (Export design build condition B1).

ON-DEMAND RELEASE CHECK. Not part of the fast ring, not run by run-tests.sh or the join: the slicers are not on every
machine. A missing slicer prints `NOT ASSESSED <slicer>: ...` and does not fail. Cost: about 30 s with both slicers
installed (one `dotnet run` for the fixtures, then 2 slicers x 10 files, each a `--info` call; the largest file, 513,596
triangles, loads in about a second as STL and a few seconds as 3MF). Ring: release (run before a build that changes a
writer, and at readiness on a machine that has the slicers).

  python3 tools/check-slicer-open.py [--out docs/proof/tmf] [--keep DIR]

What it does:
  1. `dotnet run ... --write-stl-fixtures <dir>`: the app's own writers write the Example foil with an open and a closed
     trailing edge, whole and starboard half, at Print, plus the Untitled wing at Fine, each as .stl and as .3mf, and
     `expected.json` with the size and signed volume the app reports for each file.
  2. For each installed slicer (PrusaSlicer, OrcaSlicer), `<slicer> --info <file>` reports size, facets, manifold status
     and volume, and prints repair counters only when it had to repair something. A file passes when the slicer reports
     manifold = yes, one part, no repair counter, the app's size within BBOX_TOLERANCE_MM on every axis (the slicer prints
     six decimals of a binary32 value, whose step at 450 mm is 0.00003 mm), and a volume within VOLUME_TOLERANCE of the
     app's signed volume.
     A 3MF also has to be read at the right scale: its size must equal the app's millimetres (a part that imports 1000x too
     large or too small fails, and the reason says "SCALE").
  3. The negative controls, per format: the closed whole wing with one triangle removed must NOT come back clean from a
     slicer; and the same 3MF with its unit attribute changed to `meter` must come back about 1000x larger, which proves the
     slicer honours the unit attribute, so the millimetre files above were read as millimetres by design and not by
     default. If a control does not behave, the check cannot fail and the run fails.
  4. Writes `slicer-open.json` (slicer, version, file, triangles, load seconds, warnings, bounding box, volume, verdict) to
     --out, and prints one line per slicer and file.

Exit 0: every installed slicer passed, or none is installed (NOT ASSESSED). Exit 1: a file failed, the negative control was
clean, or the fixtures could not be written. The tolerances are fixed here and are not options: a failure is reported, not tuned.
`assume:` the slicers' `--info` output is the one PrusaSlicer 2.9.4 and OrcaSlicer 2.3.2 print on macOS (confirmed there); the
Windows paths below are the installers' defaults and are unverified. Breaks if false: a slicer is reported NOT ASSESSED or a
broken file is read as clean (the negative control catches the second).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import time
import zipfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
BBOX_TOLERANCE_MM = 0.0001
VOLUME_TOLERANCE = 0.001
INFO_TIMEOUT_SECONDS = 180
# Keys a clean `--info` prints; any other key (open_edges, facets_removed, ...) means the slicer repaired or flagged the mesh.
CLEAN_KEYS = {"size_x", "size_y", "size_z", "min_x", "min_y", "min_z", "max_x", "max_y", "max_z",
              "number_of_facets", "manifold", "number_of_parts", "volume"}
SLICERS = {
    "PrusaSlicer": ("CFDW_PRUSASLICER", ["/Applications/PrusaSlicer.app/Contents/MacOS/PrusaSlicer",
                                         r"C:\Program Files\Prusa3D\PrusaSlicer\prusa-slicer-console.exe"]),
    "OrcaSlicer": ("CFDW_ORCASLICER", ["/Applications/OrcaSlicer.app/Contents/MacOS/OrcaSlicer",
                                       r"C:\Program Files\OrcaSlicer\orca-slicer.exe"]),
}


def find_slicer(name: str) -> str | None:
    variable, candidates = SLICERS[name]
    for candidate in [os.environ.get(variable, ""), *candidates]:
        if candidate and Path(candidate).is_file():
            return candidate
    return None


def run_info(slicer: str, path: Path) -> tuple[dict[str, str], list[str], float, int]:
    """The `--info` key = value pairs, the lines that are not key = value, the wall seconds, the exit status."""
    started = time.monotonic()
    done = subprocess.run([slicer, "--info", str(path)], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=INFO_TIMEOUT_SECONDS, check=False)
    elapsed = time.monotonic() - started
    pairs: dict[str, str] = {}
    other: list[str] = []
    for line in (done.stdout + done.stderr).splitlines():
        line = line.strip()
        match = re.fullmatch(r"([a-z_]+)\s*=\s*(.*)", line)
        if match:
            pairs[match.group(1)] = match.group(2)
        elif line and not (line.startswith("[") and line.endswith("]")):
            other.append(line)
    return pairs, other, elapsed, done.returncode


def version_of(slicer: str) -> str:
    done = subprocess.run([slicer, "--help"], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60, check=False)
    for line in (done.stdout + done.stderr).splitlines():
        match = re.search(r"(PrusaSlicer|OrcaSlicer)-([0-9][0-9.]*)", line)
        if match:
            return match.group(2)
    return "unknown"


def judge(expected: dict, pairs: dict[str, str], other: list[str], status: int) -> list[str]:
    """The reasons a file fails; empty when it passes."""
    reasons: list[str] = []
    if status != 0:
        reasons.append(f"exit {status}")
    if pairs.get("manifold") != "yes":
        reasons.append(f"manifold = {pairs.get('manifold')}")
    if pairs.get("number_of_parts", "").strip() != "1":
        reasons.append(f"number_of_parts = {pairs.get('number_of_parts')}")
    flagged = sorted(key for key in pairs if key not in CLEAN_KEYS)
    if flagged:
        reasons.append("repair or error counters: " + ", ".join(f"{key}={pairs[key]}" for key in flagged))
    if other:
        reasons.append("other output: " + " | ".join(other[:3]))
    try:
        if int(pairs["number_of_facets"]) != expected["triangles"]:
            reasons.append(f"number_of_facets {pairs['number_of_facets']} != {expected['triangles']}")
        for axis, app in zip("xyz", expected["size_mm"]):
            seen = float(pairs[f"size_{axis}"])
            if abs(seen - app) > BBOX_TOLERANCE_MM:
                ratio = seen / app if app else float("nan")
                scale = " SCALE: the part is not read in millimetres" if ratio > 100 or ratio < 0.01 else ""
                reasons.append(f"size_{axis} {seen} != app {app:.6f} (tolerance {BBOX_TOLERANCE_MM} mm){scale}")
        volume = float(pairs["volume"])
        if abs(volume - expected["volume_mm3"]) > VOLUME_TOLERANCE * abs(expected["volume_mm3"]):
            reasons.append(f"volume {volume} differs from app {expected['volume_mm3']:.3f} by more than {VOLUME_TOLERANCE * 100:g} %")
    except (KeyError, ValueError) as error:
        reasons.append(f"unreadable --info ({error!r})")
    return reasons


def drop_one_triangle(source: Path, target: Path) -> None:
    """The negative control: the same STL with its 101st triangle removed (and the count lowered)."""
    data = source.read_bytes()
    count = struct.unpack_from("<I", data, 80)[0]
    drop = 100
    body = data[84:]
    kept = body[: 50 * drop] + body[50 * (drop + 1):]
    target.write_bytes(data[:80] + struct.pack("<I", count - 1) + kept)


def rewrite_model(source: Path, target: Path, edit) -> None:
    """The same 3MF package with its model part passed through `edit(text) -> text`; the other parts are copied as they are."""
    with zipfile.ZipFile(source) as original, zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as copy:
        for item in original.infolist():
            content = original.read(item.filename)
            if item.filename == "3D/3dmodel.model":
                content = edit(content.decode("utf-8")).encode("utf-8")
            copy.writestr(item.filename, content)


def drop_one_triangle_3mf(source: Path, target: Path) -> None:
    """The negative control for a 3MF: its 101st <triangle> element removed."""
    def edit(text: str) -> str:
        spans = [match.span() for match in re.finditer(r"<triangle [^>]*/>", text)]
        start, end = spans[100]
        return text[:start] + text[end:]
    rewrite_model(source, target, edit)


def declare_meters(source: Path, target: Path) -> None:
    """The scale control: the same coordinates declared as meters. A slicer that honours the unit attribute reads this part 1000x larger."""
    rewrite_model(source, target, lambda text: text.replace('unit="millimeter"', 'unit="meter"', 1))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", default=str(ROOT / "docs" / "proof" / "tmf"), help="where slicer-open.json goes")
    parser.add_argument("--keep", help="write the fixtures here and keep them (default: a temp directory, removed)")
    arguments = parser.parse_args()

    slicers = {name: path for name in SLICERS if (path := find_slicer(name))}
    for name in SLICERS:
        if name not in slicers:
            print(f"NOT ASSESSED {name}: not installed (set {SLICERS[name][0]} to its executable)")
    if not slicers:
        print("NOT ASSESSED check-slicer-open: no slicer installed")
        return 0
    if shutil.which("dotnet") is None:
        print("NOT ASSESSED check-slicer-open: dotnet not found, so the fixtures cannot be written")
        return 0

    work = Path(arguments.keep) if arguments.keep else Path(tempfile.mkdtemp(prefix="cfdw-slicer-"))
    work.mkdir(parents=True, exist_ok=True)
    try:
        written = subprocess.run(
            ["dotnet", "run", "-c", "Release", "--project", str(ROOT / "tests" / "CfdWorkbench.Core.Tests"), "--",
             "--write-stl-fixtures", str(work)], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", check=False)
        if written.returncode != 0 or not (work / "expected.json").is_file():
            print("FAIL fixtures: the writer did not run\n" + (written.stdout + written.stderr)[-1500:])
            return 1
        expected = json.loads((work / "expected.json").read_text(encoding="utf-8"))
        broken = work / "broken-one-triangle-removed.stl"
        drop_one_triangle(work / "example-closed-whole.stl", broken)
        broken_3mf = work / "broken-one-triangle-removed.3mf"
        drop_one_triangle_3mf(work / "example-closed-whole.3mf", broken_3mf)
        meters = work / "scale-control-declared-meters.3mf"
        declare_meters(work / "example-closed-whole.3mf", meters)
        app_closed = next(entry for entry in expected if entry["file"] == "example-closed-whole.3mf")

        results: list[dict] = []
        failed = False
        for name, slicer in slicers.items():
            version = version_of(slicer)
            for entry in expected:
                pairs, other, seconds, status = run_info(slicer, work / entry["file"])
                reasons = judge(entry, pairs, other, status)
                results.append({"slicer": name, "version": version, "file": entry["file"], "triangles": entry["triangles"],
                                "load_seconds": round(seconds, 2), "warnings": other + [f"{k}={v}" for k, v in pairs.items() if k not in CLEAN_KEYS],
                                "size_mm_app": entry["size_mm"], "size_mm_slicer": [float(pairs.get(f"size_{a}", "nan")) for a in "xyz"],
                                "volume_mm3_app": entry["volume_mm3"], "volume_mm3_slicer": float(pairs.get("volume", "nan")),
                                "manifold": pairs.get("manifold"), "verdict": "PASS" if not reasons else "FAIL", "reasons": reasons})
                print(f"{'PASS' if not reasons else 'FAIL'} {name} {version} {entry['file']} {entry['triangles']} triangles, load {seconds:.2f} s"
                      + ("" if not reasons else " -- " + "; ".join(reasons)))
                failed |= bool(reasons)
            for control in (broken, broken_3mf):
                pairs, other, seconds, status = run_info(slicer, control)
                flagged = pairs.get("manifold") != "yes" or any(key not in CLEAN_KEYS for key in pairs) or bool(other)
                results.append({"slicer": name, "version": version, "file": control.name, "negative_control": True, "flagged_by_slicer": flagged,
                                "manifold": pairs.get("manifold"), "counters": {k: v for k, v in pairs.items() if k not in CLEAN_KEYS},
                                "verdict": "PASS" if flagged else "FAIL"})
                print(f"{'PASS' if flagged else 'FAIL'} {name} {version} negative control {control.name} (one triangle removed): "
                      + ("the slicer flags it" if flagged else "the slicer reports it clean, so this check cannot fail"))
                failed |= not flagged
            # The scale control: declared in meters, the part must read about 1000x larger than the app's millimetres.
            pairs, other, seconds, status = run_info(slicer, meters)
            try:
                seen = [float(pairs[f"size_{axis}"]) for axis in "xyz"]
                honoured = all(abs(value / app - 1000) < 1 for value, app in zip(seen, app_closed["size_mm"]))
            except (KeyError, ValueError, ZeroDivisionError):
                seen, honoured = [], False
            results.append({"slicer": name, "version": version, "file": meters.name, "scale_control": True, "size_mm_slicer": seen,
                            "size_mm_app": app_closed["size_mm"], "unit_honoured_by_slicer": honoured, "verdict": "PASS" if honoured else "FAIL"})
            print(f"{'PASS' if honoured else 'FAIL'} {name} {version} scale control (declared meters): "
                  + (f"read {seen[0]:.1f} mm wide, 1000x the app's millimetres" if honoured else f"the slicer did not read it 1000x larger ({seen}), so a unit error would go unseen"))
            failed |= not honoured

        out = Path(arguments.out)
        out.mkdir(parents=True, exist_ok=True)
        (out / "slicer-open.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {out / 'slicer-open.json'}")
        return 1 if failed else 0
    finally:
        if not arguments.keep:
            shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
