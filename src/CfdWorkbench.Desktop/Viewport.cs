using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

/// <summary>Draws only certified physical point samples. Lines guide the eye; their interpolation error is unassessed.</summary>
public sealed class Viewport : Grid
{
    private DisplayFrame? frame;
    private IReadOnlyList<ViewportSemantic> semantics = [];
    private readonly ViewportDrawing drawing;
    private readonly StackPanel annotationPanel = new() { Spacing = 2 };
    private readonly ScrollViewer annotationScroll;
    private readonly Dictionary<string, TextBlock> semanticControls = [];
    public Viewport()
    {
        drawing = new ViewportDrawing(this);
        annotationScroll = new ScrollViewer { Content = annotationPanel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(8) };
        Children.Add(drawing);
        Children.Add(annotationScroll);
    }
    public IBrush? BackgroundBrush { get; set; }
    public IBrush? GridBrush { get; set; }
    public IBrush? FoilBrush { get; set; }
    public IBrush? StationBrush { get; set; }
    public bool SectionMode { get; set; }
    private double annotationWidth;
    public double AnnotationWidth { get => annotationWidth; set { annotationWidth = value; annotationScroll.Width = value; drawing.InvalidateVisual(); } }
    public double AnnotationHeight { get => annotationScroll.Height; set => annotationScroll.Height = value; }
    public IReadOnlyList<TextBlock> SemanticControls => annotationPanel.Children.OfType<TextBlock>().ToArray();

    public DisplayFrame? Frame
    {
        get => frame;
        set { frame = value; drawing.InvalidateVisual(); }
    }

    public IReadOnlyList<ViewportSemantic> Semantics
    {
        get => semantics;
        set
        {
            semantics = value;
            var desired = new List<TextBlock>();
            foreach (var item in semantics)
            {
                if (!semanticControls.TryGetValue(item.Id, out var label))
                {
                    label = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis };
                    semanticControls.Add(item.Id, label);
                }
                label.Text = item.Name;
                label.Foreground = StationBrush;
                AutomationProperties.SetAutomationId(label, item.Id);
                AutomationProperties.SetItemStatus(label, item.Status);
                AutomationProperties.SetControlTypeOverride(label, AutomationControlType.ListItem);
                desired.Add(label);
            }
            if (!annotationPanel.Children.SequenceEqual(desired))
            {
                annotationPanel.Children.Clear();
                foreach (var label in desired) annotationPanel.Children.Add(label);
            }
            foreach (var stale in semanticControls.Keys.Except(semantics.Select(item => item.Id)).ToArray())
                semanticControls.Remove(stale);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ViewportPeer(this);

    private sealed class ViewportDrawing(Viewport viewport) : Control
    {
      public override void Render(DrawingContext context)
      {
        var frame = viewport.frame;
        var BackgroundBrush = viewport.BackgroundBrush;
        var GridBrush = viewport.GridBrush;
        var FoilBrush = viewport.FoilBrush;
        var StationBrush = viewport.StationBrush;
        var SectionMode = viewport.SectionMode;
        if (BackgroundBrush is not null) context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));
        if (frame?.Points.Count != 15 || FoilBrush is null || StationBrush is null) return;
        // An orthonormal camera basis and one shared pixel-per-metre scale preserve physical XYZ proportions.
        // Section mode uses physical X/Z at the selected interior eta, also with one shared scale.
        var selected = SectionMode ? frame.Points.Skip(5).Take(5).ToArray() : frame.Points.ToArray();
        (double Horizontal, double Vertical) Project(DisplayPoint point) => SectionMode
            ? (point.X, point.Z)
            : ((point.X - point.Y) / Math.Sqrt(2), (2 * point.Z - point.X - point.Y) / Math.Sqrt(6));
        var projected = selected.Select(Project).ToArray();
        double minH = projected.Min(p => p.Horizontal), maxH = projected.Max(p => p.Horizontal);
        double minV = projected.Min(p => p.Vertical), maxV = projected.Max(p => p.Vertical);
        double plotWidth = Math.Max(1, Bounds.Width - viewport.AnnotationWidth - 12);
        double availableWidth = Math.Max(1, plotWidth - 48), availableHeight = Math.Max(1, Bounds.Height - 48);
        double scale = Math.Min(availableWidth / Math.Max(1e-9, maxH - minH),
            availableHeight / Math.Max(1e-9, maxV - minV));
        Point Map(DisplayPoint point)
        {
            var (horizontal, vertical) = Project(point);
            return new(plotWidth / 2 + (horizontal - (minH + maxH) / 2) * scale,
                Bounds.Height / 2 - (vertical - (minV + maxV) / 2) * scale);
        }
        if (GridBrush is not null)
        {
            double y = Bounds.Height / 2;
            context.DrawLine(new Pen(GridBrush, 1), new Point(24, y), new Point(Bounds.Width - 24, y));
            if (!SectionMode)
            {
                var origin = new Point(48, Bounds.Height - 46);
                var axisPen = new Pen(StationBrush, 2);
                context.DrawLine(axisPen, origin, new Point(origin.X + 22, origin.Y + 13));
                context.DrawLine(axisPen, origin, new Point(origin.X - 22, origin.Y + 13));
                context.DrawLine(axisPen, origin, new Point(origin.X, origin.Y - 27));
            }
        }
        var foilPen = new Pen(FoilBrush, 2);
        for (int station = 0; station < selected.Length / 5; station++)
        {
            int baseIndex = station * 5;
            // Upper and lower trailing-edge samples remain independent; there is no invented closure.
            foreach (var (from, to) in new[] { (0, 1), (1, 2), (0, 3), (3, 4) })
                context.DrawLine(foilPen, Map(selected[baseIndex + from]), Map(selected[baseIndex + to]));
        }
        if (!SectionMode)
        {
            for (int station = 0; station < 2; station++)
                for (int sample = 0; sample < 5; sample++)
                    context.DrawLine(new Pen(FoilBrush, 1), Map(selected[station * 5 + sample]),
                        Map(selected[(station + 1) * 5 + sample]));
        }
        foreach (var point in selected)
        {
            var center = Map(point);
            double radius = point.NormalizedX is 0 or 1 ? 4 : 3;
            context.DrawEllipse(StationBrush, null, center, radius, radius);
        }
      }
    }
}

public sealed record ViewportSemantic(string Id, string Name, string Status);

public static class ViewportSemantics
{
    public static IReadOnlyList<ViewportSemantic> FromInspection(AcceptedInspection inspection,
        string? selectedRail = null, string? selectedId = null)
    {
        var result = new List<ViewportSemantic>();
        int stationIndex = 0;
        foreach (var station in inspection.Authored.Assignments)
        {
            result.Add(new($"station-{stationIndex++}",
                $"Accepted station η {station.Eta.ToString("G3", CultureInfo.InvariantCulture)} normalized; " +
                $"span {station.SpanMeters.ToString("G6", CultureInfo.InvariantCulture)} metres; " +
                $"profile {station.ProfileName}; identity {station.ProfileIdentity}", "accepted"));
        }
        var allControls = inspection.Authored.Rails.SelectMany(rail => rail.Controls.Select(control => (rail, control))).ToArray();
        var controls = new List<(AuthoredRail rail, AuthoredControl control)>();
        foreach (var featured in new[] {
            allControls.FirstOrDefault(item => item.rail.Name == selectedRail && item.control.Id == selectedId),
            allControls.FirstOrDefault(item => item.control.Editable),
            allControls.FirstOrDefault(item => !item.control.Editable) })
            if (featured.rail is not null && !controls.Contains(featured)) controls.Add(featured);
        controls.AddRange(allControls.Where(item => !controls.Contains(item)));
        foreach (var (rail, control) in controls)
                result.Add(new($"cv-{rail.Name}-{control.Id}",
                    $"{rail.Name} control vertex {control.Id}; η {control.Eta.ToString("G3", CultureInfo.InvariantCulture)} normalized; " +
                    $"aft position {control.OrdinateSi.ToString("G6", CultureInfo.InvariantCulture)} metres; " +
                    $"authored unit {rail.SourceUnit}; " +
                    (control.Editable ? "editable" : "locked: " + string.Join(", ", control.ApplicableLocks)),
                    control.Editable ? "editable" : "locked"));
        return result;
    }

    public static IReadOnlyList<ViewportSemantic> FromSection(DisplayFrame? frame) => frame is null ? [] :
        frame.Points.Skip(5).Take(5).Select((point, index) => new ViewportSemantic(
            $"section-{index}",
            $"Section η {frame.InteriorEta.ToString("G3", CultureInfo.InvariantCulture)} normalized; " +
            $"x/c {point.NormalizedX.ToString("G3", CultureInfo.InvariantCulture)} normalized; " +
            $"{(point.Upper ? "upper" : "lower")}; x {point.X.ToString("G6", CultureInfo.InvariantCulture)} metres; " +
            $"z {point.Z.ToString("G6", CultureInfo.InvariantCulture)} metres; point enclosure certified, segment error not assessed",
            "certified point enclosure")).ToArray();
}

internal sealed class ViewportPeer(Viewport owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}
