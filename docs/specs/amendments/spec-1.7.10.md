---
id: spec-amendments-1-7-10
title: "Spec 1.7.10 amendment note — the 32 MB document cap (A8.5 parser caps, native-document boundary row)"
type: spec
status: accepted
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, security, size-cap, native-document, ruling-258, ruling-263]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-amendments-1-7-9, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: >-
  Two proposed amendments to cfd-workbench-v1 that restate the 8 MB native-document limit as 32 MB (32,000,000 bytes),
  the value Ruling 263 set when it amended Ruling 258: the parser-caps line of the Security row and the "8 MB+ file"
  fixture of the native-document boundary row. Accepted by Ruling 264 and applied to the spec in the same change.
---

# Spec 1.7.10 amendment note — the 32 MB document cap

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md). **Status:** both rows
are **proposed**; the spec owner accepts and applies them. The spec file is not edited by this note.

## How to read this

- One row is one amendment, in the form of [spec 1.7.9](spec-1.7.9.md). *Before* quotes the current spec text (line
  numbers are those of the spec when this note was written); *After* is the exact proposed text.
- Why a spec change is needed: [Ruling 258](../../notes/rulings.md) raised `NativeProject.MaxBytes` above 8 MB, and
  [Ruling 263](../../notes/rulings.md) amended it to 32,000,000 bytes, which meets Ruling 258's unchanged budget (open
  and save within 3 s, peak working set within 1.5 GB at the cap) as measured. Two spec lines still state 8 MB as the
  current limit.
- Not changed: the 4 kB line cap, the point cap of 10⁵, the assistant excerpt caps, and the format and its version
  (Ruling 263).

## Amendments (2)

| ID | Spec clause | Before (quoted) | After (exact proposed text) | Source | Status |
|---|---|---|---|---|---|
| AM-1.7.10-1 | Non-functional table, Security row (:1315), the parser caps | `parser caps published (file ≤ 8 MB, line ≤ 4 kB, points ≤ 10⁵;` | `parser caps published (file ≤ 32 MB = 32,000,000 bytes, line ≤ 4 kB, points ≤ 10⁵;` | Rulings 258, 263 | proposed |
| AM-1.7.10-2 | Boundary table A8.5, Native document row (:1365), the negative-test fixture | `tampered hash; 8 MB+ file` | `tampered hash; file one byte over 32,000,000 bytes (refused, original kept)` | Rulings 258, 263 | proposed |

Reasons, one sentence each. AM-1.7.10-1: the published file cap is the constant every size guard reads, and Ruling 263
sets it to 32,000,000 bytes. AM-1.7.10-2: the negative test now sits at the new cap, and Ruling 263 requires that one byte
over is refused with the file kept intact.

## What does not change

- The fail-closed behaviour of the Native document row ("fail closed; original kept") is unchanged.
- An older build still refuses a document over 8 MB with DOC-SIZE and never truncates it (Ruling 263).
- Lowering the cap later needs a migration note (Ruling 263).
