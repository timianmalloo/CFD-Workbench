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
        Check("PrefStore_TextSize_Absent_Is100", TextSizeAbsent);
        Check("PrefStore_TextSize_RoundTrip", TextSizeRoundTrip);
        Check("PrefStore_TextSize_ValueError_ThatMemberDefaults_BytesUnchanged", () => TextSizeUnreadable(ValueErrors));
        Check("PrefStore_TextSize_StructureError_WholeFileDefaults_BytesUnchanged", () => TextSizeUnreadable(StructureErrors));
        Check("Rollback_TextSizeV2_NewerVersionIsStructureClass_BytesUnchanged", TextSizeFuture);
        Check("PrefStore_TextSize_PriorRoot_LayoutAndRecentUntouched", TextSizePriorRoot);
        Check("PrefStore_TextSize_SessionOnlyOrUnreadable_NeverWrites", TextSizeSessionOnly);
        Check("PrefStore_TextSize_FileSystemException_FailedNotThrown", TextSizeFileSystemException);
        Check("PrefStore_TextSize_LoadAndSaveSerialized_NoStaleHash", TextSizeLoadSerialized);
        Check("PrefStore_TextSize_SerializeOutOfSet_Throws", TextSizeSerializeOutOfSet);
        Check("PrefStore_DirectoryLink_WindowsBranchUsesJunction", DirectoryLinkWindowsBranch);
        RunView();
    }

    // WFX2 item 3: Directory.CreateSymbolicLink needs a privilege on Windows (IOException "A required privilege is not held").
    // A directory junction is a reparse point too and needs none. The Windows branch is selectable so macOS can check its command.
    internal static bool TryDirectoryLink(string link, string target, bool windows, Func<System.Diagnostics.ProcessStartInfo, int> run)
    {
        if (!windows) { Directory.CreateSymbolicLink(link, target); return true; }
        var info = new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in new[] { "/c", "mklink", "/J", link, target }) info.ArgumentList.Add(argument);
        return run(info) == 0 && Directory.Exists(link);
    }

    private static bool TryDirectoryLink(string link, string target)
    {
        if (TryDirectoryLink(link, target, OperatingSystem.IsWindows(), info =>
        {
            info.RedirectStandardOutput = true;
            using var child = System.Diagnostics.Process.Start(info)!;
            child.StandardOutput.ReadToEnd();
            child.WaitForExit();
            return child.ExitCode;
        })) return true;
        Console.WriteLine("NOT ASSESSED directory-link: mklink /J could not create a junction; the symlinked-directory case did not run");
        return false;
    }

    private static void DirectoryLinkWindowsBranch()
    {
        // Each half runs only on the host whose branch it exercises (TEST-FOREIGN-OS-BRANCH); the other half is not assessed here.
        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.ProcessStartInfo? seen = null;
            string link = Path.Combine(LayoutFileTests.Root(), "prefs");
            Equal(false, TryDirectoryLink(link, "/real", windows: true, info => { seen = info; return 1; }));
            Equal(false, seen is null);
            Equal("cmd.exe", seen!.FileName);
            Equal("/c|mklink|/J|" + link + "|/real", string.Join('|', seen.ArgumentList));
            Equal(false, Directory.Exists(link));
        }
        else Console.WriteLine("NOT ASSESSED directory-link: the junction command is checked on a Windows host only");
        if (!OperatingSystem.IsWindows())
        {
            string real = LayoutFileTests.Root(), made = Path.Combine(LayoutFileTests.Root(), "alias");
            Equal(true, TryDirectoryLink(made, real, windows: false, _ => throw new InvalidOperationException("macOS must not shell out")));
            Equal(true, new DirectoryInfo(made).LinkTarget is not null);
        }
        else Console.WriteLine("NOT ASSESSED directory-link: the symbolic-link branch is checked on a non-Windows host only");
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
        if (!TryDirectoryLink(link, real)) return;
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

    // ---------------- the Text size (DN-5): cfdw-display v1, one value per installation user ----------------

    private static PreferenceStore Prefs(string root) => new(root, () => new ProjectStore());

    private static string DisplayFile(string root, byte[] image)
    {
        string directory = Path.Combine(root, "display");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "display.json");
        File.WriteAllBytes(path, image);
        return path;
    }

    private static void TextSizeAbsent()
    {
        string root = LayoutFileTests.Root();
        var load = Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None));
        Equal(100, load.Percent);
        Equal("absent", load.Outcome);
        Equal(0, load.Codes.Count);
        Equal(false, load.NeverWrite);
        Equal(false, Directory.Exists(Path.Combine(root, "display")));
    }

    private static void TextSizeRoundTrip()
    {
        string root = LayoutFileTests.Root();
        foreach (int size in new[] { 150, 200, 125, 100 })
        {
            var store = Prefs(root);
            Wait(store.LoadTextSizeAsync(CancellationToken.None));
            var save = Wait(store.SaveTextSizeAsync(size, CancellationToken.None));
            Equal("saved", save.Outcome);
            Equal(true, save.DurabilityConfirmed);
            var again = Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None));
            Equal("restored", again.Outcome);
            Equal(size, again.Percent);
            Equal(0, again.Codes.Count);
        }
        string directory = Path.Combine(root, "display");
        Equal(Mode(directory), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Equal(Mode(Path.Combine(directory, "display.json")), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        // Two instances on one root: the later choice wins after one conflict retry.
        var first = Prefs(root);
        var second = Prefs(root);
        Wait(first.LoadTextSizeAsync(CancellationToken.None));
        Wait(second.LoadTextSizeAsync(CancellationToken.None));
        Equal("saved", Wait(first.SaveTextSizeAsync(125, CancellationToken.None)).Outcome);
        var late = Wait(second.SaveTextSizeAsync(150, CancellationToken.None));
        Equal("saved", late.Outcome);
        Equal(true, late.Retried);
        Equal(150, Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None)).Percent);
        var rejected = Wait(first.SaveTextSizeAsync(175, CancellationToken.None));
        Equal("rejected", rejected.Outcome);
        Equal("DISPLAY-SCHEMA", rejected.Code);
    }

    // Value class (Ruling 206 L4): the document is sound and one member is bad; that member alone reads its default.
    private static readonly string[] ValueErrors =
    [
        """{"format":"cfdw-display","version":1,"textSize":175}""",
        """{"format":"cfdw-display","version":1,"textSize":0}""",
        """{"format":"cfdw-display","version":1,"textSize":150.0}""",
        """{"format":"cfdw-display","version":1,"textSize":"150"}"""
    ];

    // Structure class (L2): the document cannot be trusted as a whole; every setting reads its default.
    private static readonly string[] StructureErrors =
    [
        """{"format":"cfdw-display","version":1}""",
        """{"format":"cfdw-display","version":1,"textSize":150,"textSize":200}""",
        """{"format":"cfdw-display","version":1,"textSize":150,"theme":"dark"}""",
        """{"format":"cfdw-layout","version":1,"textSize":150}""",
        """{"format":"cfdw-display","version":0,"textSize":150}""",
        "﻿{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":150}",
        """{"format":"cfdw-display","version":1,"textSize":15""",
        "\u0001\u0002 not json"
    ];

    private static void TextSizeUnreadable(string[] images)
    {
        foreach (var text in images)
        {
            try
            {
                var original = Encoding.UTF8.GetBytes(text);
                string root = LayoutFileTests.Root();
                string path = DisplayFile(root, original);
                var store = Prefs(root);
                var load = Wait(store.LoadTextSizeAsync(CancellationToken.None));
                Equal(100, load.Percent);
                Equal(true, load.NeverWrite);
                Has(load.Codes, "DISPLAY-SCHEMA");
                Equal("never-write", Wait(store.SaveTextSizeAsync(150, CancellationToken.None)).Outcome);
                Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(text + ": " + error.Message, error);
            }
        }
    }

    private static void TextSizeFuture()
    {
        string root = LayoutFileTests.Root();
        var original = Encoding.UTF8.GetBytes("""{"format":"cfdw-display","version":2,"textSize":175,"theme":"dark"}""");
        string path = DisplayFile(root, original);
        var store = Prefs(root);
        var load = Wait(store.LoadTextSizeAsync(CancellationToken.None));
        Equal(100, load.Percent);
        Equal(true, load.NeverWrite);
        Has(load.Codes, "LAYOUT-VERSION");
        Equal("never-write", Wait(store.SaveTextSizeAsync(150, CancellationToken.None)).Outcome);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
    }

    private static void TextSizePriorRoot()
    {
        // A root written before the display document existed: layout and Recent load as before and keep their bytes
        // when the Text size is saved beside them (expand-only; nothing migrates or rewrites them).
        string root = LayoutFileTests.Root();
        string layoutPath = Path.Combine(root, "layout", "layout.json");
        string recentPath = Path.Combine(root, "recent", "recent.json");
        Directory.CreateDirectory(Path.Combine(root, "layout"));
        File.WriteAllBytes(layoutPath, File.ReadAllBytes(LayoutFileTests.Fixture("example-v1.json")));
        var seed = Prefs(root);
        Wait(seed.LoadRecentAsync(CancellationToken.None));
        Equal("saved", Wait(seed.UpdateRecentAsync(new RecentOp.Add("/abs/prior.foil"), CancellationToken.None)).Outcome);
        var layoutBytes = File.ReadAllBytes(layoutPath);
        var recentBytes = File.ReadAllBytes(recentPath);
        var store = Prefs(root);
        Equal("restored", Wait(store.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None)).Outcome);
        Equal(1, Wait(store.LoadRecentAsync(CancellationToken.None)).Entries.Count);
        Equal(100, Wait(store.LoadTextSizeAsync(CancellationToken.None)).Percent);
        Equal("saved", Wait(store.SaveTextSizeAsync(200, CancellationToken.None)).Outcome);
        Equal(true, File.ReadAllBytes(layoutPath).AsSpan().SequenceEqual(layoutBytes));
        Equal(true, File.ReadAllBytes(recentPath).AsSpan().SequenceEqual(recentBytes));
        var reopened = Prefs(root);
        Equal("restored", Wait(reopened.LoadLayoutAsync(LayoutFileTests.Panes(), CancellationToken.None)).Outcome);
        Equal(200, Wait(reopened.LoadTextSizeAsync(CancellationToken.None)).Percent);
    }

    private static void TextSizeSessionOnly()
    {
        string real = LayoutFileTests.Root();
        string link = Path.Combine(LayoutFileTests.Root(), "prefs");
        if (!TryDirectoryLink(link, real)) return;
        var store = Prefs(link);
        var load = Wait(store.LoadTextSizeAsync(CancellationToken.None));
        Equal(100, load.Percent);
        Equal(true, load.SessionOnly);
        Has(load.Codes, "LAYOUT-SESSION-ONLY");
        Equal("session-only", Wait(store.SaveTextSizeAsync(150, CancellationToken.None)).Outcome);
        Equal(false, Directory.Exists(Path.Combine(real, "display")));
        // A store that cannot persist (session-only) or cannot read the file (never-write) writes nothing.
        string root = LayoutFileTests.Root();
        DisplayFile(root, [9]);
        foreach (var (code, outcome) in new[] { ("DOC-UNSUPPORTED-PERSISTENCE", "session-only"), ("DOC-IO", "never-write") })
        {
            var fake = new CodeStore(code);
            var denied = new PreferenceStore(root, () => fake);
            var refused = Wait(denied.LoadTextSizeAsync(CancellationToken.None));
            Equal(100, refused.Percent);
            Equal(outcome, refused.Outcome);
            Equal(outcome, Wait(denied.SaveTextSizeAsync(150, CancellationToken.None)).Outcome);
            Equal(0, fake.Saves);
        }
        // No file yet and a store without persistence (Windows today): the first save makes the session session-only.
        var unsupported = new CodeStore("DOC-UNSUPPORTED-PERSISTENCE");
        var fresh = new PreferenceStore(LayoutFileTests.Root(), () => unsupported);
        Equal("absent", Wait(fresh.LoadTextSizeAsync(CancellationToken.None)).Outcome);
        Equal("session-only", Wait(fresh.SaveTextSizeAsync(150, CancellationToken.None)).Outcome);
        Equal("session-only", Wait(fresh.SaveTextSizeAsync(200, CancellationToken.None)).Outcome);
        Equal(1, unsupported.Saves);
    }

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

    private static void TextSizeFileSystemException()
    {
        // A file-system exception below the store (not a ContractError) is a failed/DOC-IO outcome, never a throw.
        string root = LayoutFileTests.Root();
        DisplayFile(root, Encoding.UTF8.GetBytes("""{"format":"cfdw-display","version":1,"textSize":150}"""));
        var load = Wait(new PreferenceStore(root, () => new ThrowStore()).LoadTextSizeAsync(CancellationToken.None));
        Equal(100, load.Percent);
        Equal("failed", load.Outcome);
        Has(load.Codes, "DOC-IO");
        Equal(true, load.NeverWrite);
        var save = Wait(new PreferenceStore(LayoutFileTests.Root(), () => new ThrowStore()).SaveTextSizeAsync(150, CancellationToken.None));
        Equal("failed", save.Outcome);
        Equal("DOC-IO", save.Code);
    }

    private static void TextSizeLoadSerialized()
    {
        // A save issued while the startup read is in flight waits for it, so the read cannot leave a stale hash behind.
        string root = LayoutFileTests.Root();
        Equal("saved", Wait(Prefs(root).SaveTextSizeAsync(125, CancellationToken.None)).Outcome);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new HeldReadStore(release);
        var store = new PreferenceStore(root, () => held);
        var load = store.LoadTextSizeAsync(CancellationToken.None);
        var save = store.SaveTextSizeAsync(150, CancellationToken.None);
        save.Wait(TimeSpan.FromMilliseconds(100));
        release.SetResult();
        Equal(125, Wait(load).Percent);
        var first = Wait(save);
        Equal("saved", first.Outcome);
        Equal(false, first.Retried);
        var second = Wait(store.SaveTextSizeAsync(200, CancellationToken.None));
        Equal("saved", second.Outcome);
        Equal(false, second.Retried);
        Equal(200, Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None)).Percent);
    }

    private static void TextSizeSerializeOutOfSet()
    {
        // One rule for the set: the writer refuses an out-of-set value rather than writing a different one.
        bool threw = false;
        try { DisplayPreferences.Serialize(175); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        Equal(true, threw);
        Equal(150, DisplayPreferences.Parse(DisplayPreferences.Serialize(150)).TextSize);
    }

    /// <summary>Every read and save throws an <see cref="IOException"/>, as a failing file system would.</summary>
    private sealed class ThrowStore : IProjectStore
    {
        public void Dispose() { }
        public Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default) =>
            Task.FromException<SaveResult>(new IOException("disk"));
        public Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default) =>
            Task.FromException<ReadResult>(new IOException("disk"));
    }

    /// <summary>Reads and saves through a real store; the first read returns only after <paramref name="release"/>.</summary>
    private sealed class HeldReadStore(TaskCompletionSource release) : IProjectStore
    {
        private int reads;
        public void Dispose() { }
        public async Task<ReadResult> ReadAsync(string path, CancellationToken cancellation = default)
        {
            using var inner = new ProjectStore();
            var read = await inner.ReadAsync(path, cancellation).ConfigureAwait(false);
            if (Interlocked.Increment(ref reads) == 1) await release.Task.ConfigureAwait(false);
            return read;
        }
        public async Task<SaveResult> SaveAsync(string path, SaveRequest request, CancellationToken cancellation = default)
        {
            using var inner = new ProjectStore();
            return await inner.SaveAsync(path, request, cancellation).ConfigureAwait(false);
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

    // Track PRF: the rail comb view in display.json (docs/design/view-preferences.md section 8). Ring: fast; file I/O on a temp folder.

    internal static void RunView()
    {
        Check("PrefStore_Comb_Absent_AutoAnd32_Hidden", ViewAbsent);
        Check("PrefStore_Comb_RoundTrip_EveryStep_DefaultsNotWritten", ViewRoundTrip);
        Check("PrefStore_Comb_SaveDoesNotResetTextSizeOrUnits_AndTheReverse", ViewMerge);
        Check("PrefStore_Comb_ValueError_ThatMemberDefaults_OthersKept_BytesUnchanged", ViewValueError);
        Check("PrefStore_Comb_StructureError_WholeFileDefaults_BytesUnchanged", ViewStructureError);
        Check("PrefStore_Comb_NewerVersion_WholeFileDefaults_NeverOverwritten", ViewNewerVersion);
        Check("PrefStore_Comb_OldFileNewBuild_NoRewriteOnLoad", ViewOldFile);
        Check("PrefStore_Comb_Atomic_FaultBeforePublish_PreviousBytesKept_NoTemp", ViewAtomic);
        Check("PrefStore_Comb_TwoInstances_LaterWinsPerMember_Retried", ViewTwoInstances);
        Check("PrefStore_Comb_UnsupportedOrLinked_SessionOnly_NoThrow", ViewSessionOnly);
        Check("PrefStore_DisplayCodec_Serialize_OutOfSetComb_Throws", ViewSerializeOutOfSet);
        Check("PrefStore_DisplayCodec_ValueError_TextSizeUnitsBadOthersKept", ViewParseValueClass);
    }

    private static string ViewText(string root) => File.ReadAllText(Path.Combine(root, "display", "display.json"));

    private static void ViewAbsent()
    {
        string root = LayoutFileTests.Root();
        var load = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
        Equal(true, load.Scale is null);
        Equal(32, load.Density);
        Equal(false, load.Visible);
        Equal("absent", load.Outcome);
        Equal(0, load.Codes.Count);
        Equal(false, Directory.Exists(Path.Combine(root, "display")));
    }

    private static void ViewRoundTrip()
    {
        string root = LayoutFileTests.Root();
        foreach (double scale in DisplayPreferences.CombScales)
            foreach (int density in DisplayPreferences.CombDensities)
                foreach (bool visible in new[] { false, true })
                {
                    var store = Prefs(root);
                    Wait(store.LoadCombViewAsync(CancellationToken.None));
                    var save = Wait(store.SaveCombViewAsync(scale, density, visible, CancellationToken.None));
                    Equal("saved", save.Outcome);
                    var again = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
                    Equal("restored", again.Outcome);
                    Equal(scale, again.Scale!.Value);
                    Equal(density, again.Density);
                    Equal(visible, again.Visible);
                }
        var auto = Prefs(root);
        Wait(auto.LoadCombViewAsync(CancellationToken.None));
        Equal("saved", Wait(auto.SaveCombViewAsync(null, 32, false, CancellationToken.None)).Outcome);
        string text = ViewText(root);
        Equal(false, text.Contains("combScale") || text.Contains("combDensity") || text.Contains("combVisible"));
        var back = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
        Equal(true, back.Scale is null);
        Equal(32, back.Density);
        Equal(false, back.Visible);
        Equal("saved", Wait(auto.SaveCombViewAsync(5, 64, true, CancellationToken.None)).Outcome);
        Equal(true, ViewText(root).Contains("\"combScale\": 5,") && ViewText(root).Contains("\"combDensity\": 64") && ViewText(root).Contains("\"combVisible\": true"));
        var rejected = Wait(auto.SaveCombViewAsync(3, 32, false, CancellationToken.None));
        Equal("rejected", rejected.Outcome);
        Equal("DISPLAY-SCHEMA", rejected.Code);
        Equal("rejected", Wait(auto.SaveCombViewAsync(5, 48, false, CancellationToken.None)).Outcome);
    }

    private static void ViewMerge()
    {
        string root = LayoutFileTests.Root();
        var first = Prefs(root);
        Wait(first.LoadTextSizeAsync(CancellationToken.None));
        Wait(first.SaveTextSizeAsync(150, CancellationToken.None));
        Wait(first.SaveUnitsAsync(DisplayPreferences.Imperial, CancellationToken.None));
        // A fresh store (a restart) saves only the comb: Text size and units stay.
        var second = Prefs(root);
        Wait(second.LoadCombViewAsync(CancellationToken.None));
        Equal("saved", Wait(second.SaveCombViewAsync(10, 128, true, CancellationToken.None)).Outcome);
        var read = DisplayPreferences.Parse(File.ReadAllBytes(Path.Combine(root, "display", "display.json")));
        Equal(150, read.TextSize);
        Equal(DisplayPreferences.Imperial, read.Units);
        Equal(10.0, read.CombScale!.Value);
        Equal(128, read.CombDensity);
        Equal(true, read.CombVisible);
        // The reverse: a Text size change keeps the comb.
        var third = Prefs(root);
        Wait(third.LoadTextSizeAsync(CancellationToken.None));
        Equal("saved", Wait(third.SaveTextSizeAsync(200, CancellationToken.None)).Outcome);
        var after = DisplayPreferences.Parse(File.ReadAllBytes(Path.Combine(root, "display", "display.json")));
        Equal(200, after.TextSize);
        Equal(DisplayPreferences.Imperial, after.Units);
        Equal(10.0, after.CombScale!.Value);
        Equal(128, after.CombDensity);
        Equal(true, after.CombVisible);
        // A comb save issued before the startup read finished still keeps the file's Text size.
        var fourth = Prefs(root);
        var save = fourth.SaveCombViewAsync(null, 16, false, CancellationToken.None);
        Equal("saved", Wait(save).Outcome);
        Equal(200, DisplayPreferences.Parse(File.ReadAllBytes(Path.Combine(root, "display", "display.json"))).TextSize);
    }

    private static void ViewValueError()
    {
        const string head = """{"format":"cfdw-display","version":1,"textSize":150,"units":"imperial",""";
        (string Tail, double? Scale, int Density, bool Visible)[] cases =
        [
            ("\"combScale\":3,\"combDensity\":64,\"combVisible\":true}", null, 64, true),
            ("\"combScale\":\"5\",\"combDensity\":64,\"combVisible\":true}", null, 64, true),
            ("\"combScale\":5.5,\"combDensity\":64,\"combVisible\":true}", null, 64, true),
            ("\"combScale\":null,\"combDensity\":64,\"combVisible\":true}", null, 64, true),
            ("\"combScale\":5,\"combDensity\":48,\"combVisible\":true}", 5, 32, true),
            ("\"combScale\":5,\"combDensity\":\"64\",\"combVisible\":true}", 5, 32, true),
            ("\"combScale\":5,\"combDensity\":64.0,\"combVisible\":true}", 5, 32, true),
            ("\"combScale\":5,\"combDensity\":64,\"combVisible\":\"yes\"}", 5, 64, false),
            ("\"combScale\":5,\"combDensity\":64,\"combVisible\":1}", 5, 64, false)
        ];
        foreach (var (tail, scale, density, visible) in cases)
        {
            try
            {
                var original = Encoding.UTF8.GetBytes(head + tail);
                string root = LayoutFileTests.Root();
                string path = DisplayFile(root, original);
                var store = Prefs(root);
                var load = Wait(store.LoadCombViewAsync(CancellationToken.None));
                Equal(scale, load.Scale);
                Equal(density, load.Density);
                Equal(visible, load.Visible);
                Equal(true, load.NeverWrite);
                Has(load.Codes, "DISPLAY-SCHEMA");
                var text = Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None));
                Equal(150, text.Percent);
                var units = Wait(Prefs(root).LoadUnitsAsync(CancellationToken.None));
                Equal(DisplayPreferences.Imperial, units.Units);
                Equal("never-write", Wait(store.SaveCombViewAsync(10, 16, true, CancellationToken.None)).Outcome);
                Equal("never-write", Wait(store.SaveTextSizeAsync(200, CancellationToken.None)).Outcome);
                Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(tail + ": " + error.Message, error);
            }
        }
    }

    private static void ViewStructureError()
    {
        string[] images =
        [
            """{"format":"cfdw-display","version":1,"textSize":150,"combScale":5,"combScale":10}""",
            """{"format":"cfdw-display","version":1,"textSize":150,"combScale":5,"combTheme":"dark"}""",
            """{"format":"cfdw-layout","version":1,"textSize":150,"combScale":5}""",
            """{"format":"cfdw-display","version":0,"textSize":150,"combScale":5}""",
            """{"format":"cfdw-display","combScale":5}""",
            "﻿{\"format\":\"cfdw-display\",\"version\":1,\"textSize\":150,\"combScale\":5}",
            """{"format":"cfdw-display","version":1,"textSize":150,"combScale":5""",
            "\u0001\u0002 not json",
            """{"format":"cfdw-display","version":1,"textSize":150,"pad":"%""" + new string('x', 4200) + "\"}"
        ];
        foreach (var text in images)
        {
            try
            {
                var original = Encoding.UTF8.GetBytes(text);
                string root = LayoutFileTests.Root();
                string path = DisplayFile(root, original);
                var store = Prefs(root);
                var load = Wait(store.LoadCombViewAsync(CancellationToken.None));
                Equal(true, load.Scale is null);
                Equal(32, load.Density);
                Equal(false, load.Visible);
                Equal(true, load.NeverWrite);
                Has(load.Codes, "DISPLAY-SCHEMA");
                Equal(100, Wait(Prefs(root).LoadTextSizeAsync(CancellationToken.None)).Percent);
                Equal("never-write", Wait(store.SaveCombViewAsync(10, 16, true, CancellationToken.None)).Outcome);
                Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(text[..Math.Min(60, text.Length)] + ": " + error.Message, error);
            }
        }
    }

    private static void ViewNewerVersion()
    {
        string root = LayoutFileTests.Root();
        var original = Encoding.UTF8.GetBytes("""{"format":"cfdw-display","version":2,"textSize":150,"combScale":5,"combDensity":64,"combVisible":true}""");
        string path = DisplayFile(root, original);
        string before = Identity.Sha256(original);
        var store = Prefs(root);
        var load = Wait(store.LoadCombViewAsync(CancellationToken.None));
        Equal(true, load.Scale is null);
        Equal(32, load.Density);
        Equal(false, load.Visible);
        Equal(true, load.NeverWrite);
        Has(load.Codes, "LAYOUT-VERSION");
        Equal("never-write", Wait(store.SaveCombViewAsync(10, 16, true, CancellationToken.None)).Outcome);
        Equal("never-write", Wait(store.SaveCombViewAsync(null, 32, false, CancellationToken.None)).Outcome);
        Equal(before, Identity.Sha256(File.ReadAllBytes(path)));
    }

    private static void ViewOldFile()
    {
        string root = LayoutFileTests.Root();
        var original = Encoding.UTF8.GetBytes("""{"format":"cfdw-display","version":1,"textSize":125}""");
        string path = DisplayFile(root, original);
        var load = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
        Equal(true, load.Scale is null);
        Equal(32, load.Density);
        Equal(false, load.Visible);
        Equal("restored", load.Outcome);
        Equal(false, load.NeverWrite);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
    }

    private static void ViewAtomic()
    {
        string root = LayoutFileTests.Root();
        var seeded = Prefs(root);
        Wait(seeded.LoadTextSizeAsync(CancellationToken.None));
        Equal("saved", Wait(seeded.SaveCombViewAsync(5, 64, true, CancellationToken.None)).Outcome);
        string path = Path.Combine(root, "display", "display.json");
        var original = File.ReadAllBytes(path);
        var cts = new CancellationTokenSource();
        var store = new PreferenceStore(root, () => new ProjectStore(new StoreHooks
        {
            OnStage = stage => { if (stage == StoreStage.BeforePublish) cts.Cancel(); }
        }));
        Wait(store.LoadCombViewAsync(CancellationToken.None));
        var save = Wait(store.SaveCombViewAsync(10, 16, false, cts.Token));
        Equal("cancelled", save.Outcome);
        Equal(true, File.ReadAllBytes(path).AsSpan().SequenceEqual(original));
        Equal(0, Directory.GetFiles(Path.Combine(root, "display"), ".cfd-*.tmp").Length);
        // A clean retry replaces the whole document.
        var retry = Prefs(root);
        Wait(retry.LoadCombViewAsync(CancellationToken.None));
        Equal("saved", Wait(retry.SaveCombViewAsync(10, 16, false, CancellationToken.None)).Outcome);
        Equal(0, Directory.GetFiles(Path.Combine(root, "display"), ".cfd-*.tmp").Length);
        var read = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
        Equal(10.0, read.Scale!.Value);
        Equal(16, read.Density);
    }

    private static void ViewTwoInstances()
    {
        string root = LayoutFileTests.Root();
        var first = Prefs(root);
        var second = Prefs(root);
        Wait(first.LoadCombViewAsync(CancellationToken.None));
        Wait(second.LoadCombViewAsync(CancellationToken.None));
        Equal("saved", Wait(first.SaveTextSizeAsync(125, CancellationToken.None)).Outcome);
        var late = Wait(second.SaveCombViewAsync(20, 128, true, CancellationToken.None));
        Equal("saved", late.Outcome);
        Equal(true, late.Retried);
        var read = DisplayPreferences.Parse(File.ReadAllBytes(Path.Combine(root, "display", "display.json")));
        Equal(125, read.TextSize);
        Equal(20.0, read.CombScale!.Value);
        Equal(128, read.CombDensity);
    }

    private static void ViewSessionOnly()
    {
        string real = LayoutFileTests.Root();
        string link = Path.Combine(LayoutFileTests.Root(), "prefs");
        Directory.CreateSymbolicLink(link, real);
        var store = Prefs(link);
        var load = Wait(store.LoadCombViewAsync(CancellationToken.None));
        Equal(true, load.SessionOnly);
        Equal("session-only", Wait(store.SaveCombViewAsync(5, 64, true, CancellationToken.None)).Outcome);
        Equal(false, Directory.Exists(Path.Combine(real, "display")));
    }

    private static void ViewSerializeOutOfSet()
    {
        foreach (var bad in new Action[]
        {
            () => DisplayPreferences.Serialize(100, combScale: 3),
            () => DisplayPreferences.Serialize(100, combDensity: 48)
        })
        {
            bool threw = false;
            try { bad(); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Equal(true, threw);
        }
        var round = DisplayPreferences.Parse(DisplayPreferences.Serialize(125, DisplayPreferences.Metric, 0.5, 16, true));
        Equal(0.5, round.CombScale!.Value);
        Equal(16, round.CombDensity);
        Equal(true, round.CombVisible);
        Equal(false, round.NeverWrite);
    }

    private static void ViewParseValueClass()
    {
        var bad = DisplayPreferences.Parse("""{"format":"cfdw-display","version":1,"textSize":175,"units":"furlongs","combDensity":64}"""u8);
        Equal(100, bad.TextSize);
        Equal(DisplayPreferences.Metric, bad.Units);
        Equal(64, bad.CombDensity);
        Equal(true, bad.NeverWrite);
        Equal("DISPLAY-SCHEMA", bad.Codes[0]);
    }
}
