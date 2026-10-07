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

## Item 2 - JSON line endings

Check: Core `Json_IndentedWriters_PinLfNewLine` (`tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs`). `Environment.NewLine` cannot be forced to
`\r\n` on macOS, so the failing part on old code is the structural scan (every `src` file that indents JSON must pin `NewLine = "\n"`).

- Red: `FAIL Json_IndentedWriters_PinLfNewLine InvalidOperationException: indented JSON without NewLine = "\n": CfdWorkbench.Persistence/PreferenceStore.cs, .../LayoutCodec.cs, .../RecentList.cs, CfdWorkbench.Core/AuthoringSession.cs, CfdWorkbench.Cli/Program.cs`
- Green (after pinning the five sites and `RunStoreTests.NoRunByteIdentical`): `PASS Json_IndentedWriters_PinLfNewLine`.

## Item 3 - symlink privilege in tests

Check: Core `PrefStore_DirectoryLink_WindowsBranchUsesJunction` forces the Windows branch with a fake runner and asserts the command is `cmd.exe /c mklink /J <link> <target>` and that the macOS branch never shells out and creates a real link.

- Red: the helper `TryDirectoryLink` did not exist; the old sites called `Directory.CreateSymbolicLink` directly (Windows ring: `IOException: A required privilege is not held by the client`). The check was written with the helper, so its red is the missing member (compile error), not a run.
- Green: `PASS PrefStore_DirectoryLink_WindowsBranchUsesJunction`; `PASS PrefStore_SymlinkedDirectory_SessionOnly` and `PASS PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites` unchanged on macOS.
