namespace CfdWorkbench.Core;

// Projection types from docs/design/m12b-points.md §5.1. Methods throw until the grammar change lands.
public enum PointRole { RootEnd, RootHandle, Control, AnchorHandle, Anchor, TipHandle, TipEnd }
public enum TangentKind { Corner, Smooth, Symmetric }
public enum PointFreedom { Fixed, SpanOnly, AftOnly, Free }

public sealed record PointView(string Curve, string Id, int Index, double Eta, double SpanMeters, double AftMeters,
    PointRole Role, string? AnchorId, TangentKind? Kind, PointFreedom Freedom, IReadOnlyList<string> Locks);
public sealed record PlanSample(double SpanMeters, double AftMeters);
public sealed record CombTooth(double SpanMeters, double AftMeters, double NormalSpan, double NormalAft,
    double Curvature, bool BreakBefore);
public sealed record CurveView(string Curve, int Ceiling, IReadOnlyList<double> Knots,
    IReadOnlyList<PointView> Points, IReadOnlyList<PlanSample> Samples);
public sealed record PlanformView(string SourceHash, string Basis, long Generation, string Version,
    double HalfSpanMeters, CurveView Leading, CurveView Trailing, IReadOnlyList<AuthoredAssignment> Stations);
public sealed record ProbeReading(double Eta, double SpanMeters, double LeadingAftMeters, double TrailingAftMeters,
    double ChordMeters);

public static class Planform
{
    public static PlanformView View(byte[] source, string basis, long generation) => throw new NotImplementedException();
    public static IReadOnlyList<CombTooth> Comb(CurveView curve) => throw new NotImplementedException();
    public static ProbeReading Probe(PlanformView view, double eta) => throw new NotImplementedException();
    public static (double SpanMeters, double AftMeters) HandleTarget(PlanformView view, string curve, string handleId, double angleDegrees, double lengthMeters) => throw new NotImplementedException();
}
