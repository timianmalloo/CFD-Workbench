using CfdWorkbench.Analysis.Export;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Export, slice 1: the section .dat (docs/design/export.md 6, 7; Ruling 196). Ring: fast, in the `--section-editor` child.
/// Cost: about 4.5 s together (each check opens the Example, about 0.2 s; one window render)
/// </summary>
public static class ExportTests
{
    public static void Run()
    {
        DesktopChecks.Check("Export_Commands_RowsGestureAndPalette", Commands);
        DesktopChecks.Check("Export_Copy_MatchesRegistry_Ids474To519", CopyRegistry);
        DesktopChecks.Check("Export_NoFoil_RowsDisabledWithReason_H9", NoFoil);
        DesktopChecks.Check("Export_Session_DefaultsSummaryAndFileName", Defaults);
        DesktopChecks.Check("Export_TrailingEdge_RowAlways_BandOnlyBelowFloor_NeverBlocks", TrailingEdge);
        DesktopChecks.Check("Export_Blocked_GeometryNotAccepted_RefusesBeforeThePanel_H2", Blocked);
        DesktopChecks.Check("Export_Shape_OwnAndAtStation_DifferWhenTcIsNotTheAuthoredPeak", Shapes);
        DesktopChecks.Check("Export_Shell_FileExport_WritesFileAndStatusWithReveal", ShellWrites);
        DesktopChecks.Check("Export_Shell_SectionExportDat_OpensOnSelectedStation", SectionEntry);
        DesktopChecks.Check("Export_Cancel_AtThePanel_WritesNothingAndSaysSo_H8", PanelCancelled);
        DesktopChecks.Check("Export_WriteFailure_CauseCopy_EarlierFileUntouched_NoTemp_H6", WriteFailure);
        DesktopChecks.Check("Export_Write_IntoOneDriveNamedFolder_NotRefused_Ruling194", OneDrive);
        DesktopChecks.Check("Export_Write_SymlinkTarget_Refused_LinkAndTargetUnchanged", SymlinkTarget);
        DesktopChecks.Check("Export_FileName_FromHostileNames_HasNoSeparatorOrDotDot", SuggestedNameIsPlain);
        DesktopChecks.Check("Export_Extension_ForcedPathThatExists_NeverOverwritten", ForcedExtensionNeverOverwrites);
        DesktopChecks.Check("Export_Extension_ForcedToDat_NeverTheProjectFile", Extension);
        DesktopChecks.Check("Export_Draft_ReadsAcceptedRevisionAndSaysSo_H1", Draft);
        DesktopChecks.Check("Export_Analysis_AddsOneLine_ReadsTheSameRevision_H4", AnalysisLine);
        DesktopChecks.Check("Export_Dialog_DatStlAnd3mf_StepRowExplainsItself", DialogShape);
        DesktopChecks.Check("Export_Dialog_Keyboard_EnterExports_EscapeCancels_FocusOnExport", DialogKeys);
        DesktopChecks.Check("Export_Dialog_Failure_StaysOpenWithTwoWaysOut", DialogFailure);
        DesktopChecks.Check("Export_Dialog_JumpSelectsStationAndOpensItsSection_H3", Jump);
        DesktopChecks.Check("Export_Dialog_Renders_ReadyState_Screenshot", Screenshot);
        ExportTelemetryTests.Run();
    }

    internal static void Settle() { Dispatcher.UIThread.RunJobs(); }

    internal static T Wait<T>(Task<T> task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("export check did not complete");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        return task.GetAwaiter().GetResult();
    }

    internal static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("export check did not complete");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        task.GetAwaiter().GetResult();
    }

    internal static void Equal<T>(T expected, T actual, string what = "")
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{what} expected {expected}; actual {actual}".TrimStart());
    }

    internal static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    internal static WorkbenchController OpenExample()
    {
        var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        return controller;
    }

    internal static ExportSession Session(WorkbenchController controller) => new(controller.ExportSnapshot()!);

    private static string Registry() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DESIGN.md"));

    private static void Commands()
    {
        var file = CommandTable.Rows.Single(row => row.Id == "file.export");
        Equal("Export…", file.Title);
        Equal("File", file.Menu);
        Equal("⇧⌘E", file.Gesture);
        var section = CommandTable.Rows.Single(row => row.Id == "section.export-dat");
        Equal("Export .dat…", section.Title);
        Equal(CommandTable.SectionMenu, section.Menu);
        var gesture = NativeMenuBuilder.ParseGesture(file.Gesture, macOS: false);
        True(gesture is { Key: Key.E } && gesture.KeyModifiers == (KeyModifiers.Shift | KeyModifiers.Control), "Ctrl+Shift+E on Windows");
        var mac = NativeMenuBuilder.ParseGesture(file.Gesture, macOS: true);
        True(mac is { Key: Key.E } && mac.KeyModifiers == (KeyModifiers.Shift | KeyModifiers.Meta), "Shift+Command+E on macOS");
        var palette = CommandTable.PaletteEntries();
        True(palette.Any(entry => entry.Id == "file.export") && palette.Any(entry => entry.Id == "section.export-dat"), "both rows are palette entries");
        var others = CommandTable.Rows.Where(row => row.Id != "file.export" && row.Gesture == file.Gesture).ToArray();
        Equal(0, others.Length, "no other row takes the gesture");
    }

    // COPY-474..519 (Ruling 196): the string each constant holds is in the registry row of its id.
    private static void CopyRegistry()
    {
        string registry = Registry();
        (int Id, string Text)[] used =
        [
            (474, ExportCopy.ExportMenu), (475, ExportCopy.ExportDatMenu), (476, ExportCopy.NeedsFoil), (477, ExportCopy.Title),
            (478, ExportCopy.FormatDat), (481, ExportCopy.StepUnavailable), (482, ExportCopy.ShapeLabel), (482, ExportCopy.ShapeAtStation),
            (482, ExportCopy.ShapeOwn), (483, ExportCopy.StationLabel), (483, ExportCopy.ShapeHelp(12.0).Replace("12.0", "<tc>")),
            (484, ExportCopy.OrderLabel), (484, ExportCopy.OrderSelig), (484, ExportCopy.OrderLednicer), (485, ExportCopy.PointsLabel),
            (489, ExportCopy.SummaryHeading), (491, ExportCopy.AnalysisNote), (497, ExportCopy.Safety), (501, ExportCopy.Limit),
            (505, ExportCopy.BlockedTitle), (506, ExportCopy.Blocked), (508, ExportCopy.DiskFull), (508, ExportCopy.NoPermission),
            (508, ExportCopy.FolderGone), (509, ExportCopy.ChooseAnotherPlace), (509, ExportCopy.TryAgain), (511, ExportCopy.Cancelled),
            (512, ExportCopy.ShowInFinder), (513, ExportCopy.Cancel), (517, ExportCopy.TrailingEdgeLabel), (519, ExportCopy.ManufacturingNotAssessed),
            (490, ExportCopy.RevisionAccepted(12).Replace("12", "<n>")), (490, ExportCopy.RevisionDraft(12).Replace("12", "<n>")),
            (494, ExportCopy.Exported("<file>", 0, 0, 0).Replace(" 0 points", " <pts> points").Replace("chord 0.00 mm", "chord <chord> mm").Replace("deviation 0.0000 mm", "deviation <dev> mm")),
            (495, ExportCopy.BelowFloor(0, 0, "app default, no source").Replace("0.00 mm, below the floor of 0.00 mm", "<t> mm, below the floor of <f> mm")),
            (496, ExportCopy.ShowAt("<where>")),
            (498, ExportCopy.Fidelity(0).Replace("0.0000", "<dev>")),
            (507, ExportCopy.WriteFailed("<cause>", earlierFile: true)),
            (507, ExportCopy.WriteFailed("<cause>", earlierFile: false) + " —"),
            (520, ExportCopy.UnitFixedInName), (521, ExportCopy.UnitFixedInFile),
            (518, ExportCopy.TrailingEdge(0, "<where>", 0, "app default, no source").Replace("0.00 mm at", "<t> mm at").Replace("Floor 0.00 mm", "Floor <f> mm")),
            // The wing 3MF slice (trk-tmf).
            (480, ExportCopy.FormatThreeMf),
            // The wing STL slice (trk-stx).
            (479, ExportCopy.FormatStl), (486, ExportCopy.ScopeLabel), (486, ExportCopy.ScopeWhole), (486, ExportCopy.ScopeHalf), (486, ExportCopy.ScopeHalfHelp),
            (487, ExportCopy.ToleranceLabel), (487, ExportCopy.ToleranceDraft), (487, ExportCopy.TolerancePrint), (487, ExportCopy.ToleranceFine),
            (488, ExportCopy.UnitLabel), (488, ExportCopy.UnitFixed), (492, ExportCopy.Preparing),
            (493, ExportCopy.ExportedMesh("<file>", "<tris>", "<size>", 0).Replace("deviation 0.0000 mm", "deviation <dev> mm")),
            (496, ExportCopy.ShowTrailingEdgeGap), (499, ExportCopy.ClosedCheck), (500, ExportCopy.Rounded),
            (498, ExportCopy.FidelityMesh(0).Replace("0.0000", "<dev>")),
            (502, ExportCopy.LargeMesh("<tris>", "<size>", "<tris2>")), (503, ExportCopy.ExportAnyway),
            (504, ExportCopy.ToleranceNotReached(0, 0.02).Replace("0.0000", "<dev>").Replace("0.02", "<tol>")),
            (510, ExportCopy.MeshNotClosed), (514, ExportCopy.Writing("<tris>", "<size>")), (515, ExportCopy.WritingTitle), (516, ExportCopy.MeshNotClosedDetails),
            (518, ExportCopy.TrailingEdgeWholeSpan(0, 0, "app default, no source").Replace("0.00 mm along", "<t> mm along").Replace("Floor 0.00 mm", "Floor <f> mm")),
        ];
        foreach (var (id, text) in used)
        {
            string row = registry.Split('\n').SingleOrDefault(line => line.StartsWith($"| COPY-{id} |", StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"COPY-{id} is not in DESIGN.md section 7");
            True(row.Contains(text, StringComparison.Ordinal), $"COPY-{id} does not hold: {text}");
        }
        // No id is reserved any more: COPY-480 (EX07, the 3MF format row) landed with the 3MF slice, so the comment line that reserved it is gone.
        True(!registry.Contains("still reserved", StringComparison.OrdinalIgnoreCase), "the registry no longer reserves an Export id");
    }

    private static void NoFoil()
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        foreach (string id in new[] { "file.export", "section.export-dat" })
        {
            Equal(false, host.CanRun(id), id + " enabled with no foil");
            Equal(ExportCopy.NeedsFoil, host.ShellCommandReason(id));
            Wait(host.RunCommand(id));
            Equal(ExportCopy.NeedsFoil, host.StatusStrip.Text, id + " strip");
        }
        Equal(null, controller.ExportSnapshot());
    }

    private static void Defaults()
    {
        using var controller = OpenExample();
        var session = Session(controller);
        Equal(DatShape.AtStation, session.Shape);
        Equal(DatOrder.Selig, session.Order);
        Equal(101, session.PointsPerSurface);
        Equal("Root", session.Station.Name);
        Equal("basic-foil-root-r1.dat", session.FileName);
        var rows = session.SummaryRows.ToDictionary(row => row.Label, row => row.Value);
        Equal("Revision r1, accepted.", rows["Revision"]);
        Equal("Fractions of chord (x/c, y/c). Twist not applied.", rows["Units"]);
        Equal("120.00 mm", rows["Chord at Root"]);
        Equal("201 in the file (101 per surface), Selig order", rows["Points"]);
        Equal("Least thickness 0.00 mm at Root. Floor 0.30 mm (app default, no source).\nManufacturing: not assessed (no process chosen)", rows["Trailing edge"]);
        Equal(ExportCopy.Fidelity(session.Result!.DeviationMm), rows["Fidelity"]);
        Equal("Largest deviation between the curve and the lines joining the points: 0.0074 mm at this chord, sampled at segment midpoints.", rows["Fidelity"]);
        Equal(Settings.TrailingEdgeFloorMm, 0.3);
        True(session.ShapeHelp.StartsWith("At station: the section as the wing builds it here; its peak thickness is the station t/c, 12.0 %.", StringComparison.Ordinal), "shape help");
        // The closed Example has 0.00 mm at the trailing edge, below the 0.30 mm floor: the row is there and the advisory band too.
        Equal("Trailing edge 0.00 mm, below the floor of 0.30 mm (app default, no source)", session.Finding!.Text);
        Equal("Basic foil | Root | r1", session.PreviewLines[0]);
        Equal("...", session.PreviewLines[3]);
        True(session.PreviewLines[4].StartsWith("0 ", StringComparison.Ordinal), "the nose row follows the ellipsis: " + session.PreviewLines[4]);
        // Stations are the authored ones, with their chords.
        Equal(2, controller.ExportSnapshot()!.Stations.Count);
        Equal("Tip", controller.ExportSnapshot()!.Stations[1].Name);
    }

    internal static ExportSource WithSource(ExportSource source, string text) => source with { Source = Encoding.UTF8.GetBytes(text) };

    internal static string OpenTe(ExportSource source, double half) => Encoding.UTF8.GetString(source.Source)
        .Replace("(0.9, 0.01), (1, 0)] ids", $"(0.9, 0.01), (1, {half.ToString("R", System.Globalization.CultureInfo.InvariantCulture)})] ids", StringComparison.Ordinal)
        .Replace("(0.9, -0.01), (1, 0)] ids", $"(0.9, -0.01), (1, {(-half).ToString("R", System.Globalization.CultureInfo.InvariantCulture)})] ids", StringComparison.Ordinal)
        .Replace("\"cv-6\", \"cv-7\"] }\n    }", "\"cv-6\", \"cv-7\"] }\n      closure open\n    }", StringComparison.Ordinal);

    private static void TrailingEdge()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()!;
        // 0.26 mm: below the 0.30 mm floor. The row is there and so is the band; Export... still works.
        var below = new ExportSession(WithSource(source, OpenTe(source, 0.001)));
        Equal("Trailing edge 0.26 mm, below the floor of 0.30 mm (app default, no source)", below.Finding!.Text);
        Equal("Show at Root", below.Finding.Jump);
        Equal("Least thickness 0.26 mm at Root. Floor 0.30 mm (app default, no source).", below.TrailingEdgeLine);
        True(below.CanExport, "below the floor is advisory only (Ruling 194 (5))");
        // The row and the band read the written points, not a second computation.
        string[] lines = Encoding.UTF8.GetString(below.Result!.Bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        double upperTail = double.Parse(lines[1].Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        double lowerTail = double.Parse(lines[^1].Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        Equal((upperTail - lowerTail) * below.Result.ChordMeters * 1000, below.Result.TrailingEdgeMm);
        // 0.36 mm meets the floor: the row, no band.
        var meets = new ExportSession(WithSource(source, OpenTe(source, 0.0014)));
        Equal(null, meets.Finding);
        True(meets.TrailingEdgeLine.StartsWith("Least thickness 0.36 mm at Root.", StringComparison.Ordinal), meets.TrailingEdgeLine);
        // Closed: 0.00 mm, a row, and the band (0.00 is below 0.30): the closed trailing edge is the as-built geometry, never closed by the app.
        Equal(0.0, Session(controller).Result!.TrailingEdgeMm);
        Equal("Trailing edge 0.00 mm, below the floor of 0.30 mm (app default, no source)", Session(controller).Finding?.Text ?? "no band");
        Equal("Manufacturing: not assessed (no process chosen)", below.SummaryRows.Single(row => row.Label == "Trailing edge").Value.Split('\n')[1]);
    }

    private static void Blocked()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()! with { Geometry = GeometryStatus.NotAssessed };
        var session = new ExportSession(source);
        Equal(ExportCopy.Blocked, session.BlockedReason);
        Equal(false, session.CanExport);
        Equal(null, session.Result);
        bool asked = false;
        var outcome = Wait(session.RunAsync((_, _) => { asked = true; return Task.FromResult<string?>("never.dat"); }));
        Equal(ExportOutcomeKind.Failed, outcome.Kind);
        Equal(false, asked, "the save panel opened for a foil that is not accepted");
        True(session.SummaryRows.All(row => row.Label is "Revision" or "Analysis"), "no numbers for a blocked export");
    }

    private static void Shapes()
    {
        using var controller = OpenExample();
        var session = Session(controller);
        session.Set(station: 1);
        byte[] at = session.Result!.Bytes;
        session.Set(shape: DatShape.Own);
        byte[] own = session.Result!.Bytes;
        True(!at.AsSpan().SequenceEqual(own), "the Tip (t/c 0.12, authored peak 0.1126) is the same file in both shapes");
        session.Set(order: DatOrder.Lednicer, points: 61);
        Equal("61 61", Encoding.UTF8.GetString(session.Result!.Bytes).Split('\n')[1]);
        Equal("122 in the file (61 per surface), Lednicer order", session.SummaryRows.Single(row => row.Label == "Points").Value);
        Equal("basic-foil-tip-r1.dat", session.FileName);
        try { session.Set(points: 100); throw new InvalidOperationException("an unlisted point count was accepted"); }
        catch (ArgumentOutOfRangeException) { }
    }

    internal static string NewFolder() => TestTemp.NewDirectory("export-");

    private static void ShellWrites()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var host = new ShellHost(controller);
            string? revealed = null;
            host.RevealExportedFile = path => revealed = path;
            string? suggested = null, startFolder = "unset";
            host.ShowExportDialog = async session =>
                await session.RunAsync((name, start) => { suggested = name; startFolder = start; return Task.FromResult<string?>(Path.Combine(folder, name)); });
            Equal(true, host.CanRun("file.export"));
            Wait(host.RunCommand("file.export"));
            Equal("basic-foil-root-r1.dat", suggested);
            Equal(null, startFolder);
            string path = Path.Combine(folder, "basic-foil-root-r1.dat");
            byte[] written = File.ReadAllBytes(path);
            var expected = DatExport.Build(Encoding.UTF8.GetBytes(controller.AcceptedSource), "Basic foil", 0, "Root", DatShape.AtStation, DatOrder.Selig, 101, 1);
            True(written.AsSpan().SequenceEqual(expected.Bytes), "the written file is the writer's bytes");
            Equal("Exported basic-foil-root-r1.dat · 201 points · x/c, y/c · chord 120.00 mm · largest deviation 0.0074 mm", host.StatusStrip.Text);
            string label = OperatingSystem.IsWindows() ? "Show in Explorer" : "Show in Finder";
            Equal(label, host.StatusStrip.Action?.Label);
            host.StatusStrip.Action!.Run();
            Equal(path, revealed);
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
            // The same file imports through the existing Import .dat path.
            Equal(201, DatImport.Parse(written).OriginalPointCount);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void SectionEntry()
    {
        using var controller = OpenExample();
        var host = new ShellHost(controller);
        ExportSession? seen = null;
        host.ShowExportDialog = session => { seen = session; return Task.FromResult<ExportOutcome?>(null); };
        Wait(host.RunCommand("file.export"));
        Equal(0, seen!.StationIndex);
        controller.Select(new Selection.Station(1, 1));
        Settle();
        Wait(host.RunCommand("section.export-dat"));
        Equal(1, seen!.StationIndex);
        Equal("Tip", seen.Station.Name);
        Equal(true, host.CanRun("section.export-dat"));
        Equal(null, host.ShellCommandReason("section.export-dat"));
    }

    private static void PanelCancelled()
    {
        using var controller = OpenExample();
        var host = new ShellHost(controller);
        host.ShowExportDialog = async session => await session.RunAsync((_, _) => Task.FromResult<string?>(null));
        Wait(host.RunCommand("file.export"));
        Equal("Export cancelled. Nothing was written.", host.StatusStrip.Text);
        Equal(null, host.StatusStrip.Action);
    }

    private static void WriteFailure()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var session = Session(controller);
            string existing = Path.Combine(folder, "earlier.dat");
            File.WriteAllText(existing, "the earlier file");
            // A folder that is gone.
            var gone = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "missing", "x.dat"))));
            Equal(ExportOutcomeKind.Failed, gone.Kind);
            // Ruling 204: no file was at that name, so the earlier-file sentence is not said.
            Equal("Can't write the file. The folder no longer exists. Nothing was changed.", gone.Message);
            // The target is a directory: the rename fails, the temp file is removed, and nothing else changes.
            string asDirectory = Path.Combine(folder, "taken.dat");
            Directory.CreateDirectory(asDirectory);
            var blocked = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(asDirectory)));
            Equal(ExportOutcomeKind.Failed, blocked.Kind);
            Equal("Can't write the file. Nothing was changed.", blocked.Message);
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
            Equal("the earlier file", File.ReadAllText(existing));
            // Each cause has its own sentence; an unmapped failure still says what happened, with no invented cause.
            (Exception Error, string Cause)[] causes =
            [
                (new UnauthorizedAccessException(), "You don't have permission to write to that folder."),
                (new DirectoryNotFoundException(), "The folder no longer exists."),
                (new IOException("full", OperatingSystem.IsWindows() ? unchecked((int)0x80070070) : 28), "The disk is full."),
                (new IOException("other"), "")
            ];
            foreach (var (error, cause) in causes)
            {
                var outcome = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(existing), (_, _) => throw error));
                Equal(ExportOutcomeKind.Failed, outcome.Kind);
                Equal(cause.Length == 0 ? "Can't write the file. Nothing was changed. The earlier file is still there."
                    : $"Can't write the file. {cause} Nothing was changed. The earlier file is still there.", outcome.Message);
            }
            Equal("the earlier file", File.ReadAllText(existing));
            // A good write replaces the earlier file in one step and leaves no temp file.
            var good = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(existing)));
            Equal(ExportOutcomeKind.Written, good.Kind);
            True(File.ReadAllBytes(existing).AsSpan().SequenceEqual(session.Result!.Bytes), "replaced");
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    // Ruling 194 (5): Ruling 146's OneDrive refusal protects the project store and does not reach an export, which is a copy.
    private static void OneDrive()
    {
        string folder = NewFolder();
        try
        {
            string synced = Path.Combine(folder, "OneDrive - Example Company", "Documents");
            Directory.CreateDirectory(synced);
            using var controller = OpenExample();
            var outcome = Wait(Session(controller).RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(synced, name))));
            Equal(ExportOutcomeKind.Written, outcome.Kind);
            True(File.Exists(Path.Combine(synced, "basic-foil-root-r1.dat")), "written into a OneDrive-named folder");
        }
        finally { Directory.Delete(folder, true); }
    }

    // Security review (export-writer boundary): a symlink at the chosen target is refused, and neither the link nor its target changes.
    private static void SymlinkTarget()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            string real = Path.Combine(folder, "real.dat"), link = Path.Combine(folder, "link.dat");
            File.WriteAllText(real, "the file the link points to");
            File.CreateSymbolicLink(link, real);
            var outcome = Wait(Session(controller).RunAsync((_, _) => Task.FromResult<string?>(link)));
            Equal(ExportOutcomeKind.Failed, outcome.Kind);
            Equal("Can't write the file. Nothing was changed. The earlier file is still there.", outcome.Message);
            Equal(real, new FileInfo(link).LinkTarget);
            Equal("the file the link points to", File.ReadAllText(real));
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    // The suggested name carries nothing from the foil or station name but letters, digits and hyphens.
    private static void SuggestedNameIsPlain()
    {
        using var controller = OpenExample();
        foreach (string name in new[] { "../../x", "a/b\\c", "..", "C:\\evil" })
        {
            var source = controller.ExportSnapshot()! with { FoilName = name, Stations = [new("../Root", 1, 12)], DefaultStation = 0 };
            string file = new ExportSession(source).FileName;
            True(!file.Contains('/') && !file.Contains('\\') && !file.Contains("..") && !file.Contains(':'), "separator or .. in " + file);
            True(file.EndsWith("-r1.dat", StringComparison.Ordinal), file);
        }
    }

    // A forced extension must not overwrite a file the panel did not ask about.
    private static void ForcedExtensionNeverOverwrites()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            string other = Path.Combine(folder, "typed.dat");
            File.WriteAllText(other, "someone else's file");
            var outcome = Wait(Session(controller).RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "typed.txt"))));
            Equal(ExportOutcomeKind.Failed, outcome.Kind);
            Equal("someone else's file", File.ReadAllText(other));
            // A .dat the panel itself named was already confirmed by the panel: that one is replaced.
            var named = Wait(Session(controller).RunAsync((_, _) => Task.FromResult<string?>(other)));
            Equal(ExportOutcomeKind.Written, named.Kind);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Extension()
    {
        Equal("a.dat", ExportSession.ForceExtension("a.txt"));
        Equal("a.dat", ExportSession.ForceExtension("a"));
        Equal("a.DAT", ExportSession.ForceExtension("a.DAT"));
        Equal("a.cfdw.dat", ExportSession.ForceExtension("a.cfdw.json"));
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var outcome = Wait(Session(controller).RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "typed.txt"))));
            Equal(Path.Combine(folder, "typed.dat"), outcome.Path);
            True(File.Exists(outcome.Path), "the forced name is the one written");
            True(!File.Exists(Path.Combine(folder, "typed.txt")), "the typed name was not written");
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Draft()
    {
        using var controller = OpenExample();
        Wait(controller.EnterSectionAsync(0, EntryOrigin.Properties));
        var vertex = controller.SectionCurve(SurfaceSide.Upper)!.Points.First(point => point.Freedom != PointFreedom.Fixed);
        Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, vertex.Id, vertex.SpanMeters, vertex.Ordinate + 0.01)));
        var source = controller.ExportSnapshot()!;
        Equal(true, source.DraftOpen);
        Equal(controller.AcceptedSource, Encoding.UTF8.GetString(source.Source));
        var session = new ExportSession(source);
        Equal("Exporting revision r1. Your open draft is not included.", session.SummaryRows.Single(row => row.Label == "Revision").Value);
        True(session.CanExport, "an open draft does not block (not a block, H1)");
        var host = new ShellHost(controller);
        Equal(true, host.CanRun("file.export"));
        controller.CancelSection();
        Equal(false, controller.ExportSnapshot()!.DraftOpen);
    }

    private static void AnalysisLine()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()! with { Analysis = true };
        var session = new ExportSession(source);
        var rows = session.SummaryRows;
        Equal("Analysis layers and results are not exported.", rows.Single(row => row.Label == "Analysis").Value);
        Equal(Session(controller).Result!.Bytes.Length, session.Result!.Bytes.Length);
        True(Session(controller).Result!.Bytes.AsSpan().SequenceEqual(session.Result.Bytes), "Analysis mode reads the same accepted revision");
    }

    private static ExportDialog Dialog(WorkbenchController controller, out ExportSession session, Func<string, string?, Task<string?>>? pick = null,
        Action? jump = null)
    {
        session = Session(controller);
        var dialog = new ExportDialog(session, pick ?? ((_, _) => Task.FromResult<string?>(null)), jump);
        dialog.Show();
        Settle();
        return dialog;
    }

    internal static IEnumerable<string> Texts(Window window) => window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? "")
        .Concat(window.GetVisualDescendants().OfType<ContentControl>().Select(item => item.Content as string ?? ""));

    private static void DialogShape()
    {
        using var controller = OpenExample();
        var dialog = Dialog(controller, out var session);
        try
        {
            var formats = dialog.GetVisualDescendants().OfType<ListBoxItem>().ToArray();
            Equal(3, formats.Length);
            Equal("Section (.dat)", formats[0].Content as string);
            Equal("Wing (STL)", formats[1].Content as string);
            Equal("Wing (3MF)", formats[2].Content as string);
            string all = string.Join("\n", Texts(dialog));
            True(all.Contains("Wing (3MF)", StringComparison.Ordinal), "the 3MF row is in the format list");
            // The STL options are in the tree but not on screen while the .dat is chosen.
            Equal(false, dialog.FindControl<StackPanel>("StlOptions")!.IsVisible);
            Equal(true, dialog.FindControl<StackPanel>("DatOptions")!.IsVisible);
            var step = dialog.FindControl<Button>("StepButton")!;
            Equal(false, step.IsEnabled);
            Equal(ExportCopy.StepUnavailable, dialog.FindControl<TextBlock>("StepWhy")!.Text);
            Equal("Export", dialog.FindControl<TextBlock>("TitleText")!.Text);
            Equal("What will be written", dialog.FindControl<TextBlock>("SummaryHeading")!.Text);
            Equal(ExportCopy.Safety, dialog.FindControl<TextBlock>("SafetyLine")!.Text);
            Equal("Export…", dialog.FindControl<Button>("ExportButton")!.Content as string);
            Equal("Cancel", dialog.FindControl<Button>("CancelButton")!.Content as string);
            Equal(true, dialog.FindControl<Button>("ExportButton")!.IsDefault);
            Equal(true, dialog.FindControl<Border>("FindingBand")!.IsVisible);
            Equal("Trailing edge 0.00 mm, below the floor of 0.30 mm (app default, no source)", dialog.FindControl<TextBlock>("FindingText")!.Text);
            Equal(false, dialog.FindControl<Border>("BlockedBand")!.IsVisible);
            // The options drive the session and the summary redraws from it.
            dialog.FindControl<RadioButton>("ShapeOwn")!.IsChecked = true;
            dialog.FindControl<RadioButton>("OrderLednicer")!.IsChecked = true;
            dialog.FindControl<RadioButton>("Points201")!.IsChecked = true;
            dialog.FindControl<ComboBox>("StationBox")!.SelectedIndex = 1;
            Settle();
            Equal(DatShape.Own, session.Shape);
            Equal(DatOrder.Lednicer, session.Order);
            Equal(201, session.PointsPerSurface);
            Equal("Tip", session.Station.Name);
            True(string.Join("\n", Texts(dialog)).Contains("402 in the file (201 per surface), Lednicer order", StringComparison.Ordinal), "summary follows the options");
        }
        finally { dialog.Close(); Settle(); }
    }

    private static void DialogKeys()
    {
        using var controller = OpenExample();
        var dialog = Dialog(controller, out _);
        try
        {
            True(dialog.FindControl<Button>("ExportButton")!.IsEnabled, "Export... is enabled for an accepted foil");
            Dispatcher.UIThread.RunJobs();
            Equal(true, dialog.IsVisible);
            dialog.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = dialog, Key = Key.Escape });
            Settle();
            Equal(false, dialog.IsVisible);
        }
        finally { dialog.Close(); Settle(); }
        string folder = NewFolder();
        try
        {
            using var second = OpenExample();
            var session = Session(second);
            var enter = new ExportDialog(session, (name, _) => Task.FromResult<string?>(Path.Combine(folder, name)));
            enter.Show();
            Settle();
            Wait(enter.ExportAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
            Settle();
            True(File.Exists(Path.Combine(folder, "basic-foil-root-r1.dat")), "Export... wrote the file");
            Equal(ExportOutcomeKind.Written, enter.Outcome!.Kind);
            Equal(false, enter.IsVisible);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void DialogFailure()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            string gone = Path.Combine(folder, "missing", "x.dat");
            var dialog = Dialog(controller, out _, (_, _) => Task.FromResult<string?>(gone));
            try
            {
                Wait(dialog.ExportAsync((_, _) => Task.FromResult<string?>(gone)));
                Settle();
                Equal(true, dialog.IsVisible);
                Equal(true, dialog.FindControl<Border>("FailureBand")!.IsVisible);
                Equal("Can't write the file. The folder no longer exists. Nothing was changed.", dialog.FindControl<TextBlock>("FailureText")!.Text);
                Equal("Choose another place…", dialog.FindControl<Button>("AnotherPlaceButton")!.Content as string);
                Equal("Try again", dialog.FindControl<Button>("TryAgainButton")!.Content as string);
                // Try again after the folder exists writes the same path.
                Directory.CreateDirectory(Path.Combine(folder, "missing"));
                Wait(dialog.ExportAsync((_, _) => Task.FromResult<string?>(gone)));
                Settle();
                True(File.Exists(gone), "the retry wrote the file");
                Equal(false, dialog.IsVisible);
            }
            finally { dialog.Close(); Settle(); }
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Jump()
    {
        using var controller = OpenExample();
        var host = new ShellHost(controller);
        var session = Session(controller);
        session.Set(station: 1);
        Wait(host.ShowExportStationAsync(session));
        Settle();
        True(controller.Section is not null, "its section is open, where the trailing-edge gap is shown");
        Equal(1, controller.Section!.Draft.Assignment, "the section is the chosen station's");
    }

    private static void Screenshot()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()!;
        // The mockup's ready state: Example foil, open 0.26 mm trailing edge, Root, At station, Selig, 101.
        var session = new ExportSession(WithSource(source, OpenTe(source, 0.001)));
        var dialog = new ExportDialog(session, (_, _) => Task.FromResult<string?>(null));
        dialog.Show();
        Settle();
        try
        {
            Equal(true, dialog.FindControl<Border>("FindingBand")!.IsVisible);
            Equal("Show at Root", dialog.FindControl<Button>("JumpButton")!.Content as string);
            True(dialog.Bounds.Width >= 700 && dialog.Bounds.Height > 400, $"dialog drawn at {dialog.Bounds}");
            string? shot = Environment.GetEnvironmentVariable("CFDW_EXPORT_SHOT");
            if (shot is null) return;
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)dialog.Bounds.Width, (int)dialog.Bounds.Height), new Vector(96, 96));
            bitmap.Render(dialog);
            bitmap.Save(shot);
        }
        finally { dialog.Close(); Settle(); }
    }
}
