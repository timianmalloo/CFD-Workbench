using System.Globalization;

namespace CfdWorkbench.Core.Notebook;

/// <summary>
/// Pure zero-I/O AST for <c>FoilReq 1.0</c> matching the closed §A5 EBNF grammar
/// (<c>docs/specs/ai-guided-design-notebook.md:143-192</c>).
/// </summary>
public sealed record FoilReqQty(double Value, string? Unit)
{
    public bool TryToMeters(out double meters)
    {
        meters = 0.0;
        if (!double.IsFinite(Value)) return false;
        switch (Unit)
        {
            case "m": meters = Value; return true;
            case "cm": meters = Value * 0.01; return true;
            case "mm": meters = Value * 0.001; return true;
            case "in": meters = Value * 0.0254; return true;
            default: return false;
        }
    }

    public bool TryToSquareMeters(out double sqMeters)
    {
        sqMeters = 0.0;
        if (!double.IsFinite(Value)) return false;
        switch (Unit)
        {
            case "m2": sqMeters = Value; return true;
            case "cm2": sqMeters = Value * 1e-4; return true;
            default: return false;
        }
    }

    public bool TryToKg(out double kg)
    {
        kg = 0.0;
        if (!double.IsFinite(Value)) return false;
        switch (Unit)
        {
            case "kg": kg = Value; return true;
            case "N": kg = Value / GoalStateVersion.StandardGravity; return true;
            default: return false;
        }
    }

    public bool TryToKnots(out double knots)
    {
        knots = 0.0;
        if (!double.IsFinite(Value)) return false;
        switch (Unit)
        {
            case "kn": knots = Value; return true;
            case "m/s": knots = Value / DisciplinePresets.KnotsToMps; return true;
            default: return false;
        }
    }

    public bool TryToMps(out double mps)
    {
        mps = 0.0;
        if (!double.IsFinite(Value)) return false;
        switch (Unit)
        {
            case "m/s": mps = Value; return true;
            case "kn": mps = Value * DisciplinePresets.KnotsToMps; return true;
            default: return false;
        }
    }

    public bool TryToCelsius(out double celsius)
    {
        celsius = 0.0;
        if (!double.IsFinite(Value) || Unit != "C") return false;
        celsius = Value;
        return true;
    }

    public bool TryToPa(out double pa)
    {
        pa = 0.0;
        if (!double.IsFinite(Value) || Unit != "kPa") return false;
        pa = Value * 1000.0;
        return true;
    }

    public string Format() => Unit is null
        ? Value.ToString("0.######", CultureInfo.InvariantCulture)
        : $"{Value.ToString("0.######", CultureInfo.InvariantCulture)} {Unit}";
}

public sealed record FoilReqPriority(string Kind, double? Weight = null)
{
    public string Format() => Kind == "soft" && Weight is { } w
        ? $"soft({w.ToString("0.######", CultureInfo.InvariantCulture)})"
        : Kind;
}

public sealed record FoilReqTag(string Kind, string? Rationale = null)
{
    public string Format() => Rationale is null
        ? Kind
        : $"{Kind}({FoilReqSerializer.QuoteString(Rationale)})";
}

public sealed record FoilReqBrief(string Discipline, string Skill);

public sealed record FoilReqRiderField(string Key, FoilReqQty Quantity, FoilReqTag Tag);

public sealed record FoilReqRider(IReadOnlyList<FoilReqRiderField> Fields)
{
    public FoilReqRiderField? Find(string key) =>
        Fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.Ordinal));
}

public sealed record FoilReqWater(
    string Kind,
    FoilReqTag KindTag,
    FoilReqQty Temp,
    FoilReqTag TempTag,
    FoilReqQty? DepthRef = null,
    FoilReqTag? DepthRefTag = null,
    bool DepthExplicitUnknown = false,
    FoilReqQty? PAtm = null,
    FoilReqTag? PAtmTag = null)
{
    public bool IsDepthUnset => DepthExplicitUnknown || DepthRef is null;
}

public sealed record FoilReqConstraint(
    string Key,
    string Comparator,
    FoilReqQty? Quantity,
    bool IsUnknown,
    FoilReqPriority? Priority,
    FoilReqTag Tag);

public sealed record FoilReqReference(
    string Name,
    IReadOnlyList<string> Traits,
    string Policy,
    string Confidence);

public sealed record FoilReqObjective(
    string Key,
    string? Comparator,
    FoilReqQty? Quantity,
    FoilReqPriority? ValuePriority,
    bool IsUnknown,
    FoilReqPriority? Priority,
    FoilReqTag Tag);

public sealed record FoilReqManeuver(
    string Key,
    string Description,
    FoilReqPriority Priority,
    FoilReqTag Tag);

public sealed record FoilReqOpenQuestion(
    string Id,
    string Prompt,
    string State);

public sealed record FoilReqDocument(
    string Version,
    FoilReqBrief Brief,
    FoilReqRider Rider,
    FoilReqWater Water,
    IReadOnlyList<FoilReqConstraint> Envelope,
    IReadOnlyList<FoilReqReference> References,
    IReadOnlyList<FoilReqObjective> Objectives,
    IReadOnlyList<FoilReqManeuver> Maneuvers,
    IReadOnlyList<FoilReqOpenQuestion> OpenQuestions)
{
    public const string CurrentVersion = "1.0";
    public const string ConceptualReferencePolicy = "conceptual-only-no-proprietary-geometry";

    public static readonly IReadOnlySet<string> AllowedDisciplines = new HashSet<string>(StringComparer.Ordinal)
    {
        "wingfoil-freeride", "wingfoil-race", "wingfoil-surf",
        "windfoil-race", "windfoil-freeride", "wakefoil",
        "surffoil", "downwind", "pump", "parawing", "kitefoil", "e-foil"
    };

    public static readonly IReadOnlySet<string> AllowedSkills = new HashSet<string>(StringComparer.Ordinal)
    {
        "beginner", "intermediate", "advanced", "pro"
    };

    public static readonly IReadOnlySet<string> AllowedRiderFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "mass", "board_mass", "foil_mass", "wing_kit_mass"
    };

    public static readonly IReadOnlySet<string> AllowedConstraintKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "max_span", "min_span", "target_area", "target_ar", "max_chord", "min_tip_chord", "root_tc"
    };

    public static readonly IReadOnlySet<string> AllowedUnits = new HashSet<string>(StringComparer.Ordinal)
    {
        "mm", "cm", "m", "in", "cm2", "m2", "kg", "N", "C", "kPa", "kn", "m/s", "deg", "%"
    };

    public static readonly IReadOnlySet<string> AllowedTags = new HashSet<string>(StringComparer.Ordinal)
    {
        "@stated", "@inferred", "@defaulted", "@flagged", "@unknown"
    };

    public static readonly IReadOnlySet<string> AllowedQuestionStates = new HashSet<string>(StringComparer.Ordinal)
    {
        "@open", "@resolved", "@skipped"
    };

    public FoilReqConstraint? FindConstraint(string key) =>
        Envelope.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.Ordinal));

    public FoilReqObjective? FindObjective(string key) =>
        Objectives.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.Ordinal));
}

public sealed record FoilReqParseResult(
    bool IsValid,
    FoilReqDocument? Document,
    string? ErrorCode,
    string? ErrorMessage,
    int? Line = null,
    int? Column = null);
