// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "CFD-Workbench",
  "generator": "docs-graph.py derive",
  "rootId": "audit-log",
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
      "sourceSha256": "d5b938fe1c26fb6f95365f76979eecc02674897d2de6d3c73831c50fa931027f"
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
      "summary": "Self-contained HTML workbench for iterating curves, station profiles and the shape-to-evidence workflow. The prototype includes review controls and illustrative scientific data; it is not a production geometry kernel or solver.",
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
      "sourceSha256": "5e83fa14dbda9c926456952fd5ab4a9ab711e3f4d41034581014bbdb02d3ebee"
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
        }
      ],
      "summary": "Words-first creative direction for the hydrofoil workbench. A bounded parametric canvas joins scalar span distributions and station section anchors in one model, with precision editing and visible evidence limits.",
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
          "to": "design-language",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8f00e663d839f9d7eb32c661365ae2f9336e919c8370faa1df0fe399c4ec6a1a"
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
      "reviewBy": "2026-12-19",
      "reviewSuggested": [
        {
          "by": "spec-cfd-workbench",
          "on": "2026-09-19",
          "reason": "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision."
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
        }
      ],
      "diagrams": [],
      "sourceSha256": "1375d1db2fd7ddb2df4cd30796efee3f2e17e58a1f287a6d9f4d1b14ba0352f3"
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
      "summary": "Source map, precedence, evidence confidence, and conflicts carried from CFD-Bench into the Workbench specification. Source statements are distinguished from scientific validation and new product decisions.",
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
      "sourceSha256": "f77a67054ed548d8e06784e1fee15ef03da407f5241e5d05892e31a0ca8638d4"
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
      "sourceSha256": "bfb0649e82fa9d4adac6f1f258000ed078a3dc85dd0aabfabc118137944451d9"
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
      "sourceSha256": "4df8b7d52fbaccef952f7be418d5f1c80593aee37ea8fdea170d16cfdb037abc"
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
      "reviewSuggested": [],
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
      "sourceSha256": "cc5d2db3c4f8c382deb3cdf7b346d8434bb83369ecdd12d55cf3a274e2c9f12d"
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
        }
      ],
      "summary": "Full functional, UX and UI specification for a local Mac and Windows hydrofoil workbench. One parametric surface definition supports curves and station profiles, with traceable analysis, guided CFD, optional AI, and explicit scientific limits.",
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
          "title": "B3. Flow F2 — shape through curves and stations (GEO-01–12, CAT-01–03)",
          "mermaid": "flowchart TD\nA[Recipe or open native model] --> B[Evaluate one explicit surface]\nB --> C{Edit target}\nC -->|Distribution| D[Select curve anchor or handle]\nC -->|Station| E[Select plane / row / profile]\nD --> F[Drag, nudge or type exact value]\nE --> F\nF --> G{Recipe still linked?}\nG -->|Yes| H[Preview direct-parametric detachment]\nG -->|No| I[Preview same model]\nH --> I\nI --> J{Geometry valid?}\nJ -->|Yes, accept| K[New revision; dependent results historical]\nJ -->|No| L[Highlight location and failed constraint]\nL -->|Correct| F\nL -->|Revert| B\nK -->|Undo| B\nK --> B\nC -->|Catalog profile edit| M[Make editable copy; show fit error]\nM -->|Accept| F\nM -->|Cancel| B\nQ[Add profile from DAT] --> R{Parse and validate}\nR -->|Invalid| T[Show line or shape error; choose another file]\nT --> Q\nR -->|Valid| U[Source, normalization and profile preview]\nU -->|Accept| E\nU -->|Cancel| B\nV[Break symmetry preview / read-only ghost overlay] --> B"
        },
        {
          "kind": "flowchart",
          "title": "B4. Flow F3 — analyze and compare (ANA-01–16)",
          "mermaid": "flowchart TD\nA[Choose Section or Wing] --> B[Set operating point and method]\nB --> C{Data and envelope supported?}\nC -->|No| D[Explain: choose supported point / install backend]\nD --> B\nC -->|Yes| E[Compute against pinned revision]\nE --> F{Outcome}\nF -->|Success| G[Evidence with scope, units, uncertainty]\nF -->|Failed| H[Keep historical result; inspect reason / retry]\nH --> B\nG --> I[Compare compatible snapshots or sweep]\nI -->|Missing sample| J[Gap plus reason; retry sample]\nJ --> E\nG -->|Geometry edited| K[Historical banner; recompute current]\nK --> E\nI -->|Methods disagree| L[Side-by-side assumptions and discrepancy]"
        },
        {
          "kind": "flowchart",
          "title": "B5. Flow F4 — setup, simulation and results (CFD-01–04, VIZ-01–02)",
          "mermaid": "flowchart TD\nA[Open Simulate] --> B{Compatible backend ready?}\nB -->|No| C[Detect / choose supported setup]\nC --> D[Review download, disk, elevation and actions]\nD -->|Decline| E[Return to design]\nD -->|Approve| F[Install stages and smoke test]\nF -->|Interrupted / offline / denied| G[Explain stage; resume / repair / cancel]\nG --> C\nF -->|Pass| H[Pin version]\nB -->|Yes| I[Case setup and estimate]\nH --> I\nI --> J{Mesh gate passes?}\nJ -->|No| K[Show metrics; repair mesh]\nK --> I\nJ -->|Yes| L[Run: residuals, forces, elapsed]\nL -->|Cancel / crash| M[Retain case and partial evidence; retry eligible stage]\nM --> I\nL -->|Required outputs verified| N[Results snapshot]\nN --> O[Choose field / slice / streamline / probe]\nO -->|Missing variable| P[Unavailable with reason; choose supported field]\nP --> O"
        },
        {
          "kind": "flowchart",
          "title": "B6. Flow F5 — optional language and export (AI-01–06, EXP-01–02)",
          "mermaid": "flowchart TD\nA[Contextual assistant] --> B{Key and consent present?}\nB -->|No| C[Explain optional setup; manual path remains]\nB -->|Yes| D[Inspect sharing summary; submit]\nD --> E{Response valid and supported?}\nE -->|No| F[Unsupported / error; edit request or dismiss]\nE -->|Starting design| G[Validated fields, shape preview and explicit diff]\nG -->|Edit| G\nG -->|Discard| H[Document unchanged]\nG -->|Final Accept; base revision unchanged| I[One undoable revision]\nG -->|Base revision changed| R[Refresh preview and review changes]\nR --> G\nE -->|Grounded explanation| J[Citations to selected run]\nE -->|What-if| K[Offer copy and actual recomputation]\nX[Current design: global Export, no AI required] --> L[Choose export format and tolerance]\nI --> X\nL --> M{Format and geometry checks pass?}\nM -->|No| N[Explain failure; return to geometry]\nM -->|Yes| O{Write export}\nO -->|Success| S[Export with revision and limitations]\nO -->|Denied / disk full| T[Preserve existing file; choose path or retry]\nT --> L"
        }
      ],
      "sourceSha256": "f69da574650aacb108dfa2e849d7088cd0b2b350af242fd45b9c936bba4d89b6"
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
      "id": "surface-specs-cfd-workbench",
      "path": "docs/specs/cfd-workbench.html",
      "title": "CFD-Workbench — Product specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-cfd-workbench"
    }
  ],
  "graphSha256": "1ae6ec80456f003e2979b428e1fa6065739024b9a5f905d0d5753a046c489aa9"
};
