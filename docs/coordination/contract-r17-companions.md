---
id: coordination-r17-companions
title: Exact companion assignment for evaluator version 2 and native store rulings
type: plan
status: accepted
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, foildsl, identity, persistence]
links:
  - {to: coordination-application-build, rel: depends-on}
  - {to: coordination-contract-b-core, rel: relates-to}
  - {to: spec-foildsl, rel: depends-on}
  - {to: design-application-contracts, rel: depends-on}
  - {to: review-application-core, rel: relates-to}
review-by: 2026-10-23
summary: Root owns 30 exact companion paths for Owner Rulings 17–19 while the serial core author finishes a disjoint store and projection checkpoint.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: review-application-core, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: spec-foildsl, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: coordination-application-build, on: 2026-09-23, reason: "Ruling 21 conditional native adapter route and UI-T4 preflight require consumer review" }
---

# R17–19 · root companion assignment

**Assigned, not accepted implementation.** Owner Rulings 17–19 in the
[canonical register](../notes/rulings.md) resolve the degree-spline identity,
all-domain query-feasibility, and native overwrite/telemetry policy. Root session
`cfd-application-20260923` owns this document/specification/fixture repair in
clean `feature/application-foundation`, base
`6e855a4e021d51267899b09512901a10cf86242b`. The Coordinator granted
`WI-APPLICATION-R17-COMPANIONS` exact-path claims with a 3600-second lease on
2026-09-23; renew before expiry rather than assuming a held path stays held.
The active core author owns the disjoint 18-path Ruling 13 lease in
`feature/application-core` and continues the `/1` store/Ruling 16 checkpoint.
Root writes no production Core/Persistence source. The companion commit must
be reviewed and joined before the worker begins a dedicated `/2` R17/R18 code
continuation. Neither branch joins a partial production core.

## Exact authored and derived paths (30)

No wildcard or neighboring directory is granted. A new affected path needs a
typed seam and an exact claim before writing. `docs/docs-index.js` and official
audit/change render outputs are generated/register exceptions at the join.

| Area | Exact root-owned paths |
|---|---|
| Normative source and rendered view (2) | `docs/specs/foildsl.md`, `docs/specs/foildsl.html` |
| Architecture, design and ADR (4) | `docs/architecture/application.md`, `docs/design/application-contracts.md`, `docs/design/application-foundation.md`, `docs/adr/0004-application-project-contract.md` |
| Existing valid examples (8) | `docs/examples/foildsl/foil-basic.foil`, `docs/examples/foildsl/foil-independent-bases.foil`, `docs/examples/foildsl/foil-precision.foil`, `docs/examples/foildsl/foil-assertions.foil`, `docs/examples/foildsl/foil-comment.foil`, `docs/examples/foildsl/foil-trailing-only.foil`, `docs/examples/foildsl/foil-leading-only.foil`, `docs/examples/foildsl/section-basic.foil` |
| Existing invalid examples (6) | `docs/examples/foildsl/invalid-version.foil`, `docs/examples/foildsl/invalid-legacy-draft.foil`, `docs/examples/foildsl/invalid-syntax.foil`, `docs/examples/foildsl/invalid-geometry.foil`, `docs/examples/foildsl/invalid-units.foil`, `docs/examples/foildsl/invalid-reference.foil` |
| New refusal and example index (3) | `docs/examples/foildsl/invalid-evaluator.foil`, `docs/examples/foildsl/cases.json`, `docs/examples/foildsl/README.md` |
| Current illustrative UI contract (2) | `docs/mockups/workbench-v7.html`, `docs/mockups/workbench-v7.md` |
| B0 fixture and proof (2) | `tools/spikes/ApplicationContracts/Program.cs`, `docs/proof/application-contracts.md` |
| Independent review and mockup gate (2) | `docs/reviews/application-core.md`, `tools/check-foildsl.mjs` |
| B0 recount validator (1) | `tools/recount-application-contracts.py` |

The supplied/v3 references, earlier mockups, historical native spike, original
audit/receipt payloads, `docs/examples/foildsl/reference-probe.cjs`, and
`docs/examples/foildsl/reference-observations.json` remain immutable evidence.
An existing invalid fixture may deliberately retain `/1` only if the case
index labels it as explicit legacy/refusal evidence; an apparently valid
current example cannot quietly remain on the unsupported evaluator.

## Contract and handback

- **R17:** Keep the FoilDSL 4.0 grammar and section 6 degree-curve semantics.
  Version `cfdw-cv/2` hashes parsed binary64 degree twist CVs, with the
  evaluator/version boundary explicit; no pre-rounded radian CV identity,
  silent `/1` adoption, historical source rewrite or cache transfer. Reconcile
  section 8, exact canonical object/goldens, current examples, B0 fixture and
  mockup's illustrative evaluator claims. Preserve `/1` evidence as labelled
  history and add a stable unsupported-evaluator refusal case.
- **R18:** State that a Certified M1 assessment proves deterministic
  arithmetic/algorithmic feasibility for every supported finite-binary64
  `PointAt`/`SectionAt` query, or refuses before certification. Keep elapsed
  deadlines and cancellation distinct from that resource proof. Architecture
  and foundation design must no longer imply that a finite sample grid or a
  one-second cooperative poll alone proves all-query completion.
- **R19:** Describe one reserved directory-relative cooperative overwrite
  claim, exact target/content checks, owned cleanup before final directory
  durability, known-publication versus durability-confirmed outcomes, and
  measured native I/O events in an injected existing session ring. Do not
  claim exclusion of noncooperating writers or Windows runtime proof.
- **Proof:** execute the actual B0 recount and FoilDSL mockup gate after
  companion changes, refresh current fixture/source fingerprints, and append
  new `/2` evidence without rewriting historical `/1` outcomes. Render the
  spec HTML from the changed Markdown and verify content/links. Run
  `tools/check-docs.py`; report exact command exits, case counts and remaining
  unsupported behavior. Root cannot clear its own normative/scientific veto:
  Owner independently reviews the companion commit and root later reviews
  the production worker's executable `/2` result.
- **Recount update:** the B0 C# contract gains four named Ruling 17 checks.
  Update the validator's expected C# count from 89 to 93 and require all four
  names. Keep its 42 Python cases, 2,505 vectors and existing required names;
  run the actual recount after the candidate changes. A green B0 program alone
  is not a passed recount.

Before running an unfamiliar local companion command, inventory its exact
path with `rg --files` and inspect the script's advertised arguments/dispatch.
Do not construct a likely filename or subcommand, assume custom `--help` is
read-only, or batch a dependent read with the initial inventory. The known
recount entry is `tools/recount-application-contracts.py`; the coordination
leader readback is `coord leader who`.

At handback root supplies clean branch/HEAD, 30-path authored diff inventory
plus official metadata exceptions, source fingerprints and retained gate
receipts. The Coordinator joins the reviewed companion through the supported
conductor and gives the resulting commit to the core author **after** its
current store/Ruling 16 checkpoint is clean. C remains held until complete B
and independent scientific, Data, native, accessibility and platform gates.

The review-document lease remains released through **both** destination joins:
root companion into coordination, then the reviewed coordination HEAD into the
core worktree. Before each destination commit, read `coord check` for staged
paths. Do not reclaim `docs/reviews/application-core.md` until the core author
reports a clean post-conductor HEAD and final gate receipt. A failed merge
commit stops the next conductor call: run dependent commit, continuation and
worker dispatch as separate exit-checked steps, with HEAD/MERGE_HEAD/staged
state and owned-child absence read back after any refusal.
