---
id: proof-r163-windows-ring
title: "Ruling 163 P5 Windows ring (incomplete at 60 seconds)"
type: proof-pack
status: blocked
owner: "@win-r163-ring"
tags: [windows, ruling-163, ruling-156, determinism, test-ring]
links:
  - {to: review-pr-13, rel: relates-to}
  - {to: proof-rwf-red-first, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: "2026-11-08"
summary: >-
  One six-CPU Windows ring on main 77e53062 built successfully, and all nine catalog test rows passed in partial Core logs.
  The outer 60-second cap stopped the ring before every harness completed, so Ruling 163 P5 remains unverified.
---

# Ruling 163 P5 — Windows ring

**Goal:** measure the current Windows ring after PR #13 on main `77e53062df26ded482df6d35c27e83f3efabbb3a`.
**Done when:** report the catalog class-(d) checks and the RWF cross-OS `DRIFT` output from one ring attempt.
**Not in scope:** product, test, or tool edits; changing solver inputs; a second ring attempt.
**Tier:** T1. **Fan-out:** 0.

## Run

The single invocation used the Windows user SDK, verified before launch as .NET SDK 10.0.203:

```text
DOTNET_ROOT=%USERPROFILE%\.dotnet
PATH begins with %USERPROFILE%\.dotnet
C:\Program Files\Git\bin\bash.exe --noprofile --norc tools/run-tests.sh
Process affinity mask: 0x3F (six logical processors)
Outer timeout: 60 seconds; on timeout, taskkill /T /F stopped the process tree.
```

`run.json` records the tested HEAD, UTC start/end, elapsed time, selected SDK, affinity, outer status, and exit. The runner
stdout/stderr are in `run.stdout.txt` and `run.stderr.txt`; every `.tmp-tests` log and timing file present at stop is copied
under `raw-suites/`. `capture-manifest.json` declares each captured file's SHA-256 and byte count. After commit and before
delivery, run `py -3 docs/proof/r163-windows-ring/verify-captures.py`; it checks those declarations against the committed
Git blobs.

Observed start: `2026-10-08T23:41:59.4564082Z`. Observed end: `2026-10-08T23:42:59.8814866Z`. Elapsed: **60.422 s**.
Outer exit: **124 (timeout)**. The Release build completed in **21,659 ms** with 0 errors and two AVLN3001 warnings
(`CatalogDialog.axaml`, `SaveSectionDialog.axaml`). The ring did not reach its final summary. Therefore its status is
**INCOMPLETE**, not PASS, and the Ruling 163 P5 determinism claim remains unverified.

## Partial suite evidence

| Harness | Evidence at timeout |
|---|---|
| Core 1/3 | Incomplete; 92 PASS and 3 FAIL lines; no timing file or `RESULT` line. |
| Core 2/3 | Incomplete; 112 PASS and 4 FAIL lines; no timing file or `RESULT` line. |
| Core 3/3 | Incomplete; 107 PASS and 2 FAIL lines; no timing file or `RESULT` line. |
| Desktop | Incomplete; 2 FAIL lines; no timing file or final result. |
| Analysis 1/2 | Process completed: 110 PASS, 0 FAIL, `RESULT failures=0`, 22,143 ms. |
| Analysis 2/2 | Process completed: 134 PASS, 1 FAIL (`Section_WingRun_PanelValuesAtEveryStation`, 1-second budget), `RESULT failures=1`, 17,607 ms. |
| CLI | Process completed: 4 PASS, 2 expected Windows manifest failures, 8,462 ms. The expected-failure classifier reported 2 expected, 0 unexpected. |

All nine catalog tests emitted PASS in the partial Core logs, including the class-(d) check
`Catalog_GenEntries_RegenerateToRecordedHash`. The other eight passing rows were `Catalog_Refusal_NamesItsCheck`,
`Catalog_VendAndLink_NoCoordinates`, `Catalog_Fairings_NeverListed`,
`CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations`,
`CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX`, `CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy`,
`Catalog_GenNeverThroughDatParse`, and `Catalog_HashMismatch_CatUnavailable`.

The four RWF check methods emitted **five** exact `DRIFT` lines. `RecordPath_HandlePolar_...` reports section and target as
separate measurements:

```text
DRIFT lambda-fit max_abs=0 limit=1e-06
DRIFT tangent-angle max_abs=0 limit=1e-06
DRIFT dat-rotation max_abs=0 limit=1e-06
DRIFT handle-polar-section max_abs=0 limit=1e-06
DRIFT handle-polar-target max_abs=6.1232339957367663E-18 limit=1e-06
```

These are observed measurements from this Windows process. They do not turn the incomplete ring into a green ring.

## Raw capture identity control

Ruling 163 records that commit `1f2cc9f8` normalized the raw PR #13 `bits.stdout.txt` capture from 13,584 bytes to 13,499,
before `b5c6b52e` restored the CRLF bytes. The receipt now binds this run's raw streams and suite logs to committed blob
SHA-256 values and byte counts. The proof-local `.gitattributes` keeps captured `.txt`, `.log`, `.ms`, and `.seconds` files
un-normalized. `verify-captures.py` fails if either a committed blob's digest or byte count differs from the declaration.

No source, test, tool, case, build, or solver input was edited. No ring retry was made.
