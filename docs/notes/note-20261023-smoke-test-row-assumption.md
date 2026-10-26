---
id: note-20261023-smoke-test-row-assumption
title: "The smoke test's 'cannot apply' hit one of the three verified silent paths, most likely a catalog row without coordinates"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: investigate
tags: [decision-note, catalog, section-editor, assumption]
links:
  - { to: investigation-section-catalog-apply, rel: relates-to }
  - { to: proof-section-catalog-apply, rel: relates-to }
review-by: "2026-11-23"
review-suggested:
  - { by: investigation-section-catalog-apply, on: 2026-10-23, reason: "Investigation section-catalog-apply: the catalog sheet renders rows without coordinates as choosable, the fit's loading state has silent branches, and the Browser row and Properties section title drop the modified flag; review the m12d design's component-state table and the behaviour trace" }
summary: >-
  The operator did not name the row or document of the smoke test, and the catalog sheet records nothing on select or on
  a dropped apply, so the instance cannot be pinned; the investigation proceeds on the assumption that the row chosen had
  no coordinates, with the Enter-during-fit and fit-fault paths as the alternatives. The repair plan covers all three.
---

# The smoke test's "cannot apply" hit one of the three verified silent paths, most likely a catalog row without coordinates

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback weight.*

- **Kind:** assumption
- **Confidence:** Inferred (the three mechanisms are Verified by probe; which one the smoke test hit is not observed)
- **Made during:** `/investigate` section-catalog-apply, 2026-10-23

## The call

The operator reported "I can select a foil from the catalog but I cannot apply it" without naming the row or the
document. The sheet writes no telemetry on select or on a dropped apply, so nothing in the log names the path. Of the
three verified silent paths, the row-without-coordinates path covers 16 of the 19 rows the sheet lists and needs
nothing but a click, so it is taken as the instance; the Enter-during-fit path needs a keypress inside the fit window
(140–240 ms on the Example, longer under load) and the fit-fault path needs a non-contract exception that the three
generated shapes do not raise on the Example. The plan repairs all three, so the assumption changes the narrative, not
the scope.

## Alternatives dismissed

- `apply wiring broken in the real app` — the real route applied NACA 0012 (proof `Probe_RealRoute_ReplaceFromCatalog_Applies`).
- `the row was refused by the fit/spacing rule` — on the Example every row with coordinates lands; a refusal shows its reason and may offer the chain button. Possible on another document; it would not be silent.

## Validation condition

"Holds until the operator names the row." If the row was NACA 0009, 0012 or 4412 chosen by pointer with Replace enabled
and the click did nothing, none of the three paths explains it and the investigation reopens at the apply path with the
sheet's detail line and the status strip captured at that moment. Once Phase 2's `catalog.select` / `catalog.apply`
telemetry lands, the next report answers this from the log.

## Promotion rule

Not promotable; retire the note (`status: superseded`) when the operator confirms the row or Phase 2's telemetry makes the question answerable from the log.
