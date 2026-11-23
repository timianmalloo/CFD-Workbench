using System.Text.RegularExpressions;
using CfdWorkbench.Core.CfdRun;

namespace CfdWorkbench.Core.Results;

/// <summary>Per-tier grouped objective/constraint snapshot for one candidate; tiers are NEVER averaged together.</summary>
public sealed record CandidateTierMetrics(
    string Tier,
    IReadOnlyList<CandidateObjectiveRecord> Objectives,
    IReadOnlyList<CandidateConstraintRecord> Constraints,
    IReadOnlyList<string> DiscrepancyKeys);

/// <summary>One ranked candidate row in the Area 6 Candidate Ladder.</summary>
public sealed record RankedCandidateRow(
    string CandidateId,
    string Status,
    bool Feasible,
    bool IsParetoOptimal,
    string PrimaryTier,
    IReadOnlyList<CandidateTierMetrics> TierGroups,
    string BaseAcceptedId,
    string BaseSurfaceHash,
    bool AcceptEnabled,
    string? AcceptDisabledReason);

/// <summary>One axis descriptor for parallel-coordinates display across feasible candidates.</summary>
public sealed record ParallelAxisDescriptor(
    string Name,
    string Unit,
    double Min,
    double Max);

/// <summary>Complete Candidate Ladder view model (Pareto front, tier-grouped metrics, parallel axes, and RES-05 empty reason).</summary>
public sealed record CandidateLadderView(
    IReadOnlyList<RankedCandidateRow> Candidates,
    IReadOnlyList<string> ParetoCandidateIds,
    IReadOnlyList<ParallelAxisDescriptor> ParallelAxes,
    string? EmptyReason);

/// <summary>Evidence bundle evaluated by <see cref="CandidateLadder.EvaluatePromotion"/> (COMMIT-01, CAND-01).</summary>
public sealed record PromotionEvidenceInput(
    CandidateRecord Candidate,
    string TargetStatus,
    IReadOnlyList<string> RequiredOperatingPointIds,
    IReadOnlyDictionary<string, StoredCfdRun> CompletedTargetRunsByPointId,
    IReadOnlyDictionary<string, string> DiscrepancyKeysByPointId,
    bool ObjectiveGainSurvived);

/// <summary>Outcome of evaluating whether a candidate has complete evidence to advance one rung on the ladder.</summary>
public sealed record PromotionDecision(
    bool Admitted,
    string CurrentStatus,
    string? NextStatus,
    string? RefusalCode,
    string? RefusalReason,
    IReadOnlyList<string> MissingOperatingPoints);

/// <summary>
/// Typed CAD geometry edit draft emitted by <c>Accept as draft</c> (COMMIT-01, spec B1:1440).
/// A candidate NEVER mutates accepted CAD geometry directly; Desktop opens this draft through the normal preview/Apply/Cancel path.
/// </summary>
public sealed record CandidateGeometryDraft(
    bool AcceptEnabled,
    string? RefusalReason,
    string CandidateId,
    string BaseAcceptedId,
    string TargetAcceptedId,
    string TargetSurfaceHash,
    bool IsRebased,
    double? RebaseDeviationMeters,
    IReadOnlyList<CandidateParameterDelta> ParameterDeltas);

/// <summary>
/// Pure zero-I/O Candidate Ladder engine (COMMIT-01, RES-05, CAND-01, docs/design/results-and-candidates.md §5).
/// Enforces no cross-tier averaging, exact 2-objective Pareto extraction, multipoint promotion evidence gates,
/// and base-revision drift Rebase guard on <see cref="BuildGeometryDraft"/>.
/// All method names strictly avoid ArchitectureTests banned verbs.
/// </summary>
public static class CandidateLadder
{
    private static readonly Regex Hex64 = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    private static readonly string[] LadderOrder =
    [
        "surrogate-candidate",
        "vlm-checked",
        "cfd-checked",
        "experimentally-compared"
    ];

    public static string FormatBaseMovedReason(string baseSurfaceHash)
    {
        string shortHash = string.IsNullOrEmpty(baseSurfaceHash)
            ? "unknown"
            : baseSurfaceHash[..Math.Min(12, baseSurfaceHash.Length)];
        return $"Accept disabled — base revision moved ({shortHash}). Use Rebase to preview over the current revision.";
    }

    public static CandidateLadderView RankCandidates(
        IReadOnlyList<StoredCandidate> storedCandidates,
        string currentAcceptedId,
        string currentSurfaceHash,
        string? experimentTerminalReason = null,
        string? objective1Name = null,
        string? objective2Name = null,
        bool? minimizeObjective1 = null,
        bool? minimizeObjective2 = null)
    {
        var intact = (storedCandidates ?? [])
            .Where(s => s is not null && s.Integrity == CfdRecordIntegrity.Intact)
            .Select(s => s.Candidate)
            .ToArray();

        var feasible = intact.Where(c => c.Feasible).ToArray();
        var paretoIds = ComputeParetoFront(feasible, objective1Name, objective2Name, minimizeObjective1, minimizeObjective2);
        var paretoSet = new HashSet<string>(paretoIds, StringComparer.Ordinal);
        var axes = BuildParallelAxes(feasible);

        var rows = new List<RankedCandidateRow>(intact.Length);
        foreach (var cand in intact)
        {
            bool baseMatches =
                string.Equals(cand.BaseAcceptedId, currentAcceptedId, StringComparison.Ordinal) &&
                string.Equals(cand.BaseSurfaceHash, currentSurfaceHash, StringComparison.Ordinal);

            // Strictly grouped by candidate's own tier — never averaged across tiers.
            var tierGroups = new CandidateTierMetrics[]
            {
                new(cand.Tier, cand.Objectives, cand.Constraints, cand.DiscrepancyKeys)
            };

            rows.Add(new RankedCandidateRow(
                CandidateId: cand.CandidateId,
                Status: cand.Status,
                Feasible: cand.Feasible,
                IsParetoOptimal: paretoSet.Contains(cand.CandidateId),
                PrimaryTier: cand.Tier,
                TierGroups: tierGroups,
                BaseAcceptedId: cand.BaseAcceptedId,
                BaseSurfaceHash: cand.BaseSurfaceHash,
                AcceptEnabled: baseMatches,
                AcceptDisabledReason: baseMatches ? null : FormatBaseMovedReason(cand.BaseSurfaceHash)));
        }

        // Order: feasible Pareto first, then feasible non-Pareto, then higher status rung, then infeasible.
        var orderedRows = rows
            .OrderByDescending(r => r.Feasible)
            .ThenByDescending(r => r.IsParetoOptimal)
            .ThenByDescending(r => StatusRank(r.Status))
            .ThenBy(r => r.CandidateId, StringComparer.Ordinal)
            .ToArray();

        string? emptyReason = feasible.Length == 0
            ? (string.IsNullOrWhiteSpace(experimentTerminalReason) ? "No feasible candidates found." : experimentTerminalReason.Trim())
            : null;

        return new CandidateLadderView(orderedRows, paretoIds, axes, emptyReason);
    }

    public static IReadOnlyList<string> ComputeParetoFront(
        IReadOnlyList<CandidateRecord> feasibleCandidates,
        string? objective1Name = null,
        string? objective2Name = null,
        bool? minimizeObjective1 = null,
        bool? minimizeObjective2 = null)
    {
        if (feasibleCandidates is null || feasibleCandidates.Count == 0)
            return [];

        var withTwoObjectives = feasibleCandidates.Where(c => c.Objectives.Count >= 2).ToArray();
        if (withTwoObjectives.Length == 0)
            return feasibleCandidates.Select(c => c.CandidateId).ToArray();

        string obj1 = objective1Name ?? withTwoObjectives[0].Objectives[0].Name;
        string obj2 = objective2Name ?? withTwoObjectives[0].Objectives[1].Name;
        bool min1 = minimizeObjective1 ?? ShouldMinimizeByName(obj1);
        bool min2 = minimizeObjective2 ?? ShouldMinimizeByName(obj2);

        var points = new List<(string Id, double V1, double V2)>();
        foreach (var cand in feasibleCandidates)
        {
            var m1 = cand.Objectives.FirstOrDefault(o => string.Equals(o.Name, obj1, StringComparison.OrdinalIgnoreCase));
            var m2 = cand.Objectives.FirstOrDefault(o => string.Equals(o.Name, obj2, StringComparison.OrdinalIgnoreCase));
            if (m1 is null || m2 is null || !double.IsFinite(m1.Value) || !double.IsFinite(m2.Value))
                continue;
            // Convert to minimization space so lower is always better.
            points.Add((cand.CandidateId, min1 ? m1.Value : -m1.Value, min2 ? m2.Value : -m2.Value));
        }

        var front = new List<string>();
        for (int i = 0; i < points.Count; i++)
        {
            bool dominated = false;
            var a = points[i];
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j) continue;
                var b = points[j];
                if (b.V1 <= a.V1 && b.V2 <= a.V2 && (b.V1 < a.V1 || b.V2 < a.V2))
                {
                    dominated = true;
                    break;
                }
            }
            if (!dominated) front.Add(a.Id);
        }

        return front;
    }

    /// <summary>
    /// Enforces COMMIT-01 / CAND-01 promotion evidence: advances one ladder rung ONLY when every multipoint operating
    /// point has a verified <c>Intact</c> <c>Completed</c> run on the candidate's geometry revision, a valid
    /// <c>DiscrepancyRecord</c> key exists for each point, and the objective gain survives at the higher tier.
    /// </summary>
    public static PromotionDecision EvaluatePromotion(PromotionEvidenceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Candidate);

        int currentRank = StatusRank(input.Candidate.Status);
        int targetRank = StatusRank(input.TargetStatus);
        if (currentRank < 0 || targetRank < 0 || targetRank != currentRank + 1)
        {
            return new PromotionDecision(
                Admitted: false,
                CurrentStatus: input.Candidate.Status,
                NextStatus: null,
                RefusalCode: "CAND-PROMOTION-ORDER",
                RefusalReason: $"Illegal candidate promotion from '{input.Candidate.Status}' to '{input.TargetStatus}'.",
                MissingOperatingPoints: []);
        }

        if (input.RequiredOperatingPointIds is null || input.RequiredOperatingPointIds.Count == 0)
        {
            return new PromotionDecision(
                Admitted: false,
                CurrentStatus: input.Candidate.Status,
                NextStatus: null,
                RefusalCode: "CAND-PROMOTION-INCOMPLETE",
                RefusalReason: "Promotion requires at least one operating point in the experiment multipoint set.",
                MissingOperatingPoints: []);
        }

        string expectedAcceptedId = input.Candidate.CandidateAcceptedId ?? input.Candidate.BaseAcceptedId;
        var missingRuns = new List<string>();
        var missingDiscrepancies = new List<string>();

        foreach (string pointId in input.RequiredOperatingPointIds)
        {
            bool runValid =
                input.CompletedTargetRunsByPointId is not null &&
                input.CompletedTargetRunsByPointId.TryGetValue(pointId, out var storedRun) &&
                storedRun is not null &&
                storedRun.Integrity == CfdRecordIntegrity.Intact &&
                storedRun.Run.CurrentState == CfdRunState.Completed &&
                string.Equals(storedRun.Run.Inputs.AcceptedId, expectedAcceptedId, StringComparison.Ordinal);

            if (!runValid)
                missingRuns.Add(pointId);

            bool discValid =
                input.DiscrepancyKeysByPointId is not null &&
                input.DiscrepancyKeysByPointId.TryGetValue(pointId, out var dk) &&
                dk is not null &&
                Hex64.IsMatch(dk);

            if (!discValid)
                missingDiscrepancies.Add(pointId);
        }

        if (missingRuns.Count > 0 || missingDiscrepancies.Count > 0)
        {
            var allMissing = missingRuns.Concat(missingDiscrepancies).Distinct(StringComparer.Ordinal).ToArray();
            string reason = missingRuns.Count > 0
                ? $"Promotion to '{input.TargetStatus}' refused: missing verified Completed target run(s) for operating point(s): {string.Join(", ", missingRuns)}."
                : $"Promotion to '{input.TargetStatus}' refused: missing DiscrepancyRecord key(s) for operating point(s): {string.Join(", ", missingDiscrepancies)}.";

            return new PromotionDecision(
                Admitted: false,
                CurrentStatus: input.Candidate.Status,
                NextStatus: null,
                RefusalCode: "CAND-PROMOTION-INCOMPLETE",
                RefusalReason: reason,
                MissingOperatingPoints: allMissing);
        }

        if (!input.ObjectiveGainSurvived)
        {
            return new PromotionDecision(
                Admitted: false,
                CurrentStatus: input.Candidate.Status,
                NextStatus: null,
                RefusalCode: "CAND-GAIN-NOT-SURVIVED",
                RefusalReason: $"Promotion to '{input.TargetStatus}' refused: stated objective gain did not survive at the higher tier.",
                MissingOperatingPoints: []);
        }

        return new PromotionDecision(
            Admitted: true,
            CurrentStatus: input.Candidate.Status,
            NextStatus: input.TargetStatus,
            RefusalCode: null,
            RefusalReason: null,
            MissingOperatingPoints: []);
    }

    /// <summary>
    /// Builds a typed CAD geometry edit draft for <c>Accept as draft</c> without mutating accepted geometry.
    /// Strictly disabled with Rebase guidance when the document's <paramref name="currentAcceptedId"/> has moved.
    /// </summary>
    public static CandidateGeometryDraft BuildGeometryDraft(
        CandidateRecord candidate,
        string currentAcceptedId,
        string currentSurfaceHash)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Guard.Require(!string.IsNullOrWhiteSpace(currentAcceptedId), "RES-DRAFT-REV");
        Guard.Require(!string.IsNullOrWhiteSpace(currentSurfaceHash), "RES-DRAFT-REV");

        bool baseMatches =
            string.Equals(candidate.BaseAcceptedId, currentAcceptedId, StringComparison.Ordinal) &&
            string.Equals(candidate.BaseSurfaceHash, currentSurfaceHash, StringComparison.Ordinal);

        if (!baseMatches)
        {
            return new CandidateGeometryDraft(
                AcceptEnabled: false,
                RefusalReason: FormatBaseMovedReason(candidate.BaseSurfaceHash),
                CandidateId: candidate.CandidateId,
                BaseAcceptedId: candidate.BaseAcceptedId,
                TargetAcceptedId: currentAcceptedId,
                TargetSurfaceHash: currentSurfaceHash,
                IsRebased: false,
                RebaseDeviationMeters: null,
                ParameterDeltas: []);
        }

        return new CandidateGeometryDraft(
            AcceptEnabled: true,
            RefusalReason: null,
            CandidateId: candidate.CandidateId,
            BaseAcceptedId: candidate.BaseAcceptedId,
            TargetAcceptedId: currentAcceptedId,
            TargetSurfaceHash: currentSurfaceHash,
            IsRebased: false,
            RebaseDeviationMeters: null,
            ParameterDeltas: candidate.ParameterDeltas);
    }

    /// <summary>Builds a rebased CAD geometry edit draft over a moved revision with explicit measured geometric deviation.</summary>
    public static CandidateGeometryDraft BuildRebasedDraft(
        CandidateRecord candidate,
        string currentAcceptedId,
        string currentSurfaceHash,
        double measuredDeviationMeters)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Guard.Require(!string.IsNullOrWhiteSpace(currentAcceptedId), "RES-DRAFT-REV");
        Guard.Require(!string.IsNullOrWhiteSpace(currentSurfaceHash), "RES-DRAFT-REV");
        Guard.Require(double.IsFinite(measuredDeviationMeters) && measuredDeviationMeters >= 0.0, "RES-REBASE-DEVIATION");

        return new CandidateGeometryDraft(
            AcceptEnabled: true,
            RefusalReason: null,
            CandidateId: candidate.CandidateId,
            BaseAcceptedId: candidate.BaseAcceptedId,
            TargetAcceptedId: currentAcceptedId,
            TargetSurfaceHash: currentSurfaceHash,
            IsRebased: true,
            RebaseDeviationMeters: measuredDeviationMeters,
            ParameterDeltas: candidate.ParameterDeltas);
    }

    private static IReadOnlyList<ParallelAxisDescriptor> BuildParallelAxes(IReadOnlyList<CandidateRecord> feasible)
    {
        if (feasible.Count == 0) return [];
        var stats = new Dictionary<string, (string Unit, double Min, double Max)>(StringComparer.Ordinal);
        var order = new List<string>();

        void AddValue(string name, string unit, double value)
        {
            if (!double.IsFinite(value)) return;
            if (!stats.TryGetValue(name, out var s))
            {
                order.Add(name);
                stats[name] = (unit, value, value);
            }
            else
            {
                stats[name] = (s.Unit, Math.Min(s.Min, value), Math.Max(s.Max, value));
            }
        }

        foreach (var cand in feasible)
        {
            foreach (var obj in cand.Objectives) AddValue(obj.Name, obj.Unit, obj.Value);
            foreach (var con in cand.Constraints) AddValue(con.Name, con.Unit, con.Value);
        }

        return order.Select(name =>
        {
            var s = stats[name];
            return new ParallelAxisDescriptor(name, s.Unit, s.Min, s.Max);
        }).ToArray();
    }

    private static int StatusRank(string status) => Array.IndexOf(LadderOrder, status);

    private static bool ShouldMinimizeByName(string name) =>
        name.Contains("CD", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("drag", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("mass", StringComparison.OrdinalIgnoreCase);
}
