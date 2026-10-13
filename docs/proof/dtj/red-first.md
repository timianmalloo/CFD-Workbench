---
id: proof-dtj-red-first
title: Track DTJ red-first record (per-host Desktop child slots)
type: doc
status: done
owner: "@trk-dtj"
phase: implementation
tags: [proof, test-ring, desktop, windows]
links:
  - { to: plan-test-cost, rel: relates-to }
review-by: "2026-11-13"
summary: >-
  Red and green runs for the per-host Desktop slot count and the measurement script, with the Mac before and after Desktop walls.
---

# Track DTJ: red-first and measurements (2026-10-13)

Behaviour: the Desktop harness reads its child-slot count from `CFD_DESKTOP_SLOTS`, else the host baseline's `limit.desktop_slots`
(exported by `tools/run-tests.sh` as `CFD_DESKTOP_SLOTS_BASELINE`), else `ProcessorCount * 5 / 8`, and prints `DESKTOP-SLOTS <n> (source: ...)`.

## Red, then green

| Check (ring: fast, in-process; cost: under 5 ms) | Old code | New code |
|---|---|---|
| `DesktopSlots_EnvBaselineDefault_AndRefusals` (Desktop harness): env 1, env beats baseline, baseline 4, default 10 on 16 CPUs, refusals of 11, 0, -1, two, empty, 3.5, SpawnWith with env 3 and 2 modes refused with exit 2, one `DESKTOP-SLOTS 1 (source: env)` line | build error CS0117 `DesktopChecks` has no `ResolveSlots`; CS1501 no `SpawnWith` overload with 5 arguments | `PASS DesktopSlots_EnvBaselineDefault_AndRefusals` |
| `check-test-costs.py --self-test`, four `slots:` cases through a temp baseline dir (`limit.desktop_slots=4` on a calibrated host gives 4; no line, uncalibrated host and a sysctl host give none) | `NameError: name 'resolve_slots' is not defined` | 91/91 cases, 0 `SELFTEST FAIL` |

Found in the first smoke run: two existing in-process `SpawnWith` tests read the ambient environment, so `CFD_DESKTOP_SLOTS=8` made them fail
(`expected one SPAWN-START per mode, got: `). Both now pass `_ => null`, so the tests are hermetic.

## Mac default unchanged (Desktop wall)

- Default path prints `DESKTOP-SLOTS 10 (source: default)` on this 16-CPU Mac: the same count as `16 * 5 / 8`.
- Before (recorded, same code path): Desktop 40.3 to 40.5 s at quiet load (`SpawnWith` comment, track B3), 40.5 to 47.4 s over the B4 series.
- After: `tools/run-tests.sh` green, Desktop 42,196 ms (802 PASS), wall 49.7 s (net 47.7 s) against the 60 s budget, 0 COST-MISS, load 12.7 to 21.8.
- Smoke of `tools/measure-desktop-slots.sh` (one rep, N = 8 and 10, load 11 to 15): N = 8 51.2 s, N = 10 46.6 s, 0 TIMEOUT, 0 DRAIN, exit 0, BEST 10.
  This is a smoke run, not a calibration, and no Mac CSV is committed.
