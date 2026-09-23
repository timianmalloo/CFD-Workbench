#!/usr/bin/env python3
"""A shared regen debt must write into the invoking worktree, not primary."""

import importlib.util
from pathlib import Path
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


CORE = Path(__file__).resolve().parent.parent / "docs/ai-forward-pack/scripts/coord-core.py"
spec = importlib.util.spec_from_file_location("coord_core_regen_test", CORE)
core = importlib.util.module_from_spec(spec)
spec.loader.exec_module(core)


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="coord-regen-worktree-") as directory:
        base = Path(directory)
        primary = base / "primary"
        linked = base / "linked"
        for tree in (primary, linked):
            (tree / "docs/audit").mkdir(parents=True)
            (tree / "docs/audit/audit-data.js").write_text("original\n", encoding="utf-8", newline="\n")
            (tree / "generate.py").write_text(
                "from pathlib import Path\nPath('docs/audit/audit-data.js').write_text('regenerated\\n', encoding='utf-8', newline='\\n')\n",
                encoding="utf-8",
                newline="\n",
            )
        (primary / ".agents").mkdir()
        (primary / ".agents/artifacts.yml").write_text(
            "docs/audit/audit-data.js: derived python generate.py\n",
            encoding="utf-8",
            newline="\n",
        )
        store = primary / ".agents"
        core.record_regen_owed(store, "docs/audit/audit-data.js")
        code, results = core.cmd_regen(store, linked)
        assert code == 0 and results[0]["status"] == "ok", results
        assert (primary / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "original\n"
        assert (linked / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "regenerated\n"
        assert not core.regen_owed(store)
    print("verify-coord-regen-worktree: linked checkout updated, primary unchanged")
    return 0


if __name__ == "__main__":
    sys.exit(main())
