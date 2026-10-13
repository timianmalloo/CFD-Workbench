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

## Validation

- `tools/measure-desktop-slots.sh --self-test`: passed (six classifications and one best-N selection).
- `py -3 tools/check-test-costs.py --self-test`: passed (91/91 cases).
- `py -3 tools/check-test-costs.py --resolve-slots proc-gitbash pc-win`: returned `8`.
- `py -3 tools/check-docs.py`: passed.
