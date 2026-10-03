using System.Text;
using CfdWorkbench.Core;

// Writes the §3.3 old-build fixtures (docs/design/m12c-section-editor.md) with the M1.2c SDR build, from the repo root:
//   dotnet run --project docs/proof/m12c-old-build/generate -- docs/proof/m12c-old-build/fixtures
// Projects (c1) and (d1) are saved by the real section draft. A profile tangents row is not writable before SPT, so the
// sources with a row are assembled as text; their design hash is the hash of the same source without the row, because a
// tangent row is outside identity (ADR-0005 §4; FoilSourceTests Identity_TangentsRows_DefinitionHashUnchanged).
string output = args.Length > 0 ? args[0] : "docs/proof/m12c-old-build/fixtures";
Directory.CreateDirectory(output);
static string Id() => Guid.NewGuid().ToString("D");
static string Ids(int count) => "ids [" + string.Join(", ", Enumerable.Range(0, count).Select(i => $"\"cv-{i}\"")) + "]";

byte[] example = FoilSource.MaterializeIds(FoilSource.Parse(File.ReadAllBytes("docs/examples/foildsl/foil-basic.foil")));
string text = Encoding.UTF8.GetString(example);
string Line(string side)
{
    int start = text.IndexOf("      " + side + " cv {", StringComparison.Ordinal);
    return text[start..text.IndexOf('\n', start)];
}
string upper = Line("upper"), lower = Line("lower");
// Per-surface bases: the lower surface on its own knots (shape of record unchanged in kind, still 8 points).
string lowerOwnKnots = "      lower cv { degree 5 knots [0, 0, 0, 0, 0, 0, 0.25, 0.75, 1, 1, 1, 1, 1, 1] points [(0, 0), (0, -0.025), (0.15, -0.055), " +
    "(0.35, -0.065), (0.55, -0.05), (0.75, -0.03), (0.9, -0.01), (1, 0)] " + Ids(8) + " }";
// An anchor at vertex 5: an interior knot of multiplicity exactly p = 5, its handles collinear with it (a smooth row holds).
const string anchorKnots = "degree 5 knots [0, 0, 0, 0, 0, 0, 0.5, 0.5, 0.5, 0.5, 0.5, 1, 1, 1, 1, 1, 1]";
const string anchorPoints = "[(0, 0), (0, 0.025), (0.08, 0.045), (0.18, 0.058), (0.28, 0.064), (0.38, 0.066), (0.48, 0.068), (0.6, 0.055), (0.75, 0.035), (0.9, 0.012), (1, 0)]";
string upperAnchor = "      upper cv { " + anchorKnots + " points " + anchorPoints + " " + Ids(11) + " }";
string upperAnchorRow = "      upper cv { " + anchorKnots + " points " + anchorPoints + " " + Ids(11) + " tangents { \"cv-5\" smooth } }";
string lowerAnchor = "      lower cv { " + anchorKnots + " points " + anchorPoints.Replace(", 0.0", ", -0.0") + " " + Ids(11) + " }";

byte[] Source(string upperLine, string lowerLine, bool v41) =>
    Encoding.UTF8.GetBytes((v41 ? text.Replace("foildsl \"4.0\"", "foildsl \"4.1\"") : text).Replace(upper, upperLine).Replace(lower, lowerLine));

byte[] perSurface = Source(upper, lowerOwnKnots, false);
byte[] anchorNoRow = Source(upperAnchor, lowerAnchor, true);
byte[] anchorRow = Source(upperAnchorRow, lowerAnchor, true);
byte[] perSurfaceRowFree = Source(upperAnchor, lower, true);
byte[] perSurfaceRow = Source(upperAnchorRow, lower, true);
byte[] anchorRowMoved = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(anchorRow).Replace("(0.18, 0.058)", "(0.18, 0.059)"));
byte[] anchorNoRowMoved = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(anchorNoRow).Replace("(0.18, 0.058)", "(0.18, 0.059)"));

void Write(string name, byte[] bytes)
{
    File.WriteAllBytes(Path.Combine(output, name), bytes);
    Console.WriteLine($"wrote {name} ({bytes.Length} bytes)");
}
string Describe(byte[] bytes)
{
    var parsed = FoilSource.Parse(bytes);
    if (!parsed.IsParsed) return "parse " + parsed.Diagnostics[0].Code;
    var assessment = Geometry.Assess(parsed);
    return $"parse OK; assess {assessment.Status} {assessment.Code}";
}

Write("a-per-surface.foil", perSurface);
Write("b-profile-row.foil", anchorRow);
Write("b0-profile-anchor-no-row.foil", anchorNoRow);
foreach (var (name, bytes) in new[] { ("a", perSurface), ("b", anchorRow), ("b0", anchorNoRow), ("e-row-free", perSurfaceRowFree) })
    Console.WriteLine($"this build, {name}: {Describe(bytes)}");

// (c1) and (d1): the real section draft on the Example.
using (var session = new AuthoringSession())
{
    session.Open(example, Id(), false);
    string draft = Id();
    var view = session.BeginSectionDraft(draft, 0);
    view = session.ApplySectionStep(draft, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", 0.35, 0.07));
    string? row = session.FinishSection(Id(), session.AssessSection(draft, view.Generation, CancellationToken.None));
    Console.WriteLine($"c1 finished row {row}");
    Write("c1-section-receipt.cfdw.json", session.SaveImage());
}
using (var session = new AuthoringSession())
{
    session.Open(example, Id(), false);
    string draft = Id();
    var view = session.BeginSectionDraft(draft, 0);
    view = session.ApplySectionStep(draft, view.Generation, new SectionStep.MakeUnique());
    session.ApplySectionStep(draft, view.Generation, new SectionStep.Move(SurfaceSide.Upper, "cv-3", 0.35, 0.07));
    session.CaptureRecovery();
    Write("d1-section-recovery.cfdw.json", session.SaveImage());
}

// (c2), (d2), (e1), (e2): synthetic envelopes in the native schema.
var c2 = Chain([(example, example), (anchorRow, anchorNoRow)], "section-a");
Write("c2-section-receipt-row.cfdw.json", NativeProject.Encode(c2));
var d2 = c2 with { Recovery = new RecoveryRow(Id(), c2.Accepted[^1].Id, 1, "section", "section-a", AuthoringSession.Chunks(anchorRowMoved), "section-a", 0) };
Write("d2-section-recovery-row.cfdw.json", NativeProject.Encode(d2));
Write("e1-open-per-surface.cfdw.json", NativeProject.Encode(Chain([(perSurface, perSurface)], "")));
Write("e2-open-per-surface-row.cfdw.json", NativeProject.Encode(Chain([(perSurfaceRow, perSurfaceRowFree)], "")));
// Control for (d): the same recovery on a row-free anchored history, so the recovery rail alone is the difference.
var control = Chain([(example, example), (anchorNoRow, anchorNoRow)], "section-a");
Write("d0-section-recovery-no-row.cfdw.json", NativeProject.Encode(control with
    { Recovery = new RecoveryRow(Id(), control.Accepted[^1].Id, 1, "section", "section-a", AuthoringSession.Chunks(anchorNoRowMoved), "section-a", 0) }));

foreach (string path in Directory.GetFiles(output, "*.cfdw.json").Order())
{
    string name = Path.GetFileName(path);
    using var probe = new AuthoringSession();
    string result;
    try { probe.Reopen(File.ReadAllBytes(path)); result = "reopens"; }
    catch (ContractError error) { result = error.Code; }
    Console.WriteLine($"this build, {name}: {result}");
}

// An open row, then one "section" row per later source. Each entry is (bytes, the bytes whose surface hash it carries).
static Envelope Chain((byte[] Bytes, byte[] Identity)[] revisions, string profile)
{
    var sources = new List<SourceRow>(); var designs = new List<DesignRow>(); var accepted = new List<AcceptedRow>(); var cursors = new List<CursorRow>();
    string? parent = null;
    foreach (var (bytes, identity) in revisions)
    {
        string sourceId = CfdWorkbench.Core.Identity.Sha256(bytes);
        string surface = FoilSource.Parse(identity).SurfaceHash!;
        if (!sources.Any(row => row.Id == sourceId)) sources.Add(new(sourceId, AuthoringSession.Chunks(bytes)));
        if (designs.Count == 0 || designs[^1].SurfaceHash != surface)
            designs.Add(new(Id(), designs.Count == 0 ? null : designs[^1].Id, surface, "cfdw-cv/2"));
        string row = Id(), operation = Id();
        accepted.Add(new(row, parent, sourceId, designs[^1].Id, operation, parent is null ? null : new EditReceipt(Id(), 1, "section", profile)));
        cursors.Add(new(cursors.Count, row, parent is null ? "open" : "apply", operation));
        parent = row;
    }
    return new("cfdw-project-1", Id(), sources.ToArray(), designs.ToArray(), accepted.ToArray(), cursors.ToArray(), null);
}
