using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure zero-I/O Core tests for Wave 11B A8.6 / AI-06 evaluation suite (docs/design/byok-assistants.md §2.5 &amp; §6.1):
/// 9 active deterministic eval items + 10th physical-fidelity reserved, fixed EvalThresholdsVersion = "1.0",
/// CheckModelEligibility lockout on model id / provider / endpoint / threshold change or any single failing item (M-CORE-5),
/// receipt tamper refusal, and zero geometry identity mutation on model/settings change.
/// </summary>
internal static class AssistantEvalTests
{
    internal static void Run()
    {
        Check("AssistantEvalSuite_NineActiveOneReserved_Ai06LockoutOnModelProviderOrThresholdChange", EvalSuiteAndAi06Lockout);
        Check("AssistantEvalSuite_DeterministicFixtureScoring_AndReceiptIntegrityTamperRefused", FixtureScoringAndTamperChecks);
    }

    private static void EvalSuiteAndAi06Lockout()
    {
        Equal("2.0", AssistantEvalSuite.EvalThresholdsVersion);

        var suite = AssistantEvalSuite.GetSuiteDefinition();
        Equal(12, suite.Count);

        var active = suite.Where(i => !i.Reserved).ToArray();
        var reserved = suite.Where(i => i.Reserved).ToArray();
        Equal(11, active.Length);
        Equal(1, reserved.Length);
        Equal("eval-12-physical-fidelity", reserved[0].ItemId);
        Equal(0, reserved[0].Cases.Count);

        // Verify all 9 active items have non-empty deterministic fixtures and design-time thresholds.
        Equal(0.90, suite.Single(i => i.ItemId == "eval-01-extraction-accuracy").Threshold);
        Equal(0.90, suite.Single(i => i.ItemId == "eval-02-label-accuracy").Threshold);
        foreach (var strictItem in active.Where(i => i.ItemId is not ("eval-01-extraction-accuracy" or "eval-02-label-accuracy")))
            Equal(1.00, strictItem.Threshold);
        foreach (var item in active)
            Equal(true, item.Cases.Count >= 1);

        // Seed a geometry session to prove AI-06 model/provider/threshold transitions never touch SurfaceHash or AcceptedId.
        using var session = new AuthoringSession();
        var seeded = session.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Guid.NewGuid().ToString("D"));
        string surfaceBefore = session.Snapshot().SurfaceHash;
        string acceptedBefore = session.Snapshot().AcceptedId;

        // Build a passing receipt over all 9 active items + 1 reserved item.
        var passingItems = BuildAllPassingItems(suite);
        var settings = new AssistantSettings(
            Format: AssistantSettings.FormatId,
            Version: AssistantSettings.CurrentVersion,
            ProviderKind: AssistantProviderKind.Anthropic,
            EndpointUrl: "https://api.anthropic.com",
            ModelId: "claude-sonnet-4-5",
            PerRequestMaxTokens: 2048,
            DailyRequestCap: 50,
            TimeoutSeconds: 30,
            EvalReceipts: []);

        // No receipt -> Unevaluated on this model.
        var noReceipt = AssistantEvalSuite.CheckModelEligibility(settings);
        Equal(false, noReceipt.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, noReceipt.StatusLabel);

        var validReceipt = AssistantEvalSuite.BuildReceipt(
            providerKind: AssistantProviderKind.Anthropic,
            endpointOrigin: "https://api.anthropic.com",
            modelId: "claude-sonnet-4-5",
            evaluatedAtUtc: new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero),
            items: passingItems);
        Equal(true, validReceipt.AllActiveItemsMetThreshold);
        Equal(true, AssistantEvalSuite.VerifyReceiptIntegrity(validReceipt));

        var evaluatedSettings = settings with { EvalReceipts = [validReceipt] };
        var ok = AssistantEvalSuite.CheckModelEligibility(evaluatedSettings);
        Equal(true, ok.ProposalsEnabled);
        Equal(AssistantEvalSuite.EvaluatedStatusLabel, ok.StatusLabel);
        Equal(0, ok.FailingItemIds.Count);

        // M-CORE-5: Changing ModelId disables proposals with "Unevaluated on this model".
        var changedModel = AssistantEvalSuite.CheckModelEligibility(evaluatedSettings with { ModelId = "claude-opus-4-5" });
        Equal(false, changedModel.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, changedModel.StatusLabel);

        // M-CORE-5: Changing ProviderKind disables proposals with "Unevaluated on this model".
        var changedProvider = AssistantEvalSuite.CheckModelEligibility(evaluatedSettings with { ProviderKind = AssistantProviderKind.OpenAiCompatible });
        Equal(false, changedProvider.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, changedProvider.StatusLabel);

        // M-CORE-5: Changing EndpointUrl / EndpointOrigin disables proposals with "Unevaluated on this model".
        var changedEndpoint = AssistantEvalSuite.CheckModelEligibility(evaluatedSettings with { EndpointUrl = "http://127.0.0.1:8080" });
        Equal(false, changedEndpoint.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, changedEndpoint.StatusLabel);

        // M-CORE-5 / T-NB-06: Receipt from older EvalThresholdsVersion ("1.0") disables proposals with "Unevaluated on this model".
        var oldThresholdReceipt = AssistantEvalSuite.BuildReceipt(
            providerKind: AssistantProviderKind.Anthropic,
            endpointOrigin: "https://api.anthropic.com",
            modelId: "claude-sonnet-4-5",
            evaluatedAtUtc: new DateTimeOffset(2026, 11, 24, 12, 5, 0, TimeSpan.Zero),
            items: passingItems,
            thresholdsVersion: "1.0");
        var changedThreshold = AssistantEvalSuite.CheckModelEligibility(settings with { EvalReceipts = [oldThresholdReceipt] });
        Equal(false, changedThreshold.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, changedThreshold.StatusLabel);

        // T-NB-06: OptionalFastModelId requires its own v2.0 eval receipt via modelIdOverride.
        var withFastModel = evaluatedSettings with { OptionalFastModelId = "claude-haiku-4-5" };
        Equal(true, AssistantEvalSuite.CheckModelEligibility(withFastModel).ProposalsEnabled);
        var fastUneval = AssistantEvalSuite.CheckModelEligibility(withFastModel, modelIdOverride: withFastModel.OptionalFastModelId);
        Equal(false, fastUneval.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, fastUneval.StatusLabel);

        var fastReceipt = AssistantEvalSuite.BuildReceipt(
            providerKind: AssistantProviderKind.Anthropic,
            endpointOrigin: "https://api.anthropic.com",
            modelId: "claude-haiku-4-5",
            evaluatedAtUtc: new DateTimeOffset(2026, 11, 24, 12, 6, 0, TimeSpan.Zero),
            items: passingItems);
        var fastEvaluated = AssistantEvalSuite.CheckModelEligibility(
            withFastModel with { EvalReceipts = [validReceipt, fastReceipt] },
            modelIdOverride: withFastModel.OptionalFastModelId);
        Equal(true, fastEvaluated.ProposalsEnabled);
        Equal(AssistantEvalSuite.EvaluatedStatusLabel, fastEvaluated.StatusLabel);

        // Any single active item below threshold disables proposals with "Evaluation failed — below threshold".
        var oneFailingItems = passingItems
            .Select(i => i.ItemId == "eval-07-prompt-injection"
                ? i with { PassedCases = 0, PassRate = 0.0, MetThreshold = false }
                : i)
            .ToArray();
        var failingReceipt = AssistantEvalSuite.BuildReceipt(
            providerKind: AssistantProviderKind.Anthropic,
            endpointOrigin: "https://api.anthropic.com",
            modelId: "claude-sonnet-4-5",
            evaluatedAtUtc: new DateTimeOffset(2026, 11, 24, 12, 10, 0, TimeSpan.Zero),
            items: oneFailingItems);
        Equal(false, failingReceipt.AllActiveItemsMetThreshold);
        var failedElig = AssistantEvalSuite.CheckModelEligibility(settings with { EvalReceipts = [failingReceipt] });
        Equal(false, failedElig.ProposalsEnabled);
        Equal(AssistantEvalSuite.FailedThresholdStatusLabel, failedElig.StatusLabel);
        Equal(1, failedElig.FailingItemIds.Count);
        Equal("eval-07-prompt-injection", failedElig.FailingItemIds[0]);

        // Geometry invariance (AI-06): SurfaceHash and AcceptedId unchanged after all settings/model changes.
        Equal(surfaceBefore, session.Snapshot().SurfaceHash);
        Equal(acceptedBefore, session.Snapshot().AcceptedId);
    }

    private static void FixtureScoringAndTamperChecks()
    {
        var suite = AssistantEvalSuite.GetSuiteDefinition();
        var results = new List<AssistantEvalItemResult>();

        foreach (var item in suite)
        {
            if (item.Reserved)
            {
                results.Add(new AssistantEvalItemResult(item.ItemId, item.Title, 0, 0, 0.0, item.Threshold, Reserved: true, MetThreshold: false));
                continue;
            }

            int passed = 0;
            foreach (var evalCase in item.Cases)
            {
                var outcome = AssistantEvalSuite.ScoreCaseResponse(evalCase, evalCase.ExpectedRawResponse);
                if (!outcome.Passed)
                    throw new InvalidOperationException($"Expected baseline fixture '{evalCase.CaseId}' to pass: {outcome.Reason}");
                passed++;
            }

            double rate = (double)passed / item.Cases.Count;
            results.Add(new AssistantEvalItemResult(item.ItemId, item.Title, passed, item.Cases.Count, rate, item.Threshold, Reserved: false, MetThreshold: rate >= item.Threshold));
        }

        var receipt = AssistantEvalSuite.BuildReceipt(
            AssistantProviderKind.OpenAiCompatible,
            "http://127.0.0.1:18080",
            "local-eval-model",
            new DateTimeOffset(2026, 11, 24, 13, 0, 0, TimeSpan.Zero),
            results);
        Equal(true, receipt.AllActiveItemsMetThreshold);
        Equal(true, AssistantEvalSuite.VerifyReceiptIntegrity(receipt));

        // Tampering with pass rate or model id invalidates receipt integrity and fails closed to Unevaluated.
        var tamperedHash = receipt with { ModelId = "other-model" };
        Equal(false, AssistantEvalSuite.VerifyReceiptIntegrity(tamperedHash));

        var settings = AssistantSettings.CreateDefault() with
        {
            ProviderKind = AssistantProviderKind.OpenAiCompatible,
            EndpointUrl = "http://127.0.0.1:18080",
            ModelId = "other-model",
            EvalReceipts = [tamperedHash]
        };
        var elig = AssistantEvalSuite.CheckModelEligibility(settings);
        Equal(false, elig.ProposalsEnabled);
        Equal(AssistantEvalSuite.UnevaluatedStatusLabel, elig.StatusLabel);
    }

    private static IReadOnlyList<AssistantEvalItemResult> BuildAllPassingItems(IReadOnlyList<AssistantEvalItemDefinition> suite) =>
        suite.Select(item => item.Reserved
            ? new AssistantEvalItemResult(item.ItemId, item.Title, 0, 0, 0.0, item.Threshold, Reserved: true, MetThreshold: false)
            : new AssistantEvalItemResult(item.ItemId, item.Title, item.Cases.Count, item.Cases.Count, 1.0, item.Threshold, Reserved: false, MetThreshold: true))
            .ToArray();
}
