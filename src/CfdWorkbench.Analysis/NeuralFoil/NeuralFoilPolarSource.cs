using CfdWorkbench.Core;
using System.Text;

namespace CfdWorkbench.Analysis.NeuralFoil;

/// <summary>A polar result with the fit residual and the explicit envelope verdict for every section.</summary>
public sealed record NeuralFoilEvaluation(NeuralFoilPrediction? Prediction, double CstResidualRms,
    double CstResidualMax, bool OutsideValidatedBracket, string? Reason)
{
    public bool Computable => Prediction is not null && Reason is null;
}

/// <summary>
/// The NeuralFoil source. The resolver must return the accepted profile revision named by the requested hash;
/// it must not map an edited profile to its catalog ancestor.
/// </summary>
public sealed class NeuralFoilPolarSource(Func<string, NeuralFoilSection?> resolveSection) : IPolarSource
{
    // SPIKE-ANA-1/verdict.md: 78 XFOIL points for NACA 0012, alpha -6..6, Re 2e5..1e6, Ncrit 2/4/9.
    // Fidelity (not XFOIL accuracy) also covers NACA 2412/4412. Its largest fit max was 3.58e-4 c.
    public const double AlphaMinDeg = -6;
    public const double AlphaMaxDeg = 6;
    public const double ReynoldsMin = 200000;
    public const double ReynoldsMax = 1000000;
    public const double NcritMin = 2;
    public const double NcritMax = 9;
    public const double MaxCstResidual = 0.00036;
    // assume: 0.5 is a conservative advisory confidence floor; the spike's five fixture cases are >0.95.
    // Confirm by a wider XFOIL comparison before making the floor an accuracy claim; if false, valid points may be refused.
    public const double ConfidenceFloor = 0.5;

    public string? UnavailableReason => null;
    public static RunMethod Method { get; } = new(NeuralFoilNetwork.MethodId, NeuralFoilNetwork.MethodVersion, 0);

    public NeuralFoilEvaluation Evaluate(NeuralFoilSection section, double alphaDeg, double reynolds,
        double ncrit, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        CstFitResult fit = CstFit.Fit(section);
        string? reason = EnvelopeReason(section.Family, fit.MaxResidual, alphaDeg, reynolds, ncrit);
        bool outside = section.Family != "naca0012" || ncrit is not (2 or 4 or 9);
        if (reason is not null) return new(null, fit.RmsResidual, fit.MaxResidual, outside, reason);
        NeuralFoilPrediction prediction = NeuralFoilNetwork.FromEmbedded().Predict(fit.Parameters, alphaDeg, reynolds, ncrit);
        if (prediction.AnalysisConfidence < ConfidenceFloor)
            return new(prediction, fit.RmsResidual, fit.MaxResidual, outside,
                "analysis_confidence below the advisory floor 0.5");
        return new(prediction, fit.RmsResidual, fit.MaxResidual, outside, null);
    }

    public PolarSample? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg,
        WaterRecord water, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(water);
        NeuralFoilSection? section = resolveSection(profileHash);
        if (section is null) return null;
        if (section.ProfileHash != profileHash)
            throw new ContractError("ANA-POLAR-PROFILE-HASH", "The polar resolver returned another profile revision.");
        NeuralFoilEvaluation result = Evaluate(section, alphaDeg, reynolds, ncrit, cancellation);
        if (!result.Computable) return null;
        NeuralFoilPrediction prediction = result.Prediction!;
        string waterHash = Identity.Blake3(Encoding.UTF8.GetBytes(Jcs.Write(new Dictionary<string, object?>
        {
            ["temperatureC"] = water.TemperatureC, ["salinityGPerKg"] = water.SalinityGPerKg,
            ["rho"] = water.Rho, ["nu"] = water.Nu, ["pv"] = water.Pv,
            ["source"] = water.Source, ["tableHash"] = water.TableHash
        })));
        return new PolarSample(profileHash, Method.Id, Method.Version,
            reynolds, ncrit, "clean", alphaDeg, waterHash, prediction.Cl, prediction.Cd,
            prediction.Cm, prediction.XtrUpper, prediction.XtrLower, null, null, prediction.AnalysisConfidence, true);
    }

    private static string? EnvelopeReason(string family, double residual, double alphaDeg, double reynolds, double ncrit)
    {
        if (!double.IsFinite(alphaDeg) || alphaDeg < AlphaMinDeg || alphaDeg > AlphaMaxDeg)
            return "alpha outside -6..+6 degrees";
        if (!double.IsFinite(reynolds) || reynolds < ReynoldsMin || reynolds > ReynoldsMax)
            return "Re outside 200000..1000000";
        if (!double.IsFinite(ncrit) || ncrit < NcritMin || ncrit > NcritMax)
            return "Ncrit outside 2..9";
        if (family is not ("naca0012" or "naca2412" or "naca4412"))
            return "section family outside spike fidelity grid";
        if (residual > MaxCstResidual)
            return "CST residual above 0.00036 c";
        return null;
    }
}
