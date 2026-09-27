using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

public static class WorkspacePresets
{
    public static readonly IReadOnlyList<string> RegisteredPanes = ["properties", "browser", "points", "messages"];

    public static LayoutDocument Preset(WorkspaceId workspace, IEnumerable<string> panes)
    {
        var paneSet = panes.ToHashSet();
        var allWorkspaces = new List<WorkspaceLayout>
        {
            BuildWorkspace(WorkspaceId.Planform, paneSet),
            BuildWorkspace(WorkspaceId.Precision, paneSet),
            BuildWorkspace(WorkspaceId.Review, paneSet)
        };

        return new LayoutDocument(workspace, allWorkspaces);
    }

    public static LayoutDocument Preset(WorkspaceId workspace) =>
        Preset(workspace, RegisteredPanes);

    public static double ProportionFor(double px, double hostExtent)
    {
        if (!double.IsFinite(px) || !double.IsFinite(hostExtent) || hostExtent <= 0) return 0.0;
        double prop = px / hostExtent;
        if (prop < 0.0) return 0.0;
        if (prop > 1.0) return 1.0;
        return prop;
    }

    private static WorkspaceLayout BuildWorkspace(WorkspaceId id, HashSet<string> paneSet)
    {
        var views = id switch
        {
            WorkspaceId.Review => new WorkspaceViews(ViewArrangement.Four, SingleView.Plan),
            _ => new WorkspaceViews(ViewArrangement.Plan3d, SingleView.Plan)
        };

        var leftCandidate = new[] { "properties", "browser" };
        var bottomCandidate = new[] { "points", "messages" };

        var leftPanes = leftCandidate.Where(paneSet.Contains).ToList();
        var bottomPanes = bottomCandidate.Where(paneSet.Contains).ToList();

        var leftGroups = new List<PaneGroup>();
        if (leftPanes.Count > 0)
        {
            string active = leftPanes.Contains("properties") ? "properties" : leftPanes[0];
            leftGroups.Add(new PaneGroup(leftPanes, active, 1.0));
        }

        var bottomGroups = new List<PaneGroup>();
        if (bottomPanes.Count > 0)
        {
            string active = bottomPanes.Contains("points") ? "points" : bottomPanes[0];
            bottomGroups.Add(new PaneGroup(bottomPanes, active, 1.0));
        }

        bool leftOpen = id is WorkspaceId.Planform or WorkspaceId.Precision;
        bool bottomOpen = id is WorkspaceId.Precision;

        var regions = new List<RegionLayout>
        {
            new(RegionId.Left, leftOpen, 260.0, leftGroups),
            new(RegionId.Bottom, bottomOpen, 190.0, bottomGroups),
            new(RegionId.Right, false, 260.0, [])
        };

        var closed = paneSet.Where(p => !leftPanes.Contains(p) && !bottomPanes.Contains(p)).ToList();

        return new WorkspaceLayout(id, views, regions, [], closed);
    }
}
