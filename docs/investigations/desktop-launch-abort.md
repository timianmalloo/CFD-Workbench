---
id: investigation-desktop-launch-abort
title: Desktop test harness aborts with SIGABRT under the application gates
type: doc
status: in-review
owner: "@track-crash"
phase: implementation
tags: [application, desktop, test-harness, gates, investigation]
links:
  - {to: design-section-editor, rel: relates-to}
  - {to: adr-application-stack, rel: depends-on}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-27
summary: >-
  Under `dotnet CfdWorkbench.Desktop.Tests.dll`, the harness relaunched Environment.ProcessPath
  (the dotnet muxer) with only `--section-flow`. The child ran `dotnet --section-flow` and exited 1,
  and the unhandled exception aborted the process, which wrote a macOS crash report. Fixed with a
  launch-shape-aware relaunch and a named exit 70 for unhandled exceptions. The core gate's separate
  "live owned descendants" failure is Avalonia's build telemetry collector.
---

# Investigation: desktop-launch-abort — dotnet aborts during the application gates

Goal: find why `dotnet` aborts (SIGABRT) during `tools/verify-application-core.py` and
`tools/verify-application-adapters.py`, then fix it at the cause. Done when both gates exit 0,
`tools/run-tests.sh` and `tools/check-docs.py` exit 0, and a final gate run writes no new crash report.
Out of scope: the test/CI cost review (TESTCI), app-shell features and the Python lint gates.

## Symptom

- macOS wrote crash reports for `dotnet` (SIGABRT, "abort() called") at 08:17:14, 08:18:11, 08:18:49
  and 08:23:37 on 2026-09-27. Parent: `python3`. Loaded modules: libAvaloniaNative, SkiaSharp,
  libcfd_store. Thread 0: `IL_Throw` → `DispatchManagedException` → `TerminateProcess`, reached from `Main`.
- At the same time, the adapters gate failed with status `fail` and the core gate raised
  `RuntimeError: Build exited with live owned descendants`. Both gates are red on `main`.

## Reproduction

The managed exception text is in the adapters gate's retained receipt
(`$TMPDIR/cfd-adapters-verify-d0vp0lcx/receipts/CfdWorkbench.Desktop.Tests.stderr`, 08:23).
It reproduces with the gate's launch shape alone (observed, 08:27, exit 134, crash report `dotnet-2026-09-27-082723.ips`):

```
$ dotnet tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.dll
Could not execute because the specified command or file was not found.
  * You intended to execute a .NET program, but dotnet---section-flow does not exist.
Unhandled exception. System.Exception: section-flow exited 1
   at Program.<Main>$(String[] args) in .../tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs:line 1867
```

The same build through its apphost (`.../CfdWorkbench.Desktop.Tests`, the `dotnet run` shape that
`tools/run-tests.sh` uses) exits 0 (observed).

## Timeline

1. `aae1ac7`, `80758ec`, `d32abf0` add the section canvas, flow and tools suites. Flow and tools run
   in child processes started as `ProcessStartInfo(Environment.ProcessPath!, "--section-flow")`.
2. `tools/run-tests.sh` runs the harness with `dotnet run`, so the process path is the apphost and the relaunch works.
3. The adapters gate runs `dotnet <artifacts>/CfdWorkbench.Desktop.Tests.dll`. The process path is
   the muxer, the relaunch runs `dotnet --section-flow`, the harness throws, and the runtime aborts.
   Every gate run writes a crash report, and the Mac shows a crash dialog.

## System map

gate (python3, new session) → `dotnet X.dll` (muxer hosting the harness) → relaunch with
`Environment.ProcessPath` → child. For an apphost, the process path and the program are the same
file. For the muxer they are not, and the entry assembly must be passed again. Any unhandled exception
on any thread ends in `abort()` because neither the harness nor the app handles it at the top level.

## Hypotheses considered

| # | Hypothesis | Evidence for / against | Verdict |
|---|---|---|---|
| H1 | The relaunch drops the entry assembly under the muxer | Child stderr `dotnet---section-flow does not exist`; muxer exits 134, apphost exits 0 on the same build | **Verified** |
| H2 | The store refuses the gate's TMPDIR (symlinked `/var` path) | The adapters gate resolves its scratch (`resolve(strict=True)`); the failing child is the CLI muxer, not the store; SectionCanvas passed before the relaunch | Ruled out |
| H3 | The desktop app (`CfdWorkbench.Desktop`) itself aborts at startup under the smoke selectors | The aborting process is `/opt/homebrew/*/dotnet`, not the `CfdWorkbench.Desktop` apphost; the smoke step never ran (the gate stopped at the harness) | Ruled out as the reported crash; the app shares the abort class (below) |
| H4 | Core gate: a build-server or node-reuse process outlives `dotnet build` | `--disable-build-servers` and `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1` are set; a probe of the same build names `Avalonia.BuildServices.Collector` in the group without the opt-out, and never with it | Replaced by H5 |
| H5 | Core gate: Avalonia's build telemetry collector joins the owned group | Probe (observed): collector seen in the group with the opt-out unset, never seen with `AVALONIA_TELEMETRY_OPTOUT=1`; the adapters gate already sets the opt-out and forbids the collector | **Verified** that the collector joins the group. That it was the live PID in the base failure is Inferred, because the core receipt records PIDs, not commands. |

## Verified root cause

`tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs:1864` and `:1869` (base `bdcbefb`) relaunch
`Environment.ProcessPath` with the mode argument only. That is correct for an apphost and wrong
for the muxer (`dotnet X.dll`), which the adapters gate uses.

- **Sufficient:** the muxer launch reproduces the exception and abort (exit 134, crash report).
- **Necessary:** the apphost launch of the same build, and the muxer launch after the fix, both exit 0.

**Why a crash report instead of a failure message:** the harness and `CfdWorkbench.Desktop` had no top-level
handler. .NET turns an unhandled exception into `abort()`, and macOS reports that as a crash.
This was observed for the app too: `CFDW_REVIEW_MODE=2 CfdWorkbench.Desktop` exited 134 and wrote
`CfdWorkbench.Desktop-2026-09-27-083018.ips` before the fix.

## Causes ruled out

H2 and H3, for the reasons in the table. The harness also throws when a child fails for any other
reason. That is correct behaviour; the defect is the abort, which the handler now converts.

## Specific fix(es) for this instance

- `tests/CfdWorkbench.Desktop.Tests/SelfLaunch.cs`: `SelfLaunch.StartInfo` passes the entry assembly
  unless the host file is the program's apphost (`<name>` or `<name>.exe`, compared by file name,
  because stripping the extension from `CfdWorkbench.Desktop.Tests` eats `.Tests`). `RunChild` replaces both relaunch sites.
- `src/CfdWorkbench.Desktop/Program.cs`: `StartupFailure.Install()` registers an
  `AppDomain.UnhandledException` handler. It writes `APP-UNHANDLED <exception>` to stderr and calls
  `Environment.Exit(70)` (EX_SOFTWARE). A spike confirmed this exits 70 without `abort()` for
  main-thread and worker-thread throws. `Program.Main` and the harness both install it first.
- `tools/verify-application-core.py`: set and record `AVALONIA_TELEMETRY_OPTOUT=1`, as the adapters gate does.
- Regression tests (`SelfLaunchTests`, run first in the default harness mode): muxer shape, apphost
  shape, a child-throw probe that must exit 70 with `APP-UNHANDLED`, and a source scan that refuses
  `Environment.ProcessPath` anywhere in `src/` or `tests/` except `SelfLaunch.cs`. **Observed red**
  before the fix: 3 of 4 failed (probe exit 134, muxer shape, raw use in `WorkbenchTests.cs`).
  Observed green after the fix under both the apphost and the muxer.
- Rollback: revert the fix commits. Blast radius: test harness child launches, and the desktop
  app's unhandled-exception exit path, which changes from 134/abort to 70/named.

## Generalization — the failure class

**Class:** a self-relaunch that assumes the launch shape (`Environment.ProcessPath` is the program),
plus an unhandled exception that ends in `abort()` rather than a named exit.
**Siblings:** the only `Environment.ProcessPath`, `GetCommandLineArgs` or `MainModule` uses in
`src/` and `tests/` were the two relaunch sites (swept). The top-level programs without a handler
were `CfdWorkbench.Desktop` (fixed) and the Desktop harness (fixed). The CLI returns coded exits.
The Core and CLI test harnesses were not changed (not in scope; their throw path is the same runtime default).
**Telemetry sibling sweep:** scripts that run `dotnet` without the opt-out are
`qualify-windows-runtime.py` (builds a spike with no Avalonia reference),
`recount-*.py` and `run-tests.sh`. None asserts descendant quiescence, so none is affected.
**Markers:** there are no `assume:` or `simplify:` markers in `tests/CfdWorkbench.Desktop.Tests`,
`src/CfdWorkbench.Desktop/Program.cs` or the two gate scripts.
**Why it survived:** the inner loop (`run-tests.sh`) runs only the apphost shape, the gate that uses the muxer
was already red on `main`, and each red run's evidence was a crash report, not a message.

## Phased repair plan

| Phase | Scope (code + tests) | Failure mode eliminated | Validation | Depends on |
|---|---|---|---|---|
| 1 | `SelfLaunch` + relaunch sites + `SelfLaunchTests` | Muxer relaunch runs `dotnet --mode` | Red 3/4 → green; both launch shapes exit 0 | — |
| 2 | `StartupFailure` in app and harness | Unhandled exception → abort + crash dialog | Probe exits 70; app exits 70 on a bad selector | — |
| 3 | Core gate telemetry opt-out | Collector outlives the owned build | Core gate exit 0 | — |
| 4 | Register class `HARNESS-LAUNCH-SHAPE` | Recurrence of the class | Source scan in the harness fails on a raw relaunch (observed) | 1 |

All four phases are implemented on `fix/desktop-launch-crash`. The contract pre-authorized
continuing into the fix.

## Residual risk & follow-ups

- The core gate's `run` records PIDs only, so a future live descendant is not named. The adapters gate names the collector.
- The same run saw a `csc` SIGSEGV inside CoreCLR (`MethodTable::CheckRunClassInitThrowing`,
  `csc-2026-09-27-083059.ips`). It happened once, in a build of unchanged code; the other builds were clean. It is not caused by this repo.
- An exception that escapes into native Avalonia callbacks may still fail fast without reaching the handler. Not assessed.
- The Core and CLI test harnesses still abort on an unhandled exception.

## Adversarial review (csharp-developer, advisory, PASS-WITH-CONDITIONS)

- **Accepted and fixed (repair cycle 1).** The probe did not cover the muxer shape in `run-tests.sh`,
  and it proved the handler only in the harness. Fix: the probe now also launches the harness under
  `dotnet <dll>` explicitly, and the product as `dotnet CfdWorkbench.Desktop.dll` with
  `CFDW_REVIEW_MODE=2`. Each launch has a 60 s timeout and a kill. Mutant observed: with
  `StartupFailure.Install()` removed from `Program.Main`, the product case fails with
  `exit 134`, and the harness itself exits 70 with `APP-UNHANDLED`.
- **Open (residual).** The launch-shape match is by ordinal file name, so a renamed apphost, a
  single-file publish (empty `Location`) or Windows casing differences can add an extra argument.
  This is harmless today because the modes match with `args.Contains`. The source scan does not see
  `GetCommandLineArgs()[0]`, `MainModule` or `AppContext.BaseDirectory`. A `ProcessExit` handler that
  waits on the UI thread could deadlock `Environment.Exit`; there is none today (Inferred, not swept exhaustively).

## Gate record

The final gate exit codes, `run-tests.sh`, `check-docs.py` and the crash-report counts are recorded in the track's return message and the audit entry.
The investigator did not self-certify: the fix awaits independent review at the join.
