---
id: plan-windows-gpu-f1-r230-failure-review
title: "Independent R230 failure receipt review plan"
type: doc
status: complete
owner: "@pc-f1-r230-failure-review"
tags: [review, failure, cleanup, offline]
links:
  - { to: review-windows-gpu-f1-r230-failure, rel: relates-to }
  - { to: defect-classes, rel: depends-on }
review-by: "2026-10-20"
summary: "Bounded receipt verification and offline diagnosis with explicit confidence limits."
---

Goal: verify exact cecafe4e/901bc8d failure evidence and diagnose as far as retained
evidence permits. Done when confidence-labeled findings and next-ruling
requirements are pushed. Tier T1, no delegation, repair, live WSL/registry,
lifecycle/helper rerun, GitHub message or mail. Existing optimize-graph sequence
is reused. Local live files may be read but sensitive raw content is not copied.

| Node | Goal / input | Exit oracle | Capability | Tier | Dependency |
| --- | --- | --- | --- | --- | --- |
| A | Exact receipt/binding/manifest | Committed byte and sequence verification | Deterministic mechanics | T0 | none |
| B | A; source and available diagnostic metadata | Verified/inferred/flagged cause boundaries and offline import control | Independent review | T1 | A data |
| C | A/B; publication | Review, lessons, audit/index, commit/push and clean status | Independent review | T1 | B decision |

Three mandatory nodes and span three; no fan-out or retry loop. Diagnosis stops
where raw child evidence ends. No invented exception or live process state may
cross that boundary. Actual: manifest69 passes, probe0.542s measured, import
defect reproduced before any process call; live cause remains inferred. One
Windows search-glob invocation was corrected using rg's file filter; no candidate
source or evidence changed. Audit records whole-run duration; tokens unavailable.
