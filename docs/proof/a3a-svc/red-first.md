---
id: proof-a3a-svc-red-first
title: "A3a SVC red-first receipt"
type: proof-pack
status: active
owner: "@track-a3a-svc"
phase: implementation
tags: [a3a, svc, analysis, freshness, cli, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
  - { to: proof-a3a-sto-red-first, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  Each of the 16 SVC checks of design §18.8 observed red against its named mutant (23 red lines), including the SVC
  exit mutant: the current key built from the run's own stored inputs turns all eight freshness checks red.
---

# A3a SVC red-first receipt

Design `docs/design/area3-analysis.md` §18.2 (SVC row), §18.8; BC-1 for the ANA-07 rows. Session `track-svc`, branch
`feature/a3a-svc`, 2026-10-04. The checks and the code landed in one commit (`667dced`); before it every SVC body
threw `NotImplementedException("SVC: …")`, so each check was red at the base, but that red proves nothing about the
check. The proof is the mutant: each was planted in the committed tree at `667dced`, the owning project rebuilt, the
check selected with `CFD_TEST_ONLY` (the Cli harness runs whole), and the file restored (`git status` clean after each
batch). Mutants in `RunRecord.cs` and `AuthoringSession.cs` (STO's files) were planted transiently for the receipt
only; nothing outside the SVC row was committed. The service checks run against a fake wing method (`FakeWing`):
they are about the service, not the lattice numbers.

| Check | Ring | Mutant | Red line |
|---|---|---|---|
| `Analysis_DraftOpen_EvaluatesAcceptedRevision` | A | the service reads `Draft.Bytes` when a draft is open (sections from the draft) | `the lattice read the accepted sections; expected True; actual False` |
| `Freshness_SurfaceEdit_Historical` | A | `SurfaceHash` left out of the key | `after a twist edit expected Historical; actual Current` |
| `Freshness_ProfileEdit_Historical` | A | the current key built from the run's own stored inputs | `after a profile edit expected Historical; actual Current` |
| `Freshness_WaterChange_Historical` | A | water left out of the key | `fresh → salt at 15 °C expected Historical; actual Current` |
| `Freshness_OperatingPointChange_Historical` | A | α rounded to 0.1° before hashing | `α 3.00° → 3.01° expected Historical; actual Current` |
| `Freshness_MethodVersionBump_Historical` | A | the method version left out of the key | `1.0.0 → 1.0.1 expected Historical; actual Current` |
| `Freshness_EachSettingsField_Historical` | A | `wakeDirection` left out of `settingsHash` | `WakeDirection changed alone: expected Historical; actual Current` |
| `Freshness_UndoToEqualKey_CurrentAgain` | A | freshness stored as a flag (the first verdict per run kept) | `after Undo expected Current; actual Historical` |
| `Freshness_SaveReopen_Unchanged` | A | the reader drops the `analysis` member's runs on reopen | `Sequence contains no matching element` |
| `Units_Lbf_KeyUnchanged` | A | units inside settings (`RunSettings.DisplayUnits`) | `RunSettings.DisplayUnits carries units into the key: expected False; actual True` |
| `Evaluate_Supersede_OlderCancelledViaBarrier` | A | no generation check (the older is not cancelled and records) | `expected OperationCanceledException; the evaluation returned` |
| `Evaluate_Cancel_NoRowRecorded` | A | a cancelled run recorded as Failed (`ANA-CANCELLED` row) | `expected OperationCanceledException; the evaluation returned` |
| `Evaluate_CloseMidCompute_DocClosedNoRow` | A | `RecordRun`'s closed guard skipped (returns silently when closed) | `expected ContractError; the evaluation returned` |
| `OperatingPoint_SpeedZeroOrNegative_Undefined` | A | \|V\| used in validation and derivation | `q at V = -0.5144444444444445 expected SpeedNotPositive; actual` (empty) |
| `Telemetry_AnalysisRun_EmittedWithSubDurations` | A | `solveMs` written as 0 when not reached | `solveMs not reached expected ; actual 0` |
| `Cli_AnalyseRunKey_EqualsGui` | Cli | the CLI defaults one setting differently (p_atm 101300 instead of the shared builder) | `FAIL Cli_AnalyseRunKey_EqualsGui` (CLI key differs from the GUI key) |

**SVC exit mutant** (§18.2: "the current key built from the run's own stored inputs → every freshness test red"),
planted in `Freshness.State`: all eight `Freshness_*` checks red — SurfaceEdit, ProfileEdit, WaterChange,
OperatingPointChange, MethodVersionBump, EachSettingsField (`NSpanPerHalf changed alone: expected Historical; actual
Current`), UndoToEqualKey (`after the edit expected Historical; actual Current`) and SaveReopen (`before save expected
Current,Historical; actual Current,Current`).

**Measured cost** (COST lines, warm harness, this machine): the 15 Analysis-ring SVC checks total about 2.3 s
(52–510 ms each). Each check opens a session from one cached image (`Reopen`, about 50 ms) — the floor of every check
that needs a document. `Freshness_ProfileEdit_Historical` (about 0.5 s) pays one section assessment. The Cli check is
about 0.17 s. Every ring-A SVC check is above its §18.8 estimate (< 5 ms to < 50 ms); the estimates assumed no
session open.
