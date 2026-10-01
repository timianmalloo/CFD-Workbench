---
id: proof-m12b2-golden
title: "PL0 golden master — planted mutant receipt"
type: proof-pack
status: in-review
owner: "@track-pl0"
phase: implementation
tags: [m1.2b2, placement, golden-master, pl0]
links:
  - { to: design-m12b2-3d-elevations, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
review-by: 2026-10-28
summary: >-
  The certificate golden master was captured at 3b396e5, before the placement
  refactor. Reassociating the placed-X product turned the golden test red.
  Reordering the blend sum did not, because those interval additions commute.
  The golden master pins outputs and refusals, not the operation tree.
review-suggested: []
---

# PL0 golden master receipt

Captured at base `3b396e59c249726f64a4cfac52ef5951f2cb8ded`, before any edit to `Geometry.cs`. The bytes are `docs/proof/m12b2-golden/certificate-bits.json`. The check is `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged`.

The fixture set is five foils, each one certified:

- `example`: the 120 mm chord example foil.
- `blended-dihedral`: a two-profile blend whose profiles have the same unit-thickness shape, with dihedral.
- `blended-peaks`: a two-profile blend whose thickness peaks differ. Profile A peaks at 0.214 at x = 0.5. Profile B peaks at 0.135 at x = 0.38.
- `chord-2m`: a 2000 mm root and tip chord (`trailing` ordinate 2000 mm), with dihedral and twist.
- `c0-peak`: a degree-5 profile whose interior knot has multiplicity 5, a C⁰ thickness peak.

The `.foil` files in `tests/CfdWorkbench.Core.Tests/Fixtures/m12b2/` are the capture inputs. `PlacementRule_FoilFixtures_MatchGoldenSources` pins each file to the source text stored in the JSON. The refusing set is the proof budget, cancellation, the Taylor domain (−90°), and the whole-domain 10 nm budget (`1e20` mm trailing edge).

Repair cycle 1 replaced the `chord-2m` entry (its chord was 120 mm) and added `blended-peaks`. A throwaway worktree at `3b396e5` (`git rev-parse HEAD` printed that hash, `git diff -- src` empty) ran a capture harness added to its test project. The same harness reproduced the stored `example`, `blended-dihedral` and `c0-peak` entries exactly, which is the check that it matches the first capture. The harness and worktree were deleted after the run.

## Planted mutant that turned the check red

In `Geometry.PointAt`, the placed-X product

`abscissa * cos + z * sin`

was reassociated to

`abscissa * (cos + z * sin)`.

`CFD_TEST_ONLY=PlacementRule_CertificateGoldenMaster`, Release, exit 1:

```text
FAIL PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged InvalidOperationException: example eta 0.25 x 0.5 upper True port False X lower changed. Certificate bits changed. Review the hand-kept models QueryFeasibility, PlacementWidth and BlendPlacementWidth.
```

The edit was reverted. `git diff` on `Geometry.cs` was empty afterwards.

## Blend-sum reorder, measured

`complement * a + share * b` was reordered to `share * b + complement * a` for both the camber sum and the unit-thickness sum in `SectionExact`. The same check passed. Endpoint-wise rational interval addition commutes, so that reorder is bit-identical on this fixture set. It was reverted as well.

## What the golden master pins

The golden master pins the outputs (outward bits, witnesses) and the refusals. It does not pin the operation tree. The PL0 review probes found four more math-preserving tree changes that kept it green, because outward rounding absorbs them. The planted mutant above was red only because it changed the mathematics. A structural trace pin is required before the first change to design §6 and before VW1: an `IPlacementScalar<Trace>` instantiation of the same rule whose operation string is goldened. It is not built yet; design §13 OI-11 tracks it.
