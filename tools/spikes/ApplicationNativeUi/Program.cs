// Reproducible architecture spike, not the product parser/evaluator or M1 implementation.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace ApplicationNativeUi;

// Proposed compiling shared port vocabulary. Implementations remain a downstream gated task.
public enum Assessment { Valid, Invalid, NotAssessed, Unsupported, Cancelled, Conflict }
public sealed record Diagnostic(string Code, Assessment State, int ByteStart, int ByteLength, string Message, string Recovery);
public readonly record struct RevisionId(string Value);
public sealed record SourceSnapshot(ImmutableArray<byte> Utf8, string Sha256);
public sealed record AcceptedRevision(RevisionId Id, RevisionId? Parent, SourceSnapshot Source, string DefinitionHash);
public enum Rail { Leading, Trailing }
public sealed record DraftTarget(Rail Rail, string VertexId);
public sealed record OwnedDraft(Guid Id, RevisionId Base, DraftTarget Target, SourceSnapshot Candidate, long Generation);
public sealed record Point3(double X, double Y, double Z);
public sealed record ValidationKey(string SourceSha256, RevisionId Base, Guid DraftId, long Generation, string Evaluator);
public sealed record PreviewProvenance(ValidationKey Key, int Samples, double? MaximumErrorMetres);
public sealed record GeometryView(PreviewProvenance Provenance, ImmutableArray<Point3> Points);
public sealed class GeometryCertificate
{
    internal GeometryCertificate(ValidationKey key, string algorithm, string definitionHash) { Key = key; Algorithm = algorithm; DefinitionHash = definitionHash; }
    public ValidationKey Key { get; }
    public string Algorithm { get; }
    public string DefinitionHash { get; }
}
public sealed record Validation(ValidationKey Key, Assessment State, ImmutableArray<Diagnostic> Diagnostics, string? DefinitionHash, GeometryCertificate? Certificate, GeometryView? Preview);
public static class ValidationBinding
{
    public static void RequireMatch(AcceptedRevision current, OwnedDraft draft, Validation validation)
    {
        string actualHash = Convert.ToHexStringLower(SHA256.HashData(draft.Candidate.Utf8.AsSpan()));
        var required = new ValidationKey(actualHash, draft.Base, draft.Id, draft.Generation, "cfdw-cv/1");
        if (current.Id != draft.Base || draft.Candidate.Sha256 != actualHash || validation.Key != required ||
            validation.State != Assessment.Valid || validation.Certificate?.Key != required ||
            string.IsNullOrEmpty(validation.DefinitionHash) || validation.DefinitionHash != validation.Certificate.DefinitionHash)
            throw new InvalidOperationException("DSL-CONFLICT: certificate does not bind this accepted base and candidate.");
    }
}
public sealed record SaveToken(string? ExpectedFileSha256);
public interface IAuthoringCore
{
    Validation Validate(OwnedDraft candidate, CancellationToken cancellation);
    OwnedDraft SetRailOrdinate(AcceptedRevision basis, DraftTarget target, double metres);
    AcceptedRevision Apply(AcceptedRevision current, OwnedDraft draft, Validation currentValidation);
}
public interface IProjectStore
{
    Task<ReadOnlyMemory<byte>> ReadAsync(string path, CancellationToken cancellation);
    Task<SaveToken> SaveAsync(string path, ReadOnlyMemory<byte> envelope, SaveToken expected, CancellationToken cancellation);
}

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--hash")
        {
            byte[] bytes = File.ReadAllBytes(args[1]);
            Console.WriteLine(JsonSerializer.Serialize(new { sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), blake3 = Blake3.Hasher.Hash(bytes).ToString() }));
            return;
        }
        if (args.Contains("--contracts"))
        {
            double[] values = [0d, -0d, 0.014049d, 1e-7d, 1e21d, double.Epsilon];
            Console.WriteLine(JsonSerializer.Serialize(values.Select(x => new { bits = BitConverter.DoubleToInt64Bits(x).ToString("x16"), roundtrip = x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) })));
            var source = new SourceSnapshot([1, 2, 3], Convert.ToHexStringLower(SHA256.HashData([1, 2, 3])));
            var revision = new AcceptedRevision(new("r1"), null, source, "fixture-only");
            var draft = new OwnedDraft(Guid.Empty, revision.Id, new(Rail.Leading, "le-2"), source, 1);
            var key = new ValidationKey(source.Sha256, revision.Id, Guid.Empty, 1, "cfdw-cv/1");
            var validation = new Validation(key, Assessment.Valid, [], "fixture-only", new GeometryCertificate(key, "fixture-binding-only", "fixture-only"), null);
            ValidationBinding.RequireMatch(revision, draft, validation);
            int refused = 0;
            foreach (var bad in new[] { validation with { Key = key with { Generation = 2 } }, validation with { Key = key with { SourceSha256 = "other" } }, validation with { Certificate = null }, validation with { State = Assessment.NotAssessed }, validation with { DefinitionHash = "forged" }, validation with { DefinitionHash = "" }, validation with { Key = key with { Base = new("other") } }, validation with { Key = key with { DraftId = Guid.Parse("00000000-0000-0000-0000-000000000001") } } })
            {
                try { ValidationBinding.RequireMatch(revision, draft, bad); }
                catch (InvalidOperationException) { refused++; }
            }
            try { ValidationBinding.RequireMatch(revision with { Id = new("moved") }, draft, validation); }
            catch (InvalidOperationException) { refused++; }
            if (refused != 9) throw new Exception("Mismatched validation was accepted.");
            Console.WriteLine("ValidationBinding: matching accepted; 9 mismatches refused. Fixture certificate proves binding only.");
            return;
        }
        AppBuilder.Configure<SpikeApplication>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}

public sealed class SpikeApplication : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new SpikeWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

public sealed class SpikeWindow : Window
{
    private readonly TextBlock status = new() { Text = "Architecture spike. Analysis Unavailable.", TextWrapping = TextWrapping.Wrap };
    public SpikeWindow()
    {
        Title = "CFD-Workbench architecture spike"; Width = 900; Height = 600;
        var input = new TextBox { Text = "0.014049", Watermark = "Leading edge x (m)" };
        AutomationProperties.SetName(input, "Leading edge x in metres");
        var open = new Button { Content = "Open foil…", MinHeight = 32 };
        AutomationProperties.SetName(open, "Open foil file");
        open.Click += async (_, _) =>
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open FoilDSL", AllowMultiple = false });
                status.Text = files.Count == 0 ? "Open cancelled. Accepted source unchanged." : "Picker returned one file; no product parsing in this spike.";
                Console.WriteLine(JsonSerializer.Serialize(new { operation = "native.picker", count = files.Count, milliseconds = watch.Elapsed.TotalMilliseconds }));
                foreach (var file in files) file.Dispose();
            }
            catch (Exception error) { status.Text = "Picker failed: " + error.GetType().Name; Console.WriteLine(status.Text); }
        };
        var viewport = new SpikeViewport { MinHeight = 260, Focusable = true };
        AutomationProperties.SetName(viewport, "Foil viewport. Illustrative open-tip section. No simulation.");
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = "Native offline contract spike", FontSize = 22 });
        panel.Children.Add(input); panel.Children.Add(open); panel.Children.Add(viewport); panel.Children.Add(status);
        Content = panel;
        Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            input.Focus();
            var peer = ControlAutomationPeer.CreatePeerForElement(input);
            Console.WriteLine(JsonSerializer.Serialize(new { operation = "native.open", process = Environment.ProcessId, os = Environment.OSVersion.ToString(), architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(), canOpen = StorageProvider.CanOpen, automationPeer = peer?.GetType().Name, name = peer?.GetName(), viewportWidth = viewport.Bounds.Width, viewportHeight = viewport.Bounds.Height }));
        });
    }
}

public sealed class SpikeViewport : Control
{
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, Bounds.WithX(0).WithY(0));
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(new Point(30, 140), false);
            path.CubicBezierTo(new Point(140, 35), new Point(550, 130), new Point(760, 140));
            path.CubicBezierTo(new Point(450, 170), new Point(70, 185), new Point(30, 140));
            path.EndFigure(true);
        }
        context.DrawGeometry(null, new Pen(Brushes.Turquoise, 3), geometry);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ControlAutomationPeer(this);
}
