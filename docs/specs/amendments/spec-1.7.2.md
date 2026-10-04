---
id: spec-amendments-1-7-2
title: "Spec 1.7.2 amendment batch (draft) — guided solver setup, the smoke-test scalar and the toggle shortcut, as exact text, for the spec owner's approval"
type: spec
status: draft
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, rulings, run, backend, setup, windows, wsl, openfoam]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-1, rel: depends-on}
  - {to: spec-amendment-guided-solver-setup, rel: depends-on}
  - {to: rulings, rel: depends-on}
  - {to: design-area3-analysis, rel: depends-on}
  - {to: adr-0012-openfoam-backend-macos, rel: depends-on}
review-by: 2027-04-01
summary: >-
  A draft batch for the spec owner. 23 amendments and two insertions to cfd-workbench-v1, each with the quoted
  before-text and the exact 1.7.2 text, traced to Ruling 69 (guided solver setup, DR-SETUP-1..6) and Ruling 67 OD-1
  (the toggle shortcut). It also fixes a design-text mismatch (section handle Length). Five open questions have no
  ruling and are not applied. Revision 1.7.2 of the spec carries the batch; the change record is Appendix H, section
  H.2. Merge only after the owner approves.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut" }
---

# Spec 1.7.2 amendment batch (draft)

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.2.
**Status:** **draft, awaiting approval.** The batch is merged only after the owner approves it.

## How to read this

- **One row is one amendment**, in the form of [spec 1.7.1](spec-1.7.1.md). *Before* quotes the 1.7.1 text exactly; the
  script that applied the batch refused to write unless every quoted text matched exactly once. *After* is the exact
  new text in revision 1.7.2 (struck text is superseded and stays in place).
- **Source for every row:** [Ruling 69](../../notes/rulings.md) and the approved proposal
  [guided-solver-setup.md](guided-solver-setup.md) (AM-SET-1 to AM-SET-6), or Ruling 67 OD-1.
- **Windows is Inferred.** Every Windows statement (the WSL route, its steps, the restart, the reopen entry, the
  smoke-test value) is marked Inferred in the spec text until the operator's Windows run.
- **Already in 1.7.1, not repeated:** DR-SEC-1 A (detect an existing install, hash-check it, the "unverified install"
  disclosure).
- The shortcut: the 1.7.1 spec names no ⌘2 or ⌘3 for the toggle ("a shortcut" is unnamed), so AM-1.7.2-20 names ⇧⌘A and
  records that ⌘1 to ⌘3 stay the workspaces.

## Amendments (23)

| ID | Spec clause (line: the 1.7.1 spec) | Before (quoted) | After (exact new text) | Source | Affected tests or designs |
|---|---|---|---|---|---|
| AM-1.7.2-1 | A3.1 Backend check (:246) | `the smoke-test scalar (Cl on the bundled cavity-lid fixture) and its tolerance` | the smoke-test scalar (~~Cl on the bundled cavity-lid fixture~~ *(1.7.2: the mean Courant number at the final time, printed by the solver, on the bundled cavity-lid fixture; a lid-driven cavity has no wing, so it has no Cl. SU2: the bundled 2D case's final Cl — Ruling 69, DR-SETUP-2)*) and its tolerance | Ruling 69 DR-SETUP-2; guided-solver-setup AM-SET-3 | the Backend check fact schema; the smoke-test harvest |
| AM-1.7.2-2 | A5.10 readiness, pass clause (:1061) | `and the fixture's Cl lies inside its tolerance` | and ~~the fixture's Cl lies inside its tolerance~~ *(1.7.2: the final-time mean Courant number lies inside its tolerance — 0.222158 on macOS arm64, OpenFOAM v2512, build `_87ed40d256-20251219`; the tolerance is set after the operator's Windows run, and the Windows value is Not recorded until then; SU2: the bundled 2D case's final Cl lies inside its tolerance, its reference value Not recorded until the first SU2 run — Ruling 69, DR-SETUP-2)* | Ruling 69 DR-SETUP-2; AM-SET-3; receipt `docs/proof/spike-03/receipts/20261003T164323Z-smoke-cavity/log.icoFoam` | the install smoke test (cavity-lid fixture) |
| AM-1.7.2-3 | A5.10 backend matrix (:1057) | `Windows via Docker Desktop or WSL2; OpenFOAM.app arm64, unsigned — disclosed` | Windows via ~~Docker Desktop or WSL2~~ *(1.7.2: the default route is OpenFOAM in an app-owned WSL Linux distribution — an Ubuntu 24.04 image with the OpenCFD `openfoam2512` packages; SU2 v8.5.0 native is the other option; Docker is not used. Inferred until the operator's Windows run — Ruling 69, DR-SETUP-1)*; OpenFOAM.app arm64, unsigned — disclosed *(1.7.2: signed by its author, not notarised, and disclosed as "not notarised" — Ruling 69, DR-SETUP-3)* | Ruling 69 DR-SETUP-1, DR-SETUP-3; proposal F7a-4 | the matrix publication; ADR-0012 (Windows route) |
| AM-1.7.2-4 | A5.10 step catalogue (:1064) | `Docker Desktop · pull image by digest · create the WSL2 distribution · run the smoke test` | ~~Docker Desktop · pull image by digest · create the WSL2 distribution~~ *(1.7.2: the route's step catalogue — macOS: download · verify · place · identify; Windows (Inferred until the Windows run): enable WSL · restart · Linux base · OpenFOAM · identify; SU2: download · verify · place — Ruling 69, DR-SETUP-1)* · run the smoke test | Ruling 69 DR-SETUP-1; proposal §3.2 | the step-id allow-list per route |
| AM-1.7.2-5 | A5.10 step display (:1068) | `each step is shown with the exact command, its consequence,` | each step is shown with ~~the exact command~~ *(1.7.2: its action in plain words, its time and download size; the exact command as the tool bound it, the pin and the licence sit under "Technical details", which every step card carries and which is shown before consent — Ruling 69, DR-SETUP-5)*, its consequence, | Ruling 69 DR-SETUP-5; AM-SET-5 | the step card; the Technical details disclosure |
| AM-1.7.2-6 | A5.10 Gatekeeper clause (:1073) | `Gatekeeper/SmartScreen are never disabled; ParaView` | Gatekeeper/SmartScreen are never disabled *(1.7.2: the app neither adds nor removes `com.apple.quarantine` on its own OpenFOAM.app download, discloses "not notarised" and relies on the pinned hash check; when macOS blocks the app, setup gives the one click path System Settings › Privacy & Security › Open Anyway. The macOS behaviour is confirmed by probe P-MAC-1, pending — Ruling 69, DR-SETUP-3)*; ParaView | Ruling 69 DR-SETUP-3; probe P-MAC-1 (pending) | the macOS install path; the refused-step test from 1.7.1 stands |
| AM-1.7.2-7 | A5.10 step execution (:1067) | `the tool executes an accepted step and reports its result;` | the tool executes an accepted step and reports its result *(1.7.2: a step that asks for a restart is resumed from the facts at the next launch; on Windows a one-time per-user entry reopens the app after the restart. Inferred until the operator's Windows run — Ruling 69, DR-SETUP-6)*; | Ruling 69 DR-SETUP-6; proposal F7a-8 | the Windows after-restart entry (pending Windows run) |
| AM-1.7.2-8 | RUN-01, open Run and key (:1225) | `**when** I open Run, **then** detection shows platform, substrate, version, download and storage needs and the exact proposed action; **given** a key, **then** "Prepare my environment" proposes` | **when** I open Run *(1.7.2: or choose Set up a solver)*, **then** detection shows platform, substrate, version, download and storage needs and ~~the exact proposed action~~ *(1.7.2: one recommended route in plain words)*; ~~**given** a key, **then** "Prepare my environment"~~ *(1.7.2: **with or without a key or any network model** — the assistant, when present, only explains, AI-11 — **then** the setup)* proposes | Ruling 69 DR-SETUP-4; AM-SET-1 | the Run entry points; the no-key path test |
| AM-1.7.2-9 | RUN-01, command shown (:1225) | `with the exact command the tool bound from the published matrix, its consequence, elevation or reboot need and vendor terms verbatim,` | with ~~the exact command the tool bound from the published matrix~~ *(1.7.2: its action in plain words, time and download size; the exact command as the tool bound it, the pin and the licence are under Technical details and shown before consent)*, its consequence, elevation or reboot need and vendor terms verbatim, | Ruling 69 DR-SETUP-5; AM-SET-5 | the step card |
| AM-1.7.2-10 | RUN-01, resume and Ready (:1225) | `**given** consent denial, offline or interruption, **then** setup remains recoverable and CAD works; Ready requires detection plus the bundled cavity-lid smoke test at the pinned digest with its Cl inside tolerance,` | **given** consent denial, offline or interruption, **then** setup remains recoverable and CAD works *(1.7.2: after a restart, quit or crash it resumes at the first step without a success fact, and on Windows a one-time per-user entry reopens the app after a restart — Ruling 69, DR-SETUP-6)*; Ready requires detection plus the bundled cavity-lid smoke test at the pinned digest with ~~its Cl inside tolerance~~ *(1.7.2: its final-time mean Courant number inside tolerance, and for OpenFOAM the master banner `Disallowing`, A5.10)*, | Ruling 69 DR-SETUP-2, DR-SETUP-6; AM-SET-1, AM-SET-3 | the resume-from-facts test; the smoke test |
| AM-1.7.2-11 | AI-11 opening (:1242) | `\| AI-11 · The environment assistant proposes, the tool executes \| **Given** Run and a key, **then** every environment step` | \| AI-11 · The environment assistant proposes, the tool executes \| **Given** ~~Run and a key~~ *(1.7.2: setup, with or without a key — the rails decide and need none)*, **then** every environment step | Ruling 69 DR-SETUP-4; AM-SET-2 | the assistant gating tests |
| AM-1.7.2-12 | AI-11 closing (:1242) | `the assistant never receives a shell. \|` | the assistant never receives a shell *(1.7.2: and, given a key, it may (a) explain a step, (b) explain a failed step from its recorded output excerpt, citing the excerpt's lines, (c) answer from the bundled knowledge files, (d) suggest one step id from the set the rails allow at that moment, shown beside the rails' own next step and run only if the user chooses it; it never accepts terms, marks a step done or says Ready; an answer that tells the user to type a command, to disable Gatekeeper, SmartScreen or antivirus, or that claims the setup worked, is withheld with its reason and the step's fixed explanation is shown instead — Ruling 69, DR-SETUP-4)*. \| | Ruling 69 DR-SETUP-4; AM-SET-2; proposal §4.2 | the assistant validator; withheld-answer tests |
| AM-1.7.2-13 | A5.12 Run row (:1138) | `\| Run \| "Prepare my environment" · "Explain this failure" \|` | \| Run \| ~~"Prepare my environment"~~ *(1.7.2: "Ask about this step")* · "Explain this failure" \| | Ruling 69 DR-SETUP-4; AM-SET-4 | the Run AI entry points |
| AM-1.7.2-14 | B1 Run verb row (:1392) | `\| Run \| Check · Prepare my environment · Consent` | \| Run \| Check · ~~Prepare my environment~~ *(1.7.2: Set up a solver)* · Consent | Ruling 69 DR-SETUP-4; AM-SET-4 | the Run menu |
| AM-1.7.2-15 | B-area table, environment row (:1422) | `\| Backend environment, readiness, steps \| Run · environment panel \| Check · Prepare my environment \|` | \| Backend environment, readiness, steps \| Run · environment panel \| Check · ~~Prepare my environment~~ *(1.7.2: Set up a solver)* \| | Ruling 69 DR-SETUP-4; AM-SET-4 | the Run environment panel |
| AM-1.7.2-16 | Flow F7 node K (:1710) | `K[Detection; Prepare my environment: step ids only; parameters bound by the tool]` | K[Detection; Set up a solver, no key needed: step ids only; parameters bound by the tool] | Ruling 69 DR-SETUP-4; AM-SET-4 | the F7 diagram (a label cannot be struck) |
| AM-1.7.2-17 | Copy text, backend not ready (:1852) | `backend — "Backend not ready — <substrate> <version>: <what is missing> · Check · Prepare my environment"` | backend — ~~"Backend not ready — <substrate> <version>: <what is missing> · Check · Prepare my environment"~~ *(1.7.2: "Solver not ready — <what is missing> · Check again · Set up a solver")* | Ruling 69 DR-SETUP-4; AM-SET-4 | COPY-75 consumers |
| AM-1.7.2-18 | C2 row COPY-75 (:2245) | `\| Backend not ready \| "Backend not ready — <substrate> <version>: <what is missing> · Check · Prepare my environment" \|` | \| Backend not ready \| ~~"Backend not ready — <substrate> <version>: <what is missing> · Check · Prepare my environment"~~ *(1.7.2: "Solver not ready — <what is missing> · Check again · Set up a solver"; "substrate" and "digest" leave the user-facing string and stay under Technical details)* \| | Ruling 69 DR-SETUP-4; AM-SET-4 | COPY-75 in DESIGN.md |
| AM-1.7.2-19 | C2 row COPY-76 (:2246) | `\| Environment step \| "Step <n> of <m>: <action> — runs `<command>` · consequence: <text> · terms: <link, verbatim>" \|` | \| Environment step \| ~~"Step <n> of <m>: <action> — runs `<command>` · consequence: <text> · terms: <link, verbatim>"~~ *(1.7.2: "Step <n> of <m>: <action in plain words> · <time> · <download> · <needs: administrator approval · restart · nothing>", with Technical details — the exact command, the pin, the licence — one click away, always present and shown before consent)* \| | Ruling 69 DR-SETUP-5; AM-SET-5 | COPY-76 in DESIGN.md |
| AM-1.7.2-20 | ANA-22 shortcut (:1220) | `(one action: the toggle or a shortcut — the toggle *is* navigation` | (one action: the toggle or ~~a shortcut~~ *(1.7.2: the shortcut ⇧⌘A on macOS, Ctrl+Shift+A on Windows; ⌘1, ⌘2 and ⌘3 stay the Planform, Precision and Review workspaces — Ruling 67, A3a OD-1)* — the toggle *is* navigation | Ruling 67 OD-1 (A3a OD-1); `docs/design/area3-analysis.md` OD-1 (a) | the A3a toggle command; `CommandTable.cs` (⇧⌘A unused at `e8101f3`) |
| AM-1.7.2-21 | C2 table, four new rows (after :2259) | `(a new block of rows after the Step outcome row)` | four rows: Solver ready · Unverified install · Not notarised · Solver not ready, test run failed (the exact strings are in the spec, C2) | Ruling 69 DR-SETUP-3, DR-SEC-1 A; proposal §5 | the four honest-limit strings |
| AM-1.7.2-22 | Revision line (:73) | `Product specification · revision 1.7.1 · 4 October 2026 · *(1.7.1:` | Product specification · revision 1.7.2 · 4 October 2026 · *(1.7.2: guided solver setup, the smoke-test scalar and the CAD ↔ Analysis shortcut, Appendix H, section H.2 and [amendments/spec-1.7.2.md](amendments/spec-1.7.2.md); 1.7.1: | this batch | none |
| AM-1.7.2-23 | Front-matter summary (:55-57) | `  Disallowing (Appendix H.1; amendments/spec-1.7.1.md). ` |   Disallowing (Appendix H.1; amendments/spec-1.7.1.md).   Revision 1.7.2 applies Ruling 69 and Ruling 67 OD-1: one verb, Set up a solver, that works with no key (RUN-01,   AI-11, flow F7a, stories SETUP-01–09); the smoke test reads the final-time mean Courant number, not a Cl that a   cavity cannot have; the Windows default route is OpenFOAM in an app-owned WSL distribution (Inferred until the   Windows run); the exact command moves under Technical details; and the CAD ↔ Analysis shortcut is ⇧⌘A   (Appendix H.2; amendments/spec-1.7.2.md).  | this batch | none |

## Insertions (2)

| ID | Where | What | Source |
|---|---|---|---|
| AM-1.7.2-24 | Part A, A6 stories, after RUN-06 | Stories SETUP-01 to SETUP-09 (nine acceptance criteria, exact text in the spec) | Ruling 69; guided-solver-setup §3.4 |
| AM-1.7.2-25 | Part B, new flow B6b-1, F7a, before B6c | Flow F7a, guided solver setup (F7a-1 to F7a-11) | Ruling 69; guided-solver-setup §3.2 |

The flow diagram and the state table (proposal §3.2 and §3.3) and the setup-assist eval (§4.7) stay in the design and
proposal; see OQ-2.

## Outside the spec (same batch)

- **`docs/design/m12c-section-editor.md` §11.4** said the section handle Length is entered in % c. Spec 1.7 AM-1.7-40
  (Ruling 66 OQ-4) says mm at the station's local chord with % c beside it as a read-only fact, which is what the
  build does. §11.4 now says so, with the source. This is a docs correction, not a product decision.
- **Not changed:** ADR-0012 (a Windows-route amendment waits for the Windows run; see OQ-1); the proposal file keeps
  `status: proposed` history and is cited, not edited.

## Open questions for the owner

Nothing below has a ruling. Each has options and a recommendation; none is applied in 1.7.2.

| ID | Question | Options | Recommendation |
|---|---|---|---|
| OQ-1 | **What pins the Windows route?** The matrix text says every backend is pinned "always by image digest" (:1056). The WSL route installs OpenCFD packages into a distribution image, so there is no OpenFOAM image digest. | (a) pin the distribution image hash plus the `openfoam2512` package version, both recorded in the Backend check fact; (b) keep the digest wording and add a Windows exception later; (c) wait for the Windows run. | **(a)**, decided in an ADR-0012 amendment after the Windows run; the spec wording changes in the next batch. Until then the Windows pin is Not recorded. |
| OQ-2 | **Do the setup state table (proposal §3.3) and the setup-assist eval (§4.7, ten thresholds) enter the spec?** Ruling 69 approved the proposal, which says the rails ship without the assistant if the suite is not green. | (a) both stay in the design and proposal; the eval joins A8.6 when the assistant track starts; (b) add both to the spec now. | **(a).** The state table is a UX test list the design already carries; the eval cases are Flagged until real Windows output exists (proposal §4.7), and AI-06 wants thresholds fixed when the capability is built. |
| OQ-3 | **The smoke-test tolerance for macOS.** Ruling 69 sets it "after the Windows run". Until then Ready on macOS needs a tolerance or none. | (a) until the Windows run, macOS compares to 0.222158 at the six printed digits (same build, same mesh, so the number is deterministic), then one shared relative tolerance; (b) a loose interim tolerance (for example 1 %); (c) files and banner only until the tolerance is set. | **(a).** It uses the one Verified number and invents no tolerance. Label: Inferred that the number repeats on the same build; check by repeating the receipt run. |
| OQ-4 | **Docker Desktop in the licence list (:1342).** Docker is no longer used, so the line about Docker Desktop's organisation-size licence is moot. | (a) leave it, as a record of why Docker was rejected; (b) strike it in place. | **(a)** for now; strike it with the Windows ADR so the licence list and the matrix change together. |
| OQ-5 | **Intel Macs.** The matrix says "macOS arm64/x86", and SETUP-09 (from the approved proposal) says an Intel Mac has no route, because OpenFOAM.app is arm64 and Docker is gone. Ruling 69 does not say that x86 is dropped. | (a) amend the matrix to arm64 only; (b) keep x86 and name its route (none exists today); (c) leave both and mark SETUP-09 conditional. | **(a)**, once the owner confirms the x86 Mac is out of scope. Until then 1.7.2 leaves the matrix text alone and SETUP-09 carries a pointer here. |
