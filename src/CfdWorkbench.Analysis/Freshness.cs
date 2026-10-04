using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The inputs a run would have if it were evaluated now; their key decides Current or Historical (design §3.4).</summary>
public sealed record CurrentInputs(RunInputs Inputs, WaterRecord Water, OperatingPoint Op, RunMethod Method, string SettingsHash);

/// <summary>
/// Freshness is run-key equality; nothing writes a freshness flag (decision-freshness-by-run-key). The current key is built
/// from the session's accepted revision now, never from the run's own stored inputs, and the stored key is never trusted:
/// a run is judged by the key recomputed from its manifest (<see cref="RunRecord.RecomputedKey"/>).
/// </summary>
public static class Freshness
{
    /// <summary>
    /// What a run reads from a snapshot: the accepted id and surface hash, the profile identity of each assignment in order,
    /// the evaluator and the placement-rule version (design §4 items 1 and 4). The service and freshness both call this,
    /// so a run and the current key cannot disagree about where an input comes from. Never <c>Draft.Bytes</c>.
    /// </summary>
    public static RunInputs Inputs(SessionView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        // Parse only: identities need no geometry assessment.
        var authored = FoilSource.Parse(view.Source).Authored();
        string evaluator = authored.Binding.Evaluator ?? throw new ContractError("DOC-INTEGRITY", "the accepted source does not parse");
        return new RunInputs(view.AcceptedId, view.SurfaceHash,
            authored.Assignments.Select(assignment => assignment.ProfileIdentity).ToArray(), evaluator, Placement.PlacementRuleVersion);
    }

    /// <summary>The current inputs of the session's accepted revision under a water record, operating point and method.</summary>
    public static CurrentInputs Current(SessionView view, WaterRecord water, OperatingPoint op, RunMethod method, RunSettings settings) =>
        new(Inputs(view), water, op, method, RunRecord.SettingsHash(settings));

    /// <summary>The key of the current inputs, through <see cref="RunRecord.Key"/> (the one definition).</summary>
    public static string CurrentKey(CurrentInputs current)
    {
        ArgumentNullException.ThrowIfNull(current);
        return RunRecord.Key(current.Inputs, current.Water, current.Op, current.Method, current.SettingsHash);
    }

    /// <summary>
    /// <see cref="RunState.Unavailable"/> when the run failed its check (ADR-0011 §4); otherwise
    /// <see cref="RunState.Current"/> when its recomputed key equals the current key, else <see cref="RunState.Historical"/>.
    /// </summary>
    public static RunState State(StoredRun stored, CurrentInputs current)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (stored.Integrity != RunIntegrity.Intact) return RunState.Unavailable;
        return RunRecord.RecomputedKey(stored.Run) == CurrentKey(current) ? RunState.Current : RunState.Historical;
    }

    /// <summary>
    /// The key fields that differ between a run's manifest and the current inputs, in key order (COPY-64's "what changed"):
    /// <c>surface</c>, <c>evaluator</c>, <c>placementRule</c>, <c>profiles</c>, <c>water</c>, <c>op.speed</c>, <c>op.pAtm</c>,
    /// <c>op.hRef</c>, <c>op.datum</c>, <c>op.alphaDeg</c>, <c>op.load</c>, <c>method.id</c>, <c>method.version</c>,
    /// <c>settings</c>. Empty exactly when the keys are equal. The projection words them (PRJ).
    /// </summary>
    public static IReadOnlyList<string> WhatChanged(AnalysisRun run, CurrentInputs current)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(current);
        var changed = new List<string>();
        void Compare(string field, bool equal) { if (!equal) changed.Add(field); }
        RunInputs was = run.Inputs, now = current.Inputs;
        Compare("surface", was.SurfaceHash == now.SurfaceHash);
        Compare("evaluator", was.Evaluator == now.Evaluator);
        Compare("placementRule", was.PlacementRule == now.PlacementRule);
        Compare("profiles", was.ProfileHashes.SequenceEqual(now.ProfileHashes));
        Compare("water", run.Water == current.Water);
        Compare("op.speed", run.Op.Speed.Equals(current.Op.Speed));
        Compare("op.pAtm", run.Op.PAtm.Equals(current.Op.PAtm));
        Compare("op.hRef", Nullable.Equals(run.Op.HRef, current.Op.HRef));
        Compare("op.datum", run.Op.Datum == current.Op.Datum);
        Compare("op.alphaDeg", run.Op.AlphaDeg.Equals(current.Op.AlphaDeg));
        Compare("op.load", Nullable.Equals(run.Op.Load, current.Op.Load));
        Compare("method.id", run.Method.Id == current.Method.Id);
        Compare("method.version", run.Method.Version == current.Method.Version);
        Compare("settings", RunRecord.SettingsHash(run.Settings) == current.SettingsHash);
        return changed;
    }
}
