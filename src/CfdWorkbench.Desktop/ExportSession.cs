using System.Globalization;
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

/// <summary>The formats the dialog offers.</summary>
public enum ExportFormat { Dat, Stl, ThreeMf }

/// <summary>The three tolerance presets of the wing STL (design 4.2).</summary>
public enum StlPreset { Draft, Print, Fine }

/// <summary>The result of one Export attempt. <paramref name="Message"/> is the strip text (Written, Cancelled) or the dialog text (Failed).</summary>
public sealed record ExportOutcome(ExportOutcomeKind Kind, string Message, string? Path = null);

/// <summary>The below-the-floor advisory (design D7): the band text and its jump label. Never blocks.</summary>
public sealed record ExportFinding(string Text, string Jump);

/// <summary>
/// The Export dialog's state and its one verb, UI-free so the checks drive it directly (Export design 6.2, 6.3, 7). Options rebuild the
/// result of the chosen format; the summary rows and the finding read from it, never from a second computation. The .dat is built at once;
/// the wing STL is built by <see cref="PrepareAsync"/> off the UI thread, and <see cref="Preparing"/> is true until it lands (H10).
/// </summary>
public sealed class ExportSession
{
    /// <summary>Above this many triangles the writer shows a progress state with a Cancel (H10).</summary>
    public const int ProgressTriangles = 100_000;

    private readonly Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult> buildStl;
    private readonly Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult> build3mf;
    private DatExportResult? result;
    private StlExportResult? stl;
    private bool closureFailed;
    private long generation;

    /// <param name="buildStl">The wing STL builder; a check replaces it to make the closure check refuse (H7).</param>
    /// <param name="build3mf">The wing 3MF builder; the same replacement for the 3MF.</param>
    public ExportSession(ExportSource source,
        Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult>? buildStl = null,
        Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult>? build3mf = null)
    {
        Source = source;
        this.buildStl = buildStl ?? ((bytes, name, revision, scope, tolerance, cancel) => StlExport.Build(bytes, name, revision, scope, tolerance, cancel));
        this.build3mf = build3mf ?? ((bytes, name, revision, scope, tolerance, cancel) => ThreeMfExport.Build(bytes, name, revision, scope, tolerance, cancel));
        StationIndex = Math.Clamp(source.DefaultStation, 0, Math.Max(0, source.Stations.Count - 1));
        Rebuild();
    }

    public ExportSource Source { get; }
    public ExportFormat Format { get; private set; } = ExportFormat.Dat;
    public DatShape Shape { get; private set; } = DatShape.AtStation;
    public DatOrder Order { get; private set; } = DatOrder.Selig;
    public int PointsPerSurface { get; private set; } = 101;
    public int StationIndex { get; private set; }
    public StlScope Scope { get; private set; } = StlScope.Whole;
    public StlPreset Preset { get; private set; } = StlPreset.Print;

    public ExportStation Station => Source.Stations[StationIndex];

    /// <summary>The refusal when the accepted geometry check has not passed (H2), or null. Export... stays disabled.</summary>
    public string? BlockedReason => Source.Geometry == GeometryStatus.Certified ? null : ExportCopy.Blocked;

    /// <summary>True while the wing STL for the chosen options is not built yet: the summary shows its skeleton and Export... is disabled (H10).</summary>
    public bool Preparing { get; private set; }

    /// <summary>True when the closure check refused the mesh (H7). Nothing can be written; the dialog says so.</summary>
    public bool ClosureFailed => closureFailed;

    public bool CanExport => BlockedReason is null && !Preparing && (Format == ExportFormat.Dat ? result is not null : stl is not null);

    /// <summary>True for the two wing formats, STL and 3MF: one mesh, one scope, one set of presets, one summary.</summary>
    public bool IsMesh => Format != ExportFormat.Dat;

    public DatExportResult? Result => result;

    /// <summary>The built wing for the chosen format: its <c>Bytes</c> are the STL or the 3MF package. Null until <see cref="PrepareAsync"/> lands.</summary>
    public StlExportResult? Stl => stl;

    /// <summary>The tolerance of the chosen preset in millimetres.</summary>
    public double ToleranceMm => Preset switch { StlPreset.Draft => StlExport.DraftMm, StlPreset.Fine => StlExport.FineMm, _ => StlExport.PrintMm };

    public void Set(DatShape? shape = null, DatOrder? order = null, int? points = null, int? station = null,
        ExportFormat? format = null, StlScope? scope = null, StlPreset? preset = null)
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
        if (format is { } f) Format = f;
        if (scope is { } c) Scope = c;
        if (preset is { } r) Preset = r;
        Rebuild();
    }

    private static void Guard(bool condition)
    {
        if (!condition) throw new ArgumentOutOfRangeException(null, "The option is not one the dialog offers.");
    }

    private void Rebuild()
    {
        generation++;
        closureFailed = false;
        if (BlockedReason is not null || IsMesh)
        {
            result = null;
            stl = null;
            Preparing = BlockedReason is null;
            return;
        }
        Preparing = false;
        stl = null;
        result = DatExport.Build(Source.Source, Source.FoilName, StationIndex, Station.Name, Shape, Order, PointsPerSurface, Source.Revision);
    }

    /// <summary>
    /// Builds the wing (STL or 3MF, as chosen) for the current options off the calling thread. An option changed while it runs makes this
    /// build stale and it is dropped (the newer call lands its own). A mesh the closure check refuses sets <see cref="ClosureFailed"/> (H7).
    /// </summary>
    public async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (!Preparing || !IsMesh) return;
        long mine = generation;
        var (source, name, revision, scope, tolerance) = (Source.Source, Source.FoilName, Source.Revision, Scope, ToleranceMm);
        var build = Format == ExportFormat.ThreeMf ? build3mf : buildStl;
        StlExportResult? built = null;
        bool refused = false;
        try { built = await Task.Run(() => build(source, name, revision, scope, tolerance, cancellation), cancellation); }
        catch (ContractError error) when (error.Code == "EXPORT-NOT-CLOSED") { refused = true; }
        if (mine != generation) return;
        stl = built;
        closureFailed = refused;
        Preparing = false;
    }

    public string ShapeHelp => ExportCopy.ShapeHelp(Station.TcPercent);

    /// <summary>
    /// <c>&lt;foil-slug&gt;-&lt;station-slug&gt;-r&lt;n&gt;.dat</c>, <c>&lt;foil-slug&gt;-r&lt;n&gt;[-half]-mm.stl</c> or <c>&lt;foil-slug&gt;-r&lt;n&gt;[-half].3mf</c>
    /// (design 6.4; the 3MF carries its unit in an attribute, so its name has no <c>-mm</c>).
    /// </summary>
    public string FileName => Format switch
    {
        ExportFormat.Dat => $"{DatImport.Slug(Source.FoilName)}-{DatImport.Slug(Station.Name)}-r{Source.Revision}.dat",
        ExportFormat.ThreeMf => ThreeMfExport.FileName(Source.FoilName, Source.Revision, Scope),
        _ => $"{DatImport.Slug(Source.FoilName)}-r{Source.Revision}{(Scope == StlScope.Half ? "-half" : "")}-mm.stl"
    };

    private string Extension => Format switch { ExportFormat.Dat => ".dat", ExportFormat.ThreeMf => ".3mf", _ => ".stl" };

    private static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>The binary STL's size in megabytes (84 bytes of header and count, 50 per triangle), two decimals.</summary>
    public static string Megabytes(int triangles) => ExportCopy.Fixed((84 + 50.0 * triangles) / 1e6, 2);

    /// <summary>The size of the file the chosen format writes, in megabytes, two decimals: the STL by its formula, the 3MF by the package's own length.</summary>
    public string FileMegabytes(StlExportResult mesh) => Format == ExportFormat.ThreeMf ? ExportCopy.Fixed(mesh.Bytes.Length / 1e6, 2) : Megabytes(mesh.Triangles);

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
            if (IsMesh) return StlRows(rows);
            if (result is null) return rows;
            rows.Add(("Units", "Fractions of chord (x/c, y/c). Twist not applied."));
            rows.Add(($"Chord at {Station.Name}", ExportCopy.Fixed(result.ChordMeters * 1000, 2) + " mm"));
            rows.Add(("Points", $"{result.PointCount} in the file ({result.PointsPerSurface} per surface), {(Order == DatOrder.Selig ? ExportCopy.OrderSelig : ExportCopy.OrderLednicer)} order"));
            rows.Add((ExportCopy.TrailingEdgeLabel, TrailingEdgeLine + "\n" + ExportCopy.ManufacturingNotAssessed));
            rows.Add(("Fidelity", ExportCopy.Fidelity(result.DeviationMm)));
            return rows;
        }
    }

    // Where the unit is stated: the STL has no unit field, so it is in the file name; the 3MF sets it in the file.
    private string UnitRow => Format == ExportFormat.ThreeMf ? "mm, unscaled (the unit is set in the file)" : "mm, unscaled (in the file name)";

    private List<(string, string)> StlRows(List<(string, string)> rows)
    {
        if (stl is not { } mesh)
        {
            if (!Preparing) return rows;
            // The skeleton: the same rows at the same height as the built summary, placeholders for the numbers (H10).
            rows.Add(("Unit", UnitRow));
            rows.Add(("Mesh", "000,000 triangles (000 × 000 grid)"));
            rows.Add(("File size", "0.00 MB"));
            rows.Add(("Size of the part", "000.0 × 000.0 × 00.0 mm"));
            rows.Add((ExportCopy.TrailingEdgeLabel, ExportCopy.TrailingEdge(0, "the tip", Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel) + "\n" + ExportCopy.ManufacturingNotAssessed));
            rows.Add(("Fidelity", ExportCopy.FidelityMesh(0)));
            return rows;
        }
        rows.Add(("Unit", UnitRow));
        rows.Add(("Mesh", $"{Count(mesh.Triangles)} triangles ({mesh.Stations} × {mesh.ChordPoints} grid){(Scope == StlScope.Half ? ", with a flat root face" : "")}"));
        rows.Add(("File size", FileMegabytes(mesh) + " MB"));
        rows.Add(("Size of the part", $"{ExportCopy.Fixed(mesh.SizeXMm, 1)} × {ExportCopy.Fixed(mesh.SizeYMm, 1)} × {ExportCopy.Fixed(mesh.SizeZMm, 1)} mm"));
        rows.Add((ExportCopy.TrailingEdgeLabel, TrailingEdgeLine + "\n" + ExportCopy.ManufacturingNotAssessed));
        rows.Add(("Fidelity", ExportCopy.FidelityMesh(mesh.DeviationMm)));
        return rows;
    }

    /// <summary>The tolerance-not-reached band (EX31) when even the finest rung missed the preset, or null.</summary>
    public string? ToleranceNotReachedBand => stl is { ToleranceMet: false } mesh ? ExportCopy.ToleranceNotReached(mesh.DeviationMm, mesh.ToleranceMm) : null;

    /// <summary>The limit lines under the rows: for the wing STL the closure check, the binary32 rounding, then the app's own computation.</summary>
    public string LimitText => IsMesh ? $"{ExportCopy.ClosedCheck} {ExportCopy.Rounded} {ExportCopy.Limit}" : ExportCopy.Limit;

    /// <summary>Always shown (Ruling 195): least thickness from the written points or mesh, where, the floor and its label.</summary>
    public string TrailingEdgeLine
    {
        get
        {
            if (IsMesh)
                return stl is not { } mesh ? "" : mesh.TrailingEdgeAlongSpan
                    ? ExportCopy.TrailingEdgeWholeSpan(mesh.TrailingEdgeMm, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel)
                    : ExportCopy.TrailingEdge(mesh.TrailingEdgeMm, WhereStl(mesh), Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel);
            return result is null ? "" :
                ExportCopy.TrailingEdge(result.TrailingEdgeMm, Station.Name, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel);
        }
    }

    private static string WhereStl(StlExportResult mesh) =>
        mesh.TrailingEdgeAtTip ? "the tip" : $"y = {ExportCopy.Fixed(mesh.TrailingEdgeYMm, 0)} mm";

    /// <summary>The advisory band, only when the written trailing edge is below the floor; it never blocks (Ruling 194 (5)).</summary>
    public ExportFinding? Finding
    {
        get
        {
            if (IsMesh)
                return stl is { } mesh && mesh.TrailingEdgeMm < Settings.TrailingEdgeFloorMm
                    ? new(ExportCopy.BelowFloor(mesh.TrailingEdgeMm, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel),
                        mesh.TrailingEdgeAlongSpan ? ExportCopy.ShowTrailingEdgeGap : ExportCopy.ShowAt(WhereStl(mesh).Replace("the ", "", StringComparison.Ordinal)))
                    : null;
            return result is { } written && written.TrailingEdgeMm < Settings.TrailingEdgeFloorMm
                ? new(ExportCopy.BelowFloor(written.TrailingEdgeMm, Settings.TrailingEdgeFloorMm, Settings.TrailingEdgeFloorLabel), ExportCopy.ShowAt(Station.Name))
                : null;
        }
    }

    /// <summary>The station the finding's jump opens: the chosen one for a .dat; for the wing STL the tip, or the station nearest a located value.</summary>
    public int JumpStationIndex
    {
        get
        {
            if (Format == ExportFormat.Dat) return StationIndex;
            if (stl is not { } mesh || mesh.TrailingEdgeAlongSpan || mesh.TrailingEdgeAtTip) return Source.Stations.Count - 1;
            int nearest = 0;
            double best = double.PositiveInfinity;
            for (int index = 0; index < Source.Stations.Count; index++)
            {
                double y = Placement.Frame(Source.Source, Placement.StationEta(Source.Source, index)).SpanMeters * 1000;
                if (Math.Abs(y - mesh.TrailingEdgeYMm) < best) { best = Math.Abs(y - mesh.TrailingEdgeYMm); nearest = index; }
            }
            return nearest;
        }
    }

    /// <summary>True above 500,000 triangles (H5, B7): the band shows and the button reads Export anyway…. The writer never refuses on size.</summary>
    public bool IsLarge => stl is { } mesh && mesh.Triangles > StlExport.LargeMeshTriangles;

    /// <summary>The large-mesh band text with the triangle count Print would give for the same scope, or null.</summary>
    public string? LargeMeshBand
    {
        get
        {
            if (stl is not { } mesh || !IsLarge) return null;
            int print = Preset == StlPreset.Print ? mesh.Triangles : buildStl(Source.Source, Source.FoilName, Source.Revision, Scope, StlExport.PrintMm, CancellationToken.None).Triangles;
            return ExportCopy.LargeMesh(Count(mesh.Triangles), FileMegabytes(mesh), Count(print));
        }
    }

    /// <summary>The export button's label: the title when blocked, Preparing while the mesh is built, Export anyway… for a large mesh.</summary>
    public string ButtonLabel => BlockedReason is not null ? ExportCopy.Title : Preparing ? ExportCopy.Preparing : IsLarge ? ExportCopy.ExportAnyway : ExportCopy.ExportMenu;

    /// <summary>The in-dialog message when the closure check refused the mesh (H7), or null.</summary>
    public string? ClosureMessage => closureFailed ? ExportCopy.MeshNotClosed : null;

    /// <summary>
    /// The lines the preview block draws, an ellipsis for the rest: the name and first two rows, then in Selig order the nose and
    /// the row after it (the mockup's rows), in Lednicer order the last two rows. The wing STL is binary and has no preview.
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
    /// chosen path, or null for Cancel. The extension is forced to the format's, so the export can never name the project file.
    /// Any failure leaves the earlier file untouched (temp, then rename) and comes back as Failed with its cause (H6). A write
    /// cancelled through <paramref name="cancellation"/> removes its temp file and comes back as Cancelled (H10).
    /// </summary>
    public async Task<ExportOutcome> RunAsync(Func<string, string?, Task<string?>> pick, Func<string, byte[], Task>? write = null,
        CancellationToken cancellation = default)
    {
        byte[]? bytes = Format == ExportFormat.Dat ? result?.Bytes : stl?.Bytes;
        if (!CanExport || bytes is null) return new(ExportOutcomeKind.Failed, BlockedReason ?? ExportCopy.Blocked);
        string? chosen = await pick(FileName, StartFolder);
        if (chosen is null) return new(ExportOutcomeKind.Cancelled, ExportCopy.Cancelled);
        string path = ForceExtension(chosen, Extension);
        // The panel confirmed the replacement of the name it was given. A name the app changed was never confirmed: never replace it.
        if (path != chosen && (File.Exists(path) || Directory.Exists(path)))
            return new(ExportOutcomeKind.Failed, ExportCopy.WriteFailed(""), path);
        try
        {
            await (write ?? ((target, content) => WriteAtomicAsync(target, content, cancellation)))(path, bytes);
        }
        catch (OperationCanceledException)
        {
            return new(ExportOutcomeKind.Cancelled, ExportCopy.Cancelled);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new(ExportOutcomeKind.Failed, ExportCopy.WriteFailed(CauseOf(error)), path);
        }
        string file = Path.GetFileName(path);
        return new(ExportOutcomeKind.Written, Format == ExportFormat.Dat
            ? ExportCopy.Exported(file, result!.PointCount, result.ChordMeters * 1000, result.DeviationMm)
            : ExportCopy.ExportedMesh(file, Count(stl!.Triangles), FileMegabytes(stl), stl.DeviationMm), path);
    }

    /// <summary>The path with its extension forced to .dat; <c>x.cfdw.json</c> becomes <c>x.cfdw.dat</c>, never the project file.</summary>
    public static string ForceExtension(string path) => ForceExtension(path, ".dat");

    /// <summary>The path with its extension forced to <paramref name="extension"/> (.dat or .stl); a project file name is never kept.</summary>
    public static string ForceExtension(string path, string extension) =>
        string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase) ? path : Path.ChangeExtension(path, extension);

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
    /// failure (a cancellation included) the temp file is deleted and the target is untouched.
    /// </summary>
    public static async Task WriteAtomicAsync(string path, byte[] bytes, CancellationToken cancellation = default)
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
                await stream.WriteAsync(bytes, cancellation);
                stream.Flush(flushToDisk: true);
            }
            cancellation.ThrowIfCancellationRequested();
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(temp); } catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }
            throw;
        }
    }
}
