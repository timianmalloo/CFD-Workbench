---
id: kb-visible-presentation
title: Visible presentation timing evidence
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
phase: application-foundation
tags: [performance, native-ui, evidence]
links:
  - {to: note-m1-scope-decision, rel: depends-on}
  - {to: coordination-contract-c-native, rel: relates-to}
  - {to: review-ui-application-native, rel: relates-to}
  - {to: kb-visible-presentation-glossary, rel: uses-term}
review-by: 2026-12-24
summary: Current metrics measure compositor completion. Documented macOS presentation signals exist, but neither is integrated or qualified; this evidence packet defines the remaining measurement seams without claiming a timing pass.
---

# Visible presentation timing

Goal: establish what could honestly measure M1's visible cold-start, edit,
Preview and Cancel latency. Done when primary sources, current code, uncertainty,
failure controls and a bounded next spike are independently reviewed. Not in
scope: capture execution, permission changes, renderer replacement, production
instrumentation, timing trials or altered acceptance. T2; global active cap three.

This is a narrow research branch of the existing application graph, concurrent
with Windows W0. Root owns these eight knowledge files; Coordinator independently
reviews sources and scope. No additional agent or competing implementation plan.
The surface list for a future measurement is input/process-start clock → operation
and source/draft identity → final required UI regions → presentation observation →
raw receipt/uncertainty → percentile/gate → proof and normal-path telemetry.

Grounding followed the approved decision's typed links to the C contract and
architecture, then the C contract's native review link. The graph context tool
returned six decision chunks; relevant source/contract clauses were read directly.
Existing V16 suggestions remain review obligations, not proof that all neighbors
have been reconciled. See [references](references.md) and [sources](sources.md).

## Findings and confidence

1. **Verified code:** R39 `MainWindow.QueueNativeMetric` observes a fresh compositor
   batch and correlates source/draft/frame state. Its emitted endpoint explicitly
   says `fresh_target_batch_cycle_not_presentation`. `Program.ManagedStartTicks`
   begins inside Main. Neither proves the approved visible budgets.
2. **Verified documentation:** Apple exposes a WindowServer display-event timestamp
   in ScreenCaptureKit and drawable presentation information in Metal. Their
   documented meanings are distinct from a callback's arrival time.
3. **Verified contradiction:** pinned Avalonia's XML/comment says OpenGL then
   software, but its actual initializer orders **OpenGL, Metal, Software**. The
   app sets no renderer override. The inspected Metal interop exposes
   texture/size/scaling, not a drawable-presentation callback. **Flagged runtime:**
   the actual selected renderer has not been observed here.
4. **Inferred option:** a calibrated capture observer could retain the existing
   renderer and identify the final visible frame. It needs a supported, authorized
   capture route, exact clock calibration and pixel/operation correlation first.
5. **Flagged:** no current endpoint, reference-host pair or full workload qualifies
   an M1 p95 result. The 64 GiB M4 Max investigation host is not the stated 16 GB
   reference Mac. A smaller fixture cannot silently replace the product workload.

## Next spike, not an architecture decision

Owner should choose a bounded measurement-contract spike after this packet's
independent review. Compare a supported capture route with an exposed renderer
presentation route. Prefer a route that measures the unchanged packaged app;
do not select Metal solely to obtain a convenient clock and call it equivalent.
First prove clocks, final-region identity and failure refusal with a controlled
visible transition and injected delay. Only then dispatch product workload trials.

The current CUA API supplies screenshots/AX but no frame-presentation timestamp.
Its call duration cannot become the 100 ms metric. No alternate capture or input
automation was executed in this research. Introducing such a route requires
its own supported mechanism and applicable authorization; a CUA permission does
not establish permission for a new helper. No human permission request is made
until a concrete, reviewed route requires it.

Read [comparables](comparables.md), [measurement model](data-and-constants.md),
[open questions and falsifiers](open-questions.md), and [glossary](glossary.md).
Monetary cost and token usage: **not recorded**. The isolated skill marker starts
at 04:12:37 UTC; earlier read-only research is outside that measured span.

Independent source/Simplifier disposition: **PASS for this evidence packet** by
the Sol coordinator on 2026-09-25. Review checked SDK hashes, timestamp semantics,
the pinned Avalonia interop boundary, current metric endpoints, the inferred
interval model and the eight-file scope. The stale XML/default inference was
corrected against the executable initializer. Raw immutable source confirmed
the ledger's physical line numbers after the review tool's normalized text view
gave different numbers. API-source verification is not runtime qualification,
and this packet clears no M1 performance gate.
