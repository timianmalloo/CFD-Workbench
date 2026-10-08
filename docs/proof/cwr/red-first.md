---
id: proof-cwr-red-first
title: "Track CWR - red-first record for the Ruling 155 plain-cause copy"
type: doc
status: done
owner: "@trk-cwr"
phase: build
tags: [copy, ruling-155, red-first]
links:
  - {to: proof-cpy-cause-rows, rel: implements}
review-by: 2027-04-01
summary: >-
  Red and green runs for the Labels cause rows COPY-412 to COPY-431, the open "Code:" line (COPY-424) and the raw-code control.
---

# Track CWR red-first (Ruling 155)

Harness: `CFD_TEST_ONLY=<prefix> tools/run-suite.sh dotnet <test dll>` (Release). Red = tests written against stubs that keep the old
text (`Labels.Refusal` returning `"{code}: This change wasn't applied. Nothing changed."`, `OpenCodeLine` returning the bare code) or, for the open alert,
the old `StartView.FailureMessage` (stashed). Green = the change.

## Analysis (`tests/CfdWorkbench.Analysis.Tests/CauseCopyTests.cs`, selector `Labels_`)

Red, `RESULT failures=4`:

- `FAIL Labels_CauseRows_Ruling155_ExactText ... DOC-IO expected Save failed: the disk couldn't be written (DOC-IO) — ...; actual Save failed: DOC-IO — the previous file is intact ...`
- `FAIL Labels_UnknownCode_GenericRowWithCode_Ruling155 ...` (old text printed the unknown code as the cause)
- `FAIL Labels_UncertainSave_NamesNeitherSavedNorNotSaved_Copy421 ... Save failed: DOC-SAVE-UNCERTAIN — the previous file is intact and your changes are kept. Retry or Save As. expected False; actual True`
- `FAIL Labels_NoRawDocDslCodeAloneOnAProductSurface ... 133 raw-code texts, first: DOC-ACCESS → Save failed: DOC-ACCESS — the previous file is intact and your changes are kept. Retry or Save As.`

Green, `RESULT failures=0` (all 15 `Labels_*` checks PASS, including the four above).

## Desktop (`ShellWindowTests.cs`, selectors `Copy_Open`, `WindowsShell_Save`)

Red, old `StartView.FailureMessage`:

- `FAIL Copy_OpenFailure_EveryCodeInSrc_PlainSentenceThenCodeLine_Ruling155 ... DOC-ACCESS: 'CFD Workbench isn't allowed to read it. The file hasn't been changed. Check its permissions in Finder, or open another file.'` (no second line)
- `FAIL Copy_OpenFailures_MatchesDesignRows ... COPY-125: the alert has no second line 'Code: FILE-NOT-FOUND'`

Green: `PASS Copy_OpenFailure_EveryCodeInSrc_PlainSentenceThenCodeLine_Ruling155`, `PASS Copy_OpenFailures_MatchesDesignRows` (the approved sentence is unchanged once the code line is removed),
`PASS Copy_OpenFailureWithFoil_AlertBandMatchesStart`, `PASS WindowsShell_SaveFailureText_IsRuling134Wording`, `PASS WindowsShell_SaveRefused_ShowsMessageInStatusStrip`.

Two existing checks pinned the old COPY-31 raw-code text and were updated to the approved rows: `WindowsShell_SaveFailureText_IsRuling134Wording` (DOC-IO, COPY-412) and
`WindowsShell_SaveRefused_ShowsMessageInStatusStrip` (DOC-TYPE, COPY-419). `Labels_SaveRefusal_Ruling134Wording` keeps the unsupported-persistence line.

## Not covered

The COPY-421 sentence is asserted at the `Labels` lookup and the controller reads it through `Labels.SaveUncertain`; no check drives a real uncertain store write to the strip.
