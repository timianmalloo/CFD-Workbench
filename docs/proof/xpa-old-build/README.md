---
id: proof-xpa-old-build
title: "Compatible Fit old-build receipt: pre-CF build refuses cfdw-cv/3 project with DOC-VERSION"
type: proof-pack
status: active
owner: "@timianmalloo"
phase: implementation
tags: [xpa, compatible-fit, cfdw-cv-3, old-build, doc-version, ruling-73, ruling-269]
links:
  - { to: design-compatible-fit, rel: implements }
  - { to: design-cross-profile-abscissa, rel: implements }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: rulings, rel: implements }
review-by: "2027-03-19"
summary: >-
  Reproducible receipt for test 24 (cross-profile-abscissa.md §3.6, §10): a genuine cfdw-cv/3
  mixed-spacing project written by the CF build is inspected by the pre-CF build at commit
  df566d56, which refuses it with DOC-VERSION (CLI exit 3) and leaves the file bytes unchanged.
---

# Compatible Fit old-build receipt (test 24)

**Question (`docs/design/cross-profile-abscissa.md` §3.6, DR-XPA-2, Ruling 73, Ruling 269).**
When a mixed-spacing foil saved under evaluator id `cfdw-cv/3` is opened by a pre-CF
(`cfdw-cv/2`-only) build, does that build refuse it cleanly with `DOC-VERSION` and leave the
project file bytes untouched?

**Method.** `bash docs/proof/xpa-old-build/reproduce.sh df566d56`:

1. Copies `docs/proof/xpa-old-build/mixed-spacing.cfdw.json` (8,524 bytes, produced by the CF
   build from the Example foil after `MakeUnique` on Root and moving Root point 4 `x` by
   `+0.005` chord, then `FinishSection` and `SaveImage`) into a temporary directory and records
   its SHA-256.
2. Runs the current CF CLI (`inspect <file> --json`) and verifies `"evaluator": "cfdw-cv/3"`
   and `"status": "Certified"` (`GEOMETRY-CERTIFIED`).
3. Checks out the pre-CF commit `df566d56` into a scratch detached git worktree, builds its
   unmodified `CfdWorkbench.Cli` in `Release`, and runs `inspect <file> --json` against the
   same file.
4. Checks the exit code (`3`), JSON payload (`{"code": "DOC-VERSION"}`), and post-run SHA-256
   equality, then removes the temporary worktree and directory.

## Observed result (2026-11-19)

| Field | Measured value |
|---|---|
| Pre-CF commit | `df566d56` (`cfdw-cv/2`-only reader) |
| Fixture file | `docs/proof/xpa-old-build/mixed-spacing.cfdw.json` (8,524 bytes) |
| SHA-256 before open | `f8b99f6d29ad214bc529ac7c7ae99e097570da4aec09b48d2032d3224f239940` |
| Current CF CLI | exit `0`, `"evaluator": "cfdw-cv/3"`, `"status": "Certified"`, `"code": "GEOMETRY-CERTIFIED"` |
| Pre-CF CLI (`df566d56`) | exit `3`, stdout `{"code": "DOC-VERSION"}` |
| SHA-256 after open | `f8b99f6d29ad214bc529ac7c7ae99e097570da4aec09b48d2032d3224f239940` (byte-identical) |
| Verdict | **PASS** |
