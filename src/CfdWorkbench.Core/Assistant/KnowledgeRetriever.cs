using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Core.Assistant;

public sealed record KnowledgeEntry(
    string Id,
    string Title,
    string Area,
    IReadOnlyList<string> Tags,
    string Body);

public sealed record KnowledgeSearchHit(
    KnowledgeEntry Entry,
    double Score);

public sealed record KnowledgeCorpusLoad(
    bool Available,
    string? Reason,
    IReadOnlyList<KnowledgeEntry> Entries,
    string ActualSha256);

/// <summary>
/// Pure zero-I/O tamper-evident BM25 knowledge retriever over embedded <c>CatalogData/KnowledgeCorpus.json</c>
/// (docs/design/byok-assistants.md §2.4, spec A8.5). Fails closed on any 1-bit flip, missing resource, or invalid JSON.
/// </summary>
public sealed class KnowledgeRetriever
{
    public const string ResourceName = "CfdWorkbench.Core.CatalogData.KnowledgeCorpus.json";
    public const string ExpectedCorpusSha256 = "96f0a134fa785682d0bb1fee3d489ca5c7caf0c19baa199e6c722da73f743843";
    public const string UnavailableIntegrityMessage = "Unavailable — bundled knowledge corpus failed integrity check.";

    private const double Bm25K1 = 1.2;
    private const double Bm25B = 0.75;
    private const double AreaBoost = 1.15;

    private static readonly Regex TokenRegex = new(@"[a-z0-9_]{2,}", RegexOptions.CultureInvariant);

    private readonly IndexedDoc[] docs;
    private readonly Dictionary<string, int> documentFrequency;
    private readonly double avgDocLength;

    private KnowledgeRetriever(KnowledgeCorpusLoad load)
    {
        Available = load.Available;
        UnavailableReason = load.Reason;
        ActualSha256 = load.ActualSha256;
        Entries = load.Entries;
        KnownIds = new HashSet<string>(load.Entries.Select(e => e.Id), StringComparer.Ordinal);

        if (!load.Available || load.Entries.Count == 0)
        {
            docs = [];
            documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
            avgDocLength = 0.0;
            return;
        }

        docs = new IndexedDoc[load.Entries.Count];
        documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
        long totalLen = 0;

        for (int i = 0; i < load.Entries.Count; i++)
        {
            var entry = load.Entries[i];
            string fullText = string.Concat(entry.Title, " ", string.Join(' ', entry.Tags), " ", entry.Body);
            var tf = new Dictionary<string, int>(StringComparer.Ordinal);
            int len = 0;
            foreach (string token in Tokenize(fullText))
            {
                len++;
                tf[token] = tf.TryGetValue(token, out int existing) ? existing + 1 : 1;
            }
            foreach (string unique in tf.Keys)
                documentFrequency[unique] = documentFrequency.TryGetValue(unique, out int df) ? df + 1 : 1;

            docs[i] = new IndexedDoc(entry, tf, len);
            totalLen += len;
        }

        avgDocLength = docs.Length > 0 ? (double)totalLen / docs.Length : 1.0;
    }

    public bool Available { get; }
    public string? UnavailableReason { get; }
    public string ActualSha256 { get; }
    public IReadOnlyList<KnowledgeEntry> Entries { get; }
    public IReadOnlySet<string> KnownIds { get; }

    public static KnowledgeRetriever LoadEmbedded()
    {
        using Stream? stream = typeof(KnowledgeRetriever).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
            return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], ""));

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return LoadFromBytes(ms.ToArray(), ExpectedCorpusSha256);
    }

    public static KnowledgeRetriever LoadFromBytes(byte[] rawBytes, string? expectedSha256 = null)
    {
        if (rawBytes is null || rawBytes.Length == 0)
            return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], ""));

        byte[] actualHashBytes = SHA256.HashData(rawBytes);
        string actualHex = Convert.ToHexString(actualHashBytes).ToLowerInvariant();
        string expectedHex = (expectedSha256 ?? ExpectedCorpusSha256).Trim().ToLowerInvariant();

        if (!TryDecodeSha256Hex(expectedHex, out byte[] expectedHashBytes) ||
            !CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes))
        {
            return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBytes, new JsonDocumentOptions
            {
                MaxDepth = 8,
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            });

            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("schemaVersion", out var verEl) || verEl.ValueKind != JsonValueKind.Number || verEl.GetInt32() != 1 ||
                !root.TryGetProperty("entries", out var entriesEl) || entriesEl.ValueKind != JsonValueKind.Array)
            {
                return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
            }

            var entries = new List<KnowledgeEntry>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in entriesEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
                string? id = item.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : null;
                string? title = item.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String ? tEl.GetString() : null;
                string? area = item.TryGetProperty("area", out var aEl) && aEl.ValueKind == JsonValueKind.String ? aEl.GetString() : null;
                string? body = item.TryGetProperty("body", out var bEl) && bEl.ValueKind == JsonValueKind.String ? bEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || !seenIds.Add(id) ||
                    string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(body) ||
                    !item.TryGetProperty("tags", out var tagsEl) || tagsEl.ValueKind != JsonValueKind.Array)
                {
                    return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
                }

                var tags = new List<string>();
                foreach (var tagItem in tagsEl.EnumerateArray())
                {
                    if (tagItem.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(tagItem.GetString()))
                        return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
                    tags.Add(tagItem.GetString()!.Trim());
                }

                entries.Add(new KnowledgeEntry(id.Trim(), title.Trim(), area.Trim().ToLowerInvariant(), tags, body.Trim()));
            }

            if (entries.Count == 0)
                return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));

            return new KnowledgeRetriever(new KnowledgeCorpusLoad(true, null, entries, actualHex));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            return new KnowledgeRetriever(new KnowledgeCorpusLoad(false, UnavailableIntegrityMessage, [], actualHex));
        }
    }

    public static string ComputeCorpusSha256(byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);
        return Convert.ToHexString(SHA256.HashData(rawBytes)).ToLowerInvariant();
    }

    public IReadOnlyList<KnowledgeSearchHit> Search(string query, int maxResults = 3, AssistantArea? preferredArea = null)
    {
        if (!Available || docs.Length == 0 || maxResults <= 0 || string.IsNullOrWhiteSpace(query))
            return [];

        string[] queryTerms = Tokenize(query).Distinct(StringComparer.Ordinal).ToArray();
        if (queryTerms.Length == 0)
            return [];

        string? areaKey = preferredArea is { } a ? AreaToCorpusKey(a) : null;
        int n = docs.Length;
        var scored = new List<KnowledgeSearchHit>(n);

        for (int i = 0; i < n; i++)
        {
            var d = docs[i];
            double score = 0.0;
            foreach (string term in queryTerms)
            {
                if (!d.TermFreq.TryGetValue(term, out int tf) || tf <= 0)
                    continue;
                int df = documentFrequency.TryGetValue(term, out int dfVal) ? dfVal : 0;
                double idf = Math.Log(1.0 + (n - df + 0.5) / (df + 0.5));
                double denom = tf + Bm25K1 * (1.0 - Bm25B + Bm25B * (d.Length / avgDocLength));
                score += idf * (tf * (Bm25K1 + 1.0)) / denom;
            }

            if (score <= 0.0)
                continue;

            if (areaKey is not null && string.Equals(d.Entry.Area, areaKey, StringComparison.Ordinal))
                score *= AreaBoost;

            scored.Add(new KnowledgeSearchHit(d.Entry, score));
        }

        scored.Sort(static (left, right) =>
        {
            int cmp = right.Score.CompareTo(left.Score);
            return cmp != 0 ? cmp : StringComparer.Ordinal.Compare(left.Entry.Id, right.Entry.Id);
        });

        return scored.Count <= maxResults ? scored : scored.GetRange(0, maxResults);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        string lower = text.ToLowerInvariant();
        foreach (Match m in TokenRegex.Matches(lower))
            yield return m.Value;
    }

    private static string? AreaToCorpusKey(AssistantArea area) => area switch
    {
        AssistantArea.Setup => "setup",
        AssistantArea.Cad => "cad",
        AssistantArea.Analysis => "analysis",
        AssistantArea.Experiment => "experiment",
        AssistantArea.Run => "run",
        AssistantArea.SolverSetup => "solver-setup",
        AssistantArea.Results => "results",
        AssistantArea.Notebook => "setup",
        _ => null
    };

    private static bool TryDecodeSha256Hex(string hex, out byte[] bytes)
    {
        bytes = [];
        if (hex.Length != 64) return false;
        try
        {
            bytes = Convert.FromHexString(hex);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record IndexedDoc(KnowledgeEntry Entry, Dictionary<string, int> TermFreq, int Length);
}
