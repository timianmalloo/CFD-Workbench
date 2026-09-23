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


def main():
    run(ROOT / "tools" / "check-pack-hooks.py")
    run(ROOT / "tools" / "check-rollup-links.py")
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
