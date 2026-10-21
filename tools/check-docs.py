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


def run_lesson_controls():
    """The controls that remain after Ruling 247: each self-test is red on its planted shape. DERIVED-UNBOUND,
    BRIEF-FIXTURE-AGAINST-SPEC, the AGENT-HEREDOC hook, WINDOWS-TEXT-MODE-HASH, PROOF-PII (and the GitHub posters),
    READER-SHARE, the register merge and dispatch gates, and CASE-SCHEMA-UNGATED (cases/tools/validate-cases.py,
    which needs uv or fails by name when it is missing)."""
    for script, arguments in (
        ("check-artifact-bindings.py", ("--self-test",)),
        ("check-artifact-bindings.py", ()),
        ("check-foildsl-spec-sync.py", ("--self-test",)),
        ("check-foildsl-spec-sync.py", ()),
        ("trace-brief.py", ("--self-test",)),
        ("hooks/no-heredoc.py", ("--self-test",)),
        ("check-text-mode-hash.py", ("--self-test",)),
        ("check-text-mode-hash.py", ()),
        ("check-proof-pii.py", ("--self-test",)),
        ("check-proof-pii.py", ()),
        ("post-github-comment.py", ("--self-test",)),  # PII-OUTSIDE-THE-TREE; fast ring, ~0.2 s measured
        ("scan-github-text.py", ("--self-test",)),  # PII-OUTSIDE-THE-TREE; fast ring, ~0.2 s measured; live scan is on demand
        ("check-reader-sharing.py", ("--self-test",)),
        ("check-reader-sharing.py", ()),  # also proof/tools PowerShell, hash-pinned grandfathering of 7 frozen B1/B2 PowerShell hits (Ruling 229); ~0.3 s
        ("merge-defect-register.py", ("--self-test",)),  # JOIN-LOG-CONFLICT; fast ring, ~0.05 s
        ("dispatch-gate.py", ("--self-test",)),
        ("../cases/tools/validate-cases.py", ("--self-test",)),
        ("../cases/tools/validate-cases.py", ()),
    ):
        result = subprocess.run(
            [sys.executable, str(ROOT / "tools" / script), *arguments],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", timeout=120,
        )
        print(result.stdout, end="", flush=True)
        if result.returncode != 0:
            print(result.stderr, end="", file=sys.stderr, flush=True)
            raise SystemExit(script + " " + " ".join(arguments) + " failed.")


def check_store_subset():
    """STORE-SUBSET: the umask partition rule caught statically instead of waiting for readiness."""
    partition_names()
    print("store subset ok: umask partition holds statically", flush=True)


def main():
    check_store_subset()
    run(ROOT / "tools" / "check-merge-bindings.py")
    run(ROOT / "tools" / "check-pack-hooks.py")
    run(ROOT / "tools" / "check-rollup-links.py")
    run(ROOT / "tools" / "coordination" / "check-process-match.py")
    run(ROOT / "tools" / "coordination" / "check-test-paths.py")
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
