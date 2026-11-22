using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

public enum SweepSpeedUnit { MetersPerSecond, Knots }

public sealed record SweepPreviewRow(
    int CaseIndex,
    string CaseKey,
    string? Label,
    double SpeedMetersPerSecond,
    double? HRefMeters,
    string Datum,
    double PAtm,
    double? AlphaDeg,
    double? TargetLoadN,
    DerivedConditions Conditions,
    bool CacheHit,
    string? ExistingRunId);

public sealed record SweepPreview(
    SweepDefinition Definition,
    IReadOnlyList<SweepCase> Cases,
    IReadOnlyList<SweepPreviewRow> Rows,
    int PanelsPerRun,
    int EstimatedStorageBytes,
    int DocumentBytes,
    int DocumentHeadroomBytes,
    int CacheHits,
    double? EstimatedWallMs,
    int MeasuredRunSamples,
    bool DepthSet,
    string BasisLabel,
    string NotModelledLabel,
    double CRefMeters);

/// <summary>
/// Pure validation, deterministic schedule resolution, derived operating conditions, cache-hit detection,
/// and storage/wall estimates for local-tier sweeps (docs/design/local-sweeps.md §2.1, Ruling 271).
/// </summary>
public static class SweepSchedule
{
    private const double DuplicateTolerance = 1e-9;
    private const int EstimatedExperimentBytes = 7_500;
    private const int EstimatedVlmRunBytes = 127_000;
    private const int EstimatedEstimatorRunBytes = 1_500;
    private static readonly char[] Delimiters = [',', ';', ' ', '\t', '\r', '\n'];

    public static double ToMetersPerSecond(double value, SweepSpeedUnit unit) =>
        unit == SweepSpeedUnit.Knots ? value / Labels.KnotsPerMeterSecond : value;

    public static IReadOnlyList<double> ParseSpeeds(string rawText, SweepSpeedUnit unit)
    {
        var tokens = SplitTokens(rawText, "Speeds");
        var values = new List<double>(tokens.Length);
        foreach (string raw in tokens)
        {
            string normalized = NormalizeMinus(raw);
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                throw new ContractError("ANA-SWEEP-UNIT", Labels.SweepInvalidValue("Speeds", raw));
            if (!double.IsFinite(parsed))
                throw new ContractError("ANA-SWEEP-NONFINITE", Labels.SweepInvalidValue("Speeds", raw));
            if (parsed <= 0)
                throw new ContractError("ANA-INPUT-SPEED", Labels.SweepSpeedNonPositive(raw));
            double mps = ToMetersPerSecond(parsed, unit);
            if (values.Any(existing => Math.Abs(existing - mps) <= DuplicateTolerance))
                throw new ContractError("ANA-SWEEP-DUPLICATE", Labels.SweepDuplicateValue("Speeds", raw));
            values.Add(mps);
        }
        return values;
    }

    public static IReadOnlyList<double> ParseAngles(string rawText)
    {
        var tokens = SplitTokens(rawText, "Angles");
        var values = new List<double>(tokens.Length);
        foreach (string raw in tokens)
        {
            string normalized = NormalizeMinus(raw);
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                throw new ContractError("ANA-SWEEP-UNIT", Labels.SweepInvalidValue("Angles", raw));
            if (!double.IsFinite(parsed))
                throw new ContractError("ANA-SWEEP-NONFINITE", Labels.SweepInvalidValue("Angles", raw));
            if (values.Any(existing => Math.Abs(existing - parsed) <= DuplicateTolerance))
                throw new ContractError("ANA-SWEEP-DUPLICATE", Labels.SweepDuplicateValue("Angles", raw));
            values.Add(parsed);
        }
        return values;
    }

    public static SweepPreview BuildCartesian(
        AuthoringSession session,
        IWingMethod method,
        string tier,
        IReadOnlyList<double> speedsMetersPerSecond,
        IReadOnlyList<double> anglesDeg,
        WaterRecord water,
        double? hRefMeters,
        double pAtm = OperatingPoints.StandardAtmosphere,
        string datum = OperatingPoints.RootLeadingEdge,
        double? medianWallMs = null,
        int measuredRunSamples = 0)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(water);
        if (tier is not (SweepRecords.TierVlmStrip or SweepRecords.TierEstimator))
            throw new ContractError("ANA-INPUT-TIER", $"Unsupported local sweep tier: {tier}");

        if (speedsMetersPerSecond is null || speedsMetersPerSecond.Count == 0)
            throw new ContractError("ANA-SWEEP-EMPTY", "Speeds list cannot be empty.");
        if (anglesDeg is null || anglesDeg.Count == 0)
            throw new ContractError("ANA-SWEEP-EMPTY", "Angles list cannot be empty.");

        var validatedSpeeds = new List<double>(speedsMetersPerSecond.Count);
        foreach (double v in speedsMetersPerSecond)
        {
            if (!double.IsFinite(v))
                throw new ContractError("ANA-SWEEP-NONFINITE", Labels.SweepInvalidValue("Speeds", v.ToString(CultureInfo.InvariantCulture)));
            if (v <= 0)
                throw new ContractError("ANA-INPUT-SPEED", Labels.SweepSpeedNonPositive(v.ToString("0.###", CultureInfo.InvariantCulture)));
            if (validatedSpeeds.Any(existing => Math.Abs(existing - v) <= DuplicateTolerance))
                throw new ContractError("ANA-SWEEP-DUPLICATE", Labels.SweepDuplicateValue("Speeds", v.ToString("0.###", CultureInfo.InvariantCulture)));
            validatedSpeeds.Add(v);
        }

        var validatedAngles = new List<double>(anglesDeg.Count);
        foreach (double a in anglesDeg)
        {
            if (!double.IsFinite(a))
                throw new ContractError("ANA-SWEEP-NONFINITE", Labels.SweepInvalidValue("Angles", a.ToString(CultureInfo.InvariantCulture)));
            if (validatedAngles.Any(existing => Math.Abs(existing - a) <= DuplicateTolerance))
                throw new ContractError("ANA-SWEEP-DUPLICATE", Labels.SweepDuplicateValue("Angles", a.ToString("0.###", CultureInfo.InvariantCulture)));
            validatedAngles.Add(a);
        }

        int totalCases = validatedSpeeds.Count * validatedAngles.Count;
        if (validatedSpeeds.Count > SweepRecords.MaxSpeeds)
            throw new ContractError("ANA-SWEEP-CASE-CAP", $"Speeds count {validatedSpeeds.Count} exceeds the limit ({SweepRecords.MaxSpeeds}).");
        if (validatedAngles.Count > SweepRecords.MaxAngles)
            throw new ContractError("ANA-SWEEP-CASE-CAP", $"Angles count {validatedAngles.Count} exceeds the limit ({SweepRecords.MaxAngles}).");
        if (totalCases > SweepRecords.MaxCasesPerSweep)
            throw new ContractError("ANA-SWEEP-CASE-CAP", Labels.SweepCaseCapExceeded(totalCases, SweepRecords.MaxCasesPerSweep));

        if (hRefMeters is { } h && (!double.IsFinite(h) || h <= 0))
            throw new ContractError("ANA-INPUT-DEPTH", $"Reference depth h_ref must be finite and above zero: {h.ToString(CultureInfo.InvariantCulture)}");
        if (!double.IsFinite(pAtm) || pAtm <= 0)
            throw new ContractError("ANA-INPUT-PRESSURE", "Atmospheric pressure must be finite and above zero.");
        OperatingPoints.Validate(water);
        _ = WaterTable.At(water.TemperatureC, water.SalinityGPerKg);

        validatedSpeeds.Sort();
        validatedAngles.Sort();

        var view = session.Snapshot();
        var inputs = Freshness.Inputs(view);
        var reference = ReferenceQuantities.From(view.Source);
        (RunMethod runMethod, RunSettings runSettings) = MethodForTier(method, tier);
        string settingsHash = RunRecord.SettingsHash(runSettings);

        var intactRuns = IntactCompletedByKey(session);
        var cases = new List<SweepCase>(totalCases);
        var rows = new List<SweepPreviewRow>(totalCases);
        int caseIndex = 0;
        int cacheHits = 0;

        foreach (double speed in validatedSpeeds)
        {
            foreach (double alpha in validatedAngles)
            {
                string caseKey = SweepRecords.CaseKey(speed, hRefMeters, datum, pAtm, alpha, null);
                var op = new OperatingPoint(speed, pAtm, hRefMeters, datum, alpha, null);
                string runKey = RunRecord.Key(inputs, water, op, runMethod, settingsHash);
                bool hit = intactRuns.TryGetValue(runKey, out string? existingRunId);
                if (hit) cacheHits++;
                var conditions = OperatingPoints.Derive(op, water, reference.CRef);
                cases.Add(new SweepCase(caseIndex, caseKey, null, speed, hRefMeters, datum, pAtm, alpha, null));
                rows.Add(new SweepPreviewRow(caseIndex, caseKey, null, speed, hRefMeters, datum, pAtm, alpha, null, conditions, hit, existingRunId));
                caseIndex++;
            }
        }

        var definition = new SweepDefinition(
            SweepRecords.SourceCartesian,
            tier,
            new SweepDesignRef(view.AcceptedId, view.SurfaceHash),
            null,
            water,
            hRefMeters,
            datum,
            pAtm,
            validatedSpeeds,
            validatedAngles,
            runMethod,
            settingsHash);

        return FinalizePreview(session, definition, cases, rows, runSettings, reference.CRef, hRefMeters, cacheHits, medianWallMs, measuredRunSamples);
    }

    public static SweepPreview BuildFromGoalState(
        AuthoringSession session,
        IWingMethod method,
        string tier,
        GoalStateSweepSnapshot goal,
        double? medianWallMs = null,
        int measuredRunSamples = 0)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(goal);
        if (tier is not (SweepRecords.TierVlmStrip or SweepRecords.TierEstimator))
            throw new ContractError("ANA-INPUT-TIER", $"Unsupported local sweep tier: {tier}");
        if (goal.OperatingPoints is null || goal.OperatingPoints.Count == 0)
            throw new ContractError("ANA-SWEEP-EMPTY", "Goal state defines no operating points.");
        if (goal.OperatingPoints.Count > SweepRecords.MaxCasesPerSweep)
            throw new ContractError("ANA-SWEEP-CASE-CAP", Labels.SweepCaseCapExceeded(goal.OperatingPoints.Count, SweepRecords.MaxCasesPerSweep));

        OperatingPoints.Validate(goal.Water);
        _ = WaterTable.At(goal.Water.TemperatureC, goal.Water.SalinityGPerKg);

        var view = session.Snapshot();
        var reference = ReferenceQuantities.From(view.Source);
        (RunMethod runMethod, RunSettings runSettings) = MethodForTier(method, tier);
        string settingsHash = RunRecord.SettingsHash(runSettings);

        var cases = new List<SweepCase>(goal.OperatingPoints.Count);
        var rows = new List<SweepPreviewRow>(goal.OperatingPoints.Count);
        var speeds = new List<double>(goal.OperatingPoints.Count);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        double? minDepth = null;
        bool allHaveDepth = true;

        for (int i = 0; i < goal.OperatingPoints.Count; i++)
        {
            var pt = goal.OperatingPoints[i];
            if (!double.IsFinite(pt.SpeedMetersPerSecond) || pt.SpeedMetersPerSecond <= 0)
                throw new ContractError("ANA-INPUT-SPEED", Labels.SweepSpeedNonPositive(pt.SpeedLabel));
            if (!double.IsFinite(pt.RequiredLoadN) || pt.RequiredLoadN <= 0)
                throw new ContractError("ANA-INPUT-LOAD", $"Goal-state point {pt.Label} requires positive finite load.");
            if (pt.HRefMeters is { } h && (!double.IsFinite(h) || h <= 0))
                throw new ContractError("ANA-INPUT-DEPTH", $"Goal-state point {pt.Label} has invalid depth.");
            if (!double.IsFinite(pt.PAtmPa) || pt.PAtmPa <= 0)
                throw new ContractError("ANA-INPUT-PRESSURE", $"Goal-state point {pt.Label} has invalid atmospheric pressure.");

            string caseKey = SweepRecords.CaseKey(pt.SpeedMetersPerSecond, pt.HRefMeters, pt.Datum, pt.PAtmPa, null, pt.RequiredLoadN);
            if (!seenKeys.Add(caseKey))
                throw new ContractError("ANA-SWEEP-DUPLICATE", Labels.SweepDuplicateValue("Goal-state points", pt.Label));

            if (pt.HRefMeters is { } depth) minDepth = minDepth is null ? depth : Math.Min(minDepth.Value, depth);
            else allHaveDepth = false;

            if (!speeds.Any(v => Math.Abs(v - pt.SpeedMetersPerSecond) <= DuplicateTolerance))
                speeds.Add(pt.SpeedMetersPerSecond);

            var previewOp = new OperatingPoint(pt.SpeedMetersPerSecond, pt.PAtmPa, pt.HRefMeters, pt.Datum, 0.0, pt.RequiredLoadN);
            var conditions = OperatingPoints.Derive(previewOp, goal.Water, reference.CRef);
            cases.Add(new SweepCase(i, caseKey, pt.Label, pt.SpeedMetersPerSecond, pt.HRefMeters, pt.Datum, pt.PAtmPa, null, pt.RequiredLoadN));
            rows.Add(new SweepPreviewRow(i, caseKey, pt.Label, pt.SpeedMetersPerSecond, pt.HRefMeters, pt.Datum, pt.PAtmPa, null, pt.RequiredLoadN, conditions, false, null));
        }

        double? effectiveDepth = allHaveDepth ? minDepth : null;
        var definition = new SweepDefinition(
            SweepRecords.SourceGoalState,
            tier,
            new SweepDesignRef(view.AcceptedId, view.SurfaceHash),
            new SweepGoalStateRef(goal.GoalStateId, goal.ContentHash),
            goal.Water,
            effectiveDepth,
            goal.OperatingPoints[0].Datum,
            goal.OperatingPoints[0].PAtmPa,
            speeds,
            [],
            runMethod,
            settingsHash);

        return FinalizePreview(session, definition, cases, rows, runSettings, reference.CRef, effectiveDepth, 0, medianWallMs, measuredRunSamples);
    }

    public static bool IsPreviewStale(SweepPreview preview, AuthoringSession session, IWingMethod method, string tier, out string? whatChanged)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(method);
        var view = session.Snapshot();
        if (view.SurfaceHash != preview.Definition.DesignRef.SurfaceHash || view.AcceptedId != preview.Definition.DesignRef.AcceptedId)
        {
            var oldRev = session.RevisionOf(preview.Definition.DesignRef.AcceptedId);
            var newRev = session.RevisionOf(view.AcceptedId);
            whatChanged = $"geometry changed (r{oldRev.Ordinal} → r{newRev.Ordinal})";
            return true;
        }
        (_, RunSettings runSettings) = MethodForTier(method, tier);
        string currentSettingsHash = RunRecord.SettingsHash(runSettings);
        if (preview.Definition.Tier != tier || preview.Definition.SettingsHash != currentSettingsHash)
        {
            whatChanged = "tier or method settings changed";
            return true;
        }
        whatChanged = null;
        return false;
    }

    internal static (RunMethod Method, RunSettings Settings) MethodForTier(IWingMethod method, string tier) =>
        tier == SweepRecords.TierEstimator
            ? (WingEstimator.Method, WingEstimator.Settings(method.Settings))
            : (method.Method, method.Settings);

    private static SweepPreview FinalizePreview(
        AuthoringSession session,
        SweepDefinition definition,
        IReadOnlyList<SweepCase> cases,
        IReadOnlyList<SweepPreviewRow> rows,
        RunSettings runSettings,
        double cRef,
        double? hRefMeters,
        int cacheHits,
        double? medianWallMs,
        int measuredRunSamples)
    {
        bool depthSet = hRefMeters is not null;
        bool deepWater = hRefMeters is double h && cRef > 0 && h / cRef >= Labels.DeepWaterHc;
        int panelsPerRun = definition.Tier == SweepRecords.TierEstimator ? 0 : 2 * runSettings.NSpanPerHalf * runSettings.NChord;
        int perCaseBytes = definition.Tier == SweepRecords.TierEstimator ? EstimatedEstimatorRunBytes : EstimatedVlmRunBytes;
        int uncached = Math.Max(0, cases.Count - cacheHits);
        int estimatedStorage = EstimatedExperimentBytes + uncached * perCaseBytes;
        int docBytes = session.DocumentBytes();
        int headroom = Math.Max(0, NativeProject.MaxBytes - docBytes);
        double? estWall = medianWallMs is double m && double.IsFinite(m) && measuredRunSamples > 0 ? m * uncached : null;

        return new SweepPreview(
            definition,
            cases,
            rows,
            panelsPerRun,
            estimatedStorage,
            docBytes,
            headroom,
            cacheHits,
            estWall,
            measuredRunSamples,
            depthSet,
            Labels.FixedVlm(deepWater),
            Labels.NotModelled(deepWater),
            cRef);
    }

    private static Dictionary<string, string> IntactCompletedByKey(AuthoringSession session)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var stored in session.ReadRuns().Runs)
        {
            if (stored.Integrity == RunIntegrity.Intact && stored.Run.Outcome is RunOutcome.Completed)
                map[stored.Run.RunKey] = stored.Run.RunId;
        }
        return map;
    }

    private static string[] SplitTokens(string rawText, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new ContractError("ANA-SWEEP-EMPTY", $"{fieldName} list cannot be empty.");
        var parts = rawText.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new ContractError("ANA-SWEEP-EMPTY", $"{fieldName} list cannot be empty.");
        return parts;
    }

    private static string NormalizeMinus(string token) => token.Replace('−', '-');
}
