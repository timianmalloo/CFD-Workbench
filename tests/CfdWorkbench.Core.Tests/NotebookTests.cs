using System.Globalization;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.Notebook;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure zero-I/O Core tests for Wave 13B AI-Guided Hydrofoil Design Notebook (T-NB-01..T-NB-05,
/// <c>docs/design/ai-guided-design-notebook.md §9</c>). Strictly STORE-SUBSET clean.
/// </summary>
internal static class NotebookTests
{
    private const string WorkedF4OrcaGrill0Dsl = """
foilreq "1.0" {
  brief {
    discipline: wingfoil-surf;
    skill: advanced;
  }
  rider {
    mass: 90 kg @stated;
  }
  water {
    kind: salt @defaulted;
    temp: 15 C @defaulted;
    depth_ref: 0.45 m @defaulted;
  }
  envelope {
    max_span: <= 950 cm hard @flagged("950 cm exceeds hydrofoil wing span bounds");
    target_ar: ~ 12.0 soft(0.75) @inferred("F4 Orca conceptual aspect ratio");
  }
  reference "F4 Orca" {
    traits: [high-aspect-glide, high-camber-low-end, responsive-carve];
    policy: "conceptual-only-no-proprietary-geometry";
    confidence: Flagged;
  }
  objectives {
    takeoff_speed: <= 10.0 kn high @stated;
    glide: high @stated;
    drop_speed: ? @unknown;
  }
  maneuvers {
    steep_drop: "controlled drops on steep waves" high @stated;
    bottom_turn: "keep speed through bottom turn" high @stated;
    cutback: "powered cutbacks using foil speed or wing power" high @stated;
  }
  open_questions {
    q1_unit_sanity: "Confirm max_span = 950 mm instead of 950 cm" @open;
    q4_maneuver_feel: "Choose roll/pitch balance for cutbacks" @open;
  }
}
""";

    private const string DepthUnsetDsl = """
foilreq "1.0" {
  brief {
    discipline: downwind;
    skill: intermediate;
  }
  rider {
    mass: 80 kg @stated;
    board_mass: 6.0 kg @stated;
    foil_mass: 4.0 kg @stated;
  }
  water {
    kind: fresh @stated;
    temp: 20 C @stated;
    depth_ref: ? @unknown;
  }
  envelope {
    max_span: <= 1150 mm hard @stated;
    target_ar: ~ 11.0 soft(0.8) @stated;
  }
}
""";

    internal static void Run()
    {
        Check("T_NB_01_FoilReqParser_GoldenRoundTrip_DepthUnset_AndInvalidSyntax", T_NB_01_ParserRoundTrip);
        Check("T_NB_02_FoilReqValidator_UnitSanity_PhysicsTension_AndCoordFirewall", T_NB_02_Validator);
        Check("T_NB_03_GrillTreeEngine_SerialSingleActive_ReopenStale_SkipAndGenerateNow", T_NB_03_GrillTree);
        Check("T_NB_04_CandidateIntentValidator_TwoProfileEmit_ScorecardAndNumeralContext", T_NB_04_CandidateAndScorecard);
        Check("T_NB_05_NotebookEnvelope_Chunking20KB_RebuildEquality_TamperAndCadHandoff", T_NB_05_EnvelopeAndHandoff);
    }

    private static void T_NB_01_ParserRoundTrip()
    {
        var parsed = FoilReqParser.Parse(WorkedF4OrcaGrill0Dsl);
        Equal(true, parsed.IsValid);
        var doc = parsed.Document!;
        Equal("1.0", doc.Version);
        Equal("wingfoil-surf", doc.Brief.Discipline);
        Equal("advanced", doc.Brief.Skill);
        Equal(90.0, doc.Rider.Find("mass")!.Quantity.Value);
        Equal("kg", doc.Rider.Find("mass")!.Quantity.Unit);
        Equal(false, doc.Water.IsDepthUnset);
        Equal(0.45, doc.Water.DepthRef!.Value);

        // Verify optional rationale on @flagged("...") round-trips losslessly (killable mutant target).
        var maxSpan = doc.FindConstraint("max_span")!;
        Equal("@flagged", maxSpan.Tag.Kind);
        Equal("950 cm exceeds hydrofoil wing span bounds", maxSpan.Tag.Rationale);
        Equal("F4 Orca", doc.References[0].Name);
        Equal(3, doc.References[0].Traits.Count);
        Equal(FoilReqDocument.ConceptualReferencePolicy, doc.References[0].Policy);

        string formatted = FoilReqSerializer.Format(doc);
        var reparsed = FoilReqParser.Parse(formatted);
        Equal(true, reparsed.IsValid);
        Equal(formatted, FoilReqSerializer.Format(reparsed.Document!));
        Equal(FoilReqSerializer.ComputeContentHash(doc), FoilReqSerializer.ComputeContentHash(reparsed.Document!));
        Equal("950 cm exceeds hydrofoil wing span bounds", reparsed.Document!.FindConstraint("max_span")!.Tag.Rationale);

        // Depth-unset document (explicit '? @unknown') round-trips cleanly.
        var depthParsed = FoilReqParser.Parse(DepthUnsetDsl);
        Equal(true, depthParsed.IsValid);
        Equal(true, depthParsed.Document!.Water.IsDepthUnset);
        Equal(true, depthParsed.Document!.Water.DepthExplicitUnknown);
        string depthFmt = FoilReqSerializer.Format(depthParsed.Document!);
        var depthReparsed = FoilReqParser.Parse(depthFmt);
        Equal(true, depthReparsed.IsValid);
        Equal(true, depthReparsed.Document!.Water.IsDepthUnset);

        // Invalid syntax / unsupported version / bad reference policy rejected with NB-FOILREQ-SYNTAX.
        var badVersion = FoilReqParser.Parse("foilreq \"2.0\" { brief { discipline: surf; skill: advanced; } }");
        Equal(false, badVersion.IsValid);
        Equal(FoilReqParser.SyntaxErrorCode, badVersion.ErrorCode);

        var badPolicy = FoilReqParser.Parse("""
            foilreq "1.0" {
              brief { discipline: wingfoil-surf; skill: advanced; }
              rider { mass: 80 kg @stated; }
              water { kind: salt @stated; temp: 15 C @stated; }
              reference "Secret" { traits: [glide]; policy: "copy-geometry"; confidence: Flagged; }
            }
            """);
        Equal(false, badPolicy.IsValid);
        Equal(FoilReqParser.SyntaxErrorCode, badPolicy.ErrorCode);
    }

    private static void T_NB_02_Validator()
    {
        var doc = FoilReqParser.Parse(WorkedF4OrcaGrill0Dsl).Document!;
        var val = FoilReqValidator.Validate(doc);
        Equal(true, val.IsValid);
        Equal(false, val.HasBlockingError);
        Equal(true, val.KitMassDefaulted);
        Equal(DisciplinePresets.DefaultSaltWater15C.Rho, val.WaterDensityKgPerM3);

        // Must catch 950 cm dimensional sanity anomaly (TQ7 / COPY-926) suggesting 950 mm.
        var unitFinding = val.Findings.Single(f => f.Code == FoilReqValidator.UnitSanityCode);
        Equal("950 mm", unitFinding.SuggestedValue);
        Equal(true, unitFinding.Message.Contains("950 cm", StringComparison.Ordinal));
        Equal(true, unitFinding.Message.Contains("950 mm", StringComparison.Ordinal));

        // Must detect PHYSICS-TENSION: 90 kg + 15 kg defaulted kit, span 950 mm, AR 12 -> S ~ 752 cm2 -> CL_req > 0.95.
        var tension = val.Findings.Single(f => f.Code == FoilReqValidator.PhysicsTensionCode);
        Equal(true, val.ImpliedTakeoffCl is > 0.95);
        Equal(true, tension.ComputedValue is > 0.95);

        // Fresh water density matches DisciplinePresets.DefaultFreshWater15C at 15 C.
        Equal(DisciplinePresets.DefaultFreshWater15C.Rho, FoilReqValidator.ComputeWaterDensity("fresh", 15.0));

        // Coordinate firewall across maneuvers, open_questions, and custom grill text (COMMIT-02 / COMMIT-04).
        var coordDoc = FoilReqParser.Parse("""
            foilreq "1.0" {
              brief { discipline: wingfoil-surf; skill: advanced; }
              rider { mass: 85 kg @stated; }
              water { kind: salt @defaulted; temp: 15 C @defaulted; }
              maneuvers { bad: "0.0 0.0, 0.25 0.04, 0.50 0.06, 0.75 0.03, 1.0 0.0" high @stated; }
            }
            """).Document!;
        var coordVal = FoilReqValidator.Validate(coordDoc);
        Equal(false, coordVal.IsValid);
        Equal(true, coordVal.HasBlockingError);
        Equal(true, coordVal.Findings.Any(f => f.Code == FoilReqValidator.CoordForbiddenCode));

        var customCoordFinding = FoilReqValidator.ValidateCustomText("NACA 0012 coordinates: 0.0 0.0, 0.2 0.05, 0.5 0.06, 1.0 0.0");
        Equal(FoilReqValidator.CoordForbiddenCode, customCoordFinding?.Code);
    }

    private static void T_NB_03_GrillTree()
    {
        var doc0 = FoilReqParser.Parse(WorkedF4OrcaGrill0Dsl).Document!;
        var v0 = NotebookRecords.CreateRequirementVersion(doc0, "v1.0");
        var history = new List<GrillResolution>();

        // Strictly 1 active blocking question at a time: starts at Q1 unit sanity.
        var state1 = GrillTreeEngine.EvaluateState(doc0, FoilReqValidator.Validate(doc0), history);
        Equal(GrillTreeEngine.Q1UnitSanityId, state1.ActiveQuestion?.QuestionId);
        Equal(false, state1.ReadyToSynthesize);

        // Answer Q1 -> appends v1.1 and advances to Q2 physics tension.
        var doc1 = GrillTreeEngine.ResolveQuestion(doc0, GrillTreeEngine.Q1UnitSanityId, "opt-hard-fix");
        var v1 = NotebookRecords.CreateRequirementVersion(doc1, "v1.1", v0.ReqVersionId);
        history.Add(new GrillResolution(GrillTreeEngine.Q1UnitSanityId, "answer", "opt-hard-fix", null, v1.ReqVersionId, DateTimeOffset.UtcNow));
        Equal(950.0, doc1.FindConstraint("max_span")!.Quantity!.Value);
        Equal("mm", doc1.FindConstraint("max_span")!.Quantity!.Unit);

        var state2 = GrillTreeEngine.EvaluateState(doc1, FoilReqValidator.Validate(doc1), history);
        Equal(GrillTreeEngine.Q2PhysicsTensionId, state2.ActiveQuestion?.QuestionId);

        // Answer Q2 -> appends v1.2 and advances to Q3 speed/cavitation.
        var doc2 = GrillTreeEngine.ResolveQuestion(doc1, GrillTreeEngine.Q2PhysicsTensionId, "opt-balanced-ar");
        var v2 = NotebookRecords.CreateRequirementVersion(doc2, "v1.2", v1.ReqVersionId);
        history.Add(new GrillResolution(GrillTreeEngine.Q2PhysicsTensionId, "answer", "opt-balanced-ar", null, v2.ReqVersionId, DateTimeOffset.UtcNow));
        var state3 = GrillTreeEngine.EvaluateState(doc2, FoilReqValidator.Validate(doc2), history);
        Equal(GrillTreeEngine.Q3SpeedCavitationId, state3.ActiveQuestion?.QuestionId);

        // Skip Q3 + GenerateNow terminates grilling cleanly with open clauses visibly Flagged (@skipped).
        var doc3 = GrillTreeEngine.SkipQuestion(doc2, GrillTreeEngine.Q3SpeedCavitationId);
        var v3 = NotebookRecords.CreateRequirementVersion(doc3, "v1.3", v2.ReqVersionId);
        history.Add(new GrillResolution(GrillTreeEngine.Q3SpeedCavitationId, "skip", null, null, v3.ReqVersionId, DateTimeOffset.UtcNow));

        var docGen = GrillTreeEngine.GenerateNowDocument(doc3, FoilReqValidator.Validate(doc3), history);
        var vGen = NotebookRecords.CreateRequirementVersion(docGen, "v1.4", v3.ReqVersionId);
        history.Add(new GrillResolution("*", "generate_now", null, null, vGen.ReqVersionId, DateTimeOffset.UtcNow));
        var stateGen = GrillTreeEngine.EvaluateState(docGen, FoilReqValidator.Validate(docGen), history);
        Equal(true, stateGen.ReadyToSynthesize);
        Equal(true, stateGen.ActiveQuestion is null);
        Equal(true, docGen.OpenQuestions.Any(q => q.State == "@skipped"));

        // Candidate built against vGen is fresh; re-opening Q1 creates vReopen and marks candidate Stale (TQ9).
        var delta = MakeValidDelta();
        var cand = NotebookRecords.CreateCandidateRecord(1, vGen.ReqVersionId, delta, new string('a', 64), nameof(GeometryStatus.Certified), null, []);
        Equal(false, GrillTreeEngine.IsCandidateStale(cand, vGen.ReqVersionId));

        var docReopen = GrillTreeEngine.ReopenQuestion(docGen, GrillTreeEngine.Q1UnitSanityId);
        var vReopen = NotebookRecords.CreateRequirementVersion(docReopen, "v1.5", vGen.ReqVersionId);
        history.Add(new GrillResolution(GrillTreeEngine.Q1UnitSanityId, "reopen", null, null, vReopen.ReqVersionId, DateTimeOffset.UtcNow));
        Equal(true, GrillTreeEngine.IsCandidateStale(cand, vReopen.ReqVersionId));
        var stateReopen = GrillTreeEngine.EvaluateState(docReopen, FoilReqValidator.Validate(docReopen), history);
        Equal(GrillTreeEngine.Q1UnitSanityId, stateReopen.ActiveQuestion?.QuestionId);
    }

    private static void T_NB_04_CandidateAndScorecard()
    {
        // 1. Valid two-profile candidate-intent-delta using admitted GEN profiles.
        const string validJson = """
        {
          "kind": "candidate-intent-delta",
          "spanMeters": 0.93,
          "areaSquareMeters": 0.0845,
          "taper": 0.34,
          "rootThicknessRatio": 0.118,
          "anhedralDeg": -5.0,
          "washoutDeg": -1.2,
          "sweepQuarterChordDeg": 2.0,
          "rootProfileId": "cfdw-surf-118",
          "tipProfileId": "cfdw-surf-105",
          "provenance": [
            { "fieldName": "spanMeters", "tag": "Stated", "valueDisplay": "930 mm", "reason": "Within 950 mm cap" }
          ]
        }
        """;
        var validRes = CandidateIntentValidator.Validate(validJson);
        Equal(true, validRes.IsValid);
        var delta = validRes.Delta!;
        Equal(64, delta.DeltaHash.Length);

        // Two-profile RecipeSeeder.EmitFoilDsl materializes Certified FoilDSL 4.1 with distinct root & tip profiles.
        byte[] dslBytes = NotebookRecords.MaterializeCandidateDsl(delta, "Orca Inspired v1");
        string dslText = Encoding.UTF8.GetString(dslBytes);
        Equal(true, dslText.Contains("at root profile \"cfdw-surf-118\" at tip profile \"cfdw-surf-105\"", StringComparison.Ordinal));
        var parsed = FoilSource.Parse(dslBytes);
        Equal(true, parsed.IsParsed);
        Equal(GeometryStatus.Certified, Geometry.Assess(parsed).Status);

        // Wave 13A seam: optional FoilDSL 4.2 tipStyle="rounded" emits valid Certified 4.2 source.
        var roundedDelta = delta with { TipStyle = "rounded", TipCapRatio = 1.0 };
        byte[] roundedBytes = NotebookRecords.MaterializeCandidateDsl(roundedDelta, "Rounded v1");
        string roundedText = Encoding.UTF8.GetString(roundedBytes);
        Equal(true, roundedText.Contains("foildsl \"4.2\"", StringComparison.Ordinal));
        Equal(true, roundedText.Contains("tip rounded 1", StringComparison.Ordinal));
        var parsedRounded = FoilSource.Parse(roundedBytes);
        Equal(true, parsedRounded.IsParsed);
        Equal(GeometryStatus.Certified, Geometry.Assess(parsedRounded).Status);

        // 2. Reject raw coordinates, forces, and disabled VEND profile IDs (e817) without clamping.
        const string badCoordsJson = """{"kind":"candidate-intent-delta","spanMeters":0.93,"areaSquareMeters":0.0845,"taper":0.34,"rootThicknessRatio":0.118,"anhedralDeg":-5.0,"washoutDeg":-1.2,"rootProfileId":"cfdw-surf-118","tipProfileId":"cfdw-surf-105","coordinates":[[0,0],[1,0]],"provenance":[{"fieldName":"spanMeters","tag":"Stated","valueDisplay":"930 mm","reason":"Test"}]}""";
        var coordRef = CandidateIntentValidator.Validate(badCoordsJson);
        Equal(false, coordRef.IsValid);
        Equal("NB-COORD-FORBIDDEN", coordRef.Reason);

        const string badVendJson = """{"kind":"candidate-intent-delta","spanMeters":0.93,"areaSquareMeters":0.0845,"taper":0.34,"rootThicknessRatio":0.118,"anhedralDeg":-5.0,"washoutDeg":-1.2,"rootProfileId":"e817","tipProfileId":"cfdw-surf-105","provenance":[{"fieldName":"rootProfileId","tag":"Stated","valueDisplay":"e817","reason":"Disabled VEND"}]}""";
        var vendRef = CandidateIntentValidator.Validate(badVendJson);
        Equal(false, vendRef.IsValid);
        Equal("CAT-UNAVAILABLE", vendRef.Reason);

        // 3. Scorecard & ANA-19 depth-unset -> Unavailable — depth not set + canonical NumeralTokenRegex grounding.
        var depthUnsetDoc = FoilReqParser.Parse(DepthUnsetDsl).Document!;
        var opUnset = new CandidateOperatingPointSummary("steep_drop", 24.0, 1.2, 0.25, 0.012, 20.8, 19.9, 21.4, 9.8e5, null, null, null, null, 0.0, null, null, DepthUnset: true, "vlm-strip/1", null, 120.0);
        var candUnset = NotebookRecords.CreateCandidateRecord(1, Guid.NewGuid().ToString("D"), delta, parsed.SurfaceHash!, nameof(GeometryStatus.Certified), null, [opUnset]);
        var scUnset = RequirementScorecard.Evaluate(depthUnsetDoc, candUnset.ReqVersionId, candUnset);
        Equal("Unavailable", scUnset.Clauses.Single(c => c.ClauseId == "op.cavitation").Status);
        Equal(RequirementScorecard.DepthUnsetNotice, scUnset.Clauses.Single(c => c.ClauseId == "op.cavitation").ActualDisplay);
        Equal("Unavailable", scUnset.Clauses.Single(c => c.ClauseId == "op.free_surface").Status);

        // Depth-set scorecard + comparison + NumeralTokenRegex canonical solver_context.
        var depthSetDoc = GrillTreeEngine.ResolveQuestion(FoilReqParser.Parse(WorkedF4OrcaGrill0Dsl).Document!, GrillTreeEngine.Q1UnitSanityId, "opt-hard-fix");
        var op1To = new CandidateOperatingPointSummary("takeoff", 10.5, 5.2, 0.830, 0.036, 23.1, 22.0, 23.8, 4.31e5, -0.98, 1.332, 1.36, 28.0, 0.0, 3.86, 5.88, false, "vlm-strip/1", null, 110.0);
        var op1Gl = new CandidateOperatingPointSummary("glide", 15.0, 2.4, 0.407, 0.0165, 24.6, 23.4, 25.3, 6.15e5, -0.72, 2.150, 2.98, 28.0, 0.0, 3.86, 3.67, false, "vlm-strip/1", null, 110.0);
        var op1Dr = new CandidateOperatingPointSummary("steep_drop", 24.0, 0.8, 0.160, 0.009, 17.8, 17.0, 18.4, 9.85e5, -0.98, 1.332, 1.36, 28.0, 0.0, 3.86, 5.88, false, "vlm-strip/1", null, 110.0);
        var cand1 = NotebookRecords.CreateCandidateRecord(1, Guid.NewGuid().ToString("D"), delta, parsed.SurfaceHash!, nameof(GeometryStatus.Certified), null, [op1To, op1Gl, op1Dr]);

        var delta2 = delta with { SpanMeters = 0.915, AreaSquareMeters = 0.0815, AnhedralDeg = -6.0 };
        delta2 = delta2 with { DeltaHash = CandidateIntentDelta.ComputeDeltaHash(delta2) };
        var op2Gl = op1Gl with { WingOnlyLd = 26.4 };
        var cand2 = NotebookRecords.CreateCandidateRecord(2, cand1.ReqVersionId, delta2, parsed.SurfaceHash!, nameof(GeometryStatus.Certified), null, [op1To, op2Gl, op1Dr]);

        var sc2 = RequirementScorecard.Evaluate(depthSetDoc, cand2.ReqVersionId, cand2, baseline: cand1, latestReqVersionId: cand1.ReqVersionId);
        Equal(true, sc2.MandatoryNotices.Any(n => n.Contains("A5.1", StringComparison.Ordinal)));
        string solverContext = RequirementScorecard.FormatSolverContext(sc2, cand2, cand1);

        // Grounded translation passes FindUnsupportedNumeral with 0 false positives.
        string grounded = "At 15.0 kn glide, wing-only L/D is 26.4 (+7.3% vs v1) with h/c_root 3.86 < 5.0 (Fr_h 5.88) and V_crit 28.0 kn.";
        Equal((string?)null, AssistantValidators.FindUnsupportedNumeral(grounded, solverContext, new Dictionary<int, string>()));

        // Fabricated numeral (142) is caught immediately -> AI-03 withhold.
        string fabricated = "At 15.0 kn glide, wing-only L/D reaches 142.";
        Equal("142", AssistantValidators.FindUnsupportedNumeral(fabricated, solverContext, new Dictionary<int, string>()));
    }

    private static void T_NB_05_EnvelopeAndHandoff()
    {
        var doc = GrillTreeEngine.ResolveQuestion(FoilReqParser.Parse(WorkedF4OrcaGrill0Dsl).Document!, GrillTreeEngine.Q1UnitSanityId, "opt-hard-fix");
        var reqVer = NotebookRecords.CreateRequirementVersion(doc, "v1.0");

        // 1. 20 KB heavily-escaped transcript chunking -> EVERY JSON line <= 4,096 UTF-8 B.
        var sb20k = new StringBuilder(24_000);
        for (int i = 0; i < 400; i++)
            sb20k.Append($"Turn {i}: \"quote\" \\backslash\\ <angle> & café λ=0.34 — span <= 950 mm\n");
        string longTranscript = sb20k.ToString();
        Equal(true, Encoding.UTF8.GetByteCount(longTranscript) > 20_000);
        string[] chunks = NotebookRecords.ChunkText(longTranscript);
        Equal(longTranscript, NotebookRecords.JoinChunks(chunks));
        foreach (string ch in chunks)
            Equal(true, Encoding.UTF8.GetByteCount(ch) <= NotebookRecords.MaxRawChunkUtf8Bytes);

        // 2. Custom "My sections" snapshot determinism (SurfaceHash_Rebuilt == SurfaceHash_Recorded).
        var genRef = Catalog.Load().First(e => e.Id == "cfdw-surf-118");
        string customDat = FoilSource.Utf8.GetString(genRef.Coordinates!);
        var myCtx = new CandidateValidationContext(
            AdmittedMySectionIds: new HashSet<string>(StringComparer.Ordinal) { "my-custom-wave-118" },
            MySectionDatSnapshots: new Dictionary<string, string>(StringComparer.Ordinal) { ["my-custom-wave-118"] = customDat });
        string myDeltaJson = """{"kind":"candidate-intent-delta","spanMeters":0.93,"areaSquareMeters":0.0845,"taper":0.34,"rootThicknessRatio":0.118,"anhedralDeg":-5.0,"washoutDeg":-1.2,"rootProfileId":"my-custom-wave-118","tipProfileId":"cfdw-surf-105","provenance":[{"fieldName":"rootProfileId","tag":"Stated","valueDisplay":"my-custom-wave-118","reason":"My sections"}]}""";
        var myDeltaRes = CandidateIntentValidator.Validate(myDeltaJson, myCtx);
        Equal(true, myDeltaRes.IsValid);
        var myDelta = myDeltaRes.Delta!;
        Equal(true, myDelta.CustomRootProfileSnapshotLines is { Count: > 0 });

        byte[] myDsl = NotebookRecords.MaterializeCandidateDsl(myDelta);
        string mySurfaceHash = FoilSource.Parse(myDsl).SurfaceHash!;
        var cand1 = NotebookRecords.CreateCandidateRecord(1, reqVer.ReqVersionId, myDelta, mySurfaceHash, nameof(GeometryStatus.Certified), null, []);
        var refusedCand = NotebookRecords.CreateCandidateRecord(2, reqVer.ReqVersionId, myDelta, "", "Refused", TipChord.RefusalCode, []);

        var nbDraft = NotebookRecords.FinalizeSession(new NotebookSessionRecord(
            SessionId: Guid.NewGuid().ToString("D"),
            Title: "Wingfoil surf 90 kg",
            CreatedUtc: DateTimeOffset.UtcNow,
            Requirements: [reqVer],
            Turns: [new NotebookTurnRecord(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow, "user-prompt", reqVer.ReqVersionId, null, chunks)],
            Candidates: [cand1, refusedCand],
            Handoffs: [],
            ContentHash: ""));

        // 3. Pre-handoff lifecycle (current == null, launched from Start) -> RecordNotebookSession lives in memory,
        //    HandoffNotebookCandidate seeds r1 with Edit == null & cursor "open", and refuses non-Certified candidate.
        byte[] savedImage;
        using (var startSession = new AuthoringSession())
        {
            startSession.RecordNotebookSession(nbDraft);
            Equal(nbDraft.SessionId, startSession.ReadNotebookSession()!.SessionId);

            Refuses("NB-CANDIDATE-NOT-CERTIFIED", () =>
                startSession.HandoffNotebookCandidate(refusedCand.CandidateId, Guid.NewGuid().ToString("D")));

            var handoff1 = startSession.HandoffNotebookCandidate(cand1.CandidateId, Guid.NewGuid().ToString("D"));
            Equal(false, handoff1.SeedOutcome.IsReseed);
            Equal(1, handoff1.SeedOutcome.RevisionOrdinal);
            Equal(mySurfaceHash, handoff1.Handoff.SurfaceHash);
            var env1 = startSession.Envelope();
            Equal("cfdw-project-2", env1.Format);
            Equal((EditReceipt?)null, env1.Accepted[0].Edit);
            Equal("open", env1.Cursors[0].Reason);
            Equal(1, env1.Notebook!.Handoffs.Count);

            // Second handoff on open project (current != null): Edit.Rail == "seed", cursor "apply", 1-step Undo works!
            var handoff2 = startSession.HandoffNotebookCandidate(cand1.CandidateId, Guid.NewGuid().ToString("D"));
            Equal(true, handoff2.SeedOutcome.IsReseed);
            Equal(2, handoff2.SeedOutcome.RevisionOrdinal);
            var env2 = startSession.Envelope();
            Equal("seed", env2.Accepted[1].Edit!.Rail);
            Equal("apply", env2.Cursors[1].Reason);

            // Encode & prove EVERY serialized JSON line <= 4,096 UTF-8 B despite 20 KB heavily-escaped transcript!
            savedImage = NativeProject.Encode(env2);
            string encodedText = FoilSource.Utf8.GetString(savedImage);
            foreach (string line in encodedText.Split('\n'))
                Equal(true, FoilSource.Utf8.GetByteCount(line.TrimEnd('\r')) <= 4096);

            // Undo reverts cleanly to r1.
            startSession.Undo(Guid.NewGuid().ToString("D"));
            Equal(env2.Accepted[0].Id, startSession.Snapshot().AcceptedId);
        }

        // 4. Reopen round-trip verifies rebuild equality (GEN + My sections snapshot) and handoffs.
        using (var reopened = new AuthoringSession())
        {
            reopened.Reopen(savedImage);
            var nbLoaded = reopened.ReadNotebookSession()!;
            Equal(nbDraft.SessionId, nbLoaded.SessionId);
            Equal(longTranscript, NotebookRecords.JoinChunks(nbLoaded.Turns[0].BodyLines));
            Equal(2, nbLoaded.Handoffs.Count);
        }

        // 5. Tampered CadHandoffRecord -> DOC-REFERENCE; tampered candidate SurfaceHash -> DOC-INTEGRITY.
        var envOrig = NativeProject.Read(savedImage);
        var badHandoff = envOrig.Notebook!.Handoffs[0] with { SeededAcceptedId = Guid.NewGuid().ToString("D") };
        var badNbHandoff = NotebookRecords.FinalizeSession(envOrig.Notebook with { Handoffs = [badHandoff] });
        byte[] badHandoffBytes = NativeProject.Encode(envOrig with { Notebook = badNbHandoff });
        Refuses("DOC-REFERENCE", () => NativeProject.Read(badHandoffBytes));

        var badCand = envOrig.Notebook.Candidates[0] with { SurfaceHash = new string('f', 64) };
        badCand = badCand with { ContentHash = NotebookRecords.CandidateContentHash(badCand) };
        var badNbSurface = NotebookRecords.FinalizeSession(envOrig.Notebook with { Candidates = [badCand], Handoffs = [] });
        byte[] badSurfaceBytes = NativeProject.Encode(envOrig with { Notebook = badNbSurface });
        Refuses("DOC-INTEGRITY", () => NativeProject.Read(badSurfaceBytes));
    }

    private static CandidateIntentDelta MakeValidDelta()
    {
        const string json = """{"kind":"candidate-intent-delta","spanMeters":0.93,"areaSquareMeters":0.0845,"taper":0.34,"rootThicknessRatio":0.118,"anhedralDeg":-5.0,"washoutDeg":-1.2,"rootProfileId":"cfdw-surf-118","tipProfileId":"cfdw-surf-105","provenance":[{"fieldName":"spanMeters","tag":"Stated","valueDisplay":"930 mm","reason":"Test"}]}""";
        return CandidateIntentValidator.Validate(json).Delta!;
    }
}
