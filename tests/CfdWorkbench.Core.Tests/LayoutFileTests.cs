using System.Text;
using CfdWorkbench.Persistence;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

// Track P1 named checks for the layout codec and the recent-list codec (docs/design/app-shell.md §9).
internal static class LayoutFileTests
{
    internal static void Run()
    {
        Check("LayoutParse_Corrupt_ReturnsPresetsWithCode", Corrupt);
        Check("LayoutCodec_RandomBytes_NeverThrows", RandomBytes);
        Check("LayoutParse_BadWorkspace_OthersRestored", BadWorkspace);
        Check("LayoutParse_UnknownPane_DroppedOthersKept", UnknownPane);
        Check("LayoutParse_MissingPane_PlacedByPreset", MissingPane);
        Check("LayoutParse_RetiredSamplesPane_DroppedOthersKept", RetiredSamplesPane);
        Check("LayoutParse_OutOfRange_Clamped", OutOfRange);
        Check("RecentParse_RelativePath_Dropped", RelativeRecent);
        Check("LayoutCodec_DeepestValid_SerializesAndReaderRejectsDepth9", Deepest);
    }

    internal static IReadOnlySet<string> Panes() => new HashSet<string>(["properties", "browser", "points", "messages"], StringComparer.Ordinal);

    internal static string Fixture(string name)
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "layout", name),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "layout", name))
        ];
        foreach (var candidate in candidates)
            if (File.Exists(candidate)) return candidate;
        throw new FileNotFoundException(name);
    }

    internal static string Root()
    {
        string temp = Path.GetTempPath();
        if (temp.StartsWith("/tmp/", StringComparison.Ordinal)) temp = "/private" + temp;
        string root = Path.Combine(temp, "p1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    internal static void AssertPresets(LayoutDocument document)
    {
        Equal(WorkspaceId.Planform, document.Active);
        Equal(3, document.Workspaces.Count);
        AssertPreset(document.Workspaces[0], WorkspaceId.Planform, true, false, ViewArrangement.Plan3d);
        AssertPreset(document.Workspaces[1], WorkspaceId.Precision, true, true, ViewArrangement.Plan3d);
        AssertPreset(document.Workspaces[2], WorkspaceId.Review, false, false, ViewArrangement.Four);
    }

    private static void Corrupt()
    {
        AssertSchema(File.ReadAllBytes(Fixture("corrupt.txt")));
        AssertSchema("{"u8.ToArray());
        AssertSchema("""{"format":"nope","version":1,"active":"planform","workspaces":[]}"""u8.ToArray());
        AssertSchema("""{"format":"cfdw-layout","version":0,"active":"planform","workspaces":[]}"""u8.ToArray());
        AssertSchema("""{"format":"cfdw-layout","version":1.5,"active":"planform","workspaces":[]}"""u8.ToArray());
        AssertSchema("""{"format":"cfdw-layout","version":"1","active":"planform","workspaces":[]}"""u8.ToArray());
        AssertSchema("""{"format":"cfdw-layout","version":1,"active":"planform","workspaces":[],"extra":1}"""u8.ToArray());
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat("""{"format":"cfdw-layout","version":1,"active":"planform","workspaces":[]}"""u8.ToArray()).ToArray();
        AssertSchema(bom);
        var oversized = new byte[LayoutCodec.MaxBytes + 1];
        oversized[0] = (byte)'{';
        AssertSchema(oversized);
        AssertSchema(File.ReadAllBytes(Fixture("depth-9.json")));
    }

    private static void AssertSchema(byte[] bytes)
    {
        var parsed = LayoutCodec.Parse(bytes, Panes());
        Equal(false, parsed.NeverWrite);
        Has(parsed.Codes, "LAYOUT-SCHEMA");
        AssertPresets(parsed.Document);
    }

    private static void RandomBytes()
    {
        var random = new Random(12345);
        for (int i = 0; i < 40; i++)
        {
            var bytes = new byte[random.Next(0, 400)];
            random.NextBytes(bytes);
            LayoutCodec.Peek(bytes);
            var parsed = LayoutCodec.Parse(bytes, Panes());
            LayoutCodec.Serialize(parsed.Document);
            RecentList.Parse(bytes);
            RecentList.Serialize(parsed.Document.Workspaces.Count == 0 ? [] : []);
        }
        var big = new byte[80_000];
        random.NextBytes(big);
        LayoutCodec.Parse(big, Panes());
        RecentList.Parse(big);
        var nested = Encoding.UTF8.GetBytes(new string('[', 40) + new string(']', 40));
        LayoutCodec.Parse(nested, Panes());
        var presets = LayoutCodec.Serialize(LayoutCodec.Presets(WorkspaceId.Planform, Panes()));
        EqualBytes(presets, LayoutCodec.Serialize(LayoutCodec.Parse(presets, Panes()).Document));
    }

    private static void BadWorkspace()
    {
        var parsed = LayoutCodec.Parse(File.ReadAllBytes(Fixture("bad-workspace.json")), Panes());
        Has(parsed.Codes, "LAYOUT-WORKSPACE");
        HasNot(parsed.Codes, "LAYOUT-SCHEMA");
        Equal(false, parsed.NeverWrite);
        Equal(2, parsed.Document.Workspaces.Count);
        var planform = parsed.Document.Workspaces.Single(workspace => workspace.Id == WorkspaceId.Planform);
        Equal(300d, Left(planform));
        var precision = parsed.Document.Workspaces.Single(workspace => workspace.Id == WorkspaceId.Precision);
        Equal(true, precision.Regions.Single(region => region.Id == RegionId.Bottom).Open);
        Equal(260d, Left(precision));
        var integer = LayoutCodec.Parse("""{"format":"cfdw-layout","version":1,"active":"planform","workspaces":[{"id":"planform","views":{"arrangement":"plan-3d","single":"plan"},"regions":[{"id":"left","open":true,"size":280,"groups":[{"panes":["properties","browser"],"active":"properties","share":1}]},{"id":"bottom","open":false,"size":190,"groups":[{"panes":["points","messages"],"active":"points","share":1}]},{"id":"right","open":false,"size":260,"groups":[]}],"floats":[],"closed":[]},{"id":1,"views":{"arrangement":"plan-3d","single":"plan"},"regions":[],"floats":[],"closed":[]}]}"""u8, Panes());
        Has(integer.Codes, "LAYOUT-WORKSPACE");
        Equal(1, integer.Document.Workspaces.Count);
        Equal(280d, Left(integer.Document.Workspaces[0]));
    }

    // A layout saved while the "3D samples" document existed names it as a pane, as the active pane and as closed: it drops, nothing throws.
    private static void RetiredSamplesPane()
    {
        const string saved = """
        {"format":"cfdw-layout","version":1,"active":"planform","workspaces":[{"id":"planform",
         "views":{"arrangement":"plan-3d","single":"plan"},
         "regions":[
          {"id":"right","open":false,"size":260,"groups":[{"panes":["properties"],"active":"properties","share":1}]},
          {"id":"left","open":true,"size":260,"groups":[{"panes":["3d-samples","browser"],"active":"3d-samples","share":1}]},
          {"id":"bottom","open":false,"size":190,"groups":[{"panes":["points","messages"],"active":"points","share":1}]}],
         "floats":[],"closed":["3d-samples"]}]}
        """;
        var parsed = LayoutCodec.Parse(Encoding.UTF8.GetBytes(saved), Panes());
        Has(parsed.Codes, "LAYOUT-PANE");
        Equal(true, parsed.DroppedPanes >= 1);
        var workspace = parsed.Document.Workspaces.Single();
        Equal(0, Count(workspace, "3d-samples"));
        Equal(1, Count(workspace, "browser"));
        Equal(1, Count(workspace, "properties"));
        Equal(1, Count(workspace, "points"));
        Equal(1, Count(workspace, "messages"));
    }

    private static void UnknownPane()
    {
        var parsed = LayoutCodec.Parse(File.ReadAllBytes(Fixture("unknown-pane.json")), Panes());
        Has(parsed.Codes, "LAYOUT-PANE");
        Equal(true, parsed.DroppedPanes >= 1);
        var workspace = parsed.Document.Workspaces.Single();
        Equal(0, Count(workspace, "ghost"));
        Equal(1, Count(workspace, "browser"));
        Equal(1, Count(workspace, "properties"));
        Equal(1, Count(workspace, "points"));
        Equal(1, Count(workspace, "messages"));
        Equal(RegionId.Left, workspace.Regions[0].Id);
        Equal("browser", workspace.Regions[0].Groups[0].Panes[0]);
        Equal(true, workspace.Regions[0].Groups[0].Panes.Contains("properties"));
        Equal(0, workspace.Closed.Count);
    }

    private static void MissingPane()
    {
        var parsed = LayoutCodec.Parse(File.ReadAllBytes(Fixture("missing-pane.json")), Panes());
        Has(parsed.Codes, "LAYOUT-PANE");
        Equal(true, parsed.DroppedPanes >= 1);
        var workspace = parsed.Document.Workspaces.Single();
        Equal(300d, Left(workspace));
        var bottom = workspace.Regions.Single(region => region.Id == RegionId.Bottom);
        Equal("points", bottom.Groups[0].Panes[0]);
        Equal("messages", bottom.Groups[0].Panes[1]);
        Equal("points", bottom.Groups[0].Active);
        Equal(1, Count(workspace, "messages"));
    }

    private static void OutOfRange()
    {
        var parsed = LayoutCodec.Parse(File.ReadAllBytes(Fixture("out-of-range.json")), Panes());
        Has(parsed.Codes, "LAYOUT-CLAMPED");
        Equal(true, parsed.Clamped >= 7);
        var workspace = parsed.Document.Workspaces.Single();
        Equal(200d, workspace.Regions.Single(region => region.Id == RegionId.Left).Size);
        Equal(1d, workspace.Regions.Single(region => region.Id == RegionId.Left).Groups[0].Share);
        Equal(480d, workspace.Regions.Single(region => region.Id == RegionId.Bottom).Size);
        Equal(1d, workspace.Regions.Single(region => region.Id == RegionId.Bottom).Groups[0].Share);
        Equal(200d, workspace.Regions.Single(region => region.Id == RegionId.Right).Size);
        var floated = workspace.Floats.Single();
        Equal(160d, floated.Width);
        Equal(8192d, floated.Height);
        Equal("properties", floated.Active);
    }

    private static void RelativeRecent()
    {
        var parsed = RecentList.Parse(File.ReadAllBytes(Fixture("recent-relative.json")));
        Has(parsed.Codes, "RECENT-SCHEMA");
        Equal(false, parsed.NeverWrite);
        Equal(1, parsed.Entries.Count);
        Equal("/abs/kept.foil", parsed.Entries[0].Path);
    }

    private static void Deepest()
    {
        Equal(8, LayoutCodec.ReaderMaxDepth);
        var built = Example();
        var bytes = LayoutCodec.Serialize(built);
        Equal(true, bytes.Length > 2);
        Equal(-1, Array.IndexOf(bytes, (byte)'\r'));
        Equal(false, bytes[0] == 0xEF);
        var round = LayoutCodec.Parse(bytes, Panes());
        HasNot(round.Codes, "LAYOUT-SCHEMA");
        Equal(false, round.NeverWrite);
        EqualBytes(bytes, LayoutCodec.Serialize(round.Document));
        var fixture = LayoutCodec.Parse(File.ReadAllBytes(Fixture("example-v1.json")), Panes());
        HasNot(fixture.Codes, "LAYOUT-SCHEMA");
        Equal(false, fixture.NeverWrite);
        var plan = fixture.Document.Workspaces.Single();
        Equal(1620, plan.Floats.Single().X);
        Equal(140, plan.Floats.Single().Y);
        Equal("properties", plan.Floats.Single().Active);
        Equal(RegionId.Left, plan.Floats.Single().Origin.Region);
        Equal(2560, plan.Floats.Single().Screen.Width);
        Equal("browser", plan.Regions.Single(region => region.Id == RegionId.Left).Groups[0].Panes[0]);
        var deep = LayoutCodec.Parse(File.ReadAllBytes(Fixture("depth-9.json")), Panes());
        Has(deep.Codes, "LAYOUT-SCHEMA");
        Equal(false, deep.NeverWrite);
        AssertPresets(deep.Document);
    }

    private static LayoutDocument Example() => new(WorkspaceId.Planform,
    [
        new WorkspaceLayout(WorkspaceId.Planform, new WorkspaceViews(ViewArrangement.Plan3d, SingleView.Plan),
        [
            new RegionLayout(RegionId.Left, true, 260, [new PaneGroup(["browser"], "browser", 1)]),
            new RegionLayout(RegionId.Bottom, false, 190, [new PaneGroup(["points", "messages"], "points", 1)]),
            new RegionLayout(RegionId.Right, false, 260, [])
        ],
        [
            new FloatLayout(["properties"], "properties", 1620, 140, 260, 520, new ScreenBounds(1512, 0, 2560, 1440), new FloatOrigin(RegionId.Left, 0, 0))
        ],
        [])
    ]);

    private static void AssertPreset(WorkspaceLayout workspace, WorkspaceId id, bool leftOpen, bool bottomOpen, ViewArrangement arrangement)
    {
        Equal(id, workspace.Id);
        Equal(arrangement, workspace.Views.Arrangement);
        Equal(SingleView.Plan, workspace.Views.Single);
        Equal(leftOpen, workspace.Regions.Single(region => region.Id == RegionId.Left).Open);
        Equal(260d, Left(workspace));
        Equal(2, workspace.Regions.Single(region => region.Id == RegionId.Left).Groups[0].Panes.Count);
        Equal("properties", workspace.Regions.Single(region => region.Id == RegionId.Left).Groups[0].Active);
        var bottom = workspace.Regions.Single(region => region.Id == RegionId.Bottom);
        Equal(bottomOpen, bottom.Open);
        Equal(190d, bottom.Size);
        Equal("points", bottom.Groups[0].Active);
        var right = workspace.Regions.Single(region => region.Id == RegionId.Right);
        Equal(false, right.Open);
        Equal(0, right.Groups.Count);
        Equal(0, workspace.Floats.Count);
        Equal(0, workspace.Closed.Count);
        foreach (var pane in new[] { "properties", "browser", "points", "messages" })
            Equal(1, Count(workspace, pane));
    }

    private static double Left(WorkspaceLayout workspace) => workspace.Regions.Single(region => region.Id == RegionId.Left).Size;

    private static int Count(WorkspaceLayout workspace, string pane)
    {
        int count = workspace.Closed.Count(item => item == pane);
        count += workspace.Floats.Sum(floated => floated.Panes.Count(item => item == pane));
        count += workspace.Regions.Sum(region => region.Groups.Sum(group => group.Panes.Count(item => item == pane)));
        return count;
    }

    private static void Has(IReadOnlyList<string> codes, string code)
    {
        if (!codes.Contains(code)) throw new InvalidOperationException("missing " + code + " [" + string.Join(",", codes) + "]");
    }

    private static void HasNot(IReadOnlyList<string> codes, string code)
    {
        if (codes.Contains(code)) throw new InvalidOperationException("unexpected " + code);
    }

    private static void EqualBytes(byte[] expected, byte[] actual)
    {
        if (expected.AsSpan().SequenceEqual(actual)) return;
        throw new InvalidOperationException($"bytes {actual.Length} != {expected.Length}");
    }
}
