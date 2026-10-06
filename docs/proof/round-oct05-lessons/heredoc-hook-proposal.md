---
id: proof-round-oct05-heredoc-hook-proposal
title: "AGENT-HEREDOC hook proposal (operator decision)"
type: proof-pack
status: draft
owner: "@track-ci"
phase: implementation
tags: [round-oct05, hooks, heredoc, ct27, operator-decision]
links:
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  A PreToolUse hook that refuses Bash heredocs, with a tested draft script. Not installed: wiring a hook into
  .claude/settings.json changes harness configuration and is the operator's call.
---

# AGENT-HEREDOC hook proposal

**Decision for the operator:** install, or decline, a PreToolUse hook on `Bash` that refuses shell heredocs.
Nothing in `.claude/settings.json`, `.grok/hooks/`, `.github/hooks/` or `.agents/hooks.json` was changed.

## Why

CT27 (`.claude/knowledge/communication-and-task-discipline.md`) says a multi-line program is a file, then a run, never a
heredoc. Briefs repeated the rule. Sub-agents used a heredoc at least 7 times on 2026-10-05, and the coordinator used
them in the same session. A rule in prose did not hold, so the control must act at the tool seam (CI6).

## Draft

`docs/proof/round-oct05-lessons/no-heredoc-hook.py` (stdlib, fail-open, `--self-test` passes).

- Detects `<<` or `<<-` plus a delimiter word, optionally quoted, **and** a later line that is exactly the delimiter.
  `1 << 3`, `git commit -m "a << b"` and `<<<` here-strings are not refused.
- Denies with `permissionDecision: deny` and a reason that names the fix (Write the file under the scratchpad, then run it).

## Wiring, if approved

Add one entry to the `PreToolUse` list in `.claude/settings.json`, using the same toplevel-fallback command shape as the
other hooks (HOOK-CWD-RELATIVE), after moving the script to `docs/ai-forward-pack/hooks/` (or keeping it in `tools/hooks/`):

```json
{ "matcher": "Bash", "hooks": [ { "type": "command", "timeout": 10,
  "command": "h=tools/hooks/no-heredoc-hook.py; [ -f \"$h\" ] || h=\"$(git rev-parse --show-toplevel)/$h\"; python3 \"$h\"" } ] }
```

`tools/check-pack-hooks.py` already fails a hook command that names a pack script without the fallback. Wire the same
check for any script placed in `tools/hooks/`.

## Risks and limits

- **Sub-agents.** Whether hooks in the repo settings fire for sub-agent Bash calls is not verified here. Check with one
  planted heredoc in a sub-agent before relying on it. Label: Unverified.
- **Other hosts.** Grok, Copilot and agy use different payload shapes; this draft covers Claude Code only.
- **False positive.** A legitimate heredoc (for example feeding `git commit -F -`) is refused. The fix is a file, as CT27 says.
- **Evasion.** `printf` with embedded newlines and `echo -e` can still write a multi-line program. The hook enforces the
  named shape, not the intent.
