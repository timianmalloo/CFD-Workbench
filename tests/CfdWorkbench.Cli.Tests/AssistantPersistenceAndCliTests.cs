using System.Text;
using System.Text.Json;
using CfdWorkbench.Cli;
using CfdWorkbench.Core.Assistant;
using CfdWorkbench.Persistence.Assistant;

/// <summary>
/// Wave 11C OS credential store, closed-DTO settings/audit persistence, and CLI assistant verb tests
/// (docs/design/byok-assistants.md §6.2, C1, C3, M-RUN-1, M-RUN-2, M-RUN-5).
/// </summary>
internal static class AssistantPersistenceAndCliTests
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;
        failures += await RunOne("OsCredentialStore_WindowsCredManager_RoundTrip_SizeLimit_NoEnvFallback", TestOsCredentialStoreAsync);
        failures += await RunOne("AssistantSettingsStore_ClosedDto_SecretFieldRefused_VersionNeverWrite_SymlinkRefused_UtcDailyCap", TestSettingsAndAuditStoreAsync);
        failures += await RunOne("Cli_Assistant_StatusPreviewEvalAudit_JsonContracts", TestCliAssistantContractsAsync);
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

    private static Task TestOsCredentialStoreAsync()
    {
        string isolatedPrefix = $"CfdWorkbench:ByokKey:test-{Guid.NewGuid():N}";
        var store = new OsCredentialStore(isolatedPrefix);

        string? prevAnthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        string? prevOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            // Zero env-var fallback even when standard provider env vars are populated (AI-01, M-RUN-1).
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-env-must-never-be-read-123456");
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-openai-env-must-never-be-read-123456");

            if (store.HasKey(AssistantProviderKind.Anthropic))
                throw new Exception("HasKey returned true from env var when OS credential store is empty.");
            var envRead = store.ReadKey(AssistantProviderKind.Anthropic);
            if (envRead.Succeeded || !string.IsNullOrEmpty(envRead.Secret))
                throw new Exception("ReadKey fell back to environment variable!");

            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                if (!store.IsSupported)
                    throw new Exception("Expected OsCredentialStore.IsSupported == true on Windows/macOS.");

                // Reject empty, whitespace, control characters, and > 2560-byte blobs (C1, M-RUN-1).
                if (store.WriteKey(AssistantProviderKind.Anthropic, "").Succeeded)
                    throw new Exception("Empty key should be rejected.");
                if (store.WriteKey(AssistantProviderKind.Anthropic, "   ").Succeeded)
                    throw new Exception("Whitespace key should be rejected.");
                if (store.WriteKey(AssistantProviderKind.Anthropic, "sk-bad\0nul").Succeeded)
                    throw new Exception("Control-char key should be rejected.");

                string oversize = new('k', OsCredentialStore.MaxCredentialBlobBytes + 1);
                var tooBig = store.WriteKey(AssistantProviderKind.Anthropic, oversize);
                if (tooBig.Succeeded || tooBig.ErrorCode != "CRED-TOO-LARGE")
                    throw new Exception($"Expected CRED-TOO-LARGE for 2561-byte key, got {tooBig}");

                // Exact 2560-byte boundary succeeds.
                string exactBoundary = new('b', OsCredentialStore.MaxCredentialBlobBytes);
                var boundaryWrite = store.WriteKey(AssistantProviderKind.Anthropic, exactBoundary);
                if (!boundaryWrite.Succeeded)
                    throw new Exception($"Exact 2560-byte key write failed: {boundaryWrite.ErrorCode} {boundaryWrite.Message}");
                var boundaryRead = store.ReadKey(AssistantProviderKind.Anthropic);
                if (!boundaryRead.Succeeded || boundaryRead.Secret != exactBoundary)
                    throw new Exception("Exact 2560-byte key round-trip mismatch.");

                // Realistic key round-trip + immediate delete.
                string realTestKey = "sk-ant-wincred-roundtrip-" + Guid.NewGuid().ToString("N");
                var writeRes = store.WriteKey(AssistantProviderKind.Anthropic, realTestKey);
                if (!writeRes.Succeeded)
                    throw new Exception($"WriteKey failed: {writeRes.ErrorCode} {writeRes.Message}");
                if (!store.HasKey(AssistantProviderKind.Anthropic))
                    throw new Exception("HasKey returned false after WriteKey.");

                var readRes = store.ReadKey(AssistantProviderKind.Anthropic);
                if (!readRes.Succeeded || readRes.Secret != realTestKey)
                    throw new Exception($"ReadKey round-trip mismatch: {readRes.ErrorCode}");

                // Separate provider slot stays independent.
                if (store.HasKey(AssistantProviderKind.OpenAiCompatible))
                    throw new Exception("OpenAiCompatible slot should still be empty.");

                var delRes = store.DeleteKey(AssistantProviderKind.Anthropic);
                if (!delRes.Succeeded)
                    throw new Exception($"DeleteKey failed: {delRes.ErrorCode} {delRes.Message}");
                if (store.HasKey(AssistantProviderKind.Anthropic))
                    throw new Exception("HasKey still true after DeleteKey.");
                var afterDel = store.ReadKey(AssistantProviderKind.Anthropic);
                if (afterDel.Succeeded)
                    throw new Exception("ReadKey succeeded after DeleteKey.");

                // Idempotent repeat delete.
                var delAgain = store.DeleteKey(AssistantProviderKind.Anthropic);
                if (!delAgain.Succeeded)
                    throw new Exception($"Idempotent second DeleteKey failed: {delAgain.ErrorCode}");
            }
            else
            {
                // Unsupported OS fails closed.
                if (store.IsSupported)
                    throw new Exception("Unsupported OS must report IsSupported == false.");
                var res = store.WriteKey(AssistantProviderKind.Anthropic, "sk-test-12345");
                if (res.Succeeded || res.ErrorCode != "CRED-UNSUPPORTED-OS")
                    throw new Exception($"Unsupported OS must fail closed with CRED-UNSUPPORTED-OS, got {res}");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", prevAnthropic);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", prevOpenAi);
            try { store.DeleteKey(AssistantProviderKind.Anthropic); } catch { }
            try { store.DeleteKey(AssistantProviderKind.OpenAiCompatible); } catch { }
        }

        return Task.CompletedTask;
    }

    private static async Task TestSettingsAndAuditStoreAsync()
    {
        string root = TestTemp.NewDirectory("assistant-store-");
        try
        {
            var store = new AssistantSettingsStore(root);

            // 1. Absent file -> defaults.
            var absent = await store.LoadSettingsAsync();
            if (absent.Outcome != "absent" || absent.NeverWrite || absent.SessionOnly)
                throw new Exception($"Expected absent load, got {absent.Outcome}");

            // 2. Save valid settings + eval receipt -> atomic write, no BOM, round-trips cleanly.
            var receipt = BuildPassingReceipt(AssistantProviderKind.Anthropic, "https://api.anthropic.com", "claude-sonnet-4-5");
            var valid = AssistantSettings.CreateDefault() with
            {
                PerRequestMaxTokens = 4096,
                DailyRequestCap = 25,
                TimeoutSeconds = 45,
                EvalReceipts = [receipt]
            };
            var saveRes = await store.SaveSettingsAsync(valid);
            if (!saveRes.Succeeded || saveRes.Outcome != "saved")
                throw new Exception($"Valid settings save failed: {saveRes.Outcome} {saveRes.Code} {saveRes.Message}");

            byte[] rawSettings = await File.ReadAllBytesAsync(store.SettingsPath);
            if (rawSettings.Length >= 3 && rawSettings[0] == 0xEF && rawSettings[1] == 0xBB && rawSettings[2] == 0xBF)
                throw new Exception("assistant.json must not have UTF-8 BOM.");

            var reloaded = await store.LoadSettingsAsync();
            if (reloaded.Outcome != "restored" || reloaded.Settings.PerRequestMaxTokens != 4096 ||
                reloaded.Settings.DailyRequestCap != 25 || reloaded.Settings.TimeoutSeconds != 45 ||
                reloaded.Settings.EvalReceipts.Count != 1)
                throw new Exception($"Reloaded settings mismatch: {reloaded.Outcome}");

            // 3. Reject secret fields (apiKey, secret, token, password, authorization) on parse (M-RUN-2).
            foreach (string badProp in new[] { "apiKey", "api_key", "secret", "token", "password", "authorization" })
            {
                string secRoot = TestTemp.NewDirectory("assistant-sec-");
                try
                {
                    var secStore = new AssistantSettingsStore(secRoot);
                    Directory.CreateDirectory(secStore.AssistantDirectory);
                    string badJson = $$"""
                    {
                      "format": "cfdw-assistant",
                      "version": 1,
                      "providerKind": "Anthropic",
                      "endpointUrl": "https://api.anthropic.com",
                      "modelId": "claude-sonnet-4-5",
                      "perRequestMaxTokens": 2048,
                      "dailyRequestCap": 50,
                      "timeoutSeconds": 30,
                      "{{badProp}}": "sk-ant-must-be-rejected",
                      "evalReceipts": []
                    }
                    """;
                    await File.WriteAllTextAsync(secStore.SettingsPath, badJson, new UTF8Encoding(false));
                    var loadSec = await secStore.LoadSettingsAsync();
                    if (loadSec.Outcome != "rejected" || !loadSec.NeverWrite || !loadSec.Codes.Contains("ASSISTANT-SECRET-FORBIDDEN"))
                        throw new Exception($"Property '{badProp}' was not rejected with ASSISTANT-SECRET-FORBIDDEN: {loadSec.Outcome} [{string.Join(",", loadSec.Codes)}]");

                    var tryOverwrite = await secStore.SaveSettingsAsync(AssistantSettings.CreateDefault());
                    if (tryOverwrite.Succeeded || tryOverwrite.Outcome != "never-write")
                        throw new Exception($"Rejected secret file was overwritten for '{badProp}': {tryOverwrite.Outcome}");
                }
                finally
                {
                    if (Directory.Exists(secRoot)) Directory.Delete(secRoot, recursive: true);
                }
            }

            // 4. Fail-closed never-write on version > 1 (preserves bytes byte-for-byte).
            string v2Root = TestTemp.NewDirectory("assistant-v2-");
            try
            {
                var v2Store = new AssistantSettingsStore(v2Root);
                Directory.CreateDirectory(v2Store.AssistantDirectory);
                const string v2Text = "{\"format\":\"cfdw-assistant\",\"version\":2,\"futureField\":true}\n";
                byte[] v2Bytes = Encoding.UTF8.GetBytes(v2Text);
                await File.WriteAllBytesAsync(v2Store.SettingsPath, v2Bytes);

                var v2Load = await v2Store.LoadSettingsAsync();
                if (v2Load.Outcome != "never-write" || !v2Load.NeverWrite || !v2Load.Codes.Contains("ASSISTANT-VERSION"))
                    throw new Exception($"v2 file did not trigger never-write/ASSISTANT-VERSION: {v2Load.Outcome}");

                var v2Save = await v2Store.SaveSettingsAsync(AssistantSettings.CreateDefault());
                if (v2Save.Succeeded || v2Save.Outcome != "never-write")
                    throw new Exception($"v2 SaveSettingsAsync must return never-write, got {v2Save.Outcome}");

                byte[] afterBytes = await File.ReadAllBytesAsync(v2Store.SettingsPath);
                if (!v2Bytes.SequenceEqual(afterBytes))
                    throw new Exception("v2 file bytes were altered!");
            }
            finally
            {
                if (Directory.Exists(v2Root)) Directory.Delete(v2Root, recursive: true);
            }

            // 5. Symlink / junction refusal.
            string symRoot = TestTemp.NewDirectory("assistant-sym-");
            string symTarget = TestTemp.NewDirectory("assistant-sym-target-");
            try
            {
                string linkPath = Path.Combine(symRoot, "assistant");
                if (TestLinks.TryDirectoryLink(linkPath, symTarget))
                {
                    var symStore = new AssistantSettingsStore(symRoot);
                    var symLoad = await symStore.LoadSettingsAsync();
                    if (symLoad.Outcome != "session-only" || !symLoad.SessionOnly || !symLoad.Codes.Contains("ASSISTANT-SYMLINK-REFUSED"))
                        throw new Exception($"Symlinked assistant dir not refused on load: {symLoad.Outcome} [{string.Join(",", symLoad.Codes)}]");

                    var symSave = await symStore.SaveSettingsAsync(AssistantSettings.CreateDefault());
                    if (symSave.Succeeded || symSave.Outcome != "session-only")
                        throw new Exception($"Symlinked assistant dir not refused on save: {symSave.Outcome}");

                    bool auditOk = await symStore.AppendAuditEntryAsync(MakeAuditEntry(DateTimeOffset.UtcNow, "consented", "shown", null, null));
                    if (auditOk)
                        throw new Exception("Symlinked assistant dir allowed audit append!");
                }
            }
            finally
            {
                try { if (Directory.Exists(Path.Combine(symRoot, "assistant"))) Directory.Delete(Path.Combine(symRoot, "assistant")); } catch { }
                try { if (Directory.Exists(symRoot)) Directory.Delete(symRoot, recursive: true); } catch { }
                try { if (Directory.Exists(symTarget)) Directory.Delete(symTarget, recursive: true); } catch { }
            }

            // 6. Delete-sharing-safe append-only audit JSONL + UTC daily cap counting "Not recorded" usage (M-RUN-2).
            DateOnly day1 = new(2026, 11, 24);
            DateOnly day2 = new(2026, 11, 25);

            await store.AppendAuditEntryAsync(MakeAuditEntry(new DateTimeOffset(2026, 11, 24, 1, 0, 0, TimeSpan.Zero), "consented", "shown", 100, 50));
            await store.AppendAuditEntryAsync(MakeAuditEntry(new DateTimeOffset(2026, 11, 24, 2, 0, 0, TimeSpan.Zero), "consented", "shown", null, null)); // Not recorded MUST count!
            await store.AppendAuditEntryAsync(MakeAuditEntry(new DateTimeOffset(2026, 11, 24, 3, 0, 0, TimeSpan.Zero), "declined", "declined", null, null)); // declined must NOT count
            await store.AppendAuditEntryAsync(MakeAuditEntry(new DateTimeOffset(2026, 11, 24, 4, 0, 0, TimeSpan.Zero), "blocked-cap", "blocked", null, null)); // blocked must NOT count
            await store.AppendAuditEntryAsync(MakeAuditEntry(new DateTimeOffset(2026, 11, 25, 0, 5, 0, TimeSpan.Zero), "consented", "shown", null, null)); // next UTC day

            // Simulate concurrent reader holding ReadWrite|Delete sharing while store appends & counts.
            using (var concurrentReader = new FileStream(store.AuditPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                bool appendedWhileOpen = await store.AppendAuditEntryAsync(
                    MakeAuditEntry(new DateTimeOffset(2026, 11, 24, 23, 59, 0, TimeSpan.Zero), "consented", "error", null, null));
                if (!appendedWhileOpen)
                    throw new Exception("Audit append failed while concurrent reader held ReadWrite|Delete share.");

                int day1Count = await store.CountDailyRequestsUtcAsync(day1);
                if (day1Count != 3)
                    throw new Exception($"Expected 3 consented UTC requests on {day1} (including Not recorded), got {day1Count}");

                int day2Count = await store.CountDailyRequestsUtcAsync(day2);
                if (day2Count != 1)
                    throw new Exception($"Expected 1 consented UTC request on {day2}, got {day2Count}");
            }

            // Append corrupt trailing line -> ReadAuditEntriesAsync skips cleanly without crashing.
            await File.AppendAllTextAsync(store.AuditPath, "{corrupt-truncated-line\n", new UTF8Encoding(false));
            var entries = await store.ReadAuditEntriesAsync(100);
            if (entries.Count != 6)
                throw new Exception($"Expected 6 intact audit entries despite corrupt trailing line, got {entries.Count}");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static async Task TestCliAssistantContractsAsync()
    {
        string storeDir = TestTemp.NewDirectory("assistant-cli-");
        string excerptFile = TestTemp.Combine("assistant-excerpt-" + Guid.NewGuid().ToString("N") + ".txt");
        const string canaryKey = "sk-ant-cli-canary-9876543210abcd";
        string fakeUser = "bob_hydro";
        string fakePath = string.Concat("C:", "\\Users\\", fakeUser, "\\Desktop\\wing.dat");

        try
        {
            await File.WriteAllTextAsync(excerptFile, $"Header {fakePath} key={canaryKey}", new UTF8Encoding(false));
            var creds = new CliTestCredentialStore();
            creds.WriteKey(AssistantProviderKind.Anthropic, canaryKey);

            // 1. assistant status --json (before eval: hasApiKey true, proposalsEnabled false, key NEVER leaked).
            var statusOut = new StringWriter();
            int statusExit = await AssistantVerb.RunAsync(["assistant", "status", "--store", storeDir, "--json"], statusOut, creds);
            if (statusExit != 0) throw new Exception($"assistant status failed ({statusExit}): {statusOut}");
            string statusText = statusOut.ToString();
            AssertNoKey(statusText, canaryKey, "status");
            using (var doc = JsonDocument.Parse(statusText))
            {
                var root = doc.RootElement;
                if (root.GetProperty("schemaVersion").GetInt32() != 1) throw new Exception("status schemaVersion != 1");
                if (!root.GetProperty("hasApiKey").GetBoolean()) throw new Exception("status hasApiKey expected true");
                if (root.GetProperty("eligibility").GetProperty("proposalsEnabled").GetBoolean())
                    throw new Exception("status proposalsEnabled expected false before eval");
                if (root.GetProperty("eligibility").GetProperty("statusLabel").GetString() != AssistantEvalSuite.UnevaluatedStatusLabel)
                    throw new Exception("status expected Unevaluated on this model");
            }

            // 2. assistant eval --json (runs 9 active + 1 reserved, saves receipt, unlocks proposals).
            var evalOut = new StringWriter();
            int evalExit = await AssistantVerb.RunAsync(["assistant", "eval", "--store", storeDir, "--json"], evalOut, creds);
            if (evalExit != 0) throw new Exception($"assistant eval failed ({evalExit}): {evalOut}");
            string evalText = evalOut.ToString();
            AssertNoKey(evalText, canaryKey, "eval");
            using (var doc = JsonDocument.Parse(evalText))
            {
                var root = doc.RootElement;
                if (!root.GetProperty("eligibility").GetProperty("proposalsEnabled").GetBoolean())
                    throw new Exception("eval did not enable proposals");
                if (root.GetProperty("receipt").GetProperty("items").GetArrayLength() != AssistantEvalSuite.GetSuiteDefinition().Count)
                    throw new Exception($"eval receipt did not contain {AssistantEvalSuite.GetSuiteDefinition().Count} items");
            }

            // 3. assistant preview --area setup --prompt ... --input <file> --json -> byte-exact SHA-256 & redacted.
            var previewOut = new StringWriter();
            int previewExit = await AssistantVerb.RunAsync(
                ["assistant", "preview", "--area", "setup", "--prompt", $"Wingfoil race from {fakePath} {canaryKey}", "--input", excerptFile, "--store", storeDir, "--json"],
                previewOut,
                creds);
            if (previewExit != 0) throw new Exception($"assistant preview failed ({previewExit}): {previewOut}");
            string previewText = previewOut.ToString();
            AssertNoKey(previewText, canaryKey, "preview");
            if (previewText.Contains(fakePath, StringComparison.Ordinal))
                throw new Exception("assistant preview leaked file path!");
            using (var doc = JsonDocument.Parse(previewText))
            {
                var root = doc.RootElement;
                string canonical = root.GetProperty("canonicalWirePreview").GetString()!;
                string sha = root.GetProperty("payloadSha256").GetString()!;
                if (PromptRedactor.ComputeUtf8Sha256Hex(canonical) != sha)
                    throw new Exception("assistant preview payloadSha256 does not match canonicalWirePreview!");
                if (root.GetProperty("promptUtf8Bytes").GetInt32() > PromptRedactor.MaxPromptUtf8Bytes)
                    throw new Exception("assistant preview exceeded 32768 UTF-8 bytes!");
            }

            // 4. assistant preview --area export -> exit 3 with exact "No assistant action here."
            var exportOut = new StringWriter();
            int exportExit = await AssistantVerb.RunAsync(
                ["assistant", "preview", "--area", "export", "--prompt", "export stl", "--store", storeDir, "--json"],
                exportOut,
                creds);
            if (exportExit != 3 || !exportOut.ToString().Contains(AssistantValidators.ExportNoAssistantMessage, StringComparison.Ordinal))
                throw new Exception($"assistant preview --area export expected exit 3 + exact message, got {exportExit}: {exportOut}");

            // 5. assistant audit --limit 10 --json -> valid JSON, zero key leakage.
            var auditStore = new AssistantSettingsStore(storeDir);
            await auditStore.AppendAuditEntryAsync(MakeAuditEntry(DateTimeOffset.UtcNow, "consented", "shown", null, null));

            var auditOut = new StringWriter();
            int auditExit = await AssistantVerb.RunAsync(["assistant", "audit", "--limit", "10", "--store", storeDir, "--json"], auditOut, creds);
            if (auditExit != 0) throw new Exception($"assistant audit failed ({auditExit}): {auditOut}");
            string auditText = auditOut.ToString();
            AssertNoKey(auditText, canaryKey, "audit");
            using (var doc = JsonDocument.Parse(auditText))
            {
                var entries = doc.RootElement.GetProperty("entries");
                if (entries.GetArrayLength() != 1)
                    throw new Exception($"Expected 1 audit entry, got {entries.GetArrayLength()}");
                if (entries[0].GetProperty("usageInputTokensDisplay").GetString() != "Not recorded")
                    throw new Exception("Null token usage should render as 'Not recorded'.");
            }

            // 6. Dispatch via top-level Cli.RunAsync for unknown subcommand -> exit 2.
            var badOut = new StringWriter();
            int badExit = await Cli.RunAsync(["assistant", "bogus"], badOut);
            if (badExit != 2)
                throw new Exception($"Expected exit 2 on bogus assistant subcommand, got {badExit}: {badOut}");
        }
        finally
        {
            if (File.Exists(excerptFile)) File.Delete(excerptFile);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    private static void AssertNoKey(string text, string canaryKey, string subcommand)
    {
        if (text.Contains(canaryKey, StringComparison.Ordinal))
            throw new Exception($"CLI assistant {subcommand} leaked API key!");
    }

    private static AssistantModelEvalReceipt BuildPassingReceipt(AssistantProviderKind provider, string endpoint, string modelId)
    {
        var items = new List<AssistantEvalItemResult>();
        foreach (var def in AssistantEvalSuite.GetSuiteDefinition())
        {
            if (def.Reserved)
            {
                items.Add(new AssistantEvalItemResult(def.ItemId, def.Title, 0, 0, 0.0, def.Threshold, true, false));
                continue;
            }
            items.Add(new AssistantEvalItemResult(def.ItemId, def.Title, def.Cases.Count, def.Cases.Count, 1.0, def.Threshold, false, true));
        }
        return AssistantEvalSuite.BuildReceipt(provider, endpoint, modelId, DateTimeOffset.UtcNow, items);
    }

    private static AssistantAuditEntry MakeAuditEntry(
        DateTimeOffset utc,
        string consentAction,
        string verdict,
        int? inTok,
        int? outTok) => new(
            RequestId: Guid.NewGuid().ToString("D"),
            TimestampUtc: utc,
            Area: AssistantArea.Setup,
            ProposalKind: AssistantProposalKind.SetupSeed,
            ProviderKind: AssistantProviderKind.Anthropic,
            EndpointOrigin: "https://api.anthropic.com",
            ModelId: "claude-sonnet-4-5",
            PayloadSha256: new string('a', 64),
            PromptUtf8Bytes: 256,
            ConsentAction: consentAction,
            Verdict: verdict,
            UsageInputTokens: inTok,
            UsageOutputTokens: outTok,
            DurationMs: 120,
            ErrorCode: null);

    private sealed class CliTestCredentialStore : ICredentialStore
    {
        private readonly Dictionary<AssistantProviderKind, string> secrets = new();
        public bool IsSupported => true;
        public bool HasKey(AssistantProviderKind providerKind) => secrets.ContainsKey(providerKind);
        public CredentialStoreResult ReadKey(AssistantProviderKind providerKind) =>
            secrets.TryGetValue(providerKind, out string? s)
                ? new(true, s, null, null)
                : new(false, null, "CRED-NOT-FOUND", "No API key stored.");
        public CredentialStoreResult WriteKey(AssistantProviderKind providerKind, string apiKey)
        {
            secrets[providerKind] = apiKey;
            return new(true, null, null, null);
        }
        public CredentialStoreResult DeleteKey(AssistantProviderKind providerKind)
        {
            secrets.Remove(providerKind);
            return new(true, null, null, null);
        }
    }
}
