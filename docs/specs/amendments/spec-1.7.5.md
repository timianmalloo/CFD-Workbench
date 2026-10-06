---
id: spec-amendments-1-7-5
title: "Spec 1.7.5 amendment batch — group move on a curve (CAD-04) and the wing-only drag (A5.6, ANA-03), as exact text"
type: spec
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, rulings, cad, analysis, drag]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-2, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  Two clauses to cfd-workbench-v1 and one retired note, traced to Rulings 107 and 108. The CAD-04 clause and the retired
  node M note are approved (Ruling 107 DR-GM-8). The A5.6 and ANA-03 clause is the hydrodynamicist's form of Ruling 108
  DXM-5, which differs from the ruling's text; the operator approved it as Ruling 109. Revision 1.7.5 of the spec
  carries the batch; the change record is Appendix H, section H.5.
---

# Spec 1.7.5 amendment batch

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.5.
**Status:** CAD-04 and the node M note are **approved** (Ruling 107, 2026-10-06). The A5.6 and ANA-03 clause is
**approved — Ruling 109**.

## How to read this

- **One row is one amendment**, in the form of [spec 1.7.2](spec-1.7.2.md). *Before* quotes the 1.7.4 text exactly; *After*
  is the exact new text in revision 1.7.5 (struck text is superseded and stays in place).
- **Source:** Ruling 107 DR-GM-8 for the first two rows; Ruling 108 DXM-5 and the hydrodynamicist's verdict for the third.
- The copy rows are DESIGN.md §7 COPY-250 onward (track DOC). No code string changes in this revision.

## Amendments (3)

| ID | Spec clause (line: the 1.7.4 spec) | Before (quoted) | After (exact new text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.5-1 | CAD-04 (:1233), end of the row | `values are not announced per nudge — M1.2b D-1, accepted with the design by Ruling 57)*. \|` | the same, then *(1.7.5, Ruling 107 DR-GM-8: A drag, nudge or typed value of several points on one curve is one draft and one undo step; a limit holds the whole group; the typed entry refuses and never rewrites.)* | Ruling 107 DR-GM-8 | approved |
| AM-1.7.5-2 | F11 node M note (:2125) | `*(1.7: node M's typed clause — a typed value sets every point — is not built in M1.2. Several selected points show Mixed values read-only until a multi-point draft exists (M1.2b OI-3, accepted with the design by Ruling 57). The requirement stands.)*` | the note struck through, then *(1.7.5, Ruling 107 DR-GM-8: this note is retired; a typed value of several points on one curve is one draft, see CAD-04.)* | Ruling 107 DR-GM-8 | approved |
| AM-1.7.5-3 | A5.6 (:1000-1001) and ANA-03 (:1200), changed together | A5.6: `Total drag is Unavailable when a component is missing and lists the omitted components per tier (junction, mast, wave, spray, tip-vortex cavitation).` ANA-03: `**Given** CD ≤ 0, V ≤ 0 or a missing component, **then** Undefined or Unavailable with the cause.` | each followed by a *(1.7.5, approved — Ruling 109: …)* clause: the wing result shows its drag in one row, "Drag (Wing only)", value "<min>–<max> <force unit>" (the Ncrit 2–4 band; N in Metric, lbf in Imperial), surrogate label and low-confidence flag kept, note "Wing only: induced (VLM + strip) plus profile (polar). Not a total.", reason line "Not included: junction, mast, wave, spray"; this row replaces the separate "Wing-only drag" row (AnalysisProjection.cs:81 and :134); the craft Total drag stays Unavailable (Loads.TotalDrag unchanged); the craft CL/CD stays Unavailable and only "Wing-only CL/CD" shows a number; tip-vortex cavitation stays under Not modelled | Ruling 108 DXM-5; hydrodynamicist verdict CLEAR WITH CONDITIONS (a bare "Total drag" label beside a number is a BLOCK) | approved — Ruling 109 |

## Where the proposal differs from Ruling 108

Ruling 108 DXM-5 reads: the Total drag row shows the wing drag (induced + profile) marked "Wing only", with the omitted
components (junction, mast, wave, spray) listed beside it. The hydrodynamicist's form keeps the craft Total drag
Unavailable, labels the number "Drag (Wing only)" instead, and adds the reason line "Not included: junction, mast, wave,
spray". The operator approved the hydrodynamicist's form as Ruling 109. The approved row COPY-330 (DX row 49, "Wing only: induced (VLM + strip)
plus profile (polar). Not a total.") is the note in both forms.
