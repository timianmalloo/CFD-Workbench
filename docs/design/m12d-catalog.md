---
id: design-m12d-catalog
title: "Design: M1.2d — Replace from catalog and My sections in the section editor"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — M1.2d (operator 2026-10-04, "apply an existing profile from the catalog to the section")
tags: [desktop, core, persistence, cad, section, catalog, my-sections, replace, provenance, rights, abscissa, m1.2d, dr-4, dr-8]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-0008-section-library, rel: implements }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: design-m12c-section-editor, rel: refines }
  - { to: design-app-shell, rel: relates-to }
  - { to: rulings, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: mockup-m12d-catalog, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Detailed design of slice M1.2d. In the section editor, Replace from catalog… puts a NACA section or a My sections entry
  on the section as one undoable draft step, and Save to My sections… keeps the current section for any foil. Neighbouring
  sections must share their point spacing for the blend to certify (Ruling 71), so Replace fits the chosen shape onto the
  section's existing spacing and reports the fit against the 10 µm rule; when that fit is over the limit and a neighbour
  uses a different section, Replace for this station is refused with the number, and a Replace at every station of the
  blend is offered instead. A probe measured every number. Gate: four lenses, one repair cycle.
review-suggested: []
---

# Design: M1.2d — Replace from catalog and My sections

- **Status:** In review; four lenses in Adversary mode, repair cycle 1 of 1 applied and re-checked — the two hard vetoes
  and the test block are cleared (gate record at the end). Nothing is
  built until the operator approves the mockup (memory rule) and rules the DR-M12D batch (§13).
- **Spec / architecture:** [spec 1.7.2](../specs/cfd-workbench-v1.md) A3.1 (My sections, Profile revision), A3.2
  (Section library), A4.6, A4.10, A4.14, A4.15 ("Catalog and My sections in the section editor"), CAD-18, CAD-19,
  CAD-21, UI-38, UI-43, COPY-109…115/121; [ADR-0008](../adr/0008-profile-catalog-and-section-library.md);
  [ADR-0007](../adr/0007-edit-transactions-section-draft-and-gesture-commit.md) §4 (Replace is a draft step) and finding 5;
  [ADR-0005](../adr/0005-point-types-in-the-b-spline-record.md); [ADR-0010](../adr/0010-one-placement-rule.md);
  [M1.2c design](m12c-section-editor.md) §3, §5, §11; [app-shell design](app-shell.md) :446, :835, :866;
  [Ruling 71](../notes/rulings.md).
- **Delivery phase:** M1.2d. It follows M1.2c (joined at `56508ca`) and the Ruling 71 early-refusal fix
  (`fix/unique-section-early-refusal`), and it consumes the cross-profile abscissa rule (`design/cross-profile-abscissa`,
  in progress) rather than defining a second one (§3.6).
- **Mockup:** [`docs/mockups/m12d-catalog.html`](../mockups/m12d-catalog.html) (hub
  [`m12d-catalog.md`](../mockups/m12d-catalog.md)). The operator sees it before any build.
- **Evidence:** the Replace probe, [`docs/proof/m12d-catalog/`](../proof/m12d-catalog/) — `probe/Program.cs` runs every
  case through the as-built Core (`DatImport.FitToBasis`, `DatImport.Fit`, `AuthoringSession.ImportPatch`,
  `Geometry.Assess`); `output/table.md` holds the numbers this design cites as **P-…** rows.

## 0. What the operator will see

### 0.1 The demo at the end of M1.2d (packaged `.app`, macOS, the Example foil)

1. Open the Example. Select the Root station, choose **Edit section…**. The chip reads "Shared with Tip".
2. **Section ▾ → Replace from catalog…** The dialog lists **NACA · Eppler · Speer · My sections**, with focus in the
   search field. Eppler rows are listed but disabled, each with "Pending admission — terms requested from UIUC · GEN
   sections remain". Speer H105 is disabled with the cite-only string.
3. Type `4412`. The match count is announced once typing pauses. NACA 4412 (closed TE) is the active row. The section
   shows a **dashed accent outline of NACA 4412 over the current section**, a marker at the largest change, and the
   detail line reads: "Replaces Root and Tip (they share this section). NACA 4412 (closed TE) fits on its own 15 points:
   5.54 µm (0.0046 % chord; limit 10 µm at 120.00 mm). 8 → 15 points per surface; point types reset. Largest change
   4.91 mm at 40 % chord. t/c stays 12.00 % from the Thickness curve; NACA 4412 is 12.01 % thick. Frame: leading edge
   moved 0.036 mm, chord turned 0.17°."
4. **Replace.** The section takes the NACA 4412 shape at Root and Tip and keeps its name, section-a (DR-M12D-5 b). The
   source chip reads "Catalog original · NACA 4412". ⌘Z inside the editor puts the old section back; ⇧⌘Z replaces again.
5. Move one point. The chip reads "Modified from NACA 4412".
6. **Section ▾ → Save to My sections…** Name "Kite root 4412". The dialog reads "Provenance: Modified from NACA 4412 ·
   Rights: GEN · What is saved: 15 points per surface · its own t/c". Save. The status strip says "Saved “Kite root 4412” to My sections". Nothing else changed.
7. Save again with "kite ROOT 4412": "“kite ROOT 4412” is already in My sections. Choose another name." Nothing saved.
8. **Finish section.** One undo step for the whole edit. Open another foil, Replace from catalog… → My sections lists
   "Kite root 4412".
9. The abscissa case: **Make unique to Root**, then Replace from catalog… → NACA 4412. Tip now uses a different section,
   so the shape must fit Root's existing points. The detail line reads "Root blends point-to-point with Tip, so Root must
   keep Tip's 8 points at the same chord positions. On those points NACA 4412 is 26.86 µm off (0.0224 % chord; limit
   10 µm at 120.00 mm). Nothing changed." **Replace** is disabled with that reason; **Replace Root and Tip (Tip changes
   too)** is offered. NACA 0012 on the same points fits at 9.75 µm and **Replace** stays available.

### 0.2 What the operator will NOT see in M1.2d

| Not shown | Why / where |
|---|---|
| NACA 16-012 and the 6/6A-series (63-209, 63-412, 64A410, 66-012, 66-209, 66-018) as choosable rows | They need a TM 4741-faithful generator (A4.6); DR-M12D-2 lists them disabled "Not generated in this build" until that generator lands |
| A cambered catalog section at one station beside a different section | Not certifiable today: the shape needs 15 points per surface (NACA 4412, closed TE), and two different neighbouring sections can have at most 10 (P-E6). DR-M12D-1 option E (raise the blend proof budget) is the route |
| Rename or remove a My sections entry | No spec clause (ADR-0008) |
| Catalog polars, ranking (CAT-01) | Analysis area, not this slice |

## 1. Grounding — what this design must satisfy

Quoted, with the place each binds:

- **CAD-18:** "Given the section editor on a station whose section is shared, when Replace from catalog… opens, then
  NACA · Eppler · Speer · My sections are listed; typing filters the list and the match count is announced once typing
  pauses … When a GEN section is chosen, then a dashed preview shows on the section and the detail line names the fit
  deviation and every station replaced. When Replace is chosen, then the section takes the catalog shape, the chip reads
  'Catalog original · <source>', one Undo inside the draft reverts it, and the A4.6 residual shows; after one point edit
  the chip reads 'Modified from <source>'. Given a disabled entry and Replace, then nothing changes. Given Cancel of the
  section, then the section and every assignment return to their entry state." → §3.6, §11, §12.4.
- **CAD-19:** name required, unique ignoring case, provenance and the source's rights class, available "in another foil",
  byte-identical section/assignments/catalog original, survives section Cancel and document Undo, Cancel/Escape return
  focus to Section ▾ → §3.7, §4.
- **A4.6:** "an approximate path is accepted iff its measured deviation ≤ the model/join tolerance (10 µm) at the
  station's local chord … a path above its acceptance disables Apply with the number." → the Replace acceptance (§3.6).
- **A3.2 Section library:** "An entry name is unique ignoring case; an entry references exactly one immutable Profile
  revision and never changes it; saving never alters the source section, the catalog original or any assignment." → §3.2.
- **A4.10:** GEN / VEND / LINK with reasons; no VEND-pending or LINK coordinates are shipped (ADR-0008 §1).
- **A4.14:** shared editing names every referencing assignment; independent editing changes only the selected one. →
  the Replace scope follows the draft's scope (§3.6).
- **Ruling 71:** "refuse a section step the moment it would make a profile's abscissae differ from a neighbouring
  station's profile, with a plain reason and nothing changed (no unsaveable drafts)". Replace is such a step (§3.6).
- **ADR-0007 §4:** Replace is a section-draft step through the import fit: current basis, then a neighbour's basis, then
  an own 8–16 vertex basis. **This design amends that order** (§3.6, deviation D-1): the own-basis fallback beside a
  different neighbour is exactly the Ruling 71 defect (P-E1).

**Drift surfaced (not silently resolved):**

- *Spec DR-8 default* says a project "pins [an entry] as a content-addressed profile"; ADR-0008 §4 inlines it. This design
  follows ADR-0008 (accepted deviation; the spec owner's amendment is still owed, ADR-0008 follow-ups).
- *ADR-0008 location* names `CFD-Workbench`; the app's installation-preference root is
  `LocalApplicationData/CFD Workbench` (`App.axaml.cs:29-31`). This design uses the existing root (finding F-6).

## 2. Responsibility

M1.2d gives the section editor two verbs: **Replace from catalog…** (a section-draft step that puts an admitted catalog
shape or a My sections entry on the section under edit) and **Save to My sections…** (an installation-scope write of the
current section, outside the document). It owns the bundled catalog content, provenance parsing and the rights
derivation, the My sections store, the Replace basis rule, and the two dialogs.

It is **not** responsible for: the cross-profile basis propagation (the cross-profile design); raising the blend proof
budget (DR-M12D-1 E, a GCRT-class change); catalog polars and ranking (CAT-01); rename/remove of entries; the 6-series
generator (DR-M12D-2).

## 3. Data model (settled first)

### 3.1 Bounded contexts and ubiquitous language

Two bounded contexts meet here: **Profile catalog** (admitted sections and their rights; read-only, bundled) and
**Design** (the foil document, whose profiles are what the section editor edits). **My sections** is a third, small
context: the installation's section library (DR-8, Ruling 53).

| Word (UI) | Meaning | Code |
|---|---|---|
| Catalog entry | one bundled section: family, designation, admission class, reason; GEN entries carry generated coordinates | `CatalogEntry` (new) |
| Family | NACA · Eppler · Speer · My sections — a picker grouping, never an admission class | `CatalogFamily` (new) |
| Admission class | GEN · VEND · LINK (A4.10); VEND is "pending terms" today | `AdmissionClass` (new) |
| My sections entry | a saved section: a name plus one immutable profile record with its provenance | `LibraryEntry` (new) |
| Section (profile) | the profile record in the foil document (degree-5 B-spline pair) | `ProfileDefinition` (as built) |
| Origin | where a profile's shape came from: `gen:<id>`, `vend:<id>`, `dat:<sha256>`, or not recorded | `Provenance.Origin` (new) |
| Modified | the shape was edited after it arrived from its origin | `Provenance.Modified` (new) |
| Rights | the admission class derived from the origin, never stored | `Provenance.Rights` (derived) |
| Point spacing | the knots and control-point x of a section, shared by both surfaces (paired types) | "basis" in code |
| Replace | the section-draft step | `SectionStep.Replace` (new) |
| Blend chain | the stations whose sections must share point spacing: a maximal run of adjacent stations in η order whose neighbouring sections differ | derived (§3.6) |

"Point spacing" is the user word for the basis; "abscissae" never appears in UI copy.

### 3.2 Aggregates and invariants

| Aggregate / entity | Root and identity | The one invariant it protects |
|---|---|---|
| **Profile catalog** (read-only; bundled resource) | catalog id (`naca-0012`, `e817`, `speer-h105`) | Every GEN entry's coordinates regenerate bit-for-bit from its generator id + version and match the recorded SHA-256; VEND-pending and LINK entries carry no coordinates |
| **Section library** (installation scope) | entry = file `<sha256 of its bytes>.foil` | An entry name is unique among entries ignoring case (NFC, then `OrdinalIgnoreCase`); an entry's bytes never change after publication (create-only; the hash is its name) |
| **Foil document** (unchanged aggregate) | the accepted source revision chain | The accepted source denotes exactly one certified shape (FoilDSL §2). Replace adds no new invariant: it is one more step whose result must certify before Finish |
| **Section draft** (session memory; M1.2c) | draft id | Bytes at the cursor = base + steps 1..cursor. **New rule (Ruling 71): a step is accepted only if its bytes keep every distinct neighbouring pair on one point spacing with at most 10 points per surface** (§3.6) |

Value objects: `Provenance` (origin, modified), `ReplaceSource` (shape coordinates or a record, plus its provenance and
display name), `ReplaceReport` (§5.1). The draft refers to the library and the catalog by **value**: a Replace step holds
the source's coordinates or record bytes, not a library reference, so a later library change never alters a draft.

### 3.3 Durable representation, grain, history

**Catalog.** Bundled as an embedded resource of `CfdWorkbench.Core`: one coordinate set per GEN entry, generated by
`CatalogGenerator` from the NACA 4-digit closed form with the **closed trailing edge** (a₄ = −0.1036, DR-M12D-7 — the
form New foil's NACA 0012 already uses) **in the chord frame**: the leading edge is the continuous minimum-x point, the
chord runs to the trailing-edge midpoint at unit length; the LE shift, rotation and scale are recorded with the entry
and reported at Replace. GEN shapes are fitted from these coordinates and **never pass through `DatImport.Parse`**,
whose normalisation (LE at the minimum-x *sample*, shift by its y, scale by xMax − xMin, no rotation) is itself an
unreported frame change (geometry lens F1; finding F-5). Plus one metadata table (family, designation, class, reason, generator id + version, SHA-256). Grain of the
metadata table: **one row is exactly one catalog designation**. No measures. History: none — the catalog changes only
with a build, and the build's hash test is its audit (CI-safe, no runtime writer).

**My sections.** ADR-0008 as decided: one immutable standalone FoilDSL section document per entry, file name = SHA-256 of
its bytes, under `<installation-preference root>/sections/`. Grain: **one file is exactly one saved section**,
identified by its hash, recorded when Save publishes it. The current library is a folder scan (derive, don't store; no
index). Type-2 by construction: an edit saved again is a new entry with a new name; nothing is updated in place. No
measures, so additivity does not apply (stated, not omitted).

Entry document (FoilDSL standalone section form, `foildsl.md` §141-145):

```text
foildsl "4.1"
section "Kite root 4412" { evaluator "cfdw-cv" "2" upper cv {…} lower cv {…} closure open provenance "gen:naca-4412 modified" }
```

(`foildsl.md` :141 grammar; the parser requires `evaluator` then `upper`, `FoilSource.cs:1269-1272`.) The name is stored
NFC-normalised, so the bytes — and so the hash file name — are deterministic. Test: `Library_EntryBytes_ParseAsStandaloneSection`.

**Provenance spelling (ADR-0008 §3, "a design-slice deliverable with a parser test").** The `provenance` string is
`<origin>[ modified]` where `<origin>` is one of:

| Origin | Written by | Rights (derived) | Chip source text |
|---|---|---|---|
| `gen:<catalog-id>` | Replace from a GEN entry | GEN | the catalog designation, "NACA 4412" |
| `vend:<catalog-id>` | never in M1.2d (no VEND coordinates ship) | VEND | the designation |
| `dat:sha256:<hex>` | Import .dat… (DR-M12D-3) | Your file | "a .dat file" |
| *legacy* `selig sha256:<hex> points:<n>` / `lednicer …` | as-built DAT import (`DatImport.ProvenanceOf`) | Your file | "a .dat file" |
| missing, empty or unknown | Example sections, hand-written sources | **Not recorded** (never guessed) | "Source not recorded" |

**One writer of `modified`:** `Provenance.MarkModified`, run after every section step other than Replace whose shape
change exceeds the identity tolerance (A4.5 profile oracle); a legacy DAT string keeps its text and gains ` modified`
(no Type-1 loss). Save copies the draft's provenance **verbatim**; Replacing *from* an entry copies the entry's
provenance verbatim. Inner Undo restores the bytes, and with them the chip. The `dat:` spelling is `dat:sha256:<hex>`
everywhere (ADR-0008 §3 writes `dat:<sha256>`; one-line amendment with F-6).

**Provenance is not in the model today (persistence lens, Verified):** the parser reads the string and drops it
(`FoilSource.cs:1230`), `ProfileDefinition` (`:103`) has no field, and `Print` writes no provenance line, so a command
that reprints the document (e.g. a channel point-type change, `AuthoringSession.cs:1071`) erases it. CAT therefore owns
`FoilSource.cs`: `ProfileSource` and `ProfileDefinition` gain `string? Provenance` (outside identity), and `Print` writes
it. Test: `Provenance_SurvivesEveryRewriter` (WriteSurfaces, WriteSideTangents, Insert, Delete, MakeIndependent, Fair,
Rebuild, Print). So
"Modified from NACA 4412" survives save → reuse in another foil, and the rights class always follows the origin. A
chain (`section:<sha>`) is not recorded (ADR-0008 alternatives). `provenance` stays outside geometry identity
(`foildsl.md` §8 :365).

**Writer and compute reader per persisted field:**

| Field | Writer | Readers |
|---|---|---|
| profile block bytes after Replace | `SectionReplace.Patch` (via `PatchSectionStep`) | `Geometry.Assess`, `Placement`, `Sections.View`, CLI `inspect` |
| `provenance` (document) | `SectionReplace.Patch` (origin); `Provenance.MarkModified` (the only writer of ` modified`); every block rewriter preserves it | `ProfileDefinition.Provenance` → `Provenance.Parse` → the source chip, the Save dialog's provenance line, the library entry |
| library entry file | `SectionLibrary.Save` | `SectionLibrary.Scan` → the My sections family in the dialog |
| catalog resource | build (generator) | `Catalog.Load` → the dialog; `CatalogTests` hash check |

**Migration.** None. The document grammar is unchanged (a `provenance` string has always been legal on a profile). A
build older than M1.2d opens a document whose provenance reads `gen:naca-4412` exactly as any other string (it is
outside identity and has no reader there). The library folder is new; an older build never reads it.

### 3.4 Rights and admission (derived)

`Provenance.Rights(origin)` is a pure function: `gen:` → GEN; `vend:` → VEND; `dat:` and legacy DAT → **Your file**
(user-supplied, the designer's own responsibility, A4.10 CAT-02); otherwise **Not recorded**. It is never stored.
Choosability in the dialog: a GEN entry and every My sections entry can be chosen; a VEND-pending entry and a LINK entry
are listed `aria-disabled` with their reasons (COPY-109/110) and carry no coordinates, so "disabled but reachable" cannot
leak a shape. A My sections entry whose rights read VEND cannot exist (no VEND coordinates ship); the scan reports one if
a hand-placed file claims it (§9).

### 3.5 Catalog content in M1.2d (DR-M12D-2)

| Family | Rows | Class | Choosable in M1.2d |
|---|---|---|---|
| NACA | 0009, 0012, 4412 | GEN (closed form) | yes |
| NACA | 16-012, 63-209, 63-412, 64A410, 66-012, 66-209, 66-018 | GEN (needs the TM 4741 generator) | **no** — disabled "Not generated in this build. GEN sections above can be used." (COPY-190, proposed) |
| Eppler | E817, E818, E874, E904, E908; strut E836, E837, E838 | VEND pending | no — COPY-109 |
| Speer | H105 | LINK | no — COPY-110 |
| My sections | the folder scan | derived from origin | yes |

E862–E864 are fairings and never appear (A4.10). **assume:** the 6-series fit on a 10-point spacing behaves like 4412
(over 10 µm); confirm with the generator spike; if wrong, more GEN rows become usable beside a different section.

### 3.6 The Replace rule and the shared point-spacing constraint (Ruling 71)

**The constraint, measured.** `Geometry.Assess` certifies the Rule A blend only when each pair of neighbouring stations
with different sections has equal knots and equal control x (`SharedAbscissa`, `Geometry.cs:262-271`, `:383-384`), and
only when each section has at most 5 polynomial pieces per surface (`spanCount * 48 <= 256`, `Geometry.cs:385-399`) —
**10 points per surface when every interior knot is single** (P-E6: 10 Certified; 11 and 12 Not assessed). The budget is
necessary, not sufficient: the depth bound and the proof budget can still return Not assessed, so every accepted step is
also checked by the certificate on its result bytes. Upper and lower share one
spacing (paired types, Ruling 60). In a certified document, every run of differing neighbours therefore shares one
spacing. A catalog section arrives with its own spacing.

**Measured on the as-built Core (the probe, `docs/proof/m12d-catalog/output/table.md`):**

| Row | Case | Result |
|---|---|---|
| P-CF-0012 | NACA 0012 (closed TE) on the Example's 8 points, 120 mm chord | 9.75 µm (the open-TE form: 9.70 µm) — within 10 µm; Certified at unique Root; largest change 0.79 mm at 11 % chord |
| P-CF-0009 | NACA 0009 (closed TE) on the Example's 8 points | 7.31 µm — within |
| P-CF-0012-N | NACA 0012 (closed TE) on New foil's 10 points (sqrt) | 0.18 µm — within |
| P-CF-4412-A | NACA 4412 (closed TE) on the Example's 8 points | 26.86 µm — over |
| P-CF-4412-R | the same on the Example's spacing refined to 10 points by exact insertion | 16.91 µm — over |
| P-CF-4412-N | the same on New foil's 10 points (sqrt) | 17.21 µm — over |
| P-CF-4412-own | the same on its own sqrt spacing, 8…16 points | 32.9, 28.9, 17.2, 19.5, 10.1, 11.7, 10.1, **5.54 (15)**, 7.10 µm — 15 is the smallest within 10 µm |
| P-CF-4412-frame | the 4412 chord frame | LE moved 3.0e-4 chord (0.036 mm), chord turned 0.17°, scale 1.0000 |
| P-CF-4412-open | NACA 4412 with the standard **open** TE, on every spacing | ≥ 19.9 µm: its two trailing-edge ends sit at different x (1 and 0.9998), which a record whose surfaces share x cannot hold |
| P-CF-rule3 | Rule 3: shared Replace in place, 4412 (closed TE) on its own 15 points | **Certified**; largest change 4.91 mm at 40 % chord |
| P-E1 | As built: Import 4412 at Root of the shared Example | own spacing chosen → **Unsupported: abscissae differ** (the Ruling 71 defect, a second instance) |
| P-E2/E5 | As built: Import after Make unique (Example, New foil) | **refused `DSL-PATCH`**: the old profile would be left unreferenced |
| P-E4 | Option C: common sqrt spacing, Tip refitted (DAT path) | n = 10 certifies but 4412 is over; n = 12: **Not assessed (budget)** |
| P-E6 | Two differing sections on one sqrt spacing | n = 10 Certified; n = 11, 12 **Not assessed (budget)** |
| P-E7 | My sections entry already on the shared spacing | exact copy (0 change), Certified |
| P-B3 | Three stations Root X, Mid X, Tip Y; B over {Mid, Tip} only vs over every station | {Mid, Tip}: **Unsupported** (Root ↔ Mid differ); every station (one block, all re-pointed, unreferenced blocks deleted): parses, **Certified** |

Residuals in the P-CF rows are the **largest Euclidean distance** from the closed form (201 cosine samples per surface)
to the fitted curve; the as-built `FitToBasis` vertical residual is printed beside each in the receipt. The first
draft's rows through `DatImport.Parse` (`table.md` "NACA …" and E1c/E3/E8/E9) are kept in the receipt as the as-built
DAT path and are **superseded** here: that path moved the 4412 leading edge and so overstated the fits (43.54 µm on the
Example's spacing against 26.86 µm in the chord frame).

**Options:**

| | Rule | Evidence | Verdict |
|---|---|---|---|
| **A** | Fit the shape onto the section's current spacing (ordinates only). Accept if the fit ≤ 10 µm at the largest local chord of the stations replaced; otherwise refuse with the number | Keeps point count, ids and types; certifies when accepted (P-CF-0012). Symmetric sections fit (P-CF-0012/0009); cambered do not (P-CF-4412-A/N) | needed — the only certifiable route beside a different neighbour |
| **B** | Replace at **every station of the foil**, so no differing neighbour remains; fit on the shape's own spacing | P-CF-rule3: 4412 certifies at 5.54 µm on 15 points; P-B3: a partial chain cannot certify, every station can | needed — the only route that admits cambered sections today; it changes the other stations, so it is an explicit, named action, never automatic |
| **C** | Fit on a new common spacing and refit the differing neighbours onto it (≤ 10 µm, reported) | P-CF-4412-N: at 10 points 4412 is 17.21 µm over; P-E6: 11 or more points break the blend budget | rejected for M1.2d: it changes a neighbour by approximation and does not admit 4412 within budget |
| **D** | Accept a fit over 10 µm with a warning (Rhino Rebuild's convention: report the deviation and let the user decide; the Ruling 56 precedent for typed chords) | 17.21–26.86 µm on a 120 mm chord | operator's call (an A4.6 change); not recommended for M1.2d. If chosen, the chip reads "Fitted to NACA 4412 · 26.86 µm", never "Catalog original" |
| **E** | Raise the blend enclosure budget so 15-point sections can sit beside each other, then use C with n = 15 | Not measured (proof change, GCRT class) | follow-up spike; the real route to "cambered root, symmetric tip" |

**Chosen rule (recommendation, DR-M12D-1 = A + B):**

1. **Scope.** Replace acts on the draft's scope: *Shared* → every station that uses this section (A4.14); *Unique* →
   this station. The section block is **rewritten in place and keeps its name** (DR-M12D-5 b): no assignment is
   re-pointed, no profile is left unreferenced (fixes P-E2/E5), and no new block is added. Keeping the name matters
   because the draft's scope is derived by name today (`AuthoringSession.cs:1308`, also `:1254`, `:1268`, `:1386`,
   `:1563`; `ShellHost.cs:1022`); a rename would make a shared Replace read as Independent. **Replace at every station**
   (rule 4, option B) rewrites the edited block, re-points every assignment of the foil to it and deletes every block
   left unreferenced, in one patch (P-B3); the edited name survives, so the draft reads Shared.
2. **Neighbours.** D = the stations adjacent (in η) to the replaced set that are **not in it**. Whether a neighbour
   "differs" is never decided by name: a step is accepted only when the shared predicate passes **and** the certificate
   passes on the result bytes. (In a certified base the current spacing is the spacing of every geometrically differing
   neighbour; that explains the rule and is not relied on as a guarantee.)
3. **D empty** (every station uses this section — the Example as opened): try the current spacing first (keeps points
   and types; accepted if within 10 µm — 0012: 9.75 µm, 8 points, P-CF-0012), else the smallest own sqrt spacing of
   8–16 points within 10 µm, **scanning n = 8, 9, … 16 in order, never bisecting** — the error is not monotone in n
   (4412: 12 → 10.11 µm, 13 → 11.73 µm; 15 → 5.54 µm, 16 → 7.10 µm), so 4412 lands on 15 points, 5.54 µm
   (P-CF-4412-own; test `Replace_SharedExample4412_OwnSpacing15PointsCertified` pins 15). None within → Replace disabled with the best
   number (A4.6).
4. **D non-empty:** fit on the current spacing only (option A). Within 10 µm → Replace. Over → **Replace is disabled**
   with COPY-191 (the number in µm and % chord, the limit, the neighbour named, in a designer's words), and **Replace
   Root and Tip (Tip changes too)** — with more stations, **Replace at every station (… change too)** — is offered
   (option B), naming every station it changes and previewing each (the strip thumbnails carry the preview). B's own
   fit may need more than 10 points; that is valid (no differing neighbour remains) and the detail line discloses it
   (COPY-193).
5. **Acceptance** = 10 µm at the **largest** local chord among the stations replaced, shown in µm and in % chord
   (A4.6). **The residual** is the largest Euclidean distance from the source (201 cosine samples per surface of the
   closed form, or of the entry's record) to the fitted curve, plus every knot, in the declared chord frame — the A4.5
   profile sample set, not the vertical gap at the catalog points (geometry lens F7: for a 16-point 4412 the vertical
   gap at the nose was 94 µm while the true distance was 7 µm). The distance is taken **both ways** — source samples to
   the fitted curve, and the fitted curve's 201 samples plus knots to the source — and the larger is reported, so a
   bulge between source samples is seen (geometry re-check, Minor 1). This replaces the as-built 1e-5 chord fraction (ADR-0007 finding 5): stricter than A4.6 under 1 m,
   looser above.
6. **A My sections entry** already on the target spacing is copied exactly (P-E7: residual 0, reported "exact copy");
   otherwise it is sampled (201 cosine points per surface) and fitted like a catalog shape.
6b. **Ids, types and tangent rows.** On the current spacing the point ids are kept exactly; Horizontal and Fixed-angle
   rows become KKT equality rows of the fit; a Vertical row's sign condition is checked after the solve; a conflict
   refuses the Replace and names the row. On an own spacing the rows are dropped and listed in
   `ImportReport.DroppedRows` (new), and the detail line says "point types reset". The as-built `FitToBasis` pins only
   the endpoints and writes fresh ids (`DatImport.cs:281-283`, `:420-433`), so RPL extends it.
6c. **Thickness (what the station builds).** Each station scales the section to its Thickness-curve t/c (A4.4), so a
   source whose own t/c differs from the station's is not what the station builds (NACA 0009 at a 12 % station is
   NACA 0012). When they differ by more than the identity tolerance at the station's chord, the detail line says
   "NACA 0009 is 9.00 % thick; Root and Tip will be scaled to 12.00 % (Thickness curve).", the sheet offers "Use NACA
   0009's t/c at these stations" (the existing Station t/c from this section step, ThicknessIntent.UseSource — it edits
   the Thickness curve, and its change is reported on the A4.5 distribution-curve sample set, as built), and the
   chip reads "Catalog original · NACA 0009 · scaled to 12.00 % t/c".
7. **The budget is part of the same refusal predicate** (finding F-1, sent to the cross-profile design): ≤ 5 pieces per
   surface (10 points with single knots) **plus** the certificate on the result. A later step that would make two
   neighbouring sections differ while either is over the budget is refused at once ("Root and Tip have 15 points;
   neighbouring sections that differ can have at most 10. Rebuild to 10 points first, or edit Root and Tip together."
   — COPY-194). Without this, B creates a trap one Make-unique edit later.

**One rule, not two (coordination).** Replace calls the **same predicate** the Ruling 71 fix adds for every section step
(`SectionGuards.NeighbourSpacing(bytes, assignment)`, owned by `fix/unique-section-early-refusal`; M1.2d consumes it and
asks for the budget clause, F-1). The cross-profile design's propagation (exact knot insertion pushed to the neighbour;
paired x moves) cannot help Replace: a catalog shape needs a different x-mapping, which insertion never produces (P-E4).
If the operator later approves C-style neighbour refits in that design, DR-M12D-1 C becomes available through the same
predicate, not a second rule.

### 3.7 Save to My sections

Save takes the section **at the draft cursor** (what the editor shows), with its provenance copied verbatim, writes one
entry, and changes nothing else:
the draft, the cursor, the inner undo stack, the document, its undo depth and dirty flag, every assignment and the
catalog are byte-identical before and after (CAD-19). It is not a draft step and never enters document history, so
section Cancel and document Undo never remove it. Refusals (each leaves the library unchanged):

| Condition | Copy | Code |
|---|---|---|
| empty or whitespace name | COPY-113 "Name the section to save it." | `LIB-NAME-EMPTY` |
| name equal to an existing entry ignoring case (NFC) | COPY-114 | `LIB-NAME-DUPLICATE` |
| the section's surfaces cross (draft not certifiable alone) | COPY-195 "Fix the crossing before saving this section." (DR-M12D-4) | `LIB-SECTION-INVALID` |
| another save holds the claim (live) | COPY-196 "Another save is in progress. Try again in a moment." | `LIB-CLAIM-HELD` |
| the claim outlived its save (still held on retry) | COPY-196b "My sections is locked by a save that didn't finish. If no other CFD Workbench is open, delete “sections/.cfd-writer.claim” and save again." | `LIB-CLAIM-HELD` |
| platform publication not proved | ADR-0004's `DOC-UNSUPPORTED-PERSISTENCE` copy | `DOC-UNSUPPORTED-PERSISTENCE` |
| write failed | COPY-197 "Couldn't save to My sections: <cause>. Nothing was saved." | `LIB-IO` |

## 4. Persistence

- **Root:** `SectionLibrary.Root(preferenceRoot) = Path.Combine(preferenceRoot, "sections")`, with `preferenceRoot` the
  value `App.axaml.cs:29-31` already passes to `PreferenceStore` — one function, one root (F-6). Tests inject a temp root.
- **Save:** as built, `ProjectStore` takes the claim only for replace saves (`ProjectStore.cs:106-108`) and holds it
  inside one call, so a scan-then-check cannot run under it. LIB therefore owns `ProjectStore.cs` and adds
  `PublishUnderClaim(dir, fileName, bytes, Func<bool> precondition)`: claim (`.cfd-writer.claim`) → run the precondition
  (re-scan, name check) under the claim → write `<tmp>` → fsync → create-only link to `<sha256>.foil` → re-hash the
  published bytes and compare to the name → release. A stale claim is **reported, never deleted by age** (as built); the dialog offers COPY-196 and the status
  strip names the claim path's folder in Technical details (ADR-0008 consequence).
- **Scan:** list `*.foil`, parse each under the FoilDSL limits (SRC-09, ≤ 1 MiB), check hash = name, check one section,
  collect names; **two entries whose names collide** (a hand-copied file) are both listed disabled with COPY-199 (ADR-0008
  §2: "reports, never hides"). Problems are listed in the dialog's My sections group as disabled rows with a reason ("“<file>” is
  damaged and was skipped.", COPY-198), never hidden, never repaired.
- **Using an entry in a foil** inlines it (ADR-0008 §4) through Replace.

## 5. Contracts

### 5.1 Exposed — Core (new unless marked)

```csharp
public enum AdmissionClass { Gen, Vend, Link }
public enum CatalogFamily { Naca, Eppler, Speer, MySections }
public sealed record CatalogEntry(string Id, CatalogFamily Family, string Designation, AdmissionClass Class,
    string? DisabledReason /* COPY-109/110/190; null = choosable */, byte[]? Coordinates /* GEN only */);
public static class Catalog { public static IReadOnlyList<CatalogEntry> Load(); }          // throws CAT-UNAVAILABLE
public static class CatalogGenerator { public static byte[] Naca4(string digits); public const string Id = "naca4-closed/1"; }

public sealed record Provenance(string? Origin, bool Modified)
{
    public static Provenance Parse(string? text);       // legacy DAT forms map to dat:; unknown → Origin null
    public string Format();                             // "<origin>[ modified]"; null origin → ""
    public RightsClass Rights { get; }                  // Gen · Vend · YourFile · NotRecorded (derived)
    public string ChipText(string sourceName);          // "Catalog original · <s>" / "Modified from <s>" / "Source not recorded"
}

public abstract record ReplaceSource(string DisplayName, Provenance Provenance)
{
    public sealed record Coordinates(string DisplayName, Provenance Provenance, byte[] Selig) : ReplaceSource(DisplayName, Provenance);
    public sealed record Record(string DisplayName, Provenance Provenance, byte[] SectionDocument) : ReplaceSource(DisplayName, Provenance);
}
public enum ReplaceScope { Draft, BlendChain }
// SectionStep gains (Contracts.cs :92-109):
public sealed record Replace(ReplaceSource Source, ReplaceScope Scope) : SectionStep;

public sealed record ReplacePreview(IReadOnlyList<int> Stations, string Spacing /* current · own-<n> · exact */,
    double FitResidual, double AcceptanceChord, double LargestChangeChord, double LargestChangeAtX, int PointsPerSurface,
    string? RefusalCode /* CAT-SPACING · CAT-RESIDUAL · null */, IReadOnlyList<int>? BlendChain, byte[]? Bytes);
public static class SectionReplace
{
    public static ReplacePreview Preview(byte[] draftBytes, int assignment, SectionScope scope, ReplaceSource source, ReplaceScope replaceScope);
}
```

- `PatchSectionStep` gains `case SectionStep.Replace` → `SectionReplace.Patch` (uses `Preview`, throws `ContractError`
  with the refusal code when `RefusalCode` is set). `SectionStepKind` gains `"replace"`.
- `SectionStepReport.Import` (as built, `ImportReport`) is **kept** and filled for Replace: `MaxResidual` (the rule-5
  Euclidean residual), `VertexCount`, `Accepted`, `Provenance` (the formatted string), `Basis` (`current` · `own` ·
  `exact`). `ImportReport` gains optional fields (expand-only): `Stations`, `DroppedRows`, `FrameLeShift`,
  `FrameRotationDegrees`, `FrameScale`, `SourceThickness`. No receipt field changes: Finish's receipt stays `rail "section"` (M1.2c §3.3).
- **Import .dat… becomes Replace** with `ReplaceSource.Coordinates` (DR-M12D-3): `SectionStep.Import(byte[])` maps to
  `Replace(new Coordinates("a .dat file", dat:sha256:<hex>, bytes), Draft)`. `ImportPatch` (`AuthoringSession.cs:705-760`)
  and `NeighbourBases` (`:681-703`) are deleted in the same change once their callers move (expand-migrate-contract in
  code).
- `Provenance.MarkModified(bytes, profile, before)`: after any non-Replace step whose profile shape changed beyond the
  identity tolerance, the post-step adds ` modified` to a provenance that has an origin (the only writer of the flag,
  called from `PatchSectionStep`'s common tail).
- `FoilSource` (CAT): `ProfileSource`/`ProfileDefinition` gain `string? Provenance`; `Print` writes it; outside identity.

### 5.2 Exposed — Persistence (new)

```csharp
public sealed record LibraryEntry(string Name, string Hash, Provenance Provenance, byte[] Bytes);
public sealed record LibraryScan(IReadOnlyList<LibraryEntry> Entries, IReadOnlyList<(string File, string Reason)> Problems);
public sealed class SectionLibrary(string root)
{
    public static string Root(string preferenceRoot);
    public LibraryScan Scan();
    public LibraryEntry Save(string name, byte[] profileBlockBytes, Provenance provenance);  // throws LIB-* codes
}
```

### 5.3 Exposed — Desktop

- Commands (CommandTable `SectionMenu`): `section.replace-catalog` "Replace from catalog…", `section.save-mine` "Save to
  My sections…"; the existing `section.import-dat` "Import .dat…" keeps its row and now runs Replace.
- `WorkbenchController`: `OpenCatalog()`, `PreviewReplace(CatalogChoice)` → `ReplacePreview` (off the UI thread,
  generation-tagged; a newer preview cancels an older one), `ApplyReplaceAsync(ReplaceScope)`, `SaveToMySectionsAsync(name)`.
- `SectionCanvas`: a `Preview` overlay — the replaced section as a dashed accent outline over the current section.
- `CatalogDialog` (APG combobox + grouped listbox, UI-38) and `SaveSectionDialog` (labelled Name field, provenance line,
  inline error).

### 5.4 Consumed

| Contract | Source | Established |
|---|---|---|
| `DatImport.FitToBasis`, `DatImport.Fit`, `DatImport.Parse` | `DatImport.cs:169-291`, `:18-167` | **run** (probe) |
| `Geometry.Assess` (`SharedAbscissa`, the node budget) | `Geometry.cs:262-271`, `:383-399` | **run** (P-CF rows, P-E4, P-E6, P-B3) |
| `FoilSource.MakeIndependent`, `InsertProfileKnot`, `MaxOrdinateDeviation` | `FoilSource.cs:447`, `:707`, `:786` | **run** (probe) |
| Section draft API, `SectionStepReport`, `PatchSectionStep` | `AuthoringSession.cs:1397-1462`, `Contracts.cs:92-116` | read |
| The Ruling 71 predicate `SectionGuards.NeighbourSpacing` | `fix/unique-section-early-refusal` (not yet written) | **assume:** it returns a refusal code and plain reason for a step's bytes; confirm at its merge; if its shape differs, RPL adapts its one call site |
| Same-directory claim and atomic publish | `ProjectStore` (`application.md` §6) | read; LIB reuses, does not reimplement |
| Avalonia `AutoCompleteBox`/`ListBox` grouping | — | **not used**: the combobox is built from `TextBox` + `ListBox` like the M1.2c Points grid, so the APG roles are ours to set (spike-free) |

## 6. Patterns and structure

| Pattern | Where | Why (and the Simplifier's check) |
|---|---|---|
| **Command** (as built: section step) | `SectionStep.Replace` | Replace must be one inner-undo step (ADR-0007); reusing the step list costs nothing new |
| **Specification** (a predicate object) | `SectionGuards.NeighbourSpacing` (owned by Ruling 71) | one rule for "may these bytes enter the draft"; Replace is a caller, not a second rule |
| **Value object** | `Provenance`, `ReplaceSource` | derive rights and chip text from two fields; no stored class |
| **Repository over a folder** (no index) | `SectionLibrary` | ADR-0008: scan of immutable files; no SQLite, no facts file |
| **Latest-wins async** (as built: mesh requests) | `PreviewReplace` | typing fires previews; only the newest result paints (`WorkbenchController.cs:433-434` precedent) |

**Solution-Selection Ladder.** YAGNI: rename/remove, polars, ranking, a 6-series generator are out. Reuse: the import
fit, the section step list, the claim/publish, the latest-wins request, the preference root. Stdlib: `SHA256`,
`string.Normalize(NFC)`, `StringComparer.OrdinalIgnoreCase`. No new dependency. **`simplify:`** the catalog is 3 GEN
rows + metadata; ceiling: a generator per family; upgrade trigger: DR-M12D-2's generator spike.

## 7. Error and concurrency model

- Core functions are pure over bytes; `ContractError` codes: `CAT-UNAVAILABLE` (resource missing or hash mismatch),
  `CAT-NOT-ADMITTED` (a disabled row reached Replace — defence in depth), `CAT-SPACING` (over 10 µm on the shared
  spacing beside a differing neighbour), `CAT-RESIDUAL` (no spacing within 10 µm when unconstrained), `DSL-PROFILE-TARGET`
  (as built), and the Ruling 71 predicate's codes.
- The preview runs off the UI thread with a generation number; Replace re-validates against the draft's **current**
  generation (a stale preview is never applied: `DSL-STALE`, as built for drafts).
- Library: one process-wide claim per root; two processes are serialised by the claim (ADR-0008 test). Scan is
  read-only and may run concurrently with a save (a half-written temp file is not `*.foil`).

## 8. Change-surface list (E7)

| Surface | Replace | Save to My sections |
|---|---|---|
| store | source bytes (profile block, assignments, provenance) | library file |
| model | `SectionStep.Replace`, `ReplacePreview`, `Provenance` | `LibraryEntry`, `LibraryScan` |
| service | `SectionReplace`, `PatchSectionStep`, the Ruling 71 predicate | `SectionLibrary` |
| projection/wire | `SectionStepReport.Import` (no CLI reader of profile provenance exists; none added) | — |
| client type | `WorkbenchController` preview/apply state | controller save state |
| UI | dialog, canvas preview, source chip, detail line, status strip, Undo label "Undo Replace" | Save dialog, provenance line, live region, My sections group |
| compute reader | `Geometry.Assess`, `Placement`, `Sections.View` | the dialog's scan; Replace from an entry |

## 9. Failure-mode analysis

| # | Mode | Disposition | Test (track) |
|---|---|---|---|
| FM-1 | Catalog resource missing or hash mismatch | detect + degrade: the dialog shows the catalog-unavailable state (C2 row) with Cancel; My sections still listed | `Catalog_HashMismatch_CatUnavailable` (CAT), `CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel` (DLG) |
| FM-2 | A disabled row reaches Replace (keyboard Enter, stale UI) | prevent (no coordinates) + detect (`CAT-NOT-ADMITTED`); nothing changes | `Replace_VendEntry_NoCoordinatesNothingChanges` (RPL), `CatalogDialog_PendingRowEnter_NothingChanges` (DLG) |
| FM-3 | Replace beside a differing neighbour over 10 µm | prevent: refused with the number; Replace at the blend chain offered | `Replace_UniqueRootCambered_RefusedCatSpacingNothingChanged` (RPL), `CatalogDialog_SpacingRefusal_ReasonAndChainOffered` (DLG) |
| FM-4 | Replace leaves a profile unreferenced (as-built P-E2) | prevent: rewrite in place, keep the name | `Replace_AfterMakeUnique_NoOrphanParses`, `ImportDat_AfterMakeUnique_NoDslPatch` (RPL) |
| FM-4b | A shared Replace reads as Independent (scope derived by name) | prevent: keep the name (DR-M12D-5 b) | `Replace_SharedKeepsName_ScopeStaysShared` (RPL) |
| FM-5 | Replace at one station of a shared section falls back to an own spacing (as-built P-E1) | prevent: scope = draft scope; own spacing only when no differing neighbour | `Replace_SharedExample4412_OwnSpacing15PointsCertified`, `ImportDat_SharedExample_OwnSpacingNotUnsupported` (RPL) |
| FM-6 | B creates > 10 points; a later edit after Make unique cannot certify | prevent: the Ruling 71 predicate refuses that edit with COPY-194; B's detail line discloses | `Guard_FivePiecesDiffering_Certifies`, `Guard_SixPiecesDiffering_RefusedCopy194` (RPL, on the shared predicate), `Replace_ChainOwnFifteen_DisclosesCount` (RPL) |
| FM-7 | Fit acceptance at the wrong chord (smallest instead of largest) | prevent: largest chord of the replaced set | `Replace_AcceptanceUsesLargestReplacedChord` (RPL) |
| FM-7b | A frame change or a vertical-gap residual hides the true deviation (cambered nose) | prevent: closed form in the chord frame; Euclidean residual; frame reported | `Catalog_GenNeverThroughDatParse` (CAT), `Replace_ResidualEuclidean201PlusKnots_NotVerticalGap`, `Replace_Frame_ReportsLeShiftRotationScale` (RPL) |
| FM-7c | Option B over part of a longer foil leaves a differing pair | prevent: B is every station, one block | `Replace_ThreeStationsEveryStation_OneBlockCertified` (RPL) |
| FM-7d | The chip names a section the station does not build (t/c scaling) | prevent: scaled suffix and the UseSource option | `Replace_SourceThicknessDiffers_ReportsScaledTc` (RPL), `SourceChip_StationTcDiffers_SaysScaled` (DLG) |
| FM-8 | Stale preview applied after more typing or a draft step | prevent: generation check | `Replace_StalePreviewAfterTypingOrStep_Refused` (RPL), `Controller_PreviewLatestWins_OlderDropped` (CTL) |
| FM-9 | Inner Undo after Replace leaves the chip or the name wrong | prevent: chip derives from bytes | `Replace_InnerUndo_RestoresBytesAndChip` (RPL) |
| FM-10 | Cancel after Replace keeps a changed assignment | prevent: Cancel restores the base bytes (as built) | `Replace_CancelSection_AssignmentsAtEntry` (RPL) |
| FM-11 | Provenance lost, dropped by a rewriter, or not marked modified after an edit | prevent: one post-step writer; every rewriter keeps the line | `Provenance_MoveAfterReplace_MarkedModified`, `Provenance_SurvivesEveryRewriter`, `Provenance_LegacyDatForm_ParsesAsDat`, `Provenance_Unknown_NotRecordedNeverGuessed` (CAT) |
| FM-12 | Duplicate name across case/normalisation (é vs e + ◌́) | prevent: NFC then OrdinalIgnoreCase under the claim | `Library_DuplicateIgnoringCaseNfc_Refused` (LIB) |
| FM-13 | Two processes save the same name at once | prevent: claim-scoped publish + re-scan | `Library_TwoProcessSameName_OneWins` (UXR, readiness ring) |
| FM-13b | Two entries with one name already in the folder (hand-copied) | detect: both listed disabled, COPY-199 | `Library_ScanNameCollision_BothReported` (LIB) |
| FM-14 | Stale claim after a crash | detect + report (never delete by age), COPY-196b names the file | `Library_StaleClaim_ReportedNotDeleted` (LIB) |
| FM-15 | A damaged or renamed file in the folder | detect: listed disabled with COPY-198 | `Library_HashNotName_ListedAsProblem` (LIB) |
| FM-16 | Save changes the draft, document or undo depth | prevent: Save reads bytes only | `Controller_SaveMine_DraftDocumentUndoUnchanged` (CTL) |
| FM-17 | Save of a crossing section | prevent: refused COPY-195 (DR-M12D-4) | `Library_CrossingSection_RefusedInvalid` (LIB) |
| FM-18 | Library write fails (disk full, permission) | detect + recover: COPY-197, nothing saved, temp removed | `Library_WriteFails_NothingPublished` (LIB) |
| FM-18b | Platform publication not proved | prevent: refused, nothing written | `Library_UnprovedPlatform_RefusedNothingSaved` (LIB) |
| FM-19 | Oversized or hostile file in the folder | prevent: SRC-09 limits on scan | `Library_OversizedFile_SkippedAsProblem` (LIB) |
| FM-20 | Preview compute slow on 15–16-point fits | detect: `catalog.preview` duration; budget below | `Readiness_ReplacePreview_Under50Ms` (UXR, readiness ring), `Replace_Preview_EmitsCatalogPreviewOutcome` (RPL) |

## 10. Telemetry (normal path, no flag)

Events go to the as-built 256-event session ring; no names, paths or coordinates (privacy).

| Event | Fields | Answers |
|---|---|---|
| `catalog.open` | entry count per family, disabled count, scan problem count, load ms, outcome | did it load, how slow, how often damaged |
| `catalog.preview` | family, class, spacing kind, points, fit residual (chord), acceptance, outcome (ok · cat-spacing · cat-residual), ms | how often the spacing refusal fires — the DR-M12D-1 E trigger |
| `section.step` (as built) | `kind = replace`, scope (draft · chain), stations count, residual, outcome | Replace use and outcome |
| `library.save` | outcome code (`saved` · `LIB-*`), ms | duplicates, claims, failures |
| `library.scan` | count, problems, ms | library size and health |

Readiness (measured, not gated per track; UXR group): `Readiness_ReplacePreview_Under50Ms`, `Readiness_LibraryScan100_Under100Ms`.

## 11. UI and interaction design

Archetype: unchanged — the M1.2c section editor (routing: a mode with modal dialogs; data: the canvas). Tokens: the
existing design language (`DESIGN.md`); the preview uses the accent token dashed (UI-38), the current section the
foil token; no new tokens.

### 11.1 Key screens (the mockup draws each)

1. **Section ▾ open** — Replace from catalog… · Save to My sections… · Import .dat… · Smooth… · Make unique to Root ·
   Station t/c rows.
2. **Replace sheet, NACA 4412 active, shared section** — the sheet sits at the right of the model area so the section
   stays visible (the Fusion/Onshape command-panel pattern); families grouped; Eppler and Speer rows disabled with a
   tag and their full reason in the detail line; the canvas shows the dashed accent preview and a marker at the largest
   change; both strip thumbnails show the preview; **Replace** primary, Cancel. Keyboard focus stays in the sheet;
   pointer zoom and pan on the canvas stay available so the nose can be inspected.
3. **Replace applied** — chip "Catalog original · NACA 4412"; status strip "Replaced Root and Tip with NACA 4412. Fit
   5.54 µm (limit 10 µm). ⌘Z puts the old section back."; Points pane shows 15 points; the section keeps its name.
4. **Save to My sections** — Name field, provenance line "Modified from NACA 4412 · rights GEN", Save/Cancel; the
   duplicate-name error inline; the empty-name error.
5. **Spacing refusal (unique Root, 4412)** — Replace disabled with COPY-191; **Replace at Root and Tip** offered; the
   preview still drawn (so the user sees what was refused).
6. **Spacing fit (unique Root, 0012)** — Replace enabled; "Fits Root's 8 points: 9.75 µm (0.0081 % chord; limit 10 µm
   at 120.00 mm). Points and types kept."
7. **My sections from any foil** — a saved entry previewed with the same readout (exact copy on its own points), a
   damaged file listed with COPY-198.
8. **My sections empty** and **catalog unavailable** states (C2 rows).

The Browser row and the station card show "section-a · NACA 4412" after Replace (the name, then the source), so the
source is visible outside Properties (marine-CAD lens F3).

### 11.2 Copy (proposed COPY-190…198; spec strings cited, not restated)

| ID | String |
|---|---|
| COPY-190 | "Not generated in this build. GEN sections above can be used." |
| COPY-191 | "<station> blends point-to-point with <neighbour>, so <station> must keep <neighbour>'s <n> points at the same chord positions. On those points <source> is <r> µm off (<f> % chord; limit 10 µm at <c> mm). Nothing changed." · button "Replace <station> and <neighbour> (<neighbour> changes too)" / "Replace at every station (<others> change too)" |
| COPY-192 | "Replaces <stations> (they share this section). <source> fits on its own <n> points: <r> µm (<f> % chord; limit 10 µm at <c> mm). <n0> → <n> points per surface; point types reset. Largest change <d> mm at <x> % chord. t/c stays <t> % from the Thickness curve; <source> is <ts> % thick. Frame: leading edge moved <le> mm, chord turned <a>°." (clauses that do not apply are left out) |
| COPY-193 | "A section next to a different section can have at most 10 points." (appended when <n> > 10) |
| COPY-193b | "<source> is <ts> % thick; <stations> will be scaled to <t> % (Thickness curve)." · option "Use <source>'s t/c at these stations" · chip suffix " · scaled to <t> % t/c" |
| COPY-194 | "<a> and <b> have <n> points; neighbouring sections that differ can have at most 10. Rebuild to 10 points first, or edit <a> and <b> together." |
| COPY-194b | "This edit would give <stations> all different sections, and a wing with that many different sections in a row can't be checked yet. Keep one of them shared with its neighbour, or edit them together." — **approved (operator, 2026-10-04)**; four or more differing sections in a row, the all-query operation bound (`docs/proof/blend-certificate-budget/verdict.md` §4.2) |
| CAT-RESIDUAL | "No point spacing from 8 to 16 points holds <source> within 10 µm: the closest is <r> µm (<f> % chord; limit 10 µm at <c> mm). Nothing changed." — **approved (operator, 2026-10-04)** |
| COPY-195 | "Fix the crossing before saving this section." |
| COPY-196 | "Another save is in progress. Try again in a moment." |
| COPY-197 | "Couldn't save to My sections: <cause>. Nothing was saved." |
| COPY-198 | "“<file>” is damaged and was skipped." |
| COPY-196b | "My sections is locked by a save that didn't finish. If no other CFD Workbench is open, delete “sections/.cfd-writer.claim” and save again." |
| COPY-199 | "“<name>” appears twice in My sections. Neither can be used until one file is removed." |
| status | "Replaced <stations> with <source>. Fit <r> µm (limit 10 µm). ⌘Z puts the old section back." |

Spec strings used as is: COPY-109, 110, 111, 112, 113, 114, 115, 121, "Saved “<name>” to My sections", the My sections
empty and catalog-unavailable strings (spec :2342-2343).

### 11.3 Keyboard, focus and pointer (closes app-shell :835 and :866)

| Rule / edge | Behaviour | Test (track) |
|---|---|---|
| focusInCatalog | opening the dialog puts focus in the search field | `CatalogDialog_Open_FocusInSearch` (DLG) |
| arrows | ↑/↓ move the active option (`aria-activedescendant`), disabled rows included; moving is quiet | `CatalogDialog_ArrowsReachDisabledRows_Quiet` (DLG) |
| count | the match count is announced once typing pauses (300 ms on an injected debounce clock, so the test has no sleep), in a status region | `CatalogDialog_TypingPause_AnnouncesCountOnce` (DLG) |
| K-No-match | COPY-115 in the detail line; Replace disabled | `CatalogDialog_NoMatch_Copy115ReplaceDisabled` (DLG) |
| K-Pending-or-cite-only | Enter on a disabled row changes nothing; the reason is the detail line | `CatalogDialog_PendingRowEnter_NothingChanges` (DLG) |
| K3 → Replace, focusAfterReplace | Enter or Replace applies; the dialog closes; focus returns to Section ▾ | `CatalogDialog_Replace_FocusToSectionMenu` (DLG) |
| K-Cancel, K3-Cancel, focusAfterCatalogEscape | Cancel or Escape closes; nothing changes; focus to Section ▾ | `CatalogDialog_Escape_NothingChangedFocusToSectionMenu` (DLG) |
| focusInSave | opening Save puts focus in Name | `SaveDialog_Open_FocusInName` (DLG) |
| V-Empty-or-duplicate, focusAfterSaveError | inline error, `aria-invalid`, focus stays in Name | `SaveDialog_EmptyOrDuplicate_ErrorFocusStays` (DLG) |
| V-Cancel | Cancel or Escape: nothing saved, focus to Section ▾ | `SaveDialog_Escape_NothingSavedFocusToSectionMenu` (DLG) |
| focusAfterSave | the dialog closes, live region "Saved …", focus to Section ▾ | `SaveDialog_Save_LiveRegionFocusToSectionMenu` (DLG) |
| pointer | double-click on a choosable row = Replace; option rows ≥ 24 px | `CatalogDialog_DoubleClickRow_Replaces` (DLG) |

### 11.4 Component states

| Component | default | hover/focus | disabled | loading | empty | error | success |
|---|---|---|---|---|---|---|---|
| Replace dialog list | families with rows | active row outlined | VEND/LINK/not-generated rows with reason | "Loading sections…" while the scan runs (only if > 150 ms) | "No sections match …" (COPY-115); My sections empty string | catalog unavailable (C2) | — |
| Replace button | enabled when the preview is acceptable | — | `aria-disabled` + detail line as description | "Fitting…" while a preview computes | — | COPY-191 | dialog closes, status line |
| Replace at <chain> | hidden unless COPY-191 shows | — | — | — | — | — | as Replace |
| Canvas preview | dashed accent outline | — | none for disabled rows | previous preview kept, dimmed | none | none | becomes the section |
| Save dialog | Name, provenance line | — | Save disabled while saving | "Saving…" | — | COPY-113/114/195/196/197 inline | closes; live region |
| Source chip | "Catalog original · …" / "Modified from …" / "Source not recorded" | — | — | — | — | — | — |

### 11.5 Accessibility and performance

UI-38 and UI-43 as specified; dialogs are modal with focus trapped and returned; text contrast and targets per the
existing floors (operator priority: keyboard bugs count, VoiceOver proof deferred). Preview budget 50 ms (P95) for a
16-point fit (measured at readiness); the list filters synchronously (≤ 30 rows).

## 12. Test plan

### 12.1 Triggered directives

| Trigger | Where | Directive |
|---|---|---|
| — | all | **D0** `Method_State_Outcome`, AAA, no sleeps, fixtures from bytes |
| T1 | acceptance boundary, budget, names | **D1** exact boundary rows: residual just inside/outside 10 µm at 120 mm; 5/6 pieces (10/11 points); name equal after NFC and case fold |
| T2 | provenance parser; fit over random shapes | **D2** fixed-seed property: `Provenance.Parse(Format(p)) == p`; random symmetric sections fitted on their own spacing round-trip exact (P-E7 generalised) |
| T4 | library publish | **D4** real temp folders, a second process for the claim test |
| T7 | catalog resource | **D6** golden hashes regenerated by the generator |
| — | rendered surface (UI-RENDERED-STATE) | the dialog in a realized window; preview pixels read on the canvas |

### 12.2 Tiers and rings

Each name below states its ring. **Fast ring** (every join, `tools/run-tests.sh`, 60 s budget): CAT, RPL, LIB, CTL and
DLG. Cost: Core and Persistence ≈ 45 tests, < 2 s together (Measured basis: the probe's 45 cases ran in 0.45 s);
Desktop ≈ 25 rendered tests in the `--section-editor` suite, ≈ 6 s (Inferred from M1.2c's suite; measured at the first
DLG join). **Readiness ring** (`run-readiness.py --check`, never per track): the two-process claim test (≈ 1 s, spawns
a process) and the two budgets — listed under UXR so `check-named-tests.py` never demands them at a track join.
**Native** (UXR): rows N-12D-1 … N-12D-9 = §0.1 steps, each with an attach receipt; operator time, not CI.

**Red first.** The falsifying red is on today's Import path, not on code that does not exist yet:
`ImportDat_SharedExample_OwnSpacingNotUnsupported` (red today: P-E1, Unsupported) and
`ImportDat_AfterMakeUnique_NoDslPatch` (red today: P-E2, `DSL-PATCH`). Every other ledger name records its own red
receipt in its track's Proof Pack (a behaviour failure, never a compile failure).

**Fixtures.** The 10 µm boundary uses one shape at two chords so that residual × chord is 10.01 µm and 9.99 µm.
Symmetric fits assert "≤ 10 µm" with the measured value as a tolerance band, never an exact 9.70. The Cancel test pins
Make unique + Replace (so assignments really change). The duplicate-name test uses a decomposed é **and** a case fold
together; the empty-name test includes whitespace only.

### 12.4 Named tests (the ledger)

**CAT — catalog and provenance (Core).**
`Catalog_GenEntries_RegenerateToRecordedHash` (CAT) · `Catalog_HashMismatch_CatUnavailable` (CAT) ·
`Catalog_VendAndLink_NoCoordinates` (CAT) · `Catalog_Fairings_NeverListed` (CAT) ·
`CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations` (CAT) · `Provenance_FormatParse_RoundTrip` (CAT) ·
`Provenance_LegacyDatForm_ParsesAsDat` (CAT) · `Provenance_Unknown_NotRecordedNeverGuessed` (CAT) ·
`Provenance_Rights_DerivedFromOrigin` (CAT) · `Provenance_MoveAfterReplace_MarkedModified` (CAT) ·
`Provenance_SurvivesEveryRewriter` (CAT) · `CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX` (CAT) ·
`Catalog_GenNeverThroughDatParse` (CAT).

**RPL — the Replace step (Core).**
`ImportDat_SharedExample_OwnSpacingNotUnsupported` (RPL) · `ImportDat_AfterMakeUnique_NoDslPatch` (RPL) ·
`Replace_SharedExample4412_OwnSpacing15PointsCertified` (RPL) · `Replace_SharedExampleSymmetric_KeepsCurrentSpacingAndPoints` (RPL) ·
`Replace_NoOwnSpacingWithin10um_DisabledBestNumber` (RPL; fixture: a synthetic section with a 1 % chord bump that no
8–16 point sqrt spacing fits within 10 µm, measured in the test) · `Replace_AfterMakeUnique_NoOrphanParses` (RPL) ·
`Replace_SharedKeepsName_ScopeStaysShared` (RPL) · `Replace_UniqueRootSymmetric_FitsCurrentSpacingKeepsPoints` (RPL) ·
`Replace_UniqueRootCambered_RefusedCatSpacingNothingChanged` (RPL) · `Replace_BlendChain_AllStationsReplacedCertified` (RPL) ·
`Replace_ChainOwnFifteen_DisclosesCount` (RPL) · `Replace_AcceptanceUsesLargestReplacedChord` (RPL) ·
`Replace_ResidualJustOver10um_Refused` (RPL) · `Replace_ResidualJustUnder10um_Accepted` (RPL) ·
`Replace_MySectionsEntrySameSpacing_ExactCopy` (RPL) · `Replace_VendEntry_NoCoordinatesNothingChanges` (RPL) ·
`Replace_InnerUndo_RestoresBytesAndChip` (RPL) · `Replace_InnerRedo_ReappliesSource` (RPL) ·
`Replace_CancelSection_AssignmentsAtEntry` (RPL) · `Replace_FinishSection_OneUndoStep` (RPL) ·
`Replace_StalePreviewAfterTypingOrStep_Refused` (RPL) · `Replace_Preview_EmitsCatalogPreviewOutcome` (RPL) ·
`SectionEdits_ReplaceStep_NeverApplied` (RPL) · `Guard_FivePiecesDiffering_Certifies` (RPL) ·
`Guard_SixPiecesDiffering_RefusedCopy194` (RPL) · `Replace_ThreeStationsEveryStation_OneBlockCertified` (RPL; P-B3) ·
`Replace_ResidualEuclidean201PlusKnots_NotVerticalGap` (RPL) · `Replace_CurrentSpacing_KeepsIdsAndTangentRows` (RPL) ·
`Replace_OwnSpacing_ListsDroppedRows` (RPL) · `Replace_SourceThicknessDiffers_ReportsScaledTc` (RPL) ·
`Replace_Frame_ReportsLeShiftRotationScale` (RPL).

**LIB — My sections store (Persistence).**
`Library_Save_PublishesHashNamedFile` (LIB) · `Library_EntryBytes_ParseAsStandaloneSection` (LIB) ·
`Library_DuplicateIgnoringCaseNfc_Refused` (LIB) · `Library_EmptyName_Refused` (LIB) ·
`Library_CrossingSection_RefusedInvalid` (LIB) · `Library_StaleClaim_ReportedNotDeleted` (LIB) ·
`Library_HashNotName_ListedAsProblem` (LIB) · `Library_ScanNameCollision_BothReported` (LIB) ·
`Library_OversizedFile_SkippedAsProblem` (LIB) · `Library_WriteFails_NothingPublished` (LIB) ·
`Library_UnprovedPlatform_RefusedNothingSaved` (LIB) · `Library_SecondInstanceSameRoot_EntryListed` (LIB).

**CTL — controller and commands (Desktop).**
`Controller_PreviewLatestWins_OlderDropped` (CTL) · `Controller_ApplyReplace_OneStepChipResidualStatus` (CTL; the chip,
the A4.6 residual and the status line) · `Controller_SaveMine_DraftDocumentUndoUnchanged` (CTL) ·
`Controller_SaveMine_SurvivesSectionCancelAndDocumentUndo` (CTL) · `Controller_SaveMine_ListedAfterOpeningAnotherFoil` (CTL; through the `App` root wiring, F-6)
 · `Commands_SectionMenu_ReplaceSaveImportRowsRun` (CTL) ·
`StatusStrip_ReplaceReport_NamesStationsAndResidual` (CTL).

**DLG — dialogs and canvas (Desktop, rendered).**
`CatalogDialog_Open_FocusInSearch` (DLG) · `CatalogDialog_FamiliesGrouped_ListboxShape` (DLG) ·
`CatalogDialog_ArrowsReachDisabledRows_Quiet` (DLG) · `CatalogDialog_TypingPause_AnnouncesCountOnce` (DLG) ·
`CatalogDialog_NoMatch_Copy115ReplaceDisabled` (DLG) · `CatalogDialog_PendingRowEnter_NothingChanges` (DLG) ·
`CatalogDialog_SharedPreview_DetailNamesStationsFitLimit` (DLG) · `CatalogDialog_Replace_FocusToSectionMenu` (DLG) ·
`CatalogDialog_Escape_NothingChangedFocusToSectionMenu` (DLG) · `CatalogDialog_SpacingRefusal_ReasonAndChainOffered` (DLG) ·
`CatalogDialog_ChainButton_AppliesAllStations` (DLG) · `CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel` (DLG) ·
`CatalogDialog_MySectionsEmpty_ShowsNextAction` (DLG) · `CatalogDialog_DoubleClickRow_Replaces` (DLG) ·
`SectionCanvas_Preview_DashedAccentPixelsOverCurrent` (DLG) · `SaveDialog_Open_FocusInName` (DLG) ·
`SaveDialog_EmptyOrDuplicate_ErrorFocusStays` (DLG) · `SaveDialog_Escape_NothingSavedFocusToSectionMenu` (DLG) ·
`SaveDialog_Save_LiveRegionFocusToSectionMenu` (DLG) · `SourceChip_AfterReplaceAndEdit_TextNotColour` (DLG) ·
`SourceChip_StationTcDiffers_SaysScaled` (DLG) · `BrowserRow_AfterReplace_NameThenSource` (DLG) ·
`CatalogDialog_Preview_LargestChangeMarkerAtMeasuredX` (DLG).

**UXR — readiness ring and native rows (gathered at UXR, never per track).**
`Library_TwoProcessSameName_OneWins` (UXR) · `Readiness_ReplacePreview_Under50Ms` (UXR) ·
`Readiness_LibraryScan100_Under100Ms` (UXR); native rows N-12D-1 … N-12D-9.

## 13. Decisions, findings and open items

### 13.1 Decision batch for the operator (DR-M12D-n)

| DR | Question | Options | Recommendation |
|---|---|---|---|
| **DR-M12D-1** | How does Replace meet the shared point-spacing rule? | A fit on the current spacing, refuse over 10 µm · B Replace at every station · C common spacing + refit neighbours · D accept over 10 µm with a warning (Rhino's convention; chip "Fitted to …") · E raise the blend proof budget (spike) | **A + B now; E as the next spike.** A admits symmetric sections beside anything (P-CF-0012 9.75 µm); B admits any GEN section when every station takes it (P-CF-rule3 5.54 µm). C does not admit 4412 within budget (P-CF-4412-N, P-E6); D needs an A4.6 change and a different chip |
| **DR-M12D-2** | Which GEN rows are choosable in M1.2d? | a 4-digit closed form only (0009, 0012, 4412), the other 7 listed disabled (COPY-190) · b hold M1.2d for a TM 4741 generator | **a**; generator spike next |
| **DR-M12D-3** | Import .dat… in the Section menu | a becomes Replace with a file source (same rule, fixes P-E1/E2) · b keep the as-built Import path | **a** (one path; the as-built path makes unsaveable drafts and refuses after Make unique) |
| **DR-M12D-4** | Save to My sections when the surfaces cross | a refuse (COPY-195) · b save anyway | **a** (an entry must be usable) |
| **DR-M12D-5** | The section's name after Replace | a the source's slug ("naca-4412", "-i1" if taken) · b keep the old name | **b** (persistence lens F1: today the draft's scope is derived by name in six places, so a rename makes a shared Replace read as Independent; the Source row and chip carry the truth). A rename verb, with scope derived from the assignment set, is a later slice |
| **DR-M12D-7** | The trailing edge of the generated NACA rows | a the closed-TE form (a₄ = −0.1036), as New foil already uses · b the standard open TE | **a**: a cambered open TE ends its two surfaces at different x, which the record (surfaces share x) cannot hold — ≥ 19.9 µm at 120 mm on every spacing (P-CF-4412-open); the closed form fits in 15 points at 5.54 µm. The row carries "closed TE" |
| **DR-M12D-6** | A section with more than 10 points beside a differing section (after B, a later Make unique + edit) | a refuse that edit at once with COPY-194 (the Ruling 71 predicate gains the budget) · b refuse Make unique itself while points > 10 | **a** (Make unique alone is harmless — identical shapes are not checked; the edit that differs is the cause) |

### 13.2 Findings

- **F-1 (to the cross-profile design and the Ruling 71 fix):** the early-refusal predicate must include the blend node
  budget (≤ 10 points per surface when neighbours differ, `Geometry.cs:389-399`), or a draft can still become unsaveable
  (P-E4 n = 12, P-E6 n = 11). Same class as Ruling 71 (a step accepted that can never certify).
- **F-2 (sweep of that class, as-built):** the Import step is two more instances — it falls back to an own spacing beside
  the same section's other station (P-E1, Unsupported) and it refuses with `DSL-PATCH` whenever it would orphan a
  profile (P-E2/E5). Both are fixed by DR-M12D-3 a; the register entry and its control belong to the Ruling 71 fix's
  class → sweep → prevent record (this design is documents-only).
- **F-3:** ADR-0007 finding 5 (1e-5 chord fraction vs 10 µm at local chord) is resolved here for Replace (§3.6 rule 5);
  Fit/Rebuild keep theirs until their own slice.
- **F-4:** ADR-0007 §4's basis order is amended (deviation D-1): the neighbour-basis step is gone (in a certified draft
  the current spacing *is* the neighbours' spacing), and the own-spacing step applies only when no neighbour differs.
- **F-5 (geometry lens F1, Verified):** `DatImport.Parse` is a frame change it does not report — the LE is the minimum-x
  *sample*, the shape is shifted by that sample's y and scaled by xMax − xMin, with no rotation. For a cambered section
  this moves the leading edge between samples (2412: the parsed upper surface jumps from (0,0) to (4e-5, 3.5e-3)). GEN
  shapes no longer use it (§3.3). The DAT import path (CAT-02) keeps it; reporting its frame change is a finding for
  that path's owner, outside M1.2d except that Import .dat… now shows the residual Replace computes.
- **F-7 (marine-CAD lens F10, Flagged):** the fitted 4412's per-side LE radius (mockup readout 1.06 · 2.36 % c on the
  first draft's record) is far from the closed form's 1.1019 t² ≈ 1.59 % c. The 10 µm position rule does not bound
  curvature. The geometry lens decides whether Replace must also report or bound the LE radius (next design).
- **F-8 (idea, not in scope):** a deviation-vs-x strip coloured against 10 µm (Rhino Rebuild's point colouring) in the
  sheet; captured as a next step.
- **F-6:** ADR-0008's library path differs from the app's preference root; this design uses the app's root. ADR-0008
  gets a one-line amendment at the RPL/LIB merge.

### 13.3 Deviations

- **D-1** ADR-0007 §4 basis order (F-4). **D-2** spec DR-8 "content-addressed pin" → inline copy (ADR-0008 §4, carried).

## 14. Build tracks (exclusive file ownership)

| Track | Owns (exclusive) | Depends on | Delivers |
|---|---|---|---|
| **CAT** | `src/CfdWorkbench.Core/Catalog.cs` (new), `src/CfdWorkbench.Core/Provenance.cs` (new), `src/CfdWorkbench.Core/FoilSource.cs` (provenance parse/print), `tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs`, `src/CfdWorkbench.Core/CatalogData/` (new, embedded), `src/CfdWorkbench.Core/CfdWorkbench.Core.csproj`, `tests/CfdWorkbench.Core.Tests/CatalogTests.cs` (new), `tests/CfdWorkbench.Core.Tests/ProvenanceTests.cs` (new) | — | catalog, generator, provenance |
| **LIB** | `src/CfdWorkbench.Persistence/SectionLibrary.cs` (new), `src/CfdWorkbench.Persistence/ProjectStore.cs` (`PublishUnderClaim`), `tests/CfdWorkbench.Core.Tests/SectionLibraryTests.cs` (new) | CAT's `Provenance` (seam S-1) | My sections store |
| **RPL** | `src/CfdWorkbench.Core/SectionReplace.cs` (new), `src/CfdWorkbench.Core/DatImport.cs` (`FitToBasis`: ids, KKT tangent rows, Euclidean residual), `src/CfdWorkbench.Core/Contracts.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs`, `tests/CfdWorkbench.Core.Tests/SectionReplaceTests.cs` (new), `tests/CfdWorkbench.Core.Tests/SectionDraftTests.cs`, `tests/CfdWorkbench.Core.Tests/SectionEditTests.cs`, `tests/CfdWorkbench.Core.Tests/DatImportTests.cs` | CAT (S-1), the Ruling 71 predicate (S-2) | Replace step, Import → Replace, budget clause test |
| **CTL** | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `src/CfdWorkbench.Desktop/Shell/CommandTable.cs`, `src/CfdWorkbench.Desktop/Shell/ShellHost.cs`, `src/CfdWorkbench.Desktop/Shell/StatusStrip.axaml.cs` (a `"replace"` arm; the `"import"` arm is deleted), `src/CfdWorkbench.Desktop/App.axaml.cs`, `tests/CfdWorkbench.Desktop.Tests/StatusStripTests.cs`, `tests/CfdWorkbench.Desktop.Tests/ControllerSectionTests.cs` | RPL, LIB | controller, commands, library root wiring |
| **DLG** | `src/CfdWorkbench.Desktop/CatalogDialog.axaml(.cs)` (new), `src/CfdWorkbench.Desktop/SaveSectionDialog.axaml(.cs)` (new), `src/CfdWorkbench.Desktop/SectionCanvas.cs`, `src/CfdWorkbench.Desktop/SectionEditorView.axaml(.cs)`, `src/CfdWorkbench.Desktop/PropertiesView.cs` (Source row, station card), `src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml.cs` (name · source), `tests/CfdWorkbench.Desktop.Tests/CatalogDialogTests.cs` (new) | CTL | dialogs, canvas preview, chip |
| **UXR** | `docs/proof/m12d-uxr/` | DLG | native rows N-12D-1…9 against this mockup |

Order: CAT ∥ (Ruling 71 fix) → LIB ∥ RPL → CTL → DLG → UXR. Critical path: CAT → RPL → CTL → DLG → UXR.

### 14.1 Behaviour → data → file trace (every promised visible behaviour)

| Visible behaviour (§0.1 / §11) | Data | Written / computed in | Shown by |
|---|---|---|---|
| Families NACA · Eppler · Speer · My sections | `CatalogEntry.Family`; `LibraryScan.Entries` | `Catalog.cs`; `SectionLibrary.cs` | `CatalogDialog.axaml.cs` |
| Disabled rows with reasons | `CatalogEntry.DisabledReason` | `Catalog.cs` | `CatalogDialog.axaml.cs` |
| Match count announced once typing pauses | filter result count | `CatalogDialog.axaml.cs` | status region in `CatalogDialog.axaml` |
| Dashed preview over the current section | `ReplacePreview.Bytes` | `SectionReplace.cs` via `WorkbenchController.cs` | `SectionCanvas.cs` |
| Detail line: fit (µm and % chord), limit, stations, point count, largest change, t/c and scaling, frame | `ReplacePreview` / `ImportReport` fields; station t/c (derived, as built) | `SectionReplace.cs`, `Catalog.cs` (frame) | `CatalogDialog.axaml.cs` |
| Largest-change marker on the canvas | `ReplacePreview.LargestChangeAtX` | `SectionReplace.cs` | `SectionCanvas.cs` |
| Browser row and station card "section-a · NACA 4412" | `ProfileDefinition.Provenance` | `FoilSource.cs`, `Provenance.cs` | `BrowserPane.axaml.cs`, `PropertiesView.cs` |
| "Use NACA 0009's t/c at these stations" | existing `SectionStep.Thickness(UseSource)` | `AuthoringSession.cs` (as built) | `CatalogDialog.axaml.cs` |
| Spacing refusal + Replace at <chain> | `ReplacePreview.RefusalCode`, `BlendChain` | `SectionReplace.cs` (+ Ruling 71 predicate) | `CatalogDialog.axaml.cs` |
| Section takes the shape; one inner Undo | step bytes | `AuthoringSession.cs` (`PatchSectionStep`) | `SectionCanvas.cs`, `SectionEditorView.axaml.cs` |
| Chip "Catalog original · …" → "Modified from …" | `provenance` string in the bytes | `SectionReplace.cs`; `Provenance.cs` (`MarkModified`) | `SectionEditorView.axaml.cs` |
| Status line after Replace | `SectionStepReport.Import` | `AuthoringSession.cs` | `WorkbenchController.cs` → status strip |
| Points pane shows the new count | profile record | `AuthoringSession.cs` | `PointsView.cs` (as built, unchanged) |
| Source row "Catalog original · NACA 4412 (GEN)", name kept | `ProfileDefinition.Provenance` | `FoilSource.cs`, `Provenance.cs` | `PropertiesView.cs` (as built Section group; DLG adds the Source row) |
| Save dialog provenance line and rights | `Provenance.Parse` of the draft profile | `Provenance.cs` | `SaveSectionDialog.axaml.cs` |
| Duplicate / empty / crossing refusals | `LIB-*` codes | `SectionLibrary.cs` | `SaveSectionDialog.axaml.cs` |
| "Saved …" live region; entry in another foil | library file | `SectionLibrary.cs` | `WorkbenchController.cs`, `CatalogDialog.axaml.cs` |
| Catalog unavailable / My sections empty | `CAT-UNAVAILABLE`; empty scan | `Catalog.cs`; `SectionLibrary.cs` | `CatalogDialog.axaml.cs` |
| Later edit refused for the 10-point budget | predicate result | Ruling 71 predicate (S-2) | status strip (as built) |

### 14.2 Seams and callers of anything changed

- **S-1** `Provenance` (CAT) is read by LIB, RPL, CTL, DLG: CAT lands its signature first (one commit) so the others build.
- **S-2** the Ruling 71 predicate: RPL calls it once from `SectionReplace.Patch`; if the fix track's signature differs,
  RPL adapts that call only. The budget clause (F-1) is proposed to that track; RPL's `Guard_SixteenPointsDiffering_…`
  test runs against it.
- **Callers of changed members:**
  - `SectionStep` (new case): `AuthoringSession.PatchSectionStep` (:1397), `SectionStepKind` (:1238),
    `SectionEdits.Apply` (default arm — must never receive Replace; a guard test), `ShellHost.ApplySectionStepAsync`
    and `ReportRefusal` (:1165-1175; refusal copy for `CAT-*`).
  - `SectionStep.Import` (now mapped to Replace): `ShellHost.ImportDatAsync` (:1160), `SectionDraftTests.cs:295`,
    `SectionEditTests.cs:413-414` (`BeginSectionImport`, used by `DatImportTests.cs:49`, `:150`), and the import report
    carried by `SyncSectionDraft` (`AuthoringSession.cs:1318`).
  - `ImportPatch` / `NeighbourBases` (deleted): only `PatchSectionStep`'s Import arm (:1441).
  - The `"import"` step kind: `StatusStrip.axaml.cs:194-197` (CTL replaces it with `"replace"`).
  - Profile provenance (CAT, `FoilSource.cs`): every block rewriter — `WriteSurfaces`, `WriteSideTangents`,
    `InsertProfileKnot`, `DeleteProfileVertex`, `MakeIndependent`, `FairProfile`, `RebuildProfile`, `Print`.
  - Scope by name (unchanged because the name is kept): `AuthoringSession.cs:1254`, `:1268`, `:1308`, `:1386`,
    `:1563`; `ShellHost.cs:1022`.
  - `ImportReport` (gains optional `Stations`): `WorkbenchController.cs:74-80`, `DatImportTests.cs:56-59, 154-155`,
    `SectionDraftTests.cs:305`.
  - `App.axaml.cs:29-31` (preference root, now also passed to `SectionLibrary`).

## Adversarial analysis (STRIDE-lite)

| Boundary | Threat | Disposition | Test |
|---|---|---|---|
| Library folder (user-writable files read at scan) | **T** a hand-edited file claims another name or VEND rights | mitigate: hash = name check; VEND origin in an entry is reported, not listed choosable | `Library_HashNotName_ListedAsProblem` |
| same | **D** a huge or deeply nested file | mitigate: SRC-09 limits | `Library_OversizedFile_SkippedAsProblem` |
| same | **E** a file name with path characters | mitigate: only `[0-9a-f]{64}.foil` names are read | covered by `Library_HashNotName_ListedAsProblem` |
| Catalog resource (in the signed app) | **T** a modified catalog | transfer: app signing (ADR-0003); detect: hash test at load | `Catalog_HashMismatch_CatUnavailable` |
| Rights | **I** VEND coordinates leaking through a disabled row | mitigate: none shipped | `Catalog_VendAndLink_NoCoordinates` |
| **S/R** | not applicable: single-user local app; no identity crosses | accept | — |

## Privacy analysis (LINDDUN-lite)

The entry name is user text; it never leaves the machine and is not in telemetry (`library.save` carries outcome and
ms only). No personal data otherwise. Retention: until the user deletes the file. Rights path: the folder is
user-visible.

## Conformance notes

ADR-0007 (Replace as a step, one Finish), ADR-0008 (store, provenance, inline copy), ADR-0005 (Replace on the current
spacing keeps tangent rows — they are taken as KKT rows as the as-built fit does; an own-spacing Replace drops them and
reports, ADR-0007 §4), A4.6 (residual reported, 10 µm), UI-38/43. Deviations D-1, D-2.

## Flagged risks and residual unknowns

- The 6-series residuals are unmeasured (assume in §3.5). - Cambered sections beside a different section stay out until
  DR-M12D-1 E. - The Ruling 71 predicate's signature is assumed (§5.4). - Native APG combobox behaviour under VoiceOver is
  not proved (operator priority: deferred).

## Status & next action

| | |
|---|---|
| **Completed** | M1.2d design, probe, mockup, DR-M12D-1…6 |
| **Remaining** | the operator's mockup approval and DR rulings; the cross-profile design's answer on F-1; the generator spike (DR-M12D-2); the blend-budget spike (DR-M12D-1 E) |
| **Best next action** | Operator reviews `docs/mockups/m12d-catalog.html` and rules DR-M12D-1…6 |

## Gate record

Four lenses in Adversary mode on 2026-10-04, one repair cycle, re-checked by the same lenses (authors did not self-clear).

| Lens | First pass | Main findings | After repair cycle 1 |
|---|---|---|---|
| Computational Geometry (hard veto) | **VETO** | (1) GEN shapes went through `DatImport.Parse`, an unreported frame change for cambered sections; (2) option B over a partial chain cannot certify with ≥ 3 stations; D undefined; budget is pieces not points; tangent rows/ids not kept by `FitToBasis`; chip dishonest under t/c scaling; vertical-gap residual; 15 vs 16 | **Veto CLEARED.** Closed form in the chord frame, frame reported; B = every station (P-B3); D, budget, ids/KKT rows, `DroppedRows`, single `modified` writer, scaled-t/c chip; Euclidean residual. Minors folded in: two-sided distance, ordered n-scan, UseSource reports its channel change |
| Data & Persistence (hard veto) | PASS-WITH-CONDITIONS | rename breaks scope-by-name (six sites); provenance not in the model and erased by `Print`; entry grammar wrong; no claim on create-only saves; two `modified` writers; `dat:` chip text; callers missing; stale-claim copy | All taken: keep the name (DR-M12D-5 b); CAT owns provenance in `FoilSource`; standalone-section grammar + NFC; LIB owns `PublishUnderClaim`; one writer; "a .dat file"; callers added; COPY-196b / COPY-199. Not re-run (PASS-WITH-CONDITIONS, conditions met in the text) |
| Test Architect (hard veto) | **BLOCK** | rule 3 oracle contradicted itself; no 10/11 boundary; red-first was a compile failure; readiness names in track ledgers; detail-line and another-foil untested; debounce sleep | **Block CLEARED.** One oracle (15 points, 5.54 µm); 5/6-piece guards; red on today's Import path; UXR group; added names; injected debounce clock; wrapped ledger line fixed |
| Marine-CAD UX (soft veto) | PASS-WITH-CONDITIONS (would block on t/c scaling) | the chip names a section the station does not build (t/c scaling); design/mockup disagreed (16 vs 15, DR-5); refusal wording and B's consequence; r7 had no preview; µm + % chord; Browser/station card; craft issues | **PASS-WITH-CONDITIONS, block lifted.** Rule 6c + COPY-193b + screen 6b; aligned numbers and DR-5 b; COPY-191 reworded, "(Tip changes too)"; r7 parity; marker, point-count, frame line; craft fixes. Open condition: "100 %" axis label and the Fit pill under the sheet edge (Minor, mockup only) |

UI craft gate (`ui-craft-gate.py`): 1 Minor, `side-tab` on the inherited status-strip warning style of the approved M1.2c
page — a recorded deviation, not new in this slice. Page check: no errors, no template leaks, both themes; the page's own
largest-change number equals the probe oracle's (4.09 % chord).

Lenses not convened (named, not skipped silently): Patterns Expert, Simplifier, UX & Accessibility, Native Desktop,
Security (no new trust boundary beyond the user-writable library folder, analysed above). They are owed before build at
the operator's call.
