using System.Text.Json;
using CfdWorkbench.Analysis.NeuralFoil;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.Tests;

internal static class PolarSeamTests
{
    // Readiness only: checks moved out of the fast ring (round-oct08 RGM, Ruling 143); never run by tools/run-tests.sh.
    internal static void RunReadiness()
    {
        AnalysisChecks.Check("Section_EditedProfileNoPolar_Unavailable", EditedRevision);
    }

    internal static void Run()
    {
        AnalysisChecks.Check("PolarSample_DerivedFlags_EqualEvaluateAtWriteTime", DerivedFlags);
        AnalysisChecks.Check("PolarSample_ReOutside_DerivedEqualsWriteTime", ReOutside);
        AnalysisChecks.Check("PolarSample_LowConfidence_DerivedEqualsWriteTime", LowConfidence);
    }

    private static void DerivedFlags()
    {
        var section = Naca0012Reference.FromSelig(new string('a', 64), "catalog", CatalogGenerator.Naca4("0012"));
        var source = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1700, "fixture", new string('b', 64));
        NeuralFoilEvaluation atWrite = source.Evaluate(section, 7, 500000, 4, CancellationToken.None);
        PolarResult? returned = source.Sample(section.ProfileHash, 500000, 4, 7, water, CancellationToken.None);
        if (returned is null)
            throw new InvalidOperationException("Sample must return a non-persisted result carrying the derived flags");
        PolarSample stored = returned.Sample;
        PolarResult onRead = NeuralFoilPolarSource.DeriveStored(stored, section);
        AnalysisChecks.Equal(string.Join("|", atWrite.OutsideBracketReasons), string.Join("|", returned.OutsideBracketReasons), "write-time bracket flags");
        AnalysisChecks.Equal(string.Join("|", atWrite.OutsideBracketReasons), string.Join("|", onRead.OutsideBracketReasons), "stored-sample read flags");
        AnalysisChecks.Equal(atWrite.LowConfidence, onRead.LowConfidence, "stored-sample confidence flag");
        AnalysisChecks.Equal(atWrite.CstResidualMax, onRead.CstResidualMax, "stored-sample CST residual");
        string json = JsonSerializer.Serialize(stored);
        if (json.Contains("OutsideBracket", StringComparison.Ordinal) || json.Contains("CstResidual", StringComparison.Ordinal))
            throw new InvalidOperationException("derived metadata was persisted");
        PolarSample oldRow = JsonSerializer.Deserialize<PolarSample>(json)!;
        AnalysisChecks.Equal(string.Join("|", atWrite.OutsideBracketReasons),
            string.Join("|", NeuralFoilPolarSource.DeriveStored(oldRow, section).OutsideBracketReasons), "old-row derivation");
    }

    private static void EditedRevision()
    {
        using var session = Fixture.Opened();
        AnalysisRun original = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2));
        NeuralFoilSection before = RunPolarResolver.SectionAt(session, original, 0);
        if (!Naca0012Reference.Matches(CstFit.Fit(before).Parameters))
            throw new InvalidOperationException("the base fixture is not the catalog NACA 0012 control");
        string draftId = Fixture.Id();
        var view = session.BeginSectionDraft(draftId, 0);
        var vertex = Sections.View(view.Bytes, 0, SurfaceSide.Upper, "accepted", 0).Points.First(point => point.Id == "cv-3");
        view = session.ApplySectionStep(draftId, view.Generation,
            new SectionStep.Move(SurfaceSide.Upper, vertex.Id, vertex.Eta, vertex.Ordinate + 0.004));
        session.FinishSection(Fixture.Id(), session.AssessSection(draftId, view.Generation, CancellationToken.None));
        AnalysisRun edited = Fixture.Evaluate(new AnalysisService(session, new FakeWing()), Fixture.Op(2));
        NeuralFoilSection after = RunPolarResolver.SectionAt(session, edited, 0);
        if (Naca0012Reference.Matches(CstFit.Fit(after).Parameters) || after.ProfileHash == before.ProfileHash)
            throw new InvalidOperationException("edited NACA 0012 received the catalog section identity");
        var polar = new NeuralFoilPolarSource(hash => hash == after.ProfileHash ? after : null);
        PolarResult? sample = polar.Sample(after.ProfileHash, 500000, 2, 2, Fixture.Salt, CancellationToken.None);
        AnalysisChecks.Equal(true, sample?.SectionUnvalidated, "edited NACA must not inherit the catalog polar bracket (flagged)");
        AnalysisChecks.Equal(null, sample?.AvailabilityCode, "Ruling 117: the family is a flag, not a refusal");
        NeuralFoilSection oldRead = RunPolarResolver.SectionAt(session, original, 0);
        AnalysisChecks.Equal(before.ProfileHash, oldRead.ProfileHash, "historical run still reads its own accepted revision");
    }

    private static void ReOutside()
    {
        var section = Naca0012Reference.FromSelig(new string('a', 64), "catalog", CatalogGenerator.Naca4("0012"));
        var source = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1700, "fixture", new string('b', 64));
        NeuralFoilEvaluation write = source.Evaluate(section, 3, 150000, 4, CancellationToken.None);
        PolarResult sampled = source.Sample(section.ProfileHash, 150000, 4, 3, water, CancellationToken.None)!;
        PolarResult read = NeuralFoilPolarSource.DeriveStored(sampled.Sample, section);
        AnalysisChecks.Equal("ANA-POLAR-RE-OUTSIDE", sampled.AvailabilityCode, "write-time Re bracket");
        AnalysisChecks.Equal(sampled.AvailabilityCode, read.AvailabilityCode, "read-time Re bracket");
        AnalysisChecks.Equal(string.Join("|", write.OutsideBracketReasons),
            string.Join("|", read.OutsideBracketReasons), "read-time Re reasons");
    }

    private static void LowConfidence()
    {
        var section = Naca0012Reference.FromSelig(new string('a', 64), "catalog", CatalogGenerator.Naca4("0012"));
        var source = new NeuralFoilPolarSource(hash => hash == section.ProfileHash ? section : null);
        var water = new WaterRecord(15, 0, 999, 1e-6, 1700, "fixture", new string('b', 64));
        PolarSample computed = source.Sample(section.ProfileHash, 500000, 4, 3, water, CancellationToken.None)!.Sample;
        PolarSample lowAtWrite = computed with { Confidence = 0.2 };
        bool writeFlag = NeuralFoilPolarSource.ConfidenceWarning(lowAtWrite.Confidence!.Value) is not null;
        PolarResult read = NeuralFoilPolarSource.DeriveStored(lowAtWrite, section);
        AnalysisChecks.Equal(true, writeFlag, "write-time advisory policy");
        AnalysisChecks.Equal(writeFlag, read.LowConfidence, "stored low-confidence flag derived on read");
        AnalysisChecks.Equal(null, read.AvailabilityCode, "low confidence remains advisory");
    }
}
