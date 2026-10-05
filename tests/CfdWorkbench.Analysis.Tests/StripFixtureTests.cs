using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

/// <summary>STP fixtures F-8 through F-17, the water table, and the product wing method (design §13, §18.8).</summary>
internal static class StripFixtureTests
{
    private const double DevelopedArea = 0.108;

    internal static void Run()
    {
        AnalysisChecks.Check("F8_Dihedral20_ClRatioToPlanar0p8938", F8);
        AnalysisChecks.Check("F9_Ana03Arithmetic_LiftDragAndRatio", F9);
        AnalysisChecks.Check("F10_Bookkeeping_NearFieldVsTrefftzWithinTolerance", F10);
        AnalysisChecks.Check("F11_GeometryScaleK_CoefficientsInvariant", F11);
        AnalysisChecks.Check("F12_SpeedScaleK_ForcesScaleK2", F12);
        AnalysisChecks.Check("F13a_FreshToSalt_ReFalls4p25Percent", F13a);
        AnalysisChecks.Check("F17_GoldenMaster_ExampleFoilVector", F17);
        AnalysisChecks.Check("Strip_ReLocal_UsesLocalChord", StripReynolds);
        AnalysisChecks.Check("Reference_SrefAndSpan_FromWingEstimates", Reference);
        AnalysisChecks.Check("Water_OutsideTable_Unavailable", WaterOutside);
        AnalysisChecks.Check("Provenance_WaterTableHash_Shown", Provenance);
        AnalysisChecks.Check("Loads_AttachmentMoment_TransferAboutNamedPoint", Attachment);
        AnalysisChecks.Check("SolveResidual_FailedRunHasNoDiagnostics", Residual);
    }

    private static void F8()
    {
        // The arithmetic note's wing uses the arc parameter. Product y is η·half-span and dihedral is z, so the
        // authored half-span is 0.45·cos 20° and the tip rise is 0.45·sin 20°. Both signs, twist zero, uniform chord,
        // 32×4. CL uses the shared developed area 0.108 m², so the ratio is the lift ratio.
        double radians = 20 * Math.PI / 180.0;
        RunSettings settings = Lattice(32, 4, "uniform");
        OperatingPoint op = OperatingPoints.Custom(1, 5, null);
        WaterRecord water = Fresh();
        double planar = Coefficient(Wing(450, 120, 120, 0), settings, op, water);
        double up = Coefficient(Wing(450 * Math.Cos(radians), 120, 120, 450 * Math.Sin(radians)), settings, op, water);
        double down = Coefficient(Wing(450 * Math.Cos(radians), 120, 120, -450 * Math.Sin(radians)), settings, op, water);
        Ratio(up / planar, "dihedral +20°");
        Ratio(down / planar, "dihedral −20°");
        if (planar < 0.1) throw new InvalidOperationException("planar CL " + planar.ToString("G6", CultureInfo.InvariantCulture));
    }

    private static void F9()
    {
        (double q, double lift, double drag, double ratio) = Loads.Dynamic(1000, 8, 0.14, 0.6, 0.035);
        Near(32000, q, "q");
        Near(2688, lift, "lift");
        Near(156.8, drag, "drag");
        Near(0.6 / 0.035, ratio, "CL/CD");
        StripValue total = Loads.TotalDrag(UnavailablePolar.Instance);
        if (total.Value is not null || total.UnavailableReason != Loads.TotalDragReason)
            throw new InvalidOperationException("total drag " + total.UnavailableReason);
    }

    private static void F10()
    {
        RunSettings settings = Lattice(32, 4, "cosine");
        OperatingPoint op = OperatingPoints.Custom(8, 5, null);
        WaterRecord water = Fresh();
        LatticeSolution solution = Solve(Wing(450, 120, 120, 0), settings, op, water);
        double gap = Loads.InducedDragGap(solution, op.AlphaDeg, water.Rho);
        if (!(gap < Settings.ReconciliationTolerance))
            throw new InvalidOperationException("near/Trefftz gap " + gap.ToString("G6", CultureInfo.InvariantCulture));
    }

    private static void F11()
    {
        const double k = 2;
        RunSettings settings = Lattice(4, 2);
        OperatingPoint op = OperatingPoints.Custom(8, 5, null);
        WaterRecord water = Fresh();
        WingLoads small = Coefficients(Wing(450, 120, 120, 0), settings, op, water);
        WingLoads large = Coefficients(Wing(450 * k, 120 * k, 120 * k, 0), settings, op, water);
        if (small.Cl < 0.1) throw new InvalidOperationException("CL " + small.Cl);
        Relative(small.Cl, large.Cl, "CL");
        Relative(small.Cdi, large.Cdi, "CDi");
        Relative(small.E, large.E, "e");
        Relative(small.Lift * k * k, large.Lift, "lift");
        Relative(small.Drag * k * k, large.Drag, "drag");
    }

    private static void F12()
    {
        const double k = 2;
        RunSettings settings = Lattice(4, 2);
        byte[] wing = Wing(450, 120, 120, 0);
        WaterRecord water = Fresh();
        WingLoads slow = Coefficients(wing, settings, OperatingPoints.Custom(4, 5, null), water);
        WingLoads fast = Coefficients(wing, settings, OperatingPoints.Custom(4 * k, 5, null), water);
        Relative(slow.Cl, fast.Cl, "CL");
        Relative(slow.Cdi, fast.Cdi, "CDi");
        Relative(slow.Lift * k * k, fast.Lift, "lift");
        Relative(slow.Drag * k * k, fast.Drag, "drag");
    }

    private static void F13a()
    {
        RunSettings settings = Lattice(4, 2);
        byte[] wing = Wing(450, 120, 120, 0);
        OperatingPoint op = OperatingPoints.Custom(8, 5, null);
        WaterRecord fresh = Fresh();
        WaterRecord salt = Salt();
        (LatticeSolution freshSolution, IReadOnlyList<StripLoad> freshLoads) = Solved(wing, settings, op, fresh);
        (LatticeSolution saltSolution, IReadOnlyList<StripLoad> saltLoads) = Solved(wing, settings, op, salt);
        double fall = 1 - saltLoads[0].ReLocal / freshLoads[0].ReLocal;
        if (fall < 0.0420 || fall > 0.0430)
            throw new InvalidOperationException("Re fall " + (100 * fall).ToString("G6", CultureInfo.InvariantCulture) + " %");
        double rhoRatio = salt.Rho / fresh.Rho;
        double liftRatio = Loads.TrefftzLift(saltSolution, salt.Rho, op.Speed) / Loads.TrefftzLift(freshSolution, fresh.Rho, op.Speed);
        Relative(rhoRatio, liftRatio, "lift/rho");
        if (Math.Abs(liftRatio - 1.0269) < 1e-5)
            throw new InvalidOperationException("loads scaled by the rounded 1.0269");
        Relative(freshLoads[0].ClLocal, saltLoads[0].ClLocal, "Cl local");
    }

    private static void F17()
    {
        string path = Path.Combine(RepoRoot(), "tests", "CfdWorkbench.Analysis.Tests", "Fixtures", "a3a", "f17-example.txt");
        byte[] source = File.ReadAllBytes(Path.Combine(RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil"));
        var method = new ProductWingMethod();
        OperatingPoint op = OperatingPoints.Custom(8, 5, null);
        WaterRecord water = Salt();
        LatticeSolution solution = Solve(source, method.Settings, op, water);
        string actual = Vector(solution, water.Rho, op.Speed);
        if (!File.Exists(path)) throw new InvalidOperationException("missing golden vector\n" + actual);
        string[] expected = File.ReadAllLines(path);
        string[] got = actual.Split('\n');
        if (expected.Length != got.Length) throw new InvalidOperationException("vector length " + got.Length + "\n" + actual);
        for (int i = 0; i < expected.Length; i++)
        {
            string[] left = expected[i].Split(' ');
            string[] right = got[i].Split(' ');
            if (left[0] != right[0]) throw new InvalidOperationException(expected[i] + " vs " + got[i]);
            if (left.Length == 2) Relative(double.Parse(left[1], CultureInfo.InvariantCulture), double.Parse(right[1], CultureInfo.InvariantCulture), left[0]);
        }
    }

    private static void StripReynolds()
    {
        SectionSample root = Sample(0, 0.12);
        SectionSample tip = Sample(1, 0.06);
        var solution = new LatticeSolution(new[] { 0.1, 0.05 }, new[] { -0.01, -0.02 }, new RunDiagnostics(0, 1))
        {
            Strips = new[]
            {
                new LatticeStrip(0, 0, 0, 0.12, 0.05, 0.1, -0.01, 0.4, -2, 0, 0.3, 0, 0.05),
                new LatticeStrip(1, 0.45, 1, 0.06, 0.05, 0.05, -0.02, 0.8, -2, 0, 0.2, 0.4, 0.45)
            },
            Forces = new[]
            {
                new StripForce(0, 0, 0, 0.12, 0.1, 0, 0, 1, 0, 0, 0),
                new StripForce(1, 0.45, 1, 0.06, 0.05, 0, 0, 1, 0, 0, 0)
            }
        };
        OperatingPoint op = OperatingPoints.Custom(10, 5, null);
        IReadOnlyList<StripLoad> loads = StripCoupler.Couple(new[] { root, tip }, solution, op, Fresh(), CancellationToken.None);
        Relative(loads[0].ReLocal, 2 * loads[1].ReLocal, "tip Re");
        Near(5 + -2 - 0.4, loads[0].AlphaEff, "alpha eff");
        if (loads[0].CdNcrit2.Value is not null || loads[0].CdNcrit4.Value is not null)
            throw new InvalidOperationException("cd was a number");
        if (loads[1].CdNcrit2.UnavailableReason != UnavailablePolar.Instance.UnavailableReason)
            throw new InvalidOperationException(loads[1].CdNcrit2.UnavailableReason);
    }

    private static void Reference()
    {
        double radians = 20 * Math.PI / 180.0;
        byte[] foil = Wing(450, 120, 120, 450 * Math.Tan(radians));
        WingEstimates estimates = WingEstimates.From(foil, "accepted", 0);
        RunReference reference = ReferenceQuantities.From(foil);
        Near(estimates.AreaSquareMeters, reference.SRef, "Sref");
        Near(estimates.SpanMeters, reference.BRef, "span");
        Near(estimates.MeanChordMeters, reference.CRef, "chord");
        double foreshortened = estimates.AreaSquareMeters * Math.Cos(radians);
        if (Math.Abs(reference.SRef - foreshortened) < 1e-4)
            throw new InvalidOperationException("Sref collapsed to the foreshortened planform");
    }

    private static void WaterOutside()
    {
        Refused(-0.1, 0);
        Refused(50.1, OperatingPoints.SaltSalinityGPerKg);
        WaterRecord cold = WaterTable.At(0, 0);
        WaterRecord hot = WaterTable.At(50, OperatingPoints.SaltSalinityGPerKg);
        if (!double.IsFinite(cold.Rho) || !double.IsFinite(hot.Rho) || cold.Rho == WaterTable.At(1, 0).Rho)
            throw new InvalidOperationException("a bound clamped or was not finite");
        WaterRecord mid = WaterTable.At(15.5, 0);
        Near(0.5 * (WaterTable.At(15, 0).Rho + WaterTable.At(16, 0).Rho), mid.Rho, "midpoint rho");
        using AuthoringSession session = Fixture.Opened();
        int before = session.ReadRuns().Runs.Count;
        var refused = new WaterRecord(-0.1, 0, 999, 1e-6, 1000, WaterTable.Source, new string('a', 64));
        try
        {
            Fixture.Evaluate(new AnalysisService(session, new ProductWingMethod()), OperatingPoints.Custom(5, 3, null), refused);
            throw new InvalidOperationException("Evaluate accepted water outside the table");
        }
        catch (ContractError error)
        {
            if (error.Code != "ANA-INPUT-WATER") throw new InvalidOperationException(error.Code);
        }
        if (session.ReadRuns().Runs.Count != before) throw new InvalidOperationException("a refused water query was recorded");
    }

    private static void Provenance()
    {
        WaterRecord water = WaterTable.At(15, OperatingPoints.SaltSalinityGPerKg);
        if (water.TableHash != WaterTable.PinnedHash || water.TableHash != Identity.Blake3(WaterTable.TableBytes))
            throw new InvalidOperationException("hash " + water.TableHash);
        if (water.Source != WaterTable.Source) throw new InvalidOperationException(water.Source);
        Near(1026.0210, water.Rho, "salt rho");
        Near(1.1892e-6, water.Nu, "salt nu");
        Near(1670.9, water.Pv, "salt pv");
        WaterRecord fresh = WaterTable.At(15, 0);
        Near(999.1026, fresh.Rho, "fresh rho");
        Near(1.1386e-6, fresh.Nu, "fresh nu");
        Near(1705.8, fresh.Pv, "fresh pv");
        byte[] flipped = (byte[])WaterTable.TableBytes.Clone();
        flipped[flipped.Length / 2] ^= 0x01;
        try
        {
            WaterTable.At(15, 0, flipped);
            throw new InvalidOperationException("a flipped table loaded");
        }
        catch (ContractError error)
        {
            if (error.Message != WaterTable.FailedCheckReason) throw new InvalidOperationException(error.Message);
        }
    }

    private static void Attachment()
    {
        Loads.Vec moment = Loads.About(new Loads.Vec(0, 0, 100), new Loads.Vec(0, 0, 0), new Loads.Vec(0.1, 0, 0));
        Near(0, moment.X, "Mx");
        Near(10, moment.Y, "My");
        Near(0, moment.Z, "Mz");
    }

    private static void Residual()
    {
        using var session = new AuthoringSession();
        session.Open(File.ReadAllBytes(Path.Combine(RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil")), Guid.NewGuid().ToString("D"), true);
        // 8 per half is the smallest product lattice whose influence matrix row-swaps, so the whole-row plant
        // misses the residual tolerance. A smaller lattice does not swap and the plant is a no-op.
        var method = new ProductWingMethod(Lattice(8, 2), LatticePlant.PivotWholeRow);
        AnalysisRun run = Fixture.Evaluate(new AnalysisService(session, method), OperatingPoints.Custom(5, 3, null), Fresh());
        if (run.Outcome is not RunOutcome.Failed failed)
            throw new InvalidOperationException("outcome " + run.Outcome);
        if (failed.Code != "ANA-SOLVE-RESIDUAL" || string.IsNullOrWhiteSpace(failed.Reason))
            throw new InvalidOperationException(failed.Code + " " + failed.Reason);
        if (run.Diagnostics is not null || run.Strips.Count != 0)
            throw new InvalidOperationException("a failed run kept diagnostics or strips");
    }

    private readonly record struct WingLoads(double Cl, double Cdi, double E, double Lift, double Drag);

    private static double Coefficient(byte[] source, RunSettings settings, OperatingPoint op, WaterRecord water)
    {
        double lift = Loads.TrefftzLift(Solve(source, settings, op, water), water.Rho, op.Speed);
        return lift / (0.5 * water.Rho * op.Speed * op.Speed * DevelopedArea);
    }

    private static WingLoads Coefficients(byte[] source, RunSettings settings, OperatingPoint op, WaterRecord water)
    {
        (LatticeSolution solution, _) = Solved(source, settings, op, water);
        RunReference reference = ReferenceQuantities.From(source);
        double lift = Loads.TrefftzLift(solution, water.Rho, op.Speed);
        double drag = Loads.TrefftzDrag(solution, water.Rho);
        double q = 0.5 * water.Rho * op.Speed * op.Speed;
        double cl = lift / (q * reference.SRef);
        double cdi = drag / (q * reference.SRef);
        double aspect = reference.BRef * reference.BRef / reference.SRef;
        return new WingLoads(cl, cdi, Trefftz.Oswald(cl, aspect, cdi), lift, drag);
    }

    private static (LatticeSolution Solution, IReadOnlyList<StripLoad> Loads) Solved(byte[] source, RunSettings settings, OperatingPoint op, WaterRecord water)
    {
        var method = new ProductWingMethod(settings);
        IReadOnlyList<SectionSample> sections = Placement.Sections(source, settings.SectionEtas!, settings.SectionXs!, CancellationToken.None);
        LatticeSolution solution = method.Solve(sections, op, water, CancellationToken.None);
        return (solution, method.Couple(sections, solution, op, water, CancellationToken.None));
    }

    private static LatticeSolution Solve(byte[] source, RunSettings settings, OperatingPoint op, WaterRecord water) =>
        Solved(source, settings, op, water).Solution;

    private static string Vector(LatticeSolution solution, double rho, double speed)
    {
        var lines = new List<string>
        {
            "lift " + Loads.TrefftzLift(solution, rho, speed).ToString("G17", CultureInfo.InvariantCulture),
            "drag " + Loads.TrefftzDrag(solution, rho).ToString("G17", CultureInfo.InvariantCulture),
            "residual " + solution.Diagnostics.ResidualInf.ToString("G17", CultureInfo.InvariantCulture),
            "kappa1 " + solution.Diagnostics.Kappa1.ToString("G17", CultureInfo.InvariantCulture)
        };
        foreach (LatticeStrip strip in solution.Strips)
            lines.Add("gamma " + strip.Gamma.ToString("G17", CultureInfo.InvariantCulture));
        return string.Join('\n', lines);
    }

    private static RunSettings Lattice(int spanPerHalf, int chord, string chordSpacing = "cosine") =>
        Settings.WithStations(Settings.Default with
        {
            NSpanPerHalf = spanPerHalf,
            NChord = chord,
            ChordSpacing = chordSpacing,
            SectionEtas = null,
            SectionXs = null
        });

    private static byte[] Wing(double halfSpanMm, double rootChordMm, double tipChordMm, double tipElevationMm)
    {
        string text = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil"));
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("half_span ", StringComparison.Ordinal))
            {
                int content = lines[i].IndexOf("half_span ", StringComparison.Ordinal);
                lines[i] = lines[i][..content] + "half_span " + halfSpanMm.ToString("G17", CultureInfo.InvariantCulture) + " mm";
            }
            else if (trimmed.StartsWith("dihedral cv ", StringComparison.Ordinal))
                lines[i] = Curve(lines[i], eta => eta * tipElevationMm);
            else if (trimmed.StartsWith("twist cv ", StringComparison.Ordinal))
                lines[i] = Curve(lines[i], _ => 0);
            else if (trimmed.StartsWith("trailing cv ", StringComparison.Ordinal))
                lines[i] = Curve(lines[i], eta => rootChordMm + (tipChordMm - rootChordMm) * eta);
        }
        return Encoding.UTF8.GetBytes(string.Join('\n', lines));
    }

    private static string Curve(string line, Func<double, double> ordinate)
    {
        int start = line.IndexOf("points [", StringComparison.Ordinal);
        int end = line.IndexOf(']', start);
        if (start < 0 || end < 0) throw new InvalidOperationException("curve has no points");
        var points = new List<string>();
        foreach (Match match in Regex.Matches(line[start..end], @"\(([+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*,"))
        {
            double eta = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            points.Add("(" + match.Groups[1].Value + ", " + ordinate(eta).ToString("G17", CultureInfo.InvariantCulture) + ")");
        }
        return line[..(start + "points [".Length)] + string.Join(", ", points) + line[end..];
    }

    private static SectionSample Sample(double eta, double chord) => new(
        new StationFrame(eta, eta * 0.45, 0, chord, 0, 0, 0.12),
        new[] { 0d, 1d }, new[] { 0d, 0d }, new[] { 0d, 0d }, new[] { 0d, 0d },
        new[] { new Point3(0, eta * 0.45, 0), new Point3(chord, eta * 0.45, 0) });

    private static WaterRecord Fresh() => WaterTable.At(15, 0);

    private static WaterRecord Salt() => WaterTable.At(15, OperatingPoints.SaltSalinityGPerKg);

    private static void Refused(double temperature, double salinity)
    {
        try
        {
            WaterTable.At(temperature, salinity);
            throw new InvalidOperationException(temperature.ToString(CultureInfo.InvariantCulture) + " was inside the table");
        }
        catch (ContractError error)
        {
            if (error.Message != WaterTable.OutsideReason) throw new InvalidOperationException(error.Message);
        }
    }

    private static void Ratio(double ratio, string name)
    {
        if (Math.Abs(ratio - 0.8938) / 0.8938 > 0.01)
            throw new InvalidOperationException(name + " CL ratio " + ratio.ToString("G8", CultureInfo.InvariantCulture));
    }

    private static void Near(double expected, double actual, string name)
    {
        if (Math.Abs(actual - expected) > 1e-9)
            throw new InvalidOperationException(name + " " + actual.ToString("G17", CultureInfo.InvariantCulture) + " vs " + expected.ToString("G17", CultureInfo.InvariantCulture));
    }

    private static void Relative(double expected, double actual, string name)
    {
        double scale = Math.Max(Math.Abs(expected), 1e-30);
        if (Math.Abs(actual - expected) / scale > 1e-12)
            throw new InvalidOperationException(name + " " + actual.ToString("G17", CultureInfo.InvariantCulture) + " vs " + expected.ToString("G17", CultureInfo.InvariantCulture));
    }

    internal static string RepoRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
