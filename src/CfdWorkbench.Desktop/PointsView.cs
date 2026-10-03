using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>One row of the Points pane: a vertex of the control net (design §11.4), never an ordinate on the curve.</summary>
public sealed record PointsRow(
    PointRef Target,
    string Point,
    string Type,
    string X,
    string Y,
    string? Kind,
    bool Selected,
    bool Partner,
    bool XEditable,
    bool YEditable,
    string Name);

/// <summary>One twirl group of the Points pane: a surface in the section mode, a curve in the views.</summary>
public sealed record PointsGroup(string Id, string Title, string Summary, IReadOnlyList<PointsRow> Rows);

/// <summary>
/// The Points pane's model (design §11.4, OD-3 B). In the section mode: "Control net · degree 5", the paired-types note
/// (COPY-188) and the Upper and Lower surface groups with Point · Type · x (% c) · y (% c) · Kind. In the views: the five
/// curves with Point · Type · From root (mm) · value · Kind. With no foil: the empty state.
/// </summary>
public sealed record PointsModel(
    string? Heading,
    string? Note,
    IReadOnlyList<string> Columns,
    IReadOnlyList<PointsGroup> Groups,
    EmptyState? Empty,
    bool Section);

public static class PointsView
{
    /// <summary>COPY-188 (proposed, docs/reviews/ui-m12c-paired.md): paired point types, Ruling 60.</summary>
    public const string PairedNote = "Type and Kind apply to both surfaces. x is shared.";
    public const string EmptyTitle = "No foil open";   // COPY-136

    private static readonly string[] SectionColumns = ["Point", "Type", "x % c", "y % c", "Kind"];
    private static readonly string[] ViewColumns = ["Point", "Type", "From root mm", "Value", "Kind"];

    public static PointsModel Build(WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (controller.CurrentProjection is null)
            return new PointsModel(null, null, [], [], new EmptyState(EmptyTitle, PropertyCopy.EmptyBody), false);
        var selected = controller.Selection is Selection.Points { Items.Count: 1 } one ? one.Items[0] : null;
        if (controller.Section is { } mode &&
            controller.SectionCurve(SurfaceSide.Upper) is { } upper && controller.SectionCurve(SurfaceSide.Lower) is { } lower)
        {
            int degree = upper.Knots.Count - upper.Points.Count - 1;
            // The partner cue (Ruling 60): the point of the selected point's index on the other surface; the nose has none.
            var chosen = selected is { Curve: "upper" or "lower" } reference
                ? (reference.Curve == "upper" ? upper : lower).Points.FirstOrDefault(point => point.Id == reference.VertexId)
                : null;
            int? pairIndex = chosen is { Role: not PointRole.Nose } ? chosen.Index : null;
            var groups = new[]
            {
                SurfaceGroup(upper, "upper", "Upper surface", mode.Draft.Profile, selected, pairIndex, skipNose: false),
                SurfaceGroup(lower, "lower", "Lower surface", mode.Draft.Profile, selected, pairIndex, skipNose: true)
            };
            return new PointsModel($"Control net · degree {degree.ToString(CultureInfo.InvariantCulture)}", PairedNote,
                SectionColumns, groups, null, true);
        }
        var curves = new List<PointsGroup>();
        foreach (var (name, rows) in PropertiesView.Curves)
        {
            if (controller.CurveFor(name) is not { } curve) continue;
            curves.Add(new PointsGroup(name, rows.Name, curve.Points.Count.ToString(CultureInfo.InvariantCulture),
                curve.Points.Select(point => CurveRow(point, rows, selected)).ToArray()));
        }
        return new PointsModel(null, null, ViewColumns, curves, null, false);
    }

    private static PointsGroup SurfaceGroup(CurveView curve, string side, string title, string profile, PointRef? selected,
        int? pairIndex, bool skipNose)
    {
        var rows = new List<PointsRow>();
        foreach (var point in curve.Points)
        {
            if (skipNose && point.Role == PointRole.Nose) continue;   // the nose is shared: one row, under Upper
            bool on = selected is { } item && item.Curve == side && item.VertexId == point.Id;
            bool partner = selected is { } other && other.Curve != side && pairIndex == point.Index;
            bool named = point.Freedom == PointFreedom.Fixed;
            string label = point.Role == PointRole.Nose ? "Nose" : (point.Index + 1).ToString(CultureInfo.InvariantCulture);
            rows.Add(new PointsRow(new PointRef(side, point.Id, profile), label, SectionPoints.ShortType(point.Role),
                Quantity.Typed(point.SpanMeters * 100), Quantity.Typed(point.Ordinate * 100), SectionPoints.KindText(point),
                on, partner, !named && point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly,
                !named && point.Freedom is PointFreedom.Free or PointFreedom.ValueOnly,
                SectionPoints.Name(side, point, curve)));
        }
        return new PointsGroup(side, title, curve.Points.Count.ToString(CultureInfo.InvariantCulture), rows);
    }

    private static PointsRow CurveRow(PointView point, CurveRows rows, PointRef? selected)
    {
        bool on = selected is { } item && item.Curve == point.Curve && item.VertexId == point.Id;
        string value = Quantity.Typed(point.Ordinate * PropertiesView.FieldScale[rows.ValueFamily]);
        return new PointsRow(new PointRef(point.Curve, point.Id), (point.Index + 1).ToString(CultureInfo.InvariantCulture),
            PropertiesView.RoleText(point.Role), Quantity.TypedLength(point.SpanMeters), value,
            point.Role == PointRole.Anchor ? point.Kind?.ToString() : null, on, false,
            point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly, point.Freedom is PointFreedom.Free or PointFreedom.ValueOnly,
            $"{rows.Name} point {point.Index + 1}");
    }
}

/// <summary>
/// The section-point facts both panes share (design §11.3–§11.4, Ruling 60 paired types): names, the partner on the
/// other surface, the typed-entry grammar of x and y, and the one typed commit (a gesture that ends as one step).
/// </summary>
public static class SectionPoints
{
    /// <summary>§11.3: y never takes a length.</summary>
    public const string YRefusesMm = "y is a fraction of the chord. The station's t/c sets the built thickness.";

    public static string ShortType(PointRole role) => role switch
    {
        PointRole.Nose => "Nose",
        PointRole.TrailingEnd => "TE",
        PointRole.Anchor => "Anchor",
        PointRole.NoseHandle or PointRole.TrailingHandle or PointRole.AnchorHandle => "Handle",
        _ => "Control"
    };

    public static string? KindText(PointView point) => point.Role switch
    {
        PointRole.Nose => nameof(TangentKind.Vertical),
        PointRole.Anchor => point.Kind?.ToString(),
        _ => null
    };

    public static string Name(string side, PointView point, CurveView curve) => point.Role == PointRole.Nose
        ? "Nose"
        : $"{Surface(side)} point {point.Index + 1} of {curve.Points.Count}";

    public static string Surface(string side) => side == "upper" ? "Upper" : "Lower";

    public static string Other(string side) => side == "upper" ? "lower" : "upper";

    public static SurfaceSide Side(string curve) => curve == "upper" ? SurfaceSide.Upper : SurfaceSide.Lower;

    /// <summary>The partner of a section point on the other surface (Ruling 60): same index; the nose has none.</summary>
    public static PointView? Partner(WorkbenchController controller, PointRef reference)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(reference);
        if (reference.Curve is not ("upper" or "lower")) return null;
        var own = controller.SectionCurve(Side(reference.Curve))?.Points.FirstOrDefault(point => point.Id == reference.VertexId);
        if (own is null || own.Role == PointRole.Nose) return null;
        var other = controller.SectionCurve(Side(Other(reference.Curve)));
        return other is not null && own.Index < other.Points.Count ? other.Points[own.Index] : null;
    }

    /// <summary>The edited station's name: Root, Tip, or "Station n".</summary>
    public static string StationName(AuthoredProjection projection, int assignment)
    {
        ArgumentNullException.ThrowIfNull(projection);
        double eta = projection.Assignments[assignment].Eta;
        return eta == 0 ? "Root" : eta == 1 ? "Tip" : $"Station {assignment + 1}";
    }

    /// <summary>
    /// Parses a typed x or y (§11.3) into a chord fraction. x takes % c (a bare number) or a length (mm, cm, m) converted at
    /// the edited station's chord; y refuses a length. Returns the refusal copy, or null with <paramref name="fraction"/> set.
    /// </summary>
    public static string? Parse(string? text, bool isX, double chordMeters, out double fraction, out string? echo)
    {
        fraction = double.NaN;
        echo = null;
        string label = isX ? "x" : "y";
        string trimmed = (text ?? "").Trim();
        if (trimmed.Length == 0) return PropertyCopy.NotANumber(label);
        bool length = HasLengthUnit(trimmed);
        if (length && !isX) return YRefusesMm;
        if (length)
        {
            if (!UnitEntry.TryParse(trimmed, UnitFamily.Length, new Dictionary<string, double>(), out var millimetres) ||
                !(chordMeters > 0))
                return PropertyCopy.NotANumber(label);
            fraction = millimetres.Value / 1000 / chordMeters;
            echo = $"x {Quantity.Typed(millimetres.Value)} mm = {Quantity.Typed(fraction * 100)} % chord at {Quantity.TypedLength(chordMeters)} mm chord.";
            return null;
        }
        if (!UnitEntry.TryParse(trimmed, UnitFamily.Percent, new Dictionary<string, double>(), out var percent))
            return PropertyCopy.NotANumber(label);
        fraction = percent.Value / 100;
        return null;
    }

    private static bool HasLengthUnit(string text)
    {
        string lower = text.ToLowerInvariant();
        return lower.Contains("mm", StringComparison.Ordinal) || lower.Contains("cm", StringComparison.Ordinal) ||
               lower.TrimEnd().EndsWith('m');
    }

    /// <summary>
    /// One typed edit of a section point: a gesture that ends as exactly one step (§11.4, DR-CELL commit rules). The
    /// refusal copy comes back for the field; nothing changes on a refusal.
    /// </summary>
    public static async Task<string?> CommitAsync(WorkbenchController controller, PointRef target, double x, double y,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(controller);
        var selection = controller.Selection;
        if (!controller.BeginGesture(target, GestureInput.Typed)) return "This point can't be moved.";
        controller.Select(selection);
        controller.UpdateGesture(x, y);
        try
        {
            var outcome = await controller.EndGestureAsync(GestureEnd.Release, cancellation);
            return outcome is GestureOutcome.Refused refused ? refused.Copy : null;
        }
        catch (ContractError error)
        {
            return $"{error.Reason ?? error.Code}. Nothing changed.";
        }
    }
}
