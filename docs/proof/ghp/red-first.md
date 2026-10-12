---
id: proof-ghp-red-first
title: GHP red-first - the PII guard covers text posted to GitHub
type: proof-pack
status: draft
owner: "@trk-ghp"
tags: [ghp, pii, github, proof-pii-outside-the-tree]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-12
summary: >-
  A comment body with a Windows user path, a SID or an own-host name is refused by the posting wrapper and flagged by the GitHub-text scan; both self-tests are red with the scanner disabled and green with it.
---
# GHP red-first: PII-OUTSIDE-THE-TREE (trk-ghp, 2026-10-12)

Behaviours: (1) `tools/post-github-comment.py` refuses a body with a hit (exit 3, rule id and mask only, poster never
called) and posts a clean one; (2) `tools/scan-github-text.py` reports `<kind> <id> on #<n>: <rule> <count>` for fixture
pages, never the text, and exits 1 on a hit. Offenders are built at run time from fragments; `gh` is never called in a self-test.

## Red
Before the tools existed, `python3 tools/post-github-comment.py --self-test` and `... scan-github-text.py --self-test`
could not run (no such file). To make the red meaningful, each tool was also run as a scratch copy with its scanner call
replaced by an empty result (mutation):

- post copy: exit 1; 9 SELF-TEST FAIL lines (offenders not refused with exit 3, poster called for an offender, clean
  dry-run/post checks).
- scan copy: exit 1; expected four rule lines, got none; "hits must exit 1".

## Green
- `python3 tools/post-github-comment.py --self-test`: exit 0, "4 offenders refused (exit 3, text never printed)", 0.2 s.
- `python3 tools/scan-github-text.py --self-test`: exit 0, "6 fixture texts, 3 flagged, counts only", 0.2 s.
- Live, read-only: `python3 tools/scan-github-text.py` exit 0, "scanned 152 texts; flagged 0" (the run had no
  `CFD_PII_HOSTNAMES` list; the own-host rule is always on).

Ring: both self-tests are fast ring in `tools/check-docs.py` (~0.2 s each). The live scan is on demand.
