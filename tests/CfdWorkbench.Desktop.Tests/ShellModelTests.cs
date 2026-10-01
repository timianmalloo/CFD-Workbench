using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

// Track D1 owns this class and adds its named checks with DesktopChecks.Check (docs/design/app-shell.md §9, §12.4, §14).
public static class ShellModelTests
{
    public static void Run()
    {
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

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Menu))
                    throw new Exception($"Row {row.Id} has no Menu");

                if (!paletteIds.Contains(row.Id))
                    throw new Exception($"Row {row.Id} missing from PaletteEntries");

                // Point commands have no default gesture (m12b-points.md §5.2). Every other row keeps a key route.
                bool pointCommand = row.Id.StartsWith("point.", StringComparison.Ordinal);
                if (pointCommand && !string.IsNullOrWhiteSpace(row.Gesture))
                    throw new Exception($"Row {row.Id} has a default gesture");
                if (!pointCommand && string.IsNullOrWhiteSpace(row.Gesture))
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
    }
}
