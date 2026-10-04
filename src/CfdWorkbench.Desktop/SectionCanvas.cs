using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public class SectionCanvas : Control
{
    public WorkbenchController? Controller { get; set; }
    public Control? CancelTarget { get; set; }
    public TextBlock? ReasonTarget { get; set; }
    public Control? ReasonContainer { get; set; }
    /// <summary>The Tracing probe's text block (§11.1); the canvas writes it on every pointer move.</summary>
    public TextBlock? ProbeTarget { get; set; }
    /// <summary>DR-NAV-1: Tab from a selected point leaves the canvas to the Properties Type value.</summary>
    public Func<bool>? TabOut { get; set; }
    /// <summary>§11.3: Return on a selected point goes to its x value.</summary>
    public Func<bool>? ValueOut { get; set; }
    public bool CurvatureVisible { get; set; } = true;
    public bool ThicknessDoubled { get; set; }
    public string ProbeText { get; private set; } = "Pointer · display";
    public int CombClippedCount { get; private set; }
    /// <summary>The comb teeth the last render drew, in canvas pixels (start on the curve, tip at the tooth end).</summary>
    public IReadOnlyList<(Point Start, Point Tip)> CombTeeth { get; private set; } = [];
    public double CombScale { get; private set; }
    /// <summary>The upper and lower curves the last section render drew, in canvas pixels.</summary>
    public IReadOnlyList<IReadOnlyList<Point>> DrawnCurves { get; private set; } = [];
    public (double X0, double X1)? CrossingInterval { get; private set; }
    private double viewMinX;
    private double viewSpan = 1;
    private double viewCenterY;
    private PointView? draggedPoint;
    private IPointer? draggedPointer;
    private Point? previewPoint;
    // The released drag, drawn until its step lands: the step applies off the UI thread (§7 Concurrency), and drawing the
    // committed draft meanwhile would snap the curve back for the length of the apply. Keyed by the draft generation it
    // was released on, so a landed (or refused) step ends it.
    private (PointView Point, Point At, string DraftId, long Generation)? releasedDrag;
    private Point pressPoint;
    private (string DraftId, long Generation, string Station, double ThicknessRatio)? probeStation;

    // The named points' delete refusals (design §11.3: ⌫ is refused on named points with the reason).
    public const string NoseNotDeleted = "The nose is always an anchor. It can't be deleted.";
    public const string TrailingNotDeleted = "The trailing-edge point is always an anchor. It can't be deleted.";
    public const string UnsavedChanges = "This section has unsaved changes. Cancel discards them; Finish keeps them.";   // COPY-119

    public void Fit()
    {
        viewMinX = viewCenterY = 0;
        viewSpan = 1;
        InvalidateVisual();
    }

    /// <summary>§11.2: frames the selected point so its nearer neighbours (an anchor's handles) are at least 24 px away.</summary>
    public void FitSelection()
    {
        if (Controller?.Section is null || SelectedVertex is not { } selected) return;
        var curve = Controller.SectionCurve(selected.Side == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower);
        if (curve is null) return;
        int index = curve.Points.ToList().FindIndex(point => point.Id == selected.Id);
        if (index < 0) return;
        var point = curve.Points[index];
        double nearest = curve.Points.Where((_, other) => Math.Abs(other - index) == 1)
            .Min(neighbour => Math.Sqrt(Math.Pow(point.SpanMeters - neighbour.SpanMeters, 2) + Math.Pow(point.Ordinate - neighbour.Ordinate, 2)));
        double width = Math.Max(1, Bounds.Width - 2 * Padding);
        // 25 px, not 24: the rule is "at least 24 px", and rounding must never land a handle at 23.99.
        viewSpan = Math.Clamp(nearest * width / 25, 1e-4, 1);
        viewMinX = point.SpanMeters - viewSpan / 2;
        viewCenterY = point.Ordinate;
        InvalidateVisual();
    }

    /// <summary>Show (§5.2): frames a blocker's chord range, a quarter of the width either side.</summary>
    public void FrameRange(double x0, double x1)
    {
        double span = Math.Clamp(Math.Abs(x1 - x0) * 2, .02, 1);
        viewSpan = span;
        viewMinX = (x0 + x1) / 2 - span / 2;
        if (Controller?.Section is { } mode)
        {
            var probe = Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, Math.Clamp((x0 + x1) / 2, 0, 1));
            viewCenterY = (probe.UpperY + probe.LowerY) / 2;
        }
        InvalidateVisual();
    }

    /// <summary>The comb plate (mockup .lb): "Comb · auto scale · N teeth clipped (×)", written after each comb pass.</summary>
    public TextBlock? CombTarget { get; set; }

    private void ReportComb()
    {
        if (CombTarget is not { } target) return;
        string text = string.Create(CultureInfo.InvariantCulture, $"Comb · auto scale · {CombClippedCount} teeth clipped (×)");
        // Render must not change layout; the plate text follows on the next dispatcher turn when it changed.
        if (target.Text != text) Avalonia.Threading.Dispatcher.UIThread.Post(() => target.Text = text);
    }

    private void SetProbe(string text)
    {
        ProbeText = text;
        if (ProbeTarget is not null) ProbeTarget.Text = text;
    }

    private void ShowReason(string text, bool focus)
    {
        if (ReasonTarget is null) return;
        ReasonTarget.Text = text;
        if (ReasonContainer is not null) ReasonContainer.IsVisible = true;
        if (focus) ReasonTarget.Focus();
    }

    // The probe's "at <station> <mm> (<t/c>)" tail, computed once per draft and generation (Facts is not per-move work).
    private (string Station, double ThicknessRatio) ProbeStation(SectionMode mode)
    {
        if (probeStation is { } cached && cached.DraftId == mode.Draft.DraftId && cached.Generation == mode.Draft.Generation) return (cached.Station, cached.ThicknessRatio);
        double eta = Controller!.Inspection!.Authored.Assignments[mode.Draft.Assignment].Eta;
        string name = ElevationView.StationName(mode.Draft.Assignment, eta);
        double ratio = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment).StationThicknessRatio;
        probeStation = (mode.Draft.DraftId, mode.Draft.Generation, name, ratio);
        return (name, ratio);
    }

    public static readonly StyledProperty<ProfileView?> ProfileProperty =
        AvaloniaProperty.Register<SectionCanvas, ProfileView?>(nameof(Profile));

    public static readonly StyledProperty<bool> EditableProperty =
        AvaloniaProperty.Register<SectionCanvas, bool>(nameof(Editable), defaultValue: true);

    public static readonly StyledProperty<(string Side, string Id)?> SelectedVertexProperty =
        AvaloniaProperty.Register<SectionCanvas, (string Side, string Id)?>(nameof(SelectedVertex));

    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<SectionCanvas, IBrush?>(nameof(BackgroundBrush));

    public static readonly StyledProperty<IBrush?> FoilBrushProperty =
        AvaloniaProperty.Register<SectionCanvas, IBrush?>(nameof(FoilBrush));

    public static readonly StyledProperty<IBrush?> StationBrushProperty =
        AvaloniaProperty.Register<SectionCanvas, IBrush?>(nameof(StationBrush));

    public static readonly StyledProperty<IBrush?> FocusBrushProperty =
        AvaloniaProperty.Register<SectionCanvas, IBrush?>(nameof(FocusBrush));

    public static readonly StyledProperty<IBrush?> DangerBrushProperty =
        AvaloniaProperty.Register<SectionCanvas, IBrush?>(nameof(DangerBrush));

    public static readonly StyledProperty<Point?> RefitMarkerProperty =
        AvaloniaProperty.Register<SectionCanvas, Point?>(nameof(RefitMarker));

    public ProfileView? Profile
    {
        get => GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    public bool Editable
    {
        get => GetValue(EditableProperty);
        set => SetValue(EditableProperty, value);
    }

    public (string Side, string Id)? SelectedVertex
    {
        get => GetValue(SelectedVertexProperty);
        set => SetValue(SelectedVertexProperty, value);
    }

    public IBrush? BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush? FoilBrush
    {
        get => GetValue(FoilBrushProperty);
        set => SetValue(FoilBrushProperty, value);
    }

    public IBrush? StationBrush
    {
        get => GetValue(StationBrushProperty);
        set => SetValue(StationBrushProperty, value);
    }

    public IBrush? FocusBrush
    {
        get => GetValue(FocusBrushProperty);
        set => SetValue(FocusBrushProperty, value);
    }

    public IBrush? DangerBrush
    {
        get => GetValue(DangerBrushProperty);
        set => SetValue(DangerBrushProperty, value);
    }

    /// <summary>The refused refit's measured maximum in normalized chord coordinates.</summary>
    public Point? RefitMarker
    {
        get => GetValue(RefitMarkerProperty);
        set => SetValue(RefitMarkerProperty, value);
    }
    public string? RefitMarkerLabel { get; set; }

    public double Padding { get; set; } = 24.0;

    public event Action<string, string>? VertexSelected;
    public event Action<string, string, double, double>? VertexMoved;
    public event Shell.FocusedTargetChangedEventHandler? FocusedTargetChanged;

    public IReadOnlyList<TextBlock> SemanticControls { get; private set; } = [];
    public IReadOnlyList<ViewportSemantic> Semantics { get; private set; } = [];
    public IReadOnlyList<string> AccessibleTexts { get; private set; } = [];

    private bool isDragging;

    public SectionCanvas()
    {
        Focusable = true;
        UpdateSemantics();

        PropertyChanged += (_, args) =>
        {
            if (args.Property == ProfileProperty)
            {
                UpdateSemantics();
                InvalidateVisual();
            }
            else if (args.Property == SelectedVertexProperty ||
                     args.Property == EditableProperty ||
                     args.Property == BackgroundBrushProperty ||
                     args.Property == FoilBrushProperty ||
                     args.Property == StationBrushProperty ||
                     args.Property == FocusBrushProperty ||
                     args.Property == DangerBrushProperty ||
                     args.Property == RefitMarkerProperty)
            {
                InvalidateVisual();
            }
        };
    }

    protected virtual void OnVertexSelected(string side, string id)
    {
        VertexSelected?.Invoke(side, id);
        RaiseFocusedTarget(side, id);
    }

    public void FocusVertex(string side, string id)
    {
        if (FindVertex(side, id) is null) return;
        SelectedVertex = (side, id);
        OnVertexSelected(side, id);
        InvalidateVisual();
    }

    private void RaiseFocusedTarget(string side, string id)
    {
        var vertex = FindVertex(side, id);
        if (vertex is null) return;
        var local = ModelToScreen(vertex.X, vertex.Y);
        var top = TopLevel.GetTopLevel(this);
        var screen = top is null
            ? new PixelPoint((int)local.X, (int)local.Y)
            : top.PointToScreen(this.TranslatePoint(local, top) ?? local);
        var bounds = new Shell.PxRect(screen.X - 12, screen.Y - 12, 24, 24);
        FocusedTargetChanged?.Invoke(this, new Shell.FocusedTargetEventArgs(bounds, $"{vertex.Side} vertex {vertex.Id}"));
    }
    protected virtual void OnVertexMoved(string side, string id, double x, double y) => VertexMoved?.Invoke(side, id, x, y);

    public Point ModelToScreen(double x, double y)
    {
        double width = Bounds.Width > 0 ? Bounds.Width : (Width > 0 ? Width : 800.0);
        double height = Bounds.Height > 0 ? Bounds.Height : (Height > 0 ? Height : 400.0);
        double availableWidth = Math.Max(1.0, width - 2.0 * Padding);
        double scale = availableWidth / viewSpan;
        double originX = Padding;
        double originY = height / 2.0;
        return new Point(originX + (x - viewMinX) * scale, originY - (y - viewCenterY) * scale * (ThicknessDoubled ? 2 : 1));
    }

    public (double X, double Y) ScreenToModel(Point point)
    {
        double width = Bounds.Width > 0 ? Bounds.Width : (Width > 0 ? Width : 800.0);
        double height = Bounds.Height > 0 ? Bounds.Height : (Height > 0 ? Height : 400.0);
        double availableWidth = Math.Max(1.0, width - 2.0 * Padding);
        double scale = availableWidth / viewSpan;
        double originX = Padding;
        double originY = height / 2.0;
        return (viewMinX + (point.X - originX) / scale, viewCenterY + (originY - point.Y) / scale / (ThicknessDoubled ? 2 : 1));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bg = BackgroundBrush ?? ResolveThemeBrush("ViewportBrush") ?? ResolveThemeBrush("CanvasBrush");
        if (bg is not null)
        {
            context.FillRectangle(bg, new Rect(Bounds.Size));
        }

        if (Profile is null) return;

        var foilBrush = FoilBrush ?? ResolveThemeBrush("FoilBrush");
        var stationBrush = StationBrush ?? ResolveThemeBrush("StationBrush");
        var focusBrush = FocusBrush ?? ResolveThemeBrush("SystemControlFocusVisualPrimaryBrush") ?? ResolveThemeBrush("PrimaryBrush");
        var viewportBrush = bg ?? ResolveThemeBrush("ViewportBrush") ?? ResolveThemeBrush("SurfaceBrush");

        if (Controller?.Section is not null && foilBrush is not null && stationBrush is not null && viewportBrush is not null)
        {
            RenderSection(context, foilBrush, stationBrush, viewportBrush, focusBrush);
            return;
        }

        // 1. Draw UpperCurve and LowerCurve as smooth polylines
        if (foilBrush is not null)
        {
            var foilPen = new Pen(foilBrush, 2.0);
            DrawPolyline(context, foilPen, Profile.UpperCurve.Select(p => ModelToScreen(p.X, p.Y)));
            DrawPolyline(context, foilPen, Profile.LowerCurve.Select(p => ModelToScreen(p.X, p.Y)));
        }

        // 2. Draw each side's control polygon dashed
        if (stationBrush is not null)
        {
            var dashedPen = new Pen(stationBrush, 1.5, new DashStyle(new double[] { 4, 3 }, 0));
            DrawPolyline(context, dashedPen, Profile.Upper.Select(v => ModelToScreen(v.X, v.Y)));
            DrawPolyline(context, dashedPen, Profile.Lower.Select(v => ModelToScreen(v.X, v.Y)));

            // 3. Draw vertices
            DrawSideVertices(context, Profile.Upper, stationBrush, focusBrush, viewportBrush);
            DrawSideVertices(context, Profile.Lower, stationBrush, focusBrush, viewportBrush);
        }

        if (RefitMarker is { } marker && (DangerBrush ?? ResolveThemeBrush("DangerBrush")) is { } danger)
        {
            var at = ModelToScreen(marker.X, marker.Y);
            var pen = new Pen(danger, 2, new DashStyle([4, 3], 0));
            context.DrawLine(pen, new Point(at.X, at.Y - 14), new Point(at.X, at.Y + 14));
        }
    }

    private void RenderSection(DrawingContext context, IBrush foil, IBrush station, IBrush background, IBrush? focus)
    {
        var grid = ResolveThemeBrush("ViewportGridBrush") ?? station;
        var mute = ResolveThemeBrush("PlanMuteBrush") ?? station;
        var layer = new CurvePointLayer(ModelToScreen, ScreenToModel);
        var zero = ModelToScreen(0, 0);
        var end = ModelToScreen(1, 0);
        context.DrawLine(new Pen(mute, 1), zero, end);
        // The chord axis (mockup drawSection): a line every 10 % (5 % when zoomed in past 60 % of the chord), labelled in
        // % chord just above the bottom, over the whole visible range.
        var (left, _) = ScreenToModel(new Point(0, 0));
        var (right, _) = ScreenToModel(new Point(Bounds.Width, 0));
        double step = right - left > .6 ? .1 : .05;
        var gridPen = new Pen(grid, 1);
        for (double x = Math.Ceiling(left / step - 1e-9) * step; x <= right + 1e-9; x += step)
        {
            double at = ModelToScreen(x, 0).X;
            context.DrawLine(gridPen, new Point(at, 34), new Point(at, Bounds.Height - 30));
            var tickLabel = new FormattedText(string.Create(CultureInfo.InvariantCulture, $"{Math.Round(x * 100) + 0.0} %"),
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Default), 11, mute);
            // The last label stays inside the canvas ("100 %", not "100").
            context.DrawText(tickLabel, new Point(Math.Min(at + 3, Bounds.Width - tickLabel.Width - 3), Bounds.Height - 48));
        }
        var curvePen = new Pen(foil, 2);
        var mode = Controller!.Section!;
        var ghostPen = new Pen(mute, 1, new DashStyle([4, 4], 0));
        foreach (var entry in EntryCurves(mode))
        {
            DrawPolyline(context, ghostPen, Enumerable.Range(0, 101)
                .Select(index => Jet(entry, index / 100.0))
                .Select(jet => ModelToScreen(jet.X, jet.Y)));
        }
        var rest = RestFrame(mode);
        var (upper, lower) = DragFrame(rest.Upper, rest.Lower);
        bool live = !ReferenceEquals(upper, rest.Upper) || !ReferenceEquals(lower, rest.Lower);
        if (CurvatureVisible)
        {
            var teeth = new[] { upper, lower }.SelectMany(Comb).ToArray();
            var magnitudes = teeth.Select(tooth => Math.Abs(tooth.Curvature)).Order().ToArray();
            double p90 = magnitudes.Length == 0 ? 0 : magnitudes[(int)Math.Floor(.9 * (magnitudes.Length - 1))];
            CombScale = p90 <= 1e-12 ? 0 : 30 / p90;
            CombClippedCount = 0;
            var drawn = new List<(Point Start, Point Tip)>(teeth.Length);
            // Teeth in viewport-mute at half strength, so the curves and glyphs stay on top (mockup comb colour).
            var toothPen = new Pen(mute is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, .5) : mute, 1);
            foreach (var tooth in teeth)
            {
                double raw = Math.Abs(tooth.Curvature) * CombScale;
                bool clipped = raw > 60;
                if (clipped) CombClippedCount++;
                var start = ModelToScreen(tooth.X, tooth.Y);
                // §11.2: teeth point outward, away from the centre of curvature (mockup drawSection: dir = −sign κ).
                double sign = -Math.Sign(tooth.Curvature);
                var tip = start + new Vector(tooth.Nx * Math.Min(raw, 60) * sign,
                    -tooth.Ny * Math.Min(raw, 60) * sign * (ThicknessDoubled ? 2 : 1));
                drawn.Add((start, tip));
                context.DrawLine(toothPen, start, tip);
                if (clipped)
                {
                    context.DrawLine(new Pen(mute, 1), tip + new Vector(-3, -3), tip + new Vector(3, 3));
                    context.DrawLine(new Pen(mute, 1), tip + new Vector(-3, 3), tip + new Vector(3, -3));
                }
            }
            // §11.2: the comb breaks at an interior anchor; a dashed station mark shows where.
            var breakPen = new Pen(station, 2, new DashStyle([1.5, 1], 0));
            foreach (var anchor in new[] { upper, lower }.SelectMany(curve => curve.Points).Where(point => point.Role == PointRole.Anchor))
            {
                var at = layer.ToScreen(anchor);
                context.DrawLine(breakPen, at - new Vector(0, 20), at + new Vector(0, 20));
            }
            CombTeeth = drawn;
        }
        else { CombScale = 0; CombClippedCount = 0; CombTeeth = []; }
        ReportComb();
        // At rest the curves are the Core display samples; during a drag, the same B-spline through the moved control net.
        DrawnCurves = live
            ? [LiveSamples(upper), LiveSamples(lower)]
            : [Profile!.UpperCurve.Select(p => ModelToScreen(p.X, p.Y)).ToArray(), Profile.LowerCurve.Select(p => ModelToScreen(p.X, p.Y)).ToArray()];
        foreach (var drawnCurve in DrawnCurves) DrawPolyline(context, curvePen, drawnCurve);
        // The crossing marker belongs to the step's bytes: hidden during a drag rather than drawn where the curves were.
        var crossing = live ? null : rest.Crossing;
        CrossingInterval = crossing;
        if (crossing is { } range && (DangerBrush ?? ResolveThemeBrush("DangerBrush")) is { } crossingBrush)
        {
            // §11.2 and the mockup's crossMark: a 4 px dashed danger line along both curves over the crossing.
            var crossingPen = new Pen(crossingBrush, 4, new DashStyle([1.5, 1], 0));
            foreach (var curve in new[] { Profile!.UpperCurve, Profile.LowerCurve })
                DrawPolyline(context, crossingPen, curve.Where(p => p.X >= range.X0 && p.X <= range.X1).Select(p => ModelToScreen(p.X, p.Y)));
        }
        // The mockup's surface names at the trailing edge, whenever the whole chord is in view.
        if (ScreenToModel(new Point(Bounds.Width, 0)).X >= 1)
        {
            var upperEnd = ModelToScreen(Profile!.UpperCurve[^1].X, Profile.UpperCurve[^1].Y);
            var lowerEnd = ModelToScreen(Profile.LowerCurve[^1].X, Profile.LowerCurve[^1].Y);
            foreach (var (text, at) in new[] { ("Upper", upperEnd + new Vector(0, -24)), ("Lower", lowerEnd + new Vector(0, 6)) })
                context.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default), 11, mute), new Point(Math.Min(at.X + 12, Bounds.Width - 44), at.Y));
        }
        var brushes = new PointGlyphBrushes(foil, station, background, mute);
        foreach (var curve in new[] { upper, lower })
        {
            var polygon = new Pen(station, 1, new DashStyle([4, 3], 0));
            for (int index = 1; index < curve.Points.Count; index++)
                context.DrawLine(polygon, layer.ToScreen(curve.Points[index - 1]), layer.ToScreen(curve.Points[index]));
            foreach (var point in curve.Points)
            {
                if (curve == lower && point.Role == PointRole.Nose) continue;
                bool selected = SelectedVertex == (point.Curve, point.Id);
                var at = layer.ToScreen(point);
                CurvePointLayer.DrawGlyph(context, point, at, brushes, selected);
                if (selected && focus is not null) context.DrawEllipse(null, new Pen(focus, 3), at, 13, 13);
            }
        }
        if (SelectedVertex is { } selectedPoint)
        {
            var own = selectedPoint.Side == "upper" ? upper : lower;
            var other = selectedPoint.Side == "upper" ? lower : upper;
            int index = own.Points.ToList().FindIndex(point => point.Id == selectedPoint.Id);
            if (index >= 0 && index < other.Points.Count)
            {
                var from = layer.ToScreen(own.Points[index]);
                var partner = layer.ToScreen(other.Points[index]);
                context.DrawLine(new Pen(station, 1, new DashStyle([2, 3], 0)), from, partner);
                context.DrawEllipse(null, new Pen(station, 1.5, new DashStyle([3, 2], 0)), partner, 10, 10);
                var label = new FormattedText($"{other.Curve} pt {index + 1} · paired", CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface(FontFamily.Default), 11, station);
                context.DrawText(label, partner + new Vector(14, partner.Y > from.Y ? 16 : -10));
            }
        }
        if (RefitMarker is { } marker && (DangerBrush ?? ResolveThemeBrush("DangerBrush")) is { } danger)
        {
            var at = ModelToScreen(marker.X, marker.Y);
            context.DrawLine(new Pen(danger, 2, new DashStyle([4, 3], 0)),
                new Point(at.X, at.Y - 14), new Point(at.X, at.Y + 14));
            if (RefitMarkerLabel is { Length: > 0 } label)
                context.DrawText(new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default), 11, danger), new Point(at.X - 40, at.Y + 28 > Bounds.Height - 64 ? at.Y - 44 : at.Y + 28));   // clear of the axis labels
        }
    }

    // Per-bytes projections: a drag frame re-reads none of them (each Sections call re-parses the source).
    private (byte[] Bytes, int Assignment, CurveView[] Curves)? entryFrame;
    private (byte[] Bytes, int Assignment, CurveView Upper, CurveView Lower, (double X0, double X1)? Crossing)? restFrame;

    private CurveView[] EntryCurves(SectionMode mode)
    {
        if (entryFrame is { } cached && ReferenceEquals(cached.Bytes, mode.BaseBytes) && cached.Assignment == mode.Draft.Assignment)
            return cached.Curves;
        CurveView[] curves = [Sections.View(mode.BaseBytes, mode.Draft.Assignment, SurfaceSide.Upper, "entry", 0),
            Sections.View(mode.BaseBytes, mode.Draft.Assignment, SurfaceSide.Lower, "entry", 0)];
        entryFrame = (mode.BaseBytes, mode.Draft.Assignment, curves);
        return curves;
    }

    private (CurveView Upper, CurveView Lower, (double X0, double X1)? Crossing) RestFrame(SectionMode mode)
    {
        if (restFrame is not { } cached || !ReferenceEquals(cached.Bytes, mode.Draft.Bytes) || cached.Assignment != mode.Draft.Assignment)
        {
            cached = (mode.Draft.Bytes, mode.Draft.Assignment, Controller!.SectionCurve(SurfaceSide.Upper)!, Controller.SectionCurve(SurfaceSide.Lower)!,
                Sections.DisplayCrossing(mode.Draft.Bytes, mode.Draft.Assignment));
            restFrame = cached;
        }
        return (cached.Upper, cached.Lower, cached.Crossing);
    }

    /// <summary>
    /// The drag's display state: the control net as the release step will write it. This mirrors SectionEdits.Move (the
    /// point takes x and y; the paired point on the other surface takes x), for drawing only; the step decides on release.
    /// </summary>
    private (CurveView Upper, CurveView Lower) DragFrame(CurveView upper, CurveView lower)
    {
        if (draggedPoint is { } dragged && previewPoint is { } at) return DragFrameAt(upper, lower, dragged, at);
        if (releasedDrag is { } released && Controller?.Section is { } mode && mode.Draft.DraftId == released.DraftId &&
            mode.Draft.Generation == released.Generation)
            return DragFrameAt(upper, lower, released.Point, released.At);
        return (upper, lower);
    }

    private static (CurveView Upper, CurveView Lower) DragFrameAt(CurveView upper, CurveView lower, PointView dragged, Point at)
    {
        bool onUpper = dragged.Curve == "upper";
        var own = onUpper ? upper : lower;
        var other = onUpper ? lower : upper;
        int index = own.Points.ToList().FindIndex(point => point.Id == dragged.Id);
        if (index < 0) return (upper, lower);
        own = Moved(own, index, at.X, at.Y);
        if (index < other.Points.Count) other = Moved(other, index, at.X, other.Points[index].Ordinate);
        return onUpper ? (own, other) : (other, own);

        static CurveView Moved(CurveView curve, int index, double x, double y)
        {
            var points = curve.Points.ToArray();
            points[index] = points[index] with { SpanMeters = x, Ordinate = y };
            return curve with { Points = points };
        }
    }

    private Point[] LiveSamples(CurveView curve) =>
        Enumerable.Range(0, 201).Select(index => Jet(curve, index / 200.0)).Select(jet => ModelToScreen(jet.X, jet.Y)).ToArray();

    private readonly record struct JetPoint(double X, double Y, double Nx, double Ny, double Curvature);

    private static JetPoint Jet(CurveView curve, double parameter)
    {
        const int degree = 5;
        var knots = curve.Knots;
        int count = curve.Points.Count;
        double t = parameter == 1 ? Math.BitDecrement(1.0) : parameter;
        var levels = new double[degree + 1][];
        levels[0] = new double[count + degree];
        for (int i = 0; i < levels[0].Length && i + 1 < knots.Count; i++)
            levels[0][i] = knots[i] <= t && t < knots[i + 1] ? 1 : 0;
        for (int k = 1; k <= degree; k++)
        {
            levels[k] = new double[count + degree - k];
            for (int i = 0; i < levels[k].Length && i + k + 1 < knots.Count; i++)
            {
                double a = knots[i + k] - knots[i];
                double b = knots[i + k + 1] - knots[i + 1];
                levels[k][i] = (a == 0 ? 0 : (t - knots[i]) / a * levels[k - 1][i]) +
                    (b == 0 ? 0 : (knots[i + k + 1] - t) / b * levels[k - 1][i + 1]);
            }
        }
        double x = 0, y = 0, dx = 0, dy = 0, ddx = 0, ddy = 0;
        for (int i = 0; i < count; i++)
        {
            double d = BasisDerivative(i, degree, levels, knots);
            double dd = BasisSecond(i, degree, levels, knots);
            x += levels[degree][i] * curve.Points[i].SpanMeters;
            y += levels[degree][i] * curve.Points[i].Ordinate;
            dx += d * curve.Points[i].SpanMeters;
            dy += d * curve.Points[i].Ordinate;
            ddx += dd * curve.Points[i].SpanMeters;
            ddy += dd * curve.Points[i].Ordinate;
        }
        double speed = Math.Sqrt(dx * dx + dy * dy);
        double curvature = speed <= 1e-12 ? 0 : (dx * ddy - dy * ddx) / (speed * speed * speed);
        return new(x, y, speed <= 1e-12 ? 0 : -dy / speed, speed <= 1e-12 ? 0 : dx / speed, curvature);
    }

    private static double BasisDerivative(int index, int degree, double[][] levels, IReadOnlyList<double> knots)
    {
        double a = knots[index + degree] - knots[index];
        double b = knots[index + degree + 1] - knots[index + 1];
        return (a == 0 ? 0 : degree / a * levels[degree - 1][index]) -
            (b == 0 ? 0 : degree / b * levels[degree - 1][index + 1]);
    }

    private static double BasisSecond(int index, int degree, double[][] levels, IReadOnlyList<double> knots)
    {
        static double Derivative(int i, int k, double[][] rows, IReadOnlyList<double> values)
        {
            double a = values[i + k] - values[i], b = values[i + k + 1] - values[i + 1];
            return (a == 0 ? 0 : k / a * rows[k - 1][i]) - (b == 0 ? 0 : k / b * rows[k - 1][i + 1]);
        }
        double left = knots[index + degree] - knots[index];
        double right = knots[index + degree + 1] - knots[index + 1];
        return (left == 0 ? 0 : degree / left * Derivative(index, degree - 1, levels, knots)) -
            (right == 0 ? 0 : degree / right * Derivative(index + 1, degree - 1, levels, knots));
    }

    private static IEnumerable<JetPoint> Comb(CurveView curve)
    {
        var samples = Enumerable.Range(0, 161).Select(index => Jet(curve, index / 160.0)).ToArray();
        var distances = new double[samples.Length];
        for (int i = 1; i < samples.Length; i++)
            distances[i] = distances[i - 1] + Math.Sqrt(Math.Pow(samples[i].X - samples[i - 1].X, 2) +
                Math.Pow(samples[i].Y - samples[i - 1].Y, 2));
        if (distances[^1] <= 0) yield break;
        for (int tooth = 1; tooth < 40; tooth++)
        {
            double target = distances[^1] * tooth / 40;
            int right = Array.BinarySearch(distances, target);
            if (right < 0) right = ~right;
            right = Math.Clamp(right, 1, samples.Length - 1);
            double fraction = (target - distances[right - 1]) / (distances[right] - distances[right - 1]);
            yield return Jet(curve, (right - 1 + fraction) / 160);
        }
    }

    private static void DrawPolyline(DrawingContext context, Pen pen, IEnumerable<Point> points)
    {
        using var enumerator = points.GetEnumerator();
        if (!enumerator.MoveNext()) return;

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(enumerator.Current, false);
            while (enumerator.MoveNext())
            {
                ctx.LineTo(enumerator.Current);
            }
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geom);
    }

    private void DrawSideVertices(DrawingContext context, IReadOnlyList<ProfileVertex> vertices,
        IBrush stationBrush, IBrush? focusBrush, IBrush? viewportBrush)
    {
        var strokePen = new Pen(stationBrush, 1.5);
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            var pt = ModelToScreen(v.X, v.Y);
            bool isEnd = (i == 0 || i == vertices.Count - 1);
            bool isSelected = SelectedVertex is not null &&
                              string.Equals(SelectedVertex.Value.Side, v.Side, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(SelectedVertex.Value.Id, v.Id, StringComparison.Ordinal);

            IBrush? fillBrush = v.Fixed
                ? null
                : isSelected
                    ? stationBrush
                    : viewportBrush;

            if (isEnd)
            {
                // Diamond glyph for end vertices
                var diamond = new StreamGeometry();
                using (var ctx = diamond.Open())
                {
                    ctx.BeginFigure(new Point(pt.X - 7.5, pt.Y), true);
                    ctx.LineTo(new Point(pt.X, pt.Y - 7.5));
                    ctx.LineTo(new Point(pt.X + 7.5, pt.Y));
                    ctx.LineTo(new Point(pt.X, pt.Y + 7.5));
                    ctx.EndFigure(true);
                }
                context.DrawGeometry(fillBrush, strokePen, diamond);
            }
            else
            {
                // Square glyph (13 px)
                var rect = new Rect(pt.X - 6.5, pt.Y - 6.5, 13, 13);
                context.DrawRectangle(fillBrush, strokePen, rect);
            }

            if (isSelected && focusBrush is not null)
            {
                // Focus ring >= 2 px
                var focusPen = new Pen(focusBrush, 2.0);
                context.DrawEllipse(null, focusPen, pt, 11, 11);
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Controller?.Section is { } mode)
        {
            var position = e.GetPosition(this);
            var upper = Controller.SectionCurve(SurfaceSide.Upper);
            var lower = Controller.SectionCurve(SurfaceSide.Lower);
            if (upper is null || lower is null) return;
            var layer = new CurvePointLayer(ModelToScreen, ScreenToModel);
            var hit = layer.HitTest(upper.Points.Concat(lower.Points), position, Controller.Selection);
            if (hit is null)
            {
                if (e.ClickCount >= 2)
                {
                    var (x, y) = ScreenToModel(position);
                    var probe = Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, Math.Clamp(x, 0, 1));
                    var side = Math.Abs(y - probe.UpperY) <= Math.Abs(y - probe.LowerY) ? SurfaceSide.Upper : SurfaceSide.Lower;
                    _ = ApplyStepAsync(new SectionStep.Insert(side, Math.Clamp(x, 0, 1)));
                    e.Handled = true;
                }
                return;
            }
            SelectedVertex = (hit.Curve, hit.Id);
            var reference = new PointRef(hit.Curve, hit.Id, mode.Draft.Profile);
            Focus();
            // A started gesture selects the point itself: one shell refresh per press, not two.
            if (hit.Freedom != PointFreedom.Fixed && e.ClickCount == 1 && Controller.BeginGesture(reference, GestureInput.Pointer))
            {
                draggedPoint = hit;
                draggedPointer = e.Pointer;
                pressPoint = position;
                e.Pointer.Capture(this);
            }
            else Controller.Select(new Selection.Points([reference]));
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (!Editable || Profile is null) return;

        var pos = e.GetPosition(this);
        ProfileVertex? best = null;
        double bestDist = double.MaxValue;

        foreach (var v in Profile.Upper.Concat(Profile.Lower))
        {
            var sPt = ModelToScreen(v.X, v.Y);
            double dx = pos.X - sPt.X;
            double dy = pos.Y - sPt.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist <= 20.0 && dist < bestDist)
            {
                bestDist = dist;
                best = v;
            }
        }

        if (best is not null)
        {
            SelectedVertex = (best.Side, best.Id);
            OnVertexSelected(best.Side, best.Id);
            Focus();
            if (!best.Fixed)
            {
                isDragging = true;
                e.Pointer.Capture(this);
            }
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (Controller?.Section is { } mode)
        {
            var position = e.GetPosition(this);
            var (x, y) = ScreenToModel(position);
            if (draggedPoint is { } origin)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    var from = ModelToScreen(origin.SpanMeters, origin.Ordinate);
                    if (Math.Abs(position.X - from.X) > Math.Abs(position.Y - from.Y)) y = origin.Ordinate;
                    else x = origin.SpanMeters;
                }
                // §3.7: the frame is drawn here from the gesture; the controller only keeps the release target.
                Controller.UpdateSectionGesture(x, y, Point.Distance(position, pressPoint));
                if (Controller.Gesture == GestureState.Dragging) previewPoint = new Point(x, y);
                SetProbe(string.Create(CultureInfo.InvariantCulture,
                    $"Display · Δx {(x - origin.SpanMeters) * 100:F2} % c · Δy {(y - origin.Ordinate) * 100:F2} % c"));
            }
            else
            {
                // §11.1 and the approved mockup's probeText: record values, local thickness, what the station builds there.
                var reading = Sections.Probe(mode.Draft.Bytes, mode.Draft.Assignment, Math.Clamp(x, 0, 1));
                var (station, ratio) = ProbeStation(mode);
                SetProbe(string.Create(CultureInfo.InvariantCulture,
                    $"Display · pointer x {reading.X * 100:F2} % · upper {reading.UpperY * 100:F2} % · lower {reading.LowerY * 100:F2} % · t here {reading.Thickness * 100:F2} % · at {station} {reading.PlacedThicknessMeters * 1000:F2} mm ({ratio * 100:F2} % t/c)"));
            }
            base.OnPointerMoved(e);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        base.OnPointerMoved(e);
        if (!Editable || Profile is null || !isDragging || SelectedVertex is null) return;

        var v = FindVertex(SelectedVertex.Value.Side, SelectedVertex.Value.Id);
        if (v is null || v.Fixed) return;

        var pos = e.GetPosition(this);
        var (mx, my) = ScreenToModel(pos);
        OnVertexMoved(v.Side, v.Id, mx, my);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Controller?.Section is { } mode && draggedPoint is not null)
        {
            var held = Controller.Gesture == GestureState.Dragging && previewPoint is { } at
                ? (draggedPoint, at, mode.Draft.DraftId, mode.Draft.Generation) : ((PointView, Point, string, long)?)null;
            ReleaseDrag();
            releasedDrag = held;
            _ = EndGestureAsync(GestureEnd.Release);
            e.Handled = true;
            return;
        }
        if (isDragging)
        {
            isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void ReleaseDrag()
    {
        draggedPoint = null;
        previewPoint = null;
        draggedPointer?.Capture(null);
        draggedPointer = null;
    }

    private async Task EndGestureAsync(GestureEnd reason)
    {
        var held = releasedDrag;
        try { if (Controller is not null) await Controller.EndGestureAsync(reason); }
        catch (ContractError error) { ShowReason(error.Message, focus: false); }
        if (releasedDrag == held) releasedDrag = null;
        InvalidateVisual();
    }

    private async Task ApplyStepAsync(SectionStep step)
    {
        try { if (Controller is not null) await Controller.ApplySectionStepAsync(step); }
        catch (ContractError error) { ShowReason(error.Message, focus: false); }
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Controller?.Section is { } mode)
        {
            bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
            // DR-NAV-1: Tab from a selected point goes to its Type; with nothing selected Tab leaves as usual.
            if (e.Key == Key.Tab)
            {
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift) && SelectedVertex is not null && TabOut?.Invoke() == true) e.Handled = true;
                return;
            }
            if (command && e.Key == Key.Return)
            {
                if (mode.CanFinish) _ = FinishSectionAsync();
                else ShowReason(mode.FinishReason ?? "This section cannot Finish yet.", focus: true);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Return && SelectedVertex is not null)
            {
                e.Handled = ValueOut?.Invoke() == true;
                return;
            }
            if (e.Key is Key.OemCloseBrackets or Key.OemOpenBrackets)
            {
                var points = Controller.SectionCurve(SurfaceSide.Upper)!.Points
                    .Concat(Controller.SectionCurve(SurfaceSide.Lower)!.Points.Skip(1)).ToArray();
                int current = Array.FindIndex(points, point => SelectedVertex == (point.Curve, point.Id));
                int direction = e.Key == Key.OemCloseBrackets ? 1 : -1;
                int next = (current + direction + points.Length) % points.Length;
                var target = points[next];
                SelectedVertex = (target.Curve, target.Id);
                Controller.Select(new Selection.Points([new PointRef(target.Curve, target.Id, mode.Draft.Profile)]));
                InvalidateVisual();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.C && !command) { CurvatureVisible = !CurvatureVisible; InvalidateVisual(); e.Handled = true; return; }
            if (e.Key == Key.F && !command) { FitSelection(); e.Handled = true; return; }
            if (e.Key == Key.D0 && command) { Fit(); e.Handled = true; return; }
            if (e.Key == Key.Escape)
            {
                if (Controller.Gesture != GestureState.Idle)
                {
                    ReleaseDrag();
                    _ = EndGestureAsync(GestureEnd.Escape);
                }
                else if (SelectedVertex is { } picked)
                {
                    var curve = Controller.SectionCurve(picked.Side == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower);
                    var point = curve?.Points.FirstOrDefault(item => item.Id == picked.Id);
                    if (point?.AnchorId is { } anchor)
                    {
                        SelectedVertex = (picked.Side, anchor);
                        Controller.Select(new Selection.Points([new PointRef(picked.Side, anchor, mode.Draft.Profile)]));
                    }
                    else
                    {
                        SelectedVertex = null;
                        Controller.Select(new Selection.Station(mode.Draft.Assignment,
                            Controller.Inspection!.Authored.Assignments[mode.Draft.Assignment].Eta));
                    }
                }
                else if (mode.IsDirty)
                {
                    ShowReason(UnsavedChanges, focus: false);
                    CancelTarget?.Focus();
                }
                else Controller.CancelSection();
                InvalidateVisual();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Back && SelectedVertex is { } deletion)
            {
                var side = deletion.Side == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower;
                var role = Controller.SectionCurve(side)?.Points.FirstOrDefault(point => point.Id == deletion.Id)?.Role;
                if (role == PointRole.Nose) ShowReason(NoseNotDeleted, focus: false);
                else if (role == PointRole.TrailingEnd) ShowReason(TrailingNotDeleted, focus: false);
                else _ = ApplyStepAsync(new SectionStep.Delete(side, deletion.Id));
                e.Handled = true;
                return;
            }
            if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down && SelectedVertex is { } choice)
            {
                var reference = new PointRef(choice.Side, choice.Id, mode.Draft.Profile);
                if (Controller.Gesture == GestureState.Idle && !Controller.BeginGesture(reference, GestureInput.Keyboard)) return;
                var layer = new CurvePointLayer(ModelToScreen, ScreenToModel);
                var (span, ordinate) = layer.KeyboardDirection(e.Key);
                Controller.Nudge(span, ordinate, command ? NudgeModifier.Command :
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? NudgeModifier.Shift : NudgeModifier.Plain);
                e.Handled = true;
                return;
            }
            return;
        }
        if (!Editable || Profile is null) return;

        if (e.Key == Key.Escape)
        {
            SelectedVertex = null;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var editable = Profile.Upper.Where(v => !v.Fixed)
                .Concat(Profile.Lower.Where(v => !v.Fixed))
                .ToList();
            if (editable.Count == 0) return;

            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            int idx = -1;
            if (SelectedVertex is not null)
            {
                idx = editable.FindIndex(v => string.Equals(v.Side, SelectedVertex.Value.Side, StringComparison.OrdinalIgnoreCase) &&
                                              string.Equals(v.Id, SelectedVertex.Value.Id, StringComparison.Ordinal));
            }

            int nextIdx;
            if (idx == -1)
            {
                nextIdx = shift ? editable.Count - 1 : 0;
            }
            else
            {
                nextIdx = shift ? (idx - 1 + editable.Count) % editable.Count : (idx + 1) % editable.Count;
            }

            var next = editable[nextIdx];
            SelectedVertex = (next.Side, next.Id);
            OnVertexSelected(next.Side, next.Id);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            if (SelectedVertex is null) return;
            var v = FindVertex(SelectedVertex.Value.Side, SelectedVertex.Value.Id);
            if (v is null) return;
            if (v.Fixed)
            {
                e.Handled = true;
                return;
            }

            double step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 0.01 : 0.001;
            double dx = 0, dy = 0;
            switch (e.Key)
            {
                case Key.Left: dx = -step; break;
                case Key.Right: dx = step; break;
                case Key.Up: dy = step; break;
                case Key.Down: dy = -step; break;
            }

            double newX = v.X + dx;
            double newY = v.Y + dy;
            OnVertexMoved(v.Side, v.Id, newX, newY);
            e.Handled = true;
        }
    }

    private async Task FinishSectionAsync()
    {
        try { if (Controller is not null) await Controller.FinishSectionAsync(); }
        catch (ContractError) { ShowReason(Controller?.Section?.FinishReason ?? "This section cannot Finish yet.", focus: true); }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (Controller?.Section is not null && Controller.Gesture == GestureState.Nudging &&
            e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _ = EndGestureAsync(GestureEnd.KeyUp);
            e.Handled = true;
        }
    }

    private ProfileVertex? FindVertex(string side, string id)
    {
        if (Profile is null) return null;
        var list = string.Equals(side, "upper", StringComparison.OrdinalIgnoreCase) ? Profile.Upper : Profile.Lower;
        return list.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.Ordinal));
    }

    private void UpdateSemantics()
    {
        if (Profile is null)
        {
            Semantics = [];
            SemanticControls = [];
            AccessibleTexts = [];
            return;
        }

        var list = new List<ViewportSemantic>();
        var tbList = new List<TextBlock>();
        var textList = new List<string>();

        if (Controller?.Section is not null)
        {
            foreach (var point in Controller.SectionCurve(SurfaceSide.Upper)!.Points
                .Concat(Controller.SectionCurve(SurfaceSide.Lower)!.Points.Skip(1)))
            {
                string type = Sections.PointType(point);
                string name = $"{point.Curve} surface · point {point.Index + 1} · {type} · x {point.SpanMeters * 100:F2} % c · y {point.Ordinate * 100:F2} % c";
                string id = $"section-{point.Curve}-{point.Id}";
                list.Add(new ViewportSemantic(id, name, type));
                textList.Add(name);
                var block = new TextBlock { Text = name };
                AutomationProperties.SetName(block, name);
                AutomationProperties.SetAutomationId(block, id);
                AutomationProperties.SetControlTypeOverride(block, AutomationControlType.ListItem);
                tbList.Add(block);
            }
            Semantics = list;
            SemanticControls = tbList;
            AccessibleTexts = textList;
            return;
        }

        foreach (var v in Profile.Upper.Concat(Profile.Lower))
        {
            string side = v.Side;
            string id = v.Id;
            string fixedStr = v.Fixed ? "fixed" : "editable";
            string text = $"{side} vertex {id}, x {v.X.ToString("0.###", CultureInfo.InvariantCulture)}, y {v.Y.ToString("0.###", CultureInfo.InvariantCulture)}, {fixedStr}";
            string semanticId = $"vertex-{side}-{id}";

            list.Add(new ViewportSemantic(semanticId, text, fixedStr));
            textList.Add(text);

            var tb = new TextBlock { Text = text };
            AutomationProperties.SetName(tb, text);
            AutomationProperties.SetAutomationId(tb, semanticId);
            AutomationProperties.SetItemStatus(tb, fixedStr);
            AutomationProperties.SetControlTypeOverride(tb, AutomationControlType.ListItem);
            tbList.Add(tb);
        }

        Semantics = list;
        SemanticControls = tbList;
        AccessibleTexts = textList;
    }

    private IBrush? ResolveThemeBrush(string key)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out var res) && res is IBrush brush) return brush;
        if (Application.Current is not null && Application.Current.TryFindResource(key, out var appRes) && appRes is IBrush appBrush) return appBrush;
        return null;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SectionCanvasPeer(this);

    private sealed class SectionCanvasPeer(SectionCanvas owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override List<AutomationPeer>? GetChildrenCore()
        {
            return owner.SemanticControls.Select(CreatePeerForElement).OfType<AutomationPeer>().ToList();
        }
    }
}
