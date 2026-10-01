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
public enum GroupChip { Preview, Checking, Unavailable }

public sealed record RowMessage(string Text, MessageKind Kind);

public sealed record RowOption(string Value, string Text);

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
    public RowMessage? Message { get; init; }
    public string? AutomationName { get; init; }               // inputs, Type and Kind (§10.5)
    public string? HelperText { get; init; }                   // a locked fact's reason (§10.5 "Help text")
    public IReadOnlyList<RowOption>? Options { get; init; }
    public bool Nudge { get; init; }                           // point and handle fields only (DR-UID-2)
    public bool MustBePositive { get; init; }                  // a length that is refused at ≤ 0 (COPY-106)
    public bool AngleBounded { get; init; }                    // a rail or dihedral angle in (−90°, 90°) (COPY-158)
    public string? Subhead { get; init; }                      // "Handle toward the root" above a Corner handle's rows
    public PointRef? Target { get; init; }                     // the point an input or Kind list edits

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
    RowMessage? Lead = null);

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

/// <summary>What the model needs beyond the selection: the plan, and the shell's preview and checking states.</summary>
public sealed record PropertiesContext(
    PlanformView? Plan = null,
    bool Preview = false,
    bool Checking = false,
    bool NotChecked = false);

/// <summary>Copy rows the grid shows (DESIGN.md §7; docs/reviews/ui-property-grid.md §9).</summary>
public static class PropertyCopy
{
    public const string ControlDescription = "A control point pulls the curve toward it. The curve does not pass through it.";   // COPY-117
    public const string AnchorDescription = "An anchor point is on the curve. Its handles set the curve's direction on each side."; // COPY-149
    public const string Smooth = "Handles stay in line. Their lengths can differ.";                                                // COPY-150
    public const string Symmetric = "Handles stay in line and equal in length.";                                                   // COPY-151
    public const string Corner = "Each handle moves on its own. The curve can turn a corner here.";                                // COPY-152
    public const string AnchorOption = "Anchor point — adds handles (rail gains up to 3 points)";                                   // COPY-153
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

    public static string NotANumber(string field) => $"Enter a number. {field} is unchanged.";                                    // COPY-118
    public static string NotPositive(string field) => $"Enter a length greater than 0 mm. {field} is unchanged.";                 // COPY-106
    public static string AngleOutOfRange(string field) =>
        $"Enter an angle between −90° and 90°, from the span axis, + aft. {field} is unchanged.";                                 // COPY-158
    public static string UnavailableStatus(string what, string reason) => $"{what} unavailable — {reason}.";               // COPY-160
    public static string Unavailable(string reason) => $"Unavailable — {reason}. Undo, or edit again, to recompute.";             // COPY-155
    public static string PendingKind(string kind, string kept) =>
        $"Press Return or Space to make it {kind}, or Esc to keep {kept}.";                                                        // COPY-167
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
        string value = Quantity.Typed(entry.Value);
        string shown = unit == "°" ? value + "°" : value + " " + unit;
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

/// <summary>One curve's row set (§10.1, MC-17): rows are data, never layout.</summary>
public sealed record CurveRows(string Name, string ValueLabel, string ValueUnit, UnitFamily ValueFamily, string AngleLabel,
    bool HandlesByAngle);

public static class PropertiesView
{
    /// <summary>The per-curve table. M1.2b2 PNL fills the channel rows by data once Core publishes their points.</summary>
    public static readonly IReadOnlyDictionary<string, CurveRows> Curves = new Dictionary<string, CurveRows>(StringComparer.Ordinal)
    {
        ["leading"] = new("Leading edge", "Aft", "mm", UnitFamily.Length, "Angle", HandlesByAngle: true),
        ["trailing"] = new("Trailing edge", "Aft", "mm", UnitFamily.Length, "Angle", HandlesByAngle: true),
        ["dihedral"] = new("Dihedral", "Height", "mm", UnitFamily.Length, "Dihedral angle", HandlesByAngle: true),
        // Twist and Thickness handles are set by From root + value; their clamps are Core's (MC-19).
        ["twist"] = new("Twist", "Twist", "°", UnitFamily.Angle, "Twist", HandlesByAngle: false),
        ["thickness"] = new("Thickness", "t/c", "%", UnitFamily.Percent, "t/c", HandlesByAngle: false)
    };

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
        var groups = new List<PropertyGroup>();
        var identity = selection switch
        {
            Selection.Station station when station.Index >= 0 && station.Index < projection.Assignments.Count =>
                StationRows(station, projection, plan, groups),
            Selection.Points { Items.Count: > 1 } points when plan is not null => SeveralRows(points, plan, groups),
            Selection.Points { Items.Count: 1 } points when plan is not null && Find(plan, points.Items[0]) is { } point =>
                PointRows(point, plan, context.NotChecked, groups),
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

    public static PointView? Find(PlanformView plan, PointRef reference) =>
        Rail(plan, reference.Curve)?.Points.FirstOrDefault(point => point.Id == reference.VertexId);

    public static CurveView? Rail(PlanformView plan, string curve) => curve switch
    {
        "leading" => plan.Leading,
        "trailing" => plan.Trailing,
        _ => null
    };

    public static bool IsHandle(PointView point) =>
        point.Role is PointRole.RootHandle or PointRole.TipHandle or PointRole.AnchorHandle;

    /// <summary>A handle's direction from its anchor: the angle from the span axis (+ aft) and the length.</summary>
    public static (double AngleDegrees, double LengthMeters) HandleGeometry(PointView handle, PointView anchor)
    {
        double span = handle.SpanMeters - anchor.SpanMeters;
        double aft = handle.AftMeters - anchor.AftMeters;
        if (handle.Index < anchor.Index) { span = -span; aft = -aft; }
        return (Math.Atan2(aft, span) * 180 / Math.PI, Math.Sqrt(span * span + aft * aft));
    }

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
        List<PropertyGroup> groups)
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
        rows.Add(Prose("s:section", "Section", assignment.ProfileName));
        groups.Add(new PropertyGroup("stn", "Station", assignment.ProfileName, true, rows, []));
        string title = station.Eta == 0 ? "Root station" : station.Eta == 1 ? "Tip station" : $"Station {station.Index + 1}";
        return new SelectionIdentity(IdentityGlyph.Station, title, $"Station {station.Index + 1} of {projection.Assignments.Count}");
    }

    private static SelectionIdentity SeveralRows(Selection.Points points, PlanformView plan, List<PropertyGroup> groups)
    {
        var found = points.Items.Select(item => Find(plan, item)).OfType<PointView>().ToArray();
        var roles = found.Select(point => RoleText(point.Role)).Distinct().ToArray();
        string type = roles.Length == 1 ? roles[0] : "Mixed";
        groups.Add(new PropertyGroup("pos", "Position", "Mixed", true,
        [
            Prose("p:type", "Type", type) with { State = type == "Mixed" ? RowState.Mixed : RowState.Normal },
            Mixed("p:from", "From root"),
            Mixed("p:aft", "Aft")
        ], [new RowMessage(PropertyCopy.SelectOne, MessageKind.Reason)]));
        var curves = found.Select(point => point.Curve).Distinct().ToArray();
        string crumb = curves.Length == 1
            ? $"{Curves[curves[0]].Name} · points {Join(found.Select(point => (point.Index + 1).ToString(CultureInfo.InvariantCulture)))}"
            : Join(curves.Select(curve => Curves.TryGetValue(curve, out var rows) ? rows.Name : curve));
        return new SelectionIdentity(IdentityGlyph.Several, $"{points.Items.Count} points", crumb);
    }

    private static SelectionIdentity PointRows(PointView point, PlanformView plan, bool readOnly, List<PropertyGroup> groups)
    {
        var rail = Rail(plan, point.Curve)!;
        var curve = Curves[point.Curve];
        if (IsHandle(point)) return HandleRows(point, rail, curve, readOnly, groups);

        bool named = point.Role is not (PointRole.Control or PointRole.Anchor);
        var rows = new List<PropertyRow>
        {
            named || readOnly ? Prose("p:type", "Type", RoleText(point.Role)) with { State = RowState.Locked } : TypeRow(point)
        };
        bool spanFree = !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.SpanOnly;
        bool aftFree = !readOnly && point.Freedom is PointFreedom.Free or PointFreedom.AftOnly;
        var target = new PointRef(point.Curve, point.Id);
        rows.Add(spanFree
            ? LengthInput("p:from", "From root", point.SpanMeters, "From root, position along the span in millimetres", target, nudge: true)
            : Length("p:from", "From root", point.SpanMeters, locked: true));
        rows.Add(Count("p:eta", "η", Quantity.Eta(point.Eta)));
        bool teRoot = point.Curve == "trailing" && point.Role == PointRole.RootEnd;
        rows.Add(aftFree
            ? LengthInput("p:aft", curve.ValueLabel, point.AftMeters, $"{curve.ValueLabel} position in millimetres", target, nudge: true)
                with { Description = teRoot ? PropertyCopy.RootChordAuthority : null }
            : Length("p:aft", curve.ValueLabel, point.AftMeters, locked: true));
        var notes = new List<RowMessage>();
        string? lockNote = point.Role == PointRole.RootEnd && point.Curve == "leading" ? PropertyCopy.LeadingRootFixed
            : teRoot && point.Locks.Contains("root_mirror") ? PropertyCopy.TrailingRoot
            : point.Role == PointRole.TipEnd && point.Freedom == PointFreedom.AftOnly ? PropertyCopy.TipEndMoves
            : point.Freedom == PointFreedom.Fixed ? "Fixed."
            : null;
        if (lockNote is not null) notes.Add(new RowMessage(lockNote, MessageKind.Reason));
        string summary = point.Freedom == PointFreedom.Fixed ? "fixed"
            : $"{Quantity.TypedLength(point.SpanMeters)}, {Quantity.TypedLength(point.AftMeters)} mm";
        groups.Add(new PropertyGroup("pos", "Position", summary, true, Lock(rows, notes), notes));

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
        if (point.Role == PointRole.Anchor && point.Kind is { } kind)
        {
            var rows = new List<PropertyRow> { KindRow(point, kind, "Tangent kind", readOnly) };
            if (curve.HandlesByAngle && !readOnly) rows.AddRange(AnchorHandleRows(point, handles, kind, curve));
            return new PropertyGroup("tan", "Tangent", kind.ToString(), true, rows, [],
                Lead: new RowMessage(PropertyCopy.AngleReference, MessageKind.Info));
        }
        if (point.Role is not (PointRole.RootEnd or PointRole.TipEnd) || handles.Length != 1) return null;
        var handle = handles[0];
        var (angle, length) = HandleGeometry(handle, point);
        var target = new PointRef(handle.Curve, handle.Id);
        if (point.Role == PointRole.RootEnd && point.Locks.Contains("root_mirror"))
        {
            var mirrored = new List<PropertyRow> { Prose("t:kind", "Kind", "Square to the centre line") with { State = RowState.Locked } };
            if (!readOnly && handle.Freedom != PointFreedom.Fixed)
                mirrored.Add(LengthInput("h:length", "Handle length", length, "Handle length in millimetres", target, nudge: true) with { MustBePositive = true });
            return new PropertyGroup("tan", "Tangent", "root mirror", true, mirrored,
                [new RowMessage(PropertyCopy.RootMirrorHandle, MessageKind.Reason)]);
        }
        string title = point.Role == PointRole.TipEnd ? "Tip handle" : "Root handle";
        string which = point.Role == PointRole.TipEnd ? "the tip handle" : "the root handle";
        var endRows = readOnly
            ? new List<PropertyRow> { AngleFact("h:angle", curve.AngleLabel, angle), Length("h:length", "Length", length) }
            :
            [
                AngleInput("h:angle", curve.AngleLabel, angle, $"Angle of {which}, in degrees", target),
                LengthInput("h:length", "Length", length, $"Length of {which}, in millimetres", target, nudge: true) with { MustBePositive = true }
            ];
        return new PropertyGroup("tan", title, $"{Quantity.PlacedAngle(angle)}°, {Quantity.TypedLength(length)} mm", true, endRows, [],
            Lead: new RowMessage(PropertyCopy.AngleReference, MessageKind.Info));
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
                yield return LengthInput("h:root-length", "To root", rootLength, "To root, handle length in millimetres", rootRef, nudge: true) with { MustBePositive = true };
                yield return LengthInput("h:tip-length", "To tip", tipLength, "To tip, handle length in millimetres", tipRef, nudge: true) with { MustBePositive = true };
                break;
            case TangentKind.Symmetric:
                yield return AngleInput("h:angle", curve.AngleLabel, tipAngle, "Angle, both handles, in degrees", tipRef);
                yield return LengthInput("h:length", "Length", tipLength, "Length, both handles, in millimetres", tipRef, nudge: true) with { MustBePositive = true };
                break;
            default:
                yield return AngleInput("h:root-angle", curve.AngleLabel, rootAngle, "Angle of the handle toward the root, in degrees", rootRef)
                    with { Subhead = "Handle toward the root" };
                yield return LengthInput("h:root-length", "Length", rootLength, "Length of the handle toward the root, in millimetres", rootRef, nudge: true) with { MustBePositive = true };
                yield return AngleInput("h:tip-angle", curve.AngleLabel, tipAngle, "Angle of the handle toward the tip, in degrees", tipRef)
                    with { Subhead = "Handle toward the tip" };
                yield return LengthInput("h:tip-length", "Length", tipLength, "Length of the handle toward the tip, in millimetres", tipRef, nudge: true) with { MustBePositive = true };
                break;
        }
    }

    private static SelectionIdentity HandleRows(PointView handle, CurveView rail, CurveRows curve, bool readOnly, List<PropertyGroup> groups)
    {
        var anchor = rail.Points.First(item => item.Id == handle.AnchorId);
        var (angle, length) = HandleGeometry(handle, anchor);
        var target = new PointRef(handle.Curve, handle.Id);
        bool angleFree = !readOnly && handle.Freedom is PointFreedom.Free or PointFreedom.AftOnly;
        bool lengthFree = !readOnly && handle.Freedom != PointFreedom.Fixed;
        bool mirror = handle.Role == PointRole.RootHandle && handle.Freedom == PointFreedom.SpanOnly;
        var notes = new List<RowMessage>();
        if (mirror)
        {
            notes.Add(new RowMessage(PropertyCopy.RootHandleMoves, MessageKind.Reason));
            notes.Add(new RowMessage(PropertyCopy.RootMirrorHandle, MessageKind.Reason));
        }
        var rows = new List<PropertyRow>
        {
            angleFree
                ? AngleInput("h:angle", curve.AngleLabel, angle, "Angle in degrees, from the span axis, positive aft", target)
                : AngleFact("h:angle", curve.AngleLabel, angle) with { State = RowState.Locked },
            lengthFree
                ? LengthInput("h:length", "Length", length, "Length in millimetres", target, nudge: true) with { MustBePositive = true }
                : Length("h:length", "Length", length, locked: true)
        };
        string summary = mirror ? $"{Quantity.TypedLength(length)} mm" : $"{Quantity.PlacedAngle(angle)}°, {Quantity.TypedLength(length)} mm";
        groups.Add(new PropertyGroup("hdl", "Handle", summary, true, Lock(rows, notes), notes,
            Lead: mirror ? null : new RowMessage(PropertyCopy.AngleReference, MessageKind.Info)));

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
            Count("r:points", "Points", $"{rail.Points.Count} of {rail.Ceiling} max")
        ], []);
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

    private static PropertyRow TypeRow(PointView point) => new()
    {
        Key = "p:type",
        Label = "Type",
        Kind = RowKind.Choice,
        Value = point.Role == PointRole.Anchor ? "anchor" : "control",
        Options = [new RowOption("control", PropertyCopy.ControlOption), new RowOption("anchor", PropertyCopy.AnchorOption)],
        Description = point.Role == PointRole.Anchor ? PropertyCopy.AnchorDescription : PropertyCopy.ControlDescription,
        AutomationName = "Type",
        Target = new PointRef(point.Curve, point.Id)
    };

    private static PropertyRow KindRow(PointView anchor, TangentKind kind, string name, bool readOnly) => new()
    {
        Key = "t:kind",
        Label = "Kind",
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

    private static PropertyRow Mixed(string key, string label) => new()
    {
        Key = key, Label = label, Kind = RowKind.Fact, Unit = "mm", Family = UnitFamily.Length, Value = "Mixed", State = RowState.Mixed
    };

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
        Value = Quantity.PlacedAngle(degrees), AutomationName = name, Nudge = true, AngleBounded = true, Target = target
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
