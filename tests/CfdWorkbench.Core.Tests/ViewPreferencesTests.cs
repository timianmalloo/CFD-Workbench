using System.Text;
using CfdWorkbench.Core;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Track PRF: the rail comb's scale, density and on/off live in display.json beside Text size and units
// (docs/design/view-preferences.md section 8). Ring: fast. Cost: file I/O on a temp folder, well under 1 s per check.
internal static class ViewPreferencesTests
{
    internal static void Run()
    {
        Check("PrefStore_Comb_Absent_AutoAnd32_Hidden", Absent);
        Check("PrefStore_Comb_RoundTrip_EveryStep_DefaultsNotWritten", RoundTrip);
        Check("PrefStore_Comb_SaveDoesNotResetTextSizeOrUnits_AndTheReverse", Merge);
        Check("PrefStore_Comb_ValueError_ThatMemberDefaults_OthersKept_BytesUnchanged", ValueError);
        Check("PrefStore_Comb_StructureError_WholeFileDefaults_BytesUnchanged", StructureError);
        Check("PrefStore_Comb_NewerVersion_WholeFileDefaults_NeverOverwritten", NewerVersion);
        Check("PrefStore_Comb_OldFileNewBuild_NoRewriteOnLoad", OldFile);
        Check("PrefStore_Comb_Atomic_FaultBeforePublish_PreviousBytesKept_NoTemp", Atomic);
        Check("PrefStore_Comb_TwoInstances_LaterWinsPerMember_Retried", TwoInstances);
        Check("PrefStore_Comb_UnsupportedOrLinked_SessionOnly_NoThrow", SessionOnly);
        Check("DisplayPreferences_Serialize_OutOfSetComb_Throws", SerializeOutOfSet);
        Check("DisplayPreferences_ValueError_TextSizeUnitsBadOthersKept", ParseValueClass);
    }

    private static PreferenceStore Prefs(string root) => new(root, () => new ProjectStore());

    private static T Wait<T>(Task<T> task)
    {
        if (!task.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
        return task.Result;
    }

    private static string DisplayFile(string root, byte[] image)
    {
        string directory = Path.Combine(root, "display");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "display.json");
        File.WriteAllBytes(path, image);
        return path;
    }

    private static string Text(string root) => File.ReadAllText(Path.Combine(root, "display", "display.json"));

    private static void Absent()
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

    private static void RoundTrip()
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
        string text = Text(root);
        Equal(false, text.Contains("combScale") || text.Contains("combDensity") || text.Contains("combVisible"));
        var back = Wait(Prefs(root).LoadCombViewAsync(CancellationToken.None));
        Equal(true, back.Scale is null);
        Equal(32, back.Density);
        Equal(false, back.Visible);
        Equal("saved", Wait(auto.SaveCombViewAsync(5, 64, true, CancellationToken.None)).Outcome);
        Equal(true, Text(root).Contains("\"combScale\": 5,") && Text(root).Contains("\"combDensity\": 64") && Text(root).Contains("\"combVisible\": true"));
        var rejected = Wait(auto.SaveCombViewAsync(3, 32, false, CancellationToken.None));
        Equal("rejected", rejected.Outcome);
        Equal("DISPLAY-SCHEMA", rejected.Code);
        Equal("rejected", Wait(auto.SaveCombViewAsync(5, 48, false, CancellationToken.None)).Outcome);
    }

    private static void Merge()
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

    private static void ValueError()
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

    private static void StructureError()
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

    private static void NewerVersion()
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

    private static void OldFile()
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

    private static void Atomic()
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

    private static void TwoInstances()
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

    private static void SessionOnly()
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

    private static void SerializeOutOfSet()
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

    private static void ParseValueClass()
    {
        var bad = DisplayPreferences.Parse("""{"format":"cfdw-display","version":1,"textSize":175,"units":"furlongs","combDensity":64}"""u8);
        Equal(100, bad.TextSize);
        Equal(DisplayPreferences.Metric, bad.Units);
        Equal(64, bad.CombDensity);
        Equal(true, bad.NeverWrite);
        Equal("DISPLAY-SCHEMA", bad.Codes[0]);
    }

    private static void Has(IReadOnlyList<string> codes, string code)
    {
        if (!codes.Contains(code)) throw new InvalidOperationException($"missing {code} in [{string.Join(",", codes)}]");
    }
}
