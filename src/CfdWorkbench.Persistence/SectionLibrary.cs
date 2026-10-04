using CfdWorkbench.Core;

namespace CfdWorkbench.Persistence;

public sealed record LibraryEntry(string Name, string Hash, Provenance Provenance, byte[] Bytes);
public sealed record LibraryScan(IReadOnlyList<LibraryEntry> Entries, IReadOnlyList<(string File, string Reason)> Problems);

public sealed class SectionLibrary(string root)
{
    private readonly string root = root;
    private Func<ProjectStore> storeFactory = static () => new ProjectStore();
    internal SectionLibrary(string root, Func<ProjectStore> storeFactory) : this(root) { this.storeFactory = storeFactory; }
    public static string Root(string preferenceRoot) => Path.Combine(preferenceRoot, "sections");
    public LibraryScan Scan() { _ = root; _ = storeFactory; throw new NotImplementedException("LIB: SectionLibrary.Scan"); }
    public LibraryEntry Save(string name, byte[] profileBlockBytes, Provenance provenance) =>
        throw new NotImplementedException("LIB: SectionLibrary.Save");
}
