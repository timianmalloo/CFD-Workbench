using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using SkiaSharp;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 13A Desktop CAD UI test suite (<c>T-RT-09</c>, <c>docs/design/rounded-wing-tips.md</c> §9).
/// Covers Structure B <c>Tip</c> group across Light/Dark/HighContrast, locked <c>h:angle</c> on vertical
/// <c>TipHandle</c>, <c>DR-CELL-3</c> <c>"Use &lt;max&gt; mm"</c> repair link, <c>PlanCanvas</c> vertical guide tick
/// and <c>ValueOnly</c> x-axis drag constraint, <c>SurfaceRenderer</c> rounded tip cap + <c>SinPi</c> wireframe rib
/// fallback, local <c>EdgeHullCrossing</c> on 25 deg swept wings, <c>COPY-941</c> in both <c>AnalysisPanel</c> and
/// <c>PropertiesPane</c>, <c>ExportDialog</c> tip-closure summary binding, and zero-PII telemetry.
/// </summary>
public static class RoundedTipDesktopTests
{
    private static readonly string[] BannedLab01Words =
        ["best", "optimal", "ideal", "perfect", "world-class", "fastest", "safest", "guaranteed", "validated"];

    public static void Run()
    {
        DesktopChecks.Check("T_RT_09_TipCopy_ApprovedCopy935To942_ZeroLab01", CheckTipCopy);
        DesktopChecks.Check("T_RT_09_PropertiesPane_TipGroup_ThemesAndDrCell3Repair", CheckPropertiesPaneTipGroupAndRepair);
        DesktopChecks.Check("T_RT_09_VerticalTipHandle_AngleLocked_OnTipEndAndTipHandle", CheckVerticalTipHandleAngleLocked);
        DesktopChecks.Check("T_RT_09_PlanCanvas_VerticalGuideTick_And_ValueOnlyDragConstraint", CheckPlanCanvasVerticalTip);
        DesktopChecks.Check("T_RT_09_SurfaceRenderer_RoundedCap_And_SinPiWireframeRibs", CheckSurfaceRendererRoundedAndWireframe);
        DesktopChecks.Check("T_RT_09_EdgeHullCrossing_ZeroFalseAlarmsOn25DegSweptRoundedTipWing", CheckEdgeHullCrossing25DegSwept);
        DesktopChecks.Check("T_RT_09_AnalysisPanel_And_PropertiesPane_BothShowCopy941", CheckAnalysisCopy941BothSurfaces);
        DesktopChecks.Check("T_RT_09_ExportDialog_DisplaysTipClosureSummaryRow", CheckExportDialogTipClosure);
        DesktopChecks.Check("T_RT_09_TelemetryRing_ZeroPiiNamesPathsIdsOrCoords", CheckTelemetryZeroPii);
    }

    private static void CheckTipCopy()
    {
        if (TipCopy.GroupHeading != "Tip") throw new InvalidOperationException("COPY-935 mismatch: " + TipCopy.GroupHeading);
        if (TipCopy.TangentFree != "Free" || TipCopy.TangentVertical != "Vertical (G1)")
            throw new InvalidOperationException("COPY-936 mismatch");
        if (TipCopy.LeRadiusLabel != "LE tip radius" || TipCopy.TeRadiusLabel != "TE tip radius")
            throw new InvalidOperationException("COPY-937 mismatch");
        string oob = TipCopy.RadiusOutOfBounds(500.0, 48.2);
        if (oob != "Tip radius 500.0 mm exceeds feasible envelope (max 48.20 mm at current tip chord and span). Use 48.20 mm")
            throw new InvalidOperationException("COPY-938 mismatch: " + oob);
        if (TipCopy.VerticalHandleRequired != "Vertical tip tangent requires a nonzero handle length along the tip edge.")
            throw new InvalidOperationException("COPY-939 mismatch");
        if (TipCopy.CapLabel != "3D tip cap" || TipCopy.CapFlat != "Flat (open)" || TipCopy.CapRounded != "Rounded" || TipCopy.CapRatioLabel != "Cap ratio")
            throw new InvalidOperationException("COPY-940 mismatch");
        if (!string.Equals(TipCopy.AnalysisCapNotModelled, Labels.RoundedTipCapNotice, StringComparison.Ordinal))
            throw new InvalidOperationException("COPY-941 single-source mismatch");
        string spanLine = TipCopy.DualSpanDisclosure(915.0, 917.3);
        if (spanLine != "Projected loft span: 915.0 mm · Overall span (with cap): 917.3 mm")
            throw new InvalidOperationException("COPY-942 mismatch: " + spanLine);

        string allCopy = string.Join(" ", [
            TipCopy.GroupHeading, TipCopy.LeTangentLabel, TipCopy.TeTangentLabel, TipCopy.TangentFree, TipCopy.TangentVertical,
            TipCopy.LeRadiusLabel, TipCopy.TeRadiusLabel, oob, TipCopy.VerticalHandleRequired, TipCopy.CapLabel,
            TipCopy.CapFlat, TipCopy.CapRounded, TipCopy.CapRatioLabel, TipCopy.CapRatioHelp, TipCopy.AnalysisCapNotModelled,
            spanLine, TipCopy.OverallSpanLabel, TipCopy.VerticalTipHandleNote
        ]);
        foreach (string banned in BannedLab01Words)
        {
            if (Regex.IsMatch(allCopy, @"\b" + Regex.Escape(banned) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                throw new InvalidOperationException($"LAB-01 banned superlative '{banned}' found in TipCopy");
        }
    }

    private static void CheckPropertiesPaneTipGroupAndRepair()
    {
        WithShell((controller, host, window) =>
        {
            controller.Select(new Selection.Foil());
            Settle(window);

            // Verify Structure B Tip group renders across Light, Dark, and HighContrast with DESIGN.md brushes.
            var themes = new (ThemeVariant Variant, string Ink, string Muted)[]
            {
                (ThemeVariant.Light, "#1b2929", "#526362"),
                (ThemeVariant.Dark, "#ebf3f0", "#b2c4bf"),
                (NativeReviewThemes.HighContrast, "#ffffff", "#ffffff")
            };
            foreach (var (variant, expectedInk, expectedMuted) in themes)
            {
                window.RequestedThemeVariant = variant;
                Settle(window);
                var tipGroup = Need<Expander>(host.Properties, "Group_tip");
                if (!tipGroup.IsExpanded) throw new InvalidOperationException($"{variant.Key}: Group_tip not expanded");
                var title = Need<TextBlock>(host.Properties, "GroupTitle_tip");
                if (title.Text != TipCopy.GroupHeading) throw new InvalidOperationException($"{variant.Key}: title '{title.Text}'");
                var chevron = Need<Avalonia.Controls.Shapes.Path>(host.Properties, "GroupChevron_tip");
                if (chevron.Stroke is not ISolidColorBrush stroke || stroke.Color != Color.Parse(expectedMuted))
                    throw new InvalidOperationException($"{variant.Key}: chevron brush mismatch {(chevron.Stroke as ISolidColorBrush)?.Color}");
                var capCombo = Need<ComboBox>(host.Properties, "TipCapControl");
                if (!capCombo.IsEffectivelyVisible || !capCombo.IsEffectivelyEnabled)
                    throw new InvalidOperationException($"{variant.Key}: TipCapControl not visible/enabled");
                if (title.Foreground is not ISolidColorBrush titleBrush || titleBrush.Color != Color.Parse(expectedInk))
                    throw new InvalidOperationException($"{variant.Key}: title ink mismatch {(title.Foreground as ISolidColorBrush)?.Color}");
            }
            window.RequestedThemeVariant = ThemeVariant.Light;
            Settle(window);

            // Enable vertical tip tangent on LE & TE.
            var leTan = Pump(controller.ApplyTipTangentAsync("leading", true));
            if (leTan is not CommitOutcome.Committed) throw new InvalidOperationException("LE vertical tangent refused: " + leTan);
            var teTan = Pump(controller.ApplyTipTangentAsync("trailing", true));
            if (teTan is not CommitOutcome.Committed) throw new InvalidOperationException("TE vertical tangent refused: " + teTan);
            Settle(window);

            var leInput = Need<TextBox>(host.Properties, "TipLeRadiusInput");
            if (!leInput.IsEnabled || !leInput.IsVisible)
                throw new InvalidOperationException("TipLeRadiusInput not enabled/visible when LE tip tangent is Vertical (G1)");

            // Enter infeasible radius -> DSL-TIP-RADIUS-BOUNDS -> DR-CELL-3 "Use <max> mm" repair link.
            leInput.Focus();
            leInput.Text = "500.0";
            leInput.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = leInput, Key = Key.Enter });
            Pump(host.Properties.Pending);
            Settle(window);

            var msg = Need<TextBlock>(host.Properties, "Message_tip_le_radius");
            if (!msg.Text!.Contains("exceeds feasible envelope", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected COPY-938 refusal message, got: " + msg.Text);
            var useLink = Need<HyperlinkButton>(host.Properties, "UseLimit_tip_le_radius");
            if (!useLink.IsVisible || useLink.Content?.ToString()?.StartsWith("Use ", StringComparison.Ordinal) != true)
                throw new InvalidOperationException($"DR-CELL-3 UseLink missing or wrong content: visible={useLink.IsVisible}, content='{useLink.Content}'");

            // One-click repair link commits truncated max feasible radius cleanly.
            useLink.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, useLink));
            Pump(host.Properties.Pending);
            Settle(window);

            if (useLink.IsVisible)
                throw new InvalidOperationException("DR-CELL-3 UseLink stayed visible after one-click repair");
            if (controller.Inspection?.Geometry.Status != GeometryStatus.Certified)
                throw new InvalidOperationException("Geometry not Certified after one-click radius repair");

            // Switch 3D tip cap to Rounded and verify Cap ratio + Overall span (COPY-940, COPY-942).
            var capResult = Pump(controller.ApplyTipCapAsync("rounded", 1.0));
            if (capResult is not CommitOutcome.Committed) throw new InvalidOperationException("SetTipCap rounded failed: " + capResult);
            Settle(window);

            var ratioBox = Need<TextBox>(host.Properties, "TipCapRatioInput");
            if (!ratioBox.IsEnabled || !ratioBox.IsVisible || ratioBox.Text != "1.00")
                throw new InvalidOperationException($"TipCapRatioInput unexpected: enabled={ratioBox.IsEnabled}, text='{ratioBox.Text}'");
            var spanDesc = Need<TextBlock>(host.Properties, "Description_tip_overall_span");
            if (!spanDesc.IsVisible || !spanDesc.Text!.Contains("Projected loft span:", StringComparison.Ordinal) ||
                !spanDesc.Text.Contains("Overall span (with cap):", StringComparison.Ordinal))
                throw new InvalidOperationException("COPY-942 dual span disclosure missing: " + spanDesc.Text);
        });
    }

    private static void CheckVerticalTipHandleAngleLocked()
    {
        WithShell((controller, host, window) =>
        {
            var outcome = Pump(controller.ApplyTipTangentAsync("leading", true));
            if (outcome is not CommitOutcome.Committed) throw new InvalidOperationException("SetTipTangent failed");
            Settle(window);

            var leading = controller.Planform!.Leading;
            var tipHandle = leading.Points[^2];
            var tipEnd = leading.Points[^1];
            if (tipHandle.Role != PointRole.TipHandle || tipHandle.Freedom != PointFreedom.ValueOnly || tipEnd.Kind != TangentKind.Vertical)
                throw new InvalidOperationException($"Unexpected vertical tip point model: handle={tipHandle.Role}/{tipHandle.Freedom}, end={tipEnd.Kind}");

            // 1. Selecting TipHandle MUST lock h:angle as a read-only Fact (RowState.Locked) and keep HandleAngleInput disabled.
            controller.Select(new Selection.Points([new PointRef("leading", tipHandle.Id)]));
            Settle(window);
            var handleModel = host.Properties.ShownModel!;
            var handleAngleRow = handleModel.Groups.SelectMany(g => g.Rows).First(r => r.Key == "h:angle");
            if (handleAngleRow.Kind != RowKind.Fact || handleAngleRow.State != RowState.Locked)
                throw new InvalidOperationException($"Vertical TipHandle h:angle must be locked Fact, got {handleAngleRow.Kind}/{handleAngleRow.State}");
            var angleBox = Need<TextBox>(host.Properties, "HandleAngleInput");
            if (angleBox.IsEnabled || angleBox.IsVisible)
                throw new InvalidOperationException("HandleAngleInput must be disabled & hidden when TipHandle is vertical");
            var lengthBox = Need<TextBox>(host.Properties, "HandleLengthInput");
            if (!lengthBox.IsEnabled || !lengthBox.IsVisible)
                throw new InvalidOperationException("HandleLengthInput must remain editable on vertical TipHandle");

            // 2. Selecting TipEnd MUST also lock h:angle as a read-only Fact (RowState.Locked).
            controller.Select(new Selection.Points([new PointRef("leading", tipEnd.Id)]));
            Settle(window);
            var endModel = host.Properties.ShownModel!;
            var endAngleRow = endModel.Groups.SelectMany(g => g.Rows).First(r => r.Key == "h:angle");
            if (endAngleRow.Kind != RowKind.Fact || endAngleRow.State != RowState.Locked)
                throw new InvalidOperationException($"Vertical TipEnd h:angle must be locked Fact, got {endAngleRow.Kind}/{endAngleRow.State}");
            if (angleBox.IsEnabled || angleBox.IsVisible)
                throw new InvalidOperationException("HandleAngleInput must stay disabled & hidden on vertical TipEnd");
        });
    }

    private static void CheckPlanCanvasVerticalTip()
    {
        WithShell((controller, host, window) =>
        {
            var outcome = Pump(controller.ApplyTipTangentAsync("leading", true));
            if (outcome is not CommitOutcome.Committed) throw new InvalidOperationException("SetTipTangent failed");
            Settle(window);

            var canvas = host.GetVisualDescendants().OfType<PlanCanvas>().First();
            Settle(window);
            var leading = controller.Planform!.Leading;
            var tipHandle = leading.Points[^2];
            var tipEnd = leading.Points[^1];

            // Vertical guide tick along y = half_span.
            var guides = canvas.VerticalTipGuides;
            if (guides.Count < 1) throw new InvalidOperationException("Expected at least 1 vertical tip guide on PlanCanvas");
            double tipX = canvas.ScreenPoint(tipEnd).X;
            if (Math.Abs(guides[0].Start.X - tipX) > 1.0 || Math.Abs(guides[0].End.X - tipX) > 1.0 ||
                Math.Abs(guides[0].End.Y - guides[0].Start.Y) < 10.0)
                throw new InvalidOperationException($"Vertical guide not aligned on y = half_span: start={guides[0].Start}, end={guides[0].End}, tipX={tipX:F2}");

            // Focus TipHandle and verify live R = <mm> tracing readout.
            var handleRef = new PointRef("leading", tipHandle.Id);
            canvas.SelectPoint(handleRef, false, false);
            canvas.FocusPoint(handleRef);
            Settle(window);
            if (!canvas.TracingText.Contains("R =", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected live R = <mm> in PlanTracingStrip, got: " + canvas.TracingText);

            // Simulate pointer drag with both span (horizontal) and aft (vertical) delta; ValueOnly must lock span == half_span.
            double beforeSpan = tipHandle.SpanMeters;
            double beforeOrd = tipHandle.Ordinate;
            var startPt = canvas.ScreenPoint(tipHandle);
            var draggedPt = new Point(startPt.X - 30, startPt.Y - 12);
            var pointer = new Avalonia.Input.Pointer(1, PointerType.Mouse, true);
            var leftProps = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, canvas, startPt, 1, leftProps, KeyModifiers.None, 1));
            var moveProps = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
            canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, canvas, draggedPt, 2, moveProps, KeyModifiers.None));
            controller.FlushGestureFrame();
            var relProps = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, canvas, draggedPt, 3, relProps, KeyModifiers.None, MouseButton.Left));
            WaitIdle(controller, window);

            var afterHandle = controller.Planform!.Leading.Points[^2];
            if (Math.Abs(afterHandle.SpanMeters - beforeSpan) > 1e-9 || Math.Abs(afterHandle.Eta - 1.0) > 1e-12)
                throw new InvalidOperationException($"Vertical TipHandle drifted off y = half_span: eta={afterHandle.Eta}, span={afterHandle.SpanMeters}");
            if (Math.Abs(afterHandle.Ordinate - beforeOrd) < 1e-4)
                throw new InvalidOperationException($"Vertical TipHandle did not move along x-axis: before={beforeOrd}, after={afterHandle.Ordinate}");
        });
    }

    private static void CheckSurfaceRendererRoundedAndWireframe()
    {
        using var controller = new WorkbenchController();
        Pump(controller.OpenExampleAsync());
        byte[] flatBytes = Encoding.UTF8.GetBytes(controller.AcceptedSource);
        var flatSurface = Placement.Surface(flatBytes, "accepted", 0, CancellationToken.None);

        if (Pump(controller.ApplyTipTangentAsync("leading", true)) is not CommitOutcome.Committed)
            throw new InvalidOperationException("LE vertical tangent failed");
        if (Pump(controller.ApplyTipCapAsync("rounded", 1.0)) is not CommitOutcome.Committed)
            throw new InvalidOperationException("SetTipCap rounded failed");

        byte[] roundedBytes = Encoding.UTF8.GetBytes(controller.AcceptedSource);
        var roundedSurface = Placement.Surface(roundedBytes, "accepted", 0, CancellationToken.None);
        if (roundedSurface.TipMode != "rounded")
            throw new InvalidOperationException("SurfaceView.TipMode was not 'rounded'");

        // 1. Rounded tip cap adds real 3D cap positions and triangles in SurfaceRenderer.Topology.
        var flatCounts = SurfaceMesh.MeshCounts(flatSurface);
        var roundedCounts = SurfaceMesh.MeshCounts(roundedSurface);
        if (roundedCounts.Positions <= flatCounts.Positions || roundedCounts.Triangles <= flatCounts.Triangles)
            throw new InvalidOperationException($"Rounded tip cap did not add mesh geometry: flat={flatCounts}, rounded={roundedCounts}");

        // 2. Open-TE rounded tip cap also builds cleanly without zero-area errors.
        string openTeText = controller.AcceptedSource.Replace("closure closed", "closure open", StringComparison.Ordinal);
        if (openTeText != controller.AcceptedSource)
        {
            var openTeSurface = Placement.Surface(Encoding.UTF8.GetBytes(openTeText), "accepted", 0, CancellationToken.None);
            var openTeCounts = SurfaceMesh.MeshCounts(openTeSurface);
            if (openTeCounts.Triangles <= flatCounts.Triangles)
                throw new InvalidOperationException($"Open-TE rounded cap did not emit cap triangles: {openTeCounts}");
        }

        // 3. Wireframe rib fallback on SinPi grid yields >= 5 interior ribs and renders mute rib strokes.
        var wireRows = SurfaceMesh.WireframeRibRows(roundedSurface);
        if (wireRows.Count < 5)
            throw new InvalidOperationException($"SinPi wireframe rib fallback produced too few interior ribs: {wireRows.Count}");

        var camera = ViewCamera.Named(NamedCamera.Iso,
            new Point3(roundedSurface.MinimumX, roundedSurface.MinimumY, roundedSurface.MinimumZ),
            new Point3(roundedSurface.MaximumX, roundedSurface.MaximumY, roundedSurface.MaximumZ),
            new Size(640, 480));
        var palette = new SurfacePalette(SKColors.Gray, SKColors.LightGray, SKColors.White, SKColors.Cyan, SKColors.Yellow, SKColors.Silver);
        var frame = SurfaceMesh.Build(roundedSurface, camera, new Size(640, 480), DisplayMode.Wireframe, null, palette, null);
        bool hasMutedRibs = frame.Batches.Any(b => b.Colors is null && b.StrokeColor == SKColors.Silver && b.Points.Length >= 20);
        if (!hasMutedRibs)
            throw new InvalidOperationException("Wireframe MeshFrame missing SinPi interior rib strokes");
    }

    private static void CheckEdgeHullCrossing25DegSwept()
    {
        // Build a 25 deg swept wingfoil with vertical-tangent rounded tips where leading(1) > trailing(0).
        // half_span = 0.45 m, tan(25 deg) * 0.45 = 0.2098 m sweep, root chord = 0.12 m, tip chord = 0.025 m:
        // leading(0) = 0, trailing(0) = 0.12, leading(1) = 0.21 > 0.12 = trailing(0)!
        const string swept42 = """
foildsl "4.2"
foil "Swept25Rounded" {
  units mm
  half_span 450 mm
  evaluator "cfdw-cv" "2"
  symmetry mirror_y
  planform {
    leading cv {
      degree 3
      knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1]
      points [(0, 0), (0.25, 0), (0.85, 165), (1, 192), (1, 210)]
      ids ["le-0", "le-1", "le-2", "le-3", "le-4"]
      tangents { "le-4" vertical }
    }
    trailing cv {
      degree 3
      knots [0, 0, 0, 0, 0.7, 1, 1, 1, 1]
      points [(0, 120), (0.25, 120), (0.85, 225), (1, 246), (1, 235)]
      ids ["te-0", "te-1", "te-2", "te-3", "te-4"]
      tangents { "te-4" vertical }
    }
  }
  dihedral cv { degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.33, 0), (0.67, 0), (1, 0)] ids ["d-0", "d-1", "d-2", "d-3"] }
  twist cv { degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0), (0.33, 0), (0.67, 0), (1, 0)] ids ["tw-0", "tw-1", "tw-2", "tw-3"] }
  thickness cv { degree 3 knots [0, 0, 0, 0, 1, 1, 1, 1] points [(0, 0.12), (0.33, 0.12), (0.67, 0.10), (1, 0.092)] ids ["th-0", "th-1", "th-2", "th-3"] }
  profiles {
    profile "section-a" {
      upper cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, 0.025), (0.15, 0.055), (0.35, 0.065), (0.55, 0.05), (0.75, 0.03), (0.9, 0.01), (1, 0)] ids ["u-0", "u-1", "u-2", "u-3", "u-4", "u-5", "u-6", "u-7"] }
      lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.3333333333333333, 0.6666666666666666, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.025), (0.15, -0.055), (0.35, -0.065), (0.55, -0.05), (0.75, -0.03), (0.9, -0.01), (1, 0)] ids ["l-0", "l-1", "l-2", "l-3", "l-4", "l-5", "l-6", "l-7"] }
    }
  }
  sections { at root profile "section-a" at tip profile "section-a" }
  tip rounded 1
}
""";
        byte[] sweptBytes = Encoding.UTF8.GetBytes(swept42);
        var parsed = FoilSource.Parse(sweptBytes);
        if (!parsed.IsParsed)
            throw new InvalidOperationException("Swept 4.2 fixture failed parse: " + string.Join(", ", parsed.Diagnostics.Select(d => d.Code + ":" + d.Reason)));
        var assessed = CfdWorkbench.Core.Geometry.Assess(parsed);
        if (assessed.Status != GeometryStatus.Certified)
            throw new InvalidOperationException("Swept 4.2 fixture not certified: " + assessed.Code + " " + assessed.Reason);

        var plan = Planform.View(sweptBytes, "accepted", 0);
        // Sanity check: global rail hulls really do overlap (leading tip 0.21 m > trailing root 0.12 m).
        if (!(plan.Leading.Points[^1].Ordinate > plan.Trailing.Points[0].Ordinate))
            throw new InvalidOperationException("Fixture did not satisfy leading(1) > trailing(0)");

        if (WorkbenchController.EdgeHullCrossing(plan, "leading") is not null)
            throw new InvalidOperationException("EdgeHullCrossing gave false alarm on 25 deg swept rounded tip wing (leading)");
        if (WorkbenchController.EdgeHullCrossing(plan, "trailing") is not null)
            throw new InvalidOperationException("EdgeHullCrossing gave false alarm on 25 deg swept rounded tip wing (trailing)");

        // Negative check: genuinely crossing rails MUST be detected.
        string crossing42 = swept42.Replace("(0.85, 225)", "(0.85, 40)", StringComparison.Ordinal);
        var crossingPlan = Planform.View(Encoding.UTF8.GetBytes(crossing42), "preview", 1);
        if (WorkbenchController.EdgeHullCrossing(crossingPlan, "leading") is null)
            throw new InvalidOperationException("EdgeHullCrossing missed a genuine mid-span crossing");
    }

    private static void CheckAnalysisCopy941BothSurfaces()
    {
        WithShell((controller, host, window) =>
        {
            if (Pump(controller.ApplyTipCapAsync("rounded", 1.0)) is not CommitOutcome.Committed)
                throw new InvalidOperationException("SetTipCap rounded failed");
            controller.ToggleAnalysis();
            Pump(controller.EvaluateAnalysisAsync(controller.AnalysisOperatingPoint, controller.AnalysisWater));
            Settle(window);

            // 1. Controller AnalysisView Labels group carries COPY-941 via DeriveFeed -> ProjectionContext.RoundedTipCap.
            var labelsGroup = controller.AnalysisView.Groups.FirstOrDefault(g => g.Title == "Labels")
                ?? throw new InvalidOperationException("AnalysisView missing Labels group");
            var tipRow = labelsGroup.Rows.FirstOrDefault(r => r.Label == "Tip cap");
            if (tipRow is null || tipRow.Value != TipCopy.AnalysisCapNotModelled)
                throw new InvalidOperationException("AnalysisView Labels missing COPY-941: " + tipRow?.Value);

            // 2. Realized PropertiesPane in Analysis displays COPY-941.
            var propTexts = host.Properties.GetVisualDescendants().OfType<TextBlock>().Select(tb => tb.Text ?? "").ToList();
            if (!propTexts.Any(t => t.Contains(TipCopy.AnalysisCapNotModelled, StringComparison.Ordinal)))
                throw new InvalidOperationException("PropertiesPane in Analysis did not render COPY-941");

            // 3. Realized AnalysisPanel Provenance tab displays COPY-941.
            var panel = host.GetVisualDescendants().OfType<AnalysisPanel>().First();
            panel.Bind(controller);
            panel.PanelTabs.SelectedIndex = 3; // Provenance tab hosts Conditions / Labels / Provenance tables.
            Settle(window);
            var panelTexts = panel.GetVisualDescendants().OfType<TextBlock>().Select(tb => tb.Text ?? "").ToList();
            if (!panelTexts.Any(t => t.Contains(TipCopy.AnalysisCapNotModelled, StringComparison.Ordinal)))
                throw new InvalidOperationException("AnalysisPanel did not render COPY-941");
        });
    }

    private static void CheckExportDialogTipClosure()
    {
        using var controller = new WorkbenchController();
        Pump(controller.OpenExampleAsync());
        if (Pump(controller.ApplyTipCapAsync("rounded", 1.0)) is not CommitOutcome.Committed)
            throw new InvalidOperationException("SetTipCap rounded failed");

        var source = controller.ExportSnapshot() ?? throw new InvalidOperationException("ExportSnapshot null");
        var session = new ExportSession(source);
        var dialog = new ExportDialog(session);
        try
        {
            dialog.Show();
            Settle(dialog);
            var stlItem = Need<ListBoxItem>(dialog, "StlFormatItem");
            stlItem.IsSelected = true;
            Pump(dialog.Preparation);
            Settle(dialog);

            var closureVal = Need<TextBlock>(dialog, "TipClosureValue");
            if (!closureVal.Text!.Contains("Rounded 3D cap (ratio 1.00)", StringComparison.Ordinal) ||
                !closureVal.Text.Contains("Overall span", StringComparison.Ordinal))
                throw new InvalidOperationException("ExportDialog TipClosureValue unexpected: " + closureVal.Text);
            if (dialog.TipClosureSummary != closureVal.Text)
                throw new InvalidOperationException("ExportDialog TipClosureSummary mismatch");
        }
        finally { dialog.Close(); }
    }

    private static void CheckTelemetryZeroPii()
    {
        using var controller = new WorkbenchController();
        Pump(controller.OpenExampleAsync());
        string renamed = controller.AcceptedSource.Replace("foil \"", "foil \"SecretFoilNameXYZ_", StringComparison.Ordinal);
        Pump(controller.OpenFoilAsync(Encoding.UTF8.GetBytes(renamed), "secret.foil"));

        _ = Pump(controller.ApplyTipTangentAsync("leading", true));
        _ = Pump(controller.ApplyTipRadiusAsync("leading", 0.500)); // refused DSL-TIP-RADIUS-BOUNDS
        _ = Pump(controller.ApplyTipRadiusAsync("leading", 0.015)); // ok
        _ = Pump(controller.ApplyTipCapAsync("rounded", 1.0));

        var tipEvents = controller.ActiveSession.ReadLocalEvents().Where(e => e.Operation == "tip.edit").ToList();
        if (tipEvents.Count < 4)
            throw new InvalidOperationException($"Expected >= 4 tip.edit telemetry events, got {tipEvents.Count}");
        string serialized = JsonSerializer.Serialize(tipEvents);
        string syntheticUser = string.Concat("Us", "ers");
        if (serialized.Contains("SecretFoilNameXYZ", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains("secret.foil", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains("cv-", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains(syntheticUser, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Telemetry ring leaked PII/name/ID: " + serialized);
    }

    private static void WithShell(Action<WorkbenchController, ShellHost, Window> body)
    {
        using var controller = new WorkbenchController();
        Pump(controller.OpenExampleAsync());
        var host = new ShellHost(controller);
        var window = new Window { Width = 1280, Height = 800, Content = host };
        try
        {
            window.Show();
            Settle(window);
            body(controller, host, window);
        }
        finally
        {
            window.Close();
        }
    }

    private static void WaitIdle(WorkbenchController controller, Window window)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (controller.Gesture != GestureState.Idle && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            Thread.Sleep(5);
        }
        Settle(window);
    }

    private static void Pump(Task task, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(task))] string what = "") =>
        DesktopChecks.PumpToCompletion(task, what, TimeSpan.FromSeconds(15));

    private static T Pump<T>(Task<T> task, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(task))] string what = "")
    {
        DesktopChecks.PumpToCompletion(task, what, TimeSpan.FromSeconds(15));
        return task.Result;
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name)
        ?? root.GetLogicalDescendants().OfType<T>().FirstOrDefault(c => c.Name == name)
        ?? root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name)
        ?? throw new InvalidOperationException($"Control '{name}' ({typeof(T).Name}) not found");
}
