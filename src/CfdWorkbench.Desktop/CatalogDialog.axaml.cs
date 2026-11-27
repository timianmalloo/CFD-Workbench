using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using System.Globalization;

namespace CfdWorkbench.Desktop;

/// <summary>The Section command sheet. TextBox and ListBox own the combobox's automation shape.</summary>
public partial class CatalogDialog : Window
{
    private readonly WorkbenchController controller;
    private CatalogSnapshot snapshot;
    private readonly Control? returnFocus;
    private readonly Action<TimeSpan, Action> schedule;
    private readonly Func<Task<string?>>? pickDatFile;
    private readonly List<ListBoxItem> options = [];
    private int countTicket;
    private bool replacing;
    private bool importing;
    private (string Hash, string Note)? pendingImportNote;
    // The row whose Enter arrived during its fit; the apply runs when that row's preview lands.
    private ListBoxItem? armedApply;
    private ListBoxItem? activeItem;

    public CatalogDialog(WorkbenchController controller, CatalogSnapshot snapshot, Control? returnFocus = null,
        Action<TimeSpan, Action>? schedule = null, Func<Task<string?>>? pickDatFile = null)
    {
        this.controller = controller;
        this.snapshot = snapshot;
        this.returnFocus = returnFocus;
        this.pickDatFile = pickDatFile;
        this.schedule = schedule ?? ((delay, action) =>
        {
            _ = Task.Delay(delay).ContinueWith(_ => Dispatcher.UIThread.Post(action), TaskScheduler.Default);
        });
        InitializeComponent();
        ImportDatButton.Content = Labels.CatalogImportDatButton;
        AutomationProperties.SetControlTypeOverride(SearchBox, AutomationControlType.ComboBox);
        AutomationProperties.SetControlTypeOverride(SectionList, AutomationControlType.List);
        SearchBox.TextChanged += (_, _) => Filter();
        SearchBox.KeyDown += OnSearchKeyDown;
        SectionList.KeyDown += OnListKeyDown;
        SectionList.SelectionChanged += (_, _) => SelectRow();
        ImportDatButton.Click += async (_, _) => await ImportDatAsync();
        ReplaceButton.Click += async (_, _) => await ApplyAsync(ReplaceScope.Draft);
        ChainButton.Click += async (_, _) => await ApplyChainAsync();
        CancelButton.Click += (_, _) => Close();
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape) return;
            args.Handled = true;
            Close();
        }, RoutingStrategies.Tunnel);
        Opened += (_, _) => { SearchBox.Focus(); FlushMatchCount(); };
        Closed += (_, _) =>
        {
            controller.PreviewChanged -= OnPreviewChanged;
            controller.ClearCatalogPreview();
            returnFocus?.Focus();
        };
        controller.PreviewChanged += OnPreviewChanged;
        Filter();
    }

    public int ActiveOption => SectionList.SelectedItem is ListBoxItem item ? options.IndexOf(item) : -1;

    public void FlushMatchCount() => MatchCount.Text = $"{options.Count} sections match";

    private void Filter()
    {
        ClearActiveStatus();
        options.Clear();
        SectionList.SelectedItem = null;
        SectionList.Items.Clear();
        string query = SearchBox.Text?.Trim() ?? "";
        if (!this.TryFindResource("CompactTargetSize", ActualThemeVariant, out var targetSize) || targetSize is not double rowHeight)
            throw new InvalidOperationException("CompactTargetSize token is missing");
        foreach (var (family, title) in new[]
        {
            (CatalogFamily.Naca, "NACA"), (CatalogFamily.Hydrofoil, Labels.CatalogFamilyHydrofoil),
            (CatalogFamily.Eppler, "Eppler"), (CatalogFamily.Speer, "Speer"),
            (CatalogFamily.MySections, "My sections")
        })
        {
            var rows = family == CatalogFamily.MySections
                ? snapshot.Mine.Where(entry => entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(entry => (Name: entry.Name, Choice: (CatalogChoice)new CatalogChoice.Mine(entry), Reason: (string?)null, Tag: "saved")).ToArray()
                    .Concat(snapshot.ProblemRows.Where(problem => problem.File.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Select(problem => (Name: problem.File, Choice: (CatalogChoice)new CatalogChoice.Damaged(problem.File, problem.Reason),
                            Reason: (string?)problem.Reason, Tag: "damaged"))).ToArray()
                : snapshot.Entries.Where(entry => entry.Family == family && entry.Designation.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(entry => (Name: entry.Designation, Choice: (CatalogChoice)new CatalogChoice.Catalog(entry), Reason: entry.DisabledReason,
                        Tag: RowTag(entry))).ToArray();
            if (rows.Length == 0 && family != CatalogFamily.MySections) continue;
            SectionList.Items.Add(new ListBoxItem { Content = title, IsEnabled = false, FontWeight = Avalonia.Media.FontWeight.SemiBold });
            foreach (var row in rows)
            {
                var text = new TextBlock { Text = row.Name + " · " + row.Tag, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
                var item = new ListBoxItem { Content = text, Tag = row.Choice, MinHeight = rowHeight };
                AutomationProperties.SetControlTypeOverride(item, AutomationControlType.ListItem);
                AutomationProperties.SetName(item, row.Name);
                if (row.Reason is not null)
                {
                    item.Classes.Add("disabled");
                    AutomationProperties.SetItemStatus(item, "disabled");
                    AutomationProperties.SetHelpText(item, row.Reason);
                }
                item.DoubleTapped += async (_, _) =>
                {
                    SectionList.SelectedItem = item;
                    if (row.Reason is null) await ApplyAsync(ReplaceScope.Draft);
                };
                options.Add(item);
                SectionList.Items.Add(item);
            }
            if (family == CatalogFamily.MySections && rows.Length == 0 && query.Length == 0)
                SectionList.Items.Add(new ListBoxItem
                {
                    Content = "No saved sections yet. To keep one here, choose Save to My sections… from the Section menu in the section editor.",
                    IsEnabled = false
                });
        }
        int ticket = ++countTicket;
        schedule(TimeSpan.FromMilliseconds(300), () => { if (ticket == countTicket && IsVisible) FlushMatchCount(); });
        if (options.Count == 0)
        {
            controller.ClearCatalogPreview();
            DetailLine.Text = snapshot.Outcome == "CAT-UNAVAILABLE"
                ? "The catalog didn't load: " + (snapshot.FailureCause ?? "a catalog file failed its check") +
                  ". Your section hasn't changed. Choose Cancel to go back."
                : $"No sections match “{query}”. Try NACA, Eppler or a name.";
            ReplaceButton.IsEnabled = false;
            ChainButton.IsVisible = false;
            ImportDatButton.IsVisible = false;
        }
        else SectionList.SelectedItem = options[0];
    }

    // A row with a reason names its state; a choosable row names its admission class.
    private static string RowTag(CatalogEntry entry) => entry.DisabledReason is null
        ? entry.Class.ToString().ToUpperInvariant()
        : entry.Class switch
        {
            AdmissionClass.Gen => Labels.CatalogTagNotInBuild,
            AdmissionClass.Vend => Labels.CatalogTagPending,
            AdmissionClass.Link => Labels.CatalogTagCiteOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Class, "Unknown admission class")
        };

    private void OnSearchKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key is Key.Down or Key.Up)
        {
            Move(args.Key == Key.Down ? 1 : -1);
            args.Handled = true;
        }
        else if (args.Key is Key.Enter or Key.Return)
        {
            args.Handled = true;
            _ = ApplyAsync(ReplaceScope.Draft);
        }
    }

    private void OnListKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key is Key.Enter or Key.Return)
        {
            args.Handled = true;
            _ = ApplyAsync(ReplaceScope.Draft);
        }
    }

    private void Move(int delta)
    {
        if (options.Count == 0) return;
        int next = Math.Clamp(ActiveOption + delta, 0, options.Count - 1);
        SectionList.SelectedItem = options[next];
        SectionList.ScrollIntoView(options[next]);
    }

    private void ClearActiveStatus()
    {
        if (activeItem is { } prior && !prior.Classes.Contains("disabled"))
            prior.ClearValue(AutomationProperties.ItemStatusProperty);
        activeItem = null;
    }

    private void SelectRow()
    {
        ClearActiveStatus();
        if (SectionList.SelectedItem is not ListBoxItem { Tag: CatalogChoice choice } item || !options.Contains(item)) return;
        if (pendingImportNote is { } pending && (choice is not CatalogChoice.Mine mineChoice || mineChoice.Entry.Hash != pending.Hash))
            pendingImportNote = null;
        if (!ReferenceEquals(armedApply, item)) DropArmedApply();
        controller.RecordCatalogSelect(choice);
        string? reason = choice switch
        {
            CatalogChoice.Catalog { Entry.DisabledReason: { } disabled } => disabled,
            CatalogChoice.Damaged damaged => damaged.Reason,
            _ => null
        };
        if (reason is not null)
        {
            controller.ClearCatalogPreview();
            DetailLine.Text = reason;
            ReplaceButton.IsEnabled = false;
            ChainButton.IsVisible = false;
            ImportDatButton.IsVisible = choice is CatalogChoice.Catalog { Entry.Class: AdmissionClass.Vend or AdmissionClass.Link };
            return;
        }
        ReplaceButton.IsEnabled = false;
        ChainButton.IsVisible = false;
        ImportDatButton.IsVisible = false;
        DetailLine.Text = "Fitting…";
        controller.PreviewReplace(choice);
        AutomationProperties.SetItemStatus(item, "active");
        activeItem = item;
    }

    private void OnPreviewChanged()
    {
        if (SectionList.SelectedItem is not ListBoxItem { Tag: CatalogChoice choice } item) return;
        if (controller.CurrentPreview is { } preview && Equals(controller.PreviewChoice, choice))
        {
            string detail = Detail(preview, choice);
            if (pendingImportNote is { } note && choice is CatalogChoice.Mine mine && mine.Entry.Hash == note.Hash)
            {
                pendingImportNote = null;
                detail = note.Note + " " + detail;
            }
            DetailLine.Text = detail;
            ReplaceButton.IsEnabled = true;
            ChainButton.IsVisible = false;
            UseSourceThickness.IsVisible = ShouldOfferThickness(preview);
            if (ReferenceEquals(armedApply, item))
            {
                armedApply = null;
                _ = ApplyAsync(ReplaceScope.Draft);
            }
        }
        else if (controller.RefusedPreview is { } refused)
        {
            pendingImportNote = null;
            DetailLine.Text = refused.RefusalReason;
            ReplaceButton.IsEnabled = false;
            ChainButton.IsVisible = refused.RefusalCode == "CAT-SPACING" && refused.BlendChain is { Count: > 0 };
            if (ChainButton.IsVisible) ChainButton.Content = ChainText(refused.BlendChain!);
            DropArmedApply();
        }
        else if (controller.PreviewFault is { } error)
        {
            pendingImportNote = null;
            DetailLine.Text = error.Reason is { Length: > 0 } reason ? reason : Labels.CatalogPreviewFault;
            ReplaceButton.IsEnabled = false;
            DropArmedApply();
        }
    }

    private void DropArmedApply()
    {
        if (armedApply is null) return;
        armedApply = null;
        controller.RecordCatalogApplyDropped();
    }

    private string Detail(ReplacePreview preview, CatalogChoice choice)
    {
        string source = choice is CatalogChoice.Catalog catalog ? catalog.Entry.Designation : ((CatalogChoice.Mine)choice).Entry.Name;
        var mode = controller.Section!;
        var assignments = controller.Inspection!.Authored.Assignments;
        string stations = JoinNames(preview.Stations.Select(index =>
            ElevationView.StationName(index, assignments[index].Eta)).ToArray());
        int previous = Sections.View(mode.Draft.Bytes, mode.Draft.Assignment, SurfaceSide.Upper, "display", 0).Points.Count;
        double microns = preview.FitResidual * preview.AcceptanceChord * 1e6;
        double change = preview.LargestChangeChord * preview.AcceptanceChord * 1e3;
        var report = preview.Report;
        string text = string.Create(CultureInfo.InvariantCulture,
            $"Replaces {stations} (they share this section). {source} fits on its own {preview.PointsPerSurface} points: {microns:F2} µm ({preview.FitResidual * 100:F4} % chord; limit 10 µm at {preview.AcceptanceChord * 1e3:F2} mm). {previous} → {preview.PointsPerSurface} points per surface; point types reset. Largest change {change:F2} mm at {preview.LargestChangeAtX * 100:F2} % chord.");
        if (report?.SourceThickness is double sourceThickness)
        {
            double station = Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment).StationThicknessRatio;
            text += string.Create(CultureInfo.InvariantCulture,
                $" t/c stays {station * 100:F2} % from the Thickness curve; {source} is {sourceThickness * 100:F2} % thick.");
        }
        if (report?.FrameLeShift is double shift && report.FrameRotationDegrees is double angle)
            text += string.Create(CultureInfo.InvariantCulture, $" Frame: leading edge moved {shift * 1e3:F2} mm, chord turned {angle:F2}°.");
        return text;
    }

    private bool ShouldOfferThickness(ReplacePreview preview) =>
        preview.Report?.SourceThickness is double source && controller.Section is { } mode &&
        Math.Abs(source - Sections.Facts(mode.Draft.Bytes, mode.Draft.Assignment).StationThicknessRatio) > 1e-4;

    private string ChainText(IReadOnlyList<int> chain)
    {
        var assignments = controller.Inspection!.Authored.Assignments;
        var names = chain.Select(index => ElevationView.StationName(index, assignments[index].Eta)).ToArray();
        return names.Length == 2
            ? $"Replace {names[0]} and {names[1]} ({names[1]} changes too)"
            : $"Replace at every station ({JoinNames(names)} change too)";
    }

    private static string JoinNames(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "",
        1 => names[0],
        2 => names[0] + " and " + names[1],
        _ => string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[^1]
    };

    private async Task ImportDatAsync()
    {
        if (importing || replacing) return;
        string? suggestedName = SectionList.SelectedItem is ListBoxItem { Tag: CatalogChoice.Catalog selected }
            ? selected.Entry.Designation
            : null;
        importing = true;
        ImportDatButton.IsEnabled = false;
        try
        {
            string? path = null;
            if (pickDatFile is not null)
            {
                path = await pickDatFile();
            }
            else if (TopLevel.GetTopLevel(this) is { } top)
            {
                var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Labels.CatalogImportDatButton,
                    AllowMultiple = false
                });
                if (files.Count > 0)
                {
                    using var file = files[0];
                    path = file.Path.LocalPath;
                }
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                DetailLine.Text = Labels.CatalogImportCancelled;
                return;
            }
            var entry = await controller.ImportDatToMySectionsAsync(path, suggestedName);
            snapshot = controller.OpenCatalog();
            string query = SearchBox.Text?.Trim() ?? "";
            if (query.Length > 0 && !entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                SearchBox.Text = "";
            else
                Filter();
            var importedItem = options.FirstOrDefault(item => item.Tag is CatalogChoice.Mine mine && mine.Entry.Hash == entry.Hash);
            if (importedItem is not null)
            {
                pendingImportNote = (entry.Hash, Labels.CatalogImportSuccess(entry.Name));
                SectionList.SelectedItem = importedItem;
                SectionList.ScrollIntoView(importedItem);
            }
            else
            {
                pendingImportNote = null;
                DetailLine.Text = Labels.CatalogImportSuccess(entry.Name);
            }
        }
        catch (ContractError error)
        {
            pendingImportNote = null;
            DetailLine.Text = ImportErrorCopy(error);
        }
        finally
        {
            importing = false;
            ImportDatButton.IsEnabled = true;
        }
    }

    private static string ImportErrorCopy(ContractError error) => error.Code switch
    {
        "DSL-IMPORT" => Labels.CatalogImportInvalidDat,
        "DOC-UNSUPPORTED-PERSISTENCE" or "DOC-UNSUPPORTED-PLATFORM" =>
            Labels.SectionSaveUnavailableFor(CfdWorkbench.Persistence.ProjectStore.PlatformAdmitted()),
        "LIB-SECTION-INVALID" => "Fix the crossing before saving this section.",
        "LIB-CLAIM-HELD" => "Another save is in progress. Try again in a moment.",
        "LIB-IO" => error.Reason is { Length: > 0 } ioReason ? ioReason : Labels.CatalogImportSaveError,
        _ => error.Reason is { Length: > 0 } reason ? reason : Labels.CatalogImportSaveError
    };

    private async Task ApplyChainAsync()
    {
        if (SectionList.SelectedItem is not ListBoxItem { Tag: CatalogChoice choice } || replacing) return;
        var landed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed()
        {
            if (controller.PreviewChoice is not null || controller.PreviewFault is not null)
                landed.TrySetResult();
        }
        controller.PreviewChanged += Changed;
        try
        {
            controller.PreviewReplace(choice, ReplaceScope.BlendChain);
            await landed.Task;
            if (controller.CurrentPreview is not null) await ApplyAsync(ReplaceScope.BlendChain);
        }
        finally { controller.PreviewChanged -= Changed; }
    }

    private async Task ApplyAsync(ReplaceScope scope)
    {
        if (replacing || SectionList.SelectedItem is not ListBoxItem { Tag: CatalogChoice choice } item ||
            choice is CatalogChoice.Catalog { Entry.DisabledReason: not null } or CatalogChoice.Damaged)
            return;
        if (controller.CurrentPreview is null)
        {
            // Enter during the fit is kept, not dropped: it applies when this row's preview lands. Replace stays disabled.
            if (scope == ReplaceScope.Draft && controller.PreviewPending)
            {
                armedApply = item;
                DetailLine.Text = Labels.CatalogFittingThenReplace;
            }
            return;
        }
        replacing = true;
        ReplaceButton.IsEnabled = false;
        try
        {
            await controller.ApplyCatalogAsync(scope);
            if (UseSourceThickness.IsChecked == true)
                await controller.ApplySectionStepAsync(new SectionStep.Thickness(ThicknessIntent.UseSource));
            Close();
        }
        catch (ContractError error)
        {
            DetailLine.Text = error.Reason is { Length: > 0 } reason
                ? reason
                : "Couldn't apply this section right now. Nothing changed.";
            ReplaceButton.IsEnabled = false;
        }
        finally { replacing = false; }
    }
}
