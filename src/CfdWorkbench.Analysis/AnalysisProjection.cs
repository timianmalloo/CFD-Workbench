using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The one source of every rendered Analysis string (design §6.1). Pure: the selected run (or none), the current inputs
/// that decide Current or Historical, and the display units. PRJ owns the body.
/// </summary>
public static class AnalysisProjection
{
    public static AnalysisViewModel Build(AnalysisRun? run, CurrentInputs current, Units units) =>
        throw new NotImplementedException("PRJ: AnalysisProjection.Build");
}
