---
id: proof-e2-cad-defects-red-first
title: "E2 CAD defects red-first receipt"
type: proof-pack
status: active
owner: "@trk-e2"
phase: implementation
tags: [cad, flk-1, make-independent, tangent, red-first]
links:
  - { to: spec-foildsl, rel: depends-on }
  - { to: design-m12c-section-editor, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  Two CAD defects, each red before its fix. A refused section step republished Finish "Checking…"
  for the re-check of unchanged bytes. MakeIndependent threw DSL-PATCH when a tangent row still
  named a point id that the copy had rewritten.
---

# E2 CAD defects red-first receipt

Track `trk-e2`, branch `fix/cad-e2-defects`, 2026-10-05. Plan: `docs/coordination/round-oct05.md`.

| Test | What it catches | Red commit | Green commit |
|---|---|---|---|
| `SectionStep_Refused_NeverShowsChecking` | A refused abscissa step shows Finish "Checking…" (the button help and the reason box) while the unchanged draft is re-checked | `585eefba599d25c3f844daed047c924f0095171e` | `d2f286cff917c4c4755ab52a625ab541cb31f855` |
| `MakeIndependent_TangentRow_NoDslPatch` | `FoilSource.MakeIndependent` throws `DSL-PATCH` when a tangent row names a point whose id the copy rewrites | `21cd34696da73a6fa53f7da990f78a9874ee3789` | `bba1aa8d9293e16e8038bdb63922b95892050475` |

## FLK-1

Observed red, suite `--section-editor`, `CFD_TEST_ONLY=SectionStep_Refused_NeverShowsChecking`, exit 1:

```
FAIL SectionStep_Refused_NeverShowsChecking Exception: A refused step showed Finish "Checking…" while it re-checked: reason 'Checking…', help 'Checking…', box 'Checking…'
```

`QueueSectionStep` publishes `FinishReason = "Checking…"` and clears the certificate as soon as a step is queued, before the outcome is known. That write stays for an in-flight step whose bytes will change. On refusal, `ApplySectionStepAsync` asks for the certificate again and notifies while the placeholder is still set. The assessment replaces `FinishReason` only when the certificate returns. The section is dirty and cannot finish while the certificate is clear, so the mode bar shows that placeholder for the whole re-check.

The refusal path now puts the previous certificate back when the draft id and generation are unchanged, before that re-check is published. A later queued step does not overwrite the saved certificate with the placeholder. A step that lands still shows "Checking…" until its own certificate returns. After the fix the same check printed `PASS SectionStep_Refused_NeverShowsChecking`. `SectionMode_StepsWhileApplying_QueueInOrder` and `SectionEditor_UniqueSectionAbscissaStep_RefusedInStripFinishUnchanged` also passed.

## MakeIndependent

Observed red, `CFD_TEST_ONLY=MakeIndependent_TangentRow_NoDslPatch`, exit 1:

```
FAIL MakeIndependent_TangentRow_NoDslPatch ContractError: DSL-PATCH
```

The canonical fixture already uses `cv-N` ids, so its tangent row still names `cv-5` after the copy and that call succeeds. The same section with `pt-N` ids parses, then fails. `RewriteIds` rewrites the point-id tokens and leaves the tangent row. The row still says `"pt-5"` while the points are `cv-0` … `cv-10`. Parse reports `DSL-REFERENCE` ("Tangent row does not name a point."), and `MakeIndependent` surfaces every parse failure as `DSL-PATCH`.

The copy now replaces each quoted old id in the window after the ids list, which is where the tangent block sits, with the quoted `cv-N` id of the same vertex. Kind and angle stay as written. The profile that was copied is not edited. After the fix the same check printed `PASS MakeIndependent_TangentRow_NoDslPatch`. `Profile_MakeIndependent_MiddleStationSplitsIntervals` also passed.
