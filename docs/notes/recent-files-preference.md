---
id: note-20260927-recent-files
title: "Recent files are kept in their own preference file, separate from the layout file"
type: decision-note
status: in-review
owner: "@timianmalloo"
phase: design — M1.2a (spec 1.6)
tags: [decision-note, desktop, preferences, recent-files, privacy]
links:
  - { to: design-app-shell, rel: relates-to }
  - { to: adr-0009-cad-first-shell, rel: refines }
review-by: 2027-03-25
review-suggested: []
summary: >-
  Start's Recent group (CAD-14) is backed by recent.json beside layout.json, written through the same ProjectStore,
  at most 10 absolute paths, cleared by File ▸ Open Recent ▸ Clear Menu. The paths are personal data, so they never
  enter the layout file or telemetry.
---

# Recent files are kept in their own preference file

- **Kind:** decision
- **Confidence:** Verified for the need (CAD-14 names Recent; architecture §10.2 has no store for it); Inferred for the
  shape (no code yet)
- **Made during:** `/design-slice` of the app shell (`docs/design/app-shell.md` §4.5), 2026-09-27

## The call

CAD-14 requires a **Recent** group on Start, and nothing in architecture §10 or ADR-0009 stores it. The list lives in
`<ApplicationData>/CFD-Workbench/recent/recent.json` (its own subdirectory, so its own store claim) (`format: cfdw-recent`, `version: 1`), written through the existing
`ProjectStore` like the layout file: at most 10 unique absolute paths ending `.foil` or `.cfdw.json`, most recent first,
added after each successful open, emptied by **File ▸ Open Recent ▸ Clear Menu** (a failed Clear is reported, never silent). Reason for a separate file: the list
changes on every open while the layout changes at three save points; the paths are personal data and the layout file
must stay free of them; a corrupt recent list must not reset anyone's layouts.

## Alternatives dismissed

- **A `recent` member in `layout.json`** — mixes personal data into a file that otherwise holds none, and couples two
  lifecycles in one compare-and-swap.
- **The macOS recent-documents list (NSDocumentController)** — Avalonia 11.3 exposes no API for it (Inferred, not
  spiked); a native bridge is not worth it for ten paths.
- **No persistence (session-only Recent)** — a Recent group that is empty at every launch fails CAD-14's intent.

## Validation condition

Holds until a second consumer needs the list (for example the CLI) or the operator asks for per-project recents.

## Promotion rule

If the preference files gain a third member or a sync story, promote the preference-store shape to an ADR.
