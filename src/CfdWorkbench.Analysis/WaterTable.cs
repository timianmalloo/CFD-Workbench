using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>
/// The bundled ITTC 7.5-02-01-03 water table (an embedded resource, hashed at load; never a file read at run time).
/// STP owns the body and the table (G-T4).
/// </summary>
public static class WaterTable
{
    /// <summary>The water record at a temperature and salinity inside the table; outside it, Unavailable (never clamped).</summary>
    public static WaterRecord At(double temperatureC, double salinityGPerKg) => throw new NotImplementedException("STP: WaterTable.At");
}
