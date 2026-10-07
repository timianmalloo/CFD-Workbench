---
id: proof-wfx2-pc-reverify
title: "WFX2 - what the PC observes after the shared-code fixes"
type: proof-pack
status: active
owner: "@trk-wfx2"
phase: implementation
tags: [windows, wfx, proof]
links:
  - { to: proof-wfx2-red-first, rel: relates-to }
  - { to: proof-win-smoke-reverify, rel: relates-to }
summary: "Per fix: the Windows behaviour the Mac test simulates, and the one thing the PC should observe to confirm it."
review-by: "2027-01-07"
---

# WFX2 - the PC's re-check

The Mac proved each fix by forcing the Windows branch or the Windows default (`docs/proof/wfx2/red-first.md`). This is the part a
Mac cannot see. Record what is seen, including any step that still fails. The Windows store (W-2 B2, Rulings 136, 137) is
not touched here; save and every store-backed check keep failing until the PC lands it.

| # | Fix | Mac simulation | PC observes |
| --- | --- | --- | --- |
| 1 | Desktop crash frame | `StartupFailure.Describe(ex, detail: true)` returns the message and stack; the harness installs it, the product handler stays type-only | In `tools/run-tests.sh` output, the Desktop log now ends with `STAGE <name>` lines and, if it still dies, `APP-UNHANDLED APP-CRASH <type>` followed by the message and a stack frame. On the Mac the same crash is `Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` at the `workbench.SaveAsync` check, which is the store (W-2). Expect that line on Windows until W-2 lands. |
| 2 | JSON line endings | Structural scan: every indented JSON writer pins `NewLine = "\n"`; serialized images hold no `\r` | `Json_IndentedWriters_PinLfNewLine` PASS (Core). `LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9` no longer fails with `Expected -1; actual 1` if CRLF was its cause; if it still fails, the cause is something else (note the line). |
| 3 | Symlink privilege in tests | The Windows branch is forced with a fake runner and asserts `cmd.exe /c mklink /J <link> <target>` | `PrefStore_SymlinkedDirectory_SessionOnly` and `PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites` no longer throw `A required privilege is not held`. Either they pass (a junction was made) or the log shows `NOT ASSESSED directory-link: ...`. Other `CreateSymbolicLink` calls remain in `ProjectStoreTests.cs` (99, 101, 310) and `PropertiesCellsTests.cs` (941); they were outside this track. |
| 4 | Catalog refusal reason | Five planted mismatches each name their check (`coordinate-hash`, `generated-bytes`, `frame-le-shift`, ...) | In the Core log, each `Catalog_*` FAIL line now ends with `[check=...; detail=...]` naming the check and the first differing entry (expected vs actual hash or value, or the first differing byte). Send that line back; the equality policy is unchanged and waits for it. The Analysis harness prints `Exception.Message` only, so its `NeuralFoil_Family_CatalogNaca0012_DerivedNotLabelled` line will not show the detail; read it from the Core line. |
| 5a | F10 | A visible `Menu` in a plain window: F10 moves focus into it | Focus Evaluate, press F10: the File item is highlighted and focus is in the menu bar. Press F10 again: focus returns to Evaluate. |
| 5b | Escape focus | Right, Down, Escape from the menu returns focus to the origin | Focus Evaluate, Alt, Right, Down (Edit opens), Escape: the menu closes and focus is on Evaluate (the receipt saw Edit). One Escape leaves the whole menu bar. |
| 5c | Undo status text | Ctrl+Z on a `macOS: false` window: after sampling the strip reads `Undo selected the preceding accepted source revision.` | Drag a point, Ctrl+Z: after the geometry re-samples, the status strip still reads the Undo sentence (Ctrl+Shift+Z: the Redo sentence). Note that macOS did not show this text before either (see red-first); the text now persists on both. |
| 6 | Imperial spanwise table | `Update(points, Units.Imperial)` | Switch to Imperial (View, Units), open Analysis, Show table: the last header reads `L/span lbf/ft` and no cell reads N/m. |

Windows-specific risks the Mac cannot rule out: Avalonia's own Alt handling and the F10 handler both act on the same Menu (the F10
handler marks the key handled on key-down, so Avalonia should not also toggle it); if F10 opens and immediately closes the menu,
report it.
