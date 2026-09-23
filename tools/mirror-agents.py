#!/usr/bin/env python3
"""Mirror the project's domain-expert agent cards from .claude/agents/ to the Copilot and Grok trees.

The Claude card is the record. The Copilot mirror (.github/agents/<name>.agent.md) and the Grok mirror
(.grok/agents/<name>.md) are identical except that the `tools:` frontmatter line is dropped, exactly as
the pack's own general lenses are mirrored. Run after editing any listed card; `--check` fails on drift.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parents[1]
EXPERTS = [
    "hydrofoil-hydrodynamicist",
    "cfd-numerical-verification-expert",
    "computational-geometry-expert",
    "marine-cad-ux-expert",
    "structures-materials-expert",
    "manufacturing-cam-expert",
    "design-optimization-expert",
]


def mirror_text(src: str) -> str:
    return re.sub(r"^tools: .*\n", "", src, count=1, flags=re.M)


def main() -> int:
    check = "--check" in sys.argv
    drift: list[str] = []
    for name in EXPERTS:
        src_path = ROOT / ".claude" / "agents" / f"{name}.md"
        if not src_path.exists():
            raise SystemExit(f"missing card: {src_path}")
        text = mirror_text(src_path.read_text(encoding="utf-8"))
        for target in (ROOT / ".github" / "agents" / f"{name}.agent.md", ROOT / ".grok" / "agents" / f"{name}.md"):
            if check:
                if not target.exists() or target.read_text(encoding="utf-8") != text:
                    drift.append(str(target.relative_to(ROOT)))
            else:
                target.write_text(text, encoding="utf-8", newline="\n")
    if check:
        if drift:
            print("agent mirrors drift: " + ", ".join(drift))
            return 1
        print(f"agent mirrors current ({len(EXPERTS)} experts)")
        return 0
    print(f"mirrored {len(EXPERTS)} expert cards to .github/agents and .grok/agents")
    return 0


if __name__ == "__main__":
    sys.exit(main())
