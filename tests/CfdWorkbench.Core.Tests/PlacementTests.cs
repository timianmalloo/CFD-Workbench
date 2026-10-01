using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class PlacementTests
{
    internal static void Run()
    {
        Check("PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged", CertificateGolden);
        Check("ChannelEvaluator_WingEstimatesFold_BitsUnchanged", WingGolden);
        Check("ChannelEvaluator_PlanformViewFold_SamplesUnchanged", PlanformGolden);
    }

    private static void CertificateGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var root = document.RootElement;
        foreach (var fixture in root.GetProperty("fixtures").EnumerateArray())
        {
            string name = fixture.GetProperty("name").GetString()!;
            var assessment = Geometry.Assess(Prepare(fixture.GetProperty("source").GetString()!));
            var certificate = assessment.Certificate ?? throw new InvalidOperationException(name + " did not certify");
            Same(name + " status", fixture.GetProperty("status").GetString(), assessment.Status.ToString());
            Same(name + " code", fixture.GetProperty("code").GetString(), assessment.Code);
            Same(name + " placement width", fixture.GetProperty("placementWidthUpper").GetString(), Hex(certificate.PlacementWidthUpper));
            var width = fixture.GetProperty("exactPlacementWidth");
            Same(name + " exact width numerator", width.GetProperty("n").GetString(), certificate.ExactPlacementWidthUpper.Numerator);
            Same(name + " exact width denominator", width.GetProperty("d").GetString(), certificate.ExactPlacementWidthUpper.Denominator);
            var thickness = fixture.GetProperty("thicknessMaximum");
            Same(name + " thickness lower", thickness.GetProperty("lower").GetString(), Hex(certificate.ThicknessMaximumLower));
            Same(name + " thickness upper", thickness.GetProperty("upper").GetString(), Hex(certificate.ThicknessMaximumUpper));
            SameFeasibility(name, fixture.GetProperty("feasibility"), certificate.QueryFeasibility);
            SameWitnesses(name, fixture.GetProperty("witnesses"), certificate.Witnesses);
            foreach (var probe in fixture.GetProperty("probes").EnumerateArray())
            {
                double eta = probe.GetProperty("eta").GetDouble();
                double x = probe.GetProperty("x").GetDouble();
                bool upper = probe.GetProperty("upper").GetBoolean();
                bool port = probe.GetProperty("port").GetBoolean();
                string where = name + " eta " + eta + " x " + x + " upper " + upper + " port " + port;
                var point = Geometry.PointAt(certificate, eta, x, upper, port);
                var expectedPoint = probe.GetProperty("point");
                SameOrdinate(where + " X", expectedPoint.GetProperty("x"), point.X);
                SameOrdinate(where + " Y", expectedPoint.GetProperty("y"), point.Y);
                SameOrdinate(where + " Z", expectedPoint.GetProperty("z"), point.Z);
                var section = Geometry.SectionAt(certificate, eta, x);
                SameOrdinate(where + " section upper", probe.GetProperty("section").GetProperty("upper"), section.Upper);
                SameOrdinate(where + " section lower", probe.GetProperty("section").GetProperty("lower"), section.Lower);
            }
        }
        string example = root.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        foreach (var refusal in root.GetProperty("refusals").EnumerateArray())
            SameRefusal(refusal, example);
    }

    private static void WingGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var expected = document.RootElement.GetProperty("wingEstimates");
        string example = document.RootElement.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        var wing = WingEstimates.From(FoilSource.MaterializeIds(Prepare(example)), "accepted", 0);
        Same("wing span", expected.GetProperty("span").GetString(), Hex(wing.SpanMeters));
        Same("wing root", expected.GetProperty("rootChord").GetString(), Hex(wing.RootChordMeters));
        Same("wing tip", expected.GetProperty("tipChord").GetString(), Hex(wing.TipChordMeters));
        Same("wing mean", expected.GetProperty("meanChord").GetString(), Hex(wing.MeanChordMeters));
        Same("wing mac", expected.GetProperty("mac").GetString(), Hex(wing.MacMeters));
        Same("wing thickness", expected.GetProperty("maxThickness").GetString(), Hex(wing.MaxThicknessRatio));
        Same("wing aspect", expected.GetProperty("aspect").GetString(), Hex(wing.AspectRatio));
        Same("wing area", expected.GetProperty("area").GetString(), Hex(wing.AreaSquareMeters));
        Same("wing basis", expected.GetProperty("basis").GetString(), wing.Basis);
        Equal(expected.GetProperty("generation").GetInt64(), wing.Generation);
        Equal(expected.GetProperty("converged").GetBoolean(), wing.Converged);
        Equal(expected.GetProperty("intervals").GetInt32(), wing.Compute.IntervalCount);
        Equal(expected.GetProperty("iterations").GetInt32(), wing.Compute.Iterations);
        Same("wing outcome", expected.GetProperty("outcome").GetString(), wing.Compute.Outcome);
    }

    private static void PlanformGolden()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GoldenPath()));
        var expected = document.RootElement.GetProperty("planform");
        string example = document.RootElement.GetProperty("fixtures").EnumerateArray().First(item => item.GetProperty("name").GetString() == "example").GetProperty("source").GetString()!;
        var plan = Planform.View(FoilSource.MaterializeIds(Prepare(example)), "accepted", 0);
        Same("plan half span", expected.GetProperty("halfSpan").GetString(), Hex(plan.HalfSpanMeters));
        SameSamples("plan leading", expected.GetProperty("leading"), plan.Leading.Samples);
        SameSamples("plan trailing", expected.GetProperty("trailing"), plan.Trailing.Samples);
        var stations = expected.GetProperty("stations").EnumerateArray().ToArray();
        Equal(stations.Length, plan.Stations.Count);
        for (int index = 0; index < stations.Length; index++)
        {
            Same("plan station eta " + index, stations[index].GetProperty("eta").GetString(), Hex(plan.Stations[index].Eta));
            Same("plan station span " + index, stations[index].GetProperty("span").GetString(), Hex(plan.Stations[index].SpanMeters));
        }
    }

    private static void SameFeasibility(string name, JsonElement expected, QueryFeasibilityWitness actual)
    {
        Same(name + " algorithm", expected.GetProperty("Algorithm").GetString(), actual.Algorithm);
        Equal(expected.GetProperty("RationalBitLimit").GetInt32(), actual.RationalBitLimit);
        Equal(expected.GetProperty("MaximumIntermediateBits").GetInt32(), actual.MaximumIntermediateBits);
        Equal(expected.GetProperty("RationalOperationsUpper").GetInt64(), actual.RationalOperationsUpper);
        Equal(expected.GetProperty("InverseDepth").GetInt32(), actual.InverseDepth);
        Equal(expected.GetProperty("AngleGridBits").GetInt32(), actual.AngleGridBits);
        Same(name + " bit path", expected.GetProperty("MaximumBitPath").GetString(), actual.MaximumBitPath);
        var spans = expected.GetProperty("spans").EnumerateArray().ToArray();
        Equal(spans.Length, actual.Spans.Count);
        for (int index = 0; index < spans.Length; index++)
        {
            var span = spans[index];
            var got = actual.Spans[index];
            Same(name + " span curve " + index, span.GetProperty("Curve").GetString(), got.Curve);
            Equal(span.GetProperty("Span").GetInt32(), got.Span);
            Equal(span.GetProperty("Degree").GetInt32(), got.Degree);
            Equal(span.GetProperty("CommonDenominatorBits").GetInt32(), got.CommonDenominatorBits);
            Equal(span.GetProperty("RefinedNumeratorBits").GetInt32(), got.RefinedNumeratorBits);
            Equal(span.GetProperty("RefinedDenominatorBits").GetInt32(), got.RefinedDenominatorBits);
        }
    }

    private static void SameWitnesses(string name, JsonElement expected, IReadOnlyList<GeometryWitness> actual)
    {
        var rows = expected.EnumerateArray().ToArray();
        Equal(rows.Length, actual.Count);
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var got = actual[index];
            Same(name + " witness " + index, row.GetProperty("Curve").GetString(), got.Curve);
            SameRatio(name + " witness " + index + " start", row.GetProperty("domainStart"), got.DomainStart);
            SameRatio(name + " witness " + index + " end", row.GetProperty("domainEnd"), got.DomainEnd);
            SameRatio(name + " witness " + index + " abscissa lower", row.GetProperty("abscissaLower"), got.AbscissaDifferenceLower);
            SameRatio(name + " witness " + index + " abscissa upper", row.GetProperty("abscissaUpper"), got.AbscissaDifferenceUpper);
            SameRatio(name + " witness " + index + " ordinate lower", row.GetProperty("ordinateLower"), got.OrdinateLower);
            SameRatio(name + " witness " + index + " ordinate upper", row.GetProperty("ordinateUpper"), got.OrdinateUpper);
        }
    }

    private static void SameRefusal(JsonElement expected, string example)
    {
        string name = expected.GetProperty("name").GetString()!;
        var parsed = Prepare(example);
        var certificate = Geometry.Assess(parsed).Certificate ?? throw new InvalidOperationException("example did not certify");
        (string Kind, string Code, string Status, string Reason) actual = name switch
        {
            "point-budget" => Caught(() => Geometry.PointAt(certificate, .5, .5, true, timeBudget: TimeSpan.Zero)),
            "section-budget" => Caught(() => Geometry.SectionAt(certificate, .5, .5, TimeSpan.Zero)),
            "point-cancel" => Caught(() => Geometry.PointAt(certificate, .5, .5, true, cancellationToken: new CancellationToken(true))),
            "section-cancel" => Caught(() => Geometry.SectionAt(certificate, .5, .5, cancellationToken: new CancellationToken(true))),
            "assess-budget" => Assessed(example, TimeSpan.Zero),
            "taylor-domain" => Assessed(example.Replace("(1, -2)", "(1, -90)", StringComparison.Ordinal), null),
            "ten-nanometre-budget" => Assessed(Trailing(example), null),
            _ => throw new InvalidOperationException("Unknown refusal " + name),
        };
        Same(name + " kind", expected.GetProperty("kind").GetString(), actual.Kind);
        Same(name + " code", expected.GetProperty("code").GetString(), actual.Code);
        Same(name + " status", expected.GetProperty("status").GetString(), actual.Status);
        Same(name + " reason", expected.GetProperty("reason").GetString(), actual.Reason);
    }

    private static (string Kind, string Code, string Status, string Reason) Caught(Action action)
    {
        try { action(); }
        catch (ContractError error) { return ("contract", error.Code, "", error.Message); }
        throw new InvalidOperationException("Expected a contract refusal.");
    }

    private static (string Kind, string Code, string Status, string Reason) Assessed(string text, TimeSpan? budget)
    {
        var parsed = Prepare(text);
        if (!parsed.IsParsed) return ("parse", parsed.Diagnostics[0].Code, "", parsed.Diagnostics[0].Reason);
        var assessment = Geometry.Assess(parsed, budget);
        return ("assess", assessment.Code, assessment.Status.ToString(), assessment.Reason);
    }

    private static void SameSamples(string where, JsonElement expected, IReadOnlyList<PlanSample> actual)
    {
        var rows = expected.EnumerateArray().ToArray();
        Equal(rows.Length, actual.Count);
        for (int index = 0; index < rows.Length; index++)
        {
            Same(where + " span " + index, rows[index].GetProperty("span").GetString(), Hex(actual[index].SpanMeters));
            Same(where + " aft " + index, rows[index].GetProperty("aft").GetString(), Hex(actual[index].AftMeters));
        }
    }

    private static void SameOrdinate(string where, JsonElement expected, EnclosedOrdinate actual)
    {
        Same(where + " lower", expected[0].GetString(), Hex(actual.Lower));
        Same(where + " upper", expected[1].GetString(), Hex(actual.Upper));
    }

    private static void SameRatio(string where, JsonElement expected, ExactRatio actual)
    {
        Same(where + " n", expected.GetProperty("n").GetString(), actual.Numerator);
        Same(where + " d", expected.GetProperty("d").GetString(), actual.Denominator);
    }

    private static void Same(string where, string? expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException(where + " changed. Certificate bits changed. Review the hand-kept models QueryFeasibility, PlacementWidth and BlendPlacementWidth.");
    }

    private static string Trailing(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int index = 0; index < lines.Length; index++)
            if (lines[index].StartsWith("    trailing cv ", StringComparison.Ordinal))
                lines[index] = lines[index].Replace(", 120)", ", 1e20)", StringComparison.Ordinal);
        return string.Join('\n', lines);
    }

    private static SourceParse Prepare(string text)
    {
        var parsed = FoilSource.Parse(Encoding.UTF8.GetBytes(text));
        if (!parsed.IsParsed) return parsed;
        return FoilSource.Parse(FoilSource.MaterializeIds(parsed));
    }

    private static string Hex(double value) => BitConverter.DoubleToUInt64Bits(value).ToString("x16");

    private static string GoldenPath([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "docs", "proof", "m12b2-golden", "certificate-bits.json"));
}
