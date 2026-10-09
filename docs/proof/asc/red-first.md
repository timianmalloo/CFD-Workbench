---
id: proof-asc-red-first
title: ASC red-first - Windows store admission staleness line
type: proof-pack
status: draft
owner: "@trk-asc"
tags: [asc, windows-store, ruling-189]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-08
summary: >-
  The staleness self-test fails against a stub that always reports current and passes on the real check.
---

# ASC red first

Red: self-test against a stub whose status_line always reports current (exit 1):

```
self-test FAIL: altered tree: 0 'WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current\n'
self-test FAIL: altered file: 0 'WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current\n'
self-test FAIL: stale plus claim must fail: 0 'WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current\n'
self-test FAILED
```

Green: `python3 tools/check-windows-admission.py --self-test` prints `self-test OK` (exit 0). On this tree: `WINDOWS-STORE-EVIDENCE admitted (Ruling 189), current`.

Hard fail: detectable cheaply, via the marker `Windows store: PASS (current)` in any docs/*.md; no file carries it today. Self-test covers stale+claim exit 1 and current+claim exit 0.
