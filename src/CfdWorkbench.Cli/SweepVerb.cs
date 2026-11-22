using System.Text.Json;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>sweep preview</c>, <c>sweep run [--out &lt;path.cfdw.json&gt;]</c>, and <c>inspect &lt;path.cfdw.json&gt; --sweeps</c>
/// (docs/design/local-sweeps.md §4, Ruling 271, CLI-01). Shares exact <see cref="SweepSchedule"/>,
/// <see cref="SweepService"/>, <see cref="SweepProjection"/>, and <see cref="RunRecord"/> code with the GUI.
/// </summary>
internal static class SweepVerb
{
    private sealed record GridSpec(
        IReadOnlyList<double> SpeedsMetersPerSecond,
        IReadOnlyList<double> AnglesDeg,
        double? HRef,
        double TemperatureC,
        double SalinityGPerKg,
        string Tier);

    internal static async Task<int> RunSweepAsync(string[] args, TextWriter output, AnalysisHost? analysis, CancellationToken cancellation)
    {
        if (args.Length < 2 || args[1] is not ("preview" or "run"))
        {
            await PrintUsageAsync(output);
            return 2;
        }
        bool isRun = args[1] == "run";
        string? input = null;
        string? gridJson = null;
        string? outPath = null;

        if (args.Length < 3 || args[2].StartsWith("--", StringComparison.Ordinal))
        {
            await PrintUsageAsync(output);
            return 2;
        }
        input = args[2];
        for (int i = 3; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length) { await PrintUsageAsync(output); return 2; }
            switch (args[i])
            {
                case "--grid": gridJson = args[i + 1]; break;
                case "--out" when isRun: outPath = args[i + 1]; break;
                default: await PrintUsageAsync(output); return 2;
            }
        }
        if (gridJson is null) { await PrintUsageAsync(output); return 2; }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            if (analysis is null) throw new ContractError("ANA-METHOD-UNAVAILABLE", "no wing method is installed in this build");
            var grid = ParseGrid(gridJson);
            var water = analysis.Water(grid.TemperatureC, grid.SalinityGPerKg);

            using var session = await Cli.OpenAsync(input, cancellation);
            var preview = SweepSchedule.BuildCartesian(
                session, analysis.Method, grid.Tier, grid.SpeedsMetersPerSecond, grid.AnglesDeg, water, grid.HRef);

            if (!isRun)
            {
                await Cli.WriteAsync(output, new
                {
                    schemaVersion = 1,
                    caseCount = preview.Cases.Count,
                    cases = preview.Rows.Select(r => new
                    {
                        caseIndex = r.CaseIndex,
                        speedMs = r.SpeedMetersPerSecond,
                        alphaDeg = r.AlphaDeg,
                        hRef = r.HRefMeters,
                        q = r.Conditions.Q.Value,
                        reRef = r.Conditions.ReRef.Value,
                        hOverC = r.Conditions.DepthOverChord.Value,
                        froudeDepth = r.Conditions.FroudeDepth.Value,
                        sigma = r.Conditions.Sigma.Value,
                        cacheHit = r.CacheHit
                    }).ToArray(),
                    estimatedStorageBytes = preview.EstimatedStorageBytes,
                    documentHeadroomBytes = preview.DocumentHeadroomBytes
                });
                return 0;
            }

            var service = new SweepService(session, analysis.Method);
            var queued = service.Queue(preview, "CLI local sweep");
            var outcome = await service.RunAsync(queued.Id, cancellation);
            var stored = session.ReadExperiments().Single(e => e.Experiment.Id == queued.Id);
            var projected = SweepProjection.Project(session, stored, analysis.Method);

            if (outPath is not null)
            {
                string fullOut = Path.GetFullPath(outPath);
                byte[] image = session.SaveImage();
                using var store = new ProjectStore(session);
                string? expectedSha = null;
                if (File.Exists(fullOut))
                    expectedSha = (await store.ReadAsync(fullOut, cancellation)).DiskSha256;
                var save = await store.SaveAsync(fullOut, new SaveRequest(image, expectedSha, Guid.NewGuid().ToString("D")), cancellation);
                if (save.Code != "OK") throw new ContractError(save.Code, "could not write output project");
                session.AcknowledgeSaved(image);
            }

            await Cli.WriteAsync(output, new
            {
                schemaVersion = 1,
                experimentId = outcome.Experiment.Id,
                status = outcome.Status.ToString(),
                cases = projected.Cases.Select(c => new
                {
                    caseIndex = c.CaseIndex,
                    state = c.State.ToString(),
                    runId = c.RunId,
                    runKey = c.RunKey,
                    cl = c.Cl,
                    cdi = c.Cdi,
                    cdWingNcrit2 = c.CdWingNcrit2,
                    cdWingNcrit4 = c.CdWingNcrit4,
                    ldWingNcrit2 = c.LdWingNcrit2,
                    ldWingNcrit4 = c.LdWingNcrit4,
                    cm = c.Cm,
                    failureCode = c.FailureCode,
                    failureReason = c.FailureReason
                }).ToArray()
            });

            if (outcome.Cancelled) return 130;
            return outcome.Status == SweepExperimentStatus.Failed ? 2 : 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error)
        {
            await Cli.WriteAsync(output, new { code = error.Code, reason = error.Reason });
            return Cli.ExitForCode(error.Code);
        }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    internal static async Task<int> InspectSweepsAsync(string input, TextWriter output, AnalysisHost? analysis, CancellationToken cancellation)
    {
        if (!input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }
        try
        {
            cancellation.ThrowIfCancellationRequested();
            using var session = await Cli.OpenAsync(input, cancellation);
            var method = analysis?.Method ?? new ProductWingMethod();
            var storedList = session.ReadExperiments();
            var sweeps = storedList.Select(stored =>
            {
                var proj = SweepProjection.Project(session, stored, method);
                var exp = stored.Experiment;
                return new
                {
                    experimentId = exp.Id,
                    seriesId = exp.SeriesId,
                    version = exp.Version,
                    name = exp.Name,
                    tier = exp.Definition.Tier,
                    sourceKind = exp.Definition.SourceKind,
                    definitionHash = SweepRecords.DefinitionHash(exp.Definition),
                    integrity = stored.Integrity.ToString(),
                    status = proj.Status.Status.ToString(),
                    archived = exp.IsArchived,
                    cases = proj.Cases.Select(c => new
                    {
                        caseIndex = c.CaseIndex,
                        state = c.State.ToString(),
                        attemptCount = c.AttemptCount,
                        runId = c.RunId,
                        runKey = c.RunKey,
                        freshness = c.Freshness.ToString(),
                        failureCode = c.FailureCode
                    }).ToArray()
                };
            }).ToArray();

            await Cli.WriteAsync(output, new { schemaVersion = 1, sweeps });
            return 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error) { await Cli.WriteAsync(output, new { code = error.Code, reason = error.Reason }); return Cli.ExitForCode(error.Code); }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    private static GridSpec ParseGrid(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Malformed("grid must be a JSON object");
            string[] allowed = ["speedsKn", "speedsMs", "anglesDeg", "hRef", "water", "tier"];
            if (root.EnumerateObject().Any(m => !allowed.Contains(m.Name, StringComparer.Ordinal)))
                throw Malformed("grid has an unknown member");

            bool hasKn = root.TryGetProperty("speedsKn", out var knEl);
            bool hasMs = root.TryGetProperty("speedsMs", out var msEl);
            if (hasKn == hasMs) throw Malformed("provide exactly one of speedsKn or speedsMs");
            double[] rawSpeeds = hasKn ? NumberArray(knEl, "speedsKn") : NumberArray(msEl, "speedsMs");
            double[] speedsMs = hasKn
                ? rawSpeeds.Select(v => SweepSchedule.ToMetersPerSecond(v, SweepSpeedUnit.Knots)).ToArray()
                : rawSpeeds;

            if (!root.TryGetProperty("anglesDeg", out var angEl))
                throw Malformed("anglesDeg is required");
            double[] angles = NumberArray(angEl, "anglesDeg");

            double? hRef = root.TryGetProperty("hRef", out var hEl) && hEl.ValueKind != JsonValueKind.Null
                ? Number(hEl, "hRef") : null;

            double temperatureC = OperatingPoints.DefaultTemperatureC;
            double salinity = OperatingPoints.SaltSalinityGPerKg;
            if (root.TryGetProperty("water", out var waterEl) && waterEl.ValueKind != JsonValueKind.Null)
            {
                if (waterEl.ValueKind != JsonValueKind.Object ||
                    waterEl.EnumerateObject().Any(m => m.Name is not ("temperatureC" or "salinityGPerKg")))
                    throw Malformed("water has an unknown shape");
                if (waterEl.TryGetProperty("temperatureC", out var tEl)) temperatureC = Number(tEl, "temperatureC");
                if (waterEl.TryGetProperty("salinityGPerKg", out var sEl)) salinity = Number(sEl, "salinityGPerKg");
            }

            string tier = SweepRecords.TierVlmStrip;
            if (root.TryGetProperty("tier", out var tierEl) && tierEl.ValueKind != JsonValueKind.Null)
            {
                if (tierEl.ValueKind != JsonValueKind.String) throw Malformed("tier must be a string");
                tier = tierEl.GetString()!;
            }

            return new GridSpec(speedsMs, angles, hRef, temperatureC, salinity, tier);
        }
        catch (JsonException) { throw Malformed("grid is not valid JSON"); }

        static double[] NumberArray(JsonElement el, string name)
        {
            if (el.ValueKind != JsonValueKind.Array) throw Malformed($"{name} must be an array");
            var list = new List<double>();
            foreach (var item in el.EnumerateArray()) list.Add(Number(item, name));
            return list.ToArray();
        }
        static double Number(JsonElement el, string name) =>
            el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out double value)
                ? value
                : throw Malformed($"{name} contains a non-numeric value");
        static ContractError Malformed(string reason) => new("ANA-INPUT-OP", reason);
    }

    private static async Task PrintUsageAsync(TextWriter output)
    {
        await output.WriteLineAsync("Usage: cfd-workbench sweep preview <example|path.foil|path.cfdw.json> --grid <json>");
        await output.WriteLineAsync("       cfd-workbench sweep run <example|path.foil|path.cfdw.json> --grid <json> [--out <path.cfdw.json>]");
        await output.WriteLineAsync("       cfd-workbench inspect <path.cfdw.json> --sweeps");
    }
}
