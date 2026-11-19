# CFD-Workbench

A cross-platform client application for hydrofoil design and simulation, targeting
**Windows and macOS**.

## Current status (`main`)

The application is built on **C#/.NET 10 and Avalonia** (`CFDWorkbench.slnx`), as
accepted in [ADR-0003](docs/adr/0003-application-stack.md). **Windows** is the current
platform of record while macOS compatibility is maintained across the shared core,
analysis, CLI, and CI checks.

Waves 1–4 have landed on `main` across six projects:

- **`src/CfdWorkbench.Core`** — FoilDSL 4.0/4.1 parser, exact geometry certificate
  (`cfdw-cv/2`), parametric planform/dihedral/twist/thickness distribution curves,
  station profiles (promote, remove, match-blend, `.dat` import, provenance), and the
  append-only native project envelope (`cfdw-project-1` / `cfdw-project-2`, 32 MB cap).
- **`src/CfdWorkbench.Persistence`** — Atomic, crash-safe project store and local
  *My sections* library.
- **`src/CfdWorkbench.Analysis`** — Local in-process VLM + strip coupler, wing
  estimator tier, hashed ITTC water table, and Compare / Discrepancy records
  (including dimensional ΔL and ΔDᵢ when reference quantities differ).
- **`src/CfdWorkbench.Export`** — Export Slice A (STL mesh and Selig/Lednicer `.dat`
  section exports).
- **`src/CfdWorkbench.Desktop`** — Avalonia workbench shell (Plan, Elevation, 3D, and
  Section views, Browser, Properties, Analysis, Compare, Catalog, Save-section, and
  Export dialogs).
- **`src/CfdWorkbench.Cli`** — Headless inspection, analysis, and export CLI.

The high-fidelity CFD simulation backend (Area 5) and the NURBS/STEP/3DM CAD export
kernel (Area 7) remain unselected and gated on operator review.

## Building and testing

Build once and run all four .NET test harnesses (Core, Analysis, Desktop, CLI) plus any
Python tests from Git Bash (Windows) or bash (macOS):

```sh
bash tools/run-tests.sh
```

On headless machines or CI without a display, pass `--no-desktop` to run Core, Analysis,
and CLI (`tools/run-tests.sh --no-desktop`). You can also run `dotnet test CFDWorkbench.slnx`.

## Repository checks

The tooling requires Python 3.8 or later and uses the standard library. From the
repository root on Windows:

```sh
python tools/check-docs.py
python docs/ai-forward-pack/scripts/pack-doctor.py
```

On macOS/Linux, use `python3` in place of `python`.

After cloning, initialize the pack's local Git coordination hooks and merge drivers
once in the primary checkout (linked worktrees inherit them):

```sh
python docs/ai-forward-pack/scripts/coord-core.py install
```

The committed regeneration commands require Python 3 to be available as `python` on
`PATH`; activate a Python virtual environment if needed. CI runs
`.github/workflows/tests.yml` (`Core`, `Analysis`, `Cli`, and `tools/check-docs.py`)
on pushes to `main`.

## Review the product

- [Interactive workbench mockup v7 — section scope and design alternatives](docs/mockups/workbench-v7.html) ([hub](docs/mockups/workbench-v7.md)) — open directly in a browser; no build or network required. Earlier review artifacts: [v6](docs/mockups/workbench-v6.md) · [v5](docs/mockups/workbench-v5.md) · [v4](docs/mockups/workbench-v4.md) · [v3](docs/mockups/workbench-v3.md) · [v2](docs/mockups/workbench-v2.md) · [v1](docs/mockups/workbench-v1.md) · [prototype](docs/mockups/workbench.html).
- [Product specification v1.5 (build basis) — HTML](docs/specs/cfd-workbench-v1.html) · [Markdown](docs/specs/cfd-workbench-v1.md); the 0.2 draft it supersedes: [HTML](docs/specs/cfd-workbench.html) · [Markdown](docs/specs/cfd-workbench.md)
- [Application architecture](docs/architecture/application.md) · [Accepted stack decision](docs/adr/0003-application-stack.md) · [Authoring and native document contracts](docs/design/application-contracts.md) · [Independent contract review](docs/reviews/application-contracts.md)
- [Design language](DESIGN.md) and [visual token catalog](docs/mockups/design-language.html)
- [CFD-Bench and proposal grounding](docs/knowledge/cfd-workbench-grounding.md)
- [Independent specification review](docs/reviews/specification-gate.md)
- [Interface review and proof](docs/reviews/ui-workbench.md)

The modeling contract uses **one parametric surface definition** viewed through five
distribution curves and editable station profiles. Manual edits detach the starting
recipe while preserving an explicit parametric model.

The specification HTML is generated from the complete Markdown by
`tools/render-spec.mjs`. Its documentation-only dependencies are `marked` 17.0.5 and
`@viz-js/viz` 3.25.0. Run `node tools/render-spec.mjs` with those installed, or pass
the path of a directory containing their installed Node modules. Set
`SPEC_NAME=cfd-workbench-v1` to render the current product specification (the default
selects the historical document). Neither dependency is needed to read the committed
HTML.

## Development workflow

This repository includes the [AI-Forward Pack](https://timianmalloo.github.io/ai-forward/),
revision **120** (`2026.11.19.1`), with skills, personas, engineering guidance,
documentation tools, and integrations for Codex, Claude Code, GitHub Copilot, Grok,
and Antigravity.

- [Project instructions](AGENTS.md)
- [Domain-expert roster](docs/domain-experts.md) — the pack's twenty-three general lenses plus **seven** hydrofoil domain experts (`.claude/agents/`), derived from the [knowledge base](docs/knowledge/hydrofoil-workbench/index.md). <!-- cfd-workbench domain experts -->
- [Pack overview](docs/ai-forward-pack/OVERVIEW.md)
- [Installation guide](docs/ai-forward-pack/INSTALL.md)
- [Installation inventory](docs/ai-forward-pack/install-report.md)
- [Codex guide](docs/ai-forward-pack/codex.md)
- [Docs Explorer](docs/index.html) — open locally to browse the specification, design and evidence.

Follow the active coordination plan under `docs/coordination/` and its isolated
worktree assignments. Accepted architecture and `/design-slice` contracts govern
`/implement` increments; further load-bearing decisions use `/define-architecture`.

### FoilDSL authoring reference

[Review workbench v7](docs/mockups/workbench-v7.html) · [Product specification 1.5](docs/specs/cfd-workbench-v1.html)
· [Normative FoilDSL 4.0](docs/specs/foildsl.md) · [Reference reconciliation](docs/notes/foildsl-reconciliation.md)
· [Review walkthrough and prototype limits](docs/mockups/workbench-v7.md)
· [Section and decision workflow](docs/notes/design-iteration.md).

The source files in `reference/` are preserved originals. FoilDSL and mockup v7 form
the build basis; no high-fidelity CFD simulation backend or CAD export kernel is
selected.
