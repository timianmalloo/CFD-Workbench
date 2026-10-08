using System.Text;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

internal static class SectionLibraryTests
{
    private static readonly Provenance Origin = new("gen:naca-0012", false);

    internal static void Run()
    {
        Check("Library_Save_PublishesHashNamedFile", () =>
        {
            string root = NewRoot();
            var entry = new SectionLibrary(root).Save("My root", Block(), Origin);
            Equal(Path.Combine(root, entry.Hash + ".foil"), Directory.GetFiles(root, "*.foil").Single());
            Equal(Identity.Sha256(entry.Bytes), entry.Hash);
        });
        Check("Library_EntryBytes_ParseAsStandaloneSection", () =>
        {
            var entry = new SectionLibrary(NewRoot()).Save("Kite root", Block(), Origin);
            var parsed = FoilSource.Parse(entry.Bytes);
            Equal(true, parsed.IsParsed);
            Equal("section", parsed.Definition!.Kind);
            Equal("Kite root", parsed.Definition.Name);
            Equal(1, parsed.Definition.Profiles.Length);
        });
        Check("Library_DuplicateIgnoringCaseNfc_Refused", () =>
        {
            string root = NewRoot(); var library = new SectionLibrary(root);
            library.Save("Caf\u00e9", Block(), Origin);
            Refuses("LIB-NAME-DUPLICATE", () => library.Save("CAFE\u0301", Block(), Origin));
            Equal(1, Directory.GetFiles(root, "*.foil").Length);
        });
        Check("Library_EmptyName_Refused", () =>
        {
            string root = NewRoot();
            Refuses("LIB-NAME-EMPTY", () => new SectionLibrary(root).Save(" \t ", Block(), Origin));
            Equal(0, Directory.GetFiles(root).Length);
        });
        Check("Library_CrossingSection_RefusedInvalid", () =>
        {
            string root = NewRoot();
            string block = Encoding.UTF8.GetString(Block()).Replace("(0.15, 0.055)", "(0.15, -3)", StringComparison.Ordinal);
            Refuses("LIB-SECTION-INVALID", () => new SectionLibrary(root).Save("Crossed", Encoding.UTF8.GetBytes(block), Origin));
            Equal(0, Directory.GetFiles(root).Length);
        });
        Check("Library_StaleClaim_ReportedNotDeleted", () =>
        {
            string root = NewRoot(), claim = Path.Combine(root, ProjectStore.ClaimName());
            File.WriteAllBytes(claim, [7]);
            Refuses("LIB-CLAIM-HELD", () => new SectionLibrary(root).Save("Locked", Block(), Origin));
            Equal((byte)7, File.ReadAllBytes(claim)[0]);
            Equal(0, Directory.GetFiles(root, "*.foil").Length);
        });
        Check("Library_HashNotName_ListedAsProblem", () =>
        {
            string root = NewRoot();
            byte[] bytes = Standalone("Wrong");
            Equal(true, FoilSource.Parse(bytes).IsParsed);
            File.WriteAllBytes(Path.Combine(root, new string('0', 64) + ".foil"), bytes);
            var scan = new SectionLibrary(root).Scan();
            Equal(0, scan.Entries.Count); Equal(1, scan.Problems.Count);
            Equal(true, scan.Problems[0].Reason.Contains("damaged", StringComparison.OrdinalIgnoreCase));
        });
        Check("Library_ScanNameCollision_BothReported", () =>
        {
            string root = NewRoot();
            WriteEntry(root, "Same", Origin);
            WriteEntry(root, "same", new Provenance("gen:naca-4412", false));
            var scan = new SectionLibrary(root).Scan();
            Equal(0, scan.Entries.Count); Equal(2, scan.Problems.Count);
            const string Copy199 = "” appears twice in My sections. Neither can be used until one file is removed.";
            Equal(string.Join("|", new[] { "“Same" + Copy199, "“same" + Copy199 }.Order(StringComparer.Ordinal)),
                string.Join("|", scan.Problems.Select(item => item.Reason).Order(StringComparer.Ordinal)));
        });
        Check("Library_OversizedFile_SkippedAsProblem", () =>
        {
            string root = NewRoot();
            File.WriteAllBytes(Path.Combine(root, new string('0', 64) + ".foil"), new byte[1_048_577]);
            var scan = new SectionLibrary(root).Scan();
            Equal(0, scan.Entries.Count); Equal(1, scan.Problems.Count);
        });
        Check("Library_WriteFails_NothingPublished", () =>
        {
            string root = NewRoot();
            var library = new SectionLibrary(root, () => new ProjectStore(new StoreHooks { WriteFragment = 2, FailWriteAfter = 2 }));
            Refuses("LIB-IO", () => library.Save("No file", Block(), Origin));
            Equal(0, Directory.GetFiles(root).Length);
        });
        Check("Library_UnprovedPlatform_RefusedNothingSaved", () =>
        {
            string root = NewRoot();
            var library = new SectionLibrary(root, () => new ProjectStore(new StoreHooks { PublicationError = 45 }));
            Refuses("DOC-UNSUPPORTED-PERSISTENCE", () => library.Save("No file", Block(), Origin));
            Equal(0, Directory.GetFiles(root).Length);
        });
        Check("Library_SecondInstanceSameRoot_EntryListed", () =>
        {
            string root = NewRoot();
            new SectionLibrary(root).Save("Available", Block(), Origin);
            var scan = new SectionLibrary(root).Scan();
            Equal(1, scan.Entries.Count); Equal("Available", scan.Entries[0].Name);
        });
        Check("Library_UserFileHeldReader_SurvivesReplaceByRename", () =>
        {
            // POSIX ignores share modes, so this passes on macOS even with FileShare.Read; on Windows the replace
            // fails (Win32 32) unless the reader shares Delete (Ruling 145 (1)).
            string root = NewRoot(), path = Path.Combine(root, "held.foil"), next = Path.Combine(root, "next.tmp");
            File.WriteAllBytes(path, [1, 2, 3]);
            File.WriteAllBytes(next, [9, 8]);
            using (var held = UserFile.OpenRead(path))
            {
                File.Move(next, path, overwrite: true);
                var old = new byte[3];
                Equal(3, held.Read(old, 0, 3));
                Equal((byte)1, old[0]); Equal((byte)3, old[2]);
            }
            using var fresh = UserFile.OpenRead(path);
            Equal(2L, fresh.Length); Equal((byte)9, (byte)fresh.ReadByte());
        });
    }

    private static string NewRoot()
    {
        return TestTemp.NewDirectory("library-");
    }
    private static byte[] Block()
    {
        string path = Path.Combine(PlacementTests.RepoRoot(), "docs", "examples", "foildsl", "foil-basic.foil");
        return Encoding.UTF8.GetBytes(SectionDraftTests.ProfileBlock(File.ReadAllText(path), "section-a"));
    }
    private static byte[] Standalone(string name) => Encoding.UTF8.GetBytes(
        "foildsl \"4.1\"\nsection " + Jcs.Quote(name) + " {\n  evaluator \"cfdw-cv\" \"2\"\n" +
        Encoding.UTF8.GetString(Block()).Split('{', 2)[1].TrimEnd()[..^1] + "\n}\n");
    private static void WriteEntry(string root, string name, Provenance provenance)
    {
        byte[] bytes = new SectionLibrary(NewRoot()).Save(name, Block(), provenance).Bytes;
        File.WriteAllBytes(Path.Combine(root, Identity.Sha256(bytes) + ".foil"), bytes);
    }
}
