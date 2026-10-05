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
  named a point id that the copy had rewritten. The retarget now edits each row's id token span.
---

# E2 CAD defects red-first receipt

Track `trk-e2`, branch `fix/cad-e2-defects`, 2026-10-05. Plan: `docs/coordination/round-oct05.md`.

| Test | What it catches | Red commit | Green commit |
|---|---|---|---|
| `SectionStep_Refused_RestoresCertificate` | A refused abscissa step shows Finish "Checking…" (the button help and the reason box) while the unchanged draft is re-checked. The check covers that re-check window only | `585eefba599d25c3f844daed047c924f0095171e` | `d2f286cff917c4c4755ab52a625ab541cb31f855` |
| `MakeIndependent_TangentRow_NoDslPatch` | `FoilSource.MakeIndependent` throws `DSL-PATCH` when a tangent row names a point whose id the copy rewrites | `21cd34696da73a6fa53f7da990f78a9874ee3789` | `bba1aa8d9293e16e8038bdb63922b95892050475` |
| `MakeIndependent_TangentsBeforeIds_RetargetsRow` | A tangents block written before the ids list is skipped, and the copy throws `DSL-PATCH` | observed red below, before the span edit | `db7195a28abb1afbb726083420f2f47b7dca81ef` |
| `MakeIndependent_CollidingIds_TangentNotCascaded` | Rewriting one id also rewrites a later id, or a comment that quotes an id | observed red below, before the span edit | `db7195a28abb1afbb726083420f2f47b7dca81ef` |
| `SectionStep_LandedOrExit_ClearsPriorCertificate` | A landed step, or leaving the editor, keeps the certificate saved for a later refusal | observed red below, before the clear | the clear commit |

## FLK-1

Observed red, suite `--section-editor`, `CFD_TEST_ONLY=SectionStep_Refused_NeverShowsChecking` (renamed `SectionStep_Refused_RestoresCertificate`), exit 1:

```
FAIL SectionStep_Refused_NeverShowsChecking Exception: A refused step showed Finish "Checking…" while it re-checked: reason 'Checking…', help 'Checking…', box 'Checking…'
```

`QueueSectionStep` publishes `FinishReason = "Checking…"` and clears the certificate as soon as a step is queued, before the outcome is known. That write stays for an in-flight step whose bytes will change. On refusal, `ApplySectionStepAsync` asks for the certificate again and notifies while the placeholder is still set. The assessment replaces `FinishReason` only when the certificate returns. The section is dirty and cannot finish while the certificate is clear, so the mode bar shows that placeholder for the whole re-check.

The refusal path now puts the previous certificate back when the draft id and generation are unchanged, before that re-check is published. A later queued step does not overwrite the saved certificate with the placeholder. A step that lands still shows "Checking…" until its own certificate returns. "Checking…" from the queue until the refusal is unavoidable, because the refusal is not known beforehand. The check covers the re-check window after the refusal only. It was renamed `SectionStep_Refused_RestoresCertificate`. After the fix the same check printed `PASS SectionStep_Refused_NeverShowsChecking`. `SectionMode_StepsWhileApplying_QueueInOrder` and `SectionEditor_UniqueSectionAbscissaStep_RefusedInStripFinishUnchanged` also passed.

## MakeIndependent

Observed red, `CFD_TEST_ONLY=MakeIndependent_TangentRow_NoDslPatch`, exit 1:

```
FAIL MakeIndependent_TangentRow_NoDslPatch ContractError: DSL-PATCH
```

The canonical fixture already uses `cv-N` ids, so its tangent row still names `cv-5` after the copy and that call succeeds. The same section with `pt-N` ids parses, then fails. `RewriteIds` rewrites the point-id tokens and leaves the tangent row. The row still says `"pt-5"` while the points are `cv-0` … `cv-10`. Parse reports `DSL-REFERENCE` ("Tangent row does not name a point."), and `MakeIndependent` surfaces every parse failure as `DSL-PATCH`.

The copy now replaces each tangent row's id token, the span the parser recorded, with the quoted `cv-N` id of the same vertex. Kind and angle stay as written. `Guard.Require` fails the copy when a row has no span or a row is left unedited. The profile that was copied is not edited. After the first fix the same check printed `PASS MakeIndependent_TangentRow_NoDslPatch`. `Profile_MakeIndependent_MiddleStationSplitsIntervals` also passed.

The text search had three misses. A differently escaped id never matched. A row written before the ids list sat outside the window `[last id token end, InsertAt)`. A comment that quoted an id was rewritten with it. Repair cycle 1 records the id token on `TangentRow` and emits one positional edit per row.

Observed red, before that edit, `CFD_TEST_ONLY=MakeIndependent_TangentsBeforeIds_RetargetsRow,MakeIndependent_CollidingIds_TangentNotCascaded`, exit 1:

```
FAIL MakeIndependent_TangentsBeforeIds_RetargetsRow InvalidOperationException: Fixture did not parse: DSL-SYNTAX Source does not satisfy the syntactic contract.
FAIL MakeIndependent_CollidingIds_TangentNotCascaded InvalidOperationException: Expected True; actual False
```

The first fixture puts `tangents` before `ids`. The parser accepted only the canonical order, so the row never reached the retarget. The parser now reads either order. A second ids or tangents block still fails at `}`. The canonical writer still emits ids, then tangents. `docs/specs/foildsl.md` still says the block is after `ids`. That sentence was not edited.

The second fixture puts the anchor's id at `cv-1` and index 1's id at `cv-5` (index 0 is not an interior anchor, so the list cannot literally start `["cv-1","cv-0",...]`). The tangent stayed on `cv-5` even under the text search, because edits were collected against the original text and the second id did not see the first replacement. The same search rewrote `# keep "cv-1" here`, which is why the check was red. After the span edit the row is `cv-5`, index 1 is `cv-1`, and the comment is unchanged. The same three checks printed PASS, and `Profile_MakeIndependent_MiddleStationSplitsIntervals` passed with them.

## Saved certificate

`sectionBeforeChecking` was set when a step was queued and never cleared. A landed step and an exit from the editor both left it set.

Observed red, suite `--section-editor`, `CFD_TEST_ONLY=SectionStep_LandedOrExit_ClearsPriorCertificate`, exit 1:

```
FAIL SectionStep_LandedOrExit_ClearsPriorCertificate Exception: A landed step kept the certificate from before the bytes changed
```

A landed step, an undo or redo that moves, Finish, Cancel, switching sections, recovery and Dispose now clear it. A refusal does not: that is the re-check window. `DraftId` is one id per draft. `EnterSectionAsync` mints it with `Guid.NewGuid`, and `AuthoringSession.BeginSectionDraftCore` rejects an id this session already retired. A resumed draft keeps that id. `Generation` distinguishes its steps. The guard compares that pair, so it does not need another identity. After the clear the check printed `PASS SectionStep_LandedOrExit_ClearsPriorCertificate`, and `SectionStep_Refused_RestoresCertificate` still passed.
