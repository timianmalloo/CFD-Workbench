using CfdWorkbench.Desktop;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using Avalonia.Input;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Color = Avalonia.Media.Color;
using ISolidColorBrush = Avalonia.Media.ISolidColorBrush;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using System.Text;
using System.Text.Json.Nodes;

if (args.Contains("--section-canvas", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    CfdWorkbench.Desktop.Tests.SectionCanvasTests.Run();
    Console.WriteLine("SectionCanvas tests passed.");
    Environment.Exit(0);
}

if (args.Contains("--theme-controls", StringComparer.Ordinal) ||
    args.Contains("--theme-pointer-red", StringComparer.Ordinal) ||
    args.Contains("--numeric-paint-red", StringComparer.Ordinal) ||
    args.Contains("--focus-diagnostic", StringComparer.Ordinal) ||
    args.Contains("--focus-negatives", StringComparer.Ordinal) ||
    args.Contains("--focus-readiness", StringComparer.Ordinal) ||
    args.Contains("--closed-callback-repro", StringComparer.Ordinal))
{
    bool focusDiagnostic = args.Contains("--focus-diagnostic", StringComparer.Ordinal);
    bool pointerRed = args.Contains("--theme-pointer-red", StringComparer.Ordinal);
    bool numericPaintRed = args.Contains("--numeric-paint-red", StringComparer.Ordinal);
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    foreach (var (key, value) in new Dictionary<string, string> {
        ["CFDW_REVIEW_MODE"] = "1", ["CFDW_REVIEW_PERSONA"] = "designer",
        ["CFDW_REVIEW_STATE"] = "empty", ["CFDW_REVIEW_THEME"] = "light",
        ["CFDW_REVIEW_SIZE"] = "1024x700" })
        Environment.SetEnvironmentVariable(key, value);
    var originalRows = new[] { "toolbar.enabled", "tab.section.selected", "tab.source.selected",
        "source.active.readonly", "station.selected", "station.focused", "cv.selected", "cv.focused",
        "numeric.enabled.owned-draft", "modal.body", "modal.save", "modal.discard", "modal.cancel",
        "viewport.annotation", "section.annotation", "focus.toolbar", "focus.tab", "focus.numeric" };
    var interactionRows = new List<string>();
    foreach (string tab in new[] { "section", "source" })
        foreach (string selection in new[] { "selected", "unselected" })
            foreach (string state in new[] { "rest", "hover", "pressed", "returned", "focus-hover" })
                interactionRows.Add($"interaction.tab.{tab}.{selection}.{state}");
    foreach (string state in new[] { "rest", "hover", "pressed", "returned" })
    {
        interactionRows.Add($"interaction.toolbar.example.{state}");
        foreach (string button in new[] { "save", "discard", "cancel" })
            interactionRows.Add($"interaction.modal.{button}.{state}");
        foreach (string kind in new[] { "station", "cv" })
            foreach (string selection in new[] { "selected", "unselected" })
                interactionRows.Add($"interaction.{kind}.{selection}.{state}");
    }
    foreach (string state in new[] { "rest", "hover", "returned", "focus-hover" })
        foreach (string field in new[] { "numeric.owned", "source.readonly" })
            interactionRows.Add($"interaction.{field}.{state}");
    var textBoxRows = new[] { "unfocused-rest", "unfocused-hover", "keyboard-focus",
        "all-selected", "keyboard-focus-hover", "keyboard-focus-returned", "blurred",
        "pointer-focus-hover", "pointer-focus-returned", "refocused" }
        .SelectMany(state => new[] { "textbox.numeric." + state, "textbox.source." + state })
        .Append("textbox.numeric.typed-replacement").ToArray();
    var required = originalRows.Concat(interactionRows).Concat(textBoxRows).ToArray();
    if (required.Length != 99 || required.Distinct(StringComparer.Ordinal).Count() != 99)
        throw new Exception("Frozen per-theme 18+60+21 TextBox row table changed");
    var emitted = new HashSet<string>(StringComparer.Ordinal);
    var rowFailures = new List<string>();
    void CheckRow(string theme, string row, Action probe)
    {
        try { probe(); }
        catch (Exception error)
        {
            rowFailures.Add($"{theme}/{row}: {error.Message}");
            Console.WriteLine($"THEME-ROW-FAIL {theme}/{row} {error.GetType().Name}: {error.Message}");
        }
    }
    static Color Solid(Avalonia.Media.IBrush? brush, string label)
    {
        if (brush is not ISolidColorBrush solid || solid.Color.A != 255 ||
            Math.Abs(brush.Opacity - 1) > 0.000001)
            throw new Exception($"Applied {label} has unresolved/nonopaque brush");
        return solid.Color;
    }
    static Avalonia.Media.IBrush? PropertyBrush(object value, string name) =>
        value.GetType().GetProperty(name)?.GetValue(value) as Avalonia.Media.IBrush;
    static Avalonia.Visual TextVisual(Control target, bool textBox = false)
    {
        if (target is TextBlock block && block.Text?.Length > 0 && block.Bounds.Width > 0 && block.Bounds.Height > 0)
            return block;
        string[] preferred = textBox ? ["TextPresenter"] : ["AccessText", "TextBlock"];
        return target.GetVisualDescendants().FirstOrDefault(item =>
            preferred.Contains(item.GetType().Name, StringComparer.Ordinal) &&
            PropertyBrush(item, "Foreground") is not null && item.Bounds.Width > 0 && item.Bounds.Height > 0)
            ?? throw new Exception($"Rendered text presenter absent for {target.Name ?? target.GetType().Name}");
    }
    static Color Backing(Avalonia.Visual text, Avalonia.Visual? observedAt = null, Rect? observedBounds = null)
    {
        observedAt ??= text;
        var visibleBounds = observedBounds ?? new Rect(text.Bounds.Size);
        Color? backing = null;
        foreach (var layer in text.GetVisualAncestors().Reverse().Append(text))
        {
            if (Math.Abs(layer.Opacity - 1) > 0.000001)
                throw new Exception($"Unknown group opacity on {layer.GetType().Name}");
            if (PropertyBrush(layer, "Background") is not { } paint) continue;
            if (paint is not ISolidColorBrush solid)
                throw new Exception($"Unknown background paint on {layer.GetType().Name}");
            if (Math.Abs(paint.Opacity - 1) > 0.000001)
                throw new Exception($"Unknown brush opacity on {layer.GetType().Name}");
            if (solid.Color.A == 0) continue;
            if (solid.Color.A != 255)
                throw new Exception($"Partial alpha on {layer.GetType().Name}");
            if (layer.Bounds.Width <= 0 || layer.Bounds.Height <= 0)
                throw new Exception($"Painted background has no bounds on {layer.GetType().Name}");
            var transform = observedAt.TransformToVisual(layer);
            if (transform is null || Math.Abs(transform.Value.M11 - 1) > 0.000001 ||
                Math.Abs(transform.Value.M22 - 1) > 0.000001 ||
                Math.Abs(transform.Value.M12) > 0.000001 || Math.Abs(transform.Value.M21) > 0.000001)
                throw new Exception($"Unsupported non-translation paint transform on {layer.GetType().Name}");
            var origin = observedAt.TranslatePoint(visibleBounds.Position, layer);
            if (origin is null || origin.Value.X < 0 || origin.Value.Y < 0 ||
                origin.Value.X + visibleBounds.Width > layer.Bounds.Width + .01 ||
                origin.Value.Y + visibleBounds.Height > layer.Bounds.Height + .01)
                throw new Exception($"Painted background does not enclose text on {layer.GetType().Name}");
            backing = solid.Color;
        }
        return backing ?? throw new Exception("No proven opaque backing for rendered text");
    }
    static Color TextBoxBacking(TextBox target, Avalonia.Visual text,
        Avalonia.Visual observedAt, Rect observedBounds)
    {
        var border = target.GetVisualDescendants().OfType<Border>()
            .Single(item => item.Name == "PART_BorderElement");
        var panel = border.GetVisualParent() as Panel
            ?? throw new Exception("TextBox painter sibling panel absent");
        var host = text.GetVisualAncestors().FirstOrDefault(item =>
            ReferenceEquals(item.GetVisualParent(), panel))
            ?? throw new Exception("TextBox text-host sibling absent");
        if (host is not Control hostControl)
            throw new Exception("TextBox text-host is not a control");
        if (!ReferenceEquals(border.TemplatedParent, target) ||
            panel.Children.IndexOf(border) != 0 || panel.Children.IndexOf(hostControl) != 1 ||
            !border.IsEffectivelyVisible || !host.IsEffectivelyVisible)
            throw new Exception("TextBox painted sibling template or order changed");
        foreach (var layer in text.GetVisualAncestors().TakeWhile(item => !ReferenceEquals(item, panel))
                     .Append(text).Append(border).Append(panel))
        {
            if (Math.Abs(layer.Opacity - 1) > 0.000001)
                throw new Exception("TextBox painter group opacity is unresolved");
            if (layer.Clip is not null && layer is not Avalonia.Controls.Presenters.ScrollContentPresenter)
                throw new Exception("TextBox painter has an unsupported clip");
            if (ReferenceEquals(layer, border) || ReferenceEquals(layer, panel)) continue;
            if (PropertyBrush(layer, "Background") is { } foregroundPaint &&
                foregroundPaint is ISolidColorBrush paint && paint.Color.A != 0)
                throw new Exception("TextBox text-host overlays its sibling painter");
            if (PropertyBrush(layer, "Background") is { } unknownPaint &&
                (unknownPaint is not ISolidColorBrush || Math.Abs(unknownPaint.Opacity - 1) > 0.000001))
                throw new Exception("TextBox text-host paint is unresolved");
        }
        var transform = observedAt.TransformToVisual(border);
        var origin = observedAt.TranslatePoint(observedBounds.Position, border);
        if (transform is null || origin is null ||
            Math.Abs(transform.Value.M11 - 1) > 0.000001 ||
            Math.Abs(transform.Value.M22 - 1) > 0.000001 ||
            Math.Abs(transform.Value.M12) > 0.000001 ||
            Math.Abs(transform.Value.M21) > 0.000001 ||
            origin.Value.X < 0 || origin.Value.Y < 0 ||
            origin.Value.X + observedBounds.Width > border.Bounds.Width + .01 ||
            origin.Value.Y + observedBounds.Height > border.Bounds.Height + .01)
            throw new Exception("TextBox sibling painter does not enclose visible text");
        _ = Backing(text, observedAt, observedBounds); // Prove the opaque underlay too.
        return Solid(border.Background, "TextBox PART_BorderElement backdrop");
    }
    static double Luminance(Color color)
    {
        static double Linear(byte channel) { double value = channel / 255.0; return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4); }
        return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
    }
    static double Contrast(Color first, Color second)
    {
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
    static T RequiredCompositionProperty<T>(object visual, string name)
    {
        var value = visual.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic)?.GetValue(visual);
        return value is T typed ? typed : throw new Exception($"Composition {name} unavailable or wrong type");
    }
    static bool FocusPlacementValid(FocusPlacementFacts facts)
    {
        const double tolerance = 0.000001;
        bool Near(double a, double b) => Math.Abs(a - b) <= tolerance;
        bool Encloses(Rect clip, Rect extent) =>
            clip.X <= extent.X + tolerance && clip.Y <= extent.Y + tolerance &&
            clip.Right + tolerance >= extent.Right && clip.Bottom + tolerance >= extent.Bottom;
        var target = facts.TargetBounds;
        var outer = facts.OuterBounds;
        var inner = facts.InnerBounds;
        return facts.AdornedLink && facts.CompositionLink && facts.AdornerClipped &&
            target.Width > 4 && target.Height > 4 &&
            Near(target.X, 0) && Near(target.Y, 0) &&
            Near(outer.X, 0) && Near(outer.Y, 0) &&
            Near(outer.Width, target.Width) && Near(outer.Height, target.Height) &&
            Near(inner.X, 2) && Near(inner.Y, 2) &&
            Near(inner.Width, outer.Width - 4) && Near(inner.Height, outer.Height - 4) &&
            facts.AdornerClip is { } adornerClip && Encloses(adornerClip, outer) &&
            facts.OuterClip is { } outerClip && Encloses(outerClip, outer) &&
            Near(facts.CompositionSize.X, target.Width) &&
            Near(facts.CompositionSize.Y, target.Height) &&
            facts.CompositionOffset.X == 0 && facts.CompositionOffset.Y == 0 &&
            facts.CompositionOffset.Z == 0 &&
            facts.CompositionScale.X == 1 && facts.CompositionScale.Y == 1 &&
            facts.CompositionScale.Z == 1 &&
            facts.CompositionRotation == 0 &&
            facts.CompositionOrientation == System.Numerics.Quaternion.Identity &&
            facts.CompositionAnchor.X == 0 && facts.CompositionAnchor.Y == 0 &&
            facts.CompositionCenter.X == 0 && facts.CompositionCenter.Y == 0 &&
            facts.CompositionCenter.Z == 0;
    }
    static bool ReadinessVerdict(object? candidateBatch, object? expectedBatch,
        bool rendered, bool current, bool cancelled, Vector targetSize,
        Vector adornerSize, Rect expectedBounds) =>
        candidateBatch is not null && ReferenceEquals(candidateBatch, expectedBatch) &&
        rendered && current && !cancelled &&
        targetSize.X == expectedBounds.Width && targetSize.Y == expectedBounds.Height &&
        adornerSize.X == expectedBounds.Width && adornerSize.Y == expectedBounds.Height;
    if (args.Contains("--closed-callback-repro", StringComparer.Ordinal))
    {
        var first = new MainWindow();
        var second = new MainWindow();
        static WorkbenchController Controller(MainWindow window) =>
            (WorkbenchController)(typeof(MainWindow).GetField("workbench",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(window) ?? throw new Exception("Repro controller unavailable"));
        var firstController = Controller(first);
        var secondController = Controller(second);
        bool firstClosed = false;
        first.Closed += (_, _) => firstClosed = true;
        string? stack = null;
        void OnUnhandled(object? _, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs eventArgs)
        {
            stack = eventArgs.Exception.ToString();
            eventArgs.Handled = true;
        }
        static void Drain()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Avalonia.Threading.Dispatcher.UIThread.Post(timeout.Cancel, Avalonia.Threading.DispatcherPriority.Background);
            Avalonia.Threading.Dispatcher.UIThread.MainLoop(timeout.Token);
        }
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnUnhandled;
        try
        {
            first.Show();
            second.Show();
            Task.Run(() => firstController.OpenExampleAsync()).GetAwaiter().GetResult();
            Console.WriteLine("CLOSED-CALLBACK before " + System.Text.Json.JsonSerializer.Serialize(new {
                firstWindow = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(first),
                firstController = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(firstController),
                firstAccepted = firstController.Inspection?.Authored.Binding.AcceptedId,
                firstSource = firstController.Inspection?.Authored.Binding.SourceHash,
                secondWindow = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(second),
                secondController = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(secondController),
                secondAccepted = secondController.Inspection?.Authored.Binding.AcceptedId,
                firstClosed }));
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!.SetValue(first, true);
            first.Close();
            Console.WriteLine("CLOSED-CALLBACK after-close " + System.Text.Json.JsonSerializer.Serialize(new {
                firstClosed, secondOpen = second.IsVisible,
                firstController = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(firstController),
                secondController = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(secondController) }));
            Drain();
            Console.WriteLine("CLOSED-CALLBACK observed " + System.Text.Json.JsonSerializer.Serialize(new {
                firstClosed, secondOpen = second.IsVisible,
                exception = stack ?? "not-recorded" }));
            if (stack is not null || !firstClosed || !second.IsVisible)
                throw new Exception("Closed window callback escaped or next window closed");
            int changed = 0;
            secondController.Changed += () => changed++;
            Task.Run(() => secondController.OpenExampleAsync()).GetAwaiter().GetResult();
            Drain();
            if (stack is not null || changed == 0 || secondController.Inspection is null)
                throw new Exception("Next window did not publish its accepted update");
            var accepted = secondController.Inspection.Authored.Binding.AcceptedId;
            var editable = secondController.Inspection.Authored.Rails.Single(r => r.Name == "leading")
                .Controls.First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
            secondController.BeginEdit("leading", editable.Id);
            Drain();
            second.Close();
            var modal = second.OwnedWindows.SingleOrDefault()
                ?? throw new Exception("Unsaved close did not offer safe modal");
            modal.Close(); // Default result is Cancel, including window-manager close.
            Drain();
            if (stack is not null || !second.IsVisible || secondController.Draft is null ||
                secondController.Inspection.Authored.Binding.AcceptedId != accepted)
                throw new Exception("Unsaved close Cancel did not retain live accepted/draft state");
            secondController.UpdateDraft(editable.OrdinateSi + .005);
            Drain();
            if (stack is not null || secondController.Draft is null)
                throw new Exception("Cancelled-close window did not accept a later draft update");
            Console.WriteLine("CLOSED-CALLBACK green " + System.Text.Json.JsonSerializer.Serialize(new {
                firstClosed, secondOpen = second.IsVisible, nextWindowChanges = changed,
                nextAccepted = accepted, cancelledCloseDraftLive = true, exception = "none" }));
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!.SetValue(second, true);
            second.Close();
            Drain();
            if (stack is not null) throw new Exception("Closed-window callback escaped after cleanup", new Exception(stack));
        }
        finally { Avalonia.Threading.Dispatcher.UIThread.UnhandledException -= OnUnhandled; }
        Environment.Exit(0);
    }
    if (args.Contains("--focus-negatives", StringComparer.Ordinal))
    {
        var baseline = new FocusPlacementFacts(true, true, true,
            new Rect(0, 0, 189, 48), new Rect(0, 0, 189, 48),
            new Rect(2, 2, 185, 44), new Rect(0, 0, 189, 48),
            new Rect(0, 0, 189, 48), new Vector(189, 48),
            new Vector3D(0, 0, 0), new Vector3D(1, 1, 1), 0,
            System.Numerics.Quaternion.Identity, new Vector(0, 0),
            new Vector3D(0, 0, 0));
        if (!FocusPlacementValid(baseline) ||
            FocusPlacementValid(baseline with { CompositionLink = false }) ||
            FocusPlacementValid(baseline with { OuterBounds = new Rect(1, 0, 189, 48) }) ||
            FocusPlacementValid(baseline with { OuterClip = new Rect(1, 0, 188, 48) }))
            throw new Exception("Focus placement wrong-target/displaced/clipped negative controls failed");
        Console.WriteLine("FOCUS-NEGATIVES baseline=true wrong-target=false displaced=false clipped=false");
        Environment.Exit(0);
    }
    if (args.Contains("--focus-readiness", StringComparer.Ordinal))
    {
        Environment.SetEnvironmentVariable("CFDW_REVIEW_STATE", "example");
        var readinessWindow = new MainWindow { RequestedThemeVariant = ThemeVariant.Light };
        var readinessController = (WorkbenchController)(typeof(MainWindow).GetField("workbench",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(readinessWindow) ?? throw new Exception("Readiness controller unavailable"));
        var readinessTabs = readinessWindow.FindControl<TabControl>("DocumentTabs")
            ?? throw new Exception("Readiness tabs unavailable");
        var readinessTab = readinessTabs.Items.OfType<TabItem>().First();
        using var readinessTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        string readinessResult = "timeout";
        int readinessSettled = 0;
        using var timeoutSettlement = readinessTimeout.Token.Register(() =>
        {
            if (Interlocked.CompareExchange(ref readinessSettled, 2, 0) == 0)
                readinessResult = "timeout";
        });
        int readinessScheduled = 0;
        object? earlyBatch = null;
        string? acceptedId = null;
        string? sourceHash = null;
        DisplayFrame? acceptedFrame = null;
        long expectedRevision = -1;
        Avalonia.Rendering.Composition.CompositionVisual? targetComposition = null;
        Avalonia.Rendering.Composition.CompositionVisual? adornerComposition = null;
        Avalonia.Rendering.Composition.Compositor? compositor = null;
        readinessController.Changed += () =>
        {
            var inspected = readinessController.Inspection;
            var frame = readinessController.Frame;
            if (inspected?.Authored.Binding.AcceptedId is not { } id || frame is null ||
                frame.SourceHash != inspected.Authored.Binding.SourceHash ||
                readinessController.Draft is not null ||
                Interlocked.Exchange(ref readinessScheduled, 1) != 0) return;
            acceptedId = id;
            sourceHash = inspected.Authored.Binding.SourceHash;
            acceptedFrame = frame;
            Avalonia.Threading.Dispatcher.UIThread.Post(BeginReadiness,
                Avalonia.Threading.DispatcherPriority.Background);
        };
        readinessWindow.Show();
        readinessTabs.SelectedIndex = 0;
        readinessWindow.UpdateLayout();
        readinessTab.Focus(NavigationMethod.Tab);
        readinessWindow.UpdateLayout();
        var beforeLayer = AdornerLayer.GetAdornerLayer(readinessTab);
        var beforeAdorner = beforeLayer?.Children.OfType<Control>().SingleOrDefault(child =>
            ReferenceEquals(AdornerLayer.GetAdornedElement(child), readinessTab));
        targetComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(readinessTab);
        adornerComposition = beforeAdorner is null ? null :
            Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(beforeAdorner);
        compositor = targetComposition?.Compositor;
        if (compositor is null) throw new Exception("Readiness target compositor unavailable");
        Console.WriteLine("FOCUS-READINESS before " + System.Text.Json.JsonSerializer.Serialize(new {
            targetSize = targetComposition?.Size.ToString() ?? "null",
            adornerSize = adornerComposition?.Size.ToString() ?? "null",
            focused = readinessTab.IsFocused,
            accepted = readinessController.Inspection?.Authored.Binding.AcceptedId ?? "null",
            frame = readinessController.Frame?.SourceHash ?? "null" }));
        earlyBatch = compositor.RequestCompositionBatchCommitAsync();
        void BeginReadiness()
        {
            try
            {
                readinessWindow.UpdateLayout();
                var viewport = readinessWindow.FindControl<Viewport>("FoilViewport")
                    ?? throw new Exception("Readiness viewport unavailable");
                if (acceptedFrame is null || !ReferenceEquals(viewport.Frame, acceptedFrame) ||
                    readinessController.Inspection?.Authored.Binding.AcceptedId != acceptedId ||
                    readinessController.Inspection?.Authored.Binding.SourceHash != sourceHash ||
                    readinessController.Draft is not null)
                    throw new Exception("Opened fixture not bound to accepted viewport before barrier");
                expectedRevision = viewport.FrameRevision;
                var batch = (compositor ?? throw new Exception("Readiness compositor lost"))
                    .RequestCompositionBatchCommitAsync();
                if (ReferenceEquals(batch, earlyBatch))
                    throw new Exception("Stale pre-fixture batch reused as fresh barrier");
                Console.WriteLine("FOCUS-READINESS barrier " + System.Text.Json.JsonSerializer.Serialize(new {
                    acceptedId, sourceHash, draft = "none", revision = expectedRevision,
                    earlyBatch = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(earlyBatch!),
                    freshBatch = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(batch),
                    targetSize = targetComposition?.Size.ToString() ?? "null",
                    adornerSize = adornerComposition?.Size.ToString() ?? "null" }));
                _ = batch.Rendered.ContinueWith(completion => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (Interlocked.CompareExchange(ref readinessSettled, 1, 0) != 0) return;
                    try
                    {
                        var stillCurrent = acceptedFrame is not null &&
                            ReferenceEquals(viewport.Frame, acceptedFrame) &&
                            viewport.FrameRevision == expectedRevision &&
                            readinessController.Inspection?.Authored.Binding.AcceptedId == acceptedId &&
                            readinessController.Inspection?.Authored.Binding.SourceHash == sourceHash &&
                            readinessController.Frame?.SourceHash == sourceHash &&
                            readinessController.Draft is null && readinessTab.IsFocused;
                        var actual = ReadinessVerdict(batch, batch, completion.IsCompletedSuccessfully,
                            stillCurrent, readinessTimeout.IsCancellationRequested,
                            targetComposition?.Size ?? default, adornerComposition?.Size ?? default,
                            readinessTab.Bounds);
                        var staleRejected = !ReadinessVerdict(earlyBatch, batch, true, stillCurrent, false,
                            targetComposition?.Size ?? default, adornerComposition?.Size ?? default,
                            readinessTab.Bounds);
                        var cancelledRejected = !ReadinessVerdict(batch, batch, true, stillCurrent, true,
                            targetComposition?.Size ?? default, adornerComposition?.Size ?? default,
                            readinessTab.Bounds);
                        int cancelledSettlement = 2;
                        bool lateCallbackAccepted = ReadinessVerdict(batch, batch, true, stillCurrent, false,
                            targetComposition?.Size ?? default, adornerComposition?.Size ?? default,
                            readinessTab.Bounds) &&
                            Interlocked.CompareExchange(ref cancelledSettlement, 1, 0) == 0;
                        Console.WriteLine("FOCUS-READINESS after " + System.Text.Json.JsonSerializer.Serialize(new {
                            renderedBatch = completion.IsCompletedSuccessfully,
                            actual, staleRejected, cancelledRejected, lateCallbackAccepted,
                            stillCurrent, acceptedId, sourceHash,
                            revision = viewport.FrameRevision,
                            targetSize = targetComposition?.Size.ToString() ?? "null",
                            adornerSize = adornerComposition?.Size.ToString() ?? "null" }));
                        readinessResult = actual && staleRejected && cancelledRejected &&
                            !lateCallbackAccepted ? "ready" : "not-assessed";
                    }
                    catch (Exception error) { readinessResult = "error:" + error.Message; }
                    finally { readinessTimeout.Cancel(); }
                }, Avalonia.Threading.DispatcherPriority.Render));
            }
            catch (Exception error) { readinessResult = "error:" + error.Message; readinessTimeout.Cancel(); }
        }
        Avalonia.Threading.Dispatcher.UIThread.MainLoop(readinessTimeout.Token);
        Console.WriteLine("FOCUS-READINESS result=" + readinessResult);
        typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(readinessWindow, true);
        readinessWindow.Close();
        if (readinessResult != "ready") throw new Exception("Focus composition readiness not established: " + readinessResult);
        Environment.Exit(0);
    }
    void TextRow(string theme, string name, Control target, bool textBox = false)
    {
        var visual = TextVisual(target, textBox);
        if (!target.IsEffectivelyVisible || !visual.IsEffectivelyVisible ||
            visual.Bounds.Width <= 0 || visual.Bounds.Height <= 0)
            throw new Exception($"Required applied row is hidden or unbounded: {theme}/{name}");
        var foreground = Solid(PropertyBrush(visual, "Foreground"), $"{theme}/{name} ink");
        Avalonia.Visual observedAt = visual;
        Rect observedBounds = new Rect(visual.Bounds.Size);
        string clipEvidence = "";
        if (name == "source.active.readonly" ||
            name.StartsWith("interaction.source.readonly.", StringComparison.Ordinal) ||
            name.StartsWith("textbox.source.", StringComparison.Ordinal))
        {
            var clip = visual.GetVisualAncestors().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>()
                .FirstOrDefault() ?? throw new Exception("Source text has no scroll viewport");
            if (!clip.ClipToBounds) throw new Exception("Source text scroll viewport does not clip");
            var origin = visual.TranslatePoint(new Point(0, 0), clip)
                ?? throw new Exception("Source text/viewport transform unavailable");
            double left = Math.Max(0, origin.X), top = Math.Max(0, origin.Y);
            double right = Math.Min(clip.Bounds.Width, origin.X + visual.Bounds.Width);
            double bottom = Math.Min(clip.Bounds.Height, origin.Y + visual.Bounds.Height);
            if (right <= left || bottom <= top)
                throw new Exception("Source text has no visible clipped region");
            observedAt = clip;
            observedBounds = new Rect(left, top, right - left, bottom - top);
            clipEvidence = " clip=scroll";
        }
        var background = textBox && target is TextBox field
            ? TextBoxBacking(field, visual, observedAt, observedBounds)
            : Backing(visual, observedAt, observedBounds);
        double ratio = Contrast(foreground, background);
        if (ratio < 4.5) throw new Exception($"Applied text contrast {theme}/{name} {ratio:F3} < 4.5");
        if (!emitted.Add(theme + "/" + name)) throw new Exception($"Duplicate applied row {theme}/{name}");
        Console.WriteLine($"THEME-APPLIED {theme}/{name} fg=#{foreground.A:X2}{foreground.R:X2}{foreground.G:X2}{foreground.B:X2} " +
            $"bg=#{background.A:X2}{background.R:X2}{background.G:X2}{background.B:X2} ratio={ratio:F6} " +
            $"text={visual.GetType().Name} bounds={observedBounds.Width:F1}x{observedBounds.Height:F1}{clipEvidence}" +
            (textBox ? " painter=PART_BorderElement" : ""));
    }
    static IPseudoClasses StateClasses(Control target) =>
        typeof(StyledElement).GetProperty("PseudoClasses",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(target) as IPseudoClasses
        ?? throw new Exception("Installed protected IPseudoClasses unavailable");
    static Point PointerOrigin(Control target, Window window) =>
        target.TranslatePoint(new Point(10, 10), window)
        ?? throw new Exception("Pointer target/window transform unavailable");
    static void Enter(Control target, Window window, Pointer pointer, ulong stamp)
    {
        target.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent,
            target, pointer, window, PointerOrigin(target, window), stamp, default, KeyModifiers.None));
        if (!target.IsPointerOver || !StateClasses(target).Contains(":pointerover"))
            throw new Exception("Framework pointer-enter transition was not observed");
    }
    static void Press(Control target, Window window, Pointer pointer, ulong stamp)
    {
        target.RaiseEvent(new PointerPressedEventArgs(target, pointer, window,
            PointerOrigin(target, window), stamp,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        if (!StateClasses(target).Contains(":pressed"))
            throw new Exception("Framework pointer-pressed transition was not observed");
    }
    static void Return(Control target, Window window, Pointer pointer, ulong stamp)
    {
        // Release outside the target to avoid triggering an action during a color probe.
        target.RaiseEvent(new PointerReleasedEventArgs(target, pointer, window,
            new Point(-100, -100), stamp,
            new PointerPointProperties(RawInputModifiers.None,
                PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
            target, pointer, window, new Point(-100, -100), stamp + 1, default, KeyModifiers.None));
        if (target.IsPointerOver || StateClasses(target).Contains(":pointerover") ||
            StateClasses(target).Contains(":pressed"))
            throw new Exception("Framework pointer return-to-rest transition was not observed");
    }
    void InteractionRow(string theme, string name, Control target, string selection,
        string state, string route = "framework", bool textBox = false)
    {
        var pseudo = StateClasses(target);
        var visual = TextVisual(target, textBox);
        var foreground = PropertyBrush(visual, "Foreground");
        string ColorText(Avalonia.Media.IBrush? paint) => paint is ISolidColorBrush solid
            ? $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}"
            : "unresolved";
        string backdrop;
        try
        {
            Color color;
            if (name.StartsWith("interaction.source.readonly.", StringComparison.Ordinal))
            {
                var clip = visual.GetVisualAncestors().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>()
                    .FirstOrDefault() ?? throw new Exception("Source text scroll clip absent");
                if (!clip.ClipToBounds) throw new Exception("Source text clip disabled");
                var origin = visual.TranslatePoint(new Point(0, 0), clip)
                    ?? throw new Exception("Source text clip transform absent");
                double left = Math.Max(0, origin.X), top = Math.Max(0, origin.Y);
                double right = Math.Min(clip.Bounds.Width, origin.X + visual.Bounds.Width);
                double bottom = Math.Min(clip.Bounds.Height, origin.Y + visual.Bounds.Height);
                if (right <= left || bottom <= top) throw new Exception("Source text clip empty");
                color = target is TextBox sourceField
                    ? TextBoxBacking(sourceField, visual, clip,
                        new Rect(left, top, right - left, bottom - top))
                    : Backing(visual, clip, new Rect(left, top, right - left, bottom - top));
            }
            else color = target is TextBox numericField
                ? TextBoxBacking(numericField, visual, visual, new Rect(visual.Bounds.Size))
                : Backing(visual);
            backdrop = ColorText(new SolidColorBrush(color));
        }
        catch (Exception error) { backdrop = "unresolved:" + error.GetType().Name; }
        var selected = target switch
        {
            TabItem tab => tab.IsSelected ? "selected" : "unselected",
            ListBoxItem item => item.IsSelected ? "selected" : "unselected",
            _ => "na"
        };
        var raw = new {
            theme, name, selection = selected, expectedSelection = selection, state, route,
            enabled = target.IsEffectivelyEnabled, pointerOver = target.IsPointerOver,
            pressed = pseudo.Contains(":pressed"), focused = target.IsFocused,
            focusVisible = pseudo.Contains(":focus-visible"),
            foregroundType = foreground?.GetType().Name ?? "null",
            foreground = ColorText(foreground), background = backdrop,
            foregroundOpacity = foreground?.Opacity ?? double.NaN,
            text = visual.GetType().Name, textBounds = visual.Bounds.ToString()
        };
        Console.WriteLine("THEME-STATE " + System.Text.Json.JsonSerializer.Serialize(raw));
        bool hover = state is "hover" or "pressed" or "focus-hover";
        bool pressed = state == "pressed";
        bool focus = state == "focus-hover";
        if (!target.IsEffectivelyEnabled || selected != selection ||
            target.IsPointerOver != hover && route == "framework" ||
            pseudo.Contains(":pointerover") != hover || pseudo.Contains(":pressed") != pressed ||
            target.IsFocused != focus && focus || pseudo.Contains(":focus-visible") != focus && focus)
            throw new Exception($"Interaction state mismatch: {theme}/{name}");
        TextRow(theme, name, target, textBox);
    }
    void ProbeStates(string theme, string prefix, Control target, string selection,
        Window window, bool textBox = false)
    {
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        var states = textBox ? new[] { "rest", "hover", "returned", "focus-hover" }
            : prefix.StartsWith("interaction.tab.", StringComparison.Ordinal)
                ? new[] { "rest", "hover", "pressed", "returned", "focus-hover" }
                : new[] { "rest", "hover", "pressed", "returned" };
        foreach (string state in states)
        {
            string row = prefix + "." + state;
            CheckRow(theme, row, () =>
            {
                if (state == "hover") Enter(target, window, pointer, 10);
                else if (state == "pressed" && selection == "unselected" &&
                         target is TabItem or ListBoxItem)
                    StateClasses(target).Add(":pressed"); // Transient style state; a real press selects this item.
                else if (state == "pressed") Press(target, window, pointer, 11);
                else if (state == "returned")
                {
                    if (selection == "unselected" && target is TabItem or ListBoxItem)
                    {
                        StateClasses(target).Remove(":pressed");
                        target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                            target, pointer, window, new Point(-100, -100), 12, default, KeyModifiers.None));
                    }
                    else if (textBox)
                        target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                            target, pointer, window, new Point(-100, -100), 12, default, KeyModifiers.None));
                    else Return(target, window, pointer, 12);
                }
                else if (state == "focus-hover")
                {
                    if (target.IsFocused && target is TabItem)
                    {
                        var otherFocus = window.FindControl<Button>("ExampleButton")
                            ?? throw new Exception("Focus reset button unavailable");
                        if (!otherFocus.Focus(NavigationMethod.Tab))
                            throw new Exception("Interaction focus reset unavailable");
                    }
                    if (!target.Focus(NavigationMethod.Tab) || !target.IsFocused)
                        throw new Exception("Interaction keyboard focus unavailable");
                    Enter(target, window, pointer, 14);
                }
                window.UpdateLayout();
                InteractionRow(theme, row, target, selection, state,
                    state == "pressed" && selection == "unselected" && target is TabItem or ListBoxItem
                        ? "styled" : "framework", textBox);
            });
        }
        if (target.IsPointerOver)
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                target, pointer, window, new Point(-100, -100), 16, default, KeyModifiers.None));
    }
    void ProbeTextBoxStates(string theme, string kind, TextBox field, MainWindow window,
        WorkbenchController controller)
    {
        bool source = kind == "source";
        static string Hex(Color color) =>
            $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        var accepted = controller.Inspection?.Authored.Binding.AcceptedId
            ?? throw new Exception("TextBox state probe lacks accepted Example");
        var draftId = controller.Draft?.Id
            ?? throw new Exception("TextBox state probe lacks owned draft identity");
        long lastGeneration = -1;
        var focusReset = window.FindControl<Button>("ExampleButton")
            ?? throw new Exception("TextBox state focus reset absent");
        void State(string state, bool focused, bool hovered, bool selected, string route)
        {
            string row = $"textbox.{kind}.{state}";
            CheckRow(theme, row, () =>
            {
                window.UpdateLayout();
                var currentDraft = controller.Draft;
                if (!field.IsEffectivelyVisible || !field.IsEffectivelyEnabled ||
                    field.IsReadOnly != source || field.IsFocused != focused ||
                    field.IsPointerOver != hovered ||
                    (field.SelectionStart != field.SelectionEnd) != selected ||
                    string.IsNullOrEmpty(field.Text) ||
                    controller.Inspection?.Authored.Binding.AcceptedId != accepted ||
                    currentDraft is null || currentDraft.Id != draftId ||
                    currentDraft.Generation < lastGeneration)
                    throw new Exception($"TextBox actual state/authority mismatch: {theme}/{row}");
                lastGeneration = currentDraft.Generation;
                TextRow(theme, row, field, textBox: true);
                var border = field.GetVisualDescendants().OfType<Border>()
                    .Single(item => item.Name == "PART_BorderElement");
                var backdrop = Solid(border.Background, row + " backdrop");
                double selectionRatio = double.NaN, caretRatio = double.NaN, focusRatio = double.NaN;
                if (selected)
                {
                    selectionRatio = Contrast(Solid(field.SelectionForegroundBrush, row + " selected ink"),
                        Solid(field.SelectionBrush, row + " selection"));
                    if (selectionRatio < 4.5) throw new Exception($"Selected text contrast {selectionRatio:F3} < 4.5");
                }
                if (focused)
                {
                    if (border.BorderThickness.Left <= 0 || border.BorderThickness.Top <= 0)
                        throw new Exception("Focused TextBox lacks painted border thickness");
                    focusRatio = Contrast(Solid(border.BorderBrush, row + " focus border"), backdrop);
                    if (focusRatio < 3) throw new Exception($"Focused border contrast {focusRatio:F3} < 3");
                    if (!selected)
                    {
                        caretRatio = Contrast(Solid(field.CaretBrush, row + " caret"), backdrop);
                        if (caretRatio < 3) throw new Exception($"Caret contrast {caretRatio:F3} < 3");
                    }
                }
                Console.WriteLine("TEXTBOX-STATE " + System.Text.Json.JsonSerializer.Serialize(new {
                    theme, row, state, route, kind, accepted,
                    draft = controller.Draft?.Id, generation = controller.Draft?.Generation,
                    text = field.Text, focused = field.IsFocused, pointerOver = field.IsPointerOver,
                    selected, selectionStart = field.SelectionStart, selectionEnd = field.SelectionEnd,
                    enabled = field.IsEffectivelyEnabled, readOnly = field.IsReadOnly,
                    foreground = Hex(Solid(PropertyBrush(TextVisual(field, true), "Foreground"), row + " ink")),
                    backdrop = Hex(backdrop), selectionRatio = double.IsFinite(selectionRatio) ? selectionRatio : (double?)null,
                    caretRatio = double.IsFinite(caretRatio) ? caretRatio : (double?)null,
                    focusRatio = double.IsFinite(focusRatio) ? focusRatio : (double?)null,
                    painter = "PART_BorderElement", siblingOrder = "border0-host1"
                }));
            });
        }
        using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        void Exit(ulong stamp) => field.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
            field, pointer, window, new Point(-100, -100), stamp, default, KeyModifiers.None));
        focusReset.Focus(NavigationMethod.Tab);
        State("unfocused-rest", false, false, false, "framework-focus");
        Enter(field, window, pointer, 20);
        State("unfocused-hover", false, true, false, "framework-pointer");
        Exit(21);
        field.Focus(NavigationMethod.Tab);
        field.SelectAll();
        State("keyboard-focus", true, false, true, "framework-keyboard-focus");
        field.SelectAll();
        State("all-selected", true, false, true, "framework-selection");
        if (!source)
        {
            field.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent,
                Source = field, Text = "5" });
            if (field.Text != "5") throw new Exception("TextBox replacement input was not applied");
            State("typed-replacement", true, false, false, "framework-text-input");
        }
        Enter(field, window, pointer, 22);
        State("keyboard-focus-hover", true, true, source, "framework-pointer");
        Exit(23);
        State("keyboard-focus-returned", true, false, source, "framework-pointer");
        focusReset.Focus(NavigationMethod.Tab);
        State("blurred", false, false, false, "framework-focus");
        Enter(field, window, pointer, 24);
        field.Focus(NavigationMethod.Pointer);
        var origin = PointerOrigin(field, window);
        field.RaiseEvent(new PointerPressedEventArgs(field, pointer, window, origin, 25,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        field.RaiseEvent(new PointerReleasedEventArgs(field, pointer, window, origin, 26,
            new PointerPointProperties(RawInputModifiers.None,
                PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        State("pointer-focus-hover", true, true, false, "framework-pointer-plus-focus");
        Exit(27);
        State("pointer-focus-returned", true, false, false, "framework-pointer");
        focusReset.Focus(NavigationMethod.Tab);
        field.Focus(NavigationMethod.Tab);
        field.SelectAll();
        State("refocused", true, false, true, "framework-keyboard-focus");
    }
    void FocusRow(string theme, string name, Control target)
    {
        var painters = target.GetVisualDescendants().OfType<Control>()
            .Where(control => control is Border or Avalonia.Controls.Presenters.ContentPresenter).ToArray();
        var before = painters.ToDictionary(painter => painter,
            painter => (PropertyBrush(painter, "BorderBrush") as ISolidColorBrush)?.Color);
        if (!target.Focus(NavigationMethod.Tab) || !target.IsFocused)
            throw new Exception($"Required keyboard focus unavailable: {theme}/{name}");
        TopLevel.GetTopLevel(target)?.UpdateLayout();
        var painted = target.GetVisualDescendants().OfType<Control>()
            .Where(control => control is Border or Avalonia.Controls.Presenters.ContentPresenter)
            .FirstOrDefault(control =>
                control.IsEffectivelyVisible && control.Bounds.Width > 0 && control.Bounds.Height > 0 &&
                control.GetType().GetProperty("BorderThickness")?.GetValue(control) is Thickness thickness &&
                (thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0) &&
                PropertyBrush(control, "BorderBrush") is ISolidColorBrush brush && brush.Color.A == 255 &&
                (!before.TryGetValue(control, out var prior) || prior != brush.Color));
        bool adornerEvidence = false;
        Control? boundAdorner = null;
        if (painted is null)
        {
            var layer = AdornerLayer.GetAdornerLayer(target);
            boundAdorner = layer?.Children.OfType<Control>().SingleOrDefault(child =>
                ReferenceEquals(AdornerLayer.GetAdornedElement(child), target));
            painted = boundAdorner is null ? null : boundAdorner.GetVisualDescendants().OfType<Control>()
                .Prepend(boundAdorner).FirstOrDefault(control =>
                    (control is Border or Avalonia.Controls.Presenters.ContentPresenter) &&
                    control.IsEffectivelyVisible && control.Bounds.Width > 0 && control.Bounds.Height > 0 &&
                    control.GetType().GetProperty("BorderThickness")?.GetValue(control) is Thickness thickness &&
                    (thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0) &&
                    PropertyBrush(control, "BorderBrush") is ISolidColorBrush brush && brush.Color.A == 255);
            adornerEvidence = painted is not null;
        }
        if (painted is null)
            throw new Exception($"No changed template painter or target-bound focus adorner: {theme}/{name}");
        Color? innerRingColor = null;
        string ringGeometry = "";
        if (adornerEvidence)
        {
            var outer = painted as Border ?? throw new Exception("Focus adorner outer painter is not Border");
            var adornerRoot = boundAdorner ?? throw new Exception("Focus ring is not linked to target");
            var rings = adornerRoot.GetVisualDescendants().OfType<Border>()
                .Prepend(adornerRoot as Border).Where(border => border is not null).Cast<Border>()
                .Where(border => border.BorderBrush is not null).ToArray();
            if (rings.Length < 2 || !ReferenceEquals(rings[0], outer) ||
                rings[0].BorderThickness.Left < 2 || rings[1].BorderThickness.Left < 1)
                throw new Exception("Pinned two-ring focus adorner geometry unavailable");
            foreach (var layer in outer.GetVisualAncestors().Reverse().Append(outer).Concat(new[] { rings[1] }))
            {
                if (Math.Abs(layer.Opacity - 1) > 0.000001)
                    throw new Exception("Focus adorner has unresolved group opacity");
                if (PropertyBrush(layer, "Background") is { } paint &&
                    (paint is not ISolidColorBrush solid ||
                     (solid.Color.A != 0 && solid.Color.A != 255) ||
                     Math.Abs(paint.Opacity - 1) > 0.000001))
                    throw new Exception("Focus adorner has unresolved backdrop paint");
            }
            innerRingColor = Solid(rings[1].BorderBrush, "inner focus ring");
            var targetComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(target)
                ?? throw new Exception("Focused target composition visual absent");
            var adornerComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(adornerRoot)
                ?? throw new Exception("Focus adorner composition visual absent");
            var adornedComposition = RequiredCompositionProperty<object>(adornerComposition, "AdornedVisual");
            var adornerClip = (adornerRoot.Clip as Avalonia.Media.RectangleGeometry)?.Rect;
            var outerClip = (outer.Clip as Avalonia.Media.RectangleGeometry)?.Rect;
            var facts = new FocusPlacementFacts(
                ReferenceEquals(AdornerLayer.GetAdornedElement(adornerRoot), target),
                ReferenceEquals(adornedComposition, targetComposition),
                RequiredCompositionProperty<bool>(adornerComposition, "AdornerIsClipped"),
                target.Bounds, outer.Bounds, rings[1].Bounds, adornerClip, outerClip,
                adornerComposition.Size, adornerComposition.Offset, adornerComposition.Scale,
                adornerComposition.RotationAngle, adornerComposition.Orientation,
                adornerComposition.AnchorPoint, adornerComposition.CenterPoint);
            Console.WriteLine("FOCUS-PLACEMENT " + System.Text.Json.JsonSerializer.Serialize(new {
                theme, name, facts.AdornedLink, facts.CompositionLink, facts.AdornerClipped,
                target = facts.TargetBounds.ToString(), outer = facts.OuterBounds.ToString(),
                inner = facts.InnerBounds.ToString(),
                adornerClip = facts.AdornerClip?.ToString() ?? "null",
                outerClip = facts.OuterClip?.ToString() ?? "null",
                size = facts.CompositionSize.ToString(), offset = facts.CompositionOffset.ToString(),
                scale = facts.CompositionScale.ToString(), rotation = facts.CompositionRotation,
                orientation = facts.CompositionOrientation.ToString(),
                anchor = facts.CompositionAnchor.ToString(), center = facts.CompositionCenter.ToString(),
                outerPaint = outer.BorderBrush?.ToString(), innerPaint = rings[1].BorderBrush?.ToString(),
                backdrop = Backing(target).ToString() }));
            if (!FocusPlacementValid(facts))
                throw new Exception("Target-bound focus ring composition placement or clip unresolved");
            if (FocusPlacementValid(facts with { CompositionLink = false }) ||
                FocusPlacementValid(facts with { OuterBounds = new Rect(1, 0, outer.Bounds.Width, outer.Bounds.Height) }) ||
                FocusPlacementValid(facts with { OuterClip = new Rect(1, 0, outer.Bounds.Width - 1, outer.Bounds.Height) }))
                throw new Exception("Wrong-target, displaced, or clipped focus mutation escaped placement guard");
            var innerOrigin = rings[1].TranslatePoint(new Point(0, 0), outer);
            if (innerOrigin is null || innerOrigin.Value.X < -0.01 || innerOrigin.Value.Y < -0.01 ||
                innerOrigin.Value.X + rings[1].Bounds.Width > outer.Bounds.Width + .01 ||
                innerOrigin.Value.Y + rings[1].Bounds.Height > outer.Bounds.Height + .01)
                throw new Exception("Inner focus ring is not measured within outer ring");
            ringGeometry = $" outer={outer.Bounds.Width:F2}x{outer.Bounds.Height:F2}@composition-target";
        }
        var indicator = Solid(PropertyBrush(painted, "BorderBrush"), $"{theme}/{name} focus indicator");
        var backing = adornerEvidence ? Backing(target) : Backing(painted);
        double ratio = Contrast(indicator, backing);
        if (ratio < 3) throw new Exception($"Focus indicator contrast {theme}/{name} {ratio:F3} < 3");
        if (!emitted.Add(theme + "/" + name)) throw new Exception($"Duplicate focus row {theme}/{name}");
        Console.WriteLine($"THEME-APPLIED {theme}/{name} fg=#{indicator.A:X2}{indicator.R:X2}{indicator.G:X2}{indicator.B:X2} " +
            $"bg=#{backing.A:X2}{backing.R:X2}{backing.G:X2}{backing.B:X2} ratio={ratio:F6} " +
            $"focus={(adornerEvidence ? "target-bound-adorner" : "painted-template-border")}" +
            (innerRingColor is { } inner ? $" inner=#{inner.A:X2}{inner.R:X2}{inner.G:X2}{inner.B:X2} ring=outer2-inner1{ringGeometry}" : ""));
    }
    void ReadyThemeWindow(MainWindow window, WorkbenchController controller, TabItem tab, string theme)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        string result = "timeout";
        int settled = 0;
        using var timeoutSettlement = timeout.Token.Register(() =>
        {
            if (Interlocked.CompareExchange(ref settled, 2, 0) == 0) result = "timeout";
        });
        string? id = null;
        string? source = null;
        DisplayFrame? frame = null;
        object? earlyBatch = null;
        Avalonia.Rendering.Composition.CompositionVisual? targetComposition = null;
        Avalonia.Rendering.Composition.Compositor? compositor = null;
        controller.Changed += () =>
        {
            var inspection = controller.Inspection;
            var currentFrame = controller.Frame;
            if (id is not null || inspection?.Authored.Binding.AcceptedId is not { } accepted ||
                currentFrame is null || currentFrame.SourceHash != inspection.Authored.Binding.SourceHash ||
                controller.Draft is not null) return;
            id = accepted;
            source = inspection.Authored.Binding.SourceHash;
            frame = currentFrame;
            Avalonia.Threading.Dispatcher.UIThread.Post(Begin,
                Avalonia.Threading.DispatcherPriority.Background);
        };
        window.Show();
        window.ApplyTemplate();
        window.Measure(new Size(1024, 700));
        window.Arrange(new Rect(0, 0, 1024, 700));
        targetComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(tab);
        compositor = targetComposition?.Compositor;
        if (compositor is null) throw new Exception($"{theme} target compositor unavailable");
        earlyBatch = compositor.RequestCompositionBatchCommitAsync();
        void Begin()
        {
            try
            {
                window.UpdateLayout();
                var viewport = window.FindControl<Viewport>("FoilViewport")
                    ?? throw new Exception("Actual accepted viewport unavailable");
                if (frame is null || !ReferenceEquals(viewport.Frame, frame) ||
                    controller.Inspection?.Authored.Binding.AcceptedId != id ||
                    controller.Inspection?.Authored.Binding.SourceHash != source ||
                    controller.Draft is not null)
                    throw new Exception("Opened Example frame was not bound before focus barrier");
                if (!tab.Focus(NavigationMethod.Tab) || !tab.IsFocused)
                    throw new Exception("Focus barrier tab did not receive keyboard focus");
                window.UpdateLayout();
                var layer = AdornerLayer.GetAdornerLayer(tab);
                var adorner = layer?.Children.OfType<Control>().SingleOrDefault(child =>
                    ReferenceEquals(AdornerLayer.GetAdornedElement(child), tab));
                var adornerComposition = adorner is null ? null :
                    Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(adorner);
                var revision = viewport.FrameRevision;
                var batch = (compositor ?? throw new Exception("Focus barrier compositor lost"))
                    .RequestCompositionBatchCommitAsync();
                _ = batch.Rendered.ContinueWith(completion => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (Interlocked.CompareExchange(ref settled, 1, 0) != 0) return;
                    try
                    {
                        var current = frame is not null && ReferenceEquals(viewport.Frame, frame) &&
                            viewport.FrameRevision == revision &&
                            controller.Inspection?.Authored.Binding.AcceptedId == id &&
                            controller.Inspection?.Authored.Binding.SourceHash == source &&
                            controller.Frame?.SourceHash == source && controller.Draft is null && tab.IsFocused;
                        var valid = ReadinessVerdict(batch, batch, completion.IsCompletedSuccessfully,
                            current, timeout.IsCancellationRequested, targetComposition?.Size ?? default,
                            adornerComposition?.Size ?? default, tab.Bounds);
                        var staleRefused = !ReadinessVerdict(earlyBatch, batch, true, current, false,
                            targetComposition?.Size ?? default, adornerComposition?.Size ?? default, tab.Bounds);
                        result = valid && staleRefused ? "ready" : "not-assessed";
                        Console.WriteLine("THEME-READY " + System.Text.Json.JsonSerializer.Serialize(new {
                            theme, result, id, source, revision,
                            target = targetComposition?.Size.ToString() ?? "null",
                            adorner = adornerComposition?.Size.ToString() ?? "null",
                            staleRefused, current, rendered = completion.IsCompletedSuccessfully }));
                    }
                    catch (Exception error) { result = "error:" + error.Message; }
                    finally { timeout.Cancel(); }
                }, Avalonia.Threading.DispatcherPriority.Render));
            }
            catch (Exception error) { result = "error:" + error.Message; timeout.Cancel(); }
        }
        Avalonia.Threading.Dispatcher.UIThread.MainLoop(timeout.Token);
        if (result != "ready") throw new Exception($"{theme} focus composition readiness {result}");
    }
    foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark,
                 NativeReviewThemes.HighContrast, ThemeVariant.Default })
    {
        string theme = variant.Key.ToString() ?? throw new Exception("Theme key unavailable");
        if ((pointerRed || numericPaintRed) && theme != "HighContrast") continue;
        Environment.SetEnvironmentVariable("CFDW_REVIEW_STATE", "example");
        var window = new MainWindow { RequestedThemeVariant = variant };
        try
        {
        if (numericPaintRed)
        {
            var numericTabs = window.FindControl<TabControl>("DocumentTabs")
                ?? throw new Exception("Numeric RED numericTabs absent");
            var numericController = (WorkbenchController)(typeof(MainWindow).GetField("workbench",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(window) ?? throw new Exception("Numeric RED numericController absent"));
            ReadyThemeWindow(window, numericController, numericTabs.Items.OfType<TabItem>().First(), theme);
            var numericControls = window.FindControl<ListBox>("ControlList")
                ?? throw new Exception("Numeric RED CV list absent");
            var numericEditable = numericController.Inspection?.Authored.Rails
                .SelectMany(rail => rail.Controls.Select(control => (rail.Name, control)))
                .FirstOrDefault(item => item.control.Editable)
                ?? throw new Exception("Numeric RED numericEditable authored CV absent");
            var item = numericControls.Items.OfType<ListBoxItem>().Single(candidate =>
                candidate.Content is string label &&
                label.StartsWith($"{numericEditable.Name} · {numericEditable.control.Id} · ", StringComparison.Ordinal));
            numericControls.SelectedItem = item;
            var numericField = window.FindControl<TextBox>("NumericInput")
                ?? throw new Exception("Numeric RED field absent");
            if (numericController.Draft is null || numericController.Draft.Rail != numericEditable.Name ||
                numericController.Draft.VertexId != numericEditable.control.Id || !numericField.IsEnabled ||
                numericField.IsReadOnly || string.IsNullOrEmpty(numericField.Text))
                throw new Exception("Numeric RED lacks nonempty owned numericEditable field");
            var presenter = TextVisual(numericField, textBox: true);
            var border = numericField.GetVisualDescendants().OfType<Border>()
                .Single(candidate => candidate.Name == "PART_BorderElement");
            var panel = border.GetVisualParent() as Panel
                ?? throw new Exception("TextBox sibling panel absent");
            var host = presenter.GetVisualAncestors().FirstOrDefault(v =>
                ReferenceEquals(v.GetVisualParent(), panel))
                ?? throw new Exception("TextBox text host sibling absent");
            int borderIndex = panel.Children.IndexOf(border);
            int hostIndex = host is Control hostControl ? panel.Children.IndexOf(hostControl) : -1;
            if (borderIndex != 0 || hostIndex != 1)
                throw new Exception("Installed TextBox sibling paint order changed");
            object? Paint(Avalonia.Media.IBrush? brush) => brush is ISolidColorBrush solid
                ? new { type = "solid", color = solid.Color.ToString(), opacity = (double?)brush.Opacity }
                : new { type = brush?.GetType().Name ?? "null", color = "not-recorded",
                    opacity = brush?.Opacity };
            void Capture(string state, string route)
            {
                window.UpdateLayout();
                var origin = presenter.TranslatePoint(new Point(0, 0), border);
                var transform = presenter.TransformToVisual(border);
                Console.WriteLine("NUMERIC-PAINT " + System.Text.Json.JsonSerializer.Serialize(new {
                    theme, state, route, actualTheme = window.ActualThemeVariant.Key.ToString(),
                    rail = numericController.Draft?.Rail, cv = numericController.Draft?.VertexId,
                    draftGeneration = numericController.Draft?.Generation,
                    accepted = numericController.Inspection?.Authored.Binding.AcceptedId,
                    text = numericField.Text, presenterText = presenter.GetType().GetProperty("Text")?.GetValue(presenter),
                    enabled = numericField.IsEffectivelyEnabled, numericField.IsReadOnly,
                    focused = numericField.IsFocused, focusWithin = numericField.IsKeyboardFocusWithin,
                    windowActive = window.IsActive, pointerOver = numericField.IsPointerOver,
                    error = DataValidationErrors.GetHasErrors(numericField),
                    selectionStart = numericField.SelectionStart, selectionEnd = numericField.SelectionEnd,
                    foreground = Paint(PropertyBrush(presenter, "Foreground")),
                    caret = Paint(numericField.CaretBrush), selection = Paint(numericField.SelectionBrush),
                    selectionForeground = Paint(numericField.SelectionForegroundBrush),
                    borderBackground = Paint(border.Background), borderBrush = Paint(border.BorderBrush),
                    borderThickness = border.BorderThickness.ToString(),
                    borderBounds = border.Bounds.ToString(), hostBounds = host.Bounds.ToString(),
                    presenterBounds = presenter.Bounds.ToString(),
                    presenterToBorder = origin?.ToString() ?? "null",
                    presenterTransform = transform?.ToString() ?? "null",
                    borderClip = border.Clip?.ToString() ?? "null",
                    hostClip = host.Clip?.ToString() ?? "null",
                    panelClip = panel.Clip?.ToString() ?? "null",
                    borderOpacity = border.Opacity, hostOpacity = host.Opacity,
                    panelOpacity = panel.Opacity, presenterOpacity = presenter.Opacity,
                    borderIndex, hostIndex,
                    oldAncestorBackdrop = Backing(presenter).ToString()
                }));
            }
            var focusReset = window.FindControl<Button>("ExampleButton")
                ?? throw new Exception("Numeric RED focus reset absent");
            focusReset.Focus(NavigationMethod.Tab);
            Capture("unfocused-rest", "framework-focus");
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            Enter(numericField, window, pointer, 1);
            Capture("unfocused-hover", "framework-pointer-enter");
            numericField.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                numericField, pointer, window, new Point(-100, -100), 2, default, KeyModifiers.None));
            numericField.Focus(NavigationMethod.Tab);
            Capture("keyboard-focus", "framework-navigation");
            numericField.SelectAll();
            Capture("all-selected", "framework-selection");
            numericField.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent,
                Source = numericField, Text = "5" });
            if (numericField.Text != "5") throw new Exception("Framework text input did not replace selection");
            Capture("typed-replacement", "framework-text-input");
            Enter(numericField, window, pointer, 3);
            Capture("keyboard-focus-hover", "framework-pointer-enter");
            numericField.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                numericField, pointer, window, new Point(-100, -100), 4, default, KeyModifiers.None));
            Capture("keyboard-focus-returned", "framework-pointer-exit");
            focusReset.Focus(NavigationMethod.Tab);
            Capture("blurred", "framework-navigation");
            numericField.Focus(NavigationMethod.Tab);
            Capture("refocused", "framework-navigation");
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
            Environment.Exit(0);
        }
        if (focusDiagnostic)
        {
            window.Show();
            window.ApplyTemplate();
            window.Measure(new Size(1024, 700));
            window.Arrange(new Rect(0, 0, 1024, 700));
            var diagnosticTabs = window.FindControl<TabControl>("DocumentTabs")
                ?? throw new Exception("Focus diagnostic DocumentTabs unavailable");
            diagnosticTabs.SelectedIndex = 0;
            window.UpdateLayout();
            var diagnosticTarget = diagnosticTabs.Items.OfType<TabItem>().First();
            if (!diagnosticTarget.Focus(NavigationMethod.Tab) || !diagnosticTarget.IsFocused)
                throw new Exception("Focus diagnostic target did not receive keyboard focus");
            window.UpdateLayout();
            var diagnosticLayer = AdornerLayer.GetAdornerLayer(diagnosticTarget)
                ?? throw new Exception("Focus diagnostic adorner layer absent");
            var diagnosticAdorner = diagnosticLayer.Children.OfType<Control>().Single(child =>
                ReferenceEquals(AdornerLayer.GetAdornedElement(child), diagnosticTarget));
            var diagnosticRings = diagnosticAdorner.GetVisualDescendants().OfType<Border>()
                .Prepend(diagnosticAdorner as Border).Where(border => border is not null)
                .Cast<Border>().Where(border => border.BorderBrush is not null).ToArray();
            if (diagnosticRings.Length < 2)
                throw new Exception("Focus diagnostic two-ring adorner absent");
            var diagnosticOuter = diagnosticRings[0];
            var diagnosticInner = diagnosticRings[1];
            var transform = diagnosticOuter.TransformToVisual(diagnosticTarget);
            var origin = diagnosticOuter.TranslatePoint(new Point(0, 0), diagnosticTarget);
            var innerOrigin = diagnosticInner.TranslatePoint(new Point(0, 0), diagnosticOuter);
            var targetComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(diagnosticTarget);
            var adornerComposition = Avalonia.Rendering.Composition.ElementComposition.GetElementVisual(diagnosticAdorner);
            var adornedProperty = adornerComposition?.GetType().GetProperty("AdornedVisual",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            object? adornedComposition = adornedProperty?.GetValue(adornerComposition);
            string Backdrop()
            {
                try { return Backing(diagnosticTarget).ToString(); }
                catch (Exception error) { return "unresolved:" + error.Message; }
            }
            object? Paint(Avalonia.Media.IBrush? brush) => brush is ISolidColorBrush solid
                ? new { type = "solid", color = solid.Color.ToString(), brushOpacity = brush.Opacity }
                : new { type = brush?.GetType().Name ?? "null", color = "not-recorded", brushOpacity = brush?.Opacity ?? double.NaN };
            Console.WriteLine("FOCUS-DIAGNOSTIC " + System.Text.Json.JsonSerializer.Serialize(new {
                theme, targetFocused = diagnosticTarget.IsFocused,
                targetBounds = diagnosticTarget.Bounds.ToString(),
                outerBounds = diagnosticOuter.Bounds.ToString(),
                innerBounds = diagnosticInner.Bounds.ToString(),
                targetClip = diagnosticTarget.Clip?.ToString() ?? "null",
                adornerClip = diagnosticAdorner.Clip?.ToString() ?? "null",
                outerClip = diagnosticOuter.Clip?.ToString() ?? "null",
                innerClip = diagnosticInner.Clip?.ToString() ?? "null",
                layerClip = diagnosticLayer.Clip?.ToString() ?? "null",
                transform = transform?.ToString() ?? "null",
                origin = origin?.ToString() ?? "null",
                innerOrigin = innerOrigin?.ToString() ?? "null",
                adornedAttached = ReferenceEquals(AdornerLayer.GetAdornedElement(diagnosticAdorner), diagnosticTarget),
                adornedCompositionIdentity = adornedProperty is null ? "not-recorded" :
                    ReferenceEquals(adornedComposition, targetComposition) ? "match" : "mismatch",
                outerBorderThickness = diagnosticOuter.BorderThickness.ToString(),
                innerBorderThickness = diagnosticInner.BorderThickness.ToString(),
                targetBackground = Paint(diagnosticTarget.Background),
                outerPaint = Paint(diagnosticOuter.BorderBrush),
                innerPaint = Paint(diagnosticInner.BorderBrush),
                targetOpacity = diagnosticTarget.Opacity,
                adornerOpacity = diagnosticAdorner.Opacity,
                outerOpacity = diagnosticOuter.Opacity,
                innerOpacity = diagnosticInner.Opacity,
                candidateBackdrop = Backdrop() }));
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
            continue;
        }
        if (pointerRed)
        {
            var pointerTabs = window.FindControl<TabControl>("DocumentTabs")
                ?? throw new Exception("Pointer RED DocumentTabs unavailable");
            var pointerSourceTab = pointerTabs.Items.OfType<TabItem>().ElementAt(1);
            pointerTabs.SelectedIndex = 1;
            var pointerController = (WorkbenchController)(typeof(MainWindow).GetField("workbench",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(window) ?? throw new Exception("Pointer RED controller unavailable"));
            ReadyThemeWindow(window, pointerController, pointerSourceTab, theme);
            var pseudoProperty = typeof(StyledElement).GetProperty("PseudoClasses",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("Installed protected PseudoClasses unavailable");
            var pseudo = pseudoProperty.GetValue(pointerSourceTab) as IPseudoClasses
                ?? throw new Exception("Installed IPseudoClasses unavailable");
            using var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var origin = pointerSourceTab.TranslatePoint(new Point(10, 10), window)
                ?? throw new Exception("Pointer RED tab/window position unavailable");
            pointerSourceTab.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent,
                pointerSourceTab, pointer, window, origin, 1, default, KeyModifiers.None));
            if (!pointerSourceTab.IsPointerOver || !pseudo.Contains(":pointerover"))
                throw new Exception("Framework PointerEntered did not transition IsPointerOver/pseudo state");
            window.UpdateLayout();
            var visual = TextVisual(pointerSourceTab);
            var rawForeground = PropertyBrush(visual, "Foreground");
            var pointerRoot = pointerSourceTab.GetVisualDescendants().OfType<Border>()
                .SingleOrDefault(item => item.Name == "PART_LayoutRoot");
            var rawBackground = pointerRoot?.Background;
            Console.WriteLine("THEME-POINTER-PAINT " + System.Text.Json.JsonSerializer.Serialize(new {
                visual = visual.GetType().Name,
                foregroundType = rawForeground?.GetType().FullName ?? "null",
                foregroundColor = (rawForeground as ISolidColorBrush)?.Color.ToString() ?? "null",
                foregroundOpacity = rawForeground?.Opacity.ToString() ?? "null",
                backgroundType = rawBackground?.GetType().FullName ?? "null",
                backgroundColor = (rawBackground as ISolidColorBrush)?.Color.ToString() ?? "null",
                backgroundOpacity = rawBackground?.Opacity.ToString() ?? "null",
                rootBounds = pointerRoot?.Bounds.ToString() ?? "null",
                textBounds = visual.Bounds.ToString() }));
            pointerSourceTab.RaiseEvent(new PointerPressedEventArgs(pointerSourceTab, pointer,
                window, origin, 2,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton,
                    PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            Console.WriteLine("THEME-POINTER-PRESS " + System.Text.Json.JsonSerializer.Serialize(new {
                selected = pointerSourceTab.IsSelected,
                pointerOver = pointerSourceTab.IsPointerOver,
                pressed = pseudo.Contains(":pressed") }));
            pointerSourceTab.RaiseEvent(new PointerReleasedEventArgs(pointerSourceTab, pointer,
                window, origin, 3,
                new PointerPointProperties(RawInputModifiers.None,
                    PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            pointerSourceTab.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent,
                pointerSourceTab, pointer, window, origin, 4, default, KeyModifiers.None));
            Console.WriteLine("THEME-POINTER-REST " + System.Text.Json.JsonSerializer.Serialize(new {
                selected = pointerSourceTab.IsSelected,
                pointerOver = pointerSourceTab.IsPointerOver,
                pressed = pseudo.Contains(":pressed") }));
            pointerSourceTab.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent,
                pointerSourceTab, pointer, window, origin, 5, default, KeyModifiers.None));
            window.UpdateLayout();
            var foreground = Solid(PropertyBrush(visual, "Foreground"), "HC selected FoilDSL hovered ink");
            var background = Backing(visual);
            double ratio = Contrast(foreground, background);
            Console.WriteLine("THEME-POINTER-RED " + System.Text.Json.JsonSerializer.Serialize(new {
                theme, tab = "FoilDSL", selected = pointerSourceTab.IsSelected,
                pointerOver = pointerSourceTab.IsPointerOver,
                pressed = pseudo.Contains(":pressed"), focused = pointerSourceTab.IsFocused,
                enabled = pointerSourceTab.IsEnabled, textType = visual.GetType().Name,
                textBounds = visual.Bounds.ToString(), foreground = foreground.ToString(),
                background = background.ToString(), ratio }));
            TextRow(theme, "tab.source.selected.hover", pointerSourceTab);
            throw new Exception("HC selected FoilDSL hover did not reproduce low contrast");
        }
        var toolbar = window.FindControl<Button>("ExampleButton")
            ?? throw new Exception("Actual toolbar button did not load");
        var tabs = window.FindControl<TabControl>("DocumentTabs")
            ?? throw new Exception("Actual document tabs did not load");
        var controls = window.FindControl<ListBox>("ControlList")
            ?? throw new Exception("Actual CV list did not load");
        var numeric = window.FindControl<TextBox>("NumericInput")
            ?? throw new Exception("Actual numeric field did not load");
        var sourceTextControl = window.FindControl<TextBox>("SourceText")
            ?? throw new Exception("Actual read-only source field did not load");
        var station = window.FindControl<ListBox>("StationList")
            ?? throw new Exception("Actual station list did not load");
        if (numeric.IsEnabled) throw new Exception("Empty-state numeric field unexpectedly enabled");
        Console.WriteLine($"THEME-DISABLED-NUMERIC {theme} enabled=false exempt=true");
        var controllerField = typeof(MainWindow).GetField("workbench",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new Exception("Workbench controller field unavailable");
        var controller = (WorkbenchController)(controllerField.GetValue(window)
            ?? throw new Exception("Workbench controller unavailable"));
        var tabItems = tabs.Items.OfType<TabItem>().ToArray();
        if (tabItems.Length != 2) throw new Exception("Actual DocumentTabs count changed");
        tabs.SelectedIndex = 0;
        ReadyThemeWindow(window, controller, tabItems[0], theme);
        CheckRow(theme, "tab.section.selected", () => TextRow(theme, "tab.section.selected", tabItems[0]));
        CheckRow(theme, "focus.tab", () => FocusRow(theme, "focus.tab", tabItems[0]));
        ProbeStates(theme, "interaction.tab.section.selected", tabItems[0], "selected", window);
        ProbeStates(theme, "interaction.tab.source.unselected", tabItems[1], "unselected", window);
        CheckRow(theme, "toolbar.enabled", () => TextRow(theme, "toolbar.enabled", toolbar));
        CheckRow(theme, "focus.toolbar", () => FocusRow(theme, "focus.toolbar", toolbar));
        ProbeStates(theme, "interaction.toolbar.example", toolbar, "na", window);
        station.SelectedIndex = 0;
        var selectedStationItem = station.SelectedItem as ListBoxItem
            ?? throw new Exception("Actual Example station did not bind");
        var authoredRails = controller.Inspection?.Authored.Rails
            ?? throw new Exception("Accepted Example authored rails unavailable");
        var locked = authoredRails.SelectMany(rail => rail.Controls.Select(control => (rail.Name, control)))
            .First(item => !item.control.Editable);
        var editable = authoredRails.SelectMany(rail => rail.Controls.Select(control => (rail.Name, control)))
            .First(item => item.control.Editable);
        ListBoxItem ItemFor(string rail, string id) => controls.Items.OfType<ListBoxItem>()
            .Single(item => item.Content is string label &&
                label.StartsWith($"{rail} · {id} · ", StringComparison.Ordinal));
        var lockedItem = ItemFor(locked.Name, locked.control.Id);
        controls.SelectedItem = lockedItem;
        if (controller.Draft is not null || numeric.IsEnabled)
            throw new Exception("Locked Example CV unexpectedly created an editable draft");
        Console.WriteLine($"THEME-LOCKED-TARGET {theme} rail={locked.Name} id={locked.control.Id} draft=false");
        var cvItem = ItemFor(editable.Name, editable.control.Id);
        controls.SelectedItem = cvItem;
        if (!ReferenceEquals(controls.SelectedItem, cvItem) || controller.Draft is null ||
            controller.Draft.Rail != editable.Name || controller.Draft.VertexId != editable.control.Id ||
            !numeric.IsEnabled)
            throw new Exception("Numeric field lacks owned editable Example draft");
        Console.WriteLine($"THEME-EDITABLE-TARGET {theme} rail={editable.Name} id={editable.control.Id} owned=true");
        window.UpdateLayout();
        CheckRow(theme, "station.selected", () => TextRow(theme, "station.selected", selectedStationItem));
        CheckRow(theme, "cv.selected", () => TextRow(theme, "cv.selected", cvItem));
        var unselectedStation = station.Items.OfType<ListBoxItem>()
            .First(item => !ReferenceEquals(item, selectedStationItem));
        var unselectedCv = controls.Items.OfType<ListBoxItem>()
            .First(item => !ReferenceEquals(item, cvItem));
        ProbeStates(theme, "interaction.station.selected", selectedStationItem, "selected", window);
        ProbeStates(theme, "interaction.station.unselected", unselectedStation, "unselected", window);
        ProbeStates(theme, "interaction.cv.selected", cvItem, "selected", window);
        ProbeStates(theme, "interaction.cv.unselected", unselectedCv, "unselected", window);
        CheckRow(theme, "station.focused", () => {
            if (!selectedStationItem.Focus(NavigationMethod.Tab) || !selectedStationItem.IsFocused)
                throw new Exception("Actual Example station item lacks keyboard focus");
            TextRow(theme, "station.focused", selectedStationItem);
        });
        CheckRow(theme, "cv.focused", () => {
            if (!cvItem.Focus(NavigationMethod.Tab) || !cvItem.IsFocused)
                throw new Exception("Actual Example CV item lacks keyboard focus");
            TextRow(theme, "cv.focused", cvItem);
        });
        CheckRow(theme, "numeric.enabled.owned-draft", () => TextRow(theme, "numeric.enabled.owned-draft", numeric, textBox: true));
        CheckRow(theme, "focus.numeric", () => FocusRow(theme, "focus.numeric", numeric));
        ProbeStates(theme, "interaction.numeric.owned", numeric, "na", window, textBox: true);
        ProbeTextBoxStates(theme, "numeric", numeric, window, controller);
        var numericPresenter = TextVisual(numeric, textBox: true);
        var numericBorder = numeric.GetVisualDescendants().OfType<Border>()
            .Single(item => item.Name == "PART_BorderElement");
        var oldAncestorBacking = Backing(numericPresenter);
        var numericInk = Solid(PropertyBrush(numericPresenter, "Foreground"), "negative sibling ink");
        numericBorder.Background = new SolidColorBrush(numericInk); // Isolated oracle mutation, never product paint.
        try
        {
            bool refused = false;
            try { TextRow(theme, "negative.sibling", numeric, textBox: true); }
            catch (Exception error) when (error.Message.Contains("contrast", StringComparison.OrdinalIgnoreCase))
            { refused = true; }
            if (!refused || Backing(numericPresenter) != oldAncestorBacking)
                throw new Exception("Sibling-only low-contrast mutation escaped the actual painter oracle");
            Console.WriteLine($"TEXTBOX-SIBLING-NEGATIVE {theme} refused=true ancestorUnchanged=true");
        }
        finally { numericBorder.ClearValue(Border.BackgroundProperty); }
        tabs.SelectedIndex = 0;
        window.UpdateLayout();
        var viewportLabel = window.FindControl<TextBlock>("ViewportProvenance")
            ?? throw new Exception("Viewport annotation missing");
        var sectionLabel = window.FindControl<TextBlock>("SectionReadout")
            ?? throw new Exception("Section annotation missing");
        CheckRow(theme, "viewport.annotation", () => TextRow(theme, "viewport.annotation", viewportLabel));
        CheckRow(theme, "section.annotation", () => TextRow(theme, "section.annotation", sectionLabel));
        tabs.SelectedIndex = 1;
        window.UpdateLayout();
        CheckRow(theme, "tab.source.selected", () => TextRow(theme, "tab.source.selected", tabItems[1]));
        ProbeStates(theme, "interaction.tab.source.selected", tabItems[1], "selected", window);
        ProbeStates(theme, "interaction.tab.section.unselected", tabItems[0], "unselected", window);
        if (!sourceTextControl.IsReadOnly || string.IsNullOrEmpty(sourceTextControl.Text))
            throw new Exception("Active Source tab lost read-only accepted Example text");
        CheckRow(theme, "source.active.readonly", () => TextRow(theme, "source.active.readonly", sourceTextControl, textBox: true));
        ProbeStates(theme, "interaction.source.readonly", sourceTextControl, "na", window, textBox: true);
        ProbeTextBoxStates(theme, "source", sourceTextControl, window, controller);
        var method = typeof(MainWindow).GetMethod("UnsavedDialogAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new Exception("Unsaved dialog method unavailable");
        var pending = (Task<string>)(method.Invoke(window, null)
            ?? throw new Exception("Unsaved dialog task unavailable"));
        var modal = window.OwnedWindows.SingleOrDefault()
            ?? throw new Exception("Unsaved modal was not owned by workbench window");
        if (modal.ActualThemeVariant != window.ActualThemeVariant)
            throw new Exception("Unsaved modal did not inherit its owner theme");
        modal.UpdateLayout();
        var modalBody = modal.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(text => text.Text?.StartsWith("Save this foil", StringComparison.Ordinal) == true)
            ?? throw new Exception("Unsaved modal body unavailable");
        CheckRow(theme, "modal.body", () => TextRow(theme, "modal.body", modalBody));
        var modalButtons = modal.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Content is string).ToDictionary(button => (string)button.Content!);
        foreach (var choice in new[] { "Save", "Discard", "Cancel" })
            CheckRow(theme, "modal." + choice.ToLowerInvariant(), () =>
                TextRow(theme, "modal." + choice.ToLowerInvariant(), modalButtons[choice]));
        foreach (var choice in new[] { "Save", "Discard", "Cancel" })
            ProbeStates(theme, "interaction.modal." + choice.ToLowerInvariant(),
                modalButtons[choice], "na", modal);
        if (!modalButtons["Cancel"].IsDefault || !modalButtons["Cancel"].IsCancel ||
            !modalButtons["Cancel"].IsFocused)
            throw new Exception("Unsaved modal Cancel lacks safe default/focus");
        modal.Close();
        if (!pending.IsCompletedSuccessfully || pending.Result != "Cancel")
            throw new Exception("Unsaved modal did not close with safe Cancel");
        typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
        window.Close();
        }
        catch (Exception error)
        {
            rowFailures.Add($"{theme}/prerequisite: {error.Message}");
            Console.WriteLine($"THEME-PREREQUISITE-FAIL {theme} {error.GetType().Name}: {error.Message}");
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Close();
        }
    }
    if (focusDiagnostic)
    {
        Console.WriteLine("FOCUS-DIAGNOSTIC-CHECK variants=4");
        Environment.Exit(0);
    }
    var expected = new[] { "Light", "Dark", "HighContrast", "Default" }
        .SelectMany(theme => required.Select(row => theme + "/" + row)).ToHashSet(StringComparer.Ordinal);
    if (!emitted.SetEquals(expected))
        rowFailures.Add("Applied theme row set incomplete: " + string.Join(",", expected.Except(emitted)));
    if (rowFailures.Count > 0)
        throw new Exception("Applied theme matrix refused: " + string.Join(" | ", rowFailures));
    Console.WriteLine($"THEME-APPLIED-CHECK rows={expected.Count} variants=4 source=actual-MainWindow");
    Environment.Exit(0);
}

var reviewValues = new Dictionary<string, string>
{
    ["CFDW_REVIEW_MODE"] = "1", ["CFDW_REVIEW_PERSONA"] = "keyboard",
    ["CFDW_REVIEW_SIZE"] = "1024x700", ["CFDW_REVIEW_STATE"] = "invalid-input",
    ["CFDW_REVIEW_THEME"] = "dark", ["CFDW_REVIEW_REDUCED_MOTION"] = "1"
};
var review = NativeReviewOptions.Parse(key => reviewValues.GetValueOrDefault(key))
    ?? throw new Exception("Review selector was ignored");
if (review.Persona != "keyboard" || review.Width != 1024 || review.Height != 700 ||
    review.State != "invalid-input" || review.Theme != "dark" || !review.ReducedMotion ||
    NativeReviewOptions.Parse(_ => null) is not null)
    throw new Exception("Native review selectors did not round-trip");
if (MainWindow.ReviewFocusTarget("designer") != "viewport" ||
    MainWindow.ReviewFocusTarget("keyboard") != "numeric-or-open" ||
    MainWindow.ReviewFocusTarget("screen-reader") != "stations" ||
    MainWindow.ReviewFocusTarget("dense") != "controls")
    throw new Exception("Review persona did not select a real initial focus region");
var motionControl = new Button { Transitions = new Transitions { new DoubleTransition { Property = Button.OpacityProperty, Duration = TimeSpan.FromSeconds(1) } } };
MainWindow.SuppressTransitions(motionControl);
if (motionControl.Transitions is { Count: > 0 }) throw new Exception("Reduced-motion review retained a control transition");
using (var reviewController = new WorkbenchController())
{
    await review.ApplyStateAsync(reviewController);
    if (reviewController.Draft is null || reviewController.DraftInputValid || !reviewController.IsDirty ||
        reviewController.Inspection?.Geometry.Status != GeometryStatus.Certified)
        throw new Exception("Invalid-input review state bypassed the real accepted/draft controller route");
    var field = MainWindow.DraftField(reviewController.DraftProjection!, reviewController.Draft);
    if (field.Unit != "mm" || !double.TryParse(field.Text, out _))
        throw new Exception("Fresh review draft did not provide its owned numeric value and unit");
}
bool reviewInvalidSizeRefused = false;
try { NativeReviewOptions.Parse(key => key == "CFDW_REVIEW_MODE" ? "1" : key == "CFDW_REVIEW_SIZE" ? "900x600" : null); }
catch (ArgumentException) { reviewInvalidSizeRefused = true; }
if (!reviewInvalidSizeRefused) throw new Exception("Review harness accepted a window below its minimum size");
using (var refusedReview = new WorkbenchController())
{
    await new NativeReviewOptions("designer", 1280, 800, "refused-open", "system", false, null)
        .ApplyStateAsync(refusedReview);
    if (refusedReview.Inspection?.Geometry.Status != GeometryStatus.Certified ||
        string.IsNullOrEmpty(refusedReview.AcceptedSource) || refusedReview.PendingOriginal is null)
        throw new Exception("Refused-open review state did not preserve a real accepted source and refused bytes");
}
using (var denseFileReview = new WorkbenchController())
{
    await new NativeReviewOptions("dense", 1024, 700, "file", "dark", true,
        "src/CfdWorkbench.Desktop/Assets/example.foil").ApplyStateAsync(denseFileReview);
    if (denseFileReview.Inspection?.Geometry.Status != GeometryStatus.Certified)
        throw new Exception("File review state did not admit the real certified source");
}
bool falseNotAssessedRefused = false;
try
{
    using var notAssessedReview = new WorkbenchController();
    await new NativeReviewOptions("designer", 1024, 700, "not-assessed", "system", false,
        "src/CfdWorkbench.Desktop/Assets/example.foil").ApplyStateAsync(notAssessedReview);
}
catch (ArgumentException) { falseNotAssessedRefused = true; }
if (!falseNotAssessedRefused) throw new Exception("Review harness falsely labelled certified source Not assessed");
string geometryPath = Path.Combine(Path.GetTempPath(), $"geometry-{Guid.NewGuid():N}.foil");
try
{
    var validBytes = await File.ReadAllTextAsync("src/CfdWorkbench.Desktop/Assets/example.foil");
    var invalidGeometry = validBytes.Replace("points [(0, 0), (0.1, 0)", "points [(0, 1), (0.1, 0)", StringComparison.Ordinal);
    if (invalidGeometry == validBytes) throw new Exception("Invalid geometry fixture construction failed");
    await File.WriteAllTextAsync(geometryPath, invalidGeometry);
    using var invalidGeometryReview = new WorkbenchController();
    await new NativeReviewOptions("designer", 1024, 700, "invalid-geometry", "system", false, geometryPath)
        .ApplyStateAsync(invalidGeometryReview);
    if (invalidGeometryReview.Inspection is not null || invalidGeometryReview.PendingOriginal is null)
        throw new Exception("Invalid geometry review was accepted or original bytes were lost");
}
finally { File.Delete(geometryPath); }

using var workbench = new WorkbenchController();
await workbench.OpenExampleAsync();
var accessibleViewport = new Viewport { Frame = workbench.Frame };
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
var viewportPeer = ControlAutomationPeer.CreatePeerForElement(accessibleViewport);
var visualChildren = accessibleViewport.SemanticControls;
var semanticChildren = visualChildren.Select(ControlAutomationPeer.CreatePeerForElement).ToArray();
int authoredSemanticCount = workbench.Inspection!.Authored.Assignments.Count +
    workbench.Inspection.Authored.Rails.Sum(rail => rail.Controls.Count);
if (viewportPeer.GetAutomationControlType() != AutomationControlType.Group ||
    semanticChildren.Length != authoredSemanticCount ||
    !semanticChildren.Any(child => child.GetName().Contains("station", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("locked", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("editable", StringComparison.OrdinalIgnoreCase)) ||
    !semanticChildren.Any(child => child.GetName().Contains("trailing control vertex", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Viewport peer lacks semantic station and constrained CV children");
var stableChild = visualChildren.First();
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!);
if (!ReferenceEquals(stableChild, accessibleViewport.SemanticControls.First()))
    throw new Exception("Refresh replaced a stable semantic station peer");
accessibleViewport.Semantics = ViewportSemantics.FromInspection(workbench.Inspection!, "trailing", "cv-5");
if (accessibleViewport.SemanticControls.Count != authoredSemanticCount ||
    !accessibleViewport.SemanticControls[workbench.Inspection.Authored.Assignments.Count].Text!.Contains("trailing control vertex cv-5") ||
    !ReferenceEquals(stableChild, accessibleViewport.SemanticControls.First()))
    throw new Exception("Selected tip CV did not remain accessible with all authored controls");
if (accessibleViewport.AnnotationScroller.VerticalScrollBarVisibility != ScrollBarVisibility.Auto ||
    Viewport.PlotWidth(1024 - 240 - 300 - 24, 178) < 250)
    throw new Exception("Minimum-window geometry or dense annotation scrolling regressed");
if (workbench.Inspection?.Geometry.Status != GeometryStatus.Certified) throw new Exception("Example is not certified");
if (workbench.Points.Count != 15) throw new Exception("Expected 15 certified samples");
string source = workbench.AcceptedSource;
string acceptedHash = workbench.Inspection.Authored.Binding.SourceHash;
await workbench.OpenFoilAsync(Encoding.UTF8.GetBytes("not FoilDSL"), "invalid.foil");
if (workbench.AcceptedSource != source || workbench.Inspection?.Authored.Binding.SourceHash != acceptedHash)
    throw new Exception("Rejected FoilDSL replaced the active accepted document");
string invalidNativePath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.cfdw.json");
try
{
    await File.WriteAllTextAsync(invalidNativePath, "not a native project");
    try { await workbench.OpenPathAsync(invalidNativePath); }
    catch (ContractError) { }
    if (workbench.AcceptedSource != source || workbench.Inspection?.Authored.Binding.SourceHash != acceptedHash)
        throw new Exception("Rejected native project replaced the active accepted document");
}
finally { File.Delete(invalidNativePath); }
var rail = workbench.Inspection.Authored.Rails.Single(r => r.Name == "leading");
var control = rail.Controls.First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
workbench.BeginEdit("leading", control.Id);
workbench.UpdateDraft(control.OrdinateSi + .005);
await workbench.PreviewAsync();
if (workbench.Provenance != "preview") throw new Exception("Preview was not shown");
if (workbench.Points.Count != 15) throw new Exception("Preview did not sample bounded geometry");
workbench.Cancel();
if (workbench.AcceptedSource != source) throw new Exception("Cancel changed accepted source");
for (int attempt = 0; attempt < 100 && workbench.Provenance != "accepted"; attempt++) await Task.Delay(10);
if (workbench.Provenance != "accepted") throw new Exception("Cancel did not restore accepted view");
workbench.BeginEdit("leading", control.Id);
workbench.UpdateDraft(control.OrdinateSi + .005);
await workbench.PreviewAsync();
workbench.Apply();
if (workbench.AcceptedSource == source) throw new Exception("Apply did not change source");
workbench.Changed += () =>
{
    if (workbench.Provenance.StartsWith("accepted", StringComparison.Ordinal) &&
        workbench.Frame is { } shown && workbench.Inspection is { } active &&
        shown.SourceHash != active.Authored.Binding.SourceHash)
        throw new Exception("A stale frame was displayed as the current accepted identity");
};
workbench.Undo();
if (workbench.AcceptedSource != source) throw new Exception("Undo did not restore source");
workbench.Redo();
if (workbench.AcceptedSource == source) throw new Exception("Redo did not restore edit");
string conflictPath = Path.Combine(Path.GetTempPath(), $"existing-{Guid.NewGuid():N}.cfdw.json");
byte[] foreignImage = Encoding.UTF8.GetBytes("foreign project bytes");
try
{
    await File.WriteAllBytesAsync(conflictPath, foreignImage);
    var conflict = await workbench.SaveAsync(conflictPath);
    if (conflict.Code != "DOC-CONFLICT") throw new Exception("Expected definite create-only save conflict");
    if (workbench.SaveUncertain) throw new Exception("Definite prepublication conflict entered uncertain-save state");
    if (!(await File.ReadAllBytesAsync(conflictPath)).SequenceEqual(foreignImage))
        throw new Exception("Save conflict changed existing disk bytes");
}
finally { File.Delete(conflictPath); }
var uncertainStore = new UncertainStore();
using (var uncertainWorkbench = new WorkbenchController(_ => uncertainStore))
{
    await uncertainWorkbench.OpenExampleAsync();
    string attemptedPath = Path.Combine(Path.GetTempPath(), $"uncertain-{Guid.NewGuid():N}.cfdw.json");
    var first = await uncertainWorkbench.SaveAsync(attemptedPath);
    if (first.Code != "DOC-SAVE-UNCERTAIN" || !uncertainWorkbench.SaveUncertain ||
        uncertainWorkbench.UncertainPath != attemptedPath || !uncertainWorkbench.IsDirty)
        throw new Exception("First Save As did not retain its uncertain path and dirty source");
    if (await uncertainWorkbench.ResolveUncertainSaveAsync() || !uncertainWorkbench.IsDirty)
        throw new Exception("Matching readback or conflict incorrectly acknowledged durability");
    if (!await uncertainWorkbench.ResolveUncertainSaveAsync() || uncertainWorkbench.IsDirty ||
        uncertainWorkbench.NativePath != attemptedPath)
        throw new Exception("Confirmed durable retry did not acknowledge exact first Save As image");
}
var delayedStore = new DelayedStore();
using (var replacingWorkbench = new WorkbenchController(_ => delayedStore))
{
    await replacingWorkbench.OpenExampleAsync();
    string attemptedPath = Path.Combine(Path.GetTempPath(), $"held-{Guid.NewGuid():N}.cfdw.json");
    var heldSave = replacingWorkbench.SaveAsync(attemptedPath);
    await delayedStore.Started.Task;
    await replacingWorkbench.OpenExampleAsync();
    string? replacementId = replacingWorkbench.Inspection!.Authored.Binding.AcceptedId;
    delayedStore.Release();
    await heldSave;
    if (replacingWorkbench.Inspection?.Authored.Binding.AcceptedId != replacementId ||
        replacingWorkbench.NativePath is not null || !replacingWorkbench.IsDirty)
        throw new Exception("Late save acknowledged or attached its path to a replacement session");
}
string recoveryPath = Path.Combine(Path.GetTempPath(), $"recovery-{Guid.NewGuid():N}.cfdw.json");
try
{
    using var savingDraft = new WorkbenchController();
    await savingDraft.OpenExampleAsync();
    string acceptedBeforeDraft = savingDraft.Inspection!.Authored.Binding.SourceHash;
    var recoveryTarget = savingDraft.Inspection.Authored.Rails.Single(r => r.Name == "leading").Controls
        .First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
    savingDraft.BeginEdit("leading", recoveryTarget.Id);
    savingDraft.UpdateDraft(recoveryTarget.OrdinateSi + .005);
    byte[] retainedDraftBytes = savingDraft.Draft!.Bytes;
    var savedRecovery = await savingDraft.SaveAsync(recoveryPath);
    if (savedRecovery.Code != "OK" || savingDraft.IsDirty)
        throw new Exception("Durable recovery save did not settle the unchanged visible draft");
    using var reopenedDraft = new WorkbenchController();
    await reopenedDraft.OpenPathAsync(recoveryPath);
    if (!reopenedDraft.HasRecovery || reopenedDraft.Draft is not null ||
        reopenedDraft.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Native reopen did not offer a separate draft beside the original accepted source");
    if (reopenedDraft.RecoverySource != Encoding.UTF8.GetString(retainedDraftBytes))
        throw new Exception("Native recovery offer does not expose exact retained draft bytes");
    using var recoveryReview = new WorkbenchController();
    await new NativeReviewOptions("screen-reader", 1024, 700, "recovery", "high-contrast", true, recoveryPath)
        .ApplyStateAsync(recoveryReview);
    if (!recoveryReview.HasRecovery || recoveryReview.Draft is not null ||
        recoveryReview.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Recovery review state bypassed the saved-project offer");
    recoveryReview.ResumeRecovery();
    var recoveryField = MainWindow.DraftField(recoveryReview.DraftProjection!, recoveryReview.Draft!);
    if (recoveryField.Unit != "mm" ||
        recoveryField.Text != ((recoveryTarget.OrdinateSi + .005) * 1000).ToString("G9", System.Globalization.CultureInfo.InvariantCulture))
        throw new Exception("Resumed recovery did not bind its current draft value and unit");
    reopenedDraft.ResumeRecovery();
    if (reopenedDraft.Draft is null || !reopenedDraft.Draft.Bytes.SequenceEqual(retainedDraftBytes) ||
        reopenedDraft.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Resume did not keep recovery separate from accepted identity");
    var invalidImage = JsonNode.Parse(await File.ReadAllTextAsync(recoveryPath))!;
    invalidImage["recovery"]!["utf8Base64Chunks"] = new JsonArray(Convert.ToBase64String(Encoding.UTF8.GetBytes("not FoilDSL")));
    string invalidRecoveryPath = Path.Combine(Path.GetTempPath(), $"invalid-recovery-{Guid.NewGuid():N}.cfdw.json");
    await File.WriteAllTextAsync(invalidRecoveryPath, invalidImage.ToJsonString());
    using var invalidRecovery = new WorkbenchController();
    try { await invalidRecovery.OpenPathAsync(invalidRecoveryPath); }
    finally { File.Delete(invalidRecoveryPath); }
    invalidRecovery.ResumeRecovery();
    if (invalidRecovery.Draft is null ||
        invalidRecovery.DraftProjection is not { } unprojectable ||
        MainWindow.TryDraftField(unprojectable, invalidRecovery.Draft) is not null ||
        invalidRecovery.RecoverySource != "not FoilDSL" ||
        invalidRecovery.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Unprojectable recovery draft did not retain raw bytes and accepted source separately");
    if (!invalidRecovery.DraftInputValid) throw new Exception("Unprojectable recovery was marked as invalid numeric input");
    await invalidRecovery.PreviewAsync();
    if (invalidRecovery.Provenance != "draft — unavailable geometry" ||
        !invalidRecovery.DraftInputValid || invalidRecovery.Inspection?.Authored.Binding.SourceHash != acceptedBeforeDraft)
        throw new Exception("Unprojectable recovery could not report Preview diagnostics without changing accepted source");
}
finally { File.Delete(recoveryPath); }
foreach (string railName in new[] { "leading", "trailing" })
{
    using var targeted = new WorkbenchController();
    await targeted.OpenExampleAsync();
    var controlAtTip = targeted.Inspection!.Authored.Rails.Single(r => r.Name == railName).Controls
        .Where(c => c.Editable && c.Eta > 0 && c.Eta < 1).OrderByDescending(c => c.Eta).First();
    bool upper = true;
    double normalizedX = railName == "leading" ? 0 : 1;
    var baseline = Geometry.PointAt(targeted.Inspection.Geometry.Certificate!, controlAtTip.Eta, normalizedX, upper);
    double baselineX = (baseline.X.Lower + baseline.X.Upper) / 2;
    targeted.BeginEdit(railName, controlAtTip.Id);
    for (int attempt = 0; attempt < 100 && targeted.Frame?.InteriorEta != controlAtTip.Eta; attempt++) await Task.Delay(10);
    if (targeted.Frame?.InteriorEta != controlAtTip.Eta)
        throw new Exception("Selected target did not retarget the accepted section slice");
    targeted.UpdateDraft(controlAtTip.OrdinateSi + .005);
    await targeted.PreviewAsync();
    var displayed = targeted.Points.Single(p => p.Eta == controlAtTip.Eta && p.NormalizedX == normalizedX && p.Upper);
    if (Math.Abs(displayed.X - baselineX) < 1e-6)
        throw new Exception($"Tip-side {railName} edit does not move its certified interior display sample");
    targeted.Cancel();
    for (int attempt = 0; attempt < 100 && targeted.Frame?.InteriorEta != controlAtTip.Eta; attempt++) await Task.Delay(10);
    if (targeted.Frame?.InteriorEta != controlAtTip.Eta || targeted.Frame.Provenance != "accepted")
        throw new Exception("Cancel did not return to the same accepted eta slice");
}
using (var invalidInput = new WorkbenchController())
{
    await invalidInput.OpenExampleAsync();
    var editable = invalidInput.Inspection!.Authored.Rails.Single(r => r.Name == "leading").Controls
        .First(c => c.Editable && c.Eta > 0 && c.Eta < 1);
    invalidInput.BeginEdit("leading", editable.Id);
    invalidInput.UpdateDraft(editable.OrdinateSi + .005);
    invalidInput.InvalidateDraftInput();
    if (invalidInput.DraftInputValid) throw new Exception("Invalid visible numeric input still permits preview");
    try { await invalidInput.PreviewAsync(); throw new Exception("Preview accepted invalid visible numeric input"); }
    catch (ContractError error) when (error.Code == "DSL-INVALID-NUMERIC") { }
    try
    {
        await invalidInput.SaveAsync(Path.Combine(Path.GetTempPath(), $"invalid-input-{Guid.NewGuid():N}.cfdw.json"));
        throw new Exception("Save persisted an earlier draft value while visible input was invalid");
    }
    catch (ContractError error) when (error.Code == "DSL-INVALID-NUMERIC") { }
    invalidInput.UpdateDraft(editable.OrdinateSi + .006);
    await invalidInput.PreviewAsync();
    if (invalidInput.Provenance != "preview") throw new Exception("Corrected numeric input did not restore Preview");
}
if (MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Meta, macOS: true) != "undo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Meta | KeyModifiers.Shift, macOS: true) != "redo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Control, macOS: true) is not null ||
    MainWindow.AcceptedHistoryShortcut(Key.Z, KeyModifiers.Control, macOS: false) != "undo" ||
    MainWindow.AcceptedHistoryShortcut(Key.Y, KeyModifiers.Control, macOS: false) != "redo")
    throw new Exception("Native accepted-history shortcuts do not match macOS and Windows modifiers");
if (MainWindow.NextRegionIndex(-1, false, [true, true, true, true]) != 0 ||
    MainWindow.NextRegionIndex(-1, true, [true, true, true, true]) != 3 ||
    MainWindow.NextRegionIndex(0, true, [true, true, true, true]) != 3 ||
    MainWindow.NextRegionIndex(2, false, [true, false, true, true]) != 3 ||
    MainWindow.NextRegionIndex(2, false, [true, false, true, false]) != 0)
    throw new Exception("F6 region cycling did not skip unavailable regions in both directions");
var sectionTab = new TabItem { Header = "Section sample" };
var sourceTab = new TabItem { Header = "FoilDSL source" };
var documentRegion = new TabControl { ItemsSource = new[] { sectionTab, sourceTab }, SelectedIndex = 0 };
if (!ReferenceEquals(MainWindow.FocusCandidates(documentRegion).FirstOrDefault(), sectionTab) ||
    !MainWindow.IsReeditKey(Key.Enter) || !MainWindow.IsReeditKey(Key.Space) || MainWindow.IsReeditKey(Key.Down))
    throw new Exception("F6 document tabs or selected-item keyboard re-edit target is absent");
var numericBinding = new NumericBindingGuard();
var renderFrame = new DisplayFrame([], default!, .5, 0, "source", "accepted");
var priorFrame = new DisplayFrame([], default!, .5, 0, "source", "accepted");
var renderViewport = new Viewport { Frame = renderFrame };
long initialRevision = renderViewport.FrameRevision;
renderViewport.Frame = renderFrame;
if (renderViewport.FrameRevision != initialRevision)
    throw new Exception("Repeated Refresh advanced the same viewport frame revision");
renderViewport.InvalidateFrameForMetric();
if (renderViewport.FrameRevision != initialRevision + 1)
    throw new Exception("Metric invalidation did not create a fresh target revision");
if (NativeRenderCorrelation.SectionEligible(false, true) ||
    NativeRenderCorrelation.SectionEligible(true, false) ||
    !NativeRenderCorrelation.SectionEligible(true, true))
    throw new Exception("Source tab's hidden section was incorrectly required for a render metric");
if (NativeRenderCorrelation.Fresh(7, 7, 3, 3, renderFrame, renderFrame) ||
    NativeRenderCorrelation.Fresh(7, 8, 3, 2, renderFrame, renderFrame) ||
    NativeRenderCorrelation.Fresh(7, 8, 3, 3, renderFrame, priorFrame) ||
    !NativeRenderCorrelation.Fresh(7, 8, 3, 3, renderFrame, renderFrame))
    throw new Exception("Native batch oracle accepted a prior same-frame draw or wrong revision");
if (NativeRenderCorrelation.SameState("accepted", "hash", "draft", 2,
        "accepted", "hash", "draft", 3) ||
    !NativeRenderCorrelation.SameState("accepted", "hash", "draft", 2,
        "accepted", "hash", "draft", 2))
    throw new Exception("Stale draft generation satisfied native timing correlation");
if (NativeRenderCorrelation.TargetSnapshotMatches(renderFrame, "accepted", .5,
        priorFrame, "accepted", .5) ||
    !NativeRenderCorrelation.TargetSnapshotMatches(renderFrame, "accepted", .5,
        renderFrame, "accepted", .5))
    throw new Exception("Same-identity new frame satisfied old native metric snapshot");
var emittedMetric = NativeMetricRecord.Serialize(1, "edit", "not_assessed", "timeout", 12.5, 2, "not_recorded");
if (emittedMetric.Contains("hash", StringComparison.OrdinalIgnoreCase) ||
    emittedMetric.Contains("source", StringComparison.OrdinalIgnoreCase) ||
    !emittedMetric.Contains("not_assessed", StringComparison.Ordinal))
    throw new Exception("Normal-path metric disclosed source identity or omitted refusal status");
numericBinding.NoteProgrammatic("120");
if (numericBinding.ShouldProcess("120", editingEnabled: true) ||
    numericBinding.ShouldProcess("120", editingEnabled: true) ||
    !numericBinding.ShouldProcess("5", editingEnabled: true) ||
    numericBinding.ShouldProcess("5", editingEnabled: true) ||
    !numericBinding.ShouldProcess("120", editingEnabled: true))
    throw new Exception("Queued programmatic accepted value advanced an untouched draft or blocked user input");
numericBinding.NoteProgrammatic("");
if (numericBinding.ShouldProcess("", editingEnabled: false))
    throw new Exception("Disabled unprojectable recovery was treated as invalid user numeric input");
var stationItem = new ListBoxItem { Content = "root station" };
var stationRegion = new ListBox { ItemsSource = new[] { stationItem } };
if (!ReferenceEquals(MainWindow.FocusCandidates(stationRegion).FirstOrDefault(), stationItem))
    throw new Exception("F6 navigator region did not offer its actual focusable station item");
var originalItems = stationRegion.ItemsSource;
MainWindow.BindNavigatorItems(stationRegion, new[] { new ListBoxItem { Content = "replacement" } }, acceptedChanged: false);
if (!ReferenceEquals(stationRegion.ItemsSource, originalItems) ||
    !ReferenceEquals(stationRegion.Items[0], stationItem))
    throw new Exception("Same accepted identity replaced station AX items during selection refresh");
MainWindow.BindNavigatorItems(stationRegion, new[] { new ListBoxItem { Content = "new accepted revision" } }, acceptedChanged: true);
if (ReferenceEquals(stationRegion.ItemsSource, originalItems))
    throw new Exception("New accepted identity failed to replace stale station items");
using (var repeatedSelection = new WorkbenchController())
{
    await repeatedSelection.OpenExampleAsync();
    var acceptedCv = repeatedSelection.Inspection!.Authored.Rails.Single(rail => rail.Name == "leading")
        .Controls.Single(vertex => vertex.Id == "cv-2");
    repeatedSelection.BeginEdit("leading", acceptedCv.Id);
    repeatedSelection.UpdateDraft(.005);
    await repeatedSelection.PreviewAsync();
    repeatedSelection.Cancel();
    var acceptedField = MainWindow.AcceptedControlField(acceptedCv, "mm");
    if (acceptedField.Text != "0" || acceptedField.Unit != "mm" ||
        !MainWindow.CanRestartSelectedEdit(repeatedSelection.Draft, selectedItemMatches: true, editable: acceptedCv.Editable))
        throw new Exception("Cancel left stale numeric text or blocked same-selected CV re-edit");
    repeatedSelection.BeginEdit("leading", acceptedCv.Id);
    if (repeatedSelection.Draft is null) throw new Exception("Repeated edit did not create an owned draft");
}
Console.WriteLine("Desktop Example, bounded preview, cancel, apply, undo and redo passed.");

AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
var loadedStyles = (Styles)AvaloniaXamlLoader.Load(
    new Uri("avares://CfdWorkbench.Desktop/Styles.axaml"), null);
var expectedThemeBrushes = new (string Key, string Light, string Dark, string HighContrast)[]
{
    ("CanvasBrush", "#f0f2f1", "#101a1d", "#000000"),
    ("SurfaceBrush", "#fbfcfb", "#17272c", "#000000"),
    ("SurfaceSoftBrush", "#e8edeb", "#21353a", "#000000"),
    ("InkBrush", "#1b2929", "#edf4f2", "#ffffff"),
    ("MutedBrush", "#526362", "#b2c6c2", "#ffffff"),
    ("LineBrush", "#c9d3cf", "#4e696b", "#ffffff"),
    ("PrimaryBrush", "#006c67", "#66ddc8", "#ffff00"),
    ("OnPrimaryBrush", "#ffffff", "#101a1d", "#000000"),
    ("DangerBrush", "#a92e37", "#ff98a1", "#ffff00"),
    ("ViewportBrush", "#17272c", "#17272c", "#000000"),
    ("ViewportGridBrush", "#344b50", "#344b50", "#ffffff"),
    ("ViewportInkBrush", "#edf4f2", "#edf4f2", "#ffffff"),
    ("FoilBrush", "#85c9c4", "#85c9c4", "#ffff00"),
    ("StationBrush", "#66ddc8", "#66ddc8", "#00ffff")
};
void AssertThemeBrushes(bool emit)
{
    foreach (var (key, light, dark, highContrast) in expectedThemeBrushes)
        foreach (var (variant, expected) in new[] {
            (ThemeVariant.Light, light), (ThemeVariant.Dark, dark),
            (NativeReviewThemes.HighContrast, highContrast) })
        {
            if (!loadedStyles.TryGetResource(key, variant, out var value) ||
                value is not ISolidColorBrush brush || brush.Color != Color.Parse(expected))
                throw new Exception($"Loaded XAML theme brush {variant.Key}/{key}: " +
                    $"expected {expected}, observed {((value as ISolidColorBrush)?.Color.ToString() ?? "missing/non-solid")}");
            if (emit)
                Console.WriteLine($"THEME-RESOURCE {variant.Key}/{key}=#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}");
        }
}
AssertThemeBrushes(emit: true);
foreach (var (key, light, dark, highContrast) in new[] {
    ("SystemControlFocusVisualPrimaryBrush", "#006c67", "#66ddc8", "#ffff00"),
    ("SystemControlFocusVisualSecondaryBrush", "#1b2929", "#edf4f2", "#ffffff") })
    foreach (var (variant, expected) in new[] {
        (ThemeVariant.Light, light), (ThemeVariant.Dark, dark),
        (NativeReviewThemes.HighContrast, highContrast) })
    {
        if (!loadedStyles.TryGetResource(key, variant, out var value) ||
            value is not ISolidColorBrush brush || brush.Color != Color.Parse(expected) ||
            Math.Abs(brush.Opacity - 1) > 0.000001)
            throw new Exception($"Loaded XAML focus brush {variant.Key}/{key} is missing, translucent, or wrong");
        Console.WriteLine($"THEME-FOCUS-RESOURCE {variant.Key}/{key}=#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}");
    }
loadedStyles.Resources["SurfaceBrush"] = new SolidColorBrush(Color.Parse("#fbfcfb"));
bool shadowMutationRefused = false;
try { AssertThemeBrushes(emit: false); }
catch (Exception error) when (error.Message.Contains("Dark/SurfaceBrush", StringComparison.Ordinal))
{ shadowMutationRefused = true; }
loadedStyles.Resources.Remove("SurfaceBrush");
if (!shadowMutationRefused) throw new Exception("Root-key shadow mutation escaped loaded-resource control");
Console.WriteLine("THEME-SHADOW-MUTATION refused Dark/SurfaceBrush");
AssertThemeBrushes(emit: false);
Console.WriteLine("THEME-RESOURCE-CHECK loaded-XAML Light/Dark/HighContrast 42");
CfdWorkbench.Desktop.Tests.SectionCanvasTests.Run();
Environment.Exit(0);

sealed class UncertainStore : IProjectStore
{
    private byte[]? image;
    private int saves;
    public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
    {
        image = request.Image;
        saves++;
        return Task.FromResult(saves switch
        {
            1 => new SaveResult("DOC-SAVE-UNCERTAIN", Identity.Sha256(image), true, false),
            2 => new SaveResult("DOC-CONFLICT", null, false, false),
            _ => new SaveResult("OK", Identity.Sha256(image), true, true)
        });
    }
    public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default)
    {
        if (image is null) throw new Exception("No captured image");
        return Task.FromResult(new ReadResult(image, Identity.Sha256(image)));
    }
    public void Dispose() { }
}

sealed class DelayedStore : IProjectStore
{
    private readonly TaskCompletionSource<SaveResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private byte[]? image;
    public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
    {
        image = request.Image;
        Started.SetResult(true);
        return completion.Task;
    }
    public void Release()
    {
        if (image is null) throw new Exception("No held save");
        completion.SetResult(new SaveResult("OK", Identity.Sha256(image), true, true));
    }
    public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => throw new NotSupportedException();
    public void Dispose() { }
}

readonly record struct FocusPlacementFacts(
    bool AdornedLink,
    bool CompositionLink,
    bool AdornerClipped,
    Rect TargetBounds,
    Rect OuterBounds,
    Rect InnerBounds,
    Rect? AdornerClip,
    Rect? OuterClip,
    Vector CompositionSize,
    Vector3D CompositionOffset,
    Vector3D CompositionScale,
    double CompositionRotation,
    System.Numerics.Quaternion CompositionOrientation,
    Vector CompositionAnchor,
    Vector3D CompositionCenter);
