#!/usr/bin/env python3
"""Prove rollup source links resolve from the destination document's directory."""

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


ROOT = Path(__file__).resolve().parents[1]
GRAPH = ROOT / "docs/ai-forward-pack/scripts/docs-graph.py"


def main():
    with tempfile.TemporaryDirectory(prefix="cfd-rollup-links-") as directory:
        docs = Path(directory) / "docs"
        source = docs / "design/fixture.md"
        source.parent.mkdir(parents=True)
        source.write_text(
            "---\nid: design-fixture\ntitle: Fixture\ntype: design\n"
            'status: proposed\nowner: "@test"\nreview-by: 2099-01-01\n'
            "summary: Test fixture.\n---\n\n## Risks\n\n"
            "| Boundary | Risk |\n|---|---|\n| File | Tamper |\n",
            encoding="utf-8", newline="\n",
        )
        nested = docs / "security"
        nested.mkdir()
        for destination, options, expected in (
            (docs, [], "design/fixture.md"),
            (nested, ["--relative-to", str(nested)], "../design/fixture.md"),
        ):
            result = subprocess.run(
                [sys.executable, str(GRAPH), "--root", str(docs), "rollup",
                 "--heading", "Risks", "--type", "design", *options],
                capture_output=True, text=True, encoding="utf-8", timeout=30,
            )
            assert result.returncode == 0, result.stderr
            assert f"[design-fixture]({expected})" in result.stdout, result.stdout
            assert (destination / expected).resolve() == source.resolve()
            assert (destination / expected).is_file()
        print("Rollup links: default and nested destinations resolve to the real source.")


if __name__ == "__main__":
    main()
