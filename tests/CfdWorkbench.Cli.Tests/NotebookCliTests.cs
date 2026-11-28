using System.Text;
using System.Text.Json;
using CfdWorkbench.Cli;
using CfdWorkbench.Core;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Core.Notebook;
using CfdWorkbench.Persistence;
using CfdWorkbench.Persistence.Assistant;

/// <summary>
/// T-NB-06: Persistence (<see cref="AssistantSettingsStore"/> <c>OptionalFastModelId</c> + value-level secret screening)
/// and CLI (<c>notebook inspect|validate</c> &amp; <c>assistant eval --model</c> dynamic active count).
/// </summary>
internal static class NotebookCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunOne("T_NB_06_AssistantSettingsStore_OptionalFastModelId_AndValueLevelSecretScreening", TestSettingsFastModelAndSecretsAsync);
        failures += await RunOne("T_NB_06_Cli_NotebookInspectValidate_AndAssistantEvalModelDynamicCount", TestCliNotebookAndEvalModelAsync);
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

    private static async Task TestSettingsFastModelAndSecretsAsync()
    {
        string root = TestTemp.NewDirectory("nb-settings-");
        try
        {
            var store = new AssistantSettingsStore(root);

            // 1. Null OptionalFastModelId is OMITTED from serialized JSON (expand-only byte-compatibility).
            var baseSettings = AssistantSettings.CreateDefault();
            var saveNull = await store.SaveSettingsAsync(baseSettings);
            if (!saveNull.Succeeded) throw new Exception($"Base save failed: {saveNull.Code} {saveNull.Message}");
            string rawNullJson = await File.ReadAllTextAsync(store.SettingsPath, Encoding.UTF8);
            if (rawNullJson.Contains("optionalFastModelId", StringComparison.Ordinal))
                throw new Exception("Null OptionalFastModelId must be omitted from serialized assistant.json.");

            // 2. Non-null OptionalFastModelId round-trips cleanly.
            var withFast = baseSettings with { OptionalFastModelId = "claude-haiku-4-5" };
            var saveFast = await store.SaveSettingsAsync(withFast);
            if (!saveFast.Succeeded) throw new Exception($"Fast model save failed: {saveFast.Code} {saveFast.Message}");
            var loadedFast = await store.LoadSettingsAsync();
            if (loadedFast.Outcome != "restored" || loadedFast.Settings.OptionalFastModelId != "claude-haiku-4-5")
                throw new Exception($"OptionalFastModelId did not round-trip: {loadedFast.Outcome} '{loadedFast.Settings.OptionalFastModelId}'");

            // 3. Value-level secret screening refuses pasted API keys on SAVE in ModelId, OptionalFastModelId, and EndpointUrl.
            const string pastedSecret = "sk-ant-pasted-key-1234567890abcdef";
            var badFast = await store.SaveSettingsAsync(baseSettings with { OptionalFastModelId = pastedSecret });
            if (badFast.Succeeded || badFast.Code != "ASSISTANT-SECRET-FORBIDDEN")
                throw new Exception($"Expected ASSISTANT-SECRET-FORBIDDEN for fast model secret, got {badFast}");

            var badPrimary = await store.SaveSettingsAsync(baseSettings with { ModelId = pastedSecret });
            if (badPrimary.Succeeded || badPrimary.Code != "ASSISTANT-SECRET-FORBIDDEN")
                throw new Exception($"Expected ASSISTANT-SECRET-FORBIDDEN for primary model secret, got {badPrimary}");

            var badEndpoint = await store.SaveSettingsAsync(baseSettings with { EndpointUrl = "https://api.anthropic.com/" + pastedSecret });
            if (badEndpoint.Succeeded || badEndpoint.Code != "ASSISTANT-SECRET-FORBIDDEN")
                throw new Exception($"Expected ASSISTANT-SECRET-FORBIDDEN for endpoint secret, got {badEndpoint}");

            // 4. Value-level secret screening ALSO refuses pasted API keys on LOAD (crafted assistant.json).
            string craftedRoot = TestTemp.NewDirectory("nb-crafted-");
            try
            {
                var craftedStore = new AssistantSettingsStore(craftedRoot);
                Directory.CreateDirectory(craftedStore.AssistantDirectory);
                string craftedJson = $$"""
                {
                  "format": "cfdw-assistant",
                  "version": 1,
                  "providerKind": "Anthropic",
                  "endpointUrl": "https://api.anthropic.com",
                  "modelId": "claude-sonnet-4-5",
                  "optionalFastModelId": "{{pastedSecret}}",
                  "perRequestMaxTokens": 2048,
                  "dailyRequestCap": 50,
                  "timeoutSeconds": 30,
                  "evalReceipts": []
                }
                """;
                await File.WriteAllTextAsync(craftedStore.SettingsPath, craftedJson, new UTF8Encoding(false));
                var craftedLoad = await craftedStore.LoadSettingsAsync();
                if (craftedLoad.Outcome != "rejected" || !craftedLoad.NeverWrite || !craftedLoad.Codes.Contains("ASSISTANT-SECRET-FORBIDDEN"))
                    throw new Exception($"Crafted fast-model secret not rejected on load: {craftedLoad.Outcome} [{string.Join(",", craftedLoad.Codes)}]");
            }
            finally
            {
                if (Directory.Exists(craftedRoot)) Directory.Delete(craftedRoot, recursive: true);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static async Task TestCliNotebookAndEvalModelAsync()
    {
        string workDir = TestTemp.NewDirectory("nb-cli-");
        string storeDir = Path.Combine(workDir, "store");
        string projectPath = Path.Combine(workDir, "guided.cfdw.json");
        string plainProjectPath = Path.Combine(workDir, "plain.cfdw.json");

        try
        {
            // 1. Build a project with Envelope.Notebook + CAD handoff and save via ProjectStore.
            var reqDoc = FoilReqParser.Parse("""
                foilreq "1.0" {
                  brief { discipline: wingfoil-surf; skill: advanced; }
                  rider { mass: 90 kg @stated; }
                  water { kind: salt @defaulted; temp: 15 C @defaulted; depth_ref: 0.45 m @stated; }
                  envelope { max_span: <= 950 mm hard @stated; target_ar: ~ 10.2 soft(0.85) @stated; }
                }
                """).Document!;
            var reqVer = NotebookRecords.CreateRequirementVersion(reqDoc, "v1.0");
            var delta = CandidateIntentValidator.Validate("""
                {"kind":"candidate-intent-delta","spanMeters":0.93,"areaSquareMeters":0.0845,"taper":0.34,"rootThicknessRatio":0.118,"anhedralDeg":-5.0,"washoutDeg":-1.2,"rootProfileId":"cfdw-surf-118","tipProfileId":"cfdw-surf-105","provenance":[{"fieldName":"spanMeters","tag":"Stated","valueDisplay":"930 mm","reason":"Test"}]}
                """).Delta!;
            byte[] dsl = NotebookRecords.MaterializeCandidateDsl(delta);
            string surfHash = FoilSource.Parse(dsl).SurfaceHash!;
            var cand = NotebookRecords.CreateCandidateRecord(1, reqVer.ReqVersionId, delta, surfHash, nameof(GeometryStatus.Certified), null, []);
            var nb = NotebookRecords.FinalizeSession(new NotebookSessionRecord(
                Guid.NewGuid().ToString("D"),
                "wingfoil-surf · 90 kg",
                DateTimeOffset.UtcNow,
                [reqVer],
                [new NotebookTurnRecord(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow, "user-prompt", reqVer.ReqVersionId, null, NotebookRecords.ChunkText("90 kg surf foil"))],
                [cand],
                [],
                ""));

            using (var session = new AuthoringSession())
            {
                session.RecordNotebookSession(nb);
                session.HandoffNotebookCandidate(cand.CandidateId, Guid.NewGuid().ToString("D"));
                byte[] image = session.SaveImage();
                using var store = new ProjectStore(session);
                var save = await store.SaveAsync(projectPath, new SaveRequest(image, null, Guid.NewGuid().ToString("D")));
                if (save.Code != "OK") throw new Exception($"Save failed: {save.Code}");
                session.AcknowledgeSaved(image);
            }

            // 2. cfd-workbench notebook inspect & validate --json -> exit 0, valid == true, rebuildVerified == true.
            var inspOut = new StringWriter();
            int inspExit = await Cli.RunAsync(["notebook", "inspect", projectPath, "--json"], inspOut);
            if (inspExit != 0) throw new Exception($"notebook inspect failed ({inspExit}): {inspOut}");
            using (var doc = JsonDocument.Parse(inspOut.ToString()))
            {
                var root = doc.RootElement;
                if (!root.GetProperty("hasNotebook").GetBoolean() || !root.GetProperty("valid").GetBoolean())
                    throw new Exception($"notebook inspect expected valid=true: {inspOut}");
                if (!root.GetProperty("candidates")[0].GetProperty("rebuildVerified").GetBoolean())
                    throw new Exception("notebook inspect candidate rebuildVerified != true");
                if (root.GetProperty("handoffCount").GetInt32() != 1)
                    throw new Exception("notebook inspect handoffCount != 1");
            }

            var valOut = new StringWriter();
            int valExit = await Cli.RunAsync(["notebook", "validate", projectPath], valOut);
            if (valExit != 0 || !valOut.ToString().Contains("valid", StringComparison.Ordinal))
                throw new Exception($"notebook validate text failed ({valExit}): {valOut}");

            // 3. Project without notebook -> inspect returns exit 0 (hasNotebook=false), validate returns exit 3 (NB-ABSENT).
            using (var plainSession = new AuthoringSession())
            {
                plainSession.Seed(new SetupSeedRequest("wingfoil-race", AcknowledgeAllFlagged: true), Guid.NewGuid().ToString("D"));
                byte[] pImage = plainSession.SaveImage();
                using var pStore = new ProjectStore(plainSession);
                var pSave = await pStore.SaveAsync(plainProjectPath, new SaveRequest(pImage, null, Guid.NewGuid().ToString("D")));
                if (pSave.Code != "OK") throw new Exception($"Plain save failed: {pSave.Code}");
                plainSession.AcknowledgeSaved(pImage);
            }
            var plainValOut = new StringWriter();
            int plainValExit = await Cli.RunAsync(["notebook", "validate", plainProjectPath, "--json"], plainValOut);
            if (plainValExit != 3 || !plainValOut.ToString().Contains("NB-ABSENT", StringComparison.Ordinal))
                throw new Exception($"Expected exit 3 / NB-ABSENT on plain project, got {plainValExit}: {plainValOut}");

            // 4. assistant eval --model <fastModel> produces dynamic 11/11 active met text & 12-item v2.0 JSON receipt.
            var settingsStore = new AssistantSettingsStore(storeDir);
            await settingsStore.SaveSettingsAsync(AssistantSettings.CreateDefault() with { OptionalFastModelId = "claude-haiku-4-5" });
            var creds = new DummyCreds();

            var evalTextOut = new StringWriter();
            int evalTextExit = await AssistantVerb.RunAsync(["assistant", "eval", "--model", "claude-haiku-4-5", "--store", storeDir], evalTextOut, creds);
            if (evalTextExit != 0) throw new Exception($"assistant eval --model failed ({evalTextExit}): {evalTextOut}");
            string evalLine = evalTextOut.ToString();
            if (!evalLine.Contains("claude-haiku-4-5", StringComparison.Ordinal) || !evalLine.Contains("11/11 active met", StringComparison.Ordinal))
                throw new Exception($"Expected dynamic '11/11 active met' for claude-haiku-4-5, got: {evalLine}");

            var evalJsonOut = new StringWriter();
            int evalJsonExit = await AssistantVerb.RunAsync(["assistant", "eval", "--model", "claude-haiku-4-5", "--store", storeDir, "--json"], evalJsonOut, creds);
            if (evalJsonExit != 0) throw new Exception($"assistant eval --model --json failed ({evalJsonExit}): {evalJsonOut}");
            using (var doc = JsonDocument.Parse(evalJsonOut.ToString()))
            {
                var r = doc.RootElement.GetProperty("receipt");
                if (r.GetProperty("modelId").GetString() != "claude-haiku-4-5")
                    throw new Exception("receipt.modelId != claude-haiku-4-5");
                if (r.GetProperty("evalThresholdsVersion").GetString() != "2.0")
                    throw new Exception("receipt.evalThresholdsVersion != 2.0");
                if (r.GetProperty("items").GetArrayLength() != 12)
                    throw new Exception($"Expected 12 items in v2.0 receipt, got {r.GetProperty("items").GetArrayLength()}");
            }
        }
        finally
        {
            if (Directory.Exists(workDir)) Directory.Delete(workDir, recursive: true);
        }
    }

    private sealed class DummyCreds : ICredentialStore
    {
        public bool IsSupported => true;
        public bool HasKey(AssistantProviderKind providerKind) => false;
        public CredentialStoreResult ReadKey(AssistantProviderKind providerKind) => new(false, null, "CRED-NOT-FOUND", "No key");
        public CredentialStoreResult WriteKey(AssistantProviderKind providerKind, string apiKey) => new(true, null, null, null);
        public CredentialStoreResult DeleteKey(AssistantProviderKind providerKind) => new(true, null, null, null);
    }
}
