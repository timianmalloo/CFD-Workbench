using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Panes;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// The property grid's named tests (docs/reviews/ui-property-grid.md §10.7, N1 verbatim, and B3/B5/B6). Each name says
/// what it protects; model checks run headless without a window, pane checks on the shell at 1280 × 800.
/// </summary>
public static class PropertiesViewTests
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Run()
    {
        // ---------------- the model (PropertiesView.cs) ----------------

        DesktopChecks.Check("PropertiesView_EveryQuantityRow_HasUnitOrIsDimensionless", () =>
        {
            // O-4: every number has a unit or is declared dimensionless, in every selection the model fills.
            using var controller = Opened();
            var failures = new List<string>();
            foreach (var (name, model) in EverySelection(controller))
                foreach (var row in model.Blocks.SelectMany(group => group.Rows))
                {
                    bool number = double.TryParse(row.Value.Replace(Quantity.Minus, '-'), NumberStyles.Float, Inv, out _) ||
                                  row.State is RowState.Mixed or RowState.Unavailable && (row.Kind == RowKind.Estimate || row.Family != UnitFamily.None);
                    if ((row.Kind == RowKind.Input || number && row.Kind is RowKind.Fact or RowKind.Estimate) &&
                        row.Unit is null && !row.Dimensionless)
                        failures.Add($"{name}/{row.Key} '{row.Value}'");
                }
            if (failures.Count > 0) throw new InvalidOperationException("no unit: " + string.Join(", ", failures));
        });

        DesktopChecks.Check("PropertiesView_Formatter_PrecisionFollowsQuantity", () =>
        {
            // UI-40 as amended by DR-UID-1: precision follows the quantity, not the row (§10.2 table).
            var expected = new (string Name, string Actual, string Want)[]
            {
                ("typed length", Quantity.TypedLength(1.0), "1000.00"),
                ("typed length small", Quantity.TypedLength(0.01267), "12.67"),
                ("derived length", Quantity.DerivedLength(0.10712), "107.1"),
                ("placed angle", Quantity.PlacedAngle(-21.08), "−21.08"),
                ("derived angle", Quantity.DerivedAngle(3.14159), "3.1"),
                ("placed t/c", Quantity.PlacedPercent(12.35), "12.35"),
                ("max t/c", Quantity.MaxThickness(0.12), "12.0"),
                ("AR", Quantity.AspectRatio(10.0412), "10.04"),
                ("area", Quantity.AreaSquareCentimetres(0.09961), "996"),
                ("eta", Quantity.Eta(0.6431), "0.643"),
                ("delta", Quantity.Delta(1), "+1.00"),
                ("no negative zero", Quantity.TypedLength(-0.000001), "0.00")
            };
            var wrong = expected.Where(item => item.Actual != item.Want).Select(item => $"{item.Name} '{item.Actual}' ≠ '{item.Want}'").ToList();
            using var controller = Opened();
            var model = Build(controller, new Selection.Points([Ref(Control(controller, "trailing"))]));
            string Value(string key) => model.Blocks.SelectMany(group => group.Rows).First(row => row.Key == key).Value;
            if (Decimals(Value("w:span")) != 2) wrong.Add("Span " + Value("w:span"));
            if (Decimals(Value("e:mac")) != 1) wrong.Add("MAC " + Value("e:mac"));
            if (Decimals(Value("e:ar")) != 2) wrong.Add("AR " + Value("e:ar"));
            if (Decimals(Value("e:area")) != 0) wrong.Add("Area " + Value("e:area"));
            if (Decimals(Value("p:eta")) != 3) wrong.Add("η " + Value("p:eta"));
            if (Decimals(Value("p:aft")) != 2) wrong.Add("Aft " + Value("p:aft"));
            if (wrong.Count > 0) throw new InvalidOperationException(string.Join("; ", wrong));
        });

        DesktopChecks.Check("PropertiesView_AngleField_AcceptsDegreeExpression", () =>
        {
            // MC-4: angles take ° · deg · rad and arithmetic; the echo is in degrees.
            var none = new Dictionary<string, double>();
            var cases = new (string Text, double Want)[] { ("−21°", -21), ("3 deg", 3), ("0.35 rad", 0.35 * 180 / Math.PI), ("1+2", 3), ("(1 + 2) × 2", 6) };
            foreach (var (text, want) in cases)
                if (!UnitEntry.TryParse(text, UnitFamily.Angle, none, out var entry) || Math.Abs(entry.Value - want) > 1e-9)
                    throw new InvalidOperationException($"'{text}' parsed to {(UnitEntry.TryParse(text, UnitFamily.Angle, none, out var got) ? got.Value : double.NaN)}");
            UnitEntry.TryParse("0.35 rad", UnitFamily.Angle, none, out var radians);
            if (UnitEntry.Echo("0.35 rad", radians, "°") != "0.35 rad = 20.05°.")
                throw new InvalidOperationException("echo: " + UnitEntry.Echo("0.35 rad", radians, "°"));
            foreach (var refused in new[] { "5 mm", "#span", "abc", "1/0", "" })
                if (UnitEntry.TryParse(refused, UnitFamily.Angle, none, out _))
                    throw new InvalidOperationException($"'{refused}' was accepted as an angle");
        });

        DesktopChecks.Check("PropertiesView_ReferenceExpression_EchoSaysSetOnce", () =>
        {
            // MC-3: an expression with a reference is set once and says so (COPY-161); a unit expression echoes (COPY-157).
            var dims = new Dictionary<string, double> { ["span"] = 1.0, ["root_chord"] = 0.1267, ["tip_chord"] = 0.05 };
            if (!UnitEntry.TryParse("#root_chord × 0.1", UnitFamily.Length, dims, out var entry) || Math.Abs(entry.Value - 12.67) > 1e-9)
                throw new InvalidOperationException("reference value " + entry.Value);
            string? echo = UnitEntry.Echo("#root_chord × 0.1", entry, "mm");
            if (echo != "#root_chord × 0.1 = 12.67 mm (set once; doesn’t follow Root chord)")
                throw new InvalidOperationException("reference echo: " + echo);
            UnitEntry.TryParse("15 cm", UnitFamily.Length, dims, out var centimetres);
            if (UnitEntry.Echo("15 cm", centimetres, "mm") != "15 cm = 150.00 mm.")
                throw new InvalidOperationException("unit echo: " + UnitEntry.Echo("15 cm", centimetres, "mm"));
            UnitEntry.TryParse("150", UnitFamily.Length, dims, out var plain);
            if (UnitEntry.Echo("150", plain, "mm") is not null) throw new InvalidOperationException("a plain number echoed");
        });

        DesktopChecks.Check("PropertiesView_HandleSelection_OwnIdentity_ParentTangentRow", () =>
        {
            // O-6, F-4: a handle is named as a handle with its anchor as a link, and shows (and edits) the anchor's kind.
            using var controller = Opened();
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id && point.Index > anchor.Index);
            var model = Build(controller, new Selection.Points([Ref(handle)]));
            var identity = model.Identity!;
            if (identity.Glyph != IdentityGlyph.Handle || identity.Title != "Handle toward the tip")
                throw new InvalidOperationException($"identity {identity.Glyph} '{identity.Title}'");
            if (identity.CrumbTarget?.VertexId != anchor.Id || identity.Crumb?.Contains($"anchor point {anchor.Index + 1}", StringComparison.Ordinal) != true)
                throw new InvalidOperationException($"crumb '{identity.Crumb}' → {identity.CrumbTarget}");
            var kind = model.Groups.SelectMany(group => group.Rows).Single(row => row.Key == "t:kind");
            if (kind.Kind != RowKind.KindList || kind.Value != anchor.Kind.ToString() || kind.Target?.VertexId != anchor.Id ||
                kind.AutomationName != $"Tangent kind of anchor point {anchor.Index + 1}")
                throw new InvalidOperationException($"parent kind row {kind.Kind} {kind.Value} {kind.AutomationName}");
            if (model.Groups.SelectMany(group => group.Rows).Any(row => row.Key == "p:type"))
                throw new InvalidOperationException("a handle repeats its anchor's Type row");
        });

        DesktopChecks.Check("PropertiesView_RowSet_FollowsCurveTable", () =>
        {
            // MC-17 (rails): handles by angle + length; an anchor's handle rows follow its kind. Twist and t/c rows are
            // deferred to PNL / CH1 (Core publishes no channel points yet).
            using var controller = Opened();
            var anchor = MakeAnchor(controller);
            string Keys(Selection selection) => string.Join(",", Build(controller, selection).Groups.SelectMany(group => group.Rows)
                .Where(row => row.Kind is RowKind.Input or RowKind.KindList).Select(row => row.Key + ":" + row.Unit));
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            var checks = new List<(string Name, string Actual, string Want)>
            {
                ("control", Keys(new Selection.Points([Ref(Control(controller, "leading"))])), "p:from:mm,p:aft:mm"),
                ("handle", Keys(new Selection.Points([Ref(handle)])), "h:angle:°,h:length:mm,t:kind:")
            };
            foreach (var (kind, want) in new[] {
                (TangentKind.Smooth, "p:from:mm,p:aft:mm,t:kind:,h:angle:°,h:root-length:mm,h:tip-length:mm"),
                (TangentKind.Symmetric, "p:from:mm,p:aft:mm,t:kind:,h:angle:°,h:length:mm"),
                (TangentKind.Corner, "p:from:mm,p:aft:mm,t:kind:,h:root-angle:°,h:root-length:mm,h:tip-angle:°,h:tip-length:mm") })
            {
                Pump(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(anchor.Curve, anchor.Id, kind, null)));
                checks.Add((kind.ToString(), Keys(new Selection.Points([Ref(anchor)])), want));
            }
            var position = Build(controller, new Selection.Points([Ref(anchor)])).Groups.First(group => group.Id == "pos").Rows;
            if (position.First(row => row.Key == "p:from").Label != "From root" || position.All(row => row.Key != "p:eta"))
                checks.Add(("labels", string.Join(",", position.Select(row => row.Label)), "From root with η"));
            var wrong = checks.Where(check => check.Actual != check.Want).Select(check => $"{check.Name}: {check.Actual}").ToList();
            if (wrong.Count > 0) throw new InvalidOperationException(string.Join("; ", wrong));
        });

        DesktopChecks.Check("PropertiesView_OneNonFiniteEstimate_OthersStillShown", () =>
        {
            // BLANK-ESTIMATE: one non-finite MAC makes MAC Unavailable, never its neighbours; the note names it.
            using var controller = Opened();
            var estimates = controller.Estimates! with { MacMeters = double.NaN };
            var wing = PropertiesView.Build(new Selection.Foil(), controller.CurrentProjection, estimates, ShellMode.Workspace).Wing!;
            var estimateRows = wing.Rows.Where(row => row.Kind == RowKind.Estimate).ToList();
            var unavailable = estimateRows.Where(row => row.State == RowState.Unavailable).Select(row => row.Key).ToList();
            if (!unavailable.SequenceEqual(["e:mac"]))
                throw new InvalidOperationException("unavailable: " + string.Join(",", unavailable));
            if (estimateRows.Where(row => row.Key != "e:mac").Any(row => row.Value is "Unavailable" or "" || row.Value.Contains('—')))
                throw new InvalidOperationException("a neighbour of MAC is blank");
            if (wing.Notes.SingleOrDefault()?.Text.StartsWith("Unavailable — MAC", StringComparison.Ordinal) != true || wing.Chip is not null)
                throw new InvalidOperationException($"note '{wing.Notes.FirstOrDefault()?.Text}' chip {wing.Chip}");
        });

        DesktopChecks.Check("PropertiesView_TypedTcBelowOnePercent_WarnsFractionHint", () =>
        {
            // MC-20 (grammar): a bare 0.12 is 0.12 %, never silently 12 %, and it warns with COPY-169. The t/c row's commit
            // is PNL's (no thickness channel in Core yet).
            var none = new Dictionary<string, double>();
            if (!UnitEntry.TryParse("0.12", UnitFamily.Percent, none, out var bare) || bare.Value != 0.12)
                throw new InvalidOperationException("0.12 became " + bare.Value);
            if (UnitEntry.Hint(bare, UnitFamily.Percent)?.Text != "0.12 % — for 12 %, type 12 or 0.12 × 100.")
                throw new InvalidOperationException("hint: " + UnitEntry.Hint(bare, UnitFamily.Percent)?.Text);
            if (!UnitEntry.TryParse("0.12 × 100", UnitFamily.Percent, none, out var scaled) || Math.Abs(scaled.Value - 12) > 1e-9 ||
                UnitEntry.Hint(scaled, UnitFamily.Percent) is not null)
                throw new InvalidOperationException("0.12 × 100 became " + scaled.Value);
        });

        DesktopChecks.Check("Fact_And_Estimate_NameHasUnit", () =>
        {
            // N1 / PG-01: a fact or estimate speaks its unit; abbreviations are spoken in full (PG-24).
            using var controller = Opened();
            var failures = new List<string>();
            foreach (var (name, model) in EverySelection(controller))
                foreach (var row in model.Blocks.SelectMany(group => group.Rows).Where(row => row.Kind is RowKind.Fact or RowKind.Estimate))
                {
                    string spoken = row.SpokenText ?? "";
                    if (!spoken.StartsWith(Speech.Label(row.Label), StringComparison.Ordinal)) failures.Add($"{name}/{row.Key} label: {spoken}");
                    if (row.Unit is not null && row.State is RowState.Normal or RowState.Locked && !spoken.Contains(Speech.Unit(row.Unit), StringComparison.Ordinal))
                        failures.Add($"{name}/{row.Key} unit: {spoken}");
                }
            var ar = Build(controller, new Selection.Foil()).Wing!.Rows.First(row => row.Key == "e:ar").SpokenText;
            if (ar?.StartsWith("AR, aspect ratio, approximately ", StringComparison.Ordinal) != true || !ar.EndsWith("b squared over S", StringComparison.Ordinal))
                failures.Add("AR: " + ar);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        DesktopChecks.Check("PropertiesPane_GridBrushes_InAllThreeThemes", () =>
        {
            // §10.6 / PG-11: the grid's four brushes exist in Light, Dark and High contrast with DESIGN.md's values.
            var styles = (Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://CfdWorkbench.Desktop/Styles.axaml"), null);
            var failures = new List<string>();
            foreach (var (key, light, dark, contrast) in new[] {
                ("ControlLineBrush", "#788d87", "#79928c", "#ffffff"), ("WarningBrush", "#895900", "#efc576", "#ffee58"),
                ("SelectionBrush", "#d8eeea", "#274c47", "#ffee58"), ("FocusRingBrush", "#006c67", "#88d8c6", "#ffee58") })
                foreach (var (variant, want) in new[] { (Avalonia.Styling.ThemeVariant.Light, light), (Avalonia.Styling.ThemeVariant.Dark, dark), (NativeReviewThemes.HighContrast, contrast) })
                    if (!styles.TryGetResource(key, variant, out var value) || value is not ISolidColorBrush brush || brush.Color != Color.Parse(want))
                        failures.Add($"{variant.Key}/{key}");
            if (failures.Count > 0) throw new InvalidOperationException("missing or off-token: " + string.Join(", ", failures));
        });

        // ---------------- the pane (PropertiesPane.axaml / .cs) ----------------

        Pane("PropertiesPane_EstimatesUnavailable_ShowReasonNeverDash", (controller, host, window) =>
        {
            // D-4's symptom: no "≈ —"; each estimate reads Unavailable and the group says why (COPY-155).
            host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN });
            Settle(window);
            var values = new[] { "MeanChordText", "MacText", "MaxTcText", "AspectText", "AreaEstimateText" }.Select(name => Text(host.Properties, name)).ToList();
            if (values.Any(value => value != "Unavailable")) throw new InvalidOperationException("values: " + string.Join(" | ", values));
            var note = Need<TextBlock>(host.Properties, "Note_wing_0");
            if (!note.IsEffectivelyVisible || note.Text?.StartsWith("Unavailable — ", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("reason: " + note.Text);
            if (Text(host.Properties, "WingChipText") != "Unavailable") throw new InvalidOperationException("chip: " + Text(host.Properties, "WingChipText"));
        });

        Pane("Unavailable_AnnouncedInStatus", (controller, host, window) =>
        {
            // N1 / PG-04: the change to Unavailable is announced politely. The pane's polite region is the Wing note (the
            // shell's status line is the controller's; COPY-160 there is outside this track).
            host.Properties.Bind(controller, controller.Estimates! with { MacMeters = double.NaN });
            Settle(window);
            var note = Need<TextBlock>(host.Properties, "Note_wing_0");
            if (AutomationProperties.GetLiveSetting(note) != AutomationLiveSetting.Polite || note.Text?.Contains("MAC", StringComparison.Ordinal) != true)
                throw new InvalidOperationException($"live {AutomationProperties.GetLiveSetting(note)} '{note.Text}'");
        });

        Pane("PropertiesPane_Rows_ShareOneLabelColumn", (controller, host, window) =>
        {
            // F-1, UI-TRANSLATION-LOSS: one label column and one value edge for every row; B5: at the 200 px dock no label
            // or Kind option is truncated.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            foreach (double width in new[] { 260d, 200d })
            {
                host.Properties.Width = width;
                Settle(window);
                var grids = host.Properties.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("prop-row") && border.IsEffectivelyVisible)
                    .Select(border => (Grid)border.Child!).ToList();
                if (grids.Count < 8) throw new InvalidOperationException($"{width}: only {grids.Count} rows");
                var labelX = grids.Select(grid => grid.TranslatePoint(new Point(0, 0), host.Properties)!.Value.X).Distinct(new Near()).ToList();
                var valueX = grids.Select(grid => grid.TranslatePoint(new Point(grid.ColumnDefinitions[0].ActualWidth, 0), host.Properties)!.Value.X).Distinct(new Near()).ToList();
                var edge = grids.Select(grid => grid.TranslatePoint(new Point(grid.Bounds.Width - grid.ColumnDefinitions[2].ActualWidth, 0), host.Properties)!.Value.X).Distinct(new Near()).ToList();
                if (labelX.Count != 1 || valueX.Count != 1 || edge.Count != 1)
                    throw new InvalidOperationException($"{width}: label x {labelX.Count}, value x {valueX.Count}, value edge {edge.Count}: " + string.Join(" ", grids.Select(grid => (grid.Parent as Control)?.Name + "@" + grid.TranslatePoint(new Point(0, 0), host.Properties)!.Value.X.ToString("0.#", Inv) + "/" + grid.Bounds.Width.ToString("0.#", Inv) + "u" + grid.ColumnDefinitions[2].ActualWidth.ToString("0.#", Inv) + "l" + grid.ColumnDefinitions[0].ActualWidth.ToString("0.#", Inv))));
                var clipped = host.Properties.GetVisualDescendants().OfType<TextBlock>()
                    .Where(block => block.Classes.Contains("prop-label") && block.IsEffectivelyVisible && block.TextTrimming != TextTrimming.None)
                    .Select(block => block.Text).ToList();
                foreach (var option in host.Properties.GetVisualDescendants().OfType<RadioButton>().Where(radio => radio.IsEffectivelyVisible))
                    if (option.Bounds.Width + 0.5 < option.DesiredSize.Width) clipped.Add(option.Content?.ToString());
                if (clipped.Count > 0) throw new InvalidOperationException($"{width}: truncated " + string.Join(", ", clipped));
            }
        });

        foreach (double dock in new[] { 260d, 300d })
            Pane($"PropertiesPane_Wing_AlwaysFullyVisibleAt1280x800_{dock:0}", (controller, host, window) =>
            {
                // UI-36 as amended by DR-UID-5: the Wing is pinned and fully visible; the selection scrolls.
                var anchor = MakeAnchor(controller);
                var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
                var states = new (string Name, Action Enter)[]
                {
                    ("anchor", () => Select(controller, window, anchor)),
                    ("handle", () => Select(controller, window, handle)),
                    ("unavailable", () => { host.Properties.Bind(controller, controller.Estimates! with { AreaSquareMeters = double.NaN }); Settle(window); })
                };
                host.Properties.Width = dock;
                var failures = new List<string>();
                foreach (var (name, enter) in states)
                {
                    enter();
                    Settle(window);
                    var wing = Need<Border>(host.Properties, "WingBlock");
                    var scroll = Need<ScrollViewer>(host.Properties, "WingScroll");
                    var top = wing.TranslatePoint(new Point(0, 0), host.Properties)!.Value.Y;
                    if (scroll.Extent.Height > scroll.Viewport.Height + 0.5 || top < 0 || top + wing.Bounds.Height > host.Properties.Bounds.Height + 0.5)
                        failures.Add($"{name}: wing {scroll.Extent.Height:0}/{scroll.Viewport.Height:0} in pane {host.Properties.Bounds.Height:0}");
                }
                if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
            });

        Pane("PropertiesPane_NumberFields_AutomationNameHasLabelAndUnit", (controller, host, window) =>
        {
            // Finding #11: every number field's accessible name contains its visible label and its unit (SC 2.5.3).
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            var failures = new List<string>();
            foreach (var point in new[] { Control(controller, "leading"), anchor, handle })
            {
                Select(controller, window, point);
                foreach (var border in host.Properties.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("prop-row") && border.IsEffectivelyVisible))
                {
                    if (((Grid)border.Child!).Children.OfType<TextBox>().FirstOrDefault() is not { } box) continue;
                    string label = ((Grid)border.Child!).Children.OfType<TextBlock>().First().Text ?? "";
                    string name = AutomationProperties.GetName(box) ?? "";
                    if (!name.Contains(label, StringComparison.Ordinal) || !(name.Contains("millimetres", StringComparison.Ordinal) || name.Contains("degrees", StringComparison.Ordinal)))
                        failures.Add($"'{label}' → '{name}'");
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Tangent_CheckedKindExposed", (controller, host, window) =>
        {
            // Finding #12: the Kind group is a named group whose checked option is exposed, with position in set.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var group = Need<StackPanel>(host.Properties, "TangentGroup");
            if (AutomationProperties.GetName(group) != "Tangent kind" || AutomationProperties.GetControlTypeOverride(group) != AutomationControlType.Group)
                throw new InvalidOperationException($"group '{AutomationProperties.GetName(group)}' {AutomationProperties.GetControlTypeOverride(group)}");
            var radios = group.Children.OfType<RadioButton>().ToList();
            var checkedOnes = radios.Where(radio => radio.IsChecked == true).ToList();
            if (checkedOnes.Count != 1 || checkedOnes[0].Content?.ToString() != anchor.Kind.ToString())
                throw new InvalidOperationException("checked: " + string.Join(",", checkedOnes.Select(radio => radio.Content)));
            var peer = ControlAutomationPeer.CreatePeerForElement(checkedOnes[0]);
            if (peer.GetName() != anchor.Kind.ToString()) throw new InvalidOperationException("peer name " + peer.GetName());
            for (int index = 0; index < radios.Count; index++)
                if (AutomationProperties.GetPositionInSet(radios[index]) != index + 1 || AutomationProperties.GetSizeOfSet(radios[index]) != 3)
                    throw new InvalidOperationException("position in set on " + radios[index].Content);
        });

        Pane("PropertiesPane_FieldBlur_CommitsAndKeepsFocusTarget", (controller, host, window) =>
        {
            // UI-39, UI-C: leaving a field commits it (one undo row) and focus stays where it was sent.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            var from = Need<TextBox>(host.Properties, "PointSpanInput");
            aft.Focus();
            aft.Text = ((point.AftMeters + 0.002) * 1000).ToString("0.00", Inv);
            from.Focus();
            WaitIdle(controller, window);
            var moved = Reload(controller, point);
            if (Math.Abs(moved.AftMeters - point.AftMeters - 0.002) > 2e-6) throw new InvalidOperationException("blur did not commit: " + moved.AftMeters);
            if (!from.IsFocused) throw new InvalidOperationException("focus left the field it was sent to");
            controller.Undo();
            Settle(window);
            if (Math.Abs(Reload(controller, point).AftMeters - point.AftMeters) > 1e-6 || controller.CanUndo)
                throw new InvalidOperationException("the blur commit was not exactly one undo row");
        });

        Pane("PropertiesPane_KindArrows_MoveCheckOnly_OneUndoRowPerIntent", (controller, host, window) =>
        {
            // PG-06 = MC-1 (ruled deviation): arrows move the check only, no wrap; Return commits one undo row.
            var pane = host.Properties;   // the full shell: ShellHost must leave Return to the pane
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            string source = controller.AcceptedSource;
            var radios = Need<StackPanel>(pane, "TangentGroup").Children.OfType<RadioButton>().ToList();
            var start = radios.First(radio => radio.IsChecked == true);
            start.Focus();
            foreach (var _ in Enumerable.Range(0, 4)) Key(window.FocusManager!.GetFocusedElement() as Control ?? start, Avalonia.Input.Key.Down);
            Settle(window);
            if (controller.AcceptedSource != source || Reload(controller, anchor).Kind != anchor.Kind)
                throw new InvalidOperationException("an arrow committed the kind");
            if (radios[^1].IsChecked != true || !radios[^1].IsFocused) throw new InvalidOperationException("arrows did not stop at the last option (no wrap)");
            Key(radios[^1], Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind != TangentKind.Corner) throw new InvalidOperationException("Return did not commit Corner");
            controller.Undo();
            Settle(window);
            if (Reload(controller, anchor).Kind != anchor.Kind || controller.AcceptedSource != source)
                throw new InvalidOperationException("the kind intent was not exactly one undo row");
        });

        Pane("Tangent_KindChange_KeepsFocusOnChecked", (controller, host, window) =>
        {
            // N1: after a kind commit, focus stays on the (new) checked option.
            var pane = host.Properties;   // the full shell: ShellHost must leave Return to the pane
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var radios = Need<StackPanel>(pane, "TangentGroup").Children.OfType<RadioButton>().ToList();
            var start = radios.First(radio => radio.IsChecked == true);
            start.Focus();
            Key(start, Avalonia.Input.Key.Down);
            var target = window.FocusManager!.GetFocusedElement() as RadioButton ?? throw new InvalidOperationException("focus left the group");
            Key(target, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            var focused = window.FocusManager!.GetFocusedElement() as RadioButton;
            if (focused is null || focused.IsChecked != true || focused.Content?.ToString() != Reload(controller, anchor).Kind.ToString())
                throw new InvalidOperationException($"focus on {focused?.Content ?? "none"} after the commit");
        });

        Pane("Shell_ReturnInProperties_ReachesTheFocusedControl", (controller, host, window) =>
        {
            // §10.4 in the full shell: with a point selected, Return on the Type box, the Kind group and a group header
            // belongs to that control — the shell's "Return types a value" applies only outside Properties.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var header = Need<Expander>(host.Properties, "Group_pos").GetVisualDescendants().OfType<ToggleButton>().First();
            header.Focus();
            Key(header, Avalonia.Input.Key.Enter);
            Settle(window);
            if (Need<Expander>(host.Properties, "Group_pos").IsExpanded || !header.IsFocused)
                throw new InvalidOperationException($"Return on the header: expanded {Need<Expander>(host.Properties, "Group_pos").IsExpanded}, focus on header {header.IsFocused}");
            Key(header, Avalonia.Input.Key.Enter);
            Settle(window);
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            type.Focus();
            Key(type, Avalonia.Input.Key.Down);
            Key(type, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            var anchor = Reload(controller, point);
            if (anchor.Role != PointRole.Anchor || !type.IsFocused)
                throw new InvalidOperationException($"Return on a pending Type: role {anchor.Role}, focus on Type {type.IsFocused}");
            var radios = Need<StackPanel>(host.Properties, "TangentGroup").Children.OfType<RadioButton>().ToList();
            var start = radios.First(radio => radio.IsChecked == true);
            start.Focus();
            Key(start, Avalonia.Input.Key.Down);
            var pending = window.FocusManager!.GetFocusedElement() as RadioButton ?? throw new InvalidOperationException("focus left the Kind group");
            Key(pending, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind.ToString() != pending.Content?.ToString() || !pending.IsFocused)
                throw new InvalidOperationException($"Return on a pending Kind: kind {Reload(controller, anchor).Kind}, focus on it {pending.IsFocused}");
        });

        Pane("PropertiesPane_KindLeave_CommitsPendingOnce", (controller, host, window) =>
        {
            // PG-06 = MC-1 as ruled: leaving the Kind group with a pending kind commits it, as one undo row.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            string source = controller.AcceptedSource;
            var radios = Need<StackPanel>(host.Properties, "TangentGroup").Children.OfType<RadioButton>().ToList();
            var start = radios.First(radio => radio.IsChecked == true);
            start.Focus();
            Key(start, Avalonia.Input.Key.Down);
            var pending = (window.FocusManager!.GetFocusedElement() as RadioButton)?.Content?.ToString();
            if (controller.AcceptedSource != source) throw new InvalidOperationException("the arrow committed");
            Need<TextBox>(host.Properties, "PointAftInput").Focus();
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind.ToString() != pending)
                throw new InvalidOperationException($"leaving did not commit {pending}: kind {Reload(controller, anchor).Kind}");
            controller.Undo();
            Settle(window);
            if (controller.AcceptedSource != source || Reload(controller, anchor).Kind != anchor.Kind)
                throw new InvalidOperationException("leaving the group was not exactly one undo row");
        });

        Pane("TypeCombo_PendingThenLeave_DoesNotCommit", (controller, host, window) =>
        {
            // PG-19 (D1): a pending type is dropped when focus leaves the box.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            string source = controller.AcceptedSource;
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            type.Focus();
            type.SelectedIndex = 1;   // what an arrow on the closed box does
            Settle(window);
            if (Text(host.Properties, "Message_p_type") != PropertyCopy.PendingType)
                throw new InvalidOperationException("pending line: " + Text(host.Properties, "Message_p_type"));
            Need<TextBox>(host.Properties, "PointSpanInput").Focus();
            Settle(window);
            if (controller.AcceptedSource != source || Reload(controller, point).Role != PointRole.Control || type.SelectedIndex != 0)
                throw new InvalidOperationException("leaving the box committed the pending type");
        });

        Pane("TypeCombo_ArrowWhileClosed_DoesNotCommit", (controller, host, window) =>
        {
            // N1 / PG-07: an arrow on the closed box is pending; Return commits it as one undo row.
            var pane = host.Properties;   // the full shell: ShellHost must leave Return to the pane
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            string source = controller.AcceptedSource;
            var type = Need<ComboBox>(pane, "TypeControl");
            type.Focus();
            Key(type, Avalonia.Input.Key.Down);
            Settle(window);
            if (type.IsDropDownOpen) Key(type, Avalonia.Input.Key.Escape);
            if (controller.AcceptedSource != source) throw new InvalidOperationException("an arrow committed the type");
            if (type.SelectedIndex != 1) throw new InvalidOperationException("the arrow did not move the pending type");
            Key(type, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (Reload(controller, point).Role != PointRole.Anchor) throw new InvalidOperationException("Return did not commit the type");
            if (!Text(pane, "Message_p_type").Contains("is now an anchor point with 2 handles. The rail gained", StringComparison.Ordinal))
                throw new InvalidOperationException("report: " + Text(pane, "Message_p_type"));
            controller.Undo();
            Settle(window);
            if (controller.AcceptedSource != source) throw new InvalidOperationException("the type change was not one undo row");
        });

        Pane("PropertiesPane_TeRootAft_DescribesRootChordAuthority", (controller, host, window) =>
        {
            // MC-2: the TE root end's Aft says it moves only this point; Root chord under Wing rescales (COPY-159).
            var root = controller.Planform!.Trailing.Points.First(point => point.Role == PointRole.RootEnd);
            Select(controller, window, root);
            var description = Need<TextBlock>(host.Properties, "Description_p_aft");
            if (!description.IsEffectivelyVisible || description.Text != PropertyCopy.RootChordAuthority)
                throw new InvalidOperationException("description: " + description.Text);
            if (AutomationProperties.GetHelpText(Need<TextBox>(host.Properties, "PointAftInput"))?.Contains(PropertyCopy.RootChordAuthority, StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Aft help text lacks COPY-159");
        });

        Pane("PropertiesPane_FactRow_NotFocusable_NamedContainer_CopyCommand", (controller, host, window) =>
        {
            // PG-20 (D2), B9: a fact is never a focusable element without a name; its container speaks; Copy copies.
            Select(controller, window, Control(controller, "trailing"));
            var failures = new List<string>();
            var facts = host.Properties.GetVisualDescendants().OfType<Border>()
                .Where(border => border.Classes.Contains("prop-row") && border.IsEffectivelyVisible && border.ContextMenu is not null).ToList();
            if (facts.Count < 6) failures.Add("only " + facts.Count + " fact rows");
            foreach (var fact in facts)
            {
                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(fact))) failures.Add(fact.Name + " has no name");
                foreach (var part in fact.GetVisualDescendants().OfType<InputElement>().Prepend(fact))
                    if (part.Focusable && string.IsNullOrWhiteSpace(AutomationProperties.GetName(part))) failures.Add(fact.Name + " focusable " + part.GetType().Name);
            }
            var copied = new List<string>();
            host.Properties.ClipboardWriter = text => { copied.Add(text); return Task.CompletedTask; };
            var mac = facts.First(fact => fact.Name == "Row_e_mac");
            foreach (var item in mac.ContextMenu!.Items.OfType<MenuItem>())
                item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            var value = Build(controller, controller.Selection).Wing!.Rows.First(row => row.Key == "e:mac").Value;
            if (!copied.SequenceEqual([value, value + " mm"])) failures.Add("copied: " + string.Join(" | ", copied));
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Error_AnnouncedOncePerFailedCommit", (controller, host, window) =>
        {
            // PG-22, B10: once per failed commit — not on a re-render, and again on the next failed commit.
            Select(controller, window, Control(controller, "trailing"));
            var announced = new List<string>();
            host.Properties.Announced += (text, live) => { if (live == AutomationLiveSetting.Assertive) announced.Add(text); };
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            host.RefreshPanes();
            host.RefreshPanes();
            Settle(window);
            Key(aft, Avalonia.Input.Key.Enter);
            Settle(window);
            if (!announced.SequenceEqual(["Enter a number. Aft is unchanged.", "Enter a number. Aft is unchanged."]))
                throw new InvalidOperationException("announced: " + string.Join(" | ", announced));
            if (Text(host.Properties, "Message_p_aft") != "Enter a number. Aft is unchanged." || aft.Text != "abc")
                throw new InvalidOperationException("error line: " + Text(host.Properties, "Message_p_aft"));
            Key(aft, Avalonia.Input.Key.Escape);
            Settle(window);
            if (aft.Text == "abc" || Need<Border>(host.Properties, "Row_p_aft").Classes.Contains("error"))
                throw new InvalidOperationException("Escape did not restore the shown value");
        });

        Pane("Expander_FocusedHeader_ExposesNameAndExpandedState", (controller, host, window) =>
        {
            // N1 / PG-10: the focused header exposes the group's name and its expanded state; collapse is remembered.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var expander = Need<Expander>(host.Properties, "Group_pos");
            var header = expander.GetVisualDescendants().OfType<ToggleButton>().First();
            header.Focus();
            if (AutomationProperties.GetName(header) != "Position" || AutomationProperties.GetName(expander) != "Position")
                throw new InvalidOperationException($"header name '{AutomationProperties.GetName(header)}'");
            var provider = ControlAutomationPeer.CreatePeerForElement(expander).GetProvider<IExpandCollapseProvider>()
                ?? throw new InvalidOperationException("no expand/collapse pattern");
            if (provider.ExpandCollapseState != ExpandCollapseState.Expanded) throw new InvalidOperationException("not expanded");
            Key(header, Avalonia.Input.Key.Space, up: true);
            Settle(window);
            if (provider.ExpandCollapseState != ExpandCollapseState.Collapsed) header.IsChecked = false;   // Space toggles on key up in Fluent
            Settle(window);
            if (provider.ExpandCollapseState != ExpandCollapseState.Collapsed || !Need<TextBlock>(host.Properties, "GroupSummary_pos").IsEffectivelyVisible)
                throw new InvalidOperationException("collapse did not show the summary");
            Select(controller, window, Control(controller, "leading"));
            if (Need<Expander>(host.Properties, "Group_pos").IsExpanded) throw new InvalidOperationException("collapse was not remembered");
        });

        Pane("PropertiesPane_NoNativeMotion", (controller, host, window) =>
        {
            // B6 / PG-16: no Expander content transition and no chevron animation (the header and everything in it).
            Select(controller, window, MakeAnchor(controller));
            var expanders = host.Properties.GetVisualDescendants().OfType<Expander>().ToList();
            if (expanders.Count < 3) throw new InvalidOperationException("only " + expanders.Count + " groups");
            var moving = expanders.SelectMany(expander => expander.GetVisualDescendants().OfType<ToggleButton>().Take(1)
                    .SelectMany(header => header.GetVisualDescendants().OfType<Animatable>().Prepend(header)).Prepend(expander))
                .Where(item => item.Transitions is { Count: > 0 }).Select(item => item.GetType().Name).ToList();
            var transitions = expanders.Count(item => item.ContentTransition is not null);
            if (moving.Count > 0 || transitions > 0)
                throw new InvalidOperationException("motion on " + string.Join(", ", moving) + $"; content transitions {transitions}");
        });

        Pane("PropertiesPane_KeyboardWalk_NoTrapTabLandsOnCheckedKind", (controller, host, window) =>
        {
            // B3: the §10.4 Tab walk through an anchor selection has no trap, and the Kind group is one stop on the checked option.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var navigation = (window as IInputRoot).KeyboardNavigationHandler!;
            var first = host.Properties.GetVisualDescendants().OfType<InputElement>().First(item => item.Focusable && item.IsTabStop && item.IsEffectivelyVisible && item.IsEffectivelyEnabled);
            first.Focus(NavigationMethod.Tab);
            var stops = new List<IInputElement>();
            for (int step = 0; step < 60; step++)
            {
                var focused = window.FocusManager!.GetFocusedElement();
                if (focused is not Visual visual || !host.Properties.IsVisualAncestorOf(visual)) break;
                if (stops.Contains(focused)) throw new InvalidOperationException("Tab revisited " + focused.GetType().Name + " inside the pane (trap)");
                stops.Add(focused);
                navigation.Move(focused, NavigationDirection.Next);
            }
            var radios = stops.OfType<RadioButton>().ToList();
            if (radios.Count != 1 || radios[0].IsChecked != true) throw new InvalidOperationException($"Kind stops: {radios.Count}");
            if (!stops.OfType<ComboBox>().Any() || stops.OfType<TextBox>().Count() < 5 || !stops.OfType<ToggleButton>().Any(button => button is not RadioButton))
                throw new InvalidOperationException("walk: " + string.Join(", ", stops.Select(item => item.GetType().Name)));
        });

        // ---------------- the field nudge (DR-UID-2), with its switch on ----------------

        Nudge("PropertiesPane_FieldNudge_EscCancelsNoRow_KeyUpOneRow", (controller, host, window) =>
        {
            // MC-10: the field run is the canvas Nudging gesture — preview while held, Esc no row, KeyUp one row.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            string source = controller.AcceptedSource;
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            foreach (var _ in Enumerable.Range(0, 3)) Key(aft, Avalonia.Input.Key.Up);
            Settle(window);
            if (controller.Gesture != GestureState.Nudging || Text(host.Properties, "WingChipText") != "≈ preview")
                throw new InvalidOperationException($"run: {controller.Gesture}, chip '{Text(host.Properties, "WingChipText")}'");
            Key(aft, Avalonia.Input.Key.Escape);
            WaitIdle(controller, window);
            if (controller.AcceptedSource != source || controller.CanUndo) throw new InvalidOperationException("Esc left a row");
            Key(aft, Avalonia.Input.Key.Up);
            Key(aft, Avalonia.Input.Key.Up);
            Key(aft, Avalonia.Input.Key.Up, up: true);
            WaitIdle(controller, window);
            var moved = Reload(controller, point);
            if (Math.Abs(moved.AftMeters - point.AftMeters - 0.0002) > 2e-6) throw new InvalidOperationException("run moved " + (moved.AftMeters - point.AftMeters));
            controller.Undo();
            Settle(window);
            if (controller.AcceptedSource != source || controller.CanUndo) throw new InvalidOperationException("KeyUp was not exactly one row");
        });

        Nudge("PropertiesPane_FieldNudge_IgnoredWhileTextDirty", (controller, host, window) =>
        {
            // MC-10: arrows belong to the caret while the field text is being edited.
            Select(controller, window, Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            aft.Text = "12";
            Key(aft, Avalonia.Input.Key.Up);
            Settle(window);
            if (controller.Gesture != GestureState.Idle || aft.Text != "12") throw new InvalidOperationException($"dirty field nudged: {controller.Gesture} '{aft.Text}'");
        });

        Nudge("FieldNudge_HelpTextNamesSteps_PerFamily", (controller, host, window) =>
        {
            // PG-21 (D3), PG-08: the help text names the steps in the field's unit.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id);
            Select(controller, window, Control(controller, "trailing"));
            string aft = AutomationProperties.GetHelpText(Need<TextBox>(host.Properties, "PointAftInput")) ?? "";
            Select(controller, window, handle);
            string angle = AutomationProperties.GetHelpText(Need<TextBox>(host.Properties, "HandleAngleInput")) ?? "";
            if (!aft.Contains(PropertyCopy.NudgeHelp("mm"), StringComparison.Ordinal) || !angle.Contains(PropertyCopy.NudgeHelp("°"), StringComparison.Ordinal))
                throw new InvalidOperationException($"aft '{aft}' angle '{angle}'");
            PropertiesFieldNudge.Enabled = false;
            host.RefreshPanes();
            if (AutomationProperties.GetHelpText(Need<TextBox>(host.Properties, "HandleAngleInput"))?.Contains("arrows", StringComparison.Ordinal) == true)
                throw new InvalidOperationException("a disabled nudge is still advertised");
        });

        Nudge("FieldNudge_KeyUp_AnnouncesValueOnceInStatus", (controller, host, window) =>
        {
            // PG-21 (D3), PG-08: the new value is announced once, politely, on release — not on later re-renders.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var polite = new List<string>();
            host.Properties.Announced += (text, live) => { if (live == AutomationLiveSetting.Polite) polite.Add(text); };
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift);
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            host.RefreshPanes();
            Settle(window);
            string want = $"Aft {Quantity.TypedLength(point.AftMeters + 0.001)} mm.";
            if (!polite.SequenceEqual([want])) throw new InvalidOperationException("announced: " + string.Join(" | ", polite) + " want " + want);
        });

        Nudge("FieldNudge_AngleRun_StopsAtDomainBound", (controller, host, window) =>
        {
            // MC-23: a run at the angle bound stops there (COPY-170) and makes no undo row when nothing changed.
            var tip = controller.Planform!.Trailing.Points.First(point => point.Role == PointRole.TipHandle);
            Select(controller, window, tip);
            var angle = Need<TextBox>(host.Properties, "HandleAngleInput");
            angle.Focus();
            angle.Text = "-89.5";
            Key(angle, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (!controller.CanUndo) throw new InvalidOperationException("the typed angle was refused: " + controller.Status);
            // Hold Shift+Down until the run stops (the angle bound, or Core's ordering clamp short of it), then release.
            for (int press = 0; press < 12 && Text(host.Properties, "Message_h_angle") != PropertyCopy.AngleRunStops; press++)
                Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift);
            Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            string source = controller.AcceptedSource;
            // A second run held at the bound shows COPY-170 and, with nothing changed, makes no undo row.
            Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift);
            if (Text(host.Properties, "Message_h_angle") != PropertyCopy.AngleRunStops)
                throw new InvalidOperationException("bound line: " + Text(host.Properties, "Message_h_angle"));
            Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            if (controller.AcceptedSource != source) throw new InvalidOperationException("a run held at the bound made a row");
        });

        Nudge("FieldNudge_OffOnWindows_UntilNarratorPass", (controller, host, window) =>
        {
            // B8: on Windows the switch does not turn the nudge on.
            PropertiesFieldNudge.OnWindows = true;
            Select(controller, window, Control(controller, "trailing"));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            Key(aft, Avalonia.Input.Key.Up);
            Settle(window);
            if (controller.Gesture != GestureState.Idle) throw new InvalidOperationException("Windows nudged");
        });
    }

    // ---------------- fixtures ----------------

    private static void Pane(string name, Action<WorkbenchController, ShellHost, Window> body) => DesktopChecks.Check(name, () =>
    {
        using var controller = new WorkbenchController();
        var host = new ShellHost(controller);
        var window = new Window { Content = host, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            Pump(host.OpenExampleAsync());
            Settle(window);
            body(controller, host, window);
        }
        finally { window.Close(); }
    });

    private static void Nudge(string name, Action<WorkbenchController, ShellHost, Window> body) => Pane(name, (controller, host, window) =>
    {
        bool enabled = PropertiesFieldNudge.Enabled, windows = PropertiesFieldNudge.OnWindows;
        PropertiesFieldNudge.Enabled = true;
        PropertiesFieldNudge.OnWindows = false;
        try { body(controller, host, window); }
        finally
        {
            PropertiesFieldNudge.Enabled = enabled;
            PropertiesFieldNudge.OnWindows = windows;
        }
    });

    private static WorkbenchController Opened()
    {
        var controller = new WorkbenchController();
        Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
        return controller;
    }

    private static PropertiesModel Build(WorkbenchController controller, Selection selection) =>
        PropertiesView.Build(selection, controller.CurrentProjection, controller.Estimates, ShellMode.Workspace,
            new PropertiesContext(controller.Planform));

    private static IEnumerable<(string Name, PropertiesModel Model)> EverySelection(WorkbenchController controller)
    {
        var anchor = MakeAnchor(controller);
        var plan = controller.Planform!;
        var points = plan.Leading.Points.Concat(plan.Trailing.Points).ToList();
        yield return ("foil", Build(controller, new Selection.Foil()));
        yield return ("station", Build(controller, new Selection.Station(0, controller.CurrentProjection!.Assignments[0].Eta)));
        yield return ("several", Build(controller, new Selection.Points([Ref(points[1]), Ref(points[^2])])));
        foreach (var point in points)
            yield return ($"{point.Curve}/{point.Role}/{point.Index}", Build(controller, new Selection.Points([Ref(point)])));
        yield return ("unavailable", PropertiesView.Build(new Selection.Points([Ref(anchor)]), controller.CurrentProjection,
            controller.Estimates! with { AreaSquareMeters = double.NaN }, ShellMode.Workspace, new PropertiesContext(plan)));
        yield return ("section", PropertiesView.Build(new Selection.Foil(), controller.CurrentProjection, controller.Estimates, ShellMode.SectionEditor));
    }

    private static PointView MakeAnchor(WorkbenchController controller)
    {
        var point = Control(controller, "trailing");
        Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)));
        return Reload(controller, point);
    }

    private static PointView Control(WorkbenchController controller, string curve) =>
        (curve == "leading" ? controller.Planform!.Leading : controller.Planform!.Trailing).Points.First(point => point.Role == PointRole.Control);

    private static PointView Reload(WorkbenchController controller, PointView point) =>
        PropertiesView.Find(controller.Planform!, Ref(point)) ?? throw new InvalidOperationException("point gone: " + point.Id);

    private static PointRef Ref(PointView point) => new(point.Curve, point.Id);

    private static void Select(WorkbenchController controller, Window window, PointView point)
    {
        controller.Select(new Selection.Points([Ref(point)]));
        Settle(window);
    }

    private static int Decimals(string value) => value.Contains('.') ? value.Length - value.IndexOf('.') - 1 : 0;

    private static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? root.GetLogicalDescendants().OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? root.GetVisualDescendants().OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? throw new InvalidOperationException("missing " + name);

    private static string Text(Control root, string name) => Need<TextBlock>(root, name).Text ?? "";

    private static void Key(Control control, Key key, KeyModifiers modifiers = KeyModifiers.None, bool up = false) =>
        control.RaiseEvent(new KeyEventArgs { RoutedEvent = up ? InputElement.KeyUpEvent : InputElement.KeyDownEvent, Source = control, Key = key, KeyModifiers = modifiers });

    private static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void WaitIdle(WorkbenchController controller, Window window)
    {
        var start = DateTime.UtcNow;
        while (controller.Gesture != GestureState.Idle && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
        Settle(window);
    }

    private static void Pump(Task task)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!task.IsCompleted && !timeout.IsCancellationRequested)
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        task.GetAwaiter().GetResult();
    }

    private sealed class Near : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) < 0.5;
        public int GetHashCode(double value) => 0;
    }
}
