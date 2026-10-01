---
id: proof-m12b-native
title: M1.2b native attach evidence
type: proof-pack
status: blocked
owner: "@track-u3a"
phase: implementation
tags: [m12b, native, review, attach, accessibility]
links:
  - {to: design-m12b-points, rel: depends-on}
review-by: 2026-10-30
summary: >-
  Twelve bounded CUA attach attempts against two foreground macOS review launches returned
  cgWindowNotFound. Every launch-bound attach check exited 1 with NATIVE_REVIEW_BLOCKED;
  no screenshot, AX dump, interaction, or content assertion was available.
review-suggested: []
---

# M1.2b native attach evidence

Source HEAD: `a44f951e279ebf2c3277bdf9b48dd11252f6c40d` on `m12b-u3-attach`.
The app was published with `dotnet publish` for `osx-arm64`, packaged with
`tools/package-application.py`, and copied to a unique path with bundle ID
`com.cfdworkbench.desktop.u3aa44f9510500`. The binary SHA-256 was
`cc700004034184f372fdcd146550462aabae7e179d1ff33f1d46098bc3e121b4`.
The prelaunch guard reported empty `blocked`, `matched`, `retained`, and `unknown`
arrays before each of the two launches. [Empty-state launch](launch.json) served
N-B01; [example-state launch](launch-example.json) served N-B02–N-B12. Both used
the `keyboard` persona and `system` theme. Both were terminated after their group;
`pgrep -fl CfdWorkbench` returned no match.

Each row called `attachReview` once in a fresh CUA session. Each call made the
helper's bounded three path/ID/path attempts and returned `blocked`, with
`Error: Computer Use server error -10005: cgWindowNotFound` on every attempt.
The corresponding `check-review-attach.mjs` command exited 1 and printed
`NATIVE_REVIEW_BLOCKED: exact launch-bound AX/screenshot readiness is absent`.
The per-row `check.txt` files transcribe those observed command results. There was
no UI control after the blocked attach and no content assertion result to report.

| Row / demo step | Attach receipt | Check exit / result | Screenshot | AX dump | Content assertion result | Status |
|---|---|---|---|---|---|---|
| N-B01 · New foil / Plan | [receipt](N-B01/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B01/check.txt) | — | — | Rail and point pixels not sampled; no screenshot | blocked |
| N-B02 · Hover / probe | [receipt](N-B02/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B02/check.txt) | — | — | Hover text and glyph not observed; attach failed | blocked |
| N-B03 · Select point | [receipt](N-B03/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B03/check.txt) | — | — | Selection and Properties not observed; attach failed | blocked |
| N-B04 · Drag / Undo | [receipt](N-B04/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B04/check.txt) | — | — | Drag, Wing preview, status, and Undo not observed; attach failed | blocked |
| N-B05 · Nudge | [receipt](N-B05/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B05/check.txt) | — | — | Nudge increments and Undo grouping not observed; attach failed | blocked |
| N-B06 · Edge crossing | [receipt](N-B06/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B06/check.txt) | — | — | Crossing marker and refusal not observed; attach failed | blocked |
| N-B07 · Point type / tangents | [receipt](N-B07/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B07/check.txt) | — | — | Type, handles, and tangent changes not observed; attach failed | blocked |
| N-B08 · Save / reopen | [receipt](N-B08/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B08/check.txt) | — | — | Persistence and Foil source not observed; attach failed | blocked |
| N-B09 · Typed chords | [receipt](N-B09/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B09/check.txt) | — | — | Root/Tip chord results not observed; attach failed | blocked |
| N-B10 · Keyboard only | [receipt](N-B10/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B10/check.txt) | — | — | Point peer bounds, focus, Tab order, and VoiceOver trace not captured; no AX dump | blocked |
| N-B11 · Pointer navigation | [receipt](N-B11/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B11/check.txt) | — | — | Zoom, pan, and context menu not observed; attach failed | blocked |
| N-B12 · Curvature comb | [receipt](N-B12/attach.json) | [1 · NATIVE_REVIEW_BLOCKED](N-B12/check.txt) | — | — | Comb toggle and live drag not observed; attach failed | blocked |

This is an attach error record for the Claude U3-review track. It makes no UI
judgement and does not assert that any demo step passed.
