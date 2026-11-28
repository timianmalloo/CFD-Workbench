using System.Globalization;

namespace CfdWorkbench.Core.Notebook;

public sealed record GrillOption(
    string OptionId,
    string Label,
    string ImpactPreview,
    bool IsSuggestedDefault);

public sealed record GrillQuestion(
    string QuestionId,
    int Ordinal,
    int TotalEstimated,
    string Category,
    string Prompt,
    IReadOnlyList<string> NeededFor,
    IReadOnlyList<GrillOption> Options,
    bool AllowCustomAnswer = true);

/// <summary>Append-only record of one grill interaction (answer, skip, reopen, generate_now).</summary>
public sealed record GrillResolution(
    string QuestionId,
    string Action,
    string? SelectedOptionId,
    string? CustomAnswer,
    string ResultingReqVersionId,
    DateTimeOffset TimestampUtc);

public sealed record GrillTreeState(
    GrillQuestion? ActiveQuestion,
    int OpenCount,
    int ResolvedCount,
    int SkippedCount,
    bool ReadyToSynthesize,
    IReadOnlyList<string> OpenQuestionIds);

/// <summary>
/// Matt Pocock-style serial decision-tree disambiguation engine (<c>docs/design/ai-guided-design-notebook.md §3.2</c>).
/// Enforces at most 1 active blocking question at a time (Q1 unit sanity -&gt; Q2 physics tension -&gt; Q3 speed/cavitation -&gt; Q4 maneuver feel),
/// supports Answer, Skip (Flagged), Reopen (deriving <c>IsStale = true</c> on older candidates per TQ9), and GenerateNow.
/// Pure zero-I/O Core with <c>ArchitectureTests</c>-safe method names.
/// </summary>
public static class GrillTreeEngine
{
    public const string Q1UnitSanityId = "q1_unit_sanity";
    public const string Q2PhysicsTensionId = "q2_physics_tension";
    public const string Q3SpeedCavitationId = "q3_speed_cavitation";
    public const string Q4ManeuverFeelId = "q4_maneuver_feel";

    public static GrillQuestion? NextQuestion(
        FoilReqDocument doc,
        IReadOnlyList<GrillResolution> history) =>
        EvaluateState(doc, FoilReqValidator.Validate(doc), history).ActiveQuestion;

    public static GrillTreeState EvaluateState(
        FoilReqDocument doc,
        FoilReqValidationResult validation,
        IReadOnlyList<GrillResolution> history)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(validation);
        history ??= [];

        // Latest status per questionId from append-only history (reopen clears prior resolved/skipped status).
        var latestActionByQ = new Dictionary<string, string>(StringComparer.Ordinal);
        bool generatedNow = false;
        foreach (var h in history)
        {
            if (h.Action == "generate_now")
            {
                generatedNow = true;
                continue;
            }
            if (h.Action == "reopen")
            {
                generatedNow = false;
                latestActionByQ[h.QuestionId] = "open";
            }
            else if (h.Action is "answer" or "skip")
            {
                latestActionByQ[h.QuestionId] = h.Action;
            }
        }

        bool IsClosed(string qId) =>
            latestActionByQ.TryGetValue(qId, out string? act) && act is "answer" or "skip";

        var openQueue = new List<GrillQuestion>();

        // Q1: Dimensional sanity (TQ7)
        var unitFinding = validation.Findings.FirstOrDefault(f => f.Code == FoilReqValidator.UnitSanityCode);
        if ((unitFinding is not null || (latestActionByQ.TryGetValue(Q1UnitSanityId, out var q1s) && q1s == "open")) && !IsClosed(Q1UnitSanityId))
        {
            string suggested = unitFinding?.SuggestedValue ?? "950 mm";
            openQueue.Add(new GrillQuestion(
                QuestionId: Q1UnitSanityId,
                Ordinal: 1,
                TotalEstimated: 4,
                Category: "Unit & Span Box",
                Prompt: unitFinding?.Message ?? $"Confirm maximum span ({suggested}) and whether it is a hard cap.",
                NeededFor: ["envelope.max_span", "Planform sizing"],
                Options:
                [
                    new("opt-hard-fix", $"Use {suggested} as a hard span cap", $"max_span: <= {suggested} hard @stated", IsSuggestedDefault: true),
                    new("opt-soft-fix", $"Use {suggested} as a soft(0.8) target", $"max_span: <= {suggested} soft(0.8) @stated", IsSuggestedDefault: false)
                ]));
        }

        // Q2: Physics tension (CL_req > 0.95)
        var tensionFinding = validation.Findings.FirstOrDefault(f => f.Code == FoilReqValidator.PhysicsTensionCode);
        if ((tensionFinding is not null || (latestActionByQ.TryGetValue(Q2PhysicsTensionId, out var q2s) && q2s == "open")) && !IsClosed(Q2PhysicsTensionId))
        {
            openQueue.Add(new GrillQuestion(
                QuestionId: Q2PhysicsTensionId,
                Ordinal: 2,
                TotalEstimated: 4,
                Category: "Physics Tradeoff",
                Prompt: tensionFinding?.Message ?? "Resolve the span/aspect-ratio vs low-speed takeoff lift tradeoff.",
                NeededFor: ["envelope.target_ar", "envelope.target_area", "objectives.takeoff_speed"],
                Options:
                [
                    new("opt-balanced-ar", "Relax to AR ~10.0–10.5 (810–860 cm2) for ~10.5 kn takeoff + high glide", "target_ar: ~ 10.2; target_area: ~ 845 cm2 @stated", IsSuggestedDefault: true),
                    new("opt-high-ar", "Keep strict AR ~11.5–12.0 (720–760 cm2) with ~12.0 kn takeoff", "target_ar: ~ 11.8; takeoff_speed: <= 12.0 kn @stated", IsSuggestedDefault: false),
                    new("opt-early-takeoff", "Prioritize earliest takeoff at AR ~8.8–9.4 (900–960 cm2)", "target_ar: ~ 9.1; target_area: ~ 930 cm2 @stated", IsSuggestedDefault: false)
                ]));
        }

        // Q3: Steep-drop speed & cavitation floor (depth_ref unset or top_speed unknown)
        bool needsSpeedDepth = doc.Water.IsDepthUnset ||
                               doc.FindObjective("drop_speed") is { IsUnknown: true } ||
                               (doc.FindObjective("drop_speed") is null && doc.FindObjective("top_speed") is null);
        if ((needsSpeedDepth || (latestActionByQ.TryGetValue(Q3SpeedCavitationId, out var q3s) && q3s == "open")) && !IsClosed(Q3SpeedCavitationId))
        {
            openQueue.Add(new GrillQuestion(
                QuestionId: Q3SpeedCavitationId,
                Ordinal: 3,
                TotalEstimated: 4,
                Category: "Speed & Cavitation Floor",
                Prompt: "Set target steep-drop speed and reference submergence depth (needed for A5.4 cavitation screening; leaving depth unset keeps cavitation Unavailable per ANA-19).",
                NeededFor: ["water.depth_ref", "objectives.drop_speed", "A5.4 cavitation screen"],
                Options:
                [
                    new("opt-24kn-045m", "24 kn steep drop at 0.45 m submergence (sigma >= 1.15 * -Cp_min)", "water.depth_ref: 0.45 m @stated; drop_speed: >= 24.0 kn @stated", IsSuggestedDefault: true),
                    new("opt-20kn-040m", "20 kn wave drop at 0.40 m submergence", "water.depth_ref: 0.40 m @stated; drop_speed: >= 20.0 kn @stated", IsSuggestedDefault: false),
                    new("opt-depth-unset", "Leave depth unset (cavitation stays Unavailable per ANA-19)", "water.depth_ref: ? @unknown", IsSuggestedDefault: false)
                ]));
        }

        // Q4: Maneuver & cutback feel (open question or missing explicit feel choice)
        bool hasOpenManeuverQ = doc.OpenQuestions.Any(q => q.State == "@open" && q.Id is not (Q1UnitSanityId or Q2PhysicsTensionId or Q3SpeedCavitationId));
        if ((hasOpenManeuverQ || (latestActionByQ.TryGetValue(Q4ManeuverFeelId, out var q4s) && q4s == "open")) && !IsClosed(Q4ManeuverFeelId))
        {
            openQueue.Add(new GrillQuestion(
                QuestionId: Q4ManeuverFeelId,
                Ordinal: 4,
                TotalEstimated: 4,
                Category: "Turn & Cutback Feel",
                Prompt: "Choose roll/pitch balance for bottom turns and powered cutbacks.",
                NeededFor: ["Anhedral angle", "Tip taper & washout"],
                Options:
                [
                    new("opt-snappy-carve", "Snappy powered cutbacks (-5.0 to -6.0 deg anhedral, progressive 0.34 taper)", "maneuvers.cutback: snappy roll @stated", IsSuggestedDefault: true),
                    new("opt-locked-rail", "Locked-in rail drive (-3.0 deg anhedral, 0.40 taper)", "maneuvers.cutback: locked rail @stated", IsSuggestedDefault: false)
                ]));
        }

        // Any additional custom @open questions from the FoilReq document itself.
        foreach (var oq in doc.OpenQuestions)
        {
            if (oq.State != "@open") continue;
            if (IsClosed(oq.Id)) continue;
            if (openQueue.Any(q => q.QuestionId == oq.Id)) continue;
            openQueue.Add(new GrillQuestion(
                QuestionId: oq.Id,
                Ordinal: openQueue.Count + 1,
                TotalEstimated: Math.Max(4, openQueue.Count + 1),
                Category: "Clarification",
                Prompt: oq.Prompt,
                NeededFor: [$"open_questions.{oq.Id}"],
                Options:
                [
                    new("opt-accept-default", "Accept default assumption", $"{oq.Id}: resolved @stated", IsSuggestedDefault: true),
                    new("opt-skip-flagged", "Skip & keep assumption (Flagged)", $"{oq.Id}: @skipped", IsSuggestedDefault: false)
                ]));
        }

        // Re-index ordinals cleanly for display (1..N) while enforcing strictly at most 1 active blocking question.
        int totalOpen = openQueue.Count;
        int resolvedCount = latestActionByQ.Values.Count(v => v == "answer");
        int skippedCount = latestActionByQ.Values.Count(v => v == "skip");
        int totalEstimated = Math.Max(1, totalOpen + resolvedCount + skippedCount);

        GrillQuestion? active = (!generatedNow && openQueue.Count > 0)
            ? openQueue[0] with { Ordinal = resolvedCount + skippedCount + 1, TotalEstimated = totalEstimated }
            : null;

        bool ready = !validation.HasBlockingError && (generatedNow || openQueue.Count == 0);

        return new GrillTreeState(
            ActiveQuestion: active,
            OpenCount: generatedNow ? 0 : totalOpen,
            ResolvedCount: resolvedCount,
            SkippedCount: skippedCount,
            ReadyToSynthesize: ready,
            OpenQuestionIds: openQueue.Select(q => q.QuestionId).ToArray());
    }

    /// <summary>
    /// Produces the next immutable <see cref="FoilReqDocument"/> after answering a grill question,
    /// without mutating the input document.
    /// </summary>
    public static FoilReqDocument ResolveQuestion(
        FoilReqDocument doc,
        string questionId,
        string? selectedOptionId,
        string? customAnswer = null)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (!string.IsNullOrWhiteSpace(customAnswer))
        {
            var fw = FoilReqValidator.ValidateCustomText(customAnswer, $"grill.{questionId}");
            if (fw is not null)
                throw new ContractError(fw.Code, fw.Message);
        }

        var envelope = doc.Envelope.ToList();
        var objectives = doc.Objectives.ToList();
        var maneuvers = doc.Maneuvers.ToList();
        var openQuestions = doc.OpenQuestions.ToList();
        var water = doc.Water;

        switch (questionId)
        {
            case Q1UnitSanityId:
            {
                double mmVal = 950.0;
                var existing = envelope.FirstOrDefault(c => c.Key == "max_span");
                if (existing is { Quantity: { } q } && q.Unit == "cm" && q.Value >= 350.0 && q.Value <= 2200.0)
                    mmVal = q.Value;
                var prio = selectedOptionId == "opt-soft-fix"
                    ? new FoilReqPriority("soft", 0.8)
                    : new FoilReqPriority("hard");
                var replacement = new FoilReqConstraint(
                    "max_span", "<=", new FoilReqQty(mmVal, "mm"), false, prio, new FoilReqTag("@stated"));
                ReplaceOrAdd(envelope, c => c.Key == "max_span", replacement);
                break;
            }
            case Q2PhysicsTensionId:
            {
                if (selectedOptionId == "opt-high-ar")
                {
                    ReplaceOrAdd(envelope, c => c.Key == "target_ar",
                        new FoilReqConstraint("target_ar", "~", new FoilReqQty(11.8, null), false, new FoilReqPriority("soft", 0.9), new FoilReqTag("@stated")));
                    ReplaceOrAdd(objectives, o => o.Key == "takeoff_speed",
                        new FoilReqObjective("takeoff_speed", "<=", new FoilReqQty(12.0, "kn"), null, false, new FoilReqPriority("soft", 0.8), new FoilReqTag("@stated")));
                }
                else if (selectedOptionId == "opt-early-takeoff")
                {
                    ReplaceOrAdd(envelope, c => c.Key == "target_ar",
                        new FoilReqConstraint("target_ar", "~", new FoilReqQty(9.1, null), false, new FoilReqPriority("soft", 0.8), new FoilReqTag("@stated")));
                    ReplaceOrAdd(envelope, c => c.Key == "target_area",
                        new FoilReqConstraint("target_area", "~", new FoilReqQty(930.0, "cm2"), false, new FoilReqPriority("soft", 0.8), new FoilReqTag("@stated")));
                    ReplaceOrAdd(objectives, o => o.Key == "takeoff_speed",
                        new FoilReqObjective("takeoff_speed", "<=", new FoilReqQty(10.0, "kn"), null, false, new FoilReqPriority("high"), new FoilReqTag("@stated")));
                }
                else
                {
                    ReplaceOrAdd(envelope, c => c.Key == "target_ar",
                        new FoilReqConstraint("target_ar", "~", new FoilReqQty(10.2, null), false, new FoilReqPriority("soft", 0.85), new FoilReqTag("@stated")));
                    ReplaceOrAdd(envelope, c => c.Key == "target_area",
                        new FoilReqConstraint("target_area", "~", new FoilReqQty(845.0, "cm2"), false, new FoilReqPriority("soft", 0.85), new FoilReqTag("@stated")));
                    ReplaceOrAdd(objectives, o => o.Key == "takeoff_speed",
                        new FoilReqObjective("takeoff_speed", "<=", new FoilReqQty(10.8, "kn"), null, false, new FoilReqPriority("high"), new FoilReqTag("@stated")));
                }
                break;
            }
            case Q3SpeedCavitationId:
            {
                if (selectedOptionId == "opt-depth-unset")
                {
                    water = water with { DepthRef = null, DepthExplicitUnknown = true, DepthRefTag = new FoilReqTag("@unknown") };
                }
                else if (selectedOptionId == "opt-20kn-040m")
                {
                    water = water with { DepthRef = new FoilReqQty(0.40, "m"), DepthExplicitUnknown = false, DepthRefTag = new FoilReqTag("@stated") };
                    ReplaceOrAdd(objectives, o => o.Key == "drop_speed",
                        new FoilReqObjective("drop_speed", ">=", new FoilReqQty(20.0, "kn"), null, false, new FoilReqPriority("high"), new FoilReqTag("@stated")));
                }
                else
                {
                    water = water with { DepthRef = new FoilReqQty(0.45, "m"), DepthExplicitUnknown = false, DepthRefTag = new FoilReqTag("@stated") };
                    ReplaceOrAdd(objectives, o => o.Key == "drop_speed",
                        new FoilReqObjective("drop_speed", ">=", new FoilReqQty(24.0, "kn"), null, false, new FoilReqPriority("high"), new FoilReqTag("@stated")));
                }
                break;
            }
            case Q4ManeuverFeelId:
            {
                string desc = !string.IsNullOrWhiteSpace(customAnswer)
                    ? customAnswer.Trim()
                    : selectedOptionId == "opt-locked-rail"
                        ? "Locked-in rail drive (-3 deg anhedral, 0.40 taper)"
                        : "Snappy powered cutbacks (-5.5 deg anhedral, 0.34 taper)";
                ReplaceOrAdd(maneuvers, m => m.Key == "cutback_feel",
                    new FoilReqManeuver("cutback_feel", desc, new FoilReqPriority("high"), new FoilReqTag("@stated")));
                break;
            }
        }

        // Mark matching open question as @resolved if listed.
        for (int i = 0; i < openQuestions.Count; i++)
        {
            if (string.Equals(openQuestions[i].Id, questionId, StringComparison.Ordinal))
                openQuestions[i] = openQuestions[i] with { State = "@resolved" };
        }

        return doc with
        {
            Water = water,
            Envelope = envelope,
            Objectives = objectives,
            Maneuvers = maneuvers,
            OpenQuestions = openQuestions
        };
    }

    /// <summary>Marks a grill question skipped and flags any matching open question slot cleanly.</summary>
    public static FoilReqDocument SkipQuestion(FoilReqDocument doc, string questionId)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var openQuestions = doc.OpenQuestions.ToList();
        bool found = false;
        for (int i = 0; i < openQuestions.Count; i++)
        {
            if (string.Equals(openQuestions[i].Id, questionId, StringComparison.Ordinal))
            {
                openQuestions[i] = openQuestions[i] with { State = "@skipped" };
                found = true;
            }
        }
        if (!found)
            openQuestions.Add(new FoilReqOpenQuestion(questionId, $"Skipped grill branch '{questionId}' (Flagged assumption).", "@skipped"));

        return doc with { OpenQuestions = openQuestions };
    }

    /// <summary>Re-opens a previously answered or skipped grill branch, producing a new FoilReq version so older candidates derive <c>IsStale = true</c>.</summary>
    public static FoilReqDocument ReopenQuestion(FoilReqDocument doc, string questionId, string? prompt = null)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var openQuestions = doc.OpenQuestions.ToList();
        bool found = false;
        for (int i = 0; i < openQuestions.Count; i++)
        {
            if (string.Equals(openQuestions[i].Id, questionId, StringComparison.Ordinal))
            {
                openQuestions[i] = openQuestions[i] with { State = "@open" };
                found = true;
            }
        }
        if (!found)
            openQuestions.Add(new FoilReqOpenQuestion(questionId, prompt ?? $"Re-opened grill branch '{questionId}'.", "@open"));

        return doc with { OpenQuestions = openQuestions };
    }

    /// <summary>Terminates grilling immediately via GenerateNow, marking all remaining open questions <c>@skipped</c> (visibly Flagged on Scorecard).</summary>
    public static FoilReqDocument GenerateNowDocument(FoilReqDocument doc, FoilReqValidationResult validation, IReadOnlyList<GrillResolution> history)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var state = EvaluateState(doc, validation, history);
        var openQuestions = doc.OpenQuestions
            .Select(q => q.State == "@open" ? q with { State = "@skipped" } : q)
            .ToList();
        foreach (string openId in state.OpenQuestionIds)
        {
            if (!openQuestions.Any(q => q.Id == openId))
                openQuestions.Add(new FoilReqOpenQuestion(openId, $"Bypassed via Generate candidate now (Flagged assumption).", "@skipped"));
        }
        return doc with { OpenQuestions = openQuestions };
    }

    /// <summary>Derived staleness rule (TQ9, DM7): a candidate is stale iff its <c>ReqVersionId</c> differs from the latest FoilReq version.</summary>
    public static bool IsCandidateStale(CandidateIterationRecord candidate, string latestReqVersionId)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return !string.Equals(candidate.ReqVersionId, latestReqVersionId, StringComparison.Ordinal);
    }

    public static IReadOnlyList<string> DeriveStaleCandidateIds(
        IReadOnlyList<CandidateIterationRecord> candidates,
        string latestReqVersionId) =>
        candidates
            .Where(c => IsCandidateStale(c, latestReqVersionId))
            .Select(c => c.CandidateId)
            .ToArray();

    private static void ReplaceOrAdd<T>(List<T> list, Predicate<T> match, T replacement)
    {
        int idx = list.FindIndex(match);
        if (idx >= 0) list[idx] = replacement;
        else list.Add(replacement);
    }
}
