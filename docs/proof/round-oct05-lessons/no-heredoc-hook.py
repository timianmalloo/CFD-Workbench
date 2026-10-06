#!/usr/bin/env python3
"""DRAFT PreToolUse hook for Claude Code: refuse a Bash command that contains a shell heredoc.

AGENT-HEREDOC control (docs/lessons/defect-classes.md, CT27: a multi-line program is a file, then a run).
NOT INSTALLED: wiring it into .claude/settings.json is an operator decision (see heredoc-hook-proposal.md).

A heredoc is `<<` or `<<-` followed by a delimiter word (optionally quoted), with a line later in the command
that is exactly that word. Requiring the terminator line keeps `1 << 3`, `git commit -m "a << b"` and `<<<`
here-strings out of scope. Fail-open: any parse error allows the call.

Claude Code contract: stdin {"tool_name","tool_input":{"command"}}; deny with exit 0 and
{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":...}}.
    python3 no-heredoc-hook.py            (hook mode, reads stdin)
    python3 no-heredoc-hook.py --self-test
"""
import json
import re
import sys

START = re.compile(r"(?<!<)<<-?[ \t]*(['\"]?)([A-Za-z_][A-Za-z0-9_]*)\1(?!<)")
REASON = ("A shell heredoc is not allowed (CT27: a multi-line program is a file, then a run). Write the file with the "
          "Write tool under the scratchpad, then run it.")


def has_heredoc(command):
    lines = command.split("\n")
    for index, line in enumerate(lines):
        for match in START.finditer(line):
            word = match.group(2)
            if any(later.strip() == word for later in lines[index + 1:]):
                return True
    return False


def decide(payload):
    if payload.get("tool_name") != "Bash":
        return None
    command = (payload.get("tool_input") or {}).get("command") or ""
    return REASON if has_heredoc(command) else None


def self_test():
    heredoc = "python3 - <<'EOF'\nprint(1)\nEOF"
    cases = [
        (heredoc, True),
        ("cat > f.txt <<EOF\nx\nEOF\n", True),
        ("cat <<-END\n\tx\n\tEND", True),
        ("echo $((1 << 3))", False),
        ("git commit -m \"a << b\"", False),
        ("grep x <<< \"$var\"", False),
        ("cat <<EOF with no terminator line", False),
        ("ls -la", False),
    ]
    for command, want in cases:
        assert has_heredoc(command) is want, (command, want)
    assert decide({"tool_name": "Bash", "tool_input": {"command": heredoc}}) == REASON
    assert decide({"tool_name": "Read", "tool_input": {"command": heredoc}}) is None
    assert decide({"tool_name": "Bash", "tool_input": {}}) is None
    print("no-heredoc-hook self-test OK")


def main():
    if "--self-test" in sys.argv[1:]:
        self_test()
        return 0
    try:
        reason = decide(json.load(sys.stdin))
    except (ValueError, OSError):
        return 0
    if reason:
        print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse",
                                                 "permissionDecision": "deny", "permissionDecisionReason": reason}}))
    return 0


if __name__ == "__main__":
    sys.exit(main())
