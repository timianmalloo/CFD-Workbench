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
    public const string VerifiedLattice = "Verified fixture family: 64 × 4 cosine/cosine lattice"; // COPY-217, operator proposal
    public const string Provisional = "provisional — tip law cannot judge this strip (ANA-TIP-PROVISIONAL)"; // COPY-218, operator proposal
    public const string AtBound = "at the bound (+-U)"; // COPY-219, operator proposal
    public const string Indeterminate = "indeterminate — the tip law cannot judge this strip"; // COPY-220, operator proposal
    public const string Inside = "inside the method envelope"; // COPY-221
    public const string Outside = "outside the method envelope"; // COPY-222
    public const string FixedVlmDeep = "attached flow; no stall; no ventilation; deep water"; // COPY-223
    public const string FixedVlmNoDepth = "attached flow; no stall; no ventilation; free surface not modelled"; // COPY-224
    public const string NotModelledSet = "Not modelled: ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”."; // COPY-225
    public const string NotModelledUnset = "Not modelled: free surface, ventilation, junctions, unsteady, tip-vortex cavitation, surface state; separation only as “Section-based inference”."; // COPY-226
    public const string StructuralList = "Not assessed: take-off, pumping, breach and slam, ventilation shock, impact, fatigue."; // COPY-227
    public const string TipDepthMissing = "Unavailable — station depth not recorded"; // COPY-228
    public const string ThicknessMissing = "Unavailable — root thickness not recorded"; // COPY-229
    public const string ClCdUndefined = "Undefined — CD ≤ 0"; // COPY-230
    public const string NearFieldFlag = "Near-field diagnostics flagged outside the verified lattice family"; // COPY-231
    public const string EAdvisory = "e outside 0.85–1.00 — lattice effect; result remains available"; // COPY-232
    public const string EAboveOne = "e above 1 at 64 × 4 — lattice effect"; // COPY-233
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

    public static string Verdict(StripLoad strip, double alphaL0Deg = 0, double sweepDeg = 0)
    {
        if (strip.Provisional) return Provisional;
        StripVerdict verdict = MethodRecord.JudgeStrip(strip.AlphaEff, alphaL0Deg, strip.ClLocal, sweepDeg);
        return verdict.Text;
    }

    public static string Number(double value, string format = "0.###") => value.ToString(format, CultureInfo.InvariantCulture);
}
