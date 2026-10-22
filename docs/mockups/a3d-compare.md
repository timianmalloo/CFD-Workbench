---
id: mockup-a3d-compare
title: A3d Compare — the Compare tab of the Analysis panel
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, area-3, analysis, compare, discrepancy-record, hard-states]
links:
  - {to: design-a3d-compare, rel: documents}
  - {to: mockup-area3-analysis, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
review-by: 2027-01-31
summary: >-
  Eight states of the Compare tab of the Analysis bottom panel: a wing VLM run against the wing estimator, two
  revisions, no second run, incompatible reference quantities, different water, a Historical base, loading and an error. Every delta
  is computed in the page from the fixture numbers below with the design's one formula; a contrast audit prints the
  measured token ratios per theme. For the operator's review before any build.
---

# A3d Compare — mockup note

Open [`a3d-compare.html`](a3d-compare.html) over `file://`. It needs no network, font or script beyond the page. The
harness row switches **state**, **theme** (light, dark, high contrast), **viewport** (1280, 1024, 390 px) and
**reduced motion** (stops the loading pulse). Under the shell, the audit prints the contrast of each token pair in the
current theme and a fixture check line with the computed numbers. The design is
[`docs/design/a3d-compare.md`](../design/a3d-compare.md); copy in the page is its A3D-COPY table verbatim.

## Screens

| State | What it shows | Design ref |
|---|---|---|
| Tiers | shown base r5 VLM + strip, compared the wing estimator at the same inputs; tier headline (spec copy line 2306) with "none — no tier reports its uncertainty"; e row "Not compared"; the CDi δ cell carries "includes the estimator's elliptic loading assumption (e = 1)"; the Inputs table shows each run's **Envelope** and **Omissions** (A5.7). The record is stored once, in canonical orientation estimator → VLM | §2.1, §2.3–2.5 |
| Revisions | base r5, compared r4 (Historical); status "Comparison read — recorded earlier" (the same key a second time) | §1.3 |
| No second run | one Completed run; the empty copy and the "Compare with estimator" button | §5.4 |
| Incompatible reference | base r5 (S_ref 0.108 m², b_ref 0.900 m), compared r3 (0.100 m², 0.850 m); differing inputs bold with "≠"; one spanning blocked cell, no δ, no record | §2.2 |
| Different water | base r5, compared the same inputs in 20 °C water (ρ 1024.76 kg/m³); the water block reason, not the operating-point one; no δ, no record | §2.2 (H5) |
| Historical base | base r4 (Historical — surface, a twist point moved), compared r5 Current; δ shown with the Historical note. This is the canonical orientation (earlier → later), so this view **records** the pair; the Revisions state (r5 → r4) is the same record **read** and its δs are recomputed from the two runs, not stored again (Ruling 250 Q2) | §1.3, §1.7 |
| Loading | pickers disabled, the panel's skeleton bars, "Evaluating the estimator…" | §5.4 |
| Error | `DOC-SIZE` on record; the error card; inputs only, no δ | §5.4 |

## Fixture numbers and how each δ is computed

Common: water 15 °C, 35.165 g/kg, ρ = 1026.02 kg/m³; V = 5.14444 m/s (10 kn); α = 3.0°; q = ½ρV² = 13576.9 Pa.
δ = (compared − base) / |base|. e = CL² / (π AR CDi), AR = b² / S. L = q S CL, D_i = q S CDi.

| Run | S_ref m² | b_ref m | CL | CDi | e | L N | D_i N |
|---|---|---|---|---|---|---|---|
| r5 VLM (Current) | 0.108 | 0.900 | 0.4120 | 0.007600 | 0.947916 | 604.1 | 11.14 |
| r4 VLM (Historical) | 0.108 | 0.900 | 0.4035 | 0.007310 | 0.945276 | 591.7 | 10.72 |
| r3 VLM (Historical) | 0.100 | 0.850 | 0.4210 | 0.008120 | 0.961658 | 571.6 | 11.02 |
| estimator, r5 inputs | 0.108 | 0.900 | 0.421256 | 0.007532 | 1 (not compared) | 617.7 | 11.04 |

Estimator (Helmbold–Diederich at Λ_c/2 = 0, which equals Helmbold; "estimator, not validated"): CLα = 2π AR / (2 + √(AR² + 4)) = 4.82724 /rad at AR 7.5; α_L0 = −2.0° (fixture value), so
CL = 4.82724 × 5° in radians = 0.421256; CDi = CL² / (π AR) = 0.0075315.

| Pair (base → compared) | δ CL | δ CDi | δ e |
|---|---|---|---|
| r5 → estimator (Tiers, shown) | +2.25 % (0.0224653479) | −0.90 % (−0.0090154117) | Not compared; the mutant would print +5.49 % |
| estimator → r5 (Tiers, stored) | −2.20 % (−0.0219717450) | +0.91 % (0.0090974287) | Not compared |
| r5 → r4 (Revisions, recomputed view of the stored record) | −2.06 % (−0.0206310680) | −3.82 % | −0.28 % |
| r4 → r5 (Historical base, stored) | +2.11 % (0.0210656753) | +3.97 % (0.0396716826) | +0.28 % (0.0027926419) |
| r5 → r5 at 20 °C | blocked: water (temperatureC, rho, nu) | — | — |
| r5 → r3 | blocked: S_ref, b_ref, c_ref differ | — | — |

L and D_i carry no δ (same q · S_ref, so the fractions equal CL's and CDi's).

## Not on this page

Comparison charts and overlays (ANA-10), case schedules and per-case deltas (Area 4), the reconcile action
(DR-A3D-3), uncertainty values (no A3 tier reports one), the CLI output (design §3), and the rest of the shell (the
panel sits alone; the tab strip is the existing Analysis panel's).
