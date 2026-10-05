using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>S_ref, b_ref and c_ref from the accepted planform, read from Core's wing estimates (design §5.2, P-4).</summary>
public static class ReferenceQuantities
{
    public const string MomentDatum = "frame origin";
    public const string Axes = "body; wind for lift and drag";

    public static RunReference From(byte[] source)
    {
        WingEstimates estimates = WingEstimates.From(source, "accepted", 0);
        return new RunReference(estimates.AreaSquareMeters, estimates.SpanMeters, estimates.MeanChordMeters, MomentDatum, Axes);
    }
}
