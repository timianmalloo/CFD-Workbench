namespace CfdWorkbench.Core;

// assume: CatalogFamily is the picker grouping from design §3.1 (Naca, Eppler, Speer, MySections).
// The §5.1 snippet repeats the admission classes, which cannot group the dialog (§14.1).
// Confirm: a DLG build groups rows by Family. Breaks if a caller expected Family to be Gen/Vend/Link.
public enum CatalogFamily
{
    Naca,
    Eppler,
    Speer,
    MySections
}

public enum AdmissionClass
{
    Gen,
    Vend,
    Link
}

public sealed record CatalogEntry(
    string Id,
    CatalogFamily Family,
    string Designation,
    AdmissionClass Class,
    string? DisabledReason,
    byte[]? Coordinates)
{
    public double FrameLeShift { get; init; }
    public double FrameRotationDegrees { get; init; }
    public double FrameScale { get; init; }
    internal string? CoordinateHash { get; init; }
}

public static class Catalog
{
    public static IReadOnlyList<CatalogEntry> Load() =>
        throw new NotImplementedException("CAT: Catalog.Load");

    internal static IReadOnlyList<CatalogEntry> Read(string table, Func<string, byte[]?> coordinates) =>
        throw new NotImplementedException("CAT: Catalog.Read");
}

public static class CatalogGenerator
{
    public const string Id = "naca4-closed/1";

    public static byte[] Naca4(string digits) =>
        throw new NotImplementedException("CAT: CatalogGenerator.Naca4");
}
