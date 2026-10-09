---
id: proof-cmb-adversary
title: "Rail comb: marine-cad-ux-expert Adversary pass and dispositions (Ruling 107)"
type: doc
status: proposed
owner: "@timianmalloo"
phase: design
tags: [adversary, marine-cad, comb, rail, ruling-107, ruling-193]
links:
  - { to: design-rail-comb, rel: documents }
  - { to: mockup-rail-comb, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Verbatim-summary record of the marine-cad-ux-expert Adversary pass on the draft rail-comb note: verdict
  PASS-WITH-CONDITIONS, 7 Majors, 5 Minors, 2 Nits, each with the author's disposition. The soft veto is answered in
  writing; the persona did not clear its own review and asks the Computational Geometry expert to confirm three
  mathematical points before the build.
---

# Adversary pass: marine-cad-ux-expert on `docs/design/rail-comb.md`

- **Persona / mode / tier:** marine-cad-ux-expert, Adversary, T1. Run as a separate agent on the draft note; it read the
  note, spec A4.9, `PlanCanvas.cs`, `PointModel.cs`, `SectionCanvas.cs`, `ConstrainedFit.cs` and m12c §11.2. It did not
  read the mockup. The area 01 and 03 knowledge files are not in the repo, so vendor conventions are Flagged (recalled).
- **Verdict:** PASS-WITH-CONDITIONS. No Blocker. **Veto:** not cleared by the persona; clearing needs a written
  disposition per finding F1-F7 (below). The Geometry expert should confirm F4, F5 and the Greville choice before build.
- Evidence labels are the persona's. "Accepted" means the note and the mockup were changed; the section named is where.

| # | Sev | Finding (short) | Disposition |
|---|---|---|---|
| F1 | Major, Verified | The Plan comb ignores the sign of κ (fixed left normal, `PlanCanvas.cs:841-846`), so on a convex outline one rail's teeth point inward; "matching the section editor" needs new sign logic. | **Accepted.** Note §4 states the Plan gains the `-sign(κ)` flip, three tests named, plate legend says "Teeth point away from the centre of curvature". Mockup draws it that way. |
| F2 | Major, Flagged / Verified | Rhino, Alias and Fusion tooth side unconfirmed; A4.9 says the comb "never clips", the note clips at 60 px. | **Accepted.** Tooth side declared and written on the plate (vendor side stays Flagged until observed on a fixture). AM-RC-1 (a) amends A4.9 explicitly. |
| F3 | Major, Verified | Density "per knot span" is not even spacing (`PointModel.cs:195-204`). | **Accepted.** Density is teeth per rail, even arc-length pitch over the whole rail (16/32/64/128); test `Planform_Teeth_PitchConstantWithin5Percent`. The narrow-spike limit is stated, not refined (the ticks and × mark it). |
| F4 | Major, Inferred | A dead band relative to max \|dκ/ds\| lets the tip swallow a mid-rail wobble; jumps at anchors invisible to sampled slope; say signed; dκ/ds vs dκ/dη; adaptive sampling; tick numbering. | **Accepted.** Note §5: reversal threshold 0.02 per m on signed κ (zigzag), jump handling by one-sided samples, s stated, adaptive sampling, ticks labelled LE1.. and non-interactive; fixture "tip round plus 0.3 mm wobble reads 3". Threshold is an `assume:` confirmed by that fixture. |
| F5 | Major, Verified | Radius vocabulary teaches false models: "corner" for a smooth anchor; "straight" at an inflection; "nearest curve position" for a control point; inch units missing. | **Accepted.** "Corner" reserved for `TangentKind.Corner`; C1 anchors read "curvature jumps at this anchor"; "inflection: R infinite here"; root side / tip side; Greville position, "where point 4 pulls hardest"; inch rule added. Threshold tied to the tolerance triple at build. |
| F6 | Major, Verified | Hovered κ (required by A4.9) is missing. | **Accepted.** Signed κ in the strip (positive when convex), convention in the tooltip; prefixes "At pointer:" / "Point 4:". |
| F7 | Major, Inferred | Auto-hold needs edges: nudge runs, refit jump, section mismatch, shared gain. | **Accepted.** Note §3.1: gesture = drag, nudge run (refit at key release plus idle), open rebuild preview; legend flash and live-region text when the gain changes by more than 2x; Auto shows not-active after a manual step; "shared by both rails" on the plate. Section parity is OQ-2 and the blocker for calling the editors consistent. |
| F8 | Minor | Ladder tops out at 50 per m; a 10 mm tip radius is 100 per m. Inch ladder. | **Accepted.** Ladder to 200 per m; inch ladder in inch units; steppers disable at the ends with the reason. |
| F9 | Minor | Plate top right may cover the tip; the count is a readout (CAD-08). | **Partly accepted.** Harness check "plate covers rail or teeth: 0 points" added and passes at 1320 and 520 px; build test `PlanComb_PlateDoesNotCoverRail_AtTipFit`; the build copies the `ScaleBarBounds` exclusion pattern. **Declined:** moving the pieces lines to the Tracing strip. The count is of the whole rail, not a pointer reading; the strip stays pointer-only (CAD-08). |
| F10 | Minor | Both rails when nothing is selected is right; make LE and TE distinguishable. | **Accepted.** Operator question 1. TE teeth are drawn in the foil-edge tone, LE in the viewport-mute tone, the selected rail at full strength; ticks carry LE/TE labels. |
| F11 | Minor | Equal scale is a requirement; Thickness x2 tilts section teeth. | **Accepted.** Note §4 requirement and test; the section finding recorded as OQ-4 (Inferred, not run). |
| F12 | Minor | COPY-RC-9 is a soft verdict. | **Accepted.** The guidance sentences are removed; the help line states what is counted and the threshold. |
| F13 | Nit | No typed channel; persistent mode legend; prefix the reading source. | **Accepted.** Stated in §3 that there is no typed channel; the plate legend is persistent; strip prefixes added. |
| F14 | Nit | Ticks read like controls; Auto must show not-active; first-run discoverability. | **Accepted** for ticks (non-interactive, off the focus order) and Auto (`aria-pressed`). **Deferred** first-launch demonstration of the comb to the operator's call: not a mockup item, no spec row asks for it. |

## Veto answer

The soft veto named four failures. (a) Teeth side not what the Plan does: answered by F1. (b) Readout vocabulary
contradicting its mathematical role in three places: answered by F5. (c) Hovered κ missing: answered by F6. (d) Silent
spec conflicts on clipping and spacing: answered by AM-RC-1 and F3. No finding is overridden by rationale. The
persona's clears-when predicate (harness checks pass; strip and plate state the mode, the authoritative parameter and
every lock) is met by the mockup's in-page audit (Auto p90 = 30 px; analytic R within 0.1 % of a three-point circle;
straight rails 1 piece; plate covers nothing; targets 24 px). That is the author's reading; the persona has not re-reviewed.

## Second lens: ux-accessibility on the mockup (verdict BLOCK before fixes, 2 Blockers, 5 Majors)

Fixed in the mockup, one repair cycle: viewport focus ring no longer clipped (inset outline); the Tracing strip is a
status region and the probe svg has a group role, name and arrow-key instructions; the selected-point reading used the
Greville position of point 1, not point 4 (a false number, now `u4+u5+u6`); the plate is removed from cards where it
covered the wing (section 3, straight); svg text is counter-scaled so tick labels stay 12 px; plate focus survives
pointer re-render; teeth opacity raised to .75; Auto shows a check glyph; plate rows are labelled groups; the audit
strip is no longer a live region. **Not fixed, recorded:** the narrow plate in its open state is not rendered as a card;
disabled-at-limit buttons use `disabled` with a title (build uses `aria-disabled` and announces the limit); a text
equivalent for tick positions; high-contrast and screen-reader traces. The accessibility veto is held by that lens and
is not cleared by the author; the operator ranks accessibility proof below function and look.

## Residual risk

Vendor tooth side is Flagged. Geometry expert confirmation of the reversal threshold, the one-sided curvature at anchors
and the Greville choice is outstanding. Screen-reader reading of the plate and ticks, and the keyboard focus order of the
steppers, are unproven (the operator ranks accessibility proof below function and look; design floors are kept).
