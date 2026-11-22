using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Node SET-2 red-first tests (T9 in docs/design/setup-seeding.md §7, Ruling 272):
/// ClassRules (GWA Race 2022, iQFOiL 2025, IKA Formula Kite 2024, Open class), DRC exclusions,
/// and SanityBounds.json BLAKE3 integrity + advisory product envelope findings.
/// </summary>
internal static class SetupDrcTests
{
    internal static void Run()
    {
        Check("ClassRules_GwaIqfoilFormulaKiteOpen_ExactFindings", T9_ClassRulesExactFindings);
        Check("SanityBounds_ProductEnvelope_ExactAdvisoryFindingsAndHashCheck", T9b_SanityBoundsAndHash);
    }

    // T9 (CAT-04): GWA S = 650 cm2 and mast = 120 cm emit located findings with rule id, version 2022-02-01, date;
    // iQFOiL shows 900 cm2 & -2..+1; Formula Kite shows +-2 mm, -1 mm; open class reads "open class"; exclusions join cleanly.
    // Mutant: dropping version/date or skipping mast > 115 cm check turns this RED.
    private static void T9_ClassRulesExactFindings()
    {
        // 1. GWA Race 2022 violation: S = 650 cm2 (< 700), mast = 120 cm (> 115), geometry NOT mutated.
        var smallWing = WingWithArea(0.065, 0.90, 0.10);
        var res = DisciplinePresets.ResolvePurpose("wingfoil-race");
        var goal = DisciplinePresets.BuildGoalState(
            Guid.NewGuid().ToString("D"), null, null, res,
            DisciplinePresets.DefaultSoftTargets(res.Preset),
            mastLengthCm: 120.0);

        var gwaFindings = ClassRules.Evaluate(smallWing, goal);
        var frontFinding = gwaFindings.Single(f => f.RuleId == "class.gwa.front_area");
        Equal(DrcSeverity.Warning, frontFinding.Severity);
        Equal("GWA Race Box v2022-02-01", frontFinding.RuleVersion);
        Equal("2022-02-01", frontFinding.RuleDate);
        Equal(true, frontFinding.Message.Contains("650", StringComparison.Ordinal) && frontFinding.Message.Contains("700 cm²", StringComparison.Ordinal));

        var mastFinding = gwaFindings.Single(f => f.RuleId == "class.gwa.mast_length");
        Equal(DrcSeverity.Warning, mastFinding.Severity);
        Equal("2022-02-01", mastFinding.RuleDate);
        Equal(true, mastFinding.Message.Contains("120", StringComparison.Ordinal) && mastFinding.Message.Contains("115 cm", StringComparison.Ordinal));

        // Exclusion joins cleanly by (designId, ruleId, locationKey).
        string designId = Guid.NewGuid().ToString("D");
        var ex = new DrcExclusion(Guid.NewGuid().ToString("D"), designId, "class.gwa.front_area", "GWA Race Box v2022-02-01", "planform.area", "High-wind prototype", "designer");
        var withEx = ClassRules.Evaluate(smallWing, goal, [ex], designId);
        Equal(true, withEx.Single(f => f.RuleId == "class.gwa.front_area").Excluded);
        Equal("High-wind prototype", withEx.Single(f => f.RuleId == "class.gwa.front_area").ExclusionReason);
        Equal(false, withEx.Single(f => f.RuleId == "class.gwa.mast_length").Excluded);

        // 2. iQFOiL 2025: 900 cm2 & shim -2..+1.
        var iqRes = DisciplinePresets.ResolvePurpose("windfoil-race");
        var iqGoal = DisciplinePresets.BuildGoalState(Guid.NewGuid().ToString("D"), null, null, iqRes, DisciplinePresets.DefaultSoftTargets(iqRes.Preset));
        var iqFindings = ClassRules.Evaluate(WingWithArea(0.090, 0.949, 0.095), iqGoal);
        var iq = iqFindings.Single(f => f.RuleId == "class.iqfoil.benchmark");
        Equal(DrcSeverity.Advisory, iq.Severity);
        Equal(true, iq.Message.Contains("900 cm²", StringComparison.Ordinal) && iq.Message.Contains("−2°…+1°", StringComparison.Ordinal));

        // Non-900 cm2 iQFOiL wing escalates to Warning.
        var iqWarn = ClassRules.Evaluate(WingWithArea(0.085, 0.949, 0.095), iqGoal).Single();
        Equal(DrcSeverity.Warning, iqWarn.Severity);

        // 3. Formula Kite 2024: span ±2 mm, chord −1 mm.
        var fkRes = DisciplinePresets.ResolvePurpose("kitefoil");
        var fkGoal = DisciplinePresets.BuildGoalState(Guid.NewGuid().ToString("D"), null, null, fkRes, DisciplinePresets.DefaultSoftTargets(fkRes.Preset));
        var fk = ClassRules.Evaluate(WingWithArea(0.062, 0.807, 0.092), fkGoal).Single(f => f.RuleId == "class.formulakite.tolerances");
        Equal(DrcSeverity.Advisory, fk.Severity);
        Equal(true, fk.Message.Contains("±2 mm", StringComparison.Ordinal) && fk.Message.Contains("−1 mm", StringComparison.Ordinal));

        // 4. Open class reads "open class" verbatim (case-insensitive) with Info severity and 0 warnings.
        var openRes = DisciplinePresets.ResolvePurpose("wingfoil-freeride");
        var openGoal = DisciplinePresets.BuildGoalState(Guid.NewGuid().ToString("D"), null, null, openRes, DisciplinePresets.DefaultSoftTargets(openRes.Preset));
        var open = ClassRules.Evaluate(WingWithArea(0.120, 0.980, 0.11), openGoal).Single(f => f.RuleId == "class.open");
        Equal(DrcSeverity.Info, open.Severity);
        Equal(true, open.Message.Contains("Open class", StringComparison.OrdinalIgnoreCase));
    }

    private static void T9b_SanityBoundsAndHash()
    {
        // Embedded SanityBounds.json passes BLAKE3 integrity check and has all 7 A5.5 rows.
        var cat = SanityBounds.Load();
        Equal(7, cat.Bounds.Count);
        Equal("2026-09-20", cat.Version);

        // Tampered bytes refuse with CAT-UNAVAILABLE.
        Refuses("CAT-UNAVAILABLE", () => SanityBounds.LoadFromBytes("{\"version\":\"tampered\",\"bounds\":[]}"u8.ToArray()));

        // Default wingfoil-race stays inside all geometric sanity bounds.
        var raceWing = WingWithArea(0.100, 1.095, 0.10);
        var clean = SanityBounds.Evaluate(raceWing);
        Equal(0, clean.Count);

        // Extreme tiny wing triggers advisory COPY-49 findings (never Error).
        var tinyWing = WingWithArea(0.035, 0.500, 0.07);
        var advisories = SanityBounds.Evaluate(tinyWing);
        Equal(true, advisories.Any(f => f.RuleId == "envelope.area"));
        Equal(true, advisories.Any(f => f.RuleId == "envelope.span"));
        Equal(true, advisories.Any(f => f.RuleId == "envelope.root_tc"));
        Equal(true, advisories.All(f => f.Severity == DrcSeverity.Advisory && f.Message.StartsWith("Out-of-envelope observation —", StringComparison.Ordinal)));
    }

    private static WingEstimates WingWithArea(double areaM2, double spanM, double maxTc)
    {
        double meanChord = areaM2 / spanM;
        double ar = spanM * spanM / areaM2;
        return new WingEstimates(spanM, meanChord / 0.81, meanChord * 0.24 / 0.81, meanChord, meanChord * 1.05, maxTc, ar, areaM2, "accepted", 0, true, new("test", 0, 1, 1, "ok"));
    }
}
