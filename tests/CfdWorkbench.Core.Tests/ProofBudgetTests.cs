using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>The geometry proof limit counts work, not clock (operator ruling 2026-10-02; defect class DET-CLOCK).</summary>
internal static class ProofBudgetTests
{
    private static string Id() => Guid.NewGuid().ToString("D");

    internal static void Run()
    {
        // Structural: a budget that holds a clock can refuse on a busy machine what a quiet one accepts.
        Check("ProofBudget_HoldsNoClock", () =>
        {
            Type[] clocks = [typeof(Stopwatch), typeof(TimeSpan), typeof(TimeSpan?), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeProvider)];
            var fields = typeof(ProofBudget).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Equal("", string.Join(",", fields.Where(field => clocks.Contains(field.FieldType)).Select(field => field.Name)));
        });
        // The limit is exact: a proof that spends W certifies under W + 1 and refuses under W, naming the limit.
        Check("ProofBudget_SameProof_SameWorkAndExactBoundary", () =>
        {
            var parsed = Prepared(FoilSourceTests.Example);
            long work = Geometry.Assess(parsed).ProofWork;
            Equal(true, work > 0);
            Equal(work, Geometry.Assess(parsed).ProofWork);
            Equal(GeometryStatus.Certified, Geometry.Assess(parsed, new ProofBudget(work + 1)).Status);
            var refused = Geometry.Assess(parsed, new ProofBudget(work));
            Equal("GEOMETRY-BUDGET", refused.Code);
            Equal(GeometryStatus.NotAssessed, refused.Status);
            Equal("Proof work limit of " + work.ToString(CultureInfo.InvariantCulture) + " bit-work units reached.", refused.Reason);
        });
        // Rational.Work is per thread, so a budget read on another thread would count someone else's work.
        Check("ProofBudget_ReadOnAnotherThread_Throws", () =>
        {
            var budget = new ProofBudget();
            Exception? thrown = null;
            var reader = new Thread(() => { try { _ = budget.Spent; } catch (InvalidOperationException failure) { thrown = failure; } });
            reader.Start(); reader.Join();
            Equal(true, thrown is not null);
        });
    }

    internal static void RunReadiness()
    {
        // The measured worst case (2026-10-03): the display samples of a ten-vertex rebuild, ~0.5 s each on a
        // quiet machine. Starved by 4 spinning threads per core, the wall-clock budget refused them.
        Check("Readiness_ProofOutcome_SameWithCpuStarved", () =>
        {
            using var session = Rebuilt();
            string quiet = Outcome(session);
            string starved;
            using (new Starvation(4 * Environment.ProcessorCount)) starved = Outcome(session);
            Equal(quiet, starved);
        });
        // Calibration guard: the worst accepted proof stays within a quarter of the limit (4x margin). Red means
        // a proof got more expensive; recalibrate ProofBudget.DefaultWorkLimit deliberately, with a measurement.
        Check("Readiness_ProofWork_WorstFixtureWithinQuarterOfLimit", () =>
        {
            using var session = Rebuilt();
            var parsed = FoilSource.Parse(session.Snapshot().Source);
            var profile = parsed.Definition!.Profiles[parsed.Definition.Assignments[0].Profile];
            long sample = Math.Max(SampleWork(profile.Upper), SampleWork(profile.Lower));
            long assess = 0;
            string fixtures = Path.Combine(PlacementTests.RepoRoot(), "tests", "CfdWorkbench.Core.Tests", "Fixtures");
            foreach (string path in Directory.EnumerateFiles(fixtures, "*.foil", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var assessment = Geometry.Assess(Prepared(File.ReadAllBytes(path)));
                if (assessment.Status == GeometryStatus.Certified) assess = Math.Max(assess, assessment.ProofWork);
            }
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"MEASURE proof_work rebuild_sample={sample} fixture_assess_max={assess} limit={ProofBudget.DefaultWorkLimit}"));
            Equal(true, assess > 0);
            Equal(true, Math.Max(sample, assess) <= ProofBudget.DefaultWorkLimit / 4);
        });
    }

    private static SourceParse Prepared(byte[] source)
    {
        var parsed = FoilSource.Parse(source);
        return parsed.IsParsed && parsed.Definition!.Curves.Values.Any(curve => curve.MissingIds)
            ? FoilSource.Parse(FoilSource.MaterializeIds(parsed)) : parsed;
    }

    private static AuthoringSession Rebuilt()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        string draft = Id();
        var begun = session.BeginProfileRebuild(draft, 0, SectionScope.Shared, 10, 1e-2, PreserveEnds.Position);
        session.Apply(Id(), session.Validate(draft, begun.Generation));
        return session;
    }

    private static long SampleWork(Curve curve)
    {
        var budget = new ProofBudget();
        _ = AuthoringSession.Sample(curve, budget);
        return budget.Spent;
    }

    private static string Outcome(AuthoringSession session)
    {
        try { return JsonSerializer.Serialize(session.ProfileAt(0)); }
        catch (Exception failure) { return failure.GetType().Name + ": " + failure.Message; }
    }

    /// <summary>Spins background threads at normal priority until disposed, so the proof thread gets a fraction of a core.</summary>
    private sealed class Starvation : IDisposable
    {
        private readonly Thread[] threads;
        private volatile bool stop;

        internal Starvation(int count)
        {
            threads = new Thread[count];
            for (int index = 0; index < count; index++)
            {
                threads[index] = new Thread(() => { while (!stop) Thread.SpinWait(1000); }) { IsBackground = true };
                threads[index].Start();
            }
        }

        public void Dispose()
        {
            stop = true;
            foreach (var thread in threads) thread.Join();
        }
    }
}
