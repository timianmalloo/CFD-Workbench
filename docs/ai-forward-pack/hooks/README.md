# Hooks — the controls that run at the tool seam

Prose that says "check whether you already have it" is a memoir (`continuous-improvement.md` CI6). The
hooks here are the same rule as a **control**: a host runs them at a fixed lifecycle point regardless
of what the model decides.

| File | Host | Deploys to | Purpose |
|---|---|---|---|
| `reread-guard.py` | Claude Code, Copilot CLI, Grok Build, Antigravity | `docs/ai-forward-pack/hooks/reread-guard.py` | Counts identical reads per turn; on the third, and on any paged tool output viewed whole, adds a warning to the model's context. Warns, never blocks (a real third read exists). Fail-open on every error path. `--host claude\|copilot\|grok\|agy`. |
| `copilot.ai-forward-hooks.json` | Copilot CLI | `.github/hooks/ai-forward.json` | Copilot's hook config (`version: 1`, camelCase events, `bash`/`powershell` per platform, `timeoutSec`). Loaded from the repo automatically. Personal alternative: `~/.copilot/hooks/`. |
| `grok.ai-forward-hooks.json` | Grok Build | `.grok/hooks/ai-forward.json` | Grok's hook config (PascalCase events, `matcher: Read` which aliases to `read_file`). Project hooks require folder trust (`/hooks-trust` or `--trust`). |
| `agy.ai-forward-hooks.json` | Antigravity (`agy`) | `.agents/hooks.json` | Antigravity hook config (`PreToolUse` on `view_file` -> `reread-guard.py --host agy`; `PreInvocation` -> `session-start.py --host agy`). |
| `session-start.py` | Claude Code, Grok Build, Antigravity | `docs/ai-forward-pack/hooks/session-start.py` | Runs on session start (`SessionStart`/`SubagentStart` or `PreInvocation` invocationNum 1): records the audit start marker at the session seam (`audit-log.py start`, keyed to `$AGENT_SESSION` when the harness environment carries it, else a harness slot an `append` uses only when it has no marker of its own — `duration_source: session-start-hook`). Prints nothing; exits 0 on every path. Closes DC-190. Accepts Claude `session_id`, Grok `sessionId`, and Antigravity `conversationId`. Copilot CLI is not wired: its session-start event was not verified. |
| `claude-code.settings.hooks.json` | Claude Code | merge into `.claude/settings.json` | The `hooks` object for `PreToolUse` (matcher `Read`), `UserPromptSubmit`, `SessionStart` and `SubagentStart`. Committed project settings run in sub-agents too. |

**Contracts these are written to** (established from the hosts' own documentation and a captured event
stream, not assumed — class RIG-D): Claude Code hooks receive `{"hook_event_name","session_id",
"tool_name","tool_input"}` on stdin, and exit 0 with `hookSpecificOutput.systemMessage` to warn (exit 2
would block). Copilot CLI hooks receive `{"sessionId","toolName","toolArgs"}` and return
`{"additionalContext": …}`; on `preToolUse` any non-zero exit other than 2 **denies** the call, so the
guard exits 0 on every path including its own failures.

**Windows:** the commands above say `python`; on Linux/macOS use `python3` (INSTALL §0). The Copilot
config carries both forms; edit the Claude Code command to the interpreter your machine has.

Measured origin: the profiled TheTerrace session viewed `public.html` four times in three minutes,
a 43 KB paged output whole twice, and one sub-agent read the same mockup six times — none of it errored.
