// Executable CONTRACT FIXTURE. Not a production parser, store or geometry certificate authority.
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ApplicationContracts;

sealed class ContractError(string code) : Exception(code)
{
    public string Code { get; } = code;
}

static class Guard
{
    public static void Require(bool condition, string code) { if (!condition) throw new ContractError(code); }
    public static readonly UTF8Encoding Utf8 = new(false, true);
    public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static string Hash(object value) => Blake3.Hasher.Hash(Guard.Utf8.GetBytes(Jcs.Write(value))).ToString();
    public static string Text(byte[] bytes)
    {
        Require(bytes.Length <= 1_048_576, "DSL-LIMIT");
        try { return Utf8.GetString(bytes); } catch (DecoderFallbackException) { throw new ContractError("DSL-LEX"); }
    }
    public static string Id(int n) => $"00000000-0000-0000-0000-{n:000000000000}";
}

static class DecimalSi
{
    // Bound lexical work BEFORE parsing BigInteger or constructing powers of ten.
    public static double Parse(string token, int decimalScale = 0)
    {
        Guard.Require(token.Length <= 4096, "DSL-LIMIT");
        var m = Regex.Match(token, @"^([+-]?)(?:([0-9]+)(?:\.([0-9]+))?|\.([0-9]+))(?:[eE]([+-]?[0-9]+))?$", RegexOptions.CultureInvariant);
        Guard.Require(m.Success, "DSL-LEX");
        var exponentText = m.Groups[5].Success ? m.Groups[5].Value : "0";
        var fraction = m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
        var digits = (m.Groups[2].Value + fraction).TrimStart('0');
        // All-zero significands need no exponent evaluation, regardless of spelling.
        if (digits.Length == 0) return 0;
        var expDigits = exponentText.TrimStart('+', '-').TrimStart('0');
        // A <=4096-byte significand cannot compensate an exponent of magnitude >=10000.
        Guard.Require(expDigits.Length <= 4, "DSL-LIMIT");
        int exponent = expDigits.Length == 0 ? 0 : int.Parse(expDigits, CultureInfo.InvariantCulture) * (exponentText[0] == '-' ? -1 : 1);
        int power = exponent - fraction.Length;
        while (digits.EndsWith('0')) { digits = digits[..^1]; power++; }
        int magnitude = digits.Length - 1 + power;
        Guard.Require(magnitude is >= -400 and <= 400, "DSL-LIMIT");
        power += decimalScale;
        BigInteger numerator = BigInteger.Parse(digits, CultureInfo.InvariantCulture), denominator = BigInteger.One;
        if (power >= 0) numerator *= BigInteger.Pow(10, power); else denominator = BigInteger.Pow(10, -power);
        double result = Round(numerator, denominator);
        Guard.Require(double.IsFinite(result), "DSL-LEX");
        return result == 0 ? 0 : m.Groups[1].Value == "-" ? -result : result;
    }

    static BigInteger Quotient(BigInteger n, BigInteger d)
    {
        var q = BigInteger.DivRem(n, d, out var r);
        int cmp = (r * 2).CompareTo(d);
        return cmp > 0 || cmp == 0 && !q.IsEven ? q + 1 : q;
    }

    static double Round(BigInteger n, BigInteger d)
    {
        int e = (int)n.GetBitLength() - (int)d.GetBitLength();
        if (e >= 0 ? n < (d << e) : (n << -e) < d) e--;
        if (e > 1023) return double.PositiveInfinity;
        int shift = e < -1022 ? 1074 : 52 - e;
        BigInteger q = shift >= 0 ? Quotient(n << shift, d) : Quotient(n, d << -shift);
        if (e < -1022) return BitConverter.Int64BitsToDouble((long)q);
        if (q == (BigInteger.One << 53)) { q >>= 1; e++; }
        if (e > 1023) return double.PositiveInfinity;
        ulong bits = ((ulong)(e + 1023) << 52) | ((ulong)q - (1UL << 52));
        return BitConverter.UInt64BitsToDouble(bits);
    }
}

static class Jcs
{
    public static string Number(double value)
    {
        Guard.Require(double.IsFinite(value), "DSL-LEX");
        if (value == 0) return "0";
        bool negative = value < 0;
        string r = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
        var parts = r.Split('e');
        int exponent = parts.Length == 2 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
        int dot = parts[0].IndexOf('.');
        int decimalPosition = (dot < 0 ? parts[0].Length : dot) + exponent;
        string digits = parts[0].Replace(".", "");
        while (digits.Length > 1 && digits[0] == '0') { digits = digits[1..]; decimalPosition--; }
        digits = digits.TrimEnd('0');
        string body;
        if (decimalPosition > 0 && decimalPosition <= 21)
            body = decimalPosition >= digits.Length ? digits + new string('0', decimalPosition - digits.Length) : digits.Insert(decimalPosition, ".");
        else if (decimalPosition <= 0 && decimalPosition > -6)
            body = "0." + new string('0', -decimalPosition) + digits;
        else
        {
            int e = decimalPosition - 1;
            body = digits[0] + (digits.Length > 1 ? "." + digits[1..] : "") + "e" + (e >= 0 ? "+" : "") + e.ToString(CultureInfo.InvariantCulture);
        }
        return (negative ? "-" : "") + body;
    }

    public static string Quote(string value)
    {
        var b = new StringBuilder("\"");
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsSurrogate(c))
            {
                Guard.Require(char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]), "DSL-LEX");
                b.Append(c).Append(value[++i]); continue;
            }
            b.Append(c switch { '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\t' => "\\t", '\n' => "\\n", '\f' => "\\f", '\r' => "\\r", < ' ' => "\\u" + ((int)c).ToString("x4"), _ => c.ToString() });
        }
        return b.Append('"').ToString();
    }

    public static string Write(object? value) => value switch
    {
        null => "null", string s => Quote(s), bool b => b ? "true" : "false",
        double d => Number(d), int i => i.ToString(CultureInfo.InvariantCulture), long l => l.ToString(CultureInfo.InvariantCulture),
        IDictionary<string, object?> map => "{" + string.Join(",", map.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Quote(x.Key) + ":" + Write(x.Value))) + "}",
        System.Collections.IEnumerable list => "[" + string.Join(",", list.Cast<object?>().Select(Write)) + "]",
        _ => throw new ArgumentException("Unsupported canonical value type")
    };
}

sealed record Token(string Value, int Start, int End);
sealed record Curve(string Path, int Degree, double[] Knots, double[][] Points, string[] Ids, Token[] Ordinates, int InsertAt, bool MissingIds)
{
    public object Semantic() => new Dictionary<string, object?> { ["degree"] = Degree, ["knots"] = Knots, ["points"] = Points.Select(p => new[] { p[0], p[1] }).ToArray() };
}
sealed record Parsed(byte[] Source, int UnitScale, Dictionary<string, Curve> Curves, object Semantic, bool Supported)
{
    public string SourceHash => Guard.Sha(Source);
    public string SurfaceHash => Guard.Hash(Semantic);
}

sealed class FoilParser
{
    readonly string text;
    readonly List<Token> tokens = [];
    readonly Dictionary<string, Curve> curves = new(StringComparer.Ordinal);
    int pos;
    bool supported = true;
    static readonly Regex Lex = new(@"\G(?:[ \t\r\n]+|\#[^\r\n]*|""(?:[^""\\\x00-\x1f]|\\(?:[""\\/bfnrt]|u[0-9a-fA-F]{4}))*""|[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?|[A-Za-z_][A-Za-z_0-9]*|>=|<=|==|[{}\[\](),%])", RegexOptions.CultureInvariant);
    FoilParser(byte[] bytes)
    {
        text = Guard.Text(bytes);
        int at = text.StartsWith('\uFEFF') ? 1 : 0;
        while (at < text.Length)
        {
            var m = Lex.Match(text, at);
            Guard.Require(m.Success, "DSL-LEX");
            string v = m.Value;
            if (!(char.IsWhiteSpace(v[0]) || v[0] == '#')) tokens.Add(new(v, at, at + v.Length));
            at += v.Length;
        }
        tokens.Add(new("EOF", text.Length, text.Length));
    }
    string Peek => pos < tokens.Count ? tokens[pos].Value : "EOF";
    Token Take() { Guard.Require(pos < tokens.Count, "DSL-SYNTAX"); return tokens[pos++]; }
    void Expect(string v) { Guard.Require(Peek == v, Peek == "chord" ? "DSL-LEGACY" : "DSL-SYNTAX"); pos++; }
    bool Optional(string v) { if (Peek != v) return false; pos++; return true; }
    string Name()
    {
        Guard.Require(Peek.StartsWith('"'), "DSL-SYNTAX");
        string value;
        try { value = JsonSerializer.Deserialize<string>(Take().Value)!; } catch (JsonException) { throw new ContractError("DSL-LEX"); }
        _ = Jcs.Quote(value);
        Guard.Require(value.EnumerateRunes().Count() <= 4096, "DSL-LIMIT");
        return value;
    }
    double Number(int scale = 0) => DecimalSi.Parse(Take().Value, scale);
    int Integer() { string s = Take().Value; Guard.Require(Regex.IsMatch(s, "^[0-9]+$") && s.Length <= 4, "DSL-CURVE"); return int.Parse(s, CultureInfo.InvariantCulture); }
    int Unit() => Take().Value switch { "m" => 0, "cm" => -2, "mm" => -3, _ => throw new ContractError("DSL-UNIT") };
    double Length() { string n = Take().Value; return DecimalSi.Parse(n, Unit()); }
    void Evaluator() { Expect("evaluator"); Guard.Require(Name() == "cfdw-cv" && Name() == "2", "DSL-VERSION"); }
    double[] Numbers()
    {
        Expect("["); var list = new List<double> { Number() };
        while (Optional(",")) { Guard.Require(list.Count < 38, "DSL-LIMIT"); list.Add(Number()); }
        Expect("]"); return list.ToArray();
    }
    Curve ReadCurve(string path, int scale, bool profile = false)
    {
        Expect("cv"); Expect("{"); Expect("degree"); int degree = Integer(); Expect("knots"); var knots = Numbers();
        Expect("points"); Expect("["); var points = new List<double[]>(); var ordinates = new List<Token>();
        do
        {
            Guard.Require(points.Count < (profile ? 32 : 10), "DSL-LIMIT");
            Expect("("); double x = Number(); Expect(","); ordinates.Add(tokens[pos]); double y = Number(scale); Expect(")"); points.Add([x, y]);
        } while (Optional(","));
        Expect("]"); bool missing = Peek != "ids"; var ids = new List<string>();
        if (Optional("ids")) { Expect("["); ids.Add(Name()); while (Optional(",")) { Guard.Require(ids.Count < 32, "DSL-LIMIT"); ids.Add(Name()); } Expect("]"); }
        else ids.AddRange(Enumerable.Range(0, points.Count).Select(i => $"cv-{i}"));
        int insert = tokens[pos].Start; Expect("}");
        Guard.Require(degree == (profile ? 5 : 3) && points.Count >= 6 && knots.Length == points.Count + degree + 1, "DSL-CURVE");
        Guard.Require(ids.Count == points.Count && ids.All(x => x.Length > 0) && ids.Distinct(StringComparer.Ordinal).Count() == ids.Count, "DSL-REFERENCE");
        Guard.Require(knots.Take(degree + 1).All(x => x == 0) && knots.TakeLast(degree + 1).All(x => x == 1), "DSL-CURVE");
        Guard.Require(knots.Zip(knots.Skip(1)).All(x => x.First <= x.Second), "DSL-CURVE");
        var interior = knots.Skip(degree + 1).Take(knots.Length - 2 * (degree + 1)).ToArray();
        Guard.Require(interior.All(x => x > 0 && x < 1) && interior.GroupBy(x => x).All(g => g.Count() <= degree), "DSL-CURVE");
        Guard.Require(points[0][0] == 0 && points[^1][0] == 1 && points.Zip(points.Skip(1)).All(p => profile ? p.First[0] <= p.Second[0] : p.First[0] < p.Second[0]), "DSL-CURVE");
        var curve = new Curve(path, degree, knots, points.ToArray(), ids.ToArray(), ordinates.ToArray(), insert, missing);
        curves.Add(path, curve); return curve;
    }
    public static Parsed Parse(byte[] bytes)
    {
        var p = new FoilParser(bytes); return p.Document(bytes);
    }
    Parsed Document(byte[] bytes)
    {
        Expect("foildsl"); Guard.Require(Name() == "4.0", "DSL-VERSION");
        if (Peek == "section") throw new ContractError("DSL-UNSUPPORTED");
        Expect("foil"); _ = Name(); Expect("{"); Expect("units"); int units = Unit(); Expect("half_span"); double h = Length(); Guard.Require(h > 0, "DSL-GEOMETRY");
        Evaluator(); Expect("symmetry"); Expect("mirror_y"); Expect("planform"); Expect("{");
        Expect("leading"); var leading = ReadCurve("leading", units); Expect("trailing"); var trailing = ReadCurve("trailing", units); Expect("}");
        Expect("dihedral"); var dihedral = ReadCurve("dihedral", units); Expect("twist"); var twist = ReadCurve("twist", 0); Expect("thickness"); var thickness = ReadCurve("thickness", 0);
        Expect("profiles"); Expect("{"); var profiles = new Dictionary<string, object>(StringComparer.Ordinal);
        do
        {
            Guard.Require(profiles.Count < 4096, "DSL-LIMIT"); Expect("profile"); string name = Name(); Guard.Require(name.Length > 0 && !profiles.ContainsKey(name), "DSL-REFERENCE"); Expect("{");
            if (Peek == "asset") throw new ContractError("DSL-UNSUPPORTED");
            Expect("upper"); var upper = ReadCurve($"profile:{name}:upper", 0, true); Expect("lower"); var lower = ReadCurve($"profile:{name}:lower", 0, true);
            string closure = Optional("closure") ? Take().Value : "closed"; Guard.Require(closure is "open" or "closed", "DSL-SYNTAX");
            if (Optional("provenance")) _ = Name(); Expect("}");
            Guard.Require(upper.Points[0][1] == 0 && lower.Points[0][1] == 0 && (closure != "closed" || upper.Points[^1][1] == 0 && lower.Points[^1][1] == 0), "DSL-GEOMETRY");
            profiles.Add(name, new Dictionary<string, object?> { ["evaluator"] = new[] { "cfdw-cv", "2" }, ["upper"] = upper.Semantic(), ["lower"] = lower.Semantic(), ["closure"] = closure });
        } while (Peek == "profile");
        Expect("}"); Expect("sections"); Expect("{"); var assignedNames = new List<string>(); var assignments = new List<object>(); var etas = new List<double>();
        do
        {
            Guard.Require(assignments.Count < 4096, "DSL-LIMIT"); Expect("at"); double eta;
            if (Optional("root") || Optional("center")) eta = 0;
            else if (Optional("tip")) eta = 1;
            else { string n = Take().Value; eta = Optional("%") ? DecimalSi.Parse(n, -2) : DecimalSi.Parse(n, Unit()) / h; }
            Expect("profile"); string name = Name(); Guard.Require(profiles.ContainsKey(name), "DSL-REFERENCE");
            if (!assignedNames.Contains(name, StringComparer.Ordinal)) assignedNames.Add(name);
            assignments.Add(new object[] { eta, assignedNames.IndexOf(name) }); etas.Add(eta);
        } while (Peek == "at");
        Expect("}"); Guard.Require(etas.Count >= 2 && etas[0] == 0 && etas[^1] == 1 && etas.Zip(etas.Skip(1)).All(x => x.First < x.Second) && assignedNames.Count == profiles.Count, "DSL-REFERENCE");
        string tip = Optional("tip") ? Take().Value : "open"; Guard.Require(tip is "open" or "point", "DSL-SYNTAX");
        if (Optional("locks"))
        {
            Expect("{");
            while (Peek != "}")
            {
                if (Optional("root_mirror")) { string channel = Take().Value; Guard.Require(curves.ContainsKey(channel), "DSL-REFERENCE"); }
                else throw new ContractError("DSL-UNSUPPORTED");
            }
            Expect("}");
        }
        if (Peek == "constrain") throw new ContractError("DSL-UNSUPPORTED");
        Expect("}"); Expect("EOF");
        supported = profiles.Count == 1 && tip == "open";
        var channels = new Dictionary<string, object?> { ["leading"] = leading.Semantic(), ["trailing"] = trailing.Semantic(), ["dihedral"] = dihedral.Semantic(), ["twist"] = twist.Semantic(), ["thickness"] = thickness.Semantic() };
        object semantic = new Dictionary<string, object?> { ["format"] = "foildsl-geometry-4.0", ["kind"] = "foil", ["evaluator"] = new[] { "cfdw-cv", "2" }, ["frame"] = "aft-starboard-up-root-le", ["symmetry"] = "mirror_y", ["half_span_m"] = h, ["channels"] = channels, ["profiles"] = assignedNames.Select(n => profiles[n]).ToArray(), ["assignments"] = assignments, ["tip"] = tip };
        return new(bytes.ToArray(), units, curves, semantic, supported);
    }

    public static byte[] Materialize(Parsed parsed)
    {
        string text = Guard.Text(parsed.Source);
        foreach (var c in parsed.Curves.Values.Where(c => c.MissingIds).OrderByDescending(c => c.InsertAt))
            text = text.Insert(c.InsertAt, " ids [" + string.Join(",", c.Ids.Select(Jcs.Quote)) + "] ");
        byte[] result = Guard.Utf8.GetBytes(text);
        Guard.Require(Parse(result).SurfaceHash == parsed.SurfaceHash, "DSL-PATCH"); return result;
    }

    public static byte[] Patch(Parsed parsed, string rail, string vertexId, double si)
    {
        Guard.Require(rail is "leading" or "trailing", "DSL-TARGET");
        var c = parsed.Curves[rail]; int index = Array.IndexOf(c.Ids, vertexId); Guard.Require(index >= 0 && !c.MissingIds, "DSL-TARGET");
        // Exact inverse scale on the binary64 value, emitted as a terminating decimal rational.
        string token = InverseScale(si, parsed.UnitScale);
        string text = Guard.Text(parsed.Source); var span = c.Ordinates[index];
        byte[] bytes = Guard.Utf8.GetBytes(text[..span.Start] + token + text[span.End..]);
        var reparsed = Parse(bytes);
        Guard.Require(BitConverter.DoubleToInt64Bits(reparsed.Curves[rail].Points[index][1]) == BitConverter.DoubleToInt64Bits(si == 0 ? 0 : si), "DSL-PATCH");
        Guard.Require(Jcs.Write(parsed.Curves[rail == "leading" ? "trailing" : "leading"].Semantic()) == Jcs.Write(reparsed.Curves[rail == "leading" ? "trailing" : "leading"].Semantic()), "DSL-PATCH");
        return bytes;
    }
    static string InverseScale(double value, int scale)
    {
        Guard.Require(double.IsFinite(value), "DSL-LEX"); if (value == 0) return "0";
        ulong bits = BitConverter.DoubleToUInt64Bits(Math.Abs(value)); int exponent = (int)((bits >> 52) & 2047);
        BigInteger n = (bits & ((1UL << 52) - 1)) + (exponent == 0 ? 0 : 1UL << 52);
        int binaryPower = exponent == 0 ? -1074 : exponent - 1023 - 52;
        if (binaryPower >= 0) n <<= binaryPower;
        int places = Math.Max(0, -binaryPower);
        n *= BigInteger.Pow(5, places) * BigInteger.Pow(10, -scale);
        string digits = n.ToString(CultureInfo.InvariantCulture).PadLeft(places + 1, '0');
        string result = places == 0 ? digits : digits.Insert(digits.Length - places, ".").TrimEnd('0').TrimEnd('.');
        return (value < 0 ? "-" : "") + result;
    }
}

sealed record SourceRow(string Id, string[] Utf8Base64Chunks);
sealed record DesignRow(string Id, string? Parent, string SurfaceHash, string Evaluator);
sealed record EditReceipt(string DraftId, long Generation, string Rail, string VertexId);
sealed record AcceptedRow(string Id, string? Parent, string SourceId, string DesignId, string OperationId, EditReceipt? Edit);
sealed record CursorRow(long Sequence, string Target, string Reason, string OperationId);
sealed record RecoveryRow(string DraftId, string BaseAcceptedId, long Generation, string Rail, string VertexId, string[] Utf8Base64Chunks);
sealed record Envelope(string Format, string ProjectId, SourceRow[] Sources, DesignRow[] Designs, AcceptedRow[] Accepted, CursorRow[] Cursors, RecoveryRow? Recovery);
sealed record Draft(string Id, string Base, long Generation, string Rail, string VertexId, byte[] Bytes);
sealed record Binding(string SourceHash, string Base, string DraftId, long Generation, string Evaluator, string SurfaceHash, string Rail, string VertexId);
sealed record Assessment(string Status, string Code, Binding? Key, Certificate? Certificate);
sealed record Diagnostic(string Code, string Phase, string Severity, int ByteStart, int ByteLength, int Line, int ScalarColumn, string? Entity, string Reason, string Recovery);
sealed class SaveRequest
{
    readonly byte[] image;
    public byte[] Image => image.ToArray();
    public string? ExpectedDiskSha256 { get; }
    public string OperationId { get; }
    public bool CreateOnly => ExpectedDiskSha256 is null;
    public SaveRequest(byte[] bytes, string? expectedDiskSha256, string operationId)
    {
        Native.Uuid(operationId);
        Guard.Require(expectedDiskSha256 is null || Regex.IsMatch(expectedDiskSha256, "^[0-9a-f]{64}$"), "DOC-INTEGRITY");
        image = bytes.ToArray(); ExpectedDiskSha256 = expectedDiskSha256; OperationId = operationId;
    }
}
sealed record SaveResult(string Code, string? PublishedDiskSha256, bool PublicationKnown, bool DurabilityConfirmed);
interface IProjectStore
{
    Task<SaveResult> Save(SaveRequest request, CancellationToken cancellation);
    Task<byte[]> Read(CancellationToken cancellation);
}
static class Locations
{
    public static (int Line, int Column) At(byte[] source, int byteOffset)
    {
        Guard.Require(byteOffset >= 0 && byteOffset <= source.Length, "DSL-RANGE");
        string prefix;
        try { prefix = Guard.Utf8.GetString(source.AsSpan(0, byteOffset)); } catch (DecoderFallbackException) { throw new ContractError("DSL-RANGE"); }
        int line = 1, column = 1; bool previousCr = false;
        foreach (var rune in prefix.EnumerateRunes())
        {
            if (rune.Value == '\r') { line++; column = 1; previousCr = true; }
            else if (rune.Value == '\n') { if (!previousCr) line++; column = 1; previousCr = false; }
            else { column++; previousCr = false; }
        }
        return (line, column);
    }
}
sealed class Certificate
{
    public Binding Key { get; }
    internal Certificate(Binding key) { Key = key; }
}
sealed record View(string AcceptedId, string SourceHash, string SurfaceHash, byte[] Source, Draft? Draft, RecoveryRow? Recovery, bool Dirty);

interface IAuthoringSession
{
    byte[] Open(byte[] source, string operationId, bool acceptIdInsertion);
    Draft BeginRailEdit(string draftId, string rail, string vertexId);
    Draft UpdateDraft(string draftId, long expectedGeneration, double si);
    Assessment Validate(string draftId, long generation, CancellationToken cancellation);
    string Apply(string operationId, Assessment assessment);
    void Cancel(string draftId);
    string Undo(string operationId);
    string Redo(string operationId);
    View Snapshot();
    RecoveryRow CaptureRecovery();
    void ResumeRecovery();
    void DiscardRecovery();
    byte[] SaveImage();
    void AcknowledgeSaved(byte[] image);
    void Reopen(byte[] image);
}

interface ICertificateAuthority { Assessment Assess(Parsed candidate, Binding key, CancellationToken cancellation); }
sealed class WrongBindingAuthority : ICertificateAuthority
{
    public Assessment Assess(Parsed candidate, Binding key, CancellationToken cancellation)
    {
        var wrong = key with { SourceHash = new string('0', 64) };
        return new("Fixture admitted", "OK", wrong, new Certificate(wrong));
    }
}

// This allowlist exercises the session port, not geometry. Unknown candidates remain Not assessed.
sealed class FixtureAuthority(HashSet<string> allowed) : ICertificateAuthority
{
    public Assessment Assess(Parsed candidate, Binding key, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested) return new("Not assessed", "DSL-CANCELLED", key, null);
        return candidate.Supported && allowed.Contains(candidate.SurfaceHash)
            ? new("Fixture admitted", "OK", key, new Certificate(key))
            : new("Not assessed", "DSL-NOT-ASSESSED", key, null);
    }
}

sealed class Session(ICertificateAuthority authority, int envelopeCap = Native.MaxBytes) : IAuthoringSession
{
    readonly object sync = new();
    readonly List<SourceRow> sources = [];
    readonly List<DesignRow> designs = [];
    readonly List<AcceptedRow> accepted = [];
    readonly List<CursorRow> cursors = [];
    readonly Stack<string> redo = [];
    readonly Dictionary<string, (string Payload, string Result)> operations = [];
    Draft? draft;
    RecoveryRow? recovery;
    string? current;
    string projectId = Guard.Id(1);
    string? savedImageHash;
    public static string[] Chunks(byte[] bytes) => Enumerable.Range(0, (bytes.Length + 2303) / 2304).Select(i => Convert.ToBase64String(bytes.Skip(i * 2304).Take(2304).ToArray())).ToArray();
    static byte[] Decode(string[] chunks) => chunks.SelectMany(Convert.FromBase64String).ToArray();
    AcceptedRow Current => accepted.Single(a => a.Id == current);
    byte[] CurrentBytes => Decode(sources.Single(s => s.Id == Current.SourceId).Utf8Base64Chunks);
    Binding Key(Parsed p, Draft d) => new(p.SourceHash, d.Base, d.Id, d.Generation, "cfdw-cv/2", p.SurfaceHash, d.Rail, d.VertexId);
    void RequireAdmission(Parsed p, Binding key)
    {
        var assessment = authority.Assess(p, key, default);
        Guard.Require(assessment.Status == "Fixture admitted" && assessment.Key == key && assessment.Certificate?.Key == key, "DSL-NOT-ASSESSED");
    }
    bool Retry(string op, string payload, out string result)
    {
        Native.Uuid(op);
        if (operations.TryGetValue(op, out var prior)) { Guard.Require(prior.Payload == payload, "DOC-OPERATION-CONFLICT"); result = prior.Result; return true; }
        result = ""; return false;
    }
    public byte[] Open(byte[] source, string operationId, bool acceptIdInsertion)
    {
        lock (sync)
        {
            var p = FoilParser.Parse(source);
            byte[] candidate = FoilParser.Materialize(p);
            if (!candidate.SequenceEqual(source) && !acceptIdInsertion) return candidate;
            p = FoilParser.Parse(candidate);
            if (Retry(operationId, "open:" + p.SourceHash, out _)) return candidate;
            Guard.Require(current is null, "DOC-SESSION-NOT-EMPTY");
            var key = new Binding(p.SourceHash, "", "", 0, "cfdw-cv/2", p.SurfaceHash, "", "");
            RequireAdmission(p, key);
            string id = Commit(p, operationId, "open"); operations.Add(operationId, ("open:" + p.SourceHash, id)); return candidate;
        }
    }
    string Commit(Parsed p, string op, string reason)
    {
        Native.Uuid(op);
        string? parent = current; string? priorDesign = current is null ? null : Current.DesignId;
        string design = priorDesign is not null && designs.Single(d => d.Id == priorDesign).SurfaceHash == p.SurfaceHash ? priorDesign : Guard.Id(1000 + designs.Count);
        var nextDesigns = designs.ToList(); var nextSources = sources.ToList();
        if (!nextDesigns.Any(d => d.Id == design)) nextDesigns.Add(new(design, priorDesign, p.SurfaceHash, "cfdw-cv/2"));
        if (!nextSources.Any(s => s.Id == p.SourceHash)) nextSources.Add(new(p.SourceHash, Chunks(p.Source)));
        string id = Guard.Id(2000 + accepted.Count);
        var row = new AcceptedRow(id, parent, p.SourceHash, design, op, draft is null ? null : new(draft.Id, draft.Generation, draft.Rail, draft.VertexId));
        var cursor = new CursorRow(cursors.Count, id, reason, op);
        var prospective = new Envelope("cfdw-project-1", projectId, nextSources.ToArray(), nextDesigns.ToArray(), [.. accepted, row], [.. cursors, cursor], null);
        Native.Preflight(prospective, envelopeCap);
        designs.Clear(); designs.AddRange(nextDesigns); sources.Clear(); sources.AddRange(nextSources);
        accepted.Add(row); current = id; cursors.Add(cursor); redo.Clear(); return id;
    }
    public Draft BeginRailEdit(string draftId, string rail, string vertexId)
    {
        lock (sync)
        {
            Native.Uuid(draftId); Guard.Require(current is not null && draft is null, "DSL-DRAFT-OWNED");
            var p = FoilParser.Parse(CurrentBytes); Guard.Require(rail is "leading" or "trailing" && p.Curves[rail].Ids.Contains(vertexId), "DSL-TARGET");
            draft = new(draftId, current!, 0, rail, vertexId, CurrentBytes); return Copy(draft);
        }
    }
    static Draft Copy(Draft d) => d with { Bytes = d.Bytes.ToArray() };
    public Draft UpdateDraft(string draftId, long expectedGeneration, double si)
    {
        lock (sync)
        {
            Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == expectedGeneration && expectedGeneration < 9007199254740991, "DSL-CONFLICT");
            draft = draft! with { Generation = expectedGeneration + 1, Bytes = FoilParser.Patch(FoilParser.Parse(draft.Bytes), draft.Rail, draft.VertexId, si) }; return Copy(draft);
        }
    }
    public Assessment Validate(string draftId, long generation, CancellationToken cancellation)
    {
        Draft capture;
        lock (sync) { Guard.Require(draft is not null && draft.Id == draftId && draft.Generation == generation, "DSL-CONFLICT"); capture = Copy(draft!); }
        try { var p = FoilParser.Parse(capture.Bytes); return authority.Assess(p, Key(p, capture), cancellation); }
        catch (ContractError e) { return new(e.Code is "DSL-LIMIT" or "DSL-UNSUPPORTED" ? "Not assessed" : "Invalid", e.Code, null, null); }
    }
    public string Apply(string operationId, Assessment assessment)
    {
        lock (sync)
        {
            Guard.Require(assessment.Key is not null, "DSL-NOT-ASSESSED");
            string payload = "apply:" + JsonSerializer.Serialize(assessment.Key);
            if (Retry(operationId, payload, out string prior)) return prior;
            Guard.Require(draft is not null && current == draft.Base, "DSL-CONFLICT");
            var p = FoilParser.Parse(draft!.Bytes); var key = Key(p, draft);
            Guard.Require(assessment.Status == "Fixture admitted" && assessment.Certificate is not null && assessment.Certificate.Key == key && assessment.Key == key, "DSL-CONFLICT");
            string id = Commit(p, operationId, "apply"); operations.Add(operationId, (payload, id)); draft = null; recovery = null; return id;
        }
    }
    public void Cancel(string draftId) { lock (sync) { Guard.Require(draft?.Id == draftId, "DSL-CONFLICT"); draft = null; recovery = null; } }
    public string Undo(string operationId) => Move(operationId, false);
    public string Redo(string operationId) => Move(operationId, true);
    string Move(string op, bool forward)
    {
        lock (sync)
        {
            string payload = forward ? "redo" : "undo";
            if (Retry(op, payload, out string prior)) return prior;
            Guard.Require(draft is null && current is not null, "DSL-DRAFT-OWNED");
            string? target = forward ? redo.Count > 0 ? redo.Peek() : null : Current.Parent;
            if (target is null) { operations.Add(op, (payload, current!)); return current!; }
            var targetRow = accepted.Single(a => a.Id == target);
            var targetParsed = FoilParser.Parse(Decode(sources.Single(s => s.Id == targetRow.SourceId).Utf8Base64Chunks));
            RequireAdmission(targetParsed, new(targetParsed.SourceHash, "", "", 0, "cfdw-cv/2", targetParsed.SurfaceHash, "", ""));
            var cursor = new CursorRow(cursors.Count, target, payload, op);
            Native.Preflight(Envelope() with { Cursors = [.. cursors, cursor] }, envelopeCap);
            if (forward) redo.Pop(); else redo.Push(current!);
            current = target; cursors.Add(cursor); operations.Add(op, (payload, target)); return target;
        }
    }
    public View Snapshot()
    {
        lock (sync)
        {
            Guard.Require(current is not null, "DOC-EMPTY");
            return new(current!, Current.SourceId, designs.Single(d => d.Id == Current.DesignId).SurfaceHash, CurrentBytes, draft is null ? null : Copy(draft), CopyRecovery(recovery), draft is not null || savedImageHash != Guard.Sha(Native.Encode(Envelope())));
        }
    }
    public RecoveryRow CaptureRecovery()
    {
        lock (sync)
        {
            Guard.Require(draft is not null, "DOC-NO-RECOVERY");
            var next = new RecoveryRow(draft!.Id, draft.Base, draft.Generation, draft.Rail, draft.VertexId, Chunks(draft.Bytes));
            Native.Preflight(Envelope() with { Recovery = next }, envelopeCap);
            recovery = next; return CopyRecovery(recovery)!;
        }
    }
    public void ResumeRecovery()
    {
        lock (sync) { Guard.Require(recovery is not null && draft is null && recovery.BaseAcceptedId == current, "DOC-RECOVERY-BASE"); draft = new(recovery!.DraftId, recovery.BaseAcceptedId, recovery.Generation, recovery.Rail, recovery.VertexId, Decode(recovery.Utf8Base64Chunks)); }
    }
    public void DiscardRecovery() { lock (sync) { Guard.Require(draft is null, "DSL-DRAFT-OWNED"); recovery = null; } }
    static RecoveryRow? CopyRecovery(RecoveryRow? r) => r is null ? null : r with { Utf8Base64Chunks = r.Utf8Base64Chunks.ToArray() };
    public Envelope Envelope() { lock (sync) return new("cfdw-project-1", projectId, sources.Select(s => s with { Utf8Base64Chunks = s.Utf8Base64Chunks.ToArray() }).ToArray(), designs.ToArray(), accepted.ToArray(), cursors.ToArray(), CopyRecovery(recovery)); }
    public byte[] SaveImage() { lock (sync) { Native.Preflight(Envelope(), envelopeCap); return Native.Encode(Envelope()); } }
    public void AcknowledgeSaved(byte[] image) { lock (sync) savedImageHash = Guard.Sha(image); }
    public void Reopen(byte[] image)
    {
        lock (sync)
        {
            Guard.Require(current is null, "DOC-SESSION-NOT-EMPTY"); var env = Native.Read(image);
            var replay = Native.Replay(env); var active = env.Accepted.Single(a => a.Id == replay.Current);
            var p = FoilParser.Parse(Decode(env.Sources.Single(s => s.Id == active.SourceId).Utf8Base64Chunks));
            var key = new Binding(p.SourceHash, "", "", 0, "cfdw-cv/2", p.SurfaceHash, "", "");
            RequireAdmission(p, key);
            projectId = env.ProjectId;
            sources.AddRange(env.Sources); designs.AddRange(env.Designs); accepted.AddRange(env.Accepted); cursors.AddRange(env.Cursors); recovery = env.Recovery;
            current = replay.Current;
            foreach (string id in replay.Redo.Reverse()) redo.Push(id);
            foreach (var c in cursors)
            {
                if (c.Reason is "undo" or "redo") operations[c.OperationId] = (c.Reason, c.Target);
                else
                {
                    var a = env.Accepted.Single(a => a.Id == c.Target);
                    var d = env.Designs.Single(d => d.Id == a.DesignId);
                    string payload = c.Reason == "open" ? "open:" + a.SourceId : "apply:" + JsonSerializer.Serialize(new Binding(a.SourceId, a.Parent!, a.Edit!.DraftId, a.Edit.Generation, d.Evaluator, d.SurfaceHash, a.Edit.Rail, a.Edit.VertexId));
                    operations[c.OperationId] = (payload, c.Target);
                }
            }
            // Dirty-state identity is independent of original on-disk formatting/conflict token.
            savedImageHash = Guard.Sha(Native.Encode(Envelope()));
        }
    }
}

static class Native
{
    public const int MaxBytes = 8_000_000;
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    public static void Uuid(string id) => Guard.Require(Regex.IsMatch(id, "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$") && Guid.TryParseExact(id, "D", out _), "DOC-ID");
    static void Hash(string hash) => Guard.Require(Regex.IsMatch(hash, "^[0-9a-f]{64}$"), "DOC-INTEGRITY");
    public static byte[] Encode(Envelope envelope) => JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
    public static void Preflight(Envelope envelope, int cap)
    {
        byte[] bytes = Encode(envelope);
        Guard.Require(bytes.Length <= cap && Guard.Utf8.GetString(bytes).Split('\n').All(l => Guard.Utf8.GetByteCount(l.TrimEnd('\r')) <= 4096), "DOC-SIZE");
    }
    static byte[] Decode(string[] chunks)
    {
        Guard.Require(chunks.Length > 0, "DOC-SCHEMA"); var result = new List<byte>();
        foreach (string chunk in chunks)
        {
            Guard.Require(chunk.Length is > 0 and <= 3072 && chunk.Length % 4 == 0, "DOC-SCHEMA");
            byte[] bytes; try { bytes = Convert.FromBase64String(chunk); } catch (FormatException) { throw new ContractError("DOC-SCHEMA"); }
            Guard.Require(Convert.ToBase64String(bytes) == chunk, "DOC-SCHEMA");
            Guard.Require(result.Count + bytes.Length <= 1_048_576, "DOC-SIZE"); result.AddRange(bytes);
        }
        for (int i = 0; i + 1 < chunks.Length; i++) Guard.Require(!chunks[i].Contains('='), "DOC-SCHEMA");
        _ = Guard.Text(result.ToArray()); return result.ToArray();
    }
    static void Exact(JsonElement element, params string[] keys)
    {
        Guard.Require(element.ValueKind == JsonValueKind.Object, "DOC-SCHEMA");
        var names = element.EnumerateObject().Select(p => p.Name).ToArray();
        Guard.Require(names.Distinct(StringComparer.Ordinal).Count() == names.Length, "DOC-SCHEMA");
        Guard.Require(keys.All(names.Contains), "DOC-SCHEMA");
        Guard.Require(names.All(keys.Contains), "DOC-UNSUPPORTED-FIELD");
    }
    public static Envelope Read(byte[] bytes)
    {
        Guard.Require(bytes.Length <= MaxBytes, "DOC-SIZE");
        string text; try { text = Guard.Utf8.GetString(bytes); } catch (DecoderFallbackException) { throw new ContractError("DOC-SCHEMA"); }
        Guard.Require(!text.StartsWith('\uFEFF') && text.Split('\n').All(l => Guard.Utf8.GetByteCount(l.TrimEnd('\r')) <= 4096), "DOC-SCHEMA");
        try
        {
            using var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 }); var root = doc.RootElement;
            Exact(root, "format", "projectId", "sources", "designs", "accepted", "cursors", "recovery");
            Guard.Require(root.GetProperty("format").GetString() == "cfdw-project-1", "DOC-VERSION");
            foreach (var s in root.GetProperty("sources").EnumerateArray()) Exact(s, "id", "utf8Base64Chunks");
            foreach (var d in root.GetProperty("designs").EnumerateArray()) Exact(d, "id", "parent", "surfaceHash", "evaluator");
            foreach (var a in root.GetProperty("accepted").EnumerateArray())
            {
                Exact(a, "id", "parent", "sourceId", "designId", "operationId", "edit");
                if (a.GetProperty("edit").ValueKind != JsonValueKind.Null) Exact(a.GetProperty("edit"), "draftId", "generation", "rail", "vertexId");
            }
            foreach (var c in root.GetProperty("cursors").EnumerateArray()) Exact(c, "sequence", "target", "reason", "operationId");
            if (root.GetProperty("recovery").ValueKind != JsonValueKind.Null) Exact(root.GetProperty("recovery"), "draftId", "baseAcceptedId", "generation", "rail", "vertexId", "utf8Base64Chunks");
            var env = JsonSerializer.Deserialize<Envelope>(bytes, Options)!; Check(env); return env;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or ArgumentException or NullReferenceException or KeyNotFoundException) { throw new ContractError("DOC-SCHEMA"); }
    }
    static void Check(Envelope e)
    {
        Uuid(e.ProjectId);
        Guard.Require(e.Sources.Length > 0 && e.Designs.Length > 0 && e.Accepted.Length > 0 && e.Cursors.Length > 0, "DOC-REFERENCE");
        Guard.Require(e.Designs.All(d => d.Evaluator == "cfdw-cv/2"), "DOC-VERSION");
        Guard.Require(e.Sources.Select(x => x.Id).Distinct().Count() == e.Sources.Length && e.Designs.Select(x => x.Id).Distinct().Count() == e.Designs.Length && e.Accepted.Select(x => x.Id).Distinct().Count() == e.Accepted.Length, "DOC-REFERENCE");
        var parsed = new Dictionary<string, Parsed>();
        foreach (var s in e.Sources) { Hash(s.Id); byte[] bytes = Decode(s.Utf8Base64Chunks); Guard.Require(Guard.Sha(bytes) == s.Id, "DOC-INTEGRITY"); var p = FoilParser.Parse(bytes); Guard.Require(p.Curves.Values.All(c => !c.MissingIds), "DOC-INTEGRITY"); parsed.Add(s.Id, p); }
        var designs = new Dictionary<string, DesignRow>();
        foreach (var d in e.Designs) { Uuid(d.Id); Hash(d.SurfaceHash); Guard.Require(d.Evaluator == "cfdw-cv/2" && (designs.Count == 0 ? d.Parent is null : d.Parent is not null && designs.ContainsKey(d.Parent)), "DOC-REFERENCE"); designs.Add(d.Id, d); }
        var accepted = new Dictionary<string, AcceptedRow>();
        foreach (var a in e.Accepted)
        {
            Uuid(a.Id); Uuid(a.OperationId); Guard.Require(accepted.Count == 0 ? a.Parent is null : a.Parent is not null && accepted.ContainsKey(a.Parent), "DOC-REFERENCE");
            Guard.Require(parsed.ContainsKey(a.SourceId) && designs.ContainsKey(a.DesignId), "DOC-REFERENCE");
            Guard.Require(parsed[a.SourceId].SurfaceHash == designs[a.DesignId].SurfaceHash, "DOC-INTEGRITY");
            if (a.Parent is not null)
            {
                Guard.Require(a.Edit is not null, "DOC-REFERENCE"); Uuid(a.Edit!.DraftId);
                Guard.Require(a.Edit.Generation is >= 0 and <= 9007199254740991 && a.Edit.Rail is "leading" or "trailing" && parsed[a.SourceId].Curves[a.Edit.Rail].Ids.Contains(a.Edit.VertexId), "DOC-REFERENCE");
                var parent = accepted[a.Parent]; bool same = parsed[a.SourceId].SurfaceHash == parsed[parent.SourceId].SurfaceHash;
                Guard.Require(parsed[parent.SourceId].Curves[a.Edit.Rail].Ids.Contains(a.Edit.VertexId), "DOC-REFERENCE");
                Guard.Require(same ? a.DesignId == parent.DesignId : designs[a.DesignId].Parent == parent.DesignId, "DOC-REFERENCE");
            }
            else Guard.Require(a.Edit is null, "DOC-REFERENCE");
            accepted.Add(a.Id, a);
        }
        Guard.Require(e.Sources.All(s => e.Accepted.Any(a => a.SourceId == s.Id)) && e.Designs.All(d => e.Accepted.Any(a => a.DesignId == d.Id)), "DOC-REFERENCE");
        _ = Replay(e);
        if (e.Recovery is not null)
        {
            var r = e.Recovery; Uuid(r.DraftId); Guard.Require(accepted.ContainsKey(r.BaseAcceptedId) && r.Generation is >= 0 and <= 9007199254740991 && r.Rail is "leading" or "trailing", "DOC-REFERENCE");
            Guard.Require(r.VertexId.Length > 0 && r.VertexId.EnumerateRunes().Count() <= 4096 && parsed[accepted[r.BaseAcceptedId].SourceId].Curves[r.Rail].Ids.Contains(r.VertexId), "DOC-REFERENCE"); _ = Decode(r.Utf8Base64Chunks);
        }
    }
    public static (string Current, string[] Redo) Replay(Envelope e)
    {
        string? current = null; var redo = new Stack<string>(); var seen = new HashSet<string>(); int nextAccepted = 0;
        for (int i = 0; i < e.Cursors.Length; i++)
        {
            var c = e.Cursors[i]; Uuid(c.OperationId); Guard.Require(c.Sequence == i && seen.Add(c.OperationId), "DOC-REFERENCE");
            var target = e.Accepted.SingleOrDefault(a => a.Id == c.Target); Guard.Require(target is not null, "DOC-REFERENCE");
            switch (c.Reason)
            {
                case "open": Guard.Require(i == 0 && target!.Parent is null, "DOC-REFERENCE"); goto case "apply";
                case "apply":
                    Guard.Require(i > 0 || c.Reason == "open", "DOC-REFERENCE");
                    Guard.Require(nextAccepted < e.Accepted.Length && e.Accepted[nextAccepted] == target && target!.Parent == current && target.OperationId == c.OperationId, "DOC-REFERENCE");
                    nextAccepted++; redo.Clear(); break;
                case "undo":
                    Guard.Require(current is not null && e.Accepted.Single(a => a.Id == current).Parent == c.Target, "DOC-REFERENCE"); redo.Push(current!); break;
                case "redo":
                    Guard.Require(redo.Count > 0 && redo.Peek() == c.Target && target!.Parent == current, "DOC-REFERENCE"); redo.Pop(); break;
                default: throw new ContractError("DOC-REFERENCE");
            }
            current = c.Target;
        }
        Guard.Require(nextAccepted == e.Accepted.Length && current is not null, "DOC-REFERENCE");
        Guard.Require(e.Accepted.Select(a => a.OperationId).Distinct().Count() == e.Accepted.Length, "DOC-REFERENCE");
        return (current!, redo.ToArray());
    }
}

static class Program
{
    public const string Example = """
foildsl "4.0"
# Built-in manufactured contract Example. No simulation results.
foil "Example" {
 units mm half_span 450 mm evaluator "cfdw-cv" "2" symmetry mirror_y
 planform {
 leading cv { degree 3 knots [0,0,0,0,0.3333333333333333,0.6666666666666666,1,1,1,1] points [(0,0),(0.2,0),(0.4,0),(0.6,0),(0.8,0),(1,0)] }
 trailing cv { degree 3 knots [0,0,0,0,0.3333333333333333,0.6666666666666666,1,1,1,1] points [(0,120),(0.2,120),(0.4,120),(0.6,120),(0.8,120),(1,120)] }
 }
 dihedral cv { degree 3 knots [0,0,0,0,0.3333333333333333,0.6666666666666666,1,1,1,1] points [(0,0),(0.2,0),(0.4,0),(0.6,0),(0.8,0),(1,0)] }
 twist cv { degree 3 knots [0,0,0,0,0.3333333333333333,0.6666666666666666,1,1,1,1] points [(0,0),(0.2,0),(0.4,0),(0.6,0),(0.8,0),(1,0)] }
 thickness cv { degree 3 knots [0,0,0,0,0.3333333333333333,0.6666666666666666,1,1,1,1] points [(0,0.12),(0.2,0.12),(0.4,0.12),(0.6,0.12),(0.8,0.12),(1,0.12)] }
 profiles { profile "manufactured" {
 upper cv { degree 5 knots [0,0,0,0,0,0,1,1,1,1,1,1] points [(0,0),(0.2,0.04),(0.4,0.06),(0.6,0.06),(0.8,0.04),(1,0)] }
 lower cv { degree 5 knots [0,0,0,0,0,0,1,1,1,1,1,1] points [(0,0),(0.2,-0.04),(0.4,-0.06),(0.6,-0.06),(0.8,-0.04),(1,0)] }
 } }
 sections { at root profile "manufactured" at tip profile "manufactured" }
}
""";
    static readonly List<string> Checks = [];
    static void Check(string name, bool condition) { if (!condition) throw new Exception(name); Checks.Add(name); }
    static void Reject(string name, string code, Action action)
    {
        try { action(); } catch (ContractError e) { Check(name, e.Code == code); return; }
        throw new Exception(name + ": accepted unexpectedly");
    }
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--number") { Console.WriteLine(Jcs.Number(DecimalSi.Parse(args[1]))); return; }
        if (args.Length == 1 && args[0] == "--batch")
        {
            string? line;
            while ((line = Console.ReadLine()) is not null)
            {
                var input = JsonSerializer.Deserialize<string[]>(line)!;
                try
                {
                    double d = input[0] == "bits" ? BitConverter.UInt64BitsToDouble(ulong.Parse(input[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture)) : DecimalSi.Parse(input[1], int.Parse(input[2], CultureInfo.InvariantCulture));
                    Console.WriteLine(JsonSerializer.Serialize(new { bits = BitConverter.DoubleToUInt64Bits(d).ToString("x16"), canonical = Jcs.Number(d) }));
                }
                catch (ContractError e) { Console.WriteLine(JsonSerializer.Serialize(new { error = e.Code })); }
            }
            return;
        }
        var clock = System.Diagnostics.Stopwatch.StartNew();
        string TwistPair(string ordinate) => Regex.Replace(Example, @"twist cv \{[^}]+\}",
            "twist cv { degree 3 knots [0,0,0,0,0.5,0.5,0.5,1,1,1,1] points [(0,0),(0.125,0),(0.25," + ordinate + "),(0.5,0),(0.75,0),(0.875,0),(1,0)] }");
        var twistA = FoilParser.Parse(Encoding.UTF8.GetBytes(TwistPair("1.791")));
        var twistB = FoilParser.Parse(Encoding.UTF8.GetBytes(TwistPair("1.7910000000000001")));
        Check("Ruling17_TwistDegreeInputs_DoNotCollapseIdentity", twistA.SurfaceHash != twistB.SurfaceHash);
        Reject("Ruling17_LegacyEvaluator_NoSilentAdoption", "DSL-VERSION", () =>
            FoilParser.Parse(Encoding.UTF8.GetBytes(Example.Replace("\"cfdw-cv\" \"2\"", "\"cfdw-cv\" \"1\""))));
        var numbers = new[] { ("14.049", -3), ("1.4049", -2), ("0.014049", 0), ("-0", 0), ("1e-400", 0), ("5e-324", 0), ("9007199254740993", 0), ("1.00000000000000011102230246251565404236316680908203125", 0), ("0e1000000000", 0), ("-1e-400", 0), ("1" + new string('0', 401) + "e-401", 0), ("0." + new string('0', 400) + "1e401", 0) };
        var numberRows = numbers.Select(x => new { token = x.Item1, scale = x.Item2, bits = BitConverter.DoubleToUInt64Bits(DecimalSi.Parse(x.Item1, x.Item2)).ToString("x16"), canonical = Jcs.Number(DecimalSi.Parse(x.Item1, x.Item2)) }).ToArray();
        Check("Decimal_Units_14_049_ExactEquivalent", numberRows.Take(3).Select(x => x.bits).Distinct().Count() == 1);
        Check("Decimal_Tie_Even", numberRows[7].bits == "3ff0000000000000");
        Check("Decimal_ZeroHugeExponent_Zero", numberRows[8].bits == "0000000000000000");
        Check("Decimal_NegativeUnderflow_NormalizedZero", numberRows[9].bits == "0000000000000000");
        Check("Decimal_CompensatedExponent_Equivalent", numberRows[10].bits == "3ff0000000000000" && numberRows[11].bits == "3ff0000000000000");
        Check("Decimal_TokenLimitBoundary_Admitted", DecimalSi.Parse(new string('0', 4095) + "1") == 1);
        Check("Decimal_Underflow_Zero", numberRows[4].bits == "0000000000000000");
        Check("Decimal_Subnormal_Minimum", numberRows[5].bits == "0000000000000001");
        Reject("Decimal_HugePositiveExponent_Bounded", "DSL-LIMIT", () => DecimalSi.Parse("1e1000000000"));
        Reject("Decimal_HugeNegativeExponent_Bounded", "DSL-LIMIT", () => DecimalSi.Parse("1e-1000000000"));
        Reject("Decimal_Overflow_Refused", "DSL-LEX", () => DecimalSi.Parse("1e309"));
        Reject("Decimal_TokenLimit_Refused", "DSL-LIMIT", () => DecimalSi.Parse(new string('1', 4097)));
        Reject("Decimal_UnicodeDigits_StableLexicalRefusal", "DSL-LEX", () => DecimalSi.Parse("١٢.٣"));
        object unicode = new Dictionary<string, object?> { ["\uE000"] = 1e21, ["😀"] = -0.0, ["a"] = new object[] { 1e-7, 1e-6, "\n\"\\é" } };
        string canonical = Jcs.Write(unicode);
        Check("Jcs_Utf16Ordering_ExponentAndEscapes", canonical == "{\"a\":[1e-7,0.000001,\"\\n\\\"\\\\é\"],\"😀\":0,\"\uE000\":1e+21}");
        Check("Blake3_OfficialEmptyVector", Blake3.Hasher.Hash([]).ToString() == "af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262");
        byte[] raw = Guard.Utf8.GetBytes("\uFEFF" + Example.Replace("\n", "\r\n"));
        var parsed = FoilParser.Parse(raw); byte[] materialized = FoilParser.Materialize(parsed); var p = FoilParser.Parse(materialized);
        Check("Open_MissingIds_MaterializationPreservesMeaning", p.SurfaceHash == parsed.SurfaceHash && p.Curves.Values.All(c => !c.MissingIds) && Guard.Text(materialized).StartsWith('\uFEFF'));
        Check("Materialize_Repeated_Idempotent", FoilParser.Materialize(p).SequenceEqual(materialized));
        byte[] changed = FoilParser.Patch(p, "leading", "cv-2", DecimalSi.Parse("14.049", -3));
        var changedParsed = FoilParser.Parse(changed);
        Check("Patch_IndependentRail_ExactUntouchedSuffix", Guard.Text(changed)[Guard.Text(changed).IndexOf("trailing", StringComparison.Ordinal)..] == Guard.Text(materialized)[Guard.Text(materialized).IndexOf("trailing", StringComparison.Ordinal)..]);
        Check("Patch_ExactRoundTrip_SI", changedParsed.Curves["leading"].Points[2][1] == DecimalSi.Parse("0.014049"));
        var comment = FoilParser.Parse(Guard.Utf8.GetBytes(Guard.Text(materialized) + "\r\n# private-marker-42"));
        Check("Source_Trivia_SHAChangesSurfaceStable", comment.SourceHash != p.SourceHash && comment.SurfaceHash == p.SurfaceHash);
        byte[] diagnosticSource = Guard.Utf8.GetBytes("é😀\r\nx");
        Check("Diagnostic_Utf8ScalarCrLf_ExactLocation", Locations.At(diagnosticSource, 6) == (1, 3) && Locations.At(diagnosticSource, 8) == (2, 1));
        Reject("Diagnostic_InteriorUtf8Byte_Refused", "DSL-RANGE", () => Locations.At(diagnosticSource, 1));
        Reject("Parser_InvalidUtf8_Refused", "DSL-LEX", () => FoilParser.Parse([0xff]));
        Reject("Parser_TrailingUnknown_Refused", "DSL-SYNTAX", () => FoilParser.Parse(Guard.Utf8.GetBytes(Example + " malicious")));
        Reject("Parser_DuplicateField_Refused", "DSL-SYNTAX", () => FoilParser.Parse(Guard.Utf8.GetBytes(Example.Replace("half_span", "units mm half_span"))));
        Reject("Parser_HugeExponent_Refused", "DSL-LIMIT", () => FoilParser.Parse(Guard.Utf8.GetBytes(Example.Replace("450 mm", "1e1000000000 mm"))));
        var allowed = new HashSet<string> { p.SurfaceHash, changedParsed.SurfaceHash };
        var authority = new FixtureAuthority(allowed); var session = new Session(authority);
        var truncatedSession = new Session(authority);
        Reject("Open_TruncatedLength_StableRefusal", "DSL-SYNTAX", () => truncatedSession.Open(Guard.Utf8.GetBytes("foildsl \"4.0\" foil \"x\" { units m half_span"), Guard.Id(10), true));
        Reject("Open_TruncatedLength_NoAdoption", "DOC-EMPTY", () => truncatedSession.Snapshot());
        Reject("Open_UnrelatedCertificate_Refused", "DSL-NOT-ASSESSED", () => new Session(new WrongBindingAuthority()).Open(materialized, Guard.Id(10), true));
        byte[] proposal = session.Open(raw, Guard.Id(10), false);
        Check("Open_IdDiff_RequiresAcceptance", proposal.SequenceEqual(materialized));
        Reject("Open_UnacceptedIdDiff_NoAcceptedState", "DOC-EMPTY", () => session.Snapshot());
        session.Open(raw, Guard.Id(10), true); var initial = session.Snapshot();
        Check("Open_Retry_ExactlyOnce", session.Open(raw, Guard.Id(10), true).SequenceEqual(materialized) && session.Envelope().Accepted.Length == 1);
        Reject("Open_OperationChanged_Refused", "DOC-OPERATION-CONFLICT", () => session.Open(changed, Guard.Id(10), true));
        session.BeginRailEdit(Guard.Id(20), "leading", "cv-2"); var d1 = session.UpdateDraft(Guard.Id(20), 0, DecimalSi.Parse("0.014049"));
        Reject("Draft_RetargetBegin_Refused", "DSL-DRAFT-OWNED", () => session.BeginRailEdit(d1.Id, "trailing", "cv-2"));
        var localDraft = d1 with { Rail = "trailing", VertexId = "cv-3" }; localDraft.Bytes[0] = 0;
        Check("Draft_ExternalRetargetMutation_PreservesOwnedTarget", session.Snapshot().Draft!.Rail == "leading" && session.Snapshot().Draft!.VertexId == "cv-2" && session.Snapshot().Draft!.Bytes[0] != 0);
        var valid = session.Validate(d1.Id, d1.Generation, default);
        Check("Validate_FixtureAuthority_BindingComplete", valid.Certificate?.Key == valid.Key && valid.Key?.SourceHash == Guard.Sha(session.Snapshot().Draft!.Bytes));
        Check("Validate_Cancelled_NotAssessed", session.Validate(d1.Id, d1.Generation, new CancellationToken(true)).Status == "Not assessed");
        Reject("Apply_ForgedHash_Refused", "DSL-CONFLICT", () => session.Apply(Guard.Id(30), valid with { Key = valid.Key! with { SurfaceHash = new string('0', 64) } }));
        session.UpdateDraft(d1.Id, 1, DecimalSi.Parse("0.014049"));
        Reject("Apply_StaleGeneration_Refused", "DSL-CONFLICT", () => session.Apply(Guard.Id(30), valid));
        valid = session.Validate(d1.Id, 2, default); string applied = session.Apply(Guard.Id(30), valid);
        Check("Apply_Retry_ExactlyOnce", session.Apply(Guard.Id(30), valid) == applied && session.Envelope().Accepted.Length == 2);
        Reject("Apply_OperationPayloadChanged_Refused", "DOC-OPERATION-CONFLICT", () => session.Apply(Guard.Id(30), valid with { Key = valid.Key! with { Generation = 99 } }));
        Check("Undo_RestoresSource", session.Undo(Guard.Id(40)) == initial.AcceptedId && session.Snapshot().Source.SequenceEqual(initial.Source));
        Check("Redo_RestoresRevision", session.Redo(Guard.Id(41)) == applied);
        session.Undo(Guard.Id(42)); byte[] undone = session.SaveImage();
        var reopenedUndo = new Session(authority); reopenedUndo.Reopen(undone);
        Check("Reopen_ApplyRetry_DurableExactlyOnce", reopenedUndo.Apply(Guard.Id(30), valid) == applied && reopenedUndo.Envelope().Accepted.Length == 2);
        Reject("Reopen_OperationKindReuse_Refused", "DOC-OPERATION-CONFLICT", () => reopenedUndo.Undo(Guard.Id(10)));
        Reject("Reopen_ApplyTargetReuse_Refused", "DOC-OPERATION-CONFLICT", () => reopenedUndo.Apply(Guard.Id(30), valid with { Key = valid.Key! with { Rail = "trailing" } }));
        Check("Reopen_RedoStack_Rebuilt", reopenedUndo.Redo(Guard.Id(43)) == applied);
        session.BeginRailEdit(Guard.Id(21), "leading", "cv-2"); session.UpdateDraft(Guard.Id(21), 0, DecimalSi.Parse("0.014049")); session.Apply(Guard.Id(31), session.Validate(Guard.Id(21), 1, default));
        int cursorCount = session.Envelope().Cursors.Length; string branch = session.Snapshot().AcceptedId;
        Check("Apply_AfterUndo_RedoClearedHistoryRetained", session.Redo(Guard.Id(44)) == branch && session.Envelope().Cursors.Length == cursorCount && session.Envelope().Accepted.Length == 3);
        session.BeginRailEdit(Guard.Id(22), "leading", "cv-2"); session.UpdateDraft(Guard.Id(22), 0, 0.014);
        Check("Validate_UnknownGeometry_NotAssessed", session.Validate(Guard.Id(22), 1, default).Status == "Not assessed");
        var outwardRecovery = session.CaptureRecovery(); byte[] recoveryImage = session.SaveImage();
        outwardRecovery.Utf8Base64Chunks[0] = "";
        session.Snapshot().Recovery!.Utf8Base64Chunks[0] = "";
        session.Envelope().Recovery!.Utf8Base64Chunks[0] = "";
        session.Envelope().Sources[0].Utf8Base64Chunks[0] = "";
        Check("RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes", session.SaveImage().SequenceEqual(recoveryImage));
        var reopened = new Session(authority); reopened.Reopen(recoveryImage);
        Check("Recovery_Offer_SeparateAccepted", reopened.Snapshot().Draft is null && reopened.Snapshot().Recovery is not null && reopened.Snapshot().AcceptedId == branch);
        reopened.ResumeRecovery(); Check("Recovery_Resume_OriginalBase", reopened.Snapshot().Draft?.Base == branch); reopened.Cancel(Guard.Id(22));
        Check("Cancel_AcceptedUnchanged", reopened.Snapshot().AcceptedId == branch && reopened.Snapshot().Recovery is null);
        byte[] snapshot = reopened.Snapshot().Source; snapshot[0] = 0;
        Check("Snapshot_CallerMutation_DoesNotChangeSession", reopened.Snapshot().Source[0] != 0);
        session.Cancel(Guard.Id(22)); byte[] image = session.SaveImage(); session.AcknowledgeSaved(image); Check("Save_AcknowledgedImage_Clean", !session.Snapshot().Dirty);
        var env = Native.Read(image); Check("Native_EncodeDecode_ExactSource", Native.Encode(env).SequenceEqual(image));
        byte[] storeInput = image.ToArray(); var storeRequest = new SaveRequest(storeInput, null, Guard.Id(98));
        storeInput[0] = 0; storeRequest.Image[0] = 0;
        Check("SaveRequest_AsyncBoundary_CapturesDefensiveImage", storeRequest.Image.SequenceEqual(image));
        Check("SaveRequest_CreateMode_DerivedFromDiskToken", storeRequest.CreateOnly && !new SaveRequest(image, Guard.Sha(image), Guard.Id(98)).CreateOnly);
        byte[] alternateWhitespace = Guard.Utf8.GetBytes(Guard.Utf8.GetString(image).Replace("  \"format\"", " \"format\""));
        var differentlyFormatted = new Session(authority); differentlyFormatted.Reopen(alternateWhitespace);
        Check("Reopen_DifferentWhitespace_Clean", !differentlyFormatted.Snapshot().Dirty);
        Check("Reopen_OpenRetry_DurableExactlyOnce", differentlyFormatted.Open(raw, Guard.Id(10), true).SequenceEqual(materialized) && differentlyFormatted.Envelope().Accepted.Length == 3);
        Check("Reopen_PersistedUndoRetry_NoCursorMovement", differentlyFormatted.Undo(Guard.Id(40)) == initial.AcceptedId && differentlyFormatted.Snapshot().AcceptedId == branch);
        Reject("Reopen_ApplyDraftReuse_Refused", "DOC-OPERATION-CONFLICT", () => differentlyFormatted.Apply(Guard.Id(30), valid with { Key = valid.Key! with { DraftId = Guard.Id(999) } }));
        var noOp = new Session(authority); noOp.Open(materialized, Guard.Id(10), true); noOp.Undo(Guard.Id(95));
        Reject("NoOp_SameSession_ConflictingActionRefused", "DOC-OPERATION-CONFLICT", () => noOp.Redo(Guard.Id(95)));
        var afterNoOp = new Session(authority); afterNoOp.Reopen(noOp.SaveImage());
        Check("NoOp_Reopen_VolatileIdExpired", afterNoOp.Redo(Guard.Id(95)) == initial.AcceptedId);
        Reject("Reopen_UnknownGeometry_Refused", "DSL-NOT-ASSESSED", () => new Session(new FixtureAuthority([])).Reopen(image));
        Reject("Reopen_UnrelatedCertificate_Refused", "DSL-NOT-ASSESSED", () => new Session(new WrongBindingAuthority()).Reopen(image));
        var incomplete = env with { Recovery = new(Guard.Id(99), env.Cursors[^1].Target, 5, "leading", "cv-2", Session.Chunks(Guard.Utf8.GetBytes("foildsl \"4.0\" foil \"incomplete"))) };
        var invalidRecovery = new Session(authority); invalidRecovery.Reopen(Native.Encode(incomplete)); invalidRecovery.ResumeRecovery();
        Check("Recovery_IncompleteSource_OfferedWithoutAcceptance", invalidRecovery.Validate(Guard.Id(99), 5, default).Status == "Invalid" && invalidRecovery.Snapshot().AcceptedId == branch);
        var limited = new Session(authority, Native.Encode(new SessionSeed(authority, materialized).Envelope).Length + 200);
        limited.Open(materialized, Guard.Id(10), true); byte[] limitedBefore = limited.SaveImage(); limited.AcknowledgeSaved(limitedBefore);
        limited.BeginRailEdit(Guard.Id(90), "leading", "cv-2"); limited.UpdateDraft(Guard.Id(90), 0, DecimalSi.Parse("0.014049"));
        Reject("Growth_ApplyOverflow_RefusedBeforeMutation", "DOC-SIZE", () => limited.Apply(Guard.Id(91), limited.Validate(Guard.Id(90), 1, default)));
        Check("Growth_Refusal_PreservesDraftFactsAndDirty", limited.SaveImage().SequenceEqual(limitedBefore) && limited.Snapshot().Draft?.Generation == 1 && limited.Snapshot().Dirty);
        Reject("Growth_RecoveryOverflow_RefusedBeforeReplacement", "DOC-SIZE", () => limited.CaptureRecovery());
        Check("Growth_RecoveryRefusal_PreservesPriorImage", limited.SaveImage().SequenceEqual(limitedBefore));
        var concurrent = new Session(authority); concurrent.Open(materialized, Guard.Id(10), true); byte[] capturedSave = concurrent.SaveImage();
        concurrent.BeginRailEdit(Guard.Id(92), "leading", "cv-2"); concurrent.UpdateDraft(Guard.Id(92), 0, DecimalSi.Parse("0.014049")); concurrent.Apply(Guard.Id(93), concurrent.Validate(Guard.Id(92), 1, default)); concurrent.AcknowledgeSaved(capturedSave);
        Check("Save_LateAcknowledgement_NewerRevisionRemainsDirty", concurrent.Snapshot().Dirty && Native.Read(capturedSave).Accepted.Length == 1);
        string longId = new('é', 4096);
        byte[] longIds = Guard.Utf8.GetBytes(Guard.Text(materialized).Replace("\"cv-2\"", Jcs.Quote(longId)));
        var longSession = new Session(authority); longSession.Open(longIds, Guard.Id(10), true); byte[] longBefore = longSession.SaveImage();
        Check("Native_LongId_InSourceChunks_Readable", Native.Read(longBefore).Accepted.Length == 1);
        longSession.BeginRailEdit(Guard.Id(96), "leading", longId); longSession.UpdateDraft(Guard.Id(96), 0, DecimalSi.Parse("0.014049"));
        Reject("Native_LongEscapedId_ApplyLineOverflow_PreflightRefused", "DOC-SIZE", () => longSession.Apply(Guard.Id(97), longSession.Validate(Guard.Id(96), 1, default)));
        Reject("Native_LongEscapedId_RecoveryLineOverflow_PreflightRefused", "DOC-SIZE", () => longSession.CaptureRecovery());
        Check("Native_LineOverflow_PreservesHistoryAndDraft", longSession.SaveImage().SequenceEqual(longBefore) && longSession.Snapshot().Draft is not null);
        Check("Native_WriterLines_WithinReaderLimit", Guard.Utf8.GetString(image).Split('\n').All(l => Guard.Utf8.GetByteCount(l) <= 4096));
        Reject("Native_DuplicateKey_Refused", "DOC-SCHEMA", () => Native.Read(Guard.Utf8.GetBytes(Guard.Utf8.GetString(image).Replace("\"format\":", "\"format\":\"cfdw-project-1\",\"format\":"))));
        Reject("Native_UnknownField_ReadOnly", "DOC-UNSUPPORTED-FIELD", () => Native.Read(Guard.Utf8.GetBytes(Guard.Utf8.GetString(image).Replace("\"format\":", "\"future\":1,\"format\":"))));
        Reject("Native_MissingReference_Refused", "DOC-REFERENCE", () => Native.Read(Native.Encode(env with { Accepted = env.Accepted.Select((a, i) => i == 1 ? a with { Parent = Guard.Id(999) } : a).ToArray() })));
        Reject("Native_IllegalRedo_Refused", "DOC-REFERENCE", () => Native.Read(Native.Encode(env with { Cursors = env.Cursors.Select((c, i) => i == 1 ? c with { Reason = "redo" } : c).ToArray() })));
        var rootApply = Native.Encode(env with { Cursors = env.Cursors.Select((c, i) => i == 0 ? c with { Reason = "apply" } : c).ToArray() });
        Reject("Native_FirstApplyInsteadOfOpen_Refused", "DOC-REFERENCE", () => Native.Read(rootApply));
        var refusedReopen = new Session(authority);
        var legacyNative = Native.Encode(env with { Designs = env.Designs.Select(d => d with { Evaluator = "cfdw-cv/1" }).ToArray() });
        Reject("Ruling17_LegacyNativeEvaluator_NoAdoption", "DOC-VERSION", () => refusedReopen.Reopen(legacyNative));
        Reject("Ruling17_LegacyNative_LeavesSessionEmpty", "DOC-EMPTY", () => refusedReopen.Snapshot());
        Reject("Reopen_FirstApplyInsteadOfOpen_Refused", "DOC-REFERENCE", () => refusedReopen.Reopen(rootApply));
        Reject("Reopen_MalformedRoot_NoAdoption", "DOC-EMPTY", () => refusedReopen.Snapshot());
        Reject("Native_SourceTamper_Refused", "DOC-INTEGRITY", () => Native.Read(Native.Encode(env with { Sources = env.Sources.Select((s, i) => i == 0 ? s with { Id = new string('0', 64) } : s).ToArray() })));
        Reject("Native_SizeOverflow_Refused", "DOC-SIZE", () => Native.Read(new byte[Native.MaxBytes + 1]));
        Reject("Native_OperationCollision_Refused", "DOC-REFERENCE", () => Native.Read(Native.Encode(env with { Cursors = env.Cursors.Select((c, i) => i == 1 ? c with { OperationId = env.Cursors[0].OperationId } : c).ToArray() })));
        Reject("Native_NullApplyReceipt_Refused", "DOC-REFERENCE", () => Native.Read(Native.Encode(env with { Accepted = env.Accepted.Select((a, i) => i == 1 ? a with { Edit = null } : a).ToArray() })));
        Reject("Native_ReceiptTargetMissing_Refused", "DOC-REFERENCE", () => Native.Read(Native.Encode(env with { Accepted = env.Accepted.Select((a, i) => i == 1 ? a with { Edit = a.Edit! with { VertexId = "absent" } } : a).ToArray() })));
        var currentEnvelope = session.Envelope();
        var report = new { scope = "contract-fixture-not-production-or-geometry-certification", checks = Checks, elapsedSeconds = clock.Elapsed.TotalSeconds, numbers = numberRows, canonical, canonicalBlake3 = Guard.Hash(unicode), exampleCanonical = Jcs.Write(p.Semantic), exampleSurfaceHash = p.SurfaceHash, exampleSourceBase64 = Convert.ToBase64String(materialized), nativeBase64 = Convert.ToBase64String(Native.Encode(currentEnvelope)), runtime = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier };
        Console.WriteLine(JsonSerializer.Serialize(report));
    }
}

sealed class SessionSeed
{
    public Envelope Envelope { get; }
    public SessionSeed(ICertificateAuthority authority, byte[] bytes) { var s = new Session(authority); s.Open(bytes, Guard.Id(10), true); Envelope = s.Envelope(); }
}
