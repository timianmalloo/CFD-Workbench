using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis;

/// <summary>One closed section node in chord-normalised coordinates.</summary>
public readonly record struct SectionPoint(double X, double Z);

/// <summary>Pressure at a panel midpoint; x/c and z/c are dimensionless.</summary>
public readonly record struct PanelCp(double X, double Z, double Cp);

/// <summary>An inviscid section solve. Upper runs TE→LE, lower LE→TE; no boundary layer is modelled.</summary>
public sealed record PanelResult(IReadOnlyList<PanelCp> Upper, IReadOnlyList<PanelCp> Lower,
    double CpMin, int StationCount, double Cl, double CmQuarter);

/// <summary>
/// Continuous linear-strength vortex sheets on a closed contour, with two independent TE strengths and a Kutta row.
/// The nodal system uses the repository's checked dense LU. Surface speed is the exterior limiting velocity.
/// </summary>
public static class PanelMethod
{
    private const double TwoPi = 2 * Math.PI;

    public static PanelResult Solve(SectionSample section, double alphaDeg, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(section);
        int count = section.X.Count;
        if (count < 3 || section.Camber.Count != count || section.Thickness.Count != count)
            throw new ContractError("ANA-PANEL-GEOMETRY", "The section arrays need matching chord stations.");
        var contour = new SectionPoint[2 * count - 1];
        for (int i = 0; i < count; i++)
        {
            int source = count - 1 - i;
            contour[i] = new(section.X[source], section.Camber[source] + section.Thickness[source] / 2);
        }
        for (int i = 1; i < count; i++)
            contour[count - 1 + i] = new(section.X[i], section.Camber[i] - section.Thickness[i] / 2);
        return Solve(contour, alphaDeg, cancellation);
    }

    public static PanelResult Solve(IReadOnlyList<SectionPoint> contour, double alphaDeg,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(contour);
        int panels = contour.Count - 1;
        if (panels < 4 || panels > 800 || panels % 2 != 0 || !double.IsFinite(alphaDeg))
            throw new ContractError("ANA-PANEL-GEOMETRY", "A finite angle and an even, bounded panel count are required.");
        if (Math.Abs(contour[0].X - contour[^1].X) > 1e-10 ||
            Math.Abs(contour[0].Z - contour[^1].Z) > 1e-10)
            throw new ContractError("ANA-PANEL-GEOMETRY", "The trailing edge must close.");
        int unknowns = panels + 1;
        var segments = new Segment[panels];
        for (int j = 0; j < panels; j++)
        {
            SectionPoint p = contour[j], q = contour[j + 1];
            if (!double.IsFinite(p.X) || !double.IsFinite(p.Z) || !double.IsFinite(q.X) || !double.IsFinite(q.Z))
                throw new ContractError("ANA-PANEL-GEOMETRY", "A contour coordinate is not finite.");
            double length = Math.Sqrt(Math.Pow(q.X - p.X, 2) + Math.Pow(q.Z - p.Z, 2));
            if (!(length > 1e-12)) throw new ContractError("ANA-PANEL-GEOMETRY", "A panel has zero length.");
            segments[j] = new(p, q, length, (q.X - p.X) / length, (q.Z - p.Z) / length);
        }
        double angle = alphaDeg * Math.PI / 180;
        double vx = Math.Cos(angle), vz = Math.Sin(angle);
        double[] matrix = new double[unknowns * unknowns];
        double[] tangential = new double[panels * unknowns];
        double[] rhs = new double[unknowns];
        for (int i = 0; i < panels; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            Segment target = segments[i];
            SectionPoint mid = new((target.Start.X + target.End.X) / 2, (target.Start.Z + target.End.Z) / 2);
            double nx = -target.Tz, nz = target.Tx;
            rhs[i] = -(vx * nx + vz * nz);
            for (int j = 0; j < panels; j++)
            {
                (double u0, double v0, double u1, double v1) = Influence(mid, segments[j], i == j);
                Add(i, j, u0, v0);
                Add(i, j + 1, u1, v1);
            }
            // The right side of a counter-clockwise contour is the exterior. Its vortex-sheet jump is +γ/2.
            tangential[i * unknowns + i] += 0.25;
            tangential[i * unknowns + i + 1] += 0.25;

            void Add(int row, int col, double u, double v)
            {
                matrix[row * unknowns + col] += u * nx + v * nz;
                tangential[row * unknowns + col] += u * target.Tx + v * target.Tz;
            }
        }
        matrix[panels * unknowns] = 1;
        matrix[panels * unknowns + panels] = 1;
        double[] gamma = VortexLattice.SolveDense(matrix, rhs, unknowns, LatticePlant.None, cancellation).X;
        var upper = new List<PanelCp>(panels / 2);
        var lower = new List<PanelCp>(panels / 2);
        double cpMin = double.PositiveInfinity, cl = 0, cm = 0;
        for (int i = 0; i < panels; i++)
        {
            Segment segment = segments[i];
            double speed = vx * segment.Tx + vz * segment.Tz;
            for (int j = 0; j < unknowns; j++) speed += tangential[i * unknowns + j] * gamma[j];
            double cp = 1 - speed * speed;
            if (!double.IsFinite(cp)) throw new ContractError("ANA-PANEL-NONFINITE", "A panel Cp is not finite.");
            double x = (segment.Start.X + segment.End.X) / 2;
            double z = (segment.Start.Z + segment.End.Z) / 2;
            var sample = new PanelCp(x, z, cp);
            if (i < panels / 2) upper.Add(sample); else lower.Add(sample);
            cpMin = Math.Min(cpMin, cp);
            // Pressure force = Cp times the inward (left) normal for a counter-clockwise contour.
            double fx = -cp * segment.Tz * segment.Length;
            double fz = cp * segment.Tx * segment.Length;
            cl += -fx * Math.Sin(angle) + fz * Math.Cos(angle);
            cm -= (x - 0.25) * fz - z * fx;
        }
        return new(upper, lower, cpMin, panels, cl, cm);
    }

    private static (double U0, double V0, double U1, double V1) Influence(SectionPoint point, Segment panel, bool self)
    {
        if (self) return (-panel.Tz / TwoPi, panel.Tx / TwoPi,
            panel.Tz / TwoPi, -panel.Tx / TwoPi);
        double dx = point.X - panel.Start.X, dz = point.Z - panel.Start.Z;
        double a = dx * panel.Tx + dz * panel.Tz;
        double b = -dx * panel.Tz + dz * panel.Tx;
        double l = panel.Length;
        double start2 = a * a + b * b;
        double end2 = (a - l) * (a - l) + b * b;
        double angle = Math.Atan2(b * l, a * (a - l) + b * b);
        double integral = Math.Abs(b) > 1e-14 ? angle / b : l / (a * (a - l));
        double log = 0.5 * Math.Log(start2 / end2);
        double weighted = a * integral - log;
        double moment = a * log - l + b * b * integral;
        double u0 = -b * (integral - weighted / l) / TwoPi;
        double u1 = -b * weighted / l / TwoPi;
        double v0 = (log - moment / l) / TwoPi;
        double v1 = moment / l / TwoPi;
        return (u0 * panel.Tx - v0 * panel.Tz, u0 * panel.Tz + v0 * panel.Tx,
            u1 * panel.Tx - v1 * panel.Tz, u1 * panel.Tz + v1 * panel.Tx);
    }

    private readonly record struct Segment(SectionPoint Start, SectionPoint End, double Length, double Tx, double Tz);
}
