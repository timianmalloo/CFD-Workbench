---
id: proof-pc-win-desktop-slot-calibration
title: "PC Windows Desktop child-slot calibration"
type: proof-pack
status: done
owner: "@trk-dtj"
phase: implementation
tags: [windows, desktop, test-ring, calibration, evidence]
links:
  - {to: plan-test-cost, rel: relates-to}
review-by: "2026-11-13"
measured_source_head: "4e168c05f94ff2f717e69a78d63f7b08773f7561"
summary: >-
  Twelve Desktop harness measurements on the quiet Windows PC classify as pass with zero TIMEOUT and DRAIN; eight slots is
  fastest at a 134.3-second median.
---

# PC Windows Desktop child-slot calibration

**Goal:** measure Desktop child-slot counts on the Windows ring host.
**Done when:** three quiet repetitions each at 2, 4, 6, and 8 slots pass classification with zero TIMEOUT and DRAIN, and the
  fastest count is recorded in the host baseline.
**Not in scope:** product, test, or gate changes; another full ring.
**Tier:** T0. **Fan-out:** 0.

## Measurement

The measured source HEAD was `4e168c05f94ff2f717e69a78d63f7b08773f7561`. The run used the .NET SDK **10.0.203** from
`%USERPROFILE%\.dotnet`, selected with `DOTNET_ROOT` and a prepended `PATH`. The script ran from Git Bash with the existing
host key `pc-win`; `CFD_DESKTOP_SLOTS` was unset outside the per-sample child environment. The Windows expected-failure
classifier produced `pass` for every sample, although the raw harness exit was 1.

| Slots | Repetitions (ms) | Median (ms) | TIMEOUT | DRAIN | Verdicts |
|---:|---|---:|---:|---:|---|
| 2 | 330096 / 340076 / 350529 | 340076 | 0 | 0 | 3 pass |
| 4 | 206549 / 206946 / 204730 | 206549 | 0 | 0 | 3 pass |
| 6 | 153920 / 152809 / 152446 | 152809 | 0 | 0 | 3 pass |
| 8 | 133912 / 134325 / 134576 | 134325 | 0 | 0 | 3 pass |

The complete per-run readings are in `desktop-slots.csv`. The script selected **8 slots**. `baseline.csv` now carries
`limit.desktop_slots=8`. Its three existing quiet Ruling 173/176 rows remain unchanged: end loads 12.18, 10.08, and 11.80,
all below the host gate of 13.

## Extension above 8 (Mac leader hold, PR #40)

The PC has 20 logical processors (`docs/proof/win-routes/receipt.md`). Without a baseline line, the harness would default to
`Math.Clamp(20 * 5 / 8, 1, 14)` = 12 slots (`tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs`). Because 8 was the largest
count tested and the wall was still falling, the Mac leader asked for 10, 12 and 14 before the join.

- N=10: rep 1 took 138,271 ms and rep 2 took 141,368 ms. Both passed with zero TIMEOUT and DRAIN, and both were slower than
  the 8-slot median. Rep 3 took 128,972 ms with **one DRAIN** (verdict `fail`, load 12.03). The run stopped there, as instructed.
- N=12 and N=14 have no valid measurement. Two N=12 rows were recorded after the stop, because the outer shell kept running after
  its child was interrupted. They are not a three-repetition set and are excluded. The partial CSV stays uncommitted on the PC.

**Decision:** keep `limit.desktop_slots=8`. N=10 is no faster in its passing reps and is not clean, so 8 is the best qualified
count. It is also below the PC's untested default of 12. `tools/measure-desktop-slots-best.py` now reports a best N at the edge
of the tested range as `AT-EDGE` (exit 3) rather than `BEST`. That is how this extension was found to be needed.

## Validation

- `tools/measure-desktop-slots.sh --self-test`: passed (six classifications and one best-N selection).
- `py -3 tools/check-test-costs.py --self-test`: passed (91/91 cases).
- `py -3 tools/check-test-costs.py --resolve-slots proc-gitbash pc-win`: returned `8`.
- `py -3 tools/check-docs.py`: passed.
