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

## Revision 2 (track CMR, after Ruling 194): dispositions

Inputs: the operator's three answers; the accessibility review (7 Majors M1 to M7, build conditions C1 to C10) and the
Computational Geometry review (PASS-WITH-CONDITIONS), both as summarised in the CMR brief. The original review texts are in the
leader's record; the minors m1 to m6 and nits n1, n2 were not in the brief and are **not dispositioned here** (the leader maps them).
"Fixed" means the note section and the mockup changed; every check below was run in Chrome via
`docs/proof/cmb/mockup-audit.mjs` (Playwright, system Chrome) on 2026-10-09.

| Item | Disposition | Where | Evidence (observed) |
|---|---|---|---|
| M1 focus ring on the viewport | Fixed: `--focus-vp` (#66ddc8) replaces `--accent`; pair added to the audit | note §3.2; mockup CSS, audit | computed outline of the focused plan = rgb(102, 221, 200); min contrast over 8 pairs 6.11 : 1 light, 7.83 : 1 dark |
| M2 arrow-key probe | Fixed: removed; `[` `]` walking with "Point N:"; harness range input labelled harness only | note §3.2 | after four `]` presses the strip reads "Point 4: LE R 1986 mm · κ +0.50 per m convex at point 4's station on the curve" |
| M3 Tab skips the plate | Fixed in design: order plan, plate, Properties; `TabToProperties` change; tests named. Mockup shows a stand-in Properties button | note §3.2, C2 | Tab from the plan visits smaller, larger, auto, sparser, denser, then the stand-in (observed). `PlanCanvas.cs:597`, `:614` read |
| M4 limit moves focus to Auto | Fixed: aria-disabled keeps focus and announces; a native disabled button would fall to the paired stepper | note §3.2, C1; mockup `act()` | after pressing Larger at the limit, focus stays on Larger, aria-disabled true, live region "Largest teeth reached." |
| M5 narrow open plate covers tip and strip | Fixed: opens in flow between viewport and strip; open-state card added; coverage re-run | note §6; mockup card and audit | narrow open stage: open plate below viewport, clear; card order viewport ≤ plate ≤ strip (6485 ≤ 6492, 6718 ≤ 6724 px); 0 covered points at 520 px closed |
| M6 tick text equivalent | Fixed: pieces line "2 pieces · LE1 at η 0.69" | note §5, COPY-RC-6 | observed on plate for example, wobble, corner, g1 |
| M7 threshold as `title` | Fixed: plate line with the resolved per-m value; COPY-RC-9 and §5 aligned | note §5, COPY-RC-9 | plate line "Ignores reversals under 0.02 ÷ rail length: 0.021 per m (LE), 0.022 per m (TE)." |
| C1 to C10 | Carried into "Build conditions" with named tests; only C1 had its wording in the brief, C2 to C10 are numbered by the author from the named conditions | note §9 | not a product test; build proof |
| Geometry: threshold | Fixed: τ / L, τ = 0.02; fixtures F4 and F5 rewritten with an analytic κ(s) | note §5, §9 | `fixture-profile.py`: F4 extrema at σ 0.5098 and 0.5790, count 3 as τ → 0 and at τ; F5 drop 1.6 τ reads 3, 3, 1 at 0.5 τ, τ, 2 τ |
| Geometry: scale and mirror invariance | Demonstrated in the page | mockup audit | example TE / wobble TE read 2 / 4 at ×0.3, ×1, ×2; mirror: same counts on 4 rails |
| Geometry: extrema | Fixed: roots of the numerator of dκ/dt per span (D3), every simple knot a candidate | note §5 | mockup uses the same method; counts independent of density |
| Geometry: anchors | Fixed: triple knots (C0), explicit span selection, "G1 only", corner by measured angle against 0.1°, both-side readout, sign-change phrase, envelope per piece with a step | note §4, §5, §6; mockup | G1 anchor jump 0.000°, corner 32.4°; strip texts observed for both |
| Geometry: Greville | Fixed: copy "at point N's station on the curve"; t = ξ evaluated directly | note §4, COPY-RC-8 | mockup evaluates `jet(rail, ξ)` directly |
| Geometry: sign | Fixed: +κ LE, −κ TE, positive convex; straight = |κ|L²/8 < 10 µm; `speed2 == 0` throws; inflection only between opposite curved neighbours | note §4, §6; mockup | straight rails read "straight · κ 0", 1 piece each |
| Geometry: lengths | Fixed: Gauss–Legendre | note §4 | GL 932.39 mm vs polyline 932.39 mm |

Craft gate on the revised mockup: `ui-craft-gate.py docs/mockups/rail-comb.html --markdown`, exit 0, no findings. Console and page errors
during the audit run: none. **Residual:** one plate-coverage failure is known and recorded: on the straight and wobble variants the fixed
top-right plate covers up to 3 rail or tooth points (the example planform, the one the plate is checked on, covers 0); the product places the plate by exclusion.

## Residual risk

Vendor tooth side is Flagged. Geometry expert confirmation of the reversal threshold, the one-sided curvature at anchors
and the Greville choice is outstanding. Screen-reader reading of the plate and ticks, and the keyboard focus order of the
steppers, are unproven (the operator ranks accessibility proof below function and look; design floors are kept).
