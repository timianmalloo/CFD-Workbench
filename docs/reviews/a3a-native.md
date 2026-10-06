---
id: review-a3a-native
title: A3a native build against the approved Area 3 mockup (AUX)
type: doc
status: in-review
owner: "@trk-aux"
phase: implementation
tags: [a3a, aux, native-ui, review, analysis, mockup-parity]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: mockup-area3-analysis, rel: refines }
  - { to: design-language, rel: depends-on }
  - { to: proof-a3a-pack, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Nine mockup screens compared with the A3a build, with captures, differences and severity; the hydrodynamicist's
  re-review and the test architect's build-time veto folded in; strings with no approved copy row; what AUX fixed and
  what is listed.
---

# A3a native build against the approved mockup

Build under review: branch `feature/a3a-aux` from main `75ebb7c6`, plus the AUX polish named in section 4.
Mockup: `docs/mockups/area3-analysis.html` (nine screens, 1280 x 800).

## 1. How the captures were made (and what could not be)

- The packaged app was built from this tree (`dotnet publish ... -r osx-arm64 --self-contained false`, then
  `tools/package-application.py --platform macos`) and launched. `swift tools/native-windows.swift <pid>` found its
  window and `screencapture -l <id>` captured it (start screen shown; the file is not kept, it only proved capture works).
- **Driving the packaged app by input failed.** This session has no Accessibility permission: System Events cannot see
  the window ("Invalid index") and synthetic CGEvent clicks reached nothing. Nothing in the packaged app was clicked.
- So every state was captured by the fallback the brief allows: a scratch harness (outside the repo, in the session
  scratchpad `aux/cap/`) builds the real `ShellHost` in a real Avalonia window with the real `WorkbenchController` and the
  product `ProductWingMethod` (default lattice), opens the Example foil, drives the controller through each state, and
  renders the window to PNG with `RenderTargetBitmap`. The pixels are the shipped controls and styles, not a mockup.
  They are not the packaged binary's own window chrome (no title bar).
- The conditions band was filled through its real text boxes; Evaluate was called on the controller, as the band's event
  does. The "Running" state holds the solve on a gate. "Failed" throws from the solve. "Historical" is a real Span edit in
  CAD followed by a return to Analysis.
- Captures are 1500 x 880 (all four views fit) unless the file says 1280. Files: `docs/reviews/a3a-native/`.

## 2. The nine screens

| # | Mockup screen | Capture | Verdict |
|---|---|---|---|
| 1 | Analysis entered, no result, depth not set | `01-analysis-entered-no-result-1500.png`, `-1280.png` | **differs** (minor) |
| 2 | Evaluating, a prior run kept as Historical | `02-running-1500.png` | **differs** (minor) |
| 3 | Wing result, Current: layers, labels, omissions | `03-current-1500.png`, `03-current-1280.png` | **differs** (major at 1280, minor at 1500) |
| 3b | Wing result with depth unset | `03b-current-depth-unset-1500.png` | **match** with AUX-F7 |
| 4 | Station selected: strip result, own verdict, section Unavailable, Loads | `04-station-loads-1500.png`, `04c-tip-station-1500.png` | **match** (tip strip reads "Not judged — tip strip") |
| 5 | Historical after a CAD edit, draft hidden, Provenance | `05-historical-provenance-1500.png` | **differs**: Historical banner and layers captured; **draft hidden banner not captured** |
| 6 | Evaluation failed, previous kept as Historical | `06-failed-1500.png` | **match** |
| 7 | Outside the method envelope (alpha 12) | `07-envelope-alpha12-1500.png` | **match** (dashed outline plus count; run verdict truncated, AUX-F8) |
| 8 | A stored run failed its check (Tampered) | none | **missing** (no way to reach it through the controller; test gap, AUX-T2) |

CAD reference for the same window: `00-cad-four-1500.png`.

## 3. Differences, severity and disposition

Severity: Major = the operator would stop to ask; Minor = visible, does not mislead; Nit.

| Id | Where | Mockup | Build | Sev | Disposition |
|---|---|---|---|---|---|
| AUX-F1 | whole shell at 1280 x 800 | Plan + 3D visible with layers | **Four views falls back to One view in Analysis** (`ModelArea.EffectiveLayout`: a view under 320 x 240 after the 40 px band and the bottom panel). Only Plan shows; 3D, Side, Front layers need about 1500 x 870, or the Views menu picker. Plan + 3D at 1280 x 800 was not tried. | Major (demo) | Listed. Demo script says to use a window of at least 1500 x 870. Needs an operator ruling: shrink the bottom panel, or accept. |
| AUX-F2 | Properties, Wing result | Wing result group has room; Labels group (tier, basis, omissions) visible | The Wing result group scrolls inside the upper half of the pane; the Wing block keeps the lower half. Labels, Depth basis and Not modelled are below the fold in every capture (the hydrodynamicist could not see them rendered). | Major | Listed (PNA ownership). Demo: scroll the pane. |
| AUX-F3 | 3D view overlays | root-moment label and legend plate clear of the axes plate | Legend plate overlaps the axes plate; "Root bending moment about the root plane; pos..." and the N/m legend line clip at the right edge. | Minor | Listed (LAY `View3dLoadLayer`). |
| AUX-F4 | Spanwise loading chart | y axis Cl·c/c̄ with ticks, x axis "η (root → tip)" with ticks, series label "VLM + strip" | Single top y tick (clipped at 1280), x axis shows only "0", "η", "1"; no series or dashed-reference label (the caption names the dashed line). | Minor | Listed (`LoadingChart`). |
| AUX-F5 | Plan legend | batlow colour ramp with 0 and max ends | One text plate "Γ per strip · batlow 1.0 · 0–<max> m²/s · run <key>"; no ramp. Colour is never the only carrier (text names map and range). | Minor | Listed (LAY). DESIGN.md row says "plate", not "ramp". |
| AUX-F6 | Tier chip | pill chip above Wing result | Plain row "Tier  VLM + strip · local calculation" | Nit | Listed. |
| AUX-F7 | Conditions band | Speed in kn (10.00), Re_ref "5.19 × 10⁵" | Speed in m/s (5.14 = 10 kn); Re_ref as "5.187E+5" (the Properties format, one definition) | Nit | Re_ref fixed by AUX (was "518668.012"). Unit is a spec/operator choice, listed. |
| AUX-F8 | Properties, Envelope row | full sentence wraps | The run envelope sentence is cut at the cell edge ("... ≤ 10°"); the full bound is there but not readable in the cell. | Minor | Listed (PNA). |
| AUX-F9 | Running with a failed attempt before it | error card absent while Evaluating | When the previous attempt failed, the error card and "Analysis failed" group stay visible while the next Evaluate runs (seen in the first capture order; not in the final one). | Minor | Listed. Clear the card on a new Evaluate. |
| AUX-F10 | Conditions band, tabs, Evaluate | primary Evaluate; compact tab strip; divider before derived cells | Before AUX: Evaluate plain, bottom tabs about 22 px type (tall strip), derived cells run together | Minor | **Fixed by AUX** (section 4). |
| AUX-F11 | Preview hidden banner, screen 5 | banner "Preview hidden — Apply or Cancel in CAD" | The harness opens no draft, so it is not captured. The control exists (`ModelArea.axaml:77`). | Minor | Listed; no test either (AUX-T2). |

## 4. What AUX changed (polish, tokens only)

- `ConditionsBand.axaml`: Evaluate takes the existing `modebar primary` look (outline while Running, set in `ShowRunState`);
  a hairline divider before the derived group; derived cells use tabular figures.
- `ConditionsBand.axaml.cs`: Re_ref uses `0.###E+0`, the same format as Properties.
- `AnalysisPanel.axaml`: bottom-panel tabs at `PropFontSize`, 24 px minimum height (the property-grid target, SC 2.5.8).
- `DESIGN.md` section 4: rows for Conditions band (replaces the pre-build row), Layer legend, Analysis bottom panel,
  Layers pane. `design-lint --strict` and `xaml-token-lint` are clean. No new tokens were needed, so `Styles.axaml` is unchanged.
- `tools/check-event-subscribers.py`: no allow-list entry became stale. `check-event-subscribers` reports 4 allowed, 0
  findings; `VertexSelected`, `VertexMoved` and `FocusedTargetChanged` still have no subscriber in `src/`, so none was removed.

## 5. Hydrofoil hydrodynamicist re-review (labels, envelope, depth forms, as built)

Verdict: **PASS-WITH-CONDITIONS, no blocker; veto cleared conditionally.** Method: read the code and the captures;
the Labels group was below the fold in every capture, so the depth form text is confirmed from code only (Inferred for the
rendered text).

1. **Major.** The depth-set basis reads "deep water" at any depth: `Labels.cs:25` (COPY-223) and `AnalysisProjection.cs:32`
   switch on whether h_ref is set, not on h/c. At h/c below 5 the Labels group prints "...deep water" beside the Depth
   basis row "free surface not modelled". The strings and the rule match the approved rows; the physics is the weak point.
   **Needs an operator ruling**: h/c below 5 selects COPY-224 and COPY-226, or the contradiction is recorded as accepted.
2. Minor. Design line 402 says "h/c < 5 at any station"; the code tests h_ref/c_ref only (`AnalysisProjection.cs:36-37`).
   The tip, with the lower local depth and the local chord, can be shallow while the run shows no Depth basis row.
3. Minor. The run envelope sentence prints the full bound, but the cell truncates it (AUX-F8) and the run sentence has no ">".
4. Minor. COPY-217 begins "Verified fixture family" (approved, Ruling 82). It describes the fixture family, not a V&V
   rung. Confirm the Lab01 lint allows this exact string, or reword to "Fixture family tested". Not run here.
5. Nit. No capture shows a tapered wing, so Re_local with the local chord (BC-4) is seen only in code and in
   `Strip_ReLocal_UsesLocalChord`.

Checks that passed (Verified in source): fixed COPY-223/224/225/226 strings; depth switch on set or unset; bound printed in
full; tip strip "Not judged — tip strip"; Re_local = V·c(y)/ν; Total drag Unavailable naming profile, junction, mast, wave,
spray; CL/CD never computed from CDi; "Structural: Not assessed" with COPY-227 and COPY-60; wing scope stated; no
"cavitation-free" or "ventilation-safe" text; no Verified V&V label can render.

Residual (reviewer): numbers (sigma, Re, CL) not re-derived independently; Lab01 lint and tests not run by the reviewer.

## 6. Test architect build-time veto (section 17)

Verdict: **PASS-WITH-CONDITIONS. Veto CLEARED-WITH-CONDITIONS; it stands on the Proof Pack until F1-F4 are closed or
listed.** The reviewer ran nothing; AUX ran the ring (section 7) after the review.

- Named tests: 139 of 139 ledger names exist in `tests/` (Verified by grep). Per-track defined counts: PRE 1, COR 2, STO 14,
  VLM 23, STP 13, SVC 27, PRJ 41, TGL 12, LAY 1, PNA 5. CTX, HIST, LIM and AUX own no ledger names.
- **T1 (Major, AUX closed).** The AUX deliverables did not exist at review time; this file, the captures and
  `docs/proof/a3a/proof-pack.md` now do.
- **T2 (Major, listed).** No test drives the Tampered-run view through the controller or projection and asserts "Unavailable
  — run payload failed its check" with no layers and the row kept (`RunStoreTests.cs:170,192` cover the store half;
  `AnalysisProjection.cs:28-30`, `WorkbenchController.cs:290` are untested). Nothing asserts `PreviewHiddenBanner`
  (`ModelArea.axaml.cs:409`); a mutant deleting that line stays green. Failing inputs are written in the finding; AUX owns no
  tests and the tracks are closed, so these go to the next UI track as red-first items.
- **T3 (Major, listed).** `Layers_Vectors_BodyFrameSignConventionLabelled` (`ProjectionTests.cs:238`) checks only the legend
  string at one alpha. The row needs alpha +3 and -3 degrees with the lift vector's z sign (the mutant: direction from |L|).
- **T4 (Major, listed).** BC-1 mutant receipts are missing for the PRJ rows `Depth_*`, `Layers_Vectors_BodyFrame*`,
  `Loads_MissingTerm_UnavailableNeverZero`, `Loads_StructuralNotAssessed_ListComplete`, `Loads_AttachmentMoment*`,
  `Envelope_RunVerdict*`, `Station_StripReadout_*`. Only `docs/proof/a3a-prj2/proof-pack.md` exists for PRJ.
- Minor: `Depth_StationPiercing_EstimatorUnavailableFlagOnly` asserts one cell (second station not asserted); the moment-arc
  test does not check the datum name or N·m unit; `Toggle_NeverEvaluates` has no run seeded; no injected-300 ms input for the
  p95 row; BC-2 and BC-3 have no explicit receipts (F-2 band value inside [0.4156, 0.4198] not found in the logs read).
- Residual (reviewer): Windows parity; DR-ANA-14 bounds Inferred; `Toggle_LayersFirstFrame` timing varies; Historical-run glyph
  anchors use current geometry (LAY pack).

## 7. Strings in the app with no approved copy row

Rows approved: DESIGN.md section 7 COPY-206 to COPY-249 and the earlier rows (COPY-42..72). A string below was not found in
DESIGN.md section 7. I did not check COPY-1..205 for every UI label; row and column labels (Tier, Envelope, e (computed),
Basis) are treated as structure, not claims. **Not invented, not edited, listed for the operator.**

| String | Where | Note |
|---|---|---|
| "Unavailable" (bare, missing verdict) | `AnalysisProjection.cs:17,173,202` | carries a `simplify:` comment |
| "; <n> strips: Unavailable", "; <n> Not judged — tip strip" | `AnalysisProjection.cs:150` | run-sentence suffixes |
| FeedRevisionNotHeld reason | `Labels.cs:39` | `simplify:` working string |
| "Inside/Outside the method envelope at this strip (...)" and the run-sentence frames | `MethodRecord.cs:44-45,140-148` | extend COPY-221/222 with the bound (the design requires the bound) |
| "Unavailable — total drag missing" (CL/CD); the total-drag reason | `AnalysisProjection.cs:15`, `Loads.cs:9` | cited in design prose only |
| "Unavailable — no attachment point named (DR-ANA-5)" | `Loads.cs:11` | |
| "Analysis: Current / Historical / Unavailable ..." status items | `AnalysisProjection.cs:29,68`, status strip | V2 strip item, no row |
| Cavitation reasons ("Unavailable — water is invalid", "— local station is surface piercing", "— vapour pressure missing") | `Cavitation.cs:35-45` | |
| "Unavailable — surface piercing" (tip depth), "Undefined — speed ≤ 0" | `AnalysisProjection.cs:96`, `ConditionsBand.axaml.cs` | COPY-45 covers "depth not set" only |
| "tip depth <v> m", "free surface and tip depth, values in the conditions table" | `View3d.LoadLayer.cs:30` | |
| "dashed outline: <n> strips outside the method envelope" | `PlanLoadLayer.cs:37` | DESIGN.md Layer legend row quotes it as built |
| "Historical — previous result" | `AnalysisProjection.cs:38` | COPY-64 is "Historical — <what changed>"; this instance has no row |
| "Preview hidden — Apply or Cancel in CAD" | `ModelArea.axaml:79` | in the design (C2); not in DESIGN.md section 7 |
| "Outside strips have dashed outlines and a text count." | `AnalysisProjection.cs:251` | |

## 8. Residuals and open decisions for the operator

1. AUX-F1: four views fall back to One view at 1280 x 800 in Analysis.
2. Reviewer finding H1: "deep water" label at h/c below 5 (copy ruling).
3. Section 7 strings: approve, reword or retire.
4. Test gaps T2-T4 go to the next UI track.
5. Capture limit: packaged-app input needs Accessibility permission; the nine screens are in-process renders, not the binary's
   own window. Re-check the same states by hand in the packaged app (demo script in the Proof Pack).
