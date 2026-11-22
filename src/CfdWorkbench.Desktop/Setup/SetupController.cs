using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Setup;

/// <summary>
/// Stateful Area 1 Setup &amp; Goal State controller bound to <see cref="AuthoringSession"/>
/// (docs/design/setup-seeding.md §5, Ruling 272 DR-SET-1..5). Owns live soft-target solving,
/// Flagged acknowledgement gating, Goal state immutable updates, class-rule &amp; sanity-bound DRC,
/// DRC exclusions, and the no-key AI-02 language entry slot (COPY-53).
/// </summary>
public sealed class SetupController
{
    public const string Copy53NoApiKey = "No API key configured — every design and analysis tool works without it · Configure key";
    public const string Copy79LanguageTitle = "Describe a starting design";

    private AuthoringSession session;
    private readonly Action<SetupSeedOutcome>? onSeeded;

    public SetupController(AuthoringSession session, Action<SetupSeedOutcome>? onSeeded = null)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.onSeeded = onSeeded;
        ApplyPresetDefaults("wingfoil-race", notify: false);
    }

    public event Action? Changed;

    // ---- Brief & parameter inputs ----
    public string Purpose { get; private set; } = "wingfoil-race";
    public string SelectedPresetId { get; private set; } = "wingfoil-race";
    public string SelectedPresetName { get; private set; } = "Wingfoil race";
    public string ClassRulePresetId { get; private set; } = ClassRules.GwaRace2022;
    public string ClassRuleVersion { get; private set; } = "2022-02-01";
    public string ActiveSubTab { get; private set; } = "brief"; // "brief" | "goal" | "checks"

    public double MaxSpanMeters { get; set; } = 1.10;
    public double WeightSpan { get; set; } = 1.0;
    public double MaxChordMm { get; set; } = 120.0;
    public double WeightChord { get; set; } = 1.0;
    public double TargetAreaCm2 { get; set; } = 1000.0;
    public double WeightArea { get; set; } = 1.0;
    public double TargetAspectRatio { get; set; } = 12.0;
    public double WeightAspectRatio { get; set; } = 1.0;
    public double Taper { get; set; } = 0.24;
    public double RootThicknessPercent { get; set; } = 10.0;
    public double AnhedralDeg { get; set; } = -5.0;
    public double WashoutDeg { get; set; } = -1.0;

    public double RiderMassKg { get; set; } = 90.0;
    public bool IsSaltWater { get; set; } = true;
    public double WaterTemperatureC { get; set; } = 15.0;
    public double HRefMeters { get; set; } = 0.50;
    public double MastLengthCm { get; set; } = 95.0;
    public string SelectedProfileId { get; set; } = "naca-4412";

    public bool FlaggedAcknowledged { get; set; }
    public bool HasApiKey { get; set; }
    public string LanguagePromptText { get; set; } = "";

    // ---- Live preview & derived state ----
    public PurposeResolution? ActiveResolution { get; private set; }
    public IReadOnlyList<FlaggedAssumption> RequiredFlagged { get; private set; } = [];
    public RecipeSolveResult? PreviewSolve { get; private set; }
    public WingEstimates? PreviewEstimates { get; private set; }
    public WingEstimates? PriorEstimates { get; private set; }
    public byte[]? PreviewFoilBytes { get; private set; }
    public GoalStateVersion? PreviewGoalState { get; private set; }
    public FeasibilityReport? PreviewFeasibility { get; private set; }
    public IReadOnlyList<DrcFinding> ClassFindings { get; private set; } = [];
    public IReadOnlyList<DrcFinding> SanityFindings { get; private set; } = [];
    public string? ValidationError { get; private set; }
    public string? LastReceipt { get; private set; }
    public string? ExclusionError { get; private set; }

    public RecipeProvenanceStatus RecipeStatus { get; private set; } = new(RecipeLinkState.None, null, null, null, null);
    public IReadOnlyList<SetupBriefVersion> BriefHistory { get; private set; } = [];
    public IReadOnlyList<GoalStateVersion> GoalHistory { get; private set; } = [];
    public IReadOnlyList<DrcExclusion> Exclusions { get; private set; } = [];

    public bool IsReseed { get; private set; }
    public int NextRevisionOrdinal { get; private set; } = 1;
    public int NextBriefVersion => BriefHistory.Count + 1;
    public int CurrentGoalVersion => GoalHistory.Count;
    public int PriorRunCount { get; private set; }
    public string? CurrentDesignId { get; private set; }

    public bool HasSetupOrActive => BriefHistory.Count > 0 || GoalHistory.Count > 0;

    public bool CanSeed =>
        ValidationError is null &&
        PreviewSolve is not null &&
        (RequiredFlagged.Count == 0 || FlaggedAcknowledged);

    public string SeedButtonLabel => IsReseed
        ? Labels.SetupReseedAs(NextRevisionOrdinal)
        : Labels.SetupSeedDesign;

    public string? ReseedNoticeText => IsReseed
        ? Labels.SetupReseedNotice(NextBriefVersion, NextRevisionOrdinal)
        : null;

    public string? WakefoilNoticeText =>
        ActiveResolution?.PurposeMappingFlag is not null ? Labels.SetupWakefoilMappingNote : null;

    public string? SectionFallbackText
    {
        get
        {
            var preset = ActiveResolution?.Preset;
            if (preset is null || preset.FallbackReason is null) return null;
            if (!string.Equals(SelectedProfileId, preset.GenFallbackProfileId, StringComparison.OrdinalIgnoreCase)) return null;
            var gen = Catalog.Load().FirstOrDefault(e => string.Equals(e.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase));
            return Labels.SetupSectionFallback(preset.PreferredProfileName, preset.FallbackReason, gen?.Designation ?? SelectedProfileId);
        }
    }

    public string? TipClampNoticeText =>
        PreviewSolve is { TipClampActive: true } s
            ? Labels.SetupTipClampNotice(TipChord.Format(s.Recipe.RootChordMeters))
            : null;

    public int OpenCheckCount =>
        ClassFindings.Count(f => !f.Excluded && f.Severity is DrcSeverity.Error or DrcSeverity.Warning) +
        SanityFindings.Count(f => !f.Excluded);

    public string StatusStripText => RecipeStatus.State switch
    {
        RecipeLinkState.Linked => Labels.SetupLinkedStatus(RecipeStatus.PresetName ?? SelectedPresetName),
        RecipeLinkState.DirectParametric => Labels.SetupDirectParametric,
        _ => "Setup: ready"
    };

    public void BindSession(AuthoringSession next)
    {
        session = next ?? throw new ArgumentNullException(nameof(next));
        LastReceipt = null;
        ExclusionError = null;
        RefreshFromSession(notify: false);
        RecomputePreview(notify: false);
    }

    public void SetSubTab(string tab)
    {
        if (tab is not ("brief" or "goal" or "checks") || ActiveSubTab == tab) return;
        ActiveSubTab = tab;
        Changed?.Invoke();
    }

    public void SelectPurpose(string purposeOrPresetId) => ApplyPresetDefaults(purposeOrPresetId, notify: true);

    public void SelectClassRule(string classRulePresetId)
    {
        var info = ClassRules.Presets.FirstOrDefault(p => p.Id == classRulePresetId);
        if (info is null) return;
        ClassRulePresetId = info.Id;
        ClassRuleVersion = info.Date;
        RecomputePreview(notify: true);
    }

    public void SetFlaggedAcknowledged(bool acknowledged)
    {
        if (FlaggedAcknowledged == acknowledged) return;
        FlaggedAcknowledged = acknowledged;
        Changed?.Invoke();
    }

    public void SetApiKeyAvailable(bool available)
    {
        if (HasApiKey == available) return;
        HasApiKey = available;
        Changed?.Invoke();
    }

    public void RefreshFromSession(bool notify = true)
    {
        try
        {
            var env = HasAcceptedRevision() ? session.Envelope() : null;
            IsReseed = env is not null && env.Accepted.Length > 0;
            NextRevisionOrdinal = (env?.Accepted.Length ?? 0) + 1;
            PriorRunCount = env?.Analysis?.Runs.Count ?? 0;

            if (IsReseed)
            {
                var snap = session.Snapshot();
                PriorEstimates = WingEstimates.From(snap.Source, "accepted", 0);
                CurrentDesignId = env!.Accepted.Single(a => a.Id == snap.AcceptedId).DesignId;
            }
            else
            {
                PriorEstimates = null;
                CurrentDesignId = null;
            }

            var setup = session.ReadSetup();
            BriefHistory = setup?.Briefs ?? [];
            GoalHistory = setup?.GoalStates ?? [];
            Exclusions = setup?.DrcExclusions ?? [];
            RecipeStatus = session.RecipeStatus();
        }
        catch (ContractError err) when (err.Code == "DOC-CLOSED")
        {
            return;
        }

        if (notify)
        {
            RecomputePreview(notify: true);
        }
    }

    private void ApplyPresetDefaults(string purposeOrPresetId, bool notify)
    {
        var resolution = DisciplinePresets.ResolvePurpose(purposeOrPresetId);
        var p = resolution.Preset;
        ActiveResolution = resolution;
        Purpose = resolution.NormalizedPurpose;
        SelectedPresetId = p.Id;
        SelectedPresetName = p.Name;
        ClassRulePresetId = resolution.ClassRulePresetId;
        ClassRuleVersion = resolution.ClassRuleVersion;

        MaxSpanMeters = p.MaxSpanMeters;
        WeightSpan = 1.0;
        MaxChordMm = p.MaxChordMeters * 1000.0;
        WeightChord = 1.0;
        TargetAreaCm2 = p.TargetAreaSquareMeters * 10_000.0;
        WeightArea = 1.0;
        TargetAspectRatio = p.TargetAspectRatio;
        WeightAspectRatio = 1.0;
        Taper = p.Taper;
        RootThicknessPercent = p.RootThicknessRatio * 100.0;
        AnhedralDeg = p.AnhedralDeg;
        WashoutDeg = p.WashoutDeg;
        RiderMassKg = p.DefaultMasses.First(m => m.Id == "rider").MassKg;
        IsSaltWater = true;
        WaterTemperatureC = 15.0;
        HRefMeters = p.DefaultHRefMeters;
        MastLengthCm = p.DefaultMastLengthCm;
        SelectedProfileId = p.GenFallbackProfileId;

        RequiredFlagged = DisciplinePresets.FlaggedFor(resolution);
        FlaggedAcknowledged = RequiredFlagged.Count == 0;
        ValidationError = null;
        ExclusionError = null;

        RefreshFromSession(notify: false);
        RecomputePreview(notify: notify);
    }

    public void RecomputePreview(bool notify = true)
    {
        var sw = Stopwatch.StartNew();
        ValidationError = ValidateInputs();
        if (ValidationError is not null)
        {
            PreviewSolve = null;
            PreviewEstimates = null;
            PreviewFeasibility = null;
            if (notify) Changed?.Invoke();
            return;
        }

        try
        {
            var resolution = ActiveResolution ?? DisciplinePresets.ResolvePurpose(Purpose);
            var preset = resolution.Preset;
            RequiredFlagged = DisciplinePresets.FlaggedFor(resolution);

            var softTargets = BuildSoftTargets();
            double defaultK = RecipeSeeder.Kappa(preset.Taper);
            double defaultSpan = Math.Sqrt(preset.TargetAreaSquareMeters * preset.TargetAspectRatio);
            double defaultRoot = Math.Sqrt(preset.TargetAreaSquareMeters / preset.TargetAspectRatio) / defaultK;

            var catalog = Catalog.Load();
            var gen = catalog.FirstOrDefault(e => string.Equals(e.Id, SelectedProfileId, StringComparison.OrdinalIgnoreCase) && e.DisabledReason is null)
                ?? catalog.First(e => e.Id == preset.GenFallbackProfileId);

            string? preferredId = string.Equals(gen.Id, preset.GenFallbackProfileId, StringComparison.OrdinalIgnoreCase) ? preset.PreferredProfileId : gen.Id;
            string? fallbackReason = string.Equals(preferredId, gen.Id, StringComparison.OrdinalIgnoreCase) ? null : preset.FallbackReason;

            var solve = RecipeSeeder.Solve(new RecipeSolveInput(
                SoftTargets: softTargets,
                DefaultSpanMeters: defaultSpan,
                DefaultRootChordMeters: defaultRoot,
                AnhedralDeg: AnhedralDeg,
                WashoutDeg: WashoutDeg,
                RootThicknessRatio: RootThicknessPercent / 100.0,
                ProfileId: gen.Id,
                PreferredProfileId: preferredId,
                FallbackReason: fallbackReason));

            byte[] dsl = RecipeSeeder.EmitFoilDsl(solve.Recipe, gen, preset.Name);
            var est = WingEstimates.From(dsl, "preview", 0);
            var water = WaterTable.At(WaterTemperatureC, IsSaltWater ? OperatingPoints.SaltSalinityGPerKg : 0.0);

            var existingGoal = GoalHistory.Count > 0 ? GoalHistory[^1] : null;
            var previewGoal = DisciplinePresets.BuildGoalState(
                goalStateId: existingGoal?.GoalStateId ?? Guid.NewGuid().ToString("D"),
                parentGoalStateId: existingGoal?.Parent,
                briefId: existingGoal?.BriefId,
                resolution: resolution,
                softTargets: softTargets,
                riderMassKg: RiderMassKg,
                water: water,
                hRefMeters: HRefMeters,
                mastLengthCm: MastLengthCm,
                classRulePresetId: ClassRulePresetId,
                classRuleVersion: ClassRuleVersion);

            // Preserve user-edited constraint rows if we have a saved GoalStateVersion for this preset.
            if (existingGoal is not null && existingGoal.DisciplinePresetId == preset.Id)
            {
                var merged = previewGoal.Constraints.Select(c =>
                {
                    var prev = existingGoal.Constraints.FirstOrDefault(x => x.Id == c.Id && x.EditedByUser);
                    return prev ?? c;
                }).ToArray();
                previewGoal = previewGoal with { Constraints = merged };
                previewGoal = previewGoal with { ContentHash = SetupStore.GoalContentHash(previewGoal) };
            }

            var drcTimer = Stopwatch.StartNew();
            var feasibility = GoalFeasibility.Evaluate(est, previewGoal, polars: null, exclusions: Exclusions, designId: CurrentDesignId);
            drcTimer.Stop();

            PreviewSolve = solve;
            PreviewFoilBytes = dsl;
            PreviewEstimates = est;
            PreviewGoalState = previewGoal;
            PreviewFeasibility = feasibility;
            ClassFindings = feasibility.ClassFindings;
            SanityFindings = feasibility.SanityFindings;

            sw.Stop();
            session.RecordSetupEvent("setup.preview", "OK", sw.Elapsed.TotalMilliseconds, new SetupTelemetry
            {
                PresetId = preset.Id,
                Conflict = solve.Conflict,
                MaxRelativeDeviation = solve.MaxRelativeDeviation,
                TipClampActive = solve.TipClampActive,
                CandidatesEvaluated = solve.CandidatesEvaluated,
                SolveMs = solve.SolveMs
            });
            session.RecordSetupEvent("setup.drc.evaluate", "OK", drcTimer.Elapsed.TotalMilliseconds, new SetupTelemetry
            {
                ClassRuleId = ClassRulePresetId,
                ClassRuleVersion = ClassRuleVersion,
                ErrorCount = ClassFindings.Count(f => !f.Excluded && f.Severity == DrcSeverity.Error),
                WarningCount = ClassFindings.Count(f => !f.Excluded && f.Severity == DrcSeverity.Warning),
                AdvisoryCount = SanityFindings.Count(f => !f.Excluded) + ClassFindings.Count(f => !f.Excluded && f.Severity == DrcSeverity.Advisory),
                ExcludedCount = ClassFindings.Count(f => f.Excluded) + SanityFindings.Count(f => f.Excluded)
            });
        }
        catch (ContractError err)
        {
            ValidationError = err.Reason ?? err.Code;
            PreviewSolve = null;
            PreviewEstimates = null;
            PreviewFeasibility = null;
        }

        if (notify) Changed?.Invoke();
    }

    public SetupSeedOutcome? SeedDesign()
    {
        RecomputePreview(notify: false);
        if (!CanSeed)
        {
            if (ValidationError is null && RequiredFlagged.Count > 0 && !FlaggedAcknowledged)
                ValidationError = $"Flagged — {RequiredFlagged[0].Source} · Acknowledge to continue";
            Changed?.Invoke();
            return null;
        }

        try
        {
            var water = WaterTable.At(WaterTemperatureC, IsSaltWater ? OperatingPoints.SaltSalinityGPerKg : 0.0);
            var req = new SetupSeedRequest(
                Purpose: Purpose,
                SoftTargets: BuildSoftTargets(),
                EntryKind: "parameters",
                RiderMassKg: RiderMassKg,
                Water: water,
                HRefMeters: HRefMeters,
                MastLengthCm: MastLengthCm,
                RootThicknessRatio: RootThicknessPercent / 100.0,
                AnhedralDeg: AnhedralDeg,
                WashoutDeg: WashoutDeg,
                ProfileId: SelectedProfileId,
                ClassRulePresetId: ClassRulePresetId,
                ClassRuleVersion: ClassRuleVersion,
                AcknowledgeAllFlagged: FlaggedAcknowledged);

            var outcome = session.Seed(req, Guid.NewGuid().ToString("D"));
            LastReceipt = Labels.SetupSeedReceipt(outcome.RevisionOrdinal, SelectedPresetName, session.ReadSetup()!.Briefs.Count, session.ReadSetup()!.GoalStates.Count);
            RefreshFromSession(notify: false);
            RecomputePreview(notify: false);
            onSeeded?.Invoke(outcome);
            Changed?.Invoke();
            return outcome;
        }
        catch (ContractError err)
        {
            ValidationError = err.Reason ?? err.Code;
            Changed?.Invoke();
            return null;
        }
    }

    /// <summary>Edits one constraint bound and appends a new immutable GoalStateVersion without touching geometry (GOAL-01, GOAL-03).</summary>
    public GoalStateVersion? UpdateConstraintBound(string constraintId, double newBound)
    {
        if (!double.IsFinite(newBound) || newBound <= 0)
        {
            ValidationError = "Constraint bound must be finite and above 0.";
            Changed?.Invoke();
            return null;
        }
        if (!HasAcceptedRevision() || PreviewGoalState is null)
        {
            ValidationError = "Seed or open a design before saving Goal state changes.";
            Changed?.Invoke();
            return null;
        }

        var baseGoal = GoalHistory.Count > 0 ? GoalHistory[^1] : PreviewGoalState;
        var nextConstraints = baseGoal.Constraints
            .Select(c => c.Id == constraintId ? c with { Bound = newBound, EditedByUser = true } : c)
            .ToArray();
        var saved = session.UpdateGoalState(baseGoal with { Constraints = nextConstraints });
        LastReceipt = Labels.SetupGoalSavedReceipt(session.ReadSetup()!.GoalStates.Count);
        RefreshFromSession(notify: false);
        RecomputePreview(notify: true);
        return saved;
    }

    /// <summary>Records an immutable user DRC exclusion for one finding on the active DesignRevision (A5.8).</summary>
    public DrcExclusion? ExcludeFinding(DrcFinding finding, string reason, string attribution = "")
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ExclusionError = Labels.SetupExclusionReasonRequired;
            Changed?.Invoke();
            return null;
        }
        if (string.IsNullOrWhiteSpace(CurrentDesignId))
        {
            ExclusionError = "Seed or open a design before excluding findings.";
            Changed?.Invoke();
            return null;
        }

        ExclusionError = null;
        var ex = session.RecordDrcExclusion(CurrentDesignId, finding.RuleId, finding.RuleVersion, finding.LocationKey, reason, attribution);
        LastReceipt = $"Excluded {finding.RuleId} on active revision.";
        RefreshFromSession(notify: false);
        RecomputePreview(notify: true);
        return ex;
    }

    private SoftTargetSet BuildSoftTargets()
    {
        bool customWeights = Math.Abs(WeightSpan - 1.0) > 1e-12 ||
                             Math.Abs(WeightChord - 1.0) > 1e-12 ||
                             Math.Abs(WeightArea - 1.0) > 1e-12 ||
                             Math.Abs(WeightAspectRatio - 1.0) > 1e-12;
        return new SoftTargetSet(
            new SoftTargetScalar(MaxSpanMeters, WeightSpan, "Stated", "User parameter form"),
            new SoftTargetScalar(MaxChordMm / 1000.0, WeightChord, "Stated", "User parameter form"),
            new SoftTargetScalar(TargetAreaCm2 / 10_000.0, WeightArea, "Stated", "User parameter form"),
            new SoftTargetScalar(TargetAspectRatio, WeightAspectRatio, "Stated", "User parameter form"),
            new SoftTargetTaper(Taper, "Stated", "User parameter form"),
            customWeights ? "custom" : "equal-default");
    }

    private string? ValidateInputs()
    {
        if (!double.IsFinite(WeightSpan) || !double.IsFinite(WeightChord) || !double.IsFinite(WeightArea) || !double.IsFinite(WeightAspectRatio) ||
            WeightSpan < 0 || WeightChord < 0 || WeightArea < 0 || WeightAspectRatio < 0 ||
            WeightSpan + WeightChord + WeightArea + WeightAspectRatio <= 0)
            return Labels.SetupWeightsInvalid;
        if (!double.IsFinite(MaxSpanMeters) || MaxSpanMeters <= 0) return "Max span must be above 0.";
        if (!double.IsFinite(MaxChordMm) || MaxChordMm <= 0) return "Max chord must be above 0.";
        if (!double.IsFinite(TargetAreaCm2) || TargetAreaCm2 <= 0) return "Target area must be above 0.";
        if (!double.IsFinite(TargetAspectRatio) || TargetAspectRatio <= 0) return "Target AR must be above 0.";
        if (!double.IsFinite(Taper) || Taper is < 0.02 or > 1.0) return "Taper must lie between 0.02 and 1.00.";
        if (!double.IsFinite(RootThicknessPercent) || RootThicknessPercent is < 4.0 or > 25.0) return "Root thickness must lie between 4 % and 25 %.";
        if (!double.IsFinite(RiderMassKg) || RiderMassKg <= 0) return "Rider mass must be above 0.";
        if (!double.IsFinite(WaterTemperatureC) || WaterTemperatureC is < 0 or > 50) return WaterTable.OutsideReason;
        if (!double.IsFinite(HRefMeters) || HRefMeters <= 0) return "Reference depth h_ref must be above 0.";
        if (!double.IsFinite(MastLengthCm) || MastLengthCm <= 0) return "Mast length must be above 0.";
        return null;
    }

    private bool HasAcceptedRevision()
    {
        try { _ = session.Snapshot(); return true; }
        catch (ContractError) { return false; }
    }
}
