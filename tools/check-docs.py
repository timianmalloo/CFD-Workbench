#!/usr/bin/env python3
"""Run the AI-Forward checks against a consuming repository, including a fresh install."""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "docs" / "ai-forward-pack" / "scripts"


def run(script, *arguments, capture=False):
    return subprocess.run(
        [sys.executable, str(script), *arguments],
        cwd=ROOT,
        check=True,
        capture_output=capture,
        text=True,
        encoding="utf-8",
        timeout=120,
    )


def run_spiral_check():
    spiral = ROOT / "tools" / "check-spiral.py"
    self_test = subprocess.run(
        [sys.executable, str(spiral), "--self-test"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=120,
    )
    print(self_test.stdout, end="", flush=True)
    if self_test.returncode != 0:
        print(self_test.stderr, end="", file=sys.stderr, flush=True)
        raise SystemExit("check-spiral.py --self-test failed.")

    result = subprocess.run(
        [sys.executable, str(spiral)],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=120,
    )
    print(result.stdout, end="", flush=True)
    if result.returncode != 0:
        print(result.stderr, end="", file=sys.stderr, flush=True)
        raise SystemExit(result.stdout.strip() or "check-spiral.py failed.")


SLOW_GATES = ("verify-application-core.py", "verify-application-adapters.py")
RECOUNTS = ("tools/recount-architecture-spike.py", "tools/recount-application-contracts.py")


def join_ring_problems(contract):
    """TEST-RING: the join's fast ring runs the tests and skips the slow gates; the readiness
    ring keeps every slow gate and recount (docs/reviews/test-ci-waste.md)."""
    def lines(key):
        return [" ".join(command) for command in contract.get(key) or []]

    checks, gates, readiness = lines("checks"), lines("gates"), lines("readiness")
    problems = []
    if not any("tools/run-tests.sh" in line for line in checks):
        problems.append("join checks do not run tools/run-tests.sh")
    if not any("xaml-token-lint.py" in line for line in checks):
        problems.append("join checks do not run xaml-token-lint.py")
    if any(recount in line for line in lines("recount") + gates for recount in RECOUNTS):
        problems.append("a spike recount is in the every-join ring")
    for gate in SLOW_GATES:
        if not all("--skip" in line and gate in line for line in gates if "run-verify-gates.py" in line):
            problems.append("join gates run " + gate + " on every join")
    if not any("run-verify-gates.py" in line and "--skip" not in line for line in readiness):
        problems.append("readiness does not run every verify gate")
    for recount in RECOUNTS:
        if not any(recount in line for line in readiness):
            problems.append("readiness does not run " + recount)
    # The Core harness's readiness tier holds the full binding sweeps the fast ring samples; it ran
    # nowhere until 2026-10-02 because nothing passed the switch.
    if not any("CfdWorkbench.Core.Tests" in line and "--readiness" in line for line in readiness):
        problems.append("readiness does not run the Core harness with --readiness")
    return problems


def check_join_rings():
    path = ROOT / "docs" / "coordination" / "join.json"
    problems = join_ring_problems(json.loads(path.read_text(encoding="utf-8")))
    if problems:
        raise SystemExit("TEST-RING: " + "; ".join(problems) + " (" + str(path.relative_to(ROOT)) + ")")
    print("join rings ok: tests every join, slow gates and recounts at readiness", flush=True)


def main():
    check_join_rings()
    run(ROOT / "tools" / "check-pack-hooks.py")
    run(ROOT / "tools" / "check-rollup-links.py")
    run_spiral_check()
    graph = SCRIPTS / "docs-graph.py"
    if (ROOT / "docs" / "docs-index.js").exists():
        run(graph, "validate")
        run(graph, "freshness", "--gate", "warn")
    else:
        # INSTALL forbids seeding an index. The audit tool creates its own landing
        # document during installation; allow only that exact bootstrap state.
        # The first content workflow must create and commit the derived index.
        inventory = json.loads(run(graph, "inventory", capture=True).stdout)
        print(json.dumps(inventory, indent=2), flush=True)
        bootstrap = (
            inventory["artifacts"] == 0 and not inventory["orphans"]
        ) or (
            inventory["artifacts"] == 1 and inventory["orphans"] == ["audit-log"]
        )
        if (
            not bootstrap
            or inventory["problems"]
            or inventory["index_drift"] != ["docs-index.js missing"]
        ):
            raise SystemExit("Documentation exists or is invalid: run docs-graph.py derive and validate.")
        print("Fresh install: only bootstrap metadata; index creation deferred to the first content workflow.", flush=True)

    # The reference checker expects scripts/ and knowledge/ beneath the same root.
    # Stage that layout temporarily using the actual installed foundation files.
    with tempfile.TemporaryDirectory(prefix="cfd-workbench-foundation-") as directory:
        staging = Path(directory)
        (staging / "scripts").mkdir()
        checker = staging / "scripts" / "foundation-check.py"
        shutil.copy2(SCRIPTS / "foundation-check.py", checker)
        shutil.copytree(ROOT / ".claude" / "knowledge", staging / "knowledge")
        run(checker)

    run(SCRIPTS / "audit-log.py", "verify")
    print("Documentation checks passed.", flush=True)


if __name__ == "__main__":
    main()
