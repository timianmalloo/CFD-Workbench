---
id: decision-ncrit-pair
title: Every polar at the Ncrit pair {2, 4}, shown as a band
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [hydrodynamics, polars, evidence]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
  - {to: review-spec-v02-critique, rel: relates-to}
review-by: 2027-03-19
summary: Section polars are computed at Ncrit 2 and 4 and shown as a band labelled as a practitioner range with no measured water N-factor; a single-Ncrit polar needs a recorded user override.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)." }
---

# Every polar at the Ncrit pair {2, 4}, shown as a band

**Decision.** Every polar sample carries its Ncrit, the product computes each polar at **both** Ncrit 2 and 4, and
the chart shows the pair as a band labelled "practitioner range; no measured water N-factor; Day 2019 used 4"
(spec v1 A5.3, ANA-01, UI-15). A single-Ncrit polar is admissible only with a recorded user override. The
surface state (clean · tripped) is a second band or reads "Unavailable — not computed".

**Why.** No measured N-factor for water exists in the knowledge base (06 D7, 07); a single constant (0.2 wrote
2.0) presents a modelling choice as a fact and hides the sensitivity a transition-dependent quantity has at
Re 2×10⁵–10⁶. The band makes the uncertainty visible in the same place as the number, which is the product's
standing rule for every honest limit.

**Rejected.** Ncrit as a free user input with no default band (unfalsifiable comparisons between users); a single
value calibrated later (a promise, not a control); averaging the two (A2 non-goal: averaging across tiers or
settings).

**What it constrains.** The Polar sample grain (Ncrit in the key); catalog polar build pipeline cost (two runs per
Re per surface state); chart legends and the overlay budget (UX-16); the feasibility report (both Ncrit per cell).

**Confidence.** Verified that the band is the practitioner range and that Day 2019 used 4 (06, 07). The choice of
exactly {2, 4} rather than {1, 4} or {2, 5} is Inferred; calibration against Day 2019 is the named exit evidence.
Owner lens: Hydrofoil Hydrodynamicist.
