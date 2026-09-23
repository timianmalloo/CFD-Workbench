# CFD-Workbench

A cross-platform client application for hydrofoil design and simulation, targeting
**Windows and macOS**.

The project is implementing the reviewed specification and mockups.
The first offline milestone uses C#/.NET 10 and Avalonia, with a shared core for
the native workbench and CLI. The shared core has passed its bounded independent
gate. A buildable desktop and runnable CLI **candidate** exists on
`feature/application-native-adapters` at checkpoint `de105f0`.
[Ruling 25](docs/notes/rulings.md) keeps that candidate isolated and the native
M1 gate blocked pending actual rendered and accessibility evidence. The
simulation backend and export geometry kernel remain unselected.

On macOS, the candidate branch's argument-free
`python3 tools/verify-application-adapters.py` builds and tests the shared-core
adapters and publishes self-contained macOS ARM64 and Windows x64 development
packages in a fresh local scratch directory. The verifier has not run its
checks on a Windows host. It prints a receipt path; that JSON's `packages`
map locates the macOS `CFD Workbench.app` and Windows portable folder, while
its `publish` map locates the CLI apphosts. These are local development
candidates. The CLI, controller, recovery and package contents
have bounded independent evidence in the
[native adapter review](docs/reviews/ui-application-native.md). The final
11-step source and package gate passed, including real apphost XAML startup.
The supported native inspection tool returned `cgWindowNotFound` on the
running review app, so rendered layout, keyboard and AX behavior remain
unverified. Minimum-window behavior, Windows runtime, signing and distribution
also remain open. A successful package build is not a release or a validated
solver.

## Review the product

- [Interactive workbench mockup v7 — section scope and design alternatives](docs/mockups/workbench-v7.html) ([hub](docs/mockups/workbench-v7.md)) — open directly in a browser; no build or network required. Earlier review artifacts: [v6](docs/mockups/workbench-v6.md) · [v5](docs/mockups/workbench-v5.md) · [v4](docs/mockups/workbench-v4.md) · [v3](docs/mockups/workbench-v3.md) · [v2](docs/mockups/workbench-v2.md) · [v1](docs/mockups/workbench-v1.md) · [prototype](docs/mockups/workbench.html).
- [Product specification v1.5 (build basis) — HTML](docs/specs/cfd-workbench-v1.html) · [Markdown](docs/specs/cfd-workbench-v1.md); the 0.2 draft it supersedes: [HTML](docs/specs/cfd-workbench.html) · [Markdown](docs/specs/cfd-workbench.md)
- [Application architecture](docs/architecture/application.md) · [Accepted stack decision](docs/adr/0003-application-stack.md) · [Authoring and native document contracts](docs/design/application-contracts.md) · [Independent contract review](docs/reviews/application-contracts.md)
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
or pass the path of a directory containing their installed Node modules. Set
`SPEC_NAME=cfd-workbench-v1` to render the current product specification (the
default selects the historical document). Neither
dependency is needed to read the committed HTML; this does not select the app stack.

## Development workflow

This repository includes the [AI-Forward Pack](https://timianmalloo.github.io/ai-forward/),
revision **92** (`2026.09.21.3`), with skills, personas, engineering guidance,
documentation tools, and integrations for Codex, Claude Code, GitHub Copilot, Grok,
and Antigravity.

- [Project instructions](AGENTS.md)
- [Domain-expert roster](docs/domain-experts.md) — the pack's twenty-three general lenses plus **seven** hydrofoil domain experts (`.claude/agents/`), derived from the [knowledge base](docs/knowledge/hydrofoil-workbench/index.md). <!-- cfd-workbench domain experts -->
- [Pack overview](docs/ai-forward-pack/OVERVIEW.md)
- [Installation guide](docs/ai-forward-pack/INSTALL.md)
- [Installation inventory](docs/ai-forward-pack/install-report.md)
- [Codex guide](docs/ai-forward-pack/codex.md)
- [Docs Explorer](docs/index.html) — open locally to browse the specification, design and evidence.

Follow the [coordination plan](docs/coordination/application-build.md) and its
isolated worktree assignments. The accepted architecture and `$design-slice`
contracts govern `$implement` increments. The shared core has been joined;
the native GUI and CLI adapter candidate is under independent review. Live
workflow proof and the remaining platform gates still govern delivery.
Further load-bearing decisions use `$define-architecture`.

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


### FoilDSL authoring proposal

[Review workbench v7](docs/mockups/workbench-v7.html) · [Product specification 1.5](docs/specs/cfd-workbench-v1.html)
· [Normative FoilDSL 4.0](docs/specs/foildsl.md) · [Reference reconciliation](docs/notes/foildsl-reconciliation.md)
· [Review walkthrough and prototype limits](docs/mockups/workbench-v7.md)
· [Section and decision workflow](docs/notes/design-iteration.md).

The source files in `reference/` are preserved originals. FoilDSL and mockup v7
form the current build basis. The separate architecture decision selects the
application stack; prototype computations remain illustrative, and no simulation
backend is selected.
