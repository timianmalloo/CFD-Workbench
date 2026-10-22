---
id: proof-verify-gates-2026-10-22
title: "Verify gates on Windows ARM64: both exit 4, Not assessed"
type: proof-pack
summary: "Both macOS verify gates were run on Windows ARM64 at 176c9540. Each exits 4 with a Not-assessed line and launches no dotnet process, so the run is not evidence that the gates pass after the --harness change."
status: active
owner: "@timianmalloo"
tags: [verify-gates, windows, arm64, not-assessed]
review-by: "2026-11-30"
links:
  - { to: coordination-next-best-six, rel: relates-to }
---

# Verify gates on Windows ARM64

**Exit 4 is Not assessed, not green. On Windows neither gate launches dotnet, so this run is not evidence that the gates
pass after the `--harness` change. A real run needs a macOS host.**

## Environment

| | |
|---|---|
| commit | `176c954011afdd6dacf929570b38b357e7c884e7` (`main` at the time; run from a worktree at that commit) |
| OS and architecture | Windows 11, ARM64 |
| Python | 3.14.7 |
| .NET SDK | 10.0.402 |

## Gate 1: `tools/verify-application-core.py`

Command, from the repo root:

    python tools/verify-application-core.py > core.out 2> core.err; echo "exit $?" > core.exit

- exit code: **4**
- stdout: empty
- stderr:

      Windows gate/runtime: Not assessed; no dotnet process launched

## Gate 2: `tools/verify-application-adapters.py`

Command, from the repo root:

    python tools/verify-application-adapters.py > adapters.out 2> adapters.err; echo "exit $?" > adapters.exit

- exit code: **4**
- stdout: empty
- stderr:

      Windows-host process ownership and runtime: Not assessed; no child launched

## The `--harness` argument

Both scripts already pass `--harness` to the test assembly. Checked with `grep -n -- "--harness"`:

- `tools/verify-application-core.py` lines 231 and 244
- `tools/verify-application-adapters.py` line 485

These lines were not executed in this run, because both gates stop at the Windows guard before any child process starts.

## Findings

None. Both exits and both stderr lines match the expected Windows behaviour. The plan row named a different message
("Windows process ownership adapter: Not assessed"); that text is unreachable on Windows and did not appear.
