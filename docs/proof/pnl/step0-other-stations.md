---
id: proof-pnl-step0-other-stations
title: "PNL step 0: how to know a non-governing station's panel under-read"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, panel-method, decision-request]
links:
  - { to: proof-a3bc-seam, rel: relates-to }
  - { to: design-area3-analysis, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Ruling 103 says other stations keep the provisional row when their 200-vs-400 under-read is above 10 %,
  but nothing measures a non-governing station. Three ways to know it, with measured or estimated cost against
  the 1 s budget, and a recommendation for the Fable owner.
review-suggested: []
---

# PNL step 0 — the non-governing station's under-read

Track `trk-pnl`, 2026-10-06, macOS arm64, Release. Decision request for the Fable owner. Nothing here is built.

## The gap

Ruling 103: "other stations keep the DR-DXM-7 provisional row when their under-read is above 10 %". After this
track, `SectionTier.Evaluate` solves the governing station at 200 and 400 and records `PanelUnderreadFraction`
for that station only (`SectionTier.cs`, the `underread` local). `SectionTierResult.GoverningProvisional` reads it.
No other station has a 400-panel value, so "above 10 % at a non-governing station" cannot be evaluated.

## What the repo already measures (evidence for the options)

| Evidence | Value | Source | Label |
|---|---|---|---|
| 200-vs-400 suction delta, governing station, NACA 0012 default wing, α 3° | 1.335 % at η 0.098 | `docs/proof/a3bc-seam/proof-pack.md:71` | Verified (SEAM session) |
| Same delta, NACA 2412 (12 %, 2 % camber) 129-station wing, α 3° | 0.768 % at every station (one section, uniform) | this track, `MEASURE cambered ... first-run delta 0.768%` | Verified |
| Karman-Trefftz Cp_min error vs exact, 200 / 400 panels | 3.48 % / 1.61 % → 200-vs-400 delta 1.90 % | `docs/proof/a3b/red-first.md:81-82` (−1.653954 / −1.685973) | Verified (KT only, 8 %-offset contour) |
| Reviewer Cp_min under-read vs exact at 200 panels, 6 %-thick, α 3–6° | 6.9–7.7 % | `docs/design/area3-analysis.md:505`, `proof-pack.md:53` | Verified by the CFD review, not re-run here |
| Observed convergence order of Cp_min / interior Cp | p = 1.02–1.09 (first order) | `docs/proof/a3b/red-first.md:88-90` | Verified |

First order means E(200) ≈ 2 E(400), so the 200-vs-400 delta ≈ E(200)/2. For the reviewer's 6.9–7.7 % that is about
3.5–3.9 %. **Inferred**: it assumes first-order behaviour holds for 6 %-thick sections; I did not run one. The largest
200-vs-400 delta in the repo is therefore near 4 %, and the 10 % gate is 2.5× above it. A station that crosses 10 % would
be a thinner or sharper-nosed section at higher α_eff than anything measured. There is no thickness-sweep dataset of
200-vs-400 deltas in the repo (searched `docs/proof`, `docs/notes`, `docs/knowledge`).

Measured cost scale (this track, Release, 129 stations, one uniform section, both before and after this change): a warm
whole-wing 200-panel pass is 430–490 ms. A 400-panel solve at **all 129 stations** (the scan I ran and then reverted)
took 1.99–2.08 s on top. So one extra 400 solve ≈ 16 ms (2.0 s / 129; uniform section, so a lower bound for odd foils).

## Options

**A. Bound by thickness (no solve).** Derive a conservative delta from t/c (and α_eff) from the KT and reviewer data,
and mark a station provisional when the bound exceeds 10 %.
- Cost: 0 ms.
- Misses: the bound does not exist yet. Two data points (KT, reviewer) and no sweep; a curve through them is
  extrapolation, and a flag from a fitted bound is a number nobody measured. It also cannot see a section with a sharp
  nose at moderate t/c. Needs a new study (a 200-vs-400 sweep over t/c, camber, α) before it is Verified.

**B. 400 re-solve at stations whose 200-panel screen is close to the governing one.** Re-solve every station whose
200-panel σ/(−Cp_min) is within X of the best ratio. With the observed delta ≤ ~4 %, X = 10 % would already cover a
governing swap. Each re-solved station gets its own measured under-read.
- Cost: about 16 ms per station. Cheap on a tapered wing where few stations tie; on a uniform wing with equal α_eff
  (the 129-station fixtures above) every station ties, so it degenerates to option C-all: about 2.0 s, over the 1 s
  budget. It needs a cap (re-solve at most k stations, flag the rest "not measured"), and the cap is a new rule.
- Misses: stations outside X are unmeasured, so the 10 % statement is not made for them. It does measure the stations
  that could change which station governs, which is the safety-relevant question.

**C. 400 re-solve at every station.** Every station gets a measured under-read; the governing and the displayed
values are the same kind of number.
- Cost: about +2.0 s (measured) → ~2.5 s warm whole wing. Breaks the 1 s budget (Ruling 90 rejected the same shape at
  ">1.5 s").
- Misses: nothing about under-read; costs the budget.

## Recommendation

**Option D (a variant of B, bounded by what the user looks at): measure the under-read on demand for the station the
Section tab shows, in addition to the governing station.** DR-DXM-9 already says the Section tab shows "the selected
strip, else the governing cavitation station, named". That is the only other station whose provisional row anyone
reads. One extra 400 solve at the selected station costs about 16 ms, adds nothing to the run, and the row it feeds
is a measured number, not a bound.
- It needs a new overload (for example `SectionTier.UnderreadAt(source, eta, alphaEff, ...)`) and a call from the
  Section tab track (DX). That is a seam to DX; PNL does not touch `Evaluate`/`Derive` signatures or the projection.
- What it misses: an unselected station is never measured. That is acceptable for the flag. For the screen, a
  non-governing station can only matter if its true ratio beats the governing one; with a measured governing delta and
  observed deltas ≤ ~4 % (Inferred beyond the two measured foils), the 200-panel ordering is safe at a 10 % margin.
  If the owner wants that closed, add B with a cap of 4 stations: about 64 ms.

Assumptions (marked, per the standing rule): `assume: first-order scaling holds at 6 %-thick sections (confirm: run
the 200-vs-400 solve on a 6 % foil at α 3–6°; breaks: the ~4 % estimate, so the 10 % gate could fire more than
predicted).`

## Outcome

Ruled as Ruling 110: D + B(k=4), built in repair cycle 1 (see `red-first.md`). The thickness data this note lacked is in
`underread-measurements.md`.

## Decision requested (answered by Ruling 110)

1. Pick A, B (with cap k), C, or D (recommended), or D+B(k=4).
2. If D: authorise the DX track to call the new overload (PNL adds it; the call site and the row are DX's).
3. Test 3 of the brief (a planted thin section whose non-governing station exceeds 10 %) waits on this ruling.
