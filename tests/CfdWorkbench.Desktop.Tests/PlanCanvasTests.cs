namespace CfdWorkbench.Desktop.Tests;

// Track U1b owns this class and adds its named checks with DesktopChecks.Check (docs/design/m12b-points.md §9, §12.4, §14).
public static class PlanCanvasTests
{
    public static void Run() { }

    // Readiness-tier check, excluded from run-tests.sh (PRE's --readiness switch spawns it; §12.3).
    // `Readiness_PlanRender_Under8Ms` is written here by U1b.
    public static void RunReadiness() { }
}
