using Avalonia;
using Avalonia.Styling;
using CfdWorkbench.Core;
using System.Globalization;
using System.Diagnostics;
using System.Text;

namespace CfdWorkbench.Desktop;

internal static class Program
{
    internal static long ManagedStartTicks { get; private set; }
    [STAThread]
    private static void Main(string[] args)
    {
        ManagedStartTicks = Stopwatch.GetTimestamp();
        NativeReviewOptions.Current = NativeReviewOptions.Parse(Environment.GetEnvironmentVariable);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

public static class NativeReviewThemes
{
    public static readonly ThemeVariant HighContrast = new("HighContrast", ThemeVariant.Light);
}

/// <summary>Process-local, review-only selectors. They exercise real controller states and never invent geometry.</summary>
public sealed record NativeReviewOptions(string Persona, int Width, int Height, string State,
    string Theme, bool ReducedMotion, string? Path)
{
    public static NativeReviewOptions? Current { get; internal set; }

    public static NativeReviewOptions? Parse(Func<string, string?> read)
    {
        var enabled = read("CFDW_REVIEW_MODE");
        if (enabled is null) return null;
        if (enabled != "1") throw new ArgumentException("CFDW_REVIEW_MODE must be 1");
        var persona = read("CFDW_REVIEW_PERSONA") ?? "designer";
        if (persona is not ("designer" or "keyboard" or "screen-reader" or "dense"))
            throw new ArgumentException("Unknown review persona");
        var size = (read("CFDW_REVIEW_SIZE") ?? "1280x800").Split('x');
        if (size.Length != 2 || !int.TryParse(size[0], NumberStyles.None, CultureInfo.InvariantCulture, out int width) ||
            !int.TryParse(size[1], NumberStyles.None, CultureInfo.InvariantCulture, out int height) ||
            width is < 1024 or > 2560 || height is < 700 or > 1600)
            throw new ArgumentException("Review window must be within 1024x700 and 2560x1600");
        var state = read("CFDW_REVIEW_STATE") ?? "example";
        if (state is not ("empty" or "example" or "draft" or "invalid-input" or "refused-open" or
            "file" or "import" or "recovery" or "not-assessed" or "invalid-geometry"))
            throw new ArgumentException("Unknown review state");
        var theme = read("CFDW_REVIEW_THEME") ?? "system";
        if (theme is not ("system" or "light" or "dark" or "high-contrast"))
            throw new ArgumentException("Unknown review theme");
        var motion = read("CFDW_REVIEW_REDUCED_MOTION") ?? "0";
        if (motion is not ("0" or "1")) throw new ArgumentException("Review motion selector must be 0 or 1");
        var path = read("CFDW_REVIEW_PATH");
        if (state is "import" or "not-assessed" or "invalid-geometry" && (path is null || !path.EndsWith(".foil", StringComparison.OrdinalIgnoreCase)) ||
            state == "recovery" && (path is null || !path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase)) ||
            state == "file" && (path is null || !(path.EndsWith(".foil", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".cfdw.json", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Review state requires a matching real source path");
        if (persona == "dense" && state != "file") throw new ArgumentException("Dense review requires a real certified file");
        return new(persona, width, height, state, theme, motion == "1", path);
    }

    public async Task ApplyStateAsync(WorkbenchController workbench)
    {
        if (State == "empty") return;
        if (State is "not-assessed" or "invalid-geometry")
        {
            var bytes = await CfdWorkbench.Cli.Cli.ReadFoilBoundedAsync(Path!);
            var parsed = FoilSource.Parse(bytes);
            var expected = State == "not-assessed" ? GeometryStatus.NotAssessed : GeometryStatus.Invalid;
            if (!parsed.IsParsed || Geometry.Assess(parsed).Status != expected)
                throw new ArgumentException($"Review source did not produce {expected} geometry");
            await workbench.OpenFoilAsync(bytes, Path!);
            if (workbench.PendingOriginal is null || workbench.Inspection is not null)
                throw new InvalidOperationException($"{expected} source was not retained without acceptance");
            return;
        }
        if (State is "file" or "import" or "recovery")
        {
            await workbench.OpenPathAsync(Path!);
            if (State == "file" && workbench.Inspection?.Geometry.Status != GeometryStatus.Certified)
                throw new ArgumentException("Review file did not yield certified accepted geometry");
            if (Persona == "dense" && workbench.Inspection is { } dense &&
                dense.Authored.Assignments.Count + dense.Authored.Rails.Sum(rail => rail.Controls.Count) < 16)
                throw new ArgumentException("Dense review file has fewer than 16 actual station/control annotations");
            if (State == "import" && workbench.PendingCandidate is null)
                throw new ArgumentException("Review source did not produce an explicit-ID import candidate");
            if (State == "recovery" && !workbench.HasRecovery)
                throw new ArgumentException("Review project had no separate recovery offer");
            return;
        }
        await workbench.OpenExampleAsync();
        if (State == "refused-open")
        {
            await workbench.OpenFoilAsync(Encoding.UTF8.GetBytes("not FoilDSL"), "Review invalid source");
            if (workbench.PendingOriginal is null || workbench.Inspection?.Geometry.Status != GeometryStatus.Certified)
                throw new InvalidOperationException("Refused-open state did not retain the accepted example");
            return;
        }
        if (State is not ("draft" or "invalid-input")) return;
        var target = workbench.Inspection!.Authored.Rails.SelectMany(rail => rail.Controls
            .Where(control => control.Editable).Select(control => (rail.Name, control.Id))).First();
        workbench.BeginEdit(target.Name, target.Id);
        if (State == "invalid-input") workbench.InvalidateDraftInput();
        if (workbench.Draft is null || State == "invalid-input" && workbench.DraftInputValid)
            throw new InvalidOperationException("Review draft state did not materialize");
    }
}
