---
id: proof-cbs-audit
title: "CBS audit of the Mac timeout paths"
type: proof-pack
status: active
owner: "@trk-cbs"
phase: implementation
tags: [cbs, timeout, cleanup-blocks-ceiling, audit]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Every Mac-side timeout path audited for an unbounded wait after the deadline: three exposed and fixed, the rest safe,
  two pack-managed findings left for /updatepack.
---

# CBS audit: the Mac's timeout paths (CLEANUP-BLOCKS-CEILING sweep, 2026-10-09)

Base main 3a139e3e. Line numbers are of the base, before the fixes in this branch. Probe: a child that ignores SIGTERM and
starts a grandchild in its own session that holds stdout; `finish`/`run_gate` called with an expired or 1 s deadline and
with `os.killpg` made to raise.

| Path | file:line (base) | Waits after the deadline | Bounded? | Reader threads / pipe close | Verdict |
|---|---|---|---|---|---|
| readiness `finish` (STEP_TIMEOUT 1200 s, per-entry 60 s, `run_entry`) | tools/run-readiness.py:84-105 | `os.killpg` SIGKILL, then `process.wait()` (:97) | `wait()` unbounded; `killpg` failure raised out of `finish`, orphaning the other steps of a group | none: step output goes to a log file (:78), `output.close()` (:101) is a file close on the ceiling thread, no pipe | EXPOSED (two defects: unbounded `wait`, unhandled kill failure). Measured before: failed kill raised `PermissionError`. Fixed. |
| Windows-store gate wrapper (60 s) | tools/run-windows-store-gate.py:40-55 | `killpg`, then `communicate(timeout=5)` | bounded, but 5 s and the exception from a failed kill was unhandled | stdout is a PIPE read by `communicate` on the ceiling thread; no explicit close; a pipe held by an escaped grandchild is abandoned after the timeout | EXPOSED (measured before: held pipe 5.00 s cleanup; failed kill raised). Fixed: bound 2 s, kill failure falls back to `process.kill()`. |
| Desktop harness child kill (`RunBuffered`, 120 s) | tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs:811-813 | `Kill(entireProcessTree)`, then `WaitForExit()` with no argument (waits for both redirected streams to reach EOF) | unbounded: a process outside the killed tree holding a pipe blocks forever; `Kill` may throw | `OutputDataReceived`/`ErrorDataReceived` events (runtime reader tasks), `using var child` disposes after | EXPOSED. Fixed: kill guarded, drain on a daemon thread joined for 10 s, `FAIL DRAIN` line and exit 125. Not red-first (see red-first.md). |
| `tools/run-suite.sh` | tools/run-suite.sh:9,17 (`ring_lock_acquire`) | none after a deadline; the only timeout is the lock wait | bounded: `CFD_SUITE_LOCK_WAIT_SECONDS` (120 s), then it proceeds. Poll loop tools/ring-lock.sh:42-63. No kill, no pipe | none | SAFE. Existing self-test: `run-suite.sh --self-test` case 3 (:47-49). |
| `tools/run-tests.sh` | tools/run-tests.sh:12,27,95,116 | `wait "${pids[$index]}"` (:116) has no ceiling; lock wait is bounded (ring-lock.sh, 900 s) | The script asserts no run ceiling, so no ceiling to defeat. The `sleep 2` (:95) is fixed | none | SAFE (no timeout is claimed). Per-child ceilings live in the harnesses (WorkbenchTests.cs above). |
| `docs/ai-forward-pack/scripts/run-verify-gates.py` (pack-managed, not edited) | :82-113 | `_kill_tree` (`killpg`, `except (OSError, ProcessLookupError): process.kill()`), then `communicate(timeout=5)` (:110) | bounded at 5 s; kill failure handled for POSIX. On Windows `taskkill` uses `subprocess.run` with no timeout (:84-85) | stdout PIPE, drained by `communicate`; nothing closed on the ceiling thread | SAFE on the Mac (bounded 5 s, which exceeds the 2 s bound the other paths now hold). Finding for /updatepack: bound `taskkill` with `timeout=`, and use a shorter drain. |
| `docs/ai-forward-pack/scripts/conductor-join.py` (pack-managed, not edited) | :128-155, :299 | `subprocess.run(...)` with no `timeout=` | n/a: the file asserts no ceiling; each join step runs to its own end | none | SAFE (no ceiling claimed). Finding for /updatepack: if the join ever gets a step ceiling it must follow the rule below. |

Rule held by the fixed paths: the kill never raises (group kill, then child kill, then give up); the wait after the kill is
bounded; no pipe is closed on the ceiling thread (it is abandoned or drained on a daemon thread); the reported verdict is red.

Evidence kept for the safe paths: run-suite.sh self-test case 3 already proves the bounded lock wait with real `sleep`
processes (~5 s, ring: every join). No test was added for them: they hold no child kill.
