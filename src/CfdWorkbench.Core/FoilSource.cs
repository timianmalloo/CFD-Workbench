using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

public sealed record Diagnostic(string Code, string Phase, string Severity, int ByteStart, int ByteLength,
    int Line, int ScalarColumn, string? Entity, string Reason, string Recovery);

/// <summary>Owned static-language projection; success does not certify geometry.</summary>
public sealed class SourceParse
{
    private readonly byte[] source;
    private readonly Lazy<string> sourceIdentity;
    private readonly Lazy<string?> surfaceIdentity;
    internal Definition? Definition { get; }
    internal SourceParse(byte[] source, IEnumerable<Diagnostic> diagnostics, Definition? definition = null)
    {
        this.source = source.ToArray();
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        Definition = definition;
        sourceIdentity = new(() => Identity.Sha256(this.source));
        surfaceIdentity = new(() => definition is null ? null : Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(definition.Semantic))));
    }
    public byte[] Source => source.ToArray();
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool IsParsed => Definition is not null && Diagnostics.Count == 0;
    public string SourceHash => sourceIdentity.Value;
    public string? SurfaceHash => surfaceIdentity.Value;
    public AuthoredProjection Authored()
    {
        var definition = Definition;
        if (definition is null) return new(new(SourceHash, "Invalid", null, null, null, null, null, null, null), null, null, null, [], [], [], Diagnostics);
        bool candidate = definition.Curves.Values.Any(curve => curve.MissingIds);
        string unit = definition.UnitScale switch { -3 => "mm", -2 => "cm", _ => "m" };
        var rails = new List<AuthoredRail>();
        foreach (string name in new[] { "leading", "trailing" })
        {
            if (!definition.Curves.TryGetValue(name, out var curve)) continue;
            var controls = curve.Points.Select((point, index) =>
            {
                string id = curve.Ids[index];
                string[] locks = definition.Locks.Where(item => item.Channel.Text == name &&
                    (item.Kind == "root_mirror" ? index < 2 : item.Id is null || item.Id.String == id)).Select(item => item.Kind).Distinct(StringComparer.Ordinal).ToArray();
                return new AuthoredControl(id, point[0], point[1], Rational.From(point[1]).Exact,
                    !candidate && !locks.Contains("root_mirror") && !locks.Contains("freeze"), Array.AsReadOnly(locks));
            }).ToArray();
            rails.Add(new(name, unit, Array.AsReadOnly(controls)));
        }
        var profileIdentities = definition.Profiles.Select(profile => Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(profile.Semantic)))).ToArray();
        var assignments = definition.Assignments.Select(item => new AuthoredAssignment(item.Eta, item.Eta * definition.HalfSpan,
            definition.Profiles[item.Profile].Name, profileIdentities[item.Profile]));
        var constraints = definition.Locks.Select(item =>
        {
            int scale = item.Channel.Text is "leading" or "trailing" or "dihedral" ? definition.UnitScale : 0;
            double? eta = item.Station is null ? null : StationEta(item.Station, definition.HalfSpan);
            double[] values = item.Values.Select((value, index) => DecimalSi.Parse(value.Text, item.Kind == "freeze" && index == 0 ? 0 : scale)).ToArray();
            return new AuthoredConstraint(item.Kind, item.Channel.Text, item.Id?.String, eta, Array.AsReadOnly(values));
        });
        return new(new(SourceHash, candidate ? "IdCandidate" : "Parsed", null, null, SurfaceHash, "cfdw-cv/2", null, null, null),
            definition.Name, definition.Kind == "foil" ? unit : null, definition.Kind == "foil" ? definition.HalfSpan : null, rails, assignments, constraints, Diagnostics,
            definition.Assertions.Select(item => new AuthoredAssertion(item.Metric.Text, item.Comparison, QuantitySi(item.Value), item.Value.Unit?.Text,
                item.Tolerance is null ? null : QuantitySi(item.Tolerance))));
    }
    private static double QuantitySi(QuantitySource quantity) => DecimalSi.Parse(quantity.Number.Text,
        quantity.Unit?.Text switch { "mm" => -3, "cm" => -2, "mm2" => -6, "cm2" => -4, null or "m" or "m2" => 0, _ => throw new ContractError("DSL-UNIT") });
    private static double StationEta(StationSource station, double halfSpan)
    {
        if (station.Value.Text is "root" or "center") return 0;
        if (station.Value.Text == "tip") return 1;
        return station.Unit!.Text == "%" ? DecimalSi.Parse(station.Value.Text, -2) :
            DecimalSi.Parse(station.Value.Text, station.Unit.Text switch { "mm" => -3, "cm" => -2, "m" => 0, _ => throw new ContractError("DSL-UNIT") }) / halfSpan;
    }
}

internal sealed record SourceToken(string Text, int Start, int End)
{
    internal string String => JsonSerializer.Deserialize<string>(Text) ?? "";
}
internal readonly record struct TangentDraft(SourceToken Id, SourceToken Kind, SourceToken? Angle);
internal readonly record struct TangentRow(string Id, string Kind, double? Angle);
internal sealed record RawCurve(string Path, SourceToken Degree, SourceToken[] Knots,
    (SourceToken X, SourceToken Y)[] Points, SourceToken[]? Ids, int InsertAt, bool Profile, int PointsStart, int PointsEnd)
{
    internal TangentDraft[] Tangents { get; init; } = [];
}
internal sealed record ProfileSource(SourceToken Name, RawCurve? Upper, RawCurve? Lower, string Closure, SourceToken? Asset, int BlockStart = 0, int BlockEnd = 0)
{
    internal string? Provenance { get; init; }
}
internal sealed record StationSource(SourceToken Value, SourceToken? Unit);
internal sealed record AssignmentSource(StationSource Station, SourceToken Profile);
internal sealed record LockSource(string Kind, SourceToken Channel, SourceToken? Id, StationSource? Station, SourceToken[] Values);
internal sealed record QuantitySource(SourceToken Number, SourceToken? Unit);
internal sealed record AssertionSource(SourceToken Metric, string Comparison, QuantitySource Value, QuantitySource? Tolerance);
internal sealed record Curve(string Path, int Degree, double[] Knots, double[][] Points, string[] Ids,
    SourceToken[] Ordinates, int InsertAt, bool MissingIds, SourceToken[] Abscissae, SourceToken[]? IdTokens,
    int KnotStart, int KnotEnd, int PointsStart, int PointsEnd)
{
    internal TangentRow[] Tangents { get; init; } = [];
    internal object Semantic() => new Dictionary<string, object?>
    { ["degree"] = Degree, ["knots"] = Knots, ["points"] = Points.Select(point => new[] { point[0], point[1] }).ToArray() };
}
internal sealed record ProfileDefinition(string Name, Curve Upper, Curve Lower, string Closure, int BlockStart, int BlockEnd)
{
    internal string? Provenance { get; init; }
    internal object Semantic => new Dictionary<string, object?>
    { ["evaluator"] = new[] { "cfdw-cv", "2" }, ["upper"] = Upper.Semantic(), ["lower"] = Lower.Semantic(), ["closure"] = Closure };
}
internal sealed record Definition(string Kind, int UnitScale, double HalfSpan, Dictionary<string, Curve> Curves,
    ProfileDefinition[] Profiles, (double Eta, int Profile)[] Assignments, string Tip, LockSource[] Locks,
    AssertionSource[] Assertions, object Semantic, string Name, SourceToken[] AssignmentProfiles)
{
    internal string Version { get; init; } = "4.0";
}
internal sealed class SourceFailure(string code, string phase, SourceToken token, string? entity = null, string? reason = null) : Exception(code)
{
    internal string Code { get; } = code;
    internal string Phase { get; } = phase;
    internal SourceToken Token { get; } = token;
    internal string? Entity { get; } = entity;
    internal string? Reason { get; } = reason;
}

public static class FoilSource
{
    internal static readonly UTF8Encoding Utf8 = new(false, true);
    public static SourceParse Parse(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length > 1_048_576)
            return new([], [new("DSL-LIMIT", "Resource", "Error", 0, 0, 1, 1, null, "Source exceeds 1 MiB.", "Retain the original input and reduce its size.")]);
        source = source.ToArray();
        string text;
        try { text = Utf8.GetString(source); }
        catch (DecoderFallbackException)
        { return new(source, [new("DSL-LEX", "Lexical", "Error", 0, 0, 1, 1, null, "Input is not valid UTF-8.", "Repair the source encoding.")]); }
        try
        {
            var grammar = new Grammar(text);
            grammar.ReadDocument();
            return new(source, [], grammar.Validate());
        }
        catch (SourceFailure failure)
        {
            int start = Utf8.GetByteCount(text.AsSpan(0, failure.Token.Start));
            int length = Utf8.GetByteCount(text.AsSpan(failure.Token.Start, failure.Token.End - failure.Token.Start));
            var location = Location(source, start);
            return new(source, [new(failure.Code, failure.Phase, "Error", start, length, location.Line,
                location.Column, failure.Entity, failure.Reason ?? "Source does not satisfy the " + failure.Phase.ToLowerInvariant() + " contract.", "Repair the indicated source span and validate again.")]);
        }
    }
    public static (int Line, int Column) Location(byte[] source, int byteOffset)
    {
        Guard.Require(byteOffset >= 0 && byteOffset <= source.Length, "DSL-RANGE");
        string prefix;
        try { prefix = Utf8.GetString(source.AsSpan(0, byteOffset)); }
        catch (DecoderFallbackException) { throw new ContractError("DSL-RANGE"); }
        int line = 1, column = 1; bool previousCr = false;
        foreach (var rune in prefix.EnumerateRunes())
        {
            if (rune.Value == '\r') { line++; column = 1; previousCr = true; }
            else if (rune.Value == '\n') { if (!previousCr) line++; column = 1; previousCr = false; }
            else { column++; previousCr = false; }
        }
        return (line, column);
    }
    public static byte[] EnsureHeader41(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        string text;
        try { text = Utf8.GetString(source); }
        catch (DecoderFallbackException) { throw new ContractError("DSL-LEX"); }
        Grammar grammar;
        try { grammar = new Grammar(text); }
        catch (SourceFailure failure) { throw new ContractError(failure.Code); }
        SourceToken version;
        try { version = grammar.VersionToken(); }
        catch (SourceFailure) { throw new ContractError("DSL-VERSION"); }
        if (version.String == "4.1") return source;
        if (version.String != "4.0") throw new ContractError("DSL-VERSION");
        int zero = Utf8.GetByteCount(text.AsSpan(0, version.Start + 3));
        if ((uint)zero >= (uint)source.Length || source[zero] != (byte)'0') throw new ContractError("DSL-VERSION");
        byte[] bytes = source.ToArray();
        bytes[zero] = (byte)'1';
        return bytes;
    }

    internal static byte[] Print(Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        string unit = definition.UnitScale switch { -3 => "mm", -2 => "cm", 0 => "m", _ => throw new ContractError("DSL-UNIT") };
        var text = new StringBuilder();
        text.Append("foildsl \"").Append(definition.Version).Append("\"\n");
        text.Append("foil ").Append(Jcs.Quote(definition.Name)).Append(" {\n");
        text.Append("  units ").Append(unit).Append('\n');
        text.Append("  half_span ").Append(ExactDecimal(definition.HalfSpan, definition.UnitScale)).Append(' ').Append(unit).Append('\n');
        text.Append("  evaluator \"cfdw-cv\" \"2\"\n");
        text.Append("  symmetry mirror_y\n");
        text.Append("  planform {\n");
        text.Append("    leading cv { ").Append(CurveBody(definition.Curves["leading"], definition.UnitScale)).Append(" }\n");
        text.Append("    trailing cv { ").Append(CurveBody(definition.Curves["trailing"], definition.UnitScale)).Append(" }\n");
        text.Append("  }\n");
        text.Append("  dihedral cv { ").Append(CurveBody(definition.Curves["dihedral"], definition.UnitScale)).Append(" }\n");
        text.Append("  twist cv { ").Append(CurveBody(definition.Curves["twist"], 0)).Append(" }\n");
        text.Append("  thickness cv { ").Append(CurveBody(definition.Curves["thickness"], 0)).Append(" }\n");
        text.Append("  profiles {\n");
        foreach (var profile in definition.Profiles)
        {
            text.Append("    profile ").Append(Jcs.Quote(profile.Name)).Append(" {\n");
            text.Append("      upper cv { ").Append(CurveBody(profile.Upper, 0)).Append(" }\n");
            text.Append("      lower cv { ").Append(CurveBody(profile.Lower, 0)).Append(" }\n");
            text.Append("      closure ").Append(profile.Closure).Append('\n');
            if (profile.Provenance is not null)
                text.Append("      provenance ").Append(Jcs.Quote(profile.Provenance)).Append('\n');
            text.Append("    }\n");
        }
        text.Append("  }\n");
        text.Append("  sections {");
        foreach (var assignment in definition.Assignments)
        {
            string station = assignment.Eta == 0 ? "root" : assignment.Eta == 1 ? "tip" : ExactDecimal(assignment.Eta) + " " + unit;
            text.Append(" at ").Append(station).Append(" profile ").Append(Jcs.Quote(definition.Profiles[assignment.Profile].Name));
        }
        text.Append(" }\n");
        text.Append("  tip ").Append(definition.Tip).Append('\n');
        if (definition.Locks.Length > 0)
        {
            text.Append("  locks {\n");
            foreach (var item in definition.Locks)
            {
                text.Append("    ").Append(item.Kind).Append(' ').Append(item.Channel.Text);
                if (item.Id is not null) text.Append(' ').Append(item.Id.Text);
                if (item.Station is not null)
                {
                    text.Append(" at ").Append(item.Station.Value.Text);
                    if (item.Station.Unit is not null) text.Append(' ').Append(item.Station.Unit.Text);
                }
                foreach (var value in item.Values) text.Append(' ').Append(value.Text);
                text.Append('\n');
            }
            text.Append("  }\n");
        }
        if (definition.Assertions.Length > 0)
        {
            text.Append("  constrain {\n");
            foreach (var item in definition.Assertions)
            {
                text.Append("    ").Append(item.Metric.Text).Append(' ').Append(item.Comparison).Append(' ').Append(item.Value.Number.Text);
                if (item.Value.Unit is not null) text.Append(' ').Append(item.Value.Unit.Text);
                if (item.Tolerance is not null)
                {
                    text.Append(" tolerance ").Append(item.Tolerance.Number.Text);
                    if (item.Tolerance.Unit is not null) text.Append(' ').Append(item.Tolerance.Unit.Text);
                }
                text.Append('\n');
            }
            text.Append("  }\n");
        }
        text.Append("}\n");
        return Utf8.GetBytes(text.ToString());
    }

    internal static bool IsAnchor(double[] knots, int count, int degree, int index)
    {
        if (index < degree || index > count - degree - 1) return false;
        double knot = knots[index + 1];
        for (int offset = 2; offset <= degree; offset++)
            if (knots[index + offset] != knot) return false;
        if (knots[index] == knot || knots[index + degree + 1] == knot) return false;
        return knot > 0 && knot < 1;
    }

    internal static byte[] WriteSurfaces(byte[] source, string profile, double[] knots, double[][] upperPoints, string[] upperIds, double[][] lowerPoints, string[] lowerIds)
    {
        var (_, found) = ProfileOf(source, profile);
        return RewriteCurves(source, found, knots, upperPoints, upperIds, lowerPoints, lowerIds);
    }

    internal static byte[] WriteSideTangents(byte[] source, string profileName, SurfaceSide side, TangentRow[] rows)
    {
        if (rows.Length > 0) source = EnsureHeader41(source);
        var (_, profile) = ProfileOf(source, profileName);
        var curve = side == SurfaceSide.Upper ? profile.Upper : profile.Lower;
        string text = Utf8.GetString(source);
        string body = FormatTangents(rows);
        int window = Math.Min(curve.PointsEnd, curve.InsertAt);
        int at = text.IndexOf("tangents", window, curve.InsertAt - window, StringComparison.Ordinal);
        if (at >= 0)
        {
            int open = text.IndexOf('{', at);
            int close = text.IndexOf('}', open);
            int start = at > 0 && text[at - 1] == ' ' ? at - 1 : at;
            text = string.Concat(text.AsSpan(0, start), body, text.AsSpan(close + 1));
        }
        else if (body.Length > 0)
            text = text.Insert(curve.InsertAt, body);
        else
            return source;
        byte[] candidate = Utf8.GetBytes(text);
        var parsed = Parse(candidate);
        if (!parsed.IsParsed)
            throw new ContractError(parsed.Diagnostics.Count == 0 ? "DSL-PATCH" : parsed.Diagnostics[0].Code,
                parsed.Diagnostics.Count == 0 ? "Tangent rows did not parse." : parsed.Diagnostics[0].Reason);
        return candidate;
    }

    internal static (double[] Knots, double[][] Points, int Inserted) InsertOnce(double[] knots, double[][] points, int degree, double t) =>
        InsertKnot(knots, points, degree, t);

    internal static (double[] Knots, double[][] Points, int Removed) RemoveOnce(double[] knots, double[][] points, int degree, int r)
    {
        int removed = RemovedControl(knots, degree, r);
        var kept = RemoveKnot(points, knots, degree, r);
        var next = new double[knots.Length - 1];
        Array.Copy(knots, next, r);
        Array.Copy(knots, r + 1, next, r, knots.Length - r - 1);
        return (next, kept, removed);
    }

    internal static int SelectRemovable(double[] knots, int degree, double knot)
    {
        int last = -1;
        for (int index = 0; index < knots.Length; index++)
            if (knots[index] == knot) last = index;
        if (last < 0) throw new ContractError("DSL-CURVE", "The anchor knot is not in the vector.");
        for (int r = last; r >= 0 && knots[r] == knot; r--)
        {
            int multiplicity = 1;
            int scan = r;
            while (scan > 0 && knots[scan - 1] == knot) { multiplicity++; scan--; }
            int numer = 2 * r - multiplicity - degree;
            if (numer % 2 == 0 && numer >= 0) return r;
        }
        throw new ContractError("DSL-CURVE", "The anchor knot cannot be removed.");
    }

    private static int RemovedControl(double[] knots, int degree, int r)
    {
        double knot = knots[r];
        int multiplicity = 1;
        int scan = r;
        while (scan > 0 && knots[scan - 1] == knot) { multiplicity++; scan--; }
        int numer = 2 * r - multiplicity - degree;
        if (numer % 2 != 0) throw new ContractError("DSL-CURVE", "Knot removal index is not an integer.");
        return numer / 2;
    }

    private static string FormatTangents(TangentRow[] rows)
    {
        if (rows.Length == 0) return "";
        var parts = rows.Select(row => Jcs.Quote(row.Id) + " " + row.Kind + (row.Angle is double angle ? " " + ExactDecimal(angle) : ""));
        return " tangents { " + string.Join(" ", parts) + " }";
    }

    private static string CurveBody(Curve curve, int scale)
    {
        var text = new StringBuilder();
        text.Append("degree ").Append(curve.Degree.ToString(CultureInfo.InvariantCulture));
        text.Append(" knots [").Append(string.Join(", ", curve.Knots.Select(knot => ExactDecimal(knot)))).Append(']');
        text.Append(" points [").Append(string.Join(", ", curve.Points.Select(point => "(" + ExactDecimal(point[0]) + ", " + ExactDecimal(point[1], scale) + ")"))).Append(']');
        text.Append(" ids [").Append(string.Join(", ", curve.Ids.Select(Jcs.Quote))).Append(']');
        if (curve.Tangents.Length > 0)
        {
            text.Append(" tangents { ");
            text.Append(string.Join(" ", curve.Tangents.Select(row => Jcs.Quote(row.Id) + " " + row.Kind + (row.Angle is double angle ? " " + ExactDecimal(angle) : ""))));
            text.Append(" }");
        }
        return text.ToString();
    }

    public static byte[] MaterializeIds(SourceParse parsed)
    {
        var definition = parsed.Definition ?? throw new ContractError("DSL-PATCH");
        string text = Utf8.GetString(parsed.Source);
        foreach (var curve in definition.Curves.Values.Where(curve => curve.MissingIds).OrderByDescending(curve => curve.InsertAt))
            text = text.Insert(curve.InsertAt, " ids [" + string.Join(",", curve.Ids.Select(Jcs.Quote)) + "] ");
        byte[] candidate = Utf8.GetBytes(text);
        var result = Parse(candidate);
        Guard.Require(result.IsParsed && result.SurfaceHash == parsed.SurfaceHash, "DSL-PATCH");
        return candidate;
    }

    public static byte[] PatchSpan(byte[] source, string spanMillimetres)
    {
        ArgumentNullException.ThrowIfNull(source);
        var parsed = Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" })
            throw new ContractError(parsed.Diagnostics.Count > 0 ? parsed.Diagnostics[0].Code : "DSL-PATCH");
        string text = Utf8.GetString(parsed.Source);
        var grammar = new Grammar(text);
        grammar.ReadDocument();
        foreach (var edit in grammar.SpanEdits(spanMillimetres).OrderByDescending(item => item.Start))
            text = text[..edit.Start] + edit.Value + text[edit.End..];
        byte[] candidate = Utf8.GetBytes(text);
        var result = Parse(candidate);
        Guard.Require(result.IsParsed && result.Definition is { Kind: "foil" }, "DSL-PATCH");
        return candidate;
    }

    internal static byte[] PatchChannelOrdinates(byte[] source, double[] ordinates)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(ordinates);
        var parsed = Parse(source);
        var definition = parsed.Definition ?? throw new ContractError("DSL-PATCH");
        if (!definition.Curves.TryGetValue("thickness", out var curve) || curve.MissingIds) throw new ContractError("DSL-PATCH");
        Guard.Require(ordinates.Length == curve.Points.Length && ordinates.All(double.IsFinite), "DSL-PATCH");
        string text = Utf8.GetString(parsed.Source);
        var edits = new List<(int Start, int End, string Value)>();
        for (int index = 0; index < ordinates.Length; index++)
        {
            if (SameBits(curve.Points[index][1], ordinates[index])) continue;
            edits.Add((curve.Ordinates[index].Start, curve.Ordinates[index].End, ExactDecimal(ordinates[index], 0)));
        }
        foreach (var edit in edits.OrderByDescending(item => item.Start))
            text = text[..edit.Start] + edit.Value + text[edit.End..];
        if (edits.Count == 0) return parsed.Source;
        byte[] candidate = Utf8.GetBytes(text);
        var result = Parse(candidate);
        var patched = result.Definition?.Curves["thickness"];
        Guard.Require(result.IsParsed && patched is not null, "DSL-PATCH");
        for (int index = 0; index < ordinates.Length; index++)
            Guard.Require(SameBits(patched!.Points[index][1], ordinates[index]) && SameBits(patched.Points[index][0], curve.Points[index][0]), "DSL-PATCH");
        return candidate;
    }

    public static byte[] PatchProfilePoint(byte[] source, string profile, string side, string vertexId, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(source);
        var parsed = Parse(source);
        var definition = parsed.Definition ?? throw new ContractError("DSL-PROFILE-TARGET");
        var found = definition.Profiles.FirstOrDefault(item => item.Name == profile) ?? throw new ContractError("DSL-PROFILE-TARGET");
        Guard.Require(side is "upper" or "lower", "DSL-PROFILE-TARGET");
        var curve = side == "upper" ? found.Upper : found.Lower;
        int index = Array.IndexOf(curve.Ids, vertexId);
        Guard.Require(index >= 0, "DSL-PROFILE-TARGET");
        Guard.Require(double.IsFinite(x) && double.IsFinite(y), "DSL-PROFILE-ORDER");
        string text = Utf8.GetString(parsed.Source);
        (int Start, int End, string Value)[] edits = [(curve.Abscissae[index].Start, curve.Abscissae[index].End, ExactDecimal(x, 0)),
            (curve.Ordinates[index].Start, curve.Ordinates[index].End, ExactDecimal(y, 0))];
        foreach (var edit in edits.OrderByDescending(item => item.Start)) text = text[..edit.Start] + edit.Value + text[edit.End..];
        byte[] candidate = Utf8.GetBytes(text);
        var result = Parse(candidate);
        var patched = result.Definition?.Profiles.FirstOrDefault(item => item.Name == profile);
        var patchedCurve = side == "upper" ? patched?.Upper : patched?.Lower;
        Guard.Require(result.IsParsed && patchedCurve is not null && SameBits(patchedCurve.Points[index][0], x) && SameBits(patchedCurve.Points[index][1], y), "DSL-PATCH");
        return candidate;
    }

    public static (byte[] Source, string NewProfile) MakeIndependent(byte[] source, string profile, int assignmentIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        var parsed = Parse(source);
        var definition = parsed.Definition ?? throw new ContractError("DSL-PROFILE-TARGET");
        var found = definition.Profiles.FirstOrDefault(item => item.Name == profile) ?? throw new ContractError("DSL-PROFILE-TARGET");
        Guard.Require(assignmentIndex >= 0 && assignmentIndex < definition.Assignments.Length && found.BlockEnd > found.BlockStart &&
            definition.Profiles[definition.Assignments[assignmentIndex].Profile].Name == profile, "DSL-PROFILE-TARGET");
        var names = definition.Profiles.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        string newName = "";
        for (int k = 1; ; k++)
        {
            newName = profile + "-i" + k.ToString(CultureInfo.InvariantCulture);
            if (!names.Contains(newName)) break;
            Guard.Require(k < 100000, "DSL-LIMIT");
        }
        string text = Utf8.GetString(parsed.Source);
        int newline = text.LastIndexOf('\n', found.BlockStart);
        string indent = newline < 0 ? "" : text[(newline + 1)..found.BlockStart];
        string insertion = "\n" + indent + RewriteProfile(text, found, newName);
        var token = definition.AssignmentProfiles[assignmentIndex];
        Guard.Require(token.Start >= found.BlockEnd, "DSL-PROFILE-TARGET");
        string result = text[..found.BlockEnd] + insertion + text[found.BlockEnd..token.Start] + Jcs.Quote(newName) + text[token.End..];
        byte[] candidate = Utf8.GetBytes(result);
        Guard.Require(Parse(candidate).IsParsed, "DSL-PATCH");
        return (candidate, newName);
    }

    public static ImportedProfile ImportDat(byte[] bytes, string name) => DatImport.Fit(DatImport.Parse(bytes), name);

    /// <summary>
    /// Untitled symmetric NACA 0012 wing: span 1 m, area 0.1 m², open tip, no twist, no dihedral.
    /// Leading and trailing rails are degree 3 with 4 vertices (Ruling 64, FoilDSL 4.1).
    /// Dihedral, twist and thickness stay at 10. Power 3 clusters the 10-point knots at the tip.
    /// The section is the degree-5 sqrt basis used by <see cref="DatImport"/>, at 10 vertices.
    /// </summary>
    public static byte[] NewDefault()
    {
        const int profileCount = 10;
        const int railCount = 4;
        const int channelCount = 10;
        const double tipPower = 3;
        var (profileKnots, profileX) = SqrtProfileBasis(profileCount);
        double[] upper = FitNaca(profileKnots, profileCount);
        double[] lower = upper.Select(value => -value).ToArray();
        lower[0] = 0;
        lower[^1] = 0;
        var (railKnots, railX) = TipClusteredChannel(railCount, tipPower);
        var (channelKnots, channelX) = TipClusteredChannel(channelCount, tipPower);
        const int samples = 401;
        var eta = new double[samples];
        var leadTarget = new double[samples];
        var trailTarget = new double[samples];
        for (int index = 0; index < samples; index++)
        {
            eta[index] = index / (double)(samples - 1);
            double chord = UnitChord(eta[index]);
            leadTarget[index] = (1 - chord) / 4;
            trailTarget[index] = leadTarget[index] + chord;
        }
        double[] lead = FitChannel(railKnots, eta, leadTarget, [(0, 0d), (1, 0d), (railCount - 1, (1 - UnitChord(1)) / 4)]);
        double[] trail = FitChannel(railKnots, eta, trailTarget, [(0, 1d), (1, 1d), (railCount - 1, (1 - UnitChord(1)) / 4 + UnitChord(1))]);
        double scale = 1;
        byte[] source = [];
        for (int step = 0; step < 4; step++)
        {
            source = DefaultDocument(railKnots, railX, Scale(lead, scale), Scale(trail, scale), channelKnots, channelX, profileKnots, profileX, upper, lower);
            var parsed = Parse(source);
            if (!parsed.IsParsed) throw new ContractError(parsed.Diagnostics[0].Code);
            double area = WingEstimates.From(source, "accepted", 0).AreaSquareMeters;
            if (Math.Abs(area - 0.1) / 0.1 <= 1e-12) return source;
            scale *= 0.1 / area;
        }
        throw new ContractError("DSL-GEOMETRY");
    }

    private static double UnitChord(double eta) => Math.Sqrt(Math.Max(0, 1 - 0.99 * eta * eta));

    private static double[] Scale(double[] values, double factor) => values.Select(value => value * factor).ToArray();

    private static double[] FitNaca(double[] knots, int count)
    {
        const int samples = 401;
        var parameter = new double[samples];
        var target = new double[samples];
        for (int index = 0; index < samples; index++)
        {
            double x = 0.5 * (1 - Math.Cos(Math.PI * index / (samples - 1)));
            parameter[index] = Math.Sqrt(Math.Max(0, x));
            target[index] = Naca0012(x);
        }
        return FitOrdinates(knots, 5, parameter, target, [(0, 0d), (count - 1, 0d)]);
    }

    private static double Naca0012(double x)
    {
        if (x <= 0 || x >= 1) return 0;
        double root = Math.Sqrt(x);
        double square = x * x;
        return 0.6 * (0.2969 * root - 0.1260 * x - 0.3516 * square + 0.2843 * square * x - 0.1036 * square * square);
    }

    private static double[] FitChannel(double[] knots, double[] parameter, double[] target, (int Index, double Value)[] pins) =>
        FitOrdinates(knots, 3, parameter, target, pins);

    private static double[] FitOrdinates(double[] knots, int degree, double[] parameter, double[] target, (int Index, double Value)[] pins)
    {
        int width = knots.Length - degree - 1;
        var design = new double[target.Length, width];
        var weight = new double[target.Length];
        for (int row = 0; row < target.Length; row++)
        {
            weight[row] = 1;
            double[] basis = SplineBasis.Values(knots, degree, parameter[row]);
            for (int column = 0; column < width; column++) design[row, column] = basis[column];
        }
        var constraints = new double[pins.Length, width];
        var bound = new double[pins.Length];
        for (int index = 0; index < pins.Length; index++)
        {
            constraints[index, pins[index].Index] = 1;
            bound[index] = pins[index].Value;
        }
        var smoothing = new double[width, width];
        double[] solved = ConstrainedFit.Solve(design, target, weight, smoothing, 0, constraints, bound);
        foreach (var pin in pins) solved[pin.Index] = pin.Value;
        return solved;
    }

    private static (double[] Knots, double[] X) SqrtProfileBasis(int count)
    {
        var knots = new double[count + 6];
        for (int index = 0; index <= 5; index++) knots[index] = 0;
        int interior = count - 6;
        for (int index = 1; index <= interior; index++) knots[5 + index] = (double)index / (interior + 1);
        for (int index = knots.Length - 6; index < knots.Length; index++) knots[index] = 1;
        var controlX = new double[count];
        for (int index = 0; index < count; index++)
        {
            double sum = 0;
            for (int left = 1; left <= 5; left++)
                for (int right = left + 1; right <= 5; right++)
                    sum += knots[index + left] * knots[index + right];
            controlX[index] = sum / 10;
        }
        controlX[0] = 0;
        controlX[1] = 0;
        controlX[^1] = 1;
        return (knots, controlX);
    }

    private static (double[] Knots, double[] X) TipClusteredChannel(int count, double power)
    {
        const int degree = 3;
        int interior = count - degree - 1;
        var knots = new double[count + degree + 1];
        for (int index = 0; index <= degree; index++) knots[index] = 0;
        for (int index = 1; index <= interior; index++)
        {
            double fraction = (double)index / (interior + 1);
            knots[degree + index] = 1 - Math.Pow(1 - fraction, power);
        }
        for (int index = knots.Length - degree - 1; index < knots.Length; index++) knots[index] = 1;
        var controlX = new double[count];
        for (int index = 0; index < count; index++)
        {
            double sum = 0;
            for (int offset = 1; offset <= degree; offset++) sum += knots[index + offset];
            controlX[index] = sum / degree;
        }
        return (knots, controlX);
    }

    private static byte[] DefaultDocument(double[] railKnots, double[] railX, double[] lead, double[] trail,
        double[] channelKnots, double[] channelX, double[] profileKnots, double[] profileX, double[] upper, double[] lower)
    {
        string flat = ChannelText(3, channelKnots, channelX, new double[channelX.Length]);
        string thick = ChannelText(3, channelKnots, channelX, Enumerable.Repeat(0.12, channelX.Length).ToArray());
        string text =
            "foildsl \"4.1\"\n" +
            "foil \"Untitled\" {\n" +
            "  units m\n" +
            "  half_span " + ExactDecimal(0.5) + " m\n" +
            "  evaluator \"cfdw-cv\" \"2\"\n" +
            "  symmetry mirror_y\n" +
            "  planform {\n" +
            "    leading cv { " + ChannelText(3, railKnots, railX, lead) + " }\n" +
            "    trailing cv { " + ChannelText(3, railKnots, railX, trail) + " }\n" +
            "  }\n" +
            "  dihedral cv { " + flat + " }\n" +
            "  twist cv { " + flat + " }\n" +
            "  thickness cv { " + thick + " }\n" +
            "  profiles {\n" +
            "    profile \"naca-0012\" {\n" +
            "      upper cv { " + ChannelText(5, profileKnots, profileX, upper) + " }\n" +
            "      lower cv { " + ChannelText(5, profileKnots, profileX, lower) + " }\n" +
            "      closure closed\n" +
            "    }\n" +
            "  }\n" +
            "  sections { at root profile \"naca-0012\" at tip profile \"naca-0012\" }\n" +
            "  tip open\n" +
            "}\n";
        return Utf8.GetBytes(text);
    }

    private static string ChannelText(int degree, double[] knots, double[] x, double[] y)
    {
        string knotText = string.Join(", ", knots.Select(value => ExactDecimal(value)));
        string points = string.Join(", ", x.Zip(y, (abscissa, ordinate) => "(" + ExactDecimal(abscissa) + ", " + ExactDecimal(ordinate) + ")"));
        string ids = string.Join(", ", Enumerable.Range(0, x.Length).Select(index => "\"cv-" + index.ToString(CultureInfo.InvariantCulture) + "\""));
        return "degree " + degree.ToString(CultureInfo.InvariantCulture) + " knots [" + knotText + "] points [" + points + "] ids [" + ids + "]";
    }

    // A finite binary64 is an integer divided by a power of two. Multiplying
    // by the inverse decimal unit gives a terminating decimal without rounding.
    internal static string ExactDecimal(double value, int unitScale = 0)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        int exponent = (int)((bits >> 52) & 2047);
        BigInteger integer = bits & 0xfffffffffffffUL;
        if (exponent != 0) integer += BigInteger.One << 52;
        int shift = exponent == 0 ? -1074 : exponent - 1075;
        int places = Math.Max(0, -shift);
        if (shift >= 0) integer <<= shift;
        else integer *= BigInteger.Pow(5, places);
        integer *= BigInteger.Pow(10, -unitScale);
        string digits = integer.ToString(CultureInfo.InvariantCulture);
        if (places != 0)
        {
            digits = digits.PadLeft(places + 1, '0').Insert(Math.Max(1, digits.Length - places), ".");
            digits = digits.TrimEnd('0').TrimEnd('.');
        }
        if (value < 0) digits = "-" + digits;
        return digits;
    }
    private static bool SameBits(double actual, double expected) => BitConverter.DoubleToUInt64Bits(actual) ==
        BitConverter.DoubleToUInt64Bits(expected == 0 ? 0 : expected);
    private static string RewriteProfile(string text, ProfileDefinition profile, string newName)
    {
        var edits = new List<(int Start, int End, string Value)>();
        string quoted = Jcs.Quote(profile.Name);
        int nameAt = text.IndexOf(quoted, profile.BlockStart, profile.BlockEnd - profile.BlockStart, StringComparison.Ordinal);
        Guard.Require(nameAt >= 0, "DSL-PROFILE-TARGET");
        edits.Add((nameAt, nameAt + quoted.Length, Jcs.Quote(newName)));
        RewriteIds(profile.Upper, edits); RewriteIds(profile.Lower, edits);
        string block = text[profile.BlockStart..profile.BlockEnd];
        foreach (var edit in edits.OrderByDescending(item => item.Start))
        {
            int start = edit.Start - profile.BlockStart, end = edit.End - profile.BlockStart;
            block = block[..start] + edit.Value + block[end..];
        }
        return block;
    }
    private static void RewriteIds(Curve curve, List<(int Start, int End, string Value)> edits)
    {
        string joined = string.Join(",", Enumerable.Range(0, curve.Points.Length).Select(index => Jcs.Quote("cv-" + index.ToString(CultureInfo.InvariantCulture))));
        if (curve.IdTokens is null) edits.Add((curve.InsertAt, curve.InsertAt, " ids [" + joined + "] "));
        else for (int index = 0; index < curve.Points.Length; index++) edits.Add((curve.IdTokens[index].Start, curve.IdTokens[index].End, Jcs.Quote("cv-" + index.ToString(CultureInfo.InvariantCulture))));
    }

    internal static (byte[] Source, string VertexId) InsertProfileKnot(byte[] source, string profile, double parameterX)
    {
        var (_, found) = ProfileOf(source, profile);
        var upper = found.Upper; var lower = found.Lower;
        Guard.Require(upper.Degree == lower.Degree && upper.Points.Length == lower.Points.Length && upper.Points.Length < 32, "DSL-CURVE");
        Guard.Require(!upper.MissingIds && !lower.MissingIds && upper.IdTokens is not null && lower.IdTokens is not null, "DSL-PATCH");
        int degree = upper.Degree;
        double t = ParameterAt(upper.Points, upper.Knots, degree, parameterX);
        Guard.Require(t > 0 && t < 1, "DSL-CURVE");
        var (knots, upperPoints, inserted) = InsertKnot(upper.Knots, upper.Points, degree, t);
        var (_, lowerPoints, _) = InsertKnot(lower.Knots, lower.Points, degree, t);
        PairAbscissa(upperPoints, lowerPoints);
        string id = NextVertexId(upper.Ids.Concat(lower.Ids));
        string[] upperIds = SpliceId(upper.Ids, inserted, id);
        string[] lowerIds = SpliceId(lower.Ids, inserted, id);
        return (RewriteCurves(source, found, knots, upperPoints, upperIds, lowerPoints, lowerIds), id);
    }

    internal static (byte[] Source, string VertexId) DeleteProfileVertex(byte[] source, string profile, int vertexIndex)
    {
        var (_, found) = ProfileOf(source, profile);
        var upper = found.Upper; var lower = found.Lower;
        int count = upper.Points.Length;
        Guard.Require(upper.Degree == lower.Degree && count == lower.Points.Length && (uint)vertexIndex < (uint)count, "DSL-PROFILE-TARGET");
        Guard.Require(!upper.MissingIds && !lower.MissingIds && upper.IdTokens is not null && lower.IdTokens is not null, "DSL-PATCH");
        int degree = upper.Degree;
        int n = count - 1;
        int r = vertexIndex + (degree + 1) / 2;
        if (r < degree + 1) r = degree + 1;
        if (r > n) r = n;
        double knot = upper.Knots[r];
        while (r + 1 < upper.Knots.Length - degree - 1 && upper.Knots[r + 1] == knot) r++;
        var upperPoints = RemoveKnot(upper.Points, upper.Knots, degree, r);
        var lowerPoints = RemoveKnot(lower.Points, lower.Knots, degree, r);
        AnchorEnds(upperPoints, upper.Points);
        AnchorEnds(lowerPoints, lower.Points);
        EnforceAbscissa(upperPoints);
        PairAbscissa(upperPoints, lowerPoints);
        var knots = upper.Knots.Where((_, index) => index != r).ToArray();
        string removed = upper.Ids[vertexIndex];
        return (RewriteCurves(source, found, knots, upperPoints, DropId(upper.Ids, vertexIndex), lowerPoints, DropId(lower.Ids, vertexIndex)), removed);
    }

    internal static (byte[] Source, FairResult Result) FairProfile(byte[] source, string profile, double tolerance, PreserveEnds ends)
    {
        var (_, found) = ProfileOf(source, profile);
        Guard.Require(!found.Upper.MissingIds && !found.Lower.MissingIds && found.Upper.IdTokens is not null && found.Lower.IdTokens is not null, "DSL-PATCH");
        var result = ProfileFair.Fair(found, tolerance, ends);
        if (!result.WithinTolerance) return (source, result);
        byte[] candidate = RewriteCurves(source, found, result.Upper.Knots, result.Upper.Points, found.Upper.Ids, result.Lower.Points, found.Lower.Ids);
        return (candidate, result);
    }

    internal static (byte[] Source, FairResult Result) RebuildProfile(byte[] source, string profile, int vertexCount, double tolerance, PreserveEnds ends)
    {
        var (_, found) = ProfileOf(source, profile);
        Guard.Require(!found.Upper.MissingIds && !found.Lower.MissingIds && found.Upper.IdTokens is not null && found.Lower.IdTokens is not null, "DSL-PATCH");
        var result = ProfileFair.Rebuild(found, vertexCount, tolerance, ends);
        if (!result.WithinTolerance) return (source, result);
        string[] upperIds = vertexCount == found.Upper.Points.Length ? found.Upper.Ids : NextVertexIds(found.Upper.Ids, vertexCount);
        string[] lowerIds = vertexCount == found.Lower.Points.Length ? found.Lower.Ids : NextVertexIds(found.Lower.Ids, vertexCount);
        byte[] candidate = RewriteCurves(source, found, result.Upper.Knots, result.Upper.Points, upperIds, result.Lower.Points, lowerIds);
        return (candidate, result);
    }

    // Fresh ids are numbered strictly above the curve's own current maximum, so a
    // vertex count change never reissues an id this curve has already used (including
    // one a prior edit removed) even though DropId/SpliceId never retire the number.
    private static string[] NextVertexIds(string[] existing, int count)
    {
        int max = -1;
        foreach (string id in existing)
            if (id.StartsWith("cv-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                max = Math.Max(max, number);
        var ids = new string[count];
        for (int index = 0; index < count; index++) ids[index] = "cv-" + (++max).ToString(CultureInfo.InvariantCulture);
        return ids;
    }

    internal static double MaxOrdinateDeviation(ProfileDefinition before, ProfileDefinition after)
    {
        const int samples = 2001;
        double worst = 0;
        worst = Math.Max(worst, SideDeviation(before.Upper, after.Upper, samples));
        worst = Math.Max(worst, SideDeviation(before.Lower, after.Lower, samples));
        return worst;
    }

    private static (Definition Definition, ProfileDefinition Profile) ProfileOf(byte[] source, string profile)
    {
        var parsed = Parse(source);
        var definition = parsed.Definition ?? throw new ContractError("DSL-PROFILE-TARGET");
        var found = definition.Profiles.FirstOrDefault(item => item.Name == profile) ?? throw new ContractError("DSL-PROFILE-TARGET");
        return (definition, found);
    }

    private static byte[] RewriteCurves(byte[] source, ProfileDefinition profile, double[] knots, double[][] upperPoints, string[] upperIds, double[][] lowerPoints, string[] lowerIds)
    {
        string text = Utf8.GetString(source);
        var edits = CurveEdits(profile.Upper, knots, upperPoints, upperIds).Concat(CurveEdits(profile.Lower, knots, lowerPoints, lowerIds));
        foreach (var edit in edits.OrderByDescending(item => item.Start))
            text = text[..edit.Start] + edit.Value + text[edit.End..];
        byte[] candidate = Utf8.GetBytes(text);
        var parsed = Parse(candidate);
        var patched = parsed.Definition?.Profiles.FirstOrDefault(item => item.Name == profile.Name);
        Guard.Require(parsed.IsParsed && patched is not null, "DSL-PATCH");
        Guard.Require(patched!.Upper.Points.Length == upperPoints.Length && patched.Lower.Points.Length == lowerPoints.Length, "DSL-PATCH");
        for (int index = 0; index < knots.Length; index++)
            Guard.Require(SameBits(patched.Upper.Knots[index], knots[index]) && SameBits(patched.Lower.Knots[index], knots[index]), "DSL-PATCH");
        for (int index = 0; index < upperPoints.Length; index++)
        {
            Guard.Require(SameBits(patched.Upper.Points[index][0], upperPoints[index][0]) && SameBits(patched.Upper.Points[index][1], upperPoints[index][1]), "DSL-PATCH");
            Guard.Require(SameBits(patched.Lower.Points[index][0], lowerPoints[index][0]) && SameBits(patched.Lower.Points[index][1], lowerPoints[index][1]), "DSL-PATCH");
            Guard.Require(patched.Upper.Ids[index] == upperIds[index] && patched.Lower.Ids[index] == lowerIds[index], "DSL-PATCH");
        }
        return candidate;
    }

    private static List<(int Start, int End, string Value)> CurveEdits(Curve curve, double[] knots, double[][] points, string[] ids)
    {
        Guard.Require(curve.IdTokens is not null, "DSL-PATCH");
        return
        [
            (curve.KnotStart, curve.KnotEnd, string.Join(", ", knots.Select(knot => ExactDecimal(knot, 0)))),
            (curve.PointsStart, curve.PointsEnd, string.Join(", ", points.Select(point => "(" + ExactDecimal(point[0], 0) + ", " + ExactDecimal(point[1], 0) + ")"))),
            (curve.IdTokens![0].Start, curve.IdTokens[^1].End, string.Join(",", ids.Select(Jcs.Quote)))
        ];
    }

    private static (double[] Knots, double[][] Points, int Inserted) InsertKnot(double[] knots, double[][] points, int degree, double t)
    {
        int count = points.Length;
        int k = degree;
        while (k + 1 < count && knots[k + 1] <= t) k++;
        int multiplicity = 0;
        for (int index = k; index >= 0 && knots[index] == t; index--) multiplicity++;
        Guard.Require(multiplicity < degree, "DSL-CURVE");
        var nextKnots = new double[knots.Length + 1];
        Array.Copy(knots, nextKnots, k + 1);
        nextKnots[k + 1] = t;
        Array.Copy(knots, k + 1, nextKnots, k + 2, knots.Length - k - 1);
        var next = new double[count + 1][];
        for (int index = 0; index <= k - degree; index++) next[index] = CopyPoint(points[index]);
        for (int index = k - multiplicity; index < count; index++) next[index + 1] = CopyPoint(points[index]);
        for (int index = k - degree + 1; index <= k - multiplicity; index++)
        {
            double width = knots[index + degree] - knots[index];
            double alpha = width == 0 ? 0 : (t - knots[index]) / width;
            double x0 = points[index - 1][0], x1 = points[index][0];
            double y0 = points[index - 1][1], y1 = points[index][1];
            // Equal ordinates stay bitwise equal: (1-α)y + αy is not y.
            next[index] = new[]
            {
                x0 == x1 ? x0 : (1 - alpha) * x0 + alpha * x1,
                y0 == y1 ? y0 : (1 - alpha) * y0 + alpha * y1
            };
        }
        int firstNew = k - degree + 1;
        int lastNew = k - multiplicity;
        int inserted = firstNew <= lastNew ? (firstNew + lastNew) / 2 : k - multiplicity + 1;
        Guard.Require((uint)inserted < (uint)next.Length && next.All(point => point is { Length: 2 }), "DSL-CURVE");
        return (nextKnots, next, inserted);
    }

    private static double[][] RemoveKnot(double[][] points, double[] knots, int degree, int r)
    {
        int p = degree;
        int n = points.Length - 1;
        double knot = knots[r];
        var pw = points.Select(CopyPoint).ToArray();
        int s = 1;
        int scan = r;
        while (scan > 0 && knots[scan - 1] == knot) { s++; scan--; }
        int ord = p + 1;
        int first = r - p;
        int last = r - s;
        int off = first - 1;
        var temp = new double[n + p + 8][];
        for (int slot = 0; slot < temp.Length; slot++) temp[slot] = new double[2];
        temp[0] = CopyPoint(pw[off]);
        temp[last + 1 - off] = CopyPoint(pw[last + 1]);
        int i = first, j = last, ii = 1, jj = last - off;
        while (j - i > 0)
        {
            double deni = knots[i + ord] - knots[i];
            double denj = knots[j + ord] - knots[j];
            double alfi = (knot - knots[i]) / deni;
            double alfj = (knot - knots[j]) / denj;
            temp[ii] = new[]
            {
                (pw[i][0] - (1 - alfi) * temp[ii - 1][0]) / alfi,
                (pw[i][1] - (1 - alfi) * temp[ii - 1][1]) / alfi
            };
            temp[jj] = new[]
            {
                (pw[j][0] - alfj * temp[jj + 1][0]) / (1 - alfj),
                (pw[j][1] - alfj * temp[jj + 1][1]) / (1 - alfj)
            };
            i++; ii++; j--; jj--;
        }
        if (j - i < 0)
        {
            double midX = 0.5 * (temp[ii - 1][0] + temp[jj + 1][0]);
            double midY = 0.5 * (temp[ii - 1][1] + temp[jj + 1][1]);
            temp[ii - 1][0] = temp[jj + 1][0] = midX;
            temp[ii - 1][1] = temp[jj + 1][1] = midY;
        }
        i = first; j = last;
        while (j - i > 0)
        {
            pw[i] = CopyPoint(temp[i - off]);
            pw[j] = CopyPoint(temp[j - off]);
            i++; j--;
        }
        var kept = new double[n][];
        int write = 0;
        int foutNumer = 2 * r - s - p;
        Guard.Require(foutNumer % 2 == 0, "DSL-CURVE");
        int fout = foutNumer / 2;
        Guard.Require((uint)fout <= (uint)n, "DSL-CURVE");
        for (int index = 0; index <= n; index++)
        {
            if (index == fout) continue;
            kept[write++] = pw[index];
        }
        return kept;
    }

    private static double SideDeviation(Curve before, Curve after, int samples)
    {
        double worst = 0;
        for (int index = 0; index < samples; index++)
        {
            double x = index / (double)(samples - 1);
            double delta = Math.Abs(OrdinateAt(before, x) - OrdinateAt(after, x));
            if (delta > worst) worst = delta;
        }
        return worst;
    }

    private static double OrdinateAt(Curve curve, double x)
    {
        double lo = 0, hi = 1;
        for (int step = 0; step < 80; step++)
        {
            double mid = (lo + hi) / 2;
            if (Evaluate(curve.Points, curve.Knots, curve.Degree, mid)[0] < x) lo = mid;
            else hi = mid;
        }
        return Evaluate(curve.Points, curve.Knots, curve.Degree, (lo + hi) / 2)[1];
    }

    private static double ParameterAt(double[][] points, double[] knots, int degree, double x)
    {
        double lo = 0, hi = 1;
        for (int step = 0; step < 80; step++)
        {
            double mid = (lo + hi) / 2;
            if (Evaluate(points, knots, degree, mid)[0] < x) lo = mid;
            else hi = mid;
        }
        return (lo + hi) / 2;
    }

    private static double[] Evaluate(double[][] points, double[] knots, int degree, double t)
    {
        if (t >= 1) return CopyPoint(points[^1]);
        int span = degree;
        while (span + 1 < points.Length && knots[span + 1] <= t) span++;
        var values = Enumerable.Range(0, degree + 1).Select(index => CopyPoint(points[span - degree + index])).ToArray();
        for (int level = 1; level <= degree; level++)
            for (int index = degree; index >= level; index--)
            {
                int knot = span - degree + index;
                double width = knots[knot + degree - level + 1] - knots[knot];
                double alpha = width > 0 ? (t - knots[knot]) / width : 0;
                values[index][0] = (1 - alpha) * values[index - 1][0] + alpha * values[index][0];
                values[index][1] = (1 - alpha) * values[index - 1][1] + alpha * values[index][1];
            }
        return values[degree];
    }

    private static void AnchorEnds(double[][] result, double[][] input)
    {
        result[0] = CopyPoint(input[0]);
        result[^1] = CopyPoint(input[^1]);
    }

    private static void EnforceAbscissa(double[][] points)
    {
        points[0][0] = 0;
        points[^1][0] = 1;
        for (int index = 1; index < points.Length - 1; index++)
            if (points[index][0] < points[index - 1][0]) points[index][0] = points[index - 1][0];
        for (int index = points.Length - 2; index >= 1; index--)
            if (points[index][0] > points[index + 1][0]) points[index][0] = points[index + 1][0];
    }

    private static void PairAbscissa(double[][] upper, double[][] lower)
    {
        for (int index = 0; index < upper.Length; index++)
            lower[index] = new[] { upper[index][0], lower[index][1] };
    }

    private static double[] CopyPoint(double[] point) => new[] { point[0], point[1] };

    private static string NextVertexId(IEnumerable<string> ids)
    {
        var used = ids.ToHashSet(StringComparer.Ordinal);
        int max = -1;
        foreach (string id in used)
            if (id.StartsWith("cv-", StringComparison.Ordinal) && int.TryParse(id.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                max = Math.Max(max, number);
        string candidate;
        do candidate = "cv-" + (++max).ToString(CultureInfo.InvariantCulture);
        while (used.Contains(candidate));
        return candidate;
    }

    private static string[] SpliceId(string[] ids, int index, string id)
    {
        var list = ids.ToList();
        list.Insert(index, id);
        return list.ToArray();
    }

    private static string[] DropId(string[] ids, int index)
    {
        var list = ids.ToList();
        list.RemoveAt(index);
        return list.ToArray();
    }

    private readonly struct Dec
    {
        public readonly int Sign;
        public readonly BigInteger Digits;
        public readonly int Exp;
        private Dec(int sign, BigInteger digits, int exp) { Sign = sign; Digits = digits; Exp = exp; }
        public static Dec Parse(string text)
        {
            var match = Regex.Match(text ?? "", @"\A([+-]?)(?:([0-9]+)(?:\.([0-9]+))?|\.([0-9]+))(?:[eE]([+-]?[0-9]+))?\z", RegexOptions.CultureInvariant);
            Guard.Require(match.Success, "DSL-LEX");
            int sign = match.Groups[1].Value == "-" ? -1 : 1;
            string fraction = match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value;
            string digits = (match.Groups[2].Value + fraction).TrimStart('0');
            if (digits.Length == 0) return new(1, BigInteger.Zero, 0);
            string exponentText = match.Groups[5].Success ? match.Groups[5].Value : "0";
            int exponent = int.Parse(exponentText, CultureInfo.InvariantCulture) - fraction.Length;
            return Normalize(sign, BigInteger.Parse(digits, CultureInfo.InvariantCulture), exponent);
        }
        public Dec Shift(int places) => Normalize(Sign, Digits, Exp + places);
        public Dec Multiply(Dec other) => Normalize(Sign * other.Sign, Digits * other.Digits, Exp + other.Exp);
        public Dec Divide(int divisor) => Divide(new Dec(1, new BigInteger(divisor), 0));
        public Dec Divide(Dec other)
        {
            Guard.Require(!other.Digits.IsZero, "DSL-PATCH");
            int sign = Sign * other.Sign;
            BigInteger numerator = Digits;
            BigInteger denominator = other.Digits;
            int exponent = Exp - other.Exp;
            if (exponent >= 0) numerator *= BigInteger.Pow(10, exponent);
            else denominator *= BigInteger.Pow(10, -exponent);
            if (numerator.IsZero) return new(1, BigInteger.Zero, 0);
            var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
            numerator /= divisor;
            denominator /= divisor;
            int twos = 0, fives = 0;
            while (denominator % 2 == 0) { denominator /= 2; twos++; }
            while (denominator % 5 == 0) { denominator /= 5; fives++; }
            if (denominator != 1)
            {
                double magnitude = DecimalSi.Round(numerator, denominator);
                return Parse(ExactDecimal(sign < 0 ? -magnitude : magnitude));
            }
            int scale = Math.Max(twos, fives);
            numerator *= BigInteger.Pow(2, scale - twos) * BigInteger.Pow(5, scale - fives);
            return Normalize(sign, numerator, -scale);
        }
        public string Format()
        {
            if (Digits.IsZero) return "0";
            string digits = Digits.ToString(CultureInfo.InvariantCulture);
            string body;
            if (Exp >= 0) body = digits + new string('0', Exp);
            else
            {
                int scale = -Exp;
                if (digits.Length <= scale) digits = digits.PadLeft(scale + 1, '0');
                int dot = digits.Length - scale;
                body = digits[..dot] + "." + digits[dot..];
            }
            return Sign < 0 ? "-" + body : body;
        }
        private static Dec Normalize(int sign, BigInteger digits, int exp)
        {
            if (digits.IsZero || sign == 0) return new(1, BigInteger.Zero, 0);
            if (digits.Sign < 0) { digits = BigInteger.Abs(digits); sign = -sign; }
            while (digits % 10 == 0) { digits /= 10; exp++; }
            return new(sign < 0 ? -1 : 1, digits, exp);
        }
    }

    private sealed class Grammar
    {
        private static readonly Regex TokenPattern = new(@"\G(?:[ \t\r\n]+|\#[^\r\n]*|""(?:[^""\\\x00-\x1f]|\\(?:[""\\/bfnrt]|u[0-9a-fA-F]{4}))*""|[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?|[A-Za-z_][A-Za-z_0-9]*|>=|<=|==|[{}\[\](),%])", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        private readonly List<SourceToken> tokens = [];
        private readonly List<RawCurve> rawCurves = [];
        private readonly List<ProfileSource> profiles = [];
        private readonly List<AssignmentSource> assignments = [];
        private readonly List<LockSource> locks = [];
        private readonly List<AssertionSource> assertions = [];
        private int position;
        private string kind = "", tip = "open";
        private SourceToken version = new("", 0, 0), evaluator = new("", 0, 0), evaluatorVersion = new("", 0, 0);
        private SourceToken units = new("m", 0, 0), halfSpan = new("1", 0, 0), halfSpanUnit = new("m", 0, 0);
        private string documentName = "";

        internal Grammar(string text)
        {
            int at = text.StartsWith('\uFEFF') ? 1 : 0;
            SourceToken? previous = null;
            while (at < text.Length)
            {
                Match match;
                try { match = TokenPattern.Match(text, at); }
                catch (RegexMatchTimeoutException) { throw Failure("DSL-LIMIT", "Resource", new("", at, at)); }
                if (!match.Success) throw Failure("DSL-LEX", "Lexical", new("", at, at + (char.IsHighSurrogate(text[at]) ? 2 : 1)));
                var token = new SourceToken(match.Value, at, at + match.Length); at = token.End;
                if (token.Text[0] is ' ' or '\t' or '\r' or '\n' or '#') continue;
                if (previous is not null && previous.End == token.Start && WordOrNumber(previous) && WordOrNumber(token))
                    throw Failure("DSL-LEX", "Lexical", token);
                if (token.Text is "NaN" or "Infinity") throw Failure("DSL-LEX", "Lexical", token);
                if (token.Text.StartsWith('"')) ValidateString(token);
                if (NumberToken(token) && token.Text.Length > 4096) throw Failure("DSL-LIMIT", "Resource", token);
                tokens.Add(token); previous = token;
            }
            tokens.Add(new("EOF", text.Length, text.Length));
        }
        private static bool NumberToken(SourceToken token) => token.Text[0] is >= '0' and <= '9' or '+' or '-' or '.';
        private static bool WordOrNumber(SourceToken token) => NumberToken(token) || char.IsAsciiLetter(token.Text[0]) || token.Text[0] == '_';
        private static SourceFailure Failure(string code, string phase, SourceToken token) => new(code, phase, token);
        private static void Need(bool condition, string code, string phase, SourceToken token)
        { if (!condition) throw Failure(code, phase, token); }
        private static void ValidateString(SourceToken token)
        {
            try { string value = token.String; _ = Jcs.Quote(value); Need(value.EnumerateRunes().Count() <= 4096, "DSL-LIMIT", "Resource", token); }
            catch (JsonException) { throw Failure("DSL-LEX", "Lexical", token); }
            catch (InvalidOperationException) { throw Failure("DSL-LEX", "Lexical", token); }
            catch (ContractError) { throw Failure("DSL-LEX", "Lexical", token); }
        }
        internal SourceToken VersionToken()
        {
            Need(tokens.Count >= 2 && tokens[0].Text == "foildsl", "DSL-VERSION", "Version", tokens.Count == 0 ? new("foildsl", 0, 0) : tokens[0]);
            return tokens[1];
        }
        private SourceToken Current => tokens[Math.Min(position, tokens.Count - 1)];
        private SourceToken Take() { var token = Current; Need(position < tokens.Count, "DSL-SYNTAX", "Syntactic", token); position++; return token; }
        private SourceToken Expect(string value)
        {
            Need(Current.Text == value, Current.Text == "chord" ? "DSL-LEGACY" : "DSL-SYNTAX", "Syntactic", Current);
            return Take();
        }
        private void End()
        {
            Need(position == tokens.Count - 1, "DSL-SYNTAX", "Syntactic", Current);
            Expect("EOF");
        }
        private bool Optional(string value) { if (Current.Text != value) return false; Take(); return true; }
        private SourceToken Name() { Need(Current.Text.StartsWith('"'), "DSL-SYNTAX", "Syntactic", Current); return Take(); }
        private SourceToken Number() { Need(NumberToken(Current), "DSL-SYNTAX", "Syntactic", Current); return Take(); }
        private SourceToken Word() { Need(char.IsAsciiLetter(Current.Text[0]), "DSL-SYNTAX", "Syntactic", Current); return Take(); }
        private string Choice(params string[] values) { Need(values.Contains(Current.Text), "DSL-SYNTAX", "Syntactic", Current); return Take().Text; }
        private SourceToken[] List(Func<SourceToken> read)
        {
            Expect("["); var values = new List<SourceToken> { read() };
            while (Optional(",")) values.Add(read());
            Expect("]"); return values.ToArray();
        }
        private RawCurve ReadCurve(string path, bool profile)
        {
            Expect("cv"); Expect("{"); Expect("degree"); var degree = Number();
            Need(degree.Text.All(char.IsAsciiDigit), "DSL-SYNTAX", "Syntactic", degree);
            Expect("knots"); var knots = List(Number); Expect("points"); Expect("[");
            var points = new List<(SourceToken, SourceToken)>();
            int pointsStart = Current.Start, pointsEnd = Current.End;
            do
            {
                var open = Expect("(");
                if (points.Count == 0) pointsStart = open.Start;
                var x = Number(); Expect(","); var y = Number();
                var close = Expect(")");
                pointsEnd = close.End;
                points.Add((x, y));
            } while (Optional(","));
            Expect("]"); SourceToken[]? ids = Optional("ids") ? List(Name) : null;
            var tangents = new List<TangentDraft>();
            if (Current.Text == "tangents")
            {
                var keyword = Current;
                if (version.String != "4.1") throw Failure("DSL-SYNTAX", "Syntactic", keyword);
                Take();
                Expect("{");
                while (Current.Text != "}")
                {
                    var id = Name();
                    var kindToken = Word();
                    SourceToken? angle = kindToken.Text == "angle" ? Number() : null;
                    tangents.Add(new(id, kindToken, angle));
                }
                Expect("}");
            }
            int insert = Current.Start; Expect("}");
            var curve = new RawCurve(path, degree, knots, points.ToArray(), ids, insert, profile, pointsStart, pointsEnd) { Tangents = tangents.ToArray() };
            rawCurves.Add(curve); return curve;
        }
        private void ReadEvaluator() { Expect("evaluator"); evaluator = Name(); evaluatorVersion = Name(); }
        private ProfileSource ReadProfile(SourceToken name, int index)
        {
            if (Optional("asset")) { Expect("sha256"); return new(name, null, null, "closed", Name()); }
            Expect("upper"); var upper = ReadCurve($"profile:{index}:upper", true);
            Expect("lower"); var lower = ReadCurve($"profile:{index}:lower", true);
            string closure = Optional("closure") ? Choice("open", "closed") : "closed";
            string? provenance = Optional("provenance") ? Name().String : null;
            return new(name, upper, lower, closure, null) { Provenance = provenance };
        }
        private StationSource ReadStation()
        {
            if (Current.Text is "root" or "center" or "tip") return new(Take(), null);
            var value = Number(); return new(value, Optional("%") ? tokens[position - 1] : Word());
        }
        private void ReadLock()
        {
            string lockKind = Choice("root_mirror", "freeze", "value", "bounds");
            var channel = Take(); Need(new[] { "leading", "trailing", "dihedral", "twist", "thickness" }.Contains(channel.Text), "DSL-SYNTAX", "Syntactic", channel);
            SourceToken? id = null; StationSource? station = null; SourceToken[] values = [];
            if (lockKind is "freeze" or "bounds") id = Name();
            if (lockKind == "freeze") { Expect("at"); Expect("("); var x = Number(); Expect(","); var y = Number(); Expect(")"); values = [x, y]; }
            if (lockKind == "value") { Expect("at"); station = ReadStation(); values = [Number()]; }
            if (lockKind == "bounds") values = [Number(), Number()];
            locks.Add(new(lockKind, channel, id, station, values));
        }
        private QuantitySource ReadQuantity()
        {
            var value = Number();
            return new(value, Current.Text is "m" or "cm" or "mm" or "m2" or "cm2" or "mm2" ? Take() : null);
        }
        private void ReadAssertion()
        {
            var metric = Take();
            Need(new[] { "area", "aspect", "taper", "mean_chord", "mac", "center_chord", "tip_chord", "tip_rise", "max_drop", "max_rise", "root_thick", "tip_thick" }.Contains(metric.Text), "DSL-SYNTAX", "Syntactic", metric);
            string comparison = Choice(">=", "<=", "=="); var value = ReadQuantity(); QuantitySource? tolerance = null;
            if (comparison == "==") { Expect("tolerance"); tolerance = ReadQuantity(); }
            assertions.Add(new(metric, comparison, value, tolerance));
        }
        internal void ReadDocument()
        {
            if (Current.Text != "foildsl") throw Failure("DSL-VERSION", "Version", Current);
            Expect("foildsl"); version = Name();
            if (version.String is not ("4.0" or "4.1")) throw Failure("DSL-VERSION", "Version", version);
            kind = Choice("foil", "section"); var name = Name(); Expect("{");
            documentName = name.String;
            if (kind == "section")
            {
                ReadEvaluator(); Need(Current.Text == "upper", "DSL-SYNTAX", "Syntactic", Current);
                profiles.Add(ReadProfile(name, 0)); Expect("}"); End(); return;
            }
            Expect("units"); units = Word(); Expect("half_span"); halfSpan = Number(); halfSpanUnit = Word();
            ReadEvaluator(); Expect("symmetry"); Expect("mirror_y"); Expect("planform"); Expect("{");
            Expect("leading"); ReadCurve("leading", false); Expect("trailing"); ReadCurve("trailing", false); Expect("}");
            Expect("dihedral"); ReadCurve("dihedral", false); Expect("twist"); ReadCurve("twist", false); Expect("thickness"); ReadCurve("thickness", false);
            Expect("profiles"); Expect("{");
            do
            {
                var keyword = Expect("profile"); var profileName = Name(); Expect("{");
                var profile = ReadProfile(profileName, profiles.Count); var close = Expect("}");
                profiles.Add(profile with { BlockStart = keyword.Start, BlockEnd = close.End });
            } while (Current.Text == "profile");
            Expect("}"); Expect("sections"); Expect("{");
            do { Expect("at"); var station = ReadStation(); Expect("profile"); assignments.Add(new(station, Name())); } while (Current.Text == "at");
            Need(assignments.Count >= 2, "DSL-SYNTAX", "Syntactic", Current);
            Expect("}"); if (Optional("tip")) tip = Choice("open", "point");
            if (Optional("locks")) { Expect("{"); while (Current.Text != "}") ReadLock(); Expect("}"); }
            else foreach (string channel in new[] { "leading", "trailing", "dihedral", "twist", "thickness" }) locks.Add(new("root_mirror", new(channel, 0, 0), null, null, []));
            if (Optional("constrain")) { Expect("{"); while (Current.Text != "}") ReadAssertion(); Expect("}"); }
            Expect("}"); End();
        }
        internal List<(int Start, int End, string Value)> SpanEdits(string spanMillimetres)
        {
            int halfScale = Scale(halfSpanUnit);
            var oldHalf = Dec.Parse(halfSpan.Text);
            var newHalf = Dec.Parse(spanMillimetres).Shift(-3 - halfScale).Divide(2);
            var edits = new List<(int Start, int End, string Value)>();
            string halfText = newHalf.Format();
            if (!string.Equals(halfText, halfSpan.Text, StringComparison.Ordinal))
                edits.Add((halfSpan.Start, halfSpan.End, halfText));
            void Station(StationSource? station)
            {
                if (station?.Unit is null || station.Unit.Text == "%" || station.Value.Text is "root" or "center" or "tip") return;
                string next = Dec.Parse(station.Value.Text).Multiply(newHalf).Divide(oldHalf).Format();
                if (!string.Equals(next, station.Value.Text, StringComparison.Ordinal))
                    edits.Add((station.Value.Start, station.Value.End, next));
            }
            foreach (var assignment in assignments) Station(assignment.Station);
            foreach (var constraint in locks)
                if (constraint.Kind == "value") Station(constraint.Station);
            return edits;
        }
        private static int Scale(SourceToken unit, bool area = false) => unit.Text switch
        {
            "m" when !area => 0, "cm" when !area => -2, "mm" when !area => -3,
            "m2" when area => 0, "cm2" when area => -4, "mm2" when area => -6,
            _ => throw Failure("DSL-UNIT", "Structural", unit)
        };
        private static double ConvertNumber(SourceToken token, int scale = 0)
        {
            try { return DecimalSi.Parse(token.Text, scale); }
            catch (ContractError failure) { throw Failure(failure.Code, failure.Code == "DSL-LIMIT" ? "Resource" : "Lexical", token); }
        }
        private Curve ConvertCurve(RawCurve raw, int scale)
        {
            try
            {
                var curve = ConvertCurveValues(raw, scale);
                BindTangents(curve, raw);
                if (raw.Tangents.Length == 0) return curve;
                var rows = raw.Tangents.Select(row => new TangentRow(row.Id.String, row.Kind.Text, row.Angle is null ? null : ConvertNumber(row.Angle))).ToArray();
                return curve with { Tangents = rows };
            }
            catch (SourceFailure failure) when (failure.Code == "DSL-CURVE")
            {
                int degree = raw.Profile ? 5 : 3;
                string range = raw.Profile ? "6–32" : version.String == "4.1" ? "4–16" : "6–10";
                throw new SourceFailure(failure.Code, failure.Phase, failure.Token, raw.Path,
                    $"Curve {raw.Path} requires degree {degree}, {range} points and {raw.Points.Length + degree + 1} knots for its {raw.Points.Length} points; knots must be ordered and clamped, with ordered abscissae from 0 to 1.");
            }
        }
        private void BindTangents(Curve curve, RawCurve raw)
        {
            if (raw.Tangents.Length == 0) return;
            if (raw.Ids is null) throw new SourceFailure("DSL-SYNTAX", "Syntactic", raw.Tangents[0].Id, raw.Path, "A tangents block requires ids.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in raw.Tangents)
            {
                string kindName = row.Kind.Text;
                if (kindName is not ("smooth" or "symmetric" or "horizontal" or "vertical" or "angle"))
                    throw new SourceFailure("DSL-SYNTAX", "Syntactic", row.Kind);
                if (kindName == "angle" && row.Angle is null) throw new SourceFailure("DSL-SYNTAX", "Syntactic", row.Kind);
                if (!seen.Add(row.Id.String)) throw new SourceFailure("DSL-SYNTAX", "Syntactic", row.Id);
                if (!raw.Profile && kindName is not ("smooth" or "symmetric"))
                    throw new SourceFailure("DSL-LOCK", "Structural", row.Kind, raw.Path, "A channel tangent row is smooth or symmetric.");
                int index = Array.IndexOf(curve.Ids, row.Id.String);
                if (index < 0) throw new SourceFailure("DSL-REFERENCE", "References", row.Id, row.Id.String, "Tangent row does not name a point.");
                if (!IsAnchor(curve.Knots, curve.Points.Length, curve.Degree, index))
                    throw new SourceFailure("DSL-LOCK", "Structural", row.Id, raw.Path, "A tangent row names an interior anchor.");
            }
        }
        private Curve ConvertCurveValues(RawCurve raw, int scale)
        {
            Need(int.TryParse(raw.Degree.Text, CultureInfo.InvariantCulture, out int degree) && degree == (raw.Profile ? 5 : 3), "DSL-CURVE", "Structural", raw.Degree);
            int max = raw.Profile ? 32 : version.String == "4.1" ? 16 : 10;
            int min = raw.Profile || version.String != "4.1" ? 6 : 4;
        Need(raw.Points.Length >= min && raw.Points.Length <= max, "DSL-CURVE", "Structural", raw.Degree);
            var knots = raw.Knots.Select(token => ConvertNumber(token)).ToArray();
            var points = raw.Points.Select(point => new[] { ConvertNumber(point.X), ConvertNumber(point.Y, scale) }).ToArray();
            Need(knots.Length == points.Length + degree + 1, "DSL-CURVE", "Structural", raw.Degree);
            Need(knots.Take(degree + 1).All(x => x == 0) && knots.TakeLast(degree + 1).All(x => x == 1), "DSL-CURVE", "Structural", raw.Degree);
            Need(knots.Zip(knots.Skip(1)).All(pair => pair.First <= pair.Second), "DSL-CURVE", "Structural", raw.Degree);
            var interior = knots.Skip(degree + 1).Take(knots.Length - 2 * (degree + 1)).ToArray();
            Need(interior.All(x => x > 0 && x < 1) && interior.GroupBy(x => x).All(group => group.Count() <= degree), "DSL-CURVE", "Structural", raw.Degree);
            Need(points[0][0] == 0 && points[^1][0] == 1 && points.Zip(points.Skip(1)).All(pair => raw.Profile ? pair.First[0] <= pair.Second[0] : pair.First[0] < pair.Second[0]), "DSL-CURVE", "Structural", raw.Degree);
            string[] ids = raw.Ids?.Select(token => token.String).ToArray() ?? Enumerable.Range(0, points.Length).Select(index => $"cv-{index}").ToArray();
            return new(raw.Path, degree, knots, points, ids, raw.Points.Select(point => point.Y).ToArray(), raw.InsertAt, raw.Ids is null,
                raw.Points.Select(point => point.X).ToArray(), raw.Ids, raw.Knots[0].Start, raw.Knots[^1].End, raw.PointsStart, raw.PointsEnd);
        }
        private static double Eta(StationSource station, double halfSpan)
        {
            if (station.Value.Text is "root" or "center") return 0;
            if (station.Value.Text == "tip") return 1;
            var unit = station.Unit ?? throw Failure("DSL-UNIT", "Structural", station.Value);
            return unit.Text == "%" ? ConvertNumber(station.Value, -2) : ConvertNumber(station.Value, Scale(unit)) / halfSpan;
        }
        private void CheckDimensions(double halfSpan, int scale)
        {
            foreach (var assignment in assignments) _ = Eta(assignment.Station, halfSpan);
            foreach (var constraint in locks)
            {
                if (constraint.Channel.Text is not ("leading" or "trailing" or "dihedral" or "twist" or "thickness")) continue;
                if (constraint.Station is not null) _ = Eta(constraint.Station, halfSpan);
                int ordinateScale = constraint.Channel.Text is "leading" or "trailing" or "dihedral" ? scale : 0;
                for (int index = 0; index < constraint.Values.Length; index++)
                    _ = ConvertNumber(constraint.Values[index], constraint.Kind == "freeze" && index == 0 ? 0 : ordinateScale);
            }
            foreach (var assertion in assertions)
            {
                _ = Quantity(assertion.Value, assertion.Metric);
                if (assertion.Tolerance is not null) Need(Quantity(assertion.Tolerance, assertion.Metric) >= 0, "DSL-UNIT", "Structural", assertion.Tolerance.Number);
            }
        }
        private static double Quantity(QuantitySource quantity, SourceToken metric)
        {
            bool dimensionless = metric.Text is "aspect" or "taper";
            Need(dimensionless ? quantity.Unit is null : quantity.Unit is not null, "DSL-UNIT", "Structural", quantity.Number);
            return ConvertNumber(quantity.Number, quantity.Unit is null ? 0 : Scale(quantity.Unit, metric.Text == "area"));
        }
        internal Definition Validate()
        {
            Need(version.String is "4.0" or "4.1", "DSL-VERSION", "Version", version);
            Need(evaluator.String == "cfdw-cv" && evaluatorVersion.String == "2", "DSL-VERSION", "Version", evaluator);
            CheckNumericRange();
            Need(profiles.Count <= 4096 && assignments.Count <= 4096 && locks.Count + assertions.Count <= 4096, "DSL-LIMIT", "Resource", version);
            int scale = kind == "foil" ? Scale(units) : 0;
            double h = kind == "foil" ? ConvertNumber(halfSpan, Scale(halfSpanUnit)) : 1;
            Need(h > 0, "DSL-UNIT", "Structural", halfSpan);
            var curves = rawCurves.ToDictionary(raw => raw.Path, raw => ConvertCurve(raw, raw.Path is "leading" or "trailing" or "dihedral" ? scale : 0), StringComparer.Ordinal);
            CheckDimensions(h, scale);
            foreach (var raw in rawCurves)
            {
                var curve = curves[raw.Path];
                Need(curve.Ids.Length == curve.Points.Length && curve.Ids.All(id => id.Length > 0) && curve.Ids.Distinct(StringComparer.Ordinal).Count() == curve.Ids.Length, "DSL-REFERENCE", "References", raw.Degree);
            }
            Need(profiles.All(profile => profile.Name.String.Length > 0) && profiles.Select(profile => profile.Name.String).Distinct(StringComparer.Ordinal).Count() == profiles.Count, "DSL-REFERENCE", "References", profiles[0].Name);
            foreach (var profile in profiles) if (profile.Asset is not null) throw Failure("DSL-REFERENCE", "References", profile.Asset);
            var definitions = profiles.Select(profile => new ProfileDefinition(profile.Name.String, curves[profile.Upper!.Path], curves[profile.Lower!.Path], profile.Closure, profile.BlockStart, profile.BlockEnd) { Provenance = profile.Provenance }).ToArray();
            var resolved = new List<(double Eta, int Profile)>();
            foreach (var assignment in assignments)
            {
                int index = Array.FindIndex(definitions, profile => profile.Name == assignment.Profile.String);
                if (index < 0) throw new SourceFailure("DSL-REFERENCE", "References", assignment.Profile,
                    assignment.Profile.String, "Profile reference '" + assignment.Profile.String + "' does not name a defined profile.");
                resolved.Add((Eta(assignment.Station, h), index));
            }
            if (kind == "foil") Need(resolved.Count >= 2 && resolved[0].Eta == 0 && resolved[^1].Eta == 1 && resolved.Zip(resolved.Skip(1)).All(pair => pair.First.Eta < pair.Second.Eta) && resolved.Select(item => item.Profile).Distinct().Count() == definitions.Length, "DSL-REFERENCE", "References", version);
            foreach (var constraint in locks)
            {
                if (constraint.Id is not { } id) continue;
                var curve = curves[constraint.Channel.Text];
                Need(rawCurves.All(raw => raw.Ids is not null) && curve.Ids.Contains(id.String, StringComparer.Ordinal), "DSL-REFERENCE", "References", id);
            }
            object semantic;
            if (kind == "section") semantic = new Dictionary<string, object?> { ["format"] = "foildsl-geometry-4.0", ["kind"] = "section", ["profile"] = definitions[0].Semantic };
            else
            {
                var ordering = resolved.Select(item => item.Profile).Distinct().ToArray();
                semantic = new Dictionary<string, object?>
                {
                    ["format"] = "foildsl-geometry-4.0", ["kind"] = "foil", ["evaluator"] = new[] { "cfdw-cv", "2" },
                    ["frame"] = "aft-starboard-up-root-le", ["symmetry"] = "mirror_y", ["half_span_m"] = h,
                    ["channels"] = curves.Where(pair => !pair.Key.StartsWith("profile:", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => (object?)pair.Value.Semantic()),
                    ["profiles"] = ordering.Select(index => definitions[index].Semantic).ToArray(),
                    ["assignments"] = resolved.Select(item => new object[] { item.Eta, Array.IndexOf(ordering, item.Profile) }).ToArray(), ["tip"] = tip
                };
            }
            return new(kind, scale, h, curves, definitions, resolved.ToArray(), tip, locks.ToArray(), assertions.ToArray(), semantic, documentName,
                assignments.Select(item => item.Profile).ToArray())
            { Version = version.String };
        }
        private static int? BoundLengthScale(SourceToken? unit) => unit?.Text switch
        { "m" => 0, "cm" => -2, "mm" => -3, _ => null };
        private void CheckNumericRange()
        {
            // Only established grammar roles enter this map. Unknown interpretation
            // remains unavailable; it is never replaced by a dimensionless guess.
            var scaled = new Dictionary<SourceToken, int>();
            void Bind(SourceToken token, int? scale) { if (scale.HasValue) scaled[token] = scale.Value; }
            void Station(StationSource station)
            { if (station.Unit is not null) Bind(station.Value, station.Unit.Text == "%" ? -2 : BoundLengthScale(station.Unit)); }
            void Assertion(QuantitySource quantity, string metric)
            {
                int? scale = metric is "aspect" or "taper" ? quantity.Unit is null ? 0 : null : metric == "area" ?
                    quantity.Unit?.Text switch { "m2" => 0, "cm2" => -4, "mm2" => -6, _ => null } : BoundLengthScale(quantity.Unit);
                Bind(quantity.Number, scale);
            }
            if (kind == "foil") Bind(halfSpan, BoundLengthScale(halfSpanUnit));
            foreach (var raw in rawCurves)
            {
                int? scale = raw.Path is "leading" or "trailing" or "dihedral" ? BoundLengthScale(units) : 0;
                foreach (var knot in raw.Knots) Bind(knot, 0);
                foreach (var point in raw.Points) { Bind(point.X, 0); Bind(point.Y, scale); }
                foreach (var row in raw.Tangents) if (row.Angle is not null) Bind(row.Angle, 0);
            }
            foreach (var assignment in assignments) Station(assignment.Station);
            foreach (var constraint in locks)
            {
                if (constraint.Station is not null) Station(constraint.Station);
                int? scale = constraint.Channel.Text switch
                { "leading" or "trailing" or "dihedral" => BoundLengthScale(units), "twist" or "thickness" => 0, _ => null };
                for (int index = 0; index < constraint.Values.Length; index++)
                    Bind(constraint.Values[index], constraint.Kind == "freeze" && index == 0 && scale.HasValue ? 0 : scale);
            }
            foreach (var assertion in assertions)
            {
                Assertion(assertion.Value, assertion.Metric.Text);
                if (assertion.Tolerance is not null) Assertion(assertion.Tolerance, assertion.Metric.Text);
            }
            foreach (var pair in scaled.OrderBy(pair => pair.Key.Start)) _ = ConvertNumber(pair.Key, pair.Value);
        }
    }
}
