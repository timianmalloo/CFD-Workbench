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
    public static Provenance Parse(string? text) =>
        throw new NotImplementedException("CAT: Provenance.Parse");

    public string Format() =>
        throw new NotImplementedException("CAT: Provenance.Format");

    public RightsClass Rights =>
        throw new NotImplementedException("CAT: Provenance.Rights");

    public string ChipText(string sourceName) =>
        throw new NotImplementedException("CAT: Provenance.ChipText");

    /// <summary>
    /// The only writer of <c> modified</c>. Adds it when <paramref name="bytes"/> changed
    /// <paramref name="profile"/> beyond the identity tolerance relative to <paramref name="before"/>.
    /// </summary>
    public static byte[] MarkModified(byte[] bytes, string profile, byte[] before) =>
        throw new NotImplementedException("CAT: Provenance.MarkModified");
}
