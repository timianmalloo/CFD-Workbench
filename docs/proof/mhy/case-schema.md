---
id: proof-mhy-case-schema
title: "MHY: how a case records that no mesh was produced, and the case gate"
type: proof-pack
status: active
owner: "@track-mhy"
phase: implementation
tags: [mhy, cases, schema, gate, data-model]
links:
  - { to: rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  cases/spike03-s6-w4.yaml put prose in geometry.source.sha256 and broke schemas/cfd-case.schema.json. The schema now
  has an optional outcome (meshed | no-mesh) with a required reason, the case uses it, and validate-cases.py runs inside
  tools/check-docs.py.
---

# Recording "no mesh was produced" in a case file

## The defect

`cases/spike03-s6-w4.yaml` (S6 route W4) ran Gmsh, which failed with a PLC error and wrote no `.msh`. The case had to say
so. The author wrote the sentence into `geometry.source.sha256`, which the schema requires to match `^[0-9a-f]{64}$`.
`cases/tools/validate-cases.py` found it (red run: `docs/proof/mhy/red-first.md`), but no gate ran that tool, so main
stayed red unnoticed until the Fable review ran it by hand.

## Grain and meaning

One `geometry.source` is exactly one geometry input a run used or tried to produce. `sha256` is the identity of the file
that was produced. A file that does not exist has no identity, so a hash field cannot hold that fact. The fact needs its
own field.

## Decision

Add `geometry.source.outcome`, an optional enum `meshed | no-mesh`. Absent means `meshed`.

- `outcome` absent or `meshed`: `sha256` is required and `reason` is forbidden. This is exactly the old rule, so every
  existing case stays valid (expand only).
- `outcome: no-mesh`: `reason` (non-empty string) is required and `sha256` is forbidden. `path` stays required and names the
  file that was not written.

Implemented with a draft-07 `if/then/else` on the `source` object. The validator self-test plants the invalid shapes
(prose in `sha256`, `no-mesh` without `reason`, `no-mesh` with a `sha256`, `meshed` without `sha256`) and the valid
`no-mesh` shape.

## Alternatives

| Option | Why not |
|---|---|
| Nullable `sha256` (`["string","null"]`) plus a required `reason` when null | Two fields must agree and nothing names the state. A missing hash from a typo looks the same as "no mesh". |
| A fixed sentinel string in `sha256` (for example `"none"`) | Weakens the pattern for every case, so a half-pasted hash would pass. Prose-in-a-hash-field is the defect being fixed. |
| Drop `source` from a failed case | Loses the audit trail: the case file records which generator was tried and how it failed. |
| A separate `run_outcome` block for all failures | Larger change than the defect needs. Solver failures and gate failures are recorded elsewhere (`runs/`). Add it when a second failure kind needs a case-level record. |

The chosen option names the state, keeps the strict hash pattern for every meshed case, and needs one optional enum, one
optional string and three schema lines.

## Case gate

`tools/check-docs.py` (`run_lesson_controls`) runs `cases/tools/validate-cases.py --self-test`, then
`cases/tools/validate-cases.py` over all `cases/*.yaml`. The tool needs `jsonschema` and `pyyaml`, which the docs gate
does not otherwise use. Rule: if both import, run in-process; else if `uv` is on PATH, re-run under
`uv run --with jsonschema --with pyyaml`; else exit 2 with `FAIL validate-cases: missing Python package(s) ...`. It never
skips. Observed on this Mac with PATH=/usr/bin:/bin (no uv): exit 2 and that message.

Measured cost (Mac, uv present, 3 runs each): self-test 0.28-0.29 s, full validation of 39 cases 0.38-0.39 s, about
0.7 s added to `tools/check-docs.py`.

Ring: fast gate (`tools/check-docs.py`, every join). The PC needs uv on PATH or the two packages in its venv; if neither,
the gate FAILS by name and does not pass silently. This is a new dependency for the PC's gate run, to be confirmed by the PC.

## Review

A data-persistence review of the schema change follows at the join.
