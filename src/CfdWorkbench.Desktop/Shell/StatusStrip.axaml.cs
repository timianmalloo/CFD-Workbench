using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop.Shell;

/// <summary>The strip's one action (DR-STATUS-1): Try again, or Show (design §5.2), with what it runs.</summary>
public sealed record StripAction(string Label, Action Run);

/// <summary>
/// The status strip along the bottom of the shell (DR-STATUS-1; DESIGN.md §4 Status strip). It shows the last report only:
/// each report replaces the one before. <see cref="ShellHost.Report"/> is the one way in; the strip itself keeps no rule
/// about which report wins (STATUS-CLOBBER is the shell's sequence check).
/// </summary>
public partial class StatusStrip : UserControl
{
    // 12 px icons by kind (docs/mockups/status-bar.html V2): a check, a triangle, a circled cross.
    private const string IconInfo = "M2,6.5 L5,9 L10,3";
    internal const string IconWarning = "M6,1 L11.5,11 L0.5,11 Z M6,4.5 L6,7.5 M6,9 L6,9.8";
    private const string IconError = "M1,6 A5,5 0 1 1 11,6 A5,5 0 1 1 1,6 Z M4,4 L8,8 M8,4 L4,8";

    // The scaled copies of the prop tokens the strip draws with (DN-5), as the Properties pane does.
    private static readonly string[] ScaledTokens = ["PropFontSize", "PropLineHeight"];

    public StatusStrip()
    {
        InitializeComponent();
        SizeChanged += (_, _) => FitItems();
        // The one action slot: whatever the shown report carries runs here (UI-DEAD-CONTROL: never a dead button).
        StatusTryAgainButton.Click += (_, _) => Action?.Run();
    }

    /// <summary>What Try again runs (the shell sets it); a report that offers Try again carries it as its action.</summary>
    public Action? TryAgain { get; set; }

    /// <summary>The action the shown report carries, or null.</summary>
    public StripAction? Action { get; private set; }

    /// <summary>The kind of the report the strip shows.</summary>
    public ReportKind Kind { get; private set; }

    /// <summary>The full text of the report the strip shows; empty until something happens (DR-STATUS-4).</summary>
    public string Text => StatusText.Text ?? "";

    /// <summary>Replaces the shown report. <paramref name="offerTryAgain"/> shows the one action after the message.</summary>
    public void Show(StatusReport report, bool offerTryAgain = false) =>
        Show(report, offerTryAgain ? new StripAction("Try again", () => TryAgain?.Invoke()) : null);

    /// <summary>Replaces the shown report; <paramref name="action"/> (Try again or Show) is the one action after it.</summary>
    public void Show(StatusReport report, StripAction? action)
    {
        ArgumentNullException.ThrowIfNull(report);
        Kind = report.Kind;
        StatusText.Text = report.Text;
        // Nothing is lost when the strip trims: the full text is the tooltip and the accessible name.
        ToolTip.SetTip(StatusText, report.Text);
        AutomationProperties.SetName(StatusText, report.Text);
        foreach (var control in new Control[] { StatusText, StatusKindIcon, StatusRail })
        {
            control.Classes.Set("warning", report.Kind == ReportKind.Warning);
            control.Classes.Set("error", report.Kind == ReportKind.Error);
        }
        StatusKindIcon.Data = Avalonia.Media.Geometry.Parse(report.Kind switch
        {
            ReportKind.Warning => IconWarning,
            ReportKind.Error => IconError,
            _ => IconInfo
        });
        StatusKindIcon.IsVisible = report.Text.Length > 0;
        Action = action;
        StatusTryAgainButton.Content = action?.Label ?? "Try again";
        AutomationProperties.SetName(StatusTryAgainButton, action?.Label);
        StatusTryAgainButton.IsVisible = action is not null;
    }

    /// <summary>Empties the strip (the recent-files list was cleared after all).</summary>
    public void Clear()
    {
        Show(new StatusReport(""));
        ToolTip.SetTip(StatusText, null);
    }

    /// <summary>The read-only items: selection, units, the estimate note and Text size (DESIGN.md §4).</summary>
    public void ShowItems(string? selection, bool foilOpen, bool estimates, double textScale, string units = "Metric")
    {
        if ((string?)UnitsButton.Content != units) UnitsButton.Content = units;
        SelectionItemText.Text = selection ?? "";
        AutomationProperties.SetName(SelectionItem, selection is null ? null : "Selection: " + selection);
        string size = string.Create(CultureInfo.InvariantCulture, $"Text {textScale * 100:0} %");
        if (TextSizeItemText.Text != size) TextSizeItemText.Text = size;
        wanted = (selection is not null, true, estimates && foilOpen, true);
        FitItems();
    }

    /// <summary>Read-only state of the selected Analysis run, in either area mode.</summary>
    public void ShowAnalysisState(RunState state)
    {
        AnalysisItemText.Text = state switch
        {
            RunState.NoResult => "Analysis: no result",
            _ => "Analysis: " + state
        };
        AutomationProperties.SetName(AnalysisItem, AnalysisItemText.Text);
        AnalysisItem.IsVisible = true;
        FitItems();
    }

    private (bool Selection, bool Units, bool Estimates, bool TextSize) wanted = (false, false, false, true);

    /// <summary>
    /// The strip is one line: when the items would crowd the message (large Text size, narrow window) they drop in the order
    /// Text size, ≈ estimates, units; the selection stays.
    /// </summary>
    private void FitItems()
    {
        SelectionItem.IsVisible = wanted.Selection;
        UnitsItem.IsVisible = wanted.Units;
        EstimateItem.IsVisible = wanted.Estimates;
        TextSizeItem.IsVisible = wanted.TextSize;
        double width = Bounds.Width;
        if (width <= 0) return;
        foreach (var item in new Control[] { TextSizeItem, EstimateItem, UnitsItem })
        {
            if (ItemsWidth() <= width / 2) break;
            item.IsVisible = false;
        }
    }

    private double ItemsWidth()
    {
        double total = 0;
        foreach (var item in StatusItems.Children.Where(child => child.IsVisible))
        {
            item.Measure(Size.Infinity);
            total += item.DesiredSize.Width;
        }
        return total;
    }

    /// <summary>DN-5: the strip's 11 px type scales with Text size, as the Properties pane's does.</summary>
    public void ApplyTextScale(double scale)
    {
        foreach (var key in ScaledTokens)
            if (Application.Current?.TryFindResource(key, out var value) == true && value is double size)
                StripRoot.Resources[key] = size * scale;
        FitItems();
    }
}

/// <summary>One step's strip report and the point it moved (the point Show selects when the step makes a crossing).</summary>
public sealed record SectionStepCopy(string Text, PointRef? Moved);

/// <summary>
/// The section steps' strip reports (design §11.4 COPY-174..176; paired copy COPY-185 and COPY-186, Ruling 60; the
/// insert report that replaces M1.1's Insert report, CTL retirement ruling 1). Every number is the step's own measurement:
/// the largest change is Core's <c>MaxChange</c> on its oracle, and point counts are read from the bytes.
/// </summary>
public static class SectionStrip
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static SectionStepCopy? Report(WorkbenchController controller, byte[] before, SectionMode mode, string station)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(mode);
        if (mode.LastReport is not { } report) return null;
        int assignment = mode.Draft.Assignment;
        var oldUpper = Sections.View(before, assignment, SurfaceSide.Upper, "accepted", 0);
        var oldLower = Sections.View(before, assignment, SurfaceSide.Lower, "accepted", 0);
        var upper = controller.SectionCurve(SurfaceSide.Upper)!;
        var lower = controller.SectionCurve(SurfaceSide.Lower)!;
        string largest = $"Largest change {Pct(report.MaxChange)} % chord.";
        string counts = oldUpper.Points.Count == oldLower.Points.Count && upper.Points.Count == lower.Points.Count
            ? $"{oldUpper.Points.Count} → {upper.Points.Count} points each"
            : $"upper {oldUpper.Points.Count} → {upper.Points.Count}, lower {oldLower.Points.Count} → {lower.Points.Count} points";
        switch (report.Kind)
        {
            case "move":
                return Moved(before, mode, station, oldUpper, oldLower, upper, lower);
            case "insert" or "insert-anchor":
            {
                var added = Added(oldUpper, upper) ?? Added(oldLower, lower);
                if (added is null) return new($"Inserted a point ({counts}). {largest}", null);
                string what = report.Kind == "insert-anchor" ? "an anchor" : "a point";
                return new($"Inserted {what} at {Pct(added.SpanMeters)} % chord: now point {added.Index + 1} on both surfaces ({counts}). {largest}", null);
            }
            case "delete":
                return new($"Deleted a point on both surfaces ({counts}). {largest}", null);
            case "set-type" when upper.Points.Count > oldUpper.Points.Count:
            {
                // COPY-185 (proposed): the point became an anchor on both surfaces.
                var anchor = upper.Points.Where(point => point.Role == PointRole.Anchor)
                    .FirstOrDefault(point => !oldUpper.Points.Any(old => old.Id == point.Id && old.Role == PointRole.Anchor));
                if (anchor is null) return new($"The point is now an anchor on both surfaces ({counts}). {largest}", null);
                return new($"Point {anchor.Index + 1} is now an anchor on both surfaces (point {anchor.Index + 1} of {upper.Points.Count}; {counts}). " +
                    $"{largest} Curvature now breaks at {Pct(anchor.SpanMeters)} % chord.", null);
            }
            case "set-type":
                return new($"The point is now a control point on both surfaces ({counts}). {largest}", null);   // COPY-175, paired
            case "set-tangent":
                return new($"Tangent changed on both surfaces. {largest}", null);
            case "fair" or "rebuild":
                return new($"Smoothed the section ({counts}). {largest}", null);
            case "replace":
            {
                if (report.Import is not { } import)
                    return new($"Section changed. {largest}", null);
                string stations = ReplaceStations(controller, import.Stations);
                string source = string.IsNullOrWhiteSpace(controller.LastReplaceName) ? "the section" : controller.LastReplaceName;
                string undo = OperatingSystem.IsMacOS() ? "⌘Z" : "Ctrl+Z";
                return new(
                    $"Replaced {stations} with {source}. Fit {ReplaceMicrons(controller, import)} µm (limit 10 µm). {undo} puts the old section back.",
                    null);
            }
            case "make-unique":
                return new($"{station} now has its own copy of the section. The other stations keep {mode.Draft.Profile}.", null);
            case "thickness":
                return new(mode.Draft.Intent == ThicknessIntent.UseSource
                    ? "Station t/c now comes from this section."
                    : "Station t/c now comes from the Thickness curve.", null);
            default:
                return new($"Section changed. {largest}", null);
        }
    }

    private static PointView? Added(CurveView before, CurveView after) =>
        after.Points.FirstOrDefault(point => before.Points.All(old => old.Id != point.Id));

    // COPY-176 (a y move on one surface) and COPY-186 (an x move, shared by both surfaces).
    private static SectionStepCopy? Moved(byte[] before, SectionMode mode, string station, CurveView oldUpper, CurveView oldLower,
        CurveView upper, CurveView lower)
    {
        var facts = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment);
        string tail = $"Own t/c {Pct(facts.OwnThickness)} %; {station} stays {Pct(facts.StationThicknessRatio)} % t/c.";
        foreach (var (side, old, now) in new[] { ("upper", oldUpper, upper), ("lower", oldLower, lower) })
        {
            foreach (var point in now.Points)
            {
                var was = old.Points.FirstOrDefault(item => item.Id == point.Id);
                if (was is null || was.SpanMeters == point.SpanMeters && was.Ordinate == point.Ordinate) continue;
                var moved = new PointRef(side, point.Id, mode.Draft.Profile);
                if (was.SpanMeters != point.SpanMeters)
                    return new($"Moved point {point.Index + 1} on both surfaces: x {Pct(was.SpanMeters)} → {Pct(point.SpanMeters)} % chord. {tail}", moved);
                double distance = Math.Abs(point.Ordinate - was.Ordinate);
                return new($"Moved {side} point {point.Index + 1} by {Pct(distance)} % chord. {tail}", moved);
            }
        }
        return null;
    }

    private static string ReplaceStations(WorkbenchController controller, IReadOnlyList<int>? stations)
    {
        if (stations is not { Count: > 0 } || controller.Inspection is not { } inspection) return "the stations";
        var names = new List<string>(stations.Count);
        foreach (int index in stations)
        {
            if ((uint)index >= (uint)inspection.Authored.Assignments.Count) continue;
            names.Add(ElevationView.StationName(index, inspection.Authored.Assignments[index].Eta));
        }
        return names.Count switch
        {
            0 => "the stations",
            1 => names[0],
            2 => names[0] + " and " + names[1],
            _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]
        };
    }

    private static string ReplaceMicrons(WorkbenchController controller, ImportReport import)
    {
        double chord = 0;
        if (import.Stations is { Count: > 0 } stations && controller.Inspection is { } inspection && controller.Section is { } mode)
        {
            foreach (int index in stations)
            {
                if ((uint)index >= (uint)inspection.Authored.Assignments.Count) continue;
                chord = Math.Max(chord, Placement.Frame(mode.Draft.Bytes, inspection.Authored.Assignments[index].Eta).ChordMeters);
            }
        }
        return (import.MaxResidual * chord * 1e6).ToString("0.00", Inv);
    }

    private static string Pct(double fraction) => (fraction * 100).ToString("0.00", Inv).Replace('-', Quantity.Minus);
}
