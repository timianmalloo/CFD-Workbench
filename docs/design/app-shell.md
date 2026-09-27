---
id: design-app-shell
title: "Design: the CAD-first app shell — Start, workspaces, docks and floats, selection and Properties, menus and commands"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — M1.2a shell parts and M1.2e (spec 1.6)
tags: [desktop, shell, docking, dock, layout, workspaces, floats, nativemenu, commands, selection, properties, focus, m1.2a, m1.2e]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: architecture-application, rel: refines }
  - { to: adr-0009-cad-first-shell, rel: depends-on }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-0006-driving-dimensions, rel: depends-on }
  - { to: adr-0008-section-library, rel: depends-on }
  - { to: note-20260926-command-table-selection, rel: refines }
  - { to: note-20260927-recent-files, rel: depends-on }
  - { to: design-section-editor, rel: depends-on }
  - { to: mockup-workbench-v10, rel: relates-to }
  - { to: review-ui-workbench-v10, rel: relates-to }
  - { to: review-ui-workbench-v9, rel: relates-to }
  - { to: design-language, rel: depends-on }
review-by: 2027-03-25
summary: >-
  Detailed design of the CAD-first shell for slices M1.2a (Start/Opening/open-failed, Planform workspace, left side
  bar, Properties with the Wing block and typed Span, one command table feeding per-window NativeMenus, Edit-verb
  routing) and M1.2e (owned OS-window floats, Maximize, focus-safe floats, Review workspace, per-workspace saved
  layouts with clamping). Settles the layout-file schema with a version-first rollback rule, the selection model, the
  focus contract, the failure modes and the build tracks with exclusive file ownership and exact test names.
review-suggested: []
---

# Design: the CAD-first app shell

- **Status:** In review — gate passed with conditions after 2 of 2 repair cycles (gate record at the end); Owner acceptance pending.
- **Spec / architecture:** [spec rev 1.6](../specs/cfd-workbench-v1.md) Parts B1, B2, B7, B11, B12, C1–C4b, Appendix G ·
  [architecture §10](../architecture/application.md#10-revision-2026-09-26--spec-16-cad-first-shell-and-editing) ·
  [ADR-0009](../adr/0009-cad-first-shell-docking-and-menus.md) (shell) · [ADR-0007](../adr/0007-edit-transactions-section-draft-and-gesture-commit.md) ·
  [ADR-0006](../adr/0006-driving-dimensions-and-wing-estimates.md) · [ADR-0008](../adr/0008-profile-catalog-and-section-library.md) ·
  [note: command table and selection](../notes/command-table-and-selection.md) · [note: recent files](../notes/recent-files-preference.md).
- **Delivery slices:** the shell parts of **M1.2a** and all of **M1.2e** (architecture §10.6). Geometry internals of
  M1.2b–d are out of scope and referenced where the shell hosts them. Section-editor internals: [`section-editor.md`](section-editor.md), ADR-0007.
- **Author / date:** `/design-slice` sub-agent (session `fbfa35dc-design`), 2026-09-27. Git before: `6301eb9`.

Labels: **Verified** = observed in code, a run or a cited document; **Inferred** = reasoned, not observed; **Flagged** =
observed but suspect. `assume:` marks a belief with what confirms it and what breaks if it is false.

## 1. Grounding — what this design must satisfy

Traversal: `spec-cfd-workbench-v1` (B1, B7, F11, F12, UX-28–33, CAD-14/16/17/21, C2, UI-36–43) → `architecture-application`
§10.2, §10.5, §10.6 → `adr-0009-cad-first-shell` → `note-20260926-command-table-selection` → `adr-0006` → `adr-0007` →
`design-section-editor` → `review-ui-workbench-v9` §4–§5 and `review-ui-workbench-v10` §5–§7 → `mockup-workbench-v10`
(`tools/check-mockup-v10.mjs`, the 39 focus checks) → code on `feature/ui-cad-direction` at `6301eb9`.

| Source | Load-bearing statement |
|---|---|
| B1 | Model area Plan + 3D by default; left side bar 260 px (200–420) with **Properties** and **Browser**; bottom panel (**Points**, **Messages**) hidden; right side bar hidden; workspaces **Planform · Precision · Review**, each remembers its layout; Reset layout restores the preset |
| B7 | Tab menu **Move to · Float · Size · Maximize · Close**; drag to a highlighted drop zone; a float moves with Alt + arrows or its Position menu and docks back with ⤓ or Escape; F6 cycles regions; ⌘B / ⌘J / ⌥⌘B; ⌘⇧M maximizes; **shortcuts never act while a text field has focus** |
| UX-28 | ≤ 1 action to an editable foil (New foil / New from example), ≤ 2 via Open…; focus on the first start card; Opening cancellable from the keyboard; an open failure keeps Start, offers Open another file…, changes no file |
| UX-29 | 28 non-happy edges (F11: 23, F12: 5), each a test row (§12.4 ledger) |
| UX-30/31 | Planform: left 260 px, right and bottom hidden; Precision adds Points + Messages; Review = four views, no panes; switching keeps selection, camera and any section draft; selection agrees across views and Properties |
| UX-32 | Every pane movable, floatable, sizable, maximizable, closable from its tab menu; floats are OS windows on any monitor; layouts survive restart with floats clamped to connected monitors; **option (a)**: a focused model-area control is never left under a float |
| CAD-14, CAD-17, CAD-21 | Start / Opening / open-failed; Wing block last in Properties, estimates never stored; every verb in a menu-bar menu and the palette with a keyboard route |
| ADR-0009 §1–6 | Dock 11.3.12.1; owned OS-window floats; NativeMenu; one command table on the main window and every float; Edit verbs to a focused text field first; one selection state; shell-owned F6 ring including floats; **our own versioned layout file**; maximize, clamp and focus-safe floats are ours |
| Arch §10.5 | `layout.load` (restored · preset-fallback · pane-dropped · clamped n · session-only), `float.relocate` (moved · docked-back); closed DTO; per-pane fallback, then preset; **the stale-claim recovery action is design-slice's** (§10.7) |

**Code as built (opened, not recalled):**

- Fixed three-column grid, Navigator left, Properties right, title-strip buttons (`src/CfdWorkbench.Desktop/MainWindow.axaml`:10-14, :21, :148).
  F6 cycles four fixed groups in one window (`MainWindow.axaml.cs`:339-360) with a reusable `NextRegionIndex` (`:381-392`).
  ⌘Z/⌘⇧Z reach the document from window `KeyDown` only with no draft open (`:364-378`). No `NativeMenu`.
- The controller has no selection member (`WorkbenchController.cs`:78-166) and fires `Changed` synchronously (`:762`).
- **Open is not safely cancellable (Verified hazard).** `OpenPathAsync` reads, then `Adopt(next)` disposes the current
  session (`WorkbenchController.cs`:180-183, :664-668), then awaits `RefreshAcceptedAsync(cancellation)` (`:186`).
  A cancel after `Adopt` has already discarded the old document, so COPY-105 "Nothing changed" would be false.
- **Open has outcomes that do not throw (Verified):** a parse refusal, an ID candidate that needs Accept IDs, and
  uncertified geometry each return without `Adopt` and keep the original read-only (`WorkbenchController.cs`:196-229).
- **Open failures are not classifiable by code (Verified):** the store maps `ENOENT` → `DOC-CONFLICT` and `EACCES` →
  `DOC-IO` (`src/CfdWorkbench.Persistence/ProjectStore.cs`:181-185); the `.foil` reader throws raw
  `FileNotFoundException`/`UnauthorizedAccessException` (`src/CfdWorkbench.Cli/Program.cs`:21-27).
- `ProjectStore` is byte-generic (`ProjectStore.cs`:10-24, :98-164): create-only `linkat` or compare-and-swap `renameat`,
  mode 0600, file and directory fsync, **one fixed claim `.cfd-writer.claim` per parent directory** (`:108`, `:172`),
  never deleted by age; macOS arm64 only, else `DOC-UNSUPPORTED-PERSISTENCE` (`:176-177`). It makes its own trace id per
  call (`:58`).
- Desktop tests: `UsePlatformDetect().SetupWithoutStarting()`, sub-suites by flag, only `--section-flow` and
  `--section-tools` spawned from the default run (`tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs`:19-41, :1863-1873);
  the Desktop harness throws on the first failure; only the Core harness prints `PASS`/`FAIL` lines
  (`tests/CfdWorkbench.Core.Tests/IdentityTests.cs`:62-66). `WorkbenchTests.cs` holds 213 `throw new` assertion lines
  (grep). There is no `Avalonia.Headless` package in the repo. No `UnhandledException` handler exists under `src/`.

**Spikes and evidence** ([`docs/proof/cad-first-spikes/shell-reflect/`](../proof/cad-first-spikes/shell-reflect/), run
2026-09-27, plus the Native Desktop lens's source reading of Avalonia `release/11.3.14`):

| Claim | Label | Basis |
|---|---|---|
| `IDockable` has `CanDrop/CanDrag/CanFloat/CanClose/CanPin/CanDockAsDocument`, `DockCapabilityOverrides`, `MinWidth/MaxWidth`, `Proportion` | Verified | `reflect-output.txt` |
| `DockManager.Validate*` are not virtual; `DockControl.DockManager` has no public setter — drop validation cannot be overridden | Verified | `reflect-output.txt` |
| `IFactory` hooks `OnDockableDocked/Moved/Closed`, `OnWindowOpened/Closed/MoveDragEnd`, `OnFocusedDockableChanged`; `HostWindowLocator` settable | Verified | `reflect-output.txt` |
| `DockSettings.FloatingWindowOwnerPolicy` ∈ {Default, AlwaysOwned, NeverOwned}; owner behaviour not run | Verified (members) | `reflect-output.txt`; S7 |
| `TextBox.Undo/Redo/CanUndo/Cut/Copy/Paste/SelectAll`; `NativeMenuItem.Gesture/Command/IsEnabled`; `Screens.Changed`; `Screen.WorkingArea: PixelRect`; `Window.Position: PixelPoint`; `Window.FrameSize: Size?` (DIP); `PointToScreen → PixelPoint` | Verified | `reflect-output.txt` |
| .NET 10 on macOS: `SpecialFolder.ApplicationData` = `~/Library/Application Support` | Verified | `special-folders-output.txt` |
| **On macOS an Application-level `NativeMenu` fills only the app-name menu; a key window with no window menu shows only the app menu** — so File/Edit/Window vanish while a float is key | Verified (Avalonia source: exporter `SetAppMenu`; `AvnWindow.mm` `showAppMenuOnly`) | Native Desktop lens |
| Avalonia adds Services, Hide, Hide Others, Show All, Quit to the app menu itself | Verified (source, `PopulateStandardOSXMenuItems`) | Native Desktop lens |
| **Avalonia hard-codes `Screen.Scaling = 1` on macOS** (bounds in points) — resolves ADR-0009's Flagged "Scaling = 1 on Retina" | Verified (source, `Screens.mm`) | Native Desktop lens |
| ⌘Q → `applicationShouldTerminate` → `TryShutdown()` synchronously; an async cancel of `Closing` answers `NSTerminateCancel` | Verified (source `app.mm`) | Native Desktop lens |

**Recorded deviations from ADR-0009 (flagged review-suggested on the ADR):**

1. **Per-window NativeMenus, not one Application menu** (ADR-0009 §2). The Application `NativeMenu` holds only About;
   every window — the main window and each float, in `IFactory.OnWindowOpened` — gets its own `NativeMenu` **generated
   from the one command table**. ADR-0009's intent ("the menu stays whole when a float has focus"; "no second menu
   definition") is kept; only the carrier changes. S7 checks File/Edit/Window stay whole with a float key.
2. **`FrameSize` conversion** (ADR-0009 §6 "never through a DIP conversion"): `FrameSize` is DIP, so one conversion
   by the float window's own `DesktopScaling` is unavoidable; it lives in one pure function (§6.5).

## 2. Responsibility

The shell owns **where things are and how you move between them**: Start / Opening / open-failed; the workspace frame
(app bar, side bars, bottom panel, model area, status line); the Dock host with tabbed, movable, floatable,
maximizable panes; the three workspaces and their saved layouts; the command table with per-window NativeMenus and key
bindings; Edit-verb routing; the F6 ring across windows; focus-safe floats; the selection-driven Properties host with
the Wing block; the recent-files list.

Not the shell's: geometry, drafts, certificates, history (Core); section-editor internals (ADR-0007, `section-editor.md`);
view drawing beyond hosting (M1.2b); catalog and My sections internals (ADR-0008 — the shell hosts their dialogs); the
activity rail (DR-7, not built in M1.2).

## 3. Data model (settled first)

### 3.1 Bounded context and ubiquitous language

**Installation preferences** (architecture §2; A3.1 "Pane · dock · float · workspace"). **Pane**: Properties, Browser,
Points, Messages — ids `properties`, `browser`, `points`, `messages`. **Region**: a dock — `left`, `right`, `bottom`.
**Tab group**: ordered panes shown as tabs in one region. **Float**: an owned OS window holding one tab group.
**Workspace**: `planform`, `precision`, `review` — a task preset of views and placements that remembers its layout.
**Preset**: a workspace's built-in layout. **Layout**: the current placements in one workspace. The model area is the
one document, not a pane, and never moves. **Selection** (shape authoring context) is a session value, never persisted.

### 3.2 Aggregates and invariants

| Aggregate (root) | The one invariant it protects |
|---|---|
| **Workspace layout** (workspace id) | Every pane registered in this build has **exactly one placement**: one position in one tab group of one region, one position in one float, or `closed` |
| **Recent files** (the list) | At most 10 entries, unique by exact absolute path, most recent first |

The layout file contains up to three workspace-layout aggregates plus the active-workspace pointer. A save writes the
file; a conflict merge (§4.4) replaces whole workspace records, never parts of one. Selection is a value object (§3.6).

### 3.3 Durable representation, grain, history rule

- **Representation:** Type-1 replaceable JSON files, installation-scoped, outside document history (A3.1, ADR-0009 §5 —
  the ADR that records this choice). No facts: nothing refers to a past layout. **History rule: Type-1 for every
  attribute**, a recorded decision to discard layout history (DM10).
- **Grain:** one layout file = one installation's layouts; one workspace record = one workspace's current layout; one
  pane reference = one pane's placement in one workspace; one recent entry = one file the user opened, by absolute path.
- **Measures:** sizes and float geometry are **non-additive** lengths. The recent count is a bound.
- **Derive, don't store:** not stored — the Dock object graph (built by the factory), the maximized pane, selection,
  camera, estimates (CAD-17), on-screen-ness of a float (recomputed by the clamp). The live Dock
  model is the session authority; the file is its snapshot at the save points of §4.3. No cache is materialized.

### 3.4 Layout file schema — `cfdw-layout`, version 1

Location: `<ApplicationData>/CFD-Workbench/layout/layout.json` (macOS `~/Library/Application Support/…`, Verified). Each
preference file has **its own subdirectory** so each has its own store claim (§4.4). UTF-8, no BOM, `\n`, ≤ 64 KiB.

```json
{
  "format": "cfdw-layout",
  "version": 1,
  "active": "planform",
  "workspaces": [
    {
      "id": "planform",
      "views": { "arrangement": "plan-3d", "single": "plan" },
      "regions": [
        { "id": "left",   "open": true,  "size": 260, "groups": [ { "panes": ["browser"], "active": "browser", "share": 1.0 } ] },
        { "id": "bottom", "open": false, "size": 190, "groups": [ { "panes": ["points", "messages"], "active": "points", "share": 1.0 } ] },
        { "id": "right",  "open": false, "size": 260, "groups": [] }
      ],
      "floats": [
        { "panes": ["properties"], "active": "properties", "x": 1620, "y": 140, "width": 260, "height": 520,
          "screen": { "x": 1512, "y": 0, "width": 2560, "height": 1440 },
          "origin": { "region": "left", "group": 0, "index": 0 } }
      ],
      "closed": []
    }
  ]
}
```

| Member | Domain | Writer | Reader (compute reader) |
|---|---|---|---|
| `format` | exactly `"cfdw-layout"` | `LayoutCodec.Serialize` | `LayoutCodec.Peek`, `Parse` |
| `version` | integer; this build reads and writes 1 | `Serialize` | `Peek` (§3.5 step 1) |
| `active` | workspace id | `ShellHost.Snapshot` | `ShellHost.Restore` |
| `workspaces[]` | ≤ 3, unique `id ∈ {planform, precision, review}` | `Snapshot` | `ShellLayout.Build` |
| `views.arrangement` / `.single` | `plan-3d`·`four`·`one` / `plan`·`3d`·`side`·`front` | `Snapshot` | `ModelArea` |
| `regions[]` | ≤ 3, unique `id ∈ {left, right, bottom}` | `Snapshot` | `ShellLayout.Build`; F6 order |
| `regions[].open` | bool (⌘B / ⌘J / ⌥⌘B) | `Snapshot` | `ShellLayout.Build` |
| `regions[].size` | DIP: left/right 200–420; bottom 120–480 | `Snapshot` | `ProportionFor` (§6.6) |
| `groups[]` | ≤ 4 per region; `panes` 1–4 ids; `active ∈ panes`; `share` in (0, 1] | `Snapshot` | `ShellLayout.Build` |
| `floats[]` | ≤ 4; `panes`, `active` as a group | `Snapshot` | `ShellLayout.Build` |
| `floats[].x`, `.y` | integer, `Window.Position` units | `Snapshot` of `HostWindow` | `FloatPlacement.Clamp` |
| `floats[].width`, `.height` | DIP, 160–8192 × 120–8192 | `Snapshot` | `Clamp`, `FloatFrame.From` |
| `floats[].screen` | bounds of the screen it was on (`Screen.Bounds` units) — a matching hint only; **no display name** (minimization: a Sidecar/AirPlay name can be personal) | `Snapshot` | `Clamp` (match by bounds) |
| `floats[].origin` | region + group index + tab index to dock back to | set at Float | dock back (Escape, ⤓, relocation) |
| `closed[]` | pane ids | `Snapshot` | Window ▸ Panes |

`groups` stays because split drops cannot be blocked by overriding validation (Verified); **S8 runs before G0** (§14):
if `CanDrop`/capability overrides do block splits, G0 lands the one-group shape (`panes` + `active` per region; no
`groups`, `share`, `origin.group`) and V8's group rows and `Snapshot_SplitGroup_RoundTrips` are deleted before any code.

Serialization: `System.Text.Json` with a **source-generated** `JsonSerializerContext` over closed records, no
polymorphism, no type names (ADR-0009), `UnmappedMemberHandling = Disallow`, `MaxDepth = 8`, enums as closed lowercase
strings, stable member order, indented output. Same input → same bytes (golden fixture).

**Presets** (code): sizes are v10's named sizes (`docs/mockups/workbench-v10.html`:381: left/right Narrow 200 · Default
260 · Wide 360; bottom Narrow 130 · Default 190 · Wide 300 — Verified).

| Workspace | views | left | bottom | right |
|---|---|---|---|---|
| `planform` | `plan-3d` | open 260: [properties*, browser] | hidden 190: [points*, messages] | hidden 260, empty |
| `precision` | `plan-3d` | open 260: [properties*, browser] | **open** 190: [points*, messages] | hidden 260, empty |
| `review` | `four` | hidden 260: [properties*, browser] | hidden 190: [points*, messages] | hidden 260, empty |

`assume:` bottom splitter range 120–480 DIP (the spec fixes only the left range). Confirm at the next `/ui-design`;
if wrong, one range constant changes.

### 3.5 Validation, rollback and migration — version first

**Step 1 — `LayoutCodec.Peek` (lenient):** with `JsonDocument` (store-bounded bytes, any size up to the store's cap, any
members; a `Peek` failure itself falls to V2), read only `format` and `version`. If `format` is `"cfdw-layout"` and
`version` is an integer > 1 → **V3 wins
over every other rule**: presets for the session and **this build never writes the file**. Only then does step 2 apply.
This is what keeps rollback safe: a v2 file adds members, and without the peek a strict v1 parse would call it corrupt
(V2) and overwrite it.

**Step 2 — strict parse per workspace:**

| # | Condition | Result | Code |
|---|---|---|---|
| V1 | file absent (checked with `File.Exists` before the read, message-level only) | presets; first save create-only | outcome `preset-first-run` |
| V2 | same or unknown-older version and: > 64 KiB, not UTF-8, BOM, depth > 8, not JSON, `format` wrong, top-level unknown member, `version` not an integer ≥ 1 | all presets; the file may be replaced at the next save (Type-1; publication is atomic, so the damage was external) | `LAYOUT-SCHEMA` |
| V3 | `version` > 1 (step 1) | presets; never written; said once | `LAYOUT-VERSION` |
| V4 | a workspace record has an unknown member, wrong type, unknown or repeated `id` | that workspace uses its preset | `LAYOUT-WORKSPACE` |
| V5 | unknown pane id, or a pane placed a second time | reference dropped (first wins: regions left→bottom→right, floats, closed) | `LAYOUT-PANE` (count) |
| V6 | a registered pane has no placement | placed where its preset puts it | `LAYOUT-PANE` (count) |
| V7 | size, share or float size out of range; non-finite number | clamped; non-finite float geometry → the float docks to its origin | `LAYOUT-CLAMPED` (count) |
| V8 | empty group; `active` not in its group; > 4 groups or floats | empty group removed; first pane active; extras dropped (panes → V6) | `LAYOUT-PANE` |
| V9 | `active` unknown | `planform` | `LAYOUT-WORKSPACE` |
| V10 | store read returns `DOC-SIZE`, `DOC-IO`, `DOC-CONFLICT` | presets for the session; **never write** this session (the content is unknown) | `LAYOUT-SCHEMA` + store code |

**Migration policy.** The reader is strict, so any added member or value is `version` + 1. A build that reads *n*
migrates *n − 1 → n* in memory and writes *n* at its next save. There is no version 0 and no migration code yet (no
dead code). **Rollback fixture** (D6, red-first): three v2 variants — an unknown top-level member, an unknown workspace
member, and a > 64 KiB file — each read by the v1 build through all three save points; assert the bytes are unchanged.
The same version-first rule and fixtures apply to `recent.json`.

**Freeze gate:** v1 ships in M1.2e. The float geometry is the only platform-sensitive field; **S1 gates freezing** it for
Windows (a new member → v2 with a v1 → v2 migration before any Windows build writes). **S6** gates M1.2e's merge.

### 3.6 Selection model (session value, not persisted)

```csharp
// src/CfdWorkbench.Desktop/Selection.cs — one value, owned by WorkbenchController (ADR-0009 §4)
public abstract record Selection
{
    public sealed record None : Selection;                                   // no foil open
    public sealed record Foil : Selection;                                   // foil open, nothing picked
    public sealed record Station(int Index, double Eta) : Selection;         // an assignment, by index + exact η
    public sealed record Points(IReadOnlyList<PointRef> Items) : Selection;  // 1..n points
}
public sealed record PointRef(string Curve, string VertexId, string? Profile = null);
```

- **Owner:** `WorkbenchController.Selection` + `SelectionChanged`; every view and pane observes it and holds none.
- **Consistent state before notify (Observer rule):** after every accepted revision, draft update, Undo/Redo or open,
  the controller (1) replaces its projections, (2) reconciles the selection, (3) raises `SelectionChanged` if it
  changed, (4) raises `Changed`. `Select` called from inside a handler is queued and applied after the current
  notification completes (no re-entrant notify).
- **Reconcile:** `Points` keeps the items that exist in the new projection; none left → `Foil`. `Station` keeps `Index`
  if that assignment's `Eta` is exactly equal; else the index with that exact `Eta`; else `Foil`. Closing → `None`.
  Explicit ids make this exact (`FoilSource.MaterializeIds`).
- **Selection never retargets a draft** (defect class GEO-C). Section editor mode swaps to section points and restores
  the entering `Station` on exit.

### 3.7 Properties is a projection

`PropertiesView.Build(Selection, AuthoredProjection, WingEstimates?, Mode) → PropertiesModel` is pure
(`src/CfdWorkbench.Desktop/PropertiesView.cs`): heading, typed rows and — whenever a foil is open — the Wing block
**last** (CAD-17, UI-36). M1.2a rows: foil and station rows; Wing block with the typed **Span** input and Root chord /
Tip chord as text (inputs in M1.2b); a closing tip shows COPY-108 "Tip closes — edit the tip station". Section mode:
Wing dimensions as text with COPY-122. Point rows arrive with M1.2b.

### 3.8 Receipt expansion for typed Span (ADR-0007 §1: "design-slice names them")

`ApplySpan(text)` parses with `DecimalSi.Parse(text, -3)` (mm, `src/CfdWorkbench.Core/Identity.cs`:12) — unit suffixes and
`#` references are M1.2b's (CAD-16's Tip-chord clause). It calls `ApplyDimension` (ADR-0006 §5): one accepted row,
never a recovery row. Receipt, reusing the as-built shape (`AuthoringSession.cs`:871): **`rail = "dimension"`,
`vertexId ∈ {"span", "root-chord", "tip-chord"}` (the dimension name, never the typed value), `draftId` = the
operation id, `generation` = 0**. `EditReference` gains `"dimension"`; `RecoveryReference` keeps refusing it (a
dimension never has a recovery row), with a fixture. **Deviation recorded:** an old build refuses with `DOC-REFERENCE`
(`AuthoringSession.cs`:897-903, :934-937 — `_ => false`), as ADR-0007 §1 predicts for a new `rail` value alone, not
architecture §10.4's `DOC-UNSUPPORTED-FIELD`. The "old build refuses" fixture lands **red-first, before** the
`EditReference` change, so it runs against the old validator once. `OpenFailure` maps `DOC-REFERENCE` to NotRecognised.

## 4. Persistence — the preference store

### 4.1 Shape

```csharp
// src/CfdWorkbench.Persistence/PreferenceStore.cs
public sealed class PreferenceStore(string root, Func<IProjectStore> storeFactory)  // default: () => new ProjectStore()
{
    public Task<LayoutLoad> LoadLayoutAsync(IReadOnlySet<string> registeredPanes, CancellationToken ct);
    public Task<PrefSave>   SaveLayoutAsync(Func<LayoutDocument> snapshot, IReadOnlySet<string> changedWorkspaces, CancellationToken ct);
    public Task<RecentLoad> LoadRecentAsync(CancellationToken ct);
    public Task<PrefSave>   UpdateRecentAsync(RecentOp op, CancellationToken ct);   // Add(path) | Clear
}
public sealed record LayoutLoad(LayoutDocument Layout, string Outcome, IReadOnlyList<string> Codes,
    int DroppedPanes, int Clamped, bool NeverWrite, bool SessionOnly, string? DiskSha256);
public sealed record PrefSave(string Outcome, string? Code, bool PublicationKnown, bool DurabilityConfirmed, bool Retried, string? ClaimPath);
```

Ladder: need exists (UX-31/32, CAD-14) → **reuse** `ProjectStore` (atomic, no-symlink, 0600, fsync, CAS) → BCL
`System.Text.Json` source generation and `SemaphoreSlim`. No new dependency.

### 4.2 Load

`File.Exists` (V1) → `ReadAsync` through the store → `Peek` (V3) → size check → `Parse` → `LayoutLoad` with the disk
hash for compare-and-swap. `DOC-UNSUPPORTED-PERSISTENCE` → presets, `SessionOnly`. Other read codes → V10.

### 4.3 Save points and serialization

The layout is written at a **workspace switch** (the workspace left), **Reset layout**, and **main-window close**.
**`simplify:`** no write per pane move — ceiling: a crash loses layout changes since the last save point; upgrade
trigger: an operator report of a lost layout after a crash.

**One `SemaphoreSlim(1)` in `PreferenceStore` serializes every save of both files** (each file has its own claim, but
one gate keeps in-process saves strictly ordered and bounded). The layout **snapshot is taken after the gate is
acquired** (`Func<LayoutDocument>` invoked on the UI thread via the dispatcher), so a queued save always writes the
latest state; `changedWorkspaces` is the **union since the last successful write**, kept by `ShellHost` and cleared
only on success (prevents the lost update of a replaced set).

**Close and quit (Native Desktop finding, Verified):** the main window **always cancels the first `Closing`**. It takes
the layout snapshot synchronously before any float closes, settles the unsaved-document choice as today
(`MainWindow.axaml.cs`:477-494), awaits the save with a 2 s bound, then **re-issues the original intent**:
`TryShutdown()` when the close came from application shutdown (⌘Q, logout), else `Close()`. Residual: macOS logout is
cancelled rather than deferred while a choice is open (Avalonia has no `NSTerminateLater`).

### 4.4 Conflicts, a second app copy, and the stale claim

`SaveRequest(expectedDiskSha256 = last read or written hash)`; create-only when none was read. On `DOC-CONFLICT`
(which covers both claim contention and a content change, `ProjectStore.cs`:108-110):

1. Re-read. **Newer version** → V3 (never write). **The re-read itself fails** (`DOC-SIZE`, `DOC-IO`, `DOC-CONFLICT`)
   → step 4, never write (as V10).
2. **Disk hash unchanged** → the claim was busy: retry once with the same image.
3. **Hash changed** → merge: replace only this session's `changedWorkspaces` records in the disk document (the disk's
   `active` wins unless this session switched workspace since its last write), retry once.
4. A second conflict → **session-only** for the rest of the session, said once, naming the claim path.

*Rationale kept over the Simplifier's cut (Data & Persistence ruling):* without steps 2–3 a merely busy claim costs the
whole session's layout, and the merge is what keeps writes at aggregate grain (§3.2). This is **Optimistic Offline
Lock** with a record-grain merge.

`recent.json` uses the same steps with **re-apply** instead of merge: re-read, re-apply this session's operation
(prepend + dedupe + truncate to 10, or Clear), retry once. A Clear that still fails — including when `recent.json` is
a newer version (V3) — is reported **"Recent list not cleared"**, never silently.

**Stale-claim recovery action (architecture §10.7 asks for it).** A claim left by a crash inside the claim window makes
every overwrite of *that file* fail. Per-file subdirectories confine it to one file. The action: the once-per-session
message names the file and offers **Show in Finder**, which opens the folder holding the lock file
`.cfd-writer.claim` (`…/CFD-Workbench/layout/` or `…/recent/`); the message says Finder shows hidden files with
⌘⇧. so the user can delete it after quitting other copies; the store still never
deletes claims itself (architecture §6). Detection: `PrefSave.ClaimPath` set and the outcome `claim-held`.

### 4.5 Recent files

`<ApplicationData>/CFD-Workbench/recent/recent.json` (`{"format":"cfdw-recent","version":1,"entries":[{"path":"/abs/a.foil"}]}`),
≤ 10 entries, absolute paths ≤ 1024 bytes ending `.foil` or `.cfdw.json`. Updated after each successful open and by
**File ▸ Open Recent ▸ Clear Menu**. Separate from the layout: it changes on every open, holds personal data the layout
must not, and a corrupt list must not reset layouts ([decision note](../notes/recent-files-preference.md)). Directories
are created 0700 on macOS; an existing directory with a wider mode is tightened and the result reported
(`Directory.CreateDirectory(path, UnixFileMode)` is not used on Windows).

## 5. Contracts

### 5.1 Exposed

| Contract | Shape | Guarantee |
|---|---|---|
| `LayoutCodec` (Persistence) | `Peek(bytes) → (format, version?)`; `Parse(bytes, registeredPanes) → (LayoutDocument, codes, counts)`; `Serialize(LayoutDocument) → byte[]` | never throws; `Serialize(Parse(Serialize(x))) == Serialize(x)` byte-for-byte; deterministic bytes |
| `WorkspacePresets` (Desktop.Shell) | `Preset(workspace, panes) → LayoutDocument` | satisfies §3.2 for any registered subset |
| `FloatFrame` (Desktop.Shell, pure) | `From(PixelPoint position, Size frameSizeDip, double desktopScaling) → PxRect` | the only DIP→position-unit conversion; uses the float window's **own** `DesktopScaling` |
| `FloatPlacement` (Desktop.Shell, pure) | `Clamp(PxRect, IReadOnlyList<ScreenArea>, PxRect? hint) → PxRect`; `Clear(PxRect target, IReadOnlyList<FloatFrame>, PxRect modelArea) → IReadOnlyList<Relocation>` | clamp result fully inside one working area and idempotent; `Clear` never overlaps the target, never leaves the model area, deterministic |
| `IScreenSource` (Desktop.Shell) | `IReadOnlyList<ScreenArea> All`; `event Changed` | the real one wraps `Screens`; tests inject screens (D7-paired with S6/S3) |
| `CommandTable` (Desktop.Shell) | rows `(Id, Title, Menu, Gesture?, IsEditVerb, ICommand Command)`; `MenuFor(window)`, `Bindings(exportedGestures)`, `PaletteEntries()` | parity: every row in a menu, the palette and a key route; no menu item without a row; enabled state through `ICommand.CanExecuteChanged` |
| `EditVerbRouter` (Desktop.Shell) | `Route(verb, IInputElement? keyWindowFocus) → ToText | ToDocument | Ignored` | with a `TextBox` focused in the **key** window, Edit verbs never reach the document |
| `FocusRing` (Desktop.Shell) | region list + the existing `NextRegionIndex` (moved from `MainWindow.axaml.cs`:381-392) | skips hidden/empty regions; floats in open order; wraps |
| `FocusedTargetChanged` routed event (Desktop.Shell) | bubbling `RoutedEvent<FocusedTargetEventArgs(Rect bounds, string accessibleName)>` | raised by `Viewport` and `SectionCanvas` when the focused vertex/station inside them changes |
| `WorkbenchController` additions | `Selection`, `Select`, `SelectionChanged`; `OpenAsync(path, ct) → OpenOutcome`; `Estimates`; `ApplySpan(text)` | §3.6 ordering; §6.2 outcomes |
| `ShellEvents` (Desktop.Shell) | bounded ring (256), `Read()` | content-free (§10) |

### 5.2 Consumed

| Contract | Source | Confidence |
|---|---|---|
| Dock 11.3.12.1 (`DockControl`, `Factory`, tool/document docks, `FloatDockable`, hooks, `IDockWindow` geometry, capabilities) | ADR-0009 spike + reflection | Verified (members); owned-float behaviour Not assessed until S7 |
| Dock tab template can host our ⋯ menu button (Move to · Float · Size/Position · Maximize · Close) | Dock Fluent theme | **Inferred** — `assume:` the tool-tab template accepts a menu button; D3a's first red test (tab menu by keyboard, Shift+F10) decides; if false, the menu moves to a group header button and UX & A11y re-review |
| Split drops blockable by `CanDrop`/capability overrides | Dock | **Not assessed** → **S8 before G0** |
| Per-window `NativeMenu`; `NativeMenu.GetMenu(window)`; `NativeMenuItem.Command` honours `CanExecute` | Avalonia 11.3.14 | Menus: Verified (source); `assume:` `CanExecute` drives `IsEnabled` — first red test in D3a; if false, the builder sets `IsEnabled` from `CanExecuteChanged` |
| macOS key equivalents fire before the focused control | Native Desktop lens (source) | Verified (source); the packaged-`.app` row in U1 confirms at runtime |
| `TextBox` edit verbs; focus manager; `Screens`; `PointToScreen`; `FrameSize`; `DesktopScaling` | Avalonia 11.3.14 | Verified (members); `PointToScreen` units on Retina → S6 |
| `WingEstimates.From`, `ApplyDimension` | ADR-0006 (Core; C1) | designed in ADR-0006 |
| `IProjectStore` | `ProjectStore.cs`:35-39 | Verified |
| Section editor mode, catalog and My sections dialogs | ADR-0007 (M1.2c), ADR-0008 (M1.2d) | referenced; until M1.2c the as-built section panel is the section-editor body (§6.7) |

## 6. Patterns and structure

### 6.1 Named patterns

| Pattern (precise name) | Where | Why this, not less |
|---|---|---|
| **Command** (GoF) + one **command table** with `ICommand.CanExecuteChanged` | `CommandTable` → per-window NativeMenus, bindings, toolbar, palette | CAD-21 parity is a test over one list; menus are built once and never rebuilt on focus |
| **Presentation Model** (projection) | `PropertiesView.Build`, Browser rows | panes hold no model state (ADR-0009 §4) |
| **Observer** with "consistent state before notify" | `SelectionChanged`, `Changed` | existing idiom (`WorkbenchController.cs`:109, :762) with a defined order (§3.6) |
| **Snapshot DTO + Builder** behind an **Anti-Corruption Layer** | `ShellHost.Snapshot()` / `ShellLayout.Build()`; only `CfdWorkbench.Desktop.Shell` references `Dock.*` | the file names pane ids, not Dock types (exit from Dock 11.x); property `Snapshot(Build(x)) == Normalize(x)` |
| **Strict schema with version-first peek and per-record fallback** (bulkhead per workspace) | `LayoutCodec` | a preference must never block launch nor destroy a newer file (§3.5) |
| **Build aside, then atomic swap**, with latest-wins cancellation by generation token | `OpenAsync` | cancellation never discards the current document (§6.2); post-commit sampling runs on its own token |
| **State machine** (closed transition table) | Start / Opening / Workspace / Section editor | illegal transitions refused and tested (§6.2) |
| **Chain of Responsibility** (responder chain) | `EditVerbRouter` | text field first, then the document |
| **Collision-aware placement with ordered fallback** | `FloatPlacement.Clear` | option (a) (§6.5) |
| **Second view on one Presentation Model** | Maximize | no re-parenting of Dock-owned controls (§6.6) |
| **Optimistic Offline Lock** with record-grain merge | §4.4 | CAS on the disk hash; merge whole workspace records |
| Rejected: generic docking abstraction; plugin pane registry (closed pane set); event bus; per-kind telemetry events; Dock serializers; `Avalonia.Headless` (DR-S1); an interface for canvas focus targets (a routed event is enough); a reserved activity-rail column (YAGNI until DR-7) | — | speculative or rejected by ADR-0009 |

### 6.2 Open — outcomes and transitions

`OpenAsync(path, ct) → OpenOutcome`, a closed union: **Opened** · **NeedsIds**(candidate) · **Refused**(code; original
kept read-only, as built) · **Failed**(`OpenFailure`) · **Cancelled** · **Superseded**. **Prepare** off the UI thread:
read, parse, assess, open a *new* `AuthoringSession`; honour `ct`. **Commit** on the UI thread: if still the latest
request, `Adopt` (not cancellable); post-commit sampling uses its own token. New foil and New from example go through
the same path.

| From \ event | Open / Recent / New | Opened | NeedsIds / Refused / Failed | Cancel | a newer Open |
|---|---|---|---|---|---|
| **Start** | → Opening (focus: Cancel) | — | — | — | — |
| **Opening** | a newer Open supersedes (see last column); every other command is disabled | → Workspace (focus: model area) | → origin state + alert (focus: alert's first action) | → origin + COPY-105 (focus: the originating card or Recent row; from a menu or ⌘O with no card → the first start card on Start, the model area in Workspace) | current → Superseded, new → Opening |
| **Workspace** | unsaved choice (F1 Z), then → Opening | — | — | — | — |
| **Section editor** | refused: "Finish or cancel the section first" (F11 S11 rule), focus to Cancel | — | — | — | — |

**Origin state** keeps whatever was there: on Start the alert sits on the start card; **with a foil open the foil is
kept and the alert shows in the model-area alert band** (the band that also holds the ID-candidate and recovery offers,
§6.7). One test per illegal transition (`Open_WhileOpening_Refused`, `Open_InSectionEditor_Refused`).

### 6.3 Open-failure classification

By exception type, then code: `FileNotFoundException`/`DirectoryNotFoundException`, or a failed store read with
`!File.Exists(path)` → **Missing**; `UnauthorizedAccessException`/`EACCES` → **AccessDenied**; other `IOException`/
`DOC-IO` → **Unreadable**; `DSL-LIMIT`/`DOC-SIZE` → **TooLarge**; `DOC-VERSION`/`DSL-VERSION` → **Newer**;
`DOC-UNSUPPORTED-FIELD` → **UnknownContent**; `DOC-SCHEMA`/`DOC-TYPE`/`DOC-REFERENCE`/other `DSL-*` → **NotRecognised**.
**`simplify:`** the `File.Exists` probe only picks the message; ceiling: a file deleted during the read is named
missing (true anyway); upgrade trigger: a store read that returns `DOC-NOT-FOUND`. **Migratable** has no instance in M1
(one native format, one FoilDSL major): its copy is recorded in §11; no code path is built.

### 6.4 Command table, per-window NativeMenus, Edit verbs

- One `CommandRow` per verb with an `ICommand` (`CommunityToolkit.Mvvm` `RelayCommand` is already a pinned transitive
  of `Dock.Model.Mvvm` 11.3.12.1 — nuspec Verified — so no new package). Gesture modifier from
  `PlatformSettings.HotkeyConfiguration.CommandModifiers`.
- **Menus:** the Application `NativeMenu` holds **About** only. Avalonia supplies Services, Hide, Hide Others, Show All
  and Quit (Verified) — the table does **not** duplicate them; **Settings…** is omitted until Settings exists (HIG).
  Each window's own `NativeMenu` (main window at startup; each float in `IFactory.OnWindowOpened`) is generated from the
  table: **File** New foil · New from example · Open… · Open Recent ▸ (entries, Clear Menu) · Save · Save As… · Close.
  **Edit** Undo · Redo · Cut · Copy · Paste · Select All. **View** Left side bar ⌘B · Bottom panel ⌘J · Right side bar
  ⌥⌘B · Command palette ⌘K · Fit. **Window** Minimize ⌘M · Zoom · Planform · Precision · Review · Reset layout · Panes ▸ ·
  Bring All to Front · the float list (Avalonia sets no `windowsMenu`, Inferred). M1.2e adds Maximize pane ⌘⇧M. Exact
  placement beyond this set is a `/ui-design` decision (CAD-21).
- **Bindings per gesture, not per window:** a gesture is bound on a window iff it is not a key equivalent of that
  window's exported menu. So F6, Escape, Alt+arrows and Shift+F10 are bound on every window including floats; menu
  gestures are served by the menu on macOS and by bindings on Windows (S1).
- **Edit verbs:** asked of the **key window's** focus manager. A focused `TextBox` gets the verb (`TextBox.Undo()` etc.)
  even when `CanUndo` is false (⌘Z then does nothing); while IME composition is open the verb goes to the composition.
  Otherwise Undo/Redo reach the document (no draft open, as today); Cut/Copy/Paste are disabled.
- **Keys while a text field has focus:** unmodified and single-key shortcuts never act (B7, SC 2.1.4). **Chords the
  text system owns never leave the field**: Edit verbs, ⌥/⌘ + arrows, ⌥/⌘ + Backspace/Delete. **Alt+arrows move a float
  only when focus is not in a text field.** Other modified chords (⌘S, ⌘B, ⌘O…) first commit the field as leaving it
  would (UI-39); if the value is refused the command does not run and the error stays — **except Quit and Close**,
  which restore the shown value and continue (no trap). *Finding for the spec owner:* B7 read literally would block
  ⌘S while typing; this is the default interpretation.
- **Command palette:** Avalonia `AutoCompleteBox` over `PaletteEntries()` (plain-language names) in a dialog, Escape
  closes and returns focus; its accessibility tree is a UX & A11y check in U1 (APG combobox behaviour: `ShellWindow`
  test `Palette_Keyboard_FiltersAndRuns`).

### 6.5 Focus-safe floats (option (a))

**Triggers** — any of: `GotFocus` bubbling in the main window's model area; `FocusedTargetChanged` from a canvas (focus
moving vertex to vertex inside one `Viewport`/`SectionCanvas`); main-window `Activated`; **model-area bounds change**
(window resize, ⌘B/⌘J/⌥⌘B reflow, Views ▾ / Fit, maximize-restore); **float re-show** after restore; **end of layout
restore and end of clamp** (clamp runs **before** the initial focus is placed). A float gaining focus never triggers.

**Geometry (one unit):** target = `PointToScreen` of the target bounds inflated by the 2 px focus-ring outset. Float
frame = `FloatFrame.From(Position, FrameSize, float.DesktopScaling)` — on macOS `Screen.Scaling` is always 1 (Verified),
so the float's own scaling is used, never a saved hint. Model area = `PointToScreen` of its bounds. S6 answers the one
open unit question: does `PointToScreen` return points on Retina.

**`FloatPlacement.Clear`:** for each float intersecting the target, in z-order: candidates are the model-area corners
inset 8 px in the fixed order top-right, bottom-right, bottom-left, top-left; keep those inside the model area, clear of
the target and of other floats' final rectangles; pick the nearest to the float's centre (ties by order); none → dock
back to `origin`. Moving a float never moves focus; a re-entrancy guard ignores focus and bounds events caused by the
relocation. **Announcement:** results of one trigger are **batched into one message** in the status live region:
COPY-116 / COPY-120 with <Pane> = the float's active pane title and <target> = the target's **short name** (a point's
curve and name, not its full coordinate string); two results join with "; ".

### 6.6 Docks, sizes, Position, maximize, clamping

- **Dockables:** tools `CanPin = false`, `CanDockAsDocument = false`, `CanFloat/CanClose/CanDrag = true`; the model
  document has every capability false. Sizes are DIP; Dock sizes by proportion, so the host sets
  `Proportion = ProportionFor(px, hostExtent)` on host resize and restore, with `MinWidth`/`MaxWidth` on tool docks.
- **Size** (docked pane menu): v10's named radio sizes — Narrow · Default · Wide (left/right 200 · 260 · 360; bottom
  130 · 190 · 300); the splitter still takes arrows and Home/End for any value in range.
- **Position** (float pane menu; the single-pointer route, SC 2.5.7): v10's set — **Centre · Top right · Bottom right**
  of the model area (`workbench-v10.html`:448) — plus Alt + arrows (20 DIP, Shift × 4) when focus is on the float's
  tab or a non-text control.
- **Escape in a float:** docks back **only** when the tab strip or a non-text control has focus. In a text field Escape
  restores the shown value (UI-39) and stops there; a second Escape in the field does nothing more.
- **Maximize** (ours): the pane renders a **second view** in an overlay over the Dock host; the Dock host is invisible
  (inert: not hit-testable, not focusable, not in the AT tree); floats are hidden and re-shown at restore; focus moves
  to the same logical element and back. Both views bind **one shared per-pane view-state object** (`PaneViewState`: scroll offset, Browser expanded rows;
  Points sort and column widths join it in M1.2c), so nothing is copied back; an in-progress text edit commits first
  (UI-39). Announced "<Pane> maximized. Press Escape to restore." / "<Pane> restored" (v10:417). Escape restores
  only from a non-text control (same rule as floats). **While maximized, commands that target the Dock host restore
  first**: Window ▸ Panes, ⌘B/⌘J/⌥⌘B, workspace switch, Reset layout, Float, Move to; the palette runs them the same
  way. A float's Maximize maximizes its OS window.
- **Hiding the dock that holds focus** (⌘B etc.): focus moves to that dock's toggle in the app bar (v10:421).
- **Clamping** at restore and on `Screens.Changed`: the screen whose bounds equal the hint, else the largest
  intersection, else the primary; shrink to the working area if larger; move fully inside. Counted in `layout.load`
  and said once. Until S3 passes, clamp correctness on a real removed monitor is **Inferred** in the proof pack.
- **Floats are owned** (`FloatingWindowOwnerPolicy = AlwaysOwned`): owned floats get `FullScreenAuxiliary` and are not
  AppKit child windows (Native Desktop, source), so they can join the main window's full-screen Space but do not move
  with it. S7 asserts the owner is set before first show, front-ordering, ⌘\` and the float list.

### 6.7 Model area and modes

The model area hosts **Start** (no foil), the **views** (Plan + 3D · Four views · One view; Fit), or the **section
editor** body. Until M1.2c ships ADR-0007's draft, the as-built section panel (`MainWindow.axaml`:82-145) moves
unchanged into `SectionEditorView` so M1.1 capability is never lost. The FoilDSL source stays a second document tab
("Foil source", read-only as built). The **alert band** at the top of the model area holds the ID-candidate offer
(Accept IDs), the recovery offer (Resume / Discard) and open failures while a foil is open. No activity-rail column is
reserved (DR-7 is independent: the rail sits outside the Dock host whenever it is built).

## 7. Error and concurrency model

- **Threads:** shell state is UI-thread only; open *prepare* and store I/O run off-thread and apply only if their
  generation is current.
- **Idempotency (P8):** `ApplyDimension` memoized by operation id; the same layout snapshot gives the same bytes.
- **Cancellation:** Open (§6.2); close/quit save bounded at 2 s (§4.3); retries only as in §4.4.
- **Refusals** never change geometry or undo depth (COPY-118, COPY-106, COPY-107, span Not assessed).
- **Pane render failure:** each pane binds through one guarded `Bind(model)`; an exception first clears the pane's
  previous content (defect class UI-E), then shows the pane error state with Try again, records `shell.pane.render`
  (exception type only) and leaves other panes and the document untouched.
- **Unhandled exceptions:** the app installs `Dispatcher.UIThread.UnhandledException` and
  `AppDomain.CurrentDomain.UnhandledException` handlers that write **only the exception type and a stable code**
  (`APP-UNHANDLED`) to stderr — never `Message` or `ToString()`, which can carry file paths — and then let the process
  end as today. An exception inside Dock's own layout pass cannot be contained per pane (**accepted**; the document's
  recovery row limits the loss).

## 8. Change-surface list (E7)

| Change | Store | Model | Service | Projection | Desktop | UI | CLI | Compute reader |
|---|---|---|---|---|---|---|---|---|
| Layout file | `layout/layout.json` via `ProjectStore` | `LayoutDocument` | `PreferenceStore`, `LayoutCodec` | — | `ShellHost.Snapshot/Restore`, `ShellLayout.Build` | docks, tabs, floats, workspaces | — | clamp; F6 order |
| Recent files | `recent/recent.json` | `RecentList` | `PreferenceStore` | Start Recent rows | `OpenAsync` on success; Clear Menu | Start Recent; File ▸ Open Recent | — | — |
| Selection | none | `Selection` | controller reconcile | Properties, Browser, views | controller | highlight, Properties | — | — |
| Properties / Wing | none (CAD-17) | `PropertiesModel` | `WingEstimates.From` (C1) | `PropertiesView.Build` | pane binding | Properties | `inspect` (ADR-0006) | — |
| Typed Span | source patch; receipt `rail=dimension` | — | `ApplyDimension` (C1) | receipt | `ApplySpan` | Wing field, status | `dimension` (ADR-0006) | estimates |
| Open | — | `OpenOutcome`, `OpenFailure` | existing reads | — | `OpenAsync` | Start, Opening, alerts | — | — |
| Commands / menus | — | `CommandRow` | — | — | `CommandTable`, `NativeMenuBuilder`, `EditVerbRouter` | menus, toolbar, palette | — | — |
| Focus / F6 / floats | — | — | — | — | `FocusRing`, `FloatFrame`, `FloatPlacement`, `IScreenSource`, `ShellHost` | live region | — | — |
| Canvas focus targets | — | — | — | — | `FocusedTargetChanged` raised by `Viewport.cs`, `SectionCanvas.cs` | — | — | `FloatPlacement.Clear` |
| Crash output | stderr | — | — | — | `App.axaml.cs` handlers | — | — | — |
| Telemetry | — | `ShellEvent` | — | — | `ShellEvents` | — | — | tests |
| Dependency | `packages.lock.json` (locked) | — | — | — | Dock packages | — | — | licence register (A8.5) |
| Repo privacy review | `docs/security/privacy-review.md` row for recent paths (supersedes "M1 has no durable recent-file list") | — | — | — | — | — | — | — |
| Existing tests | — | — | — | — | the §12.5 inventory of `WorkbenchTests.cs` | — | — | — |

## 9. Failure-mode analysis

Every test name here is assigned to exactly one track in §14.

| Failure mode | From which choice | Disposition | How addressed / why accepted | Detection | Test (track) |
|---|---|---|---|---|---|
| Layout absent | own file | prevent | presets; create-only save | `preset-first-run` | `LayoutLoad_Absent_ReturnsPresets` (P1) |
| Corrupt / oversized / wrong format | own file | degrade + detect | presets (V2) | `LAYOUT-SCHEMA` | `LayoutParse_Corrupt_ReturnsPresetsWithCode`, `LayoutCodec_RandomBytes_NeverThrows` (P1) |
| One workspace bad | per-workspace parse | degrade | its preset (V4) | `LAYOUT-WORKSPACE` | `LayoutParse_BadWorkspace_OthersRestored` (P1) |
| Unknown / duplicate / missing pane | closed pane set | degrade | V5, V6 | `LAYOUT-PANE` | `LayoutParse_UnknownPane_DroppedOthersKept`, `LayoutParse_MissingPane_PlacedByPreset` (P1) |
| Out-of-range / non-finite numbers | numbers from disk | mitigate | V7 | `LAYOUT-CLAMPED` | `LayoutParse_OutOfRange_Clamped` (P1) |
| Newer version (with new members) | version policy | prevent | version-first peek (V3) | `LAYOUT-VERSION` | `Rollback_V2UnknownTopLevel_BytesUnchanged`, `Rollback_V2UnknownWorkspaceMember_BytesUnchanged`, `Rollback_V2Oversized_BytesUnchanged` (P1) |
| Read error of unknown content | store | prevent | V10 never write | code | `LayoutLoad_ReadError_NeverWrites` (P1) |
| Persistence unsupported | store macOS arm64 only | degrade | session-only | `LAYOUT-SESSION-ONLY` | `PrefStore_Unsupported_SessionOnly` (P1, fake + real contract §12.3) |
| Symlinked directory | store rule | degrade | refused → session-only | code | `PrefStore_SymlinkedDirectory_SessionOnly` (P1) |
| Layout and recent saves overlap in-process | one claim per directory | prevent | subdirectories + one semaphore | — | `PrefsSave_LayoutAndRecentConcurrent_BothKept` (P1) |
| Busy claim (another copy) | CAS | recover | retry same image once | `retried` | `LayoutSave_ClaimBusyHashSame_RetriesOnce` (P1) |
| Another copy changed the file | CAS | recover | merge changed workspaces, retry once | `retried` | `LayoutSave_Conflict_MergesChangedWorkspaceOnly` (P1) |
| Changed set lost across queued saves | serialization | prevent | union since last success | — | `LayoutSave_TwoQueuedSaves_UnionOfChangedWorkspaces` (P1) |
| Stale claim after a crash | store claim policy | detect + recover (manual) | per-file claim; message + Show in Finder | `claim-held` | `LayoutSave_StaleClaim_SessionOnlyNamesClaim` (P1) |
| Recent add / Clear conflicts | CAS | recover | re-apply op, retry once; failed Clear reported | code | `Recent_Conflict_ReappliesAdd`, `Recent_ClearFails_ReportedNotCleared` (P1) |
| Clear leaves the path on disk | erasure | prevent | Clear writes an empty list; test scans the directory | — | `Recent_Clear_NoFileContainsMarkerPath` (P1, red-first) |
| Tampered recent entry | user-writable | mitigate | absolute, extension, length checks | `RECENT-SCHEMA` | `RecentParse_RelativePath_Dropped` (P1) |
| Crash between save points | save points | accept | `simplify:` §4.3 | — | — |
| Quit with a float, clean document | ⌘Q path | prevent | first `Closing` always cancelled; snapshot before floats close | `layout.save` `close` | `Quit_CleanDocumentWithFloat_SavedLayoutHasFloat` (D4) |
| Slow disk at close | bounded save | mitigate | 2 s, then close | `timeout` | `Close_SlowLayoutSave_ClosesWithinBound` (D4) |
| Saved monitor gone | floats anywhere | mitigate | clamp | `clamped n` | `Clamp_ScreenGone_FullyInsidePrimary` (D1); S3 |
| Monitor removed live | `Screens.Changed` | mitigate | clamp live | `clamped n` | `ScreensChanged_FloatOffscreen_Clamped` (D4, injected `IScreenSource`) |
| Unit mismatch DIP vs position | one conversion | detect | `FloatFrame.From` with own scaling; S6/S2 | — | `FloatFrame_Scaling1_15_2_Exact`, `FloatRestoreSnapshot_NoDrift` (D1) |
| Float covers a focused target | floats over model | prevent | §6.5 | `float.relocate` | `Clear_NearestClearCorner_Chosen`, `Clear_TieBreak_FixedOrder`, `Clear_TwoFloats_NoOverlap`, `Clear_NeverOverlapsTarget_Property` (D1); `Viewport_FocusVertex_RaisesFocusedTargetChanged`, `SectionCanvas_FocusVertex_RaisesFocusedTargetChanged` (D3a); `Relocation_FocusedControlUnderFloat_Moves`, `Relocation_CanvasVertexUnderFloat_Moves`, `Relocation_CanvasVertexUnderFloat_DocksBack`, `Relocation_ModelAreaShrinks_Moves` (D4) |
| Relocation loops | trigger on events | prevent | re-entrancy guard | — | `Relocation_DoesNotRetrigger` (D4) |
| Float larger than the model area | option (a) | mitigate | dock back | `docked-back` | `Clear_FloatLargerThanModelArea_DocksBack` (D1) |
| Maximize loses view state or typed text | second view | prevent | `PaneViewState`; commit first | — | `Maximize_WithEditedField_CommitsFirst`, `Maximize_SharedViewState_BothViewsBound` (D4) |
| Command targets hidden Dock while maximized | overlay | prevent | restore first | — | `Maximize_ToggleDock_RestoresFirst` (D4) |
| Focus lost on Dock re-render (UI-C) | Dock rebuild | prevent | restore by logical id | — | focus rows §12.4 |
| Unexpected split | drop not overridable | mitigate | S8; groups persisted | `shell.pane.move` | `Snapshot_SplitGroup_RoundTrips` (D4; deleted if S8 blocks splits) |
| ⌘Z in a text field undoes the foil | key equivalents first | prevent | router | — | `EditVerb_UndoInSpanField_EditsText` (D3a) + packaged-`.app` row (U1) |
| Menu missing while a float is key | Application menu model | prevent | per-window menus | — | `NativeMenu_FloatWindow_HasFileEditWindow` (D4) + S7 |
| Gesture handled twice / dead in a float | bindings vs menus | prevent | per-gesture rule | — | `KeyBindings_MenuGesture_NotBound`, `KeyBindings_F6InFloat_Bound` (D3a) |
| Menu enabled state stale | commands | mitigate | `CanExecuteChanged` | — | `Menu_UndoEnabled_FollowsFocusAndHistory` (D3a) |
| Alt+arrow in a field moves the float | chord rule | prevent | text-owned chords stay in the field | — | `Float_AltArrowInTextField_MovesCaretNotFloat` (D4) |
| Escape in a float field docks it | Escape rule | prevent | §6.6 | — | `Float_EscapeInTextField_RestoresTextOnly` (D4) |
| Cancel after commit | open hazard | prevent | build aside, swap | `DOC-CANCELLED` | `Open_CancelDuringPrepare_CurrentFoilUnchanged`, `Open_CancelAfterCommit_Ignored` (D2) |
| Overlapping Opens | async | prevent | latest wins | — | `Open_SecondRequest_SupersedesFirst` (D2) |
| Non-throwing open outcomes lost | as-built returns | prevent | `OpenOutcome` union | — | `Open_IdCandidate_NeedsIds`, `Open_Uncertified_RefusedReadOnly` (D2) |
| Cause unclear | coarse codes | mitigate | `OpenFailure` | code | `OpenFailure_Missing_Classified`, `OpenFailure_AccessDenied_Classified`, `OpenFailure_Unreadable_Classified`, `OpenFailure_TooLarge_Classified`, `OpenFailure_Newer_Classified`, `OpenFailure_UnknownContent_Classified`, `OpenFailure_NotRecognised_Classified` (D2) |
| Pane binding throws | panes | degrade | §7 | `shell.pane.render` | `PaneBind_Throws_ShowsErrorStateClearsOld` (D3a) |
| Crash output leaks a path | default handler | mitigate | type + code only | `APP-UNHANDLED` | `Unhandled_Exception_StderrHasNoMarkerPath` (D3a) |
| Stale selection projected | notify order | prevent | §3.6 | — | `Selection_ReconciledBeforeChanged`, `Selection_Reconcile_PointsDropped`, `Selection_Reconcile_StationByExactEta` (D2) |
| Wing block missing or not last | projection | prevent | §3.7 | — | `Properties_WingBlockLast_WhenFoilOpen` (D2) |
| Preset violates the one-placement invariant | presets | prevent | §3.2 | — | `Preset_EveryPaneSubset_ExactlyOnePlacement` (D1) |
| F6 skips or traps a region | ring | prevent | §5.1 | — | `FocusRing_HiddenRegionsAndFloats_Order` (D1); `F6_IntoAndOutOfFloat` (D4) |
| A verb unreachable (CAD-21) | one table | prevent | parity | — | `CommandTable_Parity_EveryRowInMenuPaletteKey` (D1), `NativeMenu_MainWindow_BuiltFromTable` (D3a) |
| Dock sized in proportion drifts from px | proportion | prevent | `ProportionFor` | — | `ProportionFor_Bounds` (D1) |
| Dock referenced outside the shell | ACL | prevent | architecture test | — | `Architecture_DockConfinedToShell` (D3a) |
| Fake store drifts from the real one | D7 | prevent | one contract run on both | — | `StoreContract_CancelBeforePublish_DocCancelled` (P1) |
| Content leaks into events or stderr | telemetry | prevent | §10 | — | `Telemetry_MarkerInjection_AbsentEverywhere` (D3a) |
| Dock internal layout exception | third party | accept | recovery row | stderr code | — |
| Estimates slow | per-frame | detect | `estimates.compute` | event | `WingEstimates_Compute_EmitsEvent` (C1) |
| Wrong estimate (CAD-17) | one Core function | prevent | ADR-0006 definitions | — | `WingEstimates_ConstantChord_AreaArMeanMac`, `WingEstimates_LinearTaper_MacDiffersFromMean`, `WingEstimates_DraftBytes_DifferFromAccepted` (C1) |
| An estimate stored (CAD-17) | derive, don't store | prevent | never written | — | `WingEstimates_Save_NoEstimateInFile` (C1) |
| Typed Span changes chords or η (CAD-16) | `ApplyDimension` | prevent | half-span patch only | — | `ApplyDimension_Span15b_HalfSpanExact`, `ApplyDimension_Span_ChordAt201EtaUnchanged`, `ApplyDimension_Span_StationEtaUnchanged` (C1) |
| Span commit not one undo step (CAD-16) | one accepted row | prevent | ADR-0006 §5 | apply event `edit_kind=dimension` | `ApplyDimension_Span_OneUndoStepUndoExact`, `ApplyDimension_SameOperationId_Memoized` (C1) |
| Invalid Span changes geometry (CAD-16) | refusals | prevent | refused before any patch | code | `ApplyDimension_SpanNotNumber_RefusedUnchanged`, `ApplyDimension_SpanNonPositive_RefusedUnchanged` (C1) |
| Old build misreads a dimension receipt | receipt expansion | prevent | old validator refuses | `DOC-REFERENCE` | `Receipt_Dimension_OldReaderRefusesDocReference` (C1, red-first), `Recovery_Dimension_Refused` (C1) |
| Span editable in the section editor | draft rollback (UI-S) | prevent | Wing as text | — | `Properties_SectionMode_WingAsText` (D2) |

## 10. Telemetry (normal path, no flag)

Shell events go to a **process-lifetime** `ShellEvents` ring (256, same bounded semantics as the session ring,
`AuthoringSession.cs`:77-87); the session ring is disposed at every open (`Adopt`). Local only; no exporter
(architecture §7). Attributes are closed ids, counts, durations and codes; every event has a sequence and a 32-hex
`trace_id`. The store makes its own trace id per call (`ProjectStore.cs`:58), so `layout.save` copies the store outcome
from `PrefSave` instead of sharing a trace. A missing measurement reads "Not recorded".

| Event | Attributes | Answers |
|---|---|---|
| `layout.load` | outcome (`restored` · `preset-first-run` · `preset-fallback` · `session-only` · `never-write`), codes[], dropped_n, clamped_n, bytes, duration_ms | did restore fail, and how |
| `layout.save` | trigger (`switch` · `reset` · `close`), outcome (incl. `claim-held`, `timeout`), code, bytes, duration_ms, publication_known, durability_confirmed, retried | is the layout kept |
| `recent.save` | op (`add` · `clear`), outcome, code, retried | is the rights path working |
| `shell.workspace.switch` | from, to, duration_ms | workspace use; switch cost |
| `shell.pane.move` | pane, from, to (`left` · `right` · `bottom` · `float` · `closed`) | pane moves |
| `float.relocate` | outcome (`moved` · `docked-back`), pane, corner, duration_ms | how often option (a) fires |
| `shell.pane.render` | pane, code `SHELL-PANE-RENDER`, exception_type | pane failures |

Existing events stay (apply event with `edit_kind = dimension`; `estimates.compute`; store events). **Stable codes
added:** `LAYOUT-SCHEMA`, `LAYOUT-VERSION`, `LAYOUT-WORKSPACE`, `LAYOUT-PANE`, `LAYOUT-CLAMPED`, `LAYOUT-SESSION-ONLY`,
`LAYOUT-CONFLICT`, `RECENT-SCHEMA`, `SHELL-PANE-RENDER`, `APP-UNHANDLED`. No HTTP surface (no RFC 9457).
**No-content test** (`Telemetry_MarkerInjection_AbsentEverywhere`, D3a): open a file whose path holds a unique marker
(and a Windows-style `\` variant), float a pane on a screen, save; assert the marker is absent from the shell ring, the
preference store's ring, the session ring and captured stderr.

## 11. UI and interaction design

**Medium:** native desktop, macOS first (Apple HIG: menu bar, ⌘ shortcuts, Window menu, owned auxiliary windows);
Windows deferred (S1, S4). Archetype **G1 Parametric Modeling Workbench** with the 1.6 CAD-area deviations (C1).
**Motion:** none (v10). **Tokens:** no new tokens; `DESIGN.md` §2, §5, §12.0e; Properties labels 56 px (72 px Wing).
`assume:` `Styles.axaml` resources carry these tokens by name (checked at D3a start; a missing one is added from
`DESIGN.md`, never invented). Numbers are tabular with units at one precision per quantity (UI-40, TQ).

**Key screens:** Start (focal point: first start card); Planform (focal point: model area; Properties narrow at left);
a float over the model; a maximized pane.

**States:**

| Component | default | focus | disabled | loading | empty | error | success | first-run / overflow |
|---|---|---|---|---|---|---|---|---|
| Start card | 3 card buttons + Recent | first card (UX-28) | — | Opening `status` + Cancel over a static skeleton | Recent hidden when empty | alert with actions (below) | workspace | 64-char names never overlap |
| Model-area alert band | hidden | first action on show | — | — | — | open failure with a foil open; ID candidate; recovery | — | — |
| Properties | rows + Wing | ring | section mode: Wing as text + COPY-122 | — | "No foil open" + "Open or start a foil. Whatever you select in it shows its properties here." | pane error | — | 200 px: nothing clipped |
| Wing Span field | mm, 0.01 | ring | section mode → text | — | — | COPY-118 / COPY-106 / COPY-107 / Span not assessed; invalid state + alert | one undo step; estimates update | Escape restores |
| Browser | sections + profiles | ring | — | — | "No foil open" (single rendering) | pane error | — | full name in the accessible name |
| Dock region | tabs | tab | — | — | v10: "This side bar is empty. Drag a pane's tab here, or choose ⋯ → Move to right side bar on any pane." / "This panel is empty. … Move to bottom panel …" + Close | — | — | — |
| Tab menu | Move to · Float · Size (docked) or Position (float) · Maximize · Close | first item; Escape → tab | a disabled size at its limit says why | — | — | — | — | — |
| Float | owned window titled by its pane | Alt+arrows (non-text focus), Position menu, Escape/⤓ dock back | — | — | — | — | COPY-116 / COPY-120 (batched) | clamped at launch (said once) |
| Maximized pane | fills window | Tab stays inside; Escape restores | rest inert | — | — | — | maximized / restored announced | — |

**Copy — new strings (C2 voice: cause · preserved work · next action; one state, one string). `<file>` is the file
name only; the full path is the accessible description. Spec Part C is flagged review-suggested for these.**

| State | String | Where | Trace |
|---|---|---|---|
| Open failed — missing | "“<file>” didn't open. It isn't where it was — it may have been moved, renamed or deleted. The file hasn't been changed." · **Locate…** · **Open another file…** · from Recent: **Remove from Recent** | start card / alert band | CAD-14, UX-29 |
| Open failed — permission | "“<file>” didn't open. CFD Workbench isn't allowed to read it. The file hasn't been changed. Check its permissions in Finder, or open another file." · **Open another file…** | same | CAD-14, UX-29 |
| Open failed — unreadable | "“<file>” didn't open. It couldn't be read from the disk. The file hasn't been changed." · **Try again** · **Open another file…** | same | CAD-14, UX-29 |
| Open failed — not recognised | "“<file>” didn't open. It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed." · **Open another file…** | same | CAD-14, UX-29 |
| Open failed — too large | "“<file>” didn't open. It is larger than CFD Workbench can open (<limit>). The file hasn't been changed." · **Open another file…** | same | CAD-14 |
| Open failed — unknown content | "“<file>” didn't open. It contains parts this version doesn't understand. The file hasn't been changed. A newer version of CFD Workbench may open it." · **Open another file…** | same | CAD-14 (F1 H4) |
| Migratable (no instance in M1) | "“<file>” was saved by an earlier version. CFD Workbench can open a converted copy; the original stays as it is." · **Open a copy** · **Cancel** | same | CAD-14 (F1 H3) |
| My sections empty | "No saved sections yet. To keep one here, choose Save to My sections… from the Section menu in the section editor." | catalog dialog | CAD-18, UI-38 |
| Catalog cannot load | "The catalog didn't load: <cause>. Your section hasn't changed. Choose Cancel to go back." (<cause>: "a catalog file is missing from this installation" · "a catalog file failed its check") | catalog dialog | CAD-18, UI-38 |
| Span not assessed | "The new span couldn't be checked. Span is unchanged. Try again or enter a different value." | Wing field error | CAD-16 |
| Layout fallback | "Your saved layout for <Workspace> couldn't be read, so its preset is shown." | Messages + status | UX-32 |
| Layout newer | "This layout was saved by a newer version of CFD Workbench. The presets are shown, the file is left as it is, and layout changes in this session won't be kept." | Messages + status | UX-32 |
| Layout not kept — platform | "Layout changes won't be kept after you quit: this system can't save them safely." | Messages + status | ADR-0009 §5 |
| Layout not kept — another copy | "Layout changes won't be kept after you quit: the layout file is in use. Quit other copies of CFD Workbench, then remove the lock file Show in Finder selects." · **Show in Finder** (the claim path is the button's accessible description) | Messages + status | §4.4 |
| Recent not cleared | "The recent-files list wasn't cleared: <reason>. The list is unchanged." · **Try again** | status | privacy rights path |
| Floats clamped | one: "A floating pane was moved onto a connected display." · many: "<n> floating panes were moved onto a connected display." | Messages + status | UX-32 A-Monitor-gone |
| Maximize | "<Pane> maximized. Press Escape to restore." · "<Pane> restored" (v10:417) | status | UX-32 |
| Pane render failure | foil open: "<Pane> couldn't be shown. Your foil hasn't changed." · no foil: "<Pane> couldn't be shown." · **Try again** | inside the pane | §7 |

**Live regions:** one polite **status** live region carries every one-time message; the **Messages** pane (M1.2c) is
role `log` (polite) — an obligation on M1.2c. A layout message goes to the Messages log when the bottom panel is
visible and to the status region otherwise, never both (no double speech). **Accessibility:** WCAG 2.2 AA; SC 2.4.11 (option (a), §6.5 triggers), 2.1.1 and 2.5.7 (splitter → Size;
tab → Move to; float → Position), 2.4.3 (F6 ring), 3.3.1 (UI-39), 4.1.3 (status). Native AX proof (VoiceOver tree and
trace, incl. the `Activated` and `Screens.Changed` announcement paths) is U1's; NVDA is S5. **Performance:** a workspace
switch should render within one frame after the Dock rebuild — **Inferred**, measured by `shell.workspace.switch`, not
a gate (on-screen timing is not an M1 gate).

## 12. Test plan

### 12.1 Triggered directives

| Trigger | Where | Directive and how it is met |
|---|---|---|
| — | all | **D0**: `Method_State_Outcome`, AAA, no sleeps (bounded dispatcher loops), no wall clock, temp directories per test |
| T1 | presets, `FloatFrame`, `FloatPlacement`, `FocusRing`, `CommandTable`, selection, `PropertiesView`, `ProportionFor`, `OpenFailure` | **D1**, exact-value assertions (rectangles, corners, codes); mutation tooling is not wired → mutation confidence **Inferred** in the proof pack |
| T2 | `LayoutCodec`, recent codec | **D2** with a fixed-seed generator (no FsCheck): byte round trip `Serialize(Parse(Serialize(x))) == Serialize(x)`; `Parse` never throws on random or mutated bytes; every parse satisfies §3.2; clamp idempotent. No shrinking is claimed; a failing seed becomes a named regression check |
| T3 | `Desktop.Shell`, Dock package | **D3**: `Architecture_DockConfinedToShell` scans `src/**/*.cs` text for `Dock.` namespaces outside `CfdWorkbench.Desktop.Shell` and `CfdWorkbench.Core` for `Avalonia`/`Dock`; red-first by a planted reference; restore in locked mode |
| T4 | `PreferenceStore` | **D4**: real files under a temp directory through the real `ProjectStore` (create-only, CAS, merge, never-write, stale claim, 0600/0700, symlink, concurrency) |
| T7 | `layout.json`, `recent.json` | **D6**: versioned synthetic golden fixtures (each preset, a float, a split group, every V-row, the three rollback variants); Postel: an unknown member is *deliberately refused* at its level except under the version peek (tested) |
| T8 | fake `IProjectStore` (slow / cancel), injected `IScreenSource` | **D7**: `StoreContract_CancelBeforePublish_DocCancelled` runs against **both** the fake and the real store; the injected screens are paired with S6 (runs on this Mac, gates M1.2e) and S3 (until run, clamp-on-removed-monitor is **Inferred** in the proof pack) |

No AI triggers.

### 12.2 Harness changes (G0) — making "named checks" falsifiable

- A Desktop `Check(name, action)` like the Core harness (`IdentityTests.cs`:62-66): prints `PASS <name>` /
  `FAIL <name> …`, continues, and the process exits nonzero if any failed.
- The default Desktop run **spawns** `--shell-model`, `--controller-shell`, `--shell-window` (and later
  `--shell-float`) as child processes like `--section-flow` (`WorkbenchTests.cs`:1864-1873) and **propagates their exit
  codes**.
- `tools/check-named-tests.py <track>` (G0): extracts the track's exact names **from this design** — every backticked
  test name in §9 or §12.4, attributed to the first `(<track>` (closed by `)`, `,` or `;`) after it on the same line — the single authority (no
  second list) — and fails
  unless each appears as `PASS` in `.tmp-tests/*.log`, no `FAIL` line exists, and **the list is not empty**. Names are
  exact, never globs; a `*` or `<…>` inside a backticked name fails the script. G0 lands the script with a red-first
  self-test (`--self-test`): a planted name with no track and a planted glob must each make it fail.

### 12.3 Tiers

1. **Pure** — `--shell-model` and Core-harness classes for Persistence.
2. **Harness windows** ("headless" in this repo's sense: `SetupWithoutStarting`, real macOS windows) — `--shell-window`
   and `--shell-float`. Rules: assert **logical focus** through `FocusManager`, not window activation; check window
   readiness first and fail with the code `ENV-NOT-READY` rather than a false result; screens come from an injected
   `IScreenSource`; at the join the window suites run **three times** and must give identical results.
3. **Native** (packaged `.app`, U1): per-window menus with a float key; ⌘Z in the Span field (menu key-equivalent path the
   harness cannot reach); VoiceOver; S6, S7 single-monitor rows, S8. Recorded in `docs/reviews/app-shell-native.md`
   (defect class CO-UI-READY: attach proof, not launch success).
4. **Token control (CD8)** — `ui-craft-gate.py` reads web source, not AXAML; the native rung-2 control is the existing
   `xaml-token-lint.py` step of `tools/verify-application-adapters.py` (`:632-633`), run on D3a and D4.

### 12.4 Ledgers

**UX-29 edges — owner per edge (28).**

| Edge | Owner | Test (track) |
|---|---|---|
| F11 C-Example-missing | this design | `Start_ExampleFixtureMissing_NamesFixtureNewFoilAvailable` (D3a) |
| F11 O-Cancel | this design | `Open_CancelDuringPrepare_CurrentFoilUnchanged` (D2), `Start_OpeningCancel_FocusReturnsToCard` (D3a) |
| F11 O-Newer | this design | `OpenFailure_Newer_Classified` (D2), `Start_OpenNewer_AlertOpenAnother` (D3a) |
| F11 O-Missing-migratable-unknown | this design | `OpenFailure_Missing_Classified`, `OpenFailure_UnknownContent_Classified` (D2); `Start_OpenMissing_AlertLocate` (D3a); migratable: no instance in M1 (recorded) |
| F11 O4-Not-resolved | this design | `Start_OpenFailedDismissed_StartKept` (D3a) |
| F11 D1-No | this design | `ApplySpan_NotANumber_Refused`, `ApplySpan_NonPositive_Refused`, `ApplySpan_EdgesCross_Refused` (D2), CAD-16 span clauses (C1) |
| F11 D-Tip-closes | this design | `Properties_TipCloses_TipChordIsText` (D2) |
| F11 S1-Close-window | this design, after M1.2c | `Close_SectionDraftOpen_UnsavedChoiceSafeFocused` (D4) |
| F11 S10-Save | this design, after M1.2c | `Close_SaveWithSectionDraft_FinishOrCancelFirst` (D4) |
| F11 S1-Switch-to-Analysis | **open — spec owner**: unreachable in M1.2 (only CAD is built); a written N/A is requested | — |
| F11 P2-Yes | M1.2b design (not yet written) | — |
| F11 S2-Yes, SM-Cancel, S1-Other-station, S1-Escape, S1-Cancel, S1-Station-removed | M1.2c design (not yet written) | — |
| F11 K-No-match, K-Pending-or-cite-only, K-Cancel, K3-Cancel, V-Empty-or-duplicate, V-Cancel | M1.2d design (not yet written) | — |
| F12 A-Monitor-gone | this design | `LayoutRestore_ScreenGone_FloatClampedAndSaid` (D4) |
| F12 G-Yes | this design | `Relocation_FocusedControlUnderFloat_Moves`, `Relocation_CanvasVertexUnderFloat_Moves` (D4) |
| F12 G-No | this design | `Relocation_CanvasVertexUnderFloat_DocksBack` (D4) |
| F12 X-Escape-or-restore | this design | `Maximize_Escape_Restores` (D4) |
| F12 F-Escape-or-dock-back | this design | `Float_EscapeOnTab_DocksBackFocusToOriginTab` (D4) |

**The 39 v10 focus checks (`tools/check-mockup-v10.mjs`) → owner.**

| v10 check | Owner / test (track) |
|---|---|
| focusAfterBrowserEnter | `Focus_BrowserEnter_StaysOnRowSelectsStation` (D3a) |
| focusAfterCommitTab, focusAfterSpanCommit | `Focus_SpanCommitTab_NextField` (D3a) |
| focusAfterRootCommitTab | M1.2b (root chord input) |
| focusAfterSize | `Focus_SizeMenu_ReturnsToTab` (D3a) |
| focusAfterClose | `Focus_ClosePane_NextTab` (D3a) |
| focusAfterLastClose | `Focus_CloseLastPane_DockToggle` (D3a) |
| focusAfterShowProperties | `Focus_WindowPanesShow_PaneTab` (D3a) |
| focusWhileMaxed, focusAfterRestore | `Focus_Maximize_TabStaysInside`, `Maximize_Escape_Restores` (D4) |
| focusAfterFloat | `Focus_Float_IntoFloatTab` (D4) |
| focusAfterFloatMove | `Focus_FloatAltArrow_StaysOnTab` (D4) |
| focusAfterPosition | `Focus_FloatPositionMenu_ReturnsToTab` (D4) |
| focusAfterEscapeDock | `Float_EscapeOnTab_DocksBackFocusToOriginTab` (D4) |
| focusAfterMoveRight, focusAfterMoveLeft | `Focus_MoveTo_StaysOnMovedTab` (D3a) |
| focusAfterGridSelect, focusAfterGridEdit | M1.2c (Points pane) |
| focusAfterWorkspace | `Workspace_Switch_KeepsSelectionCameraDraftAndFocus` (D4; Planform-only in M1.2a) |
| focusAfterMenuTab | `Focus_MenuTab_ClosesMenuReturns` (D3a) |
| focusInSection, focusAfterEscSection, focusAfterEscapeWithEdits, focusAfterSectionTypeChange | M1.2c (section editor mode) |
| focusAfterInvalid, focusAfterInvalidField | `Focus_SpanInvalid_StaysInFieldWithAlert` (D3a) |
| focusAfterTypeChange | M1.2b (point types) |
| focusAfterUndo | `Focus_UndoFromCanvas_StaysOnCanvas` (D3a) |
| focusInCatalog, focusAfterReplace, focusAfterCatalogEscape, focusInSave, focusAfterSaveError, focusAfterSave | M1.2d (catalog dialogs) |
| focusWhileOpening | `Start_Opening_FocusOnCancel` (D3a) |
| focusAfterCancelOpen | `Start_OpeningCancel_FocusReturnsToCard` (D3a) |
| focusAfterOpen | `Start_Opened_FocusModelArea` (D3a) |
| focusAfterOpenError | `Start_OpenNewer_AlertOpenAnother` (D3a) |
| focusAfterNewFoil | `Start_NewFoil_FocusModelArea` (D3a) |

Also: `Focus_HideDockHoldingFocus_ToToggle` (D3a), `Focus_DockRerender_NeverWindowRoot` (D3a), `Palette_Keyboard_FiltersAndRuns` (D3a), `F6_IntoAndOutOfFloat` (D4).

### 12.5 Existing tests (harness migration) — gated

Before D3a changes any source, it commits `docs/proof/app-shell-test-inventory.md`: every `WorkbenchTests.cs`
assertion bound to a control the shell removes (the file has 213 `throw new` lines; several reach private fields by
reflection, e.g. `:359-362`), each row either **ported** (new test name; mutations may be batched, but every ported
row goes red in at least one recorded mutation run (Ruling 54)) or
**deleted** with its superseded clause (Appendix G). For D3a, `tools/check-named-tests.py` also reads the inventory's
ported-name column and fails if that column is empty while the inventory lists removed controls. No assertion is
dropped silently.

## 13. Decision requests and open items

| ID | Question | Default | Bounded change if overturned |
|---|---|---|---|
| DR-S1 | Add `Avalonia.Headless` 11.3.14 (test-only)? | **No**: the repo's harness mode with the §12.3 rules | a test reference + headless variant of the window suites; trigger: a CI runner with no window server |
| DR-S2 | B7 "shortcuts never act while a text field has focus": may modified app chords act after committing the field? | **Yes** (§6.4), with text-owned chords excluded | only the router's chord list |
| OI-S1 | UX-29 F11 S1-Switch-to-Analysis is unreachable in M1.2 | written N/A from the spec owner requested | a test row when Analysis exists |
| DR-2, DR-6, DR-7, DR-8, DR-9, DR-10, DR-11 | — | the shell does **not** depend on them (DR-2/9: chord commands; DR-6: release handler; DR-7: rail outside the Dock host; DR-8: library store; DR-10/11: Core) | none in the shell |

**Spikes:** **S8** (new, 1 h, this Mac) runs **before G0** and decides `groups`; **S6** and S7's single-monitor rows gate
M1.2e's merge, run from the packaged `.app`; **S1** gates freezing the float geometry for Windows (and records the
effective DPI awareness; no `app.manifest` exists); S2–S5 stay scheduled (ADR-0009). **Findings for owners:** ADR-0009
§2 (per-window menus) and §6 (`FrameSize` conversion) — deviations recorded, ADR flagged; architecture §10.2 omits the
recent-files store; the as-built open-cancel hazard; `privacy-review.md` "M1 has no durable recent-file list" is
superseded (updated in this change); spec A8.5 needs rows for the preference files; the Release Engineer owns signing
and notarization before any build leaves this machine (`tools/package-application.py`:47 `"signed": False`); a Windows
rights path for Clear Recent is a condition before Windows ships.

## 14. Build tracks (exclusive file ownership)

Harness allocation: Grok `grok-4.7` for Core and Persistence; Agy `gemini-3.8-flash-high` for Desktop/Avalonia; Claude
for UI judgement and glue. **Test command for every track: `tools/run-tests.sh` (exit 0), then
`python3 tools/check-named-tests.py <track>` (exit 0)** — the exact names for each track are the ones in §9 and §12.4
marked with that track; the script reads them from this file. Repair
loops capped at 2.

| Track | Slice | Owns (exclusive) | Depends on | Harness |
|---|---|---|---|---|
| **S8 spike** | e | `docs/proof/cad-first-spikes/dock-split/` (probe + output) | — | Claude (1 h, this Mac) |
| **G0 Glue** | a, e | `src/CfdWorkbench.Persistence/LayoutDocument.cs` (records + enums, §3.4 after S8, frozen); `Run()` lines in `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` + empty classes for C1/P1; the Desktop `Check` helper and the spawn lines in `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs`; `tools/check-named-tests.py` | S8 | Claude |
| **C1 Core estimates + Span** | a | `src/CfdWorkbench.Core/WingEstimates.cs`; `ApplyDimension` + `EditReference "dimension"` in `src/CfdWorkbench.Core/AuthoringSession.cs`; span patch in `src/CfdWorkbench.Core/FoilSource.cs`; `tests/CfdWorkbench.Core.Tests/WingEstimatesTests.cs`, `DimensionTests.cs` | G0 | Grok `grok-4.7` |
| **P1 Preferences** | e (codec from a) | `src/CfdWorkbench.Persistence/LayoutCodec.cs`, `RecentList.cs`, `PreferenceStore.cs`; `tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs`, `PreferenceStoreTests.cs`, `tests/CfdWorkbench.Core.Tests/Fixtures/layout/**`, `tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj` | G0 | Grok `grok-4.7` |
| **D1 Shell model** | a, e | `src/CfdWorkbench.Desktop/Shell/WorkspacePresets.cs`, `FloatFrame.cs`, `FloatPlacement.cs`, `ScreenSource.cs`, `FocusRing.cs`, `CommandTable.cs`, `ShellEvents.cs`, `FocusTargetEvents.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellModelTests.cs` | G0 | Agy `gemini-3.8-flash-high` |
| **D2 Controller** | a | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `Selection.cs`, `PropertiesView.cs`, `OpenOutcome.cs`; `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs` | C1 | Agy `gemini-3.8-flash-high` |
| **D3a Shell host (M1.2a)** | a | `src/CfdWorkbench.Desktop/Shell/ShellHost.cs`, `ShellLayout.cs`, `NativeMenuBuilder.cs`, `EditVerbRouter.cs`; `App.axaml(.cs)`, `MainWindow.axaml(.cs)`, `Styles.axaml`, `CfdWorkbench.Desktop.csproj` + `packages.lock.json`, `Program.cs`; `Panes/StartView.axaml(.cs)`, `Panes/PropertiesPane.axaml(.cs)`, `Panes/BrowserPane.axaml(.cs)`, `ModelArea.axaml(.cs)`, `SectionEditorView.axaml(.cs)`; `FocusedTargetChanged` raising in `Viewport.cs` and `SectionCanvas.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`; `docs/proof/app-shell-test-inventory.md` and the ported/deleted rows of `WorkbenchTests.cs` | D1, D2 | Agy `gemini-3.8-flash-high` |
| **D4 Floats, workspaces, saved layouts (M1.2e)** | e | `Shell/ShellHost.cs`, `ShellLayout.cs` (handed over after D3a merges), `Shell/Maximize.cs`, `Shell/FocusSafeFloats.cs`, `Shell/PaneViewState.cs`; `tests/CfdWorkbench.Desktop.Tests/ShellFloatTests.cs` | P1, D3a, M1.2c merged | Agy `gemini-3.8-flash-high` |
| **U1 UI judgement** | a, e | `DESIGN.md` §7 COPY rows for §11's strings; `docs/reviews/app-shell-native.md` (VoiceOver, AX, packaged-`.app` ⌘Z-in-field and per-window menus, S6/S7) | D3a (a), D4 (e) | Claude |

U1's test command: `python3 tools/check-docs.py` exit 0 and `python3 docs/ai-forward-pack/scripts/design-lint.py --strict DESIGN.md` clean.
Order: S8 → G0 → {C1, P1, D1} → D2 → D3a → U1 (M1.2a) → [M1.2b–d] → D4 → U1 (M1.2e). Join: the coordinator runs both
commands on each branch, and for D3a and D4 runs `tools/run-tests.sh` **three times** and requires identical `PASS`
sets, with every named suite's PASS set non-empty (Ruling 54) (the window-tier stability rule of §12.3); `tools/check-spiral.py` applies. C1's red-first receipt fixture:
the commit and its run log are recorded in the proof pack.

## Adversarial analysis (STRIDE-lite)

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| Layout file (user-writable) | T: crafted JSON crashes or hangs launch | mitigate | store-bounded read, version peek, 64 KiB cap, depth 8, closed DTO, source-generated STJ, per-workspace fallback | `LayoutCodec_RandomBytes_NeverThrows`; mutated fixtures → presets |
| Layout file | E: type-name deserialization → code execution | mitigate | no polymorphism, no `$type`, no Dock serializer | fixture with `$type` → `LAYOUT-SCHEMA` |
| Layout file | T: a float placed off-screen or over the model to hide UI | mitigate | clamp; option (a) | off-screen and oversized floats → clamped |
| Layout file | I: disclosure of the user's work | mitigate | ids, sizes and screen bounds only (no display names); mode 0600; directory 0700 | codec refuses strings outside the closed sets; modes asserted |
| Preference paths | T: symlink redirects a write | mitigate | `ProjectStore` refuses symlinks and checks the parent identity | symlinked directory → session-only |
| Layout / recent files | D: huge file slows launch | mitigate | store cap and 64 KiB cap before parse | 65 KiB file → presets, never written if newer |
| Layout / recent files | S, R | accept | single-user OS-principal files (P11); preferences need no audit; residual: another process of the same user can rewrite them, as any user file | — |
| Recent file | T: an entry points to a hostile file | mitigate | opened only on explicit choice; full path in the accessible description; FoilDSL/native limits (SRC-09) apply | relative / over-long / wrong-extension entries dropped |
| Crash output | I: exception text with paths to stderr / crash reports | mitigate | handlers write type + code only | `Unhandled_Exception_StderrHasNoMarkerPath` |
| Dock dependency | T: a changed transitive package | mitigate | exact pins + `packages.lock.json` in locked mode; licence register (A8.5) | restore with a modified lock entry fails |
| Command routing | E: a document verb runs while typing | mitigate | Edit-verb router; text-owned chords; single keys inert in fields | ⌘Z / ⌥← in fields never change the document or a float |

## Privacy analysis (LINDDUN-lite)

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| Recent files: absolute paths (can carry the account name, client or project names) | I / L | mitigate | only in `recent/recent.json` (0600, directory 0700); never in the layout file, telemetry or stderr | ≤ 10 entries, oldest dropped; **File ▸ Open Recent ▸ Clear Menu** empties it (failure reported); deleting the file removes it |
| Recent files | U: unaware that recents are kept | mitigate | the standard Open Recent menu shows exactly what is kept, with Clear Menu | as above; **residual:** no age limit (count only); uninstall leaves the file; a Windows rights path is a condition before Windows ships |
| Telemetry, stderr | D: disclosure through events or crash output | mitigate | closed ids and counts only; crash handlers write type + code; marker-injection test across all rings and stderr | rings in memory, cleared on quit |
| Layout file: screen bounds | L: weak device fingerprint | accept | bounds only (display names dropped — minimization); local, never transmitted | until Reset layout rewrites it or the file is deleted |
| Open-failure alerts | D: path on screen | accept | local display within purpose; the alert shows the file name, the full path only in the accessible description | not stored |
| All preference files | N: non-compliance | accept | no transfer, no third party, local single user; no regulation expected unless export or sync is added | — |
| Selection, estimates, pane content | — | — | not persisted, not logged | — |

## Conformance notes

All T0 deterministic; no model call (architecture §7). P3/P5 typed validation; P7 state in the document store and in
preference files; P8 operation ids on `ApplyDimension`; P9 one typed command table; P10 metadata-only events; P11 OS
principal. C4 typed boundaries, C6 operation ids, C7 explicit fallback strings, C8 named patterns (`// Pattern: …`),
C10 bounded rings and lists.

## Flagged risks and residual unknowns

- Owned floats (front, Space, ⌘\`, Window menu, owner set before first show) — Not assessed until S7.
- `PointToScreen` units on Retina — S6; mixed DPI — S2.
- Dock tab template hosting our menu button — Inferred (§5.2), fallback defined.
- `NativeMenuItem.Command` honouring `CanExecute` — `assume:`, first red test in D3a.
- Split drops — S8 before G0.
- Dock 11.x frozen; a Dock defect needs an Avalonia 12 `/migrate`.
- Screen-reader behaviour of Dock peers — Inferred (VoiceOver in U1, NVDA in S5).
- A stale claim needs a manual removal (message + Show in Finder); logout is cancelled, not deferred, while a close
  choice is open.
- 14 UX-29 edges await the M1.2b–d designs; one awaits the spec owner.

## Status & next action

| | |
|---|---|
| **Completed** | App-shell design for M1.2a (shell parts) and M1.2e: layout and recent schemas with version-first rollback, selection, contracts, patterns, open state machine, focus contract and ledgers, failure modes, STRIDE/LINDDUN, telemetry, test plan, tracks |
| **Remaining** | Designs for M1.2b, M1.2c, M1.2d; spikes S1–S8; spec-owner N/A for S1-Switch-to-Analysis |
| **Best next action** | Run S8 (1 h), land G0, then C1, P1 and D1 in parallel under `/implement` |

## Gate record

`GATE design · 2026-09-27 · patterns-expert, the-simplifier, test-architect, ux-accessibility, native-desktop-developer,
data-persistence-architect, privacy-data-governance · criteria met: data model first, E7, contracts spiked, patterns
named past both Patterns Expert and Simplifier, FMEA, STRIDE, LINDDUN, UI states and copy, telemetry, triggered
directives, tracks with exact test names · verdict: PASS-WITH-CONDITIONS · vetoes → resolution: Test Architect (hard)
cleared on cycle 2, Data & Persistence (hard) cleared on cycle 1, UX & Accessibility (hard, threatened) cleared on
cycle 1, Privacy cleared on cycle 1, Simplifier (soft) cleared on cycle 1 by written rationale`

Author: this `/design-slice` run. Every lens ran as a separate agent in Adversary Mode; each veto was cleared by its own
lens on re-review, never by the author. Repair cycles used: **2 of 2** (cycle 2 only for the Test Architect's new R1).

| Lens | Round 1 | Main findings | Repair | Final |
|---|---|---|---|---|
| Test Architect (hard veto) | **BLOCK** — 3 Blockers | named checks unfalsifiable (Desktop harness prints no PASS lines; globs; sub-suites not spawned); canvas option (a) unowned and untested; UX-29 edges without tests; unit-conversion, D7, harness-determinism, ledger and migration gaps | §12.2 harness, script, spawns; routed event + owners; 28-edge and 39-check ledgers; `FloatFrame`; D7 contract on fake and real store; §12.3 window-tier rules; §12.5 gated inventory. Cycle 2: 14 exact C1 names, 7 OpenFailure names, non-empty-list rule, 3× join runs, D3a raise tests | **Cleared** (cycle 2). Conditions: inventory ported-name column read by the script (applied); script self-test (applied); OI-S1 and the 15 M1.2b–d edges before the M1.2e merge |
| Data & Persistence (hard veto) | **BLOCK** — 1 Blocker | strict reader would call a v2 file corrupt and overwrite it; one claim per directory freezes both files; no recovery action; recent-list conflicts; receipt draft id | version-first `Peek` (V3 wins) + three red-first rollback fixtures; per-file subdirectories; one semaphore; refined CAS steps with rationale; recent re-apply; receipt `draftId` = operation id, red-first fixture; V10 | **Cleared** (cycle 1). Minors a–e applied; condition: rollback fixtures run red then green before M1.2e merges |
| UX & Accessibility (hard veto) | PASS-WITH-CONDITIONS (veto if unfixed) | Position menu missing; Alt+arrows and Escape in text fields; option (a) triggers; commands while maximized; open failure with a foil open; live regions; copy voice | all 11 rows and the copy applied | **Cleared** (cycle 1). Minors (Opening row, double speech, claim path in copy) applied |
| Privacy & Data Governance (veto) | PASS-WITH-CONDITIONS | unhandled-exception output; `privacy-review.md` contradiction; display name not minimized | crash handlers write type + code only; privacy review updated in this change; display name dropped | **Cleared** (cycle 1) |
| Simplifier (soft veto) | **SOFT VETO** | merge-retry unjustified; claim collision; S8 after G0; `via`; `DimensionInput`; interface; rail column | S8 before G0; semaphore; cuts applied; merge-retry kept with the Data & Persistence rationale; shared view-state object; single name source | **Cleared** (cycle 1) |
| Patterns Expert (advisory) | PASS-WITH-CONDITIONS | open state machine unspecified; lost update in coalescing; menus rebuilt on focus; notify order; four mis-named patterns | `OpenOutcome` + transition table; union of changed workspaces; `ICommand.CanExecuteChanged`; consistent-state-before-notify; precise names (§6.1) | Folded in; the merge-retry the Patterns Expert would concede stays on the Data & Persistence ruling |
| Native Desktop (advisory) | PASS-WITH-CONDITIONS | Application NativeMenu fills only the app menu (menus vanish with a float key); duplicated Hide/Quit; ⌘Q skips the layout save; bindings per window; `Screen.Scaling` = 1 on macOS | per-window menus (ADR-0009 deviation, flagged); table omits Avalonia's items and Settings; close/quit flow; bindings per gesture; own `DesktopScaling` | Folded in; signing/notarization handed to the Release Engineer |

Not convened: Security & Identity (no identity, secret or network boundary; the file boundaries are in the STRIDE
table), Distributed Systems (no messaging; the two-copy CAS was reviewed by Data & Persistence), SRE (telemetry
reviewed by the Test Architect and Privacy lenses). This is a stated gap in the gate's composition, not a pass.

---
**Handoff:** → `/implement`.
