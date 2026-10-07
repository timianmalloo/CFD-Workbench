---
id: proof-wfx2-red-first
title: "WFX2 red-first receipts"
type: proof-pack
status: active
owner: "@trk-wfx2"
tags: [windows, wfx, red-first]
links:
  - { to: proof-win-smoke-reverify, rel: relates-to }
review-by: "2026-11-07"
summary: "For each WFX2 item: the check that failed on the old code and passed on the new."
---

# WFX2 red-first receipts

Each entry: the check, the observed result before the fix (red), the observed result after (green). All on macOS arm64.

## Item 1 - desktop crash frame

Check: `SelfLaunchTests` case "crash detail names the message and a frame; product shape stays type-only"
(`tests/CfdWorkbench.Desktop.Tests/SelfLaunch.cs`).

- Red: `dotnet build tests/CfdWorkbench.Desktop.Tests -c Release` fails with
  `error CS0117: 'StartupFailure' does not contain a definition for 'Describe'` (old handler printed the type only and had no testable seam).
- Green: Desktop harness (default mode) prints `SelfLaunchTests: all 7 cases passed.` and exits 0.

Finding: the Windows ring's `APP-UNHANDLED APP-CRASH System.Exception` is reproduced on macOS when `TMPDIR` is the symlinked
`/var/folders/...` (the ring uses a non-symlinked `.tmp-tests`). With the frame now printed it reads
`System.Exception: Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` at `WorkbenchTests.cs` main flow
(the `workbench.SaveAsync(conflictPath)` check). On Windows the cause is the store refusing every path
(`ProjectStore.Supported()`): the PC's W-2 B2 (Rulings 136, 137), not fixed here.
The harness now also prints `STAGE <name>` before each main-flow stage, so the last STAGE or SUITE line names the crashing stage.
