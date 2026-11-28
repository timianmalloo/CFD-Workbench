using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Assistant;
using CfdWorkbench.Analysis.Notebook;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.Notebook;
using CfdWorkbench.Desktop.Assistant;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Notebook;

/// <summary>Result of user consent review for one outbound notebook BYOK call.</summary>
public sealed record NotebookConsentDecision(bool Approved, string? ConsentedPayloadSha256, AssistantExecutionResult? Execution = null);

/// <summary>
/// Native Avalonia AI-Guided Hydrofoil Design Notebook workspace (<c>GuidedDesignNotebook</c>,
/// <c>docs/design/ai-guided-design-notebook.md §8</c>, matching <c>docs/mockups/ai-guided-design-notebook.html</c>).
/// Implements all 7 locked states (<c>empty</c>, <c>grill</c>, <c>simulating</c>, <c>iterated</c>, <c>refused</c>,
/// <c>no-key</c>, <c>overflow</c>), 5 arrow-key-navigable scientific artifact tabs reusing existing controls
/// (<see cref="Analysis.SectionProfileView"/>, <see cref="Analysis.SectionChartView"/>,
/// <see cref="Sweeps.PerformanceCurveChart"/>, <see cref="Analysis.LoadingChart"/>,
/// <see cref="Setup.PlanformPreviewControl"/>, <see cref="SurfaceRenderer"/> + <see cref="ViewCamera"/>),
/// serial <c>GrillTreeEngine</c> Q&amp;A with clickable branch re-open (<c>TQ9</c> stale), and non-destructive
/// <see cref="CadHandoffDialog"/> handoff into Area 2 CAD.
/// </summary>
public partial class NotebookWorkspaceView : UserControl
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const int TabSection = 0;
    public const int TabPolars = 1;
    public const int TabElevations = 2;
    public const int TabWing3d = 3;
    public const int TabScorecard = 4;

    private readonly ToggleButton[] tabButtons;
    private readonly List<GrillResolution> grillHistory = [];
    private readonly Dictionary<string, SurfaceView> surfaceCache = new(StringComparer.Ordinal);
    private readonly Dictionary<(string SurfaceHash, string OpId, double Eta), PanelResult> panelCache = new();
    private readonly Dictionary<(string SurfaceHash, string OpId), IReadOnlyList<LoadingPoint>> loadingCache = new();
    private readonly ProductWingMethod wingMethod = new();

    private WorkbenchController? controller;
    private NotebookOrchestrator? orchestrator;
    private INotebookSolverRunner? customSolverRunner;
    private NotebookSessionRecord? sessionSnapshot;
    private FoilReqDocument? currentDoc;
    private FoilReqValidationResult? currentValidation;
    private GrillTreeState? currentGrillState;
    private CandidateIterationRecord? activeCandidate;
    private CandidateIterationRecord? compareBaseline;
    private NotebookWorkspaceState currentState = NotebookWorkspaceState.Empty;
    private int activeTabIndex = TabSection;
    private bool syncingCandidateCombo;
    private bool useFastModelForTranslation;
    private CancellationTokenSource? activeRunCts;
    private string? activeWithheldText;
    private int activeRepairAttempts;

    public NotebookWorkspaceView()
    {
        InitializeComponent();

        tabButtons =
        [
            TabSectionButton,
            TabPolarsButton,
            TabElevationsButton,
            TabWing3dButton,
            TabScorecardButton
        ];

        BackToWorkbenchButton.Content = NotebookCopy.BackToWorkbenchButton;
        OpenInCadButton.Content = NotebookCopy.OpenInCadButton;
        CadDisabledNoteText.Text = NotebookCopy.OpenInCadDisabledReason;
        EmptyPromptGuideText.Text = NotebookCopy.EmptyPromptGuide;
        NoKeyBannerText.Text = NotebookCopy.NoKeyBanner;
        ConfigureKeyButton.Content = NotebookCopy.ConfigureKeyButton;
        UseManualSetupButton.Content = NotebookCopy.UseManualSetupButton;
        ComposerHintText.Text = NotebookCopy.ComposerHint;
        PreviewPayloadButton.Content = NotebookCopy.PreviewPayloadButton;
        SendPromptButton.Content = NotebookCopy.SendButton;
        StopRunButton.Content = NotebookCopy.StopButton;
        GrillSkipButton.Content = NotebookCopy.SkipAssumptionButton;
        GrillGenerateNowButton.Content = NotebookCopy.GenerateNowButton;
        AskAiToRepairButton.Content = NotebookCopy.AskAiToRepairButton;
        ProvenanceStripText.Text = NotebookCopy.ArtifactsProvenanceStrip;
        WithheldBannerText.Text = NotebookCopy.ResponseWithheldBanner;
        PolarsNoticeText.Text = $"{RequirementScorecard.SurrogateNotice} {RequirementScorecard.AttachedFlowNotice} {RequirementScorecard.WingOnlyNotice}";

        BuildTunerChips();
        WireInteractions();
        SelectTab(TabSection);
        ApplyUiForCurrentState();
    }

    public event Action? ConfigureKeyRequested;
    public event Action? BackToWorkbenchRequested;
    public event Action? UseManualSetupRequested;
    public event Action<StatusReport>? StatusReported;
    public event Action<CadHandoffOutcome>? CadHandoffCompleted;

    /// <summary>Optional test/automation hook for consent preview (returns Approved + SHA-256 without blocking on OS modal loop).</summary>
    public Func<PreparedAssistantCall, AssistantValidationInput, Task<NotebookConsentDecision>>? ConsentHandler { get; set; }

    /// <summary>Optional test/automation hook for CAD handoff confirmation dialog.</summary>
    public Func<CandidateIterationRecord, int, Task<bool>>? CadHandoffConfirmHandler { get; set; }

    /// <summary>Optional solver runner seam for deterministic fast UI tests (defaults to real local <see cref="NotebookCandidateRunner"/>).</summary>
    public INotebookSolverRunner? SolverRunnerOverride
    {
        get => customSolverRunner;
        set
        {
            customSolverRunner = value;
            orchestrator = null;
        }
    }

    public bool UseFastModelForTranslation
    {
        get => useFastModelForTranslation;
        set => useFastModelForTranslation = value;
    }

    public NotebookWorkspaceState CurrentState => currentState;
    public int ActiveTabIndex => activeTabIndex;
    public bool CompareEnabled => CompareToggleButton.IsChecked == true;
    public bool TableTwinEnabled => TableTwinToggleButton.IsChecked == true;
    public NotebookSessionRecord? CurrentSession => sessionSnapshot;
    public CandidateIterationRecord? ActiveCandidate => activeCandidate;
    public FoilReqDocument? CurrentDocument => currentDoc;
    public GrillTreeState? CurrentGrillState => currentGrillState;
    public IReadOnlyList<GrillResolution> GrillHistory => grillHistory;

    public void Bind(WorkbenchController workbenchController)
    {
        ArgumentNullException.ThrowIfNull(workbenchController);
        bool changedController = !ReferenceEquals(controller, workbenchController);
        controller = workbenchController;
        if (changedController)
            orchestrator = null;

        var persisted = controller.ActiveSession.ReadNotebookSession();
        if (persisted is not null && !ReferenceEquals(persisted, sessionSnapshot))
        {
            BindSession(persisted);
        }
        RefreshAssistantStatus();
    }

    public void RefreshAssistantStatus()
    {
        var hub = controller?.Assistant;
        bool hasKey = hub?.HasApiKey == true;
        bool evalPassed = hub?.Eligibility.ProposalsEnabled == true;
        string modelId = hub?.CurrentSettings.ModelId ?? "Not configured";
        string? fastId = hub?.CurrentSettings.OptionalFastModelId;

        if (!hasKey)
        {
            ModelStatusChipText.Text = "BYOK: No API key";
            if (sessionSnapshot is null || currentState is NotebookWorkspaceState.Empty or NotebookWorkspaceState.NoKey)
                currentState = NotebookWorkspaceState.NoKey;
        }
        else if (!evalPassed)
        {
            ModelStatusChipText.Text = $"Model: {modelId} (Eval needed)";
            if (currentState == NotebookWorkspaceState.NoKey)
                currentState = sessionSnapshot is null ? NotebookWorkspaceState.Empty : DeriveStateFromSession(sessionSnapshot);
        }
        else
        {
            ModelStatusChipText.Text = string.IsNullOrWhiteSpace(fastId)
                ? $"Model: {modelId} ✓"
                : $"Models: {modelId} + {fastId} ✓";
            if (currentState == NotebookWorkspaceState.NoKey)
                currentState = sessionSnapshot is null ? NotebookWorkspaceState.Empty : DeriveStateFromSession(sessionSnapshot);
        }

        UnevaluatedBannerCard.IsVisible = hasKey && !evalPassed && currentState != NotebookWorkspaceState.NoKey;
        if (UnevaluatedBannerCard.IsVisible)
        {
            UnevaluatedBannerTitle.Text = AssistantCopy.UnevaluatedOnThisModel;
            UnevaluatedBannerText.Text = AssistantCopy.UnevaluatedDetail(AssistantEvalSuite.EvalThresholdsVersion);
        }

        ApplyUiForCurrentState();
    }

    /// <summary>Binds an existing <see cref="NotebookSessionRecord"/> directly (live reopen or test fixture).</summary>
    public void BindSession(NotebookSessionRecord session, string? selectedCandidateId = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        sessionSnapshot = session;
        activeWithheldText = null;
        if (session.Requirements.Count > 0)
        {
            var latestReq = session.Requirements[^1];
            var parsed = FoilReqParser.Parse(NotebookRecords.JoinChunks(latestReq.DslLines));
            if (parsed.IsValid && parsed.Document is not null)
            {
                currentDoc = parsed.Document;
                currentValidation = FoilReqValidator.Validate(currentDoc);
                currentGrillState = GrillTreeEngine.EvaluateState(currentDoc, currentValidation, grillHistory);
            }
        }
        else
        {
            currentDoc = null;
            currentValidation = null;
            currentGrillState = null;
        }

        if (session.Candidates.Count > 0)
        {
            activeCandidate = selectedCandidateId is not null
                ? session.Candidates.FirstOrDefault(c => string.Equals(c.CandidateId, selectedCandidateId, StringComparison.Ordinal)) ?? session.Candidates[^1]
                : session.Candidates[^1];
            compareBaseline = FindBaselineFor(session.Candidates, activeCandidate);
        }
        else
        {
            activeCandidate = null;
            compareBaseline = null;
        }

        currentState = DeriveStateFromSession(session);
        ApplyUiForCurrentState();
    }

    /// <summary>
    /// Applies one of the 7 locked mockup states deterministically for visual/theme/accessibility verification
    /// (<c>empty</c>, <c>grill</c>, <c>simulating</c>, <c>iterated</c>, <c>refused</c>, <c>no-key</c>, <c>overflow</c>).
    /// </summary>
    public void ApplyPreviewState(NotebookWorkspaceState state)
    {
        currentState = state;
        grillHistory.Clear();
        activeWithheldText = null;
        activeRepairAttempts = 0;

        switch (state)
        {
            case NotebookWorkspaceState.Empty:
                sessionSnapshot = null;
                currentDoc = null;
                currentValidation = null;
                currentGrillState = null;
                activeCandidate = null;
                compareBaseline = null;
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;

            case NotebookWorkspaceState.NoKey:
                sessionSnapshot = null;
                currentDoc = null;
                currentValidation = null;
                currentGrillState = null;
                activeCandidate = null;
                compareBaseline = null;
                ModelStatusChipText.Text = "BYOK: No API key";
                break;

            case NotebookWorkspaceState.Grill:
            {
                var doc = BuildWorkedOrcaDoc(unitFixed: false, resolved: false);
                var reqVer = NotebookRecords.CreateRequirementVersion(doc, "v1.0");
                var val = FoilReqValidator.Validate(doc);
                currentDoc = doc;
                currentValidation = val;
                currentGrillState = GrillTreeEngine.EvaluateState(doc, val, grillHistory);
                activeCandidate = null;
                compareBaseline = null;
                sessionSnapshot = NotebookRecords.FinalizeSession(new NotebookSessionRecord(
                    Guid.NewGuid().ToString("D"),
                    "Large-Wave 90 kg Wingfoil · F4 Orca concepts",
                    DateTimeOffset.UtcNow,
                    [reqVer],
                    [
                        new NotebookTurnRecord(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow, "user-prompt", reqVer.ReqVersionId, null, NotebookRecords.ChunkText(NotebookCopy.DefaultExampleBrief)),
                        new NotebookTurnRecord(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow, "foilreq-decomp", reqVer.ReqVersionId, null, NotebookRecords.ChunkText("Decomposed brief into FoilReq 1.0 (v1.0). Flagged 950 cm span typo and AR 12 takeoff tension."))
                    ],
                    [],
                    [],
                    ""));
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;
            }

            case NotebookWorkspaceState.Simulating:
            {
                var doc = BuildWorkedOrcaDoc(unitFixed: true, resolved: true);
                var reqVer = NotebookRecords.CreateRequirementVersion(doc, "v1.4");
                currentDoc = doc;
                currentValidation = FoilReqValidator.Validate(doc);
                currentGrillState = new GrillTreeState(null, 0, 4, 0, true, []);
                activeCandidate = null;
                compareBaseline = null;
                sessionSnapshot = NotebookRecords.FinalizeSession(new NotebookSessionRecord(
                    Guid.NewGuid().ToString("D"),
                    "Large-Wave 90 kg Wingfoil · Local Simulation",
                    DateTimeOffset.UtcNow,
                    [reqVer],
                    [
                        new NotebookTurnRecord(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow, "user-prompt", reqVer.ReqVersionId, null, NotebookRecords.ChunkText(NotebookCopy.DefaultExampleBrief))
                    ],
                    [],
                    [],
                    ""));
                SimulatingStepsText.Text = "1/3 Materialize two-profile FoilDSL 4.1 (cfdw-surf-118 → cfdw-surf-105) ✓\n2/3 PanelMethod 200p + NeuralFoil 0.3.2 polars (active…)\n3/3 VortexLattice + StripCoupler (takeoff / glide / steep_drop)";
                SimulatingProgressBar.Value = 55;
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;
            }

            case NotebookWorkspaceState.Iterated:
            {
                var (session, doc) = BuildIteratedWorkedSession(includeRefused: false, candidateCount: 2);
                sessionSnapshot = session;
                currentDoc = doc;
                currentValidation = FoilReqValidator.Validate(doc);
                currentGrillState = new GrillTreeState(null, 0, 4, 0, true, []);
                SeedWorkedGrillHistory(session.Requirements[^1].ReqVersionId);
                activeCandidate = session.Candidates[^1];
                compareBaseline = session.Candidates[0];
                CompareToggleButton.IsChecked = true;
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;
            }

            case NotebookWorkspaceState.Refused:
            {
                var (session, doc) = BuildIteratedWorkedSession(includeRefused: true, candidateCount: 2);
                sessionSnapshot = session;
                currentDoc = doc;
                currentValidation = FoilReqValidator.Validate(doc);
                currentGrillState = new GrillTreeState(null, 0, 4, 0, true, []);
                SeedWorkedGrillHistory(session.Requirements[^1].ReqVersionId);
                activeCandidate = session.Candidates[^1]; // v2b refused
                compareBaseline = session.Candidates[0];  // v1 certified baseline
                activeWithheldText = NotebookCopy.ResponseWithheldBanner;
                activeRepairAttempts = 1;
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;
            }

            case NotebookWorkspaceState.Overflow:
            {
                var (session, doc) = BuildIteratedWorkedSession(includeRefused: false, candidateCount: 12);
                sessionSnapshot = session;
                currentDoc = doc;
                currentValidation = FoilReqValidator.Validate(doc);
                currentGrillState = new GrillTreeState(null, 0, 4, 0, true, []);
                SeedWorkedGrillHistory(session.Requirements[^1].ReqVersionId);
                activeCandidate = session.Candidates[^1]; // v12
                compareBaseline = session.Candidates[0];
                CompareToggleButton.IsChecked = true;
                ModelStatusChipText.Text = "Model: claude-sonnet-4-5 ✓";
                break;
            }
        }

        ApplyUiForCurrentState();
    }

    public void SelectTab(int index)
    {
        int clamped = Math.Clamp(index, TabSection, TabScorecard);
        activeTabIndex = clamped;
        for (int i = 0; i < tabButtons.Length; i++)
            tabButtons[i].IsChecked = i == clamped;
        UpdateArtifactStageVisibility();
    }

    /// <summary>Submits a brief or refinement prompt through the live <see cref="NotebookOrchestrator"/>.</summary>
    public async Task<bool> SubmitPromptAsync(string? promptText = null, CancellationToken ct = default)
    {
        string text = (promptText ?? PromptInputBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusReported?.Invoke(new StatusReport("Enter a riding brief or refinement prompt before sending."));
            return false;
        }

        if (controller is null)
        {
            StatusReported?.Invoke(new StatusReport("Notebook controller is not bound."));
            return false;
        }

        if (!controller.Assistant.HasApiKey)
        {
            currentState = NotebookWorkspaceState.NoKey;
            ApplyUiForCurrentState();
            return false;
        }

        var orch = EnsureOrchestrator();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        activeRunCts = linkedCts;
        try
        {
            if (currentDoc is null)
            {
                // Stage 1: Brief -> FoilReq 1.0 decomposition
                var preview = await orch.PrepareDecompPreviewAsync(
                    text,
                    existingSession: sessionSnapshot,
                    useFastModel: false,
                    ct: linkedCts.Token);
                if (!preview.Allowed || preview.Prepared is null)
                {
                    StatusReported?.Invoke(new StatusReport(preview.Message ?? "Assistant request blocked."));
                    RefreshAssistantStatus();
                    return false;
                }

                var valInput = new AssistantValidationInput(
                    Area: AssistantArea.Notebook,
                    ProposalKind: AssistantProposalKind.RequirementDecomp,
                    FallbackText: NotebookCopy.ResponseWithheldBanner);

                var decision = await RequestConsentAsync(preview.Prepared, valInput);
                if (!decision.Approved || string.IsNullOrWhiteSpace(decision.ConsentedPayloadSha256))
                    return false;

                var outcome = await orch.ExecuteDecompAsync(
                    preview.Prepared,
                    decision.ConsentedPayloadSha256,
                    controller.ActiveSession,
                    rawUserPromptForTurn: text,
                    ct: linkedCts.Token);

                if (!outcome.Succeeded || outcome.Document is null || outcome.RequirementVersion is null)
                {
                    activeWithheldText = outcome.ErrorMessage ?? NotebookCopy.ResponseWithheldBanner;
                    StatusReported?.Invoke(new StatusReport(activeWithheldText));
                    ApplyUiForCurrentState();
                    return false;
                }

                PromptInputBox.Text = "";
                currentDoc = outcome.Document;
                currentValidation = outcome.Validation;
                currentGrillState = outcome.GrillState;
                sessionSnapshot = controller.ActiveSession.ReadNotebookSession();
                currentState = currentGrillState?.ActiveQuestion is not null
                    ? NotebookWorkspaceState.Grill
                    : NotebookWorkspaceState.Iterated;
                ApplyUiForCurrentState();
                return true;
            }
            else
            {
                // Stage 2: Candidate synthesis / refinement + local physics + grounded AI translation
                return await RunCandidateSynthesisLoopAsync(refinementPrompt: text, repairAttemptIndex: 0, refusalCode: null, refusalDetail: null, linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            StatusReported?.Invoke(new StatusReport("Notebook operation cancelled."));
            currentState = sessionSnapshot is null ? NotebookWorkspaceState.Empty : DeriveStateFromSession(sessionSnapshot);
            ApplyUiForCurrentState();
            return false;
        }
        finally
        {
            activeRunCts = null;
        }
    }

    /// <summary>Synthesizes a candidate from the current <see cref="FoilReqDocument"/> via the live orchestrator.</summary>
    public async Task<bool> RunCandidateSynthesisLoopAsync(
        string? refinementPrompt = null,
        int repairAttemptIndex = 0,
        string? refusalCode = null,
        string? refusalDetail = null,
        CancellationToken ct = default)
    {
        if (controller is null || currentDoc is null) return false;
        var orch = EnsureOrchestrator();

        string reqVersionId = sessionSnapshot?.Requirements.Count > 0
            ? sessionSnapshot.Requirements[^1].ReqVersionId
            : Guid.NewGuid().ToString("D");

        AssistantPreviewResult preview = repairAttemptIndex > 0
            ? await orch.PrepareRepairPreviewAsync(
                currentDoc,
                currentRepairAttemptIndex: repairAttemptIndex - 1,
                refusalCode: refusalCode ?? "DSL-TIP-CHORD-MIN",
                refusalDetail: refusalDetail ?? "Geometry check failed.",
                existingSession: sessionSnapshot,
                ct: ct)
            : await orch.PrepareCandidateDeltaPreviewAsync(
                currentDoc,
                refinementPrompt: refinementPrompt,
                existingSession: sessionSnapshot,
                ct: ct);

        if (!preview.Allowed || preview.Prepared is null)
        {
            StatusReported?.Invoke(new StatusReport(preview.Message ?? "Candidate synthesis blocked."));
            return false;
        }

        double? hardMaxSpan = currentDoc.FindConstraint("max_span") is { Priority.Kind: "hard", Quantity: { } sq } && sq.TryToMeters(out double sm)
            ? sm
            : null;
        var synthValInput = new AssistantValidationInput(
            Area: AssistantArea.Notebook,
            ProposalKind: AssistantProposalKind.CandidateIntentDelta,
            HardMaxSpanMeters: hardMaxSpan,
            FallbackText: NotebookCopy.ResponseWithheldBanner);

        var decision = await RequestConsentAsync(preview.Prepared, synthValInput);
        if (!decision.Approved || string.IsNullOrWhiteSpace(decision.ConsentedPayloadSha256))
            return false;

        currentState = NotebookWorkspaceState.Simulating;
        ApplyUiForCurrentState();

        var progress = new Progress<NotebookProgressUpdate>(u =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                SimulatingStepsText.Text = u.Detail;
                if (u.FractionComplete is { } f)
                    SimulatingProgressBar.Value = Math.Clamp(f * 100.0, 5.0, 100.0);
            });
        });

        var priorChecked = sessionSnapshot?.Candidates.LastOrDefault(IsCertified);
        var candOutcome = await orch.ExecuteCandidateSynthesisAsync(
            preview.Prepared,
            decision.ConsentedPayloadSha256,
            currentDoc,
            reqVersionId,
            repairAttemptIndex: repairAttemptIndex,
            authoringSession: controller.ActiveSession,
            baseline: priorChecked,
            progress: progress,
            ct: ct);

        sessionSnapshot = controller.ActiveSession.ReadNotebookSession() ?? sessionSnapshot;
        if (candOutcome.Candidate is not null)
        {
            activeCandidate = candOutcome.Candidate;
            compareBaseline = priorChecked;
        }

        if (!candOutcome.Succeeded || candOutcome.Candidate is null)
        {
            activeRepairAttempts = Math.Max(activeRepairAttempts, repairAttemptIndex + 1);
            currentState = NotebookWorkspaceState.Refused;
            ApplyUiForCurrentState();
            return false;
        }

        PromptInputBox.Text = "";

        // Step 3: Physics-to-riding-feel translation grounded against Tier-0 solver_context (AI-03).
        if (!string.IsNullOrWhiteSpace(candOutcome.SolverContext))
        {
            var transPreview = await orch.PrepareTranslationPreviewAsync(
                candOutcome.Candidate.CandidateId,
                candOutcome.SolverContext,
                existingSession: sessionSnapshot,
                useFastModel: useFastModelForTranslation,
                ct: ct);

            if (transPreview.Allowed && transPreview.Prepared is not null)
            {
                var transValInput = new AssistantValidationInput(
                    Area: AssistantArea.Notebook,
                    ProposalKind: AssistantProposalKind.AnalysisExplanation,
                    AllowedRunIds: new HashSet<string>(StringComparer.Ordinal) { candOutcome.Candidate.CandidateId },
                    SharedContextText: candOutcome.SolverContext,
                    FallbackText: NotebookCopy.ResponseWithheldBanner);

                var transDecision = await RequestConsentAsync(transPreview.Prepared, transValInput);
                if (transDecision.Approved && !string.IsNullOrWhiteSpace(transDecision.ConsentedPayloadSha256))
                {
                    var transOutcome = await orch.ExecuteTranslationAsync(
                        transPreview.Prepared,
                        transDecision.ConsentedPayloadSha256,
                        candOutcome.Candidate.CandidateId,
                        reqVersionId,
                        candOutcome.SolverContext,
                        controller.ActiveSession,
                        ct: ct);
                    activeWithheldText = transOutcome.Withheld ? NotebookCopy.ResponseWithheldBanner : null;
                    sessionSnapshot = controller.ActiveSession.ReadNotebookSession() ?? sessionSnapshot;
                }
            }
        }

        currentState = sessionSnapshot is { Candidates.Count: >= 10 }
            ? NotebookWorkspaceState.Overflow
            : NotebookWorkspaceState.Iterated;
        ApplyUiForCurrentState();
        return true;
    }

    /// <summary>Resolves the active grill question with a chosen option or custom answer and appends a new <c>FoilReq</c> version.</summary>
    public bool AnswerActiveGrill(string? optionId, string? customAnswer = null)
    {
        if (currentDoc is null || currentGrillState?.ActiveQuestion is not { } activeQ)
            return false;

        try
        {
            GrillErrorText.IsVisible = false;
            var nextDoc = GrillTreeEngine.ResolveQuestion(currentDoc, activeQ.QuestionId, optionId, customAnswer);
            AppendGrillMutation(nextDoc, activeQ.QuestionId, "answer", optionId, customAnswer);
            return true;
        }
        catch (ContractError ex)
        {
            GrillErrorText.Text = ex.Message;
            GrillErrorText.IsVisible = true;
            return false;
        }
    }

    /// <summary>Skips the active grill question, marking it <c>@skipped</c> (visibly Flagged on Scorecard).</summary>
    public bool SkipActiveGrill()
    {
        if (currentDoc is null || currentGrillState?.ActiveQuestion is not { } activeQ)
            return false;

        var nextDoc = GrillTreeEngine.SkipQuestion(currentDoc, activeQ.QuestionId);
        AppendGrillMutation(nextDoc, activeQ.QuestionId, "skip", null, null);
        return true;
    }

    /// <summary>Re-opens a previously answered or skipped grill branch, marking downstream candidates <c>Stale</c> (<c>TQ9</c>).</summary>
    public bool ReopenGrillBranch(string questionId)
    {
        if (currentDoc is null || string.IsNullOrWhiteSpace(questionId))
            return false;

        var nextDoc = GrillTreeEngine.ReopenQuestion(currentDoc, questionId);
        AppendGrillMutation(nextDoc, questionId, "reopen", null, null);
        currentState = NotebookWorkspaceState.Grill;
        ApplyUiForCurrentState();
        return true;
    }

    /// <summary>Terminates grilling cleanly via <c>GenerateNow</c>, tagging open branches <c>Flagged</c>.</summary>
    public void TriggerGenerateNow()
    {
        if (currentDoc is null || currentValidation is null) return;
        var nextDoc = GrillTreeEngine.GenerateNowDocument(currentDoc, currentValidation, grillHistory);
        AppendGrillMutation(nextDoc, "generate_now", "generate_now", null, null);
    }

    /// <summary>Opens <see cref="CadHandoffDialog"/> (or invokes <see cref="CadHandoffConfirmHandler"/>) and executes <c>HandoffNotebookCandidate</c>.</summary>
    public async Task<CadHandoffOutcome?> RequestCadHandoffAsync()
    {
        if (activeCandidate is null || !IsCertified(activeCandidate))
        {
            StatusReported?.Invoke(new StatusReport(NotebookCopy.OpenInCadDisabledReason));
            return null;
        }

        int nextOrd = (controller?.CurrentRevisionOrdinal ?? 0) + 1;
        string? reqLabel = sessionSnapshot?.Requirements.FirstOrDefault(r => r.ReqVersionId == activeCandidate.ReqVersionId)?.OrdinalLabel;
        string? disc = currentDoc?.Brief.Discipline;

        bool confirmed;
        if (CadHandoffConfirmHandler is not null)
        {
            confirmed = await CadHandoffConfirmHandler(activeCandidate, nextOrd);
        }
        else
        {
            var dlg = new CadHandoffDialog(activeCandidate, nextOrd, reqLabel, disc);
            if (TopLevel.GetTopLevel(this) is Window owner)
                await dlg.ShowDialog(owner);
            else
                dlg.Show();
            confirmed = dlg.Confirmed;
        }

        if (!confirmed || controller is null)
            return null;

        // Ensure current session snapshot is persisted onto ActiveSession before handoff.
        if (sessionSnapshot is not null && controller.ActiveSession.ReadNotebookSession() is null)
            controller.ActiveSession.RecordNotebookSession(sessionSnapshot);

        var outcome = controller.HandoffNotebookCandidate(activeCandidate.CandidateId);
        sessionSnapshot = controller.ActiveSession.ReadNotebookSession() ?? sessionSnapshot;
        CadHandoffCompleted?.Invoke(outcome);
        StatusReported?.Invoke(new StatusReport($"Opened {activeCandidate.CandidateId} as Design revision r{outcome.SeedOutcome.RevisionOrdinal} in Area 2 CAD."));
        return outcome;
    }

    /// <summary>Opens the read-only AI-04 redacted payload preview dialog for the current draft prompt.</summary>
    public async Task<PreparedAssistantCall?> PreviewRedactedPayloadAsync(CancellationToken ct = default)
    {
        if (controller is null || !controller.Assistant.HasApiKey)
            return null;

        string prompt = string.IsNullOrWhiteSpace(PromptInputBox.Text)
            ? NotebookCopy.DefaultExampleBrief
            : PromptInputBox.Text!.Trim();

        var excerpts = new List<UntrustedExcerpt>
        {
            new("user-brief", "draft:1", prompt)
        };
        if (currentDoc is not null)
            excerpts.Add(new UntrustedExcerpt("foilreq", "foilreq:current", FoilReqSerializer.Format(currentDoc)));

        var request = new AssistantPreviewRequest(
            Area: AssistantArea.Notebook,
            ProposalKind: currentDoc is null ? AssistantProposalKind.RequirementDecomp : AssistantProposalKind.CandidateIntentDelta,
            SystemPrompt: "CFD-Workbench AI-Guided Design Notebook — return strict JSON matching the requested schema.",
            UserPrompt: "Decompose or synthesize over the quoted untrusted excerpts only.",
            Excerpts: excerpts,
            ModelIdOverride: useFastModelForTranslation ? controller.Assistant.CurrentSettings.OptionalFastModelId : null);

        var prepared = controller.Assistant.Service.BuildOfflinePreview(request, controller.Assistant.CurrentSettings);

        if (ConsentHandler is not null)
        {
            var dummyVal = new AssistantValidationInput(AssistantArea.Notebook, request.ProposalKind);
            await ConsentHandler(prepared, dummyVal);
            return prepared;
        }

        var dlg = new AssistantConsentPreviewDialog(
            controller.Assistant,
            prepared,
            new AssistantValidationInput(AssistantArea.Notebook, request.ProposalKind));
        if (TopLevel.GetTopLevel(this) is Window owner)
            await dlg.ShowDialog(owner);
        return prepared;
    }

    private void WireInteractions()
    {
        BackToWorkbenchButton.Click += (_, _) => BackToWorkbenchRequested?.Invoke();
        ConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        OpenSettingsUnevalButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        UseManualSetupButton.Click += (_, _) => UseManualSetupRequested?.Invoke();
        InspectDslButton.Click += (_, _) => FoilReqDrawerBox.IsVisible = !FoilReqDrawerBox.IsVisible;

        StarterWaveBriefButton.Click += (_, _) => { PromptInputBox.Text = NotebookCopy.DefaultExampleBrief; PromptInputBox.Focus(); };
        StarterDownwindBriefButton.Click += (_, _) => { PromptInputBox.Text = NotebookCopy.DownwindExampleBrief; PromptInputBox.Focus(); };
        StarterFreerideBriefButton.Click += (_, _) => { PromptInputBox.Text = NotebookCopy.FreerideExampleBrief; PromptInputBox.Focus(); };

        SendPromptButton.Click += async (_, _) => await SubmitPromptAsync();
        StopRunButton.Click += (_, _) => CancelActiveRun();
        CancelRunButton.Click += (_, _) => CancelActiveRun();
        PreviewPayloadButton.Click += async (_, _) => await PreviewRedactedPayloadAsync();

        PromptInputBox.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Enter && (args.KeyModifiers.HasFlag(KeyModifiers.Control) || args.KeyModifiers.HasFlag(KeyModifiers.Meta)))
            {
                args.Handled = true;
                await SubmitPromptAsync();
            }
        };

        GrillCustomApplyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(GrillCustomAnswerBox.Text))
            {
                if (AnswerActiveGrill(null, GrillCustomAnswerBox.Text))
                    GrillCustomAnswerBox.Text = "";
            }
        };
        GrillSkipButton.Click += (_, _) => SkipActiveGrill();
        GrillGenerateNowButton.Click += async (_, _) =>
        {
            TriggerGenerateNow();
            if (controller is not null && controller.Assistant.HasApiKey && controller.Assistant.Eligibility.ProposalsEnabled)
                await RunCandidateSynthesisLoopAsync();
        };

        AskAiToRepairButton.Click += async (_, _) =>
        {
            if (activeCandidate is null) return;
            await RunCandidateSynthesisLoopAsync(
                refinementPrompt: null,
                repairAttemptIndex: activeRepairAttempts + 1,
                refusalCode: activeCandidate.RefusalCode ?? "DSL-TIP-CHORD-MIN",
                refusalDetail: "Increase tip chord above minimum TipChord floor.");
        };
        SwitchToCheckedBaselineButton.Click += (_, _) =>
        {
            if (sessionSnapshot is null) return;
            var checkedCand = sessionSnapshot.Candidates.LastOrDefault(IsCertified);
            if (checkedCand is null) return;
            activeCandidate = checkedCand;
            compareBaseline = FindBaselineFor(sessionSnapshot.Candidates, checkedCand);
            currentState = NotebookWorkspaceState.Iterated;
            ApplyUiForCurrentState();
        };

        OpenInCadButton.Click += async (_, _) => await RequestCadHandoffAsync();

        CandidateSelectorCombo.SelectionChanged += (_, _) =>
        {
            if (syncingCandidateCombo || sessionSnapshot is null) return;
            int idx = CandidateSelectorCombo.SelectedIndex;
            if (idx >= 0 && idx < sessionSnapshot.Candidates.Count)
            {
                activeCandidate = sessionSnapshot.Candidates[idx];
                compareBaseline = FindBaselineFor(sessionSnapshot.Candidates, activeCandidate);
                if (!IsCertified(activeCandidate))
                    currentState = NotebookWorkspaceState.Refused;
                else if (currentState == NotebookWorkspaceState.Refused)
                    currentState = NotebookWorkspaceState.Iterated;
                ApplyUiForCurrentState();
            }
        };

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int idx = i;
            tabButtons[i].Click += (_, _) => SelectTab(idx);
            tabButtons[i].KeyDown += (_, args) => HandleTabKeyDown(idx, args);
        }

        CompareToggleButton.IsCheckedChanged += (_, _) => RefreshArtifactViews();
        TableTwinToggleButton.IsCheckedChanged += (_, _) => RefreshArtifactViews();
    }

    private void HandleTabKeyDown(int currentIdx, KeyEventArgs args)
    {
        int next = currentIdx;
        switch (args.Key)
        {
            case Key.Right:
                next = (currentIdx + 1) % tabButtons.Length;
                break;
            case Key.Left:
                next = (currentIdx - 1 + tabButtons.Length) % tabButtons.Length;
                break;
            case Key.Home:
                next = 0;
                break;
            case Key.End:
                next = tabButtons.Length - 1;
                break;
            default:
                return;
        }
        args.Handled = true;
        SelectTab(next);
        tabButtons[next].Focus();
    }

    private void BuildTunerChips()
    {
        TunerChipsPanel.Children.Clear();
        foreach (string prompt in NotebookCopy.QuickTunerPrompts)
        {
            var chip = new Button
            {
                Content = prompt,
                Classes = { "sweep-chip" }
            };
            chip.Click += (_, _) =>
            {
                PromptInputBox.Text = prompt;
                PromptInputBox.Focus();
            };
            TunerChipsPanel.Children.Add(chip);
        }
    }

    private void CancelActiveRun()
    {
        try { activeRunCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private void AppendGrillMutation(
        FoilReqDocument nextDoc,
        string questionId,
        string action,
        string? selectedOptionId,
        string? customAnswer)
    {
        currentDoc = nextDoc;
        currentValidation = FoilReqValidator.Validate(nextDoc);

        string? parentReqId = sessionSnapshot?.Requirements.Count > 0
            ? sessionSnapshot.Requirements[^1].ReqVersionId
            : null;
        int nextOrdinal = sessionSnapshot?.Requirements.Count ?? 0;
        var reqVer = NotebookRecords.CreateRequirementVersion(nextDoc, $"v1.{nextOrdinal}", parentReqId);

        grillHistory.Add(new GrillResolution(
            QuestionId: questionId,
            Action: action,
            SelectedOptionId: selectedOptionId,
            CustomAnswer: customAnswer,
            ResultingReqVersionId: reqVer.ReqVersionId,
            TimestampUtc: DateTimeOffset.UtcNow));

        currentGrillState = GrillTreeEngine.EvaluateState(currentDoc, currentValidation, grillHistory);

        if (sessionSnapshot is not null)
        {
            var turn = new NotebookTurnRecord(
                Guid.NewGuid().ToString("D"),
                DateTimeOffset.UtcNow,
                action == "reopen" ? "grill-reopen" : "grill-resolution",
                reqVer.ReqVersionId,
                null,
                NotebookRecords.ChunkText($"Grill {action} ({questionId}): {customAnswer ?? selectedOptionId ?? action}"));

            sessionSnapshot = NotebookRecords.FinalizeSession(sessionSnapshot with
            {
                Requirements = [.. sessionSnapshot.Requirements, reqVer],
                Turns = [.. sessionSnapshot.Turns, turn]
            });

            if (controller is not null)
            {
                controller.ActiveSession.RecordNotebookSession(sessionSnapshot);
                bool markedStale = action == "reopen" && sessionSnapshot.Candidates.Count > 0;
                controller.ActiveSession.RecordNotebookEvent("notebook.grill.step", "ok", 0,
                    new NotebookTelemetry("notebook.grill.step", GrillAction: action, OpenCount: currentGrillState.OpenCount, MarkedStale: markedStale));
            }
        }

        if (currentGrillState.ActiveQuestion is not null)
            currentState = NotebookWorkspaceState.Grill;

        ApplyUiForCurrentState();
    }

    private async Task<NotebookConsentDecision> RequestConsentAsync(
        PreparedAssistantCall prepared,
        AssistantValidationInput validationInput)
    {
        if (ConsentHandler is not null)
            return await ConsentHandler(prepared, validationInput);

        if (controller is null)
            return new NotebookConsentDecision(false, null);

        var dlg = new AssistantConsentPreviewDialog(controller.Assistant, prepared, validationInput);
        if (TopLevel.GetTopLevel(this) is Window owner)
            await dlg.ShowDialog(owner);

        // If user clicked Send inside the dialog, reuse the verified wire SHA-256.
        if (dlg.ExecutionResult is not null && dlg.Accepted)
            return new NotebookConsentDecision(true, prepared.Payload.PayloadSha256, dlg.ExecutionResult);

        return new NotebookConsentDecision(false, null);
    }

    private NotebookOrchestrator EnsureOrchestrator()
    {
        if (orchestrator is not null) return orchestrator;
        if (controller is null) throw new InvalidOperationException("NotebookWorkspaceView is not bound to a WorkbenchController.");
        orchestrator = new NotebookOrchestrator(controller.Assistant.Service, customSolverRunner);
        return orchestrator;
    }

    private void ApplyUiForCurrentState()
    {
        bool isNoKey = currentState == NotebookWorkspaceState.NoKey;
        bool isSimulating = currentState == NotebookWorkspaceState.Simulating;
        bool isEmpty = currentState == NotebookWorkspaceState.Empty;
        bool isGrill = currentState == NotebookWorkspaceState.Grill;
        bool isRefused = currentState == NotebookWorkspaceState.Refused;
        bool hasCertifiedActive = activeCandidate is not null && IsCertified(activeCandidate);

        // Session Title & Subtitle
        SessionTitleText.Text = sessionSnapshot is not null
            ? $"{NotebookCopy.EntryTitle} · {sessionSnapshot.Title}"
            : NotebookCopy.EntryTitle;
        SessionSubtitleText.Text = currentDoc is not null
            ? $"Guided Design Notebook · FoilReq {currentDoc.Version} · {currentDoc.Brief.Discipline}"
            : "Guided Design Notebook · FoilReq 1.0";

        // Phase stepper pills
        SetPhasePill(PhaseBriefPill, active: isEmpty || isNoKey, reached: !isEmpty && !isNoKey);
        SetPhasePill(PhaseGrillPill, active: isGrill, reached: !isEmpty && !isNoKey && !isGrill);
        SetPhasePill(PhaseIteratePill, active: isSimulating || currentState is NotebookWorkspaceState.Iterated or NotebookWorkspaceState.Refused or NotebookWorkspaceState.Overflow, reached: hasCertifiedActive);
        SetPhasePill(PhaseCadPill, active: false, reached: sessionSnapshot?.Handoffs.Count > 0);

        // Candidate selector & sticky Open in CAD Mode -> (STRICT GeometryStatus.Certified gate!)
        SyncCandidateCombo();
        OpenInCadButton.IsEnabled = hasCertifiedActive && !isSimulating && !isNoKey;
        AutomationProperties.SetItemStatus(OpenInCadButton, OpenInCadButton.IsEnabled ? "" : "disabled");
        OpenInCadButton.Classes.Set("disabled", !OpenInCadButton.IsEnabled);
        CadDisabledNoteBar.IsVisible = !OpenInCadButton.IsEnabled;
        CadDisabledNoteText.Text = NotebookCopy.OpenInCadDisabledReason;

        // Left pane cards
        NoKeyBannerCard.IsVisible = isNoKey;
        EmptyWelcomeCard.IsVisible = isEmpty;
        SimulatingCard.IsVisible = isSimulating;

        // Composer enablement (disabled in no-key and simulating!)
        bool composerEnabled = !isNoKey && !isSimulating;
        PromptInputBox.IsEnabled = composerEnabled;
        SendPromptButton.IsVisible = !isSimulating;
        SendPromptButton.IsEnabled = composerEnabled;
        StopRunButton.IsVisible = isSimulating;
        PreviewPayloadButton.IsEnabled = !isNoKey && !isSimulating;
        foreach (var child in TunerChipsPanel.Children.OfType<Control>())
            child.IsEnabled = composerEnabled;

        // Pinned FoilReq card
        RenderPinnedFoilReq();

        // Chronological turns stream
        RenderTurnsStream();

        // Grill summary pills + active serial grill card
        RenderGrillUi(isGrill);

        // Refused card
        RenderRefusedCard(isRefused);

        // Right pane artifacts, readouts, provenance
        RefreshArtifactViews();
    }

    private void SyncCandidateCombo()
    {
        syncingCandidateCombo = true;
        try
        {
            if (sessionSnapshot is null || sessionSnapshot.Candidates.Count == 0)
            {
                CandidateSelectorCombo.ItemsSource = new[] { "No candidates yet" };
                CandidateSelectorCombo.SelectedIndex = 0;
                CandidateSelectorCombo.IsEnabled = false;
                CompareToggleButton.IsEnabled = false;
            }
            else
            {
                var items = sessionSnapshot.Candidates.Select(c =>
                {
                    string statusTag = IsCertified(c) ? "Checked" : (c.RefusalCode ?? c.GeometryStatus);
                    return string.Create(Inv, $"{c.CandidateId} · {c.Delta.SpanMeters * 1000:0} mm · {statusTag}");
                }).ToArray();
                CandidateSelectorCombo.ItemsSource = items;
                int idx = activeCandidate is not null
                    ? sessionSnapshot.Candidates.ToList().FindIndex(c => c.CandidateId == activeCandidate.CandidateId)
                    : sessionSnapshot.Candidates.Count - 1;
                CandidateSelectorCombo.SelectedIndex = Math.Max(0, idx);
                CandidateSelectorCombo.IsEnabled = true;
                CompareToggleButton.IsEnabled = sessionSnapshot.Candidates.Count >= 2;
            }
        }
        finally
        {
            syncingCandidateCombo = false;
        }
    }

    private void RenderPinnedFoilReq()
    {
        if (currentDoc is null)
        {
            FoilReqPinCard.IsVisible = false;
            return;
        }

        FoilReqPinCard.IsVisible = true;
        string verLabel = sessionSnapshot?.Requirements.Count > 0 ? sessionSnapshot.Requirements[^1].OrdinalLabel : "v1.0";
        FoilReqPinTitleText.Text = $"FoilReq 1.0 ({verLabel})";
        FoilReqDrawerBox.Text = FoilReqSerializer.Format(currentDoc);

        FoilReqChipsPanel.Children.Clear();
        AddReqChip($"Discipline: {currentDoc.Brief.Discipline}", warning: false);
        if (currentDoc.Rider.Find("mass") is { } m)
            AddReqChip($"Rider: {m.Quantity.Format()} ({m.Tag.Kind})", warning: false);
        if (currentDoc.FindConstraint("max_span") is { Quantity: { } ms } mc)
            AddReqChip($"Span {mc.Comparator} {ms.Format()}", warning: mc.Tag.Kind == "@flagged");
        if (currentDoc.FindConstraint("target_ar") is { Quantity: { } ar } arc)
            AddReqChip($"AR {arc.Comparator} {ar.Format()}", warning: arc.Tag.Kind == "@flagged");
        if (currentDoc.References.Count > 0)
            AddReqChip($"Ref: {currentDoc.References[0].Name} ({currentDoc.References[0].Confidence})", warning: true);
    }

    private void AddReqChip(string text, bool warning)
    {
        var tb = new TextBlock { Text = text };
        var border = new Border { Classes = { "prop-chip" }, Child = tb };
        if (warning)
        {
            border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("WarningBrush"));
            tb.Bind(TextBlock.ForegroundProperty, tb.GetResourceObservable("WarningBrush"));
        }
        FoilReqChipsPanel.Children.Add(border);
    }

    private void RenderTurnsStream()
    {
        TurnsListPanel.Children.Clear();
        if (sessionSnapshot is null) return;

        // If FoilReqValidator flagged a unit sanity issue, surface approved COPY-926 prominently.
        if (currentValidation?.Findings.FirstOrDefault(f => f.Code == FoilReqValidator.UnitSanityCode) is { } unitFinding)
        {
            string rawSpan = currentDoc?.FindConstraint("max_span")?.Quantity?.Format() ?? "950 cm";
            string suggested = unitFinding.SuggestedValue ?? "950 mm";
            string converted = currentDoc?.FindConstraint("max_span")?.Quantity is { } q && q.TryToMeters(out double m)
                ? string.Create(Inv, $"{m * 1000:N0} mm")
                : "9,500 mm";
            TurnsListPanel.Children.Add(BuildNoticeCard(
                "Deterministic unit check",
                NotebookCopy.UnitSanityAlert(rawSpan, converted, suggested),
                "WarningBrush"));
        }

        // Reference disclosure (COPY-927) whenever a commercial foil reference is present.
        if (currentDoc is { References.Count: > 0 })
        {
            TurnsListPanel.Children.Add(BuildNoticeCard(
                $"Reference anchor: {currentDoc.References[0].Name}",
                NotebookCopy.ReferenceDisclosure,
                "WarningBrush"));
        }

        foreach (var turn in sessionSnapshot.Turns)
        {
            string body = NotebookRecords.JoinChunks(turn.BodyLines);
            if (string.IsNullOrWhiteSpace(body)) continue;

            string roleLabel = turn.Kind switch
            {
                "user-prompt" => "Rider brief",
                "foilreq-decomp" => "FoilReq 1.0 decomposition",
                "grill-resolution" => "Grill resolution",
                "grill-reopen" => "Branch re-opened (TQ9)",
                "candidate-evaluated" => $"Local solver receipt ({turn.CandidateId})",
                "candidate-refused" => $"Geometry refused ({turn.CandidateId})",
                "ai-translation" => $"AI riding translation ({turn.CandidateId})",
                "withheld" => "Response withheld (AI-03 / LAB-01)",
                _ => turn.Kind
            };

            string borderKey = turn.Kind switch
            {
                "user-prompt" => "ControlLineBrush",
                "candidate-refused" => "DangerBrush",
                "withheld" or "grill-reopen" => "WarningBrush",
                _ => "LineBrush"
            };
            string bgKey = turn.Kind == "user-prompt" ? "SelectionBrush" : "SurfaceBrush";
            string fgKey = turn.Kind == "user-prompt" ? "OnSelectionBrush" : "InkBrush";

            var titleTb = new TextBlock { Text = roleLabel, Classes = { "pnl-head" } };
            titleTb.Bind(TextBlock.ForegroundProperty, titleTb.GetResourceObservable(fgKey));

            var bodyTb = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Classes = { "pnl-cell" } };
            bodyTb.Bind(TextBlock.ForegroundProperty, bodyTb.GetResourceObservable(fgKey));

            var stack = new StackPanel { Spacing = 4, Children = { titleTb, bodyTb } };
            var card = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
                Child = stack
            };
            card.Bind(Border.BackgroundProperty, card.GetResourceObservable(bgKey));
            card.Bind(Border.BorderBrushProperty, card.GetResourceObservable(borderKey));
            TurnsListPanel.Children.Add(card);
        }
    }

    private Border BuildNoticeCard(string heading, string body, string brushKey)
    {
        var headTb = new TextBlock { Text = heading, Classes = { "pnl-head" } };
        headTb.Bind(TextBlock.ForegroundProperty, headTb.GetResourceObservable(brushKey));
        var bodyTb = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Classes = { "pnl-cell" } };
        var card = new Border
        {
            BorderThickness = new Thickness(3, 1, 1, 1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8),
            Child = new StackPanel { Spacing = 4, Children = { headTb, bodyTb } }
        };
        card.Bind(Border.BackgroundProperty, card.GetResourceObservable("SurfaceBrush"));
        card.Bind(Border.BorderBrushProperty, card.GetResourceObservable(brushKey));
        return card;
    }

    private void RenderGrillUi(bool isGrill)
    {
        // Completed/skipped summary pills (clickable to re-open branch -> TQ9 stale)
        var completedPills = grillHistory
            .Where(h => h.Action is "answer" or "skip")
            .GroupBy(h => h.QuestionId)
            .Select(g => g.Last())
            .ToArray();

        GrillSummaryPanel.Children.Clear();
        GrillSummaryCard.IsVisible = completedPills.Length > 0;
        foreach (var item in completedPills)
        {
            string label = $"{item.QuestionId}: {item.SelectedOptionId ?? item.CustomAnswer ?? item.Action}";
            var pill = new Button
            {
                Content = label,
                Classes = { "sweep-chip" }
            };
            AutomationProperties.SetName(pill, $"Re-open grill branch {item.QuestionId}");
            string qid = item.QuestionId;
            pill.Click += (_, _) => ReopenGrillBranch(qid);
            GrillSummaryPanel.Children.Add(pill);
        }

        // Active serial Grill-Me card (at most 1 blocking question!)
        var activeQ = currentGrillState?.ActiveQuestion;
        ActiveGrillCard.IsVisible = isGrill && activeQ is not null;
        if (!ActiveGrillCard.IsVisible || activeQ is null) return;

        GrillBranchBadgeText.Text = string.Create(Inv, $"Q{activeQ.Ordinal} of {activeQ.TotalEstimated}");
        GrillCategoryText.Text = activeQ.Category;
        GrillQuestionPromptText.Text = activeQ.Prompt;
        GrillNeededForText.Text = "Needed for: " + string.Join(" · ", activeQ.NeededFor);
        GrillGenerateNowButton.Content = string.Create(Inv, $"{NotebookCopy.GenerateNowButton} ({currentGrillState!.OpenCount} open)");

        GrillOptionsPanel.Children.Clear();
        foreach (var opt in activeQ.Options)
        {
            var topGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var labelTb = new TextBlock { Text = opt.Label, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(labelTb, 0);
            topGrid.Children.Add(labelTb);

            if (opt.IsSuggestedDefault)
            {
                var badgeText = new TextBlock { Text = NotebookCopy.SuggestedDefaultPill, FontWeight = FontWeight.SemiBold };
                var badge = new Border { Classes = { "prop-chip" }, Child = badgeText };
                Grid.SetColumn(badge, 1);
                topGrid.Children.Add(badge);
            }

            var impactTb = new TextBlock { Text = opt.ImpactPreview, Classes = { "pnl-note" } };
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = new StackPanel { Spacing = 2, Children = { topGrid, impactTb } }
            };
            if (opt.IsSuggestedDefault)
                btn.Bind(Button.BorderBrushProperty, btn.GetResourceObservable("PrimaryBrush"));

            string optId = opt.OptionId;
            btn.Click += (_, _) => AnswerActiveGrill(optId);
            GrillOptionsPanel.Children.Add(btn);
        }
    }

    private void RenderRefusedCard(bool isRefused)
    {
        bool show = isRefused || (activeCandidate is not null && !IsCertified(activeCandidate));
        RefusedCard.IsVisible = show;
        if (!show) return;

        string candId = activeCandidate?.CandidateId ?? "candidate";
        string code = activeCandidate?.RefusalCode ?? activeCandidate?.GeometryStatus ?? "REFUSED";
        RefusedCardTitleText.Text = $"Candidate {candId} refused ({code})";
        RefusedCardBodyText.Text =
            $"Deterministic geometry check refused {candId} with {code}. Open in CAD Mode stays disabled for non-checked candidates; prior checked candidates remain intact.";

        WithheldBannerBox.IsVisible = !string.IsNullOrWhiteSpace(activeWithheldText);
        if (WithheldBannerBox.IsVisible)
            WithheldBannerText.Text = activeWithheldText!;

        bool canRepair = activeRepairAttempts < NotebookOrchestrator.MaxRepairAttempts;
        AskAiToRepairButton.IsEnabled = canRepair;
        AskAiToRepairButton.Content = canRepair
            ? string.Create(Inv, $"{NotebookCopy.AskAiToRepairButton} ({activeRepairAttempts}/{NotebookOrchestrator.MaxRepairAttempts})")
            : $"Repair cap reached ({NotebookOrchestrator.MaxRepairAttempts}/{NotebookOrchestrator.MaxRepairAttempts})";

        var priorChecked = sessionSnapshot?.Candidates.LastOrDefault(IsCertified);
        SwitchToCheckedBaselineButton.IsVisible = priorChecked is not null;
        if (priorChecked is not null)
            SwitchToCheckedBaselineButton.Content = $"Inspect checked {priorChecked.CandidateId}";
    }

    private void RefreshArtifactViews()
    {
        UpdateArtifactStageVisibility();

        // Display candidate: when a Refused candidate is selected, keep showing the last checked baseline (v1) underneath the refusal banner
        // while keeping Open in CAD Mode strictly DISABLED!
        CandidateIterationRecord? displayCandidate = activeCandidate is not null && IsCertified(activeCandidate)
            ? activeCandidate
            : sessionSnapshot?.Candidates.LastOrDefault(IsCertified);

        bool refusedSelected = activeCandidate is not null && !IsCertified(activeCandidate);
        RefusedArtifactBanner.IsVisible = refusedSelected;
        if (refusedSelected && activeCandidate is not null)
        {
            RefusedArtifactBannerText.Text =
                $"Selected candidate {activeCandidate.CandidateId} failed geometry check ({activeCandidate.RefusalCode ?? activeCandidate.GeometryStatus}). " +
                (displayCandidate is not null
                    ? $"Displaying last geometry-checked baseline ({displayCandidate.CandidateId}) below; Open in CAD Mode remains disabled."
                    : NotebookCopy.OpenInCadDisabledReason);
        }

        CandidateIterationRecord? baselineForCompare = CompareEnabled && compareBaseline is not null &&
            displayCandidate is not null && !string.Equals(compareBaseline.CandidateId, displayCandidate.CandidateId, StringComparison.Ordinal)
                ? compareBaseline
                : null;

        if (displayCandidate is null || currentDoc is null)
        {
            UpdateMetricStrip(null, null);
            NotebookScorecardViewBindEmpty();
            UpdateProvenanceStrip(null, currentDoc);
            return;
        }

        string latestReqId = sessionSnapshot?.Requirements.Count > 0 ? sessionSnapshot.Requirements[^1].ReqVersionId : displayCandidate.ReqVersionId;
        var scorecard = RequirementScorecard.Evaluate(currentDoc, latestReqId, displayCandidate, baselineForCompare, latestReqId);

        UpdateMetricStrip(displayCandidate, scorecard);
        UpdateProvenanceStrip(displayCandidate, currentDoc);

        // Tab 1: 2D Section & Cp
        BindSectionTab(displayCandidate, baselineForCompare, currentDoc);

        // Tab 2: Polars & Curves
        BindPolarsTab(displayCandidate, baselineForCompare, currentDoc);

        // Tab 3: Plan & Elevations
        BindElevationsTab(displayCandidate, baselineForCompare, currentDoc);

        // Tab 4: 3D Wing
        BindWing3dTab(displayCandidate);

        // Tab 5: Scorecard & FoilReq
        string verLabel = sessionSnapshot?.Requirements.Count > 0 ? sessionSnapshot.Requirements[^1].OrdinalLabel : "v1.0";
        ScorecardTabPanel.Bind(scorecard, FoilReqSerializer.Format(currentDoc), verLabel);
    }

    private void UpdateArtifactStageVisibility()
    {
        bool isSimulating = currentState == NotebookWorkspaceState.Simulating;
        bool hasDisplayCandidate = (activeCandidate is not null && IsCertified(activeCandidate)) ||
                                   (sessionSnapshot?.Candidates.Any(IsCertified) == true);

        SimulatingSkeletonPanel.IsVisible = isSimulating;
        ArtifactEmptyPlaceholder.IsVisible = !isSimulating && !hasDisplayCandidate && activeTabIndex != TabScorecard;

        bool showTabs = !isSimulating && (hasDisplayCandidate || activeTabIndex == TabScorecard);
        SectionTabPanel.IsVisible = showTabs && activeTabIndex == TabSection;
        PolarsTabPanel.IsVisible = showTabs && activeTabIndex == TabPolars;
        ElevationsTabPanel.IsVisible = showTabs && activeTabIndex == TabElevations;
        Wing3dTabPanel.IsVisible = showTabs && activeTabIndex == TabWing3d;
        ScorecardTabPanel.IsVisible = showTabs && activeTabIndex == TabScorecard;
    }

    private void UpdateMetricStrip(CandidateIterationRecord? cand, RequirementScorecardResult? scorecard)
    {
        if (cand is null || scorecard is null)
        {
            MetricSpanText.Text = "—";
            MetricAreaText.Text = "—";
            MetricArText.Text = "—";
            MetricTakeoffText.Text = "—";
            MetricLdText.Text = "—";
            MetricVCritText.Text = "—";
            return;
        }

        MetricSpanText.Text = string.Create(Inv, $"{scorecard.SpanMm:0.0} mm");
        MetricAreaText.Text = string.Create(Inv, $"{scorecard.AreaCm2:0} cm²");
        MetricArText.Text = string.Create(Inv, $"{scorecard.AspectRatio:0.00}");

        var toOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "takeoff") ?? cand.OperatingPoints.FirstOrDefault();
        MetricTakeoffText.Text = toOp is not null ? string.Create(Inv, $"{toOp.SpeedKn:0.0} kn") : "—";

        var glOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "glide") ?? cand.OperatingPoints.ElementAtOrDefault(1) ?? toOp;
        MetricLdText.Text = glOp?.WingOnlyLd is { } ld ? string.Create(Inv, $"{ld:0.0}") : "Unavailable";

        var drOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "steep_drop") ?? cand.OperatingPoints.LastOrDefault();
        MetricVCritText.Text = drOp is { DepthUnset: false, VCritKn: { } vc }
            ? string.Create(Inv, $"{vc:0.0} kn")
            : "Depth unset";
    }

    private void UpdateProvenanceStrip(CandidateIterationRecord? cand, FoilReqDocument? doc)
    {
        ProvenanceStripText.Text = NotebookCopy.ArtifactsProvenanceStrip;
        if (doc is null)
        {
            ProvenanceBasisText.Text = "15 °C Salt · Ncrit 2–4";
            ProvenanceA51Text.Text = "";
            return;
        }

        string waterKind = string.Equals(doc.Water.Kind, "fresh", StringComparison.OrdinalIgnoreCase) ? "Fresh" : "Salt";
        string depthText = doc.Water.IsDepthUnset ? "h_ref unset (ANA-19)" : $"h_ref {doc.Water.DepthRef!.Format()}";
        ProvenanceBasisText.Text = $"{doc.Water.Temp.Format()} {waterKind} · {depthText} · Ncrit 2–4";

        var drOp = cand?.OperatingPoints.FirstOrDefault(o => o.OpId == "steep_drop") ?? cand?.OperatingPoints.LastOrDefault();
        if (drOp is null)
        {
            ProvenanceA51Text.Text = "";
        }
        else if (drOp.DepthUnset || doc.Water.IsDepthUnset)
        {
            ProvenanceA51Text.Text = RequirementScorecard.DepthUnsetNotice;
        }
        else if (drOp.HOverCRoot is { } hc && hc < 5.0 && drOp.FrH is { } frh)
        {
            ProvenanceA51Text.Text = string.Create(Inv,
                $"Deep-water result; free surface not modelled (h/c = {hc:0.00} < 5, Fr_h = {frh:0.00}) · Governing η = {drOp.GoverningEta ?? 0.0:0.00}");
        }
        else
        {
            ProvenanceA51Text.Text = string.Create(Inv,
                $"Governing station: η = {drOp.GoverningEta ?? 0.0:0.00} · sheet cavitation screen only");
        }
    }

    private void BindSectionTab(CandidateIterationRecord cand, CandidateIterationRecord? baseline, FoilReqDocument doc)
    {
        var drOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "steep_drop") ?? cand.OperatingPoints.LastOrDefault();
        double eta = Math.Clamp(drOp?.GoverningEta ?? 0.0, 0.0, 0.99);
        double sigma = drOp?.Sigma ?? 0.0;
        bool depthUnset = doc.Water.IsDepthUnset || (drOp?.DepthUnset == true) || drOp?.Sigma is null;

        SectionPlateTitleText.Text = $"{cand.CandidateId} · {cand.Delta.RootProfileId} → {cand.Delta.TipProfileId} (−Cp upward)";
        SectionGoverningText.Text = depthUnset
            ? string.Create(Inv, $"Governing station η = {eta:0.00} · Depth unset (ANA-19)")
            : string.Create(Inv, $"Governing η = {eta:0.00} · depth {doc.Water.DepthRef?.Format() ?? "unset"} · σ = {sigma:0.000}");

        // Real PanelMethod 200p solve on the candidate's materialized FoilDSL at the governing station (cached per surface/op/eta).
        PanelResult? panel = GetOrSolvePanel(cand, drOp, eta);
        if (panel is null)
        {
            NotebookSectionProfileView.Model = null;
            NotebookCpChartView.Model = new ChartModel(
                Id: "notebook-cp",
                Title: "Section Cp & Cavitation",
                Plots: [],
                Legend: "",
                Label: "Unavailable — geometry not certified",
                Unavailable: "Section pressure distribution is unavailable for an uncertified candidate.");
            SectionTableTwinPanel.IsVisible = false;
            return;
        }

        PanelCp[] outline = panel.Upper.Concat(panel.Lower).ToArray();
        (PanelCp cpMinPanel, string cpMinSide) = FindCpMinPanel(panel);
        double cpLow = outline.Min(p => p.Cp);
        double cpHigh = outline.Max(p => p.Cp);
        double reportedCpMin = drOp?.CpMin ?? panel.CpMin;
        double? reportedMargin = drOp?.CavMargin ?? (!depthUnset && reportedCpMin < 0.0 ? sigma / -reportedCpMin : null);
        string cavLine = depthUnset
            ? "Cavitation screen: Unavailable — depth not set (ANA-19)"
            : drOp?.VCritKn is { } vc && reportedMargin is { } margin
                ? string.Create(Inv, $"σ = {sigma:0.000} · −Cp_min = {-reportedCpMin:0.00} · margin {margin:0.00} · V_crit {vc:0.0} kn")
                : string.Create(Inv, $"σ = {sigma:0.000} · −Cp_min = {-reportedCpMin:0.00}");

        NotebookSectionProfileView.Model = new SectionProfile(
            Outline: outline,
            CpMinPanel: cpMinPanel,
            Side: cpMinSide,
            CpLow: cpLow,
            CpHigh: cpHigh,
            Caption: string.Create(Inv, $"{cand.CandidateId} η={eta:0.00} · {cand.Delta.RootProfileId} → {cand.Delta.TipProfileId}"),
            Tier: $"PanelMethod {panel.StationCount}p · {PanelMethod.ModelLabel}",
            Cavitation: cavLine);

        // Real -Cp vs x/c chart (InvertY = true so -Cp points upward) with sigma cavitation threshold series.
        var upperSeriesPts = panel.Upper
            .Select(p => new ChartPoint(p.X, p.Cp))
            .OrderBy(p => p.X)
            .ToArray();
        var lowerSeriesPts = panel.Lower
            .Select(p => new ChartPoint(p.X, p.Cp))
            .OrderBy(p => p.X)
            .ToArray();

        var series = new List<ChartSeries>
        {
            new($"{cand.CandidateId} upper Cp", upperSeriesPts, Dashed: false, Marker: "none", Colour: 0),
            new($"{cand.CandidateId} lower Cp", lowerSeriesPts, Dashed: true, Marker: "none", Colour: 3)
        };

        if (!depthUnset)
        {
            series.Add(new ChartSeries(
                string.Create(Inv, $"-σ ({sigma:0.00})"),
                [new ChartPoint(0.0, -sigma), new ChartPoint(1.0, -sigma)],
                Dashed: true,
                Marker: "none",
                Colour: 2));
        }

        if (baseline is not null)
        {
            var baseDrop = baseline.OperatingPoints.FirstOrDefault(o => o.OpId == "steep_drop") ?? baseline.OperatingPoints.LastOrDefault();
            double baseEta = Math.Clamp(baseDrop?.GoverningEta ?? eta, 0.0, 0.99);
            PanelResult? basePanel = GetOrSolvePanel(baseline, baseDrop, baseEta);
            if (basePanel is not null)
            {
                var basePts = basePanel.Upper.Select(p => new ChartPoint(p.X, p.Cp)).OrderBy(p => p.X).ToArray();
                series.Add(new ChartSeries($"{baseline.CandidateId} upper Cp", basePts, Dashed: true, Marker: "none", Colour: 1));
            }
        }

        var markers = new List<ChartMarker>
        {
            new(string.Create(Inv, $"Cp_min {panel.CpMin:0.00}"), cpMinPanel.X, panel.CpMin)
        };

        var plot = new ChartPlot(
            Title: "Pressure coefficient (−Cp upward)",
            XTitle: "x/c",
            YTitle: "Cp",
            InvertY: true,
            Series: series,
            Markers: markers);

        NotebookCpChartView.Model = new ChartModel(
            Id: "notebook-cp",
            Title: "Section Cp & Cavitation",
            Plots: [plot],
            Legend: "Upper solid · Lower dashed · -σ threshold",
            Label: cavLine);

        SectionTableTwinPanel.IsVisible = TableTwinEnabled;
        if (TableTwinEnabled)
        {
            var sb = new StringBuilder();
            sb.AppendLine("x/c     Upper Cp   Lower Cp");
            int step = Math.Max(1, upperSeriesPts.Length / 10);
            for (int i = 0; i < upperSeriesPts.Length; i += step)
            {
                double x = upperSeriesPts[i].X;
                double cu = upperSeriesPts[i].Y;
                double cl = i < lowerSeriesPts.Length ? lowerSeriesPts[i].Y : 0.0;
                sb.AppendLine(string.Create(Inv, $"{x:0.000}   {cu,8:0.000}   {cl,8:0.000}"));
            }
            SectionTableTwinText.Text = sb.ToString().TrimEnd();
        }
    }

    private void BindPolarsTab(CandidateIterationRecord cand, CandidateIterationRecord? baseline, FoilReqDocument? doc)
    {
        var seriesList = new List<SweepCurveSeries>();
        var tableRows = new List<SweepCaseRow>();

        AddCandidatePolarSeries(cand, strokeIdx: 0, seriesList, tableRows);
        if (baseline is not null)
            AddCandidatePolarSeries(baseline, strokeIdx: 1, seriesList, tableRows);

        var chartData = new SweepChartData(
            Metric: SweepCurveMetric.LdWing,
            XAxis: SweepXAxis.Speed,
            Title: NotebookCopy.WingOnlyLdLabel,
            Subtitle: "Local VLM + Strip · Ncrit 2–4 band",
            XAxisLabel: "Speed (kn)",
            YAxisLabel: NotebookCopy.WingOnlyLdLabel,
            UnavailableReason: seriesList.Count == 0 ? "No operating points computed." : null,
            Series: seriesList,
            TableRows: tableRows,
            GoalBandNote: RequirementScorecard.WingOnlyNotice);

        NotebookPerformanceChart.Update(chartData, selectedCaseIndex: 0);

        // Populate reused LoadingChart from real ProductWingMethod (VLM Solve + Couple) spanwise strips.
        var glOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "glide") ?? cand.OperatingPoints.FirstOrDefault();
        var loadingPts = GetOrSolveSpanwiseLoading(cand, glOp, doc);
        NotebookLoadingChart.Update(loadingPts, Units.Metric);
        NotebookLoadingChart.TwinToggle.IsChecked = TableTwinEnabled;

        PolarsTableTwinPanel.IsVisible = TableTwinEnabled;
        if (TableTwinEnabled)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Op          Speed(kn)  Alpha(deg)    CL      Wing-only L/D   Re_MAC       CavMargin");
            foreach (var op in cand.OperatingPoints)
            {
                string ld = op.WingOnlyLd is { } v ? v.ToString("0.0", Inv) : "Unavail";
                string cm = !op.DepthUnset && op.CavMargin is { } m ? m.ToString("0.00", Inv) : "Depth unset";
                sb.AppendLine(string.Create(Inv,
                    $"{op.OpId,-10}  {op.SpeedKn,8:0.0}   {op.AlphaDeg,9:0.00}  {op.Cl,6:0.000}   {ld,13}   {op.ReMac,10:0.00e+0}   {cm}"));
            }
            PolarsTableTwinText.Text = sb.ToString().TrimEnd();
        }
    }

    private void BindElevationsTab(CandidateIterationRecord cand, CandidateIterationRecord? baseline, FoilReqDocument doc)
    {
        var d = cand.Delta;
        var recipe = new RecipeRecord(
            SolverVersion: cand.RecipeSeederVersion,
            PlanformFamily: RecipeSeeder.PlanformFamily,
            SpanMeters: d.SpanMeters,
            RootChordMeters: d.RootChordMeters,
            TipChordMeters: d.TipChordMeters,
            Taper: d.Taper,
            SweepQuarterChordDeg: d.SweepQuarterChordDeg,
            AnhedralDeg: d.AnhedralDeg,
            WashoutDeg: d.WashoutDeg,
            RootThicknessRatio: d.RootThicknessRatio,
            ProfileId: d.RootProfileId,
            ProfileOrigin: "gen:" + d.RootProfileId,
            PreferredProfileId: d.RootProfileId,
            FallbackReason: null,
            ConflictNotice: null,
            Deviations: []);

        WingEstimates? priorEst = baseline is not null
            ? new WingEstimates(
                SpanMeters: baseline.Delta.SpanMeters,
                RootChordMeters: baseline.Delta.RootChordMeters,
                TipChordMeters: baseline.Delta.TipChordMeters,
                MeanChordMeters: baseline.Delta.AreaSquareMeters / Math.Max(1e-6, baseline.Delta.SpanMeters),
                MacMeters: baseline.Delta.MacMeters,
                MaxThicknessRatio: baseline.Delta.RootThicknessRatio,
                AspectRatio: baseline.Delta.AspectRatio,
                AreaSquareMeters: baseline.Delta.AreaSquareMeters,
                Basis: "notebook",
                Generation: baseline.Ordinal,
                Converged: true,
                Compute: new EstimateCompute("notebook", 0, 1, 1, "ok"))
            : null;

        PlanformPlateTitleText.Text = string.Create(Inv,
            $"{cand.CandidateId} Top planform · span {d.SpanMeters * 1000:0.0} mm · root {d.RootChordMeters * 1000:0.0} mm · tip {d.TipChordMeters * 1000:0.0} mm");
        NotebookPlanformPreview.Update(recipe, priorEst);

        var drOp = cand.OperatingPoints.FirstOrDefault(o => o.OpId == "steep_drop") ?? cand.OperatingPoints.LastOrDefault();
        bool depthUnset = doc.Water.IsDepthUnset || (drOp?.DepthUnset == true);
        SurfaceView? activeSurf = GetOrBuildSurface(cand);
        SurfaceView? baselineSurf = baseline is not null ? GetOrBuildSurface(baseline) : null;
        NotebookElevationView.Update(activeSurf, baselineSurf, d, drOp?.HOverCRoot, drOp?.FrH, depthUnset);

        if (depthUnset)
        {
            ElevationsA51Banner.IsVisible = true;
            ElevationsA51BannerText.Text = "A5.1 / ANA-19: Free-surface h/c and Fr_h Unavailable — depth not set.";
        }
        else if (drOp?.HOverCRoot is { } hc && hc < 5.0 && drOp.FrH is { } fr)
        {
            ElevationsA51Banner.IsVisible = true;
            ElevationsA51BannerText.Text = string.Create(Inv,
                $"A5.1 free-surface notice: h/c_root = {hc:0.00} < 5 (Fr_h = {fr:0.00}); free-surface effects are unmodelled in this deep-water local calculation.");
        }
        else
        {
            ElevationsA51Banner.IsVisible = false;
        }
    }

    private void BindWing3dTab(CandidateIterationRecord cand)
    {
        SurfaceView? surf = GetOrBuildSurface(cand);
        Wing3dTabPanel.SetSurface(surf, surf is not null ? cand.CandidateId : null);
    }

    private SurfaceView? GetOrBuildSurface(CandidateIterationRecord cand)
    {
        if (surfaceCache.TryGetValue(cand.SurfaceHash, out var cached))
            return cached;
        try
        {
            byte[] bytes = NotebookRecords.MaterializeCandidateDsl(cand.Delta, $"Notebook {cand.CandidateId}");
            var surf = Placement.Surface(bytes, cand.SurfaceHash, cand.Ordinal, CancellationToken.None);
            surfaceCache[cand.SurfaceHash] = surf;
            return surf;
        }
        catch
        {
            return null;
        }
    }

    private PanelResult? GetOrSolvePanel(CandidateIterationRecord cand, CandidateOperatingPointSummary? op, double eta)
    {
        string opId = op?.OpId ?? "default";
        double roundedEta = Math.Round(eta, 4);
        var key = (cand.SurfaceHash, opId, roundedEta);
        if (panelCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            byte[] dsl = NotebookRecords.MaterializeCandidateDsl(cand.Delta, $"Notebook {cand.CandidateId}");
            var samples = PanelMethod.SampleSections(dsl, [eta], PanelMethod.DefaultPanelCount, CancellationToken.None);
            double alphaDeg = op?.AlphaDeg ?? 2.0;
            PanelResult result = PanelMethod.Solve(samples[0], alphaDeg, CancellationToken.None);
            panelCache[key] = result;
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static (PanelCp At, string Side) FindCpMinPanel(PanelResult panel)
    {
        PanelCp best = panel.Upper.Count > 0 ? panel.Upper[0] : default;
        double lowest = double.PositiveInfinity;
        string side = "upper";
        int skip = PanelMethod.CpMinTrailingEdgePanelsPerSide;
        for (int i = skip; i < panel.Upper.Count; i++)
        {
            if (panel.Upper[i].Cp < lowest)
            {
                lowest = panel.Upper[i].Cp;
                best = panel.Upper[i];
                side = "upper";
            }
        }
        for (int i = 0; i < panel.Lower.Count - skip; i++)
        {
            if (panel.Lower[i].Cp < lowest)
            {
                lowest = panel.Lower[i].Cp;
                best = panel.Lower[i];
                side = "lower";
            }
        }
        return (best, side);
    }

    private IReadOnlyList<LoadingPoint> GetOrSolveSpanwiseLoading(
        CandidateIterationRecord cand,
        CandidateOperatingPointSummary? glOp,
        FoilReqDocument? doc)
    {
        string opId = glOp?.OpId ?? "glide";
        var key = (cand.SurfaceHash, opId);
        if (loadingCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            byte[] dsl = NotebookRecords.MaterializeCandidateDsl(cand.Delta, $"Notebook {cand.CandidateId}");
            var settings = wingMethod.Settings;
            var etas = settings.SectionEtas;
            var xs = settings.SectionXs;
            if (etas is null || xs is null) return [];

            var sections = Placement.Sections(dsl, etas, xs, CancellationToken.None);
            var reference = wingMethod.Reference(dsl);

            double tempC = doc?.Water.Temp.TryToCelsius(out double tc) == true ? Math.Clamp(tc, 0.0, 50.0) : 15.0;
            double salinity = doc is not null && string.Equals(doc.Water.Kind, "fresh", StringComparison.OrdinalIgnoreCase)
                ? 0.0
                : OperatingPoints.SaltSalinityGPerKg;
            WaterRecord water = WaterTable.At(tempC, salinity);

            double pAtm = doc?.Water.PAtm is { } pQty && pQty.TryToPa(out double pa) ? pa : DisciplinePresets.StandardAtmospherePa;
            double? hRef = doc is not null && !doc.Water.IsDepthUnset && doc.Water.DepthRef is { } dQty && dQty.TryToMeters(out double dm) && dm > 0.0
                ? dm
                : null;
            double speedMps = (glOp?.SpeedKn ?? 16.0) * DisciplinePresets.KnotsToMps;
            double alphaDeg = glOp?.AlphaDeg ?? 2.0;
            double targetLoadN = doc is not null ? FoilReqValidator.Validate(doc).ImpliedLoadNewtons ?? 1050.0 : 1050.0;

            var op = new OperatingPoint(speedMps, pAtm, hRef, DisciplinePresets.RootLeadingEdgeDatum, alphaDeg, targetLoadN);
            var solution = wingMethod.Solve(sections, op, water, CancellationToken.None);
            var strips = wingMethod.Couple(dsl, sections, solution, op, water, CancellationToken.None);

            double qDyn = 0.5 * water.Rho * speedMps * speedMps;
            double totalLift = strips.Sum(s => s.Fz);
            double cl = reference.SRef > 1e-9 && qDyn > 1e-9 ? totalLift / (qDyn * reference.SRef) : (glOp?.Cl ?? 0.0);
            (double sinA, double cosA) = double.SinCosPi(alphaDeg / 180.0);

            var points = strips
                .Where(s => s.Y >= 0.0)
                .OrderBy(s => s.Eta)
                .Select(s =>
                {
                    double? clOverCRef = reference.CRef > 0.0 ? s.ClLocal * s.Chord / reference.CRef : null;
                    double elliptic = cl * (4.0 / Math.PI) * Math.Sqrt(Math.Max(0.0, 1.0 - s.Eta * s.Eta));
                    double? width = s.YLow.HasValue && s.YHigh.HasValue && s.YHigh.Value > s.YLow.Value
                        ? s.YHigh.Value - s.YLow.Value
                        : null;
                    double? liftPerSpan = width is { } w && w > 0.0
                        ? (-s.Fx * sinA + s.Fz * cosA) / w
                        : null;
                    return new LoadingPoint(s.Eta, clOverCRef, elliptic, s.AlphaEff, liftPerSpan);
                })
                .ToArray();

            loadingCache[key] = points;
            return points;
        }
        catch
        {
            return [];
        }
    }

    private void NotebookScorecardViewBindEmpty()
    {
        string dsl = currentDoc is not null ? FoilReqSerializer.Format(currentDoc) : "";
        string? verLabel = sessionSnapshot?.Requirements.Count > 0 ? sessionSnapshot.Requirements[^1].OrdinalLabel : null;
        ScorecardTabPanel.Bind(null, dsl, verLabel);
    }

    private static void AddCandidatePolarSeries(
        CandidateIterationRecord cand,
        int strokeIdx,
        List<SweepCurveSeries> seriesList,
        List<SweepCaseRow> tableRows)
    {
        var pts = new List<SweepPlotPoint>();
        var upperPts = new List<SweepPlotPoint>();
        int baseIdx = tableRows.Count;
        foreach (var op in cand.OperatingPoints.OrderBy(o => o.SpeedKn))
        {
            if (op.WingOnlyLd is not { } ld) continue;
            int idx = baseIdx + pts.Count;
            double lo = op.Ncrit2Ld ?? (ld * 0.95);
            double hi = op.Ncrit4Ld ?? (ld * 1.03);
            pts.Add(new SweepPlotPoint(idx, op.SpeedKn, ld, hi, $"{cand.CandidateId} {op.OpId}"));
            upperPts.Add(new SweepPlotPoint(idx, op.SpeedKn, hi, null, $"{cand.CandidateId} Ncrit4"));

            DerivedReason? depthReason = op.DepthUnset ? DerivedReason.DepthNotSet : null;
            var dummyCond = new DerivedConditions(
                Q: new DerivedValue(0, null),
                ReRef: new DerivedValue(op.ReMac, null),
                DepthOverChord: new DerivedValue(op.DepthUnset ? null : op.HOverCRoot, depthReason),
                FroudeDepth: new DerivedValue(op.DepthUnset ? null : op.FrH, depthReason),
                Sigma: new DerivedValue(op.DepthUnset ? null : op.Sigma, depthReason));

            tableRows.Add(new SweepCaseRow(
                CaseIndex: idx,
                CaseKey: $"{cand.CandidateId}:{op.OpId}",
                Label: $"{cand.CandidateId} {op.OpId}",
                SpeedMetersPerSecond: op.SpeedKn * DisciplinePresets.KnotsToMps,
                HRefMeters: op.DepthUnset ? null : 0.45,
                AlphaDeg: op.AlphaDeg,
                TargetLoadN: 1050.0,
                State: SweepAttemptState.Completed,
                AttemptCount: 1,
                RunId: null,
                RunKey: null,
                FailureCode: null,
                FailureReason: null,
                IsGap: false,
                Freshness: SweepCaseFreshness.Current,
                FreshnessText: "Current",
                Conditions: dummyCond,
                Cl: op.Cl,
                Cdi: op.Cd * 0.5,
                CdWingNcrit2: op.Cd,
                CdWingNcrit4: op.Cd * 0.94,
                LdWingNcrit2: lo,
                LdWingNcrit4: hi,
                Cm: null,
                CmUnavailableReason: null,
                DragUnavailableReason: null,
                OswaldE: null,
                LiftN: null,
                InducedDragN: null,
                WingDragNcrit2N: null,
                WingDragNcrit4N: null,
                EnvelopeVerdict: "In envelope"));
        }

        if (pts.Count > 0)
        {
            seriesList.Add(new SweepCurveSeries(
                SeriesId: cand.CandidateId,
                Label: $"{cand.CandidateId} {NotebookCopy.WingOnlyLdLabel}",
                HeldValue: cand.Delta.SpanMeters * 1000.0,
                HeldUnit: "mm",
                StrokePatternIndex: strokeIdx,
                PrimarySegments: [new SweepSeriesSegment(pts)],
                BandUpperSegments: [new SweepSeriesSegment(upperPts)],
                SecondarySegments: [],
                Gaps: []));
        }
    }

    private static bool IsCertified(CandidateIterationRecord c) =>
        string.Equals(c.GeometryStatus, nameof(GeometryStatus.Certified), StringComparison.Ordinal);

    private static CandidateIterationRecord? FindBaselineFor(IReadOnlyList<CandidateIterationRecord> list, CandidateIterationRecord active)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].CandidateId != active.CandidateId && IsCertified(list[i]))
                return list[i];
        }
        return null;
    }

    private NotebookWorkspaceState DeriveStateFromSession(NotebookSessionRecord session)
    {
        if (controller is not null && !controller.Assistant.HasApiKey)
            return NotebookWorkspaceState.NoKey;
        if (session.Candidates.Count == 0)
            return currentGrillState?.ActiveQuestion is not null ? NotebookWorkspaceState.Grill : NotebookWorkspaceState.Empty;
        if (activeCandidate is not null && !IsCertified(activeCandidate))
            return NotebookWorkspaceState.Refused;
        if (session.Candidates.Count >= 10)
            return NotebookWorkspaceState.Overflow;
        return NotebookWorkspaceState.Iterated;
    }

    private static void SetPhasePill(Border pill, bool active, bool reached)
    {
        pill.Classes.Set("active", active);
        pill.Classes.Set("reached", !active && reached);
    }

    private void SeedWorkedGrillHistory(string reqVersionId)
    {
        var now = DateTimeOffset.UtcNow;
        grillHistory.Add(new GrillResolution(GrillTreeEngine.Q1UnitSanityId, "answer", "opt-hard-fix", null, reqVersionId, now));
        grillHistory.Add(new GrillResolution(GrillTreeEngine.Q2PhysicsTensionId, "answer", "opt-balanced-ar", null, reqVersionId, now));
        grillHistory.Add(new GrillResolution(GrillTreeEngine.Q3SpeedCavitationId, "answer", "opt-24kn-045m", null, reqVersionId, now));
        grillHistory.Add(new GrillResolution(GrillTreeEngine.Q4ManeuverFeelId, "answer", "opt-snappy-carve", null, reqVersionId, now));
    }

    private static FoilReqDocument BuildWorkedOrcaDoc(bool unitFixed, bool resolved)
    {
        string spanLine = unitFixed
            ? "max_span: <= 950 mm hard @stated;"
            : "max_span: <= 950 cm @flagged(\"Unit sanity: 9,500 mm > 2,200 mm cap; confirm 950 mm\");";
        string arLine = resolved
            ? "target_ar: ~ 10.25 soft(0.9) @stated;"
            : "target_ar: ~ 12.0 soft(0.7) @inferred(\"F4 Orca trait; tension with 90 kg takeoff\");";
        string dropLine = resolved
            ? "drop_speed: >= 24.0 kn hard @stated;"
            : "drop_speed: ? @unknown;";
        string depthLine = resolved
            ? "depth_ref: 0.45 m @stated;"
            : "depth_ref: 0.45 m @defaulted(\"Mast 85-90 cm wave clearance\");";
        string qState = resolved ? "@resolved" : "@open";

        string dsl = $$"""
            foilreq "1.0" {
              brief {
                discipline: wingfoil-surf;
                skill: advanced;
              }
              rider {
                mass: 90.0 kg @stated;
                board_mass: 5.5 kg @defaulted("surf/wing board");
                foil_mass: 4.5 kg @defaulted("carbon foil");
                wing_kit_mass: 5.0 kg @defaulted("wing + harness");
              }
              water {
                kind: salt @defaulted("ocean waves");
                temp: 15.0 C @defaulted("ITTC 15 C");
                {{depthLine}}
              }
              envelope {
                {{spanLine}}
                {{arLine}}
              }
              reference "F4 Orca" {
                traits: [high-aspect-glide, high-camber-low-end, responsive-carve];
                policy: "conceptual-only-no-proprietary-geometry";
                confidence: Flagged;
              }
              objectives {
                takeoff_speed: <= 10.8 kn high @stated;
                {{dropLine}}
              }
              maneuvers {
                cutback: "controlled steep wave drops and powered cutbacks" high @stated;
              }
              open_questions {
                q4_maneuver_feel: "Choose roll/pitch balance for cutbacks" {{qState}};
              }
            }
            """;

        var parsed = FoilReqParser.Parse(dsl);
        if (!parsed.IsValid || parsed.Document is null)
            throw new InvalidOperationException("Worked FoilReq fixture failed to parse: " + parsed.ErrorMessage);
        return parsed.Document;
    }

    private static (NotebookSessionRecord Session, FoilReqDocument Doc) BuildIteratedWorkedSession(bool includeRefused, int candidateCount)
    {
        var doc = BuildWorkedOrcaDoc(unitFixed: true, resolved: true);
        var reqVer = NotebookRecords.CreateRequirementVersion(doc, "v1.4");
        var now = DateTimeOffset.UtcNow;

        var candidates = new List<CandidateIterationRecord>();
        var turns = new List<NotebookTurnRecord>
        {
            new(Guid.NewGuid().ToString("D"), now, "user-prompt", reqVer.ReqVersionId, null, NotebookRecords.ChunkText(NotebookCopy.DefaultExampleBrief))
        };

        int certifiedToBuild = Math.Max(1, candidateCount);
        for (int i = 1; i <= certifiedToBuild; i++)
        {
            double frac = certifiedToBuild == 1 ? 0.0 : (i - 1.0) / Math.Max(1, certifiedToBuild - 1);
            double spanM = Math.Round(0.930 - 0.015 * frac, 4);
            double areaM2 = Math.Round(0.0845 - 0.0030 * frac, 5);
            double anhedral = Math.Round(-5.0 - 1.0 * frac, 1);
            string rootId = i == 1 ? "cfdw-surf-118" : "cfdw-surf-105";
            string tipId = i == 1 ? "cfdw-surf-105" : "cfdw-dw-092";

            var draftDelta = new CandidateIntentDelta(
                DeltaHash: "",
                SpanMeters: spanM,
                AreaSquareMeters: areaM2,
                Taper: 0.35,
                RootThicknessRatio: i == 1 ? 0.118 : 0.105,
                AnhedralDeg: anhedral,
                WashoutDeg: i == 1 ? -1.2 : -1.5,
                SweepQuarterChordDeg: i == 1 ? 6.5 : 7.2,
                RootProfileId: rootId,
                TipProfileId: tipId,
                Provenance: [new AssistantFieldProvenance("spanMeters", ProvenanceTag.Stated, $"{spanM * 1000:0.0} mm", "Within 950 mm cap")]);
            var delta = draftDelta with { DeltaHash = CandidateIntentDelta.ComputeDeltaHash(draftDelta) };

            byte[] dslBytes = NotebookRecords.MaterializeCandidateDsl(delta, $"Notebook v{i}");
            var parsedFoil = FoilSource.Parse(dslBytes);
            string hash = parsedFoil.SurfaceHash!;

            double glideLd = Math.Round(24.6 + 1.8 * frac, 1);
            double negCpDrop = Math.Round(0.98 - 0.10 * frac, 2);
            double vCrit = Math.Round(28.0 + 1.5 * frac, 1);
            double hcRoot = Math.Round(0.45 / delta.RootChordMeters, 2);

            CandidateOperatingPointSummary[] ops =
            [
                new("takeoff", Math.Round(10.5 + 0.3 * frac, 1), 5.6, 0.82, 0.044, 18.6, 17.8, 19.2, 4.31e5, -1.42, 6.95, 4.89, vCrit, 0.0, hcRoot, 2.57, false, "ProductWingMethod/1+PanelMethod200p", null, 510),
                new("glide", 15.0, 2.4, 0.41, 0.016, glideLd, glideLd - 1.2, glideLd + 0.7, 6.15e5, -1.06, 3.41, 3.21, vCrit, 0.0, hcRoot, 3.68, false, "ProductWingMethod/1+PanelMethod200p", null, 580),
                new("steep_drop", 24.0, 0.4, 0.16, 0.011, 14.5, 13.8, 15.1, 9.85e5, -negCpDrop, 1.332, Math.Round(1.332 / negCpDrop, 2), vCrit, 0.0, hcRoot, 5.88, false, "ProductWingMethod/1+PanelMethod200p", null, 610)
            ];

            var cand = NotebookRecords.CreateCandidateRecord(i, reqVer.ReqVersionId, delta, hash, nameof(GeometryStatus.Certified), null, ops, 0, $"v{i}", now);
            candidates.Add(cand);
            turns.Add(new NotebookTurnRecord(Guid.NewGuid().ToString("D"), now, "candidate-evaluated", reqVer.ReqVersionId, cand.CandidateId,
                NotebookRecords.ChunkText(string.Create(Inv, $"Local solver evaluated {cand.CandidateId}: span {spanM * 1000:0.0} mm, Wing-only L/D {glideLd:0.0}, V_crit {vCrit:0.0} kn.")),
                WallTimeMs: 1700));
        }

        if (includeRefused)
        {
            var badDraft = new CandidateIntentDelta(
                DeltaHash: "",
                SpanMeters: 0.910,
                AreaSquareMeters: 0.080,
                Taper: 0.16,
                RootThicknessRatio: 0.10,
                AnhedralDeg: -6.0,
                WashoutDeg: -1.5,
                SweepQuarterChordDeg: 7.0,
                RootProfileId: "cfdw-surf-105",
                TipProfileId: "cfdw-dw-092",
                Provenance: [new AssistantFieldProvenance("taper", ProvenanceTag.Inferred, "0.16", "Aggressive tip reduction")]);
            var badDelta = badDraft with { DeltaHash = CandidateIntentDelta.ComputeDeltaHash(badDraft) };

            var refused = NotebookRecords.CreateCandidateRecord(
                candidates.Count + 1,
                reqVer.ReqVersionId,
                badDelta,
                "",
                "Refused",
                TipChord.RefusalCode,
                [],
                repairAttemptIndex: 1,
                candidateId: "v2b",
                createdUtc: now);
            candidates.Add(refused);
            turns.Add(new NotebookTurnRecord(Guid.NewGuid().ToString("D"), now, "candidate-refused", reqVer.ReqVersionId, refused.CandidateId,
                NotebookRecords.ChunkText("Candidate v2b refused: DSL-TIP-CHORD-MIN (tip chord fell below minimum floor)."),
                ErrorCode: TipChord.RefusalCode,
                RepairAttempt: 1));
            turns.Add(new NotebookTurnRecord(Guid.NewGuid().ToString("D"), now, "withheld", reqVer.ReqVersionId, refused.CandidateId,
                NotebookRecords.ChunkText(NotebookCopy.ResponseWithheldBanner),
                ErrorCode: "AI-03-WITHHELD"));
        }
        else
        {
            turns.Add(new NotebookTurnRecord(Guid.NewGuid().ToString("D"), now, "ai-translation", reqVer.ReqVersionId, candidates[^1].CandidateId,
                NotebookRecords.ChunkText($"At 15.0 kn glide, wing-only L/D reaches 26.4 (+7.3% vs v1) while keeping takeoff at 10.8 kn and steep-drop V_crit at 29.5 kn within the 950.0 mm span cap.")));
        }

        string title = candidateCount >= 10
            ? "Overflow Stress Session — High-Aspect Large-Wave 90 kg Wingfoil Exploration (12 Iterations)"
            : "Large-Wave 90 kg Wingfoil";

        var session = NotebookRecords.FinalizeSession(new NotebookSessionRecord(
            Guid.NewGuid().ToString("D"),
            title,
            now,
            [reqVer],
            turns,
            candidates,
            [],
            ""));

        return (session, doc);
    }
}
