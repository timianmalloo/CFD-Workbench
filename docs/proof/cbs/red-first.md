---
id: proof-cbs-red-first
title: "CBS red-first receipt"
type: proof-pack
status: active
owner: "@trk-cbs"
phase: implementation
tags: [cbs, timeout, red-first]
links:
  - { to: proof-cbs-audit, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Real-child self-tests for the readiness and Windows-store timeout paths failed on the old code and pass on the fix, with
  measured cleanup times; the Desktop harness fix has no red-first test.
---

# CBS red-first record

Real child processes throughout (Ruling 171 (1)): the child ignores SIGTERM and starts a grandchild in its own session that
holds stdout for 8 s; cases are an already-expired deadline, a held pipe, and an injected `os.killpg` failure. Each run prints
the measured cleanup time (seconds after the deadline).

## tools/run-windows-store-gate.py --self-test
- Red (old `run_gate`, new test): `store-gate-red.txt`, exit 1. Held pipe cleanup 5.00 s against a 2 s bound; failed kill
  raised `PermissionError`. Expired deadline 0.00 s (the grandchild did not exist yet when the child was killed, so it holds
  nothing; kept as a boundary case, not as proof).
- Green (fix): `store-gate-green.txt`, exit 0. Expired 0.00 s, held pipe 2.00 s, failed kill 2.01 s, bound 2 s.

## tools/run-readiness.py --self-test
- Red (old `finish`, new test): `readiness-red.txt`, exit 1. Failed kill raised `PermissionError` out of `finish`
  (expired 0.20 s, timeout 0.23 s were already bounded: step output is a file, there is no pipe).
- Green (fix): `readiness-green.txt`, exit 0. Expired 0.21 s, timeout 0.23 s, failed kill 0.23 s, bound 2 s.

## tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs `RunBuffered`
- No red-first. A real-child check of `RunBuffered` needs a harness mode that detaches a grandchild; `RunBuffered` only
  re-launches this dll by mode name through `SelfLaunch.StartInfo`, and `Program` mode dispatch and `SelfLaunch.cs` are not
  owned by this track. The hang itself (a pipe held outside the killed tree) is therefore Inferred from the .NET contract
  and from the code's own comment at the old line 813, not observed.
- After the fix, the timeout path ran with real children: `CFD_TEST_CHILD_TIMEOUT_SECONDS=1` against the built harness
  killed 17 children and returned in 5 s wall (`desktop-timeout-run.txt`, exit 124 from the harness itself).
