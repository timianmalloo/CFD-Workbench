---
id: kb-visible-presentation-comparables
title: Comparable timing approaches
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, evidence]
links:
  - {to: kb-visible-presentation, rel: documents}
review-by: 2026-12-24
summary: Compares the current batch diagnostic with documented Apple presentation and frame-observation mechanisms without treating any as qualified.
---

# Comparable approaches

| Approach | Strength | Missing obligation | Confidence |
|---|---|---|---|
| Current R29/R39 normal-path batch metric | Existing operation/source/draft correlation and stale/cancel refusal | Actual visible endpoint, pre-Main cold-start interval, full workload | Verified code; [native review](../../reviews/ui-application-native.md) |
| Apple Metal drawable callback | Presentation event and host-time value for the actual drawable | Accessible seam in the pinned toolkit, product-region correlation, dropped-frame handling, runtime backend proof | Verified API; integration Flagged; [source](sources.md) |
| Apple ScreenCaptureKit frames | Pixel evidence plus WindowServer display-event metadata independent of the application's renderer | Supported capture route, permission, target/display identity, clock calibration, complete final-region recognition and measured gaps | Verified API; proposed integration Inferred; [source](sources.md) |
| Current CUA screenshot/AX | Already-supported independent inspection of rendered correctness | No exposed frame timestamp; tool round trip is not input-to-display latency | Verified tool contract and [attachment investigation](../../investigations/review-window-attach.md) |

**Inferred design implication:** the capture option may avoid a renderer change,
but acquisition itself can perturb latency and requires measurement with and
without the observer. The Metal option is not automatically cheaper: exposing
an internal drawable may introduce a new dependency-maintenance boundary.
Neither option is adopted by this comparison.
