---
id: proof-pii-red-first
title: "PROOF-PII red-first receipt"
type: proof-pack
status: active
owner: "@track-pii"
phase: implementation
tags: [pii, proof, ruling-145]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red-first, self-test, cost and hash-safety receipt for the PROOF-PII guard and the Windows user-name scrub (Ruling 145 (5)).
---

# trk-pii: red first

- Red: `python3 tools/check-proof-pii.py` on the pre-scrub tree exited 1 and named 29 files (`red-run.txt`; the name is redacted to `<name>` there).
- Green: after the scrub the same command exits 0 (`PROOF-PII ok: 0 Windows user paths or machine SIDs (1 allowlisted)`).
- Self-test: `python3 tools/check-proof-pii.py --self-test` catches 7 offenders (backslash, JSON-escaped, forward slash, WSL, lowercase, SID with and without RID) and passes 6 clean texts, including a macOS home path.
- Cost: about 1.0 s (3 runs, 1.03 / 1.02 / 1.05 s over 2807 tracked files); ring: fast, run by `tools/check-docs.py`.
- Registers (`docs/audit/*.jsonl`, `xmsg.jsonl`, `rulings.md`): 0 Windows-form hits, so no register allowlist entry. The macOS name appears there and is out of scope.
- Hash safety: `docs/proof/win-routes/manifest.json` recorded 15 of the changed files; those rows were refreshed in the same commit and `hash-manifest.py --check` passes (160 rows). `docs/docs-index.js` hashes are derived and regenerated. `fixture-observed-sha256.txt` records the sha256 of the fixture `.deb`, not of itself, so only its path column changed.
- Allowlisted, unchanged: `docs/ai-forward-pack/scripts/verify-no-machine-paths.py` (pack self-test fixture with the dummy name `x`; pack-owned).
