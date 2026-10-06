using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Analysis copy and fixed tier labels. COPY-206…239 are proposed in DESIGN.md §7.</summary>
public static class Labels
{
    public const string NoResult = "No analysis yet. Set the conditions, then Evaluate."; // COPY-206
    public const string NoPolar = "Unavailable — no polar method installed"; // COPY-210
    public const string PayloadFailed = "Unavailable — run payload failed its check"; // COPY-211
    public const string VlmChip = "VLM + strip · local calculation"; // COPY-213
    public const string EstimatorChip = "Estimator · local calculation"; // COPY-214
    public const string PolarChip = "Polar · local calculation"; // COPY-215
    public const string OutsideLattice = "outside the verified lattice family"; // COPY-216, operator proposal
    public const string VerifiedLattice = "Verified fixture family: rectangular and elliptic planforms; ±20° dihedral at 32 × 4 cosine span, uniform chord (F-8); 45° sweep, AR 5, at 4 × 1 uniform (F-16); 4% camber at 32/64/128 × 4 cosine/cosine (F-18); 1° washin at 32/64/128 × 4 cosine/cosine (F-19); F-6 order at 32/64/128 × 4 cosine span, uniform chord; F-21 at 16 × 4 cosine/cosine per half"; // COPY-217, operator proposal
    // COPY-218 and COPY-219 are retired (Ruling 92); their rows stay in DESIGN.md and no string survives here.
    public const string TipNotJudged = "Not judged — tip strip"; // COPY-220, approved Ruling 92 (the state of Rulings 78 and 88 D13)
    public const string TipChordUnderMinimumTemplate = "Unavailable — tip chord under the minimum (<min>). The tip is not certified for analysis."; // COPY-241, Ruling 94
    public static string TipChordUnderMinimum(double rootChordMeters) =>
        TipChordUnderMinimumTemplate.Replace("<min>", TipChord.Format(rootChordMeters));
    public const string Inside = "inside the method envelope"; // COPY-221
    public const string Outside = "outside the method envelope"; // COPY-222
    public const string FixedVlmDeep = "attached flow; no stall; no ventilation; deep water"; // COPY-223
    public const string FixedVlmNoDepth = "attached flow; no stall; no ventilation; free surface not modelled"; // COPY-224
    public const string NotModelledSet = "Not modelled: ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”."; // COPY-225
    public const string NotModelledUnset = "Not modelled: free surface, ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”."; // COPY-226
    public const string StructuralList = "Not assessed: take-off, pumping, breach and slam, ventilation shock, impact, fatigue."; // COPY-227
    public const string TipDepthMissing = "Unavailable — station depth not recorded"; // COPY-228
    public const string ThicknessMissing = "Unavailable — root thickness not recorded"; // COPY-229
    public const string NearFieldFlag = "Near-field diagnostics flagged outside the verified lattice family"; // COPY-231
    public const string EBelowBand = "e below 0.85; result remains available"; // COPY-232, no cause attributed
    public const string EAboveOne = "1 < e ≤ 1.02 at 64 × 4 — small lattice bias (~+0.01); result remains available"; // COPY-233
    public const string EAboveLatticeCheck = "e above 1 — check the lattice"; // COPY-240, no cause claimed
    public const string NoVcrit = "Unavailable — needs −Cp_min"; // COPY-234
    public const string StripWidthMissing = "Unavailable — strip width not recorded; vector omitted"; // COPY-235
    // simplify: working copy for the unheld-revision note (HIST); ceiling one string, upgrade when a copy ruling names it.
    public const string FeedRevisionNotHeld = "Unavailable — this run's geometry revision is not held by this session; verdicts, stations and normals omitted";
    public const string BodyAxes = "Body axes: +x aft, +y starboard, +z up; lift and drag in wind axes"; // COPY-236
    public const string RootMoment = "Root bending moment about the root plane; positive sense about +x"; // COPY-237
    public const string ChartBasis = "Spanwise loading Cl·c/c̄ vs η; dashed elliptic reference at the same CL"; // COPY-238
    public const string StripHeader = "Strip of wing run (α_eff) · η"; // COPY-239

    public static string Chip(Tier tier) => tier switch
    {
        Tier.Estimator => EstimatorChip,
        Tier.Polar => PolarChip,
        Tier.VlmStrip => VlmChip,
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public const string TamperedNote = "The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run."; // COPY-274
    public const string ChartXTitle = "η (root → tip)"; // COPY-275
    public const string ChartYTitle = "Cl·c/c̄ (–)"; // COPY-276
    public const string ChartLegend = "dashed: elliptic, same CL"; // COPY-277
    public const string ChartSeries = "VLM + strip"; // COPY-278
    public const string HistoricalChip = "Historical · VLM + strip"; // COPY-279

    /// <summary>COPY-280 form: "&lt;speed&gt; &lt;unit&gt; · &lt;water&gt; &lt;temperature&gt; °C · as the band", from the live conditions.</summary>
    public static string ConditionsSummary(double speedMetersPerSecond, WaterRecord water, Units units) =>
        Number(units == Units.Imperial ? speedMetersPerSecond * KnotsPerMeterSecond : speedMetersPerSecond, "0.##") +
        (units == Units.Imperial ? " kn" : " m/s") + " · " + (water.SalinityGPerKg > 0 ? "salt" : "fresh") + " " +
        Number(water.TemperatureC, "0.#") + " °C · as the band";

    public const double KnotsPerMeterSecond = 1.9438444924406;

    public const string WingDragLabel ="Drag (Wing only)"; // COPY-354, Ruling 109
    public const string WingDragNote = "Wing only: induced (VLM + strip) plus profile (polar). Not a total."; // COPY-330
    public const string WingDragNotIncluded = "Not included: junction, mast, wave, spray"; // COPY-356

    /// <summary>The h/c below which the free surface is not modelled (the Depth basis row, COPY-224/226 under Ruling 101 3a).</summary>
    public const double DeepWaterHc = 5;

    /// <summary>
    /// The COPY-70 form, "Unavailable — &lt;reason&gt;" (COPY-250). A reason code with an approved text (Ruling 116, COPY-266, 305..310,
    /// 334..352, 378..383) reads that text whole; any other reason is the caller's own sentence.
    /// </summary>
    public static string UnavailableBecause(string reason) => ReasonTexts.TryGetValue(reason, out string? text) ? text : "Unavailable — " + reason;

    // ---- DX: the Section tab and the polar displays (Rulings 108, 113, 116) ----
    public const string EstimatorLabelDeep = "inviscid + turbulent-friction bound; deep water; steady · inviscid; no boundary layer"; // COPY-293
    public const string EstimatorLabelNoDepth = "inviscid + turbulent-friction bound; free surface not modelled; steady · inviscid; no boundary layer"; // COPY-293 variant, DX row 3
    public const string CpLegend = "Cp · vik pinned at 0"; // COPY-294 (the −a to +b range is appended)
    public const string ClPanel = "cl (panel)"; // COPY-295
    public const string CmQuarter = "Cm c/4"; // COPY-296
    public const string AlphaL0Panel = "α_L0 (panel)"; // COPY-297
    public const string CdBoundLabel = "Cd (turbulent bound)"; // COPY-337
    public const string CdBoundNote = "Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value"; // DX row 7
    public const string MarginLabel = "15 % margin — practitioner assumption, not sourced"; // COPY-300
    public const string CavClear = "Clear — σ is above −Cp_min plus the margin"; // COPY-301
    public const string CavInside = "Inside the margin — σ is above −Cp_min but within the margin"; // COPY-302
    public const string CavPossible = "Possible — σ is at or below −Cp_min; speed is above V_crit"; // COPY-303
    public const string DepthNotSet = "Unavailable — depth not set"; // COPY-45
    public const string CdNonPositive = "Undefined — CD ≤ 0"; // COPY-230
    public const string SigmaLabel = "σ (cavitation number)"; // COPY-359
    public const string CpMinLabel = "−Cp_min"; // COPY-360
    public const string VcritLabel = "V_crit (inception speed)"; // COPY-361
    public const string UnderreadMeasured = "Cp_min under-read, 200 vs 400 panels"; // COPY-311
    public const string Provisional = "Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels)"; // COPY-312
    public const string UnderreadNotMeasured = "Cp_min under-read not measured at this station (200 panels only)"; // COPY-384, Ruling 113
    public const string CpNoProfile = "Unavailable — no accepted section profile at this station"; // COPY-357
    public const string CpSolveFailed = "Unavailable — the panel solve failed at this station. Evaluate again."; // COPY-358
    public const string PolarSurrogate = "surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only"; // COPY-313
    public const string SurrogateAccuracy = "XFOIL-class surrogate; accuracy relative to XFOIL, not experiment"; // COPY-66
    public const string ConfidenceNotRecorded = "analysis_confidence: not recorded"; // spec A5.3
    public const string BracketInside = "Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family)"; // COPY-318
    public const string BracketLabel = "Polar bracket:"; // COPY-326
    public const string OverlayMenu = "Overlay a section ▾"; // COPY-327
    public const string ProfileDragNote = "Profile drag from the polar at α_eff, both Ncrit; band, not a prediction"; // COPY-328
    public const string PolarNotComputed = "Unavailable — not computed"; // tripped surface band (spec A5.3)
    public const string TotalDragMissingWithProfile = "Unavailable — missing: junction, mast, wave, spray"; // COPY-331
    public const string DeltaVsLattice = "Δ vs lattice Cl_local"; // COPY-324
    public const string DeltaVsLatticeNote = "polar cl at α_eff against the lattice, a per-strip consistency check"; // COPY-325
    public const string BucketLegend = "σ required and V_crit against Cl · dot: this operating point"; // COPY-366
    public const string FindTarget = "Target CL", FindBracket = "Bracket", FindIterations = "Iterations", // COPY-367..372
        FindStopped = "Stopped because", FindBasis = "Basis", FindPolarLimit = "Polar limit";
    public const string ClMaxLimit = "attached-flow polar limit, not measured stall; pumping not modelled"; // spec ANA-05
    public static string StationName(double eta, bool governing) => governing // COPY-394, COPY-395 (Ruling 118)
        ? $"Governing cavitation station · η {Number(eta, "0.###")} (no strip selected)"
        : $"Selected strip · η {Number(eta, "0.###")}";
    public const string FindNeedsInput = "Enter a target CL and an ordered α bracket."; // COPY-396 (Ruling 118)
    public const string FindRunning = "Finding…"; // COPY-397 (Ruling 118)
    public static string LowConfidenceStrips(int strips) => $"Low confidence — analysis_confidence below 0.5 at {strips} strips"; // COPY-399 (Ruling 118)

    public static string LowConfidence(double? confidence) => confidence is { } value // COPY-316
        ? $"Low confidence — analysis_confidence {Number(value, "0.00")} is below 0.5. Computed and flagged, never refused; not an error bar."
        : "Low confidence — analysis_confidence is below 0.5. Computed and flagged, never refused; not an error bar.";
    public static string Confidence(double value) => $"analysis_confidence {Number(value, "0.00")} · advisory, not an error bar"; // COPY-315
    public static string CstResidual(double max, double rms) => // COPY-317
        $"CST fit residual: max {Sci(max)} c · RMS {Sci(rms)} c (shape residual, not an aerodynamic error)";
    public static string StationCavitationLine(double eta, double depth, int count) => // COPY-304
        $"Governing station: η {Number(eta, "0.###")} · depth {Number(depth, "0.###")} m · smallest σ / (−Cp_min) of {count} stations";
    public static string CpMinWhere(double x, string side, int stations) => // DX row 8
        $"at x/c {Number(x, "0.###")} on the {side} surface · {stations} stations · three trailing-edge panels per side excluded";
    public static string ReInside(double re, double min, double max) => $"Re_local {Sci(re)} inside {Sci(min)} to {Sci(max)}"; // COPY-322
    public static string ReOutside(double re, double min, double max) => // COPY-323
        $"Re_local {Sci(re)} outside the polar’s Re range {Sci(min)} to {Sci(max)} — cd not extrapolated";
    public static string CdMissingAt(int strips) => $"Unavailable — cd missing at {strips} strips; the estimator bound is not substituted"; // COPY-329
    public static string FoundAlpha(double alpha, double target) => $"α {Number(alpha, "0.00")}° meets CL {Number(target, "0.###")} within 1 %"; // COPY-332
    public static string NoAlpha(string reason) => $"Find α found no α — {reason}. Nothing was extrapolated."; // COPY-333
    public static string TransitionLegend(string revision, double re) => $"Transition x_tr/c, upper and lower · Ncrit 2 and 4 · {revision} · Re {Sci(re)}"; // COPY-365
    public static string BracketOutsideAlpha(double alpha) => $"Outside the validated bracket — α {Number(alpha, "0.00")}° is beyond ±6°. Computed, not validated."; // COPY-319
    public static string BracketOutsideRe(double re) => $"Outside the validated bracket — Re {Sci(re)} is beyond 2 × 10⁵ to 10⁶. Computed, not validated."; // COPY-362
    public static string BracketOutsideNcrit(double ncrit) => $"Outside the validated bracket — Ncrit {Number(ncrit, "0.##")} is beyond 2 to 9. Computed, not validated."; // COPY-363
    public const string BracketOutsideFamily = "Outside the validated bracket — the surrogate is validated on NACA 0012 only. Computed, not validated."; // COPY-364
    public static string TrainingAlpha(double alpha) => $"Unavailable — α {Number(alpha, "0.00")}° is outside the surrogate’s training range (−27.9° to 28.6°)"; // COPY-320
    public static string TrainingRe(double re) => $"Unavailable — Re {Sci(re)} is outside the surrogate’s training range (1 × 10² to 1 × 10¹⁰)"; // COPY-348
    public static string TrainingNcrit(double ncrit) => $"Unavailable — Ncrit {Number(ncrit, "0.##")} is outside the surrogate’s training range (0 to 18)"; // COPY-349
    public static string CstOverLimit(double residual) => $"Unavailable — section fit residual {Sci(residual)} c exceeds the limit 3.6 × 10⁻⁴ c"; // COPY-321

    /// <summary>The five Find α no-root reasons (COPY-373..377), keyed by the termination or source code that names them.</summary>
    public static string FindReason(string code, double? hOverC = null, int? iterations = null) => code switch
    {
        "ANA-FIND-MAX-ITERATIONS" => $"the search stopped after {iterations ?? 0} iterations without converging", // COPY-398 (Ruling 118)
        "ANA-FIND-NO-SIGN-CHANGE" => "CL never reaches the target in the bracket", // COPY-373
        "ANA-POLAR-NOT-CONVERGED" => "the polar did not converge", // COPY-374
        "ANA-POLAR-LOW-CONFIDENCE" => "polar confidence is below the floor", // COPY-375
        "ANA-POLAR-RE-OUTSIDE" => "Re is outside the polar's range", // COPY-376
        "ANA-FIND-DEPTH-BELOW-FLOOR" => $"the foil is too shallow (h/c below {Number(hOverC ?? DeepWaterHc, "0.##")})", // COPY-377
        _ => code
    };

    /// <summary>Compact scientific form used by the approved strings: 1.09 × 10⁻⁴.</summary>
    public static string Sci(double value)
    {
        if (value == 0 || !double.IsFinite(value)) return Number(value, "0.##");
        int power = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        double mantissa = value / Math.Pow(10, power);
        const string digits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
        string exponent = string.Concat(Math.Abs(power).ToString(CultureInfo.InvariantCulture).Select(c => digits[c - '0']));
        return Number(mantissa, "0.00") + " × 10" + (power < 0 ? "⁻" : "") + exponent;
    }

    private const string ProgramFault = "Unavailable — the polar could not be computed for this section. This is a program fault; the run is kept."; // COPY-352

    /// <summary>
    /// Every reason code that can reach a cell, with its approved whole text (Rulings 108 and 116). This is the one lookup:
    /// approval of a changed text is a change to this table and to nothing else.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ReasonTexts { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ANA-SPEED-NOT-POSITIVE"] = "Undefined — speed ≤ 0", // COPY-266
        ["ANA-CAV-PV-MISSING"] = "Unavailable — vapour pressure missing", // COPY-305
        ["ANA-CAV-DEPTH-NOT-SET"] = DepthNotSet, // COPY-45
        ["ANA-CAV-SURFACE-PIERCING"] = "Unavailable — local station is surface piercing", // COPY-306
        ["ANA-CAV-WATER-INVALID"] = "Unavailable — water is invalid", // COPY-307
        ["ANA-CAV-DEPTH-INVALID"] = "Unavailable — local depth is invalid", // COPY-308
        ["ANA-CAV-NO-SUCTION"] = "Undefined — −Cp_min ≤ 0", // COPY-309
        ["ANA-CAV-PRESSURE-NONPOSITIVE"] = "Undefined — static pressure does not exceed vapour pressure", // COPY-310
        ["ANA-DRAG-UNAVAILABLE"] = "Unavailable — drag could not be computed. Evaluate again.", // COPY-334
        ["ANA-WING-RATIO-UNAVAILABLE"] = "Unavailable — wing drag is missing or zero, so CL/CD can't be formed.", // COPY-335
        ["ANA-WING-ONLY-RATIO"] = "Wing only: lift over wing drag. Not a craft CL/CD.", // COPY-336
        ["ANA-SECTION-ITTC1957-BOUND"] = CdBoundLabel, // COPY-337
        ["ANA-TOTAL-DRAG-MISSING-INDUCED"] = "Unavailable — induced drag is missing.", // COPY-338
        ["ANA-TOTAL-DRAG-MISSING-PROFILE"] = "Unavailable — missing: profile, junction, mast, wave, spray", // COPY-339
        ["ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY"] = TotalDragMissingWithProfile, // COPY-331
        ["ANA-PROFILE-DRAG-MISSING-STRIPS"] = "Unavailable — the run has no strips.", // COPY-340
        ["ANA-INDUCED-DRAG-MISSING-STRIPS"] = "Unavailable — the run has no strips.", // COPY-340
        ["ANA-PROFILE-DRAG-MISSING-WIDTH"] = "Unavailable — a strip width is not recorded.", // COPY-341
        ["ANA-INDUCED-DRAG-MISSING-WIDTH"] = "Unavailable — a strip width is not recorded.", // COPY-341
        ["ANA-ROOT-MOMENT-MISSING-WIDTH"] = "Unavailable — a strip width is not recorded.", // COPY-341, Ruling 116 (4)
        ["ANA-PROFILE-DRAG-NONFINITE"] = "Unavailable — a drag sum is not a finite number. Evaluate again.", // COPY-342
        ["ANA-INDUCED-DRAG-NONFINITE"] = "Unavailable — a drag sum is not a finite number. Evaluate again.", // COPY-342
        ["ANA-POLAR-PROFILE-MISSING"] = "Unavailable — no section profile for this strip.", // COPY-343
        ["ANA-POLAR-UNAVAILABLE"] = "Unavailable — the polar gave no result for this strip.", // COPY-344
        ["ANA-POLAR-CD-UNAVAILABLE"] = "Unavailable — the polar gave no drag value at this strip.", // COPY-345
        ["ANA-POLAR-METHOD-MISMATCH"] = "Unavailable — the stored polar was made with another method or profile. Evaluate to compute a new run.", // COPY-346
        ["ANA-POLAR-REVISION-MISSING"] = "Unavailable — the section revision for this polar is not held by this session.", // COPY-347
        ["ANA-POLAR-REVISION-MISMATCH"] = "Unavailable — the section revision for this polar is not held by this session.", // COPY-347
        ["ANA-POLAR-PROFILE-HASH"] = "Unavailable — the section revision for this polar is not held by this session.", // COPY-347
        ["ANA-POLAR-SECTION-UNVALIDATED"] = "Unavailable — this section family is not covered by the surrogate.", // COPY-350
        ["ANA-POLAR-NOT-CONVERGED"] = "Unavailable — the polar did not converge at this strip.", // COPY-351
        ["ANA-POLAR-NONFINITE"] = ProgramFault, ["ANA-POLAR-INPUT"] = ProgramFault, ["ANA-POLAR-CST-INPUT"] = ProgramFault, // COPY-352
        ["ANA-POLAR-CST-FIT"] = ProgramFault, ["ANA-POLAR-WEIGHTS-MISSING"] = ProgramFault, ["ANA-POLAR-WEIGHTS-HASH"] = ProgramFault,
        ["ANA-POLAR-WEIGHTS-FORMAT"] = ProgramFault,
        ["ANA-POLAR-NONCOMPUTABLE"] = "Unavailable — section fit residual exceeds the limit 3.6 × 10⁻⁴ c", // COPY-321 without its fixture value
        ["ANA-POLAR-ALPHA-OUTSIDE"] = "Unavailable — α is outside the surrogate’s training range (−27.9° to 28.6°)", // COPY-320 without its fixture value
        ["ANA-POLAR-RE-OUTSIDE"] = "Unavailable — Re is outside the surrogate’s training range (1 × 10² to 1 × 10¹⁰)", // COPY-348 without its fixture value
        ["ANA-POLAR-NCRIT-OUTSIDE"] = "Unavailable — Ncrit is outside the surrogate’s training range (0 to 18)", // COPY-349 without its fixture value
        ["ANA-POLAR-VALUE-MISSING"] = "Unavailable — the polar gave no value for this row.", // COPY-382
        ["ANA-OSWALD-UNDEFINED"] = "Unavailable — e is undefined when CL or induced drag is zero.", // COPY-378
        ["ANA-CENTRE-OF-LIFT-UNDEFINED"] = "Unavailable — no centre of lift when total lift is zero.", // COPY-379
        ["ANA-REFERENCE-AREA-MISSING"] = "Unavailable — reference area is zero or missing.", // COPY-380
        ["ANA-INDUCED-DRAG-MISSING-AREA"] = "Unavailable — reference area is zero or missing.", // COPY-380, the same cause
        ["ANA-FORCE-NOT-FINITE"] = "Unavailable — a force is not a finite number. Evaluate again.", // COPY-381
        ["ANA-VERDICT-MISSING"] = "Unavailable — no verdict was stored for this run. Evaluate again.", // COPY-383
        ["ANA-POLAR-LOW-CONFIDENCE"] = "Low confidence — analysis_confidence is below 0.5. Computed and flagged, never refused; not an error bar." // COPY-316
    };

    /// <summary>COPY-223 only when depth is set and no station is shallower than h/c 5; otherwise COPY-224.</summary>
    public static string FixedVlm(bool deepWater) => deepWater ? FixedVlmDeep : FixedVlmNoDepth;
    /// <summary>COPY-225 only when depth is set and no station is shallower than h/c 5; otherwise COPY-226.</summary>
    public static string NotModelled(bool deepWater) => deepWater ? NotModelledSet : NotModelledUnset;

    public static bool DefaultLattice(RunSettings settings) => settings.NSpanPerHalf == 64 && settings.NChord == 4
        && settings.SpanSpacing == "cosine" && settings.ChordSpacing == "cosine";

    public static string LatticeClaim(RunSettings settings) => DefaultLattice(settings) ? VerifiedLattice : OutsideLattice;

    public static string Verdict(StripLoad strip, double alphaL0Deg, double sweepDeg)
    {
        if (strip.Provisional) return TipNotJudged;
        StripVerdict verdict = MethodRecord.JudgeStrip(strip.AlphaEff, alphaL0Deg, strip.ClLocal, sweepDeg);
        return verdict.Text;
    }

    public static string Number(double value, string format = "0.###") => value.ToString(format, CultureInfo.InvariantCulture);
}
