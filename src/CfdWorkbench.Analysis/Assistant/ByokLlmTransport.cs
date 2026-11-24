using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Analysis.Assistant;

public enum AssistantTransportErrorKind
{
    None,
    InvalidEndpoint,
    RedirectRefused,
    AuthenticationFailed,
    RateLimitedOrQuota,
    Timeout,
    NetworkUnavailable,
    ResponseTooLarge,
    ProviderError,
    MalformedResponse
}

public sealed record EndpointValidationResult(
    bool IsValid,
    string? CanonicalOrigin,
    Uri? BaseUri,
    string? ErrorCode,
    string? Message);

public sealed record LlmTransportRequest(
    AssistantProviderKind ProviderKind,
    string EndpointUrl,
    string ModelId,
    string ApiKey,
    RedactedPromptPayload Payload,
    int PerRequestMaxTokens,
    int TimeoutSeconds);

public sealed record LlmTransportResponse(
    bool Succeeded,
    string? RawText,
    int? UsageInputTokens,
    int? UsageOutputTokens,
    long DurationMs,
    int? HttpStatusCode,
    AssistantTransportErrorKind ErrorKind,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Zero-new-NuGet <c>.NET 10</c> BCL <see cref="HttpClient"/> BYOK LLM wire transport
/// (docs/design/byok-assistants.md §3.3, C2). Supports Anthropic <c>/v1/messages</c> and OpenAI-compatible
/// <c>/v1/chat/completions</c>, disables HTTP redirects (<c>AllowAutoRedirect = false</c>), keeps default OS TLS
/// certificate validation, enforces strict HTTPS / exact loopback endpoint validation, bounds responses at 1 MiB,
/// and defensively scrubs active keys from all error messages.
/// </summary>
public sealed class ByokLlmTransport : IDisposable
{
    public const int MaxResponseBytes = 1_048_576;
    public const string AnthropicApiVersion = "2023-06-01";
    private const int MaxErrorSnippetChars = 240;

    private static readonly Regex HeaderSecretRegex = new(
        @"(?i)(?:x-api-key\s*[:=]\s*[^\s,;""']+|authorization\s*[:=]\s*(?:bearer\s+)?[^\s,;""']+)",
        RegexOptions.CultureInvariant);

    private readonly HttpClient httpClient;
    private readonly bool ownsClient;

    public ByokLlmTransport()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10)
            // SslOptions.RemoteCertificateValidationCallback intentionally left null (default OS TLS validation).
        };
        httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        ownsClient = true;
    }

    internal ByokLlmTransport(HttpMessageHandler customHandler, bool disposeHandler = false)
    {
        ArgumentNullException.ThrowIfNull(customHandler);
        httpClient = new HttpClient(customHandler, disposeHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        ownsClient = true;
    }

    public static EndpointValidationResult ValidateEndpointUri(string? endpointUrl)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-EMPTY",
                "Endpoint refused: enter a single HTTPS endpoint (or exact localhost / 127.0.0.1 / ::1).");
        }

        string trimmed = endpointUrl.Trim();
        foreach (char c in trimmed)
        {
            if (char.IsWhiteSpace(c) || c < 0x20 || c == 0x7F)
            {
                return new EndpointValidationResult(
                    false, null, null, "ASSISTANT-ENDPOINT-INVALID",
                    "Endpoint refused: URL contains whitespace or control characters.");
            }
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-INVALID",
                "Endpoint refused: invalid absolute URL.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-USERINFO",
                "Endpoint refused: userinfo@ credentials in the endpoint URL are not allowed.");
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-QUERY",
                "Endpoint refused: query strings and fragments are not allowed on the configured endpoint.");
        }

        // Advisory #2: use DnsSafeHost so IPv6 literals like http://[::1]:8080 yield "::1" for IPAddress.TryParse.
        string dnsHost = uri.DnsSafeHost;
        if (string.IsNullOrWhiteSpace(dnsHost))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-HOST",
                "Endpoint refused: missing host.");
        }

        bool parsedIp = IPAddress.TryParse(dnsHost, out var ipAddress);
        if (parsedIp && (ipAddress!.Equals(IPAddress.Any) || ipAddress.Equals(IPAddress.IPv6Any)))
        {
            return new EndpointValidationResult(
                false, null, null, "ASSISTANT-ENDPOINT-HOST",
                "Endpoint refused: wildcard bind address (0.0.0.0 / ::) is not allowed.");
        }

        bool isExactLoopback =
            string.Equals(dnsHost, "localhost", StringComparison.OrdinalIgnoreCase) ||
            (parsedIp && IPAddress.IsLoopback(ipAddress!));

        bool isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        bool isHttp = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        if (!isHttps && !(isHttp && isExactLoopback))
        {
            return new EndpointValidationResult(
                false, null, null, isHttp ? "ASSISTANT-ENDPOINT-INSECURE" : "ASSISTANT-ENDPOINT-SCHEME",
                "Endpoint refused: use HTTPS (or exact localhost / 127.0.0.1 / ::1) with no userinfo or lookalike host.");
        }

        string canonicalOrigin = AssistantEvalSuite.CanonicalizeEndpointOrigin(trimmed);
        return new EndpointValidationResult(true, canonicalOrigin, uri, null, null);
    }

    public static string ScrubSecrets(string? text, string? activeApiKey)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var additional = new List<string>();
        if (!string.IsNullOrEmpty(activeApiKey) && activeApiKey.Length >= 4)
        {
            string urlEncoded = Uri.EscapeDataString(activeApiKey);
            if (!string.Equals(urlEncoded, activeApiKey, StringComparison.Ordinal))
                additional.Add(urlEncoded);

            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeApiKey));
            if (b64.Length >= 6)
                additional.Add(b64);
        }

        var ctx = new RedactionContext(activeApiKey, additional, [], [], []);
        (string scrubbed, _) = PromptRedactor.ScrubSecrets(text, ctx);
        scrubbed = HeaderSecretRegex.Replace(scrubbed, "[REDACTED_SECRET]");
        return scrubbed;
    }

    public async Task<LlmTransportResponse> SendAsync(LlmTransportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        var sw = Stopwatch.StartNew();

        var endpoint = ValidateEndpointUri(request.EndpointUrl);
        if (!endpoint.IsValid || endpoint.BaseUri is null)
        {
            return Error(
                sw,
                null,
                AssistantTransportErrorKind.InvalidEndpoint,
                endpoint.ErrorCode ?? "ASSISTANT-ENDPOINT-INVALID",
                endpoint.Message ?? "Invalid endpoint.");
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Error(sw, null, AssistantTransportErrorKind.AuthenticationFailed, "ASSISTANT-NO-KEY", "No API key configured.");
        }

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            return Error(sw, null, AssistantTransportErrorKind.InvalidEndpoint, "ASSISTANT-MODEL", "Model identifier is required.");
        }

        Uri targetUri = BuildTargetUri(endpoint.BaseUri, request.ProviderKind);
        byte[] bodyBytes = BuildRequestBody(request);

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, targetUri);
        httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (request.ProviderKind == AssistantProviderKind.Anthropic)
        {
            httpReq.Headers.TryAddWithoutValidation("x-api-key", request.ApiKey);
            httpReq.Headers.TryAddWithoutValidation("anthropic-version", AnthropicApiVersion);
        }
        else
        {
            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        }

        httpReq.Content = new ByteArrayContent(bodyBytes);
        httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        int timeoutSec = Math.Clamp(request.TimeoutSeconds, 1, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        try
        {
            using var response = await httpClient
                .SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);

            int statusCode = (int)response.StatusCode;

            // C2: 3xx redirect refusal — AllowAutoRedirect = false guarantees the API key is never forwarded.
            if (statusCode is >= 300 and <= 399)
            {
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.RedirectRefused,
                    "ASSISTANT-REDIRECT-REFUSED",
                    Labels.AssistantRedirectRefused);
            }

            if (response.Content.Headers.ContentLength is { } contentLen && contentLen > MaxResponseBytes)
            {
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.ResponseTooLarge,
                    "ASSISTANT-RESPONSE-TOO-LARGE",
                    $"Provider response exceeded the {MaxResponseBytes} byte limit.");
            }

            var (bodyOk, responseText) = await ReadBoundedUtf8Async(response.Content, MaxResponseBytes, timeoutCts.Token).ConfigureAwait(false);
            if (!bodyOk)
            {
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.ResponseTooLarge,
                    "ASSISTANT-RESPONSE-TOO-LARGE",
                    $"Provider response exceeded the {MaxResponseBytes} byte limit.");
            }

            if (statusCode is 401 or 403)
            {
                string snippet = ScrubAndBoundSnippet(responseText, request.ApiKey);
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.AuthenticationFailed,
                    "ASSISTANT-AUTH-FAILED",
                    $"Authentication failed (HTTP {statusCode}): {snippet}");
            }

            if (statusCode == 429)
            {
                string snippet = ScrubAndBoundSnippet(responseText, request.ApiKey);
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.RateLimitedOrQuota,
                    "ASSISTANT-RATE-LIMITED",
                    $"Rate limit or quota exceeded (HTTP 429): {snippet}");
            }

            if (statusCode < 200 || statusCode >= 300)
            {
                string snippet = ScrubAndBoundSnippet(responseText, request.ApiKey);
                return Error(
                    sw,
                    statusCode,
                    AssistantTransportErrorKind.ProviderError,
                    $"ASSISTANT-HTTP-{statusCode}",
                    $"Provider returned HTTP {statusCode}: {snippet}");
            }

            return ParseProviderResponse(request.ProviderKind, responseText, request.ApiKey, statusCode, sw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error(
                sw,
                null,
                AssistantTransportErrorKind.Timeout,
                "ASSISTANT-TIMEOUT",
                $"Request timed out after {timeoutSec} s. Manual workflow is unaffected.");
        }
        catch (HttpRequestException ex)
        {
            return Error(
                sw,
                ex.StatusCode is { } sc ? (int)sc : null,
                AssistantTransportErrorKind.NetworkUnavailable,
                "ASSISTANT-NETWORK",
                ScrubSecrets(ex.Message, request.ApiKey));
        }
    }

    public void Dispose()
    {
        if (ownsClient)
            httpClient.Dispose();
    }

    private static Uri BuildTargetUri(Uri baseUri, AssistantProviderKind providerKind)
    {
        string expectedSuffix = providerKind == AssistantProviderKind.Anthropic
            ? "/v1/messages"
            : "/v1/chat/completions";

        string raw = baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        if (raw.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
            return new Uri(raw, UriKind.Absolute);

        if (providerKind == AssistantProviderKind.OpenAiCompatible &&
            raw.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(raw + "/chat/completions", UriKind.Absolute);
        }

        if (providerKind == AssistantProviderKind.Anthropic &&
            raw.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(raw + "/messages", UriKind.Absolute);
        }

        return new Uri(raw + expectedSuffix, UriKind.Absolute);
    }

    private static byte[] BuildRequestBody(LlmTransportRequest request)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("model", request.ModelId.Trim());
            writer.WriteNumber("max_tokens", request.PerRequestMaxTokens);

            if (request.ProviderKind == AssistantProviderKind.Anthropic)
            {
                writer.WriteString("system", request.Payload.SystemPrompt);
                writer.WriteStartArray("messages");
                writer.WriteStartObject();
                writer.WriteString("role", "user");
                writer.WriteString("content", request.Payload.UserPrompt);
                writer.WriteEndObject();
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartObject("response_format");
                writer.WriteString("type", "json_object");
                writer.WriteEndObject();
                writer.WriteStartArray("messages");
                writer.WriteStartObject();
                writer.WriteString("role", "system");
                writer.WriteString("content", request.Payload.SystemPrompt);
                writer.WriteEndObject();
                writer.WriteStartObject();
                writer.WriteString("role", "user");
                writer.WriteString("content", request.Payload.UserPrompt);
                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    private static async Task<(bool WithinLimit, string Text)> ReadBoundedUtf8Async(
        HttpContent content,
        int maxBytes,
        CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var ms = new MemoryStream();
        byte[] chunk = new byte[16_384];
        int total = 0;

        while (true)
        {
            int toRead = Math.Min(chunk.Length, maxBytes - total + 1);
            int read = await stream.ReadAsync(chunk.AsMemory(0, toRead), ct).ConfigureAwait(false);
            if (read == 0)
                break;

            total += read;
            if (total > maxBytes)
                return (false, "");

            ms.Write(chunk, 0, read);
        }

        return (true, Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
    }

    private static LlmTransportResponse ParseProviderResponse(
        AssistantProviderKind providerKind,
        string responseText,
        string activeApiKey,
        int statusCode,
        Stopwatch sw)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText, new JsonDocumentOptions { MaxDepth = 32 });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Error(sw, statusCode, AssistantTransportErrorKind.MalformedResponse, "ASSISTANT-MALFORMED-RESPONSE", "Provider response root was not a JSON object.");
            }

            if (providerKind == AssistantProviderKind.Anthropic)
            {
                if (!root.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.Array)
                {
                    return Error(sw, statusCode, AssistantTransportErrorKind.MalformedResponse, "ASSISTANT-MALFORMED-RESPONSE", "Anthropic response missing content array.");
                }

                var sb = new StringBuilder();
                foreach (var block in contentEl.EnumerateArray())
                {
                    if (block.ValueKind == JsonValueKind.Object &&
                        block.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String &&
                        string.Equals(typeEl.GetString(), "text", StringComparison.Ordinal) &&
                        block.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(textEl.GetString());
                    }
                }

                int? inputTokens = null;
                int? outputTokens = null;
                if (root.TryGetProperty("usage", out var usageEl) && usageEl.ValueKind == JsonValueKind.Object)
                {
                    if (usageEl.TryGetProperty("input_tokens", out var inEl) && inEl.ValueKind == JsonValueKind.Number && inEl.TryGetInt32(out int inVal))
                        inputTokens = inVal;
                    if (usageEl.TryGetProperty("output_tokens", out var outEl) && outEl.ValueKind == JsonValueKind.Number && outEl.TryGetInt32(out int outVal))
                        outputTokens = outVal;
                }

                return new LlmTransportResponse(
                    Succeeded: true,
                    RawText: sb.ToString(),
                    UsageInputTokens: inputTokens,
                    UsageOutputTokens: outputTokens,
                    DurationMs: sw.ElapsedMilliseconds,
                    HttpStatusCode: statusCode,
                    ErrorKind: AssistantTransportErrorKind.None,
                    ErrorCode: null,
                    ErrorMessage: null);
            }
            else
            {
                if (!root.TryGetProperty("choices", out var choicesEl) ||
                    choicesEl.ValueKind != JsonValueKind.Array ||
                    choicesEl.GetArrayLength() == 0)
                {
                    return Error(sw, statusCode, AssistantTransportErrorKind.MalformedResponse, "ASSISTANT-MALFORMED-RESPONSE", "OpenAI-compatible response missing choices.");
                }

                var firstChoice = choicesEl[0];
                if (!firstChoice.TryGetProperty("message", out var msgEl) ||
                    msgEl.ValueKind != JsonValueKind.Object ||
                    !msgEl.TryGetProperty("content", out var contentEl) ||
                    contentEl.ValueKind != JsonValueKind.String)
                {
                    return Error(sw, statusCode, AssistantTransportErrorKind.MalformedResponse, "ASSISTANT-MALFORMED-RESPONSE", "OpenAI-compatible response missing choices[0].message.content.");
                }

                int? promptTokens = null;
                int? completionTokens = null;
                if (root.TryGetProperty("usage", out var usageEl) && usageEl.ValueKind == JsonValueKind.Object)
                {
                    if (usageEl.TryGetProperty("prompt_tokens", out var inEl) && inEl.ValueKind == JsonValueKind.Number && inEl.TryGetInt32(out int inVal))
                        promptTokens = inVal;
                    if (usageEl.TryGetProperty("completion_tokens", out var outEl) && outEl.ValueKind == JsonValueKind.Number && outEl.TryGetInt32(out int outVal))
                        completionTokens = outVal;
                }

                return new LlmTransportResponse(
                    Succeeded: true,
                    RawText: contentEl.GetString() ?? "",
                    UsageInputTokens: promptTokens,
                    UsageOutputTokens: completionTokens,
                    DurationMs: sw.ElapsedMilliseconds,
                    HttpStatusCode: statusCode,
                    ErrorKind: AssistantTransportErrorKind.None,
                    ErrorCode: null,
                    ErrorMessage: null);
            }
        }
        catch (JsonException ex)
        {
            return Error(
                sw,
                statusCode,
                AssistantTransportErrorKind.MalformedResponse,
                "ASSISTANT-MALFORMED-RESPONSE",
                ScrubSecrets(ex.Message, activeApiKey));
        }
    }

    private static string ScrubAndBoundSnippet(string responseText, string activeApiKey)
    {
        string scrubbed = ScrubSecrets(responseText, activeApiKey)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        if (scrubbed.Length > MaxErrorSnippetChars)
            scrubbed = scrubbed[..MaxErrorSnippetChars] + "...";
        return string.IsNullOrEmpty(scrubbed) ? "No details returned." : scrubbed;
    }

    private static LlmTransportResponse Error(
        Stopwatch sw,
        int? statusCode,
        AssistantTransportErrorKind kind,
        string code,
        string message) => new(
            Succeeded: false,
            RawText: null,
            UsageInputTokens: null,
            UsageOutputTokens: null,
            DurationMs: sw.ElapsedMilliseconds,
            HttpStatusCode: statusCode,
            ErrorKind: kind,
            ErrorCode: code,
            ErrorMessage: message);
}
