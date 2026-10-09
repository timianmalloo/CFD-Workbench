---
id: design-export
title: "Design: Export (Area 7) - section .dat, wing STL, then 3MF"
type: design
status: proposed
owner: "@timianmalloo"
phase: design - Export slice A (Ruling 193)
tags: [export, dat, stl, 3mf, exp-02, exp-03, te-floor, fidelity, millimetres, watertight, area-7, trk-exd]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: rulings, rel: implements }
  - { to: design-language, rel: depends-on }
  - { to: design-m12d-catalog, rel: relates-to }
  - { to: mockup-export, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Detailed design of the first Export slice: the section .dat (Selig or Lednicer), the wing STL, then the 3MF, all in
  millimetres and unscaled where a unit exists, each with a stated fidelity and the TE-floor finding repeated. The
  accepted wing can be written as a closed (watertight) solid with no CAD kernel: a probe on two wings gave a manifold
  mesh with Euler characteristic 2 from the existing display evaluator plus a topology-only closing step. Copy rows and
  spec amendments are proposed for the operator; nothing is built until the operator approves the mockup.
---

# Design: Export - section .dat, wing STL, then 3MF

Scope is Ruling 193 (`docs/notes/rulings.md`, "Ruling 193"): section `.dat` and wing STL first, then 3MF, within EXP-02
and EXP-03 of `docs/specs/cfd-workbench-v1.md` (`:1217`, `:1218`). STEP, 3DM and Fusion stay out (they wait on the kernel
decision and open-and-measure evidence). **Design only. No `src/` or `tests/` change.** Copy and spec amendments below
are proposals for the operator. The companion mockup is [`export.html`](../mockups/export.html).

Goal state. **Goal:** settle what each file contains, where the geometry comes from, how the user drives it and what
every hard state says, so the build can start on a visual yes. **Done when:** this note and the mockup are committed,
the build size and the kernel question are answered with evidence, and the operator has the questions in section 11.
**Not in scope:** STEP, 3DM, Fusion, AVL, chart export, mould or CAM output, a batch "every station" export. **Tier:**
T2 (new user-facing surface, new file writers).

Confidence labels: **Verified** = observed in code or by running the probe in `docs/proof/exd/`; **Inferred** = reasoned,
not observed; **Assume** = marked `assume:` with what confirms it.

## 1. Decisions in one table

| # | Decision | Basis |
|---|---|---|
| D1 | One modal dialog, **File > Export...** (palette entry too), format list on the left, that format's options on the right, a fixed "What will be written" block, the findings, then Cancel and **Export...**. Choosing Export... opens the native save panel; the file is written after the panel. | spec `:2198` (Export is a modal dialog), F5 `:1651`; panel as in `MainWindow.axaml.cs:157-165` |
| D2 | Section menu gets **Export .dat...** (next to Import .dat..., `CommandTable.cs:150`); it opens the same dialog on the .dat format at the selected station. | symmetry with Import |
| D3 | Export reads the **accepted** revision, never an open draft, and only when the accepted geometry check has passed. | `WorkbenchController.cs:559-561`; `AnalysisService.cs:94-95` |
| D4 | The wing STL/3MF is a **closed solid**: skin from the display evaluator at a tolerance the user picks (three presets), plus a topology-only closing step (tip caps, trailing-edge strip when open, root weld by mirror). No CAD kernel. | section 3, probe |
| D5 | Unit is **not a choice**: STL and 3MF are millimetres, unscaled (EXP-03). F5's "Choose format, unit and tolerance" becomes "format and tolerance". | spec `:1218`, `:1655` |
| D6 | Fidelity is **measured and labelled as measured**: the deviation is the app's own sampled surface, sampled at cell midpoints, not a certificate. The geometry certificate itself says it has no export certificate. | `Geometry.cs:36` |
| D7 | The TE-floor finding is repeated in the dialog whenever the least trailing-edge thickness is below the floor. It never blocks. | spec `:633`, `:2302`, GEO-12 |
| D8 | Result goes to the **status strip**, failure stays **in the dialog**; the earlier file at the same path is never touched on failure (temp file, then rename). | F5 `:1660-1661`; `ProjectStore.cs:226` |

## 2. What exists today (Verified)

- There is **no export writer** anywhere in `src/` (grep for `Selig`, `Lednicer`, `Stl`, `Export` finds only the importer
  `DatImport.cs`, `Provenance.cs:94`, `SectionReplace.cs:301` and unrelated hits). The importer only reads, and returns
  chord-normalised points (`DatImport.cs:20-58`).
- The command table has no Export row (`CommandTable.cs:73-78` lists New, Open, Save, Save As, Close). `Section > Import .dat...`
  is row `section.import-dat` (`:150`).
- File I/O uses Avalonia's `StorageProvider` pickers (`MainWindow.axaml.cs:161`, `ShellHost.cs:1393`), and a failed save
  is worded in the status strip and rethrown (`MainWindow.axaml.cs:176-189`).
- The TE floor default is **0.3 mm** (`Settings.cs:28`, the last `RunSettings` argument; field `TeFloorMm`,
  `RunRecord.cs:52`). No finding for "TE below the floor" exists in code yet; the TE gap is a section readout
  (`SectionModel.cs:11,17`, shown at `PropertiesView.cs:1253`).
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
planar and x-monotone in the chord frame); (4) a half-wing export also caps the root plane the same way.

`docs/proof/exd/probe-wing-mesh.cs.txt` is the probe (a scratch console project referencing `CfdWorkbench.Core`; it
builds the closed mesh from `Placement.Surface`, checks every directed edge has exactly one opposite mate, computes
Euler's V-E+F and the signed volume, and measures the deviation against a 2x refined surface). Observed
(`probe-open.txt`, `probe-closed.txt`, `probe-open14.txt`, `probe-default-wing.txt`):

| Wing | TE | Rung (stations x chord pts) | Triangles | Edge-manifold | Euler | Volume sign |
|---|---|---|---|---|---|---|
| Example foil (450 mm half span, 120 mm chord) | open, 0.26 mm | 11x26 ... 161x401 (all six rungs) | 2,138 ... 514,238 | yes (every rung) | 2 | positive |
| Example foil | open, 0.36 mm | all six | 2,138 ... 514,238 | yes | 2 | positive |
| Example foil as shipped | closed | 41x101 ... 321x801 | 32,396 ... 2,051,196 | yes | 2 | positive |
| Untitled NACA 0012 wing (`FoilSource.NewDefault`, 500 mm half span) | closed | all six | 2,096 ... 513,596 | yes | 2 | positive |

Euler 2 means one closed genus-0 surface; positive signed volume means outward-facing normals with the winding used.
`Geometry.Assess` returned `Certified` for the sample wings (`probe-*.txt`, line `status=`).

**Consequence for the build.** STL needs the closing step, about 60 lines of topology code (Inferred) plus an edge-use check that
becomes a **post-condition of every write** (refuse and write nothing if any edge is not shared by exactly two
triangles). It needs no kernel, so the kernel decision does not block this slice. It does need a small Core change:
`Placement.Surface` only offers uniform grids (stations and chord count), so the tolerance ladder in 4.2 calls it per
rung; a new public Core member is also needed for the "Own" profile (4.1) because `ProfileEvaluator` is internal.

**Not covered by the probe (Inferred):** a wing whose last authored section is not planar in `y` (the evaluator sets `y`
per section, so this should not arise), and a foil with a closed tip (`Tip != open` is `Unsupported`,
`Geometry.cs:337-339`, so it is refused before export anyway).

## 4. What each format writes (Functional)

### 4.1 Section `.dat`

| Item | Decision |
|---|---|
| Which section | The user picks **At station** (default; the section as the wing builds it at that station, camber +/- half the thickness from `Placement.Sections`, so it carries the station t/c) or **Own** (the authored profile before the station's thickness scaling). The vocabulary "own" and "at station" is the section editor's (`m12c-section-editor.md:94`). One section per file. |
| Station | A list of authored stations (the sample has Root and Tip). Default: the station selected in the editor, else Root. |
| Ordering | **Selig** (default): trailing edge upper, along the upper surface to the nose, then along the lower surface to the trailing edge; the nose point appears once. **Lednicer**: a header line with the two counts, then upper nose to tail, a blank line, lower nose to tail. Both are read by `DatImport.cs:95-142`, so a written file re-imports. |
| Points | Per surface N = 61, **101** (default) or 201. Selig file has 2N-1 points (121, 201, 401). Spacing is the cosine law already used by the evaluator, x = (1-cos(pi i/(N-1)))/2 (`Placement.cs:707-715`), nose-dense. The importer needs at least 10 points (`DatImport.cs`, `points.Count < 10`), met. |
| Trailing edge | As built: closed gives equal first and last point (both written); open gives two distinct end points. The gap in mm at this chord is shown. Nothing is closed or rounded. |
| Coordinates | x/c and y/c, dimensionless, chord frame, **twist not applied**. Digits: shortest round-trip (spec `:623` DAT row), invariant culture, `.` decimal, `\n` line ends, UTF-8 no BOM. |
| Name line | `<foil name> | <station> | r<n>` with control characters and anything past 80 characters removed (the importer's fixture "a .dat name line containing instructions", spec `:1318`, is the matching risk). `#` comment lines are not written (other tools reject them). |
| Real rows | Selig, 101 per surface, Example foil root, open TE: row 1 `1 0.0010655364151166689`; row 2 `0.9997532801828658 0.0010892048507038603`; row 101 `0 0`; row 102 `0.0002467198171342 -0.0024037402789592885`; row 201 is the lower trailing edge (`probe2-open-root.txt`). |

### 4.2 Wing STL

| Item | Decision |
|---|---|
| Scope | **Whole wing** (default; starboard + port, welded at the root) or **Starboard half** (closed with a root cap plane at y = 0). No "one surface": an open shell is not a print solid. |
| Frame and unit | The authored coordinate frame (x aft, y span, z up), metres from the evaluator times 1000. **Millimetres, unscaled, not re-oriented** (EXP-03). |
| Tolerance | Three presets: **Draft 0.05 mm**, **Print 0.02 mm** (default), **Fine 0.005 mm**. The writer walks a fixed ladder of grids (stations x chord points: 11x26, 21x51, 31x76, 41x101, 81x201, 161x401, 321x801) and takes the first rung whose **measured** largest gap is at or under the tolerance. The dialog shows the rung's triangle count, file size and measured gap before writing. If even the finest rung misses the tolerance the dialog says so (4.5) and offers a coarser choice. `simplify:` uniform ladder; ceiling is wings whose curvature is local (the Untitled wing needs a 4x finer rung than the Example foil because the planform curves at the tip); upgrade trigger is any real wing past 500,000 triangles at Print, then refine adaptively near the tip. |
| Watertightness | Always closed: the write refuses if the edge-use check fails (section 3.3). Counts of triangles, vertices and the check result are in the summary. |
| Normals | Facet normal computed per triangle from its vertices, unit length, outward (winding verified by positive signed volume in the probe). |
| Format | **Binary** STL only: 80-byte header that does not begin with `solid`, a 32-bit count, 50 bytes per triangle. ASCII is not offered (about four times larger; every slicer reads binary). Header text: `CFD Workbench <foil slug> r<n> mm`. Coordinates are binary32; at 450 mm the rounding step is 2^-15 mm = 0.00003 mm (computed), far below the tolerance. |
| Name | `<foil-slug>-r<n>-mm.stl` (EXP-03: unit in the file name), for example `basic-foil-r12-mm.stl`. Slug from `DatImport.Slug` (`DatImport.cs:15`). |
| Size at the presets (Verified, probe) | Example foil, open TE: Draft 8,278 triangles, 0.41 MB, measured 0.0325 mm; Print 18,418, 0.92 MB, 0.0145 mm; Fine 129,118, 6.46 MB, 0.0020 mm. Untitled wing: Draft 32,396, 1.62 MB, 0.0450 mm; Print 128,796, 6.44 MB, 0.0113 mm; Fine 513,596, 25.68 MB, 0.0028 mm. Whole wing. Building a mesh takes 5-135 ms at these rungs (`surfMs` in the probe), so the dialog can recompute on every tolerance change. |

### 4.3 Wing 3MF (second increment)

Same mesh, scope, tolerance, closing step and checks as the STL. The differences:

- Package: a ZIP (`System.IO.Compression`, in the base library) with `[Content_Types].xml`, `_rels/.rels` and
  `3D/3dmodel.model`; one `<object type="model">` with one `<mesh>`, one `<build><item>`.
- Unit: `<model unit="millimeter">` and coordinates in mm, unscaled (EXP-03 "3MF attribute"). `assume:` the 3MF Core
  specification names `millimeter` as the value and counter-clockwise outward winding; confirm by a spike (open the written
  file in two slicers) before the 3MF build. Breaks if false: a part that prints at the wrong scale or inside out.
- Metadata (allowed keys, proposed): `Title` = foil name; `Description` = `Revision r<n>, tolerance <t> mm, measured gap
  <g> mm`; `Application` = `CFD Workbench <version>`. Never the user name, the path or the machine name (spec A8.5 export
  rules, `:1353`, `:1355`).
- Name: `<foil-slug>-r<n>.3mf` (the unit is in the attribute, so `-mm` is not needed).
- It ships as the second increment of this slice (Ruling 193 order). The dialog design includes it so the choice list
  does not change later.

## 5. Fidelity statement (EXP-02)

Shown in the dialog before the write and again in the status strip after it. Each line has its number and unit.

| Format | Lines shown |
|---|---|
| .dat | Units: fractions of chord (x/c, y/c); chord at this station 120.0 mm; twist not applied. Points: 201 (Selig). Revision: r12, accepted. Largest gap between the curve and the straight lines joining the points: 0.0074 mm at this chord, sampled at the midpoints of the 200 segments (Verified for N=101: 7.40 um, `probe2-open-root.txt`). Limit: points lie on the app's computed curve; no other program has read them. |
| STL | Units: mm, unscaled. Revision: r12, accepted. Tolerance: 0.02 mm. Largest measured gap to the app's computed surface: 0.0145 mm, sampled at cell midpoints, not a bound. Closed: every edge joins two triangles (checked). Coordinates rounded to 0.00003 mm (binary32). Limit: "the computed surface" is the app's own evaluation (`Geometry.cs:36`: no export certificate); no other CAD system has opened it. Not strength-checked: the fixed safety string (A5.6). |
| 3MF | As STL, plus: unit attribute millimeter. |

The deviation is a **maximum over sampled midpoints at one refinement level**; the probe measures chordwise, spanwise
and diagonal midpoints of each cell against the surface at twice the resolution. It can under-read a narrow feature. The
label says "sampled" and "not a bound" for that reason. `LAB-01` words ("certified", "validated", "recommended",
"optimized", "best") are not used.

## 6. UX

### 6.1 Entry points

| Entry | Detail |
|---|---|
| File > Export... | new row `file.export`, gesture Shift+Command+E (macOS) and Ctrl+Shift+E (Windows); free in `CommandTable.cs:73-164`. Disabled with a reason when no foil is open (COPY-EX03). |
| Command palette | every table row is a palette entry (`CommandTable.PaletteEntries`), so "Export..." is findable with no extra code. |
| Section > Export .dat... | new row `section.export-dat`; opens the dialog on .dat at the selected station. |
| CLI | Proposed, optional, recommended: `cfdw export <file> --format dat|stl|3mf [--station N] [--tolerance mm] --out <path>` beside `inspect` and `analyse` (`Cli/Program.cs:52-54`). It runs the same writer, so the build has a headless test oracle. Operator may drop it (question 4). |

### 6.2 The dialog (see the mockup)

Left: the format list - Section (.dat), Wing (STL), Wing (3MF), and a disabled STEP row carrying the spec string
(`:2299`). Right: that format's options. Below the options a fixed block, **What will be written**, with the fidelity
lines. Then the findings (TE floor; draft note), the fixed safety string, and the buttons Cancel and **Export...** (the
default button; Enter activates it; Escape cancels and returns focus to the menu item that opened it). While the mesh is
prepared the block shows its skeleton at the same size and Export... is disabled with "Preparing the mesh..." in place.

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
5. Write: build the bytes in memory, run the closure check, write to a temp file in the destination folder
   (`.cfd-<guid>.tmp`, as `ProjectStore.cs:226`), then rename over the target. On any failure delete the temp file; the
   earlier file at the path is untouched.
6. Success: the dialog closes, the status strip reads COPY-EX20 (STL, 3MF) or COPY-EX21 (.dat) with **Show in Finder** / **Show in Explorer**. Failure:
   the dialog stays open with COPY-EX34 and EX35, **Choose another place...** and **Try again** (COPY-EX36).

The extension is forced by the format; if the user types another extension the product writes the forced one. The export
can never target the project file (`.cfdw.json`).

### 6.4 File names

| Format | Suggested name | Example |
|---|---|---|
| .dat | `<foil-slug>-<station-slug>-r<n>.dat` | `basic-foil-root-r12.dat` |
| STL | `<foil-slug>-r<n>-mm.stl` | `basic-foil-r12-mm.stl` |
| 3MF | `<foil-slug>-r<n>.3mf` | `basic-foil-r12.3mf` |

Windows rule: Ruling 146's OneDrive refusal protects the project store. `assume:` it does not extend to exports, because
an export is a copy and the temp-then-rename write is safe in a synced folder; confirm with the operator (question 5 note).
Breaks if false: a sync client holds the temp file and the rename fails, which is the ordinary write-failure state (H6).

## 7. The hard states

| # | State | Behaviour | Mockup |
|---|---|---|---|
| H1 | Draft open (section editor or a gesture not yet applied) | Export proceeds on the accepted revision and says so: "Exporting revision r12. Your open draft is not included." Not a block. | `draft` detail (.dat, STL) |
| H2 | Geometry not accepted / check not passed (`Assess` is not `Certified`; the shape can still be drawn) | Dialog opens; Export... disabled; reason names the finding location (the Checks drawer) and the way out. Same gate as `AnalysisService.cs:94-95`. | `blocked` |
| H3 | TE below the floor | Spec string `Trailing edge <t> mm below the floor <f> mm (practitioner value, unverified)` in the findings, with "Show at tip" jump. Advisory. Shown for .dat (gap at that station's chord), STL and 3MF (least gap along the span). Closed TE reads 0.00 mm, below the 0.3 mm floor, and shows the same finding. | `ready`: sample A below the floor, B meets it |
| H4 | Analysis mode active | Export reads the accepted revision exactly as in CAD mode (`WorkbenchController.cs:559-561`). The dialog adds one line: "Analysis layers and results are not exported." If the foil is drawn but the geometry check has not passed, H2 applies. There is **no state today where analysis runs on geometry export refuses**: both use the `Certified` gate. | `analysis` detail |
| H5 | Large mesh | At more than 500,000 triangles (25 MB binary STL) the summary shows a warning band and the button reads "Export anyway..."; the coarser preset is one click. The writer never refuses on size alone. At the top rung (2,051,196 triangles, 102.6 MB) with a tolerance not met, the band says what was reached (COPY-EX31; not rendered, no sample wing reaches that case). Real example: Untitled wing at Fine, 513,596 triangles, 25.68 MB. | `large` |
| H6 | Write failure (permission, disk full, path gone) | In-dialog error with plain cause, "Nothing was changed. The earlier file is still there.", **Choose another place...**, **Try again**. Reasons from `IOException`/`UnauthorizedAccessException` as `MainWindow.axaml.cs:181`. | `failed` |
| H7 | Closure check fails (should not happen) | Fail closed: nothing is written, "The mesh did not close, so nothing was written." + a technical-details line, and an event `export.validate` outcome `EXPORT-NOT-CLOSED`. | `failed` detail |
| H8 | Cancel at the save panel | Status strip "Export cancelled. Nothing was written." (matches `ShellHost.cs:1398`). | `cancelled` |
| H9 | No foil open | Menu row disabled; tooltip and palette entry carry COPY-EX03. | `nofoil` |
| H10 | Mesh preparing / writing | Preparing: skeleton of the summary, Export... disabled. Writing a mesh over 100,000 triangles: progress with a Cancel that deletes the temp file. Below that the write is under a frame. | `preparing`, `writing` |

## 8. Copy (proposed; the operator approves)

IDs are track-local `COPY-EXnn`; the leader assigns the final numbers when approved (the highest used now is COPY-457,
`w2-save-picker`). The mockup's JSON block is the same table and is what a render check compares to. Strings that already
exist in the spec are marked **spec** and quoted verbatim. Words follow DESIGN.md section 3 (a unit beside every physical
value) and Rulings 155 and 158 (plain cause, the next step, no raw code alone). No "certified", "validated",
"recommended", "optimized" or "best" (LAB-01); the internal status `Certified` never reaches the user, who sees "geometry
check".

| ID | String | Where |
|---|---|---|
| EX01 | Export… | File menu row, palette, default button |
| EX02 | Export .dat… | Section menu row |
| EX03 | Export needs an open foil. | disabled row reason |
| EX04 | Export | dialog title; button when blocked |
| EX05 · EX06 · EX07 | Section (.dat) · Wing (STL) · Wing (3MF) | format rows |
| EX08 | STEP export unavailable until the open-and-measure fixture exists (**spec** `:2299`) | disabled STEP row |
| EX09 · 09a · 09b | Section shape · At station · Own | .dat option |
| EX10 · 10a | At station: the section as the wing builds it here, at t/c <tc> %. Own: the profile as authored, before the station's thickness. · Station | .dat help, station list |
| EX11 · 11a · 11b | File order · Selig · Lednicer | .dat option |
| EX12 | Points per surface (61 · 101 · 201) | .dat option |
| EX13 · 13a · 13b | Scope · Whole wing · Starboard half | STL/3MF option |
| EX14 · 14a · 14b · 14c | Tolerance · Draft 0.05 mm · Print 0.02 mm · Fine 0.005 mm | STL/3MF option |
| EX15 · 15a | Unit · Millimetres, unscaled. Fixed for print. | STL/3MF locked row |
| EX16 | What will be written | summary heading |
| EX17 · 17a | Revision r<n>, accepted. · Exporting revision r<n>. Your open draft is not included. | summary |
| EX18 | Analysis layers and results are not exported. | summary, Analysis mode |
| EX19 | Preparing the mesh… | loading |
| EX20 | Exported <file> · <tris> triangles · <size> MB · mm · largest measured gap <gap> mm | status strip (STL, 3MF) |
| EX21 | Exported <file> · <pts> points · x/c, y/c · chord <chord> mm · largest gap <gap> mm | status strip (.dat) |
| EX22 | Trailing edge <t> mm below the floor <f> mm (practitioner value, unverified) (**spec** `:2302`) | findings |
| EX23 | Show at <where> | finding jump |
| EX24 | This geometry has not been checked for strength, manufacturability or ride safety. No standard for hydrofoil-wing strength applies (RCD 2013/53/EU excludes hydrofoils and surfboards; ISO 25649 excludes rigid surf-sport devices). Test before use. (**spec** A5.6, `:1014-1018`) | fixed safety line, every geometric format |
| EX25 · 25a | Largest gap to the computed surface: <gap> mm, sampled at cell midpoints. Not a bound. · Largest gap between the curve and the lines joining the points: <gap> mm at this chord, sampled at segment midpoints. | summary, Fidelity row |
| EX26 · EX27 · EX28 | Closed: every edge joins two triangles (checked). · Coordinates are rounded to 0.00003 mm. · Computed by this app. No other CAD program has opened this file. | summary limits |
| EX29 | This mesh has <tris> triangles (<size> MB). Some slicers and CAD programs open it slowly. Choose Print for <tris2> triangles, or write it anyway. | large-mesh band |
| EX30 | Export anyway… | button in the large-mesh state |
| EX31 | The tolerance was not reached. The finest mesh has a largest measured gap of <gap> mm, over the <tol> mm you chose. | band, **not rendered** (no sample wing misses Fine at the top rung; render when one is found) |
| EX32 · EX33 | Can't export yet · The shape is drawn, but its geometry check has not passed, so there is no accepted geometry to export. Open the Checks drawer, fix the finding, then export. | blocked state |
| EX34 | Can't write the file. <cause> Nothing was changed. The earlier file is still there. | write failure |
| EX35a · b · c | The disk is full. · You don't have permission to write to that folder. · The folder no longer exists. | write-failure causes |
| EX36a · b | Choose another place… · Try again | write-failure buttons |
| EX37 · EX43 | The mesh did not close, so nothing was written. · Technical details: the edge check found edges that are not shared by exactly two triangles. | closure failure |
| EX38 | Export cancelled. Nothing was written. | status strip |
| EX39 | Show in Finder (Windows: Show in Explorer) | success action |
| EX40 | Cancel | button |
| EX41 · EX42 | Writing <tris> triangles (<size> MB)… · Writing… | writing state |

## 9. Spec amendments proposed (one batch for the operator)

| # | Where | Amendment | Why |
|---|---|---|---|
| A1 | F5, `:1655` | "Choose format, unit and tolerance" -> "Choose format and tolerance; the unit is fixed at millimetres for STL and 3MF". | EXP-03 fixes the unit (D5). |
| A2 | EXP-02, `:1217` | Add: "**Given** STL or 3MF, **then** the deviation shown is the largest gap measured at cell midpoints against the evaluator's own surface and is labelled sampled, not a bound." | The certificate has no export certificate (`Geometry.cs:36`); D6. |
| A3 | Format table, `:625` | Replace the STL/3MF (print) row with: "write; closed solid: the evaluator's skin at a user-chosen tolerance plus tip caps, a trailing-edge strip when the section is open and a root weld by mirror, no CAD kernel; refused if any edge is not shared by exactly two triangles; millimetres, never pre-scaled; 3MF `unit` attribute; STL unit in the file name and dialog." | Section 3: the closing step is part of the contract. |
| A4 | Format table, `:623` DAT row | Add: "one section per file: at a station (default) or the authored profile; chord fractions, twist not applied; the name line carries foil, station and revision". | The spec is silent on which section a .dat holds. |
| A5 | EXP-02 / GEO-12 | Define the TE finding for export: "the least trailing-edge thickness along the exported surface (for a .dat, at that station's chord); a closed trailing edge reads 0.00 mm". | Today only a per-station readout exists. |
| A6 | EXP-03, `:1218` | Name the 3MF metadata keys allowed: Title, Description (revision, tolerance, measured gap), Application; never user name, path or machine name. | Privacy rule `:1353`, `:1355`. |
| A7 | Export dialog safety string | Say the fixed string shows for every geometric format, including .dat. | The spec says "export dialog" only. |
| A8 | Reading rule | State that Export reads the accepted revision and refuses when the geometry check has not passed. | D3; matches `AnalysisService.cs:94-95`. |
| A9 | Area table `:1422` | "Export: Choose format - Write" -> "Choose format - Export..." and add the Section > Export .dat... entry. | Section 6. |

## 10. Build size (estimate)

Two slices, as Ruling 193 orders. Estimates are agent-hours of build including red-first tests and the dialog, and are
**Inferred** from the probe sizes and from comparable dialogs (`CatalogDialog`, `SaveSectionDialog`).

| Slice | Contents | Size |
|---|---|---|
| A1 .dat | Core: profile sampler (public "own" API), Selig/Lednicer writer, name-line sanitiser, deviation measure; Desktop: dialog shell with the .dat options, command rows, native save, temp-then-rename writer, status strip; tests incl. round trip through `DatImport` | about 1 day |
| A2 STL | Core: tolerance ladder over `Placement.Surface`, closing step, edge-use check, binary STL writer, TE-floor finding; Desktop: STL options, summary with skeleton, large-mesh band, failure states; tests incl. manifold/Euler on the sample wings, header not `solid`, size = 84 + 50 n | about 1.5 days |
| A3 3MF | Package writer, metadata, unit attribute; spike for slicer acceptance | about 0.5 day |
| CLI (optional) | `export` verb over the same writers | about 0.3 day |

Total about 3 days of agent time, about 1.5 for the first operator-visible build (.dat + STL).

## 11. Operator questions (at most five, each with a recommendation)

1. **Which section does a .dat hold?** Recommend: **At station** by default (what flies), with **Own** as the second
   choice. Alternative: Own only (simpler; no thickness scaling).
2. **Tolerance presets and the large-mesh line.** Recommend Draft 0.05 / Print 0.02 (default) / Fine 0.005 mm, warning
   above 500,000 triangles. Alternative: a free numeric tolerance (more control, more states to design).
3. **Whole wing only, or also a starboard half?** Recommend both (the half costs one cap and helps printing a 900 mm wing
   in pieces). Alternative: whole wing only for the first slice.
4. **A `cfdw export` CLI verb?** Recommend yes (about 0.3 day; gives the build a headless oracle). Alternative: dialog only.
5. **TE below the floor: advise only, or require a tick?** Recommend advise only (it is an advisory finding in GEO-12 and
   EXP-03 says "repeated"). Alternative: a confirm tick before writing. Also confirm that exports to a OneDrive folder are
   allowed (section 6.4).

## 12. Assumptions and open items

- `assume:` revision label `r<ordinal>` is shown on the export (Verified source `AuthoringSession.cs:2003`; the dialog
  chip text is the only part that is new).
- `assume:` spanwise cells sampled at 3 midpoint kinds are enough to read the true maximum gap. Confirms: a build-time test
  that compares the reported gap with a dense random sample on the two probe wings. Breaks if false: the tolerance label
  under-reads.
- `assume:` the 3MF unit string and winding (section 4.3); confirm by spike.
- Not verified: macOS and Windows native save panel behaviour for forced extensions (`StorageProvider` options); check
  during the build on both platforms.
- The marine-CAD UX and manufacturing adversary passes were not run in this track; recommended before the build starts.

## 13. Receipts

`docs/proof/exd/`: `probe-wing-mesh.cs.txt`, `probe-section-dat.cs.txt` (the programs, `.txt` so they stay inert),
`probe-open.txt`, `probe-closed.txt`, `probe-open14.txt`, `probe-default-wing.txt`, `probe2-open-root.txt` (outputs).
Reproduce: a scratch console project referencing `src/CfdWorkbench.Core`, run with the example foil path, the open-TE
half-thickness (0.001 or 0.0014, 0 for closed) and, for the .dat probe, the station eta.
