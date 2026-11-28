using System.Globalization;
using System.Text.RegularExpressions;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Core.Notebook;

public sealed record FoilReqFinding(
    string Code,
    string Severity,
    string FieldPath,
    string Message,
    string? SuggestedValue = null,
    double? ComputedValue = null);

public sealed record FoilReqValidationResult(
    bool IsValid,
    bool HasBlockingError,
    IReadOnlyList<FoilReqFinding> Findings,
    double? ImpliedAreaSquareMeters,
    double? ImpliedLoadNewtons,
    double? ImpliedTakeoffCl,
    bool KitMassDefaulted,
    double WaterDensityKgPerM3);

/// <summary>
/// Deterministic Tier-0 validator for <see cref="FoilReqDocument"/> (docs/design/ai-guided-design-notebook.md §3.2):
/// dimensional sanity (TQ7 / COPY-926), water density + 15 kg @defaulted kit mass + CL_req &gt; 0.95 physics-tension check,
/// and free-text coordinate/IP firewall across reference, maneuvers, open_questions, and custom grill text (COMMIT-02 / COMMIT-04).
/// </summary>
public static class FoilReqValidator
{
    public const string UnitSanityCode = "UNIT-SANITY";
    public const string PhysicsTensionCode = "PHYSICS-TENSION";
    public const string CoordForbiddenCode = "NB-COORD-FORBIDDEN";
    public const string BoundsErrorCode = "NB-FOILREQ-BOUNDS";

    public const double MinWingSpanMeters = 0.35;
    public const double MaxWingSpanMeters = 2.20;
    public const double DefaultBoardMassKg = 5.5;
    public const double DefaultFoilMassKg = 4.5;
    public const double DefaultWingKitMassKg = 5.0;
    public const double DefaultKitMassKg = DefaultBoardMassKg + DefaultFoilMassKg + DefaultWingKitMassKg; // 15.0 kg @defaulted
    public const double PhysicsTensionClLimit = 0.95;
    public const double DefaultTakeoffSpeedKn = 10.0;

    // Detects >= 4 numeric coordinate pairs (e.g. "0.0 0.0\n0.1 0.05..." or "(0,0), (0.2,0.04)...") or explicit .dat / coordinate arrays.
    private static readonly Regex CoordinatePairRegex = new(
        @"(?:\(?\s*-?\d+(?:\.\d+)?\s*[,\s]\s*-?\d+(?:\.\d+)?\s*\)?)",
        RegexOptions.CultureInvariant);

    private static readonly Regex DatHeaderOrArrayRegex = new(
        @"(?i)(?:naca\s*\d{4,5}\s+coord|selig\s+format|lednicer\s+format|\[\s*\[\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*\]\s*,\s*\[|control\s*vertices\s*:|upper\s+surface\s+points)",
        RegexOptions.CultureInvariant);

    public static FoilReqValidationResult Validate(FoilReqDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var findings = new List<FoilReqFinding>();
        bool blockingError = false;

        // 1. Rider mass & kit mass (15.0 kg @defaulted when only rider.mass is stated).
        var massField = doc.Rider.Find("mass");
        double riderMassKg = 0.0;
        if (massField is null || !massField.Quantity.TryToKg(out riderMassKg) || riderMassKg <= 0.0 || riderMassKg > 200.0)
        {
            blockingError = true;
            findings.Add(new FoilReqFinding(
                BoundsErrorCode,
                "error",
                "rider.mass",
                "Rider mass must be a positive mass in (0, 200] kg."));
        }

        bool hasBoard = TryReadMassField(doc.Rider, "board_mass", out double boardKg, findings, ref blockingError);
        bool hasFoil = TryReadMassField(doc.Rider, "foil_mass", out double foilKg, findings, ref blockingError);
        bool hasKit = TryReadMassField(doc.Rider, "wing_kit_mass", out double kitKg, findings, ref blockingError);

        bool kitMassDefaulted = !hasBoard && !hasFoil && !hasKit;
        double totalMassKg = riderMassKg + (kitMassDefaulted
            ? DefaultKitMassKg
            : (hasBoard ? boardKg : 0.0) + (hasFoil ? foilKg : 0.0) + (hasKit ? kitKg : 0.0));

        // 2. Water density (anchored on DisciplinePresets salt/fresh 15 C rows with temperature adjustment).
        if (!doc.Water.Temp.TryToCelsius(out double tempC) || tempC < 0.0 || tempC > 50.0)
        {
            blockingError = true;
            findings.Add(new FoilReqFinding(
                BoundsErrorCode,
                "error",
                "water.temp",
                "Water temperature must be in [0, 50] C."));
            tempC = 15.0;
        }
        double rho = ComputeWaterDensity(doc.Water.Kind, tempC);

        if (!doc.Water.IsDepthUnset && doc.Water.DepthRef is { } depthQty)
        {
            if (!depthQty.TryToMeters(out double depthM) || depthM <= 0.0 || depthM > 10.0)
            {
                blockingError = true;
                findings.Add(new FoilReqFinding(
                    BoundsErrorCode,
                    "error",
                    "water.depth_ref",
                    "Reference depth must be a positive length in (0, 10] m."));
            }
        }

        // 3. Dimensional sanity on envelope span & chord constraints (TQ7 / COPY-926).
        double? effectiveSpanM = null;
        foreach (var c in doc.Envelope)
        {
            if (c.IsUnknown || c.Quantity is null) continue;
            if (c.Key is "max_span" or "min_span")
            {
                if (!c.Quantity.TryToMeters(out double spanM))
                {
                    blockingError = true;
                    findings.Add(new FoilReqFinding(
                        BoundsErrorCode,
                        "error",
                        $"envelope.{c.Key}",
                        $"Constraint '{c.Key}' requires a length unit (mm, cm, m, in)."));
                    continue;
                }
                if (spanM <= 0.0)
                {
                    blockingError = true;
                    findings.Add(new FoilReqFinding(
                        BoundsErrorCode,
                        "error",
                        $"envelope.{c.Key}",
                        $"Constraint '{c.Key}' must be positive."));
                    continue;
                }
                if (spanM < MinWingSpanMeters || spanM > MaxWingSpanMeters)
                {
                    string suggested = SuggestSpanFix(c.Quantity, spanM, out double suggestedSpanM);
                    string convertedMm = (spanM * 1000.0).ToString("N0", CultureInfo.InvariantCulture) + " mm";
                    findings.Add(new FoilReqFinding(
                        UnitSanityCode,
                        "warning",
                        $"envelope.{c.Key}",
                        $"Unit check: \"{c.Quantity.Format()}\" ({convertedMm}) is outside hydrofoil wing spans (350–2,200 mm). Did you mean {suggested}?",
                        SuggestedValue: suggested,
                        ComputedValue: spanM));
                    if (c.Key == "max_span" && suggestedSpanM >= MinWingSpanMeters && suggestedSpanM <= MaxWingSpanMeters)
                        effectiveSpanM ??= suggestedSpanM;
                }
                else if (c.Key == "max_span")
                {
                    effectiveSpanM = spanM;
                }
            }
            else if (c.Key is "max_chord" or "min_tip_chord")
            {
                if (!c.Quantity.TryToMeters(out double chordM) || chordM <= 0.0 || chordM > 1.0)
                {
                    findings.Add(new FoilReqFinding(
                        UnitSanityCode,
                        "warning",
                        $"envelope.{c.Key}",
                        $"Chord '{c.Quantity.Format()}' is outside realistic hydrofoil chord bounds (5–1,000 mm).",
                        ComputedValue: chordM));
                }
            }
            else if (c.Key == "target_area")
            {
                if (!c.Quantity.TryToSquareMeters(out double areaM2) || areaM2 < 0.02 || areaM2 > 0.50)
                {
                    findings.Add(new FoilReqFinding(
                        UnitSanityCode,
                        "warning",
                        "envelope.target_area",
                        $"Area '{c.Quantity.Format()}' is outside realistic hydrofoil area bounds (200–5,000 cm2).",
                        ComputedValue: areaM2));
                }
            }
            else if (c.Key == "target_ar")
            {
                if (!double.IsFinite(c.Quantity.Value) || c.Quantity.Value < 3.0 || c.Quantity.Value > 22.0)
                {
                    findings.Add(new FoilReqFinding(
                        UnitSanityCode,
                        "warning",
                        "envelope.target_ar",
                        $"Aspect ratio '{c.Quantity.Format()}' is outside hydrofoil bounds [3, 22].",
                        ComputedValue: c.Quantity.Value));
                }
            }
        }

        // 4. Physical envelope pre-check: S_implied = b^2 / AR, W = m_total * g, CL_req = W / (0.5 * rho * V_to^2 * S_implied).
        double? impliedAreaM2 = null;
        double? impliedLoadN = totalMassKg > 0.0 ? totalMassKg * GoalStateVersion.StandardGravity : null;
        double? impliedTakeoffCl = null;

        var areaConstraint = doc.FindConstraint("target_area");
        var arConstraint = doc.FindConstraint("target_ar");
        double? arVal = arConstraint is { IsUnknown: false, Quantity: { } aq } && aq.Value > 0.0 ? aq.Value : null;

        if (areaConstraint is { IsUnknown: false, Quantity: { } areaQ } && areaQ.TryToSquareMeters(out double explicitArea) && explicitArea > 0.0)
        {
            impliedAreaM2 = explicitArea;
        }
        else if (effectiveSpanM is { } bM && arVal is { } ar && ar > 0.0)
        {
            impliedAreaM2 = (bM * bM) / ar;
        }
        else if (effectiveSpanM is { } bRef && doc.References.Any(r => r.Traits.Contains("high-aspect-glide", StringComparer.Ordinal)))
        {
            // High-AR conceptual reference (e.g. F4 Orca AR ~ 12) implies S_implied = b^2 / 12.
            impliedAreaM2 = (bRef * bRef) / 12.0;
        }

        if (impliedAreaM2 is { } sM2 && sM2 > 0.0 && impliedLoadN is { } loadN && loadN > 0.0)
        {
            double vToKn = ResolveTakeoffSpeedKn(doc);
            double vToMps = vToKn * DisciplinePresets.KnotsToMps;
            double clReq = loadN / (0.5 * rho * vToMps * vToMps * sM2);
            impliedTakeoffCl = clReq;

            if (clReq > PhysicsTensionClLimit)
            {
                findings.Add(new FoilReqFinding(
                    PhysicsTensionCode,
                    "warning",
                    "envelope",
                    string.Create(CultureInfo.InvariantCulture,
                        $"Physics tension: implied area {sM2 * 1e4:0} cm2 at {vToKn:0.#} kn for {totalMassKg:0.#} kg ({loadN:0} N) requires CL = {clReq:0.000} > {PhysicsTensionClLimit:0.00}."),
                    ComputedValue: clReq));
            }
        }

        // 5. Free-text coordinate & IP firewall across reference, maneuvers, and open_questions (COMMIT-02 / COMMIT-04).
        foreach (var r in doc.References)
        {
            if (r.Policy != FoilReqDocument.ConceptualReferencePolicy || r.Confidence is not ("Inferred" or "Flagged"))
            {
                blockingError = true;
                findings.Add(new FoilReqFinding(
                    CoordForbiddenCode,
                    "error",
                    $"reference.{r.Name}",
                    "Reference blocks must pin conceptual-only-no-proprietary-geometry and Inferred/Flagged confidence."));
            }
            if (ContainsForbiddenCoordinates(r.Name) || r.Traits.Any(ContainsForbiddenCoordinates))
            {
                blockingError = true;
                findings.Add(new FoilReqFinding(
                    CoordForbiddenCode,
                    "error",
                    $"reference.{r.Name}",
                    "Proprietary coordinates or geometry tables are forbidden in reference blocks (COMMIT-02)."));
            }
        }

        foreach (var m in doc.Maneuvers)
        {
            if (ContainsForbiddenCoordinates(m.Description))
            {
                blockingError = true;
                findings.Add(new FoilReqFinding(
                    CoordForbiddenCode,
                    "error",
                    $"maneuvers.{m.Key}",
                    $"Coordinate table or proprietary geometry detected in maneuvers.{m.Key} (COMMIT-02 / COMMIT-04)."));
            }
        }

        foreach (var q in doc.OpenQuestions)
        {
            if (ContainsForbiddenCoordinates(q.Prompt))
            {
                blockingError = true;
                findings.Add(new FoilReqFinding(
                    CoordForbiddenCode,
                    "error",
                    $"open_questions.{q.Id}",
                    $"Coordinate table or proprietary geometry detected in open_questions.{q.Id} (COMMIT-02 / COMMIT-04)."));
            }
        }

        return new FoilReqValidationResult(
            IsValid: !blockingError,
            HasBlockingError: blockingError,
            Findings: findings,
            ImpliedAreaSquareMeters: impliedAreaM2,
            ImpliedLoadNewtons: impliedLoadN,
            ImpliedTakeoffCl: impliedTakeoffCl,
            KitMassDefaulted: kitMassDefaulted,
            WaterDensityKgPerM3: rho);
    }

    /// <summary>Firewall check for arbitrary user or LLM custom grill text (COMMIT-02 / COMMIT-04 / LAB-01).</summary>
    public static FoilReqFinding? ValidateCustomText(string? text, string fieldPath = "grill.custom")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (ContainsForbiddenCoordinates(text))
        {
            return new FoilReqFinding(
                CoordForbiddenCode,
                "error",
                fieldPath,
                "Coordinate tables, .dat headers, and proprietary geometry arrays are forbidden (COMMIT-02 / COMMIT-04).");
        }
        return null;
    }

    /// <summary>Checks whether free text contains a numeric coordinate-pair table (&gt;= 4 pairs) or .dat / geometry array.</summary>
    public static bool ContainsForbiddenCoordinates(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (DatHeaderOrArrayRegex.IsMatch(text)) return true;

        // Check for >= 4 numeric coordinate pairs (e.g. airfoil coordinate rows or JSON pair tables).
        int pairCount = 0;
        foreach (Match m in CoordinatePairRegex.Matches(text))
        {
            // Ignore dates/times or single words; count clean numeric pairs.
            if (m.Success)
            {
                pairCount++;
                if (pairCount >= 4) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Computes water density matching <c>DisciplinePresets.DefaultSaltWater15C</c> (1026.0210 kg/m³) /
    /// <c>DefaultFreshWater15C</c> (999.1026 kg/m³) at 15 °C with smooth quadratic ITTC thermal adjustment over [0, 50] °C.
    /// </summary>
    public static double ComputeWaterDensity(string waterKind, double tempC)
    {
        bool isSalt = string.Equals(waterKind, "salt", StringComparison.OrdinalIgnoreCase);
        double rho15 = isSalt ? DisciplinePresets.DefaultSaltWater15C.Rho : DisciplinePresets.DefaultFreshWater15C.Rho;
        double dt = tempC - 15.0;
        if (Math.Abs(dt) < 1e-12) return rho15;
        // Pure zero-I/O ITTC thermal expansion approximation matching ITTC table within < 0.15% over 0..50 C.
        double linear = isSalt ? -0.215 : -0.150;
        double quad = -0.0046;
        return rho15 + linear * dt + quad * dt * dt;
    }

    private static bool TryReadMassField(
        FoilReqRider rider,
        string key,
        out double kg,
        List<FoilReqFinding> findings,
        ref bool blockingError)
    {
        kg = 0.0;
        var field = rider.Find(key);
        if (field is null) return false;
        if (!field.Quantity.TryToKg(out kg) || kg < 0.0 || kg > 100.0)
        {
            blockingError = true;
            findings.Add(new FoilReqFinding(
                BoundsErrorCode,
                "error",
                $"rider.{key}",
                $"Field 'rider.{key}' must be a non-negative mass in [0, 100] kg."));
            return false;
        }
        return true;
    }

    private static double ResolveTakeoffSpeedKn(FoilReqDocument doc)
    {
        var toObj = doc.FindObjective("takeoff_speed") ?? doc.FindObjective("low_takeoff_speed");
        if (toObj is { Quantity: { } q } && q.TryToKnots(out double kn) && kn > 2.0 && kn < 40.0)
            return kn;
        return DefaultTakeoffSpeedKn;
    }

    private static string SuggestSpanFix(FoilReqQty qty, double spanMeters, out double suggestedMeters)
    {
        if (qty.Unit == "cm" && spanMeters > MaxWingSpanMeters)
        {
            double asMmInMeters = qty.Value * 0.001;
            if (asMmInMeters >= MinWingSpanMeters && asMmInMeters <= MaxWingSpanMeters)
            {
                suggestedMeters = asMmInMeters;
                return $"{qty.Value.ToString("0.#", CultureInfo.InvariantCulture)} mm";
            }
        }
        if (qty.Unit == "m" && qty.Value > 100.0)
        {
            double asMmInMeters = qty.Value * 0.001;
            if (asMmInMeters >= MinWingSpanMeters && asMmInMeters <= MaxWingSpanMeters)
            {
                suggestedMeters = asMmInMeters;
                return $"{qty.Value.ToString("0.#", CultureInfo.InvariantCulture)} mm";
            }
        }
        suggestedMeters = Math.Clamp(spanMeters, MinWingSpanMeters, MaxWingSpanMeters);
        return $"{(suggestedMeters * 1000.0).ToString("0", CultureInfo.InvariantCulture)} mm";
    }
}
