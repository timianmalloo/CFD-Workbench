#!/usr/bin/env python3
"""F1 re-scope verification, plan docs/plans/f1-rescope-r237.md node V (Rulings 237, 240): the whole offline union of the
packet's checks, green on both PowerShell runtimes, with the suite-count floors the plan sets (V refuses a smaller count
before it runs anything). Width 2: the Python unit suites and the PowerShell suites run concurrently; everything else is
serial. The receipt (schema cfdw-f1-rescope-verification/1) is the code-stage proof pack the independent review (node R)
reads: HEAD, the counts, every step's command, exit and seconds. Windows only (pwsh and Windows PowerShell 5.1 are both
required); no WSL, no network.

    py -3 tools/run-f1-verification.py --receipt docs/proof/win-gpu-f1-preflight/rescope/verification-v.json
    py -3 tools/run-f1-verification.py --self-test     # a smaller count and a red step both refuse

Ring: by hand at nodes V and R (its pieces run in the fast ring and at readiness); ~2.5 minutes measured 2026-10-19.
Exit 0 green, 1 red or a count below its floor, 2 usage, 3 not Windows.
"""
import argparse
import datetime
import json
import os
import re
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
SCHEMA = "cfdw-f1-rescope-verification/1"
PACKET = "docs/proof/win-gpu-f1-preflight/packet"
PYTHON_FLOOR = 9       # plan section 5, node V: 6 before the re-scope + architecture, transaction roots, attempt-2 prepare
POWERSHELL_FLOOR = 9   # 7 before the re-scope + the C2 and C4 suites
PY = [sys.executable]
PWSH = ["pwsh", "-NoProfile", "-NonInteractive", "-File"]
RAN = re.compile(r"^Ran (\d+) tests? in ([0-9.]+)s$", re.M)
PS_TOTAL = re.compile(r"^F1-PS total (\d+) runs \((\d+) suites x (\d+) runtimes\) (\d+) ms$", re.M)


def count_suites() -> dict:
    return {"python_files": len(list((ROOT / "tests").glob("test_f1_*.py"))),
            "powershell_files": len(list((ROOT / "tests").glob("f1_*.ps1")))}


def check_floors(counts: dict, python_floor: int, powershell_floor: int) -> list[str]:
    """The plan's floors: a smaller count is a refusal, never a warning (a deleted suite is a lost control)."""
    refusals = []
    if counts["python_files"] < python_floor:
        refusals.append(f"python suites {counts['python_files']} < floor {python_floor}")
    if counts["powershell_files"] < powershell_floor:
        refusals.append(f"powershell suites {counts['powershell_files']} < floor {powershell_floor}")
    return refusals


def run_step(name: str, command: list[str], scratch: Path, timeout: int) -> dict:
    """One child, its stdout and stderr captured to the scratch (never into the receipt), its exit and seconds recorded."""
    started = time.perf_counter()
    out_path, err_path = scratch / f"{name}.stdout.txt", scratch / f"{name}.stderr.txt"
    with out_path.open("wb") as out, err_path.open("wb") as err:
        try:
            exit_code = subprocess.run(command, cwd=ROOT, stdout=out, stderr=err, timeout=timeout).returncode
        except subprocess.TimeoutExpired:
            exit_code = -1
        except OSError as error:
            err.write(str(error).encode("utf-8"))
            exit_code = -2
    seconds = round(time.perf_counter() - started, 1)
    # The receipt is committed under docs/proof: no absolute path of this machine reaches it (check-proof-pii refuses a
    # Windows user path; the first V run recorded the scratch path and was refused, as it should be).
    shown = ["py -3" if part == sys.executable else
             str(part).replace(str(scratch), "<scratch>").replace(str(ROOT), "<repo>") for part in command]
    return {"name": name, "command": " ".join(shown), "exit": exit_code, "seconds": seconds,
            "stdout": out_path.read_text(encoding="utf-8", errors="replace"),
            "stderr": err_path.read_text(encoding="utf-8", errors="replace")}


def summarize(step: dict) -> str:
    text = step["stderr"] + step["stdout"]
    ran = RAN.search(text)
    if ran:
        return f"ran {ran.group(1)} tests in {ran.group(2)} s"
    total = PS_TOTAL.search(text)
    if total:
        return f"{total.group(1)} runs ({total.group(2)} suites x {total.group(3)} runtimes) {total.group(4)} ms"
    lines = [line for line in step["stdout"].splitlines() if line.strip()]
    return lines[-1][:160] if lines else ("exit " + str(step["exit"]))


def latest_run_root() -> str:
    """The highest attempt-<n> run root under the proof folder: the next attempt's, once prepared (Ruling 245 (3))."""
    folders = sorted((p for p in (ROOT / "docs/proof/win-gpu-f1-preflight").glob("attempt-*") if p.is_dir()),
                     key=lambda p: int(p.name.split("-")[1]))
    if not folders:
        raise SystemExit("run-f1-verification: no attempt-<n> run root under docs/proof/win-gpu-f1-preflight")
    return "docs/proof/win-gpu-f1-preflight/" + folders[-1].name


def plan_steps(scratch: Path) -> dict:
    packet = ROOT / PACKET
    serial_before = [
        ("authority-scan", PY + [str(packet / "authority_scan.py")], 120),
        ("packet-selftest", PY + ["-B", str(packet / "packet_selftest.py")], 300),
        ("transaction-selftest", PY + [str(packet / "transaction.py"), "self-test"], 120),
        ("manifest-selftest", PY + [str(packet / "manifest.py"), "self-test"], 120),
        ("completed-guard-selftest", PY + [str(packet / "completed_guard.py"), "self-test"], 120),
        ("powershell-packet-selftest", PWSH + [str(packet / "SelfTest.ps1")], 300),
        ("dry-rehearsal", PWSH + [str(ROOT / "tools/run-f1-dry-rehearsal.ps1"), "-RunRoot", latest_run_root(),
                          "-Receipt", str(scratch / "rehearsal.json")], 300),   # Ruling 245 (3): the launcher's Windows-side paths, no WSL effect
    ]
    width_two = [
        ("python-unit", PY + ["-m", "unittest", "discover", "-s", "tests", "-p", "test_f1_*.py"], 900),
        ("powershell-suites", PWSH + [str(ROOT / "tools/run-f1-powershell-suites.ps1"), "-Scratch", str(scratch / "ps")], 900),
    ]
    serial_after = [
        ("host-bindings", PY + [str(ROOT / "tools/check-f1-host-bindings.py")], 120),
        ("wsl-inline", PY + [str(ROOT / "tools/check-wsl-inline.py")], 120),
        ("proof-pii", PY + [str(ROOT / "tools/check-proof-pii.py")], 300),
        ("check-docs", PY + [str(ROOT / "tools/check-docs.py")], 900),
    ]
    return {"before": serial_before, "parallel": width_two, "after": serial_after}


def run_union(scratch: Path) -> list[dict]:
    plan = plan_steps(scratch)
    (scratch / "ps").mkdir(exist_ok=True)
    results = [run_step(name, command, scratch, timeout) for name, command, timeout in plan["before"]]
    parallel: list[dict | None] = [None] * len(plan["parallel"])

    def worker(index: int, name: str, command: list[str], timeout: int) -> None:
        parallel[index] = run_step(name, command, scratch, timeout)

    threads = [threading.Thread(target=worker, args=(index, *entry)) for index, entry in enumerate(plan["parallel"])]
    for thread in threads:
        thread.start()
    for thread in threads:
        thread.join()
    results.extend(step for step in parallel if step is not None)
    results.extend(run_step(name, command, scratch, timeout) for name, command, timeout in plan["after"])
    return results


def receipt_from(head: str, counts: dict, floors: dict, refusals: list[str], steps: list[dict], started: float) -> dict:
    measured = {}
    for step in steps:
        text = step["stderr"] + step["stdout"]
        ran, total = RAN.search(text), PS_TOTAL.search(text)
        if ran:
            measured["python_tests_ran"] = int(ran.group(1))
        if total:
            measured.update(powershell_runs=int(total.group(1)), powershell_suites_run=int(total.group(2)),
                            powershell_runtimes=int(total.group(3)), powershell_ms=int(total.group(4)))
    green = not refusals and bool(steps) and all(step["exit"] == 0 for step in steps)
    return {
        "schema": SCHEMA, "head": head,
        "utc": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "host": {"platform": sys.platform, "machine": os.environ.get("PROCESSOR_ARCHITECTURE", "Not recorded"),
                 "python": sys.version.split()[0]},
        "floors": floors, "counts": {**counts, **measured}, "refusals": refusals, "green": green,
        "total_seconds": round(time.perf_counter() - started, 1),
        "steps": [{key: step[key] for key in ("name", "command", "exit", "seconds")} | {"summary": summarize(step)}
                  for step in steps],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--receipt", type=Path, help="where to write the receipt (JSON); default: print only")
    parser.add_argument("--scratch", type=Path, help="where child output goes; default: a temporary directory")
    parser.add_argument("--python-floor", type=int, default=PYTHON_FLOOR)
    parser.add_argument("--powershell-floor", type=int, default=POWERSHELL_FLOOR)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    if sys.platform != "win32":
        print("run-f1-verification: Windows only (both PowerShell runtimes are required)", file=sys.stderr)
        return 3
    started = time.perf_counter()
    head = subprocess.run(["git", "-C", str(ROOT), "rev-parse", "HEAD"], check=True, capture_output=True,
                          text=True, encoding="utf-8", errors="replace", timeout=15).stdout.strip()
    # The receipt says what tree it measured: HEAD, and whether anything beyond the receipt itself was uncommitted.
    status_args = ["git", "-C", str(ROOT), "status", "--porcelain", "--untracked-files=all", "--", "."]
    if args.receipt:
        status_args.append(":(exclude)" + args.receipt.resolve().relative_to(ROOT).as_posix())
    uncommitted = subprocess.run(status_args, check=True, capture_output=True, text=True, encoding="utf-8",
                                 errors="replace", timeout=30).stdout.splitlines()
    counts = count_suites()
    floors = {"python_files": args.python_floor, "powershell_files": args.powershell_floor}
    refusals = check_floors(counts, args.python_floor, args.powershell_floor)
    steps: list[dict] = []
    if args.receipt and args.receipt.exists():
        args.receipt.unlink()   # a stale receipt in the tree would be read by the proof-pii and check-docs steps
    if refusals:
        for refusal in refusals:
            print("F1-VERIFICATION-FLOOR: " + refusal)
    else:
        scratch = args.scratch or Path(tempfile.mkdtemp(prefix="cfdw-f1-verification-"))
        scratch.mkdir(parents=True, exist_ok=True)
        steps = run_union(scratch)
        for step in steps:
            status = "ok  " if step["exit"] == 0 else "FAIL"
            print(f"== f1-verification {status} {step['seconds']:7.1f} s  {step['name']}: {summarize(step)}")
            if step["exit"] != 0:
                tail = (step["stderr"] or step["stdout"]).splitlines()[-12:]
                for line in tail:
                    print("   | " + line)
    receipt = receipt_from(head, counts, floors, refusals, steps, started)
    receipt["worktree_clean"] = not uncommitted
    receipt["uncommitted_paths"] = sorted(line[3:] for line in uncommitted)
    if args.receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8", newline="\n")
    verdict = "GREEN" if receipt["green"] else "RED"
    print(f"F1-VERIFICATION-{verdict} head={head[:9]} clean={receipt['worktree_clean']} "
          f"python_files={counts['python_files']} powershell_files={counts['powershell_files']} total={receipt['total_seconds']} s")
    return 0 if receipt["green"] else 1


def self_test() -> int:
    counts = count_suites()
    assert check_floors(counts, counts["python_files"] + 1, POWERSHELL_FLOOR), "a smaller Python count passed"
    assert check_floors(counts, PYTHON_FLOOR, counts["powershell_files"] + 1), "a smaller PowerShell count passed"
    assert not check_floors(counts, PYTHON_FLOOR, POWERSHELL_FLOOR), "today's counts are below the plan's floors"
    red = {"name": "planted", "command": "x", "exit": 1, "seconds": 0.1, "stdout": "", "stderr": "boom"}
    green = dict(red, exit=0, stderr="Ran 3 tests in 0.010s\n\nOK\n")
    assert not receipt_from("a" * 40, counts, {}, [], [green, red], time.perf_counter())["green"], "a red step passed"
    assert receipt_from("a" * 40, counts, {}, [], [green], time.perf_counter())["green"], "a green step failed"
    assert not receipt_from("a" * 40, counts, {}, [], [], time.perf_counter())["green"], "no steps passed"
    assert not receipt_from("a" * 40, counts, {}, ["floor"], [green], time.perf_counter())["green"], "a floor refusal passed"
    assert receipt_from("a" * 40, counts, {}, [], [green], time.perf_counter())["counts"]["python_tests_ran"] == 3
    assert summarize(green) == "ran 3 tests in 0.010 s"
    with tempfile.TemporaryDirectory() as folder:
        step = run_step("planted-exit", PY + ["-c", "import sys; print('planted'); sys.exit(7)"], Path(folder), 30)
        assert step["exit"] == 7 and summarize(step) == "planted", step
        step = run_step("planted-paths", PY + [str(ROOT / "x.py"), "-Scratch", str(Path(folder) / "ps")], Path(folder), 30)
        assert step["command"] == "py -3 <repo>\\x.py -Scratch <scratch>\\ps".replace("\\", os.sep), step["command"]
        assert sys.executable not in step["command"] and folder not in step["command"]
    print(f"run-f1-verification self-test: PASS floors python>={PYTHON_FLOOR} powershell>={POWERSHELL_FLOOR} "
          f"today python={counts['python_files']} powershell={counts['powershell_files']}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, subprocess.SubprocessError, AssertionError) as error:
        print(f"run-f1-verification: {error}", file=sys.stderr)
        sys.exit(1)
