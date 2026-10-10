using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.ExportTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Export, slice 2: the wing STL in the session and the dialog (docs/design/export.md 4.2, 6, 7; Ruling 196; build conditions B7, B8).
/// Ring: fast, in the `--section-editor` child. Cost: about 4 s together (each check opens the Example, about 0.2 s; the Untitled wing at
/// Fine builds 513,596 triangles in about 0.4 s; one window render).
/// </summary>
public static class ExportStlTests
{
    public static void Run()
    {
        DesktopChecks.Check("ExportStl_Session_PreparingThenReady_Rows_FileName_Fidelity", Ready);
        DesktopChecks.Check("ExportStl_TrailingEdge_B8_WholeSpanRow_Jump_NeverBlocks", TrailingEdge);
        DesktopChecks.Check("ExportStl_Scope_Half_NameRowAndCount", Half);
        DesktopChecks.Check("ExportStl_Presets_DraftPrintFine_TriangleCounts", Presets);
        DesktopChecks.Check("ExportStl_B7_LargeMeshLine_At500000And500001_UntitledFineWholeVsHalf", LargeMesh);
        DesktopChecks.Check("ExportStl_TrailingEdge_LocatedValue_RowJumpAndStation", LocatedValue);
        DesktopChecks.Check("ExportStl_H7_ClosureRefused_NothingWritten_PanelNeverOpens", ClosureRefused);
        DesktopChecks.Check("ExportStl_StaleBuild_NewerOptionWins", StaleBuild);
        DesktopChecks.Check("ExportStl_Write_ForcedStl_NeverProjectFile_SymlinkRefused_NoTemp", Write);
        DesktopChecks.Check("ExportStl_H10_CancelledWrite_RemovesTempFile_EarlierFileUntouched", CancelledWrite);
        DesktopChecks.Check("ExportStl_Shell_FileExport_WritesBytesAndStatusLine", ShellWrites);
        DesktopChecks.Check("ExportStl_Jump_OpensTipSectionWhereTheGapIsShown", Jump);
        DesktopChecks.Check("ExportStl_Dialog_Options_Preparing_Ready_LargeBand_Closure", DialogStates);
        DesktopChecks.Check("ExportStl_Dialog_Renders_ReadyState_Screenshot", Screenshot);
    }

    private static ExportSource Open026(WorkbenchController controller)
    {
        var source = controller.ExportSnapshot()!;
        return WithSource(source, OpenTe(source, 0.001));
    }

    private static ExportSession Stl(ExportSource source, StlScope scope = StlScope.Whole, StlPreset preset = StlPreset.Print,
        Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult>? build = null)
    {
        var session = new ExportSession(source, build);
        session.Set(format: ExportFormat.Stl, scope: scope, preset: preset);
        Wait(session.PrepareAsync());
        return session;
    }

    private static ExportSource Untitled(WorkbenchController controller) =>
        controller.ExportSnapshot()! with { Source = FoilSource.NewDefault(), FoilName = "Untitled" };

    private static Dictionary<string, string> Rows(ExportSession session) => session.SummaryRows.ToDictionary(row => row.Label, row => row.Value);

    private static void Ready()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        session.Set(format: ExportFormat.Stl);
        Equal(true, session.Preparing);
        Equal(false, session.CanExport);
        Equal("Preparing the mesh…", session.ButtonLabel);
        var skeleton = Rows(session);
        True(skeleton.ContainsKey("Mesh") && skeleton["Mesh"].StartsWith("000,000", StringComparison.Ordinal), "the skeleton keeps the rows at their height");
        True(skeleton[ExportCopy.TrailingEdgeLabel].Contains("Manufacturing: not assessed", StringComparison.Ordinal), "the trailing-edge row is in the skeleton");
        Wait(session.PrepareAsync());
        Equal(false, session.Preparing);
        Equal(true, session.CanExport);
        Equal("Export…", session.ButtonLabel);
        Equal("basic-foil-r1-mm.stl", session.FileName);
        var rows = Rows(session);
        Equal("Revision r1, accepted.", rows["Revision"]);
        Equal("mm, unscaled (in the file name)", rows["Unit"]);
        Equal("18,418 triangles (31 × 76 grid)", rows["Mesh"]);
        Equal("0.92 MB", rows["File size"]);
        Equal("120.0 × 900.0 × 15.9 mm", rows["Size of the part"]);
        Equal("Largest deviation from the computed surface: 0.0145 mm, sampled at cell midpoints. Not a bound.", rows["Fidelity"]);
        Equal("Closed: every edge joins two triangles (checked). Coordinates are rounded to 0.00003 mm. Computed by this app. No other CAD program has opened this file.", session.LimitText);
        Equal(0.02, session.ToleranceMm);
        Equal(null, session.ToleranceNotReachedBand);
        Equal(null, session.LargeMeshBand);
        Equal(0, session.PreviewLines.Count);
        // The strip text is the registry's EX20, from the mesh's own numbers.
        string folder = NewFolder();
        try
        {
            var outcome = Wait(session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
            Equal("Exported basic-foil-r1-mm.stl · 18,418 triangles · 0.92 MB · mm · largest measured deviation 0.0145 mm", outcome.Message);
        }
        finally { Directory.Delete(folder, true); }
    }

    // B8: a trailing edge that holds along the whole span reads "along the whole span" and the jump lands on the gap control.
    private static void TrailingEdge()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()!;
        var open = Stl(WithSource(source, OpenTe(source, 0.001)));
        Equal("0.26 mm along the whole span. Floor 0.30 mm (app default, no source).", open.TrailingEdgeLine);
        Equal("Trailing edge 0.26 mm, below the floor of 0.30 mm (app default, no source)", open.Finding!.Text);
        Equal("Show the trailing-edge gap", open.Finding.Jump);
        True(open.CanExport, "below the floor is advisory only (Ruling 194 (5))");
        Equal("0.26 mm along the whole span. Floor 0.30 mm (app default, no source).\nManufacturing: not assessed (no process chosen)", Rows(open)["Trailing edge"]);
        var meets = Stl(WithSource(source, OpenTe(source, 0.0014)));
        Equal(null, meets.Finding);
        Equal("0.36 mm along the whole span. Floor 0.30 mm (app default, no source).", meets.TrailingEdgeLine);
        var closed = Stl(source);
        Equal("0.00 mm along the whole span. Floor 0.30 mm (app default, no source).", closed.TrailingEdgeLine);
        Equal("Show the trailing-edge gap", closed.Finding!.Jump);
        var untitled = Stl(Untitled(controller));
        Equal("0.00 mm along the whole span. Floor 0.30 mm (app default, no source).", untitled.TrailingEdgeLine);
        Equal("Show the trailing-edge gap", untitled.Finding!.Jump);
        Equal(1, untitled.JumpStationIndex);
    }

    private static void Half()
    {
        using var controller = OpenExample();
        var session = Stl(Open026(controller), StlScope.Half);
        Equal("basic-foil-r1-half-mm.stl", session.FileName);
        Equal("9,358 triangles (31 × 76 grid), with a flat root face", Rows(session)["Mesh"]);
        Equal("120.0 × 450.0 × 15.9 mm", Rows(session)["Size of the part"]);
        Equal("0.47 MB", Rows(session)["File size"]);
        var whole = Stl(Open026(controller));
        True(session.Stl!.Triangles < whole.Stl!.Triangles && session.FileName != whole.FileName, "the half cannot overwrite the whole wing");
    }

    private static void Presets()
    {
        using var controller = OpenExample();
        var source = Open026(controller);
        Equal(8_278, Stl(source, preset: StlPreset.Draft).Stl!.Triangles);
        Equal(18_418, Stl(source, preset: StlPreset.Print).Stl!.Triangles);
        Equal(129_118, Stl(source, preset: StlPreset.Fine).Stl!.Triangles);
        Equal(StlPreset.Print, new ExportSession(source).Preset);
    }

    private static StlExportResult Fake(int triangles, double teMm = 0.5, double yMm = 450, bool atTip = true, bool along = true) =>
        new([0], StlScope.Whole, 31, 76, triangles, 1, 0.02, 0.0145, true, teMm, yMm, atTip, along, 120, 900, 15.9, 1);

    // B7: the band and the button appear above 500,000 triangles, not at 500,000. The real case: the Untitled wing at Fine.
    private static void LargeMesh()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()!;
        var at = Stl(source, build: (_, _, _, _, _, _) => Fake(500_000));
        Equal(false, at.IsLarge);
        Equal(null, at.LargeMeshBand);
        Equal("Export…", at.ButtonLabel);
        var over = Stl(source, build: (_, _, _, _, _, _) => Fake(500_001));
        Equal(true, over.IsLarge);
        Equal("Export anyway…", over.ButtonLabel);
        var untitled = Untitled(controller);
        var whole = Stl(untitled, preset: StlPreset.Fine);
        Equal(513_596, whole.Stl!.Triangles);
        Equal(true, whole.IsLarge);
        Equal("This mesh has 513,596 triangles (25.68 MB). Some slicers and CAD programs open it slowly. Choose Print for 128,796 triangles, or write it anyway.", whole.LargeMeshBand);
        Equal(true, whole.CanExport);
        var half = Stl(untitled, StlScope.Half, StlPreset.Fine);
        Equal(257_596, half.Stl!.Triangles);
        Equal(false, half.IsLarge);
        // The tolerance band when even the finest mesh misses the preset (EX31): a builder that reports a miss.
        var missed = Stl(source, build: (_, _, _, _, _, _) => Fake(1000) with { ToleranceMet = false, DeviationMm = 0.0123, ToleranceMm = 0.02 });
        Equal("The tolerance was not reached. The finest mesh has a largest measured deviation of 0.0123 mm, over the 0.02 mm you chose.", missed.ToleranceNotReachedBand);
    }

    private static void LocatedValue()
    {
        using var controller = OpenExample();
        var source = controller.ExportSnapshot()!;
        var middle = Stl(source, build: (_, _, _, _, _, _) => Fake(1000, teMm: 0.1, yMm: 400, atTip: false, along: false));
        Equal("Least thickness 0.10 mm at y = 400 mm. Floor 0.30 mm (app default, no source).", middle.TrailingEdgeLine);
        Equal("Show at y = 400 mm", middle.Finding!.Jump);
        Equal(1, middle.JumpStationIndex);
        var root = Stl(source, build: (_, _, _, _, _, _) => Fake(1000, teMm: 0.1, yMm: 20, atTip: false, along: false));
        Equal(0, root.JumpStationIndex);
        var tip = Stl(source, build: (_, _, _, _, _, _) => Fake(1000, teMm: 0.1, atTip: true, along: false));
        Equal("Least thickness 0.10 mm at the tip. Floor 0.30 mm (app default, no source).", tip.TrailingEdgeLine);
        Equal("Show at tip", tip.Finding!.Jump);
    }

    private static void ClosureRefused()
    {
        using var controller = OpenExample();
        var session = Stl(controller.ExportSnapshot()!, build: (_, _, _, _, _, _) => throw new ContractError("EXPORT-NOT-CLOSED"));
        Equal(true, session.ClosureFailed);
        Equal(false, session.CanExport);
        Equal(null, session.Stl);
        Equal("The mesh did not close, so nothing was written.", session.ClosureMessage);
        bool asked = false;
        var outcome = Wait(session.RunAsync((_, _) => { asked = true; return Task.FromResult<string?>("never.stl"); }));
        Equal(ExportOutcomeKind.Failed, outcome.Kind);
        Equal(false, asked, "the save panel opened for a mesh that did not close");
        // A real refusal comes from the writer: a surface that cannot close is refused by StlExport.Build, not written.
        Equal(true, ExportCopy.MeshNotClosedDetails.StartsWith("Technical details:", StringComparison.Ordinal));
    }

    private static void StaleBuild()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        session.Set(format: ExportFormat.Stl);
        var first = session.PrepareAsync();
        session.Set(preset: StlPreset.Fine);
        Wait(first);
        Equal(true, session.Preparing);
        Equal(null, session.Stl);
        Wait(session.PrepareAsync());
        Equal(129_118, session.Stl!.Triangles);
        // Back to the .dat: the mesh is dropped and the .dat is there at once.
        session.Set(format: ExportFormat.Dat);
        Equal(null, session.Stl);
        True(session.Result is not null && session.CanExport, "the .dat is built at once");
    }

    private static void Write()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var session = Stl(Open026(controller));
            Equal("a.stl", ExportSession.ForceExtension("a.txt", ".stl"));
            Equal("a.cfdw.stl", ExportSession.ForceExtension("a.cfdw.json", ".stl"));
            Equal("a.STL", ExportSession.ForceExtension("a.STL", ".stl"));
            var outcome = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "typed.txt"))));
            Equal(ExportOutcomeKind.Written, outcome.Kind);
            Equal(Path.Combine(folder, "typed.stl"), outcome.Path);
            True(File.ReadAllBytes(outcome.Path!).AsSpan().SequenceEqual(session.Stl!.Bytes), "the file is the writer's bytes");
            True(StlExport.Check(File.ReadAllBytes(outcome.Path!)).Closed, "the file on disk passes the edge check");
            // A forced name that exists is never replaced: the panel confirmed the typed name, not this one.
            var kept = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "typed.txt"))));
            Equal(ExportOutcomeKind.Failed, kept.Kind);
            // A symlink at the target is refused, and neither link nor target changes.
            if (!OperatingSystem.IsWindows())
            {
                string target = Path.Combine(folder, "real.stl"), link = Path.Combine(folder, "link.stl");
                File.WriteAllText(target, "the earlier file");
                File.CreateSymbolicLink(link, target);
                var refused = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(link)));
                Equal(ExportOutcomeKind.Failed, refused.Kind);
                Equal("the earlier file", File.ReadAllText(target));
                True(new FileInfo(link).LinkTarget is not null, "the link is still a link");
            }
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void CancelledWrite()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var session = Stl(Open026(controller));
            string existing = Path.Combine(folder, "basic-foil-r1-mm.stl");
            File.WriteAllText(existing, "the earlier file");
            using var cancel = new CancellationTokenSource();
            cancel.Cancel();
            var outcome = Wait(session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name)), null, cancel.Token));
            Equal(ExportOutcomeKind.Cancelled, outcome.Kind);
            Equal("Export cancelled. Nothing was written.", outcome.Message);
            Equal("the earlier file", File.ReadAllText(existing));
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
            Equal(1, Directory.GetFiles(folder).Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void ShellWrites()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var host = new ShellHost(controller);
            host.ShowExportDialog = async session =>
            {
                session.Set(format: ExportFormat.Stl, scope: StlScope.Half, preset: StlPreset.Draft);
                await session.PrepareAsync();
                return await session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name)));
            };
            Wait(host.RunCommand("file.export"));
            string path = Path.Combine(folder, "basic-foil-r1-half-mm.stl");
            True(File.Exists(path), "the half wing file is named for its scope");
            var check = StlExport.Check(File.ReadAllBytes(path));
            True(check.Closed && check.Euler == 2 && check.VolumeMm3 > 0, "the written file is a closed solid");
            True(host.StatusStrip.Text.StartsWith("Exported basic-foil-r1-half-mm.stl · ", StringComparison.Ordinal) && host.StatusStrip.Text.Contains(" triangles · ", StringComparison.Ordinal), host.StatusStrip.Text);
            Equal(OperatingSystem.IsWindows() ? "Show in Explorer" : "Show in Finder", host.StatusStrip.Action?.Label);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Jump()
    {
        using var controller = OpenExample();
        var host = new ShellHost(controller);
        var session = Stl(Open026(controller));
        Wait(host.ShowExportStationAsync(session));
        Settle();
        True(controller.Section is not null, "the section opens, where the trailing-edge gap is shown");
        Equal(1, controller.Section!.Draft.Assignment, "the tip's section, where the whole-span trailing edge is read");
    }

    private static void DialogStates()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        var dialog = new ExportDialog(session, (_, _) => Task.FromResult<string?>(null));
        dialog.Show();
        Settle();
        try
        {
            var stlItem = dialog.FindControl<ListBoxItem>("StlFormatItem")!;
            stlItem.IsSelected = true;
            Settle();
            Equal(ExportFormat.Stl, session.Format);
            // Right after the choice the mesh is being prepared: skeleton, the loading line, Export... disabled with its label.
            Equal(true, dialog.FindControl<StackPanel>("StlOptions")!.IsVisible);
            Equal(false, dialog.FindControl<StackPanel>("DatOptions")!.IsVisible);
            Equal("Preparing the mesh…", dialog.FindControl<TextBlock>("LimitLine")!.Text);
            Equal(false, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
            Equal("Preparing the mesh…", dialog.FindControl<Button>("ExportButton")!.Content as string);
            Wait(dialog.Preparation);
            Settle();
            string all = string.Join("\n", Texts(dialog));
            foreach (string present in new[] { "Wing (STL)", "Scope", "Whole wing", "Starboard half", "Tolerance", "Draft 0.05 mm", "Print 0.02 mm", "Fine 0.005 mm",
                         "Unit", "18,418 triangles (31 × 76 grid)", "0.92 MB", "along the whole span" })
                if (!all.Contains(present, StringComparison.Ordinal)) throw new InvalidOperationException($"'{present}' is not on the STL dialog:\n{all}");
            True(all.Contains("Wing (3MF)", StringComparison.Ordinal), "the 3MF row is in the format list");
            Equal(true, dialog.FindControl<RadioButton>("TolerancePrint")!.IsChecked);
            Equal(true, dialog.FindControl<RadioButton>("ScopeWhole")!.IsChecked);
            Equal(true, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
            Equal("Export…", dialog.FindControl<Button>("ExportButton")!.Content as string);
            Equal(false, dialog.FindControl<TextBlock>("ScopeHelp")!.IsVisible);
            Equal(false, dialog.FindControl<Border>("PreviewBorder")!.IsVisible);
            Equal("Show the trailing-edge gap", dialog.FindControl<Button>("JumpButton")!.Content as string);
            Equal(session.LimitText, dialog.FindControl<TextBlock>("LimitLine")!.Text);
            // The options drive the session: the half shows its help line, the Fine preset the finer mesh.
            dialog.FindControl<RadioButton>("ScopeHalf")!.IsChecked = true;
            dialog.FindControl<RadioButton>("ToleranceFine")!.IsChecked = true;
            Settle();
            Wait(dialog.Preparation);
            Settle();
            Equal(StlScope.Half, session.Scope);
            Equal(StlPreset.Fine, session.Preset);
            Equal(64_958, session.Stl!.Triangles);
            Equal(true, dialog.FindControl<TextBlock>("ScopeHelp")!.IsVisible);
            Equal(ExportCopy.ScopeHalfHelp, dialog.FindControl<TextBlock>("ScopeHelp")!.Text);
            // Back to the .dat: its options and preview return.
            dialog.FindControl<ListBoxItem>("DatFormatItem")!.IsSelected = true;
            Settle();
            Equal(true, dialog.FindControl<StackPanel>("DatOptions")!.IsVisible);
            Equal(true, dialog.FindControl<Border>("PreviewBorder")!.IsVisible);
        }
        finally { dialog.Close(); Settle(); }
        // The large-mesh band and the closure refusal.
        var large = new ExportSession(Untitled(controller));
        var big = new ExportDialog(large, (_, _) => Task.FromResult<string?>(null));
        big.Show();
        Settle();
        try
        {
            large.Set(format: ExportFormat.Stl, preset: StlPreset.Fine);
            Wait(large.PrepareAsync());
            big.Refresh();
            Equal(true, big.FindControl<Border>("LargeBand")!.IsVisible);
            Equal("Export anyway…", big.FindControl<Button>("ExportButton")!.Content as string);
            Equal(true, big.FindControl<Button>("ExportButton")!.IsEnabled);
        }
        finally { big.Close(); Settle(); }
        var refused = Stl(controller.ExportSnapshot()!, build: (_, _, _, _, _, _) => throw new ContractError("EXPORT-NOT-CLOSED"));
        var closed = new ExportDialog(refused, (_, _) => Task.FromResult<string?>(null));
        closed.Show();
        Settle();
        try
        {
            Equal(true, closed.FindControl<Border>("ClosureBand")!.IsVisible);
            Equal("The mesh did not close, so nothing was written.", closed.FindControl<TextBlock>("ClosureText")!.Text);
            Equal(false, closed.FindControl<Button>("ExportButton")!.IsEnabled);
        }
        finally { closed.Close(); Settle(); }
    }

    // The mockup's STL ready state: Example foil, open 0.26 mm trailing edge, whole wing, Print. Set CFDW_EXPORT_SHOT_STL to save a PNG.
    private static void Screenshot()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        var dialog = new ExportDialog(session, (_, _) => Task.FromResult<string?>(null));
        dialog.Show();
        Settle();
        try
        {
            dialog.FindControl<ListBoxItem>("StlFormatItem")!.IsSelected = true;
            Settle();
            Wait(dialog.Preparation);
            Settle();
            Equal(true, dialog.FindControl<Border>("FindingBand")!.IsVisible);
            Equal("Show the trailing-edge gap", dialog.FindControl<Button>("JumpButton")!.Content as string);
            True(dialog.Bounds.Width >= 700 && dialog.Bounds.Height > 400, $"dialog drawn at {dialog.Bounds}");
            string? shot = Environment.GetEnvironmentVariable("CFDW_EXPORT_SHOT_STL");
            if (shot is null) return;
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)dialog.Bounds.Width, (int)dialog.Bounds.Height), new Vector(96, 96));
            bitmap.Render(dialog);
            bitmap.Save(shot);
        }
        finally { dialog.Close(); Settle(); }
    }
}
