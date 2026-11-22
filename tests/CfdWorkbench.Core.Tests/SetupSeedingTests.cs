using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Node SET-1 red-first tests (T1, T2, T2b, T3–T8, T10, T12 in docs/design/setup-seeding.md §7, Ruling 272):
/// exact 2D deterministic soft-target solver (kappa(lam) = (3+lam)/4, lexicographic J1/J2/J3), 200-trial dense
/// grid oracle check, GEO-01 exact sqrt span, Ruling 93 TipChord minimum, SET-03 equal SurfaceHash/GoalStateHash
/// across form vs proposal fixture, re-seed history &amp; Undo, GEO-04 derived RecipeStatus detachment &amp; Undo relink,
/// all 9 discipline presets + wakefoil mapping + GEN fallback + SETUP-FLAGGED-UNACK gate, immutable GoalStateVersion
/// appends without editing geometry, live CurrentGoalSweepSnapshot seam, and NativeProject round-trip &amp; fail-closed checks.
/// </summary>
internal static class SetupSeedingTests
{
    internal static void Run()
    {
        Check("Seeder_PrimaryRaceFixture_ExactTargetsAndCertified", T1_PrimaryRaceFixture);
        Check("Seeder_ConflictingTriple_WeightSpanOne_ExactSpanAndConflictNotice", T2_ConflictingTriple);
        Check("Seeder_StationaryFamilies_BeatsDenseGridOracle", T2b_StationaryFamiliesVsGrid);
        Check("Seeder_Geo01_AreaAndAr_ExactSqrtSpan", T3_Geo01ExactSqrtSpan);
        Check("Seeder_TipChordMinimum_NeverViolated", T4_TipChordMinimumNeverViolated);
        Check("Seeder_SameInputsFormAndProposalFixture_EqualSurfaceAndGoalHash", T5_FormAndProposalEqualHashes);
        Check("Session_ReseedOnEditedDoc_PreservesHistoryAndMarksRunsHistorical", T6_ReseedPreservesHistory);
        Check("Session_FirstManualEdit_DetachesRecipeAndUndoRelinks", T7_ManualEditDetachesAndUndoRelinks);
        Check("Presets_AllNineDisciplines_CertifiedNoErrorsAndGenFallbackNamed", T8_AllNinePresetsAndFlaggedGate);
        Check("GoalState_EditConstraint_AppendsVersionNeverEditsGeometry", T10_GoalStateAppendNeverEditsGeometry);
        Check("NativeProject_SetupRoundTrip_ByteEqual_AndPreSetReaderFailsClosed", T12_NativeProjectRoundTripAndFailClosed);
    }

    // T1 (SET-01, GEO-01): Primary race fixture (1.10 m, 120 mm, 1000 cm2, AR 12, lam 0.24) yields exact
    // b = 1.095445115... m, S = 0.100000 m2, AR = 12.00 via WingEstimates.From, conflict = false, Certified.
    // Mutant: replacing kappa(lam) with 1.0 causes S and AR to miss by ~16 %.
    private static void T1_PrimaryRaceFixture()
    {
        var targets = MakeTargets(bMax: 1.10, wb: 1, cMax: 0.120, wc: 1, area: 0.100, wS: 1, ar: 12.0, wAr: 1, taper: 0.24);
        double expectedRoot = Math.Sqrt(0.100 / 12.0) / 0.81;
        double expectedTip = 0.24 * expectedRoot;
        var res = RecipeSeeder.Solve(new RecipeSolveInput(targets, Math.Sqrt(1.2), expectedRoot));
        Equal(false, res.Conflict);
        Near(Math.Sqrt(1.2), res.Recipe.SpanMeters, 1e-12, "span");
        Near(0.100, res.AreaSquareMeters, 1e-12, "area");
        Near(12.0, res.AspectRatio, 1e-12, "AR");
        Near(expectedRoot, res.Recipe.RootChordMeters, 1e-12, "c_root");
        Near(expectedTip, res.Recipe.TipChordMeters, 1e-12, "c_tip");

        var naca4412 = Catalog.Load().Single(c => c.Id == "naca-4412");
        byte[] dsl = RecipeSeeder.EmitFoilDsl(res.Recipe, naca4412, "Wingfoil race");
        var parsed = FoilSource.Parse(dsl);
        Equal(true, parsed.IsParsed);
        var assessment = Geometry.Assess(parsed);
        Equal(GeometryStatus.Certified, assessment.Status);

        var est = WingEstimates.From(dsl, "accepted", 0);
        Near(res.Recipe.SpanMeters, est.SpanMeters, 1e-12, "est.span");
        Near(0.100, est.AreaSquareMeters, 1e-12, "est.area");
        Near(12.0, est.AspectRatio, 1e-12, "est.AR");
        Near(res.Recipe.RootChordMeters, est.RootChordMeters, 1e-12, "est.c_root");
        Near(res.Recipe.TipChordMeters, est.TipChordMeters, 1e-12, "est.c_tip");
        Near(res.MacMeters, est.MacMeters, 1e-10, "est.MAC");
    }

    // T2 (SET-01): Conflicting triple (S 1000 cm2, b_max 0.80 m, c_max 100 mm, lam 0.24) with w = (1, 0, 0, 0)
    // hits b = 0.800000 m exact, c_root = 0.100 m, S = 0.0648 m2, conflict = true, COPY-73 populated, Certified.
    // Also checks tight-span compromise (b_max 1.077 m, w=(2,1,2,1)) -> AR 11.60 (-3 %).
    // Mutant: omitting J2 tie-breaker returns b_lo = 0.10 m or throws on conflict.
    private static void T2_ConflictingTriple()
    {
        var triple = MakeTargets(bMax: 0.80, wb: 1, cMax: 0.100, wc: 0, area: 0.100, wS: 0, ar: null, wAr: 0, taper: 0.24);
        var res = RecipeSeeder.Solve(new RecipeSolveInput(triple, 1.0, 0.11));
        Equal(true, res.Conflict);
        Near(0.80, res.Recipe.SpanMeters, 1e-12, "conflict span exact");
        Near(0.100, res.Recipe.RootChordMeters, 1e-12, "conflict root chord exact");
        Near(0.0648, res.AreaSquareMeters, 1e-12, "conflict area");
        Equal(true, res.Recipe.ConflictNotice is not null &&
            res.Recipe.ConflictNotice.Contains("Targets conflict:", StringComparison.Ordinal) &&
            res.Recipe.ConflictNotice.Contains("seeded the nearest feasible design; targets stay as preferences", StringComparison.Ordinal));

        var naca4412 = Catalog.Load().Single(c => c.Id == "naca-4412");
        byte[] dsl = RecipeSeeder.EmitFoilDsl(res.Recipe, naca4412);
        var parsed = FoilSource.Parse(dsl);
        Equal(true, parsed.IsParsed);
        Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);

        // Tight-span compromise fixture from design §3.4.
        var tight = MakeTargets(bMax: 1.077, wb: 2, cMax: 0.120, wc: 1, area: 0.100, wS: 2, ar: 12.0, wAr: 1, taper: 0.24);
        var tightRes = RecipeSeeder.Solve(new RecipeSolveInput(tight, 1.095445, 0.1127));
        Equal(true, tightRes.Conflict);
        Near(1.077, tightRes.Recipe.SpanMeters, 1e-12, "tight span");
        Near(0.100, tightRes.AreaSquareMeters, 1e-12, "tight area");
        Near(1.077 * 1.077 / 0.100, tightRes.AspectRatio, 1e-9, "tight AR ~11.60");
    }

    // T2b (SET-01): 200 seeded adversarial target/weight trials (including stationary winners on fixed-b and S = S*)
    // beat a dense 2D grid oracle (J1 <= J1_grid + 1e-9), and emitted WingEstimates matches solver to 1e-12 relative.
    // Mutant: dropping fixed-b c_stat or S = S* bisection stationary families fails against the dense grid oracle.
    private static void T2b_StationaryFamiliesVsGrid()
    {
        var rng = new Random(20261119);
        var naca4412 = Catalog.Load().Single(c => c.Id == "naca-4412");

        // Targeted stationary-family cases + 200 adversarial random trials.
        for (int trial = 0; trial < 200; trial++)
        {
            double taper = trial % 10 == 0 ? 0.05 : 0.15 + rng.NextDouble() * 0.65;
            double bMax = 0.50 + rng.NextDouble() * 1.10;
            double cMax = 0.06 + rng.NextDouble() * 0.14;
            double area = 0.05 + rng.NextDouble() * 0.18;
            double ar = 6.0 + rng.NextDouble() * 10.0;
            double wb = trial % 7 == 0 ? 0.0 : 0.1 + rng.NextDouble() * 4.0;
            double wc = trial % 5 == 0 ? 0.0 : 0.1 + rng.NextDouble() * 4.0;
            double wS = trial % 6 == 0 ? 0.0 : 0.1 + rng.NextDouble() * 4.0;
            double wAr = trial % 8 == 0 ? 0.0 : 0.1 + rng.NextDouble() * 4.0;
            if (wb + wc + wS + wAr <= 0) wS = 1.0;

            var targets = MakeTargets(bMax, wb, cMax, wc, area, wS, ar, wAr, taper);
            var res = RecipeSeeder.Solve(new RecipeSolveInput(targets, 1.0, 0.12));
            double j1Solved = RawJ1(res.Recipe.SpanMeters, res.Recipe.RootChordMeters, taper, bMax, wb, cMax, wc, area, wS, ar, wAr);

            // Dense 2D grid oracle over feasible box [bLo, bHi] x [cLo, cHi] plus hyperbola S=S* & ray AR=AR* sweeps.
            double lam = Math.Clamp(taper, RecipeSeeder.MinTaper, RecipeSeeder.MaxTaper);
            double k = RecipeSeeder.Kappa(lam);
            double cLo = Math.Max(RecipeSeeder.MinRootChordMeters, TipChord.FloorMeters / lam);
            double bestGrid = double.PositiveInfinity;

            // Coarse-to-focused grid around target region plus full box.
            const int nB = 60, nC = 60;
            for (int ib = 0; ib <= nB; ib++)
            {
                double b = RecipeSeeder.MinSpanMeters + (2.5 - RecipeSeeder.MinSpanMeters) * ib / nB;
                for (int ic = 0; ic <= nC; ic++)
                {
                    double c = cLo + (0.50 - cLo) * ic / nC;
                    double j1 = RawJ1(b, c, lam, bMax, wb, cMax, wc, area, wS, ar, wAr);
                    if (j1 < bestGrid) bestGrid = j1;
                }
                // Also sample exact S = S* hyperbola at this b.
                double cOnS = Math.Clamp(area / (b * k), cLo, RecipeSeeder.MaxRootChordMeters);
                double jS = RawJ1(b, cOnS, lam, bMax, wb, cMax, wc, area, wS, ar, wAr);
                if (jS < bestGrid) bestGrid = jS;
            }
            // Also sample along fixed b = bMax for interior fixed-b stationary points.
            double bClamped = Math.Clamp(bMax, RecipeSeeder.MinSpanMeters, RecipeSeeder.MaxSpanMeters);
            for (int ic = 0; ic <= 200; ic++)
            {
                double c = cLo + (0.60 - cLo) * ic / 200.0;
                double j1 = RawJ1(bClamped, c, lam, bMax, wb, cMax, wc, area, wS, ar, wAr);
                if (j1 < bestGrid) bestGrid = j1;
            }

            if (j1Solved > bestGrid + 1e-9)
                throw new InvalidOperationException($"Trial {trial}: solver J1={j1Solved:G17} exceeded grid oracle J1={bestGrid:G17}");

            // Every 25th trial, emit FoilDSL and verify exact WingEstimates identity to 1e-12 relative.
            if (trial % 25 == 0)
            {
                byte[] dsl = RecipeSeeder.EmitFoilDsl(res.Recipe, naca4412);
                var est = WingEstimates.From(dsl, "accepted", 0);
                NearRel(res.Recipe.SpanMeters, est.SpanMeters, 1e-12, $"trial {trial} span");
                NearRel(res.AreaSquareMeters, est.AreaSquareMeters, 1e-12, $"trial {trial} area");
                NearRel(res.AspectRatio, est.AspectRatio, 1e-12, $"trial {trial} AR");
                NearRel(res.Recipe.RootChordMeters, est.RootChordMeters, 1e-12, $"trial {trial} c_root");
                NearRel(res.Recipe.TipChordMeters, est.TipChordMeters, 1e-12, $"trial {trial} c_tip");
                NearRel(res.MacMeters, est.MacMeters, 1e-10, $"trial {trial} MAC");
            }
        }
    }

    // T3 (GEO-01): Area 0.14 m2, AR 7.0, taper 0.30 produces b = sqrt(0.98) m within 1e-12 relative on WingEstimates.From.
    // Mutant: rounding ExactDecimal to 3 decimals fails the 1e-12 relative tolerance.
    private static void T3_Geo01ExactSqrtSpan()
    {
        var targets = MakeTargets(bMax: null, wb: 0, cMax: null, wc: 0, area: 0.14, wS: 1, ar: 7.0, wAr: 1, taper: 0.30);
        var res = RecipeSeeder.Solve(new RecipeSolveInput(targets, 1.0, 0.15));
        Equal(false, res.Conflict);
        double expectedSpan = Math.Sqrt(0.98);
        var naca4412 = Catalog.Load().Single(c => c.Id == "naca-4412");
        byte[] dsl = RecipeSeeder.EmitFoilDsl(res.Recipe, naca4412);
        var est = WingEstimates.From(dsl, "accepted", 0);
        NearRel(expectedSpan, est.SpanMeters, 1e-12, "GEO-01 sqrt(0.98) span");
        NearRel(0.14, est.AreaSquareMeters, 1e-12, "GEO-01 area");
        NearRel(7.0, est.AspectRatio, 1e-12, "GEO-01 AR");
    }

    // T4 (Ruling 93): Extreme small targets (b_max 0.2 m, c_max 15 mm, lam 0.05) clamp c_tip >= TipChord.MinimumMeters(c_root)
    // and pass AuthoringSession.Seed without DSL-TIP-CHORD-MIN.
    // Mutant: dropping 0.005 / lam_eff lower bound in Omega causes DSL-TIP-CHORD-MIN refusal.
    private static void T4_TipChordMinimumNeverViolated()
    {
        var targets = MakeTargets(bMax: 0.20, wb: 1, cMax: 0.015, wc: 1, area: 0.003, wS: 1, ar: 10.0, wAr: 1, taper: 0.05);
        var res = RecipeSeeder.Solve(new RecipeSolveInput(targets, 0.20, 0.10));
        Equal(true, res.TipClampActive);
        Equal(true, TipChord.Meets(res.Recipe.TipChordMeters, res.Recipe.RootChordMeters));

        using var session = new AuthoringSession();
        var outcome = session.Seed(new SetupSeedRequest("wingfoil-race", SoftTargets: targets, AcknowledgeAllFlagged: true), Id());
        var est = WingEstimates.From(session.Snapshot().Source, "accepted", 0);
        Equal(true, TipChord.Meets(est.TipChordMeters, est.RootChordMeters));
        Equal(1, outcome.RevisionOrdinal);
    }

    // T5 (SET-03): Form brief and recorded setup-seed proposal fixture with identical physical fields yield identical
    // SurfaceHash and GoalStateVersion.ContentHash, differing only in entryKind and provenance.
    // Mutant: hashing entryKind or provenance into GoalStateVersion causes hashes to diverge.
    private static void T5_FormAndProposalEqualHashes()
    {
        var formTargets = MakeTargets(1.10, 1, 0.120, 1, 0.100, 1, 12.0, 1, 0.24, provenance: "Stated", source: "User parameter form");
        var proposalTargets = MakeTargets(1.10, 1, 0.120, 1, 0.100, 1, 12.0, 1, 0.24, provenance: "Inferred", source: "Recorded setup-seed language proposal");

        using var formSession = new AuthoringSession();
        var formOut = formSession.Seed(new SetupSeedRequest(
            Purpose: "wingfoil-race",
            SoftTargets: formTargets,
            EntryKind: "parameters",
            AcknowledgeAllFlagged: true), Id());

        using var proposalSession = new AuthoringSession();
        var propOut = proposalSession.Seed(new SetupSeedRequest(
            Purpose: "wingfoil-race",
            SoftTargets: proposalTargets,
            EntryKind: "language",
            Text: "1000 cm2 AR 12 wingfoil race wing, 90 kg rider",
            LanguageMeta: "{\"fixture\":\"recorded-proposal-v1\"}",
            AcknowledgeAllFlagged: true), Id());

        Equal("parameters", formOut.Brief.EntryKind);
        Equal("language", propOut.Brief.EntryKind);
        Equal(formOut.SurfaceHash, propOut.SurfaceHash);
        Equal(formOut.GoalState.ContentHash, propOut.GoalState.ContentHash);
        Equal(true, formOut.Brief.ContentHash != propOut.Brief.ContentHash);
    }

    // T6 (SET-03, ANA-07): Seed r1, manual edit r2, record VLM run (matching r2 surface), re-seed r3:
    // accepted.Count == 3, briefs.Count == 2, run's surface no longer matches active r3 (Historical);
    // Undo restores r2 and run matches active surface again (Current).
    // Mutant: overwriting accepted[0] or briefs[0] in place breaks counts/history/Undo.
    private static void T6_ReseedPreservesHistory()
    {
        using var session = new AuthoringSession();
        var r1 = session.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Id());
        Equal(1, r1.RevisionOrdinal);
        Equal(false, r1.IsReseed);

        // Manual dimension edit -> r2.
        string r2Id = session.ApplyDimension(Id(), new DimensionCommand("span", "1050"));
        var snap2 = session.Snapshot();
        Equal(r2Id, snap2.AcceptedId);

        // Record a VLM run on r2.
        var runOnR2 = SampleRunOnCurrent(session);
        session.RecordRun(runOnR2);
        Equal(snap2.SurfaceHash, runOnR2.Inputs.SurfaceHash);

        // Re-seed with a different preset -> r3.
        var r3 = session.Seed(new SetupSeedRequest("downwind", AcknowledgeAllFlagged: true), Id());
        Equal(true, r3.IsReseed);
        Equal(3, r3.RevisionOrdinal);

        var env = session.Envelope();
        Equal(3, env.Accepted.Length);
        Equal(2, env.Setup!.Briefs.Count);
        Equal(2, env.Setup.GoalStates.Count);
        Equal("seed", env.Accepted[2].Edit!.Rail);
        Equal(r3.Brief.BriefId, env.Accepted[2].Edit!.VertexId);
        // Active r3 surface differs from r2's recorded run -> run is Historical on r3.
        Equal(false, string.Equals(session.Snapshot().SurfaceHash, runOnR2.Inputs.SurfaceHash, StringComparison.Ordinal));

        // Undo from r3 restores r2 -> r2's recorded run matches active surface again (Current)!
        session.Undo(Id());
        Equal(r2Id, session.Snapshot().AcceptedId);
        Equal(snap2.SurfaceHash, runOnR2.Inputs.SurfaceHash);
        // Briefs & goalStates remain append-only across Undo.
        Equal(2, session.ReadSetup()!.Briefs.Count);
    }

    // T7 (GEO-04): After Seed: RecipeStatus == Linked; after first manual edit: DirectParametric; after Undo: Linked.
    // Mutant: storing a mutable bool detached flag not restored on Undo fails the Undo relink check.
    private static void T7_ManualEditDetachesAndUndoRelinks()
    {
        using var session = new AuthoringSession();
        var seeded = session.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Id());
        var st1 = session.RecipeStatus();
        Equal(RecipeLinkState.Linked, st1.State);
        Equal("wingfoil-race", st1.PresetId);
        Equal(seeded.AcceptedId, st1.SeededAcceptedId);

        // First manual edit detaches starter recipe.
        session.ApplyDimension(Id(), new DimensionCommand("span", "1080"));
        var st2 = session.RecipeStatus();
        Equal(RecipeLinkState.DirectParametric, st2.State);
        Equal("wingfoil-race", st2.PresetId);

        // Undo returns to SeededAcceptedId -> automatically relinks with zero shadow flag.
        session.Undo(Id());
        var st3 = session.RecipeStatus();
        Equal(RecipeLinkState.Linked, st3.State);
        Equal(seeded.AcceptedId, st3.SeededAcceptedId);
    }

    // T8 (SET-04, CAT-03): All 9 discipline presets seed Certified FoilDSL 4.1 with 0 errors; wakefoil maps to surf
    // with Flagged note; every pending/LINK preferred section names its GEN fallback + reason; unacknowledged Flagged
    // field refuses Seed with SETUP-FLAGGED-UNACK.
    // Mutant: omitting SETUP-FLAGGED-UNACK guard or leaving wakefoil unmapped turns this RED.
    private static void T8_AllNinePresetsAndFlaggedGate()
    {
        Equal(9, DisciplinePresets.All.Count);
        foreach (var preset in DisciplinePresets.All)
        {
            // Unacknowledged Flagged defaults MUST refuse with SETUP-FLAGGED-UNACK.
            using var gated = new AuthoringSession();
            Refuses("SETUP-FLAGGED-UNACK", () => gated.Seed(new SetupSeedRequest(preset.Id, AcknowledgeAllFlagged: false), Id()));

            // With explicit acknowledgement, seeding succeeds and certifies cleanly.
            using var session = new AuthoringSession();
            var outcome = session.Seed(new SetupSeedRequest(preset.Id, AcknowledgeAllFlagged: true), Id());
            var inspection = session.InspectAccepted();
            Equal(GeometryStatus.Certified, inspection.Geometry.Status);
            Equal("gen:" + preset.GenFallbackProfileId, outcome.Brief.Recipe.ProfileOrigin);
            if (preset.Id == "efoil")
            {
                Equal<string?>(null, outcome.Brief.Recipe.FallbackReason);
            }
            else
            {
                Equal(true, !string.IsNullOrWhiteSpace(outcome.Brief.Recipe.FallbackReason));
                Equal(preset.PreferredProfileId, outcome.Brief.Recipe.PreferredProfileId);
            }
        }

        // wakefoil -> surf mapping with mandatory Flagged wake-speed note.
        var wake = DisciplinePresets.ResolvePurpose("wakefoil");
        Equal("surf", wake.Preset.Id);
        Equal(true, wake.PurposeMappingFlag is not null &&
            wake.PurposeMappingFlag.Source.Contains("Wakefoil uses the Surf preset", StringComparison.Ordinal));
        using var wakeSession = new AuthoringSession();
        // Acknowledging only surf flags without the wakefoil flag MUST still refuse SETUP-FLAGGED-UNACK.
        var surfOnlyAcks = wake.Preset.DefaultFlaggedAssumptions
            .Select(f => new FlaggedAcknowledgement(f.FieldPath, f.Label, f.Source)).ToArray();
        Refuses("SETUP-FLAGGED-UNACK", () => wakeSession.Seed(new SetupSeedRequest("wakefoil", FlaggedAcknowledgements: surfOnlyAcks), Id()));
        // Acknowledging all (including wakefoil flag) succeeds.
        var wakeOut = wakeSession.Seed(new SetupSeedRequest("wakefoil", AcknowledgeAllFlagged: true), Id());
        Equal("surf", wakeOut.Brief.DisciplinePresetId);
        Equal("wakefoil", wakeOut.Brief.Purpose);
        Equal(true, wakeOut.Brief.FlaggedAcknowledgements.Any(a => a.FieldPath == "brief.purpose.wakefoil"));
    }

    // T10 (GOAL-01, GOAL-03): Editing a constraint bound appends goalStates[1] with editedByUser = true, leaves
    // accepted.Count and SurfaceHash unchanged, updates live CurrentGoalSweepSnapshot(), and earlier (goalStateId,
    // contentHash) still resolves after save/reopen.
    // Mutant: mutating goalStates[0] in place breaks old hash lookup after reopen.
    private static void T10_GoalStateAppendNeverEditsGeometry()
    {
        using var session = new AuthoringSession();
        var seeded = session.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Id());
        var g0 = seeded.GoalState;
        string surfaceBefore = session.Snapshot().SurfaceHash;
        int acceptedBefore = session.Envelope().Accepted.Length;

        // Live Area 4 sweep seam now returns active GoalStateSweepSnapshot!
        var snap0 = session.CurrentGoalSweepSnapshot();
        Equal(true, snap0 is not null);
        Equal(g0.GoalStateId, snap0!.GoalStateId);
        Equal(g0.ContentHash, snap0.ContentHash);
        Equal(7, snap0.OperatingPoints.Count);

        // Edit cavitation margin constraint bound 1.15 -> 1.25 with editedByUser = true.
        var editedConstraints = g0.Constraints
            .Select(c => c.Id == "c-cav" ? c with { Bound = 1.25, EditedByUser = true } : c)
            .ToArray();
        var g1 = session.UpdateGoalState(g0 with { Constraints = editedConstraints });

        Equal(acceptedBefore, session.Envelope().Accepted.Length);
        Equal(surfaceBefore, session.Snapshot().SurfaceHash);
        Equal(g0.GoalStateId, g1.Parent);
        Equal(true, g1.ContentHash != g0.ContentHash);
        Equal(true, g1.Constraints.Single(c => c.Id == "c-cav").EditedByUser);

        // Live sweep seam reflects g1.
        var snap1 = session.CurrentGoalSweepSnapshot();
        Equal(g1.GoalStateId, snap1!.GoalStateId);
        Equal(g1.ContentHash, snap1.ContentHash);

        // Save/reopen preserves BOTH g0 and g1 so any sweep pinned to g0 still resolves!
        byte[] saved = session.SaveImage();
        using var reopened = new AuthoringSession();
        reopened.Reopen(saved);
        var setup = reopened.ReadSetup()!;
        Equal(2, setup.GoalStates.Count);
        Equal(g0.GoalStateId, setup.GoalStates[0].GoalStateId);
        Equal(g0.ContentHash, setup.GoalStates[0].ContentHash);
        Equal(g1.GoalStateId, setup.GoalStates[1].GoalStateId);
        Equal(g1.ContentHash, setup.GoalStates[1].ContentHash);
    }

    // T12 (ADR-0011, DOC-02, Ruling 272 DR-SET-1):
    // 1. Project with no runs, no experiments, no setup stays cfdw-project-1 byte-identical (no "setup" member).
    // 2. Project with setup serializes as cfdw-project-2 and round-trips byte-equal.
    // 3. Tampered GoalStateVersion.ContentHash or SetupBriefVersion.ContentHash refused with DOC-INTEGRITY.
    // 4. Unknown top-level field refused with DOC-UNSUPPORTED-FIELD (old-build fail-closed behavior).
    // Mutant: writing "setup": null on unseeded project breaks cfdw-project-1 byte-identity.
    private static void T12_NativeProjectRoundTripAndFailClosed()
    {
        // 1. Unseeded project stays cfdw-project-1 without "setup".
        using var plain = new AuthoringSession();
        plain.Open(FoilSource.NewDefault(), Id(), true);
        byte[] v1 = plain.SaveImage();
        Equal("cfdw-project-1", NativeProject.FormatOf(v1));
        Equal(false, Encoding.UTF8.GetString(v1).Contains("\"setup\"", StringComparison.Ordinal));
        Equal<GoalStateSweepSnapshot?>(null, plain.CurrentGoalSweepSnapshot());

        // 2. Seeded project writes cfdw-project-2, round-trips byte-identically, including a DrcExclusion.
        using var seeded = new AuthoringSession();
        var outcome = seeded.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Id());
        string designId = seeded.Envelope().Accepted.Single(a => a.Id == outcome.AcceptedId).DesignId;
        seeded.RecordDrcExclusion(designId, "class.gwa.front_area", "2022-02-01", "planform.area", "Test rig only", "tester");
        byte[] v2 = seeded.SaveImage();
        Equal("cfdw-project-2", NativeProject.FormatOf(v2));
        string v2Text = Encoding.UTF8.GetString(v2);
        Equal(true, v2Text.Contains("\"setup\"", StringComparison.Ordinal));
        Equal(false, v2Text.Contains("\"analysis\"", StringComparison.Ordinal));

        using var reopened = new AuthoringSession();
        reopened.Reopen(v2);
        Equal(true, reopened.SaveImage().AsSpan().SequenceEqual(v2));
        Equal(1, reopened.ReadSetup()!.DrcExclusions.Count);

        // 3. Tampered GoalStateVersion.ContentHash -> DOC-INTEGRITY.
        string goodHash = outcome.GoalState.ContentHash;
        string badHash = (goodHash[0] == 'a' ? "b" : "a") + goodHash[1..];
        string tamperedGoal = v2Text.Replace(goodHash, badHash, StringComparison.Ordinal);
        Refuses("DOC-INTEGRITY", () => new AuthoringSession().Reopen(Encoding.UTF8.GetBytes(tamperedGoal)));

        // Tampered semantic field inside GoalStateVersion -> DOC-INTEGRITY.
        string tamperedLoad = v2Text.Replace("\"frontWingLoadN\": 1050", "\"frontWingLoadN\": 1051", StringComparison.Ordinal);
        Refuses("DOC-INTEGRITY", () => new AuthoringSession().Reopen(Encoding.UTF8.GetBytes(tamperedLoad)));

        // Format mismatch: cfdw-project-1 carrying setup -> DOC-SCHEMA.
        string badFmt = v2Text.Replace("\"format\": \"cfdw-project-2\"", "\"format\": \"cfdw-project-1\"", StringComparison.Ordinal);
        Refuses("DOC-SCHEMA", () => new AuthoringSession().Reopen(Encoding.UTF8.GetBytes(badFmt)));

        // 4. Unknown top-level field -> DOC-UNSUPPORTED-FIELD (matching pre-SET reader behavior on "setup").
        string withUnknown = v2Text.Insert(v2Text.IndexOf('\n') + 1, "  \"futureArea\": {},\n");
        Refuses("DOC-UNSUPPORTED-FIELD", () => new AuthoringSession().Reopen(Encoding.UTF8.GetBytes(withUnknown)));
    }

    // ---- Helpers ----

    private static string Id() => Guid.NewGuid().ToString("D");

    private static SoftTargetSet MakeTargets(
        double? bMax, double wb,
        double? cMax, double wc,
        double? area, double wS,
        double? ar, double wAr,
        double taper,
        string provenance = "Stated",
        string source = "Test fixture") =>
        new(
            bMax is { } b ? new SoftTargetScalar(b, wb, provenance, source) : null,
            cMax is { } c ? new SoftTargetScalar(c, wc, provenance, source) : null,
            area is { } s ? new SoftTargetScalar(s, wS, provenance, source) : null,
            ar is { } a ? new SoftTargetScalar(a, wAr, provenance, source) : null,
            new SoftTargetTaper(taper, provenance, source),
            "custom");

    private static double RawJ1(
        double b, double c, double lam,
        double bMax, double wb, double cMax, double wc, double area, double wS, double ar, double wAr)
    {
        double k = RecipeSeeder.Kappa(lam);
        double s = b * c * k;
        double actualAr = b / (c * k);
        double db = Math.Max(0.0, (b - bMax) / bMax);
        double dc = Math.Max(0.0, (c - cMax) / cMax);
        double dS = Math.Abs(s - area) / area;
        double dAr = Math.Abs(actualAr - ar) / ar;
        return wb * db + wc * dc + wS * dS + wAr * dAr;
    }

    private static void Near(double expected, double actual, double absTol, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > absTol)
            throw new InvalidOperationException($"{label}: expected {expected:G17}, actual {actual:G17} (tol {absTol:G4})");
    }

    private static void NearRel(double expected, double actual, double relTol, string label)
    {
        double tol = relTol * Math.Max(1.0, Math.Abs(expected));
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tol)
            throw new InvalidOperationException($"{label}: expected {expected:G17}, actual {actual:G17} (relTol {relTol:G4})");
    }

    private static AnalysisRun SampleRunOnCurrent(AuthoringSession session)
    {
        var snap = session.Snapshot();
        var est = WingEstimates.From(snap.Source, "accepted", 0);
        var water = DisciplinePresets.DefaultSaltWater15C;
        var op = new OperatingPoint(7.0, 101325.0, 0.5, "root LE", 2.0, null);
        var method = new RunMethod("cfdw.vlm-strip", "1", 1);
        var settings = new RunSettings(16, 4, "cosine", "cosine", 20, "freestream", 1e-6, "A5.3", null, [2, 4], "clean", 0.5, [0.5], [0.5]);
        string sHash = RunRecord.SettingsHash(settings);
        var parsed = FoilSource.Parse(snap.Source).Definition!;
        string[] profileHashes = parsed.Profiles.Select(p => Identity.Blake3(Encoding.UTF8.GetBytes(p.Name))).ToArray();
        var inputs = new RunInputs(snap.AcceptedId, snap.SurfaceHash, profileHashes, Evaluator.IdFor(snap.Source), "cfdw-place/1");
        string runKey = RunRecord.Key(inputs, water, op, method, sHash);
        var reference = new RunReference(est.AreaSquareMeters, est.SpanMeters, est.MeanChordMeters, "root LE", "body; wind for lift and drag");
        var strip = new StripLoad(0, 0.1, 0.2, est.MeanChordMeters, 0.5, 0.01, 2.0, 4e5, 0.4,
            new StripValue(null, "Unavailable"), new StripValue(null, "Unavailable"),
            0, 0, 100, 0, 0, 0, 0.01);
        var draft = new AnalysisRun(Id(), runKey, "", new RunOutcome.Completed(), "vlm-strip", method, settings, sHash,
            inputs, water, op, reference, 1e-4, [strip], 10.0, new RunPlatform("test", "arm64", "10.0"),
            new RunDiagnostics(1e-12, 10.0));
        return draft with { ContentHash = RunRecord.ContentHash(draft) };
    }
}
