---
id: spec-amendments-1-7-9
title: "Spec 1.7.9 amendment note — Match the blend before Remove station (CAD-02, A4.6, SF2)"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, cad, station, remove, match-the-blend, cad-02, a4-6]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-add-station, rel: relates-to}
  - {to: design-add-station, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: >-
  Two proposed amendments to cfd-workbench-v1 CAD-02 and the A4.6 1.7 note, from section 13 of the Add station design
  (DR-S1-2 option c, the S1 Adversary's missed option): an interior station can be matched to the blend of its
  neighbours, previewed with the measured change and the residual a later Remove would still make, with no gate of its
  own; Remove keeps its 10 um acceptance. Proposed; DR-DF-4 and DR-DF-5 decide the gate and the differing-peaks offer.
---

# Spec 1.7.9 amendment note — Match the blend

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6 (with
the in-review [Add station rows](spec-add-station.md), A-S1-1..4). **Status:** both rows are **proposed**. The design is
[add-station.md §13](../../design/add-station.md).

## How to read this

- One row is one amendment, in the form of [spec 1.7.7](spec-1.7.7.md). *Before* quotes the 1.7.6 text (line numbers of
  1.7.6); *After* is the exact proposed text.
- Why a spec change is needed: CAD-02 describes Remove with an acceptance and no other path, so a station whose removal
  exceeds 10 µm cannot be removed. A4.6's 1.7 note lists the deliberate edits that show a change and have no gate; Match
  is a new one, and without the row it would read as a conversion under the 10 µm acceptance.
- The finding the rows carry: under rule A, Remove after Match is exact only when the neighbours' thickness peaks share
  a chord position; otherwise it changes the loft by the promotion deviation of the neighbours at that η (design §13.3).

## Amendments (2)

| ID | Spec clause | Before (quoted) | After (exact proposed text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.9-1 | CAD-02 (:1245), appended after "root and tip stations cannot be removed." | (no clause) | **When** removing an interior station would change the loft, **then** I can first match it to the blend of its neighbours: the preview shows the measured change and the change a later Remove would still make, both sampled with their grid; after Apply, Remove changes the loft only by that reported amount, which is within 10⁻¹² relative when the neighbours' thickness peaks share a chord position, and Remove keeps its acceptance. | design §13.1, §13.3; DR-S1-2 (c) | proposed |
| AM-1.7.9-2 | A4.6 (:540), the 1.7 note, after the Rebuild to N sentence | `a deliberate **Rebuild to N** on a master curve shows its measured change before Apply and has no gate (A4.2; Ruling 64, DR-PV-3);` | `a deliberate **Rebuild to N** on a master curve shows its measured change before Apply and has no gate (A4.2; Ruling 64, DR-PV-3); a deliberate **Match the blend** on an interior station shows its measured change and the residual of a later Remove before Apply and has no gate of its own (CAD-02);` | design §13.1 item 4; DR-DF-4 (a) | proposed |

## What does not change

- GEO-06 and A-S1-1 (Promote) are unchanged; Match reuses the promoted-profile construction but adds no station.
- Remove's acceptance (Apply disabled above 10 µm, DR-S1-2 (a)) is unchanged and applies after Match too.
- The thickness policy (A-S1-2): Match keeps current thickness; the t/c channel is byte-identical.
