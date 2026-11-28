using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.Notebook;
using CfdWorkbench.Core.SolverSetup;

namespace CfdWorkbench.Analysis.Notebook;

public sealed record NotebookDecompOutcome(
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    RequirementSpecVersion? RequirementVersion,
    FoilReqDocument? Document,
    FoilReqValidationResult? Validation,
    GrillTreeState? GrillState,
    AssistantExecutionResult? Execution);

public sealed record NotebookCandidateOutcome(
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    CandidateIterationRecord? Candidate,
    RequirementScorecardResult? Scorecard,
    string? SolverContext,
    AssistantExecutionResult? Execution);

public sealed record NotebookTranslationOutcome(
    bool Succeeded,
    bool Withheld,
    string? ErrorCode,
    string DisplayText,
    NotebookTurnRecord? Turn,
    AssistantExecutionResult? Execution);

/// <summary>
/// Wave 13B AI-Guided Design Notebook orchestrator (<c>docs/design/ai-guided-design-notebook.md §3.4</c>).
/// Composes with <see cref="AssistantService"/> and <see cref="INotebookSolverRunner"/>:
/// - Passes ONLY fixed trusted directives in <see cref="AssistantPreviewRequest.UserPrompt"/>; EVERY untrusted user brief,
///   commercial reference, custom grill answer, and bounded turn history (last 6 turns) enters strictly via
///   <see cref="UntrustedExcerpt"/> so <see cref="PromptRedactor"/> wraps it in <c>&lt;&lt;&lt;CFDW_QUOTED_DATA</c>.
/// - Enforces explicit byte-exact <c>SHA-256</c> consent, <c>NB-EMPTY-PROMPT</c>, <c>max_repair_attempts = 2</c>
///   (<c>NB-REPAIR-CAP-REACHED</c>), optional same-endpoint <c>OptionalFastModelId</c>, and <c>AI-03</c>/<c>LAB-01</c>
///   withhold (<c>COPY-934</c>) while preserving all local solver artifacts.
/// - ZERO new C# <c>event</c>s (uses <see cref="IProgress{T}"/> + existing <see cref="AssistantService.TelemetryEmitted"/>)
///   and ZERO banned <c>ArchitectureTests</c> Core calls.
/// </summary>
public sealed class NotebookOrchestrator
{
    public const int MaxRepairAttempts = 2;
    public const int MaxHistoryTurnsWindow = 6;
    public const string EmptyPromptCode = "NB-EMPTY-PROMPT";
    public const string RepairCapCode = "NB-REPAIR-CAP-REACHED";
    public const string WithheldBannerCopy934 =
        "Response withheld — cited a numeral not present in local solver output or breached LAB-01 copy policy.";

    // simplify: Multi-turn UntrustedExcerpt window keeps pinned FoilReq + latest active candidate summary + last 6 turns within 32,768 UTF-8 B cap; upgrade trigger: when sessions routinely exceed 25 iterations.

    private const string TrustedDecompDirective =
        "Decompose the quoted user brief into strict requirement-decomp JSON carrying EBNF-valid FoilReq 1.0 DSL, conceptual-only reference policy, and field provenance. Never copy proprietary coordinates or use LAB-01 banned claims.";

    private const string TrustedDeltaDirective =
        "Synthesize a bounded candidate-intent-delta JSON using admitted GEN or My sections profile IDs matching the quoted FoilReq 1.0 specification and refinement request. Never emit raw coordinates, meshes, forces, or scores.";

    private const string TrustedRepairDirective =
        "Repair the quoted candidate-intent-delta JSON to resolve the named deterministic geometry or validation refusal while preserving FoilReq 1.0 bounds and admitted profile IDs. Never emit raw coordinates or forces.";

    private const string TrustedTranslationDirective =
        "Translate the quoted Tier-0 local solver_context into concise riding-feel engineering guidance as strict AnalysisExplanation JSON citing the candidate ID. Cite ONLY numerals present verbatim in solver_context and obey LAB-01.";

    private readonly AssistantService assistant;
    private readonly INotebookSolverRunner solverRunner;
    private readonly Func<DateTimeOffset> clock;

    public NotebookOrchestrator(
        AssistantService assistant,
        INotebookSolverRunner? solverRunner = null,
        Func<DateTimeOffset>? clock = null)
    {
        this.assistant = assistant ?? throw new ArgumentNullException(nameof(assistant));
        this.solverRunner = solverRunner ?? new NotebookCandidateRunner();
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public Task<AssistantPreviewResult> PrepareDecompPreviewAsync(
        string userBriefText,
        NotebookSessionRecord? existingSession = null,
        RedactionContext? redaction = null,
        bool useFastModel = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userBriefText))
            return Task.FromResult(EmptyPromptBlocked());

        var excerpts = new List<UntrustedExcerpt>
        {
            new("user-brief", "brief:1", userBriefText.Trim())
        };
        AppendBoundedHistoryExcerpts(excerpts, existingSession);

        return PrepareInternalAsync(
            AssistantProposalKind.RequirementDecomp,
            TrustedDecompDirective,
            excerpts,
            knowledgeQuery: "hydrofoil setup brief requirements",
            redaction: redaction,
            useFastModel: useFastModel,
            ct: ct);
    }

    public async Task<NotebookDecompOutcome> ExecuteDecompAsync(
        PreparedAssistantCall prepared,
        string consentedPayloadSha256,
        AuthoringSession? authoringSession = null,
        string? rawUserPromptForTurn = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var valInput = new AssistantValidationInput(
            Area: AssistantArea.Notebook,
            ProposalKind: AssistantProposalKind.RequirementDecomp,
            FallbackText: WithheldBannerCopy934);

        var exec = await assistant.ExecuteConsentedAsync(prepared, consentedPayloadSha256, valInput, ct).ConfigureAwait(false);
        sw.Stop();

        if (!exec.Succeeded || exec.Validation?.RequirementDecomp is not { } decomp)
        {
            authoringSession?.RecordNotebookEvent("notebook.decomp", exec.ErrorCode ?? "refused", sw.Elapsed.TotalMilliseconds,
                new NotebookTelemetry("notebook.decomp", ModelId: prepared.ResolvedModelId, PayloadBytes: prepared.Payload.PromptUtf8Bytes,
                    Valid: false, Code: exec.ErrorCode ?? "refused"));
            return new NotebookDecompOutcome(false, exec.ErrorCode, exec.ErrorMessage, null, null, null, null, exec);
        }

        var existing = authoringSession?.ReadNotebookSession();
        int nextOrdinal = (existing?.Requirements.Count ?? 0) + 1;
        string ordinalLabel = $"v1.{nextOrdinal - 1}";
        string? parentReqId = existing?.Requirements.Count > 0 ? existing.Requirements[^1].ReqVersionId : null;

        var reqVer = NotebookRecords.CreateRequirementVersion(
            decomp.Document,
            ordinalLabel,
            parentReqId,
            createdUtc: clock());
        var grillState = GrillTreeEngine.EvaluateState(decomp.Document, decomp.Validation, []);

        if (authoringSession is not null)
        {
            var turns = new List<NotebookTurnRecord>(existing?.Turns ?? []);
            if (!string.IsNullOrWhiteSpace(rawUserPromptForTurn))
            {
                turns.Add(new NotebookTurnRecord(
                    Guid.NewGuid().ToString("D"), clock(), "user-prompt", reqVer.ReqVersionId, null,
                    NotebookRecords.ChunkText(rawUserPromptForTurn.Trim())));
            }
            turns.Add(new NotebookTurnRecord(
                Guid.NewGuid().ToString("D"), clock(), "foilreq-decomp", reqVer.ReqVersionId, null,
                NotebookRecords.ChunkText(decomp.Summary), WallTimeMs: sw.Elapsed.TotalMilliseconds));

            var updated = NotebookRecords.FinalizeSession(existing is null
                ? new NotebookSessionRecord(
                    Guid.NewGuid().ToString("D"),
                    NotebookRecords.DeterministicSessionTitle(decomp.Document),
                    clock(),
                    [reqVer],
                    turns,
                    [],
                    [],
                    "")
                : existing with
                {
                    Requirements = [.. existing.Requirements, reqVer],
                    Turns = turns
                });

            authoringSession.RecordNotebookSession(updated);
            authoringSession.RecordNotebookEvent("notebook.decomp", "ok", sw.Elapsed.TotalMilliseconds,
                new NotebookTelemetry(
                    EventKind: "notebook.decomp",
                    ModelId: prepared.ResolvedModelId,
                    PayloadBytes: prepared.Payload.PromptUtf8Bytes,
                    Valid: true,
                    UnitFlagged: decomp.Validation.Findings.Any(f => f.Code == FoilReqValidator.UnitSanityCode),
                    PhysicsTension: decomp.Validation.Findings.Any(f => f.Code == FoilReqValidator.PhysicsTensionCode),
                    Code: "ok"));
        }

        return new NotebookDecompOutcome(true, null, null, reqVer, decomp.Document, decomp.Validation, grillState, exec);
    }

    public Task<AssistantPreviewResult> PrepareCandidateDeltaPreviewAsync(
        FoilReqDocument req,
        string? refinementPrompt = null,
        NotebookSessionRecord? existingSession = null,
        RedactionContext? redaction = null,
        bool useFastModel = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        var excerpts = new List<UntrustedExcerpt>
        {
            new("foilreq", "foilreq:current", FoilReqSerializer.Format(req))
        };
        if (!string.IsNullOrWhiteSpace(refinementPrompt))
            excerpts.Add(new UntrustedExcerpt("user-brief", "refinement:1", refinementPrompt.Trim()));
        AppendBoundedHistoryExcerpts(excerpts, existingSession);

        return PrepareInternalAsync(
            AssistantProposalKind.CandidateIntentDelta,
            TrustedDeltaDirective,
            excerpts,
            knowledgeQuery: "hydrofoil planform section taper aspect ratio",
            redaction: redaction,
            useFastModel: useFastModel,
            ct: ct);
    }

    /// <summary>
    /// Prepares a strictly user-consented repair preview capped at <see cref="MaxRepairAttempts"/> (2).
    /// Attempting a 3rd repair fails closed with <see cref="RepairCapCode"/> and makes 0 HTTP calls.
    /// </summary>
    public Task<AssistantPreviewResult> PrepareRepairPreviewAsync(
        FoilReqDocument req,
        int currentRepairAttemptIndex,
        string refusalCode,
        string refusalDetail,
        NotebookSessionRecord? existingSession = null,
        RedactionContext? redaction = null,
        bool useFastModel = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (currentRepairAttemptIndex >= MaxRepairAttempts)
        {
            return Task.FromResult(new AssistantPreviewResult(
                Allowed: false,
                BlockKind: AssistantGateBlockKind.SettingsError,
                BlockCode: RepairCapCode,
                Message: $"Repair cap reached ({MaxRepairAttempts} attempts). Adjust FoilReq or geometry parameters manually.",
                Prepared: null,
                Eligibility: null,
                DailyUsedUtc: 0,
                DailyCap: 0));
        }

        var excerpts = new List<UntrustedExcerpt>
        {
            new("foilreq", "foilreq:current", FoilReqSerializer.Format(req)),
            new("grill-answer", "repair:context", $"Previous candidate refused with {refusalCode}: {refusalDetail}")
        };
        AppendBoundedHistoryExcerpts(excerpts, existingSession);

        return PrepareInternalAsync(
            AssistantProposalKind.CandidateIntentDelta,
            TrustedRepairDirective,
            excerpts,
            knowledgeQuery: "hydrofoil tip chord geometry refusal repair",
            redaction: redaction,
            useFastModel: useFastModel,
            ct: ct);
    }

    public async Task<NotebookCandidateOutcome> ExecuteCandidateSynthesisAsync(
        PreparedAssistantCall prepared,
        string consentedPayloadSha256,
        FoilReqDocument req,
        string reqVersionId,
        int repairAttemptIndex = 0,
        AuthoringSession? authoringSession = null,
        CandidateIterationRecord? baseline = null,
        IReadOnlySet<string>? admittedMySectionIds = null,
        IReadOnlyDictionary<string, string>? mySectionDatSnapshots = null,
        IProgress<NotebookProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (repairAttemptIndex > MaxRepairAttempts)
        {
            return new NotebookCandidateOutcome(false, RepairCapCode,
                $"Repair cap reached (max {MaxRepairAttempts} attempts).", null, null, null, null);
        }

        double? hardMaxSpan = req.FindConstraint("max_span") is { Priority.Kind: "hard", Quantity: { } sq } && sq.TryToMeters(out double sm)
            ? sm
            : null;

        var valInput = new AssistantValidationInput(
            Area: AssistantArea.Notebook,
            ProposalKind: AssistantProposalKind.CandidateIntentDelta,
            AdmittedMySectionIds: admittedMySectionIds,
            MySectionDatSnapshots: mySectionDatSnapshots,
            HardMaxSpanMeters: hardMaxSpan,
            FallbackText: WithheldBannerCopy934);

        var exec = await assistant.ExecuteConsentedAsync(prepared, consentedPayloadSha256, valInput, ct).ConfigureAwait(false);
        if (!exec.Succeeded || exec.Validation?.CandidateIntent is not { } delta)
        {
            return new NotebookCandidateOutcome(false, exec.ErrorCode, exec.ErrorMessage, null, null, null, exec);
        }

        // Run local deterministic physics (direct ProductWingMethod / PanelMethod / NeuralFoil — zero Envelope.Analysis mutation).
        var runRes = await solverRunner.EvaluateCandidateAsync(req, delta, progress, ct).ConfigureAwait(false);

        var existing = authoringSession?.ReadNotebookSession();
        int ordinal = (existing?.Candidates.Count ?? 0) + 1;
        var candidate = NotebookRecords.CreateCandidateRecord(
            ordinal: ordinal,
            reqVersionId: reqVersionId,
            delta: delta,
            surfaceHash: runRes.SurfaceHash,
            geometryStatus: runRes.GeometryStatus,
            refusalCode: runRes.RefusalCode,
            operatingPoints: runRes.OperatingPoints,
            repairAttemptIndex: repairAttemptIndex,
            createdUtc: clock());

        var scorecard = RequirementScorecard.Evaluate(req, reqVersionId, candidate, baseline, reqVersionId);
        string solverContext = RequirementScorecard.FormatSolverContext(scorecard, candidate, baseline);

        if (authoringSession is not null && existing is not null)
        {
            var turn = new NotebookTurnRecord(
                Guid.NewGuid().ToString("D"),
                clock(),
                runRes.Certified ? "candidate-evaluated" : "candidate-refused",
                reqVersionId,
                candidate.CandidateId,
                NotebookRecords.ChunkText(runRes.Certified
                    ? $"Local solver evaluated {candidate.CandidateId}: span {scorecard.SpanMm:0.0} mm, AR {scorecard.AspectRatio:0.00} ({runRes.OperatingPoints.Count} operating points)."
                    : $"Candidate {candidate.CandidateId} refused: {runRes.RefusalCode ?? runRes.GeometryStatus}."),
                WallTimeMs: runRes.TotalWallMs,
                ErrorCode: runRes.RefusalCode,
                RepairAttempt: repairAttemptIndex > 0 ? repairAttemptIndex : null);

            var updated = NotebookRecords.FinalizeSession(existing with
            {
                Candidates = [.. existing.Candidates, candidate],
                Turns = [.. existing.Turns, turn]
            });
            authoringSession.RecordNotebookSession(updated);
            authoringSession.RecordNotebookEvent("notebook.candidate.run", runRes.Certified ? "ok" : (runRes.RefusalCode ?? "refused"),
                runRes.TotalWallMs,
                new NotebookTelemetry(
                    EventKind: "notebook.candidate.run",
                    CandidateOrdinal: ordinal,
                    GeometryStatus: runRes.GeometryStatus,
                    SolverWallMs: runRes.TotalWallMs,
                    OpCount: runRes.OperatingPoints.Count,
                    RepairAttempt: repairAttemptIndex,
                    Code: runRes.RefusalCode ?? "ok"));
        }

        return new NotebookCandidateOutcome(
            Succeeded: runRes.Certified,
            ErrorCode: runRes.RefusalCode,
            ErrorMessage: runRes.Certified ? null : $"Candidate refused: {runRes.RefusalCode ?? runRes.GeometryStatus}.",
            Candidate: candidate,
            Scorecard: scorecard,
            SolverContext: solverContext,
            Execution: exec);
    }

    public Task<AssistantPreviewResult> PrepareTranslationPreviewAsync(
        string candidateId,
        string solverContext,
        NotebookSessionRecord? existingSession = null,
        RedactionContext? redaction = null,
        bool useFastModel = false,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(solverContext);

        var excerpts = new List<UntrustedExcerpt>
        {
            new("solver-context", candidateId, solverContext)
        };
        AppendBoundedHistoryExcerpts(excerpts, existingSession);

        return PrepareInternalAsync(
            AssistantProposalKind.AnalysisExplanation,
            TrustedTranslationDirective,
            excerpts,
            knowledgeQuery: "hydrofoil cavitation margin glide takeoff",
            redaction: redaction,
            useFastModel: useFastModel,
            ct: ct);
    }

    /// <summary>
    /// Executes the physics-to-riding-feel translation turn via <see cref="AssistantProposalKind.AnalysisExplanation"/>,
    /// enforcing <c>AI-03</c> numeral grounding against <paramref name="solverContext"/> and <c>LAB-01</c>.
    /// On withhold, records a <c>Withheld</c> turn carrying <c>COPY-934</c> while leaving all local solver artifacts intact.
    /// </summary>
    public async Task<NotebookTranslationOutcome> ExecuteTranslationAsync(
        PreparedAssistantCall prepared,
        string consentedPayloadSha256,
        string candidateId,
        string reqVersionId,
        string solverContext,
        AuthoringSession? authoringSession = null,
        CancellationToken ct = default)
    {
        var valInput = new AssistantValidationInput(
            Area: AssistantArea.Notebook,
            ProposalKind: AssistantProposalKind.AnalysisExplanation,
            AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { candidateId },
            SharedContextText: solverContext,
            FallbackText: WithheldBannerCopy934);

        var exec = await assistant.ExecuteConsentedAsync(prepared, consentedPayloadSha256, valInput, ct).ConfigureAwait(false);
        bool withheld = exec.Validation?.Verdict is AssistantVerdict.Withheld or AssistantVerdict.Refused;
        string displayText = exec.Succeeded && exec.Validation?.AnalysisExplanation is { } exp
            ? exp.Text
            : WithheldBannerCopy934;

        var turn = new NotebookTurnRecord(
            Guid.NewGuid().ToString("D"),
            clock(),
            exec.Succeeded ? "ai-translation" : "withheld",
            reqVersionId,
            candidateId,
            NotebookRecords.ChunkText(displayText),
            WallTimeMs: exec.AuditEntry.DurationMs,
            ErrorCode: exec.Succeeded ? null : (exec.ErrorCode ?? "AI-03-WITHHELD"));

        if (authoringSession is not null && authoringSession.ReadNotebookSession() is { } existing)
        {
            var updated = NotebookRecords.FinalizeSession(existing with
            {
                Turns = [.. existing.Turns, turn]
            });
            authoringSession.RecordNotebookSession(updated);
        }

        return new NotebookTranslationOutcome(
            Succeeded: exec.Succeeded,
            Withheld: withheld,
            ErrorCode: exec.ErrorCode,
            DisplayText: displayText,
            Turn: turn,
            Execution: exec);
    }

    private async Task<AssistantPreviewResult> PrepareInternalAsync(
        AssistantProposalKind kind,
        string trustedDirective,
        List<UntrustedExcerpt> excerpts,
        string knowledgeQuery,
        RedactionContext? redaction,
        bool useFastModel,
        CancellationToken ct)
    {
        string? modelOverride = null;
        if (useFastModel)
        {
            var status = await assistant.GetStatusAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(status.Settings.OptionalFastModelId))
                modelOverride = status.Settings.OptionalFastModelId;
        }

        var req = new AssistantPreviewRequest(
            Area: AssistantArea.Notebook,
            ProposalKind: kind,
            SystemPrompt: "CFD-Workbench AI-Guided Design Notebook — return strict JSON matching the requested schema.",
            UserPrompt: trustedDirective,
            Excerpts: excerpts,
            KnowledgeQuery: knowledgeQuery,
            ModelIdOverride: modelOverride);

        return await assistant.PreparePreviewAsync(req, redaction, ct).ConfigureAwait(false);
    }

    private static void AppendBoundedHistoryExcerpts(List<UntrustedExcerpt> excerpts, NotebookSessionRecord? session)
    {
        if (session is null || session.Turns.Count == 0) return;
        int start = Math.Max(0, session.Turns.Count - MaxHistoryTurnsWindow);
        for (int i = start; i < session.Turns.Count; i++)
        {
            var t = session.Turns[i];
            string body = NotebookRecords.JoinChunks(t.BodyLines);
            if (string.IsNullOrWhiteSpace(body)) continue;
            excerpts.Add(new UntrustedExcerpt("turn-history", $"turn:{i + 1}:{t.Kind}", body));
        }
    }

    private static AssistantPreviewResult EmptyPromptBlocked() => new(
        Allowed: false,
        BlockKind: AssistantGateBlockKind.SettingsError,
        BlockCode: EmptyPromptCode,
        Message: "Enter a riding brief or refinement prompt before sending.",
        Prepared: null,
        Eligibility: null,
        DailyUsedUtc: 0,
        DailyCap: 0);
}
