---
id: decision-foildsl-reconciliation
title: Reconcile FoilDSL v3 with the control-vertex workbench
type: decision-note
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [foildsl, reference, geometry, ux, reconciliation]
links:
  - {to: spec-foildsl, rel: documents}
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: decision-parametric-authority, rel: refines}
  - {to: control-vertex-workspace, rel: relates-to}
  - {to: kb-hw-parametric-curves-lofts-and-surfaces, rel: depends-on}
  - {to: kb-hw-file-formats-and-grammars, rel: depends-on}
review-by: 2026-12-22
summary: >-
  Compares the supplied FoilDSL v3 PDF, executable checker and JSX with the authoritative revision 1.3 workbench.
  Retains textual authoring and physical station language while evolving the record to explicit control vertices,
  transactional source editing and precise identity; records executable reference defects and compatibility costs.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions." }
---

# Reconciliation and evidence

**Decision proposed:** FoilDSL 4.0 is the canonical authored shape artifact. Preserve the existing control-vertex
geometry and desktop workbench; make Source a view within the CAD document. Keep the three reference files
unchanged as design evidence. They are not executable instructions to the assistant and are not the product's
production parser or geometry kernel.

## Authority and grounding

**Verified by reading:** `docs/specs/cfd-workbench-v1.md` calls itself the build basis, revision 1.3, and refines
`spec-cfd-workbench` (revision 0.2). Its A4.1–4.7 and Appendix D4 carry the current record, coordinates and CV
requirements. Its frontmatter identifies `mockup-workbench-v5` as the latest review artifact. The audit entries
`ui-design-workbench-v5`, `kernel-spike-degree-adr-handles`, and `vertex-drag-trackpad-fix` confirm the actual
v5 work and subsequent fixes. Filenames alone did not determine authority.

Traversed intent: `spec-cfd-workbench-v1 → depends-on kb-hydrofoil-workbench → area 02/05` and
`spec-cfd-workbench-v1 → relates-to mockup-workbench-v5 → control-vertex-workspace`, then A4's parametric-authority,
loft-rule-A and run-key notes. The geometry, hydrodynamics and data personas identify the same narrow vetoes:
one shape authority, no silent refit, fixed signs and immutable run provenance. Existing defect classes GEO-A,
GEO-B, GEO-C and FRAME-A directly constrain this change.

**Surface list, before editing:** reference files → normative language/spec conceptual model → native source
revision/projection contract → five channel/profile records → CAD Source/Visual transaction → 2D section
document → 3D evaluator/derived readouts → revision/run-key freshness → fixtures/proof → metadata/index/audit.
Production storage, evaluator and transport code are outside this task; their obligations are specified, not
claimed implemented.

## Read reference corpus

- [Language specification PDF](../../reference/FoilDSL%20v3%20%E2%80%94%20Language%20Specification.pdf),
  17 pages, dated 22 September 2026. All pages text-extracted with PyMuPDF; page 1 also rendered/inspected.
  This is the supplied normative **v3 proposal**: §§4–12 determine semantic differences below.
- [Reference grammar/checker](../../reference/FoilDSL%20v3%20%E2%80%94%20Reference%20Grammar%20%26%20Conformance%20Checker.html).
  Read tokenizer, parser, static checks, profile functions, derived geometry, canonical emitter and suite.
  Executed these functions with Node `vm`; observations are committed beside the fixtures.
- [Explorer JSX](../../reference/FoilDSL-Explorer-v3.jsx). Read its parser/emitter, `placeSection`, controls,
  section UI and `setDesign`/`onSrc`. Direct controls emit source immediately; valid text immediately changes
  the design and invalid text retains the previous design. This is useful interaction evidence, not a tested
  transaction or save/reopen implementation. It depends on React and injects remote font loading, so it is not
  itself the dependency-free workbench mockup.

## Reconciliation matrix

| Reference proposal | Existing commitment | Disposition | Rationale / affected surfaces |
|---|---|---|---|
| Source text is the authored foil | One complete parametric authority | **Retain** | Source owns the record; parsed arrays are projections. Language §2/8, spec A3/A4, native document, CAD Source. |
| `linear/smooth/monotone` through-anchor curves | v1.3 direct B-spline CVs and levers | **Adapt** | Anchors remain construction/import inputs. Accepted 4.0 emits degree/knots/CVs. No regression to through-points. |
| Independently authored LE and TE | Previous spec stored LE plus chord, so LE edits moved TE | **Retain reference; correct existing model** | User review requires independent edges. Source owns `leading` and `trailing`, each with separate CV abscissae/knots; chord is derived as TE−LE. Text/GUI/identity/history/compute readers all use this contract. |
| Full span input and physical station distances | Span derived from record; eta stations | **Adapt** | `half_span` is the single extent; full span derived. Root/tip/length/% station syntax resolves to eta. UI displays distance and eta. |
| `drop/rise/level` and extrema at distances | Signed elevation with readouts | **Retain UX, adapt syntax** | Plain-language drop/rise summaries remain; CV ordinate is signed. Extrema are derived, not additional station anchors. |
| Bank defaults true; rotate about quarter chord | Fixed span-axis planes; LE pivot | **Replace** | Different surfaces otherwise share a misleading name. 4.0 rule A fixes unbanked LE rotation; v3 migration measures deviation. |
| Four section scalars and family switch | Full degree-5 profile CVs; single effective t/c | **Replace** | Full section authoring must preserve arbitrary shapes, closure and edits. Named inline/hashed profiles; normalized shape blend. |
| `eppler`/`file` nominal 10% profiles | Admitted catalog, original bytes, conversion residual | **Replace** | Unknown data cannot masquerade as resolved shape. Assets resolve by hash or fail; import preserves provenance. |
| Cosine/linear interpolation of t, xt, m, xm | Linear normalized camber/unit-thickness blend | **Replace** | Preserve the established single t/c owner and different-control-count/peak fixtures. No mid-span family discontinuity. |
| NACA four digits and modifier | Admitted generators with measured conversion | **Defer to construction** | Reference modifier ignores LE-radius digit. Do not falsely advertise a faithful NACA modified generator. |
| User fixes loft degree/sample counts | Channel-evaluated surface; derived skin measured | **Replace** | Sampling belongs to display/export, never geometric authority. Kernel version/deviation are derived evidence. |
| Assertions report bounds without solving | Constraint rows and DRC have explicit owners | **Adapt** | `locks` bind CVs; `constrain` checks derived measures. Failed/unassessed assertions block Apply until fixed/removed. No hidden solver. |
| Duplicate field replaces previous value | Explicit auditable edits | **Replace** | Reject duplicate singleton fields; no invisible last-writer override. |
| Canonical emitter rounds to 0.1 mm/0.1% | Shortest round-trip, identity tolerance 1 µm | **Replace** | Preserve every semantic binary64 value. Source trivia survives routine edits; Format source is explicit. |
| FNV32 of emitted text includes name/context/constraints | BLAKE3 geometric input and complete run key | **Replace** | Source history differs from geometric history. Comment/assertion-only edits do not stale physics. |
| Direct UI edits immediately replace source | Draft/Preview/Apply/Cancel/Undo | **Adapt** | Both input paths share one transaction; invalid text cannot silently change accepted geometry. |
| Tabs by foil concept in Explorer | Seven-area desktop shell and four CAD views | **Retain ideas, adapt IA** | Source is a document view; selection links source, channels, station section and 3D. Preserve existing workflows. |

## Observed checker behavior and contradictions

Reproduce from the repo root:

```
node docs/examples/foildsl/reference-probe.cjs "reference/FoilDSL v3 — Reference Grammar & Conformance Checker.html"
```

**Verified, executed:** default identity `d801dac9`, area `831.7494675925924 cm²`, aspect
`9.738509389665811`. These establish the probe used the supplied implementation, not a reimplementation.

| Observation | Evidence and implication |
|---|---|
| 14.049 mm becomes 14 mm after emit/parse; its hash equals the 14 mm default | `reference-observations.json` precision case. Canonical idempotency is not losslessness. 49 µm is erased at that CV. |
| `file "missing.dat"` parses as t=0.1, camber=0.02, nominal=true | Same probe. It is illustrative geometry without asset resolution; 4.0 must fail closed. |
| Fractional loft degree 3.5 parses | Same probe. PDF calls a B-spline degree but S13 only checks inequalities; integer semantics are absent from executable validation. |
| Repeated `span` uses the last value | Same probe; consistent with PDF §4, intentionally rejected in 4.0. |
| PDF §11 promises a fitted B-spline surface through sampled net | Checker `deriveGeom` calculates curves/readouts; JSX `ThreeDView` draws placed profile loops. Neither constructs the promised B-spline skin. Declared loft counts/degree do not validate a kernel. |
| PDF identity prose says any geometry change changes identity | The measured rounding case contradicts this universal claim. Hashing rounded text cannot recover the discarded value. |
| PDF states NACA modifier's first digit is a LE-radius index | `nacaSpec` stores modifier but uses only second digit to shift xt; profile shape ignores first digit. Keep as unresolved v3 limitation. |
| PDF §10 promises rule-ID diagnostic and ordered rule checking | Parser throws ad-hoc messages at parse time, then static validation, without a stable S-code in each error. 4.0 defines explicit phase/code/location. |
| PDF S5 checks 91 stations | PDF itself acknowledges narrow crossings can escape. This is sampled evidence, not positivity proof. |
| `setDesign` writes design before rechecking emitted source | JSX may display an invalid direct edit while textual edits preserve last-good geometry. 4.0 acceptance is atomic and fail-closed. |

The 4.0 conformance cases deliberately attack these classes: sub-0.1 mm preservation, missing assets,
invalid degree/knot counts, duplicates, invalid drafts and explicit version rejection. Product-level interval
geometry proof and native platform round trips remain acceptance obligations; the mockup may only claim its
executed subset.

## Targeted external evidence and disconfirmation

Accessed 22 September 2026; **Verified by reading official primary documentation**:

- [RFC 8785 §§3.1–3.2](https://www.rfc-editor.org/rfc/rfc8785): canonical JSON separates presentation from
  hashable meaning; finite IEEE-754 numbers, string preservation and deterministic property sorting are required.
  This supports retaining authored trivia outside the geometry identity. It does not choose a hash algorithm
  or make tolerance-rounded numbers lossless. BLAKE3 is retained from existing product A4.1, not chosen anew here.
- [SciPy BSpline definition and basis recurrence](https://docs.scipy.org/doc/scipy/reference/generated/scipy.interpolate.BSpline.html):
  the B-spline is determined by coefficients, knots and degree; the documented recurrence supports the
  evaluator definition and N+p+1 knot count. It does not prove profile validity, surface fairness or a particular
  kernel's export tolerance. Existing geometry evidence supplies those separate obligations.

No new broad knowledge base is needed: the repository already contains the curve/loft and format evidence.
These two sources close the narrow serialization/notation gap. This task does not research or select a stack.

## Choice, alternatives and residual risk

**User correction, 22 September 2026:** retaining the old leading+chord model was incorrect for the requested
independent rail behavior. Changing LE while keeping chord fixed moved TE. The normative draft now stores two
independent absolute x rails and derives chord. This supersedes the earlier reconciliation choice above; it
does not rewrite what v3 actually proposed. The earlier unapproved 4.0 `chord cv` planform field is rejected,
not relabelled. Legacy conversion can add ordinates only when degree, knots and parametric abscissae agree;
different mappings require an exact proven common representation or measured fitting and an explicit preview.
Controls are the DSL-13/SRC-11 independence fixtures in text, visual edits and history, including rails with
different abscissae. The independent edge record is compatible with preserving the v1.3 CV editing model.

**Inferred design choice:** retain v3's core insight—an inspectable, diffable authored language—but evolve its
semantics to the already accepted CV product model. Reject an unmodified v3 runtime beside the existing evaluator
(two shape authorities); reject a four-parameter section-only DSL (cannot round-trip direct section edits);
reject GUI-only JSON with an illustrative DSL preview (the requested core artifact would remain secondary).

**Flagged review decisions:** the language version is proposed 4.0; the default source editor location is a
CAD document view; full v3 migration may exceed tolerance because banking and pivot semantics differ. User
review accepts or changes these decisions. No legacy v3 file is silently relabelled as compatible.

**Residual implementation obligations:** a production parser/evaluator, exact canonical hash vectors, asset
packaging, certified geometric checks, arbitrary multi-profile blending, stable persistent CV IDs, native
accessibility and cross-platform save/reopen proof. The present prototype is review evidence, not delivery of
those capabilities. Independent review findings and resolutions belong in the task's review/proof artifact.
