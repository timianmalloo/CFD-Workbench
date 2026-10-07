---
id: proof-sfv-model
title: SFV lattice model verification - chordwise panel count and moment reference point
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sfv, lattice, centre-of-pressure, ruling-128]
links:
  - { to: mockup-section-force-vectors, rel: refines }
review-by: 2026-12-31
summary: >-
  Written check, before the build, that the lattice defines the strip centre of pressure (chordwise panels nc greater than 1) and how
  the strip moment, stored about the frame origin, is moved to the strip leading edge. Both hold; the single caveat is nc = 1.
---

# SFV: the lattice model, read before building (Ruling 128)

All claims below were read in the source at this commit (Verified by reading, not by running).

## 1. Chordwise panel count

- `RunSettings.NChord` (`src/CfdWorkbench.Core/RunRecord.cs:50`) is the chordwise count. The default lattice is 64 x 4
  (`Labels.DefaultLattice`, `Labels.cs:247`). The solve builds `NChord` panels per strip (`VortexLattice.cs:125`), each with a bound
  segment at 1/4 and a control point at 3/4 of its own panel (`VortexLattice.cs:96-97, 126-132`).
- **Verdict: nc = 4 > 1 at the default, so a strip has a chordwise force distribution and x_cp is defined by the lattice.**
- The solver accepts `NChord >= 1` (`VortexLattice.cs:85`). At nc = 1 the strip has one bound vortex at c/4, so x_cp is c/4 by construction
  and carries no information. The build treats `NChord < 2` as "x_cp Undefined" (anchor c/4, couple drawn); it never presents it as a result.

## 2. Moment reference point

- `StripForce` / `StripLoad` `Mx, My, Mz` are "about the frame origin, body axes, additive across strips of one run"
  (`VortexLattice.cs:5`, `RunRecord.cs:85`). In the solve: `my[s] += pz*Fx - px*Fz`, where (px, py, pz) is the midpoint of the panel's
  bound segment (`VortexLattice.cs:230-262`). Forces are near-field, rho (V + v) x Gamma l, with V at alpha_op (body axes).
- The reference is the wing frame origin (x = 0, z = 0 of the section frame), not the strip leading edge. The run records the same fact:
  `ReferenceQuantities.MomentDatum = "frame origin"` (`ReferenceQuantities.cs:8`), shown on Properties as "moment datum: frame origin".
- **Move to the strip leading edge:** M_LE,y = My - (rz * Fx - rx * Fz), with r = (x_LE, z_LE) the strip's leading edge at its y-midpoint.
  The y-component of r x F does not involve the strip y, so only x_LE and z_LE are needed.
- x_LE and z_LE are not stored on `StripLoad`. They are the lattice's own mid-strip section frame
  (`SectionSample.Frame.LeadingMeters`, `.ElevationMeters`, interpolated at `ym = (ya + yb)/2`, `VortexLattice.cs:116-118, 701-710`),
  recoverable on read from the same source and the stored span edges, exactly as `MethodRecord.DeriveNormals` does for the strip normals
  (`MethodRecord.cs:100-120`). The build adds one internal getter beside `StripNormals`, `VortexLattice.StripLeadingEdges`, that
  returns those two numbers per strip. No numeric path changes.
- x_cp: with the strip force in section axes (body axes rotated by the strip twist), normal force N and the moment about the leading edge,
  x_cp = -M_LE,y / N, as a fraction of the strip chord. It is Undefined when nc < 2, |Cl_local (lattice)| < 0.05, or x_cp/c is outside [0, 1].

## Result

Both conditions hold. No stop. The only inputs the build adds are the leading-edge getter and the read of `run.Settings.NChord`.
