#!/usr/bin/env python3
"""Run the bounded Windows-native store qualification subset; this does not admit production writes."""
from __future__ import annotations

import time

PROCESS_STARTED = time.monotonic()

import ctypes
import datetime as dt
from contextlib import redirect_stderr, redirect_stdout
import hashlib
import io
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import sys
import tempfile
import threading
from unittest.mock import patch

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj"
TEST_SOURCE = ROOT / "tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs"
GLOBAL_JSON = ROOT / "global.json"
MAX_SECONDS = 60.0
TARGET_SECONDS = 15.0
KILL_RESERVE_SECONDS = 5.0
CLEANUP_SECONDS = 2.0
MAX_CPUS = 6
TIMEOUT_EXIT = 124
CLEANUP_FAILED_EXIT = 125
SELECTOR_PREFIXES = (
    "WindowsNative_ProductCode_",
    "WindowsStore_",
    "WindowsNative_Environment_",
    "WindowsNative_Layouts_",
    "WindowsNative_RelativeCreate_",
    "WindowsNative_Replace_ShareRead",
    "WindowsNative_Replace_HeldReaderKeepsOldImage",
    "WindowsNative_ClaimDispose_",
    "WindowsNative_Rename_",
    "WindowsNative_CreateOnly_",
    "WindowsNative_RelativeName_",
)
EXPECTED_REGISTERED = {
    "WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped",
    "WindowsStore_Unqualified_ProductionAdmissionRemainsClosed",
    "WindowsNative_Environment_RealNtfsX64",
    "WindowsNative_Layouts_SdkX64",
    "WindowsNative_RelativeCreate_PrivateAclBeforeAnyBytes",
    "WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage",
    "WindowsNative_Replace_ShareReadOnlyReaderRefused",
    "WindowsNative_Replace_HeldReaderKeepsOldImage",
    "WindowsNative_ClaimDispose_HeldObserverNamespaceRemoved",
    "WindowsNative_Rename_FreshWin32VersusNtFixtures",
    "WindowsNative_Rename_AttributionNullRootExAndRootedLegacy",
    "WindowsNative_CreateOnly_CollisionPreservesBothObjects",
    "WindowsNative_RelativeName_TraversalRefusedBeforeCreate",
}
HISTORICAL_EXPECTED_FAILURE = "WindowsNative_Replace_HeldReaderKeepsOldImage"
EXPECTED_FAILURES = {HISTORICAL_EXPECTED_FAILURE}
EXPECTED_PASSES = EXPECTED_REGISTERED - EXPECTED_FAILURES
SELECTOR = ",".join(SELECTOR_PREFIXES)


def utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="microseconds").replace("+00:00", "Z")


def redact(value: str) -> str:
    profile = os.environ.get("USERPROFILE")
    if profile:
        value = re.sub(re.escape(profile), "%USERPROFILE%", value, flags=re.IGNORECASE)
    value = re.sub(r"(?i)[A-Z]:[\\/]+Users[\\/]+[^\\/\r\n]+", "%USERPROFILE%", value)
    value = re.sub(r"(?i)S-1-5-21-\d+-\d+-\d+-\d+", "S-1-5-21-<machine>-<RID>", value)
    return value


def set_six_cpu_affinity() -> tuple[int, int]:
    """Restrict this process so every child inherits at most six available logical CPUs."""
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.GetCurrentProcess.restype = ctypes.c_void_p
    handle = kernel.GetCurrentProcess()
    process_mask = ctypes.c_size_t()
    system_mask = ctypes.c_size_t()
    kernel.GetProcessAffinityMask.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_size_t), ctypes.POINTER(ctypes.c_size_t)]
    kernel.GetProcessAffinityMask.restype = ctypes.c_int
    kernel.SetProcessAffinityMask.argtypes = [ctypes.c_void_p, ctypes.c_size_t]
    kernel.SetProcessAffinityMask.restype = ctypes.c_int
    if not kernel.GetProcessAffinityMask(handle, ctypes.byref(process_mask), ctypes.byref(system_mask)):
        raise OSError(ctypes.get_last_error(), "GetProcessAffinityMask failed")
    selected = choose_affinity_mask(process_mask.value & system_mask.value, MAX_CPUS)
    if not selected:
        raise RuntimeError("No logical processor is available for this process")
    if not kernel.SetProcessAffinityMask(handle, selected):
        raise OSError(ctypes.get_last_error(), "SetProcessAffinityMask failed")
    return process_mask.value, selected


def _remaining(deadline: float, cap: float) -> float:
    return min(cap, deadline - time.monotonic())


def _close_streams(process: subprocess.Popen) -> None:
    for stream in (process.stdout, process.stderr):
        if stream:
            stream.close()


def _close_streams_bounded(process: subprocess.Popen, deadline: float) -> bool:
    """Close pipe streams off the ceiling thread; report when a reader still owns them."""
    closer = threading.Thread(
        target=_close_streams, args=(process,), name=f"pipe-close-{process.pid}", daemon=True,
    )
    closer.start()
    closer.join(timeout=max(0.0, min(0.02, deadline - time.monotonic())))
    return not closer.is_alive()


def _terminate_tree(process: subprocess.Popen, deadline: float) -> tuple[bytes, bytes, bool, str]:
    """Stop the process tree and reap the child using bounded waits only."""
    cleanup_started = time.monotonic()
    deadline = min(deadline, cleanup_started + CLEANUP_SECONDS)
    problems: list[str] = []
    killer = None
    if time.monotonic() >= deadline:
        problems.append("cleanup deadline elapsed before taskkill")
    else:
        try:
            killer = subprocess.Popen(
                ["taskkill.exe", "/PID", str(process.pid), "/T", "/F"], stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                creationflags=subprocess.CREATE_NO_WINDOW,
            )
            try:
                wait = _remaining(deadline, 2.0)
                if wait <= 0:
                    raise subprocess.TimeoutExpired(killer.args, timeout=wait)
                killer.communicate(timeout=wait)
            except subprocess.TimeoutExpired:
                problems.append("taskkill exceeded its bounded wait")
                killer.kill()
                try:
                    wait = _remaining(deadline, 0.25)
                    if wait <= 0:
                        raise subprocess.TimeoutExpired(killer.args, timeout=wait)
                    killer.communicate(timeout=wait)
                except subprocess.TimeoutExpired:
                    problems.append("taskkill did not stop after kill")
                    if not _close_streams_bounded(killer, deadline):
                        problems.append("taskkill pipe close remains pending off the ceiling thread")
            if killer.returncode not in (0, None):
                problems.append(f"taskkill exited {killer.returncode}")
            elif killer.returncode is None:
                problems.append("taskkill completion was not observed")
        except OSError as error:
            problems.append(f"taskkill launch failed: {error}")

    stdout = b""
    stderr = b""
    try:
        wait = _remaining(deadline, 0.5)
        if wait <= 0:
            raise subprocess.TimeoutExpired(process.args, timeout=wait)
        stdout, stderr = process.communicate(timeout=wait)
    except subprocess.TimeoutExpired as timed_out:
        problems.append("target process tree remained after taskkill")
        stdout = timed_out.output or b""
        stderr = timed_out.stderr or b""
        try:
            process.kill()
        except OSError as error:
            problems.append(f"target process kill failed: {error}")
        try:
            wait = _remaining(deadline, 0.5)
            if wait <= 0:
                raise subprocess.TimeoutExpired(process.args, timeout=wait)
            stdout, stderr = process.communicate(timeout=wait)
        except subprocess.TimeoutExpired as still_running:
            stdout = still_running.output or stdout
            stderr = still_running.stderr or stderr
            problems.append("target process cleanup did not finish within its bounded wait")
            if not _close_streams_bounded(process, deadline):
                problems.append("target pipe close remains pending off the ceiling thread")

    if problems:
        root_exit = process.poll()
        root_state = "still-running-or-unreaped" if root_exit is None else str(root_exit)
        kill_state = "not-started" if killer is None else str(killer.returncode)
        problems.append(
            f"residual process tree state: taskkill_exit={kill_state}; "
            f"root_pid={process.pid}; root_exit={root_state}; "
            "descendant termination not verified"
        )

    detail = "; ".join(problems)
    return stdout, stderr, not problems, detail


def run_capture(
    command: list[str], *, env: dict[str, str], deadline: float, total_deadline: float, cwd: Path = ROOT,
) -> tuple[int, bytes, bytes, float]:
    started = time.monotonic()
    wait = _remaining(deadline, MAX_SECONDS)
    if wait <= 0:
        return TIMEOUT_EXIT, b"", b"process deadline elapsed before launch", time.monotonic() - started
    flags = subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.CREATE_NO_WINDOW
    process = subprocess.Popen(command, cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, creationflags=flags)
    wait = _remaining(deadline, MAX_SECONDS)
    if wait <= 0:
        stdout, stderr, stopped, detail = _terminate_tree(process, total_deadline)
        if not stopped:
            stderr += f"\nFAIL TREE_CLEANUP_FAILED {detail}\n".encode()
            return CLEANUP_FAILED_EXIT, stdout, stderr, time.monotonic() - started
        return TIMEOUT_EXIT, stdout, stderr, time.monotonic() - started
    try:
        stdout, stderr = process.communicate(timeout=wait)
        return process.returncode, stdout, stderr, time.monotonic() - started
    except subprocess.TimeoutExpired:
        stdout, stderr, stopped, detail = _terminate_tree(process, total_deadline)
        if not stopped:
            stderr += f"\nFAIL TREE_CLEANUP_FAILED {detail}\n".encode()
            return CLEANUP_FAILED_EXIT, stdout, stderr, time.monotonic() - started
        return TIMEOUT_EXIT, stdout, stderr, time.monotonic() - started


def decode(data: bytes) -> str:
    return redact(data.decode("utf-8", errors="replace"))


def child_output(label: str, stdout: bytes, stderr: bytes) -> None:
    sys.stdout.flush()
    sys.stderr.flush()
    out_stream = sys.stdout.buffer
    err_stream = sys.stderr.buffer
    out_stream.write(f"{label}_STDOUT_BEGIN\n".encode())
    out_stream.write(decode(stdout).encode("utf-8"))
    if stdout and not stdout.endswith((b"\n", b"\r")):
        out_stream.write(b"\n")
    out_stream.write(f"{label}_STDOUT_END\n".encode())
    err_stream.write(f"{label}_STDERR_BEGIN\n".encode())
    err_stream.write(decode(stderr).encode("utf-8"))
    if stderr and not stderr.endswith((b"\n", b"\r")):
        err_stream.write(b"\n")
    err_stream.write(f"{label}_STDERR_END\n".encode())
    out_stream.flush()
    err_stream.flush()


def sdk_candidates(env: dict[str, str]) -> list[tuple[str, Path]]:
    candidates: list[tuple[str, Path]] = []
    profile = env.get("USERPROFILE")
    if profile:
        candidates.append(("user-local", Path(profile) / ".dotnet/dotnet.exe"))
    root = env.get("DOTNET_ROOT")
    if root:
        candidates.append(("DOTNET_ROOT", Path(root) / "dotnet.exe"))
    located = shutil.which("dotnet.exe") or shutil.which("dotnet")
    if located:
        candidates.append(("PATH", Path(located)))
    unique: list[tuple[str, Path]] = []
    seen: set[str] = set()
    for label, path in candidates:
        normalized = os.path.normcase(os.path.abspath(path))
        if normalized not in seen:
            unique.append((label, path))
            seen.add(normalized)
    return unique


def parse_registered_checks(source: str) -> list[str]:
    call_count = len(re.findall(r"\bCheck\s*\(", source))
    names = re.findall(r'\bCheck\s*\(\s*"([^"]+)"\s*,', source)
    if call_count != len(names):
        raise ValueError("test registration inventory contains a nonliteral Check name")
    return names


def registered_checks() -> list[str]:
    return parse_registered_checks(TEST_SOURCE.read_text(encoding="utf-8"))


def registration_problems(names: list[str]) -> list[str]:
    problems = []
    if len(names) != len(set(names)):
        problems.append("duplicate Check registration")
    missing = EXPECTED_REGISTERED - set(names)
    extra = set(names) - EXPECTED_REGISTERED
    if missing:
        problems.append(f"missing registrations: {sorted(missing)}")
    if extra:
        problems.append(f"unexpected registrations: {sorted(extra)}")
    return problems


def inventory_rejects_nonliteral() -> bool:
    try:
        parse_registered_checks('Check(name, action);')
    except ValueError:
        return True
    return False


def inventory_rejects_unscoped() -> bool:
    names = sorted(EXPECTED_REGISTERED) + ["UnscopedNewCase"]
    return bool(registration_problems(names))


def timeout_cleanup_probe() -> tuple[bool, float, str]:
    """Exercise timeout cleanup with real Windows pipes and an injected taskkill failure."""
    if os.name != "nt":
        return True, 0.0, "not assessed off Windows"

    class FailedTaskkill:
        args = ["taskkill.exe", "/PID", "injected", "/T", "/F"]
        pid = 0
        returncode = 5
        stdout = None
        stderr = None

        def communicate(self, timeout: float | None = None) -> tuple[bytes, bytes]:
            return b"", b"injected taskkill failure"

        def kill(self) -> None:
            self.returncode = -9

    process_started = time.monotonic()
    work_deadline = process_started + 1.0
    total_deadline = process_started + 3.0
    taskkill_injected = threading.Event()
    child_pid_path: Path | None = None
    result: list[tuple[int, bytes, bytes, float]] = []
    target_processes: list[subprocess.Popen] = []
    worker: threading.Thread | None = None
    descendant_pid: int | None = None

    try:
        with tempfile.TemporaryDirectory(prefix="cfd-store-pipe-cleanup-") as temp_dir:
            child_pid_path = Path(temp_dir) / "descendant.pid"
            child_code = (
                "import subprocess,sys,time; "
                "child=subprocess.Popen([sys.executable,'-c','import time; time.sleep(20)']); "
                "open(sys.argv[1],'w',encoding='ascii').write(str(child.pid)); "
                "time.sleep(30)"
            )
            command = [sys.executable, "-c", child_code, str(child_pid_path)]
            real_popen = subprocess.Popen

            def inject_taskkill(argv: list[str], *args, **kwargs):
                if argv and Path(str(argv[0])).name.casefold() == "taskkill.exe":
                    taskkill_injected.set()
                    return FailedTaskkill()
                target = real_popen(argv, *args, **kwargs)
                target_processes.append(target)
                return target

            def invoke() -> None:
                result.append(run_capture(
                    command, env=dict(os.environ), deadline=work_deadline,
                    total_deadline=total_deadline,
                ))

            with patch.object(subprocess, "Popen", side_effect=inject_taskkill):
                worker = threading.Thread(target=invoke, name="r171-real-pipe-probe", daemon=True)
                worker.start()
                pid_wait_deadline = min(work_deadline - 0.1, time.monotonic() + 0.75)
                while descendant_pid is None and time.monotonic() < pid_wait_deadline:
                    try:
                        child_pid_text = child_pid_path.read_text(encoding="ascii").strip()
                        if child_pid_text:
                            descendant_pid = int(child_pid_text)
                    except (OSError, ValueError):
                        pass
                    if descendant_pid is not None:
                        break
                    worker.join(timeout=min(0.01, max(0.0, pid_wait_deadline - time.monotonic())))
                while time.monotonic() < work_deadline and worker.is_alive():
                    worker.join(timeout=min(0.02, max(0.0, work_deadline - time.monotonic())))
                worker.join(timeout=2.0)
                cleanup_seconds = max(0.0, time.monotonic() - work_deadline)
                completed_within_bound = not worker.is_alive() and cleanup_seconds <= 2.0

            # The failed taskkill leaves the fixture descendant alive; reap that test-owned process explicitly.
            if descendant_pid is not None:
                try:
                    os.kill(descendant_pid, 15)
                except OSError:
                    pass
            if worker is not None and worker.is_alive():
                worker.join(timeout=2.0)
            for target in target_processes:
                if target.poll() is None:
                    target.kill()
                    try:
                        target.communicate(timeout=0.25)
                    except subprocess.TimeoutExpired:
                        pass
            close_workers = [
                item for item in threading.enumerate()
                if item.name.startswith("pipe-close-")
            ]
            for close_worker in close_workers:
                close_worker.join(timeout=max(0.0, total_deadline - time.monotonic()))
            close_workers_finished = all(not item.is_alive() for item in close_workers)

            code, _, stderr, _ = result[0] if result else (None, b"", b"", 0.0)
            outcome = (
                descendant_pid is not None
                and taskkill_injected.is_set()
                and completed_within_bound
                and code == CLEANUP_FAILED_EXIT
                and b"FAIL TREE_CLEANUP_FAILED" in stderr
                and b"pipe close remains pending off the ceiling thread" in stderr
                and b"descendant termination not verified" in stderr
                and close_workers_finished
            )
            residual = next(
                (line for line in decode(stderr).splitlines() if "residual process tree state:" in line),
                "not recorded",
            )
            detail = (
                f"clock_origin=before-target-Popen; taskkill=exit-5; expired_work_deadline=true; "
                f"real_pipe_child={descendant_pid is not None}; "
                f"worker_completed={bool(worker and not worker.is_alive())}; "
                f"pipe_close_workers_finished={close_workers_finished}; "
                f"cleanup_seconds={cleanup_seconds:.6f}; exit={code}; residual={residual}"
            )
            return outcome, cleanup_seconds, detail
    except Exception as error:
        if descendant_pid is not None:
            try:
                os.kill(descendant_pid, 15)
            except OSError:
                pass
        for target in target_processes:
            if target.poll() is None:
                target.kill()
        return False, max(0.0, time.monotonic() - work_deadline), f"real-pipe probe error: {error}"


def selector_matches(names: set[str]) -> set[str]:
    return {name for name in names if name.startswith(SELECTOR_PREFIXES)}


def choose_affinity_mask(allowed: int, limit: int) -> int:
    selected = 0
    for bit in range(allowed.bit_length()):
        candidate = 1 << bit
        if allowed & candidate:
            selected |= candidate
            if bin(selected).count("1") == limit:
                break
    return selected


def bounded_child_timeout(total_deadline: float, now: float) -> float:
    return max(0.0, min(MAX_SECONDS - KILL_RESERVE_SECONDS, total_deadline - now - KILL_RESERVE_SECONDS))


def bounded_kill_cleanup_seconds() -> float:
    return CLEANUP_SECONDS


def exit_for_result(is_windows: bool, passed: bool) -> int:
    if not is_windows:
        return 4
    return 0 if passed else 1


def cli_mode(args: list[str]) -> str | None:
    if not args:
        return "run"
    if args == ["--self-test"]:
        return "self-test"
    return None


def exercise_off_windows_contract() -> bool:
    captured = io.StringIO()
    with patch.object(os, "name", "posix"), redirect_stdout(captured):
        result = main([])
    return (
        result == 4
        and "NOT ASSESSED" in captured.getvalue()
        and len(re.findall(r"(?m)^TOTAL_WALL_SECONDS=\d+\.\d{6}$", captured.getvalue())) == 1
    )


def exercise_argument_rejection() -> bool:
    captured = io.StringIO()
    total = io.StringIO()
    with redirect_stderr(captured), redirect_stdout(total):
        result = main(["--unexpected"])
    return (
        result == 1
        and "FAIL usage:" in captured.getvalue()
        and len(re.findall(r"(?m)^TOTAL_WALL_SECONDS=\d+\.\d{6}$", total.getvalue())) == 1
    )


def finalize_invocation(exit_code: int, elapsed: float) -> int:
    print(f"TOTAL_WALL_SECONDS={elapsed:.6f}", flush=True)
    if elapsed > MAX_SECONDS:
        print("FAIL hard 60-second verifier ceiling exceeded", file=sys.stderr)
        return 1
    return exit_code


def timed_entry(argv: list[str] | None, dispatch, started: float, clock) -> int:
    try:
        exit_code = dispatch(argv)
    except SystemExit as error:
        if error.code is None:
            exit_code = 0
        elif isinstance(error.code, int):
            exit_code = error.code
        else:
            print(f"FAIL verifier exited with: {redact(str(error.code))}", file=sys.stderr)
            exit_code = 1
    except KeyboardInterrupt:
        print("FAIL verifier interrupted", file=sys.stderr)
        exit_code = 130
    except Exception as error:
        print(f"FAIL verifier error: {redact(str(error))}", file=sys.stderr)
        exit_code = 1
    return finalize_invocation(exit_code, clock() - started)


def exercise_total_wall_contract() -> bool:
    for intended_exit in (0, 1, 4):
        output = io.StringIO()
        errors = io.StringIO()
        with redirect_stdout(output), redirect_stderr(errors):
            observed = timed_entry(
                ["early-path"], lambda _: intended_exit, 10.0, lambda: 10.25,
            )
        if observed != intended_exit or output.getvalue().count("TOTAL_WALL_SECONDS=0.250000") != 1:
            return False
        if errors.getvalue():
            return False

    output = io.StringIO()
    errors = io.StringIO()
    with redirect_stdout(output), redirect_stderr(errors):
        over_budget = timed_entry(["early-path"], lambda _: 4, 10.0, lambda: 70.000001)
    if over_budget != 1 or output.getvalue().count("TOTAL_WALL_SECONDS=60.000001") != 1:
        return False
    if "hard 60-second verifier ceiling exceeded" not in errors.getvalue():
        return False

    output = io.StringIO()
    errors = io.StringIO()

    def raises(_: list[str] | None) -> int:
        raise RuntimeError("injected dispatch error")

    with redirect_stdout(output), redirect_stderr(errors):
        error_exit = timed_entry(["error-path"], raises, 1.0, lambda: 1.5)
    if not (
        error_exit == 1
        and output.getvalue().count("TOTAL_WALL_SECONDS=0.500000") == 1
        and "injected dispatch error" in errors.getvalue()
    ):
        return False

    output = io.StringIO()
    errors = io.StringIO()

    def exits(_: list[str] | None) -> int:
        raise SystemExit(4)

    with redirect_stdout(output), redirect_stderr(errors):
        exit_exception = timed_entry(["exit-path"], exits, 2.0, lambda: 2.5)
    return (
        exit_exception == 4
        and output.getvalue().count("TOTAL_WALL_SECONDS=0.500000") == 1
        and not errors.getvalue()
    )


def validate_output(stdout: str, exit_code: int, classification: str, classifier_exit: int) -> list[str]:
    """Require all selected checks and exactly the Ruling 145 classified failure."""
    stdout = stdout.replace("\r\n", "\n").replace("\r", "\n")
    classification = classification.replace("\r\n", "\n").replace("\r", "\n")
    results = re.findall(r"(?m)^RESULT failures=(\d+)$", stdout)
    passes = re.findall(r"(?m)^PASS (\S+)$", stdout)
    fail_lines = re.findall(r"(?m)^FAIL .*$", stdout)
    fail_names = re.findall(r"(?m)^FAIL (\S+)", stdout)
    subset_lines = re.findall(r"(?m)^SUBSET CFD_TEST_ONLY=(.*?) ran=(\d+) skipped=(\d+)$", stdout)
    problems = []
    if exit_code != 1:
        problems.append(f"runner exit was {exit_code}, expected 1 for the classified historical failure")
    if results != ["1"]:
        problems.append("runner did not return exactly one one-failure RESULT")
    if len(fail_lines) != 1 or set(fail_names) != EXPECTED_FAILURES:
        problems.append(f"runner FAIL set mismatch expected={sorted(EXPECTED_FAILURES)} actual={sorted(fail_names)}")
    if len(passes) != len(set(passes)):
        problems.append("runner emitted duplicate PASS names")
    actual = set(passes)
    if actual != EXPECTED_PASSES:
        problems.append(f"PASS set mismatch missing={sorted(EXPECTED_PASSES - actual)} extra={sorted(actual - EXPECTED_PASSES)}")
    if len(subset_lines) != 1:
        problems.append("runner did not emit exactly one subset receipt")
    else:
        selector, ran, skipped = subset_lines[0]
        if selector != SELECTOR or int(ran) != len(EXPECTED_REGISTERED) or int(skipped) < 0:
            problems.append("runner subset receipt did not match the pinned selector and expected count")
    expected_classification = (
        f"EXPECTED {HISTORICAL_EXPECTED_FAILURE} (Ruling 145, historical-probe, until W-2 B2 lands)",
        "EXPECTED-FAIL 1 (manifest)",
        "UNEXPECTED 0",
    )
    actual_classification = tuple(line for line in classification.splitlines() if line)
    if classifier_exit != 0 or actual_classification != expected_classification:
        problems.append("Windows expected-failure classifier did not report exactly the Ruling 145 historical failure")
    return problems


def expected_output() -> str:
    lines = [f"PASS {name}" for name in sorted(EXPECTED_PASSES)]
    lines.append(f"FAIL {HISTORICAL_EXPECTED_FAILURE} NativeFailure: Win32=32")
    lines.append(f"SUBSET CFD_TEST_ONLY={SELECTOR} ran={len(EXPECTED_REGISTERED)} skipped=0")
    lines.append("RESULT failures=1")
    return "\n".join(lines) + "\n"


def expected_classification() -> str:
    return "\n".join((
        f"EXPECTED {HISTORICAL_EXPECTED_FAILURE} (Ruling 145, historical-probe, until W-2 B2 lands)",
        "EXPECTED-FAIL 1 (manifest)",
        "UNEXPECTED 0",
    )) + "\n"


def self_test() -> int:
    pipe_cleanup_ok, pipe_cleanup_seconds, pipe_cleanup_detail = timeout_cleanup_probe()
    valid = expected_output()
    missing = "\n".join(line for line in valid.splitlines() if line != "PASS WindowsNative_Rename_FreshWin32VersusNtFixtures") + "\n"
    legacy_missing = "\n".join(line for line in valid.splitlines() if not line.startswith(f"FAIL {HISTORICAL_EXPECTED_FAILURE}")) + "\n"
    extra = valid.replace("RESULT failures=1", "PASS Unexpected_Check\nRESULT failures=1")
    wrong_result = valid.replace("RESULT failures=1", "RESULT failures=0")
    no_result = "\n".join(line for line in valid.splitlines() if not line.startswith("RESULT ")) + "\n"
    duplicate = valid.replace("RESULT failures=1", "RESULT failures=1\nRESULT failures=1")
    wrong_selector = valid.replace(f"CFD_TEST_ONLY={SELECTOR}", "CFD_TEST_ONLY=WindowsNative_")
    wrong_count = valid.replace(f"ran={len(EXPECTED_REGISTERED)}", f"ran={len(EXPECTED_REGISTERED) - 1}")
    wrong_failure = valid.replace(
        f"FAIL {HISTORICAL_EXPECTED_FAILURE}", "FAIL WindowsNative_UnexpectedFailure", 1,
    )
    unexpected_classification = expected_classification().replace("UNEXPECTED 0", "UNEXPECTED 1")
    stale_classification = expected_classification().replace(
        f"EXPECTED {HISTORICAL_EXPECTED_FAILURE}",
        f"UNEXPECTED-PASS {HISTORICAL_EXPECTED_FAILURE}",
    ).replace("EXPECTED-FAIL 1 (manifest)", "EXPECTED-FAIL 0 (manifest)")
    checks = [
        ("selector includes Ruling 152 historical probe", selector_matches(EXPECTED_REGISTERED) == EXPECTED_REGISTERED),
        ("valid classified Ruling 145 result accepted", not validate_output(valid, 1, expected_classification(), 0)),
        ("missing required pass rejected", bool(validate_output(missing, 1, expected_classification(), 0))),
        ("missing historical failure rejected", bool(validate_output(legacy_missing, 1, expected_classification(), 0))),
        ("extra pass rejected", bool(validate_output(extra, 1, expected_classification(), 0))),
        ("wrong RESULT rejected", bool(validate_output(wrong_result, 1, expected_classification(), 0))),
        ("missing RESULT rejected", bool(validate_output(no_result, 1, expected_classification(), 0))),
        ("duplicate RESULT rejected", bool(validate_output(duplicate, 1, expected_classification(), 0))),
        ("wrong selector rejected", bool(validate_output(wrong_selector, 1, expected_classification(), 0))),
        ("wrong selected count rejected", bool(validate_output(wrong_count, 1, expected_classification(), 0))),
        ("wrong failure name rejected", bool(validate_output(wrong_failure, 1, expected_classification(), 0))),
        ("unexpected failure rejected", bool(validate_output(valid, 1, unexpected_classification, 0))),
        ("stale manifest pass rejected", bool(validate_output(valid, 0, stale_classification, 1))),
        ("classifier failure rejected", bool(validate_output(valid, 1, expected_classification(), 1))),
        ("wrong subject exit rejected", bool(validate_output(valid, 0, expected_classification(), 0))),
        ("affinity mask limited to six", choose_affinity_mask(0xFFFF, MAX_CPUS) == 0x3F),
        ("empty affinity refused", choose_affinity_mask(0, MAX_CPUS) == 0),
        ("child timeout leaves kill reserve", bounded_child_timeout(60.0, 0.0) == 55.0
         and bounded_child_timeout(60.0, 58.0) == 0.0
         and bounded_kill_cleanup_seconds() < KILL_RESERVE_SECONDS),
        ("nonliteral inventory registration rejected", inventory_rejects_nonliteral()),
        ("duplicate inventory registration rejected", bool(registration_problems(sorted(EXPECTED_REGISTERED) + [sorted(EXPECTED_REGISTERED)[0]]))),
        ("missing inventory registration rejected", bool(registration_problems(sorted(EXPECTED_REGISTERED)[1:]))),
        ("unscoped inventory addition rejected", inventory_rejects_unscoped()),
        ("real Windows pipe timeout cleanup bounded after taskkill failure", pipe_cleanup_ok),
        ("all exit paths report total wall and enforce the hard ceiling", exercise_total_wall_contract()),
        ("off-Windows execution is NOT ASSESSED", exercise_off_windows_contract()),
        ("Windows subject failure exits 1", exit_for_result(True, False) == 1),
        ("no-argument mode is default", cli_mode([]) == "run"),
        ("self-test is explicit", cli_mode(["--self-test"]) == "self-test"),
        ("other arguments are rejected", cli_mode(["--verbose"]) is None and exercise_argument_rejection()),
    ]
    failures = [label for label, passed in checks if not passed]
    print(f"SELFTEST PIPE_CLEANUP_SECONDS={pipe_cleanup_seconds:.6f}")
    print(f"SELFTEST PIPE_CLEANUP_EVIDENCE={pipe_cleanup_detail}")
    for label, passed in checks:
        print(f"SELFTEST {'PASS' if passed else 'FAIL'} {label}")
    if failures:
        return 1
    try:
        registered = registered_checks()
    except ValueError as error:
        print(f"SELFTEST FAIL registered Windows test inventory is not literal: {error}")
        return 1
    inventory_errors = registration_problems(registered)
    if inventory_errors:
        print(f"SELFTEST FAIL registered test inventory drift: {'; '.join(inventory_errors)}")
        return 1
    print(f"SELFTEST PASS registered Windows test inventory exact ({len(EXPECTED_REGISTERED)} checks)")
    return 0


def _dispatch(argv: list[str] | None = None) -> int:
    started = PROCESS_STARTED
    mode = cli_mode(sys.argv[1:] if argv is None else argv)
    if mode == "self-test":
        return self_test()
    if mode is None:
        print("FAIL usage: verify-windows-store.py [--self-test]", file=sys.stderr)
        return 1
    if os.name != "nt":
        print("NOT ASSESSED: this verifier requires Windows", flush=True)
        return exit_for_result(False, False)

    total_deadline = started + MAX_SECONDS
    work_deadline = total_deadline - KILL_RESERVE_SECONDS
    start_utc = utc_now()
    try:
        original_affinity, affinity = set_six_cpu_affinity()
    except Exception as error:
        print(f"FAIL CPU affinity could not be bounded: {redact(str(error))}", file=sys.stderr)
        return 1

    env = dict(os.environ)
    env["DOTNET_PROCESSOR_COUNT"] = str(min(MAX_CPUS, affinity.bit_count()))
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1"
    try:
        expected_sdk = json.loads(GLOBAL_JSON.read_text(encoding="utf-8"))["sdk"]["version"]
        git_code, git_out, git_err, _ = run_capture(
            ["git", "rev-parse", "HEAD"], env=env,
            deadline=min(time.monotonic() + 3.0, work_deadline), total_deadline=total_deadline,
        )
        head = git_out.decode("ascii", errors="replace").strip() if git_code == 0 else "unavailable"
        print(f"START_UTC={start_utc}")
        print(f"HEAD={head}")
        print(f"SCRIPT_SHA256={hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}")
        print(f"OS=Windows release={platform.release()} version={platform.version()} machine={platform.machine()}")
        print(f"AFFINITY_MASK=0x{affinity:x} CPUs={affinity.bit_count()} prior_mask=0x{original_affinity:x}")
        print(f"SDK_REQUIRED={expected_sdk}")
        if git_code != 0:
            child_output("GIT", git_out, git_err)
            print("FAIL could not read the tested HEAD", file=sys.stderr)
            return 1

        selected_dotnet: Path | None = None
        selected_label = ""
        for label, candidate in sdk_candidates(env):
            if not candidate.is_file():
                continue
            code, out, err, _ = run_capture(
                [str(candidate), "--version"], env=env,
                deadline=min(time.monotonic() + 4.0, work_deadline), total_deadline=total_deadline,
            )
            version = out.decode("utf-8", errors="replace").strip()
            print(f"SDK_CANDIDATE={label} exit={code} version={redact(version) or 'not recorded'}")
            if code == 0 and version == expected_sdk:
                selected_dotnet = candidate
                selected_label = label
                break
        if selected_dotnet is None:
            print("FAIL pinned .NET SDK was not resolved", file=sys.stderr)
            return 1
        env["DOTNET_ROOT"] = str(selected_dotnet.parent)
        env["PATH"] = str(selected_dotnet.parent) + os.pathsep + env.get("PATH", "")
        env["CFD_TEST_ONLY"] = SELECTOR
        print(f"SDK_SELECTED={selected_label} version={expected_sdk}")

        names = registered_checks()
        inventory_errors = registration_problems(names)
        if inventory_errors:
            print(f"FAIL test registration drift: {'; '.join(inventory_errors)}", file=sys.stderr)
            return 1
        if selector_matches(set(names)) != EXPECTED_REGISTERED:
            print("FAIL selector does not select all 13 registered qualification checks", file=sys.stderr)
            return 1

        print(f"TEST_START_UTC={utc_now()}")
        remaining = bounded_child_timeout(total_deadline, time.monotonic())
        if remaining <= 0:
            print("FAIL total verifier budget exhausted before test launch", file=sys.stderr)
            return 1
        command = [str(selected_dotnet), "run", "--configuration", "Release", "--project", str(PROJECT), "-m:6"]
        print("COMMAND=dotnet run --configuration Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj -m:6")
        code, out, err, run_seconds = run_capture(
            command, env=env, deadline=min(work_deadline, time.monotonic() + remaining), total_deadline=total_deadline,
        )
        child_output("TEST", out, err)
        stdout_text = decode(out)
        with tempfile.TemporaryDirectory(prefix="cfd-store-verifier-") as temp_dir:
            harness_log = Path(temp_dir) / "harness.stdout.txt"
            harness_log.write_text(stdout_text, encoding="utf-8", newline="\n")
            if time.monotonic() >= work_deadline:
                classifier_code, classifier_out, classifier_err, classifier_seconds = TIMEOUT_EXIT, b"", b"", 0.0
            else:
                classifier_code, classifier_out, classifier_err, classifier_seconds = run_capture(
                    [sys.executable, str(ROOT / "tools/check-expected-failures.py"), "--log", str(harness_log),
                     "--status", str(code), "--host", "windows"],
                    env=env, deadline=work_deadline, total_deadline=total_deadline,
                )
        child_output("CLASSIFIER", classifier_out, classifier_err)
        classification_text = decode(classifier_out)
        print(f"CLASSIFIER_COMMAND=tools/check-expected-failures.py --status {code} --host windows")
        problems = validate_output(stdout_text, code, classification_text, classifier_code)
        if code == TIMEOUT_EXIT:
            problems.append(f"test command hit timeout exit {TIMEOUT_EXIT}")
        if problems:
            print("FAIL " + "; ".join(problems), file=sys.stderr)
            overall_code = 1
        else:
            print(f"PASS Windows store qualification checks={len(EXPECTED_REGISTERED)} expected_failures={len(EXPECTED_FAILURES)}")
            overall_code = 0
        print(f"TEST_EXIT={code}")
        print(f"TEST_DURATION_SECONDS={run_seconds:.6f}")
        print(f"CLASSIFIER_EXIT={classifier_code}")
        print(f"CLASSIFIER_DURATION_SECONDS={classifier_seconds:.6f}")
        return overall_code
    except Exception as error:
        print(f"FAIL verifier error: {redact(str(error))}", file=sys.stderr)
        return 1
    finally:
        print(f"END_UTC={utc_now()}")
        duration = time.monotonic() - started
        print(f"TARGET_SECONDS={TARGET_SECONDS:.0f}")
        print(f"TARGET_MET={'true' if duration < TARGET_SECONDS else 'false'}")


def main(argv: list[str] | None = None) -> int:
    return timed_entry(argv, _dispatch, PROCESS_STARTED, time.monotonic)


if __name__ == "__main__":
    raise SystemExit(main())
