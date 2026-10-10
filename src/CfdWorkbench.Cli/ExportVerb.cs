using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;
using System.Text;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>export &lt;example|path.foil|path.cfdw.json&gt; --format dat|stl --out &lt;path&gt; [options]</c> (Export design 6.1, Ruling 194 (4)).
/// It drives the dialog's own <see cref="ExportSession"/>, so the writers, the file name rules, the summary rows and the
/// hardened write path (symlink refused, exclusive flushed temp file, publish by rename, a forced extension never replaces)
/// are the dialog's, not copies. It prints plain text; the exit codes are listed in <see cref="Usage"/>.
/// </summary>
internal static class ExportVerb
{
    // Exit codes beyond the CLI's shared ones (2 usage or input, 3 unsupported, 4 not assessed, 5 I/O, 130 cancelled).
    internal const int ExitUsage = 2, ExitFormatUnavailable = 3, ExitGeometryNotAccepted = 4, ExitIo = 5, ExitTargetLink = 6,
        ExitMeshNotClosed = 7, ExitWouldReplace = 8;

    internal const string Usage =
        "Usage: cfd-workbench export <example|path.foil|path.cfdw.json> --format dat|stl --out <path>\n" +
        "         [--shape at|own] [--station N] [--order selig|lednicer] [--points 61|101|201]   (dat only)\n" +
        "         [--scope whole|half] [--tolerance draft|print|fine]                             (stl only)\n" +
        "  --station N is the 0-based index of an authored station (0 is the root); the default is 0.\n" +
        "  Defaults: --shape at, --order selig, --points 101, --scope whole, --tolerance print.\n" +
        "  The extension of --out is forced to .dat or .stl; an existing file of the exact --out name is replaced,\n" +
        "  a forced name that already exists is never replaced.\n" +
        "Exit codes: 0 written (a trailing edge below the floor is advised and still written); 2 usage, bad option or input;\n" +
        "  3 format not available yet (3mf) or input unsupported; 4 geometry not accepted; 5 path not writable;\n" +
        "  6 target is a symlink; 7 mesh did not close, nothing written; 8 forced-extension name exists; 130 cancelled.";

    private sealed record Options(string Input, ExportFormat Format, string Out, DatShape Shape, int? Station, DatOrder Order,
        int Points, StlScope Scope, StlPreset Preset);

    private sealed class UsageError(string message) : Exception(message);

    public static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation)
    {
        Options options;
        try
        {
            options = Parse(args);
        }
        catch (UsageError error)
        {
            await output.WriteLineAsync("Error EXPORT-USAGE: " + error.Message);
            await output.WriteLineAsync(Usage);
            return ExitUsage;
        }
        catch (FormatUnavailable)
        {
            await output.WriteLineAsync("Error EXPORT-FORMAT-UNAVAILABLE: 3mf is not available yet. Use --format dat or stl.");
            return ExitFormatUnavailable;
        }
        try
        {
            cancellation.ThrowIfCancellationRequested();
            using var session = await OpenAsync(options.Input, cancellation);
            return await ExportAsync(session, options, output, cancellation);
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("Error DOC-CANCELLED: Export cancelled. Nothing was written."); return 130; }
        catch (ContractError error)
        {
            await output.WriteLineAsync($"Error {error.Code}: {(string.IsNullOrEmpty(error.Reason) ? "the input could not be opened." : error.Reason)}");
            return Cli.ExitForCode(error.Code);
        }
        catch (IOException) { await output.WriteLineAsync("Error DOC-IO: the input could not be read."); return ExitIo; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("Error DOC-IO: the input could not be read."); return ExitIo; }
    }

    private static Options Parse(string[] args)
    {
        if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal)) throw new UsageError("the input file is missing.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] known = ["--format", "--out", "--shape", "--station", "--order", "--points", "--scope", "--tolerance"];
        for (int index = 2; index < args.Length; index += 2)
        {
            string name = args[index];
            if (!known.Contains(name, StringComparer.Ordinal)) throw new UsageError($"unknown option {name}.");
            if (index + 1 >= args.Length) throw new UsageError($"{name} needs a value.");
            if (!values.TryAdd(name, args[index + 1])) throw new UsageError($"{name} is given twice.");
        }
        if (!values.TryGetValue("--format", out string? formatText)) throw new UsageError("--format is required.");
        if (!values.TryGetValue("--out", out string? path) || path.Length == 0) throw new UsageError("--out is required.");
        // 3mf is a known format whose writer has not landed (track TMF); it is refused by name, not as an unknown value.
        if (formatText == "3mf") throw new FormatUnavailable();
        var format = formatText switch { "dat" => ExportFormat.Dat, "stl" => ExportFormat.Stl, _ => throw new UsageError($"--format {formatText} is not dat or stl.") };
        string[] datOnly = ["--shape", "--station", "--order", "--points"], stlOnly = ["--scope", "--tolerance"];
        foreach (string name in format == ExportFormat.Dat ? stlOnly : datOnly)
            if (values.ContainsKey(name)) throw new UsageError($"{name} does not apply to --format {formatText}.");
        return new Options(args[1], format, path,
            Choice(values, "--shape", DatShape.AtStation, ("at", DatShape.AtStation), ("own", DatShape.Own)),
            values.TryGetValue("--station", out string? station)
                ? int.TryParse(station, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int stationIndex)
                    ? stationIndex : throw new UsageError("--station is not a whole number.")
                : null,
            Choice(values, "--order", DatOrder.Selig, ("selig", DatOrder.Selig), ("lednicer", DatOrder.Lednicer)),
            values.TryGetValue("--points", out string? points)
                ? int.TryParse(points, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int count) &&
                  DatExport.PointChoices.Contains(count) ? count : throw new UsageError("--points is not 61, 101 or 201.")
                : 101,
            Choice(values, "--scope", StlScope.Whole, ("whole", StlScope.Whole), ("half", StlScope.Half)),
            Choice(values, "--tolerance", StlPreset.Print, ("draft", StlPreset.Draft), ("print", StlPreset.Print), ("fine", StlPreset.Fine)));
    }

    private sealed class FormatUnavailable : Exception;

    private static T Choice<T>(Dictionary<string, string> values, string name, T fallback, params (string Word, T Value)[] words)
    {
        if (!values.TryGetValue(name, out string? text)) return fallback;
        foreach (var (word, value) in words)
            if (text == word) return value;
        throw new UsageError($"{name} {text} is not one of {string.Join(", ", words.Select(w => w.Word))}.");
    }

    private static async Task<AuthoringSession> OpenAsync(string input, CancellationToken cancellation)
    {
        var session = new AuthoringSession();
        try
        {
            if (input.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))
            {
                using var store = new CfdWorkbench.Persistence.ProjectStore(session);
                session.Reopen((await store.ReadAsync(input, cancellation)).Image);
            }
            else if (input == "example" || input.EndsWith(".foil", StringComparison.OrdinalIgnoreCase))
                session.Open(input == "example" ? Cli.ExampleBytes() : await Cli.ReadFoilBoundedAsync(input, cancellation), Guid.NewGuid().ToString("D"), true);
            else throw new ContractError("DOC-TYPE");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    // The same source the dialog reads (WorkbenchController.ExportSnapshot): the accepted revision, its stations, their names.
    private static ExportSource Snapshot(AuthoringSession session)
    {
        var inspection = session.InspectAccepted();
        var view = session.Snapshot();
        var stations = new List<ExportStation>();
        for (int index = 0; index < inspection.Authored.Assignments.Count; index++)
        {
            double eta = inspection.Authored.Assignments[index].Eta;
            var frame = Placement.Frame(view.Source, eta);
            string name = eta == 0 ? "Root" : eta == 1 ? "Tip" : $"Station {index + 1}";
            stations.Add(new(name, frame.ChordMeters * 1000, frame.ThicknessRatio * 100));
        }
        return new(view.Source, inspection.Authored.Name ?? "foil", session.RevisionOf(view.AcceptedId).Ordinal, inspection.Geometry.Status,
            false, false, stations, 0, null);
    }

    private static async Task<int> ExportAsync(AuthoringSession authoring, Options options, TextWriter output, CancellationToken cancellation)
    {
        var source = Snapshot(authoring);
        var session = new ExportSession(source);
        // A foil whose geometry is not accepted is refused when it is opened (exit 3 or 4 by its code); this guards the session's own
        // rule (H2) should the opener ever admit one.
        if (session.BlockedReason is not null)
        {
            await output.WriteLineAsync("Error EXPORT-GEOMETRY-NOT-ACCEPTED: " + ExportCopy.Blocked);
            return ExitGeometryNotAccepted;
        }
        int station = options.Station ?? 0;
        if (station >= source.Stations.Count)
        {
            await output.WriteLineAsync($"Error EXPORT-USAGE: --station {station} is out of range; the stations are " +
                string.Join(", ", source.Stations.Select((s, i) => $"{i} ({s.Name})")) + ".");
            return ExitUsage;
        }
        session.Set(options.Shape, options.Order, options.Points, station, options.Format, options.Scope, options.Preset);
        await session.PrepareAsync(cancellation);
        if (session.ClosureFailed)
        {
            await output.WriteLineAsync("Error EXPORT-NOT-CLOSED: " + ExportCopy.MeshNotClosed);
            return ExitMeshNotClosed;
        }
        // An existing folder as --out takes the dialog's suggested name (design 6.4), e.g. basic-foil-r1-half-mm.stl.
        string chosen = Directory.Exists(options.Out) ? Path.Combine(options.Out, session.FileName) : options.Out;
        string path = ExportSession.ForceExtension(chosen, session.Format == ExportFormat.Dat ? ".dat" : ".stl");
        if (path != chosen && (File.Exists(path) || Directory.Exists(path)))
        {
            await output.WriteLineAsync($"Error EXPORT-WOULD-REPLACE: {path} exists and was not the name you gave, so it was not replaced. Nothing was written.");
            return ExitWouldReplace;
        }
        if ((File.Exists(chosen) || Directory.Exists(chosen)) && (File.GetAttributes(chosen) & FileAttributes.ReparsePoint) != 0)
        {
            await output.WriteLineAsync("Error EXPORT-TARGET-LINK: the target is a symbolic link. Nothing was written.");
            return ExitTargetLink;
        }
        await output.WriteAsync(Summary(session));
        var outcome = await session.RunAsync((_, _) => Task.FromResult<string?>(chosen), cancellation: cancellation);
        switch (outcome.Kind)
        {
            case ExportOutcomeKind.Written:
                await output.WriteLineAsync(outcome.Message);
                await output.WriteLineAsync("Path: " + Path.GetFullPath(outcome.Path!));
                return 0;
            case ExportOutcomeKind.Cancelled:
                await output.WriteLineAsync("Error DOC-CANCELLED: " + outcome.Message);
                return 130;
            default:
                await output.WriteLineAsync("Error DOC-IO: " + outcome.Message);
                return ExitIo;
        }
    }

    /// <summary>The dialog's "What will be written" block as plain text: one <c>Label: value</c> line per row (a second line of a value is
    /// indented), the limit, the advisory when the trailing edge is below the floor, then the fixed safety statement.</summary>
    internal static string Summary(ExportSession session)
    {
        var text = new StringBuilder();
        foreach (var (label, value) in session.SummaryRows)
            text.Append(label).Append(": ").Append(value.Replace("\n", "\n  ", StringComparison.Ordinal)).Append('\n');
        text.Append("Limit: ").Append(session.LimitText).Append('\n');
        if (session.ToleranceNotReachedBand is { } band) text.Append("Advisory: ").Append(band).Append('\n');
        if (session.Finding is { } finding) text.Append("Advisory: ").Append(finding.Text).Append('\n');
        text.Append("Safety: ").Append(ExportCopy.Safety).Append('\n');
        return text.ToString();
    }
}
