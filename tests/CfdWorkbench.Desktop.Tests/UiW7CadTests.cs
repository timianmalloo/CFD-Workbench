using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;
using Pointer = Avalonia.Input.Pointer;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 7B Track W7-CAD red-first regression tests covering F-21, F-24, F-25, F-26, F-27, F-33, F-37, F-39, F-40,
/// F-41, F-42, F-44, F-45, and F-46 from docs/reviews/ui-as-built-workbench.md.
/// Wired into DesktopChecks.Spawn and ScaleModes as --ui-w7-cad.
/// </summary>
public static class UiW7CadTests
{
    public static void Run()
    {
        DesktopChecks.Check("UiW7Cad_F21_AsyncHygiene_NoPumpUiSpinLoops_AndPendingGuard", CheckF21AsyncHygiene);
        DesktopChecks.Check("UiW7Cad_F24_PrecisionEditing_DoubleClickFocus_PointsLostFocus_FocusRouting_MatchFlyout", CheckF24PrecisionEditing);
        DesktopChecks.Check("UiW7Cad_F25_SectionCanvas_ZoomPan_DeleteKey_CSync_CurveProximityInsert", CheckF25SectionCanvas);
        DesktopChecks.Check("UiW7Cad_F26_ViewportLayout_FrontPortClear_FourViewLanes_ScopeChipNoClip_PlanCanvasLayout", CheckF26ViewportLayout);
        DesktopChecks.Check("UiW7Cad_F27_F41_F44_BrowserPane_SelectionClear_InPlaceDiff_CanRunMenu_RoleGlyphs_SingleParse", CheckF27BrowserPane);
        DesktopChecks.Check("UiW7Cad_F33_PointsPaneTextScale_And_CombPlateResponsiveWidth", CheckF33ScalingAndCombPlate);
        DesktopChecks.Check("UiW7Cad_F37_F39_F40_F41_F42_F45_F46_CadPolish_Typography_Parity_Errors_Menu_Bounds_Roles", CheckF37ThroughF46);
    }

    private static string RepoRoot([CallerFilePath] string self = "") =>
        System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(self)!, "..", ".."));

    private static string RepoFile(params string[] parts) =>
        System.IO.Path.Combine([RepoRoot(), .. parts]);

    private static void CheckF21AsyncHygiene()
    {
        string propsSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Panes", "PropertiesPane.axaml.cs"));
        string browserSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Panes", "BrowserPane.axaml.cs"));
        if (propsSrc.Contains("PumpUi", StringComparison.Ordinal) ||
            propsSrc.Contains("Dispatcher.UIThread.RunJobs", StringComparison.Ordinal))
            throw new InvalidOperationException("PropertiesPane.axaml.cs still contains synchronous PumpUi / RunJobs spin loop");
        if (browserSrc.Contains("Dispatcher.UIThread.RunJobs", StringComparison.Ordinal) ||
            browserSrc.Contains("TimeSpan.FromSeconds(8)", StringComparison.Ordinal))
            throw new InvalidOperationException("BrowserPane.axaml.cs still contains synchronous RunJobs 8 s spin loop");

        // Verify PropertiesPane exposes an async Pending task and commits without blocking.
        var pendingProp = typeof(PropertiesPane).GetProperty("Pending", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PropertiesPane.Pending task property is missing");
        var browserPendingProp = typeof(BrowserPane).GetProperty("Pending", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("BrowserPane.Pending task property is missing");

        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var span = host.Properties.FindControl<TextBox>("SpanInput")!;
            string before = controller.AcceptedSource;
            span.Focus();
            span.Text = "1020";
            span.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = span, Key = Key.Enter });
            var pending = (Task)pendingProp.GetValue(host.Properties)!;
            DesktopChecks.PumpToCompletion(pending, "properties async span commit", TimeSpan.FromSeconds(8));
            PropertiesViewTests.WaitIdle(controller, window);
            if (controller.AcceptedSource == before)
                throw new InvalidOperationException("Async PropertiesPane Enter commit did not apply span edit");
            if (browserPendingProp.GetValue(host.Browser) is not Task)
                throw new InvalidOperationException("BrowserPane.Pending is not a Task");
        }
        finally { window.Close(); }
    }

    private static void CheckF24PrecisionEditing()
    {
        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            // (1) Single-point double-click AND Return in PlanCanvas focus PropertiesPane.
            var plan = host.ModelView.PlanCanvas;
            var pt = controller.Planform!.Trailing.Points.First(p => p.Role == PointRole.Control);
            var pref = new PointRef(pt.Curve, pt.Id);
            plan.SelectPoint(pref, extend: false, toggle: false);
            plan.FocusPoint(pref);
            Settle(window);
            if (!plan.IsFocused)
                throw new InvalidOperationException("Precondition failed: PlanCanvas did not hold focus before double-click");
            using (var dblPtr = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true))
            {
                var ptScreen = plan.ScreenPoint(pt);
                plan.RaiseEvent(new PointerPressedEventArgs(plan, dblPtr, window,
                    plan.TranslatePoint(ptScreen, window)!.Value, 1,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None, 2));
            }
            Settle(window);
            if (!host.Properties.IsKeyboardFocusWithin)
                throw new InvalidOperationException("Single-point double-click on PlanCanvas did not focus PropertiesPane");

            plan.FocusPoint(pref);
            Settle(window);
            if (!plan.IsFocused)
                throw new InvalidOperationException("Precondition failed: PlanCanvas did not hold focus before Return");
            plan.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = plan, Key = Key.Return });
            Settle(window);
            if (!host.Properties.IsKeyboardFocusWithin)
                throw new InvalidOperationException("Single-point Return on PlanCanvas did not focus PropertiesPane");

            // (2) Single-point Return in ElevationView focuses PropertiesPane.
            controller.Layout = ViewLayout.Four;
            host.RefreshPanes();
            Settle(window);
            var twistPt = controller.CurveFor("twist")!.Points.First(p => p.Freedom != PointFreedom.Fixed);
            var twistRef = new PointRef("twist", twistPt.Id);
            host.ModelView.SideElevation.SelectPoint(twistRef, extend: false, toggle: false);
            host.ModelView.SideElevation.FocusPoint(twistRef);
            Settle(window);
            host.ModelView.SideElevation.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent, Source = host.ModelView.SideElevation, Key = Key.Return
            });
            Settle(window);
            if (!host.Properties.IsKeyboardFocusWithin)
                throw new InvalidOperationException("Single-point Return on ElevationView did not focus PropertiesPane");

            // (3) PropertiesPane Shift+Tab from first value on twist routes back to SideElevation (not PlanCanvas).
            var firstVal = window.FocusManager?.GetFocusedElement() as Control
                ?? throw new InvalidOperationException("No focused control in PropertiesPane");
            firstVal.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent, Source = firstVal, Key = Key.Tab, KeyModifiers = KeyModifiers.Shift
            });
            Settle(window);
            if (!host.ModelView.SideElevation.IsFocused)
                throw new InvalidOperationException("PropertiesPane Shift+Tab on twist point did not return focus to SideElevation; focused=" +
                    window.FocusManager?.GetFocusedElement()?.GetType().Name);

            // (4) PointsPane LostFocus commits edited coordinate.
            host.ApplyWorkspace(WorkspaceId.Precision);
            Settle(window);
            var yBox = host.Points.GetVisualDescendants().OfType<TextBox>()
                .FirstOrDefault(b => b.Name == "PointY_trailing_" + pt.Id)
                ?? throw new InvalidOperationException("PointsPane trailing Y TextBox not found");
            double oldOrd = controller.CurveFor("trailing")!.Points.First(p => p.Id == pt.Id).Ordinate;
            string newVal = ((oldOrd + 0.004) * 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            yBox.Focus();
            Settle(window);
            yBox.Text = newVal;
            host.Properties.FocusFirstValue();
            Settle(window);
            DesktopChecks.PumpToCompletion(host.Points.Pending, "points LostFocus commit", TimeSpan.FromSeconds(8));
            PropertiesViewTests.WaitIdle(controller, window);
            double afterOrd = controller.CurveFor("trailing")!.Points.First(p => p.Id == pt.Id).Ordinate;
            if (Math.Abs(afterOrd - (oldOrd + 0.004)) > 1e-4)
                throw new InvalidOperationException($"PointsPane LostFocus did not commit edit: before={oldOrd:F5}, after={afterOrd:F5}");

            // (5) PropertiesPane Shift+Tab & second Escape in Section mode route to SectionModeEditor.ModeCanvas.
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Properties), "enter section", TimeSpan.FromSeconds(15));
            host.RefreshPanes();
            Settle(window);
            var secPt = controller.SectionCurve(SurfaceSide.Upper)!.Points.First(p => p.Freedom == PointFreedom.Free);
            controller.Select(new Selection.Points([new PointRef("upper", secPt.Id, controller.Section!.Draft.Profile)]));
            host.RefreshPanes();
            Settle(window);
            host.Properties.FocusFirstValue();
            Settle(window);
            var focusedSec = window.FocusManager?.GetFocusedElement() as Control
                ?? throw new InvalidOperationException("Properties first value not focused in Section mode");
            focusedSec.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent, Source = focusedSec, Key = Key.Tab, KeyModifiers = KeyModifiers.Shift
            });
            Settle(window);
            if (!host.ModelView.SectionModeEditor.ModeCanvas.IsFocused)
                throw new InvalidOperationException("PropertiesPane Shift+Tab in Section mode did not return focus to SectionModeEditor.ModeCanvas");

            var secSpanBox = host.Properties.FindControl<TextBox>("PointSpanInput")!;
            secSpanBox.Focus();
            Settle(window);
            secSpanBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = secSpanBox, Key = Key.Escape });
            Settle(window);
            if (!host.ModelView.SectionModeEditor.ModeCanvas.IsFocused)
                throw new InvalidOperationException("PropertiesPane second Escape in Section mode did not return focus to SectionModeEditor.ModeCanvas");

            controller.CancelSection();
            host.RefreshPanes();
            Settle(window);

            // (6) WorkbenchController.Select preserves StationFlyoutKind.Match on re-selecting the same station.
            // Open an interior station (or add one) and trigger Match flyout.
            controller.PlaceSlice(0.5);
            DesktopChecks.PumpToCompletion(controller.PromoteSliceAsync(), "promote mid station", TimeSpan.FromSeconds(15));
            var mid = controller.Selection as Selection.Station
                ?? throw new InvalidOperationException("Promoted station was not selected");
            controller.RunStationAction("stn:match");
            var startWait = DateTime.UtcNow;
            while (controller.StationPanel?.Flyout is null && DateTime.UtcNow - startWait < TimeSpan.FromSeconds(5))
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            if (controller.StationPanel?.Flyout?.Kind != WorkbenchController.StationFlyoutKind.Match)
                throw new InvalidOperationException("Match flyout did not open: " + controller.StationPanel?.Flyout?.Kind);
            controller.Select(new Selection.Station(mid.Index, mid.Eta));
            if (controller.StationPanel?.Flyout?.Kind != WorkbenchController.StationFlyoutKind.Match)
                throw new InvalidOperationException("WorkbenchController.Select dismissed StationFlyoutKind.Match on re-selecting the same station");
        }
        finally { window.Close(); }
    }

    private static void CheckF25SectionCanvas()
    {
        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Properties), "enter section", TimeSpan.FromSeconds(15));
            host.RefreshPanes();
            Settle(window);

            var editor = host.ModelView.SectionModeEditor;
            var canvas = editor.ModeCanvas;
            var curvToggle = editor.FindControl<ToggleButton>("CurvatureToggle")!;
            var combPlate = editor.FindControl<Border>("ModeCombPlate")!;

            // (1) Wheel zoom & Ctrl± zoom.
            var beforeLeft = canvas.ScreenToModel(new Point(0, 0)).X;
            var beforeRight = canvas.ScreenToModel(new Point(canvas.Bounds.Width, 0)).X;
            double spanBefore = beforeRight - beforeLeft;
            using var wheelPtr = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            canvas.RaiseEvent(new PointerWheelEventArgs(canvas, wheelPtr, window,
                canvas.TranslatePoint(new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2), window)!.Value,
                1, new PointerPointProperties(), KeyModifiers.None, new Vector(0, 2)));
            Settle(window);
            double spanAfterWheel = canvas.ScreenToModel(new Point(canvas.Bounds.Width, 0)).X - canvas.ScreenToModel(new Point(0, 0)).X;
            if (!(spanAfterWheel < spanBefore * 0.95))
                throw new InvalidOperationException($"SectionCanvas wheel did not zoom in: before={spanBefore:F4}, after={spanAfterWheel:F4}");

            canvas.Focus();
            canvas.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent, Source = canvas, Key = Key.OemPlus, KeyModifiers = KeyModifiers.Control
            });
            Settle(window);
            double spanAfterCtrlPlus = canvas.ScreenToModel(new Point(canvas.Bounds.Width, 0)).X - canvas.ScreenToModel(new Point(0, 0)).X;
            if (!(spanAfterCtrlPlus < spanAfterWheel * 0.95))
                throw new InvalidOperationException($"SectionCanvas Ctrl+ did not zoom in: before={spanAfterWheel:F4}, after={spanAfterCtrlPlus:F4}");

            // (2) Alt+Arrows pan.
            double xBeforePan = canvas.ScreenToModel(new Point(0, 0)).X;
            canvas.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent, Source = canvas, Key = Key.Right, KeyModifiers = KeyModifiers.Alt
            });
            Settle(window);
            double xAfterAltPan = canvas.ScreenToModel(new Point(0, 0)).X;
            if (Math.Abs(xAfterAltPan - xBeforePan) < 1e-5)
                throw new InvalidOperationException("SectionCanvas Alt+Arrow did not pan view");

            // (3) Empty-space drag pan.
            var panStart = new Point(canvas.Bounds.Width * 0.5, 40);
            var panEnd = new Point(canvas.Bounds.Width * 0.5 + 40, 60);
            double xBeforeDrag = canvas.ScreenToModel(new Point(0, 0)).X;
            using var dragPtr = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, dragPtr, window,
                canvas.TranslatePoint(panStart, window)!.Value, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None, 1));
            canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, dragPtr, window,
                canvas.TranslatePoint(panEnd, window)!.Value, 2,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, dragPtr, window,
                canvas.TranslatePoint(panEnd, window)!.Value, 3,
                new PointerPointProperties(), KeyModifiers.None, MouseButton.Left));
            Settle(window);
            double xAfterDrag = canvas.ScreenToModel(new Point(0, 0)).X;
            if (Math.Abs(xAfterDrag - xBeforeDrag) < 1e-5)
                throw new InvalidOperationException("SectionCanvas empty-space drag did not pan view");

            canvas.Fit();
            Settle(window);

            // (4) C shortcut toggles curvature AND syncs CurvatureToggle + ModeCombPlate.
            bool initialCurv = canvas.CurvatureVisible;
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = canvas, Key = Key.C });
            Settle(window);
            if (canvas.CurvatureVisible == initialCurv || curvToggle.IsChecked != canvas.CurvatureVisible || combPlate.IsVisible != canvas.CurvatureVisible)
                throw new InvalidOperationException(
                    $"SectionCanvas C toggle out of sync: canvas={canvas.CurvatureVisible}, toggle={curvToggle.IsChecked}, plate={combPlate.IsVisible}");

            // (5) Double-click far from curve does NOT insert CV; double-click on curve DOES insert CV.
            int countBeforeEmpty = controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count;
            var farPoint = new Point(canvas.Bounds.Width * 0.5, 16);
            using var clickPtr = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, clickPtr, window,
                canvas.TranslatePoint(farPoint, window)!.Value, 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None, 2));
            Settle(window);
            int countAfterEmpty = controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count;
            if (countAfterEmpty != countBeforeEmpty)
                throw new InvalidOperationException($"SectionCanvas double-click in empty space inserted a CV ({countBeforeEmpty} -> {countAfterEmpty})");

            var upperPts = controller.SectionCurve(SurfaceSide.Upper)!.Points;
            double bestGapX = 0.55;
            double bestGap = -1;
            for (int i = 1; i < upperPts.Count; i++)
            {
                double gap = upperPts[i].SpanMeters - upperPts[i - 1].SpanMeters;
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestGapX = (upperPts[i - 1].SpanMeters + upperPts[i].SpanMeters) * 0.5;
                }
            }
            var upperProbe = Sections.Probe(controller.Section!.Draft.Bytes, controller.Section.Draft.Assignment, bestGapX);
            var onCurve = canvas.ModelToScreen(bestGapX, upperProbe.UpperY);
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, clickPtr, window,
                canvas.TranslatePoint(onCurve, window)!.Value, 2,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None, 2));
            var waitInsert = DateTime.UtcNow;
            while (controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count == countBeforeEmpty &&
                   DateTime.UtcNow - waitInsert < TimeSpan.FromSeconds(8))
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            Settle(window);
            int countAfterInsert = controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count;
            if (countAfterInsert <= countBeforeEmpty)
            {
                string reason = editor.FindControl<TextBlock>("ModeReason")?.Text ?? "(none)";
                throw new InvalidOperationException($"SectionCanvas double-click on curve at x={bestGapX:F3} did not insert a CV (selected={canvas.SelectedVertex}, reason={reason})");
            }

            // (6) Key.Delete deletes selected interior point.
            var removable = controller.SectionCurve(SurfaceSide.Upper)!.Points.First(p => p.Freedom == PointFreedom.Free);
            canvas.SelectedVertex = ("upper", removable.Id);
            controller.Select(new Selection.Points([new PointRef("upper", removable.Id, controller.Section.Draft.Profile)]));
            Settle(window);
            canvas.Focus();
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = canvas, Key = Key.Delete });
            var waitDel = DateTime.UtcNow;
            while (controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count == countAfterInsert &&
                   DateTime.UtcNow - waitDel < TimeSpan.FromSeconds(8))
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            Settle(window);
            int countAfterDelete = controller.SectionCurve(SurfaceSide.Upper)!.Points.Count + controller.SectionCurve(SurfaceSide.Lower)!.Points.Count;
            if (countAfterDelete >= countAfterInsert)
                throw new InvalidOperationException("SectionCanvas Key.Delete did not delete selected CV");
        }
        finally { window.Close(); }
    }

    private static void CheckF26ViewportLayout()
    {
        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        controller.Layout = ViewLayout.Four;
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1024, Height = 700 };
        try
        {
            window.Show();
            Settle(window);

            // (1) Front "port" label clear of scale plate.
            var front = host.ModelView.FrontElevation;
            var side = host.ModelView.SideElevation;
            var portProp = typeof(ElevationView).GetProperty("PortLabelBounds", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ElevationView.PortLabelBounds not exposed");
            var scaleProp = typeof(ElevationView).GetProperty("ScalePlateBounds", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ElevationView.ScalePlateBounds not exposed");
            var portRect = (Rect)portProp.GetValue(front)!;
            var scaleRect = (Rect?)scaleProp.GetValue(front);
            if (scaleRect is null || portRect.Width <= 0)
                throw new InvalidOperationException($"Front elevation missing port/scale bounds: port={portRect}, scale={scaleRect}");
            if (portRect.Intersects(scaleRect.Value))
                throw new InvalidOperationException($"Front 'port' label {portRect} overlaps scale plate {scaleRect.Value}");

            // (2) Four-View Elevation lanes usable at 1024x700 (>= 60 px height).
            if (front.LaneRect.Height < 60 || side.LaneRect.Height < 60)
                throw new InvalidOperationException(
                    $"Four-view Elevation lanes too short at 1024x700: Front={front.LaneRect.Height:F1} px, Side={side.LaneRect.Height:F1} px");

            // (3) SectionEditorView ScopeChip trims and never clips Cancel/Finish at 1024 px.
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Properties), "enter section", TimeSpan.FromSeconds(15));
            host.RefreshPanes();
            Settle(window);
            var editor = host.ModelView.SectionModeEditor;
            var scopeChip = editor.FindControl<TextBlock>("ScopeChip")!;
            scopeChip.Text = "Only Station 2 uses this section · the wing blends on Root's spacing + Station 1 and Tip's anchors";
            Settle(window);
            if (scopeChip.TextTrimming != TextTrimming.CharacterEllipsis)
                throw new InvalidOperationException("SectionEditorView ScopeChip does not use CharacterEllipsis");
            var cancelBtn = editor.FindControl<Button>("ModeCancelButton")!;
            var finishBtn = editor.FindControl<Button>("ModeFinishButton")!;
            var finishOrigin = finishBtn.TranslatePoint(default, window)
                ?? throw new InvalidOperationException("Finish button has no visual transform");
            var cancelOrigin = cancelBtn.TranslatePoint(default, window)
                ?? throw new InvalidOperationException("Cancel button has no visual transform");
            if (cancelOrigin.X + cancelBtn.Bounds.Width > window.Bounds.Width + 0.5 ||
                finishOrigin.X + finishBtn.Bounds.Width > window.Bounds.Width + 0.5 ||
                finishBtn.Bounds.Width < 60)
                throw new InvalidOperationException(
                    $"SectionEditorView ModeBar clipped Cancel/Finish at 1024 px: cancel={cancelOrigin.X:F1}+{cancelBtn.Bounds.Width:F1}, finish={finishOrigin.X:F1}+{finishBtn.Bounds.Width:F1}, win={window.Bounds.Width:F1}");
            controller.CancelSection();
            controller.Layout = ViewLayout.Plan3d;
            host.RefreshPanes();
            Settle(window);

            // (4) PlanCanvas error banner y >= 36, tooltip box measured to fit text, station chip clipped inside bounds.
            var plan = host.ModelView.PlanCanvas;
            var pt = controller.Planform!.Trailing.Points.First(p => p.Role == PointRole.Control);
            plan.HoverAt(plan.ScreenPoint(pt));
            Settle(window);
            using (var bmp = PropertiesCellsTests.Render(window, 1)) { }
            var tipProp = typeof(PlanCanvas).GetProperty("TooltipBounds", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("PlanCanvas.TooltipBounds not exposed");
            var tipRect = (Rect?)tipProp.GetValue(plan);
            if (tipRect is null || tipRect.Value.Right > plan.Bounds.Width + 0.5 || tipRect.Value.Left < 0)
                throw new InvalidOperationException($"PlanCanvas tooltip bounds invalid/clipped: {tipRect} in width {plan.Bounds.Width}");

            var bannerProp = typeof(PlanCanvas).GetProperty("RenderBannerBounds", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("PlanCanvas.RenderBannerBounds not exposed");
            plan.RenderGuard = () => throw new InvalidOperationException("probe banner position");
            try
            {
                plan.InvalidateVisual();
                using (var bmp = PropertiesCellsTests.Render(window, 1)) { }
                var bannerRect = (Rect?)bannerProp.GetValue(plan);
                if (bannerRect is null || bannerRect.Value.Y < 36)
                    throw new InvalidOperationException($"PlanCanvas error banner overlaps PlanLabel: {bannerRect}");
            }
            finally
            {
                plan.RenderGuard = null;
                plan.RetryRender();
            }
        }
        finally { window.Close(); }
    }

    private static void CheckF27BrowserPane()
    {
        string browserSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Panes", "BrowserPane.axaml.cs"));
        // Single-parse check (F-44): FoilSource.Parse should not run inside the per-station loop.
        int loopIdx = browserSrc.IndexOf("foreach (var station in authored.Assignments)", StringComparison.Ordinal);
        int bindEndIdx = browserSrc.IndexOf("BindStations(StationList", StringComparison.Ordinal);
        if (loopIdx > 0 && bindEndIdx > loopIdx)
        {
            string loopBody = browserSrc[loopIdx..bindEndIdx];
            if (loopBody.Contains("Encoding.UTF8.GetBytes", StringComparison.Ordinal) ||
                loopBody.Contains("FoilSource.Parse", StringComparison.Ordinal) ||
                loopBody.Contains("StationSourceText(station.ProfileName, source)", StringComparison.Ordinal))
                throw new InvalidOperationException("BrowserPane.Bind still re-encodes or re-parses FoilSource inside the per-station loop (F-44)");
        }

        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            host.ShowPane("browser");
            Settle(window);

            var stationList = host.Browser.FindControl<ListBox>("StationList")!;
            var leList = host.Browser.FindControl<ListBox>("LeadingEdgeList")!;

            // (1) StationList.SelectedItem clears on non-station selection.
            controller.Select(new Selection.Station(0, controller.Inspection!.Authored.Assignments[0].Eta));
            host.RefreshPanes();
            Settle(window);
            if (stationList.SelectedItem is null)
                throw new InvalidOperationException("StationList did not select station 0");
            var pt = controller.Planform!.Leading.Points.First(p => p.Role == PointRole.Control);
            controller.Select(new Selection.Points([new PointRef("leading", pt.Id)]));
            host.RefreshPanes();
            Settle(window);
            if (stationList.SelectedItem is not null)
                throw new InvalidOperationException("BrowserPane did not clear StationList.SelectedItem on non-station selection");

            // (2) In-place diff preserves ListBoxItem instances & keyboard focus across Bind().
            var firstItem = leList.Items.OfType<ListBoxItem>().First();
            firstItem.Focus();
            Settle(window);
            host.Browser.Bind(controller);
            Settle(window);
            var firstItemAfter = leList.Items.OfType<ListBoxItem>().First();
            if (!ReferenceEquals(firstItem, firstItemAfter) || !firstItemAfter.IsFocused)
                throw new InvalidOperationException("BrowserPane.Bind rebuilt rail ListBoxItems and dropped keyboard focus");

            // (3) Role-matched glyphs: RootEnd is diamond Path, Control is Ellipse (distinct shapes, all RoleGlyph).
            var rootGlyph = firstItem.GetVisualDescendants().OfType<Shape>().FirstOrDefault(s => s.Name == "RoleGlyph");
            var ctrlItem = leList.Items.OfType<ListBoxItem>().First(i => Equals(i.Tag, pt.Id));
            var ctrlGlyph = ctrlItem.GetVisualDescendants().OfType<Shape>().FirstOrDefault(s => s.Name == "RoleGlyph");
            if (rootGlyph is null || ctrlGlyph is null || rootGlyph.GetType() == ctrlGlyph.GetType())
                throw new InvalidOperationException(
                    $"BrowserPane uses identical glyph types for RootEnd ({rootGlyph?.GetType().Name}) and Control ({ctrlGlyph?.GetType().Name})");

            // (4) Context menu gated via ShellHost.CanRun (RootEnd cannot Make Anchor Point; Control can).
            controller.Select(new Selection.Points([new PointRef("leading", controller.Planform.Leading.Points[0].Id)]));
            host.RefreshPanes();
            var rootMenu = firstItem.ContextMenu
                ?? throw new InvalidOperationException("BrowserPane rail item has no ContextMenu");
            rootMenu.Open(firstItem);
            Settle(window);
            var makeAnchor = rootMenu.Items.OfType<MenuItem>().FirstOrDefault(m => Equals(m.Header, "Make anchor"));
            var makeControl = rootMenu.Items.OfType<MenuItem>().FirstOrDefault(m => Equals(m.Header, "Make control"));
            var remove = rootMenu.Items.OfType<MenuItem>().FirstOrDefault(m => Equals(m.Header, "Remove point"));
            rootMenu.Close();
            if (makeAnchor is null || makeControl is null || remove is null)
                throw new InvalidOperationException("BrowserPane context menu missing Make anchor / Make control / Remove point");
            if (makeAnchor.IsEnabled || remove.IsEnabled)
                throw new InvalidOperationException("BrowserPane context menu did not gate Make anchor / Remove point via ShellHost.CanRun on RootEnd");
        }
        finally { window.Close(); }
    }

    private static void CheckF33ScalingAndCombPlate()
    {
        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1024, Height = 700 };
        try
        {
            window.Show();
            host.ApplyWorkspace(WorkspaceId.Precision);
            Settle(window);

            // (1) ShellHost.SetTextScale scales PointsPane.
            var textScaleProp = typeof(PointsPane).GetProperty("TextScale", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("PointsPane.TextScale property missing");
            host.SetTextScale(1.5);
            Settle(window);
            double scaled = (double)textScaleProp.GetValue(host.Points)!;
            if (Math.Abs(scaled - 1.5) > 1e-6)
                throw new InvalidOperationException($"ShellHost.SetTextScale(1.5) did not scale PointsPane (TextScale={scaled})");
            if (!host.Points.TryFindResource("PropFontSize", out var fontRes) || fontRes is not double fs || Math.Abs(fs - 16.5) > 0.1)
                throw new InvalidOperationException($"PointsPane PropFontSize did not scale to 16.5 (got {fontRes})");
            host.SetTextScale(1.0);
            Settle(window);

            // (2) CombPlate uses MaxWidth="360" + Stretch and does not overflow a 320 px narrow disclosure.
            controller.CombVisible = true;
            host.RefreshPanes();
            Settle(window);
            var plate = host.ModelView.CombPlate;
            var card = plate.FindControl<Border>("Card")!;
            if (!double.IsNaN(card.Width) || Math.Abs(card.MaxWidth - 360) > 0.5)
                throw new InvalidOperationException($"CombPlate Card should use MaxWidth=360 & NaN Width (got Width={card.Width}, MaxWidth={card.MaxWidth})");
            var hostBorder = new Border { Width = 320 };
            var narrowWin = new Window { Content = hostBorder, Width = 360, Height = 500 };
            try
            {
                var narrowPlate = new CombPlate { Controller = controller, DisclosureHost = hostBorder };
                narrowPlate.SetWidth(320);
                narrowPlate.FindControl<Button>("SummaryButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                narrowWin.Show();
                Settle(narrowWin);
                var narrowCard = narrowPlate.FindControl<Border>("Card")!;
                if (narrowCard.Bounds.Width > 320.5)
                    throw new InvalidOperationException($"CombPlate overflowed 320 px disclosure: width={narrowCard.Bounds.Width:F1}");
            }
            finally { narrowWin.Close(); }
        }
        finally { window.Close(); }
    }

    private static void CheckF37ThroughF46()
    {
        // (1) F-37: Zero hardcoded "Inter" or FontFamily.Default Typeface in CAD canvases; zero raw Color.Parse / FontSize < 11.
        string[] cadFiles =
        [
            RepoFile("src", "CfdWorkbench.Desktop", "PlanCanvas.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "ElevationView.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "View3d.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "SectionCanvas.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "PointContextMenu.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "Panes", "PropertiesPane.axaml.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "Panes", "BrowserPane.axaml.cs"),
            RepoFile("src", "CfdWorkbench.Desktop", "Panes", "PointsPane.axaml.cs")
        ];
        foreach (var path in cadFiles)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException("Missing expected CAD file: " + System.IO.Path.GetFileName(path));
            string src = File.ReadAllText(path);
            if (src.Contains("new Typeface(\"Inter\"", StringComparison.Ordinal))
                throw new InvalidOperationException($"{System.IO.Path.GetFileName(path)} still hardcodes Typeface(\"Inter\")");
            if (src.Contains("new Typeface(FontFamily.Default", StringComparison.Ordinal))
                throw new InvalidOperationException($"{System.IO.Path.GetFileName(path)} still hardcodes Typeface(FontFamily.Default)");
            if (Regex.IsMatch(src, @"Color\.Parse\s*\("))
                throw new InvalidOperationException($"{System.IO.Path.GetFileName(path)} contains raw Color.Parse(...)");
        }

        // +tnum,+lnum on RebuildPopover and BrowserPane.
        string rebuildAxaml = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "RebuildPopover.axaml"));
        string browserAxaml = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Panes", "BrowserPane.axaml"));
        if (!rebuildAxaml.Contains("+tnum,+lnum", StringComparison.Ordinal))
            throw new InvalidOperationException("RebuildPopover.axaml missing +tnum,+lnum");
        if (!browserAxaml.Contains("+tnum,+lnum", StringComparison.Ordinal))
            throw new InvalidOperationException("BrowserPane.axaml missing +tnum,+lnum");

        // (2) F-39: OS-branched View3d help text & smooth-scroll wheel zoom; PropertiesFieldNudge gate invariant.
        var helpFor = typeof(View3d).GetMethod("HelpTextFor", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("View3d.HelpTextFor(bool) missing");
        string winHelp = (string)helpFor.Invoke(null, [false])!;
        string macHelp = (string)helpFor.Invoke(null, [true])!;
        if (!winHelp.Contains("Alt", StringComparison.Ordinal) || !winHelp.Contains("Ctrl", StringComparison.Ordinal) ||
            winHelp.Contains("Option", StringComparison.Ordinal) || winHelp.Contains("Command", StringComparison.Ordinal))
            throw new InvalidOperationException("Windows View3d help text has macOS keys: " + winHelp);
        if (!macHelp.Contains("Option", StringComparison.Ordinal) || !macHelp.Contains("Command", StringComparison.Ordinal))
            throw new InvalidOperationException("macOS View3d help text missing Option/Command: " + macHelp);
        var isTrackpad = typeof(View3d).GetMethod("IsTrackpadPan", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("View3d.IsTrackpadPan(Vector, bool) missing");
        if ((bool)isTrackpad.Invoke(null, [new Vector(0, 0.25), false])!)
            throw new InvalidOperationException("Windows smooth-scroll vertical wheel (0, 0.25) misclassified as trackpad pan");
        if (!(bool)isTrackpad.Invoke(null, [new Vector(0.5, -1.25), false])!)
            throw new InvalidOperationException("Two-finger scroll (0.5, -1.25) not classified as pan");
        // PropertiesFieldNudge gate invariant (B7/B8): disabled by default and always off on Windows.
        if (PropertiesFieldNudge.Active)
            throw new InvalidOperationException("PropertiesFieldNudge should stay gated off by default (B7/B8)");

        // (3) F-40: RoleText in PlanCanvas tooltip & AT name; plain-English fallback errors; CatalogDialog clears ItemStatus.
        string planSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "PlanCanvas.cs"));
        if (planSrc.Contains("point.Role.ToString().ToLowerInvariant()", StringComparison.Ordinal))
            throw new InvalidOperationException("PlanCanvas still uses raw point.Role.ToString().ToLowerInvariant() instead of PropertiesView.RoleText");
        string pointsViewSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "PointsView.cs"));
        if (pointsViewSrc.Contains("error.Reason ?? error.Code", StringComparison.Ordinal))
            throw new InvalidOperationException("PointsView.cs still leaks raw error.Code");
        string saveSecSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "SaveSectionDialog.axaml.cs"));
        if (!saveSecSrc.Contains("Couldn't finish saving this section ({error.Code})", StringComparison.Ordinal))
            throw new InvalidOperationException("SaveSectionDialog.axaml.cs must preserve approved COPY-203 (Ruling 82)");
        string catalogSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "CatalogDialog.axaml.cs"));
        if (catalogSrc.Contains("error.Reason ?? error.Code", StringComparison.Ordinal))
            throw new InvalidOperationException("CatalogDialog.axaml.cs still leaks raw error.Code");

        // (4) F-41: Hide ThreeDDrawing on render error; HC comb without 50% SolidColorBrush per-frame allocation.
        string secCanvasSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "SectionCanvas.cs"));
        if (secCanvasSrc.Contains("new SolidColorBrush(solid.Color, .5)", StringComparison.Ordinal))
            throw new InvalidOperationException("SectionCanvas still allocates per-frame 50% opacity SolidColorBrush for comb");

        // (5) F-42: Shared PointContextMenu; zero regex in PropertiesPane; CommitOutcome structured fields.
        string propsSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "Panes", "PropertiesPane.axaml.cs"));
        if (propsSrc.Contains("Regex", StringComparison.Ordinal))
            throw new InvalidOperationException("PropertiesPane.axaml.cs still uses Regex copy parsing instead of structured CommitOutcome fields");
        if (typeof(CommitOutcome.Refused).GetProperty("MaxMoveAmount") is null ||
            typeof(CommitOutcome.Committed).GetProperty("MaxDeviationText") is null)
            throw new InvalidOperationException("CommitOutcome missing structured MaxMoveAmount / MaxDeviationText fields");
        if (!propsSrc.Contains("intentField.Pending = null", StringComparison.Ordinal) ||
            !propsSrc.Contains(".Append(intentControl)", StringComparison.Ordinal))
            throw new InvalidOperationException("PropertiesPane.axaml.cs missing intentField reset or intentControl cleanup (F-39)");

        // (6) F-45: "Foil source text" AT name; CDi (Trefftz) alignment; model-derived parent title on Escape.
        string modelAreaAxaml = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "ModelArea.axaml"));
        if (modelAreaAxaml.Contains("Retained FoilDSL source text", StringComparison.Ordinal) ||
            !modelAreaAxaml.Contains("Foil source text", StringComparison.Ordinal))
            throw new InvalidOperationException("ModelArea.axaml SourceText AT name is not 'Foil source text'");
        string propsViewSrc = File.ReadAllText(RepoFile("src", "CfdWorkbench.Desktop", "PropertiesView.cs"));
        if (!propsViewSrc.Contains("\"CDi (Trefftz)\"", StringComparison.Ordinal))
            throw new InvalidOperationException("PropertiesView.cs missing 'CDi (Trefftz)' skeleton label matching ProjectionTests.cs");
        if (propsSrc.Contains("Selected {IdentityTitle.Text}", StringComparison.Ordinal))
            throw new InvalidOperationException("PropertiesPane Escape still reads IdentityTitle.Text synchronously instead of deriving parent title from model");

        // (7) Live runtime checks: CatalogDialog clears ItemStatus, ThreeDDrawing hides on error, F-46 distinct Browser vs Points roles.
        using var controller = new WorkbenchController();
        DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);

            // F-46: Distinct pane headers/tooltips/AT names on BrowserPane vs PointsPane.
            string browserName = AutomationProperties.GetName(host.Browser) ?? "";
            string pointsName = AutomationProperties.GetName(host.Points) ?? "";
            if (!browserName.Contains("navigation", StringComparison.OrdinalIgnoreCase) &&
                !browserName.Contains("tree", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"BrowserPane AT name does not state navigation role: '{browserName}'");
            if (!pointsName.Contains("coordinate", StringComparison.OrdinalIgnoreCase) &&
                !pointsName.Contains("tabular", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"PointsPane AT name does not state tabular coordinate editor role: '{pointsName}'");
            if (ToolTip.GetTip(host.Browser) is null || ToolTip.GetTip(host.Points) is null)
                throw new InvalidOperationException("BrowserPane and/or PointsPane missing role tooltip (F-46)");

            // F-41: ThreeDDrawing hidden when ThreeDRenderErrorBand is shown.
            var drawing = host.ModelView.FindControl<TextBlock>("ThreeDDrawing")!;
            var errBand = host.ModelView.FindControl<Border>("ThreeDRenderErrorBand")!;
            var renderer = host.ModelView.FindControl<SurfaceRenderer>("ThreeDRenderer")!;
            renderer.RenderGuard = () => throw new InvalidOperationException("3D error probe");
            try
            {
                renderer.InvalidateVisual();
                using (var bmp = PropertiesCellsTests.Render(window, 1)) { }
                Settle(window);
                if (!errBand.IsVisible || drawing.IsVisible)
                    throw new InvalidOperationException($"ThreeDDrawing visible ({drawing.IsVisible}) while ThreeDRenderErrorBand visible ({errBand.IsVisible})");
            }
            finally
            {
                renderer.RenderGuard = null;
                renderer.RetryRender();
            }

            // F-40: PlanCanvas tooltip uses RoleText (no raw "rootend").
            var rootPt = controller.Planform!.Leading.Points[0];
            host.ModelView.PlanCanvas.HoverAt(host.ModelView.PlanCanvas.ScreenPoint(rootPt));
            if (host.ModelView.PlanCanvas.TooltipText?.Contains("rootend", StringComparison.OrdinalIgnoreCase) == true ||
                host.ModelView.PlanCanvas.TooltipText?.Contains("root end", StringComparison.OrdinalIgnoreCase) != true)
                throw new InvalidOperationException("PlanCanvas tooltip did not use RoleText: " + host.ModelView.PlanCanvas.TooltipText);

            // F-40: CatalogDialog clears prior ItemStatus="active" when selection changes.
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Properties), "enter section", TimeSpan.FromSeconds(15));
            var snap = controller.OpenCatalog();
            var dialog = new CatalogDialog(controller, snap);
            try
            {
                dialog.Show(window);
                Settle(dialog);
                var list = dialog.FindControl<ListBox>("SectionList")!;
                var choosable = list.Items.OfType<ListBoxItem>()
                    .Where(i => i.Tag is CatalogChoice.Catalog { Entry.DisabledReason: null })
                    .Take(2).ToArray();
                if (choosable.Length < 2)
                    throw new InvalidOperationException($"Expected at least 2 choosable catalog rows, found {choosable.Length}");
                list.SelectedItem = choosable[0];
                Settle(dialog);
                list.SelectedItem = choosable[1];
                Settle(dialog);
                if (AutomationProperties.GetItemStatus(choosable[0]) == "active")
                    throw new InvalidOperationException("CatalogDialog did not clear prior ItemStatus='active' on selection change");
                if (AutomationProperties.GetItemStatus(choosable[1]) != "active")
                    throw new InvalidOperationException("CatalogDialog did not set ItemStatus='active' on new selection");
            }
            finally { dialog.Close(); Settle(window); }
        }
        finally { window.Close(); }
    }

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }
}
