---
id: proof-readyfix2
title: READYFIX2 — Core fixture cwd-relative paths and the Plan-canvas theme key set
type: proof-pack
status: in-review
owner: "@track-readyfix2"
phase: readiness
tags: [readiness, test-repo-layout, theme, m12b, proof]
links:
  - {to: defect-classes, rel: depends-on}
  - {to: design-m12b-points, rel: relates-to}
review-by: 2026-10-27
summary: >-
  Fixes the two causes LEGACY left readiness RED on: Core.Tests fixture reads resolved against the
  process cwd rather than the repo, and the adapters theme gate's declared-brush-key set did not
  account for U1b's seven Plan-canvas aliases. Extends the TEST-REPO-LAYOUT scan to catch the
  cwd-relative-literal shape, and proves the theme gate's declared-key check with a mutant.
review-suggested: []
---

# READYFIX2: fixture paths, the repo-root-walk scan, and the Plan-canvas brush keys

Branch `fix/readiness-m12b`, starting at `a89cb65` (the LEGACY join head).

## 1. Core fixture paths (TEST-REPO-LAYOUT)

`tools/verify-application-core.py` runs the *published* `CfdWorkbench.Core.Tests.dll` with
`cwd=published` (a task-local artifacts directory) to prove the shipped binary matches the one
`tools/run-tests.sh` builds and runs in place. Six test files read M1.2b fixtures through a
cwd-relative literal, so every fixture read resolved against the wrong directory under that run:
`RESULT failures=38`.

**Fixed (7 call sites, 6 files):**

| File | Line(s) before | Fix |
|---|---|---|
| `FoilSourceTests.cs` | `:164` (`Fx` const), 11 call sites | `Fx(name)` → `M12bFixtures.Path(name)` |
| `GeometryTests.cs` | `:171`, `:183` | inline literal → `M12bFixtures.Path(name)` |
| `PointModelTests.cs` | `:11` (`Fx` const), 8 call sites | `Fx(name)` → `M12bFixtures.Path(name)` |
| `PointGestureTests.cs` | `:17` | inline literal → `M12bFixtures.Path(...)` |
| `PointCommandTests.cs` | `:67` | inline literal → `M12bFixtures.Path(...)` |
| `ReopenPointEditTests.cs` | `:10` (`Golden`) | inline literal → `M12bFixtures.Path(...)` |

New: `tests/CfdWorkbench.Core.Tests/M12bFixtures.cs` — resolves `AppContext.BaseDirectory` first,
falling back to the `[CallerFilePath]`-relative source tree, the same shape
`LayoutFileTests.Fixture` already uses for the layout fixtures. `CfdWorkbench.Core.Tests.csproj`
gained `<Content Include="Fixtures\m12b\**\*" CopyToOutputDirectory="PreserveNewest" />` so the
published DLL carries its own fixtures.

Confirmed no `"tests/` or `"src/` literal remains in `tests/CfdWorkbench.Core.Tests/*.cs`
(repo-wide grep, post-fix, zero hits).

## 2. The control gap: NoCwdRelativeFixturePath

`SelfLaunchTests.NoRuntimeRepoRootWalk` only scanned for the `CFDWorkbench.slnx`-walk shape and
missed this cwd-relative-literal variant. Added a sibling, `NoCwdRelativeFixturePath`
(`tests/CfdWorkbench.Desktop.Tests/SelfLaunch.cs`), which fails on any string literal under
`tests/CfdWorkbench.Core.Tests` matching `"(tests|src)/[^"]*"`.

**Scope note (verified, not inferred):** the scan is bounded to `CfdWorkbench.Core.Tests` rather
than all of `tests/**`. Reading every `cwd=` argument across `tools/*.py` shows
`verify-application-core.py` is the only gate that runs a test DLL from a directory other than
`ROOT` (`store_masks(published / "...Core.Tests.dll", "published", published)`,
`verify-application-core.py:276`); `verify-application-adapters.py` launches
`CfdWorkbench.Desktop.Tests`/`CfdWorkbench.Cli.Tests` with `cwd=ROOT` at every call site (`:297`,
`:348`, `:445`). `WorkbenchTests.cs` and `ControllerShellTests.cs` carry the same literal shape
(`"src/CfdWorkbench.Desktop/Assets/example.foil"`) but are not failing today and are outside this
track's owned files; recorded as a residual watch item in `docs/lessons/defect-classes.md`
(TEST-REPO-LAYOUT recurrence note) rather than silently fixed or silently ignored.

**Red, before the fix** (regex run directly against the pre-fix file contents, recovered via
`git stash`/`git show`):

```text
FoilSourceTests.cs:      "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/"
GeometryTests.cs:         "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/"   (x2)
PointModelTests.cs:      "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/"
PointGestureTests.cs:    "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/foil-41-tangents.foil"
PointCommandTests.cs:    "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/foil-41-sixteen-three-anchors.foil"
ReopenPointEditTests.cs: "tests/CfdWorkbench.Core.Tests/Fixtures/m12b/m12a-rail-recovery.cfdw"
```
7 matches, 6 files — the regex the new scan uses would have thrown on every one of them.

**Green, after the fix:** `tools/run-tests.sh` (Release), three consecutive runs, all exit 0,
identical PASS sets each time (376 `PASS` Core, 1 Cli, 281 Desktop). `.tmp-tests/Desktop.log`
line 1: `SelfLaunchTests: all 6 cases passed.`

## 3. Adapters theme key set (U1b Plan-canvas brushes)

`tools/verify-application-adapters.py`'s `contrast_checks` required each `Styles.axaml` theme
variant to declare exactly 16 keys (`expected_keys | focus_keys`). U1b added seven Plan-canvas
brushes per variant (23 total), so the gate raised `"{variant} theme brush keys are missing or
duplicated"` for all three variants.

**Each new key checked against `DESIGN.md`'s token authority** — all seven alias an existing
token, none are off-token:

| Brush key | DESIGN.md token | Styles.axaml `StaticResource` |
|---|---|---|
| `PlanFoilBrush` | `{colors.foil}` | `FoilColor` |
| `PlanSelectionBrush` | `{colors.station}` | `StationColor` |
| `PlanFocusBrush` | `{colors.focus-ring-viewport}` | `FocusRingViewportColor` |
| `PlanMuteBrush` | `{colors.viewport-mute}` | `ViewportMuteColor` |
| `PlanDangerBrush` | `{colors.danger-viewport}` | `DangerViewportColor` |
| `PlanWarningBrush` | `{colors.warning-viewport}` | `WarningViewportColor` |
| `PlanSoftBrush` | `{colors.viewport-soft}` | `ViewportSoftColor` |

(`DESIGN.md` L144–156 and the v10 Point row, L215, name every one of these tokens; `Styles.axaml`
L34–40/59–65/84–90 declare the seven keys per variant against the same `StaticResource` names
already used for `FoilBrush`/`StationBrush` etc.) All seven are legitimate; none required a token
fix, so no key was added to the expected set without a traced source.

**Fix:** `tools/verify-application-adapters.py` — added a `plan_keys` set and extended both the
root-shadow check and the per-variant declared-key check (previously `len(declared) != 16 or
set(declared) != expected_keys | focus_keys`) to `len(declared) != 23 or set(declared) !=
expected_keys | focus_keys | plan_keys`. The runtime loaded-XAML resource check (`expected_keys`,
the `matches`/`luminance` logic) is untouched — it checks the 14+2 keys the live app renders
through `THEME-RESOURCE`/`THEME-FOCUS-RESOURCE` lines, a separate, already-passing surface the
Plan brushes do not participate in.

**Green:** `python3 tools/verify-application-adapters.py` → `{"status": "pass", ...}`, exit 0.

**Mutant (drop `PlanSoftBrush` from the Light variant → red), two levels of proof:**

1. End-to-end: mutated `src/CfdWorkbench.Desktop/Styles.axaml` (removed the Light-variant
   `PlanSoftBrush` line), re-ran `python3 tools/verify-application-adapters.py` → exit 1,
   `"error": "RuntimeError: CfdWorkbench.Desktop.Tests failed; inspect retained raw logs and child
   lifecycle"`. The retained raw log shows `FAIL PlanCanvas_Brushes_AllFromThemeResources
   Exception: Plan brush is missing or not opaque from the theme`, caught by U1b's own runtime
   check before the Python static check ran — the gate is red either way.
2. Isolated: re-ran this track's exact declared-key-set logic (lines ~106–112) directly against
   the mutated file, bypassing the build/app pipeline: `RED: Light theme brush keys are missing or
   duplicated (count=22, missing={... 'PlanSoftBrush' ...})`. Re-ran the same logic against the
   restored file: `GREEN: all three variants declare exactly the 23 expected keys`. This isolates
   proof that the specific lines this track edited are the ones enforcing the key set, not only
   that some other check happens to catch the same mutant.

`src/CfdWorkbench.Desktop/Styles.axaml` was restored byte-for-byte after the mutant proof
(`git diff` against it is empty).

## 4. Readiness

`python3 tools/run-readiness.py` requires a clean tree, so the receipt below is for the commit
that carries this track's fix (see SHAs). All of B0, B1a, B1b, U1a, U1b, U2 (the last four with
`--design docs/design/m12b-points.md`), D3a, D1, D2, C1 and P1 exit 0 against the same
`.tmp-tests/*.log` run. `python3 tools/check-docs.py` exits 0 (0 problems, 0 defects; the 100
`review-suggested` findings are non-failing).

## SHAs

- Before: `a89cb65` (LEGACY join head, the tree this track started from).
- After: see the commit this proof pack ships with.
