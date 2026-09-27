using System.Text.Json.Serialization;

namespace CfdWorkbench.Persistence;

// The `cfdw-layout` version 1 file (docs/design/app-shell.md §3.4). Frozen: any added member or value is version 2 (§3.5).
// Member names are the camelCase of these properties; enum values are the closed lowercase strings named below, and the
// codec's string-enum converter refuses integer values. Ranges and counts are enforced by the codec (V1–V10), not here.

/// <summary>One installation's layouts. <see cref="Format"/> and <see cref="Version"/> are written by the codec, never by a snapshot.</summary>
public sealed record LayoutDocument(WorkspaceId Active, IReadOnlyList<WorkspaceLayout> Workspaces)
{
    public const string FormatName = "cfdw-layout";
    public const int CurrentVersion = 1;
    [JsonPropertyOrder(-2)] public string Format => FormatName;
    [JsonPropertyOrder(-1)] public int Version => CurrentVersion;
}

/// <summary>One workspace's current layout: its views, its three regions, its floats and its closed panes.</summary>
public sealed record WorkspaceLayout(
    WorkspaceId Id, WorkspaceViews Views, IReadOnlyList<RegionLayout> Regions, IReadOnlyList<FloatLayout> Floats,
    IReadOnlyList<string> Closed);

public sealed record WorkspaceViews(ViewArrangement Arrangement, SingleView Single);

/// <summary>One dock region. <see cref="Size"/> is in DIP.</summary>
public sealed record RegionLayout(RegionId Id, bool Open, double Size, IReadOnlyList<PaneGroup> Groups);

/// <summary>One tab group: pane ids, the active pane, and its share of the region in (0, 1].</summary>
public sealed record PaneGroup(IReadOnlyList<string> Panes, string Active, double Share);

/// <summary>One float window. <see cref="X"/> and <see cref="Y"/> are <c>Window.Position</c> units; width and height are DIP.</summary>
public sealed record FloatLayout(
    IReadOnlyList<string> Panes, string Active, int X, int Y, double Width, double Height, ScreenBounds Screen,
    FloatOrigin Origin);

/// <summary>The bounds of the screen a float was on, in <c>Screen.Bounds</c> units. A matching hint only; no display name.</summary>
public sealed record ScreenBounds(int X, int Y, int Width, int Height);

/// <summary>Where a float docks back to: region, group index and tab index.</summary>
public sealed record FloatOrigin(RegionId Region, int Group, int Index);

public enum WorkspaceId
{
    [JsonStringEnumMemberName("planform")] Planform,
    [JsonStringEnumMemberName("precision")] Precision,
    [JsonStringEnumMemberName("review")] Review
}

public enum RegionId
{
    [JsonStringEnumMemberName("left")] Left,
    [JsonStringEnumMemberName("right")] Right,
    [JsonStringEnumMemberName("bottom")] Bottom
}

public enum ViewArrangement
{
    [JsonStringEnumMemberName("plan-3d")] Plan3d,
    [JsonStringEnumMemberName("four")] Four,
    [JsonStringEnumMemberName("one")] One
}

public enum SingleView
{
    [JsonStringEnumMemberName("plan")] Plan,
    [JsonStringEnumMemberName("3d")] ThreeD,
    [JsonStringEnumMemberName("side")] Side,
    [JsonStringEnumMemberName("front")] Front
}
