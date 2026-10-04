---
id: mockup-solver-setup
title: "Solver setup — the guided install on Windows and macOS, in today's shell (for the operator's approval)"
type: design
status: proposed
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, run, backend, install, setup, openfoam, su2, wsl, windows, macos, assistant, hard-states, operator-show]
links:
  - { to: design-guided-solver-setup, rel: documents }
  - { to: spec-amendment-guided-solver-setup, rel: implements }
  - { to: mockup-status-bar, rel: refines }
  - { to: design-language, rel: depends-on }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
review-by: 2026-12-31
summary: >-
  The 1280 × 800 shell with a Solver setup document tab, on Windows (11 states) and macOS (7 states): first launch with
  no solver, nothing installed with one recommended route, WSL not turned on, the Windows administrator prompt, restart
  needed, resumed after the restart, virtualization off in firmware, installing, a test run that failed with an
  explained cause, an unknown failure with Copy a report and an assistant suggestion, Ready; on macOS an existing
  install that is not the tested build, the licence and download step, macOS blocked the app, a failed test after Use
  mine anyway, Ready. The assistant panel has three modes (answer shown, answer withheld, no key). DESIGN.md tokens,
  the DR-STATUS-1 status strip. Browser check green (0 errors, 0 findings, 0 contrast failures, 22 captures). For the
  operator's visual approval before any build.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut" }
---

# Solver setup — mockup

Open [`solver-setup.html`](solver-setup.html) over `file://`. The **Computer** menu switches Windows (his laptop) and
macOS (the operator's); **State** lists that computer's states; **Assistant** switches answer shown · answer withheld ·
no key; the top-right button switches the theme. A query string opens a state directly, for example
`solver-setup.html?os=win&st=reboot`. Below the shell: the state table and a contact sheet of every state.

**What the operator approves here:** the flow and the look. Nothing is built.

## Captures

`docs/proof/solver-setup/` — one PNG per state in light (`<os>-<state>-ok-light.png`), plus dark for the failed test
and Ready, and `browser-check.json` (the check's full result).

| Windows | macOS |
|---|---|
| `win-first` · `win-nothing` · `win-wsl` · `win-admin` · `win-reboot` · `win-resumed` · `win-virt` · `win-installing` · `win-smokefail` · `win-unknown` · `win-ready` | `mac-first` · `mac-nothing` · `mac-existing` · `mac-download` · `mac-blocked` · `mac-smokefail` · `mac-ready` |

## Labels on what is shown

- **macOS values are Verified** (design §0): the zip, image and script hashes, the build id, the 0.222158 test value,
  the ≈ 4 s test time. The failing value 0.240117 and the build id in the assistant fixture are illustrative.
- **Every Windows value and behaviour is Inferred:** sizes, times, the step order, the Lenovo firmware click path
  (Flagged), the WSL error text in the unknown-failure state (authored, not captured).
- **The Windows prompt is an illustration** of Windows' own dialog, labelled on the page. The app does not draw it.
- **Assistant text is a fixed fixture.** No key, no network.

## Checks run (2026-10-04)

| Check | Result |
|---|---|
| Browser check (`docs/proof/solver-setup/setup-check.mjs <playwright node_modules> <worktree> <capture dir>`, system Chrome): 18 states × assistant modes × themes; page errors; placeholder leaks; the no-command lint over user copy (design §7, outside Technical details); targets ≥ 24 px; text ≥ 11 px; clipped regions; exactly one current step (none on Ready); at most one primary action per card; token-pair contrast in both themes | **0 errors, 0 findings, 0 contrast failures**, 22 captures. Two defects were found and fixed on the way: an unsized assistant icon filled the panel; click-path list items split their text into columns |
| UI craft gate (`ui-craft-gate.py`) | 3 Major + 2 Minor, all **recorded deviations**: the three colours are the drawn Windows prompt (it must look like Windows, and is labelled an illustration); the two side stripes are the accepted DR-STATUS-1 status strip tone |
| Independent persona review | **Not run.** Recommended before build: security (DR-SETUP-3, the RunOnce entry, root inside the app-owned distribution) and UX (the copy for a non-engineer) |

## Proposed copy (DESIGN.md COPY rows, not yet added)

The four honest-limit strings go to spec C2 (amendment §5). The rest are DESIGN.md rows the `ui-design` run adds after
approval. The main ones:

| Proposed row | String |
|---|---|
| Solver item | "Solver: not set up" · "Solver: step <n> of <m>" · "Solver: waiting for Windows" · "Solver: restart needed" · "Solver: not ready" · "Solver: Ready" |
| No solver | "No solver is set up on this <PC · Mac> yet" + "Your designs and the quick analyses work without one. A solver is needed for full CFD runs." |
| Admin prompt | "Windows will ask for permission" + the three-line click path + "CFD Workbench never sees your password. If you click No, nothing changes and you can start this step again later." |
| Restart | "Restart needed" + "After the restart, CFD Workbench opens again by itself and carries on from step <n>. Nothing is lost if you restart later." |
| Resumed | "Welcome back. WSL is on. Setup carries on from step <n>." |
| Firmware | "Turn on virtualization in this PC's firmware" + the maker's click path |
| macOS blocked | "macOS blocked OpenFOAM" + the System Settings click path |
| Existing install | "OpenFOAM is on this Mac, but it is not the tested build" · Install the tested build · Use mine anyway |
| Unknown failure | "We do not recognise this error." + "Next step: copy a report and send it to whoever helps you with this computer. Your designs are not affected." |
| Assistant header | "Explains steps and errors in plain words. It can be wrong. It cannot run anything or change your computer." |
| Withheld | "Response withheld: <reason>. Showing the step's own explanation instead." |
| Suggestion | "Assistant suggestion" + "Opens the step card first. Nothing runs until you start it." |
