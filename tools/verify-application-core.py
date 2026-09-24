#!/usr/bin/env python3
"""Build and exercise the UI-free core with task-local outputs and owned processes."""
from __future__ import annotations

import json
import os
from pathlib import Path
import signal
import shutil
import subprocess
import sys
import tempfile
import time

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parents[1]

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
        label: str | None = None, cwd: Path | None = None) -> None:
    if os.name == "nt":
        raise RuntimeError("Windows process ownership adapter: Not assessed; no child launched")
    started = time.monotonic()
    owned: dict[int, str] = {}
    working_directory = cwd or ROOT
    receipt: dict[str, object] = {"command": command, "cwd": str(working_directory), "child_umask": oct(child_umask) if child_umask >= 0 else "inherited"}
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
    canonical = scratch.resolve(strict=True)
    allowed_parent = Path("/tmp").resolve(strict=True)
    if canonical.parent != allowed_parent:
        raise RuntimeError("Scratch escaped the explicitly selected /tmp parent")
    print(json.dumps({"scratch": str(scratch), "canonical_scratch": str(canonical),
                      "tmp_canonical_alias": str(allowed_parent)}), flush=True)
    environment = dict(os.environ)
    # Fault-probe selectors are owned by this gate; an inherited selector must
    # never silently skip the normal production store suite.
    for selector in ("CFD_NATIVE_CAPABILITY_PROBE", "CFD_OWNER_STRIPPING_MASK", "CFD_OWNER_STRIPPING_ROOT"):
        environment.pop(selector, None)
    for key, directory in {"DOTNET_CLI_HOME": "dotnet-home", "NUGET_PACKAGES": "nuget", "NUGET_HTTP_CACHE_PATH": "http-cache", "TMPDIR": "tmp", "TMP": "tmp", "TEMP": "tmp"}.items():
        path = scratch / directory
        path.mkdir(parents=True, exist_ok=True)
        environment[key] = str(path)
    (scratch / "receipts").mkdir(exist_ok=True)
    environment.update(DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1",
                       DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false")
    receipt_environment = {key: environment[key] for key in (
        "DOTNET_CLI_HOME", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "TMPDIR", "TMP", "TEMP",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "DOTNET_CLI_TELEMETRY_OPTOUT",
        "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "DOTNET_GENERATE_ASPNET_CERTIFICATE",
    )}
    (scratch / "receipts" / "environment.json").write_text(json.dumps(receipt_environment, indent=2), encoding="utf-8", newline="\n")
    artifacts = scratch / "artifacts"
    # SIGTERM follows the same finally/owned-cleanup path as an interactive interrupt.
    signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(128 + signum))
    run(["dotnet", "build", "CFDWorkbench.slnx", "--artifacts-path", str(artifacts), "--disable-build-servers", "-p:UseSharedCompilation=false", "--nologo"], environment, scratch)
    for mask in (0, 0o22, 0o77):
        environment["CFD_TEST_UMASK"] = f"{mask:04o}"
        run(["dotnet", str(artifacts / "bin" / "CfdWorkbench.Core.Tests" / "debug" / "CfdWorkbench.Core.Tests.dll")],
            environment, scratch, child_umask=mask, label=f"tests-{mask:04o}")
    published = scratch / "published"
    run(["dotnet", "publish", str(ROOT / "tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj"),
         "-c", "Release", "--artifacts-path", str(artifacts), "--output", str(published), "--disable-build-servers",
         "-p:UseSharedCompilation=false", "-p:UseAppHost=false", "--nologo"], environment, scratch, label="publish")
    # Test data belongs to this test package, not to production native-library resolution.
    shutil.copytree(ROOT / "docs/examples/foildsl", published / "docs/examples/foildsl")
    for mask in (0, 0o22, 0o77):
        environment["CFD_TEST_UMASK"] = f"{mask:04o}"
        run(["dotnet", str(published / "CfdWorkbench.Core.Tests.dll")], environment, scratch,
            child_umask=mask, label=f"published-{mask:04o}", cwd=published)
    environment["CFD_OWNER_STRIPPING_MASK"] = "0600"
    owner_stripping_root = scratch / "owner-stripping-parent"
    owner_stripping_root.mkdir(mode=0o700)
    environment["CFD_OWNER_STRIPPING_ROOT"] = str(owner_stripping_root.resolve())
    environment["CFD_TEST_UMASK"] = "0600"
    run(["dotnet", str(published / "CfdWorkbench.Core.Tests.dll")], environment, scratch,
        child_umask=0o600, label="owner-stripping", cwd=published)
    del environment["CFD_OWNER_STRIPPING_MASK"]
    del environment["CFD_OWNER_STRIPPING_ROOT"]
    environment["CFD_TEST_UMASK"] = "0077"
    for variant in ("missing", "unloadable"):
        isolated = scratch / variant
        shutil.copytree(published, isolated)
        helper = isolated / "libcfd_store.dylib"
        if variant == "missing":
            helper.unlink()
        else:
            helper.write_bytes(b"deliberately invalid native helper")
        environment["CFD_NATIVE_CAPABILITY_PROBE"] = variant
        run(["dotnet", str(isolated / "CfdWorkbench.Core.Tests.dll")], environment, scratch,
            child_umask=0o77, label=variant, cwd=isolated)


if __name__ == "__main__":
    main()
