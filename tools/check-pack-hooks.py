#!/usr/bin/env python3
"""Reject duplicate managed Claude hook targets after an additive pack refresh."""
import json
from pathlib import Path
import re
import sys

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


CONFIGS = (".claude/settings.json", ".grok/hooks/ai-forward.json", ".agents/hooks.json",
           ".github/hooks/ai-forward.json")
HOOK_PATH = re.compile(r"(?:docs/ai-forward-pack|tools)/hooks/[\w-]+\.py")


def command_strings(node):
    """Every string under a hook-command key, in any of the four harness shapes."""
    if isinstance(node, dict):
        for key, value in node.items():
            if key in ("command", "bash", "powershell") and isinstance(value, str):
                yield value
            else:
                yield from command_strings(value)
    elif isinstance(node, list):
        for item in node:
            yield from command_strings(item)


def bare_relative_commands(root):
    """HOOK-CWD-RELATIVE: a hook command must still find its script after the shell cwd moves
    into a subdirectory, so it must fall back to `git rev-parse --show-toplevel`."""
    bad = []
    for name in CONFIGS:
        data = json.loads((root / name).read_text(encoding="utf-8"))
        for cmd in command_strings(data):
            if HOOK_PATH.search(cmd) and "git rev-parse --show-toplevel" not in cmd:
                bad.append((name, cmd))
    return bad


def main():
    root = Path(__file__).resolve().parents[1]
    bare = bare_relative_commands(root)
    if bare:
        raise SystemExit("HOOK-CWD-RELATIVE: hook commands without the git-toplevel fallback: " + repr(bare))
    settings = json.loads((root / ".claude/settings.json").read_text(encoding="utf-8"))
    seen = set()
    duplicates = []
    for event, groups in settings.get("hooks", {}).items():
        for group in groups:
            for hook in group.get("hooks", []):
                match = re.search(r"(?:docs/ai-forward-pack|tools)/hooks/[\w-]+\.py", hook.get("command", ""))
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
