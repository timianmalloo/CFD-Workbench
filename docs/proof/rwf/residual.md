---
id: proof-rwf-residual
title: "Track RWF - residual risk, surface list and spec check (Ruling 157)"
type: doc
status: draft
owner: "@trk-rwf"
phase: build
tags: [determinism, record-path, ruling-157, residual, surfaces]
links:
  - {to: proof-rwf-red-first, rel: relates-to}
review-by: 2027-04-01
summary: >-
  The DatImport.cs:386 residual risk, the surfaces the two new gate entries reach, and the spec search for a cross-OS byte-identity claim on project files.
---

# Residual risk, surfaces, spec check (Ruling 157)

Label: **Verified** (observed or read on this Mac) unless stated.

## Residual risk: `DatImport.cs:386` (Ruling 157 item 4, left unchanged)

`DatImport.EuclideanResidual` compares the fit with the source at 201 parameters `0.5 * (1 - Math.Cos(Math.PI * i / 200))`. Its result is the residual that `SectionReplace` tests against the limit (`current.Residual <= limit`) and compares between spacings (`own.Residual < best`). It also chooses which spacing is written.

Residual risk (Inferred from the control flow in SectionReplace.cs:54-67, not measured): a residual that sits within about 1e-16 of the limit, or two spacings whose residuals differ by that much, can flip the chosen spacing. The flip changes the written knots and control points by far more than 1e-6, and it can happen on any OS and on any build, because it needs only a last-bit difference in the cosine samples. The identity tolerance of the family checks does not cover it: those checks assert a stable spacing (`Equal` on the curve and value counts), so a flip in a committed input would be a loud FAIL, not a quiet drift. No committed input is known to sit at such a boundary; :386 was not perturbed in this track (the dat-rotation family was perturbed at :524 and :460 only, with no change of shape, and it would FAIL loudly if the shape changed).

## Surfaces the two new FILES entries reach (E7 list, by search)

Search: `grep -rlE "NewDefault|SectionLibrary|FitNaca|ReplaceSource|SectionReplace|BeyondProfileIdentity"` over `src/`, `tests/`, `docs/examples/`, and `git ls-files` for 64-hex file names.

| Reached | By | Committed golden or hash | Result |
|---|---|---|---|
| `FoilSource.NewDefault()` (FitNaca :581, TipClusteredChannel :654) | `WorkbenchController.cs:2527` (the new-project task), `SectionLibrary.cs:17` (certify fixture); test files that mention these names include PointVerbTests, PointCommandTests, PointGestureTests, GroupGestureTests, DimensionTests, ProjectStoreTests, SectionLibraryTests, WingEstimatesTests and seven Analysis and four Desktop test files | the new `new-default-bits.txt` only. `planform-verbs/new-default-10.foil` is read as an input (PointVerbTests, PointCommandTests, Desktop WorkbenchTests), never compared with `NewDefault()` bytes. No `.foil` named by a 64-hex hash is committed. | full Core harness green; ring green except one Desktop timing flake (below) |
| `FoilSource.BeyondProfileIdentity` (:1010) | section edits, Replace | none (a boolean at 1e-6) | green |
| `SectionReplace.Cosine` (:327) and the worst-change scan (:215) | Replace of a record/catalog source | none; the provenance hash in `ReplaceSource.Coordinates` is of the input dat, not the output | green |
| `SectionLibrary` user store | file name is `Identity.Sha256(bytes)` of the user's own bytes at run time | not committed | n/a |

**E7 gap (computational-geometry review G2).** `NewDefault()` also runs an area loop at `FoilSource.cs:563`: `WingEstimates.From(...).AreaSquareMeters`. That calls `WingEstimates.Quadrature` and `GaussLegendre` (`WingEstimates.cs:212-218`), which seeds its Newton iteration for the nodes with `Math.Cos(Math.PI * (i + 0.75) / (order + 0.5))` (`WingEstimates.cs:224`). The area reaches the scale and the rail control points of the New-project default. `WingEstimates.cs` is not in the gate's FILES and the call is unchanged here (no code change was ruled). Newton converges to the node to full precision, so a last-bit difference in the seed should not move the node; that is Inferred, not measured. The detector is `NewDefault_ControlPointDoubles_BitGolden`, which on the PC ring compares the whole default with the Mac bits: a seed that changed a node would show there.

**G1 note.** `Math.Pow(x, 2)` is not exact (2,647 of 2,000,000 differ by 1 ulp), so an earlier reading of it as exact is withdrawn. The squared-distance sites on the record path (`DatImport.cs:460`, `SectionEdits.cs:402-403`, `PointModel.cs:193`) now use `d*d`. `Geometry.cs:495,537,538` and `ChannelEdits.cs:514` still call `Math.Pow`; they are validation or display sites (class c, classified by reading `Geometry.cs:494-495` as a `<= tau` boolean; `:537-538` and `ChannelEdits.cs:514` not read in detail) and off the gate.

New-project bytes change once: the profile ordinates (8 of 36 values per side moved on this Mac). Saved projects are not rewritten; only a New project opened after this change differs.

## Desktop spelling of the same helper (not owned, not edited)

`src/CfdWorkbench.Desktop/PropertiesView.cs:655` and `:1210` call `double.SinCosPi(angleDegrees / 180)` directly. It is the same expression as `PlacementRule.SinCosDegrees`; Desktop cannot call the Core `internal` helper. Folding them needs either `InternalsVisibleTo` for Desktop or a public Core helper, and a file outside this track's list. Left as a follow-up.

## Spec check (Ruling 157 item 5): no claim found, nothing edited

Searched `docs/specs/` and `docs/design/` (and `docs/adr/`) for byte-identical, bit-identical, same bytes, byte-for-byte, deterministic or reproducible near project, `.foil`, record, OS, Windows, macOS, platform. No text claims that project files are byte-identical across OS. Nearest texts, none of which is that claim:

- `docs/specs/foildsl.md:267` says the opposite: "Trigonometric evaluation must meet the identity oracle, not promise bit-identical libm".
- `docs/specs/foildsl.md:495` (DSL-10): "Given save/reopen on either OS, then accepted source bytes, dependency hashes, recovery draft and historical runs survive". This is survival of the saved bytes across save and reopen on one OS, not regeneration to the same bytes on two.
- `docs/design/cross-profile-abscissa.md:479` already restates a test as a tolerance because "bit equality across platforms" is not promised.

Nothing for the operator to decide on spec text.
