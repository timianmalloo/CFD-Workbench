using System.Globalization;
using System.Text.RegularExpressions;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

public enum ShellMode
{
    Start,
    Opening,
    Workspace,
    SectionEditor
}

public enum Mode
{
    Start,
    Opening,
    Workspace,
    SectionEditor
}

// The property grid's model (docs/reviews/ui-property-grid.md §10.1): one row type, filled per selection.
public enum IdentityGlyph { Foil, Control, Anchor, End, Handle, Several, Station }
public enum RowKind { Input, Fact, Estimate, Choice, KindList, Action }
public enum UnitFamily { None, Length, Angle, Percent }
public enum RowState { Normal, Warning, Error, Unavailable, Mixed, Locked }
public enum MessageKind { Report, Echo, Warning, Error, Reason, Info }
/// <summary>What a point or handle field moves: its span, its value (the curve's ordinate), or a handle's angle or length.</summary>
public enum RowAxis { None, Span, Value, Angle, Length }
public enum GroupChip { Preview, Checking, Unavailable }

public sealed record RowMessage(string Text, MessageKind Kind);

/// <summary>One option of an enum row; a disabled option names its reason (UI-DEAD-CONTROL, §11.4 Vertical).</summary>
public sealed record RowOption(string Value, string Text, bool Enabled = true, string? Reason = null);

public sealed record SelectionIdentity(IdentityGlyph Glyph, string Title, string? Crumb = null, PointRef? CrumbTarget = null);

public sealed record EmptyState(string Title, string Body);

public sealed record PropertyRow
{
    public required string Key { get; init; }                 // stable: p:from, p:aft, h:angle, w:root …
    public required string Label { get; init; }
    public required RowKind Kind { get; init; }
    public string Value { get; init; } = "";                   // pre-formatted by Quantity (§10.2)
    public string? Unit { get; init; }                         // null only with Dimensionless, or on a prose fact
    public UnitFamily Family { get; init; }                    // the entry grammar of an input; None on prose
    public bool Dimensionless { get; init; }
    public RowState State { get; init; }
    public string? Description { get; init; }                  // help under the row (COPY-117, COPY-149, COPY-159 …)
    public bool DescriptionAlwaysVisible { get; init; }        // a ruled authority line (COPY-159) shows without focus (B)
    public RowMessage? Message { get; init; }
    public string? AutomationName { get; init; }               // inputs, Type and Kind (§10.5)
    public string? HelperText { get; init; }                   // a locked fact's reason (§10.5 "Help text")
    public IReadOnlyList<RowOption>? Options { get; init; }
    public bool Nudge { get; init; }                           // point and handle fields only (DR-UID-2)
    public bool MustBePositive { get; init; }                  // a length that is refused at ≤ 0 (COPY-106)
    public bool AngleBounded { get; init; }                    // a rail or dihedral angle in (−90°, 90°) (COPY-158)
    public string? Subhead { get; init; }                      // "Handle toward the root" above a Corner handle's rows
    public PointRef? Target { get; init; }                     // the point an input or Kind list edits
    public RowAxis Axis { get; init; }                         // what a point or handle input moves (None elsewhere)

    public bool IsEditable => Kind is RowKind.Input or RowKind.Choice or RowKind.KindList;

    /// <summary>The one line a fact or estimate row speaks (§10.5, PG-01, PG-24); null on every other kind.</summary>
    public string? SpokenText => Kind is RowKind.Fact or RowKind.Estimate ? Speech.Line(this) : null;
}

public sealed record PropertyGroup(
    string Id,
    string Title,
    string Summary,
    bool Collapsible,
    IReadOnlyList<PropertyRow> Rows,
    IReadOnlyList<RowMessage> Notes,
    GroupChip? Chip = null,
    RowMessage? Lead = null,
    bool Continues = false);   // B (DR-CELL-1): the rows continue the group before it, under its twirl (no header)

public sealed record PropertiesModel(
    SelectionIdentity? Identity,
    IReadOnlyList<PropertyGroup> Groups,
    PropertyGroup? Wing,
    RowMessage? Banner = null,
    EmptyState? Empty = null,
    string? AvailabilityStatus = null)   // COPY-160 for the status line while an estimate is unavailable
{
    /// <summary>The groups in display order: the selection's groups, then the Wing, always last (CAD-17, UI-36).</summary>
    public IReadOnlyList<PropertyGroup> Blocks => Wing is null ? Groups : [.. Groups, Wing];
}

/// <summary>
/// What the model needs beyond the selection: the plan, the shell's preview and checking states, the points of any of
/// the five curves (<see cref="WorkbenchController.CurveFor"/>; the plan's two rails when absent), and the placed frame
/// at a station (<see cref="Placement.Frame"/>, F-12).
/// </summary>
public sealed record PropertiesContext(
    PlanformView? Plan = null,
    bool Preview = false,
    bool Checking = false,
    bool NotChecked = false,
    Func<string, CurveView?>? Curves = null,
    Func<double, StationFrame?>? Frame = null,
    SectionContext? Section = null);

/// <summary>
/// The open section draft as the Properties pane shows it (design §11.4): both surfaces of the cursor bytes, the section's
/// facts at the edited station, and the facts at each station that uses the section (its name and placed readouts).
/// </summary>
public sealed record SectionContext(
    SectionMode Mode,
    CurveView Upper,
    CurveView Lower,
    SectionFacts Facts,
    string Station,
    IReadOnlyList<(string Station, SectionFacts Facts)> Stations)
{
    /// <summary>The context of the controller's open section, or null outside the mode.</summary>
    public static SectionContext? Of(WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (controller.Section is not { } mode || controller.CurrentProjection is not { } projection ||
            controller.SectionCurve(SurfaceSide.Upper) is not { } upper || controller.SectionCurve(SurfaceSide.Lower) is not { } lower)
            return null;
        var bytes = mode.Draft.Bytes;
        var stations = new List<(string, SectionFacts)>();
        for (int index = 0; index < projection.Assignments.Count; index++)
            if (index == mode.Draft.Assignment || projection.Assignments[index].ProfileName == mode.Draft.Profile)
                stations.Add((SectionPoints.StationName(projection, index), Sections.Facts(bytes, index)));
        return new SectionContext(mode, upper, lower, Sections.Facts(bytes, mode.Draft.Assignment),
            SectionPoints.StationName(projection, mode.Draft.Assignment), stations);
    }
}

/// <summary>Copy rows the grid shows (DESIGN.md §7; docs/reviews/ui-property-grid.md §9).</summary>
public static class PropertyCopy
{
    public const string ControlDescription = "A control point pulls the curve toward it. The curve does not pass through it.";   // COPY-117
    public const string AnchorDescription = "An anchor point is on the curve. Its handles set the curve's direction on each side."; // COPY-149
    public const string Smooth = "Handles stay in line. Their lengths can differ.";                                                // COPY-150
    public const string Symmetric = "Handles stay in line and equal in length.";                                                   // COPY-151
    public const string Corner = "Each handle moves on its own. The curve can turn a corner here.";                                // COPY-152
    public const string AnchorOption = "Anchor point";
    public const string AddsHandles = " Choosing Anchor point adds handles (the rail gains up to 3 points).";                      // COPY-153
    public const string ControlOption = "Control point";
    public const string RootMirrorHandle = "Square to the centre line (root mirror). Only its length can change.";                // COPY-156
    public const string RootChordAuthority =
        "This is the root chord. Typing here moves only this point; Root chord under Wing rescales the planform.";              // COPY-159
    public const string KeepsThisHandle = "Keeps this handle; the other one moves.";                                               // COPY-162
    public const string OtherHandleStays = "The other handle does not move.";
    public const string AngleReference = "Angles are measured from the span axis, + aft.";                                         // COPY-164
    public const string PendingType = "Press Return to change the type, or Esc to keep it.";                                       // COPY-166
    public const string AngleRunStops = "Stops here: the angle stays between −90° and 90° from the span axis.";                   // COPY-170
    public const string TipCloses = "Tip closes — edit the tip station";                                                           // COPY-108
    public const string SetInWorkspace = "Set these in the workspace.";                                                            // COPY-122
    public const string EmptyTitle = "No foil open";                                                                               // COPY-136
    public const string EmptyBody = "Open or start a foil. Whatever you select in it shows its properties here.";                  // COPY-137
    public const string SelectOne = "Select one point to change it.";
    public const string FoilNote = "Select a point or a station to see its properties here. Set span and chords under Wing.";
    public const string LeadingRootFixed = "Fixed: the leading edge starts at the root.";
    public const string TrailingRoot = "On the centre line; its tangent is square to it (root mirror).";
    public const string TipEndMoves = "At the tip; moves fore and aft only.";
    public const string RootHandleMoves = "Moves along the span only (root mirror).";
    public const string NotChecked = "Points can’t be moved because this foil couldn’t be checked. Nothing changed.";
    public const string DihedralAngleReference = "Angles are measured from the span axis, + up.";                            // COPY-164, Dihedral (MC-21)
    // M1.2b2 §11.4 copy rows.
    public const string LockedDihedralRoot = "The dihedral root is at the centre line. It can't be moved.";
    public const string CoupledRoot = "The root end and its handle move together (root mirror).";
    public const string ThicknessClamp = "t/c must stay above 0 % and below 100 %.";

    /// <summary>The twist clamp reason, its number Core's domain through the placed-angle formatter the probe uses (§11.4).</summary>
    public static string TwistClamp =>
        $"Twist is limited to ±{Quantity.PlacedAngle(Geometry.TwistDomainDegrees)}° — larger angles can't be checked yet.";

    /// <summary>COPY-168 (MC-19): a typed twist or t/c that Core clamped, with Core's number.</summary>
    public static string Clamped(string typed, string reached, bool largest) =>
        $"{typed.Trim()} typed; set to {reached}, the {(largest ? "largest" : "smallest")} that can be checked.";

    /// <summary>COPY-171 (MC-22): under Smooth, a channel handle's value moves the other handle onto the line.</summary>
    public static string SmoothChannel(string valueLabel) => $"Changing one handle's {valueLabel} moves the other onto the line.";

    /// <summary>
    /// §11.4 "Committed move", one site for the Plan, the elevations and Properties: the point, the change of its value in
    /// the curve's display unit (or of its span when only the span moved), then the curve's summary quantity — MAC for
    /// the rails, the tip height or twist, the largest t/c. The numbers are the operation's own.
    /// </summary>
    public static string CommittedMove(PointView before, PointView after, CurveView curve, double? macMeters, double? maxThicknessRatio)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(curve);
        var rows = PropertiesView.Curves[after.Curve];
        string name = rows.Name.ToLowerInvariant() + " " + PointName(after, curve);
        double scale = PropertiesView.FieldScale[rows.ValueFamily];
        double valueDelta = (after.Ordinate - before.Ordinate) * scale;
        string moved = Math.Abs(valueDelta) >= 0.005 || Math.Abs(after.SpanMeters - before.SpanMeters) < 0.000005
            ? $"Moved {name} by {Quantity.WithUnit(Quantity.Typed(valueDelta), rows.ValueUnit)}."
            : $"Moved {name} along the span by {Quantity.WithUnit(Quantity.TypedLength(after.SpanMeters - before.SpanMeters), "mm")}.";
        var tip = curve.Points.FirstOrDefault(point => point.Role == PointRole.TipEnd);
        string? summary = after.Curve switch
        {
            "dihedral" when tip is not null => $"Tip height {Quantity.WithUnit(Quantity.TypedLength(tip.Ordinate), "mm")}.",
            "twist" when tip is not null => $"Tip twist {Quantity.WithUnit(Quantity.PlacedAngle(tip.Ordinate), "°")}.",
            "thickness" when maxThicknessRatio is { } ratio && double.IsFinite(ratio) =>
                $"Max t/c {Quantity.WithUnit(Quantity.PlacedPercent(ratio * 100), "%")}.",
            "leading" or "trailing" when macMeters is { } mac && double.IsFinite(mac) => $"MAC {Quantity.WithUnit(Quantity.TypedLength(mac), "mm")}.",
            _ => null
        };
        return summary is null ? moved : moved + " " + summary;
    }

    /// <summary>"point 5", "root end", "tip end", or "point 4 handle toward the tip".</summary>
    private static string PointName(PointView point, CurveView curve)
    {
        if (point.Role == PointRole.RootEnd) return "root end";
        if (point.Role == PointRole.TipEnd) return "tip end";
        if (point.Role == PointRole.AnchorHandle && curve.Points.FirstOrDefault(item => item.Id == point.AnchorId) is { } anchor)
            return $"point {anchor.Index + 1} handle toward the {(point.Index > anchor.Index ? "tip" : "root")}";
        return $"point {point.Index + 1}";
    }

    public static string NotANumber(string field) => $"Enter a number. {field} is unchanged.";                                    // COPY-118
    public static string NotPositive(string field) => $"Enter a length greater than 0 mm. {field} is unchanged.";                 // COPY-106
    public static string AngleOutOfRange(string field, string sense = "aft") =>
        $"Enter an angle between −90° and 90°, from the span axis, + {sense}. {field} is unchanged.";                             // COPY-158
    public static string UnavailableStatus(string what, string reason) => $"{what} unavailable — {reason}.";               // COPY-160
    public static string Unavailable(string reason) => $"Unavailable — {reason}. Undo, or edit again, to recompute.";             // COPY-155
    public static string PendingKind(string kind, string kept) =>
        $"Press Return to make it {kind}, or Esc to keep {kept}.";                                                                 // COPY-167 (an enum, DR-CELL-2)
    public static string FractionHint(string value) => $"{value} % — for 12 %, type 12 or 0.12 × 100.";                          // COPY-169
    public static string NudgeHelp(string unit) =>
        $"Up and Down arrows step 0.1 {unit}; with Command (Ctrl on Windows) 0.01; with Shift 1. Release to apply; Esc cancels."; // COPY-163
    public static string KindDescription(TangentKind kind) => kind switch
    {
        TangentKind.Smooth => Smooth,
        TangentKind.Symmetric => Symmetric,
        _ => Corner
    };
}

/// <summary>One formatter per quantity (DR-UID-1, §10.2). Read-only text uses U+2212 for a negative number.</summary>
public static class Quantity
{
    public const char Minus = '−';

    public static string TypedLength(double meters) => Text(meters * 1000, "0.00");
    public static string DerivedLength(double meters) => Text(meters * 1000, "0.0");
    public static string PlacedAngle(double degrees) => Text(degrees, "0.00");
    public static string DerivedAngle(double degrees) => Text(degrees, "0.0");
    public static string PlacedPercent(double percent) => Text(percent, "0.00");
    public static string MaxThickness(double ratio) => Text(ratio * 100, "0.0");
    public static string AspectRatio(double value) => Text(value, "0.00");
    public static string AreaSquareCentimetres(double squareMeters) => Text(squareMeters * 10_000, "0");
    public static string Eta(double eta) => Text(eta, "0.000");
    public static string Delta(double value) => (value > 0 ? "+" : "") + Text(value, "0.00");

    /// <summary>A typed value in its field's unit (mm, °, %) at 0.01, as an echo shows it.</summary>
    public static string Typed(double fieldValue) => Text(fieldValue, "0.00");

    /// <summary>A formatted value with its unit as copy writes it: "−1.00°", "12.40 mm", "0.30 %".</summary>
    public static string WithUnit(string value, string unit) => unit == "°" ? value + "°" : value + " " + unit;

    /// <summary>The text an input shows: the same digits with an ASCII minus, which every keyboard can type back.</summary>
    public static string ForField(string shown) => shown.Replace(Minus, '-');

    private static string Text(double value, string format)
    {
        string text = value.ToString(format, CultureInfo.InvariantCulture);
        if (text.TrimStart('-').All(c => c is '0' or '.')) text = text.TrimStart('-');   // never "−0.00"
        return text.StartsWith('-') ? Minus + text[1..] : text;
    }
}

/// <summary>Spoken lines for facts and estimates (§10.5, PG-24: abbreviations spoken in full).</summary>
public static class Speech
{
    public static string Unit(string? unit) => unit switch
    {
        "mm" => "millimetres",
        "°" => "degrees",
        "%" => "percent",
        "cm²" => "square centimetres",
        "b²/S" => "b squared over S",
        null => "",
        _ => unit
    };

    public static string Label(string label) => label switch
    {
        "AR" => "AR, aspect ratio",
        "Max t/c" => "Max t over c",
        "t/c" => "t/c, t over c",
        _ => label
    };

    public static string Line(PropertyRow row)
    {
        string label = Label(row.Label);
        if (row.State == RowState.Mixed) return $"{label}, mixed";
        if (row.State == RowState.Unavailable) return $"{label}, unavailable";
        string value = row.Value.Replace(Quantity.Minus.ToString(), "minus ", StringComparison.Ordinal);
        string approx = row.Kind == RowKind.Estimate ? "approximately " : "";
        string unit = row.Unit is null ? "" : " " + Unit(row.Unit);
        string locked = row.State == RowState.Locked ? ", locked" : "";
        return $"{label}, {approx}{value}{unit}{locked}";
    }
}

/// <summary>A parsed field entry in the field's own unit (mm, °, %).</summary>
public sealed record ParsedEntry(double Value, bool IsExpression, IReadOnlyList<string> References);

/// <summary>
/// The entry grammar per unit family (§10.2, MC-4). A bare number is in the field's unit; expressions take + − × ÷ and
/// parentheses. Lengths are Core's A4.8 grammar (LengthExpression: mm · cm · m and #span, #root_chord, #tip_chord).
/// </summary>
public static partial class UnitEntry
{
    private static readonly Dictionary<string, string> ReferenceNames = new(StringComparer.Ordinal)
    {
        ["span"] = "Span",
        ["root_chord"] = "Root chord",
        ["tip_chord"] = "Tip chord"
    };

    public static bool TryParse(string? text, UnitFamily family, IReadOnlyDictionary<string, double> referencesMeters,
        out ParsedEntry entry)
    {
        entry = new ParsedEntry(double.NaN, false, []);
        if (string.IsNullOrWhiteSpace(text)) return false;
        string normal = text.Replace(Quantity.Minus, '-').Replace('×', '*').Replace('÷', '/').Trim();
        double value;
        if (family == UnitFamily.Length)
        {
            try { value = LengthExpression.ParseMeters(normal, referencesMeters) * 1000; }
            catch (ContractError) { return false; }
        }
        else
        {
            if (normal.Contains('#', StringComparison.Ordinal)) return false;
            if (!new ScalarParser(normal, family).TryParse(out value)) return false;
        }
        if (!double.IsFinite(value)) return false;
        var references = Reference().Matches(normal).Select(match => match.Groups[1].Value)
            .Where(ReferenceNames.ContainsKey).Distinct().ToArray();
        entry = new ParsedEntry(value, !PlainNumber().IsMatch(normal), references);
        return true;
    }

    /// <summary>COPY-157, or COPY-161 when the expression names a reference; null for a plain number.</summary>
    public static string? Echo(string typed, ParsedEntry entry, string unit)
    {
        if (!entry.IsExpression) return null;
        string shown = Quantity.WithUnit(Quantity.Typed(entry.Value), unit);
        if (entry.References.Count == 0) return $"{typed.Trim()} = {shown}.";
        string names = string.Join(" and ", entry.References.Select(name => ReferenceNames[name]));
        return $"{typed.Trim()} = {shown} (set once; doesn’t follow {names})";
    }

    /// <summary>MC-20: a t/c typed under 1 % is taken as typed, never reinterpreted, and says how to type a fraction.</summary>
    public static RowMessage? Hint(ParsedEntry entry, UnitFamily family) =>
        family == UnitFamily.Percent && entry.Value is > 0 and < 1
            ? new RowMessage(PropertyCopy.FractionHint(Quantity.PlacedPercent(entry.Value)), MessageKind.Warning)
            : null;

    [GeneratedRegex(@"^\s*-?(\d+(\.\d*)?|\.\d+)\s*$")]
    private static partial Regex PlainNumber();

    [GeneratedRegex(@"#([a-z_]+)")]
    private static partial Regex Reference();

    // Scalar arithmetic with a unit on each number: ° · deg · rad for angles, % for t/c.
    private sealed class ScalarParser(string text, UnitFamily family)
    {
        private int index;

        public bool TryParse(out double value)
        {
            value = double.NaN;
            try
            {
                value = Sum(0);
                Skip();
                return index == text.Length;
            }
            catch (FormatException) { return false; }
        }

        private double Sum(int depth)
        {
            double left = Product(depth);
            while (true)
            {
                Skip();
                if (Peek is not ('+' or '-')) return left;
                char op = text[index++];
                double right = Product(depth);
                left = op == '+' ? left + right : left - right;
            }
        }

        private double Product(int depth)
        {
            double left = Unary(depth);
            while (true)
            {
                Skip();
                if (Peek is not ('*' or '/')) return left;
                char op = text[index++];
                double right = Unary(depth);
                if (op == '/' && right == 0) throw new FormatException("division by zero");
                left = op == '*' ? left * right : left / right;
            }
        }

        private double Unary(int depth)
        {
            Skip();
            if (Peek == '-') { index++; return -Unary(depth); }
            if (Peek == '+') { index++; return Unary(depth); }
            return Primary(depth);
        }

        private double Primary(int depth)
        {
            Skip();
            if (Peek == '(')
            {
                if (depth > 16) throw new FormatException("nesting too deep");
                index++;
                double inner = Sum(depth + 1);
                Skip();
                if (Peek != ')') throw new FormatException("unclosed parenthesis");
                index++;
                return inner;
            }
            int start = index;
            while (Peek is >= '0' and <= '9' or '.') index++;
            if (index == start || !double.TryParse(text[start..index], NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out double number))
                throw new FormatException("number expected");
            Skip();
            return number * UnitFactor();
        }

        private double UnitFactor()
        {
            foreach (var (unit, factor) in Units())
                if (string.CompareOrdinal(text, index, unit, 0, unit.Length) == 0 &&
                    (index + unit.Length == text.Length || !char.IsAsciiLetter(text[index + unit.Length])))
                {
                    index += unit.Length;
                    return factor;
                }
            if (index < text.Length && (char.IsAsciiLetter(text[index]) || text[index] is '°' or '%'))
                throw new FormatException("a unit of another family");
            return 1;
        }

        private (string Unit, double Factor)[] Units() => family == UnitFamily.Angle
            ? [("°", 1), ("deg", 1), ("rad", 180 / Math.PI)]
            : [("%", 1)];

        private void Skip() { while (index < text.Length && char.IsWhiteSpace(text[index])) index++; }
        private char Peek => index < text.Length ? text[index] : '\0';
    }
}

/// <summary>
/// One curve's row set (§10.1, MC-17): rows are data, never layout. <see cref="ValueFamily"/> is the value's unit family
/// (its scale is <see cref="PropertiesView.FieldScale"/>); <see cref="Noun"/> names the curve in copy ("the rail gains");
/// <see cref="AngleSense"/> is the positive sense of a handle angle (COPY-158, COPY-164).
/// </summary>
public sealed record CurveRows(string Name, string ValueLabel, string ValueUnit, UnitFamily ValueFamily, string AngleLabel,
    bool HandlesByAngle, string Noun = "rail", string AngleSense = "aft")
{
    /// <summary>The name in a context-menu row, which is title case ("Rebuild Trailing Edge…", as "Make Anchor Point").</summary>
    public string MenuName => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Name);
}

public static class PropertiesView
{
    /// <summary>
    /// The per-curve table. Which handles are typed by angle comes from Core's channel table
    /// (<see cref="ChannelUnit.HandleTyping"/>); Twist and Thickness clamps are Core's (MC-19).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, CurveRows> Curves = new Dictionary<string, CurveRows>(StringComparer.Ordinal)
    {
        ["leading"] = Row("leading", "Leading edge", "Aft", "mm", UnitFamily.Length, "Angle", "rail", "aft"),
        ["trailing"] = Row("trailing", "Trailing edge", "Aft", "mm", UnitFamily.Length, "Angle", "rail", "aft"),
        ["dihedral"] = Row("dihedral", "Dihedral", "Height", "mm", UnitFamily.Length, "Dihedral angle", "curve", "up"),
        ["twist"] = Row("twist", "Twist", "Twist", "°", UnitFamily.Angle, "Twist", "curve", "aft"),
        ["thickness"] = Row("thickness", "Thickness", "t/c", "%", UnitFamily.Percent, "t/c", "curve", "aft")
    };

    /// <summary>The one unit table: a field's number per SI unit (metres → mm, degrees → °, chord fraction → %).</summary>
    // The one degrees-per-radian site in this file (PlacementRule_RadiansConstant_SingleSiteInSource).
    private const double DegreesPerRadian = 180 / Math.PI;

    public static readonly IReadOnlyDictionary<UnitFamily, double> FieldScale = new Dictionary<UnitFamily, double>
    {
        [UnitFamily.Length] = 1000,
        [UnitFamily.Angle] = 1,
        [UnitFamily.Percent] = 100
    };

    private static CurveRows Row(string curve, string name, string value, string unit, UnitFamily family, string angle, string noun,
        string sense) =>
        new(name, value, unit, family, angle, Channels.Unit(curve).HandleTyping == Channels.AngleAndLength, noun, sense);

    public static PropertiesModel Build(
        Selection selection,
        AuthoredProjection? projection,
        WingEstimates? estimates,
        ShellMode mode,
        PropertiesContext? context = null)
    {
        if (projection is null)
            return new PropertiesModel(null, [], null, Empty: new EmptyState(PropertyCopy.EmptyTitle, PropertyCopy.EmptyBody));
        context ??= new PropertiesContext();
        var plan = context.Plan;
        Func<string, CurveView?>? curves = context.Curves ?? (plan is null ? null : curve => Rail(plan, curve));
        var groups = new List<PropertyGroup>();
        if (context.Section is { } section)
        {
            // The section mode (§11.4): a section point's rows, the Section group always, the Wing read-only (COPY-122).
            var sectionIdentity = selection is Selection.Points { Items.Count: 1 } chosen &&
                                  chosen.Items[0] is { Curve: "upper" or "lower" } reference &&
                                  (reference.Curve == "upper" ? section.Upper : section.Lower).Points.Any(point => point.Id == reference.VertexId)
                ? SectionPointRows(reference, section, groups)
                : SectionOnly(section, groups);
            var (sectionWing, sectionAvailability) = Wing(projection, estimates, ShellMode.SectionEditor, context);
            return new PropertiesModel(sectionIdentity, groups, sectionWing, AvailabilityStatus: sectionAvailability);
        }
        var identity = selection switch
        {
            Selection.Station station when station.Index >= 0 && station.Index < projection.Assignments.Count =>
                StationRows(station, projection, plan, context.Frame, groups),
            Selection.Points { Items.Count: > 1 } points when curves is not null => SeveralRows(points, curves, groups),
            Selection.Points { Items.Count: 1 } points when curves is not null && Find(curves, points.Items[0]) is { } point =>
                PointRows(point, curves(point.Curve)!, context.NotChecked, groups),
            _ => FoilRows(projection, groups)
        };
        var banner = context.NotChecked && selection is Selection.Points
            ? new RowMessage(PropertyCopy.NotChecked, MessageKind.Warning) : null;
        var (wing, availability) = Wing(projection, estimates, mode, context);
        return new PropertiesModel(identity, groups, wing, banner, AvailabilityStatus: availability);
    }

    public static PropertiesModel Build(
        Selection selection,
        AuthoredProjection? projection,
        WingEstimates? estimates,
        Mode mode) => Build(selection, projection, estimates, (ShellMode)mode);

    public static PointView? Find(PlanformView plan, PointRef reference) => Find(curve => Rail(plan, curve), reference);

    /// <summary>The point a reference names, on any of the five curves.</summary>
    public static PointView? Find(Func<string, CurveView?> curves, PointRef reference)
    {
        ArgumentNullException.ThrowIfNull(curves);
        ArgumentNullException.ThrowIfNull(reference);
        return curves(reference.Curve)?.Points.FirstOrDefault(point => point.Id == reference.VertexId);
    }

    public static CurveView? Rail(PlanformView plan, string curve) => curve switch
    {
        "leading" => plan.Leading,
        "trailing" => plan.Trailing,
        _ => null
    };

    public static bool IsHandle(PointView point) =>
        point.Role is PointRole.RootHandle or PointRole.TipHandle or PointRole.AnchorHandle;

    /// <summary>
    /// A handle's direction from its anchor: the angle from the span axis (+ aft on the rails, + up on Dihedral) and the
    /// length. Oriented root → tip for both handles, so the two handles of a Smooth anchor read one angle (§3.6).
    /// </summary>
    public static (double AngleDegrees, double LengthMeters) HandleGeometry(PointView handle, PointView anchor)
    {
        double span = handle.SpanMeters - anchor.SpanMeters;
        double aft = handle.Ordinate - anchor.Ordinate;
        if (handle.Index < anchor.Index) { span = -span; aft = -aft; }
        return (Math.Atan2(aft, span) * DegreesPerRadian, Math.Sqrt(span * span + aft * aft));
    }

    /// <summary>The inverse of <see cref="HandleGeometry"/>: where a handle typed by angle and length sits (Dihedral).</summary>
    public static (double SpanMeters, double Ordinate) HandleAt(PointView handle, PointView anchor, double angleDegrees, double lengthMeters)
    {
        double side = handle.Index < anchor.Index ? -1 : 1;
        var (sin, cos) = double.SinCosPi(angleDegrees / 180);   // half-turn trigonometry: a handle direction, not a placement
        return (anchor.SpanMeters + side * lengthMeters * cos, anchor.Ordinate + side * lengthMeters * sin);
    }

    /// <summary>The value a point or handle field shows now, in the field's unit; null when its point is gone.</summary>
    public static double? ReadValue(PropertyRow row, Func<string, CurveView?> curves)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Target is not { } target || Find(curves, target) is not { } point) return null;
        switch (row.Axis)
        {
            case RowAxis.Span: return point.SpanMeters * FieldScale[UnitFamily.Length];
            case RowAxis.Value: return point.Ordinate * FieldScale[row.Family];
        }
        if (point.AnchorId is null || curves(point.Curve)?.Points.FirstOrDefault(item => item.Id == point.AnchorId) is not { } anchor)
            return null;
        var (angle, length) = HandleGeometry(point, anchor);
        return row.Axis == RowAxis.Angle ? angle : length * FieldScale[UnitFamily.Length];
    }

    /// <summary>
    /// Where a typed field value puts its point: a span or value replaces that coordinate; a handle's angle or length goes
    /// through Core's rail handle target on the rails (it keeps Core's ordering clamp) and through
    /// <see cref="HandleAt"/> on Dihedral. Core clamps the result again when the gesture applies it.
    /// </summary>
    public static (double SpanMeters, double Ordinate) Destination(PropertyRow row, PointView point, double fieldValue,
        Func<string, CurveView?> curves, PlanformView? plan)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(point);
        switch (row.Axis)
        {
            case RowAxis.Span: return (fieldValue / FieldScale[UnitFamily.Length], point.Ordinate);
            case RowAxis.Value: return (point.SpanMeters, fieldValue / FieldScale[row.Family]);
        }
        var anchor = curves(point.Curve)!.Points.First(item => item.Id == point.AnchorId);
        var (angle, length) = HandleGeometry(point, anchor);
        double typedAngle = row.Axis == RowAxis.Angle ? fieldValue : angle;
        double typedLength = row.Axis == RowAxis.Length ? fieldValue / FieldScale[UnitFamily.Length] : length;
        if (plan is not null && Rail(plan, point.Curve) is not null)
        {
            var reached = CfdWorkbench.Core.Planform.HandleTarget(plan, point.Curve, point.Id, typedAngle, typedLength);
            return (reached.SpanMeters, reached.Ordinate);
        }
        return HandleAt(point, anchor, typedAngle, typedLength);
    }

    /// <summary>The positive sense of a handle angle on the row's curve: "aft" on the rails, "up" on Dihedral.</summary>
    public static string AngleSense(PropertyRow row) =>
        row.Target is { } target && Curves.TryGetValue(target.Curve, out var curve) ? curve.AngleSense : "aft";

    public static string RoleText(PointRole role) => role switch
    {
        PointRole.RootEnd => "Root end",
        PointRole.TipEnd => "Tip end",
        PointRole.RootHandle => "Root handle",
        PointRole.TipHandle => "Tip handle",
        PointRole.AnchorHandle => "Handle",
        PointRole.Anchor => "Anchor point",
        _ => "Control point"
    };

    // ---------------- selections ----------------

    private static SelectionIdentity FoilRows(AuthoredProjection projection, List<PropertyGroup> groups)
    {
        string name = projection.Name ?? "Unnamed";
        groups.Add(new PropertyGroup("foil", "Foil", $"{projection.Assignments.Count} stations", true,
        [
            Prose("f:name", "Name", name),
            Count("f:stations", "Stations", projection.Assignments.Count.ToString(CultureInfo.InvariantCulture))
        ], [new RowMessage(PropertyCopy.FoilNote, MessageKind.Info)]));
        return new SelectionIdentity(IdentityGlyph.Foil, name, "Foil · nothing selected");
    }

    private static SelectionIdentity StationRows(Selection.Station station, AuthoredProjection projection, PlanformView? plan,
        Func<double, StationFrame?>? frame, List<PropertyGroup> groups)
    {
        var assignment = projection.Assignments[station.Index];
        var rows = new List<PropertyRow>
        {
            Length("s:from", "From root", assignment.SpanMeters, locked: true),
            Count("s:eta", "η", Quantity.Eta(station.Eta))
        };
        if (plan is not null)
        {
            double chord = CfdWorkbench.Core.Planform.Probe(plan, station.Eta).ChordMeters;
            // DR-UID-1: a station chord at the root or tip is the typed dimension; between them it is derived.
            rows.Add(station.Eta is 0 or 1
                ? Length("s:chord", "Chord", chord)
                : Estimate("s:chord", "Chord", Quantity.DerivedLength(chord), "mm", double.IsFinite(chord)));
        }
        // F-12: the placed t/c at the station, pointwise from Placement.Frame (a non-constant t/c is no longer refused).
        if (frame?.Invoke(station.Eta) is { } placed)
            rows.Add(new PropertyRow
            {
                Key = "s:tc", Label = "t/c", Kind = RowKind.Fact, Unit = "%", Family = UnitFamily.Percent,
                Value = Quantity.PlacedPercent(placed.ThicknessRatio * FieldScale[UnitFamily.Percent])
            });
        rows.Add(Prose("s:section", "Section", assignment.ProfileName));
        // CAD-20 / COPY-172: the Station group ends with the link that opens the section editor.
        rows.Add(new PropertyRow { Key = "s:edit", Label = "", Kind = RowKind.Action, Value = EditSection, AutomationName = EditSection });
        groups.Add(new PropertyGroup("stn", "Station", assignment.ProfileName, true, rows, []));
        string title = station.Eta == 0 ? "Root station" : station.Eta == 1 ? "Tip station" : $"Station {station.Index + 1}";
        return new SelectionIdentity(IdentityGlyph.Station, title, $"Station {station.Index + 1} of {projection.Assignments.Count}");
    }

    private static SelectionIdentity SeveralRows(Selection.Points points, Func<string, CurveView?> curves, List<PropertyGroup> groups)
    {
        var found = points.Items.Select(item => Find(curves, item)).OfType<PointView>().ToArray();
        var roles = found.Select(point => RoleText(point.Role)).Distinct().ToArray();
        string type = roles.Length == 1 ? roles[0] : "Mixed";
        var names = found.Select(point => point.Curve).Distinct().ToArray();
        var shared = names.Length == 1 && Curves.TryGetValue(names[0], out var one) ? one : Curves["trailing"];
        groups.Add(new PropertyGroup("pos", "Point", "Mixed", true,
        [
            Prose("p:type", "Type", type) with { State = type == "Mixed" ? RowState.Mixed : RowState.Normal },
            Mixed("p:from", "From root", "mm", UnitFamily.Length),
            Mixed("p:aft", shared.ValueLabel, shared.ValueUnit, shared.ValueFamily)
        ], [new RowMessage(PropertyCopy.SelectOne, MessageKind.Reason)]));
        string crumb = names.Length == 1
            ? $"{Curves[names[0]].Name} · points {Join(found.Select(point => (point.Index + 1).ToString(CultureInfo.InvariantCulture)))}"
            : Join(names.Select(curve => Curves.TryGetValue(curve, out var rows) ? rows.Name : curve));
        return new SelectionIdentity(IdentityGlyph.Several, $"{points.Items.Count} points", crumb);
    }

    private static SelectionIdentity PointRows(PointView point, CurveView rail, bool readOnly, List<PropertyGroup> groups)
    {
        var curve = Curves[point.Curve];
        if (IsHandle(point)) return HandleRows(point, rail, curve, readOnly, groups);

        bool named = point.Role is not (PointRole.Control or PointRole.Anchor);
        var rows = new List<PropertyRow>
        {
            named || readOnly ? Prose("p:type", "Type", RoleText(point.Role)) with { State = RowState.Locked } : TypeRow(point, curve)
        };
        bool spanFree = !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly;
        bool valueFree = !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.ValueOnly;
        var target = new PointRef(point.Curve, point.Id);
        rows.Add(spanFree
            ? LengthInput("p:from", "From root", point.SpanMeters, "From root, position along the span in millimetres", target, nudge: true)
                with { Axis = RowAxis.Span }
            : Length("p:from", "From root", point.SpanMeters, locked: true));
        rows.Add(Count("p:eta", "η", Quantity.Eta(point.Eta)));
        bool teRoot = point.Curve == "trailing" && point.Role == PointRole.RootEnd;
        rows.Add(valueFree
            ? ValueInput("p:aft", curve.ValueLabel, point.Ordinate, curve, ValueName(curve), target)
                with { Description = teRoot ? PropertyCopy.RootChordAuthority : null, DescriptionAlwaysVisible = teRoot }
            : ValueFact("p:aft", curve.ValueLabel, point.Ordinate, curve, locked: true));
        var notes = new List<RowMessage>();
        bool mirrored = point.Locks.Contains("root_mirror");
        string? lockNote = point.Role == PointRole.RootEnd && point.Curve == "leading" ? PropertyCopy.LeadingRootFixed
            : point.Role == PointRole.RootEnd && point.Curve == "dihedral" && point.Freedom == PointFreedom.Fixed ? PropertyCopy.LockedDihedralRoot
            : teRoot && mirrored ? PropertyCopy.TrailingRoot
            : point.Role == PointRole.RootEnd && mirrored && point.Curve is "twist" or "thickness" ? PropertyCopy.CoupledRoot
            : point.Role == PointRole.TipEnd && point.Freedom == PointFreedom.ValueOnly
                ? curve.HandlesByAngle && curve.Noun == "rail" ? PropertyCopy.TipEndMoves : $"At the tip; only its {ValueNoun(curve)} changes."
            : point.Freedom == PointFreedom.Fixed ? "Fixed."
            : null;
        if (lockNote is not null) notes.Add(new RowMessage(lockNote, MessageKind.Reason));
        string summary = point.Freedom == PointFreedom.Fixed ? "fixed"
            : $"{Quantity.TypedLength(point.SpanMeters)} mm, {Quantity.WithUnit(Value(point.Ordinate, curve), curve.ValueUnit)}";
        groups.Add(new PropertyGroup("pos", "Point", summary, true, Lock(rows, notes), notes));

        if (TangentGroup(point, rail, curve, readOnly) is { } tangent) groups.Add(tangent);
        groups.Add(RailGroup(rail, curve));

        string where = point.Role switch
        {
            PointRole.RootEnd => $"{curve.Name} · root end",
            PointRole.TipEnd => $"{curve.Name} · tip end",
            _ => $"{curve.Name} · point {point.Index + 1} of {rail.Points.Count}"
        };
        var glyph = point.Role switch
        {
            PointRole.RootEnd or PointRole.TipEnd => IdentityGlyph.End,
            PointRole.Anchor => IdentityGlyph.Anchor,
            _ => IdentityGlyph.Control
        };
        string? crumb = point.Role switch
        {
            PointRole.RootEnd => "Anchor · root end",
            PointRole.TipEnd => "Anchor · tip end",
            _ => null
        };
        return new SelectionIdentity(glyph, where, crumb);
    }

    private static PropertyGroup? TangentGroup(PointView point, CurveView rail, CurveRows curve, bool readOnly)
    {
        var handles = rail.Points.Where(item => item.AnchorId == point.Id).OrderBy(item => item.Index).ToArray();
        var lead = curve.HandlesByAngle ? new RowMessage(AngleReference(curve), MessageKind.Info) : null;
        if (point.Role == PointRole.Anchor && point.Kind is { } kind)
        {
            var rows = new List<PropertyRow> { KindRow(point, kind, "Tangent kind", readOnly) };
            if (!readOnly) rows.AddRange(curve.HandlesByAngle ? AnchorHandleRows(point, handles, kind, curve) : ChannelHandleRows(point, handles, curve));
            var notes = !curve.HandlesByAngle && kind == TangentKind.Smooth
                ? new List<RowMessage> { new(PropertyCopy.SmoothChannel(ValueNoun(curve)), MessageKind.Info) } : [];
            return new PropertyGroup("tan", "Tangent", kind.ToString(), true, rows, notes, Lead: lead, Continues: true);
        }
        if (point.Role is not (PointRole.RootEnd or PointRole.TipEnd) || handles.Length != 1) return null;
        var handle = handles[0];
        var (angle, length) = HandleGeometry(handle, point);
        var target = new PointRef(handle.Curve, handle.Id);
        string title = point.Role == PointRole.TipEnd ? "Tip handle" : "Root handle";
        if (!curve.HandlesByAngle)
        {
            // Twist and t/c: the end's handle is typed by From root and value, as a point (§3.6).
            var endRows = new List<PropertyRow>
            {
                SpanRow("h:from", "From root", handle, readOnly, $"From root of the {title.ToLowerInvariant()}, in millimetres"),
                ValueRow("h:value", curve.ValueLabel, handle, curve, readOnly, $"{curve.ValueLabel} of the {title.ToLowerInvariant()}, {Speech.Unit(curve.ValueUnit)}")
            };
            var endNotes = point.Role == PointRole.RootEnd && point.Locks.Contains("root_mirror")
                ? new List<RowMessage> { new(PropertyCopy.CoupledRoot, MessageKind.Reason) } : [];
            return new PropertyGroup("tan", title,
                $"{Quantity.TypedLength(handle.SpanMeters)} mm, {Quantity.WithUnit(Value(handle.Ordinate, curve), curve.ValueUnit)}", true,
                Lock(endRows, endNotes), endNotes, Continues: true);
        }
        if (point.Role == PointRole.RootEnd && point.Locks.Contains("root_mirror"))
        {
            var mirrored = new List<PropertyRow> { Prose("t:kind", "Kind", "Square to the centre line") with { State = RowState.Locked } };
            if (!readOnly && handle.Freedom != PointFreedom.Fixed)
                mirrored.Add(LengthInput("h:length", "Handle length", length, "Handle length in millimetres", target, nudge: true)
                    with { MustBePositive = true, Axis = RowAxis.Length });
            return new PropertyGroup("tan", "Tangent", "root mirror", true, mirrored,
                [new RowMessage(PropertyCopy.RootMirrorHandle, MessageKind.Reason)], Continues: true);
        }
        string which = point.Role == PointRole.TipEnd ? "the tip handle" : "the root handle";
        var angleRows = readOnly
            ? new List<PropertyRow> { AngleFact("h:angle", curve.AngleLabel, angle), Length("h:length", "Length", length) }
            :
            [
                AngleInput("h:angle", curve.AngleLabel, angle, $"Angle of {which}, in degrees", target),
                LengthInput("h:length", "Length", length, $"Length of {which}, in millimetres", target, nudge: true)
                    with { MustBePositive = true, Axis = RowAxis.Length }
            ];
        return new PropertyGroup("tan", title, $"{Quantity.PlacedAngle(angle)}°, {Quantity.TypedLength(length)} mm", true, angleRows, [],
            Lead: lead, Continues: true);
    }

    private static IEnumerable<PropertyRow> AnchorHandleRows(PointView anchor, PointView[] handles, TangentKind kind, CurveRows curve)
    {
        var root = handles.FirstOrDefault(item => item.Index < anchor.Index);
        var tip = handles.FirstOrDefault(item => item.Index > anchor.Index);
        if (root is null || tip is null) yield break;
        var (rootAngle, rootLength) = HandleGeometry(root, anchor);
        var (tipAngle, tipLength) = HandleGeometry(tip, anchor);
        var rootRef = new PointRef(root.Curve, root.Id);
        var tipRef = new PointRef(tip.Curve, tip.Id);
        // assume: under Smooth and Symmetric, Core keeps the other handle in line (Symmetric: equal) when one handle
        // moves (m12b-points §3 tangent rules). Not yet confirmed by a check: commit an Angle on a Smooth anchor and read
        // both handles (B2.1 in docs/reviews/property-grid-native.md). If false, "Angle, both handles" moves one handle only.
        switch (kind)
        {
            case TangentKind.Smooth:
                yield return AngleInput("h:angle", curve.AngleLabel, tipAngle, "Angle, both handles, in degrees", tipRef);
                yield return HandleLength("h:root-length", "To root", rootLength, "To root, handle length in millimetres", rootRef);
                yield return HandleLength("h:tip-length", "To tip", tipLength, "To tip, handle length in millimetres", tipRef);
                break;
            case TangentKind.Symmetric:
                yield return AngleInput("h:angle", curve.AngleLabel, tipAngle, "Angle, both handles, in degrees", tipRef);
                yield return HandleLength("h:length", "Length", tipLength, "Length, both handles, in millimetres", tipRef);
                break;
            default:
                yield return AngleInput("h:root-angle", curve.AngleLabel, rootAngle, "Angle of the handle toward the root, in degrees", rootRef)
                    with { Subhead = "Handle toward the root" };
                yield return HandleLength("h:root-length", "Length", rootLength, "Length of the handle toward the root, in millimetres", rootRef);
                yield return AngleInput("h:tip-angle", curve.AngleLabel, tipAngle, "Angle of the handle toward the tip, in degrees", tipRef)
                    with { Subhead = "Handle toward the tip" };
                yield return HandleLength("h:tip-length", "Length", tipLength, "Length of the handle toward the tip, in millimetres", tipRef);
                break;
        }
    }

    /// <summary>Twist and t/c (MC-17): each handle of an anchor by From root and value, whatever the kind.</summary>
    private static IEnumerable<PropertyRow> ChannelHandleRows(PointView anchor, PointView[] handles, CurveRows curve)
    {
        foreach (var (handle, side) in new[] { (handles.FirstOrDefault(item => item.Index < anchor.Index), "root"),
                     (handles.FirstOrDefault(item => item.Index > anchor.Index), "tip") })
        {
            if (handle is null) continue;
            string subject = $"the handle toward the {side}";
            yield return SpanRow($"h:{side}-from", "From root", handle, false, $"From root of {subject}, in millimetres")
                with { Subhead = $"Handle toward the {side}" };
            yield return ValueRow($"h:{side}-value", curve.ValueLabel, handle, curve, false, $"{curve.ValueLabel} of {subject}, {Speech.Unit(curve.ValueUnit)}");
        }
    }

    private static SelectionIdentity HandleRows(PointView handle, CurveView rail, CurveRows curve, bool readOnly, List<PropertyGroup> groups)
    {
        var anchor = rail.Points.First(item => item.Id == handle.AnchorId);
        var (angle, length) = HandleGeometry(handle, anchor);
        var target = new PointRef(handle.Curve, handle.Id);
        bool mirror = handle.Role == PointRole.RootHandle && handle.Freedom == PointFreedom.SpanOnly;
        var notes = new List<RowMessage>();
        if (mirror)
        {
            notes.Add(new RowMessage(PropertyCopy.RootHandleMoves, MessageKind.Reason));
            if (curve.HandlesByAngle) notes.Add(new RowMessage(PropertyCopy.RootMirrorHandle, MessageKind.Reason));
        }
        List<PropertyRow> rows;
        string summary;
        if (curve.HandlesByAngle)
        {
            bool angleFree = !readOnly && handle.Freedom is PointFreedom.Free or PointFreedom.ValueOnly;
            bool lengthFree = !readOnly && handle.Freedom != PointFreedom.Fixed;
            rows =
            [
                angleFree
                    ? AngleInput("h:angle", curve.AngleLabel, angle, $"Angle in degrees, from the span axis, positive {curve.AngleSense}", target)
                    : AngleFact("h:angle", curve.AngleLabel, angle) with { State = RowState.Locked },
                lengthFree
                    ? HandleLength("h:length", "Length", length, "Length in millimetres", target)
                    : Length("h:length", "Length", length, locked: true)
            ];
            summary = mirror ? $"{Quantity.TypedLength(length)} mm" : $"{Quantity.PlacedAngle(angle)}°, {Quantity.TypedLength(length)} mm";
        }
        else
        {
            // Twist and t/c: a handle is placed like a point, by From root and value (§3.6).
            rows =
            [
                SpanRow("p:from", "From root", handle, readOnly, "From root, position along the span in millimetres"),
                ValueRow("p:aft", curve.ValueLabel, handle, curve, readOnly, ValueName(curve))
            ];
            summary = $"{Quantity.TypedLength(handle.SpanMeters)} mm, {Quantity.WithUnit(Value(handle.Ordinate, curve), curve.ValueUnit)}";
        }
        groups.Add(new PropertyGroup("hdl", "Handle", summary, true, Lock(rows, notes), notes,
            Lead: mirror || !curve.HandlesByAngle ? null : new RowMessage(AngleReference(curve), MessageKind.Info)));

        string title;
        string crumb;
        if (handle.Role == PointRole.AnchorHandle && anchor.Kind is { } kind)
        {
            title = handle.Index > anchor.Index ? "Handle toward the tip" : "Handle toward the root";
            crumb = $"{curve.Name} · anchor point {anchor.Index + 1} of {rail.Points.Count}";
            // F-4: a handle shows, and edits, its parent anchor's tangent kind.
            groups.Add(new PropertyGroup("atan", $"Anchor point {anchor.Index + 1}", kind.ToString(), true,
                [KindRow(anchor, kind, $"Tangent kind of anchor point {anchor.Index + 1}", readOnly)],
                [new RowMessage(kind == TangentKind.Corner ? PropertyCopy.OtherHandleStays : PropertyCopy.KeepsThisHandle, MessageKind.Info)]));
        }
        else
        {
            title = RoleText(handle.Role);
            crumb = $"{curve.Name} · " + anchor.Role switch
            {
                PointRole.TipEnd => "tip end",
                PointRole.RootEnd => "root end",
                _ => $"point {anchor.Index + 1}"
            };
        }
        return new SelectionIdentity(IdentityGlyph.Handle, title, crumb, new PointRef(anchor.Curve, anchor.Id));
    }

    private static PropertyGroup RailGroup(CurveView rail, CurveRows curve)
    {
        int degree = rail.Knots.Count - rail.Points.Count - 1;
        string summary = $"{(degree == 3 ? "cubic" : $"degree {degree}")} · {rail.Points.Count} points ({rail.Ceiling} max)";
        return new PropertyGroup("rail", curve.Name, summary, true,
        [
            Count("r:degree", "Degree", degree == 3 ? "3 (cubic)" : degree.ToString(CultureInfo.InvariantCulture)),
            Count("r:points", "Points", $"{rail.Points.Count} of {rail.Ceiling} max"),
            new PropertyRow { Key = "r:rebuild", Label = "", Kind = RowKind.Action, Value = "Rebuild…",
                AutomationName = $"Rebuild {curve.Name}", Target = new PointRef(rail.Points[0].Curve, rail.Points[0].Id) }
        ], []);
    }

    private static string AngleReference(CurveRows curve) =>
        curve.AngleSense == "up" ? PropertyCopy.DihedralAngleReference : PropertyCopy.AngleReference;

    /// <summary>The value's name in running text: "aft", "height", "twist", "t/c".</summary>
    private static string ValueNoun(CurveRows curve) => curve.ValueLabel == "t/c" ? "t/c" : curve.ValueLabel.ToLowerInvariant();

    private static string ValueName(CurveRows curve) => curve.ValueFamily == UnitFamily.Length && curve.Noun == "rail"
        ? $"{curve.ValueLabel} position in millimetres"
        : $"{curve.ValueLabel} in {Speech.Unit(curve.ValueUnit)}";

    private static string Value(double ordinate, CurveRows curve) => Quantity.Typed(ordinate * FieldScale[curve.ValueFamily]);

    // ---------------- the section mode (design §11.4; paired types, Ruling 60) ----------------

    /// <summary>COPY-189 (proposed): the partner line under a section point's identity.</summary>
    public static string PairLine(string other, int index) => $"⇄ Paired with {other} point {index}. Type, kind and x are shared.";

    public const string NoseType = "The nose is always an anchor. It stays at the leading edge with a vertical tangent.";   // COPY-177
    public const string TrailingType = "The trailing-edge point is always an anchor. It moves up and down only.";           // COPY-178
    public const string TrailingClosedType = "The trailing-edge point is always an anchor. It stays on the chord line: the trailing edge is closed.";
    public const string SectionAngles = "Angles are in the section's own chord coordinates.";                                 // COPY-183 (as drawn)
    public const string ThicknessNote = "From the Thickness curve. At each station this shape is scaled to that t/c.";
    public const string EditSection = "Edit section…";                                                                         // COPY-172

    private static SelectionIdentity SectionPointRows(PointRef reference, SectionContext section, List<PropertyGroup> groups)
    {
        var curve = reference.Curve == "upper" ? section.Upper : section.Lower;
        var other = reference.Curve == "upper" ? section.Lower : section.Upper;
        var point = curve.Points.First(item => item.Id == reference.VertexId);
        string surface = SectionPoints.Surface(reference.Curve);
        string otherSide = SectionPoints.Other(reference.Curve);
        bool nose = point.Role == PointRole.Nose;
        bool trailing = point.Role == PointRole.TrailingEnd;
        bool handle = point.Role is PointRole.NoseHandle or PointRole.TrailingHandle or PointRole.AnchorHandle;
        var rows = new List<PropertyRow>();
        var notes = new List<RowMessage>();
        if (nose || trailing)
        {
            string help = nose ? NoseType : point.Freedom == PointFreedom.Fixed ? TrailingClosedType : TrailingType;
            rows.Add(Prose("p:type", "Type", PropertyCopy.AnchorOption) with
            {
                State = RowState.Locked, Description = help, DescriptionAlwaysVisible = true, HelperText = help
            });
        }
        else if (handle)
            rows.Add(Prose("p:type", "Type", "Handle") with { State = RowState.Locked });
        else
        {
            rows.Add(new PropertyRow
            {
                Key = "p:type", Label = "Type · both surfaces", Kind = RowKind.Choice,
                Value = point.Role == PointRole.Anchor ? "anchor" : "control",
                Options = [new RowOption("control", PropertyCopy.ControlOption), new RowOption("anchor", PropertyCopy.AnchorOption)],
                AutomationName = "Type, both surfaces", Target = reference
            });
            if (point.Role == PointRole.Control) notes.Add(new RowMessage(PropertyCopy.ControlDescription, MessageKind.Info));
        }
        bool xFree = point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly;
        bool yFree = point.Freedom is PointFreedom.Free or PointFreedom.ValueOnly;
        rows.Add(SectionValue("p:from", nose ? "x" : "x · both surfaces", point.SpanMeters, xFree, reference, RowAxis.Span,
            "x, both surfaces, in percent of chord"));
        rows.Add(SectionValue("p:aft", "y", point.Ordinate, yFree, reference, RowAxis.Value, "y in percent of chord"));
        groups.Add(new PropertyGroup("pos", "Point", $"{Quantity.Typed(point.SpanMeters * 100)} % c, {Quantity.Typed(point.Ordinate * 100)} % c",
            true, rows, notes));
        if (point.Role == PointRole.Anchor && point.Kind is { } kind)
            groups.Add(SectionTangentGroup(point, curve, reference, kind, section));
        groups.AddRange(SectionGroups(section));
        string title = nose ? "Nose" : $"{surface} surface · point {point.Index + 1} of {curve.Points.Count}";
        var glyph = nose || trailing ? IdentityGlyph.End : point.Role == PointRole.Anchor ? IdentityGlyph.Anchor
            : handle ? IdentityGlyph.Handle : IdentityGlyph.Control;
        string? pair = nose || point.Index >= other.Points.Count ? null : PairLine(otherSide, point.Index + 1);
        return new SelectionIdentity(glyph, title, pair);
    }

    private static PropertyRow SectionValue(string key, string label, double fraction, bool free, PointRef target, RowAxis axis, string name) =>
        free
            ? new PropertyRow
            {
                Key = key, Label = label, Kind = RowKind.Input, Unit = "% c", Family = UnitFamily.Percent,
                Value = Quantity.Typed(fraction * 100), AutomationName = name, Target = target, Axis = axis
            }
            : new PropertyRow
            {
                Key = key, Label = label, Kind = RowKind.Fact, Unit = "% c", Family = UnitFamily.Percent,
                Value = Quantity.Typed(fraction * 100), State = RowState.Locked
            };

    private static PropertyGroup SectionTangentGroup(PointView anchor, CurveView curve, PointRef reference, TangentKind kind, SectionContext section)
    {
        var rows = new List<PropertyRow>
        {
            new()
            {
                Key = "t:kind", Label = "Kind · both surfaces", Kind = RowKind.KindList, Value = kind.ToString(),
                Options = Sections.KindChoices(anchor).Select(choice => new RowOption(choice.Kind.ToString(), KindName(choice.Kind),
                    choice.Kind == kind || choice.Enabled, choice.Reason)).ToArray(),
                AutomationName = "Kind, both surfaces", Target = reference
            }
        };
        string surface = SectionPoints.Surface(reference.Curve);
        foreach (var (handle, toward) in new[] { (anchor.Index - 1, "nose"), (anchor.Index + 1, "tail") })
        {
            if (handle < 0 || handle >= curve.Points.Count) continue;
            var point = curve.Points[handle];
            var target = reference with { VertexId = point.Id };
            var (angle, length) = SectionHandle(point, anchor);
            double chord = section.Facts.StationChordMeters;
            rows.Add(new PropertyRow
            {
                Key = $"h:{toward}-angle", Label = "Angle", Kind = RowKind.Input, Unit = "°", Family = UnitFamily.Angle,
                Value = Quantity.PlacedAngle(angle), AutomationName = $"{surface} handle toward the {toward}, angle in degrees",
                Target = target, Axis = RowAxis.Angle, Subhead = $"{surface} handle toward the {toward}"
            });
            // OQ-4 (Ruling 66): a section handle's length is in mm at the station chord, with % c beside it.
            rows.Add(new PropertyRow
            {
                Key = $"h:{toward}-length", Label = "Length", Kind = RowKind.Input, Unit = "mm", Family = UnitFamily.Length,
                Value = Quantity.Typed(length * chord * 1000), AutomationName = $"{surface} handle toward the {toward}, length in millimetres",
                Description = $"{Quantity.Typed(length * 100)} % c", DescriptionAlwaysVisible = true, MustBePositive = true,
                Target = target, Axis = RowAxis.Length
            });
        }
        return new PropertyGroup("tan", "Tangent", KindName(kind), true, rows, [new RowMessage(SectionAngles, MessageKind.Info)]);
    }

    private static string KindName(TangentKind kind) => kind == TangentKind.Angle ? "Fixed angle" : kind.ToString();

    /// <summary>A section handle's direction from its anchor in the section's own chord coordinates, and its length (chord fractions).</summary>
    public static (double AngleDegrees, double Length) SectionHandle(PointView handle, PointView anchor)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(anchor);
        double dx = handle.SpanMeters - anchor.SpanMeters, dy = handle.Ordinate - anchor.Ordinate;
        return (Math.Atan2(dy, dx) * DegreesPerRadian, Math.Sqrt(dx * dx + dy * dy));
    }

    /// <summary>The inverse of <see cref="SectionHandle"/>: where a handle typed by angle and length sits.</summary>
    public static (double X, double Y) SectionHandleAt(PointView anchor, double angleDegrees, double length)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        var (sin, cos) = double.SinCosPi(angleDegrees / 180);
        return (anchor.SpanMeters + length * cos, anchor.Ordinate + length * sin);
    }

    /// <summary>
    /// The Section group (always present in the mode): own t/c and where, t/c at each station that uses the section with
    /// the consequence line, the station t/c intent, LE radius, TE gap and wedge own and at the station, and the counts.
    /// </summary>
    internal static IReadOnlyList<PropertyGroup> SectionGroups(SectionContext section)
    {
        var facts = section.Facts;
        int degree = section.Upper.Knots.Count - section.Upper.Points.Count - 1;
        string source = SourceDisplay(section.Mode.Draft.Bytes, section.Mode.Draft.Profile);
        var rows = new List<PropertyRow>
        {
            Prose("sec:source", "Source", source),
            Prose("sec:own", "Own t/c", $"{Quantity.Typed(facts.OwnThickness * 100)} % at {Quantity.Typed(facts.OwnThicknessX * 100)}") with { Unit = "% c" }
        };
        foreach (var (name, station) in section.Stations)
            rows.Add(new PropertyRow
            {
                Key = "sec:tc-" + name, Label = "t/c at " + name, Kind = RowKind.Fact, Unit = "%", Family = UnitFamily.Percent,
                Value = Quantity.PlacedPercent(station.StationThicknessRatio * 100)
            });
        var thickness = rows;
        rows = [];
        rows.Add(new PropertyRow
        {
            Key = "sec:intent", Label = "Station t/c", Kind = RowKind.Choice,
            Value = section.Mode.Draft.Intent == ThicknessIntent.UseSource ? "source" : "channel",
            Options = [new RowOption("channel", "From the Thickness curve"), new RowOption("source", "From this section")],
            AutomationName = "Station t/c"
        });
        rows.Add(Pair("sec:le", "LE radius own", facts.UpperLeRadius * 100, facts.LowerLeRadius * 100, "% c"));
        var first = section.Stations.Count > 0 ? section.Stations[0] : (section.Station, facts);
        bool same = section.Stations.All(item => Math.Abs(item.Facts.StationThicknessRatio - first.Item2.StationThicknessRatio) < 1e-12 &&
                                                Math.Abs(item.Facts.StationChordMeters - first.Item2.StationChordMeters) < 1e-12);
        foreach (var (name, station) in same ? [first] : section.Stations)
            rows.Add(Pair("sec:le-" + name, "LE radius at " + name, station.StationUpperLeRadius * 100, station.StationLowerLeRadius * 100, "% c"));
        string others = string.Join(" and ", section.Stations.Select(item => item.Station).Where(name => name != first.Item1));
        string leNote = same && others.Length > 0 ? $"Upper · lower. {others} is the same (same t/c)." : "Upper · lower.";
        var radius = rows;
        rows = [];
        rows.Add(Pair("sec:te-gap", $"TE gap own · {first.Item1}", facts.TrailingGap * 100, first.Item2.StationTrailingGap * 100, "% c"));
        rows.Add(Pair("sec:te-wedge", $"TE wedge own · {first.Item1}", facts.TrailingWedgeDegrees, first.Item2.StationTrailingWedgeDegrees, "°"));
        rows.Add(Prose("sec:points", "Points", $"{section.Upper.Points.Count} upper · {section.Lower.Points.Count} lower"));
        // The notes sit where the mockup draws them: under the t/c rows and under the LE radius rows (B continuations).
        return
        [
            new PropertyGroup("sec", "Section", $"{section.Mode.Draft.Profile} · {SourceName(section.Mode.Draft.Bytes, section.Mode.Draft.Profile)} · degree {degree}", true, thickness,
                [new RowMessage(ThicknessNote, MessageKind.Info)]),
            new PropertyGroup("sec-le", "Section", "", true, radius, [new RowMessage(leNote, MessageKind.Info)], Continues: true),
            new PropertyGroup("sec-te", "Section", "", true, rows, [], Continues: true)
        ];
    }

    public static string SourceName(byte[] foil, string profile)
    {
        string? raw = FoilSource.Parse(foil).Profile(profile)?.Provenance;
        var provenance = Provenance.Parse(raw);
        if (provenance.Origin is not { } origin) return "Source not recorded";
        int colon = origin.IndexOf(':');
        if (colon > 0 && origin.StartsWith("gen:", StringComparison.Ordinal))
        {
            try
            {
                var row = Catalog.Load().FirstOrDefault(entry => entry.Id == origin[(colon + 1)..]);
                if (row is not null) return row.Designation;
            }
            catch (ContractError) { }
        }
        return origin;
    }

    public static string SourceDisplay(byte[] foil, string profile)
    {
        string? raw = FoilSource.Parse(foil).Profile(profile)?.Provenance;
        var provenance = Provenance.Parse(raw);
        string display = provenance.ChipText(SourceName(foil, profile));
        return provenance.Rights == RightsClass.NotRecorded ? display : display + " (" + provenance.Rights.ToString().ToUpperInvariant() + ")";
    }

    private static PropertyRow Pair(string key, string label, double first, double second, string unit) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = unit,
        Value = $"{Quantity.Typed(first)} · {Quantity.Typed(second)}"
    };


    private static SelectionIdentity SectionOnly(SectionContext section, List<PropertyGroup> groups)
    {
        groups.AddRange(SectionGroups(section));
        return new SelectionIdentity(IdentityGlyph.Station, $"{section.Station} section", "Select a point to change it.");
    }

    // ---------------- the Wing (always last; never collapsible) ----------------

    private static (PropertyGroup Group, string? Status) Wing(AuthoredProjection projection, WingEstimates? estimates, ShellMode mode,
        PropertiesContext context)
    {
        double spanMeters = estimates?.SpanMeters ?? (projection.HalfSpanMeters ?? 0) * 2;
        var rows = new List<PropertyRow>();
        var notes = new List<RowMessage>();
        if (mode == ShellMode.SectionEditor)
        {
            foreach (var (key, label, meters) in new[] { ("w:span", "Span", spanMeters),
                         ("w:root", "Root chord", estimates?.RootChordMeters ?? double.NaN),
                         ("w:tip", "Tip chord", estimates?.TipChordMeters ?? double.NaN) })
                rows.Add(Length(key, label, meters, locked: true) with { HelperText = PropertyCopy.SetInWorkspace });
            notes.Add(new RowMessage(PropertyCopy.SetInWorkspace, MessageKind.Reason));
        }
        else
        {
            rows.Add(SpanField(spanMeters));
            rows.Add(LengthInput("w:root", "Root chord", estimates?.RootChordMeters ?? double.NaN, "Root chord in millimetres", null, nudge: false) with { MustBePositive = true });
            // COPY-108: a closing tip is a statement, not a dimension to type.
            rows.Add(estimates is not null && estimates.TipChordMeters <= 1e-9
                ? Prose("w:tip", "Tip chord", PropertyCopy.TipCloses) with { State = RowState.Locked }
                : LengthInput("w:tip", "Tip chord", estimates?.TipChordMeters ?? double.NaN, "Tip chord in millimetres", null, nudge: false) with { MustBePositive = true });
        }

        // Per-quantity availability (§10.1): one non-finite estimate never blanks its neighbours (BLANK-ESTIMATE).
        bool noArea = estimates is null || !double.IsFinite(estimates.AreaSquareMeters) || estimates.AreaSquareMeters <= 0;
        double Pick(Func<WingEstimates, double> value) => noArea ? double.NaN : value(estimates!);
        PropertyRow Row(string key, string label, Func<WingEstimates, double> value, Func<double, string> format, string unit)
        {
            double picked = Pick(value);
            return Estimate(key, label, format(picked), unit, double.IsFinite(picked));
        }
        var estimateRows = new[]
        {
            Row("e:mean", "Mean chord", e => e.MeanChordMeters, Quantity.DerivedLength, "mm"),
            Row("e:mac", "MAC", e => e.MacMeters, Quantity.DerivedLength, "mm"),
            Row("e:maxtc", "Max t/c", e => e.MaxThicknessRatio, Quantity.MaxThickness, "%"),
            Row("e:ar", "AR", e => e.AspectRatio, Quantity.AspectRatio, "b²/S") with { Dimensionless = true },
            Row("e:area", "Area", e => e.AreaSquareMeters, Quantity.AreaSquareCentimetres, "cm²")
        };
        rows.AddRange(estimateRows);
        var unavailable = estimateRows.Where(row => row.State == RowState.Unavailable).ToArray();
        bool allUnavailable = unavailable.Length == estimateRows.Length;
        string? status = null;
        if (allUnavailable)
        {
            string reason = estimates is null ? "the estimates could not be computed for this shape" : "the leading and trailing edges cross";
            notes.Add(new RowMessage(PropertyCopy.Unavailable(reason), MessageKind.Warning));
            status = PropertyCopy.UnavailableStatus("Estimates", reason);
        }
        else if (unavailable.Length > 0)
        {
            string what = Join(unavailable.Select(row => row.Label));
            notes.Add(new RowMessage(PropertyCopy.Unavailable(
                $"{what} did not converge for this shape; the other estimates are current"), MessageKind.Warning));
            status = PropertyCopy.UnavailableStatus(what, "did not converge for this shape");
        }
        GroupChip? chip = context.Preview ? GroupChip.Preview
            : context.Checking ? GroupChip.Checking
            : allUnavailable ? GroupChip.Unavailable
            : null;
        return (new PropertyGroup("wing", "Wing", "", false, rows, notes, chip), status);
    }

    // ---------------- row builders ----------------

    /// <summary>The Wing's Span field (b, the only quantity labelled "Span", MC-6).</summary>
    public static PropertyRow SpanField(double spanMeters) =>
        LengthInput("w:span", "Span", spanMeters, "Span in millimetres", null, nudge: false) with { MustBePositive = true };

    private static PropertyRow TypeRow(PointView point, CurveRows curve) => new()
    {
        Key = "p:type",
        Label = "Type",
        Kind = RowKind.Choice,
        Value = point.Role == PointRole.Anchor ? "anchor" : "control",
        Options = [new RowOption("control", PropertyCopy.ControlOption), new RowOption("anchor", PropertyCopy.AnchorOption)],
        Description = point.Role == PointRole.Anchor ? PropertyCopy.AnchorDescription
            : PropertyCopy.ControlDescription + PropertyCopy.AddsHandles.Replace("the rail", "the " + curve.Noun, StringComparison.Ordinal),
        AutomationName = "Type",
        Target = new PointRef(point.Curve, point.Id)
    };

    private static PropertyRow KindRow(PointView anchor, TangentKind kind, string name, bool readOnly) => new()
    {
        Key = "t:kind",
        Label = "Tangent kind",
        Kind = readOnly ? RowKind.Fact : RowKind.KindList,
        Value = kind.ToString(),
        Options = [new RowOption(nameof(TangentKind.Smooth), "Smooth"), new RowOption(nameof(TangentKind.Symmetric), "Symmetric"),
            new RowOption(nameof(TangentKind.Corner), "Corner")],
        Description = PropertyCopy.KindDescription(kind),
        AutomationName = name,
        State = readOnly ? RowState.Locked : RowState.Normal,
        Target = new PointRef(anchor.Curve, anchor.Id)
    };

    /// <summary>A fact that is words, not a quantity (Name, Type, Section, "Tip closes …").</summary>
    private static PropertyRow Prose(string key, string label, string value) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Value = value
    };

    /// <summary>A dimensionless quantity (η, a count, a degree).</summary>
    private static PropertyRow Count(string key, string label, string value) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Value = value, Dimensionless = true
    };

    private static PropertyRow Length(string key, string label, double meters, bool locked = false) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = "mm", Family = UnitFamily.Length,
        Value = double.IsFinite(meters) ? Quantity.TypedLength(meters) : "Unavailable",
        State = !double.IsFinite(meters) ? RowState.Unavailable : locked ? RowState.Locked : RowState.Normal
    };

    private static PropertyRow AngleFact(string key, string label, double degrees) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = "°", Family = UnitFamily.Angle, Value = Quantity.PlacedAngle(degrees)
    };

    private static PropertyRow Mixed(string key, string label, string unit, UnitFamily family) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = unit, Family = family, Value = "Mixed", State = RowState.Mixed
    };

    /// <summary>A curve's value (Aft, Height, Twist, t/c) as a typed field in the curve's display unit (§3.6).</summary>
    private static PropertyRow ValueInput(string key, string label, double ordinate, CurveRows curve, string name, PointRef target) => new()
    {
        Key = key, Label = label, Kind = RowKind.Input, Unit = curve.ValueUnit, Family = curve.ValueFamily,
        Value = double.IsFinite(ordinate) ? Value(ordinate, curve) : "", AutomationName = name, Nudge = true, Target = target,
        Axis = RowAxis.Value
    };

    private static PropertyRow ValueFact(string key, string label, double ordinate, CurveRows curve, bool locked) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = curve.ValueUnit, Family = curve.ValueFamily,
        Value = double.IsFinite(ordinate) ? Value(ordinate, curve) : "Unavailable",
        State = !double.IsFinite(ordinate) ? RowState.Unavailable : locked ? RowState.Locked : RowState.Normal
    };

    /// <summary>A handle's From root: a field when its freedom allows span motion, else a locked fact.</summary>
    private static PropertyRow SpanRow(string key, string label, PointView point, bool readOnly, string name) =>
        !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly
            ? LengthInput(key, label, point.SpanMeters, name, new PointRef(point.Curve, point.Id), nudge: true) with { Axis = RowAxis.Span }
            : Length(key, label, point.SpanMeters, locked: true);

    /// <summary>A handle's value: a field when its freedom allows value motion, else a locked fact.</summary>
    private static PropertyRow ValueRow(string key, string label, PointView point, CurveRows curve, bool readOnly, string name) =>
        !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.ValueOnly
            ? ValueInput(key, label, point.Ordinate, curve, name, new PointRef(point.Curve, point.Id))
            : ValueFact(key, label, point.Ordinate, curve, locked: true);

    private static PropertyRow HandleLength(string key, string label, double meters, string name, PointRef target) =>
        LengthInput(key, label, meters, name, target, nudge: true) with { MustBePositive = true, Axis = RowAxis.Length };

    private static PropertyRow Estimate(string key, string label, string value, string unit, bool available) => new()
    {
        Key = key, Label = label, Kind = RowKind.Estimate, Unit = unit,
        Value = available ? value : "Unavailable",
        State = available ? RowState.Normal : RowState.Unavailable
    };

    private static PropertyRow LengthInput(string key, string label, double meters, string name, PointRef? target, bool nudge) => new()
    {
        Key = key, Label = label, Kind = RowKind.Input, Unit = "mm", Family = UnitFamily.Length,
        Value = double.IsFinite(meters) ? Quantity.TypedLength(meters) : "",
        AutomationName = name, Nudge = nudge, Target = target
    };

    private static PropertyRow AngleInput(string key, string label, double degrees, string name, PointRef target) => new()
    {
        Key = key, Label = label, Kind = RowKind.Input, Unit = "°", Family = UnitFamily.Angle,
        Value = Quantity.PlacedAngle(degrees), AutomationName = name, Nudge = true, AngleBounded = true, Target = target,
        Axis = RowAxis.Angle
    };

    /// <summary>Locked and mixed facts carry their group's reason note as help text (§10.5).</summary>
    private static List<PropertyRow> Lock(List<PropertyRow> rows, List<RowMessage> notes)
    {
        string? reason = notes.FirstOrDefault(note => note.Kind == MessageKind.Reason)?.Text;
        return reason is null ? rows
            : rows.Select(row => row.State is RowState.Locked or RowState.Mixed && row.HelperText is null
                ? row with { HelperText = reason } : row).ToList();
    }

    private static string Join(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count <= 1 ? string.Concat(list) : string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1];
    }
}
