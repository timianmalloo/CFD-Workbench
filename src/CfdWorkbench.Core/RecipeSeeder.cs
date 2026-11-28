using System.Diagnostics;
using System.Globalization;

namespace CfdWorkbench.Core;

// Exact 2D deterministic soft-target solver & one-way FoilDSL 4.1 emitter (docs/design/setup-seeding.md §3, Ruling 272 DR-SET-3).
// Zero black-box optimizer; zero Math.Pow (Ruling 157); always satisfies TipChord.MinimumMeters (Ruling 93).

/// <summary>Inputs to <see cref="RecipeSeeder.Solve"/>.</summary>
public sealed record RecipeSolveInput(
    SoftTargetSet SoftTargets,
    double DefaultSpanMeters,
    double DefaultRootChordMeters,
    double AnhedralDeg = -5.0,
    double WashoutDeg = -1.0,
    double RootThicknessRatio = 0.10,
    double SweepQuarterChordDeg = 0.0,
    string ProfileId = "naca-4412",
    string? PreferredProfileId = null,
    string? FallbackReason = null);

/// <summary>Output of <see cref="RecipeSeeder.Solve"/>.</summary>
public sealed record RecipeSolveResult(
    RecipeRecord Recipe,
    double AreaSquareMeters,
    double AspectRatio,
    double MeanChordMeters,
    double MacMeters,
    bool Conflict,
    double MaxRelativeDeviation,
    bool TipClampActive,
    int CandidatesEvaluated,
    double SolveMs);

/// <summary>Request submitted to <see cref="AuthoringSession.Seed"/>.</summary>
public sealed record SetupSeedRequest(
    string Purpose,
    SoftTargetSet? SoftTargets = null,
    string EntryKind = "parameters",
    string? Text = null,
    string? LanguageMeta = null,
    double? RiderMassKg = null,
    WaterRecord? Water = null,
    FieldProvenance? WaterProvenance = null,
    double? HRefMeters = null,
    double? MastLengthCm = null,
    double? RootThicknessRatio = null,
    double? AnhedralDeg = null,
    double? WashoutDeg = null,
    string? ProfileId = null,
    string? ClassRulePresetId = null,
    string? ClassRuleVersion = null,
    IReadOnlyList<FlaggedAcknowledgement>? FlaggedAcknowledgements = null,
    bool AcknowledgeAllFlagged = false);

public static class RecipeSeeder
{
    public const string SolverVersion = "cfdw-seed/1";
    public const string PlanformFamily = "cubic-tapered-4cv";
    public const double MinSpanMeters = 0.10;
    public const double MaxSpanMeters = 5.00;
    public const double MinRootChordMeters = 0.020;
    public const double MaxRootChordMeters = 1.00;
    public const double MinTaper = 0.02;
    public const double MaxTaper = 1.00;

    /// <summary>Exact closed-form fill factor of the 4-CV cubic Bézier planform: <c>κ(λ) = (3 + λ) / 4</c>.</summary>
    public static double Kappa(double taper)
    {
        double lam = Math.Clamp(taper, MinTaper, MaxTaper);
        return (3.0 + lam) * 0.25;
    }

    /// <summary>Exact closed-form MAC of the 4-CV cubic Bézier planform (agrees with <see cref="WingEstimates.From"/> to &lt; 1e-12).</summary>
    public static double Mac(double rootChordMeters, double taper)
    {
        double lam = Math.Clamp(taper, MinTaper, MaxTaper);
        double k = Kappa(lam);
        double oneMinus = 1.0 - lam;
        double intG2 = 0.5 * (1.0 + lam) + (oneMinus * oneMinus) / 7.0;
        return rootChordMeters * intG2 / k;
    }

    /// <summary>
    /// Exact deterministic finite-candidate minimization of lexicographic <c>(J1, J2, J3)</c> on the hard feasible
    /// domain Ω (Ruling 272 DR-SET-3, Ruling 93 TipChord floor, Ruling 157 zero Math.Pow).
    /// </summary>
    public static RecipeSolveResult Solve(RecipeSolveInput input)
    {
        var sw = Stopwatch.StartNew();
        ArgumentNullException.ThrowIfNull(input);
        SetupStore.CheckSoftTargets(input.SoftTargets);

        var st = input.SoftTargets;
        double lam = Math.Clamp(st.Taper.Value, MinTaper, MaxTaper);
        double k = Kappa(lam);

        double bLo = MinSpanMeters;
        double bHi = MaxSpanMeters;
        double cLo = Math.Max(MinRootChordMeters, TipChord.FloorMeters / lam);
        double cHi = MaxRootChordMeters;

        double bMax = st.MaxSpanMeters?.Value ?? 0.0;
        double wb = st.MaxSpanMeters?.Weight ?? 0.0;
        double cMax = st.MaxChordMeters?.Value ?? 0.0;
        double wc = st.MaxChordMeters?.Weight ?? 0.0;
        double areaM2 = st.TargetAreaSquareMeters?.Value ?? 0.0;
        double wS = st.TargetAreaSquareMeters?.Weight ?? 0.0;
        double arTarget = st.TargetAspectRatio?.Value ?? 0.0;
        double wAr = st.TargetAspectRatio?.Weight ?? 0.0;

        double bDef = Math.Clamp(input.DefaultSpanMeters > 0 && double.IsFinite(input.DefaultSpanMeters) ? input.DefaultSpanMeters : 1.0, bLo, bHi);
        double cDef = Math.Clamp(input.DefaultRootChordMeters > 0 && double.IsFinite(input.DefaultRootChordMeters) ? input.DefaultRootChordMeters : 0.12, cLo, cHi);

        var bVals = new List<double>(8) { bLo, bHi, bDef };
        if (bMax > 0) bVals.Add(bMax);
        var cVals = new List<double>(8) { cLo, cHi, cDef };
        if (cMax > 0) cVals.Add(cMax);
        if (areaM2 > 0 && arTarget > 0)
        {
            bVals.Add(Math.Sqrt(areaM2 * arTarget));
            cVals.Add(Math.Sqrt(areaM2 / arTarget) / k);
        }

        var candidates = new List<(double B, double C)>(160);

        // 1. Pairwise intersections of loci and box boundaries.
        foreach (double b in bVals)
        {
            foreach (double c in cVals) candidates.Add((b, c));
            if (areaM2 > 0) candidates.Add((b, areaM2 / (b * k)));
            if (arTarget > 0) candidates.Add((b, b / (arTarget * k)));
        }
        foreach (double c in cVals)
        {
            if (areaM2 > 0) candidates.Add((areaM2 / (c * k), c));
            if (arTarget > 0) candidates.Add((arTarget * c * k, c));
        }

        // 2 & 3. Interior stationary points on fixed-b lines and S = S* hyperbola for J1 weights (and zero-weight J2 tie-break).
        AddStationaryFamilies(candidates, bVals, bLo, bHi, k, bMax, wb, cMax, wc, areaM2, wS, arTarget, wAr);
        if ((bMax > 0 && wb == 0) || (cMax > 0 && wc == 0) || (areaM2 > 0 && wS == 0) || (arTarget > 0 && wAr == 0))
        {
            AddStationaryFamilies(candidates, bVals, bLo, bHi, k,
                bMax, bMax > 0 && wb == 0 ? 1.0 : 0.0,
                cMax, cMax > 0 && wc == 0 ? 1.0 : 0.0,
                areaM2, areaM2 > 0 && wS == 0 ? 1.0 : 0.0,
                arTarget, arTarget > 0 && wAr == 0 ? 1.0 : 0.0);
        }

        CandidateScore best = default;
        bool hasBest = false;
        int evaluated = 0;

        foreach (var (bIn, cIn) in candidates)
        {
            if (!double.IsFinite(bIn) || !double.IsFinite(cIn) || bIn <= 0 || cIn <= 0) continue;
            double b = Math.Clamp(bIn, bLo, bHi);
            double c = Math.Clamp(cIn, cLo, cHi);
            double s = b * c * k;
            double ar = b / (c * k);
            double db = bMax > 0 ? Math.Max(0.0, (b - bMax) / bMax) : 0.0;
            double dc = cMax > 0 ? Math.Max(0.0, (c - cMax) / cMax) : 0.0;
            double dS = areaM2 > 0 ? Math.Abs(s - areaM2) / areaM2 : 0.0;
            double dAr = arTarget > 0 ? Math.Abs(ar - arTarget) / arTarget : 0.0;

            double j1 = Math.Round((wb * db + wc * dc + wS * dS + wAr * dAr) * 1e14) / 1e14;
            double j2 = Math.Round(((wb == 0 ? db : 0.0) + (wc == 0 ? dc : 0.0) + (wS == 0 ? dS : 0.0) + (wAr == 0 ? dAr : 0.0)) * 1e14) / 1e14;
            double j3 = Math.Abs(b - bDef) / bDef + Math.Abs(c - cDef) / cDef;

            evaluated++;
            if (!hasBest ||
                j1 < best.J1 ||
                (j1 == best.J1 && j2 < best.J2) ||
                (j1 == best.J1 && j2 == best.J2 && j3 < best.J3))
            {
                best = new CandidateScore(j1, j2, j3, b, c, s, ar, db, dc, dS, dAr);
                hasBest = true;
            }
        }

        double maxDev = Math.Max(Math.Max(best.Db, best.Dc), Math.Max(best.DS, best.DAr));
        bool conflict = maxDev > 1e-6;
        bool tipClamped = best.C <= cLo + 1e-12;

        var deviations = new List<RecipeDeviation>(4);
        if (bMax > 0)
            deviations.Add(new("maxSpan", bMax, best.B, best.B > bMax ? (best.B - bMax) / bMax : 0.0));
        if (cMax > 0)
            deviations.Add(new("maxChord", cMax, best.C, best.C > cMax ? (best.C - cMax) / cMax : 0.0));
        if (areaM2 > 0)
            deviations.Add(new("targetArea", areaM2, best.S, (best.S - areaM2) / areaM2));
        if (arTarget > 0)
            deviations.Add(new("targetAR", arTarget, best.Ar, (best.Ar - arTarget) / arTarget));

        string? conflictNotice = conflict ? BuildConflictNotice(st, best, k) : null;
        double tipChord = best.C * lam;
        double meanChord = best.S / best.B;
        double mac = Mac(best.C, lam);

        sw.Stop();
        var recipe = new RecipeRecord(
            SolverVersion: SolverVersion,
            PlanformFamily: PlanformFamily,
            SpanMeters: best.B,
            RootChordMeters: best.C,
            TipChordMeters: tipChord,
            Taper: lam,
            SweepQuarterChordDeg: input.SweepQuarterChordDeg,
            AnhedralDeg: input.AnhedralDeg,
            WashoutDeg: input.WashoutDeg,
            RootThicknessRatio: Math.Clamp(input.RootThicknessRatio, 0.04, 0.25),
            ProfileId: input.ProfileId,
            ProfileOrigin: "gen:" + input.ProfileId,
            PreferredProfileId: input.PreferredProfileId,
            FallbackReason: input.FallbackReason,
            ConflictNotice: conflictNotice,
            Deviations: deviations);

        return new RecipeSolveResult(
            recipe,
            best.S,
            best.Ar,
            meanChord,
            mac,
            conflict,
            maxDev,
            tipClamped,
            evaluated,
            sw.Elapsed.TotalMilliseconds);
    }

    private static void AddStationaryFamilies(
        List<(double B, double C)> candidates,
        List<double> bVals,
        double bLo, double bHi, double k,
        double bMax, double wb,
        double cMax, double wc,
        double areaM2, double wS,
        double arTarget, double wAr)
    {
        // Fixed-b interior stationary points in c (AR > AR*, wAr > 0, sigma_c in {0, 1}, sigma_S in {-1, 0, +1}).
        if (arTarget > 0 && wAr > 0)
        {
            foreach (double b in bVals)
            {
                if (b <= 0) continue;
                double bigB = (wAr * b) / (k * arTarget);
                for (int sc = 0; sc <= 1; sc++)
                {
                    double tc = sc == 1 && cMax > 0 ? wc / cMax : 0.0;
                    for (int sS = -1; sS <= 1; sS++)
                    {
                        double tS = areaM2 > 0 ? (sS * wS * b * k) / areaM2 : 0.0;
                        double bigA = tc + tS;
                        if (bigA > 1e-18)
                            candidates.Add((b, Math.Sqrt(bigB / bigA)));
                    }
                }
            }
        }

        // Hyperbola S = S* interior stationary points in b (c > cMax, wc > 0).
        if (areaM2 > 0 && cMax > 0 && wc > 0)
        {
            double cC = (wc * areaM2) / (k * cMax);
            if (bMax > 0 && wb > 0)
            {
                double bStat = Math.Sqrt((cC * bMax) / wb);
                candidates.Add((bStat, areaM2 / (bStat * k)));
            }
            if (arTarget > 0 && wAr > 0)
            {
                double cAr = (2.0 * wAr) / (areaM2 * arTarget);
                double bAr = Math.Sqrt(areaM2 * arTarget);
                for (int sb = 0; sb <= 1; sb++)
                {
                    if (sb == 1 && !(bMax > 0 && wb > 0)) continue;
                    double cB = sb == 1 ? wb / bMax : 0.0;

                    // Region AR > AR* (sigma_AR = +1): strictly increasing derivative.
                    double lo1 = Math.Max(bLo, Math.Max(bAr, sb == 1 ? bMax : bLo));
                    if (lo1 < bHi)
                    {
                        double? r1 = BisectRoot(x => cB + cAr * x - cC / (x * x), lo1, bHi);
                        if (r1 is { } rv1) candidates.Add((rv1, areaM2 / (rv1 * k)));
                    }

                    // Region AR < AR* (sigma_AR = -1, sigma_b = 1): convex up to bPeak where cAr*x^3 = 2*cC.
                    double lo2 = Math.Max(bLo, sb == 1 ? bMax : bLo);
                    double hi2 = Math.Min(bHi, bAr);
                    if (lo2 < hi2 && cB > 0)
                    {
                        double bPeak = BisectRoot(x => cAr * x * x * x - 2.0 * cC, bLo, bHi) ?? bHi;
                        double hiConv = Math.Min(hi2, bPeak);
                        if (lo2 < hiConv)
                        {
                            double? r2 = BisectRoot(x => cB - cAr * x - cC / (x * x), lo2, hiConv);
                            if (r2 is { } rv2) candidates.Add((rv2, areaM2 / (rv2 * k)));
                        }
                    }
                }
            }
        }
    }

    private static double? BisectRoot(Func<double, double> fn, double lo, double hi)
    {
        double flo = fn(lo);
        double fhi = fn(hi);
        if (flo == 0.0) return lo;
        if (fhi == 0.0) return hi;
        if ((flo > 0.0) == (fhi > 0.0)) return null;
        double a = lo, b = hi, fa = flo;
        for (int i = 0; i < 64; i++)
        {
            double m = 0.5 * (a + b);
            double fm = fn(m);
            if (fm == 0.0) return m;
            if ((fa > 0.0) == (fm > 0.0)) { a = m; fa = fm; }
            else b = m;
        }
        return 0.5 * (a + b);
    }

    private static string BuildConflictNotice(SoftTargetSet st, CandidateScore best, double k)
    {
        string a, b;
        if (st.TargetAreaSquareMeters is { } area && st.MaxSpanMeters is { } span && st.MaxChordMeters is { } chord &&
            span.Value * chord.Value * k < area.Value - 1e-9)
        {
            a = $"target area ({area.Value * 1e4:0} cm²)";
            b = $"max span ({span.Value:0.##} m) × max chord ({chord.Value * 1e3:0} mm)";
        }
        else if (st.MaxSpanMeters is { } sp && st.TargetAreaSquareMeters is { } arArea && st.TargetAspectRatio is { } ar &&
                 sp.Value < Math.Sqrt(arArea.Value * ar.Value) - 1e-9)
        {
            a = $"max span ({sp.Value:0.###} m)";
            b = $"target AR ({ar.Value:0.##}) at {arArea.Value * 1e4:0} cm²";
        }
        else if (st.MaxChordMeters is { } ch && st.TargetAreaSquareMeters is { } cArea && st.TargetAspectRatio is { } cAr &&
                 ch.Value < Math.Sqrt(cArea.Value / cAr.Value) / k - 1e-9)
        {
            a = $"max chord ({ch.Value * 1e3:0} mm)";
            b = $"target area ({cArea.Value * 1e4:0} cm²) at AR {cAr.Value:0.##}";
        }
        else
        {
            a = best.Db >= best.Dc ? "max span" : "max chord";
            b = best.DS >= best.DAr ? "target area" : "target AR";
        }
        return $"Targets conflict: {a} and {b} — seeded the nearest feasible design; targets stay as preferences";
    }

    private readonly record struct CandidateScore(
        double J1, double J2, double J3,
        double B, double C, double S, double Ar,
        double Db, double Dc, double DS, double DAr);

    /// <summary>
    /// One-way emission of a <see cref="RecipeRecord"/> into valid <c>foildsl "4.1"</c> UTF-8 bytes using
    /// <see cref="FoilSource.ExactDecimal"/> (zero binary64 rounding, GEO-01 / GEO-04 / ADR-0002).
    /// </summary>
    public static byte[] EmitFoilDsl(RecipeRecord recipe, CatalogEntry genProfile, string foilName = "Seeded Wing")
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(genProfile);
        if (genProfile.Coordinates is null || genProfile.DisabledReason is not null)
            throw new ContractError("CAT-UNAVAILABLE", $"Profile '{genProfile.Id}' is not available in this build.");

        string prov = "gen:" + genProfile.Id;
        string datText = FoilSource.Utf8.GetString(genProfile.Coordinates);
        return EmitFoilDsl(
            recipe: recipe,
            rootId: genProfile.Id,
            rootDatText: datText,
            tipId: genProfile.Id,
            tipDatText: datText,
            foilName: foilName,
            rootProvenance: prov,
            tipProvenance: prov);
    }

    /// <summary>
    /// Two-profile pure zero-I/O <see cref="RecipeRecord"/> emitter supporting distinct root and tip sections
    /// (admitted GEN or embedded <c>My sections</c> coordinate snapshots) and optional <c>foildsl "4.2"</c>
    /// rounded 3D tip closure (<c>docs/design/ai-guided-design-notebook.md §3.2</c>).
    /// </summary>
    public static byte[] EmitFoilDsl(
        RecipeRecord recipe,
        string rootId,
        string rootDatText,
        string tipId,
        string tipDatText,
        string foilName = "Seeded Wing",
        string? rootProvenance = null,
        string? tipProvenance = null,
        string? tipStyle = null,
        double? tipCapRatio = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (string.IsNullOrWhiteSpace(rootId) || string.IsNullOrWhiteSpace(rootDatText))
            throw new ContractError("CAT-UNAVAILABLE", "Root profile id and .dat coordinates are required.");
        if (string.IsNullOrWhiteSpace(tipId) || string.IsNullOrWhiteSpace(tipDatText))
            throw new ContractError("CAT-UNAVAILABLE", "Tip profile id and .dat coordinates are required.");

        string cleanRootId = rootId.Trim();
        string cleanTipId = tipId.Trim();

        // 4-CV degree-3 planform rails on TipClusteredChannel(4, 3): knots [0,0,0,0,1,1,1,1], x [0, 1/3, 2/3, 1].
        double[] railKnots = [0, 0, 0, 0, 1, 1, 1, 1];
        double[] railX = [0, 1.0 / 3.0, 2.0 / 3.0, 1.0];
        double[] g = [1.0, 1.0, 1.0, recipe.Taper];
        double halfSpan = recipe.SpanMeters * 0.5;
        double tanSweep = recipe.SweepQuarterChordDeg == 0 ? 0.0 : Math.Tan(recipe.SweepQuarterChordDeg * PlacementRule.RadiansPerDegree);
        var lead = new double[4];
        var trail = new double[4];
        for (int i = 0; i < 4; i++)
        {
            double chord = recipe.RootChordMeters * g[i];
            // Keep P1.aft == P0.aft on both rails so root_mirror G1 symmetry holds for nonzero quarter-chord sweep.
            double sweepFraction = i <= 1 ? 0.0 : (railX[i] - railX[1]) / (1.0 - railX[1]);
            double le = recipe.RootChordMeters * (1.0 - g[i]) * 0.25 + halfSpan * sweepFraction * tanSweep;
            lead[i] = le;
            trail[i] = le + chord;
        }

        // 10-CV degree-3 tip-clustered channels for dihedral, twist, thickness (matching FoilSource.NewDefault).
        var (channelKnots, channelX) = TipClusteredChannel10();
        double tipDihedral = recipe.AnhedralDeg == 0 ? 0.0 : halfSpan * Math.Tan(recipe.AnhedralDeg * PlacementRule.RadiansPerDegree);
        var dihedral = SmoothRootChannel(channelX, tipDihedral);
        var twist = SmoothRootChannel(channelX, recipe.WashoutDeg);
        var thickness = Enumerable.Repeat(Math.Clamp(recipe.RootThicknessRatio, 0.04, 0.25), 10).ToArray();

        string rootBlock = BuildIndentedProfileBlock(cleanRootId, rootDatText, rootProvenance ?? ("gen:" + cleanRootId));
        bool sameProfile = string.Equals(cleanRootId, cleanTipId, StringComparison.Ordinal);
        string profilesBody = sameProfile
            ? rootBlock
            : rootBlock + "\n" + BuildIndentedProfileBlock(cleanTipId, tipDatText, tipProvenance ?? ("gen:" + cleanTipId));

        string safeName = foilName.Replace("\"", "", StringComparison.Ordinal).Trim();
        if (safeName.Length == 0) safeName = "Seeded Wing";

        bool roundedTip = string.Equals(tipStyle, "rounded", StringComparison.OrdinalIgnoreCase);
        string dslVersion = roundedTip ? "4.2" : "4.1";
        string tipClause;
        if (roundedTip)
        {
            double ratio = tipCapRatio ?? TipCapMath.DefaultCapRatio;
            if (!double.IsFinite(ratio) || ratio < TipCapMath.MinCapRatio || ratio > TipCapMath.MaxCapRatio)
                throw new ContractError("DSL-RANGE", $"Tip cap ratio must be in [{TipCapMath.MinCapRatio}, {TipCapMath.MaxCapRatio}].");
            tipClause = "  tip rounded " + FoilSource.ExactDecimal(ratio) + "\n";
        }
        else
        {
            tipClause = "  tip open\n";
        }

        string text =
            $"foildsl \"{dslVersion}\"\n" +
            $"foil {Jcs.Quote(safeName)} {{\n" +
            "  units m\n" +
            "  half_span " + FoilSource.ExactDecimal(halfSpan) + " m\n" +
            "  evaluator \"cfdw-cv\" \"2\"\n" +
            "  symmetry mirror_y\n" +
            "  planform {\n" +
            "    leading cv { " + ChannelText(3, railKnots, railX, lead) + " }\n" +
            "    trailing cv { " + ChannelText(3, railKnots, railX, trail) + " }\n" +
            "  }\n" +
            "  dihedral cv { " + ChannelText(3, channelKnots, channelX, dihedral) + " }\n" +
            "  twist cv { " + ChannelText(3, channelKnots, channelX, twist) + " }\n" +
            "  thickness cv { " + ChannelText(3, channelKnots, channelX, thickness) + " }\n" +
            "  profiles {\n" +
            profilesBody + "\n" +
            "  }\n" +
            $"  sections {{ at root profile {Jcs.Quote(cleanRootId)} at tip profile {Jcs.Quote(cleanTipId)} }}\n" +
            tipClause +
            "}\n";
        return FoilSource.Utf8.GetBytes(text);
    }

    private static string BuildIndentedProfileBlock(string profileId, string datText, string provenance)
    {
        var parsedDat = DatImport.Parse(FoilSource.Utf8.GetBytes(datText));
        var fitted = DatImport.Fit(parsedDat, profileId);
        string profileBlock = fitted.ProfileBlock.Replace(
            "provenance " + Jcs.Quote(fitted.Provenance),
            "provenance " + Jcs.Quote(provenance),
            StringComparison.Ordinal);
        return string.Join("\n", profileBlock.Split('\n').Select(line => "    " + line));
    }

    private static (double[] Knots, double[] X) TipClusteredChannel10()
    {
        const int count = 10;
        const int degree = 3;
        int interior = count - degree - 1;
        var knots = new double[count + degree + 1];
        for (int i = 0; i <= degree; i++) knots[i] = 0;
        for (int i = 1; i <= interior; i++)
        {
            double fraction = (double)i / (interior + 1);
            double oneMinus = 1.0 - fraction;
            double remaining = oneMinus * oneMinus * oneMinus; // repeated multiplication (Ruling 157, zero Math.Pow)
            knots[degree + i] = 1.0 - remaining;
        }
        for (int i = knots.Length - degree - 1; i < knots.Length; i++) knots[i] = 1;
        var x = new double[count];
        for (int i = 0; i < count; i++)
            x[i] = (knots[i + 1] + knots[i + 2] + knots[i + 3]) / 3.0;
        return (knots, x);
    }

    // Horizontal root tangent P1.y == P0.y == 0 (root_mirror G1) rising smoothly to tipValue at eta = 1.
    private static double[] SmoothRootChannel(double[] x, double tipValue)
    {
        var y = new double[x.Length];
        if (tipValue == 0.0) return y;
        double x1 = x[1];
        double denom = 1.0 - x1;
        for (int i = 2; i < x.Length; i++)
        {
            double t = (x[i] - x1) / denom;
            y[i] = tipValue * t * t;
        }
        y[^1] = tipValue;
        return y;
    }

    private static string ChannelText(int degree, double[] knots, double[] x, double[] y)
    {
        string knotText = string.Join(", ", knots.Select(v => FoilSource.ExactDecimal(v)));
        string points = string.Join(", ", x.Zip(y, (a, b) => "(" + FoilSource.ExactDecimal(a) + ", " + FoilSource.ExactDecimal(b) + ")"));
        string ids = string.Join(", ", Enumerable.Range(0, x.Length).Select(i => "\"cv-" + i.ToString(CultureInfo.InvariantCulture) + "\""));
        return "degree " + degree.ToString(CultureInfo.InvariantCulture) + " knots [" + knotText + "] points [" + points + "] ids [" + ids + "]";
    }
}
