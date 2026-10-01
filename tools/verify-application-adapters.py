#!/usr/bin/env python3
"""Build and verify the first adapters in one retained, task-local artifact root."""
from __future__ import annotations

import datetime as dt
import hashlib
import json
import math
import os
import pathlib
import re
import signal
import subprocess
import sys
import tempfile
import time

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRATCH: pathlib.Path
ARTIFACTS: pathlib.Path
RECEIPTS: pathlib.Path
ENV: dict[str, str]
RECORDED_ENV: dict[str, str]
COMMON: list[str]


def sha(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        while chunk := source.read(65536):
            digest.update(chunk)
    return digest.hexdigest()


def source_outputs() -> dict[str, str]:
    result = {}
    for project_root in (ROOT / "src", ROOT / "tests"):
        for directory in project_root.rglob("*"):
            if directory.name not in ("bin", "obj") or not directory.is_dir():
                continue
            for item in directory.rglob("*"):
                if item.is_symlink():
                    raise RuntimeError(f"source output symlink: {item}")
                if item.is_file():
                    result[str(item.relative_to(ROOT))] = sha(item)
    return result


def source_inputs() -> dict[str, str]:
    paths = [ROOT / "CFDWorkbench.slnx", ROOT / "global.json", ROOT / ".editorconfig",
             ROOT / "tools" / "verify-application-adapters.py",
             ROOT / "tools" / "package-application.py", ROOT / "DESIGN.md"]
    paths.extend((ROOT / "docs" / "examples" / "foildsl").glob("*.foil"))
    for directory in (ROOT / "src", ROOT / "tests"):
        paths.extend(item for item in directory.rglob("*") if item.is_file()
                     and not any(part in ("bin", "obj") for part in item.relative_to(directory).parts))
    return {str(item.relative_to(ROOT)): sha(item) for item in sorted(set(paths))}


def contrast_checks(test_step: dict) -> dict[str, object]:
    """Check colors resolved by the loaded-XAML test, never guessed XML overrides."""
    output = pathlib.Path(test_step["stdout"])
    raw = output.read_text(encoding="utf-8")
    matches = re.findall(r"^THEME-RESOURCE (Light|Dark|HighContrast)/([A-Za-z]+Brush)=(#[0-9A-Fa-f]{8})$",
                         raw, re.M)
    expected_keys = {"CanvasBrush", "SurfaceBrush", "SurfaceSoftBrush", "InkBrush",
        "MutedBrush", "LineBrush", "PrimaryBrush", "OnPrimaryBrush", "DangerBrush",
        "ViewportBrush", "ViewportGridBrush", "ViewportInkBrush", "FoilBrush", "StationBrush"}
    focus_keys = {"SystemControlFocusVisualPrimaryBrush",
                  "SystemControlFocusVisualSecondaryBrush"}
    expected = {(variant, key) for variant in ("Light", "Dark", "HighContrast")
                for key in expected_keys}
    resolved = {(variant, key): color for variant, key, color in matches}
    if (len(matches) != 42 or set(resolved) != expected or
            "THEME-SHADOW-MUTATION refused Dark/SurfaceBrush" not in raw or
            "THEME-RESOURCE-CHECK loaded-XAML Light/Dark/HighContrast 42" not in raw):
        raise RuntimeError("loaded-XAML theme resource evidence missing, duplicated, or stale")
    if any(color[:3].upper() != "#FF" for color in resolved.values()):
        raise RuntimeError("theme resource is translucent; contrast background is unresolved")
    focus_matches = re.findall(
        r"^THEME-FOCUS-RESOURCE (Light|Dark|HighContrast)/(SystemControlFocusVisual(?:Primary|Secondary)Brush)=(#[0-9A-Fa-f]{8})$",
        raw, re.M)
    focus_expected = {(variant, key) for variant in ("Light", "Dark", "HighContrast")
                      for key in focus_keys}
    focus_resolved = {(variant, key): color for variant, key, color in focus_matches}
    if len(focus_matches) != 6 or set(focus_resolved) != focus_expected or \
            any(color[:3].upper() != "#FF" for color in focus_resolved.values()):
        raise RuntimeError("loaded-XAML opaque two-ring focus resources missing or duplicated")
    source = (ROOT / "src" / "CfdWorkbench.Desktop" / "Styles.axaml").read_text(encoding="utf-8")
    raw_blocks = re.findall(r'<ResourceDictionary x:Key="([^"]+)">(.*?)</ResourceDictionary>',
                            source, re.S)
    blocks = {("HighContrast" if key == "{x:Static local:NativeReviewThemes.HighContrast}" else key): block
              for key, block in raw_blocks}
    if set(blocks) != {"Light", "Dark", "HighContrast"}:
        raise RuntimeError("theme dictionary is missing")
    without_themes = re.sub(r'<ResourceDictionary x:Key="[^"]+">.*?</ResourceDictionary>',
                            '', source, flags=re.S)
    # U1b: the Plan canvas (Plan view) aliases existing DESIGN.md viewport tokens under
    # their own brush keys, per variant, rather than reusing the 3D-viewport brush names —
    # PlanFoilBrush/PlanSelectionBrush/PlanFocusBrush/PlanMuteBrush/PlanDangerBrush/
    # PlanWarningBrush/PlanSoftBrush map to {colors.foil}/{colors.station}/
    # {colors.focus-ring-viewport}/{colors.viewport-mute}/{colors.danger-viewport}/
    # {colors.warning-viewport}/{colors.viewport-soft} (DESIGN.md L144-156, L215-218).
    plan_keys = {"PlanFoilBrush", "PlanSelectionBrush", "PlanFocusBrush", "PlanMuteBrush",
        "PlanDangerBrush", "PlanWarningBrush", "PlanSoftBrush"}
    # The property grid's brushes (PGRID, docs/reviews/ui-property-grid.md §10.6): {colors.control-line},
    # {colors.focus-ring}, {colors.selection} and its ink, {colors.warning}; their DESIGN.md values in all three
    # themes are pinned by PropertiesPane_GridBrushes_InAllThreeThemes.
    grid_keys = {"ControlLineBrush", "FocusRingBrush", "SelectionBrush", "OnSelectionBrush", "WarningBrush"}
    brush_pattern = r'<SolidColorBrush x:Key="([A-Za-z]+Brush)"'
    if (expected_keys | focus_keys | plan_keys | grid_keys).intersection(re.findall(brush_pattern, without_themes)):
        raise RuntimeError("root resource shadows theme brush")
    for variant, block in blocks.items():
        declared = re.findall(brush_pattern, block)
        all_keys = expected_keys | focus_keys | plan_keys | grid_keys
        if len(declared) != len(all_keys) or set(declared) != all_keys:
            raise RuntimeError(f"{variant} theme brush keys are missing or duplicated")
    def luminance(color: str) -> float:
        values = [int(color[index:index + 2], 16) / 255 for index in (3, 5, 7)]
        linear = [value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4 for value in values]
        return sum(weight * value for weight, value in zip((.2126, .7152, .0722), linear))
    pairs = (("InkBrush", "SurfaceBrush"), ("MutedBrush", "SurfaceBrush"),
             ("ViewportInkBrush", "ViewportBrush"), ("OnPrimaryBrush", "PrimaryBrush"),
             ("StationBrush", "ViewportBrush"))
    result = {}
    for variant in ("Light", "Dark", "HighContrast"):
        for foreground, background in pairs:
            first = luminance(resolved[(variant, foreground)])
            second = luminance(resolved[(variant, background)])
            result[f"{variant}:{foreground}/{background}"] = (max(first, second) + .05) / (min(first, second) + .05)
    if min(result.values()) < 4.5:
        raise RuntimeError(f"critical token contrast below 4.5:1: {result}")
    return {"ratios": result, "testStdoutSha256": sha(output),
            "stylesSha256": sha(ROOT / "src" / "CfdWorkbench.Desktop" / "Styles.axaml"),
            "testDllSha256": sha(ARTIFACTS / "bin" / "CfdWorkbench.Desktop.Tests" / "debug" /
                                 "CfdWorkbench.Desktop.Tests.dll"),
            "desktopDllSha256": sha(ARTIFACTS / "bin" / "CfdWorkbench.Desktop" / "debug" /
                                    "CfdWorkbench.Desktop.dll"),
            "appliedTemplateContrast": "not_assessed"}


# The applied-contrast matrix on the real shell window (ShellWindowTests ThemeMatrix_ShellControls_AppliedContrast).
# It replaced the pre-shell `--theme-controls` matrix when the pre-shell window retired. Every row is
# `THEME-ROW <theme>/<row> fg=#AARRGGBB bg=#AARRGGBB ratio=<F4> floor=<n>`; this gate re-derives each ratio.
SHELL_THEME_ROWS = {
    "focus.tab": 3, "focus.tab.vs-fill": 3,
    "tab.Plan.unselected.rest": 4.5, "tab.3D samples.selected.rest": 4.5, "select.tab.3D samples": 3,
    "tab.Section sample.unselected.rest": 4.5, "tab.Foil source.unselected.rest": 4.5,
    "tab.Section.unselected.rest": 4.5, "tab.Foil source.unselected.hover": 4.5,
    "focus.tab.selected-while-focused": 3, "focus.tab.selected-while-focused.vs-fill": 3,
    "tab.Foil source.selected.rest": 4.5, "select.tab.Foil source": 3, "tab.Foil source.selected.hover": 4.5,
    "tool.Properties.selected.rest": 4.5, "select.tool.Properties": 3, "tool.Properties.selected.hover": 4.5,
    "tool.Browser.unselected.rest": 4.5, "tool.Browser.unselected.hover": 4.5,
    "tool.Rail controls.unselected.rest": 4.5, "tool.Rail controls.unselected.hover": 4.5,
    "appbar.sidebar.rest": 4.5, "focus.appbar": 3, "focus.appbar.vs-fill": 3,
    "browser.selected": 4.5, "browser.unselected": 4.5, "focus.browser": 3, "focus.browser.vs-fill": 3,
    "focus.browser.selected": 3, "focus.browser.selected.vs-fill": 3,
    "span.text": 4.5, "focus.span": 3, "source.text": 4.5,
    "viewport.annotation": 4.5, "section.annotation": 4.5,
    "modal.body": 4.5, "modal.save.rest": 4.5, "modal.save.hover": 4.5, "modal.discard.rest": 4.5,
    "modal.discard.hover": 4.5, "modal.cancel.rest": 4.5, "modal.cancel.hover": 4.5,
    # Pressed and returned (Styles.axaml Button:pressed, ListBoxItem :pressed / :selected:pressed); returned paints as rest.
    "appbar.sidebar.hover": 4.5, "appbar.sidebar.pressed": 4.5, "appbar.sidebar.returned": 4.5,
    **{f"modal.{choice}.{state}": 4.5 for choice in ("save", "discard", "cancel") for state in ("pressed", "returned")},
    **{f"browser.{kind}.{state}": 4.5 for kind in ("selected", "unselected") for state in ("hover", "pressed", "returned")},
    **{f"tab.Foil source.{kind}.{state}": 4.5 for kind in ("selected", "unselected") for state in ("pressed", "returned")},
    # TextBox states (Styles.axaml TextBox rules): 4.5 for text, 3 for the caret.
    "span.hover": 4.5, "span.focus.text": 4.5, "span.focus.caret": 3, "span.focus-hover": 4.5,
    "span.selection": 4.5, "span.returned": 4.5, "source.focus.text": 4.5, "source.selection": 4.5,
    # The point fields that replace the retired per-control numeric field.
    "point-span.text": 4.5, "focus.point-span": 3,
}
SHELL_LIVE_FLIP_ROWS = {"live-flip.dark.tab.Section.unselected": 4.5, "live-flip.dark.select.tab.Foil source": 3}
SHELL_THEMES = ("light", "dark", "high-contrast", "default")
SHELL_EXPECTED = {f"{theme}/{row}": floor for theme in SHELL_THEMES for row, floor in SHELL_THEME_ROWS.items()}
SHELL_EXPECTED.update({f"light/{row}": floor for row, floor in SHELL_LIVE_FLIP_ROWS.items()})
SHELL_SUMMARY = f"THEME-SHELL-CHECK rows={len(SHELL_EXPECTED)} variants={len(SHELL_THEMES)} source=shell-MainWindow"
SHELL_ROW = re.compile(r"^THEME-ROW (?P<key>[a-z-]+/[^=]+?) fg=(?P<fg>#[0-9A-Fa-f]{8}) bg=(?P<bg>#[0-9A-Fa-f]{8}) "
                       r"ratio=(?P<ratio>\S+) floor=(?P<floor>\S+)$")


def parse_applied_theme_rows(raw: str) -> dict[str, float]:
    def lightness(value: str) -> float:
        channels = [int(value[index:index + 2], 16) / 255 for index in (3, 5, 7)]
        linear = [channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4
                  for channel in channels]
        return sum(weight * channel for weight, channel in zip((.2126, .7152, .0722), linear))
    observed: dict[str, float] = {}
    for line in raw.splitlines():
        if not line.startswith("THEME-ROW "):
            continue
        match = SHELL_ROW.match(line)
        if match is None:
            raise RuntimeError(f"malformed shell theme row: {line}")
        key = match["key"]
        if key not in SHELL_EXPECTED or key in observed:
            raise RuntimeError(f"missing, duplicate, or unknown shell theme row: {key}")
        fg, bg = match["fg"].upper(), match["bg"].upper()
        if fg[:3] != "#FF" or bg[:3] != "#FF":
            raise RuntimeError(f"shell theme row has unresolved alpha: {key}")
        try:
            emitted, floor = float(match["ratio"]), float(match["floor"])
        except ValueError:
            raise RuntimeError(f"shell theme row ratio or floor is malformed: {key}") from None
        first, second = lightness(fg), lightness(bg)
        ratio = (max(first, second) + .05) / (min(first, second) + .05)
        if not math.isfinite(emitted) or abs(ratio - emitted) > 0.00051 or floor != SHELL_EXPECTED[key] or \
                ratio + 0.00001 < floor:
            raise RuntimeError(f"shell theme contrast fails or emitted ratio/floor mismatches: {key}")
        observed[key] = ratio
    if set(observed) != set(SHELL_EXPECTED) or SHELL_SUMMARY not in raw.splitlines() or \
            "PASS ThemeMatrix_ShellControls_AppliedContrast" not in raw.splitlines():
        raise RuntimeError("shell applied theme required row set is incomplete")
    return observed


def applied_theme_checks(step: dict) -> dict[str, object]:
    output = pathlib.Path(step["stdout"])
    raw = output.read_text(encoding="utf-8")
    result = parse_applied_theme_rows(raw)
    lines = raw.splitlines()
    first = next(line for line in lines if line.startswith("THEME-ROW light/focus.tab "))
    text = next(line for line in lines if line.startswith("THEME-ROW high-contrast/modal.cancel.rest "))
    background = re.search(r"bg=(#[0-9A-Fa-f]{8})", text)[1]
    negative_cases = {
        "missing": raw.replace(first + "\n", "", 1),
        "duplicate": raw + "\n" + first + "\n",
        "alpha": raw.replace(first, first.replace("fg=#FF", "fg=#80", 1), 1),
        "ratio": raw.replace(first, re.sub(r"ratio=\S+", "ratio=99.0000", first), 1),
        "nan": raw.replace(first, re.sub(r"ratio=\S+", "ratio=nan", first), 1),
        "floor-lowered": raw.replace(text, text.replace("floor=4.5", "floor=3", 1), 1),
        "below-floor": raw.replace(text, re.sub(r"fg=#[0-9A-Fa-f]{8}", "fg=" + background, text, count=1)
                                   .replace(re.search(r"ratio=\S+", text)[0], "ratio=1.0000", 1), 1),
        "unknown-row": raw + "\nTHEME-ROW light/invented.row fg=#FF000000 bg=#FFFFFFFF ratio=21.0000 floor=4.5\n",
        "variant-missing": "\n".join(line for line in lines if not line.startswith("THEME-ROW default/")),
        "summary-missing": "\n".join(line for line in lines if not line.startswith("THEME-SHELL-CHECK ")),
        "check-not-passed": "\n".join(line for line in lines
                                      if line != "PASS ThemeMatrix_ShellControls_AppliedContrast"),
    }
    for name, mutation in negative_cases.items():
        try:
            parse_applied_theme_rows(mutation)
        except RuntimeError:
            continue
        raise RuntimeError(f"applied theme negative control escaped: {name}")
    return {"ratios": result, "rows": len(result), "source": "shell-MainWindow",
        "negativeControls": sorted(negative_cases),
        "stdoutSha256": sha(output),
        "stylesSha256": sha(ROOT / "src" / "CfdWorkbench.Desktop" / "Styles.axaml"),
        "windowSourceSha256": sha(ROOT / "src" / "CfdWorkbench.Desktop" / "MainWindow.axaml.cs"),
        "testDllSha256": sha(ARTIFACTS / "bin" / "CfdWorkbench.Desktop.Tests" / "debug" /
                             "CfdWorkbench.Desktop.Tests.dll"),
        "desktopDllSha256": sha(ARTIFACTS / "bin" / "CfdWorkbench.Desktop" / "debug" /
                                "CfdWorkbench.Desktop.dll")}


def asset_roots() -> dict[str, dict[str, object]]:
    result = {}
    assets = sorted((ARTIFACTS / "obj").glob("*/project.assets.json"))
    if len(assets) < 7:
        raise RuntimeError(f"expected at least seven project asset maps, found {len(assets)}")
    expected_packages = pathlib.Path(ENV["NUGET_PACKAGES"]).resolve(strict=True)
    expected_obj = (ARTIFACTS / "obj").resolve(strict=True)
    for path in assets:
        data = json.loads(path.read_text(encoding="utf-8"))
        folders = [pathlib.Path(folder).resolve(strict=True) for folder in data["packageFolders"]]
        output = pathlib.Path(data["project"]["restore"]["outputPath"]).resolve(strict=True)
        if folders != [expected_packages] or output.parent != expected_obj:
            raise RuntimeError(f"restore/cache path escaped task scratch: {path}")
        result[path.parent.name] = {"assets": str(path), "packageFolders": [str(folder) for folder in folders],
                                    "restoreOutput": str(output)}
    return result


def binary_hashes(receipt: dict) -> dict[str, str]:
    result = {}
    for runtime in ("osx-arm64", "win-x64"):
        for project in ("CfdWorkbench.Desktop", "CfdWorkbench.Cli"):
            output = pathlib.Path(receipt["publish"][f"{project}-{runtime}"])
            for name in (project + ".dll", project + (".exe" if runtime == "win-x64" else ""),
                         "CfdWorkbench.Core.dll", "CfdWorkbench.Persistence.dll"):
                path = output / name
                if path.is_file():
                    result[str(path.relative_to(SCRATCH))] = sha(path)
    if len(result) < 12:
        raise RuntimeError("published binary identity inventory is incomplete")
    return result


def process_table() -> dict[int, dict[str, object]]:
    if os.name == "nt":
        raise RuntimeError("Windows process ownership adapter: Not assessed")
    output = subprocess.check_output(["ps", "-axo", "pid=,ppid=,pgid=,state=,lstart=,command="],
                                     cwd=ROOT, text=True, encoding="utf-8", errors="replace", timeout=10)
    rows = {}
    for line in output.splitlines():
        fields = line.split()
        if len(fields) < 10:
            raise RuntimeError("Unexpected process identity record")
        rows[int(fields[0])] = {"parent": int(fields[1]), "group": int(fields[2]),
                                "state": fields[3], "start": " ".join(fields[4:9]),
                                "command": " ".join(fields[9:])}
    return rows


def observe(group: int, owned: dict[int, str]) -> dict[int, dict[str, object]]:
    table = process_table()
    for pid, row in table.items():
        if row["group"] == group:
            if pid in owned and owned[pid] != row["start"]:
                raise RuntimeError("Owned process identity changed")
            owned[pid] = str(row["start"])
    return {pid: row for pid, row in table.items()
            if pid in owned and owned[pid] == row["start"] and not str(row["state"]).startswith("Z")}


def terminate_owned(group: int, owned: dict[int, str]) -> None:
    for requested_signal in (signal.SIGTERM, signal.SIGKILL):
        for pid in observe(group, owned):
            current = process_table().get(pid)
            if current is not None and current["start"] == owned[pid]:
                try:
                    os.kill(pid, requested_signal)
                except ProcessLookupError:
                    pass
        deadline = time.monotonic() + 5
        while time.monotonic() < deadline:
            if not observe(group, owned):
                return
            time.sleep(.05)
    raise RuntimeError("Owned processes remain alive after exact cleanup")


def run(name: str, argv: list[str], timeout: int = 600) -> dict:
    stdout_path, stderr_path = RECEIPTS / f"{name}.stdout", RECEIPTS / f"{name}.stderr"
    started = dt.datetime.now(dt.timezone.utc).isoformat()
    process_table()  # Verify the ownership observer before acquiring a child.
    owned: dict[int, str] = {}
    collector: set[str] = set()
    timed_out = False
    cleanup_error: str | None = None
    failure: str | None = None
    code: int | None = None
    with stdout_path.open("wb") as stdout, stderr_path.open("wb") as stderr:
        child = subprocess.Popen(argv, cwd=ROOT, env=ENV, stdin=subprocess.DEVNULL,
                                 stdout=stdout, stderr=stderr, start_new_session=True)
        pid = child.pid
        start_identity = "Not recorded"
        deadline = time.monotonic() + timeout
        try:
            start_identity = process_table().get(pid, {}).get("start", "Not recorded")
            while True:
                code = child.poll()
                live = observe(pid, owned)
                collector.update(f"{child_pid} {row['start']} {row['command']}" for child_pid, row in live.items()
                                 if "Avalonia.BuildServices.Collector" in str(row["command"]))
                if collector:
                    raise RuntimeError("Avalonia collector observed in exact-owned process group")
                if code is not None:
                    if live:
                        raise RuntimeError("Build exited with live owned descendants")
                    break
                if time.monotonic() >= deadline:
                    timed_out = True
                    raise RuntimeError("Owned command timed out")
                time.sleep(.1)
        except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as error:
            failure = f"{type(error).__name__}: {error}"
        finally:
            try:
                terminate_owned(pid, owned)
                child.wait(timeout=5)
                if observe(pid, owned):
                    raise RuntimeError("Owned descendants appeared after cleanup")
            except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as error:
                cleanup_error = f"{type(error).__name__}: {error}"
                if child.poll() is None:
                    child.terminate()
                    try:
                        child.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        child.kill()
                        child.wait(timeout=5)
    remaining = observe(pid, owned) if cleanup_error is None else {"Not assessed": cleanup_error}
    return {"argv": argv, "cwd": str(ROOT), "environment": dict(RECORDED_ENV), "pid": pid,
            "startUtc": started, "psStartIdentity": start_identity, "exitCode": child.returncode,
            "timedOut": timed_out, "collectorObserved": sorted(collector),
            "ownedPidStart": owned, "remainingProcessGroup": remaining, "cleanupError": cleanup_error,
            "failure": failure,
            "stdout": str(stdout_path), "stderr": str(stderr_path),
            "stdoutSha256": sha(stdout_path), "stderrSha256": sha(stderr_path)}


def require_step(receipt: dict, name: str, argv: list[str], timeout: int = 600) -> None:
    step = run(name, argv, timeout)
    receipt["steps"].append(step)
    if step["exitCode"] != 0 or step["remainingProcessGroup"] or step["collectorObserved"] or step["timedOut"] or step["failure"] or step["cleanupError"]:
        raise RuntimeError(f"{name} failed; inspect retained raw logs and child lifecycle")


def publish_dir(project: str, runtime: str) -> pathlib.Path:
    candidates = [item.parent for item in (ARTIFACTS / "publish" / project).rglob(project + ".dll")
                  if runtime in str(item.parent)]
    if len(candidates) != 1:
        raise RuntimeError(f"expected one {project} {runtime} publish output, found {candidates}")
    return candidates[0]


def main() -> int:
    if os.name == "nt":
        print("Windows-host process ownership and runtime: Not assessed; no child launched", file=sys.stderr)
        return 4
    process_table()
    global SCRATCH, ARTIFACTS, RECEIPTS, ENV, RECORDED_ENV, COMMON
    selected_temp_root = pathlib.Path(tempfile.gettempdir()).resolve(strict=True)
    SCRATCH = pathlib.Path(tempfile.mkdtemp(prefix="cfd-adapters-verify-", dir=selected_temp_root)).resolve(strict=True)
    if SCRATCH.parent != selected_temp_root:
        raise RuntimeError("Scratch escaped the selected canonical temporary parent")
    ARTIFACTS, RECEIPTS = SCRATCH / "artifacts", SCRATCH / "receipts"
    for name in ("artifacts", "receipts", "dotnet-home", "nuget", "http-cache", "tmp"):
        (SCRATCH / name).mkdir()
    ENV = os.environ.copy()
    for key in ("CFDW_REVIEW_MODE", "CFDW_REVIEW_PERSONA", "CFDW_REVIEW_SIZE",
                "CFDW_REVIEW_STATE", "CFDW_REVIEW_THEME", "CFDW_REVIEW_REDUCED_MOTION",
                "CFDW_REVIEW_PATH", "CFDW_STARTUP_SMOKE", "CFDW_TIMING_CONTROL",
                "CFDW_TIMING_TRIALS"):
        ENV.pop(key, None)
    for key, suffix in {"DOTNET_CLI_HOME": "dotnet-home", "NUGET_PACKAGES": "nuget",
                        "NUGET_HTTP_CACHE_PATH": "http-cache", "TMPDIR": "tmp",
                        "TMP": "tmp", "TEMP": "tmp"}.items():
        ENV[key] = str(SCRATCH / suffix)
    ENV.update(DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1",
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false",
               AVALONIA_TELEMETRY_OPTOUT="1")
    RECORDED_ENV = {key: ENV[key] for key in ("DOTNET_CLI_HOME", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH",
        "TMPDIR", "TMP", "TEMP", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "DOTNET_CLI_TELEMETRY_OPTOUT",
        "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "DOTNET_GENERATE_ASPNET_CERTIFICATE", "AVALONIA_TELEMETRY_OPTOUT")}
    COMMON = ["--artifacts-path", str(ARTIFACTS), "--disable-build-servers",
              "-p:UseSharedCompilation=false", "--nologo"]
    signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(128 + signum))
    receipt = {"cwd": str(ROOT), "scratch": str(SCRATCH), "environment": RECORDED_ENV,
               "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT,
                                               text=True, encoding="utf-8", errors="replace").strip(),
               "sourceInputsBefore": source_inputs(), "sourceOutputsBefore": source_outputs(),
               "steps": [], "publish": {}, "packages": {},
               "status": "incomplete"}
    try:
        xaml = sorted((ROOT / "src" / "CfdWorkbench.Desktop").glob("*.axaml"))
        if not xaml:
            raise RuntimeError("native token corpus is empty")
        receipt["xamlCorpus"] = [str(item.relative_to(ROOT)) for item in xaml]
        require_step(receipt, "xaml-token-lint", [sys.executable,
            "docs/ai-forward-pack/scripts/xaml-token-lint.py", "--root", str(ROOT),
            "src/CfdWorkbench.Desktop"])
        require_step(receipt, "build", ["dotnet", "build", "CFDWorkbench.slnx", *COMMON])
        receipt["assetRootsAfterBuild"] = asset_roots()
        for project in ("CfdWorkbench.Cli.Tests", "CfdWorkbench.Desktop.Tests"):
            dll = ARTIFACTS / "bin" / project / "debug" / (project + ".dll")
            if not dll.is_file():
                raise RuntimeError(f"test assembly missing: {dll}")
            require_step(receipt, project, ["dotnet", str(dll)])
            if project == "CfdWorkbench.Desktop.Tests":
                receipt["themeContrast"] = contrast_checks(receipt["steps"][-1])
                receipt["appliedThemeContrast"] = applied_theme_checks(receipt["steps"][-1])
        smoke = ARTIFACTS / "bin" / "CfdWorkbench.Desktop" / "debug" / "CfdWorkbench.Desktop"
        if not smoke.is_file():
            raise RuntimeError(f"native startup executable missing: {smoke}")
        smoke_selectors = {"CFDW_STARTUP_SMOKE": "1", "CFDW_REVIEW_MODE": "1",
                           "CFDW_REVIEW_PERSONA": "designer", "CFDW_REVIEW_SIZE": "1024x700",
                           "CFDW_REVIEW_STATE": "empty", "CFDW_REVIEW_THEME": "high-contrast",
                           "CFDW_REVIEW_REDUCED_MOTION": "1"}
        ENV.update(smoke_selectors)
        RECORDED_ENV.update(smoke_selectors)
        receipt["nativeSmokeEnvironment"] = smoke_selectors | {"CFDW_REVIEW_PATH": None}
        try:
            require_step(receipt, "native-xaml-startup-smoke", [str(smoke)], timeout=30)
            raw = pathlib.Path(receipt["steps"][-1]["stderr"]).read_text(encoding="utf-8")
            if "NATIVE-STARTUP smoke-opened" not in raw:
                raise RuntimeError("native XAML startup did not reach Window.Opened")
        finally:
            for key in smoke_selectors:
                ENV.pop(key, None)
                RECORDED_ENV.pop(key, None)
        for runtime in ("osx-arm64", "win-x64"):
            for project in ("CfdWorkbench.Desktop", "CfdWorkbench.Cli"):
                require_step(receipt, f"publish-{project}-{runtime}", ["dotnet", "publish",
                    f"src/{project}/{project}.csproj", "-c", "Release", "-r", runtime,
                    "--self-contained", "true", *COMMON], timeout=900)
                output = publish_dir(project, runtime)
                receipt["publish"][f"{project}-{runtime}"] = str(output)
                runtime_file = output / ("libcoreclr.dylib" if runtime == "osx-arm64" else "coreclr.dll")
                if not runtime_file.is_file():
                    raise RuntimeError(f"self-contained runtime missing: {runtime_file}")
            output = publish_dir("CfdWorkbench.Desktop", runtime)
            target = SCRATCH / "packages" / runtime
            require_step(receipt, f"package-{runtime}", [sys.executable, "tools/package-application.py",
                "--build-dir", str(output), "--output-dir", str(target),
                "--platform", "macos" if runtime == "osx-arm64" else "windows"])
            receipt["packages"][runtime] = str(target)
        receipt["assetRootsFinal"] = asset_roots()
        receipt["binarySha256"] = binary_hashes(receipt)
        receipt["status"] = "pass"
    except Exception as error:
        receipt["status"] = "fail"
        receipt["error"] = f"{type(error).__name__}: {error}"
    finally:
        receipt["sourceInputsAfter"] = source_inputs()
        receipt["sourceInputsUnchanged"] = receipt["sourceInputsBefore"] == receipt["sourceInputsAfter"]
        receipt["sourceOutputsAfter"] = source_outputs()
        receipt["sourceOutputsUnchanged"] = receipt["sourceOutputsBefore"] == receipt["sourceOutputsAfter"]
        receipt["artifactFiles"] = [str(item.relative_to(SCRATCH)) for item in ARTIFACTS.rglob("*") if item.is_file()]
        receipt["artifactSymlinks"] = [str(item.relative_to(SCRATCH)) for item in ARTIFACTS.rglob("*") if item.is_symlink()]
        if not receipt["sourceInputsUnchanged"] or not receipt["sourceOutputsUnchanged"] or receipt["artifactSymlinks"]:
            receipt["status"] = "fail"
        path = RECEIPTS / "verification.json"
        path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
        print(json.dumps({"status": receipt["status"], "receipt": str(path), "error": receipt.get("error"),
                          "steps": [{"name": step["argv"][0], "exit": step["exitCode"],
                                     "remaining": step["remainingProcessGroup"]} for step in receipt["steps"]],
                          "sourceInputsUnchanged": receipt["sourceInputsUnchanged"],
                          "sourceOutputsUnchanged": receipt["sourceOutputsUnchanged"]}))
    return 0 if receipt["status"] == "pass" else 1


if __name__ == "__main__":
    sys.exit(main())
