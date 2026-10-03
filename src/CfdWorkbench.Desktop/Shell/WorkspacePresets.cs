using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

/// <summary>
/// The workspace presets the shell applies (⌘1 Planform, ⌘2 Precision, ⌘3 Review). They are the codec's presets, so the
/// desktop and the layout file can never disagree on a pane's home (§11.8; one table, <see cref="LayoutCodec.Homes"/>).
/// </summary>
public static class WorkspacePresets
{
    /// <summary>The panes the shell registers: each pane of <see cref="LayoutCodec.Homes"/>, in home order.</summary>
    public static readonly IReadOnlyList<string> RegisteredPanes = LayoutCodec.Homes.Select(home => home.Pane).ToArray();

    public static LayoutDocument Preset(WorkspaceId workspace, IEnumerable<string> panes) =>
        LayoutCodec.Presets(workspace, panes.ToHashSet(StringComparer.Ordinal));

    public static LayoutDocument Preset(WorkspaceId workspace) =>
        Preset(workspace, RegisteredPanes);

    /// <summary>The region that shows a pane in one workspace's preset, or null when the preset hides it.</summary>
    public static RegionId? ShownIn(WorkspaceId workspace, string pane)
    {
        var layout = Preset(workspace).Workspaces.Single(item => item.Id == workspace);
        return layout.Regions.FirstOrDefault(region => region.Open && region.Groups.Any(group => group.Panes.Contains(pane)))?.Id;
    }

    public static double ProportionFor(double px, double hostExtent)
    {
        if (!double.IsFinite(px) || !double.IsFinite(hostExtent) || hostExtent <= 0) return 0.0;
        double prop = px / hostExtent;
        if (prop < 0.0) return 0.0;
        if (prop > 1.0) return 1.0;
        return prop;
    }
}
