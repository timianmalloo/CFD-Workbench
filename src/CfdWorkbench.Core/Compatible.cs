using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core;

/// <summary>
/// Evaluator identifier rule (Ruling 73 DR-XPA-2, Ruling 269 BC-5): <c>cfdw-cv/3</c> when a foil's station
/// sections do not all share one spacing, else <c>cfdw-cv/2</c>. Single site for all evaluator id literals.
/// </summary>
public static class Evaluator
{
    public const string V2 = "cfdw-cv/2";
    public const string V3 = "cfdw-cv/3";

    internal static bool IsKnown(string? id) => id is V2 or V3;

    internal static bool NeedsCompatibleFit(Definition? definition)
    {
        if (definition is null || definition.Kind != "foil" || definition.Assignments.Length < 2) return false;
        // Independent upper vs lower abscissa within a single profile stays Unsupported (SharedAbscissa), not CF.
        for (int index = 0; index < definition.Assignments.Length; index++)
        {
            var profile = definition.Profiles[definition.Assignments[index].Profile];
            if (!Placement.SameAbscissa(profile.Upper, profile.Lower))
                return false;
        }
        var first = definition.Profiles[definition.Assignments[0].Profile];
        for (int index = 1; index < definition.Assignments.Length; index++)
        {
            var other = definition.Profiles[definition.Assignments[index].Profile];
            if (ReferenceEquals(first, other)) continue;
            if (!Placement.SameAbscissa(first.Upper, other.Upper))
                return true;
        }
        return false;
    }

    internal static string IdFor(Definition? definition) => NeedsCompatibleFit(definition) ? V3 : V2;

    internal static string IdFor(SourceParse? parsed) => IdFor(parsed?.Definition);

    /// <summary>Returns the evaluator identifier for parsed source bytes.</summary>
    public static string IdFor(byte[] source) => IdFor(FoilSource.Parse(source));
}

internal sealed record CompatibleDefinition(
    Definition Definition,
    IReadOnlyList<BlendMatch> Matches,
    string BasisProfile,
    int SharedSpans,
    bool NeedsFit,
    bool Failed,
    string? FailureReason,
    bool HasOtherAnchors);

/// <summary>
/// Derived, never-stored compatible fit stage for mixed-spacing wings (Ruling 73, Ruling 269;
/// <c>docs/design/compatible-fit.md</c> and <c>docs/design/cross-profile-abscissa.md</c>).
/// Versioned under <c>cfdw-cv/3</c>:
/// <list type="bullet">
///   <item>Basis: richest station section (most Bezier spans, then most vertices, then lowest station index).</item>
///   <item>Shared spacing S*: basis spacing plus exact Boehm insertion (skip-if-present) to multiplicity 5 of every other station section's interior anchors located on the basis x(t).</item>
///   <item>Copies: identity when spacing matches S* bitwise; Boehm refinement adopting S* x bitwise when knots nest and x(t) agrees within 1e-12; otherwise <see cref="DatImport.FitToBasis"/> least-squares on 161 cosine-clustered samples (<c>double.CosPi</c>) with KKT tangent rows at coinciding anchors.</item>
///   <item>Surface residual: <see cref="StationEdits.CompareMeasures"/> between authored and compatible <see cref="Placement.SectionsForMeasure"/> on the versioned production grid (<c>129 eta x 2001 x</c>) across all intervals touching any non-identity copy; identity copies report 0.</item>
/// </list>
/// </summary>
public static class Compatible
{
    /// <summary>Admission limit on the placed surface (Ruling 61 A4.6, DR-XPA-4): 10 um.</summary>
    public const double LimitMeters = 10e-6;

    /// <summary>Versioned fit sample count under cfdw-cv/3 (BC-3 / CG-3): 161 cosine-spaced points.</summary>
    internal const int FitSampleCount = 161;

    /// <summary>Versioned surface residual grid under cfdw-cv/3 (BC-3 / CG-2): 129 eta per interval x 2001 x.</summary>
    internal static readonly StationGrid ResidualGrid = StationGrid.Production;

    private const double AnchorSnapTolerance = 1e-12;
    private const double RefinementAbscissaTolerance = 1e-12;
    private const double AnchorCoincideTolerance = 1e-9;

    // Session-memory memos only (never stored; XPA-I1). Bounded so long sessions never accumulate unbounded entries.
    private const int CopyMemoLimit = 512;
    private const int DefinitionMemoLimit = 64;
    private const int MeasureMemoLimit = 64;

    private sealed record CopyMemoEntry(string Kind, Curve Upper, Curve Lower, double RecordDeviation, bool Failed, string? FailureReason);

    private static readonly ConcurrentDictionary<string, CopyMemoEntry> CopyMemo = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, CompatibleDefinition> PrepMemo = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, CompatibleDefinition> MeasureMemo = new(StringComparer.Ordinal);

    internal static void ClearMemos()
    {
        CopyMemo.Clear();
        PrepMemo.Clear();
        MeasureMemo.Clear();
    }

    /// <summary>Returns the derived compatible-fit match report for a foil source, measured on the production residual grid.</summary>
    public static IReadOnlyList<BlendMatch> Matches(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" }) return [];
        return Measure(parsed).Matches;
    }

    /// <summary>
    /// Returns the basis profile name, whether S* carries anchors from another station section, and the derived
    /// matches for UI readouts (COPY-C1..C6). Never writes or stores anything.
    /// </summary>
    public static (string BasisProfile, bool HasOtherAnchors, IReadOnlyList<BlendMatch> Matches) Summary(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.Definition is not { Kind: "foil" }) return ("", false, []);
        var result = Measure(parsed);
        return (result.BasisProfile, result.HasOtherAnchors, result.Matches);
    }

    internal static CompatibleDefinition Of(Definition definition, string? surfaceKey = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Kind != "foil" || definition.Assignments.Length == 0)
            return new(definition, [], "", 0, false, false, null, false);

        if (!Evaluator.NeedsCompatibleFit(definition))
            return IdentityResult(definition);

        string key = DefinitionMemoKey(definition, surfaceKey);
        if (PrepMemo.TryGetValue(key, out var cached))
            return cached;

        var built = BuildCompatible(definition);
        if (PrepMemo.Count >= DefinitionMemoLimit) PrepMemo.Clear();
        PrepMemo[key] = built;
        return built;
    }

    internal static CompatibleDefinition Measure(SourceParse parsed, StationGrid? gridOverride = null)
    {
        ArgumentNullException.ThrowIfNull(parsed);
        if (parsed.Definition is null) throw new ContractError("DSL-INVALID");
        return Measure(parsed.Definition, gridOverride, parsed.SurfaceHash);
    }

    internal static CompatibleDefinition Measure(Definition definition, StationGrid? gridOverride = null, string? surfaceKey = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var prep = Of(definition, surfaceKey);
        if (!prep.NeedsFit || prep.Failed) return prep;

        var grid = gridOverride ?? ResidualGrid;
        string defKey = DefinitionMemoKey(definition, surfaceKey);
        string key = string.Create(CultureInfo.InvariantCulture, $"{defKey}|{grid.EtaSamples}x{grid.XSamples}");
        if (MeasureMemo.TryGetValue(key, out var cached))
            return cached;

        var measured = MeasureSurfaceResiduals(definition, prep, grid);
        if (MeasureMemo.Count >= MeasureMemoLimit) MeasureMemo.Clear();
        MeasureMemo[key] = measured;
        return measured;
    }

    private static CompatibleDefinition IdentityResult(Definition definition)
    {
        var stations = CollectStationProfiles(definition);
        int basisProfile = stations[0].ProfileIndex;
        int spans = SpanCount(definition.Profiles[basisProfile].Upper.Knots);
        var matches = new BlendMatch[stations.Count];
        for (int i = 0; i < stations.Count; i++)
        {
            var entry = stations[i];
            matches[i] = new BlendMatch(
                definition.Profiles[entry.ProfileIndex].Name,
                entry.Assignments,
                entry.ProfileIndex == basisProfile,
                "identity",
                0d,
                0d,
                0d,
                LimitMeters,
                spans);
        }
        return new(definition, matches, definition.Profiles[basisProfile].Name, spans, false, false, null, false);
    }

    private static CompatibleDefinition BuildCompatible(Definition definition)
    {
        var stations = CollectStationProfiles(definition);
        int basisSlot = SelectBasis(definition, stations);
        var basisEntry = stations[basisSlot];
        var basisProfile = definition.Profiles[basisEntry.ProfileIndex];

        if (!Placement.SameAbscissa(basisProfile.Upper, basisProfile.Lower))
            return FailedResult(definition, basisProfile.Name, "Basis profile '" + basisProfile.Name + "' has independent upper and lower abscissae.");

        int degree = basisProfile.Upper.Degree;
        double[] sKnots = basisProfile.Upper.Knots.ToArray();
        double[][] sUpper = CopyPoints(basisProfile.Upper.Points);
        double[][] sLower = CopyPoints(basisProfile.Lower.Points);

        // Step 2: collect interior anchors of every other station section, mapped onto the basis x(t).
        var anchorParameters = new List<double>();
        for (int slot = 0; slot < stations.Count; slot++)
        {
            if (slot == basisSlot) continue;
            var other = definition.Profiles[stations[slot].ProfileIndex];
            var curve = other.Upper;
            int p = curve.Degree;
            for (int idx = p; idx <= curve.Points.Length - p - 1; idx++)
            {
                if (!FoilSource.IsAnchor(curve.Knots, curve.Points.Length, p, idx)) continue;
                double xa = curve.Points[idx][0];
                if (!double.IsFinite(xa) || xa <= 0 || xa >= 1) continue;
                double u = ProfileEvaluator.ParameterFor(basisProfile.Upper, xa);
                if (double.IsFinite(u) && u > 0 && u < 1) anchorParameters.Add(u);
            }
        }
        anchorParameters.Sort();

        bool hasOtherAnchors = false;
        foreach (double rawU in anchorParameters)
        {
            double u = SnapKnot(sKnots, rawU);
            if (u <= 0 || u >= 1) continue;
            int mult = CountKnot(sKnots, u);
            // BC-3 (CG-4): skip if present at multiplicity >= degree (protects XPA-I3).
            if (mult >= degree) continue;
            int additions = degree - mult;
            for (int turn = 0; turn < additions; turn++)
            {
                var uIns = FoilSource.InsertOnce(sKnots, sUpper, degree, u);
                var lIns = FoilSource.InsertOnce(sKnots, sLower, degree, u);
                sKnots = uIns.Knots;
                sUpper = uIns.Points;
                sLower = lIns.Points;
                for (int i = 0; i < sUpper.Length; i++) sLower[i][0] = sUpper[i][0];
            }
            hasOtherAnchors = true;
        }

        double[] sX = new double[sUpper.Length];
        for (int i = 0; i < sUpper.Length; i++)
        {
            sX[i] = sUpper[i][0];
            if (!double.IsFinite(sX[i]) || (i > 0 && sX[i] < sX[i - 1]))
                return FailedResult(definition, basisProfile.Name, "Shared spacing abscissae are not finite or ordered.");
        }
        int sharedSpans = SpanCount(sKnots);
        string[] sIds = MakeIds(sX.Length);
        string sStarKey = BasisKey(degree, sKnots, sX);

        // Step 3: construct compatible copy for every station profile.
        var compatProfiles = (ProfileDefinition[])definition.Profiles.Clone();
        // BC-2: update BOTH definition.Profiles and matching definition.Curves entries.
        var compatCurves = new Dictionary<string, Curve>(definition.Curves, StringComparer.Ordinal);
        var matches = new BlendMatch[stations.Count];

        for (int slot = 0; slot < stations.Count; slot++)
        {
            var entry = stations[slot];
            var authored = definition.Profiles[entry.ProfileIndex];
            bool isBasis = slot == basisSlot;

            if (isBasis)
            {
                if (!hasOtherAnchors)
                {
                    matches[slot] = new BlendMatch(authored.Name, entry.Assignments, true, "identity", 0d, 0d, 0d, LimitMeters, sharedSpans);
                    continue;
                }
                if (!AllFinite(sUpper) || !AllFinite(sLower))
                    return FailedResult(definition, basisProfile.Name, "Refined basis profile '" + authored.Name + "' is not finite.");
                var upperCopy = MakeDerivedCurve(authored.Upper, degree, sKnots, sUpper, sIds);
                var lowerCopy = MakeDerivedCurve(authored.Lower, degree, sKnots, sLower, sIds);
                var copyProfile = authored with { Upper = upperCopy, Lower = lowerCopy };
                double recDev = FoilSource.MaxOrdinateDeviation(authored, copyProfile);
                if (!double.IsFinite(recDev))
                    return FailedResult(definition, basisProfile.Name, "Record deviation for '" + authored.Name + "' is not finite.");
                compatProfiles[entry.ProfileIndex] = copyProfile;
                compatCurves[upperCopy.Path] = upperCopy;
                compatCurves[lowerCopy.Path] = lowerCopy;
                matches[slot] = new BlendMatch(authored.Name, entry.Assignments, true, "refined", recDev, 0d, 0d, LimitMeters, sharedSpans);
                continue;
            }

            if (SameSpacing(authored.Upper, degree, sKnots, sX) && SameSpacing(authored.Lower, degree, sKnots, sX))
            {
                matches[slot] = new BlendMatch(authored.Name, entry.Assignments, false, "identity", 0d, 0d, 0d, LimitMeters, sharedSpans);
                continue;
            }

            var memoEntry = CopyFor(authored, degree, sKnots, sX, sIds, sStarKey);
            if (memoEntry.Failed)
                return FailedResult(definition, basisProfile.Name, memoEntry.FailureReason ?? "Compatible fit failed for profile '" + authored.Name + "'.");

            var derivedUpper = memoEntry.Upper with { Path = authored.Upper.Path };
            var derivedLower = memoEntry.Lower with { Path = authored.Lower.Path };
            var derivedProfile = authored with { Upper = derivedUpper, Lower = derivedLower };
            compatProfiles[entry.ProfileIndex] = derivedProfile;
            compatCurves[derivedUpper.Path] = derivedUpper;
            compatCurves[derivedLower.Path] = derivedLower;
            matches[slot] = new BlendMatch(authored.Name, entry.Assignments, false, memoEntry.Kind, memoEntry.RecordDeviation, 0d, 0d, LimitMeters, sharedSpans);
        }

        var compatDef = definition with { Profiles = compatProfiles, Curves = compatCurves };
        return new(compatDef, matches, basisProfile.Name, sharedSpans, true, false, null, hasOtherAnchors);
    }

    private static CopyMemoEntry CopyFor(ProfileDefinition authored, int degree, double[] sKnots, double[] sX, string[] sIds, string sStarKey)
    {
        string profileKey = Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(authored.Semantic)));
        string memoKey = sStarKey + "|" + profileKey + "|" + authored.Closure + "|" + TangentsKey(authored) + "|" + Evaluator.V3;
        if (CopyMemo.TryGetValue(memoKey, out var cached))
            return cached;

        var computed = ComputeCopy(authored, degree, sKnots, sX, sIds);
        if (CopyMemo.Count >= CopyMemoLimit) CopyMemo.Clear();
        CopyMemo[memoKey] = computed;
        return computed;
    }

    private static CopyMemoEntry ComputeCopy(ProfileDefinition authored, int degree, double[] sKnots, double[] sX, string[] sIds)
    {
        // Branch 1: exact Boehm refinement when knots nest and x(t) matches S* within 1e-12; adopt S* x bitwise.
        if (TryBoehmRefine(authored, degree, sKnots, sX, sIds, out var refinedUpper, out var refinedLower))
        {
            var refinedProfile = authored with { Upper = refinedUpper!, Lower = refinedLower! };
            double dev = FoilSource.MaxOrdinateDeviation(authored, refinedProfile);
            if (double.IsFinite(dev))
                return new("refined", refinedUpper!, refinedLower!, dev, false, null);
        }

        // Branch 2: DatImport.FitToBasis least-squares on versioned 161 cosine samples with KKT tangent rows at coinciding anchors.
        var upperSamples = new List<ProfilePoint>(FitSampleCount);
        var lowerSamples = new List<ProfilePoint>(FitSampleCount);
        double step = FitSampleCount - 1;
        for (int i = 0; i < FitSampleCount; i++)
        {
            double x = i == 0 ? 0d : i == FitSampleCount - 1 ? 1d : 0.5 * (1d - double.CosPi(i / step));
            double yu = i == 0 ? 0d : i == FitSampleCount - 1 ? authored.Upper.Points[^1][1] : ProfileEvaluator.OrdinateAt(authored.Upper, x);
            double yl = i == 0 ? 0d : i == FitSampleCount - 1 ? authored.Lower.Points[^1][1] : ProfileEvaluator.OrdinateAt(authored.Lower, x);
            if (!double.IsFinite(yu) || !double.IsFinite(yl))
                return new("fitted", authored.Upper, authored.Lower, double.PositiveInfinity, true,
                    "Compatible fit for profile '" + authored.Name + "' produced a non-finite sample.");
            upperSamples.Add(new ProfilePoint(x, yu));
            lowerSamples.Add(new ProfilePoint(x, yl));
        }

        var dat = new DatProfile(authored.Name, "compatible-fit", upperSamples, lowerSamples, Array.Empty<byte>(), 2 * FitSampleCount - 1);
        var mappedUpperRows = MapTangentRows(authored.Upper, sKnots, sX, degree, sIds);
        var mappedLowerRows = MapTangentRows(authored.Lower, sKnots, sX, degree, sIds);

        BasisFit? fit;
        try
        {
            fit = DatImport.FitToBasis(dat, sKnots, sX, degree, authored.Closure == "closed", mappedUpperRows, sIds, mappedLowerRows, sIds);
        }
        catch (ContractError error)
        {
            return new("fitted", authored.Upper, authored.Lower, double.PositiveInfinity, true,
                "Compatible fit for profile '" + authored.Name + "' failed: " + (error.Reason ?? error.Code));
        }

        if (fit is null || fit.Upper.Any(v => !double.IsFinite(v)) || fit.Lower.Any(v => !double.IsFinite(v)))
            return new("fitted", authored.Upper, authored.Lower, double.PositiveInfinity, true,
                "Compatible fit for profile '" + authored.Name + "' is singular or not finite.");

        var upperPts = new double[sX.Length][];
        var lowerPts = new double[sX.Length][];
        for (int i = 0; i < sX.Length; i++)
        {
            upperPts[i] = [sX[i], fit.Upper[i]];
            lowerPts[i] = [sX[i], fit.Lower[i]];
        }
        var upperCopy = MakeDerivedCurve(authored.Upper, degree, sKnots, upperPts, sIds);
        var lowerCopy = MakeDerivedCurve(authored.Lower, degree, sKnots, lowerPts, sIds);
        var fittedProfile = authored with { Upper = upperCopy, Lower = lowerCopy };
        double recordDev = FoilSource.MaxOrdinateDeviation(authored, fittedProfile);
        if (!double.IsFinite(recordDev))
            return new("fitted", authored.Upper, authored.Lower, double.PositiveInfinity, true,
                "Compatible fit for profile '" + authored.Name + "' produced a non-finite residual.");

        return new("fitted", upperCopy, lowerCopy, recordDev, false, null);
    }

    private static bool TryBoehmRefine(ProfileDefinition authored, int degree, double[] sKnots, double[] sX, string[] sIds,
        out Curve? upperCopy, out Curve? lowerCopy)
    {
        upperCopy = null;
        lowerCopy = null;
        if (authored.Upper.Degree != degree || authored.Lower.Degree != degree) return false;
        if (!authored.Upper.Knots.SequenceEqual(authored.Lower.Knots)) return false;

        // Every interior knot of authored must appear in sKnots with >= multiplicity.
        var authoredCounts = InteriorKnotCounts(authored.Upper.Knots);
        var targetCounts = InteriorKnotCounts(sKnots);
        foreach (var (k, count) in authoredCounts)
            if (!targetCounts.TryGetValue(k, out int targetCount) || targetCount < count) return false;

        var missing = new List<double>();
        foreach (var (k, targetCount) in targetCounts.OrderBy(pair => pair.Key))
        {
            int have = authoredCounts.TryGetValue(k, out int existing) ? existing : 0;
            for (int add = 0; add < targetCount - have; add++) missing.Add(k);
        }

        double[] knots = authored.Upper.Knots.ToArray();
        double[][] up = CopyPoints(authored.Upper.Points);
        double[][] lo = CopyPoints(authored.Lower.Points);
        foreach (double k in missing)
        {
            var uIns = FoilSource.InsertOnce(knots, up, degree, k);
            var lIns = FoilSource.InsertOnce(knots, lo, degree, k);
            knots = uIns.Knots;
            up = uIns.Points;
            lo = lIns.Points;
        }

        if (!knots.SequenceEqual(sKnots) || up.Length != sX.Length || lo.Length != sX.Length) return false;
        for (int i = 0; i < sX.Length; i++)
        {
            if (!double.IsFinite(up[i][0]) || !double.IsFinite(up[i][1]) || !double.IsFinite(lo[i][0]) || !double.IsFinite(lo[i][1]))
                return false;
            if (Math.Abs(up[i][0] - sX[i]) > RefinementAbscissaTolerance || Math.Abs(lo[i][0] - sX[i]) > RefinementAbscissaTolerance)
                return false;
        }

        // Adopt S*'s control x bitwise so SharedAbscissa holds over exact rationals (cross-profile-abscissa.md §3.1.3).
        for (int i = 0; i < sX.Length; i++)
        {
            up[i][0] = sX[i];
            lo[i][0] = sX[i];
        }
        upperCopy = MakeDerivedCurve(authored.Upper, degree, sKnots, up, sIds);
        lowerCopy = MakeDerivedCurve(authored.Lower, degree, sKnots, lo, sIds);
        return true;
    }

    private static CompatibleDefinition MeasureSurfaceResiduals(Definition authored, CompatibleDefinition prep, StationGrid grid)
    {
        var stations = authored.Assignments;
        var kindByProfile = new Dictionary<int, string>();
        for (int i = 0; i < prep.Matches.Count; i++)
        {
            var match = prep.Matches[i];
            int profileIndex = stations[match.Assignments[0]].Profile;
            kindByProfile[profileIndex] = match.Kind;
        }

        // BC-4: collect all intervals touching any non-identity copy.
        var touched = new List<(int Interval, int Start, int Count)>();
        var allEtas = new List<double>();
        for (int iv = 0; iv + 1 < stations.Length; iv++)
        {
            int leftProfile = stations[iv].Profile;
            int rightProfile = stations[iv + 1].Profile;
            if (kindByProfile[leftProfile] == "identity" && kindByProfile[rightProfile] == "identity") continue;
            var ivEtas = StationEdits.Grid(grid, (stations[iv].Eta, stations[iv + 1].Eta));
            touched.Add((iv, allEtas.Count, ivEtas.Count));
            allEtas.AddRange(ivEtas);
        }

        if (allEtas.Count == 0) return prep;

        double[] xs = new double[grid.XSamples];
        for (int i = 0; i < grid.XSamples; i++) xs[i] = i / (double)(grid.XSamples - 1);

        // C1: sole caller of Placement.SectionsForMeasureAuthored (all-authored vs all-copies).
        var authoredMeasures = EvaluateParallel(allEtas, chunk => Placement.SectionsForMeasureAuthored(authored, chunk, xs, CancellationToken.None));
        var copyMeasures = EvaluateParallel(allEtas, chunk => Placement.SectionsForMeasure(authored, chunk, xs, CancellationToken.None));

        var intervalResult = new Dictionary<int, (double Meters, double AtEta)>(touched.Count);
        foreach (var (iv, start, count) in touched)
        {
            var sliceEtas = allEtas.GetRange(start, count);
            var sliceA = new SectionMeasure[count];
            var sliceB = new SectionMeasure[count];
            Array.Copy(authoredMeasures, start, sliceA, 0, count);
            Array.Copy(copyMeasures, start, sliceB, 0, count);
            var (_, meters, atEta, _) = StationEdits.CompareMeasures(sliceA, sliceB, sliceEtas, xs.Length);
            intervalResult[iv] = (meters, atEta);
        }

        // BC-4: identity copies report 0; each non-identity copy reports the worst all-authored vs all-copies
        // surface residual across all intervals touching that copy.
        var updated = new BlendMatch[prep.Matches.Count];
        for (int i = 0; i < prep.Matches.Count; i++)
        {
            var match = prep.Matches[i];
            if (match.Kind == "identity")
            {
                updated[i] = match with { SurfaceDeviationMeters = 0d, SurfaceEta = 0d };
                continue;
            }
            int profileIndex = stations[match.Assignments[0]].Profile;
            double bestMeters = 0d;
            double bestEta = stations[match.Assignments[0]].Eta;
            foreach (var (iv, (meters, atEta)) in intervalResult)
            {
                if (stations[iv].Profile != profileIndex && stations[iv + 1].Profile != profileIndex) continue;
                if (meters > bestMeters)
                {
                    bestMeters = meters;
                    bestEta = atEta;
                }
            }
            updated[i] = match with { SurfaceDeviationMeters = bestMeters, SurfaceEta = bestEta };
        }

        return prep with { Matches = updated };
    }

    private static SectionMeasure[] EvaluateParallel(IReadOnlyList<double> etas, Func<IReadOnlyList<double>, IReadOnlyList<SectionMeasure>> eval)
    {
        var result = new SectionMeasure[etas.Count];
        int chunks = Math.Min(etas.Count, Environment.ProcessorCount);
        Parallel.For(0, chunks, chunk =>
        {
            int from = (int)((long)etas.Count * chunk / chunks);
            int to = (int)((long)etas.Count * (chunk + 1) / chunks);
            var slice = new double[to - from];
            for (int i = 0; i < slice.Length; i++) slice[i] = etas[from + i];
            var part = eval(slice);
            for (int i = 0; i < part.Count; i++) result[from + i] = part[i];
        });
        return result;
    }

    private static List<(int ProfileIndex, int[] Assignments)> CollectStationProfiles(Definition definition)
    {
        var order = new List<int>();
        var map = new Dictionary<int, List<int>>();
        for (int i = 0; i < definition.Assignments.Length; i++)
        {
            int p = definition.Assignments[i].Profile;
            if (!map.TryGetValue(p, out var list))
            {
                list = [];
                map[p] = list;
                order.Add(p);
            }
            list.Add(i);
        }
        return order.Select(p => (p, map[p].ToArray())).ToList();
    }

    private static int SelectBasis(Definition definition, List<(int ProfileIndex, int[] Assignments)> stations)
    {
        int best = 0;
        var bestCurve = definition.Profiles[stations[0].ProfileIndex].Upper;
        int bestSpans = SpanCount(bestCurve.Knots);
        int bestVertices = bestCurve.Points.Length;
        int bestStation = stations[0].Assignments[0];

        for (int i = 1; i < stations.Count; i++)
        {
            var curve = definition.Profiles[stations[i].ProfileIndex].Upper;
            int spans = SpanCount(curve.Knots);
            int vertices = curve.Points.Length;
            int firstStation = stations[i].Assignments[0];
            if (spans > bestSpans ||
                spans == bestSpans && vertices > bestVertices ||
                spans == bestSpans && vertices == bestVertices && firstStation < bestStation)
            {
                best = i;
                bestSpans = spans;
                bestVertices = vertices;
                bestStation = firstStation;
            }
        }
        return best;
    }

    private static TangentRow[] MapTangentRows(Curve authored, double[] sKnots, double[] sX, int degree, string[] sIds)
    {
        if (authored.Tangents.Length == 0) return [];
        var sAnchors = new List<int>();
        for (int j = degree; j <= sX.Length - degree - 1; j++)
            if (FoilSource.IsAnchor(sKnots, sX.Length, degree, j)) sAnchors.Add(j);
        if (sAnchors.Count == 0) return [];

        var mapped = new List<TangentRow>();
        var used = new HashSet<int>();
        foreach (var row in authored.Tangents)
        {
            int idx = Array.IndexOf(authored.Ids, row.Id);
            if (idx <= 0 || idx >= authored.Points.Length - 1) continue;
            double xa = authored.Points[idx][0];
            int bestAnchor = -1;
            double bestDist = double.PositiveInfinity;
            foreach (int j in sAnchors)
            {
                double dist = Math.Abs(sX[j] - xa);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestAnchor = j;
                }
            }
            if (bestAnchor >= 0 && bestDist <= AnchorCoincideTolerance && used.Add(bestAnchor))
                mapped.Add(new TangentRow(sIds[bestAnchor], row.Kind, row.Angle));
        }
        return mapped.ToArray();
    }

    private static CompatibleDefinition FailedResult(Definition definition, string basisProfile, string reason) =>
        new(definition, [], basisProfile, 0, true, true, reason, false);

    private static Curve MakeDerivedCurve(Curve template, int degree, double[] knots, double[][] points, string[] ids) =>
        template with
        {
            Degree = degree,
            Knots = knots.ToArray(),
            Points = CopyPoints(points),
            Ids = ids.ToArray(),
            Tangents = []
        };

    private static bool SameSpacing(Curve curve, int degree, double[] knots, double[] x)
    {
        if (curve.Degree != degree || curve.Knots.Length != knots.Length || curve.Points.Length != x.Length) return false;
        for (int i = 0; i < knots.Length; i++) if (curve.Knots[i] != knots[i]) return false;
        for (int i = 0; i < x.Length; i++) if (curve.Points[i][0] != x[i]) return false;
        return true;
    }

    private static int SpanCount(double[] knots)
    {
        var distinct = new HashSet<double>();
        foreach (double k in knots)
            if (k > 0 && k < 1) distinct.Add(k);
        return distinct.Count + 1;
    }

    private static Dictionary<double, int> InteriorKnotCounts(double[] knots)
    {
        var counts = new Dictionary<double, int>();
        foreach (double k in knots)
        {
            if (k <= 0 || k >= 1) continue;
            counts[k] = counts.TryGetValue(k, out int existing) ? existing + 1 : 1;
        }
        return counts;
    }

    private static double SnapKnot(double[] knots, double t)
    {
        foreach (double existing in knots)
            if (existing > 0 && existing < 1 && Math.Abs(existing - t) <= AnchorSnapTolerance * Math.Max(1, Math.Abs(t)))
                return existing;
        return t;
    }

    private static int CountKnot(double[] knots, double t)
    {
        int count = 0;
        for (int i = 0; i < knots.Length; i++) if (knots[i] == t) count++;
        return count;
    }

    private static bool AllFinite(double[][] points)
    {
        for (int i = 0; i < points.Length; i++)
            if (!double.IsFinite(points[i][0]) || !double.IsFinite(points[i][1])) return false;
        return true;
    }

    private static double[][] CopyPoints(double[][] points)
    {
        var copy = new double[points.Length][];
        for (int i = 0; i < points.Length; i++) copy[i] = [points[i][0], points[i][1]];
        return copy;
    }

    private static string[] MakeIds(int count)
    {
        var ids = new string[count];
        for (int i = 0; i < count; i++) ids[i] = "cv-" + i.ToString(CultureInfo.InvariantCulture);
        return ids;
    }

    private static string BasisKey(int degree, double[] knots, double[] x)
    {
        var sb = new StringBuilder();
        sb.Append(degree).Append('|');
        foreach (double k in knots) sb.Append(BitConverter.DoubleToInt64Bits(k).ToString("X16", CultureInfo.InvariantCulture)).Append(',');
        sb.Append('|');
        foreach (double v in x) sb.Append(BitConverter.DoubleToInt64Bits(v).ToString("X16", CultureInfo.InvariantCulture)).Append(',');
        return Identity.Blake3(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static string TangentsKey(ProfileDefinition profile)
    {
        if (profile.Upper.Tangents.Length == 0 && profile.Lower.Tangents.Length == 0) return "";
        var sb = new StringBuilder();
        foreach (var row in profile.Upper.Tangents)
            sb.Append('U').Append(row.Id).Append(':').Append(row.Kind).Append(':').Append(row.Angle?.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        foreach (var row in profile.Lower.Tangents)
            sb.Append('L').Append(row.Id).Append(':').Append(row.Kind).Append(':').Append(row.Angle?.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        return sb.ToString();
    }

    private static string DefinitionMemoKey(Definition definition, string? surfaceKey)
    {
        string baseKey = surfaceKey ?? SemanticKey(definition);
        string tangents = AllStationTangentsKey(definition);
        return tangents.Length == 0
            ? baseKey + "|" + Evaluator.V3
            : baseKey + "|" + tangents + "|" + Evaluator.V3;
    }

    private static string AllStationTangentsKey(Definition definition)
    {
        StringBuilder? sb = null;
        for (int index = 0; index < definition.Assignments.Length; index++)
        {
            var profile = definition.Profiles[definition.Assignments[index].Profile];
            string key = TangentsKey(profile);
            if (key.Length == 0) continue;
            sb ??= new StringBuilder();
            sb.Append(index).Append('=').Append(profile.Name).Append('[').Append(key).Append(']');
        }
        return sb is null ? "" : sb.ToString();
    }

    private static string SemanticKey(Definition definition) =>
        Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(definition.Semantic)));
}
