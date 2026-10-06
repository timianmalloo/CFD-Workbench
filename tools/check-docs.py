#!/usr/bin/env python3
"""Run the AI-Forward checks against a consuming repository, including a fresh install."""

import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
from store_subset import partition_names  # noqa: E402

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


def run_lesson_controls():
    """Controls from docs/lessons/defect-classes.md, fast ring (each self-test is red on its planted shape):
    DERIVED-UNBOUND, BRIEF-FIXTURE-AGAINST-SPEC, and the OWNERSHIP-MISSES-DATA-SOURCE aid's own test."""
    for script, arguments in (
        ("check-artifact-bindings.py", ("--self-test",)),
        ("check-artifact-bindings.py", ()),
        ("check-foildsl-spec-sync.py", ("--self-test",)),
        ("check-foildsl-spec-sync.py", ()),
        ("trace-brief.py", ("--self-test",)),
    ):
        result = subprocess.run(
            [sys.executable, str(ROOT / "tools" / script), *arguments],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", timeout=120,
        )
        print(result.stdout, end="", flush=True)
        if result.returncode != 0:
            print(result.stderr, end="", file=sys.stderr, flush=True)
            raise SystemExit(script + " " + " ".join(arguments) + " failed.")


SLOW_GATES = ("verify-application-core.py", "verify-application-adapters.py")
RECOUNTS = ("tools/recount-architecture-spike.py", "tools/recount-application-contracts.py")


def join_ring_problems(contract):
    """TEST-RING: the join's fast ring runs the tests and skips the slow gates; the readiness
    ring keeps every slow gate and recount (docs/reviews/test-ci-waste.md)."""
    def lines(key):
        # A readiness entry may be a concurrent group: a list of commands (tools/run-readiness.py).
        commands = [command for entry in contract.get(key) or []
                    for command in (entry if entry and isinstance(entry[0], list) else [entry])]
        return [" ".join(command) for command in commands]

    checks, gates, readiness = lines("checks"), lines("gates"), lines("readiness")
    problems = []
    # Ruling 89: the join runs the ring through tools/join-ring.sh, which skips it for a docs-only merge and
    # otherwise runs tools/run-tests.sh and tools/check-test-costs.py. A literal run-tests.sh entry is also accepted.
    wrapper = ROOT / "tools" / "join-ring.sh"
    if any("tools/join-ring.sh" in line for line in checks):
        text = wrapper.read_text(encoding="utf-8") if wrapper.exists() else ""
        if "tools/run-tests.sh" not in text or "tools/check-test-costs.py" not in text:
            problems.append("tools/join-ring.sh does not run tools/run-tests.sh and tools/check-test-costs.py")
    elif not any("tools/run-tests.sh" in line for line in checks):
        problems.append("join checks do not run tools/run-tests.sh (directly or through tools/join-ring.sh)")
    if not any("xaml-token-lint.py" in line for line in checks):
        problems.append("join checks do not run xaml-token-lint.py")
    if any(recount in line for line in lines("recount") + gates for recount in RECOUNTS):
        problems.append("a spike recount is in the every-join ring")
    for gate in SLOW_GATES:
        if not all("--skip" in line and gate in line for line in gates if "run-verify-gates.py" in line):
            problems.append("join gates run " + gate + " on every join")
    # Every verify gate: run-verify-gates.py with no --skip, or with each skipped gate run as its own readiness step
    # (so the slow gates can overlap; docs/plans/test-cost.md L4).
    def every_gate(line):
        if "--skip" not in line:
            return True
        skipped = line.split("--skip", 1)[1].split()
        return all(any(other.endswith("tools/" + gate) for other in readiness) for gate in skipped)
    if not any("run-verify-gates.py" in line and every_gate(line) for line in readiness):
        problems.append("readiness does not run every verify gate")
    for recount in RECOUNTS:
        if not any(recount in line for line in readiness):
            problems.append("readiness does not run " + recount)
    # The Core harness's readiness tier holds the full binding sweeps the fast ring samples; it ran
    # nowhere until 2026-10-02 because nothing passed the switch.
    if not any("CfdWorkbench.Core.Tests" in line and "--readiness" in line for line in readiness):
        problems.append("readiness does not run the Core harness with --readiness")
    # The Desktop harness's --readiness tier (render and frame budgets) was likewise run by nothing until VW1 found it.
    if not any("CfdWorkbench.Desktop.Tests" in line and "--readiness" in line for line in readiness):
        problems.append("readiness does not run the Desktop harness with --readiness")
    return problems


def check_join_rings():
    path = ROOT / "docs" / "coordination" / "join.json"
    problems = join_ring_problems(json.loads(path.read_text(encoding="utf-8")))
    if problems:
        raise SystemExit("TEST-RING: " + "; ".join(problems) + " (" + str(path.relative_to(ROOT)) + ")")
    print("join rings ok: tests every code join (docs-only joins skip, Ruling 89), slow gates and recounts at readiness", flush=True)


def check_store_subset():
    """STORE-SUBSET: the umask partition rule caught statically instead of waiting for readiness."""
    partition_names()
    print("store subset ok: umask partition holds statically", flush=True)


def main():
    check_join_rings()
    check_store_subset()
    run(ROOT / "tools" / "check-pack-hooks.py")
    run(ROOT / "tools" / "check-rollup-links.py")
    run(ROOT / "tools" / "coordination" / "check-process-match.py")
    run(ROOT / "tools" / "coordination" / "check-test-paths.py")
    run(ROOT / "tools" / "check-debug-parity.py")
    run_spiral_check()
    run_lesson_controls()
    graph =SCRIPTS / "docs-graph.py"
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
