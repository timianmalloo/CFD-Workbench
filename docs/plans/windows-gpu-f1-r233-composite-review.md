---
id: plan-windows-gpu-f1-r233-composite-review
title: "Bounded independent composite review"
type: plan
status: complete
owner: "@pc-f1-r233-repairs-review"
links:
  - { to: review-windows-gpu-f1-r233-composite, rel: relates-to }
  - { to: proof-f1-composite-helper-reader-r233, rel: depends-on }
review-by: "2026-10-20"
summary: "Exact candidate, independent safety/conservation checks, retained ring, disposition and publication."
---

Goal: decide ACCEPT/HOLD/REJECT for exact b74af1e composite. Done when the
independent evidence and disposition are committed and pushed. Outside scope:
source repair, live/registry/lifecycle action, ring rerun, and live authority.
Tier T1; delegated fan-out zero. Reviewer seat: independent Astra adversary.

Execution graph: A ground exact scope → B production controls and C byte/ring
binding → D disposition → E audit, derive, docs checks and publication. B and C
may overlap only for read-only independent checks; tool width at most four.
Mandatory floors are authority/GUID/absence/unknown-state controls, reader
red-first/disposal, test union, conservation, evidence privacy and safe gates.
One pass through each node; a code veto stops this final cycle without repair.

Actual: focused helper 13/13 and Python 94/94, both PowerShell runtimes, prior
adversarial probes, stronger independent conservation and original ring binding
passed. Safe bundle passed 10 gates with three configured skips. No full ring
was rerun. Corrected the review receipt parser to inspect measured command argv
as a list; no candidate change. Final artifact preserves the cost misses and
separates offline acceptance from live authority. Publication ends the task.
