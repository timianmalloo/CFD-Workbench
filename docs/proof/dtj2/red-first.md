---
id: proof-dtj2-red-first
title: Track DTJ2 red-first record (desktop-slots measurement classifies expected failures)
type: doc
status: done
owner: "@trk-dtj2"
phase: implementation
tags: [proof, test-ring, desktop, windows]
links:
  - { to: plan-test-cost, rel: relates-to }
review-by: "2026-11-13"
summary: >-
  Red and green runs for the measurement script classifying listed expected failures as the ring does, and the best-N pick on the verdict column.
---

# DTJ2 red-first

Defect: `tools/measure-desktop-slots.sh` recorded the raw Desktop harness exit. On Windows the harness always exits 1 (listed
expected failures), so every sample read as a failure and no best N could be chosen (PC, PR #38, N=2, 341,770 ms).

Check: `tools/measure-desktop-slots.sh --self-test` (no build; under 1 s). It classifies a log with one listed failure (want `pass`),
one with a listed plus an unlisted failure (want `fail`, names the unlisted one), a TIMEOUT-only log, a stale listed pass
(`unexpected-pass`), the Mac rule (verdict follows the raw exit), and picks a best N from a CSV whose rows have raw exit 1 but verdict `pass`.

## Red (old code; the self-test added before `classify_log` and before the picker read `verdict`)

1. Self-test before `classify_log` existed: exit 1, `classify_log: command not found`.
2. Self-test with `classify_log` in place and the picker unchanged: exit 1,
   `SELF-TEST FAIL: best N from the verdict column` and `BEST none: every slot count had a TIMEOUT or a failing run`
   (the PC symptom: raw exit 1 disqualifies every N). Output kept in `red-run2.txt`.

## Green

`tools/measure-desktop-slots.sh --self-test`: exit 0, `measure-desktop-slots self-test ok: 6 classifications, 1 best-N pick`
(`green-run.txt`).

## Not run here

The real Desktop calibration (needs the PC; the Mac has no expected failures). The CSV gains a `verdict` column; an older CSV without it
makes the picker fail with a KeyError, so re-measure rather than reuse.
