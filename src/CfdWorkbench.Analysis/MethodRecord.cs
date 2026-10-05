using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The input range inside which the method's claims hold (DR-ANA-14; design §5.4).</summary>
public sealed record MethodEnvelope(double AlphaEffFromZeroLiftMaxDeg, double ClLocalMax, double QuarterChordSweepMaxDeg);

/// <summary>One strip's envelope verdict, derived on read from α_eff, Cl_local and sweep (DM7: not a stored flag).</summary>
public sealed record StripVerdict(bool Inside, IReadOnlyList<string> Exceeded, string Text)
{
    /// <summary>Outermost strip per half. The status is provisional; <see cref="Inside"/> is not an outside claim.</summary>
    public bool Provisional { get; init; }
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
    /// A provisional strip (Ruling 77(5)) reads <c>Labels.TipNotJudged</c>, not inside or outside. The sentence is held.
    /// </summary>
    public static StripVerdict JudgeStrip(double alphaEffDeg, double alphaL0Deg, double clLocal, double sweepDeg, bool provisional = false)
    {
        if (provisional)
            return new StripVerdict(false, Array.Empty<string>(), Labels.TipNotJudged) { Provisional = true };
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
        var verdicts = new StripVerdict[solution.Strips.Count];
        for (int i = 0; i < verdicts.Length; i++)
        {
            LatticeStrip strip = solution.Strips[i];
            verdicts[i] = JudgeStrip(alphaDeg + strip.TwistDeg - strip.InducedAngleDeg, alphaL0Deg, strip.ClLocal, strip.SweepDeg);
        }
        return verdicts;
    }

    /// <summary>
    /// Verdicts for every strip of a stored run, derived on read (DM7: nothing here is stored). α_eff and Cl_local are the
    /// strip's own stored facts. The sweep is the quarter-chord line of the run's accepted source across the strip's span
    /// edges, and α_L0 is the section estimator's zero-lift angle at the run's section stations, interpolated in η as the
    /// lattice interpolates its sections. Null when the run lacks span edges or section stations, or the source cannot be
    /// placed: a missing input is never a zero standing in for it. The source must be the one the run was solved on.
    /// </summary>
    // simplify: α_L0 is interpolated between the run's section stations (the lattice's own section model), not re-solved
    // per strip. Upgrade trigger: a station layout coarse enough that α_L0 is not near-linear between stations.
    public static IReadOnlyList<StripVerdict>? DeriveVerdicts(AnalysisRun run, byte[] source, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(source);
        if (run.Outcome is not RunOutcome.Completed || run.Strips.Count == 0 || run.Settings.SectionEtas is not { Count: > 0 } stationEtas
            || run.Strips.Any(strip => strip.YLow is null || strip.YHigh is null)) return null;
        try
        {
            var strips = run.Strips.OrderBy(strip => strip.J).ToArray();
            if (strips.Where((strip, i) => strip.J != i).Any()) return null;
            double halfSpan = strips.Max(strip => Math.Max(Math.Abs(strip.YLow!.Value), Math.Abs(strip.YHigh!.Value)));
            if (!(halfSpan > 0)) return null;
            double[] edgeEtas = strips.SelectMany(strip => new[] { Math.Abs(strip.YLow!.Value), Math.Abs(strip.YHigh!.Value) })
                .Select(y => Math.Min(1, y / halfSpan)).Distinct().Order().ToArray();
            var edges = Placement.Sections(source, edgeEtas, [0d, 1d], cancellation);
            double Quarter(double y)
            {
                double eta = Math.Min(1, Math.Abs(y) / halfSpan);
                int k = Array.FindIndex(edgeEtas, e => e == eta);
                StationFrame frame = edges[k].Frame;
                return frame.LeadingMeters + 0.25 * frame.ChordMeters;
            }
            double[] stations = stationEtas.Order().ToArray();
            double[] alphaL0 = stations.Select(eta => SectionEstimator.Estimate(source, eta, 0, 1e6, 200, cancellation).AlphaL0Deg).ToArray();
            double L0At(double eta)
            {
                if (eta <= stations[0]) return alphaL0[0];
                for (int i = 1; i < stations.Length; i++)
                    if (eta <= stations[i])
                        return alphaL0[i - 1] + (alphaL0[i] - alphaL0[i - 1]) * (eta - stations[i - 1]) / (stations[i] - stations[i - 1]);
                return alphaL0[^1];
            }
            var verdicts = new StripVerdict[strips.Length];
            for (int i = 0; i < strips.Length; i++)
            {
                StripLoad strip = strips[i];
                double dy = strip.YHigh!.Value - strip.YLow!.Value;
                double sweep = dy > 0 ? VortexLattice.ToDegrees(Math.Atan((Quarter(strip.YHigh!.Value) - Quarter(strip.YLow!.Value)) / dy)) : 0;
                bool tip = strip.Provisional && strip.ProvisionalReason == StripLoad.TipProvisionalReason;
                verdicts[i] = JudgeStrip(strip.AlphaEff, L0At(Math.Abs(strip.Eta)), strip.ClLocal, sweep, tip);
            }
            return verdicts;
        }
        catch (ContractError) { return null; }
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
            if (strip.Provisional) continue;
            judged++;
            if (strip.Inside) continue;
            outside++;
            foreach (string part in strip.Exceeded)
                if (!parts.Contains(part)) parts.Add(part);
        }
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
