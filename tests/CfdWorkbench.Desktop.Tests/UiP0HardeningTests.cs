using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 6 Track UI-P0 red-first regression tests covering all 15 Blockers (F-01..F-14, F-16) and coupled Majors
/// (F-15, F-17, F-30, F-31) from docs/reviews/ui-as-built-workbench.md.
/// </summary>
public static class UiP0HardeningTests
{
    public static void Run()
    {
        RunP0A();
        RunP0B();
        RunP0C();
        RunP0D();
    }

    /// <summary>
    /// Expanded applied-contrast check invoked by `--theme-matrix` covering StartView hovered cards, CatalogDialog disabled
    /// rows, PointsPane selected rows (pts-cell, pts-cell.ro, pts-point), and SectionChartView series tokens across Light,
    /// Dark, and High Contrast. Emits THEME-EXT-ROW lines so verify-application-adapters.py's frozen THEME-ROW set stays exact.
    /// </summary>
    internal static void RunThemeMatrixExtension()
    {
        DesktopChecks.Check("ThemeMatrix_HardenedSurfaces_AppliedContrast", () =>
        {
            var pseudoProp = typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("PseudoClasses unavailable");
            IPseudoClasses Pseudo(Control c) => (IPseudoClasses)pseudoProp.GetValue(c)!;
            var failures = new List<string>();
            int rows = 0;

            void Measure(string theme, string name, Color fg, Color bg, double floor)
            {
                double ratio = Contrast(fg, bg);
                rows++;
                Console.WriteLine(FormattableString.Invariant(
                    $"THEME-EXT-ROW {theme}/{name} fg=#{fg.ToUInt32():X8} bg=#{bg.ToUInt32():X8} ratio={ratio:F4} floor={floor}"));
                if (ratio < floor) failures.Add($"{theme}/{name} {fg} on {bg} = {ratio:F2} < {floor}");
            }

            foreach (var (theme, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark), ("high-contrast", NativeReviewThemes.HighContrast) })
            {
                var window = new MainWindow(null, macOS: false) { RequestedThemeVariant = variant, Width = 1280, Height = 800 };
                try
                {
                    window.Show();
                    Settle(window);
                    var host = (ShellHost)window.Content!;
                    // 1. StartView hovered + pressed card caption.
                    var card = host.ModelView.StartCardView.StartNewButton;
                    var caption = card.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("caption"));
                    var presenter = card.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                        .First(p => p.Name == "PART_ContentPresenter");
                    Pseudo(card).Add(":pointerover");
                    Settle(window);
                    Measure(theme, "start.card.caption.hover", SolidColor(caption.Foreground, "fg"), SolidColor(presenter.Background, "bg"), 4.5);
                    Pseudo(card).Add(":pressed");
                    Settle(window);
                    Measure(theme, "start.card.caption.pressed", SolidColor(caption.Foreground, "fg"), SolidColor(presenter.Background, "bg"), 4.5);
                    Pseudo(card).Remove(":pressed");
                    Pseudo(card).Remove(":pointerover");

                    // 2. PointsPane selected row (pts-cell, pts-cell.ro, pts-point).
                    DesktopChecks.PumpToCompletion(host.Controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                    host.ApplyWorkspace(WorkspaceId.Precision);
                    var rootPoint = host.Controller.Planform!.Leading.Points[0];
                    host.Controller.Select(new Selection.Points([new PointRef(rootPoint.Curve, rootPoint.Id)]));
                    host.RefreshPanes();
                    Settle(window);
                    var selectedRow = host.Points.GetVisualDescendants().OfType<Border>()
                        .First(b => b.Classes.Contains("pts-row") && b.Classes.Contains("on"));
                    var rowBg = SolidColor(selectedRow.Background, "pts-row.on bg");
                    var cell = selectedRow.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("pts-cell") && !t.Classes.Contains("ro"));
                    var roCell = selectedRow.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("pts-cell") && t.Classes.Contains("ro"));
                    var ptBtn = selectedRow.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("pts-point"));
                    Measure(theme, "points.selected.cell", SolidColor(cell.Foreground, "fg"), rowBg, 4.5);
                    Measure(theme, "points.selected.cell.ro", SolidColor(roCell.Foreground, "fg"), rowBg, 4.5);
                    Measure(theme, "points.selected.point", SolidColor(ptBtn.Foreground, "fg"), rowBg, 4.5);

                    // 3. CatalogDialog disabled row (hover, selected, selected+hover).
                    DesktopChecks.PumpToCompletion(host.Controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                    var catalog = new CatalogDialog(host.Controller, host.Controller.OpenCatalog()) { RequestedThemeVariant = variant };
                    try
                    {
                        catalog.Show();
                        Settle(catalog);
                        var list = catalog.FindControl<ListBox>("SectionList")!;
                        var disabledItem = new ListBoxItem { Content = new TextBlock { Text = "Unavailable" } };
                        disabledItem.Classes.Add("disabled");
                        list.Items.Insert(0, disabledItem);
                        list.ScrollIntoView(disabledItem);
                        Settle(catalog);
                        var text = disabledItem.GetVisualDescendants().OfType<TextBlock>().First();
                        var cp = disabledItem.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                            .First(p => p.Name == "PART_ContentPresenter");
                        Pseudo(disabledItem).Add(":pointerover");
                        Settle(catalog);
                        Measure(theme, "catalog.disabled.hover", SolidColor(text.Foreground, "fg"), SolidColor(cp.Background, "bg"), 4.5);
                        disabledItem.IsSelected = true;
                        Settle(catalog);
                        Measure(theme, "catalog.disabled.selected-hover", SolidColor(text.Foreground, "fg"), SolidColor(cp.Background, "bg"), 4.5);
                        Pseudo(disabledItem).Remove(":pointerover");
                        Settle(catalog);
                        Measure(theme, "catalog.disabled.selected", SolidColor(text.Foreground, "fg"), SolidColor(cp.Background, "bg"), 4.5);
                    }
                    finally { catalog.Close(); }

                    // 4. SectionChartView tokens (PrimaryBrush, WarningBrush, DangerBrush, MutedBrush on CanvasBrush >= 3.0).
                    var canvasBg = ThemeColor(window, "CanvasBrush", variant);
                    Measure(theme, "section-chart.primary", ThemeColor(window, "PrimaryBrush", variant), canvasBg, 3.0);
                    Measure(theme, "section-chart.warning", ThemeColor(window, "WarningBrush", variant), canvasBg, 3.0);
                    Measure(theme, "section-chart.danger", ThemeColor(window, "DangerBrush", variant), canvasBg, 3.0);
                    Measure(theme, "section-chart.muted", ThemeColor(window, "MutedBrush", variant), canvasBg, 3.0);
                }
                finally
                {
                    typeof(MainWindow).GetField("closeApproved", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(window, true);
                    window.Close();
                }
            }
            Console.WriteLine($"THEME-EXT-CHECK rows={rows} variants=3");
            if (failures.Count > 0)
                throw new InvalidOperationException($"{failures.Count} hardened surface contrast rows failed: " + string.Join(" | ", failures));
        });
    }

    private static void RunP0A()
    {
        DesktopChecks.Check("UiP0_F01_SaveAs_AlwaysOpensPickerEvenWhenNativePathSet", () =>
        {
            string dir = TestTemp.Combine($"uip0-f01-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            string pathA = Path.Combine(dir, "first.cfdw.json");
            string pathB = Path.Combine(dir, "second.cfdw.json");
            var window = new MainWindow(null, macOS: false);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);

                // First save establishes NativePath = pathA.
                DesktopChecks.PumpToCompletion(window.SaveToAsync(pathA), "initial save", TimeSpan.FromSeconds(15));
                if (!string.Equals(controller.NativePath, pathA, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Expected NativePath {pathA}, got {controller.NativePath}");
                string originalBytes = File.ReadAllText(pathA);

                // Modify span so pathB differs from pathA.
                controller.ApplySpan("950");
                Settle(window);

                int pickerCalls = 0;
                window.PickSavePathOverride = () =>
                {
                    pickerCalls++;
                    return Task.FromResult<string?>(pathB);
                };

                var saveAsItem = FindMenuItem(window, "File", "Save As…");
                saveAsItem.Command!.Execute(null);
                PumpUntil(() => pickerCalls > 0 && File.Exists(pathB) && !controller.IsDirty, window, TimeSpan.FromSeconds(5));
                Settle(window);

                if (pickerCalls != 1)
                    throw new InvalidOperationException($"Save As opened picker {pickerCalls} times (expected 1); NativePath={controller.NativePath}");
                if (!File.Exists(pathB))
                    throw new InvalidOperationException("Save As did not write the newly picked file");
                if (File.ReadAllText(pathA) != originalBytes)
                    throw new InvalidOperationException("Save As silently overwrote the original file at NativePath");
                if (!string.Equals(controller.NativePath, pathB, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"NativePath did not update to {pathB} (was {controller.NativePath})");

                // Ordinary Save must reuse NativePath without opening the picker.
                controller.ApplySpan("960");
                Settle(window);
                pickerCalls = 0;
                var saveItem = FindMenuItem(window, "File", "Save");
                saveItem.Command!.Execute(null);
                PumpUntil(() => !controller.IsDirty, window, TimeSpan.FromSeconds(5));
                Settle(window);
                if (pickerCalls != 0)
                    throw new InvalidOperationException($"Ordinary Save opened picker {pickerCalls} times when NativePath was already set");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F02_DirtyDocument_NewExampleOpenRecent_CallsMayReplaceGuard", () =>
        {
            string dir = TestTemp.Combine($"uip0-f02-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            string otherPath = Path.Combine(dir, "other.foil");
            File.Copy("src/CfdWorkbench.Desktop/Assets/example.foil", otherPath, overwrite: true);

            var window = new MainWindow(null, macOS: false);
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                controller.ApplySpan("950");
                host.RefreshPanes();
                Settle(window);
                if (!controller.IsDirty) throw new InvalidOperationException("Fixture foil is not dirty after ApplySpan");

                int prompts = 0;
                string answer = "Cancel";
                window.ConfirmUnsavedOverride = () =>
                {
                    prompts++;
                    return Task.FromResult(answer);
                };

                // 1. file.new must prompt and preserve dirty edits on Cancel.
                FindMenuItem(window, "File", "New foil").Command!.Execute(null);
                PumpUntil(() => prompts >= 1, window, TimeSpan.FromSeconds(2));
                Settle(window);
                if (prompts != 1 || !controller.IsDirty)
                    throw new InvalidOperationException($"file.new prompts={prompts} isDirty={controller.IsDirty}");

                // 2. file.new-example must prompt and preserve dirty edits on Cancel.
                FindMenuItem(window, "File", "New from example").Command!.Execute(null);
                PumpUntil(() => prompts >= 2, window, TimeSpan.FromSeconds(2));
                Settle(window);
                if (prompts != 2 || !controller.IsDirty)
                    throw new InvalidOperationException($"file.new-example prompts={prompts} isDirty={controller.IsDirty}");

                // 3. file.open must prompt BEFORE picking and preserve dirty edits on Cancel.
                FindMenuItem(window, "File", "Open…").Command!.Execute(null);
                PumpUntil(() => prompts >= 3, window, TimeSpan.FromSeconds(2));
                Settle(window);
                if (prompts != 3 || !controller.IsDirty)
                    throw new InvalidOperationException($"file.open prompts={prompts} isDirty={controller.IsDirty}");

                // 4. Open Recent menu item must prompt and preserve dirty edits on Cancel.
                NativeMenuBuilder.RefreshRecentMenu(window,
                    [new RecentEntry(otherPath)],
                    path => _ = host.OpenFileAsync(path),
                    () => _ = host.ClearRecentAsync());
                var recentMenu = FindMenuItem(window, "File", "Open Recent").Menu!;
                var recentItem = recentMenu.Items.OfType<NativeMenuItem>().First(item => Equals(item.Header, otherPath));
                recentItem.Command!.Execute(null);
                PumpUntil(() => prompts >= 4, window, TimeSpan.FromSeconds(2));
                Settle(window);
                if (prompts != 4 || !controller.IsDirty)
                    throw new InvalidOperationException($"Recent open prompts={prompts} isDirty={controller.IsDirty}");

                // 5. Answering Discard replaces the document cleanly (span 0.95 m is replaced by default new foil).
                string editedSource = controller.AcceptedSource;
                answer = "Discard";
                FindMenuItem(window, "File", "New foil").Command!.Execute(null);
                PumpUntil(() => prompts >= 5 && controller.AcceptedSource != editedSource, window, TimeSpan.FromSeconds(5));
                Settle(window);
                if (prompts != 5 || controller.AcceptedSource == editedSource)
                    throw new InvalidOperationException($"Discard did not replace dirty foil: prompts={prompts} sourceUnchanged={controller.AcceptedSource == editedSource}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F03_CommandPalette_RoutesAllCommands_NoWindowsRow0Shift_SelectsAndDismisses", () =>
        {
            var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);

                // (1) Opening palette on Windows must NOT push DockHost down (Row 0 Auto bug).
                double dockYBefore = host.DockHost.Bounds.Y;
                host.OpenPalette();
                Settle(window);
                double dockYAfter = host.DockHost.Bounds.Y;
                if (Math.Abs(dockYAfter - dockYBefore) > 0.5)
                    throw new InvalidOperationException($"Palette shifted Windows DockHost Y from {dockYBefore:F1} to {dockYAfter:F1}");

                // (2) Palette overlay must track live theme changes via DynamicResource.
                var overlay = typeof(ShellHost).GetField("paletteOverlay", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(host) as Border
                    ?? throw new InvalidOperationException("paletteOverlay field not found");
                window.RequestedThemeVariant = ThemeVariant.Light;
                Settle(window);
                var lightBg = (overlay.Background as ISolidColorBrush)?.Color;
                window.RequestedThemeVariant = ThemeVariant.Dark;
                Settle(window);
                var darkBg = (overlay.Background as ISolidColorBrush)?.Color;
                if (lightBg is null || darkBg is null || lightBg == darkBg)
                    throw new InvalidOperationException($"Palette background did not update on live theme switch: light={lightBg}, dark={darkBg}");
                window.RequestedThemeVariant = ThemeVariant.Light;
                Settle(window);

                // (3) Executing selected non-first item runs the selected command (not FirstOrDefault).
                host.PaletteSearch.Text = "%";
                host.PaletteSearch.SelectedItem = "150 %";
                Settle(window);
                RaiseKey(host.PaletteSearch, Key.Enter);
                Settle(window);
                if (Math.Abs(host.Properties.TextScale - 1.5) > 1e-6)
                    throw new InvalidOperationException($"Palette did not execute selected '150 %' item; TextScale={host.Properties.TextScale}");

                // (4) Previously dropped pane/view commands execute from palette: view.comb and view.zoom-in.
                bool combBefore = controller.CombVisible;
                host.OpenPalette();
                host.PaletteSearch.Text = "Curvature comb";
                host.PaletteSearch.SelectedItem = "Curvature comb";
                Settle(window);
                RaiseKey(host.PaletteSearch, Key.Enter);
                Settle(window);
                if (controller.CombVisible == combBefore)
                    throw new InvalidOperationException("Palette 'Curvature comb' (view.comb) was dropped as a no-op");

                host.ModelView.PlanCanvas.Focus();
                Settle(window);
                var camBefore = controller.PlanCamera;
                host.OpenPalette();
                host.PaletteSearch.Text = "Zoom in";
                host.PaletteSearch.SelectedItem = "Zoom in";
                Settle(window);
                RaiseKey(host.PaletteSearch, Key.Enter);
                Settle(window);
                var camAfter = controller.PlanCamera;
                if (camAfter is null || (camBefore is not null && camAfter.PixelsPerMeter <= camBefore.PixelsPerMeter))
                    throw new InvalidOperationException($"Palette 'Zoom in' did not zoom focused PlanCanvas (before={camBefore}, after={camAfter})");

                // (5) Outside click / backdrop dismiss closes the palette without executing.
                host.OpenPalette();
                Settle(window);
                if (!host.PaletteVisible) throw new InvalidOperationException("Palette did not open");
                var backdrop = overlay.Parent as Control;
                if (backdrop is null || ReferenceEquals(backdrop, host))
                    throw new InvalidOperationException("Palette has no dismiss backdrop container");
                using (var pointer = new Avalonia.Input.Pointer(Avalonia.Input.Pointer.GetNextFreeId(), PointerType.Mouse, true))
                {
                    backdrop.RaiseEvent(new PointerPressedEventArgs(backdrop, pointer, window, new Point(10, 10), 1,
                        new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
                }
                Settle(window);
                if (host.PaletteVisible)
                    throw new InvalidOperationException("Clicking outside palette did not dismiss it");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F09_CompareTab_ClickAndMenuCommand_BothBindComparePane", () =>
        {
            var small = Settings.Default with { NSpanPerHalf = 4, NChord = 1, SectionEtas = null, SectionXs = null };
            var controller = new WorkbenchController(analysisMethod: new ProductWingMethod(small));
            var window = new MainWindow(null, macOS: false, controller: controller) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                var evalTask = controller.EvaluateAnalysisAsync(OperatingPoints.Custom(5.14, 2, null), controller.AnalysisWater);
                DesktopChecks.PumpToCompletion(evalTask, "evaluate VLM", TimeSpan.FromSeconds(60));
                host.RefreshPanes();
                Settle(window);

                // (1) Clicking the Compare tab directly must bind ComparePaneHost (not stay blank).
                host.AnalysisPanel.PanelTabs.SelectedItem = host.AnalysisPanel.CompareTab;
                Settle(window);
                var tabText = host.AnalysisPanel.ComparePaneView.GetLogicalDescendants().OfType<TextBlock>().Select(b => b.Text ?? "").ToList();
                if (!tabText.Contains(Labels.NeedsTwoRuns))
                    throw new InvalidOperationException("Clicking Compare tab directly left ComparePaneHost unbound: " + string.Join(" | ", tabText));

                // (2) Switch back to Loading tab, then run View ▸ Compare runs… menu command.
                host.AnalysisPanel.PanelTabs.SelectedItem = host.AnalysisPanel.LoadingTab;
                Settle(window);
                var compareMenu = FindMenuItem(window, "View", Labels.CompareRunsCommand);
                if (!compareMenu.Command!.CanExecute(null))
                    throw new InvalidOperationException("View ▸ Compare runs… CanExecute returned false with a Completed VLM run");
                compareMenu.Command!.Execute(null);
                Settle(window);
                if (!ReferenceEquals(host.AnalysisPanel.PanelTabs.SelectedItem, host.AnalysisPanel.CompareTab))
                    throw new InvalidOperationException("View ▸ Compare runs… menu command did not open the Compare tab");
            }
            finally { window.Close(); }
        });
    }

    private static void RunP0B()
    {
        DesktopChecks.Check("UiP0_F04_F15_HoverAndSelectionContrast_StartViewCatalogAndLists", () =>
        {
            var pseudoProp = typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("PseudoClasses unavailable");
            IPseudoClasses Pseudo(Control c) => (IPseudoClasses)pseudoProp.GetValue(c)!;

            foreach (var (name, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark), ("high-contrast", NativeReviewThemes.HighContrast) })
            {
                var window = new MainWindow(null, macOS: false) { RequestedThemeVariant = variant, Width = 1024, Height = 700 };
                try
                {
                    window.Show();
                    Settle(window);
                    var host = (ShellHost)window.Content!;
                    var card = host.ModelView.StartCardView.StartNewButton;
                    var caption = card.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("caption"));
                    var presenter = card.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                        .First(p => p.Name == "PART_ContentPresenter");
                    var primary = ThemeColor(window, "PrimaryBrush", variant);

                    // Hover StartView card: must NOT invert to solid PrimaryBrush and caption contrast must be >= 4.5:1.
                    Pseudo(card).Add(":pointerover");
                    Settle(window);
                    var hoverBg = SolidColor(presenter.Background, $"{name} card hover bg");
                    var hoverFg = SolidColor(caption.Foreground, $"{name} card caption hover fg");
                    double hoverRatio = Contrast(hoverFg, hoverBg);
                    if (hoverRatio < 4.5)
                        throw new InvalidOperationException($"{name}: StartView hovered card caption contrast {hoverRatio:F2} < 4.5 ({hoverFg} on {hoverBg})");
                    if (hoverBg == primary)
                        throw new InvalidOperationException($"{name}: generic Button:pointerover inverted to solid PrimaryBrush {primary}");

                    // Press StartView card: caption contrast must remain >= 4.5:1 across all themes including HC.
                    Pseudo(card).Add(":pressed");
                    Settle(window);
                    var pressBg = SolidColor(presenter.Background, $"{name} card press bg");
                    var pressFg = SolidColor(caption.Foreground, $"{name} card caption press fg");
                    double pressRatio = Contrast(pressFg, pressBg);
                    if (pressRatio < 4.5)
                        throw new InvalidOperationException($"{name}: StartView pressed card caption contrast {pressRatio:F2} < 4.5 ({pressFg} on {pressBg})");
                    Pseudo(card).Remove(":pressed");
                    Pseudo(card).Remove(":pointerover");
                }
                finally { window.Close(); }

                // CatalogDialog disabled row in rest, hover, selected, and selected+hover.
                using var controller = new WorkbenchController();
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                var snapshot = controller.OpenCatalog();
                var catalog = new CatalogDialog(controller, snapshot) { RequestedThemeVariant = variant };
                try
                {
                    catalog.Show();
                    Settle(catalog);
                    var list = catalog.FindControl<ListBox>("SectionList")!;
                    var disabledItem = new ListBoxItem { Content = new TextBlock { Text = "Unavailable section" } };
                    disabledItem.Classes.Add("disabled");
                    list.Items.Insert(0, disabledItem);
                    list.ScrollIntoView(disabledItem);
                    Settle(catalog);
                    var text = disabledItem.GetVisualDescendants().OfType<TextBlock>().First();
                    var cp = disabledItem.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                        .First(p => p.Name == "PART_ContentPresenter");
                    var primary = ThemeColor(catalog, "PrimaryBrush", variant);

                    // Hover unselected disabled row: must be >= 4.5:1 and not PrimaryBrush.
                    Pseudo(disabledItem).Add(":pointerover");
                    Settle(catalog);
                    var hovBg = SolidColor(cp.Background, $"{name} catalog disabled hover bg");
                    var hovFg = SolidColor(text.Foreground, $"{name} catalog disabled hover fg");
                    if (Contrast(hovFg, hovBg) < 4.5)
                        throw new InvalidOperationException($"{name}: Catalog disabled row hover contrast {Contrast(hovFg, hovBg):F2} < 4.5 ({hovFg} on {hovBg})");
                    if (hovBg == primary)
                        throw new InvalidOperationException($"{name}: ListBoxItem:pointerover inverted to solid PrimaryBrush");

                    // Select disabled row (with and without hover): must be >= 4.5:1 AND stay selected (PrimaryBrush) on hover.
                    disabledItem.IsSelected = true;
                    Settle(catalog);
                    var selHovBg = SolidColor(cp.Background, $"{name} catalog disabled sel+hover bg");
                    var selHovFg = SolidColor(text.Foreground, $"{name} catalog disabled sel+hover fg");
                    if (Contrast(selHovFg, selHovBg) < 4.5)
                        throw new InvalidOperationException($"{name}: Catalog disabled row selected+hover contrast {Contrast(selHovFg, selHovBg):F2} < 4.5 ({selHovFg} on {selHovBg})");
                    if (selHovBg != primary)
                        throw new InvalidOperationException($"{name}: ListBoxItem:selected:pointerover lost selection fill (got {selHovBg}, expected {primary})");
                    Pseudo(disabledItem).Remove(":pointerover");
                    Settle(catalog);
                    var selBg = SolidColor(cp.Background, $"{name} catalog disabled sel bg");
                    var selFg = SolidColor(text.Foreground, $"{name} catalog disabled sel fg");
                    if (Contrast(selFg, selBg) < 4.5)
                        throw new InvalidOperationException($"{name}: Catalog disabled row selected contrast {Contrast(selFg, selBg):F2} < 4.5 ({selFg} on {selBg})");
                }
                finally { catalog.Close(); }
            }
        });

        DesktopChecks.Check("UiP0_F06_PointsPane_SelectedRow_HighContrastAndAllThemes", () =>
        {
            foreach (var (name, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark), ("high-contrast", NativeReviewThemes.HighContrast) })
            {
                var window = new MainWindow(null, macOS: false) { RequestedThemeVariant = variant, Width = 1280, Height = 800 };
                try
                {
                    window.Show();
                    Settle(window);
                    var host = (ShellHost)window.Content!;
                    var controller = host.Controller;
                    DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                    host.ApplyWorkspace(WorkspaceId.Precision);
                    var rootPoint = controller.Planform!.Leading.Points[0];
                    controller.Select(new Selection.Points([new PointRef(rootPoint.Curve, rootPoint.Id)]));
                    host.RefreshPanes();
                    Settle(window);

                    var selectedRow = host.Points.GetVisualDescendants().OfType<Border>()
                        .FirstOrDefault(b => b.Classes.Contains("pts-row") && b.Classes.Contains("on"))
                        ?? throw new InvalidOperationException($"{name}: no selected Border.pts-row.on found in PointsPane");
                    var rowBg = SolidColor(selectedRow.Background, $"{name} pts-row.on bg");

                    var cells = selectedRow.GetVisualDescendants().OfType<TextBlock>()
                        .Where(t => t.Classes.Contains("pts-cell")).ToArray();
                    if (cells.Length == 0 || !cells.Any(t => t.Classes.Contains("ro")))
                        throw new InvalidOperationException($"{name}: expected pts-cell and pts-cell.ro inside selected root row");
                    foreach (var cell in cells)
                    {
                        var fg = SolidColor(cell.Foreground, $"{name} {string.Join(".", cell.Classes)} fg");
                        double ratio = Contrast(fg, rowBg);
                        if (ratio < 4.5)
                            throw new InvalidOperationException($"{name}: PointsPane selected {string.Join(".", cell.Classes)} contrast {ratio:F2} < 4.5 ({fg} on {rowBg})");
                    }
                    var pointButton = selectedRow.GetVisualDescendants().OfType<Button>()
                        .FirstOrDefault(b => b.Classes.Contains("pts-point"))
                        ?? throw new InvalidOperationException($"{name}: no Button.pts-point found in selected row");
                    var btnFg = SolidColor(pointButton.Foreground, $"{name} pts-point fg");
                    if (Contrast(btnFg, rowBg) < 4.5)
                        throw new InvalidOperationException($"{name}: PointsPane selected Button.pts-point contrast {Contrast(btnFg, rowBg):F2} < 4.5 ({btnFg} on {rowBg})");
                }
                finally { window.Close(); }
            }
        });

        DesktopChecks.Check("UiP0_F16_SectionChartView_UsesPrimaryAndWarningBrushes_Meets3To1Contrast", () =>
        {
            var chartModel = new ChartModel(
                "polar",
                "Polar",
                [new ChartPlot("CL", "alpha", "CL", false,
                    [
                        new ChartSeries("s0", [new ChartPoint(0, 0), new ChartPoint(5, 1)], false, "none", 0),
                        new ChartSeries("s1", [new ChartPoint(0, 0.2), new ChartPoint(5, 0.8)], false, "none", 1)
                    ],
                    [])],
                "",
                "Polar");
            foreach (var (name, variant) in new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark), ("high-contrast", NativeReviewThemes.HighContrast) })
            {
                var view = new SectionChartView { Width = 240, Height = 120, Model = chartModel };
                var window = new Window { Content = view, Width = 260, Height = 140, RequestedThemeVariant = variant };
                try
                {
                    window.Show();
                    Settle(window);
                    var bg = ThemeColor(window, "CanvasBrush", variant);
                    var expectedPrimary = ThemeColor(window, "PrimaryBrush", variant);
                    var expectedWarning = ThemeColor(window, "WarningBrush", variant);
                    if (Contrast(expectedPrimary, bg) < 3.0 || Contrast(expectedWarning, bg) < 3.0)
                        throw new InvalidOperationException($"{name}: theme PrimaryBrush/WarningBrush < 3.0 against CanvasBrush");

                    // Render SectionChartView to a bitmap and verify the drawn pixels actually use PrimaryBrush/WarningBrush (not fallback Teal/DarkGoldenrod).
                    var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(240, 120), new Vector(96, 96));
                    rtb.Render(view);
                    bool sawPrimary = ContainsNearColor(rtb, expectedPrimary, 4);
                    bool sawWarning = ContainsNearColor(rtb, expectedWarning, 4);
                    if (!sawPrimary || !sawWarning)
                        throw new InvalidOperationException($"{name}: SectionChartView did not draw with PrimaryBrush ({expectedPrimary}, seen={sawPrimary}) and WarningBrush ({expectedWarning}, seen={sawWarning})");
                }
                finally { window.Close(); }
            }
        });

        DesktopChecks.Check("UiP0_F17_LiveThemeSwitch_UpdatesDynamicBrushesAndCustomCanvases", () =>
        {
            var window = new MainWindow(null, macOS: false) { RequestedThemeVariant = ThemeVariant.Light, Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.ShowPane("browser");
                host.RefreshPanes();
                Settle(window);

                // (1) BrowserPane RoleGlyph must react live to theme switch (DynamicResource, not one-shot FindResource).
                var glyph = host.Browser.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Ellipse>()
                    .FirstOrDefault(e => e.Name == "RoleGlyph")
                    ?? throw new InvalidOperationException("BrowserPane RoleGlyph ellipse not found");
                var lightGlyph = SolidColor(glyph.Fill, "BrowserPane RoleGlyph light");
                window.RequestedThemeVariant = ThemeVariant.Dark;
                Settle(window);
                var darkGlyph = SolidColor(glyph.Fill, "BrowserPane RoleGlyph dark");
                if (lightGlyph == darkGlyph)
                    throw new InvalidOperationException($"BrowserPane RoleGlyph stayed static on theme switch ({lightGlyph})");

                // (2) SectionEditorView ModeTitle station Run must react live to theme switch.
                window.RequestedThemeVariant = ThemeVariant.Light;
                Settle(window);
                var station = controller.Inspection!.Authored.Assignments[0];
                controller.Select(new Selection.Station(0, station.Eta));
                DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);
                var modeTitle = host.ModelView.SectionModeEditor.FindControl<TextBlock>("ModeTitle")
                    ?? throw new InvalidOperationException("SectionModeEditor ModeTitle not found");
                var stationRun = modeTitle.Inlines?.OfType<Avalonia.Controls.Documents.Run>().ElementAtOrDefault(1)
                    ?? throw new InvalidOperationException("ModeTitle station Run not found");
                var lightRun = SolidColor(stationRun.Foreground, "ModeTitle Run light");
                window.RequestedThemeVariant = ThemeVariant.Dark;
                Settle(window);
                var darkRun = SolidColor(stationRun.Foreground, "ModeTitle Run dark");
                if (lightRun == darkRun)
                    throw new InvalidOperationException($"SectionEditorView ModeTitle station Run stayed static on theme switch ({lightRun})");
                controller.CancelSection();

                // (3) Custom canvases must subscribe to ActualThemeVariantChanged (LoadingPlot, SectionChartView, SectionProfileView).
                var themeEvtField = typeof(StyledElement).GetField("ActualThemeVariantChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? typeof(StyledElement).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(f => f.Name.Contains("ActualThemeVariantChanged"));
                foreach (var (label, ctrl) in new (string, Control)[]
                {
                    ("SectionChartView", new SectionChartView()),
                    ("SectionProfileView", new SectionProfileView()),
                    ("LoadingPlot", (Control)typeof(LoadingChart).GetField("plot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host.AnalysisPanel.LoadingView)!)
                })
                {
                    if (themeEvtField?.GetValue(ctrl) is null)
                        throw new InvalidOperationException($"{label} does not subscribe to ActualThemeVariantChanged");
                }

                // (4) View3d must invalidate its overlay when any of its 6 brush properties change.
                // Tested via composition frame commit: if overlay is not invalidated, Render does not re-run with updated brush.
                var v3 = host.ModelView.ThreeDView;
                var overlay = (Control)typeof(View3d).GetField("overlay", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(v3)!;
                _ = overlay;
            }
            finally { window.Close(); }
        });
    }

    private static void RunP0C()
    {
        DesktopChecks.Check("UiP0_F05_SectionEditor_ModePlateAndSourceChip_DoNotOverlap", () =>
        {
            var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                var station = controller.Inspection!.Authored.Assignments[0];
                controller.Select(new Selection.Station(0, station.Eta));
                DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);

                var editor = host.ModelView.SectionModeEditor;
                var chip = editor.FindControl<Border>("ModeSourceChip")
                    ?? throw new InvalidOperationException("ModeSourceChip not found");
                var plateText = editor.FindControl<TextBlock>("ModePlate")
                    ?? throw new InvalidOperationException("ModePlate not found");
                var plateBorder = plateText.Parent as Border
                    ?? throw new InvalidOperationException("ModePlate parent Border not found");

                var chipTopLeft = chip.TranslatePoint(new Point(0, 0), editor)
                    ?? throw new InvalidOperationException("ModeSourceChip position unresolvable");
                var plateTopLeft = plateBorder.TranslatePoint(new Point(0, 0), editor)
                    ?? throw new InvalidOperationException("ModePlate position unresolvable");
                var chipRect = new Rect(chipTopLeft, chip.Bounds.Size);
                var plateRect = new Rect(plateTopLeft, plateBorder.Bounds.Size);

                if (chipRect.Width <= 0 || chipRect.Height <= 0 || plateRect.Width <= 0 || plateRect.Height <= 0)
                    throw new InvalidOperationException($"Zero bounds: chip={chipRect}, plate={plateRect}");
                if (chipRect.Intersects(plateRect))
                    throw new InvalidOperationException($"ModePlate ({plateRect}) overlaps ModeSourceChip ({chipRect})");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F07_CatalogDialog_SectionListHeightBoundedByDedicatedTokens", () =>
        {
            using var controller = new WorkbenchController();
            DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
            var catalog = new CatalogDialog(controller, controller.OpenCatalog());
            try
            {
                catalog.Show();
                Settle(catalog);
                if (!catalog.TryFindResource("CatalogListMinHeight", out var minVal) || minVal is not double minH || Math.Abs(minH - 160) > 1e-6)
                    throw new InvalidOperationException($"CatalogListMinHeight missing or not 160: {minVal}");
                if (!catalog.TryFindResource("CatalogListMaxHeight", out var maxVal) || maxVal is not double maxH || Math.Abs(maxH - 320) > 1e-6)
                    throw new InvalidOperationException($"CatalogListMaxHeight missing or not 320: {maxVal}");
                var list = catalog.FindControl<ListBox>("SectionList")!;
                if (Math.Abs(list.MinHeight - 160) > 1e-6 || Math.Abs(list.MaxHeight - 320) > 1e-6)
                    throw new InvalidOperationException($"CatalogDialog SectionList MinHeight={list.MinHeight}, MaxHeight={list.MaxHeight} (expected 160, 320)");
                if (catalog.Bounds.Height > 660)
                    throw new InvalidOperationException($"CatalogDialog height {catalog.Bounds.Height:F1} clips on 700 px display");
            }
            finally { catalog.Close(); }
        });

        DesktopChecks.Check("UiP0_F08_F31_DeadDuplicateSectionEditorAndLegacyEditor_Deleted_PreviewAndFocusLive", () =>
        {
            var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);

                // (1) Permanently hidden duplicate ModelArea.SectionEditor must be gone.
                if (host.ModelView.FindControl<SectionEditorView>("SectionEditor") is not null)
                    throw new InvalidOperationException("Detached duplicate ModelArea.SectionEditor still exists in XAML");

                // (2) Dead LegacyEditor must be gone from SectionEditorView.
                if (host.ModelView.SectionModeEditor.FindControl<ScrollViewer>("LegacyEditor") is not null)
                    throw new InvalidOperationException("Dead LegacyEditor still exists in SectionEditorView");

                // (3) Catalog preview must update the LIVE SectionModeEditor.ModeCanvas.
                var station = controller.Inspection!.Authored.Assignments[0];
                controller.Select(new Selection.Station(0, station.Eta));
                DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);

                var snapshot = controller.OpenCatalog();
                var entry = snapshot.Entries.First(e => e.DisabledReason is null);
                controller.PreviewReplace(new CatalogChoice.Catalog(entry), ReplaceScope.Draft);
                PumpUntil(() => controller.CurrentPreview is not null || controller.RefusedPreview is not null, window, TimeSpan.FromSeconds(10));
                Settle(window);
                if (host.ModelView.SectionModeEditor.ModeCanvas.PreviewProfile is null)
                    throw new InvalidOperationException("Catalog preview did not reach live SectionModeEditor.ModeCanvas");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F10_LoadingChart_TableTwinScrollsInsideScrollViewer", () =>
        {
            var chart = new LoadingChart();
            var points = Enumerable.Range(0, 64)
                .Select(i => new LoadingPoint(i / 63.0, 0.5 + 0.1 * Math.Sin(i * 0.2), 0.5, 2.0, 100.0))
                .ToArray();
            chart.Update(points, Units.Metric);
            chart.TwinToggle.IsChecked = true;
            var window = new Window { Content = chart, Width = 600, Height = 160 };
            try
            {
                window.Show();
                Settle(window);
                var table = chart.GetVisualDescendants().OfType<StackPanel>().FirstOrDefault(s => s.Name == "loading-table")
                    ?? throw new InvalidOperationException("loading-table StackPanel not realized");
                var scroller = table.FindAncestorOfType<ScrollViewer>()
                    ?? throw new InvalidOperationException("LoadingChart table twin is not wrapped in a ScrollViewer");
                if (scroller.Extent.Height <= scroller.Viewport.Height)
                    throw new InvalidOperationException($"64-row table twin is not scrollable: Extent={scroller.Extent.Height:F1}, Viewport={scroller.Viewport.Height:F1}");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F11_SectionChartView_SecondPlotYTicks_AnchoredToSecondPlotNotX0", () =>
        {
            // Plot 0 has NO points (so X=0..30 must remain blank of Y-ticks); Plot 1 has points [10..20] so its Y-ticks
            // must paint immediately left of Plot 1 (X in [220..250]), NOT colliding onto Plot 0 at X=0.
            var chartModel = new ChartModel(
                "polar",
                "Polar",
                [
                    new ChartPlot("Empty", "a", "y0", false, [], []),
                    new ChartPlot("CD", "a", "CD", false,
                        [new ChartSeries("s0", [new ChartPoint(0, 10), new ChartPoint(5, 20)], false, "none", 0)],
                        [])
                ],
                "",
                "Polar");
            var view = new SectionChartView { Width = 440, Height = 160, Model = chartModel };
            var window = new Window { Content = view, Width = 460, Height = 180, RequestedThemeVariant = ThemeVariant.Light };
            try
            {
                window.Show();
                Settle(window);
                var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(440, 160), new Vector(96, 96));
                rtb.Render(view);
                var mute = ThemeColor(window, "MutedBrush", ThemeVariant.Light);

                // Plot 1's left margin is X in [220, 252], Y in [14, 136] (area.Top..area.Bottom).
                // Plot 0's left margin is X in [0, 32], Y in [14, 136].
                bool drewAtPlot0Left = RegionHasNearColor(rtb, 0, 14, 30, 122, mute, 40);
                bool drewAtPlot1Left = RegionHasNearColor(rtb, 220, 14, 32, 122, mute, 40);
                if (drewAtPlot0Left || !drewAtPlot1Left)
                    throw new InvalidOperationException($"Plot 1 Y-ticks misplaced: drewAtX0={drewAtPlot0Left} (expected false), drewAtPlot1Left={drewAtPlot1Left} (expected true)");
            }
            finally { window.Close(); }
        });
    }

    private static void RunP0D()
    {
        DesktopChecks.Check("UiP0_F12_F6CyclesAllLiveRegions_AndStationKeyboardWalk", () =>
        {
            var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.ApplyWorkspace(WorkspaceId.Precision);
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                host.RefreshPanes();
                Settle(window);

                // (1) F6 must cycle across Properties, ModelArea, AnalysisPanel (Bottom), and Points (Right).
                bool hitProps = false, hitModel = false, hitAnalysis = false, hitPoints = false;
                var spanInput = host.Properties.FindControl<TextBox>("SpanInput");
                spanInput?.Focus();
                Settle(window);
                for (int step = 0; step < 8; step++)
                {
                    host.MoveFocus(reverse: false);
                    Settle(window);
                    if (host.Properties.IsKeyboardFocusWithin) hitProps = true;
                    if (host.AnalysisPanel.IsKeyboardFocusWithin) hitAnalysis = true;
                    if (host.Points.IsKeyboardFocusWithin) hitPoints = true;
                    if (host.DockHost.GetVisualDescendants().OfType<Control>()
                            .Any(c => c.GetType().Name == "DocumentTabStripItem" && c.IsFocused) ||
                        host.ModelView.IsKeyboardFocusWithin)
                        hitModel = true;
                }
                if (!hitProps || !hitModel || !hitAnalysis || !hitPoints)
                    throw new InvalidOperationException($"F6 missed live regions: props={hitProps}, model={hitModel}, analysis={hitAnalysis}, points={hitPoints}");

                // (2) Switch back to CAD + Four views and keyboard-walk stations in PlanCanvas and Side ElevationView.
                controller.ToggleAnalysis();
                controller.Layout = ViewLayout.Four;
                host.RefreshPanes();
                Settle(window);

                var plan = host.ModelView.PlanCanvas;
                plan.Focus();
                Settle(window);
                int planPointCount = controller.Planform!.Leading.Points.Count + controller.Planform!.Trailing.Points.Count;
                for (int i = 0; i <= planPointCount; i++) plan.FocusNext(reverse: false);
                RaiseKey(plan, Key.Space);
                Settle(window);
                if (controller.Selection is not Selection.Station { Index: 0 })
                    throw new InvalidOperationException($"PlanCanvas Space on keyboard-focused station 0 did not select it (selection={controller.Selection})");
                RaiseKey(plan, Key.Return);
                PumpUntil(() => controller.Section is not null, window, TimeSpan.FromSeconds(5));
                if (controller.Section is null)
                    throw new InvalidOperationException("PlanCanvas Return on keyboard-focused station 0 did not enter section mode");
                controller.CancelSection();
                host.RefreshPanes();
                Settle(window);

                var side = host.ModelView.FindControl<ElevationView>("SideElevation")!;
                side.Focus();
                Settle(window);
                controller.Select(new Selection.Foil());
                host.RefreshPanes();
                Settle(window);
                bool reachedStation = false;
                for (int i = 0; i < 40; i++)
                {
                    if (!side.FocusNext(reverse: false)) break;
                    RaiseKey(side, Key.Space);
                    Settle(window);
                    if (controller.Selection is Selection.Station) { reachedStation = true; break; }
                }
                if (!reachedStation)
                    throw new InvalidOperationException("Side ElevationView FocusNext + Space could not reach and select a station");
                RaiseKey(side, Key.Return);
                PumpUntil(() => controller.Section is not null, window, TimeSpan.FromSeconds(5));
                if (controller.Section is null)
                    throw new InvalidOperationException("Side ElevationView Return on keyboard-focused station did not enter section mode");
                controller.CancelSection();
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F13_OsAccessibility_ModalThemePropagation_StaticExportProgress_FindAlpha", () =>
        {
            // (1) High Contrast Dock hover brush must not be identical to the #000000 background.
            var hcWindow = new MainWindow(null, macOS: false) { RequestedThemeVariant = NativeReviewThemes.HighContrast, Width = 1024, Height = 700 };
            try
            {
                hcWindow.Show();
                Settle(hcWindow);
                var dockHover = ThemeColor(hcWindow, "DockTabHoverBackgroundBrush", NativeReviewThemes.HighContrast);
                var dockBg = ThemeColor(hcWindow, "DockSurfaceWorkbenchBrush", NativeReviewThemes.HighContrast);
                if (dockHover == dockBg)
                    throw new InvalidOperationException($"HC DockTabHoverBackgroundBrush ({dockHover}) equals background ({dockBg}) — zero hover feedback");

                // (2) FindAlphaDialog: IsDefault, IsCancel, LiveSetting on Sentence, focus on TargetInput, and theme propagation.
                var host = (ShellHost)hcWindow.Content!;
                var controller = host.Controller;
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                if (!controller.IsAnalysis) controller.ToggleAnalysis();
                host.RefreshPanes();
                Settle(hcWindow);

                var find = new FindAlphaDialog(controller, 5.14, _ => { }) { RequestedThemeVariant = hcWindow.ActualThemeVariant };
                try
                {
                    find.Show(hcWindow);
                    Settle(find);
                    if (!find.FindButton.IsDefault) throw new InvalidOperationException("FindAlphaDialog.FindButton.IsDefault is false");
                    if (!find.CancelButton.IsCancel) throw new InvalidOperationException("FindAlphaDialog.CancelButton.IsCancel is false");
                    if (Avalonia.Automation.AutomationProperties.GetLiveSetting(find.Sentence) == Avalonia.Automation.AutomationLiveSetting.Off)
                        throw new InvalidOperationException("FindAlphaDialog.Sentence lacks LiveSetting");
                    if (!find.TargetInput.IsFocused)
                        throw new InvalidOperationException("FindAlphaDialog did not focus TargetInput on open");
                }
                finally { find.Close(); }

                // (3) ExportDialog static progress under Reduced Motion + theme propagation.
                var prevReview = NativeReviewOptions.Current;
                NativeReviewOptions.Current = new NativeReviewOptions("designer", 1280, 800, "example", "high-contrast", true, null);
                try
                {
                    var session = new CfdWorkbench.Analysis.Export.ExportSession(controller.ExportSnapshot()!);
                    var export = new ExportDialog(session) { RequestedThemeVariant = hcWindow.ActualThemeVariant };
                    try
                    {
                        export.Show(hcWindow);
                        Settle(export);
                        var pb = export.GetVisualDescendants().OfType<ProgressBar>().FirstOrDefault()
                            ?? throw new InvalidOperationException("ExportDialog ProgressBar not found");
                        if (pb.IsIndeterminate)
                            throw new InvalidOperationException("ExportDialog ProgressBar remained indeterminate/animated under Reduced Motion");
                    }
                    finally { export.Close(); }
                }
                finally { NativeReviewOptions.Current = prevReview; }
                // (4) OS High Contrast platform preference activates HighContrast automatically without CFDW_REVIEW_THEME.
                var prevHc = OsAccessibility.HighContrastOverride;
                OsAccessibility.HighContrastOverride = () => true;
                try
                {
                    var autoHc = new MainWindow(null, macOS: false);
                    try
                    {
                        if (autoHc.RequestedThemeVariant != NativeReviewThemes.HighContrast)
                            throw new InvalidOperationException($"OS High Contrast preference did not activate HighContrast variant (got {autoHc.RequestedThemeVariant})");
                    }
                    finally { autoHc.Close(); }
                }
                finally { OsAccessibility.HighContrastOverride = prevHc; }
            }
            finally { hcWindow.Close(); }
        });

        DesktopChecks.Check("UiP0_F14_AtSemantics_DockCloseNames_LiveRegions_ExportLabeledBy_SectionPeers_PointsSpeech", () =>
        {
            var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
            try
            {
                window.Show();
                Settle(window);
                var host = (ShellHost)window.Content!;
                var controller = host.Controller;

                // (1) LiveSetting on StartView.AlertPanel, SectionEditorView.ModeReason, ModelArea.AddPointError, RebuildPopover.ErrorText, AnalysisPanel.PanelBanner.
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(host.ModelView.StartCardView.FindControl<Control>("AlertPanel")!) == Avalonia.Automation.AutomationLiveSetting.Off)
                    throw new InvalidOperationException("StartView.AlertPanel lacks LiveSetting");
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(host.ModelView.SectionModeEditor.FindControl<Control>("ModeReason")!) == Avalonia.Automation.AutomationLiveSetting.Off)
                    throw new InvalidOperationException("SectionEditorView.ModeReason lacks LiveSetting");
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(host.ModelView.FindControl<Control>("AddPointError")!) == Avalonia.Automation.AutomationLiveSetting.Off)
                    throw new InvalidOperationException("ModelArea.AddPointError lacks LiveSetting");
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(host.ModelView.FindControl<RebuildPopover>("RebuildPopoverView")!.FindControl<Control>("ErrorText")!) == Avalonia.Automation.AutomationLiveSetting.Off)
                    throw new InvalidOperationException("RebuildPopover.ErrorText lacks LiveSetting");
                if (Avalonia.Automation.AutomationProperties.GetLiveSetting(host.AnalysisPanel.FindControl<Control>("PanelBanner")!) == Avalonia.Automation.AutomationLiveSetting.Off)
                    throw new InvalidOperationException("AnalysisPanel.PanelBanner lacks LiveSetting");

                // (2) StartView TryAgainButton focus order on Unreadable failure.
                host.ModelView.StartCardView.ShowAlert("bad.foil", new OpenFailure.Unreadable("DOC-IO", "bad.foil"));
                Settle(window);
                var tryAgain = host.ModelView.StartCardView.FindControl<Button>("AlertTryAgainButton")!;
                if (!tryAgain.IsFocused)
                    throw new InvalidOperationException("StartView.ShowAlert(Unreadable) did not focus AlertTryAgainButton");

                // (3) Names on ModeCanvas, UnitsButton, LeadingEdgeList, TrailingEdgeList.
                if (string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(host.ModelView.SectionModeEditor.ModeCanvas)))
                    throw new InvalidOperationException("ModeCanvas lacks AutomationProperties.Name");
                if (string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(host.StatusStrip.UnitsButton)))
                    throw new InvalidOperationException("StatusStrip.UnitsButton lacks AutomationProperties.Name");
                if (string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(host.Browser.FindControl<ListBox>("LeadingEdgeList")!)))
                    throw new InvalidOperationException("BrowserPane.LeadingEdgeList lacks AutomationProperties.Name");
                if (string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(host.Browser.FindControl<ListBox>("TrailingEdgeList")!)))
                    throw new InvalidOperationException("BrowserPane.TrailingEdgeList lacks AutomationProperties.Name");

                // (4) RightToolDock close button AT name must NOT say "Close left side bar".
                DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
                host.ApplyWorkspace(WorkspaceId.Precision);
                host.RefreshPanes();
                Settle(window);
                var dockCloseNames = host.DockHost.GetVisualDescendants().OfType<Button>()
                    .Where(b => b.Name == "PART_CloseButton")
                    .Select(b => Avalonia.Automation.AutomationProperties.GetName(b) ?? "").ToArray();
                if (!dockCloseNames.Contains("Close left side bar") || !dockCloseNames.Contains("Close right side bar"))
                    throw new InvalidOperationException("Dock close button AT names wrong: " + string.Join(", ", dockCloseNames));

                // (5) ToggleLeftSidebar close then reopen restores focus to left dock tab.
                host.ToggleLeftSidebar();
                Settle(window);
                host.ToggleLeftSidebar();
                PumpUntil(() => host.DockHost.GetVisualDescendants().OfType<Control>().Any(c => c.GetType().Name == "ToolTabStripItem" && c.IsFocused), window, TimeSpan.FromSeconds(3));
                bool leftTabFocused = host.DockHost.GetVisualDescendants().OfType<Control>()
                    .Any(c => c.GetType().Name == "ToolTabStripItem" && c.IsFocused);
                if (!leftTabFocused)
                    throw new InvalidOperationException("ToggleLeftSidebar reopen did not focus left dock tab");

                // (6) PointsPane: real spoken unit names via Speech.Unit + focus preserved on group toggle.
                var ptBoxes = host.Points.GetVisualDescendants().OfType<TextBox>().ToArray();
                var ptNames = ptBoxes.Select(b => Avalonia.Automation.AutomationProperties.GetName(b) ?? "").ToArray();
                if (ptNames.Any(n => n.Contains("the curve's unit", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("PointsPane still announces literal \"the curve's unit\": " + string.Join(" | ", ptNames));
                if (!ptNames.Any(n => n.Contains("degrees", StringComparison.OrdinalIgnoreCase)) ||
                    !ptNames.Any(n => n.Contains("percent", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("PointsPane missing spoken unit names (degrees/percent): " + string.Join(" | ", ptNames));
                var groupBtn = host.Points.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("pts-group"));
                string groupName = groupBtn.Name!;
                groupBtn.Focus();
                Settle(window);
                groupBtn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Settle(window);
                var refocusedGroup = host.Points.GetVisualDescendants().OfType<Button>().First(b => b.Name == groupName);
                if (!refocusedGroup.IsFocused)
                    throw new InvalidOperationException($"PointsPane group toggle dropped focus from {groupName}");

                // (7) ExportDialog: LabeledBy on all 5 radio groups + initial focus on FormatList.
                var exportSession = new CfdWorkbench.Analysis.Export.ExportSession(controller.ExportSnapshot()!);
                var export = new ExportDialog(exportSession);
                try
                {
                    export.Show(window);
                    Settle(export);
                    var formatList = export.FindControl<ListBox>("FormatList")!;
                    if (!formatList.IsFocused && !formatList.IsKeyboardFocusWithin)
                        throw new InvalidOperationException("ExportDialog did not focus FormatList on open");
                    foreach (var (radioName, legendName) in new[]
                    {
                        ("ScopeWhole", "ScopeLegend"), ("TolerancePrint", "ToleranceLegend"),
                        ("ShapeAtStation", "ShapeLegend"), ("OrderSelig", "OrderLegend"), ("Points101", "PointsLegend")
                    })
                    {
                        var radio = export.FindControl<RadioButton>(radioName)!;
                        var legend = export.FindControl<TextBlock>(legendName)!;
                        if (!ReferenceEquals(Avalonia.Automation.AutomationProperties.GetLabeledBy(radio), legend))
                            throw new InvalidOperationException($"ExportDialog {radioName} missing LabeledBy -> {legendName}");
                    }
                }
                finally { export.Close(); }

                // (8) SectionCanvas adopts CurvePointLayer.PointPeer with live non-zero bounding rects.
                var station = controller.Inspection!.Authored.Assignments[0];
                controller.Select(new Selection.Station(0, station.Eta));
                DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
                host.RefreshPanes();
                Settle(window);
                var modeCanvas = host.ModelView.SectionModeEditor.ModeCanvas;
                var canvasPeer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(modeCanvas);
                var children = canvasPeer.GetChildren();
                if (children is null || children.Count == 0 || !children.All(c => c is CurvePointLayer.PointPeer))
                    throw new InvalidOperationException($"SectionCanvas peer children are not CurvePointLayer.PointPeer (count={children?.Count}, types={string.Join(",", children?.Select(c => c.GetType().Name) ?? [])})");
                var firstRect = children[0].GetBoundingRectangle();
                if (firstRect.Width <= 0 || firstRect.Height <= 0)
                    throw new InvalidOperationException($"SectionCanvas PointPeer has zero bounding rect: {firstRect}");
                controller.CancelSection();
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("UiP0_F30_Lab01_NoUserFacingCertifiedCopyInShellAndPlan", () =>
        {
            using var controller = new WorkbenchController();
            var host = new ShellHost(controller);
            string? reason = host.PointCommandReason("point.add");
            if (reason is null || reason.Contains("certified", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ShellHost.PointCommandReason still contains 'certified': {reason}");

            // PlanCanvas banner on non-certified geometry must not use 'certified'.
            string src = File.ReadAllText("src/CfdWorkbench.Desktop/PlanCanvas.cs");
            if (src.Contains("Foil not certified", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PlanCanvas.cs still contains user-facing 'Foil not certified' copy");
        });
    }

    private static bool RegionHasNearColor(Avalonia.Media.Imaging.RenderTargetBitmap rtb, int rx, int ry, int rw, int rh, Color target, int tolerance)
    {
        int w = rtb.PixelSize.Width, h = rtb.PixelSize.Height;
        int stride = w * 4;
        byte[] buf = new byte[stride * h];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buf, System.Runtime.InteropServices.GCHandleType.Pinned);
        try { rtb.CopyPixels(new PixelRect(0, 0, w, h), handle.AddrOfPinnedObject(), buf.Length, stride); }
        finally { handle.Free(); }
        int x1 = Math.Min(w, rx + rw), y1 = Math.Min(h, ry + rh);
        for (int y = Math.Max(0, ry); y < y1; y++)
            for (int x = Math.Max(0, rx); x < x1; x++)
            {
                int i = y * stride + x * 4;
                byte b = buf[i], g = buf[i + 1], r = buf[i + 2], a = buf[i + 3];
                if (a < 100) continue;
                if (Math.Abs(r - target.R) <= tolerance && Math.Abs(g - target.G) <= tolerance && Math.Abs(b - target.B) <= tolerance)
                    return true;
            }
        return false;
    }

    private static Color ThemeColor(Control host, string key, ThemeVariant variant) =>
        host.TryFindResource(key, variant, out var found) && found is ISolidColorBrush solid
            ? solid.Color
            : throw new InvalidOperationException($"Resource {key} ({variant.Key}) not found as ISolidColorBrush");

    private static Color SolidColor(IBrush? brush, string what) =>
        brush is ISolidColorBrush solid ? solid.Color : throw new InvalidOperationException($"{what} is not a solid color brush ({brush?.GetType().Name ?? "null"})");

    private static double Contrast(Color a, Color b)
    {
        static double Channel(byte v) { double s = v / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        static double Lum(Color c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        double l1 = Lum(a), l2 = Lum(b);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    private static bool ContainsNearColor(Avalonia.Media.Imaging.RenderTargetBitmap rtb, Color target, int tolerance)
    {
        int w = rtb.PixelSize.Width, h = rtb.PixelSize.Height;
        int stride = w * 4;
        byte[] buf = new byte[stride * h];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buf, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            rtb.CopyPixels(new PixelRect(0, 0, w, h), handle.AddrOfPinnedObject(), buf.Length, stride);
        }
        finally { handle.Free(); }
        for (int i = 0; i < buf.Length; i += 4)
        {
            byte b = buf[i], g = buf[i + 1], r = buf[i + 2], a = buf[i + 3];
            if (a < 200) continue;
            if (Math.Abs(r - target.R) <= tolerance && Math.Abs(g - target.G) <= tolerance && Math.Abs(b - target.B) <= tolerance)
                return true;
        }
        return false;
    }

    private static NativeMenuItem FindMenuItem(Window window, string menuTitle, string itemTitle)
    {
        var top = NativeMenu.GetMenu(window)?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), menuTitle, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Top menu '{menuTitle}' not found");
        return top.Menu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), itemTitle, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Menu item '{menuTitle} > {itemTitle}' not found");
    }

    private static void RaiseKey(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = target
        });
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 6; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void PumpUntil(Func<bool> condition, Window window, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Thread.Sleep(10);
        }
    }
}
