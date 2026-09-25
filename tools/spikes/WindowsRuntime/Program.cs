using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

// Disposable ABI experiment, never a production IProjectStore implementation.
// Primary signatures and supported-subset limits are in docs/design/windows-runtime.md.
internal static class Program
{
    private static readonly string[] Cases = ["create", "create-collision", "read", "overwrite", "conflict", "immutable-input",
        "cancel-before", "cancel-after", "write-fault", "replace-fault", "sharing", "dacl-create", "dacl-inheritance",
        "dacl-replacement", "denial", "case-alias", "unicode-alias", "ads-device-unc", "hard-link", "leaf-reparse",
        "ancestor-reparse", "ancestor-substitution", "owned-cleanup", "cleanup-refusal", "file-flush", "directory-durability"];
    private static string source = "", binary = "";
    private static byte[] a = [], b = [];
    private static Dictionary<string, object> evidence = [];
    private static bool published;
    private static bool containmentLost;

    private static int Main(string[] args)
    {
        source = Environment.GetEnvironmentVariable("W0_SOURCE") ?? "not recorded";
        string executable = Environment.ProcessPath!, directory = Path.GetDirectoryName(executable)!;
        string[] binaries = [Path.GetFileName(executable), "WindowsRuntime.dll", "WindowsRuntime.deps.json", "WindowsRuntime.runtimeconfig.json"];
        binary = Hash(Encoding.UTF8.GetBytes(string.Concat(binaries.Order(StringComparer.Ordinal)
            .Select(name => name + "\0" + Hash(File.ReadAllBytes(Path.Combine(directory, name))) + "\n"))));
        if (args is ["--diagnostic-controls"]) return DiagnosticControls();
        if (args is ["--final-arm-controls"]) return FinalArmControls();
        if (args is ["--constructor-controls", var controlRoot]) return ConstructorControls(controlRoot);
        if (args is ["--layout-controls"]) return LayoutControls();
        if (args is ["--discrimination", var candidateRoot, var inputA, var inputB])
            return Discrimination(candidateRoot, inputA, inputB);
        if (args.Length == 1 && args[0].StartsWith("--tree-", StringComparison.Ordinal))
            return Tree(args[0]);
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            foreach (string name in Cases) Emit(name, "Not assessed", "W0-UNSUPPORTED-HOST");
            return 3;
        }
        if (args.Length != 3) return 2;
        try
        {
            Require(Marshal.SizeOf<Native.SecurityAttributes>() == 24 && Marshal.SizeOf<Native.FileInfo>() == 52,
                "W0-ABI");
            a = File.ReadAllBytes(args[1]); b = File.ReadAllBytes(args[2]);
            Require(a.Length > 0 && b.Length > 0 && Hash(a) != Hash(b), "W0-FIXTURES");
            string root = Path.GetFullPath(args[0]);
            Require(!Directory.Exists(root), "W0-ROOT-EXISTS");
            using var ancestors = new PinnedPath(Path.GetDirectoryName(root)!);
            CreateDirectory(root, ProtectedSddl());
            int failures = 0;
            foreach (string name in Cases)
            {
                evidence.Clear(); published = false;
                evidence["os"] = RuntimeInformation.OSDescription;
                evidence["architecture"] = RuntimeInformation.ProcessArchitecture.ToString();
                evidence["filesystem"] = new DriveInfo(Path.GetPathRoot(root)!).DriveFormat;
                evidence["fixtureA"] = Hash(a); evidence["fixtureB"] = Hash(b);
                evidence["tokenUserSid"] = Sid;
                if (RefuseAfterContainmentLoss(name)) continue;
                string folder = Path.Combine(root, name);
                CreateDirectory(folder, ProtectedSddl());
                var timer = Stopwatch.StartNew();
                try { RunScenario(name, folder); Emit(name, "Pass", "OK", timer.Elapsed.TotalMilliseconds); }
                catch (Unsupported error) { failures++; Emit(name, "Not assessed", "W0-UNSUPPORTED-" + error.Message, timer.Elapsed.TotalMilliseconds); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
                { failures++; evidence["error"] = error.Message; Emit(name, "Fail", "W0-ORACLE-FAILED", timer.Elapsed.TotalMilliseconds); }
            }
            // Retain all fixtures. No recursive path cleanup, including on failure.
            return failures == 0 ? 0 : 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        { Console.Error.WriteLine(JsonSerializer.Serialize(new { code = "W0-SETUP-FAILED", error = error.Message })); return 1; }
    }

    private static void Emit(string name, string status, string code, double milliseconds = 0) =>
        Console.WriteLine(JsonSerializer.Serialize(new { @case = name, status, code, source, binary,
            publication = published, durability = false, elapsedMilliseconds = milliseconds, evidence }));
    private static bool RefuseAfterContainmentLoss(string name)
    {
        if (!containmentLost) return false;
        Emit(name, "Not assessed", "W0-UNSUPPORTED-PRIOR-CONTAINMENT-LOSS"); return true;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Require(bool value, string code) { if (!value) throw new InvalidOperationException(code); }
    private static void Check(bool value) { if (!value) throw new Win32Exception(Marshal.GetLastPInvokeError()); }
    private static string Sid
    {
        get
        {
            using var process = Process.GetCurrentProcess();
            Check(Native.OpenProcessToken(process.SafeHandle, 0x0008, out var token)); // TOKEN_QUERY only.
            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
                return identity.User?.Value ?? throw new Unsupported("SID");
        }
    }
    private static string ProtectedSddl() => "O:" + Sid + "D:P(A;;FA;;;" + Sid + ")";

    private static void RunScenario(string name, string folder)
    {
        if (name != "overwrite") { Scenario(name, folder); return; }
        using var parent = new PinnedPath(folder);
        WithReplacementDiagnostics(folder, NativeDiagnosticArm, () =>
        {
            using var fresh = new PinnedPath(folder);
            if (!parent.Identities.SequenceEqual(fresh.Identities)) throw new UnsafeDiagnostic("parent identity changed");
        }, () => Scenario(name, folder));
    }

    private static void Scenario(string name, string folder)
    {
        string target = Path.Combine(folder, "target.bin"), temp = Path.Combine(folder, "temp.bin");
        using var parent = new PinnedPath(folder);
        if (name is "create" or "create-collision" or "overwrite" or "conflict" or "immutable-input" or
            "cancel-before" or "cancel-after" or "write-fault" or "replace-fault")
        {
            bool existing = name is "create-collision" or "overwrite" or "conflict" or "cancel-after" or "replace-fault";
            if (existing) { using var old = Owned.Create(target); old.Write(a); }
            byte[] caller = b.ToArray();
            string? expected = existing && name != "create-collision" ? Hash(name == "conflict" ? b : a) : null;
            using var cancellation = new CancellationTokenSource();
            if (name == "cancel-before") cancellation.Cancel();
            SaveObservation result = Publish(target, caller, expected, cancellation.Token,
                afterSnapshot: name == "immutable-input" ? () => caller[0] ^= 0xff : null,
                fault: name is "write-fault" or "replace-fault" ? name : null,
                afterPublish: name == "cancel-after" ? cancellation.Cancel : null);
            published = result.PublicationKnown;
            evidence["saveCode"] = result.Code;
            evidence["candidateDurability"] = result.DurabilityConfirmed;
            evidence["cancellationRequested"] = cancellation.IsCancellationRequested;
            evidence["expectedDiskHash"] = expected ?? "absent";
            evidence["targetExists"] = File.Exists(target);
            evidence["claimExists"] = File.Exists(Path.Combine(folder, "writer.claim"));
            evidence["tempExists"] = File.Exists(Path.Combine(folder, "staged.bin"));
            evidence["after"] = File.Exists(target) ? Hash(File.ReadAllBytes(target)) : "absent";
            string code = name switch
            {
                "cancel-before" => "DOC-CANCELLED",
                "conflict" or "create-collision" => "DOC-CONFLICT",
                "write-fault" => "DOC-IO",
                _ => "DOC-SAVE-UNCERTAIN"
            };
            Require(result.Code == code, "W0-WRONG-SAVE-RESULT");
            bool shouldPublish = name is "create" or "overwrite" or "immutable-input" or "cancel-after";
            Require(published == shouldPublish && !result.DurabilityConfirmed, "W0-WRONG-SAVE-STATE");
            if (published)
            {
                using var fresh = OpenRegular(target);
                Require(Read(fresh).SequenceEqual(b) && result.PublishedSha256 == Hash(b), "W0-WRONG-SAVED-BYTES");
                evidence["after"] = Hash(Read(fresh)); evidence["identityAfter"] = Identity(fresh);
            }
            else if (existing)
            {
                using var preserved = OpenRegular(target);
                Require(Read(preserved).SequenceEqual(a), "W0-ORIGINAL-MUTATED");
                evidence["after"] = Hash(Read(preserved));
            }
            else Require(!File.Exists(target), "W0-UNEXPECTED-PUBLICATION");
            evidence["targetExists"] = File.Exists(target);
            evidence["claimExists"] = File.Exists(Path.Combine(folder, "writer.claim"));
            evidence["tempExists"] = File.Exists(Path.Combine(folder, "staged.bin"));
            Require(!(bool)evidence["claimExists"] && !(bool)evidence["tempExists"], "W0-OWNED-LEFTOVER");
            return;
        }
        if (name == "directory-durability")
        {
            evidence["contract"] = "No accepted equivalent to final-directory fsync; volume flush requires elevation and is forbidden.";
            throw new Unsupported("DIRECTORY-DURABILITY");
        }
        if (name == "unicode-alias")
        {
            RejectPath(Path.Combine(folder, "caf\u00e9.bin"));
            RejectPath(Path.Combine(folder, "cafe\u0301.bin"));
            evidence["rejectedForms"] = 2; return;
        }
        if (name == "ads-device-unc")
        {
            foreach (string path in new[] { target + ":stream", "\\\\server\\share\\target.bin", "\\\\?\\C:\\target.bin",
                Path.Combine(folder, "NUL"), Path.Combine(folder, "target.bin."), Path.Combine(folder, "..", "outside.bin") }) RejectPath(path);
            evidence["rejectedForms"] = 6; return;
        }
        if (name is "leaf-reparse" or "ancestor-reparse")
        {
            string original = Path.Combine(folder, "original");
            CreateDirectory(original, ProtectedSddl());
            string originalFile = Path.Combine(original, "original.bin");
            using (var file = Owned.Create(originalFile)) file.Write(a);
            string link = Path.Combine(folder, "redirect");
            bool directory = name == "ancestor-reparse";
            if (!Native.CreateSymbolicLinkW(link, directory ? original : originalFile, directory ? 3u : 2u))
            {
                int error = Marshal.GetLastPInvokeError(); evidence["win32"] = error;
                if (error is 1314 or 5) throw new Unsupported("REPARSE-CREATION-RIGHTS");
                throw new Win32Exception(error);
            }
            bool rejected = false;
            try
            {
                if (directory) { using var pinned = new PinnedPath(link); }
                else { using var file = OpenRegular(link); }
            }
            catch (Unsupported) { rejected = true; }
            Require(rejected && File.ReadAllBytes(originalFile).SequenceEqual(a), "W0-REDIRECT-FOLLOWED");
            evidence["redirectRejected"] = rejected; evidence["after"] = Hash(File.ReadAllBytes(originalFile)); return;
        }
        if (name == "ancestor-substitution")
        {
            evidence["ancestorOperands"] = new { sourcePath = folder, destinationPath = folder + "-moved",
                destinationExisted = Directory.Exists(folder + "-moved"), flags = 0,
                heldAncestors = parent.Identities, ancestorAccess = 0, ancestorShare = 3,
                sourceBefore = DirectorySnapshot(folder), destinationBefore = DirectorySnapshot(folder + "-moved"),
                lifetime = "all ancestor handles retained through call" };
            bool moved = Native.MoveFileExW(folder, folder + "-moved", 0);
            int error = Marshal.GetLastPInvokeError();
            evidence["moved"] = moved; evidence["win32"] = moved ? null! : error;
            evidence["heldAncestors"] = parent.Identities;
            evidence["ancestorAfter"] = new { source = DirectorySnapshot(folder), destination = DirectorySnapshot(folder + "-moved") };
            Require(!moved && error is 5 or 32, "W0-ANCESTOR-MOVED"); return;
        }
        if (name == "dacl-inheritance")
        {
            string permissive = Path.Combine(folder, "permissive");
            CreateDirectory(permissive, "D:(A;OICI;FA;;;WD)");
            using var fixture = Owned.Create(Path.Combine(permissive, "protected.bin"));
            evidence["protectedDescriptor"] = SecurityEvidence(fixture.Handle);
            Require(PrivateDacl(fixture.Handle), "W0-INHERITANCE-LEAK");
            using var inherited = Open(Path.Combine(permissive, "inherited.bin"), Native.ReadWrite, 7, 1);
            evidence["inheritedDescriptor"] = SecurityEvidence(inherited);
            Require(!PrivateDacl(inherited), "W0-NEGATIVE-DACL-NOT-DETECTED");
            evidence["protectedPrivate"] = PrivateDacl(fixture.Handle);
            evidence["inheritedControlDetected"] = !PrivateDacl(inherited); return;
        }
        if (name == "denial")
        {
            using var sd = new Descriptor("D:P(D;;GA;;;" + Sid + ")");
            using var denied = Open(target, 0, 7, 1, sd);
            using var attempt = Native.CreateFileW(target, Native.ReadWrite, 7, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
            int error = Marshal.GetLastPInvokeError();
            evidence["expectedDenial"] = new { path = target, metadata = Metadata(denied),
                access = Native.ReadWrite, error, denied = attempt.IsInvalid, creationHandleRetained = !denied.IsClosed,
                sha256 = (string?)null, hashStatus = "Not assessed: read access denied" };
            Require(attempt.IsInvalid && error == 5, "W0-DENIAL-NOT-ENFORCED");
            evidence["win32"] = error; return;
        }
        using var held = Owned.Create(target); held.Write(a);
        evidence["before"] = Hash(held.Read()); evidence["identityBefore"] = Identity(held.Handle);
        if (name == "read") { Require(held.Read().SequenceEqual(a), "W0-BYTES"); evidence["after"] = Hash(held.Read()); return; }
        if (name is "dacl-create" or "dacl-replacement")
        {
            evidence["creationDacl"] = SecurityEvidence(held.Handle);
            Require(PrivateDacl(held.Handle), "W0-DACL");
            if (name == "dacl-replacement")
            {
                using var replacement = Owned.Create(temp); replacement.Write(b);
                evidence["replacementDacl"] = SecurityEvidence(replacement.Handle);
                evidence["replacementOperands"] = new { targetIdentity = Identity(held.Handle),
                    targetAccess = Native.ReadWrite | 0x10000 | 0x20000, targetShare = 5,
                    targetLifetime = "original owned target handle retained through rename", parentIdentities = parent.Identities };
                replacement.Rename(target, true); published = true;
                Require(PrivateDacl(replacement.Handle), "W0-REPLACEMENT-DACL");
                evidence["replacementDacl"] = SecurityEvidence(replacement.Handle);
            }
            return;
        }
        if (name == "file-flush") { Check(Native.FlushFileBuffers(held.Handle)); evidence["fileFlushed"] = true; return; }
        if (name == "sharing")
        {
            using var writer = Native.CreateFileW(target, Native.ReadWrite, 7, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
            int error = Marshal.GetLastPInvokeError(); Require(writer.IsInvalid && error == 32, "W0-WRITER-NOT-EXCLUDED");
            evidence["win32"] = error; return;
        }
        if (name == "case-alias")
        {
            using var alias = Open(target.ToUpperInvariant(), 0, 7, 3);
            Require(Identity(alias) == Identity(held.Handle), "W0-CASE-SENSITIVE-DIRECTORY");
            evidence["aliasIdentity"] = Identity(alias); return;
        }
        if (name == "hard-link")
        {
            Check(Native.CreateHardLinkW(temp, target, IntPtr.Zero));
            bool rejected = false;
            try { using var linked = OpenRegular(temp); } catch (Unsupported) { rejected = true; }
            Require(rejected && held.Read().SequenceEqual(a), "W0-HARDLINK-ACCEPTED");
            evidence["links"] = Info(held.Handle).Links; evidence["after"] = Hash(held.Read()); return;
        }
        if (name == "owned-cleanup")
        {
            bool deleted = held.Cleanup();
            Require(deleted && !File.Exists(target), "W0-CLEANUP");
            evidence["ownedDeleted"] = deleted; evidence["targetExists"] = File.Exists(target); return;
        }
        if (name == "cleanup-refusal")
        {
            held.Rename(temp, false); // Owned original moved; foreign entry occupies the old name.
            using var foreign = Owned.Create(target); foreign.Write(b);
            bool refused = !held.Cleanup();
            Require(refused && foreign.Read().SequenceEqual(b), "W0-FOREIGN-DELETED");
            evidence["foreignAfter"] = Hash(foreign.Read()); evidence["cleanupRefused"] = refused;
            evidence["targetExists"] = File.Exists(target); evidence["ownedOriginalExists"] = File.Exists(temp); return;
        }
        throw new InvalidOperationException("W0-UNKNOWN-CASE");
    }

    private sealed record SaveObservation(string Code, string? PublishedSha256, bool PublicationKnown, bool DurabilityConfirmed);
    private static object DirectorySnapshot(string path)
    {
        try
        {
            using var handle = Open(path, 0, 7, 3, flags: 0x02200000);
            return new { exists = true, identity = Identity(handle), attributes = (uint?)Info(handle).Attributes, error = (string?)null };
        }
        catch (Win32Exception error)
        { return new { exists = Directory.Exists(path), identity = (string?)null, attributes = (uint?)null, error = error.NativeErrorCode.ToString() }; }
    }
    private static object Snapshot(string path)
    {
        try
        {
            using var file = OpenRegular(path);
            return new { exists = true, identity = Identity(file), sha256 = Hash(Read(file)), error = (string?)null };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        { return new { exists = File.Exists(path), identity = (string?)null, sha256 = (string?)null, error = error.Message }; }
    }
    private static void WithReplacementDiagnostics(string folder, Action<string, bool, Dictionary<string, object>> runArm,
        Action verifySafety, Action original)
    {
        var originalEvidence = evidence;
        bool originalPublication = published, safe = true;
        var arms = new List<Dictionary<string, object>>(); originalEvidence["replacementDiagnostics"] = arms;
        foreach (bool release in new[] { false, true })
        {
            string arm = release ? "released-target" : "held-target";
            string directory = Path.Combine(folder, arm);
            var observation = new Dictionary<string, object> { ["arm"] = arm, ["directory"] = directory,
                ["targetPath"] = Path.Combine(directory, "target.bin"), ["stagedPath"] = Path.Combine(directory, "staged.bin"),
                ["inputA"] = Hash(a), ["inputB"] = Hash(b), ["steps"] = new List<string>(),
                ["targetIdentity"] = "Not recorded", ["stagedIdentity"] = "Not recorded", ["nativeError"] = "Not recorded",
                ["finalContainment"] = "Not established",
                ["cleanup"] = "no handles acquired", ["status"] = "Not assessed" };
            arms.Add(observation);
            if (!safe) { observation["reason"] = "prior arm lost containment or cleanup"; continue; }
            evidence = observation;
            try
            {
                verifySafety();
                runArm(directory, release, observation);
                observation["status"] = "Observed";
            }
            catch (UnsafeDiagnostic error) { safe = false; observation["exception"] = error.Message; observation["status"] = "Unsafe"; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
            {
                observation["exception"] = error.Message; observation["exceptionType"] = error.GetType().Name; observation["status"] = "Fail";
                if (error is Win32Exception native) observation["nativeError"] = native.NativeErrorCode;
            }
            finally
            {
                try { verifySafety(); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
                { safe = false; observation["safetyFailure"] = error.Message; }
                if (observation["cleanup"] is not ("no handles acquired" or "all acquired handles disposed")) safe = false;
                if (observation["finalContainment"] is not "Verified") safe = false;
                if (!safe) observation["status"] = "Unsafe";
                evidence = originalEvidence; published = originalPublication;
            }
        }
        originalEvidence["originalOverwriteInvoked"] = false;
        if (!safe) { containmentLost = true; throw new Unsupported("DIAGNOSTIC-CONTAINMENT-OR-CLEANUP"); }
        originalEvidence["originalOverwriteInvoked"] = true;
        original(); // This is the unchanged original scenario, not a diagnostic substitute.
    }
    private static void NativeDiagnosticArm(string directory, bool release, Dictionary<string, object> observation)
    {
        var steps = (List<string>)observation["steps"];
        PinnedPath? parent = null;
        Owned? initial = null, staged = null;
        SafeFileHandle? heldTarget = null;
        bool directoryCreated = false;
        try
        {
            steps.Add("setup");
            if (Directory.Exists(directory) || File.Exists(directory)) throw new UnsafeDiagnostic("diagnostic path collision");
            CreateDirectory(directory, ProtectedSddl());
            directoryCreated = true;
            try { parent = new PinnedPath(directory); }
            catch (Unsupported error) { throw new UnsafeDiagnostic(error.Message); }
            string target = (string)observation["targetPath"], stagedPath = (string)observation["stagedPath"];
            observation["cleanup"] = "handles acquired; disposal pending";
            initial = Owned.Create(target); initial.Write(a); initial.Dispose();
            staged = Owned.Create(stagedPath); staged.Write(b);
            observation["stagedIdentity"] = Identity(staged.Handle);
            heldTarget = OpenRegular(target);
            observation["targetAccess"] = 0x80000000u; observation["targetShare"] = 7;
            observation["targetIdentity"] = Identity(heldTarget); observation["targetBefore"] = Hash(Read(heldTarget));
            observation["parentIdentities"] = parent.Identities;
            observation["targetLifetime"] = release ? "closed immediately before rename" : "retained through rename";
            if (release) heldTarget.Dispose();
            steps.Add("operation");
            try { staged.Rename(target, true); observation["returned"] = true; observation["nativeError"] = null!; }
            catch (Win32Exception error) { observation["returned"] = false; observation["nativeError"] = error.NativeErrorCode; }
            steps.Add("observation");
            observation["destinationAfter"] = Snapshot(target);
            observation["stagedIdentityAfter"] = Identity(staged.Handle);
            observation["stagedBytesAfter"] = Hash(staged.Read());
        }
        catch (Exception error)
        {
            observation["armExceptionBeforeFinal"] = new { type = error.GetType().FullName, message = error.Message,
                nativeError = error is Win32Exception native ? (int?)native.NativeErrorCode : null };
            throw;
        }
        finally
        {
            try
            {
                VerifyFinalArm(observation, () =>
                {
                    observation["finalContainmentScope"] = directoryCreated ? "created arm directory identity" : "no arm directory created";
                    if (!directoryCreated) return;
                    if (parent is null) throw new Unsupported("ARM-INITIAL-IDENTITY-NOT-ESTABLISHED");
                    using var fresh = new PinnedPath(directory);
                    if (!fresh.Identities.SequenceEqual(parent.Identities)) throw new UnsafeDiagnostic("arm parent identity changed");
                });
            }
            finally
            {
                steps.Add("cleanup");
                heldTarget?.Dispose(); staged?.Dispose(); initial?.Dispose(); parent?.Dispose();
                observation["cleanup"] = "all acquired handles disposed";
                observation["fixtureDisposition"] = "retained for bounded driver inventory; no path deletion";
            }
        }
    }
    private sealed class UnsafeDiagnostic(string message, Exception? inner = null) : InvalidOperationException(message, inner);
    private static void VerifyFinalArm(Dictionary<string, object> observation, Action verify)
    {
        observation["finalContainment"] = "Not established";
        try { verify(); observation["finalContainment"] = "Verified"; }
        catch (Exception error)
        {
            observation["finalContainment"] = "Unsafe";
            observation["finalContainmentException"] = new { type = error.GetType().FullName, message = error.Message,
                nativeError = error is Win32Exception native ? (int?)native.NativeErrorCode : null };
            throw new UnsafeDiagnostic("final arm containment not established", error);
        }
    }
    private static int FinalArmControls()
    {
        int failures = 0;
        foreach (bool failReleased in new[] { false, true })
        foreach (string fault in new[] { "unsupported", "access", "observation", "identity", "cleanup" })
        {
            evidence = []; published = false; containmentLost = false;
            int originalCalls = 0, operations = 0;
            try
            {
                WithReplacementDiagnostics("synthetic-final-arm-root", (directory, release, row) =>
                {
                    operations++;
                    try
                    {
                        VerifyFinalArm(row, () =>
                        {
                            if (release != failReleased) return;
                            switch (fault)
                            {
                                case "unsupported": throw new Unsupported("injected final pin unsupported");
                                case "access": throw new Win32Exception(5, "injected final pin access");
                                case "observation": throw new IOException("injected final observation");
                                case "identity": throw new UnsafeDiagnostic("injected final identity inequality");
                            }
                        });
                    }
                    finally { row["cleanup"] = release == failReleased && fault == "cleanup" ? "injected unclosed handle" : "all acquired handles disposed"; }
                }, () => { }, () => originalCalls++);
            }
            catch (Unsupported error) { evidence["refusal"] = error.Message; }
            var arms = (List<Dictionary<string, object>>)evidence["replacementDiagnostics"];
            bool passed = originalCalls == 0 && operations == (failReleased ? 2 : 1) &&
                evidence["originalOverwriteInvoked"] is false && containmentLost &&
                (failReleased || (string)arms[1]["status"] == "Not assessed");
            var retained = evidence;
            using var rows = new StringWriter();
            TextWriter stdout = Console.Out;
            try
            {
                Console.SetOut(rows);
                foreach (string name in Cases) { evidence = []; if (!RefuseAfterContainmentLoss(name)) passed = false; }
            }
            finally { Console.SetOut(stdout); evidence = retained; }
            if (!passed) failures++;
            Console.WriteLine(JsonSerializer.Serialize(new { control = (failReleased ? "released-" : "held-") + fault,
                scope = "synthetic actions through actual final guard and orchestrator", result = passed ? "Pass" : "Fail",
                source, binary, originalCalls, operations, evidence, refusedRows = rows.ToString() }));
        }
        return failures == 0 ? 0 : 1;
    }
    private static int DiagnosticControls()
    {
        // Same orchestration, deterministic injected actions. No Windows API or native claim.
        foreach (string fault in new[] { "setup", "operation", "observation", "original", "unsafe", "cleanup" })
        {
            evidence = []; published = false; containmentLost = false; int originalCalls = 0;
            try
            {
                WithReplacementDiagnostics("synthetic-disjoint-root", (directory, release, row) =>
                {
                    var steps = (List<string>)row["steps"];
                    try
                    {
                        foreach (string step in new[] { "setup", "operation", "observation" })
                        {
                            steps.Add(step);
                            if (!release && step == fault) throw new IOException("injected " + step);
                            if (!release && fault == "unsafe") throw new UnsafeDiagnostic("injected containment loss");
                        }
                    }
                    finally
                    {
                        VerifyFinalArm(row, () => { });
                        row["cleanup"] = !release && fault == "cleanup" ? "injected unclosed handle" : "all acquired handles disposed";
                    }
                }, () => { }, () =>
                {
                    originalCalls++;
                    if (fault == "original") throw new IOException("injected original scenario");
                });
            }
            catch (Exception error) when (error is IOException or Unsupported) { evidence["originalException"] = error.Message; }
            var arms = (List<Dictionary<string, object>>)evidence["replacementDiagnostics"];
            bool stop = fault is "unsafe" or "cleanup";
            Require(arms.Count == 2 && originalCalls == (stop ? 0 : 1), "W0-DIAGNOSTIC-ISOLATION");
            Require((string)arms[1]["status"] == (stop ? "Not assessed" : "Observed"), "W0-DIAGNOSTIC-SECOND-ARM");
            Require(fault != "original" || evidence.ContainsKey("originalException"), "W0-ORIGINAL-FAILURE-NOT-RECORDED");
            Require(!published, "W0-DIAGNOSTIC-PUBLICATION-LEAK");
            Console.WriteLine(JsonSerializer.Serialize(new { control = fault, scope = "synthetic orchestration", result = "Pass", source, binary, originalCalls, evidence }));
        }
        return 0;
    }
    private static int ConstructorControls(string root)
    {
        Require(!Directory.Exists(root) && !File.Exists(root), "W0-CONSTRUCTOR-ROOT-EXISTS");
        Directory.CreateDirectory(root);
        string[] paths = Enumerable.Range(0, 4).Select(i => Path.Combine(root, "owned-" + i + ".bin")).ToArray();
        foreach (string path in paths) using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) file.WriteByte(42);
        string foreignPath = Path.Combine(root, "foreign.bin");
        using (var file = new FileStream(foreignPath, FileMode.CreateNew, FileAccess.Write)) file.WriteByte(17);
        foreach (int failAfter in new[] { 1, 3 })
        {
            using var foreign = File.OpenHandle(foreignPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var acquired = new List<SafeFileHandle>();
            var fault = new IOException("injected inspection after acquisition " + failAfter);
            Exception? caught = null;
            try
            {
                using var pin = new PinnedPath(paths, path =>
                {
                    var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    acquired.Add(handle); return handle;
                }, handle =>
                {
                    if (acquired.Count == failAfter) throw fault;
                    return acquired.Count.ToString();
                });
            }
            catch (IOException error) { caught = error; }
            bool sameError = ReferenceEquals(caught, fault);
            bool[] closed = acquired.Select(h => h.IsClosed).ToArray();
            bool foreignOpen = !foreign.IsClosed && RandomAccess.GetLength(foreign) == 1;
            Require(acquired.Count == failAfter && closed.All(v => v) && foreignOpen && sameError, "W0-CONSTRUCTOR-CLEANUP");
            Console.WriteLine(JsonSerializer.Serialize(new { control = "constructor-after-" + failAfter,
                scope = "actual PinnedPath ownership path with injected file-handle acquisition/inspection", result = "Pass",
                source, binary, acquired = acquired.Count, closed, foreignOpen, sameError, error = caught!.Message }));
        }
        return 0;
    }
    private static SaveObservation Publish(string path, byte[] input, string? expected, CancellationToken cancellation,
        Action? afterSnapshot = null, string? fault = null, Action? afterPublish = null)
    {
        byte[] snapshot = input.ToArray(); afterSnapshot?.Invoke();
        bool known = false; string code = "DOC-SAVE-UNCERTAIN";
        Owned? claim = null, staged = null;
        SafeFileHandle? target = null;
        using var parent = new PinnedPath(Path.GetDirectoryName(path)!);
        try
        {
            cancellation.ThrowIfCancellationRequested();
            claim = Owned.Create(Path.Combine(Path.GetDirectoryName(path)!, "writer.claim"));
            if (expected is not null)
            {
                target = OpenRegular(path);
                if (!PrivateDacl(target)) throw new Unsupported("OVERWRITE-DACL");
                if (Hash(Read(target)) != expected) return new("DOC-CONFLICT", null, false, false);
            }
            staged = Owned.Create(Path.Combine(Path.GetDirectoryName(path)!, "staged.bin"));
            if (fault == "write-fault")
            { staged.Write(snapshot[..1]); evidence["writtenBeforeFault"] = checked((int)RandomAccess.GetLength(staged.Handle)); throw new IOException("injected partial-write failure"); }
            staged.Write(snapshot);
            cancellation.ThrowIfCancellationRequested();
            if (target is not null)
            {
                using var fresh = OpenRegular(path);
                if (Identity(fresh) != Identity(target) || Hash(Read(fresh)) != expected)
                    return new("DOC-CONFLICT", null, false, false);
            }
            try
            {
                evidence["publishOperands"] = new { heldTarget = target is not null, targetAccess = 0x80000000u,
                    targetShare = 7, targetLifetime = "retained through rename and cleanup",
                    parentIdentities = parent.Identities, ancestorAccess = 0, ancestorShare = 3 };
                staged.Rename(fault == "replace-fault" ? Path.Combine(Path.GetDirectoryName(path)!, "missing", "target.bin") : path, expected is not null);
                known = true; published = true;
            }
            catch (Win32Exception error) when (error.NativeErrorCode is 80 or 183)
            { evidence["nativeError"] = error.NativeErrorCode; code = "DOC-CONFLICT"; }
            catch (Win32Exception error) { evidence["nativeError"] = error.NativeErrorCode; code = "DOC-SAVE-UNCERTAIN"; }
            if (known)
            {
                afterPublish?.Invoke();
                using var fresh = OpenRegular(path);
                Require(Identity(fresh) == Identity(staged.Handle) && Read(fresh).SequenceEqual(snapshot), "W0-PUBLISH-IDENTITY");
            }
        }
        catch (OperationCanceledException) { code = known ? "DOC-SAVE-UNCERTAIN" : "DOC-CANCELLED"; }
        catch (IOException) { code = known ? "DOC-SAVE-UNCERTAIN" : "DOC-IO"; }
        finally
        {
            // Deleted by retained handle only; moved/collided entries are preserved and refuse success.
            bool clean = true;
            try
            {
                if (!known && staged is not null) clean &= staged.Cleanup();
                if (claim is not null) clean &= claim.Cleanup();
            }
            finally { target?.Dispose(); staged?.Dispose(); claim?.Dispose(); }
            Require(clean, "W0-CLEANUP-REFUSED");
        }
        return new(code, known ? Hash(snapshot) : null, known, false);
    }

    private static void ValidatePath(string path)
    {
        if (!Regex.IsMatch(path, @"\A[A-Za-z]:\\[A-Za-z0-9_. \\-]+\z") || path.Length > 240)
            throw new Unsupported("PATH");
        foreach (string part in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
            if (part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                Regex.IsMatch(part, @"\A(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|\z)", RegexOptions.IgnoreCase))
                throw new Unsupported("PATH");
    }
    private static void RejectPath(string path)
    {
        bool rejected = false;
        try { ValidatePath(path); } catch (Unsupported) { rejected = true; }
        Require(rejected, "W0-PATH-ACCEPTED");
    }
    private sealed class PinnedPath : IDisposable
    {
        private readonly List<SafeFileHandle> handles = [];
        public readonly List<string> Identities = [];
        public PinnedPath(string path) : this(AncestorPaths(path),
            path => Open(path, 0, 3, 3, flags: 0x02200000), InspectDirectory) { }
        public PinnedPath(IEnumerable<string> paths, Func<string, SafeFileHandle> acquire, Func<SafeFileHandle, string> inspect)
        {
            try
            {
                foreach (string path in paths)
                {
                    var handle = acquire(path);
                    handles.Add(handle);
                    Identities.Add(inspect(handle));
                }
            }
            catch { Dispose(); throw; }
        }
        private static IEnumerable<string> AncestorPaths(string path)
        {
            ValidatePath(path);
            var drive = new DriveInfo(Path.GetPathRoot(path)!);
            if (drive.DriveType != DriveType.Fixed || drive.DriveFormat != "NTFS") throw new Unsupported("FILESYSTEM");
            string cursor = Path.GetPathRoot(path)!;
            yield return cursor;
            foreach (string part in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
            { cursor = Path.Combine(cursor, part); yield return cursor; }
        }
        private static string InspectDirectory(SafeFileHandle handle)
        {
            // Keep every ancestor without FILE_SHARE_DELETE until all file operations finish.
            var info = Info(handle);
            if ((info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0) throw new Unsupported("ANCESTOR-REPARSE");
            return Identity(handle);
        }
        public void Dispose() { foreach (var handle in handles) handle.Dispose(); }
    }
    private sealed class Unsupported(string message) : InvalidOperationException(message);
    private sealed class Descriptor : IDisposable
    {
        public IntPtr Pointer;
        public Descriptor(string sddl) => Check(Native.ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, 1, out Pointer, out _));
        public void Dispose() { if (Pointer != IntPtr.Zero) { Native.LocalFree(Pointer); Pointer = IntPtr.Zero; } }
    }
    private static void CreateDirectory(string path, string sddl)
    {
        using var descriptor = new Descriptor(sddl);
        var attributes = new Native.SecurityAttributes { Length = 24, Descriptor = descriptor.Pointer };
        Check(Native.CreateDirectoryW(path, ref attributes));
    }
    private static SafeFileHandle Open(string path, uint access, uint share, uint disposition, Descriptor? descriptor = null, uint flags = 0x00200000)
    {
        IntPtr pointer = IntPtr.Zero;
        try
        {
            if (descriptor is not null)
            {
                pointer = Marshal.AllocHGlobal(24);
                Marshal.StructureToPtr(new Native.SecurityAttributes { Length = 24, Descriptor = descriptor.Pointer }, pointer, false);
            }
            var handle = Native.CreateFileW(path, access, share, pointer, disposition, flags, IntPtr.Zero);
            if (handle.IsInvalid) { int error = Marshal.GetLastPInvokeError(); handle.Dispose(); throw new Win32Exception(error); }
            return handle;
        }
        finally { if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer); }
    }
    private static SafeFileHandle OpenRegular(string path)
    {
        ValidatePath(path);
        // Permit the already-retained writer handle; it still denies new writers.
        var handle = Open(path, 0x80000000, 7, 3);
        try
        {
            var info = Info(handle);
            if ((info.Attributes & 0x410) != 0 || info.Links != 1) throw new Unsupported("FILE-KIND");
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }
    private static Native.FileInfo Info(SafeFileHandle handle) { Check(Native.GetFileInformationByHandle(handle, out var info)); return info; }
    private static object Metadata(SafeFileHandle handle)
    {
        var info = Info(handle);
        return new { identity = Identity(handle), attributes = info.Attributes, links = info.Links,
            size = ((ulong)info.SizeHigh << 32) | info.SizeLow };
    }
    private static string Identity(SafeFileHandle handle)
    { var i = Info(handle); return $"{i.Volume:x8}:{i.IndexHigh:x8}{i.IndexLow:x8}"; }
    private static byte[] Read(SafeFileHandle handle)
    {
        long length = RandomAccess.GetLength(handle); Require(length <= 1024 * 1024, "W0-FIXTURE-SIZE");
        byte[] bytes = new byte[checked((int)length)]; int read = 0;
        while (read < bytes.Length) { int count = RandomAccess.Read(handle, bytes.AsSpan(read), read); Require(count > 0, "W0-SHORT-READ"); read += count; }
        return bytes;
    }
    private static RawSecurityDescriptor ReadDescriptor(SafeFileHandle handle)
    {
        uint error = Native.GetSecurityInfo(handle, 1, 7, out _, out _, out _, out _, out var descriptor);
        if (error != 0) throw new Win32Exception((int)error);
        try
        {
            int size = checked((int)Native.GetSecurityDescriptorLength(descriptor)); byte[] bytes = new byte[size];
            Marshal.Copy(descriptor, bytes, 0, size);
            return new RawSecurityDescriptor(bytes, 0);
        }
        finally { Native.LocalFree(descriptor); }
    }
    private static bool PrivateDacl(SafeFileHandle handle)
    {
        var descriptor = ReadDescriptor(handle);
        return descriptor.Owner?.Value == Sid &&
            (descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0 &&
            (descriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0 &&
            descriptor.DiscretionaryAcl is { Count: 1 } acl && acl[0] is CommonAce ace &&
            ace.AceType == AceType.AccessAllowed && ace.AceFlags == AceFlags.None &&
            ace.AceQualifier == AceQualifier.AccessAllowed && !ace.IsInherited && ace.SecurityIdentifier.Value == Sid &&
            ace.AccessMask == 0x1f01ff;
    }
    private static object SecurityEvidence(SafeFileHandle handle)
    {
        var descriptor = ReadDescriptor(handle);
        string raw = descriptor.GetSddlForm(AccessControlSections.Owner | AccessControlSections.Access);
        var facts = DescriptorFacts(descriptor);
        using var parsed = new Descriptor(raw); // Parse the original raw text through the Windows contract.
        var resolved = DescriptorFacts(DescriptorFromPointer(parsed.Pointer));
        Require(JsonSerializer.Serialize(facts) == JsonSerializer.Serialize(resolved), "W0-RAW-DESCRIPTOR-CONTRADICTION");
        var context = new Dictionary<string, object>();
        Match tokens = Regex.Match(raw, @"\AO:(S-1-[0-9-]+|LA|SY)D:P\(A;;FA;;;(S-1-[0-9-]+|LA|SY)\)\z");
        // Non-private inheritance controls retain their descriptor but cannot gain admission.
        if (tokens.Success)
        {
            if (tokens.Groups[1].Value == "LA" || tokens.Groups[2].Value == "LA")
            {
                context["method"] = "LsaQueryInformationPolicy:PolicyAccountDomainInformation:local";
                context["accountDomainSid"] = AccountDomainSid();
            }
            string Resolve(string token) => token switch
            {
                "SY" => "S-1-5-18",
                "LA" => (string)context["accountDomainSid"] + "-500",
                _ => new SecurityIdentifier(token).Value
            };
            Require(Resolve(tokens.Groups[1].Value) == descriptor.Owner?.Value &&
                descriptor.DiscretionaryAcl is { Count: 1 } acl && acl[0] is KnownAce ace &&
                Resolve(tokens.Groups[2].Value) == ace.SecurityIdentifier.Value, "W0-RAW-ALIAS-CONTEXT-MISMATCH");
        }
        else if (PrivateDacl(handle)) throw new Unsupported("RAW-ALIAS-OR-DESCRIPTOR-SUBSET");
        facts["rawSddl"] = raw; facts["tokenUserSid"] = Sid;
        facts["rawResolution"] = new { method = "ConvertStringSecurityDescriptorToSecurityDescriptorW", context, descriptor = resolved };
        return facts;
    }
    private static Dictionary<string, object> DescriptorFacts(RawSecurityDescriptor descriptor)
    {
        var aces = new List<object>();
        if (descriptor.DiscretionaryAcl is { } acl)
            foreach (GenericAce entry in acl)
                aces.Add(new { type = entry.AceType.ToString(), flags = (int)entry.AceFlags,
                    inherited = entry.IsInherited, qualifier = (entry as QualifiedAce)?.AceQualifier.ToString(),
                    mask = (entry as KnownAce)?.AccessMask, trusteeSid = (entry as KnownAce)?.SecurityIdentifier.Value });
        return new() { ["ownerSid"] = descriptor.Owner?.Value!,
            ["daclPresent"] = (descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0,
            ["daclProtected"] = (descriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0,
            ["aceCount"] = descriptor.DiscretionaryAcl?.Count!, ["aces"] = aces };
    }
    private static RawSecurityDescriptor DescriptorFromPointer(IntPtr pointer)
    {
        int size = checked((int)Native.GetSecurityDescriptorLength(pointer));
        byte[] bytes = new byte[size]; Marshal.Copy(pointer, bytes, 0, size);
        return new RawSecurityDescriptor(bytes, 0);
    }
    private static string AccountDomainSid()
    {
        Require(Marshal.SizeOf<Native.LsaObjectAttributes>() == 48 && Marshal.SizeOf<Native.AccountDomainInfo>() == 24,
            "W0-LSA-ABI");
        var attributes = new Native.LsaObjectAttributes { Length = 48 };
        uint status = Native.LsaOpenPolicy(IntPtr.Zero, ref attributes, 1, out var policy); // Read-only POLICY_VIEW_LOCAL_INFORMATION.
        if (status != 0) throw new Win32Exception((int)Native.LsaNtStatusToWinError(status));
        try
        {
            status = Native.LsaQueryInformationPolicy(policy, 5, out var buffer);
            if (status != 0) throw new Win32Exception((int)Native.LsaNtStatusToWinError(status));
            try
            {
                var info = Marshal.PtrToStructure<Native.AccountDomainInfo>(buffer);
                Require(info.DomainSid != IntPtr.Zero, "W0-LSA-DOMAIN-SID-MISSING");
                return new SecurityIdentifier(info.DomainSid).Value;
            }
            finally { Require(Native.LsaFreeMemory(buffer) == 0, "W0-LSA-FREE"); }
        }
        finally { Require(Native.LsaClose(policy) == 0, "W0-LSA-CLOSE"); }
    }
    private sealed class Owned(string path, SafeFileHandle handle) : IDisposable
    {
        public SafeFileHandle Handle { get; } = handle;
        public static Owned Create(string path)
        {
            using var descriptor = new Descriptor(ProtectedSddl());
            var handle = Open(path, Native.ReadWrite | 0x10000 | 0x20000, 5, 1, descriptor);
            try
            {
                if (!evidence.TryGetValue("creationDescriptors", out var entries))
                    evidence["creationDescriptors"] = entries = new List<object>();
                ((List<object>)entries).Add(new { path, descriptor = SecurityEvidence(handle) });
                Require(PrivateDacl(handle), "W0-DACL-BEFORE-PAYLOAD"); return new Owned(path, handle);
            }
            catch { handle.Dispose(); throw; }
        }
        public void Write(byte[] bytes) { RandomAccess.Write(Handle, bytes, 0); Check(Native.FlushFileBuffers(Handle)); }
        public byte[] Read() => Program.Read(Handle);
        public void Rename(string target, bool replace)
        {
            // WinBase.h FILE_RENAME_INFO x64: union at 0, HANDLE at 8, DWORD at 16, WCHAR at 20.
            byte[] name = Encoding.Unicode.GetBytes(target); byte[] buffer = new byte[20 + name.Length + 2];
            buffer[0] = replace ? (byte)1 : (byte)0; BitConverter.GetBytes(name.Length).CopyTo(buffer, 16); name.CopyTo(buffer, 20);
            var diagnostic = new Dictionary<string, object?> { ["sourcePath"] = path, ["destinationPath"] = target,
                ["replace"] = replace, ["sourceIdentity"] = Identity(Handle), ["sourceBefore"] = Hash(Read()),
                ["sourceAccess"] = Native.ReadWrite | 0x10000 | 0x20000, ["sourceShare"] = 5,
                ["sourceLifetime"] = "owned handle retained through call", ["destinationBefore"] = Snapshot(target) };
            diagnostic["sourceParentBefore"] = DirectorySnapshot(Path.GetDirectoryName(path)!);
            diagnostic["destinationParentBefore"] = DirectorySnapshot(Path.GetDirectoryName(target)!);
            if (!evidence.TryGetValue("renames", out var entries)) evidence["renames"] = entries = new List<object>();
            ((List<object>)entries).Add(diagnostic);
            bool renamed = Native.SetFileInformationByHandle(Handle, 3, buffer, (uint)buffer.Length);
            int error = Marshal.GetLastPInvokeError();
            diagnostic["returned"] = renamed; diagnostic["nativeError"] = renamed ? null : error;
            diagnostic["destinationAfter"] = Snapshot(target); diagnostic["sourceAfter"] = Hash(Read());
            diagnostic["sourceParentAfter"] = DirectorySnapshot(Path.GetDirectoryName(path)!);
            diagnostic["destinationParentAfter"] = DirectorySnapshot(Path.GetDirectoryName(target)!);
            if (!renamed) throw new Win32Exception(error);
        }
        public bool RenameEx(string target, uint flags)
        {
            byte[] buffer = RenameBuffer(target, flags);
            var row = new Dictionary<string, object?> { ["class"] = 22, ["flags"] = flags,
                ["bufferLength"] = buffer.Length, ["nameBytes"] = Encoding.Unicode.GetByteCount(target),
                ["sourcePath"] = path, ["destinationPath"] = target, ["sourceIdentity"] = Identity(Handle),
                ["sourceHash"] = Hash(Read()), ["destinationBefore"] = Snapshot(target) };
            evidence["exCall"] = row; // Operands survive an observation exception.
            bool result = Native.SetFileInformationByHandle(Handle, 22, buffer, (uint)buffer.Length);
            int error = Marshal.GetLastPInvokeError();
            row["returned"] = result; row["nativeError"] = result ? null : error;
            return result;
        }
        public bool Cleanup()
        {
            using var entry = Open(path, 0, 7, 3);
            if (Identity(entry) != Identity(Handle)) return false;
            Check(Native.SetFileInformationByHandle(Handle, 4, [1], 1));
            Handle.Dispose(); return true;
        }
        public void Dispose() => Handle.Dispose();
    }
    private static int Tree(string mode)
    {
        if (!OperatingSystem.IsWindows()) return 3;
        if (mode != "--tree-grandchild")
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            start.ArgumentList.Add(mode == "--tree-root" ? "--tree-child" : "--tree-grandchild");
            using var child = Process.Start(start) ?? throw new InvalidOperationException("W0-CHILD");
        }
        // Finite timeout target, owned by the driver's already-assigned job. No detached child.
        using var wait = new ManualResetEvent(false); wait.WaitOne(TimeSpan.FromSeconds(30)); return 0;
    }

    // Microsoft windows-sys 0.59.0 generated repr(C) binding; exact provenance in proof.
    [StructLayout(LayoutKind.Sequential)] private struct RenameInfo
    { public uint Flags; public IntPtr RootDirectory; public uint FileNameLength; public ushort FileName; }
    private static byte[] RenameBuffer(string target, uint flags)
    {
        Require(IntPtr.Size == 8 && BitConverter.IsLittleEndian && Marshal.SizeOf<RenameInfo>() == 24 &&
            Marshal.OffsetOf<RenameInfo>(nameof(RenameInfo.RootDirectory)).ToInt32() == 8 &&
            Marshal.OffsetOf<RenameInfo>(nameof(RenameInfo.FileNameLength)).ToInt32() == 16 &&
            Marshal.OffsetOf<RenameInfo>(nameof(RenameInfo.FileName)).ToInt32() == 20, "R53-ABI");
        byte[] name = Encoding.Unicode.GetBytes(target), buffer = new byte[24 + name.Length];
        BitConverter.GetBytes(flags).CopyTo(buffer, 0); BitConverter.GetBytes(name.Length).CopyTo(buffer, 16);
        name.CopyTo(buffer, 20); return buffer;
    }
    private static int LayoutControls()
    {
        string target = "C:\\owned\\caf\u00e9.bin";
        byte[] buffer = RenameBuffer(target, 3), name = Encoding.Unicode.GetBytes(target);
        bool pass = BitConverter.ToUInt32(buffer) == 3 && BitConverter.ToUInt64(buffer, 8) == 0 &&
            BitConverter.ToUInt32(buffer, 16) == name.Length && buffer.AsSpan(20, name.Length).SequenceEqual(name) &&
            buffer.Length == 24 + name.Length && buffer.AsSpan(20 + name.Length).ToArray().All(x => x == 0);
        Console.WriteLine(JsonSerializer.Serialize(new { control = "rename-ex-layout", source, binary, pass,
            infoClass = 22, flags = 3, widths = new[] { 4, 8, 4, 2 }, offsets = new[] { 0, 8, 16, 20 },
            structSize = Marshal.SizeOf<RenameInfo>(), bufferLength = buffer.Length, nameBytes = name.Length,
            bufferHex = Convert.ToHexStringLower(buffer), scope = "managed ABI layout only; not Windows API execution" }));
        return pass ? 0 : 1;
    }
    private static int Discrimination(string root, string inputA, string inputB)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return 3;
        a = File.ReadAllBytes(inputA); b = File.ReadAllBytes(inputB);
        Require(a.Length > 0 && b.Length > 0 && Hash(a) != Hash(b), "R53-INPUTS");
        Require(!Directory.Exists(root) && !File.Exists(root), "R53-ROOT-COLLISION");
        CreateDirectory(root, ProtectedSddl());
        using var rootPin = new PinnedPath(root);
        bool safe = true;
        foreach (string arm in new[] { "ex-replace", "ex-no-replace", "ex-conflict", "ex-cancel-before", "ex-cancel-after", "ex-foreign-cleanup",
            "ancestor-zero", "ancestor-list-held", "ancestor-list-released" })
        {
            evidence = new() { ["arm"] = arm, ["fixtureA"] = Hash(a), ["fixtureB"] = Hash(b), ["tokenUserSid"] = Sid,
                ["publication"] = false, ["durability"] = false, ["cleanup"] = "Not established" };
            string status = "Not assessed", folder = Path.Combine(root, arm);
            evidence["directory"] = folder;
            if (safe)
            {
                try
                {
                    CreateDirectory(folder, ProtectedSddl());
                    using (var directoryHandle = Open(folder, 0, 7, 3, flags: 0x02200000))
                        evidence["armDirectoryIdentity"] = Identity(directoryHandle);
                    if (arm.StartsWith("ex-", StringComparison.Ordinal)) ExArm(arm, folder);
                    else AncestorArm(arm, folder);
                    status = "Observed";
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
                { status = "Fail"; evidence["error"] = error.Message; evidence["errorType"] = error.GetType().Name; }
                finally
                {
                    try
                    {
                        using var fresh = new PinnedPath(root);
                        Require(rootPin.Identities.SequenceEqual(fresh.Identities), "R53-ROOT-IDENTITY");
                        string actualPath = evidence.TryGetValue("moved", out var moved) && moved is true ? folder + "-moved" : folder;
                        using var actual = Open(actualPath, 0, 7, 3, flags: 0x02200000);
                        Require(evidence.TryGetValue("armDirectoryIdentity", out var originalIdentity) &&
                            Identity(actual) == (string)originalIdentity && (Info(actual).Attributes & 0x410) == 0x10,
                            "R53-FINAL-ARM-IDENTITY");
                        Require(evidence["cleanup"] is "all acquired handles disposed", "R53-CLEANUP-UNKNOWN");
                        evidence["containment"] = "Verified disposable root only";
                    }
                    catch (Exception error) { safe = false; status = "Unsafe"; evidence["safetyError"] = error.Message; }
                }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { arm, status, source, binary, evidence }));
        }
        return safe ? 0 : 1;
    }
    private static void ExArm(string arm, string folder)
    {
        try
        {
            using var parent = new PinnedPath(folder);
            string target = Path.Combine(folder, "target.bin"), stagePath = Path.Combine(folder, "staged.bin");
            using (var initial = Owned.Create(target)) initial.Write(a);
            using var held = OpenRegular(target);
            using var staged = Owned.Create(stagePath); staged.Write(b);
            evidence["oldIdentity"] = Identity(held); evidence["oldHash"] = Hash(Read(held));
            evidence["stagedIdentity"] = Identity(staged.Handle); evidence["stagedHash"] = Hash(staged.Read());
            evidence["parentIdentities"] = parent.Identities;
            evidence["targetAccess"] = 0x80000000u; evidence["targetShare"] = 7;
            evidence["targetLifetime"] = "retained through call and all observations";
            using var cancellation = new CancellationTokenSource();
            if (arm == "ex-cancel-before") cancellation.Cancel();
            string expected = arm == "ex-conflict" ? Hash(b) : Hash(a);
            evidence["expectedHash"] = expected;
            bool invoke = !cancellation.IsCancellationRequested && Hash(Read(held)) == expected;
            if (invoke)
            {
                using var current = OpenRegular(target);
                Require(Identity(current) == Identity(held) && Hash(Read(current)) == expected, "R53-EXPECTED-TARGET-DRIFT");
            }
            evidence["callInvoked"] = invoke;
            bool result = invoke && staged.RenameEx(target, arm == "ex-no-replace" ? 2u : 3u);
            evidence["publication"] = result;
            if (result && arm == "ex-cancel-after") cancellation.Cancel();
            evidence["cancellationRequested"] = cancellation.IsCancellationRequested;
            evidence["saveCode"] = result ? "DOC-SAVE-UNCERTAIN" : cancellation.IsCancellationRequested ? "DOC-CANCELLED" :
                !invoke || arm == "ex-no-replace" ? "DOC-CONFLICT" : "DOC-IO";
            evidence["oldHandleOpen"] = !held.IsClosed;
            evidence["oldIdentityAfter"] = Identity(held); evidence["oldHashAfter"] = Hash(Read(held));
            evidence["newPath"] = Snapshot(target); evidence["stagedHashAfter"] = Hash(staged.Read());
            evidence["stagedDacl"] = SecurityEvidence(staged.Handle);
            Require(PrivateDacl(staged.Handle) && Read(held).SequenceEqual(a) && staged.Read().SequenceEqual(b), "R53-RETAINED-BYTES-DACL");
            Require(Identity(held) == (string)evidence["oldIdentity"], "R53-OLD-IDENTITY");
            if (result)
            {
                using var fresh = OpenRegular(target);
                Require(Identity(fresh) == Identity(staged.Handle) && Read(fresh).SequenceEqual(b), "R53-NEW-IDENTITY");
                if (arm == "ex-foreign-cleanup")
                {
                    using var foreign = Owned.Create(stagePath); foreign.Write(a);
                    evidence["cleanupRefused"] = !staged.Cleanup();
                    evidence["foreignHash"] = Hash(foreign.Read());
                    Require((bool)evidence["cleanupRefused"] && foreign.Read().SequenceEqual(a), "R53-FOREIGN-CLEANUP");
                }
            }
            bool expectedPublication = arm is "ex-replace" or "ex-cancel-after" or "ex-foreign-cleanup";
            Require(result == expectedPublication, "R53-CANDIDATE-RESULT");
            using var final = new PinnedPath(folder);
            Require(parent.Identities.SequenceEqual(final.Identities), "R53-ARM-IDENTITY");
        }
        finally { evidence["cleanup"] = "all acquired handles disposed"; }
    }
    private static void AncestorArm(string arm, string folder)
    {
        SafeFileHandle? held = null;
        try
        {
            uint access = arm == "ancestor-zero" ? 0u : 0x81u; // FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES.
            string destination = folder + "-moved";
            evidence["sourcePath"] = folder; evidence["destinationPath"] = destination;
            evidence["access"] = access; evidence["share"] = 3; evidence["flags"] = 0;
            evidence["sourceBefore"] = DirectorySnapshot(folder); evidence["destinationBefore"] = DirectorySnapshot(destination);
            using var parent = new PinnedPath(Path.GetDirectoryName(folder)!);
            evidence["parentIdentities"] = parent.Identities;
            try { held = Open(folder, access, 3, 3, flags: 0x02200000); }
            catch (Win32Exception accessError) when (accessError.NativeErrorCode == 5)
            { evidence["accessRefusal"] = accessError.NativeErrorCode; throw new Unsupported("DIRECTORY-LIST-ACCESS-UNAVAILABLE"); }
            evidence["heldIdentity"] = Identity(held);
            if (arm == "ancestor-list-released") held.Dispose();
            evidence["handleOpenAtCall"] = !held.IsClosed;
            bool moved = Native.MoveFileExW(folder, destination, 0); int error = Marshal.GetLastPInvokeError();
            evidence["moved"] = moved; evidence["nativeError"] = moved ? null! : error;
            evidence["sourceAfter"] = DirectorySnapshot(folder); evidence["destinationAfter"] = DirectorySnapshot(destination);
            using var actual = Open(moved ? destination : folder, 0, 7, 3, flags: 0x02200000);
            Require(Identity(actual) == (string)evidence["heldIdentity"], "R53-MOVE-IDENTITY");
            if (arm == "ancestor-list-held") Require(!moved && error is 5 or 32, "R53-LIST-PIN-MOVED");
            else Require(moved, "R53-NEGATIVE-MOVE-NOT-OBSERVED");
        }
        finally { held?.Dispose(); evidence["cleanup"] = "all acquired handles disposed"; }
    }
}

internal static class Native
{
    internal const uint ReadWrite = 0xc0000000;
    [StructLayout(LayoutKind.Sequential)] internal struct LsaObjectAttributes
    { public uint Length; public IntPtr RootDirectory, ObjectName; public uint Attributes; public IntPtr SecurityDescriptor, SecurityQualityOfService; }
    [StructLayout(LayoutKind.Sequential)] internal struct LsaUnicodeString
    { public ushort Length, MaximumLength; public IntPtr Buffer; }
    [StructLayout(LayoutKind.Sequential)] internal struct AccountDomainInfo
    { public LsaUnicodeString DomainName; public IntPtr DomainSid; }
    [DllImport("advapi32.dll")] internal static extern uint LsaOpenPolicy(IntPtr system, ref LsaObjectAttributes attributes, uint access, out IntPtr handle);
    [DllImport("advapi32.dll")] internal static extern uint LsaQueryInformationPolicy(IntPtr handle, int informationClass, out IntPtr buffer);
    [DllImport("advapi32.dll")] internal static extern uint LsaNtStatusToWinError(uint status);
    [DllImport("advapi32.dll")] internal static extern uint LsaFreeMemory(IntPtr buffer);
    [DllImport("advapi32.dll")] internal static extern uint LsaClose(IntPtr handle);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [StructLayout(LayoutKind.Sequential)] internal struct SecurityAttributes { public uint Length; public IntPtr Descriptor; public int Inherit; }
    [StructLayout(LayoutKind.Sequential)] internal struct FileInfo
    { public uint Attributes, CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh, Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateDirectoryW(string path, ref SecurityAttributes security);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInfo info);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetFileInformationByHandle(SafeFileHandle handle, int infoClass, byte[] info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlushFileBuffers(SafeFileHandle handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MoveFileExW(string source, string target, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool CreateSymbolicLinkW(string link, string target, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateHardLinkW(string link, string target, IntPtr security);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string sddl, uint revision, out IntPtr descriptor, out uint size);
    [DllImport("advapi32.dll")] internal static extern uint GetSecurityInfo(SafeFileHandle handle, int type, uint information,
        out IntPtr owner, out IntPtr group, out IntPtr dacl, out IntPtr sacl, out IntPtr descriptor);
    [DllImport("advapi32.dll")] internal static extern uint GetSecurityDescriptorLength(IntPtr descriptor);
    [DllImport("kernel32.dll")] internal static extern IntPtr LocalFree(IntPtr memory);
}
