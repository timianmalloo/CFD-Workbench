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

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation = default)
    {
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
                        var point = Geometry.PointAt(certificate, eta, sample.X, sample.Upper, timeBudget: TimeSpan.FromSeconds(1), cancellationToken: cancellation);
                        points.Add(new { eta, normalizedX = sample.X, side = sample.Upper ? "upper" : "lower", xMeters = point.X, yMeters = point.Y, zMeters = point.Z });
                    }
                }
            }
            var section = certificate is null ? null : Geometry.SectionAt(certificate, .5, .5, TimeSpan.FromSeconds(1), cancellation);
            await WriteAsync(output, new
            {
                schemaVersion = 1,
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
}

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
