using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.NeuralFoil;

/// <summary>Builds a polar section from the accepted source held for this run, including edited and blended profiles.</summary>
public static class RunPolarResolver
{
    public static NeuralFoilSection SectionAt(AuthoringSession session, AnalysisRun run, double eta,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(run);
        byte[] source = session.AcceptedSourceOf(run.Inputs.AcceptedId)
            ?? throw new ContractError("ANA-POLAR-REVISION-MISSING", "The run's accepted source revision is unavailable.");
        SourceParse parsed = FoilSource.Parse(source);
        if (!parsed.IsParsed || parsed.SurfaceHash != run.Inputs.SurfaceHash ||
            !parsed.Authored().Assignments.Select(a => a.ProfileIdentity).SequenceEqual(run.Inputs.ProfileHashes))
            throw new ContractError("ANA-POLAR-REVISION-MISMATCH", "The accepted source does not match the run's manifest.");
        return SectionsAt(source, [eta], cancellation)[eta];
    }

    /// <summary>All strip sections from one accepted source, sampled on one cosine grid after one parse.</summary>
    public static IReadOnlyDictionary<double, NeuralFoilSection> SectionsAt(byte[] source, IReadOnlyList<double> etas,
        CancellationToken cancellation = default)
    {
        IReadOnlyList<SectionSample> placed = PanelMethod.SampleSections(source, etas, PanelMethod.DefaultPanelCount, cancellation);
        var result = new Dictionary<double, NeuralFoilSection>();
        for (int i = 0; i < placed.Count; i++) result.Add(etas[i], FromPlaced(placed[i]));
        return result;
    }

    private static NeuralFoilSection FromPlaced(SectionSample placed)
    {
        double[] upper = placed.Camber.Zip(placed.Thickness, (camber, thickness) => camber + thickness / 2).ToArray();
        double[] lower = placed.Camber.Zip(placed.Thickness, (camber, thickness) => camber - thickness / 2).ToArray();
        string hash = Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(new Dictionary<string, object?>
        {
            ["x"] = placed.X.ToArray(), ["upper"] = upper, ["lower"] = lower
        })));
        return new NeuralFoilSection(hash, "derived from accepted revision", placed.X, upper, lower);
    }

    public static PolarResult DeriveStored(AuthoringSession session, AnalysisRun run, double eta, PolarSample sample,
        CancellationToken cancellation = default) =>
        NeuralFoilPolarSource.DeriveStored(sample, SectionAt(session, run, eta, cancellation));
}
