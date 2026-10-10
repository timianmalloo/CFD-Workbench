using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop;

/// <summary>One authored station an Export can write: its display name, chord in mm and station t/c in percent.</summary>
public sealed record ExportStation(string Name, double ChordMm, double TcPercent);

/// <summary>
/// What Export reads: the accepted revision, never an open draft (design D3). <paramref name="DraftOpen"/> and
/// <paramref name="Analysis"/> only add a line to the summary. <paramref name="ProjectFolder"/> is where the save panel starts.
/// </summary>
public sealed record ExportSource(byte[] Source, string FoilName, int Revision, GeometryStatus Geometry, bool DraftOpen, bool Analysis,
    IReadOnlyList<ExportStation> Stations, int DefaultStation, string? ProjectFolder);

public enum ExportOutcomeKind { Written, Cancelled, Failed }

/// <summary>The result of one Export attempt. <paramref name="Message"/> is the strip text (Written, Cancelled) or the dialog text (Failed).</summary>
public sealed record ExportOutcome(ExportOutcomeKind Kind, string Message, string? Path = null);

/// <summary>The below-the-floor advisory (design D7): the band text and its jump label. Never blocks.</summary>
public sealed record ExportFinding(string Text, string Jump);

/// <summary>
/// The .dat Export dialog's state and its one verb, UI-free so the checks drive it directly (Export design 6.2, 6.3, 7).
/// Options rebuild <see cref="Result"/>; the summary rows and the finding read from it, never from a second computation.
/// </summary>
public sealed class ExportSession
{
    private DatExportResult? result;

    public ExportSession(ExportSource source)
    {
        Source = source;
        StationIndex = Math.Clamp(source.DefaultStation, 0, Math.Max(0, source.Stations.Count - 1));
        Rebuild();
    }

    public ExportSource Source { get; }
    public DatShape Shape { get; private set; } = DatShape.AtStation;
    public DatOrder Order { get; private set; } = DatOrder.Selig;
    public int PointsPerSurface { get; private set; } = 101;
    public int StationIndex { get; private set; }

    public ExportStation Station => Source.Stations[StationIndex];

    /// <summary>The refusal when the accepted geometry check has not passed (H2), or null. Export... stays disabled.</summary>
    public string? BlockedReason => Source.Geometry == GeometryStatus.Certified ? null : ExportCopy.Blocked;

    public bool CanExport => BlockedReason is null && result is not null;

    public DatExportResult? Result => result;

    public void Set(DatShape? shape = null, DatOrder? order = null, int? points = null, int? station = null)
    {
        if (shape is { } s) Shape = s;
        if (order is { } o) Order = o;
        if (points is { } p)
        {
            Guard(DatExport.PointChoices.Contains(p));
            PointsPerSurface = p;
        }
        if (station is { } t)
        {
            Guard(t >= 0 && t < Source.Stations.Count);
            StationIndex = t;
        }
        Rebuild();
    }

    private static void Guard(bool condition)
    {
        if (!condition) throw new ArgumentOutOfRangeException(null, "The option is not one the dialog offers.");
    }

    private void Rebuild() => result = BlockedReason is not null ? null : DatExport.Build(Source.Source, Source.FoilName, StationIndex, Station.Name,
        Shape, Order, PointsPerSurface, Source.Revision);

    public string ShapeHelp => ExportCopy.ShapeHelp(Station.TcPercent);

    /// <summary><c>&lt;foil-slug&gt;-&lt;station-slug&gt;-r&lt;n&gt;.dat</c> (design 6.4).</summary>
    public string FileName =>
        $"{DatImport.Slug(Source.FoilName)}-{DatImport.Slug(Station.Name)}-r{Source.Revision}.dat";

    /// <summary>The summary rows in the order the mockup draws them. The trailing-edge row has two lines.</summary>
    public IReadOnlyList<(string Label, string Value)> SummaryRows
    {
        get
        {
            var rows = new List<(string, string)>
            {
                ("Revision", Source.DraftOpen ? ExportCopy.RevisionDraft(Source.Revision) : ExportCopy.RevisionAccepted(Source.Revision)),
            };
            if (Source.Analysis) rows.Add(("Analysis", ExportCopy.AnalysisNote));
            if (result is null) return rows;
            rows.Add(("Units", "Fractions of chord (x/c, y/c). Twist not applied."));
            rows.Add(($"Chord at {Station.Name}", ExportCopy.Fixed(result.ChordMeters * 1000, 2) + " mm"));
            rows.Add(("Points", $"{result.PointCount} in the file ({result.PointsPerSurface} per surface), {(Order == DatOrder.Selig ? ExportCopy.OrderSelig : ExportCopy.OrderLednicer)} order"));
            rows.Add((ExportCopy.TrailingEdgeLabel, TrailingEdgeLine + "\n" + ExportCopy.ManufacturingNotAssessed));
            rows.Add(("Fidelity", ExportCopy.Fidelity(result.DeviationMm)));
            return rows;
        }
    }

    /// <summary>Always shown (Ruling 195): least thickness from the written points, where, the floor and its label.</summary>
    public string TrailingEdgeLine => result is null ? "" :
        ExportCopy.TrailingEdge(result.TrailingEdgeMm, Station.Name, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel);

    /// <summary>The advisory band, only when the written trailing edge is below the floor; it never blocks (Ruling 194 (5)).</summary>
    public ExportFinding? Finding => result is { } written && written.TrailingEdgeMm < Settings.TrailingEdgeFloorMm
        ? new(ExportCopy.BelowFloor(written.TrailingEdgeMm, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel), ExportCopy.ShowAt(Station.Name))
        : null;

    /// <summary>
    /// The lines the preview block draws, an ellipsis for the rest: the name and first two rows, then in Selig order the nose and
    /// the row after it (the mockup's rows), in Lednicer order the last two rows.
    /// </summary>
    public IReadOnlyList<string> PreviewLines
    {
        get
        {
            if (result is null) return [];
            string[] lines = System.Text.Encoding.UTF8.GetString(result.Bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return Order == DatOrder.Selig
                ? [.. lines.Take(3), "...", lines[result.PointsPerSurface], lines[result.PointsPerSurface + 1]]
                : [.. lines.Take(3), "...", .. lines.TakeLast(2)];
        }
    }

    /// <summary>The save panel's start folder (design 6.3 step 4): the project's folder, else the default CFD Workbench folder.</summary>
    public string? StartFolder => Source.ProjectFolder;

    /// <summary>
    /// Choose the file, write it, say what happened. The picker gets the suggested name and the start folder and returns the
    /// chosen path, or null for Cancel. The extension is forced to .dat, so the export can never name the project file.
    /// Any failure leaves the earlier file untouched (temp, then rename) and comes back as Failed with its cause (H6).
    /// </summary>
    public async Task<ExportOutcome> RunAsync(Func<string, string?, Task<string?>> pick, Func<string, byte[], Task>? write = null)
    {
        if (!CanExport || result is null) return new(ExportOutcomeKind.Failed, BlockedReason ?? ExportCopy.Blocked);
        string? chosen = await pick(FileName, StartFolder);
        if (chosen is null) return new(ExportOutcomeKind.Cancelled, ExportCopy.Cancelled);
        string path = ForceExtension(chosen);
        // The panel confirmed the replacement of the name it was given. A name the app changed was never confirmed: never replace it.
        if (path != chosen && (File.Exists(path) || Directory.Exists(path)))
            return new(ExportOutcomeKind.Failed, ExportCopy.WriteFailed(""), path);
        try
        {
            await (write ?? WriteAtomicAsync)(path, result.Bytes);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new(ExportOutcomeKind.Failed, ExportCopy.WriteFailed(CauseOf(error)), path);
        }
        return new(ExportOutcomeKind.Written, ExportCopy.Exported(Path.GetFileName(path), result.PointCount, result.ChordMeters * 1000, result.DeviationMm), path);
    }

    /// <summary>The path with its extension forced to .dat; <c>x.cfdw.json</c> becomes <c>x.cfdw.dat</c>, never the project file.</summary>
    public static string ForceExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".dat", StringComparison.OrdinalIgnoreCase) ? path : Path.ChangeExtension(path, ".dat");

    /// <summary>The plain cause for a failed write (EX35), or empty when none of the three fits, so the sentence still says what happened.</summary>
    public static string CauseOf(Exception error) => error switch
    {
        UnauthorizedAccessException => ExportCopy.NoPermission,
        DirectoryNotFoundException => ExportCopy.FolderGone,
        // Windows: ERROR_DISK_FULL 0x70 and ERROR_HANDLE_DISK_FULL 0x27 in the low word; elsewhere the raw errno ENOSPC (28).
        IOException io when IsDiskFull(io) => ExportCopy.DiskFull,
        _ => ""
    };

    private static bool IsDiskFull(IOException error) =>
        OperatingSystem.IsWindows() ? (error.HResult & 0xFFFF) is 0x70 or 0x27 : error.HResult == 28;

    /// <summary>
    /// The directory is the one the save panel chose and is followed as chosen; the app builds no path component. A symlink at the target is
    /// refused. The temp file is created exclusive, with a random name, in that directory, flushed to disk, then published by rename. On any
    /// failure the temp file is deleted and the target is untouched.
    /// </summary>
    public static async Task WriteAtomicAsync(string path, byte[] bytes)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new DirectoryNotFoundException(path);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
        if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("The target is a link.");
        string temp = Path.Combine(folder, ".cfd-" + Guid.NewGuid().ToString("D") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(temp); } catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }
            throw;
        }
    }
}
