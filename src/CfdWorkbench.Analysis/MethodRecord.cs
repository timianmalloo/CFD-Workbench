using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The input range inside which the method's claims hold (DR-ANA-14; design §5.4).</summary>
public sealed record MethodEnvelope(double AlphaEffFromZeroLiftMaxDeg, double ClLocalMax, double QuarterChordSweepMaxDeg);

/// <summary>A method: its stored identity (id, version, convergence order) and its envelope. VLM owns the values.</summary>
public sealed record MethodRecord(RunMethod Method, MethodEnvelope Envelope)
{
    /// <summary><c>cfdw.vlm-strip</c> 1.0.0, order 1, envelope 10°, Cl 1.0, sweep 30°.</summary>
    public static MethodRecord VlmStrip => throw new NotImplementedException("VLM: MethodRecord.VlmStrip");
}
