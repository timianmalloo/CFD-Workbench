---
id: mockup-w2-recovery
title: W2 recovery — Clear unfinished save and read-only Open from OneDrive
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, w2, crash-recovery, clear-unfinished-save, onedrive, read-only-open, windows, ruling-259]
links:
  - {to: design-language, rel: depends-on}
  - {to: design-windows-store-recovery, rel: implements}
  - {to: mockup-w2-save-picker, rel: refines}
  - {to: rulings, rel: implements}
review-by: 2026-12-31
summary: >-
  Every state of the Ruling 259 build, shown before any code: the Save is blocked dialog with Clear unfinished save, its
  in-place confirm, the in-use refusal, the failure, the cleared outcome with no recovery wording, and the read-only Open
  from OneDrive (title and status notice, cloud-only refusal, Save refused, Save As at the local default folder, a still
  refused location). Approved copy is verbatim; new strings are provisional DS-COPY rows for the operator.
review-suggested: []
---

# W2 recovery — the mockup

Open [`w2-recovery.html`](w2-recovery.html) over `file://`. The header switches the state (A1 to A6, B1 to B5), the
viewport (1280 × 800, 1500 × 870, 1024 × 700), the theme (light, dark, high contrast) and reduced motion. The query
string `?state=B2&theme=dark` opens a state directly. Below the window, the page lists the flow step and every copy row
the state shows, with its id. The S3 and S4 dialogs keep the look of [`w2-save-picker.html`](w2-save-picker.html)
(variant A, approved by Ruling 160). The design is [`windows-store-recovery.md`](../design/windows-store-recovery.md).

**Display only.** Nothing under `src/` or `tests/` changed. The page makes no network request and loads no file.

## States

| State | What it shows | Design section |
|---|---|---|
| A1 | Save is blocked, now with Clear unfinished save…; step 3 per Ruling 259; DS-COPY-5 | 4, 5 |
| A2 | The confirm (S4) swapped in place; focus and Enter on Cancel; Escape returns to A1 | 2.4, 4 |
| A3 | Clearing: both buttons disabled, `aria-busy` | 2.4 |
| A4 | Refused: a live writer holds a leftover (COPY-449, Try again) | 2.3 |
| A5 | Failed: a foreign `.cfd-` entry, nothing removed (COPY-455 with DOC-CONFLICT) | 2.2 |
| A6 | Cleared: the dialog is gone and the blocked save runs (COPY-452); no clear or recovery sentence | 2.4 I-7, DR-DS-3 |
| B1 | Read-only Open from OneDrive: title suffix DS-COPY-1, status DS-COPY-2 (info), no modal | 2.7 |
| B2 | A cloud-only file refused before any read: DS-COPY-3 and the code line DOC-CLOUD-ONLY | 2.5, 2.6 |
| B3 | Save on the read-only document: the S2 OneDrive refusal (COPY-440, Ruling 146 (1)) | 2.7 |
| B4 | Save As: the Windows picker at the local default folder | 2.7 |
| B5 | A junction or other location still refused: DS-COPY-4 (COPY-590 without "OneDrive") | 2.6 O1, 5 |

## Copy

| ID | Text | Status | Where |
|---|---|---|---|
| COPY-444 | Clear unfinished save… | approved — Ruling 160 | A1 |
| COPY-446 | Clear the unfinished save? | approved — Ruling 160 | A2 to A5 title |
| COPY-447 | This removes the files the unfinished save left in this folder (`.cfd-writer.claim` and any `.cfd-….tmp`). Your saved file and your current work are not touched. It works only if no other CFD Workbench window is using this project. | refined W2 text, held; shown here for the operator to confirm | A2 to A5 body |
| COPY-448 | Clear unfinished save | approved — Ruling 160 | A2, A3 |
| COPY-449 | Can't clear it yet: another CFD Workbench window is still using this project. Close that window, then try again. Your saved file and your current work are unchanged. | approved — Ruling 160 | A4 |
| COPY-455 | Couldn't clear the unfinished save (<code>). Your saved file and your current work are unchanged. | approved — Ruling 160 | A5 |
| Step 3 | Use Save As to keep your current work in a different folder, or choose Clear unfinished save, then Save. | ruled — Ruling 259 | A1 |
| DS-COPY-1 | (read-only: opened from OneDrive) | provisional | B1, B3 title suffix |
| DS-COPY-2 | This project was opened from OneDrive, so it can't be saved there. Use Save As to save it in a local folder. | provisional | B1, B3 status strip |
| DS-COPY-3 | This file is in OneDrive but isn't on this PC yet. The file hasn't been changed and wasn't downloaded. In OneDrive, choose "Always keep on this device" for it, or copy it to a local folder, then open it again. | provisional; seam to WF | B2 |
| DS-COPY-4 | CFD Workbench can't open a project from this location (a network or removable folder, or a linked folder). The file hasn't been changed. Copy the project to a local folder and open it from there. | provisional (Ruling 259 drops "OneDrive"); WF owns the row | B5 |
| DS-COPY-5 | This folder stays blocked until the unfinished save is cleared. | provisional wording of a Ruling 259 rule | A1 |

COPY-441, 443, 445, 450, 452, 440 and 442 are reused as built or approved. No row announces a clear or a recovery.

## Measurement

- **Render** (Edge headless, `--dump-dom` per state, `?state=` for each of the 11 states): every state rendered; no
  `undefined` or `NaN` in any DOM. Screenshots of A2 (light) and B1 (dark) were opened and read: the confirm shows
  Cancel as the focused default; the read-only title suffix and the info notice are legible in dark. Captures stay in
  the session scratch, not in git.
- **Script:** `node --check` on the page script: exit 0. The page holds no `http` or `https` URL.
- **Not done:** the craft gate and a mockup oracle (no oracle exists for this page yet; the build's Desktop checks are
  the oracle for the strings, section 7 of the design); contrast measurement of the new `--info` token in the three
  themes (UX & Accessibility, Q-U4); a screen-reader pass; anything on the real native dialog.
