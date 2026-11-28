using System.Globalization;
using CfdWorkbench.Analysis;

namespace CfdWorkbench.Desktop;

/// <summary>
/// Approved Wave 13A Rounded &amp; Radiused Wing Tips UI copy (<c>DESIGN.md</c> <c>COPY-935..942</c>,
/// operator-approved 2026-11-27, <c>LAB-01</c> clean).
/// </summary>
public static class TipCopy
{
    public const string GroupHeading = "Tip";                                                                // COPY-935
    public const string LeTangentLabel = "LE tip tangent";
    public const string TeTangentLabel = "TE tip tangent";
    public const string TangentFree = "Free";                                                                // COPY-936
    public const string TangentVertical = "Vertical (G1)";                                                   // COPY-936
    public const string LeRadiusLabel = "LE tip radius";                                                     // COPY-937
    public const string TeRadiusLabel = "TE tip radius";                                                     // COPY-937
    public const string VerticalHandleRequired =                                                             // COPY-939
        "Vertical tip tangent requires a nonzero handle length along the tip edge.";
    public const string CapLabel = "3D tip cap";                                                             // COPY-940
    public const string CapFlat = "Flat (open)";                                                             // COPY-940
    public const string CapRounded = "Rounded";                                                              // COPY-940
    public const string CapRatioLabel = "Cap ratio";                                                         // COPY-940
    public const string CapRatioHelp = "1.00 = semicircular";                                                // COPY-940
    public const string OverallSpanLabel = "Overall span (with cap)";
    public const string StraightRadius = "∞ / straight";
    public const string VerticalTipHandleNote =
        "Vertical tip tangent (G1): handle moves fore and aft along the tip edge only.";

    /// <summary>COPY-941 single source of truth shared with Area 3 <see cref="Labels.RoundedTipCapNotice"/>.</summary>
    public static string AnalysisCapNotModelled => Labels.RoundedTipCapNotice;                               // COPY-941

    /// <summary>COPY-938: <c>DSL-TIP-RADIUS-BOUNDS</c> refusal copy with exact max feasible radius.</summary>
    public static string RadiusOutOfBounds(double requestedMm, double maxMm) =>                              // COPY-938
        string.Create(CultureInfo.InvariantCulture,
            $"Tip radius {requestedMm:0.0} mm exceeds feasible envelope (max {maxMm:0.00} mm at current tip chord and span). Use {maxMm:0.00} mm");

    /// <summary>COPY-942: Dual span disclosure shown when <c>tip rounded</c> is active.</summary>
    public static string DualSpanDisclosure(double projectedSpanMm, double overallSpanMm) =>                 // COPY-942
        string.Create(CultureInfo.InvariantCulture,
            $"Projected loft span: {projectedSpanMm:0.0} mm · Overall span (with cap): {overallSpanMm:0.0} mm");
}
