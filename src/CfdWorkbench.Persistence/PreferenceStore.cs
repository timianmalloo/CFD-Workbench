namespace CfdWorkbench.Persistence;

public sealed record LayoutLoad(LayoutDocument Layout, string Outcome, IReadOnlyList<string> Codes,
    int DroppedPanes, int Clamped, bool NeverWrite, bool SessionOnly, string? DiskSha256);

public sealed record RecentLoad(IReadOnlyList<RecentEntry> Entries, string Outcome, IReadOnlyList<string> Codes,
    bool NeverWrite, bool SessionOnly, string? DiskSha256);

public sealed record PrefSave(string Outcome, string? Code, bool PublicationKnown, bool DurabilityConfirmed, bool Retried, string? ClaimPath);

/// <summary>Red stub. The store lands after the named checks fail.</summary>
public sealed class PreferenceStore(string root, Func<IProjectStore> storeFactory)
{
    private readonly string preferenceRoot = root;
    private readonly Func<IProjectStore> factory = storeFactory;
    public Task<LayoutLoad> LoadLayoutAsync(IReadOnlySet<string> registeredPanes, CancellationToken ct)
    {
        _ = preferenceRoot;
        _ = factory;
        throw new NotImplementedException();
    }
    public Task<PrefSave> SaveLayoutAsync(Func<LayoutDocument> snapshot, IReadOnlySet<string> changedWorkspaces, CancellationToken ct) => throw new NotImplementedException();
    public Task<RecentLoad> LoadRecentAsync(CancellationToken ct) => throw new NotImplementedException();
    public Task<PrefSave> UpdateRecentAsync(RecentOp op, CancellationToken ct) => throw new NotImplementedException();
}
