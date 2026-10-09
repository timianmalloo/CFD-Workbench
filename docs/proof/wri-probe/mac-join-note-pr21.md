---
id: proof-wri-probe-mac-join-note-pr21
title: "WRI probe - Mac join note (Ruling 179 conditions, PR #21)"
type: doc
status: done
owner: "@mac-leader"
tags: [windows, dpi, proof]
links:
  - { to: review-pr-21, rel: relates-to }
  - { to: proof-wri-probe-mac-join-note, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  The Ruling 177 comparison in this receipt did not run the ninth named check, and item 2's failure is a second
  zero-tolerance assertion, not the one WDF changed. Written as a new file because the capture manifest pins every other
  file in this folder, including the PR #20 note.
---

# WRI probe — Mac join note for PR #21

Added by the Mac at the PR #21 join, under Ruling 179's conditions. `receipt.md` and `mac-join-note.md` are both pinned by
`capture-manifest.json`, so this note is a new file. It is never edited after this join.

- **The ninth check was not run.** `PropertiesPane_Density_EveryInputDeclaresMinHeightOf24` is absent from the Ruling 177
  comparison (head 43830897). The Ruling 178 supplemental attempt (ac58814f) stopped before any scale change, so the
  check is NOT ASSESSED at both scales. The receipt's row 9 says so.
- **Item 2 is a second assertion.** `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` fails at 150 % on
  `Near(width, grid.Bounds.Width, 0, "arrangement width")` (`ControllerViewTests.cs:622` at the tested head, `:640` on
  main): 647 DIP lays out at 971 px = 647.333 DIP. WDF changed only the later `AssertFourOrOne`. The height assertion
  on the next line has the same zero tolerance (487 DIP comes to 486.667 DIP at 1.5) and is masked by the width failure.
- **Next run.** Per Ruling 179, the nine named checks run on main after the item-2 repair, at 150 % then 200 %, with the
  item-6 fact print and `Process.ExitCode` recorded, after a read-only UIA preflight.
