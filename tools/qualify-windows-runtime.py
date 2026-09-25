#!/usr/bin/env python3
"""Disposable W0 qualification. No product acceptance or automatic dispatch."""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import signal
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ["global.json", "tools/spikes/WindowsRuntime/WindowsRuntime.csproj",
           "tools/spikes/WindowsRuntime/Program.cs", "tools/qualify-windows-runtime.py",
           ".github/workflows/application-windows-qualification.yml"]
CASES = ("create", "create-collision", "read", "overwrite", "conflict", "immutable-input",
         "cancel-before", "cancel-after", "write-fault", "replace-fault", "sharing",
         "dacl-create", "dacl-inheritance", "dacl-replacement", "denial", "case-alias",
         "unicode-alias", "ads-device-unc", "hard-link", "leaf-reparse", "ancestor-reparse",
         "ancestor-substitution", "owned-cleanup", "cleanup-refusal", "file-flush",
         "directory-durability")
FIXTURE_A = b"W0 immutable fixture A\r\n\x00\xff"
FIXTURE_B = b"W0 independent fixture B\r\n\x01\xfe"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def verify_sources(base, manifest):
    after = {p: digest((base / p).read_bytes()) for p in manifest}
    if after != manifest:
        raise ValueError("W0-SOURCE-DRIFT")
    return after


def validate_case(name, proof):
    """Case-specific consumer contract, independent of native assertions."""
    def equals(key, expected):
        actual = proof.get(key)
        if actual != expected or type(actual) is not type(expected):
            raise ValueError("W0-WRONG-" + name + "-" + key)

    save_codes = {"create": "DOC-SAVE-UNCERTAIN", "create-collision": "DOC-CONFLICT",
                  "overwrite": "DOC-SAVE-UNCERTAIN", "conflict": "DOC-CONFLICT",
                  "immutable-input": "DOC-SAVE-UNCERTAIN", "cancel-before": "DOC-CANCELLED",
                  "cancel-after": "DOC-SAVE-UNCERTAIN", "write-fault": "DOC-IO",
                  "replace-fault": "DOC-SAVE-UNCERTAIN"}
    if name in save_codes:
        equals("saveCode", save_codes[name])
        equals("candidateDurability", False)
        equals("targetExists", name not in ("cancel-before", "write-fault"))
        equals("tempExists", False)
        equals("claimExists", False)
        equals("cancellationRequested", name in ("cancel-before", "cancel-after"))
        if name == "write-fault":
            equals("writtenBeforeFault", 1)
    if name == "cleanup-refusal":
        equals("foreignAfter", digest(FIXTURE_B))
        equals("cleanupRefused", True)
        equals("targetExists", True)
        equals("ownedOriginalExists", True)
    elif name == "owned-cleanup":
        equals("targetExists", False)
        equals("ownedDeleted", True)
    elif name == "sharing":
        equals("win32", 32)
    elif name == "denial":
        equals("win32", 5)
    elif name == "unicode-alias":
        equals("rejectedForms", 2)
    elif name == "ads-device-unc":
        equals("rejectedForms", 6)
    elif name == "dacl-inheritance":
        equals("protectedPrivate", True)
        equals("inheritedControlDetected", True)
    elif name in ("dacl-create", "dacl-replacement"):
        for field in (["creationDacl", "replacementDacl"] if name == "dacl-replacement" else ["creationDacl"]):
            sddl = proof.get(field, "")
            if not isinstance(sddl, str) or not re.fullmatch(r"O:(S-[0-9-]+)D:P\(A;;FA;;;\1\)", sddl):
                raise ValueError("W0-WRONG-DACL")
    elif name == "case-alias":
        identity = proof.get("identityBefore", "")
        if not re.fullmatch(r"[0-9a-f]{8}:[0-9a-f]{16}", identity):
            raise ValueError("W0-IDENTITY-MISSING")
        equals("aliasIdentity", identity)
    elif name == "hard-link":
        if type(proof.get("links")) is not int or proof["links"] < 2:
            raise ValueError("W0-HARDLINK-NOT-OBSERVED")
        equals("after", digest(FIXTURE_A))
    elif name in ("leaf-reparse", "ancestor-reparse"):
        equals("redirectRejected", True)
        equals("after", digest(FIXTURE_A))
    elif name == "ancestor-substitution":
        if proof.get("win32") not in (5, 32) or not proof.get("heldAncestors"):
            raise ValueError("W0-ANCESTOR-RENAME-NOT-REFUSED")
    elif name == "file-flush":
        equals("fileFlushed", True)


def validate(rows, source, binary):
    if not isinstance(rows, list) or any(not isinstance(row, dict) or not isinstance(row.get("case"), str) for row in rows):
        raise ValueError("W0-ROW-SCHEMA")
    if len(rows) != len(CASES) or {r.get("case") for r in rows} != set(CASES):
        raise ValueError("W0-CASE-SET")
    for row in rows:
        if row.get("source") != source or row.get("binary") != binary:
            raise ValueError("W0-BINDING")
        if row.get("status") not in ("Pass", "Expected rejection", "Not assessed", "Fail"):
            raise ValueError("W0-STATUS")
        if type(row.get("publication")) is not bool or row.get("durability") is not False:
            raise ValueError("W0-DURABILITY-CONTRACT")
        if not isinstance(row.get("evidence"), dict) or not isinstance(row.get("code"), str):
            raise ValueError("W0-EVIDENCE")
        if row["status"] in ("Pass", "Expected rejection"):
            if row["code"].startswith("W0-UNSUPPORTED") or not row["evidence"]:
                raise ValueError("W0-UNSUPPORTED-PASS")
            proof = row["evidence"]
            if proof.get("fixtureA") != digest(FIXTURE_A) or proof.get("fixtureB") != digest(FIXTURE_B):
                raise ValueError("W0-FIXTURE-BINDING")
            if proof.get("architecture") != "X64" or proof.get("filesystem") != "NTFS":
                raise ValueError("W0-HOST-CLAIM")
            name = row["case"]
            expected = {"create": FIXTURE_B, "read": FIXTURE_A, "immutable-input": FIXTURE_B,
                        "create-collision": FIXTURE_A, "conflict": FIXTURE_A, "replace-fault": FIXTURE_A,
                        "overwrite": FIXTURE_B, "cancel-after": FIXTURE_B}
            if name in expected and proof.get("after") != digest(expected[name]):
                raise ValueError("W0-WRONG-BYTES")
            if row["publication"] != (name in ("create", "overwrite", "immutable-input", "cancel-after", "dacl-replacement")):
                raise ValueError("W0-WRONG-PUBLICATION")
            if name == "directory-durability":
                raise ValueError("W0-UNRESOLVED-DURABILITY")
            validate_case(name, proof)
    return all(r["status"] in ("Pass", "Expected rejection") for r in rows)


def self_test():
    """Synthetic protocol data, never Windows runtime evidence."""
    source, binary = "a" * 64, "b" * 64
    rows = [dict(case=c, status="Not assessed", code="W0-UNSUPPORTED", source=source,
                 binary=binary, publication=False, durability=False, evidence={}) for c in CASES]
    assert validate(rows, source, binary) is False
    mutations = []
    mutations.append(("missing", rows[:-1]))
    mutations.append(("duplicate", rows + [rows[0]]))
    for name, key, value in [("source", "source", "c" * 64), ("binary", "binary", "c" * 64),
                             ("unsupported-pass", "status", "Pass"),
                             ("durability", "durability", True)]:
        changed = copy.deepcopy(rows)
        changed[0][key] = value
        mutations.append((name, changed))
    for name, changed in mutations:
        try:
            validate(changed, source, binary)
        except ValueError:
            print(json.dumps({"control": name, "result": "rejected"}))
        else:
            raise AssertionError(name + " accepted wrong result")
    for wrong in ("after", "publication"):
        changed = copy.deepcopy(rows)
        changed[0].update(status="Pass", code="OK", publication=True,
                          evidence={"fixtureA": digest(FIXTURE_A), "fixtureB": digest(FIXTURE_B),
                                    "architecture": "X64", "filesystem": "NTFS", "after": digest(FIXTURE_B)})
        if wrong == "after":
            changed[0]["evidence"]["after"] = digest(FIXTURE_A)
        else:
            changed[0]["publication"] = False
        try:
            validate(changed, source, binary)
        except ValueError:
            print(json.dumps({"control": "wrong-" + wrong, "result": "rejected"}))
        else:
            raise AssertionError("wrong " + wrong + " accepted")
    for receipt in [{"quiescent": False, "observed": []},
                    {"quiescent": True, "observed": []}]:
        try:
            validate_tree(receipt)
        except ValueError:
            print(json.dumps({"control": "live-or-unobserved-tree", "result": "rejected"}))
        else:
            raise AssertionError("unproved child cleanup accepted")
    # Independent retained-schema examples for the root's concrete false accepts.
    fixtures = {
        "conflict": {"after": digest(FIXTURE_A), "saveCode": "DOC-CONFLICT", "candidateDurability": False,
                     "targetExists": True, "tempExists": False, "claimExists": False, "cancellationRequested": False},
        "cancel-before": {"saveCode": "DOC-CANCELLED", "candidateDurability": False,
                          "targetExists": False, "tempExists": False, "claimExists": False, "cancellationRequested": True},
        "write-fault": {"saveCode": "DOC-IO", "candidateDurability": False, "targetExists": False,
                        "tempExists": False, "claimExists": False, "writtenBeforeFault": 1, "cancellationRequested": False},
        "cleanup-refusal": {"foreignAfter": digest(FIXTURE_B), "cleanupRefused": True,
                            "targetExists": True, "ownedOriginalExists": True}}
    for case, proof in fixtures.items():
        example = copy.deepcopy(rows)
        row = next(r for r in example if r["case"] == case)
        row.update(status="Pass", code="OK", evidence=dict(proof, fixtureA=digest(FIXTURE_A),
                   fixtureB=digest(FIXTURE_B), architecture="X64", filesystem="NTFS"))
        assert validate(example, source, binary) is False  # Other cases remain NA.
        for field in proof:
            changed = copy.deepcopy(example)
            next(r for r in changed if r["case"] == case)["evidence"][field] = None
            try:
                validate(changed, source, binary)
            except ValueError:
                print(json.dumps({"control": case + "-wrong-" + field, "result": "rejected"}))
            else:
                raise AssertionError(case + " accepted wrong " + field)
    with tempfile.TemporaryDirectory(prefix="cfd-w0-source-control-") as scratch:
        copied = Path(scratch) / "copied-source.cs"
        copied.write_bytes((ROOT / "tools/spikes/WindowsRuntime/Program.cs").read_bytes())
        manifest = {copied.name: digest(copied.read_bytes())}
        assert verify_sources(Path(scratch), manifest) == manifest
        copied.write_bytes(copied.read_bytes() + b"\n// source changed during the simulated operation\n")
        try:
            verify_sources(Path(scratch), manifest)
        except ValueError as error:
            assert str(error) == "W0-SOURCE-DRIFT"
            print(json.dumps({"control": "source-drift-during-operation", "result": "rejected"}))
        else:
            raise AssertionError("source drift accepted")
    # Preserve argparse's real exit: intercepting sys.exit can accidentally run main.
    help_result = subprocess.run([sys.executable, str(Path(__file__).resolve()), "--help"],
                                 capture_output=True, text=True, encoding="utf-8", errors="replace",
                                 timeout=10, env=dict(os.environ, PYTHONIOENCODING="cp1252"))
    assert help_result.returncode == 0 and help_result.stderr == ""
    assert help_result.stdout.startswith("usage: qualify-windows-runtime.py")
    assert '"native_qualification"' not in help_result.stdout
    print(json.dumps({"control": "help-exits-without-qualification", "result": "Pass"}))
    print(json.dumps({"self_test": "Pass", "native_qualification": "Not assessed"}))


def validate_tree(receipt, executable_hash=None):
    if receipt.get("quiescent") is not True or len(receipt.get("observed", [])) < 3:
        raise ValueError("W0-PROCESS-NOT-OBSERVED")
    for child in receipt["observed"]:
        if not all(child.get(k) for k in ("pid", "start", "executable", "sha256")):
            raise ValueError("W0-PROCESS-IDENTITY-MISSING")
        if executable_hash is not None and child["sha256"] != executable_hash:
            raise ValueError("W0-UNEXPECTED-CHILD")
    if len({(p["pid"], p["start"]) for p in receipt["observed"]}) != len(receipt["observed"]):
        raise ValueError("W0-DUPLICATE-CHILD")


def run_local(argv, out, timeout, env):
    started = time.monotonic()
    with (out / "stdout.txt").open("wb") as stdout, (out / "stderr.txt").open("wb") as stderr:
        child = subprocess.Popen(argv, stdout=stdout, stderr=stderr, env=env, cwd=ROOT,
                                 start_new_session=True)
        timed_out = False
        try:
            code = child.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            timed_out = True
            os.killpg(child.pid, signal.SIGKILL)
            code = child.wait(timeout=10)
        try:
            os.killpg(child.pid, 0)
            quiescent = False
        except ProcessLookupError:
            quiescent = True
    return dict(argv=argv, pid=child.pid, exit=code, timeout=timed_out, quiescent=quiescent,
                elapsed_seconds=time.monotonic() - started, native_qualification="Not assessed")


def run_windows(argv, out, timeout, env, observer_fault=False):
    """Create suspended -> assign kill-on-close job -> resume; never kill by PID scan.

    ABI: Microsoft processthreadsapi.h, winnt.h and jobapi2.h, linked in design.
    Every process is owned before its first instruction. Query failure is fatal.
    """
    import ctypes as C
    import msvcrt
    U32, U64, PTR = C.c_uint32, C.c_uint64, C.c_void_p

    class SI(C.Structure):
        _fields_ = [("cb", U32), ("reserved", PTR), ("desktop", PTR), ("title", PTR),
                    ("x", U32), ("y", U32), ("xs", U32), ("ys", U32), ("xc", U32),
                    ("yc", U32), ("fill", U32), ("flags", U32), ("show", C.c_uint16),
                    ("reserved2size", C.c_uint16), ("reserved2", PTR),
                    ("stdin", PTR), ("stdout", PTR), ("stderr", PTR)]

    class PI(C.Structure):
        _fields_ = [("process", PTR), ("thread", PTR), ("pid", U32), ("tid", U32)]

    class Limits(C.Structure):
        _fields_ = [("user", U64), ("job", U64), ("flags", U32), ("min", U64),
                    ("max", U64), ("active", U32), ("affinity", U64),
                    ("priority", U32), ("scheduling", U32), ("io", U64 * 6),
                    ("processmemory", U64), ("jobmemory", U64), ("peakprocess", U64),
                    ("peakjob", U64)]

    assert C.sizeof(SI) == 104 and C.sizeof(PI) == 24 and C.sizeof(Limits) == 144
    api = C.WinDLL("kernel32", use_last_error=True)
    signatures = {
        "CreateJobObjectW": (PTR, [PTR, C.c_wchar_p]),
        "SetInformationJobObject": (U32, [PTR, U32, PTR, U32]),
        "QueryInformationJobObject": (U32, [PTR, U32, PTR, U32, PTR]),
        "CreateProcessW": (U32, [C.c_wchar_p, C.c_wchar_p, PTR, PTR, U32, U32, PTR,
                                  C.c_wchar_p, C.POINTER(SI), C.POINTER(PI)]),
        "AssignProcessToJobObject": (U32, [PTR, PTR]), "ResumeThread": (U32, [PTR]),
        "WaitForSingleObject": (U32, [PTR, U32]), "GetExitCodeProcess": (U32, [PTR, PTR]),
        "TerminateJobObject": (U32, [PTR, U32]), "TerminateProcess": (U32, [PTR, U32]),
        "CloseHandle": (U32, [PTR]), "OpenProcess": (PTR, [U32, U32, U32]),
        "GetProcessTimes": (U32, [PTR, PTR, PTR, PTR, PTR]),
        "QueryFullProcessImageNameW": (U32, [PTR, U32, C.c_wchar_p, PTR])}
    for name, (result, arguments) in signatures.items():
        method = getattr(api, name)
        method.restype, method.argtypes = result, arguments

    def check(ok):
        if not ok:
            raise OSError(C.get_last_error(), "W0-WIN32")

    def active_pids(job):
        # Fixed 128-process ceiling; ERROR_MORE_DATA is a refusal, never truncation.
        buffer = C.create_string_buffer(8 + 128 * 8)
        check(api.QueryInformationJobObject(job, 3, buffer, len(buffer), None))
        assigned, count = U32.from_buffer(buffer).value, U32.from_buffer(buffer, 4).value
        if assigned != count or count > 128:
            raise ValueError("W0-PROCESS-OBSERVER-INCOMPLETE")
        return list((U64 * count).from_buffer(buffer, 8))

    def identity(pid):
        handle = api.OpenProcess(0x1000, 0, pid)
        check(handle)
        try:
            times = [U64() for _ in range(4)]
            check(api.GetProcessTimes(handle, *(C.byref(t) for t in times)))
            name, size = C.create_unicode_buffer(32768), U32(32768)
            check(api.QueryFullProcessImageNameW(handle, 0, name, C.byref(size)))
            return dict(pid=pid, start=times[0].value, executable=name.value,
                        sha256=digest(Path(name.value).read_bytes()))
        finally:
            api.CloseHandle(handle)

    started, observed = time.monotonic(), {}
    job = api.CreateJobObjectW(None, None)
    check(job)
    info, process = Limits(), PI()
    info.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway.
    assigned, quiescent, code, timed_out = False, False, None, False
    try:
        check(api.SetInformationJobObject(job, 9, C.byref(info), C.sizeof(info)))
        with (out / "stdout.txt").open("wb") as stdout, (out / "stderr.txt").open("wb") as stderr, open(os.devnull, "rb") as stdin:
            handles = [msvcrt.get_osfhandle(f.fileno()) for f in (stdin, stdout, stderr)]
            for handle in handles:
                os.set_handle_inheritable(handle, True)
            startup = SI(cb=C.sizeof(SI), flags=0x100, stdin=handles[0], stdout=handles[1], stderr=handles[2])
            block = C.create_unicode_buffer("\0".join(k + "=" + v for k, v in sorted(env.items())) + "\0\0")
            command = C.create_unicode_buffer(subprocess.list2cmdline(argv))
            try:
                check(api.CreateProcessW(argv[0], command, None, None, 1, 0x404,
                                         block, str(ROOT), C.byref(startup), C.byref(process)))
            finally:
                for handle in handles:
                    os.set_handle_inheritable(handle, False)
            check(api.AssignProcessToJobObject(job, process.process))
            assigned = True
            observed[process.pid] = identity(process.pid)
            if api.ResumeThread(process.thread) == 0xFFFFFFFF:
                raise OSError(C.get_last_error(), "W0-RESUME")
            while True:
                if observer_fault:
                    raise ValueError("W0-INJECTED-OBSERVER-FAILURE")
                for pid in active_pids(job):
                    if pid not in observed:
                        observed[pid] = identity(pid)
                wait = api.WaitForSingleObject(process.process, 50)
                if wait == 0:
                    result = U32()
                    check(api.GetExitCodeProcess(process.process, C.byref(result)))
                    code = result.value
                    break
                if wait != 258:
                    raise OSError(C.get_last_error(), "W0-WAIT")
                if time.monotonic() - started >= timeout:
                    timed_out, code = True, 124
                    break
    finally:
        cleanup_error = None
        try:
            if assigned:
                check(api.TerminateJobObject(job, 125))
                deadline = time.monotonic() + 10
                while active_pids(job) and time.monotonic() < deadline:
                    time.sleep(0.02)
                quiescent = not active_pids(job)
            elif process.process:
                # Only the retained, never-resumed process handle is eligible here.
                check(api.TerminateProcess(process.process, 125))
                quiescent = api.WaitForSingleObject(process.process, 10000) == 0
        except (OSError, ValueError) as error:
            cleanup_error = str(error)
        finally:
            for handle in (process.thread, process.process, job):
                if handle:
                    api.CloseHandle(handle)
        receipt = dict(argv=argv, exit=code, timeout=timed_out, quiescent=quiescent,
                       cleanup_error=cleanup_error, observed=list(observed.values()), elapsed_seconds=time.monotonic() - started)
        (out / "process.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8", newline="\n")
    if not quiescent:
        raise ValueError("W0-CLEANUP-NOT-OBSERVED")
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    # Existing paths are never reused or recursively deleted.
    out = args.output.absolute() if args.output else Path(tempfile.gettempdir()) / ("cfd-w0-" + os.urandom(8).hex())
    out.mkdir(parents=False, exist_ok=False)
    manifest = {p: digest((ROOT / p).read_bytes()) for p in SOURCES}
    source = digest(json.dumps(manifest, sort_keys=True, separators=(",", ":")).encode())
    env = dict(os.environ, DOTNET_CLI_HOME=str(out / "dotnet-home"),
               NUGET_PACKAGES=str(out / "nuget"), DOTNET_CLI_TELEMETRY_OPTOUT="1",
               DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
               DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE="true", MSBUILDDISABLENODEREUSE="1")
    windows = sys.platform == "win32" and platform.machine().lower() in ("amd64", "x86_64")
    runner = run_windows if windows else run_local
    summary = dict(schema=1, source=source, sources=manifest, os=platform.platform(),
                   arch=platform.machine(), native_qualification="Not assessed", output=str(out),
                   runner_image={k: os.environ.get(k, "Not recorded") for k in ("ImageOS", "ImageVersion", "RUNNER_ARCH")})
    (out / "source.json").write_text(json.dumps(summary, indent=2), encoding="utf-8", newline="\n")

    def run(name, argv, timeout=90, **kwargs):
        target = out / name
        target.mkdir()
        receipt = runner(argv, target, timeout, env, **kwargs)
        (target / "process.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8", newline="\n")
        if not receipt["quiescent"]:
            raise ValueError("W0-CLEANUP-NOT-OBSERVED")
        return receipt, (target / "stdout.txt").read_text(encoding="utf-8", errors="strict")

    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("W0-SDK-MISSING")
    sdk, version = run("sdk", [dotnet, "--version"])
    if sdk["exit"] != 0 or version.strip() != "10.0.203":
        raise ValueError("W0-SDK-MISMATCH")
    sdk_info, _ = run("sdk-info", [dotnet, "--info"])
    if sdk_info["exit"] != 0:
        raise ValueError("W0-SDK-INFO-MISSING")
    build, _ = run("build", [dotnet, "build", str(ROOT / SOURCES[1]), "--configuration", "Release",
                              "--artifacts-path", str(out / "artifacts"), "--disable-build-servers",
                              "-p:NuGetAudit=false", "-p:UseSharedCompilation=false"], timeout=180)
    if build["exit"] != 0:
        raise ValueError("W0-BUILD-FAILED")
    executable = out / "artifacts/bin/WindowsRuntime/release" / ("WindowsRuntime.exe" if windows else "WindowsRuntime")
    binary_files = {name: digest((executable.parent / name).read_bytes()) for name in
                    (executable.name, "WindowsRuntime.dll", "WindowsRuntime.deps.json", "WindowsRuntime.runtimeconfig.json")}
    binary = digest("".join(name + "\0" + binary_files[name] + "\n" for name in sorted(binary_files)).encode())
    for name, fixture in (("fixture-a.bin", FIXTURE_A), ("fixture-b.bin", FIXTURE_B)):
        (out / name).write_bytes(fixture)
    runenv = dict(env, W0_SOURCE=source)
    env = runenv
    native, output = run("native", [str(executable), str(out / "native-fixtures"), str(out / "fixture-a.bin"), str(out / "fixture-b.bin")])
    rows = [json.loads(line) for line in output.splitlines()]
    qualified = validate(rows, source, binary)
    if (out / "fixture-a.bin").read_bytes() != FIXTURE_A or (out / "fixture-b.bin").read_bytes() != FIXTURE_B:
        raise ValueError("W0-INPUT-MUTATED")
    if windows:
        tree, _ = run("tree-timeout", [str(executable), "--tree-root"], timeout=5)
        validate_tree(tree, binary_files[executable.name])
        if not tree["timeout"] or tree["exit"] != 124:
            raise ValueError("W0-TIMEOUT-NOT-OBSERVED")
        try:
            run("observer-fault", [str(executable), "--tree-root"], timeout=5, observer_fault=True)
        except ValueError as error:
            if str(error) != "W0-INJECTED-OBSERVER-FAILURE":
                raise
            fault = json.loads((out / "observer-fault/process.json").read_text())
            if fault["quiescent"] is not True:
                raise ValueError("W0-FAULT-CLEANUP-NOT-OBSERVED")
        else:
            raise ValueError("W0-OBSERVER-FAULT-ACCEPTED")
        powershell = shutil.which("powershell.exe")
        if not powershell:
            raise ValueError("W0-UIA-PROBE-UNAVAILABLE")
        run("uia-capability", [powershell, "-NoProfile", "-NonInteractive", "-Command",
             "$ErrorActionPreference='Stop'; Add-Type -AssemblyName UIAutomationClient; "
             "$r=[System.Windows.Automation.AutomationElement]::RootElement; "
             "@{rootName=$r.Current.Name; childCount=$r.FindAll([System.Windows.Automation.TreeScope]::Children,"
             "[System.Windows.Automation.Condition]::TrueCondition).Count; "
             "userInteractive=[Environment]::UserInteractive; sessionId=(Get-Process -Id $PID).SessionId} | ConvertTo-Json -Compress"])
    failed = any(row["status"] == "Fail" for row in rows) or native["exit"] not in (0, 3)
    summary.update(sources_after=verify_sources(ROOT, manifest), binary=binary, binary_files=binary_files, sdk=version.strip(), native_exit=native["exit"],
                   native_qualification="Fail" if failed else "Pass" if qualified and native["exit"] == 0 and windows else "Not assessed")
    (out / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8", newline="\n")
    print(json.dumps(summary, sort_keys=True))
    return 1 if failed else 0 if summary["native_qualification"] == "Pass" else 3


if __name__ == "__main__":
    # PLAT-A: match pack-doctor's legacy-console guard; never depend on cp1252.
    for _stream in (sys.stdout, sys.stderr):
        if hasattr(_stream, "reconfigure"):
            try:
                _stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        sys.exit(main())
    except (OSError, ValueError, AssertionError, subprocess.SubprocessError) as error:
        print(json.dumps({"code": "W0-FAILED", "error": str(error)}), file=sys.stderr)
        sys.exit(1)
