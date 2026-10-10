using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Track PNL (docs/design/m12c-section-editor.md §12.4): the Points pane, the section rows of Properties, the strip's
/// section reports and Show, the workspaces and the section commands, on the whole realized window
/// (UI-RENDERED-STATE). Run from the status-strip suite.
/// </summary>
public static class PointsPaneTests
{
    private const string Copy123 = "Upper and lower surfaces cross. Move the point back to finish.";
    private const string Copy124 = "Surfaces no longer cross. Finish is available.";

    // Readiness only: checks moved out of the fast ring (round-oct06 SPL, Ruling 123); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        Section("SectionCommands_EveryRow_RunsOrNamesReason", (controller, host, window) =>
        {
            var ids = CommandTable.Rows.Select(row => row.Id).Where(ShellHost.IsShellCommand).ToList();
            var failures = new List<string>();
            foreach (var id in ids)
            {
                string? reason = host.ShellCommandReason(id);
                if (reason is null != host.CanRun(id)) failures.Add($"{id}: CanRun disagrees with its reason");
                if (reason is { Length: 0 }) failures.Add($"{id}: empty reason");
            }
            // In the mode with a control point selected, every row runs or names why not; a run leaves a report.
            SelectControl(controller);
            Settle(window);
            // Modal rows (CommandRow.Modal, the one flag; Commands_ModalFlag_MatchesRowsThatShowADialog keeps it true) have their own
            // dialog checks; awaiting them here would wait for an operator choice. Finish and Cancel end the mode, so they run apart.
            var modal = CommandTable.Rows.Where(row => row.Modal).Select(row => row.Id).ToHashSet();
            foreach (var id in ids.Where(id => !modal.Contains(id) && id is not ("section.finish" or "section.cancel")))
            {
                string? reason = host.ShellCommandReason(id);
                string before = host.StatusStrip.Text;
                long generation = controller.Section?.Draft.Generation ?? -1;
                Pump(host.RunCommand(id));
                WaitAssessed(controller, window);
                if (reason is not null && host.StatusStrip.Text != reason) failures.Add($"{id}: refusal not reported ('{host.StatusStrip.Text}')");
                if (reason is null && host.StatusStrip.Text == before && (controller.Section?.Draft.Generation ?? -1) == generation)
                    failures.Add($"{id}: ran with no visible effect");
                if (controller.Section is null) Pump(controller.EnterSectionAsync(0, EntryOrigin.Palette));
                if (controller.Selection is not Selection.Points) SelectControl(controller);
                Settle(window);
            }
            // UI-DEAD-CONTROL in the section mode: every enabled button in the shell has an action.
            foreach (var button in host.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible && button.IsEnabled &&
                         button.Name?.StartsWith("PART_", StringComparison.Ordinal) != true))
            {
                if (button.Command is not null || button.Flyout is MenuFlyout { Items.Count: > 0 }) continue;
                var store = typeof(Interactive).GetField("_eventHandlers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .GetValue(button) as System.Collections.IDictionary;
                if (store?.Contains(Button.ClickEvent) != true) failures.Add("dead button " + (button.Name ?? button.Content?.ToString()));
            }
            Pump(host.RunCommand("section.cancel"));
            Settle(window);
            if (controller.Section is not null || !host.StatusStrip.Text.StartsWith("Cancelled. ", StringComparison.Ordinal))
                failures.Add($"section.cancel: '{host.StatusStrip.Text}'");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });
        Section("Properties_SectionPointTypedX_OneStepExact", (controller, host, window) =>
        {
            SelectUpper(controller, "cv-5");
            Settle(window);
            int steps = controller.Section!.Draft.StepCount;
            var (typed, expected) = Between(controller, "cv-5");
            var box = Need<TextBox>(host.Properties, "PointSpanInput");
            box.Focus();
            box.Text = typed;
            Key(box, Avalonia.Input.Key.Enter);
            Settle(window);
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-5");
            if (controller.Section!.Draft.StepCount != steps + 1 || point.SpanMeters != expected)
                throw new InvalidOperationException($"steps {steps} → {controller.Section.Draft.StepCount}; x {point.SpanMeters:R}");
        });
        Section("Properties_SectionPointTypedMmX_ConvertedAtStationChord", (controller, host, window) =>
        {
            SelectUpper(controller, "cv-5");
            Settle(window);
            double chord = Sections.Facts(controller.Section!.Draft.Bytes, controller.Section.Draft.Assignment).StationChordMeters;
            var (typed, _) = Between(controller, "cv-5");
            double millimetres = double.Parse(typed, System.Globalization.CultureInfo.InvariantCulture) / 100 * chord * 1000;
            string mm = millimetres.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            var box = Need<TextBox>(host.Properties, "PointSpanInput");
            box.Focus();
            box.Text = mm + " mm";
            Key(box, Avalonia.Input.Key.Enter);
            Settle(window);
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-5");
            double expected = double.Parse(mm, System.Globalization.CultureInfo.InvariantCulture) / 1000 / chord;
            string strip = host.StatusStrip.Text;
            if (point.SpanMeters != expected || !strip.Contains($"= {Quantity.Typed(expected * 100)} % chord", StringComparison.Ordinal))
                throw new InvalidOperationException($"x {point.SpanMeters:R} (expected {expected:R} at chord {chord}); strip '{strip}'");
        });
        Section("StatusStrip_CrossingCleared_Copy124RenderedOnce", (controller, host, window) =>
        {
            int cleared = 0;
            var text = StatusStripTests.Text(host);
            text.PropertyChanged += (_, change) =>
            {
                if (change.Property == TextBlock.TextProperty && change.NewValue as string == Copy124) cleared++;
            };
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            // A step with no crossing before it never says COPY-124.
            Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, point.Ordinate + 0.005)));
            WaitAssessed(controller, window);
            if (cleared != 0) throw new InvalidOperationException("COPY-124 shown without a crossing");
            Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, -0.3)));
            WaitAssessed(controller, window);
            Pump(controller.UndoSectionStepAsync());
            WaitAssessed(controller, window);
            if (cleared != 1 || host.StatusStrip.Text != Copy124)
                throw new InvalidOperationException($"COPY-124 rendered {cleared} times; strip '{host.StatusStrip.Text}'");
            Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, point.Ordinate + 0.006)));
            WaitAssessed(controller, window);
            if (cleared != 1) throw new InvalidOperationException($"COPY-124 rendered {cleared} times after a later step");
        });
    }

    public static void Run()
    {
        Capture();
        Pane("Workspace_Precision_ShowsPointsInHomeRegion", (controller, host, window) =>
        {
            host.ApplyWorkspace(WorkspaceId.Precision);
            Settle(window);
            var points = host.Points;
            var at = points.TranslatePoint(default, window) ?? throw new InvalidOperationException("Points pane not in the window");
            var model = host.ModelView.TranslatePoint(new Point(host.ModelView.Bounds.Width, 0), window)!.Value;
            if (!host.RightSidebarShown || !points.IsEffectivelyVisible || points.Bounds.Width <= 0 ||
                host.LayoutFactory.RightToolDock.ActiveDockable != host.LayoutFactory.PointsTool || at.X < model.X - 1 ||
                Need<StackPanel>(points, "PointsGroups").Children.Count != 5)
                throw new InvalidOperationException($"Points at x {at.X} (model right {model.X}), visible {points.IsEffectivelyVisible}, right bar {host.RightSidebarShown}");
            if (!host.Properties.IsEffectivelyVisible) throw new InvalidOperationException("Precision hid Properties (Planform plus Points)");
        });

        Pane("Workspace_Planform_HidesPoints", (controller, host, window) =>
        {
            host.ApplyWorkspace(WorkspaceId.Precision);
            Settle(window);
            host.ApplyWorkspace(WorkspaceId.Planform);
            Settle(window);
            // Hidden = out of the window (Dock detaches a removed tool), or not effectively visible in it.
            if (host.RightSidebarShown || host.Points.GetVisualRoot() is not null && host.Points.IsEffectivelyVisible || !host.Properties.IsEffectivelyVisible)
                throw new InvalidOperationException($"Planform: right bar {host.RightSidebarShown}, Points visible {host.Points.IsEffectivelyVisible}, attached {host.Points.GetVisualRoot() is not null}, parent {host.Points.GetVisualParent()?.GetType().Name}, Properties visible {host.Properties.IsEffectivelyVisible}");
            Pump(host.RunCommand("window.workspace-precision"));
            Settle(window);
            if (!host.Points.IsEffectivelyVisible) throw new InvalidOperationException("⌘2's row did not show the Points pane");
        });

        Section("PointsPane_SectionMode_ControlNetGroupsWithKind", (controller, host, window) =>
        {
            Pump(host.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, "cv-3", true)));
            Settle(window);
            var model = host.Points.Model ?? throw new InvalidOperationException("no Points model");
            var upper = controller.SectionCurve(SurfaceSide.Upper)!;
            var lower = controller.SectionCurve(SurfaceSide.Lower)!;
            int degree = upper.Knots.Count - upper.Points.Count - 1;
            var anchorRow = model.Groups[0].Rows.Single(row => row.Target.VertexId == "cv-3");
            int index = upper.Points.Single(point => point.Id == "cv-3").Index;
            var partner = model.Groups[1].Rows.Single(row => row.Point == (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (model.Heading != $"Control net · degree {degree}" || model.Note != PointsView.PairedNote ||
                !model.Columns.SequenceEqual(["Point", "Type", "x % c", "y % c", "Kind"]) ||
                model.Groups.Select(group => group.Title).SequenceEqual(["Upper surface", "Lower surface"]) is false ||
                model.Groups[0].Rows[0] is not { Point: "Nose", Kind: "Vertical" } ||
                model.Groups[0].Rows.Count != upper.Points.Count || model.Groups[1].Rows.Count != lower.Points.Count - 1 ||
                anchorRow.Type != "Anchor" || anchorRow.Kind is null || partner.Type != "Anchor" || partner.Kind is null)
                throw new InvalidOperationException($"control net model wrong: {model.Heading} | {anchorRow} | {partner}");
            // Rendered: the heading, the note and the anchor's Kind cell are drawn in the window.
            var texts = host.Points.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ToList();
            if (!texts.Contains(model.Heading) || !texts.Contains(PointsView.PairedNote) || !texts.Contains(anchorRow.Kind) ||
                !texts.Contains("Upper surface") || !texts.Contains("Lower surface"))
                throw new InvalidOperationException($"control net not rendered (kind '{anchorRow.Kind}'): " + string.Join(" | ", texts));
        });

        Section("PointsPane_RowClick_SelectsPointInCanvas", (controller, host, window) =>
        {
            var button = Need<Button>(host.Points, "PointButton_upper_cv-5");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            var upper = controller.SectionCurve(SurfaceSide.Upper)!;
            int index = upper.Points.Single(point => point.Id == "cv-5").Index;
            string item = Text(StatusStripTests.Strip(host), "SelectionItemText");
            if (controller.Selection is not Selection.Points { Items: [{ Curve: "upper", VertexId: "cv-5" }] } ||
                item != $"Upper · pt {index + 1} of {upper.Points.Count} ⇄ lower")
                throw new InvalidOperationException($"row click selected {controller.Selection}; strip item '{item}'");
            if (!Need<Border>(host.Points, "PointRow_upper_cv-5").Classes.Contains("on"))
                throw new InvalidOperationException("the clicked row is not marked selected");
        });

        Section("PointsPane_CanvasSelection_RowSelected", (controller, host, window) =>
        {
            // The canvas selects through the controller (it has no events of its own, §5.2); the pane follows.
            var lower = controller.SectionCurve(SurfaceSide.Lower)!.Points.Single(point => point.Id == "cv-5");
            controller.Select(new Selection.Points([new PointRef("lower", lower.Id, controller.Section!.Draft.Profile)]));
            Settle(window);
            var row = Need<Border>(host.Points, "PointRow_lower_cv-5");
            var partnerId = controller.SectionCurve(SurfaceSide.Upper)!.Points[lower.Index].Id;
            var partner = Need<Border>(host.Points, "PointRow_upper_" + partnerId);
            string content = Need<Button>(host.Points, "PointButton_lower_cv-5").Content as string ?? "";
            if (!row.Classes.Contains("on") || !partner.Classes.Contains("pair") || !content.StartsWith('⇄'))
                throw new InvalidOperationException($"row on {row.Classes.Contains("on")}, partner {partner.Classes.Contains("pair")}, '{content}'");
            if (!row.IsEffectivelyVisible || row.Bounds.Height < 23.5)
                throw new InvalidOperationException($"selected row not drawn at 24 px: {row.Bounds.Height}");
        });

        Section("PointsPane_TypedX_OneStep", (controller, host, window) =>
        {
            int steps = controller.Section!.Draft.StepCount;
            var (typed, expected) = Between(controller, "cv-5");
            var box = Need<TextBox>(host.Points, "PointX_upper_cv-5");
            box.Text = typed;
            Key(box, Avalonia.Input.Key.Enter);
            Pump(host.Points.Pending);
            Settle(window);
            var upper = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(point => point.Id == "cv-5");
            var lower = controller.SectionCurve(SurfaceSide.Lower)!.Points[upper.Index];
            if (controller.Section!.Draft.StepCount != steps + 1 || upper.SpanMeters != expected || lower.SpanMeters != expected)
                throw new InvalidOperationException($"steps {steps} → {controller.Section.Draft.StepCount}; x {upper.SpanMeters} / lower {lower.SpanMeters}");
        });

        Section("PointsPane_InvalidValue_FieldErrorNoStep", (controller, host, window) =>
        {
            var before = controller.Section!.Draft;
            var box = Need<TextBox>(host.Points, "PointX_upper_cv-5");
            box.Text = "abc";
            Key(box, Avalonia.Input.Key.Enter);
            Pump(host.Points.Pending);
            Settle(window);
            var error = Need<TextBlock>(host.Points, "PointError_upper_cv-5");
            if (controller.Section!.Draft.StepCount != before.StepCount || !error.IsEffectivelyVisible ||
                error.Text != PropertyCopy.NotANumber("x") || !Need<Border>(host.Points, "PointRow_upper_cv-5").Classes.Contains("error"))
                throw new InvalidOperationException($"invalid x: steps {controller.Section.Draft.StepCount}, error '{error.Text}' visible {error.IsEffectivelyVisible}");
            var y = Need<TextBox>(host.Points, "PointY_upper_cv-5");
            y.Text = "5 mm";
            Key(y, Avalonia.Input.Key.Enter);
            Pump(host.Points.Pending);
            Settle(window);
            error = Need<TextBlock>(host.Points, "PointError_upper_cv-5");
            if (controller.Section!.Draft.StepCount != before.StepCount || error.Text != SectionPoints.YRefusesMm ||
                !controller.Section.Draft.Bytes.AsSpan().SequenceEqual(before.Bytes))
                throw new InvalidOperationException($"y in mm made a step or no reason: '{error.Text}'");
        });

        Section("StatusStrip_ShowAction_FramesBlockingPoint", (controller, host, window) =>
        {
            (PointRef? Point, (double, double)? Range)? shown = null;
            host.SectionShowRequested += (point, range) => shown = (point, range);
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, -0.3)));
            WaitAssessed(controller, window);
            controller.Select(new Selection.Foil());
            Settle(window);
            var action = Need<Button>(StatusStripTests.Strip(host), "StatusTryAgainButton");
            if (host.StatusStrip.Text != Copy123 || !action.IsEffectivelyVisible || action.Content as string != "Show")
                throw new InvalidOperationException($"crossing strip '{host.StatusStrip.Text}', action '{action.Content}' visible {action.IsEffectivelyVisible}");
            action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            if (controller.Selection is not Selection.Points { Items: [{ Curve: "upper", VertexId: "cv-3" }] } ||
                shown is not { Point.VertexId: "cv-3", Range: not null })
                throw new InvalidOperationException($"Show selected {controller.Selection}; frame request {shown}");
        });

        Section("StatusStrip_SectionInsertReport_CountAndDeviation", (controller, host, window) =>
        {
            SelectUpper(controller, "cv-3");
            Settle(window);
            int before = controller.SectionCurve(SurfaceSide.Upper)!.Points.Count;
            Pump(host.RunCommand("section.insert-point"));
            WaitAssessed(controller, window);
            var upper = controller.SectionCurve(SurfaceSide.Upper)!;
            var lower = controller.SectionCurve(SurfaceSide.Lower)!;
            string strip = host.StatusStrip.Text;
            var report = controller.Section!.LastReport!;
            string counts = $"({before} → {upper.Points.Count} points each)";
            if (upper.Points.Count != before + 1 || lower.Points.Count != upper.Points.Count || !strip.Contains(counts, StringComparison.Ordinal) ||
                report.MaxChange > 5e-5 || !strip.EndsWith("Largest change 0.00 % chord.", StringComparison.Ordinal) ||
                !strip.StartsWith("Inserted a point at ", StringComparison.Ordinal))
                throw new InvalidOperationException($"insert report '{strip}' (max change {report.MaxChange:R}, {before} → {upper.Points.Count})");
        });

    }

    /// <summary>Properties' section rows (§11.4), run from the properties-view suite so the window checks split across two.</summary>
    public static void RunProperties()
    {
        // The operator's blank Properties (2026-10-04): the left side bar hidden and shown again (its toggle, or the Review
        // workspace and back) must put the one bound Properties pane back on screen. A detached pane still reports itself
        // visible and still renders its model, so the check reads the visual root and the on-screen panes, not IsVisible.
        foreach (var (route, hideAndShow) in new (string, Action<ShellHost>)[]
                 {
                     ("Toggle", host => { host.ToggleLeftSidebar(); host.ToggleLeftSidebar(); }),
                     ("ReviewWorkspace", host => { host.ApplyWorkspace(WorkspaceId.Review); host.ApplyWorkspace(WorkspaceId.Planform); })
                 })
            Pane("Properties_LeftSidebarHiddenAndShown_PaneOnScreen_" + route, (controller, host, window) =>
            {
                hideAndShow(host);
                Settle(window);
                var point = controller.Planform!.Leading.Points[2];
                controller.Select(new Selection.Points([Ref(point)]));
                Settle(window);
                var onScreen = window.GetVisualDescendants().OfType<PropertiesPane>().ToList();
                string title = $"Leading edge · point {point.Index + 1} of {controller.Planform.Leading.Points.Count}";
                bool titleShown = onScreen.SelectMany(pane => pane.GetVisualDescendants().OfType<TextBlock>())
                    .Any(text => text.Text == title && text.IsEffectivelyVisible);
                if (host.Properties.GetVisualRoot() is null || onScreen.Count != 1 || !ReferenceEquals(onScreen[0], host.Properties) || !titleShown)
                    throw new InvalidOperationException($"bound pane attached {host.Properties.GetVisualRoot() is not null}; panes on screen {onScreen.Count}; " +
                                                        $"'{title}' shown {titleShown}");
            });

        // The class, on the right side bar: Precision shows the Points pane, Planform removes its dock, Precision shows it again.
        Pane("Points_RightSidebarHiddenAndShown_PaneOnScreen", (controller, host, window) =>
        {
            foreach (var workspace in new[] { WorkspaceId.Precision, WorkspaceId.Planform, WorkspaceId.Precision })
            {
                host.ApplyWorkspace(workspace);
                Settle(window);
            }
            var onScreen = window.GetVisualDescendants().OfType<PointsPane>().ToList();
            if (host.Points.GetVisualRoot() is null || onScreen.Count != 1 || !ReferenceEquals(onScreen[0], host.Points))
                throw new InvalidOperationException($"bound Points pane attached {host.Points.GetVisualRoot() is not null}; Points panes on screen {onScreen.Count}");
        });

        // A render that throws (here: a status subscriber, reached through an estimate becoming unavailable) shows its
        // reason in the pane, never a blank pane, and its telemetry carries the exception's message, not only its type.
        Pane("Properties_RenderFailure_ShowsVisibleReason", (controller, host, window) =>
        {
            const string why = "render-failure probe";
            ShellEvents.Clear();
            host.Properties.Reported += _ => throw new InvalidOperationException(why);
            host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN });
            Settle(window);
            var error = Need<TextBlock>(host.Properties, "ErrorText");
            var failure = ShellEvents.Read().LastOrDefault(item => item is { Name: "shell.pane.render", Outcome: "error" });
            if (!error.IsEffectivelyVisible || error.Bounds.Height <= 0 || error.Text?.StartsWith("Properties couldn't be shown.", StringComparison.Ordinal) != true ||
                failure is not { ExceptionType: nameof(InvalidOperationException) } || failure.ExceptionMessage != why)
                throw new InvalidOperationException($"failure text visible {error.IsEffectivelyVisible} height {error.Bounds.Height} '{error.Text}'; event {failure}");
        });

        Section("Properties_SectionPoint_TypeXYRowsInPercentChord", (controller, host, window) =>
        {
            var point = SelectUpper(controller, "cv-3");
            Settle(window);
            var model = host.Properties.ShownModel ?? throw new InvalidOperationException("no Properties model");
            var rows = model.Groups.Single(group => group.Id == "pos").Rows;
            var upper = controller.SectionCurve(SurfaceSide.Upper)!;
            if (rows[0] is not { Label: "Type · both surfaces", Kind: RowKind.Choice } ||
                rows[1] is not { Label: "x · both surfaces", Unit: "% c", Kind: RowKind.Input } || rows[1].Value != Quantity.Typed(point.SpanMeters * 100) ||
                rows[2] is not { Label: "y", Unit: "% c", Kind: RowKind.Input } || rows[2].Value != Quantity.Typed(point.Ordinate * 100) ||
                model.Identity?.Title != $"Upper surface · point {point.Index + 1} of {upper.Points.Count}" ||
                model.Identity.Crumb != PropertiesView.PairLine("lower", point.Index + 1))
                throw new InvalidOperationException("section point rows: " + string.Join(" | ", rows.Select(row => $"{row.Label}={row.Value} {row.Unit}")) + $" · {model.Identity?.Title} · {model.Identity?.Crumb}");
            var labels = host.Properties.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ToList();
            if (!labels.Contains("x · both surfaces") || !labels.Contains("Type · both surfaces") || !labels.Contains("% c") ||
                !labels.Contains(PropertiesView.PairLine("lower", point.Index + 1)))
                throw new InvalidOperationException("section rows not rendered: " + string.Join(" | ", labels.Take(20)));
        });

        Pane("Properties_StationGroup_EndsWithEditSectionLink", (controller, host, window) =>
        {
            controller.Select(new Selection.Station(0, controller.CurrentProjection!.Assignments[0].Eta));
            Settle(window);
            var station = host.Properties.ShownModel!.Groups.Single(group => group.Id == "stn");
            if (station.Rows[^1] is not { Kind: RowKind.Action, Value: PropertiesView.EditSection })
                throw new InvalidOperationException("the Station group does not end with Edit section…");
            var link = Need<HyperlinkButton>(host.Properties, "Link_s_edit");
            if (!link.IsEffectivelyVisible || link.Content as string != PropertiesView.EditSection)
                throw new InvalidOperationException("the Edit section… link is not drawn");
            link.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var start = DateTime.UtcNow;
            while (controller.Section is null && DateTime.UtcNow - start < TimeSpan.FromSeconds(8)) Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            if (controller.Section is not { Origin: EntryOrigin.Properties, Draft.Assignment: 0 })
                throw new InvalidOperationException("the link did not open the section editor from Properties");
        });

        Section("Properties_SectionGroup_OwnTcAndPerStationTcConsequence", (controller, host, window) =>
        {
            SelectUpper(controller, "cv-3");
            Settle(window);
            var mode = controller.Section!;
            var facts = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment);
            var groups = host.Properties.ShownModel!.Groups;
            var section = groups.Single(group => group.Id == "sec");
            var ownRow = section.Rows.Single(row => row.Key == "sec:own");
            string own = $"{Quantity.Typed(facts.OwnThickness * 100)} % at {Quantity.Typed(facts.OwnThicknessX * 100)}";
            var stations = section.Rows.Where(row => row.Label.StartsWith("t/c at ", StringComparison.Ordinal)).ToList();
            if (ownRow is not { Label: "Own t/c" } || ownRow.Value != own || stations.Count != 2 ||
                stations.Any(row => row.Unit != "%") || section.Notes[0].Text != PropertiesView.ThicknessNote ||
                !groups.Single(group => group.Id == "sec-le").Rows.Any(row => row is { Key: "sec:intent", Value: "channel" }))
                throw new InvalidOperationException("Section group: " + string.Join(" | ", section.Rows.Select(row => $"{row.Label}={row.Value}")));
            string rootBefore = stations[0].Value;
            // The consequence (R-3): a thicker section does not thicken the foil under "From the Thickness curve".
            var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
            Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, "cv-3", point.SpanMeters, point.Ordinate + 0.01)));
            Settle(window);
            section = host.Properties.ShownModel!.Groups.Single(group => group.Id == "sec");
            ownRow = section.Rows.Single(row => row.Key == "sec:own");
            if (ownRow.Value == own || section.Rows.First(row => row.Label.StartsWith("t/c at ", StringComparison.Ordinal)).Value != rootBefore)
                throw new InvalidOperationException($"own t/c {ownRow.Value} (was {own}); station t/c moved");
            var texts = host.Properties.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ToList();
            if (!texts.Contains("Own t/c") || !texts.Contains(PropertiesView.ThicknessNote))
                throw new InvalidOperationException("Section group not rendered");
        });
    }

    /// <summary>
    /// Review captures of the built shell (CFDW_PNL_CAPTURE=&lt;dir&gt;): Properties, the Points pane and the strip in the
    /// mockup's paired states — s2 (point 4 an anchor, Horizontal), s2x (a paired x move), s3 (a crossing with
    /// Show). s2r (the refused refit) is not reachable: Core writes a paired Kind on one surface only (see the PNL report) — light and dark. Off by default; never part of the gate.
    /// </summary>
    public static void Capture()
    {
        if (Environment.GetEnvironmentVariable("CFDW_PNL_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        foreach (var (variant, theme) in new[] { (Avalonia.Styling.ThemeVariant.Light, "light"), (Avalonia.Styling.ThemeVariant.Dark, "dark") })
            Section("Capture_Pnl_" + theme, (controller, host, window) =>
            {
                window.RequestedThemeVariant = variant;
                var upper = controller.SectionCurve(SurfaceSide.Upper)!;
                string id = upper.Points[3].Id;   // the mockup's point 4
                Pump(host.ApplySectionStepAsync(new SectionStep.SetType(SurfaceSide.Upper, id, true)));
                WaitAssessed(controller, window);
                Pump(host.ApplySectionStepAsync(new SectionStep.SetTangent(SurfaceSide.Upper, id, TangentKind.Horizontal, null, null)));
                WaitAssessed(controller, window);
                SelectUpper(controller, id);
                Settle(window);
                Save(window, Path.Combine(directory, $"pnl-s2-{theme}.png"));
                var control = controller.SectionCurve(SurfaceSide.Upper)!.Points.Last(point => point.Role == PointRole.Control);
                var (typed, _) = Between(controller, control.Id);
                SelectUpper(controller, control.Id);
                Settle(window);
                var box = Need<TextBox>(host.Properties, "PointSpanInput");
                box.Focus();
                box.Text = typed;
                Key(box, Avalonia.Input.Key.Enter);
                WaitAssessed(controller, window);
                Save(window, Path.Combine(directory, $"pnl-s2x-{theme}.png"));
                Pump(controller.UndoSectionStepAsync());
                WaitAssessed(controller, window);
                var lower = controller.SectionCurve(SurfaceSide.Lower)!;
                var low = lower.Points[lower.Points.Count - 3];
                controller.Select(new Selection.Points([new PointRef("lower", low.Id, controller.Section!.Draft.Profile)]));
                Pump(host.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Lower, low.Id, low.SpanMeters, 0.10)));
                WaitAssessed(controller, window);
                Save(window, Path.Combine(directory, $"pnl-s3-{theme}.png"));
                Console.WriteLine($"CAPTURE {theme} strip '{host.StatusStrip.Text}'");
            });
    }

    private static void Save(Window window, string path)
    {
        Settle(window);
        var size = new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height);
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
        bitmap.Render(window);
        bitmap.Save(path);
    }

    /// <summary>A window on the Example in the Precision workspace with the Root section open.</summary>
    private static void Section(string name, Action<WorkbenchController, ShellHost, Window> body) => Pane(name, (controller, host, window) =>
    {
        host.ApplyWorkspace(WorkspaceId.Precision);
        Pump(controller.EnterSectionAsync(0, EntryOrigin.Properties));
        WaitAssessed(controller, window);
        body(controller, host, window);
    });

    private static void SelectControl(WorkbenchController controller)
    {
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.First(item => item.Role == PointRole.Control);
        SelectUpper(controller, point.Id);
    }

    /// <summary>An x between the point and its neighbour toward the nose, typed to 0.01 % c, and the chord fraction it is.</summary>
    private static (string Typed, double Fraction) Between(WorkbenchController controller, string id)
    {
        var points = controller.SectionCurve(SurfaceSide.Upper)!.Points;
        var point = points.Single(item => item.Id == id);
        double percent = Math.Round((points[point.Index - 1].SpanMeters + point.SpanMeters) / 2 * 100, 2);
        string typed = percent.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return (typed, double.Parse(typed, System.Globalization.CultureInfo.InvariantCulture) / 100);
    }

    private static PointView SelectUpper(WorkbenchController controller, string id)
    {
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == id);
        controller.Select(new Selection.Points([new PointRef("upper", id, controller.Section!.Draft.Profile)]));
        return point;
    }

    /// <summary>Pumps until the open section's assessment for its current generation has landed (or none is due).</summary>
    private static void WaitAssessed(WorkbenchController controller, Window window)
    {
        var start = DateTime.UtcNow;
        while (controller.Section is { } mode && mode.Draft.StepCount > 0 && mode.Assessment is null && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Settle(window);
    }
}
