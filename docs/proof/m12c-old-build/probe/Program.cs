using System.Text.Json;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop;

// The §3.3 old-build characterization (docs/design/m12c-section-editor.md). Built against a worktree at 4b9bc35, it runs each
// fixture through that build's CLI (`inspect <path> --json`) and app controller (OpenAsync, one keyboard edit, Save to the
// same path), on a scratch copy, and prints one JSON line per fixture with the SHA-256 of the file before and after.
//   dotnet run --project <probe> -p:OldBuildRoot=<worktree> -- <fixtures dir> <scratch dir>
string fixtures = args[0], scratch = args[1];
Directory.CreateDirectory(scratch);
foreach (string fixture in Directory.GetFiles(fixtures).Order(StringComparer.Ordinal))
{
    string name = Path.GetFileName(fixture);
    string path = Path.Combine(scratch, name);
    File.Copy(fixture, path, overwrite: true);
    string before = Identity.Sha256(File.ReadAllBytes(path));

    var cli = new StringWriter();
    int exit = await CfdWorkbench.Cli.Cli.RunAsync(["inspect", path, "--json"], cli);
    string cliText = cli.ToString().Trim();

    string open, status, edit, save;
    bool document;
    using (var app = new WorkbenchController())
    {
        try { open = (await app.OpenAsync(path)).ToString(); }
        catch (Exception error) { open = Failure(error); }
        status = app.Status;
        document = app.Inspection is not null;
        try
        {
            bool began = app.BeginGesture(new PointRef("leading", "cv-2"), GestureInput.Keyboard);
            if (began) app.Nudge(0, 1, NudgeModifier.Plain);
            edit = began ? "began" : "refused: no point to edit";
        }
        catch (Exception error) { edit = Failure(error); }
        try { save = (await app.SaveAsync(path)).Code; }
        catch (Exception error) { save = Failure(error); }
    }
    string after = Identity.Sha256(File.ReadAllBytes(path));
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        fixture = name, cliExit = exit, cliCode = CliCode(cliText), cliReason = CliReason(cliText), appOpen = open, appStatus = status,
        appDocument = document, edit, save, sha256Before = before, sha256After = after, unchanged = before == after
    }));
}

static string Failure(Exception error) => error is ContractError contract ? "refused " + contract.Code : error.GetType().Name + ": " + error.Message;

static string? CliCode(string json)
{
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    if (root.TryGetProperty("code", out var code)) return code.GetString();
    return root.TryGetProperty("assessment", out var assessment) && assessment.TryGetProperty("code", out var inner) ? inner.GetString() : null;
}

static string? CliReason(string json)
{
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    if (root.TryGetProperty("assessment", out var assessment))
        return assessment.TryGetProperty("reason", out var reason) ? reason.GetString() : assessment.TryGetProperty("Reason", out var upper) ? upper.GetString() : null;
    return root.TryGetProperty("diagnostics", out var diagnostics) && diagnostics.GetArrayLength() > 0 &&
        diagnostics[0].TryGetProperty("reason", out var first) ? first.GetString() : null;
}
