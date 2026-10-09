---
id: proof-rrf-red-first
title: "RRF: red-first record for the readiness rule match"
type: proof-pack
status: draft
owner: "@trk-rrf"
phase: implementation
tags: [readiness, run-readiness, red-first]
links:
  - { to: proof-wdf-red-first, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  `rule_for` in tools/run-readiness.py matched a rule against any argument; it now matches the executed script only.
  The new self-test case is red on the old code and green on the new.
---

# RRF red-first record

Check: `tools/run-readiness.py --self-test`, case "a ruled script named only as an argument". It runs
`python3 tools/other.py --skip verify-windows-store.py`, where `other.py` exits 4. No rule applies, so the step is `fail`
and the ring exits 1.

- Red (old `rule_for`, any argument): `self-test FAIL: a ruled script named only as an argument got its rule: ring 0, status not-assessed`, then `self-test FAILED`, exit 1.
- Green (rule for the executed script only): `self-test OK`, exit 0, with every earlier VWR case still passing.

Real readiness lines are quoted in the track Return (the run needs a clean, committed tree).
