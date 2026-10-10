---
id: design-view-preferences
title: "View preferences: the rail comb's scale and density survive a restart"
type: design
status: in-review
owner: "@timianmalloo"
phase: build, revision 2 (track PRF) - Rulings 205, 206, 207; built and proven (docs/proof/prf)
tags: [desktop, persistence, preferences, view-settings, rail-comb, display, ruling-205, trk-prf]
links:
  - { to: rulings, rel: implements }
  - { to: design-rail-comb, rel: refines }
  - { to: design-app-shell, rel: refines }
  - { to: design-windows-native-store, rel: relates-to }
review-by: 2027-04-01
summary: >-
  Ruling 205 asks for a small per-user preferences file for view settings, starting with the comb's scale and density. That
  file already exists: display/display.json (cfdw-display, version 1), written today for Text size and display units by
  PreferenceStore through ProjectStore. The design adds three optional members to it (combScale, combDensity, and, by Ruling 207,
  combVisible: the plan Curvature toggle) and reuses its location, atomic writer, claim, never-write rule, session-only rule and
  display.load / display.save telemetry (trigger comb, text-size, units). No new store, file, folder or user-facing text. Two
  fault classes, both never-write: a structure fault defaults the whole file, a value fault defaults only its member (Ruling 206).
  Nothing in the foil or project file changes.
---

# View preferences: the rail comb's scale and density

Status: built in phase 2 (Rulings 206 and 207 applied; sections 5, 8 and 10 record what shipped). Phase 1 text follows. Labels: **Verified** = opened in this session (file:line);
**Inferred** = reasoned, not observed; **Flagged** = needs a decision.

## 0. The finding that shapes the design

Ruling 205 says "add a small per-user preferences file ... reusing the app's existing data folder if there is one". Both
exist already.

- The data folder: `LocalApplicationData` + `"CFD Workbench"` (`src/CfdWorkbench.Desktop/App.axaml.cs:29-31`, Verified).
- The preferences file: `<root>/display/display.json`, format `cfdw-display`, version 1, members `format`, `version`,
  `textSize`, optional `units` (`src/CfdWorkbench.Persistence/PreferenceStore.cs`, the `DisplayPreferences` class; spec `app-shell.md` section 4.6).
  `PreferenceStore` is built once at start-up over `new ProjectStore()` (`App.axaml.cs:31-32`, Verified).

Text size and the display units are view settings, and they are held there now. The comb's scale and density are the third
and fourth. A second file would duplicate the folder, the claim, the writer, the recovery rules and the telemetry for two
values (YAGNI; solution-selection ladder rungs "reuse in codebase", "installed dep"). **Recommendation: extend
`display.json` additively. Do not add a file.** Flagged for the leader (question L1): this reads Ruling 205's "a small
per-user preferences file" as the file that already exists.

## 1. Data model

**Bounded context.** Desktop view state: how the user looks at the foil, not what the foil is. It has no link to the
foil, the project file, undo or the audit trail.

**Grain.** One row is exactly one installation user on one machine: one document per preference root (same as
`app-shell.md` section 4.6 "Grain"). It is not per project and not per window.

**Entity.** A value object (`ViewSettings`: a small immutable set of values compared by value). It has no identity, no
aggregate, no invariant between members, and no history. It is a snapshot of the user's choices, replaced as a whole.

**Fields (new in this design).**

| Member | JSON type | Domain | Default | Absent means | Source |
|---|---|---|---|---|---|
| `combScale` | number | exactly one of 0.5, 1, 2, 5, 10, 20, 50, 100, 200: the fixed gain "30 px = N per metre" | Auto | Auto | `rail-comb.md` section 3 (:78); `RailComb.Gains` (`RailComb.cs:17`, Verified) |
| `combDensity` | integer | exactly one of 16, 32, 64, 128 teeth per rail | 32 | 32 | `rail-comb.md` :79; `RailComb.Densities`, `DefaultDensity` (`RailComb.cs:18-19`, Verified) |

Rules for the fields:

- Auto is the absence of `combScale`, not a value. This is the existing controller state: `CombGain` is `null` for Auto
  (`WorkbenchController.cs:658`, Verified). The writer omits `combScale` for Auto and `combDensity` at 32, as it already
  omits `units` when metric (`PreferenceStore.cs:81`, Verified). The file stays small and a default is never written as a choice.
- The stored value is the gain per metre. The plate says "per metre" and `RailComb` has one ladder today (Verified);
  `rail-comb.md` :78 mentions a 1-2-5 ladder per inch in an inch project, which the code does not build
  (`RailComb.Gains` is a single static list, Verified). If an inch ladder is ever built, the file still holds the per-metre
  gain (derive, do not store a second definition of one quantity; DM rule). Flagged only as a note; no question.
- The ladders have one definition each (`RailComb.Gains`, `RailComb.Densities`), and the codec reads them from there, as
  `CommandTable.TextSizes` derives from `DisplayPreferences.TextSizes` (`app-shell.md` section 4.6, Verified). Persistence
  cannot reference Desktop, so the sets move to Core or Persistence (see section 9).

**The Curvature toggle.** `rail-comb.md` :72-73 says scale and density "persist for the session and across files in the
user's view preferences (the home of the Curvature toggle)". In code the toggle is `WorkbenchController.CombVisible`, default
`false`, session-only; the comment at `WorkbenchController.cs:645-646` says the settings are kept "beside the Curvature
toggle so they last the session and carry across files" (Verified). So "the home" is the controller, in memory; no
preferences file held it before this design.

**Decided (Ruling 207, operator): `combVisible` persists.** Boolean, default false, absent means off, written only when true.
If the plan comb was on at quit it is on at the next launch, with its plate. This is the plan comb only (key C, `WorkbenchController.CombVisible`),
not the section editor's curvature flag. The three comb values are set and saved together and compared as one triple.

**Other view settings today, listed, none added:**

| Setting | Where it lives now | Persisted? |
|---|---|---|
| Text size | `display.json` `textSize` | yes (DN-5) |
| Display units (metric / imperial) | `display.json` `units` | yes (Ruling 121) |
| Pane layout, per workspace | `layout/layout.json` | yes (`app-shell.md` section 4) |
| Recent files | `recent/recent.json` | yes |
| Plan Curvature toggle | `WorkbenchController.CombVisible` | no |
| Section editor Curvature toggle | `SectionCanvas.CurvatureVisible`, default **true** (`SectionCanvas.cs:25`), a separate flag from the Plan toggle | no |
| Layer visibility | `hiddenLayers` in the controller (`WorkbenchController.cs:265`) | no (Inferred: session-only; not opened further) |
| Plan zoom / camera | controller camera objects | no, and arguably not a preference; out of scope |

Proposal (P1, for the operator): after this slice, consider the two Curvature toggles and layer visibility as the next
candidates. None is built here.

**History rule.** None. The last value wins. Why: no past record depends on a past setting (nothing in the foil, the
undo stack or an export reads it), so a change does not rewrite the meaning of any earlier row. This is the same Type-1
decision recorded for Text size in `app-shell.md` section 4.6. Append-only history here would be a shadow schema for a value
nobody will ask the past of.

## 2. Location

- Folder: `Environment.GetFolderPath(SpecialFolder.LocalApplicationData) + "CFD Workbench"` (`App.axaml.cs:29-31`, Verified),
  then `display/display.json` (`PreferenceStore.cs:122-123`, Verified).
- macOS: `~/Library/Application Support/CFD Workbench/display/display.json`. Verified that .NET 10 on macOS resolves both
  `ApplicationData` and `LocalApplicationData` to `~/Library/Application Support`
  (`docs/proof/cad-first-spikes/shell-reflect/special-folders-output.txt:1-2`).
- Windows: `%LOCALAPPDATA%\CFD Workbench\display\display.json`. Inferred from the .NET mapping of `LocalApplicationData` on
  Windows (not run on this machine).
- Flagged (documentation drift, not a decision): `app-shell.md` section 4.5-4.6 writes `<ApplicationData>/CFD-Workbench/...`
  (hyphen, `ApplicationData`). The code uses a space and `LocalApplicationData`. The code is the authority; the build
  corrects the doc sentence.
- **Windows today.** `ProjectStore.Supported()` accepts only macOS on arm64 (`ProjectStore.cs:266-270`, Verified; "No
  path-only fallback"). On Windows every save returns `DOC-UNSUPPORTED-PERSISTENCE`, and `PreferenceStore` treats that as
  session-only (`LoadTextSizeCore`, `PreferenceStore.cs:464-`, Verified). So on Windows the comb settings, like Text size and units, last the
  session only until the Windows native store (`windows-native-store.md`, status in-review) lands. This is not a new
  gap, and no Windows-specific code is needed here (Flagged to the operator: O2, to be stated before they test on the PC).
- Owning project: **Persistence**. `PreferenceStore`, `DisplayPreferences`, `LayoutCodec` and `RecentList` live there;
  `ProjectStore` is the writer it calls. Core must not know about files. Desktop owns only the wiring (section 9).

## 3. Format and version

JSON, UTF-8, no BOM, LF, 2-space indent as written today (`PreferenceStore.cs:73-84`, Verified).

```json
{
  "format": "cfdw-display",
  "version": 1,
  "textSize": 150,
  "units": "imperial",
  "combScale": 5,
  "combDensity": 64
}
```

**Version rule.** `version` is bumped only when the meaning of an existing member changes or a member is removed. Adding an
optional member does not bump it. Precedent: `units` was added to version 1 (Ruling 121; the class comment at `PreferenceStore.cs:20-25`, Verified).

What each side does:

- New build reads an old file: the new members are absent, so scale is Auto and density 32. Nothing is rewritten until the
  user changes something.
- Old build reads a new file: `Parse` rejects an unknown member, so the old build reads 100 %, metric, `DISPLAY-SCHEMA`, and **never
  writes** the file this session. It never destroys the newer data. Ruling 207: no older build is in use, so no compatibility work and no
  tolerant reader (Ruling 206).
- A file whose `version` is above 1: see section 5.

## 4. Write policy

**Mechanism: reuse as is.** `PreferenceStore.SaveDisplayAsync` -> `Write` -> `ProjectStore.SaveAsync(path, SaveRequest(image,
expectedHash, operationId))` (`PreferenceStore.cs:405-424`, `:519-548`, Verified). That is: a claim file `.cfd-writer.claim`,
an exclusive temp file `.cfd-<guid>.tmp` in the same directory, `fsync` of the file, rename over the target, `fsync` of the
directory, and a compare of the on-disk SHA-256 against the hash the caller read (`DOC-CONFLICT` on mismatch)
(`ProjectStore.cs:103-153`, Verified; a first-ever save publishes by no-replace link, an overwrite by rename). On macOS the file mode is 0600 from the
`ProjectStore` `CreationMode 0x180`; `EnsureDir` sets only the directory to 0700 (Verified in the build; `PreferenceStore.cs` `EnsureDir`).

**Which path policy applies, and why.** The ProjectStore `ParentPath` policy (no symlinked folder anywhere on the path, opened
component by component; `ProjectStore.cs:333-360`, Verified), plus `PreferenceStore`'s own refusal of a linked root or
`display` folder (`Linked`, `PreferenceStore.cs:617-621`). Not the export policy of Ruling 198 (the chosen folder is followed
as chosen, only a symlink at the target name is refused). Reason: Ruling 198's export policy exists because the user picks
the folder in a save panel and may choose a Dropbox or OneDrive link on purpose. Here the app builds every path component
itself from a fixed folder, the user never chooses it, so there is no legitimate symlink to follow, and the stricter rule
costs nothing. This is the same reasoning Ruling 198 gives for Recovery. A linked root or folder means session-only, no
error dialog.

**When it writes.** On change, immediately, one save per change, no debounce, not on exit. Reasons: (1) a change is one click
of a stepper or one palette verb, never a drag, so there is no burst to coalesce; (2) the save is queued behind a gate and
queued saves write the latest wanted value (`SaveTextSizeAsync`, `PreferenceStore.cs:512-516`, Verified), so even a fast sequence cannot
reorder; (3) write-on-exit loses the setting on a crash and a forced quit, and a sleeping laptop is a normal way for this
app to end. What a crash loses: at most the one change in flight. The publish is a rename, so the file on disk is always the
whole old document or the whole new one, never a torn one (Verified by the writer's sequence above).

**No write back at load.** Applying the loaded values must not save and must not announce (section 9, same rule as
`ApplyLoadedTextSize`, Verified at `ShellHost.cs:923-` and the Units code at `ShellHost.cs:377-387`).

**Per-member merge (two choices, one file).** The writer holds `wanted` values and a `set` flag per member
(`PreferenceStore.cs:115`, Verified for `textSizeSet`, `unitsSet`). The design adds `combScaleWanted/Set` and
`combDensityWanted/Set`. A save writes the whole document: members this session set use the session's value; members it
never set keep what the file held at the last read. So changing the comb never resets Text size, and the reverse.

## 5. The hard states

| State | What happens | Mechanism | Outcome / code |
|---|---|---|---|
| **Missing** (no file, no folder) | Defaults: scale Auto, density 32. No folder is created at load. | `LoadTextSizeCore` `!File.Exists` branch (`:472`) | `absent`, no code |
| **Corrupt: bad JSON, BOM, over 4 KiB, wrong format, wrong or missing `version`, a repeated or unknown member** | Every setting reads its default, the file is **not** rewritten this session. | `Parse` -> `Unreadable()` (`:37-85`, `:87`) | `never-write`, `DISPLAY-SCHEMA` |
| **Corrupt: a known member has a wrong type or an out-of-set value** | **Changed by this design.** That member alone reads its default; every valid member keeps its value. The file is not rewritten this session. | per-member read in `Parse` (section 9) | `never-write`, `DISPLAY-SCHEMA` (the load still reports `restored` values for the valid members; see below) |
| **Newer version (above 1)** | Defaults for all members; the file is **never overwritten** this session. | `peek.Version is > CurrentVersion` (`:43`) | `never-write`, `LAYOUT-VERSION` |
| **Unwritable or unsupported folder** (read-only, no space, Windows today, a symlink in the path) | Settings apply for the session. No dialog. | `Session()` / `Failed()` | `session-only` or `failed` |
| **Two app instances** | The claim file serialises the writers; a hash conflict re-reads and retries once; the later change wins per member; a claim still held makes this instance session-only for the rest of the session. | `ResolveDisplay` (`:550-`), `Claim` | `saved` with `retried`, or `claim-held` |

How "keep the bad file for inspection" works: it is kept by never touching it. `displayNeverWrite` blocks every later save
this session (`PreferenceStore.cs:525`, Verified), so the bytes stay exactly as they were. No copy is made. The cost: until
the user deletes the file (or fixes it), no view setting persists, because every start finds it still bad. That is the rule
already shipped for Text size. **Alternative (not recommended, Flagged L2):** on the first bad read, copy the bytes once to
`display.json.bad` (link without replace, as `ProjectStore.WriteBackup` does, `ProjectStore.cs:233-260`) and then allow
the next save to replace the file. It self-heals but adds a second file, a sweep of stale copies, and a changed
contract for Text size; the single user can delete one file. Choose the simple rule.

**Newer version: why not read the known fields.** The brief asked for this. The design keeps the existing rule (defaults for
all, never overwrite) because `version` bumps only when an existing member's meaning changes (section 3). A newer file's
`textSize` or `combScale` may therefore not mean what this build thinks, and reading it could show a wrong value with no
signal. Defaults are honest; never-write protects the newer data. If the leader wants known-field reading, it is a ten-line
change in `Parse` plus one test, and the risk above is the price (Flagged L3).

**Per-member fallback: what it changed in shipped behaviour (built, Ruling 206 L4).** Before: any invalid member made the whole file
`Unreadable()`, so a bad `units` value reset Text size to 100 %. Now there are two classes, both never-write. A **structure** fault (not JSON,
BOM, over 4 KiB, wrong format, version other than 1, repeated or unknown member, missing `textSize`) defaults the whole file. A **value** fault
(a known member of the wrong type or outside its set) defaults only that member. "One rule for the set" is kept for `Serialize` only. The two
pinned tests were renamed and split so each name says its class: `PrefStore_TextSize_ValueError_ThatMemberDefaults_BytesUnchanged`,
`PrefStore_TextSize_StructureError_WholeFileDefaults_BytesUnchanged` and `Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged`.

**Load result.** `TextSizeLoad` keeps its shape. The comb values ride the same read (the parse the load keeps, as `units` did) and are
returned by `LoadCombViewAsync` as `CombViewLoad(double? Scale, int Density, bool Visible, outcome, codes, ...)`.

## 6. Copy

**No new user-facing text.** Silent fallback plus telemetry (section 7).

- A defaulted or session-only comb setting is not an error the user can act on, and the comb is a diagnostic aid.
- There is already approved wording for a failed Text-size save: "Text size will apply this session only: <reason>."
  (`ShellHost.cs:893-912`, Verified). It is specific to Text size. It is not extended to the comb. Units already treat a failed
  save as silent: "no wording is approved for it" (`ShellHost.cs:362`, Verified). The comb follows units.
- Draft only if the operator wants a notice (O4), marked **draft, not approved**: "Comb settings will apply this session
  only: <reason>." with `<reason>` from the existing `NotKeptReason` table. No other text is proposed.
- No existing string changes. The plate's strings and live-region sentences (`COPY-RC-2..4`, `COPY-RC-13`, `rail-comb.md`
  :221-233) are untouched, and the restored value reaches the screen only through them: no announcement at load.

## 7. Telemetry

Reuse the shipped events (built as designed, L5). They answer the same question for the same file.

- `display.load` is recorded once per start by `LoadTextSizeAsync` (`ShellHost.cs:880-887`, Verified). It already covers the
  comb members because they ride the same read. Attributes: outcome, codes, duration_ms. **No path** is recorded.
- `display.save` records every write: outcome, code, duration_ms, publication_known, durability_confirmed, retried
  (`ShellHost.cs:389-396`, Verified). The comb save sets `trigger = "comb"` (a `ShellEvent` field that exists,
  `ShellEvents.cs:13`, Verified), so the question "how often does the comb setting fail to save" is answerable apart
  from Text size.
- Ruling 206 L5: keep `display.*`. Every `display.save` row now carries `trigger`: `comb`, `text-size` or `units`, so each setting's
  failure rate is answerable apart. `app-shell.md` section 4.6 and the event table are updated.
- The brief cites the `ExportTelemetry` / `CatalogTelemetry` pattern in `AuthoringSession` (`AuthoringSession.cs:56-59`,
  Verified to exist). Those are session-bound records for export and catalog. Preferences are recorded through
  `ShellEvents`, not `AuthoringSession`, because they are Desktop-level and exist before any project (Verified; nothing in
  `PreferenceStore` references `AuthoringSession`). The pattern is followed in spirit (an outcome, a code, timing, no path).
- Update `app-shell.md` :701-702: "did the Text size restore" becomes "did the display settings restore".

## 8. Test list for phase 2 (red first)

**As built.** Core checks are in `tests/CfdWorkbench.Core.Tests/PreferenceStoreTests.cs` (names begin `PrefStore_Comb_` and
`PrefStore_DisplayCodec_`; item 11 is `..._Serialize_OutOfSetComb_Throws`). Desktop checks are in
`tests/CfdWorkbench.Desktop.Tests/PlanCombViewPrefsTests.cs` (names begin `Comb_Settings_`), in the `--plan-canvas` ring. They add
`combVisible` (the plan toggle restored on and off, the plate shown, the teeth compared by pixels against the first session and against the
default comb). Proof: `docs/proof/prf/red-first.md`.

Each runs red against the current code, then green. Rings: all are in the fast ring (no UI timing). Core tests use a temp
root as `PreferenceStoreTests` does; Desktop tests use the existing shell-restart pattern of `UnitsSwitchTests` (:84-94).
Cost: under 1 s each (file I/O on a temp folder; Inferred from the existing 20 or so store checks sharing one ring).

Core / Persistence (`tests/CfdWorkbench.Core.Tests/PreferenceStoreTests.cs`):

1. `PrefStore_Comb_Absent_AutoAnd32` - no file: `LoadCombViewAsync` gives (null, 32), outcome `absent`, no folder created.
2. `PrefStore_Comb_RoundTrip` - every ladder step and every density round-trips through a second `PreferenceStore`; Auto
   (null) round-trips and the written file has no `combScale` member; density 32 writes no `combDensity`.
3. `PrefStore_Comb_SaveDoesNotResetTextSizeOrUnits` and the reverse - the per-member merge.
4. `PrefStore_Comb_OutOfSetOrWrongType_ThatMemberDefaults_OthersKept_BytesUnchanged` - e.g. `combScale: 3`, `"5"`, `5.5`, `null`;
   `combDensity: 48`, `"64"`, `64.0`; the other members load; no save changes a byte.
5. `PrefStore_Comb_StructureCorrupt_AllDefaults_BytesUnchanged` - bad JSON, BOM, over 4 KiB, repeated member, unknown member.
6. `PrefStore_Comb_NewerVersion_Defaults_NeverOverwritten` - version 2 with comb members: defaults, `LAYOUT-VERSION`,
   later saves return `never-write`, SHA-256 of the file unchanged. (Extends `Rollback_TextSizeV2_BytesUnchanged`.)
7. `PrefStore_Comb_OldFileNewBuild_NoRewriteOnLoad` - a file with only `textSize` loads and its bytes are unchanged after load.
8. `PrefStore_Comb_Atomic_NoTornFile` - a fault hook between temp write and publish (the `StoreStage` hooks in `ProjectStore`
   exist, `ProjectStore.cs:433`) leaves the previous bytes intact and no `.cfd-*.tmp` in the folder after a clean retry.
9. `PrefStore_Comb_TwoInstances_LaterWinsPerMember_RetriedTrue` - the existing two-instance test, with one instance changing
   scale and the other density: both survive.
10. `PrefStore_Comb_ClaimHeld_SessionOnly_NoThrow` and `..._UnsupportedOrLinkedFolder_SessionOnly` - the existing session-only cases, with a comb save.
11. `DisplayPreferences_Serialize_OutOfSetComb_Throws` - one rule for the set, as `TextSizeSerializeOutOfSet`.

Desktop (`tests/CfdWorkbench.Desktop.Tests`):

12. `Comb_Settings_Persist_ChoiceSurvivesShellRestart` - step the scale and density, wait for the save, build a new shell over
    the same root, wait for load: the controller's `CombGain` and `CombDensity` equal the saved values; the plate's steppers
    and the status line read them (the rendered surface, not only the controller); no live-region sentence is written at load
    (`CombAnnouncements` unchanged).
13. `Comb_Settings_ChoiceBeforeLoadWins` - a stepper click before the startup read completes is not overwritten by the file.
14. `Comb_Settings_LimitPress_NoSave` - pressing a stepper at its limit (`CombChanged` fires from `Announce`, `WorkbenchController.cs:761-766`)
    does not write.
15. `Comb_Settings_FoilAndProjectBytesUnchanged` - open the example foil; save and reload comb settings; the foil file's SHA-256 is
    unchanged, undo depth is unchanged, and an export of the same foil is byte-identical to one made before.
16. `Comb_Settings_Telemetry_NoPath` - `display.save` rows carry `trigger = "comb"` and no field holds the folder text;
    `display.load` is recorded once.

Gates: `tools/check-event-subscribers.py` and the xaml token lint run if Desktop changes (join.json `checks`).
A new test or gate states its ring and cost (`AGENTS.md`): all above are fast-ring, each named with its cost in the build commit.

## 9. Build size and surface list

Estimate: 6 files touched, no new file in `src/`, about 130 lines of product code and about 220 lines of tests. One build
session in the track's usual size.

Surface list (E7):

| Layer | Surface | Change | Approx. lines |
|---|---|---|---|
| Store | `Persistence/PreferenceStore.cs` `DisplayPreferences` | `Parse`: two optional members and per-member value fallback; `Serialize`: write them when non-default; `DisplayParse` gains `CombScale`, `CombDensity` | 40 |
| Store | `PreferenceStore` | wanted/set fields, `LoadCombViewAsync`, `SaveCombViewAsync(double? scale, int density)`; `DisplayImage` passes them | 45 |
| Model | the two ladders (`RailComb.Gains`, `RailComb.Densities`) | one definition visible to Persistence: move to Core (a `CombView` constants class) or to `DisplayPreferences` with `RailComb` deriving, as `CommandTable.TextSizes` does. Decide in the build; the Persistence-owned option has the smaller diff | 10 |
| Service | `WorkbenchController` | `ApplyCombView(double? gain, int density)`: sets `CombGain`/`CombDensity`, no `Announce`, raises `CombChanged` once so canvas and plate redraw | 10 |
| Wiring | `Shell/ShellHost.cs` | load after construction (beside `LoadUnitsAsync`, `:349-350`); apply on the UI thread unless a stepper was already used (a `combChosen` flag, as `unitsChosen`); subscribe to `CombChanged` and save when the (gain, density) pair differs from the last applied or saved pair; `ShellEvents.Record("display.save", ..., trigger: "comb")` | 35 |
| UI | `CombPlate`, `PlanCanvas`, `RailComb` | none: they already read `CombGain` / `CombDensity` and redraw on `CombChanged` (`CombPlate.axaml.cs:160-170`, Verified) | 0 |
| Projection / wire / compute reader | foil, project file, export, Core | none; nothing reads view settings | 0 |
| Docs | `app-shell.md` section 4.6 and :701-702, `rail-comb.md` :72-73 | members, merge rule, per-member fallback, event wording, path wording | docs |
| Tests | section 8 | 11 Core checks, 5 Desktop checks | 220 |

If the operator adds `combVisible` (O1): one more member, one more `set` flag, `CombVisible` in the same pair compare. About
10 lines and one test.

A subscriber to the existing `CombChanged` is added in `ShellHost`; the existing event is already subscribed, so
`check-event-subscribers.py` is unaffected (no new `event` is declared). As built, the subscription is
not removed on disposal, like `UnitsChanged` (the host and the controller live and end together).

## 10. Decisions and open questions

**Resolved (phase 2).** L1 to L6 were ruled in Ruling 206 as recommended (L6: the ladders live in Persistence, `DisplayPreferences.CombScales`
and `CombDensities`; `RailComb` derives from them). O1 is decided by Ruling 207 (the plan comb's on/off persists). O2 stands (Windows is
session-only until the Windows native store lands). O3: no older build is in use. O4: silent fallback. P1 (other toggles, layer visibility) is not built.

For the leader and Fable (not for the operator):

- L1. Reading of Ruling 205: extend `display/display.json` (no new file). The ruling's "reusing the app's existing data
  folder if there is one" is satisfied, and the file exists too.
- L2. Corrupt file: keep "never touch it" (shipped) over "copy to `.bad` and heal".
- L3. Newer version: defaults for all, never overwrite (shipped), over "read the known fields" (the brief's wording).
- L4. Per-member fallback for value errors, a change to the shipped whole-file rule, which the build must reflect in two
  existing tests.
- L5. Telemetry names: keep `display.load` / `display.save` with `trigger = "comb"`, over the brief's `prefs.*`.
- L6. Where the ladders live (Core constants vs Persistence-owned): a build decision, small either way.

For the operator:

- O1. Should the Curvature toggle (`combVisible`) also persist? Recommendation: no. A restart that reopens the comb on, with
  its plate, is a surprise; the toggle is one keypress (`C`).
- O2. On Windows the settings last the session only, like Text size and units today, until the Windows native store lands.
  Accept?
- O3. Is any older packaged build in use? An older build ignores (and never writes) a file that has the new members.
- O4. Silent fallback (recommended), or the draft notice in section 6 (draft, not approved)?
- P1. Next candidates for the same file: the two Curvature toggles and layer visibility. Not built here.

## 11. Scope and non-goals

In: two optional members in `display.json` and their load, save and restore. Out: any new file, any foil or project-file
change, history of settings, a settings screen, sync between machines, Windows persistence (blocked by the store, not by
this design), and any new user-facing text.
