using CfdWorkbench.Core;

namespace CfdWorkbench.Persistence;

public sealed record LayoutLoad(LayoutDocument Layout, string Outcome, IReadOnlyList<string> Codes,
    int DroppedPanes, int Clamped, bool NeverWrite, bool SessionOnly, string? DiskSha256);

public sealed record RecentLoad(IReadOnlyList<RecentEntry> Entries, string Outcome, IReadOnlyList<string> Codes,
    bool NeverWrite, bool SessionOnly, string? DiskSha256);

public sealed record PrefSave(string Outcome, string? Code, bool PublicationKnown, bool DurabilityConfirmed, bool Retried, string? ClaimPath);

/// <summary>Installation preferences under <paramref name="root"/>/<c>layout</c> and <c>recent</c> (§4).</summary>
public sealed class PreferenceStore(string root, Func<IProjectStore> storeFactory)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object sync = new();
    private readonly HashSet<string> pending = new(StringComparer.Ordinal);
    private IReadOnlySet<string> registered = new HashSet<string>(StringComparer.Ordinal);
    private string? layoutHash;
    private string? recentHash;
    private WorkspaceId? lastActive;
    private bool layoutNeverWrite;
    private bool recentNeverWrite;
    private bool layoutSession;
    private bool recentSession;
    private bool layoutHeld;
    private bool recentHeld;
    private string? layoutBlock;
    private string? recentBlock;

    private string LayoutDir => Path.Combine(root, "layout");
    private string RecentDir => Path.Combine(root, "recent");
    private string LayoutPath => Path.Combine(LayoutDir, "layout.json");
    private string RecentPath => Path.Combine(RecentDir, "recent.json");
    private static string Claim(string directory) => Path.Combine(directory, ".cfd-writer.claim");

    public async Task<LayoutLoad> LoadLayoutAsync(IReadOnlySet<string> registeredPanes, CancellationToken ct)
    {
        registered = registeredPanes ?? new HashSet<string>(StringComparer.Ordinal);
        var presets = LayoutCodec.Presets(WorkspaceId.Planform, registered);
        if (Linked(root) || Linked(LayoutDir))
        {
            layoutSession = true;
            lastActive = presets.Active;
            return new LayoutLoad(presets, "session-only", ["LAYOUT-SESSION-ONLY"], 0, 0, true, true, null);
        }
        if (!File.Exists(LayoutPath))
        {
            lastActive = presets.Active;
            layoutHash = null;
            return new LayoutLoad(presets, "preset-first-run", [], 0, 0, false, false, null);
        }
        var (read, error) = await Read(LayoutPath, ct).ConfigureAwait(false);
        if (error is not null) return LayoutReadFailed(presets, error);
        var parsed = LayoutCodec.Parse(read!.Image, registered);
        lastActive = parsed.Document.Active;
        if (parsed.NeverWrite)
        {
            layoutNeverWrite = true;
            layoutBlock = parsed.Codes.Count == 0 ? "LAYOUT-VERSION" : parsed.Codes[0];
            return new LayoutLoad(parsed.Document, "never-write", parsed.Codes, parsed.DroppedPanes, parsed.Clamped, true, true, read.DiskSha256);
        }
        layoutHash = read.DiskSha256;
        string outcome = parsed.Codes.Contains("LAYOUT-SCHEMA") ? "preset-fallback" : "restored";
        return new LayoutLoad(parsed.Document, outcome, parsed.Codes, parsed.DroppedPanes, parsed.Clamped, false, false, read.DiskSha256);
    }

    public async Task<PrefSave> SaveLayoutAsync(Func<LayoutDocument> snapshot, IReadOnlySet<string> changedWorkspaces, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (sync)
        {
            if (changedWorkspaces is not null)
                foreach (var id in changedWorkspaces)
                    if (!string.IsNullOrEmpty(id)) pending.Add(id);
        }
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (Linked(root) || Linked(LayoutDir) || layoutSession) return Session();
            if (layoutNeverWrite) return new PrefSave("never-write", layoutBlock ?? "LAYOUT-VERSION", false, false, false, null);
            if (layoutHeld) return new PrefSave("claim-held", "DOC-CONFLICT", false, false, false, Claim(LayoutDir));
            EnsureDir(LayoutDir);
            var snap = snapshot();
            string[] mine;
            lock (sync) mine = pending.ToArray();
            var image = LayoutCodec.Serialize(snap);
            var first = await Write(LayoutPath, image, layoutHash, ct).ConfigureAwait(false);
            if (first.Code == "OK")
            {
                CommitLayout(snap, first, mine);
                return Saved(first, false);
            }
            if (first.Code != "DOC-CONFLICT") return Failed(first);
            return await ResolveLayout(snap, mine, image, ct).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    public async Task<RecentLoad> LoadRecentAsync(CancellationToken ct)
    {
        if (Linked(root) || Linked(RecentDir))
        {
            recentSession = true;
            return new RecentLoad([], "session-only", ["LAYOUT-SESSION-ONLY"], true, true, null);
        }
        if (!File.Exists(RecentPath))
        {
            recentHash = null;
            return new RecentLoad([], "absent", [], false, false, null);
        }
        var (read, error) = await Read(RecentPath, ct).ConfigureAwait(false);
        if (error is not null) return RecentReadFailed(error);
        var parsed = RecentList.Parse(read!.Image);
        if (parsed.NeverWrite)
        {
            recentNeverWrite = true;
            recentBlock = parsed.Codes.Count == 0 ? "LAYOUT-VERSION" : parsed.Codes[0];
            return new RecentLoad([], "never-write", parsed.Codes, true, true, read.DiskSha256);
        }
        recentHash = read.DiskSha256;
        return new RecentLoad(parsed.Entries, parsed.Codes.Count == 0 ? "restored" : "preset-fallback", parsed.Codes, false, false, read.DiskSha256);
    }

    public async Task<PrefSave> UpdateRecentAsync(RecentOp op, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(op);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (Linked(root) || Linked(RecentDir) || recentSession) return Session();
            if (recentHeld) return new PrefSave(op is RecentOp.Clear ? "Recent list not cleared" : "claim-held", "DOC-CONFLICT", false, false, false, Claim(RecentDir));
            if (op is RecentOp.Add add && !RecentList.Accepts(add.Path))
                return new PrefSave("rejected", "RECENT-SCHEMA", false, false, false, null);
            if (recentNeverWrite)
                return new PrefSave(op is RecentOp.Clear ? "Recent list not cleared" : "never-write", recentBlock ?? "LAYOUT-VERSION", false, false, false, null);
            EnsureDir(RecentDir);
            var (entries, hash, blocked) = await CurrentRecent(ct).ConfigureAwait(false);
            if (blocked is not null)
                return op is RecentOp.Clear ? new PrefSave("Recent list not cleared", blocked.Code, false, false, false, null) : blocked;
            var image = RecentList.Serialize(RecentList.Apply(entries, op));
            var first = await Write(RecentPath, image, hash, ct).ConfigureAwait(false);
            if (first.Code == "OK")
            {
                recentHash = first.PublishedSha256;
                return Saved(first, false);
            }
            if (first.Code != "DOC-CONFLICT") return op is RecentOp.Clear ? NotCleared(first.Code, false) : Failed(first);
            return await ResolveRecent(op, image, hash, ct).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private async Task<PrefSave> ResolveLayout(LayoutDocument snap, string[] mine, byte[] image, CancellationToken ct)
    {
        var (read, error) = await Read(LayoutPath, ct).ConfigureAwait(false);
        if (error is not null || read is null)
        {
            layoutNeverWrite = true;
            layoutBlock = error ?? "DOC-IO";
            return new PrefSave("never-write", layoutBlock, false, false, true, null);
        }
        var peek = LayoutCodec.Peek(read.Image);
        if (!peek.Failed && peek.Format == LayoutDocument.FormatName && peek.Version is > LayoutDocument.CurrentVersion)
        {
            layoutNeverWrite = true;
            layoutBlock = "LAYOUT-VERSION";
            return new PrefSave("never-write", "LAYOUT-VERSION", false, false, false, null);
        }
        if (read.DiskSha256 == layoutHash)
        {
            var retry = await Write(LayoutPath, image, layoutHash, ct).ConfigureAwait(false);
            if (retry.Code == "OK")
            {
                CommitLayout(snap, retry, mine);
                return Saved(retry, true);
            }
            return HoldLayout();
        }
        var parsed = LayoutCodec.Parse(read.Image, registered);
        if (parsed.NeverWrite || parsed.Codes.Contains("LAYOUT-SCHEMA"))
        {
            layoutNeverWrite = true;
            layoutBlock = parsed.Codes.Count == 0 ? "LAYOUT-SCHEMA" : parsed.Codes[0];
            return new PrefSave("never-write", layoutBlock, false, false, true, null);
        }
        var merged = Merge(parsed.Document, snap, mine);
        var retryMerge = await Write(LayoutPath, LayoutCodec.Serialize(merged), read.DiskSha256, ct).ConfigureAwait(false);
        if (retryMerge.Code == "OK")
        {
            CommitLayout(merged, retryMerge, mine);
            return Saved(retryMerge, true);
        }
        return HoldLayout();
    }

    private async Task<PrefSave> ResolveRecent(RecentOp op, byte[] image, string? hash, CancellationToken ct)
    {
        var (read, error) = await Read(RecentPath, ct).ConfigureAwait(false);
        if (error is not null || read is null)
            return op is RecentOp.Clear ? NotCleared(error ?? "DOC-IO", true) : HoldRecent(error);
        var parsed = RecentList.Parse(read.Image);
        if (parsed.NeverWrite)
        {
            recentNeverWrite = true;
            recentBlock = parsed.Codes.Count == 0 ? "LAYOUT-VERSION" : parsed.Codes[0];
            return new PrefSave(op is RecentOp.Clear ? "Recent list not cleared" : "never-write", recentBlock, false, false, false, null);
        }
        if (read.DiskSha256 == hash)
        {
            var retry = await Write(RecentPath, image, hash, ct).ConfigureAwait(false);
            if (retry.Code == "OK") { recentHash = retry.PublishedSha256; return Saved(retry, true); }
            return op is RecentOp.Clear ? NotCleared(retry.Code, true) : HoldRecent(retry.Code);
        }
        var reapplied = RecentList.Serialize(RecentList.Apply(parsed.Entries, op));
        var second = await Write(RecentPath, reapplied, read.DiskSha256, ct).ConfigureAwait(false);
        if (second.Code == "OK") { recentHash = second.PublishedSha256; return Saved(second, true); }
        return op is RecentOp.Clear ? NotCleared(second.Code, true) : HoldRecent(second.Code);
    }

    private async Task<(IReadOnlyList<RecentEntry> Entries, string? Hash, PrefSave? Blocked)> CurrentRecent(CancellationToken ct)
    {
        if (!File.Exists(RecentPath)) return ([], null, null);
        var (read, error) = await Read(RecentPath, ct).ConfigureAwait(false);
        if (error is not null)
        {
            if (error == "DOC-UNSUPPORTED-PERSISTENCE") recentSession = true;
            else { recentNeverWrite = true; recentBlock = error; }
            return ([], null, new PrefSave("never-write", error, false, false, false, null));
        }
        var parsed = RecentList.Parse(read!.Image);
        if (parsed.NeverWrite)
        {
            recentNeverWrite = true;
            recentBlock = parsed.Codes.Count == 0 ? "LAYOUT-VERSION" : parsed.Codes[0];
            return ([], read.DiskSha256, new PrefSave("never-write", recentBlock, false, false, false, null));
        }
        recentHash = read.DiskSha256;
        return (parsed.Entries, read.DiskSha256, null);
    }

    private LayoutDocument Merge(LayoutDocument disk, LayoutDocument ours, IReadOnlyList<string> changed)
    {
        var want = new HashSet<WorkspaceId>();
        foreach (var name in changed)
            if (LayoutCodec.TryWorkspace(name, out var id)) want.Add(id);
        var map = new Dictionary<WorkspaceId, WorkspaceLayout>();
        foreach (var workspace in disk.Workspaces) map[workspace.Id] = workspace;
        foreach (var workspace in ours.Workspaces)
            if (want.Contains(workspace.Id)) map[workspace.Id] = workspace;
        bool switched = lastActive is WorkspaceId prior && ours.Active != prior;
        var active = switched ? ours.Active : disk.Active;
        if (!map.ContainsKey(active)) map[active] = LayoutCodec.Preset(active, registered);
        var list = new List<WorkspaceLayout>();
        foreach (var id in new[] { WorkspaceId.Planform, WorkspaceId.Precision, WorkspaceId.Review })
            if (map.TryGetValue(id, out var workspace)) list.Add(workspace);
        return new LayoutDocument(active, list);
    }

    private void CommitLayout(LayoutDocument document, SaveResult result, IReadOnlyList<string> mine)
    {
        layoutHash = result.PublishedSha256;
        lastActive = document.Active;
        lock (sync)
            foreach (var id in mine) pending.Remove(id);
    }

    private LayoutLoad LayoutReadFailed(LayoutDocument presets, string error)
    {
        lastActive = presets.Active;
        if (error == "DOC-UNSUPPORTED-PERSISTENCE")
        {
            layoutSession = true;
            return new LayoutLoad(presets, "session-only", ["LAYOUT-SESSION-ONLY", error], 0, 0, true, true, null);
        }
        layoutNeverWrite = true;
        layoutBlock = error;
        return new LayoutLoad(presets, "never-write", ["LAYOUT-SCHEMA", error], 0, 0, true, true, null);
    }

    private RecentLoad RecentReadFailed(string error)
    {
        if (error == "DOC-UNSUPPORTED-PERSISTENCE")
        {
            recentSession = true;
            return new RecentLoad([], "session-only", ["LAYOUT-SESSION-ONLY", error], true, true, null);
        }
        recentNeverWrite = true;
        recentBlock = error;
        return new RecentLoad([], "never-write", ["RECENT-SCHEMA", error], true, true, null);
    }

    private PrefSave HoldLayout()
    {
        layoutHeld = true;
        return new PrefSave("claim-held", "DOC-CONFLICT", false, false, true, Claim(LayoutDir));
    }

    private PrefSave HoldRecent(string? code)
    {
        recentHeld = true;
        return new PrefSave("claim-held", code ?? "DOC-CONFLICT", false, false, true, Claim(RecentDir));
    }

    private static PrefSave NotCleared(string? code, bool retried) =>
        new("Recent list not cleared", code, false, false, retried, null);

    private static PrefSave Saved(SaveResult result, bool retried) =>
        new("saved", null, result.PublicationKnown, result.DurabilityConfirmed, retried, null);

    private static PrefSave Failed(SaveResult result) =>
        new(result.Code == "DOC-CANCELLED" ? "cancelled" : "failed", result.Code, result.PublicationKnown, result.DurabilityConfirmed, false, null);

    private static PrefSave Session() => new("session-only", "LAYOUT-SESSION-ONLY", false, false, false, null);

    private async Task<SaveResult> Write(string path, byte[] image, string? expected, CancellationToken ct)
    {
        var store = storeFactory();
        try
        {
            return await store.SaveAsync(path, new SaveRequest(image, expected, Guid.NewGuid().ToString("D")), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new SaveResult("DOC-CANCELLED", null, false, false);
        }
        catch (ContractError error)
        {
            return new SaveResult(error.Code, null, false, false);
        }
        finally
        {
            if (store is IDisposable disposable) disposable.Dispose();
        }
    }

    private async Task<(ReadResult? Read, string? Error)> Read(string path, CancellationToken ct)
    {
        var store = storeFactory();
        try
        {
            return (await store.ReadAsync(path, ct).ConfigureAwait(false), null);
        }
        catch (OperationCanceledException)
        {
            return (null, "DOC-CANCELLED");
        }
        catch (ContractError error)
        {
            return (null, error.Code);
        }
        finally
        {
            if (store is IDisposable disposable) disposable.Dispose();
        }
    }

    private static void EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static bool Linked(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path)) return false;
        return File.ResolveLinkTarget(path, returnFinalTarget: false) is not null;
    }
}
