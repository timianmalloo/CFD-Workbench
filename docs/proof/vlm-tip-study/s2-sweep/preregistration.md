---
id: proof-vlm-tip-s2-preregistration
title: "S2 small-tip-chord VLM sweep: pre-registration"
type: proof-pack
status: draft
owner: "@trk-s2"
phase: implementation
tags: [analysis, vlm, tip-strip, taper, preregistration, ruling-78, ruling-88]
links:
  - { to: plan-tip-handling, rel: implements }
review-by: "2027-04-05"
summary: "Case grid, recorded fields and decision rules for the S2 sweep, written before any solve."
---

# S2 pre-registration (written before any solve)

Authority: `docs/plans/tip-handling.md` section 6 row S2 and Ruling 88 D2. One run set. No law fitting.

## Question

Does the production VLM (main, no eta* law) give a judged-strip verdict that flips with lattice refinement n on a
small-tip-chord planform? Ruling 78 hides only the outermost strip per half. If k2 or k3 flips at small tip chord, Ruling 78 is not enough.

## Planform

Tapered rectangle, straight quarter-chord line (zero sweep), half-span 1, root chord 0.25, linear taper:
`chord(eta) = 0.25 * (1 - (1 - r) * |eta|)`, tip chord ratio r = c_tip / c_root. No twist, flat plate (alpha_L0 = 0).
Representable on main: the probe builds `SectionSample`s with a per-station chord and calls `VortexLattice.Solve`
directly, as the held probe does. A positive r is what the certifier admits (`Geometry.cs:371`: strictly positive chord, no minimum). The probe bypasses the certifier.

## Grid (one run set)

- Decision grid: r in {0.01, 0.02, 0.05, 0.1, 0.25}, alpha in {5, 8, 18} deg, n in {32, 64, 128, 256} per half. 60 cases.
- Ruling 88 D2 cases (r = 0.01 and 0.02 at alpha 18, n = 32..256) are 8 of those 60. They are read under their own rule below.
- Control (not in any decision): r = 1.0 (rectangle), same alpha and n, 12 cases. It checks the probe against the held branch's rect values (alpha 5 k1 alpha_eff near 3.07 deg).
- Lattice: `Settings.Default` with NSpanPerHalf = n, NChord = 4, cosine span spacing, wake 20 spans. Only n varies. V = 10, rho = 1.

## Recorded per case

- k1, k2, k3 (strip k from the tip, port is mirrored so use the starboard end): eta, chord, alpha_eff, Cl_local, raw envelope verdict (`MethodRecord.Verdicts`, in or out with the exceeded parts).
- Product reading: k1 is the outermost strip of its half and is NOT judged (Ruling 78). The judged strips are k2 and k3 (and all inboard strips). The `any_judged_out` flag is true if any strip except the two outermost reads out.
- Consistency index per strip: C = Cl_local / (a0 * (alpha_eff - alpha_L0)), normalised by the strip of smallest |y| (root). The edge is the first strip, walking from root outward, with C/C_root < 0.9. Record its eta and the tip-side distance in tip chords, `(1 - eta_edge) * halfspan / c_tip`. "none" if no strip crosses.
- Solve seconds, CL, AR, lattice diagnostics.

## Definitions

- Judged-strip flip at (r, alpha): the triple (k2 verdict, k3 verdict, any_judged_out) is not identical at all four n.
- Flip direction: out at fine n and in at coarse n (or the reverse) is recorded. In at some n while out at others is the dangerous direction: a real overload read as inside.
- k1 alpha_eff drift is recorded but is not a flip (k1 is not judged).

## Decision rules (applied verbatim after the run)

1. No judged-strip flip across n at any r (all 15 decision pairs): no change. Ruling 78 stands. `MarkOutermostProvisional` stays.
2. Flips only at r at or below some r_flip: fallback band at the measured edge. The band is every strip outboard of the measured consistency edge (C/C_root < 0.9) at n = 256, reported per case. r_flip is the largest ratio with a flip. No constant width from recall.
3. Flips at r = 0.25 or at the r = 1.0 control: the sweep contradicts the earlier "finite tips do not flip" finding. Stop and report to the operator. No fix is proposed.
4. D2 reading (r = 0.01, 0.02, alpha 18): if k2 and k3 read out at all n, the alpha-18 falsifier survives at small chord. If either reads in at any n, that is a flip under rule 2 and is the strongest S3 trigger.
5. Judge-k1 reading (not part of rule 1-3): k1 can be judged on finite tips only if k1 alpha_eff verdict is stable across n at every r with no flip, and the consistency index at k1 is within 0.9 of the root. Otherwise k1 stays provisional. The Cl_local to 0 limit at the edge is known (plan section 2.1), so this spike can only exclude, not support, judging k1.

## What each outcome means for S3 (Ruling 78)

| Outcome | S3 action |
|---|---|
| Rule 1 | No change to Ruling 78. S3 is doc and test work only. |
| Rule 2 | Add a tip-chord-ratio floor or band at the measured edge. S3 states r_flip and the edge. |
| Rule 3 | Escalate. Ruling 78 is not sufficient. |

No fit of alpha_eff or any correction law will be made from these data. A fitted correction is the eta* trap.
