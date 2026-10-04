namespace CfdWorkbench.Core;

/// <summary>Rights derived from a provenance origin. Never stored.</summary>
public enum RightsClass
{
    Gen,
    Vend,
    YourFile,
    NotRecorded
}

/// <summary>
/// Origin of a profile shape plus the one modified flag. Rights and chip text are derived.
/// Seam S-1: the signature LIB and RPL build against. Bodies land in the CAT commits after this one.
/// </summary>
public sealed record Provenance(string? Origin, bool Modified)
{
    public static Provenance Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new(null, false);
        string trimmed = text.Trim();
        bool modified = false;
        const string flag = " modified";
        if (trimmed.EndsWith(flag, StringComparison.Ordinal))
        {
            modified = true;
            trimmed = trimmed[..^flag.Length];
        }
        return TryOrigin(trimmed, out string origin) ? new(origin, modified) : new(null, false);
    }

    public string Format() => Origin is null ? "" : Modified ? Origin + " modified" : Origin;

    public RightsClass Rights => Classify(Origin);

    public string ChipText(string sourceName) =>
        Origin is null ? "Source not recorded"
        : Modified ? "Modified from " + sourceName
        : "Catalog original · " + sourceName;

    /// <summary>
    /// The only writer of <c> modified</c>. Adds it when <paramref name="bytes"/> changed
    /// <paramref name="profile"/> beyond the identity tolerance relative to <paramref name="before"/>.
    /// </summary>
    public static byte[] MarkModified(byte[] bytes, string profile, byte[] before) =>
        throw new NotImplementedException("CAT: Provenance.MarkModified");

    private static RightsClass Classify(string? origin)
    {
        if (origin is null || !TryOrigin(origin, out string canonical) || canonical != origin)
            return RightsClass.NotRecorded;
        if (origin.StartsWith("gen:", StringComparison.Ordinal)) return RightsClass.Gen;
        if (origin.StartsWith("vend:", StringComparison.Ordinal)) return RightsClass.Vend;
        return RightsClass.YourFile;
    }

    private static bool TryOrigin(string text, out string origin)
    {
        origin = "";
        if (Prefixed(text, "gen:", out string genId) || Prefixed(text, "vend:", out genId))
        {
            if (!CatalogId(genId)) return false;
            origin = text;
            return true;
        }
        const string dat = "dat:sha256:";
        if (text.StartsWith(dat, StringComparison.Ordinal))
        {
            if (!Hex64(text[dat.Length..])) return false;
            origin = text;
            return true;
        }
        int sha = text.IndexOf(" sha256:", StringComparison.Ordinal);
        int points = text.LastIndexOf(" points:", StringComparison.Ordinal);
        if (sha <= 0 || points < 0 || points < sha) return false;
        string format = text[..sha];
        if (format is not ("selig" or "lednicer")) return false;
        string hex = text[(sha + " sha256:".Length)..points];
        string count = text[(points + " points:".Length)..];
        if (!Hex64(hex) || count.Length == 0 || !count.All(char.IsAsciiDigit)) return false;
        origin = dat + hex;
        return true;
    }

    private static bool Prefixed(string text, string prefix, out string rest)
    {
        if (text.StartsWith(prefix, StringComparison.Ordinal))
        {
            rest = text[prefix.Length..];
            return true;
        }
        rest = "";
        return false;
    }

    private static bool CatalogId(string id) => id.Length > 0 && id.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static bool Hex64(string hex) => hex.Length == 64 && hex.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
