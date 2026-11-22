using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

// Area 1 Goal state feasibility evaluator (docs/design/setup-seeding.md §2.4, §4.1, §4.5, GOAL-02, Ruling 272 DR-SET-4).
// Reuses OperatingPoints.Derive for q, Re_ref, h/c, Fr_h, sigma (DM7 — single source of truth).
// Polar-dependent cells honestly report Unavailable when no section polar exists (never 0, never guessed).

public enum FeasibilityCellStatus
{
    Met,
    NotMet,
    Advisory,
    Unavailable,
    Undefined
}

/// <summary>One constraint × operating-point cell in the feasibility matrix.</summary>
public sealed record FeasibilityCell(
    string ConstraintId,
    string ConstraintKind,
    string ConstraintLabel,
    int? Ncrit,
    FeasibilityCellStatus Status,
    string DisplayText,
    double? Value = null);

/// <summary>One operating-point row of the Goal feasibility report, carrying exact derived hydrodynamics and constraint cells.</summary>
public sealed record GoalOperatingPointEvaluation(
    GoalOperatingPoint Point,
    DerivedConditions Derived,
    double? ClRequired,
    bool ShallowWaterNotice,
    IReadOnlyList<FeasibilityCell> Cells);

/// <summary>Full feasibility report evaluated on read from current wing geometry + GoalStateVersion.</summary>
public sealed record FeasibilityReport(
    string GoalStateId,
    string GoalContentHash,
    string TierChip,
    double WingLoadingKPa,
    IReadOnlyList<GoalOperatingPointEvaluation> Points,
    IReadOnlyList<DrcFinding> ClassFindings,
    IReadOnlyList<DrcFinding> SanityFindings);

public static class GoalFeasibility
{
    public const string TierChip = "Goal feasibility · exact hydrodynamics (no polar)";

    /// <summary>
    /// Evaluates exact hydrodynamics (<c>q</c>, <c>CL_req</c>, <c>Re_ref</c>, <c>h/c</c>, <c>Fr_h</c>, <c>σ</c>),
    /// constraint × operating-point cells (for each <c>Ncrit</c>), class-rule DRC, and sanity bounds.
    /// </summary>
    public static FeasibilityReport Evaluate(
        WingEstimates wing,
        GoalStateVersion goal,
        IReadOnlyList<PolarSample>? polars = null,
        IReadOnlyList<DrcExclusion>? exclusions = null,
        string? designId = null)
    {
        ArgumentNullException.ThrowIfNull(wing);
        ArgumentNullException.ThrowIfNull(goal);

        var classFindings = ClassRules.Evaluate(wing, goal, exclusions, designId);
        var sanityFindings = SanityBounds.Evaluate(wing, goal, exclusions, designId);

        double wingLoadingKPa = wing.AreaSquareMeters > 0
            ? (goal.FrontWingLoadN / wing.AreaSquareMeters) / 1000.0
            : double.NaN;

        var rows = new List<GoalOperatingPointEvaluation>(goal.OperatingPoints.Count);
        foreach (var pt in goal.OperatingPoints)
        {
            // Reuse OperatingPoints.Derive (DM7: one formula for q, Re_ref, h/c, Fr_h, sigma).
            var op = new OperatingPoint(pt.SpeedMetersPerSecond, pt.PAtmPa, pt.HRefMeters, pt.Datum, 0.0, pt.RequiredLoadN);
            var derived = OperatingPoints.Derive(op, goal.Water, wing.MeanChordMeters);

            double? clReq = derived.Q.Value is double q && q > 0 && wing.AreaSquareMeters > 0
                ? pt.RequiredLoadN / (q * wing.AreaSquareMeters)
                : null;

            bool shallowNotice = derived.DepthOverChord.Value is double hcVal && hcVal < Labels.DeepWaterHc;

            var cells = new List<FeasibilityCell>();
            foreach (var constraint in goal.Constraints)
            {
                if (!constraint.Enabled) continue;
                switch (constraint.Kind)
                {
                    case "required-load":
                        // Exact CL_req is computed; stall/CL_max check requires section polar (DR-SET-4 (a)).
                        foreach (int ncrit in pt.Ncrit)
                        {
                            var polar = FindPolar(polars, derived.ReRef.Value, ncrit);
                            if (polar?.Cl is not { } clMax)
                            {
                                cells.Add(new FeasibilityCell(
                                    constraint.Id, constraint.Kind, constraint.Label, ncrit,
                                    FeasibilityCellStatus.Unavailable,
                                    Labels.SetupPolarUnavailable,
                                    clReq));
                            }
                            else
                            {
                                bool met = clReq is { } req && clMax >= req;
                                cells.Add(new FeasibilityCell(
                                    constraint.Id, constraint.Kind, constraint.Label, ncrit,
                                    met ? FeasibilityCellStatus.Met : FeasibilityCellStatus.NotMet,
                                    string.Create(CultureInfo.InvariantCulture, $"CL_req {clReq:0.000} vs CL_max {clMax:0.000}"),
                                    clReq));
                            }
                        }
                        break;

                    case "cavitation-margin":
                        double marginRatio = constraint.Bound ?? 1.15;
                        foreach (int ncrit in pt.Ncrit)
                        {
                            if (derived.Sigma.Value is not { } sigma)
                            {
                                cells.Add(new FeasibilityCell(
                                    constraint.Id, constraint.Kind, constraint.Label, ncrit,
                                    derived.Sigma.Reason == DerivedReason.SpeedNotPositive ? FeasibilityCellStatus.Undefined : FeasibilityCellStatus.Unavailable,
                                    derived.Sigma.Reason == DerivedReason.SpeedNotPositive ? "Undefined — speed ≤ 0" : "Unavailable — depth not set (COPY-45)"));
                                continue;
                            }
                            var polar = FindPolar(polars, derived.ReRef.Value, ncrit);
                            if (polar?.CpMin is not { } cpMin || cpMin >= 0)
                            {
                                cells.Add(new FeasibilityCell(
                                    constraint.Id, constraint.Kind, constraint.Label, ncrit,
                                    FeasibilityCellStatus.Unavailable,
                                    Labels.SetupPolarUnavailable,
                                    sigma));
                            }
                            else
                            {
                                double ratio = sigma / (-cpMin);
                                bool met = ratio >= marginRatio;
                                cells.Add(new FeasibilityCell(
                                    constraint.Id, constraint.Kind, constraint.Label, ncrit,
                                    met ? FeasibilityCellStatus.Met : FeasibilityCellStatus.NotMet,
                                    string.Create(CultureInfo.InvariantCulture, $"σ/(−Cp_min) = {ratio:0.00} (bound ≥ {marginRatio:0.00})"),
                                    ratio));
                            }
                        }
                        break;

                    case "polar-re-envelope":
                        double reMin = constraint.BoundMin ?? 200_000.0;
                        double reMax = constraint.BoundMax ?? 2_000_000.0;
                        if (derived.ReRef.Value is not { } re)
                        {
                            cells.Add(new FeasibilityCell(
                                constraint.Id, constraint.Kind, constraint.Label, null,
                                FeasibilityCellStatus.Undefined, "Undefined — speed ≤ 0"));
                        }
                        else
                        {
                            bool inside = re >= reMin && re <= reMax;
                            cells.Add(new FeasibilityCell(
                                constraint.Id, constraint.Kind, constraint.Label, null,
                                inside ? FeasibilityCellStatus.Met : FeasibilityCellStatus.NotMet,
                                string.Create(CultureInfo.InvariantCulture, $"Re {re:0.00e0} ({(inside ? "inside" : "outside")} {reMin:0.0e0}–{reMax:0.0e0})"),
                                re));
                        }
                        break;

                    case "deep-water-floor":
                        double hcFloor = constraint.Bound ?? Labels.DeepWaterHc;
                        if (derived.DepthOverChord.Value is not { } hc)
                        {
                            cells.Add(new FeasibilityCell(
                                constraint.Id, constraint.Kind, constraint.Label, null,
                                FeasibilityCellStatus.Unavailable, "Unavailable — depth not set"));
                        }
                        else
                        {
                            bool deep = hc >= hcFloor;
                            string hcMsg = deep
                                ? string.Create(CultureInfo.InvariantCulture, $"h/c = {hc:0.00} (≥ {hcFloor:0.#})")
                                : string.Create(CultureInfo.InvariantCulture, $"h/c = {hc:0.00} (< {hcFloor:0.#} — free surface not modelled)");
                            cells.Add(new FeasibilityCell(
                                constraint.Id, constraint.Kind, constraint.Label, null,
                                deep ? FeasibilityCellStatus.Met : FeasibilityCellStatus.NotMet,
                                hcMsg,
                                hc));
                        }
                        break;

                    case "class-rule-ref":
                        int violations = classFindings.Count(f => !f.Excluded && f.Severity is DrcSeverity.Error or DrcSeverity.Warning);
                        cells.Add(new FeasibilityCell(
                            constraint.Id, constraint.Kind, constraint.Label, null,
                            violations == 0 ? FeasibilityCellStatus.Met : FeasibilityCellStatus.NotMet,
                            violations == 0 ? "0 class warnings" : $"{violations} class warning(s)",
                            violations));
                        break;

                    case "sanity-bounds":
                        int advisories = sanityFindings.Count(f => !f.Excluded);
                        cells.Add(new FeasibilityCell(
                            constraint.Id, constraint.Kind, constraint.Label, null,
                            advisories == 0 ? FeasibilityCellStatus.Met : FeasibilityCellStatus.Advisory,
                            advisories == 0 ? "Inside catalogued envelope" : $"{advisories} out-of-envelope observation(s)",
                            advisories));
                        break;
                }
            }

            rows.Add(new GoalOperatingPointEvaluation(pt, derived, clReq, shallowNotice, cells));
        }

        string chip = polars is { Count: > 0 }
            ? "Goal feasibility · exact hydrodynamics + section polar"
            : TierChip;

        return new FeasibilityReport(goal.GoalStateId, goal.ContentHash, chip, wingLoadingKPa, rows, classFindings, sanityFindings);
    }

    private static PolarSample? FindPolar(IReadOnlyList<PolarSample>? polars, double? reRef, int ncrit)
    {
        if (polars is null || polars.Count == 0 || reRef is not { } re) return null;
        return polars
            .Where(p => p.Converged && Math.Abs(p.Ncrit - ncrit) < 1e-6)
            .OrderBy(p => Math.Abs(p.Reynolds - re))
            .FirstOrDefault();
    }
}
