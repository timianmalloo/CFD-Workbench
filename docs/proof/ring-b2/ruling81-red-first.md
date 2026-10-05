---
id: proof-ring-b2-ruling81-red-first
title: "Ring B2: Ruling 81 red-first receipt"
type: proof-pack
status: active
owner: "@trk-b2"
phase: implementation
tags: [ring, ruling-81, readiness]
links:
  - { to: proof-ring-b2-profile, rel: relates-to }
summary: "A planted slow orbit frame fails at low load and prints READINESS-MISS, never PASS, at high load; the unplanted frame passes."
review-by: "2026-11-05"
---

# Ruling 81 red-first

The check under test is `Readiness_OrbitFrameP95Under33Ms`. Planted input (temporary, not committed): a 40 ms `Thread.Sleep` after each timed frame, and a
temporary environment override of the load reading. The same helper, `DesktopChecks.RequireFrameBudget`, serves `Readiness_WindowRenderPlan3d_Under33Ms`.

| Run | Planted slow frame | Load reading | Output |
|---|---|---|---|
| A | yes | 5 | `FAIL Readiness_OrbitFrameP95Under33Ms Exception: Readiness_OrbitFrameP95Under33Ms 61.68 ms is over 33 ms at load 5.00 (gate 24)` |
| B | yes | 60 | `READINESS-MISS Readiness_OrbitFrameP95Under33Ms value_ms=61.44 target_ms=33 load=60.00 gate=24` (no PASS, no FAIL) |
| C | no | 60 | `READINESS Readiness_OrbitFrameP95Under33Ms value_ms=22.61 ...` then `PASS Readiness_OrbitFrameP95Under33Ms` |
| D | no | real (47.30 at start) | value_ms=22.09, `PASS Readiness_OrbitFrameP95Under33Ms` |
| E | no, window check | real | `READINESS-MEASURE WindowRenderPlan3d median 21.78 ms target 33.00 ms`, `PASS Readiness_WindowRenderPlan3d_Under33Ms` |

The committed fast check `ReadinessGate_PlantedSlowFrame_FailsQuietMissesLoaded` repeats A, B and the gate edge (24 fails, 24.01 misses, load not recorded misses)
on planted values without the machine.
