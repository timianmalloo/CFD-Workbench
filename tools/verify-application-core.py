#!/usr/bin/env python3
"""Build and exercise the UI-free core with task-local outputs and owned processes."""
from __future__ import annotations

import atexit
from concurrent.futures import ThreadPoolExecutor
import json
import os
from pathlib import Path
import re
import signal
import shutil
import subprocess
import sys
import tempfile
import threading
import time

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parents[1]
# The umask- and native-sensitive checks: every check in ProjectStoreTests.cs, LayoutFileTests.cs
# and PreferenceStoreTests.cs. They are the only Core checks that create files, read
# CFD_TEST_UMASK, load libcfd_store, or (P1) depend on the preference store's owner-only file
# modes. The rest of the suite runs once per build shape; these also run under the other masks
# and the fault variants (F2).
STORE_TESTS = ROOT / "tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs"
STORE_TEST_FILES = (
    STORE_TESTS,
    ROOT / "tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs",
    ROOT / "tests/CfdWorkbench.Core.Tests/PreferenceStoreTests.cs",
)
STORE_PREFIXES = ("Store_", "NativePrimitive_",
                   "LayoutParse_", "LayoutCodec_", "RecentParse_",
                   "LayoutLoad_", "Rollback_", "PrefStore_", "PrefsSave_", "LayoutSave_",
                   "Recent_", "StoreContract_")
STORE_SUBSET = ",".join(STORE_PREFIXES)
# Checks that run only under a fault variant, never in a normal run.
# The published full suite runs as this many concurrent parts (test-cost L3; part model 16.6/14.8/15.1 s of 45 s).
PARTS = 3
VARIANT_CHECKS = {"Store_OwnerStrippingUmask_FailsClosedWithoutRepair", "Store_MissingOrUnloadableHelper_FailsClosed"}
# Anything that could make a check depend on the umask, the environment or the native helper.
# Reading the example files, and listing a directory to read it, are umask-independent and allowed
# (a umask only shapes the modes of files a process creates). The harness entry point reads
# CFD_TEST_ONLY and names every suite, so it is exempt.
SENSITIVE = re.compile(r"\bFile\.(?!ReadAll(?:Bytes|Text)\b)|\bDirectory\.(?!(?:EnumerateFiles|GetFiles)\b)|\bFileStream\b|\bFileInfo\b|GetTempPath"
                       r"|GetEnvironmentVariable|DllImport|LibraryImport|\bProjectStore\b"
                       r'|(?<!InternalsVisibleTo\(")CfdWorkbench\.Persistence')
PARTITION_EXEMPT = {path.name for path in STORE_TEST_FILES} | {"IdentityTests.cs"}


def store_checks_selectable() -> set[str]:
    """Return the normal-run store check names; fail if the umask partition no longer holds."""
    names: list[str] = []
    for file in STORE_TEST_FILES:
        source = file.read_text(encoding="utf-8")
        file_names = re.findall(r'\bCheck\("([^"]+)"', source)
        stray = [name for name in file_names if not name.startswith(STORE_PREFIXES)]
        if not file_names or stray or len(file_names) != len(re.findall(r"\bCheck\(", source)):
            raise SystemExit(f"STORE-SUBSET: every check in {file.name} needs a literal name "
                             f"starting with one of {STORE_PREFIXES}; found {len(file_names)}, stray {stray}")
        names.extend(file_names)
    # A file, environment or native dependency outside the store files would run at one umask only.
    others = [path for path in sorted(STORE_TESTS.parent.glob("*.cs")) if path.name not in PARTITION_EXEMPT]
    others += sorted((ROOT / "src/CfdWorkbench.Core").glob("*.cs"))
    leaks = [f"{path.parent.name}/{path.name}:{number}" for path in others
             for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1) if SENSITIVE.search(line)]
    if leaks:
        raise SystemExit(f"STORE-SUBSET: umask/native-sensitive code outside {STORE_TEST_FILES} would run at one "
                         f"umask only; move it into the store checks or widen the subset: {leaks}")
    return set(names) - VARIANT_CHECKS


def passes(scratch: Path, label: str) -> set[str]:
    log = (scratch / "receipts" / f"{label}.log").read_text(encoding="utf-8")
    return {line[len("PASS "):] for line in log.splitlines() if line.startswith("PASS ")}


def require_passes(label: str, expected: set[str], actual: set[str]) -> None:
    """An exit 0 is not a result: the selected run must pass exactly the expected checks."""
    if not expected or actual != expected:
        raise SystemExit(f"STORE-SUBSET: {label} passed {len(actual)} checks, expected {len(expected)}; "
                         f"missing {sorted(expected - actual)}, extra {sorted(actual - expected)}")

def process_table() -> dict[int, dict[str, object]]:
    """Read POSIX process identities; never substitute a guessed identity."""
    if os.name == "nt":
        raise RuntimeError("Windows process ownership adapter: Not assessed")
    output = subprocess.check_output(
        ["ps", "-axo", "pid=,ppid=,pgid=,state=,lstart="],
        cwd=ROOT, text=True, encoding="utf-8", timeout=10,
    )
    rows = {}
    for line in output.splitlines():
        fields = line.split()
        if len(fields) != 9:
            raise RuntimeError("Unexpected process identity record")
        rows[int(fields[0])] = {
            "parent": int(fields[1]), "group": int(fields[2]),
            "state": fields[3], "start": " ".join(fields[4:]),
        }
    return rows


def observe(group: int, owned: dict[int, str]) -> dict[int, dict[str, object]]:
    table = process_table()
    for pid, row in table.items():
        if row["group"] == group:
            prior = owned.get(pid)
            if prior is not None and prior != row["start"]:
                raise RuntimeError("Owned process identity changed")
            owned[pid] = str(row["start"])
    return {
        pid: row for pid, row in table.items()
        if pid in owned and owned[pid] == row["start"] and not str(row["state"]).startswith("Z")
    }


def terminate_owned(group: int, owned: dict[int, str]) -> None:
    """Signal only PID/start pairs observed inside this invocation's process group."""
    if os.name == "nt":
        raise RuntimeError("Windows process ownership adapter: Not assessed")
    for requested_signal in (signal.SIGTERM, signal.SIGKILL):
        live = observe(group, owned)
        for pid in live:
            current = process_table().get(pid)
            if current is not None and current["start"] == owned[pid]:
                try:
                    os.kill(pid, requested_signal)
                except ProcessLookupError:
                    pass  # The verified process exited between observation and signal.
        deadline = time.monotonic() + 5
        while time.monotonic() < deadline:
            if not observe(group, owned):
                return
            time.sleep(0.05)
    raise RuntimeError("Owned processes remain alive after cleanup")


def write_receipt(scratch: Path, receipt: dict[str, object]) -> None:
    path = scratch / "receipts" / f"process-{receipt['pid']}.json"
    path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8", newline="\n")


def reap_direct_child(child: subprocess.Popen[str]) -> None:
    """Fallback retains Popen ownership; it makes no descendant cleanup claim."""
    if child.poll() is None:
        child.terminate()
    try:
        child.wait(timeout=5)
    except subprocess.TimeoutExpired:
        child.kill()
        child.wait(timeout=5)


def run(command: list[str], environment: dict[str, str], scratch: Path, child_umask: int = -1,
        label: str | None = None, cwd: Path | None = None, suite: str | None = None,
        stop: threading.Event | None = None) -> None:
    if os.name == "nt":
        raise RuntimeError("Windows process ownership adapter: Not assessed; no child launched")
    started = time.monotonic()
    owned: dict[int, str] = {}
    working_directory = cwd or ROOT
    receipt: dict[str, object] = {"command": command, "cwd": str(working_directory), "child_umask": oct(child_umask) if child_umask >= 0 else "inherited"}
    if suite is not None:
        # Since F2 a test label no longer implies the full suite; this field says which checks ran.
        receipt["suite"] = suite
    output_path = scratch / "receipts" / ((label or ("build" if command[1] == "build" else "tests")) + ".log")
    # Verify the observer before acquiring a child it must later identify and clean.
    process_table()
    with output_path.open("w", encoding="utf-8") as output:
        child = subprocess.Popen(command, cwd=working_directory, env=environment, start_new_session=True, stdout=output, stderr=subprocess.STDOUT, umask=child_umask)
        receipt.update(pid=child.pid, output=str(output_path))
        try:
            first_observation = True
            while True:
                code = child.poll()
                live = observe(child.pid, owned)
                receipt.update(owned=owned, live=live)
                write_receipt(scratch, receipt)
                if first_observation:
                    print(json.dumps(receipt), flush=True)
                    first_observation = False
                if code is not None:
                    break
                if time.monotonic() - started >= 180:
                    raise subprocess.TimeoutExpired(command, 180)
                if stop is not None and stop.is_set():
                    raise RuntimeError("Stopped: a concurrent part of this run failed")
                time.sleep(0.05)
            if live:
                raise RuntimeError("Build exited with live owned descendants")
        finally:
            cleanup_failure = None
            try:
                terminate_owned(child.pid, owned)
                child.wait(timeout=5)
                survivors = observe(child.pid, owned)
                if survivors:
                    raise RuntimeError("Owned descendants appeared after cleanup")
                receipt.update(live=survivors, quiescent=True)
            except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as failure:
                cleanup_failure = failure
                receipt.update(live="Not assessed", quiescent="Not assessed",
                               cleanup="Observation failed; direct child fallback only")
                reap_direct_child(child)
            finally:
                receipt.update(exit_code=child.poll(), duration_seconds=time.monotonic() - started)
                write_receipt(scratch, receipt)
                print(json.dumps(receipt), flush=True)
                output.flush()
                print(output_path.read_text(encoding="utf-8"), flush=True)
            if cleanup_failure is not None:
                raise RuntimeError("Process observation failed; descendant quiescence Not assessed; route stopped") from cleanup_failure
    if code:
        raise SystemExit(code)


def main() -> None:
    # Temporary capability guard: Windows ownership/cleanup still requires native proof.
    if os.name == "nt":
        raise SystemExit("Windows gate/runtime: Not assessed; no dotnet process launched")
    scratch = Path(tempfile.mkdtemp(prefix="cfd-application-core-20260923-", dir="/tmp"))
    # A red run keeps its scratch for debugging; say where, whichever way the run fails.
    atexit.register(lambda: (scratch / "artifacts").exists() and print(f"SCRATCH kept: {scratch}", flush=True))
    canonical = scratch.resolve(strict=True)
    allowed_parent = Path("/tmp").resolve(strict=True)
    if canonical.parent != allowed_parent:
        raise RuntimeError("Scratch escaped the explicitly selected /tmp parent")
    print(json.dumps({"scratch": str(scratch), "canonical_scratch": str(canonical),
                      "tmp_canonical_alias": str(allowed_parent)}), flush=True)
    environment = dict(os.environ)
    # Fault-probe selectors are owned by this gate; an inherited selector must
    # never silently skip the normal production store suite.
    for selector in ("CFD_NATIVE_CAPABILITY_PROBE", "CFD_OWNER_STRIPPING_MASK", "CFD_OWNER_STRIPPING_ROOT", "CFD_TEST_ONLY"):
        environment.pop(selector, None)
    for key, directory in {"DOTNET_CLI_HOME": "dotnet-home", "NUGET_PACKAGES": "nuget", "NUGET_HTTP_CACHE_PATH": "http-cache", "TMPDIR": "tmp", "TMP": "tmp", "TEMP": "tmp"}.items():
        path = scratch / directory
        path.mkdir(parents=True, exist_ok=True)
        environment[key] = str(path)
    (scratch / "receipts").mkdir(exist_ok=True)
    environment.update(DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1",
                       DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false",
                       # Without it Avalonia's build telemetry collector joins the build's process group and can outlive it.
                       AVALONIA_TELEMETRY_OPTOUT="1")
    receipt_environment = {key: environment[key] for key in (
        "DOTNET_CLI_HOME", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "TMPDIR", "TMP", "TEMP",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "DOTNET_CLI_TELEMETRY_OPTOUT",
        "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "DOTNET_GENERATE_ASPNET_CERTIFICATE", "AVALONIA_TELEMETRY_OPTOUT",
    )}
    (scratch / "receipts" / "environment.json").write_text(json.dumps(receipt_environment, indent=2), encoding="utf-8", newline="\n")
    artifacts = scratch / "artifacts"
    # SIGTERM follows the same finally/owned-cleanup path as an interactive interrupt.
    signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(128 + signum))

    def tests(dll: Path, mask: int, label: str, cwd: Path | None = None, only: str | None = None) -> set[str]:
        """Run the Core harness; `only` sets the harness check-name prefix selector CFD_TEST_ONLY."""
        environment["CFD_TEST_UMASK"] = f"{mask:04o}"
        environment.pop("CFD_TEST_ONLY", None)
        if only is not None:
            environment["CFD_TEST_ONLY"] = only
        run(["dotnet", str(dll)], environment, scratch, child_umask=mask, label=label, cwd=cwd,
            suite="full" if only is None else f"subset CFD_TEST_ONLY={only}")
        return passes(scratch, label)

    def full_in_parts(dll: Path, label: str, cwd: Path | None, count: int = PARTS) -> set[str]:
        """The full suite at 0022 as `count` concurrent `--part=k/n` processes (test-cost L3). The parts run each
        check once only if every part printed one `PARTITION k/n of N checks` line, k covers 1..n, N is the same,
        and no check passed in two parts; otherwise a check could drop out silently, so the gate fails."""
        environment["CFD_TEST_UMASK"] = "0022"
        environment.pop("CFD_TEST_ONLY", None)
        labels = [f"{label}-part{k}of{count}" for k in range(1, count + 1)]
        stop, errors = threading.Event(), []
        with ThreadPoolExecutor(count) as pool:
            futures = [pool.submit(run, ["dotnet", str(dll), f"--part={k}/{count}"], dict(environment), scratch, 0o22,
                                   labels[k - 1], cwd, f"full part {k}/{count}", stop) for k in range(1, count + 1)]
            for future in futures:
                try:
                    future.result()
                except BaseException as error:  # a red part (SystemExit) or a signal: stop the siblings, keep the first
                    stop.set()
                    errors.append(error)
        if errors:
            raise errors[0]
        union: set[str] = set()
        passed, registered, reports = 0, set(), []
        for k, part_label in enumerate(labels, 1):
            log = (scratch / "receipts" / f"{part_label}.log").read_text(encoding="utf-8").splitlines()
            lines = [line for line in log if line.startswith("PARTITION ")]
            reports += lines
            match = re.fullmatch(r"PARTITION (\d+)/(\d+) of (\d+) checks", lines[0]) if len(lines) == 1 else None
            registered.add(match[3] if match and (int(match[1]), int(match[2])) == (k, count) else f"bad part {k}")
            passed += sum(1 for line in log if line.startswith("PASS "))
            union |= passes(scratch, part_label)
        if len(registered) != 1 or not next(iter(registered)).isdigit() or passed != len(union):
            raise SystemExit(f"PARTITION: the {count} parts of {label} are incomplete, enumerated different checks, or "
                             f"ran a check twice: {reports}; {passed} PASS lines, {len(union)} distinct")
        print(f"PARTITION {label}: {count} parts, {next(iter(registered))} checks registered, "
              f"{len(union)} passed once each", flush=True)
        return union

    def store_masks(dll: Path, prefix: str, cwd: Path | None = None, full_suite: bool = True) -> None:
        """The store checks under all three masks; with `full_suite`, the whole suite at 0022.
        Every run must pass every store check named in the source, so a check that silently
        stops running at any mask fails the gate. The non-published Debug shape runs the store
        subset only (test-cost L1): the full suite runs in Release every join, and
        tools/check-debug-parity.py fails if Debug and Release could run different code."""
        if full_suite:
            full = full_in_parts(dll, f"{prefix}-0022", cwd)
            require_passes(f"{prefix}-0022 store checks", store_checks, {name for name in full if name.startswith(STORE_PREFIXES)})
        else:
            require_passes(f"{prefix}-0022", store_checks, tests(dll, 0o22, f"{prefix}-0022", cwd, only=STORE_SUBSET))
        for mask in (0, 0o77):
            label = f"{prefix}-{mask:04o}"
            require_passes(label, store_checks, tests(dll, mask, label, cwd, only=STORE_SUBSET))

    store_checks = store_checks_selectable()
    run(["dotnet", "build", "CFDWorkbench.slnx", "--artifacts-path", str(artifacts), "--disable-build-servers", "-p:UseSharedCompilation=false", "--nologo"], environment, scratch)
    store_masks(artifacts / "bin" / "CfdWorkbench.Core.Tests" / "debug" / "CfdWorkbench.Core.Tests.dll", "tests", full_suite=False)
    published = scratch / "published"
    run(["dotnet", "publish", str(ROOT / "tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj"),
         "-c", "Release", "--artifacts-path", str(artifacts), "--output", str(published), "--disable-build-servers",
         "-p:UseSharedCompilation=false", "-p:UseAppHost=false", "--nologo"], environment, scratch, label="publish")
    # Test data belongs to this test package, not to production native-library resolution.
    shutil.copytree(ROOT / "docs/examples/foildsl", published / "docs/examples/foildsl")
    store_masks(published / "CfdWorkbench.Core.Tests.dll", "published", published)
    environment["CFD_OWNER_STRIPPING_MASK"] = "0600"
    owner_stripping_root = scratch / "owner-stripping-parent"
    owner_stripping_root.mkdir(mode=0o700)
    environment["CFD_OWNER_STRIPPING_ROOT"] = str(owner_stripping_root.resolve())
    check = "Store_OwnerStrippingUmask_FailsClosedWithoutRepair"
    require_passes("owner-stripping", {check},
                   tests(published / "CfdWorkbench.Core.Tests.dll", 0o600, "owner-stripping", published, only=check))
    del environment["CFD_OWNER_STRIPPING_MASK"]
    del environment["CFD_OWNER_STRIPPING_ROOT"]
    check = "Store_MissingOrUnloadableHelper_FailsClosed"
    for variant in ("missing", "unloadable"):
        isolated = scratch / variant
        shutil.copytree(published, isolated)
        helper = isolated / "libcfd_store.dylib"
        if variant == "missing":
            helper.unlink()
        else:
            helper.write_bytes(b"deliberately invalid native helper")
        environment["CFD_NATIVE_CAPABILITY_PROBE"] = variant
        require_passes(variant, {check},
                       tests(isolated / "CfdWorkbench.Core.Tests.dll", 0o77, variant, isolated, only=check))
    prune_scratch(scratch)


def prune_scratch(scratch: Path) -> None:
    """Green: delete the build, package cache and published copies (about 1.6 GB a run), keep receipts/ (the
    step logs and durations). Red never reaches here, so a failed run keeps everything (test-cost F-1)."""
    removed = 0
    for item in scratch.iterdir():
        if item.name == "receipts":
            continue
        if item.is_dir() and not item.is_symlink():
            removed += sum(path.lstat().st_size for path in item.rglob("*") if path.is_file())
            shutil.rmtree(item)
        else:
            removed += item.lstat().st_size
            item.unlink()
    print(f"SCRATCH green: removed {removed / 1e9:.2f} GB, kept {scratch / 'receipts'}", flush=True)


if __name__ == "__main__":
    main()
