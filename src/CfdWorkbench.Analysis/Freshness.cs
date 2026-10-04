using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>The inputs a run would have if it were evaluated now; their key decides Current or Historical (design §3.4).</summary>
public sealed record CurrentInputs(RunInputs Inputs, WaterRecord Water, OperatingPoint Op, RunMethod Method, string SettingsHash);

/// <summary>Freshness is run-key equality; nothing writes a freshness flag. SVC owns the bodies.</summary>
public static class Freshness
{
    /// <summary>The key of the current inputs, through <see cref="RunRecord.Key"/> (the one definition).</summary>
    public static string CurrentKey(CurrentInputs current) => throw new NotImplementedException("SVC: Freshness.CurrentKey");
}
