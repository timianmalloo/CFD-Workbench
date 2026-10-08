using CfdWorkbench.Desktop;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Automation;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Media;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>M1.2d fast-ring dialog and rendered-surface checks.</summary>
public static class CatalogDialogTests
{
    private static readonly string[] CatalogNames =
    [
        "CatalogDialog_Open_FocusInSearch", "CatalogDialog_FamiliesGrouped_ListboxShape",
        "CatalogDialog_ArrowsReachDisabledRows_Quiet", "CatalogDialog_TypingPause_AnnouncesCountOnce",
        "CatalogDialog_NoMatch_Copy115ReplaceDisabled", "CatalogDialog_PendingRowEnter_NothingChanges",
        "CatalogDialog_SharedPreview_DetailNamesStationsFitLimit", "CatalogDialog_Replace_FocusToSectionMenu",
        "CatalogDialog_Escape_NothingChangedFocusToSectionMenu", "CatalogDialog_SpacingRefusal_ReasonAndChainOffered",
        "CatalogDialog_ChainButton_AppliesAllStations", "CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel",
        "CatalogDialog_MySectionsEmpty_ShowsNextAction", "CatalogDialog_DoubleClickRow_Replaces",
        "CatalogDialog_Preview_LargestChangeMarkerAtMeasuredX"
    ];
    private static readonly string[] SaveNames =
    [
        "SaveDialog_Open_FocusInName", "SaveDialog_EmptyOrDuplicate_ErrorFocusStays",
        "SaveDialog_Escape_NothingSavedFocusToSectionMenu", "SaveDialog_Save_LiveRegionFocusToSectionMenu"
    ];

    public static void Run()
    {
        foreach (string name in CatalogNames)
            DesktopChecks.Check(name, () => CheckCatalog(name));
        foreach (string name in SaveNames)
            DesktopChecks.Check(name, () => CheckSave(name));
        DesktopChecks.Check("SectionCanvas_Preview_DashedAccentPixelsOverCurrent", CheckCanvasPixels);
        DesktopChecks.Check("SourceChip_AfterReplaceAndEdit_TextNotColour", CheckChipAfterEdit);
        DesktopChecks.Check("SourceChip_StationTcDiffers_SaysScaled", CheckScaledChip);
        DesktopChecks.Check("BrowserRow_AfterReplace_NameThenSource", CheckBrowserText);
        DesktopChecks.Check("CatalogDialog_DamagedLibraryRow_DisabledWithReason", CheckDamagedRow);
        DesktopChecks.Check("SaveDialog_UnsupportedPersistence_ExplainsSafety", CheckUnsupportedSave);
        DesktopChecks.Check("Properties_StationSource_UsesProfileProvenance", CheckStationSource);
    }

    private static void CheckCatalog(string name)
    {
        using var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
        if (name is "CatalogDialog_SpacingRefusal_ReasonAndChainOffered" or "CatalogDialog_ChainButton_AppliesAllStations")
            Wait(controller.ApplySectionStepAsync(new SectionStep.MakeUnique()));
        Action? count = null;
        var snapshot = name == "CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel"
            ? new CatalogSnapshot([], [], 0, 0, 0, "CAT-UNAVAILABLE")
                { FailureCause = "a catalog file is missing from this installation" } : controller.OpenCatalog();
        var dialog = new CatalogDialog(controller, snapshot, schedule: (_, callback) => count = callback);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var search = Need<TextBox>(dialog, "SearchBox");
            var list = Need<ListBox>(dialog, "SectionList");
            var detail = Need<TextBlock>(dialog, "DetailLine");
            var replace = Need<Button>(dialog, "ReplaceButton");
            var chain = Need<Button>(dialog, "ChainButton");
            switch (name)
            {
                case "CatalogDialog_Open_FocusInSearch":
                    Require(ReferenceEquals(dialog.FocusManager?.GetFocusedElement(), search), "Search lacks opening focus");
                    break;
                case "CatalogDialog_FamiliesGrouped_ListboxShape":
                    string[] headings = list.Items.OfType<ListBoxItem>().Where(item => item.Tag is null)
                        .Select(item => item.Content?.ToString() ?? "").ToArray();
                    Require(headings.Contains("NACA") && headings.Contains("Eppler") && headings.Contains("Speer") &&
                        headings.Contains("My sections"), "Catalog family headings are missing");
                    Require(list.Items.OfType<ListBoxItem>().Any(item => item.Tag is CatalogChoice.Catalog { Entry.DisabledReason: not null }),
                        "Disabled entries are missing from the listbox");
                    break;
                case "CatalogDialog_ArrowsReachDisabledRows_Quiet":
                    search.Text = "E817";
                    Dispatcher.UIThread.RunJobs();
                    count?.Invoke();
                    PressKey(search, Key.Down);
                    Require(dialog.ActiveOption == 0 && Need<TextBlock>(dialog, "MatchCount").Text == "1 sections match",
                        $"Arrow did not reach the disabled row quietly: active={dialog.ActiveOption}, count={Need<TextBlock>(dialog, "MatchCount").Text}");
                    break;
                case "CatalogDialog_TypingPause_AnnouncesCountOnce":
                    search.Text = "NACA";
                    Dispatcher.UIThread.RunJobs();
                    var old = count;
                    search.Text = "NACA 0";
                    Dispatcher.UIThread.RunJobs();
                    string? before = Need<TextBlock>(dialog, "MatchCount").Text;
                    old?.Invoke();
                    Require(Need<TextBlock>(dialog, "MatchCount").Text == before, "Stale debounce announced a count");
                    count?.Invoke();
                    Require(Need<TextBlock>(dialog, "MatchCount").Text == $"{dialog.FindControl<ListBox>("SectionList")!.Items.OfType<ListBoxItem>().Count(item => item.Tag is CatalogChoice)} sections match",
                        "Latest count was not announced after the pause");
                    break;
                case "CatalogDialog_NoMatch_Copy115ReplaceDisabled":
                    search.Text = "there-is-no-such-section";
                    Dispatcher.UIThread.RunJobs();
                    Require(detail.Text == "No sections match “there-is-no-such-section”. Try NACA, Eppler or a name." && !replace.IsEnabled,
                        $"COPY-115 or disabled Replace is missing: '{detail.Text}', enabled={replace.IsEnabled}");
                    break;
                case "CatalogDialog_PendingRowEnter_NothingChanges":
                    byte[] beforeBytes = controller.Section!.Draft.Bytes.ToArray();
                    SelectCatalog(list, "E817");
                    PressKey(search, Key.Enter);
                    Require(beforeBytes.AsSpan().SequenceEqual(controller.Section!.Draft.Bytes) && !replace.IsEnabled &&
                        detail.Text?.Contains("Pending admission", StringComparison.Ordinal) == true,
                        "Enter on a pending row changed the section or hid its reason");
                    break;
                case "CatalogDialog_SharedPreview_DetailNamesStationsFitLimit":
                    SelectCatalog(list, "NACA 0012");
                    WaitFor(() => controller.CurrentPreview is not null);
                    Require(detail.Text?.Contains("Root and Tip") == true && detail.Text.Contains("µm") &&
                        detail.Text.Contains("% chord") && detail.Text.Contains("limit 10 µm") &&
                        detail.Text.Contains("points per surface") && detail.Text.Contains("Largest change") &&
                        detail.Text.Contains("t/c stays") && detail.Text.Contains("Frame:"), "Preview detail is incomplete");
                    break;
                case "CatalogDialog_Replace_FocusToSectionMenu":
                    SelectCatalog(list, "NACA 0012");
                    WaitFor(() => controller.CurrentPreview is not null);
                    replace.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    WaitFor(() => controller.Section!.Draft.Cursor == 1 && !dialog.IsVisible);
                    Require(!dialog.IsVisible, "Replace did not close the dialog");
                    break;
                case "CatalogDialog_Escape_NothingChangedFocusToSectionMenu":
                    byte[] original = controller.Section!.Draft.Bytes.ToArray();
                    PressKey(search, Key.Escape);
                    Require(!dialog.IsVisible && original.AsSpan().SequenceEqual(controller.Section!.Draft.Bytes),
                        "Escape changed the section or left the dialog open");
                    break;
                case "CatalogDialog_SpacingRefusal_ReasonAndChainOffered":
                    SelectCatalog(list, "NACA 4412");
                    WaitFor(() => controller.RefusedPreview is not null);
                    Require(!replace.IsEnabled && chain.IsVisible &&
                        detail.Text?.StartsWith("Root blends point-to-point with Tip", StringComparison.Ordinal) == true &&
                        controller.CurrentPreview is null, "Spacing refusal did not show reason and chain");
                    break;
                case "CatalogDialog_ChainButton_AppliesAllStations":
                    SelectCatalog(list, "NACA 4412");
                    WaitFor(() => controller.RefusedPreview is not null);
                    chain.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    WaitFor(() => controller.Section!.Draft.Cursor == 2);
                    Require(controller.Section!.LastReport?.Import?.Stations?.Count == controller.Inspection!.Authored.Assignments.Count,
                        "Chain button did not apply at all stations");
                    break;
                case "CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel":
                    Require(detail.Text == "The catalog didn't load: a catalog file is missing from this installation. Your section hasn't changed. Choose Cancel to go back." &&
                        !replace.IsEnabled && Need<Button>(dialog, "CancelButton").IsEnabled,
                        "Catalog-unavailable state lacks cause or Cancel");
                    break;
                case "CatalogDialog_MySectionsEmpty_ShowsNextAction":
                    Require(list.Items.OfType<ListBoxItem>().Any(item => item.Content?.ToString()?.StartsWith("No saved sections yet.", StringComparison.Ordinal) == true),
                        "My sections empty state lacks its next action");
                    break;
                case "CatalogDialog_DoubleClickRow_Replaces":
                    var row = SelectCatalog(list, "NACA 0012");
                    WaitFor(() => controller.CurrentPreview is not null);
                    row.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
                    WaitFor(() => controller.Section!.Draft.Cursor == 1);
                    break;
                case "CatalogDialog_Preview_LargestChangeMarkerAtMeasuredX":
                    SelectCatalog(list, "NACA 0012");
                    WaitFor(() => controller.CurrentPreview is not null);
                    Require(controller.CurrentPreview!.LargestChangeAtX is >= 0 and <= 1, "Largest-change x is not on chord");
                    break;
            }
        }
        finally { dialog.Close(); }
    }

    private static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? throw new Exception($"{name} missing");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static ListBoxItem SelectCatalog(ListBox list, string designation)
    {
        var item = list.Items.OfType<ListBoxItem>().First(row => row.Tag is CatalogChoice.Catalog choice &&
            choice.Entry.Designation.EndsWith(designation, StringComparison.Ordinal));
        list.SelectedItem = item;
        return item;
    }

    private static void PressKey(Control control, Key key) => control.RaiseEvent(new KeyEventArgs
    {
        RoutedEvent = InputElement.KeyDownEvent, Source = control, Key = key
    });

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("DLG result did not settle");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void Wait(Task task)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("DLG setup did not complete");
            Dispatcher.UIThread.RunJobs();
            Thread.Yield();
        }
        task.GetAwaiter().GetResult();
    }

    private static void CheckDamagedRow()
    {
        string root = TestTemp.Combine("dlg-damaged-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "bad.foil"), "damaged");
            using var controller = new WorkbenchController(sections: new SectionLibrary(root));
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var snapshot = controller.OpenCatalog();
            string reason = "“bad.foil” is damaged and was skipped.";
            Require(snapshot.ProblemRows.Single().Reason == reason, "Controller dropped the scan problem");
            var dialog = new CatalogDialog(controller, snapshot);
            dialog.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                var list = Need<ListBox>(dialog, "SectionList");
                var row = list.Items.OfType<ListBoxItem>().Single(item => item.Tag is CatalogChoice.Damaged);
                list.SelectedItem = row;
                Require(AutomationProperties.GetHelpText(row) == reason &&
                    Need<TextBlock>(dialog, "DetailLine").Text == reason &&
                    !Need<Button>(dialog, "ReplaceButton").IsEnabled,
                    "Damaged row did not show its reason and disable Replace");
            }
            finally { dialog.Close(); }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void CheckUnsupportedSave()
    {
        foreach (var (code, expected) in new[]
        {
            ("DOC-UNSUPPORTED-PERSISTENCE", "This system can't save to My sections safely. Nothing was saved."),
            ("LIB-IO", "Couldn't save to My sections: the file couldn't be written. Nothing was saved."),
            ("DOC-SAVE-UNCERTAIN", "CFD Workbench couldn't confirm whether this section was saved. Check My sections before trying again."),
            ("LIB-UNKNOWN", "Couldn't finish saving this section (LIB-UNKNOWN). Check My sections before trying again.")
        })
        {
            using var controller = new WorkbenchController();
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var dialog = new SaveSectionDialog(controller, _ => Task.FromException(new ContractError(code)));
            dialog.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                Need<TextBox>(dialog, "NameBox").Text = "Kept";
                Wait(dialog.SaveAsync());
                Require(Need<TextBlock>(dialog, "SaveError").Text == expected,
                    code + " did not show its proposed failure copy");
            }
            finally { dialog.Close(); }
        }
    }

    private static void CheckStationSource()
    {
        using var controller = Replaced("NACA 0012");
        var projection = controller.CurrentProjection!;
        var model = PropertiesView.Build(new Selection.Station(0, projection.Assignments[0].Eta), projection,
            controller.Estimates, ShellMode.Workspace,
            new PropertiesContext(controller.Planform, StationSource: controller.StationSource));
        var row = model.Groups.Single(group => group.Id == "stn").Rows.Single(item => item.Key == "s:source");
        Require(row.Value == "Catalog original · NACA 0012 (GEN)",
            "Station card did not show the profile's parsed provenance");
    }

    private static void CheckSave(string name)
    {
        string root = TestTemp.Combine("dlg-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var library = new SectionLibrary(root);
            using var controller = new WorkbenchController(sections: library);
            Wait(controller.OpenExampleAsync());
            Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
            var dialog = new SaveSectionDialog(controller, value => controller.SaveToMySectionsAsync(value));
            dialog.Show();
            Dispatcher.UIThread.RunJobs();
            var input = Need<TextBox>(dialog, "NameBox");
            switch (name)
            {
                case "SaveDialog_Open_FocusInName":
                    Require(ReferenceEquals(dialog.FocusManager?.GetFocusedElement(), input) &&
                        Need<TextBlock>(dialog, "RightsLine").Text?.Contains("Rights:") == true,
                        "Save did not focus Name or show rights");
                    break;
                case "SaveDialog_EmptyOrDuplicate_ErrorFocusStays":
                    input.Text = " ";
                    Wait(dialog.SaveAsync());
                    Require(Need<TextBlock>(dialog, "SaveError").Text == "Name the section to save it." && input.IsFocused,
                        "Empty name did not show COPY-113 in Name");
                    Wait(controller.SaveToMySectionsAsync("Sample"));
                    input.Text = "sample";
                    Wait(dialog.SaveAsync());
                    Require(Need<TextBlock>(dialog, "SaveError").Text == "“sample” is already in My sections. Choose another name." &&
                        input.IsFocused && AutomationProperties.GetItemStatus(input) == "invalid",
                        "Duplicate name did not show COPY-114 with invalid Name focus");
                    break;
                case "SaveDialog_Escape_NothingSavedFocusToSectionMenu":
                    input.Text = "Unsaved";
                    PressKey(input, Key.Escape);
                    Require(!dialog.IsVisible && controller.OpenCatalog().Mine.Count == 0, "Escape wrote a library entry");
                    break;
                case "SaveDialog_Save_LiveRegionFocusToSectionMenu":
                    input.Text = "Kite root";
                    Wait(dialog.SaveAsync());
                    Require(dialog.SavedName == "Kite root" && Need<TextBlock>(dialog, "SavedRegion").Text ==
                        "Saved “Kite root” to My sections" && controller.OpenCatalog().Mine.Any(entry => entry.Name == "Kite root"),
                        "Save did not publish and announce the exact name");
                    break;
            }
            dialog.Close();
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static WorkbenchController Replaced(string designation)
    {
        var controller = new WorkbenchController();
        Wait(controller.OpenExampleAsync());
        Wait(controller.EnterSectionAsync(0, EntryOrigin.Palette));
        var entry = Catalog.Load().Single(row => row.Designation == designation);
        controller.PreviewReplace(new CatalogChoice.Catalog(entry));
        WaitFor(() => controller.CurrentPreview is not null);
        Wait(controller.ApplyReplaceAsync(ReplaceScope.Draft));
        return controller;
    }

    private static void CheckChipAfterEdit()
    {
        using var controller = Replaced("NACA 0012");
        var view = new SectionEditorView();
        var window = new Window { Content = view, Width = 800, Height = 400 };
        window.Show();
        view.Bind(controller);
        Require(view.SourceChipText?.StartsWith("Catalog original · NACA 0012", StringComparison.Ordinal) == true,
            "The source chip did not name the catalog original");
        var point = controller.SectionCurve(SurfaceSide.Upper)!.Points.Single(item => item.Id == "cv-3");
        Wait(controller.ApplySectionStepAsync(new SectionStep.Move(SurfaceSide.Upper, point.Id, point.SpanMeters, point.Ordinate + .01)));
        view.Bind(controller);
        Require(view.SourceChipText?.StartsWith("Modified from NACA 0012", StringComparison.Ordinal) == true,
            "The chip did not report modification in text");
        window.Close();
    }

    private static void CheckScaledChip()
    {
        using var controller = Replaced("NACA 0009");
        var view = new SectionEditorView();
        var window = new Window { Content = view, Width = 800, Height = 400 };
        window.Show();
        view.Bind(controller);
        Require(view.SourceChipText?.Contains("scaled to", StringComparison.Ordinal) == true &&
            view.SourceChipText.Contains("% t/c", StringComparison.Ordinal), "Station t/c scaling is missing from chip text");
        window.Close();
    }

    private static void CheckBrowserText()
    {
        using var controller = Replaced("NACA 0012");
        string profile = controller.Section!.Draft.Profile;
        string text = Panes.BrowserPane.StationSourceText(profile, controller.Section.Draft.Bytes);
        Require(text == profile + " · NACA 0012", "Browser did not show name then source: " + text);
    }

    private static void CheckCanvasPixels()
    {
        using var controller = Replaced("NACA 0012");
        var current = controller.SectionView(0);
        var preview = current with
        {
            UpperCurve = current.UpperCurve.Select(point => new ProfilePoint(point.X, point.Y + .04)).ToArray(),
            LowerCurve = current.LowerCurve.Select(point => new ProfilePoint(point.X, point.Y - .04)).ToArray()
        };
        var canvas = new SectionCanvas
        {
            Controller = controller, Profile = current, Width = 800, Height = 400,
            BackgroundBrush = Brushes.Black, FoilBrush = Brushes.White, StationBrush = Brushes.Gray,
            PreviewLargestX = .47
        };
        var window = new Window { Content = canvas, Width = 800, Height = 400 };
        window.Show();
        using var before = PropertiesCellsTests.Render(window, 1);
        canvas.PreviewProfile = preview;
        canvas.InvalidateVisual();
        using var after = PropertiesCellsTests.Render(window, 1);
        int changed = 0;
        for (int y = 100; y < 300; y += 2)
            for (int x = 100; x < 700; x += 2)
                if (before.At(x, y) != after.At(x, y)) changed++;
        window.Close();
        Require(changed > 20, "No dashed preview pixels over the current section: " + changed);
    }
}
