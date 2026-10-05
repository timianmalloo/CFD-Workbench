using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The input range inside which the method's claims hold (DR-ANA-14; design §5.4).</summary>
public sealed record MethodEnvelope(double AlphaEffFromZeroLiftMaxDeg, double ClLocalMax, double QuarterChordSweepMaxDeg);

/// <summary>One strip's envelope verdict, derived on read from α_eff, Cl_local and sweep (DM7: not a stored flag).</summary>
public sealed record StripVerdict(bool Inside, IReadOnlyList<string> Exceeded, string Text);

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
    /// </summary>
    public static StripVerdict JudgeStrip(double alphaEffDeg, double alphaL0Deg, double clLocal, double sweepDeg)
    {
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

    /// <summary>The run sentence: inside at all strips, or "&lt;n&gt; of &lt;m&gt;" with the exceeded parts.</summary>
    public static string JudgeRun(IReadOnlyList<StripVerdict> strips)
    {
        MethodEnvelope env = VlmStrip.Envelope;
        string bound = "(|α_eff − α_L0| ≤ " + Num(env.AlphaEffFromZeroLiftMaxDeg) + "°, Cl_local ≤ " + Num(env.ClLocalMax)
            + ", quarter-chord sweep ≤ " + Num(env.QuarterChordSweepMaxDeg) + "°)";
        int outside = 0;
        var parts = new List<string>();
        foreach (StripVerdict strip in strips)
        {
            if (strip.Inside) continue;
            outside++;
            foreach (string part in strip.Exceeded)
                if (!parts.Contains(part)) parts.Add(part);
        }
        return outside == 0
            ? "Inside the method envelope " + bound + " at all " + strips.Count + " strips"
            : "Outside the method envelope " + bound + " — " + outside + " of " + strips.Count
                + " strips; exceeded: " + string.Join(", ", parts);
    }

    private static string Part(string name, double value, string valueUnit, double bound, string boundUnit, List<string> exceeded)
    {
        bool over = exceeded.Contains(name);
        return name + " " + Num(value) + valueUnit + (over ? " > " : " ≤ ") + Num(bound) + boundUnit;
    }

    private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
