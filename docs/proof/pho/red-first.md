---
id: proof-pho-red-first
title: "PHO red-first receipt"
type: proof-pack
status: active
owner: "@track-pho"
phase: implementation
tags: [pii, proof, ruling-213]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  Red-first and self-test receipt for the PII gate matching the running machine's own hostname (Ruling 213).
---
# PHO red-first: the PII gate knows its own host's name (trk-pho, Ruling 213)

Change: `tools/check-proof-pii.py` adds this machine's own names (`socket.gethostname()`, its short form,
`platform.node()`, `COMPUTERNAME`) to the literal set with no setting. Rule id `hostname:own-host`, masked output.
`CFD_PII_HOSTNAMES` still adds names (union). A name under 4 characters or in `GENERIC_HOSTS` is ignored.
The self-test injects a fake name through the `own_hosts` seam and never reads the real one.

## Red (self-test written first, old code)

`python3 tools/check-proof-pii.py --self-test` exit 1:
`TypeError: scan_text() got an unexpected keyword argument 'own_hosts'` (the old code has no own-host path).

## Green (new code)

`python3 tools/check-proof-pii.py --self-test` exit 0: `PROOF-PII self-test ok: 19 offenders caught, 23 clean passed`.
Own-host checks in the self-test: a UTF-8 file and a UTF-16LE file with the fake name fail; a mixed-case hit fails;
short and generic names are ignored; the short form of a dotted name is added; no hit string contains the name.

## Real gate on this Mac

`python3 tools/check-proof-pii.py` exit 0: `PROOF-PII ok: 0 Windows user paths or machine SIDs (1 allowlisted)`.
The Mac's own name is found nowhere in the tree.

## Cost

One extra regex pass per file per own name (2 names typical); the gate stays in the fast ring.
