---
id: design-export
title: "Design: Export (Area 7) - section .dat, wing STL, then 3MF"
type: design
status: proposed
owner: "@timianmalloo"
phase: design - Export slice A (Ruling 193), revised for Rulings 194 and 195
tags: [export, dat, stl, 3mf, exp-02, exp-03, te-floor, fidelity, millimetres, watertight, area-7, trk-exd, trk-exr]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: implements }
  - { to: design-language, rel: depends-on }
  - { to: design-m12d-catalog, rel: relates-to }
  - { to: mockup-export, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Detailed design of the first Export slice: the section .dat (Selig or Lednicer), the wing STL (whole wing or a starboard
  half with a root cap), then the 3MF, all in millimetres and unscaled where a unit exists. Every geometric export shows an
  always-on trailing-edge row (least thickness, where, the floor labelled "app default, no source", manufacturing not
  assessed). Revision 2 folds in Rulings 194 and 195 and the manufacturing review: seven committed probe rungs for each of four
  wings, whole and half, all closed (Euler 2); the "deviation" term for mesh and .dat fidelity; build conditions B1-B9, each
  with a red-first test. Final copy and spec amendments are one table for the operator; nothing is built until approved.
---

# Design: Export - section .dat, wing STL, then 3MF

Scope is Ruling 193 (`docs/notes/rulings.md`, "Ruling 193"): section `.dat` and wing STL first, then 3MF, within EXP-02
and EXP-03 of `docs/specs/cfd-workbench-v1.md` (`:1217`, `:1218`). STEP, 3DM and Fusion stay out (they wait on the kernel
decision and open-and-measure evidence). **Design only. No `src/` or `tests/` change.** Copy and spec amendments are
proposals for the operator (section 14). The companion mockup is [`export.html`](../mockups/export.html).

**Revision 2 (trk-exr, 2026-10-09).** The operator approved the mockup visually and answered the five questions
(Ruling 194), and relabelled the trailing-edge floor and added an always-on trailing-edge row (Ruling 195). The
manufacturing-CAM review returned PASS-WITH-CONDITIONS; its design fixes are in sections 4, 5, 7, 8 and 14, and its build
conditions are in section 12. The probe was re-run from committed source (section 3.3).

Goal state. **Goal:** settle what each file contains, where the geometry comes from, how the user drives it and what
every hard state says, so the build can start on a visual yes. **Done when:** this note and the mockup are committed, the
build size and the kernel question are answered with evidence, and the operator has section 14 and the three items in
section 11. **Not in scope:** STEP, 3DM, Fusion, AVL, chart export, mould or CAM output, process-specific trailing-edge
floors (a later slice), alignment features and connectors on the half wing, a batch "every station" export. **Tier:** T2
(new user-facing surface, new file writers).

Confidence labels: **Verified** = observed in code or by running the probe in `docs/proof/exd/`; **Inferred** = reasoned,
not observed; **Assume** = marked `assume:` with what confirms it.

Terms. "Deviation" is the measured distance between a written mesh or point set and the app's computed curve or surface
(the EXP-02 term). "Gap" is used only for the trailing-edge gap (the thickness at the trailing edge).

## 1. Decisions in one table

| # | Decision | Basis |
|---|---|---|
| D1 | One modal dialog, **File > Export...** (palette entry too), format list on the left, that format's options on the right, a fixed "What will be written" block, the findings, then Cancel and **Export...**. Choosing Export... opens the native save panel; the file is written after the panel. | spec `:2198` (Export is a modal dialog), F5 `:1651`; panel as in `MainWindow.axaml.cs:157-165` |
| D2 | Section menu gets **Export .dat...** (next to Import .dat..., `CommandTable.cs:150`); it opens the same dialog on the .dat format at the selected station. | symmetry with Import |
| D3 | Export reads the **accepted** revision, never an open draft, and only when the accepted geometry check has passed. | `WorkbenchController.cs:559-561`; `AnalysisService.cs:94-95` |
| D4 | The wing STL/3MF is a **closed solid**: skin from the display evaluator at a tolerance the user picks (three presets), plus a topology-only closing step (tip caps, trailing-edge strip when open, root weld by mirror). Scope is the whole wing or a starboard half closed with a root cap. No CAD kernel. | section 3, probe |
| D5 | Unit is **not a choice**: STL and 3MF are millimetres, unscaled (EXP-03). F5's "Choose format, unit and tolerance" becomes "format and tolerance". | spec `:1218`, `:1655` |
| D6 | Fidelity is **measured and labelled as measured**: the deviation is the app's own sampled surface, sampled at cell midpoints, not a certificate. The geometry certificate itself says it has no export certificate. | `Geometry.cs:36` |
| D7 | **Trailing-edge row, always (Ruling 195).** Every geometric export shows, in "What will be written", the least trailing-edge thickness in mm, where it is, the floor with its label "app default, no source", and "Manufacturing: not assessed (no process chosen)". The finding band (spec string, with a jump) appears **only** when the thickness is below the floor, as an advisory that never blocks. | Rulings 194 (5) and 195; spec `:633`, `:1190`, `:2302` |
| D8 | Result goes to the **status strip**, failure stays **in the dialog**; the earlier file at the same path is never touched on failure (temp file, then rename). | F5 `:1660-1661`; `ProjectStore.cs:226` |

Operator answers recorded (Ruling 194): (1) a `.dat` holds the section at a chosen station by default, the authored profile
second; (2) presets Draft 0.05, Print 0.02 (default), Fine 0.005 mm, with a large-mesh warning above 500,000 triangles; (3)
whole wing and a starboard half with a root cap; (4) a `cfdw export` CLI verb; (5) a TE below the floor is advised only, and
export into OneDrive folders is allowed.

## 2. What exists today (Verified)

- There is **no export writer** anywhere in `src/` (grep for `Selig`, `Lednicer`, `Stl`, `Export` finds only the importer
  `DatImport.cs`, `Provenance.cs:94`, `SectionReplace.cs:301` and unrelated hits). The importer only reads, and returns
  chord-normalised points (`DatImport.cs:20-58`).
- The command table has no Export row (`CommandTable.cs:73-78` lists New, Open, Save, Save As, Close). `Section > Import .dat...`
  is row `section.import-dat` (`:150`).
- File I/O uses Avalonia's `StorageProvider` pickers (`MainWindow.axaml.cs:161`, `ShellHost.cs:1393`), and a failed save
  is worded in the status strip and rethrown (`MainWindow.axaml.cs:176-189`).
- The TE floor default is **0.3 mm** (`Settings.cs:29`, the last `RunSettings` argument; field `TeFloorMm`,
  `RunRecord.cs:52`, hashed at `RunRecord.cs:305`). It carries **no label in code**: a grep of `src/` for "practitioner" finds only
  the cavitation margin label (`Labels.cs:99`, `Cavitation.cs:29`), not this floor. The label "practitioner value, unverified"
  lives only in the spec (`:1035`, `:2302`). No finding for "TE below the floor" exists in code yet; the TE gap is a section
  readout (`SectionModel.cs:11,17`, shown at `PropertiesView.cs:1253`).
- The revision label is `r<ordinal>` from `AuthoringSession.RevisionOf` (`AuthoringSession.cs:2003-2010`), shown by
  `AnalysisProjection.cs:159`.

## 3. Source of the geometry, and the kernel question

**Answer: a watertight solid is available without a CAD kernel.** It needs one closing step that is pure topology.

### 3.1 The evaluators the export reads (Verified)

| Need | Reader | Where |
|---|---|---|
| Wing skin | `Placement.Surface(source, basis, generation, ct, stations, chordSamples)` returns `SurfaceView`: sections, each with `Upper` and `Lower` `Point3` lists in metres, on a cosine chord grid and a uniform-eta grid plus the authored stations | `Placement.cs:304-332`, `:694-720`; record `Placement.cs:13-17` |
| What the 3D view draws | the same call, default 41 stations x 101 chord points; the renderer applies the port mirror and the camera "and nothing else" | `WorkbenchController.cs:531-532`; `SurfaceRenderer.cs:15-17`; `View3d.cs:252` |
| Section at a station | `Placement.Sections(source, etas, xs, ct)` returns camber and thickness in chord units at the given x; `Placement.Frame` gives chord, twist, t/c | `Placement.cs:334-371` |
| Own profile | `ProfileEvaluator.Samples(curve, n)`, cosine spacing, but **internal** | `Placement.cs:217-228` |
| Geometry check | `Geometry.Assess(parsed)`; status `Certified` required | `Geometry.cs:320-339` |

The mirror is not optional: the grammar requires `symmetry mirror_y` (`FoilSource.cs:1376`), so the port half is the
starboard mesh through `Point3.Port()` (`Placement.cs:7`).

### 3.2 Why the display mesh is not yet a solid (Verified)

- The tip is "the open planar end at the last authored station" in v1 (spec `:805`; `Geometry.cs:337-339` only assesses
  `Tip == "open"`). Every point of the last section has the same `y` (`Placement.cs:432-448`), so the tip is a planar
  outline with no cap triangles.
- `Closure` defaults to `closed` (`FoilSource.cs:1330`), where the two surfaces meet at the trailing edge; with
  `closure open` they leave a gap and the skin has a slit.
- The display mesh has no triangles for the tip, the slit or any root closure; it is a shell.

### 3.3 The closing step, and the probe (Verified by running it)

The closing step adds no geometry: (1) weld vertices that are bit-identical (the leading edge, the root section shared by
the two halves, a closed trailing edge); (2) when the trailing edge is open, one strip of two triangles per span cell
joining `Upper[last]` to `Lower[last]`; (3) each tip outline gets a cap by zipping `Upper[j]`, `Lower[j]` (the section is
planar and x-monotone in the chord frame); (4) a **starboard-half** export caps the root plane the same way, with the
opposite winding, so the root face lies in the plane y = 0.

`docs/proof/exd/probe-wing-mesh.cs.txt` is the probe (a scratch console project referencing `CfdWorkbench.Core`). It builds
the whole-wing and the starboard-half closed mesh from `Placement.Surface` at each of **seven** rungs, checks every directed
edge has exactly one opposite mate, computes Euler's V-E+F and the signed volume, checks that every root point of the half
has y exactly 0, finds the least trailing-edge thickness and where, and measures the deviation against a 2x refined surface.
**Revision 2 re-ran it from the committed source for each wing** (commands and the corrections to the first receipts are in
`docs/proof/exd/probe-commands.txt`). The first receipts were not reproducible from the committed source: the source ran six
rungs ending at 161x401 while the design and mockup claimed seven, and two output files came from an uncommitted variant.
Observed (`probe-open.txt`, `probe-open14.txt`, `probe-closed.txt`, `probe-default-wing.txt`; every rung of every wing,
whole and half, reports `manifold=True euler=2` and a positive volume):

| Wing | TE | Rungs (stations x chord pts) | Whole wing triangles | Starboard half triangles | Edge-manifold | Euler |
|---|---|---|---|---|---|---|
| Example foil (450 mm half span, 120 mm chord) | open, 0.26 mm | 11x26 ... 321x801 (all seven) | 2,138 ... 2,052,478 | 1,118 ... 1,027,838 | yes (every rung) | 2 |
| Example foil | open, 0.36 mm | all seven | 2,138 ... 2,052,478 | 1,118 ... 1,027,838 | yes | 2 |
| Example foil as shipped | closed | all seven | 2,096 ... 2,051,196 | 1,096 ... 1,027,196 | yes | 2 |
| Untitled NACA 0012 wing (`FoilSource.NewDefault`, 500 mm half span) | closed | all seven | 2,096 ... 2,051,196 | 1,096 ... 1,027,196 | yes | 2 |

Euler 2 means one closed genus-0 surface; a positive signed volume means outward-facing normals with the winding used. The
whole-wing volume is twice the half volume to within 0.001 cm3 at every rung. The half's root face is planar at y = 0
(`rootCapPlanarAtY0=True`). `Geometry.Assess` returned `Certified` for all four wings (`status=` line of each file).

**Consequence for the build.** STL needs the closing step, about 60 lines of topology code (Inferred) plus an edge-use check that
becomes a **post-condition of every write**, run on the bytes as written (B2). It needs no kernel, so the kernel decision
does not block this slice. It does need a small Core change: `Placement.Surface` only offers uniform grids (stations and chord
count), so the tolerance ladder in 4.2 calls it per rung; a new public Core member is also needed for the "Own" profile
(4.1) because `ProfileEvaluator` is internal.

**What the probe does not prove (Inferred).** It checks the mesh in memory with exact double coordinates. The written STL is
binary32, where two distinct doubles can round to one float and a `-0.0` can differ in bits from `+0.0`; B2 closes that gap. No
slicer or CAD program has opened any of these meshes yet; B1 closes that gap. A wing whose last authored section is not planar
in `y` should not arise (the evaluator sets `y` per section), and a foil with a closed tip is `Unsupported`
(`Geometry.cs:337-339`) and refused before export.

## 4. What each format writes (Functional)

### 4.1 Section `.dat`

| Item | Decision |
|---|---|
| Which section | The user picks **At station** (default) or **Own**. **At station:** camber plus and minus half the thickness, measured **vertically** at the same chord x, with the thickness rescaled so its **peak equals the station t/c** (`Placement.cs:84-98`); it is the section the wing builds at that station, before twist, chord scale and elevation. **Own:** the authored upper and lower curves as drawn, **unscaled** (`ProfileEvaluator.Samples`, `Placement.cs:217-228`). Neither applies thickness normal to the camber line. A section's authored peak is not its t/c, so the two files differ whenever those differ (computational-geometry review, `docs/proof/exd/section-thickness-expert.txt`). The vocabulary "own" and "at station" is the section editor's (`m12c-section-editor.md:94`). One section per file. |
| Station | The authored stations (the sample has Root and Tip), so "Own" always has an authored profile. Default: the station selected in the editor, else Root. |
| Ordering | **Selig** (default): trailing edge upper, along the upper surface to the nose, then along the lower surface to the trailing edge; the nose point appears once. **Lednicer**: a header line with the two counts, then upper nose to tail, a blank line, lower nose to tail. Both are read by `DatImport.cs:95-142`, so a written file re-imports. |
| Points | Per surface N = 61, **101** (default) or 201. Selig file has 2N-1 points (121, 201, 401). Spacing is the cosine law already used by the evaluator, x = (1-cos(pi i/(N-1)))/2 (`Placement.cs:707-715`), nose-dense. The importer needs at least 10 points (`DatImport.cs`, `points.Count < 10`), met. The x grid must equal the STL chord grid exactly (the build asserts it, B9). |
| Trailing edge | As built: closed gives equal first and last point (both written); open gives two distinct end points. Nothing is closed or rounded. The trailing-edge row (D7) is computed **from the points as written** (last upper minus last lower, times the chord), so it matches the file for both "At station" and "Own". |
| Coordinates | x/c and y/c, dimensionless, chord frame, **twist not applied**, no chord scaling, no elevation. Digits: shortest round-trip, in **positional notation, never exponent form** (B5), invariant culture, `.` decimal, `-0` written as `0`, `\n` line ends, UTF-8 no BOM. The first probe wrote `3.585447714271229E-05` (`probe2-default-root.txt`, row 2); a positional writer writes `0.00003585447714271229`. |
| Name line | `<foil name> | <station> | r<n>` with control characters and anything past 80 characters removed (the importer's fixture "a .dat name line containing instructions", spec `:1318`, is the matching risk). **The name line never begins with two numbers** (B6): if its first two whitespace-separated tokens both parse as numbers it is prefixed with `foil ` so a reader cannot take it for a point or the Lednicer counts. `#` comment lines are not written (other tools reject them). |
| Real rows | Selig, 101 per surface, Example foil root, open TE, At station: row 1 `1 0.0010655364151166689`; row 2 `0.9997532801828658 0.0010892048507038603`; row 101 `0 0`; row 102 `0.0002467198171342 -0.0024037402789592885`; row 201 is the lower trailing edge (`probe2-open-root.txt`). |

### 4.2 Wing STL

| Item | Decision |
|---|---|
| Scope | **Whole wing** (default; starboard + port, welded at the root) or **Starboard half** (the skin plus a flat root face in the plane y = 0). The half is a **root split, a bonding aid and a datum**. Further cuts, and connectors, are done in the slicer; alignment features are out of scope. No "one surface": an open shell is not a print solid. |
| Frame and unit | The authored coordinate frame (x aft, y span, z up), metres from the evaluator times 1000. **Millimetres, unscaled, not re-oriented** (EXP-03). |
| Tolerance | Three presets: **Draft 0.05 mm**, **Print 0.02 mm** (default), **Fine 0.005 mm**. The writer walks a fixed ladder of seven grids (stations x chord points: 11x26, 21x51, 31x76, 41x101, 81x201, 161x401, 321x801) and takes the first rung whose **measured** largest deviation is at or under the tolerance. The dialog shows the rung's triangle count, file size and measured deviation before writing. If even the finest rung misses the tolerance the dialog says so (EX31) and offers a coarser choice. `simplify:` uniform ladder; ceiling is wings whose curvature is local (the Untitled wing needs a 4x finer rung than the Example foil because the planform curves at the tip); upgrade trigger is any real wing past 500,000 triangles at Print, then refine adaptively near the tip. |
| Watertightness | Always closed: the write refuses if the edge-use check fails on the bytes as written (B2). Counts of triangles, vertices and the check result are in the summary. |
| Normals | Facet normal computed per triangle from its vertices, unit length, outward (winding verified by positive signed volume in the probe). |
| Format | **Binary** STL only: 80-byte header that does not begin with `solid`, a 32-bit count, 50 bytes per triangle. ASCII is not offered (about four times larger; every slicer reads binary). Header text: `CFD Workbench <foil slug> r<n> mm`. Coordinates are binary32, written from the welded vertex table with `-0.0` turned into `+0.0` (B2); at 450 mm the rounding step is 2^-15 mm = 0.00003 mm (computed), far below the tolerance. |
| Name | `<foil-slug>-r<n>-mm.stl` (EXP-03: unit in the file name), for example `basic-foil-r12-mm.stl`. A starboard half adds `-half`: `basic-foil-r12-half-mm.stl`, so the two scopes cannot overwrite each other. Slug from `DatImport.Slug` (`DatImport.cs:15`). |
| Size at the presets (Verified, probe) | Whole wing, Example foil (both open TEs): Draft 8,278 triangles, 0.41 MB, deviation 0.0325 mm; Print 18,418, 0.92 MB, 0.0145 mm; Fine 129,118, 6.46 MB, 0.0020 mm. Starboard half, same foil: Draft 4,238, 0.21 MB; Print 9,358, 0.47 MB; Fine 64,958, 3.25 MB. Whole wing, Untitled: Draft 32,396, 1.62 MB, 0.0450 mm; Print 128,796, 6.44 MB, 0.0113 mm; Fine 513,596, 25.68 MB, 0.0028 mm. Starboard half, Untitled: Draft 16,396, 0.82 MB; Print 64,796, 3.24 MB; Fine 257,596, 12.88 MB. Building a mesh takes 5-49 ms at these rungs (`surfMs` in the probe), so the dialog can recompute on every tolerance change. |

### 4.3 Wing 3MF (second increment)

Same mesh, scope, tolerance, closing step and checks as the STL. The differences:

- Package: a ZIP (`System.IO.Compression`, in the base library) with `[Content_Types].xml`, `_rels/.rels` and
  `3D/3dmodel.model`; one `<object type="model">` with one `<mesh>`, one `<build><item>`.
- Unit: `<model unit="millimeter">` and coordinates in mm, unscaled (EXP-03 "3MF attribute"). `assume:` the 3MF Core
  specification names `millimeter` as the value and counter-clockwise outward winding; confirm by the slicer fixture (B1)
  before the 3MF build. Breaks if false: a part that prints at the wrong scale or inside out.
- Metadata (allowed keys, proposed): `Title` = foil name; `Description` = `Revision r<n>, tolerance <t> mm, measured deviation
  <d> mm`; `Application` = `CFD Workbench <version>`. Never the user name, the path or the machine name (spec A8.5 export
  rules, `:1353`, `:1355`).
- Name: `<foil-slug>-r<n>.3mf`, half `<foil-slug>-r<n>-half.3mf` (the unit is in the attribute, so `-mm` is not needed).
- It ships as the second increment of this slice (Ruling 193 order). The dialog design includes it so the choice list
  does not change later.

## 5. Fidelity statement (EXP-02)

Shown in the dialog before the write and again in the status strip after it. Each line has its number and unit.

| Format | Lines shown |
|---|---|
| .dat | Units: fractions of chord (x/c, y/c); chord at this station 120.0 mm; twist not applied. Points: 201 (Selig). Revision: r12, accepted. Trailing edge: least thickness, where, the floor with its label, manufacturing not assessed (D7). Largest deviation between the curve and the straight lines joining the points: 0.0074 mm at this chord, sampled at the midpoints of the 200 segments (Verified for N=101: 7.40 um, `probe2-open-root.txt`). Limit: points lie on the app's computed curve; no other program has read them. |
| STL | Units: mm, unscaled. Revision: r12, accepted. Tolerance: 0.02 mm. Trailing edge row (D7). Largest measured deviation from the app's computed surface: 0.0145 mm, sampled at cell midpoints, not a bound. Closed: every edge joins two triangles (checked). Coordinates rounded to 0.00003 mm (binary32). Limit: "the computed surface" is the app's own evaluation (`Geometry.cs:36`: no export certificate); no other CAD system has opened it. Not strength-checked: the fixed safety string (A5.6). |
| 3MF | As STL, plus: unit attribute millimeter. |

The deviation is a **maximum over sampled midpoints at one refinement level**; the probe measures chordwise, spanwise
and diagonal midpoints of each cell against the surface at twice the resolution. It can under-read a narrow feature. The
label says "sampled" and "not a bound" for that reason, and B4 adds a dense-sample test on both probe wings. `LAB-01` words
("certified", "validated", "recommended", "optimized", "best") are not used.

## 6. UX

### 6.1 Entry points

| Entry | Detail |
|---|---|
| File > Export... | new row `file.export`, gesture Shift+Command+E (macOS) and Ctrl+Shift+E (Windows); free in `CommandTable.cs:73-164`. Disabled with a reason when no foil is open (COPY-EX03). |
| Command palette | every table row is a palette entry (`CommandTable.PaletteEntries`), so "Export..." is findable with no extra code. |
| Section > Export .dat... | new row `section.export-dat`; opens the dialog on .dat at the selected station. |
| CLI (Ruling 194 (4), approved) | `cfdw export <file> --format dat|stl|3mf [--shape at|own] [--station N] [--order selig|lednicer] [--points 61|101|201] [--scope whole|half] [--tolerance draft|print|fine|<mm>] --out <path>` beside `inspect` and `analyse` (`Cli/Program.cs:52-54`). It runs the same writers, prints the same summary lines including the trailing-edge row, and gives the build a headless test oracle. |

### 6.2 The dialog (see the mockup)

Left: the format list - Section (.dat), Wing (STL), Wing (3MF), and a disabled STEP row carrying the spec string
(`:2299`). Right: that format's options. Below the options a fixed block, **What will be written**, with the fidelity
lines and the always-on **Trailing edge** row. Then the finding band (only below the floor; the draft note), the fixed safety
string, and the buttons Cancel and **Export...** (the default button; Enter activates it; Escape cancels and returns focus to
the menu item that opened it). While the mesh is prepared the block shows its skeleton at the same size, including the
trailing-edge row, and Export... is disabled with "Preparing the mesh..." in place. With **Starboard half** chosen, one help
line under the tolerance states what the half is for (EX13c).

Keyboard (each needs a handler and a check at build): Tab order is format list, options, Cancel, Export...; the format
list is one tab stop with arrow keys (a listbox, so a roving index); Escape and Enter as above. The native save panel
keeps the platform's keys.

### 6.3 Flow (F5, amended)

1. User chooses File > Export... (or Section > Export .dat...).
2. Gate: no foil -> row disabled. Accepted geometry check not passed -> dialog opens with Export... disabled and the reason
   (H2). Otherwise continue.
3. The user picks a format and options. For STL/3MF the mesh is built off the UI thread for the chosen tolerance and the
   summary fills in (loading, then ready).
4. **Export...** opens the native save panel. Start folder: the project file's folder, else the default
   `CFD Workbench` folder (`w2-save-picker.md` S1). Suggested name per 6.4. The panel's own prompt covers replacing an
   existing file; the product adds no second prompt.
5. Write: build the bytes in memory, run the closure check **on those bytes** (B2), write to a temp file in the destination
   folder (`.cfd-<guid>.tmp`, as `ProjectStore.cs:226`), then rename over the target. On any failure delete the temp file; the
   earlier file at the path is untouched.
6. Success: the dialog closes, the status strip reads COPY-EX20 (STL, 3MF) or COPY-EX21 (.dat) with **Show in Finder** / **Show in Explorer**. Failure:
   the dialog stays open with COPY-EX34 and EX35, **Choose another place...** and **Try again** (COPY-EX36).

The extension is forced by the format; if the user types another extension the product writes the forced one. The export
can never target the project file (`.cfdw.json`).

### 6.4 File names

| Format | Suggested name | Example |
|---|---|---|
| .dat | `<foil-slug>-<station-slug>-r<n>.dat` | `basic-foil-root-r12.dat` |
| STL | `<foil-slug>-r<n>[-half]-mm.stl` | `basic-foil-r12-mm.stl`, `basic-foil-r12-half-mm.stl` |
| 3MF | `<foil-slug>-r<n>[-half].3mf` | `basic-foil-r12.3mf` |

Windows rule: Ruling 146's OneDrive refusal protects the project store and **does not extend to exports** (Ruling 194 (5)): an
export is a copy and the temp-then-rename write is safe in a synced folder. If a sync client holds the temp file and the
rename fails, that is the ordinary write-failure state (H6).

## 7. The hard states

| # | State | Behaviour | Mockup |
|---|---|---|---|
| H1 | Draft open (section editor or a gesture not yet applied) | Export proceeds on the accepted revision and says so: "Exporting revision r12. Your open draft is not included." Not a block. | `draft` detail (.dat, STL) |
| H2 | Geometry not accepted / check not passed (`Assess` is not `Certified`; the shape can still be drawn) | Dialog opens; Export... disabled; reason names the finding location (the Checks drawer) and the way out. Same gate as `AnalysisService.cs:94-95`. | `blocked` |
| H3 | Trailing edge (always shown; below the floor adds a band) | The **Trailing edge row** is in every summary: the least thickness in mm, where, "Floor 0.30 mm (app default, no source).", and "Manufacturing: not assessed (no process chosen)". Below the floor the band reads "Trailing edge 0.26 mm, below the floor of 0.30 mm (app default, no source)" with a jump. The jump lands on the trailing-edge gap control: "Show at tip" for a located value, "Show the trailing-edge gap" when the value holds along the whole span (B8). Advisory, never blocks. A closed trailing edge reads "0.00 mm along the whole span". For .dat the value is at that station, from the written points. | `ready`: A below the floor (tip), B meets it (y = 439 mm, no band), C closed (whole span) |
| H4 | Analysis mode active | Export reads the accepted revision exactly as in CAD mode (`WorkbenchController.cs:559-561`). The dialog adds one line: "Analysis layers and results are not exported." If the foil is drawn but the geometry check has not passed, H2 applies. There is **no state today where analysis runs on geometry export refuses**: both use the `Certified` gate. | `analysis` detail |
| H5 | Large mesh | At more than 500,000 triangles (25 MB binary STL) the summary shows a warning band and the button reads "Export anyway..."; the coarser preset is one click. The writer never refuses on size alone. **The line stays at 500,000** (B7): Ruling 194 (2) fixed it, no slicer load time is measured yet (B1 measures it, and the line is reviewed with that evidence), and the Untitled wing at Fine (513,596 triangles, 25.68 MB) sits just over it, so a real case exercises the state. At the top rung with a tolerance not met, the band says what was reached (COPY-EX31; not rendered). The starboard half halves the count (Untitled at Fine, half: 257,596, no warning). | `large` |
| H6 | Write failure (permission, disk full, path gone) | In-dialog error with plain cause, "Nothing was changed. The earlier file is still there.", **Choose another place...**, **Try again**. Reasons from `IOException`/`UnauthorizedAccessException` as `MainWindow.axaml.cs:181`. | `failed` |
| H7 | Closure check fails (should not happen) | Fail closed: nothing is written, "The mesh did not close, so nothing was written." + a technical-details line, and an event `export.validate` outcome `EXPORT-NOT-CLOSED`. | `failed` detail |
| H8 | Cancel at the save panel | Status strip "Export cancelled. Nothing was written." (matches `ShellHost.cs:1398`). | `cancelled` |
| H9 | No foil open | Menu row disabled; tooltip and palette entry carry COPY-EX03. | `nofoil` |
| H10 | Mesh preparing / writing | Preparing: skeleton of the summary (same height, with the trailing-edge row), Export... disabled. Writing a mesh over 100,000 triangles: progress with a Cancel that deletes the temp file. Below that the write is under a frame. | `preparing`, `writing` |

## 8. Copy

The final copy is the table in **section 14** (one table with the spec amendments). IDs stay track-local `COPY-EXnn`; the
leader assigns the final numbers when approved (the highest used now is COPY-457, `w2-save-picker`). The mockup's JSON block
is the same strings and is what a render check compares to. Words follow DESIGN.md section 3 (a unit beside every physical
value) and Rulings 155 and 158 (plain cause, the next step, no raw code alone). No "certified", "validated", "recommended",
"optimized" or "best" (LAB-01); the internal status `Certified` never reaches the user, who sees "geometry check". The floor is
always called "app default, no source" (Ruling 195), never "practitioner value".

## 9. Spec amendments

The amendments are rows A1-A14 of the table in section 14.

## 10. Build size (estimate)

Two slices plus the CLI, as Rulings 193 and 194 order. Estimates are agent-hours of build including red-first tests and the
dialog, and are **Inferred** from the probe sizes and from comparable dialogs (`CatalogDialog`, `SaveSectionDialog`). Revision 2
adds the work the rulings and the review created; the first estimate was about 3 days.

| Slice | Contents | Size |
|---|---|---|
| A1 .dat | Core: profile sampler (public "own" API), Selig/Lednicer writer with positional digits (B5), name-line sanitiser (B6), deviation measure, trailing-edge row from the written points; Desktop: dialog shell with the .dat options, command rows, native save, temp-then-rename writer, status strip; tests incl. round trip through `DatImport` and the At-station/Own fixture (B9) | about 1.6 days (was 1.0) |
| A2 STL | Core: tolerance ladder over `Placement.Surface`, closing step, **starboard half with root cap (B3)**, byte-level edge-use check (B2), binary STL writer, TE-floor finding, whole-span wording (B8), dense-sample deviation test (B4); Desktop: STL options, summary with skeleton, large-mesh band, failure states; tests incl. manifold/Euler on the four probe wings, header not `solid`, size = 84 + 50 n; slicer fixture for STL (B1) | about 2.7 days (was 1.5) |
| A3 3MF | Package writer, metadata, unit attribute; extend the slicer fixture to 3MF (B1) | about 0.6 day (was 0.5) |
| CLI (approved) | `export` verb over the same writers | about 0.3 day |
| Shared: floor label (D2) | `Settings.cs:29` label constant, spec and Settings label text, finding moved to the success path (A12), a test that no surface says "practitioner value" for the floor | about 0.2 day, counted in A1 |

Total about **5.2 days** of agent time (was about 3); the first operator-visible build (.dat + STL) about **4.3 days** (was about
1.5). Largest single new item: B1, which needs two slicers installed on the machine that runs the fixture (a one-off human or
scripted step; see section 11).

## 11. Items for the operator

Questions 1, 2, 4 and 5 are answered (Ruling 194). Question 3 is reworded and answered. Three small items are new:

1. **Which section does a .dat hold?** Answered: At station by default, Own second.
2. **Tolerance presets and the large-mesh line.** Answered: Draft 0.05, Print 0.02 (default), Fine 0.005 mm; warning above 500,000 triangles (kept, section 7 H5).
3. **Starboard half.** Answered: yes, with a root cap. The half is a **root split, a bonding aid and a datum**. Further cuts and connectors happen in the slicer; alignment features are out of scope.
4. **A `cfdw export` CLI verb?** Answered: yes.
5. **TE below the floor: advise only?** Answered: advise only; export into OneDrive folders is allowed.

New, for approval with section 14:

- **N1.** A starboard half is named with `-half` (`basic-foil-r12-half-mm.stl`) so it cannot overwrite the whole wing (section 6.4). Recommend yes.
- **N2.** The slicer fixture (B1) needs two slicers on the build machine (for example PrusaSlicer and Orca or Bambu Studio). Recommend you confirm which two, or that the build may install them.
- **N3.** The `Settings` label change (A11) is a spec and code change owned outside this track; recommend approving it with the Export build.

## 12. Build conditions (from the manufacturing-CAM review)

Each condition names the red-first test or fixture that proves it. "Red first" means the check fails on the code before the
change and passes after; the build records both runs in `docs/proof/<track>/red-first.md`.

| # | Condition | Red-first test or fixture |
|---|---|---|
| B1 | **Open and measure in two slicers.** Write the whole wing and the starboard half, each with an open and a closed trailing edge (the four probe wings give both), as STL and as 3MF; open each in two slicers. Zero repair warnings; the slicer's bounding box equals the reported size; the slicer's volume is within 0.1 % of the signed volume. Store the result as a fixture. Extend the 3MF spike to STL. | Fixture `export-slicer-measure.json` (slicer, version, file, warnings, bounding box, volume). Red: a deliberately broken STL (one triangle removed) must show a repair warning in the fixture run, so the check can fail. Also measures load time, which reviews the 500,000 line (B7). |
| B2 | **Closure check on the bytes as written.** Write from the welded vertex table, turn `-0.0` into `+0.0`, then re-weld the float32 bytes by bit pattern and check: every edge shared by exactly two triangles, no zero-area triangle. | Unit test that reads back the written STL bytes and re-welds by bit pattern. Red: a mesh with two doubles that round to one float, and a `-0.0` vertex beside a `+0.0` one, fail on a writer that checks the doubles. |
| B3 | **Half-wing tests, red first.** Edges paired, Euler 2, positive signed volume, the root face planar at y = 0. | Four checks on the half of each probe wing, at the 41x101 and the finest rung. Red: before the cap exists the pairing check fails. The probe already observed all four (probe files). |
| B4 | **Dense-sample deviation test (mandatory).** The reported deviation is compared with a dense random sample of the surface on both probe wings (Example foil, Untitled wing); the reported value must be at least the dense maximum minus a stated margin, or the label must change. | Test with a fixed seed, 100,000 points per wing, at Print. Red: a reporter that samples only chordwise midpoints must fail on the Untitled wing, whose spanwise deviation is larger than its chordwise one at the coarse rungs (`dev_span_mm` against `dev_chord_mm` in `probe-default-wing.txt`). |
| B5 | **`.dat` numbers in positional notation**, round-trip digits, never exponent form; plus the spec `:623` wording amendment (A10). | Test: write values 1e-5, 3.585447714271229e-5 and 1e-300; the text has no `E` or `e`, and `double.Parse` returns the same bits. Red: the shortest-round-trip default prints `3.585447714271229E-05`, which fails. |
| B6 | **The `.dat` name line never begins with two numbers.** | Test names `0.5 0.5 foil`, `1 2`, `3 4 5` and `foil 1 2`: the first three are prefixed, the last is not; the written file re-imports with the same point count. |
| B7 | **Large-mesh line.** Decided: keep 500,000 (section 7 H5), review with B1's load times. | Test that the band appears at 500,001 and not at 500,000; the Untitled wing at Fine (whole) shows it, the starboard half does not. |
| B8 | **A TE of 0.00 mm along the whole span** reads "along the whole span" (EX45a), and the jump lands on the trailing-edge gap control. | Test on the closed Example foil and the Untitled wing: the row text equals EX45a with 0.00, the jump label equals EX23a, and the jump target is the TE-gap readout (`PropertiesView.cs:1253`). Red: a located-value formatter prints "at the tip" for a closed foil. |
| B9 | **At station and Own, and the .dat against the STL** (computational-geometry review, `docs/proof/exd/section-thickness-expert.txt` item 5). On a station with twist other than 0 and t/c other than the authored peak: (a) the At-station points equal camber plus and minus half the thickness from `Sections`; (b) inverting the placement on the STL section points (undo elevation, divide by chord, rotate by minus twist) agrees within 1e-12; (c) the peak thickness equals the station t/c within 1e-12; (d) the Own peak is the authored maximum and does not change with the t/c channel; (e) Own and At station agree within 1e-12 when t/c equals the authored peak; (f) one blended eta between profiles with different peaks. Also: the .dat trailing-edge row equals last upper minus last lower times chord of the written points, for both shapes; the .dat x grid equals the STL chord grid. | Six checks and two assertions on one fixture foil. Red: a .dat built from raw `Samples` fails (a) and (c). |
| B10 | **Floor label (D2).** `Settings.cs:29` carries the label "app default, no source" through one named constant, and every surface that shows the floor uses it; the hash and the analysis numerics do not change. | Test that no string "practitioner value" is reachable for the TE floor, and that `RunRecord.cs:305` hashes the same bytes as before the change (golden hash). |

## 13. Assumptions and open items

- `assume:` revision label `r<ordinal>` is shown on the export (Verified source `AuthoringSession.cs:2003`; the dialog
  chip text is the only part that is new).
- `assume:` spanwise cells sampled at 3 midpoint kinds are enough to read the true maximum deviation. Confirmed or refuted by
  B4. Breaks if false: the tolerance label under-reads.
- `assume:` the 3MF unit string and winding (section 4.3); confirmed by B1.
- `assume:` the least trailing-edge thickness is the 3D distance between the last upper and last lower point of each
  evaluated section, taken over the sections of the exported mesh. Confirm at build against the section readout at the same
  station. Breaks if false: the row and the section inspector disagree on the same wing.
- Not verified: macOS and Windows native save panel behaviour for forced extensions (`StorageProvider` options); check
  during the build on both platforms.
- The manufacturing-CAM review ran and returned PASS-WITH-CONDITIONS (folded in here). The marine-CAD UX adversary pass has
  not run; recommended before the build starts.
- The mockup shows one trailing-edge value for both "At station" and "Own"; the build computes it from the written points
  (section 4.1, B9). The mockup's real file rows exist only for the probed section.

## 14. Final copy and amendments for operator approval

One table: every proposed string and every spec amendment. Copy rows are quoted exactly as the mockup renders them; ids are
track-local. **Spec** marks a string that already exists in the spec. Rows marked **changed** or **new** differ from revision 1.
Amendment rows give the place, the new text and the reason; A11 and the build item are owned outside this track.

| Kind | ID | Where | Final text or amendment |
|---|---|---|---|
| Copy | EX01 | File menu row, palette, default button | Export… |
| Copy | EX02 | Section menu row | Export .dat… |
| Copy | EX03 | disabled row reason | Export needs an open foil. |
| Copy | EX04 | dialog title; button when blocked | Export |
| Copy | EX05 · EX06 · EX07 | format rows | Section (.dat) · Wing (STL) · Wing (3MF) |
| Copy | EX08 | disabled STEP row | STEP export unavailable until the open-and-measure fixture exists (**spec** `:2299`) |
| Copy | EX09 · 09a · 09b | .dat option | Section shape · At station · Own |
| Copy | EX10 (**changed**) | .dat help | At station: the section as the wing builds it here; its peak thickness is the station t/c, <tc> %. Own: the profile as authored, unscaled. Both are in chord units, with no twist. |
| Copy | EX10a | station list | Station |
| Copy | EX11 · 11a · 11b | .dat option | File order · Selig · Lednicer |
| Copy | EX12 | .dat option | Points per surface (61 · 101 · 201) |
| Copy | EX13 · 13a · 13b | STL/3MF option | Scope · Whole wing · Starboard half |
| Copy | EX13c (**new**) | help line under the tolerance, only with Starboard half | Starboard half: one half wing with a flat root face, for bonding and as a datum. Cut it further, and add connectors, in your slicer. |
| Copy | EX14 · 14a · 14b · 14c | STL/3MF option | Tolerance · Draft 0.05 mm · Print 0.02 mm · Fine 0.005 mm |
| Copy | EX15 · 15a | STL/3MF locked row | Unit · Millimetres, unscaled. Fixed for print. |
| Copy | EX16 | summary heading | What will be written |
| Copy | EX17 · 17a | summary | Revision r<n>, accepted. · Exporting revision r<n>. Your open draft is not included. |
| Copy | EX18 | summary, Analysis mode | Analysis layers and results are not exported. |
| Copy | EX19 | loading | Preparing the mesh… |
| Copy | EX20 (**changed**) | status strip (STL, 3MF) | Exported <file> · <tris> triangles · <size> MB · mm · largest measured deviation <dev> mm |
| Copy | EX21 (**changed**) | status strip (.dat) | Exported <file> · <pts> points · x/c, y/c · chord <chord> mm · largest deviation <dev> mm |
| Copy | EX22 (**changed**) | finding band, only below the floor | Trailing edge <t> mm, below the floor of <f> mm (app default, no source) |
| Copy | EX23 | finding jump, located value | Show at <where> |
| Copy | EX23a (**new**) | finding jump, whole-span value | Show the trailing-edge gap |
| Copy | EX24 | fixed safety line, every geometric format | This geometry has not been checked for strength, manufacturability or ride safety. No standard for hydrofoil-wing strength applies (RCD 2013/53/EU excludes hydrofoils and surfboards; ISO 25649 excludes rigid surf-sport devices). Test before use. (**spec** A5.6, `:1014-1018`) |
| Copy | EX25 (**changed**) | summary, Fidelity row (STL, 3MF) | Largest deviation from the computed surface: <dev> mm, sampled at cell midpoints. Not a bound. |
| Copy | EX25a (**changed**) | summary, Fidelity row (.dat) | Largest deviation between the curve and the lines joining the points: <dev> mm at this chord, sampled at segment midpoints. |
| Copy | EX26 · EX27 · EX28 | summary limits | Closed: every edge joins two triangles (checked). · Coordinates are rounded to 0.00003 mm. · Computed by this app. No other CAD program has opened this file. |
| Copy | EX29 | large-mesh band | This mesh has <tris> triangles (<size> MB). Some slicers and CAD programs open it slowly. Choose Print for <tris2> triangles, or write it anyway. |
| Copy | EX30 | button, large-mesh state | Export anyway… |
| Copy | EX31 (**changed**; not rendered) | band, tolerance not reached | The tolerance was not reached. The finest mesh has a largest measured deviation of <dev> mm, over the <tol> mm you chose. |
| Copy | EX32 · EX33 | blocked state | Can't export yet · The shape is drawn, but its geometry check has not passed, so there is no accepted geometry to export. Open the Checks drawer, fix the finding, then export. |
| Copy | EX34 | write failure | Can't write the file. <cause> Nothing was changed. The earlier file is still there. |
| Copy | EX35a · b · c | write-failure causes | The disk is full. · You don't have permission to write to that folder. · The folder no longer exists. |
| Copy | EX36a · b | write-failure buttons | Choose another place… · Try again |
| Copy | EX37 · EX43 | closure failure | The mesh did not close, so nothing was written. · Technical details: the edge check found edges that are not shared by exactly two triangles. |
| Copy | EX38 | status strip | Export cancelled. Nothing was written. |
| Copy | EX39 | success action | Show in Finder (Windows: Show in Explorer) |
| Copy | EX40 | button | Cancel |
| Copy | EX41 · EX42 | writing state | Writing <tris> triangles (<size> MB)… · Writing… |
| Copy | EX44 (**new**) | summary row label, every format | Trailing edge |
| Copy | EX45 (**new**) | summary row, located value (STL, 3MF, .dat) | Least thickness <t> mm at <where>. Floor <f> mm (app default, no source). |
| Copy | EX45a (**new**) | summary row, whole-span value | <t> mm along the whole span. Floor <f> mm (app default, no source). |
| Copy | EX46 (**new**) | second line of the summary row | Manufacturing: not assessed (no process chosen) |
| Spec | A1 | F5, `:1655` | "Choose format, unit and tolerance" -> "Choose format and tolerance; the unit is fixed at millimetres for STL and 3MF". Why: EXP-03 fixes the unit (D5). |
| Spec | A2 (**revised**) | EXP-02, `:1217` | Add: "**Given** STL or 3MF, **then** the deviation shown is the largest value measured at cell midpoints against the evaluator's own surface and is labelled sampled, not a bound." Why: the certificate has no export certificate (`Geometry.cs:36`); D6. |
| Spec | A3 (**revised**) | Format table, `:625` | Replace the STL/3MF (print) row with: "write; closed solid: the evaluator's skin at a user-chosen tolerance plus tip caps, a trailing-edge strip when the section is open and a root weld by mirror, or a starboard half closed with a root cap at y = 0; no CAD kernel; refused if any edge of the written bytes is not shared by exactly two triangles; millimetres, never pre-scaled; 3MF `unit` attribute; STL unit in the file name and dialog." Why: section 3; Ruling 194 (3); B2. |
| Spec | A4 (**revised**) | Format table, `:623` DAT row | Add: "one section per file: at a station (default; camber plus and minus half the thickness measured vertically, peak thickness equal to the station t/c) or the authored profile, unscaled; chord fractions, twist not applied; the name line carries foil, station and revision and never begins with two numbers". Why: the spec is silent on which section a .dat holds; Ruling 194 (1); B6; the geometry review. |
| Spec | A5 (**revised**) | EXP-02 / GEO-12 | "Every geometric export shows a trailing-edge row: the least trailing-edge thickness along the exported surface in mm (for a .dat, at that station from the written points), where it is, the floor with its label, and 'Manufacturing: not assessed (no process chosen)'. A closed trailing edge reads 0.00 mm. A thickness below the floor adds the advisory finding." Why: Ruling 195; D7. |
| Spec | A6 | EXP-03, `:1218` | Name the 3MF metadata keys allowed: Title, Description (revision, tolerance, measured deviation), Application; never user name, path or machine name. Why: privacy rule `:1353`, `:1355`. |
| Spec | A7 | Export dialog safety string | Say the fixed string shows for every geometric format, including .dat. Why: the spec says "export dialog" only. |
| Spec | A8 | Reading rule | State that Export reads the accepted revision and refuses when the geometry check has not passed. Why: D3; matches `AnalysisService.cs:94-95`. |
| Spec | A9 (**revised**) | Area table `:1422` | "Export: Choose format - Write" -> "Choose format - Export..."; add the Section > Export .dat... entry and the `cfdw export` verb. Why: section 6; Ruling 194 (4). |
| Spec | A10 (**new**, B5) | Format table, `:623` DAT row | Replace "shortest-round-trip digits" with "shortest round-trip digits in positional notation, never exponent form". Why: the first probe wrote `3.585447714271229E-05` (`probe2-default-root.txt` row 2); some readers do not parse exponents. |
| Spec | A11 (**new**, review D2; Ruling 195) | Setting registry `:1035` (`manufacturing.te_floor`) and every place the spec shows the floor's label (GEO-12 `:1190`) | Replace the label "practitioner value, unverified" with "app default, no source". The value 0.3 mm, the analysis numerics and the settings hash do not change. Why: Ruling 195. Build item: `src/CfdWorkbench.Analysis/Settings.cs:29` gains one named label constant, and any UI string that says "practitioner value" for this floor is replaced (a grep of `src/` found none today); test B10. |
| Spec | A12 (**new**, review D3) | F5, `:1657` | Move the TE-floor finding from the failure branch ("Explain; TE floor finding; return to geometry") to the success path as an advisory that never blocks; the failure branch keeps only the explanation. Why: GEO-12 (`:1190`) calls it an advisory DRC finding, and Ruling 194 (5) confirms advise-only. |
| Spec | A13 (**new**, review D4) | Copy table, `:2302` (TE below floor, EX22) | Replace the string with "Trailing edge <t> mm, below the floor of <f> mm (app default, no source)". Why: Ruling 195; one label everywhere. |
| Spec | A14 (**new**, review D5) | EXP-02 `:1217`, F5 `:1660` | Use "deviation" for mesh and .dat fidelity; keep "gap" only for the trailing-edge gap. The spec already says "deviation" at `:1217` and `:1660`, so this is a wording rule for the new rows, not a rewrite. Why: one term per quantity. |

## 15. Receipts

`docs/proof/exd/`: `probe-wing-mesh.cs.txt`, `probe-section-dat.cs.txt` (the programs, `.txt` so they stay inert),
`probe-open.txt`, `probe-open14.txt`, `probe-closed.txt`, `probe-default-wing.txt` (wing mesh outputs, regenerated by trk-exr
from the committed source), `probe2-open-root.txt`, `probe2-open-tip.txt`, `probe2-open14-root.txt`, `probe2-default-root.txt`,
`probe2-default-tip.txt` (.dat outputs), `probe-commands.txt` (commands and the corrections), `section-thickness-expert.txt`
(the geometry hand-off), `craft-gate.txt`, `capture-run.txt` and `shoot-states.mjs.txt` (the browser capture and its script).
Reproduce: a scratch console project referencing `src/CfdWorkbench.Core`, run with the Example foil
(`src/CfdWorkbench.Desktop/Assets/example.foil`) or `default`, the open-TE half-thickness (0.001 or 0.0014, 0 for closed)
and, for the .dat probe, the station eta (0 or 1).
