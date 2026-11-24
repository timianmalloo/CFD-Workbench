using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Persistence.Assistant;

/// <summary>
/// Closed-DTO installation-local persistence for <c>assistant/assistant.json</c> (<c>cfdw-assistant</c> v1, max 64 KiB)
/// and delete-sharing-safe append-only <c>assistant/assistant-audit.jsonl</c> (docs/design/byok-assistants.md §3.2, C3).
/// Enforces private directory ACLs (<see cref="WindowsNative.EnsurePrivateDirectory"/> / POSIX <c>0700</c>),
/// symlink/reparse refusal, fail-closed <c>never-write</c> on <c>version &gt; 1</c>, and strict rejection of any
/// secret/API-key property on parse and write.
/// </summary>
public sealed class AssistantSettingsStore : IAssistantSettingsStore
{
    public const int MaxSettingsBytes = 65_536;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonDocumentOptions StrictJsonOptions = new()
    {
        MaxDepth = 8,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly HashSet<string> ForbiddenSecretPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "apiKey",
        "api_key",
        "api-key",
        "secret",
        "secretKey",
        "secret_key",
        "token",
        "accessToken",
        "access_token",
        "authorization",
        "password",
        "credential",
        "credentials",
        "bearer"
    };

    private static readonly HashSet<string> AllowedRootProperties = new(StringComparer.Ordinal)
    {
        "format",
        "version",
        "providerKind",
        "endpointUrl",
        "modelId",
        "perRequestMaxTokens",
        "dailyRequestCap",
        "timeoutSeconds",
        "evalReceipts"
    };

    private static readonly HashSet<string> AllowedReceiptProperties = new(StringComparer.Ordinal)
    {
        "receiptId",
        "providerKind",
        "endpointOrigin",
        "modelId",
        "evalThresholdsVersion",
        "evaluatedAtUtc",
        "allActiveItemsMetThreshold",
        "items",
        "contentHash"
    };

    private static readonly HashSet<string> AllowedEvalItemProperties = new(StringComparer.Ordinal)
    {
        "itemId",
        "title",
        "passedCases",
        "totalCases",
        "passRate",
        "threshold",
        "reserved",
        "metThreshold"
    };

    private readonly Func<string, bool> ownerProbe;
    private readonly SemaphoreSlim settingsGate = new(1, 1);
    private readonly SemaphoreSlim auditGate = new(1, 1);
    private bool neverWrite;
    private string? neverWriteCode;
    private bool sessionOnly;

    public AssistantSettingsStore(string root)
        : this(root, WindowsOwner)
    {
    }

    internal AssistantSettingsStore(string root, Func<string, bool> ownerProbe)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root directory is required.", nameof(root));
        Root = Path.GetFullPath(root);
        this.ownerProbe = ownerProbe ?? WindowsOwner;
    }

    public string Root { get; }
    public string AssistantDirectory => Path.Combine(Root, "assistant");
    public string SettingsPath => Path.Combine(AssistantDirectory, "assistant.json");
    public string AuditPath => Path.Combine(AssistantDirectory, "assistant-audit.jsonl");

    public async Task<AssistantSettingsLoadResult> LoadSettingsAsync(CancellationToken ct = default)
    {
        await settingsGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await LoadSettingsCoreAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            neverWrite = true;
            neverWriteCode = "ASSISTANT-IO";
            return new AssistantSettingsLoadResult(
                AssistantSettings.CreateDefault(),
                "never-write",
                ["ASSISTANT-IO"],
                NeverWrite: true,
                SessionOnly: true);
        }
        finally
        {
            settingsGate.Release();
        }
    }

    public async Task<AssistantSettingsSaveResult> SaveSettingsAsync(AssistantSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!TryValidateSettingsForWrite(settings, out string? valCode, out string? valMsg))
            return new AssistantSettingsSaveResult(false, "rejected", valCode, valMsg);

        await settingsGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsLinked(Root) || IsLinked(AssistantDirectory) || IsLinked(SettingsPath) || sessionOnly)
            {
                sessionOnly = true;
                return new AssistantSettingsSaveResult(false, "session-only", "ASSISTANT-SYMLINK-REFUSED", "Refusing symlink or untrusted settings directory.");
            }

            if (Directory.Exists(AssistantDirectory) && !SafeOwnerProbe(AssistantDirectory))
            {
                sessionOnly = true;
                return new AssistantSettingsSaveResult(false, "session-only", "ASSISTANT-OWNER-REFUSED", "Settings directory is not owned by the current user.");
            }

            if (neverWrite)
                return new AssistantSettingsSaveResult(false, "never-write", neverWriteCode ?? "ASSISTANT-VERSION", "Existing assistant.json is protected (never-write).");

            // Even if LoadSettingsAsync was not called yet, inspect an existing file so version > 1 or secret files are never clobbered.
            if (File.Exists(SettingsPath))
            {
                var existing = await InspectExistingSettingsFileAsync(ct).ConfigureAwait(false);
                if (existing.NeverWrite)
                {
                    neverWrite = true;
                    neverWriteCode = existing.Codes.Count > 0 ? existing.Codes[0] : "ASSISTANT-VERSION";
                    return new AssistantSettingsSaveResult(false, "never-write", neverWriteCode, "Existing assistant.json is protected (never-write).");
                }
            }

            if (!EnsurePrivateDirectory(AssistantDirectory))
            {
                sessionOnly = true;
                return new AssistantSettingsSaveResult(false, "session-only", "ASSISTANT-OWNER-REFUSED", "Could not enforce private directory permissions.");
            }

            byte[] serialized = SerializeSettings(settings);
            if (serialized.Length > MaxSettingsBytes)
                return new AssistantSettingsSaveResult(false, "rejected", "ASSISTANT-SIZE", "Serialized assistant settings exceed the 64 KiB cap.");

            // Defense in depth: verify serialized bytes contain zero forbidden secret keys before writing to disk.
            if (!VerifySerializedNoSecrets(serialized))
                return new AssistantSettingsSaveResult(false, "rejected", "ASSISTANT-SECRET-FORBIDDEN", "Secret fields are forbidden in assistant.json.");

            string tempPath = Path.Combine(AssistantDirectory, $"assistant.json.{Guid.NewGuid():N}.tmp");
            try
            {
                await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await fs.WriteAsync(serialized, ct).ConfigureAwait(false);
                    await fs.FlushAsync(ct).ConfigureAwait(false);
                    fs.Flush(flushToDisk: true);
                }

                if (IsLinked(SettingsPath))
                {
                    sessionOnly = true;
                    return new AssistantSettingsSaveResult(false, "session-only", "ASSISTANT-SYMLINK-REFUSED", "Refusing symlinked assistant.json.");
                }

                File.Move(tempPath, SettingsPath, overwrite: true);
                return new AssistantSettingsSaveResult(true, "saved", null, null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                TryDeleteQuietly(tempPath);
                return new AssistantSettingsSaveResult(false, "failed", "ASSISTANT-IO", "Failed to save assistant.json.");
            }
            catch
            {
                TryDeleteQuietly(tempPath);
                throw;
            }
        }
        finally
        {
            settingsGate.Release();
        }
    }

    public async Task<bool> AppendAuditEntryAsync(AssistantAuditEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await auditGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsLinked(Root) || IsLinked(AssistantDirectory) || IsLinked(AuditPath))
                return false;

            if (Directory.Exists(AssistantDirectory) && !SafeOwnerProbe(AssistantDirectory))
                return false;

            if (!EnsurePrivateDirectory(AssistantDirectory))
                return false;

            if (IsLinked(AuditPath))
                return false;

            byte[] lineBytes = SerializeAuditLine(entry);
            await using var fs = new FileStream(
                AuditPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            await fs.WriteAsync(lineBytes, ct).ConfigureAwait(false);
            await fs.FlushAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            auditGate.Release();
        }
    }

    public async Task<IReadOnlyList<AssistantAuditEntry>> ReadAuditEntriesAsync(int maxEntries = 200, CancellationToken ct = default)
    {
        if (maxEntries <= 0)
            return [];

        if (UnsafeForRead(Root) || UnsafeForRead(AssistantDirectory) || IsLinked(AuditPath) || !File.Exists(AuditPath))
            return [];

        var all = new List<AssistantAuditEntry>();
        try
        {
            await using var stream = OpenAuditRead(AuditPath);
            using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (TryParseAuditLine(line, out var entry))
                    all.Add(entry!);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        if (all.Count <= maxEntries)
            return all;

        return all.GetRange(all.Count - maxEntries, maxEntries);
    }

    public async Task<int> CountDailyRequestsUtcAsync(DateOnly utcDate, CancellationToken ct = default)
    {
        if (UnsafeForRead(Root) || UnsafeForRead(AssistantDirectory) || IsLinked(AuditPath) || !File.Exists(AuditPath))
            return 0;

        int count = 0;
        try
        {
            await using var stream = OpenAuditRead(AuditPath);
            using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (!TryParseAuditLine(line, out var entry))
                    continue;

                // Count every consented request on this UTC date even when provider token usage is null ("Not recorded").
                if (DateOnly.FromDateTime(entry!.TimestampUtc.UtcDateTime) == utcDate &&
                    string.Equals(entry.ConsentAction, "consented", StringComparison.Ordinal))
                {
                    count++;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }

        return count;
    }

    private async Task<AssistantSettingsLoadResult> LoadSettingsCoreAsync(CancellationToken ct)
    {
        if (IsLinked(Root) || IsLinked(AssistantDirectory) || IsLinked(SettingsPath))
        {
            sessionOnly = true;
            return new AssistantSettingsLoadResult(
                AssistantSettings.CreateDefault(),
                "session-only",
                ["ASSISTANT-SYMLINK-REFUSED"],
                NeverWrite: true,
                SessionOnly: true);
        }

        if ((Directory.Exists(Root) && !SafeOwnerProbe(Root)) ||
            (Directory.Exists(AssistantDirectory) && !SafeOwnerProbe(AssistantDirectory)))
        {
            sessionOnly = true;
            return new AssistantSettingsLoadResult(
                AssistantSettings.CreateDefault(),
                "session-only",
                ["ASSISTANT-OWNER-REFUSED"],
                NeverWrite: true,
                SessionOnly: true);
        }

        if (!File.Exists(SettingsPath))
            return new AssistantSettingsLoadResult(AssistantSettings.CreateDefault(), "absent", [], NeverWrite: false, SessionOnly: false);

        var parsed = await InspectExistingSettingsFileAsync(ct).ConfigureAwait(false);
        if (parsed.NeverWrite)
        {
            neverWrite = true;
            neverWriteCode = parsed.Codes.Count > 0 ? parsed.Codes[0] : "ASSISTANT-SCHEMA";
        }
        return parsed;
    }

    private async Task<AssistantSettingsLoadResult> InspectExistingSettingsFileAsync(CancellationToken ct)
    {
        byte[] bytes;
        try
        {
            bytes = await ReadBoundedUserFileAsync(SettingsPath, MaxSettingsBytes, ct).ConfigureAwait(false);
        }
        catch (OversizeFileException)
        {
            return new AssistantSettingsLoadResult(AssistantSettings.CreateDefault(), "rejected", ["ASSISTANT-SCHEMA"], NeverWrite: true, SessionOnly: true);
        }

        return ParseSettingsBytes(bytes);
    }

    internal static AssistantSettingsLoadResult ParseSettingsBytes(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxSettingsBytes)
            return Rejected("ASSISTANT-SCHEMA");

        // Reject UTF-8 BOM.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Rejected("ASSISTANT-SCHEMA");

        try
        {
            using var doc = JsonDocument.Parse(bytes, StrictJsonOptions);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Rejected("ASSISTANT-SCHEMA");

            // 1. Recursive secret-property check (strictly forbid apiKey/secret/token/password/authorization anywhere).
            if (ContainsForbiddenSecretProperty(root))
                return Rejected("ASSISTANT-SECRET-FORBIDDEN");

            // 2. Duplicate property name check across the entire JSON tree.
            if (ContainsDuplicateProperty(root))
                return Rejected("ASSISTANT-SCHEMA");

            // 3. Version-first peek: format == "cfdw-assistant" and version > 1 -> fail-closed never-write.
            if (root.TryGetProperty("format", out var fmtEl) && fmtEl.ValueKind == JsonValueKind.String &&
                string.Equals(fmtEl.GetString(), AssistantSettings.FormatId, StringComparison.Ordinal) &&
                root.TryGetProperty("version", out var verEl) && verEl.ValueKind == JsonValueKind.Number &&
                verEl.TryGetInt32(out int ver) && ver > AssistantSettings.CurrentVersion)
            {
                return new AssistantSettingsLoadResult(
                    AssistantSettings.CreateDefault(),
                    "never-write",
                    ["ASSISTANT-VERSION"],
                    NeverWrite: true,
                    SessionOnly: true);
            }

            // 4. Closed-DTO v1 validation.
            foreach (var prop in root.EnumerateObject())
            {
                if (!AllowedRootProperties.Contains(prop.Name))
                    return Rejected("ASSISTANT-SCHEMA");
            }

            if (!TryGetString(root, "format", out string format) || format != AssistantSettings.FormatId)
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetInt(root, "version", out int version) || version != AssistantSettings.CurrentVersion)
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetString(root, "providerKind", out string providerStr) ||
                !TryParseProviderKind(providerStr, out var providerKind))
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetString(root, "endpointUrl", out string endpointUrl) || string.IsNullOrWhiteSpace(endpointUrl))
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetString(root, "modelId", out string modelId) || string.IsNullOrWhiteSpace(modelId) || modelId.Length > 256)
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetInt(root, "perRequestMaxTokens", out int maxTokens) ||
                maxTokens < AssistantSettings.MinPerRequestMaxTokens || maxTokens > AssistantSettings.MaxPerRequestMaxTokens)
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetInt(root, "dailyRequestCap", out int dailyCap) ||
                dailyCap < AssistantSettings.MinDailyRequestCap || dailyCap > AssistantSettings.MaxDailyRequestCap)
                return Rejected("ASSISTANT-SCHEMA");
            if (!TryGetInt(root, "timeoutSeconds", out int timeoutSeconds) ||
                timeoutSeconds < AssistantSettings.MinTimeoutSeconds || timeoutSeconds > AssistantSettings.MaxTimeoutSeconds)
                return Rejected("ASSISTANT-SCHEMA");

            if (!root.TryGetProperty("evalReceipts", out var receiptsEl) || receiptsEl.ValueKind != JsonValueKind.Array)
                return Rejected("ASSISTANT-SCHEMA");

            var receipts = new List<AssistantModelEvalReceipt>();
            foreach (var recEl in receiptsEl.EnumerateArray())
            {
                if (!TryParseEvalReceipt(recEl, out var receipt))
                    return Rejected("ASSISTANT-SCHEMA");
                receipts.Add(receipt!);
            }

            var settings = new AssistantSettings(
                Format: format,
                Version: version,
                ProviderKind: providerKind,
                EndpointUrl: endpointUrl.Trim(),
                ModelId: modelId.Trim(),
                PerRequestMaxTokens: maxTokens,
                DailyRequestCap: dailyCap,
                TimeoutSeconds: timeoutSeconds,
                EvalReceipts: receipts);

            return new AssistantSettingsLoadResult(settings, "restored", [], NeverWrite: false, SessionOnly: false);
        }
        catch (JsonException)
        {
            return Rejected("ASSISTANT-SCHEMA");
        }
    }

    private static AssistantSettingsLoadResult Rejected(string code) => new(
        AssistantSettings.CreateDefault(),
        "rejected",
        [code],
        NeverWrite: true,
        SessionOnly: true);

    private static bool TryValidateSettingsForWrite(AssistantSettings settings, out string? code, out string? message)
    {
        if (settings.Format != AssistantSettings.FormatId || settings.Version != AssistantSettings.CurrentVersion)
        {
            code = "ASSISTANT-SCHEMA";
            message = "Settings format must be cfdw-assistant v1.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(settings.EndpointUrl))
        {
            code = "ASSISTANT-ENDPOINT";
            message = "Endpoint URL is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(settings.ModelId) || settings.ModelId.Length > 256 ||
            settings.ModelId.Any(c => c < 0x20 || c == 0x7F))
        {
            code = "ASSISTANT-MODEL";
            message = "Model identifier must be non-empty and free of control characters.";
            return false;
        }
        if (settings.PerRequestMaxTokens < AssistantSettings.MinPerRequestMaxTokens ||
            settings.PerRequestMaxTokens > AssistantSettings.MaxPerRequestMaxTokens)
        {
            code = "ASSISTANT-CAP";
            message = "Per-request max tokens must be within [128, 16384].";
            return false;
        }
        if (settings.DailyRequestCap < AssistantSettings.MinDailyRequestCap ||
            settings.DailyRequestCap > AssistantSettings.MaxDailyRequestCap)
        {
            code = "ASSISTANT-CAP";
            message = "Daily UTC request cap must be within [1, 500].";
            return false;
        }
        if (settings.TimeoutSeconds < AssistantSettings.MinTimeoutSeconds ||
            settings.TimeoutSeconds > AssistantSettings.MaxTimeoutSeconds)
        {
            code = "ASSISTANT-TIMEOUT";
            message = "Timeout must be within [5, 120] seconds.";
            return false;
        }

        code = null;
        message = null;
        return true;
    }

    private static byte[] SerializeSettings(AssistantSettings settings)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", AssistantSettings.FormatId);
            writer.WriteNumber("version", AssistantSettings.CurrentVersion);
            writer.WriteString("providerKind", settings.ProviderKind.ToString());
            writer.WriteString("endpointUrl", settings.EndpointUrl.Trim());
            writer.WriteString("modelId", settings.ModelId.Trim());
            writer.WriteNumber("perRequestMaxTokens", settings.PerRequestMaxTokens);
            writer.WriteNumber("dailyRequestCap", settings.DailyRequestCap);
            writer.WriteNumber("timeoutSeconds", settings.TimeoutSeconds);
            writer.WriteStartArray("evalReceipts");
            if (settings.EvalReceipts is not null)
            {
                foreach (var r in settings.EvalReceipts)
                {
                    writer.WriteStartObject();
                    writer.WriteString("receiptId", r.ReceiptId);
                    writer.WriteString("providerKind", r.ProviderKind.ToString());
                    writer.WriteString("endpointOrigin", r.EndpointOrigin);
                    writer.WriteString("modelId", r.ModelId);
                    writer.WriteString("evalThresholdsVersion", r.EvalThresholdsVersion);
                    writer.WriteString("evaluatedAtUtc", r.EvaluatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                    writer.WriteBoolean("allActiveItemsMetThreshold", r.AllActiveItemsMetThreshold);
                    writer.WriteStartArray("items");
                    foreach (var item in r.Items)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("itemId", item.ItemId);
                        writer.WriteString("title", item.Title);
                        writer.WriteNumber("passedCases", item.PassedCases);
                        writer.WriteNumber("totalCases", item.TotalCases);
                        writer.WriteNumber("passRate", item.PassRate);
                        writer.WriteNumber("threshold", item.Threshold);
                        writer.WriteBoolean("reserved", item.Reserved);
                        writer.WriteBoolean("metThreshold", item.MetThreshold);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteString("contentHash", r.ContentHash);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    private static bool VerifySerializedNoSecrets(byte[] serialized)
    {
        using var doc = JsonDocument.Parse(serialized, StrictJsonOptions);
        return !ContainsForbiddenSecretProperty(doc.RootElement);
    }

    private static byte[] SerializeAuditLine(AssistantAuditEntry entry)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("requestId", entry.RequestId);
            writer.WriteString("timestampUtc", entry.TimestampUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("area", entry.Area.ToString());
            writer.WriteString("proposalKind", entry.ProposalKind.ToString());
            writer.WriteString("providerKind", entry.ProviderKind.ToString());
            writer.WriteString("endpointOrigin", entry.EndpointOrigin);
            writer.WriteString("modelId", entry.ModelId);
            writer.WriteString("payloadSha256", entry.PayloadSha256);
            writer.WriteNumber("promptUtf8Bytes", entry.PromptUtf8Bytes);
            writer.WriteString("consentAction", entry.ConsentAction);
            writer.WriteString("verdict", entry.Verdict);
            if (entry.UsageInputTokens is { } inTok) writer.WriteNumber("usageInputTokens", inTok);
            else writer.WriteNull("usageInputTokens");
            if (entry.UsageOutputTokens is { } outTok) writer.WriteNumber("usageOutputTokens", outTok);
            else writer.WriteNull("usageOutputTokens");
            if (entry.DurationMs is { } durMs) writer.WriteNumber("durationMs", durMs);
            else writer.WriteNull("durationMs");
            if (entry.ErrorCode is not null) writer.WriteString("errorCode", entry.ErrorCode);
            else writer.WriteNull("errorCode");
            writer.WriteEndObject();
        }
        var span = buffer.GetSpan(1);
        span[0] = (byte)'\n';
        buffer.Advance(1);
        return buffer.WrittenSpan.ToArray();
    }

    private static bool TryParseAuditLine(string line, out AssistantAuditEntry? entry)
    {
        entry = null;
        try
        {
            using var doc = JsonDocument.Parse(line, StrictJsonOptions);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;
            if (ContainsForbiddenSecretProperty(root))
                return false;

            if (!TryGetString(root, "requestId", out string requestId)) return false;
            if (!TryGetString(root, "timestampUtc", out string tsStr) ||
                !DateTimeOffset.TryParse(tsStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
                return false;
            if (!TryGetString(root, "area", out string areaStr) || !Enum.TryParse<AssistantArea>(areaStr, ignoreCase: false, out var area))
                return false;
            if (!TryGetString(root, "proposalKind", out string kindStr) || !Enum.TryParse<AssistantProposalKind>(kindStr, ignoreCase: false, out var kind))
                return false;
            if (!TryGetString(root, "providerKind", out string provStr) || !TryParseProviderKind(provStr, out var provider))
                return false;
            if (!TryGetString(root, "endpointOrigin", out string origin)) return false;
            if (!TryGetString(root, "modelId", out string modelId)) return false;
            if (!TryGetString(root, "payloadSha256", out string sha)) return false;
            if (!TryGetInt(root, "promptUtf8Bytes", out int promptBytes)) return false;
            if (!TryGetString(root, "consentAction", out string consentAction)) return false;
            if (!TryGetString(root, "verdict", out string verdict)) return false;

            int? usageIn = TryGetNullableInt(root, "usageInputTokens");
            int? usageOut = TryGetNullableInt(root, "usageOutputTokens");
            long? durationMs = TryGetNullableLong(root, "durationMs");
            string? errorCode = root.TryGetProperty("errorCode", out var errEl) && errEl.ValueKind == JsonValueKind.String
                ? errEl.GetString()
                : null;

            entry = new AssistantAuditEntry(
                RequestId: requestId,
                TimestampUtc: ts.ToUniversalTime(),
                Area: area,
                ProposalKind: kind,
                ProviderKind: provider,
                EndpointOrigin: origin,
                ModelId: modelId,
                PayloadSha256: sha,
                PromptUtf8Bytes: promptBytes,
                ConsentAction: consentAction,
                Verdict: verdict,
                UsageInputTokens: usageIn,
                UsageOutputTokens: usageOut,
                DurationMs: durationMs,
                ErrorCode: errorCode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseEvalReceipt(JsonElement el, out AssistantModelEvalReceipt? receipt)
    {
        receipt = null;
        if (el.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in el.EnumerateObject())
        {
            if (!AllowedReceiptProperties.Contains(p.Name)) return false;
        }

        if (!TryGetString(el, "receiptId", out string receiptId)) return false;
        if (!TryGetString(el, "providerKind", out string provStr) || !TryParseProviderKind(provStr, out var provider)) return false;
        if (!TryGetString(el, "endpointOrigin", out string origin)) return false;
        if (!TryGetString(el, "modelId", out string modelId)) return false;
        if (!TryGetString(el, "evalThresholdsVersion", out string ver)) return false;
        if (!TryGetString(el, "evaluatedAtUtc", out string tsStr) ||
            !DateTimeOffset.TryParse(tsStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var evaluatedAtUtc))
            return false;
        if (!TryGetBool(el, "allActiveItemsMetThreshold", out bool allMet)) return false;
        if (!TryGetString(el, "contentHash", out string contentHash)) return false;
        if (!el.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array) return false;

        var items = new List<AssistantEvalItemResult>();
        foreach (var itemEl in itemsEl.EnumerateArray())
        {
            if (itemEl.ValueKind != JsonValueKind.Object) return false;
            foreach (var p in itemEl.EnumerateObject())
            {
                if (!AllowedEvalItemProperties.Contains(p.Name)) return false;
            }
            if (!TryGetString(itemEl, "itemId", out string itemId)) return false;
            if (!TryGetString(itemEl, "title", out string title)) return false;
            if (!TryGetInt(itemEl, "passedCases", out int passedCases)) return false;
            if (!TryGetInt(itemEl, "totalCases", out int totalCases)) return false;
            if (!TryGetFiniteDouble(itemEl, "passRate", out double passRate)) return false;
            if (!TryGetFiniteDouble(itemEl, "threshold", out double threshold)) return false;
            if (!TryGetBool(itemEl, "reserved", out bool reserved)) return false;
            if (!TryGetBool(itemEl, "metThreshold", out bool metThreshold)) return false;

            items.Add(new AssistantEvalItemResult(itemId, title, passedCases, totalCases, passRate, threshold, reserved, metThreshold));
        }

        receipt = new AssistantModelEvalReceipt(
            ReceiptId: receiptId,
            ProviderKind: provider,
            EndpointOrigin: origin,
            ModelId: modelId,
            EvalThresholdsVersion: ver,
            EvaluatedAtUtc: evaluatedAtUtc.ToUniversalTime(),
            AllActiveItemsMetThreshold: allMet,
            Items: items,
            ContentHash: contentHash);
        return true;
    }

    private static bool ContainsForbiddenSecretProperty(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (ForbiddenSecretPropertyNames.Contains(prop.Name))
                        return true;
                    if (ContainsForbiddenSecretProperty(prop.Value))
                        return true;
                }
                return false;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsForbiddenSecretProperty(item))
                        return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool ContainsDuplicateProperty(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var prop in element.EnumerateObject())
                {
                    if (!names.Add(prop.Name))
                        return true;
                    if (ContainsDuplicateProperty(prop.Value))
                        return true;
                }
                return false;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsDuplicateProperty(item))
                        return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryParseProviderKind(string value, out AssistantProviderKind kind)
    {
        if (string.Equals(value, nameof(AssistantProviderKind.Anthropic), StringComparison.Ordinal))
        {
            kind = AssistantProviderKind.Anthropic;
            return true;
        }
        if (string.Equals(value, nameof(AssistantProviderKind.OpenAiCompatible), StringComparison.Ordinal))
        {
            kind = AssistantProviderKind.OpenAiCompatible;
            return true;
        }
        kind = default;
        return false;
    }

    private static bool TryGetString(JsonElement obj, string name, out string value)
    {
        value = "";
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        value = el.GetString() ?? "";
        return true;
    }

    private static bool TryGetInt(JsonElement obj, string name, out int value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value);
    }

    private static int? TryGetNullableInt(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
            return null;
        return el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v) ? v : null;
    }

    private static long? TryGetNullableLong(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
            return null;
        return el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out long v) ? v : null;
    }

    private static bool TryGetFiniteDouble(JsonElement obj, string name, out double value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number &&
               el.TryGetDouble(out value) && double.IsFinite(value);
    }

    private static bool TryGetBool(JsonElement obj, string name, out bool value)
    {
        value = false;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return false;
        value = el.GetBoolean();
        return true;
    }

    private static async Task<byte[]> ReadBoundedUserFileAsync(string path, int maxBytes, CancellationToken ct)
    {
        await using var stream = UserFile.OpenRead(path, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.CanSeek && stream.Length > maxBytes)
            throw new OversizeFileException();

        byte[] buffer = new byte[maxBytes + 1];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct).ConfigureAwait(false);
            if (read == 0)
                return buffer.AsSpan(0, total).ToArray();
            total += read;
        }
        throw new OversizeFileException();
    }

    private static FileStream OpenAuditRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static bool EnsurePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            string? parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            return WindowsNative.EnsurePrivateDirectory(path);
        }

        Directory.CreateDirectory(path);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return true;
    }

    private bool UnsafeForRead(string path)
    {
        try
        {
            return IsLinked(path) || (Directory.Exists(path) && !ownerProbe(path));
        }
        catch (Exception)
        {
            return true;
        }
    }

    private bool SafeOwnerProbe(string path)
    {
        try { return ownerProbe(path); }
        catch (Exception) { return false; }
    }

    private static bool WindowsOwner(string path) =>
        !OperatingSystem.IsWindows() || WindowsNative.OwnedByCurrentUser(path);

    private static bool IsLinked(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return false;
        try
        {
            return File.ResolveLinkTarget(path, returnFinalTarget: false) is not null;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static void TryDeleteQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private sealed class OversizeFileException : Exception;
}
