using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CfdWorkbench.Persistence;

/// <summary>Read and write of <c>cfdw-layout</c> version 1 (docs/design/app-shell.md §3.4–§3.5). Never throws.</summary>
public static class LayoutCodec
{
    public const int MaxBytes = 64 * 1024;
    // Reader bound is the design's MaxDepth. Writer is one deeper: measured on the §3.4 example,
    // reflection serialization of these IReadOnlyList records throws at MaxDepth 8
    // (path $.Workspaces.Regions.Groups.Panes) while JsonDocument still reads those bytes at 8.
    public const int ReaderMaxDepth = 8;
    public const int WriterMaxDepth = 9;

    private static readonly JsonSerializerOptions Reader = Options(ReaderMaxDepth);
    private static readonly JsonSerializerOptions Writer = Options(WriterMaxDepth);
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly WorkspaceId[] WorkspaceOrder = [WorkspaceId.Planform, WorkspaceId.Precision, WorkspaceId.Review];
    private static readonly RegionId[] RegionOrder = [RegionId.Left, RegionId.Bottom, RegionId.Right];
    /// <summary>
    /// Each registered pane's home region, in placement order: the one table the presets, the desktop's
    /// <c>WorkspacePresets</c> and a repair of a saved file read. M1.2c (OD-2 A, OD-3 B): there is no Messages pane, and
    /// Points lives in the right side bar. A saved file that still names "messages" loses it with LAYOUT-PANE.
    /// A3a (G-T6): Layers is a left-dock tab after Browser; a file saved before it gets it placed, never refused.
    /// The Analysis bottom panel is not a pane (no Homes row): it is a shell slot shown only in Analysis, so no saved
    /// layout can place, close or float it. <c>rail-controls</c> likewise has no row: it is an unfilled left tool.
    /// </summary>
    public static IReadOnlyList<(string Pane, RegionId Region)> Homes { get; } =
    [
        ("properties", RegionId.Left),
        ("browser", RegionId.Left),
        ("layers", RegionId.Left),
        ("points", RegionId.Right)
    ];

    public readonly record struct LayoutPeek(string? Format, int? Version, bool Failed);

    public sealed record LayoutParse(LayoutDocument Document, IReadOnlyList<string> Codes, int DroppedPanes, int Clamped, bool NeverWrite);

    public static LayoutPeek Peek(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var body = StripBom(bytes);
            _ = Utf8.GetString(body);
            using var doc = Open(body, 64);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return new LayoutPeek(null, null, true);
            string? format = null;
            int? version = null;
            if (doc.RootElement.TryGetProperty("format", out var formatValue) && formatValue.ValueKind == JsonValueKind.String)
                format = formatValue.GetString();
            if (doc.RootElement.TryGetProperty("version", out var versionValue) && versionValue.ValueKind == JsonValueKind.Number && versionValue.TryGetInt32(out var parsed))
                version = parsed;
            return new LayoutPeek(format, version, false);
        }
        catch (Exception)
        {
            return new LayoutPeek(null, null, true);
        }
    }

    public static LayoutParse Parse(ReadOnlySpan<byte> bytes, IReadOnlySet<string> registeredPanes)
    {
        try
        {
            var registered = registeredPanes ?? new HashSet<string>(StringComparer.Ordinal);
            bool bom = HasBom(bytes);
            var peek = Peek(bytes);
            if (!peek.Failed && peek.Format == LayoutDocument.FormatName && peek.Version is > LayoutDocument.CurrentVersion)
                return new LayoutParse(Presets(WorkspaceId.Planform, registered), ["LAYOUT-VERSION"], 0, 0, true);
            if (bom || bytes.Length > MaxBytes || peek.Failed || peek.Format != LayoutDocument.FormatName || peek.Version is null or < 1)
                return Schema(registered);
            try
            {
                using var depth = Open(bytes, ReaderMaxDepth);
            }
            catch (JsonException)
            {
                return Schema(registered);
            }
            return ParseDocument(bytes, registered);
        }
        catch (Exception)
        {
            return Schema(registeredPanes ?? new HashSet<string>(StringComparer.Ordinal));
        }
    }

    public static byte[] Serialize(LayoutDocument document)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(document, Writer);
        }
        catch (Exception)
        {
            return Utf8.GetBytes("{\"format\":\"cfdw-layout\",\"version\":1,\"active\":\"planform\",\"workspaces\":[]}");
        }
    }

    // assume: single view is plan for every preset. §3.4's preset table names arrangement only;
    // the worked example's single is plan. Confirmed if a later preset table names single; one constant changes.
    public static LayoutDocument Presets(WorkspaceId active, IReadOnlySet<string> registeredPanes)
    {
        if (!WorkspaceOrder.Contains(active)) active = WorkspaceId.Planform;
        var workspaces = new List<WorkspaceLayout>(WorkspaceOrder.Length);
        foreach (var id in WorkspaceOrder) workspaces.Add(Preset(id, registeredPanes));
        return new LayoutDocument(active, workspaces);
    }

    public static WorkspaceLayout Preset(WorkspaceId id, IReadOnlySet<string> registeredPanes)
    {
        var registered = registeredPanes ?? new HashSet<string>(StringComparer.Ordinal);
        var regions = new List<RegionLayout>(RegionOrder.Length);
        foreach (var region in RegionOrder)
        {
            var (open, size) = Chrome(id, region);
            var panes = Homes.Where(home => home.Region == region && registered.Contains(home.Pane)).Select(home => home.Pane).ToArray();
            IReadOnlyList<PaneGroup> groups = panes.Length == 0 ? [] : [new PaneGroup(panes, panes[0], 1)];
            regions.Add(new RegionLayout(region, open, size, groups));
        }
        var arrangement = id == WorkspaceId.Review ? ViewArrangement.Four : ViewArrangement.Plan3d;
        return new WorkspaceLayout(id, new WorkspaceViews(arrangement, SingleView.Plan), regions, [], []);
    }

    internal static bool TryWorkspace(string? name, out WorkspaceId id)
    {
        switch (name)
        {
            case "planform": id = WorkspaceId.Planform; return true;
            case "precision": id = WorkspaceId.Precision; return true;
            case "review": id = WorkspaceId.Review; return true;
            default: id = WorkspaceId.Planform; return false;
        }
    }

    private static LayoutParse ParseDocument(ReadOnlySpan<byte> bytes, IReadOnlySet<string> registered)
    {
        using var doc = Open(bytes, ReaderMaxDepth);
        var root = doc.RootElement;
        foreach (var property in root.EnumerateObject())
            if (property.Name is not ("format" or "version" or "active" or "workspaces"))
                return Schema(registered);
        if (!root.TryGetProperty("workspaces", out var workspaceValues) || workspaceValues.ValueKind != JsonValueKind.Array)
            return Schema(registered);
        var codes = new List<string>();
        WorkspaceId active = WorkspaceId.Planform;
        if (!root.TryGetProperty("active", out var activeValue) || activeValue.ValueKind != JsonValueKind.String || !TryWorkspace(activeValue.GetString(), out active))
        {
            active = WorkspaceId.Planform;
            Add(codes, "LAYOUT-WORKSPACE");
        }
        var seen = new HashSet<WorkspaceId>();
        var workspaces = new List<WorkspaceLayout>();
        int dropped = 0, clamped = 0;
        foreach (var element in workspaceValues.EnumerateArray())
        {
            if (workspaces.Count >= 3) { Add(codes, "LAYOUT-WORKSPACE"); break; }
            WorkspaceId? id = element.ValueKind == JsonValueKind.Object ? ReadId(element) : null;
            try
            {
                if (element.ValueKind != JsonValueKind.Object) throw new JsonException("workspace");
                var raw = JsonSerializer.Deserialize<WorkspaceLayout>(element.GetRawText(), Reader) ?? throw new JsonException("workspace");
                if (id is null || raw.Id != id.Value || !seen.Add(raw.Id))
                {
                    Add(codes, "LAYOUT-WORKSPACE");
                    if (id is WorkspaceId known && seen.Add(known)) workspaces.Add(Preset(known, registered));
                    continue;
                }
                workspaces.Add(Normalize(raw, registered, codes, ref dropped, ref clamped));
            }
            catch (Exception)
            {
                Add(codes, "LAYOUT-WORKSPACE");
                if (id is WorkspaceId known && seen.Add(known)) workspaces.Add(Preset(known, registered));
            }
        }
        if (!seen.Contains(active)) workspaces.Add(Preset(active, registered));
        workspaces.Sort((a, b) => Array.IndexOf(WorkspaceOrder, a.Id).CompareTo(Array.IndexOf(WorkspaceOrder, b.Id)));
        return new LayoutParse(new LayoutDocument(active, workspaces), codes.ToArray(), dropped, clamped, false);
    }

    private static WorkspaceLayout Normalize(WorkspaceLayout raw, IReadOnlySet<string> registered, List<string> codes, ref int dropped, ref int clamped)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var regions = new Dictionary<RegionId, WorkingRegion>();
        var seenRegion = new HashSet<RegionId>();
        foreach (var region in raw.Regions)
            if (!seenRegion.Add(region.Id)) Add(codes, "LAYOUT-WORKSPACE");
        // Pane priority is left, then bottom, then right, independent of file order (§3.5 V5).
        foreach (var id in RegionOrder)
        {
            RegionLayout? region = null;
            foreach (var candidate in raw.Regions)
                if (candidate.Id == id) { region = candidate; break; }
            if (region is null) continue;
            regions[id] = WorkingRegion.From(region, registered, seen, codes, ref dropped, ref clamped);
        }
        var floats = new List<FloatLayout>();
        foreach (var floated in raw.Floats)
        {
            if (floats.Count >= 4)
            {
                Add(codes, "LAYOUT-PANE");
                dropped += floated.Panes.Count;
                continue;
            }
            if (!double.IsFinite(floated.Width) || !double.IsFinite(floated.Height))
            {
                clamped++;
                Add(codes, "LAYOUT-CLAMPED");
                Dock(regions, floated, registered, seen, ref dropped);
                continue;
            }
            var panes = Take(floated.Panes, registered, seen, 4, codes, ref dropped);
            if (panes.Count == 0) { Add(codes, "LAYOUT-PANE"); continue; }
            var active = panes.Contains(floated.Active) ? floated.Active : panes[0];
            if (active != floated.Active) Add(codes, "LAYOUT-PANE");
            double width = Clamp(floated.Width, 160, 8192, ref clamped, codes);
            double height = Clamp(floated.Height, 120, 8192, ref clamped, codes);
            floats.Add(new FloatLayout(panes, active, floated.X, floated.Y, width, height, floated.Screen, floated.Origin));
        }
        var closed = Take(raw.Closed, registered, seen, int.MaxValue, codes, ref dropped);
        PlaceMissing(raw.Id, regions, registered, seen, codes, ref dropped);
        var ordered = new List<RegionLayout>();
        foreach (var id in RegionOrder)
            if (regions.TryGetValue(id, out var region)) ordered.Add(region.Freeze());
        return new WorkspaceLayout(raw.Id, raw.Views, ordered, floats, closed);
    }

    private static void PlaceMissing(WorkspaceId workspace, Dictionary<RegionId, WorkingRegion> regions, IReadOnlySet<string> registered, HashSet<string> seen, List<string> codes, ref int dropped)
    {
        foreach (var (pane, regionId) in Homes)
        {
            if (!registered.Contains(pane) || !seen.Add(pane)) continue;
            if (!regions.TryGetValue(regionId, out var region))
            {
                var (open, size) = Chrome(workspace, regionId);
                region = new WorkingRegion(regionId, open, size);
                regions[regionId] = region;
            }
            if (region.Groups.Count == 0) region.Groups.Add(new WorkingGroup());
            var group = region.Groups[0];
            Insert(group.Panes, pane, Homes.Where(home => home.Region == regionId).Select(home => home.Pane).ToArray());
            if (group.Active is null || !group.Panes.Contains(group.Active)) group.Active = group.Panes[0];
            dropped++;
            Add(codes, "LAYOUT-PANE");
        }
    }

    private static void Dock(Dictionary<RegionId, WorkingRegion> regions, FloatLayout floated, IReadOnlySet<string> registered, HashSet<string> seen, ref int dropped)
    {
        if (!regions.TryGetValue(floated.Origin.Region, out var region) || floated.Origin.Group < 0 || floated.Origin.Group >= region.Groups.Count)
            return;
        var group = region.Groups[floated.Origin.Group];
        int index = Math.Clamp(floated.Origin.Index, 0, group.Panes.Count);
        foreach (var pane in floated.Panes)
        {
            if (string.IsNullOrEmpty(pane) || !registered.Contains(pane) || !seen.Add(pane)) { dropped++; continue; }
            group.Panes.Insert(Math.Min(index, group.Panes.Count), pane);
            index++;
        }
        if (group.Active is null || !group.Panes.Contains(group.Active)) group.Active = group.Panes.Count == 0 ? null : group.Panes[0];
    }

    private static List<string> Take(IReadOnlyList<string> panes, IReadOnlySet<string> registered, HashSet<string> seen, int limit, List<string> codes, ref int dropped)
    {
        var kept = new List<string>();
        foreach (var pane in panes)
        {
            if (kept.Count >= limit || string.IsNullOrEmpty(pane) || !registered.Contains(pane) || !seen.Add(pane))
            {
                dropped++;
                Add(codes, "LAYOUT-PANE");
                continue;
            }
            kept.Add(pane);
        }
        return kept;
    }

    private static void Insert(List<string> panes, string pane, string[] order)
    {
        int at = Array.IndexOf(order, pane);
        int index = panes.Count;
        for (int i = 0; i < panes.Count; i++)
        {
            int other = Array.IndexOf(order, panes[i]);
            if (other < 0 || (at >= 0 && other > at)) { index = i; break; }
        }
        panes.Insert(index, pane);
    }

    private static WorkspaceId? ReadId(JsonElement element)
    {
        if (!element.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String) return null;
        return TryWorkspace(id.GetString(), out var parsed) ? parsed : null;
    }

    // §11.8: Precision = Planform plus the right side bar (Points); Review shows no panes. No pane's home is the bottom
    // panel any more, so it is closed in every preset.
    private static (bool Open, double Size) Chrome(WorkspaceId id, RegionId region) => region switch
    {
        RegionId.Left => (id != WorkspaceId.Review, 260),
        RegionId.Bottom => (false, 190),
        _ => (id == WorkspaceId.Precision, 260)
    };

    private static double Clamp(double value, double min, double max, ref int clamped, List<string> codes)
    {
        if (!double.IsFinite(value) || value < min || value > max)
        {
            clamped++;
            Add(codes, "LAYOUT-CLAMPED");
            if (!double.IsFinite(value)) return min;
            return Math.Clamp(value, min, max);
        }
        return value;
    }

    private static double ClampShare(double share, ref int clamped, List<string> codes)
    {
        if (!double.IsFinite(share) || share <= 0 || share > 1)
        {
            clamped++;
            Add(codes, "LAYOUT-CLAMPED");
            return 1;
        }
        return share;
    }

    private static LayoutParse Schema(IReadOnlySet<string> registered) =>
        new(Presets(WorkspaceId.Planform, registered), ["LAYOUT-SCHEMA"], 0, 0, false);

    private static void Add(List<string> codes, string code)
    {
        if (!codes.Contains(code)) codes.Add(code);
    }

    private static JsonDocument Open(ReadOnlySpan<byte> bytes, int depth) =>
        JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = depth });

    private static bool HasBom(ReadOnlySpan<byte> bytes) => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

    private static ReadOnlySpan<byte> StripBom(ReadOnlySpan<byte> bytes) => HasBom(bytes) ? bytes[3..] : bytes;

    private static JsonSerializerOptions Options(int depth)
    {
        var options = new JsonSerializerOptions(LayoutJsonContext.Default.Options) { MaxDepth = depth };
        options.Converters.Insert(0, new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    private sealed class WorkingRegion
    {
        internal WorkingRegion(RegionId id, bool open, double size) { Id = id; Open = open; Size = size; }
        internal RegionId Id { get; }
        internal bool Open { get; }
        internal double Size { get; }
        internal List<WorkingGroup> Groups { get; } = [];
        internal static WorkingRegion From(RegionLayout region, IReadOnlySet<string> registered, HashSet<string> seen, List<string> codes, ref int dropped, ref int clamped)
        {
            double min = region.Id == RegionId.Bottom ? 120 : 200;
            double max = region.Id == RegionId.Bottom ? 480 : 420;
            var working = new WorkingRegion(region.Id, region.Open, Clamp(region.Size, min, max, ref clamped, codes));
            foreach (var group in region.Groups)
            {
                if (working.Groups.Count >= 4) { Add(codes, "LAYOUT-PANE"); dropped += group.Panes.Count; continue; }
                var panes = Take(group.Panes, registered, seen, 4, codes, ref dropped);
                if (panes.Count == 0) { Add(codes, "LAYOUT-PANE"); continue; }
                var active = panes.Contains(group.Active) ? group.Active : panes[0];
                if (active != group.Active) Add(codes, "LAYOUT-PANE");
                working.Groups.Add(new WorkingGroup { Panes = panes, Active = active, Share = ClampShare(group.Share, ref clamped, codes) });
            }
            return working;
        }
        internal RegionLayout Freeze() => new(Id, Open, Size, Groups.Select(group => new PaneGroup(group.Panes, group.Active ?? group.Panes[0], group.Share)).ToArray());
    }

    private sealed class WorkingGroup
    {
        internal List<string> Panes { get; set; } = [];
        internal string? Active { get; set; }
        internal double Share { get; set; } = 1;
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    NewLine = "\n",
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(LayoutDocument))]
[JsonSerializable(typeof(WorkspaceLayout))]
internal partial class LayoutJsonContext : JsonSerializerContext;
