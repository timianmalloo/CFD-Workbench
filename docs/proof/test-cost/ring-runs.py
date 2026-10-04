#!/usr/bin/env python3
"""Count how often agent sessions ran the fast ring, readiness, and narrowed runs.

usage: python3 docs/proof/test-cost/ring-runs.py <claude-projects-dir-for-this-repo>
Reads Claude Code transcripts (*.jsonl, including subagent transcripts) and counts Bash tool calls
whose command runs tools/run-tests.sh, tools/run-readiness.py (not --check), or a CFD_TEST_ONLY /
single-mode run. Prints the per-session distribution. Copilot/Grok/agy sessions are not counted:
their share is "not recorded" here, so the totals are a floor.
"""
import json
import re
import statistics
import sys
from pathlib import Path

PATTERNS = {
    "ring": re.compile(r"tools/run-tests\.sh"),
    "readiness": re.compile(r"tools/run-readiness\.py(?!\s+--check)"),
    "narrow": re.compile(r"CFD_TEST_ONLY=|Tests\.dll\s+--[a-z-]+|--project\s+\S*Tests\.csproj"),
}
per_session = {}
for path in Path(sys.argv[1]).rglob("*.jsonl"):
    counts = dict.fromkeys(PATTERNS, 0)
    for line in path.open(encoding="utf-8", errors="replace"):
        if '"Bash"' not in line:
            continue
        try:
            record = json.loads(line)
        except ValueError:
            continue
        content = (record.get("message") or {}).get("content")
        if not isinstance(content, list):
            continue
        for block in content:
            if isinstance(block, dict) and block.get("type") == "tool_use" and block.get("name") == "Bash":
                command = str((block.get("input") or {}).get("command", ""))
                for key, pattern in PATTERNS.items():
                    if pattern.search(command):
                        counts[key] += 1
    if any(counts.values()):
        per_session[str(path)] = counts
for key in PATTERNS:
    values = [c[key] for c in per_session.values() if c[key]]
    if values:
        values.sort()
        print(f"{key:9}: {sum(values):5} runs in {len(values):4} transcripts; median {statistics.median(values)}, "
              f"p90 {values[int(len(values) * .9) - 1]}, max {values[-1]}")
    else:
        print(f"{key:9}: none recorded")
