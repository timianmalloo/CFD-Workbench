using System.Globalization;

namespace CfdWorkbench.Core.CfdRun;

public enum MeshKind { UnstructuredOpenFoam, Structured }

public sealed record MeshMetrics(
    double MinCellVolume,
    int InvalidFaces,
    int ZeroAreaFaces,
    int OpenCells,
    int FailedCheckMeshCount,
    double MaxNonOrthogonalityDeg,
    double MaxInternalSkewness,
    double MaxBoundarySkewness,
    double MinCellDeterminantLayer,
    double MinCellDeterminantCore,
    double? MinStructuredDeterminant,
    double? MeanStructuredDeterminant,
    double UpstreamChords,
    double DownstreamChords,
    double LayerExpansionRatio,
    double LayerCoverage20PlusFraction,
    double LayerCoverage10PlusFraction,
    IReadOnlyList<RegionLayerCoverage> RegionCoverage,
    double? YPlusMin,
    double? YPlusP95,
    double? YPlusMax,
    int? WallFunctionLayerPoints);

public sealed record MeshGateOptions(
    WallTreatmentKind WallTreatment = WallTreatmentKind.WallResolved,
    MeshKind MeshKind = MeshKind.UnstructuredOpenFoam,
    bool FieldIsStationary = true,
    bool WallFunctionFeasible = false);

/// <summary>
/// Pure DR-F3-1 A / DR-F2-3 / DR-F2-6 / ITTC 7.5-03-02-03 mesh quality gate evaluator
/// (docs/design/cfd-run-pipeline.md §5). Evaluates every threshold and reports every failing measure
/// with observed value and exact threshold so failing meshes stop cleanly before Solving.
/// </summary>
public static class MeshGateEvaluator
{
    public const double MaxNonOrthogonalityLimitDeg = 70.0;
    public const double MaxInternalSkewnessLimit = 4.0;
    public const double MaxBoundarySkewnessLimit = 20.0;
    public const double MinOpenFoamDeterminantLimit = 0.001;
    public const double MinStructuredLocalDeterminantLimit = 0.15;
    public const double MinStructuredBulkDeterminantLimit = 0.30;
    public const double MinUpstreamChordsLimit = 10.0;
    public const double MinDownstreamChordsLimit = 20.0;
    public const double MaxLayerExpansionRatioLimit = 1.2;
    public const double MinLayer20CoverageFraction = 0.95;
    public const double MinLayer10CoverageFraction = 1.0;
    public const double WallResolvedYPlusP95Limit = 1.0;
    public const double WallResolvedYPlusMaxLimit = 2.0;
    public const double WallFunctionYPlusMinLimit = 30.0;
    public const double WallFunctionYPlusMaxLimit = 100.0;
    public const int WallFunctionMinLayerPoints = 15;

    public const string ScreenOnlyLabel = "mesh screen — not the y+ gate (awaiting stationary field)";

    public static MeshGateResult EvaluateMeshGate(MeshMetrics metrics, MeshGateOptions options)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<MeshGateFailure>();
        string branch = options.WallTreatment == WallTreatmentKind.WallFunction ? "wall-function" : "wall-resolved";

        // 1. Validity: positive volumes & topology
        if (!double.IsFinite(metrics.MinCellVolume) || metrics.MinCellVolume <= 0.0)
            failures.Add(Fail("MinCellVolume", metrics.MinCellVolume, 0.0, "Minimum cell volume must be strictly positive."));
        if (metrics.InvalidFaces != 0)
            failures.Add(Fail("InvalidFaces", metrics.InvalidFaces, 0.0, "Mesh contains invalid faces."));
        if (metrics.ZeroAreaFaces != 0)
            failures.Add(Fail("ZeroAreaFaces", metrics.ZeroAreaFaces, 0.0, "Mesh contains zero-area faces."));
        if (metrics.OpenCells != 0)
            failures.Add(Fail("OpenCells", metrics.OpenCells, 0.0, "Mesh contains open cells."));
        if (metrics.FailedCheckMeshCount != 0)
            failures.Add(Fail("FailedCheckMeshCount", metrics.FailedCheckMeshCount, 0.0, "checkMesh reported failed checks."));

        // 2. Non-orthogonality & skewness (DR-F3-1 A)
        if (!double.IsFinite(metrics.MaxNonOrthogonalityDeg) || metrics.MaxNonOrthogonalityDeg > MaxNonOrthogonalityLimitDeg)
            failures.Add(Fail("MaxNonOrthogonalityDeg", metrics.MaxNonOrthogonalityDeg, MaxNonOrthogonalityLimitDeg,
                string.Create(CultureInfo.InvariantCulture, $"Max non-orthogonality {metrics.MaxNonOrthogonalityDeg:0.##} deg exceeds {MaxNonOrthogonalityLimitDeg:0.##} deg.")));
        if (!double.IsFinite(metrics.MaxInternalSkewness) || metrics.MaxInternalSkewness > MaxInternalSkewnessLimit)
            failures.Add(Fail("MaxInternalSkewness", metrics.MaxInternalSkewness, MaxInternalSkewnessLimit,
                string.Create(CultureInfo.InvariantCulture, $"Max internal skewness {metrics.MaxInternalSkewness:0.##} exceeds {MaxInternalSkewnessLimit:0.##}.")));
        if (!double.IsFinite(metrics.MaxBoundarySkewness) || metrics.MaxBoundarySkewness > MaxBoundarySkewnessLimit)
            failures.Add(Fail("MaxBoundarySkewness", metrics.MaxBoundarySkewness, MaxBoundarySkewnessLimit,
                string.Create(CultureInfo.InvariantCulture, $"Max boundary skewness {metrics.MaxBoundarySkewness:0.##} exceeds {MaxBoundarySkewnessLimit:0.##}.")));

        // 3. Cell determinant (DR-F2-6, reported separately for layer vs core)
        if (!double.IsFinite(metrics.MinCellDeterminantLayer) || metrics.MinCellDeterminantLayer < MinOpenFoamDeterminantLimit)
            failures.Add(Fail("MinCellDeterminantLayer", metrics.MinCellDeterminantLayer, MinOpenFoamDeterminantLimit,
                string.Create(CultureInfo.InvariantCulture, $"Layer cell determinant {metrics.MinCellDeterminantLayer:G6} is below {MinOpenFoamDeterminantLimit:G6}.")));
        if (!double.IsFinite(metrics.MinCellDeterminantCore) || metrics.MinCellDeterminantCore < MinOpenFoamDeterminantLimit)
            failures.Add(Fail("MinCellDeterminantCore", metrics.MinCellDeterminantCore, MinOpenFoamDeterminantLimit,
                string.Create(CultureInfo.InvariantCulture, $"Core cell determinant {metrics.MinCellDeterminantCore:G6} is below {MinOpenFoamDeterminantLimit:G6}.")));

        if (options.MeshKind == MeshKind.Structured)
        {
            double minDet = metrics.MinStructuredDeterminant ?? 0.0;
            double meanDet = metrics.MeanStructuredDeterminant ?? 0.0;
            if (!double.IsFinite(minDet) || minDet < MinStructuredLocalDeterminantLimit)
                failures.Add(Fail("MinStructuredDeterminant", minDet, MinStructuredLocalDeterminantLimit,
                    "Structured local cell determinant must be >= 0.15."));
            if (!double.IsFinite(meanDet) || meanDet <= MinStructuredBulkDeterminantLimit)
                failures.Add(Fail("MeanStructuredDeterminant", meanDet, MinStructuredBulkDeterminantLimit,
                    "Structured bulk cell determinant must be > 0.30."));
        }

        // 4. Domain extents & layer expansion (ITTC 7.5-03-02-03)
        if (!double.IsFinite(metrics.UpstreamChords) || metrics.UpstreamChords < MinUpstreamChordsLimit)
            failures.Add(Fail("UpstreamChords", metrics.UpstreamChords, MinUpstreamChordsLimit,
                "Upstream domain extent must be >= 10 chords."));
        if (!double.IsFinite(metrics.DownstreamChords) || metrics.DownstreamChords < MinDownstreamChordsLimit)
            failures.Add(Fail("DownstreamChords", metrics.DownstreamChords, MinDownstreamChordsLimit,
                "Downstream domain extent must be >= 20 chords."));
        if (!double.IsFinite(metrics.LayerExpansionRatio) || metrics.LayerExpansionRatio > MaxLayerExpansionRatioLimit)
            failures.Add(Fail("LayerExpansionRatio", metrics.LayerExpansionRatio, MaxLayerExpansionRatioLimit,
                "Boundary-layer expansion ratio must be <= 1.2."));

        // 5. Layer coverage (DR-F2-3: >= 20 layers on >= 95% AND >= 10 layers on 100%)
        if (!double.IsFinite(metrics.LayerCoverage20PlusFraction) || metrics.LayerCoverage20PlusFraction < MinLayer20CoverageFraction)
            failures.Add(Fail("LayerCoverage20PlusFraction", metrics.LayerCoverage20PlusFraction, MinLayer20CoverageFraction,
                "At least 95% of wing faces must carry >= 20 prism layers."));
        if (!double.IsFinite(metrics.LayerCoverage10PlusFraction) || metrics.LayerCoverage10PlusFraction < MinLayer10CoverageFraction)
            failures.Add(Fail("LayerCoverage10PlusFraction", metrics.LayerCoverage10PlusFraction, MinLayer10CoverageFraction,
                "100% of wing faces must carry >= 10 prism layers."));

        if (metrics.RegionCoverage is not null)
        {
            foreach (var region in metrics.RegionCoverage)
            {
                if (!double.IsFinite(region.FractionAtLeast10) || region.FractionAtLeast10 < MinLayer10CoverageFraction)
                    failures.Add(Fail($"Region:{region.Region}:FractionAtLeast10", region.FractionAtLeast10, MinLayer10CoverageFraction,
                        $"Region '{region.Region}' has faces with fewer than 10 layers."));
            }
        }

        // 6. Stationary y+ gate (DR-F2-3); pre-stationary is marked IsScreenOnly = true.
        bool isScreenOnly = !options.FieldIsStationary;
        if (options.FieldIsStationary)
        {
            if (options.WallTreatment == WallTreatmentKind.WallResolved)
            {
                if (metrics.YPlusP95 is { } p95)
                {
                    if (!double.IsFinite(p95) || p95 > WallResolvedYPlusP95Limit)
                        failures.Add(Fail("YPlusP95", p95, WallResolvedYPlusP95Limit,
                            string.Create(CultureInfo.InvariantCulture, $"Wall-resolved y+ p95 ({p95:0.###}) exceeds {WallResolvedYPlusP95Limit:0.#}.")));
                }
                else
                {
                    failures.Add(Fail("YPlusP95", double.NaN, WallResolvedYPlusP95Limit, "Stationary wall-resolved y+ p95 is missing."));
                }

                if (metrics.YPlusMax is { } ymax)
                {
                    if (!double.IsFinite(ymax) || ymax > WallResolvedYPlusMaxLimit)
                        failures.Add(Fail("YPlusMax", ymax, WallResolvedYPlusMaxLimit,
                            string.Create(CultureInfo.InvariantCulture, $"Wall-resolved y+ max ({ymax:0.###}) exceeds {WallResolvedYPlusMaxLimit:0.#}.")));
                }
                else
                {
                    failures.Add(Fail("YPlusMax", double.NaN, WallResolvedYPlusMaxLimit, "Stationary wall-resolved y+ max is missing."));
                }
            }
            else
            {
                if (!options.WallFunctionFeasible)
                    failures.Add(Fail("WallFunctionFeasible", 0.0, 1.0, "Wall-function branch is not feasible for this Reynolds number / chord (DR-F2-3)."));
                double yMin = metrics.YPlusMin ?? 0.0;
                double yMax = metrics.YPlusMax ?? double.PositiveInfinity;
                int pts = metrics.WallFunctionLayerPoints ?? 0;
                if (!double.IsFinite(yMin) || yMin < WallFunctionYPlusMinLimit)
                    failures.Add(Fail("YPlusMin", yMin, WallFunctionYPlusMinLimit, "Wall-function y+ min must be >= 30."));
                if (!double.IsFinite(yMax) || yMax > WallFunctionYPlusMaxLimit)
                    failures.Add(Fail("YPlusMax", yMax, WallFunctionYPlusMaxLimit, "Wall-function y+ max must be <= 100."));
                if (pts < WallFunctionMinLayerPoints)
                    failures.Add(Fail("WallFunctionLayerPoints", pts, WallFunctionMinLayerPoints, "Wall-function branch requires >= 15 layer points."));
            }
        }

        return new MeshGateResult(
            Passed: failures.Count == 0,
            Branch: branch,
            MaxNonOrthogonalityDeg: metrics.MaxNonOrthogonalityDeg,
            MaxInternalSkewness: metrics.MaxInternalSkewness,
            MaxBoundarySkewness: metrics.MaxBoundarySkewness,
            MinCellVolume: metrics.MinCellVolume,
            MinCellDeterminantLayer: metrics.MinCellDeterminantLayer,
            MinCellDeterminantCore: metrics.MinCellDeterminantCore,
            LayerCoverage20PlusFraction: metrics.LayerCoverage20PlusFraction,
            LayerCoverage10PlusFraction: metrics.LayerCoverage10PlusFraction,
            RegionCoverage: metrics.RegionCoverage ?? [],
            YPlusP95: metrics.YPlusP95,
            YPlusMax: metrics.YPlusMax,
            IsScreenOnly: isScreenOnly,
            Failures: failures);
    }

    private static MeshGateFailure Fail(string measure, double observed, double threshold, string message) =>
        new(measure, double.IsFinite(observed) ? observed : -1.0, threshold, message);
}
