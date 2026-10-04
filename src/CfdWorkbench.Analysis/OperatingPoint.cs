using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>Why a derived condition has no number: Undefined (speed ≤ 0, ANA-03) or Unavailable (depth not set, COPY-45).</summary>
public enum DerivedReason
{
    SpeedNotPositive,
    DepthNotSet
}

/// <summary>A derived condition: a value, or the reason it has none — never a zero standing in for a missing number.</summary>
public sealed record DerivedValue(double? Value, DerivedReason? Reason)
{
    internal static DerivedValue Of(double value) => new(value, null);
    internal static DerivedValue Without(DerivedReason reason) => new(null, reason);
}

/// <summary>The conditions derived on read from an operating point, its water and c_ref (design §3.5). SI units.</summary>
public sealed record DerivedConditions(DerivedValue Q, DerivedValue ReRef, DerivedValue DepthOverChord, DerivedValue FroudeDepth,
    DerivedValue Sigma);

/// <summary>
/// Validation and the derived quantities of an operating point (q, Re_ref, h/c, Fr_h, σ) — derived on read, never stored
/// (design §3.5). The stored record is Core's <see cref="OperatingPoint"/>. One place builds a Custom operating point, so
/// the conditions band and the CLI cannot default a field differently (CLI-01).
/// </summary>
public static class OperatingPoints
{
    /// <summary>Standard gravity, m/s².</summary>
    public const double Gravity = 9.80665;

    /// <summary>The atmospheric pressure a Custom operating point starts with, Pa.</summary>
    public const double StandardAtmosphere = 101325;

    /// <summary>The datum h_ref is measured from (design §5.6).</summary>
    public const string RootLeadingEdge = "root LE";

    /// <summary>The water the band starts with: salt at 15 °C (mockup screen 1).</summary>
    public const double DefaultTemperatureC = 15;

    /// <summary>Standard seawater absolute salinity, g/kg (the design manifest's salt row, §5.6).</summary>
    public const double SaltSalinityGPerKg = 35.16504;

    /// <summary>A Custom operating point (DR-ANA-9): speed in m/s, α in degrees, h_ref in metres or null for "depth not set".</summary>
    public static OperatingPoint Custom(double speed, double alphaDeg, double? hRef) =>
        new(speed, StandardAtmosphere, hRef, RootLeadingEdge, alphaDeg, null);

    /// <summary>
    /// Refuses an operating point the method cannot evaluate, before compute and with nothing recorded (design §8):
    /// <c>ANA-INPUT-SPEED</c> (speed ≤ 0 or not finite), <c>ANA-INPUT-ALPHA</c>, <c>ANA-INPUT-DEPTH</c> (h_ref set and not
    /// above zero), <c>ANA-INPUT-PRESSURE</c>, <c>ANA-INPUT-LOAD</c> (A3a stores no load, DR-ANA-9).
    /// </summary>
    public static void Validate(OperatingPoint op)
    {
        ArgumentNullException.ThrowIfNull(op);
        Require(double.IsFinite(op.Speed) && op.Speed > 0, "ANA-INPUT-SPEED", "speed ≤ 0 or not finite");
        Require(double.IsFinite(op.AlphaDeg), "ANA-INPUT-ALPHA", "α is not finite");
        Require(op.HRef is null || (double.IsFinite(op.HRef.Value) && op.HRef.Value > 0), "ANA-INPUT-DEPTH", "h_ref is not above zero");
        Require(double.IsFinite(op.PAtm) && op.PAtm > 0, "ANA-INPUT-PRESSURE", "atmospheric pressure is not above zero");
        Require(op.Load is null, "ANA-INPUT-LOAD", "a design load is not an A3a input");
    }

    /// <summary>
    /// q = ½ρV², Re_ref = V·c_ref/ν, h/c = h_ref/c_ref, Fr_h = V/√(g·h_ref), σ = (p_atm + ρ·g·h_ref − p_v)/q. Speed ≤ 0 makes
    /// every speed-borne value Undefined (the signed speed, never |V|); depth not set makes h/c, Fr_h and σ Unavailable.
    /// </summary>
    public static DerivedConditions Derive(OperatingPoint op, WaterRecord water, double cRef)
    {
        ArgumentNullException.ThrowIfNull(op);
        ArgumentNullException.ThrowIfNull(water);
        bool moving = op.Speed > 0;
        var speedless = DerivedValue.Without(DerivedReason.SpeedNotPositive);
        var depthless = DerivedValue.Without(DerivedReason.DepthNotSet);
        double q = 0.5 * water.Rho * op.Speed * op.Speed;
        return new DerivedConditions(
            moving ? DerivedValue.Of(q) : speedless,
            moving ? DerivedValue.Of(op.Speed * cRef / water.Nu) : speedless,
            op.HRef is double h ? DerivedValue.Of(h / cRef) : depthless,
            !moving ? speedless : op.HRef is double hf ? DerivedValue.Of(op.Speed / Math.Sqrt(Gravity * hf)) : depthless,
            !moving ? speedless : op.HRef is double hs ? DerivedValue.Of((op.PAtm + water.Rho * Gravity * hs - water.Pv) / q) : depthless);
    }

    /// <summary>The table's tabulated temperature band, °C (spec 1.7 water-record row: "admitted range 0–50 °C").</summary>
    public const double TableMinTemperatureC = 0, TableMaxTemperatureC = 50;

    /// <summary>
    /// Refuses a water record outside the table before compute, nothing recorded (design §8): <c>ANA-INPUT-WATER</c> when
    /// a property is not finite, ρ or ν is not above zero, p_v is negative, or the temperature or salinity lies outside
    /// the table. The table itself refuses outside its band (<see cref="WaterTable.At"/>, never clamped); this guards a
    /// record the table did not build.
    /// </summary>
    public static void Validate(WaterRecord water)
    {
        ArgumentNullException.ThrowIfNull(water);
        bool finite = new[] { water.TemperatureC, water.SalinityGPerKg, water.Rho, water.Nu, water.Pv }.All(double.IsFinite);
        Require(finite && water.Rho > 0 && water.Nu > 0 && water.Pv >= 0, "ANA-INPUT-WATER", "a water property is not a table value");
        Require(water.TemperatureC is >= TableMinTemperatureC and <= TableMaxTemperatureC, "ANA-INPUT-WATER",
            "the temperature is outside the table (0–50 °C)");
        // assume: the table spans salinity from fresh (0) to standard seawater (35.16504 g/kg) and admits values between.
        // Confirmed when STP's WaterTable lands (G-T4); if it holds the two columns only, this narrows to those two values.
        Require(water.SalinityGPerKg is >= 0 and <= SaltSalinityGPerKg, "ANA-INPUT-WATER", "the salinity is outside the table");
    }

    private static void Require(bool condition, string code, string reason)
    {
        if (!condition) throw new ContractError(code, reason);
    }
}
