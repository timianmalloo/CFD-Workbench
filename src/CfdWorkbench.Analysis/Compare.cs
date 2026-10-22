using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The wing coefficients of one run, as numbers: CL, CDi and e. A null is Unavailable, never a zero.</summary>
public sealed record Coefficients(double? Cl, double? Cdi, double? E);

/// <summary>How a stored Discrepancy record reads back (design a3d-compare.md 1.6, 1.7, 3). Never silently repaired or deleted.</summary>
public enum DiscrepancyIntegrity { Intact, PayloadFailedCheck, NoLongerReproduces, RuleUnknown, RunFailedCheck }

/// <summary>Why a numeric delta is blocked (design 2.2): the reference quantities, the operating point or the water differ.</summary>
public enum BlockReason { Reference, Point, Water }

/// <summary>One block reason and the names of the fields that differ, in a fixed order.</summary>
public sealed record Block(BlockReason Reason, IReadOnlyList<string> Fields);

/// <summary>
/// The single wing coefficients of a Completed VLM + strip run, shared by the Analysis projection and Compare (DM7: one
/// definition). CL is the near-field lift over q S_ref, CDi the Trefftz induced drag over q S_ref, e = CL_T^2 / (pi AR CDi).
/// </summary>
internal static class WingCoefficients
{
    internal static Coefficients Of(AnalysisRun run)
    {
        double q = 0.5 * run.Water.Rho * run.Op.Speed * run.Op.Speed;
        double a = VortexLattice.ToRadians(run.Op.AlphaDeg);
        double lift = run.Strips.Sum(s => -s.Fx * Math.Sin(a) + s.Fz * Math.Cos(a));
        double? cl = q > 0 && run.Reference.SRef > 0 ? lift / (q * run.Reference.SRef) : null;
        double? drag = Loads.InducedDrag(run).Value;
        double? cdi = drag.HasValue && q > 0 && run.Reference.SRef > 0 ? drag / (q * run.Reference.SRef) : null;
        double? trefftzLift = run.Strips.Count > 0 && run.Strips.All(s => Loads.StripWidth(run, s) > 0)
            ? run.Water.Rho * run.Op.Speed * run.Strips.Sum(s => s.Gamma * Loads.StripWidth(run, s)) : null;
        double? clTrefftz = trefftzLift.HasValue && q > 0 && run.Reference.SRef > 0
            ? trefftzLift / (q * run.Reference.SRef) : null;
        double ar = run.Reference.SRef > 0 ? run.Reference.BRef * run.Reference.BRef / run.Reference.SRef : 0;
        double? e = clTrefftz.HasValue && cdi > 0 && ar > 0 ? Trefftz.Oswald(clTrefftz.Value, ar, cdi.Value) : null;
        return new Coefficients(cl, cdi, e);
    }
}

/// <summary>
/// Compare (design a3d-compare.md 1-2): compatibility, the one rule that turns two runs into Discrepancy lines, and the
/// re-verification of a stored record. Canonical orientation and the key are defined once, in Core
/// (<see cref="DiscrepancyRecords"/>); this class calls them and defines nothing of its own. Nothing here combines two values:
/// no mean, blend, weighted value, winner or rank (A5.7).
/// </summary>
public static class Compare
{
    /// <summary>Rule 1.0.0 floors: the resolution below which a wing coefficient is noise (design 1.5).</summary>
    public const double ClFloor = 1e-4, CdiFloor = 1e-6, EFloor = 1e-3;
    public const string BaseZero = "ANA-CMP-BASE-ZERO", BaseFloor = "ANA-CMP-BASE-FLOOR", EIllConditioned = "ANA-CMP-E-ILL-CONDITIONED";
    public const string EstimatorE = "ANA-CMP-ESTIMATOR-E", ValueUnavailable = "ANA-CMP-VALUE-UNAVAILABLE", RuleUnknownCode = "ANA-CMP-RULE-UNKNOWN";

    /// <summary>One rule version's function from two runs to lines. A rule bump adds a function and keeps the old one (design 1.7).</summary>
    public delegate IReadOnlyList<DiscrepancyLine> RuleFunction(AnalysisRun first, AnalysisRun second, Func<AnalysisRun, Coefficients> values);

    internal static readonly IReadOnlyDictionary<string, RuleFunction> Rules = new Dictionary<string, RuleFunction>(StringComparer.Ordinal)
    {
        [CompareRule.V100] = Lines100
    };

    /// <summary>The coefficients of a run as numbers: the VLM + strip projection's, or the estimator's derived from its inputs and its accepted source.</summary>
    public static Func<AnalysisRun, Coefficients> Derivation(Func<string, byte[]?> acceptedSourceOf)
    {
        // A run is immutable, so its coefficients are derived once per derivation function.
        var known = new Dictionary<string, Coefficients>(StringComparer.Ordinal);
        return run =>
        {
            if (known.TryGetValue(run.RunId, out var cached)) return cached;
            return known[run.RunId] = Derive(run, acceptedSourceOf);
        };
    }

    private static Coefficients Derive(AnalysisRun run, Func<string, byte[]?> acceptedSourceOf)
    {
        if (run.Outcome is not RunOutcome.Completed) return new Coefficients(null, null, null);
        if (run.Tier == WingEstimator.TierId)
        {
            if (acceptedSourceOf(run.Inputs.AcceptedId) is not { } source) return new Coefficients(null, null, null);
            try
            {
                var outputs = WingEstimator.Derive(run, source);
                return new Coefficients(outputs.Cl, outputs.Cdi, outputs.E);
            }
            catch (ContractError) { return new Coefficients(null, null, null); }
        }
        return WingCoefficients.Of(run);
    }

    /// <summary>
    /// The block reasons of a pair, in the order reference, point, water, each with the names of the fields that differ (design
    /// 2.2). Exact equality, not a tolerance. Empty means Compatible. Never stored.
    /// </summary>
    public static IReadOnlyList<Block> Compatibility(AnalysisRun a, AnalysisRun b)
    {
        var blocks = new List<Block>();
        var reference = new List<string>();
        if (a.Reference.SRef != b.Reference.SRef) reference.Add("S_ref");
        if (a.Reference.BRef != b.Reference.BRef) reference.Add("b_ref");
        if (a.Reference.CRef != b.Reference.CRef) reference.Add("c_ref");
        if (a.Reference.MomentDatum != b.Reference.MomentDatum) reference.Add("moment datum");
        if (a.Reference.Axes != b.Reference.Axes) reference.Add("axes");
        if (reference.Count > 0) blocks.Add(new Block(BlockReason.Reference, reference));
        var point = new List<string>();
        if (a.Op.Speed != b.Op.Speed) point.Add("speed");
        if (a.Op.PAtm != b.Op.PAtm) point.Add("atmospheric pressure");
        if (a.Op.HRef != b.Op.HRef) point.Add("depth");
        if (a.Op.Datum != b.Op.Datum) point.Add("datum");
        if (a.Op.AlphaDeg != b.Op.AlphaDeg) point.Add("α");
        if (a.Op.Load != b.Op.Load) point.Add("load");
        if (point.Count > 0) blocks.Add(new Block(BlockReason.Point, point));
        var water = new List<string>();
        if (a.Water.TemperatureC != b.Water.TemperatureC) water.Add("temperature");
        if (a.Water.SalinityGPerKg != b.Water.SalinityGPerKg) water.Add("salinity");
        if (a.Water.Rho != b.Water.Rho) water.Add("ρ");
        if (a.Water.Nu != b.Water.Nu) water.Add("ν");
        if (a.Water.Pv != b.Water.Pv) water.Add("vapour pressure");
        if (a.Water.Source != b.Water.Source) water.Add("water source");
        if (a.Water.TableHash != b.Water.TableHash) water.Add("water table");
        if (water.Count > 0) blocks.Add(new Block(BlockReason.Water, water));
        return blocks;
    }

    /// <summary>
    /// The canonical orientation of a pair (Core's definition): lower tier first; the same tier, the earlier accepted revision,
    /// then the earlier run in <paramref name="documentOrder"/>. <paramref name="acceptedOrdinal"/> is the revision's
    /// accepted-row ordinal.
    /// </summary>
    public static (AnalysisRun First, AnalysisRun Second) Orient(IReadOnlyList<AnalysisRun> documentOrder, AnalysisRun a, AnalysisRun b,
        Func<string, int?> acceptedOrdinal)
    {
        int indexA = IndexOf(documentOrder, a), indexB = IndexOf(documentOrder, b);
        int ordinalA = acceptedOrdinal(a.Inputs.AcceptedId) ?? 0, ordinalB = acceptedOrdinal(b.Inputs.AcceptedId) ?? 0;
        return DiscrepancyRecords.AFirst(a.Tier, ordinalA, indexA, b.Tier, ordinalB, indexB) ? (a, b) : (b, a);
    }

    private static int IndexOf(IReadOnlyList<AnalysisRun> runs, AnalysisRun run)
    {
        for (int i = 0; i < runs.Count; i++) if (runs[i].RunId == run.RunId) return i;
        return int.MaxValue;
    }

    public static string Key(string baseRunId, string comparedRunId, CompareRule rule) => DiscrepancyRecords.Key(baseRunId, comparedRunId, rule);

    /// <summary>
    /// The one definition of the lines: the function of the rule version <paramref name="rule"/> names, over the two runs in the
    /// given orientation. Throws <c>ANA-CMP-RULE-UNKNOWN</c> for a rule this build does not know and
    /// <c>ANA-CMP-VALUE-UNAVAILABLE</c> when CL or CDi is Unavailable or not finite in either run (the pair is then not recorded).
    /// </summary>
    public static IReadOnlyList<DiscrepancyLine> Lines(AnalysisRun first, AnalysisRun second, CompareRule rule,
        Func<AnalysisRun, Coefficients> values, IReadOnlyDictionary<string, RuleFunction>? rules = null)
    {
        rules ??= Rules;
        if (rule.Id != CompareRule.RuleId || !rules.TryGetValue(rule.Version, out var function))
            throw new ContractError(RuleUnknownCode, "this build does not know the compare rule " + rule.Id + " " + rule.Version);
        return function(first, second, values);
    }

    /// <summary>A sealed record of the pair, in the orientation given (the canonical one). Ids are random, like run ids.</summary>
    public static DiscrepancyRecord Build(AnalysisRun first, AnalysisRun second, Func<AnalysisRun, Coefficients> values) =>
        DiscrepancyRecords.Seal(new DiscrepancyRecord(Guid.NewGuid().ToString("D"), "", CompareRule.Current, first.RunId, second.RunId,
            Lines(first, second, CompareRule.Current, values)));

    /// <summary>
    /// Re-verifies a stored record (design 1.6): the stored hash is never trusted, a failed referenced run is reported, the rule
    /// the record names re-derives its lines from the two runs, and the lines must match bit for bit. Nothing is rewritten.
    /// </summary>
    public static DiscrepancyIntegrity Verify(DiscrepancyRecord record, AnalysisRun? first, AnalysisRun? second,
        Func<AnalysisRun, Coefficients> values, IReadOnlyDictionary<string, RuleFunction>? rules = null)
    {
        try { if (record.ContentHash != DiscrepancyRecords.ContentHash(record)) return DiscrepancyIntegrity.PayloadFailedCheck; }
        catch (ContractError) { return DiscrepancyIntegrity.PayloadFailedCheck; }
        if (first is null || second is null || !RunRecord.Verify(first) || !RunRecord.Verify(second)) return DiscrepancyIntegrity.RunFailedCheck;
        try
        {
            var lines = Lines(first, second, record.Rule, values, rules);
            return lines.SequenceEqual(record.Lines) ? DiscrepancyIntegrity.Intact : DiscrepancyIntegrity.NoLongerReproduces;
        }
        catch (ContractError error) when (error.Code == RuleUnknownCode) { return DiscrepancyIntegrity.RuleUnknown; }
        catch (ContractError) { return DiscrepancyIntegrity.NoLongerReproduces; }
    }

    /// <summary>The result of comparing a pair: the block reasons (non-empty means no numeric delta and no record), else the record and whether this call created it.</summary>
    public sealed record CompareResult(IReadOnlyList<Block> Blocks, DiscrepancyRecord? Record, bool Created);

    /// <summary>
    /// Compares two Completed runs of one session (design 1.3, 2.2): a blocked pair stores nothing and emits a
    /// <c>blocked-*</c> event; a compatible pair is oriented canonically (either order is the same pair), looked up by key and
    /// either read or recorded under the session lock. Throws the session's <see cref="ContractError"/> when recording is
    /// refused (for example <c>DOC-SIZE</c>) and <c>ANA-CMP-VALUE-UNAVAILABLE</c> when a coefficient is Unavailable.
    /// </summary>
    public static CompareResult Record(AuthoringSession session, AnalysisRun shownBase, AnalysisRun shownCompared,
        Func<AnalysisRun, Coefficients>? values = null)
    {
        values ??= Derivation(session.AcceptedSourceOf);
        var blocks = Compatibility(shownBase, shownCompared);
        if (blocks.Count > 0)
        {
            session.RecordCompareEvent("blocked-" + blocks[0].Reason.ToString().ToLowerInvariant(), null,
                new CompareEvent { BaseTier = shownBase.Tier, ComparedTier = shownCompared.Tier, Records = session.ReadDiscrepancies().Count });
            return new CompareResult(blocks, null, false);
        }
        var documentOrder = session.ReadRuns().Runs.Select(stored => stored.Run).ToList();
        var (first, second) = Orient(documentOrder, shownBase, shownCompared, AcceptedOrdinal(session));
        string key = Key(first.RunId, second.RunId, CompareRule.Current);
        var hit = session.ReadDiscrepancies().FirstOrDefault(record => DiscrepancyRecords.Key(record) == key);
        var record = hit ?? Build(first, second, values);
        var outcome = session.RecordDiscrepancy(record);
        return new CompareResult([], outcome.Record, outcome.Created);
    }

    /// <summary>The accepted-row ordinal (r1 is the open) of a revision, null when the session holds none by that id.</summary>
    public static Func<string, int?> AcceptedOrdinal(AuthoringSession session) => id =>
    {
        try { return session.RevisionOf(id).Ordinal; }
        catch (ContractError) { return null; }
    };

    // Rule 1.0.0: CL, CDi and e, each normalised to the base run's value (design 1.5, 2.4).
    private static IReadOnlyList<DiscrepancyLine> Lines100(AnalysisRun first, AnalysisRun second, Func<AnalysisRun, Coefficients> values)
    {
        Coefficients b = values(first), c = values(second);
        if (!IsFinite(b.Cl) || !IsFinite(c.Cl) || !IsFinite(b.Cdi) || !IsFinite(c.Cdi))
            throw new ContractError(ValueUnavailable, "CL or CDi is not available in one of the runs");
        var cl = Line("CL", b.Cl!.Value, c.Cl!.Value, ClFloor);
        var cdi = Line("CDi", b.Cdi!.Value, c.Cdi!.Value, CdiFloor);
        return [cl, cdi, ELine(first, second, b, c, cl)];
    }

    private static DiscrepancyLine Line(string quantity, double b, double c, double floor)
    {
        if (b == 0) return new DiscrepancyLine(quantity, b, c, Undefined: BaseZero);
        if (Math.Abs(b) < floor) return new DiscrepancyLine(quantity, b, c, Undefined: BaseFloor);
        return new DiscrepancyLine(quantity, b, c, Delta: (c - b) / Math.Abs(b));
    }

    // The estimator's e is 1 by construction and never enters a delta (DR-ANA-11 (a)); near zero lift e is a 0/0 form and is
    // Undefined whenever its inputs are, so an absolute e floor alone is a last guard only.
    private static DiscrepancyLine ELine(AnalysisRun first, AnalysisRun second, Coefficients b, Coefficients c, DiscrepancyLine clLine)
    {
        bool firstEstimator = first.Tier == WingEstimator.TierId, secondEstimator = second.Tier == WingEstimator.TierId;
        if (firstEstimator || secondEstimator)
            return new DiscrepancyLine("e", firstEstimator ? null : b.E, secondEstimator ? null : c.E, NotCompared: EstimatorE);
        if (!IsFinite(b.E) || !IsFinite(c.E)) throw new ContractError(ValueUnavailable, "e is not available in one of the runs");
        if (clLine.Undefined is not null || Math.Abs(b.Cdi!.Value) < CdiFloor || Math.Abs(c.Cdi!.Value) < CdiFloor)
            return new DiscrepancyLine("e", b.E, c.E, Undefined: EIllConditioned);
        return Line("e", b.E!.Value, c.E!.Value, EFloor);
    }

    private static bool IsFinite(double? value) => value is { } v && double.IsFinite(v);
}
