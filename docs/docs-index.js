// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "CFD-Workbench",
  "generator": "docs-graph.py derive",
  "rootId": "a3a-vlm3-red-first",
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
        "amended",
        "ruling-62",
        "floor-4"
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
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "relates-to"
        },
        {
          "to": "proof-planform-verbs-fairness",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f471429c838a8954de4ecd383c41ca1797cbc4405b96e730ae193000fe3377dc"
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
          "by": "design-m12c-section-editor",
          "on": "2026-10-03",
          "reason": "M1.2c designs section point types per surface (Ruling 53 DR-11): Control->Anchor and Anchor->Control act on one surface; §6's paired default and its other-surface refit survive only as the OD-4 fallback. Profile tangent rows get a unit-free rule (horizontal/vertical exact, smooth/symmetric/angle within 1e-9 chord) and IsAnchor becomes degree-general."
        },
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-09-30",
          "reason": "Amendment 1 (DR-10, M1.2b design): channels hold 6-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); old builds refuse most 4.1 files with DSL-SYNTAX or DOC-UNSUPPORTED-FIELD, not DSL-VERSION (ADR-0005's rollback claim at :127 is corrected in docs/design/m12b-points.md 3.8)."
        },
        {
          "by": "design-m12b2-3d-elevations",
          "on": "2026-09-30",
          "reason": "M1.2b2 applies tangent rows to the dihedral, twist and thickness channels with a unit-free rule (ordinate deviation from the handle line within tau_c: 1 um, 1e-6 deg, 1e-8) instead of the 0.1 deg direction tolerance, which is meaningless in a metres x degrees plane (docs/design/m12b2-3d-elevations.md 3.6)."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "3d8c8faa6bc70040cb1434150f16181e77c77c1e78640d6dcd04e3d10ce439c6"
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
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "6ae9eb2359808a1afda7195ae8d538121e77cacfa1345c49d1235856464ccabb"
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
      "reviewSuggested": [
        {
          "by": "design-m12c-section-editor",
          "on": "2026-10-03",
          "reason": "M1.2c names the section-draft step record (SectionStep), the receipt (rail section) and recovery (rail section), the mode state machine, and the contract-step deletion of the M1.1 single-vertex members (seam S-3)."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "6d8f7605ceb1d329654e97d053d8592f4c8faeded2a76e07cfa42910352a248b"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "bdadf3b3425d218c7f8f0b3d56e78bf42065e03e91516bd9a7416889e6f14141"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "a211a52f861b431d72f7d2959a65b593d6d106599fe972f2fa76c16f5592093e"
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
      "sourceSha256": "f80244fbefc0ceb2f15b42560e959e19837f67f38169391b71969041a9c99cf1"
    },
    {
      "id": "adr-0011-analysis-run-storage",
      "path": "docs/adr/0011-analysis-run-storage.md",
      "title": "ADR-0011: analysis run storage — runs are append-only facts in the native project as cfdw-project-2, written only when a run exists",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "implementation — A3a PRE (Area 3 Analysis, Ruling 63)",
      "reviewBy": "none while accepted",
      "reviewSuggested": [],
      "summary": "Analysis runs are stored in the native project file as append-only rows under a new optional top-level `analysis` member. The format string is derived from the run count, so a project with no run is still written as `cfdw-project-1`, byte for byte as today; one run makes it `cfdw-project-2`. Store invariants are checked on read and in RecordRun; each run carries a content hash and its key is recomputed, never trusted. The first save to `-2` writes a `.v1.bak` first. Retention keeps every run reachable from a retained revision or the redo stack plus the latest 20 others per tier, and leaves a tombstone for each pruned run.",
      "tags": [
        "analysis",
        "persistence",
        "native-format",
        "run-key",
        "retention",
        "migration",
        "adr",
        "a3a",
        "dr-ana-4"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "implements"
        },
        {
          "to": "adr-application-project-contract",
          "rel": "refines"
        },
        {
          "to": "spec-amendments-1-7",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b9fc408b0a43a595e0b1fa1734d62a280acc4da3048eeb0f420f286a47d43c87"
    },
    {
      "id": "adr-0012-openfoam-backend-macos",
      "path": "docs/adr/0012-openfoam-backend-macos.md",
      "title": "ADR-0012: the OpenFOAM backend on macOS arm64 — substrate pin, product launcher, A4 convergence oracle and SA numerics (what rounds 1 and 2 proved)",
      "type": "adr",
      "status": "proposed",
      "owner": "@fluids-f1",
      "phase": "spike — fluids rounds 1 and 2 (Rulings 60, 65), round 3 planned",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        }
      ],
      "summary": "Pins only what fluids rounds 1 and 2 measured on macOS arm64. Substrate: OpenFOAM ESI v2512, the gerlero native app, by DMG sha256 and build id, launched by argv. Product launcher (right-sized, Ruling 68): six short rules for one trusted user on one laptop, the app-owned controlDict turns case code off and a live \"Disallowing\" banner proves it; verified by the 2026-10-04 probes (S-1, S-2, S-4); Run is enabled when the install smoke test shows Disallowing; an existing OpenFOAM install is detected and hash-checked (DR-SEC-1 A). Convergence: the A4 oracle (it rejected a period-2 cycle and accepted four runs). Numerics: SA-noft2 with first-order nuTilda and relaxation 0.7 (met A4 on TMR only). Compressibility delta measured once (+1.147 % Cl, +0.81 % Cd; U_delta not stated). Open: Windows, Docker digests, v2512 vs v2606, the mesh route, GCI. The DR-F2-6 determinant floor needs an amendment; this ADR files a request (DR-F3-1) and does not decide it.",
      "tags": [
        "adr",
        "backend",
        "openfoam",
        "v2512",
        "security",
        "launcher",
        "convergence",
        "a4",
        "spalart-allmaras",
        "mesh-gate",
        "macos"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "proof-spike-03",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-04",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03-round2",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-04-round2",
          "rel": "depends-on"
        },
        {
          "to": "plan-fluids-round2",
          "rel": "depends-on"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fd0143551ea6ffa5f28284037ee2f5b522bf49e5d21c2038a9181ff53f993fe3"
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
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "156df962dc794ac9c8b576c6751b2d45b325d0446cdfb5df98805c6e4a804d2b"
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
      "id": "note-area3-analysis-reading-contract",
      "path": "docs/notes/area3-analysis-reading-contract.md",
      "title": "Analysis reads the accepted revision through the snapshot and one Core read, never ProfileAt",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-04-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Analysis takes the accepted bytes, AcceptedId and SurfaceHash from AuthoringSession.Snapshot(), never ProfileAt (which may return a profile draft), and takes every lattice coordinate from one new Core read, Placement.Sections, built on the existing internal placement rule — so analysis can neither draw over a preview nor disagree with the drawn foil.",
      "tags": [
        "analysis",
        "reading-contract",
        "placement",
        "draft-safety",
        "area-3"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "refines"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3affd6ce0568546f2313c65461cf7e5d18fbd92067839d05d63ba38da87ecc48"
    },
    {
      "id": "note-area3-fixture-arithmetic",
      "path": "docs/notes/area3-fixture-arithmetic.md",
      "title": "Area 3 fixture numbers — the F-2 band, F-6 lattices and mutant, F-8 convention, from an independent lattice",
      "type": "decision-note",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-04-03",
      "reviewSuggested": [],
      "summary": "The numbers behind design-area3-analysis §13.2 rev 3, measured with a 56-line reference horseshoe lattice (listed here, run with Node 22): the F-2 lifting-surface band 0.4156–0.4198 for the elliptic AR 8 wing at 5°, F-6's lattices 32/64/128 (observed order 1.07 for CL, 1.01 for e), an O(1) F-6 mutant that drives the order to −0.74, and the F-8 fixture at 20° dihedral with the developed S_ref, which the dihedral-ignored mutant misses by 11.9 %. The 2026-10-04 repair adds the pointwise α_i oracle for F-15, the near-field convergence numbers for F-5 and the solver residuals.",
      "tags": [
        "analysis",
        "vlm",
        "fixtures",
        "mutants",
        "observed-order",
        "richardson",
        "area-3",
        "test-plan"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "refines"
        },
        {
          "to": "review-area3-analysis-personas",
          "rel": "relates-to"
        },
        {
          "to": "kb-hw-low-order-hydrodynamics",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4d6ccf04838c59e5f8ce0c4d0c2d44e9e1f85205a702e03fac807d7a675c47c4"
    },
    {
      "id": "note-backlog-2d-section-workbench",
      "path": "docs/notes/backlog-2d-section-workbench.md",
      "title": "Backlog: a 2D section workbench (simulate, iterate, optimise to a goal, save)",
      "type": "decision-note",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "backlog",
      "reviewBy": "2027-01-04",
      "reviewSuggested": [],
      "summary": "Operator request (2026-10-04, Ruling 72), parked for later: tune a foil section on its own in 2D - start from a section, simulate and analyse it, iterate by hand or optimise toward a goal state (e.g. lowest stall speed while keeping efficiency), then save the result as a section - so 2D section tuning is separate from 3D wing optimisation.",
      "tags": [
        "backlog",
        "section",
        "2d",
        "analysis",
        "optimisation",
        "goal-state",
        "my-sections"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "design-m12d-catalog",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a751c211ad65b84b206fabe0105424dfa8788ed278cc51c49e54e437f4ef869c"
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
      "id": "note-m12c-rulings",
      "path": "docs/notes/m12c-rulings.md",
      "title": "M1.2c — operator rulings on the section editor decisions, the ADR-0007 amendment and the section display path",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Operator and Owner rulings of 2026-10-03 on the M1.2c design, made after seeing the mockup. The editor opens in the model area. There is no Messages pane: blockers show where they block, plus Show in the status strip. The Points pane goes in the right side bar. A no-go spike ships paired point types. The ADR-0007 amendment (no step count in the receipt) is accepted. Section drawing uses the one binary64 display profile evaluator, bound to the certificate within 1e-9 chord (ADR-0010 Amendment 1).",
      "tags": [
        "m12c",
        "section-editor",
        "rulings",
        "messages",
        "points-pane",
        "display",
        "evaluator"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "refines"
        },
        {
          "to": "mockup-m12c-section-editor",
          "rel": "refines"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "refines"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "refines"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a766fb09771fddd849dfc0acfb62de6f97cfbd199c69acf62705c901d675ccdc"
    },
    {
      "id": "note-solver-security-right-size",
      "path": "docs/notes/solver-security-right-size.md",
      "title": "Solver security, right-sized: one trusted user on one laptop, cases the app writes",
      "type": "decision-note",
      "status": "applied (Ruling 68)",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        },
        {
          "by": "adr-0012-openfoam-backend-macos",
          "on": "2026-10-04",
          "reason": "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)"
        }
      ],
      "summary": "Ruling 67 asked whether the solver security requirements are over-complicated. They are. The threat model is one trusted user on his own laptop, running cases the app writes from typed templates. Against that model, 4 of the 18 current requirements stay as they are, 8 get simpler and 6 are dropped. The minimal set (M1-M8) is: the app writes its own controlDict with allowSystemOperations 0; it stops any launch whose master-rank banner is not Disallowing; argv only, built from typed values; it runs only cases whose every file it emitted (one CI emitter test, with the existing lint as oracle); each run gets an exclusive app-owned run directory; a clean child environment; resource caps with a kill fallback; the install, new or existing, is checked against the pinned hashes. Today's probe run (3 PASS / 5 FAIL) verifies the one control that matters. The 5 FAILs are 3 probe over-expectations, 1 probe defect (S-7 never reached the environment) and 1 lint gap. None is a failure of the refusal. Security review: PASS WITH CONDITIONS; simplifier: soft BLOCK cleared. All conditions are applied. Applied by Ruling 68 (2026-10-04): ADR-0012 D2, spec 1.7.1 A8.5 and A5.10, and the round-3 plan. DR-SEC-1 was ruled A (detect an existing install).",
      "tags": [
        "security",
        "openfoam",
        "launcher",
        "threat-model",
        "right-size",
        "install",
        "adr-0012",
        "ruling-67"
      ],
      "links": [
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "refines"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "relates-to"
        },
        {
          "to": "proof-spike-03-round2",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "36f4140ad8227f371e8514b58ac7b42c846b73aab8b3ab334063ce1f8961a144"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5898b7ef9a3b81056ca76f7c37cbd4c774cf0c0235ebb6737b36089aa9c9080e"
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
          "by": "design-m12c-section-editor",
          "on": "2026-10-03",
          "reason": "M1.2c OD-2 (pending operator): no Messages pane; blockers show at their source and in the status strip with one Show action; the role=log obligation of §11 is retired if ruled. Precision (⌘1/⌘2/⌘3) applies presets without memory (simplify; D4 owns memory). The Section document tab is removed: the editor is a model-area mode."
        },
        {
          "by": "property-grid-rulings",
          "on": "2026-10-02",
          "reason": "DR-STATUS-1: reports render in a 24 px status strip at the bottom of the shell plus a transient warning toast; no scrollable message list sits in or docks to the bottom bar (V3 rejected). The M1.2c Messages pane (bottom panel, history of edit reports, role log) must not be a docked scrolling pane in the bottom bar; where history goes is open (docs/reviews/ui-status-bar.md D-4)."
        },
        {
          "by": "design-language",
          "on": "2026-09-30",
          "reason": "M1.2b adds token warning-viewport (#efc576, 9.48:1 on the viewport), a Point (v10) component row superseding the v5 control-vertex row on the Plan view, and re-measures danger-viewport at 8.78:1."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "b5f2874d14d6c4f2b40c994203f7cc5a5bbaf4e08ffb4005e129b4bc9064fd14"
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
      "id": "design-area3-analysis",
      "path": "docs/design/area3-analysis.md",
      "title": "Design: Area 3 — Analysis (local tiers): the section (2D) tier, the VLM + strip (3D) tier, the Run manifest and the CAD ↔ Analysis toggle",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — Area 3 (Ruling 60, lane F2; documents only; architecture increment M3)",
      "reviewBy": "2027-04-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Detailed design of spec Area 3: Design revision + Operating point → Analysis run on the local tiers. The data model comes first: the Analysis run is an immutable fact (one evaluation of one Surface revision at one Operating point by one method version under one settings hash), Strip loads are its child facts, wing totals, Trefftz quantities and freshness are derived. Analysis reads the accepted revision by identity through the session snapshot and one new Core read built on the placement rule, never a draft. The estimator and VLM + strip tiers are own code in process; the polar tier is DR-ANA-1. Includes the fixture suite with mutants, rings and costs, a story-to-test matrix, the toggle contract, telemetry and fourteen decision requests.",
      "tags": [
        "analysis",
        "vlm",
        "strip-theory",
        "polar",
        "estimator",
        "run-manifest",
        "run-key",
        "freshness",
        "loads",
        "cavitation",
        "depth",
        "data-model",
        "area-3",
        "m3",
        "fluids-f2"
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
          "to": "decision-freshness-by-run-key",
          "rel": "depends-on"
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
          "to": "adr-application-stack",
          "rel": "depends-on"
        },
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-low-order-hydrodynamics",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "decision-seven-areas",
          "rel": "relates-to"
        },
        {
          "to": "mockup-m12b2-views",
          "rel": "relates-to"
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "mockup-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "note-area3-analysis-reading-contract",
          "rel": "relates-to"
        },
        {
          "to": "review-area3-analysis-personas",
          "rel": "tested-by"
        },
        {
          "to": "note-area3-fixture-arithmetic",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "04c3aa307ff7bb5311c1bba65f3a3c9140a14009fdde0279f685eff88e577be0"
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
      "id": "design-cross-profile-abscissa",
      "path": "docs/design/cross-profile-abscissa.md",
      "title": "Design: cross-profile abscissa — certify a blend between sections with their own point counts (compatible fit; knot propagation as fallback; Ruling 71 option 1)",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — Ruling 71 (operator 2026-10-04) and the operator's requirement change the same day; documents only; build after operator approval",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "After 'Make unique to Root', a step that changes Root's knots or control x leaves Root and Tip on different point spacings, so the blend cannot be certified. The operator then required that sections keep their own point counts. Primary design (compatible fit): every section stays as authored; the evaluator derives, never stores, one shared spacing for the wing (the richest station spacing plus every station's anchor positions) and a compatible copy of each section on it; the certificate and the placed surface both use those copies; each copy's deviation from its authored section is measured on the placed surface and admitted within 10 um, else the step is refused. Measured with the as-built Core: 6-point Tip on 10-point Root 1.5e-16 chord; 8-point Example section on 10-point NACA 5.7 um at 127 mm; fitting the richer onto the poorer fails (38 um). Fallback: copy knot changes to the neighbour (insertion exact, 6e-17 chord). Both hit the certificate's 5-span blend capacity (DR-XPA-1); SetTangent Angle already breaks one profile's paired spacing (F-XPA-1).",
      "tags": [
        "core",
        "desktop",
        "section",
        "profile",
        "abscissa",
        "compatible-fit",
        "point-spacing",
        "knot-insertion",
        "knot-removal",
        "refit",
        "certificate",
        "blend",
        "make-unique",
        "ruling-71",
        "xpa"
      ],
      "links": [
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "refines"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "refines"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "refines"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "proof-cross-profile-abscissa",
          "rel": "depends-on"
        },
        {
          "to": "proof-m12c-certificate-spike",
          "rel": "depends-on"
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "aa9802f6d721e3a4278285e29a3a349904eff21506879a94b7dac47756329798"
    },
    {
      "id": "design-dx-screen-states",
      "path": "docs/design/dx-screen-states.md",
      "title": "DX step 1: A3b and A3c screen states checked against the approved Area 3 mockup",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Every visible A3b and A3c screen state (54), set against the operator-approved Area 3 mockup (rev 3, Ruling 63). 4 are covered by the approved mockup, 3 are covered in part, 47 are not covered; the 50 not or only partly covered are rendered in dx-section-polar-states.html for the operator's approval. Each state names its copy: an approved row, a spec string, or NEW with a proposed string (42 rows need operator copy). Nine decision requests are listed.",
      "tags": [
        "analysis",
        "a3b",
        "a3c",
        "screen-states",
        "mockup",
        "copy",
        "polar",
        "cavitation",
        "find-alpha",
        "dx"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "refines"
        },
        {
          "to": "mockup-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "mockup-dx-section-polar-states",
          "rel": "documents"
        },
        {
          "to": "proof-spike-ana-1",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3c-polar-source",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e58b693773f0ca4d53316cdcf86fa6c1d3176f6dcd2e041e4bb8af6efa021682"
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
      "id": "design-group-move-node-m",
      "path": "docs/design/group-move-node-m.md",
      "title": "Proposal: group move and typed value for several points (node M, OI-3)",
      "type": "design",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "design — operator sees the mockup before any build (memory rule); track E4, round oct05",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Design for the CAD increment after the limit hold: select several points on one curve, drag them as one rigid gesture and one undo step, nudge them, and type one value for all of them (set-all, move-by, or both per row). A group that includes an end vertex holds as a whole at the Ruling 93 limit (Ruling 96). Closes spec node M's typed clause (AM-1.7-15), the multi-point half of CAD-04 and the several-vertices clause of GEO-05 (OI-3). Nine decision requests; the mockup shows the hold variants, the typed-value variants and the hard states.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "group-move",
        "typed-value",
        "node-m",
        "oi-3",
        "ruling-96",
        "ruling-93",
        "proposal",
        "operator-show"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "refines"
        },
        {
          "to": "design-next-cad-increment",
          "rel": "refines"
        },
        {
          "to": "mockup-group-move-node-m",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6cc7ede240c60ce518c5e07895243a619d492234c59b021011c0031c0e5c3dc2"
    },
    {
      "id": "design-guided-solver-setup",
      "path": "docs/design/guided-solver-setup.md",
      "title": "Design: guided solver setup — Backend environment model, step catalogue per OS and route, detection, smoke test, assistant, telemetry, tests and tracks",
      "type": "design",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "design — documents only (Ruling 67 (b)); no build until the operator approves the mockup",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut"
        }
      ],
      "summary": "The design behind the guided-setup amendment. Model: the existing Backend environment and Backend check, plus three append-only facts (Host survey, Environment step, Install acceptance) and a product-published Route catalogue; the current step, the session state and Ready are derived, never stored, so resume after a restart is a re-derivation. Routes: macOS OpenFOAM.app v2512 (6 steps; the release zip, the inner disk image and the two launch scripts are hash-pinned, all Verified on this Mac today); Windows OpenFOAM in an app-owned WSL distribution (Ubuntu 24.04.5 image and OpenCFD apt packages pinned by sha256, 8 steps, one administrator prompt, one restart); Windows SU2 v8.5.0 native (win64-omp zip pinned, 4 steps, no prompt). Every Windows behaviour is Inferred until the operator's Windows run. Findings: the Homebrew cask the operator used strips the quarantine flag (a step M8 refuses), so the product never installs through Homebrew; the spec's smoke scalar (Cl on a cavity) cannot exist. DR-SETUP-1..6 are open.",
      "tags": [
        "run",
        "backend",
        "install",
        "setup",
        "openfoam",
        "su2",
        "wsl",
        "windows",
        "macos",
        "assistant",
        "telemetry",
        "data-model",
        "ruling-67"
      ],
      "links": [
        {
          "to": "spec-amendment-guided-solver-setup",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "note-solver-security-right-size",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03-round2",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-simulation-openfoam-su2-interop",
          "rel": "depends-on"
        },
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "design-windows-runtime",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "mockup-solver-setup",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ed49b7be5af24cdd4191ce71b6881fe57ec4dbcdfb5f65fb28702f46007bbeb7"
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
          "by": "design-m12c-section-editor",
          "on": "2026-10-03",
          "reason": "M1.2c resolves §0.2's 'Messages pane' row (OD-2: no history list; DR-STATUS-1) and scopes Precision to a preset without memory (D4 owns workspace memory)."
        },
        {
          "by": "property-grid-rulings",
          "on": "2026-10-02",
          "reason": "DR-STATUS-1: reports render in a 24 px status strip at the bottom of the shell plus a transient warning toast; no scrollable message list sits in or docks to the bottom bar (V3 rejected). The M1.2c Messages pane (bottom panel, history of edit reports, role log) must not be a docked scrolling pane in the bottom bar; where history goes is open (docs/reviews/ui-status-bar.md D-4)."
        },
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
          "reason": "Operator rulings DR-UID-1 and MC-6 need spec-owner amendments: precision follows the quantity (UI-40 angle text: placed/typed 0.01°, derived 0.1°; placed t/c 0.01 %; station chord at root/tip 0.01 mm; m12b §11.4 'Lengths display at 0.01 mm' covers typed dimensions only; status 'MAC 101.3 mm'); a point's spanwise coordinate is 'From root' with η (hover/peer names, probe, CAD-15/UI-37); A4.8 expressions are set once; COPY-149..167 proposed."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "cee8a510b2b8fb5864f5303cd974120f962c8973cf4b629aac9a2c058051ff23"
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
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
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
      "sourceSha256": "794566ca4595bd209d91c8b6934c15198ceb227dc49513a86e25cc98f8edd6c2"
    },
    {
      "id": "design-m12c-section-editor",
      "path": "docs/design/m12c-section-editor.md",
      "title": "Design: M1.2c — section editor mode, per-surface section points, the Points pane and the Precision workspace",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2c (operator 2026-10-03, \"Start M1.2c next\")",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Detailed design of slice M1.2c. A station's section becomes editable as a CAD mode: Edit section (from Properties, the Plan, the Side view or the Browser) replaces the views with a 2D editor of the profile record, and every move, type change and construction is a step of one section draft (ADR-0007) that Finish commits as one undo step. Section points get Anchor/Control types per surface (DR-11), which needs the deferred B6 certificate restarted as a spike first; a paired fallback is pre-designed and tested. The slice also adds the Points pane and the Precision preset, resolves the planned Messages pane against DR-STATUS-1, and records the operator's rulings of 2026-10-03 (OD-1 A, OD-2 A, OD-3 B, OD-4 a). Gate: four lenses, one repair cycle.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "section",
        "profile",
        "section-editor",
        "point-types",
        "anchor",
        "b6",
        "certificate",
        "points-pane",
        "precision",
        "messages",
        "m1.2c",
        "dr-11"
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
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-section-editor",
          "rel": "refines"
        },
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "depends-on"
        },
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "property-grid-rulings",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-m12c-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2055ed8f07942a1533818b99b6821ab3bee9261a3fb70e5555bda11c824faa42"
    },
    {
      "id": "design-m12d-catalog",
      "path": "docs/design/m12d-catalog.md",
      "title": "Design: M1.2d — Replace from catalog and My sections in the section editor",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2d (operator 2026-10-04, \"apply an existing profile from the catalog to the section\")",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Detailed design of slice M1.2d. In the section editor, Replace from catalog… puts a NACA section or a My sections entry on the section as one undoable draft step, and Save to My sections… keeps the current section for any foil. Neighbouring sections must share their point spacing for the blend to certify (Ruling 71), so Replace fits the chosen shape onto the section's existing spacing and reports the fit against the 10 µm rule; when that fit is over the limit and a neighbour uses a different section, Replace for this station is refused with the number, and a Replace at every station of the blend is offered instead. A probe measured every number. Gate: four lenses, one repair cycle.",
      "tags": [
        "desktop",
        "core",
        "persistence",
        "cad",
        "section",
        "catalog",
        "my-sections",
        "replace",
        "provenance",
        "rights",
        "abscissa",
        "m1.2d",
        "dr-4",
        "dr-8"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "adr-0008-section-library",
          "rel": "implements"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "refines"
        },
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-m12d-catalog",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a72cac34fd2d1f165afd734f2418c3d146964ac0d9b171441eba678cadbfdaf7"
    },
    {
      "id": "design-next-cad-increment",
      "path": "docs/design/next-cad-increment.md",
      "title": "Proposal: the next CAD increment — planform limits felt during the gesture (drag holds at the minimum tip chord; root-widen refusal says what to do)",
      "type": "design",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "design — operator sees the mockup before any build (memory rule); track E3, round oct05",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Inventory of the CAD-* and GEO-* rows against main, a ranked list of candidate CAD increments for one foil designer, and one pick: make the Ruling 93 minimum tip chord a limit the drag holds at during the gesture (instead of a refusal at release), with a visible limit marker and readout, the same hold under keyboard nudge, and a root-chord refusal that says \"widen the tip first\". Core change is one clamp in the point-gesture frame; no new row flips to built, so the proposal also names the next two candidates that do (group move and typed value for several points, then comb scale and the monotone count). Mockup shows today's behaviour, three drag variants side by side, and the hard states.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "planform",
        "tip-chord",
        "gesture",
        "clamp",
        "copy",
        "ruling-93",
        "ruling-94",
        "proposal",
        "operator-show"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "refines"
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-cad-limits-in-gesture",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6e740747c19fbb2f10f079f02dd51a792982fffd166d0b10de877389b9f7feed"
    },
    {
      "id": "design-planform-point-verbs",
      "path": "docs/design/planform-point-verbs.md",
      "title": "Design: planform outline point verbs — Add point, Remove point, Rebuild to N (floor 4 at degree 3)",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — Ruling 62 (operator 2026-10-03); build after M1.2c joins",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-10-03",
          "reason": "Amendment 2 (Ruling 62): channels hold 4-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); the verbs Add point, Remove point and Rebuild to N are designed in docs/design/planform-point-verbs.md; foildsl.md 5 item 3, A4.1/A4.2/GEO-05 floor text need amendment (F-4)."
        },
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Detailed design of the three planform-outline verbs of Ruling 62. Add point (double-click on a curve) inserts a vertex by exact Boehm knot insertion. Remove point (⌫) drops one knot and refits only the two replacement vertices, refused at the floor of 4 and on named, handle and anchor points with the reason; the change is measured and reported. Rebuild to N (4–10) previews the refit with the measured largest change in mm and the curvature-break count, and applies as one undo step. Each verb is a one-shot point command (one accepted row, a new receipt kind), on all five channels, gated on FoilDSL 4.1 for the lowered floor (ADR-0001 Amendment 2). Fit points is deferred with reasons.",
      "tags": [
        "desktop",
        "core",
        "cad",
        "planform",
        "rail",
        "channel",
        "point-verbs",
        "insert-cv",
        "delete-cv",
        "rebuild",
        "knot-insertion",
        "knot-removal",
        "foildsl",
        "ruling-62",
        "floor-4"
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
          "to": "adr-0001-master-curve-degree",
          "rel": "refines"
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
          "to": "adr-foildsl-authority",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-cad-first-shell",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "refines"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "depends-on"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "mockup-planform-point-verbs",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "916081f37e656c4a648c82485078b1e2701192007e02aea2ccb2e8f993159dc8"
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
          "by": "design-m12c-section-editor",
          "on": "2026-10-03",
          "reason": "M1.2c restarts B6 as spike GSPK (x-overlay certificate, budget-derived tolerance) and replaces the dead M1.1 Section tab controls with the section editor mode; paired abscissae end when per-surface bases certify."
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
      "sourceSha256": "1ead8948b387530146f2d8e8a522823c8d2213d4ed2228ccd1f361ef41a94c1e"
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
      "id": "mockup-area3-analysis",
      "path": "docs/mockups/area3-analysis.md",
      "title": "Area 3 analysis — the Analysis surface (local tiers) in today's shell",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Nine screens of the 1280 × 800 shell for the Example foil in Analysis: no result with depth unset, evaluating, the wing result Current with loading drawn on the geometry, the same result with depth unset, a station selected (the strip with its own envelope verdict, Re against the polar range and omissions) with the Loads tab, Historical after a CAD edit with a draft hidden and the Provenance tab, a failed evaluation, a result outside the method envelope, and a run that failed its integrity check. Every load number is computed in the page by a reference vortex lattice; every label, envelope, depth basis and omission is shown. For the operator's approval before any build.",
      "tags": [
        "mockup",
        "area-3",
        "analysis",
        "vlm",
        "loads",
        "conditions-band",
        "toggle",
        "hard-states",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "documents"
        },
        {
          "to": "mockup-m12b2-views",
          "rel": "refines"
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "be86d3dee46e04b950f1aae878be23f6bef5ef5363915f5840e9f88696d0ef56"
    },
    {
      "id": "mockup-cad-limits-in-gesture",
      "path": "docs/mockups/cad-limits-in-gesture.md",
      "title": "CAD limits in the gesture — a planform drag at the minimum tip chord, three variants side by side",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Four screens for the operator's approval before any build: the tip-vertex drag at the Ruling 93 limit as today (free, refused at release), A hold at the limit, and B land at the limit on release, side by side with the Wing block and status strip; the root-chord refusal copy before and after; a keyboard run of ten presses; and the hard states (legacy file, closing tip, empty, mixed, Escape, Analysis). Every number is computed in the page from the rule max(5 mm, 2 % of root); positions are scripted, not captured from the product.",
      "tags": [
        "mockup",
        "planform",
        "tip-chord",
        "gesture",
        "clamp",
        "ruling-93",
        "ruling-94",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-next-cad-increment",
          "rel": "documents"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "915aa2d27250f12d3f378ade1408e336842365b6e5576a2ce9404b2dc1135d14"
    },
    {
      "id": "mockup-dx-section-polar-states",
      "path": "docs/mockups/dx-section-polar-states.md",
      "title": "Area 3 section and polar states (A3b, A3c) in the approved shell",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Six screens of the approved 1280 x 800 shell and a state sheet for the A3b and A3c states the approved Area 3 mockup does not draw: the Section view with Cp on the profile, the Section tab (Cp, Polar, Transition, Bucket), the cavitation screen with its governing station and margin states, the polar tier chip and flags, the drag sources, Total drag, and Find alpha. Nine decision requests for the operator.",
      "tags": [
        "mockup",
        "area-3",
        "analysis",
        "a3b",
        "a3c",
        "cp",
        "cavitation",
        "polar",
        "find-alpha",
        "hard-states",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-dx-screen-states",
          "rel": "documents"
        },
        {
          "to": "mockup-area3-analysis",
          "rel": "refines"
        },
        {
          "to": "design-area3-analysis",
          "rel": "documents"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2956a764fccea08c3ae365c6be502a168f0e4f109130375cec006eaf65193ed0"
    },
    {
      "id": "mockup-group-move-node-m",
      "path": "docs/mockups/group-move-node-m.md",
      "title": "Group move and typed value — several selected points as one gesture, three typed-value variants side by side",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Five screens for the operator's approval before any build: a group drag to the Ruling 96 tip limit with three ways to hold (whole group, limited point only, today); the typed value for several points as set-all, move-by or both per row; typed refusals with a Use action; a ten-press keyboard run; and the hard states (mixed types, two curves, locked point, locked axis, orphan handle, neighbour hold, Escape, Analysis, legacy file, one point). Every number is computed in the page from the rule max(5 mm, 2 % of root); positions are scripted, not captured from the product.",
      "tags": [
        "mockup",
        "planform",
        "group-move",
        "typed-value",
        "node-m",
        "oi-3",
        "ruling-96",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-group-move-node-m",
          "rel": "documents"
        },
        {
          "to": "design-next-cad-increment",
          "rel": "relates-to"
        },
        {
          "to": "mockup-cad-limits-in-gesture",
          "rel": "relates-to"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2c62fba4ca09e3338df968ffaf3a459b25f4803d197e2ca5b761f2b24e6e167b"
    },
    {
      "id": "mockup-m12b2-views",
      "path": "docs/mockups/m12b2-views.md",
      "title": "M1.2b2 views — the 3D view and the Front and Side elevations in today's shell",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator asked to see the 3D view and elevation mockups. Only workbench-v10.html existed, and the M1.2b2 design has moved on from it. This page draws four screens of the 1280 × 800 shell for the Example foil: Plan + 3D (default), Four views, One view 3D in Wireframe, and One view Side with a twist point selected. The shell is today's: structure-B Properties, the V2 status strip, and 4 px gutters with 1 px frames between views. The geometry is computed from the fixture's B-spline channels and the FoilDSL §6 placement rule, plus a 60 mm tip dihedral added so the Front view shows it. Three choices the design leaves open are listed with recommendations.",
      "tags": [
        "mockup",
        "m12b2",
        "3d-view",
        "elevations",
        "twist",
        "dihedral",
        "thickness",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench-v10",
          "rel": "refines"
        },
        {
          "to": "mockup-property-grid",
          "rel": "relates-to"
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b2b840d46b3afa2999952d00632c99eb259678ce31954e21847461f4f6ee57c3"
    },
    {
      "id": "mockup-m12c-section-editor",
      "path": "docs/mockups/m12c-section-editor.md",
      "title": "M1.2c section editor — editing a section from the Side view, the editor mode, and the four decisions",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator asked to edit sections (2026-10-03). This page draws five screens of today's 1280 × 800 shell for the Example foil: picking the Root station in the Side view; the section editor with point 4 made an anchor on both surfaces (paired point types, Ruling 60; comb, pointer probe, readouts, the pairing cue); a paired x move; a paired Anchor → Control refused over 10 µm; and a Finish blocked by crossing surfaces. It then shows the four decisions as side-by-side variants. All geometry, and every number printed, is computed in the page from section-a.",
      "tags": [
        "mockup",
        "m12c",
        "section-editor",
        "point-types",
        "paired",
        "points-pane",
        "messages",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "mockup-m12b2-views",
          "rel": "refines"
        },
        {
          "to": "mockup-status-bar",
          "rel": "relates-to"
        },
        {
          "to": "mockup-property-grid",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "review-ui-m12c-paired",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2d01610ad2fde60524ce1184f215ebb87428bb211a731e2483d65585616273b7"
    },
    {
      "id": "mockup-m12d-catalog",
      "path": "docs/mockups/m12d-catalog.md",
      "title": "M1.2d catalog Replace and My sections — the section editor's Replace sheet, Save to My sections, and the point-spacing refusal",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator asked to apply an existing catalog profile to a section (2026-10-04). This page extends the approved M1.2c section-editor look with nine screens of today's 1280 × 800 shell for the Example foil: the Section menu, the Replace sheet with NACA 4412 previewed on the shared section, Replace applied, Save to My sections with a duplicate name refused, the point-spacing refusal after Make unique (with Replace Root and Tip offered), a symmetric section that fits, a thin section scaled to the station t/c, My sections seen from any foil, and the hard states. Every fit and change number comes from the probe receipt; previews are the NACA closed form.",
      "tags": [
        "mockup",
        "m12d",
        "section-editor",
        "catalog",
        "my-sections",
        "replace",
        "abscissa",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-m12d-catalog",
          "rel": "documents"
        },
        {
          "to": "mockup-m12c-section-editor",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "edad592b6dd921c03a7749266dc155d9e30eab6c7c7d1dc7330990d150e7ec34"
    },
    {
      "id": "mockup-planform-point-verbs",
      "path": "docs/mockups/planform-point-verbs.md",
      "title": "Planform point verbs — Add point, Remove point and Rebuild to N on the outline, in today's shell",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Ruling 62 asked for fewer points on the planform outline, with Add point, Remove point and Rebuild to N. This page draws six screens of today's 1280 × 800 shell on the New foil: where the verbs live (Edit menu and context menus), the Rebuild preview from 10 to 4 points with the measured largest change, the rebuilt curve, Add point by double-click, Remove point with ⌫, and the refusal at the floor of 4. The rails and every number are computed in the page with the design's algorithms.",
      "tags": [
        "mockup",
        "planform",
        "rail",
        "point-verbs",
        "rebuild",
        "ruling-62",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-planform-point-verbs",
          "rel": "documents"
        },
        {
          "to": "mockup-m12b2-views",
          "rel": "refines"
        },
        {
          "to": "mockup-m12c-section-editor",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2da1a3193ddcb8ebc7c274dcd3f2a89e2d18e19a93b55e23f447723a5610c1a6"
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
        },
        {
          "to": "review-ui-property-grid-cells",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cdae6ceea97aa6e177e8e9d702e6e1dacdef2020030b5a7b4cba4f1ea6508723"
    },
    {
      "id": "mockup-property-grid-cells",
      "path": "docs/mockups/property-grid-cells.md",
      "title": "Property sheet — cell layouts for the operator to pick (Visual Studio Properties window first)",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator rejected the row-and-box structure (\"it needs to look more like cells and labels\") and then picked the Visual Studio Properties window. The page shows the same anchor point and Wing content at the real 260 px pane, 11 px text. Primary: a faithful VS Properties grid. Secondary: Premiere Effect Controls and a VS Code compact table for comparison. Nothing is built from it yet.",
      "tags": [
        "mockup",
        "properties",
        "property-grid",
        "cells",
        "operator-pick"
      ],
      "links": [
        {
          "to": "mockup-property-grid",
          "rel": "refines"
        },
        {
          "to": "review-ui-property-grid-density",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "071422f30794621b0cc4b4ae8ce037b57f183169e84cae49547f139e0649741d"
    },
    {
      "id": "mockup-solver-setup",
      "path": "docs/mockups/solver-setup.md",
      "title": "Solver setup — the guided install on Windows and macOS, in today's shell (for the operator's approval)",
      "type": "design",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut"
        }
      ],
      "summary": "The 1280 × 800 shell with a Solver setup document tab, on Windows (11 states) and macOS (7 states): first launch with no solver, nothing installed with one recommended route, WSL not turned on, the Windows administrator prompt, restart needed, resumed after the restart, virtualization off in firmware, installing, a test run that failed with an explained cause, an unknown failure with Copy a report and an assistant suggestion, Ready; on macOS an existing install that is not the tested build, the licence and download step, macOS blocked the app, a failed test after Use mine anyway, Ready. The assistant panel has three modes (answer shown, answer withheld, no key). DESIGN.md tokens, the DR-STATUS-1 status strip. Browser check green (0 errors, 0 findings, 0 contrast failures, 22 captures). For the operator's visual approval before any build.",
      "tags": [
        "mockup",
        "run",
        "backend",
        "install",
        "setup",
        "openfoam",
        "su2",
        "wsl",
        "windows",
        "macos",
        "assistant",
        "hard-states",
        "operator-show"
      ],
      "links": [
        {
          "to": "design-guided-solver-setup",
          "rel": "documents"
        },
        {
          "to": "spec-amendment-guided-solver-setup",
          "rel": "implements"
        },
        {
          "to": "mockup-status-bar",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "68819e2e8ac572c325ef25206f514fab68141a5355ae5aceb309eba0a618b539"
    },
    {
      "id": "mockup-status-bar",
      "path": "docs/mockups/status-bar.md",
      "title": "Status bar — where reports go (V2 chosen, DR-STATUS-1)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator asked for reports such as the point-type change to leave the Properties sheet and render in a status bar at the bottom of the shell. The page shows the whole shell at 1280 × 800 (structure-B Properties, the Plan, a 24 px bottom strip) in three variants and three moments. The operator chose V2 (DR-STATUS-1): a status strip plus a transient warning toast; errors stay where they arise. V1 and V3 remain only as the record of the pick; V3's scrolling Messages pane in the bottom bar is rejected.",
      "tags": [
        "mockup",
        "status-bar",
        "toast",
        "shell",
        "properties",
        "operator-pick"
      ],
      "links": [
        {
          "to": "mockup-property-grid",
          "rel": "relates-to"
        },
        {
          "to": "mockup-property-grid-cells",
          "rel": "relates-to"
        },
        {
          "to": "property-grid-rulings",
          "rel": "relates-to"
        },
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "afcbb83a0896f242f7c7f978ad866db1068c97aae1f74f60d38d6c5868797eb8"
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
      "sourceSha256": "eec813f13a7d55c5f556010f20c7a2abc15a129da2b0c657f46c6a4d58f8ec39"
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
      "sourceSha256": "790d1260faea480f213a19882fd77808c8803c32c917d9b265077592b50e2111"
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
      "id": "plan-fluids-round2",
      "path": "docs/plans/fluids-round2.md",
      "title": "Fluids round 2 — convergence, meshing and security plan for SPIKE-03 / SPIKE-04",
      "type": "doc",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Round 2 plan, documents only. Convergence: judge the iterative part on ITTC's three-order residual drop plus a stated Cl/Cd stationarity band, run OpenFOAM simpleFoam SA with a TVD nuTilda scheme on TMR levels 6/5/4, and measure (not model) the compressibility delta; SU2 v8.5.0 (SA-neg, x86_64-only macOS binary) is a referee leg only on operator approval. Meshing: wall-resolved y+ <= 1 (the wall-function floor does not fit inside the boundary layer over most of the chord at Re_c 6e5), snappy with a finite trailing edge plus a Gmsh layer probe at AR 8, and the \"determinant > 0.3\" floor replaced (OpenFOAM's cellDeterminant is not the ITTC Jacobian measure). Security: pin a product-owned controlDict, prove refusal with an operator-run probe set. Core budget about 4.1 h wall.",
      "tags": [
        "plan",
        "spike-03",
        "spike-04",
        "openfoam",
        "su2",
        "verification",
        "meshing",
        "security",
        "backend"
      ],
      "links": [
        {
          "to": "proof-spike-03",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-04",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "58e3f7b591e63e1c9190c9bc6b14332b60cd43a509cbe02577015746bacec2ab"
    },
    {
      "id": "plan-fluids-round3",
      "path": "docs/plans/fluids-round3.md",
      "title": "Fluids round 3 — bring the Gmsh wing mesh inside the gate (SPIKE-03) and find a monotone TMR grid family (SPIKE-04)",
      "type": "doc",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "adr-0012-openfoam-backend-macos",
          "on": "2026-10-04",
          "reason": "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)"
        }
      ],
      "summary": "Round 3 plan, documents only, for the two round-2 NO-GOs. Mesh: locate the 811 faces above 70 degrees on the Gmsh AR 8 mesh with checkMesh sets (no new app), then two unattended variants aimed at the measured cluster (TE arc resolution, tip poles, the prism-top/tet size jump), the first height cut to 6.0 um, then AR 5 and 12. GCI: a cheap L6 test of the one scheme that acts on L6 only (the limited laplacian), then one numerics cycle on L6/L5/L4 with the iteration band held at 1 % of the grid change; L3 (4-10 h) only by ruling. Core budget about 4.5 h wall plus 1.5 h authoring.",
      "tags": [
        "plan",
        "spike-03",
        "spike-04",
        "openfoam",
        "gmsh",
        "mesh-gate",
        "gci",
        "tmr",
        "round-3"
      ],
      "links": [
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03-round2",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-04-round2",
          "rel": "depends-on"
        },
        {
          "to": "plan-fluids-round2",
          "rel": "supersedes"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "433b18582db86fd28aecda3d727dd82b63e744c9aee4fd0c633e564b6dcc0ffc"
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
      "id": "plan-prj2-cfd-veto",
      "path": "docs/plans/prj2-cfd-veto.md",
      "title": "PRJ-2 CFD veto repair graph",
      "type": "doc",
      "status": "active",
      "owner": "@track-prj2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Bounded one-worker execution graph for the six PRJ-2 CFD corrections, with red-first checks, a shared convergence step, and the required join gates.",
      "tags": [
        "a3a",
        "prj",
        "analysis",
        "execution-graph"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3a-prj2-cfd-veto",
          "rel": "tested-by"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "PRJ-2 execution graph",
          "mermaid": "flowchart LR\n    M --> R --> F --> P --> G --> C"
        }
      ],
      "sourceSha256": "c9bfc23f35fdc549fa42267a519290dfd4066ba1c2b1cec7e45dac4d08b95387"
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
      "id": "plan-test-cost",
      "path": "docs/plans/test-cost.md",
      "title": "Test time and cost — measured baseline and ranked levers (Ruling 67)",
      "type": "doc",
      "status": "proposed",
      "owner": "@track-test-cost-study",
      "phase": "",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Where the fast ring, the readiness ring and agent repair loops spend their time on 2026-10-04 (measured), and the levers that cut it, each with its saving, coverage risk and cost to build, in a recommended order. §8 records the levers shipped on 2026-10-04 (L1-L6 and the safety fixes) with their measured before/after: readiness 348.6 s to 101.6 s, the PASS multiset unchanged.",
      "tags": [
        "testing",
        "ci",
        "cost",
        "rings",
        "performance",
        "agents"
      ],
      "links": [
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "review-test-ci-waste",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6888b97f9edae9f2fb2e7e885cafff8c05c441de0d32e1e2431580d08595810e"
    },
    {
      "id": "plan-tip-handling",
      "path": "docs/plans/tip-handling.md",
      "title": "Tip handling — one proposal for the VLM tip strip, the RANS tip mesh and the tip geometry",
      "type": "doc",
      "status": "in-review",
      "owner": "@cfd-leader-4e90c621",
      "phase": "",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Track A of round-oct05 (Ruling 80): a 9-agent judge panel (Sonnet grounding, Opus proposals, Fable comparison and synthesis, Opus completeness pass) recommends keeping Ruling 78 with a written scope (certified finite-chord tips), no tip zone for the envelope verdict, two bounded measurements (a small-tip-chord VLM sweep and a 4-variant tip mesh coupon), the open planar end as the v1 tip of record, salvage of the held branch's evidence without the eta* law, and 16 decision requests for the operator.",
      "tags": [
        "plan",
        "tip",
        "vlm",
        "a3a",
        "a3c",
        "spike-03",
        "mesh",
        "geometry",
        "ruling-78",
        "ruling-80"
      ],
      "links": [
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "relates-to"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "relates-to"
        },
        {
          "to": "coordination-round-oct05",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e6c2c81df9511f763e9d981824aa824b56d0f84cb3fc6cbeb5b0f6ff9e613f29"
    },
    {
      "id": "review-a3a-native",
      "path": "docs/reviews/a3a-native.md",
      "title": "A3a native build against the approved Area 3 mockup (AUX)",
      "type": "doc",
      "status": "in-review",
      "owner": "@trk-aux",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Nine mockup screens compared with the A3a build, with captures, differences and severity; the hydrodynamicist's re-review and the test architect's build-time veto folded in; strings with no approved copy row; what AUX fixed, what POL (the polish track) fixed after the review, and what is still listed.",
      "tags": [
        "a3a",
        "aux",
        "native-ui",
        "review",
        "analysis",
        "mockup-parity"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "mockup-area3-analysis",
          "rel": "refines"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-pack",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cc870231b82c7ced26e161276c2f2fb38fc1803723269d85dacb074c4556179e"
    },
    {
      "id": "review-area3-analysis-personas",
      "path": "docs/reviews/area3-analysis-personas.md",
      "title": "Area 3 analysis design — five lenses in Adversary Mode, and the folds",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design-slice",
      "reviewBy": "2027-04-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "Gate record for design-area3-analysis revision 1. Hydrodynamicist and Test Architect blocked (VLM result without its method envelope; story clauses without tests); CFD verification, computational geometry and data persistence approved with changes. Every finding is folded into revision 2 or carried as a DR-ANA item; the mockup's UX and accessibility review is recorded at the end. Revision 3 (repair cycle 2 of 2) folds the two lenses' rev 2 re-review (B-H1, M-H1, B-T1, M-T1…M-T3 and minors), mapped finding by finding with file:line.",
      "tags": [
        "review",
        "area-3",
        "analysis",
        "personas",
        "gate",
        "vlm",
        "data-model",
        "test-plan"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "documents"
        },
        {
          "to": "mockup-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "888036d4f5c64d15feb620430c052d4e1157ef68a943202bf34aded884dac8d8"
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
      "sourceSha256": "5a1da2dee58da5d9eae870c008061a9c946032063adbd4c430b85537149a3022"
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
      "id": "review-ui-m12c-paired",
      "path": "docs/reviews/ui-m12c-paired.md",
      "title": "M1.2c section editor — paired point types, before/after against the approved mockup",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Ruling 60 ships paired section point types (the certificate spike was a no-go). This note lists every visible change to the approved M1.2c mockup so the operator can decide: the pairing cue on canvas, Points pane and Properties; one Type and Kind control for both surfaces; a new paired x-move screen; a new refused Anchor → Control screen with the measured 0.0185 mm against the 0.010 mm limit; and four proposed COPY rows. Two findings go to the design owner.",
      "tags": [
        "ui-review",
        "m12c",
        "section-editor",
        "point-types",
        "paired",
        "sptf",
        "operator-show"
      ],
      "links": [
        {
          "to": "mockup-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "proof-m12c-certificate-spike",
          "rel": "depends-on"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9b54214270823b4370fb73efecded0ed62d94facc547fd07d824a43c0c0d4e13"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "a4c9bf84ba547df3187a47d1da87621176d140ac0604c23992e466bed6ab9e50"
    },
    {
      "id": "review-ui-property-grid-cells",
      "path": "docs/reviews/ui-property-grid-cells.md",
      "title": "UI review — the property sheet in structure B (Premiere Effect Controls), promoted to the full design",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator picked variant B, Premiere Pro Effect Controls, \"as is\" (DR-CELL-1). B is now the property sheet's structure in every state of the mockup, carrying every ruling still in force. Measured against the PGRID build: 11 px one size, 20/24 px rows, 24 px twirl headers, the Wing from 339 to 230 px, the anchor selection from 549 to 313 px. Editability is shown by colour plus a dotted underline (the non-colour cue); focus shows a box. This brief supersedes the density brief where they conflict.",
      "tags": [
        "ui-review",
        "properties",
        "property-grid",
        "premiere",
        "structure-b",
        "accessibility",
        "build-brief"
      ],
      "links": [
        {
          "to": "mockup-property-grid",
          "rel": "documents"
        },
        {
          "to": "mockup-property-grid-cells",
          "rel": "refines"
        },
        {
          "to": "review-ui-property-grid-density",
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
      "sourceSha256": "71b6c431d00dca92309dfb1ec991ec1c33c02ea0056748e144245f8876e666f1"
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
      "id": "review-ui-status-bar",
      "path": "docs/reviews/ui-status-bar.md",
      "title": "Status strip and warning toast (DR-STATUS-1) — message inventory and build brief",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "ui-design",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "The operator chose V2 (DR-STATUS-1): a status strip along the bottom of the shell plus a transient toast for commit warnings; field errors stay at their field; command reports leave the property sheet. This brief lists every message the app shows today, where it renders now and where it renders after V2, then gives the build: the strip in ShellHost row 1, the toast in ModelArea, one report sink that replaces the model area's top status line and the pane's row reports, the STATUS-CLOBBER rule at the strip, 17 named red-first checks, and the existing checks that move. Four small decisions remain for the operator.",
      "tags": [
        "ui-review",
        "status-bar",
        "toast",
        "shell",
        "properties",
        "build-brief",
        "announcements"
      ],
      "links": [
        {
          "to": "mockup-status-bar",
          "rel": "documents"
        },
        {
          "to": "property-grid-rulings",
          "rel": "implements"
        },
        {
          "to": "review-ui-property-grid-cells",
          "rel": "relates-to"
        },
        {
          "to": "design-app-shell",
          "rel": "relates-to"
        },
        {
          "to": "design-m12b-points",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "52df9e692e0ef89c423521398bf02770bea372d40ed7a6a60e5d8b9afe232ccc"
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
      "sourceSha256": "9d2d70e0aa17af6cd97fe08da2267f32ed217d5b248c4aa26693d5fc1890dceb"
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
      "sourceSha256": "c0595ad939fed834bb14f4c51aeca9d7e51514688b58d66fa7eddd33b236ccbf"
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
      "sourceSha256": "cb80d4c20f9686e5d3ea657521b95fab3915dd01f54180df0b8a62f1b4ee7ab8"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "cba56d67549de2f3d909f5649e01199eab6b5a45a2f84f5095bf01368d5effdd"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "58b48f57cdf0861c1fef89081e40150be60eb7f03b062762e23756751fd3573e"
    },
    {
      "id": "coordination-pc-kickoff",
      "path": "docs/coordination/pc-kickoff.md",
      "title": "PC session kickoff - Windows setup, smoke test, Windows Save/Open, OpenFOAM and SU2 runs, cfMesh",
      "type": "plan",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-06",
      "reviewSuggested": [],
      "summary": "The first prompt for the Claude Code session on the Windows PC (Rulings 79, 102, 106). Tasks W-0 to W-5 with their done-when evidence: setup and coord install, the first Windows smoke test, Windows Save/Open, the WSL OpenFOAM and native SU2 routes verified step by step, NACA 0012 code-to-code runs and the SPIKE-04 L3 run, and the cfMesh tip spike.",
      "tags": [
        "coordination",
        "windows",
        "kickoff",
        "openfoam",
        "su2",
        "wsl",
        "cfmesh",
        "smoke-test"
      ],
      "links": [
        {
          "to": "coordination-two-machine",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "coordination-windows-runtime-route",
          "rel": "relates-to"
        },
        {
          "to": "design-guided-solver-setup",
          "rel": "relates-to"
        },
        {
          "to": "plan-tip-handling",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "889a1a56d46d2a913d39a324a0914b9d641e484a2987269b28ffcf7b4a3c7cca"
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
      "id": "coordination-round-oct05",
      "path": "docs/coordination/round-oct05.md",
      "title": "Coordination plan - round of 2026-10-05 (tip study, fast ring, A3a UI, A3b, A3c polars, CAD)",
      "type": "plan",
      "status": "proposed",
      "owner": "@cfd-leader-4e90c621",
      "phase": "",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Schedules the operator-approved round of 2026-10-05: the Fable tip study (A, running), the fast ring with RNG and Ruling 81 (B), copy markers and small findings (C), the never-built A3a desktop UI (TGL, LAY, PNA, AUX as design section 18 cuts it), A3b section numerics (D1), SPIKE-ANA-1 then A3c polars (D2), the section and polar displays (DX) and CAD fixes plus the next CAD increment (E) - at most 3 coding tracks and 2 heavy test runs at once.",
      "tags": [
        "coordination",
        "worktrees",
        "parallelism",
        "analysis",
        "a3a",
        "a3b",
        "a3c",
        "test-ring",
        "cad"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "plan-test-cost",
          "rel": "relates-to"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        },
        {
          "to": "coordination-m12b-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8908f5a29bbf71b05ec706a8a262415689d3d84cd31c5f21e1453c7182c3f802"
    },
    {
      "id": "coordination-round-oct06",
      "path": "docs/coordination/round-oct06.md",
      "title": "Coordination plan - Mac round of 2026-10-06 (copy fixes, adaptive panels, DX, group move, 1280x800 layout, hook, spec 1.7.5)",
      "type": "plan",
      "status": "proposed",
      "owner": "@cfd-leader-14e5e8d5",
      "phase": "",
      "reviewBy": "2026-11-06",
      "reviewSuggested": [],
      "summary": "Schedules the Mac half of Rulings 101-108: a docs-first copy and spec commit (DOC) that removes DESIGN.md contention, the A3a copy and display fixes (CPY), adaptive panels (PNL), the DX section and polar build, group move (Core then Desktop), the 1280x800 four-view layout behind an operator pick, and the heredoc hook inline. At most 3 coding tracks and 2 heavy test runs at once; the PC's W-0..W-5 run on their own machine and reach main only as reviewed PRs.",
      "tags": [
        "coordination",
        "worktrees",
        "parallelism",
        "analysis",
        "a3b",
        "a3c",
        "dx",
        "group-move",
        "layout",
        "spec"
      ],
      "links": [
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "implements"
        },
        {
          "to": "coordination-two-machine",
          "rel": "depends-on"
        },
        {
          "to": "coordination-round-oct05",
          "rel": "relates-to"
        },
        {
          "to": "coordination-pc-kickoff",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "15b3ed7fc97c3cd5f524b776805c506bf6dffd023b29d963ab8b535617bdb7e8"
    },
    {
      "id": "coordination-two-machine",
      "path": "docs/coordination/two-machine.md",
      "title": "Two machines - the Mac session and the Windows PC session, and how they meet in git",
      "type": "plan",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-06",
      "reviewSuggested": [],
      "summary": "Ruling 106. The Mac session is the only leader and owns Mac-only and shared work; the Windows PC session owns Windows-only work (setup, smoke test, Windows Save/Open, WSL OpenFOAM and native SU2, NACA 0012 runs, L3, cfMesh). The PC pushes win/* branches and opens pull requests; the Fable owner on the Mac reviews them; the Mac leader merges through conductor-join. Messages travel in docs/coordination/xmsg.jsonl through tools/xmsg.py.",
      "tags": [
        "coordination",
        "windows",
        "macos",
        "git",
        "rendezvous",
        "pull-request"
      ],
      "links": [
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "coordination-windows-runtime-route",
          "rel": "relates-to"
        },
        {
          "to": "design-guided-solver-setup",
          "rel": "relates-to"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "relates-to"
        },
        {
          "to": "coordination-pc-kickoff",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "17f64e07ff79ade4bbe0a1ae2156fb4885b435fd6c202762b07cba7e672d4efa"
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
      "id": "plan-seam-repair-1",
      "path": "docs/plans/seam-repair-1.md",
      "title": "SEAM repair cycle 1 execution graph",
      "type": "plan",
      "status": "complete",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Bounded sequential repair graph for the eleven CFD review conditions, with red-first focused checks before a single full test ring and gate sequence.",
      "tags": [
        "analysis",
        "repair",
        "verification"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3bc-seam",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "SEAM repair cycle 1 graph",
          "mermaid": "flowchart LR\n  A --> B\n  B --> C\n  B --> D\n  B --> E\n  C --> F\n  D --> F\n  E --> F\n  F --> G --> H"
        }
      ],
      "sourceSha256": "6bf6faeb5edc5bcf6cbfb66014bf1eac12b194fa794531bd961c4ea868c683e7"
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
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c89007c3e991f6f152f1351f58df542875488e16b29c7ca435688379bca840ae"
    },
    {
      "id": "a3a-vlm3-red-first",
      "path": "docs/proof/a3a-vlm3/red-first-receipt.md",
      "title": "VLM-3 red-first receipt",
      "type": "proof-pack",
      "status": "verified",
      "owner": "@vlm3",
      "phase": "implementation",
      "reviewBy": "2027-04-04",
      "reviewSuggested": [],
      "summary": "The three VLM-3 fixtures fail on the old camber-surface horseshoes and front-bound sweep.",
      "tags": [
        "analysis",
        "vlm",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "08ff878ce4ea473d342dce59214849a1c29f0caa3deeaf78fb38e1e9f6a5eba4"
    },
    {
      "id": "a3a-vlm3c-camber-normals",
      "path": "docs/proof/a3a-vlm3/vlm3c-camber-normals.md",
      "title": "VLM-3c control-point camber normal proof",
      "type": "proof-pack",
      "status": "verified",
      "owner": "@vlm3c",
      "phase": "implementation",
      "reviewBy": "2027-04-04",
      "reviewSuggested": [],
      "summary": "F-21 fails on corner normals and passes on control-point camber slope normals; the existing spanwise and flat-wing fixtures remain green.",
      "tags": [
        "analysis",
        "vlm",
        "camber",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "tested-by"
        },
        {
          "to": "note-area3-fixture-arithmetic",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4bcb15a6a095cc5b44cdc797c742395ab92807dd4e1f8a08c36767a12fbe8f9d"
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
      "id": "proof-a3a-cor-red-first",
      "path": "docs/proof/a3a-cor/red-first.md",
      "title": "A3a COR red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-cor",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "The red runs of the COR track: both named tests failed on the skeleton, the bitwise test failed when Sections placed camber with its own formula, and the counter test failed when the Interlocked increments were restored to ++.",
      "tags": [
        "a3a",
        "cor",
        "placement",
        "sections",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8dec74f7a259e8cc2a66cd8927451e1bedb5512f8a665826dacafe0f01fb5886"
    },
    {
      "id": "proof-a3a-ctx-red-first",
      "path": "docs/proof/a3a-ctx/red-first.md",
      "title": "A3a CTX projection feed receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-ctx",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "The red runs of the A3a CTX track: every non-tip strip read \"Not judged - tip strip\", and the controller fed the projection no stations, verdicts or root t/c. Verdicts are derived on read; no schema changed.",
      "tags": [
        "a3a",
        "ctx",
        "analysis",
        "projection",
        "verdict",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7ff95489d408015b143594123d13d98b8a88ad0d9c01e35a4c1a457cb6edb422"
    },
    {
      "id": "proof-a3a-hist-red-first",
      "path": "docs/proof/a3a-hist/red-first.md",
      "title": "A3a HIST selected-run feed receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-hist",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "The projection feed follows the selected run's own revision. A geometry-Historical run keeps its verdicts, stations, root t/c and exact strip normals; an unheld revision reads Unavailable with a reason; a failed Evaluate keeps the previous Completed run's feed; a layer toggle raises LayersChanged. No schema or file-format change.",
      "tags": [
        "a3a",
        "hist",
        "analysis",
        "projection",
        "feed",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3a-ctx-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1d98cea1eca8e2d053636527a92f25027d84da2119500f19c380263af7a19c54"
    },
    {
      "id": "proof-a3a-lay-pack",
      "path": "docs/proof/a3a-lay/proof-pack.md",
      "title": "A3a LAY canvas layer proof pack",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-lay",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Evidence for the A3a Plan Γ, 3D load, and Side/Front depth layers on the existing desktop views. The pack ties selected-run projection samples to their visible glyphs, accessible names, table twins, and camera-step cost.",
      "tags": [
        "a3a",
        "lay",
        "analysis",
        "canvas",
        "proof"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-lay-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e78838c40e80ccace7aa1420150bb15371d8f1f9156ead430ddc353b173b867a"
    },
    {
      "id": "proof-a3a-lay-red-first",
      "path": "docs/proof/a3a-lay/red-first.md",
      "title": "A3a LAY red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-lay",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "The LAY Desktop ring was committed red before its three canvas-layer classes existed. A planted removal of both the dashed outside outline and its text count later made the named Plan test fail on the implemented code.",
      "tags": [
        "a3a",
        "lay",
        "analysis",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-lay-pack",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "664f24159907fb5fa3911ca328e7cdab3ba133a629eb613fdbd1f2c745622deb"
    },
    {
      "id": "proof-a3a-old-build",
      "path": "docs/proof/a3a-old-build/README.md",
      "title": "A3a old-build receipt: a cfdw-project-2 file opened by the build at the A3a base",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-sto",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "A cfdw-project-2 sample written by the STO writer, opened by the build at the A3a base (9709f72, before PRE): refused with DOC-UNSUPPORTED-FIELD, classified UnknownContent (COPY-130), and the file's SHA-256 is unchanged.",
      "tags": [
        "a3a",
        "sto",
        "analysis",
        "native-format",
        "rollback",
        "old-build",
        "copy-130"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a0cd33ccb663f518420dc28036021505bda9d589ad570ff1b322d30a8cdb83f1"
    },
    {
      "id": "proof-a3a-pack",
      "path": "docs/proof/a3a/proof-pack.md",
      "title": "A3a Proof Pack (wing VLM + strip, inviscid): tracks, tests, residuals and the operator demo",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-aux",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "What slice A3a built, the tests that prove it (per-track named totals, receipts, mutants), the two persona verdicts, the residuals, and the script for the operator's morning demo of the packaged macOS app.",
      "tags": [
        "a3a",
        "aux",
        "proof",
        "analysis",
        "demo"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "review-a3a-native",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3a-lay-pack",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e9fdeb86a45f377a81b4507f7d113cfeb8592e2578d60d31eddaa1939c20be08"
    },
    {
      "id": "proof-a3a-pna-red-first",
      "path": "docs/proof/a3a-pna/red-first.md",
      "title": "A3a PNA panes and bottom panel receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-pna",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "The red runs of the A3a PNA track: the Layers pane, the Analysis bottom panel with its chart twin, the Properties Analysis groups and the shell slots. Each owned test with its red commit and the planted mutant that turns it red.",
      "tags": [
        "a3a",
        "pna",
        "analysis",
        "layers",
        "bottom-panel",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-ctx-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e0d0949e985922614b3e21e32bdb40617adb89e4eac18ecf7eb77ca4ed86cdb9"
    },
    {
      "id": "proof-a3a-pre-red-first",
      "path": "docs/proof/a3a-pre/red-first.md",
      "title": "A3a PRE red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-pre",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "The red runs of the A3a PRE track: the architecture check turned red by a planted ProfileAt call and by a planted edit verb, the PlacedSection.Assignment check red before the station-index fix, and the checker self-test case red on a mutant that drops the new section flags.",
      "tags": [
        "a3a",
        "pre",
        "analysis",
        "architecture",
        "placement",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "25d7106d6ce00df21120a2b3d91593da54888f3d6c2c02b53d882807abc59c1b"
    },
    {
      "id": "proof-a3a-prj2-cfd-veto",
      "path": "docs/proof/a3a-prj2/proof-pack.md",
      "title": "PRJ-2 CFD label veto proof",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-prj2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Red-first and planted-mutant evidence for the PRJ-2 CFD veto. Checks cover the displayed CL/CD claim, provisional tip verdicts, Trefftz e, original strip widths, moment-arc sign, and proposed copy scope.",
      "tags": [
        "a3a",
        "analysis",
        "prj",
        "cfd",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "design-language",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e8569fc5d21241366dd4a31c2ef9323bda6b060678f10a8cb9ebb1fb59fde3bc"
    },
    {
      "id": "proof-a3a-sto-red-first",
      "path": "docs/proof/a3a-sto/red-first.md",
      "title": "A3a STO red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-sto",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Each STO check of design §18.8 observed red against its named mutant (15 red lines over 14 checks), including the planted mutant of the STO exit: the content hash not checked on read turns both tamper checks red.",
      "tags": [
        "a3a",
        "sto",
        "analysis",
        "persistence",
        "run-key",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "implements"
        },
        {
          "to": "proof-a3a-old-build",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "48cf4d7c57f8942050f8c3c7fc11455084c644c03a26eeecfcfd2cb7feab6b9d"
    },
    {
      "id": "proof-a3a-stp-red-first",
      "path": "docs/proof/a3a-stp/red-first.md",
      "title": "A3a STP red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-stp",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "The red run of the A3a STP track. Each owned check was observed red against its named mutant, then the mutant was removed before the commit. The water-table second check is signed in the water-table proof.",
      "tags": [
        "a3a",
        "stp",
        "analysis",
        "red-first",
        "fixtures",
        "water"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "note-area3-fixture-arithmetic",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3a-water-table",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5447f97817bdc16d3331d7ca08c5bb5d3ce3eb926d14ca000675e0e962093d25"
    },
    {
      "id": "proof-a3a-svc-red-first",
      "path": "docs/proof/a3a-svc/red-first.md",
      "title": "A3a SVC red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-svc",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Each of the 16 SVC checks of design §18.8 observed red against its named mutant (23 red lines), including the SVC exit mutant: the current key built from the run's own stored inputs turns all eight freshness checks red.",
      "tags": [
        "a3a",
        "svc",
        "analysis",
        "freshness",
        "cli",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3a-sto-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "80d9a61a46d8e5a6f7b4607b3756e35be1e5e9b591abe9344d8b2bb85198d4a2"
    },
    {
      "id": "proof-a3a-svc2-red-first",
      "path": "docs/proof/a3a-svc2/red-first.md",
      "title": "A3a SVC-2 red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-svc2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "The SVC-2 fixes for the adversarial C# review of the A3a analysis service: 10 new checks and 3 strengthened ones, each red under its own planted mutant (16 mutants, all red), plus 5 red lines observed against the unfixed code.",
      "tags": [
        "a3a",
        "svc",
        "svc-2",
        "analysis",
        "review-fixes",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-svc-red-first",
          "rel": "relates-to"
        },
        {
          "to": "adr-0011-analysis-run-storage",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3f58feb2f4786e41aa837920cca6671d06f2d6fa11dc076951feac0396ed1443"
    },
    {
      "id": "proof-a3a-tgl-red-first",
      "path": "docs/proof/a3a-tgl/red-first.md",
      "title": "A3a TGL red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-a3a-tgl",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Records the failing TGL named checks before each implementation step and the mutant each check must catch.",
      "tags": [
        "analysis",
        "desktop",
        "tdd"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6308a0647aa352a85761dde8d3afeac98bfea0cfba83b669965ef5443aa1d720"
    },
    {
      "id": "proof-a3a-vlm-red-first",
      "path": "docs/proof/a3a-vlm/red-first.md",
      "title": "A3a VLM red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-vlm",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "The red run of the A3a VLM track. Planting the O(1) wake mutant (wake length per panel instead of per wing) turned F-6 red. The mutant was removed before the commit. The 2026-10-04 repair adds the red lines for the LU pivot defect (one-solve residual 1.98 on a 12 x 12 matrix), the convergence form of F-5 and the pointwise form of F-15.",
      "tags": [
        "a3a",
        "vlm",
        "analysis",
        "red-first",
        "fixtures"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "note-area3-fixture-arithmetic",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "90a9c5441215a9dca5bd7ea998ecd2b899e1f1841d413543338878f1a22f49a9"
    },
    {
      "id": "proof-a3a-water-table",
      "path": "docs/proof/a3a-water-table/transcription.md",
      "title": "ITTC water table transcription and second check",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-stp",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Integer fresh-water and standard-seawater rows from ITTC 7.5-02-01-03 Rev 03, hashed at load. The second check matched Table 1 and Table 3 against the 0.1 °C appendix. The 0 °C row is a one-step extension.",
      "tags": [
        "a3a",
        "stp",
        "water",
        "ittc"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3a-stp-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3200a4d15b168872a8d39187379c0b65da541222ed1cd1f2c96aa2dd3a4127e4"
    },
    {
      "id": "proof-a3b-red-first",
      "path": "docs/proof/a3b/red-first.md",
      "title": "A3b section numerics red-first and proof pack",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-d1",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "A3b red-first receipt and numerical proof for the linear-vorticity section panel, the panel-based section estimator and cavitation screen. Includes the corrected conformal-map oracle, measured interior convergence and Cp_min error, red-first repair receipts, test-ring costs, and integration limits.",
      "tags": [
        "analysis",
        "section",
        "panel-method",
        "cavitation",
        "proof"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
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
      "sourceSha256": "5cf2c6fcd22d1025fa39c668d46ba98e9c9e66e0ac77ebe9f399825dfac7514f"
    },
    {
      "id": "proof-a3bc-seam",
      "path": "docs/proof/a3bc-seam/proof-pack.md",
      "title": "A3b/A3c service and projection seam proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Connects the section and polar tiers to run and projection, adds A3c-2 numerical values, and preserves Ruling 100's persisted PolarSample grain by deriving validity metadata on read.",
      "tags": [
        "analysis",
        "section",
        "polar",
        "projection",
        "numerics",
        "ruling-100"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "implements"
        },
        {
          "to": "design-dx-screen-states",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3c-polar-source",
          "rel": "depends-on"
        },
        {
          "to": "proof-a3bc-seam-red-first",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3bc-seam-state-coverage",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0a4922b498583f601e49dd56091c82ea98d7a57ca4bb2dc226f9151d2c41f509"
    },
    {
      "id": "proof-a3bc-seam-red-first",
      "path": "docs/proof/a3bc-seam/red-first.md",
      "title": "Track SEAM red-first ledger",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Red commits, observed failure modes and green implementation commits for the A3b/A3c seam.",
      "tags": [
        "analysis",
        "tests",
        "red-first"
      ],
      "links": [
        {
          "to": "proof-a3bc-seam",
          "rel": "relates-to"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "807f39101543c9eea2668d132390d5f21ede8179d8cc7c3cdeac27b46bbc753e"
    },
    {
      "id": "proof-a3bc-seam-state-coverage",
      "path": "docs/proof/a3bc-seam/state-coverage.md",
      "title": "A3b/A3c projection data against DX's 54 states",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Checks each of the 54 DX screen states against the non-persisted data made available by Track SEAM. Visual rendering and the 42 proposed strings remain with DX and the operator.",
      "tags": [
        "analysis",
        "section",
        "polar",
        "projection",
        "screen-states"
      ],
      "links": [
        {
          "to": "design-dx-screen-states",
          "rel": "relates-to"
        },
        {
          "to": "proof-a3bc-seam",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "457fbb636278d62cf3ab67e8dfe7a677e703a9231035d8d25f75c5760ad54759"
    },
    {
      "id": "proof-a3c-polar-source",
      "path": "docs/proof/a3c-polar-source/proof-pack.md",
      "title": "A3c-1 production NeuralFoil polar source proof",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Ports the xxxlarge NeuralFoil 0.3.2 model into an integrity-checked in-process polar source, with CST fit, envelope refusals, measured fidelity, pinned wheel conversion and third-party notice gates. The source is not yet wired into the analysis service or UI.",
      "tags": [
        "analysis",
        "neuralfoil",
        "polar",
        "cst",
        "weights",
        "licence",
        "ruling-85"
      ],
      "links": [
        {
          "to": "proof-spike-ana-1",
          "rel": "depends-on"
        },
        {
          "to": "design-area3-analysis",
          "rel": "implements"
        },
        {
          "to": "coordination-round-oct05",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a03725be264cb49de2583171d5a79688e00118df31e1cd8ef203b8f70d4e3f19"
    },
    {
      "id": "proof-a3c-polar-source-red-first",
      "path": "docs/proof/a3c-polar-source/red-first.md",
      "title": "A3c-1 NeuralFoil red-first evidence",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Records the observed red and green commits for the NeuralFoil fixture, integrity, envelope, confidence, telemetry and non-computable-result checks, including the limits of the first compile-red observation.",
      "tags": [
        "analysis",
        "neuralfoil",
        "polar",
        "red-first"
      ],
      "links": [
        {
          "to": "proof-a3c-polar-source",
          "rel": "relates-to"
        },
        {
          "to": "proof-spike-ana-1",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b0673fdb6f2871565c8fc0f5b1be74a6148438f314dab6e8a27e7c4ac00effe4"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "b0e48b1c582943f3a76882377969950043a57b6bfaac6c783948b4b11e76df0d"
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
      "id": "proof-blend-certificate-budget",
      "path": "docs/proof/blend-certificate-budget/verdict.md",
      "title": "Blend certificate budget spike verdict (Ruling 73, DR-XPA-1 c, DR-M12D-1 spike E)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Measured against the as-built Core. The node budget can scale with spans without weakening soundness: it never enters the enclosure (52 fixtures x 23 queries are bit-identical under five rules), and the real need is 17-21 nodes at any span count. Scaling it alone gains one span (two sections: 6; three: 5). The limit is the all-query operation bound (1e6), which charges 6 N (N + 1) for the budget N rather than for the nodes a query uses. With that bound lifted, 5-16 spans certify for two and three sections at 120 mm and 2 m within 3.9 % of the 1e9 work limit and under 1 s. New finding: four differing station sections are refused as built at any span count.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "all-query-bound",
        "spans",
        "ruling-73",
        "dr-xpa-1",
        "dr-m12d-1",
        "spike"
      ],
      "links": [
        {
          "to": "design-cross-profile-abscissa",
          "rel": "relates-to"
        },
        {
          "to": "design-m12d-catalog",
          "rel": "relates-to"
        },
        {
          "to": "proof-cross-profile-abscissa",
          "rel": "depends-on"
        },
        {
          "to": "proof-m12c-certificate-spike",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7d0e374e8cb164e081ac1758b8c45c84dd7de91ed74c73d9a92bcc7fa4c96f7d"
    },
    {
      "id": "proof-blend-certificate-budget-four-as-built",
      "path": "docs/proof/blend-certificate-budget/output/four-as-built.md",
      "title": "Blend budget probe - four, as-built Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4e9b1232638b8c769b308b2a84d466690431c401803ab5daf85f4c5a12c055bf"
    },
    {
      "id": "proof-blend-certificate-budget-four-hooked",
      "path": "docs/proof/blend-certificate-budget/output/four-hooked.md",
      "title": "Blend budget probe - four, hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7b5cb4df7cd8c8802db18a8385634dfe39a9833db070ac35952f2e512d2680df"
    },
    {
      "id": "proof-blend-certificate-budget-neutrality-four",
      "path": "docs/proof/blend-certificate-budget/output/neutrality-four.md",
      "title": "Blend budget probe - neutrality check, four (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/neutrality.py: the hooked Core copy at its defaults equals the as-built Core, row by row.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c9e8d2244cb9f6a95134d8dc578d8a314471f2705250f3abac8602420a416e79"
    },
    {
      "id": "proof-blend-certificate-budget-neutrality-stage1",
      "path": "docs/proof/blend-certificate-budget/output/neutrality-stage1.md",
      "title": "Blend budget probe - neutrality check, stage1 (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/neutrality.py: the hooked Core copy at its defaults equals the as-built Core, row by row.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "32f57280c3089590581ab7d7163b8994eb3425e7d3d707c22c887380795dff54"
    },
    {
      "id": "proof-blend-certificate-budget-neutrality-stage2",
      "path": "docs/proof/blend-certificate-budget/output/neutrality-stage2.md",
      "title": "Blend budget probe - neutrality check, stage2 (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/neutrality.py: the hooked Core copy at its defaults equals the as-built Core, row by row.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f2e215b0632e4d73683269a904b4c4cc8bab4462469e0b2879eb269e1009a872"
    },
    {
      "id": "proof-blend-certificate-budget-stage1-as-built",
      "path": "docs/proof/blend-certificate-budget/output/stage1-as-built.md",
      "title": "Blend budget probe - stage1, as-built Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f205e8b40b61699df8bd0ac3e3d34150e2b799eb469579933d22901f32088bd1"
    },
    {
      "id": "proof-blend-certificate-budget-stage1-hooked",
      "path": "docs/proof/blend-certificate-budget/output/stage1-hooked.md",
      "title": "Blend budget probe - stage1, hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c14c8d4bbac4fd78bb4e0db44199599592c7b6481c18f5dd904cbc12a449d159"
    },
    {
      "id": "proof-blend-certificate-budget-stage2-as-built",
      "path": "docs/proof/blend-certificate-budget/output/stage2-as-built.md",
      "title": "Blend budget probe - stage2, as-built Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "41c84cbf71bc7847f3b0a8d9dd1e70980f7c7026b3348ec29e15a48379f3f16d"
    },
    {
      "id": "proof-blend-certificate-budget-stage2-hooked",
      "path": "docs/proof/blend-certificate-budget/output/stage2-hooked.md",
      "title": "Blend budget probe - stage2, hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-budget/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "42638998a24dd8bfedcabc997280a72fa8d3b5ed26e699efab44861cfac233b0"
    },
    {
      "id": "proof-blend-certificate-budget-summary",
      "path": "docs/proof/blend-certificate-budget/output/summary.md",
      "title": "Blend budget probe - summary (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/summarize.py from the hooked runs: maximum spans per rule, op-count decomposition, hash equality.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1384abe8803a0ead471ee0562a479766e8cfd6724623c8396588b4a8e4561fa9"
    },
    {
      "id": "proof-blend-certificate-budget-timing",
      "path": "docs/proof/blend-certificate-budget/output/timing.md",
      "title": "Blend budget probe - re-timed heaviest fixtures (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by the probe's timing mode: minimum and median wall time of the heaviest fixtures under machine load.",
      "tags": [
        "certificate",
        "blend",
        "budget",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2fb09414e43cbf319a03ae0c7605f59a53cc78797d1b73578f5ced83dbf71d56"
    },
    {
      "id": "proof-blend-certificate-heap",
      "path": "docs/proof/blend-certificate-heap/verdict.md",
      "title": "Blend certificate heap spike verdict (Ruling 73, DR-XPA-1 c)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Measured on a branch build. A binary max-heap in Bernstein.Maximum, keyed on (coefficient maximum, insertion sequence), reproduces the rescan bit for bit: 52 of 52 fixtures, 1,196 queries, admission and query work equal. With its comparisons charged instead of 6 N (N + 1), the 1e6 all-query bound admits the 27-span (32-point) ceiling for two to four differing stations, 20 spans for five and 5 spans for six. GO for heap + max(256, 48 s) with the bound kept at 1e6.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "budget",
        "all-query-bound",
        "spans",
        "ruling-73",
        "dr-xpa-1",
        "spike"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-budget",
          "rel": "depends-on"
        },
        {
          "to": "design-cross-profile-abscissa",
          "rel": "relates-to"
        },
        {
          "to": "design-m12d-catalog",
          "rel": "relates-to"
        },
        {
          "to": "proof-m12b2-golden",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "da8fa0a653567a2223906b70c4ec83df3dd80b985c437243caf8aeec8fee7414"
    },
    {
      "id": "proof-blend-certificate-heap-ceiling-heap",
      "path": "docs/proof/blend-certificate-heap/output/ceiling-heap.md",
      "title": "Blend heap probe - ceiling, heap Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "dad23eb0ecb4dd4579b99028f33f951642788c553858bb58fa6203b40f9f4c82"
    },
    {
      "id": "proof-blend-certificate-heap-ceiling-heap-hooked",
      "path": "docs/proof/blend-certificate-heap/output/ceiling-heap-hooked.md",
      "title": "Blend heap probe - ceiling, heap-hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a2b4bfd0b0973f8bf70d251db25089a15846247f36ee91da1a8b98160da1ec9c"
    },
    {
      "id": "proof-blend-certificate-heap-four-heap",
      "path": "docs/proof/blend-certificate-heap/output/four-heap.md",
      "title": "Blend heap probe - four, heap Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8b582ee0c39344c4566dced48408f95d81ecfb0fe8bc01e0f780011eb603d778"
    },
    {
      "id": "proof-blend-certificate-heap-four-heap-hooked",
      "path": "docs/proof/blend-certificate-heap/output/four-heap-hooked.md",
      "title": "Blend heap probe - four, heap-hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c9554dac47c7d15c86fc7324e9cbafe7b7858b7d1cd0d6f3d444eed7a39e19fd"
    },
    {
      "id": "proof-blend-certificate-heap-neutrality",
      "path": "docs/proof/blend-certificate-heap/output/neutrality.md",
      "title": "Blend heap probe - neutrality check (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/neutrality.py: the heap build reproduces the rescan build's admission and query results row by row.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fdfb79766e19c2dc2ac73cfea24c427538a6095640181a5222eef1d2a65b1f9c"
    },
    {
      "id": "proof-blend-certificate-heap-stage1-heap",
      "path": "docs/proof/blend-certificate-heap/output/stage1-heap.md",
      "title": "Blend heap probe - stage1, heap Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c131478ded31b3c2c72882de4a8b070d53ce9c392c0ffc61fe521a861655e028"
    },
    {
      "id": "proof-blend-certificate-heap-stage1-heap-hooked",
      "path": "docs/proof/blend-certificate-heap/output/stage1-heap-hooked.md",
      "title": "Blend heap probe - stage1, heap-hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4a19e009a8b620c6e50424e5ef2602358b1d81c41f846486624e9e103fe82cd5"
    },
    {
      "id": "proof-blend-certificate-heap-stage2-heap",
      "path": "docs/proof/blend-certificate-heap/output/stage2-heap.md",
      "title": "Blend heap probe - stage2, heap Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f1abcc9ed79d6c5ab02ae615ff488aa54bfe5965b6a010072eb7e431c494ffcd"
    },
    {
      "id": "proof-blend-certificate-heap-stage2-heap-hooked",
      "path": "docs/proof/blend-certificate-heap/output/stage2-heap-hooked.md",
      "title": "Blend heap probe - stage2, heap-hooked Core (generated; do not edit, re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/blend-certificate-heap/probe. Times vary by machine; every other column is deterministic.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "220ad8a7f5c0de591345387bcd923ec5f720361575d3a76aa23278727f6da6d2"
    },
    {
      "id": "proof-blend-certificate-heap-summary",
      "path": "docs/proof/blend-certificate-heap/output/summary.md",
      "title": "Blend heap probe - summary (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by probe/summarize.py: capacity per family (as built, 4.2 M alternative, heap), op counts, measured costs.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "90698f069ee1b77d12d0fe0113fbed274dc5baf549e21985e0194cfcb66bd16f"
    },
    {
      "id": "proof-blend-certificate-heap-timing",
      "path": "docs/proof/blend-certificate-heap/output/timing.md",
      "title": "Blend heap probe - re-timed heaviest fixtures (generated)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by the probe's timing mode: minimum and median wall time of the heaviest fixtures under machine load.",
      "tags": [
        "certificate",
        "blend",
        "heap",
        "probe",
        "generated",
        "ruling-73"
      ],
      "links": [
        {
          "to": "proof-blend-certificate-heap",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c3c2e20ddb41ade9bb6869725dbf84d648ca1916d1b08e9ff62af890fc7f6f04"
    },
    {
      "id": "proof-c-copy-findings-red-first",
      "path": "docs/proof/c-copy-findings/red-first.md",
      "title": "Track C red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-c",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Track C of round-oct05: Ruling 82 copy markers, the security probe lock path, the STORE-SUBSET static check, four analysis-service nits and the F-4 helper, each with its red run and its green run.",
      "tags": [
        "round-oct05",
        "copy",
        "ruling-82",
        "security-probe",
        "store-subset",
        "analysis",
        "cli"
      ],
      "links": [
        {
          "to": "proof-a3a-svc2-red-first",
          "rel": "relates-to"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "908c26e00577c4e7878486e68e94cd3da98dcb4e8551c757e89463c07431bc79"
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
      "id": "proof-cpy-red-first",
      "path": "docs/proof/cpy/red-first.md",
      "title": "Track CPY, round-oct06 — red-first receipts and captures",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@trk-cpy",
      "phase": "implementation",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "For each Ruling 101 and 109 item, the check that failed on the old code and the run that passed after the change, plus the capture list at 1500x870 and 1280x800.",
      "tags": [
        "proof",
        "copy",
        "layout",
        "analysis",
        "ruling-101",
        "ruling-109"
      ],
      "links": [
        {
          "to": "proof-doc-oct06",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "359ef932076656929ed94883a87e19a8f3c1b7017f90075ca322673b5e509049"
    },
    {
      "id": "proof-cross-profile-abscissa",
      "path": "docs/proof/cross-profile-abscissa/README.md",
      "title": "XPA probe — compatible fit and knot propagation across station profiles (Ruling 71 option 1)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Measured with the as-built Core. Compatible fit (each section keeps its own points; a derived copy is fitted onto one shared spacing): a 6-point Tip fits a 10-point Root's spacing to 1.5e-16 chord (the app's sqrt spacings nest), an 8-point Example section fits a 10-point NACA spacing to 3.9e-5 chord on the record (5.7 um on the placed surface at 127 mm); the reverse direction (10 onto 6) is 2.3e-4 chord (28.7 um at 127 mm, over); a Tip anchor missing from the shared spacing costs 1.2e-3 chord. Fit and measure take about 100-130 ms per profile, assess 25-70 ms. Knot propagation: insertion moves the partner 6.0e-17 chord with bitwise-equal x. Both options hit the certificate's blend capacity: at most 5 Bezier spans as built, so any anchor on a 10-point section needs the budget raised; with it raised, 6 spans certify.",
      "tags": [
        "section",
        "profile",
        "abscissa",
        "compatible-fit",
        "knot-insertion",
        "knot-removal",
        "certificate",
        "blend",
        "ruling-71",
        "xpa",
        "probe"
      ],
      "links": [
        {
          "to": "design-cross-profile-abscissa",
          "rel": "relates-to"
        },
        {
          "to": "proof-m12c-certificate-spike",
          "rel": "relates-to"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e8919f5dc318ed44cf1cf7722773d783f6c083b6cdcc63fc9d84cd9b33cd5df5"
    },
    {
      "id": "proof-cross-profile-abscissa-table",
      "path": "docs/proof/cross-profile-abscissa/output/table.md",
      "title": "XPA probe — generated table (as-built Core) (do not edit; re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/cross-profile-abscissa/probe. Timings vary by machine; every other row is deterministic.",
      "tags": [
        "xpa",
        "abscissa",
        "probe",
        "generated"
      ],
      "links": [
        {
          "to": "proof-cross-profile-abscissa",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bad451eda12edffe2c5adaab7f2fc1fa31e11251fbc966269d4ae864775b0ed8"
    },
    {
      "id": "proof-cross-profile-abscissa-table-budget-variant",
      "path": "docs/proof/cross-profile-abscissa/output/table-budget-variant.md",
      "title": "XPA probe — generated table (budget-variant) (do not edit; re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/cross-profile-abscissa/probe. Timings vary by machine; every other row is deterministic.",
      "tags": [
        "xpa",
        "abscissa",
        "probe",
        "generated"
      ],
      "links": [
        {
          "to": "proof-cross-profile-abscissa",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "44d233467669276d6f9ad487b7a69e6f51761c89cbe0700f22d175544b929c9f"
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
      "id": "proof-doc-oct06",
      "path": "docs/proof/doc-oct06/README.md",
      "title": "Track DOC, round-oct06 — new copy rows and spec 1.7.5",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "implementation",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Rows COPY-250 to COPY-356 added to DESIGN.md section 7 for Rulings 101, 107 and 108, the reason-code drafts, and spec 1.7.5.",
      "tags": [
        "proof",
        "copy",
        "spec",
        "rulings"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "49d004c4567a9a1dbba0bc946b28f474727e5860ab28e3474b54bb2db53c1719"
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
      "id": "proof-dx-red-first",
      "path": "docs/proof/dx/red-first.md",
      "title": "DX step 2: red-first record, exit evidence and gaps",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-dx",
      "phase": "implementation",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "What DX built, how each check was shown red, the observed run results and the known gaps (cost ring, unapproved validation copy, example foil family, two-sided defaults).",
      "tags": [
        "analysis",
        "a3b",
        "a3c",
        "dx",
        "proof",
        "red-first"
      ],
      "links": [
        {
          "to": "proof-dx-test-plan",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ae71a530a9c3c09f37186f843908150b339ff08cb429ad17e0ce47eb413dd483"
    },
    {
      "id": "proof-dx-test-plan",
      "path": "docs/proof/dx/test-plan.md",
      "title": "DX test plan and state coverage (round-oct06 step 1)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-dx",
      "phase": "implementation",
      "reviewBy": "2026-12-31",
      "reviewSuggested": [],
      "summary": "Which of the 54 A3b and A3c states the approved mockup draws, the states to show the operator first, and 39 named checks (35 fast, 4 readiness) covering every state, DXM-2..9 and Charts_TransitionAndBucket_Overlays.",
      "tags": [
        "analysis",
        "a3b",
        "a3c",
        "test-plan",
        "dx"
      ],
      "links": [
        {
          "to": "design-dx-screen-states",
          "rel": "depends-on"
        },
        {
          "to": "mockup-dx-section-polar-states",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cc1943ae0d8e69f9d96c347e55421297d97fbe39896be48f2542495f7f2701c6"
    },
    {
      "id": "proof-e2-cad-defects-red-first",
      "path": "docs/proof/e2-cad-defects/red-first.md",
      "title": "E2 CAD defects red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-e2",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Two CAD defects, each red before its fix. A refused section step republished Finish \"Checking…\" for the re-check of unchanged bytes. MakeIndependent threw DSL-PATCH when a tangent row still named a point id that the copy had rewritten. The retarget now edits each row's id token span. A tangents block before ids stays DSL-SYNTAX.",
      "tags": [
        "cad",
        "flk-1",
        "make-independent",
        "tangent",
        "red-first"
      ],
      "links": [
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9cbbe5c0a750558d520e47c982e1b944d873a49d45152427f3e41ccfeada4a93"
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
      "id": "proof-grp-red-first",
      "path": "docs/proof/grp/red-first.md",
      "title": "GRP half 1 red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-grp",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Red on the old Core, two planted mutants red, and the design section 9 Core names green.",
      "tags": [
        "grp",
        "group-move",
        "red-first",
        "mutants"
      ],
      "links": [
        {
          "to": "design-group-move-node-m",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d2528f7faa8bc98faab5bb1c2ece5b347d1fe8cd163e08eb15e90a4a08ed78c9"
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
      "reviewSuggested": [
        {
          "by": "adr-0001-master-curve-degree",
          "on": "2026-10-03",
          "reason": "Amendment 2 (Ruling 62): channels hold 4-16 control vertices under FoilDSL 4.1 (6-10 under 4.0); the verbs Add point, Remove point and Rebuild to N are designed in docs/design/planform-point-verbs.md; foildsl.md 5 item 3, A4.1/A4.2/GEO-05 floor text need amendment (F-4)."
        }
      ],
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
      "sourceSha256": "f339969ed2aa7416835fd5bf4daecd750860b024c80b687a8cdf736e3b62b4b9"
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
      "sourceSha256": "744d50e63498b484cb0878f8035cfdbda89af6d9dedfd5a979d713da225cd5e7"
    },
    {
      "id": "proof-m12c-certificate-spike",
      "path": "docs/proof/m12c-certificate-spike/verdict.md",
      "title": "GSPK — per-surface certificate spike verdict (M1.2c)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-gspk",
      "phase": "implementation",
      "reviewBy": "2026-10-31",
      "reviewSuggested": [],
      "summary": "NO-GO, measured at stage 1. The x-overlay certificate admits per-surface bases on one profile (F1 at 2 m: 17.6 M bit-work, 0.06 s, 7 atoms). It cannot enclose the blend maximum max T0 between profiles whose abscissae differ (F4 at 2 m) within the 1e9 bit-work, 4,096-atom and 1 s budgets. Per OD-4 a, SPT builds the paired list (SPTF), and DR-11 goes back to the operator with these numbers.",
      "tags": [
        "m1.2c",
        "certificate",
        "b6",
        "per-surface",
        "gspk",
        "spike"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "depends-on"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-one-placement-rule",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b8f3d99ea805c57a7af6b8a07a64cc12c617cd17c1a881bcb61254e0379fe371"
    },
    {
      "id": "proof-m12c-old-build",
      "path": "docs/proof/m12c-old-build/README.md",
      "title": "M1.2c old-build characterization of section files",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "implementation",
      "reviewBy": "2026-12-30",
      "reviewSuggested": [],
      "summary": "What the 4b9bc35 CLI and app controller do with M1.2c section files (cases a–e of design §3.3): every file is refused or read-only, no edit or Save succeeds, and every file's bytes are unchanged.",
      "tags": [
        "m12c",
        "sdr",
        "foildsl",
        "characterization",
        "section",
        "downgrade"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "adr-0007-edit-transactions",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-point-types",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7d0bcfae3ee831072f629ec8d9f3a4cacf1971b608aa58287b13606ab9ff36e6"
    },
    {
      "id": "proof-m12c-pnl-red-first",
      "path": "docs/proof/m12c-pnl/red-first-receipt.md",
      "title": "M1.2c PNL red-first receipt (product mutants)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@track-pnl",
      "phase": "implementation",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [],
      "summary": "PNL wrote code before tests for ten named checks. Each was then turned red by a planted product mutant and back to PASS on revert; none is a tautology. Per check: mutant file:line, the FAIL line, revert confirmed.",
      "tags": [
        "m12c",
        "pnl",
        "red-first",
        "mutants",
        "tests"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "64ea1243991e70530d48470a9937235fd7c554a799409a97219a27bcd4e351fc"
    },
    {
      "id": "proof-m12d-catalog-probe",
      "path": "docs/proof/m12d-catalog/output/table.md",
      "title": "M1.2d Replace probe — fits, certificates and the shared point-spacing rule",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design — M1.2d",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/m12d-catalog/probe (dotnet run -c Release -- <proof dir> <repo>) through the as-built Core. Rows CF and B3 are the design's evidence; the earlier NACA rows through DatImport.Parse are the as-built DAT path.",
      "tags": [
        "m1.2d",
        "catalog",
        "replace",
        "abscissa",
        "probe"
      ],
      "links": [
        {
          "to": "design-m12d-catalog",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e1bb249783feea665a0463b4843647acf8cfbb12ebc4c3f2a2240bd3a9093023"
    },
    {
      "id": "proof-m12d-dlg2",
      "path": "docs/proof/m12d-dlg2/proof-pack.md",
      "title": "DLG-2 granted seams and dialog proof",
      "type": "proof-pack",
      "status": "active",
      "owner": "@track-dlg2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "DLG-2 proof for refused candidate bytes, station provenance, catalog failure cause, damaged library rows, and proposed save copy. Records the rendered tests, one full ring attempt, the isolated repairs, and the remaining gate failures.",
      "tags": [
        "m12d",
        "dlg",
        "catalog",
        "provenance",
        "library",
        "red-first"
      ],
      "links": [
        {
          "to": "design-m12d-catalog",
          "rel": "depends-on"
        },
        {
          "to": "mockup-m12d-catalog",
          "rel": "depends-on"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1ea31f885299d0cb449f3d45f253820b41e5fe40d9336490f71c8bcf94554bab"
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
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
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
      "sourceSha256": "ea91f4960495f21b428612c898dd80b200b72d581580eb40aa349c099972fb6e"
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
      "id": "proof-planform-verbs-fairness",
      "path": "docs/proof/planform-verbs-fairness/README.md",
      "title": "SPK — fairness and Rebuild evidence for 4- and 5-vertex channels (planform point verbs)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-spk",
      "phase": "implementation",
      "reviewBy": "2026-10-31",
      "reviewSuggested": [],
      "summary": "Measured with the as-built Core (SplineBasis, ChannelEvaluator, ConstrainedFit, FoilSource's own New foil construction, WingEstimates' area integral): at 4 and 5 vertices the five Example curves are fairer (lower κ′ energy) and have no curvature breaks, but no longer pass through their six anchors (LE 0.84 mm, chord 1.0 mm at 4). Support is global. The 4-point New foil (Ruling 64) moves the rails by LE 3.05 mm and TE 8.83 mm at 467.5 mm and turns the TE tip 35.0°. Its κ′ energy is about 10⁻⁷ of today's 10-point rails. Each 4-point rail has one comb sign change 24 mm from the root. The other three channels are constants, exact at any count.",
      "tags": [
        "planform-verbs",
        "fairness",
        "rebuild",
        "adr-0001",
        "amendment-2",
        "ruling-62",
        "ruling-64",
        "spk",
        "spike"
      ],
      "links": [
        {
          "to": "design-planform-point-verbs",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a567df6e890a28be5ba57eb8d6dbf31525aadcd71bf315ccccc424c7471da129"
    },
    {
      "id": "proof-planform-verbs-fairness-table",
      "path": "docs/proof/planform-verbs-fairness/output/table.md",
      "title": "SPK — generated fairness and Rebuild table (do not edit; re-run the probe)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-spk",
      "phase": "implementation",
      "reviewBy": "2026-10-31",
      "reviewSuggested": [],
      "summary": "Generated by docs/proof/planform-verbs-fairness/probe from fixture.json; the full SPK table.",
      "tags": [
        "planform-verbs",
        "fairness",
        "rebuild",
        "spk",
        "generated"
      ],
      "links": [
        {
          "to": "proof-planform-verbs-fairness",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2bfeb5dc35cc7ad5317645dbe18828695d0bcf11358ce15613ccfd345745f6b0"
    },
    {
      "id": "proof-planform-verbs-old-build",
      "path": "docs/proof/planform-verbs-old-build/receipt.md",
      "title": "PVC old-build characterization and planted-mutant receipt",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@track-pvc",
      "phase": "implementation",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Pre-PVC commit 0cf4e4feae65e844fb6eac0c3825100b6d1c56e5 refuses a 4-point 4.1 source with DSL-CURVE and refuses point-add, point-remove and curve-rebuild envelopes with DOC-REFERENCE. Each file's SHA-256 is unchanged. An alpha denominator one span high turns AddPoint_Boehm_ShapeUnchangedWithin1e12Relative red.",
      "tags": [
        "planform",
        "point-verbs",
        "foildsl",
        "characterization",
        "pvc"
      ],
      "links": [
        {
          "to": "design-planform-point-verbs",
          "rel": "depends-on"
        },
        {
          "to": "spec-foildsl",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8b85b88e777fe865de4d9857177e1d883e951180512270c36cff9ea1e6d00490"
    },
    {
      "id": "proof-pnl-red-first",
      "path": "docs/proof/pnl/red-first.md",
      "title": "PNL red-first ledger (adaptive panels)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Red then green receipts for Ruling 103: the governing station's screen and Cp_min from the 400-panel solve, and the method version change that turns older runs Historical. Test 3 is not done (waits on the step-0 ruling).",
      "tags": [
        "analysis",
        "section",
        "panel-method",
        "red-first"
      ],
      "links": [
        {
          "to": "proof-pnl-timing",
          "rel": "relates-to"
        },
        {
          "to": "proof-pnl-step0-other-stations",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f1ba4ed4a4841e3f933f461e4897148246ec5399ab5ab3833a418926613e1fdc"
    },
    {
      "id": "proof-pnl-step0-other-stations",
      "path": "docs/proof/pnl/step0-other-stations.md",
      "title": "PNL step 0: how to know a non-governing station's panel under-read",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Ruling 103 says other stations keep the provisional row when their 200-vs-400 under-read is above 10 %, but nothing measures a non-governing station. Three ways to know it, with measured or estimated cost against the 1 s budget, and a recommendation for the Fable owner.",
      "tags": [
        "analysis",
        "section",
        "panel-method",
        "decision-request"
      ],
      "links": [
        {
          "to": "proof-a3bc-seam",
          "rel": "relates-to"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5ad82050ed8622f29512f05ea819f68409ab5b8ac81177ab925fa5c913f60880"
    },
    {
      "id": "proof-pnl-timing",
      "path": "docs/proof/pnl/timing.md",
      "title": "PNL timing: adaptive panels and the four whole-wing figures",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Re-measured Section_CamberedWing129_WarmTime with the governing 400-panel estimate, and reconciled the 419 / 723 / 430 ms and 1.6 s whole-wing figures by workload.",
      "tags": [
        "analysis",
        "section",
        "timing",
        "budget"
      ],
      "links": [
        {
          "to": "proof-a3bc-seam",
          "rel": "relates-to"
        },
        {
          "to": "proof-pnl-step0-other-stations",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ace33e86baa269e2496e9b195ecbc0b52b13dff9f6a509976359f7e24719a5bc"
    },
    {
      "id": "proof-pnl-underread-measurements",
      "path": "docs/proof/pnl/underread-measurements.md",
      "title": "PNL: measured 200-vs-400 under-read by thickness (Ruling 110 (6))",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "A3c",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Measured two-grid (p assumed 1) 200-vs-400 suction under-read for 1-12 % thick sections at alpha 3, 6, 10 deg. The 6 %-thick value is 3.60 % at alpha 3 and 4.39 % at alpha 6; thinner sections exceed 10 %.",
      "tags": [
        "analysis",
        "section",
        "panel-method",
        "measurement"
      ],
      "links": [
        {
          "to": "proof-pnl-step0-other-stations",
          "rel": "relates-to"
        },
        {
          "to": "proof-pnl-red-first",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e86a4c9b8b64650cf467324af8b9e0695383b7adfb662f3c4dbaec35f7535c38"
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
      "id": "proof-ring-b1-red-first",
      "path": "docs/proof/ring-b1/red-first.md",
      "title": "Ring B1 red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-b1",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Each self-test case of tools/check-test-costs.py was red against a stub checker (7429f70) and green once the checker landed; OD-2 measured and not met.",
      "tags": [
        "ring",
        "test-cost",
        "red-first"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "plan-test-cost",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "53823766f1d679444af5731548fbe23364c56d203ed72ac469d0cb961e78e076"
    },
    {
      "id": "proof-ring-b2-moves",
      "path": "docs/proof/ring-b2/moves.md",
      "title": "Ring B2 move list: Analysis under C-2 with every A8.4 check kept in ring 0",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-b2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Analysis harness made cheaper without moving any A8.4 oracle or observed-order check out of the fast ring; two n128 convergence halves move to readiness; PASS union loses no name.",
      "tags": [
        "ring",
        "test-cost",
        "c-2"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "plan-test-cost",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2cbcd895cae7c31ac2cea448e77eaa14e101b7e9afefe1cb473203553a7a412a"
    },
    {
      "id": "proof-ring-b2-pgo-compare",
      "path": "docs/proof/ring-b2/pgo-compare.md",
      "title": "Ring B2: Analysis readiness with TieredPGO on and off",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-b2",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "One Analysis --readiness pass with DOTNET_TieredPGO=1 and one with 0 print identical PASS names and identical MEASURE values.",
      "tags": [
        "ring",
        "test-cost",
        "tiered-pgo"
      ],
      "links": [
        {
          "to": "proof-ring-b2-profile",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "54f751cc1a9d564d3161ca836907e7f3f66666d2f718d1a818e20466453dece4"
    },
    {
      "id": "proof-ring-b2-profile",
      "path": "docs/proof/ring-b2/profile.md",
      "title": "Ring B2 profile: the fast ring under a concurrent build",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-b2",
      "phase": "implementation",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [],
      "summary": "Measured profile of tools/run-tests.sh with and without one concurrent heavy build, the levers tried, what shipped, and the Ruling 84 condition 3 verdict.",
      "tags": [
        "ring",
        "test-cost",
        "profile",
        "concurrency"
      ],
      "links": [
        {
          "to": "plan-test-cost",
          "rel": "relates-to"
        },
        {
          "to": "proof-ring-b2-moves",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b9043f4735b485762ba5bb31f2d07761274c57f5c616a6e2ddc9e6523719ae60"
    },
    {
      "id": "proof-ring-b2-ruling81-red-first",
      "path": "docs/proof/ring-b2/ruling81-red-first.md",
      "title": "Ring B2: Ruling 81 red-first receipt",
      "type": "proof-pack",
      "status": "active",
      "owner": "@trk-b2",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "A planted slow orbit frame fails at low load and prints READINESS-MISS, never PASS, at high load; the unplanted frame passes.",
      "tags": [
        "ring",
        "ruling-81",
        "readiness"
      ],
      "links": [
        {
          "to": "proof-ring-b2-profile",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "95f212dc46d2f49e65072aab721162cf59d63518f214b2325143fa40777cd95f"
    },
    {
      "id": "proof-round-oct05-heredoc-hook-proposal",
      "path": "docs/proof/round-oct05-lessons/heredoc-hook-proposal.md",
      "title": "AGENT-HEREDOC hook proposal (operator decision)",
      "type": "proof-pack",
      "status": "draft",
      "owner": "@track-ci",
      "phase": "implementation",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "A PreToolUse hook that refuses Bash heredocs, with a tested draft script. Not installed: wiring a hook into .claude/settings.json changes harness configuration and is the operator's call.",
      "tags": [
        "round-oct05",
        "hooks",
        "heredoc",
        "ct27",
        "operator-decision"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3431b4251b27d49c4c9ced0518f61fcbd7bb7d89a908ebc51033467b6f03777e"
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
      "id": "proof-spike-03",
      "path": "docs/proof/spike-03/verdict.md",
      "title": "SPIKE-03 verdict — unattended meshing across AR 5 / 8 / 12 on OpenFOAM v2512",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "NO-GO against the Appendix R words. snappyHexMesh meshes AR 5, 8 and 12 half wings unattended (35-63 s on 6 ranks, 1.1-2.6 M cells) and every mesh passes checkMesh, but none passes the ITTC/A5.10 floors: 15 layers reach 65-73 % of wing faces (0 % at the trailing edge and tip), measured mean y+ is 22 (below 30-100), and 32-35 % of cells have an OpenFOAM cell determinant below 0.3. macOS only; Windows not run. Dictionaries can run code by default (allowSystemOperations 1); a product-owned controlDict turns it off.",
      "tags": [
        "spike-03",
        "openfoam",
        "snappyhexmesh",
        "mesh-gate",
        "run",
        "backend"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "18e3220d90ee8091b6a61665b7fd1ff05fa8bb8e847381b949b6838d39dd127c"
    },
    {
      "id": "proof-spike-03-round2",
      "path": "docs/proof/spike-03/verdict-round2.md",
      "title": "SPIKE-03 round 2 verdict — wall-resolved meshing at AR 8 (snappyHexMesh and a Gmsh probe) and the solver-security launcher",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        }
      ],
      "summary": "Meshing NO-GO at AR 8, so AR 5 and 12 were not run. snappyHexMesh with wall-resolved absolute layers (8 um, ER 1.2, 20 layers) on the finite-TE, round-tip analysis wing stops at 14-15 layers (0 % of faces reach 20; 10 or more on 89 % / 75 %). The Gmsh boundary-layer probe puts 20 layers on 100 % of the wing faces by construction. Every wall- resolved mesh fails the DR-F2-6 floor (cellDeterminant >= 0.001) on its thin wall cells, and the gate's non-orthogonality limit. Security: the product launcher printed Disallowing on every round-2 process; the probe set S-1..S-8 is ready for the operator, not yet run. macOS arm64 only.",
      "tags": [
        "spike-03",
        "openfoam",
        "snappyhexmesh",
        "gmsh",
        "mesh-gate",
        "security",
        "launcher",
        "round-2"
      ],
      "links": [
        {
          "to": "proof-spike-03",
          "rel": "supersedes"
        },
        {
          "to": "plan-fluids-round2",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "proof-spike-04-round2",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d7b210f3102adc05f4c0216de3fd9cea9fa92b5ffbd4c9c26fcd61f9fa256838"
    },
    {
      "id": "proof-spike-03-round3",
      "path": "docs/proof/spike-03/verdict-round3.md",
      "title": "SPIKE-03 round 3 verdict — locating and repairing the Gmsh AR 8 wing mesh against the DR-F3-1 A gate",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [],
      "summary": "Mesh NO-GO at AR 8 after both repair cycles, so R3-M2, R3-M3 (AR 5, 12) and R3-M4 (the 3-D y+ gate solve) were not run. R3-M0 located the 811 faces above 70 deg: 616 on the TE arc strip, 173 at the tip's TE end, none at the prism/tet interface. The cause is measured, not assumed: the 1.49 mm layer stack spreads each prism column 7.0x around the 0.25 mm TE arc and not along the span. A structured TE strip sized by that ratio (R3-M1b) took the panel TE from 616 faces to 1. The gate still fails on the revolved tip (246 faces > 70 deg, max 85.2 deg) and on 4 faces with weight < 0.05. Gmsh repeated M-2c bitwise. macOS arm64 only.",
      "tags": [
        "spike-03",
        "openfoam",
        "gmsh",
        "mesh-gate",
        "boundary-layer",
        "round-3"
      ],
      "links": [
        {
          "to": "proof-spike-03-round2",
          "rel": "supersedes"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "relates-to"
        },
        {
          "to": "proof-spike-04-round3",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "21746faee23c69059cc4534d362e1744a99503cc6acb4f450ad565ce8e4ab2d9"
    },
    {
      "id": "proof-spike-03-tip-bl-route",
      "path": "docs/proof/spike-03/tip-bl-route/verdict.md",
      "title": "SPIKE-03 tip BL route S6 — verdict",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-s6",
      "phase": "spike",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "No boundary-layer route meshes the flat tip of record to DR-F3-1 A. W1 (Gmsh fan option) is byte-identical to S4 V1: the option has no 3-D effect. W2a and W2b (snappyHexMesh) cut the failures from 3,813 faces to 61 and 6 and remove the negative cells in W2b, but reach 0 % full layer columns on the tip (at most 14 of 20). W4 (8 um round) fails before a mesh exists (Gmsh PLC error). W3 had no tool. The rule says stop and report; S5 stays untriggered. W2b is the nearest and had one untested cause (surface cells vs stack). W2c (Ruling 98) tested it: finer refinement gave cleaner tip cells but fewer layers (mean 2.8 of 20), so the hypothesis is refuted at +1 and +2 did not finish. S5 stays untriggered. macOS arm64 only.",
      "tags": [
        "spike-03",
        "gmsh",
        "snappyhexmesh",
        "mesh-gate",
        "tip",
        "boundary-layer",
        "ruling-97"
      ],
      "links": [
        {
          "to": "proof-spike-03-tip-bl-route-prereg",
          "rel": "implements"
        },
        {
          "to": "proof-spike-03-tip-coupon",
          "rel": "relates-to"
        },
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "132ffd2765f29e99e85e652ffc0af77d2c9295ee48ce62692fe78b3638271167"
    },
    {
      "id": "proof-spike-03-tip-bl-route-prereg",
      "path": "docs/proof/spike-03/tip-bl-route/preregistration.md",
      "title": "SPIKE-03 tip BL route S6 — pre-registration (variants, metrics, decision rule)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-s6",
      "phase": "spike",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Written and committed before any S6 mesh. Four boundary-layer route variants on S4's V1 coupon (the planar flat cut at b/2 of Ruling 93): W1 Gmsh with the corner-fan option set, W2a and W2b snappyHexMesh addLayers (layers around the edge, layers terminated at the edge), W4 an in-mesh 8 um perimeter round (needs an operator ruling). W3 (K2 normal smoothing) has no available tool and is not run. The S4 metrics, the pass definition and the decision rule.",
      "tags": [
        "spike-03",
        "gmsh",
        "snappyhexmesh",
        "mesh-gate",
        "tip",
        "boundary-layer",
        "pre-registration",
        "ruling-97"
      ],
      "links": [
        {
          "to": "proof-spike-03-tip-coupon",
          "rel": "relates-to"
        },
        {
          "to": "proof-spike-03-tip-coupon-prereg",
          "rel": "relates-to"
        },
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b92a51dd7acb8363449c183b1fb35aea9fd0dd5fb2f247f7755bcab3ff44ab2b"
    },
    {
      "id": "proof-spike-03-tip-coupon",
      "path": "docs/proof/spike-03/tip-coupon/verdict.md",
      "title": "SPIKE-03 tip mesh coupon S4 — verdict",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-s4",
      "phase": "spike",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "All four coupon variants fail DR-F3-1 A in the tip region, so the pre-registered stop rule fires and S5 is not triggered. V0 reproduces round 3 (178 tip-pole and 37 tip faces above 70 degrees). V1, the tip of record, is far worse: 3,813 faces, 99 % at the convex cap edge, 263 negative-volume cells. V2 (short pole flats) leaves 312 faces and 63 negative-volume cells. V3 (stack cut to 0.47 mm) leaves 213, 5 % below V0. The cause is not separated; the stack-height fan reading is not supported. macOS arm64 only.",
      "tags": [
        "spike-03",
        "gmsh",
        "mesh-gate",
        "tip",
        "coupon",
        "ruling-88"
      ],
      "links": [
        {
          "to": "proof-spike-03-tip-coupon-prereg",
          "rel": "implements"
        },
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        },
        {
          "to": "proof-spike-03-round3",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cb3f9c2e50e04a314f9ec73768437edc3a9ff80f8fbb922678419db666ff9885"
    },
    {
      "id": "proof-spike-03-tip-coupon-prereg",
      "path": "docs/proof/spike-03/tip-coupon/preregistration.md",
      "title": "SPIKE-03 tip mesh coupon S4 — pre-registration (variants, metrics, receipts, decision rules)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@trk-s4",
      "phase": "spike",
      "reviewBy": "2026-11-05",
      "reviewSuggested": [],
      "summary": "Written and committed before any coupon mesh is generated. Four variants of a 25 mm tip-only coupon (V0 current revolve, V1 planar flat cut at b/2, V2 short flats at the poles, V3 stack height cut at fixed first height), the metrics and receipts for each, and the decision rules of docs/plans/tip-handling.md section 4.2 verbatim, with every term that needed a number given one here, now.",
      "tags": [
        "spike-03",
        "gmsh",
        "mesh-gate",
        "tip",
        "coupon",
        "pre-registration",
        "ruling-88"
      ],
      "links": [
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        },
        {
          "to": "proof-spike-03-round3",
          "rel": "relates-to"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "61f09198b08e03c6bb46e05e604f108332035827471ff46b782a3617d7285fdf"
    },
    {
      "id": "proof-spike-04",
      "path": "docs/proof/spike-04/verdict.md",
      "title": "SPIKE-04 verdict — three-grid convergence oracle on NASA TMR NACA 0012 (OpenFOAM v2512)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-03",
          "reason": "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar."
        }
      ],
      "summary": "NO-GO; stopped at Stage 1. On TMR Family II levels 6 and 5 (SA, alpha 10, Re 6e6), simpleFoam never met the stated residual floor (1e-7). With linearUpwind, bound() clipped negative nuTilda in 39,994 of 40,000 iterations and residuals froze at a fixed point. With TVD limitedLinear, a period-2 limiter cycle kept nuTilda at 4-5e-5. The repair cap (2) was reached, so the third grid and GCI were not run. v2512 has no SA-neg model.",
      "tags": [
        "spike-04",
        "openfoam",
        "verification",
        "gci",
        "tmr",
        "naca0012",
        "spalart-allmaras"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "055439ed8209cb8c795e1c6b94d5e64db4f837e80f5c9b3230e4de8cdf0aef5c"
    },
    {
      "id": "proof-spike-04-round2",
      "path": "docs/proof/spike-04/verdict-round2.md",
      "title": "SPIKE-04 round 2 verdict — A4 convergence and three-grid study on NASA TMR NACA 0012 (OpenFOAM v2512)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        }
      ],
      "summary": "NO-GO on the GCI clause, GO on the iterative oracle. After two numerics repairs on level 6 (first-order upwind nuTilda, then relaxation 0.7), OpenFOAM v2512 simpleFoam SA-noft2 meets the A4 criterion on TMR Family II levels 6, 5 and 4 and rhoSimpleFoam meets it on level 5. The three-grid sequence is oscillatory for Cl and Cd (R = -0.010 and -0.053), so there is no observed order and no GCI. The measured compressible-minus-incompressible delta at TMR conditions on level 5 (one grid, alpha 10, Re 6e6, M 0.15) is +1.147 % in Cl and +0.81 % in Cd, U_Delta not stated. Fully turbulent air, 2-D. macOS arm64 only.",
      "tags": [
        "spike-04",
        "openfoam",
        "verification",
        "gci",
        "tmr",
        "naca0012",
        "spalart-allmaras",
        "round-2",
        "a4"
      ],
      "links": [
        {
          "to": "proof-spike-04",
          "rel": "supersedes"
        },
        {
          "to": "plan-fluids-round2",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "documents"
        },
        {
          "to": "proof-spike-03-round2",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "383f7afc75666a6a5ccde35bc048b72fc8faa4a15fd6191dd57603fd56df5e59"
    },
    {
      "id": "proof-spike-04-round3",
      "path": "docs/proof/spike-04/verdict-round3.md",
      "title": "SPIKE-04 round 3 verdict — searching for a monotone TMR grid family (OpenFOAM v2512)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@fluids-f1",
      "phase": "spike",
      "reviewBy": "2026-11-03",
      "reviewSuggested": [],
      "summary": "GCI NO-GO: no monotone triplet was admitted, so no observed order and no GCI. R3-G0 rejects H1: the unlimited laplacian moves L6 Cl by -1.03e-4, below the 2.8e-4 threshold. Numerics cycle 1 (limitedLinear 1 nuTilda at relaxation 0.7) misses A4 on L6 on clause 3 (5,013 clipped iterations in 40,000), so cycle 2 is not run. Per DR-F3-3 the L3 run used the D4 numerics. It reached the 10 h solve cap at 41,005 iterations without A4 (Cl half-band 9.8e-5 against 1e-5), so the L5/L4/L3 triplet is not admitted. A4 and D4 stand. DR-F3-7 default (c) is recommended.",
      "tags": [
        "spike-04",
        "openfoam",
        "verification",
        "gci",
        "tmr",
        "naca0012",
        "spalart-allmaras",
        "round-3",
        "a4"
      ],
      "links": [
        {
          "to": "proof-spike-04-round2",
          "rel": "supersedes"
        },
        {
          "to": "plan-fluids-round3",
          "rel": "implements"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "relates-to"
        },
        {
          "to": "proof-spike-03-round3",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4fb99111a558deab5e10d9f3eb5c70674e6e077fbc1cc68bde8540ade240461f"
    },
    {
      "id": "proof-spike-ana-1",
      "path": "docs/proof/spike-ana-1/verdict.md",
      "title": "SPIKE-ANA-1: in-process NeuralFoil port fidelity, XFOIL accuracy, CST fit and licence facts",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "GO on C# port fidelity: 198 outputs across 90 shared CST cases differ from Python NeuralFoil by at most 3.91e-14 absolute. GO on the measured NACA 0012 accuracy grid: 78 of 78 local XFOIL 6.99 points meet both Cl and ln Cd limits. The package declares MIT and includes the weight files, but the weights have no separate notice in the installed wheel; distribution awaits security and operator licence review.",
      "tags": [
        "analysis",
        "neuralfoil",
        "xfoil",
        "cst",
        "kulfan",
        "polar",
        "licence",
        "spike",
        "dr-ana-1"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "relates-to"
        },
        {
          "to": "coordination-round-oct05",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "98fc323b64cc5321cef2814b7e0a4bc22c94176923d646d7f274a05a6744392f"
    },
    {
      "id": "proof-spike-ana-1-red-first",
      "path": "docs/proof/spike-ana-1/red-first.md",
      "title": "SPIKE-ANA-1 red-first probe test",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "design",
      "reviewBy": "2026-11-30",
      "reviewSuggested": [],
      "summary": "Records the observed failing Python-oracle test on the placeholder C# probe and its passing commit after the port.",
      "tags": [
        "analysis",
        "neuralfoil",
        "spike",
        "red-first"
      ],
      "links": [
        {
          "to": "proof-spike-ana-1",
          "rel": "relates-to"
        },
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9eadaee45e837755d74b4a2b2444a7f426ab118b7d2c53526e615c858a17f992"
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
      "id": "proof-vlm-tip-repaired",
      "path": "docs/proof/vlm-tip-study/repaired-verdict.md",
      "title": "Tip-strip law on the repaired 1.1.0 lattice",
      "type": "proof-pack",
      "status": "superseded",
      "owner": "@vlm4",
      "phase": "implementation",
      "reviewBy": "2027-04-04",
      "reviewSuggested": [],
      "summary": "Superseded by the tip-handling study (docs/plans/tip-handling.md, Ruling 88). Historical record of the rejected eta-star convention: tests the fixed-station tip convention against an analytic midspan anchor and local lift; the rectangular high-alpha falsifier blocks a universal tip verdict.",
      "tags": [
        "analysis",
        "vlm",
        "tip-strip"
      ],
      "links": [
        {
          "to": "proof-vlm-tip-study",
          "rel": "refines"
        },
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "02d9debd201e44ce2372db58673134d39780944f5f7711e6e7431ead6ec5947f"
    },
    {
      "id": "proof-vlm-tip-s2-preregistration",
      "path": "docs/proof/vlm-tip-study/s2-sweep/preregistration.md",
      "title": "S2 small-tip-chord VLM sweep: pre-registration",
      "type": "proof-pack",
      "status": "draft",
      "owner": "@trk-s2",
      "phase": "implementation",
      "reviewBy": "2027-04-05",
      "reviewSuggested": [],
      "summary": "Case grid, recorded fields and decision rules for the S2 sweep, written before any solve.",
      "tags": [
        "analysis",
        "vlm",
        "tip-strip",
        "taper",
        "preregistration",
        "ruling-78",
        "ruling-88"
      ],
      "links": [
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "495d4fb663b53437ac42f5e23f69acf313ed7729b6bdc015430dcc7585e39425"
    },
    {
      "id": "proof-vlm-tip-s2-verdict",
      "path": "docs/proof/vlm-tip-study/s2-sweep/verdict.md",
      "title": "S2 small-tip-chord VLM sweep: verdict",
      "type": "proof-pack",
      "status": "verified",
      "owner": "@trk-s2",
      "phase": "implementation",
      "reviewBy": "2027-04-05",
      "reviewSuggested": [],
      "summary": "One judged-strip flip in 15 (ratio, alpha) pairs: r = 0.01 at alpha 8, coarse lattice only, in the conservative direction.",
      "tags": [
        "analysis",
        "vlm",
        "tip-strip",
        "taper",
        "ruling-78",
        "ruling-88"
      ],
      "links": [
        {
          "to": "proof-vlm-tip-s2-preregistration",
          "rel": "implements"
        },
        {
          "to": "plan-tip-handling",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6d64b2f1fc999b79594d596dc98814f0ccf97719f64d5796523d842c7243135d"
    },
    {
      "id": "proof-vlm-tip-study",
      "path": "docs/proof/vlm-tip-study/verdict.md",
      "title": "VLM tip-strip envelope study",
      "type": "proof-pack",
      "status": "verified",
      "owner": "@vlm3",
      "phase": "implementation",
      "reviewBy": "2027-04-04",
      "reviewSuggested": [],
      "summary": "Measured tip-strip refinement, tolerance-law proposal, and the default lattice's camber and twist failure.",
      "tags": [
        "analysis",
        "vlm",
        "tip-strip",
        "camber",
        "twist"
      ],
      "links": [
        {
          "to": "design-area3-analysis",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "01e899a5a83c12376107a8216aa4474158fe4fefbd6bf60f9a89d27b450d373e"
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
      "id": "review-m12c-native",
      "path": "docs/reviews/m12c-native.md",
      "title": "Native review — M1.2c section editor (UXR polish and the operator's native-look checklist)",
      "type": "proof-pack",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "implementation — UXR (M1.2c)",
      "reviewBy": "2026-10-31",
      "reviewSuggested": [],
      "summary": "Track UXR, 4 October 2026. The deviations EDT's and PNL's captures showed are fixed, except where §3 says why not. Screens 2, 2b, 2c and 3 were recaptured from the built app in light and dark mode (docs/proof/m12c-uxr). The marine-CAD re-review raised one soft veto: the comb pointed into the foil. It is fixed under a red-first test and the veto is cleared. Three findings go to the operator: handle Length is entered in mm, the comb is sparse, and the Points y colour. The packaged app is ready for the operator's native-look walk (§5). The native rows N-12C-1 to N-12C-11 are pending until the operator attaches their receipts.",
      "tags": [
        "native-ui",
        "m1.2c",
        "section-editor",
        "review",
        "operator-run",
        "captures"
      ],
      "links": [
        {
          "to": "design-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "review-m12b-native",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3b9d46da8fc3e5cf4c03bdd853aa3ca70a4e30ae263e56ba3b52082e0bed81cb"
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
      "sourceSha256": "0166a2f2090401df8de20032ca2b18dd75ff2023410f1f3c7e6df8f6fb652261"
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
      "id": "spec-amendment-guided-solver-setup",
      "path": "docs/specs/amendments/guided-solver-setup.md",
      "title": "Spec amendment proposal: guided solver setup (UX layer and AI assistance) — OpenFOAM on macOS, OpenFOAM and SU2 on Windows",
      "type": "spec",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "specification — UX layer, proposal (not applied)",
      "reviewBy": "2026-11-04",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut"
        }
      ],
      "summary": "Ruling 67 asks for an \"on rails\" solver install for a foil designer who is not a software engineer: Windows for him, macOS for the operator. This proposal adds flow F7a (guided solver setup) to the spec's UX layer: no setup prompt at first launch; three entry points; detect, then one recommended route per OS (macOS: OpenFOAM.app v2512; Windows: OpenFOAM in an app-owned WSL distribution, SU2 native as the other option); each step in plain words with the details one click away; the app does every step it can and hands the user one exact OS click path for the rest (administrator approval, restart, firmware virtualization, macOS Open Anyway); Ready only from the smoke-test fact; resume after a restart. The rails work with no key and no network model; the assistant only explains and may suggest one allowed step id. Amends RUN-01, AI-11, A5.10 (smoke scalar), A5.12 (Run entry name), COPY-75/76 and adds C2 rows for the four honest-limit strings. Windows behaviour is Inferred until the operator's Windows smoke test. Six decisions are open as DR-SETUP-1..6 in the design.",
      "tags": [
        "run",
        "backend",
        "install",
        "setup",
        "openfoam",
        "su2",
        "wsl",
        "windows",
        "macos",
        "assistant",
        "hax",
        "shape-of-ai",
        "ruling-67"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "note-solver-security-right-size",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "depends-on"
        },
        {
          "to": "kb-hw-simulation-openfoam-su2-interop",
          "rel": "depends-on"
        },
        {
          "to": "proof-spike-03",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "implements"
        },
        {
          "to": "design-guided-solver-setup",
          "rel": "tested-by"
        },
        {
          "to": "mockup-solver-setup",
          "rel": "relates-to"
        },
        {
          "to": "design-language",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "3.2 Flows",
          "mermaid": "flowchart TD\nL[App launch] --> L1{Open setup session?}\nL1 -->|No| L2[Normal start: no setup prompt. Status strip: Solver: not set up]\nL1 -->|Yes, last step asked for a restart| R0[Survey again] --> RS[Solver setup tab opens on the resumed step]\nL2 --> E1[Run area: Set up a solver]\nL2 --> E2[An analysis that needs a solver: Set up OpenFOAM]\nL2 --> E3[Status strip: Solver item]\nE1 --> S[Survey this computer: read-only, no consent]\nE2 --> S\nE3 --> S\nS --> F{Existing install of the pinned build?}\nF -->|Yes, hashes match| V[Verify: identify + smoke test]\nF -->|Yes, does not match| U[Unverified install: Use the tested build · Use mine anyway]\nU -->|Use the tested build| RT\nU -->|Use mine anyway: disclosure accepted| V\nF -->|No| RT{Route for this OS}\nRT -->|macOS arm64, 14+| MAC[OpenFOAM.app v2512: download · verify · place · identify]\nRT -->|Windows 10 2004+ / 11| WIN[OpenFOAM in WSL: enable WSL · restart · Linux base · OpenFOAM · identify]\nRT -->|Other options| SU2[Windows: SU2 v8.5.0: download · verify · place]\nRT -->|No route: Intel Mac, old OS| NR[No supported solver for this computer: reason · what still works]\nMAC --> STEP\nWIN --> STEP\nSU2 --> STEP\nSTEP[Step card: plain words · time · size · needs · Technical details] -->|Start this step| OSQ{Needs the OS?}\nOSQ -->|Admin approval| UAC[Windows asks: click Yes] --> RUN\nOSQ -->|Restart| RB[Restart needed: save, restart; the app reopens and continues] --> L\nOSQ -->|Firmware virtualization off| FW[Turn on virtualization in firmware: maker-specific click path] --> L\nOSQ -->|macOS blocked the app| OA[System Settings · Privacy and Security · Open Anyway] --> RUN\nOSQ -->|No| RUN[App runs the step]\nRUN -->|Succeeded| NEXT{More steps?}\nRUN -->|Failed: known cause| FC[Cause in plain words + one next step]\nRUN -->|Failed: unknown cause| UK[Explain this failure if a key · Copy a report always]\nFC --> STEP\nUK --> STEP\nNEXT -->|Yes| STEP\nNEXT -->|No| V\nV -->|Smoke passes: Backend check fact| RD[Ready: solver, build, test time · Back to my analysis]\nV -->|Smoke fails| SF[Not ready: cause + one next step]\nSF --> STEP"
        }
      ],
      "sourceSha256": "22f04e75d82c392bf6e55fd9088ca341f1ce853066f053917f58d5c7dcb69e4f"
    },
    {
      "id": "spec-amendments-1-7",
      "path": "docs/specs/amendments/spec-1.7.md",
      "title": "Spec 1.7 amendment batch — every spec-owner request, as exact text, approved in Ruling 66",
      "type": "spec",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        }
      ],
      "summary": "One batch for the spec owner, approved in Ruling 66. 49 amendments to cfd-workbench-v1 (CAD 24, Analysis 8, Run 7, Copy 3, Shell 7), each with the quoted before-text, the exact 1.7 text, the ruling it comes from and the tests or designs it touches; revision 1.7 carries them all. AM-1.7-37 to -49 are the 13 open questions the owner accepted as recommended. The knowledge-base correction KB-1 (ITTC Eq. 10, ln to log10) is made in the knowledge base.",
      "tags": [
        "spec",
        "amendments",
        "rulings",
        "cad",
        "analysis",
        "run",
        "shell",
        "copy"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "property-grid-rulings",
          "rel": "depends-on"
        },
        {
          "to": "note-m12c-rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "plan-fluids-round2",
          "rel": "depends-on"
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b-points",
          "rel": "depends-on"
        },
        {
          "to": "design-m12b2-3d-elevations",
          "rel": "depends-on"
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "depends-on"
        },
        {
          "to": "design-app-shell",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-master-curve-degree",
          "rel": "depends-on"
        },
        {
          "to": "review-ui-property-grid",
          "rel": "relates-to"
        },
        {
          "to": "kb-hw-data-and-constants",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "eeaa0b25f4a0896fb18df297a833e6d6c71927198d5e46c9f7d044d1ccbc27ce"
    },
    {
      "id": "spec-amendments-1-7-1",
      "path": "docs/specs/amendments/spec-1.7.1.md",
      "title": "Spec 1.7.1 amendment batch — the solver-security right-size, as exact text, approved in Ruling 68",
      "type": "spec",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized"
        },
        {
          "by": "adr-0012-openfoam-backend-macos",
          "on": "2026-10-04",
          "reason": "ADR-0012 D2 right-sized and DR-SEC-1 A recorded (Ruling 68)"
        }
      ],
      "summary": "One small batch for the spec owner, approved in Ruling 68. Four amendments to cfd-workbench-v1 (A8.5 two, A5.10 two), each with the quoted before-text and the exact 1.7.1 text. They record the verified controlDict refusal, the right-sized launcher rules (ADR-0012 D2) and the install check, and make Run depend on the install smoke test showing Disallowing. Revision 1.7.1 carries them all; the change record is Appendix H, section H.1.",
      "tags": [
        "spec",
        "amendments",
        "rulings",
        "security",
        "backend",
        "openfoam"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "spec-amendments-1-7",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "note-solver-security-right-size",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e641848c00abc3c83c662f5ee4689086d7f796d2dee3ce4b1173f9294907de5b"
    },
    {
      "id": "spec-amendments-1-7-2",
      "path": "docs/specs/amendments/spec-1.7.2.md",
      "title": "Spec 1.7.2 amendment batch — guided solver setup, the smoke-test scalar and the toggle shortcut, as exact text, for the spec owner's approval",
      "type": "spec",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench-v1",
          "on": "2026-10-04",
          "reason": "Spec 1.7.2 (draft; Ruling 69, Ruling 67 OD-1): guided solver setup, smoke-test scalar, Windows route, toggle shortcut"
        }
      ],
      "summary": "Approved in Ruling 70 (with OQ-1 to OQ-5 as recommended). 29 amendments and two insertions to cfd-workbench-v1, each with the quoted before-text and the exact 1.7.2 text, traced to Ruling 69 (guided solver setup, DR-SETUP-1..6) and Ruling 67 OD-1 (the toggle shortcut). It also fixes a design-text mismatch (section handle Length). Five open questions have no ruling and are not applied. Revision 1.7.2 of the spec carries the batch; the change record is Appendix H, section H.2. Approved; the merge is the coordinator's.",
      "tags": [
        "spec",
        "amendments",
        "rulings",
        "run",
        "backend",
        "setup",
        "windows",
        "wsl",
        "openfoam"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "spec-amendments-1-7-1",
          "rel": "depends-on"
        },
        {
          "to": "spec-amendment-guided-solver-setup",
          "rel": "depends-on"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        },
        {
          "to": "design-area3-analysis",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-openfoam-backend-macos",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "59fb7876a86762911337c984f3786c4f1ef6d2fffcdd5f71fec5c99c1530f54e"
    },
    {
      "id": "spec-amendments-1-7-5",
      "path": "docs/specs/amendments/spec-1.7.5.md",
      "title": "Spec 1.7.5 amendment batch — group move on a curve (CAD-04) and the wing-only drag (A5.6, ANA-03), as exact text",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-04-01",
      "reviewSuggested": [],
      "summary": "Two clauses to cfd-workbench-v1 and one retired note, traced to Rulings 107 and 108. The CAD-04 clause and the retired node M note are approved (Ruling 107 DR-GM-8). The A5.6 and ANA-03 clause is the hydrodynamicist's form of Ruling 108 DXM-5, which differs from the ruling's text; the operator approved it as Ruling 109. Revision 1.7.5 of the spec carries the batch; the change record is Appendix H, section H.5.",
      "tags": [
        "spec",
        "amendments",
        "rulings",
        "cad",
        "analysis",
        "drag"
      ],
      "links": [
        {
          "to": "spec-cfd-workbench-v1",
          "rel": "refines"
        },
        {
          "to": "spec-amendments-1-7-2",
          "rel": "relates-to"
        },
        {
          "to": "rulings",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f86ae43d2dafa586c8a3f8bea110c9ee530e964da91f5944e1aed37eb4d6ebe3"
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
      "title": "CFD-Workbench — product specification v1.7 (CAD-first editing, Wing estimates, section catalog and the 1.7 amendments)",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-04-01",
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
        }
      ],
      "summary": "The specification the product is built against. Seven discrete, complementary areas — Setup · CAD · Analysis · Experiment setup · Run · Results · Export — each with an AI prompt entry whose output is a typed, validated, previewed proposal. One explicit parametric definition whose payload reproduces its surface; an operating point that carries depth, water and a goal state; analysis tiers that may claim only what their fixtures earn; a catalog admitted by rights class; a sweep-or-optimize experiment driven end to end against OpenFOAM or SU2 with evidence by files; results as sequences of admitted samples with named bases; hard states and fixed copy for every honest limit. Revision 1.5 adds persistent section editing, shared-profile scope, draft-safe inspection, named design alternatives and explicit geometry-intent commands to FoilDSL authoring. Revision 1.6 makes the CAD area CAD-first (mockup v10): a start card, a workspace of views with a narrow left Properties pane and optional docks, per-point Anchor/Control types, typed Span/Root chord/Tip chord with always-visible derived Wing estimates (mean chord S/b, MAC, max t/c, AR, area), a section editor mode with Finish/Cancel, and Replace from catalog / Save to My sections; superseded 1.1a–1.5 wording is marked in place (Appendix G). Revision 1.7 applies one batch of 49 spec-owner amendments approved in Ruling 66, each traced to a ruling (Rulings 53–66, the property-grid and M1.2c rulings): the 4–16 vertex range under FoilDSL 4.1 with Add point, Remove point and Rebuild to N; paired section point types; the quarter-chord held line; the Evaluate verb, panel Cp, the depth-unset VLM label and the revised lattice oracles; the three-part residual criterion and the revised mesh gate; no Messages pane and Points in the right side bar; Add point and Remove point applying at once (Appendix H; the batch is amendments/spec-1.7.md). Revision 1.7.1 applies Ruling 68: the A8.5 backend-substrate row and the A5.10 readiness text take the verified controlDict refusal and the right-sized launcher rules, and Run is enabled when the install smoke test shows Disallowing (Appendix H.1; amendments/spec-1.7.1.md). Revision 1.7.2 applies Ruling 69 and Ruling 67 OD-1: one verb, Set up a solver, that works with no key (RUN-01, AI-11, flow F7a, stories SETUP-01–09); the smoke test reads the final-time mean Courant number, not a Cl that a cavity cannot have; the Windows default route is OpenFOAM in an app-owned WSL distribution (Inferred until the Windows run); the exact command moves under Technical details; and the CAD ↔ Analysis shortcut is ⇧⌘A (Appendix H.2; amendments/spec-1.7.2.md). Revision 1.7.3 applies Ruling 92 (with Rulings 86, 88 D3 and D16, and 79): the cavitation screen is governed by the station with the smallest σ_i/(−Cp_min,i), the v1 tip of record is the open planar end at the last authored station, and SPIKE-04 is code-to-code against CFL3D until an L3 grid triplet is admitted (Appendix H.3). Revision 1.7.4 applies Rulings 93, 94 and 95: a wing tip is always a finite chord (a tip point is out of scope for v1), the tip chord is never less than max(5 mm, 2 % of the root chord), the planform edits refuse an edit that would cross it, and the analysis refusal for older files names the same minimum (Appendix H.4).",
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
          "mermaid": "flowchart TD\nA[Choose Section or Wing] --> B{Operating point}\nB -->|From Goal state point n| B2[Speed, water, h_ref, load copied; read-only link]\nB -->|Custom| B3[Set speed, water, incidence or load; Goal state untouched]\nB2 --> C\nB3 --> C{Depth set?}\nC -->|No| D[σ, Fr_h, V_crit Unavailable; Set depth stays offered]\nD --> E\nC -->|Yes| E[Derive h/c, Fr_h, σ per station]\nE --> E2{Any h(y) ≤ 0?}\nE2 -->|Yes| E3[Station estimator Unavailable; surface-piercing flag only]\nE3 --> F\nE2 -->|No| F{Water record admitted for T and S?}\nF -->|No| G[Unavailable: outside the ITTC table; choose admitted range]\nG --> B\nF -->|Yes| H{Polar data at this Re and profile?}\nH -->|No| I[Unavailable: outside the Re grid or no backend; choose an admitted point]\nI --> B\nH -->|Yes| L[Compute at Ncrit pair against the pinned revision]\nL --> M{Outcome}\nM -->|Success| N[Results with labels, omissions, depth basis, band]\nM -->|Derived quantity outside a bound| K[Out-of-envelope observation: advisory finding; result kept]\nK --> N\nM -->|Failed| O[Keep Historical; inspect reason; retry]\nO --> B\nM -->|Find α or take-off: no crossing| O2[Reason enum shown; nothing extrapolated]\nO2 --> B\nN --> P[Compare revisions or tiers]\nP -->|Incompatible references| Q[Block delta until reconciled]\nQ -->|Reconcile reference quantities| P\nP -->|Compatible| R[Normalised per-point deltas; Discrepancy record]\nN -->|Definition, setting or method changed| S[Historical banner until Evaluate is pressed - 1.7, DR-ANA-6]\nS --> L\nN --> T[Checks drawer: envelope and label findings]\nN -->|Sweep this| U[Jump chip: define a Case schedule in Experiment]"
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
          "mermaid": "flowchart TD\nA[Open Experiment] --> B{Kind}\nB -->|Sweep| C[Angles × speeds or Goal-state points; held water, depth, geometry, method]\nB -->|Optimize| D[Objective over multipoint set; constraints incl. A_cav; design vector; robustness; tier; budget]\nB -->|Describe the experiment| E[experiment-config proposal; preview; edit]\nE --> C\nE --> D\nD --> D2{Single-point objective?}\nD2 -->|Yes| D3[Refused with the A5.9 string; add a point]\nD3 --> D\nD2 -->|No| F\nC --> F[Preview cases with derived quantities and estimate]\nF -->|Invalid sample| G[Blocked; input named]\nG --> C\nF -->|Queue| H[Experiment version immutable; status Queued]\nH --> I[Open Run]\nI --> I2{Tier}\nI2 -->|local · in-process| S2[Evaluate in process; attempts and evidence as for a backend]\nS2 --> X\nI2 -->|backend| J{Backend Ready?}\nJ -->|No| K[Detection; Set up a solver, no key needed: step ids only; parameters bound by the tool]\nK -->|Step failed or declined| L[Recoverable; CAD works; Run stays Not ready]\nK -->|Step refused: outside the allow-list| L\nK -->|Smoke test passes: Backend check fact| M[Ready]\nJ -->|No row matches the pin| L2[Not ready; pin named; nothing launches]\nJ -->|Yes| M\nM -->|Disk exhausted or version mismatch| M2[Stop safely; case retained; retry from a valid stage]\nM2 --> M\nM --> N{Case supported by capability record?}\nN -->|No| O[Unsupported with reason; other cases proceed]\nN -->|Yes| P[Meshing]\nP -->|Cancel| T\nP --> Q{Mesh gate}\nQ -->|Fail| R[Stopped before solving; measure and threshold named; Explain this failure]\nR -->|Repair accepted| R2[New Experiment version in Draft; Open repaired draft]\nQ -->|Pass| S[Solving: residuals, forces, elapsed, resources]\nS -->|Cancel| T[Substrate kill; tree kill; orphan scan; Cancelled with partial outputs]\nS -->|Crash| U[Failed with reason; logs retained; Retry sample]\nS -->|Exit| V{Outputs present?}\nV -->|No| U\nV -->|Yes| W[Harvesting: evidence by files; Field evidence rows]\nW -->|Cancel| T\nW -->|Unknown column layout| U\nW -->|Checkpoint valid| W2[Resume offered from the validated checkpoint]\nW --> X[Completed; Converged label if criteria met]\nS -->|App quit| S3[Orphan scan on relaunch; state from events]\nS3 --> S\nX -->|All cases terminal| X2{Experiment status}\nX2 -->|≥ 1 Completed| Y\nX2 -->|0 Completed| X3[Experiment Failed; reasons per case]\nX --> Y[Open Results]"
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
      "sourceSha256": "089d17363f4285a32add7c197d86fc21a671625ad2f8a2cb96f4b59a8bcbe157"
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
      "sourceSha256": "25f071b72c9676cd418be3606adf4a79a0e36315b529ce03a3b414e357649955"
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
        },
        {
          "to": "design-m12c-section-editor",
          "rel": "documents"
        },
        {
          "to": "design-planform-point-verbs",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f89e1dab447746685bf5f8750910c83900049ba327bdac8e00718aa1bcd8ffe4"
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
      "id": "surface-mockups-area3-analysis",
      "path": "docs/mockups/area3-analysis.html",
      "title": "Area 3 analysis",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-area3-analysis"
    },
    {
      "id": "surface-mockups-dx-section-polar-states",
      "path": "docs/mockups/dx-section-polar-states.html",
      "title": "Area 3 section and polar states",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-dx-section-polar-states"
    },
    {
      "id": "surface-mockups-cad-limits-in-gesture",
      "path": "docs/mockups/cad-limits-in-gesture.html",
      "title": "CAD limits in the gesture",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-cad-limits-in-gesture"
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
      "id": "surface-coordination-round-oct05",
      "path": "docs/coordination/round-oct05.html",
      "title": "CFD-Workbench — coordination plan",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "coordination-round-oct05"
    },
    {
      "id": "surface-coordination-round-oct06",
      "path": "docs/coordination/round-oct06.html",
      "title": "CFD-Workbench — coordination plan",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "coordination-round-oct06"
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
    },
    {
      "id": "surface-mockups-group-move-node-m",
      "path": "docs/mockups/group-move-node-m.html",
      "title": "Group move and typed value",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-group-move-node-m"
    },
    {
      "id": "surface-mockups-m12b2-views",
      "path": "docs/mockups/m12b2-views.html",
      "title": "M1.2b2 views",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-m12b2-views"
    },
    {
      "id": "surface-mockups-m12c-section-editor",
      "path": "docs/mockups/m12c-section-editor.html",
      "title": "M1.2c section editor",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-m12c-section-editor"
    },
    {
      "id": "surface-mockups-m12d-catalog",
      "path": "docs/mockups/m12d-catalog.html",
      "title": "M1.2d Catalog Replace",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-m12d-catalog"
    },
    {
      "id": "surface-mockups-planform-point-verbs",
      "path": "docs/mockups/planform-point-verbs.html",
      "title": "Planform point verbs",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-planform-point-verbs"
    },
    {
      "id": "surface-mockups-property-grid-cells",
      "path": "docs/mockups/property-grid-cells.html",
      "title": "Property sheet — three cell layouts",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-property-grid-cells"
    },
    {
      "id": "surface-mockups-solver-setup",
      "path": "docs/mockups/solver-setup.html",
      "title": "Solver setup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-solver-setup"
    },
    {
      "id": "surface-mockups-status-bar",
      "path": "docs/mockups/status-bar.html",
      "title": "Status bar — where reports go",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-status-bar"
    }
  ],
  "graphSha256": "0b1eb35a841718a6bd12e28ccb87548de1373ee797d93b28f4e1ba2ff2e0118e"
};
