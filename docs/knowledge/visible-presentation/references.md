---
id: kb-visible-presentation-references
title: Presentation timing requirements and references
type: knowledge
status: in-review
owner: "@cfd-timing-evidence-20260925"
tags: [performance, requirements]
links:
  - {to: kb-visible-presentation, rel: documents}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2026-12-24
summary: Records the approved M1 timing authority and distinguishes it from illustrative prototype and small-fixture diagnostics.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-26, reason: "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims." }
---

# Requirements and references

**Verified authority:** the [approved scope decision](../../notes/m1-scope-decision.md)
keeps Windows x64 and actual visible timing in M1. Its performance discussion
preserves cold launch ≤5 s, numeric feedback p95 ≤100 ms, Preview p95 ≤250 ms and
Cancel acknowledgement ≤250 ms. It does not promote all later results/analysis
targets into this offline increment.

**Verified source:** [product A8.1](../../specs/cfd-workbench-v1.md#a81-iso-25010-table-each-row-a-fixture-or-a-labelled-boundary)
specifies the 21-station/201-slice/50k-triangle/10k-plot-point reference and fixed
16 GB Apple-silicon/Windows x64 machines. Its wider performance table labels
targets proposed. The accepted M1 ruling and C contract make the named M1 proof
obligations explicit; this knowledge base changes neither wording nor scope.

**Verified distinction:** [C's contract](../../coordination/contract-c-native.md)
starts with a bounded display projection, and the [native review](../../reviews/ui-application-native.md)
retains small-Example batch diagnostics. The mockup's illustrative calculations
and a faster small-fixture result do not satisfy the larger workload obligation.

Apple API contracts and the exact Avalonia source revision are listed in
[sources](sources.md). They are evidence for a spike, not a performance standard
or proof that the current product exposes those signals.
