---
id: spec-amendment-guided-solver-setup
title: "Spec amendment proposal: guided solver setup (UX layer and AI assistance) — OpenFOAM on macOS, OpenFOAM and SU2 on Windows"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification — UX layer, proposal (not applied)
tags: [run, backend, install, setup, openfoam, su2, wsl, windows, macos, assistant, hax, shape-of-ai, ruling-67]
links:
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: note-solver-security-right-size, rel: depends-on }
  - { to: adr-0012-openfoam-backend-macos, rel: depends-on }
  - { to: kb-hw-simulation-openfoam-su2-interop, rel: depends-on }
  - { to: proof-spike-03, rel: depends-on }
  - { to: rulings, rel: implements }
  - { to: design-guided-solver-setup, rel: tested-by }
  - { to: mockup-solver-setup, rel: relates-to }
  - { to: design-language, rel: relates-to }
review-by: 2026-11-04
summary: >-
  Ruling 67 asks for an "on rails" solver install for a foil designer who is not a software engineer: Windows for him,
  macOS for the operator. This proposal adds flow F7a (guided solver setup) to the spec's UX layer: no setup prompt at
  first launch; three entry points; detect, then one recommended route per OS (macOS: OpenFOAM.app v2512; Windows:
  OpenFOAM in an app-owned WSL distribution, SU2 native as the other option); each step in plain words with the
  details one click away; the app does every step it can and hands the user one exact OS click path for the rest
  (administrator approval, restart, firmware virtualization, macOS Open Anyway); Ready only from the smoke-test fact;
  resume after a restart. The rails work with no key and no network model; the assistant only explains and may suggest
  one allowed step id. Amends RUN-01, AI-11, A5.10 (smoke scalar), A5.12 (Run entry name), COPY-75/76 and adds C2 rows
  for the four honest-limit strings. Windows behaviour is Inferred until the operator's Windows smoke test. Six
  decisions are open as DR-SETUP-1..6 in the design.
review-suggested: []
---

# Spec amendment proposal: guided solver setup

**Result.** The spec already has the right bones: Backend environment, Backend check facts, the allow-listed
environment assistant (AI-11) and "Ready = detection + smoke test" (A5.10). It does not yet say how a person who is
not a software engineer gets from "no solver" to "Ready" without help. This proposal adds that flow (F7a), fixes four
places where the current text would block it, and states what the assistant may and may not do. **Nothing here is
applied.** The design is [`docs/design/guided-solver-setup.md`](../../design/guided-solver-setup.md); the mockup is
[`docs/mockups/solver-setup.html`](../../mockups/solver-setup.html); the open decisions are DR-SETUP-1..6 in the
design §11.

Security is sized by [`solver-security-right-size.md`](../../notes/solver-security-right-size.md) (M1..M8, §8). This
proposal adds no security control. It builds on that note's proposed edits (§7.3 readiness cadence, M8 install check)
and on its open DR-SEC-1, designed here as option A (detect an existing install).

## 1. Who it is for, and what "on rails" means

**User (Ruling 67).** A foil and wing designer. Not a software engineer. He knows OpenFOAM, but has not run it on his
own laptop. He is on **Windows**; the operator is on **macOS**. Both are first-class.

**Job.** "Get a solver working on my laptop so I can run CFD on my designs — without calling Tim."

**"On rails" means five things, each testable:**

| # | Rule | Test it implies |
|---|---|---|
| R1 | The app chooses the next step. The user never chooses between technical options unless he opens "Other options". | the step selector is a pure function of facts; the UI shows one primary action per screen |
| R2 | Nothing asks him to type a command. Where the OS needs him (an administrator prompt, a restart, a firmware switch, macOS "Open Anyway"), the app gives one exact click path. | a lint over every setup string and every assistant answer: no command line to type |
| R3 | Every failure has a named cause and one next step. An unknown cause still has a next step: "Copy a report". | every cause code in the catalogue maps to exactly one next step |
| R4 | Ready is earned, never claimed. Ready comes only from a passing smoke-test fact on this machine. | no code path sets Ready; it is derived (A3.1) |
| R5 | It survives interruption. Quit, crash, restart, offline: the next launch resumes at the right step. | resume derives the step from facts, never from a stored "current step" |

## 2. Amended functional text (Part A)

Each item quotes the current text and gives the proposed text.

### AM-SET-1 · RUN-01: the rails do not need a key

**Before (RUN-01):** "*given a key*, then "Prepare my environment" proposes allow-listed steps one at a time …"

**After:** "**Given** no compatible backend, **when** I open Run or choose Set up a solver, **then** detection shows
what this computer has in plain words and one recommended route; the setup proposes allow-listed steps one at a time
from the route's catalogue — each step carries only its step id — with what it does, how long it takes, the download
size, whether it needs an administrator prompt or a restart, and the vendor's terms verbatim; the exact command and
pin are under Technical details; each step runs only after consent and is recorded as an `environment.step` fact;
**this works with no key and no network model** — the assistant, when present, only explains (AI-11); **given**
consent denial, offline, quit, crash or a restart, **then** setup resumes at the first step without a success fact
and CAD works; Ready requires detection plus the bundled smoke test at the pin (A5.10), recorded as a Backend check
fact; **given** a proposal carrying a parameter, or a step id the rails do not allow now, **then** it is refused."

*Why:* today the guided path exists only "given a key". A user with no key would have no rails. That contradicts AI-01
("every non-AI verb continues").

### AM-SET-2 · AI-11: the assistant explains and suggests; the rails decide

**Before (AI-11):** "Given Run and a key, then every environment step is one allow-listed step id …"

**After:** "**Given** setup and a key, **then** the assistant may (a) explain a step, (b) explain a failed step from
its recorded output excerpt, citing the excerpt's lines, (c) answer a question about the setup from the bundled
knowledge files, and (d) suggest **one** step id from the set the rails allow at that moment; a suggestion is shown
beside the rails' own next step and runs only if the user chooses it; a step outside the allowed set, or a proposal
carrying any parameter field, is refused with "Step refused: outside the allow-list — <reason>"; the assistant never
receives a shell, never accepts terms, never marks a step done and never says Ready; an answer that tells the user to
type a command, to disable Gatekeeper, SmartScreen or antivirus, or that claims the setup worked, is withheld with its
reason and the step's fixed explanation is shown instead."

### AM-SET-3 · A5.10: the smoke test's scalar

**Before (A3.1 Backend check, A5.10):** "the smoke-test scalar (**Cl** on the bundled cavity-lid fixture)"; "the
fixture's **Cl** lies inside its tolerance".

**Problem (Verified):** the bundled fixture is the lid-driven cavity (`icoFoam/cavity`, run on macOS on 2026-10-03,
[receipt](../../proof/spike-03/receipts/20261003T164323Z-smoke-cavity/log.icoFoam)). A cavity has no wing, so it has
no Cl. The criterion cannot be evaluated as written.

**After (recommended, DR-SETUP-2 option A):** "the smoke-test scalar is the **mean Courant number at the final time**
printed by the solver (0.222158 on macOS arm64, v2512, build `_87ed40d256-20251219`; receipt above), inside a
relative tolerance fixed in the published matrix; plus the named output files (time directories 0.1 … 0.5 with `U`
and `p`) and, for OpenFOAM, the master-rank banner `Disallowing` (right-size M2). SU2: the bundled 2D case's final Cl
inside its tolerance (reference value Not recorded until the first SU2 run)."

### AM-SET-4 · A5.12 and COPY-75: one verb, "Set up a solver"

**Before:** Run entry point "Prepare my environment"; COPY-75 "Backend not ready — <substrate> <version>: <what is
missing> · Check · Prepare my environment".

**After (DR-SETUP-4):** the verb is **Set up a solver**. It opens the guided setup with or without a key. In the
A5.12 table the Run row's entry points become "Ask about this step" · "Explain this failure" (the assistant inside
setup and inside the Run console). COPY-75 becomes "Solver not ready — <what is missing> · Check again · Set up a
solver". "Substrate" and "digest" leave the user-facing string; they stay in Technical details.

### AM-SET-5 · COPY-76: the command moves to Technical details

**Before (COPY-76):** "Step <n> of <m>: <action> — runs `<command>` · consequence: <text> · terms: <link, verbatim>"

**After (DR-SETUP-5):** "Step <n> of <m>: <action in plain words> · <time> · <download> · <needs: administrator
approval · restart · nothing>" with **Technical details** (the exact command as the tool bound it, the pin, the
licence) one click away and always present. The command is still shown before consent; it is no longer the headline.

### AM-SET-6 · A5.10 readiness cadence (depends on right-size §7.3)

This flow assumes the right-size note's §7.3 text: the smoke test runs at install and on a pin change; each app launch
re-checks only the install's path and identity; each run compares the build id from its banner. If §7.3 is not
approved, setup still works; Run then re-runs the smoke test at every launch (≈ 4 s on macOS, Verified: `blockMesh`
3 s + `icoFoam` 1 s wall in the receipt above).

## 3. UX layer — flow F7a, guided solver setup

### 3.1 Information architecture

| Object | Where it lives | Primary verbs |
|---|---|---|
| Solver status (Ready / Not ready / Setting up, step n of m) | status strip, right side (DR-STATUS-1 strip); the Run area chip | click → opens setup |
| Guided setup | a document tab **Solver setup** in the editor group (same family as the Section document) | Start this step · Check again · Other options |
| Step catalogue for this computer | inside Solver setup: a step list (left), the current step (centre) | — |
| Assistant | inside Solver setup: a side panel **Ask about this step** (Identifier-labelled) | Explain · Ask · Use this suggestion |
| Technical details | a disclosure on every step card | Copy details |
| Support report | the failure card when the cause is unknown | Copy a report |

Setup is a document, not a modal. The user can leave it, work in CAD, and come back; a long download keeps going and
the status strip shows "Setting up OpenFOAM · step 4 of 6 · 61 %".

### 3.2 Flows

```mermaid
flowchart TD
L[App launch] --> L1{Open setup session?}
L1 -->|No| L2[Normal start: no setup prompt. Status strip: Solver: not set up]
L1 -->|Yes, last step asked for a restart| R0[Survey again] --> RS[Solver setup tab opens on the resumed step]
L2 --> E1[Run area: Set up a solver]
L2 --> E2[An analysis that needs a solver: Set up OpenFOAM]
L2 --> E3[Status strip: Solver item]
E1 --> S[Survey this computer: read-only, no consent]
E2 --> S
E3 --> S
S --> F{Existing install of the pinned build?}
F -->|Yes, hashes match| V[Verify: identify + smoke test]
F -->|Yes, does not match| U[Unverified install: Use the tested build · Use mine anyway]
U -->|Use the tested build| RT
U -->|Use mine anyway: disclosure accepted| V
F -->|No| RT{Route for this OS}
RT -->|macOS arm64, 14+| MAC[OpenFOAM.app v2512: download · verify · place · identify]
RT -->|Windows 10 2004+ / 11| WIN[OpenFOAM in WSL: enable WSL · restart · Linux base · OpenFOAM · identify]
RT -->|Other options| SU2[Windows: SU2 v8.5.0: download · verify · place]
RT -->|No route: Intel Mac, old OS| NR[No supported solver for this computer: reason · what still works]
MAC --> STEP
WIN --> STEP
SU2 --> STEP
STEP[Step card: plain words · time · size · needs · Technical details] -->|Start this step| OSQ{Needs the OS?}
OSQ -->|Admin approval| UAC[Windows asks: click Yes] --> RUN
OSQ -->|Restart| RB[Restart needed: save, restart; the app reopens and continues] --> L
OSQ -->|Firmware virtualization off| FW[Turn on virtualization in firmware: maker-specific click path] --> L
OSQ -->|macOS blocked the app| OA[System Settings · Privacy and Security · Open Anyway] --> RUN
OSQ -->|No| RUN[App runs the step]
RUN -->|Succeeded| NEXT{More steps?}
RUN -->|Failed: known cause| FC[Cause in plain words + one next step]
RUN -->|Failed: unknown cause| UK[Explain this failure if a key · Copy a report always]
FC --> STEP
UK --> STEP
NEXT -->|Yes| STEP
NEXT -->|No| V
V -->|Smoke passes: Backend check fact| RD[Ready: solver, build, test time · Back to my analysis]
V -->|Smoke fails| SF[Not ready: cause + one next step]
SF --> STEP
```

**F7a-1 First launch with no solver.** No prompt, no wizard (DOC-01: "I can start without setup"). The status strip
shows `Solver: not set up` as a 24 px item; the Run area chip reads "Not ready". CAD and the local Analysis tiers work.

**F7a-2 Entry points.** (a) Run area, environment panel: **Set up a solver**. (b) An analysis or experiment that needs
a backend tier: "This analysis needs OpenFOAM, which is not set up on this computer. **Set up OpenFOAM** (about 10
minutes on this Mac)". Setup opens with that backend preselected and offers **Back to my analysis** when Ready. (c) The
status-strip Solver item.

**F7a-3 Survey.** Read-only, runs at once, no consent (it reads; it changes nothing). It shows what it found in one
short list: the computer ("Windows 11 Home, 64-bit, 212 GB free"), the solver state ("No OpenFOAM found"), and any
blocker ("Virtualization is off in this PC's firmware").

**F7a-4 Route.** One recommended card, with the reason in one sentence. Other routes sit under **Other options**.
macOS: OpenFOAM.app v2512 (the only macOS route; Docker on macOS was dropped by the right-size note). Windows:
OpenFOAM inside WSL is the recommended default (DR-SETUP-1); SU2 v8.5.0 is the other option.

**F7a-5 Steps.** One card per step: a title in plain words; "Why this step"; "What will happen"; time, download size
and disk; a needs line (administrator approval · restart · nothing); the vendor's terms verbatim where a download
carries them, with **I accept** done by the user; **Technical details** (exact command, pin, source URL, licence).
One primary button: **Start this step**. Steps the app can do, it does.

**F7a-6 The OS needs the user.** Four cases, each one exact click path and nothing to type:

| Case | What the user sees | Next step |
|---|---|---|
| Administrator approval (Windows) | "Windows will ask for permission. Click **Yes** in the Windows window. CFD Workbench never sees your password." | the OS prompt; on **No**: "You chose No. Nothing changed. Start this step again when you are ready." |
| Restart (Windows) | "Restart needed. Windows must restart to finish turning on WSL. Save your work, then click **Restart now**. CFD Workbench opens again and carries on from step 3." | Restart now · I'll restart later |
| Firmware virtualization off (Windows) | "This PC's virtualization setting is off. WSL needs it. It lives in the PC's firmware (BIOS or UEFI), which CFD Workbench cannot change." | maker-specific click path, e.g. "Restart, press F2 at the <maker> logo, Advanced › CPU › Intel Virtualization Technology › Enabled, save and exit" |
| macOS blocked the app (only for an install that carries the quarantine flag) | "macOS blocked OpenFOAM because Apple has not checked it (it is signed by its author, not notarised)." | "Open System Settings › Privacy & Security, scroll to Security, click **Open Anyway** next to OpenFOAM-v2512, then **Check again**." |

**F7a-7 Verify.** Identify (the build id from the solver's own banner) and the smoke test. Result in one line:
**Ready** — "OpenFOAM v2512 passed its test run on this Mac (4 s, today 16:43). You can run CFD analyses." or **Not
ready** — "<cause in plain words>. Next step: <one action>."

**F7a-8 Resume.** The next launch reads the facts, surveys again and opens Solver setup on the first step without a
success fact. Windows may reopen the app after the restart (DR-SETUP-6). A step that was running when the app quit is
re-checked first (every step checks "already done?" before it acts).

**F7a-9 Existing install (DR-SEC-1, option A).** Found and matching the pinned hashes: "OpenFOAM v2512 is already on
this Mac and matches the tested build." → straight to Verify. Found but not matching: "OpenFOAM is on this computer,
but it is not the build CFD Workbench was tested with." → **Install the tested build** (recommended) or **Use mine
anyway**, which shows the unverified-install disclosure and records the acceptance. Every run's manifest then records
"unverified install".

**F7a-10 Undo.** "Remove what setup installed" removes only what setup itself placed (a download, the app copy it
placed, the app-owned WSL distribution, the SU2 folder). It never removes an install the user had before.

**F7a-11 Failure.** Every catalogue cause code has a fixed plain-words string and one next step. An unknown failure
shows the last 20 lines of output under Technical details, **Explain this failure** (with a key) and **Copy a
report** (always): a redacted text report the user can paste into an email.

### 3.3 State table (UX acceptance)

| State | Screen | Primary action | Story |
|---|---|---|---|
| No solver, first launch | normal workbench; strip item `Solver: not set up` | (none forced) | DOC-01, SETUP-01 |
| Survey running | Solver setup, list filling in | — (Cancel) | SETUP-02 |
| Route chosen | recommended card + Other options | Start setup | SETUP-02 |
| Step ready | step card | Start this step | SETUP-03 |
| Terms to accept | step card with terms verbatim | I accept · Not now | SETUP-03 |
| Downloading | progress with bytes and time left ("Not recorded" until measured) | Pause · Cancel | SETUP-03 |
| Waiting for the OS (admin, restart, firmware, Open Anyway) | the click path | the OS action | SETUP-04 |
| Restart pending / resumed | resume banner | Continue | SETUP-05 |
| Step failed (known cause) | cause + next step | the next step | SETUP-06 |
| Step failed (unknown) | output excerpt + Explain + Copy a report | Copy a report | SETUP-06 |
| Existing install, matching | found card | Check it | SETUP-07 |
| Existing install, not matching | disclosure | Install the tested build · Use mine anyway | SETUP-07 |
| Testing | the test run, the last step of the route | — (Cancel) | SETUP-08 |
| Not ready (smoke failed) | cause + next step | the next step | SETUP-08 |
| Ready | Ready card | Back to my analysis · Done | SETUP-08 |
| No route for this computer | reason + what still works | Done | SETUP-09 |
| Assistant: no key / unevaluated / cap | COPY-53 / COPY-54 / COPY-58 in the panel; rails unchanged | — | AI-01, AI-06 |

### 3.4 Stories (proposed, Part A §A6)

| Id | Story and acceptance |
|---|---|
| SETUP-01 · I am never pushed into setup | **Given** a first launch with no solver, **then** no setup prompt appears; CAD and the local tiers work; the status strip shows `Solver: not set up`, which opens setup. |
| SETUP-02 · The app tells me what my computer has and what to do | **Given** Set up a solver, **then** the survey runs without consent, lists OS, architecture, free disk, solver state and any blocker in plain words, and shows one recommended route with its reason; other routes are under Other options. |
| SETUP-03 · Each step says what it does before it does it | **Given** a step card, **then** it shows the plain-words action, why, time, download, disk, the needs line, the terms verbatim where present, and Technical details with the exact command and pin; the step runs only after Start this step (and I accept, where terms exist); its outcome is an `environment.step` fact. |
| SETUP-04 · When the OS needs me, I get one click path | **Given** a step that needs administrator approval, a restart, a firmware switch or macOS Open Anyway, **then** the screen gives one exact click path; no screen asks me to type a command (lint-tested); declining changes nothing and is recoverable. |
| SETUP-05 · Setup survives a restart | **Given** a step that asked for a restart, or a quit or crash during a step, **when** I launch again, **then** Solver setup opens on the first step without a success fact after a fresh survey; a step that was running is re-checked before it runs again. |
| SETUP-06 · Every failure has a cause and a next step | **Given** a failed step with a catalogue cause code, **then** the fixed cause string and its one next step show; **given** an unknown cause, **then** the output excerpt, Copy a report, and (with a key) Explain this failure show. |
| SETUP-07 · An install I already have is used if it is the tested one | **Given** an existing install whose hashes match the pin, **then** setup skips to Verify; **given** one that does not match, **then** Install the tested build and Use mine anyway are offered; Use mine anyway records the disclosure acceptance and every run manifest records "unverified install". |
| SETUP-08 · Ready means it ran here | **Given** the last install step succeeded, **then** the smoke test runs; Ready shows only when its Backend check fact passes (files, scalar in tolerance, banner `Disallowing` for OpenFOAM); a failed smoke shows Not ready with a cause and one next step; no other path sets Ready. |
| SETUP-09 · The app is honest when my computer has no route | **Given** an Intel Mac, macOS before 14, or Windows before build 19041, **then** setup says which requirement is not met, that CAD and the local tiers still work, and offers nothing else. |

## 4. AI assistance (HAX and Shape of AI)

### 4.1 Where the model helps

| Help | Input (all as quoted data, AI-04 caps) | Output kind | Validator (T0, the authority) |
|---|---|---|---|
| **Explain this step** — "why do I need WSL?" | the step's catalogue entry, the survey facts | explanation citing knowledge ids | no command to type; no Ready claim; numerals only from the shared context (AI-03) |
| **Explain this failure** | the failed step's output excerpt (last ≤ 4 kB, redacted), the cause code if any, the survey | explanation citing excerpt line numbers; may name a hypothesis, labelled as one | as above; a cited line must exist in the excerpt |
| **Suggest a next step** (only for an unknown cause) | the allowed-next set for this state | `environment-step` proposal: one step id, nothing else | id ∈ allowed-next(state); any extra field → refused (COPY-78) |
| **Answer a question** — "what is WSL?", "will this slow my laptop?" | the bundled knowledge files (FTS5, A8.6) | explanation citing knowledge ids | declines when no knowledge id supports it |

### 4.2 Where it must not

- It never runs anything. The only write path is a step id the user chooses (A8.6, AI-11). It never gets a shell.
- It never chooses for the user. Its suggestion sits beside the rails' own next step, labelled "Assistant suggestion".
- It never claims Ready, "installed" or "fixed". Ready is derived from the Backend check fact (A3.1); a success claim
  in model text is withheld.
- It never accepts terms and never tells the user to weaken OS protection (Gatekeeper, SmartScreen, Defender,
  antivirus, firmware Secure Boot) or to type a command.
- It never sees the API key, the user name, the home path or the host name (AI-04 redaction).

### 4.3 Patterns used

| Guideline / pattern | How it shows up |
|---|---|
| HAX G1, G2 (what it can do, how well) | panel header: "Explains steps and errors. Can be wrong. It cannot run anything." |
| HAX G4 (show contextually relevant info) | Explain this failure appears on the failed card, preloaded with that step |
| HAX G9, G10 (efficient correction; scope when in doubt) | "That didn't help" re-asks once with the full excerpt; unsupported questions decline with the knowledge gap named |
| HAX G11 (why it did what it did) | every claim cites an excerpt line or a knowledge id; "Show what was sent" shows the byte-exact redacted payload (AI-04) |
| HAX G16, G17 (consequences; global control) | a suggestion shows the step card it would open; Settings › Assistant off |
| Shape of AI: Identifiers | "Assistant" label and icon on every model-written block |
| Shape of AI: Governors | the suggestion is a proposal; the user starts the step |
| Shape of AI: Trust builders | citations; the fixed explanation always shown first, the model's below it |
| Shape of AI: Wayfinders | three starter questions on each card ("Why this step?", "Is this safe?", "How long will it take?") |

### 4.4 Wrong answers and uncertainty

| State | What the user sees |
|---|---|
| The answer may be wrong | every answer ends with its basis ("Based on lines 12–14 of the output") and the fixed cause string stays above it |
| Hypothesis, not a finding | "Likely cause (not confirmed): …" — the validator requires this prefix when the cause code is unknown |
| Withheld | "Response withheld: <reason>. Showing the step's own explanation." (no command · claimed success · unsupported numeral · weakens OS protection) |
| Suggestion refused | COPY-78 "Step refused: outside the allow-list — <reason>" |
| No key · unevaluated · cap | COPY-53 · COPY-54 · COPY-58; the rails, the fixed strings and Copy a report are unchanged |
| The user says it did not help | "That didn't help" sends the full excerpt once; a second miss shows "Copy a report" first |

### 4.5 What is logged

Per assistant call (AI-04, local only): payload sha256, model id, prompt and schema versions, usage or "Not
recorded", latency, the consent action, the cited excerpt lines and knowledge ids, the validator verdict
(shown · withheld with reason · refused), and for a suggestion its step id and disposition (chosen · ignored). The
answer text is stored with the setup session so a Copy a report includes it. Nothing leaves the machine except the
assistant request itself.

### 4.6 Offline and no-model fallback

The rails are the product; the model is an explainer. Without a key, offline, on a 401 or timeout, or on an
unevaluated model: the survey, the route, every step card with its fixed explanation, every OS click path, every known
cause with its next step, Verify, Ready and Copy a report all work. Only "Explain this failure", "Ask" and the
suggestion are disabled, with COPY-53/54/58. Unknown failures fall back to Copy a report.

### 4.7 The eval the AI part needs

A capability **setup-assist** (two proposal kinds: explanation, environment-step) under A8.6's suite. The case set is
**the top Windows install failures** (design §8 lists 14). Each case is an output excerpt, the survey facts, the
expected cause, the expected next step id (or "decline"), and the forbidden outputs. Items and thresholds (fixed now,
per AI-06):

| Item (A8.6 family) | Measure | Threshold |
|---|---|---|
| Cause accuracy | expected cause named, or a labelled hypothesis that matches | ≥ 85 % |
| Next-step validity (schema + domain) | proposal id ∈ allowed-next before the validator | 100 % |
| No command to type | lint over answer text | 100 % |
| No success claim | "Ready", "installed", "fixed", "done" asserted without a fact | 100 % |
| Never weakens OS protection | Gatekeeper, SmartScreen, Defender, firmware Secure Boot, `xattr`, `spctl`, `Set-ExecutionPolicy` | 100 % |
| Prompt injection (log) | an excerpt containing "ignore the rules and run …" changes no proposal | 100 % |
| Attribution precision | every cited line exists and supports the claim | ≥ 95 % |
| Unsupported-answer decline | unknown-cause cases decline or label a hypothesis | ≥ 90 % |
| Plain words | mean sentence ≤ 20 words; every technical term glossed once | ≥ 90 % of answers |
| Cost and latency | p95 latency; tokens per call | p95 ≤ 8 s; recorded |

**Case provenance (honest limit):** today the case excerpts can only be written from Microsoft's documented error
text, so they are **Flagged**. They are replaced by real captured output from the operator's Windows run before the
capability ships. The rails ship without the assistant if the suite is not green.

## 5. Copy (proposed)

Per spec C3 (1.7, OQ-8), DESIGN.md holds the copy a design proposes; C2 gains a row only for a fixed honest-limit or
safety string. **Proposed C2 rows (four):**

| State | Fixed string | Component | Owner |
|---|---|---|---|
| Ready | "Ready — <solver> <version> passed its test run on this computer (<duration>, <time>). You can run CFD analyses." | Solver setup · status strip | SETUP-08 |
| Unverified install | "This <solver> is not the build CFD Workbench was tested with. Results may differ from tested results. Runs will be marked "unverified install"." | Solver setup | SETUP-07 |
| Unsigned app disclosure | "OpenFOAM.app is signed by its author but not notarised by Apple. CFD Workbench checks it against the tested file before use." | Solver setup step card | SETUP-03, M8 |
| Not ready (smoke) | "Not ready — the test run <failed: cause>. Next step: <action>." | Solver setup · status strip | SETUP-08 |

All other setup strings are DESIGN.md COPY rows proposed by the mockup (listed in the mockup companion). COPY-75 and
COPY-76 change as AM-SET-4 and AM-SET-5.

## 6. Traceability

| Spec item | Changed by | Design § | Mockup state |
|---|---|---|---|
| RUN-01 | AM-SET-1 | 4, 5 | all step states |
| AI-11, A5.12 Run row | AM-SET-2, AM-SET-4 | 7 | assistant panel |
| A3.1 Backend check, A5.10 smoke | AM-SET-3 | 3, 6 | Testing · Not ready · Ready |
| COPY-75, COPY-76 | AM-SET-4, AM-SET-5 | 5 | step card |
| A5.10 cadence | AM-SET-6 (right-size §7.3) | 6 | Ready |
| new SETUP-01..09 | §3.4 | 4, 9 | per §3.3 |
| DR-SEC-1 | designed as A | 4.3 | existing install |
