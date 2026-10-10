---
id: proof-phk-red-first
title: "PHK red-first receipt"
type: proof-pack
status: active
owner: "@track-phk"
phase: implementation
tags: [phk, hook, ct27, red-first]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  The pipe-before-commit hook rule: self-test red on the old code, green after, hook-mode check, and the transcript sweep.
---
# PHK - refuse a pipe before git commit / git push (red first)

Control for AGENT-HEREDOC 2026-10-10 (the pipe shape, CT27). Built into `tools/hooks/no-heredoc.py` (one file: same
wiring in `.claude/settings.json`, same payload contract, same `--self-test` already run by `check-docs.py`).

## Red
The 15 pipe cases were added to `--self-test` before the code. Run on the old code:

    python3 tools/hooks/no-heredoc.py --self-test
    exit=1  NameError: name 'pipe_before_commit' is not defined

## Green
After `pipe_before_commit` and `PIPE_REASON`:

    python3 tools/hooks/no-heredoc.py --self-test
    exit=0  no-heredoc hook self-test OK

Cases: the exact leader command, `a | tail -1 && git push`, `git -C x commit` refused; `set -o pipefail;` and
`set -euo pipefail` allowed; quoted `|` in a commit message and in a grep pattern allowed; `;`, newline and `||` end the
chain; `a > f 2>&1 && git commit` allowed; `$(a | b)` allowed; a pipe with no later commit allowed; an unclosed quote allowed.

## Hook mode, end to end
Payload on stdin (script `scratch/phk/sweep.py`): the leader command gave exit 0 and stdout
`{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": ...}}`
(same shape as the heredoc deny). `ls | wc -l && git status` gave empty stdout. Invalid JSON gave exit 0, empty stdout.

## Sweep (read only)
`.agents/log/3122f106.jsonl` holds no `command` fields (2905 session-log records, none with a command), so it could not
be swept. The Claude transcripts for this project (`~/.claude/projects/-Users-mallalieut-projects-CFD-Workbench/*.jsonl`,
8 files) hold 10398 `command` values (8630 contain `| `); the rule refuses 141 (some commands appear twice in the
transcripts, so this is an upper bound on distinct commands). Typical hits are `... | tail -1 && git add ... && git commit`
and `git status --short | head -3 && git commit`. The rule is as strict as the brief says: a harmless display pipe
(`| head`) before a commit also refuses; the fix is to move the display step after `;` or use `set -o pipefail`.
