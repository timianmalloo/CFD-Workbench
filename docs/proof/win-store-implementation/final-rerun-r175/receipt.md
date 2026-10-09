---
id: proof-win-store-final-rerun-r175
title: "Ruling 175 final joined verifier direct Windows run — incomplete exit capture"
type: proof-pack
status: blocked
owner: "@win-store-final-rerun-r175"
tags: [windows, persistence, ruling-175, evidence]
links:
  - { to: review-pr-19, rel: implements }
  - { to: proof-windows-store-implementation, rel: relates-to }
review-by: "2026-11-09"
summary: "One direct final-script Windows run emitted qualification PASS, but the outer capture did not retain its process exit. Windows PASS may not enter readiness from this incomplete receipt."
---

# Ruling 175 final joined verifier direct run

**Windows PASS may not enter readiness from this receipt.** The final-script run printed the expected qualification PASS, but the outer launcher exit is **Not recorded**. The capture remains fail-closed rather than inferring exit 0 from stdout.

Goal: capture exactly one no-argument Windows run of the final joined verifier under a 60-second outer watchdog.
Done when the receipt binds source/process identity, stdout/stderr/exit and timing, captures pass integrity/privacy gates, and a proof-only commit is ready.
Not in scope: verifier/product/test changes, admission policy, zero-budget/SKIP follow-ups, full application ring or a second verifier run. Tier T1; fan-out 0; repair cap 2.

Confidence: **Verified** means observed in the retained output or process snapshot; **Not recorded** means no measurement is available; **Not assessed** means the required property was not established. This is a blocked evidence checkpoint, not qualification admission.

## Exact source and command

- Tested HEAD: `421b5860cbaed5cf047e8c29661b68ec0f84e9f3` on `win/store-final-rerun-r175`.
- Script: `tools/verify-windows-store.py`; Git blob: `528b56d84bb1bb409d01ac683355f78981765d7c`.
- Script SHA-256: `a79ac73c04cc8769d65868cde2f3c69154f27ebfcff40743bd20ca0b668afd52`. The checkout hash matches the verifier's own emitted hash. It is the joined script including the portable stdio guard, rather than the earlier `92151a8f…` reviewed capture.
- Exact direct command: `py -3 tools/verify-windows-store.py`. No self-test, filter or budget argument was passed. The project harness selected internally by the verifier is its normal 13-check qualification subset, not the full application ring.
- SDK: user-local **10.0.203**; `DOTNET_ROOT=%USERPROFILE%\.dotnet` and that directory first on PATH. `dotnet --version` was checked before the single launch.
- Native environment emitted: Windows 11 `10.0.26300`, AMD64; harness `NATIVE-ENV os=10.0.26300.0 arch=X64 filesystem=NTFS`.
- Requested and emitted affinity: **`0x3f`, six CPUs**. The verifier emitted `prior_mask=0x3f`; the watchdog originally held `0xfffff`, then set itself to `0x3f` before launching the child.

The scratch watchdog was written outside the repository and is copied here as `watchdog.ps1` to retain the actual capture method. It launched the direct command once with separate stdout/stderr files, sampled its descendant process tree, imposed an outer 60-second timeout, and would call `taskkill /T /F` on the launcher PID at timeout. A timeout, missing process exit or observed residual makes its result FAIL. Sampling cannot prove that no short-lived descendant was missed. The timeout path was not exercised; this receipt makes no new watchdog-control correctness claim.

Plan: source/contract grounding → one direct captured run → read result → receipt/hash/PII checks → audit/index → documentation gates → proof commit. Actual: the direct run completed, but missing launcher exit blocked its acceptance; no second run or verifier repair occurred. The remaining work preserves that failure and validates the evidence rather than rerunning it.

## Time and process binding

| Measurement | Observed value |
| --- | --- |
| Outer launch UTC | `2026-10-09T03:33:40.9266832Z` |
| Launcher PID / start | `33632` / `2026-10-09T03:33:40.9541571Z` |
| Actual verifier Python PID / creation | `31704` / `2026-10-09T03:33:41.0078690Z` |
| Verifier START_UTC | `2026-10-09T03:33:41.332614Z` |
| Verifier END_UTC | `2026-10-09T03:34:00.522950Z` |
| Verifier module-start total | `19.466184 s` |
| Outer measured launch-to-completion observation | `19.772830 s` |
| Outer metadata completed UTC, after residual sampling | `2026-10-09T03:34:02.9713273Z` |
| Outer deadline | `60 s`; `watchdogTimedOut=false` |
| Launcher process exited | `true` |
| Launcher/verifier exit | **Not recorded**; captured `processExit=null` |
| taskkill | Not invoked; `taskkillExit=null` |
| Watchdog result | **FAIL** |

`process.json` retains observed process IDs, parent IDs, creation times and redacted command lines. After launcher completion it still observed MSBuild PID `34700`, VBCSCompiler PID `34580` and their console hosts `372` and `12992`, matched by PID and creation time. These are build-server residuals identified by their command lines. They were not terminated in this track. Unobserved short-lived descendants remain Not assessed.

## Verifier output readback

`verifier.stdout.txt` and `verifier.stderr.txt` are separately retained byte captures.

- Verified: 12 named qualification checks emit PASS.
- Verified: `WindowsNative_Replace_HeldReaderKeepsOldImage` emits the approved historical failure: `NativeFailure: NTSTATUS=0xc0000043; IO_STATUS=0x00000000; Win32=32`, followed by the sharing-violation message.
- Verified: the harness reports `RESULT failures=1`, and the verifier emits `TEST_EXIT=1` with `TEST_DURATION_SECONDS=18.615918`.
- Verified: classifier identifies that single Ruling 145 expected failure, reports `EXPECTED-FAIL 1 (manifest)` and `UNEXPECTED 0`; `CLASSIFIER_EXIT=0`, `CLASSIFIER_DURATION_SECONDS=0.169855`.
- Verified: `PASS Windows store qualification checks=13 expected_failures=1`.
- Verified: `TARGET_SECONDS=15`, `TARGET_MET=false`. Ruling 175 makes this target advisory; the measured invocation remained below the 60-second ceiling.
- Verified: stderr contains only empty TEST and CLASSIFIER stderr delimiters; no verifier FAIL text was emitted.

These output facts do not recover the missing OS exit code. The script's successful-output path returning 0 is source evidence, not an observed process exit, so this receipt does not promote it to a Windows readiness PASS.

## Capture defect and boundary

The capture author did not retain a stable process handle/exit observation before Windows PowerShell `Start-Process`/`Refresh` lost the available `ExitCode`; the final process object reports `HasExited` but returns a null exit. This is a defect in this receipt's measurement, not a verified verifier failure. No cause beyond the observed process-object state is claimed.

Class: process output and completion observed without preserving the OS exit. Sweep: the only direct run's metadata and stdout were compared; `TEST_EXIT=1` is the nested harness exit and cannot substitute for the missing launcher exit. Preventive control: the retained watchdog rejects any missing exit and the receipt denies readiness eligibility. The exact measurement gap must be fixed and qualified in a separately authorized run; no prose inference or second run under this track closes it.

## Validation

Validation outcomes and the final evidence commit are reported at handoff. The existing capture manifest is extended to bind these new captures and metadata; raw stdout/stderr retain their captured line endings. No pre-existing capture is rewritten. The checkpoint links this blocked receipt without relabelling earlier historical results.
