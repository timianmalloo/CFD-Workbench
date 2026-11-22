using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CfdWorkbench.Core;

// Machine-readable A5.5 hydrofoil sanity-bound advisory DRC (docs/design/setup-seeding.md §4.4, ANA-20).
// Loaded from embedded CatalogData/SanityBounds.json, BLAKE3-checked at load, advisory only (never gates).

public sealed record SanityBoundRow(
    string RuleId,
    string Label,
    double Min,
    double Max,
    string Unit,
    string Confidence,
    string Source,
    string AccessDate,
    string LocationKey,
    string JumpTarget);

public sealed record SanityBoundsCatalog(
    string Version,
    string Source,
    string TableHash,
    IReadOnlyList<SanityBoundRow> Bounds);

public static class SanityBounds
{
    private const string ResourceName = "CfdWorkbench.Core.CatalogData.SanityBounds.json";

    // Pinned BLAKE3 of LF-normalized CatalogData/SanityBounds.json.
    internal const string PinnedHash = "263dcef369f90336e79c3fafd5c141792b82e5e9abfa3829cbdfa2a1381063e6";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly byte[] RawBytes = LoadResource();

    public static string ComputeCurrentResourceHash() => Identity.Blake3(NormalizeLf(RawBytes));

    public static SanityBoundsCatalog Load() => LoadFromBytes(RawBytes);

    internal static SanityBoundsCatalog LoadFromBytes(byte[] bytes)
    {
        byte[] normalized = NormalizeLf(bytes);
        string hash = Identity.Blake3(normalized);
        if (!string.Equals(hash, PinnedHash, StringComparison.Ordinal))
            throw new ContractError("CAT-UNAVAILABLE", $"SanityBounds.json failed its BLAKE3 integrity check (actual {hash}).");
        var doc = JsonSerializer.Deserialize<SanityBoundsFile>(normalized, JsonOptions)
            ?? throw new ContractError("CAT-UNAVAILABLE", "SanityBounds.json is empty.");
        if (doc.Bounds is null || doc.Bounds.Count == 0)
            throw new ContractError("CAT-UNAVAILABLE", "SanityBounds.json contains no bounds.");
        return new SanityBoundsCatalog(doc.Version, doc.Source, hash, doc.Bounds);
    }

    /// <summary>
    /// Evaluates advisory sanity bounds against current wing geometry and optional Goal state.
    /// Emits <see cref="DrcSeverity.Advisory"/> findings formatted per COPY-49; never gates seeding.
    /// </summary>
    public static IReadOnlyList<DrcFinding> Evaluate(
        WingEstimates wing,
        GoalStateVersion? goal = null,
        IReadOnlyList<DrcExclusion>? exclusions = null,
        string? designId = null)
    {
        ArgumentNullException.ThrowIfNull(wing);
        var catalog = Load();
        var findings = new List<DrcFinding>();

        var metrics = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["envelope.area"] = wing.AreaSquareMeters * 1e4,
            ["envelope.span"] = wing.SpanMeters * 1e3,
            ["envelope.aspect_ratio"] = wing.AspectRatio,
            ["envelope.root_tc"] = wing.MaxThicknessRatio * 100.0
        };

        if (goal is not null && wing.AreaSquareMeters > 0)
        {
            metrics["envelope.wing_loading"] = (goal.FrontWingLoadN / wing.AreaSquareMeters) / 1000.0;
            var takeoff = goal.OperatingPoints.FirstOrDefault(op => op.CriticalMetric == "takeoff-cl");
            if (takeoff is not null && takeoff.SpeedMetersPerSecond > 0)
            {
                double q = 0.5 * goal.Water.Rho * takeoff.SpeedMetersPerSecond * takeoff.SpeedMetersPerSecond;
                metrics["envelope.takeoff_cl"] = takeoff.RequiredLoadN / (q * wing.AreaSquareMeters);
            }
            var cruise = goal.OperatingPoints.FirstOrDefault(op => op.CriticalMetric == "cruise-ld");
            if (cruise is not null && cruise.SpeedMetersPerSecond > 0)
            {
                double q = 0.5 * goal.Water.Rho * cruise.SpeedMetersPerSecond * cruise.SpeedMetersPerSecond;
                metrics["envelope.cruise_cl"] = cruise.RequiredLoadN / (q * wing.AreaSquareMeters);
            }
        }

        foreach (var bound in catalog.Bounds)
        {
            if (!metrics.TryGetValue(bound.RuleId, out double actual) || !double.IsFinite(actual))
                continue;
            if (actual < bound.Min - 1e-9 || actual > bound.Max + 1e-9)
            {
                string unitSuffix = string.IsNullOrEmpty(bound.Unit) ? "" : " " + bound.Unit;
                string message = string.Create(CultureInfo.InvariantCulture,
                    $"Out-of-envelope observation — {bound.Label} {actual:0.##}{unitSuffix} outside {bound.Min:0.##}–{bound.Max:0.##}{unitSuffix} ({bound.Source}, {bound.AccessDate}).");
                findings.Add(new DrcFinding(
                    bound.RuleId,
                    catalog.Version,
                    bound.AccessDate,
                    DrcSeverity.Advisory,
                    bound.LocationKey,
                    bound.Label,
                    message,
                    bound.JumpTarget));
            }
        }

        return ClassRules.ApplyExclusions(findings, exclusions, designId);
    }

    private static byte[] LoadResource()
    {
        using Stream? stream = typeof(SanityBounds).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
            throw new ContractError("CAT-UNAVAILABLE", "SanityBounds.json embedded resource is missing.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] NormalizeLf(byte[] input)
    {
        string text = Encoding.UTF8.GetString(input).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(text);
    }

    private sealed record SanityBoundsFile(string Version, string Source, IReadOnlyList<SanityBoundRow> Bounds);
}
