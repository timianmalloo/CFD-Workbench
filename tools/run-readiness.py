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
# The most the timeout path waits for a killed step to be reaped (CLEANUP-BLOCKS-CEILING): the ceiling is held, not asserted.
KILL_WAIT_SECONDS = 2
# Per-script rules, keyed by the script's file name (a readiness entry stays a plain command, which check-docs
# TEST-RING reads). timeout overrides STEP_TIMEOUT; notAssessed lists exits that mean "could not be assessed here",
# never a failure and left out of the ring result; evidence marks a pass recorded as evidence, not as a gate.
# tools/verify-windows-store.py (Rulings 171 (4), 175): exit 4 off Windows; a Windows PASS enters readiness only
# through a reviewed receipt from a rerun of the final script, so a pass here is evidence only.
ENTRY_RULES = {"verify-windows-store.py": {"timeout": 60, "notAssessed": (4,), "evidence": True}}


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
    env = None
    if argv and argv[0] == "dotnet":
        # The test harnesses' store checks refuse a symlinked temp root (macOS /tmp and /var are links), so the dotnet
        # steps get the same non-symlinked TMPDIR tools/run-tests.sh exports. The verifiers keep their own scratch
        # (one writes ~2.75 GB per run), so only dotnet steps are redirected. Found 2026-10-04: STO's readiness-tier
        # Store_HundredThousandStrips_RefusedDocSize read DOC-UNSUPPORTED-PERSISTENCE inside readiness only.
        scratch = root.resolve() / ".tmp-tests"
        scratch.mkdir(parents=True, exist_ok=True)
        env = {**os.environ, "TMPDIR": str(scratch) + "/", "TMP": str(scratch), "TEMP": str(scratch)}
    process = subprocess.Popen(argv, cwd=root, stdout=output, stderr=subprocess.STDOUT if output else None,
                               start_new_session=os.name != "nt", env=env)
    return process, output


def finish(running: list, deadlines: list[float]) -> list[tuple[int, bool, float]]:
    """Wait for every process to its own deadline, recording when each ends; past its deadline kill that
    step's whole process group. Returns (exit, timed out, end time) per process, in order."""
    ends: dict[int, tuple[int, bool, float]] = {}
    while len(ends) < len(running):
        for index, (_, _, process, output) in enumerate(running):
            if index in ends:
                continue
            timed_out = time.monotonic() >= deadlines[index] and process.poll() is None
            if timed_out:
                try:
                    if os.name == "nt":
                        process.kill()
                    else:
                        os.killpg(process.pid, signal.SIGKILL)
                except OSError:
                    try:
                        process.kill()  # the group kill failed or the group is gone: kill the step itself, never raise
                    except OSError:
                        pass
                try:
                    process.wait(timeout=KILL_WAIT_SECONDS)
                except subprocess.TimeoutExpired:
                    pass  # unkillable: report the step red now rather than hold the ring past its ceiling
            if process.poll() is not None or timed_out:
                ends[index] = (process.returncode if process.returncode is not None else -9, timed_out, time.monotonic())
                if output is not None:
                    output.close()
        time.sleep(0.2)
    return [ends[index] for index in range(len(running))]


def rule_for(command: list[str], rules: dict) -> dict:
    interpreters = {"python3", "python", "py"}
    executed = next((part for part in command[1:] if not part.startswith("-")), "") if Path(command[0]).stem in interpreters else command[0]
    return rules.get(Path(executed).name, {})


def status_of(code: int, timed_out: bool, rule: dict) -> str:
    if timed_out:
        return "fail"
    if code == 0:
        return "evidence" if rule.get("evidence") else "ok"
    return "not-assessed" if code in rule.get("notAssessed", ()) else "fail"


def run_entry(entry: list, root: Path, logs: Path, index: int, timeout: float, rules: dict = ENTRY_RULES) -> list[dict]:
    """Run one ring entry: a command in the foreground, or a group concurrently with logs."""
    group = bool(entry) and isinstance(entry[0], list)
    commands = entry if group else [entry]
    started, results, running = time.monotonic(), [], []
    rule_list = [rule_for(command, rules) for command in commands]
    limits = [rule.get("timeout", timeout) for rule in rule_list]
    for number, command in enumerate(commands):
        log = logs / "{0:02d}-{1}.log".format(index, number) if group else None
        running.append((command, log, *start(command, root, log)))
    ends = finish(running, [started + limit for limit in limits])
    for (command, log, _, _), (code, timed_out, ended), rule, limit in zip(running, ends, rule_list, limits):
        status = status_of(code, timed_out, rule)
        result = {"command": command, "exit": code, "status": status, "seconds": round(ended - started, 1)}
        if group:
            result["concurrent"] = True
            result["log"] = str(log)
        if timed_out:
            result["timedOut"] = limit
        results.append(result)
        print("run-readiness: {0} {1:6.1f} s  {2}{3}{4}".format(
            {"ok": "ok  ", "evidence": "EVID", "not-assessed": "N/A "}.get(status, "FAIL"), result["seconds"], "|| " if group else "", " ".join(command),
            "  (TIMEOUT after {0:.0f} s; process group killed)".format(limit) if timed_out
            else "  (NOT ASSESSED, excluded from the ring result)" if status == "not-assessed"
            else "  (evidence, not a gate: a Windows PASS counts only through a reviewed receipt)" if status == "evidence"
            else ""), flush=True)
        if status == "fail" and log is not None:
            print("run-readiness: last lines of " + str(log))
            print("\n".join(log.read_text(encoding="utf-8", errors="replace").splitlines()[-40:]), flush=True)
    return results


def run_ring(root: Path, join: Path, receipt: Path, budget: float = BUDGET_SECONDS,
             timeout: float = STEP_TIMEOUT, rules: dict = ENTRY_RULES) -> int:
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
        results += run_entry(entry, root, logs, index, timeout, rules)
    total, load_end = round(time.monotonic() - started, 1), load()
    green = all(item["status"] != "fail" for item in results)
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
        # Per-entry rules: exit 4 of the Windows store verifier is NOT ASSESSED (green, excluded), a pass is evidence,
        # exit 1 and any other exit are red, exit 4 of any other script is red, and the entry's own timeout applies.
        (repo / "tools").mkdir()
        stub = repo / "tools" / "verify-windows-store.py"
        step = ["python3", "tools/verify-windows-store.py"]
        for exit_code, ring_exit, status in ((4, 0, "not-assessed"), (0, 0, "evidence"), (1, 1, "fail"), (2, 1, "fail")):
            stub.write_text("raise SystemExit({0})\n".format(exit_code), encoding="utf-8", newline="\n")
            ring([step, ok], "windows store exit {0}".format(exit_code))
            observed = run_ring(repo, join, receipt)
            recorded = [item["status"] for item in json.loads(receipt.read_text(encoding="utf-8"))["results"]][0]
            if observed != ring_exit or recorded != status or check(repo, receipt) != ring_exit:
                problems.append("verifier exit {0} gave ring {1} and status {2}, not {3} and {4}".format(
                    exit_code, observed, recorded, ring_exit, status))
        ring([["python3", "-c", "raise SystemExit(4)"]], "exit 4 of another script")
        if run_ring(repo, join, receipt) != 1:
            problems.append("exit 4 of a script with no rule was not red")
        # A rule belongs to the executed script only: a ruled name in a later argument (a --skip list) earns no rule.
        (repo / "tools" / "other.py").write_text("raise SystemExit(4)\n", encoding="utf-8", newline="\n")
        ring([["python3", "tools/other.py", "--skip", "verify-windows-store.py"]], "ruled name as an argument")
        observed = run_ring(repo, join, receipt)
        recorded = json.loads(receipt.read_text(encoding="utf-8"))["results"][0]["status"]
        if observed != 1 or recorded != "fail":
            problems.append("a ruled script named only as an argument got its rule: ring {0}, status {1}".format(observed, recorded))
        stub.write_text("import time\ntime.sleep(30)\n", encoding="utf-8", newline="\n")
        ring([step], "verifier hang")
        began = time.monotonic()
        short = {"verify-windows-store.py": {"timeout": 1, "notAssessed": (4,)}}
        if run_ring(repo, join, receipt, rules=short) != 1 or time.monotonic() - began > 20:
            problems.append("an entry timeout override did not kill the hung verifier and report red")
        # CLEANUP-BLOCKS-CEILING (Ruling 171 (1)): a real child that ignores SIGTERM and leaves a grandchild in its own
        # session, an already-expired deadline, and a failed group kill. finish() must return bounded and not raise.
        if os.name != "nt":
            holder = ("import signal, subprocess, sys, time; signal.signal(signal.SIGTERM, signal.SIG_IGN); "
                      "subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(8)'], start_new_session=True); time.sleep(30)")
            real_killpg = os.killpg

            def failing_killpg(*_):
                raise PermissionError("injected kill failure")

            for label, limit, kill in (("expired deadline", -1, real_killpg), ("timeout", 1, real_killpg), ("failed kill", 1, failing_killpg)):
                child = subprocess.Popen([sys.executable, "-c", holder], start_new_session=True, stdout=subprocess.DEVNULL)
                began, os.killpg = time.monotonic(), kill
                try:
                    code, timed_out, _ = finish([(None, None, child, None)], [began + limit])[0]
                    outcome = "" if timed_out else "not timed out"
                except Exception as error:  # noqa: BLE001 - the unfixed path raises here; the self-test reports it
                    outcome = "raised {0!r}".format(error)
                finally:
                    os.killpg = real_killpg
                cleanup = time.monotonic() - began - max(limit, 0)
                print("cleanup after {0}: {1:.2f} s (bound {2:.0f} s)".format(label, cleanup, KILL_WAIT_SECONDS))
                if outcome or cleanup > KILL_WAIT_SECONDS + 1:
                    problems.append("{0}: {1}, cleanup {2:.2f} s".format(label, outcome or "slow", cleanup))
                try:
                    real_killpg(child.pid, signal.SIGKILL)
                except OSError:
                    pass
                child.wait()
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
