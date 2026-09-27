namespace CfdWorkbench.Desktop;

// src/CfdWorkbench.Desktop/Selection.cs — one value, owned by WorkbenchController (ADR-0009 §4)
public abstract record Selection
{
    public sealed record None : Selection;                                   // no foil open
    public sealed record Foil : Selection;                                   // foil open, nothing picked
    public sealed record Station(int Index, double Eta) : Selection;         // an assignment, by index + exact η
    public sealed record Points(IReadOnlyList<PointRef> Items) : Selection;  // 1..n points
}

public sealed record PointRef(string Curve, string VertexId, string? Profile = null);
