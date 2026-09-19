# Grok Build — AI-Forward Pack surface

This repository installs the AI-Forward Pack. Grok Build already loads root `AGENTS.md` as project rules (the constitution). This file is only the **Grok path map** so you do not follow Copilot-only or Claude-only locations. Do not paste knowledge docs here — that would attach them on every turn (defect class CTX-B).

## Skills

Project skills live in `.grok/skills/<name>/SKILL.md`. Invoke as `/specify`, `/implement`, `/design-slice`, `/optimize-graph`, and the other pack skills. Stage files are `reference/` beside each `SKILL.md`; read them at the stage, never by re-invoking the skill.

The pack `/implement` **overrides** Grok's bundled implement skill in this repository. That is intended: a pack-installed repo runs the Rigor Protocol implement loop, not the bundled one.

Claude compatibility (default on) also discovers `.claude/skills/`. Same files; a same-named `.grok/skills/` entry wins.

## Knowledge

Read the shared copies at `.claude/knowledge/<name>.md`. Where `AGENTS.md` cites `.github/instructions/<name>.instructions.md`, that is the Copilot wrap of the same document — do not treat the wrap as a second source. Docs with `load: skill` or `load: reference` are also under `.github/knowledge/`; prefer `.claude/knowledge/`.

## Personas

Spawn with `spawn_subagent`. `subagent_type` is the persona `name` (`orchestrator`, `test-architect`, `the-simplifier`, `domain-researcher`, …). Definitions: `.grok/agents/<name>.md`. Author in Peer Mode, review in Adversary Mode; the author never clears its own hard veto.

Grok's built-in `explore`, `plan`, and `general-purpose` types remain available.

## Hooks

`.grok/hooks/ai-forward.json` wires the re-read guard (CTX-D) and the session-start audit marker (AL4a). Project hooks run only after folder trust (`/hooks-trust` or `--trust`).

## Scripts

On Windows use `python` or `py -3`; on Linux/macOS use `python3`. `docs/ai-forward-pack/scripts/pack-doctor.py` names the working form for this machine.
