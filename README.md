# CFD-Workbench

A cross-platform client application for hydrofoil design and simulation, targeting
**Windows and macOS**.

The project is at the repository setup stage. The application framework, geometry
tools, and simulation backend will be selected during specification and architecture
work. There is no runnable application yet.

## Development workflow

This repository includes the [AI-Forward Pack](https://timianmalloo.github.io/ai-forward/),
revision **73** (`2026.09.19.1`), with skills, personas, engineering guidance,
documentation tools, and integrations for Codex, Claude Code, GitHub Copilot, Grok,
and Antigravity.

- [Project instructions](AGENTS.md)
- [Pack overview](docs/ai-forward-pack/OVERVIEW.md)
- [Installation guide](docs/ai-forward-pack/INSTALL.md)
- [Installation inventory](docs/ai-forward-pack/install-report.md)
- [Codex guide](docs/ai-forward-pack/codex.md)
- [Docs Explorer](docs/index.html) — open locally; the first content workflow creates its index.

Begin with domain knowledge collection and a specification of the first hydrofoil
design and simulation workflow. In Codex, use `$collectknowledge` and `$specify`.
Use `$adopt` when bringing existing implementation or documentation into the project.

## Repository checks

The tooling requires Python 3.8 or later and uses the standard library. From the
repository root on macOS:

```sh
python3 tools/check-docs.py
python3 docs/ai-forward-pack/scripts/pack-doctor.py
```

On Windows, use `py -3` or `python` in place of `python3`.

After cloning, initialize the pack's local Git coordination hooks and merge drivers:

```sh
python3 docs/ai-forward-pack/scripts/coord-core.py install
```

Run this once per clone in the primary checkout. Linked worktrees inherit it.
The committed regeneration commands require Python 3 to be available as `python`
on `PATH`; activate a Python virtual environment if needed. If regenerating the
artifact registry, retain portable interpreter commands instead of machine-specific paths.
The `docs-health` GitHub Actions workflow checks documentation and pack readiness
on relevant pull requests, pushes to `main`, and manual runs.
