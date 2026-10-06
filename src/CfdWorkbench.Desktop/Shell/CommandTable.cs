using System.Windows.Input;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

public sealed record CommandRow(
    string Id,
    string Title,
    string Menu,
    string? Gesture,
    bool IsEditVerb,
    ICommand Command);

public sealed record PaletteEntry(string Id, string Title, string? Gesture, string Menu);

public sealed class DelegateCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public static class CommandTable
{
    private static readonly DelegateCommand NoOp = new(() => { });

    /// <summary>The View submenu that holds the Metric and Imperial items (Ruling 115).</summary>
    public const string UnitsMenu = "Units";

    /// <summary>The Units a "view.units-*" command sets, or null for any other command.</summary>
    public static CfdWorkbench.Analysis.Units? UnitsOf(string id) => id switch
    {
        "view.units-metric" => CfdWorkbench.Analysis.Units.Metric,
        "view.units-imperial" => CfdWorkbench.Analysis.Units.Imperial,
        _ => null
    };

    /// <summary>The menu that holds the section editor's rows (§5.2).</summary>
    public const string SectionMenu = "Section";

    /// <summary>The View submenu that holds the Text size items.</summary>
    public const string TextSizeMenu = "Text size";

    /// <summary>
    /// The Text size ladder (DN-5): 100 %, 125 %, 150 %, 200 %; never below or above. Derived from the stored set
    /// (<see cref="CfdWorkbench.Persistence.DisplayPreferences.TextSizes"/>), so there is one definition.
    /// </summary>
    public static readonly IReadOnlyList<double> TextSizes =
        CfdWorkbench.Persistence.DisplayPreferences.TextSizes.Select(percent => percent / 100.0).ToArray();

    /// <summary>The ladder step a "view.text-NNN" command sets, or null for any other command.</summary>
    public static double? TextSizeOf(string id) =>
        id.StartsWith("view.text-", StringComparison.Ordinal) && int.TryParse(id["view.text-".Length..], out int percent)
            ? percent / 100.0 : null;

    public static readonly IReadOnlyList<CommandRow> DefaultRows =
    [
        // File
        new("file.new", "New foil", "File", "⌘N", false, NoOp),
        new("file.new-example", "New from example", "File", "⇧⌘N", false, NoOp),
        new("file.open", "Open…", "File", "⌘O", false, NoOp),
        new("file.save", "Save", "File", "⌘S", false, NoOp),
        new("file.save-as", "Save As…", "File", "⇧⌘S", false, NoOp),
        new("file.close", "Close", "File", "⌘W", false, NoOp),

        // Edit
        new("edit.undo", "Undo", "Edit", "⌘Z", true, NoOp),
        new("edit.redo", "Redo", "Edit", "⇧⌘Z", true, NoOp),
        new("edit.cut", "Cut", "Edit", "⌘X", true, NoOp),
        new("edit.copy", "Copy", "Edit", "⌘C", true, NoOp),
        new("edit.paste", "Paste", "Edit", "⌘V", true, NoOp),
        new("edit.select-all", "Select All", "Edit", "⌘A", true, NoOp),
        new("point.add", "Add point…", "Edit", null, false, NoOp),
        new("point.remove", "Remove point", "Edit", "⌫", false, NoOp),
        new("point.rebuild", "Rebuild curve…", "Edit", null, false, NoOp),

        // View
        new("view.analysis", "Analysis", "View", "⇧⌘A", false, NoOp),
        new("view.toggle-left", "Left side bar", "View", "⌘B", false, NoOp),
        new("view.toggle-bottom", "Bottom panel", "View", "⌘J", false, NoOp),
        new("view.toggle-right", "Right side bar", "View", "⌥⌘B", false, NoOp),
        new("view.palette", "Command palette", "View", "⌘K", false, NoOp),
        new("view.fit", "Fit", "View", "⌘0", false, NoOp),
        // M1.2b2 §5.2: Fit Selection and the View ▸ Views / Display / Camera / Pan submenus, run by ViewCommands. Their keys are
        // shown, never bound in the menu bar: F and the arrows are bound on the view control (2.1.4), Home on the 3D view.
        new("view.fit-selection", "Fit Selection", "View", "F", false, NoOp),
        new("view.layout-plan3d", "Plan + 3D", ViewCommands.ViewsMenu, null, false, NoOp),
        new("view.layout-four", "Four views", ViewCommands.ViewsMenu, null, false, NoOp),
        new("view.layout-one", "One view", ViewCommands.ViewsMenu, null, false, NoOp),
        new("view.display-shaded", "Shaded", ViewCommands.DisplayMenu, null, false, NoOp),
        new("view.display-wireframe", "Wireframe", ViewCommands.DisplayMenu, null, false, NoOp),
        new("view.camera-top", "Top", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.camera-front", "Front", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.camera-side", "Side", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.camera-iso", "Iso", ViewCommands.CameraMenu, "Home", false, NoOp),
        new("view.camera-bottom", "Bottom", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.camera-back", "Back", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.camera-port", "Port", ViewCommands.CameraMenu, null, false, NoOp),
        new("view.pan-left", "Pan left", ViewCommands.PanMenu, "⇧← / ⌥←", false, NoOp),
        new("view.pan-right", "Pan right", ViewCommands.PanMenu, "⇧→ / ⌥→", false, NoOp),
        new("view.pan-up", "Pan up", ViewCommands.PanMenu, "⇧↑ / ⌥↑", false, NoOp),
        new("view.pan-down", "Pan down", ViewCommands.PanMenu, "⇧↓ / ⌥↓", false, NoOp),
        new("view.comb", "Curvature comb", "View", "C", false, NoOp),
        // M1.2c §5.2: Thickness ×2 draws y at twice its scale in the section editor; values are never scaled.
        new("view.thickness-x2", "Thickness ×2", "View", null, false, NoOp),
        new("view.zoom-in", "Zoom in", "View", "⌘=", false, NoOp),
        new("view.zoom-out", "Zoom out", "View", "⌘−", false, NoOp),

        // View ▸ Text size (DN-5). DR-DEN-4: ⌘= / ⌘− zoom a focused model view and step the Text size anywhere else; these
        // items always change the Text size.
        new("view.text-bigger", "Bigger", TextSizeMenu, null, false, NoOp),
        new("view.text-smaller", "Smaller", TextSizeMenu, null, false, NoOp),
        new("view.text-100", "100 %", TextSizeMenu, null, false, NoOp),
        new("view.text-125", "125 %", TextSizeMenu, null, false, NoOp),
        new("view.text-150", "150 %", TextSizeMenu, null, false, NoOp),
        new("view.text-200", "200 %", TextSizeMenu, null, false, NoOp),
        // View ▸ Units (Ruling 115): display units for every area; the status-bar item toggles the same state.
        new("view.units-metric", "Metric", UnitsMenu, null, false, NoOp),
        new("view.units-imperial", "Imperial", UnitsMenu, null, false, NoOp),
        new("point.make-anchor", "Make anchor", "Edit", null, false, NoOp),
        new("point.make-control", "Make control", "Edit", null, false, NoOp),
        new("point.tangent-smooth", "Smooth tangent", "Edit", null, false, NoOp),
        new("point.tangent-symmetric", "Symmetric tangent", "Edit", null, false, NoOp),
        new("point.tangent-corner", "Corner tangent", "Edit", null, false, NoOp),

        // Section (M1.2c §5.2): the section editor's rows. Each runs, or names why it cannot (ShellHost.ShellCommandReason).
        new("section.edit", "Edit section…", SectionMenu, "↩", false, NoOp),
        new("section.finish", "Finish section", SectionMenu, "⌘↩", false, NoOp),
        new("section.cancel", "Cancel section", SectionMenu, null, false, NoOp),
        new("section.insert-point", "Insert point", SectionMenu, null, false, NoOp),
        new("section.insert-anchor", "Insert anchor (keep shape)", SectionMenu, null, false, NoOp),
        new("section.delete-point", "Delete point", SectionMenu, "⌫", false, NoOp),
        new("section.smooth", "Smooth", SectionMenu, null, false, NoOp),
        new("section.replace-catalog", "Replace from catalog…", SectionMenu, null, false, NoOp),
        new("section.save-mine", "Save to My sections…", SectionMenu, null, false, NoOp),
        new("section.import-dat", "Import .dat…", SectionMenu, null, false, NoOp),
        new("section.make-unique", "Make unique to this station", SectionMenu, null, false, NoOp),
        new("section.thickness-channel", "Station t/c from the Thickness curve", SectionMenu, null, false, NoOp),
        new("section.thickness-source", "Station t/c from this section", SectionMenu, null, false, NoOp),

        // Window
        new("window.minimize", "Minimize", "Window", "⌘M", false, NoOp),
        new("window.zoom", "Zoom", "Window", "⌃⌘F", false, NoOp),
        new("window.workspace-planform", "Planform", "Window", "⌘1", false, NoOp),
        new("window.workspace-precision", "Precision", "Window", "⌘2", false, NoOp),
        new("window.workspace-review", "Review", "Window", "⌘3", false, NoOp),
        new("window.points", "Points", "Window", null, false, NoOp),
        new("window.layers", "Layers", "Window", null, false, NoOp),
        new("window.reset-layout", "Reset layout", "Window", "⌥⌘R", false, NoOp),
        new("window.maximize-pane", "Maximize pane", "Window", "⇧⌘M", false, NoOp)
    ];

    public static IReadOnlyList<CommandRow> Rows => DefaultRows;

    public static IReadOnlyList<PaletteEntry> PaletteEntries() =>
        Rows.Select(r => new PaletteEntry(r.Id, r.Title, r.Gesture, r.Menu)).ToList();

    public static IReadOnlyList<CommandRow> MenuFor(string menuName) =>
        Rows.Where(r => string.Equals(r.Menu, menuName, StringComparison.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<CommandRow> Bindings(IReadOnlySet<string>? exportedGestures = null) =>
        Rows.Where(r => r.Id is not ("point.remove" or "view.analysis") && !string.IsNullOrEmpty(r.Gesture) &&
            (exportedGestures == null || !exportedGestures.Contains(r.Gesture))).ToList();
}

/// <summary>
/// What a view command needs from the shell: the controller, the drawn size of each view, the status strip's
/// <c>Report</c> sink, the model view that has keyboard focus (it, not the target view, takes ⌘= / ⌘− / ⌘0), and the
/// 3D view's own preset verb (<see cref="View3d.ApplyPreset"/>, the cube's path), which View ▸ Camera calls when present.
/// </summary>
public sealed record ViewCommandContext(WorkbenchController Controller, Func<SingleView, Avalonia.Size> ViewSize,
    Action<StatusReport> Report, SingleView? Focused = null, Action<NamedCamera>? ApplyPreset = null);

/// <summary>
/// The view command runner (M1.2b2 §5.2, §6.2): layouts, display modes, named cameras, pan, fit and zoom, acting on the
/// target view through the controller's session values. Every verb is one <see cref="ViewCamera"/> function, so the
/// menu, the palette and the keys produce the same camera. Each command either runs or names why it cannot
/// (UI-DEAD-CONTROL).
/// </summary>
public static class ViewCommands
{
    public const string ViewsMenu = "Views";
    public const string DisplayMenu = "Display";
    public const string CameraMenu = "Camera";
    public const string PanMenu = "Pan";

    /// <summary>One step of View ▸ Pan: 10 % of the view (§6.2).</summary>
    public const double PanFraction = 0.1;

    /// <summary>One step of zoom in or out about the centre.</summary>
    public const double ZoomStep = 1.25;

    private static readonly IReadOnlyDictionary<string, NamedCamera> Cameras = new Dictionary<string, NamedCamera>(StringComparer.Ordinal)
    {
        ["view.camera-top"] = NamedCamera.Top, ["view.camera-front"] = NamedCamera.Front, ["view.camera-side"] = NamedCamera.Side,
        ["view.camera-iso"] = NamedCamera.Iso, ["view.camera-bottom"] = NamedCamera.Bottom, ["view.camera-back"] = NamedCamera.Back,
        ["view.camera-port"] = NamedCamera.Port
    };

    private static readonly IReadOnlyDictionary<string, (int X, int Y)> Pans = new Dictionary<string, (int, int)>(StringComparer.Ordinal)
    {
        ["view.pan-left"] = (-1, 0), ["view.pan-right"] = (1, 0), ["view.pan-up"] = (0, -1), ["view.pan-down"] = (0, 1)
    };

    public static bool Handles(string id) =>
        id.StartsWith("view.layout-", StringComparison.Ordinal) || id.StartsWith("view.display-", StringComparison.Ordinal) ||
        Cameras.ContainsKey(id) || Pans.ContainsKey(id) || id is "view.fit-selection" or "view.fit" or "view.zoom-in" or "view.zoom-out";

    public static bool CanRun(string id, WorkbenchController controller) => DisabledReason(id, controller) is null;

    /// <summary>Why <paramref name="id"/> cannot run now, or null when it can.</summary>
    public static string? DisabledReason(string id, WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (controller.Inspection is null) return "Open a foil to use the views.";
        if (id.StartsWith("view.layout-", StringComparison.Ordinal) || id.StartsWith("view.display-", StringComparison.Ordinal)) return null;
        if (Cameras.ContainsKey(id))
            return !controller.Layout.Shows(SingleView.ThreeD) ? "Show the 3D view to choose its camera."
                : controller.Surface is null ? "The 3D view is still being drawn." : null;
        var target = Target(controller, null);
        return target == SingleView.Plan || controller.Surface is not null ? null : $"The {Name(target)} view is still being drawn.";
    }

    /// <summary>The radio state of a Views or Display item; null for any other command.</summary>
    public static bool? IsChecked(string id, WorkbenchController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return id switch
        {
            "view.layout-plan3d" => controller.Layout.Arrangement == ViewArrangement.Plan3d,
            "view.layout-four" => controller.Layout.Arrangement == ViewArrangement.Four,
            "view.layout-one" => controller.Layout.Arrangement == ViewArrangement.One,
            "view.display-shaded" => controller.DisplayFor(DisplayTarget(controller)) == DisplayMode.Shaded,
            "view.display-wireframe" => controller.DisplayFor(DisplayTarget(controller)) == DisplayMode.Wireframe,
            _ => null
        };
    }

    public static void Run(string id, ViewCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var controller = context.Controller;
        if (!CanRun(id, controller)) return;
        switch (id)
        {
            case "view.layout-plan3d":
                controller.Layout = ViewLayout.Plan3d;
                context.Report(new StatusReport("Plan + 3D."));
                return;
            case "view.layout-four":
                controller.Layout = ViewLayout.Four;
                context.Report(new StatusReport("Four views."));
                return;
            case "view.layout-one":
                var single = Target(controller, null);
                controller.Layout = ViewLayout.One(single);
                context.Report(new StatusReport($"One view: {Name(single)}."));
                return;
            case "view.display-shaded" or "view.display-wireframe":
                var shown = DisplayTarget(controller);
                var mode = id == "view.display-shaded" ? DisplayMode.Shaded : DisplayMode.Wireframe;
                controller.SetDisplay(shown, mode);
                context.Report(new StatusReport($"{Name(shown)} view: {mode.ToString().ToLowerInvariant()}."));
                return;
        }
        if (Cameras.TryGetValue(id, out var named))
        {
            // One path for the cube and the menu: the 3D view fits the preset to its own size and announces it.
            if (context.ApplyPreset is { } applyPreset)
            {
                applyPreset(named);
                return;
            }
            var (minimum, maximum) = WholeBounds(controller.Surface!);
            var camera = ViewCamera.Named(named, minimum, maximum, context.ViewSize(SingleView.ThreeD));
            controller.Camera3d = camera;
            context.Report(new StatusReport($"3D view: {camera.Title}."));
            return;
        }
        // DR-DEN-4: the keys ⌘= / ⌘− / ⌘0 act on the model view that has focus; menu verbs act on the target view.
        var target = Target(controller, id is "view.zoom-in" or "view.zoom-out" or "view.fit" ? context.Focused : null);
        var size = context.ViewSize(target);
        if (target == SingleView.Plan)
        {
            RunOnPlan(id, controller, size, context.Report);
            return;
        }
        var current = CameraOf(controller, target, size);
        ViewCamera next;
        string? report = null;
        if (Pans.TryGetValue(id, out var step)) next = current.Pan(step.X * size.Width * PanFraction, step.Y * size.Height * PanFraction, size);
        else if (id is "view.zoom-in" or "view.zoom-out")
        {
            bool zoomIn = id == "view.zoom-in";
            next = current.ZoomAbout(new Avalonia.Point(size.Width / 2, size.Height / 2), zoomIn ? ZoomStep : 1 / ZoomStep, size);
            report = zoomIn ? "Zoomed in." : "Zoomed out.";
        }
        else
        {
            var (minimum, maximum) = id == "view.fit-selection" ? controller.FitBounds()!.Value : WholeBounds(controller.Surface!);
            next = current.Fit(minimum, maximum, size);
            report = id == "view.fit-selection" && controller.Selection is Selection.Station ? "Fit to the selected station." : "Fit.";
        }
        SetCamera(controller, target, next);
        if (report is not null) context.Report(new StatusReport(report));
    }

    /// <summary>The Plan keeps M1.2b's camera: pan by pixels, zoom by scale, Fit resets (Fit Selection fits all there).</summary>
    private static void RunOnPlan(string id, WorkbenchController controller, Avalonia.Size size, Action<StatusReport> report)
    {
        var camera = controller.PlanCamera;
        if (Pans.TryGetValue(id, out var step))
        {
            controller.PlanCamera = camera with
            {
                PanSpanPixels = camera.PanSpanPixels + step.X * size.Width * PanFraction,
                PanAftPixels = camera.PanAftPixels + step.Y * size.Height * PanFraction
            };
            return;
        }
        switch (id)
        {
            case "view.zoom-in":
                controller.PlanCamera = camera with { PixelsPerMeter = camera.PixelsPerMeter * ZoomStep };
                report(new StatusReport("Zoomed in."));
                return;
            case "view.zoom-out":
                controller.PlanCamera = camera with { PixelsPerMeter = Math.Max(50, camera.PixelsPerMeter / ZoomStep) };
                report(new StatusReport("Zoomed out."));
                return;
            default:
                controller.PlanCamera = camera with { PixelsPerMeter = 1000, PanSpanPixels = 0, PanAftPixels = 0 };
                report(new StatusReport("Fit."));
                return;
        }
    }

    /// <summary>The focused model view, else the target view, else (when the target is not shown) the view the layout shows first.</summary>
    internal static SingleView Target(WorkbenchController controller, SingleView? focused)
    {
        var layout = controller.Layout;
        foreach (var candidate in new[] { focused, controller.TargetView })
            if (candidate is { } view && layout.Shows(view)) return view;
        return layout.Arrangement == ViewArrangement.One ? layout.Single : SingleView.Plan;
    }

    /// <summary>Display ▾ acts on the target view; the Plan has one display, so then it acts on the 3D view.</summary>
    private static SingleView DisplayTarget(WorkbenchController controller) =>
        controller.TargetView == SingleView.Plan ? SingleView.ThreeD : controller.TargetView;

    private static ViewCamera CameraOf(WorkbenchController controller, SingleView view, Avalonia.Size size)
    {
        var current = view == SingleView.ThreeD ? controller.Camera3d : controller.CameraFor(view);
        if (current is { } camera) return camera;
        var (minimum, maximum) = WholeBounds(controller.Surface!);
        return ViewCamera.Named(view switch { SingleView.Side => NamedCamera.Side, SingleView.Front => NamedCamera.Front, _ => NamedCamera.Iso },
            minimum, maximum, size);
    }

    private static void SetCamera(WorkbenchController controller, SingleView view, ViewCamera camera)
    {
        if (view == SingleView.ThreeD) controller.Camera3d = camera;
        else controller.SetCameraFor(view, camera);
    }

    /// <summary>Both halves: the port half is the starboard mesh mirrored in y (FoilDSL §5.2).</summary>
    private static (Point3 Minimum, Point3 Maximum) WholeBounds(SurfaceView surface) =>
        (new Point3(surface.MinimumX, -surface.MaximumY, surface.MinimumZ), new Point3(surface.MaximumX, surface.MaximumY, surface.MaximumZ));

    private static string Name(SingleView view) => view switch
    {
        SingleView.ThreeD => "3D",
        SingleView.Side => "Side",
        SingleView.Front => "Front",
        _ => "Plan"
    };
}
