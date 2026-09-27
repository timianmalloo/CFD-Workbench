using CfdWorkbench.Persistence;

namespace CfdWorkbench.Desktop.Shell;

public static class WorkspacePresets
{
    public static readonly IReadOnlyList<string> RegisteredPanes = ["properties", "browser", "points", "messages"];

    public static LayoutDocument Preset(WorkspaceId workspace, IEnumerable<string> panes)
    {
        throw new NotImplementedException("track D1 stub");
    }

    public static LayoutDocument Preset(WorkspaceId workspace) =>
        Preset(workspace, RegisteredPanes);

    public static double ProportionFor(double px, double hostExtent)
    {
        throw new NotImplementedException("track D1 stub");
    }
}
