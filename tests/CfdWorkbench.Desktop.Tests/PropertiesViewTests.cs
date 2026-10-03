using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
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
            // MC-17: rails and Dihedral set handles by angle + length (an anchor's handle rows follow its kind); Twist and t/c
            // set every point and handle by From root + value, in the channel's own unit (M1.2b2 §3.6).
            using var channels = Opened();
            string ChannelKeys(Selection selection) => string.Join(",", Build(channels, selection).Groups.SelectMany(group => group.Rows)
                .Where(row => row.Kind is RowKind.Input or RowKind.KindList).Select(row => row.Key + ":" + row.Unit));
            var channelChecks = new List<(string Name, string Actual, string Want)>
            {
                ("dihedral control", ChannelKeys(new Selection.Points([Ref(Channel(channels, "dihedral", 4))])), "p:from:mm,p:aft:mm"),
                ("twist control", ChannelKeys(new Selection.Points([Ref(Channel(channels, "twist", 4))])), "p:from:mm,p:aft:°"),
                ("thickness control", ChannelKeys(new Selection.Points([Ref(Channel(channels, "thickness", 4))])), "p:from:mm,p:aft:%")
            };
            var twistAnchor = AnchorOn(channels, "twist", 3, TangentKind.Corner);
            channelChecks.Add(("twist corner anchor", ChannelKeys(new Selection.Points([Ref(twistAnchor)])),
                "p:from:mm,p:aft:°,t:kind:,h:root-from:mm,h:root-value:°,h:tip-from:mm,h:tip-value:°"));
            channelChecks.Add(("twist handle", ChannelKeys(new Selection.Points([Ref(Handles(channels, twistAnchor).Tip)])), "p:from:mm,p:aft:°,t:kind:"));
            var dihedralAnchor = AnchorOn(channels, "dihedral", 3, TangentKind.Corner);
            channelChecks.Add(("dihedral handle", ChannelKeys(new Selection.Points([Ref(Handles(channels, dihedralAnchor).Tip)])), "h:angle:°,h:length:mm,t:kind:"));
            var channelWrong = channelChecks.Where(check => check.Actual != check.Want).Select(check => $"{check.Name}: {check.Actual}").ToList();
            if (channelWrong.Count > 0) throw new InvalidOperationException(string.Join("; ", channelWrong));
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

        Pane("PropertiesView_TypedTcBelowOnePercent_WarnsFractionHint", (controller, host, window) =>
        {
            // MC-20: a bare 0.12 is 0.12 %, never silently 12 %; the t/c row commits it as typed and warns with COPY-169 in
            // the status strip (DR-STATUS-1).
            var point = Channel(controller, "thickness", 3);
            Select(controller, window, point);
            Type(controller, window, Need<TextBox>(host.Properties, "PointAftInput"), "0.12");
            var committed = Reload(controller, point);
            string strip = Status(host).Text ?? "";
            if (Math.Abs(committed.Ordinate - 0.0012) > 1e-7 || strip != "0.12 % — for 12 %, type 12 or 0.12 × 100." ||
                StatusStripTests.Kind(host) != "warning")
                throw new InvalidOperationException($"t/c {committed.Ordinate}, strip '{strip}' ({StatusStripTests.Kind(host)})");
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
            // N1 / PG-04 / PG-26: the change to Unavailable is a text change of the shell's attached polite status line
            // (COPY-160), once; PG-27: the Wing note is already attached (hidden while empty) and only its text changes.
            var status = Status(host);
            var note = Need<TextBlock>(host.Properties, "Note_wing_0");
            if (!status.IsAttachedToVisualTree() || AutomationProperties.GetLiveSetting(status) != AutomationLiveSetting.Polite || !note.IsAttachedToVisualTree() || note.IsVisible)
                throw new InvalidOperationException($"before: status attached {status.IsAttachedToVisualTree()}, note attached {note.IsAttachedToVisualTree()} visible {note.IsVisible}");
            var seen = Changes(status);
            host.Properties.Bind(controller, controller.Estimates! with { MacMeters = double.NaN });
            Settle(window);
            host.Properties.Bind(controller, controller.Estimates! with { MacMeters = double.NaN });
            Settle(window);
            const string want = "MAC unavailable — did not converge for this shape.";
            if (seen.Count(text => text == want) != 1 || !ReferenceEquals(note, Need<TextBlock>(host.Properties, "Note_wing_0")) || !note.IsVisible)
                throw new InvalidOperationException("status changes: " + string.Join(" | ", seen));
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

        Pane("PropertiesPane_Tangent_SelectedKindExposed", (controller, host, window) =>
        {
            // Finding #12, retargeted to the enum (DR-CELL-2): the Tangent kind box is named and exposes its selected kind.
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            if (AutomationProperties.GetName(kind) != "Tangent kind" || !kind.IsEffectivelyVisible || !kind.IsEnabled)
                throw new InvalidOperationException($"kind box '{AutomationProperties.GetName(kind)}' visible {kind.IsEffectivelyVisible}");
            if ((kind.SelectedItem as ComboBoxItem)?.Content?.ToString() != anchor.Kind.ToString())
                throw new InvalidOperationException("selected: " + (kind.SelectedItem as ComboBoxItem)?.Content);
            var selection = ControlAutomationPeer.CreatePeerForElement(kind).GetProvider<ISelectionProvider>()
                ?? throw new InvalidOperationException("the kind box exposes no selection");
            var chosen = selection.GetSelection().SingleOrDefault();
            if (chosen?.GetName() != anchor.Kind.ToString()) throw new InvalidOperationException("exposed selection " + chosen?.GetName());
            if (host.Properties.GetVisualDescendants().OfType<RadioButton>().Any())
                throw new InvalidOperationException("a Kind radio list is still drawn");
        });

        Pane("PropertiesPane_FieldBlur_CommitsAndKeepsFocusTarget", (controller, host, window) =>
        {
            // UI-39, UI-C: leaving a field commits it (one undo row) and focus stays where it was sent.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            var from = Need<TextBox>(host.Properties, "PointSpanInput");
            aft.Focus();
            aft.Text = ((point.Ordinate + 0.002) * 1000).ToString("0.00", Inv);
            from.Focus();
            WaitIdle(controller, window);
            var moved = Reload(controller, point);
            if (Math.Abs(moved.Ordinate - point.Ordinate - 0.002) > 2e-6) throw new InvalidOperationException("blur did not commit: " + moved.Ordinate);
            if (!from.IsFocused) throw new InvalidOperationException("focus left the field it was sent to");
            controller.Undo();
            Settle(window);
            if (Math.Abs(Reload(controller, point).Ordinate - point.Ordinate) > 1e-6 || controller.CanUndo)
                throw new InvalidOperationException("the blur commit was not exactly one undo row");
        });

        Pane("PropertiesPane_KindBox_ArrowWhileClosed_IsPending_OneUndoRowPerIntent", (controller, host, window) =>
        {
            // PG-06 as amended by DR-CELL-2: arrows on the closed box stage a kind (no wrap) and commit nothing; Return
            // commits it as one undo row.
            var pane = host.Properties;   // the full shell: ShellHost must leave Return to the pane
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            string source = controller.AcceptedSource;
            var kind = Need<ComboBox>(pane, "KindControl");
            kind.Focus();
            foreach (var _ in Enumerable.Range(0, 4)) Key(kind, Avalonia.Input.Key.Down);
            Settle(window);
            if (controller.AcceptedSource != source || Reload(controller, anchor).Kind != anchor.Kind)
                throw new InvalidOperationException("an arrow committed the kind");
            if ((kind.SelectedItem as ComboBoxItem)?.Content?.ToString() != "Corner" || kind.IsDropDownOpen)
                throw new InvalidOperationException("arrows did not stop at the last kind (no wrap)");
            if (Text(pane, "Message_t_kind") != PropertyCopy.PendingKind("corner", anchor.Kind!.Value.ToString().ToLowerInvariant()))
                throw new InvalidOperationException("pending line: " + Text(pane, "Message_t_kind"));
            Key(kind, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind != TangentKind.Corner) throw new InvalidOperationException("Return did not commit Corner");
            controller.Undo();
            Settle(window);
            if (Reload(controller, anchor).Kind != anchor.Kind || controller.AcceptedSource != source)
                throw new InvalidOperationException("the kind intent was not exactly one undo row");
        });

        Pane("Tangent_KindChange_KeepsFocusOnKindBox", (controller, host, window) =>
        {
            // N1, retargeted to the enum: after a kind commit, focus stays on the Tangent kind box, which shows the new kind.
            var pane = host.Properties;   // the full shell: ShellHost must leave Return to the pane
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var kind = Need<ComboBox>(pane, "KindControl");
            kind.Focus();
            Key(kind, Avalonia.Input.Key.Down);
            Key(kind, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            var focused = window.FocusManager!.GetFocusedElement();
            if (!ReferenceEquals(focused, kind) || (kind.SelectedItem as ComboBoxItem)?.Content?.ToString() != Reload(controller, anchor).Kind.ToString())
                throw new InvalidOperationException($"focus on {focused?.GetType().Name ?? "none"} after the commit");
            if (Reload(controller, anchor).Kind == anchor.Kind) throw new InvalidOperationException("Return did not commit the staged kind");
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
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            kind.Focus();
            Key(kind, Avalonia.Input.Key.Down);
            string? pending = (kind.SelectedItem as ComboBoxItem)?.Content?.ToString();
            Key(kind, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            if (Reload(controller, anchor).Kind.ToString() != pending || !kind.IsFocused)
                throw new InvalidOperationException($"Return on a pending Kind: kind {Reload(controller, anchor).Kind}, focus on it {kind.IsFocused}");
        });

        Pane("PropertiesPane_KeyboardCopy_SelectionAndGroupHeader", (controller, host, window) =>
        {
            // PG-25 (C1): facts stay out of the Tab order (D2), so copying is a keyboard command. ⌘⇧C / Ctrl+Shift+C copies the
            // selection's rows as "label value unit" lines; a group header's context menu (Shift+F10) copies its group;
            // ⌘C / Ctrl+C never copies a row the pointer chose before focus moved. No pointer event drives a copy here.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var copied = new List<string>();
            host.Properties.ClipboardWriter = text => { copied.Add(text); return Task.CompletedTask; };
            var command = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus(NavigationMethod.Tab);
            Key(aft, Avalonia.Input.Key.C, command | KeyModifiers.Shift);
            var lines = copied.SingleOrDefault()?.Split('\n') ?? [];
            foreach (var want in new[] { "Type Control point", $"From root {Quantity.TypedLength(point.SpanMeters)} mm", $"η {Quantity.Eta(point.Eta)}", $"Aft {Quantity.TypedLength(point.Ordinate)} mm" })
                if (!lines.Contains(want)) throw new InvalidOperationException($"selection copy lacks '{want}': {string.Join(" / ", lines)}");
            copied.Clear();
            var rail = Need<Expander>(host.Properties, "Group_rail");
            var header = rail.GetVisualDescendants().OfType<ToggleButton>().First();
            header.Focus(NavigationMethod.Tab);
            Key(header, Avalonia.Input.Key.F10, KeyModifiers.Shift);
            var menu = rail.ContextMenu ?? throw new InvalidOperationException("the header has no context menu");
            if (!menu.IsOpen) throw new InvalidOperationException("Shift+F10 on a focused header did not open its menu");
            menu.Items.OfType<MenuItem>().First().RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            var group = copied.SingleOrDefault()?.Split('\n') ?? [];
            if (group.Length != 2 || !group[0].StartsWith("Degree ", StringComparison.Ordinal) || !group[1].StartsWith("Points ", StringComparison.Ordinal))
                throw new InvalidOperationException("group copy: " + string.Join(" / ", group));
            copied.Clear();
            var mac = Need<Border>(host.Properties, "Row_e_mac");
            mac.RaiseEvent(new PointerPressedEventArgs(mac, new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true), window, new Point(1, 1), 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            aft.Focus(NavigationMethod.Tab);
            header.Focus(NavigationMethod.Tab);
            Key(header, Avalonia.Input.Key.C, command);
            if (copied.Count != 0) throw new InvalidOperationException("a stale pointer row was copied: " + copied[0]);
        });

        Pane("PropertiesPane_StatusLine_SelectedAndKindReport", (controller, host, window) =>
        {
            // PG-28: Esc on a handle selects its anchor and says so; PG-33 / MC-11: a kind commit reports from its result —
            // both as text changes of the shell's polite status line.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id && point.Index > anchor.Index);
            Select(controller, window, handle);
            var seen = Changes(Status(host));
            var header = Need<Expander>(host.Properties, "Group_hdl").GetVisualDescendants().OfType<ToggleButton>().First();
            header.Focus(NavigationMethod.Tab);
            Key(header, Avalonia.Input.Key.Escape);
            Settle(window);
            string selected = $"Selected Trailing edge · point {anchor.Index + 1} of {controller.Planform!.Trailing.Points.Count}.";
            if (!seen.Contains(selected) || controller.Selection is not Selection.Points { Items: [var chosen] } || chosen.VertexId != anchor.Id)
                throw new InvalidOperationException("Esc: " + string.Join(" | ", seen));
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            kind.IsDropDownOpen = true;
            kind.SelectedIndex = 1;   // a pointer pick of Symmetric in the open list
            kind.IsDropDownOpen = false;
            WaitIdle(controller, window);
            if (!seen.Any(text => text.StartsWith($"Trailing edge point {anchor.Index + 1} is now Symmetric.", StringComparison.Ordinal)))
                throw new InvalidOperationException("kind report: " + string.Join(" | ", seen));
        });

        Pane("PropertiesPane_StateBrushes_InAllThreeThemes", (controller, host, window) =>
        {
            // PG-29 (C3), in B: the twirl chevron, the band-less group header, the enum boxes and their popup resolve to
            // DESIGN.md tokens in each state and in Light, Dark and High contrast (which would otherwise inherit Fluent Light).
            Select(controller, window, MakeAnchor(controller));
            var themes = new (Avalonia.Styling.ThemeVariant Variant, string Ink, string Muted, string Surface, string Selection, string OnSelection, string Focus)[]
            {
                (Avalonia.Styling.ThemeVariant.Light, "#1b2929", "#526362", "#fbfcfb", "#d8eeea", "#1b2929", "#006c67"),
                (Avalonia.Styling.ThemeVariant.Dark, "#ebf3f0", "#b2c4bf", "#1e2d31", "#274c47", "#ebf3f0", "#88d8c6"),
                (NativeReviewThemes.HighContrast, "#ffffff", "#ffffff", "#000000", "#ffee58", "#000000", "#ffee58")
            };
            var failures = new List<string>();
            foreach (var theme in themes)
            {
                window.RequestedThemeVariant = theme.Variant;
                Settle(window);
                void Expect(string what, IBrush? brush, string want)
                {
                    if (brush is not ISolidColorBrush solid || solid.Color != Color.Parse(want))
                        failures.Add($"{theme.Variant.Key}/{what} {(brush as ISolidColorBrush)?.Color.ToString() ?? "none"}≠{want}");
                }
                void Clear(string what, IBrush? brush)
                {
                    if (brush is not null && brush is not ISolidColorBrush { Color.A: 0 })
                        failures.Add($"{theme.Variant.Key}/{what} is not transparent");
                }
                var group = Need<Expander>(host.Properties, "Group_pos");
                var header = group.GetVisualDescendants().OfType<ToggleButton>().First();
                Expect("chevron", Need<Avalonia.Controls.Shapes.Path>(host.Properties, "GroupChevron_pos").Stroke, theme.Muted);
                foreach (var state in new[] { ":checked", ":pressed", ":pointerover" })
                {
                    Pseudo(header, state, true);
                    Settle(window);
                    Clear("header" + state, Part<Border>(header, "ToggleButtonBackground").Background);
                    Pseudo(header, state, false);
                }
                foreach (var name in new[] { "TypeControl", "KindControl" })
                {
                    var box = Need<ComboBox>(host.Properties, name);
                    Pseudo(box, ":pressed", true);
                    Settle(window);
                    Clear(name + ":pressed", Part<Border>(box, "Background").Background);
                    Pseudo(box, ":pressed", false);
                    Expect(name + " glyph", Part<PathIcon>(box, "DropDownGlyph").Foreground, theme.Muted);
                }
                var type = Need<ComboBox>(host.Properties, "TypeControl");
                type.IsDropDownOpen = true;
                Settle(window);
                Expect("type:dropdownopen border", Part<Border>(type, "Background").BorderBrush, theme.Focus);
                var popup = type.GetVisualDescendants().OfType<Popup>().First().Child as Border;
                Expect("popup", popup?.Background, theme.Surface);
                foreach (var item in popup?.GetVisualDescendants().OfType<ComboBoxItem>() ?? [])
                {
                    var presenter = Part<ContentPresenter>(item, "PART_ContentPresenter");
                    Expect($"item{(item.IsSelected ? ":selected" : "")} text", presenter.Foreground, item.IsSelected ? theme.OnSelection : theme.Ink);
                    if (item.IsSelected) Expect("item:selected fill", presenter.Background, theme.Selection);
                }
                type.IsDropDownOpen = false;
                Settle(window);
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
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
            if (!(StatusStripTests.Text(host).Text ?? "").Contains("is now an anchor point with 2 handles. The rail gained", StringComparison.Ordinal))
                throw new InvalidOperationException("report: " + StatusStripTests.Text(host).Text);
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
            // PG-20 (D2), B9: a fact is never a focusable element without a name; its container speaks; Copy copies — from
            // the header's menu, since B has no row context menus (DN-3).
            Select(controller, window, Control(controller, "trailing"));
            var failures = new List<string>();
            var facts = host.Properties.GetVisualDescendants().OfType<Border>()
                .Where(border => border.Classes.Contains("prop-row") && border.IsEffectivelyVisible &&
                                 AutomationProperties.GetControlTypeOverride(border) == AutomationControlType.Text).ToList();
            if (facts.Count < 6) failures.Add("only " + facts.Count + " fact rows");
            foreach (var fact in facts)
            {
                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(fact))) failures.Add(fact.Name + " has no name");
                foreach (var part in fact.GetVisualDescendants().OfType<InputElement>().Prepend(fact))
                    if (part.Focusable && string.IsNullOrWhiteSpace(AutomationProperties.GetName(part))) failures.Add(fact.Name + " focusable " + part.GetType().Name);
            }
            var copied = new List<string>();
            host.Properties.ClipboardWriter = text => { copied.Add(text); return Task.CompletedTask; };
            var menu = Need<Grid>(host.Properties, "WingHeader").ContextMenu ?? throw new InvalidOperationException("the Wing header has no menu");
            foreach (var item in menu.Items.OfType<MenuItem>().Where(item => item.Header as string is "Copy MAC" or "Copy MAC with unit"))
                item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            var value = Build(controller, controller.Selection).Wing!.Rows.First(row => row.Key == "e:mac").Value;
            if (!copied.SequenceEqual([value, value + " mm"])) failures.Add("copied: " + string.Join(" | ", copied));
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("PropertiesPane_Error_AnnouncedOncePerFailedCommit", (controller, host, window) =>
        {
            // PG-22, B10, PG-26: once per failed commit — not on a re-render, and again on the next failed commit — as a
            // text change of the field's attached assertive message line.
            Select(controller, window, Control(controller, "trailing"));
            var line = Need<TextBlock>(host.Properties, "Message_p_aft");
            if (!line.IsAttachedToVisualTree() || AutomationProperties.GetLiveSetting(line) is not AutomationLiveSetting.Assertive and not AutomationLiveSetting.Polite)
                throw new InvalidOperationException("the message line is not an attached live region");
            var announced = Changes(line);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            host.RefreshPanes();
            host.RefreshPanes();
            Settle(window);
            Key(aft, Avalonia.Input.Key.Enter);
            Settle(window);
            if (announced.Count(text => text == "Enter a number. Aft is unchanged.") != 2 || AutomationProperties.GetLiveSetting(line) != AutomationLiveSetting.Assertive)
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
            if (AutomationProperties.GetName(header) != "Point" || AutomationProperties.GetName(expander) != "Point")   // B: one "Point" group
                throw new InvalidOperationException($"header name '{AutomationProperties.GetName(header)}'");
            var provider = ControlAutomationPeer.CreatePeerForElement(expander).GetProvider<IExpandCollapseProvider>()
                ?? throw new InvalidOperationException("no expand/collapse pattern");
            if (provider.ExpandCollapseState != ExpandCollapseState.Expanded) throw new InvalidOperationException("not expanded");
            Key(header, Avalonia.Input.Key.Space);
            Key(header, Avalonia.Input.Key.Space, up: true);
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
            if (expanders.Count < 2) throw new InvalidOperationException("only " + expanders.Count + " groups");   // B: Point and the rail
            var moving = expanders.SelectMany(expander => expander.GetVisualDescendants().OfType<ToggleButton>().Take(1)
                    .SelectMany(header => header.GetVisualDescendants().OfType<Animatable>().Prepend(header)).Prepend(expander))
                .Where(item => item.Transitions is { Count: > 0 }).Select(item => item.GetType().Name).ToList();
            var transitions = expanders.Count(item => item.ContentTransition is not null);
            if (moving.Count > 0 || transitions > 0)
                throw new InvalidOperationException("motion on " + string.Join(", ", moving) + $"; content transitions {transitions}");
        });

        Pane("PropertiesPane_KeyboardWalk_NoTrap_KindBoxOneStop", (controller, host, window) =>
        {
            // B3: the §10.4 Tab walk through an anchor selection has no trap, and the Tangent kind is one stop (DR-CELL-2).
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var stops = TabWalk(host.Properties, window);
            if (stops.OfType<ComboBox>().Count(box => box.Name == "KindControl") != 1 || stops.OfType<RadioButton>().Any())
                throw new InvalidOperationException("Kind stops: " + stops.OfType<ComboBox>().Count(box => box.Name == "KindControl"));
            if (!stops.OfType<ComboBox>().Any(box => box.Name == "TypeControl") || stops.OfType<TextBox>().Count() < 5 ||
                !stops.OfType<ToggleButton>().Any(button => button.TemplatedParent is Expander))
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
            if (Math.Abs(moved.Ordinate - point.Ordinate - 0.0002) > 2e-6) throw new InvalidOperationException("run moved " + (moved.Ordinate - point.Ordinate));
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
            // PG-26: the announcement is a text change of the shell's attached polite status line.
            var status = Status(host);
            if (!status.IsAttachedToVisualTree()) throw new InvalidOperationException("status line not attached");
            var polite = Changes(status);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift);
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            host.RefreshPanes();
            Settle(window);
            string want = $"Aft {Quantity.TypedLength(point.Ordinate + 0.001)} mm.";
            if (polite.Count(text => text == want) != 1) throw new InvalidOperationException("status changes: " + string.Join(" | ", polite) + " want " + want);
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
            for (int press = 0; press < 12 && StatusStripTests.Text(host).Text != PropertyCopy.AngleRunStops; press++)
                Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift);
            Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            string source = controller.AcceptedSource;
            // A second run held at the bound shows COPY-170 and, with nothing changed, makes no undo row.
            Key(angle, Avalonia.Input.Key.Down, KeyModifiers.Shift);
            if (StatusStripTests.Text(host).Text != PropertyCopy.AngleRunStops || StatusStripTests.Kind(host) != "warning")
                throw new InvalidOperationException("bound report: " + StatusStripTests.Text(host).Text);
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

        // ---------------- the status strip (DR-STATUS-1; docs/reviews/ui-status-bar.md §2.5) ----------------

        Pane("StatusStrip_TypeChange_ReportInStrip_NotInRow", (controller, host, window) =>
        {
            // The operator's case: the type report is in the strip and the Type row shows no message.
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var type = Need<ComboBox>(host.Properties, "TypeControl");
            type.Focus();
            Key(type, Avalonia.Input.Key.Down);
            Settle(window);
            if (type.IsDropDownOpen) Key(type, Avalonia.Input.Key.Escape);
            Key(type, Avalonia.Input.Key.Enter);
            WaitIdle(controller, window);
            string strip = StatusStripTests.Text(host).Text ?? "";
            if (!strip.Contains("is now an anchor point with 2 handles. The rail gained", StringComparison.Ordinal) ||
                Need<TextBlock>(host.Properties, "Message_p_type").IsEffectivelyVisible)
                throw new InvalidOperationException($"strip '{strip}', row '{Text(host.Properties, "Message_p_type")}'");
        });

        Pane("StatusStrip_KindChange_ReportInStrip_NotInRow", (controller, host, window) =>
        {
            var anchor = MakeAnchor(controller);
            Select(controller, window, anchor);
            var kind = Need<ComboBox>(host.Properties, "KindControl");
            kind.IsDropDownOpen = true;
            kind.SelectedIndex = 1;   // a pointer pick of Symmetric in the open list
            kind.IsDropDownOpen = false;
            WaitIdle(controller, window);
            string strip = StatusStripTests.Text(host).Text ?? "";
            if (!strip.StartsWith($"Trailing edge point {anchor.Index + 1} is now Symmetric.", StringComparison.Ordinal) ||
                Need<TextBlock>(host.Properties, "Message_t_kind").IsEffectivelyVisible)
                throw new InvalidOperationException($"strip '{strip}', row '{Text(host.Properties, "Message_t_kind")}'");
        });

        Nudge("StatusStrip_NudgeRelease_ValueInStrip_NotInRow", (controller, host, window) =>
        {
            var point = Control(controller, "trailing");
            Select(controller, window, point);
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift);
            Key(aft, Avalonia.Input.Key.Up, KeyModifiers.Shift, up: true);
            WaitIdle(controller, window);
            string want = $"Aft {Quantity.TypedLength(point.Ordinate + 0.001)} mm.";
            string strip = StatusStripTests.Text(host).Text ?? "";
            if (strip != want || Need<TextBlock>(host.Properties, "Message_p_aft").IsEffectivelyVisible)
                throw new InvalidOperationException($"strip '{strip}' want '{want}', row '{Text(host.Properties, "Message_p_aft")}'");
        });

        Pane("StatusStrip_FieldError_StaysAtField_StripUnchanged", (controller, host, window) =>
        {
            // A field error speaks assertively at its field and is not repeated in the strip.
            Select(controller, window, Control(controller, "trailing"));
            var seen = Changes(StatusStripTests.Text(host));
            var aft = Need<TextBox>(host.Properties, "PointAftInput");
            aft.Focus();
            aft.Text = "abc";
            Key(aft, Avalonia.Input.Key.Enter);
            Settle(window);
            var line = Need<TextBlock>(host.Properties, "Message_p_aft");
            if (line.Text != "Enter a number. Aft is unchanged." || !line.IsEffectivelyVisible ||
                AutomationProperties.GetLiveSetting(line) != AutomationLiveSetting.Assertive)
                throw new InvalidOperationException($"field line '{line.Text}' visible {line.IsEffectivelyVisible}");
            if (seen.Count > 0) throw new InvalidOperationException("the strip changed: " + string.Join(" | ", seen));
        });

        Pane("StatusStrip_Refresh_DoesNotReshowOlderControllerStatus", (controller, host, window) =>
        {
            // §2.3 (Inferred hazard): a refresh after a pane report must not put an older controller status back over it.
            var anchor = MakeAnchor(controller);
            var handle = controller.Planform!.Trailing.Points.First(point => point.AnchorId == anchor.Id && point.Index > anchor.Index);
            Select(controller, window, handle);
            var header = Need<Expander>(host.Properties, "Group_hdl").GetVisualDescendants().OfType<ToggleButton>().First();
            header.Focus(NavigationMethod.Tab);
            Key(header, Avalonia.Input.Key.Escape);
            Settle(window);
            string selected = $"Selected Trailing edge · point {anchor.Index + 1} of {controller.Planform!.Trailing.Points.Count}.";
            host.RefreshPanes();
            host.RefreshPanes();
            Settle(window);
            string strip = StatusStripTests.Text(host).Text ?? "";
            if (strip != selected) throw new InvalidOperationException($"strip '{strip}' (controller '{controller.Status}'), want '{selected}'");
        });

        ChannelRows();
        ShellModelTests.RunWindowed();
        CaptureTwistRow();
    }

    /// <summary>
    /// Review capture (CFDW_PNL_CAPTURE=&lt;dir&gt;): the approved mockup's screen 3b — One view: Side, twist point 5 of 7
    /// moved by −0.25° to −1.00°, its Twist field focused — the Properties pane and the whole window. Off by default;
    /// never part of the gate.
    /// </summary>
    private static void CaptureTwistRow()
    {
        if (Environment.GetEnvironmentVariable("CFDW_PNL_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        Pane("Capture_PnlTwistRow", (controller, host, window) =>
        {
            controller.Layout = ViewLayout.One(CfdWorkbench.Persistence.SingleView.Side);
            var point = Channel(controller, "twist", 4);
            MoveTo(controller, point, point.SpanMeters, -0.75);
            MoveTo(controller, Reload(controller, point), point.SpanMeters, -1.0);
            Select(controller, window, Reload(controller, point));
            Need<TextBox>(host.Properties, "PointAftInput").Focus();
            Settle(window);
            Save(host.Properties, System.IO.Path.Combine(directory, "twist-row.png"));
            Save(window, System.IO.Path.Combine(directory, "twist-row-window.png"));
        });
    }

    private static void Save(Control control, string path)
    {
        var size = new PixelSize((int)control.Bounds.Width, (int)control.Bounds.Height);
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
        bitmap.Render(control);
        bitmap.Save(path);
    }

    // ---------------- M1.2b2 PNL: channel rows, typed channel values, copy (docs/design/m12b2-3d-elevations.md §11.4) ----------------

    private static void ChannelRows()
    {
        DesktopChecks.Check("Properties_ChannelPoints_SpanAndValueRowsPerUnitTable", () =>
        {
            // §3.6 / §11.4: a channel point shows From root (mm), η and its value in the channel's display unit at 0.01 —
            // Height (mm), Twist (°), t/c (%) — as typed fields; one row per channel.
            using var controller = Opened();
            var failures = new List<string>();
            foreach (var (curve, label, unit, family, scale) in new[] {
                ("dihedral", "Height", "mm", UnitFamily.Length, 1000.0), ("twist", "Twist", "°", UnitFamily.Angle, 1.0),
                ("thickness", "t/c", "%", UnitFamily.Percent, 100.0) })
            {
                var point = Channel(controller, curve, 4);
                var model = Build(controller, new Selection.Points([Ref(point)]));
                var rows = model.Groups.FirstOrDefault(group => group.Id == "pos")?.Rows ?? [];
                var from = rows.FirstOrDefault(row => row.Key == "p:from");
                var value = rows.FirstOrDefault(row => row.Key == "p:aft");
                string name = PropertiesView.Curves[curve].Name;
                if (model.Identity?.Title != $"{name} · point 5 of 7") failures.Add($"{curve} identity '{model.Identity?.Title}'");
                if (from is not { Kind: RowKind.Input, Unit: "mm", Label: "From root" } || from.Value != Quantity.TypedLength(point.SpanMeters))
                    failures.Add($"{curve} from {from?.Kind} {from?.Unit} '{from?.Value}'");
                if (rows.All(row => row.Key != "p:eta")) failures.Add($"{curve} has no η");
                string want = Quantity.Typed(point.Ordinate * scale);
                if (value is null || value.Kind != RowKind.Input || value.Label != label || value.Unit != unit || value.Family != family || value.Value != want)
                    failures.Add($"{curve} value {value?.Kind} {value?.Label} {value?.Unit} {value?.Family} '{value?.Value}', want '{want}'");
            }
            // The approved mockup's screen 3b: twist point 5 of 7 at 315.00 mm from the root, −1.00°.
            var twist = Build(controller, new Selection.Points([Ref(Channel(controller, "twist", 4))])).Groups
                .FirstOrDefault(group => group.Id == "pos")?.Rows ?? [];
            string reading = string.Join(",", twist.Where(row => row.Key is "p:from" or "p:aft").Select(row => row.Value));
            if (reading != "315.00,−1.00") failures.Add("twist point 5 reads " + reading);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        DesktopChecks.Check("Properties_DihedralHandle_AngleIsLocalDihedralBothHandles", () =>
        {
            // §3.6: a Dihedral handle is typed as angle and length; the angle is the local dihedral angle of the tangent
            // line, atan2(Δheight, Δspan) root → tip, for both handles, so a Smooth anchor shows one value on both.
            using var controller = Opened();
            var anchor = AnchorOn(controller, "dihedral", 3, TangentKind.Smooth);
            var tip = Handles(controller, anchor).Tip;
            MoveTo(controller, tip, tip.SpanMeters, tip.Ordinate + 0.012);   // tilt the tangent; co-motion keeps the line
            anchor = Reload(controller, anchor);
            var (root, after) = Handles(controller, anchor);
            double expected = Math.Atan2(after.Ordinate - root.Ordinate, after.SpanMeters - root.SpanMeters) * 180 / Math.PI;
            var failures = new List<string>();
            if (Math.Abs(expected) < 0.5) failures.Add($"the tangent did not tilt ({expected}°)");
            foreach (var handle in new[] { root, after })
            {
                var rows = Build(controller, new Selection.Points([Ref(handle)])).Groups.SelectMany(group => group.Rows).ToList();
                var angle = rows.FirstOrDefault(row => row.Key == "h:angle");
                if (angle is not { Kind: RowKind.Input, Label: "Dihedral angle", Unit: "°" } || angle.Value != Quantity.PlacedAngle(expected))
                    failures.Add($"handle {handle.Index}: {angle?.Label} '{angle?.Value}', want '{Quantity.PlacedAngle(expected)}'");
                if (rows.FirstOrDefault(row => row.Key == "h:length") is not { Unit: "mm", Kind: RowKind.Input })
                    failures.Add($"handle {handle.Index} has no length field");
            }
            var anchorAngle = Build(controller, new Selection.Points([Ref(anchor)])).Groups.SelectMany(group => group.Rows)
                .FirstOrDefault(row => row.Key == "h:angle");
            if (anchorAngle?.Value != Quantity.PlacedAngle(expected)) failures.Add($"anchor angle '{anchorAngle?.Value}'");
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
        });

        Pane("Properties_TwistHandle_SpanAndValueTyped", (controller, host, window) =>
        {
            // §3.6 / §11.4: a Twist handle has no angle (an angle in mm × ° means nothing); it is typed by From root and value.
            var anchor = AnchorOn(controller, "twist", 3, TangentKind.Corner);
            var tip = Handles(controller, anchor).Tip;
            string keys = string.Join(",", Build(controller, new Selection.Points([Ref(tip)])).Groups.SelectMany(group => group.Rows)
                .Where(row => row.Kind == RowKind.Input).Select(row => row.Key + ":" + row.Unit));
            if (keys != "p:from:mm,p:aft:°") throw new InvalidOperationException("handle fields " + keys);
            Select(controller, window, tip);
            Type(controller, window, Need<TextBox>(host.Properties, "PointAftInput"), "-0.75");
            var valued = Reload(controller, tip);
            if (Math.Abs(valued.Ordinate + 0.75) > 1e-5 || Math.Abs(valued.SpanMeters - tip.SpanMeters) > 1e-9)
                throw new InvalidOperationException($"value commit: {valued.SpanMeters} m, {valued.Ordinate}°");
            Type(controller, window, Need<TextBox>(host.Properties, "PointSpanInput"),
                Quantity.ForField(Quantity.TypedLength(valued.SpanMeters + 0.005)));
            var spanned = Reload(controller, tip);
            if (Math.Abs(spanned.SpanMeters - valued.SpanMeters - 0.005) > 1e-5 || Math.Abs(spanned.Ordinate - valued.Ordinate) > 1e-5)
                throw new InvalidOperationException($"span commit: {spanned.SpanMeters} m, {spanned.Ordinate}°");
        });

        Pane("Properties_TypedTwistExpression_DegreesEchoed", (controller, host, window) =>
        {
            // MC-4 on the Twist row: an angle expression commits in degrees and its echo (COPY-157) goes to the strip.
            var point = Channel(controller, "twist", 4);
            Select(controller, window, point);
            var seen = Changes(Status(host));
            Type(controller, window, Need<TextBox>(host.Properties, "PointAftInput"), "-1.5 + 0.25");
            var moved = Reload(controller, point);
            if (Math.Abs(moved.Ordinate + 1.25) > 1e-5 || Math.Abs(moved.SpanMeters - point.SpanMeters) > 1e-9)
                throw new InvalidOperationException($"twist {moved.Ordinate}° at {moved.SpanMeters} m");
            if (!seen.Contains("-1.5 + 0.25 = −1.25°.")) throw new InvalidOperationException("strip: " + string.Join(" | ", seen));
            string field = Need<TextBox>(host.Properties, "PointAftInput").Text ?? "";
            if (field != "-1.25") throw new InvalidOperationException("field " + field);
        });

        Pane("Properties_TypedThicknessPercent_EchoedPercent", (controller, host, window) =>
        {
            // MC-4 on the t/c row: a % expression commits as a chord fraction and echoes in % (COPY-157).
            var point = Channel(controller, "thickness", 4);
            Select(controller, window, point);
            var seen = Changes(Status(host));
            Type(controller, window, Need<TextBox>(host.Properties, "PointAftInput"), "12 + 0.5");
            var moved = Reload(controller, point);
            if (Math.Abs(moved.Ordinate - 0.125) > 1e-7) throw new InvalidOperationException("t/c " + moved.Ordinate);
            if (!seen.Contains("12 + 0.5 = 12.50 %.")) throw new InvalidOperationException("strip: " + string.Join(" | ", seen));
            string field = Need<TextBox>(host.Properties, "PointAftInput").Text ?? "";
            if (field != "12.50") throw new InvalidOperationException("field " + field);
        });

        Pane("PropertiesView_TypedTwistBeyondDomain_ClampedEchoWarns", (controller, host, window) =>
        {
            // MC-19: a typed twist past the certificate's domain is clamped in Core, never refused; the echo (COPY-168) goes
            // to the strip as a warning with Core's number. The pane holds no domain constant.
            var tip = Channel(controller, "twist", 6);
            Select(controller, window, tip);
            Type(controller, window, Need<TextBox>(host.Properties, "PointAftInput"), "70");
            var clamped = Reload(controller, tip);
            string limit = Quantity.PlacedAngle(CfdWorkbench.Core.Geometry.TwistDomainDegrees);
            string want = $"70 typed; set to {limit}°, the largest that can be checked.";
            string strip = Status(host).Text ?? "";
            if (Math.Abs(clamped.Ordinate - CfdWorkbench.Core.Geometry.TwistDomainDegrees) > 1e-5 || !controller.LastGestureClamped ||
                strip != want || StatusStripTests.Kind(host) != "warning")
                throw new InvalidOperationException($"twist {clamped.Ordinate}, clamped {controller.LastGestureClamped}, strip '{strip}' ({StatusStripTests.Kind(host)})");
            string field = Need<TextBox>(host.Properties, "PointAftInput").Text ?? "";
            if (field != limit) throw new InvalidOperationException("field " + field);
        });

        Pane("StatusLine_ChannelCommitReport_PoliteLiveRegion", (controller, host, window) =>
        {
            // §11.4 "Committed move" and §11.6: a channel commit is reported once in the strip's polite status line, from
            // the operation's own numbers.
            var point = Channel(controller, "twist", 4);
            var status = Status(host);
            var seen = Changes(status);
            MoveTo(controller, point, point.SpanMeters, -1.25);
            Settle(window);
            const string want = "Moved twist point 5 by −0.25°. Tip twist −2.00°.";
            if (seen.Count(text => text == want) != 1 || AutomationProperties.GetLiveSetting(status) != AutomationLiveSetting.Polite)
                throw new InvalidOperationException($"live {AutomationProperties.GetLiveSetting(status)}: " + string.Join(" | ", seen));
        });

        Pane("WingBlock_DuringThicknessDrag_MaxTcChangesBeforeRelease", (controller, host, window) =>
        {
            // §0.1: dragging a t/c point updates the Wing's Max t/c from the draft before release; Esc restores it.
            var point = Channel(controller, "thickness", 3);
            Select(controller, window, point);
            string before = Text(host.Properties, "MaxTcText");
            if (!controller.BeginGesture(Ref(point), GestureInput.Pointer)) throw new InvalidOperationException("no gesture");
            controller.UpdateGesture(point.SpanMeters, 0.16);
            controller.FlushGestureFrame();
            Settle(window);
            string during = Text(host.Properties, "MaxTcText");
            var state = controller.Gesture;
            Pump(controller.EndGestureAsync(GestureEnd.Escape));
            Settle(window);
            string after = Text(host.Properties, "MaxTcText");
            static double Number(string text) => double.Parse(text.Replace("≈", "", StringComparison.Ordinal).Trim(), Inv);
            if (state != GestureState.Dragging || !(Number(during) > Number(before)) || after != before)
                throw new InvalidOperationException($"Max t/c {before} → {during} ({state}) → {after}");
        });

        DesktopChecks.Check("Copy_TwistClampReason_SameFormatterAsProbe", () =>
        {
            // §11.4: the clamp reason's number is Core's Geometry.TwistDomainDegrees through the placed-angle formatter that
            // the probe and Properties use, so the reason and a clamped reading show the same digits.
            string limit = Quantity.PlacedAngle(CfdWorkbench.Core.Geometry.TwistDomainDegrees);
            string want = $"Twist is limited to ±{limit}° — larger angles can't be checked yet.";
            if (PropertyCopy.TwistClamp != want) throw new InvalidOperationException("reason: " + PropertyCopy.TwistClamp);
            using var controller = Opened();
            var tip = Channel(controller, "twist", 6);
            MoveTo(controller, tip, tip.SpanMeters, 90);
            string reading = Build(controller, new Selection.Points([Ref(tip)])).Groups.First(group => group.Id == "pos").Rows
                .First(row => row.Key == "p:aft").Value;
            if (!controller.LastGestureClamped || reading != limit || !PropertyCopy.TwistClamp.Contains("±" + reading + "°", StringComparison.Ordinal))
                throw new InvalidOperationException($"clamped {controller.LastGestureClamped}, reading '{reading}', reason '{PropertyCopy.TwistClamp}'");
        });

        DesktopChecks.Check("Copy_M12b2Outcomes_ExactStrings", () =>
        {
            // §11.4 copy table, exactly; numbers come from the operation (before/after points, the curve, the estimates).
            static PointView P(string curve, int index, PointRole role, double span, double ordinate) =>
                new(curve, "cv-" + index, index, span / 0.45, span, ordinate, role, null, null, PointFreedom.Free, []);
            static CurveView C(string curve, double tip) =>
                new(curve, 16, [], [P(curve, 0, PointRole.RootEnd, 0, 0), P(curve, 6, PointRole.TipEnd, 0.45, tip)], []);
            var cases = new (string Actual, string Want)[]
            {
                (PropertyCopy.CommittedMove(P("dihedral", 4, PointRole.Control, 0.3, 0.0004), P("dihedral", 4, PointRole.Control, 0.3, 0.0124),
                    C("dihedral", 0.0124), null, null), "Moved dihedral point 5 by 12.00 mm. Tip height 12.40 mm."),
                (PropertyCopy.CommittedMove(P("twist", 6, PointRole.TipEnd, 0.45, -2), P("twist", 6, PointRole.TipEnd, 0.45, -3),
                    C("twist", -3), null, null), "Moved twist tip end by −1.00°. Tip twist −3.00°."),
                (PropertyCopy.CommittedMove(P("thickness", 1, PointRole.Control, 0.05, 0.12), P("thickness", 1, PointRole.Control, 0.05, 0.123),
                    C("thickness", 0.12), null, 0.123), "Moved thickness point 2 by 0.30 %. Max t/c 12.30 %."),
                (PropertyCopy.CommittedMove(P("trailing", 4, PointRole.Control, 0.2, 0.1), P("trailing", 4, PointRole.Control, 0.2, 0.10214),
                    C("trailing", 0.1), 0.1013, null), "Moved trailing edge point 5 by 2.14 mm. MAC 101.30 mm."),
                (PropertyCopy.LockedDihedralRoot, "The dihedral root is at the centre line. It can't be moved."),
                (PropertyCopy.CoupledRoot, "The root end and its handle move together (root mirror)."),
                (PropertyCopy.ThicknessClamp, "t/c must stay above 0 % and below 100 %."),
                (WorkbenchController.SurfaceKeptNote, "Showing the last shape that could be drawn.")
            };
            var wrong = cases.Where(item => item.Actual != item.Want).Select(item => $"'{item.Actual}' ≠ '{item.Want}'").ToList();
            if (wrong.Count > 0) throw new InvalidOperationException(string.Join("; ", wrong));
        });
    }

    // ---------------- channel fixtures ----------------

    /// <summary>The point at <paramref name="index"/> of a channel in the example foil (seven points, controls between the ends).</summary>
    internal static PointView Channel(WorkbenchController controller, string curve, int index) =>
        controller.CurveFor(curve)?.Points.FirstOrDefault(point => point.Index == index)
        ?? throw new InvalidOperationException($"{curve} has no point {index}");

    /// <summary>Makes a channel control point an anchor of <paramref name="kind"/> and returns it.</summary>
    internal static PointView AnchorOn(WorkbenchController controller, string curve, int index, TangentKind kind)
    {
        var point = Channel(controller, curve, index);
        Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(curve, point.Id)));
        var anchor = Reload(controller, point);
        if (anchor.Kind != kind) Pump(controller.ApplyPointCommandAsync(new PointCommand.SetTangent(curve, anchor.Id, kind, null)));
        return Reload(controller, anchor);
    }

    internal static (PointView Root, PointView Tip) Handles(WorkbenchController controller, PointView anchor)
    {
        var handles = controller.CurveFor(anchor.Curve)!.Points.Where(point => point.AnchorId == anchor.Id).OrderBy(point => point.Index).ToArray();
        return (handles[0], handles[^1]);
    }

    /// <summary>One typed gesture to (span, ordinate): the path Properties and the elevations commit through.</summary>
    internal static void MoveTo(WorkbenchController controller, PointView point, double spanMeters, double ordinate)
    {
        if (!controller.BeginGesture(Ref(point), GestureInput.Typed)) throw new InvalidOperationException("no gesture on " + point.Id);
        controller.UpdateGesture(spanMeters, ordinate);
        controller.FlushGestureFrame();
        var outcome = controller.EndGestureAsync(GestureEnd.Release);
        Pump(outcome);
        if (outcome.Result is not GestureOutcome.Committed) throw new InvalidOperationException("move not committed: " + outcome.Result);
    }

    /// <summary>Types into a field and presses Return, then waits for the commit.</summary>
    internal static void Type(WorkbenchController controller, Window window, TextBox field, string text)
    {
        field.Focus();
        field.Text = text;
        Key(field, Avalonia.Input.Key.Enter);
        WaitIdle(controller, window);
    }

    // ---------------- fixtures ----------------

    /// <summary>
    /// Tabs from where focus really is after a pointer selection — the Plan, on the selected point — until focus enters the
    /// pane, then on until it leaves it; a revisit inside the pane is a trap. Each step is a Tab key event on the focused
    /// element, as the keyboard raises it. NS-1: a walk that began on the pane's first stop passed while natively Tab from
    /// the Plan crossed the dock tabs and reached the Wing before the selection.
    /// </summary>
    internal static List<IInputElement> TabWalk(Control pane, Window window)
    {
        var canvas = window.GetVisualDescendants().OfType<PlanCanvas>().First(item => item.IsEffectivelyVisible);
        if (canvas.Controller?.Selection is Selection.Points { Items: [var selected] }) canvas.FocusPoint(selected);
        else canvas.Focus(NavigationMethod.Pointer);
        bool Inside() => window.FocusManager!.GetFocusedElement() is Visual visual && pane.IsVisualAncestorOf(visual);
        for (int step = 0; step < 120 && !Inside(); step++) PressTab(window);
        if (!Inside()) throw new InvalidOperationException("Tab from the Plan never reached the pane");
        var stops = new List<IInputElement>();
        for (int step = 0; step < 80 && Inside(); step++)
        {
            var focused = window.FocusManager!.GetFocusedElement()!;
            if (stops.Contains(focused)) throw new InvalidOperationException("Tab revisited " + focused.GetType().Name + " inside the pane (trap)");
            stops.Add(focused);
            PressTab(window);
        }
        return stops;
    }

    /// <summary>One Tab (Shift+Tab) key press on the focused element, routed as the keyboard device raises it.</summary>
    internal static void PressTab(Window window, bool shift = false)
    {
        var target = window.FocusManager!.GetFocusedElement() as Avalonia.Interactivity.Interactive ?? window;
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = Avalonia.Input.Key.Tab,
            KeyModifiers = shift ? KeyModifiers.Shift : KeyModifiers.None
        });
        Settle(window);
    }

    internal static void Pane(string name, Action<WorkbenchController, ShellHost, Window> body) => DesktopChecks.Check(name, () =>
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

    internal static void Nudge(string name, Action<WorkbenchController, ShellHost, Window> body) => Pane(name, (controller, host, window) =>
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

    internal static WorkbenchController Opened()
    {
        var controller = new WorkbenchController();
        Task.Run(() => controller.OpenExampleAsync()).GetAwaiter().GetResult();
        return controller;
    }

    internal static PropertiesModel Build(WorkbenchController controller, Selection selection) =>
        PropertiesView.Build(selection, controller.CurrentProjection, controller.Estimates, ShellMode.Workspace,
            new PropertiesContext(controller.Planform, Curves: controller.CurveFor));

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

    internal static PointView MakeAnchor(WorkbenchController controller)
    {
        var point = Control(controller, "trailing");
        Pump(controller.ApplyPointCommandAsync(new PointCommand.MakeAnchor(point.Curve, point.Id)));
        return Reload(controller, point);
    }

    internal static PointView Control(WorkbenchController controller, string curve) =>
        (curve == "leading" ? controller.Planform!.Leading : controller.Planform!.Trailing).Points.First(point => point.Role == PointRole.Control);

    internal static PointView Reload(WorkbenchController controller, PointView point) =>
        PropertiesView.Find(controller.CurveFor, Ref(point)) ?? throw new InvalidOperationException("point gone: " + point.Id);

    internal static PointRef Ref(PointView point) => new(point.Curve, point.Id);

    internal static void Select(WorkbenchController controller, Window window, PointView point)
    {
        controller.Select(new Selection.Points([Ref(point)]));
        Settle(window);
    }

    internal static T Part<T>(Control templated, string name) where T : Control =>
        templated.GetVisualDescendants().OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? throw new InvalidOperationException($"{templated.GetType().Name} has no {typeof(T).Name}#{name}");

    internal static void Pseudo(Control control, string state, bool on) => ((IPseudoClasses)control.Classes).Set(state, on);

    /// <summary>The window's polite status line: the status strip's (DR-STATUS-1).</summary>
    internal static TextBlock Status(ShellHost host) => StatusStripTests.Text(host);

    /// <summary>Every new text a live region takes from here on: what a screen reader would be told.</summary>
    internal static List<string> Changes(TextBlock block)
    {
        var seen = new List<string>();
        block.PropertyChanged += (_, change) => { if (change.Property == TextBlock.TextProperty) seen.Add(change.NewValue as string ?? ""); };
        return seen;
    }

    private static int Decimals(string value) => value.Contains('.') ? value.Length - value.IndexOf('.') - 1 : 0;

    internal static T Need<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name) ?? root.GetLogicalDescendants().OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? root.GetVisualDescendants().OfType<T>().FirstOrDefault(item => item.Name == name)
        ?? throw new InvalidOperationException("missing " + name);

    internal static string Text(Control root, string name) => Need<TextBlock>(root, name).Text ?? "";

    internal static void Key(Control control, Key key, KeyModifiers modifiers = KeyModifiers.None, bool up = false) =>
        control.RaiseEvent(new KeyEventArgs { RoutedEvent = up ? InputElement.KeyUpEvent : InputElement.KeyDownEvent, Source = control, Key = key, KeyModifiers = modifiers });

    internal static void Settle(Window window)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    internal static void WaitIdle(WorkbenchController controller, Window window)
    {
        var start = DateTime.UtcNow;
        while (controller.Gesture != GestureState.Idle && DateTime.UtcNow - start < TimeSpan.FromSeconds(8))
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Background);
        Settle(window);
    }

    internal static void Pump(Task task)
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
