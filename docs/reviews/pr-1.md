---
id: review-pr-1
title: "PR #1 (Windows PC) - W-0..W-5 execution plan, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine]
links:
  - { to: coordination-windows-w0-w5-execution, rel: relates-to }
  - { to: coordination-two-machine, rel: depends-on }
  - { to: coordination-pc-kickoff, rel: depends-on }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS. The PC's docs-only W-0..W-5 plan follows two-machine.md and pc-kickoff.md; six conditions
  bind its later briefs (macOS native helper out of the store lease, model routing to the operator, GPU measurement-only,
  Rulings 79/102 steps, the units handoff, PR fields). Merged by the Mac leader through conductor-join.
---

# PR #1 — Fable owner review

PR #1 "Plan Windows W-0 through W-5 execution" (origin/win/coord-w0-w5, head 738e1ba6, base 7102e90f) was reviewed by
the Fable owner on the Mac on 2026-10-07 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**. The plan is docs-only
(docs/coordination/windows-w0-w5-execution.md/.html, xmsg pr-ready, audit entries, derived views); `tools/check-docs.py`
passed on the head in a scratch clone on the Mac. Its tracks, budgets, join rule and authority split follow
two-machine.md and pc-kickoff.md; W-4c is consistent with Ruling 79 and W-5 with Ruling 102; the base already carries the
Ruling 121 units key (45c26ee8) and its handoff (xmsg 20261006T222032), and no lease overlaps PreferenceStore.cs.

Conditions bind the briefs, not this merge:

1. `src/CfdWorkbench.Persistence/native/cfd_store.c` leaves the B2 lease — it is the macOS helper, compiled only under
   `IsOSPlatform('OSX')` — and the Windows store path is a new file the PC creates; a handoff per file for
   `ProjectStoreTests.cs`, `tools/verify-application-core.py` and `docs/proof/application-core.md` before dispatch.
2. The gpt-6-astra Owner / GPT-6.1 Sol routing is flagged to the operator, who chose gpt-6-sol over gpt-6-astra on price on
   2026-10-03 (Mac side; recorded in the Mac session's notes, not in the repo); the Owner is dispatched after the operator
   answers.
3. GPU qualification stays measurement-only with no new dependency, one attempt inside W-4a's 60 min, never feeding the
   W-4a equivalence comparison, the L3 run or W-5; any admission is a Mac decision-request.
4. The SPIKE-04 GCI-clause relabel is a Mac ruling on the PC's triplet; the W-5 brief carries Ruling 102's step 1
   (cartesianMesh present, version recorded, decision-request if absent).
5. B1 cites the units handoff; B2 adds PreferenceStore.cs to its lease explicitly if it needs it.
6. Later PR bodies carry pc-kickoff's "Every PR" fields.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/1#issuecomment-6042295093
