// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "CFD-Workbench",
  "generator": "docs-graph.py derive",
  "rootId": "adr-0001-master-curve-degree",
  "artifactTypes": [
    "knowledge",
    "glossary",
    "spec",
    "architecture",
    "adr",
    "design",
    "design-language",
    "investigation",
    "proof-pack",
    "decision-note",
    "threat-model",
    "privacy-review",
    "api",
    "source",
    "doc",
    "index",
    "plan"
  ],
  "relationRegistry": [
    "implements",
    "refines",
    "depends-on",
    "supersedes",
    "tested-by",
    "documents",
    "uses-term",
    "relates-to"
  ],
  "policyVersion": "traversal-policy/v1",
  "policySha256": "968b035a9618e6f997592e4f7ae91fd412b1c059c0ee89d6d8ff3025c26279fd",
  "traversalPolicies": {
    "grounding": [
      {
        "rel": "implements",
        "direction": "outbound",
        "priority": 0
      },
      {
        "rel": "refines",
        "direction": "outbound",
        "priority": 1
      },
      {
        "rel": "depends-on",
        "direction": "outbound",
        "priority": 2
      },
      {
        "rel": "uses-term",
        "direction": "outbound",
        "priority": 3
      },
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 4
      },
      {
        "rel": "documents",
        "direction": "outbound",
        "priority": 5
      }
    ],
    "impact": [
      {
        "rel": "implements",
        "direction": "inbound",
        "priority": 0
      },
      {
        "rel": "refines",
        "direction": "inbound",
        "priority": 1
      },
      {
        "rel": "depends-on",
        "direction": "inbound",
        "priority": 2
      },
      {
        "rel": "tested-by",
        "direction": "inbound",
        "priority": 3
      },
      {
        "rel": "uses-term",
        "direction": "inbound",
        "priority": 4
      }
    ],
    "proof": [
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 0
      }
    ],
    "explore-neighborhood": [
      {
        "rel": "depends-on",
        "direction": "outbound",
        "priority": 0
      },
      {
        "rel": "depends-on",
        "direction": "inbound",
        "priority": 0
      },
      {
        "rel": "documents",
        "direction": "outbound",
        "priority": 1
      },
      {
        "rel": "documents",
        "direction": "inbound",
        "priority": 1
      },
      {
        "rel": "implements",
        "direction": "outbound",
        "priority": 2
      },
      {
        "rel": "implements",
        "direction": "inbound",
        "priority": 2
      },
      {
        "rel": "refines",
        "direction": "outbound",
        "priority": 3
      },
      {
        "rel": "refines",
        "direction": "inbound",
        "priority": 3
      },
      {
        "rel": "relates-to",
        "direction": "outbound",
        "priority": 4
      },
      {
        "rel": "relates-to",
        "direction": "inbound",
        "priority": 4
      },
      {
        "rel": "supersedes",
        "direction": "outbound",
        "priority": 5
      },
      {
        "rel": "supersedes",
        "direction": "inbound",
        "priority": 5
      },
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 6
      },
      {
        "rel": "tested-by",
        "direction": "inbound",
        "priority": 6
      },
      {
        "rel": "uses-term",
        "direction": "outbound",
        "priority": 7
      },
      {
        "rel": "uses-term",
        "direction": "inbound",
        "priority": 7
      }
    ]
  },
  "limits": {
    "indexBytes": 5242880,
    "artifacts": 1000,
    "relationships": 5000,
    "spatialNodes": 500,
    "spatialEdges": 1000,
    "visibleLabels": 150,
    "surfaces": 100
  },
  "artifacts": [
    {
      "id": "adr-0001-master-curve-degree",
      "path": "docs/adr/0001-master-curve-degree.md",
      "title": "ADR-0001: master curves are degree-3 B-splines with seven vertices; the degree is a record field",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "specification 1.3",
      "reviewBy": "none while accepted",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Re-decides the knowledge base's degree-5 reading for the five master (distribution) curves: the record's default is a degree-3 clamped B-spline with seven control vertices (six to ten), the degree is stored per curve, and section curves stay degree 5. Decided on a measured fixture (fairness, anchor residual, support, lever effect) over the five example curves at both degrees, and on the loft spike showing the surface's spanwise continuity is the kernel's, measured, not the master curve's.",
      "tags": [
        "geometry",
        "b-spline",
        "degree",
        "control-vertex",
        "adr",
        "dr-10",
        "amended"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        },
        {
          "to": "kernel-spike-occt-loft",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "relates-to"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "068e7f9b1605abeea0c4d402b309d4d56bfc80052f17d5327f80826ccd1b0ddd"
    },
    {
      "id": "adr-0005-point-types",
      "path": "docs/adr/0005-point-types-in-the-b-spline-record.md",
      "title": "ADR-0005: point types are knot multiplicity in the existing B-spline record; tangent kinds are a FoilDSL 4.1 curve annotation",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-09-30",
          "reason": "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "M1.2b2 applies tangent rows to the dihedral, twist and thickness channels with a unit-free rule (ordinate deviation from the handle line within tau_c: 1 um, 1e-6 deg, 1e-8) instead of the 0.1 deg direction tolerance, which is meaningless in a metres x degrees plane (docs/design/m12b2-3d-elevations.md 3.6)."
        }
      ],
      "summary": "Settles DR-5. A Point is a control vertex of the clamped non-rational B-spline of record. An interior vertex is an Anchor point exactly when one interior knot of multiplicity p sits at it, so point type is derived from the knot vector and never stored. Tangent kinds are editing intent in an optional FoilDSL 4.1 `tangents` block outside geometry identity. Every type change is measured and reported on the A4.5 oracle. On a section the two surfaces share one chord basis, so point types are paired across the surfaces (DR-11, default); the other surface's shape is exact on Anchor creation and refitted within 10 µm, reported, on Anchor removal.",
      "tags": [
        "geometry",
        "b-spline",
        "point-type",
        "anchor",
        "control-point",
        "foildsl",
        "adr",
        "dr-5",
        "dr-10",
        "dr-11"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "refines"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1d9f2c0df1939f360835cc9c2295b61cb138cdcde86d2440a58d771b51df0c83"
    },
    {
      "id": "adr-0006-driving-dimensions",
      "path": "docs/adr/0006-driving-dimensions-and-wing-estimates.md",
      "title": "ADR-0006: driving dimensions are commands that refit the rails of record; Wing estimates are derived in Core",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-09-30",
          "reason": "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)."
        }
      ],
      "summary": "Typed Span patches half_span only (exact). Typed Root or Tip chord refits the moved rail on its own knots and abscissae, ordinates only, with the typed end pinned exactly and every lock a hard row; the held line (DR-2) is one parameter and the quarter-chord option re-sets the frame. A spike shows the operator's linear chord blend cannot meet 10 µm on a rail with the default root-mirror lock (0.08–4 mm), so DR-9 asks which rule wins; until then A4.6 is strict and the residual against the operator's rule is always reported. Wing estimates are the FoilDSL metric definitions, computed by one pure Core function and never stored.",
      "tags": [
        "geometry",
        "planform",
        "driving-dimension",
        "wing-estimates",
        "mac",
        "adr",
        "dr-2",
        "dr-9"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "dc42a0e4077031bd41f3cb8ebd5c89fcb4309bc3cba1d332172582f1dc93c167"
    },
    {
      "id": "adr-0007-edit-transactions",
      "path": "docs/adr/0007-edit-transactions-section-draft-and-gesture-commit.md",
      "title": "ADR-0007: one multi-step section draft per section-editor visit; a workspace gesture commits at its end; catalog Replace is a draft step",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [],
      "summary": "The section editor holds one draft whose bytes advance through an ordered list of source-patch steps (moves, type changes, Replace, constructions); inner Undo pops a step, Cancel discards the draft, Finish applies it as exactly one accepted revision. A workspace point gesture (DR-6 default) is a draft opened at pointer-down and applied at release when certified. Catalog Replace (DR-4) reuses the as-built import fit as one draft step with its residual reported.",
      "tags": [
        "authoring-session",
        "draft",
        "undo",
        "section-editor",
        "gesture",
        "catalog",
        "adr",
        "dr-4",
        "dr-6"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "relates-to"
        },
        {
          "to": "design-section-editor",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a39569689b1578ae635f373b87d1913aec1d55cc28b7ff83bb965853992231b5"
    },
    {
      "id": "adr-0008-section-library",
      "path": "docs/adr/0008-profile-catalog-and-section-library.md",
      "title": "ADR-0008: the Profile catalog is bundled and read-only; My sections is a folder of immutable, create-only section documents",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [],
      "summary": "Designs to the DR-8 default location. The catalog is a bundled, read-only set of GEN coordinate sets with generator and hash; VEND and LINK rows ship metadata only. My sections is a per-user folder of create-only standalone FoilDSL section documents named by their SHA-256, each carrying its own name and a flat provenance (origin plus a modified flag); the current library is a folder scan. Rights class is derived from the origin. A foil that uses an entry gets an inline copy — a stated deviation from the spec's content-addressed pin.",
      "tags": [
        "catalog",
        "section-library",
        "my-sections",
        "persistence",
        "provenance",
        "rights",
        "adr",
        "dr-8"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "decision-catalog-admission-classes",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6d6a53e3478d1bd54c3a82f9cdecfaa6c136e8e71fabd1168f7fbdcf21ab0c40"
    },
    {
      "id": "adr-0009-cad-first-shell",
      "path": "docs/adr/0009-cad-first-shell-docking-and-menus.md",
      "title": "ADR-0009: the CAD-first shell uses Dock for Avalonia 11.3.12.1 with OS-window floats, Avalonia NativeMenu, one command table and our own layout file",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [],
      "summary": "Adopts Dock for Avalonia 11.3.12.1 (MIT; the last release for Avalonia 11) for tabbed, dockable and floating panes; a spike observed a floated pane as its own NSWindow. Menus use Avalonia NativeMenu, exported to the macOS menu bar in the spike. One command table feeds menus, toolbar, palette and shortcuts. Layout is saved in our own versioned file, not Dock's serializer (its System.Text.Json path failed and its Newtonsoft JSON stores CLR type names). Maximize, monitor clamping and focus-safe floats are ours to build. Windows, mixed-DPI and screen-reader spikes are scheduled.",
      "tags": [
        "desktop",
        "avalonia",
        "docking",
        "dock",
        "nativemenu",
        "layout",
        "preferences",
        "adr",
        "ux-31",
        "ux-32"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "adr-application-stack",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-workbench-v9",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ad4b3fc85b28efeeedc146c3ff95db39446ef6e05d6c82cc08e7d8d1a35a79c0"
    },
    {
      "id": "adr-0010-one-placement-rule",
      "path": "docs/adr/0010-one-placement-rule.md",
      "title": "ADR-0010: one placement rule — FoilDSL §6 is written once in Core and instantiated over the certificate's interval arithmetic and the display's binary64",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "design — M1.2b2 (3D view and elevations, Ruling 56)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [],
      "summary": "FoilDSL §6 (Rule A section blend and the twist/dihedral placement) is written once, as a generic Core function over an arithmetic domain. The certificate instantiates it over rational intervals (its output bits unchanged, proved by a golden master); every display instantiates it over binary64 and is bound to the certificate by a measured test (at most 1 nm outside the certified enclosure). Curve evaluation stays two paths, as note-20260926 ruled; placement is one.",
      "tags": [
        "geometry",
        "placement",
        "certificate",
        "display",
        "evaluator",
        "twist",
        "dihedral",
        "rule-a",
        "adr",
        "m1.2b2",
        "oi-1"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "note-20260926-binary64-evaluator",
          "rel": "refines"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c000bcce7d56e898c274bd2b1eac452d477fbaf4c7252b6805730ad62da8bb15"
    },
    {
      "id": "adr-application-project-contract",
      "path": "docs/adr/0004-application-project-contract.md",
      "title": "Native-v1 immutable receipts and bounded admission",
      "type": "adr",
      "status": "accepted",
      "owner": "@cfd-owner-20260923",
      "phase": "design",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams."
        },
        {
          "by": "adr-application-stack",
          "on": "2026-09-23",
          "reason": "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates."
        },
        {
          "by": "proof-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "review-application-core",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Records Owner-approved unshipped native-v1 policy for durable rail edit receipts, bounded immutable history, exact numeric resource admission and fail-closed platform persistence. Independent executable-contract acceptance remains separate from these design-policy rulings.",
      "tags": [
        "adr",
        "persistence",
        "contracts",
        "identity"
      ],
      "links": [
        {
          "to": "adr-application-stack",
          "rel": "refines"
        },
        {
          "to": "design-application-contracts",
          "rel": "documents"
        },
        {
          "to": "proof-application-contracts",
          "rel": "tested-by"
        },
        {
          "to": "review-application-core",
          "rel": "tested-by"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e95a6df05d22c0140a448f08fa59bc8137d9913f7ff69c7e97970b049b70e404"
    },
    {
      "id": "adr-application-stack",
      "path": "docs/adr/0003-application-stack.md",
      "title": "Native modular monolith and source-snapshot persistence for M1",
      "type": "adr",
      "status": "accepted",
      "owner": "@cfd-owner-20260923",
      "phase": "architecture",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        }
      ],
      "summary": "Selects C#/.NET with Avalonia for the conditional offline native milestone after actual candidate spikes. Retains lossless source snapshots and append-only project facts without a database or editable AST shadow; Owner Ruling 13 accepts the direction; named cross-platform/numerical/persistence product gates remain required.",
      "tags": [
        "adr",
        "native",
        "stack",
        "persistence"
      ],
      "links": [
        {
          "to": "architecture-application",
          "rel": "documents"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "refines"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "proof-application-spikes",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "53f63a14f84abf87c56c573ede93acc4831173568897deff1956d009f87cf084"
    },
    {
      "id": "adr-foildsl-authority",
      "path": "docs/adr/0002-foildsl-authority.md",
      "title": "FoilDSL is the authored surface definition",
      "type": "adr",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Proposed adoption of a versioned FoilDSL control-vertex language as the sole authored shape representation, with concrete source preservation, deterministic semantic identity and append-only project revisions.",
      "tags": [
        "foildsl",
        "geometry",
        "persistence",
        "authority"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "decision-parametric-authority",
          "rel": "supersedes"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "92398ca5bc964c8c8eb81ff70ee0f089720e2943c5f07e9da764a6934112255c"
    },
    {
      "id": "architecture-application",
      "path": "docs/architecture/application.md",
      "title": "CFD-Workbench application architecture and offline first milestone",
      "type": "architecture",
      "status": "accepted",
      "owner": "@cfd-owner-20260923",
      "phase": "architecture",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "adr-application-stack",
          "on": "2026-09-23",
          "reason": "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Active-seat dispatch control and observed serial core checkpoints added; review execution references."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "Ruling 56 adds slice M1.2b2 (after M1.2b, before M1.2c): 10.6 phasing needs its row; ADR-0010 makes FoilDSL 6 one generic Core rule instantiated by the certificate and every display."
        }
      ],
      "summary": "Defines the accepted native modular monolith with one deterministic source-authoring core and GUI/CLI adapters. Defines the whole application's boundaries, durable source/history invariants and vertical delivery; the first offline slice stays behind independently reviewed numerical, persistence and native gates. §10 (proposed, spec 1.6) adds the CAD-first shell, point types, driving dimensions, Wing estimates, the section draft, catalog and My sections, with ADR-0005–0009 and slices M1.2a–e.",
      "tags": [
        "application",
        "native",
        "offline",
        "architecture",
        "cad-first",
        "spec-1.6"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "adr-application-stack",
          "rel": "depends-on"
        },
        {
          "to": "design-application-foundation",
          "rel": "relates-to"
        },
        {
          "to": "proof-application-spikes",
          "rel": "tested-by"
        },
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0008-section-library",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "4. Components and composition",
          "mermaid": "flowchart LR\n  GUI[Native desktop adapter] --> Session[Authoring session commands]\n  CLI[Command line adapter] --> Session\n  Session --> Parser[Lossless source parser and patcher]\n  Parser --> Kernel[Deterministic geometry and interval validator]\n  Kernel --> Identity[Canonical identity]\n  Session --> Store[Native project store]\n  Store --> Bytes[Immutable source snapshots and history facts]\n  Kernel --> View[Derived viewport and section projection]\n  View --> GUI\n  Session --> Unavailable[Analysis unavailable in M1]"
        },
        {
          "kind": "flowchart",
          "title": "10.3 Components and layers (additions to §4)",
          "mermaid": "flowchart LR\n  subgraph Desktop\n    Shell[Shell: Dock host, workspaces, NativeMenu] --> Cmd[Command table]\n    Cmd --> Ctl[WorkbenchController: selection, modes, gestures]\n    Ctl --> Props[Properties / Points / Messages / Browser panes]\n    Ctl --> Views[Plan · 3D · Side · Front · Section canvas]\n  end\n  subgraph Core\n    Session[Authoring session: drafts, section steps, ApplyDimension] --> Parser[FoilDSL 4.0/4.1 parser and patcher]\n    Session --> Fit[ConstrainedFit and import fit]\n    Session --> Kernel[Rational certificate: Geometry.Assess]\n    Basis[SplineBasis, binary64] --> Est[WingEstimates]\n    Points[Point model: derived type, tangent rows] --> Session\n    Catalog[Profile catalog, read-only]\n  end\n  subgraph Persistence\n    Store[Native project store]\n    Lib[Section library folder store]\n    Prefs[Layout preference store]\n  end\n  Ctl --> Session\n  Ctl --> Est\n  Ctl --> Catalog\n  Ctl --> Lib\n  Shell --> Prefs\n  Session --> Store"
        }
      ],
      "sourceSha256": "337de5e2476f612b4d2da99b8383b4c0e6a558aeb134bb4e14447ea808613413"
    },
    {
      "id": "cad-editing-views",
      "path": "docs/notes/cad-editing-views.md",
      "title": "Elevations edit, 3D looks, a station is a document — the CAD editing model",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v4",
          "on": "2026-09-20",
          "reason": "Mockup v4 (CAD editing views) supersedes v3; spec 1.2 CAD-04–06, UX-23, UI-24–25; oracle tools/check-mockup-v4.mjs."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "CAD-04–06 (spec 1.2) — the four control curves are edited in the elevation that shapes them (Top · Front · Starboard), the 3D viewport is one free camera used for looking and selecting, and a station is a document tab with a full 2D section editor; every curve is a spline and the rail carries icons.",
      "tags": [
        "cad",
        "camera",
        "elevations",
        "control-curves",
        "station",
        "splines"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "refines"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v4",
          "rel": "relates-to"
        },
        {
          "to": "thick-client-shell",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c55f81fad0c3c2f24a2e57f3b3c4f61e2c140a4c8721e018f85d7022e77db6b5"
    },
    {
      "id": "control-vertex-workspace",
      "path": "docs/notes/control-vertex-workspace.md",
      "title": "The vertices are the record; the workspace is four viewports and a palette — the v5 CAD model",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v5",
          "on": "2026-09-21",
          "reason": "Mockup v5 (control-vertex splines, four viewports, tool palette) supersedes v4; spec 1.3 GEO-03/05/13/15, CAD-01/04/06/07/08, A4.2, A4.12, UX-24, UI-25–27; oracle tools/check-mockup-v5.mjs"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Specification 1.3 — the geometry of record is a control-vertex B-spline per master curve (degree 3, seven vertices, levers at the ends; Fit points and Fair are constructions with reported residuals; locks are vertex constraints), the CAD workspace is four viewports with title menus and a nine-verb tool palette, the 3D body is a NURBS loft with a display cage (never a T-spline), and the geometry kernel is an owned evaluator plus OCCT and rhino3dm behind a spike gate.",
      "tags": [
        "cad",
        "control-vertex",
        "splines",
        "levers",
        "viewports",
        "palette",
        "cage",
        "kernel",
        "geometry"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "refines"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v5",
          "rel": "relates-to"
        },
        {
          "to": "cad-editing-views",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "08f94bd98223f75066991797044651499425eda584e5010c59704fb03c42c333"
    },
    {
      "id": "decision-catalog-admission-classes",
      "path": "docs/notes/catalog-admission-classes.md",
      "title": "Catalog admission by rights class: GEN, VEND, LINK",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Every bundled section carries an admission class with its reason: GEN generated at build from a public-domain definition, VEND redistributed under written terms, LINK cited only. Eppler sections are pending until UIUC terms exist.",
      "tags": [
        "catalog",
        "licensing",
        "sections"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "review-spec-v02-critique",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c2197a2b1bb99b6e7914332e68972004b55fc6cf928b0f1812e79d7f588b222a"
    },
    {
      "id": "decision-design-iteration",
      "path": "docs/notes/design-iteration.md",
      "title": "Section scope and evidence-based design alternatives",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Defines visible section editing, shared-profile scope, draft-safe inspection, named design alternatives and explicit dimensional intent. Corrects thickness, interpolation and file-opening inconsistencies without adding a competing shape authority or a simulation implementation.",
      "tags": [
        "geometry",
        "authoring",
        "ux",
        "provenance",
        "alternatives"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "spec-foildsl",
          "rel": "refines"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8e8f167ff5a13afa1f66823b6c18e0b8e85e59841329f1fc3970b93ddef96199"
    },
    {
      "id": "decision-foildsl-reconciliation",
      "path": "docs/notes/foildsl-reconciliation.md",
      "title": "Reconcile FoilDSL v3 with the control-vertex workbench",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Compares the supplied FoilDSL v3 PDF, executable checker and JSX with the authoritative revision 1.3 workbench. Retains textual authoring and physical station language while evolving the record to explicit control vertices, transactional source editing and precise identity; records executable reference defects and compatibility costs.",
      "tags": [
        "foildsl",
        "reference",
        "geometry",
        "ux",
        "reconciliation"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "decision-parametric-authority",
          "rel": "refines"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        },
        {
          "to": "kb-hw-parametric-curves-lofts-and-surfaces",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-file-formats-and-grammars",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0b58151806b63dd37284a1a155228a5038216fe0422ab2ec2743ec6ff0101941"
    },
    {
      "id": "decision-freshness-by-run-key",
      "path": "docs/notes/freshness-by-run-key.md",
      "title": "Freshness is derived by run-key equality, never written",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "A run is Current when the BLAKE3 key over its canonical inputs, method id + version and settings hash equals the key recomputed from the current design; nothing ever writes a freshness flag.",
      "tags": [
        "analysis",
        "provenance",
        "data-model"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "review-spec-v02-critique",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "99ccc9c75ee9ae182b65d059e600a08edb82b65855cc00e7a9ea083c55d793c3"
    },
    {
      "id": "decision-loft-rule-a",
      "path": "docs/notes/loft-rule-a.md",
      "title": "Loft rule A: the record is channel-evaluated; skins are derived",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "The surface of record between stations is the channel-evaluated analytic surface; every B-spline skin (display, STEP, 3DM) is a derived approximation with a measured, reported deviation. Rule B (skin as record) is not offered.",
      "tags": [
        "geometry",
        "loft",
        "export"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "review-spec-v02-critique",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "58f0b17a4bd1da52acae9adde617644b6ae4d397d81938baf6c9cd0b688fc08f"
    },
    {
      "id": "decision-ncrit-pair",
      "path": "docs/notes/ncrit-pair.md",
      "title": "Every polar at the Ncrit pair {2, 4}, shown as a band",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Section polars are computed at Ncrit 2 and 4 and shown as a band labelled as a practitioner range with no measured water N-factor; a single-Ncrit polar needs a recorded user override.",
      "tags": [
        "hydrodynamics",
        "polars",
        "evidence"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "review-spec-v02-critique",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "76f4c1cda8859d11ef377bf72f4a5eefd5e085f7db25201a6ffd3aa7abf725ea"
    },
    {
      "id": "decision-parametric-authority",
      "path": "docs/notes/parametric-authority.md",
      "title": "One parametric definition, two editing views",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        }
      ],
      "summary": "Curves and station profiles are coordinated views of one complete explicit surface definition. Detaching the initial recipe changes which parameters drive the shape, not whether the model is parametric.",
      "tags": [
        "geometry",
        "curves",
        "stations"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4fb76602b85faec100692cfc90ab371c6ff3db223a0964d1012544e8996fc2b9"
    },
    {
      "id": "decision-seven-areas",
      "path": "docs/notes/seven-areas.md",
      "title": "Seven discrete areas, each with a typed AI proposal kind",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-20",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v2",
          "on": "2026-09-20",
          "reason": "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "The product is seven discrete, complementary areas — Setup, CAD, Analysis, Experiment setup, Run, Results, Export — each owning a typed input and output object and a verb set, with one AI prompt entry per area whose output is a validated, previewed proposal the user accepts.",
      "tags": [
        "ia",
        "ai",
        "experiment",
        "run",
        "results"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v2",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "04018cb7f4c10afcdceac3190c8ee9624cbd69b9fc47f7ccf6dbabf3d7c5b4d6"
    },
    {
      "id": "kernel-spike-occt-loft",
      "path": "docs/notes/kernel-spike-occt-loft.md",
      "title": "Kernel spike — OCCT ThruSections loft against the owned evaluator (A4.12 exit evidence)",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-09-21",
          "reason": "ADR-0001 decides the master-curve degree (3, seven vertices, stored per curve); the spec's Open decisions and A4.1/A4.2 cite it"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "The Spike Protocol run on the geometry kernel decision of specification 1.3 A4.12 — OCCT 7.8.1 (via FreeCAD 1.1.1 headless, macOS arm64) lofting N exact section B-splines against the owned evaluator's rule-A surface at 50 × 200 closest-point samples, with a STEP round trip. Base and maximum-twist cases meet the 10 µm acceptance from N = 16 sections (1.1 µm and 0.5 µm; 0.8/0.4 µm at N = 64); the zero-chord tip does not converge with uniform sections (2.5–18 mm) and needs its own rule. Windows x64 and the licence review remain open.",
      "tags": [
        "geometry",
        "kernel",
        "occt",
        "loft",
        "step",
        "spike",
        "evidence"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6d3771f50f16013e3003d30d25b5733c133cf180dbe079923198dede8ff9f2c2"
    },
    {
      "id": "note-20260926-binary64-evaluator",
      "path": "docs/notes/binary64-evaluator.md",
      "title": "Core keeps one binary64 B-spline basis (SplineBasis); only the FoilSource copy is folded into it",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "2027-03-25",
      "reviewSuggested": [],
      "summary": "The Wing estimates and every display path use the shared binary64 `SplineBasis`; the one duplicate private evaluator (`FoilSource.cs:574`) is folded into it under a byte-identical golden master of every fit and patch. The rational certificate and its display sampler are excluded, because they are the proof path.",
      "tags": [
        "decision-note",
        "geometry",
        "evaluator",
        "refactor"
      ],
      "links": [
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7d191b6a6628b6c838fa70d98551d264a9d2cdde66baf473e518b4747268a1ad"
    },
    {
      "id": "note-20260926-command-table-selection",
      "path": "docs/notes/command-table-and-selection.md",
      "title": "The CAD-first shell has one command table and one selection state; telemetry reuses the apply event",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "architecture — spec 1.6 (CAD-first)",
      "reviewBy": "2027-03-25",
      "reviewSuggested": [],
      "summary": "Every CAD verb is one row in a command table that feeds the native menu, toolbar, palette and key bindings on every window; the controller owns one selection state that every view and pane observe. Applies of every kind emit the existing apply event with an edit_kind attribute instead of new per-kind events.",
      "tags": [
        "decision-note",
        "desktop",
        "commands",
        "selection",
        "telemetry"
      ],
      "links": [
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ceb98bc5764fa8eb3509fac773e4446d1c159029c38b1d78f8da3ddd44a05c9b"
    },
    {
      "id": "note-20260927-recent-files",
      "path": "docs/notes/recent-files-preference.md",
      "title": "Recent files are kept in their own preference file, separate from the layout file",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2a (spec 1.6)",
      "reviewBy": "2027-03-25",
      "reviewSuggested": [],
      "summary": "Start's Recent group (CAD-14) is backed by recent.json beside layout.json, written through the same ProjectStore, at most 10 absolute paths, cleared by File ▸ Open Recent ▸ Clear Menu. The paths are personal data, so they never enter the layout file or telemetry.",
      "tags": [
        "decision-note",
        "desktop",
        "preferences",
        "recent-files",
        "privacy"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e403ec1fa6749f66600021007d59dfa7500598d7dc70f5fca239831f1c7dc0dd"
    },
    {
      "id": "note-m1-scope-decision",
      "path": "docs/notes/m1-scope-decision.md",
      "title": "User decision — section editing in M1.1; on-screen timing removed as a gate; Windows deferred",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@owner",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-24",
          "reason": "User-approved M1 two-platform visible-timing gates and M1.1 section authoring changed the delivery architecture; review dependent milestone claims."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-24",
          "reason": "Approved Windows/timing M1 and section M1.1 placement plus R39 native veto changed the coordination gates; review dependent status and handoffs."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-25",
          "reason": "D1/D2 revision removes on-screen timing as an M1 gate and defers Windows qualification; review §8 gate claims."
        },
        {
          "by": "plan-application-build",
          "on": "2026-09-25",
          "reason": "D1/D2 revision removes on-screen timing as an M1 gate and defers Windows qualification; review the execution graph's gate claims."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-25",
          "reason": "D1/D2 revision removes on-screen timing as an M1 gate and defers Windows qualification; review coordination status."
        },
        {
          "by": "coordination-contract-c-native",
          "on": "2026-09-25",
          "reason": "D1/D2 revision removes on-screen timing as an M1 gate; review §3 budget requirement."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "The user approved Windows x64 runtime and measured on-screen budgets in M1 and placed the full section editor in M1.1. A 2026-09-25 revision then removed on-screen timing as an M1 gate and deferred Windows x64 qualification; section editing in M1.1 is unchanged.",
      "tags": [
        "decision-note",
        "milestone",
        "windows",
        "performance",
        "sections"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "spec-foildsl",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        },
        {
          "to": "coordination-contract-c-native",
          "rel": "relates-to"
        },
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        },
        {
          "to": "plan-application-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1e0f6c012b58b20223e3ffef24b40cef762c22c4c770f9a6ccc3c2d135817da1"
    },
    {
      "id": "note-sweep-replay-semantics",
      "path": "docs/notes/sweep-replay-semantics.md",
      "title": "Sweep playback selects an operating point, not physical time",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-18",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        }
      ],
      "summary": "A velocity-by-incidence sweep has discrete case identities, and every result surface follows one selected case. Playback cannot imply transient fluid time or carry fields from a missing case's predecessor.",
      "tags": [
        "decision-note",
        "simulation",
        "visualization",
        "provenance"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "93ac732f86f2792b257f31e00ef12869535e32a64fa0c3659ebe99de30c29b19"
    },
    {
      "id": "property-grid-rulings",
      "path": "docs/notes/property-grid-rulings.md",
      "title": "Property grid — operator rulings on precision, field nudge, scrubbing, build order and labels",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-30",
      "reviewSuggested": [],
      "summary": "Operator rulings of 2026-10-01 on the property-grid review. Precision follows the quantity, not the row. The field nudge is adopted for point and handle fields only, as the canvas Nudging gesture. Drag-to-scrub is rejected. A dedicated track builds the component between the M1.2b fix track and PNL. Both root-chord fields stay editable and are labelled. Expressions are set once and say so. A point's spanwise coordinate is \"From root\", with η beside it.",
      "tags": [
        "ui",
        "properties",
        "property-grid",
        "precision",
        "keyboard",
        "rulings"
      ],
      "links": [
        {
          "to": "review-ui-property-grid",
          "rel": "refines"
        },
        {
          "to": "mockup-property-grid",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "refines"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-property-grid-density",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "34d25d5855e3053ed3187b8055c51d88cd45a496805805819e050a9faf01845e"
    },
    {
      "id": "thick-client-shell",
      "path": "docs/notes/thick-client-shell.md",
      "title": "The window is the unit — a thick-client shell, not a scrolling page",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v3",
          "on": "2026-09-20",
          "reason": "Mockup v3 (thick-client shell) supersedes v2 as the review artifact; shell contract proven by tools/check-mockup-v3.mjs; UI-23 and the activity rail in spec 1.1a."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "The CFD-Workbench client is a fixed window whose regions scroll inside themselves — menu bar, one-row measured toolbar, parameter row, activity rail, docks, editor with document tabs and a tabbed bottom panel, status bar — with each area's content arranged for that vignette; page scroll and toolbar wrapping are defects the oracle fails.",
      "tags": [
        "ui",
        "shell",
        "thick-client",
        "layout",
        "toolbar"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "refines"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v3",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "da2a659e45dffc501e47f21e2a6725fec8bdd3826b3da9d73fc20796d7d8f265"
    },
    {
      "id": "design-app-shell",
      "path": "docs/design/app-shell.md",
      "title": "Design: the CAD-first app shell — Start, workspaces, docks and floats, selection and Properties, menus and commands",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2a shell parts and M1.2e (spec 1.6)",
      "reviewBy": "2027-03-25",
      "reviewSuggested": [
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Detailed design of the CAD-first shell for slices M1.2a (Start/Opening/open-failed, Planform workspace, left side bar, Properties with the Wing block and typed Span, one command table feeding per-window NativeMenus, Edit-verb routing) and M1.2e (owned OS-window floats, Maximize, focus-safe floats, Review workspace, per-workspace saved layouts with clamping). Settles the layout-file schema with a version-first rollback rule, the selection model, the focus contract, the failure modes and the build tracks with exclusive file ownership and exact test names.",
      "tags": [
        "desktop",
        "shell",
        "docking",
        "dock",
        "layout",
        "workspaces",
        "floats",
        "nativemenu",
        "commands",
        "selection",
        "properties",
        "focus",
        "m1.2a",
        "m1.2e"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "refines"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0008-section-library",
          "rel": "depends-on"
        },
        {
          "to": "note-20260926-command-table-selection",
          "rel": "refines"
        },
        {
          "to": "note-20260927-recent-files",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v9",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "91dbbecdedda14bcb8e711f7d66de4c13f162a5c52471641e7a28bf8c9151c42"
    },
    {
      "id": "design-application-contracts",
      "path": "docs/design/application-contracts.md",
      "title": "Native M1 session and project contracts",
      "type": "design",
      "status": "in-review",
      "owner": "@cfd-owner-20260923",
      "phase": "design",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "adr-application-project-contract",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "proof-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Defines the complete serial M1 session, source patch, native-v1 history, identity and persistence seams. Executable contract fixtures establish bounded behavior without certifying geometry or claiming a native store. Owner and independent review retain the production gate.",
      "tags": [
        "application",
        "contracts",
        "source",
        "history",
        "identity"
      ],
      "links": [
        {
          "to": "design-application-foundation",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "adr-application-project-contract",
          "rel": "depends-on"
        },
        {
          "to": "proof-application-contracts",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e4aef2c723f005e573eee85742a6a4881c85290d43f04ff63f8ed9848938b354"
    },
    {
      "id": "design-application-foundation",
      "path": "docs/design/application-foundation.md",
      "title": "Offline accepted-source workbench slice",
      "type": "design",
      "status": "proposed",
      "owner": "@cfd-owner-20260923",
      "phase": "design",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "adr-application-stack",
          "on": "2026-09-23",
          "reason": "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Designs the first native GUI/CLI vertical slice around lossless accepted source, one owned rail draft, certified conservative geometry and append-only save/recovery. Defines compiling port vocabulary, failure/security/privacy tests and exact downstream ownership proposals without production implementation.",
      "tags": [
        "application",
        "source",
        "geometry",
        "persistence",
        "native"
      ],
      "links": [
        {
          "to": "architecture-application",
          "rel": "implements"
        },
        {
          "to": "adr-application-stack",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "proof-application-spikes",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e696a409baf1ced3600e971e5dab68c598d0a629e710d66c5dc4d305883638e2"
    },
    {
      "id": "design-authoring-decisions",
      "path": "docs/design/authoring-decisions.md",
      "title": "Section authoring and design-decision interaction direction",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevates the existing spatial workbench around the complete alternative-to-decision task: visible section entry, explicit shared scope and thickness intent, read-only draft inspection, and honest baseline evidence. Reuses the established design language and separates prototype proof from native/scientific obligations.",
      "tags": [
        "ux",
        "cad",
        "profiles",
        "authoring",
        "comparison"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "decision-design-iteration",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "design-foildsl-authoring",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e8a4436a4e96122828f90787c0e14342ea630f7085d06136b47b364441896bca"
    },
    {
      "id": "design-foildsl-authoring",
      "path": "docs/design/foildsl-authoring-direction.md",
      "title": "FoilDSL authoring direction and transaction contract",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Keep the spatial CAD workbench and add a source document with explicit validation and shared transactions, drawing useful authoring ideas from the supplied JSX without importing its scrolling page or alternate geometry model.",
      "tags": [
        "foildsl",
        "ux",
        "cad",
        "direction"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4194dc97f5d6fd7b6b4921a0e53ae92fd9dcb2e7c563c8aad18306c2d2b601bd"
    },
    {
      "id": "design-m12b-points",
      "path": "docs/design/m12b-points.md",
      "title": "Design: M1.2b — CAD point editing on the Plan view (rail points, point types, gestures, typed chords)",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2b (spec 1.6, architecture §10.6)",
      "reviewBy": "2027-03-29",
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-09-30",
          "reason": "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "M1.2b2 design asks seams SR-1..SR-5 before B0/U1b dispatch: rename AftMeters/AftOnly to Ordinate/ValueOnly across PointView, GestureFrame, PlanSample, CombTooth, HandleTarget and UpdateGesture; CurvePointLayer extraction from PlanCanvas; one binary64 channel inversion; curve guard from one table; optional unit-free row rule for rails (F-14). Each has a fallback owned by M1.2b2."
        },
        {
          "by": "mockup-property-grid",
          "on": "2026-10-01",
          "reason": "F-1 property grid: Properties becomes identity + collapsible groups + label | value | unit rows; Tangent is a labelled group shown on handles too (F-4); one identity per selection (O-6); units and UI-40 precision everywhere (O-4); estimates Unavailable with a reason instead of ≈ — (COPY-155); COPY-149..157 proposed. Review §11.4 Properties rows and the precision conflict DR-UID-1."
        },
        {
          "by": "property-grid-rulings",
          "on": "2026-10-01",
          "reason": "Operator rulings DR-UID-1 and MC-6 need spec-owner amendments: precision follows the quantity (UI-40 angle text: placed/typed 0.01°, derived 0.1°; placed t/c 0.01 %; station chord at root/tip 0.01 mm; m12b §11.4 \"Lengths display at 0.01 mm\" covers typed dimensions only; status \"MAC 101.3 mm\"); a point's spanwise coordinate is \"From root\" with η (hover/peer names, probe, CAD-15/UI-37); A4.8 expressions are set once; COPY-149..167 proposed."
        }
      ],
      "summary": "Detailed design of slice M1.2b: a real Plan view (top-down, both rails as curves, stations, every rail point as a typed glyph, a Tracing probe and a curvature comb) on which a point or handle is selected, dragged, nudged at 0.01/0.1/1 mm or typed, and committed as one undo step at the end of the gesture while the Wing estimates follow the drag. Properties sets Anchor/Control type and Smooth/Symmetric/Corner tangents (FoilDSL 4.1); typed Root and Tip chord refit both rails under the ruled quarter-chord hold and root-flat blend with both numbers reported. Amends ADR-0001 to 6-16 channel vertices under 4.1.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "plan-view",
        "point-types",
        "anchor",
        "control-point",
        "tangent",
        "gesture",
        "undo",
        "wing-estimates",
        "driving-dimension",
        "foildsl-4.1",
        "dr-2",
        "dr-6",
        "dr-9",
        "dr-10",
        "m1.2b"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "refines"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "coordination-app-shell-build",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "17a458bd420e83c321c09ed642c694ff871866c4140c9cc00b1c6af6c3460968"
    },
    {
      "id": "design-m12b2-3d-elevations",
      "path": "docs/design/m12b2-3d-elevations.md",
      "title": "Design: M1.2b2 — one placement rule, the 3D view beside the Plan, the Front and Side elevations, and dihedral, twist and thickness editing",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2b2 (Ruling 56 OI-1; after M1.2b, before M1.2c)",
      "reviewBy": "2027-03-29",
      "reviewSuggested": [
        {
          "by": "mockup-property-grid",
          "on": "2026-10-01",
          "reason": "PNL should fill the property-grid component (docs/reviews/ui-property-grid.md §10) from PropertiesView.Build by data; channel rows Height (mm), Twist (°), t/c (%) render in the same row kinds; the M1.2b fix track is recommended to build the component first (DR-UID-4)."
        }
      ],
      "summary": "Detailed design of slice M1.2b2. FoilDSL §6 (Rule A and the twist/dihedral placement) is written once in Core and instantiated over the certificate's rational intervals (bits unchanged) and over binary64 for every display, bound by a measured test (at most 1 nm outside the certified enclosure). On that rule: a shaded or wireframe 3D view beside the Plan with orbit, pan, zoom, view cube and named cameras; Front and Side elevations drawing the placed foil with dihedral, t/c and twist lanes; and those three channels edited with M1.2b's point, handle and gesture model.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "geometry",
        "placement",
        "certificate",
        "3d-view",
        "orbit",
        "view-cube",
        "elevations",
        "front",
        "side",
        "body-plan",
        "dihedral",
        "twist",
        "thickness",
        "lanes",
        "m1.2b2",
        "oi-1",
        "dr-13"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "refines"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "note-20260926-binary64-evaluator",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "coordination-app-shell-build",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b06047368e5a9dd734eb1f00867632fd002885870e7790b53177278c789ef78c"
    },
    {
      "id": "design-section-editor",
      "path": "docs/design/section-editor.md",
      "title": "M1.1 full section editor",
      "type": "design",
      "status": "in-review",
      "owner": "@cfd-leader-fbfa35dc",
      "phase": "design",
      "reviewBy": "2026-12-25",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-26",
          "reason": "Spec 1.6 revision (§10 proposed): point types as knot multiplicity + FoilDSL 4.1 tangents (ADR-0005), driving dimensions/Wing estimates (ADR-0006), multi-step section draft and gesture commit (ADR-0007), catalog and My sections (ADR-0008), Dock/NativeMenu shell (ADR-0009); DR-9/10/11 raised."
        }
      ],
      "summary": "Design for the M1.1 section editor: profile control-vertex editing with shared or independent scope, certified multi-profile blending, and a UI-25 control-frame canvas in the desktop app. Delivered in two named increments (M1.1a edit core, M1.1b construction operations and thickness proposals); the editor is called \"full\" only once both land.",
      "tags": [
        "section",
        "profile",
        "editor",
        "m1.1",
        "geometry",
        "desktop"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "implements"
        },
        {
          "to": "design-application-contracts",
          "rel": "refines"
        },
        {
          "to": "note-m1-scope-decision",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "97b70ff221af3ffe6d67db18417d25d4e74b4e2d32d33d8c1dd5f5ec205c4044"
    },
    {
      "id": "design-visible-presentation",
      "path": "docs/design/visible-presentation.md",
      "title": "Visible presentation endpoint feasibility contract",
      "type": "design",
      "status": "in-review",
      "owner": "@cfd-visible-endpoint-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [
        {
          "by": "proof-visible-presentation",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        }
      ],
      "summary": "A source-bound synthetic target and fail-closed receipt model test endpoint feasibility. A target-only native observer is compiled but unexecuted; no visible latency or physical visibility is qualified.",
      "tags": [
        "performance",
        "presentation",
        "spike"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "depends-on"
        },
        {
          "to": "note-m1-scope-decision",
          "rel": "refines"
        },
        {
          "to": "proof-visible-presentation",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4fd5d57828dd597508e8e5ded565d454e9146f6dd74e33bd6543ecf040669cec"
    },
    {
      "id": "design-windows-runtime",
      "path": "docs/design/windows-runtime.md",
      "title": "Windows x64 native qualification contract",
      "type": "design",
      "status": "in-review",
      "owner": "@cfd-windows-w0-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "proof-windows-runtime",
          "on": "2026-09-24",
          "reason": "W1 real Windows x64 execution failed native qualification; review source-bound proof and open product gates."
        },
        {
          "by": "coordination-windows-runtime-route",
          "on": "2026-09-24",
          "reason": "R43-R44 hosted route executed with failed native cases and a DACL receipt refusal; review route dependencies."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-26",
          "reason": "Spec 1.6 revision (§10 proposed): point types as knot multiplicity + FoilDSL 4.1 tangents (ADR-0005), driving dimensions/Wing estimates (ADR-0006), multi-step section draft and gesture commit (ADR-0007), catalog and My sections (ADR-0008), Dock/NativeMenu shell (ADR-0009); DR-9/10/11 raised."
        }
      ],
      "summary": "Defines the disposable W0 Windows file and process experiment, its fixed failure matrix and receipt boundary. It does not admit a production Windows store: directory durability, complete hostile-namespace containment and passing native qualification remain open after the failed W1 hosted run.",
      "tags": [
        "windows",
        "persistence",
        "security",
        "qualification"
      ],
      "links": [
        {
          "to": "coordination-windows-runtime-route",
          "rel": "depends-on"
        },
        {
          "to": "design-application-contracts",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        },
        {
          "to": "proof-windows-runtime",
          "rel": "tested-by"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution graph and review floors",
          "mermaid": "flowchart LR\n  G[Ground contract] --> R[Receipt RED and negative controls]\n  R --> N[Native spike and local build]\n  G --> W[Prepare hosted workflow]\n  N --> P[Proof and exact-path handback]\n  W --> P\n  P --> V[Independent root review]"
        }
      ],
      "sourceSha256": "b834c7381e6b2ed57ee0f816caa417d98f3f753d69af48a832a2eb92bfaba945"
    },
    {
      "id": "mockup-property-grid",
      "path": "docs/mockups/property-grid.md",
      "title": "CFD-Workbench — the Properties pane as a property grid",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "F-1 from the M1.2b native review, elevated: Properties becomes one reusable property grid — a selection identity, collapsible groups and label | value | unit rows on a shared column — in every selection state (foil, control, anchor with a labelled Tangent group, handle with its anchor's tangent, named points, several points, station) and every hard state, with the Wing block pinned at the foot and an honest Unavailable state in place of \"≈ —\".",
      "tags": [
        "mockup",
        "properties",
        "property-grid",
        "native-ui",
        "m1.2b",
        "m1.2b2",
        "wing",
        "tangent"
      ],
      "links": [
        {
          "to": "mockup-workbench-v10",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "relates-to"
        },
        {
          "to": "review-m12b-native",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-property-grid",
          "rel": "tested-by"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-property-grid-density",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "855a2d3b8d5a8ef9aecea48e05b2ef27ad7ada50a7fc35d947a672fb5f351c48"
    },
    {
      "id": "mockup-workbench",
      "path": "docs/mockups/workbench.md",
      "title": "CFD-Workbench interactive design prototype",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-18",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        },
        {
          "by": "design-language",
          "on": "2026-09-19",
          "reason": "Initial cross-platform workbench token and interaction language created for review."
        }
      ],
      "summary": "Self-contained HTML workbench with editable catalog section curves, weighted smoothing, fluid-aware force previews and linked velocity–angle sweep visualization. The review harness and synthetic field fixtures demonstrate interaction contracts, not a production geometry kernel or solver.",
      "tags": [
        "mockup",
        "hydrofoil",
        "curves",
        "stations"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "064b1723e8af100858a07b82387785bb87208fe88b90387717cb8ea13493a4ac"
    },
    {
      "id": "mockup-workbench-v1",
      "path": "docs/mockups/workbench-v1.md",
      "title": "CFD-Workbench interactive design mockup v1",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Self-contained HTML workbench built against specification v1: Brief with the seven-point goal state computed live, Shape with a genuine constrained weighted least-squares B-spline evaluator, Sections with admission classes and a two-layout DAT detector, Analyze where every number carries its basis (tier, depth, Ncrit band, surface state, omissions, fixed strings), a Checks drawer, a gated export dialog and the assistant's honest states. A review harness switches persona, viewport, state, theme, density, capability, navigation preset, modifier scheme, trackpad mode and reduced motion. Illustrative throughout; no kernel, solver or file I/O.",
      "tags": [
        "mockup",
        "hydrofoil",
        "curves",
        "stations",
        "analysis",
        "v1"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fdca07873d80d9bbe8ad26ea5e4b18059231f8b77a5e1186ff3bbd32f6037c02"
    },
    {
      "id": "mockup-workbench-v10",
      "path": "docs/mockups/workbench-v10.md",
      "title": "CFD-Workbench v10 — first run, focus-safe floats, point types, catalog and a Wing block",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "v10 draw3d/drawSide place sections without twist (html :690-697) - recorded as defect class GEOM-AUTHORITY; M1.2b2 also departs from v10 on Front handedness (D-2) and Side nose direction (D-10)."
        }
      ],
      "summary": "v9's docked-pane CAD workspace, elevated: first-run, opening and open-failed states inside the workspace; floats move clear of any focused target in the model area; per-point Anchor / Control type; a Wing block with typed span and chords above running estimates; and, in the section editor, Replace from catalog and Save to My sections.",
      "tags": [
        "mockup",
        "cad",
        "docking",
        "properties",
        "catalog",
        "point-types",
        "estimates",
        "first-run"
      ],
      "links": [
        {
          "to": "mockup-workbench-v9",
          "rel": "supersedes"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8572e079dbbeca764d544fe764dca466250d0d2079d1b9646410a14a7afd08ae"
    },
    {
      "id": "mockup-workbench-v2",
      "path": "docs/mockups/workbench-v2.md",
      "title": "CFD-Workbench interactive design mockup v2 — seven areas",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-20",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Self-contained HTML workbench built against specification v1.1: an area strip in flow order — Setup · CAD · Analysis · Experiment · Run · Results · Export — with readiness chips, a prompt entry in every area whose output is that area's typed proposal, one canvas shared by CAD and Analysis, a process console for Run over a stepped fixture, and Results as sequences over admitted samples with every layer's basis. Run and Results render their full target state and carry the \"gated (SPIKE-03/04)\" chip. Illustrative throughout; no kernel, solver, file I/O or model call.",
      "tags": [
        "mockup",
        "hydrofoil",
        "setup",
        "cad",
        "analysis",
        "experiment",
        "run",
        "results",
        "export",
        "v2"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v1",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v2",
          "rel": "relates-to"
        },
        {
          "to": "decision-seven-areas",
          "rel": "relates-to"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "52f5f5231d8d6a1a24def728d613eb5a57551d7abcbeb2ba8f8c33e1b4b887cc"
    },
    {
      "id": "mockup-workbench-v3",
      "path": "docs/mockups/workbench-v3.md",
      "title": "CFD-Workbench interactive design mockup v3 — thick-client shell",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Self-contained HTML workbench rebuilt as a thick-client shell: a fixed window that never scrolls — menu bar, one-row toolbar with measured overflow, parameter row, activity rail of the six document areas plus Export as a dialog, Navigator and Properties docks, document tabs over one viewport, a tabbed bottom panel and a status bar — with the v2 content re-homed per vignette. Illustrative throughout; no kernel, solver, file I/O or model call.",
      "tags": [
        "mockup",
        "hydrofoil",
        "shell",
        "thick-client",
        "setup",
        "cad",
        "analysis",
        "experiment",
        "run",
        "results",
        "export",
        "v3"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v2",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v3",
          "rel": "relates-to"
        },
        {
          "to": "thick-client-shell",
          "rel": "relates-to"
        },
        {
          "to": "decision-seven-areas",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "49df9692296702a8b534a852c6692bc6c835e03907ac798aec8da4cf7910a6cf"
    },
    {
      "id": "mockup-workbench-v4",
      "path": "docs/mockups/workbench-v4.md",
      "title": "CFD-Workbench interactive design mockup v4 — CAD editing views",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "The v3 thick-client shell with the CAD editing views of specification 1.2: an icon rail, splines everywhere, one camera with named views, a view cube and free orbit, editing elevations (Top over Front, Starboard beside) where the outline rails, dihedral/anhedral, twist and thickness are explicit control curves, and a Station document that replaces the modal section editor. Illustrative throughout; no kernel, solver, file I/O or model call.",
      "tags": [
        "mockup",
        "hydrofoil",
        "cad",
        "camera",
        "elevations",
        "control-curves",
        "station",
        "splines",
        "v4"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v3",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v4",
          "rel": "relates-to"
        },
        {
          "to": "cad-editing-views",
          "rel": "relates-to"
        },
        {
          "to": "thick-client-shell",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "914ca8fcf281457956d7f62cc75bbea3da3a711651c2603d02740ed9768d97cb"
    },
    {
      "id": "mockup-workbench-v5",
      "path": "docs/mockups/workbench-v5.md",
      "title": "CFD-Workbench interactive design mockup v5 — control-vertex splines, four viewports, a tool palette",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2027-03-21",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "The v4 shell and camera with the CAD experience rebuilt around specification 1.3's control-vertex record: every master curve is a clamped B-spline whose vertices and levers are the editing surface (a vertex pulls the curve and never lies on it), a four-viewport lines-drawing workspace with title menus and maximise, a nine-verb tool palette with an options strip, a display cage for the 3D body, and a station document whose conversion residual is measured. Illustrative throughout; no kernel, solver, file I/O or model call.",
      "tags": [
        "mockup",
        "hydrofoil",
        "cad",
        "control-vertex",
        "splines",
        "levers",
        "viewports",
        "palette",
        "cage",
        "v5"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v4",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v5",
          "rel": "relates-to"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        },
        {
          "to": "cad-editing-views",
          "rel": "relates-to"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "84b9b6c2fa3973d64e17071db310cf60caec9373fe56e5990a72b5762670f277"
    },
    {
      "id": "mockup-workbench-v6",
      "path": "docs/mockups/workbench-v6.md",
      "title": "CFD-Workbench v6 — FoilDSL and spatial authoring",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "The four-viewport workbench gains a FoilDSL source document, validation, shared transactions, file round-trip and revision freshness. A bounded language prototype, not the product evaluator or a CFD solver.",
      "tags": [
        "mockup",
        "foildsl",
        "cad",
        "language"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "design-foildsl-authoring",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v5",
          "rel": "supersedes"
        },
        {
          "to": "review-foildsl-independent",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d5490f06678a31eb867388ad3076f514ead42c72930e1c04ab99efa88c6c4b92"
    },
    {
      "id": "mockup-workbench-v7",
      "path": "docs/mockups/workbench-v7.md",
      "title": "CFD-Workbench v7 — section scope and design alternatives",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "The spatial workbench adds persistent section editing, shared and independent profiles, draft-safe inspection, explicit chord/span intent, and page-session alternatives with baseline comparison and decision rationale. Geometry remains sampled; scientific, native persistence and full language conformance are not proven.",
      "tags": [
        "mockup",
        "foildsl",
        "cad",
        "profiles",
        "alternatives"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "decision-design-iteration",
          "rel": "depends-on"
        },
        {
          "to": "design-authoring-decisions",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v6",
          "rel": "supersedes"
        },
        {
          "to": "proof-authoring-decisions",
          "rel": "relates-to"
        },
        {
          "to": "review-authoring-v7-independent",
          "rel": "relates-to"
        },
        {
          "to": "review-authoring-v7-gaps",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0d51a7b7dc7f0e89262462c345b2ee7c6c09fc7a8a11164dd45f36578ff45fcf"
    },
    {
      "id": "mockup-workbench-v8",
      "path": "docs/mockups/workbench-v8.md",
      "title": "CFD-Workbench v8 — CAD-first direction (curves and points)",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "A CAD-first rethink at the Fusion 360 / Shape3D bar: open or create a foil, edit it in model-filling views, then enter the section editor as a mode. Every curve is on-curve points with tangent handles; ends are named points with typed values. Direction evidence only — not native proof.",
      "tags": [
        "mockup",
        "cad",
        "direction",
        "section-editor",
        "points",
        "handles"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "refines"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "supersedes"
        },
        {
          "to": "review-ui-workbench-v8",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "203241043875cece7f34a03058bc0ff6ab1ec0907137cfe7d87b9d3e58d33a3d"
    },
    {
      "id": "mockup-workbench-v9",
      "path": "docs/mockups/workbench-v9.md",
      "title": "CFD-Workbench v9 — docked panes and a Properties pane",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "v8's CAD surface with VS Code / Premiere Pro window management: one narrow left panel holds a selection-driven Properties pane by default; the right side bar and bottom panel are optional; panes tab, dock, float and maximize; workspaces are task presets. Direction evidence only — native floats and menus are build requirements.",
      "tags": [
        "mockup",
        "cad",
        "docking",
        "properties",
        "window-management",
        "workspaces"
      ],
      "links": [
        {
          "to": "mockup-workbench-v8",
          "rel": "supersedes"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "review-ui-workbench-v9",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6a5d3930c06932ef21913cf5402e8d429d1d2d776f1cb5b689fe0967fbb28283"
    },
    {
      "id": "workbench-direction",
      "path": "docs/design/workbench-direction.md",
      "title": "CFD-Workbench — interface direction",
      "type": "design",
      "status": "in-review",
      "owner": "Product and UX",
      "phase": "specify",
      "reviewBy": "2027-03-18",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        },
        {
          "by": "design-language",
          "on": "2026-09-19",
          "reason": "Initial cross-platform workbench token and interaction language created for review."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Words-first creative direction for the hydrofoil workbench, extended on 2026-09-20 with the v1 elevation brief (candid, not reassuring; the basis travels with the number). A bounded parametric canvas joins scalar span distributions and station section anchors in one model, with precision editing and visible evidence limits.",
      "tags": [
        "hydrofoil",
        "ui",
        "direction"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "refines"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "eae7d84bb0b644648e74cd377ca1f2495fc7a1ced55e57395069f744bca4dc3e"
    },
    {
      "id": "design-language",
      "path": "docs/design/design-language.md",
      "title": "CFD-Workbench design language",
      "type": "design-language",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-18",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "M1.2b2 adds token foil-shade-lit (#3f6a6c), the Shaded surface and Elevation lane rows and updates the View cube row in DESIGN.md."
        }
      ],
      "summary": "Discoverability hub for the root DESIGN.md token system and its visual preview. Tokens remain authored once in DESIGN.md; this hub does not duplicate them.",
      "tags": [
        "design-language",
        "ui",
        "tokens"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "implements"
        },
        {
          "to": "workbench-direction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bc50aa8c224aad260b8953517159013c58ce9f011823e1e05c720bb3700f3faf"
    },
    {
      "id": "audit-log",
      "path": "docs/audit/audit-log.md",
      "title": "Audit & Change Log",
      "type": "doc",
      "status": "accepted",
      "owner": "@maintainers",
      "phase": "",
      "reviewBy": "2027-09-19",
      "reviewSuggested": [],
      "summary": "The durable, committed history of what was prompted, done, and decided in this repository, so work compounds across sessions. The two JSONL files are the source of truth; audit-data.js and index.html are derived projections.",
      "tags": [
        "audit",
        "history",
        "change-log",
        "project-memory"
      ],
      "links": [],
      "diagrams": [],
      "sourceSha256": "1c1aaf45768e0e5c215188fe18896d01b08fce3c69e6d755e164c79632953db1"
    },
    {
      "id": "defect-classes",
      "path": "docs/lessons/defect-classes.md",
      "title": "CFD-Workbench defect-class register",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        },
        {
          "by": "mockup-workbench-v1",
          "on": "2026-09-20",
          "reason": "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Design-time failure classes and their mandatory checks, loaded at session grounding under AGENTS.md. Product-runtime controls remain explicitly pending until the corresponding implementation exists.",
      "tags": [
        "lessons",
        "controls",
        "geometry",
        "specification"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "021727106fad3bc16d0484bc49a075a4bf5f0831e6b6499385fbc6aaa5116b43"
    },
    {
      "id": "domain-experts",
      "path": "docs/domain-experts.md",
      "title": "CFD-Workbench domain-expert roster",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2027-03-19",
      "reviewSuggested": [],
      "summary": "Seven subject-matter lenses derived from the repository's own evidence and the hydrofoil knowledge base: hydrodynamicist, CFD and numerical verification, computational geometry, marine CAD interaction, structures and materials, manufacturing and CAM, and design optimization. Each is a §8-conformant dual-mode card with a proportional veto; nine candidate roles were merged or rejected with reasons.",
      "tags": [
        "personas",
        "domain-experts",
        "hydrofoil",
        "roster"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "plan-knowledge-experts-spec-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "993e63742245c5842ec3872b2bbe9d9342d356d7d8895c22c964c92a94e7cd30"
    },
    {
      "id": "examples-foildsl",
      "path": "docs/examples/foildsl/README.md",
      "title": "FoilDSL language conformance examples",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-03-22",
      "reviewSuggested": [
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Complete foil and section examples, invalid documents and precision/comment variants with explicit expected outcomes. A reproducible probe records the supplied v3 checker behavior; normative 4.0 fixtures are acceptance vectors, not a claim of an implemented production evaluator.",
      "tags": [
        "foildsl",
        "fixtures",
        "conformance"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "decision-foildsl-reconciliation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ece9151e6db29e8085315292b585b42689c9cec3d9d8c52a65157fbc62257096"
    },
    {
      "id": "investigation-desktop-launch-abort",
      "path": "docs/investigations/desktop-launch-abort.md",
      "title": "Desktop test harness aborts with SIGABRT under the application gates",
      "type": "doc",
      "status": "in-review",
      "owner": "@track-crash",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Under `dotnet CfdWorkbench.Desktop.Tests.dll`, the harness relaunched Environment.ProcessPath (the dotnet muxer) with only `--section-flow`. The child ran `dotnet --section-flow` and exited 1, and the unhandled exception aborted the process, which wrote a macOS crash report. Fixed with a launch-shape-aware relaunch and a named exit 70 for unhandled exceptions. The core gate's separate \"live owned descendants\" failure is Avalonia's build telemetry collector.",
      "tags": [
        "application",
        "desktop",
        "test-harness",
        "gates",
        "investigation"
      ],
      "links": [
        {
          "to": "design-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "adr-application-stack",
          "rel": "depends-on"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a67325917405618785960aa40f8d95cf4537ea449afa3183e58896e325cbfd15"
    },
    {
      "id": "investigation-native-save-permissions",
      "path": "docs/investigations/native-save-permissions.md",
      "title": "Native save creates a file with unintended permissions",
      "type": "doc",
      "status": "in-review",
      "owner": "@cfd-application-20260923",
      "phase": "implementation",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [],
      "summary": "A real macOS Save created mode 0454 despite the store requesting 0600. A controlled ABI probe isolates fixed-versus-variadic argument passing; the managed production repair still requires independent native proof.",
      "tags": [
        "application",
        "persistence",
        "native-interop",
        "investigation"
      ],
      "links": [
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-application-native",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "316f3457566f8e9d4a0c50e1ce02bb1b12a707e7a3dd40dfd4e7054a3de86125"
    },
    {
      "id": "plan-application-build",
      "path": "docs/plans/application-build.md",
      "title": "Coordinated application build execution graph",
      "type": "doc",
      "status": "in-progress",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "A gated, bounded graph for the first working offline CFD-Workbench slice and later dependency-ready increments.",
      "tags": [
        "application",
        "coordination",
        "execution-graph"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Graph and mandatory floors",
          "mermaid": "flowchart LR\nG0 --> G1 --> G2 --> G3\nG3 --> G4 --> G6\nG3 --> G5 --> G6\nG6 -. new session only .-> G7"
        }
      ],
      "sourceSha256": "79e964ce5f0a6131ea249ba4877ef35116dba15b16cfb92d35390012483d1f51"
    },
    {
      "id": "plan-authoring-decisions",
      "path": "docs/plans/authoring-decisions.md",
      "title": "Complete the authoring decisions and review remaining gaps",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Bounded T2 continuation covering section discovery, edit scope, inspection, alternatives and edit intent.",
      "tags": [
        "plan",
        "foildsl",
        "ux"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "plan-foildsl-authoring",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Graph and ownership",
          "mermaid": "flowchart TD\n G --> C\n C --> S\n C --> U\n C --> T\n S --> V\n U --> V\n T --> V\n V --> R\n R --> J"
        }
      ],
      "sourceSha256": "fea1a95fa83dafa9631a491ba7e99a607d3ab747ac241c939a509691158569f9"
    },
    {
      "id": "plan-foil-editing-flow-results",
      "path": "docs/plans/foil-editing-flow-results.md",
      "title": "Editable foils and flow-result iteration plan",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        }
      ],
      "summary": "Extend the existing specification and interactive prototype with weighted geometry editing, water-aware analysis, two-axis simulation sweeps, and synchronized field inspection. Independent review and rendered behavior checks bound the delivery claim to specification and prototype evidence.",
      "tags": [
        "planning",
        "geometry",
        "simulation",
        "visualization"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution graph",
          "mermaid": "flowchart LR\nG[Ground sources] --> P[Review plan]\nG --> S[Model and specification]\nS --> D[Direction and state contract]\nD --> U[Interactive mockup]\nS --> H[Render spec]\nP --> V[Independent rendered review]\nU --> V\nH --> V\nV --> C[Checks, graph and audit]"
        }
      ],
      "sourceSha256": "55fab0f48f8e1e08783bd946080a3b7cdb614c47fe137011593890e38968ed64"
    },
    {
      "id": "plan-foildsl-authoring",
      "path": "docs/plans/foildsl-authoring.md",
      "title": "FoilDSL specification and workbench evolution",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Bounded T2 plan to reconcile the supplied language with the control-vertex workbench, publish the normative contract, and demonstrate its transactions without implementing the product.",
      "tags": [
        "plan",
        "foildsl",
        "specification",
        "mockup"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution graph",
          "mermaid": "flowchart TD\n  G[Ground references and baseline] --> P[Independent plan review]\n  P --> C[Settle language and model]\n  C --> S[Product spec and ADR]\n  S --> U[Evolve interactive mockup]\n  U --> V[Execute verification]\n  V --> R[Independent artifact review]\n  R --> J[Join and review handoff]"
        }
      ],
      "sourceSha256": "381158f5628b3b25a0947736f892f07e5b86d26749e0c43dc00f53460312c41e"
    },
    {
      "id": "plan-foildsl-publish",
      "path": "docs/plans/foildsl-publish.md",
      "title": "Publish the reviewed FoilDSL work",
      "type": "doc",
      "status": "approved",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [],
      "summary": "Fast-forward integration and remote verification of the reviewed FoilDSL specification and mockup.",
      "tags": [
        "plan",
        "git"
      ],
      "links": [
        {
          "to": "plan-authoring-decisions",
          "rel": "relates-to"
        },
        {
          "to": "proof-authoring-decisions",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution graph",
          "mermaid": "flowchart LR\n A --> B --> C --> D"
        }
      ],
      "sourceSha256": "d40fef2158e997b9ae4658dac9da63f9531003c95d4c51fff4123a022112df54"
    },
    {
      "id": "plan-independent-edges",
      "path": "docs/plans/independent-edges.md",
      "title": "Independent leading and trailing edge correction",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Correct the authored planform to independent leading and trailing rails, derive chord, and prove the untouched rail remains unchanged through edits and history.",
      "tags": [
        "plan",
        "geometry",
        "foildsl"
      ],
      "links": [
        {
          "to": "mockup-workbench-v6",
          "rel": "relates-to"
        },
        {
          "to": "spec-foildsl",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Independent edges",
          "mermaid": "flowchart LR\n G --> C\n C --> D\n C --> U\n D --> V\n U --> V\n V --> R\n R --> J"
        }
      ],
      "sourceSha256": "2fe14b0815e3579130998f28954eb6974b3399ad2b6943f4dad624672d3b7e39"
    },
    {
      "id": "plan-knowledge-experts-spec-v1",
      "path": "docs/plans/knowledge-experts-spec-v1.md",
      "title": "Knowledge base, domain experts, build-basis specification and elevated mockups",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "One turn, four skills. Deep research across fourteen areas becomes a sourced knowledge base; the base grounds a domain-expert roster; the roster and the base drive a critique of specification 0.2 and a new build-basis specification; the new specification drives an elevated mockup. Planned versus actual is recorded at close.",
      "tags": [
        "planning",
        "knowledge",
        "personas",
        "specification",
        "ui-design"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution graph",
          "mermaid": "flowchart LR\nG --> R1 --> K\nG --> R2 --> K\nG --> R3 --> K\nK --> E --> C --> S --> D --> U --> X\nK --> X\nE --> X\nS --> X"
        }
      ],
      "sourceSha256": "6c59fc20555b4c53c4e869f806b11ffb9c792d04016d9df7688e9b45e8814bcb"
    },
    {
      "id": "plan-specification-and-ui",
      "path": "docs/plans/specification-and-ui.md",
      "title": "CFD-Workbench specification and interface plan",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        }
      ],
      "summary": "Produce a full three-layer product specification and a reviewable desktop design from the proposal sequence and CFD-Bench knowledge. Evidence, independent review, rendered proof, and discoverability are explicit completion gates.",
      "tags": [
        "planning",
        "specification",
        "ui"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "audit-log",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Graph",
          "mermaid": "flowchart LR\nG[Source grounding] --> P[Plan review]\nG --> S[Functional and UX specification]\nS --> R[Independent gate]\nP --> R\nR --> D[Direction and tokens]\nD --> U[Mockup]\nU --> V[Rendered review]\nR --> H[Specification HTML]\nV --> C[Checks and audit]\nH --> C"
        }
      ],
      "sourceSha256": "b80e0ce647471b451674c0f740bd2dab113675824867a2e1f2e540cb59aeef97"
    },
    {
      "id": "review-authoring-v7-gaps",
      "path": "docs/reviews/authoring-v7-gaps.md",
      "title": "Remaining specification and UX decisions after v7",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "A bounded post-change review separates three remaining design decisions from already declared validation and production obligations. No additional implementation is authorized or performed by this review.",
      "tags": [
        "review",
        "ux",
        "geometry",
        "foildsl",
        "gaps"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        },
        {
          "to": "decision-design-iteration",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "991423f295c46b0298f878890a83fd106452da094d9e57b870640d0d7f1b5620"
    },
    {
      "id": "review-authoring-v7-independent",
      "path": "docs/reviews/authoring-v7-independent.md",
      "title": "Authoring decisions v7 — independent review",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Independent adversarial review of the five authorized authoring improvements, with runtime geometry, transaction, keyboard and rendered-surface evidence. Native persistence, certified geometry and formative usability remain explicitly unverified.",
      "tags": [
        "review",
        "ux",
        "geometry",
        "testing",
        "foildsl"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        },
        {
          "to": "decision-design-iteration",
          "rel": "relates-to"
        },
        {
          "to": "review-authoring-v7-gaps",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cda5910b12c1444c1d1448485de6a49156906ac8dacae775efba846007a4782f"
    },
    {
      "id": "review-foildsl-independent",
      "path": "docs/reviews/foildsl-independent.md",
      "title": "FoilDSL authoring — independent review",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Independent review of the FoilDSL contract and authoring experience. Separates observed baseline evidence, pre-build contract findings and final rendered-surface gates from unverified production obligations.",
      "tags": [
        "foildsl",
        "review",
        "geometry",
        "data",
        "security",
        "accessibility"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v5",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v6",
          "rel": "relates-to"
        },
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "plan-foildsl-authoring",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "19b2415080b3bbbc0ba2f9eb3e7c2be17d261a55ce0099dc7b56c31ba0f68782"
    },
    {
      "id": "review-independent-edges",
      "path": "docs/reviews/independent-edges.md",
      "title": "Independent foil edges — adversarial review",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        }
      ],
      "summary": "Independent geometry, test and simplification gates for separately authored leading and trailing planform rails. Includes observed browser proof, resolved findings and bounded residual risks.",
      "tags": [
        "foildsl",
        "review",
        "geometry",
        "testing"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v6",
          "rel": "relates-to"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "09bc219f3ec625d9029e88ced4f4253d3a6930b26739c8ef436214a076d3e6e2"
    },
    {
      "id": "review-property-grid-native",
      "path": "docs/reviews/property-grid-native.md",
      "title": "Property grid — the operator's native checklist (B2, B4, B7, B8)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "The acceptance rows of the property-grid build that only a native session can prove: the VoiceOver trace and AX dump (B2), rendered contrast in three themes at 100 % and 200 % (B4), the field-nudge close gates (B7, B8), and the shell's Return conflict found and fixed by the build. Each row says what to do, what to hear or see, and what passes. The headless build tests cover B1, B3, B5, B6, B9 and B10.",
      "tags": [
        "ui-review",
        "properties",
        "property-grid",
        "native-ui",
        "accessibility",
        "voiceover",
        "narrator",
        "contrast",
        "m1.2b"
      ],
      "links": [
        {
          "to": "review-ui-property-grid",
          "rel": "refines"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "12036aff00a5003138a5c82f1019de0f6f12612c27aadc33cc5a2a85cf05fbc7"
    },
    {
      "id": "review-proposal-gap-reconciliation",
      "path": "docs/reviews/proposal-gap-reconciliation.md",
      "title": "Final proposal gaps — requirement reconciliation",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        }
      ],
      "summary": "Maps the completed Grok proposal's explicit CAD, section-analysis and wing-analysis v1 tables into acceptance criteria. Conflicting earlier wording and all eight final open questions have explicit dispositions.",
      "tags": [
        "proposal",
        "traceability",
        "gaps"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2b8da62f2a38a7cd01584a657c60babad502999f9e1db69f28d96fa11a297c23"
    },
    {
      "id": "review-ui-property-grid",
      "path": "docs/reviews/ui-property-grid.md",
      "title": "UI review — the Properties pane as a property grid (F-1)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "Elevate-mode review of the M1.2b Properties pane (build c43711a). Measured first: three equal headings, four value x-positions in one block, four of twelve quantities without a unit, nine of nine dark tokens drifted from DESIGN.md, input boundaries at 1.49:1 and no visible current tangent kind. Verdict BLOCK on the as-built pane. The fix is one reusable property grid (identity, groups, label | value | unit rows); building it is the highest-leverage change, and the implementation brief in section 10 maps it to Avalonia for the M1.2b fix track and M1.2b2's PNL track.",
      "tags": [
        "ui-review",
        "properties",
        "property-grid",
        "native-ui",
        "accessibility",
        "m1.2b",
        "m1.2b2"
      ],
      "links": [
        {
          "to": "mockup-property-grid",
          "rel": "documents"
        },
        {
          "to": "review-m12b-native",
          "rel": "refines"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e6a380b177639968240f5526414c289403d5eafeb84b7956fb9525f7139dbc4b"
    },
    {
      "id": "review-ui-property-grid-density",
      "path": "docs/reviews/ui-property-grid-density.md",
      "title": "UI review — property grid density pass (after the native build)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "Elevate-mode density pass on the built property grid (dade554). The operator found the sheet \"too large\". The cause is measured in source: values at 14 px beside 12 px labels, rows at 28/32 px, 33 px group headers and 32 px Kind options from Avalonia Fluent defaults, 12 px insets, a value column far from its label. The proposal (after repair cycle 1 and the operator's 11 px ruling) is one 11/14 type size with nothing below 11, 18/24 px rows with a 24 px target around a 20 px field, 24 px headers, an app Text size setting, and the value next to its label. On the anchor state, selection content falls from 625 to 423 px and the Wing from 339 to 231 px, with every WCAG floor kept.",
      "tags": [
        "ui-review",
        "properties",
        "property-grid",
        "density",
        "native-ui",
        "accessibility",
        "typography"
      ],
      "links": [
        {
          "to": "mockup-property-grid",
          "rel": "documents"
        },
        {
          "to": "review-ui-property-grid",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "46fa25b9c4998abf5d10fc9450855bcba33b6502572f86680506ee2686edcc99"
    },
    {
      "id": "review-ui-workbench-v1",
      "path": "docs/reviews/ui-workbench-v1.md",
      "title": "UI review — workbench mockup v1",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-20",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v1",
          "on": "2026-09-20",
          "reason": "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevate-mode review of the v1 interactive mockup against specification v1. The independent UX & Accessibility lens returned BLOCK on the first pass (focus loss on nudge, handles under role=img, page-wide live region, sub-12 px chart text, NaN in the error state), PASS-WITH-CONDITIONS on the second, and PASS (veto cleared) after the conditions were applied and re-measured. Highest-leverage change: re-query the SVG handle after every rerender so keyboard editing survives — one line per editor that unblocked the keyboard-only persona entirely.",
      "tags": [
        "ui-review",
        "ux",
        "accessibility",
        "mockup",
        "v1"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "workbench-direction",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a7ebc005eaaefd17e71b9a0c57616f19cafde1535f26245d659123d057931e03"
    },
    {
      "id": "review-ui-workbench-v2",
      "path": "docs/reviews/ui-workbench-v2.md",
      "title": "UI review — workbench mockup v2 (seven areas)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v2",
          "on": "2026-09-20",
          "reason": "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevate-mode review of the seven-area mockup against specification v1.1. The independent UX & Accessibility lens returned BLOCK on the first pass (layer names presentational under role=img, a bare character-key shortcut, a false inequality on the candidate card, and a Major list across state completeness, copy truth and the marine CAD idiom) and PASS-WITH-CONDITIONS with the veto cleared after the fixes were applied and re-measured. Highest-leverage change: the outer SVGs of the plan view and the Results viewport became role=group, which exposed every authored layer name to assistive technology with one attribute in two places.",
      "tags": [
        "ui-review",
        "ux",
        "accessibility",
        "mockup",
        "v2"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v2",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "workbench-direction",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v1",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3e30dc4065bcd9216a110dcf093cb0eb6d96966d7fbdc47cf05515f0daebd63f"
    },
    {
      "id": "review-ui-workbench-v3",
      "path": "docs/reviews/ui-workbench-v3.md",
      "title": "UI review — workbench mockup v3 (thick-client shell)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v3",
          "on": "2026-09-20",
          "reason": "Mockup v3 (thick-client shell) supersedes v2 as the review artifact; shell contract proven by tools/check-mockup-v3.mjs; UI-23 and the activity rail in spec 1.1a."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevate-mode review of the thick-client shell rebuild. The v2 page was measured first (1,450–6,500 px tall, a wrapping area strip, a clipping toolbar); the v3 shell was built to a shell contract proven by its oracle at five window presets × six areas. The independent UX & Accessibility lens returned BLOCK on its first read (a clipped overflow menu, a 0-px bottom panel at the reflow preset, focus dropped on re-render, composite roles without keyboards, one-way dock collapse) and the Native Desktop lens PASS-WITH-CONDITIONS (sashes, maximize, real document tabs, the macOS title bar, platform key labels); both sets were built and are observed by the oracle. The veto cleared on the third read; the review artifact passes.",
      "tags": [
        "ui-review",
        "ux",
        "accessibility",
        "desktop",
        "mockup",
        "v3"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v3",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "workbench-direction",
          "rel": "relates-to"
        },
        {
          "to": "thick-client-shell",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v2",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d6a5f3fa8ab984de0c1ce0582c58592c49f5d648e993b275685cdaa8e4a6c022"
    },
    {
      "id": "review-ui-workbench-v4",
      "path": "docs/reviews/ui-workbench-v4.md",
      "title": "UI review — workbench mockup v4 (CAD editing views)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v4",
          "on": "2026-09-20",
          "reason": "Mockup v4 (CAD editing views) supersedes v3; spec 1.2 CAD-04–06, UX-23, UI-24–25; oracle tools/check-mockup-v4.mjs."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevate-mode review of the CAD editing views (icon rail, splines, one free camera with named views and a view cube, editing elevations for the four control curves, the Station document) against specification 1.2. Two independent lenses: UX & Accessibility (hard veto) on the surface and UX Researcher / IA (UX-specification veto) on the 1.2 stories; both cleared their vetoes after two fix passes, with every clearing observation now an oracle assertion whose values the proof records.",
      "tags": [
        "ui-review",
        "ux",
        "accessibility",
        "cad",
        "mockup",
        "v4"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v4",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "workbench-direction",
          "rel": "relates-to"
        },
        {
          "to": "cad-editing-views",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v3",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cf10e04b509500687491d0bad41060b567ec4482d3bb1b35ef42071f34d29b15"
    },
    {
      "id": "review-ui-workbench-v5",
      "path": "docs/reviews/ui-workbench-v5.md",
      "title": "UI review — workbench mockup v5 (control-vertex splines, four viewports, a tool palette)",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-21",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v5",
          "on": "2026-09-21",
          "reason": "Mockup v5 (control-vertex splines, four viewports, tool palette) supersedes v4; spec 1.3 GEO-03/05/13/15, CAD-01/04/06/07/08, A4.2, A4.12, UX-24, UI-25–27; oracle tools/check-mockup-v5.mjs"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevate-mode review of the v5 CAD experience (control-vertex splines with levers, four viewports with title menus, a nine-verb tool palette and options strip, the display cage, the measured station residual) against specification 1.3. Four independent lenses: Computational Geometry and UX Researcher / IA on the spec delta, UX & Accessibility (hard veto) and Marine CAD UX on the artifact. All four returned BLOCK or PASS-WITH-CONDITIONS on first read; every Blocker, Major and condition was fixed in place and became an oracle row whose value the proof records. The accessibility veto cleared on the second pass; the marine veto on the third.",
      "tags": [
        "ui-review",
        "ux",
        "accessibility",
        "geometry",
        "marine-cad",
        "mockup",
        "v5"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v5",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "workbench-direction",
          "rel": "relates-to"
        },
        {
          "to": "control-vertex-workspace",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-workbench-v4",
          "rel": "supersedes"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bda163aa9a70b7e01905e5acaeef3cb7794aedc28249ce879e32c88607a99360"
    },
    {
      "id": "rulings",
      "path": "docs/notes/rulings.md",
      "title": "Rulings — the Owner seat's numbered decisions (the only definition site)",
      "type": "doc",
      "status": "accepted",
      "owner": "@owner",
      "phase": "coordination",
      "reviewBy": "2027-03-22",
      "reviewSuggested": [],
      "summary": "The ruling register. Each `### Ruling NN — <title>` heading defines exactly one numbered decision of the Owner seat; prose anywhere cites it as `Ruling NN`. Written only by `coord decide rule`; numbering is read from these headings; verify-ruling-citations.py fails a cited number with no heading here and a number defined twice.",
      "tags": [
        "coordination",
        "owner-review",
        "rulings",
        "register"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "341c68ae648268670c3d6de92961c4a4037f37da4a64f0f9ce85981bdbca35c3"
    },
    {
      "id": "kb-hw-glossary",
      "path": "docs/knowledge/hydrofoil-workbench/glossary.md",
      "title": "Hydrofoil workbench glossary",
      "type": "glossary",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "The ubiquitous language of hydrofoil design, analysis, simulation and optimization as used by CFD-Workbench, merged alphabetically from every area file. A term defined by more than one area lists every definition so a conflict is visible rather than silently resolved.",
      "tags": [
        "glossary",
        "hydrofoil",
        "generated"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "77bf858e2aa70c85722484f0d28b738e239d0d836e1e0fb662916e0bb3ef0560"
    },
    {
      "id": "kb-visible-presentation-glossary",
      "path": "docs/knowledge/visible-presentation/glossary.md",
      "title": "Visible timing glossary",
      "type": "glossary",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Separates batch completion, OS presentation, observation and physical display response so timing evidence cannot silently change meaning.",
      "tags": [
        "performance",
        "terminology"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5c055ea5c81c886b0dec09537d4a016214010fbe30e377ff7eb5f781b88760d7"
    },
    {
      "id": "investigation-review-window-attach",
      "path": "docs/investigations/review-window-attach.md",
      "title": "Review window attachment and stalled human waits",
      "type": "investigation",
      "status": "in-review",
      "owner": "@cfd-focus-20260924",
      "phase": "",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "plan-application-build",
          "on": "2026-09-24",
          "reason": "Approved M1 Windows and visible-timing gates plus named M1.1 section design changed this execution plan; review dependent status and gates."
        }
      ],
      "summary": "Reproduced global CUA attachment failure and recovered the unchanged process by resetting the supported CUA session. Two consecutive fresh builds passed the reset/readiness protocol without human foregrounding; internal CUA state remains inferred.",
      "tags": [
        "native-ui",
        "coordination",
        "investigation"
      ],
      "links": [
        {
          "to": "coordination-contract-c-native",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-application-native",
          "rel": "relates-to"
        },
        {
          "to": "plan-application-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "49075dfa346e1458edc2a94d4a26bd4852e575a0a1c40545bca947d6dd4273e9"
    },
    {
      "id": "kb-cfd-workbench-grounding",
      "path": "docs/knowledge/cfd-workbench-grounding.md",
      "title": "CFD-Bench and proposal grounding",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        }
      ],
      "summary": "Source map, precedence, confidence and conflicts carried from CFD-Bench and the proposal into Workbench. Includes the follow-up requirements for weighted section/outline editing, water-dependent loads, Cartesian simulation sweeps and scientifically labeled 2D/3D replay.",
      "tags": [
        "hydrofoils",
        "sources",
        "geometry",
        "proposals"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "16011236c5cfaecedfd1e006c2544e810723b956bea848d352ada80cde3fd153"
    },
    {
      "id": "kb-hw-cad-programs-and-ux",
      "path": "docs/knowledge/hydrofoil-workbench/01-cad-programs-and-ux.md",
      "title": "CAD programs and their UI/UX paradigms for a precision parametric foil editor",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes, from vendor documentation and HCI literature, what the major CAD systems do for precision curve editing, navigation, cross-platform behaviour and accessibility, and which of those conventions a bounded parametric foil editor should adopt. Main implication: the spec's Through points / Smooth split is the industry's on-curve-point / control-point dichotomy, the \"few master controls\" rule is both a documented professional rule and a mathematically grounded fairness criterion, and keyboard-first plus preset-based navigation is achievable because every comparable already exposes the same three precision channels (typed value, modifier-stepped nudge, click-to-type gizmo) while none of them is accessible by their own accounts.",
      "tags": [
        "hydrofoil",
        "cad",
        "ux",
        "curves",
        "splines",
        "navigation",
        "accessibility",
        "cross-platform",
        "comparables"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "67a4b83ea1f8a2ee97b0b44844caaaa1cac9253550082cbb3cca06b6cde047ef"
    },
    {
      "id": "kb-hw-comparables",
      "path": "docs/knowledge/hydrofoil-workbench/comparables.md",
      "title": "Comparable solutions and problem framings",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "How existing products, libraries and the literature frame and solve each part of the problem, with what each does well and badly and its licence, compiled from the area files.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "comparables"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0517cab08e221f95c97edb5b38ed0f9e1e03296f198f8933a3f8cfe0e73b2953"
    },
    {
      "id": "kb-hw-data-and-constants",
      "path": "docs/knowledge/hydrofoil-workbench/data-and-constants.md",
      "title": "Domain data, constants, formulae and invariants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Formulae with units and validity envelopes, datasets, constants and invariants per area, compiled from the area files.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "data-and-constants"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1f4e6dc71af40d4cfaf8c81271f3c9d85728696e7d0dc04d09da1066e73ec920"
    },
    {
      "id": "kb-hw-file-formats-and-grammars",
      "path": "docs/knowledge/hydrofoil-workbench/05-file-formats-and-grammars.md",
      "title": "File formats and grammars for curves, surfaces, meshes and CFD data",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes what the workbench must read, write and design for its own native document: the exact Selig/Lednicer .dat layouts and a fail-closed parser contract, the station grammars of AVL, XFLR5, OpenVSP, MachUpX and Shape3d, the one STEP entity (B_SPLINE_SURFACE_WITH_KNOTS in a shell) a CAM round trip needs, the mesh and CFD result formats with their licences, and an evidence-based design for a versioned, JCS-hashed, Git-friendly .cfdw.json with binary sidecars. Main implication: store the explicit definition only, hash a canonical form, embed nothing large, and gate every export on an open-and-measure proof rather than on writing bytes.",
      "tags": [
        "hydrofoil",
        "file-formats",
        "step",
        "dat",
        "mesh",
        "cfd-data",
        "json-schema",
        "provenance",
        "grammars"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "892ddfa491491010d2268c7e0345bc2068fac8d1522d81968cf2a6454dc4adb8"
    },
    {
      "id": "kb-hw-foil-section-catalog",
      "path": "docs/knowledge/hydrofoil-workbench/06-foil-section-catalog.md",
      "title": "Foil section catalog for hydrofoil wings, stabilizers, struts and fins",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes which section families matter for water-sports hydrofoils (Eppler hydrofoil set, NACA 4/16/6-series, Speer H105, low-Re Selig/Drela/Wortmann sections, symmetric strut families), what each one's geometry actually is (measured from the coordinate files), where the authoritative coordinates and polars live and under what terms, and what the evidence says about water Ncrit, cavitation screening and section selection. Main design implication: the v1 catalog must separate \"coordinates we may bundle\" from \"coordinates we generate analytically at build\" from \"link-only, pending permission\" — the UIUC coordinate database carries no stated licence, the UIUC wind-tunnel data is GPL, Airfoil Tools forbids reproduction, and H105 has no published redistribution terms.",
      "tags": [
        "hydrofoil",
        "sections",
        "catalog",
        "provenance",
        "licensing",
        "cavitation",
        "ncrit"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cefe9b7f1d507b8dfbe3e8dc0c75648c1518391aa9c82a09198b4a39cf550fce"
    },
    {
      "id": "kb-hw-hydrofoil-disciplines-and-design-data",
      "path": "docs/knowledge/hydrofoil-workbench/04-hydrofoil-disciplines-and-design-data.md",
      "title": "Hydrofoil disciplines and design data for water-sports foils",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes, per water-sports discipline, the rider/craft system, speed envelope, design-CL and Reynolds bands, real product geometry bands (30+ front wings from official and retailer spec pages), the racing class rules that bound geometry, and the operating physics (free surface, ventilation, cavitation, pumping) with cited numbers. Main design implication: discipline presets and operating points must carry explicit, labelled speed × load × depth × water inputs, and the analysis tier must bound its output against the product table and the cavitation/free-surface envelopes here.",
      "tags": [
        "hydrofoil",
        "disciplines",
        "wingfoil",
        "windfoil",
        "kitefoil",
        "downwind",
        "pump-foil",
        "e-foil",
        "parawing",
        "product-specs",
        "class-rules",
        "operating-points",
        "cavitation",
        "ventilation",
        "free-surface"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0b7f054cb6fa31ba68ac7ef871d87a653d8ce8ddeeee1fce444393b70bb65002"
    },
    {
      "id": "kb-hw-integration-and-ai-workflows",
      "path": "docs/knowledge/hydrofoil-workbench/10-integration-and-ai-workflows.md",
      "title": "Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes how engineering tools compose estimator, polar, VLM, CFD and optimizer tiers (dependency graphs with content-hashed invalidation, job queues, PROV-style run records), how multi-fidelity results are reconciled without averaging (correction models, trust-region model management, verify-at-higher-tier), what the 2024–2026 evidence says ML surrogates and LLM agents can and cannot do for CFD/CAD, and which permissive libraries can build the composition layer in .NET/Rust. Main design implication: v1 should be a salsa/Snakemake-style incremental graph keyed by BLAKE3 content hashes with tier-specific recompute policy, an explicit run manifest per Analysis run, and a model-backed assistant confined to typed proposals validated by deterministic code and gated by a named eval set.",
      "tags": [
        "hydrofoil",
        "integration",
        "workflow",
        "multi-fidelity",
        "provenance",
        "surrogates",
        "llm-agents",
        "mcp",
        "drc",
        "reproducibility"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "68af80e8c809bb7d00b5b79393d11508d5f8665e04b67b1e0e9dd873dedf1f41"
    },
    {
      "id": "kb-hw-low-order-hydrodynamics",
      "path": "docs/knowledge/hydrofoil-workbench/07-low-order-hydrodynamics.md",
      "title": "Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes what each non-CFD analysis tier (closed-form estimator, 2D polar via XFOIL/NeuralFoil, VLM + strip theory) can honestly claim for a water-sports hydrofoil, with formulae, units, validity envelopes, verification fixtures and licences. Main implication: every tier is an attached-flow, deep-water, steady result unless a named correction is applied; cavitation, free-surface, ventilation and junction effects are screening labels, never predictions, and disagreement between tiers is shown, never averaged.",
      "tags": [
        "hydrofoil",
        "hydrodynamics",
        "panel-method",
        "xfoil",
        "neuralfoil",
        "vlm",
        "lifting-line",
        "cavitation",
        "free-surface",
        "ventilation",
        "trim",
        "multi-fidelity",
        "licences"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "27f4c7a213040028526370ef778362e3780000f09684f2f0b927bb76691ff41a"
    },
    {
      "id": "kb-hw-marine-and-board-cad-tooling",
      "path": "docs/knowledge/hydrofoil-workbench/03-marine-and-board-cad-tooling.md",
      "title": "Marine and board CAD tooling: stations, master curves and loft UX",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes, from the vendors' own manuals and source, how Shape3d, AkuShaper, BoardCAD, MultiSurf, Orca3D, Maxsurf, DELFTship, OpenVSP, AVL, XFLR5 and the Rhino/Fusion/Onshape loft commands describe a lofted shape as a few master curves plus sections, and which editing gestures, readouts, loft options and export paths practitioners actually use. Main implication: the spec's five-channel station model is the surfboard/naval \"master curves + slices\" paradigm restated for a wing; keep the 2D curve editors primary, expose a three-word loft vocabulary (straight / through stations / blended), adopt OpenVSP's driver-set and closure names, and add an explicit root-tangent invariant.",
      "tags": [
        "hydrofoil",
        "cad",
        "ux",
        "shape3d",
        "akushaper",
        "multisurf",
        "loft",
        "lines-plan",
        "openvsp",
        "comparables"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f5d15a6cd3c8070622555d130d1571713aaeda0ff33d14649b660a45418acb53"
    },
    {
      "id": "kb-hw-open-questions",
      "path": "docs/knowledge/hydrofoil-workbench/open-questions.md",
      "title": "Open questions and domain failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "What the research could not settle, the cheapest next probe for each, the silent and expensive failure modes of the domain, and the disconfirming views that were sought, compiled from the area files.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "open-questions"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "acfe69aa9dc37db0e475346afb1bdec9719c060fd3b40e3680e0ef521cc57766"
    },
    {
      "id": "kb-hw-optimization-strategies",
      "path": "docs/knowledge/hydrofoil-workbench/09-optimization-strategies.md",
      "title": "Optimization strategies for 2D foil sections and 3D hydrofoil wings",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes how a rider brief becomes a constrained multipoint optimization problem (objectives, operating-point sets, cavitation/structural/manufacturing/class constraints, robustness), which algorithms fit which evaluation cost, what the hydrofoil-optimization literature (Garg/Young 2017–2019, Ng/Yildirim 2025, Drela 1998) proves about optimizer exploitation and multipoint design, and which optimizer tooling is licence-compatible. Main design implication: v1 must model the goal state, constraint set, design vector (= the explicit curve/station definition) and per-evaluation provenance now, so the deferred optimizer can plug in behind COMMIT-01 gates without a second geometry truth.",
      "tags": [
        "hydrofoil",
        "optimization",
        "multipoint",
        "cavitation-constraint",
        "surrogate",
        "multi-fidelity",
        "adjoint",
        "pareto",
        "goal-state",
        "provenance",
        "licences"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ff59b2dfbf1d9122f932d95878cb89bffd52920223f37445e48ccea222f816b9"
    },
    {
      "id": "kb-hw-parametric-curves-lofts-and-surfaces",
      "path": "docs/knowledge/hydrofoil-workbench/02-parametric-curves-lofts-and-surfaces.md",
      "title": "Parametric curves, lofts, splines and surfaces for foil profiles and wings",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes the mathematics the workbench's own geometry kernel must implement: clamped degree-5 B-splines as the editable curve of record, weighted least-squares approximation with KKT-imposed hard constraints as the \"Smooth · weighted controls\" formulation (verified by an executed probe), CST/NACA/ PARSEC/DAT as measured-residual conversions, a channel-driven analytic loft whose B-spline skin is a derived export, and openNURBS-style tolerance semantics. Main implication: the geometry of record is knots + control points + constraints + rule + evaluator version, and every conversion reports a residual.",
      "tags": [
        "hydrofoil",
        "geometry",
        "splines",
        "nurbs",
        "cst",
        "loft",
        "fairing",
        "tolerance",
        "licensing"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "91d52857aaccb17c35bfaa375292cda964239fbc9ea74e58ff77cba5703c69a2"
    },
    {
      "id": "kb-hw-references",
      "path": "docs/knowledge/hydrofoil-workbench/references.md",
      "title": "Reference information",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Standards, specifications, primary documentation and seminal works per area, with what each requires of CFD-Workbench, compiled from the area files.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "references"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "66e16618c7f4523991773796d3d3510c85abdefea3f9feac6507e6153ff1016c"
    },
    {
      "id": "kb-hw-simulation-openfoam-su2-interop",
      "path": "docs/knowledge/hydrofoil-workbench/08-simulation-openfoam-su2-interop.md",
      "title": "Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes which OpenFOAM (ESI v2606 / Foundation 14) and SU2 (v8.5.0) solvers, models and meshes answer which hydrofoil question at Re 5e5–2e6, what the ITTC verification procedure requires before a result may be called converged, and why both solvers must be driven as child processes over generated files (never linked) from C#/Rust. Main design implication: meshing, not solving, is the gating capability; SU2 ships no mesher and no free-surface or cavitation solver, so the v1 backend matrix is a two-solver matrix by physics, not a choice.",
      "tags": [
        "hydrofoil",
        "cfd",
        "openfoam",
        "su2",
        "meshing",
        "interop",
        "installation",
        "validation",
        "licensing"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3fbccea5137db62b8a17f24f634c25c5614fe04f3628a74ed451c6ab121208fb"
    },
    {
      "id": "kb-hw-sources",
      "path": "docs/knowledge/hydrofoil-workbench/sources.md",
      "title": "Sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Every source cited by the area files, with type, URL, access date and the claim it supports; ids are area-prefixed so a citation such as 02·S7 resolves to one row.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "sources"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ef7344b5613400b811f4d240479008197d767be62d23e07f17ffe429dff4eabe"
    },
    {
      "id": "kb-hw-state-of-the-art",
      "path": "docs/knowledge/hydrofoil-workbench/state-of-the-art.md",
      "title": "State of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Current best practice, leading techniques and the research frontier for every area of hydrofoil modeling, analysis, simulation, optimization and workbench design, compiled from the area files.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "generated",
        "state-of-the-art"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "048476ee0e72b95cff848ca7f95c1215029737beb2d99c424be237c2a810c5cb"
    },
    {
      "id": "kb-hw-structures-materials-and-manufacturing",
      "path": "docs/knowledge/hydrofoil-workbench/12-structures-materials-and-manufacturing.md",
      "title": "Structures, materials, manufacturing and safety for water-sports hydrofoils",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes what the user's topic list did not name and the gap register says blocks or reshapes the product: how loads become bending, shear and torsion in a composite foil; that a 1D composite beam coupled to a lifting line/VLM is the published, validated fidelity for static hydroelasticity and bend-twist tailoring; the material, manufacturing and surface-finish floors that bound the design space; and the regulatory finding that no product standard governs hydrofoil wing strength. Main design implication: v1 must model load cases, layup/material and manufacturing policy as typed value objects now (empty and \"Not assessed\" by default), price thickness structurally later through a beam hook, and never let hydrodynamic adequacy read as structural sign-off.",
      "tags": [
        "hydrofoil",
        "structures",
        "hydroelastic",
        "composites",
        "bend-twist",
        "materials",
        "manufacturing",
        "cnc",
        "molds",
        "3d-printing",
        "roughness",
        "safety",
        "standards",
        "cam"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "32e2b62ff090821c079e22a4dbbe53646709a3dec34437d455b69680420e7527"
    },
    {
      "id": "kb-hw-validation-special-physics-and-numerical-testing",
      "path": "docs/knowledge/hydrofoil-workbench/13-validation-special-physics-and-numerical-testing.md",
      "title": "Validation data, special hydrofoil physics and testing numerical design software",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes which measured datasets a hydrofoil design tool can honestly compare against (and what each actually contains), the cavitation, free-surface, ventilation and unsteady/pumping physics a screening tier can and cannot claim, the ITTC/ASME/Roache verification-and-validation vocabulary that makes the A6 label ladder concrete, and the testing practice (analytic references, manufactured solutions, metamorphic and golden-master tests, cross-platform float rules) that must exist before any number is labelled Verified. Main implication: no closed-form free-surface or cavitation correction is Verified today; the tool ships them as Computed estimates with named datasets and the comparison error E and validation uncertainty U_V displayed, never a single fudge factor.",
      "tags": [
        "hydrofoil",
        "validation",
        "cavitation",
        "free-surface",
        "ventilation",
        "unsteady",
        "pumping",
        "verification",
        "uncertainty",
        "testing",
        "numerical-software",
        "gci",
        "ittc",
        "v-and-v"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fe627305cc7bac1b1670fe4a52c9ad571b9f3ee3ba92ab7a8389c6b37f059d8a"
    },
    {
      "id": "kb-hw-visualization",
      "path": "docs/knowledge/hydrofoil-workbench/11-visualization.md",
      "title": "Visualization for hydrofoil design and optimization",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "Establishes the published conventions for foil engineering charts (Cp negative-up, polars, cavitation bucket, spanwise loading), the perceptual rules for colour maps (lightness-monotonic sequential maps, diverging maps only about a physical zero, no rainbow), what each flow-visualization technique may honestly claim (streamlines are steady snapshots; Q/lambda2 find vortex cores, never wall separation; skin-friction lines do), the licences and .NET/Rust reach of ParaView/VTK and the charting/GPU libraries, and the data budget that forces every CFD volume to be reduced to surfaces, slices and precomputed streamlines before it reaches a 16 GB laptop's UI process. Main design implication: v1 renders charts and reduced surfaces/slices/streamlines in-process on a permissive stack (ScottPlot + own OpenGL/wgpu viewport), reduces with a pvbatch/foamToVTK sidecar, and hands the full volume to an external ParaView; every view carries a provenance strip and an Illustrative/Computed label.",
      "tags": [
        "hydrofoil",
        "visualization",
        "charts",
        "colormaps",
        "flow-visualization",
        "paraview",
        "vtk",
        "replay",
        "accessibility",
        "optimization"
      ],
      "links": [
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4acb7f9ec51e6cf3148d47c1c40e3f2a39218b5362b646d233e0f47f12610f67"
    },
    {
      "id": "kb-hydrofoil-workbench",
      "path": "docs/knowledge/hydrofoil-workbench/index.md",
      "title": "Hydrofoil workbench — domain knowledge base",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "knowledge",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [],
      "summary": "The sourced, confidence-labelled evidence base for designing, analysing, simulating and optimising water-sports hydrofoils in a Mac/Windows workbench: thirteen area files, 591 sources, 200 glossary terms. Headline: the spec's five-channel station model is the established marine paradigm and its weighted-control mode has an executed mathematical formulation; the analysis tiers may claim only Computed estimate until per-method fixtures exist; the catalog's bundled coordinates have no established redistribution right; depth and Froude number are mandatory operating-point inputs; and structures, manufacturing and safety must be modelled as vocabulary now even though no solver consumes them in v1.",
      "tags": [
        "hydrofoil",
        "knowledge",
        "index",
        "cad",
        "geometry",
        "hydrodynamics",
        "cfd",
        "optimization",
        "visualization",
        "structures",
        "validation"
      ],
      "links": [
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "refines"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "relates-to"
        },
        {
          "to": "kb-hw-cad-programs-and-ux",
          "rel": "documents"
        },
        {
          "to": "kb-hw-parametric-curves-lofts-and-surfaces",
          "rel": "documents"
        },
        {
          "to": "kb-hw-marine-and-board-cad-tooling",
          "rel": "documents"
        },
        {
          "to": "kb-hw-hydrofoil-disciplines-and-design-data",
          "rel": "documents"
        },
        {
          "to": "kb-hw-file-formats-and-grammars",
          "rel": "documents"
        },
        {
          "to": "kb-hw-foil-section-catalog",
          "rel": "documents"
        },
        {
          "to": "kb-hw-low-order-hydrodynamics",
          "rel": "documents"
        },
        {
          "to": "kb-hw-simulation-openfoam-su2-interop",
          "rel": "documents"
        },
        {
          "to": "kb-hw-optimization-strategies",
          "rel": "documents"
        },
        {
          "to": "kb-hw-integration-and-ai-workflows",
          "rel": "documents"
        },
        {
          "to": "kb-hw-visualization",
          "rel": "documents"
        },
        {
          "to": "kb-hw-structures-materials-and-manufacturing",
          "rel": "documents"
        },
        {
          "to": "kb-hw-validation-special-physics-and-numerical-testing",
          "rel": "documents"
        },
        {
          "to": "kb-hw-glossary",
          "rel": "uses-term"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e369034ec7fc41f5aac86db9473f6754ecca1afa302501fa17fb329e22779f7b"
    },
    {
      "id": "kb-visible-presentation",
      "path": "docs/knowledge/visible-presentation/index.md",
      "title": "Visible presentation timing evidence",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Current metrics measure compositor completion. Documented macOS presentation signals exist, but neither is integrated or qualified; this evidence packet defines the remaining measurement seams without claiming a timing pass.",
      "tags": [
        "performance",
        "native-ui",
        "evidence"
      ],
      "links": [
        {
          "to": "note-m1-scope-decision",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-c-native",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-application-native",
          "rel": "relates-to"
        },
        {
          "to": "kb-visible-presentation-glossary",
          "rel": "uses-term"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e924ddc4fabefa5428a64ac48b51b0e7f090cc8b89839350c54d53f01120025c"
    },
    {
      "id": "kb-visible-presentation-comparables",
      "path": "docs/knowledge/visible-presentation/comparables.md",
      "title": "Comparable timing approaches",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Compares the current batch diagnostic with documented Apple presentation and frame-observation mechanisms without treating any as qualified.",
      "tags": [
        "performance",
        "evidence"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5fa8aa51ba5d3be2819164ffaa0ae1e4bf60525ed5b4f662c36352bc0dc87c6d"
    },
    {
      "id": "kb-visible-presentation-data",
      "path": "docs/knowledge/visible-presentation/data-and-constants.md",
      "title": "Timing units, uncertainty and acceptance data",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Proposes interval-based latency evidence and lists the fixed workload, identity, clock and failure information a future spike must retain.",
      "tags": [
        "performance",
        "measurement"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3578e9f2279e0ae614dfe5989981e984de5cd296ad6520cd103ec258e95328dd"
    },
    {
      "id": "kb-visible-presentation-methods",
      "path": "docs/knowledge/visible-presentation/state-of-the-art.md",
      "title": "Presentation measurement methods",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Distinguishes renderer completion, OS presentation signals and calibrated frame observation for this pinned native application.",
      "tags": [
        "performance",
        "evidence"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "dec8208955e00e45355bdde7c510b5d600ddc56b7ded742466a7fc708c2d5898"
    },
    {
      "id": "kb-visible-presentation-open",
      "path": "docs/knowledge/visible-presentation/open-questions.md",
      "title": "Presentation timing gaps and falsifiers",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Defines unverified integration, hardware and workload seams plus the negative controls required before latency trials count as proof.",
      "tags": [
        "performance",
        "risks",
        "verification"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "dcf8f124d0ac8ed1e8d83dbd26b5c3838d42bb97b1a8bc927e678a4e2554e440"
    },
    {
      "id": "kb-visible-presentation-references",
      "path": "docs/knowledge/visible-presentation/references.md",
      "title": "Presentation timing requirements and references",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Records the approved M1 timing authority and distinguishes it from illustrative prototype and small-fixture diagnostics.",
      "tags": [
        "performance",
        "requirements"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6464201a60a355bd0f2c03619ef409cac7d94183857ff53e4339bf14e405667d"
    },
    {
      "id": "kb-visible-presentation-sources",
      "path": "docs/knowledge/visible-presentation/sources.md",
      "title": "Visible timing source ledger",
      "type": "knowledge",
      "status": "in-review",
      "owner": "@cfd-timing-evidence-20260925",
      "phase": "",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [],
      "summary": "Primary API and pinned implementation sources checked on 25 September 2026; source verification is kept distinct from runtime measurement.",
      "tags": [
        "performance",
        "sources"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8669892cc3300712ec64a124ec982ef2fc4710df3655a7ec1a151580db6a028b"
    },
    {
      "id": "coordination-app-shell-build",
      "path": "docs/coordination/app-shell-build.md",
      "title": "Coordination plan - app-shell build (M1.2a shell and M1.2e floats and layouts)",
      "type": "plan",
      "status": "accepted",
      "owner": "@cfd-leader-fbfa35dc",
      "phase": "",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Schedules the app-shell design's nine tracks plus one review-flag track across Claude Code, Grok and Agy with two concurrent coding lanes, a serial spine of Owner rulings, S8, G0 and D3a, and M1.2b-d and the M1.1b follow-ups as gated later waves.",
      "tags": [
        "coordination",
        "worktrees",
        "parallelism",
        "desktop",
        "shell",
        "m1.2a",
        "m1.2e"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d19a802fe8a9046ed9ee813e7ad2353c7b5778439d8a0388d319f64cd77464ee"
    },
    {
      "id": "coordination-application-build",
      "path": "docs/coordination/application-build.md",
      "title": "Coordination plan - first CFD-Workbench application increment",
      "type": "plan",
      "status": "in-progress",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "plan-application-build",
          "on": "2026-09-23",
          "reason": "Execution readback records B0/G3 gates and serial G4 checkpoints; review coordination timing and claims."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Track accepted architecture and core, isolated permission and adapter repairs, and the independent M1 join gates.",
      "tags": [
        "coordination",
        "worktrees",
        "parallelism",
        "application"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "spec-foildsl",
          "rel": "implements"
        },
        {
          "to": "plan-application-build",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cfba797958fff76505451299da30d6db6280137d3199687d10376a36ed8f5195"
    },
    {
      "id": "coordination-contract-b-core",
      "path": "docs/coordination/contract-b-core.md",
      "title": "Proposed first production core author assignment",
      "type": "plan",
      "status": "accepted",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "adr-application-project-contract",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Active-seat dispatch control and observed serial core checkpoints added; review execution references."
        },
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Ruling 13 freezes one serial first-core implementation track and exactly 18 authored paths, subject to actual worker identity and cache preflight.",
      "tags": [
        "coordination",
        "application",
        "core",
        "implementation"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-b0",
          "rel": "depends-on"
        },
        {
          "to": "design-application-foundation",
          "rel": "depends-on"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        },
        {
          "to": "adr-application-project-contract",
          "rel": "depends-on"
        },
        {
          "to": "coordination-application-cancel-drill",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ec2fdf7147bd79c730d4fd15e4818865354e9e2d3bb6653decaac95fe6f694f9"
    },
    {
      "id": "coordination-contract-b0",
      "path": "docs/coordination/contract-b0.md",
      "title": "Serial application contract-completion author assignment",
      "type": "plan",
      "status": "active",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "plan-application-build",
          "on": "2026-09-23",
          "reason": "Execution readback records B0/G3 gates and serial G4 checkpoints; review coordination timing and claims."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Active-seat dispatch control and observed serial core checkpoints added; review execution references."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Exact isolated author assignment and handback gate for the serial B0 session, schema, identity and persistence contracts before production implementation.",
      "tags": [
        "coordination",
        "application",
        "contracts",
        "design"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "depends-on"
        },
        {
          "to": "plan-application-build",
          "rel": "depends-on"
        },
        {
          "to": "design-application-foundation",
          "rel": "refines"
        },
        {
          "to": "architecture-application",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "aa0d8e3b1d8c04070200458210cc829ab562fc8f2b6476e7e328139f17607a73"
    },
    {
      "id": "coordination-contract-c-api-freeze",
      "path": "docs/coordination/contract-c-api-freeze.md",
      "title": "Native adapter public API freeze at joined core",
      "type": "plan",
      "status": "proposed",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [],
      "summary": "Compiled final-core consumer and measured sparse native projection seam before adapter dispatch.",
      "tags": [
        "coordination",
        "application",
        "desktop",
        "cli",
        "contract"
      ],
      "links": [
        {
          "to": "coordination-contract-c-native",
          "rel": "relates-to"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "depends-on"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b432112527d64f515dc547940a1725874a97939a04f41790ad04691f22d78118"
    },
    {
      "id": "coordination-contract-c-native",
      "path": "docs/coordination/contract-c-native.md",
      "title": "Provisional native desktop and CLI adapter assignment",
      "type": "plan",
      "status": "in-progress",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Ruling 21 conditional native adapter route and UI-T4 preflight require consumer review"
        },
        {
          "by": "investigation-review-window-attach",
          "on": "2026-09-24",
          "reason": "Two fresh native launches passed bounded attachment readiness; internal CUA cause remains inferred."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "A held, exact-path candidate for the first native desktop and CLI adapter after the full core gate.",
      "tags": [
        "coordination",
        "application",
        "desktop",
        "cli"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "depends-on"
        },
        {
          "to": "architecture-application",
          "rel": "depends-on"
        },
        {
          "to": "design-application-foundation",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-c-api-freeze",
          "rel": "depends-on"
        },
        {
          "to": "coordination-application-c-launch",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        },
        {
          "to": "investigation-review-window-attach",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a8fe6a11d37a3e71fe06bf0d627574d5605bc612ca34692e9eb09a1b7f1efcce"
    },
    {
      "id": "coordination-m12b-build",
      "path": "docs/coordination/m12b-build.md",
      "title": "Coordination plan - M1.2b build (CAD point editing on the Plan view)",
      "type": "plan",
      "status": "proposed",
      "owner": "@cfd-leader-fbfa35dc",
      "phase": "",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Schedules the M1.2b design's seven tracks (B0, B1a, B1b, U1a, U1b, U2, U3) behind one precondition track (PRE: the checker's --design flag and the test-class stubs), on Grok, Codex and Claude with at most two concurrent coding tracks, boxes at 3x measured priors, join gates, the 2-cycle cap and a closing operator review.",
      "tags": [
        "coordination",
        "worktrees",
        "parallelism",
        "desktop",
        "core",
        "cad",
        "plan-view",
        "m1.2b"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "implements"
        },
        {
          "to": "architecture-application",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "coordination-app-shell-build",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cb3e6f4176b2e58ff38e6168dac9533f4ee761451a77ebe0214dfd552fee6f5f"
    },
    {
      "id": "coordination-r17-companions",
      "path": "docs/coordination/contract-r17-companions.md",
      "title": "Exact companion assignment for evaluator version 2 and native store rulings",
      "type": "plan",
      "status": "accepted",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "review-application-core",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Ruling 21 conditional native adapter route and UI-T4 preflight require consumer review"
        }
      ],
      "summary": "Root owns 30 exact companion paths for Owner Rulings 17–19 while the serial core author finishes a disjoint store and projection checkpoint.",
      "tags": [
        "coordination",
        "application",
        "foildsl",
        "identity",
        "persistence"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "relates-to"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        },
        {
          "to": "review-application-core",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "00f11106030f8afe3b2eb66526a744b92064898ecc9bf5d4e6a502dd4d6348b3"
    },
    {
      "id": "coordination-windows-runtime-route",
      "path": "docs/coordination/windows-runtime-route.md",
      "title": "Windows x64 M1 runtime route and ownership packet",
      "type": "plan",
      "status": "in-progress",
      "owner": "@cfd-coordinator-20260923",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "coordination-application-build",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-26",
          "reason": "Spec 1.6 revision (§10 proposed): point types as knot multiplicity + FoilDSL 4.1 tangents (ADR-0005), driving dimensions/Wing estimates (ADR-0006), multi-step section draft and gesture commit (ADR-0007), catalog and My sections (ADR-0008), Dock/NativeMenu shell (ADR-0009); DR-9/10/11 raised."
        }
      ],
      "summary": "Route real Windows x64 M1 execution through an unattended hosted runner and a separately qualified interactive desktop, with distinct store, adapter and independent proof ownership. A hosted Windows job ran, but its native qualification failed; interactive and product gates remain open.",
      "tags": [
        "coordination",
        "application",
        "windows",
        "runtime"
      ],
      "links": [
        {
          "to": "note-m1-scope-decision",
          "rel": "depends-on"
        },
        {
          "to": "coordination-application-build",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-c-native",
          "rel": "relates-to"
        },
        {
          "to": "architecture-application",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "29e9139be1d7c2f42645cb1905d23a44d2e00e74f79329be1e320b20d70024a0"
    },
    {
      "id": "privacy-review",
      "path": "docs/security/privacy-review.md",
      "title": "Offline application privacy review",
      "type": "privacy-review",
      "status": "proposed",
      "owner": "@cfd-owner-20260923",
      "phase": "architecture",
      "reviewBy": "2027-03-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Captures identifying source comments, names, local paths and retained recovery/history for the offline slice. No personal-data transfer is introduced; metadata minimization and explicit local retention remain testable implementation obligations rather than assumed properties of the toolkit.",
      "tags": [
        "privacy",
        "application",
        "local-files"
      ],
      "links": [
        {
          "to": "architecture-application",
          "rel": "documents"
        },
        {
          "to": "design-application-foundation",
          "rel": "documents"
        },
        {
          "to": "design-application-contracts",
          "rel": "documents"
        },
        {
          "to": "design-app-shell",
          "rel": "documents"
        },
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8973f192bf4f3fd93dd1a79f4dedbc48c7eff815b809252181c0cf56b584bbb8"
    },
    {
      "id": "coordination-application-c-launch",
      "path": "docs/coordination/application-c-launch.md",
      "title": "Native adapter author launch and monitoring receipt",
      "type": "proof-pack",
      "status": "in-progress",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [],
      "summary": "Actual conditional Sol collaborator launch after joined B and independently reviewed API freeze.",
      "tags": [
        "coordination",
        "application",
        "native",
        "launch"
      ],
      "links": [
        {
          "to": "coordination-contract-c-native",
          "rel": "implements"
        },
        {
          "to": "coordination-contract-c-api-freeze",
          "rel": "depends-on"
        },
        {
          "to": "coordination-application-cancel-drill",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c809d415e38d07ecebb08b622bc50ed4214f14339d3a79388c7d40158c239c46"
    },
    {
      "id": "coordination-application-cancel-drill",
      "path": "docs/coordination/application-cancel-drill.md",
      "title": "First-core built-in worker cancellation drill",
      "type": "proof-pack",
      "status": "reviewed",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Active-seat dispatch control and observed serial core checkpoints added; review execution references."
        }
      ],
      "summary": "Observed built-in agent interruption and explicit owned-child cleanup; automatic subprocess cancellation is not established.",
      "tags": [
        "coordination",
        "application",
        "cancellation"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "310c6282b9979fff02ad55c4705d27522746cab11c75abcc1afeec093ac0ff55"
    },
    {
      "id": "coordination-application-core-launch",
      "path": "docs/coordination/application-core-launch.md",
      "title": "First-core G3 launch and prewrite receipt",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Records the exact Ruling 13 serial worker lease, compiled brief identity and read-only prewrite gate; implementation evidence remains pending.",
      "tags": [
        "coordination",
        "application",
        "implementation"
      ],
      "links": [
        {
          "to": "coordination-contract-b-core",
          "rel": "documents"
        },
        {
          "to": "coordination-application-cancel-drill",
          "rel": "depends-on"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fb6f988d2a8c3486f864c93667bc23cb59e9014da9afed9fd27505de64542500"
    },
    {
      "id": "coordination-architecture-qualification",
      "path": "docs/coordination/qualification-architecture.md",
      "title": "Architecture author harness qualification, 2026-09-23",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-coordinator-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "plan-application-build",
          "on": "2026-09-23",
          "reason": "Execution readback records B0/G3 gates and serial G4 checkpoints; review coordination timing and claims."
        },
        {
          "by": "coordination-application-build",
          "on": "2026-09-23",
          "reason": "Active-seat dispatch control and observed serial core checkpoints added; review execution references."
        }
      ],
      "summary": "Native Agy and Grok probes did not qualify an architecture author; an existing Codex author was assigned a fresh isolated worktree under the same contract.",
      "tags": [
        "coordination",
        "harness",
        "qualification",
        "architecture"
      ],
      "links": [
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        },
        {
          "to": "plan-application-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "45f0254b7dff5e2d3230608516b6073a347d3a2d1fba3aa2654011fdca7aebba"
    },
    {
      "id": "proof-app-shell-test-inventory",
      "path": "docs/proof/app-shell-test-inventory.md",
      "title": "App-shell test inventory — WorkbenchTests.cs assertions bound to controls the shell removes or changes",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-d3a",
      "phase": "implementation — D3b completion",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Every throw-new assertion in WorkbenchTests.cs (212, measured) is classified. All 60 ported rows have destination tests and recorded red evidence; 33 rows stay unchanged under Ruling 55. None are deleted.",
      "tags": [
        "app-shell",
        "desktop",
        "d3a",
        "test-inventory",
        "harness-migration",
        "named-tests",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "26730784e14cf0e60bc1beae9afb3f6547cc690b1529ddbfe8d4190ced964069"
    },
    {
      "id": "proof-application-adapters",
      "path": "docs/proof/application-adapters.md",
      "title": "Native application adapter implementation proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-native-adapters-20260923",
      "phase": "implementation",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [],
      "summary": "Source-bound build, CLI, controller and package evidence for the first native adapter; rendered macOS and Windows runtime acceptance remain open.",
      "tags": [
        "application",
        "native-ui",
        "cli",
        "accessibility",
        "proof"
      ],
      "links": [
        {
          "to": "coordination-contract-c-native",
          "rel": "depends-on"
        },
        {
          "to": "design-application-foundation",
          "rel": "depends-on"
        },
        {
          "to": "workbench-direction",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "24d745572af88c87dc54dd386d14f0daec16c87b4c53f0a58515391de5f86efe"
    },
    {
      "id": "proof-application-contracts",
      "path": "docs/proof/application-contracts.md",
      "title": "Executable native M1 contract evidence",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-contracts-author-20260923",
      "phase": "design",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams."
        },
        {
          "by": "adr-application-project-contract",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        }
      ],
      "summary": "Records actual C# build/session assertions and independent decimal, RFC 8785, BLAKE3 and history oracles. Distinguishes fixture geometry authority and persistence models from live local filesystem primitives. Native Windows, full language/kernel/store and production telemetry remain unassessed.",
      "tags": [
        "application",
        "contracts",
        "proof",
        "identity",
        "persistence"
      ],
      "links": [
        {
          "to": "design-application-contracts",
          "rel": "documents"
        },
        {
          "to": "adr-application-project-contract",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d7f4daf06dc318eb17178a7a872b4a5ff902448e7cfb514ae73f66b7c6147a7b"
    },
    {
      "id": "proof-application-core",
      "path": "docs/proof/application-core.md",
      "title": "Production core implementation checkpoints",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-application-core-20260923",
      "phase": "implementation",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [],
      "summary": "Records isolated, incomplete core increments with parser, identity and continuous subset evidence. Includes placed enclosures, session/history, authored projection and macOS store candidates; scientific and platform review remain open. This checkpoint is not a join candidate or M1 acceptance claim.",
      "tags": [
        "application",
        "core",
        "parser",
        "identity",
        "proof"
      ],
      "links": [
        {
          "to": "design-application-contracts",
          "rel": "documents"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f68917bfc2937c3f069bd7028813652cd4f720fdc4889385662f8227fcc84a63"
    },
    {
      "id": "proof-application-spikes",
      "path": "docs/proof/application-spikes.md",
      "title": "Application architecture contract and native spike evidence",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-owner-20260923",
      "phase": "architecture",
      "reviewBy": "2026-12-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "adr-application-stack",
          "on": "2026-09-23",
          "reason": "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Records actual pinned SDK/package builds, native macOS accessibility and picker observations, exact identity vectors, conservative rational geometry bounds and filesystem fault injection. Separates the bounded architecture spike from unfinished production and Windows evidence.",
      "tags": [
        "proof",
        "native",
        "identity",
        "geometry",
        "persistence"
      ],
      "links": [
        {
          "to": "architecture-application",
          "rel": "documents"
        },
        {
          "to": "adr-application-stack",
          "rel": "documents"
        },
        {
          "to": "design-application-foundation",
          "rel": "documents"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "113fbe36ed36611cb9ef6b51eefd0f4f5a3799a81c73c3ba3d256af6a1fd03d1"
    },
    {
      "id": "proof-authoring-decisions",
      "path": "docs/proof/authoring-decisions.md",
      "title": "V7 authoring decisions proof and review boundary",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Observed source, profile-bank, dimension, comparison, history and rendered browser proof for v7, with explicit prototype and production limits.",
      "tags": [
        "proof",
        "foildsl",
        "ux",
        "authoring"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "documents"
        },
        {
          "to": "plan-authoring-decisions",
          "rel": "relates-to"
        },
        {
          "to": "review-authoring-v7-independent",
          "rel": "depends-on"
        },
        {
          "to": "review-authoring-v7-gaps",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ceeab7a707197be1050f41774ff0b8a6dce901230a617e5d941e5a82a69a01e0"
    },
    {
      "id": "proof-c1-red-runs",
      "path": "docs/proof/c1-red-runs.md",
      "title": "C1 wing estimates and span red-first run",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-c1",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red run of the fourteen C1 checks at 32e59ce, before EditReference accepted a dimension receipt. Receipt_Dimension_OldReaderRefusesDocReference failed with DOC-REFERENCE. The same checks passed at 7d8b588.",
      "tags": [
        "app-shell",
        "wing-estimates",
        "span",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5d4ca477769a2466f3ae9863ae2a1c1a632002b7a8f6d0ed801c1f26ca8aeda1"
    },
    {
      "id": "proof-copyfix-red-runs",
      "path": "docs/proof/copyfix-red-runs.md",
      "title": "COPYFIX red-first runs — M1.2a copy decisions, two missing states, atomic Remove from Recent",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-copyfix",
      "phase": "implementation — M1.2a review fixes",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red runs for track COPYFIX. Four new Copy_* checks failed against the U1FIX build and one new preference-store check failed against a Remove op with no Apply support. Two checks pinned strings U1FIX had already built, so their red evidence is a string mutant, recorded as such.",
      "tags": [
        "app-shell",
        "desktop",
        "copy",
        "recent-files",
        "named-tests",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "711ca0f0fa03ea865db7996d59c384f1f354a215477182310bee76d044d9f0cf"
    },
    {
      "id": "proof-d1-red-runs",
      "path": "docs/proof/d1-red-runs.md",
      "title": "D1 shell model red-first runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-d1",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red-first run for track D1 (Shell model) of the app-shell build — proves that the spawned --shell-model suite fails and turns tools/run-tests.sh red with exit code 1.",
      "tags": [
        "app-shell",
        "desktop",
        "shell-model",
        "named-tests",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9df5fa6f94e4c4610dfbbd4e436035b32e6e08971fd1bedc98961d44cead1b3c"
    },
    {
      "id": "proof-d2-red-runs",
      "path": "docs/proof/d2-red-runs.md",
      "title": "D2 controller red-first runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-d2",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red-first run for track D2 (Controller) of the app-shell build — proves that the spawned --controller-shell suite fails and turns tools/run-tests.sh red with exit code 1, including Open_CancelDuringPrepare_CurrentFoilUnchanged red against the old controller.",
      "tags": [
        "app-shell",
        "desktop",
        "controller",
        "named-tests",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5b2ecaa87814cd9d5ee6faf37838233a02fc3c1a666a10e068b10ad044fcafa4"
    },
    {
      "id": "proof-d3a-red-runs",
      "path": "docs/proof/d3a-red-runs.md",
      "title": "D3a shell window red runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-d3a",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Recorded foreground red runs across D3a, D3b and THEME. All 60 ported inventory rows have destination tests and red evidence.",
      "tags": [
        "app-shell",
        "desktop",
        "d3a",
        "red-first"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6fc8d736919d6e164dd704d9d7ffadc013b0872f5fceeec02b7bb27922c03e0a"
    },
    {
      "id": "proof-dock-split-s8",
      "path": "docs/proof/cad-first-spikes/dock-split/README.md",
      "title": "S8 spike: can Dock capability overrides block split drops",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-s8",
      "phase": "design — S8 before G0 (app-shell build)",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Drives Dock 11.3.12.1 DockManager.IsDockTargetVisible and ValidateDockable with no UI, then executes each drop and prints the tree. CanDrop and every capability override or policy block all drops (tab, split and float alike), so they cannot block splits alone. The documented AllowedDockOperations mask (Fill|Window) on the dragged pane blocks every edge split and keeps tab and float drops. The GUI drag gesture was not driven.",
      "tags": [
        "dock",
        "docking",
        "split",
        "capability",
        "candrop",
        "spike",
        "s8"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2c327ae792fd61658e4e6c3d11829f42cbe68ff861652200638b19b3c3722084"
    },
    {
      "id": "proof-foildsl-authoring",
      "path": "docs/proof/foildsl-authoring.md",
      "title": "FoilDSL authoring specification and mockup proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "examples-foildsl",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Executed browser and documentation evidence for the bounded review artifact, independent findings and explicit production obligations; no scientific or full-language certification.",
      "tags": [
        "foildsl",
        "proof",
        "specification",
        "mockup"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "documents"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v6",
          "rel": "documents"
        },
        {
          "to": "review-foildsl-independent",
          "rel": "relates-to"
        },
        {
          "to": "examples-foildsl",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e69dd7c13a60ed21e352a98b34f038ce4ffbc3b49950b6d7fd97a0ee0c244583"
    },
    {
      "id": "proof-g0-red-runs",
      "path": "docs/proof/g0-red-runs.md",
      "title": "G0 glue red-first runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-g0",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red-first and green runs for G0 of the app-shell build — the named-test checker's self-test, the Desktop suite spawns, and the LayoutDocument wire-shape probe. The planted-suite red run was not performed (permission denied).",
      "tags": [
        "app-shell",
        "harness",
        "named-tests",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0d3e7c8e078e3951c47e3530df6786727343ae35ebab3161388ae1befaa81536"
    },
    {
      "id": "proof-legacy-gate-retarget",
      "path": "docs/proof/legacy-gate-retarget.md",
      "title": "Legacy gate retarget — the adapters gate's applied-contrast step moves from the pre-shell window to the shell matrix",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-legacy",
      "phase": "implementation — LEGACY repair cycle 1",
      "reviewBy": "2026-10-31",
      "reviewSuggested": [],
      "summary": "The pre-shell window retired, so the adapters gate now reads the shell matrix ThemeMatrix_ShellControls_AppliedContrast: 286 frozen rows over 4 theme variants, with every ratio re-derived. Repair cycle 1 restores the pressed and returned states, the TextBox states and the point Span field that the Test Architect's veto named. A mutant made the new rows fail before they passed.",
      "tags": [
        "legacy",
        "app-shell",
        "desktop",
        "theme",
        "contrast",
        "gate",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "proof-app-shell-test-inventory",
          "rel": "relates-to"
        },
        {
          "to": "proof-application-adapters",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1c5fb18d7b7b0f9f2acb5b22e6a7a413d1d133b98929c270c8fb333b53dd7bae"
    },
    {
      "id": "proof-m12b-b0-red-runs",
      "path": "docs/proof/m12b-b0-red-runs.md",
      "title": "M1.2b B0 red run of the named checks",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "implementation",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "The 29 B0 checks at commit 9ab4603, run by tools/run-tests.sh before the FoilDSL 4.1 grammar change. Exit 1. Twenty-six checks fail; three already match the unchanged reader.",
      "tags": [
        "m12b",
        "foildsl",
        "red-first",
        "b0"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "proof-m12b-old-build",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4f2194835126e5d05954f026d84c3f383be080ba71dc5f9558d0f5da0e9d88ef"
    },
    {
      "id": "proof-m12b-b1a-red-runs",
      "path": "docs/proof/m12b-b1a-red-runs.md",
      "title": "M1.2b B1a chord red-first run",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-b1a",
      "phase": "implementation",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Red run of the 24 B1a checks at 6705872, before the chord fit and the dimension fingerprint. Reopen_RetrySameDimensionOperationId_ReturnsPriorId failed with DOC-OPERATION-CONFLICT against the as-built memo. A hand mutant of the limit to 20 µm turned the 10.1 µm fixture red and left the 9.9 µm fixture green; the committed limit is 10 µm.",
      "tags": [
        "m12b",
        "chords",
        "length-expression",
        "proof"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-driving-dimensions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a3c8def8df25300d137222db33bf3892be4a7ecca2ac4aadbf2ab829409a9292"
    },
    {
      "id": "proof-m12b-b1b-red-runs",
      "path": "docs/proof/m12b-b1b-red-runs.md",
      "title": "M1.2b B1b red-first and mutation runs",
      "type": "proof-pack",
      "status": "in-progress",
      "owner": "track-b1b",
      "phase": "implementation — M1.2b",
      "reviewBy": "2027-03-29",
      "reviewSuggested": [],
      "summary": "Red-first run of the B1b named tests and recorded hand-mutant observations. The complete run output is preserved below before the implementation commit.",
      "tags": [
        "m12b",
        "b1b",
        "red-first",
        "point-editing"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "coordination-m12b-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8ac6bc7cbab13cb0aaa11074e791c0e38db67c6c1784d9c293c13c7980b0dc86"
    },
    {
      "id": "proof-m12b-native",
      "path": "docs/proof/m12b-native/index.md",
      "title": "M1.2b native attach evidence",
      "type": "proof-pack",
      "status": "blocked",
      "owner": "@track-u3a",
      "phase": "implementation",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Twelve bounded CUA attach attempts against two foreground macOS review launches returned cgWindowNotFound. Every launch-bound attach check exited 1 with NATIVE_REVIEW_BLOCKED; no screenshot, AX dump, interaction, or content assertion was available.",
      "tags": [
        "m12b",
        "native",
        "review",
        "attach",
        "accessibility"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "973ef78347a6a6fa76a839f375ea3cda8d2e3e374f01dc5645eb1f9a087f33e1"
    },
    {
      "id": "proof-m12b-old-build",
      "path": "docs/proof/m12b-old-build/README.md",
      "title": "M1.2b old-build characterization of FoilDSL 4.1",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "implementation",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "Codes the committed 4.1 fixtures and a drag, nudge and span project draw from the parser at 10f0628, recorded before B0 changes that parser.",
      "tags": [
        "m12b",
        "foildsl",
        "characterization",
        "b0"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "03d41ba3af24e65ff2dcfb228579b07ec31d34bae66aa9bb9f62d0724a0ee51a"
    },
    {
      "id": "proof-m12b-u1a",
      "path": "docs/proof/m12b/U1a.md",
      "title": "M1.2b U1a controller proof pack",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-u1a2",
      "phase": "implementation",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Records U1a controller red-first and current green evidence, the 3 px activation mutant, readiness measurements, and the remaining Core Span seam before this track can exit.",
      "tags": [
        "m12b",
        "u1a",
        "desktop",
        "controller",
        "proof"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "proof-m12b-u1a-red-runs",
          "rel": "depends-on"
        },
        {
          "to": "coordination-m12b-build",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3ffd1cf704b4b58c57ed2bb26d1c34f3b3304ec32ed84dd50ea61d3d364db736"
    },
    {
      "id": "proof-m12b-u1a-red-runs",
      "path": "docs/proof/m12b-u1a-red-runs.md",
      "title": "M1.2b U1a red runs",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-u1a2",
      "phase": "implementation",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Records the foreground red run of all 23 named U1a controller checks before the controller implementation, and the separate threshold mutant run required by the U1a exit evidence.",
      "tags": [
        "m12b",
        "u1a",
        "desktop",
        "controller",
        "red-first"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "coordination-m12b-build",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fa086ae8209f957432b575d24320010de8599f398c61f31f27bbc0357d65d7ed"
    },
    {
      "id": "proof-m12b-u1b-red-runs",
      "path": "docs/proof/m12b-u1b-red-runs.md",
      "title": "U1b Plan canvas red and mutant runs",
      "type": "proof-pack",
      "status": "complete",
      "owner": "track-u1b",
      "phase": "m1.2b",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Records the observed red runs and mutation controls for the U1b Plan canvas, including whole-window rendered pixels and point automation peers.",
      "tags": [
        "plan-canvas",
        "tdd",
        "rendered-state"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "472dcb401c2327c95b35c249c5af76d2fe2dd1b6cc919bd2201a2d68229cf957"
    },
    {
      "id": "proof-m12b-u2-red-runs",
      "path": "docs/proof/m12b-u2-red-runs.md",
      "title": "M1.2b U2 red runs",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-u2",
      "phase": "implementation",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Records the foreground red run of all 25 named U2 pane checks before the Properties, Browser, command, and retirement implementation, and the dead-control mutant that turned the sweep red.",
      "tags": [
        "m12b",
        "u2",
        "desktop",
        "panes",
        "red-first"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "coordination-m12b-build",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "aa10c715e67af0d14891756276142c698d1c698b31df88fa0e2022c8e605084c"
    },
    {
      "id": "proof-m12b2-golden",
      "path": "docs/proof/m12b2-golden/receipt.md",
      "title": "PL0 golden master — planted mutant receipt",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-pl0",
      "phase": "implementation",
      "reviewBy": "2026-10-28",
      "reviewSuggested": [],
      "summary": "The certificate golden master was captured at 3b396e5, before the placement refactor. Reassociating the placed-X product turned the golden test red. Reordering the blend sum did not, because those interval additions commute. The golden master pins outputs and refusals, not the operation tree.",
      "tags": [
        "m1.2b2",
        "placement",
        "golden-master",
        "pl0"
      ],
      "links": [
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "afa2eb389b44da436b0398fd36ab0dacea18ff046a5b266d6b83f3f5173ad3a0"
    },
    {
      "id": "proof-native-ui-workbench",
      "path": "docs/proof/native-ui-workbench.md",
      "title": "CFD-Workbench native UI proof obligations",
      "type": "proof-pack",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        }
      ],
      "summary": "Native-platform evidence remains explicitly unverified because this deliverable is an HTML design prototype. The required Mac and Windows checks are named without choosing an application framework.",
      "tags": [
        "native",
        "accessibility",
        "cross-platform"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6064910d7c0a08c235953046f974b5f29b130e7fe0551ecfcfc4199f527fc34b"
    },
    {
      "id": "proof-newfoil-red-runs",
      "path": "docs/proof/newfoil-red-runs.md",
      "title": "NEWFOIL red-first run",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-newfoil",
      "phase": "implementation",
      "reviewBy": "2026-10-28",
      "reviewSuggested": [],
      "summary": "Red run of the New foil checks before FoilSource.NewDefault and WorkbenchController.NewFoilAsync existed. tools/run-tests.sh exited 1 at the Release build with nine missing-member errors and zero warnings.",
      "tags": [
        "app-shell",
        "new-foil",
        "foildsl",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0a8fae111d27b62989f3791fec1e6cadd331aaa144aebf350b126c9b8870070a"
    },
    {
      "id": "proof-openfix-red-runs",
      "path": "docs/proof/openfix-red-runs.md",
      "title": "OPENFIX red runs and verification — M1.2a Open outcomes and Span input",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-openfix",
      "phase": "implementation — M1.2a Open-flow repair",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red-first evidence for the ShellHost candidate-accept path, Start origin alerts, retained refused original, and nonfinite Span input; plus the three-run green set.",
      "tags": [
        "app-shell",
        "desktop",
        "open",
        "span",
        "red-first",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "411ed7be048a52582a06afa9f21c0cc170ba90254050bd671e7d8b689c5a5fe1"
    },
    {
      "id": "proof-p1-red-runs",
      "path": "docs/proof/p1-red-runs.md",
      "title": "P1 preferences red-first runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-p1",
      "phase": "implementation",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Red run of the P1 checks at 92079ab, while the codec and store still threw. With the version-first return removed, the three v2 rollback fixtures were overwritten. The same checks passed at 4efe980. Reader depth stays 8; the writer uses 9.",
      "tags": [
        "app-shell",
        "preferences",
        "layout",
        "proof"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "adee8b3bb2ae1cab0c26acad09efd64af6075d81ba0032a44dd092f05e94faaf"
    },
    {
      "id": "proof-readyfix2",
      "path": "docs/proof/readyfix2.md",
      "title": "READYFIX2 — Core fixture cwd-relative paths and the Plan-canvas theme key set",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-readyfix2",
      "phase": "readiness",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Fixes the two causes LEGACY left readiness RED on: Core.Tests fixture reads resolved against the process cwd rather than the repo, and the adapters theme gate's declared-brush-key set did not account for U1b's seven Plan-canvas aliases. Extends the TEST-REPO-LAYOUT scan to catch the cwd-relative-literal shape, and proves the theme gate's declared-key check with a mutant.",
      "tags": [
        "readiness",
        "test-repo-layout",
        "theme",
        "m12b",
        "proof"
      ],
      "links": [
        {
          "to": "defect-classes",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "535bf69270259d3c7d8e7b9b0e10deae5b78da9b7dd98df1ebad355d92d99bb5"
    },
    {
      "id": "proof-shellfix-red-runs",
      "path": "docs/proof/shellfix-red-runs.md",
      "title": "Shell visual defect red runs",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "m1.2a-shellfix",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Red-first rendered-control checks for the operator's first native shell findings. Each row records the observed failure before its production fix.",
      "tags": [
        "app-shell",
        "rendered-ui",
        "regression"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "tested-by"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b4ae8bc942c4c5e7bcab75f8109c116b394928fc18a0b5f62b5ea63860cc0b74"
    },
    {
      "id": "proof-u1fix-red-runs",
      "path": "docs/proof/u1fix-red-runs.md",
      "title": "U1FIX app-shell repair proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "M1.2a U1FIX",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Red and green Desktop harness evidence for the U1FIX open-failure actions, review shell, accessible controls and copy. Native AX and VoiceOver review remain operator-run.",
      "tags": [
        "app-shell",
        "accessibility",
        "copy",
        "tdd"
      ],
      "links": [
        {
          "to": "review-app-shell-native",
          "rel": "relates-to"
        },
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ef3473e02f906e97f98dceed90f7019c88f42445232d4aeec1e785c6c85c30f9"
    },
    {
      "id": "proof-visible-presentation",
      "path": "docs/proof/visible-presentation.md",
      "title": "Visible presentation feasibility proof packet",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-visible-endpoint-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-12-24",
      "reviewSuggested": [
        {
          "by": "design-visible-presentation",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        }
      ],
      "summary": "Source-bound builds, 50 synthetic receipt controls, four killed wrong-result mutants and 32 noncapture clock pairs establish limited preparation evidence. Native capture, visibility, lifecycle, rendered-state correlation and every M1 latency budget remain Not assessed.",
      "tags": [
        "performance",
        "presentation",
        "evidence"
      ],
      "links": [
        {
          "to": "design-visible-presentation",
          "rel": "documents"
        },
        {
          "to": "kb-visible-presentation",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8ba9b363a2dc1f706d2dc6e3ac217494b68d67563f6ec363acedde1c2a4a98be"
    },
    {
      "id": "proof-windows-runtime",
      "path": "docs/proof/windows-runtime.md",
      "title": "Windows W0 preparation and failed W1 hosted qualification",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-windows-w0-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "coordination-windows-runtime-route",
          "on": "2026-09-24",
          "reason": "R43-R44 hosted route executed with failed native cases and a DACL receipt refusal; review route dependencies."
        }
      ],
      "summary": "Records local W0 preparation and an actual but failed W1 Windows x64 native qualification. Independent review rejected production acceptance; UIA, directory durability and product runtime remain open.",
      "tags": [
        "windows",
        "proof",
        "qualification",
        "native"
      ],
      "links": [
        {
          "to": "design-windows-runtime",
          "rel": "documents"
        },
        {
          "to": "coordination-windows-runtime-route",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fbd2221b43f9cc84427285d30106e6c0863c2b680354bd63e82266ca009f6797"
    },
    {
      "id": "review-app-shell-native",
      "path": "docs/reviews/app-shell-native.md",
      "title": "Native review — CAD-first app shell, M1.2a rows",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design — U1a (M1.2a)",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "U1a for M1.2a. COPY-125 to COPY-139 recorded; the build has fourteen copy and behaviour findings (C-1 to C-14; C-10, dead open-failed buttons, is a Blocker). No native row was attached: the supported CUA adapter is absent from this harness, and review mode turns the Dock shell off, so the supported attach cannot bind the M1.2a shell. Every native row is operator-run, not done. The accessibility veto is held. M1.2a stays open.",
      "tags": [
        "native-ui",
        "accessibility",
        "app-shell",
        "m1.2a",
        "copy",
        "operator-run"
      ],
      "links": [
        {
          "to": "design-app-shell",
          "rel": "documents"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-application-native",
          "rel": "refines"
        },
        {
          "to": "investigation-review-window-attach",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "52f02005ed6f91de53d6b51ed3f026fd9aea17cc4b3bc0c4df1b44417f3b84c9"
    },
    {
      "id": "review-application-architecture",
      "path": "docs/reviews/application-architecture.md",
      "title": "Independent review of the application foundation architecture",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-application-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Independent lead review of the first application architecture and contract spikes. Records observed native interaction evidence, contract findings and outstanding gates; it does not certify an application implementation or Windows runtime behavior.",
      "tags": [
        "architecture",
        "application",
        "independent-review",
        "native-ui",
        "provenance"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "domain-experts",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "87f51d884298aea0a26e445490eb70ae9b6e6a2486fff34b9be0c51f949da3c1"
    },
    {
      "id": "review-application-contracts",
      "path": "docs/reviews/application-contracts.md",
      "title": "Independent review of the application session contracts",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-application-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "Ruling 15 clarifies diagnostic phase when numeric range depends on a trusted unit and role binding; review citations without changing accepted syntax."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Independent review of the serial B0 contract fixture, its durable identity and session boundaries, with explicit limits on what fixture evidence establishes.",
      "tags": [
        "application",
        "contracts",
        "independent-review",
        "persistence",
        "identity"
      ],
      "links": [
        {
          "to": "coordination-contract-b0",
          "rel": "documents"
        },
        {
          "to": "architecture-application",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b2331eecaeb6b95bbd35145b9eab231f88ae4abd860ca6be6bc9463437952371"
    },
    {
      "id": "review-application-core",
      "path": "docs/reviews/application-core.md",
      "title": "Independent review of the production application core",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-application-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        }
      ],
      "summary": "Records independent observations against named frozen production-core checkpoints, including numeric and continuous-geometry oracles and a reproduced cancelled-assessment defect. Review remains open; these partial results do not authorize integration or application acceptance.",
      "tags": [
        "application",
        "core",
        "independent-review",
        "geometry",
        "persistence",
        "identity"
      ],
      "links": [
        {
          "to": "coordination-contract-b-core",
          "rel": "documents"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        },
        {
          "to": "architecture-application",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7445730f0ec31bf91dca72db4ebe50a7196d1e8870eb92ee5935c1206a20e5df"
    },
    {
      "id": "review-foil-editing-flow-results",
      "path": "docs/reviews/foil-editing-flow-results.md",
      "title": "Editable foils and flow results — review and proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        }
      ],
      "summary": "Review of the specification and interactive mockup iteration for weighted foil editing, dimensional loads, water-aware sweeps and linked field replay. This evidence concerns the HTML design artifact; numerical and native product validation remain separate gates.",
      "tags": [
        "review",
        "geometry",
        "analysis",
        "simulation",
        "visualization"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench",
          "rel": "documents"
        },
        {
          "to": "plan-foil-editing-flow-results",
          "rel": "documents"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1ebbb7f2e0ba6243c97ee41db8e4416787407715c381bef63c99106087b77f31"
    },
    {
      "id": "review-m12b-native",
      "path": "docs/reviews/m12b-native.md",
      "title": "Native review — M1.2b CAD point editing on the Plan view",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "implementation — U3 (M1.2b)",
      "reviewBy": "2026-10-30",
      "reviewSuggested": [],
      "summary": "Operator-run native session on build c43711a, 1 October 2026. The operator walked the twelve §0.1 demo steps; ten pass and two pass with a defect. Six defects (D-1 to D-6), five design findings (F-1 to F-5), seven observations and one question (ruled). D-4 (MAC and Mean chord go blank after the first edit) and F-1 (Properties is not a property grid) are the two that matter most. M1.2b stays open until D-1 to D-4 are fixed under test.",
      "tags": [
        "native-ui",
        "m1.2b",
        "plan-view",
        "points",
        "operator-run",
        "review"
      ],
      "links": [
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "proof-m12b-native",
          "rel": "refines"
        },
        {
          "to": "review-app-shell-native",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "849b6fdc0b019dc03c971adfc96f1f1fde6889b4c43a783dd21e46103fe6bb1d"
    },
    {
      "id": "review-spec-v02-critique",
      "path": "docs/reviews/spec-v02-critique.md",
      "title": "Critique of specification revision 0.2 against the knowledge base",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Fourteen lenses (the seven new domain experts and seven pack lenses) attacked specification revision 0.2 in Adversary Mode against the hydrofoil knowledge base. Every veto-holding lens returned BLOCK: 22 Blockers and 110 Majors, resolved into a consolidated list of what v1 must add and what it must tighten. Revision 1.0 is written against this list; each finding names the v1 section that resolves it.",
      "tags": [
        "review",
        "specification",
        "critique",
        "knowledge",
        "personas"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "domain-experts",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bf8fd669bc3c11218fbd3d6b6979c7a33bc7af8565b70edeaa38e7b1d2807a34"
    },
    {
      "id": "review-specification-gate",
      "path": "docs/reviews/specification-gate.md",
      "title": "CFD-Workbench independent specification gate",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
        }
      ],
      "summary": "Independent review of the plan and three-layer specification found and resolved modeling, flow and coverage defects before mockup authoring. The pass concerns the product contract, not implemented scientific or native behavior.",
      "tags": [
        "review",
        "specification",
        "geometry",
        "ux"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "plan-specification-and-ui",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4f7269fa4a2586f8fbcc18e814e208fca70da09995d1ad12f37e079f8d74f1dc"
    },
    {
      "id": "review-test-ci-waste",
      "path": "docs/reviews/test-ci-waste.md",
      "title": "Test and CI waste review — measured baseline, cuts and controls",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-testci",
      "phase": "",
      "reviewBy": "2026-10-27",
      "reviewSuggested": [],
      "summary": "Measured cost of the test harnesses, repo checks, verify gates and join on 2026-09-27; the waste found, what was cut and why, the ring split, the plan-ceremony patch list and the controls that stop the waste returning.",
      "tags": [
        "testing",
        "ci",
        "cost",
        "rings",
        "controls"
      ],
      "links": [
        {
          "to": "coordination-app-shell-build",
          "rel": "relates-to"
        },
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b756a61cfbd25e28654cb0a9798d26f047599530c7d5459d960e8b60a02eaf78"
    },
    {
      "id": "review-ui-application-native",
      "path": "docs/reviews/ui-application-native.md",
      "title": "Independent native application and adapter review",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-application-20260923",
      "phase": "",
      "reviewBy": "2026-10-23",
      "reviewSuggested": [
        {
          "by": "investigation-review-window-attach",
          "on": "2026-09-24",
          "reason": "Two fresh native launches passed bounded attachment readiness; internal CUA cause remains inferred."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-24",
          "reason": "User-approved M1 two-platform visible-timing gates and M1.1 section authoring changed the delivery architecture; review dependent milestone claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Independent source-bound CLI, controller, recovery and package observations. Named native interactions and bounded B permission repair pass independent review. Remaining native theme, performance and accessibility obligations keep C and M1 open.",
      "tags": [
        "application",
        "native-ui",
        "independent-review",
        "accessibility",
        "persistence"
      ],
      "links": [
        {
          "to": "coordination-contract-c-native",
          "rel": "documents"
        },
        {
          "to": "design-application-contracts",
          "rel": "depends-on"
        },
        {
          "to": "architecture-application",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        },
        {
          "to": "investigation-review-window-attach",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b29caeb52160eb188658e2511b501e61d34fe2711bc46e3b5d2ef053b29fc153"
    },
    {
      "id": "review-ui-workbench",
      "path": "docs/reviews/ui-workbench.md",
      "title": "CFD-Workbench interface review and proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Independent review clears the HTML workbench for design iteration after correcting geometry authority, historical results, keyboard focus, narrow navigation and partial-field rendering. Four minor craft findings and unverified native, scientific and full accessibility obligations remain explicit.",
      "tags": [
        "ui",
        "accessibility",
        "geometry",
        "review"
      ],
      "links": [
        {
          "to": "mockup-workbench",
          "rel": "documents"
        },
        {
          "to": "design-language",
          "rel": "documents"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "documents"
        },
        {
          "to": "proof-native-ui-workbench",
          "rel": "depends-on"
        },
        {
          "to": "review-specification-gate",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3e0b42345aa3ad84018e49ac2cbaf3548b8e2acd40a513ad0a68ede2a54bdf24"
    },
    {
      "id": "review-ui-workbench-v10",
      "path": "docs/reviews/ui-workbench-v10.md",
      "title": "UI review — v10 first run, focus-safe floats, point types, catalog and Wing block (elevate)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevated v9 after measuring it: restored the first-run and loading states, one precision per quantity, Properties readable at 200 px, and floats that move clear of a focused target (option a, operator-confirmed). Folded in the operator's four requests plus the chord, MAC and typed-dimension decisions. One repair cycle; the accessibility veto cleared by the lens, the Simplifier's veto cleared, 15 of 15 oracle gates green.",
      "tags": [
        "ui-review",
        "accessibility",
        "properties",
        "catalog",
        "point-types",
        "estimates",
        "first-run",
        "docking"
      ],
      "links": [
        {
          "to": "mockup-workbench-v10",
          "rel": "documents"
        },
        {
          "to": "review-ui-workbench-v9",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8048af7c6e1e85a5a24ea09d584024c888a52fe0e13f96f51f303dd9f766dd2a"
    },
    {
      "id": "review-ui-workbench-v8",
      "path": "docs/reviews/ui-workbench-v8.md",
      "title": "UI review — CAD-first direction v8 (elevate)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-26",
          "reason": "Spec 1.6 makes the CAD area CAD-first (v10): point types, typed Span/Root/Tip chord, Wing estimates (S/b, MAC), section editor mode, catalog Replace and My sections; supersedes CAD-05/07, UX-13/24, UI-26 and parts of DOC-01, CAD-04/08, UX-23, UI-25; DR-2, DR-4-8 open. Review dependent claims."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Measured the shipped native window (43 controls, 38 labels, three always-on panels, implementation vocabulary), set a CAD-first direction at the Fusion 360 / Shape3D bar, and built mockup v8 through two repair cycles against accessibility, simplifier and marine-CAD adversaries. Mechanical checks pass; three decisions remain the operator's.",
      "tags": [
        "ui-review",
        "cad",
        "direction",
        "accessibility",
        "section-editor",
        "points",
        "handles"
      ],
      "links": [
        {
          "to": "mockup-workbench-v8",
          "rel": "documents"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6520ea5d2f278b2f9676825f882ae063af040ddd954eb753bf72bdffc28eb14a"
    },
    {
      "id": "review-ui-workbench-v9",
      "path": "docs/reviews/ui-workbench-v9.md",
      "title": "UI review — v9 docked panes and Properties pane (elevate)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-26",
      "reviewSuggested": [
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        }
      ],
      "summary": "Elevated v8 with VS Code / Premiere Pro window management and a selection-driven Properties pane, placed per the operator in one narrow left panel with optional right and bottom docks. Two repair cycles against accessibility, native-desktop and simplifier lenses; one accessibility Major (floats covering focused canvas targets) is open at the cap.",
      "tags": [
        "ui-review",
        "docking",
        "properties",
        "window-management",
        "accessibility",
        "native-desktop"
      ],
      "links": [
        {
          "to": "mockup-workbench-v9",
          "rel": "documents"
        },
        {
          "to": "review-ui-workbench-v8",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0f044331488e666786b7d5d07bbff847013bbe525b68181eb288243ee436907c"
    },
    {
      "id": "review-visible-presentation",
      "path": "docs/reviews/visible-presentation.md",
      "title": "Independent visible-presentation preparation review",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-visible-identity-review-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "coordination-application-build",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        },
        {
          "by": "design-visible-presentation",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        },
        {
          "by": "proof-visible-presentation",
          "on": "2026-09-24",
          "reason": "R47/R50 joined source, independent review and bounded status changed this dependency; reassess current claims"
        }
      ],
      "summary": "R47 preparation passes independent package, pure request and identity replay. The concrete private package closure is established; native identity, geometry, lifecycle and on-screen timing remain unmeasured and no capture execution is authorized.",
      "tags": [
        "performance",
        "review",
        "privacy",
        "testing"
      ],
      "links": [
        {
          "to": "kb-visible-presentation",
          "rel": "depends-on"
        },
        {
          "to": "note-m1-scope-decision",
          "rel": "documents"
        },
        {
          "to": "coordination-application-build",
          "rel": "relates-to"
        },
        {
          "to": "design-visible-presentation",
          "rel": "documents"
        },
        {
          "to": "proof-visible-presentation",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "69454b8cb20ee355a4c14ed10e193a2a87d49984d707f9b71c16f0ad78880e75"
    },
    {
      "id": "review-windows-runtime",
      "path": "docs/reviews/windows-runtime.md",
      "title": "Independent Windows qualification review",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@cfd-windows-review-20260925",
      "phase": "application-foundation",
      "reviewBy": "2026-10-25",
      "reviewSuggested": [
        {
          "by": "coordination-windows-runtime-route",
          "on": "2026-09-24",
          "reason": "R43-R44 hosted route executed with failed native cases and a DACL receipt refusal; review route dependencies."
        },
        {
          "by": "defect-classes",
          "on": "2026-09-24",
          "reason": "W1 added workflow-context and leadership/identity recurrence controls; review related class assumptions."
        }
      ],
      "summary": "W0 preparation passed conditionally, but the first executed Windows qualification failed. Source-bound native receipts expose four failed cases and a DACL consumer refusal; product runtime, containment, durability, accessibility and timing remain unqualified.",
      "tags": [
        "windows",
        "review",
        "security",
        "persistence",
        "testing"
      ],
      "links": [
        {
          "to": "coordination-windows-runtime-route",
          "rel": "documents"
        },
        {
          "to": "note-m1-scope-decision",
          "rel": "depends-on"
        },
        {
          "to": "coordination-contract-b-core",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4ff7b199189535ba8d993358fe164d12549fd6cf35b68adab979f9f806794bf5"
    },
    {
      "id": "spec-cfd-workbench",
      "path": "docs/specs/cfd-workbench.md",
      "title": "CFD-Workbench — product specification",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "design-language",
          "on": "2026-09-19",
          "reason": "Initial cross-platform workbench token and interaction language created for review."
        },
        {
          "by": "mockup-workbench",
          "on": "2026-09-19",
          "reason": "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof."
        }
      ],
      "summary": "Functional, UX and UI specification for a local Mac and Windows hydrofoil workbench. Editable catalog-derived sections and weighted curve fairing feed traceable water-dependent loads, velocity-by-incidence sweeps and synchronized 2D/3D flow inspection with explicit scientific limits.",
      "tags": [
        "hydrofoil",
        "cad",
        "parametric",
        "cross-platform",
        "simulation"
      ],
      "links": [
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        },
        {
          "to": "plan-specification-and-ui",
          "rel": "relates-to"
        },
        {
          "to": "plan-foil-editing-flow-results",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "B2. Flow F1 — start, restore and preserve (DOC-01–04)",
          "mermaid": "flowchart TD\nA[Launch] --> B{Recent saved document?}\nB -->|Yes| C{Can reopen?}\nC -->|Yes| D[Design workspace]\nC -->|Missing or invalid| E[Explain failure: Locate / Open / Sample]\nB -->|No| F[New / Open / Sample; optional language start]\nF --> D\nE --> D\nD --> G[Edit preview]\nG -->|Accept| H[Dirty revision]\nG -->|Cancel| D\nH --> I{Save succeeds?}\nI -->|Yes| J[Saved revision]\nI -->|No| K[Keep dirty state; retry or Save as]\nK --> I\nH --> L[Interrupted session]\nL --> M[Compare recovery with saved version]\nM -->|Restore or discard recovery| D"
        },
        {
          "kind": "flowchart",
          "title": "B3. Flow F2 — shape through curves and stations (GEO-01–14, CAT-01–03)",
          "mermaid": "flowchart TD\nA[Recipe or open native model] --> B[Evaluate one explicit surface]\nB --> C{Edit target}\nC -->|Distribution| D[Select curve mode, control or handle]\nC -->|Station| E[Select plane / row / profile]\nD --> F[Drag, nudge or type value / influence weight]\nE --> F\nF --> G{Recipe still linked?}\nG -->|Yes| H[Preview direct-parametric detachment]\nG -->|No| I[Preview same model]\nH --> I\nI --> J{Geometry, locks and fit tolerance valid?}\nJ -->|Yes, accept| K[New revision; dependent results historical]\nJ -->|No| L[Highlight location and failed constraint]\nL -->|Correct| F\nL -->|Revert| B\nI -->|Cancel / Escape| B\nK -->|Undo| B\nK --> B\nC -->|Edit section, including NACA 0012| M[Source-linked upper/lower curve draft; show fit error]\nM --> F\nM -->|Cancel| B\nC -->|Smooth section or outline| W[Preview weighted control polygon, curve and comb]\nW --> X[Inspect deviation, evaluated dimensions and explicit locks]\nX --> F\nQ[Add profile from DAT] --> R{Parse and validate}\nR -->|Invalid| T[Show line or shape error; choose another file]\nT --> Q\nR -->|Valid| U[Source, normalization and profile preview]\nU -->|Accept| E\nU -->|Cancel| B\nV[Break symmetry preview / read-only ghost overlay] --> B"
        },
        {
          "kind": "flowchart",
          "title": "B4. Flow F3 — analyze and compare (ANA-01–18)",
          "mermaid": "flowchart TD\nA[Choose Section or Wing] --> B[Set speed, incidence, fresh/salt water and method]\nB --> C{Data and envelope supported?}\nC -->|No| D[Explain: choose supported point / install backend]\nD --> B\nC -->|Yes| E[Compute against pinned revision]\nE --> F{Outcome}\nF -->|Success| G[Coefficients and scope-correct loads, units and uncertainty]\nF -->|Failed| H[Keep historical result; inspect reason / retry]\nH --> B\nG --> I[Compare compatible snapshots or sweep]\nG --> U[Convert N to lbf; same physical result]\nI -->|Missing sample| J[Gap plus reason; retry sample]\nJ --> E\nG -->|Geometry edited| K[Historical banner; recompute current]\nK --> E\nI -->|Methods disagree| L[Side-by-side assumptions and discrepancy]\nG -->|Water or physical input edited| K"
        },
        {
          "kind": "flowchart",
          "title": "B5. Flow F4 — setup, simulation and results (CFD-01–06, VIZ-01–04)",
          "mermaid": "flowchart TD\nA[Open Simulate] --> B{Compatible backend ready?}\nB -->|No| C[Detect / choose supported setup]\nC --> D[Review download, disk, elevation and actions]\nD -->|Decline| E[Return to design]\nD -->|Approve| F[Install stages and smoke test]\nF -->|Interrupted / offline / denied| G[Explain stage; resume / repair / cancel]\nG --> C\nF -->|Pass| H[Pin version]\nB -->|Yes| I[Water, single point or speed-by-incidence sweep]\nH --> I\nI --> Q{Fluid and resolved sample schedule valid?}\nQ -->|No| R[Show invalid value; preserve draft]\nR --> I\nQ -->|Yes| S[Review Cartesian matrix, held conditions and resource estimate]\nS --> J{Mesh gate passes?}\nJ -->|No| K[Show metrics; repair mesh]\nK --> I\nJ -->|Yes| L[Queue samples: status, residuals, forces, elapsed]\nL -->|Cancel / crash| M[Retain case and partial evidence; retry eligible stage]\nM --> I\nL -->|Required outputs verified per sample| N[Results matrix; pinned snapshot per sample]\nN --> O[Choose 2D / 3D, field / slice / streamline / probe]\nO -->|Missing variable| P[Unavailable with reason; choose supported field]\nP --> O\nO --> T[Choose replay axis and held coordinate; Play or step]\nT --> U{Selected sample has evidence?}\nU -->|Yes| V[Update matrix, charts, metrics and field together]\nV -->|Next sample| T\nU -->|No| W[Pause; clear field/metrics; show reason]\nW -->|Retry| L\nW -->|Skip explicitly| T\nT -->|Pause / leave Results| N"
        },
        {
          "kind": "flowchart",
          "title": "B6. Flow F5 — optional language and export (AI-01–06, EXP-01–02)",
          "mermaid": "flowchart TD\nA[Contextual assistant] --> B{Key and consent present?}\nB -->|No| C[Explain optional setup; manual path remains]\nB -->|Yes| D[Inspect sharing summary; submit]\nD --> E{Response valid and supported?}\nE -->|No| F[Unsupported / error; edit request or dismiss]\nE -->|Starting design| G[Validated fields, shape preview and explicit diff]\nG -->|Edit| G\nG -->|Discard| H[Document unchanged]\nG -->|Final Accept; base revision unchanged| I[One undoable revision]\nG -->|Base revision changed| R[Refresh preview and review changes]\nR --> G\nE -->|Grounded explanation| J[Citations to selected run]\nE -->|What-if| K[Offer copy and actual recomputation]\nX[Current design: global Export, no AI required] --> L[Choose export format and tolerance]\nI --> X\nL --> M{Format and geometry checks pass?}\nM -->|No| N[Explain failure; return to geometry]\nM -->|Yes| O{Write export}\nO -->|Success| S[Export with revision and limitations]\nO -->|Denied / disk full| T[Preserve existing file; choose path or retry]\nT --> L"
        }
      ],
      "sourceSha256": "5dcd67855c1e3a004cd0ac67b12ed3c3a2f2609451b871bc8f0aa223226c46a9"
    },
    {
      "id": "spec-cfd-workbench-v1",
      "path": "docs/specs/cfd-workbench-v1.md",
      "title": "CFD-Workbench — product specification v1.6 (CAD-first editing, Wing estimates and section catalog)",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-03-26",
      "reviewSuggested": [
        {
          "by": "mockup-workbench-v3",
          "on": "2026-09-20",
          "reason": "Mockup v3 (thick-client shell) supersedes v2 as the review artifact; shell contract proven by tools/check-mockup-v3.mjs; UI-23 and the activity rail in spec 1.1a."
        },
        {
          "by": "mockup-workbench-v4",
          "on": "2026-09-20",
          "reason": "Mockup v4 (CAD editing views) supersedes v3; spec 1.2 CAD-04–06, UX-23, UI-24–25; oracle tools/check-mockup-v4.mjs."
        },
        {
          "by": "mockup-workbench-v5",
          "on": "2026-09-21",
          "reason": "Mockup v5 (control-vertex splines, four viewports, tool palette) supersedes v4; spec 1.3 GEO-03/05/13/15, CAD-01/04/07/08, A4.2, A4.12, UX-24, UI-25–27; oracle tools/check-mockup-v5.mjs."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts."
        },
        {
          "by": "adr-application-stack",
          "on": "2026-09-23",
          "reason": "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates."
        },
        {
          "by": "adr-application-project-contract",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open."
        },
        {
          "by": "mockup-workbench-v7",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "M1.2b2 (ADR-0010) builds the 3D view and Front/Side elevations: F-10 proposes a t/c nudge ladder 0.01/0.1/1 %; F-11 reads CAD-04's body plan as overlaid sections and CAD-06's F as fit-selection; deviations D-2 (Front camera, starboard on the viewer's left), D-9 (quad order), D-10 (Side nose right), D-11 (probe overlay, not a strip)."
        },
        {
          "by": "mockup-property-grid",
          "on": "2026-10-01",
          "reason": "F-1 property grid: Properties becomes identity + collapsible groups + label | value | unit rows; Tangent is a labelled group shown on handles too (F-4); one identity per selection (O-6); units and UI-40 precision everywhere (O-4); estimates Unavailable with a reason instead of ≈ — (COPY-155); COPY-149..157 proposed. Review §11.4 Properties rows and the precision conflict DR-UID-1."
        },
        {
          "by": "property-grid-rulings",
          "on": "2026-10-01",
          "reason": "Operator rulings DR-UID-1 and MC-6 need spec-owner amendments: precision follows the quantity (UI-40 angle text: placed/typed 0.01°, derived 0.1°; placed t/c 0.01 %; station chord at root/tip 0.01 mm; m12b §11.4 \"Lengths display at 0.01 mm\" covers typed dimensions only; status \"MAC 101.3 mm\"); a point's spanwise coordinate is \"From root\" with η (hover/peer names, probe, CAD-15/UI-37); A4.8 expressions are set once; COPY-149..171 proposed; DR-UID-5 amends UI-36 to \\\"the Wing block is pinned and always fully visible; the selection section may scroll; groups stay collapsible and remember state\\\"; a typed twist or t/c past the domain is clamped in Core with a warning echo, as m12b2 says (MC-19)."
        }
      ],
      "summary": "The specification the product is built against. Seven discrete, complementary areas — Setup · CAD · Analysis · Experiment setup · Run · Results · Export — each with an AI prompt entry whose output is a typed, validated, previewed proposal. One explicit parametric definition whose payload reproduces its surface; an operating point that carries depth, water and a goal state; analysis tiers that may claim only what their fixtures earn; a catalog admitted by rights class; a sweep-or-optimize experiment driven end to end against OpenFOAM or SU2 with evidence by files; results as sequences of admitted samples with named bases; hard states and fixed copy for every honest limit. Revision 1.5 adds persistent section editing, shared-profile scope, draft-safe inspection, named design alternatives and explicit geometry-intent commands to FoilDSL authoring. Revision 1.6 makes the CAD area CAD-first (mockup v10): a start card, a workspace of views with a narrow left Properties pane and optional docks, per-point Anchor/Control types, typed Span/Root chord/Tip chord with always-visible derived Wing estimates (mean chord S/b, MAC, max t/c, AR, area), a section editor mode with Finish/Cancel, and Replace from catalog / Save to My sections; superseded 1.1a–1.5 wording is marked in place (Appendix G).",
      "tags": [
        "hydrofoil",
        "cad",
        "parametric",
        "cross-platform",
        "simulation",
        "build-basis"
      ],
      "links": [
        {
          "to": "adr-application-stack",
          "rel": "relates-to"
        },
        {
          "to": "adr-application-project-contract",
          "rel": "relates-to"
        },
        {
          "to": "decision-design-iteration",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v7",
          "rel": "relates-to"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench-v6",
          "rel": "relates-to"
        },
        {
          "to": "design-foildsl-authoring",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench",
          "rel": "refines"
        },
        {
          "to": "kb-hydrofoil-workbench",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-glossary",
          "rel": "uses-term"
        },
        {
          "to": "domain-experts",
          "rel": "depends-on"
        },
        {
          "to": "review-spec-v02-critique",
          "rel": "depends-on"
        },
        {
          "to": "kb-cfd-workbench-grounding",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v3",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v4",
          "rel": "relates-to"
        },
        {
          "to": "cad-editing-views",
          "rel": "relates-to"
        },
        {
          "to": "thick-client-shell",
          "rel": "relates-to"
        },
        {
          "to": "plan-knowledge-experts-spec-v1",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-workbench-v10",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-workbench-v9",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-workbench-v8",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "note-m1-scope-decision",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "A3.5 Relationships (reference direction)",
          "mermaid": "flowchart TD\nP[Project] -->|1..*| B[Setup brief]\nB -->|seeds 1| G[Goal state]\nB -->|seeds 1| D[Design revision]\nP -->|1..*| D\nP -->|0..*| R[Analysis run]\nP -->|1..* versions| G\nP -->|0..*| Q[Experiment]\nQ -->|1| D\nQ -->|1| G\nQ -->|0..1| C[Case schedule]\nQ -->|0..*| N[Candidate]\nQ -->|1 pin value, 0..1 row at Run| V[Backend environment]\nV -->|0..*| Z[Backend check]\nQ -->|0..*| J[Evaluation]\nN -->|1| J\nN -->|0..* promotion| R\nN -->|0..1 applied| D\nT[Sweep attempt] -->|0..*| H[Field evidence]\nD -->|1| S[Surface revision]\nD -->|0..1 parent| D\nS -->|1 per station| F[Profile revision]\nG -->|1..*| O[Operating point]\nR -->|1| S\nR -->|1| O\nR -->|0..*| L[Strip load]\nX[Discrepancy record] -->|2| R\nX -->|or 2| Y[Polar sample]\nY -->|1| F\nC -->|1..* attempts| T\nT -->|0..1| R\nK[DRC finding] -->|1| D\nK -->|0..1| E[Geometry edit draft]\nA[Assistance proposal] -->|1 base| D\nE -->|1 base| D"
        },
        {
          "kind": "flowchart",
          "title": "B2. Flow F1 — start, first launch, preserve (DOC-01–04)",
          "mermaid": "flowchart TD\nA[Launch] --> B{First launch?}\nB -->|Yes| C{Example fixture valid?}\nC -->|Yes| D[Open Example race light: station selected, tips visible]\nC -->|No| E[New with parameter form; name the missing fixture]\nB -->|No| F{Recent document?}\nF -->|None| D2[Start: Example, New, Open]\nF -->|Reopens| G[Design workspace]\nF -->|Missing or invalid| H[Explain; Locate / Open / Example]\nF -->|Unsupported major version| H2[Keep active document; name version and path; no migration]\nF -->|Older migratable version| H3[Migrate to a copy; original kept]\nF -->|Unknown optional content| H4[Retain read-only or refuse before save]\nD --> G\nD2 --> G\nE --> G\nH --> G\nH2 --> G\nH3 --> G\nH4 --> G\nG --> I[Edit preview]\nI -->|Accept| J[Dirty Design revision]\nI -->|Cancel| G\nJ --> K{Save succeeds?}\nK -->|Yes| L[Saved]\nL --> G\nK -->|No| M[Keep dirty state; retry or Save as]\nM --> K\nJ --> N[Interrupted session]\nN --> O[Compare recovery revision with saved]\nO --> G\nJ --> Z{Close with unsaved work}\nZ -->|Save| K\nZ -->|Discard| A\nZ -->|Cancel| G"
        },
        {
          "kind": "flowchart",
          "title": "B3. Flow F2 — shape through curves and stations (GEO-01–15, CAT-01–04)",
          "mermaid": "flowchart TD\nA[Recipe from the Brief preset, or open document] --> B[Evaluate one explicit surface]\nB --> C{Edit target}\nC -->|Distribution| D[Select mode, control or handle]\nC -->|Station| E[Select plane, row or profile]\nC -->|Edit section| M[Source-linked draft with residual]\nC -->|Smooth or Fair| W[Preview polygon, curve, comb, deviation, locks]\nD --> F[Drag, nudge, type expression or weight]\nE --> F\nM --> F\nW --> F\nF --> G{Recipe still linked?}\nG -->|Yes| H[Preview detachment]\nG -->|No| I[Preview same model]\nH --> I\nI --> J{Constraints feasible, residual within acceptance, no error finding?}\nJ -->|Yes, Apply| K{Apply commits?}\nK -->|Yes| K2[Append Design revision; dependents Historical]\nK -->|No| K3[Roll back to base; then show the error]\nK3 --> I\nK2 --> B\nJ -->|Infeasible locks| L[Show DOF, conflicting locks, removable list]\nJ -->|Residual above acceptance| L2[Show deviation and acceptance; Apply disabled]\nJ -->|Crossing or fold| L3[Located DRC finding; closed export blocked; Apply disabled]\nJ -->|Insufficient controls| L4[Blocked with the Schoenberg–Whitney reason]\nL -->|Release lock| I\nL -->|Correct| F\nL -->|Cancel| B\nL2 --> F\nL3 --> F\nL4 --> F\nI -->|Cancel| B\nQ[Add profile from DAT] --> R{Parse both layouts}\nR -->|Invalid| T[Name line or shape; library unchanged]\nR -->|Ambiguous| S[Show both previews; choose or cancel]\nS -->|Cancel| B\nR -->|Detected| U[Preview layout, order, closure, hash; switch layout]\nS --> U\nU -->|Add to library| U2[Library entry with provenance; No analysis]\nU2 -->|Assign to station| E\nU2 -->|Done| B\nU -->|Cancel| B\nT --> Q\nT -->|Cancel| B\nV[Rank sections at the operating points] --> V2{Admitted?}\nV2 -->|Pending admission| V3[Excluded with reason]\nV2 -->|Admitted| V4[Rank per point, both Ncrit, surface state, tier chip]\nV3 --> V"
        },
        {
          "kind": "flowchart",
          "title": "B4. Flow F3 — brief and feasibility (GOAL-01–03, CAT-04)",
          "mermaid": "flowchart TD\nA[Open Brief] --> B[Enter masses, water, depth band, wind band]\nB --> C{Every field labelled?}\nC -->|Flagged field| D[Show label and source; acknowledge]\nD --> E\nC -->|Yes| E[Derive speed band and operating points with h_ref]\nE --> E2[Choose discipline preset; Constraint set written]\nE2 --> E3{Design exists?}\nE3 -->|No| E4[Generate recipe from preset; one Design revision]\nE3 -->|Yes| F\nE4 --> F[Choose class rule preset]\nF --> G[Validate recipe]\nG -->|Violation| H[Located DRC finding with rule, version, date]\nH --> I\nG -->|Pass| I[Feasibility matrix per point and constraint]\nI -->|Unavailable cell| J[Show reason: depth, envelope, capability]\nJ --> I\nI -->|Edit constraint row| I2[New Goal state version; recompute]\nI2 --> I\nI --> K[Open CAD or Analysis; goal state unchanged]"
        },
        {
          "kind": "flowchart",
          "title": "B5. Flow F4 — analyze and compare (ANA-01–20, DRC-01)",
          "mermaid": "flowchart TD\nA[Choose Section or Wing] --> B{Operating point}\nB -->|From Goal state point n| B2[Speed, water, h_ref, load copied; read-only link]\nB -->|Custom| B3[Set speed, water, incidence or load; Goal state untouched]\nB2 --> C\nB3 --> C{Depth set?}\nC -->|No| D[σ, Fr_h, V_crit Unavailable; Set depth stays offered]\nD --> E\nC -->|Yes| E[Derive h/c, Fr_h, σ per station]\nE --> E2{Any h(y) ≤ 0?}\nE2 -->|Yes| E3[Station estimator Unavailable; surface-piercing flag only]\nE3 --> F\nE2 -->|No| F{Water record admitted for T and S?}\nF -->|No| G[Unavailable: outside the ITTC table; choose admitted range]\nG --> B\nF -->|Yes| H{Polar data at this Re and profile?}\nH -->|No| I[Unavailable: outside the Re grid or no backend; choose an admitted point]\nI --> B\nH -->|Yes| L[Compute at Ncrit pair against the pinned revision]\nL --> M{Outcome}\nM -->|Success| N[Results with labels, omissions, depth basis, band]\nM -->|Derived quantity outside a bound| K[Out-of-envelope observation: advisory finding; result kept]\nK --> N\nM -->|Failed| O[Keep Historical; inspect reason; retry]\nO --> B\nM -->|Find α or take-off: no crossing| O2[Reason enum shown; nothing extrapolated]\nO2 --> B\nN --> P[Compare revisions or tiers]\nP -->|Incompatible references| Q[Block delta until reconciled]\nQ -->|Reconcile reference quantities| P\nP -->|Compatible| R[Normalised per-point deltas; Discrepancy record]\nN -->|Definition, setting or method changed| S[Historical banner; recompute]\nS --> L\nN --> T[Checks drawer: envelope and label findings]\nN -->|Sweep this| U[Jump chip: define a Case schedule in Experiment]"
        },
        {
          "kind": "flowchart",
          "title": "B6. Flow F5 — export and optional assistance (EXP-01–03, AI-01–06)",
          "mermaid": "flowchart TD\nX[Current design: Export, no AI required] --> L[Choose format, unit and tolerance]\nL --> M{Geometry and format checks pass?}\nM -->|No| N[Explain; TE floor finding; return to geometry]\nM -->|STEP without CAM fixture| P[Unavailable: open-and-measure proof pending]\nM -->|Yes| O{Write}\nO -->|Success| S[Export with revision, deviation and safety string]\nO -->|Denied or disk full| T[Preserve existing file; choose path or retry]\nT --> L\nA[Assistant entry point] --> B{Key and consent?}\nB -->|No| C[Disabled with Configure key; manual path remains]\nB -->|Yes| B2{Model evaluated?}\nB2 -->|No| C2[Unevaluated on this model; proposals disabled; explanations labelled]\nB2 -->|Yes| B3{Within caps?}\nB3 -->|No| C3[Cap exceeded: per-request or daily; raise in Settings or wait]\nB3 -->|Yes| D[Inspect redacted payload; submit]\nD --> D2{Transport}\nD2 -->|401, timeout or quota| C4[Named error; retry; manual path remains]\nD2 -->|Response| E{Response valid?}\nE -->|Schema or domain failure| F[Show rejected fields with bounds; dismiss]\nE -->|Proposal| G[Labelled fields, preview, diff]\nG -->|Accept and base unchanged| H[One Design revision]\nG -->|Base changed| I[Refresh preview]\nI --> G\nG -->|Discard| J[Document unchanged]\nE -->|Explanation| K{Every numeral in shared context?}\nK -->|Yes| K2[Citations to run or knowledge id]\nK -->|No| K3[Withheld with the reason]"
        },
        {
          "kind": "flowchart",
          "title": "B6a. Flow F6 — setup from language or parameters (SET-01–04, AI-02)",
          "mermaid": "flowchart TD\nA[Open Setup] --> B{Entry}\nB -->|Language| C{Key configured?}\nC -->|No| D[No-key string; parameter form remains]\nD -->|Fall back| E\nC -->|Yes| F[Type the description; inspect redacted payload; Propose]\nF -->|401, timeout or quota| F2[Named error; retry; parameter form remains]\nF2 --> F\nF --> G{Proposal valid?}\nG -->|Rejected fields| H[Show each field with its bound; edit or Discard]\nH --> F\nG -->|Valid| I[Preview seeded planform, Goal state, per-field provenance]\nI -->|Accept| J[Setup brief + Goal state version + Design revision r1]\nI -->|Base changed before Accept| I2[Refresh preview]\nI2 --> I\nI -->|Flagged field| I3[Acknowledge to continue]\nI3 --> I\nI -->|Re-seed on an edited document| I4[New brief version; r2…rn kept as history; runs Historical]\nI4 --> J\nI -->|Discard| A\nB -->|Parameters| E[Purpose, soft targets with weights, rider mass, water]\nE --> K{Targets consistent?}\nK -->|Conflict| L[Seed nearest feasible; name the conflict; keep targets as preferences]\nL --> I\nK -->|Yes| I\nJ --> M[Open CAD or Analysis]"
        },
        {
          "kind": "flowchart",
          "title": "B6b. Flow F7 — experiment setup and run (XS-01–03, RUN-01–06, AI-09, AI-11)",
          "mermaid": "flowchart TD\nA[Open Experiment] --> B{Kind}\nB -->|Sweep| C[Angles × speeds or Goal-state points; held water, depth, geometry, method]\nB -->|Optimize| D[Objective over multipoint set; constraints incl. A_cav; design vector; robustness; tier; budget]\nB -->|Describe the experiment| E[experiment-config proposal; preview; edit]\nE --> C\nE --> D\nD --> D2{Single-point objective?}\nD2 -->|Yes| D3[Refused with the A5.9 string; add a point]\nD3 --> D\nD2 -->|No| F\nC --> F[Preview cases with derived quantities and estimate]\nF -->|Invalid sample| G[Blocked; input named]\nG --> C\nF -->|Queue| H[Experiment version immutable; status Queued]\nH --> I[Open Run]\nI --> I2{Tier}\nI2 -->|local · in-process| S2[Evaluate in process; attempts and evidence as for a backend]\nS2 --> X\nI2 -->|backend| J{Backend Ready?}\nJ -->|No| K[Detection; Prepare my environment: step ids only; parameters bound by the tool]\nK -->|Step failed or declined| L[Recoverable; CAD works; Run stays Not ready]\nK -->|Step refused: outside the allow-list| L\nK -->|Smoke test passes: Backend check fact| M[Ready]\nJ -->|No row matches the pin| L2[Not ready; pin named; nothing launches]\nJ -->|Yes| M\nM -->|Disk exhausted or version mismatch| M2[Stop safely; case retained; retry from a valid stage]\nM2 --> M\nM --> N{Case supported by capability record?}\nN -->|No| O[Unsupported with reason; other cases proceed]\nN -->|Yes| P[Meshing]\nP -->|Cancel| T\nP --> Q{Mesh gate}\nQ -->|Fail| R[Stopped before solving; measure and threshold named; Explain this failure]\nR -->|Repair accepted| R2[New Experiment version in Draft; Open repaired draft]\nQ -->|Pass| S[Solving: residuals, forces, elapsed, resources]\nS -->|Cancel| T[Substrate kill; tree kill; orphan scan; Cancelled with partial outputs]\nS -->|Crash| U[Failed with reason; logs retained; Retry sample]\nS -->|Exit| V{Outputs present?}\nV -->|No| U\nV -->|Yes| W[Harvesting: evidence by files; Field evidence rows]\nW -->|Cancel| T\nW -->|Unknown column layout| U\nW -->|Checkpoint valid| W2[Resume offered from the validated checkpoint]\nW --> X[Completed; Converged label if criteria met]\nS -->|App quit| S3[Orphan scan on relaunch; state from events]\nS3 --> S\nX -->|All cases terminal| X2{Experiment status}\nX2 -->|≥ 1 Completed| Y\nX2 -->|0 Completed| X3[Experiment Failed; reasons per case]\nX --> Y[Open Results]"
        },
        {
          "kind": "flowchart",
          "title": "B6c. Flow F8 — results, replay and candidates (RES-01–05, AI-10)",
          "mermaid": "flowchart TD\nA[Open Results] --> B{Admitted samples?}\nB -->|None| C[Empty: no admitted sample; reasons per case; open Run]\nB -->|Some| D[Sample list with status; layer list from the evidence manifest]\nD --> E{Layer}\nE -->|Present| F[Render with legend fields, isolines, probe, table twin]\nE -->|Absent| G[Unavailable with reason: field missing · not computed · failed]\nE -->|Reduction failed| G2[Reduction failed string; raw case retained]\nE -->|Separation| H{τ_w on wall?}\nH -->|Yes| I[Separation layer with named criterion]\nH -->|No| J[No supported criterion; vortex-core candidates only]\nD --> K[Replay: held speed or held angle; Play, step, scrub]\nK -->|Failed sample| L[Pause; clear fields and metrics; reason one action]\nK -->|Reduced motion| M[Stepping only; no autoplay]\nD --> N[Sweep visuals: small multiples; metric vs α and speed with gaps; difference flood pinned at 0]\nD -->|Optimize| O[Candidates with provenance; Pareto or parallel coordinates]\nO -->|Accept candidate| P[Geometry edit draft in CAD; never direct geometry]\nO -->|Base revision moved| P2[Accept disabled; Rebase offered with deviation]\nO -->|Zero feasible candidates| P3[Terminal reason only]\nD -->|Experiment revision superseded| D2[Historical banner on every layer]\nK -->|Incompatible series| L2[Unavailable — mesh differs; no replay across series]\nD --> Q[Ask about this result: cited answer or No supported criterion]\nD --> R{ParaView 5.12+ present?}\nR -->|Yes| R1[Open in ParaView: case directory hand-off]\nR -->|No| R2[Absence string; surface floods and forces remain]"
        },
        {
          "kind": "flowchart",
          "title": "B9. FoilDSL authoring flow (SRC-01–11)",
          "mermaid": "flowchart TD\nA[Accepted foil and source] --> B{Edit route}\nB -->|Visual| C[Shared geometry draft and source patch]\nB -->|FoilDSL| D[Editable source draft]\nB -->|Open or New| D\nC --> E[Validate candidate and base revision]\nD --> E\nE -->|Invalid or incomplete| F[Diagnostic with location and repair; accepted shape retained]\nF -->|Edit again| D\nF -->|Cancel| A\nE -->|Unsupported| G[Explain unsupported feature or migration requirement]\nG -->|Cancel or keep original| A\nE -->|Valid| H[Labelled candidate preview and change summary]\nH -->|Apply| I[Append accepted source and semantic revision if changed]\nH -->|Cancel| A\nI --> J[Geometry and text projections agree; run freshness recomputed]\nJ -->|Undo or Redo| K[Select matching historical source and definition]\nK --> A\nJ -->|Save| L[Write project or explicit shape-only source]\nL -->|Failure| M[Previous file intact; retry or save elsewhere]\nM --> L\nL -->|Reopen and validate| A"
        },
        {
          "kind": "flowchart",
          "title": "B10. Flow F10 — one uninterrupted design decision (revision 1.5)",
          "mermaid": "flowchart TD\nA[Inspect accepted design] --> B[Pin immutable baseline]\nB --> C[Create and name alternative]\nC --> D[Select middle authored station]\nD --> E[Persistent thumbnail and Edit section]\nE --> F{Shared or independent scope}\nF -->|Shared| G[Show all assignments and adjacent intervals]\nF -->|Independent| H[Copy profile and preview selected assignment intervals]\nG --> I[Choose thickness policy and edit section]\nH --> I\nI --> J[Inspect another station or 3D impact without retargeting draft]\nJ --> K{Valid supported change}\nK -->|No| L[Explain lock or geometry failure; retain draft]\nL --> I\nK -->|Cancel| D\nK -->|Apply| M[Accepted alternative revision and source]\nM --> N[Compare geometry and compatible evidence with pinned baseline]\nN --> O{Evidence available and compatible}\nO -->|Yes| P[Show provenance and difference basis]\nO -->|No| Q[Show missing or incompatible reason without a number]\nP --> R[Write decision rationale]\nQ --> R\nR --> S{Keep or discard}\nS -->|Keep| T[Record decision; chosen alternative stays active]\nS -->|Discard| U[Record decision; archive alternative; return to baseline]\nS -->|No rationale| R"
        },
        {
          "kind": "flowchart",
          "title": "B11. Flow F11 — CAD-first journey and the section editor (revision 1.6; CAD-14–20)",
          "mermaid": "flowchart TD\nA[Launch or File menu] --> B{Foil open?}\nB -->|No| C[Start a foil: New foil, New from example, Open, Recent]\nC -->|New foil or New from example| W[Design workspace: views, Properties with Wing block]\nC -->|Example fixture missing or corrupt| E1[Fixture named; nothing overwritten; New foil available]\nE1 --> C\nC -->|Open or Recent| O[Opening file with Cancel]\nO -->|Cancel| O2[Opening cancelled. Nothing changed]\nO2 --> C\nO -->|Newer version| O3[File named; file unchanged; Open another file]\nO3 --> C\nO -->|Missing, migratable or unknown content| O4[F1 outcome: Locate, migrate to a copy, or read-only; cause named]\nO4 -->|Resolved| W\nO4 -->|Not resolved| C\nO -->|Opened| W\nB -->|Yes| W\nW --> P{Select}\nP -->|Point| P1[Properties: type, position, tangent kind, handles]\nP1 -->|Drag or nudge| P2{Edges cross?}\nP2 -->|No| P3[One undo step; estimates follow live]\nP2 -->|Yes| P4[Refused; geometry unchanged; reason shown]\nP3 --> W\nP4 --> P1\nP1 -->|Change type| P5[One undo step; curve changes between neighbouring anchors only]\nP5 --> W\nP -->|Several points| M[Properties: shared values, Mixed where they differ; a typed value sets every point]\nM --> W\nP -->|Any selection| D[Wing block: type Span, Root chord or Tip chord]\nD --> D1{Valid length and edges stay apart?}\nD1 -->|No| D2[Inline error; geometry and undo depth unchanged]\nD2 --> D\nD1 -->|Yes| D3[One undo step; estimates update]\nD3 --> W\nD -->|Tip closes| D4[Tip chord not editable; edit the tip station]\nD4 --> W\nP -->|Station, then Edit section| S[Section editor: focus on first point; Wing dimensions read-only]\nS --> S1{Action}\nS1 -->|Edit points or types| S2{Surfaces cross?}\nS2 -->|Yes| S3[Finish disabled with the reason]\nS3 --> S1\nS2 -->|No| S1\nS1 -->|Replace from catalog| K[Search NACA, Eppler, Speer, My sections]\nK -->|No match| K1[No sections match; try NACA, Eppler or a name]\nK1 --> K\nK -->|Pending or cite-only entry| K2[Disabled with its reason; nothing changes]\nK2 --> K\nK -->|Admitted entry| K3[Dashed preview; fit deviation; stations replaced named]\nK3 -->|Replace| K4[Catalog original chip; one step inside the draft]\nK3 -->|Cancel| S1\nK -->|Cancel or Escape| S1\nK4 --> S1\nS1 -->|Save to My sections| V[Name and provenance]\nV -->|Empty or duplicate name| V1[Inline error; nothing saved]\nV1 --> V\nV -->|Cancel or Escape| S1\nV -->|Save| V2[Entry in My sections; section and assignments unchanged]\nV2 --> S1\nS1 -->|Smooth| SM[Smooth dialog: tolerance, points, largest change]\nSM -->|OK| S1\nSM -->|Cancel| S1\nS1 -->|Switch to Analysis| S8[Draft hidden, not lost; editor back on return]\nS8 --> S1\nS1 -->|Edited station removed| S9[Mode ends; draft discarded; reason in the status line]\nS9 --> W\nS1 -->|Close window with unsaved edits| S10[F1 unsaved-close choice; safe choice focused]\nS10 -->|Cancel| S1\nS10 -->|Save| S11[Finish or Cancel the section first; nothing saved or discarded]\nS11 --> S1\nS10 -->|Discard| S12[Window closes; section draft and unsaved changes discarded by explicit choice]\nS1 -->|Other station with unsaved edits| S4[Refused: finish or cancel first]\nS4 --> S1\nS1 -->|Escape with unsaved edits| S5[Focus moves to Cancel; nothing discarded]\nS5 --> S1\nS1 -->|Finish section| S6[One undo step; back to the station in the workspace]\nS1 -->|Cancel| S7[Entry state restored; undo depth unchanged]\nS6 --> W\nS7 --> W"
        },
        {
          "kind": "flowchart",
          "title": "B12. Flow F12 — panes, docks and floats (revision 1.6; UX-31–32)",
          "mermaid": "flowchart TD\nA[Workspace preset: Planform, Precision or Review] --> B[Pane in a dock]\nA -->|At launch a float's monitor is gone| L[Float clamped onto a connected monitor]\nL --> F\nB -->|Maximize| X[Pane fills the window; the rest inert]\nX -->|Escape or restore| B\nB -->|Close a pane| J2[Pane closed; Window menu Panes lists it to show again]\nJ2 --> B\nB -->|Move to, or drag to a drop zone| C[Pane in another dock or tab group]\nB -->|Float| F[Float over the model area]\nC --> B\nF -->|Escape or dock back| B\nF -->|Alt and arrows or Position menu| F\nF -->|A control under the float takes focus| G{A corner of the model area clears it?}\nG -->|Yes| H[Float moves to the nearest clear corner and says so]\nG -->|No| I[Float docks back where it came from and says so]\nH --> F\nI --> B\nB -->|Close the last pane of a dock| J[Dock closes; toggle shows it again]\nJ --> B\nB -->|Switch workspace| K[That workspace's remembered layout]\nK --> B\nB -->|Reset layout| A"
        }
      ],
      "sourceSha256": "9a90b984d2667b3ebcfd329747e9d03d931c0fc2c1b5514d8a5d060f0c2812e2"
    },
    {
      "id": "spec-foildsl",
      "path": "docs/specs/foildsl.md",
      "title": "FoilDSL 4.0 — canonical foil and section authoring language",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-03-22",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-22",
          "reason": "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors."
        }
      ],
      "summary": "Normative proposed FoilDSL 4.0 language contract for lossless control-vertex foil and section documents. Defines complete syntax, evaluation, identity, draft transactions and migration from the supplied v3 references. Production conformance remains an acceptance obligation; the workbench demonstrates a declared subset.",
      "tags": [
        "foildsl",
        "geometry",
        "language",
        "canonical-authority"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "decision-foildsl-reconciliation",
          "rel": "depends-on"
        },
        {
          "to": "decision-parametric-authority",
          "rel": "refines"
        },
        {
          "to": "decision-design-iteration",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-parametric-curves-lofts-and-surfaces",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-file-formats-and-grammars",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "9. UX contract and acceptance cases",
          "mermaid": "flowchart TD\n  A[Accepted source and shape] --> B[Visual edit or source draft bound to base]\n  B --> C[Validate]\n  C -->|Invalid or incomplete| D[Locate error; accepted view labelled; Apply disabled]\n  D --> B\n  C -->|Valid supported definition| E[Preview shape and source diff]\n  C -->|Valid unsupported feature| U[Keep source; explicit unsupported message]\n  E -->|Cancel| A\n  B -->|Cancel| A\n  E -->|Apply at unchanged base| F[Atomic source revision and geometric identity]\n  E -->|Base changed| G[Conflict; rebase or discard]\n  G --> B\n  F --> H[Recompute result freshness from run key]\n  H -->|Undo| A\n  A -->|Redo accepted edit| F"
        }
      ],
      "sourceSha256": "2e5491affa63a5ae11ffd1ff1bcaa374b0eafe12cee8eb11315f53e6c418886a"
    },
    {
      "id": "threat-model",
      "path": "docs/security/threat-model.md",
      "title": "Application security boundary review",
      "type": "threat-model",
      "status": "proposed",
      "owner": "@cfd-owner-20260923",
      "phase": "architecture",
      "reviewBy": "2027-03-23",
      "reviewSuggested": [
        {
          "by": "design-application-contracts",
          "on": "2026-09-23",
          "reason": "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams."
        },
        {
          "by": "architecture-application",
          "on": "2026-09-23",
          "reason": "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open."
        },
        {
          "by": "design-application-foundation",
          "on": "2026-09-23",
          "reason": "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims"
        }
      ],
      "summary": "Rolls up the offline application's file, command, rendering and telemetry threat analysis. Mitigations are proposed and tested only to the extent recorded in the architecture spike proof; filesystem race handling and distribution trust remain independent release gates.",
      "tags": [
        "security",
        "application",
        "files"
      ],
      "links": [
        {
          "to": "architecture-application",
          "rel": "documents"
        },
        {
          "to": "design-application-foundation",
          "rel": "documents"
        },
        {
          "to": "design-application-contracts",
          "rel": "documents"
        },
        {
          "to": "design-app-shell",
          "rel": "documents"
        },
        {
          "to": "design-m12b-points",
          "rel": "documents"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cd14639d50088abc46de5bea5930b5f67b9050680a65f3821b50ee60f2631e3a"
    }
  ],
  "surfaces": [
    {
      "id": "surface-audit-index",
      "path": "docs/audit/index.html",
      "title": "CFD-Workbench — Audit & Change Log",
      "kind": "audit",
      "description": "Browse the committed audit and change timeline.",
      "artifactId": "audit-log"
    },
    {
      "id": "surface-mockups-workbench-v8",
      "path": "docs/mockups/workbench-v8.html",
      "title": "CFD Workbench — CAD-first direction (v8 r2)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v8"
    },
    {
      "id": "surface-mockups-workbench-v10",
      "path": "docs/mockups/workbench-v10.html",
      "title": "CFD Workbench — docked panes (v10)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v10"
    },
    {
      "id": "surface-mockups-workbench-v9",
      "path": "docs/mockups/workbench-v9.html",
      "title": "CFD Workbench — docked panes (v9 r2)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v9"
    },
    {
      "id": "surface-mockups-property-grid",
      "path": "docs/mockups/property-grid.html",
      "title": "CFD Workbench — Properties property grid",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-property-grid"
    },
    {
      "id": "surface-mockups-design-language",
      "path": "docs/mockups/design-language.html",
      "title": "CFD-Workbench · Design language",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact."
    },
    {
      "id": "surface-mockups-workbench",
      "path": "docs/mockups/workbench.html",
      "title": "CFD-Workbench · Hydrofoil design studio",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench"
    },
    {
      "id": "surface-coordination-app-shell-build",
      "path": "docs/coordination/app-shell-build.html",
      "title": "CFD-Workbench — coordination plan",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "coordination-app-shell-build"
    },
    {
      "id": "surface-coordination-application-build",
      "path": "docs/coordination/application-build.html",
      "title": "CFD-Workbench — coordination plan",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "coordination-application-build"
    },
    {
      "id": "surface-coordination-m12b-build",
      "path": "docs/coordination/m12b-build.html",
      "title": "CFD-Workbench — coordination plan",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "coordination-m12b-build"
    },
    {
      "id": "surface-specs-cfd-workbench-v1",
      "path": "docs/specs/cfd-workbench-v1.html",
      "title": "CFD-Workbench — Product specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-cfd-workbench-v1"
    },
    {
      "id": "surface-specs-cfd-workbench",
      "path": "docs/specs/cfd-workbench.html",
      "title": "CFD-Workbench — Product specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-cfd-workbench"
    },
    {
      "id": "surface-mockups-workbench-v1",
      "path": "docs/mockups/workbench-v1.html",
      "title": "CFD-Workbench — workbench v1 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v1"
    },
    {
      "id": "surface-mockups-workbench-v2",
      "path": "docs/mockups/workbench-v2.html",
      "title": "CFD-Workbench — workbench v2 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v2"
    },
    {
      "id": "surface-mockups-workbench-v3",
      "path": "docs/mockups/workbench-v3.html",
      "title": "CFD-Workbench — workbench v3 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v3"
    },
    {
      "id": "surface-mockups-workbench-v4",
      "path": "docs/mockups/workbench-v4.html",
      "title": "CFD-Workbench — workbench v4 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v4"
    },
    {
      "id": "surface-mockups-workbench-v5",
      "path": "docs/mockups/workbench-v5.html",
      "title": "CFD-Workbench — workbench v4 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v5"
    },
    {
      "id": "surface-mockups-workbench-v6",
      "path": "docs/mockups/workbench-v6.html",
      "title": "CFD-Workbench — workbench v4 mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v6"
    },
    {
      "id": "surface-mockups-workbench-v7",
      "path": "docs/mockups/workbench-v7.html",
      "title": "CFD-Workbench — workbench v7 authoring review",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench-v7"
    },
    {
      "id": "surface-specs-foildsl",
      "path": "docs/specs/foildsl.html",
      "title": "FoilDSL 4.0 — Language specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-foildsl"
    }
  ],
  "graphSha256": "842f8f26aad043b3d42bc9493b03c38a5ab5ecc2fdd9652c62dae8e5ccd47727"
};
