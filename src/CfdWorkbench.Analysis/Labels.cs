using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Analysis copy and fixed tier labels. COPY-206…239 are proposed in DESIGN.md §7.</summary>
public static class Labels
{
    public const string NoResult = "No analysis yet. Set the conditions, then Evaluate."; // COPY-206
    public const string NoPolar = "Unavailable — no polar method installed"; // COPY-210
    public const string PayloadFailed = "Unavailable — run payload failed its check"; // COPY-211
    public const string SectionCp = "Unavailable — no section Cp method (DR-ANA-2)"; // COPY-212
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

    public static string FixedVlm(bool depthSet) => depthSet ? FixedVlmDeep : FixedVlmNoDepth;
    public static string NotModelled(bool depthSet) => depthSet ? NotModelledSet : NotModelledUnset;

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
