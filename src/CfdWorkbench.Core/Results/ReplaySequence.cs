using System.Globalization;

namespace CfdWorkbench.Core.Results;

/// <summary>Held parameter axis for a compatible parametric replay sequence (spec A5.11, RES-03).</summary>
public enum ReplayHeldAxis
{
    HeldSpeed,
    HeldAlpha
}

/// <summary>Pinned camera, streamline seed rake, and slice plane configuration preserved across replay frames.</summary>
public sealed record ReplayCameraState(
    string CameraPreset,
    string SeedRake,
    IReadOnlyList<string> SlicePlanes);

/// <summary>One discrete sample slot (admitted or failed/gap) inside a parametric replay series.</summary>
public sealed record ReplaySampleSlot(
    int CaseIndex,
    string RunId,
    string? SurfaceHash,
    string? MeshHash,
    string MethodId,
    string MethodVersion,
    WaterRecord Water,
    string FieldInventoryHash,
    double SpeedMs,
    double AlphaDeg,
    bool Admitted,
    string? FailureReason,
    string? RetrySampleRunId,
    IReadOnlyList<double?>? FieldValues,
    IReadOnlyDictionary<string, double?>? Metrics,
    ProvenanceStrip? Provenance);

/// <summary>Immutable state snapshot of an ordered discrete parametric replay sequence.</summary>
public sealed record ReplaySeries(
    string SeriesId,
    ReplayHeldAxis HeldAxis,
    double HeldValue,
    string SurfaceHash,
    string MeshHash,
    string MethodId,
    string MethodVersion,
    string FieldInventoryHash,
    string Banner,
    double? LockedScalarMin,
    double? LockedScalarMax,
    bool RangeLocked,
    string RangeScaleLabel,
    ReplayCameraState CameraState,
    bool ReducedMotion,
    bool IsPlaying,
    int CurrentIndex,
    IReadOnlyList<ReplaySampleSlot> Frames,
    IReadOnlyList<double?>? ActiveFieldData,
    IReadOnlyDictionary<string, double?>? ActiveMetrics,
    string? ActiveFailureReason,
    string? ActiveRetrySampleRunId);

/// <summary>
/// Pure zero-I/O parametric replay state machine (spec A5.11, RES-03, docs/design/results-and-candidates.md §4.1).
/// Enforces compatible-series admission (identical MeshHash/SurfaceHash/Method/Water/FieldInventoryHash/HeldAxis),
/// locked scalar range &amp; camera with labelled unlock, failed-sample pause &amp; clear, zero interpolation, and reduced motion.
/// All method names strictly avoid ArchitectureTests banned verbs.
/// </summary>
public static class ReplaySequence
{
    public const string SeriesBanner = "Parametric sweep — sequence over admitted samples";
    public const string RangeLockedLabel = "Range locked — series scale";
    public const string RangeUnlockedLabel = "Range unlocked — per-sample scale";

    private const double HeldAxisTolerance = 1e-9;

    public static ReplaySeries CreateSeries(
        IReadOnlyList<ReplaySampleSlot> samples,
        ReplayHeldAxis heldAxis,
        ReplayCameraState? cameraState = null,
        bool reducedMotion = false)
    {
        Guard.Require(samples is not null && samples.Count > 0, "RES-SERIES-EMPTY");

        var first = samples![0];
        Guard.Require(
            !string.IsNullOrWhiteSpace(first.SurfaceHash) &&
            !string.IsNullOrWhiteSpace(first.MeshHash) &&
            !string.IsNullOrWhiteSpace(first.MethodId) &&
            !string.IsNullOrWhiteSpace(first.MethodVersion) &&
            !string.IsNullOrWhiteSpace(first.FieldInventoryHash) &&
            first.Water is not null,
            "RES-SERIES-INCOMPATIBLE");

        double heldValue = heldAxis == ReplayHeldAxis.HeldSpeed ? first.SpeedMs : first.AlphaDeg;
        Guard.Require(double.IsFinite(heldValue), "RES-SERIES-INCOMPATIBLE");

        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            Guard.Require(s is not null && s.Water is not null, "RES-SERIES-INCOMPATIBLE");
            Guard.Require(double.IsFinite(s!.SpeedMs) && double.IsFinite(s.AlphaDeg), "RES-SERIES-INCOMPATIBLE");

            double slotHeld = heldAxis == ReplayHeldAxis.HeldSpeed ? s.SpeedMs : s.AlphaDeg;
            bool compatible =
                string.Equals(s.SurfaceHash, first.SurfaceHash, StringComparison.Ordinal) &&
                string.Equals(s.MeshHash, first.MeshHash, StringComparison.Ordinal) &&
                string.Equals(s.MethodId, first.MethodId, StringComparison.Ordinal) &&
                string.Equals(s.MethodVersion, first.MethodVersion, StringComparison.Ordinal) &&
                string.Equals(s.FieldInventoryHash, first.FieldInventoryHash, StringComparison.Ordinal) &&
                SameWater(s.Water!, first.Water!) &&
                Math.Abs(slotHeld - heldValue) <= HeldAxisTolerance;

            if (!compatible)
            {
                throw new ContractError(
                    "RES-SERIES-INCOMPATIBLE",
                    "Samples in a replay series must share identical SurfaceHash, MeshHash, Method, Water, FieldInventoryHash, and held axis.");
            }
        }

        // Order strictly ascending by free axis (alpha at held speed, or speed at held alpha).
        var ordered = (heldAxis == ReplayHeldAxis.HeldSpeed
                ? samples.OrderBy(s => s.AlphaDeg).ThenBy(s => s.CaseIndex)
                : samples.OrderBy(s => s.SpeedMs).ThenBy(s => s.CaseIndex))
            .ToArray();

        // Compute locked scalar range once across all admitted samples only.
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        bool anyFinite = false;
        foreach (var frame in ordered)
        {
            if (!frame.Admitted || frame.FieldValues is null) continue;
            foreach (double? v in frame.FieldValues)
            {
                if (v is { } finite && double.IsFinite(finite))
                {
                    if (finite < min) min = finite;
                    if (finite > max) max = finite;
                    anyFinite = true;
                }
            }
        }

        var cam = cameraState ?? new ReplayCameraState("starboard-iso", "default-rake", []);
        string seriesId = string.Create(
            CultureInfo.InvariantCulture,
            $"{first.MeshHash![..Math.Min(12, first.MeshHash!.Length)]}-{heldAxis}-{heldValue:G6}");

        var initial = new ReplaySeries(
            SeriesId: seriesId,
            HeldAxis: heldAxis,
            HeldValue: heldValue,
            SurfaceHash: first.SurfaceHash!,
            MeshHash: first.MeshHash!,
            MethodId: first.MethodId,
            MethodVersion: first.MethodVersion,
            FieldInventoryHash: first.FieldInventoryHash,
            Banner: SeriesBanner,
            LockedScalarMin: anyFinite ? min : null,
            LockedScalarMax: anyFinite ? max : null,
            RangeLocked: true,
            RangeScaleLabel: RangeLockedLabel,
            CameraState: cam,
            ReducedMotion: reducedMotion,
            IsPlaying: false,
            CurrentIndex: 0,
            Frames: ordered,
            ActiveFieldData: null,
            ActiveMetrics: null,
            ActiveFailureReason: null,
            ActiveRetrySampleRunId: null);

        return SelectFrame(initial, 0);
    }

    /// <summary>Moves to a discrete integer frame index (zero interpolation). Failed samples pause and clear displayed data.</summary>
    public static ReplaySeries SelectFrame(ReplaySeries series, int index)
    {
        ArgumentNullException.ThrowIfNull(series);
        Guard.Require(index >= 0 && index < series.Frames.Count, "RES-FRAME-RANGE");

        var frame = series.Frames[index];
        if (!frame.Admitted)
        {
            // Failed-sample pause & clear invariant: MUST stop playback and clear ActiveFieldData/ActiveMetrics immediately.
            return series with
            {
                CurrentIndex = index,
                IsPlaying = false,
                ActiveFieldData = null,
                ActiveMetrics = null,
                ActiveFailureReason = string.IsNullOrWhiteSpace(frame.FailureReason) ? "Sample failed" : frame.FailureReason,
                ActiveRetrySampleRunId = frame.RetrySampleRunId ?? frame.RunId
            };
        }

        return series with
        {
            CurrentIndex = index,
            IsPlaying = series.ReducedMotion ? false : series.IsPlaying,
            ActiveFieldData = frame.FieldValues,
            ActiveMetrics = frame.Metrics,
            ActiveFailureReason = null,
            ActiveRetrySampleRunId = null
        };
    }

    public static ReplaySeries ScrubTo(ReplaySeries series, int index) => SelectFrame(series, index);

    public static ReplaySeries AdvanceStep(ReplaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        int next = Math.Min(series.CurrentIndex + 1, series.Frames.Count - 1);
        return SelectFrame(series, next);
    }

    public static ReplaySeries StepBackward(ReplaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        int prev = Math.Max(series.CurrentIndex - 1, 0);
        return SelectFrame(series, prev);
    }

    public static ReplaySeries Play(ReplaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        if (series.ReducedMotion)
            return series with { IsPlaying = false };
        var current = series.Frames[series.CurrentIndex];
        if (!current.Admitted)
            return series with { IsPlaying = false, ActiveFieldData = null, ActiveMetrics = null };
        return series with { IsPlaying = true };
    }

    public static ReplaySeries Hold(ReplaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        return series with { IsPlaying = false };
    }

    public static ReplaySeries ToggleRangeLock(ReplaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        bool nextLocked = !series.RangeLocked;
        return series with
        {
            RangeLocked = nextLocked,
            RangeScaleLabel = nextLocked ? RangeLockedLabel : RangeUnlockedLabel
        };
    }

    public static ReplaySeries SetReducedMotion(ReplaySeries series, bool reducedMotion)
    {
        ArgumentNullException.ThrowIfNull(series);
        return series with
        {
            ReducedMotion = reducedMotion,
            IsPlaying = reducedMotion ? false : series.IsPlaying
        };
    }

    private static bool SameWater(WaterRecord a, WaterRecord b) =>
        string.Equals(a.TableHash, b.TableHash, StringComparison.Ordinal) &&
        Math.Abs(a.TemperatureC - b.TemperatureC) <= 1e-9 &&
        Math.Abs(a.SalinityGPerKg - b.SalinityGPerKg) <= 1e-9;
}
