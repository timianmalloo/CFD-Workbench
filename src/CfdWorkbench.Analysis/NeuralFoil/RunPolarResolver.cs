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
        SectionSample placed = PanelMethod.SampleSection(source, eta, PanelMethod.DefaultPanelCount, cancellation);
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
