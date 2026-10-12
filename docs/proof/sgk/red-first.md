---
id: proof-sgk-red-first
title: SGK red-first - POSIX-only signal names on Windows
type: proof-pack
status: draft
owner: "@trk-sgk"
tags: [sgk, windows, portability, plat-a]
links:
  - { to: proof-ofn-red-first, rel: relates-to }
review-by: 2026-11-12
summary: >-
  A bare POSIX-only signal name fails the portable-text-io gate on the unfixed tree; it passes after the two fixes.
---
# SGK red-first: POSIX-only `signal` names (trk-sgk, 2026-10-12)

Residual of OFN (`docs/proof/ofn/red-first.md`). Windows behaviour is **Inferred** (no Windows run).

## Sweep (tools/, docs/ai-forward-pack/scripts, docs/ai-forward-pack/hooks)

Searched: SIGKILL, SIGHUP, SIGQUIT, SIGUSR1, SIGUSR2, SIGALRM, SIGCHLD, SIGPIPE, SIGSTOP, SIGCONT, `signal.alarm`, `signal.pause`, `signal.setitimer`. Only SIGKILL occurs; no other name, no `from signal import`, nothing in `hooks/`.

| Site | Can run on Windows? | Action |
|---|---|---|
| docs/ai-forward-pack/scripts/coord_transport.py:213 (`_Wire.close`) | `_Wire` is the POSIX wire; `_signal_group` already returns `terminate_owned_process` on `nt` and ignores the signal | Fixed: `getattr(signal, "SIGKILL", signal.SIGTERM)`; the Windows kill is unchanged (`terminate_owned_process`) |
| tools/verify-application-adapters.py:334 (`terminate_owned`) | No: `main()` returns 4 (Not assessed) on `nt` and `process_table` raises first | Fixed: `os.name == "nt"` guard that raises, as in `verify-application-core.py` |
| tools/verify-application-core.py:91 | No: function raises on `nt` before the loop | None |
| tools/run-windows-store-gate.py:53, tools/run-readiness.py:107,329 | No: `nt` branch uses `process.kill()`; :329 is inside `if os.name != "nt"` | None |
| docs/ai-forward-pack/scripts/platform_process.py:160 | No: after the `nt` return in `terminate_owned_process` | None |
| tools/qualify-windows-runtime.py:263 | No: `nt` branch from OFN (file not touched, Ruling 40 freeze) | None |

## Control

`docs/ai-forward-pack/scripts/verify-portable-text-io.py` rule (4) gains `POSIX_ONLY_SIGNAL` (the 10 names plus `alarm`, `pause`, `setitimer`). Same guards as for `os`: an enclosing function that tests `os.name`, `sys.platform`, `platform.system()` or `hasattr/getattr(os|signal, ...)`. Self-test gained one planted `signal.SIGKILL` offender (line 12) and one allowed `getattr(signal, ...)` form (line 14).

## Red, then green

- Red (rule added, code unfixed): `red-gate-output.txt`, exit 1, exactly 2 findings (adapters:334, coord_transport:213).
- The self-test is red against the unfixed rule by construction (its new fixture expects a `signal.SIGKILL` finding that the old rule cannot produce).
- Green: `green-gate-output.txt` exit 0; `self-test-green.txt` exit 0.

## Pack files changed (shared with other repositories)

`docs/ai-forward-pack/scripts/verify-portable-text-io.py`, `docs/ai-forward-pack/scripts/coord_transport.py`.
