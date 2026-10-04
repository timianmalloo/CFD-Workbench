---
id: proof-planform-verbs-old-build
title: "PVC old-build characterization and planted-mutant receipt"
type: proof-pack
status: in-review
owner: "@track-pvc"
phase: implementation
tags: [planform, point-verbs, foildsl, characterization, pvc]
links:
  - { to: design-planform-point-verbs, rel: depends-on }
  - { to: spec-foildsl, rel: depends-on }
review-by: 2027-04-01
summary: >-
  Pre-PVC commit 0cf4e4feae65e844fb6eac0c3825100b6d1c56e5 refuses a 4-point 4.1
  source with DSL-CURVE and refuses point-add, point-remove and curve-rebuild
  envelopes with DOC-REFERENCE. Each file's SHA-256 is unchanged. An alpha
  denominator one span high turns AddPoint_Boehm_ShapeUnchangedWithin1e12Relative red.
review-suggested: []
---

# Old-build characterization

Base: `0cf4e4feae65e844fb6eac0c3825100b6d1c56e5` (`chore(join): join-fluids-adr12-round3`), the branch HEAD before this track's commits. Throwaway worktree `/tmp/pvc-oldbuild`, detached at that commit, `git status --short` empty. The harness was `/tmp/pvc-oldchar`, built against that worktree's `CfdWorkbench.Core`. It was not part of the suite. The worktree was removed after this run.

The fast suite reads this file. It does not rebuild `0cf4e4fe`.

| Case | Input | Result | SHA-256 before | SHA-256 after |
|---|---|---|---|---|
| a | 4-point 4.1 New foil (`FoilSource.NewDefault`, header `foildsl "4.1"`, leading and trailing N=4) | parse `DSL-CURVE` (degree 3, 6–16 points, 8 knots for 4 points); `AuthoringSession.Open` `DSL-CURVE` | `1D8F99E365E522AA7DE4087F2A9C1EE763C8B036C82E7427C6D7784F9CC9535F` | same |
| b | envelope whose accepted row is `point-add` on the 7-point example leading rail (child stays in 6–10) | `Reopen` `DOC-REFERENCE` | `CEE6F27BAD7B0A8AC1980AAC785C111D8461F0F73D5145ECF9D83BD05C97101C` | same |
| c | envelope whose accepted row is `point-remove` (example leading index 2, id `cv-2`) | `Reopen` `DOC-REFERENCE` | `625D1022D1A283487B6FE2F0D7C8BA94EE28A2013E666D4E1E06761B12599B4D` | same |
| d | envelope whose accepted row is `curve-rebuild` (example leading rebuilt to 8) | `Reopen` `DOC-REFERENCE` | `12CC0B53C10868120C227CF91B5C48374A9AD421D8223AAD03BB48D9829063FC` | same |

`unchanged True` on every case. No file was written back.

PASS Reopen_OldBuildNewReceiptKind_RefusedFileUnchanged

# Planted mutant

`FoilSource.InsertKnot`, the Boehm denominator, one span high: `knots[index + degree + 1] - knots[index]` in place of `knots[index + degree] - knots[index]`. Not committed.

On the 4-point New foil trailing rail the combined spans all end on the clamped knot 1, so that edit leaves α unchanged and the check stays green. The check therefore also inserts at η = 0.4 on the saved 10-point trailing rail (`Fixtures/planform-verbs/new-default-10.foil`), whose interior knots are not a single clamp. With the mutant:

```
FAIL AddPoint_Boehm_ShapeUnchangedWithin1e12Relative InvalidOperationException: 0.00013012772392519134
```

The denominator was restored to `knots[index + degree] - knots[index]`. The same check then printed `PASS AddPoint_Boehm_ShapeUnchangedWithin1e12Relative`.
