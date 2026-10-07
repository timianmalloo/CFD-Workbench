using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>Relaunches this harness in a child mode with the same launch shape it was started with.</summary>
public static class SelfLaunch
{
    public static ProcessStartInfo StartInfo(string mode) =>
        StartInfo(Environment.ProcessPath!, Assembly.GetEntryAssembly()!.Location, mode);

    public static ProcessStartInfo StartInfo(string processPath, string entryAssembly, string mode)
    {
        var info = new ProcessStartInfo(processPath) { UseShellExecute = false };
        // Under `dotnet X.dll` the process path is the muxer, which needs the entry assembly again; the apphost is the program itself.
        string program = Path.GetFileNameWithoutExtension(entryAssembly), host = Path.GetFileName(processPath);
        if (host != program && host != program + ".exe") info.ArgumentList.Add(entryAssembly);
        info.ArgumentList.Add(mode);
        return info;
    }
}

public static class SelfLaunchTests
{
    public const string FailureProbe = "--startup-failure-probe";

    public static void Run()
    {
        var failures = new List<string>();
        Check(failures, "unhandled child exception exits with a named code", UnhandledChildExitsNamed);
        Check(failures, "crash detail names the message and a frame; product shape stays type-only", CrashDetailNamesMessageAndFrame);
        Check(failures, "muxer launch passes the entry assembly", MuxerLaunchPassesEntryAssembly);
        Check(failures, "apphost launch passes the mode only", AppHostLaunchPassesModeOnly);
        Check(failures, "no raw Environment.ProcessPath relaunch", () => NoRawProcessPathRelaunch());
        Check(failures, "no test resolves the repo root at runtime", () => NoRuntimeRepoRootWalk());
        Check(failures, "no cwd-relative tests/ or src/ fixture path", () => NoCwdRelativeFixturePath());
        if (failures.Count > 0) throw new Exception("SelfLaunchTests failed:\n  " + string.Join("\n  ", failures));
        Console.WriteLine("SelfLaunchTests: all 7 cases passed.");
    }

    // WFX2 item 1: the Windows ring died with "APP-CRASH System.Exception" and no frame. The harness handler adds the message and stack;
    // the product handler stays type-only (threat model "Crash output").
    private static void CrashDetailNamesMessageAndFrame()
    {
        Exception thrown;
        try { ThrowForFrame("probe-message-7"); thrown = null!; } catch (Exception caught) { thrown = caught; }
        string detailed = StartupFailure.Describe(thrown, detail: true), product = StartupFailure.Describe(thrown, detail: false);
        string[] lines = detailed.Split('\n');
        if (lines[0].TrimEnd('\r') != "APP-UNHANDLED APP-CRASH System.InvalidOperationException")
            throw new Exception("first line changed: " + lines[0]);
        if (!detailed.Contains("probe-message-7") || !detailed.Contains(nameof(ThrowForFrame)))
            throw new Exception("detail lacks the message or a frame: " + detailed);
        if (product.Contains("probe-message-7") || product.Contains('\n'))
            throw new Exception("product shape leaked detail: " + product);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForFrame(string message) => throw new InvalidOperationException(message);

    private static void Check(List<string> failures, string name, Action test)
    {
        try { test(); }
        catch (Exception error) { failures.Add($"{name}: {error.Message}"); }
    }

    // `dotnet X.dll` makes the muxer the process path; relaunching it with the mode alone runs `dotnet --mode`.
    private static void MuxerLaunchPassesEntryAssembly()
    {
        foreach (var muxer in new[] { "/usr/local/share/dotnet/dotnet", "/opt/homebrew/bin/dotnet", "C:/Program Files/dotnet/dotnet.exe" })
            Expect(SelfLaunch.StartInfo(muxer, "/b/CfdWorkbench.Desktop.Tests.dll", "--section-flow"),
                muxer, "/b/CfdWorkbench.Desktop.Tests.dll", "--section-flow");
    }

    // The apphost is the program itself. Its name ends in ".Tests", which a naive extension strip would eat.
    private static void AppHostLaunchPassesModeOnly()
    {
        foreach (var apphost in new[] { "/b/CfdWorkbench.Desktop.Tests", "C:/b/CfdWorkbench.Desktop.Tests.exe" })
            Expect(SelfLaunch.StartInfo(apphost, "/b/CfdWorkbench.Desktop.Tests.dll", "--section-flow"),
                apphost, "--section-flow");
    }

    private static void Expect(ProcessStartInfo info, string file, params string[] arguments)
    {
        if (info.FileName != file || !info.ArgumentList.SequenceEqual(arguments))
            throw new Exception($"{info.FileName} [{string.Join(", ", info.ArgumentList)}] != {file} [{string.Join(", ", arguments)}]");
    }

    // A child that throws must exit with the named startup-failure code, never abort (SIGABRT, 134) with an OS crash report.
    // Covered in this run's own launch shape, under the muxer explicitly (the gate's shape), and for the product's Main.
    private static void UnhandledChildExitsNamed()
    {
        string entry = Assembly.GetEntryAssembly()!.Location;
        ExpectNamedExit(SelfLaunch.StartInfo(FailureProbe), "System.InvalidOperationException");
        ExpectNamedExit(SelfLaunch.StartInfo("dotnet", entry, FailureProbe), "System.InvalidOperationException");
        var product = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        product.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(entry)!, "CfdWorkbench.Desktop.dll"));
        product.Environment["CFDW_REVIEW_MODE"] = "2";
        ExpectNamedExit(product, "System.ArgumentException");
    }

    private static void ExpectNamedExit(ProcessStartInfo info, string exception)
    {
        info.RedirectStandardError = true;
        using var child = Process.Start(info)!;
        var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            child.Kill(entireProcessTree: true);
            throw new Exception($"{string.Join(' ', info.ArgumentList)} did not exit within 60 s");
        }
        if (child.ExitCode != StartupFailure.ExitCode ||
            !stderr.Result.Split('\n').Any(line => line.TrimEnd('\r') == $"{StartupFailure.Code} {StartupFailure.FailureCode} {exception}"))
            throw new Exception($"{string.Join(' ', info.ArgumentList)}: exit {child.ExitCode}, stderr: {stderr.Result.Split('\n')[0]}");
    }

    // Every relaunch must go through SelfLaunch; the sources are located from this file's build-time path.
    private static void NoRawProcessPathRelaunch([CallerFilePath] string self = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));
        if (!File.Exists(Path.Combine(root, "CFDWorkbench.slnx"))) throw new Exception($"repository root not found from {self}");
        var offenders = new[] { "src", "tests" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !string.Equals(file, self, StringComparison.Ordinal) &&
                           File.ReadAllText(file).Contains("Environment.ProcessPath", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file)).ToList();
        if (offenders.Count > 0) throw new Exception("use SelfLaunch.StartInfo in " + string.Join(", ", offenders));
    }

    // Track TEST-REPO-LAYOUT (docs/lessons/defect-classes.md): a test that walks AppContext.BaseDirectory
    // looking for CFDWorkbench.slnx breaks when the gate runs the built assembly from an artifacts
    // directory outside the repo (HARNESS-LAUNCH-SHAPE's sibling). The only safe way a test may resolve
    // the checked-out tree is the compile-time CallerFilePath path this file uses, never a runtime walk.
    private static void NoRuntimeRepoRootWalk([CallerFilePath] string self = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));
        var offenders = Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file =>
            {
                string text = File.ReadAllText(file);
                return text.Contains("CFDWorkbench.slnx", StringComparison.Ordinal) &&
                       !text.Contains("CallerFilePath", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(root, file)).ToList();
        if (offenders.Count > 0)
            throw new Exception("runtime repo-root walk toward CFDWorkbench.slnx outside CallerFilePath in " + string.Join(", ", offenders));
    }

    // TEST-REPO-LAYOUT recurrence (docs/lessons/defect-classes.md): NoRuntimeRepoRootWalk only scanned
    // for the CFDWorkbench.slnx walk pattern and missed the cwd-relative-literal variant (FoilSourceTests,
    // GeometryTests, PointGestureTests, PointModelTests, PointCommandTests, ReopenPointEditTests all read
    // fixtures through `"tests/CfdWorkbench.Core.Tests/Fixtures/m12b/" + name`, which resolves against the
    // process's current directory rather than the repo). That breaks only where a gate runs the built test
    // DLL from a directory other than the repo root — verified (read, not inferred) in
    // tools/verify-application-core.py: `store_masks(published / "...Core.Tests.dll", "published", published)`
    // runs the published Core.Tests DLL with cwd=published. tools/verify-application-adapters.py has no such
    // branch for CfdWorkbench.Desktop.Tests/Cli.Tests — every subprocess there launches with cwd=ROOT — so
    // this scan is scoped to CfdWorkbench.Core.Tests, the one project a gate relocates. A test project that
    // gains a relocated-cwd run must gain this scan's coverage in the same change.
    private static void NoCwdRelativeFixturePath([CallerFilePath] string self = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", ".."));
        var scope = Path.Combine(root, "tests", "CfdWorkbench.Core.Tests");
        var literal = new Regex("\"(tests|src)/[^\"]*\"", RegexOptions.None, TimeSpan.FromSeconds(5));
        var offenders = Directory.EnumerateFiles(scope, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           literal.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file)).ToList();
        if (offenders.Count > 0)
            throw new Exception("cwd-relative tests/ or src/ path literal (breaks when the gate runs the published dll elsewhere) in " +
                                 string.Join(", ", offenders));
    }
}
