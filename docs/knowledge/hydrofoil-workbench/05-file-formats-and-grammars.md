---
id: kb-hw-file-formats-and-grammars
title: "File formats and grammars for curves, surfaces, meshes and CFD data"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, file-formats, step, dat, mesh, cfd-data, json-schema, provenance, grammars]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes what the workbench must read, write and design for its own native document: the
  exact Selig/Lednicer .dat layouts and a fail-closed parser contract, the station grammars of AVL,
  XFLR5, OpenVSP, MachUpX and Shape3d, the one STEP entity (B_SPLINE_SURFACE_WITH_KNOTS in a shell)
  a CAM round trip needs, the mesh and CFD result formats with their licences, and an evidence-based
  design for a versioned, JCS-hashed, Git-friendly .cfdw.json with binary sidecars. Main implication:
  store the explicit definition only, hash a canonical form, embed nothing large, and gate every
  export on an open-and-measure proof rather than on writing bytes.
---

# File formats and grammars for curves, surfaces, meshes and CFD data

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. 2D airfoil coordinate formats (Selig, Lednicer, XFOIL/XFLR5, OpenVSP, CST, AeroSandbox) and what a robust parser must detect and reject.
2. Wing/foil geometry grammars (AVL, XFLR5, OpenVSP, MachUpX, AeroSandbox, Shape3d, the CFD-Bench station grammar) — what each stores vs derives; comparison table.
3. Exact-surface CAD interchange (STEP, IGES, 3DM, Parasolid, SAT, glTF, USD, OBJ, DXF) and what a CNC/mold-CAM round trip actually requires.
4. Mesh formats for printing and CFD (STL, 3MF, OBJ, PLY, Gmsh, SU2, CGNS, OpenFOAM polyMesh, Exodus, VTK, Fluent, Tecplot, EnSight) with reader/writer licences.
5. CFD results/data formats (OpenFOAM function objects, SU2 outputs, ParaView readers, HDF5, Parquet/Arrow, Zarr/NetCDF) and what a run document references vs embeds.
6. Native project format design (`.cfdw.json`): JSON Schema 2020-12, versioning/migration, JCS hashing, units, append-only history, Git-friendliness, binary sidecars; critique of the existing sketch.
7. Grammars/DSLs for parametric geometry — when a DSL beats a schema.
8. Licence and platform notes for every reader/writer library a C#/.NET or Rust implementation could use.

Labels: **Verified** = a primary source opened this session (or a result executed this session); **Inferred** = reasoned from Verified sources; **Flagged** = recall, tertiary, dated or contested. Fast-moving facts carry their date.

## Headline findings

1. The UIUC database ships **two layouts under one extension**: Selig (name line, then TE→upper→LE→lower→TE) and Lednicer (name line, then a `NU. NL.` count line, blank, upper LE→TE, blank, lower LE→TE). `coord/e817.dat` is Lednicer, `coord_seligFmt/e817.dat` is Selig, same airfoil. A parser that reads a Lednicer file as Selig gets a *plausible* wrong shape — the Phase-0 defect. — *(Verified by download, [S3]; Selig ordering Verified, [S1])*
2. UIUC files may contain `#` comment lines anywhere, and Fortran readers choke on names starting with `T`/`F`; the database states no explicit licence — redistribution rights of any bundled coordinate set must be established per file. — *(Verified [S1][S2]; licence absence Flagged)*
3. AeroSandbox (MIT) parses `.dat` by regex-splitting every line into exactly-two-float pairs and **does not detect Lednicer**; its writer emits `%f %f` (6 decimals, ≈1 µm at 1 m chord, 0.1 µm at 0.1 m). Do not adopt it as the reference parser. — *(Verified, [S4][S5])*
4. AVL's `.avl` is the canonical minimal station grammar (`SECTION Xle Yle Zle Chord Ainc`, `AFILE`, `NACA`, `CLAF`, `YDUPLICATE`), interpolates chord/incidence **linearly** between sections, and `Ainc` modifies only the camber-line boundary condition — it does **not** rotate the section geometry. An AVL export is therefore lossy in twist semantics and loft rule by construction. — *(Verified, [S6])*
5. MachUpX stores spanwise distributions as **functions of span fraction** (float, array, CSV or function) plus an airfoil list over span — the closest published analogue to the workbench's five distribution curves; MIT. — *(Verified, [S24][S31])*
6. STEP's `B_SPLINE_SURFACE_WITH_KNOTS` (subtype of `B_SPLINE_SURFACE`; rational form via the `RATIONAL_B_SPLINE_SURFACE` complex entity) is the single entity the export needs; OpenVSP writes it via STEPcode (BSD-3) and since 3.21.0 offers **trimmed** surfaces forming a "single watertight BREP solid" versus **untrimmed** surfaces that "will not have slivers, gaps and holes". CAM systems want a face/shell/solid, not a bare surface. — *(Verified [S8][S10]; OpenVSP trimmed/untrimmed Verified from release notes [S34]; "CAM wants a shell" Inferred)*
7. rhino3dm (MIT, .NET NuGet, v8.17) reads/writes **3DM only** — it is a NURBS maths library, not an interchange path; USD's `UsdGeomNurbsPatch` and OBJ `cstype bspline` carry NURBS but no mainstream CAM imports either. — *(Verified, [S9][S25][S26])*
8. Mesh formats carry **no units** except 3MF (`unit` attribute: micron/millimeter/centimeter/inch/foot/meter, default millimeter) and CGNS (`DimensionalUnits` in the SIDS). STL, OBJ, Gmsh MSH ("there are no units in Gmsh"), SU2 and VTK are unitless: the exporter must state the unit and the reader must ask. — *(Verified, [S11][S12][S13][S14])*
9. SU2's native mesh is a small ASCII grammar (`NDIME/NPOIN/NELEM/NMARK/MARKER_TAG/MARKER_ELEMS`, VTK element codes 3/5/9/10/12/13/14) and SU2 reads CGNS keeping boundary names as marker tags; OpenFOAM's `polyMesh` is a **directory** (points/faces/owner/neighbour/boundary) with an owner-normal ordering rule. Both are writable without any library. — *(Verified, [S13][S15])*
10. OpenFOAM-12 `forces` writes `<timeDir>/forces.dat` (and `forces_bin.dat`); SU2 defaults to `OUTPUT_FILES= (RESTART, PARAVIEW, SURFACE_PARAVIEW)` with CSV/Tecplot/STL variants and a history file. Both are line-oriented text or VTK; a run document should **reference** them by path + hash, never embed them. — *(Verified, [S17][S32])*
11. HDF5 (BSD-3-style), PureHDF (MIT, pure C#, read+write, chunking+filters), Parquet.Net (MIT, .NET 8/10), Apache Arrow/Parquet Rust crates (Apache-2.0, v60.0.0 on 2026-09-15), hdf5-metno (MIT OR Apache-2.0, 0.15.0 on 2026-09-18), stl_io (MIT, 0.11.0 on 2026-03-15), ruststep (Apache-2.0, 0.4.0, last release 2024-09-20) all satisfy COMMIT-02. Gmsh is GPL, meshio is MIT but last pushed 2024-07-23. — *(Verified, [S18][S19][S20][S30][S31])*
12. .NET (Core 3.0+) `Double.ToString()` and `Utf8JsonWriter` emit the **shortest round-trippable** digits and the probe confirmed **bit-exact** round trip for every tested value; but the output is *not* RFC 8785 canonical (`1E-07` vs JCS `1e-7`; `-0` vs JCS `0`), and NaN/Infinity cannot be written as JSON numbers. A content hash must therefore run over a **separate JCS canonicalisation**, not over the saved bytes. — *(Verified by execution, [S36][S21][S22])*
13. .NET 10 file-based apps (`dotnet run app.cs`) disable reflection-based `JsonSerializer` by default; a trimmed/AOT desktop build must use source-generated `JsonSerializerContext`. — *(Verified by execution, [S36])*
14. KiCad's S-expression rewrite is the best-documented precedent for a Git-friendly engineering file: human readability as a stated goal, one unit (mm), **no exponential floats**, a `version YYYYMMDD` token, a `generator` token and UUIDs for identity. — *(Verified, [S23])*
15. RFC 8785 (JCS) is Informational, sorts properties by UTF-16 code units, serialises numbers per ECMAScript `Number.prototype.toString`, forbids whitespace, and recommends strings for numbers beyond IEEE-754 double. It is the right canonical form for ANA-09 hashes. — *(Verified, [S21])*

## State of the art

### 1. 2D airfoil coordinate formats

**Selig format.** The UIUC page: coordinates "order starts from upper surface trailing edge, then wraps around the leading edge to the lower surface trailing edge"; the first line is the name; files may contain `#` comment lines "anywhere", which XFOIL ignores and "software applications reading these airfoil .dat files should be configured to handle" [S1]. Observed `coord_seligFmt/e817.dat` [S3]:

```
EPPLER 817 HYDROFOIL AIRFOIL
1.0000000 0.0000000
0.9966800 0.0008500
0.9872300 0.0035200
```

**Lednicer format.** Observed `coord/e817.dat` [S3]:

```
EPPLER 817 HYDROFOIL AIRFOIL
       35.       34.

 0.0000100 -.0000500
 0.0001600 0.0008700
 0.0009300 0.0024200
```

Verified from the file: a name line, a count line of two **floats** (`35.` `34.` = upper and lower point counts), a blank line, then the upper surface **from LE to TE** (x increasing from 0). The lower surface follows after another blank line, LE→TE — *(Inferred from the count line and the format's documented convention; the second block was not printed in this session — Flagged until the parser fixture confirms it)*. Note the two blocks may have **different counts** (35 vs 34) and the leading-edge point may appear in both blocks. The first coordinate `0.00001 -0.00005` shows Lednicer files are not guaranteed to start exactly at (0,0).

**Detection rule (Inferred from the two layouts, [S1][S3]).** After the name line, read the first numeric line: if both values are ≥ 1.5 *and* integral-valued (`35.` `34.`) it is a Lednicer count line; a Selig file's first numeric line is a coordinate with 0 ≤ x ≤ ~1.05. A single-block file whose x runs 1→0→1 is Selig; two blocks each running 0→1 separated by a blank line is Lednicer. Files where x runs 0→1 once are neither (a half-profile or a corrupted file) and must be **rejected**, not guessed. The Phase-0 defect (Lednicer read as Selig) produces a self-crossing polygon, so the self-intersection test in CAT-02 is the last line of defence, not the first.

**Robust parser contract (Inferred, grounded in [S1][S3][S4][S5]):**
- Tokenise on whitespace, comma, semicolon, tab (AeroSandbox's regex `[;|,|\s|\t]` [S5]); accept `.5`, `-.00005`, `1E-3`; reject any non-finite value; reject lines with ≠2 numeric tokens except the name line and a Lednicer count line; accept `#` comments anywhere [S1].
- Strip a UTF-8 BOM; accept CRLF (UIUC states "DOS EOF format (PC)" [S1]) and LF.
- Detect layout per the rule above; report **detected format**, point counts, whether the LE point is duplicated, whether the TE is open (`y_upper(1) ≠ y_lower(1)`) and by how much (TE thickness in chord units), and whether the profile is closed.
- Normalise: chord to 1 by translating the LE (the point of minimum distance from the TE midpoint, not simply `min(x)`) to (0,0) and rotating the TE to (1,0) — AeroSandbox `normalize()` does "translation and rotation" [S4]. Record the transform. Keep the **original** coordinates and hash them (CAT-02 requires "original points retained").
- Reject: self-crossing polylines, fewer than ~20 points per surface (a heuristic; state it), x outside [−0.05, 1.05] before normalisation, duplicate consecutive points, non-monotone x within a Lednicer block.
- Names beginning with `T`/`F` are a Fortran pitfall on **export** to XFOIL/AVL: prefix `_` as UIUC advises [S1].

**Other 2D encodings.**
- *XFOIL / XFLR5*: read the Selig layout; XFOIL also reads a "plain" file with no name line (Flagged, recall). XFLR5 reads the same `.dat` files (Inferred from UIUC's link to XFLR5 [S2]).
- *OpenVSP*: exports Selig `.dat` and a Bezier `.bz` file ("the first two lines list the file name followed by the total number of Bezier segments", each line one segment with order, global t-values and control points), plus a CSV metadata file with LE/TE coordinates, chord and section index [S7]. An internal `.af` format was **not** found on the export page — Flagged; the pack's earlier "OpenVSP airfoil file (`.af`)" mention is unconfirmed.
- *CST coefficient files*: no standard container exists; AeroSandbox's `get_kulfan_coordinates(lower_weights, upper_weights, leading_edge_weight, TE_thickness, n_points_per_side, N1=0.5, N2=1.0)` [S5] is a de-facto parameter set (upper/lower Bernstein weights, LE modifier, TE thickness, class exponents). A native profile revision that stores CST must store **all** of these plus the number of points and the sampling law used to evaluate, or the evaluated coordinates are not reproducible.
- *Airfoil Tools exports*: Selig `.dat` (Flagged, recall — not opened this session).
- *AeroSandbox `write_dat`*: `%f %f` = 6 decimal places [S4]. At chord 1 that is 1 µm resolution — exactly the DOC-02 tolerance with no margin; at a 0.10 m chord it is 0.1 µm. Our own DAT export should write **shortest round-trip** or at least 9 significant digits and state the resolution in the EXP-02 dialog.

### 2. Wing / foil geometry grammars

**AVL `.avl`** [S6]. Header: title, Mach, `iYsym iZsym Zsym`, `Sref Cref Bref`, `Xref Yref Zref`, optional `CDp`. Frame: "X = downstream, Y = out right wing, Z = up" (matches the spec's +x aft, +y starboard, +z up). `SURFACE name / Nchord Cspace [Nspan Sspace]`, `YDUPLICATE Ydupl`, `SCALE`, `TRANSLATE`, `ANGLE dAinc`, then `SECTION Xle Yle Zle Chord Ainc [Nspan Sspace]` with `NACA xxxx [X1 X2]`, `AIRFOIL [X1 X2]` + inline pairs, or `AFILE [X1 X2] filename` ("x/c, y/c coordinates run from TE, to LE, back to the TE again in either direction"), `CLAF CLaf` (dcl/dα = 2π·CLaf), `CDCL`, `CONTROL`. Two semantics matter: "the local chord and incidence are linearly interpolated between defining sections" and "Ainc is used only to modify the flow tangency boundary condition on the airfoil camber line, and does not rotate the geometry of the airfoil section itself." AVL therefore stores stations only, derives everything, has no loft rule but linear, and its twist is an aerodynamic not geometric rotation. Export from the workbench: sample authored stations **plus** enough inspection slices that linear interpolation of chord/`Zle`/`Xle` stays within a stated tolerance of the evaluated distribution (Inferred).

**XFLR5** (GPL — process/export only). Plane/wing definitions export/import as XML ("Plane/Sections/Section/Chord", `Left_Side_FoilName`/`Right_Side_FoilName`) and, since a developer thread, as a plain-text `.xwimp` with one line per segment: `y chord offset dihedral twist nx ny x-dist y-dist right-foil left-foil` [S28 — tertiary: SourceForge threads and a third-party generator; **Flagged** until an exported XML is opened]. The `.wpa` project file is XFLR5's binary/project container (Flagged, recall). Note that XFLR5 describes dihedral and twist **per panel between sections** and both sides' foils per section, and that "twist" applies about the section's quarter chord or LE depending on version (Flagged).

**OpenVSP `.vsp3`** is XML (Flagged, recall — not opened; the airfoil-export wiki page [S7] and the STEP release notes [S34] were). Its wing component is built from "stacked, parallel sections" with XSec parameters (span, root/tip chord, sweep, sweep location, twist, twist location, dihedral per section) and blending controls (already Verified in the repo's parametric-geometry source, §"3D grammar"). Twist has an explicit **twist location** (fraction of chord) — a stored twist axis, unlike AVL's implicit camber-line BC. Licence: NASA Open Source Agreement 1.3 (Flagged, recall; GitHub reports `NOASSERTION` [S31]) — permissive-compatible for **file interchange**, but do not link.

**MachUpX aircraft JSON** [S24]. `"wings": {id: {...}}` with `semispan`, `dihedral`, `sweep`, `twist` and `chord` each given as "a float, array (span fraction vs. angle), CSV file path, or function", `chord` also `["elliptic", root_chord]`, `side` right/left/both, `connect_to` (ID, location root/tip, dx/dy/dz/y_offset), `airfoil` as a name or an array mapping span fractions to airfoil names, and `grid` (N, distribution linear/cosine_cluster/explicit). Airfoils are separate JSON objects typed `linear` / `database` / `poly_fit`, with `outline_points` (TE→TE) or `NACA` used **only for 3D export, not aerodynamics**. Twist "can include step changes by specifying the same span location twice". MIT [S31]. This is the closest published analogue to the workbench's distribution curves: distributions are functions over span fraction, stations are implicit, symmetry is a `side` flag, and the section polar is decoupled from the section outline.

**AeroSandbox `Wing`/`WingXSec`** (MIT [S31]): Python objects; `WingXSec(xyz_le, chord, twist, airfoil)` per section with `symmetric` on the wing — a station grammar equivalent to AVL's, with the airfoil object attached (Flagged: the class signature is recall; only `airfoil.py` was opened [S4]).

**Shape3d `.s3d` / `.s3dx`.** Tertiary sources state `.s3dx` is XML-based, cannot be opened by V8 or older, and saving to V8 `.s3d` loses "the 3D layers and multi-curves edition" [S29]. Whether `.s3d` is binary was **not established** (Flagged). Shape3d publishes a "Shape3d to XFLR5" tutorial [S29], i.e. its foil path is section `.dat` + XFLR5 plane definition, not a CAD surface — a precedent that the surf/foil market already round-trips through Selig `.dat` and XFLR5 XML.

**DELFTship `.fbm` and BoardCAD `.brd`**: not opened this session — Flagged; both are application-native formats and neither is a plausible interchange target for v1.

**CFD-Bench station grammar** (repo, [S39]): `assembly { surface { axis, mirror, loft, station y= chord= inc= section= [x=] [z=] } }` — one line per station, section by catalog id, mirror flag, loft `linear|spline`, derived quantities absent. The spec's A4 contract supersedes station-only storage: authored stations **plus** five distribution-curve definitions (mode, controls, weights, constraints) form the surface of record [S40].

#### Comparison table — what each grammar stores vs derives

| Grammar | Encoding | Span description | Per-section profile override | Twist axis | Symmetry | Loft rule | Stored vs derived | Provenance fields | Label |
|---|---|---|---|---|---|---|---|---|---|
| AVL `.avl` | keyword text | explicit `SECTION` rows | yes (`AFILE`/`NACA`/`AIRFOIL` per section) | none — `Ainc` is a camber-line BC, geometry not rotated | `YDUPLICATE` | linear only (implicit) | stations stored; area/AR **input** as `Sref/Bref` (can disagree with geometry) | none | Verified [S6] |
| XFLR5 XML / `.xwimp` | XML / one line per segment | explicit sections with per-panel dihedral/twist | left/right foil names per section | version-dependent (Flagged) | left/right explicit | linear panels | stations stored; area derived in-app | none | Flagged [S28] |
| OpenVSP `.vsp3` | XML | stacked sections with per-section span/chord/sweep/twist/dihedral | per XSec airfoil (NACA/file/CST/Bezier) | explicit twist location | symmetry flags | blending/continuity controls | parameters stored; derived on load | none formal | Flagged (recall) |
| MachUpX JSON | JSON | distributions as functions of span fraction | airfoil list vs span fraction | implicit (Flagged) | `side` | linear between given points | distributions stored; everything else derived | none | Verified [S24] |
| AeroSandbox objects | Python | `WingXSec` list | airfoil object per xsec | about LE (Flagged) | `symmetric` | linear | stations stored | none | Flagged |
| Shape3d `.s3dx` | XML | orthogonal curves + slices | per slice | n/a | n/a | fair surface | curves stored | none known | Flagged [S29] |
| CFD-Bench grammar | line DSL | stations | `section=` per station | about LE (spec A4) | `mirror` | `linear|spline` | stations only; derive-never-store | catalog id + hash intended | Verified [S39] |
| Workbench A4 (target) | JSON | **five distribution curves + authored stations** | profile revision per station | LE, positive nose-up (spec) | mirror, Break symmetry | correspondence/continuity/closure | curves + stations stored; span/area/AR derived | hashes, source, tool/version required | Verified [S40] |

Observation (Inferred): every published grammar stores stations or distributions but none stores **curve editing state** (control weights, modes, locks). That state is the workbench's differentiator and its round-trip risk; it has no external precedent to borrow from, so DOC-02's "controls, weights, mode, locks" survive only in the native format.

### 3. Exact-surface CAD interchange

**STEP (ISO 10303).** Part 21 is the clear-text exchange structure (`ISO-10303-21; HEADER; FILE_DESCRIPTION/FILE_NAME/FILE_SCHEMA; ENDSEC; DATA; #n=ENTITY(...); ENDSEC; END-ISO-10303-21;`) — Flagged as recall for the exact tokens; the entity definitions were opened [S8]. `B_SPLINE_SURFACE` (a `BOUNDED_SURFACE`) carries `u_degree`, `v_degree`, `control_points_list: LIST OF LIST OF cartesian_point`, `surface_form`, `u_closed`, `v_closed`, `self_intersect`; `B_SPLINE_SURFACE_WITH_KNOTS` adds `u_multiplicities`, `v_multiplicities` (`LIST [2:?] OF INTEGER`), `u_knots`, `v_knots` (`LIST [2:?] OF parameter_value`) and `knot_spec: knot_type`, with derived `knot_u_upper = SIZEOF(u_knots)` and where-rules calling `constraints_param_b_spline` plus multiplicity checks (WR3, WR4). Rational surfaces are the complex entity `(B_SPLINE_SURFACE_WITH_KNOTS ... RATIONAL_B_SPLINE_SURFACE(weights_data))` [S8]. Consequence (Inferred): the writer must emit knots in the **multiplicity-compressed** form (distinct knots + multiplicities), not the flat knot vector rhino3dm/our maths hold; knot counts must satisfy Σmult = n_control + degree + 1 — a classic off-by-one in home-made writers.

*Bare surface vs face/shell.* A file that contains only `B_SPLINE_SURFACE_WITH_KNOTS` entities inside a `GEOMETRICALLY_BOUNDED_SURFACE_SHAPE_REPRESENTATION` imports as **surface bodies**, not as a solid (Flagged: entity name from recall; behaviour Inferred from OpenVSP's own description). OpenVSP's release notes [S34] draw exactly this line: untrimmed files "will not have slivers, gaps, and holes that can be introduced by the trimming process", while trimmed export "can form a watertight BREP solid" and the shell representation "is by definition not a single watertight BREP but is a bunch of separate trimmed surfaces". For a mold-CAM round trip the CAM system needs at least a **face set with a consistent outward normal**, ideally a closed shell (`ADVANCED_FACE → CLOSED_SHELL → MANIFOLD_SOLID_BREP`); a lofted wing that is closed at the trailing edge and capped at the tip is a candidate for a closed shell **only if** the tip cap and TE closure are themselves faces (Inferred). The repo's decision-0001 "one entity type, OpenVSP proves it" therefore proves *surface* export, not *solid* export — see **Contradicts existing repo knowledge** below.

*Application protocols.* AP203 (configuration-controlled design), AP214 (automotive), AP242 (managed model-based 3D engineering, merges 203/214, adds PMI). All three carry the same B-spline geometry entities; the choice affects the `FILE_SCHEMA` string and which header/product entities must be present (Flagged, recall). OpenVSP writes AP203 (repo Verified, [S41]). Fusion, Mastercam, PowerMill and Vectric all read AP203/AP214/AP242 (Flagged: vendor claims not opened this session; the EXP-02 gate requires an actual open-and-measure test).

*Writers.* STEPcode: "3-clause BSD", generates C++ (and experimental Python) from EXPRESS schemas, reads/writes Part 21, SDAI; originated at NIST, renamed 2012, developed with BRL-CAD; pushed 2026-09-10 [S10][S31]. `ruststep` (Apache-2.0) last released 0.4.0 on 2024-09-20 [S30] — usable for reading our own entity, stale for anything else (Flagged as maintenance risk). An own bounded writer (decision-0001 option 4) remains viable: the surface entity plus the minimal AP203 header/product/shape-representation scaffold is a few hundred lines, but it is **unproven until opened by CAM** (decision-0001 residual risk stands).

*Units in STEP.* Length unit is declared via `SI_UNIT(.MILLI., .METRE.)` or `(.METRE.)` inside the geometric representation context with an `UNCERTAINTY_MEASURE_WITH_UNIT` (global tolerance) (Flagged, recall). Millimetre is the CAM convention; the writer must state it and the EXP-02 dialog must show it. Part 21 reals are text: write shortest round-trip (17 significant digits max) so that the 1 µm DOC-02 tolerance is not consumed by serialisation (Inferred; see §6 probe).

**IGES.** Entity 128 rational B-spline surface, 126 rational B-spline curve, 144 trimmed surface, 143 bounded surface (Flagged, recall — the IGES 5.3 spec was not opened). OpenVSP writes IGES with the same trimmed/untrimmed options [S34]. IGES is legacy: writers exist, it lacks a solid model in practice, and CAM vendors accept it but prefer STEP (Flagged, practitioner consensus).

**3DM / openNURBS.** rhino3dm: MIT; Python/JS/.NET (NuGet `Rhino3dm`) on Windows/macOS/Linux; "points, point clouds, NURBS curves and surfaces, polysurfaces (B-Reps), meshes, annotations, extrusions, and SubDs"; "read and write all of the above information to and from the .3dm file format"; no STEP/IGES [S9]. Version 8.17.0 referenced in the README example (date not shown) [S9]. 3DM is the right **secondary** exact-surface export (Rhino is common in the surfboard/foil shaping world) and a free way to get a NURBS evaluator and Brep container in-process.

**Parasolid `.x_t/.x_b`, ACIS `.sat/.sab`.** Proprietary kernel formats; readable by Fusion/Mastercam but writable only through licensed kernels or OCCT-class translators — **never** for this project (Flagged, recall; consistent with decision-0001).

**glTF 2.0.** Khronos, mesh-only in core (triangles, PBR, animation); no NURBS; units are metres by convention (Flagged, recall — spec not opened). Suitable for viewport sharing/web preview, useless for CAM. **USD.** `UsdGeomNurbsPatch` "encodes a rational or polynomial non-uniform B-spline surface, with optional trim curves", following RenderMan's encoding; attributes `uVertexCount/vVertexCount`, `uOrder/vOrder`, `uKnots/vKnots`, `uRange/vRange`, `uForm/vForm` (open/closed/periodic), `pointWeights` (length must match points), flattened `trimCurve*` arrays [S26]. USD is a DCC/animation interchange; no CAM reads it (Inferred). **OBJ** has free-form support — `cstype [rat] bmatrix|bezier|bspline|cardinal|taylor`, `deg degu degv`, `surf s0 s1 t0 t1 v1 v2…`, `parm u|v …`, `trim`, `hole`, `vp u v w`, `end` — and "no unit metadata" [S25]; practically no importer beyond Maya/Blender-era tools reads the curve statements (Flagged). **DXF** `SPLINE` entity carries degree/knots/control points/weights for curves only (Flagged, recall) — useful for 2D section/planform export to laser/CNC-router 2D workflows (Vectric reads DXF), not for surfaces.

**What "opens as a smooth surface" requires (Inferred from [S8][S34] and decision-0001):** one non-rational or rational B-spline surface of degree ≤ 3 in each direction (higher degrees import but some CAM kernels degree-reduce; OpenVSP offers a "to cubic" option — Flagged), knots valid per the where-rules, no repeated interior knots unless a deliberate kink, no near-degenerate control rows at the tip (collapse the tip to a proper cap or leave it open), consistent parameter direction, and — for the CAM to see a **body** — faces with an outer loop, oriented, sewn within the file's uncertainty. Acceptance is EXP-02's "open-and-measure": import in Fusion, measure chord at three stations and max deviation ≤ 1 µm against the native evaluation; repeat in one more CAM system before the format is marked released.

### 4. Mesh formats for printing and CFD

| Format | Structure | Boundary markers | Units | Cell types | Reader/writer (licence) | Label |
|---|---|---|---|---|---|---|
| STL ASCII/binary | unindexed triangle soup, per-facet normal; binary = 80-byte header + uint32 count + 50-byte facets | none | **none** (mm by printer convention) | tri | own writer (trivial); Rust `stl_io` MIT 0.11.0 (2026-03-15) [S30]; meshio MIT | Flagged (layout recall) |
| 3MF | ZIP/OPC, `/3D/3dModel.model` XML, relationships; `unit` ∈ {micron, millimeter, centimeter, inch, foot, meter}, default millimeter; meshes MUST be manifold ("every triangle edge … shares … exactly 1 other triangle") with outward normals | per-object metadata; components | **yes** | tri (extensions add beam lattice, slices) | lib3mf BSD-2-Clause [S31]; own writer feasible (ZIP + XML) | Verified [S11] |
| OBJ | indexed `v/vt/vn/f`, groups | `g`/`o` names | none | polygons, free-form (rare) | own; meshio | Verified [S25] |
| PLY | header-described vertex/face lists, ASCII/binary | none | none | polygons | meshio | Flagged |
| Gmsh MSH 4.1 | `$MeshFormat` (version, file-type, data-size), `$PhysicalNames`, `$Entities`, `$Nodes`, `$Elements`; ASCII or binary; only elements in a physical group are written unless `Mesh.SaveAll` | physical groups | "There are no units in Gmsh" | full set incl. tet/hex/prism/pyramid | Gmsh itself **GPL** (process only); meshio MIT | Verified [S12] |
| SU2 `.su2` | `NDIME`, `NPOIN` + coords, `NELEM` + `vtk_type n0 n1…`, `NMARK`, `MARKER_TAG`, `MARKER_ELEMS`; 0-based, VTK ordering | marker tags | none | 3 line, 5 tri, 9 quad, 10 tet, 12 hex, 13 prism, 14 pyramid | own writer trivial; meshio | Verified [S13] |
| CGNS | SIDS data model + MLL (C/Fortran) over HDF5 (or ADF); AIAA Recommended Practice; boundary conditions and `DimensionalUnits`/`DataClass` are in the SIDS | BC nodes, families | **yes** | structured + unstructured | CGNS library (zlib-style licence — Flagged; site is CC0-dedicated content [S14]); SU2 reads CGNS "preserving boundary names as marker tags but ignoring embedded BCs" [S13] | Verified [S14][S13] |
| OpenFOAM `polyMesh/` | directory: `points`, `faces`, `owner`, `neighbour`, `boundary` (+ optional zones/sets); internal faces first with normal into the higher-label cell, boundary faces grouped by patch with outward normal; `boundary` = `{ name { type patch; nFaces; startFace; } }`; `owner` header carries `nCells` | patches | none (metres by convention; `convertToMeters` lives in `blockMeshDict`, not polyMesh — Flagged) | polyhedral | own writer feasible; OpenFOAM utilities (GPL, process) | Verified [S15] |
| Exodus II | NetCDF/HDF5 container, element blocks, node/side sets, time steps | side sets | none | FE types | SEACAS (BSD-3 — Flagged; GitHub `NOASSERTION` [S31]); meshio | Flagged |
| VTK legacy `.vtk` / XML `.vtu .vtp .vts .vtr .vti .vtm .pvd .pvtu` | legacy: version line, title, ASCII/BINARY, DATASET type, POINT_DATA/CELL_DATA; XML: appended/base64/zlib | none (field arrays) | none | full set | VTK BSD-3 (Flagged; `NOASSERTION` [S31]); meshio; own writer for `.vtu` ASCII/appended is small | Flagged (docs 404 this session) |
| Fluent `.msh/.cas` | zone-based sections, documented in Ansys manuals; `.cas` also carries setup | zones | none (scale in case) | full set | meshio reads ANSYS msh | Flagged |
| Tecplot `.dat/.plt/.szplt` | ASCII documented; binary proprietary/TecIO | zones | none | ordered/FE | meshio reads Tecplot .dat; SU2 writes Tecplot [S17] | Flagged |
| EnSight Gold | case file + geometry + per-variable files | parts | none | full set | VTK/ParaView read | Flagged |

`meshio` (MIT) reads/writes Abaqus, ANSYS msh, AVS-UCD, CGNS, DOLFIN, Exodus, FLAC3D, H5M, MDPA, Medit, MED, Nastran, Netgen, Gmsh, OBJ, OFF, PERMAS, PLY, STL, Tecplot .dat, TetGen, SVG, SU2, UGRID, VTK, VTU, WKT, XDMF [S16] — but its last push was 2024-07-23 [S31], so treat it as a **Python sidecar for conversion in development and tests**, not as a product dependency (Inferred). It is not installed in the local Python (`ModuleNotFoundError`, observed).

**The STL units problem.** STL carries no unit; slicers assume mm and CAD exporters differ (Fusion exports mm by default; some tools export m). The exporter must (a) write mm for printing, (b) put the unit in the filename or an accompanying manifest, and (c) prefer 3MF, which declares the unit, for printing (Inferred from [S11]; the mm default is Flagged recall). For CFD the workbench controls both ends: write metres, write the unit into the run document, and never trust an imported mesh's scale without asking.

### 5. CFD results and data formats

**OpenFOAM.** A case is a directory: `system/`, `constant/polyMesh/`, time directories `0/`, `100/`… each holding one file per field with a `FoamFile` header (`version`, `format ascii|binary`, `class volScalarField…`, `object`) (Flagged: header keys from recall; the polyMesh part is Verified [S15]). Function objects write under `postProcessing/<name>/<startTime>/` (Flagged, recall); OpenFOAM-12's `forces` "calculates the forces and moments by integrating the pressure and skin-friction forces over a given list of patches", requires `patches`, `CofR`, `rho`/`rhoInf` ("for incompressible cases, set rho to rhoInf"), and "writes the forces/moments into the file `<timeDir>/forces.dat` and bin data (if selected) to `<timeDir>/forces_bin.dat`" [S32]. The ESI (openfoam.com) line writes `force.dat`/`moment.dat` with separate total/pressure/viscous columns instead — the two forks differ here (Flagged; the ESI page was 404 this session). `forceCoeffs` adds `magUInf`, `lRef`, `Aref`, `liftDir`, `dragDir`, `pitchAxis` and writes `coefficient.dat` (Flagged, recall). `yPlus`, `wallShearStress` write **fields** into the time directory; `probes` and `sample`/`surfaces` write raw/CSV/VTK under `postProcessing/` (Flagged, recall). ParaView reads a case through the built-in reader ("PVFoamReader and vtkPVFoam libraries" via `paraFoam`) with time/region/field selection in the Properties panel [S33]; the empty `<case>.foam` file trick for ParaView's own reader was not confirmed on the page opened (Flagged, recall — widely used).

**SU2.** `HISTORY_OUTPUT` selects fields (residuals `RMS_DENSITY…`, coefficients `DRAG`, `LIFT`, `EFFICIENCY`, iteration indices, `WALL_TIME`) written to the history file (`CONV_FILENAME`, CSV or Tecplot); `OUTPUT_FILES` ∈ {RESTART, RESTART_ASCII, PARAVIEW, PARAVIEW_ASCII, PARAVIEW_MULTIBLOCK, SURFACE_PARAVIEW(_ASCII), TECPLOT(_ASCII), SURFACE_TECPLOT(_ASCII), CSV, SURFACE_CSV, STL_BINARY, STL_ASCII, MESH}, default `(RESTART, PARAVIEW, SURFACE_PARAVIEW)`; `VOLUME_OUTPUT` defaults to `COORDINATES,SOLUTION,PRIMITIVE`; `OUTPUT_WRT_FREQ` controls frequency [S17]. Default filenames `history.csv`, `flow.vtu`, `surface_flow.vtu`, `restart_flow.dat` (Flagged, recall). SU2 is LGPL-2.1 (Flagged, recall; `NOASSERTION` [S31]) — process invocation only, which the constraints already allow.

**Containers for our own results.**
- *HDF5*: BSD-3-style licence (retain notice, reproduce in binary distributions, no endorsement) [S18]. PureHDF: "a pure C# library without native dependencies" for reading **and writing** (groups, datasets, attributes, chunking, compression filters, async read), v3 targets .NET 8+, follows the HDF5 file-format spec v1.10; MIT [S19]. `HDF.PInvoke` (HDF Group) last pushed 2024-02-20 and needs native binaries [S31] — prefer PureHDF. Rust: `hdf5-metno` 0.15.0 (2026-09-18, MIT OR Apache-2.0), a maintained fork of `aldanor/hdf5-rust` [S30].
- *Parquet/Arrow*: Parquet.Net "fully managed", MIT, .NET 8 and 10, all types/encodings/codecs, row groups, class serialisation, `Microsoft.Data.Analysis` DataFrame integration [S20]; Rust `parquet`/`arrow` 60.0.0 (2026-09-15, Apache-2.0) [S30]. Parquet is columnar and immutable per file — ideal for **sweep tables** (one row = one sample × one quantity or one row = one sample with columns), poor for arrays of fields.
- *Zarr / NetCDF*: chunked N-D arrays; NetCDF-4 is HDF5 underneath; Zarr v3 is directory-of-chunks with JSON metadata — Git-hostile at scale but cloud-friendly; no mature pure-.NET writer known (Flagged, recall). Not needed while HDF5 covers volumetric results.

**Reference vs embed (Inferred from the above and from A3's Analysis run/Sweep/Field evidence).** The run document (`*.cfdw-run.json`) should embed: the input snapshot hashes (surface revision, profile revisions, water record, method/settings), the resolved sample schedule, the backend identity/version/smoke-test id, per-sample status and timing, scalar outcomes (CL, CD, CM, residual floors, y⁺ range, Cp_min) and the **inventory** of field files with their path, format, unit, variable names, byte size and SHA-256. It should reference: meshes, solver case directories, `.vtu`/`.foam` fields, history CSVs and any HDF5/Parquet sidecar. It should never embed a field. Deleting a referenced file demotes the run to "evidence missing", not to "invalid".

### 6. Native project format design (`.cfdw.json`)

#### Evidence base

- **JSON Schema.** The current specification is **2020-12** (succeeding 2019-09), split into Core and Validation, with meta-schemas under `draft/2020-12/schema`; no Hyper-Schema for 2020-12 [S27]. Release notes cover the 2019-09→2020-12 changes (`prefixItems`/`items`, `$dynamicRef`, `unevaluatedProperties`) — Flagged as recall since the release-notes page was not opened. `$schema: "https://json-schema.org/draft/2020-12/schema"` is the identifier to write (Inferred from the path shown).
- **Canonicalisation for hashing.** RFC 8785 JCS (Informational): properties sorted by UTF-16 code units "independent of locale settings"; numbers "MUST be serialized according to Section 7.1.12.1 of ECMA-262" (shortest round-trip, ES `Number.prototype.toString`); "whitespace between JSON tokens MUST NOT be emitted"; strings escaped per ECMAScript; numbers beyond IEEE-754 double "RECOMMENDED to represent … as JSON strings"; purpose: cryptographic operations over a canonical counterpart while the wire form stays free [S21].
- **.NET number formatting.** MS Learn: since .NET Core 3.0 `Double.ToString()`/`"G"` gives the "smallest round-trippable number of digits"; `"R"` is "recommended for the BigInteger type only", and in .NET Framework/.NET Core < 3.0 "R" fails to round-trip some doubles; `"G17"` always round-trips but prints noise digits [S22].
- **Probe (executed 2026-09-20, .NET runtime 10.0.7, `dotnet run app.cs`, scratchpad `spike-json/app.cs`) [S36]:**

| value | `ToString()` de-DE | Invariant | `"R"` | `"G17"` | `Utf8JsonWriter` | bit-exact round trip |
|---|---|---|---|---|---|---|
| 0.1 | `0,1` | `0.1` | `0.1` | `0.10000000000000001` | `0.1` | yes |
| 1/3 | `0,3333333333333333` | `0.3333333333333333` | same | `0.33333333333333331` | `0.3333333333333333` | yes |
| 1e-7 | `1E-07` | `1E-07` | `1E-07` | `9.9999999999999995E-08` | `1E-07` | yes |
| 0.30000000000000004 | … | `0.30000000000000004` | same | same | same | yes |
| −0.0 | `-0` | `-0` | `-0` | `-0` | `-0` | yes |
| 1e21 | `1E+21` | `1E+21` | `1E+21` | `1E+21` | `1E+21` | yes |
| 2.5e-5 | `2,5E-05` | `2.5E-05` | `2.5E-05` | `2.5000000000000001E-05` | `2.5E-05` | yes |
| NaN | — | — | — | — | **ArgumentException** ("cannot be written as valid JSON") | n/a |

Observed consequences (Verified by execution):
  1. `Utf8JsonWriter` is culture-invariant and shortest-round-trip; storage adds **zero** geometric error, so DOC-02's 1 µm budget is spent entirely on evaluation, not serialisation.
  2. Its exponent form (`1E-07`, `1E+21`, `-0`) is **not** JCS (`1e-7`, `1e+21`, `0`). A JCS hash must be computed by a dedicated canonicaliser, and the file bytes are not the hash input.
  3. NaN/±Infinity throw: "not recorded" must be `null` or an explicit status field, never a numeric sentinel — which is also what IO ("degrades to not recorded") demands.
  4. `"G17"` prints noise (`0.10000000000000001`) — never use it for a Git-diffed file; `"R"` on modern .NET equals shortest but the docs steer away from it; use the default/`Utf8JsonWriter`.
  5. .NET 10 file-based apps ship with reflection-based `JsonSerializer` **disabled** (`InvalidOperationException: Reflection-based serialization has been disabled`); the product must use `JsonSerializerContext` source generation (also required for trimmed/AOT Avalonia builds).
- **KiCad precedent** [S23]: S-expressions derived from Specctra DSN; "human readability is a design goal"; "all values are given in millimeters. Exponential floating point values are not used for readability purposes"; nanometre board precision ("six decimal places or 0.000001 mm"); `version YYYYMMDD` token; UUID identity; legacy timestamps re-encoded as UUIDs on migration. KiCad chose a *fixed decimal count* over shortest-round-trip — readable diffs, at the cost of a quantisation floor (1 nm), which is well below any CAM tolerance.
- **OpenVSP `.vsp3`**: XML with every parameter as a `Parm` element carrying value and id (Flagged, recall). **Blender `.blend`**: binary DNA-described; **Fusion**: cloud-native; **Onshape**: cloud DB — none is a usable precedent for a Git-friendly local file (Flagged, recall). **Shape3d** `.s3dx` XML [S29] is the closest in-domain precedent and it is application-private.
- **Units.** UCUM (Unified Code for Units of Measure) and QUDT (RDF units ontology) provide machine-readable unit identifiers (Flagged, recall; neither site opened this session). Options: units in field names (`chord_m`, `inc_rad` — the repo sketch), a per-document unit declaration (`"units": "SI"`), or per-value objects (`{"value": 0.141, "unit": "m"}`).

#### Critique of the repo's `.cfdw.json` sketch (proposal-sequence §3.2, [S42])

| Sketch element | Verdict | Evidence / reason |
|---|---|---|
| `"format": "cfdw-wing", "version": 1` | Keep; add `$schema` (2020-12 URI), `generator` (app name+version, KiCad precedent [S23]) and make `version` **semver-like with separate major/minor** (major = breaking; minor = additive optional fields — EXP-01 "unknown optional context retained/read-only or refused before save") | [S23][S27], spec EXP-01/DOC-04 |
| `"units": "SI"` + `_m`/`_rad` suffixes | Keep **both**: suffix on every numeric field (self-describing rows in a diff; GAP-04 "carry units in the type") and the document-level declaration as a redundancy check; reject a file whose suffixes disagree with the declaration | GAP-04 [S43]; Inferred |
| `"frame": "le-root-xaft-zup"` | Keep; make it an enum with exactly one v1 value and a documented mapping to AVL's X-downstream/Y-right/Z-up [S6] and to OpenVSP | spec A4 units section [S40] |
| `generative` block as recipe | Keep as `recipe` with `authoritative: true|false` flipped to `false` by the first station/curve edit (one-way burst, §3.5 of the sequence) — but never delete it (provenance) | [S42][S39] |
| `surface.stations[]` as the geometry | **Insufficient.** A4 requires the five distribution curves with mode, controls, weights and constraints. Stations become `authored_stations[]` carrying identity (UUID), span location, profile revision reference and per-channel constraints; `distributions.{sweep,chord,elevation,twist,thickness}` carry `mode`, `controls[]`, `constraints[]`, and `continuity` | spec A3/A4 [S40] |
| `"section": "E817"` string | Replace with `{ "profile": "<profile-revision-id>" }` resolving to a `profiles` table in the same document holding original coordinates, detected format, normalisation transform, source URL/licence note, SHA-256 of the original bytes, and the editable curve definition | CAT-01/02, ANA-09 [S40] |
| Derived quantities absent | Keep — but **write a `derived_check` block** (span, S, AR at 1e-6 relative) that a reader must recompute and compare; disagreement is a corruption/version signal, not a second authority (derive-don't-store still holds: the block is a checksum, not an input) | DM "derive don't store"; Inferred |
| Results "in a sidecar keyed by content hash" | Keep; specify the hash = SHA-256 over the **JCS form** of the `surface_revision` subtree (not the whole file, so metadata edits do not orphan runs), stored as `revision_hash` on save and verified on load | RFC 8785 [S21]; ANA-09 |
| Single `surface` | Make `surfaces[]` (front wing, stabiliser, strut are in the grammar already) with v1 UI limited to one; the loader must accept and preserve others read-only | [S39], EXP-01 |
| No history | Add `history[]` as an **append-only** list of accepted edits (`{id, parent, at, kind, summary, revision_hash}`) — the A3 "Design revision" chain; store full snapshots per revision in a `revisions/` sidecar or in-file with a size cap, and reconstruct undo from the live model, not from the file | A3 [S40]; parametric-geometry "append-only" [S39] |
| Numbers | Shortest round-trip via `Utf8JsonWriter`; **fixed key order** (schema order, not sorted — readability) and **one key per line** with 2-space indent so Git diffs are one row = one change; arrays of controls one element per line | probe [S36]; KiCad [S23] |
| No identity | Every station, control, constraint and profile carries a UUID (v7 preferred for sortable time-ordering — Flagged) so that history entries and assistance proposals can reference them by identity, not by index | A3 "referenced by identity" |
| Large data | Never in `.cfdw.json`. Polars → Parquet sidecar (MIT reader); fields → HDF5 or the solver's own VTK files referenced by hash | [S19][S20] |

#### Recommended document shape (Inferred synthesis)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "format": "cfdw",
  "format_version": { "major": 1, "minor": 0 },
  "generator": { "name": "CFD-Workbench", "version": "0.1.0+abc123" },
  "units": "SI", "frame": "le-root-xaft-ystarboard-zup",
  "design": { "id": "…uuid…", "name": "Downwind 1100", "revision_hash": "sha256:…", "context": { } },
  "profiles": { "<profile-rev-id>": { "source": { "kind": "uiuc-dat", "url": "…", "sha256": "…", "detected_format": "lednicer" },
                                       "original_points": [[1.0,0.0], …], "normalization": { "translate": [0,0], "rotate_rad": 0, "scale": 1 },
                                       "curve": { "upper": { "mode": "smooth", "controls": [ … ] }, "lower": { … } } } },
  "surfaces": [ { "id": "…", "role": "front-wing", "mirror": true,
                  "distributions": { "sweep": { "mode": "through-points", "anchors": [ … ], "constraints": [ … ] },
                                     "chord": { "mode": "smooth", "controls": [ { "id": "…", "s": 0.3, "value_m": 0.108, "weight": 1.0 } ] },
                                     "elevation": { … }, "twist": { … }, "thickness": { … } },
                  "authored_stations": [ { "id": "…", "s_m": 0.0, "profile": "<profile-rev-id>", "constraints": [ … ] } ],
                  "loft": { "correspondence": "arc-length", "continuity": "G2", "closure": { "tip": "cap", "te": "closed" } } } ],
  "recipe": { "authoritative": false, "area_m2": 0.14, "aspect_ratio": 7.0, "…": "…" },
  "derived_check": { "span_m": 0.98995, "area_m2": 0.14, "aspect_ratio": 7.0, "tolerance_rel": 1e-6 },
  "history": [ { "id": "…", "parent": null, "at": "2026-09-20T10:00:00Z", "kind": "recipe.generate", "revision_hash": "sha256:…" } ],
  "runs": [ { "ref": "runs/2026-09-20T10-05-00Z-a1b2.cfdw-run.json", "sha256": "…", "status": "complete" } ],
  "extensions": { }
}
```

Rules: unknown top-level keys and unknown keys inside `extensions` are preserved verbatim on save (EXP-01); unknown keys **inside** `surfaces`/`profiles` with a higher `minor` are preserved; a higher `major` refuses to open with the path named (DOC-04); migrations write `<name>.cfdw.json.v1.bak` first (DOC-04 "migrations preserve an original copy").

### 7. Grammars and DSLs

- **OpenSCAD** (GPL-2.0 — Flagged; GitHub `NOASSERTION` [S31]) is a functional CSG language: text is the model, no GUI editing state, diffs are semantic. **CadQuery** (Apache-2.0 — Flagged; `NOASSERTION` [S31]) and **build123d** (Apache-2.0, pushed 2026-09-20 [S31]) are Python APIs over OCCT: "code as CAD", excellent for reproducibility and parameter sweeps, poor for direct-manipulation editing — every drag would have to be re-expressed as a code edit. **OpenVSP** exposes an AngelScript/Python API over its parameter tree; the `.vsp3` file remains a schema, the script is an *operation* language (Flagged, recall).
- **The CFD-Bench station DSL** [S39] is readable and diffable, but it cannot carry curve controls, weights, constraints or UUIDs without growing a full grammar — at which point it is JSON with a custom parser and no validator.
- **When a DSL wins (Inferred):** the artifact is authored by humans in a text editor, reproducibility across versions matters more than tool state, and operations (not states) are the unit of sharing — e.g. an **optimisation or sweep script**, an assistant "recipe", a test fixture. **When a schema wins:** the artifact is produced by direct manipulation, must round-trip editing state losslessly (DOC-02), needs machine validation (JSON Schema 2020-12 [S27]) and canonical hashing (JCS [S21]), and must be readable by other tools without a bespoke parser.
- **Recommendation:** schema for the document of record; a small **command grammar** (JSON operations: `set-control`, `add-station`, `apply-recipe`, `import-profile`) as the history entry kind and the assistant's structured-output target (the sequence's "typed object, never coordinates"). A text DSL for *scripts* (sweeps/optimisation) is a later door, not v1.

### 8. Licence and platform notes for candidate libraries (2026-09-20)

| Need | .NET | Rust | Python sidecar (dev/test only) | Licence (SPDX) | Evidence |
|---|---|---|---|---|---|
| JSON read/write, schema | `System.Text.Json` (+ source-gen context); JsonSchema.Net (MIT — Flagged) | `serde`/`serde_json` (MIT OR Apache-2.0) | — | MIT | [S30][S36] |
| JCS canonical hash | own (~150 lines; ES number formatting is the hard part) | `serde_jcs` (Flagged) | — | — | [S21] |
| NURBS maths / 3DM | Rhino3dm NuGet (MIT, v8.17) | `truck` (MIT, 1.0.0 dated 2020-09-20 — stale, Flagged) | rhino3dm.py | MIT | [S9][S30] |
| STEP write/read (own entity) | own bounded writer; STEPcode via P/Invoke (BSD-3) | `ruststep` (Apache-2.0, 0.4.0, 2024-09-20) | — | BSD-3 / Apache-2.0 | [S10][S30] |
| STL | own | `stl_io` (MIT, 0.11.0, 2026-03-15) | meshio, numpy-stl | MIT | [S30] |
| 3MF | own (ZIP+XML) or lib3mf (BSD-2-Clause, C++ with C# bindings — Flagged) | — | — | BSD-2-Clause | [S31][S11] |
| SU2 mesh, OpenFOAM polyMesh, VTU | own writers (text grammars) | own | meshio (MIT, last push 2024-07-23) | MIT | [S13][S15][S16] |
| HDF5 | PureHDF (MIT, pure C#, .NET 8+) | `hdf5-metno` (MIT OR Apache-2.0, 0.15.0) — links libhdf5 (BSD-3-style) | h5py (BSD-3 — Flagged) | MIT / BSD-3 | [S18][S19][S30] |
| Parquet/Arrow | Parquet.Net (MIT, .NET 8/10); Apache.Arrow (Apache-2.0 — Flagged) | `parquet`/`arrow` 60.0.0 (Apache-2.0) | pyarrow (Apache-2.0) | MIT / Apache-2.0 | [S20][S30] |
| CGNS | none pure-managed known (Flagged); MLL via P/Invoke (zlib-style) | `cgns-sys` (Flagged) | pyCGNS/h5py | zlib-style (Flagged) | [S14] |
| Solvers (process only) | SU2 (LGPL-2.1, Flagged), OpenFOAM (GPL-3.0, Flagged), Gmsh (GPL [S12]), XFLR5 (GPL, Flagged) | — | — | copyleft, invoked as processes | [S12][S31] |
| Airfoil tooling | own parser | own | AeroSandbox (MIT) for fixtures and cross-checks | MIT | [S31][S4] |

Platform note: PureHDF and Parquet.Net are pure managed and run on arm64 macOS and x64/arm64 Windows without native binaries [S19][S20]; `HDF.PInvoke` and STEPcode bring native builds per platform — a packaging cost the constraints ("compatible with both target platforms") make visible.

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| UIUC `.dat` (Selig/Lednicer) [S1][S3] | A profile is a point list with a name | Two text layouts, one extension | Universal, tiny, XFOIL-native | Layout ambiguity, no units, no provenance, no licence statement | none stated | Verified |
| AVL `.avl` [S6] | A wing is sections + spacing + refs | Keyword text | Minimal, decades-stable, VLM-ready | Linear only; twist is a BC; Sref can disagree with geometry | GPL (tool) | Verified |
| MachUpX JSON [S24] | Distributions are functions of span fraction | JSON + airfoil JSON | Closest to five-channel model; step twist; airfoil list over span | No editing state; aerodynamics decoupled from outline | MIT | Verified |
| OpenVSP `.vsp3` + STEP/IGES [S7][S34] | Parametric components with export options | XML params; STEPcode writer | Trimmed watertight BREP since 3.21; Bezier `.bz`; CSV metadata | Trimming can create slivers/gaps; NOSA licence for code | NOSA 1.3 (Flagged) | Verified export behaviour |
| Shape3d `.s3dx` [S29] | Board/foil as orthogonal curves + slices | XML, app-private | In-domain; XFLR5 tutorial shows the interchange path | Version lock (V8 loses data); undocumented | proprietary | Flagged |
| STEP AP203 B-spline surface [S8] | Exact NURBS for CAD/CAM | Part 21 text | Universal CAM import; one entity suffices for a surface | Surface ≠ solid; knot multiplicity encoding; own writer unproven | ISO (fee); STEPcode BSD-3 | Verified entity, Inferred CAM behaviour |
| 3MF [S11] | Printable model with units and manifold rules | ZIP/OPC + XML | Units, manifold contract | Printing only; no CFD reader | spec: open (Flagged); lib3mf BSD-2 | Verified |
| CGNS [S14] | CFD data with BCs and units as a data model | HDF5 + SIDS | Units, BCs, structured/unstructured; SU2 reads it | Heavy library; no pure-.NET writer | zlib-style (Flagged) | Verified scope |
| OpenFOAM case dir [S15][S32] | The directory is the format | Text dictionaries and lists | Human-inspectable; ParaView-native | Two forks differ in outputs; no units | GPL-3.0 (process) | Verified core |
| SU2 `.su2` + outputs [S13][S17] | Minimal ASCII mesh + configurable outputs | Text + VTK/Tecplot/CSV | Trivial to write; markers by name | No units; binary restart is SU2-private | LGPL-2.1 (process) | Verified |
| KiCad S-expr [S23] | Readable, diffable EDA file | S-expressions, fixed decimals, version date, UUIDs | Best Git-friendly precedent | Custom parser; no schema validator | GPL (tool) | Verified |
| RFC 8785 JCS [S21] | Canonical JSON for hashing/signing | Sort keys, ES numbers, no whitespace | Deterministic hash independent of pretty form | Not a storage format; ES number rules ≠ .NET default | RFC (free) | Verified |
| AeroSandbox [S4][S5] | Airfoil/wing as Python objects | numpy; `.dat` regex reader; CST | CST parameter set; fixtures | No Lednicer detection; 6-decimal writer | MIT | Verified |

## Reference information

- **ISO 10303-21 / -42 / AP203/214/242** — clear-text encoding and geometric resource entities; requires the B-spline surface where-rules (knot multiplicities, degree, closed flags) to hold and, for a body, faces/shells with orientation. Entities Verified via the STEP Tools AIM pages [S8]; the ISO texts themselves are paywalled and were not opened.
- **UIUC Airfoil Coordinates Database** — ≈1,650 airfoils, Selig ordering, `#` comments, `coord/`, `coord_seligFmt/`, `coord_updates/` archives; "© 1994–2026 UIUC Applied Aerodynamics Group"; **no licence text found** — bundling any UIUC file needs written permission or an independent source for the coordinates (many Eppler sections are published in Eppler's book; NACA sections are computable) [S1][S2][S3].
- **AVL 3.40 user guide** — the `.avl` grammar and its semantics [S6].
- **3MF Core Specification** — units, container, manifold/orientation MUSTs [S11].
- **Gmsh reference manual (MSH 4.1)** — sections, physical groups, no units [S12].
- **SU2 docs: Mesh File; Custom Output** — `.su2` grammar, `OUTPUT_FILES`, history [S13][S17].
- **CGNS SIDS** — data model incl. BCs and `DimensionalUnits` [S14].
- **OpenFOAM v2xxx user guide 4.1 Mesh description; OpenFOAM-12 `forces.H`; CFD Direct v12 ParaView chapter** [S15][S32][S33].
- **RFC 8785** [S21]; **JSON Schema 2020-12** [S27]; **.NET standard numeric format strings** [S22]; **KiCad S-expression intro** [S23].
- **HDF5 LICENSE**, **PureHDF**, **Parquet.Net**, **crates.io** and **GitHub licence metadata** [S18][S19][S20][S30][S31].
- **Repo sources**: parametric-geometry grammar [S39], spec A3/A4/A5 [S40], decision-0001 [S41], proposal-sequence §3.2–3.4 [S42], gap register GAP-04/GAP-08 [S43].

## Data, constants, formulae and invariants

- **Selig invariant:** `x[0] ≈ 1`, `x` decreases to a minimum ≈ 0 exactly once, then increases to ≈ 1; polygon simple (no self-intersection); `n ≥ ~40`. **Lednicer invariant:** counts `NU, NL` are integral floats ≥ 2; block 1 has `NU` rows with `x` non-decreasing from ≈0 to ≈1; block 2 has `NL` rows likewise; conversion to Selig = `reverse(upper) ++ lower[1:]` if the LE point is duplicated, else `reverse(upper) ++ lower` (Inferred, [S3]).
- **Trailing-edge thickness:** `t_te = |y_upper(1) − y_lower(1)|` (chord units); catalog TEs are 0–0.08 mm at 80 mm chord (repo, [S42]) — below any CNC/print floor; the exporter must state whether it closed the TE and how.
- **Storage resolution:** shortest round-trip double = exact (probe [S36]); AeroSandbox `%f` = 1e-6 chord units [S4]; KiCad = 1 nm [S23]; DOC-02 = 1 µm model space ⇒ at chord ≥ 1 m a 6-decimal chord-unit export is at the tolerance edge.
- **STEP knot rule:** `Σ u_multiplicities = n_u_control + u_degree + 1` (and likewise v); `knot_spec` ∈ {UNIFORM_KNOTS, QUASI_UNIFORM_KNOTS, PIECEWISE_BEZIER_KNOTS, UNSPECIFIED} (Flagged: enumeration from recall; where-rule existence Verified [S8]).
- **SU2 mesh:** node indices 0-based; element line = `vtk_type n0 … nk`; boundary elements in 2D are lines (3), in 3D triangles (5)/quads (9) [S13].
- **OpenFOAM polyMesh:** internal faces first; each internal face's normal points into the higher-numbered cell (owner < neighbour); boundary faces contiguous per patch (`startFace`, `nFaces`); boundary normals point out of the domain [S15].
- **3MF:** default unit millimetre; every edge shared by exactly two triangles; outward normals [S11].
- **Content hash:** `revision_hash = "sha256:" + hex(SHA-256(JCS(surface_revision_subtree)))`; JCS numbers per ECMA-262 §7.1.12.1 (`1e-7`, not `1E-07`; `0`, not `-0`) [S21][S36].
- **Units policy invariant:** every numeric field name ends in a unit suffix from a closed list (`_m`, `_m2`, `_rad`, `_kg`, `_N`, `_Pa`, `_K` or `_degC`, `_mps`) or is dimensionless and documented as such; `weight` is dimensionless and positive; a reader rejects an unknown suffix (Inferred from GAP-04 [S43]).

## Design implications for CFD-Workbench

1. **CAT-02 parser is a two-layout detector with fail-closed rules** (§1). Ship fixtures: `e817` in both UIUC layouts [S3], a `#`-commented file, a CRLF file, a BOM file, a Lednicer file with unequal counts, a Lednicer file read as Selig (must be rejected as self-crossing), a half-profile (must be rejected). Preview shows detected format, counts, LE duplication, TE gap, normalisation transform and SHA-256 of the original bytes. Touches CAT-02, A4 "Catalog and bounded section import".
2. **Do not bundle UIUC files without rights** (§1, [S2]). The catalog admission gate (CAT-01/ANA-09) records the coordinate source and licence per profile; where UIUC is the only source, mark **Pending admission** and prefer a computable (NACA) or independently published (Eppler) source. This is a **Flagged** legal risk, not a technical one.
3. **Native format = A4 explicit definition + provenance, nothing derived** (§6). Adopt the recommended shape: `format_version {major, minor}`, `generator`, `units` + suffixes, `frame` enum, `profiles` table with originals and hashes, `surfaces[].distributions` with mode/controls/weights/constraints, `authored_stations` with UUIDs, `recipe.authoritative`, `derived_check`, `history[]`, `runs[]` references, `extensions`. Touches DOC-02, DOC-04, EXP-01, A3 aggregates.
4. **Hash the JCS form, not the bytes** (§6 probe). Implement an RFC 8785 canonicaliser with ES number formatting and test it against the .NET default output differences (`1E-07`, `-0`). Touches ANA-09, the run/sweep evidence model ("Analysis run references the exact inputs").
5. **Serialise with `Utf8JsonWriter` + source-generated contexts**, one key per line, schema-ordered keys, shortest-round-trip doubles, `null` for not-recorded, never NaN (§6 probe). Validate on load against a committed JSON Schema 2020-12 document; keep the schema in the repo as the contract test fixture.
6. **DOC-02's 1 µm is an evaluation tolerance**, not a storage one (§6 probe). The round-trip test must re-evaluate the loft from the reloaded definition and compare **surfaces**, not compare JSON text; serialisation contributes 0.
7. **STEP export = surface(s) + faces + shell, millimetres, cubic where possible; released only after open-and-measure in ≥2 CAM systems** (§3). Write `B_SPLINE_SURFACE_WITH_KNOTS` (rational only if the loft is rational), then wrap in `ADVANCED_FACE`/`OPEN_SHELL` at minimum; offer "untrimmed surfaces" and "closed shell (tip cap + TE)" as separate options with the OpenVSP wording about slivers vs watertightness. Touches EXP-02 and decision-0001 residual risk.
8. **AVL export must sample the distributions**, not just authored stations, to bound linear-interpolation error, and must document that `Ainc` is a camber-line BC (§2, [S6]). The exported `Sref/Bref/Cref` are derived from the same evaluation as the UI's AR readout. Touches ANA (VLM tier) and EXP-02.
9. **Mesh export declares units explicitly**: 3MF (mm, `unit="millimeter"`) as the default print format, STL as a fallback with unit in the filename and the dialog, `.su2`/`polyMesh`/`.vtu` in metres for CFD (§4). Touches EXP-02, CFD-01/02.
10. **Run documents reference, never embed, fields** (§5): path + format + unit + variable inventory + SHA-256 per artifact; scalar outcomes and sample schedule embedded; sweep tables as Parquet sidecar; volumetric extractions as HDF5 via PureHDF. Missing artifact ⇒ "evidence missing" state. Touches A3 Analysis run / Sweep / Field evidence, VIZ-01–04.
11. **OpenFOAM fork awareness**: the results reader must detect `forces.dat` (Foundation) vs `force.dat`+`moment.dat` (ESI) and fail closed on an unknown layout rather than guess columns (§5, [S32]; ESI layout Flagged). Touches CFD-05/06 capability detection.
12. **Format matrix (recommended):**

| Format | v1 read | v1 write | Later | Never | Justification |
|---|---|---|---|---|---|
| `.cfdw.json` (native) | yes | yes | — | — | The only open path (COMMIT-03) |
| Selig/Lednicer `.dat` | yes (CAT-02) | yes (section export, shortest-round-trip) | — | — | Universal; fixtures exist [S1][S3] |
| CST parameter JSON | — | — | read/write inside profile revision | — | No external standard; internal representation first [S5] |
| AVL `.avl` + `AFILE` | — | yes (behind VLM tier) | read (stations only, Inferred label) | — | Verified grammar; lossy semantics [S6] |
| XFLR5 XML / `.xwimp` | — | later (write) | read | — | Tertiary-sourced; open an exported file first [S28] |
| MachUpX JSON | — | — | write (comparison runs) | — | MIT, closest model [S24] |
| OpenVSP `.vsp3` | — | — | maybe write | read | XML params unverified; different loft semantics |
| STEP AP203 (surface/shell) | own entity only (later) | yes, **gated by open-and-measure** | AP242 header | arbitrary B-Rep import | decision-0001; [S8][S34] |
| IGES 128/144 | — | — | write if a CAM asks | read | Legacy; STEP suffices (Flagged) |
| 3DM | — | later via rhino3dm (MIT) | read own surfaces | — | [S9] |
| Parasolid / ACIS | — | — | — | **never** | Proprietary kernels |
| glTF / USD | — | glTF viewport export (later) | — | CAM use | Mesh-only / DCC-only [S26] |
| OBJ (mesh) | — | yes (cheap) | — | free-form curves | No units [S25] |
| DXF (2D sections/planform) | — | later | — | surfaces | 2D router workflows (Flagged) |
| STL | later (slice-and-fit spike) | yes (mm, unit stated) | — | — | Printing fallback |
| 3MF | — | yes | — | — | Units + manifold contract [S11] |
| `.su2` / `polyMesh` / Gmsh `.msh` | — | `.su2` and `polyMesh` yes; `.msh` via Gmsh process | read own meshes | — | Text grammars [S13][S15]; Gmsh GPL process-only [S12] |
| CGNS | — | — | read/write via MLL if SU2 workflow needs it | — | No managed writer (Flagged) |
| VTK `.vtu/.vtp/.pvd` | yes (results) | yes (extractions) | — | — | ParaView-native; BSD (Flagged) |
| OpenFOAM time dirs / `postProcessing` | yes (results) | case setup yes | — | — | [S15][S32] |
| SU2 history CSV / VTU | yes | config yes | — | — | [S17] |
| Tecplot / EnSight / Fluent | — | — | read if a user brings them | write | Proprietary or redundant |
| HDF5 (PureHDF) | yes | yes (sidecars) | — | — | MIT, pure managed [S19] |
| Parquet (Parquet.Net) | yes | yes (sweep tables) | — | — | MIT, .NET 8/10 [S20] |
| Zarr / NetCDF | — | — | if cloud/remote runs appear | — | No need while HDF5 covers it (Flagged) |

## Open questions and domain failure modes

**Open (cheapest next probe in parentheses):**
- Lednicer lower-block layout and LE duplication across the UIUC archive (download `coord/*.dat`, classify by count-line presence, assert the invariants in §Data — one script, one afternoon).
- UIUC redistribution terms (email m-selig@illinois.edu; meanwhile bundle only computable/independently published sections).
- Whether Fusion/Mastercam/PowerMill/Vectric import a bare `B_SPLINE_SURFACE_WITH_KNOTS` as a usable surface body and whether they need `ADVANCED_FACE`/shell (SPIKE-02: write one lofted surface three ways — bare, face, closed shell — and open each in Fusion; measure chord at three stations).
- STEP degree handling: do the target CAM systems keep degree-5 surfaces or degree-reduce (same spike, add a quintic sample).
- XFLR5 XML exact schema and twist axis (export one plane from XFLR5 and open the file).
- Shape3d `.s3d` binary vs text and `.s3dx` schema (obtain a sample file; ask Shape3d).
- ESI-OpenFOAM `force.dat`/`moment.dat` columns (open `postProcessing/` of one v2406 run).
- 3MF spec licence and lib3mf C# binding maturity (read the spec repo LICENSE; build lib3mf on macOS arm64).
- CGNS licence text (open `CGNS/CGNS` `license.txt`).
- OpenVSP `.vsp3` XML structure and licence text (open one `.vsp3` and the OpenVSP LICENSE).
- A pure-.NET JCS implementation to adopt vs own (search NuGet; if none, own ~150 lines with a fixture set from the RFC's test vectors).

**Failure modes:**
- *Silent:* Lednicer read as Selig (plausible wrong shape); STL/OBJ in the wrong unit (1000× scale, "opens fine"); `Sref` in an `.avl` disagreeing with geometry; `G17` noise producing meaningless Git diffs; a hash computed over pretty-printed bytes that changes when indentation changes; NaN written as `null` without a status field, later read as "zero".
- *Expensive:* a STEP that opens as a bag of surfaces the CAM operator must sew (hours per part); a 4-hour CFD run whose fields were embedded in a project file that then cannot be opened; a schema major bump without a migration and a `.bak`.
- *Irreversible:* overwriting the original `.dat` bytes with the normalised copy (provenance lost); rewriting `recipe` after a manual edit (one-way burst violated); dropping unknown keys from a newer-minor file on save (EXP-01 forbids).

## Disconfirming views sought

1. *"Selig vs Lednicer detection is trivial — just check whether the first numeric line's values exceed 1."* Held mostly, but the observed Lednicer file starts its upper block at `0.00001 −0.00005` and Selig files may start at `1.0000000 0.0000000`; a Selig file with a slightly-over-1 TE (`1.00003`) would defeat a naive `>1` test, and a Lednicer count line like `2. 2.` would pass a `>1.5` test but fail the minimum-count rule. The two-block structural test is required, not optional. Finding 1 stands, strengthened.
2. *"OpenVSP proves single-entity STEP export is enough."* Partly overturned: OpenVSP's own notes distinguish untrimmed surfaces from trimmed watertight BREPs [S34]; the precedent proves surface export, not CAM-ready solids. Finding 6 revised accordingly (see Contradiction below).
3. *"Use `JsonSerializer` defaults; .NET already writes round-trippable doubles, so the bytes can be hashed."* Round-trip held (bit-exact in the probe); hashing the bytes fell: .NET's exponent/negative-zero forms differ from JCS, pretty-printing changes bytes, and reflection serialisation is off by default in .NET 10 file-based apps. Finding 12/13 stand.
4. *"KiCad-style fixed decimals are better than shortest round-trip for Git."* Real trade-off: fixed decimals give stable column widths and no exponents but quantise (KiCad accepts a 1 nm floor). Shortest round-trip is exact and still readable for values in [1e-4, 1e15) (probe: `0.0001` prints without exponent; `2.5E-05` does). Recommendation: shortest round-trip **plus** a writer rule that geometry values are stored in metres with magnitudes that avoid exponents in practice; revisit if diffs prove noisy.
5. *"meshio makes mesh I/O a solved problem."* It does for Python, but it is a Python dependency last pushed 2024-07-23 [S31] and the product must not embed CPython; the text grammars we need (`.su2`, `polyMesh`, `.vtu`, STL, 3MF) are small enough to own. Finding 9 stands.
6. *"HDF5 is overkill; keep everything in JSON."* JSON cannot carry a 50-million-cell field or a NaN; PureHDF is MIT and pure managed [S19], so the cost is low. Stands, with Parquet for tabular sweeps.

**Contradicts existing repo knowledge.** `bench-geometry-kernel-decision` and `bench-cad-ux` state that "OpenVSP uses STEPcode to write AP203 files that currently contain only `B_SPLINE_SURFACE_WITH_KNOTS` entities" and treat this as the decisive precedent for CNC-ready export. OpenVSP's 3.21.0/3.21.2 release notes [S34] show that since 2020 OpenVSP exports **trimmed** STEP/IGES that "can form a watertight BREP solid" as well as untrimmed surfaces, and warns that untrimmed files avoid "slivers, gaps and holes" while the shell form is "not a single watertight BREP". Both claims can be true (the surface entity is still the only *geometry* entity; trimmed export adds topology entities), but the repo's inference "one entity type is enough for CAM" is **not established**: EXP-02's open-and-measure gate must test a face/shell form, not only bare surfaces. Confidence: Verified for the OpenVSP behaviour (release notes), Inferred for the CAM requirement.

## Glossary terms

- **Selig format** — airfoil `.dat` layout: name line, then points from the upper-surface TE forward over the upper surface, around the LE, aft along the lower surface to the TE. *(Verified, [S1][S3])*
- **Lednicer format** — airfoil `.dat` layout: name line, a count line `NU. NL.`, blank, upper surface LE→TE, blank, lower surface LE→TE. *(Verified header/first block, Inferred second block, [S3])*
- **B_SPLINE_SURFACE_WITH_KNOTS** — STEP entity: B-spline surface with explicit distinct knots and multiplicities in u and v; rational form via the `RATIONAL_B_SPLINE_SURFACE` complex entity. *(Verified, [S8])*
- **Trimmed vs untrimmed export** — OpenVSP terms: untrimmed = whole parametric surfaces (no slivers/gaps); trimmed = surfaces cut to intersections that can form a watertight BREP solid. *(Verified, [S34])*
- **JCS (RFC 8785)** — JSON Canonicalization Scheme: sorted keys by UTF-16 code units, ECMAScript number serialisation, no whitespace; the input to a content hash. *(Verified, [S21])*
- **Shortest round-trip formatting** — the fewest decimal digits that parse back to the identical IEEE-754 double; .NET Core 3.0+ default. *(Verified, [S22][S36])*
- **Physical group / marker / patch** — the boundary-naming concept of Gmsh / SU2 / OpenFOAM respectively; the hook by which boundary conditions attach to mesh faces. *(Verified, [S12][S13][S15])*
- **polyMesh** — OpenFOAM's face-addressed mesh directory: `points`, `faces`, `owner`, `neighbour`, `boundary`. *(Verified, [S15])*
- **Function object** — an OpenFOAM run-time post-processing plugin (e.g. `forces`) writing to `<timeDir>/…dat`. *(Verified, [S32])*
- **Sidecar** — a separately stored binary artifact (HDF5, Parquet, VTU) referenced from the JSON document by path and SHA-256. *(Inferred)*
- **Recipe (authoritative flag)** — generative parameters that emit stations; authoritative until the first manual edit, then provenance only. *(Verified repo intent, [S42])*
- **CST (Kulfan) parameter set** — class exponents N1, N2, upper/lower Bernstein weights, LE modifier weight, TE thickness, plus the sampling law. *(Verified, [S5])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | UIUC Airfoil Coordinates Database — format notes | primary | https://m-selig.ae.illinois.edu/ads/coord_database.html | 2026-09-20 | Selig ordering, `#` comments, T/F pitfall, count |
| S2 | UIUC Airfoil Data Site (home) | primary | https://m-selig.ae.illinois.edu/ads.html | 2026-09-20 | Copyright notice; absence of licence text; archives |
| S3 | UIUC `coord/e817.dat` and `coord_seligFmt/e817.dat` (downloaded) | primary (data) | https://m-selig.ae.illinois.edu/ads/coord/e817.dat ; …/coord_seligFmt/e817.dat | 2026-09-20 | Observed Lednicer vs Selig layouts |
| S4 | AeroSandbox `airfoil.py` | primary (source) | https://raw.githubusercontent.com/peterdsharpe/AeroSandbox/master/aerosandbox/geometry/airfoil/airfoil.py | 2026-09-20 | Ordering convention, `write_dat`, `normalize` |
| S5 | AeroSandbox `airfoil_families.py` | primary (source) | https://raw.githubusercontent.com/peterdsharpe/AeroSandbox/master/aerosandbox/geometry/airfoil/airfoil_families.py | 2026-09-20 | Regex parser, no Lednicer handling, CST signature |
| S6 | AVL 3.40 user guide | primary | https://web.mit.edu/drela/Public/web/avl/avl_doc.txt | 2026-09-20 | `.avl` grammar and semantics |
| S7 | OpenVSP wiki — airfoil export | primary | https://openvsp.org/wiki/doku.php?id=airfoilexport | 2026-09-20 | Selig `.dat`, `.bz`, CSV metadata |
| S8 | STEP Tools AIM — `b_spline_surface_with_knots` | secondary (standard mirror) | https://www.steptools.com/stds/stp_aim/html/t_b_spline_surface_with_knots.html | 2026-09-20 | EXPRESS attributes, where-rules, rational form |
| S9 | rhino3dm README | primary | https://github.com/mcneel/rhino3dm | 2026-09-20 | MIT, 3DM only, platforms, v8.17 |
| S10 | STEPcode README | primary | https://github.com/stepcode/stepcode | 2026-09-20 | BSD-3, Part 21, EXPRESS, provenance |
| S11 | 3MF Core Specification | standard | https://github.com/3MFConsortium/spec_core/blob/master/3MF%20Core%20Specification.md | 2026-09-20 | Units, container, manifold rules |
| S12 | Gmsh reference manual — MSH file format | primary | https://gmsh.info/doc/texinfo/gmsh.html#MSH-file-format | 2026-09-20 | Sections, physical groups, no units, GPL |
| S13 | SU2 docs — Mesh File | primary | https://su2code.github.io/docs_v7/Mesh-File/ | 2026-09-20 | `.su2` grammar, VTK codes, CGNS input |
| S14 | CGNS.org | primary | https://cgns.org/ | 2026-09-20 | SIDS/MLL, AIAA RP, BCs and units |
| S15 | OpenFOAM user guide 4.1 Mesh description | primary | https://www.openfoam.com/documentation/user-guide/4-mesh-generation-and-conversion/4.1-mesh-description | 2026-09-20 | polyMesh files and ordering rules |
| S16 | meshio README | primary | https://github.com/nschloe/meshio | 2026-09-20 | Format list, MIT |
| S17 | SU2 docs — Custom Output | primary | https://su2code.github.io/docs_v7/Custom-Output/ | 2026-09-20 | HISTORY_OUTPUT, OUTPUT_FILES, defaults |
| S18 | HDF5 LICENSE | primary | https://raw.githubusercontent.com/HDFGroup/hdf5/develop/LICENSE | 2026-09-20 | BSD-3-style terms |
| S19 | PureHDF README | primary | https://github.com/Apollo3zehn/PureHDF | 2026-09-20 | MIT, pure C#, read/write, .NET 8+ |
| S20 | Parquet.Net README | primary | https://github.com/aloneguid/parquet-dotnet | 2026-09-20 | MIT, .NET 8/10, features |
| S21 | RFC 8785 JSON Canonicalization Scheme | standard (Informational) | https://www.rfc-editor.org/rfc/rfc8785 | 2026-09-20 | Canonical form for hashing |
| S22 | MS Learn — Standard numeric format strings | primary | https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings | 2026-09-20 | R vs G17, shortest round-trip default |
| S23 | KiCad dev docs — S-expression intro | primary | https://dev-docs.kicad.org/en/file-formats/sexpr-intro/ | 2026-09-20 | Git-friendly file design precedent |
| S24 | MachUpX — Creating input files | primary | https://machupx.readthedocs.io/en/latest/creating_input_files.html | 2026-09-20 | Wing/airfoil JSON grammar |
| S25 | Paul Bourke — OBJ specification | secondary (spec mirror) | https://paulbourke.net/dataformats/obj/ | 2026-09-20 | Free-form curve/surface statements, no units |
| S26 | OpenUSD API — UsdGeomNurbsPatch | primary | https://openusd.org/release/api/class_usd_geom_nurbs_patch.html | 2026-09-20 | USD NURBS attributes |
| S27 | JSON Schema — Specification | primary | https://json-schema.org/specification | 2026-09-20 | 2020-12 current, Core/Validation |
| S28 | XFLR5 SourceForge threads; xflr5-xml-generator | tertiary | https://sourceforge.net/p/xflr5/discussion/679398/thread/7db3809b/ ; https://github.com/juan-g-bonilla/xflr5-xml-generator | 2026-09-20 | XML/`.xwimp` existence and fields |
| S29 | Shape3d FAQ / V8→VX page; file-extension notes; Shape3d→XFLR5 tutorial (PDF, not text-extractable) | tertiary | https://www.shape3d.com/support/faq.aspx ; https://www.shape3d.com/products/FromV8toVX.aspx ; https://www.shape3d.com/Manuals/Tuto%20Shape3d%20Xflr5%20EN.pdf | 2026-09-20 | `.s3dx` XML, V8 loss, XFLR5 path |
| S30 | crates.io API — stl_io, ruststep, truck, hdf5-metno, serde, parquet, arrow | primary (registry) | https://crates.io/api/v1/crates/<name> | 2026-09-20 | Licences, versions, dates |
| S31 | GitHub API — licence/pushed_at for CadQuery, build123d, openscad, OpenVSP, SU2, meshio, seacas, HDF.PInvoke, VTK, lib3mf, MachUpX, AeroSandbox, stepcode, CGNS | primary (registry) | https://api.github.com/repos/<owner>/<repo> | 2026-09-20 | SPDX ids where detected; activity |
| S32 | OpenFOAM-12 `forces.H` | primary (source) | https://raw.githubusercontent.com/OpenFOAM/OpenFOAM-12/master/src/functionObjects/forces/forces/forces.H | 2026-09-20 | forces dictionary and output files |
| S33 | CFD Direct OpenFOAM v12 user guide — ParaView | primary | https://doc.cfd.direct/openfoam/user-guide-v12/paraview | 2026-09-20 | paraFoam, PVFoamReader |
| S34 | OpenVSP 3.21.0 / 3.21.2 release announcements; OpenVSP Google Group threads | secondary | https://openvsp.org/blogs/announcements/2020/04/11/openvsp-3-21-0-released ; https://openvsp.org/blogs/announcements/2020/07/28/openvsp-3-21-2-released ; https://groups.google.com/g/openvsp/c/DbjFIcY6wk8 | 2026-09-20 | Trimmed vs untrimmed STEP/IGES |
| S35 | OpenVSP issue #195 Trimmed Surfaces export bug | tertiary | https://github.com/OpenVSP/OpenVSP/issues/195 | 2026-09-20 | Trimming defects exist in practice |
| S36 | Spike: `.NET 10.0.7` probe `spike-json/app.cs` (scratchpad; disposable) | primary (executed) | scratchpad `spike-json/app.cs`, run with `dotnet run app.cs` | 2026-09-20 | Double formatting, JSON numbers, NaN, reflection-off default |
| S37 | ISO 10303 Part 21 / AP203-214-242 (not opened; recall) | standard | — | — | Flagged claims only |
| S38 | Fusion/Mastercam/PowerMill/Vectric import documentation (not opened) | vendor | — | — | Flagged claims only |
| S39 | Repo: `docs/knowledge/sources/bench-parametric-geometry.md.txt` | repo | — | 2026-09-20 | Station grammar, derive-never-store |
| S40 | Repo: `docs/specs/cfd-workbench.md` A3–A5 | repo | — | 2026-09-20 | Domain model, DOC/CAT/EXP/ANA stories |
| S41 | Repo: `docs/knowledge/sources/bench-geometry-kernel-decision.md.txt` | repo | — | 2026-09-20 | Permissive path, OpenVSP precedent |
| S42 | Repo: `docs/knowledge/sources/proposal-sequence.md.txt` §3.2–3.5 | repo | — | 2026-09-20 | `.cfdw.json` sketch, library table |
| S43 | Repo: `docs/knowledge/sources/bench-gap-register.md.txt` GAP-04/08/09 | repo | — | 2026-09-20 | Units, data model, parser defect |
