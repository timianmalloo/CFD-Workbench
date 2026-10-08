---
id: proof-wsf-red-first
title: "READER-SHARE red-first receipt"
type: proof-pack
status: active
owner: "@track-wsf"
phase: implementation
tags: [windows, proof, ruling-145]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red-first and behaviour-test receipt for the reader-share gate and the shared UserFile opener (Ruling 145 (1), (6)).
---
# WSF red-first: reader share flags (Ruling 145 (1), (6))

## Gate `tools/check-reader-sharing.py`

Red, against main's code (2062dff8, before the fix), `python3 tools/check-reader-sharing.py`, exit 1, 0.26 s wall:

```
READER-SHARE: src/CfdWorkbench.Cli/Program.cs:26: await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
READER-SHARE: src/CfdWorkbench.Desktop/Shell/ShellHost.cs:1402: try { dat = await File.ReadAllBytesAsync(path); }
READER-SHARE: src/CfdWorkbench.Persistence/SectionLibrary.cs:37: using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
READER-SHARE FAIL: open user files with CfdWorkbench.Persistence.UserFile.OpenRead (FileShare.Read | FileShare.Delete)
```

Green, after the three sites moved to `UserFile`: `READER-SHARE ok: 0 readers without delete sharing`, exit 0.
Self-test: `READER-SHARE self-test ok: 9 offenders caught, 8 clean passed`.
Readers vs writers: a `FileShare.Read` hit is skipped when its statement opens for writing (`FileAccess.Write` or `FileMode.Append/Create/CreateNew/Truncate`, no `FileAccess.Read`); `PaneDiagnostics.cs` (`FileShare.ReadWrite`) never matches the token.

## Behaviour test `Library_UserFileHeldReader_SurvivesReplaceByRename` (Core harness)

Mac run: `CFD_TEST_ONLY=Library_UserFileHeld tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`
gives `PASS Library_UserFileHeldReader_SurvivesReplaceByRename`, `RESULT failures=0`.
macOS ignores share modes, so this test passes on both the old flags and the new ones here; it is red on the old flags only on Windows (the PC ring).
The Mac-visible red is the gate above.
