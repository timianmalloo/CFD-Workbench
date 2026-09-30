namespace CfdWorkbench.Core.Tests;

// Track B0 owns this class and adds its named checks (docs/design/m12b-points.md §9, §12.4, §14).
internal static class PointModelTests
{
    internal static void Run() { }

    // Readiness-tier check, excluded from run-tests.sh (PRE's --readiness switch spawns it; §12.3).
    // `Readiness_SixteenPointThreeAnchors_AssessUnderProofBudget` is written here by B0.
    internal static void RunReadiness() { }
}
