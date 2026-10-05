using CfdWorkbench.Core;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace CfdWorkbench.Analysis.NeuralFoil;

/// <summary>A polar result with the fit residual and the explicit envelope verdict for every section.</summary>
public sealed record NeuralFoilEvaluation(NeuralFoilPrediction? Prediction, double CstResidualRms,
    double CstResidualMax, IReadOnlyList<string> OutsideBracketReasons, string? Reason, string? ConfidenceWarning = null)
{
    public bool Computable => Prediction is not null && Reason is null;

    /// <summary>NeuralFoil's analysis_confidence is below the advisory flag threshold; the point is still computed.</summary>
    public bool LowConfidence => ConfidenceWarning is not null;

    /// <summary>True when the point is computed but is not inside the XFOIL-validated bracket.</summary>
    public bool OutsideValidatedBracket => OutsideBracketReasons.Count > 0;
}

/// <summary>
/// The NeuralFoil source. The resolver must return the accepted profile revision named by the requested hash;
/// it must not map an edited profile to its catalog ancestor.
/// </summary>
public sealed class NeuralFoilPolarSource(Func<string, NeuralFoilSection?> resolveSection) : IPolarSource
{
    private static readonly Meter Meter = new("CfdWorkbench.Analysis.NeuralFoil", "0.3.2");
    private static readonly ActivitySource ActivitySource = new("CfdWorkbench.Analysis.NeuralFoil", "0.3.2");
    private static readonly Counter<long> Calls = Meter.CreateCounter<long>("neuralfoil.evaluate.calls");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("neuralfoil.evaluate.duration_ms", "ms");
    // SPIKE-ANA-1/verdict.md: 78 XFOIL points for NACA 0012, alpha -6..6, Re 2e5..1e6, Ncrit 2/4/9.
    // Fidelity (not XFOIL accuracy) also covers NACA 2412/4412. Its largest fit max was 3.58e-4 c.
    public const double AlphaMinDeg = -6;
    public const double AlphaMaxDeg = 6;
    public const double ReynoldsMin = 200000;
    public const double ReynoldsMax = 1000000;
    public const double NcritMin = 2;
    public const double NcritMax = 9;
    public const double MaxCstResidual = 0.00036;
    // Network training range, the hard limit. Source: docs/knowledge/hydrofoil-workbench/07-low-order-hydrodynamics.md:38,
    // citing the NeuralFoil paper (Sharpe and Hansman, arXiv 2503.16323): alpha -27.9..+28.6 degrees, Re about 1e2..1e10,
    // Ncrit uniformly sampled in [0, 18]. Not re-read from upstream in this track; the installed package does not state it.
    public const double TrainingAlphaMinDeg = -27.9;
    public const double TrainingAlphaMaxDeg = 28.6;
    public const double TrainingReynoldsMin = 1e2;
    public const double TrainingReynoldsMax = 1e10;
    public const double TrainingNcritMin = 0;
    public const double TrainingNcritMax = 18;
    // analysis_confidence is NeuralFoil's learned convergence and in-distribution indicator. It is advisory: a point below
    // this threshold is computed and flagged, never refused. The threshold only places the flag; the five spike cases
    // are all above 0.95, so no evidence ties it to an error size.
    public const double LowConfidenceBelow = 0.5;

    public string? UnavailableReason => null;
    public static RunMethod Method { get; } = new(NeuralFoilNetwork.MethodId, NeuralFoilNetwork.MethodVersion, 0);

    public NeuralFoilEvaluation Evaluate(NeuralFoilSection section, double alphaDeg, double reynolds,
        double ncrit, CancellationToken cancellation)
    {
        long started = Stopwatch.GetTimestamp();
        using Activity? activity = ActivitySource.StartActivity("neuralfoil.evaluate");
        string outcome = "failed";
        try
        {
            cancellation.ThrowIfCancellationRequested();
            CstFitResult fit = CstFit.Fit(section);
            IReadOnlyList<string> outside = BracketFlags(Naca0012Reference.Matches(fit.Parameters), alphaDeg, reynolds, ncrit);
            string? reason = RefusalReason(fit.MaxResidual, alphaDeg, reynolds, ncrit);
            if (reason is not null)
            {
                outcome = "noncomputable";
                return new(null, fit.RmsResidual, fit.MaxResidual, outside, reason);
            }
            NeuralFoilPrediction prediction = NeuralFoilNetwork.FromEmbedded().Predict(fit.Parameters, alphaDeg, reynolds, ncrit);
            outcome = "computed";
            return new(prediction, fit.RmsResidual, fit.MaxResidual, outside, null,
                ConfidenceWarning(prediction.AnalysisConfidence));
        }
        finally
        {
            activity?.SetTag("outcome", outcome);
            Calls.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("outcome", outcome));
        }
    }

    public static string? ConfidenceWarning(double confidence) =>
        !double.IsFinite(confidence) || confidence < LowConfidenceBelow
            ? "analysis_confidence is below 0.5. NeuralFoil's confidence is an advisory convergence and in-distribution indicator, not an accuracy statement."
            : null;

    public PolarSample? Sample(string profileHash, double reynolds, double ncrit, double alphaDeg,
        WaterRecord water, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(water);
        NeuralFoilSection? section = resolveSection(profileHash);
        if (section is null)
            throw new ContractError("ANA-POLAR-PROFILE-MISSING", "The requested profile revision has no section geometry.");
        if (section.ProfileHash != profileHash)
            throw new ContractError("ANA-POLAR-PROFILE-HASH", "The polar resolver returned another profile revision.");
        NeuralFoilEvaluation result = Evaluate(section, alphaDeg, reynolds, ncrit, cancellation);
        if (!result.Computable)
            throw new ContractError("ANA-POLAR-NONCOMPUTABLE", result.Reason ?? "The polar point is non-computable.");
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

    // Hard refusals: outside the network's training range, or a CST fit too poor to represent the section.
    private static string? RefusalReason(double residual, double alphaDeg, double reynolds, double ncrit)
    {
        if (!double.IsFinite(alphaDeg) || alphaDeg < TrainingAlphaMinDeg || alphaDeg > TrainingAlphaMaxDeg)
            return "alpha outside the network training range -27.9..+28.6 degrees";
        if (!double.IsFinite(reynolds) || reynolds < TrainingReynoldsMin || reynolds > TrainingReynoldsMax)
            return "Re outside the network training range 1e2..1e10";
        if (!double.IsFinite(ncrit) || ncrit < TrainingNcritMin || ncrit > TrainingNcritMax)
            return "Ncrit outside the network training range 0..18";
        if (residual > MaxCstResidual)
            return "CST residual above 0.00036 c";
        return null;
    }

    // Flags, never refusals: the point is computed but the XFOIL validation does not cover it (SPIKE-ANA-1 verdict).
    private static List<string> BracketFlags(bool validatedSection, double alphaDeg, double reynolds, double ncrit)
    {
        var flags = new List<string>();
        if (alphaDeg < AlphaMinDeg || alphaDeg > AlphaMaxDeg) flags.Add("alpha outside the validated -6..+6 degrees");
        if (reynolds < ReynoldsMin || reynolds > ReynoldsMax) flags.Add("Re outside the validated 200000..1000000");
        if (ncrit is not (2 or 4 or 9)) flags.Add("Ncrit is not one of the validated 2, 4, 9");
        if (!validatedSection) flags.Add("section geometry is not the validated NACA 0012");
        return flags;
    }
}
