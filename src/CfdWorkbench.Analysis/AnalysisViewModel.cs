using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

// The view-model shape TGL, LAY and PNA bind to (design §18.2: "PRJ's view-model shape (PRE)"). PRJ owns this file
// after PRE and fills it; every rendered string comes from AnalysisProjection (design §6.1).

/// <summary>The status-strip states of the selected run (trace row 29).</summary>
public enum RunState
{
    NoResult,
    Running,
    Current,
    Historical,
    Failed,
    Unavailable
}

/// <summary>
/// The display units (ANA-18), Type-1 by decision: never in the run key. assume: a metric/imperial pair covers ANA-18
/// for A3a (Units_Lbf_KeyUnchanged names lbf); confirmed or widened by PRJ against spec ANA-18 before its first row.
/// </summary>
public enum Units
{
    Metric,
    Imperial
}

/// <summary>Everything the Analysis surfaces render for one selected run, already worded.</summary>
public sealed record AnalysisViewModel(RunState State, string StatusText, string? Banner, string? ErrorCard,
    IReadOnlyList<ResultGroup> Groups, IReadOnlyList<LayerData> Layers, string? RunKey)
{
    public IReadOnlyList<LoadingPoint> Loading { get; init; } = [];
    public IReadOnlyList<StripDetail> StripDetails { get; init; } = [];
    /// <summary>A5.2 data beside the deep-water result; null when depth was not supplied.</summary>
    public FreeSurfaceResult? FreeSurface { get; init; }
    /// <summary>Per-strip polar/lattice comparison and measured exemption edge; null without a run polar.</summary>
    public PolarConsistencyResult? PolarConsistency { get; init; }
    public StripValue? WingDragNcrit2 { get; init; }
    public StripValue? WingDragNcrit4 { get; init; }
}

/// <summary>A titled group of rows (Wing result, Loads, Labels, Provenance…).</summary>
public sealed record ResultGroup(string Title, IReadOnlyList<ResultRow> Rows);

/// <summary>One label/value row; <paramref name="Note"/> carries a verdict, omission or Unavailable reason.</summary>
public sealed record ResultRow(string Label, string Value, string? Unit, string? Note);

/// <summary>One view layer (Plan Γ strips, 3D vectors, depth band…), its legend and its table twin's id.</summary>
public sealed record LayerData(string Id, string Title, bool Visible, string Legend, string TwinId)
{
    public IReadOnlyList<LayerSample> Samples { get; init; } = [];
    public string? Note { get; init; }
}

/// <summary>Measured strip load and its body-frame vector; absent values are omitted, never drawn at zero.</summary>
public sealed record LayerSample(double Eta, double Y, double? Value, Loads.Vec? Vector, bool Outside, bool Provisional)
{
    public string? Verdict { get; init; }
    /// <summary>The strip's unit normal in the lattice's frame, derived from the run's own revision; null when it cannot be derived.</summary>
    public Loads.Vec? Normal { get; init; }
    /// <summary>The run's actual strip edges, when present in its lattice record.</summary>
    public double? YLow { get; init; }
    public double? YHigh { get; init; }
    /// <summary>The selected run's placed station height for a depth margin.</summary>
    public double? Elevation { get; init; }
}

/// <summary>The chart and table consume the same spanwise loading values.</summary>
public sealed record LoadingPoint(double Eta, double? ClChordOverMeanChord, double? EllipticReference,
    double AlphaEffDeg, double? LiftPerSpan);

/// <summary>Station selection reads this complete strip result from one projected run.</summary>
public sealed record StripDetail(double Eta, IReadOnlyList<ResultRow> Rows);
