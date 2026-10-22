---
id: proof-pvh-crash-loop
title: PVH crash loop - the properties-view part 2/2 native crash did not reproduce, and a native exit is now named as one
type: proof-pack
status: draft
owner: "@trk-d1"
tags: [pvh, desktop-tests, native-crash, harness-crash-as-verdict]
links:
  - { to: proof-pvh-red-first, rel: relates-to }
review-by: 2026-11-21
summary: >-
  Not reproduced in 6 runs of the Desktop harness alone and 6 runs of the full suite (0 crashes in 12); cause not found. The harness now prints a native child exit as a native crash with its NTSTATUS hex or signal, pinned by a check seen red then green.
---
# PVH crash loop (track D1, 2026-10-22)

Branch `d1-desktop-diagnostics`, base `176c9540`. Machine: Windows ARM64 laptop, real display, 200 percent. Earlier work:
`docs/proof/pvh/red-first.md` (the hang track: 0 hangs in 104 Mac runs).

## The evidence being chased (2026-10-22 join, Verified from its saved log)

- The child `--properties-view --part=2/2` exited **-1073740940 = 0xC0000374, STATUS_HEAP_CORRUPTION** after 56 s, having printed no checks.
- The PARTITION check then failed. Totals: 778 PASS, 2 FAIL, 23 SKIP.
- Run standalone, the same child passed 3 of 3 (34 PASS each).

This is a native crash under concurrency, not a hang. A standalone loop of the one part would not see it, so two loops were run.

## Commands

    E=tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.exe
    $E --harness                      # loop A: the whole Desktop harness (all children in parallel), stdout and stderr to separate files
    bash tools/run-tests.sh           # loop B: four test projects at once; Desktop.log copied after each run

## Loop A: Desktop harness alone (6 runs)

| run | harness exit | `SUITE --properties-view --part=2/2` | crashed child | exit hex | stderr tail |
|---|---|---|---|---|---|
| 1 | 0 | exit 0 | none | - | (none) |
| 2 | 0 | exit 0 | none | - | (none) |
| 3 | 0 | exit 0 | none | - | (none) |
| 4 | 0 | exit 0 | none | - | (none) |
| 5 | 0 | exit 0 | none | - | (none) |
| 6 | 0 | exit 0 | none | - | (none) |

Crash rate: **0 crashes / 6 runs**. No `SUITE ... exit <n>` line with n other than 0 in any run.

## Loop B: full suite (6 runs)

Runs 1 to 5 were on the unchanged formatter, run 6 on the final commit.

| run | verdict | seconds | non-zero child exits | crashed child | exit hex |
|---|---|---|---|---|---|
| 1 | TESTS-GREEN | 94 | none | none | - |
| 2 | TESTS-GREEN | 86 | none | none | - |
| 3 | TESTS-GREEN | 85 | none | none | - |
| 4 | TESTS-GREEN | 86 | none | none | - |
| 5 | TESTS-GREEN | 86 | none | none | - |
| 6 | TESTS-GREEN | 92 | none | none | - |

Crash rate: **0 crashes / 6 runs**. Run 6 project lines: Core 762 passed, 26 skipped; Analysis 260 passed; Desktop 782 passed,
23 skipped; Cli 19 passed, 2 skipped; 0 failed in each.

## Result

**Not reproduced in 6 runs of loop A and 6 runs of loop B.** 0 / 12 crashes. The Windows Application event log was not read
because no native crash fired, so there is no faulting module to record. **Cause not found.** Twelve clean runs do not prove the
crash is gone: the evidence is one crash in one join, and the rate is unknown. Nothing in this track points at our own unsafe
or native code, so no fix to the crash was attempted (an Avalonia, Skia or driver fault would be a finding, not a fix).

## The control: a native exit is named as one

Before, the harness printed `FAIL --properties-view --part=2/2 exited -1073740940`, which reads like a check verdict. Now
`DesktopChecks.ExitLine` prints, for a negative exit on Windows (an NTSTATUS, 0xC0000000 and up) or 129 to 255 on POSIX (128 plus
the signal): `FAIL <mode> exited -1073740940 (0xC0000374): native crash, not a check verdict`. Exits 124 (timeout) and 125 (drain)
keep their meaning. Note -1073740940 sits above -1073741000, so a "below -1073741000" threshold would have missed it.

The pure check `ExitLine_NativeTermination_NamedNotAVerdict` feeds -1073740940 and -1073741819 (Windows), 134 (POSIX), 124 and 1.

Red on the old formatter (Verified):

    FAIL ExitLine_NativeTermination_NamedNotAVerdict Exception: -1073740940/win: got 'FAIL m exited -1073740940' | -1073741819/win: got 'FAIL m exited -1073741819' | 134/posix: got 'FAIL m exited 134'

Green on the new:

    PASS ExitLine_NativeTermination_NamedNotAVerdict

The lesson is `HARNESS-CRASH-AS-VERDICT` in `docs/lessons/defect-classes.md`.
