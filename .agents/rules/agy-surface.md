# Antigravity (agy) — AI-Forward Pack surface

This repository installs the AI-Forward Pack. Antigravity automatically loads root `AGENTS.md` as project rules (the constitution). This file is only the **Antigravity path map** so you do not follow Copilot-only or Claude-only locations. Do not paste knowledge docs here — that would attach them on every turn (defect class CTX-B).

## Skills

Project skills live in `.agents/skills/<name>/SKILL.md` (and are discoverable via `.agents/skills.json`). Invoke as `/specify`, `/implement`, `/design-slice`, `/optimize-graph`, and the other pack skills. Stage files are `reference/` beside each `SKILL.md`; read them at the stage via `view_file`, never by re-invoking the skill.

The pack `/implement` **overrides** any generic implementation behavior. In a pack-installed repo, follow the Rigor Protocol implement loop.

## Knowledge

Read the shared copies at `.claude/knowledge/<name>.md`. Where `AGENTS.md` cites `.github/instructions/<name>.instructions.md`, that is the Copilot wrap of the same document — do not treat the wrap as a second source. Docs with `load: skill` or `load: reference` are also under `.github/knowledge/`; prefer `.claude/knowledge/`.

## Personas & Subagents

To convene the persona council:
1. **Inline turn (default)**: For standard turns, enact the peer/adversary perspectives inline with explicit PASS/BLOCK ratings.
2. **Subagent `self`**: Call `invoke_subagent(TypeName="self", Role="<Persona>", Prompt="...")`. Subagent `self` inherits repo rules (`AGENTS.md`) and tools.
3. **Dynamic subagents (`define_subagent`)**: For persistent specialists, register the persona via `define_subagent(name="...", description="...", system_prompt="...")` reading prompts from `.claude/agents/<name>.md`.

Author in Peer Mode, review in Adversary Mode; the author never clears its own hard veto.

## Hooks

`.agents/hooks.json` wires the re-read guard (`reread-guard.py` on `PreToolUse` for `view_file`, defect class CTX-D) and the session-start audit marker (`session-start.py` on `PreInvocation`, AL4a).

## Scripts

On Windows use `python` or `py -3`; on Linux/macOS use `python3`. `docs/ai-forward-pack/scripts/pack-doctor.py` names the working form for this machine.
