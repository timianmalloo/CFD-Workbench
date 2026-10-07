---
id: design-guided-solver-setup
title: "Design: guided solver setup — Backend environment model, step catalogue per OS and route, detection, smoke test, assistant, telemetry, tests and tracks"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — documents only (Ruling 67 (b)); no build until the operator approves the mockup
tags: [run, backend, install, setup, openfoam, su2, wsl, windows, macos, assistant, telemetry, data-model, ruling-67]
links:
  - { to: spec-amendment-guided-solver-setup, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: note-solver-security-right-size, rel: depends-on }
  - { to: adr-0012-openfoam-backend-macos, rel: depends-on }
  - { to: proof-spike-03, rel: depends-on }
  - { to: proof-spike-03-round2, rel: depends-on }
  - { to: kb-hw-simulation-openfoam-su2-interop, rel: depends-on }
  - { to: design-app-shell, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: mockup-status-bar, rel: relates-to }
  - { to: design-windows-runtime, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: implements }
  - { to: mockup-solver-setup, rel: tested-by }
review-by: 2026-11-04
summary: >-
  The design behind the guided-setup amendment. Model: the existing Backend environment and Backend check, plus three
  append-only facts (Host survey, Environment step, Install acceptance) and a product-published Route catalogue; the
  current step, the session state and Ready are derived, never stored, so resume after a restart is a re-derivation.
  Routes: macOS OpenFOAM.app v2512 (6 steps; the release zip, the inner disk image and the two launch scripts are
  hash-pinned, all Verified on this Mac today); Windows OpenFOAM in an app-owned WSL distribution (Ubuntu 24.04.5 image
  and OpenCFD apt packages pinned by sha256, 8 steps, one administrator prompt, one restart); Windows SU2 v8.5.0 native
  (win64-omp zip pinned, 4 steps, no prompt). Every Windows behaviour is Inferred until the operator's Windows run.
  Findings: the Homebrew cask the operator used strips the quarantine flag (a step M8 refuses), so the product never
  installs through Homebrew; the spec's smoke scalar (Cl on a cavity) cannot exist. DR-SETUP-1..6 are open.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut" }
---

# Design: guided solver setup

**Goal.** Take a foil designer who is not a software engineer from "no solver" to a **Ready** OpenFOAM (or SU2) on
his own laptop, with no command typed and no call to the operator. **Done when:** the operator approves the mockup and
DR-SETUP-1..6; then the tracks in §10 build it. **Not in scope:** the Run pipeline itself (meshing, solving,
harvesting), any new security control (the right-size note owns security), Linux, Intel Macs, Docker on macOS.

The UX and the AI boundaries are in the amendment proposal
[`guided-solver-setup.md`](../specs/amendments/guided-solver-setup.md). The screens are in
[`solver-setup.html`](../mockups/solver-setup.html).

## 0. Evidence ledger (what is Verified, what is not)

| Fact | Value | Label | How observed |
|---|---|---|---|
| The pinned macOS "DMG" is the disk image **inside** the app bundle | `OpenFOAM-v2512.app/Contents/Resources/OpenFOAM-v2512.dmg`, 1,100,175,546 bytes, sha256 `5eb2ab10…393439` (= ADR-0012 D1) | **Verified** 2026-10-04 | `ls -la`, `shasum -a 256` on this Mac; 2.1 s to hash (M4 Max, warm cache) |
| The two launch scripts beside it | `etc/openfoam` sha256 `fa900c4e2cf4c57b0e7c0df4fa167a607435e83aa1675101cf0bffc87502572a`; `volume` sha256 `a1d4e57b2931a32501a39cff6fc7ab509f4306c12a8c5f6dd09f7f051196da28` | **Verified** | `shasum -a 256` |
| The download is a zip, not a DMG | gerlero/openfoam-app release v2.1.3 (2026-01-04): `openfoam2512-app-arm64.zip`, 253,440,212 bytes, sha256 `22ffc888ba49fc6ab3c2207ad33e8044ebc0783edba638f31ecdefd434093d00` | **Verified** | GitHub releases API `digest`; matches the Homebrew cask's `sha256` |
| The operator's install came from Homebrew, and the cask strips quarantine | cask `openfoam` 2.1.3, installed 2026-05-05; `postflight` runs `xattr -rd com.apple.quarantine` on the app | **Verified** | `brew info --cask openfoam`; the tap's `Casks/openfoam.rb` |
| So every macOS spike ran on a quarantine-stripped install | `xattr` on the app shows only `com.apple.provenance` | **Verified** | `xattr` |
| How a quarantined copy behaves when our launcher runs it | — | **Flagged** (probe P-MAC-1, §9) | not observed in any round |
| macOS smoke: `icoFoam/cavity` | ran to t = 0.5, exit 0; `blockMesh` 3 s + `icoFoam` 1 s wall; final "Courant Number mean: 0.222158 max: 0.852134"; build `_87ed40d256-20251219` | **Verified** (2026-10-03 receipt) | `docs/proof/spike-03/receipts/20261003T164323Z-smoke-cavity/` |
| SU2 v8.5.0 Windows assets | `SU2-v8.5.0-win64-omp.zip` 28,043,921 bytes sha256 `4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11`; `-win64-mpi.zip` 28,925,770 bytes sha256 `7da985f9…bd5d25` | **Verified** (publisher digests) | GitHub releases API |
| Ubuntu WSL image | `ubuntu-24.04.5-wsl-amd64.wsl`, 388,975,696 bytes, sha256 `bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e` | **Verified** (publisher SHA256SUMS) | releases.ubuntu.com/noble |
| OpenCFD apt packages for Ubuntu 24.04 (noble) amd64 | `openfoam2512` 2512.0-2: 68,264,976 bytes, sha256 `c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed`, installed 333,725 kB; `openfoam2512-common` 2512.0-2 sha256 `f438ecbf…9026f`; `-default` pulls `-dev` (compilers) | **Verified** (repo index) | `dl.openfoam.com/repos/deb/dists/noble/main/binary-amd64/Packages` |
| OpenCFD repo signing key | fingerprint `DC93C096174122E256DA24063386DD74948D208F` (rsa4096, 2020-04-21) | **Verified** | `gpg --show-keys` on `dl.openfoam.com/pubkey.gpg` |
| Docker alternative | `opencfd/openfoam-default:2512` index digest `sha256:33fb575a…622f319`; amd64 image 477,919,718 bytes compressed | **Verified** | Docker Hub tags API |
| Observed Windows route behavior | Ubuntu 24.04.5 private WSL2 import and signed OpenCFD runtime/common 2512.0-2 hashes; Linux build `_bd2b6720-20260127`; manual M1 cavity to t = 0.5, Courant mean 0.222158; native SU2 v8.5.0, 2,048-cell incompressible smoke, CL −2.157833374e−16 and CD 2.885552317; surveyed Windows 11 Pro build 26300.9457 | **Verified** on this x64 PC; enable/restart, UAC, SmartScreen, managed-device/error paths and product launcher behavior remain **Inferred/Flagged** | `docs/proof/win-routes/receipt.md` and `r133/` command ledger, Ruling 133; manual route evidence only |

## 1. Surface list (E7)

store (append-only setup facts) → model (Backend environment, Route catalogue, facts) → service (survey, step runner,
step selector, smoke runner, assistant validator) → projection (setup state, solver status) → client types (view
models) → UI (Solver setup tab, status-strip Solver item, Run area chip, analysis "needs a solver" prompt) → compute
reader (Run: the Backend environment row and its Ready derivation; the run manifest's install identity). Every row of
§12 names the file family that produces each visible behaviour's data.

## 2. Domain model

**Bounded context:** Run (Backend environment), per A3.1. **Ubiquitous language added:** *route*, *step*, *survey*,
*install identity*, *setup session*. Everything else is A3.1's.

| Term | Kind | Grain ("one row is exactly one …") | Fields | History rule |
|---|---|---|---|---|
| **Route** | value object, product-published (the backend matrix, A5.10) | one supported way to make one backend Ready on one OS and architecture | route id (`mac-ofapp-2512`, `win-wsl-of2512`, `win-su2-850`), OS, arch, minimum OS build, backend, version, pins (hashes, sizes, URLs, build id), ordered step ids, smoke fixture id and tolerance | versioned with the app; a pin change is a new route version; hashed at load (A8.5 "bundled decision data") |
| **Step** | value object inside a Route | one allow-listed action | step id, kind (survey · download · verify · place · os-handoff · run · smoke), plain-words copy ids, needs admin, needs restart, reversible + undo step id, done-check id, cause table | as Route |
| **Host survey** | fact, append-only | one read-only survey of this machine at one time | at, OS name and build, arch, free disk per relevant volume, reboot pending, virtualization (firmware flag, hypervisor present), WSL state and version, distributions, existing installs found (path + install identity), proxy detected, managed-device hints | never updated; the latest survey wins |
| **Environment step** | fact, append-only (the spec's `environment.step` event) | one attempt of one step in one setup session | session id, route id + version, step id, consent record id, started, ended, outcome (succeeded · failed · declined · refused · needs-restart · interrupted), cause code or `unknown`, exit code, elevation used (OS prompt shown, yes/no), bytes downloaded, output excerpt ref (≤ 4 kB, redacted) | never updated; an interrupted attempt is closed by the next launch with outcome `interrupted` |
| **Install acceptance** | fact, append-only | one user acceptance of the unverified-install disclosure for one install identity | at, install identity hash, route id, the disclosure copy id | voided by any change of install identity (Type-2 by identity, no update) |
| **Backend environment** | entity (A3.1, unchanged) | one (substrate, backend, version, OS) on this machine | + `install identity` (hash set), `verified` (bool, derived from the identity vs the pin), `install root` | A3.1 |
| **Backend check** | fact (A3.1) | one detection or smoke test of one Backend environment at one time | + kind ∈ {identify, smoke}; smoke scalar name (AM-SET-3), value, tolerance; banner (`Disallowing` or not); build id; duration | A3.1 |

**Aggregates and invariants.**

- **Setup session** (aggregate root, identity only — its state is derived): *at most one step attempt is open at a
  time; a step attempt starts only when the step is in allowed-next(facts) and a consent record exists.* That is the
  one invariant it protects. A session ends when Ready is derived or the user chooses Done.
- **Backend environment** (A3.2, unchanged): *Ready is derived from the latest Backend check facts (identify ∧ smoke
  passed at the pin with the scalar in tolerance and, for OpenFOAM, banner `Disallowing`).*

**Derived, never stored (derive-don't-store).**

| Derived value | From |
|---|---|
| allowed-next(state) — the set of step ids that may run now | the route's step order, the latest survey, the step facts since the session began, the install acceptance |
| the rails' next step | the first step in allowed-next whose done-check is false |
| the session's screen (Step ready · Waiting for the OS · Failed · Testing · Ready …) | the last step fact + the latest survey |
| "Setting up · step n of m · p %" on the status strip | the open attempt + the route's step count + bytes |
| Ready / Not ready | Backend check facts (A3.1) |
| verified install | install identity == the route's pin set |

**Resume after a restart is a re-derivation.** No "current step" is stored. On launch: close any open attempt as
`interrupted`, run a survey, derive allowed-next, open Solver setup on the rails' next step. Each step's done-check
runs before its action, so a step that finished just before the crash is not repeated.

**Measures.** bytes downloaded (additive over steps); step duration (additive); free disk (semi-additive: a snapshot,
never summed over time); smoke scalar (non-additive).

**Store.** The same local, append-only store the Run area uses for its events (ADR-0011 is reserved for run storage).
`assume:` the setup facts live in that store under the app data directory; *confirm:* when ADR-0011 is written;
*breaks if false:* only the file location changes — the facts and derivations are store-independent.

## 3. Routes and step catalogue

Every parameter (URL, hash, size, distribution name, path, version) is bound by the tool from the route record. The
assistant can only name a step id (AI-11). Each step's **done-check** runs first; if it is true the step records
`succeeded` without acting.

### 3.1 macOS · OpenFOAM.app v2512 (`mac-ofapp-2512`) — Verified values, Flagged first-open

Requirements: Apple silicon (arm64), macOS 14 or later (KB-08 finding 8), ≈ 1.5 GB free (253 MB zip + 1.1 GB image +
the extracted app; Inferred sum, measured by telemetry).

| # | Step id | What it does (plain words) | Done-check / verification | Admin? | Restart? | Reversible? |
|---|---|---|---|---|---|---|
| 0 | `survey.mac` | Looks at this Mac: chip, macOS version, free space, any OpenFOAM already here | survey fact written | no | no | n/a (reads only) |
| 1 | `ofapp.check-existing` | If OpenFOAM-v2512.app is in `/Applications` or `~/Applications`, checks it against the tested build | inner image sha256 `5eb2ab10…` + `etc/openfoam` `fa900c4e…` + `volume` `a1d4e57b…` | no | no | n/a |
| 2 | `ofapp.download` | Downloads OpenFOAM.app (253 MB) from its author's GitHub release, after you accept the licence (GPL-3.0) and read that it is not notarised | zip sha256 `22ffc888…` and size 253,440,212; a mismatch deletes the file | no | no | yes: delete the cached zip |
| 3 | `ofapp.place` | Unpacks it into Applications (`/Applications` if writable without a prompt, else `~/Applications`) | the three hashes of step 1; never removes or adds a quarantine flag | no (Inferred for a standard user: falls back to `~/Applications`) | no | yes: `ofapp.remove` moves the copy setup placed to the Trash |
| 4 | `ofapp.identify` | Starts OpenFOAM once to read its version | banner build id `_87ed40d256-20251219`, through the product launcher (right-size M3, M6) | no | no | n/a |
| 5 | `smoke.openfoam` | Runs a 4-second test case to prove it works on this Mac | §6 | no | no | n/a (writes only its own run directory, M5) |

**Never** on this route: Homebrew (its cask strips quarantine, §0), `xattr`, `spctl`, `sudo`, a Terminal window.

### 3.2 Windows · OpenFOAM in an app-owned WSL distribution (`win-wsl-of2512`) — all Inferred

Requirements: Windows 10 build 19041+ or Windows 11, x64 (arm64 Inferred-possible: the OpenCFD repo has
`binary-arm64`), virtualization on in firmware, ≈ 4 GB free (389 MB image + 68 MB package + ≈ 330 MB installed +
dependencies; Inferred, measured by telemetry).

| # | Step id | What it does (plain words) | Done-check / verification | Admin? | Restart? | Reversible? |
|---|---|---|---|---|---|---|
| 0 | `survey.win` | Looks at this PC: Windows version, free space, virtualization, WSL, any OpenFOAM already here | survey fact | no | no | n/a |
| 1 | `wsl.enable` | Turns on Windows' built-in Linux (WSL) | `wsl.exe --status` exits 0 and reports WSL 2; launched as `wsl.exe --install --no-distribution` through the OS's own administrator prompt (spec A5.10: the tool never runs elevated) | **yes** (Windows prompt) | usually | yes, by the user (Windows Features); setup gives the click path, it does not automate it |
| 2 | `os.restart` | Asks you to restart so Windows can finish | after relaunch: survey shows no reboot pending and `wsl.exe --status` OK | no | **yes** | n/a |
| 3 | `wsl.base.download` | Downloads Ubuntu 24.04 for WSL (389 MB) from Canonical | sha256 `bb415d82…` and size | no | no | yes: delete |
| 4 | `wsl.base.import` | Creates a private Linux just for CFD Workbench, named `cfdw-openfoam2512`, in the app's folder — it does not touch any Linux you already have | **Ruling 133:** before the command the app creates `<app-data>\wsl\` (the parent of the import destination) and verifies it exists and is writable; `wsl.exe --import` does not create a missing parent. Then `wsl.exe -l -v` lists it as version 2; argv `wsl.exe --import cfdw-openfoam2512 <app-data>\wsl\cfdw-openfoam2512 <file> --version 2` (`--import` of a `.wsl` file is **Flagged**; fallback `wsl.exe --install --from-file`) | no | no | yes: `wsl.unregister` (`wsl.exe --unregister cfdw-openfoam2512`) |
| 5 | `of.repo` | Adds OpenFOAM's official package source to that private Linux | the app writes the source line and the pinned key file (fingerprint `DC93C096…208F`) through `\\wsl.localhost\cfdw-openfoam2512\etc\apt\` (Inferred: an imported distribution's default user is root); `apt-get update` exits 0 | no | no | yes (removed with the distribution) |
| 6 | `of.install` | Installs OpenFOAM v2512 (68 MB download) — runtime only, no compilers | argv `wsl.exe -d cfdw-openfoam2512 -u root -e /usr/bin/env DEBIAN_FRONTEND=noninteractive /usr/bin/apt-get install -y --no-install-recommends openfoam2512=2512.0-2 openfoam2512-common=2512.0-2`; then the cached `.deb` sha256 = `c59e65ff…` | no | no | yes (removed with the distribution) |
| 7 | `of.identify` | Starts OpenFOAM once to read its version | banner build id (Linux build id **Not recorded** — recorded on the first run and then pinned) | no | no | n/a |
| 8 | `smoke.openfoam` | Runs the 4-second test case | §6; run directory inside the distribution (Inferred faster than `/mnt/c`) | no | no | n/a |

**Owned-parent rule (Ruling 133, defect class ROUTE-PARENT-MISSING).** Before every external command, the route creates
and verifies every destination parent directory it owns (`mkdir -p` or the platform equivalent, then a check that the
directory exists and is writable), and a failed check stops the step with a cause code before the command runs. This
covers the import destination (step 4), the files step 5 writes through `\\wsl.localhost\...`, and the run directory in
§6. The step that creates the run directory is the route itself, never the solver command. Each such step has a
negative-path test: with its parent absent, the step either creates the parent and succeeds, or refuses cleanly with a
next step the user can take; it never reaches the external command and fails there. The PC-observed rows of this
section are untouched by this amendment; the PC updates its Inferred rows in its W-3 re-entry PR.

Runtime-only install: `openfoam2512-default` depends on `-dev`, which pulls `g++` and `gfortran`. The product needs
neither. Without a compiler in the distribution, `#codeStream` cannot compile even if M1 failed — a free second wall
(Inferred; M1 and M2 remain the control).

### 3.3 Windows · SU2 v8.5.0 native (`win-su2-850`) — Inferred

| # | Step id | What it does | Done-check / verification | Admin? | Restart? | Reversible? |
|---|---|---|---|---|---|---|
| 0 | `survey.win` | as above | survey fact | no | no | n/a |
| 1 | `su2.download` | Downloads SU2 (28 MB) from the SU2 GitHub release, after you accept its licence (LGPL-2.1) | sha256 `4466fe21…` and size 28,043,921 (`win64-omp`: OpenMP threads, no MS-MPI needed — Inferred from the asset name) | no | no | yes |
| 2 | `su2.place` | Unpacks it into the app's own folder | `SU2_CFD.exe` present; its sha256 recorded and pinned on first run | no | no | yes: delete the folder |
| 3 | `smoke.su2` | Runs a small 2D test case | §6 | no | no | n/a |

### 3.4 Undo steps (shared)

`ofapp.remove`, `wsl.unregister`, `su2.remove`, `cache.clear`. Each removes only what a setup step placed, by the
step fact that placed it; an install that existed before setup (a survey found it) has no undo step.

### 3.5 Existing installs (DR-SEC-1, designed as option A)

| OS | Where the survey looks | Accepted as verified when | Otherwise |
|---|---|---|---|
| macOS | `/Applications/OpenFOAM-v2512.app`, `~/Applications/OpenFOAM-v2512.app` | the three hashes of step 1 match (true for the operator's Mac today, §0) | Install the tested build · Use mine anyway (Install acceptance fact) |
| Windows | `wsl.exe -l -v`; in each WSL 2 distribution, `/usr/lib/openfoam/openfoam2512` and `dpkg-query -W openfoam2512` (argv, read-only) | the package version and `.deb` sha256 match the pin (Inferred mechanism) | as above; another OpenFOAM version is reported, not used |
| Windows | `%LOCALAPPDATA%\…\su2-8.5.0\SU2_CFD.exe` placed by setup | its recorded hash matches | reinstall |

A detected OpenFOAM v2606 or another version is listed ("OpenFOAM v2606 found — CFD Workbench is tested with v2512")
and never used: the pin is the product contract (ADR-0012 D1).

## 4. Detection (the survey)

Read-only, no consent, < 5 s budget (measured, §8). Each field names its source; a field that cannot be read is "Not
recorded", never guessed.

| Field | macOS source (Verified commands exist) | Windows source (Inferred) |
|---|---|---|
| OS and build | `sw_vers` | `RtlGetVersion` / registry `CurrentBuildNumber` |
| Arch | `sysctl hw.optional.arm64` | `IsWow64Process2` / `PROCESSOR_ARCHITECTURE` |
| Free disk | `statfs` on the target volume | `GetDiskFreeSpaceEx` on the system drive |
| Reboot pending | n/a | registry: CBS `RebootPending`, WindowsUpdate `RebootRequired`, `PendingFileRenameOperations` |
| Virtualization | n/a | `Win32_Processor.VirtualizationFirmwareEnabled`, or hypervisor present (`Win32_ComputerSystem.HypervisorPresent`) |
| WSL | n/a | `wsl.exe --status` / `--version` exit code and text; optional features `Microsoft-Windows-Subsystem-Linux`, `VirtualMachinePlatform` |
| Existing installs | §3.5 | §3.5 |
| Device maker (for the firmware click path) | n/a | `Win32_ComputerSystem.Manufacturer` |
| Managed device | n/a | MDM enrolment / Group Policy WSL keys (Flagged) |
| Network | a HEAD request to each route's download host | same; proxy from WinHTTP settings |

`wsl.exe` writes UTF-16 output on some versions (Flagged); the parser reads both encodings.

## 5. The step runner and the step selector

- **Selector:** a pure function `allowedNext(route, survey, facts) → set<stepId>` plus `railsNext = first(allowedNext
  where !doneCheck)`. Table-driven from the route record. No I/O. This is the unit most of the tests hit.
- **Runner:** for a consented step id: done-check → act → verify → write one Environment step fact. One step at a
  time (the invariant). Downloads write to a `.part` file and rename after the hash matches; a mismatch deletes it and
  records cause `download.hash-mismatch`. Every process is argv only (M3), with the clean environment (M6). An OS
  hand-off step (UAC, restart) records `needs-restart` or the OS prompt's result.
- **Consent:** one record per step attempt (A5.10); a download that carries terms also records the terms' copy id and
  the user's I accept.

## 6. Smoke test

| Backend | Fixture | Pass = all of | Reference | Label |
|---|---|---|---|---|
| OpenFOAM (both OSes) | bundled `cavity` (blockMesh + icoFoam to t = 0.5), emitted by the app into a fresh run directory (M4, M5) under the app's controlDict (M1) | exit 0; time directories 0.1 … 0.5 with `U` and `p`; master banner `Disallowing` (M2); build id = the route's pin; final-time Courant mean within tolerance | 0.222158 (macOS arm64 and manual Windows WSL2 v2512 run; difference 0 at printed precision) | **Verified** manual Windows M1 result (`docs/proof/win-routes/receipt.md`, Ruling 133); app emission remains Inferred. Proposed 1 × 10⁻³ relative tolerance remains **Inferred**, not calibrated by one equal observation |
| SU2 | a bundled small 2D incompressible case (≤ 5 k cells) | exit 0; `history.csv` with the recorded columns; final Cl within tolerance | Manual Windows native v8.5.0 2,048-cell cylinder smoke: CL −2.157833374e−16, CD 2.885552317 at iteration 61 | **Verified** observation (`docs/proof/win-routes/receipt.md`, Ruling 133); CL is near zero by symmetry, CD is the discriminating measured scalar. Independent physical reference, numerical tolerance and product smoke integration are **Not recorded/Inferred** |

Run directory (Ruling 133): the route, not `blockMesh` or `icoFoam`, creates the fresh run directory and its parents
(in the Windows distribution, under the app's own `/root/CFDWorkbench/` tree), verifies that each exists and is writable,
and only then launches the first command. The smoke test carries the negative-path test of the owned-parent rule in §3.2:
with the run directory's parent absent, the route creates it, or refuses cleanly with a next step.

Budget: 60 s hard timeout → cause `smoke.timeout`. Measured on macOS: ≈ 4 s wall (receipt).

## 7. Assistant design

- **Allocation (A8.6):** Grounded Synthesizer for explanations; Tool-Mediated Constructor for the one-field
  `environment-step` proposal.
- **Context sent (quoted data, AI-04 caps):** the step's catalogue entry, the survey (redacted: no user name, path or
  host), the cause code, the output excerpt (≤ 4 kB, line-numbered), the allowed-next set as ids with their plain
  titles, and the top knowledge snippets (FTS5 over the bundled setup knowledge files: WSL, virtualization, Gatekeeper
  and notarisation, SmartScreen, disk space, OpenFOAM vs SU2).
- **Output schema:** `{ kind: "explanation", text, citations[] }` or `{ kind: "environment-step", stepId }`. Any
  other field → refused.
- **Validator (T0, deterministic, the authority):** (1) `stepId ∈ allowedNext`; (2) every citation resolves to an
  excerpt line or a knowledge id; (3) **no-command lint**: no code span or block, no line that starts with a prompt
  character, no token from the command list (`wsl`, `apt`, `sudo`, `xattr`, `spctl`, `powershell`, `cmd`, `bcdedit`,
  `dism`, `brew`, `curl`, `Set-ExecutionPolicy` …) followed by a flag or argument; the word "WSL" in prose is allowed;
  (4) **no success claim**: "ready", "installed", "fixed", "done", "working" asserted about this machine; (5) **no
  weakening**: Gatekeeper, SmartScreen, Defender, antivirus, Secure Boot with off/disable/allow-anyway verbs (the
  Open Anyway click path is fixed copy, never model text); (6) numerals only from the shared context (AI-03).
  A failed check withholds the answer with the reason and shows the fixed explanation.
- **Prompt, schema and tool descriptions are versioned;** a change re-runs the setup-assist suite (A8.6).
- **The validator lint is the same function** that the SETUP-04 copy test runs over every fixed setup string — one
  definition of "a command to type".

## 8. Failure modes and the cause catalogue

Every cause has a code, a fixed plain-words string, one next step, and a label. The Windows rows are also the
**eval cases** for setup-assist (amendment §4.7): **14 Windows cases, all Flagged** until real output from the
operator's Windows run replaces the authored excerpts.

| Code | Route / step | Detected by | User sees (summary) | Next step | Label |
|---|---|---|---|---|---|
| `win.build-too-old` | survey | build < 19041 | Windows needs an update before WSL can run | Open Windows Update (click path) | Inferred |
| `win.virtualization-off` | survey, `wsl.enable` | firmware flag false; WSL error 0x80370102 | Virtualization is off in this PC's firmware | maker-specific firmware click path | Inferred |
| `win.wsl-feature-missing` | `wsl.enable` | error 0x8007019e / feature state | WSL is not turned on yet | Start step 1 again | Inferred |
| `win.uac-declined` | `wsl.enable` | ShellExecute cancelled (ERROR_CANCELLED 1223) | You chose No. Nothing changed. | Start this step again | Inferred |
| `win.restart-pending` | `os.restart` | reboot-pending keys after relaunch | Windows still needs a restart | Restart now | Inferred |
| `win.policy-blocked` | `wsl.enable` | managed-device hint; install refused | Your organisation's settings block WSL on this PC | SU2 (Other options) · ask your IT team | Flagged |
| `win.store-blocked` | `wsl.enable` | WSL package download from the Store fails | Windows could not download WSL | Check the internet connection · Try again | Flagged |
| `net.offline` | any download | HEAD fails, no route | No internet connection | Try again when online | Inferred |
| `net.proxy-tls` | any download | TLS error / certificate not trusted | A network filter blocked the download | Try another network · Copy a report | Flagged |
| `download.hash-mismatch` | any download | sha256 ≠ pin | The download did not match the tested file; it was deleted | Try again (once); then Copy a report | Verified mechanism |
| `disk.low` | survey, any step | free < the route's need | Not enough space on C: (needs ≈ <n> GB) | Free up space · Check again | Inferred |
| `wsl.import-failed` | `wsl.base.import` | non-zero exit | Windows could not create the private Linux | Try again · Copy a report | Flagged |
| `apt.unreachable` | `of.repo`, `of.install` | `apt-get` exit + "Could not resolve" | The private Linux could not reach OpenFOAM's server | Try again | Inferred |
| `apt.version-gone` | `of.install` | "Version '2512.0-2' for 'openfoam2512' was not found" | The tested OpenFOAM build is no longer offered by its publisher | Copy a report (the operator pins a new build) | Inferred |
| `av.quarantined` | `su2.place`, `smoke.su2` | `SU2_CFD.exe` missing after unzip | Your antivirus removed SU2 | Copy a report (never "turn off antivirus") | Flagged |
| `mac.intel` | survey | arm64 false | OpenFOAM.app needs an Apple-silicon Mac | (no route; CAD still works) | Verified (KB-08) |
| `mac.os-too-old` | survey | macOS < 14 | macOS 14 or later is needed | Software Update click path | Verified requirement (KB-08) |
| `mac.blocked` | `ofapp.identify` | launch refused for a quarantined app | macOS blocked OpenFOAM | Open Anyway click path | Flagged (P-MAC-1) |
| `mac.volume-busy` | `ofapp.identify` | the image cannot mount (another copy mounted) | Another OpenFOAM window is open | Close it · Check again | Flagged |
| `smoke.banner` | smoke | banner ≠ `Disallowing` | CFD Workbench could not switch off case code; this is an app fault | Copy a report | Verified control (M2) |
| `smoke.build-mismatch` | identify, smoke | build id ≠ pin (not raised for an install accepted as unverified; its run manifests record the build) | This is a different OpenFOAM build | Install the tested build | Verified mechanism |
| `smoke.scalar` | smoke | value outside tolerance | The test result did not match | Install the tested build · Copy a report | Verified value / Inferred tolerance |
| `smoke.no-outputs` | smoke | exit 0, time directories missing | The test finished without results | Try again · Copy a report | Verified mechanism (A5.10) |
| `smoke.timeout` | smoke | > 60 s | The test took too long | Try again | Inferred |
| `unknown` | any | none of the above | <excerpt> | Explain this failure (key) · Copy a report | — |

**Copy a report** builds a plain-text report: route, step facts, survey (redacted), the excerpt, the app version. It
contains no key and no user name or path (the A8.5 credential negative test applies).

## 9. Telemetry (instrumentation over inference)

Local, structured (OpenTelemetry data model), on by default, no flag. Nothing leaves the machine.

| Operator question | Event · attribute | Emitted by |
|---|---|---|
| How long does setup take, per step and per route? | `environment.step` · `duration_ms`, `route`, `step` | runner |
| How often does each step fail, and why? | `environment.step` · `outcome`, `cause` | runner |
| How much was downloaded, how much disk used? | `environment.step` · `bytes`; `setup.survey` · `free_bytes` before and after | runner, survey |
| Which path did the user take (existing install, route, other options)? | `setup.session` · `entry` (run area · analysis · status strip), `route`, `existing_install` (verified · unverified · none) | session |
| Did it end Ready? After how many sessions and restarts? | `backend.check` · `kind`, `outcome`, `duration_ms`, `scalar`; `setup.session` · `resumes`, `restarts` | smoke runner, session |
| Did the assistant help? | `assistant.call` · `kind`, `verdict` (shown · withheld:<reason> · refused), `suggestion_disposition`, `latency_ms`, tokens or "Not recorded" | assistant |
| How long does the survey take? | `setup.survey` · `duration_ms` (budget 5 s) | survey |

Every attribute degrades to "Not recorded", never to a default number.

## 10. Test plan

Rings per the repo rule (fast = every join, 60 s budget; readiness = before merge to main; manual = on the named
machine). Every test names what it protects.

**Testable on macOS now:**

| Test | Ring · cost | Protects |
|---|---|---|
| Selector table tests: every route × survey fixture × fact sequence → allowed-next and rails-next | fast · < 1 s | R1, R5 (resume = re-derivation), the one-step invariant |
| Resume: an open attempt + relaunch → `interrupted` closed, done-check before re-act | fast · < 1 s | SETUP-05 |
| Assistant validator: refused ids, extra fields, the no-command lint, success claims, weakening verbs, citations | fast · < 1 s | AI-11 amended, R2 |
| Copy lint: every setup COPY row passes the same no-command lint | fast · < 1 s | SETUP-04 |
| Route records load and their hash matches the build manifest (A8.5) | fast · < 1 s | supply chain of the pins themselves |
| Survey parser fixtures (macOS: captured `sw_vers`, `sysctl`; Windows: authored, Flagged) | fast · < 1 s | §4 |
| Download + verify against a local HTTP fixture (good, truncated, wrong hash) | fast · ≈ 1 s | `download.hash-mismatch`, `.part` rename |
| **macOS route end to end** in a scratch Applications folder: real zip (253 MB, cached), hashes, identify, smoke | manual on the operator's Mac · ≈ 2 min with a warm cache | SETUP-08, the §0 values |
| **P-MAC-1**: copy the app with `com.apple.quarantine` set; run `identify` through the launcher; record what macOS does | manual · ≈ 5 min | DR-SETUP-3, `mac.blocked` |
| Existing-install path on the operator's Mac (the Homebrew install matches the pin today) | manual · ≈ 1 min | SETUP-07, DR-SEC-1 A |

**Needs the operator's Windows laptop (all Windows behaviour is Inferred until then):** survey on real Windows and
capture of its outputs (replace the authored fixtures); `wsl.enable` with UAC and the restart, then resume
(SETUP-05); `--import` of the `.wsl` file; `of.repo`/`of.install` as root; the Linux build id; the smoke test (record
the Windows Courant mean, set the tolerance); SU2 download, place, smoke (record its Cl); provoke `win.uac-declined`,
`net.offline` and `disk.low`; capture every real error text for the eval case set. One sitting, ≈ 60–90 min including
the restart (Inferred).

## 11. Build tracks (after mockup approval and DR rulings)

| Track | Owns (files that produce the data) | Depends on | Size (Inferred) |
|---|---|---|---|
| **T1 Setup core** | `CfdWorkbench.Core` setup model: Route records, facts, selector, validator lint; the route data file | DR-SETUP-1, -2 | small |
| **T2 macOS adapters** | survey, downloader + hasher, placer, identify, smoke runner | T1; **the product launcher (right-size M1–M3, M5, M6)** — not built yet; the smoke test cannot run without it | medium |
| **T3 Setup UI** | Solver setup document, step cards, status-strip Solver item, Run chip, analysis prompt | T1; mockup approval | medium |
| **T4 Assistant** | the setup-assist prompt, schema, knowledge files, eval suite | T1, T3; a key; cases from the Windows run before it ships | small–medium |
| **T5 Windows adapters** | Windows survey, WSL steps, SU2 steps, restart/resume | T1; **the operator's Windows run** (evidence first) | medium |

Order: T1 → (T2 ∥ T3) → T4; T5 after the Windows evidence. The rails (T1–T3) ship without T4.

## 12. Data-source trace (every promised behaviour → the file that produces its data)

| Promised visible behaviour | Data comes from | Owning track |
|---|---|---|
| "No OpenFOAM found" / "already on this Mac and matches" | survey + install-identity hasher | T2 / T5 |
| The recommended route and its reason | route records + selector | T1 |
| Step n of m, time, size, needs line | route record (step fields) + selector | T1 |
| Progress % and time left | downloader byte counts ("Not recorded" until measured) | T2 / T5 |
| "Restart needed … carries on from step 3" | `needs-restart` fact + selector on relaunch | T1, T5 |
| The firmware click path for this PC's maker | survey `maker` + a maker table in the route data (Flagged rows) | T5 |
| Cause + next step | cause table in the route record + runner's cause detection | T1 + T2/T5 |
| Ready line (duration, time) | Backend check fact | T2 / T5 |
| Assistant citations | the stored excerpt (line-numbered) + knowledge ids | T4 |
| Status-strip "Solver: not set up · Setting up · Ready" | projection over facts | T1, T3 |

## 13. Decision requests — DR-SETUP batch

| DR | Question | Options | Evidence | Recommendation |
|---|---|---|---|---|
| **DR-SETUP-1** | Default Windows route | **A.** OpenFOAM in an app-owned WSL distribution (Ubuntu 24.04.5 image by sha256; OpenCFD apt `openfoam2512=2512.0-2` by version and `.deb` sha256). **B.** Docker Desktop + `opencfd/openfoam-default:2512@sha256:33fb575a…`. **C.** SU2 native as the default, OpenFOAM later | B needs WSL 2 anyway (its Windows backend), plus a second installer, an always-running app and subscription terms the product cannot judge for his organisation (KB-08 finding 10). A needs only the WSL enable step that B also needs. C installs in minutes with no prompt but is not the ecosystem he uses, and has no free surface or cavitation (A5.10). A's pin is weaker than an image digest (a repo can drop a version) — mitigated by the `.deb` sha256 check and the `apt.version-gone` cause | **A**, with SU2 under Other options. All Inferred until the Windows run |
| **DR-SETUP-2** | The smoke scalar (the spec's "Cl on the cavity" cannot exist) | **A.** Final-time Courant mean (0.222158 on macOS) + files + banner + build id. **B.** Swap the fixture for a tiny 2D foil with a forces function object (gives a Cl). **C.** Files + banner only, no scalar | A uses the receipt we have, runs in ≈ 4 s, needs no new fixture; B is closer to the product's physics but needs a mesh and a longer run, and the smoke test proves installation, not physics; C cannot catch a silently wrong build | **A** (AM-SET-3); tolerance fixed after the Windows run |
| **DR-SETUP-3** | macOS: the app's own download and the quarantine flag | **A.** Neither add nor remove it (the app's download, like `curl`, carries none); disclose "signed by its author, not notarised" and rely on the pinned hash. **B.** Add the flag to the download and guide the user through Open Anyway | M8 forbids removing the flag. Adding it gives a non-engineer a System Settings detour that checks less than the pinned sha256 does. Whether a quarantined copy even runs through our launcher is unknown (P-MAC-1) | **A**, confirmed by P-MAC-1 and by the security lens. Open Anyway stays for an existing install that carries the flag |
| **DR-SETUP-4** | One verb, and rails without a key | **A.** "Set up a solver" for everyone; the assistant is "Ask about this step" / "Explain this failure" (AM-SET-1, -2, -4). **B.** Keep "Prepare my environment" as the AI entry and add a key-free path beside it | Today's RUN-01 gives the guided path only "given a key", against AI-01 | **A** |
| **DR-SETUP-5** | Where the exact command shows | **A.** Under Technical details, always present, shown before consent (AM-SET-5). **B.** In the step headline (COPY-76 today) | the user is not a software engineer; the operator still needs the command in a screenshot | **A** |
| **DR-SETUP-6** | Reopen the app after a Windows restart | **A.** Register a one-time per-user `RunOnce` entry before asking for the restart (no admin; removed by Windows after use). **B.** No; the next manual launch resumes | A removes "now what?" after a restart; B is one fewer OS integration. Both resume the same way | **A** (Inferred mechanism; tested in the Windows run) |

**DR-SEC-1** (right-size note, open) is designed here as option A (§3.5); option B would delete step 1 and §3.5 and
nothing else.

## 14. Residual risk

- **Windows is unproven.** Every Windows step, source and string in this design is Inferred or Flagged. The first
  Windows run will change some of them.
- **The macOS first-open behaviour is unobserved** for a quarantined copy (P-MAC-1). The operator's own install had
  its flag stripped by Homebrew, so no round tested it.
- **Pins can vanish.** A GitHub release, an Ubuntu image or an apt version can be withdrawn. The causes
  `download.hash-mismatch` and `apt.version-gone` stop setup honestly; the fix is a new pin (an app update).
- **The smoke test proves the install, not the physics.** Ready means "this solver runs here and switches off case
  code"; the A7 labels still govern every result.
- **The product launcher does not exist yet** (right-size M1–M7 are proposals). The smoke test needs it; T2 waits on it.
- **The product's own Windows build is not qualified** (design-windows-runtime: the W1 hosted run failed native
  qualification). Setup on Windows inherits that gate.
