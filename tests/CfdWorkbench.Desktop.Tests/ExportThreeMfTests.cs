using CfdWorkbench.Analysis.Export;
using Avalonia;
using Avalonia.Controls;
using CfdWorkbench.Core;
using static CfdWorkbench.Desktop.Tests.ExportTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Export, slice 3: the wing 3MF in the session and the dialog (docs/design/export.md 4.3, 6, 7; Ruling 196). It has the STL's scope, presets, summary,
/// trailing-edge row and hard states, so these checks hold the 3MF to the STL's numbers and add what differs: the name, the unit row, the file size
/// read from the package, the extension, the writer path. Ring: fast, in the `--section-editor` child. Cost: about 1.5 s together (each check opens the
/// Example, about 0.2 s; one window render).
/// </summary>
public static class ExportThreeMfTests
{
    public static void Run()
    {
        DesktopChecks.Check("ExportThreeMf_Session_PreparingThenReady_Rows_FileName_MatchTheStl", Ready);
        DesktopChecks.Check("ExportThreeMf_Scope_Half_NameAndCount_ChangingFormatRebuilds", Half);
        DesktopChecks.Check("ExportThreeMf_Write_ForcedExtension_ClosedPackage_StatusLine_CancelledLeavesNoTemp", Write);
        DesktopChecks.Check("ExportThreeMf_H7_ClosureRefused_NothingWritten", ClosureRefused);
        DesktopChecks.Check("ExportThreeMf_Dialog_FormatRow_SharedOptions_Ready_PickerName", DialogStates);
        DesktopChecks.Check("ExportThreeMf_Dialog_Renders_ReadyState_Screenshot", Screenshot);
    }

    private static ExportSource Open026(WorkbenchController controller)
    {
        var source = controller.ExportSnapshot()!;
        return WithSource(source, OpenTe(source, 0.001));
    }

    private static ExportSession ThreeMf(ExportSource source, StlScope scope = StlScope.Whole, StlPreset preset = StlPreset.Print,
        Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult>? build = null)
    {
        var session = new ExportSession(source, null, build);
        session.Set(format: ExportFormat.ThreeMf, scope: scope, preset: preset);
        Wait(session.PrepareAsync());
        return session;
    }

    private static Dictionary<string, string> Rows(ExportSession session) => session.SummaryRows.ToDictionary(row => row.Label, row => row.Value);

    private static void Ready()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        session.Set(format: ExportFormat.ThreeMf);
        Equal(true, session.Preparing);
        Equal(false, session.CanExport);
        Equal("Preparing the mesh…", session.ButtonLabel);
        True(Rows(session)["Mesh"].StartsWith("000,000", StringComparison.Ordinal), "the skeleton keeps the rows at their height");
        Wait(session.PrepareAsync());
        Equal(false, session.Preparing);
        Equal(true, session.CanExport);
        Equal("Export…", session.ButtonLabel);
        Equal("basic-foil-r1.3mf", session.FileName);
        var rows = Rows(session);
        Equal("Revision r1, accepted.", rows["Revision"]);
        Equal("mm, unscaled (the unit is set in the file)", rows["Unit"]);
        Equal("18,418 triangles (31 × 76 grid)", rows["Mesh"]);
        // The file size is the package's own length, not the STL formula: a 3MF is XML in a ZIP.
        Equal(ExportCopy.Fixed(session.Stl!.Bytes.Length / 1e6, 2) + " MB", rows["File size"]);
        True(rows["File size"] != "0.92 MB", "not the STL's size: " + rows["File size"]);
        Equal("120.0 × 900.0 × 15.9 mm", rows["Size of the part"]);
        Equal("Largest deviation from the computed surface: 0.0145 mm, sampled at cell midpoints. Not a bound.", rows["Fidelity"]);
        Equal("Closed: every edge joins two triangles (checked). Coordinates are rounded to 0.00003 mm. Computed by this app. No other CAD program has opened this file.", session.LimitText);
        // The same mesh, trailing-edge row and finding as the STL.
        var stl = new ExportSession(Open026(controller));
        stl.Set(format: ExportFormat.Stl);
        Wait(stl.PrepareAsync());
        Equal(stl.Stl!.Triangles, session.Stl.Triangles);
        Equal(stl.TrailingEdgeLine, session.TrailingEdgeLine);
        Equal(stl.Finding!.Text, session.Finding!.Text);
        Equal(stl.Finding.Jump, session.Finding.Jump);
        Equal(stl.JumpStationIndex, session.JumpStationIndex);
        Equal(0, session.PreviewLines.Count);
        string folder = NewFolder();
        try
        {
            var outcome = Wait(session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
            Equal($"Exported basic-foil-r1.3mf · 18,418 triangles · {ExportCopy.Fixed(session.Stl.Bytes.Length / 1e6, 2)} MB · mm · largest measured deviation 0.0145 mm", outcome.Message);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Half()
    {
        using var controller = OpenExample();
        var session = ThreeMf(Open026(controller), StlScope.Half);
        Equal("basic-foil-r1-half.3mf", session.FileName);
        Equal("9,358 triangles (31 × 76 grid), with a flat root face", Rows(session)["Mesh"]);
        // Changing the format back and forth builds the format asked for, not the last one built.
        session.Set(format: ExportFormat.Stl);
        Equal(true, session.Preparing);
        Wait(session.PrepareAsync());
        True(session.Stl!.Bytes.Length == 84 + 50 * 9_358, "the STL bytes after switching to STL");
        session.Set(format: ExportFormat.ThreeMf);
        Wait(session.PrepareAsync());
        True(session.Stl!.Bytes.Length != 84 + 50 * 9_358 && ThreeMfExport.Check(session.Stl.Bytes).Closed, "the 3MF package after switching back");
    }

    private static void Write()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var session = ThreeMf(Open026(controller), StlScope.Half, StlPreset.Draft);
            // The extension is forced to .3mf; a name the app changed is never replaced, a project file name is never kept.
            var outcome = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(folder, "wing.cfdw.json"))));
            Equal(ExportOutcomeKind.Written, outcome.Kind);
            string path = Path.Combine(folder, "wing.cfdw.3mf");
            Equal(path, outcome.Path);
            var check = ThreeMfExport.Check(File.ReadAllBytes(path));
            True(check.Closed && check.Euler == 2 && check.VolumeMm3 > 0, "the written file is a closed solid");
            Equal(1, Directory.GetFiles(folder).Length);
            Equal("wing.cfdw.3mf", ExportSession.ForceExtension("wing.cfdw.json", ".3mf"));
            // H10: a cancelled write removes its temp file and leaves the earlier file alone.
            string existing = Path.Combine(folder, "basic-foil-r1-half.3mf");
            File.WriteAllText(existing, "the earlier file");
            using var cancel = new CancellationTokenSource();
            cancel.Cancel();
            var cancelled = Wait(session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name)), null, cancel.Token));
            Equal(ExportOutcomeKind.Cancelled, cancelled.Kind);
            Equal("the earlier file", File.ReadAllText(existing));
            Equal(0, Directory.GetFiles(folder, ".cfd-*.tmp").Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void ClosureRefused()
    {
        using var controller = OpenExample();
        var refused = ThreeMf(controller.ExportSnapshot()!, build: (_, _, _, _, _, _) => throw new ContractError("EXPORT-NOT-CLOSED"));
        Equal(true, refused.ClosureFailed);
        Equal(false, refused.CanExport);
        Equal("The mesh did not close, so nothing was written.", refused.ClosureMessage);
        string folder = NewFolder();
        try
        {
            bool opened = false;
            var outcome = Wait(refused.RunAsync((name, _) => { opened = true; return Task.FromResult<string?>(Path.Combine(folder, name)); }));
            Equal(ExportOutcomeKind.Failed, outcome.Kind);
            Equal(false, opened);
            Equal(0, Directory.GetFiles(folder).Length);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void DialogStates()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        string? offered = null;
        var dialog = new ExportDialog(session, (name, _) => { offered = name; return Task.FromResult<string?>(null); });
        dialog.Show();
        Settle();
        try
        {
            var item = dialog.FindControl<ListBoxItem>("ThreeMfFormatItem")!;
            Equal("Wing (3MF)", item.Content as string);
            Equal(3, dialog.FindControl<ListBox>("FormatList")!.ItemCount);
            item.IsSelected = true;
            Settle();
            Equal(ExportFormat.ThreeMf, session.Format);
            // The STL's options and states: scope, presets, the locked unit row, the skeleton while preparing.
            Equal(true, dialog.FindControl<StackPanel>("StlOptions")!.IsVisible);
            Equal(false, dialog.FindControl<StackPanel>("DatOptions")!.IsVisible);
            Equal("Preparing the mesh…", dialog.FindControl<TextBlock>("LimitLine")!.Text);
            Equal(false, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
            Wait(dialog.Preparation);
            Settle();
            string all = string.Join("\n", Texts(dialog));
            foreach (string present in new[] { "Wing (3MF)", "Wing (STL)", "Scope", "Whole wing", "Starboard half", "Tolerance", "Draft 0.05 mm", "Print 0.02 mm",
                         "Fine 0.005 mm", "Unit", "18,418 triangles (31 × 76 grid)", "along the whole span", "the unit is set in the file" })
                if (!all.Contains(present, StringComparison.Ordinal)) throw new InvalidOperationException($"'{present}' is not on the 3MF dialog:\n{all}");
            Equal(true, dialog.FindControl<Button>("ExportButton")!.IsEnabled);
            Equal("Show the trailing-edge gap", dialog.FindControl<Button>("JumpButton")!.Content as string);
            dialog.FindControl<RadioButton>("ScopeHalf")!.IsChecked = true;
            Settle();
            Wait(dialog.Preparation);
            Settle();
            Equal(ExportFormat.ThreeMf, session.Format);
            Equal(StlScope.Half, session.Scope);
            Equal(9_358, session.Stl!.Triangles);
            Equal(true, dialog.FindControl<TextBlock>("ScopeHelp")!.IsVisible);
            // The save panel is offered the 3MF name; the picker's file type follows the name.
            Wait(dialog.ExportAsync((name, folder) => { offered = name; return Task.FromResult<string?>(null); }));
            Equal("basic-foil-r1-half.3mf", offered);
            // Back to STL and to .dat: each format's options return.
            dialog.FindControl<ListBoxItem>("StlFormatItem")!.IsSelected = true;
            Settle();
            Wait(dialog.Preparation);
            Equal(ExportFormat.Stl, session.Format);
            dialog.FindControl<ListBoxItem>("DatFormatItem")!.IsSelected = true;
            Settle();
            Equal(true, dialog.FindControl<StackPanel>("DatOptions")!.IsVisible);
        }
        finally { dialog.Close(); Settle(); }
    }

    // The mockup's ready state with the 3MF row chosen. Set CFDW_EXPORT_SHOT_3MF to save a PNG.
    private static void Screenshot()
    {
        using var controller = OpenExample();
        var session = new ExportSession(Open026(controller));
        var dialog = new ExportDialog(session, (_, _) => Task.FromResult<string?>(null));
        dialog.Show();
        Settle();
        try
        {
            dialog.FindControl<ListBoxItem>("ThreeMfFormatItem")!.IsSelected = true;
            Settle();
            Wait(dialog.Preparation);
            Settle();
            Equal(true, dialog.FindControl<Border>("FindingBand")!.IsVisible);
            True(dialog.Bounds.Width >= 700 && dialog.Bounds.Height > 400, $"dialog drawn at {dialog.Bounds}");
            string? shot = Environment.GetEnvironmentVariable("CFDW_EXPORT_SHOT_3MF");
            if (shot is null) return;
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)dialog.Bounds.Width, (int)dialog.Bounds.Height), new Vector(96, 96));
            bitmap.Render(dialog);
            bitmap.Save(shot);
        }
        finally { dialog.Close(); Settle(); }
    }
}
