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

## Item 2 - the case file that breaks the schema

Red (old schema, old case): `uv run --with jsonschema --with pyyaml python3 cases/tools/validate-cases.py` exit 1:

```
FAIL spike03-s6-w4.yaml: 'none: Gmsh 3-D meshing failed (PLC error), no msh written' does not match '^[0-9a-f]{64}$'
39 case(s), 1 error(s)
```

Green: same command after the schema and case change: `39 case(s), 0 error(s)`, exit 0. The 7-shape self-test (`--self-test`) is red on each planted shape and green on the clean and valid no-mesh shapes.

## Item 3 - the gate

Mutant (planted after commit a7d77839, reverted with git checkout of a file with no other edits): line 19 of cases/spike03-ar5.yaml set to `sha256: "none: planted"`. `python3 tools/check-docs.py` exit 1:

```
FAIL spike03-ar5.yaml: 'none: planted' does not match '^[0-9a-f]{64}$'
../cases/tools/validate-cases.py  failed.
```

Clean tree: `python3 tools/check-docs.py` exit 0 (`39 case(s), 0 error(s)`, then `Documentation checks passed.`).
