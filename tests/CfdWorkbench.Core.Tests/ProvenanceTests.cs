using System.Globalization;
using System.Text;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class ProvenanceTests
{
    private const string Origin = "gen:naca-4412";

    internal static void Run()
    {
        Check("Provenance_FormatParse_RoundTrip", FormatParseRoundTrip);
        Check("Provenance_LegacyDatForm_ParsesAsDat", LegacyDatForm);
        Check("Provenance_Unknown_NotRecordedNeverGuessed", UnknownNeverGuessed);
        Check("Provenance_Rights_DerivedFromOrigin", RightsDerived);
        Check("Provenance_MoveAfterReplace_MarkedModified", MoveAfterReplace);
        Check("Provenance_SurvivesEveryRewriter", SurvivesEveryRewriter);
    }

    private static void FormatParseRoundTrip()
    {
        Provenance[] samples =
        [
            new("gen:naca-4412", false),
            new("gen:naca-0009", true),
            new("vend:e817", false),
            new("vend:speer-h105", true),
            new("dat:sha256:" + new string('a', 64), false),
            new("dat:sha256:" + new string('b', 64), true),
            new(null, false)
        ];
        foreach (Provenance sample in samples)
            Equal(sample, Provenance.Parse(sample.Format()));
        Equal(Origin, new Provenance(Origin, false).Format());
        Equal(Origin + " modified", new Provenance(Origin, true).Format());
        Equal("", new Provenance(null, false).Format());
        Equal(new Provenance(Origin, true), Provenance.Parse("  " + Origin + " modified  "));
        Equal("Catalog original · NACA 4412", new Provenance(Origin, false).ChipText("NACA 4412"));
        Equal("Modified from NACA 4412", new Provenance(Origin, true).ChipText("NACA 4412"));
        var random = new Random(12);
        for (int index = 0; index < 24; index++)
        {
            string origin = (index % 3) switch
            {
                0 => "gen:naca-" + random.Next(0, 10000).ToString("0000", CultureInfo.InvariantCulture),
                1 => "vend:e" + random.Next(100, 1000).ToString(CultureInfo.InvariantCulture),
                _ => "dat:sha256:" + Hex(random)
            };
            var sample = new Provenance(origin, index % 2 == 0);
            Equal(sample, Provenance.Parse(sample.Format()));
        }
    }

    private static void LegacyDatForm()
    {
        string hex = new string('c', 64);
        string selig = "selig sha256:" + hex + " points:162";
        string lednicer = "lednicer sha256:" + hex + " points:81";
        Provenance parsed = Provenance.Parse(selig);
        Equal("dat:sha256:" + hex, parsed.Origin);
        Equal(false, parsed.Modified);
        Equal(RightsClass.YourFile, parsed.Rights);
        Equal("Catalog original · a .dat file", parsed.ChipText("a .dat file"));
        Provenance flagged = Provenance.Parse(lednicer + " modified");
        Equal("dat:sha256:" + hex, flagged.Origin);
        Equal(true, flagged.Modified);
        Equal(RightsClass.YourFile, flagged.Rights);
        Equal("Modified from a .dat file", flagged.ChipText("a .dat file"));
        Equal(parsed, Provenance.Parse(parsed.Format()));
    }

    private static void UnknownNeverGuessed()
    {
        string?[] unknown =
        [
            null, "", " ", "naca-4412", "NACA 4412", "gen:", "GEN:naca-4412", "vend:",
            "dat:sha256:" + new string('A', 64), "dat:abc", "dat:sha256:abcd",
            "selig sha256:" + new string('c', 63) + " points:10",
            "modified", "your file", Origin + " extra", "banana modified"
        ];
        foreach (string? text in unknown)
        {
            Provenance parsed = Provenance.Parse(text);
            Equal(null, parsed.Origin);
            Equal(false, parsed.Modified);
            Equal(RightsClass.NotRecorded, parsed.Rights);
            Equal("Source not recorded", parsed.ChipText("NACA 4412"));
        }
    }

    private static void RightsDerived()
    {
        Equal(RightsClass.Gen, new Provenance(Origin, false).Rights);
        Equal(RightsClass.Gen, new Provenance(Origin, true).Rights);
        Equal(RightsClass.Vend, new Provenance("vend:e817", true).Rights);
        Equal(RightsClass.YourFile, new Provenance("dat:sha256:" + new string('d', 64), false).Rights);
        Equal(RightsClass.NotRecorded, new Provenance(null, false).Rights);
        Equal(RightsClass.NotRecorded, new Provenance(null, true).Rights);
        Equal(RightsClass.YourFile, Provenance.Parse("selig sha256:" + new string('e', 64) + " points:20").Rights);
    }

    private static void MoveAfterReplace()
    {
        byte[] before = With(Origin);
        byte[] moved = Move(before, 0.02);
        byte[] marked = Provenance.MarkModified(moved, "section-a", before);
        Equal(Origin + " modified", Read(marked));
        Equal(Origin, Read(moved));

        byte[] tiny = Move(before, 1e-9);
        Equal(Origin, Read(Provenance.MarkModified(tiny, "section-a", before)));

        byte[] plain = Identified();
        Equal(null, Read(Provenance.MarkModified(Move(plain, 0.02), "section-a", plain)));

        string legacy = "selig sha256:" + new string('a', 64) + " points:162";
        byte[] legacyBefore = With(legacy);
        Equal(legacy + " modified", Read(Provenance.MarkModified(Move(legacyBefore, 0.02), "section-a", legacyBefore)));

        byte[] already = With(Origin + " modified");
        Equal(Origin + " modified", Read(Provenance.MarkModified(Move(already, 0.02), "section-a", already)));
    }

    private static void SurvivesEveryRewriter()
    {
        byte[] identified = Identified();
        byte[] source = With(Origin);
        Equal(FoilSource.Parse(identified).SurfaceHash, FoilSource.Parse(source).SurfaceHash);
        Equal(Origin, Read(source));
        var profile = FoilSource.Parse(source).Definition!.Profiles.Single(item => item.Name == "section-a");
        Keep(FoilSource.WriteSurfaces(source, "section-a", profile.Upper.Knots, profile.Upper.Points, profile.Upper.Ids,
            profile.Lower.Points, profile.Lower.Ids));
        Keep(FoilSource.WriteSideTangents(source, "section-a", SurfaceSide.Upper, [new TangentRow(profile.Upper.Ids[3], "smooth", null)]));
        byte[] inserted = FoilSource.InsertProfileKnot(source, "section-a", 0.4).Source;
        Keep(inserted);
        Keep(FoilSource.DeleteProfileVertex(inserted, "section-a", 2).Source);
        var independent = FoilSource.MakeIndependent(source, "section-a", 0);
        Keep(independent.Source);
        Equal(Origin, FoilSource.Parse(independent.Source).Definition!.Profiles.Single(item => item.Name == independent.NewProfile).Provenance);
        Keep(FoilSource.FairProfile(source, "section-a", 1e-2, PreserveEnds.Position).Source);
        Keep(FoilSource.RebuildProfile(source, "section-a", 8, 1e-2, PreserveEnds.Position).Source);
        Keep(FoilSource.Print(FoilSource.Parse(source).Definition!));
    }

    private static void Keep(byte[] bytes) => Equal(Origin, Read(bytes));

    private static string? Read(byte[] bytes) =>
        FoilSource.Parse(bytes).Definition!.Profiles.Single(item => item.Name == "section-a").Provenance;

    private static byte[] Identified() =>
        FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes(Path.Combine(
            PlacementTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil"))));

    private static byte[] With(string provenance)
    {
        byte[] source = Identified();
        var profile = FoilSource.Parse(source).Definition!.Profiles.Single(item => item.Name == "section-a");
        string text = FoilSource.Utf8.GetString(source);
        string next = text[..(profile.BlockEnd - 1)] + " provenance " + Jcs.Quote(provenance) + text[(profile.BlockEnd - 1)..];
        byte[] bytes = FoilSource.Utf8.GetBytes(next);
        var parsed = FoilSource.Parse(bytes);
        if (!parsed.IsParsed) throw new InvalidOperationException(parsed.Diagnostics[0].Code);
        return bytes;
    }

    private static byte[] Move(byte[] source, double dy)
    {
        var profile = FoilSource.Parse(source).Definition!.Profiles.Single(item => item.Name == "section-a");
        double[][] upper = profile.Upper.Points.Select(point => new[] { point[0], point[1] }).ToArray();
        upper[3][1] += dy;
        return FoilSource.WriteSurfaces(source, profile.Name, profile.Upper.Knots, upper, profile.Upper.Ids,
            profile.Lower.Points, profile.Lower.Ids);
    }

    private static string Hex(Random random)
    {
        var builder = new StringBuilder(64);
        for (int index = 0; index < 64; index++)
            builder.Append("0123456789abcdef"[random.Next(16)]);
        return builder.ToString();
    }
}
