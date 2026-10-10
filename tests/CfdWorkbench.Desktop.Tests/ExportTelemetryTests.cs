using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Shell;
using static CfdWorkbench.Desktop.Tests.ExportTests;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Export telemetry (docs/design/export.md section 7, H7): one <c>export.write</c> per write attempt, one <c>export.validate</c> per refusal
/// before writing, none with a path, a file name, a foil name, a user or a host. Ring: fast, in the `--section-editor` child.
/// Cost: about 1.5 s together (each check opens the Example, about 0.2 s).
/// </summary>
public static class ExportTelemetryTests
{
    public static void Run()
    {
        DesktopChecks.Check("ExportTelemetry_Dialog_WrittenDat_OneWriteEventOnTheSessionRing", DialogWritten);
        DesktopChecks.Check("ExportTelemetry_MeshFormats_WrittenEvent_CarriesScopePresetTrianglesDeviationBytes", MeshWritten);
        DesktopChecks.Check("ExportTelemetry_Refused_GeometryAndClosure_OneValidateEventEach_NoWrite", Refused);
        DesktopChecks.Check("ExportTelemetry_FailedWrite_OneCodedEventPerCause", Failed);
        DesktopChecks.Check("ExportTelemetry_PickerCancel_NoEvent_WriteCancel_Cancelled", Cancelled);
        DesktopChecks.Check("ExportTelemetry_Events_CarryNoPathNameUserOrHost", NoIdentifiers);
    }

    private static List<ExportTelemetry> Events(WorkbenchController controller) =>
        [.. controller.LocalEvents.Where(item => item.Export is not null).Select(item => item.Export!)];

    private static ExportSession Mesh(WorkbenchController controller, ExportFormat format, StlScope scope, StlPreset preset,
        Func<byte[], string, int, StlScope, double, CancellationToken, StlExportResult>? build = null)
    {
        var session = new ExportSession(controller.ExportSnapshot()!, build, build, controller.RecordExport);
        session.Set(format: format, scope: scope, preset: preset);
        Wait(session.PrepareAsync());
        return session;
    }

    // The dialog's own path: ShellHost builds the session, so its sink is the controller's.
    private static void DialogWritten()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var host = new ShellHost(controller);
            host.ShowExportDialog = async session => await session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name)));
            Wait(host.RunCommand("file.export"));
            var events = Events(controller);
            Equal(1, events.Count, "one event for one write");
            var e = events[0];
            Equal("export.write", e.Operation);
            Equal("written", e.Outcome);
            Equal("dat", e.Format);
            Equal(null, e.Scope);
            Equal(null, e.Preset);
            Equal(new FileInfo(Path.Combine(folder, "basic-foil-root-r1.dat")).Length, e.Bytes ?? -1);
            True(e.Milliseconds is >= 0, "milliseconds are measured");
            Equal(null, e.Triangles);
            Equal(null, e.DeviationMm);
            Equal("export.write", controller.LocalEvents.Last().Operation);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void MeshWritten()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            foreach (var (format, scope, preset, text) in new[] { (ExportFormat.Stl, StlScope.Half, StlPreset.Draft, ("stl", "half", "draft")),
                (ExportFormat.ThreeMf, StlScope.Whole, StlPreset.Print, ("3mf", "whole", "print")) }.Select(c => (c.Item1, c.Item2, c.Item3, c.Item4)))
            {
                var session = Mesh(controller, format, scope, preset);
                var outcome = Wait(session.RunAsync((name, _) => Task.FromResult<string?>(Path.Combine(folder, name))));
                Equal(ExportOutcomeKind.Written, outcome.Kind);
                var e = Events(controller).Last();
                Equal("export.write", e.Operation);
                Equal(text.Item1, e.Format);
                Equal(text.Item2, e.Scope);
                Equal(text.Item3, e.Preset);
                Equal("written", e.Outcome);
                Equal(session.Stl!.Triangles, e.Triangles ?? -1);
                Equal(session.Stl.DeviationMm, e.DeviationMm ?? -1);
                Equal(new FileInfo(outcome.Path!).Length, e.Bytes ?? -1);
            }
            Equal(2, Events(controller).Count);
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Refused()
    {
        using var controller = OpenExample();
        // Geometry not accepted (H2): one validate event when the session opens, none for the Run that follows.
        var blocked = new ExportSession(controller.ExportSnapshot()! with { Geometry = GeometryStatus.NotAssessed }, record: controller.RecordExport);
        Wait(blocked.RunAsync((_, _) => Task.FromResult<string?>("never.dat")));
        var one = Events(controller);
        Equal(1, one.Count);
        Equal("export.validate", one[0].Operation);
        Equal("EXPORT-GEOMETRY-NOT-ACCEPTED", one[0].Outcome);
        Equal(null, one[0].Bytes);
        // Closure refused (H7): one validate event with the code; the write that cannot happen adds none.
        var closed = Mesh(controller, ExportFormat.Stl, StlScope.Whole, StlPreset.Print, (_, _, _, _, _, _) => throw new ContractError("EXPORT-NOT-CLOSED"));
        Wait(closed.RunAsync((_, _) => Task.FromResult<string?>("never.stl")));
        var two = Events(controller);
        Equal(2, two.Count);
        Equal("export.validate", two[1].Operation);
        Equal("EXPORT-NOT-CLOSED", two[1].Outcome);
        Equal("stl", two[1].Format);
        Equal(0, two.Count(item => item.Operation == "export.write"), "a refusal is not a write");
    }

    private static void Failed()
    {
        string folder = NewFolder();
        try
        {
            using var controller = OpenExample();
            var session = new ExportSession(controller.ExportSnapshot()!, record: controller.RecordExport);
            (Func<string, byte[], Task>? Write, string Path, string Code)[] cases =
            [
                (null, Path.Combine(folder, "missing", "x.dat"), "EXPORT-FOLDER-GONE"),
                ((_, _) => throw new UnauthorizedAccessException(), Path.Combine(folder, "a.dat"), "EXPORT-NO-PERMISSION"),
                ((_, _) => throw new IOException("full", OperatingSystem.IsWindows() ? unchecked((int)0x80070070) : 28), Path.Combine(folder, "b.dat"), "EXPORT-DISK-FULL"),
                ((_, _) => throw new IOException("other"), Path.Combine(folder, "c.dat"), "EXPORT-WRITE-FAILED"),
            ];
            int expected = 0;
            foreach (var (write, path, code) in cases)
            {
                var outcome = Wait(session.RunAsync((_, _) => Task.FromResult<string?>(path), write));
                Equal(ExportOutcomeKind.Failed, outcome.Kind);
                var events = Events(controller);
                Equal(++expected, events.Count, "one event per failed write");
                Equal("export.write", events[^1].Operation);
                Equal(code, events[^1].Outcome);
                True(events[^1].Bytes > 0, "the size of what was to be written");
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    private static void Cancelled()
    {
        using var controller = OpenExample();
        var session = new ExportSession(controller.ExportSnapshot()!, record: controller.RecordExport);
        Wait(session.RunAsync((_, _) => Task.FromResult<string?>(null)));
        Equal(0, Events(controller).Count, "a cancelled panel is not a write attempt");
        var outcome = Wait(session.RunAsync((_, _) => Task.FromResult<string?>("x.dat"), (_, _) => throw new OperationCanceledException()));
        Equal(ExportOutcomeKind.Cancelled, outcome.Kind);
        var events = Events(controller);
        Equal(1, events.Count);
        Equal("cancelled", events[0].Outcome);
    }

    // A run-time fixture: a hostile folder, file and foil-looking name, then every string an event holds is searched for them.
    private static void NoIdentifiers()
    {
        string folder = NewFolder();
        try
        {
            string secretFolder = Path.Combine(folder, "SecretFolderQ7"), secretFile = "SecretFileQ7.dat";
            Directory.CreateDirectory(secretFolder);
            using var controller = OpenExample();
            var session = new ExportSession(controller.ExportSnapshot()!, record: controller.RecordExport);
            Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(secretFolder, secretFile))));
            Wait(session.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(secretFolder, "gone", secretFile))));
            var mesh = Mesh(controller, ExportFormat.Stl, StlScope.Whole, StlPreset.Print);
            Wait(mesh.RunAsync((_, _) => Task.FromResult<string?>(Path.Combine(secretFolder, "SecretMeshQ7.stl")), (_, _) => throw new IOException(Path.Combine(secretFolder, "SecretMeshQ7.stl"))));
            var events = Events(controller);
            Equal(3, events.Count);
            string[] banned = ["SecretFolderQ7", "SecretFileQ7", "SecretMeshQ7", folder, "basic-foil", "Basic foil", Environment.UserName, Environment.MachineName];
            foreach (var e in events)
            {
                var strings = typeof(ExportTelemetry).GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => (string?)p.GetValue(e) ?? "").ToArray();
                foreach (string text in strings.Append(e.ToString()))
                    foreach (string word in banned)
                        True(!text.Contains(word, StringComparison.OrdinalIgnoreCase), $"an export event carries '{word}': {text}");
            }
            // Only these fields exist: a new string field is a new place for an identifier and must be argued here.
            Equal("Operation,Format,Scope,Preset,Outcome", string.Join(",", typeof(ExportTelemetry).GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => p.Name)));
        }
        finally { Directory.Delete(folder, true); }
    }
}
