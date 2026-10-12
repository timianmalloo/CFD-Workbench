---
id: proof-ofn-red-first
title: OFN red-first - POSIX-only os attributes on Windows
type: proof-pack
status: draft
owner: "@trk-ofn"
tags: [ofn, windows, portability, plat-a]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-12
summary: >-
  A bare POSIX-only os attribute fails the portable-text-io gate on main and the spike under a Windows-shaped os; both pass after the fix.
---
# OFN red-first: POSIX-only `os` attributes (trk-ofn, 2026-10-12)

Defect: PC readiness (PR #33) failed in the architecture recount on `os.O_NOFOLLOW` (absent on Windows).
Windows behaviour below is **Inferred** (no Windows run); the Mac proof uses a Windows-shaped `os` shim.

## Sweep (tools/, docs/ai-forward-pack/scripts, docs/ai-forward-pack/hooks; no `adapters/` directory exists here)

Searched: O_NOFOLLOW, O_NONBLOCK, O_CLOEXEC, O_DIRECTORY, fchmod/fchown/lchown, getuid/geteuid/getgid, killpg, getpgid, setsid, fork, mkfifo.

| Site | Use | Can run on Windows? | Action |
|---|---|---|---|
| tools/spikes/application-contract-vectors.py:138 | bare `os.O_NOFOLLOW` | Yes (the PC recount) | Fixed: `getattr`; the `is_symlink()` walk (line 127) and `O_EXCL` keep the symlink refusal on every OS |
| same file :149-151 | `os.open(directory)` + fsync | Yes; Windows raises PermissionError (Inferred) | Fixed: skipped on `nt`, comment states `os.replace` is the only durability step |
| tools/run-windows-store-gate.py:106,115,122 | bare `os.killpg` in `--self-test` | Yes (Windows `--self-test`) | Fixed: group-kill cases skipped on `nt`; the run path already branched |
| tools/qualify-windows-runtime.py:259,262 | bare `os.killpg` in `run_local` | Windows ARM only (x64 uses `run_windows`, line 428) | Fixed: `nt` branch, `child.kill()`, direct-child quiescence only. W0-named file, see Return |
| docs/ai-forward-pack/scripts/coord_transport.py:83 | bare `os.killpg` in `_signal_group` | `_Wire` is POSIX-only; Windows uses `_ThreadedWire` | Fixed (defence in depth): `nt` returns `terminate_owned_process` |
| coord_runtime.py:88,125,86,129; coord_files.py:115; coord-runner.py:551,742-748 | O_NOFOLLOW / O_NONBLOCK / getuid / O_DIRECTORY | No: each is behind an `os.name == "nt"` branch or a `posix` `require` | None |
| coord-core.py:547, docs-graph.py:281,368 | `getattr(os, ...)` | Already portable | None |
| run-verify-gates.py:88, platform_process.py:160, run-readiness.py:107,309-329, tools/run-windows-store-gate.py:53 | killpg | No: behind `os.name == "nt"` | None |
| bounded_process.py:36 | `resource` in a POSIX child wrapper string | No: POSIX wrapper only | None |

Not covered: `signal.SIGKILL` (same class, absent on Windows) at tools/verify-application-adapters.py:334 and coord_transport.py:211; both are POSIX paths. Left for a follow-up.

## Control

`docs/ai-forward-pack/scripts/verify-portable-text-io.py` rule (4). Self-test plants one offender (bare `os.O_NOFOLLOW`) and two allowed forms (`getattr(os, ...)`, `os.name` branch). ROOTS now include `docs/ai-forward-pack/scripts` and `docs/ai-forward-pack/hooks` (before, this repo scanned only `tools/`). Widening exposed two pre-existing `write_text` calls in `conductor-join.py` self-test fixtures; fixed with `write_bytes`.

## Red, then green

- Red on the unfixed tree (rule added, code unfixed): `red-gate-output.txt`, exit 1, 9 findings: 7 POSIX (qualify 2, store gate 3, spike 1, coord_transport 1) and 2 conductor-join text writes. Raw output is in the file.
- Red behaviour: `red-shim-output.txt`, exit 1: `AttributeError: module 'os' has no attribute 'O_NOFOLLOW' (as on Windows)`.
- Green: `green-shim-output.txt` exit 0; `green-gate-output.txt` exit 0; `verify-portable-text-io.py --self-test` exit 0; `run-windows-store-gate.py --self-test` exit 0; `qualify-windows-runtime.py --self-test` exit 0.
- `spike-windows-shim.py` runs `atomic_spike` under a Windows-shaped `os` (name `nt`, no O_NOFOLLOW, directory `os.open` raises PermissionError). This is a stand-in; it does not prove Windows.

## Recount cleanup failure (PC line: WinError 5 deleting `Avalonia.BuildServices.Collector.dll` in the recount's temp NuGet cache)

Fix in `tools/recount-architecture-spike.py`: child env gets `AVALONIA_TELEMETRY_OPTOUT=1` and `DOTNET_CLI_TELEMETRY_OPTOUT=1` (same opt-out as `tools/verify-application-core.py:213`); the build runs with `--disable-build-servers -p:UseSharedCompilation=false`; the temp folder is removed with 5 bounded retries (1 s apart) that raise a named error if a file stays locked, never leaving it silently.
Mac: `recount-mac-output.txt`, exit 0, 30 oracle checks pass (real blake3/rfc8785, symlink checks included), temp folder gone. POSIX `rmtree` does not fail on a loaded file, so the lock is not reproducible on the Mac.
Windows effect is **Inferred**: the collector holds the DLL and the opt-out should stop it launching. Not proven.

## Inferred, for the PC

- The directory `os.open` fix (spike) is source-visible but was not the PC's reported temp failure. The oracle's `link.symlink_to(...)` setup (spike lines ~236-241) needs the symlink privilege on Windows; not changed (the oracle asserts exactly 30 checks).

## Pack files changed (shared with other repositories)

`docs/ai-forward-pack/scripts/verify-portable-text-io.py`, `coord_transport.py`, `conductor-join.py`.
