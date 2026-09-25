---
id: kb-visible-presentation-data
title: Timing units, uncertainty and acceptance data
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, measurement]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Proposes interval-based latency evidence and lists the fixed workload, identity, clock and failure information a future spike must retain.
---

# Measurement data and uncertainty

**Proposed model, Inferred:** if the input time is bounded by `[Ilo, Ihi]`, and
validated consecutive observations bound first final visible state by
`(Fold, Fnew]`, then nonnegative latency is bounded by
`[max(0, Fold - Ihi), Fnew - Ilo]`. These bounds are useful only after clock
conversion error is included and the preceding frame is proven nonfinal.
Missing frames or an unbounded input timestamp must widen the interval or make
the result Not assessed. Do not infer a fixed capture interval from requested FPS.

**Proposed gate:** report per-trial lower/upper bounds and the chosen percentile
definition. An upper bound at or below the budget supports a pass; a lower bound
above it supports a failure; overlap is inconclusive. State how timeout/failure
trials enter the distribution before sampling. No unobserved trial is dropped.
Sample count, cold-cache procedure and percentile estimator remain design choices;
the old 30 synthetic trials are not automatically the new protocol.

**Verified units:** the installed Apple SDK specifies mach absolute units for
ScreenCaptureKit display events, while Metal documents host seconds. Current
application metrics use `Stopwatch`. No common epoch is assumed. Pair timestamp
samples and retain conversion parameters, observed drift and uncertainty;
negative or inconsistent deltas fail qualification. [Sources](sources.md).

**Required product data, Verified:** workload and budgets are in
[references](references.md). A trial receipt needs package/source hash, process
identity, OS/device/display/refresh/DPI, fixture counts, operation ID, accepted
revision/source hash, draft/generation, start event convention, final-region
oracle, raw timestamps/clock domains, dropped/incomplete frames, outcome and
measurement uncertainty. These are proposed receipt fields, not a new project
document or stored geometry authority.

Cold start includes process creation and runtime work before Main; warm-up cannot
be counted as cold. Edit checks inspector/status/canvas; Preview checks the final
correct preview rather than Assessing; Cancel checks accepted-state acknowledgement
and later stale-result rejection. A nominal 120 Hz display does not establish an
8.33 ms bound on end-to-end observation.
