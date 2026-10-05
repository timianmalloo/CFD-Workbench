using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The input range inside which the method's claims hold (DR-ANA-14; design §5.4).</summary>
public sealed record MethodEnvelope(double AlphaEffFromZeroLiftMaxDeg, double ClLocalMax, double QuarterChordSweepMaxDeg);

/// <summary>Read-time envelope state. AtBound carries η* convention discretisation; Provisional has no honest verdict.</summary>
public enum StripVerdictState { Inside, Outside, AtBound, Provisional }

/// <summary>One strip's envelope verdict, derived on read from α_eff, Cl_local and sweep (DM7: not a stored flag).
/// <see cref="UncertaintyDeg"/> is discretisation of the η* convention, not model uncertainty or an error bar.</summary>
public sealed record StripVerdict(bool Inside, IReadOnlyList<string> Exceeded, string Text)
{
    /// <summary>Outermost strip per half. The status is provisional; <see cref="Inside"/> is not an outside claim.</summary>
    public bool Provisional { get; init; }
    public StripVerdictState State { get; init; } = Inside ? StripVerdictState.Inside : StripVerdictState.Outside;
    public string? ReasonCode { get; init; }
    public double? UncertaintyDeg { get; init; }
    public double? EvaluatedAlphaEffDeg { get; init; }
}

/// <summary>The exactly calibrated tip family, reconstructed from the run's sections and settings on either read path.
/// AR is 7.98–8.02 (the AR 8 section quadrature), four cosine chord panels, cosine span stations, a 20-span wake,
/// straight quarter chord, zero dihedral, and either elliptical chord or linear taper with tip/root in [0.5, 1].
/// Sections are flat/untwisted, flat with 1° linear washin, or 4% parabolic camber without twist.</summary>
public sealed record TipLawGeometry(int NChord, string ChordSpacing, string SpanSpacing, int WakeSpans,
    double AspectRatio, double QuarterChordOffsetOverSpan, double ElevationOffsetOverSpan,
    double TipToRootChord, bool EllipticPlanform, bool LinearPlanform, bool SectionLawCalibrated)
{
    public bool Calibrated => NChord == 4 && ChordSpacing == "cosine" && SpanSpacing == "cosine" && WakeSpans == 20
        && AspectRatio >= 7.98 && AspectRatio <= 8.02
        && QuarterChordOffsetOverSpan <= 1e-8 && ElevationOffsetOverSpan <= 1e-8
        && (EllipticPlanform || LinearPlanform && TipToRootChord >= 0.5 && TipToRootChord <= 1)
        && SectionLawCalibrated;

    /// <summary>Derive the read-time family check from the same placed sections supplied to the solver.</summary>
    public static TipLawGeometry FromSections(IReadOnlyList<SectionSample> sections, RunSettings settings)
    {
        SectionSample[] sorted = sections.OrderBy(s => s.PlacedCamber[0].Y).ToArray();
        if (sorted.Length < 3) throw new ArgumentException("At least three sections are required.", nameof(sections));
        double span = sorted[^1].PlacedCamber[0].Y - sorted[0].PlacedCamber[0].Y;
        SectionSample root = sorted.MinBy(s => Math.Abs(s.PlacedCamber[0].Y))!;
        double rootChord = root.Frame.ChordMeters;
        double tipChord = Math.Max(sorted[0].Frame.ChordMeters, sorted[^1].Frame.ChordMeters);
        double tipRatio = tipChord / rootChord;
        double area = 0, quarterOffset = 0, elevationOffset = 0;
        double rootQuarter = root.Frame.LeadingMeters + rootChord / 4;
        foreach (SectionSample section in sorted)
        {
            double quarter = section.Frame.LeadingMeters + section.Frame.ChordMeters / 4;
            quarterOffset = Math.Max(quarterOffset, Math.Abs(quarter - rootQuarter) / span);
            elevationOffset = Math.Max(elevationOffset, Math.Abs(section.Frame.ElevationMeters - root.Frame.ElevationMeters) / span);
        }
        for (int i = 1; i < sorted.Length; i++)
            area += 0.5 * (sorted[i - 1].Frame.ChordMeters + sorted[i].Frame.ChordMeters)
                * (sorted[i].PlacedCamber[0].Y - sorted[i - 1].PlacedCamber[0].Y);
        bool elliptic = sorted.All(s => Math.Abs(s.Frame.ChordMeters / rootChord
            - Math.Sqrt(Math.Max(0, 1 - Math.Pow(2 * Math.Abs(s.PlacedCamber[0].Y - root.PlacedCamber[0].Y) / span, 2)))) <= 1e-6);
        bool linear = sorted.All(s => Math.Abs(s.Frame.ChordMeters / rootChord
            - (1 - (1 - tipRatio) * 2 * Math.Abs(s.PlacedCamber[0].Y - root.PlacedCamber[0].Y) / span)) <= 1e-8);
        bool flat = sorted.All(s => s.Camber.All(value => Math.Abs(value) <= 1e-8));
        bool parabolic4 = sorted.All(s => s.Camber.Count == s.X.Count
            && Enumerable.Range(0, s.X.Count).All(i => Math.Abs(s.Camber[i] - 0.16 * s.X[i] * (1 - s.X[i])) <= 1e-6));
        bool untwisted = sorted.All(s => Math.Abs(s.Frame.TwistDegrees) <= 1e-8);
        bool washin1 = sorted.All(s => Math.Abs(s.Frame.TwistDegrees
            - 2 * Math.Abs(s.PlacedCamber[0].Y - root.PlacedCamber[0].Y) / span) <= 1e-8);
        return new TipLawGeometry(settings.NChord, settings.ChordSpacing, settings.SpanSpacing, settings.WakeSpans,
            span * span / area, quarterOffset, elevationOffset, tipRatio, elliptic, linear,
            flat && untwisted || linear && Math.Abs(tipRatio - 1) <= 1e-8
                && (flat && washin1 || parabolic4 && untwisted));
    }
}

/// <summary>A method: its stored identity (id, version, convergence order) and its envelope. VLM owns the values.</summary>
public sealed record MethodRecord(RunMethod Method, MethodEnvelope Envelope)
{
    /// <summary><c>cfdw.vlm-strip</c> 1.1.0, order 1, envelope 10°, Cl 1.0, sweep 30°.</summary>
    public static MethodRecord VlmStrip { get; } = new(
        new RunMethod("cfdw.vlm-strip", "1.1.0", 1),
        new MethodEnvelope(10, 1.0, 30));

    /// <summary>
    /// The per-strip verdict. <paramref name="alphaEffDeg"/> is α + twist − α_i, never α_geo alone.
    /// An exceeded part is named and prints "&gt;"; a part inside its bound prints "≤".
    /// A provisional strip (Ruling 77(5)) reads <c>provisional</c>, not inside or outside. The sentence is held.
    /// </summary>
    public static StripVerdict JudgeStrip(double alphaEffDeg, double alphaL0Deg, double clLocal, double sweepDeg, bool provisional = false)
    {
        if (provisional)
            return new StripVerdict(false, Array.Empty<string>(), "provisional")
            { Provisional = true, State = StripVerdictState.Provisional, ReasonCode = StripLoad.TipProvisionalReason };
        MethodEnvelope env = VlmStrip.Envelope;
        double alpha = Math.Abs(alphaEffDeg - alphaL0Deg);
        double sweep = Math.Abs(sweepDeg);
        var exceeded = new List<string>(3);
        if (alpha > env.AlphaEffFromZeroLiftMaxDeg) exceeded.Add("|α_eff − α_L0|");
        if (clLocal > env.ClLocalMax) exceeded.Add("Cl_local");
        if (sweep > env.QuarterChordSweepMaxDeg) exceeded.Add("sweep");
        string parts = Part("|α_eff − α_L0|", alpha, "°", env.AlphaEffFromZeroLiftMaxDeg, "°", exceeded)
            + " · " + Part("Cl_local", clLocal, "", env.ClLocalMax, "", exceeded)
            + " · " + Part("sweep", sweep, "°", env.QuarterChordSweepMaxDeg, "°", exceeded);
        string text = exceeded.Count == 0
            ? "Inside the method envelope at this strip (" + parts + ")"
            : "Outside the method envelope at this strip — exceeded: " + string.Join(", ", exceeded) + " (" + parts + ")";
        return new StripVerdict(exceeded.Count == 0, exceeded, text);
    }

    /// <summary>Per-strip verdicts from α_eff = α + twist − α_i. The wing angle alone is not the input.</summary>
    public static IReadOnlyList<StripVerdict> Verdicts(LatticeSolution solution, double alphaDeg, double alphaL0Deg)
    {
        return JudgePoints(solution.Strips.Select(s => new TipPoint(s.J, s.Y, Math.Abs(s.Eta), s.InducedAngleDeg,
            alphaDeg + s.TwistDeg - s.InducedAngleDeg, s.ClLocal, s.SweepDeg, false)).ToArray(), alphaL0Deg,
            solution.TipLawGeometry);
    }

    /// <summary>Read-time verdicts for stored strips. The caller supplies quarter-chord sweep derived from the run geometry.</summary>
    public static IReadOnlyList<StripVerdict> Verdicts(IReadOnlyList<StripLoad> strips,
        IReadOnlyList<double> quarterChordSweepDeg, double alphaL0Deg, TipLawGeometry? geometry = null)
    {
        if (strips.Count != quarterChordSweepDeg.Count) throw new ArgumentException("One sweep value is required per strip.");
        return JudgePoints(strips.Select((s, i) => new TipPoint(i, s.Y, Math.Abs(s.Eta), s.AlphaI, s.AlphaEff,
            s.ClLocal, quarterChordSweepDeg[i], s.Provisional)).ToArray(), alphaL0Deg, geometry);
    }

    private static readonly double TipStation = 0.5 * (1 + Math.Cos(Math.PI / 128));
    private readonly record struct TipPoint(int Index, double Y, double Eta, double AlphaI, double AlphaEff,
        double Cl, double Sweep, bool StoredProvisional);

    private static IReadOnlyList<StripVerdict> JudgePoints(TipPoint[] points, double alphaL0Deg, TipLawGeometry? geometry)
    {
        var verdicts = new StripVerdict[points.Length];
        foreach (bool port in new[] { true, false })
        {
            TipPoint[] side = points.Where(p => port ? p.Y < 0 : p.Y > 0)
                .OrderByDescending(p => p.Eta).ToArray();
            bool supported = TryTipBasis(side, out double aiStar, out double uncertainty);
            for (int k = 0; k < side.Length; k++)
            {
                TipPoint point = side[k];
                bool target = point.Eta >= TipStation - 1e-12 || (k == 0 && side.Length < 64);
                if ((target || point.StoredProvisional) && (!supported || geometry?.Calibrated != true))
                {
                    verdicts[point.Index] = Provisional(supported ? "ANA-TIP-UNCALIBRATED" : StripLoad.TipProvisionalReason);
                    continue;
                }
                if (!target)
                {
                    verdicts[point.Index] = JudgeStrip(point.AlphaEff, alphaL0Deg, point.Cl, point.Sweep);
                    continue;
                }
                double adjusted = point.AlphaEff + point.AlphaI - aiStar;
                StripVerdict nominal = JudgeStrip(adjusted, alphaL0Deg, point.Cl, point.Sweep);
                double alpha = Math.Abs(adjusted - alphaL0Deg);
                bool otherOutside = point.Cl > VlmStrip.Envelope.ClLocalMax || Math.Abs(point.Sweep) > VlmStrip.Envelope.QuarterChordSweepMaxDeg;
                StripVerdictState state = otherOutside || alpha - uncertainty > VlmStrip.Envelope.AlphaEffFromZeroLiftMaxDeg
                    ? StripVerdictState.Outside
                    : alpha + uncertainty <= VlmStrip.Envelope.AlphaEffFromZeroLiftMaxDeg
                        ? StripVerdictState.Inside : StripVerdictState.AtBound;
                // Cl_local/(2π) radians is an independent thin-airfoil angle check. U is only the
                // discretisation of η*, not model uncertainty: it cannot certify a physics disagreement.
                double clImpliedAngleDeg = VortexLattice.ToDegrees(Math.Abs(point.Cl) / (2 * Math.PI));
                if (state != StripVerdictState.Inside && !otherOutside
                    && alpha + uncertainty > VlmStrip.Envelope.AlphaEffFromZeroLiftMaxDeg
                    && clImpliedAngleDeg <= VlmStrip.Envelope.AlphaEffFromZeroLiftMaxDeg)
                {
                    verdicts[point.Index] = Provisional("ANA-TIP-INCONSISTENT") with
                    { UncertaintyDeg = uncertainty, EvaluatedAlphaEffDeg = adjusted };
                    continue;
                }
                verdicts[point.Index] = state == StripVerdictState.AtBound
                    ? new StripVerdict(false, Array.Empty<string>(), "")
                    { State = state, ReasonCode = "ANA-TIP-AT-BOUND", UncertaintyDeg = uncertainty, EvaluatedAlphaEffDeg = adjusted }
                    : nominal with { Inside = state == StripVerdictState.Inside, State = state,
                        UncertaintyDeg = uncertainty, EvaluatedAlphaEffDeg = adjusted };
            }
        }
        for (int i = 0; i < verdicts.Length; i++)
            verdicts[i] ??= JudgeStrip(points[i].AlphaEff, alphaL0Deg, points[i].Cl, points[i].Sweep,
                provisional: points[i].StoredProvisional);
        return verdicts;
    }

    private static StripVerdict Provisional(string reason) => new(false, Array.Empty<string>(), "provisional")
    { Provisional = true, State = StripVerdictState.Provisional, ReasonCode = reason };

    private static bool TryTipBasis(TipPoint[] side, out double aiStar, out double uncertainty)
    {
        aiStar = uncertainty = 0;
        int n = side.Length;
        double c = n switch { 16 => 2.40, 32 => 0.84, 64 => 0.20, 128 or 256 => 0, _ => double.NaN };
        if (!double.IsFinite(c) || n < 2) return false;
        for (int k = 0; k < n; k++)
        {
            double eta = 0.5 * (Math.Cos(k * Math.PI / (2 * n)) + Math.Cos((k + 1) * Math.PI / (2 * n)));
            if (!double.IsFinite(side[k].AlphaI) || !double.IsFinite(side[k].AlphaEff)
                || !double.IsFinite(side[k].Cl) || !double.IsFinite(side[k].Sweep)
                || Math.Abs(side[k].Eta - eta) > 1e-8) return false;
        }
        int outer = 0;
        while (outer + 1 < n - 1 && side[outer + 1].Eta > TipStation) outer++;
        TipPoint a = side[outer], b = side[outer + 1];
        double xa = Math.Log(1 - a.Eta), xb = Math.Log(1 - b.Eta), x = Math.Log(1 - TipStation);
        aiStar = a.AlphaI + (b.AlphaI - a.AlphaI) * (x - xa) / (xb - xa);
        uncertainty = c * Math.Abs(side[0].AlphaI - side[1].AlphaI) + 0.05;
        return double.IsFinite(aiStar) && double.IsFinite(uncertainty);
    }

    /// <summary>The run sentence: inside at all strips, or "&lt;n&gt; of &lt;m&gt;" with the exceeded parts.</summary>
    public static string JudgeRun(IReadOnlyList<StripVerdict> strips)
    {
        MethodEnvelope env = VlmStrip.Envelope;
        string bound = "(|α_eff − α_L0| ≤ " + Num(env.AlphaEffFromZeroLiftMaxDeg) + "°, Cl_local ≤ " + Num(env.ClLocalMax)
            + ", quarter-chord sweep ≤ " + Num(env.QuarterChordSweepMaxDeg) + "°)";
        int outside = 0;
        int judged = 0;
        var parts = new List<string>();
        foreach (StripVerdict strip in strips)
        {
            // A provisional tip is not inside and not outside. It does not change the run sentence's count.
            if (strip.Provisional || strip.State == StripVerdictState.AtBound) continue;
            judged++;
            if (strip.Inside) continue;
            outside++;
            foreach (string part in strip.Exceeded)
                if (!parts.Contains(part)) parts.Add(part);
        }
        // The operator has held run-level wording for an at-bound strip. Do not issue a false all-inside sentence.
        if (outside == 0 && strips.Any(strip => strip.State == StripVerdictState.AtBound)) return string.Empty;
        return outside == 0
            ? "Inside the method envelope " + bound + " at all " + judged + " strips"
            : "Outside the method envelope " + bound + " — " + outside + " of " + judged
                + " strips; exceeded: " + string.Join(", ", parts);
    }

    private static string Part(string name, double value, string valueUnit, double bound, string boundUnit, List<string> exceeded)
    {
        bool over = exceeded.Contains(name);
        return name + " " + Num(value) + valueUnit + (over ? " > " : " ≤ ") + Num(bound) + boundUnit;
    }

    private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
