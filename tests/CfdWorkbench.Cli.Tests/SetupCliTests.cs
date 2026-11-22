using System.Text.Json;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;

/// <summary>
/// Node SET-3 tests (T13 in docs/design/setup-seeding.md §7, Ruling 272, CLI-01):
/// <c>cfd-workbench seed</c> + <c>inspect --json</c> parity with GUI <see cref="AuthoringSession.Seed"/>
/// by exact <c>SurfaceHash</c> and <c>GoalStateVersion.ContentHash</c>, plus <c>SETUP-FLAGGED-UNACK</c> gate.
/// </summary>
internal static class SetupCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunOne("Cli_SeedAndInspect_AgreesWithGuiBySurfaceAndGoalHash", T13_SeedAndInspectParityAsync);
        return failures;
    }

    private static async Task<int> RunOne(string name, Func<Task> check)
    {
        try
        {
            await check();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {name} {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    // T13 (CLI-01): cfd-workbench seed --preset wingfoil-race --acknowledge-flagged --out a.cfdw.json produces exact
    // same SurfaceHash and GoalStateHash as AuthoringSession.Seed, and inspect a.cfdw.json --json projects the same hashes.
    // Mutant: using different default taper or WaterTable temperature in CLI diverges SurfaceHash or GoalStateHash.
    private static async Task T13_SeedAndInspectParityAsync()
    {
        // 1. Unacknowledged Flagged defaults MUST fail closed with SETUP-FLAGGED-UNACK (exit 2).
        var unackOut = new StringWriter();
        int unackExit = await Cli.RunAsync(["seed", "--preset", "wingfoil-race"], unackOut);
        if (unackExit != 2 || !unackOut.ToString().Contains("SETUP-FLAGGED-UNACK", StringComparison.Ordinal))
            throw new Exception($"Expected exit 2 SETUP-FLAGGED-UNACK, got {unackExit}: {unackOut}");

        // 2. CLI seed --preset wingfoil-race --acknowledge-flagged --out <path>.
        string projectPath = TestTemp.Combine($"setup-seed-{Guid.NewGuid():N}.cfdw.json");
        try
        {
            var seedOut = new StringWriter();
            int seedExit = await Cli.RunAsync(
                ["seed", "--preset", "wingfoil-race", "--acknowledge-flagged", "--out", projectPath],
                seedOut);
            if (seedExit != 0)
                throw new Exception($"CLI seed failed ({seedExit}): {seedOut}");

            using var seedDoc = JsonDocument.Parse(seedOut.ToString());
            var sRoot = seedDoc.RootElement;
            string cliSurface = sRoot.GetProperty("surfaceHash").GetString()!;
            string cliGoalHash = sRoot.GetProperty("goalStateHash").GetString()!;

            // Compare against GUI AuthoringSession.Seed with identical inputs.
            using var guiSession = new AuthoringSession();
            var guiOutcome = guiSession.Seed(
                new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true),
                Guid.NewGuid().ToString("D"));

            if (cliSurface != guiOutcome.SurfaceHash)
                throw new Exception($"SurfaceHash mismatch: CLI {cliSurface} != GUI {guiOutcome.SurfaceHash}");
            if (cliGoalHash != guiOutcome.GoalState.ContentHash)
                throw new Exception($"GoalStateHash mismatch: CLI {cliGoalHash} != GUI {guiOutcome.GoalState.ContentHash}");

            // Inspect the written .cfdw.json via inspect --json and confirm exact projection parity.
            var inspectOut = new StringWriter();
            int inspectExit = await Cli.RunAsync(["inspect", projectPath, "--json"], inspectOut);
            if (inspectExit != 0)
                throw new Exception($"CLI inspect failed ({inspectExit}): {inspectOut}");

            using var inspDoc = JsonDocument.Parse(inspectOut.ToString());
            var iRoot = inspDoc.RootElement;
            if (iRoot.GetProperty("definitionHash").GetString() != guiOutcome.SurfaceHash)
                throw new Exception("inspect --json definitionHash mismatch");
            var setupEl = iRoot.GetProperty("setup");
            if (setupEl.GetProperty("recipeStatus").GetProperty("state").GetString() != "Linked")
                throw new Exception("inspect --json recipeStatus expected Linked");
            if (setupEl.GetProperty("latestGoalState").GetProperty("contentHash").GetString() != guiOutcome.GoalState.ContentHash)
                throw new Exception("inspect --json latestGoalState.contentHash mismatch");
            if (setupEl.GetProperty("latestGoalState").GetProperty("operatingPointCount").GetInt32() != 7)
                throw new Exception("inspect --json expected 7 operating points for wingfoil-race");

            // 3. Custom targets + fresh water override parity check.
            var customOut = new StringWriter();
            int customExit = await Cli.RunAsync(
                ["seed", "--preset", "downwind", "--area", "0.14", "--ar", "7", "--taper", "0.30", "--water", "fresh", "--temp-c", "15", "--acknowledge-flagged"],
                customOut);
            if (customExit != 0)
                throw new Exception($"CLI custom seed failed ({customExit}): {customOut}");
            using var customDoc = JsonDocument.Parse(customOut.ToString());
            string customSurface = customDoc.RootElement.GetProperty("surfaceHash").GetString()!;
            string customGoal = customDoc.RootElement.GetProperty("goalStateHash").GetString()!;

            var dw = DisciplinePresets.Get("downwind");
            var defTargets = DisciplinePresets.DefaultSoftTargets(dw);
            var guiCustomTargets = defTargets with
            {
                TargetAreaSquareMeters = defTargets.TargetAreaSquareMeters! with { Value = 0.14, Provenance = "Stated" },
                TargetAspectRatio = defTargets.TargetAspectRatio! with { Value = 7.0, Provenance = "Stated" },
                Taper = defTargets.Taper with { Value = 0.30, Provenance = "Stated" }
            };
            using var guiCustomSession = new AuthoringSession();
            var guiCustomOut = guiCustomSession.Seed(new SetupSeedRequest(
                "downwind",
                SoftTargets: guiCustomTargets,
                Water: DisciplinePresets.DefaultFreshWater15C,
                AcknowledgeAllFlagged: true), Guid.NewGuid().ToString("D"));

            if (customSurface != guiCustomOut.SurfaceHash)
                throw new Exception($"Custom SurfaceHash mismatch: CLI {customSurface} != GUI {guiCustomOut.SurfaceHash}");
            if (customGoal != guiCustomOut.GoalState.ContentHash)
                throw new Exception($"Custom GoalStateHash mismatch: CLI {customGoal} != GUI {guiCustomOut.GoalState.ContentHash}");
        }
        finally
        {
            if (File.Exists(projectPath)) File.Delete(projectPath);
        }
    }
}
