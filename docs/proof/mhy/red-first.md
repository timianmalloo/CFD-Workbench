---
id: proof-mhy-red-first
title: "MHY red-first receipts"
type: proof-pack
status: active
owner: "@track-mhy"
phase: implementation
tags: [mhy, red-first]
links:
  - { to: proof-mhy-case-schema, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  Red then green runs for the MHY track items.
---

# MHY red-first receipts

## Item 1 - save-failure wording (Ruling 134)

Command: `CFD_TEST_ONLY=WindowsShell_Save tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.dll --shell-window`

Red (old code, tests already asserting the ruling's text and a non-info kind):

```
FAIL WindowsShell_SaveRefused_ShowsMessageInStatusStrip InvalidOperationException: A thrown save showed 'DOC-TYPE: Save was not acknowledged. Resolve the refusal before retry.' (kind error); threw DOC-TYPE
FAIL WindowsShell_SaveFailureText_IsRuling134Wording InvalidOperationException: 'DOC-UNSUPPORTED-PERSISTENCE: Save was not acknowledged. Resolve the refusal before retry.' / 'DOC-IO: Save was not acknowledged. Resolve the refusal before retry.'
```

Green (after Labels.SaveRefusal and the two call sites):

```
PASS WindowsShell_SaveRefused_ShowsMessageInStatusStrip
PASS WindowsShell_SaveFailureText_IsRuling134Wording
```

Also green: `Labels_SaveRefusal_Ruling134Wording` (Analysis.Tests, new; written after the code, so it is a pin, not a red-first); `python3 tools/check-copy-ids.py` exit 0 (411 rows).

The returned-result path (store returns DOC-UNSUPPORTED-PERSISTENCE) is the second assertion of WindowsShell_SaveRefused_ShowsMessageInStatusStrip: on the Mac a relative path makes the real store return the code, and the strip shows the Windows sentence in warning kind.
