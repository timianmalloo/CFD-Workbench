---
id: proof-win-smoke
title: "W-1 Windows smoke receipt — SDK 10.0.203"
type: proof-pack
status: accepted
owner: "@win-smoke"
tags: [windows, smoke-test, native, evidence]
links:
  - { to: coordination-pc-kickoff, rel: implements }
review-by: "2026-11-06"
summary: >-
  Solution build and docs check passed on Windows. The one Git Bash test-ring execution stopped at the
  python3 Store alias before any harness ran. Native default-foil creation, point drag and Analysis
  evaluation were observed. Ctrl+Z and Ctrl+S had no observed effect; persistence error remains Not assessed.
---

# W-1 Windows smoke receipt

Observed 2026-10-07 on Windows NT 10.0.26300.0. This is bounded smoke evidence, not a platform acceptance verdict.
Confidence labels: **Verified** means directly observed in this run; **Not assessed** means the required behavior was not observed.

Goal: execute W-1 and commit evidence under `docs/proof/win-smoke/`. Done when every required row has an observed
disposition. Scope excludes product changes, repairs, tool installation, W-2, publishing, audit writes and index updates.
Fan-out cap: one assigned worker. Plan: SDK identity → build → one ring → docs check → native launch and interactions → evidence commit.
Actual: same sequence, with one capture-mechanics repair cycle before native interaction. No ring retry or product repair.

## Source and launch binding

- Branch: `win/smoke`; tested HEAD: `aa642fc6a1a3da2b8f7bb1f5578139e7ff601859`.
- The W-0 prerequisite is the coordinator-delivered PR #2 evidence at `ddc6f44d40f4b502f65d001a224145ff66b94225`; it was not copied or changed.
- Persisted User PATH followed by Machine PATH, and persisted User DOTNET_ROOT, were loaded into each build/launch process.
- Plain `dotnet --version`: **10.0.203**. Executable: `C:\Users\malla\.dotnet\dotnet.exe`; DOTNET_ROOT: `C:\Users\malla\.dotnet`.
- Built apphost: `C:\Projects\CFD-Workbench-win-smoke\src\CfdWorkbench.Desktop\bin\Debug\net10.0\CfdWorkbench.Desktop.exe`.
- Apphost SHA-256: `71AEE07293AA173A6788382A2FE09A379431F1E5AD23B6E3DC08E5308EBA9B2B`.
- Adjacent Desktop DLL SHA-256: `4D85822E78F4237582A0ABE5FD37F37A2183A97D6777E150B15A5B3C115A4439`.
- Launched directly from that build directory with `Start-Process -FilePath <apphost> -WorkingDirectory <build-directory> -PassThru -WindowStyle Hidden`.
- PID: **996**; process start: **2026-10-07T16:49:24.7538751Z**. No relaunch occurred.
- Native title: **CFD Workbench — Offline Foil**. `launch.json` retains exact source, path, hashes and process identity.

## Command outcomes

| Command / check | Observed result | Exit | Measured seconds | Evidence |
| --- | --- | --- | --- | --- |
| `dotnet build CFDWorkbench.slnx` | Verified: build succeeded; 0 errors, 2 warnings | 0 | 29.548 | `build.log`, `checks.json` |
| `"C:\Program Files\Git\bin\bash.exe" --noprofile --norc tools/run-tests.sh` | Verified: `RING-LOCK waited 0 s`; stopped at first `python3` use, before build or harnesses | 49 | 1.913 | `test-ring.log`, `checks.json` |
| `py -3 tools/check-docs.py` | Verified: `Documentation checks passed.` | 0 | 17.618 | `docs-check.log`, `checks.json` |
| `py -3 tools/check-docs.py` after receipt creation | Verified failure: `validate: 1 defect(s) - 0 problem(s), 0 orphan(s), 1 index-drift item(s).`; first defect `file not in index: proof-win-smoke` | 1 | Not recorded | `docs-check-final.log` |
| Coordinator: `py -3 tools/check-docs.py` after shared-index regeneration | Coordinator-reported final green docs gate at index commit `da6e324d530a27902e3e7e25e09199f45ab10a63`; not rerun by this worker | 0 | Not recorded | Coordinator handoff; index-only commit `da6e324d` |

Every failing check observed in the single ring execution: **ring startup / Python clock prerequisite**.
First error line: `Python was not found; run without arguments to install from the Microsoft Store, or disable this shortcut from Settings > Apps > Advanced app settings > App execution aliases.`
Core parts 1/3, 2/3 and 3/3; Desktop; Analysis parts 1/2 and 2/2; Cli; partition checks; and cost/budget checks:
**Not assessed**. None ran, so there are no named harness failures or passes to claim. No `.tmp-tests` suite logs existed to preserve.

Build warning first lines:

| Finding | First warning line |
| --- | --- |
| Catalog dialog XAML runtime-loader warning | `CatalogDialog.axaml : Avalonia warning AVLN3001: XAML resource "avares://CfdWorkbench.Desktop/CatalogDialog.axaml" won't be reachable via runtime loader, as no public constructor was found` |
| Save-section dialog XAML runtime-loader warning | `SaveSectionDialog.axaml : Avalonia warning AVLN3001: XAML resource "avares://CfdWorkbench.Desktop/SaveSectionDialog.axaml" won't be reachable via runtime loader, as no public constructor was found` |

## Native steps

Screenshots were captured from the visible Windows screen, not generated from source or a managed test.
Each numbered PNG has a matching JSON recording PID/start time, capture time, mouse or keyboard inputs, window title,
DPI and the native UI Automation descendant snapshot. PID start time was checked before every numbered capture.
Scratch automation stayed in `C:\Users\malla\AppData\Local\Temp\w1-ui.ps1` outside the repository.
It uses Win32 `ShowWindow`, `SetForegroundWindow`, `SetCursorPos`/`mouse_event`, Windows Forms `SendKeys.SendWait`,
`Graphics.CopyFromScreen`, and Windows UI Automation. Mouse coordinates below are screen coordinates.
Only the screenshots can establish rendered state; some point automation names remained stale after the drag.
Screenshot and descendant sampling are consecutive, not atomic. The evaluation snapshot advanced before its rendered
image settled; `08-final-state.png` is the stable completed-result image.

| Required step | Input and directly observed disposition | Screenshot |
| --- | --- | --- |
| Default wing open | Verified: clicked `New foil` at (700,380). `Opening new foil…` then default near-elliptic **Untitled** foil: span 1000.00 mm, root chord 127.04 mm, tip chord 12.70 mm, two NACA 0012 stations, approximately 1000 cm² area | `02-default-wing.png`; `02-default-wing-settled.png` |
| Drag one point | Verified: selected leading-edge root handle at (1129,312). First vertical drag to (1129,352) left it unchanged; visible properties state it moves along span only. Spanwise drag to (1179,312) changed rendered length **166.67 → 226.40 mm**, area **~1000 → ~1005 cm²**; status: `Moved leading edge point 2 along the span by 59.74 mm. MAC 110.81 mm.` | `03-drag-point.png`; `03-drag-point-along-span.png` |
| Undo | Verified finding: sent `Ctrl+Z` (`SendKeys '^z'`). No observed undo: rendered length stayed **226.40 mm**, area stayed **~1005 cm²**, and moved-point status remained. Undo restoration therefore failed in this interaction; root cause Not assessed | `04-undo.png` |
| Switch to Analysis | Verified: clicked native `Analysis area` at (1028,1244). Conditions band and result panel rendered with `No analysis yet. Set the conditions, then Evaluate.` | `05-analysis.png` |
| Evaluate | Verified: clicked Evaluate at (1270,226). `Evaluating — VLM + strip · 512 panels…` then `Analysis complete — VLM + strip · 5.245 s`. Stable result: CL **0.177**, CDi (Trefftz) **0.00099**, lift **240.38 N**, induced drag **1.354 N**, `Analysis: Current` | `06-evaluate.png`; `06-evaluate-settled.png`; `08-final-state.png` |
| Save | Verified input finding: sent `Ctrl+S` (`SendKeys '^s'`). No save dialog, new visible status, or error appeared. Expected `DOC-UNSUPPORTED-PERSISTENCE` is **Not assessed** because the persistence operation was not observed. Actual visible message stayed `Analysis complete — VLM + strip · 5.245 s`; no error code was shown | `07-save.png`; `08-final-state.png` |
| Unchanged document after Save attempt | Verified at the observed UI boundary: all serialized UI Automation descendant nodes were identical between `06-evaluate-settled.json` and `07-save.json` (`Compare-Object` returned no differences). Name **Untitled**, displayed span/chords, point shape, analysis results and status remained unchanged. Internal document identity/revision and filesystem persistence are **Not assessed** | `07-save.json`; `08-final-state.png` |

Evaluation displays expected scope limits: wing drag unavailable because `cd missing at 38 strips`, total CL/CD unavailable
because junction/mast/wave/spray contributions are missing, and cavitation unavailable because depth is not set.
These are visible results, not numerical validation claims.

## Windows UI observations

| Surface | Direct observation / limit |
| --- | --- |
| Ctrl shortcuts | Ctrl+Z and Ctrl+S were sent to the visible app using native desktop input. Neither had the requested visible effect. Other shortcuts Not assessed. Key-handler root cause Not assessed. |
| In-window menu | No File/Edit menu was visible in the native window, and neither Save nor Undo appeared in its UI Automation descendants. Plan/Section/Foil source tabs and the CAD/Analysis workspace bar were visible. Alternate menu invocation Not assessed. |
| Title bar | Standard Windows title bar visibly shows `CFD Workbench — Offline Foil` and minimize/maximize/close controls. Title-bar interaction Not assessed. `windows-chrome.png` is a lossless app-window crop from the full desktop image; `windows-chrome.json` binds it to this launch and records the crop. The oversized full-desktop duplicate was removed. |
| DPI/scaling | `GetDpiForWindow` returned **144** throughout, corresponding to **150%** of 96 DPI. App text indicator stayed `Text 100 %`. Two screens were visible in the full desktop capture. Monitor transitions, other scaling levels and minimum-window behavior Not assessed. |
| Point accessibility consistency | After the successful spanwise drag, point descendant name still reported length 166.67 mm while rendered properties and hover text reported 226.40 mm. This is an observed cross-surface discrepancy; root cause Not assessed. |

## Capture-mechanics repair and residual blockers

One bounded capture repair cycle occurred before interaction. Initial error: `PID start-time binding changed`.
PowerShell `ConvertFrom-Json` had parsed `startUtc` as `System.DateTime`; comparing it with a formatted string failed.
The scratch guard was corrected to compare UTC ticks. A scratch declaration-placement error produced
`Unexpected attribute 'DllImport'.`; its placement was corrected in the same cycle.
The initial `WindowStyle Hidden` launch and foreground attempt were occluded by the terminal. `ShowWindow(9)` alone
did not clear occlusion. The coordinator-authorized `Shell.Application.MinimizeAll()` followed by existing-PID
activation and `ShowWindow(9)` cleared it. No operator action or product change was used.

The worker's final docs check failed on the excluded shared index; its failure output is retained above.
The coordinator regenerated `docs/docs-index.js` in `da6e324d530a27902e3e7e25e09199f45ab10a63` and reported a green
`py -3 tools/check-docs.py` result before this receipt-only follow-up. This closes the index blocker, not the native findings.
Accepted status records the completed bounded evidence disposition; it does not grant Windows platform acceptance.

Residual blockers: the ring did not reach harnesses; Undo did not restore the edit; Save did not visibly invoke persistence,
so the requested unsupported-persistence code is Not assessed. No failures were fixed. The live app remains at the
final analysis view with its unsaved edited foil, available for operator review.
The required authored paths are the only worker commit scope. The coordinator owns audit/index reconciliation after handoff.
