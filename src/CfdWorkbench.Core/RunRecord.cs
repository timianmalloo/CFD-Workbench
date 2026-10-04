namespace CfdWorkbench.Core;

// The stored Analysis run row (ADR-0011; design area3-analysis.md §3.3, §3.6, §5.6). Core holds it because NativeProject
// is in Core and Core cannot reference CfdWorkbench.Analysis (G-T8, P-3): one definition of the row, its canonical form,
// its run key and its content hash. PRE lands the shape; STO owns the bodies (seam S-A3). VLM, STP, SVC and PRJ read
// these records and never add members: a missing field is a seam request to STO.

/// <summary>The optional top-level <c>analysis</c> member of a <c>cfdw-project-2</c> document. Append-only facts.</summary>
public sealed record AnalysisRecords(IReadOnlyList<AnalysisRun> Runs, IReadOnlyList<PolarSample> PolarSamples,
    IReadOnlyList<PrunedRun> Pruned);

/// <summary>
/// One evaluation attempt of one Surface revision (with its Profile revisions) at one operating point by one method
/// id + version under one settings hash, recorded when the method returns (AM-1.7-19). Immutable once recorded.
/// <paramref name="Tier"/> is the tier id (<c>vlm-strip</c>). <paramref name="RunKey"/> and <paramref name="ContentHash"/>
/// are stored but never trusted on read: both are recomputed (ADR-0011 §4).
/// </summary>
public sealed record AnalysisRun(string RunId, string RunKey, string ContentHash, RunOutcome Outcome, string Tier,
    RunMethod Method, RunSettings Settings, string SettingsHash, RunInputs Inputs, WaterRecord Water, OperatingPoint Op,
    RunReference Reference, double ReconciliationTolerance, RunDiagnostics Diagnostics, IReadOnlyList<StripLoad> Strips,
    double WallMs, RunPlatform Platform);

/// <summary>Completed, or Failed with a stable code and a reason. Cancelled is telemetry, never a row.</summary>
public abstract record RunOutcome
{
    private RunOutcome() { }
    public sealed record Completed : RunOutcome;
    public sealed record Failed(string Code, string Reason) : RunOutcome;
}

/// <summary>The method id, its version and its observed convergence order (VLM + strip: <c>cfdw.vlm-strip</c>, p = 1).</summary>
public sealed record RunMethod(string Id, string Version, int Order);

/// <summary>Every numeric choice of a run; all of it is in the run key through <see cref="AnalysisRun.SettingsHash"/>.</summary>
public sealed record RunSettings(int NSpanPerHalf, int NChord, string SpanSpacing, string ChordSpacing, int WakeSpans,
    string WakeDirection, double SingularityCutoff, string Envelope, RunPolar? Polar, IReadOnlyList<int> Ncrit,
    string SurfaceState, double TeFloorMm);

/// <summary>The polar method a run used, or none (A3a: none — DR-ANA-1 (a)).</summary>
public sealed record RunPolar(string Id, string Version, string Model);

/// <summary>What the run read: the accepted revision, never a draft (design §4 item 1).</summary>
public sealed record RunInputs(string AcceptedId, string SurfaceHash, IReadOnlyList<string> ProfileHashes, string Evaluator,
    string PlacementRule);

/// <summary>A water record from the bundled ITTC table, with the table's hash.</summary>
public sealed record WaterRecord(double TemperatureC, double SalinityGPerKg, double Rho, double Nu, double Pv,
    string Source, string TableHash);

/// <summary>
/// The operating point as stored. <paramref name="HRef"/> null is "depth not set" (DR-ANA-13).
/// <paramref name="Load"/> is reserved by the design manifest (§5.6) and always null in A3a (DR-ANA-9: Custom operating
/// points only). assume: it is the goal-state design load in newtons; confirmed when Area 1 defines it; if not, it is
/// renamed before any run stores a value (every A3a row stores null, so the rename changes no stored key).
/// </summary>
public sealed record OperatingPoint(double Speed, double PAtm, double? HRef, string Datum, double AlphaDeg, double? Load);

/// <summary>The reference quantities used, derived at run time and recorded so a later change is a visible difference.</summary>
public sealed record RunReference(double SRef, double BRef, double CRef, string MomentDatum, string Axes);

/// <summary>Solver diagnostics: ‖AΓ − b‖∞ and the 1-norm condition estimate κ₁ (non-additive).</summary>
public sealed record RunDiagnostics(double ResidualInf, double Kappa1);

public sealed record RunPlatform(string Os, string Arch, string Dotnet);

/// <summary>
/// Spanwise lattice row <paramref name="J"/> of one Completed run (contiguous 0…n−1). Forces and moments are near-field,
/// about the frame origin, in body axes, and additive across the strips of one run only.
/// </summary>
public sealed record StripLoad(int J, double Y, double Eta, double Chord, double Gamma, double AlphaI, double AlphaEff,
    double ReLocal, double ClLocal, StripValue CdNcrit2, StripValue CdNcrit4,
    double Fx, double Fy, double Fz, double Mx, double My, double Mz, double DownwashTrefftz);

/// <summary>A value, or Unavailable with its reason — never a zero standing in for a missing number.</summary>
public sealed record StripValue(double? Value, string? UnavailableReason);

/// <summary>One polar evaluation at its grain key (empty in A3a: no polar method is installed).</summary>
public sealed record PolarSample(string ProfileHash, string MethodId, string MethodVersion, double Reynolds, double Ncrit,
    string SurfaceState, double AlphaDeg, string WaterHash, double? Cl, double? Cd, double? Cm, double? XtrUpper,
    double? XtrLower, double? CpMin, int? CpMinStations, double? Confidence, bool Converged);

/// <summary>The tombstone a pruned run leaves (ADR-0011 §7), so an Undo that reaches it reads "pruned", not "missing".</summary>
public sealed record PrunedRun(string RunId, string RunKey, DateTimeOffset PrunedAt);

/// <summary>The one definition of the run key, the content hash and the format string (design §3.4, ADR-0011).</summary>
public static class RunRecord
{
    /// <summary>BLAKE3 over JCS of {surface, profiles, water, op, method {id, version}, settings: settingsHash}.</summary>
    public static string Key(RunInputs inputs, WaterRecord water, OperatingPoint op, RunMethod method, string settingsHash) =>
        throw new NotImplementedException("STO: RunRecord.Key");

    /// <summary>BLAKE3 over JCS of the settings.</summary>
    public static string SettingsHash(RunSettings settings) => throw new NotImplementedException("STO: RunRecord.SettingsHash");

    /// <summary>BLAKE3 over JCS of the row without its <c>contentHash</c>.</summary>
    public static string ContentHash(AnalysisRun run) => throw new NotImplementedException("STO: RunRecord.ContentHash");

    /// <summary><c>cfdw-project-1</c> with no run, <c>cfdw-project-2</c> with one or more (ADR-0011 §1).</summary>
    public static string Format(int runCount) => throw new NotImplementedException("STO: RunRecord.Format");
}
