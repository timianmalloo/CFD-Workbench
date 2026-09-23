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
    "index"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
        }
      ],
      "summary": "Re-decides the knowledge base's degree-5 reading for the five master (distribution) curves: the record's default is a degree-3 clamped B-spline with seven control vertices (six to ten), the degree is stored per curve, and section curves stay degree 5. Decided on a measured fixture (fairness, anchor residual, support, lever effect) over the five example curves at both degrees, and on the loft spike showing the surface's spanwise continuity is the kernel's, measured, not the master curve's.",
      "tags": [
        "geometry",
        "b-spline",
        "degree",
        "control-vertex",
        "adr"
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
        }
      ],
      "diagrams": [],
      "sourceSha256": "8424795f2e4f93b339c1f09df4eb9966d470d08991c428044f20afabb62690be"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "5fc9f94d0dee100cdebbeb391a5e0331254ac32019516a5500904deeb0b0c76c"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "0422a9e44ee6368fe267e23d7d8b0565cf8884f2906efc423911459a97fa2cee"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "12c1eaedb0441a751fe7d6662e96ed5137b908e499370ceb0bff8332ad34c4a5"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "30b00b57ded67222f1a865a406aaaf716b8bb4a6a3dd5f4f2dcd1a257b806dc8"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
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
      "sourceSha256": "634e5be4e94ea325c6a21cd9bfe87e082386db9564beac45d9a584862106dc1a"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "f7f61ece5a53f819c3da518ac2e29ae41bd2aa28c2717e32a5b096d8e8f05ca0"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "bbbe525f2c6c1dd751e140e3f52e3965f731712631e91b5b2c4fb2b1b8d55bec"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "6dd4255a7413ab14e1e230a35cafd87076ee302eb78f2fe59f5d0ecca158a007"
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
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-20",
          "reason": "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table."
        },
        {
          "by": "mockup-workbench-v2",
          "on": "2026-09-20",
          "reason": "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact."
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
      "sourceSha256": "5c9fca11728546c0fd2ec5a4f67bd609e5e669dc564f29d1f405573614456463"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "a0497aba81c724c9def09e07ce847aae730a1d5e1977c51647b84bfaad562acd"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "9b1ffbd9e99ff22ca37a14147836b4f1c32cda41835874392bfd71f3cab807eb"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "3deacf0f9830a514900ce967928790c7cc0876d95b688cf9c90773359543f4f8"
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
          "on": "2026-09-20",
          "reason": "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table."
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
      "sourceSha256": "292a55b70408d461c973819ba15ffab3d2708f967407c4fc6fecd408fe7efcaf"
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
          "on": "2026-09-20",
          "reason": "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table."
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
      "sourceSha256": "625839c24e5965b987030b1dffda9c39c7f31c597b2057d86ce30b7d2a4a1766"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "411ab0d450324c252130bcb522410f64e718e7b724d0493a633eb08672842c54"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "c05f28a260ea27f96a207ac19f95f7253f5cb9074dff969ebf0c14c2eda003fd"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "ca8c3e283ffbe77fa75d1593a717c5fd596061ceba5a4ffaf2f61d65e5e28c95"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
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
      "sourceSha256": "7b26b4c0919d052a94bf22c9f044e2f6121ea67834770950b7cc26fd15b50c73"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "8a69caeb0078c85f4d136cb6bb863ace3c8a5e67cde7d9e77fb8faf35f513b3b"
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
      "sourceSha256": "916fe9a7969b8668446e7f8d3f5dc3cd399875a01ae567f2a9e0ed86afb2d012"
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
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
        },
        {
          "by": "mockup-workbench-v1",
          "on": "2026-09-20",
          "reason": "Mockup v1 built against spec v1 and cleared by the UX & Accessibility lens 2026-09-20; supersedes the 2026-09-19 prototype as the review artifact."
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
      "sourceSha256": "29a976925b053206f9750823eab34e2f9b57a17657f083227dbb27ae1a3d9420"
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
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
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
      "sourceSha256": "11139336edb00987356b4e053452a1aae2ce72b8fb079a210f484e79967edc4b"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "d200118e5d5e5665823b3da56a471362087a0a34f4630f350e2ce2fe6147991a"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
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
      "sourceSha256": "5f36aa3e3bdece229e8019c66a45c8f74589242d3a9b2225a5617c27954ed07a"
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
          "on": "2026-09-20",
          "reason": "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table."
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
      "sourceSha256": "cfe6b712cb87c965447917043bca109cfbd96d28d8ac18ae92f535562919830a"
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
          "by": "spec-cfd-workbench-v1",
          "on": "2026-09-20",
          "reason": "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table."
        },
        {
          "by": "mockup-workbench-v2",
          "on": "2026-09-20",
          "reason": "Mockup v2 (seven areas) cleared by the UX & Accessibility lens 2026-09-21; supersedes v1 as the review artifact."
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
      "sourceSha256": "37e6a766999b27e62db74e5bb7daba92f8208419c433f5e0649e2c62476902ff"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "3883f3cc2b8cbdc06dd2f16596befe6a3c566a9aad3d4af348c950e7620b3b32"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "1ae4a8040535683cd008337fa94b61e23e939bcfb901e795df67fd92565834f7"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "5ebdd6015332c1dcd86b2d01dc46b67e1e42de985e82dcb9af43f4016d1eb98c"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "e9d1bad471fecefcf6f13f37492b4d1ad1f90dd6e959aea1becda47190b72491"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
        },
        {
          "by": "spec-foildsl",
          "on": "2026-09-22",
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
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
      "sourceSha256": "e1659c24554411bf1f8c4618abd6755905ca5898e0839b1eaa66360f4da1f820"
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
          "on": "2026-09-20",
          "reason": "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)."
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
      "sourceSha256": "bee8e6e46887d9583a7161be28e3d9ad3151f1c7a60f84264531a47e300840f4"
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
      "sourceSha256": "590f0fef9ab4b7e53f2292b69dbec7ec92408122ae2cd99f3d2a0e030974da68"
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
      "title": "CFD-Workbench — product specification v1.4 (FoilDSL authoring)",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "specification",
      "reviewBy": "2027-03-19",
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
          "reason": "New normative FoilDSL 4.0 contract is ready for human review; compare dependent examples, source UI and persistence decisions."
        }
      ],
      "summary": "The specification the product is built against. Seven discrete, complementary areas — Setup · CAD · Analysis · Experiment setup · Run · Results · Export — each with an AI prompt entry whose output is a typed, validated, previewed proposal. One explicit parametric definition whose payload reproduces its surface; an operating point that carries depth, water and a goal state; analysis tiers that may claim only what their fixtures earn; a catalog admitted by rights class; a sweep-or-optimize experiment driven end to end against OpenFOAM or SU2 with evidence by files; results as sequences of admitted samples with named bases; hard states and fixed copy for every honest limit; 131 falsifiable acceptance criteria (87 stories, 22 UX, 22 UI) traced to the knowledge base. Revision 1.4 adds FoilDSL as the canonical authored foil/section language and the source/CAD transaction contract.",
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
          "title": "B9. FoilDSL authoring flow (SRC-01–10)",
          "mermaid": "flowchart TD\nA[Accepted foil and source] --> B{Edit route}\nB -->|Visual| C[Shared geometry draft and source patch]\nB -->|FoilDSL| D[Editable source draft]\nB -->|Open or New| D\nC --> E[Validate candidate and base revision]\nD --> E\nE -->|Invalid or incomplete| F[Diagnostic with location and repair; accepted shape retained]\nF -->|Edit again| D\nF -->|Cancel| A\nE -->|Unsupported| G[Explain unsupported feature or migration requirement]\nG -->|Cancel or keep original| A\nE -->|Valid| H[Labelled candidate preview and change summary]\nH -->|Apply| I[Append accepted source and semantic revision if changed]\nH -->|Cancel| A\nI --> J[Geometry and text projections agree; run freshness recomputed]\nJ -->|Undo or Redo| K[Select matching historical source and definition]\nK --> A\nJ -->|Save| L[Write project or explicit shape-only source]\nL -->|Failure| M[Previous file intact; retry or save elsewhere]\nM --> L\nL -->|Reopen and validate| A"
        }
      ],
      "sourceSha256": "38eb00b93c901bbe03999cc748aab2b0b20a0a8c12605ba8233c4c5088e21f7b"
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
          "reason": "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts."
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
      "sourceSha256": "b7ebafb9f389995fc01455fc7a99e679ce01ce80a7b7a9bbb0a84802077b6cf9"
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
      "id": "surface-specs-foildsl",
      "path": "docs/specs/foildsl.html",
      "title": "FoilDSL 4.0 — Language specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-foildsl"
    }
  ],
  "graphSha256": "3cb9bf6eac28524118e9a0fd578c5fbb992a82936356c4101a5bdb49b1999031"
};
