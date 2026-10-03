---
id: review-ui-m12c-paired
title: M1.2c section editor — paired point types, before/after against the approved mockup
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, m12c, section-editor, point-types, paired, sptf, operator-show]
links:
  - {to: mockup-m12c-section-editor, rel: documents}
  - {to: design-m12c-section-editor, rel: depends-on}
  - {to: rulings, rel: implements}
  - {to: proof-m12c-certificate-spike, rel: depends-on}
  - {to: note-m12c-rulings, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  Ruling 60 ships paired section point types (the certificate spike was a no-go). This note lists every visible change
  to the approved M1.2c mockup so the operator can decide: the pairing cue on canvas, Points pane and Properties; one
  Type and Kind control for both surfaces; a new paired x-move screen; a new refused Anchor → Control screen with the
  measured 0.0185 mm against the 0.010 mm limit; and four proposed COPY rows. Two findings go to the design owner.
review-suggested: []
---

# M1.2c section editor — paired point types

**For the operator.** The approved page drew **per-surface** point types. The certificate spike was a no-go
([verdict](../proof/m12c-certificate-spike/verdict.md)), so Ruling 60 ships **paired** types (OD-4 a). The page
[`m12c-section-editor.html`](../mockups/m12c-section-editor.html) now draws the paired behaviour. This list is every
change you can see. Nothing else moved: layout, density, tokens, the Points pane structure, the Precision preset and
the status strip are as approved.

Captures (Playwright, system Chrome, 1360 × 900 page, 1280 × 800 shells): `docs/proof/m12c-paired-mockup/`
`light-s2.png`, `light-s2x.png`, `light-s2r.png`, `light-s3.png`, `light-od3.png`, and the dark set `dark-s2.png`,
`dark-s2x.png`, `dark-s2r.png`, `dark-s3.png`.

## What changed, screen by screen

| # | Where | Before (approved) | After (paired) | Rule |
|---|---|---|---|---|
| 1 | Page header | "upper point 4 made an anchor on the upper surface only" | "point 4 made an anchor on both surfaces (paired point types, Ruling 60)" | — |
| 2 | Page header | Theme button only | A **State** list beside it jumps to any screen or decision (the page's state switch) | harness |
| 3 | Screen 2 canvas | Lower surface: 8 points, no anchor | Lower surface: 13 points, an anchor at the same 35 % chord, its handles drawn | type on both surfaces |
| 4 | Screen 2 canvas | Comb break at the upper anchor only | Comb break and dashed break line at **both** anchors (lower: −0.167 → 0.643 per chord) | kind on both surfaces |
| 5 | Screen 2 canvas | Selected upper anchor only | **Pairing cue:** the lower partner has a dashed ring in the station colour, a dashed link at the shared x, and the label "lower pt 7 · paired" | partner cue |
| 6 | Properties | Identity, then the Point group | A line under the identity: "⇄ Paired with lower point 7. Type, kind and x are shared." | partner cue |
| 7 | Properties | "Type", "Kind" | "Type · both surfaces", "Kind · both surfaces" (one control each) | one control |
| 8 | Properties | "Handle toward the nose / tail" | "Upper handle toward the nose / tail" (the handles stay per surface) | — |
| 9 | Properties | Points "13 upper · 8 lower" | "13 upper · 13 lower" | type on both surfaces |
| 10 | Properties | 14 of 17 rows visible, the last "LE radius at Root" | 13 of 17 visible, the last "LE radius own": the pairing line pushes "LE radius at Root" below the fold (measured) | side effect |
| 11 | Points pane | No note | A note under "Control net · degree 5": "Type and Kind apply to both surfaces. x is shared." | one control |
| 12 | Points pane | Selected row tinted | Selected row tinted with a 2 px accent rail; its **partner row** on the other surface gets a lighter tint and rail; both rows start with ⇄ | partner cue |
| 13 | Points pane | Lower group: 7 rows, no anchor | Lower group: 12 rows, row 7 "Anchor · Horizontal" | type on both surfaces |
| 14 | Status strip (2) | COPY-174: "Upper point 4 is now an anchor … lower unchanged …" | Proposed COPY-185 (below) | type on both surfaces |
| 15 | Status strip | "Upper · pt 7 of 13" | "Upper · pt 7 of 13 ⇄ lower" | partner cue |
| 16 | **New screen 2b** | — | A paired x move: point 10 typed from 64.36 % to 60.00 %; upper and lower point 10 move together (dashed rings at the old place, arrows), x row "x · both surfaces" in focus, proposed COPY-186 in the strip | x move paired |
| 17 | **New screen 2c** | — | Anchor → Control **refused**: warning strip with the measured number and the limit (proposed COPY-187) and **Show**; a red dashed marker on the lower surface where it would move most, labelled "lower would move 0.018 mm here (limit 0.010 mm)"; the canvas and Properties are unchanged (nothing changed) | refit over 10 µm refused |
| 18 | Screen 3 | Lower point 6 of 8 dragged from −3.00 % to 8.00 %; cross 78.4–83.4 % | Lower point 11 of 13 dragged from −2.80 % to 10.00 %; cross 77.6–89.1 %; its upper partner linked. A denser net needs a longer drag to cross | side effect |
| 19 | OD-1 B, OD-3 A–D cards | Lower surface 8 points | Lower 13 points, partner cue drawn. OD-3 A now shows 3 of 25 point rows (was 4 of 20); B and D show 25 of 25 | side effect |
| 20 | OD-4 card | "paired types would give the lower surface 13 points too" | "The certificate spike measured a no-go, so a ships (Ruling 60). This page now draws paired types …" | — |
| 21 | 3D and Front drafts (OD-1 B) | Placed from upper-anchored + original lower | Placed from both paired surfaces | — |
| 22 | Measured line (page foot) | "Lower surface bytes unchanged by the upper anchor: true" | Paired facts: knots equal, abscissae equal, lower change from the knots, lower change from Horizontal, refused refit number | — |

## Proposed copy (UXR records the IDs in DESIGN.md §7)

These follow the V2 strip and the COPY-174 / 176 pattern. Each strip string was measured to fit the 1280 px strip
without truncation on its screen.

| ID (proposed) | String | Where | Replaces under SPTF |
|---|---|---|---|
| COPY-185 | "Point <n> is now an anchor on both surfaces (point <m> of <N>; <a> → <b> points each). Largest change <d> % chord, <surface>; <other> shape unchanged. Curvature now breaks at <x> % chord." | strip | COPY-174 |
| COPY-186 | "Moved point <n> on both surfaces: x <x0> → <x1> % chord. Own t/c <t> %; <station> stays <s> % t/c." | strip | COPY-176 for an x move |
| COPY-187 | "Point <n> stays an anchor. As a control point the <other> surface would move <d> mm, over the <limit> mm limit at <c> mm chord. Nothing changed." | strip, warning, with **Show** | new (`SectionEdits_PairedAnchorToControlRefitOverLimit_Refused`) |
| COPY-188 | "Type and Kind apply to both surfaces. x is shared." | Points pane note | new |
| COPY-189 | "Paired with <other> point <n>. Type, kind and x are shared." | Properties, under the identity | new |

The row labels "Type · both surfaces", "Kind · both surfaces" and "x · both surfaces" are labels, not COPY rows.

## Measured on the page (`window.__measure`)

| Quantity | Value |
|---|---|
| Knots equal on both surfaces · abscissae equal | true · true |
| Lower shape change from the paired knots (Boehm) | 4.4 × 10⁻¹⁶ |
| Lower change from the paired Horizontal kind | 0.028 % chord |
| Own t/c after | 12.12 % at 35.0 % |
| Paired x move: abscissae equal after · x monotone | true · true |
| Paired x move: largest change upper · lower | 0.17 · 0.16 % chord |
| Refused refit: lower deviation (least squares at equal x, nose and TE held, 1999 equal-x samples) | 0.0154 % chord = **0.0185 mm** at 33.4 % chord |
| Limit | 0.010 mm = 0.0083 % chord at 120.00 mm (Root and Tip both 120.00 mm) |
| Upper change if it had gone through | 0.33 % chord |
| Partner link offset on screen | 0 px (vertical: same x) |
| Console errors · NaN / undefined / null / `${` leaks | none · none (light and dark) |
| Strip messages truncated | none (screen 2 fits exactly: 990 of 990 px) |

**UI craft gate** (`ui-craft-gate.py --markdown`, a floor, not a verdict): one Minor, `side-tab` on
`.sb .msg.warn`. This is the same recorded deviation as the approved page (DESIGN.md §4, the 3 px warning rail,
DR-STATUS-1 V2). The new 2 px row rails in the Points pane are not flagged.

## Findings for the design owner

1. **The "smallest chord sets the limit" rule looks inverted.** Inferred from the arithmetic; the Computational
   Geometry lens should confirm. The design (§3.4 fallback) says the smallest local chord sets the 10 µm limit. A
   deviation δ in chord units is δ·c in mm, so the **largest** chord produces the largest physical deviation. Using
   the smallest chord gives the most lenient normalized limit (10 µm / c_min) and lets the largest station exceed
   10 µm. For example, with Root 200 mm and Tip 100 mm, a 0.0099 % chord refit passes at the Tip's limit but moves the
   Root by 19.8 µm. If the intent is "10 µm at every station", the largest chord sets it. The failure-mode test
   `SectionEdits_PairedRefitSharedProfile_SmallestChordSetsLimit` carries the same wording.
2. **Paired Anchor → Control will often be refused.** On this page's own state, the partner's Horizontal kind leaves
   a curvature break that the reduced basis cannot reproduce within 10 µm (0.0185 mm at 120 mm). That is the
   behaviour the rule asks for, but the operator should expect the refusal to be common after a kind change on a
   paired anchor. A real build measures it on the A4.5 oracle; this page uses a least-squares fit at equal x with
   the nose and TE held, so the number is indicative (Inferred), not the build's.

## Not shown, and why

- **Different chords sharing the profile.** example.foil's Root and Tip are both 120.00 mm, so the smallest-chord
  rule cannot change the number on this page. Inventing a chord would put a fixture value on the page that is not in
  the file. The refusal names the chord that set the limit, so the rule is visible in the copy.
- **Interaction.** The page is static, as approved. The x move and the refusal are drawn as the state after the
  step.
- **Persona and viewport switches.** The approved page never had them: one persona (the shaper) and the fixed
  1280 × 800 shell. The page's harness is the screen list, now with a State switch, and the theme toggle. The new
  states are on both.
