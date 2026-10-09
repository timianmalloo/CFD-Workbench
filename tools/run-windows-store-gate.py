#!/usr/bin/env python3
"""Run tools/verify-windows-store.py under the join's contract (Rulings 171 (4), 175 (4)).

The verifier exits 0 = pass, 1 = fail, 4 = NOT ASSESSED off Windows. A runner that treats every
non-zero exit as FAIL (run-verify-gates.py) would fail every Mac join, so the join skips the
verifier in run-verify-gates and runs it here instead:

  exit 0 -> PASS · exit 4 -> prints NOT ASSESSED (verify-windows-store), exits 0 · anything else,
  or no end within 60 s -> FAIL (exit 1). The verifier's TOTAL_WALL_SECONDS line is echoed.

A Windows PASS here is evidence only; it enters readiness through a reviewed receipt (Ruling 175 (2)).

  python3 tools/run-windows-store-gate.py
  python3 tools/run-windows-store-gate.py --self-test
"""
from __future__ import annotations

import contextlib
import io
import os
import signal
import subprocess
import sys
import time
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
VERIFIER = ROOT / "tools" / "verify-windows-store.py"
TIMEOUT_SECONDS = 60
CLEANUP_SECONDS = 2  # the most the timeout path may wait after the kill: the ceiling is held, not asserted
NOT_ASSESSED_EXIT = 4


def run_gate(command: list[str], timeout: float = TIMEOUT_SECONDS) -> int:
    """Run `command`; return 0 for pass or NOT ASSESSED, 1 for fail or timeout."""
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               encoding="utf-8", errors="replace", start_new_session=os.name != "nt")
    try:
        output, _ = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        try:
            if os.name == "nt":
                process.kill()
            else:
                os.killpg(process.pid, signal.SIGKILL)
        except OSError:
            try:
                process.kill()  # the group kill failed or the group is gone: kill the child itself, never raise
            except OSError:
                pass
        # Bounded drain (CLEANUP-BLOCKS-CEILING): a grandchild that left the group may hold the pipe open, so the wait is
        # CLEANUP_SECONDS at most; the pipe is then abandoned, not closed here.
        try:
            process.communicate(timeout=CLEANUP_SECONDS)
        except subprocess.TimeoutExpired:
            pass
        print("verify-windows-store: FAIL - no result within {0:.0f} s; process group killed".format(timeout))
        return 1
    for line in output.splitlines():
        if line.startswith("TOTAL_WALL_SECONDS="):
            print(line)
    code = process.returncode
    if code == 0:
        print("verify-windows-store: PASS")
        return 0
    if code == NOT_ASSESSED_EXIT:
        print("NOT ASSESSED (verify-windows-store)")
        return 0
    print("verify-windows-store: FAIL - exit {0}".format(code))
    print(output[-2000:])
    return 1


def self_test() -> int:
    """Each exit of a stub child maps as the contract says, the wall line is echoed, and a hung child is killed."""
    problems = []
    py = sys.executable
    wall = "print('TOTAL_WALL_SECONDS=0.250000')"
    cases = [(0, 0, "PASS"), (4, 0, "NOT ASSESSED (verify-windows-store)"), (1, 1, "FAIL"), (2, 1, "FAIL"), (9, 1, "FAIL")]
    for exit_code, expected, text in cases:
        buffer = io.StringIO()
        with contextlib.redirect_stdout(buffer):
            observed = run_gate([py, "-c", "{0}; raise SystemExit({1})".format(wall, exit_code)])
        printed = buffer.getvalue()
        if observed != expected or text not in printed or "TOTAL_WALL_SECONDS=0.250000" not in printed.splitlines():
            problems.append("stub exit {0}: got {1} and {2!r}".format(exit_code, observed, printed))
    buffer = io.StringIO()
    began = time.monotonic()
    with contextlib.redirect_stdout(buffer):
        observed = run_gate([py, "-c", "import time; time.sleep(30)"], timeout=1)
    if observed != 1 or "FAIL - no result within" not in buffer.getvalue() or time.monotonic() - began > 15:
        problems.append("a hung child was not killed at its limit and reported FAIL")
    # CLEANUP-BLOCKS-CEILING (Ruling 171 (1)): real children, not fake streams. The child ignores SIGTERM and starts a grandchild
    # in its own session that holds the output pipe for 8 s, so the pipe cannot reach end of file. Cleanup must stay bounded
    # for an already-expired deadline (timeout 0) and when the kill itself fails; the measured cleanup time is printed.
    holder = ("import signal, subprocess, sys, time; signal.signal(signal.SIGTERM, signal.SIG_IGN); "
              "subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(8)'], start_new_session=True); time.sleep(30)")
    real_killpg = os.killpg

    def failing_killpg(*_):
        raise PermissionError("injected kill failure")

    for label, deadline, kill in (("expired deadline", 0, real_killpg), ("held pipe", 1, real_killpg),
                                   ("failed kill", 1, failing_killpg)):
        buffer = io.StringIO()
        began = time.monotonic()
        os.killpg = kill
        try:
            with contextlib.redirect_stdout(buffer):
                observed = run_gate([sys.executable, "-c", holder], timeout=deadline)
        except Exception as error:  # noqa: BLE001 - the unfixed path raises here; the self-test reports it
            observed = "raised {0!r}".format(error)
        finally:
            os.killpg = real_killpg
        cleanup = time.monotonic() - began - deadline
        print("cleanup after {0}: {1:.2f} s (bound {2:.0f} s)".format(label, cleanup, CLEANUP_SECONDS))
        if observed != 1 or "FAIL - no result within" not in buffer.getvalue() or cleanup > CLEANUP_SECONDS + 1:
            problems.append("{0}: gave {1!r}, cleanup {2:.2f} s".format(label, observed, cleanup))
    for problem in problems:
        print("self-test FAIL: " + problem)
    print("self-test OK" if not problems else "self-test FAILED")
    return 1 if problems else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if argv:
        print(__doc__)
        return 2
    return run_gate([sys.executable, str(VERIFIER)])


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
