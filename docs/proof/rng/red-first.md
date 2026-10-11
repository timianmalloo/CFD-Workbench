---
id: proof-rng-red-first
title: "RNG red-first: Analysis in three parts, fractional-scale expected-failure class"
type: proof-pack
status: active
owner: "@trk-rng"
phase: implementation
tags: [rng, analysis, partition, c-2, expected-failures, windows]
links:
  - { to: proof-etc-c2-cost, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  C-2 tripped four times with Analysis in two parts. Analysis now runs as three parts (tools/run-tests.sh, tools/check-test-costs.py),
  the 5,000 ms cap unchanged; the Windows expected-failures manifest gets a fractional-scale class and two entries. Self-tests red then green,
  timings before and after.
---

# RNG: Analysis in three parts, and a `fractional-scale` class

Track `trk-rng`, branch `fix/rng-ring-parts`, base `b2c5d81d`, 2026-10-11, macOS, Release. Raw output: `cfd-workbench-continuation/scratch/rng/`.

## Item 1: three Analysis parts

The harness needed no code change: `AnalysisChecks.cs` places whole test classes by `CostHintMs` for any `--part=k/n` (`Assign`), and
`check_parts Analysis` in `tools/run-tests.sh` derives n from the `jobs` list. The change is the job list and the cost checker.

Files changed (all that named the two-part split in live code):

| File | Change |
|---|---|
| `tools/run-tests.sh` | `jobs=` now `"Analysis 1/3" "Analysis 2/3" "Analysis 3/3"`; comment says three parts |
| `tools/check-test-costs.py` | `DEFAULT_JOBS`, usage text, `Analysis.part<k>of3` in every self-test fixture; part 3 added to each fixture |

Checked and needing no edit: `docs/coordination/join.json` (runs `--readiness` and `check-test-costs.py` with the default jobs; no part name),
`tools/check-named-tests.py` (reads every `.tmp-tests/*.log`), `tools/verify-application-core.py` (Core only), `tools/windows-scale-run.ps1`
(names a Desktop readiness check), `tools/run-suite.sh`, `tools/join-ring.sh`, `tools/check-expected-failures.py` (its `--part=k/n` regex is
generic), `tests/CfdWorkbench.Desktop.Tests` (Desktop parts). Not edited, historical: `docs/proof/**` receipts and logs that quote
`Analysis.part1of2`, and `docs/plans/test-cost.md` section 9.7 (a dated record of Track B4).

PC ring: the Windows ring runs `bash tools/run-tests.sh` (no private job list; no `.ps1` or `.py` names an Analysis part), so the third
part is picked up automatically. A `docs/proof/ring-<host>/baseline.csv` `limit.analysis_part` line still applies per part.

### Red then green (`python3 tools/check-test-costs.py --self-test`)

- Before (main): `SELFTEST 83/83 cases`.
- Red: `DEFAULT_JOBS` at three parts with the old two-part fixtures: `SELFTEST 54/83 cases`. C-6 reports
  `Analysis.part3of3.ms is missing or unreadable` and `Analysis.part3of3.log is missing`, so a ring that drops part 3 cannot pass.
- Green: fixtures carry part 3 and four new cases (C-2 per part at part 3, C-5 reads part 3's log, C-6 part 3 log missing, C-6 part 3 `.ms`
  deleted): `SELFTEST 87/87 cases`, exit 0.

### Timings (Analysis only, `dotnet <dll> --part=k/n`, ms, three runs each, one after another, load 10 to 12)

| Layout | Part walls (median) | Slowest part median | PASS lines per whole run |
|---|---|---|---|
| 2 parts (before) | 6038 / 5054 (runs 7388, 6038, 4191 and 6289, 5054, 4385: the first runs were under heavy load) | 6038 | 248, 248, 248 |
| 3 parts (after) | 3114 / 3057 / 3064 (runs within 3053 to 3149) | 3114 | 248, 248, 248 |

Every check runs once: 248 PASS before and after, and in the ring 43 + 69 + 136 = 248 (before the ring ran 102 + 146 = 248). `check_parts`
accepted the three `PARTITION k/3` lines. The `CostHintMs` hints were not re-measured as ETC did: the group set is unchanged and the
three parts measure 3.05 to 3.15 s, a 2 % spread. A stale hint costs balance, never coverage.

### Ring (`tools/run-tests.sh`, whole ring, ms)

| Run | Analysis parts | wall (net) | load start to end | result |
|---|---|---|---|---|
| before, 2 parts | 5020 / 4595 | 49600 (47874) | 24.4 to 30.7 | C-2 over 5000 at load 30.7: COST-MISS (load gate), not a failure |
| after, 3 parts, run 1 | 3863 / 3876 / 3803 | 60374 (51912) | 17.0 to 26.5, a concurrent ring present | exit 0; TEST-BUDGET-MISS 61 s at load 26.5 |
| after, 3 parts, run 2 | 3559 / 3575 / 3552 | 52325 (50599) | 26.7 to 49.2 | exit 0; C-3 COST-MISS at load 49.2 |

The slowest Analysis part falls from 5020 ms to 3559 to 3876 ms in the loaded ring, a margin of 1.1 to 1.4 s under the 5000 ms cap (was
inside noise). The third job costs about 0.5 s of process start per part, and about 26 CPU-seconds more per ring in the single paired
reading (cpu 555 s against 529 s; noisy at this load). The ring's critical path is Desktop (47 s) and Core, not Analysis, so wall did not
fall. The 60 s budget was met at 52 s under load 27 to 49 and missed once (61 s) with a second ring running; at a quiet load the wall is
the Desktop job's 40 to 48 s plus the build. I did not get a quiet-load reading: the machine sat at load 10 to 50 throughout. Labelled
Inferred for the quiet case.

## Item 2: `fractional-scale` class (Ruling 186 pause)

`tools/check-expected-failures.py`: `CLASSES` gains `fractional-scale`. Self-test: the fixture manifest gains `Scale_D`; four new cases and a class-load check:
name and fragment match is expected; same name with another number is unexpected; a pass is a stale entry; not Windows the failure stays a
failure; the class-load check: `fractional-scale` loads and `no-such-class` is refused.

`tests/expected-failures.windows.json`: two entries (ruling `186`, class `fractional-scale`, until "the Ruling 186 fractional-scale proof
resumes"): `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` (`input ends 218.33, fact 217.33`) and
`PropertiesPane_Density_UnitOnValueBaseline` (`input 143.87 unit 144.54 label 144.54; fact 120.54/120.54`), from
`docs/proof/wrx/classification.md` section 5.

- Red (class removed, entries present): `SELF-TEST FAIL: manifest does not load: manifest entry PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs: unknown class fractional-scale`, exit 1.
- Green: `EXPECTED-FAILURES self-test ok: 20 cases, manifest 54 entries`, exit 0 (was 16 cases, 52 entries before).
