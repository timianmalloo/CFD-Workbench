---
id: coordination-pc-kickoff
title: "PC session kickoff - Windows setup, smoke test, Windows Save/Open, OpenFOAM and SU2 runs, cfMesh"
type: plan
status: accepted
owner: "@timianmalloo"
tags: [coordination, windows, kickoff, openfoam, su2, wsl, cfmesh, smoke-test]
links:
  - { to: coordination-two-machine, rel: depends-on }
  - { to: rulings, rel: implements }
  - { to: coordination-windows-runtime-route, rel: relates-to }
  - { to: design-guided-solver-setup, rel: relates-to }
  - { to: plan-tip-handling, rel: relates-to }
review-by: "2026-11-06"
summary: >-
  The first prompt for the Claude Code session on the Windows PC (Rulings 79, 102, 106). Tasks W-0 to W-5 with their
  done-when evidence: setup and coord install, the first Windows smoke test, Windows Save/Open, the WSL OpenFOAM and
  native SU2 routes verified step by step, NACA 0012 code-to-code runs and the SPIKE-04 L3 run, and the cfMesh tip spike.
---

# PC session kickoff

**How to start (operator).** In a Claude Code session on the PC, inside the cloned repo:
*"Read `docs/coordination/pc-kickoff.md` and `docs/coordination/two-machine.md`, then run W-0."* Then one task at a
time; each ends at its done-when or a `blocked` message.

Labels: **Verified** = observed; **Inferred** = reasoned, not observed. Everything about this PC is Inferred until W-0.

## Grounding for the PC session

- Read `CLAUDE.md` / `AGENTS.md`, then [two-machine.md](two-machine.md). You are the **PC session**: you never push
  `main`, never number a ruling, and push only `win/*` branches, each with a pull request.
- Read [rulings 79, 98, 102, 106](../notes/rulings.md),
  [windows-runtime-route.md](windows-runtime-route.md) (W0-W4 there predate this plan; its findings still hold),
  and [guided-solver-setup.md](../design/guided-solver-setup.md) section 3.2 onward (the Windows routes).
- Operator's working rules (from the Mac's global instructions, which the PC does not have): plan before acting on a
  change of more than one file; ask before destructive commands; small conventional commits (`feat:`, `fix:`,
  `chore:`, `docs:`); never commit secrets; **every CFD run has a YAML case file under `cases/`** (schema
  `schemas/cfd-case.schema.json`), and run artifacts go to `runs/<timestamp>/`, never overwriting a previous run.
- Repo lessons the Mac learned the hard way:
  - Tests are `tools/run-tests.sh` (Git Bash), **not** `dotnet test`: the test projects are console harnesses.
    Inner loop: `CFD_TEST_ONLY=<name>`; the full ring once before a PR.
  - The docs gate is `python tools/check-docs.py`. The portability gate is
    `python docs/ai-forward-pack/scripts/run-verify-gates.py`.
  - A multi-line program is a file, then a run, never a heredoc (Ruling 104).
  - Time boxes are in minutes, sized to measured work. A test stays only if it names what it protects.
  - On Windows, `python3` is a Store alias that is not Python: use `python` or `py -3`.
- One worktree per task: `python docs/ai-forward-pack/scripts/coord-core.py worktree new --branch win/<topic>`
  (after W-0's `coord install` in the primary clone).
- Start of every task: `git fetch && git merge origin/main`, then `python tools/xmsg.py unread --mark`.

## Tasks

| # | Goal | Done when (evidence) | Box |
|---|---|---|---|
| **W-0** setup | The PC can build, test and merge this repo | Git for Windows with `core.autocrlf false`; Python 3 (`py -3 --version`); .NET SDK **10.0.203** (`dotnet --version`; `global.json` forbids roll-forward); `gh auth status` ok; `coord-core.py install` then `coord-core.py doctor` with merge drivers effective; **host survey** in `docs/proof/win-setup/receipt.md`: Windows edition and build, CPU model and cores, RAM, free disk, x64 or arm64, virtualization on, WSL state. Post `done` | 30 min |
| **W-1** smoke test | The first Windows run of the app since 2026-09-25, failures recorded rather than fixed | `dotnet build` of the solution; `tools/run-tests.sh` in Git Bash, every failing check listed by name with its first error line; `python tools/check-docs.py`; the Desktop app launched: open the default wing, drag a point, undo, switch to Analysis, Evaluate, then **Save, which is expected to fail** with `DOC-UNSUPPORTED-PERSISTENCE` (`ProjectStore.cs:266`); screenshots of each step; Windows UI notes (Ctrl shortcuts, in-window menu, title bar, DPI). All in `docs/proof/win-smoke/`, PR `win/smoke`. Fix nothing in this task; each defect becomes a line in the receipt | 60 min |
| **W-2** Windows Save/Open | Your friend can save and reopen a project on Windows | Design first, docs only, as a PR for the Fable owner and the data-persistence-architect: the Windows native store contract (handles, reparse points, sharing modes, durable replace, identity), read from the Win32 docs, **no guessed constants** (windows-runtime-route.md W0). Then red-first: the store suite's existing cases plus the Windows-specific ones, green on Windows; the macOS path unchanged and the Mac's readiness green at the join | design 60 min; build sized by the design |
| **W-3** solver routes | The guided-setup Windows steps are Verified, not Inferred | Run by hand, in order, design section 3.2 (OpenFOAM v2512 in an app-owned WSL Ubuntu 24.04.5, 8 steps) and the native SU2 v8.5.0 route (4 steps): for each step its command, exit code, duration, any prompt or restart, and its check, against the pinned sha256 values. Then `cases/win-smoke-cavity.yaml` (copy of `smoke-cavity.yaml` for WSL) runs to t = 0.5. Update only the Inferred rows of `guided-solver-setup.md` you observed, citing `docs/proof/win-routes/` | 90 min |
| **W-4** NACA 0012 runs | Code-to-code evidence on both solvers, then the long L3 run | (a) OpenFOAM in WSL: rerun the round-3 L6 case on this PC and compare Cl/Cd against the Mac's receipt (same numerics, so a cross-platform equivalence check). (b) SU2: the NASA TMR NACA 0012 SA case, compared with CFL3D the way SPIKE-04 is. `assume:` SU2's own test-case repo carries a TMR NACA 0012 config. Confirm before relying on it; if it is false, build the config from the TMR page. (c) **L3 to convergence** (Ruling 79): `cases/spike04r3-g2-l3.yaml` without the 10 h cap, `nice`, ≤ cores − 2, about 24–27 h (Inferred from round 3), in the background; when A4 passes, run `cases/tools/gci.py` on the L5/L4/L3 triplet. Each run has its YAML and a receipt; L3 posts a `note` at start and a `done` or `blocked` at the end | (a) 60 min, (b) 120 min, (c) machine time |
| **W-5** cfMesh tip spike | Is cfMesh a tip-mesh route where snappyHexMesh failed? (Ruling 102) | Step 1: does the installed OpenFOAM provide cfMesh (`cartesianMesh -help`)? Record its version; if it is absent, stop and post `decision-request`. Step 2: the S4/S6 tip coupon of Ruling 98 (`cases/spike03-s6-w2c1.yaml`, `w2c2.yaml`; `docs/plans/tip-handling.md`) meshed with cfMesh; pass is DR-F3-1 A in the tip region with no negative cells, the same checkMesh and layer-coverage measures (`cases/tools/layer-coverage.py`); verdict GO / NO-GO with numbers in `docs/proof/win-cfmesh/`. Runs beside L3 only if cores allow; L3 has priority | 120 min |

**Order.** W-0 → W-1 → (W-2 design ∥ W-3) → W-4 (a), (b), then start (c) → W-5 while L3 runs. W-2's build waits for the
design's review.

## Every PR

- Branch `win/<topic>`; PR to `main`. The body has: goal, done-when, the evidence paths, the receipt's commit sha, and
  what was **not** done.
- The receipt names the commit tested, the Windows build, the SDK, every command with its exit code. An exit code is
  not a result: read the output.
- `python tools/check-docs.py` green on the branch.
- Append the audit entry: `python docs/ai-forward-pack/scripts/audit-log.py` with `--prompt`, the goal, done-when and
  a summary, then `docs-graph.py derive`.
- Post `pr-ready` via `tools/xmsg.py`, commit, push. The Mac answers with `review` (verdict and conditions) and `done`
  after the merge.

## Stops

- A file outside your paths ([two-machine.md](two-machine.md), Paths): post `handoff` and wait. Do not edit it.
- Two failed repair cycles on one task: stop and post `blocked` with the numbers.
- Anything that needs the operator (spec, copy, an install the guided-setup design does not list, anything public):
  post `decision-request`, then ask the operator in this session if they are present.
