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
    brush_pattern = r'<SolidColorBrush x:Key="([A-Za-z]+Brush)"'
    if (expected_keys | focus_keys).intersection(re.findall(brush_pattern, without_themes)):
        raise RuntimeError("root resource shadows theme brush")
    for variant, block in blocks.items():
        declared = re.findall(brush_pattern, block)
        if len(declared) != 16 or set(declared) != expected_keys | focus_keys:
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


APPLIED_ROWS = {"toolbar.enabled", "tab.section.selected", "tab.source.selected",
    "source.active.readonly", "station.selected", "station.focused", "cv.selected", "cv.focused",
    "numeric.enabled.owned-draft", "modal.body", "modal.save", "modal.discard", "modal.cancel",
    "viewport.annotation", "section.annotation", "focus.toolbar", "focus.tab", "focus.numeric"}
APPLIED_THEMES = {"Light", "Dark", "HighContrast", "Default"}


def parse_applied_theme_rows(raw: str) -> dict[str, float]:
    expected = {f"{theme}/{row}" for theme in APPLIED_THEMES for row in APPLIED_ROWS}
    observed: dict[str, float] = {}
    placement = set()
    disabled = set()
    for line in raw.splitlines():
        if line.startswith("FOCUS-PLACEMENT "):
            try:
                fact = json.loads(line.removeprefix("FOCUS-PLACEMENT "))
            except json.JSONDecodeError:
                raise RuntimeError("malformed focus placement evidence") from None
            key = str(fact.get("theme")) + "/" + str(fact.get("name"))
            if key not in {f"{theme}/focus.tab" for theme in APPLIED_THEMES} or key in placement or \
                    any(fact.get(name) is not True for name in
                        ("AdornedLink", "CompositionLink", "AdornerClipped")) or \
                    any(not isinstance(fact.get(name), str) or not fact[name] or fact[name] == "null"
                        for name in ("target", "outer", "inner", "adornerClip", "outerClip",
                                     "size", "offset", "scale", "orientation", "anchor", "center",
                                     "outerPaint", "innerPaint", "backdrop")) or \
                    not isinstance(fact.get("rotation"), (float, int)):
                raise RuntimeError("focus placement missing target-bound composition or clip operands")
            placement.add(key)
        if line.startswith("THEME-DISABLED-NUMERIC "):
            parts = line.split()
            if len(parts) != 4 or parts[1] not in APPLIED_THEMES or parts[2:] != ["enabled=false", "exempt=true"]:
                raise RuntimeError("malformed disabled-numeric observation")
            if parts[1] in disabled:
                raise RuntimeError("duplicate disabled-numeric observation")
            disabled.add(parts[1])
        if not line.startswith("THEME-APPLIED "):
            continue
        parts = line.split()
        if len(parts) < 6 or parts[1] not in expected or parts[1] in observed:
            raise RuntimeError("missing, duplicate, or unknown applied theme row")
        fields = dict(part.split("=", 1) for part in parts[2:] if "=" in part)
        if len(fields) != len(parts) - 2 or not re.fullmatch(r"#[0-9A-Fa-f]{8}", fields.get("fg", "")) or \
                not re.fullmatch(r"#[0-9A-Fa-f]{8}", fields.get("bg", "")):
            raise RuntimeError("applied theme row has malformed color evidence")
        if fields["fg"][:3].upper() != "#FF" or fields["bg"][:3].upper() != "#FF":
            raise RuntimeError("applied theme row has unresolved alpha")
        if parts[1].split("/", 1)[1].startswith("focus."):
            if fields.get("focus") not in ("painted-template-border", "target-bound-adorner"):
                raise RuntimeError("focus row lacks painted border evidence")
            if fields["focus"] == "target-bound-adorner" and \
                    (fields.get("ring") != "outer2-inner1" or
                     not re.fullmatch(r"#FF[0-9A-Fa-f]{6}", fields.get("inner", "")) or
                     not re.fullmatch(r"[0-9.]+x[0-9.]+@composition-target", fields.get("outer", "")) or
                     parts[1] not in placement):
                raise RuntimeError("target-bound focus adorner lacks measured two-ring evidence")
            threshold = 3.0
        else:
            if fields.get("text") not in ("AccessText", "TextPresenter", "TextBlock") or \
                    not re.fullmatch(r"[0-9.]+x[0-9.]+", fields.get("bounds", "")):
                raise RuntimeError("text row lacks rendered presenter bounds")
            width, height = map(float, fields["bounds"].split("x"))
            if width <= 0 or height <= 0:
                raise RuntimeError("text presenter has zero bounds")
            if parts[1].endswith("/source.active.readonly") and fields.get("clip") != "scroll":
                raise RuntimeError("active source text lacks measured scroll clip")
            threshold = 4.5
        def lightness(value: str) -> float:
            channels = [int(value[index:index + 2], 16) / 255 for index in (3, 5, 7)]
            linear = [channel / 12.92 if channel <= .04045 else ((channel + .055) / 1.055) ** 2.4
                      for channel in channels]
            return sum(weight * channel for weight, channel in zip((.2126, .7152, .0722), linear))
        first, second = lightness(fields["fg"]), lightness(fields["bg"])
        ratio = (max(first, second) + .05) / (min(first, second) + .05)
        try:
            emitted_ratio = float(fields["ratio"])
        except (KeyError, ValueError):
            raise RuntimeError("applied contrast ratio is missing or malformed") from None
        if not math.isfinite(emitted_ratio) or ratio + 0.00001 < threshold or \
                abs(ratio - emitted_ratio) > 0.001:
            raise RuntimeError(f"applied contrast fails or emitted ratio mismatches: {parts[1]}")
        observed[parts[1]] = ratio
    if set(observed) != expected or disabled != APPLIED_THEMES or \
            placement != {f"{theme}/focus.tab" for theme in APPLIED_THEMES} or \
            "THEME-APPLIED-CHECK rows=72 variants=4 source=actual-MainWindow" not in raw:
        raise RuntimeError("applied theme required row set is incomplete")
    return observed


def applied_theme_checks(step: dict) -> dict[str, object]:
    output = pathlib.Path(step["stdout"])
    raw = output.read_text(encoding="utf-8")
    result = parse_applied_theme_rows(raw)
    first = next(line for line in raw.splitlines() if line.startswith("THEME-APPLIED Light/toolbar.enabled "))
    placement = next(line for line in raw.splitlines() if line.startswith("FOCUS-PLACEMENT "))
    negative_cases = {
        "missing": raw.replace(first + "\n", "", 1),
        "duplicate": raw + "\n" + first + "\n",
        "alpha": raw.replace(first, first.replace("fg=#FF", "fg=#80", 1), 1),
        "ratio": raw.replace(first, re.sub(r"ratio=[0-9.]+", "ratio=99.000000", first), 1),
        "nan": raw.replace(first, re.sub(r"ratio=[0-9.]+", "ratio=nan", first), 1),
        "focus-placement-missing": raw.replace(placement + "\n", "", 1),
        "focus-wrong-target": raw.replace(placement, placement.replace('"CompositionLink":true',
                                                          '"CompositionLink":false', 1), 1),
        "focus-clip-missing": raw.replace(placement, re.sub(r'"outerClip":"[^"]+"',
                                                       '"outerClip":"null"', placement, count=1), 1),
    }
    for name, mutation in negative_cases.items():
        try:
            parse_applied_theme_rows(mutation)
        except RuntimeError:
            continue
        raise RuntimeError(f"applied theme negative control escaped: {name}")
    return {"ratios": result, "negativeControls": sorted(negative_cases),
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
                require_step(receipt, "theme-applied-controls", ["dotnet", str(dll), "--theme-controls"])
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
