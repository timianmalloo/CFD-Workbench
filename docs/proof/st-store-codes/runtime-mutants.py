"""Build and run the one-handle oracle against three temporary native mutants.

Run from Git bash with the PATH strip specified in run.sh. The original source is
restored even on failure. Mutants are never committed. A final build restores the
real assembly as well. No filesystem semantics are simulated by this driver.
"""
from pathlib import Path
import os
import subprocess

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "src/CfdWorkbench.Persistence/WindowsNative.cs"
PROJECT = "tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj"
EXE = "tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe"


def build():
    result = subprocess.run(["dotnet", "build", PROJECT, "-c", "Debug", "-nologo", "-v", "q"], cwd=ROOT,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    if result.returncode:
        raise RuntimeError(result.stdout)


def run():
    env = dict(os.environ, CFD_TEST_ONLY="PrefStore_OneHeldHandle")
    result = subprocess.run([str(ROOT / EXE), "--harness"], cwd=ROOT, env=env,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    lines = [line for line in result.stdout.splitlines() if line.startswith(("PASS PrefStore_", "FAIL PrefStore_"))]
    print("\n".join(lines), flush=True)
    return result.returncode, lines


def main():
    original = SOURCE.read_bytes()
    source = original.decode("utf-8")
    check = "                if (!OwnedByCurrentUser(handle)) return false;"
    set_acl = "uint result = SetSecurityInfo(handle, FileObject, DaclProtected, 0, 0, acl, 0);"
    signature = '''
    [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint SetNamedSecurityInfoW(string path, int kind, uint information,
        nint owner, nint group, nint dacl, nint sacl);
'''
    mutants = {
        "second-relative-open": source.replace(check, check + '\n                using var second = OpenRelative(handle, "", ReadControl | Synchronize, ShareRead | ShareWrite | ShareDelete, NtOpen, 0, directory: true, sameFile: true);', 1),
        "path-dacl-comment-decoy": source.replace(set_acl, "// SetSecurityInfo(handle, is a comment decoy.\n                uint result = SetNamedSecurityInfoW(path, FileObject, DaclProtected, 0, 0, acl, 0);", 1).replace("    // Signatures: Microsoft Learn", signature + "\n    // Signatures: Microsoft Learn", 1),
        "owner-after-dacl": source.replace(check, "", 1).replace(set_acl, set_acl + "\n" + check, 1),
    }
    try:
        for name, mutant in mutants.items():
            if mutant == source:
                raise RuntimeError("mutation anchor missing: " + name)
            SOURCE.write_text(mutant, encoding="utf-8")
            build()
            print("MUTANT " + name, flush=True)
            code, lines = run()
            if code == 0 or not any(line.startswith("FAIL PrefStore_") for line in lines):
                raise RuntimeError("mutant survived or crashed without the expected assertion: " + name)
    finally:
        SOURCE.write_bytes(original)
        build()
    print("REAL CODE", flush=True)
    code, lines = run()
    if code != 0 or not any(line.startswith("PASS PrefStore_") for line in lines):
        raise RuntimeError("real-code runtime proof failed")


if __name__ == "__main__":
    main()
