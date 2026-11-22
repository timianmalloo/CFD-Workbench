using System.Globalization;

namespace CfdWorkbench.Core;

// Class-rule presets and pure DRC evaluators (docs/design/setup-seeding.md §4.3, CAT-04, A5.5, A5.8).
// Never blocks seeding and never mutates geometry (GOAL-03).

public enum DrcSeverity
{
    Error,
    Warning,
    Advisory,
    Info
}

/// <summary>One located KiCad-shaped DRC finding produced by ClassRules or SanityBounds.</summary>
public sealed record DrcFinding(
    string RuleId,
    string RuleVersion,
    string RuleDate,
    DrcSeverity Severity,
    string LocationKey,
    string LocationLabel,
    string Message,
    string JumpTarget,
    bool Excluded = false,
    string? ExclusionReason = null,
    string? ExclusionAttribution = null);

public sealed record ClassRulePresetInfo(
    string Id,
    string Name,
    string Version,
    string Date,
    string Summary);

public static class ClassRules
{
    public const string GwaRace2022 = "gwa-race-2022";
    public const string IqFoil2025 = "iqfoil-2025";
    public const string FormulaKite2024 = "formula-kite-2024";
    public const string OpenClass = "open-class";

    public static IReadOnlyList<ClassRulePresetInfo> Presets { get; } =
    [
        new(GwaRace2022, "GWA Race Box v2022-02-01", "GWA Race Box v2022-02-01", "2022-02-01",
            "Front wing >= 700 cm², rear >= 150 cm², mast <= 115 cm, fuselage <= 100 cm, foil >= 3.0 kg"),
        new(IqFoil2025, "iQFOiL One-Design 2025", "iQFOiL One-Design 2025", "2025",
            "900 cm² front · 255 cm² tail · 115 Plus fuselage · 95 cm mast · shim −2°…+1°"),
        new(FormulaKite2024, "IKA Formula Kite 2024", "IKA Formula Kite 2024", "2024-08-10",
            "Registration tolerances: span ±2 mm · chord −1 mm · general ±0.4 mm · mass ±4 % (maxima Flagged)"),
        new(OpenClass, "Open class", "Open class", "open",
            "Open class — no equipment box rules apply.")
    ];

    /// <summary>
    /// Evaluates the active class-rule preset against the current wing estimates and Goal state equipment context.
    /// Joins any user <see cref="DrcExclusion"/> facts matching <c>(designId, ruleId, locationKey)</c>.
    /// </summary>
    public static IReadOnlyList<DrcFinding> Evaluate(
        WingEstimates wing,
        GoalStateVersion goal,
        IReadOnlyList<DrcExclusion>? exclusions = null,
        string? designId = null)
    {
        ArgumentNullException.ThrowIfNull(wing);
        ArgumentNullException.ThrowIfNull(goal);

        var raw = new List<DrcFinding>();
        double areaCm2 = wing.AreaSquareMeters * 1e4;
        var eq = goal.EquipmentContext;

        switch (goal.ClassRulePresetId)
        {
            case GwaRace2022:
                const string gwaVer = "GWA Race Box v2022-02-01";
                const string gwaDate = "2022-02-01";
                if (areaCm2 < 700.0 - 1e-9)
                {
                    raw.Add(new DrcFinding(
                        "class.gwa.front_area", gwaVer, gwaDate, DrcSeverity.Warning,
                        "planform.area", "Front wing area",
                        string.Create(CultureInfo.InvariantCulture,
                            $"Front wing area {areaCm2:0.#} cm² is below GWA Race minimum 700 cm² ({gwaVer}, {gwaDate})."),
                        "targetArea"));
                }
                if (eq.RearAreaCm2 < 150.0 - 1e-9)
                {
                    raw.Add(new DrcFinding(
                        "class.gwa.rear_area", gwaVer, gwaDate, DrcSeverity.Warning,
                        "equipment.rear_area", "Rear wing area",
                        string.Create(CultureInfo.InvariantCulture,
                            $"Rear wing area {eq.RearAreaCm2:0.#} cm² is below GWA Race minimum 150 cm² ({gwaVer}, {gwaDate})."),
                        "rearArea"));
                }
                if (eq.MastLengthCm > 115.0 + 1e-9)
                {
                    raw.Add(new DrcFinding(
                        "class.gwa.mast_length", gwaVer, gwaDate, DrcSeverity.Warning,
                        "equipment.mast_length", "Mast length",
                        string.Create(CultureInfo.InvariantCulture,
                            $"Mast length {eq.MastLengthCm:0.#} cm exceeds GWA Race maximum 115 cm ({gwaVer}, {gwaDate})."),
                        "mastLength"));
                }
                if (eq.FuselageLengthCm > 100.0 + 1e-9)
                {
                    raw.Add(new DrcFinding(
                        "class.gwa.fuselage_length", gwaVer, gwaDate, DrcSeverity.Warning,
                        "equipment.fuselage_length", "Fuselage length",
                        string.Create(CultureInfo.InvariantCulture,
                            $"Fuselage length {eq.FuselageLengthCm:0.#} cm exceeds GWA Race maximum 100 cm ({gwaVer}, {gwaDate})."),
                        "fuselageLength"));
                }
                if (eq.FoilMassKg < 3.0 - 1e-9)
                {
                    raw.Add(new DrcFinding(
                        "class.gwa.foil_mass", gwaVer, gwaDate, DrcSeverity.Warning,
                        "equipment.foil_mass", "Foil mass",
                        string.Create(CultureInfo.InvariantCulture,
                            $"Foil mass {eq.FoilMassKg:0.##} kg is below GWA Race minimum 3.0 kg ({gwaVer}, {gwaDate})."),
                        "foilMass"));
                }
                break;

            case IqFoil2025:
                const string iqVer = "iQFOiL One-Design 2025";
                const string iqDate = "2025";
                bool areaDeviates = Math.Abs(areaCm2 - 900.0) > 1.0;
                raw.Add(new DrcFinding(
                    "class.iqfoil.benchmark", iqVer, iqDate,
                    areaDeviates ? DrcSeverity.Warning : DrcSeverity.Advisory,
                    "planform.area", "iQFOiL one-design benchmark",
                    string.Create(CultureInfo.InvariantCulture,
                        $"iQFOiL one-design benchmark: 900 cm² front · 255 cm² tail · 115 Plus fuselage · 95 cm mast · shim −2°…+1°; current wing is {areaCm2:0.#} cm² (iQFOiL 2025)."),
                    "targetArea"));
                break;

            case FormulaKite2024:
                raw.Add(new DrcFinding(
                    "class.formulakite.tolerances", "IKA Formula Kite 2024", "2024-08-10", DrcSeverity.Advisory,
                    "class.formulakite", "IKA Formula Kite tolerances",
                    "IKA Formula Kite tolerances: span ±2 mm · chord −1 mm · general ±0.4 mm · mass ±4 % (dimensional maxima Flagged).",
                    "classRule"));
                break;

            default:
                raw.Add(new DrcFinding(
                    "class.open", "Open class", "open", DrcSeverity.Info,
                    "class.open", "Open class",
                    "Open class — no equipment box rules apply.",
                    "classRule"));
                break;
        }

        return ApplyExclusions(raw, exclusions, designId);
    }

    internal static IReadOnlyList<DrcFinding> ApplyExclusions(
        List<DrcFinding> findings,
        IReadOnlyList<DrcExclusion>? exclusions,
        string? designId)
    {
        if (exclusions is null || exclusions.Count == 0 || string.IsNullOrWhiteSpace(designId))
            return findings;

        var result = new List<DrcFinding>(findings.Count);
        foreach (var f in findings)
        {
            var match = exclusions.LastOrDefault(ex =>
                string.Equals(ex.DesignId, designId, StringComparison.Ordinal) &&
                string.Equals(ex.RuleId, f.RuleId, StringComparison.Ordinal) &&
                string.Equals(ex.LocationKey, f.LocationKey, StringComparison.Ordinal));
            result.Add(match is null
                ? f
                : f with { Excluded = true, ExclusionReason = match.Reason, ExclusionAttribution = match.Attribution });
        }
        return result;
    }
}
