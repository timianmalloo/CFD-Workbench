---
id: proof-wri-probe-mac-join-note
title: "WRI probe - Mac join note (Ruling 178 conditions)"
type: doc
status: done
owner: "@mac-leader"
tags: [windows, dpi, proof]
links:
  - { to: review-pr-20, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  The P6 attempt in this probe predates Ruling 177 and ran on 93240b06 without the WDF fixes; it is not the Ruling 177 P6,
  which is still owed. Kept beside the receipt so the PC's capture-manifest-pinned receipt stays byte-identical.
---

# WRI probe — Mac join note

Added by the Mac at the PR #20 join, under Ruling 178's conditions. It is a separate file because `receipt.md` is pinned
by `capture-manifest.json`: editing it would break the capture check.

- The P6 attempt in this receipt predates Ruling 177. The ruling was recorded at 02:31:37Z, and this cycle ran from
  02:22:27 to 02:22:50Z.
- Its head, 93240b06, does not contain the WDF fixes, which are in main 9a250e3c and later.
- It is not the Ruling 177 P6. That P6 is still owed: a head at or after 9a250e3c, no AppliedDPI gate, and the in-process
  RenderScaling printed before the first check. It runs the nine named checks at 150 % and then 200 %, prints the P3
  clauses in the same run, prints the item-6 fact TextBlock bounds, and ends with a committed restore.
