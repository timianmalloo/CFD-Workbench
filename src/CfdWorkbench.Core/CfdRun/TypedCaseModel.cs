namespace CfdWorkbench.Core.CfdRun;

public enum CfdBackendKind { OpenFoamEsi, Su2 }

public enum TurbulenceModelKind { SpalartAllmaras, KOmegaSst }

public enum WallTreatmentKind { WallResolved, WallFunction }

public enum TransitionModeKind { FullyTurbulent }

public sealed record Vector3D(double X, double Y, double Z);

public sealed record WingBounds(double MinX, double MaxX, double MinY, double MaxY, double MinZ, double MaxZ);

public sealed record DomainExtents(
    double UpstreamChords = 12.0,
    double DownstreamChords = 24.0,
    double LateralHalfSpans = 6.0,
    double VerticalChords = 12.0);

public sealed record MeshSettings(
    int SurfaceLayers = 20,
    double ExpansionRatio = 1.18,
    double FirstLayerHeightMeters = 1.0e-5,
    double BaseCellSizeMeters = 0.05,
    int RefinementLevel = 3);

public sealed record PhysicsFlags(
    bool RequiresFreeSurface = false,
    bool RequiresCavitation = false,
    bool Transient = false);

public sealed record BackendCapability(
    CfdBackendKind Backend,
    string Version,
    bool SupportsFreeSurface,
    bool SupportsCavitation,
    bool HasBuiltInMesher)
{
    public static readonly BackendCapability OpenFoamEsiV2512 =
        new(CfdBackendKind.OpenFoamEsi, "v2512", SupportsFreeSurface: false, SupportsCavitation: false, HasBuiltInMesher: true);

    public static readonly BackendCapability Su2V850 =
        new(CfdBackendKind.Su2, "v8.5.0", SupportsFreeSurface: false, SupportsCavitation: false, HasBuiltInMesher: false);
}

public sealed record CapabilityCheckResult(bool Supported, string? Reason);

public sealed record WallFunctionFeasibilityResult(
    bool Feasible,
    double ReynoldsChord,
    double CfEstimate,
    double Utau,
    double BoundaryLayerThicknessMeters,
    double FirstCellCenterY1Meters,
    double StackHeight15Meters,
    string Reason);

/// <summary>
/// Immutable pure Core CFD case model feeding both OpenFOAM ESI v2512 and SU2 v8.5.0 emitters
/// (docs/design/cfd-run-pipeline.md §4.1). Imposes angle of attack by rotating inflow direction so
/// one mesh serves all angles at held geometry and speed (identical <see cref="ComputeMeshHash"/>).
/// Zero user-entered strings ever reach emitted dictionaries.
/// </summary>
public sealed record TypedCaseModel(
    RunInputs Inputs,
    RunReference Reference,
    WingBounds Bounds,
    WaterRecord Water,
    OperatingPoint Op,
    CfdBackendPin BackendPin,
    TurbulenceModelKind TurbulenceModel = TurbulenceModelKind.SpalartAllmaras,
    WallTreatmentKind WallTreatment = WallTreatmentKind.WallResolved,
    double TurbulenceIntensity = 0.01,
    double ViscosityRatio = 3.0,
    TransitionModeKind TransitionMode = TransitionModeKind.FullyTurbulent,
    DomainExtents? Domain = null,
    MeshSettings? Mesh = null,
    int MaxIterations = 2000,
    int AveragingWindowIterations = 200,
    double ClHalfBandCap = 1e-3,
    double CdHalfBandCap = 2e-4,
    double ResidualDropTarget = 1e-3,
    double TeFloorMm = 0.3,
    PhysicsFlags? Physics = null)
{
    public DomainExtents EffectiveDomain => Domain ?? new DomainExtents();
    public MeshSettings EffectiveMesh => Mesh ?? new MeshSettings();
    public PhysicsFlags EffectivePhysics => Physics ?? new PhysicsFlags();

    /// <summary>Validates all numeric parameters are finite and physically positive before emission (M4).</summary>
    public void ValidateInputs()
    {
        var dom = EffectiveDomain;
        var mesh = EffectiveMesh;
        RequireFinite(Op.Speed, Op.PAtm, Op.AlphaDeg,
            Water.TemperatureC, Water.SalinityGPerKg, Water.Rho, Water.Nu, Water.Pv,
            Reference.SRef, Reference.BRef, Reference.CRef,
            Bounds.MinX, Bounds.MaxX, Bounds.MinY, Bounds.MaxY, Bounds.MinZ, Bounds.MaxZ,
            TurbulenceIntensity, ViscosityRatio,
            dom.UpstreamChords, dom.DownstreamChords, dom.LateralHalfSpans, dom.VerticalChords,
            mesh.ExpansionRatio, mesh.FirstLayerHeightMeters, mesh.BaseCellSizeMeters,
            ClHalfBandCap, CdHalfBandCap, ResidualDropTarget, TeFloorMm);
        if (Op.HRef is { } h) RequireFinite(h);
        if (Op.Load is { } l) RequireFinite(l);

        Guard.Require(Op.Speed > 0 && Op.PAtm > 0 && Water.Rho > 0 && Water.Nu > 0, "CFD-CASE-NONFINITE");
        Guard.Require(Reference.SRef > 0 && Reference.BRef > 0 && Reference.CRef > 0, "CFD-CASE-NONFINITE");
        Guard.Require(TurbulenceIntensity > 0 && ViscosityRatio > 0, "CFD-CASE-NONFINITE");
        Guard.Require(dom.UpstreamChords > 0 && dom.DownstreamChords > 0 && dom.LateralHalfSpans > 0 && dom.VerticalChords > 0, "CFD-CASE-NONFINITE");
        Guard.Require(mesh.SurfaceLayers >= 1 && mesh.ExpansionRatio >= 1.0 && mesh.FirstLayerHeightMeters > 0 && mesh.BaseCellSizeMeters > 0 && mesh.RefinementLevel >= 0, "CFD-CASE-NONFINITE");
        Guard.Require(MaxIterations >= 1 && AveragingWindowIterations >= 1, "CFD-CASE-NONFINITE");
        Guard.Require(ClHalfBandCap > 0 && CdHalfBandCap > 0 && ResidualDropTarget > 0 && TeFloorMm >= 0, "CFD-CASE-NONFINITE");
    }

    /// <summary>Inflow velocity vector U_inf = (V cos α, 0, V sin α) in body axes (+x aft, +y starboard, +z up).</summary>
    public Vector3D InflowVelocity()
    {
        ValidateInputs();
        double rad = Op.AlphaDeg * PlacementRule.RadiansPerDegree;
        return new Vector3D(Op.Speed * Math.Cos(rad), 0.0, Op.Speed * Math.Sin(rad));
    }

    /// <summary>Wind-axis lift unit vector (-sin α, 0, cos α).</summary>
    public Vector3D LiftDirection()
    {
        ValidateInputs();
        double rad = Op.AlphaDeg * PlacementRule.RadiansPerDegree;
        return new Vector3D(-Math.Sin(rad), 0.0, Math.Cos(rad));
    }

    /// <summary>Wind-axis drag unit vector (cos α, 0, sin α).</summary>
    public Vector3D DragDirection()
    {
        ValidateInputs();
        double rad = Op.AlphaDeg * PlacementRule.RadiansPerDegree;
        return new Vector3D(Math.Cos(rad), 0.0, Math.Sin(rad));
    }

    /// <summary>
    /// Mesh identity hash over (SurfaceHash, ProfileHashes, Speed, WaterRecord, DomainExtents, MeshSettings, Bounds).
    /// Explicitly excludes AlphaDeg so every angle in a held-speed sweep shares one mesh hash.
    /// </summary>
    public string ComputeMeshHash()
    {
        ValidateInputs();
        var dom = EffectiveDomain;
        var mesh = EffectiveMesh;
        var manifest = new Dictionary<string, object?>
        {
            ["surfaceHash"] = Inputs.SurfaceHash,
            ["profileHashes"] = Inputs.ProfileHashes.ToList(),
            ["speed"] = Op.Speed,
            ["water"] = new Dictionary<string, object?>
            {
                ["temperatureC"] = Water.TemperatureC,
                ["salinityGPerKg"] = Water.SalinityGPerKg,
                ["rho"] = Water.Rho,
                ["nu"] = Water.Nu,
                ["tableHash"] = Water.TableHash
            },
            ["bounds"] = new Dictionary<string, object?>
            {
                ["minX"] = Bounds.MinX, ["maxX"] = Bounds.MaxX,
                ["minY"] = Bounds.MinY, ["maxY"] = Bounds.MaxY,
                ["minZ"] = Bounds.MinZ, ["maxZ"] = Bounds.MaxZ
            },
            ["domain"] = new Dictionary<string, object?>
            {
                ["upstreamChords"] = dom.UpstreamChords,
                ["downstreamChords"] = dom.DownstreamChords,
                ["lateralHalfSpans"] = dom.LateralHalfSpans,
                ["verticalChords"] = dom.VerticalChords
            },
            ["mesh"] = new Dictionary<string, object?>
            {
                ["surfaceLayers"] = mesh.SurfaceLayers,
                ["expansionRatio"] = mesh.ExpansionRatio,
                ["firstLayerHeightMeters"] = mesh.FirstLayerHeightMeters,
                ["baseCellSizeMeters"] = mesh.BaseCellSizeMeters,
                ["refinementLevel"] = mesh.RefinementLevel
            }
        };
        return CfdRunRecords.Blake3Jcs(manifest);
    }

    /// <summary>Deterministic BLAKE3(JCS) hash of the typed case model settings.</summary>
    public string ComputeCaseModelHash()
    {
        ValidateInputs();
        var dom = EffectiveDomain;
        var mesh = EffectiveMesh;
        var phys = EffectivePhysics;
        var manifest = new Dictionary<string, object?>
        {
            ["turbulenceModel"] = TurbulenceModel.ToString(),
            ["wallTreatment"] = WallTreatment.ToString(),
            ["turbulenceIntensity"] = TurbulenceIntensity,
            ["viscosityRatio"] = ViscosityRatio,
            ["transitionMode"] = TransitionMode.ToString(),
            ["domain"] = new Dictionary<string, object?>
            {
                ["upstreamChords"] = dom.UpstreamChords,
                ["downstreamChords"] = dom.DownstreamChords,
                ["lateralHalfSpans"] = dom.LateralHalfSpans,
                ["verticalChords"] = dom.VerticalChords
            },
            ["mesh"] = new Dictionary<string, object?>
            {
                ["surfaceLayers"] = mesh.SurfaceLayers,
                ["expansionRatio"] = mesh.ExpansionRatio,
                ["firstLayerHeightMeters"] = mesh.FirstLayerHeightMeters,
                ["baseCellSizeMeters"] = mesh.BaseCellSizeMeters,
                ["refinementLevel"] = mesh.RefinementLevel
            },
            ["reference"] = new Dictionary<string, object?>
            {
                ["sRef"] = Reference.SRef,
                ["bRef"] = Reference.BRef,
                ["cRef"] = Reference.CRef,
                ["momentDatum"] = Reference.MomentDatum,
                ["axes"] = Reference.Axes
            },
            ["bounds"] = new Dictionary<string, object?>
            {
                ["minX"] = Bounds.MinX, ["maxX"] = Bounds.MaxX,
                ["minY"] = Bounds.MinY, ["maxY"] = Bounds.MaxY,
                ["minZ"] = Bounds.MinZ, ["maxZ"] = Bounds.MaxZ
            },
            ["maxIterations"] = MaxIterations,
            ["averagingWindowIterations"] = AveragingWindowIterations,
            ["clHalfBandCap"] = ClHalfBandCap,
            ["cdHalfBandCap"] = CdHalfBandCap,
            ["residualDropTarget"] = ResidualDropTarget,
            ["teFloorMm"] = TeFloorMm,
            ["physics"] = new Dictionary<string, object?>
            {
                ["requiresFreeSurface"] = phys.RequiresFreeSurface,
                ["requiresCavitation"] = phys.RequiresCavitation,
                ["transient"] = phys.Transient
            }
        };
        return CfdRunRecords.Blake3Jcs(manifest);
    }

    /// <summary>Canonical deterministic 64-hex run key (also valid as an M5 directory name).</summary>
    public string ComputeCaseKey() =>
        CfdRunRecords.ComputeRunKey(Inputs, Water, Op, BackendPin, ComputeCaseModelHash());

    /// <summary>
    /// DR-F2-3 wall-function feasibility check (docs/design/cfd-run-pipeline.md §5.2):
    /// admits wall functions iff 15 layer points starting at y+ = 30 fit inside the estimated turbulent BL thickness delta.
    /// </summary>
    public WallFunctionFeasibilityResult CheckWallFunctionFeasibility()
    {
        ValidateInputs();
        double reC = Op.Speed * Reference.CRef / Water.Nu;
        double rePow = Math.Pow(reC, -0.2);
        double cf = 0.058 * rePow;
        double uTau = Op.Speed * Math.Sqrt(cf / 2.0);
        double delta = 0.37 * Reference.CRef * rePow;
        double y1 = 30.0 * Water.Nu / uTau;
        double dy1 = 2.0 * y1;
        double r = Math.Min(EffectiveMesh.ExpansionRatio, 1.2);
        double h15 = Math.Abs(r - 1.0) < 1e-12
            ? 15.0 * dy1
            : dy1 * (Math.Pow(r, 15) - 1.0) / (r - 1.0);

        bool feasible = h15 <= delta;
        string reason = feasible
            ? "Wall-function stack fits inside estimated turbulent boundary layer (H15 <= delta)."
            : "Wall-function branch refused (DR-F2-3): 15-point y+>=30 layer stack exceeds estimated boundary-layer thickness; use wall-resolved (y+ <= 1).";
        return new WallFunctionFeasibilityResult(feasible, reC, cf, uTau, delta, y1, h15, reason);
    }

    /// <summary>Backend capability pre-check before leaving Pending (docs/design/cfd-run-pipeline.md §4.3).</summary>
    public CapabilityCheckResult CheckCapability(BackendCapability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        var phys = EffectivePhysics;
        if ((phys.RequiresFreeSurface && !capability.SupportsFreeSurface) ||
            (phys.RequiresCavitation && !capability.SupportsCavitation))
        {
            string reason = capability.Backend == CfdBackendKind.Su2
                ? "SU2 v8.5.0 does not support free-surface or cavitation physics"
                : $"{capability.Backend} {capability.Version} does not support free-surface or cavitation physics";
            return new CapabilityCheckResult(false, reason);
        }
        return new CapabilityCheckResult(true, null);
    }

    private static void RequireFinite(params double[] values)
    {
        foreach (double v in values)
        {
            if (!double.IsFinite(v))
                throw new ContractError("CFD-CASE-NONFINITE", "Case model parameter must be finite.");
        }
    }
}
