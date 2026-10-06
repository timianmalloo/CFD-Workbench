using System.Globalization;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>One section of a wing run. α_eff is the stored strip value, never the geometric operating α.</summary>
public sealed record CavitationStation(SectionSample Section, double AlphaEffDeg);

public enum CavitationState { Clear, InsideMargin, PossibleAboveCritical, Undefined, Unavailable }

/// <summary>A derived screen, not an inception prediction. Missing or nonphysical inputs carry no plausible number.</summary>
public sealed record CavitationResult(CavitationState State, string ScreenText, string MarginLabel,
    double MarginFraction, double? Sigma, double? CriticalSpeed, double? CpMin, int StationCount,
    string? GoverningStation, double? GoverningDepth, string? Reason);

/// <summary>The A5.4 sheet screen and its local-depth, α_eff wing reduction (design §5.1).</summary>
public static class Cavitation
{
    public const double DefaultMarginFraction = 0.15;
    public const string PvMissing = "ANA-CAV-PV-MISSING";
    public const string DepthNotSet = "ANA-CAV-DEPTH-NOT-SET";
    public const string WaterInvalid = "ANA-CAV-WATER-INVALID";
    public const string DepthInvalid = "ANA-CAV-DEPTH-INVALID";
    public const string SurfacePiercing = "ANA-CAV-SURFACE-PIERCING";
    public const string NoSuction = "ANA-CAV-NO-SUCTION";
    public const string PressureNonpositive = "ANA-CAV-PRESSURE-NONPOSITIVE";
    private const string ScreenPrefix = "Cavitation screening (sheet, by −Cp_min): inception is possible above V_crit; " +
        "not a prediction of inception, extent, tip-vortex or cloud cavitation; Cp_min resolution: ";
    private const string MarginProvenance = "practitioner assumption, not sourced";

    public static CavitationResult Screen(double cpMin, int stationCount, double? depth, double speed, double rho,
        double pAtm, double? pv, string station, double marginFraction = DefaultMarginFraction)
    {
        if (!double.IsFinite(cpMin) || stationCount < 1 || !double.IsFinite(speed) || speed <= 0 ||
            !double.IsFinite(rho) || rho <= 0 || !double.IsFinite(pAtm) || pAtm <= 0 ||
            !double.IsFinite(marginFraction) || marginFraction < 0)
            throw new ContractError("ANA-CAV-INPUT", "The cavitation screen received a nonphysical input.");
        string copy = ScreenPrefix + stationCount.ToString(CultureInfo.InvariantCulture) + " stations";
        string margin = (100 * marginFraction).ToString("0.#", CultureInfo.InvariantCulture) +
            "% margin — " + MarginProvenance;
        if (pv is null || depth is null)
            return new(CavitationState.Unavailable, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, pv is null ? PvMissing : DepthNotSet);
        if (!double.IsFinite(pv.Value) || pv < 0)
            return new(CavitationState.Unavailable, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, WaterInvalid);
        if (!double.IsFinite(depth.Value))
            return new(CavitationState.Unavailable, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, DepthInvalid);
        if (depth <= 0)
            return new(CavitationState.Unavailable, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, SurfacePiercing);
        double suction = -cpMin;
        if (suction <= 0)
            return new(CavitationState.Undefined, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, NoSuction);
        double numerator = pAtm + rho * OperatingPoints.Gravity * depth.Value - pv.Value;
        if (!(numerator > 0))
            return new(CavitationState.Undefined, copy, margin, marginFraction, null, null, cpMin, stationCount,
                station, depth, PressureNonpositive);
        double sigma = numerator / (0.5 * rho * speed * speed);
        double criticalSpeed = Math.Sqrt(2 * numerator / (rho * suction));
        CavitationState state = sigma <= suction ? CavitationState.PossibleAboveCritical :
            sigma <= suction * (1 + marginFraction) ? CavitationState.InsideMargin : CavitationState.Clear;
        return new(state, copy, margin, marginFraction, sigma, criticalSpeed, cpMin, stationCount,
            station, depth, null);
    }

    public static CavitationResult ScreenWing(IReadOnlyList<CavitationStation> stations, OperatingPoint op,
        double rho, double? pv, StationFrame datum, double marginFraction = DefaultMarginFraction,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(stations);
        ArgumentNullException.ThrowIfNull(op);
        ArgumentNullException.ThrowIfNull(datum);
        if (stations.Count == 0) throw new ContractError("ANA-CAV-INPUT", "A wing screen needs a station.");
        var screens = new List<CavitationResult>(stations.Count);
        double alpha = VortexLattice.ToRadians(op.AlphaDeg);
        foreach (CavitationStation candidate in stations)
        {
            cancellation.ThrowIfCancellationRequested();
            PanelResult panel = PanelMethod.Solve(candidate.Section, candidate.AlphaEffDeg, cancellation);
            double? depth = null;
            if (op.HRef is double h)
            {
                // The body frame is +x aft, +y starboard, +z up (docs/specs/cfd-workbench.md:167).
                StationFrame frame = candidate.Section.Frame;
                double rise = (frame.ElevationMeters - datum.ElevationMeters) * Math.Cos(alpha) -
                    (frame.LeadingMeters - datum.LeadingMeters) * Math.Sin(alpha);
                depth = h - rise;
            }
            string name = "η " + candidate.Section.Frame.Eta.ToString("0.###", CultureInfo.InvariantCulture);
            CavitationResult screen = Screen(panel.CpMin, panel.StationCount, depth, op.Speed, rho, op.PAtm, pv,
                name, marginFraction);
            screens.Add(screen);
        }
        return SelectWing(screens);
    }

    /// <summary>Ruling 86 reduction: the smallest local sigma/(-Cp_min) governs; a missing input stays Unavailable.</summary>
    public static CavitationResult SelectWing(IReadOnlyList<CavitationResult> screens)
    {
        if (screens.Count == 0) throw new ContractError("ANA-CAV-INPUT", "A wing screen needs a station.");
        CavitationResult? unavailable = screens.FirstOrDefault(screen => screen.State == CavitationState.Unavailable);
        if (unavailable is not null) return unavailable;
        CavitationResult? governing = screens.Where(screen => screen.Sigma.HasValue && screen.CpMin is < 0)
            .MinBy(screen => screen.Sigma!.Value / -screen.CpMin!.Value);
        return governing ?? screens.First(screen => screen.State == CavitationState.Undefined);
    }
}
