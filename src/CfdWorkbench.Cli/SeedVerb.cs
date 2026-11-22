using System.Diagnostics;
using System.Globalization;
using CfdWorkbench.Analysis;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;

namespace CfdWorkbench.Cli;

/// <summary>
/// <c>cfd-workbench seed --preset &lt;id|purpose&gt; [options] [--acknowledge-flagged] [--out &lt;path.cfdw.json&gt;]</c>
/// (docs/design/setup-seeding.md §1, §7 T13, Ruling 272, CLI-01). Uses the exact same
/// <see cref="AuthoringSession.Seed"/>, <see cref="DisciplinePresets"/>, <see cref="RecipeSeeder"/>, and
/// <see cref="WaterTable"/> code as the Desktop UI so <c>SurfaceHash</c> and <c>GoalStateVersion.ContentHash</c>
/// agree bit-for-bit.
/// </summary>
internal static class SeedVerb
{
    internal static async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellation)
    {
        var timer = Stopwatch.StartNew();
        string? presetOrPurpose = null;
        string? outPath = null;
        string? profileId = null;
        string waterKind = "salt";
        double tempC = OperatingPoints.DefaultTemperatureC;
        bool hasWaterOverride = false;
        double? bMax = null, wb = null, cMax = null, wc = null, area = null, wS = null, ar = null, wAr = null, taper = null;
        double? riderKg = null, hRef = null, mastCm = null, thickness = null;
        bool ackFlagged = false;

        try
        {
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--acknowledge-flagged") { ackFlagged = true; continue; }
                if (i + 1 >= args.Length) { await PrintUsageAsync(output); return 2; }
                string val = args[++i];
                switch (arg)
                {
                    case "--preset": presetOrPurpose = val; break;
                    case "--out": outPath = val; break;
                    case "--profile": profileId = val; break;
                    case "--water":
                        waterKind = val.Trim().ToLowerInvariant();
                        if (waterKind is not ("salt" or "fresh")) throw new ContractError("ANA-INPUT-WATER", "water must be 'salt' or 'fresh'");
                        hasWaterOverride = true;
                        break;
                    case "--temp-c": tempC = ParseDouble(val, "--temp-c"); hasWaterOverride = true; break;
                    case "--max-span": bMax = ParseDouble(val, "--max-span"); break;
                    case "--w-span": wb = ParseDouble(val, "--w-span"); break;
                    case "--max-chord": cMax = ParseDouble(val, "--max-chord"); break;
                    case "--w-chord": wc = ParseDouble(val, "--w-chord"); break;
                    case "--area": area = ParseDouble(val, "--area"); break;
                    case "--w-area": wS = ParseDouble(val, "--w-area"); break;
                    case "--ar": ar = ParseDouble(val, "--ar"); break;
                    case "--w-ar": wAr = ParseDouble(val, "--w-ar"); break;
                    case "--taper": taper = ParseDouble(val, "--taper"); break;
                    case "--rider-kg": riderKg = ParseDouble(val, "--rider-kg"); break;
                    case "--href": hRef = ParseDouble(val, "--href"); break;
                    case "--mast-cm": mastCm = ParseDouble(val, "--mast-cm"); break;
                    case "--thickness": thickness = ParseDouble(val, "--thickness"); break;
                    default: await PrintUsageAsync(output); return 2;
                }
            }

            if (string.IsNullOrWhiteSpace(presetOrPurpose))
            {
                await PrintUsageAsync(output);
                return 2;
            }

            cancellation.ThrowIfCancellationRequested();
            var resolution = DisciplinePresets.ResolvePurpose(presetOrPurpose);
            var preset = resolution.Preset;

            SoftTargetSet? targets = null;
            if (bMax.HasValue || wb.HasValue || cMax.HasValue || wc.HasValue ||
                area.HasValue || wS.HasValue || ar.HasValue || wAr.HasValue || taper.HasValue)
            {
                var def = DisciplinePresets.DefaultSoftTargets(preset);
                bool customWeights = wb.HasValue || wc.HasValue || wS.HasValue || wAr.HasValue;
                targets = new SoftTargetSet(
                    new SoftTargetScalar(bMax ?? def.MaxSpanMeters!.Value, wb ?? def.MaxSpanMeters!.Weight, bMax.HasValue ? "Stated" : def.MaxSpanMeters!.Provenance, bMax.HasValue ? "CLI seed" : def.MaxSpanMeters!.Source),
                    new SoftTargetScalar(cMax ?? def.MaxChordMeters!.Value, wc ?? def.MaxChordMeters!.Weight, cMax.HasValue ? "Stated" : def.MaxChordMeters!.Provenance, cMax.HasValue ? "CLI seed" : def.MaxChordMeters!.Source),
                    new SoftTargetScalar(area ?? def.TargetAreaSquareMeters!.Value, wS ?? def.TargetAreaSquareMeters!.Weight, area.HasValue ? "Stated" : def.TargetAreaSquareMeters!.Provenance, area.HasValue ? "CLI seed" : def.TargetAreaSquareMeters!.Source),
                    new SoftTargetScalar(ar ?? def.TargetAspectRatio!.Value, wAr ?? def.TargetAspectRatio!.Weight, ar.HasValue ? "Stated" : def.TargetAspectRatio!.Provenance, ar.HasValue ? "CLI seed" : def.TargetAspectRatio!.Source),
                    new SoftTargetTaper(taper ?? def.Taper.Value, taper.HasValue ? "Stated" : def.Taper.Provenance, taper.HasValue ? "CLI seed" : def.Taper.Source),
                    customWeights ? "custom" : def.WeightBasis);
            }

            WaterRecord? water = hasWaterOverride
                ? WaterTable.At(tempC, waterKind == "salt" ? OperatingPoints.SaltSalinityGPerKg : 0.0)
                : null;

            using var session = new AuthoringSession();
            var request = new SetupSeedRequest(
                Purpose: presetOrPurpose,
                SoftTargets: targets,
                EntryKind: "parameters",
                RiderMassKg: riderKg,
                Water: water,
                HRefMeters: hRef,
                MastLengthCm: mastCm,
                RootThicknessRatio: thickness,
                ProfileId: profileId,
                AcknowledgeAllFlagged: ackFlagged);

            var outcome = session.Seed(request, Guid.NewGuid().ToString("D"));
            var est = WingEstimates.From(session.Snapshot().Source, "accepted", 0);
            var feasibility = GoalFeasibility.Evaluate(est, outcome.GoalState);

            int outputBytes = 0;
            if (outPath is not null)
            {
                string fullOut = Path.GetFullPath(outPath);
                byte[] image = session.SaveImage();
                outputBytes = image.Length;
                using var store = new ProjectStore(session);
                string? expectedSha = null;
                if (File.Exists(fullOut))
                    expectedSha = (await store.ReadAsync(fullOut, cancellation)).DiskSha256;
                var save = await store.SaveAsync(fullOut, new SaveRequest(image, expectedSha, Guid.NewGuid().ToString("D")), cancellation);
                if (save.Code != "OK") throw new ContractError(save.Code, "could not write output project");
                session.AcknowledgeSaved(image);
            }

            timer.Stop();
            session.RecordSetupEvent("cli.seed", "OK", timer.Elapsed.TotalMilliseconds, new SetupTelemetry
            {
                PresetId = outcome.Brief.DisciplinePresetId,
                SurfaceHash12 = outcome.SurfaceHash[..12],
                GoalHash12 = outcome.GoalState.ContentHash[..12],
                OutputBytes = outputBytes > 0 ? outputBytes : null
            });

            await Cli.WriteAsync(output, new
            {
                schemaVersion = 1,
                purpose = outcome.Brief.Purpose,
                presetId = outcome.Brief.DisciplinePresetId,
                classRulePresetId = outcome.GoalState.ClassRulePresetId,
                surfaceHash = outcome.SurfaceHash,
                goalStateId = outcome.GoalState.GoalStateId,
                goalStateHash = outcome.GoalState.ContentHash,
                briefId = outcome.Brief.BriefId,
                briefHash = outcome.Brief.ContentHash,
                acceptedId = outcome.AcceptedId,
                revisionOrdinal = outcome.RevisionOrdinal,
                recipeStatus = session.RecipeStatus().State.ToString(),
                estimates = new
                {
                    est.SpanMeters,
                    est.AreaSquareMeters,
                    est.AspectRatio,
                    est.RootChordMeters,
                    est.TipChordMeters,
                    est.MeanChordMeters,
                    est.MacMeters,
                    est.MaxThicknessRatio
                },
                conflict = outcome.Brief.Recipe.ConflictNotice is not null,
                conflictNotice = outcome.Brief.Recipe.ConflictNotice,
                deviations = outcome.Brief.Recipe.Deviations,
                drcWarnings = feasibility.ClassFindings.Count(f => f.Severity is DrcSeverity.Error or DrcSeverity.Warning),
                drcAdvisories = feasibility.SanityFindings.Count
            });
            return 0;
        }
        catch (OperationCanceledException) { await output.WriteLineAsync("{\"code\":\"DOC-CANCELLED\"}"); return 130; }
        catch (ContractError error)
        {
            await Cli.WriteAsync(output, new { code = error.Code, reason = error.Reason });
            return Cli.ExitForCode(error.Code);
        }
        catch (IOException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
        catch (UnauthorizedAccessException) { await output.WriteLineAsync("{\"code\":\"DOC-IO\"}"); return 5; }
    }

    internal static object? SetupProjection(AuthoringSession session)
    {
        var setup = session.ReadSetup();
        if (setup is null) return null;
        var status = session.RecipeStatus();
        var latestBrief = setup.Briefs.Count > 0 ? setup.Briefs[^1] : null;
        var latestGoal = setup.GoalStates.Count > 0 ? setup.GoalStates[^1] : null;
        return new
        {
            briefCount = setup.Briefs.Count,
            goalStateCount = setup.GoalStates.Count,
            drcExclusionCount = setup.DrcExclusions.Count,
            recipeStatus = new
            {
                state = status.State.ToString(),
                presetId = status.PresetId,
                presetName = status.PresetName,
                briefId = status.BriefId,
                seededAcceptedId = status.SeededAcceptedId
            },
            latestBrief = latestBrief is null ? null : new
            {
                briefId = latestBrief.BriefId,
                contentHash = latestBrief.ContentHash,
                purpose = latestBrief.Purpose,
                disciplinePresetId = latestBrief.DisciplinePresetId,
                entryKind = latestBrief.EntryKind,
                seededAcceptedId = latestBrief.SeededAcceptedId,
                seededGoalStateId = latestBrief.SeededGoalStateId
            },
            latestGoalState = latestGoal is null ? null : new
            {
                goalStateId = latestGoal.GoalStateId,
                contentHash = latestGoal.ContentHash,
                disciplinePresetId = latestGoal.DisciplinePresetId,
                classRulePresetId = latestGoal.ClassRulePresetId,
                classRuleVersion = latestGoal.ClassRuleVersion,
                frontWingLoadN = latestGoal.FrontWingLoadN,
                operatingPointCount = latestGoal.OperatingPoints.Count
            }
        };
    }

    private static double ParseDouble(string text, string flag)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
            throw new ContractError("SETUP-INPUT", $"Invalid numeric value for {flag}: '{text}'.");
        return value;
    }

    private static async Task PrintUsageAsync(TextWriter output)
    {
        await output.WriteLineAsync("Usage: cfd-workbench seed --preset <id|purpose> [--max-span <m>] [--max-chord <m>] [--area <m2>] [--ar <ratio>] [--taper <ratio>] [--w-span <w>] [--w-chord <w>] [--w-area <w>] [--w-ar <w>] [--rider-kg <kg>] [--water salt|fresh] [--temp-c <C>] [--href <m>] [--mast-cm <cm>] [--thickness <ratio>] [--profile <id>] [--acknowledge-flagged] [--out <path.cfdw.json>]");
    }
}
