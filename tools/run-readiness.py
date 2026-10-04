#!/usr/bin/env python3
"""Run the readiness ring from docs/coordination/join.json and record a receipt for HEAD.

The join's fast ring (checks + gates) runs on every join. The readiness ring holds the slow,
GUI-launching and publish-heavy gates and the spike recounts, and runs before a merge to main
or a milestone close (docs/reviews/test-ci-waste.md). The receipt makes that ring checkable:

  python3 tools/run-readiness.py            run the ring on a clean tree, write the receipt
  python3 tools/run-readiness.py --check    exit 0 only if a green, in-budget receipt names this HEAD
  python3 tools/run-readiness.py --self-test

A `readiness` entry is a command (a list of strings) or a group (a list of commands). A group's
commands run concurrently, each logging to .tmp-tests/readiness/<step>.log; the ring waits for the
whole group before the next entry. Only steps that share no scratch, build output or wall-time budget
belong in one group (docs/plans/test-cost.md L4). Every step has a time limit (STEP_TIMEOUT); past it
its process group is killed and the step is red.

Exit 0 green · 1 a command failed, or --check found no green in-budget receipt for HEAD · 2 usage ·
3 green, but the ring took longer than its budget (READINESS-BUDGET; CFD_READINESS_BUDGET_SECONDS).
"""
from __future__ import annotations

import json
import os
import signal
import subprocess
import sys
import tempfile
import time
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
# The ring's wall budget. Measured 2026-10-04 after L1-L4: see docs/plans/test-cost.md §8.
BUDGET_SECONDS = 240
# One step's limit: the gates' own child limits are 180 s (core) and 600/900 s (adapters); a hung step must not
# hang the ring past them.
STEP_TIMEOUT = 1200


def git(root: Path, *arguments: str) -> str:
    return subprocess.run(["git", *arguments], cwd=root, check=True, capture_output=True,
                          text=True, encoding="utf-8", errors="replace").stdout.strip()


def load() -> str:
    """The 1-minute load average, or "not recorded" (Windows); never a guess."""
    try:
        return "{0:.2f}".format(os.getloadavg()[0])
    except (AttributeError, OSError):
        return "not recorded"


def start(command: list[str], root: Path, log: Path | None) -> tuple[subprocess.Popen, object]:
    argv = [sys.executable if c in ("python3", "python") else c for c in command]
    output = log.open("w", encoding="utf-8") if log is not None else None
    process = subprocess.Popen(argv, cwd=root, stdout=output, stderr=subprocess.STDOUT if output else None,
                               start_new_session=os.name != "nt")
    return process, output


def finish(process: subprocess.Popen, output: object, deadline: float) -> tuple[int, bool]:
    """Wait to the deadline; past it kill the step's whole process group. Returns (exit, timed out)."""
    try:
        return process.wait(timeout=max(0.0, deadline - time.monotonic())), False
    except subprocess.TimeoutExpired:
        if os.name == "nt":
            process.kill()
        else:
            os.killpg(process.pid, signal.SIGKILL)
        return process.wait(), True
    finally:
        if output is not None:
            output.close()


def run_entry(entry: list, root: Path, logs: Path, index: int, timeout: float) -> list[dict]:
    """Run one ring entry: a command in the foreground, or a group concurrently with logs."""
    group = bool(entry) and isinstance(entry[0], list)
    commands = entry if group else [entry]
    started, results, running = time.monotonic(), [], []
    for number, command in enumerate(commands):
        log = logs / "{0:02d}-{1}.log".format(index, number) if group else None
        running.append((command, log, *start(command, root, log)))
    for command, log, process, output in running:
        code, timed_out = finish(process, output, started + timeout)
        result = {"command": command, "exit": code, "seconds": round(time.monotonic() - started, 1)}
        if group:
            result["concurrent"] = True
            result["log"] = str(log)
        if timed_out:
            result["timedOut"] = timeout
        results.append(result)
        print("run-readiness: {0} {1:6.1f} s  {2}{3}{4}".format(
            "ok  " if code == 0 else "FAIL", result["seconds"], "|| " if group else "", " ".join(command),
            "  (TIMEOUT after {0:.0f} s; process group killed)".format(timeout) if timed_out else ""), flush=True)
        if code != 0 and log is not None:
            print("run-readiness: last lines of " + str(log))
            print("\n".join(log.read_text(encoding="utf-8", errors="replace").splitlines()[-40:]), flush=True)
    return results


def run_ring(root: Path, join: Path, receipt: Path, budget: float = BUDGET_SECONDS,
             timeout: float = STEP_TIMEOUT) -> int:
    if git(root, "status", "--porcelain"):
        print("run-readiness: the tree is not clean; commit first so the receipt names what ran")
        return 1
    ring = json.loads(join.read_text(encoding="utf-8")).get("readiness") or []
    if not ring:
        print("run-readiness: {0} has no `readiness` commands".format(join))
        return 2
    logs = receipt.parent / "readiness"
    logs.mkdir(parents=True, exist_ok=True)
    head, results, load_start, started = git(root, "rev-parse", "HEAD"), [], load(), time.monotonic()
    for index, entry in enumerate(ring):
        results += run_entry(entry, root, logs, index, timeout)
    total, load_end = round(time.monotonic() - started, 1), load()
    green = all(item["exit"] == 0 for item in results)
    over = green and total > budget
    receipt.write_text(json.dumps({"head": head, "green": green, "seconds": total, "budgetSeconds": budget,
                                   "overBudget": over, "load": [load_start, load_end], "results": results},
                                  indent=2) + "\n", encoding="utf-8", newline="\n")
    print("run-readiness: total {0:.1f} s (budget {1:.0f} s) load {2} -> {3}".format(total, budget, load_start, load_end))
    print("run-readiness: {0} for {1} (receipt {2})".format("GREEN" if green else "RED", head[:9], receipt))
    if over:
        print("READINESS-BUDGET: green, but {0:.1f} s is over the {1:.0f} s budget. Read the load on the line above and the "
              "step seconds before raising it (docs/plans/test-cost.md).".format(total, budget))
        return 3
    return 0 if green else 1


def check(root: Path, receipt: Path) -> int:
    head = git(root, "rev-parse", "HEAD")
    try:
        data = json.loads(receipt.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        print("run-readiness: no readiness receipt; run python3 tools/run-readiness.py")
        return 1
    if data.get("head") != head or data.get("green") is not True:
        print("run-readiness: the receipt is {0} for {1}, not green for HEAD {2}".format(
            "green" if data.get("green") else "red", str(data.get("head"))[:9], head[:9]))
        return 1
    if data.get("overBudget"):
        print("run-readiness: the receipt for HEAD {0} is green but over budget ({1} s > {2} s); READINESS-BUDGET".format(
            head[:9], data.get("seconds"), data.get("budgetSeconds")))
        return 1
    print("run-readiness: green receipt for HEAD {0}".format(head[:9]))
    return 0


def self_test() -> int:
    """A red command or a red group member makes the ring red, a green ring passes --check, a new commit makes
    the old receipt stale, an over-budget green ring exits 3 and fails --check, and a hung step is killed."""
    problems = []
    with tempfile.TemporaryDirectory() as directory:
        repo = Path(directory)
        for command in (["init", "-q"], ["config", "user.email", "t@example.com"], ["config", "user.name", "T"]):
            git(repo, *command)
        join, receipt = repo / "join.json", repo / ".tmp" / "readiness.json"
        (repo / ".gitignore").write_text(".tmp/\n", encoding="utf-8", newline="\n")

        def ring(entries: list, message: str) -> None:
            join.write_text(json.dumps({"readiness": entries}), encoding="utf-8", newline="\n")
            git(repo, "add", "-A")
            git(repo, "commit", "-q", "--allow-empty", "-m", message)

        ok, red = ["python3", "-c", "print('ok')"], ["python3", "-c", "raise SystemExit(1)"]
        ring([red], "red")
        if run_ring(repo, join, receipt) != 1 or check(repo, receipt) != 1:
            problems.append("a red readiness command was not reported red")
        ring([[ok, red], ok], "red group")
        if run_ring(repo, join, receipt) != 1 or check(repo, receipt) != 1:
            problems.append("a red command inside a concurrent group was not reported red")
        ring([[ok, ok], ok], "green")
        if run_ring(repo, join, receipt) != 0 or check(repo, receipt) != 0:
            problems.append("a green ring did not pass --check")
        (repo / "later.txt").write_text("x\n", encoding="utf-8", newline="\n")
        git(repo, "add", "-A")
        git(repo, "commit", "-q", "-m", "later")
        if check(repo, receipt) != 1:
            problems.append("a receipt for an older HEAD passed --check")
        if run_ring(repo, join, receipt, budget=-1) != 3 or check(repo, receipt) != 1:
            problems.append("an over-budget green ring was not exit 3, or passed --check")
        ring([["python3", "-c", "import time; time.sleep(30)"]], "hung")
        began = time.monotonic()
        if run_ring(repo, join, receipt, timeout=1) != 1 or time.monotonic() - began > 20:
            problems.append("a hung step was not killed at its limit and reported red")
    for problem in problems:
        print("self-test FAIL: " + problem)
    print("self-test OK" if not problems else "self-test FAILED")
    return 1 if problems else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    receipt = ROOT / ".tmp-tests" / "readiness.json"
    if argv == ["--check"]:
        return check(ROOT, receipt)
    if argv:
        print(__doc__)
        return 2
    budget = float(os.environ.get("CFD_READINESS_BUDGET_SECONDS") or BUDGET_SECONDS)
    return run_ring(ROOT, ROOT / "docs" / "coordination" / "join.json", receipt, budget)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
