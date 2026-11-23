using System.Globalization;
using System.Text;
using CfdWorkbench.Analysis.CfdRun;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;
using static CfdWorkbench.Analysis.Tests.AnalysisChecks;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>
/// Red-first Analysis harvester tests (docs/design/cfd-run-pipeline.md §7.1, §9):
/// OpenFOAM forceCoeffs/solverInfo/yPlus/wallShearStress/wingSurface &amp; SU2 history.csv parsing,
/// fail-closed CFD-HARVEST-COLUMNS on unknown/missing headers, DR-F2-1 turbulence bounding detection,
/// and wallShearStress absence handling.
/// </summary>
internal static class CfdHarvesterTests
{
    internal static void Run()
    {
        Check("Harvester_OpenFoamAndSu2_FailClosedOnUnknownColumns", Harvester_OpenFoamAndSu2_FailClosed);
        Check("Harvester_DrF21_BoundingAndWallShearStressAbsence", Harvester_BoundingAndWssAbsence);
    }

    private static void Harvester_OpenFoamAndSu2_FailClosed()
    {
        string ofDir = Path.Combine(Path.GetTempPath(), "cfdw-harv-of-" + Guid.NewGuid().ToString("N"));
        string su2Dir = Path.Combine(Path.GetTempPath(), "cfdw-harv-su2-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = CfdRunPipelineTests.OpenDefault();
            var ofModel = CfdRunPipelineTests.BuildSampleModel(session, CfdBackendKind.OpenFoamEsi);
            string runId = Guid.NewGuid().ToString("D");
            string meshHash = ofModel.ComputeMeshHash();

            // 1. Valid OpenFOAM outputs harvest cleanly.
            WriteValidOpenFoamOutputs(ofDir, "500");
            var ofResult = CfdOutputHarvester.HarvestOpenFoam(ofDir, runId, meshHash, ofModel, solverLog: "Iteration 500 clean\n");
            Equal(true, ofResult.ResidualSummary.CriterionMet);
            Equal(true, ofResult.HasWallShearStress);
            Equal(true, ofResult.YPlusP95 is <= 1.0);
            Equal(true, ofResult.YPlusMax is <= 2.0);
            Equal("500", ofResult.FinalTimeDirectory);
            Equal(true, ofResult.FieldEvidence.Count >= 1);
            Equal(true, ofResult.FieldEvidence[0].HasWallShearStress);
            Equal("skin-friction-line convergence", ofResult.FieldEvidence[0].SeparationCriterion);

            // 2. Mutated column header in forceCoeffs -> fails closed with CFD-HARVEST-COLUMNS naming file!
            string coeffPath = Path.Combine(ofDir, "postProcessing", "forceCoeffs", "0", "coefficient.dat");
            string originalCoeff = File.ReadAllText(coeffPath, Encoding.UTF8);
            File.WriteAllText(coeffPath, originalCoeff.Replace("Cl", "LiftCoeffUnknown", StringComparison.Ordinal), Encoding.UTF8);
            AssertContractErrorContains("CFD-HARVEST-COLUMNS", "coefficient.dat", () =>
                CfdOutputHarvester.HarvestOpenFoam(ofDir, runId, meshHash, ofModel));
            File.WriteAllText(coeffPath, originalCoeff, Encoding.UTF8);

            // 3. Mutated column header in solverInfo -> fails closed with CFD-HARVEST-COLUMNS!
            string solverInfoPath = Path.Combine(ofDir, "postProcessing", "solverInfo", "0", "solverInfo.dat");
            string originalSolver = File.ReadAllText(solverInfoPath, Encoding.UTF8);
            File.WriteAllText(solverInfoPath, originalSolver.Replace("p_initial", "pressure_init_mystery", StringComparison.Ordinal), Encoding.UTF8);
            AssertContractErrorContains("CFD-HARVEST-COLUMNS", "solverInfo.dat", () =>
                CfdOutputHarvester.HarvestOpenFoam(ofDir, runId, meshHash, ofModel));
            File.WriteAllText(solverInfoPath, originalSolver, Encoding.UTF8);

            // 4. Valid SU2 history.csv harvests cleanly.
            var su2Model = CfdRunPipelineTests.BuildSampleModel(session, CfdBackendKind.Su2);
            WriteValidSu2Outputs(su2Dir);
            var su2Result = CfdOutputHarvester.HarvestSu2(su2Dir, runId, su2Model.ComputeMeshHash(), su2Model);
            Equal(true, su2Result.ResidualSummary.CriterionMet);
            Equal(true, su2Result.FieldEvidence.Count >= 1);

            // 5. Mutated SU2 history.csv header -> fails closed with CFD-HARVEST-COLUMNS naming history.csv!
            string histPath = Path.Combine(su2Dir, "history.csv");
            string originalHist = File.ReadAllText(histPath, Encoding.UTF8);
            File.WriteAllText(histPath, originalHist.Replace("\"CL\"", "\"MysteryLift\"", StringComparison.Ordinal), Encoding.UTF8);
            AssertContractErrorContains("CFD-HARVEST-COLUMNS", "history.csv", () =>
                CfdOutputHarvester.HarvestSu2(su2Dir, runId, su2Model.ComputeMeshHash(), su2Model));
        }
        finally
        {
            if (Directory.Exists(ofDir)) Directory.Delete(ofDir, recursive: true);
            if (Directory.Exists(su2Dir)) Directory.Delete(su2Dir, recursive: true);
        }
    }

    private static void Harvester_BoundingAndWssAbsence()
    {
        string ofDir = Path.Combine(Path.GetTempPath(), "cfdw-harv-wss-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = CfdRunPipelineTests.OpenDefault();
            var model = CfdRunPipelineTests.BuildSampleModel(session, CfdBackendKind.OpenFoamEsi);
            WriteValidOpenFoamOutputs(ofDir, "500", includeWallShearStress: false);

            // Turbulence bounding in solver log fails DR-F2-1 part 3!
            string boundingLog = "Time = 490\nbounding nuTilda, min: -1e-6 max: 0.01 average: 0.001\nTime = 500\nEnd\n";
            var res = CfdOutputHarvester.HarvestOpenFoam(ofDir, Guid.NewGuid().ToString("D"), model.ComputeMeshHash(), model, boundingLog);
            Equal(false, res.ResidualSummary.CriterionMet);
            Equal(true, res.ResidualSummary.TurbulenceBoundingCountInWindow >= 1);

            // Missing wallShearStress.dat -> HasWallShearStress == false and SeparationCriterion == null (VIZ invariant!).
            Equal(false, res.HasWallShearStress);
            foreach (var ev in res.FieldEvidence)
            {
                Equal(false, ev.HasWallShearStress);
                Equal<string?>(null, ev.SeparationCriterion);
            }
        }
        finally
        {
            if (Directory.Exists(ofDir)) Directory.Delete(ofDir, recursive: true);
        }
    }

    internal static void WriteValidOpenFoamCheckpoint(string caseDir, string timeDir)
    {
        string tPath = Path.Combine(caseDir, timeDir);
        Directory.CreateDirectory(tPath);
        foreach (string field in new[] { "U", "p", "nuTilda" })
        {
            File.WriteAllText(Path.Combine(tPath, field),
                $"FoamFile\n{{\n    version 2.0;\n    format ascii;\n    class volScalarField;\n    object {field};\n}}\ninternalField uniform 0;\n",
                Encoding.UTF8);
        }
    }

    internal static void WriteValidOpenFoamOutputs(string caseDir, string timeDir, bool includeWallShearStress = true)
    {
        WriteValidOpenFoamCheckpoint(caseDir, timeDir);
        if (!File.Exists(Path.Combine(caseDir, "cfdw-manifest.json")))
            File.WriteAllText(Path.Combine(caseDir, "cfdw-manifest.json"), "[\"system/controlDict\"]\n", Encoding.UTF8);

        // forceCoeffs
        string fcDir = Path.Combine(caseDir, "postProcessing", "forceCoeffs", "0");
        Directory.CreateDirectory(fcDir);
        var fc = new StringBuilder();
        fc.AppendLine("# Force coefficients");
        fc.AppendLine("# Time\tCd\tCs\tCl\tCmRoll\tCmPitch\tCmYaw");
        for (int i = 1; i <= 40; i++)
        {
            double cl = 0.412 + (i % 2 == 0 ? 1.0e-4 : -1.0e-4);
            double cd = 0.0184 + (i % 2 == 0 ? 2.0e-5 : -2.0e-5);
            fc.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{i * 12.5:0.#}\t{cd:G17}\t0.0\t{cl:G17}\t0.0\t-0.041\t0.0"));
        }
        File.WriteAllText(Path.Combine(fcDir, "coefficient.dat"), fc.ToString(), Encoding.UTF8);

        // solverInfo
        string siDir = Path.Combine(caseDir, "postProcessing", "solverInfo", "0");
        Directory.CreateDirectory(siDir);
        var si = new StringBuilder();
        si.AppendLine("# Solver information");
        si.AppendLine("# Time\tU_solver\tUx_initial\tUx_final\tUx_iters\tUy_initial\tUy_final\tUy_iters\tUz_initial\tUz_final\tUz_iters\tU_converged\tp_solver\tp_initial\tp_final\tp_iters\tp_converged\tnuTilda_solver\tnuTilda_initial\tnuTilda_final\tnuTilda_iters\tnuTilda_converged");
        si.AppendLine("1\tsmoothSolver\t0.8\t0.4\t5\t0.5\t0.2\t5\t0.6\t0.3\t5\tfalse\tGAMG\t0.5\t0.2\t10\tfalse\tsmoothSolver\t0.2\t0.1\t5\tfalse");
        si.AppendLine("500\tsmoothSolver\t2.0e-4\t1.0e-4\t2\t1.0e-4\t5.0e-5\t2\t1.5e-4\t7.0e-5\t2\ttrue\tGAMG\t2.0e-4\t1.0e-4\t3\ttrue\tsmoothSolver\t4.0e-5\t2.0e-5\t2\ttrue");
        File.WriteAllText(Path.Combine(siDir, "solverInfo.dat"), si.ToString(), Encoding.UTF8);

        // yPlus
        string ypDir = Path.Combine(caseDir, "postProcessing", "yPlus", "0");
        Directory.CreateDirectory(ypDir);
        File.WriteAllText(Path.Combine(ypDir, "yPlus.dat"),
            "# yPlus\n# Time\tpatch\tmin\tmax\taverage\tp95\n500\twing\t0.12\t1.38\t0.45\t0.62\n",
            Encoding.UTF8);

        // wallShearStress
        if (includeWallShearStress)
        {
            string wssDir = Path.Combine(caseDir, "postProcessing", "wallShearStress", "0");
            Directory.CreateDirectory(wssDir);
            File.WriteAllText(Path.Combine(wssDir, "wallShearStress.dat"),
                "# wallShearStress\n# Time\tpatch\tmin\tmax\tintegral\n500\twing\t(-5 0 0)\t(20 0 0)\t(1.2 0 0)\n",
                Encoding.UTF8);
        }

        // wingSurface sample
        string surfDir = Path.Combine(caseDir, "postProcessing", "wingSurface", timeDir);
        Directory.CreateDirectory(surfDir);
        string surfHeader = includeWallShearStress
            ? "# x y z p Cp wallShearStress\n0.01 0.1 0.005 -1200.0 -1.84 14.2\n0.05 0.2 0.002 650.0 1.00 8.5\n"
            : "# x y z p Cp\n0.01 0.1 0.005 -1200.0 -1.84\n0.05 0.2 0.002 650.0 1.00\n";
        File.WriteAllText(Path.Combine(surfDir, "wing.raw"), surfHeader, Encoding.UTF8);
    }

    internal static void WriteValidSu2Outputs(string caseDir)
    {
        Directory.CreateDirectory(caseDir);
        var sb = new StringBuilder();
        sb.AppendLine("\"Inner_Iter\",\"rms[P]\",\"rms[U]\",\"CL\",\"CD\",\"CMz\"");
        for (int i = 0; i < 40; i++)
        {
            double rmsP = i == 0 ? 0.0 : -4.0;
            double rmsU = i == 0 ? 0.0 : -4.2;
            double cl = 0.415 + (i % 2 == 0 ? 1e-4 : -1e-4);
            double cd = 0.0182 + (i % 2 == 0 ? 2e-5 : -2e-5);
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{i},{rmsP:G17},{rmsU:G17},{cl:G17},{cd:G17},-0.039"));
        }
        File.WriteAllText(Path.Combine(caseDir, "history.csv"), sb.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(caseDir, "restart_flow.dat"), "SU2_RESTART_BINARY_PLACEHOLDER\n", Encoding.UTF8);
    }

    private static void AssertContractErrorContains(string expectedCode, string expectedSubstring, Action action)
    {
        try { action(); }
        catch (ContractError ex)
        {
            Equal(expectedCode, ex.Code);
            Equal(true, (ex.Reason ?? "").Contains(expectedSubstring, StringComparison.Ordinal),
                $"Expected reason containing '{expectedSubstring}', got '{ex.Reason}'");
            return;
        }
        throw new InvalidOperationException("Expected ContractError " + expectedCode);
    }
}
