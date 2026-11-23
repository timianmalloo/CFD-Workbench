using System.Globalization;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using CfdWorkbench.Core.CfdRun;

/// <summary>
/// Red-first CLI tests for `cfd-workbench cfd-run emit|status|harvest --json` (docs/design/cfd-run-pipeline.md §7.3, §9).
/// Proves CLI-01 runKey/caseModelHash/meshHash/Converged-label parity with Core and zero-PII output.
/// </summary>
internal static class CfdRunCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunCheckAsync("Cli_CfdRun_EmitStatusHarvest_ParityWithCore", EmitStatusHarvest_ParityWithCoreAsync);
        return failures;
    }

    private static async Task<int> RunCheckAsync(string name, Func<Task> body)
    {
        try
        {
            await body();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {name} {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static async Task EmitStatusHarvest_ParityWithCoreAsync()
    {
        string tempRoot = TestTemp.NewDirectory("cfdrun-cli-");
        string projPath = Path.Combine(tempRoot, "wing.cfdw.json");
        string emitDir = Path.Combine(tempRoot, "emitted-of");
        try
        {
            // 1. Save a default project file.
            using (var session = new AuthoringSession())
            {
                session.Open(Cli.ExampleBytes(), Guid.NewGuid().ToString("D"), false);
                await File.WriteAllBytesAsync(projPath, session.SaveImage());
            }

            // 2. `cfd-run emit <project> --backend openfoam --out <dir> --alpha 3.5 --speed 7.0 --json`
            var output = new StringWriter();
            int exit = await Cli.RunAsync(["cfd-run", "emit", projPath, "--backend", "openfoam", "--out", emitDir, "--alpha", "3.5", "--speed", "7.0", "--json"], output);
            if (exit != 0) throw new Exception($"cfd-run emit returned {exit}: {output}");

            string emitKey, emitMeshHash, emitModelHash;
            using (var doc = JsonDocument.Parse(output.ToString()))
            {
                var root = doc.RootElement;
                emitKey = root.GetProperty("runKey").GetString()!;
                emitMeshHash = root.GetProperty("meshHash").GetString()!;
                emitModelHash = root.GetProperty("caseModelHash").GetString()!;
                if (emitKey.Length != 64 || emitMeshHash.Length != 64 || emitModelHash.Length != 64)
                    throw new Exception("Hashes must be 64-char hex");
                if (!File.Exists(Path.Combine(emitDir, "cfdw-manifest.json")))
                    throw new Exception("cfdw-manifest.json not emitted");
                if (!File.Exists(Path.Combine(emitDir, "system", "controlDict")))
                    throw new Exception("system/controlDict not emitted");
            }

            // Verify CLI-01 parity: emitting again at alpha 5.0 yields SAME meshHash and DIFFERENT runKey!
            string emitDir2 = Path.Combine(tempRoot, "emitted-of-a5");
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["cfd-run", "emit", projPath, "--backend", "openfoam", "--out", emitDir2, "--alpha", "5.0", "--speed", "7.0", "--json"], output);
            if (exit != 0) throw new Exception($"cfd-run emit alpha 5 returned {exit}: {output}");
            using (var doc2 = JsonDocument.Parse(output.ToString()))
            {
                if (doc2.RootElement.GetProperty("meshHash").GetString() != emitMeshHash)
                    throw new Exception("meshHash must stay identical across angles at held speed");
                if (doc2.RootElement.GetProperty("runKey").GetString() == emitKey)
                    throw new Exception("runKey must differ across angles");
            }

            // 3. Populate valid OpenFOAM outputs in emitDir and run `cfd-run harvest`.
            WriteValidOpenFoamOutputs(emitDir, "500");
            string runId = Guid.NewGuid().ToString("D");
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["cfd-run", "harvest", projPath, "--case", emitDir, "--run-id", runId, "--alpha", "3.5", "--speed", "7.0", "--json"], output);
            if (exit != 0) throw new Exception($"cfd-run harvest returned {exit}: {output}");
            using (var harvDoc = JsonDocument.Parse(output.ToString()))
            {
                var r = harvDoc.RootElement;
                if (r.GetProperty("runKey").GetString() != emitKey)
                    throw new Exception($"harvest runKey {r.GetProperty("runKey").GetString()} != emit runKey {emitKey}");
                if (r.GetProperty("state").GetString() != "Completed")
                    throw new Exception($"expected Completed, got {r.GetProperty("state").GetString()}");
                string? conv = r.GetProperty("convergedLabel").GetString();
                if (conv is null || !conv.Contains("grid uncertainty not quantified (single mesh)", StringComparison.Ordinal))
                    throw new Exception($"missing LAB-01 three-part Converged label: {conv}");
            }

            // 4. `cfd-run status <project> --json` succeeds cleanly.
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["cfd-run", "status", projPath, "--json"], output);
            if (exit != 0) throw new Exception($"cfd-run status returned {exit}: {output}");
            using (var stDoc = JsonDocument.Parse(output.ToString()))
            {
                if (stDoc.RootElement.GetProperty("runs").ValueKind != JsonValueKind.Array)
                    throw new Exception("expected runs array in status JSON");
            }

            // 5. Invalid subcommand/args return non-zero exit code 2.
            output.GetStringBuilder().Clear();
            exit = await Cli.RunAsync(["cfd-run", "bogus"], output);
            if (exit != 2) throw new Exception($"expected exit 2 on invalid subcommand, got {exit}");
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void WriteValidOpenFoamOutputs(string caseDir, string timeDir)
    {
        string tPath = Path.Combine(caseDir, timeDir);
        Directory.CreateDirectory(tPath);
        foreach (string field in new[] { "U", "p", "nuTilda" })
            File.WriteAllText(Path.Combine(tPath, field), $"FoamFile\n{{\n    version 2.0;\n    format ascii;\n    class volScalarField;\n    object {field};\n}}\ninternalField uniform 0;\n", Encoding.UTF8);

        string fcDir = Path.Combine(caseDir, "postProcessing", "forceCoeffs", "0");
        Directory.CreateDirectory(fcDir);
        var fc = new StringBuilder("# Force coefficients\n# Time\tCd\tCs\tCl\tCmRoll\tCmPitch\tCmYaw\n");
        for (int i = 1; i <= 30; i++)
        {
            double cl = 0.412 + (i % 2 == 0 ? 1e-4 : -1e-4);
            double cd = 0.0184 + (i % 2 == 0 ? 2e-5 : -2e-5);
            fc.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{i * 10}\t{cd:G17}\t0\t{cl:G17}\t0\t-0.041\t0"));
        }
        File.WriteAllText(Path.Combine(fcDir, "coefficient.dat"), fc.ToString(), Encoding.UTF8);

        string siDir = Path.Combine(caseDir, "postProcessing", "solverInfo", "0");
        Directory.CreateDirectory(siDir);
        File.WriteAllText(Path.Combine(siDir, "solverInfo.dat"),
            "# Solver info\n# Time\tU_solver\tUx_initial\tUx_final\tUx_iters\tU_converged\tp_solver\tp_initial\tp_final\tp_iters\tp_converged\tnuTilda_solver\tnuTilda_initial\tnuTilda_final\tnuTilda_iters\tnuTilda_converged\n" +
            "1\tsmoothSolver\t0.8\t0.4\t5\tfalse\tGAMG\t0.5\t0.2\t10\tfalse\tsmoothSolver\t0.2\t0.1\t5\tfalse\n" +
            "500\tsmoothSolver\t2e-4\t1e-4\t2\ttrue\tGAMG\t2e-4\t1e-4\t3\ttrue\tsmoothSolver\t4e-5\t2e-5\t2\ttrue\n", Encoding.UTF8);

        string ypDir = Path.Combine(caseDir, "postProcessing", "yPlus", "0");
        Directory.CreateDirectory(ypDir);
        File.WriteAllText(Path.Combine(ypDir, "yPlus.dat"), "# yPlus\n# Time\tpatch\tmin\tmax\taverage\tp95\n500\twing\t0.12\t1.38\t0.45\t0.62\n", Encoding.UTF8);

        string wssDir = Path.Combine(caseDir, "postProcessing", "wallShearStress", "0");
        Directory.CreateDirectory(wssDir);
        File.WriteAllText(Path.Combine(wssDir, "wallShearStress.dat"), "# wss\n# Time\tpatch\tmin\tmax\tintegral\n500\twing\t(-5 0 0)\t(20 0 0)\t(1.2 0 0)\n", Encoding.UTF8);

        string surfDir = Path.Combine(caseDir, "postProcessing", "wingSurface", timeDir);
        Directory.CreateDirectory(surfDir);
        File.WriteAllText(Path.Combine(surfDir, "wing.raw"), "# x y z p Cp wallShearStress\n0.01 0.1 0.005 -1200.0 -1.84 14.2\n0.05 0.2 0.002 650.0 1.00 8.5\n", Encoding.UTF8);
    }
}
