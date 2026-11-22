using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Persistence;
using Dock.Avalonia.Controls;
using Dock.Model.Core;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Wave 7A Track W7-SHELL red-first regression tests covering F-18, F-19, F-20, F-22, F-23, F-32, F-34, F-35, F-36,
/// F-40, F-42, F-43, and F-44 from docs/reviews/ui-as-built-workbench.md.
/// Wired into DesktopChecks.Spawn and ScaleModes as --ui-w7-shell.
/// </summary>
public static class UiW7ShellTests
{
    public static void Run()
    {
        DesktopChecks.Check("UiW7_F23_Modals_BoundedMaxHeight_ScrollableBody_StickyFooter_NoClipAt200Percent", CheckF23Modals200Percent);
        DesktopChecks.Check("UiW7_F18_ShellCommands_ToggleRight_ResetLayout_AboutDialog_NoDeadMaximize", CheckF18ShellCommandsAndAbout);
        DesktopChecks.Check("UiW7_F19_Tabs_HiddenOnStart_SectionTooltipAndAtName_CloseFoilVsCloseWindow", CheckF19TabsAndCloseFoil);
        DesktopChecks.Check("UiW7_F20_WindowsHig_NoCtrlFHijack_Mnemonics_CtrlYRedo_MenuCanExecuteRefresh", CheckF20WindowsHig);
        DesktopChecks.Check("UiW7_F22_WindowTitleAndPlanTab_ShowFilenameAndDirtyDot", CheckF22TitleAndDirtyDot);
        DesktopChecks.Check("UiW7_F32_DockSizePreset_ResizesProportionalStackPanelLocalProportion", CheckF32DockSizePreset);
        DesktopChecks.Check("UiW7_F34_StartViewAndStatusStrip_WrapAlert_TrimmedRecent_BusyDisable_SaveGuard_AnalysisFit", CheckF34StartAndStatusStrip);
        DesktopChecks.Check("UiW7_F35_AppIcon_And_ConsistentOsDialogButtonOrder_CenterOwner", CheckF35IconAndDialogHig);
        DesktopChecks.Check("UiW7_F40_StartOpenCard_MentionsCfdwJson", CheckF40StartOpenCopy);
        DesktopChecks.Check("UiW7_F36_F42_F43_F44_TokenHygiene_DeadCodePruned_LayoutScanGated", CheckF36F42F43F44Hygiene);
    }

    private static void CheckF23Modals200Percent()
    {
        var window = new MainWindow(null, macOS: false) { Width = 1024, Height = 700 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;
            host.SetTextScale(2.0);
            DesktopChecks.PumpToCompletion(host.Controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            host.RefreshPanes();
            Settle(window);

            static void AssertModalLayout(string name, Window dialog, double viewportHeight)
            {
                Settle(dialog);
                if (double.IsInfinity(dialog.MaxHeight) || dialog.MaxHeight > viewportHeight)
                    throw new InvalidOperationException($"{name}: MaxHeight {dialog.MaxHeight} is not bounded to viewport {viewportHeight}");
                if (dialog.Bounds.Height > viewportHeight + 0.5)
                    throw new InvalidOperationException($"{name}: rendered height {dialog.Bounds.Height:F1} exceeds viewport {viewportHeight}");
                var scroller = dialog.GetVisualDescendants().OfType<ScrollViewer>()
                    .FirstOrDefault(s => s.VerticalScrollBarVisibility == ScrollBarVisibility.Auto)
                    ?? throw new InvalidOperationException($"{name}: missing body ScrollViewer with VerticalScrollBarVisibility=Auto");
                var buttons = dialog.GetVisualDescendants().OfType<Button>()
                    .Where(b => b.IsEffectivelyVisible && !scroller.IsVisualAncestorOf(b))
                    .ToArray();
                if (buttons.Length == 0)
                    throw new InvalidOperationException($"{name}: no sticky footer buttons outside body ScrollViewer");
                foreach (var btn in buttons)
                {
                    var origin = btn.TranslatePoint(default, dialog)
                        ?? throw new InvalidOperationException($"{name}: footer button '{btn.Content}' has no visual transform");
                    double bottom = origin.Y + btn.Bounds.Height;
                    if (btn.Bounds.Height < 24 || origin.Y < -0.5 || bottom > dialog.Bounds.Height + 0.5)
                        throw new InvalidOperationException(
                            $"{name}: footer button '{btn.Content}' clipped at y={origin.Y:F1}..{bottom:F1} in dialog h={dialog.Bounds.Height:F1}");
                }
            }

            // 1. ExportDialog with 200% text scale and multiple bands open.
            var session = new ExportSession(host.Controller.ExportSnapshot()!);
            var export = new ExportDialog(session) { FontSize = 28 };
            try
            {
                export.Show(window);
                Settle(export);
                AssertModalLayout("ExportDialog", export, 700);
            }
            finally { export.Close(); Settle(window); }

            // 2. FolderRefusalDialog at 200% scale.
            var refusal = new FolderRefusalDialog(FolderLocation.Cloud) { FontSize = 28 };
            try
            {
                refusal.Show(window);
                Settle(refusal);
                AssertModalLayout("FolderRefusalDialog", refusal, 700);
            }
            finally { refusal.Close(); Settle(window); }

            // 3. SaveBlockedDialog at 200% scale.
            var blocked = new SaveBlockedDialog(() => Task.FromResult(new ClearResult(ClearOutcome.Cleared, "OK", 1))) { FontSize = 28 };
            try
            {
                blocked.Show(window);
                Settle(blocked);
                AssertModalLayout("SaveBlockedDialog", blocked, 700);
            }
            finally { blocked.Close(); Settle(window); }

            // 4. UnsavedDialogAsync at 200% scale.
            var unsavedMethod = typeof(MainWindow).GetMethod("UnsavedDialogAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("UnsavedDialogAsync not found");
            var pending = (Task<string>)unsavedMethod.Invoke(window, null)!;
            var modal = window.OwnedWindows.SingleOrDefault()
                ?? throw new InvalidOperationException("UnsavedDialog did not open");
            try
            {
                modal.FontSize = 28;
                Settle(modal);
                AssertModalLayout("UnsavedDialog", modal, 700);
            }
            finally
            {
                modal.Close();
                Settle(window);
                DesktopChecks.PumpToCompletion(pending, "unsaved close", TimeSpan.FromSeconds(5));
            }
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF18ShellCommandsAndAbout()
    {
        // (1) window.maximize-pane must either be pruned (YAGNI S-2) or cleanly handled — check pruned here.
        if (CommandTable.Rows.Any(r => r.Id == "window.maximize-pane"))
            throw new InvalidOperationException("Dead command row window.maximize-pane is still in CommandTable");

        // (2) macOS About CFD Workbench menu item must have a wired Command that opens an About dialog.
        var appMenu = NativeMenu.GetMenu(Application.Current!)
            ?? throw new InvalidOperationException("Application NativeMenu missing");
        var aboutItem = appMenu.Items.OfType<NativeMenuItem>().FirstOrDefault(i => Equals(i.Header, "About CFD Workbench"))
            ?? throw new InvalidOperationException("About CFD Workbench item missing");
        if (aboutItem.Command is null)
            throw new InvalidOperationException("About CFD Workbench NativeMenuItem has no Command handler");

        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;

            // (3) view.toggle-right toggles right sidebar on and off via ShellHost.RunCommand and menu.
            if (host.RightSidebarShown) throw new InvalidOperationException("Right sidebar should start hidden");
            DesktopChecks.PumpToCompletion(host.RunCommand("view.toggle-right"), "toggle right on", TimeSpan.FromSeconds(5));
            Settle(window);
            if (!host.RightSidebarShown) throw new InvalidOperationException("view.toggle-right did not show Right sidebar");
            DesktopChecks.PumpToCompletion(host.RunCommand("view.toggle-right"), "toggle right off", TimeSpan.FromSeconds(5));
            Settle(window);
            if (host.RightSidebarShown) throw new InvalidOperationException("view.toggle-right did not hide Right sidebar");

            // (4) window.reset-layout restores Left dock open, Right dock closed, ModelDocument active, and reports status.
            host.ToggleLeftSidebar();
            host.ToggleRightSidebar();
            Settle(window);
            DesktopChecks.PumpToCompletion(host.RunCommand("window.reset-layout"), "reset layout", TimeSpan.FromSeconds(5));
            Settle(window);
            bool leftOpen = host.LayoutFactory.TopProportionalDock.VisibleDockables?.Contains(host.LayoutFactory.LeftToolDock) == true;
            if (!leftOpen || host.RightSidebarShown || !ReferenceEquals(host.LayoutFactory.MainDocumentDock.ActiveDockable, host.LayoutFactory.ModelDocument))
                throw new InvalidOperationException($"window.reset-layout did not restore default layout: left={leftOpen}, right={host.RightSidebarShown}");
            if (!host.StatusStrip.Text.Contains("Layout", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"window.reset-layout did not report status: '{host.StatusStrip.Text}'");

            // (5) About dialog opens with version and closes cleanly.
            var showAbout = typeof(App).GetMethod("CreateAboutDialog", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("App.CreateAboutDialog missing");
            var aboutDialog = (Window)showAbout.Invoke(null, [window])!;
            try
            {
                aboutDialog.Show(window);
                Settle(aboutDialog);
                string allText = string.Join("\n", aboutDialog.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? ""));
                if (!allText.Contains("CFD Workbench", StringComparison.Ordinal) || !allText.Contains("0.1.0", StringComparison.Ordinal))
                    throw new InvalidOperationException("About dialog missing app name or version 0.1.0: " + allText);
            }
            finally { aboutDialog.Close(); Settle(window); }
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF19TabsAndCloseFoil()
    {
        // Distinct Close foil vs Close window rows in CommandTable.
        var closeFoil = CommandTable.Rows.FirstOrDefault(r => r.Id == "file.close")
            ?? throw new InvalidOperationException("file.close missing");
        var closeWin = CommandTable.Rows.FirstOrDefault(r => r.Id == "file.close-window")
            ?? throw new InvalidOperationException("file.close-window missing");
        if (closeFoil.Title != "Close foil")
            throw new InvalidOperationException($"file.close title expected 'Close foil', got '{closeFoil.Title}'");
        if (closeWin.Title != "Close window" || closeWin.Gesture != "⇧⌘W")
            throw new InvalidOperationException($"file.close-window expected 'Close window' / '⇧⌘W', got '{closeWin.Title}' / '{closeWin.Gesture}'");

        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;

            // (1) On Start (no foil loaded), Section and Foil source tabs must NOT be visible in MainDocumentDock.
            var startDocIds = host.LayoutFactory.MainDocumentDock.VisibleDockables!.Select(d => d.Id).ToArray();
            if (startDocIds.Contains("section") || startDocIds.Contains("foil-source"))
                throw new InvalidOperationException("Section or Foil source tab remained visible on Start: " + string.Join(",", startDocIds));

            // (2) After opening a foil, Section and Foil source appear; COPY-404 "Section" preserved verbatim, tooltip/AT = "Section analysis".
            DesktopChecks.PumpToCompletion(host.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            Settle(window);
            var openDocIds = host.LayoutFactory.MainDocumentDock.VisibleDockables!.Select(d => d.Id).ToArray();
            if (!openDocIds.SequenceEqual(["model", "section", "foil-source"]))
                throw new InvalidOperationException("Expected [model, section, foil-source] when foil open, got: " + string.Join(",", openDocIds));
            if (host.LayoutFactory.SectionDocument.Title != "Section" || Labels.SectionDocumentTitle != "Section")
                throw new InvalidOperationException($"Ruling 125 COPY-404 violated: SectionDocument.Title='{host.LayoutFactory.SectionDocument.Title}'");
            var sectionTab = host.DockHost.GetVisualDescendants().OfType<DocumentTabStripItem>()
                .FirstOrDefault(t => ReferenceEquals(t.DataContext, host.LayoutFactory.SectionDocument))
                ?? throw new InvalidOperationException("Section DocumentTabStripItem not realized");
            if (!Equals(ToolTip.GetTip(sectionTab), "Section analysis") || AutomationProperties.GetName(sectionTab) != "Section analysis")
                throw new InvalidOperationException(
                    $"Section tab tooltip='{ToolTip.GetTip(sectionTab)}' AT='{AutomationProperties.GetName(sectionTab)}' (expected 'Section analysis')");

            // (3) Close foil on dirty document prompts; Cancel retains foil; Discard returns to Start with window still open.
            host.Controller.ApplySpan("950");
            host.RefreshPanes();
            Settle(window);
            int prompts = 0;
            string reply = "Cancel";
            window.ConfirmUnsavedOverride = () => { prompts++; return Task.FromResult(reply); };

            var closeFoilItem = FindMenuItem(window, "File", "Close foil");
            closeFoilItem.Command!.Execute(null);
            PumpUntil(() => prompts == 1, window, TimeSpan.FromSeconds(3));
            Settle(window);
            if (host.Controller.Inspection is null)
                throw new InvalidOperationException("Close foil discarded edits after Cancel");

            reply = "Discard";
            closeFoilItem.Command!.Execute(null);
            PumpUntil(() => prompts == 2 && host.Controller.Inspection is null, window, TimeSpan.FromSeconds(5));
            Settle(window);
            if (!window.IsVisible || host.Controller.Inspection is not null || !host.ModelView.StartCardView.IsVisible)
                throw new InvalidOperationException($"Close foil did not return to Start: prompts={prompts}, winVis={window.IsVisible}, inspNull={host.Controller.Inspection is null}, startVis={host.ModelView.StartCardView.IsVisible}, status='{host.Controller.Status}'");
            var afterCloseIds = host.LayoutFactory.MainDocumentDock.VisibleDockables!.Select(d => d.Id).ToArray();
            if (afterCloseIds.Contains("section") || afterCloseIds.Contains("foil-source"))
                throw new InvalidOperationException("Section/Foil source tabs were not hidden after Close foil");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF20WindowsHig()
    {
        // (1) ⌃⌘F must map to F11 on Windows (NEVER Ctrl+F), and Ctrl+Meta+F on macOS.
        var winZoom = NativeMenuBuilder.ParseGesture("⌃⌘F", macOS: false);
        if (winZoom is null || winZoom.Key != Key.F11 || winZoom.KeyModifiers != KeyModifiers.None)
            throw new InvalidOperationException($"Windows window.zoom gesture expected F11, got {winZoom}");
        var macZoom = NativeMenuBuilder.ParseGesture("⌃⌘F", macOS: true);
        if (macZoom is null || macZoom.Key != Key.F || macZoom.KeyModifiers != (KeyModifiers.Control | KeyModifiers.Meta))
            throw new InvalidOperationException($"macOS window.zoom gesture expected Ctrl+Meta+F, got {macZoom}");

        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;

            // No Ctrl+F hijacked on Windows.
            if (window.KeyBindings.Any(b => b.Gesture is { Key: Key.F, KeyModifiers: KeyModifiers.Control }))
                throw new InvalidOperationException("Windows window still binds Ctrl+F to window.zoom");
            // F11 bound on Windows.
            if (!window.KeyBindings.Any(b => b.Gesture is { Key: Key.F11, KeyModifiers: KeyModifiers.None }))
                throw new InvalidOperationException("Windows window missing F11 zoom binding");
            // Ctrl+Y bound on Windows alongside Ctrl+Shift+Z.
            if (!window.KeyBindings.Any(b => b.Gesture is { Key: Key.Y, KeyModifiers: KeyModifiers.Control }))
                throw new InvalidOperationException("Windows window missing Ctrl+Y Redo binding");

            // (2) Windows in-window menu bar top-level MenuItems carry _File/_Edit/_View/_Section/_Window mnemonics.
            var menu = host.MenuBar!.GetVisualDescendants().OfType<Menu>().FirstOrDefault()
                ?? throw new InvalidOperationException("Windows in-window Menu not found");
            var topHeaders = menu.GetVisualDescendants().OfType<MenuItem>()
                .Where(m => ReferenceEquals(m.Parent, menu) || m.FindAncestorOfType<MenuItem>() is null)
                .Select(m => m.Header?.ToString() ?? "")
                .ToArray();
            string[] expectedMnemonics = ["_File", "_Edit", "_View", "_Section", "_Window"];
            foreach (string expected in expectedMnemonics)
                if (!topHeaders.Contains(expected, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Windows MenuBar missing mnemonic '{expected}'; saw: [{string.Join(", ", topHeaders)}]");

            // (3) Ctrl+Y actually redoes an undone edit.
            DesktopChecks.PumpToCompletion(host.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            host.Controller.ApplySpan("950");
            host.RefreshPanes();
            Settle(window);
            host.ModelView.PlanCanvas.Focus();
            Settle(window);
            PressKeyBinding(window, Key.Z, KeyModifiers.Control);
            Settle(window);
            if (!host.Controller.CanRedo) throw new InvalidOperationException("Ctrl+Z did not enable Redo");
            PressKeyBinding(window, Key.Y, KeyModifiers.Control);
            Settle(window);
            if (host.Controller.CanRedo || !host.Controller.CanUndo)
                throw new InvalidOperationException("Ctrl+Y did not execute Redo on Windows");

            // (4) Menu CanExecute refreshes across ALL menus (not only Edit): file.export & view.fit enable on open foil and fire CanExecuteChanged.
            var exportItem = FindMenuItem(window, "File", ExportCopy.ExportMenu);
            var fitItem = FindMenuItem(window, "View", "Fit");
            int exportChanged = 0, fitChanged = 0;
            exportItem.Command!.CanExecuteChanged += (_, _) => exportChanged++;
            fitItem.Command!.CanExecuteChanged += (_, _) => fitChanged++;
            host.Controller.ApplySpan("960");
            Settle(window);
            if (!exportItem.IsEnabled || !fitItem.IsEnabled || exportChanged == 0 || fitChanged == 0)
                throw new InvalidOperationException(
                    $"Menu CanExecute did not refresh across menus: export.IsEnabled={exportItem.IsEnabled} (ev={exportChanged}), fit.IsEnabled={fitItem.IsEnabled} (ev={fitChanged})");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF22TitleAndDirtyDot()
    {
        string dir = TestTemp.NewDirectory("w7-f22-");
        string savePath = Path.Combine(dir, "mywing.cfdw.json");
        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;
            if (host.LayoutFactory.ModelDocument.Title != "Plan")
                throw new InvalidOperationException($"Empty ModelDocument.Title expected 'Plan', got '{host.LayoutFactory.ModelDocument.Title}'");

            DesktopChecks.PumpToCompletion(host.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            Settle(window);
            if (!host.LayoutFactory.ModelDocument.Title!.Contains("example.foil", StringComparison.Ordinal))
                throw new InvalidOperationException($"Loaded ModelDocument.Title expected 'example.foil', got '{host.LayoutFactory.ModelDocument.Title}'");
            if (!window.Title!.Contains("example.foil", StringComparison.Ordinal))
                throw new InvalidOperationException($"Loaded MainWindow.Title expected 'example.foil', got '{window.Title}'");

            // Dirty edit adds • to both MainWindow.Title and Plan tab.
            host.Controller.ApplySpan("950");
            host.RefreshPanes();
            Settle(window);
            if (!host.LayoutFactory.ModelDocument.Title!.Contains('•') || !window.Title!.Contains('•'))
                throw new InvalidOperationException($"Dirty dot missing: tab='{host.LayoutFactory.ModelDocument.Title}', window='{window.Title}'");

            // Saving to mywing.cfdw.json clears • and updates filename.
            DesktopChecks.PumpToCompletion(window.SaveToAsync(savePath), "save mywing", TimeSpan.FromSeconds(15));
            Settle(window);
            if (host.LayoutFactory.ModelDocument.Title != "mywing.cfdw.json")
                throw new InvalidOperationException($"Saved tab title expected 'mywing.cfdw.json', got '{host.LayoutFactory.ModelDocument.Title}'");
            if (window.Title != "mywing.cfdw.json — CFD Workbench")
                throw new InvalidOperationException($"Saved window title expected 'mywing.cfdw.json — CFD Workbench', got '{window.Title}'");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF32DockSizePreset()
    {
        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;
            double WidthOfLeft() => host.DockHost.GetVisualDescendants().OfType<ToolDockControl>()
                .First(d => ReferenceEquals(d.DataContext, host.LayoutFactory.LeftToolDock)).Bounds.Width;

            double defaultW = WidthOfLeft();
            host.SetPaneSize("properties", "Wide");
            Settle(window);
            double wideW = WidthOfLeft();
            host.SetPaneSize("properties", "Narrow");
            Settle(window);
            double narrowW = WidthOfLeft();
            if (wideW <= defaultW + 40 || narrowW >= defaultW - 40)
                throw new InvalidOperationException($"Dock Size preset did not resize rendered pane: narrow={narrowW:F1}, default={defaultW:F1}, wide={wideW:F1}");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF34StartAndStatusStrip()
    {
        var window = new MainWindow(null, macOS: false) { Width = 1024, Height = 700 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;
            var start = host.ModelView.StartCardView;

            // (1) Alert actions container uses WrapPanel.
            var alertBorder = start.FindControl<Border>("AlertPanel")!;
            if (!alertBorder.GetVisualDescendants().OfType<WrapPanel>().Any())
                throw new InvalidOperationException("StartView AlertPanel action row is not a WrapPanel");

            // (2) Recent rows trim to filename + parent folder, full path in ToolTip, request emits full path.
            string fullPath = Path.Combine(TestTemp.Root, "HydroWing", "demo.cfdw.json");
            var standaloneStart = new StartView();
            standaloneStart.PopulateRecent([fullPath]);
            var row = standaloneStart.FindControl<ListBox>("RecentListBox")!.Items.OfType<ListBoxItem>().Single();
            string shown = row.Content?.ToString() ?? "";
            if (shown == fullPath || !shown.Contains("demo.cfdw.json", StringComparison.Ordinal) || !shown.Contains("HydroWing", StringComparison.Ordinal))
                throw new InvalidOperationException($"Recent row did not trim to filename + parent folder: '{shown}'");
            if (!Equals(ToolTip.GetTip(row), fullPath))
                throw new InvalidOperationException($"Recent row ToolTip expected full path, got '{ToolTip.GetTip(row)}'");
            string? requested = null;
            standaloneStart.RecentRequested += p => requested = p;
            var recentList = standaloneStart.FindControl<ListBox>("RecentListBox")!;
            recentList.SelectedItem = row;
            recentList.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = recentList });
            if (requested != fullPath)
                throw new InvalidOperationException($"RecentRequested emitted '{requested}', expected full path '{fullPath}'");

            // (3) CardsPanel disabled while opening, re-enabled on Cancel/Hide.
            var cards = start.FindControl<StackPanel>("CardsPanel")!;
            start.ShowOpening("demo.foil");
            if (cards.IsEnabled) throw new InvalidOperationException("CardsPanel stayed enabled while opening");
            start.CancelOpening();
            if (!cards.IsEnabled) throw new InvalidOperationException("CardsPanel stayed disabled after CancelOpening");

            // (4) file.save CanExecute is false when no foil is open, true when foil is open.
            var saveItem = FindMenuItem(window, "File", "Save");
            if (saveItem.Command!.CanExecute(null) || saveItem.IsEnabled)
                throw new InvalidOperationException("file.save was enabled with no foil open");
            DesktopChecks.PumpToCompletion(host.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            Settle(window);
            if (!saveItem.Command!.CanExecute(null) || !saveItem.IsEnabled)
                throw new InvalidOperationException("file.save stayed disabled after opening a foil");

            // (5) StatusStrip.FitItems includes AnalysisItem so narrow windows drop it instead of clipping.
            host.StatusStrip.ShowAnalysisState(RunState.Current);
            host.StatusStrip.Width = 140;
            Settle(window);
            var analysisItem = host.StatusStrip.FindControl<Border>("AnalysisItem")!;
            if (analysisItem.IsVisible)
                throw new InvalidOperationException("StatusStrip.FitItems did not drop AnalysisItem at narrow width 140 px");
            host.StatusStrip.Width = double.NaN;
            Settle(window);
            if (!analysisItem.IsVisible)
                throw new InvalidOperationException("StatusStrip.FitItems did not restore AnalysisItem at full width");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF35IconAndDialogHig()
    {
        // (1) App icon asset embedded and wired on MainWindow + dialogs.
        var iconUri = new Uri("avares://CfdWorkbench.Desktop/Assets/app-icon.ico");
        if (!AssetLoader.Exists(iconUri))
            throw new InvalidOperationException("Assets/app-icon.ico is not embedded as an AvaloniaResource");

        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            if (window.Icon is null) throw new InvalidOperationException("MainWindow.Icon is null");
            var host = (ShellHost)window.Content!;
            DesktopChecks.PumpToCompletion(host.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            var export = new ExportDialog(new ExportSession(host.Controller.ExportSnapshot()!));
            try
            {
                if (export.Icon is null) throw new InvalidOperationException("ExportDialog.Icon is null");
                if (export.WindowStartupLocation != WindowStartupLocation.CenterOwner)
                    throw new InvalidOperationException($"ExportDialog.WindowStartupLocation expected CenterOwner, got {export.WindowStartupLocation}");

                // (2) Consistent button order: on Windows Primary is left of Cancel; on macOS Cancel is left of Primary.
                export.Show(window);
                Settle(export);
                var exportCancel = export.FindControl<Button>("CancelButton")!;
                var exportPrimary = export.FindControl<Button>("ExportButton")!;
                double cancelX = exportCancel.TranslatePoint(default, export)!.Value.X;
                double primaryX = exportPrimary.TranslatePoint(default, export)!.Value.X;
                bool expectPrimaryLeft = !OperatingSystem.IsMacOS();
                if (expectPrimaryLeft && primaryX >= cancelX)
                    throw new InvalidOperationException($"Windows ExportDialog must put primary left of Cancel (primaryX={primaryX:F1}, cancelX={cancelX:F1})");
                if (!expectPrimaryLeft && cancelX >= primaryX)
                    throw new InvalidOperationException($"macOS ExportDialog must put Cancel left of primary (cancelX={cancelX:F1}, primaryX={primaryX:F1})");
            }
            finally { export.Close(); Settle(window); }

            var refusal = new FolderRefusalDialog(FolderLocation.Cloud);
            if (refusal.Icon is null) throw new InvalidOperationException("FolderRefusalDialog.Icon is null");
            var blocked = new SaveBlockedDialog();
            if (blocked.Icon is null) throw new InvalidOperationException("SaveBlockedDialog.Icon is null");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
    }

    private static void CheckF40StartOpenCopy()
    {
        var start = new StartView();
        var openBtn = start.FindControl<Button>("StartOpenButton")!;
        string text = string.Join(" ", openBtn.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? ""));
        if (string.IsNullOrEmpty(text))
            text = string.Join(" ", Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(openBtn).OfType<TextBlock>().Select(t => t.Text ?? ""));
        if (!text.Contains(".foil", StringComparison.Ordinal) || !text.Contains(".cfdw.json", StringComparison.Ordinal))
            throw new InvalidOperationException($"StartView Open card caption must mention both .foil and .cfdw.json: '{text}'");
    }

    private static void CheckF36F42F43F44Hygiene()
    {
        // (1) F-36: shared tokens exist in Application resources.
        var app = Application.Current ?? throw new InvalidOperationException("Application.Current missing");
        foreach (string key in new[] { "MonoFontFamily", "DialogPadding", "DialogSpacing", "WarningBandBorder" })
            if (!app.TryFindResource(key, out var val) || val is null)
                throw new InvalidOperationException($"Missing promoted Styles.axaml token '{key}'");

        // (2) F-43: pruned unused legacy tokens must be absent.
        foreach (string deadKey in new[] { "TitleSize", "SectionThumbnailWidth", "SectionThumbnailHeight", "SectionPanelHeight" })
            if (app.TryFindResource(deadKey, out _))
                throw new InvalidOperationException($"Unused legacy token '{deadKey}' still exists in Styles.axaml");

        // (3) F-42: dead RailControlsTool deleted while FloatFrame/FloatPlacement retained.
        if (typeof(ShellLayoutFactory).GetProperty("RailControlsTool") is not null)
            throw new InvalidOperationException("Dead RailControlsTool property still exists on ShellLayoutFactory");
        var factory = new ShellLayoutFactory();
        factory.CreateLayout();
        if (factory.FindDockable("rail-controls") is not null)
            throw new InvalidOperationException("Dead rail-controls dockable still found in ShellLayoutFactory");
        _ = FloatFrame.From(0, 0, 260, 400, 1.0); // retained C-1

        // (4) F-44: DockHost.LayoutUpdated scan gated (no redundant scans on layout churn).
        var prop = typeof(ShellHost).GetProperty("DockLayoutFixupPasses", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("ShellHost.DockLayoutFixupPasses telemetry property missing (F-44 gate)");
        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var host = (ShellHost)window.Content!;
            int settledPasses = (int)prop.GetValue(host)!;
            for (int i = 0; i < 6; i++)
            {
                host.DockHost.InvalidateArrange();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
            }
            int afterChurn = (int)prop.GetValue(host)!;
            if (afterChurn > settledPasses)
                throw new InvalidOperationException($"DockHost.LayoutUpdated rescanned on redundant layout passes: before={settledPasses}, after={afterChurn}");
        }
        finally
        {
            ApproveClose(window);
            window.Close();
        }
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

    private static void PressKeyBinding(Window window, Key key, KeyModifiers modifiers)
    {
        var focused = window.FocusManager?.GetFocusedElement() as Visual ?? window;
        var down = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers, Source = focused };
        for (Visual? at = focused; at is not null && !down.Handled; at = at.GetVisualParent())
            if (at is InputElement element)
                foreach (var binding in element.KeyBindings)
                {
                    binding.TryHandle(down);
                    if (down.Handled) break;
                }
        if (!down.Handled) (focused as Interactive)?.RaiseEvent(down);
    }

    private static void ApproveClose(MainWindow window) =>
        typeof(MainWindow).GetField("closeApproved", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(window, true);

    private static void Settle(Window window)
    {
        for (int i = 0; i < 8; i++)
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
