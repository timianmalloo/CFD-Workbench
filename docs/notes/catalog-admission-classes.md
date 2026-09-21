---
id: decision-catalog-admission-classes
title: Catalog admission by rights class: GEN, VEND, LINK
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [catalog, licensing, sections]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
  - {to: review-spec-v02-critique, rel: relates-to}
review-by: 2027-03-19
summary: Every bundled section carries an admission class with its reason: GEN generated at build from a public-domain definition, VEND redistributed under written terms, LINK cited only. Eppler sections are pending until UIUC terms exist.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)." }
---

# Catalog admission by rights class: GEN, VEND, LINK

**Decision.** A Profile revision in the catalog carries one of three admission classes with the reason shown to
the user (spec v1 A4.10, CAT-01): **GEN** — generated at build time from a public-domain definition with the
generator command and output hash (NACA 4/4-mod/5/16/6/6A); **VEND** — a coordinate file whose redistribution
terms are written down (currently none); **LINK** — cite only (Speer H105, Airfoil Tools, UIUC LSAT polars under
GPL). Presets default to GEN sections and name their pending preference.

**Why.** UIUC states no licence for its coordinate files, Airfoil Tools reserves all rights, LSAT polars are GPL
data (06). Shipping Eppler coordinates would put the product's licence register (COMMIT-02) at risk on day one,
and "the catalog has Eppler" was an unverified 0.2 claim. A class with a reason turns a legal fact into a
product state the user can see, and admission fails closed (ANA-09).

**Rejected.** Bundling UIUC files under an assumed academic-use norm; re-deriving Eppler sections from published
figures (a conversion with an unknown residual and the same rights question); a catalog with no rights field.

**What it constrains.** The build-time polar pipeline (only GEN and VEND sections get polars); the CAT-01 ranking
(Pending admission is excluded); the Sections IA (class chip on every entry); the release gate (GEN-only is the
default fallback while the UIUC and H105 e-mails are outstanding).

**Confidence.** Verified absence of terms (06, dated pages). Whether UIUC will grant terms is unknown; the GEN-only
release is the plan of record. Owner lenses: Product Strategist · Security & Identity Architect.
