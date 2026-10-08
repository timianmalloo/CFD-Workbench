---
id: proof-wfx2-pc-ring-20261008
title: "WFX2 - Windows ring evidence, 2026-10-08"
type: proof-pack
status: active
owner: "@trk-wfx2"
phase: implementation
tags: [windows, wfx, proof, ring]
links:
  - { to: proof-wfx2-pc-reverify, rel: relates-to }
  - { to: proof-wfx2-red-first, rel: relates-to }
summary: "Evidence-only receipt for one completed Windows test ring; records outcomes, redacted log extracts, and the UI checks the ring cannot assess."
review-by: "2027-11-08"
---

# WFX2 Windows ring receipt

This receipt records one completed Windows ring from the retained files under `.tmp-tests` and the existing WFX2 PC re-check table. It does not claim that the ring passed: the full ring ended with exit code 1 (coordinator-observed). The unredacted run artifacts remain outside this repository.

## Binding and run record

| Item | Evidence and confidence |
| --- | --- |
| Delivery head | Pre-receipt delivery commit: `e2e47765955d23b38e17c2300311330c271cd0b7`, verified as an ancestor of this evidence packet. The evidence commit is reported separately in the handoff. |
| Tested source | `9eef04892566623d0ff9e7cca65b8f82a9934647` (assigned by coordinator; verified as an ancestor of the delivery head). The retained logs themselves do not identify a source SHA. |
| Runner shape | The seven retained log names match the seven jobs configured by `tools/run-tests.sh` at the tested source SHA. The actual invocation command was not retained. The script declares `Release` as its default and accepts `CFD_TEST_CONFIGURATION`; the selected configuration and environment were not retained. |
| Build metadata | SDK `10.0.203`, two Avalonia warnings, zero build errors (coordinator-observed; not present in the retained `.tmp-tests` files). |
| File-derived build duration | `build.ms` contains `47848` ms. |
| File-derived ring duration | `wall.ms` contains `139923` ms. The sidecar values below are the per-job elapsed milliseconds and rounded shell seconds recorded by the runner. |
| Coordinator-observed terminal values | Build 49 s; ring wall 140 s; net 92075 ms; load 10.56 → 13.06; six-CPU affinity. These are terminal observations, not values read from retained files. |
| Result | Six test logs report 39 named failures in total; the Desktop log records an `APP-UNHANDLED APP-CRASH`. The terminal ring exit code was 1 (coordinator-observed). No PASS claim is made for the full ring. |

Per-job timing sidecars, read from the retained files:

| Log/job | `.ms` | `.seconds` |
| --- | ---: | ---: |
| `Analysis.part1of2` | 26530 | 27 |
| `Analysis.part2of2` | 23416 | 24 |
| `Cli` | 11875 | 12 |
| `Core.part1of3` | 76506 | 77 |
| `Core.part2of3` | 67349 | 68 |
| `Core.part3of3` | 85401 | 86 |
| `Desktop` | 18294 | 19 |

The invocation and selected configuration are missing from retained evidence. The runner source establishes its declared default, not which environment was used for this ring. The command and environment must remain unasserted.

## Named WFX2 observations

Each statement below is limited to the named output in [the redacted extracts](evidence-extracts.txt). Source log line numbers are preserved there.

| WFX2 row | Ring evidence and assessment |
| --- | --- |
| 1 — Desktop crash frame | **Observed.** `Desktop.log` records `APP-UNHANDLED APP-CRASH System.Exception`, followed by the message `Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` and a stack frame. This is a crash observation, not a passing Desktop result. |
| 2 — JSON line endings | **Observed pass for both named tests.** `LayoutCodec_IndentedJson_PinsLfNewLine` and `LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9` each have a `PASS` line in the Core logs. |
| 3 — Windows directory-link path | **Mixed observations.** `PrefStore_SymlinkedDirectory_SessionOnly` and `PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites` each have a `PASS` line. `PrefStore_DirectoryLink_WindowsBranchUsesJunction` has a `FAIL` line whose recorded exception says `A required privilege is not held by the client.` No cause beyond that message is inferred. |
| 4 — Catalog refusal detail | **Observed, with differing output detail.** Five Core failure lines name `[check=generated-bytes; detail=entry naca-0009: lengths 6686 vs generated 6686, first differing byte 727]`. `NeuralFoil_Family_CatalogNaca0012_DerivedNotLabelled` also fails in Analysis, but that log line contains no check/detail text. The extracts preserve every named catalog failure. |
| 5a — F10, 5b — Escape focus, 5c — Undo status text | **Not assessed by this ring.** These are manual UI observations in the PC re-check table and no such interaction is recorded in the seven logs. Manual UI checks remain required. |
| 6 — Imperial spanwise table | **Automated evidence only.** `SectionForce_Imperial_ForceAndMomentPerSpan`, `Projection_DragWingOnly_OneRow_ImperialLbf`, and `Units_Imperial_NoAnalysisRowKeepsNewtonsPerMetre` each have `PASS` lines. The logs do not record opening the Windows Analysis table or reading its rendered header/cells; that display value is not claimed as verified. |

## All named failures

The seven logs contain 39 named failing tests: 2 Analysis, 1 CLI, and 36 Core. The Desktop log has an unhandled process crash but does not emit a named `FAIL` test line. The crash stopped the Desktop harness at stage `native-review-options`, so every Desktop check after that point is unassessed in this ring, not passed (added by the Mac at the join, Ruling 154 condition 2; the crash is now a reported FAIL that lets the harness continue, track WRT). The exact failure lines and each log's reported `RESULT failures=` line are in the extracts. They are reproduced without root-cause interpretation.

## Evidence limits and redaction

- `evidence-extracts.txt` is a line-numbered extraction of failure/result lines and the WFX2-specific pass/crash lines from all seven logs. The repository path is replaced by `%REPO%`; this does not change diagnostic text.
- No full unrelated logs, temporary test payloads, user-profile paths, machine identifiers, or host names are included.
- SDK and build-warning metadata above are coordinator-observed only; no retained build output was present in the supplied `.tmp-tests` set.
- The ring result remains failed. Passing individual checks do not upgrade the whole ring to PASS.
- No tests, builds, or docs gates were run to produce this receipt.
