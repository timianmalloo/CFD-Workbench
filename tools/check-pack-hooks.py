#!/usr/bin/env python3
"""Reject duplicate managed Claude hook targets after an additive pack refresh."""
import json
from pathlib import Path
import re


def main():
    root = Path(__file__).resolve().parents[1]
    settings = json.loads((root / ".claude/settings.json").read_text(encoding="utf-8"))
    seen = set()
    duplicates = []
    for event, groups in settings.get("hooks", {}).items():
        for group in groups:
            for hook in group.get("hooks", []):
                match = re.search(r"docs/ai-forward-pack/hooks/[\w-]+\.py", hook.get("command", ""))
                if not match:
                    continue
                key = (event, group.get("matcher", ""), match.group())
                if key in seen:
                    duplicates.append(key)
                seen.add(key)
    if duplicates:
        raise SystemExit("Duplicate managed hook targets: " + repr(duplicates))
    if not seen:
        raise SystemExit("No managed Claude hooks inspected")
    print(f"Managed Claude hooks: {len(seen)} unique event/matcher/target entries")


if __name__ == "__main__":
    main()
