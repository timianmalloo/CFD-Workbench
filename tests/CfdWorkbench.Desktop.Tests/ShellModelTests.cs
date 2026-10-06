using Avalonia.Controls;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

// Track D1 owns this class and adds its named checks with DesktopChecks.Check (docs/design/app-shell.md §9, §12.4, §14).
public static class ShellModelTests
{
    public static void Run()
    {
        // Ruling 81, red-first: a planted slow frame (50 ms against 33) fails at a quiet load and prints READINESS-MISS, never
        // PASS, above the gate or with no load recorded; a fast frame passes at any load. Ring: fast. Cost: under 1 ms.
        DesktopChecks.Check("ReadinessGate_PlantedSlowFrame_FailsQuietMissesLoaded", () =>
        {
            static void Equal<T>(T expected, T actual, string what)
            {
                if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, got {actual}");
            }
            Equal(DesktopChecks.FrameVerdict.Fail, DesktopChecks.FrameBudgetVerdict(50, 33, 5), "slow frame at load 5");
            Equal(DesktopChecks.FrameVerdict.Fail, DesktopChecks.FrameBudgetVerdict(50, 33, 24), "slow frame at the gate");
            Equal(DesktopChecks.FrameVerdict.Miss, DesktopChecks.FrameBudgetVerdict(50, 33, 24.01), "slow frame just above the gate");
            Equal(DesktopChecks.FrameVerdict.Miss, DesktopChecks.FrameBudgetVerdict(50, 33, null), "slow frame, load not recorded");
            Equal(DesktopChecks.FrameVerdict.Pass, DesktopChecks.FrameBudgetVerdict(20, 33, 90), "fast frame at load 90");
            Equal(24.5, DesktopChecks.LoadDuring(5, 24.5), "the larger load counts");
            Equal(true, DesktopChecks.LoadDuring(5, null) is null, "an unrecorded load stays unrecorded");
            string loaded = DesktopChecks.Outcome("X", () => DesktopChecks.RequireFrameBudget("X", 50, 33, 90));
            if (!loaded.StartsWith("READINESS-MISS X ", StringComparison.Ordinal) || loaded.Contains("PASS"))
                throw new Exception("a slow frame at load 90 did not end in READINESS-MISS: " + loaded);
            if (DesktopChecks.Outcome("X", () => DesktopChecks.RequireFrameBudget("X", 20, 33, 90)) != "PASS X")
                throw new Exception("a fast frame did not pass");
        });
        DesktopChecks.Check("CommandTable_PointRemove_GesturesPerOs", () =>
        {
            var rows = CommandTable.MenuFor("Edit");
            var verbs = rows.Where(row => row.Id is "point.add" or "point.remove" or "point.rebuild").ToArray();
            if (verbs.Select(row => row.Id).SequenceEqual(["point.add", "point.remove", "point.rebuild"]) is false)
                throw new Exception("Point verbs are absent or out of Edit menu order");
            if (verbs[1].Gesture != "⌫" || NativeMenuBuilder.ParseGesture(verbs[1].Gesture) is not null)
                throw new Exception("Remove point must display Backspace without binding it in text fields");
            if (CommandTable.Bindings().Any(row => row.Id == "point.remove"))
                throw new Exception("Unmodified Backspace was bound at window scope");
        });

        DesktopChecks.Check("Preset_EveryPaneSubset_ExactlyOnePlacement", () =>
        {
            var registered = WorkspacePresets.RegisteredPanes;
            var allWorkspaces = new[] { WorkspaceId.Planform, WorkspaceId.Precision, WorkspaceId.Review };

            int totalSubsets = 1 << registered.Count;
            for (int mask = 0; mask < totalSubsets; mask++)
            {
                var subset = new List<string>();
                for (int i = 0; i < registered.Count; i++)
                {
                    if ((mask & (1 << i)) != 0) subset.Add(registered[i]);
                }

                foreach (var ws in allWorkspaces)
                {
                    var doc = WorkspacePresets.Preset(ws, subset);
                    if (doc.Active != ws) throw new Exception($"Active workspace expected {ws}, got {doc.Active}");
                    var layout = doc.Workspaces.FirstOrDefault(w => w.Id == ws)
                        ?? throw new Exception($"Workspace {ws} not found in doc");

                    var counts = registered.ToDictionary(p => p, _ => 0);
                    foreach (var region in layout.Regions)
                    {
                        foreach (var group in region.Groups)
                        {
                            foreach (var pane in group.Panes)
                            {
                                if (!counts.ContainsKey(pane))
                                    throw new Exception($"Unknown pane '{pane}' placed in region {region.Id}");
                                counts[pane]++;
                            }
                        }
                    }
                    foreach (var fl in layout.Floats)
                    {
                        foreach (var pane in fl.Panes)
                        {
                            if (!counts.ContainsKey(pane))
                                throw new Exception($"Unknown pane '{pane}' placed in float");
                            counts[pane]++;
                        }
                    }
                    foreach (var closed in layout.Closed)
                    {
                        if (!counts.ContainsKey(closed))
                            throw new Exception($"Unknown pane '{closed}' in closed");
                        counts[closed]++;
                    }

                    foreach (var pane in registered)
                    {
                        int expected = subset.Contains(pane) ? 1 : 0;
                        int actual = counts[pane];
                        if (actual != expected)
                            throw new Exception($"Workspace {ws}, subset [{string.Join(",", subset)}]: pane '{pane}' placed {actual} times, expected {expected}");
                    }
                }
            }
        });

        DesktopChecks.Check("Presets_DesktopEqualsCodec_EveryWorkspace", () =>
        {
            // One table (LayoutCodec.Homes): the shell's presets are the codec's, byte for byte, in every workspace.
            var registered = WorkspacePresets.RegisteredPanes.ToHashSet(StringComparer.Ordinal);
            foreach (var workspace in new[] { WorkspaceId.Planform, WorkspaceId.Precision, WorkspaceId.Review })
            {
                var desktop = LayoutCodec.Serialize(WorkspacePresets.Preset(workspace));
                var codec = LayoutCodec.Serialize(LayoutCodec.Presets(workspace, registered));
                if (!desktop.AsSpan().SequenceEqual(codec)) throw new Exception($"{workspace}: desktop preset differs from the codec's");
            }
            if (registered.Contains("messages") || WorkspacePresets.ShownIn(WorkspaceId.Precision, "points") != RegionId.Right ||
                WorkspacePresets.ShownIn(WorkspaceId.Planform, "points") is not null || WorkspacePresets.ShownIn(WorkspaceId.Review, "points") is not null ||
                WorkspacePresets.ShownIn(WorkspaceId.Planform, "properties") != RegionId.Left)
                throw new Exception("pane homes: " + string.Join(", ", LayoutCodec.Homes.Select(home => $"{home.Pane}@{home.Region}")));
        });

        DesktopChecks.Check("Layout_SavedMessagesPane_DroppedWithCode", () =>
        {
            // A layout saved before M1.2c names the retired Messages pane (OD-2 A): it drops with LAYOUT-PANE, the rest stays.
            const string saved = """
            {"format":"cfdw-layout","version":1,"active":"precision","workspaces":[{"id":"precision",
             "views":{"arrangement":"plan-3d","single":"plan"},
             "regions":[
              {"id":"left","open":true,"size":260,"groups":[{"panes":["properties","browser"],"active":"properties","share":1}]},
              {"id":"bottom","open":true,"size":190,"groups":[{"panes":["points","messages"],"active":"messages","share":1}]},
              {"id":"right","open":false,"size":260,"groups":[]}],
             "floats":[],"closed":["messages"]}]}
            """;
            var parsed = LayoutCodec.Parse(System.Text.Encoding.UTF8.GetBytes(saved), WorkspacePresets.RegisteredPanes.ToHashSet(StringComparer.Ordinal));
            var workspace = parsed.Document.Workspaces.Single(item => item.Id == WorkspaceId.Precision);
            var panes = workspace.Regions.SelectMany(region => region.Groups).SelectMany(group => group.Panes).Concat(workspace.Closed).ToList();
            var bottom = workspace.Regions.Single(region => region.Id == RegionId.Bottom);
            if (!parsed.Codes.Contains("LAYOUT-PANE") || parsed.DroppedPanes < 2 || parsed.NeverWrite || panes.Contains("messages") ||
                panes.Count(pane => pane == "points") != 1 || bottom.Groups.Single().Active != "points" || parsed.Codes.Contains("LAYOUT-SCHEMA"))
                throw new Exception($"codes [{string.Join(",", parsed.Codes)}] dropped {parsed.DroppedPanes} panes [{string.Join(",", panes)}]");
        });

        DesktopChecks.Check("ProportionFor_Bounds", () =>
        {
            double p1 = WorkspacePresets.ProportionFor(260, 1040);
            if (Math.Abs(p1 - 0.25) > 1e-6) throw new Exception($"Expected 0.25, got {p1}");

            double pZero = WorkspacePresets.ProportionFor(0, 1000);
            if (Math.Abs(pZero - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for px=0, got {pZero}");
            double pNeg = WorkspacePresets.ProportionFor(-50, 1000);
            if (Math.Abs(pNeg - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for px=-50, got {pNeg}");

            double pMax = WorkspacePresets.ProportionFor(1000, 1000);
            if (Math.Abs(pMax - 1.0) > 1e-6) throw new Exception($"Expected 1.0 for px=1000, got {pMax}");
            double pOver = WorkspacePresets.ProportionFor(1500, 1000);
            if (Math.Abs(pOver - 1.0) > 1e-6) throw new Exception($"Expected 1.0 for px=1500, got {pOver}");

            double pZeroExt = WorkspacePresets.ProportionFor(260, 0);
            if (Math.Abs(pZeroExt - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for hostExtent=0, got {pZeroExt}");
            double pNegExt = WorkspacePresets.ProportionFor(260, -500);
            if (Math.Abs(pNegExt - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for negative extent, got {pNegExt}");

            double pNan = WorkspacePresets.ProportionFor(double.NaN, 1000);
            if (Math.Abs(pNan - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for NaN px, got {pNan}");
            double pInf = WorkspacePresets.ProportionFor(260, double.PositiveInfinity);
            if (Math.Abs(pInf - 0.0) > 1e-6) throw new Exception($"Expected 0.0 for inf extent, got {pInf}");
        });

        DesktopChecks.Check("FloatFrame_Scaling1_15_2_Exact", () =>
        {
            var f1 = FloatFrame.From(100, 200, 260, 520, 1.0);
            if (f1.X != 100 || f1.Y != 200 || f1.Width != 260 || f1.Height != 520)
                throw new Exception($"Scaling 1.0 mismatch: {f1}");

            var f15 = FloatFrame.From(100, 200, 260, 520, 1.5);
            if (f15.X != 100 || f15.Y != 200 || f15.Width != 390 || f15.Height != 780)
                throw new Exception($"Scaling 1.5 mismatch: {f15}");

            var f2 = FloatFrame.From(100, 200, 260, 520, 2.0);
            if (f2.X != 100 || f2.Y != 200 || f2.Width != 520 || f2.Height != 1040)
                throw new Exception($"Scaling 2.0 mismatch: {f2}");
        });

        DesktopChecks.Check("FloatRestoreSnapshot_NoDrift", () =>
        {
            double origW = 260.0;
            double origH = 520.0;
            double[] scalings = [1.0, 1.5, 2.0];

            foreach (var scaling in scalings)
            {
                double currentW = origW;
                double currentH = origH;

                for (int cycle = 0; cycle < 100; cycle++)
                {
                    var frame = FloatFrame.From(100, 200, currentW, currentH, scaling);
                    currentW = FloatFrame.ToDipWidth(frame.Width, scaling);
                    currentH = FloatFrame.ToDipHeight(frame.Height, scaling);
                }

                if (Math.Abs(currentW - origW) > 1e-6)
                    throw new Exception($"Scaling {scaling}: width drifted from {origW} to {currentW}");
                if (Math.Abs(currentH - origH) > 1e-6)
                    throw new Exception($"Scaling {scaling}: height drifted from {origH} to {currentH}");
            }
        });

        DesktopChecks.Check("Clamp_ScreenGone_FullyInsidePrimary", () =>
        {
            var primary = new ScreenArea(new PxRect(0, 0, 1920, 1080), new PxRect(0, 25, 1920, 1055), true);
            var screens = new[] { primary };

            var oldHint = new PxRect(1920, 0, 2560, 1440);
            var floatRect = new PxRect(2100, 200, 400, 500);

            var clamped = FloatPlacement.Clamp(floatRect, screens, oldHint);

            if (!primary.WorkingArea.Contains(clamped))
                throw new Exception($"Clamped {clamped} not inside primary working area {primary.WorkingArea}");

            var reclamped = FloatPlacement.Clamp(clamped, screens, oldHint);
            if (reclamped != clamped)
                throw new Exception($"Clamp not idempotent: {clamped} vs {reclamped}");
        });

        DesktopChecks.Check("Clear_NearestClearCorner_Chosen", () =>
        {
            var modelArea = new PxRect(100, 100, 1000, 800);
            var target = new PxRect(450, 350, 100, 100);
            var origin = new FloatOrigin(RegionId.Left, 0, 0);

            var floatRect = new PxRect(500, 200, 200, 200);
            var floatFrame = new FloatFrame("float-1", floatRect, origin, "properties");

            var relocations = FloatPlacement.Clear(target, [floatFrame], modelArea);
            if (relocations.Count != 1)
                throw new Exception($"Expected 1 relocation, got {relocations.Count}");

            var rel = relocations[0];
            if (rel.Kind != RelocationKind.Moved)
                throw new Exception($"Expected RelocationKind.Moved, got {rel.Kind}");
            if (rel.Corner != CornerPlacement.TopRight)
                throw new Exception($"Expected CornerPlacement.TopRight, got {rel.Corner}");

            if (rel.Rect.X != 892 || rel.Rect.Y != 108)
                throw new Exception($"Expected (892, 108), got ({rel.Rect.X}, {rel.Rect.Y})");
        });

        DesktopChecks.Check("Clear_TieBreak_FixedOrder", () =>
        {
            var modelArea = new PxRect(0, 0, 1000, 1000);
            var target = new PxRect(450, 450, 100, 100);
            var origin = new FloatOrigin(RegionId.Left, 0, 0);

            var floatRect = new PxRect(400, 400, 200, 200);
            var floatFrame = new FloatFrame("float-tie", floatRect, origin, "browser");

            var relocations = FloatPlacement.Clear(target, [floatFrame], modelArea);
            if (relocations.Count != 1)
                throw new Exception($"Expected 1 relocation, got {relocations.Count}");

            var rel = relocations[0];
            if (rel.Kind != RelocationKind.Moved)
                throw new Exception($"Expected RelocationKind.Moved, got {rel.Kind}");

            if (rel.Corner != CornerPlacement.TopRight)
                throw new Exception($"Tie-break must choose TopRight (first in fixed order), got {rel.Corner}");
        });

        DesktopChecks.Check("Clear_TwoFloats_NoOverlap", () =>
        {
            var modelArea = new PxRect(0, 0, 1000, 800);
            var target = new PxRect(450, 350, 100, 100);
            var origin = new FloatOrigin(RegionId.Left, 0, 0);

            var f1 = new FloatFrame("f1", new PxRect(420, 320, 200, 200), origin, "properties");
            var f2 = new FloatFrame("f2", new PxRect(440, 340, 200, 200), origin, "browser");

            var relocations = FloatPlacement.Clear(target, [f1, f2], modelArea);
            if (relocations.Count != 2)
                throw new Exception($"Expected 2 relocations, got {relocations.Count}");

            var r1 = relocations[0];
            var r2 = relocations[1];

            if (r1.Kind != RelocationKind.Moved || r2.Kind != RelocationKind.Moved)
                throw new Exception($"Both floats should move, got {r1.Kind}, {r2.Kind}");

            if (r1.Rect.Intersects(target))
                throw new Exception("Float 1 overlaps target");
            if (r2.Rect.Intersects(target))
                throw new Exception("Float 2 overlaps target");

            if (r1.Rect.Intersects(r2.Rect))
                throw new Exception($"Floats overlap each other: {r1.Rect} vs {r2.Rect}");
        });

        DesktopChecks.Check("Clear_NeverOverlapsTarget_Property", () =>
        {
            var modelArea = new PxRect(50, 50, 1200, 900);
            var origin = new FloatOrigin(RegionId.Left, 0, 0);

            for (int tx = 200; tx <= 900; tx += 200)
            {
                for (int ty = 200; ty <= 700; ty += 200)
                {
                    var target = new PxRect(tx, ty, 80, 80);
                    var f = new FloatFrame("f-prop", new PxRect(tx - 20, ty - 20, 240, 300), origin, "properties");

                    var rels = FloatPlacement.Clear(target, [f], modelArea);
                    foreach (var r in rels)
                    {
                        if (r.Kind == RelocationKind.Moved)
                        {
                            if (r.Rect.Intersects(target))
                                throw new Exception($"Property violation: moved float overlaps target at ({tx},{ty})");
                            if (!modelArea.Contains(r.Rect))
                                throw new Exception($"Property violation: moved float outside modelArea at ({tx},{ty})");
                        }
                    }
                }
            }
        });

        DesktopChecks.Check("Clear_FloatLargerThanModelArea_DocksBack", () =>
        {
            var modelArea = new PxRect(0, 0, 500, 500);
            var target = new PxRect(200, 200, 50, 50);
            var origin = new FloatOrigin(RegionId.Bottom, 1, 2);

            var largeFloat = new FloatFrame("large-float", new PxRect(100, 100, 600, 600), origin, "messages");

            var relocations = FloatPlacement.Clear(target, [largeFloat], modelArea);
            if (relocations.Count != 1)
                throw new Exception($"Expected 1 relocation, got {relocations.Count}");

            var rel = relocations[0];
            if (rel.Kind != RelocationKind.DockedBack)
                throw new Exception($"Expected RelocationKind.DockedBack, got {rel.Kind}");
            if (rel.Origin != origin)
                throw new Exception($"Expected origin {origin}, got {rel.Origin}");
        });

        DesktopChecks.Check("FocusRing_HiddenRegionsAndFloats_Order", () =>
        {
            var available = new[] { true, true, false, false, true, true };

            int r0 = FocusRing.NextRegionIndex(0, false, available);
            if (r0 != 1) throw new Exception($"From 0 expected 1, got {r0}");

            int r1 = FocusRing.NextRegionIndex(1, false, available);
            if (r1 != 4) throw new Exception($"From 1 expected 4, got {r1}");

            int r4 = FocusRing.NextRegionIndex(4, false, available);
            if (r4 != 5) throw new Exception($"From 4 expected 5, got {r4}");

            int r5 = FocusRing.NextRegionIndex(5, false, available);
            if (r5 != 0) throw new Exception($"From 5 expected 0 (wrap), got {r5}");

            int rev0 = FocusRing.NextRegionIndex(0, true, available);
            if (rev0 != 5) throw new Exception($"Reverse from 0 expected 5 (wrap), got {rev0}");

            int rev5 = FocusRing.NextRegionIndex(5, true, available);
            if (rev5 != 4) throw new Exception($"Reverse from 5 expected 4, got {rev5}");

            int rev4 = FocusRing.NextRegionIndex(4, true, available);
            if (rev4 != 1) throw new Exception($"Reverse from 4 expected 1, got {rev4}");

            int rev1 = FocusRing.NextRegionIndex(1, true, available);
            if (rev1 != 0) throw new Exception($"Reverse from 1 expected 0, got {rev1}");
        });

        DesktopChecks.Check("CommandTable_Parity_EveryRowInMenuPaletteKey", () =>
        {
            var rows = CommandTable.Rows;
            if (rows.Count == 0) throw new Exception("Command table has no rows");

            var palette = CommandTable.PaletteEntries();
            var paletteIds = palette.Select(p => p.Id).ToHashSet();
            var analysis = rows.Single(r => r.Id == "view.analysis");
            if (analysis.Menu != "View" || analysis.Gesture != "⇧⌘A" ||
                !paletteIds.Contains(analysis.Id) ||
                !CommandTable.MenuFor("View").Any(r => r.Id == analysis.Id))
                throw new Exception("View ▸ Analysis must have menu, palette, and shortcut parity");

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Menu))
                    throw new Exception($"Row {row.Id} has no Menu");

                if (!paletteIds.Contains(row.Id))
                    throw new Exception($"Row {row.Id} missing from PaletteEntries");

                // PVU adds a displayed, view-scoped Backspace route for Remove; other point commands have no default gesture.
                bool pointCommand = row.Id.StartsWith("point.", StringComparison.Ordinal);
                if (pointCommand && row.Id != "point.remove" && !string.IsNullOrWhiteSpace(row.Gesture))
                    throw new Exception($"Row {row.Id} has a default gesture");
                // DR-DEN-4: the Text size key route is ⌘= / ⌘− (the zoom rows), routed by focus; a second binding of the
                // same keys on Bigger / Smaller would make two menu items claim one gesture.
                bool textSize = row.Menu == CommandTable.TextSizeMenu;
                if (textSize && (!string.IsNullOrWhiteSpace(row.Gesture) ||
                                 rows.Single(r => r.Id == "view.zoom-in").Gesture != "⌘=" || rows.Single(r => r.Id == "view.zoom-out").Gesture != "⌘−"))
                    throw new Exception($"Row {row.Id} does not reach its key route through ⌘= / ⌘−");
                // M1.2b2 §5.2 names no key for a layout, a display mode or a camera other than Home = Iso: they are choices
                // reached from the menu bar and the palette (and the cube's faces in Tab order, V3D).
                bool choice = row.Menu is ViewCommands.ViewsMenu or ViewCommands.DisplayMenu or CommandTable.UnitsMenu ||
                              row.Menu == ViewCommands.CameraMenu && row.Id != "view.camera-iso";
                // M1.2c §5.2 names keys for section.edit (↩), section.finish (⌘↩) and section.delete-point (⌫) only; the
                // other section rows, Thickness ×2 and Window ▸ Points are "—": reached from the menus and the palette.
                choice |= row.Menu == CommandTable.SectionMenu && row.Id is not ("section.edit" or "section.finish" or "section.delete-point") ||
                          row.Id is "view.thickness-x2" or "window.points" or "window.layers";
                if (!pointCommand && !textSize && !choice && string.IsNullOrWhiteSpace(row.Gesture))
                    throw new Exception($"Row {row.Id} has no Gesture/key route");
            }

            var menus = rows.Select(r => r.Menu).Distinct();
            foreach (var m in menus)
            {
                var menuRows = CommandTable.MenuFor(m);
                foreach (var mr in menuRows)
                {
                    if (!rows.Any(r => r.Id == mr.Id))
                        throw new Exception($"Menu {m} contains item {mr.Id} not in CommandTable.Rows");
                }
            }
        });

        ViewAndChannelCommands();
    }

    // ---------------- M1.2b2 PNL: view menus, Browser channel groups, channel point commands (§5.2, §11.4) ----------------

    private static readonly string[] ViewRowIds =
    [
        "view.layout-plan3d", "view.layout-four", "view.layout-one", "view.display-shaded", "view.display-wireframe",
        "view.camera-top", "view.camera-front", "view.camera-side", "view.camera-iso", "view.camera-bottom", "view.camera-back",
        "view.camera-port", "view.pan-left", "view.pan-right", "view.pan-up", "view.pan-down", "view.fit-selection"
    ];

    private static void ViewAndChannelCommands()
    {
        DesktopChecks.Check("CommandTable_ViewRows_ExecuteOrDisabled", () =>
        {
            // UI-DEAD-CONTROL at the table: every §5.2 view row either runs and changes the state it names, or is disabled
            // with a reason — with no foil open, and with a foil and its mesh.
            var missing = ViewRowIds.Where(id => CommandTable.Rows.All(row => row.Id != id) || !ViewCommands.Handles(id)).ToList();
            if (missing.Count > 0) throw new InvalidOperationException("missing view rows: " + string.Join(", ", missing));
            var failures = new List<string>();
            using (var empty = new WorkbenchController())
                foreach (var id in ViewRowIds)
                    if (ViewCommands.CanRun(id, empty) || string.IsNullOrWhiteSpace(ViewCommands.DisabledReason(id, empty)))
                        failures.Add($"{id} with no foil: runs {ViewCommands.CanRun(id, empty)}, reason '{ViewCommands.DisabledReason(id, empty)}'");
            using var controller = PropertiesViewTests.Opened();
            controller.SurfaceWanted = true;
            PropertiesViewTests.Pump(controller.WhenSurfaceSettledAsync());
            var reports = new List<StatusReport>();
            var context = new ViewCommandContext(controller, _ => new Avalonia.Size(400, 300), reports.Add);
            foreach (var id in ViewRowIds)
            {
                Disturb(controller, id);
                string before = Fingerprint(controller);
                if (!ViewCommands.CanRun(id, controller)) { failures.Add($"{id} disabled: {ViewCommands.DisabledReason(id, controller)}"); continue; }
                ViewCommands.Run(id, context);
                if (Fingerprint(controller) == before) failures.Add($"{id} changed nothing");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });
    }

    /// <summary>
    /// The PNL checks that need a realized shell window. The --shell-model suite runs headless without an Avalonia
    /// platform, so the windowed --properties-view suite runs these (it is the shorter of the windowed suites).
    /// </summary>
    public static void RunWindowed()
    {
        PropertiesViewTests.Pane("Zoom_FocusedThreeDView_ZoomsViewNotTextSize", (controller, host, window) =>
        {
            // DR-DEN-4: ⌘= / ⌘− zoom whichever model view has keyboard focus — found by its place in a model-area view
            // frame, so the 3D view and the elevations count without a type list — and change the Text size elsewhere.
            WaitMesh(controller, window);
            var view = host.ModelView.FindControl<View3d>("ThreeDView") ?? throw new InvalidOperationException("no 3D view");
            var face = view.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.IsEffectivelyVisible);
            double text = host.Properties.TextScale;
            var failures = new List<string>();
            foreach (var (name, target) in new (string, Control?)[] { ("the 3D view", view), ("a cube face", face) })
            {
                if (target is null || !target.Focus()) { failures.Add($"{name} cannot take focus"); continue; }
                PropertiesViewTests.Settle(window);
                double before = controller.Camera3d!.Value.Distance;
                PropertiesViewTests.Pump(host.RunCommand("view.zoom-in"));
                PropertiesViewTests.Settle(window);
                if (!(controller.Camera3d!.Value.Distance < before) || host.Properties.TextScale != text)
                    failures.Add($"{name} focused: distance {before} → {controller.Camera3d!.Value.Distance}, text {text} → {host.Properties.TextScale}");
            }
            double distance;
            PropertiesViewTests.Need<TextBox>(host.Properties, "SpanInput").Focus();
            PropertiesViewTests.Settle(window);
            distance = controller.Camera3d!.Value.Distance;
            PropertiesViewTests.Pump(host.RunCommand("view.zoom-in"));
            PropertiesViewTests.Settle(window);
            if (controller.Camera3d!.Value.Distance != distance || !(host.Properties.TextScale > text))
                failures.Add($"field focused: distance {distance} → {controller.Camera3d!.Value.Distance}, text {text} → {host.Properties.TextScale}");
            PropertiesViewTests.Pump(host.RunCommand("view.text-100"));
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Menu_ViewsLayouts_RadioCheckedMatchesLayout", (controller, host, window) =>
        {
            // §5.2 / §11.7: View ▸ Views holds Plan + 3D, Four views and One view as radio items; the checked item is the
            // layout shown, whichever control changed it (the menu, or Return / double-click on a view label).
            var menu = NativeMenuBuilder.BuildMenu(window);
            var views = Items(MenuEntry(menu, "View", ViewCommands.ViewsMenu));
            string Checked() => string.Join(",", views.Where(item => item.IsChecked).Select(item => item.Header));
            var failures = new List<string>();
            string titles = string.Join(",", views.Select(item => item.Header));
            if (titles != "Plan + 3D,Four views,One view" || views.Any(item => item.ToggleType != NativeMenuItemToggleType.Radio))
                failures.Add($"items {titles}");
            if (Checked() != "Plan + 3D") failures.Add("default checks " + Checked());
            foreach (var (title, want) in new[] { ("Four views", ViewLayout.Four), ("One view", ViewLayout.One(SingleView.Plan)), ("Plan + 3D", ViewLayout.Plan3d) })
            {
                Click(views.Single(item => Equals(item.Header, title)), window);
                if (controller.Layout != want || Checked() != title) failures.Add($"{title}: layout {controller.Layout}, checked {Checked()}");
            }
            controller.ToggleOneView(SingleView.ThreeD);   // a view label's path
            PropertiesViewTests.Settle(window);
            if (Checked() != "One view") failures.Add("after a view label: " + Checked());
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Menu_Display_AppliesToTargetView", (controller, host, window) =>
        {
            // §5.2: Display ▸ Shaded / Wireframe are radio items for the target view (the view label clicked last); with the
            // Plan as the target they set the 3D view, the one mesh view beside it. The strip says which view changed.
            WaitMesh(controller, window);
            var menu = NativeMenuBuilder.BuildMenu(window);
            var display = Items(MenuEntry(menu, "View", ViewCommands.DisplayMenu));
            string Checked() => string.Join(",", display.Where(item => item.IsChecked).Select(item => item.Header));
            var failures = new List<string>();
            if (display.Any(item => item.ToggleType != NativeMenuItemToggleType.Radio)) failures.Add("not radio items");
            controller.TargetView = SingleView.ThreeD;
            PropertiesViewTests.Settle(window);
            Click(display.Single(item => Equals(item.Header, "Wireframe")), window);
            string strip = PropertiesViewTests.Status(host).Text ?? "";
            if (controller.DisplayFor(SingleView.ThreeD) != DisplayMode.Wireframe || controller.DisplayFor(SingleView.Side) != DisplayMode.Shaded ||
                Checked() != "Wireframe" || strip != "3D view: wireframe.")
                failures.Add($"3D: {controller.DisplayFor(SingleView.ThreeD)} side {controller.DisplayFor(SingleView.Side)} checked {Checked()} strip '{strip}'");
            controller.Layout = ViewLayout.Four;
            controller.TargetView = SingleView.Side;
            PropertiesViewTests.Settle(window);
            if (Checked() != "Shaded") failures.Add("Side target checks " + Checked());
            Click(display.Single(item => Equals(item.Header, "Wireframe")), window);
            strip = PropertiesViewTests.Status(host).Text ?? "";
            if (controller.DisplayFor(SingleView.Side) != DisplayMode.Wireframe || controller.DisplayFor(SingleView.Front) != DisplayMode.Shaded ||
                strip != "Side view: wireframe.")
                failures.Add($"Side: {controller.DisplayFor(SingleView.Side)} front {controller.DisplayFor(SingleView.Front)} strip '{strip}'");
            controller.TargetView = SingleView.Plan;
            PropertiesViewTests.Settle(window);
            Click(display.Single(item => Equals(item.Header, "Shaded")), window);
            if (controller.DisplayFor(SingleView.ThreeD) != DisplayMode.Shaded || Checked() != "Shaded")
                failures.Add($"Plan target: 3D {controller.DisplayFor(SingleView.ThreeD)} checked {Checked()}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Menu_CameraPresets_ApplyTo3dView", (controller, host, window) =>
        {
            // §5.2 / §6.2: View ▸ Camera sets the 3D view's named camera (axis cameras orthographic, Iso perspective,
            // DR-VIEW-2), fitted to the whole foil; the view's title and the strip name it.
            WaitMesh(controller, window);
            var menu = NativeMenuBuilder.BuildMenu(window);
            var cameras = Items(MenuEntry(menu, "View", ViewCommands.CameraMenu));
            var failures = new List<string>();
            var size = host.ViewSize(SingleView.ThreeD);
            var surface = controller.Surface!;
            var corners = (from x in new[] { surface.MinimumX, surface.MaximumX } from y in new[] { -surface.MaximumY, surface.MaximumY }
                           from z in new[] { surface.MinimumZ, surface.MaximumZ } select new Point3(x, y, z)).ToArray();
            foreach (var (title, name) in new[] { ("Top", NamedCamera.Top), ("Front", NamedCamera.Front), ("Side", NamedCamera.Side),
                         ("Bottom", NamedCamera.Bottom), ("Back", NamedCamera.Back), ("Port", NamedCamera.Port), ("Iso", NamedCamera.Iso) })
            {
                Click(cameras.Single(item => Equals(item.Header, title)), window);
                var camera = controller.Camera3d;
                var label = host.ModelView.FindControl<Button>("ThreeDLabel")?.Content as string;
                string strip = PropertiesViewTests.Status(host).Text ?? "";
                var projection = name == NamedCamera.Iso ? Projection.Perspective : Projection.Orthographic;
                if (camera is not { } shown || shown.Name != name || shown.Projection != projection)
                    failures.Add($"{title}: camera {camera?.Name} {camera?.Projection}");
                else
                {
                    if (label != "3D · " + shown.Title || strip != $"3D view: {shown.Title}.") failures.Add($"{title}: label '{label}', strip '{strip}'");
                    var outside = corners.Select(corner => shown.Project(corner, size))
                        .Where(point => point.X < -0.5 || point.Y < -0.5 || point.X > size.Width + 0.5 || point.Y > size.Height + 0.5).ToList();
                    if (outside.Count > 0) failures.Add($"{title}: {outside.Count} corners outside the view");
                }
            }
            // V3D's View3d_CubeHiddenBelow240_FocusToViewMenuReachesPresets, the menu half: with the 3D view too narrow
            // for the cube, View ▸ Camera still reaches every preset (§11.5 overflow).
            window.Width = 860;
            PropertiesViewTests.Settle(window);
            var threeD = host.ModelView.FindControl<View3d>("ThreeDView")!;
            if (threeD.CubeVisible) failures.Add($"the cube still shows at {threeD.Bounds.Width:0} px");
            Click(cameras.Single(item => Equals(item.Header, "Back")), window);
            if (controller.Camera3d?.Name != NamedCamera.Back) failures.Add("cube hidden: Camera ▸ Back gave " + controller.Camera3d?.Name);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Menu_PanItems_PanTargetView", (controller, host, window) =>
        {
            // §6.2 / 2.5.7: View ▸ Pan moves the target view by 10 % of its size, one click, no drag — the model follows the
            // arrow (left moves it left), as ⌥-arrows do on the Plan.
            WaitMesh(controller, window);
            var menu = NativeMenuBuilder.BuildMenu(window);
            var pans = Items(MenuEntry(menu, "View", ViewCommands.PanMenu));
            var failures = new List<string>();
            controller.TargetView = SingleView.ThreeD;
            var size = host.ViewSize(SingleView.ThreeD);
            foreach (var (title, dx, dy) in new[] { ("Pan left", -1, 0), ("Pan right", 1, 0), ("Pan up", 0, -1), ("Pan down", 0, 1) })
            {
                var before = controller.Camera3d!.Value;
                Click(pans.Single(item => Equals(item.Header, title)), window);
                var moved = controller.Camera3d!.Value.Project(before.Target, size);
                double wantX = size.Width / 2 + dx * size.Width * 0.1, wantY = size.Height / 2 + dy * size.Height * 0.1;
                if (Math.Abs(moved.X - wantX) > 0.5 || Math.Abs(moved.Y - wantY) > 0.5)
                    failures.Add($"{title}: the centre moved to {moved}, want ({wantX:0.#}, {wantY:0.#})");
            }
            controller.TargetView = SingleView.Plan;
            var plan = controller.PlanCamera;
            Click(pans.Single(item => Equals(item.Header, "Pan left")), window);
            double wantSpan = plan.PanSpanPixels - host.ViewSize(SingleView.Plan).Width * 0.1;
            if (Math.Abs(controller.PlanCamera.PanSpanPixels - wantSpan) > 1e-9 || controller.PlanCamera.PanAftPixels != plan.PanAftPixels)
                failures.Add($"Plan: {controller.PlanCamera}, want span pan {wantSpan}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Menu_FitSelection_FitsSelectedStation", (controller, host, window) =>
        {
            // §6.2 / F-11: View ▸ Fit Selection (F) fits the target view to the selected station's section, or to the whole
            // foil when nothing is selected.
            WaitMesh(controller, window);
            var menu = NativeMenuBuilder.BuildMenu(window);
            var fit = MenuEntry(menu, "View", "Fit Selection");
            var size = host.ViewSize(SingleView.ThreeD);
            var failures = new List<string>();
            controller.TargetView = SingleView.ThreeD;
            var assignments = controller.CurrentProjection!.Assignments;
            controller.Select(new Selection.Station(assignments.Count - 1, assignments[^1].Eta));
            var disturbed = controller.Camera3d!.Value.Orbit(40, 10).Pan(60, -30, size);
            controller.Camera3d = disturbed;
            PropertiesViewTests.Settle(window);
            Click(fit, window);
            var section = controller.Surface!.Sections.Single(item => item.Eta == assignments[^1].Eta);
            var (minimum, maximum) = WorkbenchController.SectionBounds(section);
            // The fit is ViewCamera.Fit of the station's bounds (DR-VIEW-6: the projection fills the view, centred).
            var camera = controller.Camera3d!.Value;
            if (camera != disturbed.Fit(minimum, maximum, size)) failures.Add($"camera {camera}, want the station fit");
            var projected = new[] { minimum, maximum }.Select(point => camera.Project(point, size)).ToArray();
            double spread = Math.Max(Math.Abs(projected[0].X - projected[1].X), Math.Abs(projected[0].Y - projected[1].Y));
            if (spread < size.Width * 0.25 || projected.Any(point => point.X < 0 || point.Y < 0 || point.X > size.Width || point.Y > size.Height))
                failures.Add($"the station spans {spread:0} px of {size.Width:0} ({string.Join(" ", projected)})");
            controller.Select(new Selection.Foil());
            var stationFit = controller.Camera3d!.Value;
            Click(fit, window);
            var all = controller.FitBounds()!.Value;
            if (controller.Camera3d!.Value != stationFit.Fit(all.Minimum, all.Maximum, size))
                failures.Add($"nothing selected: camera {controller.Camera3d}, want the whole-foil fit");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("Browser_ChannelGroups_SelectPointOnElevation", (controller, host, window) =>
        {
            // §11.4 / 2.5.8: the Browser lists Dihedral, Twist and Thickness points (the equivalent path for close points);
            // picking a row selects that channel point everywhere, and Properties follows. The elevation half of this name
            // (the point drawn selected on Side or Front) waits for ELV's join.
            var failures = new List<string>();
            foreach (var (curve, list, header) in new[] { ("dihedral", "DihedralList", "Dihedral"), ("twist", "TwistList", "Twist"), ("thickness", "ThicknessList", "Thickness") })
            {
                var box = host.Browser.FindControl<ListBox>(list);
                var caption = host.Browser.FindControl<TextBlock>(header + "Header");
                var points = controller.CurveFor(curve)!.Points;
                if (box is null || caption?.Text != header || box.ItemCount != points.Count || string.IsNullOrEmpty(Avalonia.Automation.AutomationProperties.GetName(box)))
                {
                    failures.Add($"{curve}: list {box?.ItemCount} rows, caption '{caption?.Text}'");
                    continue;
                }
                box.SelectedIndex = 4;
                PropertiesViewTests.Settle(window);
                if (controller.Selection is not Selection.Points { Items: [var item] } || item.Curve != curve || item.VertexId != points[4].Id)
                    failures.Add($"{curve}: selection {controller.Selection}");
                string title = PropertiesViewTests.Text(host.Properties, "IdentityTitle");
                if (title != $"{header} · point 5 of 7") failures.Add($"{curve}: Properties '{title}'");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        PropertiesViewTests.Pane("ContextMenu_TwistPointMakeAnchor_SameEffectAsProperties", (controller, host, window) =>
        {
            // §11.3: Make Anchor Point from a point's context menu (the shell command every context menu runs) and Type ▸
            // Anchor point in Properties make the same accepted source. The lane point's own menu waits for ELV's join.
            var point = PropertiesViewTests.Channel(controller, "twist", 3);
            string original = controller.AcceptedSource;
            PropertiesViewTests.Select(controller, window, point);
            if (!host.CanRun("point.make-anchor")) throw new InvalidOperationException("Make anchor is disabled on a twist control point");
            PropertiesViewTests.Pump(host.RunCommand("point.make-anchor"));
            PropertiesViewTests.Settle(window);
            string fromMenu = controller.AcceptedSource;
            controller.Undo();
            PropertiesViewTests.Settle(window);
            if (controller.AcceptedSource != original || fromMenu == original) throw new InvalidOperationException("the menu path changed nothing, or Undo missed it");
            PropertiesViewTests.Select(controller, window, PropertiesViewTests.Reload(controller, point));
            var type = PropertiesViewTests.Need<ComboBox>(host.Properties, "TypeControl");
            type.IsDropDownOpen = true;
            type.SelectedIndex = 1;
            type.IsDropDownOpen = false;
            PropertiesViewTests.WaitIdle(controller, window);
            if (controller.AcceptedSource != fromMenu)
                throw new InvalidOperationException("Properties made a different source than the context menu");
        });

        PropertiesViewTests.Pane("UI_DEAD_CONTROL_ViewAndChannelControlsHaveActions", (controller, host, window) =>
        {
            // UI-DEAD-CONTROL: every new menu item runs and changes what it names, or is disabled with a reason; the Edit
            // menu's point items reach channel points; every Browser channel row has its context action.
            WaitMesh(controller, window);
            var menu = NativeMenuBuilder.BuildMenu(window);
            var failures = new List<string>();
            var items = new[] { ViewCommands.ViewsMenu, ViewCommands.DisplayMenu, ViewCommands.CameraMenu, ViewCommands.PanMenu }
                .SelectMany(sub => Items(MenuEntry(menu, "View", sub))).Append(MenuEntry(menu, "View", "Fit Selection")).ToList();
            foreach (var item in items)
            {
                string id = CommandTable.Rows.Single(row => row.Title == (string?)item.Header && ViewCommands.Handles(row.Id)).Id;
                controller.Layout = ViewLayout.Plan3d;
                Disturb(controller, id);
                PropertiesViewTests.Settle(window);
                string before = Fingerprint(controller);
                if (item.Command is null) { failures.Add($"{item.Header} has no command"); continue; }
                if (!item.Command.CanExecute(null))
                {
                    if (string.IsNullOrWhiteSpace(ViewCommands.DisabledReason(id, controller))) failures.Add($"{item.Header} disabled with no reason");
                    continue;
                }
                Click(item, window);
                if (Fingerprint(controller) == before) failures.Add($"{item.Header} changed nothing");
            }
            var anchor = PropertiesViewTests.AnchorOn(controller, "twist", 3, TangentKind.Smooth);
            PropertiesViewTests.Select(controller, window, anchor);
            var edit = MenuEntry(menu, "Edit", "Make control");
            int pointsBefore = controller.CurveFor("twist")!.Points.Count;
            if (MenuEntry(menu, "Edit", "Make anchor").Command!.CanExecute(null) || !edit.Command!.CanExecute(null))
                failures.Add("Edit ▸ point items do not follow a twist anchor");
            else
            {
                Click(edit, window);
                PropertiesViewTests.WaitIdle(controller, window);
                if (controller.CurveFor("twist")!.Points.Count >= pointsBefore) failures.Add("Edit ▸ Make control did nothing on a twist anchor");
            }
            foreach (var list in new[] { "DihedralList", "TwistList", "ThicknessList" })
            {
                var rows = host.Browser.FindControl<ListBox>(list)?.Items.OfType<ListBoxItem>().ToList() ?? [];
                if (rows.Count == 0 || rows.Any(row => row.ContextMenu?.Items.OfType<MenuItem>().Any(entry => Equals(entry.Header, "Make anchor")) != true))
                    failures.Add($"{list}: {rows.Count} rows, some without a context action");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });
    }

    /// <summary>Puts the controller where <paramref name="id"/> is not a no-op, so "changed nothing" means a dead control.</summary>
    private static void Disturb(WorkbenchController controller, string id)
    {
        if (!id.StartsWith("view.layout-", StringComparison.Ordinal)) controller.Layout = ViewLayout.Plan3d;
        switch (id)
        {
            case "view.layout-plan3d": controller.Layout = ViewLayout.Four; break;
            case "view.layout-four" or "view.layout-one": controller.Layout = ViewLayout.Plan3d; break;
            case "view.display-shaded": controller.SetDisplay(SingleView.ThreeD, DisplayMode.Wireframe); break;
            case "view.display-wireframe": controller.SetDisplay(SingleView.ThreeD, DisplayMode.Shaded); break;
            default:
                if (controller.Camera3d is { } camera) controller.Camera3d = camera.Orbit(17, 5);
                controller.PlanCamera = new PlanCamera(777, 13, 7);
                break;
        }
    }

    private static string Fingerprint(WorkbenchController controller) =>
        $"{controller.Layout}|{controller.DisplayFor(SingleView.ThreeD)}|{controller.DisplayFor(SingleView.Side)}|{controller.DisplayFor(SingleView.Front)}|" +
        $"{controller.Camera3d}|{controller.CameraFor(SingleView.Side)}|{controller.CameraFor(SingleView.Front)}|{controller.PlanCamera}";

    private static void WaitMesh(WorkbenchController controller, Avalonia.Controls.Window window)
    {
        PropertiesViewTests.Settle(window);
        PropertiesViewTests.Pump(controller.WhenSurfaceSettledAsync());
        PropertiesViewTests.Settle(window);
        if (controller.Surface is null || controller.Camera3d is null) throw new InvalidOperationException("no mesh or 3D camera");
    }

    private static Avalonia.Controls.NativeMenuItem MenuEntry(Avalonia.Controls.NativeMenu menu, params string[] path)
    {
        Avalonia.Controls.NativeMenuItem? item = null;
        var level = menu;
        foreach (var header in path)
        {
            item = level?.Items.OfType<Avalonia.Controls.NativeMenuItem>().FirstOrDefault(entry => Equals(entry.Header, header))
                ?? throw new InvalidOperationException("no menu item " + string.Join(" ▸ ", path));
            level = item.Menu;
        }
        return item!;
    }

    private static List<Avalonia.Controls.NativeMenuItem> Items(Avalonia.Controls.NativeMenuItem parent) =>
        parent.Menu?.Items.OfType<Avalonia.Controls.NativeMenuItem>().ToList() ?? throw new InvalidOperationException($"{parent.Header} has no submenu");

    private static void Click(Avalonia.Controls.NativeMenuItem item, Avalonia.Controls.Window window)
    {
        if (item.Command?.CanExecute(null) != true) throw new InvalidOperationException($"{item.Header} is disabled");
        item.Command.Execute(null);
        PropertiesViewTests.Settle(window);
    }
}
