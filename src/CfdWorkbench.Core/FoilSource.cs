using System.Globalization;
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
    internal Definition? Definition { get; }
    internal SourceParse(byte[] source, IEnumerable<Diagnostic> diagnostics, Definition? definition = null)
    {
        this.source = source.ToArray();
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        Definition = definition;
    }
    public byte[] Source => source.ToArray();
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool IsParsed => Definition is not null && Diagnostics.Count == 0;
    public string SourceHash => Identity.Sha256(source);
    public string? SurfaceHash => Definition is null ? null : Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(Definition.Semantic)));
}

internal sealed record SourceToken(string Text, int Start, int End)
{
    internal string String => JsonSerializer.Deserialize<string>(Text) ?? "";
}
internal sealed record RawCurve(string Path, SourceToken Degree, SourceToken[] Knots,
    (SourceToken X, SourceToken Y)[] Points, SourceToken[]? Ids, int InsertAt, bool Profile);
internal sealed record ProfileSource(SourceToken Name, RawCurve? Upper, RawCurve? Lower, string Closure, SourceToken? Asset);
internal sealed record StationSource(SourceToken Value, SourceToken? Unit);
internal sealed record AssignmentSource(StationSource Station, SourceToken Profile);
internal sealed record LockSource(string Kind, SourceToken Channel, SourceToken? Id, StationSource? Station, SourceToken[] Values);
internal sealed record QuantitySource(SourceToken Number, SourceToken? Unit);
internal sealed record AssertionSource(SourceToken Metric, string Comparison, QuantitySource Value, QuantitySource? Tolerance);
internal sealed record Curve(string Path, int Degree, double[] Knots, double[][] Points, string[] Ids,
    SourceToken[] Ordinates, int InsertAt, bool MissingIds)
{
    internal object Semantic(double factor = 1) => new Dictionary<string, object?>
    { ["degree"] = Degree, ["knots"] = Knots, ["points"] = Points.Select(point => new[] { point[0], point[1] * factor }).ToArray() };
}
internal sealed record ProfileDefinition(string Name, Curve Upper, Curve Lower, string Closure)
{
    internal object Semantic => new Dictionary<string, object?>
    { ["evaluator"] = new[] { "cfdw-cv", "1" }, ["upper"] = Upper.Semantic(), ["lower"] = Lower.Semantic(), ["closure"] = Closure };
}
internal sealed record Definition(string Kind, int UnitScale, double HalfSpan, Dictionary<string, Curve> Curves,
    ProfileDefinition[] Profiles, (double Eta, int Profile)[] Assignments, string Tip, LockSource[] Locks,
    AssertionSource[] Assertions, object Semantic);
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
            do { Expect("("); var x = Number(); Expect(","); var y = Number(); Expect(")"); points.Add((x, y)); } while (Optional(","));
            Expect("]"); SourceToken[]? ids = Optional("ids") ? List(Name) : null;
            int insert = Current.Start; Expect("}");
            var curve = new RawCurve(path, degree, knots, points.ToArray(), ids, insert, profile);
            rawCurves.Add(curve); return curve;
        }
        private void ReadEvaluator() { Expect("evaluator"); evaluator = Name(); evaluatorVersion = Name(); }
        private ProfileSource ReadProfile(SourceToken name, int index)
        {
            if (Optional("asset")) { Expect("sha256"); return new(name, null, null, "closed", Name()); }
            Expect("upper"); var upper = ReadCurve($"profile:{index}:upper", true);
            Expect("lower"); var lower = ReadCurve($"profile:{index}:lower", true);
            string closure = Optional("closure") ? Choice("open", "closed") : "closed";
            if (Optional("provenance")) Name();
            return new(name, upper, lower, closure, null);
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
            Expect("foildsl"); version = Name(); kind = Choice("foil", "section"); var name = Name(); Expect("{");
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
            do { Expect("profile"); var profileName = Name(); Expect("{"); profiles.Add(ReadProfile(profileName, profiles.Count)); Expect("}"); } while (Current.Text == "profile");
            Expect("}"); Expect("sections"); Expect("{");
            do { Expect("at"); var station = ReadStation(); Expect("profile"); assignments.Add(new(station, Name())); } while (Current.Text == "at");
            Need(assignments.Count >= 2, "DSL-SYNTAX", "Syntactic", Current);
            Expect("}"); if (Optional("tip")) tip = Choice("open", "point");
            if (Optional("locks")) { Expect("{"); while (Current.Text != "}") ReadLock(); Expect("}"); }
            else foreach (string channel in new[] { "leading", "trailing", "dihedral", "twist", "thickness" }) locks.Add(new("root_mirror", new(channel, 0, 0), null, null, []));
            if (Optional("constrain")) { Expect("{"); while (Current.Text != "}") ReadAssertion(); Expect("}"); }
            Expect("}"); End();
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
            try { return ConvertCurveValues(raw, scale); }
            catch (SourceFailure failure) when (failure.Code == "DSL-CURVE")
            {
                int degree = raw.Profile ? 5 : 3;
                throw new SourceFailure(failure.Code, failure.Phase, failure.Token, raw.Path,
                    $"Curve {raw.Path} requires degree {degree}, {(raw.Profile ? "6–32" : "6–10")} points and {raw.Points.Length + degree + 1} knots for its {raw.Points.Length} points; knots must be ordered and clamped, with ordered abscissae from 0 to 1.");
            }
        }
        private Curve ConvertCurveValues(RawCurve raw, int scale)
        {
            Need(int.TryParse(raw.Degree.Text, CultureInfo.InvariantCulture, out int degree) && degree == (raw.Profile ? 5 : 3), "DSL-CURVE", "Structural", raw.Degree);
            Need(raw.Points.Length >= 6 && raw.Points.Length <= (raw.Profile ? 32 : 10), "DSL-CURVE", "Structural", raw.Degree);
            var knots = raw.Knots.Select(token => ConvertNumber(token)).ToArray();
            var points = raw.Points.Select(point => new[] { ConvertNumber(point.X), ConvertNumber(point.Y, scale) }).ToArray();
            Need(knots.Length == points.Length + degree + 1, "DSL-CURVE", "Structural", raw.Degree);
            Need(knots.Take(degree + 1).All(x => x == 0) && knots.TakeLast(degree + 1).All(x => x == 1), "DSL-CURVE", "Structural", raw.Degree);
            Need(knots.Zip(knots.Skip(1)).All(pair => pair.First <= pair.Second), "DSL-CURVE", "Structural", raw.Degree);
            var interior = knots.Skip(degree + 1).Take(knots.Length - 2 * (degree + 1)).ToArray();
            Need(interior.All(x => x > 0 && x < 1) && interior.GroupBy(x => x).All(group => group.Count() <= degree), "DSL-CURVE", "Structural", raw.Degree);
            Need(points[0][0] == 0 && points[^1][0] == 1 && points.Zip(points.Skip(1)).All(pair => raw.Profile ? pair.First[0] <= pair.Second[0] : pair.First[0] < pair.Second[0]), "DSL-CURVE", "Structural", raw.Degree);
            string[] ids = raw.Ids?.Select(token => token.String).ToArray() ?? Enumerable.Range(0, points.Length).Select(index => $"cv-{index}").ToArray();
            return new(raw.Path, degree, knots, points, ids, raw.Points.Select(point => point.Y).ToArray(), raw.InsertAt, raw.Ids is null);
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
            CheckNumericRange();
            Need(version.String == "4.0", "DSL-VERSION", "Version", version);
            Need(evaluator.String == "cfdw-cv" && evaluatorVersion.String == "1", "DSL-VERSION", "Version", evaluator);
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
            var definitions = profiles.Select(profile => new ProfileDefinition(profile.Name.String, curves[profile.Upper!.Path], curves[profile.Lower!.Path], profile.Closure)).ToArray();
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
                    ["format"] = "foildsl-geometry-4.0", ["kind"] = "foil", ["evaluator"] = new[] { "cfdw-cv", "1" },
                    ["frame"] = "aft-starboard-up-root-le", ["symmetry"] = "mirror_y", ["half_span_m"] = h,
                    ["channels"] = curves.Where(pair => !pair.Key.StartsWith("profile:", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => (object?)pair.Value.Semantic(pair.Key == "twist" ? 0.017453292519943295 : 1)),
                    ["profiles"] = ordering.Select(index => definitions[index].Semantic).ToArray(),
                    ["assignments"] = resolved.Select(item => new object[] { item.Eta, Array.IndexOf(ordering, item.Profile) }).ToArray(), ["tip"] = tip
                };
            }
            return new(kind, scale, h, curves, definitions, resolved.ToArray(), tip, locks.ToArray(), assertions.ToArray(), semantic);
        }
        private static int LexicalScale(SourceToken? unit) => unit?.Text switch
        { "cm" or "%" => -2, "mm" => -3, "cm2" => -4, "mm2" => -6, _ => 0 };
        private void CheckNumericRange()
        {
            var scaled = tokens.Where(NumberToken).ToDictionary(token => token, _ => 0);
            if (kind == "foil") scaled[halfSpan] = LexicalScale(halfSpanUnit);
            foreach (var raw in rawCurves)
            {
                scaled.Remove(raw.Degree); // Integer cardinality belongs to structural validation.
                int scale = raw.Path is "leading" or "trailing" or "dihedral" ? LexicalScale(units) : 0;
                foreach (var point in raw.Points) scaled[point.Y] = scale;
            }
            foreach (var assignment in assignments)
                if (assignment.Station.Unit is not null) scaled[assignment.Station.Value] = LexicalScale(assignment.Station.Unit);
            foreach (var constraint in locks)
            {
                if (constraint.Station?.Unit is not null) scaled[constraint.Station.Value] = LexicalScale(constraint.Station.Unit);
                int scale = constraint.Channel.Text is "leading" or "trailing" or "dihedral" ? LexicalScale(units) : 0;
                for (int index = 0; index < constraint.Values.Length; index++)
                    scaled[constraint.Values[index]] = constraint.Kind == "freeze" && index == 0 ? 0 : scale;
            }
            foreach (var assertion in assertions)
            {
                scaled[assertion.Value.Number] = LexicalScale(assertion.Value.Unit);
                if (assertion.Tolerance is not null) scaled[assertion.Tolerance.Number] = LexicalScale(assertion.Tolerance.Unit);
            }
            foreach (var pair in scaled.OrderBy(pair => pair.Key.Start)) _ = ConvertNumber(pair.Key, pair.Value);
        }
    }
}
