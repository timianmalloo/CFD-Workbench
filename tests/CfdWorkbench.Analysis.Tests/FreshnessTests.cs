using System.Reflection;
using CfdWorkbench.Core;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Track SVC, ANA-07 freshness (design area3-analysis.md §13.3, §18.8): a run is Current exactly when the key recomputed from
/// its manifest equals the key of the session's current inputs; each key input changed alone makes it Historical and is
/// named by "what changed"; units are outside the key.
/// </summary>
internal static class FreshnessTests
{
    internal static void Run()
    {
        Check("Freshness_SurfaceEdit_Historical", SurfaceEdit);
        Check("Freshness_WaterChange_Historical", WaterChange);
        Check("Freshness_OperatingPointChange_Historical", OperatingPointChange);
        Check("Freshness_MethodVersionBump_Historical", MethodVersionBump);
        Check("Freshness_EachSettingsField_Historical", EachSettingsField);
        Check("Freshness_Pre117MethodVersion_HistoricalAndEvaluateComputesNew", Pre117MethodVersion);
        Check("Freshness_UndoToEqualKey_CurrentAgain", UndoToEqualKey);
        Check("Freshness_SaveReopen_Unchanged", SaveReopen);
        Check("Units_Lbf_KeyUnchanged", UnitsOutsideKey);
    }

    internal static void RunReadiness() => Check("Freshness_ProfileEdit_Historical", ProfileEdit);

    private static void SurfaceEdit()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op);
        Equal(RunState.Current, Fixture.StateOf(session, run, Fixture.Current(session, op)), "before the edit");
        Fixture.TwistEdit(session, 1.0);
        var current = Fixture.Current(session, op);
        Equal(RunState.Historical, Fixture.StateOf(session, run, current), "after a twist edit");
        Equal("surface", string.Join(",", Freshness.WhatChanged(run, current)), "what changed");
    }

    // One profile coordinate moves; the run is Historical and both the surface and the profile hashes changed (§3.4 assume).
    private static void ProfileEdit()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op);
        string draftId = Fixture.Id();
        var view = session.BeginSectionDraft(draftId, 0);
        var vertex = Sections.View(view.Bytes, 0, SurfaceSide.Upper, "accepted", 0).Points.First(point => point.Id == "cv-3");
        view = session.ApplySectionStep(draftId, view.Generation, new SectionStep.Move(SurfaceSide.Upper, vertex.Id, vertex.Eta, vertex.Ordinate + 0.002));
        session.FinishSection(Fixture.Id(), session.AssessSection(draftId, view.Generation, CancellationToken.None));
        var current = Fixture.Current(session, op);
        Equal(RunState.Historical, Fixture.StateOf(session, run, current), "after a profile edit");
        var changed = Freshness.WhatChanged(run, current);
        Equal(true, changed.Contains("surface") && changed.Contains("profiles"), "surface and profile hashes both changed: " + string.Join(",", changed) + ";");
    }

    private static void WaterChange()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op, Fixture.Fresh);
        Equal(RunState.Current, Fixture.StateOf(session, run, Fixture.Current(session, op, Fixture.Fresh)), "fresh");
        var current = Fixture.Current(session, op, Fixture.Salt);
        Equal(RunState.Historical, Fixture.StateOf(session, run, current), "fresh → salt at 15 °C");
        Equal("water", string.Join(",", Freshness.WhatChanged(run, current)), "what changed");
    }

    // α 3.00° → 3.01°: no rounding before hashing.
    private static void OperatingPointChange()
    {
        using var session = Fixture.Opened();
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(3.00));
        var current = Fixture.Current(session, Fixture.Op(3.01));
        Equal(RunState.Historical, Fixture.StateOf(session, run, current), "α 3.00° → 3.01°");
        Equal("op.alphaDeg", string.Join(",", Freshness.WhatChanged(run, current)), "what changed");
    }

    private static void MethodVersionBump()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op);
        var bumped = new FakeWing { Method = new RunMethod("cfdw.vlm-strip", "1.0.1", 1) };
        var current = Fixture.Current(session, op, wing: bumped);
        Equal(RunState.Historical, Fixture.StateOf(session, run, current), "1.0.0 → 1.0.1");
        Equal("method.version", string.Join(",", Freshness.WhatChanged(run, current)), "what changed");
    }

    // Ruling 117 changed the coupling, so the product method version moved on from 1.3.0: a run stored under it must not be
    // re-served as the current one, and Evaluate must compute a new run.
    private static void Pre117MethodVersion()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var old = new FakeWing { Method = new RunMethod("cfdw.vlm-strip", "1.3.0/panel200-gov400-te3", 1) };
        var stale = Fixture.Evaluate(new AnalysisService(session, old), op);
        var product = new FakeWing { Method = MethodRecord.VlmStrip.Method };
        Equal(RunState.Historical, Fixture.StateOf(session, stale, Fixture.Current(session, op, wing: product)), "pre-117 run");
        var fresh = Fixture.Evaluate(new AnalysisService(session, product), op);
        Equal(true, fresh.RunKey != stale.RunKey, "Evaluate computes a new run, not the stored one");
        Equal(2, session.ReadRuns().Runs.Count, "both runs stored");
    }

    // Each settings field changed alone makes the run Historical; the failure names the field. A field added to
    // RunSettings without a variant here fails the count.
    private static void EachSettingsField()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var wing = new FakeWing();
        var run = Fixture.Evaluate(new AnalysisService(session, wing), op);
        var s = wing.Settings;
        var variants = new (string Field, RunSettings Settings)[]
        {
            ("NSpanPerHalf", s with { NSpanPerHalf = 32 }), ("NChord", s with { NChord = 8 }),
            ("SpanSpacing", s with { SpanSpacing = "uniform" }), ("ChordSpacing", s with { ChordSpacing = "uniform" }),
            ("WakeSpans", s with { WakeSpans = 40 }), ("WakeDirection", s with { WakeDirection = "freestream" }),
            ("SingularityCutoff", s with { SingularityCutoff = 1e-9 }), ("Envelope", s with { Envelope = "vlm-envelope/2" }),
            ("Polar", s with { Polar = new RunPolar("cfdw.polar", "1.0.0", "m") }), ("Ncrit", s with { Ncrit = [2, 9] }),
            ("SurfaceState", s with { SurfaceState = "rough" }), ("TeFloorMm", s with { TeFloorMm = 0.5 }),
            ("SectionEtas", s with { SectionEtas = [0, 1] }), ("SectionXs", s with { SectionXs = [0, 0.5, 1] })
        };
        Equal(typeof(RunSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Length, variants.Length, "settings fields covered");
        foreach (var (field, settings) in variants)
        {
            var current = Fixture.Current(session, op, wing: new FakeWing { Settings = settings });
            Equal(RunState.Historical, Fixture.StateOf(session, run, current), field + " changed alone:");
            Equal("settings", string.Join(",", Freshness.WhatChanged(run, current)), field + " what changed");
        }
    }

    // Edit, then Undo back to equal inputs: the same run is Current again (no stored flag; the accepted id is not keyed).
    private static void UndoToEqualKey()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op);
        Fixture.TwistEdit(session, 1.0);
        Equal(RunState.Historical, Fixture.StateOf(session, run, Fixture.Current(session, op)), "after the edit");
        session.Undo(Fixture.Id());
        Equal(RunState.Current, Fixture.StateOf(session, run, Fixture.Current(session, op)), "after Undo");
    }

    // One Current and one Historical run survive save and reopen with the same states and keys.
    private static void SaveReopen()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var service = new AnalysisService(session, new FakeWing());
        var current = Fixture.Evaluate(service, op);
        var historical = Fixture.Evaluate(service, Fixture.Op(3.0));
        var before = new[] { Fixture.StateOf(session, current, Fixture.Current(session, op)), Fixture.StateOf(session, historical, Fixture.Current(session, op)) };
        Equal("Current,Historical", string.Join(",", before), "before save");
        using var reopened = new AuthoringSession();
        reopened.Reopen(session.SaveImage());
        var after = new[] { Fixture.StateOf(reopened, current, Fixture.Current(reopened, op)), Fixture.StateOf(reopened, historical, Fixture.Current(reopened, op)) };
        Equal(string.Join(",", before), string.Join(",", after), "after reopen");
        Equal(current.RunKey, RunRecord.RecomputedKey(reopened.ReadRuns().Runs[0].Run), "recomputed key after reopen");
    }

    // Display units are Type-1 by decision (ANA-18, FM-6): no key input carries them, so N → lbf leaves the key and the
    // state unchanged. A Units member added to any key input (settings, op, water, method, inputs) turns this red.
    private static void UnitsOutsideKey()
    {
        using var session = Fixture.Opened();
        var op = Fixture.Op(2.0);
        var run = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), op);
        foreach (var type in new[] { typeof(CurrentInputs), typeof(RunSettings), typeof(OperatingPoint), typeof(WaterRecord), typeof(RunMethod), typeof(RunInputs) })
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                Equal(false, property.PropertyType == typeof(Units) || property.Name.Contains("unit", StringComparison.OrdinalIgnoreCase),
                    $"{type.Name}.{property.Name} carries units into the key:");
        var current = Fixture.Current(session, op);
        foreach (var units in Enum.GetValues<Units>())
        {
            Equal(run.RunKey, Freshness.CurrentKey(current), units + " key");
            Equal(RunState.Current, Fixture.StateOf(session, run, current), units + " state");
        }
    }
}
