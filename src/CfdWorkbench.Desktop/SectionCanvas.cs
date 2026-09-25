using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

public class SectionCanvas : Control
{
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

    public double Padding { get; set; } = 24.0;

    public event Action<string, string>? VertexSelected;
    public event Action<string, string, double, double>? VertexMoved;

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
                     args.Property == FocusBrushProperty)
            {
                InvalidateVisual();
            }
        };
    }

    protected virtual void OnVertexSelected(string side, string id) => VertexSelected?.Invoke(side, id);
    protected virtual void OnVertexMoved(string side, string id, double x, double y) => VertexMoved?.Invoke(side, id, x, y);

    public Point ModelToScreen(double x, double y)
    {
        double width = Bounds.Width > 0 ? Bounds.Width : (Width > 0 ? Width : 800.0);
        double height = Bounds.Height > 0 ? Bounds.Height : (Height > 0 ? Height : 400.0);
        double availableWidth = Math.Max(1.0, width - 2.0 * Padding);
        double scale = availableWidth / 1.0;
        double originX = Padding;
        double originY = height / 2.0;
        return new Point(originX + x * scale, originY - y * scale);
    }

    public (double X, double Y) ScreenToModel(Point point)
    {
        double width = Bounds.Width > 0 ? Bounds.Width : (Width > 0 ? Width : 800.0);
        double height = Bounds.Height > 0 ? Bounds.Height : (Height > 0 ? Height : 400.0);
        double availableWidth = Math.Max(1.0, width - 2.0 * Padding);
        double scale = availableWidth / 1.0;
        double originX = Padding;
        double originY = height / 2.0;
        return ((point.X - originX) / scale, (originY - point.Y) / scale);
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
        if (isDragging)
        {
            isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
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
        if (this.TryFindResource(key, out var res) && res is IBrush brush) return brush;
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
