using CfdWorkbench.Core.SolverSetup;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>
/// Pure zero-I/O tests for T4 AI-11 deterministic SetupAssistantValidator:
/// schema strictness, allowedNext enforcement, citation resolution, SETUP-04 command lint,
/// no-success-claim, no-weakening-verbs, AI-03 numerals-in-context, and AI-01 offline key-free fallback.
/// </summary>
internal static class SolverAssistantValidatorTests
{
    internal static void Run()
    {
        Check("SolverAssistant_EnvironmentStep_AllowedAccepted_DisallowedOrExtraFieldRefused", EnvironmentStepValidation);
        Check("SolverAssistant_Explanation_ValidCitedAccepted_UnresolvedCitationWithheld", ExplanationCitations);
        Check("SolverAssistant_Explanation_CommandSuccessWeakeningAndNumeralsWithheld", ExplanationSafetyAndNumerals);
        Check("SolverAssistant_OfflineKeyFree_FallbackPreservesRails", OfflineKeyFreeFallback);
    }

    private static AssistantValidationContext MakeContext() => new(
        AllowedNextStepIds: new HashSet<string>(StringComparer.Ordinal) { "wsl.base.download", "wsl.unregister" },
        ExcerptLines: new Dictionary<int, string>
        {
            [1] = "Importing the distribution, this may take a few minutes.",
            [2] = "Error code: Wsl/Service/RegisterDistro/ERROR_FILE_EXISTS",
            [3] = "Free disk space observed: 0.2 GB on C: (needs 4 GB)."
        },
        KnownKnowledgeIds: new HashSet<string>(StringComparer.Ordinal) { "kb.wsl", "kb.disk", "kb.gatekeeper" },
        SharedContextText: "Step 4 failed with ERROR_FILE_EXISTS. Free disk space: 0.2 GB. Required: 4 GB. Download size: 389 MB.",
        CauseIsUnknown: true,
        FallbackFixedExplanation: "Windows could not create the private Linux. Copy a report or try step 4 again.");

    private static void EnvironmentStepValidation()
    {
        var ctx = MakeContext();

        // Allowed step id with exact 2-field schema -> Accepted.
        var ok = SetupAssistantValidator.Validate("""{"kind":"environment-step","stepId":"wsl.base.download"}""", ctx);
        Equal(AssistantVerdict.Shown, ok.Verdict);
        Equal("wsl.base.download", ok.SuggestedStepId);

        // Step outside allowedNext -> Refused (COPY-78).
        var outside = SetupAssistantValidator.Validate("""{"kind":"environment-step","stepId":"smoke.openfoam"}""", ctx);
        Equal(AssistantVerdict.Refused, outside.Verdict);
        Equal(true, outside.Message.StartsWith("Step refused: outside the allow-list", StringComparison.Ordinal));

        // Extra field (args/command/url) -> Refused!
        var extraField = SetupAssistantValidator.Validate("""{"kind":"environment-step","stepId":"wsl.base.download","args":["--force"]}""", ctx);
        Equal(AssistantVerdict.Refused, extraField.Verdict);
        Equal(true, extraField.Reason.Contains("extra-field", StringComparison.Ordinal));

        // Unknown kind -> Refused.
        var badKind = SetupAssistantValidator.Validate("""{"kind":"shell","command":"ls"}""", ctx);
        Equal(AssistantVerdict.Refused, badKind.Verdict);
    }

    private static void ExplanationCitations()
    {
        var ctx = MakeContext();

        // Valid explanation with resolved excerpt line + knowledge id + numerals from context -> Shown.
        var ok = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): an earlier private Linux copy still exists on line 2, and drive C: has 0.2 GB free while setup needs 4 GB.","citations":["line:2","kb.wsl"]}""",
            ctx);
        Equal(AssistantVerdict.Shown, ok.Verdict);
        Equal(2, ok.Citations.Count);

        // Unresolved line citation -> Withheld.
        var badLine = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): line 99 shows a missing file.","citations":["line:99"]}""",
            ctx);
        Equal(AssistantVerdict.Withheld, badLine.Verdict);
        Equal("unresolved-citation", badLine.Reason);
        Equal(ctx.FallbackFixedExplanation, badLine.DisplayText);

        // Unresolved knowledge id -> Withheld.
        var badKb = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): see the guide.","citations":["kb.nonexistent"]}""",
            ctx);
        Equal(AssistantVerdict.Withheld, badKb.Verdict);
        Equal("unresolved-citation", badKb.Reason);

        // Empty citations -> Withheld.
        var emptyCitations = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): disk space is low.","citations":[]}""",
            ctx);
        Equal(AssistantVerdict.Withheld, emptyCitations.Verdict);

        // Extra field on explanation -> Refused.
        var extra = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): see line 2.","citations":["line:2"],"run":"wsl"}""",
            ctx);
        Equal(AssistantVerdict.Refused, extra.Verdict);
    }

    private static void ExplanationSafetyAndNumerals()
    {
        var ctx = MakeContext();

        // Command to type -> Withheld (SETUP-04 shared lint).
        var cmd = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): run wsl --unregister cfdw-openfoam2512 to fix line 2.","citations":["line:2"]}""",
            ctx);
        Equal(AssistantVerdict.Withheld, cmd.Verdict);
        Equal("command-to-type", cmd.Reason);

        // Success claim -> Withheld (R4).
        string[] successClaims =
        [
            "OpenFOAM is ready on this computer now.",
            "The solver is installed and working on this PC.",
            "The problem is fixed and setup is done."
        ];
        foreach (string claim in successClaims)
        {
            var res = SetupAssistantValidator.Validate(
                $$$"""{"kind":"explanation","text":"{{{claim}}}","citations":["line:2"]}""", ctx);
            Equal(AssistantVerdict.Withheld, res.Verdict);
            Equal("claimed-success", res.Reason);
        }

        // Weakening OS protection -> Withheld (M8 / AI-11).
        string[] weakenings =
        [
            "You should disable Gatekeeper so OpenFOAM opens.",
            "Turn off SmartScreen temporarily for this download.",
            "Disable Windows Defender or turn off antivirus while unpacking.",
            "Click allow anyway in SmartScreen to bypass protection."
        ];
        foreach (string weak in weakenings)
        {
            var res = SetupAssistantValidator.Validate(
                $$$"""{"kind":"explanation","text":"{{{weak}}}","citations":["kb.gatekeeper"]}""", ctx);
            Equal(AssistantVerdict.Withheld, res.Verdict);
            Equal("weakens-os-protection", res.Reason);
        }

        // Numeral not in shared context -> Withheld (AI-03).
        var badNumeral = SetupAssistantValidator.Validate(
            """{"kind":"explanation","text":"Likely cause (not confirmed): line 2 fails when free space is below 73.5 GB.","citations":["line:2"]}""",
            ctx);
        Equal(AssistantVerdict.Withheld, badNumeral.Verdict);
        Equal("unsupported-numeral", badNumeral.Reason);
    }

    private static void OfflineKeyFreeFallback()
    {
        var offline = SetupAssistantValidator.OfflineStatus(hasApiKey: false);
        Equal(false, offline.Available);
        Equal(true, offline.RailsUnaffected);
        Equal(true, SetupCopyLint.Lint(offline.Notice).Passed);
    }
}
