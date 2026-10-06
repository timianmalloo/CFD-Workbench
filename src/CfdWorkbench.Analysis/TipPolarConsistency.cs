using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Inputs to the strip/polar lift consistency check, all from the selected run.</summary>
public sealed record PolarConsistencyInput(double Eta, double AlphaEffDeg, double ClLocal, double PolarCl,
    double PolarSlopePerDeg, double AlphaL0Deg, bool TipNotJudged, double Confidence);

public sealed record PolarConsistencyStrip(double Eta, double? Ratio, double? ClDelta, string Code);

public sealed record PolarConsistencyResult(double? EdgeEta, IReadOnlyList<PolarConsistencyStrip> Strips);

/// <summary>Ruling 88 D8: remeasure the tip consistency edge using the polar's own lift slope and zero lift.</summary>
public static class TipPolarConsistency
{
    /// <summary>Read the run's own profile revision and local Reynolds number for every judged strip.</summary>
    public static PolarConsistencyResult? Derive(AnalysisRun run, byte[] source, CancellationToken cancellation = default)
    {
        if (run.Settings.Polar is null || run.Strips.Count == 0) return null;
        double[] etas = run.Strips.Select(strip => Math.Abs(strip.Eta)).Distinct().ToArray();
        IReadOnlyDictionary<double, NeuralFoilSection> sections = RunPolarResolver.SectionsAt(source, etas, cancellation);
        var byHash = sections.Values.GroupBy(section => section.ProfileHash, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var polar = new NeuralFoilPolarSource(hash => byHash.GetValueOrDefault(hash));
        var inputs = new List<PolarConsistencyInput>();
        foreach (StripLoad strip in run.Strips.Where(strip => strip.Eta >= 0).OrderBy(strip => strip.Eta))
        {
            cancellation.ThrowIfCancellationRequested();
            NeuralFoilSection section = sections[Math.Abs(strip.Eta)];
            bool tip = strip.ProvisionalReason == StripLoad.TipProvisionalReason;
            PolarConsistencyInput? input = null;
            try
            {
                PolarResult? atZero = polar.Sample(section.ProfileHash, strip.ReLocal, 2, 0, run.Water, cancellation);
                PolarResult? atOne = polar.Sample(section.ProfileHash, strip.ReLocal, 2, 1, run.Water, cancellation);
                PolarResult? atStrip = polar.Sample(section.ProfileHash, strip.ReLocal, 2, strip.AlphaEff, run.Water, cancellation);
                if (atZero?.AvailabilityCode is null && atOne?.AvailabilityCode is null &&
                    atStrip?.AvailabilityCode is null && atZero?.Sample.Cl is { } cl0 &&
                    atOne?.Sample.Cl is { } cl1 && atStrip?.Sample.Cl is { } cl &&
                    Math.Abs(cl1 - cl0) >= 1e-12)
                {
                    double slope = cl1 - cl0;
                    input = new(strip.Eta, strip.AlphaEff, strip.ClLocal, cl, slope, -cl0 / slope,
                        tip, atStrip.Sample.Confidence ?? 0);
                }
            }
            catch (ContractError error) when (error.Code.StartsWith("ANA-POLAR-", StringComparison.Ordinal)) { }
            inputs.Add(input ?? new(strip.Eta, strip.AlphaEff, strip.ClLocal, double.NaN, double.NaN,
                double.NaN, tip, double.NaN));
        }
        return inputs.Count == 0 ? null : Evaluate(inputs);
    }

    public static PolarConsistencyResult Evaluate(IReadOnlyList<PolarConsistencyInput> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ordered = input.OrderBy(strip => Math.Abs(strip.Eta)).ToArray();
        double? root = null;
        var ratio = new double?[ordered.Length];
        for (int i = 0; i < ordered.Length; i++)
        {
            PolarConsistencyInput strip = ordered[i];
            double denominator = strip.PolarSlopePerDeg * (strip.AlphaEffDeg - strip.AlphaL0Deg);
            if (!double.IsFinite(denominator) || Math.Abs(denominator) < 1e-12) continue;
            double c = strip.ClLocal / denominator;
            if (!double.IsFinite(c)) continue;
            if (root is null && !strip.TipNotJudged && Math.Abs(c) > 1e-12) root = c;
            if (root is not null) ratio[i] = c / root.Value;
        }
        double? edge = null;
        for (int i = 0; i < ordered.Length; i++)
            if (ratio[i] is < 0.9) { edge = Math.Abs(ordered[i].Eta); break; }
        var result = new PolarConsistencyStrip[ordered.Length];
        for (int i = 0; i < ordered.Length; i++)
        {
            PolarConsistencyInput strip = ordered[i];
            string code = strip.TipNotJudged ? "ANA-TIP-PROVISIONAL" :
                edge is { } first && Math.Abs(strip.Eta) >= first ? "ANA-POLAR-CONSISTENCY-EXEMPT" :
                ratio[i] is null ? "ANA-POLAR-CONSISTENCY-UNAVAILABLE" :
                "ANA-POLAR-CONSISTENCY-JUDGED";
            double? delta = double.IsFinite(strip.PolarCl) ? strip.ClLocal - strip.PolarCl : null;
            result[i] = new(strip.Eta, ratio[i], delta, code);
        }
        return new(edge, result);
    }
}
