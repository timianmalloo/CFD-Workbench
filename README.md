# CFD-Workbench

A cross-platform client application for hydrofoil design and simulation, targeting
**Windows and macOS**.

The project is in specification and interface design. The application framework,
geometry implementation, and simulation backend remain unselected. There is no
runnable production application yet.

## Review the product

- [Interactive workbench mockup v4 — CAD editing views](docs/mockups/workbench-v4.html) ([hub](docs/mockups/workbench-v4.md)) — open directly in a browser; no build or network required. Earlier review artifacts: [v3](docs/mockups/workbench-v3.md) · [v2](docs/mockups/workbench-v2.md) · [v1](docs/mockups/workbench-v1.md) · [prototype](docs/mockups/workbench.html).
- [Product specification v1.2 (build basis) — HTML](docs/specs/cfd-workbench-v1.html) · [Markdown](docs/specs/cfd-workbench-v1.md); the 0.2 draft it supersedes: [HTML](docs/specs/cfd-workbench.html) · [Markdown](docs/specs/cfd-workbench.md)
- [Design language](DESIGN.md) and [visual token catalog](docs/mockups/design-language.html)
- [CFD-Bench and proposal grounding](docs/knowledge/cfd-workbench-grounding.md)
- [Independent specification review](docs/reviews/specification-gate.md)
- [Interface review and proof](docs/reviews/ui-workbench.md)

The modeling contract uses **one parametric surface definition** viewed through
five distribution curves and editable station profiles. Manual edits detach the
starting recipe while preserving an explicit parametric model. Analysis in the
mockup is illustrative; it is not numerical validation or a working solver.

The specification HTML is generated from the complete Markdown by
`tools/render-spec.mjs`. Its documentation-only dependencies are `marked` 17.0.5
and `@viz-js/viz` 3.25.0. Run `node tools/render-spec.mjs` with those installed,
or pass the path of a directory containing their installed Node modules. Neither
dependency is needed to read the committed HTML; this does not select the app stack.

## Development workflow

This repository includes the [AI-Forward Pack](https://timianmalloo.github.io/ai-forward/),
revision **73** (`2026.09.19.1`), with skills, personas, engineering guidance,
documentation tools, and integrations for Codex, Claude Code, GitHub Copilot, Grok,
and Antigravity.

- [Project instructions](AGENTS.md)
- [Domain-expert roster](docs/domain-experts.md) — the pack's twenty-three general lenses plus **seven** hydrofoil domain experts (`.claude/agents/`), derived from the [knowledge base](docs/knowledge/hydrofoil-workbench/index.md). <!-- cfd-workbench domain experts -->
- [Pack overview](docs/ai-forward-pack/OVERVIEW.md)
- [Installation guide](docs/ai-forward-pack/INSTALL.md)
- [Installation inventory](docs/ai-forward-pack/install-report.md)
- [Codex guide](docs/ai-forward-pack/codex.md)
- [Docs Explorer](docs/index.html) — open locally to browse the specification, design and evidence.

Iterate the mockup and specification first, then use `$define-architecture` for the
cross-platform geometry, file and analysis boundaries. Use `$design-slice` before
implementing each accepted surface.

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
