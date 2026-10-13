---
id: review-windows-gpu-f1-r228-supervisor
title: "Independent R228 renewal-supervisor supplement"
type: doc
status: accepted
owner: "@pc-f1-r225-attempt1-review"
phase: preflight
tags: [windows, preflight, authorization, independent-review]
links:
  - { to: review-windows-gpu-f1-r225-attempt1-epoch8, rel: depends-on }
  - { to: proof-win-gpu-f1-r225-attempt1-admission, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  ACCEPT of c9c7af0e with the committed epoch-8 renewal supervisor and unchanged
  payload hashes. Activation/pre-WSL lease samples and whole-run supervisor
  evidence remain mandatory runtime conditions before live use.
---

# Exact verdict

**ACCEPT — supplemental offline readiness only.** Candidate
`c9c7af0ea9d6361cda5934884a2ed9db47ada787` contains ancestors
`fa59b125d5011ee958999b1e1fe8ad528ce627ad` and
`df21decf4ed90fcedaecceec449c6edcd87e2179`; both ancestry checks passed.
The review branch merges this candidate while preserving the prior independent
epoch-8 review. Goal: verify the added supervisor and two Mac conditions, then
publish ACCEPT/VETO. No WSL, live renewal or lifecycle action. Tier T1, no
delegation. Graph: source/ancestry → inert probes/hashes → gates → publication.

The candidate adds `attempt-1/leader-renew.ps1` and eight README lines.
Supervisor SHA-256:
`ae05660c6206cbc5fb178c77222a366f12d7278213b1dc94042b0df7ffa01f3a`.
Source anchor remains `b7821e27d75a0a5ffed5ee3f9d36c3bf08e535a8`. The packet,
recovery source, historical evidence and three payload files have empty diffs
from `df21decf`. The independent payload probe again verified sixteen packet
files, one external helper and all seven actual inputs.

| Unchanged binding | SHA-256 |
| --- | --- |
| Contract | `1141ee15dde086991290c508104addf9fe5c32c789dccbdbfdbfbaeb9e49f696` |
| Proposed authorization | `a274d4d89eb789ffae57009b4197c08ea0dd632c95bfa576f7c446a30749e5b6` |
| Accepted projection | `39217fae100c358c1c98aba5e1827e23cb0e0824d2be9329f98c339a8275dff3` |

# Verified supervisor

Defaults are expected epoch 8 and interval 90 seconds. Each cycle validates a
successful JSON read with exact `live` state, `mac` leader and expected epoch,
renews, then repeats that validation on readback. Command errors and binding
changes write refusal events and exit 1. Valid renewal logs UTC, epoch, floored
remaining TTL and ref OID, then sleeps. A stop-file ends the loop. Parameters
are configurable: the coordinator must invoke the reviewed epoch-8/90-second
values. The coordinator API was inspected: renewal preserves epoch, uses the
existing TTL, refuses expired state and writes through its compare-and-swap.
A read-only sample returned Mac epoch 8 live with 898 seconds remaining. That
single observation does not prove future continuity.

The independent `r225-leader-renew-probe.ps1` parses and runs the production
script with inert command/log/sleep/stop boundaries. Nine cases passed on both
PowerShell 5.1 and 7: valid renewal; changed leader, epoch or state; initial
read, renewal or post-read errors; post-renew binding change; malformed JSON.
Initial invalidity never calls renew. The valid case asserts the event fields
and default interval. All negatives exit 1. Ring: offline-admission; measured
about 3.1 seconds on 5.1 and 10 seconds on 7. No real renewal or WSL was invoked.

Two initial review-harness failures were corrected: a dot-sourced subject's
explicit exit needed propagation, and PS5 native stderr needed capture before
checking the exit. The final nine-case cross-runtime matrix prevents both
evidence-boundary errors; the existing class register records this recurrence.

# Mac conditions and operational limits

The coordinator relayed Mac/Fable's conditions; this review does not author a
ruling. R228 and activation must require both:

1. Record `leader who --json` showing live Mac epoch 8 with **at least 200
   seconds remaining** at activation and again immediately before the first WSL
   command. A review-time sample satisfies neither checkpoint.
2. Retain supervisor events for the **whole live run** in its receipt. Retain
   process exit and stderr too: malformed JSON or I/O failure exits fail-closed
   before an explicit refusal event can be written.

The README assigns these to the coordinator. The supervisor does not itself
implement the two activation checkpoints or control the preflight process.
Its invocation, health and retained evidence must be checked at runtime.
Authorization remains proposed. Exact R228 and independent activation rehash
are still required. This ACCEPT grants no live authority. No WSL or live action
was performed by this review; closing documentation/privacy/graph evidence is
recorded in the accompanying audit entry.
