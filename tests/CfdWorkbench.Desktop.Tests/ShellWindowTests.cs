using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using CfdWorkbench.Desktop.Panes;

namespace CfdWorkbench.Desktop.Tests;

public static class ShellWindowTests
{
    public static void Run()
    {
        DesktopChecks.Check("Architecture_DockConfinedToShell", () =>
        {
            string root = FindRepoRoot();
            string desktop = Path.Combine(root, "src", "CfdWorkbench.Desktop");
            var offenders = Directory.EnumerateFiles(desktop, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}Shell{Path.DirectorySeparatorChar}"))
                .Where(path => File.ReadAllText(path).Contains("Dock.", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(root, path)).ToArray();
            if (offenders.Length != 0)
                throw new InvalidOperationException("Dock reference outside Shell/: " + string.Join(", ", offenders));
        });

        DesktopChecks.Check("NativeMenu_MainWindow_BuiltFromTable", () =>
        {
            var window = new Window();
            var menu = NativeMenuBuilder.BuildForWindow(window);
            var expected = CommandTable.Rows.Select(row => row.Title).ToHashSet(StringComparer.Ordinal);
            var actual = menu.Items.OfType<NativeMenuItem>()
                .SelectMany(item => item.Menu?.Items.OfType<NativeMenuItem>() ?? [])
                .Select(item => item.Header?.ToString() ?? "").ToHashSet(StringComparer.Ordinal);
            if (!expected.IsSubsetOf(actual))
                throw new InvalidOperationException("Native menu lacks: " + string.Join(", ", expected.Except(actual)));
            if (!ReferenceEquals(NativeMenu.GetMenu(window), menu))
                throw new InvalidOperationException("Menu was not installed on the window");
        });

        DesktopChecks.Check("ShellHost_PlanformLayout_ContainsModelAndSidePanes", () =>
        {
            using var controller = new WorkbenchController();
            ShellHost host;
            try { host = new ShellHost(controller); }
            catch (Exception error) { throw new InvalidOperationException(error.ToString(), error); }
            var ids = host.LayoutFactory.MainDocumentDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (!ids.Contains("model") || !ids.Contains("section-sample") || !ids.Contains("foil-source"))
                throw new InvalidOperationException("Model area document tabs are absent");
            var paneIds = host.LayoutFactory.LeftToolDock.VisibleDockables?.Select(item => item.Id).ToArray() ?? [];
            if (!paneIds.Contains("properties") || !paneIds.Contains("browser") || !paneIds.Contains("rail-controls"))
                throw new InvalidOperationException("Planform side bar is incomplete");
            if (host.DockHost.Layout is null || host.DockHost.Factory is null)
                throw new InvalidOperationException("Dock host is not connected to its layout");
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                window.UpdateLayout();
                if (!host.DockHost.IsVisible)
                    throw new InvalidOperationException("Dock host did not render in the window");
                foreach (var document in new[]
                {
                    host.LayoutFactory.SectionSampleDocument,
                    host.LayoutFactory.FoilSourceDocument,
                    host.LayoutFactory.SectionDocument,
                    host.LayoutFactory.ModelDocument
                })
                {
                    host.LayoutFactory.MainDocumentDock.ActiveDockable = document;
                    window.UpdateLayout();
                }
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("ShellHost_AcceptedExample_BindsPanes", () =>
        {
            using var controller = new WorkbenchController();
            Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
            var host = new ShellHost(controller);
            var window = new Window { Content = host, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                window.UpdateLayout();
                if (!host.ModelView.FindControl<Control>("DocumentTabs")!.IsVisible ||
                    host.Browser.FindControl<ListBox>("StationList")!.ItemCount == 0 ||
                    !host.Properties.FindControl<Control>("ContentPanel")!.IsVisible ||
                    !host.Properties.FindControl<Control>("WingBlock")!.IsVisible)
                    throw new InvalidOperationException("Accepted foil did not reach the model and pane surfaces");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Viewport_FocusVertex_RaisesFocusedTargetChanged", () =>
        {
            var viewport = new Viewport
            {
                Semantics = [new ViewportSemantic("cv-1", "upper control vertex cv-1", "editable")]
            };
            FocusedTargetEventArgs? raised = null;
            viewport.FocusedTargetChanged += (_, args) => raised = args;
            viewport.FocusVertex("cv-1");
            if (raised?.AccessibleName != "upper control vertex cv-1" ||
                raised.Bounds.Width <= 0 || raised.Bounds.Height <= 0)
                throw new InvalidOperationException("Viewport did not emit the focused semantic target");
            raised = null;
            viewport.FocusVertex("missing");
            if (raised is not null)
                throw new InvalidOperationException("Unknown viewport target emitted focus");
        });

        DesktopChecks.Check("SectionCanvas_FocusVertex_RaisesFocusedTargetChanged", () =>
        {
            var vertices = new[] { new ProfileVertex("upper", "u1", .25, .08, false) };
            var points = new[] { new ProfilePoint(.25, .08) };
            var canvas = new SectionCanvas
            {
                Width = 800,
                Height = 400,
                Profile = new ProfileView("section", "hash", vertices, [], points, [], "open")
            };
            FocusedTargetEventArgs? raised = null;
            canvas.FocusedTargetChanged += (_, args) => raised = args;
            canvas.FocusVertex("upper", "u1");
            if (canvas.SelectedVertex != ("upper", "u1") ||
                raised?.AccessibleName != "upper vertex u1" ||
                raised.Bounds.Width <= 0 || raised.Bounds.Height <= 0)
                throw new InvalidOperationException("SectionCanvas did not emit the selected vertex bounds");
            raised = null;
            canvas.FocusVertex("upper", "missing");
            if (raised is not null)
                throw new InvalidOperationException("Unknown section vertex emitted focus");
        });

        DesktopChecks.Check("Start_Opening_FocusOnCancel", () =>
        {
            var start = new StartView();
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                start.ShowOpening("foil.foil", StartControl<Button>(start, "StartOpenButton"));
                if (!StartControl<Control>(start, "OpeningPanel").IsVisible ||
                    !StartControl<Button>(start, "OpenCancelButton").IsFocused)
                    throw new InvalidOperationException("Opening did not focus its Cancel button");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_OpeningCancel_FocusReturnsToCard", () =>
        {
            var start = new StartView();
            var window = new Window { Content = start, Width = 1024, Height = 700 };
            try
            {
                window.Show();
                start.ShowOpening("foil.foil", StartControl<Button>(start, "StartOpenButton"));
                start.CancelOpening();
                if (StartControl<Control>(start, "OpeningPanel").IsVisible ||
                    !StartControl<Button>(start, "StartOpenButton").IsFocused)
                    throw new InvalidOperationException("Cancel did not return focus to the originating card action");
            }
            finally { window.Close(); }
        });

        DesktopChecks.Check("Start_OpenMissing_AlertLocate", () =>
        {
            var start = new StartView();
            start.ShowAlert("missing.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "missing.foil"));
            if (!StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "AlertLocateButton").IsVisible ||
                StartControl<TextBlock>(start, "AlertTitle").Text?.Contains("missing.foil", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Missing-file alert lacks its file name or Locate action");
        });

        DesktopChecks.Check("Start_OpenNewer_AlertOpenAnother", () =>
        {
            var start = new StartView();
            start.ShowAlert("newer.foil", new OpenFailure.Newer("FUTURE-VER", "newer.foil"));
            if (!StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "AlertOpenAnotherButton").IsVisible)
                throw new InvalidOperationException("Newer-file alert lacks Open another file");
        });

        DesktopChecks.Check("Start_OpenFailedDismissed_StartKept", () =>
        {
            var start = new StartView();
            start.ShowAlert("broken.foil", new OpenFailure.Missing("FILE-NOT-FOUND", "broken.foil"));
            start.DismissAlert();
            if (StartControl<Control>(start, "AlertPanel").IsVisible ||
                !StartControl<Button>(start, "StartNewButton").IsVisible)
                throw new InvalidOperationException("Dismissing open failure did not keep the Start card");
        });
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "CFDWorkbench.slnx")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new DirectoryNotFoundException("CFDWorkbench.slnx not found");
    }

    private static T StartControl<T>(StartView start, string name) where T : Control =>
        start.FindControl<T>(name) ?? throw new InvalidOperationException($"Start control {name} missing");
}
