using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using System.Reflection;
using System.Text.Json;

namespace CfdWorkbench.Cli;

public static class Cli
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static byte[] ExampleBytes()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CfdWorkbench.Example.foil")
            ?? throw new ContractError("DOC-EXAMPLE-MISSING");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static async Task<byte[]> ReadFoilBoundedAsync(string path, CancellationToken cancellation = default)
    {
        // The core's 1 MiB source ceiling is checked before allocating beyond one sentinel byte.
        const int maxBytes = 1_048_576;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 8192, options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maxBytes) throw new ContractError("DSL-LIMIT");
        var buffer = new byte[maxBytes + 1];
        int used = 0;
        while (used < buffer.Length)
        {
            int count = await stream.ReadAsync(buffer.AsMemory(used), cancellation);
            if (count == 0) return buffer.AsSpan(0, used).ToArray();
            used += count;
        }
        throw new ContractError("DSL-LIMIT");
    }

    public static int ExitForCode(string code) => code switch
    {
        "DOC-CANCELLED" or "GEOMETRY-CANCELLED" => 130,
        "DOC-VERSION" or "DOC-UNSUPPORTED-FIELD" or "DOC-UNSUPPORTED-PERSISTENCE" or
            "DSL-VERSION" or "DSL-UNSUPPORTED" => 3,
        "GEOMETRY-BUDGET" or "DSL-NOT-ASSESSED" or "DSL-LIMIT" => 4,
        "DOC-IO" or "DOC-CONFLICT" or "DOC-SAVE-UNCERTAIN" => 5,
        _ => 2
    };

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation = default,
        AnalysisHost? analysis = null)
    {
        if (args.Length == 4 && args[0] == "analyse" && args[2] == "--op") return await AnalyseAsync(args[1], args[3], output, analysis, cancellation);
        if (args.Length == 3 && args[0] == "inspect" && args[2] == "--runs") return await InspectRunsAsync(args[1], output, cancellation);
        if (args.Length != 3 || args[0] != "inspect" || args[2] != "--json")
        {
            await output.WriteLineAsync("Usage: cfd-workbench inspect <example|path.foil|path.cfdw.json> --json");
            return 2;
        }
        string input = args[1];
        bool example = input == "example";
        bool native = input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase);
        if (!example && !native && !input.EndsWith(".foil", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }
        try
        {
            cancellation.ThrowIfCancellationRequested();
            using var session = new AuthoringSession();
            byte[] source;
            if (native)
            {
                using var store = new ProjectStore(session);
                var read = await store.ReadAsync(input, cancellation);
                session.Reopen(read.Image);
                source = session.Snapshot().Source;
            }
            else
            {
                source = example ? ExampleBytes() : await ReadFoilBoundedAsync(input, cancellation);
                var parsed = FoilSource.Parse(source);
                if (!parsed.IsParsed)
                {
                    string code = parsed.Diagnostics.FirstOrDefault()?.Code ?? "DSL-INVALID";
                    await WriteAsync(output, new { schemaVersion = 1, code, diagnostics = parsed.Diagnostics });
                    return ExitForCode(code);
                }
                byte[] candidate = FoilSource.MaterializeIds(parsed);
                if (!candidate.AsSpan().SequenceEqual(source))
                {
                    await WriteAsync(output, new { schemaVersion = 1, code = "DSL-IDS-REQUIRED", candidateSource = System.Text.Encoding.UTF8.GetString(candidate), diagnostics = parsed.Diagnostics });
                    return 2;
                }
                var admission = Geometry.Assess(parsed);
                if (admission.Status != GeometryStatus.Certified)
                {
                    await WriteAsync(output, new { schemaVersion = 1, code = admission.Code, assessment = new { status = admission.Status.ToString(), admission.Reason }, diagnostics = parsed.Diagnostics });
                    return admission.Status == GeometryStatus.Unsupported ? 3 : admission.Status == GeometryStatus.NotAssessed ? 4 : 2;
                }
                session.Open(source, Guid.NewGuid().ToString("D"), false);
            }
            var inspection = session.InspectAccepted();
            var view = session.Snapshot();
            var certificate = inspection.Geometry.Certificate;
            var points = new List<object>();
            if (certificate is not null)
            {
                foreach (double eta in new[] { 0d, .5d, 1d })
                {
                    foreach (var sample in new (double X, bool Upper)[] { (0, true), (.5, true), (1, true), (.5, false), (1, false) })
                    {
                        var point = Geometry.PointAt(certificate, eta, sample.X, sample.Upper, cancellationToken: cancellation);
                        points.Add(new { eta, normalizedX = sample.X, side = sample.Upper ? "upper" : "lower", xMeters = point.X, yMeters = point.Y, zMeters = point.Z });
                    }
                }
            }
            var section = certificate is null ? null : Geometry.SectionAt(certificate, .5, .5, cancellationToken: cancellation);
            object? pointModel = null;
            {
                var plan = Planform.View(source, "accepted", 0);
                static object Rail(CurveView curve) => curve.Points.Select(point => new
                {
                    id = point.Id,
                    role = point.Role.ToString(),
                    kind = point.Kind?.ToString(),
                    locks = point.Locks,
                    freedom = point.Freedom.ToString(),
                    ordinate = point.Ordinate,
                    aftMeters = point.Ordinate
                }).ToArray();
                pointModel = new
                {
                    leading = Rail(plan.Leading),
                    trailing = Rail(plan.Trailing),
                    dihedral = Rail(Channels.View(source, "dihedral", "accepted", 0)),
                    twist = Rail(Channels.View(source, "twist", "accepted", 0)),
                    thickness = Rail(Channels.View(source, "thickness", "accepted", 0))
                };
            }
            await WriteAsync(output, new
            {
                schemaVersion = 1,
                points = pointModel,
                sectionPoints = Sections.Points(source),
                acceptedSourceSha256 = view.SourceHash,
                definitionHash = view.SurfaceHash,
                evaluator = inspection.Authored.Binding.Evaluator,
                projectId = session.Envelope().ProjectId,
                designId = inspection.Authored.Binding.DesignId,
                acceptedId = view.AcceptedId,
                assessment = new { status = inspection.Geometry.Status.ToString(), code = inspection.Geometry.Code, reason = inspection.Geometry.Reason,
                    placementErrorMetersUpper = certificate?.PlacementWidthUpper, samplingError = "Not assessed" },
                diagnostics = inspection.Authored.Diagnostics,
                derived = new { points, pointUnit = "m", centerSection = section, centerSectionEta = .5,
                    centerSectionNormalizedX = .5, centerSectionUnit = "z/c", provenance = "accepted" },
                localEvents = session.ReadLocalEvents().Select(e => new { e.Operation, e.Outcome, e.DurationMilliseconds, e.InputBytes, e.OutputBytes, e.Generation, e.PublicationKnown, e.DurabilityConfirmed })
            });
            return inspection.Geometry.Status switch { GeometryStatus.Certified => 0, GeometryStatus.Unsupported => 3, _ => 4 };
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error)
        {
            await WriteAsync(output, new { code = error.Code });
            return ExitForCode(error.Code);
        }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    private static Task WriteAsync(TextWriter output, object value) => output.WriteLineAsync(JsonSerializer.Serialize(value, Json));

    /// <summary>
    /// <c>analyse &lt;example|path.foil|path.cfdw.json&gt; --op &lt;json&gt;</c>: one evaluation of the accepted revision through
    /// the GUI's service and the GUI's operating-point builder, so the run key is the GUI's (CLI-01). Prints the run; the
    /// file is not written. <c>op</c> holds <c>speed</c> (m/s) and <c>alphaDeg</c>, optionally <c>hRef</c> (m) and
    /// <c>water</c> {<c>temperatureC</c>, <c>salinityGPerKg</c>}; any other member is refused (<c>ANA-INPUT-OP</c>).
    /// </summary>
    private static async Task<int> AnalyseAsync(string input, string op, TextWriter output, AnalysisHost? analysis,
        CancellationToken cancellation)
    {
        try
        {
            cancellation.ThrowIfCancellationRequested();
            var (point, temperatureC, salinity) = ParseOp(op);
            // simplify: the product VLM + strip method composes VLM's lattice and STP's coupler, polar stub, water table
            // and reference quantities, which are not on this branch, so the shipped binary has no method to run. Upgrade
            // trigger: VLM and STP joined — Program.Main passes the product AnalysisHost.
            if (analysis is null) throw new ContractError("ANA-METHOD-UNAVAILABLE", "no wing method is installed in this build");
            using var session = await OpenAsync(input, cancellation);
            var service = new AnalysisService(session, analysis.Method);
            var run = await service.EvaluateAsync(point, analysis.Water(temperatureC, salinity), Tier.VlmStrip, new Scope.Wing(), cancellation);
            await WriteAsync(output, new { schemaVersion = 1, run });
            return run.Outcome is RunOutcome.Failed failed ? ExitForCode(failed.Code) : 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error) { await WriteAsync(output, new { code = error.Code, reason = error.Reason }); return ExitForCode(error.Code); }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    /// <summary>
    /// <c>inspect &lt;path.cfdw.json&gt; --runs</c>: every stored run in document order with its key recomputed from the
    /// manifest (never the stored one), its integrity and its revision ordinal; and the tombstones retention left.
    /// </summary>
    private static async Task<int> InspectRunsAsync(string input, TextWriter output, CancellationToken cancellation)
    {
        if (!input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync("{\"code\":\"DOC-TYPE\"}");
            return 2;
        }
        try
        {
            using var session = await OpenAsync(input, cancellation);
            var ledger = session.ReadRuns();
            var runs = ledger.Runs.Select(stored => new
            {
                stored.Run.RunId,
                runKey = RunRecord.RecomputedKey(stored.Run),
                integrity = stored.Integrity.ToString(),
                outcome = stored.Run.Outcome is RunOutcome.Failed failed ? failed.Code : "Completed",
                stored.Run.Tier,
                method = new { stored.Run.Method.Id, stored.Run.Method.Version },
                stored.Run.Inputs.AcceptedId,
                revision = session.RevisionOf(stored.Run.Inputs.AcceptedId).Ordinal,
                stored.Run.Op,
                water = new { stored.Run.Water.TemperatureC, stored.Run.Water.SalinityGPerKg },
                strips = stored.Run.Strips.Count
            }).ToArray();
            await WriteAsync(output, new { schemaVersion = 1, runs, pruned = ledger.Pruned.Select(tombstone => new { tombstone.RunId, tombstone.RunKey }) });
            return 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error) { await WriteAsync(output, new { code = error.Code }); return ExitForCode(error.Code); }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    // The accepted revision of a native document, the Example or a .foil (the inspect path's three inputs).
    private static async Task<AuthoringSession> OpenAsync(string input, CancellationToken cancellation)
    {
        var session = new AuthoringSession();
        try
        {
            if (input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
            {
                using var store = new ProjectStore(session);
                session.Reopen((await store.ReadAsync(input, cancellation)).Image);
            }
            else if (input == "example" || input.EndsWith(".foil", StringComparison.OrdinalIgnoreCase))
            {
                byte[] source = input == "example" ? ExampleBytes() : await ReadFoilBoundedAsync(input, cancellation);
                session.Open(source, Guid.NewGuid().ToString("D"), false);
            }
            else throw new ContractError("DOC-TYPE");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    private static (OperatingPoint Op, double TemperatureC, double Salinity) ParseOp(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Malformed("the operating point is not an object");
            string[] allowed = ["speed", "alphaDeg", "hRef", "water"];
            if (root.EnumerateObject().Any(member => !allowed.Contains(member.Name, StringComparer.Ordinal)))
                throw Malformed("the operating point has an unknown member");
            double? hRef = root.TryGetProperty("hRef", out var depth) && depth.ValueKind != JsonValueKind.Null ? Number(depth) : null;
            double temperatureC = OperatingPoints.DefaultTemperatureC, salinity = OperatingPoints.SaltSalinityGPerKg;
            if (root.TryGetProperty("water", out var water))
            {
                if (water.ValueKind != JsonValueKind.Object ||
                    water.EnumerateObject().Any(member => member.Name is not ("temperatureC" or "salinityGPerKg")))
                    throw Malformed("the water member has an unknown shape");
                if (water.TryGetProperty("temperatureC", out var t)) temperatureC = Number(t);
                if (water.TryGetProperty("salinityGPerKg", out var s)) salinity = Number(s);
            }
            if (!root.TryGetProperty("speed", out var speed) || !root.TryGetProperty("alphaDeg", out var alpha))
                throw Malformed("speed and alphaDeg are required");
            var point = OperatingPoints.Custom(Number(speed), Number(alpha), hRef);
            OperatingPoints.Validate(point);
            return (point, temperatureC, salinity);
        }
        catch (JsonException) { throw Malformed("the operating point is not JSON"); }

        static double Number(JsonElement element) =>
            element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double value) ? value : throw Malformed("a member is not a number");
        static ContractError Malformed(string reason) => new("ANA-INPUT-OP", reason);
    }
}

/// <summary>
/// What <c>analyse</c> runs: the wing method and the water-table lookup (temperature °C, salinity g/kg). The composition
/// root passes it; a check passes its own method (design §18.2: in process via <see cref="Cli.RunAsync"/>).
/// </summary>
public sealed record AnalysisHost(IWingMethod Method, Func<double, double, WaterRecord> Water);

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try { return await Cli.RunAsync(args, Console.Out, cancellation.Token); }
        finally { Console.CancelKeyPress -= handler; }
    }
}
