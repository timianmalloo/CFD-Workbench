---
id: note-20260926-command-table-selection
title: "The CAD-first shell has one command table and one selection state; telemetry reuses the apply event"
type: decision-note
status: in-review
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [decision-note, desktop, commands, selection, telemetry]
links:
  - { to: adr-0009-cad-first-shell, rel: refines }
  - { to: architecture-application, rel: relates-to }
review-by: 2027-03-25
review-suggested: []
summary: >-
  Every CAD verb is one row in a command table that feeds the native menu, toolbar, palette and key bindings on every
  window; the controller owns one selection state that every view and pane observe. Applies of every kind emit the
  existing apply event with an edit_kind attribute instead of new per-kind events.
---

# One command table, one selection state, one apply event

- **Kind:** decision
- **Confidence:** Inferred for the shape (no code yet); Verified for the spec clauses cited
- **Made during:** `/define-architecture` for spec 1.6, repair cycle 1 (Enterprise finding 6, Simplifier finding 12)

## The call

1. **Command table.** A row is id · title · menu group · platform gesture · enabled predicate · execute. The native menu
   (Application level), the toolbar, the command palette and the key bindings of the main window and every float are
   generated from it. Reason: CAD-21 and B1 require every verb in a menu, the palette and a keyboard route; one
   definition makes that a test (every row reachable in all three; no menu item without a row) rather than a review.
   Edit verbs route to a focused text field first (ADR-0009 §3).
2. **Selection state.** The controller owns one value — none · foil · station · point(s) with the curve and point ids.
   Views, the Points grid and Properties observe it and never hold their own. Reason: UX-30 "selection agrees across
   every visible view and the Properties pane".
3. **Telemetry.** A gesture, a typed dimension, a point-type change and a section Finish each append one accepted row,
   so each emits the existing apply event with `edit_kind` (`gesture` · `dimension` · `point-type` · `section`), as the
   section editor already does for `profile` (`docs/design/section-editor.md`:115). New event names are kept only for
   work that is not an apply: `estimates.compute`, `section.step`, `library.save`, `layout.load`, `float.relocate`.

## Alternatives dismissed

- Menus, toolbar and shortcuts defined separately — no: the parity CAD-21 asks for would rest on review alone.
- Per-kind apply events — no: four names for one fact (Simplifier).

## Promotion rule

If a second consumer (for example the CLI or an assistant proposal) starts dispatching through the command table,
promote this to an ADR.
