#!/usr/bin/env python3
"""A shared regen debt must write into the invoking worktree, not primary."""

import importlib.util
import os
from pathlib import Path
import subprocess
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


def run(argv: list[str], cwd: Path, *, env: dict[str, str] | None = None) -> None:
    result = subprocess.run(
        argv, cwd=cwd, env=env, capture_output=True, text=True,
        encoding="utf-8", errors="replace", check=False,
    )
    if result.returncode:
        raise AssertionError(f"{argv[0]} exited {result.returncode}: {result.stdout}{result.stderr}")


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="coord-regen-worktree-") as directory:
        base = Path(directory)
        primary = base / "primary"
        linked = base / "linked"
        primary.mkdir()
        run(["git", "init", "-q"], primary)
        (primary / "docs/audit").mkdir(parents=True)
        (primary / "docs/audit/audit-data.js").write_text("original\n", encoding="utf-8", newline="\n")
        (primary / "generate.py").write_text(
            "from pathlib import Path\nPath('docs/audit/audit-data.js').write_text('regenerated\\n', encoding='utf-8', newline='\\n')\n",
            encoding="utf-8", newline="\n",
        )
        (primary / ".agents").mkdir()
        (primary / ".agents/artifacts.yml").write_text(
            "docs/audit/audit-data.js: derived python generate.py\n",
            encoding="utf-8",
            newline="\n",
        )
        run(["git", "add", "-A"], primary)
        run(["git", "-c", "user.name=Regen Test", "-c", "user.email=regen@example.invalid", "commit", "-q", "-m", "fixture"], primary)
        run(["git", "worktree", "add", "-q", "-b", "linked", str(linked)], primary)
        store = primary / ".agents"
        core.record_regen_owed(store, "docs/audit/audit-data.js")
        env = os.environ.copy()
        env["AGENT_SESSION"] = "coord-regen-test"
        env["AGENT_HOST"] = "codex"
        env["PYTHONPATH"] = str(CORE.parent) + os.pathsep + env.get("PYTHONPATH", "")
        old_call = "code, results = cmd_regen(root, checkout_top(os.getcwd()), args.timeout)"
        source = CORE.read_text(encoding="utf-8")
        assert source.count(old_call) == 1
        before = base / "coord-core-before.py"
        before.write_text(
            source.replace(old_call, "code, results = cmd_regen(root, repo, args.timeout)"),
            encoding="utf-8", newline="\n",
        )
        run([sys.executable, str(before), "regen"], linked, env=env)
        assert (primary / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "regenerated\n"
        assert (linked / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "original\n"
        (primary / "docs/audit/audit-data.js").write_text("original\n", encoding="utf-8", newline="\n")
        core.record_regen_owed(store, "docs/audit/audit-data.js")
        run([sys.executable, str(CORE), "regen"], linked, env=env)
        assert (primary / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "original\n"
        assert (linked / "docs/audit/audit-data.js").read_text(encoding="utf-8") == "regenerated\n"
        assert not core.regen_owed(store)
    print("verify-coord-regen-worktree: linked checkout updated, primary unchanged")
    return 0


if __name__ == "__main__":
    sys.exit(main())
