---
id: investigation-section-catalog-apply
title: "Investigation: a catalog row that cannot be applied, and an edited section still listed as its catalog original"
type: investigation
status: draft
owner: "@timianmalloo"
phase: investigate
tags: [catalog, section-editor, provenance, browser-pane, rendered-surface, desktop]
links:
  - { to: design-m12d-catalog, rel: relates-to }
  - { to: mockup-m12d-catalog, rel: relates-to }
  - { to: design-m12c-section-editor, rel: relates-to }
  - { to: adr-0008-section-library, rel: relates-to }
  - { to: decision-catalog-admission-classes, rel: relates-to }
  - { to: proof-section-catalog-apply, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: note-20261023-smoke-test-row-assumption, rel: relates-to }
review-by: "2026-11-23"
summary: >-
  The operator's smoke test could select a catalog foil but not apply it, and an edited catalog section stayed listed as
  its original. Verified: the catalog sheet renders its 16 rows without coordinates exactly like its 3 choosable rows,
  with the refusal only in the detail line; a fault inside the fit leaves the sheet on "Fitting…" with no reason; Enter
  during the fit is dropped silently; the real route applies every choosable row. Verified: the Browser station row and
  the Properties section title compose the source text from the origin alone and drop the modified flag that the chip
  and station card carry. Three classes registered; four repair phases proposed; stopped for review.
---

# Investigation: section-catalog-apply — a catalog row that cannot be applied; an edited section still listed as its original

- **Status:** Root cause verified · fix proposed · **stopped for human review**
- **Severity / tier:** T1 (two user-facing defects in the section editor; no data loss; the second shows a wrong label on a saved-state surface)
- **Reported by / date:** the operator, smoke test, 2026-10-23
- **Related spec/design-slice:** `docs/design/m12d-catalog.md` (§3.4 admission, §11.4 component states, §14.1 behaviour trace), `docs/mockups/m12d-catalog.html` (screen 2, `.opt.dis`), ADR-0008 §1–3, `docs/notes/catalog-admission-classes.md`
- **Reproduction scripts and observed lines:** `docs/proof/section-catalog-apply/README.md`

Labels: **Verified** = observed in this session (a probe line or a file:line); **Inferred** = reasoned from code, not run; **Flagged** = cannot be settled here.

## Symptom

1. *"When I am in the section view and I attempt to change a section to one from the catalog, I can select a foil from the catalog but I cannot apply it."* Expected: choosing a row and pressing Replace replaces the section. Actual: the row selects; Replace does nothing.
2. *"When I edit an existing section based on a given foil section (e.g. the default NACA foil section) it should no longer be listed as the original foil section — it should be 'custom'."* Expected: after an edit the section is no longer listed as the catalog original. Actual: a listing still names the original.

The operator's words name the surfaces loosely ("section view", "listed"). The grounding below names them.

## Reproduction

**Defect 1, the verified mechanism (Verified, `Probe_DisabledRow_RenderedState`).** Open the Example, *Edit section…* at Root, *Section ▾ → Replace from catalog…*. The sheet lists 19 rows; 3 carry coordinates (NACA 0009, 0012, 4412) and 16 do not (7 NACA rows "Not generated in this build", 8 Eppler "Pending admission", Speer H105 "Cite only"). Every row renders with `IsEnabled=True`, `Opacity=1`, the same foreground, no style class, and the 7 pending NACA rows carry the same " · GEN" tag as the 3 choosable ones. Select NACA 63-412: the detail line shows the reason, Replace is disabled, the status strip does not change; a Replace click changes nothing. From the operator's seat: a row that looks like every other row was chosen and cannot be applied.

**Defect 1, the latent mechanism (Verified, `Probe_PreviewInternalFault_DialogState`).** If the fit throws anything but a `ContractError`, the sheet stays at "Fitting…" with Replace disabled; `CurrentPreview`, `RefusedPreview` and `PreviewFault` are all null and no status text is written. Telemetry records `INTERNAL-ERROR`; no surface does.

**Defect 1, the second latent mechanism (Verified, `Probe_EnterDuringFit_ChoosableRow`, raised by the Test Architect).** Choose a choosable row and press Enter while its fit is still running (140–240 ms by design, longer under load): `OnSearchKeyDown`/`OnListKeyDown` call `ApplyAsync`, which returns when `CurrentPreview` is null. Nothing happens, nothing is queued, no text changes; after the fit lands the cursor is still 0, and only a second Enter applies. An operator who presses Enter on a row and sees nothing has "selected but cannot apply".

**Defect 1, the route that works (Verified, `Probe_RealRoute_ReplaceFromCatalog_Applies`, `Probe_CatalogSweep_*`).** Through the real shell command, the real dialog over an owner window and a Replace click, NACA 0012 applies (cursor 1, sheet closed, chip "Catalog original · NACA 0012"). All three rows with coordinates land on the shared Example; on an independent Root, 4412 is refused `CAT-SPACING` with the chain button offered, as designed (Ruling 71).

**Defect 2 (Verified, `Probe_SourceSurfaces_AfterEdit`).** Replace with NACA 0012, then move one point. The bytes read `gen:naca-0012 modified`. The source chip reads "Modified from NACA 0012", the station card "Modified from NACA 0012 (GEN)". The Browser pane's station rows read "η 0 · section-a · NACA 0012" and "η 1 · section-a · NACA 0012": the modified flag is dropped on that surface only.

Note on the operator's "default NACA foil section" (Verified): the Example's one profile `section-a` carries no provenance (`src/CfdWorkbench.Desktop/Assets/example.foil`), so its chip reads "Source not recorded". A section reads "NACA …" only after a Replace from the catalog. The reproduction therefore starts from a Replace.

## Timeline (Change Analysis)

- 2026-10-04 `0ebe5855` *feat(dlg): add catalog and save surfaces with refused preview* — the sheet, its `Filter()` with `AutomationProperties.SetItemStatus(item, "disabled")` and no visual state, and `BrowserPane.StationSourceText` as "name · SourceName", all land in one commit (Verified, `git log -S`). Neither site changed since. These are design-time omissions, not regressions.
- The catalog has carried 3 choosable rows of 19 since DR-M12D-2 chose "a · NACA 0009, 0012, 4412 (closed form); the other seven listed disabled" (`docs/mockups/m12d-catalog.md` DR-M12D-2; `catalog.tsv`).

## System map

```
catalog.tsv (19 rows, 3 with coordinates)
   └─ Catalog.Load() ──► CatalogSnapshot ──► CatalogDialog.Filter()
                                              ├─ row: ListBoxItem{Tag=choice, text "name · CLASS"}   ← automation status only
                                              └─ SelectionChanged → SelectRow() → controller.PreviewReplace(choice)
controller.PreviewReplace → StartPreview (Task.Run: session.PreviewReplace) → FinishPreview (UI thread)
   ├─ landed   → CurrentPreview, PreviewChoice           → dialog.OnPreviewChanged: Replace enabled
   ├─ refused  → RefusedPreview / PreviewFault(Contract) → detail line = reason, Replace off, chain if CAT-SPACING
   └─ non-contract fault or draft changed → all null     → dialog stays "Fitting…"            ← latent
Enter (search box or list) during the fit → ApplyAsync → `CurrentPreview is null` → return   ← latent, intent dropped
Replace click → ApplyAsync → ApplyReplaceAsync → ApplySectionStepAsync(Replace) → bytes with provenance "gen:<id>"
any later step but Replace/Import → Provenance.MarkModified → "gen:<id> modified"  (one writer, by design)
readers of the provenance string:
   controller.SourceChip / StationSource  → Provenance.ChipText(ChipSourceName)    "Modified from NACA 0012"
   PropertiesView.SourceDisplay           → Provenance.ChipText(SourceName)        "Modified from NACA 0012 (GEN)"
   SaveSectionDialog                      → controller.SourceChip                  "Provenance: Modified from NACA 0012"
   BrowserPane.StationSourceText          → name + " · " + SourceName              "section-a · NACA 0012"   ← drops Modified
```

Structure produces the behaviour: the sheet's row has one state channel (automation) where the mockup has two (automation and the muted name, `.opt.dis .on`, `docs/mockups/m12d-catalog.html:159`); the source text has one writer but four composers.

## Hypotheses considered

Method: Ishikawa over the dialog's four state sources (row model, preview path, apply path, rendered surface), each verified by a probe; then Change Analysis to date the sites.

| Hypothesis | Predicts we'd see… | Evidence for | Evidence against | Verdict |
|---|---|---|---|---|
| H1 The apply wiring fails in the real app (owner window, focus return, dispatcher) | a choosable row previews, Replace enabled, click does nothing | — | `Probe_RealRoute`: NACA 0012 applied through `RunCommand` + real dialog + click | **Ruled out** for the three choosable rows on the Example (Verified) |
| H2 The chosen row was refused by the fit/spacing rule (Ruling 71) | detail line with the µm reason; chain button on CAT-SPACING | 4412 on an independent Root refuses with the chain offered | on the shared Example all three land; a refusal names its reason and offers the chain | Possible on another document; **not the Example's shape** (Verified) |
| H3 The chosen row has no coordinates and looks choosable | reason in the detail line, Replace off, nothing else differs | 16 of 19 rows; identical rendering; 7 pending NACA rows tagged " · GEN"; mockup mutes the name, build does not | the operator did not name the row (Flagged) | **Verified mechanism; the operator's path is the stated assumption** (note-20261023) |
| H4 The fit faulted and the sheet never left "Fitting…" | "Fitting…" forever, Replace off, no status | `Probe_PreviewInternalFault`: exactly that | no evidence the smoke test's fit faulted; the three generated shapes fit the Example | **Verified latent sibling**, not shown to be the instance |
| H5 Analysis area was active (`ANA-EDIT-INERT`) | PreviewReplace throws inside the constructor; the sheet never opens | — | the operator saw rows and selected one | Ruled out (Inferred from `CatalogDialog` ctor → `Filter()` → `SelectRow()` → `PreviewReplace` throw) |
| H6 (defect 2) The modified flag is never written | provenance stays `gen:naca-0012`; chip stays "Catalog original" | — | bytes read `gen:naca-0012 modified`; chip and card say "Modified from" | **Ruled out** (Verified) |
| H7 (defect 2) One surface composes the source text without the flag | that surface alone names the original | Browser row "section-a · NACA 0012" while chip/card say "Modified from"; `BrowserPane.axaml.cs:109` uses `SourceName` only | — | **Verified root cause of defect 2** |
| H8 (defect 2) The wording is the complaint ("Modified from X" vs "custom") | every surface says "Modified from"; operator still objects | the operator asked for "custom" | the Browser row says neither | Product decision, raised as DR-INV-1 |
| H9 Enter pressed on a choosable row while its fit runs (Test Architect) | nothing happens, no text, no queued apply; a later Enter applies | `Probe_EnterDuringFit`: cursor 0, detail and status unchanged, second Enter applies | not shown to be the smoke test's path (Flagged) | **Verified latent sibling**; the keyboard path of "select, cannot apply" |

## Verified root cause

**Defect 1 — a component state that reaches the automation tree and not the rendered surface.** `CatalogDialog.Filter()` (`src/CfdWorkbench.Desktop/CatalogDialog.axaml.cs`, the `row.Reason is not null` branch) writes `ItemStatus "disabled"` and `HelpText` and nothing visual: no class, no pseudo-class, no style, no change to the tag text (the tag is `entry.Class`, so a pending GEN row reads " · GEN" like a choosable one). The axaml has no Style for a disabled row; the mockup has one (`.opt.dis .on { color: var(--mute) }`). The design asked for rows "listed aria-disabled with their reasons" (§3.4) and a disabled column in the component-state table (§11.4) and the build met the automation half only.
*Necessary:* with a visual state the rows are distinguishable and the choice is informed (Inferred: not yet built; the mockup renders it). *Sufficient:* `Probe_DisabledRow_RenderedState` shows the identical rendering, and selecting such a row yields exactly "select, cannot apply" (Verified).
*Systemic, not proximate:* the proximate line is "no Style for the row"; the systemic cause is that the harness proves dialog states by model properties (`DisabledReason`, `HelpText`, "Enter changes nothing") and never reads a rendered property, so a state missing from the surface stays green (E-rules: prove the rendered surface).

**Defect 1, latent — a fault with telemetry and no surface.** `FinishPreview`'s drop branch sets `PreviewFault = fault as ContractError ?? …` (`WorkbenchController.cs` ≈2335), so a non-contract exception, already recorded as `INTERNAL-ERROR`, leaves every user-facing field null; `CatalogDialog.OnPreviewChanged` changes nothing when all three are null and the detail line keeps "Fitting…". The same branch is taken when the draft changed during the fit (`!sameDraft`), which also leaves no reason (Inferred from code; the probe covered the fault).

**Defect 1, second latent — an input during a loading state is dropped.** `ApplyAsync` (`CatalogDialog.axaml.cs`, the guard at its top) returns silently when `CurrentPreview` is null, and both key handlers route Enter there without checking the state. The loading state has no rule for input: not refused with a reason, not queued to run on landing. `Probe_EnterDuringFit_ChoosableRow` shows the drop and that the same key applies once the fit has landed (Verified). Nothing is recorded, so a smoke test that hit this path left no trace (the dialog writes no telemetry on select or on a dropped apply: Verified by grep, no `RecordCatalog` in `CatalogDialog.axaml.cs`).

**Defect 2 — one display quantity composed five ways; two composers drop a flag.** `BrowserPane.StationSourceText` (`src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml.cs:109`) returns `profile + " · " + PropertiesView.SourceName(foil, profile)`, and the Properties pane's section-group title (`PropertiesView.cs:1325`) composes `{profile} · {SourceName(...)} · degree {n}` the same way; `SourceName` maps the origin to a designation and never reads `Provenance.Modified`. The chip, station card Source row and Save dialog go through `Provenance.ChipText`, which does. The stored field has one writer (`MarkModified`, by design); the derived text has five composers and two designation lookups (`WorkbenchController.ChipSourceName`, `PropertiesView.SourceName`).
*Necessary and sufficient (Verified):* the same bytes produce "Modified from NACA 0012" through `ChipText` and "NACA 0012" through `StationSourceText`; the only difference is the composer.

## Causes ruled out

- H1 apply wiring: the real route applies (Verified, probe line "after-click visible=False cursor=1").
- H2 as the Example's instance: all rows with coordinates land on the shared Example (Verified, sweep). It remains possible on another document, and then the sheet does say why and may offer the chain.
- H5 Analysis area: the sheet could not have opened (Inferred, constructor path).
- H6 flag never written: the bytes carry ` modified` after one Move (Verified).
- "The dialog's Replace handler drops the click": `ApplyAsync` returns early only for a disabled/damaged choice, a null `CurrentPreview`, or `replacing`; with a landed preview it applied (Verified).

## Specific fix(es) for this instance

**F1 — the non-choosable row shows its state (defect 1).**
- *Fix:* in `Filter()`, a row with a reason gets a `disabled` style class; `CatalogDialog.axaml` styles `ListBoxItem.disabled` with the muted ink token (`MutedBrush`, already used by `CombPlate.axaml`) and keeps the row reachable (no `IsEnabled=false`, per §3.4 "disabled but reachable"); the tag names the state, not the class: " · not in this build" / " · pending" / " · cite only" (COPY to be numbered). The detail line keeps the full reason.
- *Why systemic:* it closes the surface the automation channel already carries, and Phase 4 makes the pair a rule.
- *Rollback:* revert the Style and the class; no data or contract changes.
- *Failing-first regression test:* `CatalogDialog_NonChoosableRow_RenderedDistinct` (DLG): for one choosable and one pending row, the rendered `Foreground`/class differ and the pending row's tag text is not " · GEN". Red today (`Probe_DisabledRow_RenderedState` prints identical values).

**F2 — the loading state has an outcome on every branch (defect 1, both latent paths).**
- *Fix:* (i) in `FinishPreview`'s drop branch, a non-contract fault maps to `PreviewFault = new ContractError("CAT-PREVIEW-FAULT", "The preview couldn't be computed. Nothing changed.")` and a draft change during the fit to `("DSL-STALE", "The section changed while fitting. Choose the row again.")`; `OnPreviewChanged` already renders any `PreviewFault`, and the shell already reports it. (ii) Enter during the fit on a choosable row arms a pending apply that `OnPreviewChanged` runs when that row's preview lands (and clears when it is refused or the selection moves), with the detail line reading "Fitting… Replace will follow." (copy to be numbered); the Replace button stays disabled. (iii) The sheet records `catalog.select` (outcome `choosable | disabled | damaged`) and `catalog.apply` (outcome `applied | dropped-fitting | refused`) through the session's `RecordCatalog`, so the next smoke test is diagnosable from the log (IO rule: measurable by default).
- *Why systemic:* every exit of the loading state, and every input during it, has a user-facing outcome and a telemetry row (U9, IO1).
- *Rollback:* revert the assignments and the pending-apply field; telemetry rows are additive.
- *Failing-first regression tests:* `Controller_PreviewInternalFault_ReportsReason` (CTL, `previewGate` throwing) asserts `PreviewFault` non-null and the status strip text; `CatalogDialog_PreviewFault_LeavesFitting` (DLG) asserts the detail line is not "Fitting…"; `CatalogDialog_EnterDuringFit_AppliesOnLanding` (DLG, `previewGate` held) asserts the cursor reaches 1 after the fit lands with one Enter; `Catalog_Select_RecordsOutcome` (CTL) asserts the telemetry row. All red today (probes: `fault=null`, detail "Fitting…", cursor 0 after one Enter, no `RecordCatalog` call in the sheet).

**F3 — one composer for the source text (defect 2).**
- *Fix:* `BrowserPane.StationSourceText` derives its source part from the same function as the chip and the card (`PropertiesView.SourceDisplay` without the rights suffix, or a new `Provenance`-level `SourceText(sourceName)`), and the two designation lookups collapse to one. Wording per **DR-INV-1** below.
- *Why systemic:* derive, don't restate (DM: two definitions of one quantity is a defect signature); the fix removes composers rather than patching one.
- *Rollback:* revert the one call site; the bytes are untouched.
- *Failing-first regression test:* `BrowserRow_AfterEdit_NotTheOriginal` (DLG): after Replace + Move the Browser row does not read "section-a · NACA 0012". Red today (probe line). Plus `SourceText_AllSurfacesAgree` (DLG): chip, station card (minus rights), Save dialog line (minus prefix) and Browser row carry one source string for one set of bytes.

## Generalization — the failure class

- **Failure class 1 (named): STATE-IN-AUTOMATION-TREE-ONLY.** A component state written to accessibility properties (`ItemStatus`, `HelpText`, `aria-*`) with no rendered counterpart; the mockup styles it, the axaml does not; the tests assert the model. Sighted users get no state and the gate stays green.
- **Failure class 2 (named): DISPLAY-QUANTITY-COMPOSED-PER-SURFACE.** A derived display value composed independently on each surface from the raw field, so one surface drops a flag the others carry; the DM rule was applied to the stored field (one writer) and not to the derived text.
- **Failure class 3 (named): LOADING-STATE-WITH-SILENT-BRANCHES.** A loading state ("Fitting…") whose exits and whose inputs are not all mapped to a user-facing outcome: a fault branch records telemetry and sets no reason, a draft change sets no reason, and an action taken during the state returns silently instead of being queued or refused.
- **Broader systemic solution:** (1) a Desktop harness sweep, `AutomationState_HasRenderedState`, over every sheet the shell can show (the `Modal` command rows already enumerated by `Commands_ModalFlag_MatchesRowsThatShowADialog`): any item whose `ItemStatus` is `disabled`/`invalid` must carry a matching style class, else FAIL by control name; (2) a single source-text composer in Core (`Provenance`) with the designation lookup injected once, and a consistency test across the four surfaces; (3) the rule for a finisher: every drop branch assigns a user-facing reason, proven by a test that throws inside the compute.

| Candidate sibling (where) | Same class because… | Verdict | Evidence |
|---|---|---|---|
| Browser pane station row (`BrowserPane.axaml.cs:109`) | composes source text without `Modified` | **confirmed** (class 2) | probe line "browserRow=section-a · NACA 0012" after edit |
| Properties pane section-group title (`PropertiesView.cs:1325`, raised by the Test Architect) | `{profile} · {SourceName(...)} · degree` — the same lookup, no `Modified` | **confirmed** (class 2) | read in session; `SourceName` is the function the probe showed dropping the flag |
| Enter during the fit (`CatalogDialog.axaml.cs` key handlers → `ApplyAsync` guard) | a loading-state branch with no user-facing outcome | **confirmed** (class 3) | `Probe_EnterDuringFit_ChoosableRow` |
| Station card Source row (`PropertiesView.SourceDisplay`, `controller.StationSource`) | composes source text | ruled out | both go through `ChipText`; probe "Modified from NACA 0012 (GEN)" |
| Save dialog provenance line (`SaveSectionDialog.axaml.cs:24`) | composes source text | ruled out | reads `controller.SourceChip` |
| `WorkbenchController.ChipSourceName` vs `PropertiesView.SourceName` | two designation lookups of one id | **confirmed** (class 2, no visible defect today) | both read in session; `SourceName` falls back to the raw origin, `ChipSourceName` to `LastReplaceName` |
| Analysis panel Provenance tab (`AnalysisPanel.axaml.cs:119`, `AnalysisProjection.cs:393`) | shows provenance | ruled out | the run's conditions/labels/provenance tables; no section-source composer (Verified by grep: no `ChipText`/`SourceName` reader) |
| My sections damaged rows (`CatalogChoice.Damaged`) in the same sheet | state via `ItemStatus` only | **confirmed** (class 1) | same `Filter()` branch; same rendering path |
| `FinishPreview` draft-changed drop (`!sameDraft`) | drop with no reason | **confirmed** (class 3, Inferred from code) | same branch as the probed fault |
| Section editor Finish button (`ModeFinishButton`, reason in `HelpText`) | disabled state with the reason in automation only? | ruled out | `IsEnabled=false` renders through the Button's disabled style; the reason also shows in `ModeReasonBox` (`SectionEditorView.axaml.cs` ≈145) |

**Markers harvested** (`simplify:` / `assume:` in `Catalog.cs`, `SectionReplace.cs`, `Provenance.cs`, `CatalogDialog.axaml.cs`, `SectionEditorView.axaml.cs`, `WorkbenchController.cs`, `AuthoringSession.cs`): one `assume:` in `Catalog.cs:6` (family grouping) — holds, unrelated; four `simplify:` in `AuthoringSession.cs` — none past its trigger, none about this path. No marker predicted either defect.

**Product gap surfaced (a finding, not a repair):** the catalog offers 3 usable sections of 19. The hydrofoil sections an operator reaches for first (63-412, 64A410, 66-012) are the pending ones (A4.6 generator, DR-M12D-2). Even with F1 the sheet will say "not in this build" for most rows. Next step, not in this plan.

## Phased repair plan

| Phase | Repair item (code + tests) | Failure mode it eliminates | Validation | Depends on |
|---|---|---|---|---|
| 1 | F1: `disabled` class + Style on the catalog row (muted ink on the leaf `TextBlock`, row reachable), state-naming tag text; tests `CatalogDialog_NonChoosableRow_RenderedDistinct` (reads the leaf `TextBlock`'s resolved Foreground/Opacity after `Show()` for one choosable and one pending row), `CatalogDialog_PendingGenRow_TagNamesState`; `CatalogDialog_ArrowsReachDisabledRows_Quiet` must stay green (no `IsEnabled=false`) | a non-choosable row read as choosable | tests red on today's code (leaf brushes identical), green after; `ui-craft-gate.py` clean on the sheet | DR-INV-2 (default a) |
| 2 | F2: every `FinishPreview` drop branch assigns a `PreviewFault`; Enter during the fit arms a pending apply; `catalog.select` / `catalog.apply` telemetry; tests `Controller_PreviewInternalFault_ReportsReason`, `Controller_PreviewDraftChanged_ReportsReason`, `CatalogDialog_PreviewFault_LeavesFitting`, `CatalogDialog_EnterDuringFit_AppliesOnLanding`, `Catalog_Select_RecordsOutcome` | "Fitting…" with no exit; an Enter dropped during the fit; an undiagnosable smoke test | tests red first (probes: `fault=null`, cursor 0 after one Enter); `catalog.preview` outcome unchanged | — |
| 3 | F3: one source-text composer (Core), one designation lookup; Browser row and Properties section-group title derive from it; tests `BrowserRow_AfterEdit_NotTheOriginal`, `Properties_SectionTitle_AfterEdit_NotTheOriginal`, `SourceText_AllSurfacesAgree` (chip, station card minus rights, Save line minus prefix, Browser row, section title: one string for one set of bytes); update `BrowserRow_AfterReplace_NameThenSource` only if DR-INV-1 changes the string | a surface naming the original after an edit | tests red first (probe line; `:1325` read); the m12d §14.1 trace rows for the Browser row and station card updated | DR-INV-1 |
| 4 | Class prevention: `AutomationState_HasRenderedState` sweep over every Modal sheet; register the three classes (done in this change); a `docs/design` note in m12d §11.4 that the row's disabled column is "muted name + reason" | the class recurring in the next sheet | the sweep observed red with Phase 1's class removed, green with it | 1 |

**Decision requests for the operator.**
- **DR-INV-1 — wording of an edited catalog section.** a · keep the spec's "Modified from NACA 0012" on every surface (chip and card already say it; the Browser row would read "section-a · Modified from NACA 0012"); b · "Custom · from NACA 0012"; c · "Custom" alone (loses the lineage the Save dialog and rights class depend on). Recommendation: **a**, or **b** if "Modified" reads wrong to designers; never c, because the rights class derives from the origin and the operator may want to find the source again.
- **DR-INV-2 — how the 16 non-choosable rows appear.** a · listed, muted, reason in the detail line and a state tag (the mockup); b · grouped under a "Not in this build" sub-heading below the choosable rows; c · hidden (contradicts ADR-0008 "reports, never hides"). Recommendation: **a**, with b as a later craft pass.
- **Confirm the smoke-test row.** If the row chosen was NACA 0009, 0012 or 4412 and Replace still did nothing, the diagnosis changes (see the decision note's validation condition) and the next capture is the sheet's detail line and the status strip at that moment.

> **STOP — human review gate.** `/investigate` ends here. Review the root cause, the generalization and this plan; approve the phases to execute. Implementation begins only after approval (→ `/implement` per approved phase).

## Residual risk & follow-ups

- The smoke test's row and document are not known (Flagged). The verified mechanisms cover 16 of 19 rows and every non-contract fault; a document-specific `CAT-SPACING` refusal (H2) would still be a "cannot apply" with its reason shown.
- The real pointer path was exercised with a raised `Click` event and a programmatic selection, not a mouse; Avalonia routes both the same way (Inferred). A manual pass after Phase 1 closes it.
- The `!sameDraft` drop was read, not probed (Inferred); Phase 2's test makes it observed.
- Pixel sampling versus property reads: the probe read the leaf `TextBlock`'s resolved Foreground and Opacity (the row has no custom renderer and no Style, Verified). For Phase 1 that property read is the regression test; a pixel sample is needed only for a contrast-ratio claim and belongs in the `ui-design` Stage 3 check, not in the fast ring.
- `ApplyAsync` runs the `UseSourceThickness` step after the Replace step; if that second step is refused the sheet stays open with the Replace already applied (`CatalogDialog.axaml.cs`, the `try` in `ApplyAsync`; Inferred, raised by the Test Architect). Not in this plan; a next-step note.
- The three mechanisms are verified; which one the smoke test hit is not (the sheet records nothing on select or on a dropped apply). Phase 2's telemetry closes that gap for the next report.

## Gate record

`GATE investigate · 2026-10-23 · SRE (investigator, Peer) · Test Architect (Adversary, spawned, read-only) · root cause verified: yes (defect 1: rows without coordinates rendered as choosable; two latent silent branches of the loading state; defect 2: the Browser row and the Properties section title drop the modified flag) · verdict: Test Architect PASS-WITH-CONDITIONS · vetoes→resolution: the Enter-during-fit path was asserted as a competing mechanism → probed and confirmed, folded into F2/Phase 2 as a third verified mechanism; "diagnosis 1 is Inferred until the operator names the row" → accepted, the assumption is a decision note with a validation condition and Phase 2 adds the telemetry that would have named it; the fourth composer (`PropertiesView.cs:1325`) → confirmed by reading, added to Phase 3; the leaf-TextBlock read for Phase 1's test → adopted; "custom" wording is a product decision → DR-INV-1. The investigator did not self-clear the diagnosis; the hard veto on an unproven fix stays until Phase tests are seen red.`
