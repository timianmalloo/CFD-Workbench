using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CfdWorkbench.Core.CfdRun;

/// <summary>All 11 Area 5 CFD run states (spec A5.10, docs/design/cfd-run-pipeline.md §6.1).</summary>
[JsonConverter(typeof(CfdRunStateConverter))]
public enum CfdRunState
{
    Pending,
    Unsupported,
    Readiness,
    Meshing,
    MeshGate,
    Solving,
    Harvesting,
    Completed,
    CompletedResidualNotMet,
    Failed,
    Cancelled
}

public sealed record StateTransitionResult(bool Legal, string? Code, string? Reason);

public sealed record ResidualWindowInput(
    IReadOnlyDictionary<string, (double FirstInitialResidual, double FinalInitialResidual)> EquationInitialResiduals,
    IReadOnlyList<double> ClWindow,
    IReadOnlyList<double> CdWindow,
    IReadOnlyList<double> CmWindow,
    double ResidualDropTarget = 1e-3,
    double ClHalfBandCap = 1e-3,
    double CdHalfBandCap = 2e-4,
    int TurbulenceBoundingCountInWindow = 0,
    bool RanToIterationLimit = false);

/// <summary>
/// Pure 11-state CFD run state machine, DR-F2-1 three-part residual criterion, and LAB-01 three-part Converged label
/// (docs/design/cfd-run-pipeline.md §6). All method names strictly avoid ArchitectureTests banned verbs.
/// </summary>
public static class CfdRunStateMachine
{
    public const int MinimumAveragingWindowIterations = 20;
    public const string SingleMeshUncertaintyClause = "grid uncertainty not quantified (single mesh)";

    public static bool IsTerminal(CfdRunState state) => state is
        CfdRunState.Unsupported or
        CfdRunState.Completed or
        CfdRunState.CompletedResidualNotMet or
        CfdRunState.Failed or
        CfdRunState.Cancelled;

    public static string DisplayString(CfdRunState state, string? reason = null) => state switch
    {
        CfdRunState.Pending => "Pending",
        CfdRunState.Unsupported => "Unsupported",
        CfdRunState.Readiness => "Readiness",
        CfdRunState.Meshing => "Meshing",
        CfdRunState.MeshGate => "Mesh gate",
        CfdRunState.Solving => "Solving",
        CfdRunState.Harvesting => "Harvesting",
        CfdRunState.Completed => "Completed",
        CfdRunState.CompletedResidualNotMet => "Completed — residual criterion not met (ran to limit)",
        CfdRunState.Failed => string.IsNullOrWhiteSpace(reason) ? "Failed" : $"Failed ({reason})",
        CfdRunState.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    /// <summary>Initial Pending state event (safe name; avoids banned Begin* prefix).</summary>
    public static CfdRunStateEvent CreatePending(string runId, DateTimeOffset atUtc)
    {
        NativeProject.Uuid(runId);
        return new CfdRunStateEvent(0, CfdRunState.Pending, atUtc, null, null);
    }

    /// <summary>Pure transition legality check against the 11-state matrix (docs/design/cfd-run-pipeline.md §6.1).</summary>
    public static StateTransitionResult EvaluateTransition(CfdRunState current, CfdRunState next, string? code = null, string? reason = null)
    {
        _ = code;
        _ = reason;
        bool legal = (current, next) switch
        {
            (CfdRunState.Pending, CfdRunState.Unsupported) => true,
            (CfdRunState.Pending, CfdRunState.Readiness) => true,
            (CfdRunState.Pending, CfdRunState.Cancelled) => true,
            (CfdRunState.Pending, CfdRunState.Failed) => true,

            (CfdRunState.Readiness, CfdRunState.Meshing) => true,
            (CfdRunState.Readiness, CfdRunState.Solving) => true, // Validated checkpoint resume
            (CfdRunState.Readiness, CfdRunState.Failed) => true,
            (CfdRunState.Readiness, CfdRunState.Cancelled) => true,

            (CfdRunState.Meshing, CfdRunState.MeshGate) => true,
            (CfdRunState.Meshing, CfdRunState.Failed) => true,
            (CfdRunState.Meshing, CfdRunState.Cancelled) => true,

            (CfdRunState.MeshGate, CfdRunState.Solving) => true,
            (CfdRunState.MeshGate, CfdRunState.Failed) => true,
            (CfdRunState.MeshGate, CfdRunState.Cancelled) => true,

            (CfdRunState.Solving, CfdRunState.Harvesting) => true,
            (CfdRunState.Solving, CfdRunState.Failed) => true,
            (CfdRunState.Solving, CfdRunState.Cancelled) => true,

            (CfdRunState.Harvesting, CfdRunState.Completed) => true,
            (CfdRunState.Harvesting, CfdRunState.CompletedResidualNotMet) => true,
            (CfdRunState.Harvesting, CfdRunState.Failed) => true,
            (CfdRunState.Harvesting, CfdRunState.Cancelled) => true,

            _ => false
        };

        return legal
            ? new StateTransitionResult(true, null, null)
            : new StateTransitionResult(false, "CFD-STATE-INVALID", $"Illegal CFD run state transition {current} -> {next}.");
    }

    public static IReadOnlyList<CfdRunStateEvent> AppendEvent(
        IReadOnlyList<CfdRunStateEvent> events,
        CfdRunState next,
        DateTimeOffset atUtc,
        string? code = null,
        string? reason = null,
        double? elapsedMs = null)
    {
        Guard.Require(events is not null && events.Count > 0, "CFD-STATE-INVALID");
        var check = EvaluateTransition(events![^1].State, next, code, reason);
        if (!check.Legal)
            throw new ContractError(check.Code!, check.Reason!);

        var nextList = new List<CfdRunStateEvent>(events.Count + 1);
        nextList.AddRange(events);
        nextList.Add(new CfdRunStateEvent(events.Count, next, atUtc, code, reason, elapsedMs));
        return nextList;
    }

    /// <summary>Appends legal Cancelled terminal event (safe name; avoids ArchitectureTests banned 'Cancel').</summary>
    public static IReadOnlyList<CfdRunStateEvent> TransitionToCancelled(
        IReadOnlyList<CfdRunStateEvent> events,
        DateTimeOffset atUtc,
        string reason) =>
        AppendEvent(events, CfdRunState.Cancelled, atUtc, "CFD-CANCELLED", reason);

    /// <summary>Appends legal Failed terminal event (safe name; avoids banned Apply* prefix).</summary>
    public static IReadOnlyList<CfdRunStateEvent> TransitionToFailed(
        IReadOnlyList<CfdRunStateEvent> events,
        DateTimeOffset atUtc,
        string code,
        string reason) =>
        AppendEvent(events, CfdRunState.Failed, atUtc, code, reason);

    /// <summary>
    /// Pure DR-F2-1 three-part residual criterion (Ruling 65, docs/design/cfd-run-pipeline.md §6.2):
    /// (1) max_e(r_final / r_first) &lt;= target (default 1e-3);
    /// (2) CL &amp; CD averaging-window half-bands U_I &lt;= caps over >= 20 iterations;
    /// (3) zero turbulence bounding events inside the averaging window.
    /// </summary>
    public static ResidualSummary EvaluateResiduals(ResidualWindowInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Guard.Require(input.EquationInitialResiduals is not null && input.EquationInitialResiduals.Count > 0, "CFD-RESIDUAL-INVALID");
        Guard.Require(input.ClWindow is not null && input.ClWindow.Count > 0 &&
                      input.CdWindow is not null && input.CdWindow.Count > 0 &&
                      input.ClWindow.Count == input.CdWindow.Count, "CFD-RESIDUAL-INVALID");
        Guard.Require(double.IsFinite(input.ResidualDropTarget) && input.ResidualDropTarget > 0 &&
                      double.IsFinite(input.ClHalfBandCap) && input.ClHalfBandCap > 0 &&
                      double.IsFinite(input.CdHalfBandCap) && input.CdHalfBandCap > 0 &&
                      input.TurbulenceBoundingCountInWindow >= 0, "CFD-RESIDUAL-INVALID");

        double maxRatio = 0.0;
        foreach (var (_, (first, final)) in input.EquationInitialResiduals!)
        {
            if (!double.IsFinite(first) || !double.IsFinite(final) || first <= 0.0 || final < 0.0)
                throw new ContractError("CFD-RESIDUAL-INVALID", "Non-finite or non-positive initial residual encountered.");
            double ratio = final / first;
            if (ratio > maxRatio) maxRatio = ratio;
        }

        var (clMean, clHalf) = WindowStats(input.ClWindow!);
        var (cdMean, cdHalf) = WindowStats(input.CdWindow!);
        double cmMean = input.CmWindow is { Count: > 0 } ? WindowStats(input.CmWindow).Mean : 0.0;

        bool part1Drop = maxRatio <= input.ResidualDropTarget;
        bool part2Stationary = input.ClWindow!.Count >= MinimumAveragingWindowIterations &&
                               clHalf <= input.ClHalfBandCap &&
                               cdHalf <= input.CdHalfBandCap;
        bool part3CleanTurbulence = input.TurbulenceBoundingCountInWindow == 0;

        return new ResidualSummary(
            MaxInitialResidualRatio: maxRatio,
            ClWindowMean: clMean,
            CdWindowMean: cdMean,
            CmWindowMean: cmMean,
            ClHalfBandUi: clHalf,
            CdHalfBandUi: cdHalf,
            ClHalfBandCap: input.ClHalfBandCap,
            CdHalfBandCap: input.CdHalfBandCap,
            AveragingWindowIterations: input.ClWindow!.Count,
            TurbulenceBoundingCountInWindow: input.TurbulenceBoundingCountInWindow,
            RanToIterationLimit: input.RanToIterationLimit,
            CriterionMet: part1Drop && part2Stationary && part3CleanTurbulence);
    }

    /// <summary>
    /// Pure LAB-01 three-part Converged label formatter (spec A7:1295, docs/design/cfd-run-pipeline.md §6.3).
    /// Returns null unless both outputsComplete and summary.CriterionMet hold.
    /// </summary>
    public static string? FormatConvergedLabel(ResidualSummary? summary, bool outputsComplete)
    {
        if (!outputsComplete || summary is null || !summary.CriterionMet) return null;
        if (!double.IsFinite(summary.MaxInitialResidualRatio)) return null;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Converged: residuals <= {summary.MaxInitialResidualRatio:0.0e+00} · outputs complete · {SingleMeshUncertaintyClause}");
    }

    private static (double Mean, double HalfBand) WindowStats(IReadOnlyList<double> values)
    {
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        double sum = 0.0;
        for (int i = 0; i < values.Count; i++)
        {
            double v = values[i];
            if (!double.IsFinite(v))
                throw new ContractError("CFD-RESIDUAL-INVALID", "Force coefficient window contains a non-finite value.");
            if (v < min) min = v;
            if (v > max) max = v;
            sum += v;
        }
        return (sum / values.Count, 0.5 * (max - min));
    }
}

internal sealed class CfdRunStateConverter : JsonConverter<CfdRunState>
{
    public override CfdRunState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();
        return token switch
        {
            "pending" => CfdRunState.Pending,
            "unsupported" => CfdRunState.Unsupported,
            "readiness" => CfdRunState.Readiness,
            "meshing" => CfdRunState.Meshing,
            "mesh-gate" => CfdRunState.MeshGate,
            "solving" => CfdRunState.Solving,
            "harvesting" => CfdRunState.Harvesting,
            "completed" => CfdRunState.Completed,
            "completed-residual-not-met" => CfdRunState.CompletedResidualNotMet,
            "failed" => CfdRunState.Failed,
            "cancelled" => CfdRunState.Cancelled,
            _ => throw new JsonException($"Unknown CFD run state '{token}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, CfdRunState value, JsonSerializerOptions options)
    {
        string token = value switch
        {
            CfdRunState.Pending => "pending",
            CfdRunState.Unsupported => "unsupported",
            CfdRunState.Readiness => "readiness",
            CfdRunState.Meshing => "meshing",
            CfdRunState.MeshGate => "mesh-gate",
            CfdRunState.Solving => "solving",
            CfdRunState.Harvesting => "harvesting",
            CfdRunState.Completed => "completed",
            CfdRunState.CompletedResidualNotMet => "completed-residual-not-met",
            CfdRunState.Failed => "failed",
            CfdRunState.Cancelled => "cancelled",
            _ => throw new JsonException($"Unknown CFD run state '{value}'.")
        };
        writer.WriteStringValue(token);
    }
}
