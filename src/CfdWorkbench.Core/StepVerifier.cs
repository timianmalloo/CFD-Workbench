using System.Globalization;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core;

/// <summary>Independent structural, topological, B-spline where-rule, and zero-PII verification report for STEP AP203 text.</summary>
public sealed record StepVerificationReport(
    bool Passed,
    bool StructureValid,
    bool Ap203SchemaValid,
    bool MillimetreUnitValid,
    int EntityCount,
    int DuplicateIdCount,
    int DanglingRefCount,
    int BSplineSurfaceCount,
    int BSplineCurveCount,
    bool KnotWhereRulesValid,
    bool ClosedShellChecked,
    int FaceCount,
    int EdgeCount,
    int VertexCount,
    int BoundaryEdgeCount,
    int InteriorEdgeCount,
    int NonManifoldEdgeCount,
    int EulerCharacteristic,
    bool EveryEdgePairedOpposite,
    bool AllEdgeLoopsClosed,
    double MaxVertexEdgeEndpointMismatchMm,
    bool ZeroPiiHeaderValid,
    IReadOnlyList<string> Errors);

/// <summary>
/// Pure-C# ISO 10303-21 Part 21 AP203 verifier (ADR-0013, <c>KRN-SPK</c> Phase 1 production upgrade).
/// Explicitly separates boundary edges (degree 1) from interior edges (degree 2, opposite sense <c>+1/-1</c>)
/// on <c>OPEN_SHELL</c>, and requires every edge to have degree 2 with opposite sense on <c>CLOSED_SHELL</c>.
/// </summary>
public static class StepVerifier
{
    private static readonly Regex EntityLineRx = new(@"^#(\d+)\s*=\s*(.+);$", RegexOptions.Compiled);
    private static readonly Regex RefRx = new(@"#(\d+)", RegexOptions.Compiled);
    private static readonly Regex WinUserRx = new(@"(?:[A-Za-z]:|/mnt/[a-z])(?:\\|/)+Users(?:\\|/)+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DefaultHostRx = new(@"(?:DESKTOP|LAPTOP)-[A-Z0-9]{7}|WIN-[A-Z0-9]{11}", RegexOptions.Compiled);
    private static readonly Regex FileNameRx = new(@"FILE_NAME\(\s*'([^']*)'\s*,\s*'([^']*)'\s*,\s*\('([^']*)'\)\s*,\s*\('([^']*)'\)", RegexOptions.Compiled);

    public static StepVerificationReport Verify(string stepText, StepVariant expectedVariant, IReadOnlyList<string>? extraForbiddenTokens = null)
    {
        ArgumentNullException.ThrowIfNull(stepText);
        var errors = new List<string>();
        string[] lines = stepText.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        bool hasStart = lines.Length > 0 && lines[0] == "ISO-10303-21;";
        bool hasEnd = lines.Length > 1 && lines.Last(l => l.Length > 0) == "END-ISO-10303-21;";
        int headerIdx = Array.IndexOf(lines, "HEADER;");
        int dataIdx = Array.IndexOf(lines, "DATA;");
        bool structureValid = hasStart && hasEnd && headerIdx > 0 && dataIdx > headerIdx;
        if (!structureValid) errors.Add("Invalid Part 21 section framing (ISO-10303-21 / HEADER / DATA / END-ISO-10303-21).");

        bool schemaValid = stepText.Contains("FILE_SCHEMA(('CONFIG_CONTROL_DESIGN'));", StringComparison.Ordinal);
        if (!schemaValid) errors.Add("Missing AP203 FILE_SCHEMA(('CONFIG_CONTROL_DESIGN')).");

        bool mmValid = stepText.Contains("SI_UNIT(.MILLI., .METRE.)", StringComparison.Ordinal);
        if (!mmValid) errors.Add("Missing millimetre SI_UNIT(.MILLI., .METRE.).");

        bool zeroPiiValid = CheckZeroPiiHeader(stepText, extraForbiddenTokens, errors);

        var entities = new Dictionary<int, string>();
        int duplicateIds = 0;
        if (dataIdx >= 0)
        {
            for (int i = dataIdx + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "ENDSEC;") break;
                if (line.Length == 0) continue;
                var m = EntityLineRx.Match(line);
                if (!m.Success)
                {
                    errors.Add($"Malformed DATA entity line {i + 1}: {line[..Math.Min(60, line.Length)]}");
                    continue;
                }
                int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!entities.TryAdd(id, m.Groups[2].Value))
                    duplicateIds++;
            }
        }
        if (duplicateIds > 0) errors.Add($"Duplicate STEP entity IDs: {duplicateIds}.");

        int danglingRefs = 0;
        foreach (var (id, body) in entities)
        {
            foreach (Match rm in RefRx.Matches(body))
            {
                int target = int.Parse(rm.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!entities.ContainsKey(target))
                {
                    danglingRefs++;
                    if (danglingRefs <= 5) errors.Add($"Dangling reference #{target} inside #{id}.");
                }
            }
        }

        var points = new Dictionary<int, CadVec3>();
        var vertices = new Dictionary<int, int>();
        var edgeCurves = new Dictionary<int, (int VStart, int VEnd, int GeomId)>();
        var orientedEdges = new Dictionary<int, (int EdgeId, bool Forward)>();
        var edgeLoops = new Dictionary<int, int[]>();
        var faceBounds = new Dictionary<int, int>();
        var faces = new Dictionary<int, int[]>();
        var bsplineCurves = new Dictionary<int, (int Degree, int[] PoleIds, int[] Mults, double[] Knots)>();

        int bsplineSurfaceCount = 0;
        bool knotRulesValid = true;

        foreach (var (id, body) in entities)
        {
            if (body.StartsWith("CARTESIAN_POINT(", StringComparison.Ordinal))
            {
                var nums = ParseTupleDoubles(body);
                if (nums.Length == 3) points[id] = new(nums[0], nums[1], nums[2]);
            }
            else if (body.StartsWith("VERTEX_POINT(", StringComparison.Ordinal))
            {
                var refs = ExtractRefs(body);
                if (refs.Length == 1) vertices[id] = refs[0];
            }
            else if (body.StartsWith("EDGE_CURVE(", StringComparison.Ordinal))
            {
                var refs = ExtractRefs(body);
                if (refs.Length == 3) edgeCurves[id] = (refs[0], refs[1], refs[2]);
            }
            else if (body.StartsWith("ORIENTED_EDGE(", StringComparison.Ordinal))
            {
                var refs = ExtractRefs(body);
                bool forward = body.EndsWith(".T.)", StringComparison.Ordinal);
                if (refs.Length == 1) orientedEdges[id] = (refs[0], forward);
            }
            else if (body.StartsWith("EDGE_LOOP(", StringComparison.Ordinal))
            {
                edgeLoops[id] = ExtractRefs(body);
            }
            else if (body.StartsWith("FACE_OUTER_BOUND(", StringComparison.Ordinal))
            {
                var refs = ExtractRefs(body);
                if (refs.Length == 1) faceBounds[id] = refs[0];
            }
            else if (body.StartsWith("ADVANCED_FACE(", StringComparison.Ordinal))
            {
                var refs = ExtractRefs(body);
                if (refs.Length >= 2) faces[id] = refs[..^1];
            }
            else if (body.StartsWith("B_SPLINE_CURVE_WITH_KNOTS(", StringComparison.Ordinal))
            {
                if (!ValidateBSplineCurve(id, body, out var parsed, errors))
                    knotRulesValid = false;
                else
                    bsplineCurves[id] = parsed;
            }
            else if (body.StartsWith("B_SPLINE_SURFACE_WITH_KNOTS(", StringComparison.Ordinal))
            {
                bsplineSurfaceCount++;
                if (!ValidateBSplineSurface(id, body, errors))
                    knotRulesValid = false;
            }
        }

        double maxEndpointMismatch = 0.0;
        foreach (var (_, ec) in edgeCurves)
        {
            if (!vertices.TryGetValue(ec.VStart, out int psId) || !vertices.TryGetValue(ec.VEnd, out int peId)) continue;
            if (!points.TryGetValue(psId, out var pStart) || !points.TryGetValue(peId, out var pEnd)) continue;

            if (bsplineCurves.TryGetValue(ec.GeomId, out var crv) && crv.PoleIds.Length >= 2)
            {
                if (points.TryGetValue(crv.PoleIds[0], out var cStart) && points.TryGetValue(crv.PoleIds[^1], out var cEnd))
                {
                    maxEndpointMismatch = Math.Max(maxEndpointMismatch, (pStart - cStart).Length());
                    maxEndpointMismatch = Math.Max(maxEndpointMismatch, (pEnd - cEnd).Length());
                }
            }
        }
        if (maxEndpointMismatch > 1e-6)
            errors.Add($"Vertex-to-edge endpoint mismatch {maxEndpointMismatch:E3} mm exceeds 1 nm.");

        bool closedShellChecked = false;
        int shellFaceCount = 0;
        int shellEdgeCount = 0;
        int shellVertexCount = 0;
        int boundaryEdgeCount = 0;
        int interiorEdgeCount = 0;
        int nonManifoldEdgeCount = 0;
        int euler = 0;
        bool everyEdgePaired = true;
        bool allLoopsClosed = true;

        string shellTag = expectedVariant == StepVariant.ClosedShell ? "CLOSED_SHELL(" : "OPEN_SHELL(";
        var shellEntry = entities.FirstOrDefault(kv => kv.Value.StartsWith(shellTag, StringComparison.Ordinal));
        if (shellEntry.Key == 0)
        {
            errors.Add($"Missing expected {shellTag} entity.");
        }
        else
        {
            int[] shellFaces = ExtractRefs(shellEntry.Value);
            shellFaceCount = shellFaces.Length;
            var usedVertices = new HashSet<int>();
            var edgeUses = new Dictionary<int, (int Pos, int Neg)>();

            foreach (int fId in shellFaces)
            {
                if (!faces.TryGetValue(fId, out int[]? bIds)) { errors.Add($"Missing face #{fId}."); continue; }
                foreach (int bId in bIds)
                {
                    if (!faceBounds.TryGetValue(bId, out int loopId) || !edgeLoops.TryGetValue(loopId, out int[]? oeIds))
                    {
                        errors.Add($"Missing loop for bound #{bId}.");
                        allLoopsClosed = false;
                        continue;
                    }
                    if (oeIds.Length < 2)
                    {
                        errors.Add($"Degenerate loop #{loopId} with {oeIds.Length} edges.");
                        allLoopsClosed = false;
                        continue;
                    }
                    for (int i = 0; i < oeIds.Length; i++)
                    {
                        var oeA = orientedEdges[oeIds[i]];
                        var oeB = orientedEdges[oeIds[(i + 1) % oeIds.Length]];
                        var ecA = edgeCurves[oeA.EdgeId];
                        var ecB = edgeCurves[oeB.EdgeId];
                        int endA = oeA.Forward ? ecA.VEnd : ecA.VStart;
                        int startB = oeB.Forward ? ecB.VStart : ecB.VEnd;
                        usedVertices.Add(ecA.VStart);
                        usedVertices.Add(ecA.VEnd);
                        if (endA != startB)
                        {
                            allLoopsClosed = false;
                            errors.Add($"Open loop #{loopId} between edge #{oeA.EdgeId} and #{oeB.EdgeId}.");
                        }
                        var cur = edgeUses.GetValueOrDefault(oeA.EdgeId);
                        edgeUses[oeA.EdgeId] = oeA.Forward ? (cur.Pos + 1, cur.Neg) : (cur.Pos, cur.Neg + 1);
                    }
                }
            }

            shellEdgeCount = edgeUses.Count;
            shellVertexCount = usedVertices.Count;
            euler = shellVertexCount - shellEdgeCount + shellFaceCount;

            if (expectedVariant == StepVariant.ClosedShell)
            {
                closedShellChecked = true;
                foreach (var (eId, uses) in edgeUses)
                {
                    int degree = uses.Pos + uses.Neg;
                    if (uses.Pos == 1 && uses.Neg == 1)
                    {
                        interiorEdgeCount++;
                    }
                    else
                    {
                        if (degree == 1) boundaryEdgeCount++;
                        nonManifoldEdgeCount++;
                        everyEdgePaired = false;
                        errors.Add($"Non-manifold edge #{eId} in CLOSED_SHELL: (+{uses.Pos}, -{uses.Neg}).");
                    }
                }
                if (euler != 2)
                    errors.Add($"CLOSED_SHELL Euler characteristic V-E+F = {euler}, expected 2 (V={shellVertexCount}, E={shellEdgeCount}, F={shellFaceCount}).");
                if (!stepText.Contains("MANIFOLD_SOLID_BREP(", StringComparison.Ordinal))
                    errors.Add("Missing MANIFOLD_SOLID_BREP for closed-shell variant.");
            }
            else
            {
                // KRN-SPK Phase 1 fix: explicitly separate boundary edges (degree 1) vs interior edges (degree 2, opposite sense).
                foreach (var (eId, uses) in edgeUses)
                {
                    int degree = uses.Pos + uses.Neg;
                    if (degree == 1)
                    {
                        boundaryEdgeCount++;
                    }
                    else if (uses.Pos == 1 && uses.Neg == 1)
                    {
                        interiorEdgeCount++;
                    }
                    else
                    {
                        nonManifoldEdgeCount++;
                        everyEdgePaired = false;
                        errors.Add($"Non-manifold or misoriented interior edge #{eId} in OPEN_SHELL: (+{uses.Pos}, -{uses.Neg}).");
                    }
                }
                if (boundaryEdgeCount == 0)
                    errors.Add("OPEN_SHELL contains zero boundary edges (degree 1).");
                if (interiorEdgeCount == 0)
                    errors.Add("OPEN_SHELL contains zero interior shared edges (degree 2).");
                if (!stepText.Contains("SHELL_BASED_SURFACE_MODEL(", StringComparison.Ordinal))
                    errors.Add("Missing SHELL_BASED_SURFACE_MODEL for untrimmed variant.");
            }
        }

        bool passed = errors.Count == 0 && structureValid && schemaValid && mmValid && zeroPiiValid
            && duplicateIds == 0 && danglingRefs == 0 && knotRulesValid && allLoopsClosed
            && nonManifoldEdgeCount == 0
            && (expectedVariant != StepVariant.ClosedShell || (everyEdgePaired && boundaryEdgeCount == 0 && euler == 2))
            && (expectedVariant != StepVariant.Untrimmed || (everyEdgePaired && boundaryEdgeCount > 0 && interiorEdgeCount > 0));

        return new StepVerificationReport(
            passed,
            structureValid,
            schemaValid,
            mmValid,
            entities.Count,
            duplicateIds,
            danglingRefs,
            bsplineSurfaceCount,
            bsplineCurves.Count,
            knotRulesValid,
            closedShellChecked,
            shellFaceCount,
            shellEdgeCount,
            shellVertexCount,
            boundaryEdgeCount,
            interiorEdgeCount,
            nonManifoldEdgeCount,
            euler,
            everyEdgePaired,
            allLoopsClosed,
            maxEndpointMismatch,
            zeroPiiValid,
            errors);
    }

    private static bool CheckZeroPiiHeader(string text, IReadOnlyList<string>? extraForbidden, List<string> errors)
    {
        bool ok = true;
        var fn = FileNameRx.Match(text);
        if (!fn.Success)
        {
            errors.Add("Zero-PII check failed: FILE_NAME header missing or unparseable.");
            return false;
        }
        string ts = fn.Groups[2].Value;
        string author = fn.Groups[3].Value;
        string org = fn.Groups[4].Value;
        if (ts != StepAp203Export.FixedZeroPiiTimestamp)
        {
            errors.Add($"Zero-PII check failed: FILE_NAME timestamp '{ts}' != '{StepAp203Export.FixedZeroPiiTimestamp}'.");
            ok = false;
        }
        if (author.Length != 0)
        {
            errors.Add($"Zero-PII check failed: FILE_NAME author is non-empty ('{author}').");
            ok = false;
        }
        if (org.Length != 0)
        {
            errors.Add($"Zero-PII check failed: FILE_NAME organization is non-empty ('{org}').");
            ok = false;
        }
        if (WinUserRx.IsMatch(text))
        {
            errors.Add("Zero-PII check failed: Windows user-profile path detected in STEP bytes.");
            ok = false;
        }
        if (DefaultHostRx.IsMatch(text))
        {
            errors.Add("Zero-PII check failed: default Windows hostname token detected in STEP bytes.");
            ok = false;
        }
        string user = Environment.UserName;
        if (!string.IsNullOrEmpty(user) && user.Length >= 3 && text.Contains(user, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Zero-PII check failed: current OS username detected in STEP bytes.");
            ok = false;
        }
        string host = Environment.MachineName;
        if (!string.IsNullOrEmpty(host) && host.Length >= 4 && text.Contains(host, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Zero-PII check failed: current machine hostname detected in STEP bytes.");
            ok = false;
        }
        if (extraForbidden is not null)
        {
            foreach (string token in extraForbidden)
            {
                if (!string.IsNullOrEmpty(token) && text.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Zero-PII check failed: forbidden marker detected in STEP bytes.");
                    ok = false;
                }
            }
        }
        return ok;
    }

    private static bool ValidateBSplineCurve(int id, string body, out (int Degree, int[] PoleIds, int[] Mults, double[] Knots) parsed, List<string> errors)
    {
        parsed = default;
        var args = SplitTopLevelArgs(body[(body.IndexOf('(') + 1)..^1]);
        if (args.Count < 9) { errors.Add($"Curve #{id} has fewer than 9 attributes."); return false; }
        int deg = int.Parse(args[1].Trim(), CultureInfo.InvariantCulture);
        int[] poles = ExtractRefs(args[2]);
        int[] mults = ParseIntList(args[6]);
        double[] knots = ParseDoubleList(args[7]);
        parsed = (deg, poles, mults, knots);
        return CheckKnotVector($"Curve #{id}", deg, poles.Length, mults, knots, errors);
    }

    private static bool ValidateBSplineSurface(int id, string body, List<string> errors)
    {
        var args = SplitTopLevelArgs(body[(body.IndexOf('(') + 1)..^1]);
        if (args.Count < 13) { errors.Add($"Surface #{id} has fewer than 13 attributes."); return false; }
        int degU = int.Parse(args[1].Trim(), CultureInfo.InvariantCulture);
        int degV = int.Parse(args[2].Trim(), CultureInfo.InvariantCulture);
        var gridRows = SplitTopLevelArgs(args[3].Trim()[1..^1]);
        int nu = gridRows.Count;
        int nv = nu > 0 ? ExtractRefs(gridRows[0]).Length : 0;
        int[] um = ParseIntList(args[8]);
        int[] vm = ParseIntList(args[9]);
        double[] uk = ParseDoubleList(args[10]);
        double[] vk = ParseDoubleList(args[11]);

        bool okU = CheckKnotVector($"Surface #{id} (u)", degU, nu, um, uk, errors);
        bool okV = CheckKnotVector($"Surface #{id} (v)", degV, nv, vm, vk, errors);
        return okU && okV;
    }

    private static bool CheckKnotVector(string label, int degree, int nPoles, int[] mults, double[] knots, List<string> errors)
    {
        if (mults.Length != knots.Length || mults.Length < 2)
        {
            errors.Add($"{label}: knot/multiplicity length mismatch ({knots.Length} vs {mults.Length}).");
            return false;
        }
        int sumMult = mults.Sum();
        int expected = nPoles + degree + 1;
        if (sumMult != expected)
        {
            errors.Add($"{label}: sum(mult)={sumMult} != n+p+1={expected} (n={nPoles}, p={degree}).");
            return false;
        }
        if (mults[0] != degree + 1 || mults[^1] != degree + 1)
        {
            errors.Add($"{label}: end multiplicities ({mults[0]}, {mults[^1]}) != p+1 ({degree + 1}).");
            return false;
        }
        for (int i = 1; i < knots.Length; i++)
        {
            if (knots[i] <= knots[i - 1])
            {
                errors.Add($"{label}: knots not strictly increasing at index {i}.");
                return false;
            }
        }
        return true;
    }

    private static int[] ExtractRefs(string text) =>
        RefRx.Matches(text).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();

    private static double[] ParseTupleDoubles(string body)
    {
        int start = body.LastIndexOf('(');
        int end = body.IndexOf(')', start + 1);
        if (start < 0 || end < 0) return [];
        return body[(start + 1)..end].Split(',').Select(s => double.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();
    }

    private static int[] ParseIntList(string tuple) =>
        tuple.Trim()[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();

    private static double[] ParseDoubleList(string tuple) =>
        tuple.Trim()[1..^1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => double.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();

    private static List<string> SplitTopLevelArgs(string s)
    {
        var result = new List<string>();
        int depth = 0;
        bool inQuote = false;
        int start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\'') inQuote = !inQuote;
            else if (!inQuote)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(s[start..i]);
                    start = i + 1;
                }
            }
        }
        if (start <= s.Length) result.Add(s[start..]);
        return result;
    }
}
