using System.Globalization;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Analysis.CfdRun;

public sealed record CfdRunSummaryView(
    string RunId,
    string RunKey,
    string RunKey12,
    string? MeshHash,
    CfdRunState State,
    string StateDisplay,
    CfdRecordIntegrity Integrity,
    string? IntegrityWarning,
    string? ConvergedLabel,
    bool? MeshGatePassed,
    bool MeshGateIsScreenOnly,
    string MeshGateLabel,
    ResidualSummary? ResidualSummary,
    CfdRunResources Resources,
    string BackendLabel,
    bool CanResume,
    double AlphaDeg,
    double SpeedMs);

public sealed record FailureExplanation(
    string RunId,
    string FailureCode,
    string Summary,
    string SuggestedNextStep,
    IReadOnlyList<int> CitedLineNumbers);

public sealed record CaseDiffProposal(
    string SourceRunId,
    int? MaxIterations = null,
    int? SurfaceLayers = null,
    double? ExpansionRatio = null,
    double? ClHalfBandCap = null,
    double? CdHalfBandCap = null,
    string? LogNote = null);

public sealed record CaseDiffValidationResult(
    bool Valid,
    bool CreatesNewDraftVersion,
    bool AutoLaunchAllowed,
    string Summary);

/// <summary>
/// Read-only UI projection for Wave 10D RunWorkspaceView and offline key-free AI-01/AI-11 validators
/// (docs/design/cfd-run-pipeline.md §7.3). Enforces LAB-01 three-part Converged labels and AI-05 data-only logs.
/// </summary>
public static class CfdRunProjection
{
    public static IReadOnlyList<CfdRunSummaryView> ProjectLedger(CfdLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var views = new List<CfdRunSummaryView>(ledger.Runs.Count);

        foreach (var stored in ledger.Runs)
        {
            var run = stored.Run;
            bool intact = stored.Integrity == CfdRecordIntegrity.Intact;
            var lastEvent = run.Events[^1];
            string stateDisplay = CfdRunStateMachine.DisplayString(run.CurrentState, lastEvent.Reason);
            bool outputsComplete = intact && run.CurrentState is CfdRunState.Completed or CfdRunState.CompletedResidualNotMet;
            string? convergedLabel = intact
                ? CfdRunStateMachine.FormatConvergedLabel(run.ResidualSummary, outputsComplete)
                : null;

            string meshGateLabel = run.MeshGate is null
                ? "Not evaluated"
                : run.MeshGate.IsScreenOnly
                    ? MeshGateEvaluator.ScreenOnlyLabel
                    : run.MeshGate.Passed
                        ? $"Passed ({run.MeshGate.Branch})"
                        : $"Failed ({run.MeshGate.Failures.Count} check(s))";

            string backendLabel = $"{run.BackendPin.Backend} {run.BackendPin.Version} ({run.BackendPin.Substrate})"
                + (run.BackendPin.VerifiedInstall ? "" : " · unverified install");

            views.Add(new CfdRunSummaryView(
                RunId: run.RunId,
                RunKey: run.RunKey,
                RunKey12: run.RunKey[..12],
                MeshHash: run.MeshHash,
                State: run.CurrentState,
                StateDisplay: stateDisplay,
                Integrity: stored.Integrity,
                IntegrityWarning: intact ? null : Labels.CfdPayloadFailed,
                ConvergedLabel: convergedLabel,
                MeshGatePassed: run.MeshGate?.Passed,
                MeshGateIsScreenOnly: run.MeshGate?.IsScreenOnly ?? false,
                MeshGateLabel: meshGateLabel,
                ResidualSummary: intact ? run.ResidualSummary : null,
                Resources: run.Resources,
                BackendLabel: backendLabel,
                CanResume: intact && run.Checkpoint?.HarvesterValidated == true && run.CurrentState != CfdRunState.Completed,
                AlphaDeg: run.Op.AlphaDeg,
                SpeedMs: run.Op.Speed));
        }

        return views;
    }

    /// <summary>
    /// Deterministic offline key-free failure explainer (AI-01): maps failure code, MeshGate failures, and
    /// cited line numbers from a redacted log excerpt with zero network calls.
    /// </summary>
    public static FailureExplanation ExplainFailure(CfdRunRecord run, string? redactedLogExcerpt = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        var last = run.Events[^1];
        string code = last.Code ?? run.CurrentState.ToString();

        var cited = new List<int>();
        if (!string.IsNullOrEmpty(redactedLogExcerpt))
        {
            string[] lines = redactedLogExcerpt.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (l.Contains("FATAL", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("REFUSED", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("bounding", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("missing", StringComparison.OrdinalIgnoreCase))
                {
                    cited.Add(i + 1);
                }
            }
        }

        string nextStep = code switch
        {
            "MESH-GATE-FAIL" => "Tighten mesh settings (surface layers, non-orthogonality, or refinement level) and queue a new Draft version.",
            "CFD-NO-OUTPUTS" => "Inspect solver log excerpt for early abort and verify function-object output paths before retrying.",
            "CFD-HARVEST-COLUMNS" => "Verify solver postProcessing output format matches pinned backend version before retrying.",
            "CFD-CHECKPOINT-INVALID" => "Checkpoint is incomplete or mismatched; use Retry sample for a clean start.",
            "CFD-UNSUPPORTED-PHYSICS" => "Switch to a backend supporting the requested physics or disable multiphase flags.",
            "smoke.banner" => "Re-run Guided Solver Setup to verify M1 Disallowing banner before launching CFD.",
            _ => "Review the cited log lines and use Retry sample or Preview case diff."
        };

        string detail = run.MeshGate is { Passed: false } mg && mg.Failures.Count > 0
            ? string.Join("; ", mg.Failures.Select(f => f.Message))
            : last.Reason ?? "Run did not complete.";

        string summary = string.Create(CultureInfo.InvariantCulture,
            $"Run {run.RunKey[..12]} stopped with {code}: {detail}");

        return new FailureExplanation(run.RunId, code, summary, nextStep, cited);
    }

    /// <summary>
    /// Offline typed CaseDiff validator (AI-11, AI-05): refuses mutating a Queued experiment (CFD-DIFF-IMMUTABLE),
    /// checks all numeric overrides are finite and in bounds, and guarantees log text can never auto-launch a run.
    /// </summary>
    public static CaseDiffValidationResult ValidateCaseDiffProposal(CaseDiffProposal proposal, bool experimentIsQueued)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (experimentIsQueued)
            throw new ContractError("CFD-DIFF-IMMUTABLE", "Queued experiments are immutable; create a new Draft version to apply a case diff.");

        if (proposal.MaxIterations is { } iter && (iter < 10 || iter > 100_000))
            throw new ContractError("CFD-CASE-NONFINITE", "MaxIterations override is outside allowed bounds [10, 100000].");
        if (proposal.SurfaceLayers is { } layers && (layers < 10 || layers > 60))
            throw new ContractError("CFD-CASE-NONFINITE", "SurfaceLayers override is outside allowed bounds [10, 60].");
        if (proposal.ExpansionRatio is { } ratio && (!double.IsFinite(ratio) || ratio < 1.0 || ratio > 1.2))
            throw new ContractError("CFD-CASE-NONFINITE", "ExpansionRatio must be finite in [1.0, 1.2].");
        if (proposal.ClHalfBandCap is { } clCap && (!double.IsFinite(clCap) || clCap <= 0))
            throw new ContractError("CFD-CASE-NONFINITE", "ClHalfBandCap must be finite and positive.");
        if (proposal.CdHalfBandCap is { } cdCap && (!double.IsFinite(cdCap) || cdCap <= 0))
            throw new ContractError("CFD-CASE-NONFINITE", "CdHalfBandCap must be finite and positive.");

        // AI-05: LogNote is strictly quoted data and never alters disposition or triggers auto-launch.
        return new CaseDiffValidationResult(
            Valid: true,
            CreatesNewDraftVersion: true,
            AutoLaunchAllowed: false,
            Summary: "Typed case diff validated; accepting creates a new Draft version referencing the prior run.");
    }
}
