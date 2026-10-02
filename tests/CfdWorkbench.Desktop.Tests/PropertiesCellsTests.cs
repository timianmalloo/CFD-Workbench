using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.PropertiesViewTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The property sheet in structure B (docs/reviews/ui-property-grid-cells.md §5.3): the B tests, the density tests it keeps
/// and the Text size tests. Each name says what it protects; every check runs on the shell at 1280 × 800.
/// </summary>
public static class PropertiesCellsTests
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly (ThemeVariant Variant, string Name)[] Themes =
        [(ThemeVariant.Light, "light"), (ThemeVariant.Dark, "dark"), (NativeReviewThemes.HighContrast, "high-contrast")];

    public static void Run()
    {
        Pane("PropertiesPane_B_EditableValueHasDottedUnderline", (controller, host, window) =>
        {
            // CL-3, SC 1.4.1: an editable value carries a dotted underline under its text, measured on pixels at 1× and 2×
            // in three themes: dashes in the accent at ≥ 3:1 against the surface, with gaps between them.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (var (variant, name) in Themes)
            {
                window.RequestedThemeVariant = variant;
                Settle(window);
                foreach (int scale in new[] { 1, 2 })
                {
                    var measured = MeasureCue(window, Need<TextBox>(host.Properties, "PointAftInput"), scale);
                    Console.WriteLine(FormattableString.Invariant(
                        $"MEASURE underline {name} {scale}x: dashes {measured.On}/{measured.Length} px at {measured.Contrast:0.00}:1, gaps {measured.Gaps}"));
                    if (measured.Contrast < 3 || measured.On < measured.Length / 3 || measured.Gaps < measured.Length / 3)
                        failures.Add(FormattableString.Invariant($"{name} {scale}x: {measured.On} dashes, {measured.Gaps} gaps of {measured.Length} px, {measured.Contrast:0.00}:1"));
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_ReadOnlyValueHasNoEditCue", (controller, host, window) =>
        {
            // SC 1.4.1: a read-only value is ink with no underline and no ▾; only editable values carry a cue.
            Select(controller, window, MakeAnchor(controller));
            var failures = new List<string>();
            var ink = Resolved(window, "InkBrush");
            foreach (var row in VisibleRows(host))
            {
                bool fact = AutomationProperties.GetControlTypeOverride(row) == AutomationControlType.Text;
                var cues = row.GetVisualDescendants().OfType<PropertiesPane.EditCue>().Where(cue => cue.IsEffectivelyVisible).ToList();
                if (fact)
                {
                    if (cues.Count > 0 || row.GetVisualDescendants().OfType<ComboBox>().Any() || row.GetVisualDescendants().OfType<TextBox>().Any())
                        failures.Add(row.Name + " carries an edit cue");
                    var value = row.GetVisualDescendants().OfType<TextBlock>().First(block => block.Classes.Contains("prop-value"));
                    if (Paint(value.Foreground) != ink || value.TextDecorations is { Count: > 0 })
                        failures.Add($"{row.Name} value {Paint(value.Foreground)} decorated {value.TextDecorations?.Count ?? 0}");
                }
                else if (((Grid)row.Child!).Children.OfType<TextBox>().Any() && cues.Count != 1)
                    failures.Add(row.Name + " editable value has no underline at rest");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_EditCueContrastAtLeast3InThreeThemes", (controller, host, window) =>
        {
            // SC 1.4.11 / 1.4.3: the underline (accent) and the ▾ (muted) are ≥ 3:1 on the surface, the enum text ≥ 4.5:1,
            // in light, dark and high contrast.
            Select(controller, window, MakeAnchor(controller));
            var failures = new List<string>();
            foreach (var (variant, name) in Themes)
            {
                window.RequestedThemeVariant = variant;
                Settle(window);
                var surface = Resolved(window, "SurfaceBrush");
                var cue = Need<TextBox>(host.Properties, "PointAftInput").GetVisualDescendants().OfType<PropertiesPane.EditCue>().Single();
                var kind = Need<ComboBox>(host.Properties, "KindControl");
                var checks = new (string What, Color Ink, double Floor)[]
                {
                    ("underline", Paint(cue.Stroke), 3),
                    ("enum text", Paint(Part<ContentControl>(kind, "ContentPresenter").Foreground), 4.5),
                    ("enum ▾", Paint(Part<PathIcon>(kind, "DropDownGlyph").Foreground), 3)
                };
                foreach (var (what, colour, floor) in checks)
                {
                    double ratio = Contrast(colour, surface);
                    Console.WriteLine(FormattableString.Invariant($"MEASURE contrast {name} {what} {ratio:0.00}:1"));
                    if (ratio < floor) failures.Add(FormattableString.Invariant($"{name} {what} {ratio:0.00}:1"));
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_FocusedValueShowsBox", (controller, host, window) =>
        {
            // SC 2.4.7: at rest no box; focused by Tab or by pointer, a 20 px box with a 1 px accent boundary, the text in
            // ink and no underline. The enum shows the same box.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            var box = Part<Border>(aft, "PART_BorderElement");
            var cue = aft.GetVisualDescendants().OfType<PropertiesPane.EditCue>().Single();
            var primary = Resolved(window, "PrimaryBrush");
            var ink = Resolved(window, "InkBrush");
            var failures = new List<string>();
            if (Paint(box.BorderBrush).A != 0 || !cue.IsVisible || Paint(Presenter(aft).Foreground) != primary)
                failures.Add("at rest: a box is drawn, or no underline, or the text is not the accent");
            foreach (var method in new[] { NavigationMethod.Tab, NavigationMethod.Pointer })
            {
                Need<TextBox>(host.Properties, "PointSpanInput").Focus();
                aft.Focus(method);
                Settle(window);
                Console.WriteLine(FormattableString.Invariant($"MEASURE focus box ({method}) {box.Bounds.Width:0.#} × {box.Bounds.Height:0.#}, band {aft.Bounds.Height:0.#}"));
                if (Paint(box.BorderBrush) != primary || box.BorderThickness != new Thickness(1) || Math.Abs(box.Bounds.Height - 20) > 0.5 ||
                    cue.IsVisible || Paint(Presenter(aft).Foreground) != ink)
                    failures.Add($"{method}: box {Paint(box.BorderBrush)} {box.BorderThickness} h {box.Bounds.Height}, underline {cue.IsVisible}");
            }
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            var typeBox = Part<Border>(type, "Background");
            if (Paint(typeBox.BorderBrush).A != 0) failures.Add("the Type box is drawn at rest");
            type.Focus(NavigationMethod.Tab);
            Settle(window);
            if (Paint(typeBox.BorderBrush) != primary || Math.Abs(typeBox.Bounds.Height - 20) > 0.5)
                failures.Add($"Type focused: {Paint(typeBox.BorderBrush)} h {typeBox.Bounds.Height}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_EveryEditableValueIsTabStop", (controller, host, window) =>
        {
            // SC 2.1.1: every editable value and enum is reached by Tab, with every group expanded, in the anchor (Smooth and
            // Corner), handle and control-point states.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            var failures = new List<string>();
            var states = new (string Name, Action Enter)[]
            {
                ("control", () => Select(controller, window, PropertiesViewTests.Control(controller, "leading"))),
                ("anchor", () => Select(controller, window, anchor)),
                ("anchor corner", () => { Pump(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, TangentKind.Corner, null))); Select(controller, window, Reload(controller, anchor)); }),
                ("handle", () => Select(controller, window, handle))
            };
            foreach (var (name, enter) in states)
            {
                enter();
                foreach (var group in host.Properties.GetVisualDescendants().OfType<Expander>()) group.IsExpanded = true;
                Settle(window);
                var stops = TabWalk(host.Properties, window);
                var editors = host.Properties.GetVisualDescendants().OfType<InputElement>()
                    .Where(item => item is TextBox or ComboBox && item.IsEffectivelyVisible && item.IsEffectivelyEnabled).ToList();
                var missed = editors.Where(editor => !stops.Contains(editor)).Select(editor => editor.Name).ToList();
                Console.WriteLine($"MEASURE tab stops {name}: {stops.Count}, editable values {editors.Count}");
                if (missed.Count > 0 || editors.Count == 0) failures.Add($"{name}: not reached {string.Join(",", missed)}");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_RowPressFocusesValue", (controller, host, window) =>
        {
            // SC 2.5.8 / DR-DEN-1: the whole 24 px editable row is the target — a press on its label focuses its value; a
            // read-only row is not a target.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (var (label, editor) in new[] { ("Label_p_aft", "PointAftInput"), ("Label_p_type", "TypeControl"), ("Label_p_from", "PointSpanInput") })
            {
                Need<ToggleButton>(host.Properties, "HowMeasuredButton").Focus();
                Press(window, Need<TextBlock>(host.Properties, label));
                Settle(window);
                if (!ReferenceEquals(window.FocusManager!.GetFocusedElement(), Need<Control>(host.Properties, editor)))
                    failures.Add($"{label} → {window.FocusManager!.GetFocusedElement()?.GetType().Name}");
            }
            var before = window.FocusManager!.GetFocusedElement();
            Press(window, Need<TextBlock>(host.Properties, "Label_p_eta"));
            Settle(window);
            if (!ReferenceEquals(window.FocusManager!.GetFocusedElement(), before)) failures.Add("a read-only row took focus");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_HelpShowsWhileFocused_AndIsDescription", (controller, host, window) =>
        {
            // B: help shows under a row while it has focus, and is always the editor's accessible description; the angle
            // reference shows only while focus is in its group (DR-CELL-4).
            Select(controller, window, MakeAnchor(controller));
            var help = Need<TextBlock>(host.Properties, "PointHelper");
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            var reference = Need<TextBlock>(host.Properties, "Note_tan_0");
            var failures = new List<string>();
            if (help.IsEffectivelyVisible || reference.IsEffectivelyVisible) failures.Add("help shows at rest");
            if (AutomationProperties.GetHelpText(type) != help.Text) failures.Add("Type's description is not its help line");
            type.Focus();
            Settle(window);
            if (!help.IsEffectivelyVisible) failures.Add("Type's help is hidden while focused");
            Need<TextBox>(host.Properties, "HandleAngleInput").Focus();
            Settle(window);
            if (help.IsEffectivelyVisible || !reference.IsEffectivelyVisible || reference.Text != PropertyCopy.AngleReference)
                failures.Add($"after moving to Angle: help {help.IsEffectivelyVisible}, reference {reference.IsEffectivelyVisible}");
            Need<TextBox>(host.Properties, "PointAftInput").Focus();
            Settle(window);
            if (reference.IsEffectivelyVisible) failures.Add("the angle reference stays after focus leaves its group");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_PointGroupHoldsPositionAndTangent", (controller, host, window) =>
        {
            // DR-CELL-1: one "Point" twirl holds Type, From root, η, Aft and the tangent rows; there is no Tangent header.
            Select(controller, window, MakeAnchor(controller));
            var point = Need<Expander>(host.Properties, "Group_pos");
            var labels = point.GetVisualDescendants().OfType<TextBlock>().Where(block => block.Classes.Contains("prop-label") && block.IsEffectivelyVisible)
                .Select(block => block.Text).ToList();
            string want = "Type,From root,η,Aft,Tangent kind,Angle,To root,To tip";
            if (Text(host.Properties, "GroupTitle_pos") != "Point" || string.Join(",", labels) != want)
                throw new InvalidOperationException($"'{Text(host.Properties, "GroupTitle_pos")}': {string.Join(",", labels)}");
            if (host.Properties.GetVisualDescendants().OfType<Expander>().Any(group => group.Name == "Group_tan"))
                throw new InvalidOperationException("the tangent rows have their own header");
            point.IsExpanded = false;
            Settle(window);
            if (Need<ComboBox>(host.Properties, "KindControl").IsEffectivelyVisible) throw new InvalidOperationException("collapsing Point left the tangent rows");
        });

        Pane("PropertiesPane_B_KindIsEnum_CommitRulesMatchType", (controller, host, window) =>
        {
            // DR-CELL-2: Tangent kind is a ▾ enum with Type's rules — arrows stage, Esc keeps, Return applies, a pick applies.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            if (!kind.Classes.Contains("prop-b") || host.Properties.GetVisualDescendants().OfType<RadioButton>().Any())
                throw new InvalidOperationException("Tangent kind is not the B enum");
            string source = controller.AcceptedSource;
            kind.Focus();
            Key(kind, Avalonia.Input.Key.Down);
            Key(kind, Avalonia.Input.Key.Escape);
            Settle(window);
            if (controller.AcceptedSource != source || Selected(kind) != anchor.Kind.ToString() || Need<TextBlock>(host.Properties, "Message_t_kind").IsEffectivelyVisible)
                throw new InvalidOperationException($"Esc did not keep {anchor.Kind}: shows {Selected(kind)}");
            Key(kind, Avalonia.Input.Key.Down);
            Key(kind, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            var afterReturn = Reload(controller, anchor).Kind;
            if (afterReturn == anchor.Kind) throw new InvalidOperationException("Return did not apply the staged kind");
            kind.IsDropDownOpen = true;
            kind.SelectedIndex = 2;
            kind.IsDropDownOpen = false;
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind != TangentKind.Corner) throw new InvalidOperationException("a pick did not apply Corner");
            controller.Undo();
            Settle(window);
            if (Reload(controller, anchor).Kind != afterReturn) throw new InvalidOperationException("the pick was not one undo row");
        });

        Pane("PropertiesPane_B_RowPitch20_24", (controller, host, window) =>
        {
            // B: read-only rows are 20 px, editable rows 24 px (plus the half-strength rule between rows).
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            Console.WriteLine(FormattableString.Invariant($"MEASURE identity block {Need<Border>(host.Properties, "IdentityBlock").Bounds.Height:0.#} px, Wing block {Need<Border>(host.Properties, "WingBlock").Bounds.Height:0.#} px"));
            foreach (var (name, want) in new[] { ("Row_p_eta", 20d), ("Row_p_from", 24d), ("Row_p_aft", 24d), ("Row_p_type", 24d), ("Row_e_mac", 20d), ("Row_w_span", 24d) })
            {
                double height = Need<Border>(host.Properties, name).Bounds.Height;
                Console.WriteLine(FormattableString.Invariant($"MEASURE row {name} {height:0.##} px"));
                if (Math.Abs(height - want) > 0.5) failures.Add(FormattableString.Invariant($"{name} {height:0.##} ≠ {want}"));
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_HeaderTwirl24_NoBand", (controller, host, window) =>
        {
            // B: a 24 px twirl header with no band and a 10 px muted chevron at its left; Fluent's chevron button is gone.
            Select(controller, window, MakeAnchor(controller));
            var failures = new List<string>();
            foreach (var id in new[] { "pos", "rail" })
            {
                var header = Need<Expander>(host.Properties, "Group_" + id).GetVisualDescendants().OfType<ToggleButton>().First();
                var chevron = Need<Avalonia.Controls.Shapes.Path>(host.Properties, "GroupChevron_" + id);
                var title = Need<TextBlock>(host.Properties, "GroupTitle_" + id);
                Console.WriteLine(FormattableString.Invariant($"MEASURE header {id} {header.Bounds.Height:0.##} px"));
                if (Math.Abs(header.Bounds.Height - 24) > 0.5) failures.Add($"{id} header {header.Bounds.Height}");
                if (Paint(Part<Border>(header, "ToggleButtonBackground").Background).A != 0) failures.Add(id + " header has a band");
                if (Part<Border>(header, "ExpandCollapseChevronBorder").IsVisible) failures.Add(id + " keeps Fluent's chevron button");
                if (chevron.Bounds.Width != 10 || chevron.TranslatePoint(default, header)!.Value.X >= title.TranslatePoint(default, header)!.Value.X ||
                    Paint(chevron.Stroke) != Resolved(window, "MutedBrush"))
                    failures.Add($"{id} chevron {chevron.Bounds.Width} px, {Paint(chevron.Stroke)}");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused", (controller, host, window) =>
        {
            // CL-1 / DR-CELL-3: focused in error, the accent box with a 1 px danger box just outside it; unfocused in error,
            // the danger box only. The two differ.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            Settle(window);
            var box = Part<Border>(aft, "PART_BorderElement");
            var ring = Part<Border>(aft, "PART_ErrorRing");
            var primary = Resolved(window, "PrimaryBrush");
            var danger = Resolved(window, "DangerBrush");
            var failures = new List<string>();
            var boxRect = box.Bounds;
            var ringRect = ring.Bounds;
            if (!aft.IsFocused || Paint(box.BorderBrush) != primary || !ring.IsVisible || Paint(ring.BorderBrush) != danger ||
                Math.Abs(boxRect.X - ringRect.X - 1) > 0.01 || Math.Abs(ringRect.Bottom - boxRect.Bottom - 1) > 0.01)
                failures.Add($"focused: box {Paint(box.BorderBrush)} ring {ring.IsVisible} {Paint(ring.BorderBrush)} {ringRect} around {boxRect}");
            Need<TextBox>(host.Properties, "PointSpanInput").Focus();
            Settle(window);
            if (Paint(box.BorderBrush) != danger || ring.IsVisible)
                failures.Add($"unfocused: box {Paint(box.BorderBrush)} ring {ring.IsVisible}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("KindBox_DropDownOpen_ArrowsThenEsc_NoUndoRow", (controller, host, window) =>
        {
            // CB-3: in the open list, arrows then Esc close it on the committed kind and leave no undo row.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            string source = controller.AcceptedSource;
            var kind = OpenKind(host, window);
            ArrowInList(kind, window);
            Key(kind, Avalonia.Input.Key.Escape);
            WaitIdle(controller, window);
            if (kind.IsDropDownOpen || controller.AcceptedSource != source || Reload(controller, anchor).Kind != anchor.Kind || Selected(kind) != anchor.Kind.ToString())
                throw new InvalidOperationException($"after Esc: open {kind.IsDropDownOpen}, kind {Reload(controller, anchor).Kind}, shows {Selected(kind)}");
        });

        Pane("KindBox_DropDownOpen_ArrowsThenClose_OneUndoRow", (controller, host, window) =>
        {
            // CB-3: in the open list, arrows then Return close it on the new kind as exactly one undo row.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            string source = controller.AcceptedSource;
            var kind = OpenKind(host, window);
            string target = ArrowInList(kind, window);
            var item = kind.GetVisualDescendants().OfType<Popup>().First().Child!.GetVisualDescendants().OfType<ComboBoxItem>().First(entry => entry.IsFocused);
            Key(item, Avalonia.Input.Key.Enter);   // Return on the focused item in the open list
            WaitIdle(controller, window);
            if (kind.IsDropDownOpen || Reload(controller, anchor).Kind.ToString() != target)
                throw new InvalidOperationException($"after closing: open {kind.IsDropDownOpen}, kind {Reload(controller, anchor).Kind}, wanted {target}");
            controller.Undo();
            Settle(window);
            if (controller.AcceptedSource != source || Reload(controller, anchor).Kind != anchor.Kind)
                throw new InvalidOperationException("closing on a new kind was not exactly one undo row");
        });

        Pane("PropertiesPane_B_PendingDropIsAnnounced_PendingHelpTextNamesKeys", (controller, host, window) =>
        {
            // CL-2, CB-4, both enums: while a value is staged the box's help names the keys; leaving drops it and the status
            // line says so politely.
            var anchor = MakeAnchor(controller);
            var control = PropertiesViewTests.Control(controller, "leading");
            var seen = Changes(Status(host));
            var failures = new List<string>();
            string source = controller.AcceptedSource;
            foreach (var (point, name, help, said) in new[]
            {
                (control, "TypeControl", "Return applies; Esc keeps control point", "Type unchanged: Control point."),
                (anchor, "KindControl", $"Return applies; Esc keeps {anchor.Kind.ToString()!.ToLowerInvariant()}", $"Tangent kind unchanged: {anchor.Kind}.")
            })
            {
                Select(controller, window, point);
                var box = Need<ComboBox>(host.Properties, name);
                box.Focus();
                Key(box, Avalonia.Input.Key.Down);
                Settle(window);
                if (AutomationProperties.GetHelpText(box) != help) failures.Add($"{name} help '{AutomationProperties.GetHelpText(box)}'");
                Need<TextBox>(host.Properties, "PointAftInput").Focus();
                Settle(window);
                if (!seen.Contains(said)) failures.Add($"{name}: not announced '{said}'");
            }
            if (controller.AcceptedSource != source) failures.Add("leaving applied a staged value");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures) + " | status: " + string.Join(" | ", seen));
        });

        Pane("PropertiesPane_B_FocusedRowStaysInViewWhenHelpToggles", (controller, host, window) =>
        {
            // CL-5 (extends DN-6): when a focused row's help line appears, the row and its help are brought into view.
            var anchor = MakeAnchor(controller);
            Pump(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, TangentKind.Corner, null)));
            host.SetTextScale(2);
            Select(controller, window, Reload(controller, anchor));
            var scroll = Need<ScrollViewer>(host.Properties, "SelectionScroll");
            scroll.Offset = default;
            Settle(window);
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            kind.Focus();
            Settle(window);
            var row = Need<Border>(host.Properties, "Row_t_kind");
            var help = Need<TextBlock>(host.Properties, "Description_t_kind");
            var top = row.TranslatePoint(default, scroll)!.Value.Y;
            var bottom = help.TranslatePoint(new Point(0, help.Bounds.Height), scroll)!.Value.Y;
            Console.WriteLine(FormattableString.Invariant($"MEASURE CL-5 kind row {top:0.#}..{bottom:0.#} in a {scroll.Viewport.Height:0.#} px viewport, offset {scroll.Offset.Y:0.#}"));
            if (!help.IsEffectivelyVisible || top < -0.5 || bottom > scroll.Viewport.Height + 0.5)
                throw new InvalidOperationException(FormattableString.Invariant($"row {top:0.#}..{bottom:0.#} outside 0..{scroll.Viewport.Height:0.#}"));
        });

        // ---------------- kept from the density brief ----------------

        Pane("PropertiesPane_Density_OneFontSizeForLabelValueUnit", (controller, host, window) =>
        {
            // DR-DEN-3: label, value and unit are one size, 11 px.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var sizes = new[] { Need<TextBlock>(host.Properties, "Label_p_aft").FontSize, Need<TextBox>(host.Properties, "PointAftInput").FontSize,
                Need<TextBlock>(host.Properties, "Unit_p_aft").FontSize, Need<TextBlock>(host.Properties, "Value_p_eta").FontSize,
                Need<TextBlock>(host.Properties, "Label_p_eta").FontSize };
            if (sizes.Any(size => size != 11)) throw new InvalidOperationException("sizes " + string.Join(", ", sizes));
        });

        Pane("PropertiesPane_Density_NothingBelow11", (controller, host, window) =>
        {
            // DR-DEN-3: no text in the pane is below 11 px, in the anchor, handle and unavailable states.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            var small = new List<string>();
            foreach (var enter in new Action[] { () => Select(controller, window, anchor), () => Select(controller, window, handle),
                         () => { host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN }); Settle(window); } })
            {
                enter();
                foreach (var text in host.Properties.GetVisualDescendants().Where(item => item.IsEffectivelyVisible))
                {
                    double? size = text switch { TextBlock block => block.FontSize, TextPresenter presenter => presenter.FontSize, _ => null };
                    if (size < 11) small.Add($"{(text as Control)?.Name ?? text.GetType().Name} {size}");
                }
            }
            if (small.Count > 0) throw new InvalidOperationException("below 11: " + string.Join(", ", small.Distinct()));
        });

        Pane("PropertiesPane_Density_UnitOnValueBaseline", (controller, host, window) =>
        {
            // D-2 fixed: the unit, the value and the label share one baseline, on an input row and on a fact row.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            double Baseline(Visual visual, double y) => visual.TranslatePoint(new Point(0, y), host.Properties)!.Value.Y;
            var presenter = Presenter(Need<TextBox>(host.Properties, "PointAftInput"));
            var unit = Need<TextBlock>(host.Properties, "Unit_p_aft");
            var label = Need<TextBlock>(host.Properties, "Label_p_aft");
            var value = Need<TextBlock>(host.Properties, "Value_p_eta");
            var factLabel = Need<TextBlock>(host.Properties, "Label_p_eta");
            double input = Baseline(presenter, presenter.TextLayout.Baseline);
            double unitLine = Baseline(unit, unit.Padding.Top + unit.TextLayout.Baseline);
            double labelLine = Baseline(label, label.Padding.Top + label.TextLayout.Baseline);
            double factLine = Baseline(value, value.Padding.Top + value.TextLayout.Baseline);
            double factLabelLine = Baseline(factLabel, factLabel.Padding.Top + factLabel.TextLayout.Baseline);
            Console.WriteLine(FormattableString.Invariant($"MEASURE baselines input {input:0.##} unit {unitLine:0.##} label {labelLine:0.##}; fact {factLine:0.##} label {factLabelLine:0.##}"));
            if (Math.Abs(input - unitLine) > 0.5 || Math.Abs(input - labelLine) > 0.5 || Math.Abs(factLine - factLabelLine) > 0.5)
                throw new InvalidOperationException(FormattableString.Invariant($"input {input:0.##} unit {unitLine:0.##} label {labelLine:0.##}; fact {factLine:0.##}/{factLabelLine:0.##}"));
        });

        Pane("PropertiesPane_Density_FocusedAndErrorFieldTextNotClipped", (controller, host, window) =>
        {
            // At 11 px the text fits inside the 20 px box when focused and in error, for the TextBox and the ComboBox.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            var failures = new List<string>();
            void Inside(string state, Visual text, double height, Visual frame)
            {
                var top = text.TranslatePoint(default, frame)!.Value.Y;
                if (top < -0.01 || top + height > frame.Bounds.Height + 0.01) failures.Add(FormattableString.Invariant($"{state}: text {top:0.##}+{height:0.##} in {frame.Bounds.Height:0.##}"));
            }
            aft.Focus();
            Settle(window);
            Inside("focused", Presenter(aft), Presenter(aft).TextLayout.Height, Part<Border>(aft, "PART_BorderElement"));
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            Settle(window);
            Inside("error", Presenter(aft), Presenter(aft).TextLayout.Height, Part<Border>(aft, "PART_BorderElement"));
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            type.Focus();
            Settle(window);
            var shown = Part<ContentControl>(type, "ContentPresenter").GetVisualDescendants().OfType<TextBlock>().First();
            Inside("Type focused", shown, shown.Bounds.Height, Part<Border>(type, "Background"));
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_DrawnBoundaryOnePxInEveryState", (controller, host, window) =>
        {
            // The focus box and the error box are 1 px in every state (rest draws none); the enum's box is 1 px too.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            var box = Part<Border>(aft, "PART_BorderElement");
            var failures = new List<string>();
            void Expect(string state)
            {
                Settle(window);
                if (box.BorderThickness != new Thickness(1)) failures.Add($"{state}: {box.BorderThickness}");
            }
            Expect("rest");
            aft.Focus();
            Expect("focus");
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            Expect("focus+error");
            if (Part<Border>(aft, "PART_ErrorRing").BorderThickness != new Thickness(1)) failures.Add("error ring");
            Need<TextBox>(host.Properties, "PointSpanInput").Focus();
            Expect("error");
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            type.Focus();
            Settle(window);
            if (Part<Border>(type, "Background").BorderThickness != new Thickness(1)) failures.Add("Type box " + Part<Border>(type, "Background").BorderThickness);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_FocusedInputShowsWholeExpression", (controller, host, window) =>
        {
            // DC-1 (CB-1): once the text differs from the committed value the field widens across the row, so an expression
            // shows whole; the label stays visible and the unit steps aside. Wing fields at 200, 260 and 300 px, and a
            // Length row under a handle subhead at 200 px.
            var anchor = MakeAnchor(controller);
            Pump(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, TangentKind.Corner, null)));
            Select(controller, window, Reload(controller, anchor));
            var failures = new List<string>();
            void Whole(string where, TextBox box, string label, string unit, string text)
            {
                box.Focus();
                box.Text = text;
                Settle(window);
                var viewer = Part<ScrollViewer>(box, "PART_ScrollViewer");
                var labelBlock = Need<TextBlock>(host.Properties, label);
                Console.WriteLine(FormattableString.Invariant($"MEASURE DC-1 {where} '{text}': text {viewer.Extent.Width:0.#} in {viewer.Viewport.Width:0.#}, label {labelBlock.Bounds.Width:0.#}"));
                if (viewer.Extent.Width > viewer.Viewport.Width + 0.5 || labelBlock.Bounds.Width < 1 || Need<TextBlock>(host.Properties, unit).IsVisible)
                    failures.Add(FormattableString.Invariant($"{where} '{text}': {viewer.Extent.Width:0.#}/{viewer.Viewport.Width:0.#}, label {labelBlock.Bounds.Width:0.#}"));
                Key(box, Avalonia.Input.Key.Escape);
                Settle(window);
            }
            foreach (double dock in new[] { 200d, 260d, 300d })
            {
                host.Properties.Width = dock;
                Settle(window);
                foreach (var text in new[] { "#root_chord × 0.35", "(#span − 2 cm) / 2" })
                    Whole($"{dock:0}", Need<TextBox>(host.Properties, "TipChordInput"), "Label_w_tip", "TipChordUnit", text);
            }
            host.Properties.Width = 200;
            Settle(window);
            Whole("200 subhead", Need<TextBox>(host.Properties, "Input_h_tip_length"), "Label_h_tip_length", "Unit_h_tip_length", "#tip_chord × 0.1");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_WorstCaseNumbersNotClippedAt200", (controller, host, window) =>
        {
            // DC-2: at the 200 px dock the worst-case numbers fit the 62 px editor and the fact column unclipped.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            host.Properties.Width = 200;
            host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = 3.8125 });
            Settle(window);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            double room = Presenter(aft).Bounds.Width;
            var failures = new List<string>();
            foreach (var text in new[] { "−1234.56", "12000.00", "−89.99" })
            {
                var probe = new TextBlock { Text = text, FontFamily = aft.FontFamily, FontSize = aft.FontSize, FontFeatures = aft.FontFeatures };
                probe.Measure(Size.Infinity);
                Console.WriteLine(FormattableString.Invariant($"MEASURE DC-2 '{text}' {probe.DesiredSize.Width:0.#} of {room:0.#} px"));
                if (probe.DesiredSize.Width > room + 0.01) failures.Add(FormattableString.Invariant($"'{text}' {probe.DesiredSize.Width:0.#} > {room:0.#}"));
            }
            var area = Need<TextBlock>(host.Properties, "AreaEstimateText");
            Console.WriteLine(FormattableString.Invariant($"MEASURE DC-2 area '{area.Text}' {area.Bounds.Width:0.#}×{area.Bounds.Height:0.#}"));
            if (area.Text != "≈ 38125" || area.Bounds.Height > 14.5 || area.DesiredSize.Width > area.Bounds.Width + 0.01)
                failures.Add($"area '{area.Text}' {area.Bounds}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs", (controller, host, window) =>
        {
            // DC-3: a fact's digits end where an editable value's digits end (the same 5 px right inset).
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var presenter = Presenter(Need<TextBox>(host.Properties, "PointAftInput"));
            var fact = Need<TextBlock>(host.Properties, "Value_p_eta");
            double Right(Visual visual, Avalonia.Media.TextFormatting.TextLayout layout, double left, string text) =>
                visual.TranslatePoint(new Point(left + layout.HitTestTextRange(0, text.Length).Max(rect => rect.Right), 0), host.Properties)!.Value.X;
            double input = Right(presenter, presenter.TextLayout, 0, presenter.Text ?? "");
            double facts = Right(fact, fact.TextLayout, fact.Padding.Left, fact.Text ?? "");
            Console.WriteLine(FormattableString.Invariant($"MEASURE DC-3 input digits end {input:0.##}, fact digits end {facts:0.##}"));
            if (Math.Abs(input - facts) > 0.5) throw new InvalidOperationException(FormattableString.Invariant($"input ends {input:0.##}, fact {facts:0.##}"));
        });

        Pane("PropertiesPane_Density_ValuesUseTabularFigures", (controller, host, window) =>
        {
            // Values set tabular lining figures, so digits are one width and decimals line up down a column.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (var (name, features) in new[] { ("PointAftInput", Need<TextBox>(host.Properties, "PointAftInput").FontFeatures),
                         ("Value_p_eta", Need<TextBlock>(host.Properties, "Value_p_eta").FontFeatures) })
                if (features?.Any(feature => feature.Tag == "tnum" && feature.Value == 1) != true) failures.Add(name + " lacks tnum");
            double Width(string text)
            {
                var aft = Need<TextBox>(host.Properties, "PointAftInput");
                var probe = new TextBlock { Text = text, FontSize = 11, FontFamily = aft.FontFamily, FontFeatures = aft.FontFeatures };
                probe.Measure(Size.Infinity);
                return probe.DesiredSize.Width;
            }
            if (Math.Abs(Width("1111.11") - Width("0000.00")) > 0.01) failures.Add($"1111.11 is {Width("1111.11")}, 0000.00 is {Width("0000.00")}");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_HeaderMenuCopiesEachRow_NoRowContextMenus", (controller, host, window) =>
        {
            // DN-3: no row context menus; the group header's menu copies the group, or one row with or without its unit.
            Select(controller, window, MakeAnchor(controller));
            var withMenus = VisibleRows(host).Where(row => row.ContextMenu is not null).Select(row => row.Name).ToList();
            if (withMenus.Count > 0) throw new InvalidOperationException("row menus: " + string.Join(", ", withMenus));
            var menu = Need<Expander>(host.Properties, "Group_pos").ContextMenu!;
            var items = menu.Items.OfType<MenuItem>().ToDictionary(item => item.Header as string ?? "", StringComparer.Ordinal);
            foreach (var want in new[] { "Copy values", "Copy Type", "Copy From root", "Copy From root with unit", "Copy η", "Copy Aft", "Copy Aft with unit", "Copy Tangent kind" })
                if (!items.ContainsKey(want)) throw new InvalidOperationException($"no '{want}' in: {string.Join(" / ", items.Keys)}");
            var copied = new List<string>();
            host.Properties.ClipboardWriter = text => { copied.Add(text); return Task.CompletedTask; };
            foreach (var header in new[] { "Copy Aft with unit", "Copy Type" })
                items[header].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            string aft = Quantity.TypedLength(Reload(controller, PropertiesViewTests.Control(controller, "trailing")).AftMeters);
            if (copied.Count != 2 || !copied[0].EndsWith(" mm", StringComparison.Ordinal) || copied[1] != "Anchor point")
                throw new InvalidOperationException("copied: " + string.Join(" | ", copied) + " (aft " + aft + ")");
        });

        Pane("PropertiesPane_Density_EveryTargetAtLeast24", (controller, host, window) =>
        {
            // DR-DEN-1 / SC 2.5.8: every pointer target in the pane is at least 24 px high — values, enums, headers, the
            // definitions link and the crumb link.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            var small = new List<string>();
            foreach (var point in new[] { anchor, handle })
            {
                Select(controller, window, point);
                foreach (var target in host.Properties.GetVisualDescendants().OfType<InputElement>()
                             .Where(item => item.Focusable && item.IsEffectivelyVisible && item.IsEffectivelyEnabled && item is not ScrollViewer))
                    if (target.Bounds.Height < 23.5) small.Add($"{target.Name ?? target.GetType().Name} {target.Bounds.Height:0.#}");
            }
            if (small.Count > 0) throw new InvalidOperationException("under 24 px: " + string.Join(", ", small.Distinct()));
        });

        Pane("PropertiesPane_Density_DefinitionsIsLinkDisclosure", (controller, host, window) =>
        {
            // DR-CELL-5 / PG-31: "Estimates · definitions" is a link (accent, solid underline) and a disclosure whose
            // checked state is its expanded state, 24 px high.
            var link = Need<ToggleButton>(host.Properties, "HowMeasuredButton");
            var text = link.GetVisualDescendants().OfType<TextBlock>().Single();
            var underline = text.TextDecorations?.SingleOrDefault();
            if (text.Text != "Estimates · definitions" || !link.Classes.Contains("prop-link") || underline?.Location != TextDecorationLocation.Underline ||
                underline.StrokeDashArray is { Count: > 0 } || Paint(text.Foreground) != Resolved(window, "PrimaryBrush") || link.Bounds.Height < 23.5)
                throw new InvalidOperationException($"'{text.Text}' underline {underline?.Location} dashed {underline?.StrokeDashArray?.Count} h {link.Bounds.Height}");
            var toggle = ControlAutomationPeer.CreatePeerForElement(link).GetProvider<IToggleProvider>() ?? throw new InvalidOperationException("no toggle pattern");
            link.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            if (toggle.ToggleState != ToggleState.On || !Need<TextBlock>(host.Properties, "HowMeasuredBody").IsVisible)
                throw new InvalidOperationException("opening the definitions did not expose expanded");
        });

        Pane("PropertiesPane_Density_StackedRowsAtLargeText", (controller, host, window) =>
        {
            // DN-5: at 150 % and 200 % (set through the Text size setting) the value drops under its label, right-aligned.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (double scale in new[] { 1, 1.5, 2 })
            {
                host.SetTextScale(scale);
                Settle(window);
                var label = Need<TextBlock>(host.Properties, "Label_p_aft");
                var aft = Need<TextBox>(host.Properties, "PointAftInput");
                var row = Need<Border>(host.Properties, "Row_p_aft");
                double labelBottom = label.TranslatePoint(new Point(0, label.Bounds.Height), row)!.Value.Y;
                double valueTop = aft.TranslatePoint(default, row)!.Value.Y;
                bool stacked = valueTop >= labelBottom - 0.5;
                Console.WriteLine(FormattableString.Invariant($"MEASURE stacked {scale:0.##}: label bottom {labelBottom:0.#}, value top {valueTop:0.#}, row {row.Bounds.Height:0.#}"));
                if (stacked != scale >= 1.5 || host.Properties.Classes.Contains("prop-stacked") != scale >= 1.5)
                    failures.Add($"{scale}: stacked {stacked}");
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Density_FocusedWingFieldInViewAtLargeText", (controller, host, window) =>
        {
            // DR-DEN-2: at 200 % (through the setting) the Wing scrolls inside itself, and a focused field and its message
            // line are brought into view after a commit.
            host.SetTextScale(2);
            Select(controller, window, MakeAnchor(controller));
            var tip = Need<TextBox>(host.Properties, "TipChordInput");
            var scroll = Need<ScrollViewer>(host.Properties, "WingScroll");
            scroll.Offset = default;
            tip.Focus();
            tip.Text = "abc";
            Key(tip, Avalonia.Input.Key.Enter);
            Settle(window);
            var message = Need<TextBlock>(host.Properties, "Message_w_tip");
            double top = tip.TranslatePoint(default, scroll)!.Value.Y;
            double bottom = message.TranslatePoint(new Point(0, message.Bounds.Height), scroll)!.Value.Y;
            Console.WriteLine(FormattableString.Invariant($"MEASURE Wing at 200 %: tip {top:0.#}..{bottom:0.#} in {scroll.Viewport.Height:0.#} (extent {scroll.Extent.Height:0.#})"));
            if (!message.IsEffectivelyVisible || top < -0.5 || bottom > scroll.Viewport.Height + 0.5)
                throw new InvalidOperationException(FormattableString.Invariant($"tip {top:0.#}..{bottom:0.#} outside 0..{scroll.Viewport.Height:0.#}"));
        });

        // ---------------- the Text size setting (DN-5, DR-DEN-4) ----------------

        Pane("TextSize_Setting_ScalesEveryPropToken", (controller, host, window) =>
        {
            // One multiplier scales every Prop type and row token; the rows follow it.
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            var failures = new List<string>();
            foreach (double scale in new[] { 1.25, 1.5, 2, 1 })
            {
                host.SetTextScale(scale);
                Settle(window);
                foreach (var key in PropertiesPane.ScaledTokens)
                {
                    double base_ = Application.Current!.TryFindResource(key, out var value) && value is double number ? number : double.NaN;
                    if (!host.Properties.TryFindResource(key, out var scaled) || scaled is not double got || Math.Abs(got - base_ * scale) > 1e-9)
                        failures.Add($"{scale}/{key}");
                }
                if (Math.Abs(Need<TextBlock>(host.Properties, "Label_p_aft").FontSize - 11 * scale) > 1e-9 ||
                    Math.Abs(Need<Border>(host.Properties, "Row_p_eta").Bounds.Height - 20 * scale) > 0.5 && scale < 1.5)
                    failures.Add($"{scale}: label {Need<TextBlock>(host.Properties, "Label_p_aft").FontSize}, η row {Need<Border>(host.Properties, "Row_p_eta").Bounds.Height}");
            }
            if (failures.Count > 0) throw new InvalidOperationException("not scaled: " + string.Join(", ", failures));
        });

        Pane("TextSize_CommandPlusMinus_StepsLadder_Clamps100To200", (controller, host, window) =>
        {
            // DN-5: Bigger and Smaller step 100 · 125 · 150 · 200 % and stop at both ends; each change is announced.
            var seen = Changes(Status(host));
            var steps = new List<double>();
            foreach (var id in Enumerable.Repeat("view.text-bigger", 5).Concat(Enumerable.Repeat("view.text-smaller", 5)))
            {
                host.RunCommand(id).GetAwaiter().GetResult();
                steps.Add(host.TextScale);
            }
            string got = string.Join(",", steps.Select(step => step.ToString(Inv)));
            if (got != "1.25,1.5,2,2,2,1.5,1.25,1,1,1" || !seen.Contains("Text size 200 %.") || !seen.Contains("Text size 100 %."))
                throw new InvalidOperationException($"ladder {got}; status {string.Join(" | ", seen)}");
            host.RunCommand("view.text-150").GetAwaiter().GetResult();
            if (host.TextScale != 1.5) throw new InvalidOperationException("150 % set " + host.TextScale);
        });

        Pane("TextSize_ModelViewKeepsZoomShortcut", (controller, host, window) =>
        {
            // DR-DEN-4 (ruled): with a model view focused ⌘= / ⌘− zoom it; anywhere else they change the Text size.
            var canvas = host.ModelView.FindControl<PlanCanvas>("PlanCanvas")!;
            canvas.Focus();
            double zoom = controller.PlanCamera.PixelsPerMeter;
            host.RunCommand("view.zoom-in").GetAwaiter().GetResult();
            if (controller.PlanCamera.PixelsPerMeter <= zoom || host.TextScale != 1)
                throw new InvalidOperationException($"in the Plan view: zoom {controller.PlanCamera.PixelsPerMeter}, text {host.TextScale}");
            Select(controller, window, PropertiesViewTests.Control(controller, "trailing"));
            Need<TextBox>(host.Properties, "PointAftInput").Focus();
            zoom = controller.PlanCamera.PixelsPerMeter;
            host.RunCommand("view.zoom-in").GetAwaiter().GetResult();
            if (controller.PlanCamera.PixelsPerMeter != zoom || host.TextScale != 1.25)
                throw new InvalidOperationException($"in Properties: zoom {controller.PlanCamera.PixelsPerMeter}, text {host.TextScale}");
            host.RunCommand("view.zoom-out").GetAwaiter().GetResult();
            if (host.TextScale != 1) throw new InvalidOperationException("⌘− in Properties did not step back: " + host.TextScale);
        });

        Capture();
    }

    // ---------------- helpers ----------------

    /// <summary>A theme brush's colour as the window resolves it now.</summary>
    private static Color Resolved(Window window, string key) =>
        window.TryFindResource(key, window.ActualThemeVariant, out var value) && value is ISolidColorBrush brush ? brush.Color : default;

    /// <summary>A brush's colour; transparent when there is none.</summary>
    private static Color Paint(IBrush? brush) => brush is ISolidColorBrush solid && solid.Opacity > 0 ? solid.Color : Colors.Transparent;

    private static List<Border> VisibleRows(ShellHost host) =>
        host.Properties.GetVisualDescendants().OfType<Border>().Where(row => row.Classes.Contains("prop-row") && row.IsEffectivelyVisible).ToList();

    private static TextPresenter Presenter(TextBox box) => box.GetVisualDescendants().OfType<TextPresenter>().Single();

    private static string? Selected(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString();

    /// <summary>A left-button press on <paramref name="target"/>, as a pointer would raise it.</summary>
    private static void Press(Window window, Control target) =>
        target.RaiseEvent(new PointerPressedEventArgs(target, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true), window,
            new Point(1, 1), 0, new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));

    private static ComboBox OpenKind(ShellHost host, Window window)
    {
        var kind = Need<ComboBox>(host.Properties, "KindControl");
        kind.Focus();
        kind.IsDropDownOpen = true;
        Settle(window);
        return kind;
    }

    /// <summary>What an arrow does in the open list: focus moves to the next item. Returns that item's kind.</summary>
    private static string ArrowInList(ComboBox kind, Window window)
    {
        var items = kind.GetVisualDescendants().OfType<Popup>().First().Child!.GetVisualDescendants().OfType<ComboBoxItem>().ToList();
        Key(kind, Avalonia.Input.Key.Down);
        Settle(window);
        return items.FirstOrDefault(item => item.IsFocused)?.Content?.ToString() ?? throw new InvalidOperationException("no item has focus in the open list");
    }

    // ---------------- measuring ----------------

    internal readonly record struct CueMeasure(int Length, int On, int Gaps, double Contrast);

    /// <summary>Renders the window at <paramref name="scale"/> and reads the underline's pixel row against the surface beside it.</summary>
    internal static CueMeasure MeasureCue(Window window, TextBox box, int scale)
    {
        var cue = box.GetVisualDescendants().OfType<PropertiesPane.EditCue>().SingleOrDefault()
            ?? throw new InvalidOperationException("no underline part in " + box.Name);
        if (!cue.IsEffectivelyVisible || cue.Line() is not { } line) return default;
        var start = cue.TranslatePoint(line.Start, window)!.Value;
        var end = cue.TranslatePoint(line.End, window)!.Value;
        using var pixels = Render(window, scale);
        int row = (int)Math.Floor((start.Y - 0.5) * scale);
        int x0 = (int)Math.Round(start.X * scale), x1 = (int)Math.Round(end.X * scale);
        var surface = pixels.At(x0 - 3 * scale, row);
        int on = 0, gaps = 0;
        double best = 1;
        for (int x = x0; x < x1; x++)
        {
            double contrast = Contrast(pixels.At(x, row), surface);
            best = Math.Max(best, contrast);
            if (contrast >= 3) on++;
            else if (contrast < 1.2) gaps++;
        }
        return new CueMeasure(x1 - x0, on, gaps, best);
    }

    internal static Pixels Render(Visual visual, int scale)
    {
        var size = new PixelSize((int)(visual.Bounds.Width * scale), (int)(visual.Bounds.Height * scale));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        bitmap.Render(visual);
        var writeable = new WriteableBitmap(size, new Vector(96 * scale, 96 * scale), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
        using (var frame = writeable.Lock()) bitmap.CopyPixels(frame, AlphaFormat.Unpremul);
        return new Pixels(writeable);
    }

    internal sealed class Pixels(WriteableBitmap bitmap) : IDisposable
    {
        public WriteableBitmap Bitmap { get; } = bitmap;

        public Color At(int x, int y)
        {
            using var frame = Bitmap.Lock();
            int offset = y * frame.RowBytes + x * 4;
            return Color.FromRgb(Marshal.ReadByte(frame.Address, offset + 2), Marshal.ReadByte(frame.Address, offset + 1),
                Marshal.ReadByte(frame.Address, offset));
        }

        public void Dispose() => Bitmap.Dispose();
    }

    /// <summary>WCAG 2.2 contrast ratio of two opaque colours.</summary>
    internal static double Contrast(Color a, Color b)
    {
        static double Channel(byte value)
        {
            double c = value / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    // ---------------- captures (B2-2): only when CFDW_PROPERTIES_CAPTURE names a directory ----------------

    private static void Capture()
    {
        if (Environment.GetEnvironmentVariable("CFDW_PROPERTIES_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        Pane("Capture_PropertiesPane_B", (controller, host, window) =>
        {
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id && point.Index > anchor.Index);
            var states = new (string Name, Action Enter)[]
            {
                ("anchor", () => { Select(controller, window, anchor); Need<TextBox>(host.Properties, "PointAftInput").Focus(); }),
                ("handle", () => Select(controller, window, handle)),
                ("field-error", () =>
                {
                    Select(controller, window, anchor);
                    var aft = Need<TextBox>(host.Properties, "PointAftInput");
                    aft.Focus();
                    aft.Text = "abc";
                    Key(aft, Avalonia.Input.Key.Enter);
                }),
                ("unavailable", () => { Select(controller, window, anchor); host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN }); })
            };
            foreach (var (variant, theme) in Themes)
            {
                window.RequestedThemeVariant = variant;
                foreach (var (name, enter) in states)
                {
                    enter();
                    Settle(window);
                    foreach (int scale in new[] { 1, 2 })
                    {
                        using var pixels = Render(host.Properties, scale);
                        pixels.Bitmap.Save(System.IO.Path.Combine(directory, $"b-{name}-{theme}-{scale}x.png"));
                    }
                }
            }
        });
    }
}
