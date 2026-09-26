using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class DatImportTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        Check("DatImport_Naca0012_SeligFitAndSessionCycle", () =>
        {
            byte[] seligBytes = GenerateNaca0012Selig();
            var parsed = DatImport.Parse(seligBytes);
            Equal("NACA 0012", parsed.Name);
            Equal("selig", parsed.Format);
            Equal(81, parsed.OriginalPointCount);

            var fitted = DatImport.Fit(parsed, "naca-0012");
            Equal(true, fitted.Accepted);
            Equal(true, fitted.MaxResidual <= 1e-5);
            Equal(true, fitted.VertexCount is >= 8 and <= 16);

            // Verify symmetric sides: y_upper == -y_lower for each control vertex
            var sourceText = WrapFoil(fitted.ProfileBlock, "naca-0012");
            var foilParsed = FoilSource.Parse(Encoding.UTF8.GetBytes(sourceText));
            Equal(true, foilParsed.IsParsed);
            var profileDef = foilParsed.Definition!.Profiles.Single(p => p.Name == "naca-0012");
            Equal(fitted.VertexCount, profileDef.Upper.Points.Length);
            Equal(fitted.VertexCount, profileDef.Lower.Points.Length);
            for (int i = 0; i < fitted.VertexCount; i++)
            {
                Equal(BitConverter.DoubleToUInt64Bits(profileDef.Upper.Points[i][0]),
                      BitConverter.DoubleToUInt64Bits(profileDef.Lower.Points[i][0]));
                Equal(true, Math.Abs(profileDef.Upper.Points[i][1] + profileDef.Lower.Points[i][1]) < 1e-9);
            }

            // Session import: base foil has root & tip sharing a profile with the same basis
            string baseFoil = WrapFoil(fitted.ProfileBlock.Replace("naca-0012", "base-section"), "base-section");
            byte[] originalBytes = FoilSource.MaterializeIds(FoilSource.Parse(Encoding.UTF8.GetBytes(baseFoil)));

            using var session = new AuthoringSession();
            session.Open(originalBytes, Id(), true);

            string draftId = Id();
            var draft = session.BeginProfileImport(draftId, 0, seligBytes);
            Equal(0L, draft.Generation);
            Equal("naca-0012", draft.Profile);
            Equal(0, draft.Assignment);

            var assessment = session.Validate(draftId, draft.Generation);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(true, assessment.ImportReport is not null);
            Equal(true, assessment.ImportReport!.Accepted);
            Equal(fitted.MaxResidual, assessment.ImportReport.MaxResidual);
            Equal(fitted.VertexCount, assessment.ImportReport.VertexCount);

            session.Apply(Id(), assessment);
            Equal(false, originalBytes.AsSpan().SequenceEqual(session.Snapshot().Source));

            session.Undo(Id());
            Equal(true, originalBytes.AsSpan().SequenceEqual(session.Snapshot().Source));
        });

        Check("DatImport_Naca0012_LednicerGivesSameProfileBlock", () =>
        {
            byte[] seligBytes = GenerateNaca0012Selig();
            byte[] lednicerBytes = GenerateNaca0012Lednicer();

            var seligProfile = DatImport.Parse(seligBytes);
            var lednicerProfile = DatImport.Parse(lednicerBytes);

            Equal("selig", seligProfile.Format);
            Equal("lednicer", lednicerProfile.Format);

            var seligFit = DatImport.Fit(seligProfile, "naca-0012");
            var lednicerFit = DatImport.Fit(lednicerProfile, "naca-0012");

            Equal(seligFit.VertexCount, lednicerFit.VertexCount);
            Equal(StripProvenance(seligFit.ProfileBlock), StripProvenance(lednicerFit.ProfileBlock));
        });

        Check("DatImport_Naca2412_CamberPositiveAtPointFour", () =>
        {
            byte[] bytes = GenerateNaca2412Selig();
            var parsed = DatImport.Parse(bytes);
            var fitted = DatImport.Fit(parsed, "naca-2412");
            Console.WriteLine($"NACA 2412 residual: {fitted.MaxResidual}, accepted: {fitted.Accepted}");

            string sourceText = WrapFoil(fitted.ProfileBlock, "naca-2412");
            var foilParsed = FoilSource.Parse(Encoding.UTF8.GetBytes(sourceText));
            Equal(true, foilParsed.IsParsed);

            var profile = foilParsed.Definition!.Profiles.Single(p => p.Name == "naca-2412");
            double u = Math.Sqrt(0.4);
            double[] basis = SplineBasis.Values(profile.Upper.Knots, 5, u);
            double upperY = profile.Upper.Points.Select((p, idx) => p[1] * basis[idx]).Sum();
            double lowerY = profile.Lower.Points.Select((p, idx) => p[1] * basis[idx]).Sum();
            double camber = (upperY + lowerY) / 2.0;

            Equal(true, camber > 0);
        });

        Check("DatImport_MalformedInput_ReportsLineNumber", () =>
        {
            // Case 1: non-numeric line at line 3
            string nonNumeric = "NACA 0012\n1.000000 0.000000\n0.950000 invalid_y\n0.000000 0.000000\n1.000000 0.000000";
            RefusesLine("DSL-IMPORT", 3, () => DatImport.Parse(Encoding.UTF8.GetBytes(nonNumeric)));

            // Case 2: fewer than 10 points
            string shortInput = "NACA 0012\n1.0 0.0\n0.5 0.05\n0.0 0.0\n1.0 0.0";
            RefusesLine("DSL-IMPORT", 5, () => DatImport.Parse(Encoding.UTF8.GetBytes(shortInput)));
        });

        Check("DatImport_Provenance_ContainsSha256OfOriginalBytes", () =>
        {
            byte[] seligBytes = GenerateNaca0012Selig();
            var parsed = DatImport.Parse(seligBytes);
            var fitted = DatImport.Fit(parsed, "naca-0012");
            string expectedHash = Identity.Sha256(seligBytes);

            Equal(true, fitted.Provenance.Contains(expectedHash));
            Equal(true, fitted.ProfileBlock.Contains(expectedHash));
        });

        Check("DatImport_Naca0012_ExampleTip_NeighbourBasisOrFallback", () =>
            ImportAtExampleTip(GenerateNaca0012Selig(), allowNeighbour: true));

        Check("DatImport_Naca2412_ExampleTip_NeighbourBasisOrFallback", () =>
            ImportAtExampleTip(GenerateNaca2412Selig(), allowNeighbour: true));

        Check("DatImport_Reflexed_ExampleTip_FallsBackUncertified", () =>
            ImportAtExampleTip(GenerateReflexedSelig(), allowNeighbour: false));
    }

    private const string OwnSpacingPrefix = "The imported shape needs its own vertex spacing (residual ";
    private const string OwnSpacingSuffix = " on the neighbour basis). Blending across different spacings is not certified yet: import it at every station that shares this profile, or Rebuild the neighbouring profiles.";

    private static void ImportAtExampleTip(byte[] dat, bool allowNeighbour)
    {
        byte[] originalBytes = FoilSource.MaterializeIds(FoilSource.Parse(FoilSourceTests.Example));
        using var session = new AuthoringSession();
        session.Open(originalBytes, Id(), true);
        byte[] opened = session.Snapshot().Source.ToArray();

        string draftId = Id();
        var draft = session.BeginProfileImport(draftId, 1, dat);
        Equal(1, draft.Assignment);

        var assessment = session.Validate(draftId, draft.Generation);
        Equal(true, assessment.ImportReport is not null);
        var report = assessment.ImportReport!;

        if (allowNeighbour && report.Basis == "neighbour")
        {
            Equal(true, report.MaxResidual <= 1e-5);
            Equal(GeometryStatus.Certified, assessment.Status);
            Equal(false, assessment.Diagnostics.Any(item => item.Reason.StartsWith(OwnSpacingPrefix, StringComparison.Ordinal)));
            session.Apply(Id(), assessment);
            Equal(false, opened.AsSpan().SequenceEqual(session.Snapshot().Source));
            session.Undo(Id());
            Equal(true, opened.AsSpan().SequenceEqual(session.Snapshot().Source));
            Console.WriteLine(FormattableString.Invariant($"IMPORT-BASIS: neighbour {report.MaxResidual}"));
            return;
        }

        Equal("own", report.Basis);
        Equal(true, assessment.Status != GeometryStatus.Certified);
        var diagnostic = assessment.Diagnostics.FirstOrDefault(item => item.Reason.StartsWith(OwnSpacingPrefix, StringComparison.Ordinal));
        Equal(true, diagnostic is not null);
        string reason = diagnostic!.Reason;
        Equal(true, reason.EndsWith(OwnSpacingSuffix, StringComparison.Ordinal));
        string token = reason[OwnSpacingPrefix.Length..^OwnSpacingSuffix.Length];
        double neighbourResidual = double.Parse(token, CultureInfo.InvariantCulture);
        Equal(true, neighbourResidual > 1e-5);
        Equal(OwnSpacingPrefix + neighbourResidual.ToString("G17", CultureInfo.InvariantCulture) + OwnSpacingSuffix, reason);
        Console.WriteLine(FormattableString.Invariant($"IMPORT-BASIS: own {neighbourResidual}"));
    }

    private static void RefusesLine(string code, int expectedLine, Action action)
    {
        try { action(); }
        catch (ContractError failure)
        {
            Equal(code, failure.Code);
            Equal(expectedLine, failure.Line ?? -1);
            return;
        }
        throw new InvalidOperationException($"Expected ContractError {code} at line {expectedLine} was not thrown.");
    }

    private static string StripProvenance(string block) =>
        string.Join("\n", block.Split('\n').Where(line => !line.TrimStart().StartsWith("provenance")));

    private static string WrapFoil(string profileBlock, string profileName) =>
        "foildsl \"4.0\"\n" +
        "foil \"Import test\" {\n" +
        "  units mm\n" +
        "  half_span 450 mm\n" +
        "  evaluator \"cfdw-cv\" \"2\"\n" +
        "  symmetry mirror_y\n" +
        "  planform {\n" +
        "    leading cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }\n" +
        "    trailing cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 120), (0.1, 120), (0.3, 120), (0.5, 120), (0.7, 120), (0.9, 120), (1, 120)] }\n" +
        "  }\n" +
        "  dihedral cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, 0), (0.5, 0), (0.7, 0), (0.9, 0), (1, 0)] }\n" +
        "  twist cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0), (0.1, 0), (0.3, -0.25), (0.5, -0.5), (0.7, -1), (0.9, -1.5), (1, -2)] }\n" +
        "  thickness cv { degree 3 knots [0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1] points [(0, 0.12), (0.1, 0.12), (0.3, 0.12), (0.5, 0.12), (0.7, 0.12), (0.9, 0.12), (1, 0.12)] }\n" +
        "  profiles {\n" +
        profileBlock + "\n" +
        "  }\n" +
        $"  sections {{ at root profile \"{profileName}\" at tip profile \"{profileName}\" }}\n" +
        "}\n";

    private static byte[] GenerateNaca0012Selig()
    {
        const int n = 40; // 41 points per side, 81 total
        var x = new double[n + 1];
        var yt = new double[n + 1];
        for (int i = 0; i <= n; i++)
        {
            x[i] = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            yt[i] = 5.0 * 0.12 * (0.2969 * Math.Sqrt(x[i]) - 0.1260 * x[i] - 0.3516 * x[i] * x[i] + 0.2843 * Math.Pow(x[i], 3) - 0.1036 * Math.Pow(x[i], 4));
        }

        var sb = new StringBuilder();
        sb.AppendLine("NACA 0012");
        // Upper from TE to LE
        for (int i = n; i >= 0; i--)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], yt[i]));
        // Lower from LE to TE (skip LE point to get 81 total)
        for (int i = 1; i <= n; i++)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], -yt[i]));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateNaca0012Lednicer()
    {
        const int n = 40; // 41 points per side
        var x = new double[n + 1];
        var yt = new double[n + 1];
        for (int i = 0; i <= n; i++)
        {
            x[i] = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            yt[i] = 5.0 * 0.12 * (0.2969 * Math.Sqrt(x[i]) - 0.1260 * x[i] - 0.3516 * x[i] * x[i] + 0.2843 * Math.Pow(x[i], 3) - 0.1036 * Math.Pow(x[i], 4));
        }

        var sb = new StringBuilder();
        sb.AppendLine("NACA 0012");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", n + 1, n + 1));
        // Upper LE to TE
        for (int i = 0; i <= n; i++)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], yt[i]));
        // Lower LE to TE
        for (int i = 0; i <= n; i++)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], -yt[i]));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateNaca2412Selig()
    {
        const int n = 40;
        const double m = 0.02, p = 0.4, t = 0.12;
        var x = new double[n + 1];
        var yu = new double[n + 1];
        var yl = new double[n + 1];
        for (int i = 0; i <= n; i++)
        {
            x[i] = 0.5 * (1.0 - Math.Cos(Math.PI * i / n));
            double yt = 5.0 * t * (0.2969 * Math.Sqrt(x[i]) - 0.1260 * x[i] - 0.3516 * x[i] * x[i] + 0.2843 * Math.Pow(x[i], 3) - 0.1036 * Math.Pow(x[i], 4));
            double yc = x[i] < p
                ? (m / (p * p)) * (2.0 * p * x[i] - x[i] * x[i])
                : (m / Math.Pow(1.0 - p, 2)) * ((1.0 - 2.0 * p) + 2.0 * p * x[i] - x[i] * x[i]);
            yu[i] = yc + yt;
            yl[i] = yc - yt;
        }

        var sb = new StringBuilder();
        sb.AppendLine("NACA 2412");
        for (int i = n; i >= 0; i--)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], yu[i]));
        for (int i = 1; i <= n; i++)
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x[i], yl[i]));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateReflexedSelig()
    {
        const int n = 100;
        var builder = new StringBuilder();
        builder.AppendLine("Reflex spike");
        for (int i = n; i >= 0; i--)
        {
            double x = i / (double)n;
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x, ReflexOrdinate(x, true)));
        }
        for (int i = 1; i <= n; i++)
        {
            double x = i / (double)n;
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:F6} {1:F6}", x, ReflexOrdinate(x, false)));
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static double ReflexOrdinate(double x, bool upper)
    {
        double spike = Math.Exp(-Math.Pow((x - 0.35) / 0.012, 2)) - 0.85 * Math.Exp(-Math.Pow((x - 0.72) / 0.012, 2));
        const double half = 0.004;
        return upper ? 0.15 * spike + half : 0.15 * spike - half;
    }
}
