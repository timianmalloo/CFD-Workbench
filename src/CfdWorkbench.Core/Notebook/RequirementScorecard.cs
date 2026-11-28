using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core.Notebook;

public sealed record ScorecardClauseResult(
    string ClauseId,
    string Label,
    string TargetDisplay,
    string ActualDisplay,
    string Status,
    string MethodTier,
    string? Notice = null);

public sealed record CandidateComparisonDelta(
    string BaselineId,
    string CandidateId,
    double DeltaSpanMm,
    double DeltaAreaCm2,
    double? DeltaLdPercent,
    double DeltaRollInertiaPercent,
    double RollInertiaRatio,
    double? DeltaTakeoffKn);

public sealed record RequirementScorecardResult(
    string ReqVersionId,
    string CandidateId,
    bool IsStale,
    double AspectRatio,
    double SpanMm,
    double AreaCm2,
    double RootChordMm,
    double MacMm,
    IReadOnlyList<ScorecardClauseResult> Clauses,
    IReadOnlyList<string> MandatoryNotices,
    CandidateComparisonDelta? Comparison);

/// <summary>
/// Derived pure C# Requirement Scorecard &amp; Tier-0 canonical <c>solver_context</c> formatter
/// (<c>docs/design/ai-guided-design-notebook.md §3.2</c>, DM7, AI-03, ANA-19, A5.1/A5.3/A5.4/ANA-05).
/// Never stored; recomputed deterministically from <c>(FoilReqDocument, CandidateIterationRecord, baseline?)</c>.
/// </summary>
public static class RequirementScorecard
{
    public const string DepthUnsetNotice = "Unavailable — depth not set";
    public const string SurrogateNotice = "XFOIL-class surrogate; accuracy relative to XFOIL, not experiment.";
    public const string AttachedFlowNotice = "Attached-flow polar limit, not measured stall; pumping not modelled.";
    public const string VentilationNotice = "Cavitation screening (sheet, by -Cp_min); ventilation not modelled.";
    public const string WingOnlyNotice = "Wing-only L/D (fuselage, mast, and stabilizer unmodelled).";

    public static RequirementScorecardResult Evaluate(
        FoilReqDocument req,
        string reqVersionId,
        CandidateIterationRecord candidate,
        CandidateIterationRecord? baseline = null,
        string? latestReqVersionId = null)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(candidate);

        var d = candidate.Delta;
        double spanMm = d.SpanMeters * 1000.0;
        double areaCm2 = d.AreaSquareMeters * 1e4;
        double ar = d.AspectRatio;
        double rootChordMm = d.RootChordMeters * 1000.0;
        double macMm = d.MacMeters * 1000.0;

        bool isStale = !string.IsNullOrWhiteSpace(latestReqVersionId) &&
                       !string.Equals(candidate.ReqVersionId, latestReqVersionId, StringComparison.Ordinal);

        var clauses = new List<ScorecardClauseResult>();
        var notices = new List<string> { WingOnlyNotice, SurrogateNotice, AttachedFlowNotice, VentilationNotice };

        // 1. Max span clause
        var maxSpan = req.FindConstraint("max_span");
        if (maxSpan is { Quantity: { } maxSpanQty } && maxSpanQty.TryToMeters(out double maxSpanM))
        {
            double maxSpanMm = maxSpanM * 1000.0;
            string status = spanMm <= maxSpanMm + 1e-6 ? "Pass" : "Violated";
            clauses.Add(new ScorecardClauseResult(
                "envelope.max_span",
                "Max span",
                $"<= {Fmt(maxSpanMm, "0.0")} mm",
                $"{Fmt(spanMm, "0.0")} mm",
                status,
                "Tier 0 Geometry"));
        }

        // 2. Aspect ratio / Area clause
        var targetAr = req.FindConstraint("target_ar");
        if (targetAr is { Quantity: { } arQ })
        {
            double relDiff = Math.Abs(ar - arQ.Value) / Math.Max(1e-6, arQ.Value);
            string status = relDiff <= 0.08 ? "Pass" : "Tradeoff";
            clauses.Add(new ScorecardClauseResult(
                "envelope.target_ar",
                "Aspect ratio",
                $"~ {Fmt(arQ.Value, "0.00")}",
                Fmt(ar, "0.00"),
                status,
                "Tier 0 Geometry"));
        }

        // 3. Takeoff operating point
        var takeoffOp = candidate.OperatingPoints.FirstOrDefault(o => string.Equals(o.OpId, "takeoff", StringComparison.OrdinalIgnoreCase))
                        ?? candidate.OperatingPoints.FirstOrDefault();
        if (takeoffOp is not null)
        {
            string status = takeoffOp.UnavailableReason is not null
                ? "Unavailable"
                : takeoffOp.Cl <= FoilReqValidator.PhysicsTensionClLimit ? "Pass" : "Tradeoff";
            clauses.Add(new ScorecardClauseResult(
                "op.takeoff",
                "Takeoff lift",
                $"CL <= {Fmt(FoilReqValidator.PhysicsTensionClLimit, "0.00")}",
                takeoffOp.UnavailableReason ?? $"CL {Fmt(takeoffOp.Cl, "0.000")} at {Fmt(takeoffOp.SpeedKn, "0.0")} kn (Re_MAC {FmtSci(takeoffOp.ReMac)})",
                status,
                takeoffOp.MethodVersion,
                AttachedFlowNotice));
        }

        // 4. Glide / bottom turn wing-only L/D
        var glideOp = candidate.OperatingPoints.FirstOrDefault(o => string.Equals(o.OpId, "glide", StringComparison.OrdinalIgnoreCase))
                      ?? (candidate.OperatingPoints.Count > 1 ? candidate.OperatingPoints[1] : takeoffOp);
        if (glideOp is not null)
        {
            string actual = glideOp.WingOnlyLd is { } ld
                ? $"Wing-only L/D {Fmt(ld, "0.0")} at {Fmt(glideOp.SpeedKn, "0.0")} kn"
                : glideOp.UnavailableReason ?? "Unavailable";
            clauses.Add(new ScorecardClauseResult(
                "op.glide",
                "Wing-only L/D (glide)",
                "Maximize glide",
                actual,
                glideOp.WingOnlyLd.HasValue ? "Pass" : "Unavailable",
                glideOp.MethodVersion,
                WingOnlyNotice));
        }

        // 5. Steep-drop cavitation screening & free-surface submergence (ANA-19 + A5.1)
        var dropOp = candidate.OperatingPoints.FirstOrDefault(o => string.Equals(o.OpId, "steep_drop", StringComparison.OrdinalIgnoreCase))
                     ?? candidate.OperatingPoints.LastOrDefault();
        if (dropOp is not null)
        {
            bool depthUnset = req.Water.IsDepthUnset || dropOp.DepthUnset;
            if (depthUnset)
            {
                clauses.Add(new ScorecardClauseResult(
                    "op.cavitation",
                    "Cavitation margin (steep drop)",
                    "sigma / (-Cp_min) >= 1.15",
                    DepthUnsetNotice,
                    "Unavailable",
                    "PanelMethod 200p/400p",
                    DepthUnsetNotice));
                clauses.Add(new ScorecardClauseResult(
                    "op.free_surface",
                    "Free-surface submergence (h/c_root)",
                    "h/c_root >= 5.0",
                    DepthUnsetNotice,
                    "Unavailable",
                    "Tier 0 Geometry",
                    DepthUnsetNotice));
            }
            else
            {
                if (dropOp.CavMargin is { } margin && dropOp.Sigma is { } sigma && dropOp.CpMin is { } cpMin)
                {
                    string status = margin >= 1.15 ? "Pass" : margin >= 1.0 ? "Tradeoff" : "Violated";
                    string vCritPart = dropOp.VCritKn is { } vc ? $", V_crit {Fmt(vc, "0.0")} kn" : "";
                    clauses.Add(new ScorecardClauseResult(
                        "op.cavitation",
                        "Cavitation margin (steep drop)",
                        "sigma / (-Cp_min) >= 1.15",
                        $"sigma {Fmt(sigma, "0.000")}, -Cp_min {Fmt(-cpMin, "0.00")}, margin {Fmt(margin, "0.00")}{vCritPart}",
                        status,
                        "PanelMethod 200p/400p",
                        VentilationNotice));
                }
                else
                {
                    clauses.Add(new ScorecardClauseResult(
                        "op.cavitation",
                        "Cavitation margin (steep drop)",
                        "sigma / (-Cp_min) >= 1.15",
                        dropOp.UnavailableReason ?? "Unavailable",
                        "Unavailable",
                        "PanelMethod 200p/400p"));
                }

                if (dropOp.HOverCRoot is { } hc && dropOp.FrH is { } frh)
                {
                    bool shallow = hc < 5.0;
                    string? a51 = shallow
                        ? $"A5.1 free-surface notice: h/c_root = {Fmt(hc, "0.00")} < 5 (Fr_h = {Fmt(frh, "0.00")}); free-surface effects are unmodelled in this deep-water local calculation."
                        : null;
                    if (a51 is not null) notices.Add(a51);
                    clauses.Add(new ScorecardClauseResult(
                        "op.free_surface",
                        "Free-surface submergence (h/c_root)",
                        "h/c_root >= 5.0",
                        $"h/c_root {Fmt(hc, "0.00")}, Fr_h {Fmt(frh, "0.00")}",
                        shallow ? "Flagged" : "Pass",
                        "Tier 0 Geometry",
                        a51));
                }
            }
        }

        // 6. Any skipped/open grill questions show visibly as Flagged on Scorecard.
        foreach (var oq in req.OpenQuestions.Where(q => q.State is "@open" or "@skipped"))
        {
            clauses.Add(new ScorecardClauseResult(
                $"open_questions.{oq.Id}",
                $"Open assumption ({oq.Id})",
                oq.Prompt,
                oq.State == "@skipped" ? "Skipped — Flagged assumption" : "Open — Flagged",
                "Flagged",
                "GrillTree"));
        }

        // 7. Precomputed Tier-0 comparison deltas when baseline is provided.
        CandidateComparisonDelta? comparison = null;
        if (baseline is not null)
        {
            var b = baseline.Delta;
            double dSpanMm = (d.SpanMeters - b.SpanMeters) * 1000.0;
            double dAreaCm2 = (d.AreaSquareMeters - b.AreaSquareMeters) * 1e4;
            double rollRatio = ComputeGeometricRollInertiaRatio(d, b);
            double dRollPct = (rollRatio - 1.0) * 100.0;

            var baseGlide = baseline.OperatingPoints.FirstOrDefault(o => string.Equals(o.OpId, "glide", StringComparison.OrdinalIgnoreCase));
            double? dLdPct = (glideOp?.WingOnlyLd is { } curLd && baseGlide?.WingOnlyLd is { } prevLd && prevLd > 1e-6)
                ? ((curLd - prevLd) / prevLd) * 100.0
                : null;

            var baseTo = baseline.OperatingPoints.FirstOrDefault(o => string.Equals(o.OpId, "takeoff", StringComparison.OrdinalIgnoreCase));
            double? dToKn = (takeoffOp is not null && baseTo is not null)
                ? takeoffOp.SpeedKn - baseTo.SpeedKn
                : null;

            comparison = new CandidateComparisonDelta(
                BaselineId: baseline.CandidateId,
                CandidateId: candidate.CandidateId,
                DeltaSpanMm: dSpanMm,
                DeltaAreaCm2: dAreaCm2,
                DeltaLdPercent: dLdPct,
                DeltaRollInertiaPercent: dRollPct,
                RollInertiaRatio: rollRatio,
                DeltaTakeoffKn: dToKn);
        }

        return new RequirementScorecardResult(
            ReqVersionId: reqVersionId,
            CandidateId: candidate.CandidateId,
            IsStale: isStale,
            AspectRatio: ar,
            SpanMm: spanMm,
            AreaCm2: areaCm2,
            RootChordMm: rootChordMm,
            MacMm: macMm,
            Clauses: clauses,
            MandatoryNotices: notices,
            Comparison: comparison);
    }

    /// <summary>
    /// Formats every derived metric and Tier-0 delta in plain decimal tokens matching
    /// <c>AssistantValidators.NumeralTokenRegex</c> (zero thousands commas, zero Unicode superscripts)
    /// so <c>AI-03</c> numeral grounding verifies grounded translations with zero false positives.
    /// </summary>
    public static string FormatSolverContext(
        RequirementScorecardResult scorecard,
        CandidateIterationRecord candidate,
        CandidateIterationRecord? baseline = null)
    {
        ArgumentNullException.ThrowIfNull(scorecard);
        ArgumentNullException.ThrowIfNull(candidate);

        var sb = new StringBuilder(512);
        sb.Append("candidate=").Append(candidate.CandidateId)
          .Append(" status=").Append(candidate.GeometryStatus)
          .Append(" span=").Append(Fmt(scorecard.SpanMm, "0.0")).Append(" mm")
          .Append(" area=").Append(Fmt(scorecard.AreaCm2, "0.0")).Append(" cm2")
          .Append(" AR=").Append(Fmt(scorecard.AspectRatio, "0.00"))
          .Append(" root_chord=").Append(Fmt(scorecard.RootChordMm, "0.0")).Append(" mm")
          .Append(" MAC=").Append(Fmt(scorecard.MacMm, "0.0")).Append(" mm")
          .Append(" taper=").Append(Fmt(candidate.Delta.Taper, "0.00"))
          .Append(" anhedral=").Append(Fmt(candidate.Delta.AnhedralDeg, "0.0")).Append(" deg")
          .Append(" washout=").Append(Fmt(candidate.Delta.WashoutDeg, "0.0")).Append(" deg")
          .Append(" root_profile=").Append(candidate.Delta.RootProfileId)
          .Append(" tip_profile=").Append(candidate.Delta.TipProfileId)
          .Append('\n');

        foreach (var op in candidate.OperatingPoints)
        {
            sb.Append("op=").Append(op.OpId)
              .Append(" speed=").Append(Fmt(op.SpeedKn, "0.0")).Append(" kn")
              .Append(" alpha=").Append(Fmt(op.AlphaDeg, "0.00")).Append(" deg")
              .Append(" CL=").Append(Fmt(op.Cl, "0.000"))
              .Append(" CD=").Append(Fmt(op.Cd, "0.0000"))
              .Append(" Re_MAC=").Append(FmtSci(op.ReMac));
            if (op.WingOnlyLd is { } ld) sb.Append(" wing_only_LD=").Append(Fmt(ld, "0.0"));
            if (op.Ncrit2Ld is { } n2) sb.Append(" Ncrit2_LD=").Append(Fmt(n2, "0.0"));
            if (op.Ncrit4Ld is { } n4) sb.Append(" Ncrit4_LD=").Append(Fmt(n4, "0.0"));
            if (op.DepthUnset)
            {
                sb.Append(" cavitation=Unavailable_depth_not_set free_surface=Unavailable_depth_not_set");
            }
            else
            {
                if (op.CpMin is { } cp) sb.Append(" neg_Cp_min=").Append(Fmt(-cp, "0.00"));
                if (op.Sigma is { } sig) sb.Append(" sigma=").Append(Fmt(sig, "0.000"));
                if (op.CavMargin is { } cm) sb.Append(" cav_margin=").Append(Fmt(cm, "0.00"));
                if (op.VCritKn is { } vc) sb.Append(" V_crit=").Append(Fmt(vc, "0.0")).Append(" kn");
                if (op.GoverningEta is { } eta) sb.Append(" governing_eta=").Append(Fmt(eta, "0.00"));
                if (op.HOverCRoot is { } hc) sb.Append(" h_over_c_root=").Append(Fmt(hc, "0.00"));
                if (op.FrH is { } fr) sb.Append(" Fr_h=").Append(Fmt(fr, "0.00"));
            }
            sb.Append(" method=").Append(op.MethodVersion).Append('\n');
        }

        if (scorecard.Comparison is { } cmp && baseline is not null)
        {
            sb.Append("delta_vs=").Append(baseline.CandidateId)
              .Append(" delta_span=").Append(FmtSigned(cmp.DeltaSpanMm, "0.0")).Append(" mm")
              .Append(" delta_area=").Append(FmtSigned(cmp.DeltaAreaCm2, "0.0")).Append(" cm2")
              .Append(" roll_inertia_ratio=").Append(Fmt(cmp.RollInertiaRatio, "0.00"))
              .Append(" delta_roll_inertia=").Append(FmtSigned(cmp.DeltaRollInertiaPercent, "0.0")).Append("%");
            if (cmp.DeltaLdPercent is { } dld)
                sb.Append(" delta_wing_only_LD=").Append(FmtSigned(dld, "0.0")).Append("%");
            if (cmp.DeltaTakeoffKn is { } dto)
                sb.Append(" delta_takeoff=").Append(FmtSigned(dto, "0.0")).Append(" kn");
            sb.Append('\n');
        }

        // Standard reference numbers used in mandatory notices (5.0 h/c threshold, 1.15 cavitation margin, 2 and 4 Ncrit).
        sb.Append("thresholds: h_over_c_floor=5.0 cav_margin_floor=1.15 ncrit=2 4\n");
        return sb.ToString();
    }

    // Geometric second moment of area proxy I_xx ~ integral c(y)*y^2 dy ~ S * b^2 * f(taper), pure Tier-0 ratio.
    private static double ComputeGeometricRollInertiaRatio(CandidateIntentDelta current, CandidateIntentDelta baseline)
    {
        double cur = RollInertiaProxy(current);
        double bas = RollInertiaProxy(baseline);
        return bas > 1e-12 ? cur / bas : 1.0;
    }

    private static double RollInertiaProxy(CandidateIntentDelta d)
    {
        // For cubic planform g(eta) = 1 - (1 - lam) * eta^3, integral_0^1 g(eta) * eta^2 d_eta = 1/3 - (1 - lam)/6 = (1 + lam)/6.
        double shapeFactor = (1.0 + d.Taper) / (6.0 * RecipeSeeder.Kappa(d.Taper));
        return d.AreaSquareMeters * d.SpanMeters * d.SpanMeters * shapeFactor;
    }

    private static string Fmt(double v, string fmt) => v.ToString(fmt, CultureInfo.InvariantCulture);

    private static string FmtSigned(double v, string fmt) =>
        (v >= 0.0 ? "+" : "") + v.ToString(fmt, CultureInfo.InvariantCulture);

    private static string FmtSci(double v) =>
        v.ToString("0.00e+0", CultureInfo.InvariantCulture).Replace("e+", "e", StringComparison.Ordinal);
}
