using System.Globalization;
using System.Text;

namespace CfdWorkbench.Core.CfdRun;

/// <summary>
/// Pure deterministic SU2 v8.5.0 configuration emitter (docs/design/cfd-run-pipeline.md §4.3).
/// Enforces capability pre-check before emission (refuses free-surface or cavitation with CFD-UNSUPPORTED-PHYSICS),
/// emits LF-only invariant-culture G17 numbers, zero '#' directives/macros, and zero user strings.
/// </summary>
public static class Su2CaseEmitter
{
    public static IReadOnlyDictionary<string, string> EmitConfigFiles(TypedCaseModel model, bool restartSol = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        model.ValidateInputs();

        var cap = model.CheckCapability(BackendCapability.Su2V850);
        if (!cap.Supported)
            throw new ContractError("CFD-UNSUPPORTED-PHYSICS", cap.Reason!);

        var uInf = model.InflowVelocity();
        double mu = model.Water.Rho * model.Water.Nu;
        string turbModel = model.TurbulenceModel == TurbulenceModelKind.KOmegaSst ? "SST" : "SA";

        var sb = new StringBuilder();
        sb.Append("SOLVER= INC_RANS\n");
        sb.Append($"KIND_TURB_MODEL= {turbModel}\n");
        sb.Append("MATH_PROBLEM= DIRECT\n");
        sb.Append($"RESTART_SOL= {(restartSol ? "YES" : "NO")}\n");
        sb.Append("INC_DENSITY_MODEL= CONSTANT\n");
        sb.Append($"INC_DENSITY_INIT= {F(model.Water.Rho)}\n");
        sb.Append($"INC_VELOCITY_INIT= ( {F(uInf.X)}, {F(uInf.Y)}, {F(uInf.Z)} )\n");
        sb.Append("VISCOSITY_MODEL= CONSTANT_VISCOSITY\n");
        sb.Append($"MU_CONSTANT= {F(mu)}\n");
        sb.Append($"FREESTREAM_TURBULENCEINTENSITY= {F(model.TurbulenceIntensity)}\n");
        sb.Append($"FREESTREAM_TURB2LAMVISCRATIO= {F(model.ViscosityRatio)}\n");
        sb.Append($"AOA= {F(model.Op.AlphaDeg)}\n");
        sb.Append($"REF_AREA= {F(model.Reference.SRef * 0.5)}\n");
        sb.Append($"REF_LENGTH= {F(model.Reference.CRef)}\n");
        sb.Append("REF_ORIGIN_MOMENT_X= 0.0\n");
        sb.Append("REF_ORIGIN_MOMENT_Y= 0.0\n");
        sb.Append("REF_ORIGIN_MOMENT_Z= 0.0\n");
        sb.Append("MARKER_HEATFLUX= ( wing, 0.0 )\n");
        sb.Append("MARKER_FAR= ( farfield )\n");
        sb.Append("MARKER_SYM= ( symmetry )\n");
        sb.Append("MARKER_PLOTTING= ( wing )\n");
        sb.Append("MARKER_MONITORING= ( wing )\n");
        sb.Append($"ITER= {model.MaxIterations.ToString(CultureInfo.InvariantCulture)}\n");
        sb.Append("CONV_RESIDUAL_MINVAL= -8\n");
        sb.Append("HISTORY_OUTPUT= (ITER, RMS_RES, AERO_COEFF, YPLUS)\n");
        sb.Append("CONV_FILENAME= history\n");
        sb.Append("SURFACE_FILENAME= surface_flow\n");
        sb.Append("VOLUME_FILENAME= flow\n");
        sb.Append("RESTART_FILENAME= restart_flow.dat\n");
        sb.Append("OUTPUT_FILES= (RESTART, SURFACE_CSV, PARAVIEW_MULTIBLOCK)\n");
        sb.Append("SCREEN_OUTPUT= (INNER_ITER, RMS_PRESSURE, RMS_VELOCITY-X, LIFT, DRAG)\n");

        return new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["case.cfg"] = sb.ToString(),
            ["cfdw-manifest.json"] = "[\n  \"case.cfg\"\n]\n"
        };
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
