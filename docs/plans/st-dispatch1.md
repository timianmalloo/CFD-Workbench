---
id: plan-st-dispatch1
title: ST dispatch 1 execution and evidence
type: doc
status: active
owner: "@st"
tags: [store, testing, coordination]
links:
  - {to: coordination-wave3-next-steps, rel: implements}
  - {to: coordination-next-best-six-wave2, rel: depends-on}
  - {to: proof-st-store-codes, rel: tested-by}
review-by: 2026-11-26
summary: Serial red-green implementation of wave 3 ST items 1 to 5, bounded by the coordinator's file ownership and one final full-suite run.
---

Goal: implement ST items 1–5. Done when every item has observed red and green evidence and the requested suite has run. Dispatch 2 is excluded. Tier T1, width one, budget 45 calls / 90 minutes. The explicit assigned worktree and branch are retained.

The selected graph was five serial red-green increments, then one full suite and the Return. Each increment consumes the brief and current code, exits on its named red and green checks, and commits before the next increment. Serial ordering is the brief's instruction and avoids concurrent edits/builds in this tree. No sub-agents were launched.

| Node | Capability | Input → exit | Tier | Dependency |
|---|---|---|---|---|
| Ground / boundaries | Reasoning | brief → allowed surfaces enumerated | T1 | none |
| Codes | Deterministic mechanics | store causes → CLI / status / StartView / preferences red then green | T1 | grounding |
| Drive | Deterministic mechanics | refused drive path → drive and NTFS remedy red then green | T1 | codes |
| Reparse | Deterministic mechanics | Windows FormatMessage → mapping red then green | T1 | drive |
| Dispatcher | Deterministic mechanics | injected probe and real dispatcher → red then green for missing and existing paths | T1 | reparse |
| Operations | Deterministic mechanics | real store actions → four distinct operation checks red then green | T1 | dispatcher |
| Suite / proof | Deterministic mechanics | committed fixes → one suite run, proof, audit and Return | T1 | operations |

```mermaid
flowchart LR
  Ground --> Codes --> Drive --> Reparse --> Dispatcher --> Operations --> Suite
```

All red/green gates and final-suite execution are immovable. The loop variant is the set of unmet assertions in the current increment; floor zero, exit named checks green. Budget expiry requires a partial Return, never skipping a gate. A boundary hit blocks that item only, per Amendment 2. Re-plan on a rejected claim, unowned required edit, or new test failure. The shared controller changes reach only its constructor test seam and open/save regions; the one harness wiring line remains one line.

Before/after topology: seven nodes, width one, one bounded verification loop per item; no speculative speed estimate. Inferred normalized work and span are both seven stage units, so parallelism cannot shorten the selected critical path. Actual red and green observations are in the linked proof. Rework: compile-time test-seam corrections, nullable-flow annotation, and qualifying Avalonia's real dispatcher after its uninitialized dispatcher returned true on a worker thread. No independent persona clearance is claimed here; coordinator review remains the join's responsibility.

Actual delivery: five green focused increments and the requested one full-suite invocation; suite-discovered ST regressions fixed with focused verification. The full suite remains red due to the known CLI environment defect and was not repeated. Wall time about 22 minutes from audit start to proof finalization (measured UTC timestamps); token usage is not exposed by the harness.
