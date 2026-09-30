using System.Text;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Track P1 named checks for the preference store (docs/design/app-shell.md §4, §9).
internal static class PreferenceStoreTests
{
    internal static void Run()
    {
        Check("LayoutLoad_Absent_ReturnsPresets", Absent);
        Check("Rollback_V2UnknownTopLevel_BytesUnchanged", () => Rollback("v2-unknown-top.json"));
        Check("Rollback_V2UnknownWorkspaceMember_BytesUnchanged", () => Rollback("v2-unknown-workspace.json"));
        Check("Rollback_V2Oversized_BytesUnchanged", () => Rollback("v2-oversized.json"));
        Check("LayoutLoad_ReadError_NeverWrites", ReadError);
        Check("PrefStore_Unsupported_SessionOnly", Unsupported);
        Check("PrefStore_SymlinkedDirectory_SessionOnly", Symlink);
        Check("PrefsSave_LayoutAndRecentConcurrent_BothKept", Concurrent);
        Check("LayoutSave_ClaimBusyHashSame_RetriesOnce", Busy);
        Check("LayoutSave_Conflict_MergesChangedWorkspaceOnly", Conflict);
        Check("LayoutSave_TwoQueuedSaves_UnionOfChangedWorkspaces", Union);
        Check("LayoutSave_StaleClaim_SessionOnlyNamesClaim", Stale);
        Check("Recent_Conflict_ReappliesAdd", RecentConflict);
        Check("Recent_ClearFails_ReportedNotCleared", ClearFails);
        Check("Recent_Clear_NoFileContainsMarkerPath", ClearErases);
        Check("Recent_Remove_KeepsEveryOtherEntry", RemoveKeepsOthers);
        Check("Recent_RemoveWriteFails_ListUnchanged", RemoveFails);
        Check("StoreContract_CancelBeforePublish_DocCancelled", CancelContract);
    }

    private static void Absent()
    {
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ProjectStore());
        var load = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal("preset-first-run", load.Outcome);
        Equal(0, load.Codes.Count);
        Equal(false, load.NeverWrite);
        Equal(false, load.SessionOnly);
        Equal(true, load.DiskSha256 is null);
        LayoutFileTests.AssertPresets(load.Layout);
        Equal(false, File.Exists(Path.Combine(root, "layout", "layout.json")));
        var save = Wait(store.SaveLayoutAsync(() => load.Layout, new HashSet<string>(["planform"]), CancellationToken.None));
        Equal("saved", save.Outcome);
        Equal(true, save.PublicationKnown);
        Equal(true, save.DurabilityConfirmed);
        Equal(false, save.Retried);
        string directory = Path.Combine(root, "layout");
        string file = Path.Combine(directory, "layout.json");
        Equal(Mode(directory), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Equal(Mode(file), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        var again = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal("restored", again.Outcome);
        LayoutFileTests.AssertPresets(again.Layout);
    }

    private static void Rollback(string fixture)
    {
        string root = LayoutFileTests.Root();
        string directory = Path.Combine(root, "layout");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "layout.json");
        var original = File.ReadAllBytes(LayoutFileTests.Fixture(fixture));
        File.WriteAllBytes(path, original);
        var store = new PreferenceStore(root, () => new ProjectStore());
        var load = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(true, load.NeverWrite);
        Has(load.Codes, "LAYOUT-VERSION");
        var mutated = LayoutCodec.Presets(WorkspaceId.Review, LayoutFileTests.Panes());
        for (int point = 0; point < 3; point++)
        {
            var save = Wait(store.SaveLayoutAsync(() => mutated, new HashSet<string>(["planform"]), CancellationToken.None));
            Equal(false, save.PublicationKnown);
            Equal("never-write", save.Outcome);
        }
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
            throw new InvalidOperationException(fixture + " bytes changed");
    }

    private static void ReadError()
    {
        foreach (var code in new[] { "DOC-IO", "DOC-SIZE", "DOC-CONFLICT" })
        {
            string root = LayoutFileTests.Root();
            string directory = Path.Combine(root, "layout");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "layout.json");
            File.WriteAllBytes(path, [1, 2, 3]);
            var fake = new CodeStore(code);
            var store = new PreferenceStore(root, () => fake);
            var load = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
            Equal(true, load.NeverWrite);
            Equal(true, load.SessionOnly);
            Has(load.Codes, "LAYOUT-SCHEMA");
            Has(load.Codes, code);
            LayoutFileTests.AssertPresets(load.Layout);
            var save = Wait(store.SaveLayoutAsync(() => load.Layout, new HashSet<string>(["planform"]), CancellationToken.None));
            Equal(false, save.PublicationKnown);
            Equal(0, fake.Saves);
            Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }));
        }
    }

    private static void Unsupported()
    {
        string root = LayoutFileTests.Root();
        string directory = Path.Combine(root, "layout");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "layout.json"), [9]);
        var fake = new CodeStore("DOC-UNSUPPORTED-PERSISTENCE");
        var denied = new PreferenceStore(root, () => fake);
        var load = Wait(denied.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(true, load.SessionOnly);
        Equal("session-only", load.Outcome);
        Has(load.Codes, "LAYOUT-SESSION-ONLY");
        Has(load.Codes, "DOC-UNSUPPORTED-PERSISTENCE");
        var save = Wait(denied.SaveLayoutAsync(() => load.Layout, new HashSet<string>(["planform"]), CancellationToken.None));
        Equal("session-only", save.Outcome);
        Equal(0, fake.Saves);
        var real = Wait(new PreferenceStore(LayoutFileTests.Root(), () => new ProjectStore()).LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(false, real.SessionOnly);
        Equal("preset-first-run", real.Outcome);
    }

    private static void Symlink()
    {
        string real = LayoutFileTests.Root();
        string link = Path.Combine(LayoutFileTests.Root(), "prefs");
        Directory.CreateSymbolicLink(link, real);
        var store = new PreferenceStore(link, () => new ProjectStore());
        var load = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(true, load.SessionOnly);
        Equal("session-only", load.Outcome);
        Has(load.Codes, "LAYOUT-SESSION-ONLY");
        var save = Wait(store.SaveLayoutAsync(() => load.Layout, new HashSet<string>(["planform"]), CancellationToken.None));
        Equal("session-only", save.Outcome);
        Equal(false, save.PublicationKnown);
        Equal(false, File.Exists(Path.Combine(real, "layout", "layout.json")));
    }

    private static void Concurrent()
    {
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ProjectStore());
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        var layout = LayoutCodec.Presets(WorkspaceId.Precision, LayoutFileTests.Panes());
        const string path = "/abs/concurrent-kept.foil";
        var layoutSave = store.SaveLayoutAsync(() => layout, new HashSet<string>(["precision"]), CancellationToken.None);
        var recentSave = store.UpdateRecentAsync(new RecentOp.Add(path), CancellationToken.None);
        if (!Task.WaitAll([layoutSave, recentSave], TimeSpan.FromSeconds(5))) throw new TimeoutException("concurrent saves");
        Equal("saved", layoutSave.Result.Outcome);
        Equal("saved", recentSave.Result.Outcome);
        var loaded = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(WorkspaceId.Precision, loaded.Layout.Active);
        var recent = Wait(store.LoadRecentAsync(CancellationToken.None));
        Equal(path, recent.Entries.Single().Path);
        Equal(true, File.Exists(Path.Combine(root, "layout", "layout.json")));
        Equal(true, File.Exists(Path.Combine(root, "recent", "recent.json")));
    }

    private static void Busy()
    {
        var script = new Script();
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ScriptStore(script));
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Wait(store.SaveLayoutAsync(() => Sizes(201, 202, 203), new HashSet<string>(["planform", "precision", "review"]), CancellationToken.None));
        script.Trap = Trap.Busy;
        script.ArmedSaves = 0;
        var save = Wait(store.SaveLayoutAsync(() => Sizes(300, 202, 203), new HashSet<string>(["planform"]), CancellationToken.None));
        Equal(true, save.Retried);
        Equal("saved", save.Outcome);
        Equal(true, save.PublicationKnown);
        Equal(300d, Left(Read(root), WorkspaceId.Planform));
    }

    private static void Conflict()
    {
        var script = new Script();
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ScriptStore(script));
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Wait(store.SaveLayoutAsync(() => Sizes(201, 202, 203), new HashSet<string>(["planform", "precision", "review"]), CancellationToken.None));
        script.External = LayoutCodec.Serialize(Sizes(201, 202, 333));
        script.ArmedSaves = 0;
        script.Trap = Trap.Replace;
        var save = Wait(store.SaveLayoutAsync(() => Sizes(300, 202, 203), new HashSet<string>(["planform"]), CancellationToken.None));
        Equal(true, save.Retried);
        Equal("saved", save.Outcome);
        var final = Read(root);
        Equal(300d, Left(final, WorkspaceId.Planform));
        Equal(202d, Left(final, WorkspaceId.Precision));
        Equal(333d, Left(final, WorkspaceId.Review));
    }

    private static void Union()
    {
        var script = new Script();
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ScriptStore(script));
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Wait(store.SaveLayoutAsync(() => Sizes(201, 202, 203), new HashSet<string>(["planform", "precision", "review"]), CancellationToken.None));
        script.External = LayoutCodec.Serialize(Sizes(201, 202, 333));
        script.ArmedSaves = 0;
        script.Trap = Trap.Replace;
        var box = new LayoutDocument[1];
        box[0] = Sizes(300, 202, 203);
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        // The gate is free, so the snapshot runs on the caller. Park it on the pool so this thread can release it.
        var first = Task.Run(() => store.SaveLayoutAsync(() =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("snapshot");
            return box[0];
        }, new HashSet<string>(["planform"]), CancellationToken.None));
        if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("entered");
        box[0] = Sizes(300, 310, 203);
        var second = store.SaveLayoutAsync(() => box[0], new HashSet<string>(["precision"]), CancellationToken.None);
        release.Set();
        if (!Task.WaitAll([first, second], TimeSpan.FromSeconds(5))) throw new TimeoutException("queued saves");
        Equal("saved", first.Result.Outcome);
        Equal(true, first.Result.Retried);
        bool found = false;
        foreach (var image in script.Published)
        {
            var document = LayoutCodec.Parse(image, LayoutFileTests.Panes()).Document;
            if (Left(document, WorkspaceId.Planform) == 300d && Left(document, WorkspaceId.Precision) == 310d && Left(document, WorkspaceId.Review) == 333d)
                found = true;
        }
        Equal(true, found);
    }

    private static void Stale()
    {
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ProjectStore());
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Wait(store.SaveLayoutAsync(() => Sizes(201, 202, 203), new HashSet<string>(["planform"]), CancellationToken.None));
        string claim = Path.Combine(root, "layout", ".cfd-writer.claim");
        File.WriteAllBytes(claim, [1]);
        var save = Wait(store.SaveLayoutAsync(() => Sizes(300, 202, 203), new HashSet<string>(["planform"]), CancellationToken.None));
        Equal("claim-held", save.Outcome);
        Equal(true, save.Retried);
        Equal(claim, save.ClaimPath);
        Equal(201d, Left(Read(root), WorkspaceId.Planform));
        var again = Wait(store.SaveLayoutAsync(() => Sizes(310, 202, 203), new HashSet<string>(["planform"]), CancellationToken.None));
        Equal("claim-held", again.Outcome);
        Equal(claim, again.ClaimPath);
        Equal(201d, Left(Read(root), WorkspaceId.Planform));
    }

    private static void RecentConflict()
    {
        var script = new Script();
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ScriptStore(script));
        Wait(store.UpdateRecentAsync(new RecentOp.Add("/abs/b.foil"), CancellationToken.None));
        Wait(store.UpdateRecentAsync(new RecentOp.Add("/abs/c.foil"), CancellationToken.None));
        // Adds prepend, so the file is c then b. The conflict replaces it with d.
        script.External = RecentList.Serialize([new RecentEntry("/abs/d.foil")]);
        script.ArmedSaves = 0;
        script.Trap = Trap.Replace;
        var save = Wait(store.UpdateRecentAsync(new RecentOp.Add("/abs/a.foil"), CancellationToken.None));
        Equal(true, save.Retried);
        Equal("saved", save.Outcome);
        var recent = Wait(store.LoadRecentAsync(CancellationToken.None));
        Equal(2, recent.Entries.Count);
        Equal("/abs/a.foil", recent.Entries[0].Path);
        Equal("/abs/d.foil", recent.Entries[1].Path);
    }

    private static void ClearFails()
    {
        string root = LayoutFileTests.Root();
        string directory = Path.Combine(root, "recent");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "recent.json");
        var original = File.ReadAllBytes(LayoutFileTests.Fixture("recent-v2.json"));
        File.WriteAllBytes(path, original);
        var store = new PreferenceStore(root, () => new ProjectStore());
        var save = Wait(store.UpdateRecentAsync(new RecentOp.Clear(), CancellationToken.None));
        Equal("Recent list not cleared", save.Outcome);
        Equal("LAYOUT-VERSION", save.Code);
        Equal(false, save.PublicationKnown);
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
            throw new InvalidOperationException("recent v2 bytes changed");
    }

    private static void RemoveKeepsOthers()
    {
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ProjectStore());
        foreach (string path in new[] { "/abs/a.foil", "/abs/b.foil", "/abs/c.foil" })
            Equal("saved", Wait(store.UpdateRecentAsync(new RecentOp.Add(path), CancellationToken.None)).Outcome);
        // Adds prepend, so the list is c, b, a. Removing b keeps c then a, in order, in one write.
        var save = Wait(store.UpdateRecentAsync(new RecentOp.Remove("/abs/b.foil"), CancellationToken.None));
        Equal("saved", save.Outcome);
        var recent = Wait(store.LoadRecentAsync(CancellationToken.None));
        Equal("/abs/c.foil,/abs/a.foil", string.Join(",", recent.Entries.Select(entry => entry.Path)));
    }

    private static void RemoveFails()
    {
        string root = LayoutFileTests.Root();
        var seed = new PreferenceStore(root, () => new ProjectStore());
        Wait(seed.UpdateRecentAsync(new RecentOp.Add("/abs/a.foil"), CancellationToken.None));
        Wait(seed.UpdateRecentAsync(new RecentOp.Add("/abs/b.foil"), CancellationToken.None));
        string path = Path.Combine(root, "recent", "recent.json");
        byte[] before = File.ReadAllBytes(path);
        var failing = new PreferenceStore(root, () => new SaveFailStore());
        var save = Wait(failing.UpdateRecentAsync(new RecentOp.Remove("/abs/a.foil"), CancellationToken.None));
        Equal("failed", save.Outcome);
        Equal("DOC-IO", save.Code);
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(before))
            throw new InvalidOperationException("recent bytes changed after a failed Remove");
        var recent = Wait(seed.LoadRecentAsync(CancellationToken.None));
        Equal("/abs/b.foil,/abs/a.foil", string.Join(",", recent.Entries.Select(entry => entry.Path)));
    }

    private static void ClearErases()
    {
        const string marker = "P1_RECENT_MARKER_7f3a";
        string path = "/abs/" + marker + ".foil";
        string root = LayoutFileTests.Root();
        var store = new PreferenceStore(root, () => new ProjectStore());
        var add = Wait(store.UpdateRecentAsync(new RecentOp.Add(path), CancellationToken.None));
        Equal("saved", add.Outcome);
        Equal(true, Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root, "recent", "recent.json"))).Contains(marker, StringComparison.Ordinal));
        var clear = Wait(store.UpdateRecentAsync(new RecentOp.Clear(), CancellationToken.None));
        Equal("saved", clear.Outcome);
        var recent = Wait(store.LoadRecentAsync(CancellationToken.None));
        Equal(0, recent.Entries.Count);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file).Contains(marker, StringComparison.Ordinal))
                throw new InvalidOperationException("name " + file);
            if (Encoding.UTF8.GetString(File.ReadAllBytes(file)).Contains(marker, StringComparison.Ordinal))
                throw new InvalidOperationException("bytes " + file);
        }
    }

    private static void CancelContract()
    {
        CancelReal();
        CancelFake();
    }

    private static void CancelReal()
    {
        string root = LayoutFileTests.Root();
        string directory = Path.Combine(root, "layout");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "layout.json");
        var original = "seed-layout"u8.ToArray();
        File.WriteAllBytes(path, original);
        var cts = new CancellationTokenSource();
        var store = new PreferenceStore(root, () => new ProjectStore(new StoreHooks
        {
            OnStage = stage => { if (stage == StoreStage.BeforePublish) cts.Cancel(); }
        }));
        var load = Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        Equal(false, load.NeverWrite);
        var save = Wait(store.SaveLayoutAsync(() => LayoutCodec.Presets(WorkspaceId.Planform, LayoutFileTests.Panes()), new HashSet<string>(["planform"]), cts.Token));
        Equal("DOC-CANCELLED", save.Code);
        Equal("cancelled", save.Outcome);
        Equal(false, save.PublicationKnown);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
    }

    private static void CancelFake()
    {
        string root = LayoutFileTests.Root();
        string directory = Path.Combine(root, "layout");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "layout.json");
        var original = "seed-layout"u8.ToArray();
        File.WriteAllBytes(path, original);
        var cts = new CancellationTokenSource();
        string hash = Identity.Sha256(original);
        var store = new PreferenceStore(root, () => new CancelStore(cts, original, hash));
        Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None));
        var save = Wait(store.SaveLayoutAsync(() => LayoutCodec.Presets(WorkspaceId.Planform, LayoutFileTests.Panes()), new HashSet<string>(["planform"]), cts.Token));
        Equal("DOC-CANCELLED", save.Code);
        Equal(false, save.PublicationKnown);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
    }

    private static LayoutDocument Sizes(double planform, double precision, double review)
    {
        var document = LayoutCodec.Presets(WorkspaceId.Planform, LayoutFileTests.Panes());
        return new LayoutDocument(document.Active, document.Workspaces.Select(workspace => workspace.Id switch
        {
            WorkspaceId.Planform => LeftSize(workspace, planform),
            WorkspaceId.Precision => LeftSize(workspace, precision),
            _ => LeftSize(workspace, review)
        }).ToArray());
    }

    private static WorkspaceLayout LeftSize(WorkspaceLayout workspace, double size)
    {
        var regions = workspace.Regions.Select(region => region.Id == RegionId.Left ? region with { Size = size } : region).ToArray();
        return workspace with { Regions = regions };
    }

    private static LayoutDocument Read(string root) =>
        LayoutCodec.Parse(File.ReadAllBytes(Path.Combine(root, "layout", "layout.json")), LayoutFileTests.Panes()).Document;

    private static double Left(LayoutDocument document, WorkspaceId id) =>
        document.Workspaces.Single(workspace => workspace.Id == id).Regions.Single(region => region.Id == RegionId.Left).Size;

    private static UnixFileMode Mode(string path)
    {
        if (OperatingSystem.IsWindows()) return 0;
        return File.GetUnixFileMode(path) & (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
    }

    private static void Has(IReadOnlyList<string> codes, string code)
    {
        if (!codes.Contains(code)) throw new InvalidOperationException("missing " + code + " [" + string.Join(",", codes) + "]");
    }

    private static T Wait<T>(Task<T> task)
    {
        if (!task.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("preference operation");
        return task.GetAwaiter().GetResult();
    }

    private enum Trap { None, Busy, Replace }

    private sealed class Script
    {
        internal Trap Trap;
        internal byte[] External = [];
        internal int ArmedSaves;
        internal List<byte[]> Published = [];
    }

    private sealed class ScriptStore(Script script) : IProjectStore
    {
        public void Dispose() { }
        public async Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default)
        {
            using var store = new ProjectStore();
            return await store.ReadAsync(path, cancellation);
        }
        public async Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
        {
            bool trap;
            Trap mode;
            lock (script)
            {
                trap = script.Trap != Trap.None && script.ArmedSaves == 0;
                if (script.Trap != Trap.None) script.ArmedSaves++;
                mode = script.Trap;
            }
            if (trap && mode == Trap.Busy)
            {
                string claim = Path.Combine(Path.GetDirectoryName(path)!, ".cfd-writer.claim");
                File.WriteAllBytes(claim, [1]);
                using var busy = new ProjectStore();
                var blocked = await busy.SaveAsync(path, request, cancellation);
                File.Delete(claim);
                return blocked;
            }
            StoreHooks? hooks = trap && mode == Trap.Replace
                ? new StoreHooks { OnStage = stage => { if (stage == StoreStage.BeforePublish) File.WriteAllBytes(path, script.External); } }
                : null;
            using var store = hooks is null ? new ProjectStore() : new ProjectStore(hooks);
            var result = await store.SaveAsync(path, request, cancellation);
            if (result.Code == "OK") lock (script) script.Published.Add(request.Image.ToArray());
            return result;
        }
    }

    private sealed class CodeStore(string code) : IProjectStore
    {
        public int Saves;
        public void Dispose() { }
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
        {
            Saves++;
            return Task.FromResult(new SaveResult(code, null, false, false));
        }
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) =>
            Task.FromException<ReadResult>(new ContractError(code));
    }

    /// <summary>Reads through the real store; every save fails with <c>DOC-IO</c> and publishes nothing.</summary>
    private sealed class SaveFailStore : IProjectStore
    {
        private readonly ProjectStore inner = new();
        public void Dispose() => inner.Dispose();
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) => inner.ReadAsync(path, cancellation);
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default) =>
            Task.FromResult(new SaveResult("DOC-IO", null, false, false));
    }

    private sealed class CancelStore(CancellationTokenSource cts, byte[] image, string hash) : IProjectStore
    {
        public void Dispose() { }
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) =>
            Task.FromResult(new ReadResult(image, hash));
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
        {
            cts.Cancel();
            return Task.FromResult(new SaveResult("DOC-CANCELLED", null, false, false));
        }
    }
}
