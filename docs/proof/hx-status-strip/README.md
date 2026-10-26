# HX: status-strip race and encode cost (wave 3)

Two reproducers, both run from the repo root, both load-sensitive: run them with no other test suite running.

## repeat20.sh: the status-strip check, 20 times

`bash docs/proof/hx-status-strip/repeat20.sh` runs `StatusStrip_BackgroundCompletion_*` 20 times through the Desktop exe
(Debug build, `dotnet build CFDWorkbench.slnx -c Debug` first). It lists foreign `CfdWorkbench.Desktop.Tests.exe` and
`testhost.exe` processes before and after (counts of matching lines; 0 and 0 means alone). A run with a foreign process
does not count. Expected: `REPEATS pass=20 fail=0`.

What the check does: `ControllerShellTests.cs` `SupersedeAttempt`. The sampling completion runs beside the test, so it can
finish before the report is shown; that attempt is inconclusive and repeated (up to 20; the PASS note prints the attempt
reached). Forced timing: a 2000 ms delay between the command and the report turned the old check red
(`setup: sampling finished before the report`). `StatusSlot` and `ShellHost` were correct; the fault was the test's setup.

## encode.sh: `CompareFields` whole-document encode at the cap

`dotnet build tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj -c Release`, then
`bash docs/proof/hx-status-strip/encode.sh`. The document reaches 31,826,151 bytes (cap 32,000,000), seven reads per run.

Result (Release, two runs, 0 foreign test processes before; 1 Desktop test process in the listing after the second run):
median about 60 ms (60.1 and 60.6), cold first read 99.3 ms (at the 100 ms limit), later reads 58-66 ms with one 81.5 ms.
Against the 100 ms rule: under at the median, at the limit on a cold read. `AuthoringSession.cs` is unchanged.

## The 20 repeats alone (main session, 2026-10-26)

Run by the main session `wave3-coordinator` with no coder running, after a Debug build of HX head 0a80976f:
`bash docs/proof/hx-status-strip/repeat20.sh`. Output:

```text
foreign before:
0
0
REPEATS pass=20 fail=0
foreign after:
0
0
```

Verdict: 20 of 20 PASS alone; 0 foreign Desktop or testhost processes before and after. This meets the review blocker (finding 1).
