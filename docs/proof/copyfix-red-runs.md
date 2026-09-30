---
id: proof-copyfix-red-runs
title: COPYFIX red-first runs — M1.2a copy decisions, two missing states, atomic Remove from Recent
type: proof-pack
status: in-review
owner: "@track-copyfix"
phase: implementation — M1.2a review fixes
tags: [app-shell, desktop, copy, recent-files, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Red runs for track COPYFIX. Four new Copy_* checks failed against the U1FIX build and one new
  preference-store check failed against a Remove op with no Apply support. Two checks pinned strings
  U1FIX had already built, so their red evidence is a string mutant, recorded as such.
---

# COPYFIX red-first runs

Track COPYFIX closes review findings C-4, C-5, C-9, C-12 and C-14 (`docs/reviews/app-shell-native.md` §2) and
seam `req-01M3SG5TE62HKD3T7CPYMDXVZ3` (atomic Remove from Recent). The copy record is `DESIGN.md` §7,
COPY-140 to COPY-148. Every run below used `dotnet run -c Release --no-build` on the named suite, with `TMPDIR`
set to the non-symlinked `.tmp-tests/` that `tools/run-tests.sh` uses.

## 1. Copy checks against the U1FIX build (base 52d8a09)

The COPY rows and the checks were added first. The build was unchanged. `--shell-window` exited 1.

```text
PASS Copy_SpanNotAssessed_MatchesDesignRow
FAIL Copy_StartAlert_NoUncataloguedFallback InvalidOperationException: StartView.ShowAlert still accepts an uncatalogued fallback message
FAIL Copy_ExampleMissing_MatchesDesignRow InvalidOperationException: COPY-143: expected '“example.foil” didn't open. The built-in example is missing or damaged. Nothing was overwritten. · New foil · Open another file…', built '“example.foil” didn't open. It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed. · Open another file… · New foil'
PASS Copy_Dismiss_MatchesDesignRow
FAIL Copy_AlertBand_IdCandidateRefusedAcceptFailed_MatchDesignRows InvalidOperationException: COPY-140: expected '“sample.foil” has no control-point IDs. CFD Workbench can add them. The file hasn't been changed. · Accept candidate IDs', built 'Explicit candidate IDs available for insertion. · Accept candidate IDs'
FAIL Copy_RecentNotCleared_StatusAndTryAgain InvalidOperationException: COPY-147: expected 'The recent-files list wasn't cleared: it was saved by a newer version of CFD Workbench. The list is unchanged. · Try again', built '', unchanged=True
```

## 2. Pins proved by mutant

`Copy_SpanNotAssessed_MatchesDesignRow` and `Copy_Dismiss_MatchesDesignRow` passed on first run: U1FIX (015df0f)
had already built the §11 Span-not-assessed string and the Dismiss label. A red-first run is not possible for a
string that exists. The evidence is a mutant instead. The Span string in `PropertiesPane.axaml.cs` and the Dismiss
label in `StartView.axaml` were edited in the working tree, `--shell-window` was run (exit 1), and both files
were restored before any commit.

```text
FAIL Copy_SpanNotAssessed_MatchesDesignRow InvalidOperationException: COPY-145: expected 'The new span couldn't be checked. Span is unchanged. Try again or enter a different value.', built 'The new span could not be checked. Try again or enter a different value.', committed=False
FAIL Copy_Dismiss_MatchesDesignRow InvalidOperationException: COPY-144: expected 'Dismiss', built 'Close', 'Dismiss'
```

## 3. Atomic Remove from Recent

`RecentOp.Remove(path)` was added with no `RecentList.Apply` support. Core subset, exit 1:

```text
FAIL Recent_Remove_KeepsEveryOtherEntry InvalidOperationException: Expected /abs/c.foil,/abs/a.foil; actual /abs/c.foil,/abs/b.foil,/abs/a.foil
PASS Recent_RemoveWriteFails_ListUnchanged
```

`Recent_RemoveWriteFails_ListUnchanged` passes before and after: the store's single compare-and-swap write
already publishes nothing on a failed save. The check pins that property for the new op. After `Apply` handled
`Remove`, both checks and `Recent_Conflict_ReappliesAdd` and `Recent_ClearFails_ReportedNotCleared` passed.
