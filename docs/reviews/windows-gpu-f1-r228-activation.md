---
id: review-windows-gpu-f1-r228-activation
title: "Independent R228 activation rehash"
type: doc
status: accepted
owner: "@pc-f1-r225-attempt1-review"
phase: preflight
tags: [windows, preflight, authorization, independent-review]
links:
  - { to: review-windows-gpu-f1-r228-supervisor, rel: depends-on }
  - { to: proof-win-gpu-f1-r225-attempt1-admission, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  ACCEPT of exact activation 2abcd2a2 under R228. Promoted bytes equal the
  accepted projection, activation lease exceeds 200 seconds and the production
  read-only contract verifier passes. Pre-WSL sampling remains mandatory.
---

# Exact activation verdict

**ACCEPT** activation `2abcd2a26e349ee6a2d58127195158f0b9d5e19e` against
R228 at `7a25122f65893d9b2357bedd668e016688d2514c`. This satisfies the
independent activation rehash condition. It does not replace R228's remaining
runtime conditions or create another attempt authorization.

Goal: verify only the exact ruling, ancestry, promotion and activation sample,
then publish the verdict. Done when those checks and the read-only verifier
pass and this artifact is committed/pushed. No WSL or live lifecycle action.
Tier T1, no delegation. Graph: exact ruling/diff → byte/sample/verifier checks
→ review/audit/derived checks → publication.

Both Git inspection and `r228-activation-probe.py` proved ancestry from the
prior supervisor review `2cb40b67587e3ce5a90b5e8d0a65d9cd367450cb`, candidate
`c9c7af0ea9d6361cda5934884a2ed9db47ada787`, epoch-8 candidate
`df21decf4ed90fcedaecceec449c6edcd87e2179`, source
`b7821e27d75a0a5ffed5ee3f9d36c3bf08e535a8` and the ruling commit.
Parent `46101cf5ef263c6536c2cf483c87e6705047fe45` merges R228. The activation
commit changes exactly two files: status/ruling promotion in the live-path
authorization and the new `leader-activation.json` sample. Source, supervisor,
contract and accepted-projection bytes are unchanged from the reviewed candidate.

The live authorization is byte-identical to the accepted projection, SHA-256
`39217fae100c358c1c98aba5e1827e23cb0e0824d2be9329f98c339a8275dff3`.
Contract SHA-256 remains
`1141ee15dde086991290c508104addf9fe5c32c789dccbdbfdbfbaeb9e49f696`.
The production authorization checker accepted the exact status, ruling, source,
run, branch/session/epoch, scope and hashes. Its bounded R228 section reader
accepted the exact committed ruling. The reviewer also read all five conditions.

# Activation sample and actual verifier

The committed activation JSON records `leader: mac`, `epoch: 8`, `state: live`,
`expires_in: 874.3052248954773`, and ref OID
`1b9762d0cca1a878ab0510b854ca8f1aa9fa304b`. The value exceeds the required
200 seconds. The sample's durable fields match the local Git object at that
OID, and remaining TTL is within its 900-second lease. This validates the
activation observation; it is not the immediately-before-WSL observation.

The unchanged production `contract.py verify` was executed against the actual
clean execution worktree at the exact activation HEAD, using the bound paths,
branch/session/RunId and explicit exclusive-RunId argument. It returned
`INVOCATION-CONTRACT-PASS`. That path validates committed bytes, ruling, packet
and helper source hashes, all seven actual inputs and the live leader. Its
subprocesses are Git and the coordinator's read-only leader query; no WSL is
invoked. The independent activation probe passed in 2.331 seconds. An exclusive
RunId argument is an attestation input, not independent machine-exclusivity proof.

# Remaining R228 conditions

Immediately before the first WSL command, the coordinator must capture a new
`leader who --json` sample proving live Mac epoch 8 with at least 200 seconds
remaining; less refuses. Both samples belong in the receipt. Retain the
supervisor's whole-run UTC/TTL/epoch/ref events, stderr and exit. Maintain PC
exclusivity. Exactly one attempt is authorized; any failure stops and returns
its committed receipt. Later attempts need separate hash-bound rulings. R217's
limits and forbidden operations remain unchanged. No WSL or live action was
performed by this review. Narrow graph/privacy/whitespace results are in the
audit entry; unchanged broad suites were not repeated.
