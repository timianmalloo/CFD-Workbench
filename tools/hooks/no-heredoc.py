#!/usr/bin/env python3
"""PreToolUse hook for Claude Code: refuse a Bash command that contains a shell heredoc, or that pipes before a commit.

Second rule (AGENT-HEREDOC, 2026-10-10 "pipe shape", CT27), kept in this file because it shares the wiring, the
payload contract and the check-docs self-test: refuse a command where, in one `&&` chain, a pipeline (`|`, not `||`)
precedes `git commit` or `git push`, unless `set -o pipefail` came earlier. Quote aware (shlex); `;`, newline, `||`
and `&` end a chain; a pipe inside `$( ... )` or parentheses is out of scope and allowed; an untokenizable command
is allowed.

AGENT-HEREDOC control (docs/lessons/defect-classes.md, CT27: a multi-line program is a file, then a run).
Installed by Ruling 104 (2026-10-06) in .claude/settings.json; proposal: docs/proof/round-oct05-lessons/heredoc-hook-proposal.md.

A heredoc is `<<` or `<<-` followed by a delimiter word (optionally quoted), with a line later in the command
that is exactly that word. Requiring the terminator line keeps `1 << 3`, `git commit -m "a << b"` and `<<<`
here-strings out of scope. Fail-open: any parse error allows the call.

Claude Code contract: stdin {"tool_name","tool_input":{"command"}}; deny with exit 0 and
{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":...}}.
    python3 tools/hooks/no-heredoc.py            (hook mode, reads stdin)
    python3 tools/hooks/no-heredoc.py --self-test
"""
import json
import re
import shlex
import sys

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

START = re.compile(r"(?<!<)<<-?[ \t]*(['\"]?)([A-Za-z_][A-Za-z0-9_]*)\1(?!<)")
REASON = ("A shell heredoc is not allowed (CT27: a multi-line program is a file, then a run). Write the file with the "
          "Write tool under the scratchpad, then run it.")
PIPE_REASON = ("A pipeline before `git commit` or `git push` in one && chain is not allowed (CT27: a gate's exit "
               "status is never behind a pipe; AGENT-HEREDOC 2026-10-10, the pipe shape): the pipe hides the failure "
               "and the commit proceeds. Redirect the step to a file and test its exit, or `set -o pipefail`.")


def has_heredoc(command):
    lines = command.split("\n")
    for index, line in enumerate(lines):
        for match in START.finditer(line):
            word = match.group(2)
            if any(later.strip() == word for later in lines[index + 1:]):
                return True
    return False


def _tokens(command):
    lexer = shlex.shlex(command, posix=True, punctuation_chars=";()<>|&\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    return list(lexer)


def _git_verb(tokens, i):
    """The git subcommand at tokens[i] == 'git', skipping -C/-c <value> and other options; else None."""
    j = i + 1
    while j < len(tokens):
        if tokens[j] in ("-C", "-c"):
            j += 2
        elif tokens[j].startswith("-"):
            j += 1
        else:
            return tokens[j]
    return None


def pipe_before_commit(command):
    """Return the offending verb ('commit' or 'push') when one `&&` chain pipes before it, else None.

    A chain ends at `;`, a newline, `||` or `&`. `set ... pipefail` earlier in the command lifts the rule.
    A pipe inside parentheses ($( ... ), subshells) is out of scope and allowed. A command shlex cannot tokenize
    is allowed. Pipe characters inside quotes are not pipes (the tokenizer strips the quoted word).
    """
    try:
        tokens = _tokens(command)
    except ValueError:
        return None
    piped = False
    pipefail = False
    depth = 0
    for i, token in enumerate(tokens):
        if token == "pipefail" and "set" in tokens[:i]:
            pipefail = True
        elif token == "(":
            depth += 1
        elif token == ")":
            depth = max(0, depth - 1)
        elif depth:
            continue
        elif token in (";", "||", "&") or set(token) == {"\n"}:
            piped = False
        elif token in ("|", "|&"):
            piped = not pipefail
        elif token == "git" and piped:
            verb = _git_verb(tokens, i)
            if verb in ("commit", "push"):
                return verb
    return None


def decide(payload):
    if payload.get("tool_name") != "Bash":
        return None
    command = (payload.get("tool_input") or {}).get("command") or ""
    if has_heredoc(command):
        return REASON
    return PIPE_REASON if pipe_before_commit(command) else None


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
    leader = "python3 rule_one.py r206.json | tail -1 && git add a b && git commit -m 'Ruling 206'"
    pipe_cases = [
        (leader, True),
        ("a | tail -1 && git push", True),
        ("a | tail -1 && git -C /x commit -m m", True),
        ("set -o pipefail; a | tail && git commit -m x", False),
        ("set -euo pipefail\na | tail && git commit -m x", False),
        ("git commit -m \"a | b\"", False),
        ("grep 'x\\|y' f && git commit -m m", False),
        ("a | head; git commit -m m", False),
        ("a | head\ngit commit -m m", False),
        ("git diff --cached --quiet || git commit -m m", False),
        ("a | b || git commit -m m", False),
        ("a > f 2>&1 && git commit -m m", False),
        ("x=$(a | b) && git commit -m m", False),
        ("a | tail -1 && echo done", False),
        ("echo 'unclosed | && git commit", False),
    ]
    for command, want in pipe_cases:
        assert (pipe_before_commit(command) is not None) is want, (command, want)
    assert decide({"tool_name": "Bash", "tool_input": {"command": leader}}) == PIPE_REASON
    assert decide({"tool_name": "Bash", "tool_input": {"command": heredoc}}) == REASON
    assert decide({"tool_name": "Read", "tool_input": {"command": heredoc}}) is None
    assert decide({"tool_name": "Bash", "tool_input": {}}) is None
    print("no-heredoc hook self-test OK")


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
