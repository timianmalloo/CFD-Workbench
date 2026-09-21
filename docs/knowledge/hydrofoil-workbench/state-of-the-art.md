---
id: kb-hw-state-of-the-art
title: "State of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, generated, state-of-the-art]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
review-by: 2026-12-19
summary: >-
  Current best practice, leading techniques and the research frontier for every area of hydrofoil modeling, analysis, simulation, optimization and workbench design, compiled from the area files.
---

# State of the art

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

## CAD programs and their UI/UX paradigms for a precision parametric foil editor

*Source: [01-cad-programs-and-ux.md](01-cad-programs-and-ux.md) (`kb-hw-cad-programs-and-ux`).*

#### 1. Interaction paradigms and what each buys a foil editor

**History-based parametric feature modelling (SolidWorks, Inventor, Fusion, Onshape, FreeCAD Part Design).** The model is an ordered list of features (sketch, extrude, loft, fillet) replayed from a tree; a change anywhere upstream regenerates everything downstream. The 2025 novice study on SolidWorks records the costs: beginners confuse "in or out of sketch" state, cannot tell which tab they are in, and do not distinguish construction geometry from centrelines or dimensions from measurements; "Lack of feedback about users' conditions (e.g., keyboard shortcuts) leads to mistakes and user confusion" *(Verified, [S50])*. For a foil whose only "features" are five distribution curves and a set of stations lofted between them, a generic tree is strictly more ceremony than the domain needs; the spec already replaces it with a domain history ("domain station/curve history replaces generic sketch/extrude/fillet history", C1) *(Verified, spec)*. What the tree paradigm does well and we must keep: every parameter is nameable, typed and re-editable, and edits regenerate the derived surface rather than mutating it *(Inferred)*.

**Direct NURBS surface modelling (Rhino, Alias, MoI, Plasticity).** No tree by default; curves and surfaces are first-class objects edited through control points. Rhino's documentation defines the two point types precisely: "Control points are coefficients of NURBS basis functions", they "do not have to be on the curve or surface", and "Edit points are displayed on the curve evaluated at knot averages"; "moving one edit point generally changes the shape of the entire curve (moving one control point only changes the shape of the curve in a sub region)" *(Verified, [S1])*. Alias formalises the CV count: "the number of CVs in a curve or surface direction = degree + number of spans", degree 3 and above can be shaped in three dimensions, maximum degree 9 *(Verified, [S14])*. This is the paradigm closest to the foil editor's curve layer, but with unconstrained freedom: nothing in Rhino stops the user producing a non-foil. Rhino adds opt-in history: "The History command stores the connection between a command's input geometry and the result so that when the input geometry changes, the result updates accordingly"; "With History recording and Update turned on, a lofted surface can be changed by editing the input curves"; the status-bar pane records history for one command unless "Always Record History" is enabled *(Verified, [S5])*. The foil editor is that arrangement made permanent and narrow: five curves and stations are always the parents; the loft is always the child.

**Subdivision surfaces (Rhino SubD, Blender, Fusion Form/T-splines).** A coarse control cage is smoothed by a subdivision rule; "moving a vertex changes the cage, which changes the resulting smooth surface" *(Verified, [S12])*. Fusion's Form environment offers "Box Display" (unsmoothed), "Smooth Display" and "Control Frame Display" ("a combination of the unsmoothed and smoothed versions") *(Verified, [S20])*. T-Splines objects imported into Rhino 7+ become SubD objects *(Verified, [S12])*. What is transferable is exactly the dual display of cage plus smooth result, which is the visual grammar the spec requires for Smooth mode ("The evaluated curve and its dashed control polygon remain visibly distinct", A4). What is not transferable is the topology freedom: SubD is for organic solids, not a lofted wing with a defined leading edge and trailing-edge corner.

**Relational / associative geometry (MultiSurf).** Mention only; the marine-tools area covers it. The repo already records its relevance and its interoperability trap (`bench-cad-ux`, Verified there).

**Visual programming (Grasshopper, Dynamo).** Node graphs on a canvas where outputs are wired to inputs; Dynamo (2011 beta) copied the Grasshopper model for Revit *(Verified, [S46], Wikipedia-level)*. This exposes the full construction history as an editable program, which is the right model for an *export* or automation layer and the wrong model for an interactive fairing loop: every drag becomes a graph re-solve and the user must learn a second language. The repo's earlier conclusion ("Not for v1") stands *(Inferred)*.

**Constraint-based 2D sketching (SolveSpace, Onshape sketch, Fusion sketch, FreeCAD Sketcher on planegcs).** Geometry plus a set of dimensional and geometric constraints solved numerically. SolveSpace documents the degrees-of-freedom accounting: an arc "has five degrees of freedom", the "dof" column "shows how mobile your design is", an overconstrained sketch "fails to solve", the background "is drawn in red" and the tool "calculates a list of constraints that could be removed to make the sketch consistent"; it also warns that "it's possible to have an error with fewer constraints than degrees of freedom, if those constraints overconstrain some DOF and leave others free" *(Verified, [S39])*. FreeCAD's planegcs is "based on numeric optimization methods, such as DogLeg, Levenberg-Marquardt, BFGS or SQP" and the repository ships a solver manual and a Sketcher lecture as PDFs *(Verified, [S37])*. Onshape and Fusion sketches include splines with per-point constraints; Onshape's variables (`#name`) may be used in a dimension "as a stand-alone value or as part of an equation", expressions are evaluated by FeatureScript and "the result must contain no units or units to the first power" *(Verified, [S25])*. For the foil editor the lesson is the *reporting* discipline, not the solver: the spec's hard constraints (endpoint, symmetry, dimension locks) are a small constraint system, and SolveSpace's explicit DOF count, red-background infeasibility and "which constraint to remove" list are the correct UX for GEO-13's "an infeasible preview is rejected" *(Inferred from [S39] and A4)*.

**Fit for "a bounded family of fair curves" (the foil's five distributions and section curves).** Ranked by fit, with the reason:

| Paradigm | Fit for foil distribution/section curves | Reason |
|---|---|---|
| Direct control-point / on-curve editing with a fixed narrow parent-child relation (Rhino curves + always-on History) | Best | Same object model as the foil; needs only the added domain constraints |
| Constraint-based sketch reporting (SolveSpace DOF/infeasibility UX) | Borrow the reporting | Lock feasibility must be explained, not just refused |
| SubD dual cage display | Borrow the display grammar | Control polygon vs evaluated curve |
| Feature tree | Poor | Generic tree over five curves adds mode confusion the novice study measures |
| Visual programming | Poor for interactive fairing; possible export target | Second language, graph re-solve per drag |

*(Inferred from the Verified items above.)*

#### 2. Precision input and direct manipulation conventions

**Numeric entry with units and expressions.** Blender's numeric input has a simple mode and an advanced mode: "In advanced mode you can additionally enter expressions and units", entered with `=` or NumpadAsterisk; "Units such as cm, ", deg, etc. are supported" and "Basic operations from Python (+, *, /, **, etc.) are available"; a preference "Default to Advanced Numeric Input" exists *(Verified, [S31])*. Onshape numeric fields accept expressions with variables (`#d1*2`) and unitless variables "assume the unit of the workspace" *(Verified, [S25])*. Convention to adopt: every numeric field parses `value [unit] [op value [unit]]`, resolves to the model unit, and echoes the resolved model-unit value beside the field so the user can see what was accepted *(Inferred)*.

**Nudging with modifier steps.** Rhino: "Alt + Arrow keys are used for the nudge keys", Alt+PageUp/PageDown nudge in Z, and Options > Modeling Aids > Nudge sets three distances: "with nudge keys alone, with Ctrl + Nudge key and with Shift + Nudge key" *(Verified, [S8])*. Shape3d also moves a selected control point with arrow keys with "accurate keyboard displacement steps" (already Verified in `bench-cad-ux`). The three-step convention (coarse / default / fine) is the expert expectation; the spec's two steps (1 mm, 0.1 mm) should become three (10 mm / 1 mm / 0.1 mm for outlines; 1 % / 0.1 % / 0.01 % chord for normalised sections; 1 / 0.1 / 0.01 degrees for twist) so that a modifier maps to an order of magnitude in each unit family *(Inferred from [S8] and A4)*.

**Gizmos and click-to-type.** Rhino's Gumball lets the user "drag the x, y, or z arrow to move the objects in the arrow direction, or click on the arrow handle to type the moving distance" *(Verified, [S8] page family)*. Onshape's View Cube arrows rotate 15 degrees on click, 90 with Shift, 5 with Ctrl *(Verified, [S26])*. The convention is: drag for approximate, click-the-same-handle for exact, modifier for step size. A 2D curve editor needs at most a planar two-axis handle on the selected anchor plus one tangent lever per side; a full 3D gizmo is unnecessary in the curve views and only needed for station selection in the 3D viewport *(Inferred)*.

**Tangent handles.** The Alias help defines the tangent grip: "the tangent arrow grip changes the magnitude of the tangent at the base point, creating a sharper or flatter curvature" (Verified in `bench-cad-ux`). Illustrator-style handles are linked (moving one rotates the other, G1 preserved) or split (a corner). The spec's "linked/split tangent direction/magnitude" (A4) and "Given a split handle, then any continuity break is marked" (GEO-05) reproduce this; the added obligation is that a split is always rendered as a break marker on the comb, because a G1 break at a station is hydrodynamically real *(Inferred; continuity vocabulary Verified in `bench-cad-ux`)*.

**Control-point versus fit-point splines.** Fusion's FAQ distinguishes fit-point splines (handles on on-curve points) from control-point splines (a visible control frame, curve constraints separate from frame constraints) *(Verified, [S18], already in repo)*. Onshape's tech tip on high-quality sketched curves and its Curve/Surface analysis (comb, zebra) exist for the same reason *(Verified, [S28])*. Rhino's definition above closes the loop: a fit/edit point is a global-influence handle; a control point is a local-influence handle. The spec's Smooth mode adds a third quantity, a positive influence weight, which is the rational-B-spline weight concept (NURBS weights pull the curve toward a control point without forcing it through) expressed as a user-facing number; Rhino exposes NURBS weights only through the `Weight` command and treats them as an expert-only edit *(Flagged: Rhino Weight command from recall, not opened this session)*.

**Degree and knot exposure.** Alias: "number of CVs = degree + number of spans"; maximum degree 9 *(Verified, [S14])*. Rhino's Rebuild "will be rebuilt with curves of a specified degree and specified number of control points, with the knot of the resulting curve more evenly spaced", and the docs advise "Turn on control points and curvature graph to see the details of the curve structure" *(Verified, [S3])*. Neither product exposes knot vectors for direct editing in the normal UI; both expose degree and point count as the rebuild parameters. Convention: show degree and control count as read-only facts in the inspector, offer a Rebuild/Refit action with degree and count as inputs and a reported maximum deviation, never expose the knot vector *(Inferred from [S3][S14])*. The spec's "Insertion/removal preserves degree-five representation where used and reports any change in shape" (A4) is consistent.

**Curvature combs, zebra, isophotes, Gaussian.** Onshape: "Curvature combs are evaluated at evenly spaced isolines, not necessarily at the control points, and are used for evaluating the resultant shape of a curve/surface up to Flow (G3) continuity"; the comb "is now dynamic and updates in real time on drag" *(Verified, [S28])*. Fusion: right-click a spline, "Toggle Curvature Display"; a Setup Curvature Display dialog adjusts "Density and Scale" *(Verified, [S19])*. Rhino Zebra: "If the stripes have kinks or jump sideways as they cross connections between surfaces, there's a discontinuity"; stripe direction, size and colour and the analysis-mesh density are settable *(Verified, [S6])*. Rhino's Fair page: "You can use the CurvatureGraph command to view the curvature hair while fairing" *(Verified, [S2])*. Convention: the comb is a first-class, persistent, scalable overlay with its own scale readout; zebra is a surface-level check applied to the loft, not the curves; a Gaussian/mean curvature false-colour map is a third, optional surface check. Onshape's forum carries a standing request to *quantify* the comb (numeric curvature readout on hover), which no comparable ships by default *(Flagged, forum-level, [S28] family)* — the spec's "curvature comb with scale" plus a hovered numeric value would exceed comparables.

**Fairing tools.** Rhino Fair (quoted in finding 6) and the RhinoCommon `Curve.Fair` method ("works best on degree 3 (cubic) curves, attempting to remove large curvature variations while limiting the geometry changes to be no more than the specified tolerance") *(Verified, [S2][S4])*. Alias' Golden Rules substitute discipline for a fairing operator: fewer CVs and single spans *(Verified, [S13])*. Convention: an automatic fair is defined by (tolerance, end condition) and reports achieved deviation; a manual fair is achieved by reducing controls. The spec's Smooth is the manual path with a preview; an automatic "Fair within tolerance" action is a candidate addition (see Design implications).

**Rebuild / refit.** Rhino Rebuild takes degree and point count and evens the knots *(Verified, [S3])*; the grounding register already requires that catalog-to-editable conversions report a residual. The convention that matters is that rebuild is *always* reported as a shape change with a maximum deviation, which A4 already demands ("Conversion changes and shape-edit changes are reported separately").

**History versus record history.** Covered in section 1: Rhino's opt-in History with parent/child linkage that survives cut/copy/paste and group moves *(Verified, [S5])*. For the foil the relation is fixed and always on; the UI obligation is to make the direction of dependency visible (curves and stations are parents; loft, area, span, aspect ratio are children), which the spec states as derive-never-store.

#### 3. Selection, viewport and navigation conventions

**Multi-view and named views.** Rhino's 4View "sets up a four-viewport workspace with first or third angle projection", and NamedView saves, restores and animates transitions between saved views *(Verified, [S9])*. Onshape's View Cube gives Top, Bottom, Front, Back, Left, Right and a trimetric corner; its help "does not document dedicated keyboard shortcuts for named views" *(Verified, [S26])*. Fusion has a similar cube and a keyboard reference *(Verified, [S64])*. For a foil, the four canonical views are plan (planform), front (anhedral/elevation), side (sweep/twist) and a station section; a four-view mode plus a single dominant view is the Shape3d and Rhino pattern (Shape3d already Verified in `bench-cad-ux`).

**Orbit, pan, zoom mappings.**

| Product | Rotate/orbit | Pan | Zoom | Presets / notes | Label |
|---|---|---|---|---|---|
| Rhino (Win, Mac) | Right-drag in perspective (right-drag pans in parallel views) | Shift+right-drag | Ctrl+right-drag; wheel | Middle-button alternative (MMB rotate, Shift+MMB pan, Ctrl+MMB zoom); Mac substitutes Cmd for Ctrl; trackpad gestures supported | Verified [S7][S10] |
| Onshape (web) | Right-drag; touchpad: two-finger click-drag on Mac | Middle-drag or Ctrl+right-drag | Wheel; pinch on touchpad | Presets: SOLIDWORKS, NX 10, Creo, AutoCAD; keyboard rotate arrows 15/90/5 degrees, Z/Shift+Z zoom, Ctrl+Shift+arrows pan | Verified [S26][S27] |
| Fusion (Win, Mac) | Shift+middle-drag (Fusion default) | Middle-drag | Wheel | Presets: Fusion, SolidWorks, Inventor, Tinkercad, Alias, PowerMill; reverse zoom; camera pivot; Mac gesture mode | Verified [S16] (default mapping Flagged: from the blog summary, not the preference dialog) |
| Blender | Middle-drag | Shift+middle-drag | Ctrl+middle-drag; wheel | "Emulate 3 Button Mouse" preference for laptops; numpad views | Flagged (Blender manual not opened this session) |

Three conclusions. First, the industry has converged on *mapping presets named after products*, not on a mapping; a new tool with no preset panel forces every expert to relearn *(Inferred from [S16][S27])*. Second, the Mac trackpad path is treated as a distinct input mode by both Rhino and Fusion (gesture options), so trackpad orbit/pan/zoom must be designed, not assumed *(Verified, [S10][S16])*. Third, the only documented keyboard-complete 3D navigation is Onshape's stepped rotation, and it is the cheapest way to satisfy GEO-10's keyboard-only clause *(Verified, [S26])*.

**Command lines, palettes and search.** Rhino: "To start a command, just start typing and your text will appear in the command field in the left sidebar" (Mac); "A command options dialog will appear that has all the command options that appear in the Rhino for Windows prompt line" *(Verified, [S10])*. Fusion: pressing S "brings up a shortcut dialog right next to the cursor" where the user can "simply start to search for a command", and commands can be pinned to it *(Verified, [S17])*. Blender's F3 menu search and Onshape's Alt+C are the same pattern *(Flagged: from recall, not opened this session)*. The novice study's finding that beginners cannot tell functions apart by icon (the French "Bossage/Base" prefix problem) is an argument for a text-searchable palette with plain-language names over an icon strip *(Verified finding, Inferred application, [S50])*.

**Inspector / property panel.** Fusion and Onshape place a feature dialog beside the viewport with OK/Cancel while the feature previews; Rhino uses a Properties panel and command-line options. The spec's "one selection inspector" (B7) that identifies "the authoritative parameter and distinguishes a hard lock from an influence weight" (B3) is the Fusion/Onshape dialog pattern with domain semantics added *(Inferred)*.

**Undo/redo transaction models.** Blender: a modal operator that returns CANCELLED creates no undo step; if an error occurs after some data has been modified "it is better to return FINISHED, unless it is possible to fully undo the changes before returning" *(Verified, [S32])*. Onshape has no traditional undo stack across sessions; its versioning and history replace it *(Flagged, from recall)*. Rhino's Undo is a command-level stack with multi-step UndoMultiple *(Flagged, from recall)*. Convention to adopt is Blender's: one accepted preview equals one undo step; a cancelled preview leaves no step; a failed apply must fully roll back before reporting, never leave a half-applied state. GEO-14 already states this.

**Preview-accept-cancel.** Blender: "The action of a modal operator can be confirmed using LMB or Return. To cancel a modal operator use RMB or Esc" *(Verified, [S32])*. The keyboard pair Return/Esc is the cross-product constant; the mouse pair differs (Blender RMB cancels, Rhino Mac RMB opens a context menu). Design rule: Return accepts, Esc cancels, and no mouse button has a hidden meaning *(Inferred from [S10][S32])*.

**Live derived readouts while dragging.** Onshape's comb updates on drag *(Verified, [S28])*; Shape3d shows width and rocker while dragging (Verified in `bench-cad-ux`). The spec's live area/span/t/c and L/D readouts during a curve drag extend this convention and are, by comparison with all comparables, the product's distinctive UX claim.

#### 4. Cross-platform desktop CAD on macOS and Windows

| Product | Windows | macOS | Built with | Evidence | Label |
|---|---|---|---|---|---|
| Rhino 8 | Yes | Yes, native macOS UI by default; optional "Rhino for Windows" theme | Native per-platform UI over one core | McNeel wiki: Cmd replaces Ctrl, sidebar command field, right-click context menu, trackpad gestures | Verified [S10] |
| Fusion | Yes | Yes | Qt (platform plugin errors are a documented failure mode on both) | Autodesk support article on the Qt platform plugin | Verified (Qt use) [S24] |
| Onshape | Browser | Browser | Web (WebGL) | Onshape help | Verified [S26] |
| Blender | Yes | Yes | Custom: GHOST windowing/event layer, OpenGL-drawn widgets | Blender developer docs | Verified [S33] |
| FreeCAD 1.x | Yes | Yes | Qt (Qt5/PySide2 stable; Qt6/PySide6 migration with open regressions) | FreeCAD issues #6992, #13303 | Verified (issues), Inferred (status) [S38] |
| Plasticity | Yes (10+) | Yes (12+, Intel and Apple silicon) | Parasolid kernel; UI framework not established this session | Wikipedia, CG Channel | Verified (platforms), Flagged (framework) [S44] |
| MoI 4 | Yes | Yes | Proprietary | Vendor purchase page | Flagged (page not opened) [S45] |
| Alias 2026 | Windows 11 | Not listed | Proprietary | Autodesk system requirements (fetch returned 403; search excerpt) | Flagged [S15] |
| SolidWorks | Yes | No | Proprietary (Win32) | Recall | Flagged |
| Avalonia (our candidate) | Yes | Yes | .NET; NativeMenu "builds the macOS application menu where macOS puts it, generates the Quit item" and "is ignored on platforms other than macOS"; NativeMenuBar shows the same items on Windows/Linux; Windows guide covers DPI scaling and multi-monitor Screens | Avalonia docs | Verified [S49] |

**OS conventions that matter for CAD.** Apple's HIG: "Prefer the Command key as the main modifier key in custom keyboard shortcuts", respect standard shortcuts because "people expect them to work regardless of the app", and use Shift as the secondary modifier for complementary shortcuts *(Verified, [S47])*. Rhino's Mac port applies exactly this substitution and documents that laptop users need fn for function keys and that a one-button mouse uses Control-click *(Verified, [S10])*. Windows: per-monitor DPI awareness v2 is the recommended mode; "Windows will not bitmap stretch the application when the DPI changes" but sends WM_DPICHANGED and "it is then the complete responsibility of the application to handle resizing itself" *(Verified, [S48])*. A CAD viewport that is bitmap-stretched on a DPI change renders blurred combs and wrong pick radii, so PMv2 is a floor. Trackpad: both Rhino and Fusion expose a gesture mode, which confirms that pinch-zoom and two-finger pan are expected on macOS and that a right-drag orbit needs a two-finger-click or modifier substitute *(Verified, [S10][S16])*.

**Accessibility: what exists, honestly.** Fusion's VPAT (Version 1.6, dated 2018-02-28, published under the 2019 folder; the most recent Fusion VPAT this session could open) marks 1194.21(a) keyboard access as "Supports with exceptions": "Text/value keyboard input is supported for most commands", "Not all commands are accessible via keyboard"; focus tracking as "Supports with exceptions": "The focus on Primitives cannot be operated correctly by clicking Tab"; forms for assistive technology as "Supports with exceptions": "Not support Parameters Form Edit"; colour coding as supported because "Object selection includes a mix of color, contrast and outline treatment" *(Verified, [S21]; currency Flagged: 2018 document)*. Autodesk maintains a Section 508 VPAT index per product *(Verified, [S22])*. Onshape's VPAT "is available upon request" via education customer success *(Verified, [S30])*. Blender's toolkit draws its own widgets over GHOST, so there is no native accessibility tree; a long-running developer-forum thread on accessibility exists, and the 2025 "Beyond Mouse & Keyboard" programme targets tablets, pens and touch, not assistive technology *(Verified for GHOST and the 2025 blog; Inferred for the absence of a screen-reader tree, [S33][S34][S35])*. Colour-blind palettes: no CAD comparable documents a colour-vision-deficiency mode for its analysis overlays in the pages opened this session; the visualization area (gap 15 in the register) owns the map choice. Conclusion: the state of the art in desktop CAD accessibility is "keyboard for most commands, focus for most controls, nothing verified for screen readers"; the spec's A7 target is above the market, and the cheapest proof is the one the spec already names (native accessibility tree names/roles/values on both OSes, UI-03).

#### 5. Onboarding, empty state and learnability

**First run in comparables.** No vendor page opened this session documents a first-run empty state as a designed artefact; the market comparisons that surfaced discuss install friction (Onshape: create an account and model "within two minutes"; Fusion: a multi-gigabyte installer) rather than the first screen *(Flagged, tertiary, [S46]-class blogs)*. The register's GAP-16 (empty state undesigned) therefore has no comparable to copy; the closest published pattern is Blender's splash with recent files and a quick-setup step on first launch *(Flagged, recall)*.

**What the literature establishes.**
- Lee-Remond, Sagot and Ostrosi (2025): three-stage framework (analyse UX with ethnography, questionnaire, card sorting; design; evaluate with eye-tracking). Beginner difficulties on SolidWorks after a 15-hour course, by frequency (n=46): Comprehension 16 ("Misunderstanding of text and icons"), Visibility 12 ("Knowing where the user is in the software, in which status, and at which stage"; "Conceptual distinguishment between the Sketch tab and the Solid tab"), Projection 3D 11, Global vision 5, Unfamiliar function 4 ("Construction of Bezier curves (vector curves)"), Forgivability 2, Dimension 2. Eye-tracking N=19; the redesigned interface improved Agreeable (Z = -3.739, p < 0.001), Understandable (Z = -3.624, p < 0.001), Consistent (Z = -3.314, p < 0.001), Status Visibility (Z = -2.812, p <= 0.005), Distinction (Z = -3.816, p < 0.001), Finding functions (Z = -3.508, p < 0.001), Layout (Z = -3.589, p < 0.001) on Wilcoxon signed-rank tests *(Verified, [S50])*.
- Allwood (1994): usability in CAD from a psychological perspective; novices "encounter special difficulties" and can be helped to see similarities between tasks *(Verified abstract-level, [S51])*.
- Bhavnani, John and Flemming (1999) and Bhavnani et al. (2008, TOCHI): inefficient strategies persist "despite many years of experience and formal training"; strategic knowledge "is neither defined nor explicitly taught"; teaching strategy helped students who lacked it and did not change those who had it *(Verified, [S52])*.
- Grossman and Fitzmaurice (2010) ToolClips: contextual text-plus-video tooltips; "users successfully completed 7 times as many unfamiliar tasks, in comparison to using a commercial professionally developed on-line help system", with retention one week later *(Verified, [S53])*.
- Matejka, Li, Grossman and Fitzmaurice (2009) CommunityCommands: item-based collaborative filtering "generates 2.1 times as many good suggestions as existing techniques" in a 3-month professional study; deployed to over 1,100 AutoCAD users over one year *(Verified, [S54])*.
- Usability evaluation of CAD/CAM is "very rare" as a research topic (Procedia CIRP 2015) *(Verified abstract-level, [S59])*.

**Error recovery.** The novice study names forgivability explicitly, and SolveSpace's "which constraint to remove" list is the best documented recovery UX for constraint failure *(Verified, [S39][S50])*. Blender's rule that a cancelled operator leaves no undo step is the corresponding transactional rule *(Verified, [S32])*.

#### 6. Few master curves versus many control points

**Professional rule.** Alias Golden Rule 2: "Use the minimum number of CVs", because "Each CV takes hard work to get into the right position. Fewer CVs means less work" and fewer CVs are "smoother – and this is normally something we want in our designs"; Golden Rule 3: "Use single-span for smooth curves"; "for Class A work, it is normal to limit surfaces to being single-span" *(Verified, [S13][S14])*.

**Theory.** Farin and Sapidis (1989): a curve is fair "if its curvature plot consists of relatively few monotone pieces"; later refinements add continuity of the plot. Two families of fairing measures exist: curvature-plot conditions where control points are adjusted for a continuous, suitably monotone plot, and energy functionals (bending plus tension) *(Verified, [S58])*. The connection to control count is direct: with n control points a degree-p B-spline has n - p spans, each able to host a curvature inflection; fewer spans bound the number of monotone curvature pieces from above *(Inferred from [S14][S58])*.

**HCI and CAGD on direct versus indirect manipulation.** Bartels and Beatty (1989) let the user "pick any point on the curve and move it to a new location", solved in real time, generalisable to constraints; Fowler and Bartels (1993) extended it to "first and second derivatives at any points" and to systems of constraints on composite curves *(Verified, [S55])*. Finkelstein and Salesin (1994) support "continuous levels of smoothing as well as direct manipulation of an arbitrary portion of the curve; the control points, as well as the discrete nature of the underlying hierarchical representation, can be hidden from the user", with no extra storage beyond the original control points *(Verified, [S56])*. Forsey and Bartels (1988) introduced hierarchical B-splines, "in which a single control point can be added without covering an entire row or column of control points", giving "local refinement" and "multi-resolution editing" *(Verified, [S57])*. A 2000 engineering-software paper generalises direct manipulation to NURBS curves including weights *(Verified abstract-level, [S62])*. The consistent reading across 35 years: users should manipulate the curve (position, tangent, curvature at chosen points) and the system should solve for the control net; exposing the control net is the expert's escape hatch, not the primary interface. This is exactly the spec's split: Through points (on-curve anchors and tangent levers, the Bartels-Fowler model) as the primary mode, Smooth (off-curve weighted controls) as the explicit, visible control-net mode.

**Degrees of freedom versus fairness (a design invariant).** For a planar curve with n control points, DOF = 2n minus the number of independent scalar constraints (fixed endpoints, tangent locks, symmetry, dimension locks). Every DOF left free is a place a user can introduce a curvature reversal; every extra control point adds two. A distribution curve that must be fair over a half-span with one or two intentional inflections (a chord curve with a tip taper; a twist curve with washout) needs on the order of 4-7 anchors, not 20; this matches the Shape3d practice already in the repo and Alias' single-span guidance *(Inferred from [S13][S58] and `bench-cad-ux`)*.

**Disconfirmation attempted.** The counter-position is that few controls limit expressiveness and that experienced users want dense control (e.g., a tip with a fast chord drop). The literature's answer is local refinement (Forsey-Bartels) at the place that needs it, not global density; the spec's "insertion/removal preserves degree-five representation and reports any change in shape" (A4) is the local-refinement path. The counter-position survives as a requirement that insertion be cheap and local, not as a reason to default to many controls *(Inferred)*.

## Parametric curves, lofts, splines and surfaces for foil profiles and wings

*Source: [02-parametric-curves-lofts-and-surfaces.md](02-parametric-curves-lofts-and-surfaces.md) (`kb-hw-parametric-curves-lofts-and-surfaces`).*

#### 1. Curve representations and continuity

**Bézier.** P(t)=Σ_{i=0}^{n} B_{i,n}(t)P_i, B_{i,n}=C(n,i)t^i(1−t)^{n−i}. Global support: moving any control point moves the whole curve. Degree = number of control points − 1, so local detail costs degree. Suitable as a *span* representation (one Bézier per B-spline span via `MakePiecewiseBezier`, [S17]) and as the exact form of CST/PARSEC after reparameterization (below), not as the editing model. *(Inferred [S1][S2])*

**B-spline.** C(u)=Σ_{i=0}^{n} N_{i,p}(u)P_i over a knot vector U=(u_0,…,u_m), m=n+p+1, basis by the Cox–de Boor recurrence. Properties that matter for an editor: local support (P_i affects [u_i,u_{i+p+1})), convex hull per span, variation diminishing (the curve wiggles no more than its polygon — a fairness guarantee the polygon makes visible), and continuity C^{p−s} at a knot of multiplicity s. Clamped ("open uniform" / end-knot multiplicity p+1) knot vectors make the curve start and end at the first/last control points with end tangent along the polygon — the affordance a foil editor wants at LE and TE. **Uniform vs non-uniform:** uniform knots make basis functions shifted copies of each other and equalise influence; non-uniform knots (from interpolation parameters, or after insertion) are unavoidable in Through-points mode and must be stored. **Degree 3 vs 5:** cubic gives C² (curvature-continuous) with the least control-point overhead; quintic gives C⁴ so that the curvature *derivative* is continuous too, which matters because the pressure distribution reacts to curvature and its gradient. Degree 5 needs at least 6 control points per curve and stiffens the curve (each control influences 6 spans). Even degrees are avoided for interpolation (Dierckx/scipy warn "even values of k should be avoided especially with small s", [S7]). *(Verified for the scipy warning [S7]; the rest Inferred from standard theory [S1][S3])*

**What "degree 5 gives G4 internally" precisely means.** Between knots a degree-5 B-spline is a polynomial (C^∞). At an interior knot of multiplicity 1 the spline is C⁴: position, first…fourth parametric derivatives match, which implies G⁴ wherever the curve is regular (|C'(u)|≠0). It **fails**: (i) at a knot of multiplicity s>1 — C^{5−s}, so multiplicity 5 is a C⁰ corner (this is how a *deliberate* G1 break, e.g. a sharp TE or a split handle, is represented inside one curve); (ii) at the ends — there is nothing to be continuous *with*; the LE junction of upper and lower curves, or the mirror plane of an outline, is a separate matching condition (positions, tangents, curvatures across two curves) that must be imposed and measured; (iii) where |C'|=0 (a cusp: the curve can be C⁴ parametrically yet G⁰ geometrically); (iv) after any *approximate* operation (knot removal, degree reduction, refitting) — continuity is then a property of the new curve and must be re-measured. *(Inferred [S1][S3]; agrees with the reconciliation register [S30])*

**C^k vs G^k.** C^k: derivatives up to order k agree with a common parameterization. G^k: agree after reparameterization (G1 = same unit tangent; G2 = same tangent and same curvature vector; G3 = same rate of change of curvature). C^k ⇒ G^k for regular curves; G^k ⇏ C^k. Rhino/Alias UI vocabulary is G0–G3 [S30]; openNURBS internally tests both C and G variants with separate point, derivative and curvature tolerances (defaults ON_ZERO_TOLERANCE for point/d1/d2 and ON_SQRT_EPSILON ≈ 1.49×10⁻⁸ for curvature) [S15]. *(Verified for openNURBS constants [S15]; definitions Inferred)*

**Curvature and the comb.** Planar: κ(u) = (x'y'' − y'x'')/(x'²+y'²)^{3/2}; 3D: κ = |C'×C''|/|C'|³. The comb draws C(u) + s·κ(u)·n(u) (n the unit normal, s a user scale); at a G1-only joint the teeth are parallel but differ in length; at G2 the comb is continuous; at G3 it is smooth [S30]. Continuity at a knot is *measured* by evaluating one-sided derivatives from the left and right spans (de Boor on each side) and comparing: ‖ΔC‖ (G0), the angle between unit tangents (G1), |Δκ| and the angle between curvature normals (G2), and |Δ(dκ/ds)| (G3), each against its own tolerance — exactly the openNURBS `IsContinuous`/`GetNextDiscontinuity` pattern [S15]. *(Inferred; constants Verified [S15])*

**NURBS and when weights matter.** C(u)=Σ N_{i,p}w_iP_i / Σ N_{i,p}w_i. Rational weights are required only to represent conics exactly (a circular arc is a rational degree-2 NURBS — `CreateFromArc`/`CreateFromCircle` in rhino3dm are rational degree 2 [S17]). Foil sections and span channels are not conics, so **the editor never needs rational weights for shape**; a weight does act as an attractor (executed: monotone approach, never reaching, [S29]) but it also distorts parameter speed and makes derivatives rational, and a weight of 0 or ∞ degenerates the curve. The spec's "influence weights" should therefore be data-fit weights (§2), not NURBS weights, with all w_i = 1 stored explicitly so an exporter can write a polynomial `B_SPLINE_CURVE_WITH_KNOTS`. *(Verified for the probe [S29]; recommendation Inferred)*

**Hermite / Catmull-Rom / Akima.** Cubic Hermite interpolates points and tangents per segment; Catmull-Rom chooses tangents from neighbours ((P_{i+1}−P_{i−1})/2 in the uniform form) and is C¹ only; the *centripetal* form (parameter increments ∝ ‖ΔP‖^{0.5}) avoids cusps and self-intersections (Lee 1989 [S24]; also the Catmull-Rom result of Yuksel et al. 2011, Flagged). Akima (1970) is a local C¹ interpolant designed to suppress overshoot; it is *not* curvature-continuous. All three are appropriate for lookup tables and UI easing, not as a G2+ geometry of record. *(Verified [S24]; Akima/Yuksel Flagged from recall)*

**Clothoids.** Curvature linear in arc length (Euler spiral); no polynomial form (Fresnel integrals). Piecewise-clothoid splines give G2 interpolation with monotone curvature between points and are the basis of Levien's "spiro" and MEC/MVC approximations [S13]. For this product they are a *reference* (the fairest interpolant to compare a spline against), not a storage format — CAD interchange has no clothoid entity. *(Flagged: [S13] was downloaded but not parsed this session)*

**Exactness of representation changes.** Knot insertion (Boehm): new control points are convex combinations α_i P_i + (1−α_i)P_{i−1} with α_i=(ū−u_i)/(u_{i+p}−u_i); the curve is unchanged to round-off — **executed: max deviation 6.9×10⁻¹⁷** [S29]. Degree elevation (Piegl & Tiller A5.9, Flagged) is exact. Knot removal is exact only when the curve is actually C^{p−s+1} there; otherwise it is an approximation with a computable bound (A5.8 uses a tolerance). Degree reduction is approximate. This is the mathematical reason GEO-05's "insertion preserves degree 5 and reports any change in shape" is satisfiable with a reported change of exactly zero for insertion, and a *measured* non-zero change for removal. *(Verified [S29]; algorithm citations Flagged [S1])*

#### 2. The two editing modes

##### (a) Through points — global interpolation with tangent handles

Given anchors Q_0…Q_n, choose parameters ū_k (chord length: ū_k = ū_{k−1} + |Q_k−Q_{k−1}|/L; centripetal: same with the square root of the chord [S24]); choose knots by averaging u_{j+p} = (1/p)Σ_{i=j}^{j+p−1}ū_i (Piegl & Tiller Eq. 9.8, Flagged) so the Schoenberg–Whitney condition holds and the collocation matrix N_{i,p}(ū_k) is totally positive, banded and non-singular; solve N·P = Q. For degree 3 with n+1 anchors two more conditions are needed (end tangents D_0, D_n — added as extra control points P_1 = P_0 + (u_{p+1}/p)D_0, symmetric at the end — A9.2, Flagged); for degree 5 four are needed (end tangents *and* end curvatures or second derivatives, or "natural"/not-a-knot style conditions). scipy's `splrep` default end condition is not-a-knot ("the first and second segment at a curve end are the same polynomial") [S7]. **Tangent handle semantics:** direction = unit tangent C'(ū_k)/|C'(ū_k)|; magnitude = |C'(ū_k)|, which is a *parametric* speed and so depends on the parameterization — the UI should display it relative to the local chord (Hermite "tension"), and a linked handle means one derivative constraint, a split handle means the anchor becomes a knot of multiplicity p (C⁰) with two independent one-sided tangents, which the comb must mark as a G1 break (GEO-05). Interior tangent constraints beyond the ends turn the problem into constrained interpolation (more unknowns than points): add one control point per extra condition or solve it as the constrained least-squares problem of §2(b) with zero-residual weights — the same solver serves both modes. *(Inferred [S1][S7][S24])*

Chord-length vs centripetal: for foil sections sampled with cosine spacing and for span channels with monotone abscissa, chord length is adequate and closer to arc length (useful when the handle magnitude is shown as a length); centripetal is safer when anchors are unevenly spaced or nearly collinear with a sharp turn (tip region, TE closure). Store the choice. *(Inferred [S24])*

##### (b) Smooth · weighted controls — which mathematics matches

| Formulation | Object | "Raise weight → attracts, never forces"? | Hard constraints | Linear? | Verdict |
|---|---|---|---|---|---|
| Plain B-spline control polygon | control points, fixed knots | Not applicable (no weight); moving a control moves the curve within p+1 spans | via knot multiplicity/end clamping only | evaluation only | The *display* object (dashed polygon); not by itself the weighted mode |
| NURBS rational weight w_i | homogeneous control point | **Yes** (executed: dist 0.080→0.006 for w 0.25→16, never 0) [S29] | none natively | evaluation rational | Rejected: distorts parameterization, breaks polynomial export, no constraint mechanism |
| Weighted least-squares B-spline (Dierckx `curfit` s=0 fixed knots; P&T A9.7 with weights) | min Σ_k w_k‖Q_k − C(ū_k)‖² over control points, knots fixed | **Yes** (executed: gap 0.0385→0 monotone as w 1→1000) [S29]; splrep squares w inside the sum [S7] | equality rows via KKT / null-space; exact interpolation of a point = w→∞ or a constraint row | **Yes** | **Recommended** |
| Smoothing spline (Reinsch/Wahba; de Boor `smooth`) | min Σ w_i|y_i − f(x_i)|² + λ∫f''² [S8] | **Yes** (executed: gap 0.0389→0.00018) [S29]; w linear (not squared) in `make_smoothing_spline` [S8] | as above; λ trades fairness vs fit; GCV can choose λ [S8] | Yes | Recommended **regulariser** for the LSQ form (adds fairness); function-of-x form only |
| Energy / MVC with point attraction | min ∫κ² or ∫(κ_s)² + Σ w_i‖C(u_i)−P_i‖² [S12] | Yes | Lagrange multipliers, nonlinear | No (Newton) | Reference/optional "fairest" mode; slower, possibly non-unique |
| Knot-multiplicity "influence" | multiplicity as a sharpness dial | No — it changes continuity class, not attraction | — | — | Not a weight; keep for deliberate breaks |

**Recommended Smooth formulation.** Unknown control points P ∈ ℝ^{(n+1)×d}; design matrix N (rows N_{i,p}(ū_k) at each control's *attachment parameter* ū_k, chosen as the Greville abscissa of that control or the parameter of the nearest curve point at the moment of creation); weights W = diag(w_k); a fairness term λ Fₚ where F is the Gram matrix of ∫(C''')² (degree-5 friendly proxy for curvature variation) or ∫(C'')² (bending energy). Solve

  minimise (NP − Q)ᵀW(NP − Q) + λ PᵀFP   subject to  A P = b,

where rows of A encode hard constraints that are **linear in P**: endpoint position (N(u_end)·P = Q_end), tangent direction and magnitude (N'(u)·P = D), curvature-vector match at a junction (N''(u)·P = K), exact channel value at a station (N(η_s)·P = c_s — GEO-03's "explicit curve-value constraint"), mirror symmetry (P_i − P_{n−i} reflected = 0), and closure. KKT system: [2(NᵀWN + λF)  Aᵀ; A  0][P; μ] = [2NᵀWQ; b]. Feasibility diagnostics: rank(A) < rows(A) with inconsistent b ⇒ "conflicting locks"; NᵀWN singular ⇒ "insufficient controls" (Schoenberg–Whitney violated) — both map to the spec's blocked-Apply cases. Positive-thickness and non-crossing are *inequalities*; they are checked after the solve and reported, or (later) handled by an active-set/QP step. Weight semantics to expose: w is relative (the solution is invariant to scaling all w together); w→∞ converges to interpolation; the spec's "weight 1→2 moves toward the control" (GEO-13) is satisfied because the objective is convex and the residual at that control is strictly decreasing in w when other controls are fixed (observed in the probe [S29]). *(Verified for the behaviour [S29][S7][S8]; formulation Inferred; reference texts [S31][S32] Flagged)*

Executed probe (Python, scipy 1.x under `uv run`, file `probe.py` in the session scratchpad — disposable per the Spike Protocol; results copied here):

```
Weighted LSQ B-spline (splrep, fixed knots, k=5): w=1 gap=0.0385 · w=2 0.0228 · w=4 0.0087 · w=8 0.0025 · w=16 0.00065 · w=64 4.1e-5 · w=1000 0.0
Smoothing spline (make_smoothing_spline, lam=1e-4): w=1 0.0389 · w=2 0.0318 · w=4 0.0234 · w=8 0.0152 · w=16 0.0090 · w=64 0.0026 · w=1000 0.00018
NURBS rational weight on P2 (deg 3): w=0.25 dist 0.080 · 0.5 0.067 · 1 0.050 · 2 0.033 · 4 0.020 · 16 0.006
Degree-5 knot insertion at u=0.37: max|after−before| = 6.9e-17; degree 5→5; 19→20 control points
```

*(Verified by execution [S29])*

**Section-specific handling.** A foil surface is not a function y(x) near the LE (vertical tangent), so the section curves must be *parametric* (x(u), y(u)) B-splines, or the fit must be done in a coordinate where the LE is regular: CST's ψ = t² trick makes x = t² and y polynomial in t for the LE-radius class [S4][S5]. Practical choice: fit upper and lower as one closed-at-LE parametric curve pair in the (x/c, y/c) plane with u∈[0,1] from TE-upper through LE to TE-lower (as XFOIL/Selig order the points), with the LE tangency constraint (vertical tangent at LE) as a hard row. Span channels are functions of η and can use the function form directly. *(Inferred)*

#### 3. Fairing

- **Farin & Sapidis (1989):** fairness = curvature plot with few monotone pieces; local fairing removes the knot whose removal most reduces a discontinuity in the curvature *derivative* and reinserts it, repeated until a tolerance is met [S11]. Cheap, local, deterministic — a good "Fair selected region" action for Through-points curves. *(Verified [S11])*
- **Energy methods:** MEC minimises ∫κ² ds (elastica — can be unstable with few constraints); **MVC** minimises ∫(dκ/ds)² ds and yields fairer, scale-invariant shapes whose minimisers include circles and helices [S12]. Both are nonlinear; discretise on the B-spline and use quasi-Newton. *(Verified [S12])*
- **Curvature-based/target-curvature fairing** prescribes a smooth target κ(s) and refits — the naval-architecture "porcupine" workflow: the designer looks for unintended inflections (sign changes of κ) and unintended extrema; hull-fairing optimisation formulations minimise bending energy subject to offset tolerances (secondary sources in [S11] search results). *(Flagged: single search-level sources)*
- **Frontier (2026):** progressive-iterative fairing with per-control-point weights for localised fairing (Lin et al., arXiv 2604.23590) — directly the "weighted region" idea, but it does not claim a continuity class and is unreviewed. *(Verified abstract [S14]; claims Flagged)*
- **Measures to display:** number of κ sign changes (inflections) vs intended; number of κ extrema; ∫(κ_s)² per unit length; max |Δκ| at knots; G0/G1/G2 gaps at every knot and at junctions, each against its tolerance; the comb itself. "Do not infer fairness from smooth shading" (A4) is correct: Gouraud/Phong shading hides G2 breaks; zebra/reflection lines and the comb reveal them. *(Inferred [S11][S30])*

#### 4. Airfoil-specific parameterizations

**CST / Kulfan.** ζ(ψ)=C(ψ)·S(ψ)+ψ·ζ_TE with ζ=z/c, ψ=x/c, C(ψ)=ψ^{N1}(1−ψ)^{N2}, S(ψ)=Σ_{i=0}^{n}A_i·K_i·ψ^i(1−ψ)^{n−i}, K_i=n!/(i!(n−i)!) — implemented verbatim in AeroSandbox (`C = x**N1 * (1-x)**N2`, Bernstein matrix, `y = C*S`, TE term `± x*TE_thickness/2`) [S5]. N1=0.5, N2=1 gives a round LE and a pointed TE [S5][S6]. Kulfan's identities A_0 = √(2R_LE/c) (first coefficient ↔ LE radius) and A_n = tan β + ζ_TE (last coefficient ↔ boat-tail angle) are **Flagged** this session (the 2007/2008 PDFs could not be fetched; the search summary confirms only that "only the first term defines the leading edge radius and only the last term the boat-tail angle" [S4]). The LE modification (Kulfan 2008) adds a term ψ(1−ψ)^{n+0.5}·A_LE to decouple LE radius from the first Bernstein term; AeroSandbox implements it as `leading_edge_weight * x * (1-x)**(n+0.5)` [S5]. **Fitting** is linear least squares because ζ is linear in (A_i, A_LE, ζ_TE); AeroSandbox solves `lstsq(A,b)` and re-solves with ζ_TE=0 if the fitted TE thickness is negative [S5]. Note: pyGeo's rendered doc showed the class function as `x^N1 + (1−x)^N2` — that is a rendering/extraction artefact, the product form is correct [S5][S6]. **Exact Bézier form:** with ψ=t², ζ = t·(1−t²)·poly(t²) is a polynomial in t, so a CST airfoil is an exact polynomial Bézier of degree 2n+3 in t (the "exact Bézier representations of CST shapes" result, Flagged from search title only). *(Verified from source [S5]; identities Flagged [S4])*

**PARSEC (Sobieczky 1998).** Eleven parameters: R_LE, upper crest (X_up, Y_up, Y_xx,up), lower crest (X_lo, Y_lo, Y_xx,lo), TE angle α_TE, wedge β_TE, TE thickness, TE offset; each surface y = Σ_{n=1}^{6} a_n x^{n−1/2}, coefficients from a 6×6 linear system per surface [S27]. Exact polynomial in t with x=t². Intuitive but only 12 shape DOF — poor coverage for real foils (Masters et al. [S9]). *(Verified [S27][S9])*

**Hicks-Henne.** Perturbation of a baseline: y = y_base + Σ a_i f_i(x), f_i(x)=sin^t(π x^{m_i}), m_i = ln 0.5/ln x_i, peak at x_i, zero at 0 and 1 [S26]. Needs a baseline; bumps are C^∞ but the sum's curvature near x→0 is baseline-dominated. Optimizer-only. *(Verified [S26])*

**B-spline / Bézier airfoils.** Direct control-point representations; Bézier-PARSEC (Derksen & Rogalsky 2010, Flagged) maps PARSEC-like parameters onto Bézier controls. Masters et al.: B-splines reach coverage with similar counts to CST; the 2025 review notes B-spline smoothness sped convergence in some studies while CST needed fewer variables [S9][S10]. *(Verified [S9][S10])*

**NACA 4-digit (closed form).** Thickness (half, in chord units): y_t = 5t(0.2969√x − 0.1260x − 0.3516x² + 0.2843x³ − 0.1015x⁴); with −0.1036 the TE closes to zero thickness [S5]. Camber for mpxx: y_c = (m/p²)(2px − x²) for x≤p, (m/(1−p)²)((1−2p)+2px−x²) for x>p; thickness applied **normal** to the camber line: x_U = x − y_t sinθ, y_U = y_c + y_t cosθ, x_L = x + y_t sinθ, y_L = y_c − y_t cosθ, θ = atan(dy_c/dx) [S5]. Because of the normal offset and cosθ, a *cambered* NACA 4-digit surface is **not** a polynomial curve in any polynomial reparameterization; only the symmetric thickness form is (x=t², degree-8 polynomial in t). NACA 5-digit uses a different camber line (k₁ table) — Flagged from recall; TM 4741 covers 4-, 5-, 6- and 6A-series [S28]. **NACA 6-series:** defined by Theodorsen conformal mapping of a near-circle with ψ/ε functions on [0,π], thickness reached by iterative scaling; TM 4741 agrees with published ordinates to ~5×10⁻⁵ c (6-series) and up to 9×10⁻⁵ c near the LE (6A) [S28]. Consequence: NACA 64A410 in the catalog must be *coordinates with provenance* (TM 4741 program output or published table), never regenerated from a formula. *(Verified [S5][S28])*

**FFD** (Sederberg & Parry 1986, Flagged): deforms the space around a baseline via a Bézier/B-spline lattice — an optimizer tool over an existing shape, not a representation of record.

**2024–2026 comparisons.** Prog. Aerospace Sci. 158 (Oct 2025) reviews methods from the early 20th century to 2025 and ranks CST first for optimization with <22 variables, then PARSEC, B-spline, Hicks-Henne [S10]; generative/data-driven parameterizations (SVD/POD modes, Bézier-GAN, latent diffusion "AirfoilGen" 2026) are the frontier for *optimizer* coordinates [S10 search results]; SVD modes cover UIUC most efficiently [S9]. None of these is a direct-manipulation editor. *(Verified at abstract level [S9][S10]; frontier items Flagged)*

**Recommendation (Q4).** Editable representation of record for a *modified* profile: parametric clamped degree-5 B-spline pair (upper/lower or one closed curve) in normalized chord units, with constraints. CST, PARSEC, NACA 4/5 formulae, Hicks-Henne and DAT coordinates are **importers/conversions** producing (i) sampled coordinates (cosine-spaced, ≥ 160 points, refined near LE) and (ii) a fitted B-spline with a reported max normalized deviation. A CST vector may additionally be kept as *optimizer coordinates* derived by least squares from the record with its own residual. Residual expectations, **to be measured as fixtures, not assumed**: CST with 8–12 Bernstein terms per surface on smooth NACA/Eppler foils — order 10⁻⁴ c (Inferred from Masters' 20–25-variable coverage result and Kulfan practice; Flagged until measured); degree-5 B-spline LSQ with 20–30 controls per surface — ≤10⁻⁵ c on analytic foils (Inferred; fixture required); an exact-copy conversion (DAT → interpolating spline through every given point) reports zero at the points and a bounded between-point deviation. This keeps the repo's "one conversion each way" intent while moving the *editing* authority from CST to B-spline (see "Contradicts existing repo knowledge" below). *(Inferred)*

#### 5. Loft / skinning for wings

**Classical B-spline skinning (Piegl & Tiller ch. 10, Flagged from recall; corroborated by [S33]).** Given section curves C_k(u), k=0…K: (1) make them *compatible* — same degree (elevate the lower ones) and same knot vector (insert the union of all knots into every curve); (2) choose section parameters v̄_k (chord length across corresponding control points, averaged over columns) and knots by averaging; (3) for each control-point column i, interpolate the points P_{i,k} across k with a degree-q B-spline (q=1 ruled, q=3 cubic); the resulting tensor-product surface S(u,v)=ΣΣ N_{i,p}(u)N_{j,q}(v)P_{i,j} passes exactly through every section. The known cost: knot merging inflates the control net ("an astonishing number of control points" [S33]); remedies are common-knot-vector determination (Park 2008 [S33]) or approximate compatibility by refitting (Piegl & Tiller 1999-style, [S33]). *(Verified [S33]; algorithm steps Flagged)*

**Our lever: compatibility by construction.** If every station's section is *generated* from the profile-blend rule on a shared knot vector and degree (normalized camber and unit-thickness shapes fitted once onto a common parameterization, then blended, renormalized and scaled), the sections are compatible with zero knot merging, and the surface control net is the station control nets stacked. The correspondence rule is then the shared parameter u: u=0 at TE-upper, u=u_LE at LE (fixed, e.g. 0.5), u=1 at TE-lower, with cosine-like spacing in ψ. Feature correspondence (max-thickness alignment) is a reparameterization of each profile before fitting, not a post-hoc knot insertion. *(Inferred [S33][S30])*

**Channel-driven loft vs skinning (the A4 question).** A4 defines the station at span fraction η as: blend normalized profiles (camber line + unit-thickness shape, linearly in η between assigned profiles, then renormalize thickness to unit max) → scale by chord(η) and t/c(η) → rotate about the LE by twist(η) → translate by sweep(η) and elevation(η) [S30]. If the channels are B-splines in η and the blend is linear, the *unrotated* section net is polynomial in η; the twist rotation introduces cos(twist(η)) and sin(twist(η)), so **the exact A4 surface is not a tensor-product B-spline in η.** Two honest options: (A) define the record as the channel-evaluated analytic surface and treat any B-spline skin (for STEP/3DM export, or for the viewport) as a *derived approximation* built by sampling intermediate sections until the skin deviates from the analytic surface by less than a stated tolerance; (B) define the record as "skin the authored-station control nets with a degree-q B-spline in η" so the surface is exactly a B-spline, in which case the *channels* between stations are no longer authoritative (the interpolated twist is whatever the control-net interpolation implies). The spec's "every station dimension, loft, area and export derives from the same evaluated distribution" [S30] chooses (A). Deviation of (B)-style skins from (A) is second order in the station spacing and twist rate and small for typical washout (a few degrees), but it is a *measured* number, not zero. *(Inferred from A4 [S30])*

**Gordon, Coons, ruled, rails.** A Coons patch bilinearly blends four boundary curves; a Gordon surface interpolates two *intersecting* families of curves (sections and rails) and reduces to Coons for one curve per family — the families must intersect exactly, which is a strong compatibility requirement (Farin ch. 22/Gordon 1969, Flagged). A ruled surface is degree 1 in v (linear loft). In the workbench the LE and TE rails are *views* of the sweep/chord channels [S30], so a Gordon construction would double the authority; use it only as an internal check that the skin's iso-curves reproduce the rails within tolerance. **Tension / loose vs tight:** "tight" = interpolate the stations (degree 3 in η, or degree 1 ruled); "loose" = approximate them (fewer control rows); since authored stations are exact by definition (GEO-06), the loft is always tight *at stations* and the looseness is expressed through the channel curves' own Smooth weights. *(Inferred; Gordon/Coons citations Flagged)*

**Blending profiles.** Blend *normalized camber* and *unit-thickness shape* at equal ψ, not raw coordinates: blending raw coordinates of two profiles with different max-thickness positions produces an intermediate with wrong t/c and a false thickness peak; blending normalized shapes then renormalizing to unit max thickness and applying the single t/c channel preserves the channel's meaning (A4's mandatory fixture) [S30]. Higher-order blends (Hermite in η with zero end slope, or a B-spline over three or more assigned profiles) are a later option and change intermediate profiles measurably; keep the blend rule id in the payload. *(Inferred [S30])*

**Curvature-reversal hazard with sparse stations.** A cubic (or quintic) interpolant of chord or twist through few stations with a sudden change overshoots (Runge/overshoot behaviour), producing a spanwise curvature reversal in the surface that shading hides. Mitigations, in order: show the channel curvature comb along η; prefer centripetal parameterization for the channel anchors [S24]; offer a monotone (Fritsch–Carlson/PCHIP, C¹ only — Flagged) *preview* to reveal the overshoot, never as the record; require ≥ 2 stations per "feature" (root, max-chord, tip start, tip). *(Inferred; consistent with the bench "open risk" [S30])*

**Surface continuity analysis.** First fundamental form E=S_u·S_u, F=S_u·S_v, G=S_v·S_v; second form L=S_uu·n, M=S_uv·n, N=S_vv·n with n = S_u×S_v/|S_u×S_v|; Gaussian K=(LN−M²)/(EG−F²), mean H=(EN−2FM+GL)/(2(EG−F²)). Display K and H maps, iso-curve combs, and zebra (reflection lines: stripes of n·d for a fixed direction d — jumps reveal G1 breaks, kinks reveal G2 breaks). Across a station plane, G2 in η is measured by comparing one-sided S_vv from adjacent knot spans. *(Inferred; standard differential geometry)*

**Tessellation for display/meshing.** For a curve arc of radius R=1/κ subtended by Δθ, chordal deviation (sag) ε = R(1−cos(Δθ/2)) ⇒ Δθ_max = 2·acos(1−ε/R); adaptive subdivision until |C((u₁+u₂)/2) − (C(u₁)+C(u₂))/2| ≤ ε **and** the tangent turn ≤ angle tolerance **and** the span length ≤ a max edge. For surfaces, subdivide the knot-span grid in u and v independently by the same test on iso-curves and additionally test the mid-patch normal deviation. Emit the actual measured max deviation with the mesh (instrumentation-over-inference). Rhino's default angle tolerance is 1°, recommended 0.1° for fine work [S16]. *(Inferred; constants Verified [S16])*

**Exact integrals from the surface definition.** Reference planform area S_ref = 2∫₀¹ c(η)·(b/2) dη over the half-span (unpitched chord integral, A4's definition [S30]); mean geometric chord = S/b. Wetted area A_w = ∫∫ |S_u × S_v| du dv; volume V = (1/3)∮ (S·n) dA over the closed surface (divergence theorem); centroid x̄ = (1/V)∫∫ (x²/2)·n_x dA etc. Integrate per knot span with Gauss–Legendre: for polynomial integrands (planform area of a B-spline chord channel) a rule with ⌈(2p+1)/2⌉ points per span is *exact* (degree-5 chord channel: 3 points — Inferred from Gauss exactness of 2m−1); for |S_u×S_v| (a square root) use 6–10 points per span and report the estimated quadrature error by Richardson comparison, never a plausible number. Degenerate tips (chord→0) integrate fine (the integrand → 0) but normals do not (below). *(Inferred)*

#### 6. Numerical robustness and tolerance semantics

- **Evaluation.** Cox–de Boor / de Boor are convex-combination recurrences and are numerically stable; Bernstein (and hence per-span Bézier) is the optimally stable non-negative basis (Farouki & Goodman 1996) [S25]. Never convert to the power basis for evaluation or storage; evaluate derivatives by the derivative-spline recurrence, not by finite differences. *(Verified [S25])*
- **Tolerances (openNURBS, verified constants [S15]):** ON_EPSILON = 2⁻⁵² ≈ 2.22×10⁻¹⁶; ON_SQRT_EPSILON ≈ 1.49×10⁻⁸; ON_ZERO_TOLERANCE = 2⁻³² ≈ 2.33×10⁻¹⁰ ("when an absolute zero tolerance is required to compare model space coordinates"); ON_RELATIVE_TOLERANCE = 2⁻⁴² ≈ 2.27×10⁻¹³ used as (|a|+|b|)·ON_RELATIVE_TOLERANCE; default angle tolerance = 1° (π/180); ON_DEFAULT_DISTANCE_TOLERANCE_MM = 0.01; unset values are the sentinel ±1.23432101234321e+308, chosen to round-trip through text I/O. **Rhino practice [S16]:** absolute tolerance = the greatest permissible distance for two things to count as touching; templates use 0.01 or 0.001 units; recommended 0.01–0.0001, never below 1e-5; joins allow 2× absolute tolerance; smallest feature should be ≥ 10× tolerance and model ≤ 10⁵ units; angle 0.1° or finer; "if you need a tighter tolerance use smaller units". *(Verified [S15][S16])*
- **Is 1 µm sensible?** A 1 m wing in metres has coordinates ~1 with ~1.1×10⁻¹⁶ relative double spacing, so 1 µm is ~10¹⁰ ULPs — comfortably representable, and it equals Rhino's tightest mm template (0.001 mm). It is appropriate as a **round-trip/identity** tolerance (DOC-02, GEO-06, GEO-11) and as the *reporting* precision for derived dimensions with a relative 10⁻⁶ companion (GEO-01). It is **not** appropriate as an operational geometry tolerance for closest-point, join, or self-intersection tests (Rhino's floor is 1e-5 units for good reason: Newton iterations and quadrature stop at their own convergence, typically 1e-8–1e-10 relative, and demanding 1e-6 *absolute* on a 1 m model at a CFD-scale operation risks non-termination or false "not equal" on legitimately equal geometry). Recommendation: three named tolerances — **identity** 1 µm absolute *and* 10⁻⁶ relative (both must fail to declare a difference), **model/join** 10 µm (0.01 mm) default, **angle** 0.1°; plus the openNURBS-style zero tolerance for internal comparisons. Serialization must write doubles in shortest-round-trip form (.NET's default `double.ToString()` since Core 3.0 is round-trippable — Flagged from recall); a 1 µm test on reopened geometry is otherwise a test of the formatter. *(Inferred from [S15][S16]; .NET claim Flagged)*
- **Parameter → arc length and inverse.** s(u) = ∫₀ᵘ |C'(t)| dt by Gauss–Legendre per span (accumulate span lengths once, cache); invert s→u by Newton on s(u)−s* with bisection safeguard inside the bracketing span; tolerance in *length* units (identity tolerance), max iterations reported. Rhino exposes this as `NormalizedLengthParameter`/`DivideByLength` in RhinoCommon but **not in rhino3dm** [S17]. *(Inferred; absence Verified [S17])*
- **Closest-point projection.** Coarse candidate search over sampled points (or the control polygon's convex hull per span), then Newton on f(u) = C'(u)·(C(u)−P) = 0 with two stopping criteria (Piegl & Tiller A6-style, Flagged): point coincidence |C(u)−P| ≤ ε₁ and zero-cosine |C'·(C−P)|/(|C'||C−P|) ≤ ε₂, clamped to the domain; for surfaces the 2×2 Newton system. Watch: near a degenerate tip |S_u×S_v|→0 and the Jacobian is singular — project to the tip curve instead.
- **Degenerate cases.** Zero-chord tip: all section control points coincide → a degenerate surface edge (legal in B-spline surfaces and in STEP); normals must be computed as the limit from the interior (or from the adjacent row); tessellators must collapse the edge to one vertex; wetted-area integrals converge. Coincident/out-of-order stations: the v-collocation matrix loses Schoenberg–Whitney and becomes singular — reject at input (the spec already does). Nearly coincident anchors in Through-points: chord-length parameters nearly equal → ill-conditioned; enforce a minimum parametric spacing (e.g. 1e-4 of the domain) and report. Interior thickness ≤ 0: detect by sampling the blended profile at the shared u and checking upper−lower along the local normal. *(Inferred)*
- **Symmetry and units.** Mirror as an exact reflection of control points (no re-fit); store SI metres; normalize sections to unit chord in a double frame (Rhino's "use smaller units" advice becomes "use normalized chord for sections"). *(Inferred [S16][S30])*

#### 7. Libraries (licences checked 2026-09-20)

| Library | What it offers | SPDX | Platform / binding | Version seen | Usable under COMMIT-02? |
|---|---|---|---|---|---|
| openNURBS / rhino3dm (`mcneel/rhino3dm`) | NURBS curve/surface types, evaluation, knot ops (`IncreaseDegree`, `MakePiecewiseBezier`, Greville), 3DM I/O; **no** interpolation, closest point, length, continuity query, loft, fairing [S17][S18] | MIT (GitHub API) [S19] | C++/.NET (NuGet 8.35.0)/Python/JS, Win/mac/Linux | 8.35.0 NuGet; Python docs 8.17 | **Yes** — as a type/serialization fallback and 3DM export; not as the loft |
| NURBS-Python / geomdl | Pure-Python NURBS, fitting (interpolate/approximate curve & surface, P&T algorithms), tessellation | MIT [S19] | Python | 5.4.0 PyPI | Yes — **reference implementation for fixtures/spikes**, not shipped |
| curvo (`mattatz/curvo`) | Rust NURBS: interpolation, loft/sweep/revolve, closest point, intersections, offset, adaptive tessellation, curvature/discontinuity analysis; depends on nalgebra [S19] | MIT (crates.io) [S19] | Rust | 0.1.91 (2026-06-25); nalgebra 0.35.0 | Yes — candidate if the Rust "door" opens; pre-1.0, API churn risk (Flagged) |
| truck (`ricosjp/truck`, truck-geometry/-modeling) | Rust B-rep kernel with NURBS geometry | Apache-2.0 [S19] | Rust | truck-geometry 0.5.0 (2024-09-20) | Yes; heavier than needed; (the crates.io crate `truck` 1.0.0 MIT is an unrelated placeholder) |
| Math.NET Numerics | Linear algebra (dense/banded solves, QR/SVD), cubic/Akima interpolation, Gauss–Legendre | MIT [S23] | .NET | 6.0.0-beta2 NuGet (5.0.0 stable) | Yes — KKT/LSQ solves |
| SISL (SINTEF) | Full spline library (fairing, intersections) | **AGPL-3.0** [S20] | C | — | **No** (copyleft; commercial licence otherwise) |
| Open CASCADE | B-rep kernel | LGPL-2.1 + exception | C++ | — | **No** (decision-0001 [S30]) |
| NLopt | Constrained nonlinear optimisation | **LGPL overall**; MIT new code, LGPL Luksan parts [S21] | C, many bindings | — | **No** for linking (process invocation only) |
| Ceres Solver | Nonlinear least squares (would suit energy fairing) | BSD-3-Clause [S22] | C++ | 2.2.x | Yes if a native dependency is accepted; not needed for the linear Smooth solver |
| scipy (FITPACK `curfit`, `make_smoothing_spline`) | Weighted LSQ and smoothing splines [S7][S8] | BSD-3-Clause (Flagged: not re-checked this session) | Python | 1.16–1.18 docs | Yes for spikes/fixtures; not a runtime dependency of a .NET app |
| STEPcode | STEP Part 21 writer for `B_SPLINE_SURFACE_WITH_KNOTS` | BSD (per decision-0001 [S30]) | C++ | — | Yes (or own writer) |

`SharpSpline` was named in the task; no authoritative repository or licence was located this session — **Flagged, do not depend on it**. *(Verified rows cite [S19]–[S23]; Flagged rows marked)*

#### 8. The geometry-of-record payload (what makes an edit lossless and reproducible)

A wing is reproducible only if every quantity that the evaluator reads is stored, and every quantity the evaluator *produces* is not (derive, never store). The following is the minimum; field names are illustrative, the *content* is the requirement. *(Inferred from §1–§7 and A4 [S30])*

```
wing
  frame: { convention: "body:+x aft,+y starboard,+z up; origin root LE", units: "m", version }
  tolerances: { identity_abs_m: 1e-6, identity_rel: 1e-6, model_abs_m: 1e-5, angle_deg: 0.1 }
  evaluator: { id: "cfdw.geom", version: "1.0.0", solver: "wlsq-kkt", fairness: "int(C''')^2", lambda }
  symmetry: { mode: "mirror-y" | "independent-halves" }
  channels: [sweep, chord, elevation, twist, thickness]   # each a `curve` over eta in [0,1]
  stations: [ { eta, profile: {ref, revision_hash}, thickness_policy: "source"|"channel",
                locks: [...], closure: {le, te, tip} } ]
  blend: { id: "linear-normalized-camber-unit-thickness", version, renormalize: true }
  loft: { rule: "channel-evaluated" (A) | "skin-station-nets" (B), v_degree, parameterization }
  correspondence: { u_te_upper: 0.0, u_le: 0.5, u_te_lower: 1.0, spacing: "cosine", feature_align: none|max-thickness }

curve                                   # one channel, or one section upper/lower/closed curve
  family: "bspline"                      # polynomial; rational only if weights != 1
  degree: 5
  knots: [...]                            # full clamped vector, count = n_ctrl + degree + 1 (Piegl–Tiller convention;
                                          #  openNURBS stores two fewer — state the convention explicitly)
  control_points: [[x,y],...]             # in the curve's own frame; units stated
  weights: [1,1,...]                      # explicit, so an exporter can prove non-rational
  mode: "through-points" | "smooth"
  through_points: { anchors: [{u, point, tangent: {linked|split, dir_left, mag_left, dir_right, mag_right}}],
                    parameterization: "chord"|"centripetal", end_conditions: {...} }
  smooth: { controls: [{id, position, influence_weight, attach_u}],
            constraints: [{type: point|tangent|curvature|value-at-station|symmetry|closure, u_or_eta, value, source}],
            lambda, fairness_functional }
  continuity_intent: [{u, class: "G1-break"|"G2"|"G4"}]  # deliberate breaks (knot multiplicity) are intent, not accident
  provenance: { source: catalog id / DAT hash / recipe, conversion: {method, max_dev_normalized, tolerance, accepted_by} }
```

Rules that follow: (1) evaluated curves, station readouts, tessellations, areas, spans and AR are never stored (they are recomputed and compared against the identity tolerance on load — a mismatch means the evaluator changed, which is itself a new revision); (2) the `evaluator.version` is part of the record because the Smooth solution depends on the fairness functional and λ, and a different solver version yields a different curve from the same controls and weights — GEO-14's "controls, weights, mode, locks and evaluated shape recover within DOC-02 tolerance" is only testable if the evaluator is pinned; (3) hard constraints carry their *source* (user lock, symmetry, closure, station value) so a conflict can name both parties; (4) a Through-points curve stores the anchors *and* the resulting knots/controls — anchors are the intent, knots/controls are what the evaluator reads, and a mode switch re-derives one from the other with a reported deviation (A4: "no round-trip exactness is assumed" [S30]).

## Marine and board CAD tooling: stations, master curves and loft UX

*Source: [03-marine-and-board-cad-tooling.md](03-marine-and-board-cad-tooling.md) (`kb-hw-marine-and-board-cad-tooling`).*

#### The master-curve + slice paradigm (surfboards)

Shape3d's Design Mode presents up to three 2D panels at once, each showing *outline, profile, thickness or slices*, and every curve is edited through its control points [S1]. The board is therefore fully described by: the outline curve (plan view, half-width vs length), the profile curves (bottom rocker and deck, side view), the thickness curve (deck-to-bottom distance vs length), and a list of slices (cross-sections at chosen lengths). The 3D surface is built by joining corresponding control points across slices, which is why the manual requires *"all the slices must have the same number of control points"* and advises *"the less slices the smoothest"* [S1]. Shape3d X adds a **slices list with positions**, volume repartition and cross-section area displays, and *"Multi-curves edition"* where stringer, rail and apex curves are edited together and *"edited curves can be set Fixed to another edited curve"* — a small relational link between master curves [S1][S4]. Six kinds of **3D Layers** (center, twin, free, constant-depth, side-cut, vertical-cut) add or subtract shapes on deck or bottom, stacked [S1]. *(Verified)*

Control-point editing in Shape3d: left-click selects (red), drag or arrow keys move, double-left-click adds at the clicked position (or right-click → Control point → Add new point), `Suppr` or right-click removes; group selection; a keyboard-step field that is *Auto* when blank (*"the more you zoom in the smaller the steps"*) with Shift/Ctrl/Shift+Ctrl dividing by 2/4/10. Five tangent kinds: continuous, angular, vertical, horizontal, and continuous with fixed angle. Curvature, curvature radius and directional curvature radius can be drawn either perpendicular to the curve or along the x-axis (*"Display curvature along the curve"* preference). **Tracing**: a left-click at any position displays the measurements of the design curves there (distance from tail and nose; in the side view also thickness and V/concave depth); **Master Scale** shows name, volume, length, width, thickness; **Guidelines** appear as green crosses, are created by a Guidelines Wizard, and can be imported from files to any curve; a **Ghost board** or image can be superposed [S1][S4]. *(Verified)*

The 3D Mode renders solid or wire; in wire view slice control points can be moved in 3D, *"not as easy as in the 2D view"* [S1]. The **Import/Scan** option fits curves automatically to guidelines from DXF/STL/text scan data [S2]. *(Verified)*

Formats and editions: native `.s3dx` (X) and `.s3d` (V8); opens `.brd`, `.srf`, `.kms`, `.pbd`. Lite (free) has outline, stringer, slices and 3D view; Design adds thickness control, fin-plug positioning, ghost/image; Design Pro adds multi-curve edition, asymmetric designs (fins/hydrofoils), 3D Layers, NACA profile generator, 2D exports (Text, DXF, IGES, PDF), hollow-wood plans, mold creation and buoyancy; the **3D Export** option adds mesh STL/DXF/OBJ/IGES and spline-surface IGES *"compatible with Rhino, Solidworks, CATIA"*; the **CNC** option writes G-code for 3-, 4- and 5-axis machines with bullnose/disk/spherical cutters (GCode, DXF, Shopbot, ACL/Data). Fin plugs include a *"Foil tracks"* preset. Windows is the primary platform; Mac is *"OSX 10.8.0 and later (portage by fr.portmyapps.com)"* [S2][S4]. Version 9.1.3.4 is dated 01/06/2026 in the manual [S1]. Pricing is subscription, EUR, per the table in finding 5 [S3]. *(Verified)*

**Disconfirming check on "Shape3d is Windows-native only":** the product page lists Windows 7…11 with DirectX and the Mac port by a third-party service. Whether the port is Wine-class cannot be confirmed from Shape3d's pages; "portmyapps" did not surface as a documented tool in a direct search, while the surviving Mac wrapper ecosystem (Wineskin → Kegworks → Sikarugir, Porting Kit, WineBottler) is what such services use [S34]. *(Inferred; Flagged for the exact mechanism.)*

#### AkuShaper

AkuShaper models *"every element of the design … through the use of bezier curves. Fluid continuous lines controlled through 3 control points each"* for outline, rocker (nose and tail rocker adjusted independently with auto-adjust), rail curves and thickness flow; it shows 2D deck-outline layouts *"as a surfer would expect"* and a *"3-D shaping bay"* with a **Contour highlight** 3D view for concaves. Measurement tools: Sliding Dimensions, Horizontal Line Guide, Zoom Box, Dimensions Adjustment, Center of Mass, Volume Sections, Rail Thickness Marks, Tokoro rail thickness, Biolos foot marks, Slice Area Profile, Ghost Board overlay, Point Movement Size, Rail Curve Refiner (tuck and apex). Exports OBJ/STL/IGES/DXF (Heavyweight tier), native `.brd`, encrypted `.brx`; imports `.s3dx` and `.srf`; *"compatible with over 300 CNC machines"*; plans Free demo, Backyarder USD 12.95/mo, Up-and-Comer 29.95/mo, Heavyweight 49.95/mo; Windows and Mac with a web login portal [S5]. *(Verified)*

**Difference from Shape3d:** AkuShaper exposes a lower-degree, fewer-handles model (three control points per segment) and emphasises shaper-facing readouts (foot marks, rail thickness marks, cutting preview) and CNC-centre compatibility; Shape3d exposes more curve machinery (five tangent kinds, curvature radius, multi-curve relational links, 3D layers, IGES spline surfaces, NACA generator, XFLR5/Flow5 export) and a wider licence ladder. Both keep the 2D panel as the primary editor and the 3D view as inspection. *(Inferred from [S1][S2][S5].)*

#### BoardCAD, BoardCAD LE and OpenShaper

BoardCAD is a Java CAD/CAM program for surfboards that exports STEP and g-code [S11]. Its user guide: *"The outline, rocker and cross sections are defined using composite bezier curves,"* with blue end points and tangent control points that *"define the direction and 'velocity' of the curve."* Cross-section interpolation is configurable: point-correspondence interpolation works when all cross-sections share control-point counts; **S-blend** handles varying counts. Editing is by mouse, keyboard and a control-point info panel; multi-select by box drag or Ctrl; ghost boards; curvature display (*"a measure of how far from flat a curve is"*); a **sliding cross section** readout; exports STL for CNC and DXF for templates, native `.brd` [S12]. OpenShaper is a browser rebuild of BoardCAD LE under GPL-3.0-or-later with planshape, rocker and rail-to-rail cross-sections, live volume/area/weight, 3D preview and STL/DXF/PDF export [S13]. BoardCAD's own licence was not stated on the pages opened *(Flagged)*; OpenShaper's GPL-3.0-or-later is from its site via search *(Inferred)*.

#### Relational geometry: MultiSurf / SurfaceWorks

AeroHydro's Relational Geometry (RG) is a *"patented conceptual framework"* developed since 1991 in which *"relationships between elementary objects may be captured and retained in a data structure equivalent to a directed graph, such that they can be utilized for automatically updating the complete model geometry following changes"*; the program *"can selectively update only the objects affected by a given change"* [S9]. Entities include **magnets** (points constrained to a surface) and **snakes** (curves constrained to a surface), which make joins *durable* under edits; **SubSurfs** use snakes as boundaries [S9]. Six **lofted surface** types share one three-stage evaluation: evaluate each master curve at parameter t, take those points as data, construct a lofting curve of the surface's type through them, evaluate it: Ruled (line), Arc-lofted (circular arc), Foil-lofted (NACA foil), B-lofted (B-spline), C-lofted (interpolating spline), X-lofted (explicit spline) [S9]. Cosine panel spacing is obtained by **relabeling** (reparameterizing) supporting curves, and the surface mesh follows [S9]. MultiSurf 9.0 offers *"30 surface types … (NOT limited to NURBS like other applications)"*, *"accurate, durable joins between surfaces"*, *"parametric variation: MultiSurf updates the entire model when you change underlying objects"*; imports DXF, IGES, FL/2B, Autoship, Maxsurf, FastYacht; exports DXF, IGES, POV-Ray, VRML; Windows 7–11; SurfaceWorks is the SOLIDWORKS integration; pricing is behind a pricelist link [S7][S8]. *(Verified)*

Interoperability, from a practitioner (Kasten Marine): the MultiSurf model *"is not a NURBS surface model, rather it is a relational parametric model composed of a variety of interrelated points, curves and surface types"*; MultiSurf *"includes very good tools which automate the NURBS creation process"* but the result *"is not precisely the same … rather it is a very close approximation"*, so transfer is *"a one way street from MultiSurf to NURBS to CAD"*, and *"the resulting NURBS control net is inordinately complex, making any down-stream editing nearly impossible if any semblance of fairness is to be preserved."* He also credits RG's *"extreme accuracy"* and its fit for downstream CFD [S10]. *(Verified, secondary source.)*

**Contradicts existing repo knowledge (refinement, not reversal).** `bench-cad-ux.md` states MultiSurf is *"notably not a NURBS model"*. AeroHydro's own page says the system is *"NOT limited to NURBS"* and its surface list includes B-spline and NURBS types [S8]; Kasten's point is that the *relational model as a whole* is not a NURBS model and that its NURBS export is approximate and one-way [S10]. The repo's practical conclusion (internal model may be relational; exported model must be NURBS/B-Rep, and the export is a conversion with a deviation) stands and is strengthened: the deviation must be measured and reported, exactly as spec A4 already demands for section conversions.

#### Naval-architecture NURBS modellers

**Orca3D** is a Rhino plug-in: *"the hull is created as a NURBS surface"*, Hull Assistants generate hulls from dimensional and shape parameters, and stations/buttocks/waterlines/other planar sections are defined once and *"update in real-time as the hull surface is modified"*, with real-time hydrostatics; modules (Design, Analysis, Advanced Stability) sell as perpetual or annual licences [S21][S22]. 2025 reseller pricing was quoted at GBP 365–1,385 depending on module and licence type [S22]. *(Verified [S21]; tertiary [S22].)* **Maxsurf Modeler** (Bentley): *"unlimited number of NURB surfaces & curves"*, *"trimmed NURB surfaces"*, shaded and contour curvature display, dynamic surface contouring, parametric transformation of the NURB model, DXF and IGES import/export, read/write Rhino files, Excel copy/paste [S23]. Licence terms were not on the page opened *(Flagged)*. **DELFTship**: Free and Professional editions; Professional adds *"automatic fairing of control curves"*, *"automatic fairing of surfaces or control points"*, asymmetric hulls, plate development and scripting; imports IGES entity 128 (NURBS surface) and STL; exports DXF, STL, IGES curves/surfaces, Seaway/Octopus stations [S24]. Its subdivision-surface model is stated on the vendor home page as quoted by search *(Inferred)*; the GPL FREE!ship ancestry is recall *(Flagged)*.

#### Wing tools: parameterization and tuning

**OpenVSP** (source `APIDefines.h`, opened): a wing section is driven by a chosen subset of `WING_DRIVERS` — `AR`, `SPAN`, `AREA`, `TAPER`, `AVEC` (average chord), `ROOTC`, `TIPC`, `SECSWEEP`, plus `SWEEP`, `SWEEPLOC`, `SECSWEEPLOC` — so the user picks which parameters drive and the rest are derived. Between sections, `WING_BLEND` offers `BLEND_FREE`, `BLEND_ANGLES` (sweep & dihedral), `BLEND_MATCH_IN_LE_TRAP`, `BLEND_MATCH_IN_TE_TRAP`, `BLEND_MATCH_OUT_LE_TRAP`, `BLEND_MATCH_OUT_TE_TRAP`, `BLEND_MATCH_IN_ANGLES`, `BLEND_MATCH_LE_ANGLES`. Section curve types (`XSEC_CRV_TYPE`) include `XS_FOUR_SERIES`, `XS_SIX_SERIES`, `XS_FOUR_DIGIT_MOD`, `XS_FIVE_DIGIT`, `XS_FIVE_DIGIT_MOD`, `XS_ONE_SIX_SERIES`, `XS_FILE_AIRFOIL`, `XS_CST_AIRFOIL`, `XS_VKT_AIRFOIL`, `XS_BICONVEX`, `XS_WEDGE`, `XS_EDIT_CURVE` and the fuselage families. Trailing-edge closure `XSEC_CLOSE_TYPE`: `CLOSE_NONE`, `CLOSE_SKEWLOW`, `CLOSE_SKEWUP`, `CLOSE_SKEWBOTH`, `CLOSE_EXTRAP`; trim `TRIM_NONE`, `TRIM_X`, `TRIM_THICK`. The generic edit-curve parameterizations (`PCURV_TYPE`) are `LINEAR`, `PCHIP` (piecewise cubic Hermite interpolating), `CEDIT` (cubic Bézier) and `APPROX_CEDIT` [S14][S15]. *(Verified from source.)* OpenVSP's licence is the NASA Open Source Agreement 1.3 (SPDX `NASA-1.3`) — recall, not re-confirmed this session *(Flagged)*.

**AVL** (`avl_doc.txt`, opened): each `SECTION` gives `Xle Yle Zle Chord Ainc [Nspan Sspace]`; *"the local chord and incidence angle are linearly interpolated between defining sections"*; *"at least two sections (root and tip) must be specified"*; the section shape follows via `NACA` (4-digit camber line), `AIRFOIL` (inline x/c, y/c) or `AFILE`; `Sspace` in [-3, 3] selects equal / sine / cosine / equal / cosine / -sine / equal at 3, 2, 1, 0, -1, -2, -3; cosine spacing is recommended chordwise and spanwise [S17]. *(Verified.)* AVL is GPL-2.0 — recall *(Flagged)*.

**XFLR5**: the Wing Edition dialog is a table of sections with span position y, chord, offset, dihedral, twist, foil, x-panels and y-panels with uniform/cosine/sine/-sine distributions; from v3.05 twist is applied about the quarter-chord after the dihedral rotation; cosine spacing is recommended where pressure gradients are high (tips, junctions) [S18]. *(Inferred — official guideline PDF and release notes quoted via search index, not opened; the xflr5.tech wing-design PDF returned 404.)* Flow5 is XFLR5's commercial successor; Shape3d exports to both [S1] *(Verified for the export; Flagged for Flow5's licence terms)*.

**WingHopper**: browser-based parametric hydrofoil wing tool — *"Shape the planform with live 3D feedback: span, area, sweep, dihedral"*, *"Blend real hydrofoil sections and control thickness along the span"*, *"Tune twist (washout) distribution for stall behavior"*, *"Export watertight STL, CAD-ready STEP, or XFLR5 for analysis"* [S28]. *(Verified.)* Whether distributions are sliders, tables or curves is not stated; free/paid status is from forum titles only *(Flagged)*. **KaroroCAD** (David Kay) is described as a dedicated wing-design CAD created to launch a wingfoil brand, and **3DFoil** (Hanley Innovations, Windows) analyses finite wings/hydrofoils with taper, twist, dihedral and sweep [S29][S30] *(Flagged — tertiary snippets)*.

#### The lines-plan tradition and its digital reproduction

A lines plan is three mutually consistent orthographic views of one 3D hull: the **body plan** (transverse sections at **stations**), the **half-breadth plan** (**waterlines**, horizontal cuts), and the **sheer plan** (profile and **buttocks**, longitudinal vertical cuts); **diagonals** are added as a fairness check; the **table of offsets** records the half-breadths and heights at every station/waterline/buttock intersection [S25][S26]. The FAO lofting text describes laying off the grid *"with all the water lines, buttock lines, diagonals and centre line"*, taking values *"from the designer's offset table"*, and checking *"all the lines as they are laid … for fairness"* using *"splines, or battens, and straight edges"*, admitting that *"the fairness of each line is 'in the eye of the beholder'"* [S25]. *(Verified.)* The lecture-note statement that the three sets are *"faired in turn and the changes in the other two noted … at the end of the iteration the three sets will be mutually compatible"* was seen only through the search index *(Flagged)*.

Digital tools reproduce this in two ways. NURBS modellers (Maxsurf, Orca3D, Rhino) keep the surface as the authority and **derive** stations/waterlines/buttocks as live section curves that update on every control-point move [S21][S23]. Relational and master-curve tools (MultiSurf, Shape3d, BoardCAD) keep **a few authored curves** as the authority and derive the surface by lofting through them [S1][S9][S12]. The spec's five channels plus authored stations are the second family: the plan view is sweep + chord, the front view is elevation, the section editor is the body plan, the station table is the table of offsets.

**Fairness, operationally.** Farin & Sapidis (IEEE Computer Graphics & Applications, 1989) propose that a curve is fair if its curvature plot consists of relatively few monotone pieces, i.e. as few curvature extrema as possible, and use curvature plots as the design instrument [S27]. This is the definition the repo's curvature-comb requirement should cite. *(Flagged — bibliographic details and definition match recall and the search abstract; the DOI page returned 403 and was not opened.)*

#### Loft option vocabulary across CAD

Rhino 8 `Loft` (help opened): **Loose** — *"the surface control points are created at the same locations as the control points of the original"*; **Normal** — *"an average amount of stretching between the curves"*; **Tight** — *"closely follows the original"*; **Straight sections** — *"creates a ruled surface"*; **Uniform** — *"makes the object knot vectors uniform"*; **Closed loft**; **Match start/end tangent** (only when the end curve is a surface edge); **SplitAtTangents**; cross-section handling **Do not simplify** / **Rebuild with N control points** / **Refit within tolerance**. Developable is not a Loft style in Rhino 8 (DevLoft is a separate command) [S16]. *(Verified.)* Fusion Loft: rails *"must intersect each section … must be tangent continuous"*; a **centerline** is a rail *"to which the Loft sections are held normal … A loft can only have one centerline"* and *"is just a general suggestion for how the loft behaves. It does not precisely specify the shape"*; end conditions Tangent (G1) / Curvature (G2); tangent-chain selection [S19]. Onshape Loft: start/end conditions Normal to profile, Tangent to profile, Match tangent, Match curvature (the last two need adjacent faces); guides must touch the profile outsides; optional **match vertices** define correspondence; without guides Onshape *"estimates the proximity within the existing vertices"* [S20]. *(Both Inferred — official help quoted via search index.)*

**What the vocabulary means for a foil editor.** Every one of these options exists to cope with inputs the loft did not author: mismatched control nets (Rebuild/Refit/Loose/Tight/match vertices), unknown correspondence (guides, seams, vertex matching), and unknown boundary neighbours (Match start/end tangent). A foil editor that owns its section parameterization (normalized chord, aligned LE/TE, fixed control counts or normalized blend) has no mismatched nets and no neighbours; the residual choices are only (a) the spanwise interpolation family between authored stations and (b) the tangent behaviour at a station. That is a three-word vocabulary — **straight** (ruled, AVL/Rhino Straight sections), **through stations** (interpolating spline, MultiSurf C-lofted, OpenVSP PCHIP-like), **blended** (tangent/strength control at a station, OpenVSP `BLEND_ANGLES`/match-angles) — plus per-station tangent locks. *(Inferred from [S9][S14][S16][S17].)*

## Hydrofoil disciplines and design data for water-sports foils

*Source: [04-hydrofoil-disciplines-and-design-data.md](04-hydrofoil-disciplines-and-design-data.md) (`kb-hw-hydrofoil-disciplines-and-design-data`).*

#### The rider/craft system per discipline

The system a design tool must model is rider + board + foil (+ hand-held wing, kite, parawing or motor/battery), moving at a speed set by the drive (wind, wave, muscle or motor) and supported by front-wing lift minus stabilizer down-force. The table below records what could be tied to a source; everything else is Flagged. Speeds are in knots with m/s in brackets.

| Discipline | Rider mass (kg) | Added mass (kg) | Drive power / envelope | Take-off · cruise · top (kn) | Optimizes for | Label |
|---|---|---|---|---|---|---|
| Wingfoil freeride | 55–110 (size charts) | board 4–8 [Flagged], foil 3.5–5 [Inferred from 2.19 kg front wing alone, S9], wing 1.8–2.6 (Duotone Unit 3.5–6.5 m², S25) | wind 10–25 kn for a 5.0 m² wing, rider >70 kg [S25] | 8–10 · 12–18 · 20–25 [Flagged] | balance of early lift, glide, carve | Inferred/Flagged |
| Wingfoil race (GWA box) | 60–95 [Flagged] | board ≥3 kg, foil ≥3 kg (rule minima, S12); wing 2.2–2.6 | wind: races held 5–35 kn for iQFOiL-style fleets [S26, secondary]; GWA wind limits Flagged | 9–12 · 15–25 · 30–37.9 (1 s record 37.89 kn [S20, tertiary]) | top speed, upwind VMG, ventilation resistance, stability at speed | Inferred/Flagged |
| Windfoil iQFOiL | Olympic athletes, ~70–90 M / ~55–70 W [Flagged] | board + rig + foil; foil one-design [S13] | wind 5–35 kn (package design range) [S26] | 10–12 · 18–28 · 30–35 [Flagged] | course racing speed and pumping efficiency in light wind | Verified equipment, Flagged speeds |
| Windfoil slalom/Formula (PWA) | 75–100 [Flagged] | sails ≤8.0 m² (2026 foil slalom) [S24, tertiary] | wind 7–35 kn [Flagged] | 12 · 25–30 · 35+ [Flagged] | top speed, cavitation margin, jibe stability | Flagged |
| Kitefoil race (Formula Kite) | ~65–85 M, ~50–65 W [Flagged] | kites ≤21 m² men / ≤19 m² women from 10 Aug 2024 [S15, secondary]; foil systems registered [S14] | wind 6–35 kn [Flagged] | 8–10 · 25–32 · ≈38 ("up to 70 km/h" Paris 2024 [S19]) | top speed, ventilation resistance (main athlete-reported limit [S17]) | Verified rule items, Flagged speeds |
| Surf foil (prone) | 55–100 | board 3–5, foil 3.5–5 [Flagged] | wave-driven; no published wave-energy band found | 6–8 · 10–15 · 18–22 [Flagged] | early lift, turning (roll rate, low second moment of area [S2]), ventilation recovery on breach [S3], pump to reconnect | Flagged |
| Pump foil / dock start | 50–95 | board 2–4 [Flagged], foil 3.5–5 | muscle: cadence ≈1–2 Hz [Flagged, S27] | 6–8 · 8–12 · 14 [Flagged]; Sabfoil Leviathan 1400 states 7–9 kn take-off, 16–18 kn top for 80 kg [S9] | glide (L/D), low take-off speed, pumping efficiency | Verified where cited |
| Downwind (SUP/prone) | 60–100 | SUP board 6–9 [Flagged], paddle | ocean swell/bump; Sabfoil Leviathan Pro 1060: 10–12 kn take-off, 25–27 kn top for 80 kg [S10, tertiary] | 7–10 · 12–18 · 25–27 | glide, early lift at low speed, high AR/long span | Inferred |
| Parawing foiling | as downwind | parawing 0.3–0.8 [Flagged] | wind 12–30 kn [Flagged]; World Cup status 2026 [S23] | as downwind; upwind legs exist [S23] | downwind glide plus upwind ability | Flagged |
| E-foil | 40–120 | board 20–21 without battery, battery 8.75–13 [S28, tertiary] | motor 4–6 kW (Waydoo EVO) [S28, tertiary]; Fliteboard cells "up to 45 min / 1.5 h / 2.5 h" [S29] | 6–8 · 12–18 · 21.6–24 (40–45 km/h [S28]) | stability, low take-off speed at high system mass, battery range | Verified Fliteboard ride times only |

Practitioner claims (tertiary) that a wingfoil racer's *foil* is chosen by wind band — ≈1000–1300 cm² at 8–12 kn, ≈700–900 cm² at 15–25 kn — are consistent with the GWA 700 cm² floor [S12] and the Axis ART Pro / Lift HA-X size ladders [S2][S6], but were not confirmed by a race-results dataset (Flagged; the IWSA equipment articles returned 404/403 this session [S30]).

#### Front-wing geometry bands from product specs

The table records official spec pages (primary, Verified) and retailer spec sheets (tertiary, Inferred). AR_calc = span²/area computed here; AR_stated is the manufacturer's number. Accessed 2026-09-20.

| Brand · model | Area cm² | Span mm | AR_stated | AR_calc | Discipline (maker's words) | Source · type |
|---|---|---|---|---|---|---|
| Axis ART v2 1099 | 1220 | 1099 | ~10 | 9.90 | winging; prone/DW/SUP/wind/kite | S2 official |
| Axis ART v2 999 | 1038 | 999 | ~10 | 9.61 | as above | S2 official |
| Axis ART v2 939 | 900 | 939 | ~10 | 9.80 | as above | S2 official |
| Axis ART v2 879 | 790 | 879 | ~10 | 9.78 | as above | S2 official |
| Axis ART v2 819 | 647 | 819 | ~10 | 10.37 | as above | S2 official |
| Axis ART Pro 1201 | 1318 | 1201 | 11.14 | 10.94 | speed/glide, 12+ AR marketing | S31 tertiary |
| Axis ART Pro 1121 | 1088 | 1121 | 11.78 | 11.55 | | S31 tertiary |
| Axis ART Pro 1051 | 932 | 1051 | 12.05 | 11.85 | | S31 tertiary |
| Axis ART Pro 1001 | 845 | 1001 | 12.06 | 11.86 | root chord 105 mm listed → mean 84 mm | S31 tertiary |
| Axis ART Pro 901 | 686 | 901 | 12.04 | 11.83 | chord 93 mm | S31 tertiary |
| Axis ART Pro 801 | 582 | 801 | 11.17 | 11.02 | chord 86 mm | S31 tertiary |
| Axis ART Pro 751 | 518 | 751 | 11.10 | 10.89 | | S31 tertiary |
| Armstrong MA625 | 625 | 690 | 7.6 | 7.62 | mid-aspect, "stability at speed + fast roll rate" | S3 official |
| Armstrong MA800 | 800 | 765 | 7.2 | 7.31 | | S3 official |
| Armstrong MA1000 | 1000 | 850 | 7.2 | 7.23 | | S3 official |
| Armstrong MA1225 | 1225 | 935 | 7.1 | 7.14 | | S3 official |
| Armstrong MA1475 | 1475 | 1050 | 7.5 | 7.47 | | S3 official |
| Armstrong MA1750 | 1750 | 1152 | 7.5 | 7.58 | | S3 official |
| Armstrong HA480 | 480 | 726 | 11 | 10.98 | high aspect | S32 tertiary |
| Armstrong HA680 | 680 | 818 | 9.80 | 9.84 | | S32 tertiary |
| Armstrong HA980 | 980 | 974 | 9.68 | 9.68 | | S32 tertiary |
| Armstrong HA1180 | 1180 | 1066 | 9.63 | 9.63 | | S32 tertiary |
| Armstrong APF1350 | 1350 | 1202 | 10.7 | 10.70 | pump foil | S32 tertiary |
| Armstrong APF1675 | 1675 | 1202 | 8.6 | 8.63 | pump foil | S32 tertiary |
| Armstrong APF1880 | 1880 | 1302 | 9.2 | 9.02 | pump foil | S32 tertiary |
| Lift 90 HA | 580 | 810 | 11 | 11.31 | HA pump/wave | S6 tertiary |
| Lift 110 HA-X | 710 | 965 | 13.1 | 13.11 | HA-X specialty | S6 tertiary |
| Lift 150 HA-X | 970 | 990 | 10.1 | 10.10 | | S6 tertiary |
| Lift 170 HA | 1100 | 940 | — | 8.03 | | S6 tertiary |
| Code 720S | 720 | 830 | 9.5 | 9.57 | "loose and surfy" | S7 tertiary |
| Code 850S | 850 | 900 | 9.5 | 9.53 | all-round | S7 tertiary |
| F-One Seven Seas 1200 | 1200 | 950 | 7.6 (range 7.5–8.0) | 7.52 | wing freeride | S8 tertiary |
| F-One SK8 (550–1050) | 550–1050 | — | 8.0 | — | surf/wing | S8 tertiary |
| Unifoil Progression 100 | 645 | 824 | 10.5 | 10.53 | | S11 tertiary |
| Unifoil Progression 170 | 1097 | 953 | 8.3 | 8.28 | easy lift, stable pitch, long glide | S11 tertiary |
| Unifoil Progression 200 | 1290 | 1037 | 8.3 | 8.34 | | S11 tertiary |
| North Sonar HA1050 | 1050 | 1050 | 10.5 | 10.50 | "upper speed spectrum, short chord, thin profile" | S11 tertiary |
| Sabfoil Leviathan Blackbird 1400 | 1549 | 1380 | 12.3 | 12.29 | DW/pump/SUP; root chord 173 mm, max t 18 mm (t/c_root 10.4%), 2190 g | S9 official |
| Sabfoil Leviathan Pro 1060 | 947 | 1060 | 11.53 | 11.87 | fast DW, pump race; max t 14 mm, volume 757 cm³ | S10 tertiary |
| Sabfoil Leviathan Pro 1160 | 1083 | 1160 | 12.42 | 12.42 | | S10 tertiary |
| Mikes Lab (wing/DW) | 950 | 1010 | — | 10.74 | split fuselage, DW/parawing/light-wind wing | S33 tertiary |
| Starboard iQFOiL 900 | 900 | — (not published) | — | — | one-design race; Speer profile | S13 official |
| Starboard iQFOiL 800 (junior) | 800 | — | — | — | | S13 official |

Geometry bands not on spec pages — taper, sweep, anhedral, twist, section family, tip shape — were not published by any brand opened this session. What can be stated: Axis lists a root chord (105 mm for ART Pro 1001) against a mean chord of 84 mm, implying taper (mean/root ≈ 0.8) consistent with a ≈0.4–0.5 tip/root ratio on a rounded-tip planform (Inferred); Axis ART v2 cites a "tapered outline and reduced second moment of area for roll" [S2] (Verified wording); Armstrong MA cites "rondure" for ventilation recovery on breach [S3] (Verified wording). Anhedral of 5–12°, washout 0–3° and downturned tips are practitioner claims (Flagged). The existing catalog note that hydrofoil sections average ≈9.3% t/c (E817–E908) matches Sabfoil's 10.4% root t/c on a "race-derived thin profile" [S9]; the "ultra-thin" marketing of 2024–2026 therefore means ≈9–11% at the root, not <8% (Inferred).

**Rear stabilizer.** Rule floor 150 cm² (GWA) [S12]; iQFOiL 255 cm² with −2° default and a −2° to +1° shim range [S13]. Typical wing/surf stabilizers are 130–300 cm², span 350–500 mm, AR 4–7, with anhedral and 0–2° shims (Flagged; no spec page opened gave span/AR). Decalage (front-wing incidence minus stabilizer incidence) is the pitch-stability and trim driver; the iQFOiL shim range is the only rule-published decalage adjustment found.

**Mast.** GWA cap 115 cm [S12]; iQFOiL 95 cm carbon UHM, Deep Tuttle [S13]; consumer masts 60–100 cm (Flagged practitioner; surf-magazine advice 85–95 cm for sporty riding/racing [S34, tertiary]). Mast chord ≈110–140 mm, t/c ≈11–15% with symmetric sections (E836–E838, NACA 00xx class in the existing catalog) (Flagged). Stiffness is marketed (UHM/HM carbon) but no brand publishes EI or tip deflection under load (gap).

**Fuselage.** GWA cap 100 cm [S12]; iQFOiL "115 Plus" (length not published on the page opened) [S13]; consumer 55–85 cm (Flagged). A longer fuselage increases stabilizer moment arm (pitch damping, lower shim), raises yaw/roll inertia, and moves the mast–wing junction relative to the pressure centre; no primary measurement of the effect on a consumer foil was found.

#### Racing class rules

| Class | Governing text | Geometry constraints | Openness | Label |
|---|---|---|---|---|
| GWA Wingfoil World Tour, Race | Rulebook via equipment page [S12] | production ≥50 pcs; front wing ≥700 cm²; rear ≥150 cm²; mast ≤115 cm (≤3); fuselage ≤100 cm; foil ≥3 kg; board ≥3 kg, no extra strap plugs; wings symmetric, inflatable LE, any size, ≤5/season; ≤3 foil systems, ≤2 boards/season; box rule since 1 Feb 2022 | designer free above the minima; size and count caps push toward one "all-wind" foil of ≈700–900 cm² | Verified |
| iQFOiL (Olympic windfoil) | Starboard iQFOiL pages [S13]; class rules 2025 not opened | one-design: 900 front (800 junior), 255 tail −2°…+1°, 115 Plus fuselage, 95 cm mast, Deep Tuttle | nothing to design; useful as a fixed benchmark | Verified equipment |
| Formula Kite (Olympic) | IKA builder info [S14]; news [S15] | registered production series (≥100 pcs/model/size per news item); tolerances weight ±4%, general ±0.4 mm, height/length ±2 mm, chord −1 mm, span ±2 mm; 10 pieces 3D-scanned; 500 EUR/item + 5000 EUR/manufacturer; kites ≤21 m² M / ≤19 m² W | maxima (span, length, mast) are in the class rules PDF — not opened | Verified tolerances; Flagged maxima |
| International Moth | Class rules 2017 [S35] (Dec 2024 edition exists, not opened) | 6.2 overall beam ≤2250 mm; 6.3.3 any foil except rudder foils must protrude from the hull below the static waterplane; 8.1 mast spars ≤6250 mm; 11.2 no multihull; 12.1 sail pumping limited | foils otherwise unrestricted (development class); whether foil span counts in "overall beam" is not stated in the extracted text | Verified quotes; Flagged span interpretation |
| PWA windfoil slalom 2026 | forum report [S24] | 1 mast, 2 front, 2 back wings, 2 fuselages; 3 sails ≤8.0 m² | count limits only | Flagged (tertiary) |
| Parawing downwind | SUPboarder report [S23] | World Cup status 2026 (Leucate); no box rule found | open | Flagged |
| IWSA Formula Wing / Open class | article [S30] returned 404 | — | — | Flagged (not opened) |

"Open-class" (Moth, downwind, parawing, freeride) designers are free in area, span, AR, section and configuration; box-rule classes (GWA, Formula Kite) fix minima/maxima and force production runs, which is why race foils cluster at the rule floor (≈700 cm² wing race) and why a design tool needs rule presets that validate a recipe against these numbers.

#### Operating physics that shape design

**Free surface.** The CEHINAV tank campaign [S16] (Martínez-Barberá, Calderon-Sanchez, Moulian, Souto-Iglesias, J. Marine Sci. Appl. 25(1), 2026) towed a 0.059 m² kitefoil/windfoil wing (span 0.634 m, mean chord 0.0735 m; AR 6.8 by the b²/S convention used throughout this base — the paper itself defines AR := b_eff/c with b_eff = S/c, i.e. S/c² = 10.9, which is why area file 07 quotes 10.9; both numbers describe the same wing and the convention must travel with any imported reference figure) at Re 7.3×10⁴–2.9×10⁵ and Fr_h = V/√(g·h) 0.4–6.7, h/c 0.5–9.5, α −5° to +10°. Findings (Verified): coefficients asymptote to deep-water values only for h/c > 5; effects concentrate at h/c < 4; at h/c = 4, raising Fr_h from 2 to 5 removes ≈17% of the lift coefficient; the authors state Fr_h ≈ 5 is the competition regime. Two-dimensional RANS-VOF work by Pernod et al. (J. Sailing Technology 2023) [S36] reports free-surface deformation for h/c < 2 with lift rising slightly to h/c ≈ 1 then falling sharply, and drag up to ≈3× the deep value near h/c ≈ 0.5 (Flagged: taken from search abstracts; article returned 403). The classical treatments — Wadlin & Christopher (1958) finite-depth lift for rectangular surfaces including dihedral to 30°, and Hough & Moran (1969) Froude-number effects — are cited via secondary summaries only (Flagged). Consequence: the high-Froude limit behaves like an image vortex of opposite sign (lift-slope reduction), the low-Froude limit like a rigid wall (lift-slope increase), so *depth and speed must both be operating-point inputs* and "deep water" must be an explicit assumption with h/c ≥ 5 as its validity floor.

**Ventilation.** Augier et al. (MARINE 2025) [S17] tested Olympic-series kitefoil masts on a boat-mounted beam at several speeds and surface treatments; ventilation inception correlates with the *dynamics* of angle of attack and vertical acceleration rather than their static values, surface preparation is secondary, and a hysteresis model represents the behaviour (Verified abstract). Aguiar Ferreira et al. (J. Fluid Mech. 1028, A25, 2026) [S18] towed surface-piercing semi-ogive and modified NACA 0010-34 models (AR 1.0, 1.5) at Fr 0.5–2.5: nose ventilation dominates at Fr < 1.0–1.25 (inception α rising with Fr), tail ventilation at higher Fr (inception α falling), base ventilation only on the blunt profile; the bistable/stable boundary extends to significantly higher α than previous maps (Verified abstract). Bartesaghi & Provinciali (2022) "Kite foil mast ventilation study" exists but was not opened (Flagged). Design reading (Inferred): a steady estimator cannot predict ventilation onset; it can only show the σ- and Fr-based envelope and flag surface-piercing/near-surface states, and the tool must never label a result "ventilation-safe".

**Cavitation.** σ = (p_atm + ρ g h − p_v)/(½ ρ V²); inception when −Cp_min ≥ σ (Verified definition; standard). With p_atm = 101.325 kPa, h = 0.5 m, seawater 15 °C (ρ = 1026.02 kg/m³, p_v = 1.671 kPa [S1]): p_∞ − p_v = 104.69 kPa. See the σ table below. Tom Speer's H105 page (tspeer.com) could not be opened (TLS failure) — the claims that H105 has a flat rooftop, higher incipient-cavitation speed than E817 at 10–20 kN/m² loading and lower take-off speed come from a forum thread quoting it (Flagged, tertiary [S37]). The existing repo note "E818 42.1 kn vs NACA 4412 31.4 kn" (IHS source) is consistent with −Cp_min ≈ 0.42 vs ≈0.76 at 0.5 m depth in this arithmetic (Inferred).

**Pumping.** The flapping-foil literature places peak propulsive efficiency at St = f·A/U ≈ 0.25–0.40 with large heave/chord and 15–25° maximum effective α at ≈90° pitch–heave phase [S21, secondary]; quasi-steady analysis loses accuracy above ≈2–3 Hz [S38, secondary]. Human pump cadence (≈1–2 Hz, 60–120 strokes/min) and heave amplitude (≈0.2–0.4 m) are practitioner numbers only [S27] (Flagged); no field IMU dataset was found. At f = 1.2 Hz, A = 0.3 m, U = 5 m/s, St = 0.072 (Inferred) — a low-St, glide-dominated regime in which lift-to-drag ratio at CL ≈ 0.4–0.7 and Re 4–7×10⁵ governs, consistent with the market's high-AR pump foils [S3].

**Stall and take-off.** Take-off occurs at the highest CL the section family sustains without laminar separation at Re 3–6×10⁵ — ≈0.6–0.8 from the Sabfoil envelopes above (Inferred) and up to ≈0.78 for surf presets per the existing repo note. Thin race sections (≈9–10% t/c, low camber) have lower CL_max and need either a larger wing, pumping, or more wind to take off; this is the light-wind/top-speed conflict every racer manages by carrying 2–3 registered front wings [S12].

**Roll/yaw stability, anhedral, junction.** Anhedral adds dihedral-effect of negative sign and lowers the roll second moment, giving the "loose" feel brands advertise [S2]; no primary quantification for consumer foils was found (Flagged). The mast–wing junction sits at the root where loading is highest; the existing gap register lists junction drag as unquantified and this session found no consumer-foil junction measurement (gap stands).

**Water properties.** See the ITTC table and Re table below (Verified [S1]).

#### Real-world validation and measurement

| Dataset / study | What it measured | Usable for | Label · source |
|---|---|---|---|
| CEHINAV kitefoil/windfoil wing tank test (2026) | CL, CD vs α, h/c, Fr_h; Re 0.7–2.9×10⁵ | free-surface correction validation; low-Re polars of a real race-type planform | Verified [S16] |
| Delft surface-piercing foils (JFM 2026) | ventilation inception maps vs Fr, α, AR | envelope flags for mast/near-surface states | Verified [S18] |
| Augier et al. Olympic kitefoil mast (MARINE 2025) | ventilation inception vs input dynamics | justification for "steady result ≠ ventilation-safe" | Verified [S17] |
| Pernod et al. 2D RANS-VOF (JST 2023) | 2D free-surface lift/drag vs h/c | 2D correction sanity check | Flagged [S36] |
| ResearchGate "Tank testing of a windfoil hydrofoil with free surface effects" (2025) | tank data on a windfoil | potential second anchor | Flagged (403) [S39] |
| Manufacturer take-off/top-speed statements | operating envelope per wing size and rider mass | bounding CL bands | Verified [S9], tertiary [S10] |
| GPS speed records / GPS Formula racing metrics | best 200/400/1000 m speeds, tack/jibe VMG | upper speed bound | Tertiary [S20][S40] |
| Vaaka cadence sensor | paddle stroke cadence (paddling, not foiling) | none found for pumping | Flagged [S41] |
| Foilmount | mounting adapters, not sensors | none | Flagged [S22] |

No brand publishes force/moment data, and no open GPS/IMU dataset for a consumer race foil was found; the estimator's sanity bounds must therefore come from (a) the product table (area/span/AR envelope), (b) manufacturer envelopes (CL band), (c) the σ and Fr_h tables, and (d) the CEHINAV low-Re polars once obtained.

#### Goal-state derivation: 90 kg rider, race wingfoil, salt water, 10–20 kn wind

Assumptions, each labelled:

| Input | Value | Label · basis |
|---|---|---|
| Rider | 90 kg | given |
| Race board | 5.5 kg | Flagged (GWA minimum 3 kg [S12]; typical carbon race boards 4–7 kg, practitioner) |
| Foil complete | 4.5 kg | Inferred (front wing alone 2.19 kg [S9]; GWA minimum 3 kg [S12]) |
| Hand wing | 2.4 kg | Inferred (Duotone Unit 5.0 m² 2.18–2.34 kg [S25, retailer-quoted spec]) |
| Wetsuit, harness, helmet, leashes | 3 kg | Flagged |
| System mass | ≈105 kg → W = 1030 N | Inferred |
| Front-wing lift | 1.00–1.10 W (stabilizer down-force minus wing aero vertical lift) → use 1050 N | Flagged (no measured trim data) |
| Water | seawater 35 g/kg, 15 °C: ρ 1026.02 kg/m³, ν 1.1892×10⁻⁶ m²/s, p_v 1.671 kPa | Verified [S1] |
| Foil depth | 0.3–0.5 m (mast 85–95 cm, board clearance) | Flagged |
| Board speeds, 10 kn wind | take-off 9–11; upwind 11–14; downwind 14–18 kn | Flagged (no race dataset opened; consistent with iQFOiL 5–35 kn design range [S26] and record 37.9 kn [S20]) |
| Board speeds, 20 kn wind | upwind 18–22; downwind 24–30 kn | Flagged |
| Foil choice by wind | 10 kn: ≈1000 cm², AR ≈12, span ≈1.10 m; 20 kn: ≈750 cm², AR ≈12, span ≈0.95 m | Inferred from the product ladder [S2][S6][S31] and the GWA 700 cm² floor [S12] |

Operating points computed with q = ½ρV², CL = L/(q S), Re = V c̄/ν, c̄ = S/b:

| Wind | Wing | V kn (m/s) | q Pa | CL | Re | σ at 0.5 m | Note |
|---|---|---|---|---|---|---|---|
| 10 kn | 1000 cm², b 1.10 m, c̄ 0.091 m | 10 (5.14) | 13,577 | 0.773 | 3.9×10⁵ | 7.7 | take-off; at/above thin-section CL_max → pump or bigger wing |
| 10 kn | same | 14 (7.20) | 26,611 | 0.395 | 5.5×10⁵ | 3.9 | upwind cruise |
| 10 kn | same | 18 (9.26) | 43,989 | 0.239 | 7.1×10⁵ | 2.4 | downwind |
| 20 kn | 750 cm², b 0.95 m, c̄ 0.079 m | 15 (7.72) | 30,548 | 0.458 | 5.1×10⁵ | 3.4 | post-take-off |
| 20 kn | same | 22 (11.32) | 65,712 | 0.213 | 7.5×10⁵ | 1.6 | upwind |
| 20 kn | same | 28 (14.40) | 106,443 | 0.132 | 9.6×10⁵ | 0.98 | downwind |
| 20 kn | same | 32 (16.46) | 139,028 | 0.101 | 1.1×10⁶ | 0.75 | top end; needs −Cp_min < 0.75 |

*Correction 2026-09-20 (spec v1 gate, re-executed with 1 kn = 0.514444 m/s, ρ 1026.02, W = 1050 N, h_ref 0.5 m): q recomputed for every row; the 10 kn CL is 0.773, not 0.774 (the earlier row rounded q to 13,570). σ at 0.3 m reads 7.56 / 3.86 / 2.33 / 3.36 / 1.56 / 0.96 / 0.74.*

Wing loading 10.5 kPa (1000 cm²) and 14.0 kPa (750 cm²) — inside the 10–20 kN/m² range attributed to Speer's kite/wind foil design discussion [S37, tertiary]. Cavitation ceiling: at 0.5 m depth a section holding −Cp_min ≤ 0.63 at CL ≈ 0.1 reaches 35 kn; ≤ 0.48 reaches 40 kn (Inferred from [S1] arithmetic). Plausible race geometry for this brief: 700–1050 cm², AR 11–13, span 0.93–1.15 m, root t/c 9–10.5%, mean chord 75–95 mm, Eppler E817/E818-class or a Speer-type flat-rooftop section ranked at Re 5×10⁵–1.1×10⁶ (Inferred). Competing race-oriented production foils with published numbers: Axis ART Pro 751–1051 (518–932 cm², AR 11.1–12.1) [S31]; Lift 110 HA-X (710 cm², AR 13.1) [S6]; Armstrong HA480–HA680 (480–680 cm², AR 9.8–11) [S32]; North Sonar HA1050 (AR 10.5) [S11]; Sabfoil Leviathan Pro 1060/1160 (947/1083 cm², AR 11.5–12.4, downwind-marketed) [S10]; Mikes Lab 950 (AR 10.7) [S33]; the iQFOiL 900 as the fixed windfoil benchmark [S13]. Dedicated wing-race models (F-One, North "R", Sabfoil race line) were not opened and are Flagged.

#### Product-line trends 2023–2026

- **Downwind-derived ultra-high-AR foils** (AR 11–13, spans 1.2–1.4 m, areas 950–1550 cm²) crossed from downwind into wing and pump use (Sabfoil Leviathan/Blackbird [S9][S10], Lift HA-X [S6], Axis ART Pro [S31], Naish "Super High Aspect" blog [S42, secondary]). Industry guides call downwind and wing the two sales drivers of 2025 [S43, secondary].
- **Thin "race-derived" sections** are now the default across freeride lines (Sabfoil Blackbird: 10.4% root t/c, Verified arithmetic on [S9]); the trade is higher take-off speed and a narrower CL band, which brands offset with "active-lift" fuselages and bigger areas.
- **Mast stiffness** is marketed via UHM/HM carbon (iQFOiL "stiffer UHM carbon mast" [S13]; F-One SK8 HM [S8]); no brand publishes stiffness numbers (gap).
- **Modular interfaces**: 90 × 165 mm plate de facto; F-One 160 × 90/165 × 90; Deep Tuttle for windfoil [S22, tertiary; S13]; proprietary wing–fuselage joints (Sabfoil T8/MH172 [S9][S10]).
- **Parawing** (2024–2026) reuses downwind foils and adds upwind legs to racing [S23]; **e-foil** systems converge on 4–6 kW motors and 2–2.3 kWh packs [S28, tertiary].
- Implication (Inferred): a design tool's presets must span AR 7–13 and area 480–1900 cm², expose depth and speed as first-class operating inputs, and treat "thin" as a t/c channel value (≈0.09–0.11 root) rather than a marketing tag.

## File formats and grammars for curves, surfaces, meshes and CFD data

*Source: [05-file-formats-and-grammars.md](05-file-formats-and-grammars.md) (`kb-hw-file-formats-and-grammars`).*

#### 1. 2D airfoil coordinate formats

**Selig format.** The UIUC page: coordinates "order starts from upper surface trailing edge, then wraps around the leading edge to the lower surface trailing edge"; the first line is the name; files may contain `#` comment lines "anywhere", which XFOIL ignores and "software applications reading these airfoil .dat files should be configured to handle" [S1]. Observed `coord_seligFmt/e817.dat` [S3]:

```
EPPLER 817 HYDROFOIL AIRFOIL
1.0000000 0.0000000
0.9966800 0.0008500
0.9872300 0.0035200
```

**Lednicer format.** Observed `coord/e817.dat` [S3]:

```
EPPLER 817 HYDROFOIL AIRFOIL
       35.       34.

 0.0000100 -.0000500
 0.0001600 0.0008700
 0.0009300 0.0024200
```

Verified from the file: a name line, a count line of two **floats** (`35.` `34.` = upper and lower point counts), a blank line, then the upper surface **from LE to TE** (x increasing from 0). The lower surface follows after another blank line, LE→TE — *(Inferred from the count line and the format's documented convention; the second block was not printed in this session — Flagged until the parser fixture confirms it)*. Note the two blocks may have **different counts** (35 vs 34) and the leading-edge point may appear in both blocks. The first coordinate `0.00001 -0.00005` shows Lednicer files are not guaranteed to start exactly at (0,0).

**Detection rule (Inferred from the two layouts, [S1][S3]).** After the name line, read the first numeric line: if both values are ≥ 1.5 *and* integral-valued (`35.` `34.`) it is a Lednicer count line; a Selig file's first numeric line is a coordinate with 0 ≤ x ≤ ~1.05. A single-block file whose x runs 1→0→1 is Selig; two blocks each running 0→1 separated by a blank line is Lednicer. Files where x runs 0→1 once are neither (a half-profile or a corrupted file) and must be **rejected**, not guessed. The Phase-0 defect (Lednicer read as Selig) produces a self-crossing polygon, so the self-intersection test in CAT-02 is the last line of defence, not the first.

**Robust parser contract (Inferred, grounded in [S1][S3][S4][S5]):**
- Tokenise on whitespace, comma, semicolon, tab (AeroSandbox's regex `[;|,|\s|\t]` [S5]); accept `.5`, `-.00005`, `1E-3`; reject any non-finite value; reject lines with ≠2 numeric tokens except the name line and a Lednicer count line; accept `#` comments anywhere [S1].
- Strip a UTF-8 BOM; accept CRLF (UIUC states "DOS EOF format (PC)" [S1]) and LF.
- Detect layout per the rule above; report **detected format**, point counts, whether the LE point is duplicated, whether the TE is open (`y_upper(1) ≠ y_lower(1)`) and by how much (TE thickness in chord units), and whether the profile is closed.
- Normalise: chord to 1 by translating the LE (the point of minimum distance from the TE midpoint, not simply `min(x)`) to (0,0) and rotating the TE to (1,0) — AeroSandbox `normalize()` does "translation and rotation" [S4]. Record the transform. Keep the **original** coordinates and hash them (CAT-02 requires "original points retained").
- Reject: self-crossing polylines, fewer than ~20 points per surface (a heuristic; state it), x outside [−0.05, 1.05] before normalisation, duplicate consecutive points, non-monotone x within a Lednicer block.
- Names beginning with `T`/`F` are a Fortran pitfall on **export** to XFOIL/AVL: prefix `_` as UIUC advises [S1].

**Other 2D encodings.**
- *XFOIL / XFLR5*: read the Selig layout; XFOIL also reads a "plain" file with no name line (Flagged, recall). XFLR5 reads the same `.dat` files (Inferred from UIUC's link to XFLR5 [S2]).
- *OpenVSP*: exports Selig `.dat` and a Bezier `.bz` file ("the first two lines list the file name followed by the total number of Bezier segments", each line one segment with order, global t-values and control points), plus a CSV metadata file with LE/TE coordinates, chord and section index [S7]. An internal `.af` format was **not** found on the export page — Flagged; the pack's earlier "OpenVSP airfoil file (`.af`)" mention is unconfirmed.
- *CST coefficient files*: no standard container exists; AeroSandbox's `get_kulfan_coordinates(lower_weights, upper_weights, leading_edge_weight, TE_thickness, n_points_per_side, N1=0.5, N2=1.0)` [S5] is a de-facto parameter set (upper/lower Bernstein weights, LE modifier, TE thickness, class exponents). A native profile revision that stores CST must store **all** of these plus the number of points and the sampling law used to evaluate, or the evaluated coordinates are not reproducible.
- *Airfoil Tools exports*: Selig `.dat` (Flagged, recall — not opened this session).
- *AeroSandbox `write_dat`*: `%f %f` = 6 decimal places [S4]. At chord 1 that is 1 µm resolution — exactly the DOC-02 tolerance with no margin; at a 0.10 m chord it is 0.1 µm. Our own DAT export should write **shortest round-trip** or at least 9 significant digits and state the resolution in the EXP-02 dialog.

#### 2. Wing / foil geometry grammars

**AVL `.avl`** [S6]. Header: title, Mach, `iYsym iZsym Zsym`, `Sref Cref Bref`, `Xref Yref Zref`, optional `CDp`. Frame: "X = downstream, Y = out right wing, Z = up" (matches the spec's +x aft, +y starboard, +z up). `SURFACE name / Nchord Cspace [Nspan Sspace]`, `YDUPLICATE Ydupl`, `SCALE`, `TRANSLATE`, `ANGLE dAinc`, then `SECTION Xle Yle Zle Chord Ainc [Nspan Sspace]` with `NACA xxxx [X1 X2]`, `AIRFOIL [X1 X2]` + inline pairs, or `AFILE [X1 X2] filename` ("x/c, y/c coordinates run from TE, to LE, back to the TE again in either direction"), `CLAF CLaf` (dcl/dα = 2π·CLaf), `CDCL`, `CONTROL`. Two semantics matter: "the local chord and incidence are linearly interpolated between defining sections" and "Ainc is used only to modify the flow tangency boundary condition on the airfoil camber line, and does not rotate the geometry of the airfoil section itself." AVL therefore stores stations only, derives everything, has no loft rule but linear, and its twist is an aerodynamic not geometric rotation. Export from the workbench: sample authored stations **plus** enough inspection slices that linear interpolation of chord/`Zle`/`Xle` stays within a stated tolerance of the evaluated distribution (Inferred).

**XFLR5** (GPL — process/export only). Plane/wing definitions export/import as XML ("Plane/Sections/Section/Chord", `Left_Side_FoilName`/`Right_Side_FoilName`) and, since a developer thread, as a plain-text `.xwimp` with one line per segment: `y chord offset dihedral twist nx ny x-dist y-dist right-foil left-foil` [S28 — tertiary: SourceForge threads and a third-party generator; **Flagged** until an exported XML is opened]. The `.wpa` project file is XFLR5's binary/project container (Flagged, recall). Note that XFLR5 describes dihedral and twist **per panel between sections** and both sides' foils per section, and that "twist" applies about the section's quarter chord or LE depending on version (Flagged).

**OpenVSP `.vsp3`** is XML (Flagged, recall — not opened; the airfoil-export wiki page [S7] and the STEP release notes [S34] were). Its wing component is built from "stacked, parallel sections" with XSec parameters (span, root/tip chord, sweep, sweep location, twist, twist location, dihedral per section) and blending controls (already Verified in the repo's parametric-geometry source, §"3D grammar"). Twist has an explicit **twist location** (fraction of chord) — a stored twist axis, unlike AVL's implicit camber-line BC. Licence: NASA Open Source Agreement 1.3 (Flagged, recall; GitHub reports `NOASSERTION` [S31]) — permissive-compatible for **file interchange**, but do not link.

**MachUpX aircraft JSON** [S24]. `"wings": {id: {...}}` with `semispan`, `dihedral`, `sweep`, `twist` and `chord` each given as "a float, array (span fraction vs. angle), CSV file path, or function", `chord` also `["elliptic", root_chord]`, `side` right/left/both, `connect_to` (ID, location root/tip, dx/dy/dz/y_offset), `airfoil` as a name or an array mapping span fractions to airfoil names, and `grid` (N, distribution linear/cosine_cluster/explicit). Airfoils are separate JSON objects typed `linear` / `database` / `poly_fit`, with `outline_points` (TE→TE) or `NACA` used **only for 3D export, not aerodynamics**. Twist "can include step changes by specifying the same span location twice". MIT [S31]. This is the closest published analogue to the workbench's distribution curves: distributions are functions over span fraction, stations are implicit, symmetry is a `side` flag, and the section polar is decoupled from the section outline.

**AeroSandbox `Wing`/`WingXSec`** (MIT [S31]): Python objects; `WingXSec(xyz_le, chord, twist, airfoil)` per section with `symmetric` on the wing — a station grammar equivalent to AVL's, with the airfoil object attached (Flagged: the class signature is recall; only `airfoil.py` was opened [S4]).

**Shape3d `.s3d` / `.s3dx`.** Tertiary sources state `.s3dx` is XML-based, cannot be opened by V8 or older, and saving to V8 `.s3d` loses "the 3D layers and multi-curves edition" [S29]. Whether `.s3d` is binary was **not established** (Flagged). Shape3d publishes a "Shape3d to XFLR5" tutorial [S29], i.e. its foil path is section `.dat` + XFLR5 plane definition, not a CAD surface — a precedent that the surf/foil market already round-trips through Selig `.dat` and XFLR5 XML.

**DELFTship `.fbm` and BoardCAD `.brd`**: not opened this session — Flagged; both are application-native formats and neither is a plausible interchange target for v1.

**CFD-Bench station grammar** (repo, [S39]): `assembly { surface { axis, mirror, loft, station y= chord= inc= section= [x=] [z=] } }` — one line per station, section by catalog id, mirror flag, loft `linear|spline`, derived quantities absent. The spec's A4 contract supersedes station-only storage: authored stations **plus** five distribution-curve definitions (mode, controls, weights, constraints) form the surface of record [S40].

##### Comparison table — what each grammar stores vs derives

| Grammar | Encoding | Span description | Per-section profile override | Twist axis | Symmetry | Loft rule | Stored vs derived | Provenance fields | Label |
|---|---|---|---|---|---|---|---|---|---|
| AVL `.avl` | keyword text | explicit `SECTION` rows | yes (`AFILE`/`NACA`/`AIRFOIL` per section) | none — `Ainc` is a camber-line BC, geometry not rotated | `YDUPLICATE` | linear only (implicit) | stations stored; area/AR **input** as `Sref/Bref` (can disagree with geometry) | none | Verified [S6] |
| XFLR5 XML / `.xwimp` | XML / one line per segment | explicit sections with per-panel dihedral/twist | left/right foil names per section | version-dependent (Flagged) | left/right explicit | linear panels | stations stored; area derived in-app | none | Flagged [S28] |
| OpenVSP `.vsp3` | XML | stacked sections with per-section span/chord/sweep/twist/dihedral | per XSec airfoil (NACA/file/CST/Bezier) | explicit twist location | symmetry flags | blending/continuity controls | parameters stored; derived on load | none formal | Flagged (recall) |
| MachUpX JSON | JSON | distributions as functions of span fraction | airfoil list vs span fraction | implicit (Flagged) | `side` | linear between given points | distributions stored; everything else derived | none | Verified [S24] |
| AeroSandbox objects | Python | `WingXSec` list | airfoil object per xsec | about LE (Flagged) | `symmetric` | linear | stations stored | none | Flagged |
| Shape3d `.s3dx` | XML | orthogonal curves + slices | per slice | n/a | n/a | fair surface | curves stored | none known | Flagged [S29] |
| CFD-Bench grammar | line DSL | stations | `section=` per station | about LE (spec A4) | `mirror` | `linear|spline` | stations only; derive-never-store | catalog id + hash intended | Verified [S39] |
| Workbench A4 (target) | JSON | **five distribution curves + authored stations** | profile revision per station | LE, positive nose-up (spec) | mirror, Break symmetry | correspondence/continuity/closure | curves + stations stored; span/area/AR derived | hashes, source, tool/version required | Verified [S40] |

Observation (Inferred): every published grammar stores stations or distributions but none stores **curve editing state** (control weights, modes, locks). That state is the workbench's differentiator and its round-trip risk; it has no external precedent to borrow from, so DOC-02's "controls, weights, mode, locks" survive only in the native format.

#### 3. Exact-surface CAD interchange

**STEP (ISO 10303).** Part 21 is the clear-text exchange structure (`ISO-10303-21; HEADER; FILE_DESCRIPTION/FILE_NAME/FILE_SCHEMA; ENDSEC; DATA; #n=ENTITY(...); ENDSEC; END-ISO-10303-21;`) — Flagged as recall for the exact tokens; the entity definitions were opened [S8]. `B_SPLINE_SURFACE` (a `BOUNDED_SURFACE`) carries `u_degree`, `v_degree`, `control_points_list: LIST OF LIST OF cartesian_point`, `surface_form`, `u_closed`, `v_closed`, `self_intersect`; `B_SPLINE_SURFACE_WITH_KNOTS` adds `u_multiplicities`, `v_multiplicities` (`LIST [2:?] OF INTEGER`), `u_knots`, `v_knots` (`LIST [2:?] OF parameter_value`) and `knot_spec: knot_type`, with derived `knot_u_upper = SIZEOF(u_knots)` and where-rules calling `constraints_param_b_spline` plus multiplicity checks (WR3, WR4). Rational surfaces are the complex entity `(B_SPLINE_SURFACE_WITH_KNOTS ... RATIONAL_B_SPLINE_SURFACE(weights_data))` [S8]. Consequence (Inferred): the writer must emit knots in the **multiplicity-compressed** form (distinct knots + multiplicities), not the flat knot vector rhino3dm/our maths hold; knot counts must satisfy Σmult = n_control + degree + 1 — a classic off-by-one in home-made writers.

*Bare surface vs face/shell.* A file that contains only `B_SPLINE_SURFACE_WITH_KNOTS` entities inside a `GEOMETRICALLY_BOUNDED_SURFACE_SHAPE_REPRESENTATION` imports as **surface bodies**, not as a solid (Flagged: entity name from recall; behaviour Inferred from OpenVSP's own description). OpenVSP's release notes [S34] draw exactly this line: untrimmed files "will not have slivers, gaps, and holes that can be introduced by the trimming process", while trimmed export "can form a watertight BREP solid" and the shell representation "is by definition not a single watertight BREP but is a bunch of separate trimmed surfaces". For a mold-CAM round trip the CAM system needs at least a **face set with a consistent outward normal**, ideally a closed shell (`ADVANCED_FACE → CLOSED_SHELL → MANIFOLD_SOLID_BREP`); a lofted wing that is closed at the trailing edge and capped at the tip is a candidate for a closed shell **only if** the tip cap and TE closure are themselves faces (Inferred). The repo's decision-0001 "one entity type, OpenVSP proves it" therefore proves *surface* export, not *solid* export — see **Contradicts existing repo knowledge** below.

*Application protocols.* AP203 (configuration-controlled design), AP214 (automotive), AP242 (managed model-based 3D engineering, merges 203/214, adds PMI). All three carry the same B-spline geometry entities; the choice affects the `FILE_SCHEMA` string and which header/product entities must be present (Flagged, recall). OpenVSP writes AP203 (repo Verified, [S41]). Fusion, Mastercam, PowerMill and Vectric all read AP203/AP214/AP242 (Flagged: vendor claims not opened this session; the EXP-02 gate requires an actual open-and-measure test).

*Writers.* STEPcode: "3-clause BSD", generates C++ (and experimental Python) from EXPRESS schemas, reads/writes Part 21, SDAI; originated at NIST, renamed 2012, developed with BRL-CAD; pushed 2026-09-10 [S10][S31]. `ruststep` (Apache-2.0) last released 0.4.0 on 2024-09-20 [S30] — usable for reading our own entity, stale for anything else (Flagged as maintenance risk). An own bounded writer (decision-0001 option 4) remains viable: the surface entity plus the minimal AP203 header/product/shape-representation scaffold is a few hundred lines, but it is **unproven until opened by CAM** (decision-0001 residual risk stands).

*Units in STEP.* Length unit is declared via `SI_UNIT(.MILLI., .METRE.)` or `(.METRE.)` inside the geometric representation context with an `UNCERTAINTY_MEASURE_WITH_UNIT` (global tolerance) (Flagged, recall). Millimetre is the CAM convention; the writer must state it and the EXP-02 dialog must show it. Part 21 reals are text: write shortest round-trip (17 significant digits max) so that the 1 µm DOC-02 tolerance is not consumed by serialisation (Inferred; see §6 probe).

**IGES.** Entity 128 rational B-spline surface, 126 rational B-spline curve, 144 trimmed surface, 143 bounded surface (Flagged, recall — the IGES 5.3 spec was not opened). OpenVSP writes IGES with the same trimmed/untrimmed options [S34]. IGES is legacy: writers exist, it lacks a solid model in practice, and CAM vendors accept it but prefer STEP (Flagged, practitioner consensus).

**3DM / openNURBS.** rhino3dm: MIT; Python/JS/.NET (NuGet `Rhino3dm`) on Windows/macOS/Linux; "points, point clouds, NURBS curves and surfaces, polysurfaces (B-Reps), meshes, annotations, extrusions, and SubDs"; "read and write all of the above information to and from the .3dm file format"; no STEP/IGES [S9]. Version 8.17.0 referenced in the README example (date not shown) [S9]. 3DM is the right **secondary** exact-surface export (Rhino is common in the surfboard/foil shaping world) and a free way to get a NURBS evaluator and Brep container in-process.

**Parasolid `.x_t/.x_b`, ACIS `.sat/.sab`.** Proprietary kernel formats; readable by Fusion/Mastercam but writable only through licensed kernels or OCCT-class translators — **never** for this project (Flagged, recall; consistent with decision-0001).

**glTF 2.0.** Khronos, mesh-only in core (triangles, PBR, animation); no NURBS; units are metres by convention (Flagged, recall — spec not opened). Suitable for viewport sharing/web preview, useless for CAM. **USD.** `UsdGeomNurbsPatch` "encodes a rational or polynomial non-uniform B-spline surface, with optional trim curves", following RenderMan's encoding; attributes `uVertexCount/vVertexCount`, `uOrder/vOrder`, `uKnots/vKnots`, `uRange/vRange`, `uForm/vForm` (open/closed/periodic), `pointWeights` (length must match points), flattened `trimCurve*` arrays [S26]. USD is a DCC/animation interchange; no CAM reads it (Inferred). **OBJ** has free-form support — `cstype [rat] bmatrix|bezier|bspline|cardinal|taylor`, `deg degu degv`, `surf s0 s1 t0 t1 v1 v2…`, `parm u|v …`, `trim`, `hole`, `vp u v w`, `end` — and "no unit metadata" [S25]; practically no importer beyond Maya/Blender-era tools reads the curve statements (Flagged). **DXF** `SPLINE` entity carries degree/knots/control points/weights for curves only (Flagged, recall) — useful for 2D section/planform export to laser/CNC-router 2D workflows (Vectric reads DXF), not for surfaces.

**What "opens as a smooth surface" requires (Inferred from [S8][S34] and decision-0001):** one non-rational or rational B-spline surface of degree ≤ 3 in each direction (higher degrees import but some CAM kernels degree-reduce; OpenVSP offers a "to cubic" option — Flagged), knots valid per the where-rules, no repeated interior knots unless a deliberate kink, no near-degenerate control rows at the tip (collapse the tip to a proper cap or leave it open), consistent parameter direction, and — for the CAM to see a **body** — faces with an outer loop, oriented, sewn within the file's uncertainty. Acceptance is EXP-02's "open-and-measure": import in Fusion, measure chord at three stations and max deviation ≤ 1 µm against the native evaluation; repeat in one more CAM system before the format is marked released.

#### 4. Mesh formats for printing and CFD

| Format | Structure | Boundary markers | Units | Cell types | Reader/writer (licence) | Label |
|---|---|---|---|---|---|---|
| STL ASCII/binary | unindexed triangle soup, per-facet normal; binary = 80-byte header + uint32 count + 50-byte facets | none | **none** (mm by printer convention) | tri | own writer (trivial); Rust `stl_io` MIT 0.11.0 (2026-03-15) [S30]; meshio MIT | Flagged (layout recall) |
| 3MF | ZIP/OPC, `/3D/3dModel.model` XML, relationships; `unit` ∈ {micron, millimeter, centimeter, inch, foot, meter}, default millimeter; meshes MUST be manifold ("every triangle edge … shares … exactly 1 other triangle") with outward normals | per-object metadata; components | **yes** | tri (extensions add beam lattice, slices) | lib3mf BSD-2-Clause [S31]; own writer feasible (ZIP + XML) | Verified [S11] |
| OBJ | indexed `v/vt/vn/f`, groups | `g`/`o` names | none | polygons, free-form (rare) | own; meshio | Verified [S25] |
| PLY | header-described vertex/face lists, ASCII/binary | none | none | polygons | meshio | Flagged |
| Gmsh MSH 4.1 | `$MeshFormat` (version, file-type, data-size), `$PhysicalNames`, `$Entities`, `$Nodes`, `$Elements`; ASCII or binary; only elements in a physical group are written unless `Mesh.SaveAll` | physical groups | "There are no units in Gmsh" | full set incl. tet/hex/prism/pyramid | Gmsh itself **GPL** (process only); meshio MIT | Verified [S12] |
| SU2 `.su2` | `NDIME`, `NPOIN` + coords, `NELEM` + `vtk_type n0 n1…`, `NMARK`, `MARKER_TAG`, `MARKER_ELEMS`; 0-based, VTK ordering | marker tags | none | 3 line, 5 tri, 9 quad, 10 tet, 12 hex, 13 prism, 14 pyramid | own writer trivial; meshio | Verified [S13] |
| CGNS | SIDS data model + MLL (C/Fortran) over HDF5 (or ADF); AIAA Recommended Practice; boundary conditions and `DimensionalUnits`/`DataClass` are in the SIDS | BC nodes, families | **yes** | structured + unstructured | CGNS library (zlib-style licence — Flagged; site is CC0-dedicated content [S14]); SU2 reads CGNS "preserving boundary names as marker tags but ignoring embedded BCs" [S13] | Verified [S14][S13] |
| OpenFOAM `polyMesh/` | directory: `points`, `faces`, `owner`, `neighbour`, `boundary` (+ optional zones/sets); internal faces first with normal into the higher-label cell, boundary faces grouped by patch with outward normal; `boundary` = `{ name { type patch; nFaces; startFace; } }`; `owner` header carries `nCells` | patches | none (metres by convention; `convertToMeters` lives in `blockMeshDict`, not polyMesh — Flagged) | polyhedral | own writer feasible; OpenFOAM utilities (GPL, process) | Verified [S15] |
| Exodus II | NetCDF/HDF5 container, element blocks, node/side sets, time steps | side sets | none | FE types | SEACAS (BSD-3 — Flagged; GitHub `NOASSERTION` [S31]); meshio | Flagged |
| VTK legacy `.vtk` / XML `.vtu .vtp .vts .vtr .vti .vtm .pvd .pvtu` | legacy: version line, title, ASCII/BINARY, DATASET type, POINT_DATA/CELL_DATA; XML: appended/base64/zlib | none (field arrays) | none | full set | VTK BSD-3 (Flagged; `NOASSERTION` [S31]); meshio; own writer for `.vtu` ASCII/appended is small | Flagged (docs 404 this session) |
| Fluent `.msh/.cas` | zone-based sections, documented in Ansys manuals; `.cas` also carries setup | zones | none (scale in case) | full set | meshio reads ANSYS msh | Flagged |
| Tecplot `.dat/.plt/.szplt` | ASCII documented; binary proprietary/TecIO | zones | none | ordered/FE | meshio reads Tecplot .dat; SU2 writes Tecplot [S17] | Flagged |
| EnSight Gold | case file + geometry + per-variable files | parts | none | full set | VTK/ParaView read | Flagged |

`meshio` (MIT) reads/writes Abaqus, ANSYS msh, AVS-UCD, CGNS, DOLFIN, Exodus, FLAC3D, H5M, MDPA, Medit, MED, Nastran, Netgen, Gmsh, OBJ, OFF, PERMAS, PLY, STL, Tecplot .dat, TetGen, SVG, SU2, UGRID, VTK, VTU, WKT, XDMF [S16] — but its last push was 2024-07-23 [S31], so treat it as a **Python sidecar for conversion in development and tests**, not as a product dependency (Inferred). It is not installed in the local Python (`ModuleNotFoundError`, observed).

**The STL units problem.** STL carries no unit; slicers assume mm and CAD exporters differ (Fusion exports mm by default; some tools export m). The exporter must (a) write mm for printing, (b) put the unit in the filename or an accompanying manifest, and (c) prefer 3MF, which declares the unit, for printing (Inferred from [S11]; the mm default is Flagged recall). For CFD the workbench controls both ends: write metres, write the unit into the run document, and never trust an imported mesh's scale without asking.

#### 5. CFD results and data formats

**OpenFOAM.** A case is a directory: `system/`, `constant/polyMesh/`, time directories `0/`, `100/`… each holding one file per field with a `FoamFile` header (`version`, `format ascii|binary`, `class volScalarField…`, `object`) (Flagged: header keys from recall; the polyMesh part is Verified [S15]). Function objects write under `postProcessing/<name>/<startTime>/` (Flagged, recall); OpenFOAM-12's `forces` "calculates the forces and moments by integrating the pressure and skin-friction forces over a given list of patches", requires `patches`, `CofR`, `rho`/`rhoInf` ("for incompressible cases, set rho to rhoInf"), and "writes the forces/moments into the file `<timeDir>/forces.dat` and bin data (if selected) to `<timeDir>/forces_bin.dat`" [S32]. The ESI (openfoam.com) line writes `force.dat`/`moment.dat` with separate total/pressure/viscous columns instead — the two forks differ here (Flagged; the ESI page was 404 this session). `forceCoeffs` adds `magUInf`, `lRef`, `Aref`, `liftDir`, `dragDir`, `pitchAxis` and writes `coefficient.dat` (Flagged, recall). `yPlus`, `wallShearStress` write **fields** into the time directory; `probes` and `sample`/`surfaces` write raw/CSV/VTK under `postProcessing/` (Flagged, recall). ParaView reads a case through the built-in reader ("PVFoamReader and vtkPVFoam libraries" via `paraFoam`) with time/region/field selection in the Properties panel [S33]; the empty `<case>.foam` file trick for ParaView's own reader was not confirmed on the page opened (Flagged, recall — widely used).

**SU2.** `HISTORY_OUTPUT` selects fields (residuals `RMS_DENSITY…`, coefficients `DRAG`, `LIFT`, `EFFICIENCY`, iteration indices, `WALL_TIME`) written to the history file (`CONV_FILENAME`, CSV or Tecplot); `OUTPUT_FILES` ∈ {RESTART, RESTART_ASCII, PARAVIEW, PARAVIEW_ASCII, PARAVIEW_MULTIBLOCK, SURFACE_PARAVIEW(_ASCII), TECPLOT(_ASCII), SURFACE_TECPLOT(_ASCII), CSV, SURFACE_CSV, STL_BINARY, STL_ASCII, MESH}, default `(RESTART, PARAVIEW, SURFACE_PARAVIEW)`; `VOLUME_OUTPUT` defaults to `COORDINATES,SOLUTION,PRIMITIVE`; `OUTPUT_WRT_FREQ` controls frequency [S17]. Default filenames `history.csv`, `flow.vtu`, `surface_flow.vtu`, `restart_flow.dat` (Flagged, recall). SU2 is LGPL-2.1 (Flagged, recall; `NOASSERTION` [S31]) — process invocation only, which the constraints already allow.

**Containers for our own results.**
- *HDF5*: BSD-3-style licence (retain notice, reproduce in binary distributions, no endorsement) [S18]. PureHDF: "a pure C# library without native dependencies" for reading **and writing** (groups, datasets, attributes, chunking, compression filters, async read), v3 targets .NET 8+, follows the HDF5 file-format spec v1.10; MIT [S19]. `HDF.PInvoke` (HDF Group) last pushed 2024-02-20 and needs native binaries [S31] — prefer PureHDF. Rust: `hdf5-metno` 0.15.0 (2026-09-18, MIT OR Apache-2.0), a maintained fork of `aldanor/hdf5-rust` [S30].
- *Parquet/Arrow*: Parquet.Net "fully managed", MIT, .NET 8 and 10, all types/encodings/codecs, row groups, class serialisation, `Microsoft.Data.Analysis` DataFrame integration [S20]; Rust `parquet`/`arrow` 60.0.0 (2026-09-15, Apache-2.0) [S30]. Parquet is columnar and immutable per file — ideal for **sweep tables** (one row = one sample × one quantity or one row = one sample with columns), poor for arrays of fields.
- *Zarr / NetCDF*: chunked N-D arrays; NetCDF-4 is HDF5 underneath; Zarr v3 is directory-of-chunks with JSON metadata — Git-hostile at scale but cloud-friendly; no mature pure-.NET writer known (Flagged, recall). Not needed while HDF5 covers volumetric results.

**Reference vs embed (Inferred from the above and from A3's Analysis run/Sweep/Field evidence).** The run document (`*.cfdw-run.json`) should embed: the input snapshot hashes (surface revision, profile revisions, water record, method/settings), the resolved sample schedule, the backend identity/version/smoke-test id, per-sample status and timing, scalar outcomes (CL, CD, CM, residual floors, y⁺ range, Cp_min) and the **inventory** of field files with their path, format, unit, variable names, byte size and SHA-256. It should reference: meshes, solver case directories, `.vtu`/`.foam` fields, history CSVs and any HDF5/Parquet sidecar. It should never embed a field. Deleting a referenced file demotes the run to "evidence missing", not to "invalid".

#### 6. Native project format design (`.cfdw.json`)

##### Evidence base

- **JSON Schema.** The current specification is **2020-12** (succeeding 2019-09), split into Core and Validation, with meta-schemas under `draft/2020-12/schema`; no Hyper-Schema for 2020-12 [S27]. Release notes cover the 2019-09→2020-12 changes (`prefixItems`/`items`, `$dynamicRef`, `unevaluatedProperties`) — Flagged as recall since the release-notes page was not opened. `$schema: "https://json-schema.org/draft/2020-12/schema"` is the identifier to write (Inferred from the path shown).
- **Canonicalisation for hashing.** RFC 8785 JCS (Informational): properties sorted by UTF-16 code units "independent of locale settings"; numbers "MUST be serialized according to Section 7.1.12.1 of ECMA-262" (shortest round-trip, ES `Number.prototype.toString`); "whitespace between JSON tokens MUST NOT be emitted"; strings escaped per ECMAScript; numbers beyond IEEE-754 double "RECOMMENDED to represent … as JSON strings"; purpose: cryptographic operations over a canonical counterpart while the wire form stays free [S21].
- **.NET number formatting.** MS Learn: since .NET Core 3.0 `Double.ToString()`/`"G"` gives the "smallest round-trippable number of digits"; `"R"` is "recommended for the BigInteger type only", and in .NET Framework/.NET Core < 3.0 "R" fails to round-trip some doubles; `"G17"` always round-trips but prints noise digits [S22].
- **Probe (executed 2026-09-20, .NET runtime 10.0.7, `dotnet run app.cs`, scratchpad `spike-json/app.cs`) [S36]:**

| value | `ToString()` de-DE | Invariant | `"R"` | `"G17"` | `Utf8JsonWriter` | bit-exact round trip |
|---|---|---|---|---|---|---|
| 0.1 | `0,1` | `0.1` | `0.1` | `0.10000000000000001` | `0.1` | yes |
| 1/3 | `0,3333333333333333` | `0.3333333333333333` | same | `0.33333333333333331` | `0.3333333333333333` | yes |
| 1e-7 | `1E-07` | `1E-07` | `1E-07` | `9.9999999999999995E-08` | `1E-07` | yes |
| 0.30000000000000004 | … | `0.30000000000000004` | same | same | same | yes |
| −0.0 | `-0` | `-0` | `-0` | `-0` | `-0` | yes |
| 1e21 | `1E+21` | `1E+21` | `1E+21` | `1E+21` | `1E+21` | yes |
| 2.5e-5 | `2,5E-05` | `2.5E-05` | `2.5E-05` | `2.5000000000000001E-05` | `2.5E-05` | yes |
| NaN | — | — | — | — | **ArgumentException** ("cannot be written as valid JSON") | n/a |

Observed consequences (Verified by execution):
  1. `Utf8JsonWriter` is culture-invariant and shortest-round-trip; storage adds **zero** geometric error, so DOC-02's 1 µm budget is spent entirely on evaluation, not serialisation.
  2. Its exponent form (`1E-07`, `1E+21`, `-0`) is **not** JCS (`1e-7`, `1e+21`, `0`). A JCS hash must be computed by a dedicated canonicaliser, and the file bytes are not the hash input.
  3. NaN/±Infinity throw: "not recorded" must be `null` or an explicit status field, never a numeric sentinel — which is also what IO ("degrades to not recorded") demands.
  4. `"G17"` prints noise (`0.10000000000000001`) — never use it for a Git-diffed file; `"R"` on modern .NET equals shortest but the docs steer away from it; use the default/`Utf8JsonWriter`.
  5. .NET 10 file-based apps ship with reflection-based `JsonSerializer` **disabled** (`InvalidOperationException: Reflection-based serialization has been disabled`); the product must use `JsonSerializerContext` source generation (also required for trimmed/AOT Avalonia builds).
- **KiCad precedent** [S23]: S-expressions derived from Specctra DSN; "human readability is a design goal"; "all values are given in millimeters. Exponential floating point values are not used for readability purposes"; nanometre board precision ("six decimal places or 0.000001 mm"); `version YYYYMMDD` token; UUID identity; legacy timestamps re-encoded as UUIDs on migration. KiCad chose a *fixed decimal count* over shortest-round-trip — readable diffs, at the cost of a quantisation floor (1 nm), which is well below any CAM tolerance.
- **OpenVSP `.vsp3`**: XML with every parameter as a `Parm` element carrying value and id (Flagged, recall). **Blender `.blend`**: binary DNA-described; **Fusion**: cloud-native; **Onshape**: cloud DB — none is a usable precedent for a Git-friendly local file (Flagged, recall). **Shape3d** `.s3dx` XML [S29] is the closest in-domain precedent and it is application-private.
- **Units.** UCUM (Unified Code for Units of Measure) and QUDT (RDF units ontology) provide machine-readable unit identifiers (Flagged, recall; neither site opened this session). Options: units in field names (`chord_m`, `inc_rad` — the repo sketch), a per-document unit declaration (`"units": "SI"`), or per-value objects (`{"value": 0.141, "unit": "m"}`).

##### Critique of the repo's `.cfdw.json` sketch (proposal-sequence §3.2, [S42])

| Sketch element | Verdict | Evidence / reason |
|---|---|---|
| `"format": "cfdw-wing", "version": 1` | Keep; add `$schema` (2020-12 URI), `generator` (app name+version, KiCad precedent [S23]) and make `version` **semver-like with separate major/minor** (major = breaking; minor = additive optional fields — EXP-01 "unknown optional context retained/read-only or refused before save") | [S23][S27], spec EXP-01/DOC-04 |
| `"units": "SI"` + `_m`/`_rad` suffixes | Keep **both**: suffix on every numeric field (self-describing rows in a diff; GAP-04 "carry units in the type") and the document-level declaration as a redundancy check; reject a file whose suffixes disagree with the declaration | GAP-04 [S43]; Inferred |
| `"frame": "le-root-xaft-zup"` | Keep; make it an enum with exactly one v1 value and a documented mapping to AVL's X-downstream/Y-right/Z-up [S6] and to OpenVSP | spec A4 units section [S40] |
| `generative` block as recipe | Keep as `recipe` with `authoritative: true|false` flipped to `false` by the first station/curve edit (one-way burst, §3.5 of the sequence) — but never delete it (provenance) | [S42][S39] |
| `surface.stations[]` as the geometry | **Insufficient.** A4 requires the five distribution curves with mode, controls, weights and constraints. Stations become `authored_stations[]` carrying identity (UUID), span location, profile revision reference and per-channel constraints; `distributions.{sweep,chord,elevation,twist,thickness}` carry `mode`, `controls[]`, `constraints[]`, and `continuity` | spec A3/A4 [S40] |
| `"section": "E817"` string | Replace with `{ "profile": "<profile-revision-id>" }` resolving to a `profiles` table in the same document holding original coordinates, detected format, normalisation transform, source URL/licence note, SHA-256 of the original bytes, and the editable curve definition | CAT-01/02, ANA-09 [S40] |
| Derived quantities absent | Keep — but **write a `derived_check` block** (span, S, AR at 1e-6 relative) that a reader must recompute and compare; disagreement is a corruption/version signal, not a second authority (derive-don't-store still holds: the block is a checksum, not an input) | DM "derive don't store"; Inferred |
| Results "in a sidecar keyed by content hash" | Keep; specify the hash = SHA-256 over the **JCS form** of the `surface_revision` subtree (not the whole file, so metadata edits do not orphan runs), stored as `revision_hash` on save and verified on load | RFC 8785 [S21]; ANA-09 |
| Single `surface` | Make `surfaces[]` (front wing, stabiliser, strut are in the grammar already) with v1 UI limited to one; the loader must accept and preserve others read-only | [S39], EXP-01 |
| No history | Add `history[]` as an **append-only** list of accepted edits (`{id, parent, at, kind, summary, revision_hash}`) — the A3 "Design revision" chain; store full snapshots per revision in a `revisions/` sidecar or in-file with a size cap, and reconstruct undo from the live model, not from the file | A3 [S40]; parametric-geometry "append-only" [S39] |
| Numbers | Shortest round-trip via `Utf8JsonWriter`; **fixed key order** (schema order, not sorted — readability) and **one key per line** with 2-space indent so Git diffs are one row = one change; arrays of controls one element per line | probe [S36]; KiCad [S23] |
| No identity | Every station, control, constraint and profile carries a UUID (v7 preferred for sortable time-ordering — Flagged) so that history entries and assistance proposals can reference them by identity, not by index | A3 "referenced by identity" |
| Large data | Never in `.cfdw.json`. Polars → Parquet sidecar (MIT reader); fields → HDF5 or the solver's own VTK files referenced by hash | [S19][S20] |

##### Recommended document shape (Inferred synthesis)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "format": "cfdw",
  "format_version": { "major": 1, "minor": 0 },
  "generator": { "name": "CFD-Workbench", "version": "0.1.0+abc123" },
  "units": "SI", "frame": "le-root-xaft-ystarboard-zup",
  "design": { "id": "…uuid…", "name": "Downwind 1100", "revision_hash": "sha256:…", "context": { } },
  "profiles": { "<profile-rev-id>": { "source": { "kind": "uiuc-dat", "url": "…", "sha256": "…", "detected_format": "lednicer" },
                                       "original_points": [[1.0,0.0], …], "normalization": { "translate": [0,0], "rotate_rad": 0, "scale": 1 },
                                       "curve": { "upper": { "mode": "smooth", "controls": [ … ] }, "lower": { … } } } },
  "surfaces": [ { "id": "…", "role": "front-wing", "mirror": true,
                  "distributions": { "sweep": { "mode": "through-points", "anchors": [ … ], "constraints": [ … ] },
                                     "chord": { "mode": "smooth", "controls": [ { "id": "…", "s": 0.3, "value_m": 0.108, "weight": 1.0 } ] },
                                     "elevation": { … }, "twist": { … }, "thickness": { … } },
                  "authored_stations": [ { "id": "…", "s_m": 0.0, "profile": "<profile-rev-id>", "constraints": [ … ] } ],
                  "loft": { "correspondence": "arc-length", "continuity": "G2", "closure": { "tip": "cap", "te": "closed" } } } ],
  "recipe": { "authoritative": false, "area_m2": 0.14, "aspect_ratio": 7.0, "…": "…" },
  "derived_check": { "span_m": 0.98995, "area_m2": 0.14, "aspect_ratio": 7.0, "tolerance_rel": 1e-6 },
  "history": [ { "id": "…", "parent": null, "at": "2026-09-20T10:00:00Z", "kind": "recipe.generate", "revision_hash": "sha256:…" } ],
  "runs": [ { "ref": "runs/2026-09-20T10-05-00Z-a1b2.cfdw-run.json", "sha256": "…", "status": "complete" } ],
  "extensions": { }
}
```

Rules: unknown top-level keys and unknown keys inside `extensions` are preserved verbatim on save (EXP-01); unknown keys **inside** `surfaces`/`profiles` with a higher `minor` are preserved; a higher `major` refuses to open with the path named (DOC-04); migrations write `<name>.cfdw.json.v1.bak` first (DOC-04 "migrations preserve an original copy").

#### 7. Grammars and DSLs

- **OpenSCAD** (GPL-2.0 — Flagged; GitHub `NOASSERTION` [S31]) is a functional CSG language: text is the model, no GUI editing state, diffs are semantic. **CadQuery** (Apache-2.0 — Flagged; `NOASSERTION` [S31]) and **build123d** (Apache-2.0, pushed 2026-09-20 [S31]) are Python APIs over OCCT: "code as CAD", excellent for reproducibility and parameter sweeps, poor for direct-manipulation editing — every drag would have to be re-expressed as a code edit. **OpenVSP** exposes an AngelScript/Python API over its parameter tree; the `.vsp3` file remains a schema, the script is an *operation* language (Flagged, recall).
- **The CFD-Bench station DSL** [S39] is readable and diffable, but it cannot carry curve controls, weights, constraints or UUIDs without growing a full grammar — at which point it is JSON with a custom parser and no validator.
- **When a DSL wins (Inferred):** the artifact is authored by humans in a text editor, reproducibility across versions matters more than tool state, and operations (not states) are the unit of sharing — e.g. an **optimisation or sweep script**, an assistant "recipe", a test fixture. **When a schema wins:** the artifact is produced by direct manipulation, must round-trip editing state losslessly (DOC-02), needs machine validation (JSON Schema 2020-12 [S27]) and canonical hashing (JCS [S21]), and must be readable by other tools without a bespoke parser.
- **Recommendation:** schema for the document of record; a small **command grammar** (JSON operations: `set-control`, `add-station`, `apply-recipe`, `import-profile`) as the history entry kind and the assistant's structured-output target (the sequence's "typed object, never coordinates"). A text DSL for *scripts* (sweeps/optimisation) is a later door, not v1.

#### 8. Licence and platform notes for candidate libraries (2026-09-20)

| Need | .NET | Rust | Python sidecar (dev/test only) | Licence (SPDX) | Evidence |
|---|---|---|---|---|---|
| JSON read/write, schema | `System.Text.Json` (+ source-gen context); JsonSchema.Net (MIT — Flagged) | `serde`/`serde_json` (MIT OR Apache-2.0) | — | MIT | [S30][S36] |
| JCS canonical hash | own (~150 lines; ES number formatting is the hard part) | `serde_jcs` (Flagged) | — | — | [S21] |
| NURBS maths / 3DM | Rhino3dm NuGet (MIT, v8.17) | `truck` (MIT, 1.0.0 dated 2020-09-20 — stale, Flagged) | rhino3dm.py | MIT | [S9][S30] |
| STEP write/read (own entity) | own bounded writer; STEPcode via P/Invoke (BSD-3) | `ruststep` (Apache-2.0, 0.4.0, 2024-09-20) | — | BSD-3 / Apache-2.0 | [S10][S30] |
| STL | own | `stl_io` (MIT, 0.11.0, 2026-03-15) | meshio, numpy-stl | MIT | [S30] |
| 3MF | own (ZIP+XML) or lib3mf (BSD-2-Clause, C++ with C# bindings — Flagged) | — | — | BSD-2-Clause | [S31][S11] |
| SU2 mesh, OpenFOAM polyMesh, VTU | own writers (text grammars) | own | meshio (MIT, last push 2024-07-23) | MIT | [S13][S15][S16] |
| HDF5 | PureHDF (MIT, pure C#, .NET 8+) | `hdf5-metno` (MIT OR Apache-2.0, 0.15.0) — links libhdf5 (BSD-3-style) | h5py (BSD-3 — Flagged) | MIT / BSD-3 | [S18][S19][S30] |
| Parquet/Arrow | Parquet.Net (MIT, .NET 8/10); Apache.Arrow (Apache-2.0 — Flagged) | `parquet`/`arrow` 60.0.0 (Apache-2.0) | pyarrow (Apache-2.0) | MIT / Apache-2.0 | [S20][S30] |
| CGNS | none pure-managed known (Flagged); MLL via P/Invoke (zlib-style) | `cgns-sys` (Flagged) | pyCGNS/h5py | zlib-style (Flagged) | [S14] |
| Solvers (process only) | SU2 (LGPL-2.1, Flagged), OpenFOAM (GPL-3.0, Flagged), Gmsh (GPL [S12]), XFLR5 (GPL, Flagged) | — | — | copyleft, invoked as processes | [S12][S31] |
| Airfoil tooling | own parser | own | AeroSandbox (MIT) for fixtures and cross-checks | MIT | [S31][S4] |

Platform note: PureHDF and Parquet.Net are pure managed and run on arm64 macOS and x64/arm64 Windows without native binaries [S19][S20]; `HDF.PInvoke` and STEPcode bring native builds per platform — a packaging cost the constraints ("compatible with both target platforms") make visible.

## Foil section catalog for hydrofoil wings, stabilizers, struts and fins

*Source: [06-foil-section-catalog.md](06-foil-section-catalog.md) (`kb-hw-foil-section-catalog`).*

#### Eppler hydrofoil sections (E817–E838, E874, E904, E908)

Eppler's hydrofoil work was done for DTNSRDC with Y. T. Shen using the Eppler design/analysis code. Part 1 (1979) produced "a series of symmetrical hydrofoil sections with improved hydrodynamic characteristics in terms of cavitation inception"; Part 2 (1981) applied the method to non-symmetrical sections "having a minimum pressure bucket whose depth and width are adapted to practical applications"; Part 3 (1985) reports experimental verification [S4][S5][S6]. The design philosophy is a **rooftop** pressure distribution: constant maximum velocity over the forward upper surface at the design angle, which gives the widest cavitation-free Cl range for a given −Cp_min, then a concave pressure recovery. Speer's critique (the H105 page) is that the recovery aft of the rooftop "is too short for low Reynolds numbers" and the flat rooftop "makes for a tendency toward leading edge stall" — that is, these are ship-scale (≈40 kn, Re ≳ 3 × 10⁶) sections [S9] *(Verified)*. The book *Airfoil Design and Data* (Springer 1990, ISBN 3-540-52505-X) exists and is cited by the E817 cavitation papers; whether it reproduces the hydrofoil chapter and the individual design Cl/Re for each of E817–E908 could not be opened this session, so per-section **design Cl and design Re remain Flagged** [S7][S25].

Geometry measured here from the UIUC coordinate files (linear interpolation on 1,001 chord stations; camber = mean line at its maximum |value|) *(Verified by execution)*:

| Section | UIUC file | Format | Points | t/c | x(t_max) | Camber | x(c_max) | TE gap | UIUC description |
|---|---|---|---|---|---|---|---|---|---|
| E817 | e817.dat | Lednicer | 70 | 10.98 % | 0.33 | 2.88 % | 0.69 | 0.00 % | "Eppler 817 hydrofoil airfoil" |
| E818 | e818.dat | Lednicer | 69 | 9.37 % | 0.33 | 2.79 % | 0.67 | 0.00 % | "Eppler 818 hydrofoil airfoil" |
| E874 | e874.dat | Lednicer | 122 | 7.90 % | 0.29 | 0.95 % | 0.33 | 0.00 % | "Eppler 874 hydrofoil airfoil" |
| E904 | e904.dat | Lednicer | 111 | 9.00 % | 0.45 | 1.34 % | 0.50 | 0.00 % | "Eppler 904 airfoil" (UIUC index: hydrofoil) |
| E908 | e908.dat | Lednicer | 111 | 9.00 % | 0.44 | 2.77 % | 0.66 | 0.00 % | "Eppler 908 airfoil" (UIUC index: hydrofoil) |
| E836 | e836.dat | Lednicer | 63 | 12.64 % | 0.43 | 0.00 % | — | 0.00 % | "Eppler E836 hydrofoil airfoil" |
| E837 | e837.dat | Lednicer | 63 | 16.10 % | 0.37 | 0.00 % | — | 0.00 % | "Eppler E837 hydrofoil airfoil" |
| E838 | e838.dat | Lednicer | 63 | 18.37 % | 0.37 | 0.00 % | — | 0.00 % | "Eppler E838 hydrofoil airfoil" |
| E862 | e862.dat | Lednicer | 63 | 32.37 % | 0.28 | 0.00 % | — | 1.20 % | "Eppler 862 strut airfoil" |
| E863 | e863.dat | Lednicer | 63 | 35.73 % | 0.28 | 0.00 % | — | 1.40 % | "Eppler 863 strut airfoil" |
| E864 | e864.dat | Lednicer | 63 | 38.82 % | 0.33 | 0.06 % | 0.11 | 1.60 % | "Eppler 864 strut airfoil" |

Reading of the table *(Inferred)*: E817/E818/E908 carry their camber far aft (x/c 0.66–0.69) — aft loading, which is how a rooftop section keeps the forward upper surface flat; E874 and E904 are the low-camber (≈1 %) members intended for higher speed / lower design Cl; E874 is the thinnest (7.9 %). The UIUC index also lists E850–E858 as propeller sections [S1]. The "E817–E838 family" phrasing in the task is only partly right: UIUC carries E817, E818, E836, E837, E838 in that numeric range; E819–E835 are not listed as hydrofoils there *(Verified by listing)*.

Experimental evidence on E817: Kjeldsen, Arndt & Effertz (ASME JFE 122(1), 2000) and Arndt et al. (JFE 124(1), 2002, scale effect) measured cavitation inception and development on a 2D E817 at St. Anthony Falls Laboratory; the ASME pages were not readable (HTTP 403) so inception σ values are **Flagged** [S7][S8]. Arndt's "Some remarks on hydrofoil cavitation" (J. Hydrodynamics 2012) is the review to cite for the physics of sheet/cloud cavitation on these sections [S26] *(Verified existence)*.

#### NACA families: definitions, generators, and where "exact" and "generated" differ

**4-digit (0009/0010/0012 struts and fins, 4412 baseline).** Closed-form thickness polynomial and parabolic-arc mean line; the classical thickness polynomial gives a finite TE (0.21 % of chord for 0010 and 0.26 % for 4412 in the UIUC files measured here) unless the closed-TE coefficient (−0.1036 in place of −0.1015) is used. The repo's Phase-0 "silent 6 %-thick NACA 0012" defect [grounding] is exactly the Lednicer-read-as-Selig failure; the UIUC `naca0012` file name was not found this session (404 for `naca0012.dat`; `naca0010.dat` exists), so **generate 4-digit analytically, never vendor** *(Verified by execution and by TM 4741 [S16])*.

**5-digit.** Same thickness form with the 230-type mean line (design Cl 0.3, max camber forward); defined in TM X-3284/TM 4741. No hydrofoil use found; keep as generator capability only *(Inferred, [S16])*.

**16-series (16-006, 16-012, 16-021, 16-309).** Defined in TM X-3284 as a 4-digit-modified thickness distribution with leading-edge index 4 and maximum thickness at 0.5c; nomenclature 16-*d*ₗ*tt*: design Cl in tenths, thickness in percent (16-309 → Cl 0.3, 9 %). TN 1546 (Lindsey, Stevenson, Daley, 1948) is the wind-tunnel dataset for 24 sections at M 0.3–0.8 [S17]. Measured here from UIUC: 16-006 6.00 %, 16-012 12.00 %, 16-021 21.00 %, all with maximum thickness at x/c 0.50 and TE gaps 0.12/0.24/0.42 % (33 points each — coarse) *(Verified by execution)*. Hydrodynamic role: the classic propeller/ship-hydrofoil family (PCH used 16-309 [S18]); its mid-chord maximum thickness gives a flat rooftop and a deep cavitation bucket but poor pressure recovery at low Re — the reason Speer and the DTNSRDC test prefer a 6-series [S18] *(Verified ranking; mechanism Inferred)*. The mean line used with 16-series in the NACA generator is the a = 1.0 uniform-load line — **Flagged** (recall, not re-opened).

**6-series and 6A (63-, 64-, 65-, 66-; 64A410, 63-209, 63-412, 66-209, 66-012, 66-018).** Nomenclature 6*p*-*d*ₗ*tt*: second digit = chordwise position of minimum pressure in tenths for the basic symmetric thickness at zero lift; digit after the dash = design Cl in tenths; last two = t/c; an optional subscript gives the half-width of the low-drag bucket in Cl. The "A" modification replaces the cusped rear with essentially straight sides from about 0.8c to the TE, giving a thicker, more manufacturable trailing edge and slightly different bucket behaviour *(Inferred from TM 4741 [S16]; standard NACA nomenclature per Report 824 [S27] — the report's text was not re-opened, so the description is Inferred)*. Ordinates are **not** closed-form: the thickness forms were derived by conformal mapping and are tabulated; TM X-3069/TM 4741 reproduce them by fitted procedures, agreeing with the published tables "generally within 5 × 10⁻⁵ chord" for 6-series, with a known exception at the 6A leading edge [S16] *(Verified)*. Measured here (UIUC, 51–53 points, Selig format except `n63412.dat` which is Lednicer): 63-209 9.00 %/1.10 %; 63-412 12.00 %/2.20 %; 63A210 9.99 %/1.33 % (TE 0.04 %); 64-210 9.98 %/1.10 %; 64A410 9.99 %/2.66 % at x 0.40 (TE 0.04 %); 65-410 9.99 %/2.21 %; 65₂-415 14.99 %/2.20 %; 66-209 9.00 %/1.10 %; 66-210 10.00 %/1.10 % *(Verified by execution)*. Note the camber of the a = 1.0 mean line at design Cl 0.2 is ≈1.1 % and at 0.4 ≈2.2 % — consistent with the files, a useful admission check.

**7-series.** Tabulated in Report 824; designed for extended laminar flow on both surfaces; no hydrofoil use found and no generator in TM 4741 — exclude from v1 *(Inferred)*.

**Generators and their scope (all Verified this session unless noted):**
- NASA TM 4741 Fortran (US Government work; public domain in the US) — 4, 4-mod, 5, 16, 6, 6A [S16].
- AeroSandbox `Airfoil("naca4412")` → `get_NACA_coordinates`: **4-digit only**, raises otherwise; `get_UIUC_coordinates` reads a copy of the UIUC database bundled in the package (MIT-licensed repo; the bundling does not itself establish UIUC's terms) [S14].
- XFOIL `NACA` command: 4- and 5-digit only — **Flagged** (recall of the XFOIL manual; not re-opened).
- `airfoils` (airinnova, Apache-2.0): NACA 4-digit generator plus file I/O [S15] — scope beyond 4-digit **Flagged**.
- OpenVSP (NASA Open Source Agreement 1.3): includes 4-digit, 4-digit-mod, 5-digit, 16-series and 6-series cross-section types [S15][grounding]; NOSA is not MIT/BSD/Apache, so it is a process-invocation tool at most under COMMIT-02 *(Verified licence; feature list Verified via grounding note)*.
- Airfoil Tools NACA 4/5-digit generators — web only, no-reproduction terms [S21].

**Where exact and generated differ.** UIUC files for NACA sections are digitised/typed tables with 33–53 points and finite or zero TE gaps that vary by file; a TM 4741 generation at, say, 161 cosine-spaced points is the better-conditioned source for the same name and can be regenerated bit-for-bit at build. The two will differ at the 10⁻⁴–10⁻³ chord level (interpolation, LE resolution, TE closure), which is visible in Cp_min at high α *(Inferred from the point counts and TE gaps measured above)*.

#### Designed-for-hydrofoil and low-Re sections

**Speer H105.** Intent verbatim on the page: low-Re sailing hydrofoils, 20–30 kn, "designed to avoid laminar separation and ventilation when operating at low speeds and moderate angles of attack, while still having low velocities at small angles of attack to avoid cavitation at high speeds"; "convex velocity profiles, which use the entire surface to control the position of the laminar separation bubble"; "the location of the maximum velocity also changes with angle of attack"; "restrained in its use of aft loading". The page compares H105 with the earlier H002/H005 and with E817; it gives **no** design Re, design Cl, t/c or camber numbers, and no coordinates or terms [S9] *(Verified)*. Community reports say H105 is used on the Axiom Moth and that Speer has shared coordinates by e-mail and in forum attachments — tertiary, **Flagged** [S28]. Because `h105.dat` is absent from UIUC (404) and no written redistribution statement was found, the catalog cannot bundle it; even a t/c value should be labelled Unknown until a coordinate file with a rights statement is in hand.

**Moth-class sections.** Forum-level history: NACA 63-412 on the original Ilett/Fastacraft foils "until the Bladerider", H105 on Axiom [S28] *(Flagged, tertiary)*. Primary: Beaver & Zseleczky (CSYS 2009) full-scale tow tests of a Moth [S29] *(Verified existence)*; Day, Cocard & Troll (CSYS 2019) tested a Bladerider main T-foil full-scale (mean-chord Re ≈ 1.2 × 10⁵ at 1.5 m/s and up), digitised the horizontal section (max thickness/camber ratios reported: 12.8 %, 3.1 %; strut ≈ NACA 66012 scaled to 14.5 %), ran XFOIL 6.99 at six Re between 10⁵ and 10⁶, 13 α and 17 flap angles with free transition and **Ncrit = 4** ("corresponding to a turbulence level of around 0.6 %"), and added the free surface by a biplane-image approach (about +3 % on drag area for a 12 % section at moderate chord Froude numbers). Their conclusion: the predicted lift–drag relation is "reasonably accurate in most cases", and "further investigation of turbulence levels and transition models are planned" [S12] *(Verified by execution — PDF text decoded)*.

**Kitefoil / windfoil.** A 2025 *Journal of Marine Science and Application* paper measures fluid forces on a kitefoil/windfoil hydrofoil as a function of the submergence-based Froude number [S30] *(Verified title only; body behind login — Flagged)*. Kiteforum/boatdesign threads discuss E387 vs H105 and "Reynolds 600K–1.5MM" kitefoil design — tertiary [S28].

**Selig S1223.** Designed for Re 2 × 10⁵ with concave pressure recovery and aft loading, using PROFOIL, the Eppler code and ISES; wind-tunnel Cl,max 2.2 (2.3 with vortex generators or a 1 % Gurney flap, separately) [S31] *(Verified abstract)*. Measured here: 12.14 % t/c at x 0.20, camber 8.67 % *(Verified by execution)*. Its Cp_min at working Cl is far below hydrofoil cavitation limits at 15 kn; it is a take-off/high-lift study fixture, not a foiling section *(Inferred)*.

**SD7003.** 8.51 % t/c, 1.46 % camber (file header "SD7003-085-88") *(Verified by execution)*. Its status as the standard laminar-separation-bubble benchmark near Re 6 × 10⁴ is **Flagged** (recall). Relevant as a regression fixture for transition/LSB behaviour of the 2D solver at the low end of the water Re band.

**Drela AG series.** `ag24.dat` header reads "AG24 Bubble Dancer DLG by Mark Drela"; AG24 8.41 %/2.22 %, AG35 8.72 %/4.37 % *(Verified by execution)*. Discus-launch-glider sections for Re ≈ 5 × 10⁴–2 × 10⁵ — **Flagged** design range (recall). Not hydrofoil sections; useful only as low-Re solver fixtures.

**Wortmann FX 63-137.** 13.71 % t/c, 5.97 % camber, 99 points *(Verified by execution)*; a high-lift low-Re sailplane/wind-turbine section. An *Aeronautical Journal* study of TE extensions on Göttingen 797 and FX 63-137 at Re 3 × 10⁵–10⁶ exists [S32] *(Verified title)*. Design Re/Cl **Flagged**.

**Clark Y.** 11.71 % t/c, 3.43 % camber, near-flat lower surface *(Verified by execution)*. Historic; the flat lower surface is a manufacturing convenience, not a cavitation feature *(Inferred)*.

**Göttingen 796/797.** UIUC files are coarse (43 and 29 points), 12.00 %/3.69 % and 16.00 %/5.02 %, TE gaps 0.40 %/0.80 % *(Verified by execution)*. The claim that these were "classic hydrofoil" sections (Supramar/Bell era) could **not** be confirmed in any source this session — **Flagged**; do not label them "hydrofoil" in the catalog without a citation.

**Eppler E387/E393.** E387 9.07 %/3.80 %; E393 11.53 %/4.00 % *(Verified by execution)*. E393 is the section used in a Moth performance program (tertiary [S28]); E387 is the low-Re wind-tunnel reference airfoil in the UIUC LSAT volumes — **Flagged** (recall).

**Liebeck high-lift, Tulin supercavitating, base-vented sections.** Named for completeness. Liebeck sections (Stratford-recovery high lift) were not verified this session — **Flagged**. Supercavitating (Tulin) and base-vented families are out of scope for a sub-cavitating design tool; the IHS compilation records U.S. Navy struts that were "one subcavitating, one base-ventilated, and one supercavitating", a 4 %-thick 16(35)04 rung, and an 18 % parabolic strut tested at 65 kt, and notes that "many strut 'cavitation' problems are really ventilation problems" [S33] *(Verified text; tertiary compilation of practitioner Q&A)*.

#### Struts, masts and fins

**Families.** Symmetric candidates are (i) NACA 00xx (0009–0012, fins and thin struts), (ii) NACA 6-series symmetric (63-012, 64-012, 66-012/66-018 — the Moth strut in Day et al. "matched very closely" a 66012 scaled to 14.5 % [S12]), (iii) NACA 16-0xx (16-012, 16-021: mid-chord maximum thickness, deep bucket), (iv) Eppler symmetric hydrofoil sections E836/E837/E838 (12.6/16.1/18.4 %, designed for cavitation inception, Part 1 of the JSR trilogy [S4]). Trade-offs *(Inferred from the sources above)*: drag at zero lift favours thin (9–12 %) and aft maximum thickness; cavitation at yaw/sideslip favours a wide bucket (Eppler symmetric or 16/66-series) and a modest LE radius; ventilation on a surface-piercing mast is a separate mechanism driven by low pressure near the free surface at angle, not by vapour pressure [S20][S33]; bending/torsional stiffness scales with t³ and with chord, so brands trade drag against stiffness through thickness at fixed chord ≈ 106–118 mm (Armstrong: Mk II ≈ 30 % stiffer in bending than Mk I; Performance-X "12 mm") [S19] *(Verified chord/stiffness statements; the 12 mm figure is quoted from the page but other vendor summaries give 13.8–15.8 mm — treat the thickness as Flagged until read off a datasheet)*. Sabfoil ≈ 14 mm and a "10–12.5 % at ≈120 mm chord" rule of thumb are tertiary [S34] *(Flagged)*.

**Surface-piercing struts.** Aguiar Ferreira et al. 2025 (CC BY 4.0) is the current primary reference: quasi-steady towing-tank tests of a semi-ogive blunt-TE strut and a modified NACA 0010-34 at Fr 0.5–2.5, AR 1.0 and 1.5, sweeping α to ventilation onset. Three mechanisms: nose ventilation (Fr < 1.0–1.25, increasing α), tail ventilation (higher Fr, negative-α trends), and base ventilation (blunt section only, unstable cavity). The bistable/globally-stable boundary "extends to significantly higher α than previously estimated" and the paper publishes a revised stability map [S20] *(Verified)*. Speer's recommended validation case for surface-piercing foils is Kuhn & Scragg, "Analysis of Lift and Drag on a Surface Piercing Foil", 11th CSYS 1993 [S10] *(Verified citation)*.

**Surf/windsurf fins.** No primary literature on fin section templates (Futures/FCS, "flat-back", 50/50 vs 80/20 foils) was found in this session; only maker files (a NACA 0012 FCS fin print) — **Flagged, open question**. Treat fins as a future surface with the 4-digit symmetric generator as the only admitted default.

#### Selection criteria for hydrofoil sections

1. **Cavitation bucket (σ_required = −Cp_min vs Cl).** Screening inception when −Cp_min ≥ σ, σ = (p_atm + ρ g h − p_v)/(½ ρ V²). Depth h, temperature (p_v) and salinity (ρ) enter through σ; the section enters through Cp_min(α, Re). Hepperle's page states the same dependence (vapour pressure, speed, immersion) and shows the usable Cl range shrinking with speed, with 15 and 20 kn examples on NACA 4405 vs 4410 [S22] *(Verified)*. Cp_min from a panel/IBL code under-reads sharp suction peaks at coarse resolution — the unsafe direction (the repo already records this; ANA-02).
2. **Laminar bucket at water Re (5 × 10⁵–2 × 10⁶).** For chord 0.06–0.15 m at 4–12 m/s and ν ≈ 1.05 × 10⁻⁶ m²/s (salt, ≈20 °C; ITTC value — **Flagged** recall, the repo's fluid table governs), Re ≈ 2.3 × 10⁵–1.7 × 10⁶; the Day et al. runs at 10⁵–10⁶ bracket this [S12]. Bucket width shrinks with Ncrit and Re; the bucket edges, not the bottom, are where E817's short recovery penalises low Re [S9] *(Inferred)*.
3. **Ncrit for water.** Evidence table — see Data section. Practical outcome: run every polar at two Ncrit values (e.g. 2 and 4) and show the spread; the spread is the transition uncertainty, which is currently unmeasurable *(Inferred from [S11][S12])*.
4. **Leading-edge radius vs debris tolerance.** Speer's remark that transition in water is promoted by "critters and particulate matter" [S11] is the only source found; no quantitative roughness-sensitivity study on hydrofoil sections was found — **Flagged**; the repo's roughness gap (GAP register) remains open.
5. **Thickness for structure.** No hydrodynamic optimum (repo Phase 0); Armstrong's mast data show the stiffness-vs-thickness trade being made at fixed chord [S19]. For wings, E874 (7.9 %) vs E817 (11 %) spans the practical range *(Verified geometry; trade Inferred)*.
6. **TE thickness for manufacture.** UIUC Eppler files close to 0.00 %; NACA 4/16-series carry 0.2–0.4 %; a 2026 study of thin NACA sections with moderately truncated trailing edges exists [S35] *(Verified title; content Flagged)*. The catalog should record the as-filed TE gap and any applied closure as a profile revision (A3).
7. **Pitching moment Cm.** Aft-loaded sections (E817/E818/E908 camber peak at x/c ≈ 0.67–0.69; S1223) carry large negative Cm₀, which increases the stabilizer down-load and thus the stabilizer area/drag for a given fuselage length *(Inferred from the measured camber positions; the Cm values themselves must come from the polar pipeline, not from this file)*.
8. **Free-surface proximity.** Day et al. added ≈3 % drag-area for a 12 % section at moderate chord Froude numbers using a biplane-image model and note wave-making effects at higher speeds require a free-surface CFD [S12]; the 2025 JMSA kitefoil paper frames the effect through the submergence Froude number [S30] *(Verified/Flagged as marked)*.

#### Data sources and tools for polars

| Source | What it holds | Accuracy / method | Terms (as read 2026-09-20) | Confidence |
|---|---|---|---|---|
| UIUC coordinate database [S1] | ≈1,650 coordinate files, Selig or Lednicer | Provenance varies per file (typed tables, digitised) | No licence statement; site copyright notice only | Verified |
| UIUC LSAT volumes 1–4 (+ Williamson thesis, "vol. 6") [S3] | Low-speed wind-tunnel polars, Re ≈ 6 × 10⁴–5 × 10⁵ (range Flagged) | Measured | GPL + Manifesto; commercial use OK with conspicuous attribution, no data surcharge, redistribution rights preserved | Verified (terms) |
| Airfoil Tools [S21] | 1,638 airfoils, XFOIL polars (Ncrit 5 and 9, Re 5 × 10⁴–10⁶ — Flagged recall) | XFOIL-generated | "All Rights Reserved … should not be reproduced without permission" | Verified (terms) |
| NeuralFoil [S13] | Surrogate of XFOIL, Re 10²–10¹⁰, 360° α | 0.37 % / 2.0 % mean relative drag error vs XFOIL | MIT | Verified |
| XFOIL 6.99 [S36] | Panel + IBL, e^N transition | Reference for the surrogate; not experiment | GPL (process-invoke only under COMMIT-02) | Verified |
| NASA TM 4741 [S16] | NACA ordinate generator | ≤ 5 × 10⁻⁵ c vs tables (6-series) | US Government work | Verified |
| NACA Report 824 [S27] | 4/5/6/7-series tables and polars | Measured (LTPT) | Public domain, on NTRS | Verified availability |
| DTIC ADA032272 [S18] | 16-309 vs 64A309 foil+strut, flapped, tow + rotating arm | Measured | US Government work | Verified abstract |
| Shen & Dimotakis 1989 [S37] | NACA 66(MOD), GALCIT HSWT, 9–18 m/s, α 0–6°, 13 taps, cavitating/non-cavitating | Measured | Caltech authors' repository; reuse terms Flagged | Verified abstract |
| Delft Twist-11 (Foeth 2008) [S38] | NACA 0009 twisted foil, c 150 mm, span 300 mm; cavitation benchmark (VIRTUE WP4, SMP'11) | Measured | TU Delft thesis; data terms Flagged | Verified description |
| Aguiar Ferreira et al. 2025 [S20] | Surface-piercing strut ventilation maps | Measured | CC BY 4.0 | Verified |
| Day et al. 2019 [S12] | Full-scale Moth T-foil lift/drag vs speed, α, flap, immersion | Measured; XFOIL Ncrit 4 comparison | Open repository copy (Strathprints); reuse terms not checked — Flagged | Verified content |
| NASA TMR [grounding] | Turbulence-model verification cases (SST etc.), not hydrofoil polars | — | Public | Verified (grounding) |

**XFOIL/NeuralFoil vs experiment at water Re.** The only direct comparison found this session is Day et al.: XFOIL (Ncrit 4) inside a lifting-line + image model reproduces the measured lift–drag relation "reasonably accurate in most cases" but not the α/flap mapping [S12]. NeuralFoil's published errors are against XFOIL, so the chain "experiment ← XFOIL ← NeuralFoil" carries two unquantified links at water Re — the repo's "checked against experiment" verb (COMMIT-01) is still unmet at section level *(Inferred)*.

## Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity

*Source: [07-low-order-hydrodynamics.md](07-low-order-hydrodynamics.md) (`kb-hw-low-order-hydrodynamics`).*

#### 2D section methods

**Thin-airfoil theory (closed form; estimator tier).** For a thin section at small α with attached flow, C_l = 2π(α − α_L0) per radian, C_m,c/4 depends only on camber, and the zero-lift angle α_L0 = −(1/π)∫₀^π (dz_c/dx)(cos θ − 1) dθ with x = (c/2)(1 − cos θ). The ideal (design) angle is where the leading-edge singularity vanishes (A₀ = 0), i.e. the incidence at which the stagnation point sits on the leading edge; NACA 6-series "design C_l" is defined there. Validity: |α − α_L0| ≲ 10°, t/c ≲ 15 %, Re high enough that the boundary layer is thin; it yields no drag and no C_l,max. *(Inferred; standard in Abbott & von Doenhoff, Anderson, Katz & Plotkin — textbooks not opened this session; confirmed indirectly by the VLM lift-slope limits in [S15][S27].)*

**Conformal mapping (exact inviscid references).** Joukowski sections (cusped trailing edge) and Kármán–Trefftz sections (finite trailing-edge angle) have closed-form potential solutions and are the correct *unit-test oracles* for any panel code: the surface velocity is known analytically, so the panel Cp and C_l must converge to it as panel count rises. Theodorsen's method maps an arbitrary section to a near-circle and is the classical route to inviscid Cp for NACA data. Recall of the Joukowski lift slope with thickness correction (≈ 2π(1 + 0.77 t/c)) is **Flagged**. Use: oracle only; never as a product number.

**Panel methods.** Hess–Smith (constant-strength sources plus a single vortex strength per body, Neumann boundary condition, Kutta condition at the trailing edge) is the textbook 2D method (Katz & Plotkin ch. 11; Mason ch. 4 [S15]). XFOIL's inviscid formulation is "a simple linear-vorticity stream function panel method"; "a finite trailing edge base thickness is modeled with a source panel"; a Kármán–Tsien compressibility correction is included [S1]. **In water the compressibility correction is a no-op:** at 10 m/s and c ≈ 1480 m/s, M ≈ 0.0068 and the Kármán–Tsien factor differs from 1 by < 10⁻⁴. The default 160 panels are bunched near the leading edge [S1]; a 2D verification fixture for the workbench's own panel code should show Cp_min convergence against a Kármán–Trefftz analytic solution at 100 / 200 / 400 panels. *(Verified for XFOIL statements [S1]; Inferred for the Mach arithmetic.)*

**Integral boundary layer and e^N transition (XFOIL).** XFOIL couples the panel solution to a two-equation integral boundary layer with lag-entrainment closure and uses a global Newton solve; "the e^n method is always active", and "Ncrit … is the log of the amplification factor of the most-amplified frequency which triggers transition"; the documented table is sailplane 12–14, motorglider 11–13, clean wind tunnel 10–12, average wind tunnel 9, dirty wind tunnel 4–8, and "bypass transition … can be mimicked … by setting Ncrit to a small value — Ncrit = 1 or less" [S1]. The frequently quoted mapping Ncrit = 9 ↔ 0.07 % turbulence intensity comes from Mack's correlation (N = −8.43 − 2.4 ln(Tu)) and was **not** found in the XFOIL primer this session; it is **Flagged** as a secondary attribution [S42]. Drag: "CD … is obtained by applying the Squire-Young formula at the last point in the wake — NOT at the trailing edge", C_D = 2θ(u_e/V)^((H+5)/2), which is "always reasonable" one chord downstream where u_e ≈ V [S1]. Failure modes stated by the author: "massive separation from excessive airfoil thickness, flap deflection, or angle of attack" and "Reynolds number too low" [S1]. Envelope for product use: attached or mildly separated flow, Re ≳ 10⁵, α up to the first non-convergence; C_l,max from XFOIL is an *estimate that typically overshoots* experiment because the integral method does not capture massive separation (Inferred; the primer states non-convergence, not the sign of the error — Flagged direction).

**Ncrit for water.** No primary source giving an Ncrit for open-water hydrofoils was found. Practitioner sources (tertiary) argue for lower values than air — "Ncrit = 3 … for towing testing … a smaller number should be necessary for real-life keels", "since water near the surface is strongly turbulent, Ncrit = 1 was used" [S43][S42] — while towing tanks and cavitation tunnels are described as low-turbulence facilities needing further study [S43]. The spec's ANA-15 preset (2.0, labelled a source-based assumption) is consistent with this tertiary practice but has **no primary source**; the honest label is "practitioner assumption; polar changes materially with Ncrit". Because NeuralFoil was trained with Ncrit ∈ [0, 18] [S3], Ncrit is a first-class input at the polar tier and a polar must be keyed by (profile hash, Re, Ncrit, α). *(Inferred; magnitude of the Cd change between Ncrit 1 and 9 at Re 10⁶ is Flagged — measure it with an XFOIL sweep, see Open questions.)*

**NeuralFoil.** Architecture: a physics-informed MLP trained in PyTorch, executed in NumPy, eight sizes from "xxsmall" to "xxxlarge", < 500 lines of user-facing code [S2]. Inputs: CST-parameterised shape (8 per side + LE modification + TE thickness = 18), α, Re, Ncrit, optional control deflection; trained at M∞ = 0 with compressibility applied post-network (irrelevant in water) [S3]. Outputs: CL, CD, CM, Top_Xtr, Bot_Xtr, analysis_confidence, plus θ, H, u_e/u_∞ at 64 stations and hence a pressure distribution [S2][S3]. Accuracy vs XFoil test data: "xlarge" MAE CL 0.013, ln CD 0.024, CM 0.002 [S2]; "xxxlarge" 0.012 / 0.020 / 0.002 [S3]; "mean relative error of drag is 0.37 % on simple cases" and "as low as 2.0 % on a test dataset with numerous post-stall and transitional cases" [S3]. Post-stall: "NeuralFoil fuses its attached-flow results with empirical models for massively-separated (post-stall) flow conditions … Truong" [S3]; stated limitations: no reversed-flow reattachment near α = 180°, moments less accurate post-stall [S3]. `analysis_confidence` "is trained on binary features corresponding to whether an XFoil analysis with these input conditions converged", passed through a logistic function and a Mahalanobis-distance correction so that it → 0 out of distribution [S3]. **What it may claim:** an XFOIL-class estimate (its error is measured against XFOIL, not against experiment), inside the CST-representable shape space, with confidence as a *validity flag*. **What it may not claim:** experimental agreement, resolved suction peaks (64 stations), stall behaviour beyond an empirical blend, or any number where analysis_confidence is low. *(Verified, [S2][S3])*

**AeroSandbox airfoil tools.** `Airfoil` class with CST fitting, repanelling, `get_aero_from_neuralfoil`, an XFoil wrapper (process invocation; XFOIL stays GPL) and Cp access; dependencies NumPy, CasADi, SciPy, NeuralFoil; MIT [S4]. Relevance to a C#/.NET product: reference implementation and Python-side oracle, not a linked dependency.

**MSES / multi-element.** MSES (Drela) is a coupled Euler/integral-BL multi-element code; it is proprietary/licensed and not on the permissive path; it is mentioned only because published MSES polars are sometimes the "truth" in comparisons. *(Flagged — not researched this session.)*

**Empirical corrections.** (a) *Reynolds scaling of profile drag:* for turbulent flat-plate friction the ITTC 1957 line C_F = 0.075/(log₁₀Re − 2)² [S25] with Hoerner's streamline-section form factor (1 + 2(t/c) + 60(t/c)⁴) gives C_d0 ≈ 2·C_F·(1 + 2(t/c) + 60(t/c)⁴) for a fully turbulent section [S26]; it is *not* a substitute for a laminar-bucket polar at Re ≈ 10⁶ but is the right fallback and upper bound when transition is tripped. (b) *Roughness / laminar-bucket collapse:* NACA TR 824 "standard roughness" (carborundum grains over the first 8 % chord) removes the laminar bucket and lowers C_l,max; the drag rises toward the fully turbulent form-factor value [S35]. Recall of the grain size (0.011 in on 24-in chord models) is **Flagged**. For a foil ridden in salt water with fouling and sand, the fully turbulent value is the honest *pessimistic* bound and the Ncrit-9 polar the optimistic one; show both. *(Inferred.)*

#### Cavitation screening

Cavitation number σ = (p_∞ − p_v)/(½ ρ V²) [dimensionless]; p_∞ = p_atm + ρ g h [Pa] at the depth h [m] of the point considered; p_v [Pa] from ITTC 7.5-02-01-03 Rev 03 (fresh water: IAPWS; seawater: TEOS-10 with vapour pressure per Sharqawy et al.), tabulated 0–50 °C for S_A = 0 and 35.16504 g/kg, with sensitivity coefficients dp_v/dt and a worked uncertainty (fresh water 20 °C: p_v = 2.34 ± 0.14 kPa for U_t = ±1 °C, 2.339 ± 0.014 kPa for ±0.1 °C) [S5]. Inception *screen*: local pressure reaches p_v where Cp = −σ, so the necessary condition is σ ≤ −Cp_min. Solving for speed:

V_crit = √( 2 (p_atm + ρ g h − p_v) / ( ρ · (−Cp_min) ) )  [m/s], with p in Pa, ρ in kg/m³, g = 9.80665 m/s², h in m.

The cavitation bucket is the curve σ_required(C_l) = −Cp_min(C_l) (or vs α) for one section at one Re/Ncrit; the operating point is safe *by this screen* if σ_operating > σ_required with margin. Distinctions: **sheet** (attached, from the LE suction peak; the −Cp_min screen targets this), **bubble** (mid-chord, gentle Cp minima, nuclei-dependent), **tip-vortex** (core pressure of the trailing vortex, scales with circulation and Re, not visible in a 2D Cp) [S28][S30]. Measured σ_i is "quite high" in its deviation from −Cp_min and "significantly depends on Reynolds number"; nuclei transport and water quality modulate inception; the "pressure drop assumption is not a sufficient criterion for cavitation inception" [S28]. The practitioner summary that "the usable range of lift coefficients shrinks with increasing forward speed, and there is a speed limit above which no cavitation free flow is possible" [S29] follows from V_crit ∝ (−Cp_min)^(−½) and Cp_min becoming more negative with |C_l − C_l,design|. **Honest label:** "Cavitation screening (sheet, by −Cp_min): inception is possible above V_crit; not a prediction of inception, extent, or tip-vortex cavitation; Cp_min resolution: N stations". *(Verified for definitions and ITTC data [S5]; Inferred for the deviation statements [S28]; the panel-resolution under-read is Inferred, unquantified.)*

#### 3D finite-wing methods

**Prandtl lifting line.** For an unswept wing of moderate-to-high AR the bound circulation Γ(y) and downwash w(y) satisfy α_eff(y) = α_geo(y) − α_i(y), α_i = w/V; elliptic loading gives α_i = C_L/(π AR), C_Lα = a₀/(1 + a₀/(π AR)) (a₀ = 2π → 2π AR/(AR + 2)) and C_Di = C_L²/(π AR); for other loadings C_Di = C_L²/(π e AR) with span efficiency e ≤ 1 (Oswald factor when it also absorbs the lift-dependent profile drag of the whole aircraft) [S38]. Validity: AR ≳ 4, sweep ≲ 15°, attached flow, linear section lift. *(Inferred; [S38] and standard texts.)*

**Helmbold (low AR).** C_Lα = 2π AR/(2 + √(AR² + 4)) per radian, equivalently 2π/(√(1 + (2/AR)²) + 2/AR); ESDU TM 169 reports the Helmbold–Diederich form within ±2 % of data for βA ≥ 1.5 [S27]. Values: AR 2 → 2.60/rad; AR 4 → 3.88/rad (corrected in the completion pass: the first draft wrote 3.99; 2π·4/(2+√20) = 25.13/6.472 = 3.88); AR 8 → 4.91/rad (vs Prandtl 5.03/rad; 2.4 % apart). Use: estimator tier for stabilizers and short-span front wings; the VLM should reproduce these within ≈ 3 %. *(Inferred; arithmetic this session.)*

**Weissinger / extended lifting line.** The bound vortex at the quarter chord and the tangency condition at the three-quarter chord (one chordwise panel) recover sweep and low-AR effects that Prandtl misses; the "nonlinear Weissinger" variants replace the linear section law with a polar [S46]. The general numerical lifting line of Phillips & Snyder uses "a fully three-dimensional vortex lifting law" for "systems of lifting surfaces with arbitrary camber, sweep, and dihedral", with accuracy "comparable to that obtained from numerical panel methods and inviscid computational fluid dynamics solutions" [S9]; Goates & Hunsaker (2021) and Reid & Hunsaker (2020, sweep) are the implementation references used by MachUpX [S10][S40]. *(Verified [S9][S10]; Inferred [S46].)*

**Vortex lattice.** Horseshoe vortices (one bound segment plus two semi-infinite trailing legs) or vortex rings on the mean camber surface; bound vortex at the panel quarter chord, control point at the panel three-quarter chord with the no-penetration condition; induced drag either in the Trefftz plane (far field; robust) or by near-field Kutta–Joukowski on each bound segment (sensitive to panelling); limits: thin, attached, linear, small α [S15]. A VLM handles twist, dihedral/anhedral, sweep and multiple surfaces geometrically; the wake is usually a rigid planar or freestream-aligned sheet — adequate for steady deep-water lift and induced drag, inadequate near a free surface without an image system, and it cannot represent thickness or viscous drag. Downwash at a stabilizer is obtained by evaluating the front-wing lattice's induced velocity at the stabilizer's control points (the classic far-field estimate ε ≈ 2 C_L/(π AR e) applies only many spans downstream, which a 0.6–0.8 m fuselage is not). *(Inferred; [S15] extraction was partial because the PDF could not be rendered — see Open questions.)*

**3D panel methods (source/doublet, Morino).** Needed when thickness, fuselage/mast bodies or surface Cp on the 3D wing matter (e.g. 3D cavitation screening near the tip or at the junction), and when a free-surface image system is to be applied to a thick body. The BEM-vs-RANS study near the free surface shows this class reproduces free-surface lift trends but needs viscous corrections and still departs from experiment at higher α [S32]. Not a v1 tier. *(Inferred.)*

**Strip theory (quasi-3D).** For each spanwise strip: α_eff = α_geo − α_i from the VLM/lifting line, local Re = V c(y)/ν, then C_l, C_d, C_m from the 2D polar at (Re, Ncrit, α_eff); profile drag is integrated over strips, induced drag comes from the 3D solve. Known failures: near tips the 2D assumption breaks (the tip vortex is not a section phenomenon), near stall the coupling diverges or multi-values (the nonlinear lifting line needs relaxation and can converge to unphysical branches), and at sweep the section sees the normal component of V (simple sweep theory: Re and C_l scale with cos Λ). *(Inferred; consistent with [S9] and the 2D-vs-3D remark in [S32].)*

**Leading-edge suction analogy.** Polhamus (NASA TN D-3767, 1966) recovers vortex lift on sharp-edged low-AR wings from the leading-edge suction of potential flow [S21]. Water-sports foils have rounded leading edges and AR ≥ 5, so the analogy is *not* applicable to the main wing; it is a warning label for a sharp-edged stabilizer or a foil at high yaw. *(Verified existence [S21]; applicability Inferred.)*

**Viscous drag build-up.** Wing profile drag from strips (polar-based) or, as a bound, 2 C_F(Re)·(1 + 2 t/c + 60 (t/c)⁴)·S_ref with the ITTC 1957 line [S25][S26]; mast/strut the same with its own Re; **junction drag** at wing–fuselage and strut–fuselage intersections from Hoerner's interference-drag data for T-foils with and without fairings [S26][S37] — the classic magnitude is of order C_D,int·t² per junction (t = the intersecting section thickness) with C_D,int of order 0.1–0.2 for a 90° unfaired junction, **Flagged** (recall); the gap register already lists junction drag as a gap and this session did not close it.

#### Hydrofoil-specific corrections

**Free surface — the physics.** Two limits bracket the effect of a free surface a distance h above the foil. (i) *Zero depth-Froude number* (Fr_h = V/√(gh) → 0; gravity dominates): the surface stays flat, the condition is ∂φ/∂n = 0, the image vortex has the opposite circulation — this is ground effect and lift increases. (ii) *Infinite Fr_h* (gravity negligible): the linearised condition is φ = 0 (constant pressure), the image of a vortex with circulation Γ at (0, h) is a vortex of the **same** sign at (0, −h) — Faltinsen's "negative image" of the potential — and lift decreases, halving as h → 0 (the planing-plate limit C_l = πα). Real foils are between the limits according to Fr_h, and the wave system between them costs wave drag that peaks at intermediate Fr. *(Inferred derivation this session from the boundary conditions; the closed-form infinite-Fr ratio L/L_∞ = (1 + 16(h/c)²)/(2 + 16(h/c)²) recalled from Faltinsen is Flagged — confirm against [S20] ch. 6.)*

**Free surface — usable data.** Thiart (1994) solved a hydrofoil near the surface with a linearised free-surface condition u² + w² = (g/V²)w on z = 0, using an image lifting line and trailing sheet plus a wave-making potential, with 2D section characteristics computed by a Giesing–Smith panel method at h_te/c = 1/8, 1/4, 1/2, 1 and fitted to quadratics; at Fr = 4.3 and h_te/c ≥ 1/2 predictions were "fairly well" within ≈ 1° of α, while at h_te/c = 1/4 and 1/8 the simple Prandtl correction "significantly underpredicts lift" [S16]. The 2026 JMSA study (main wing NACA 63-210, mean chord 0.0735 m, span 0.634 m, AR 10.92; rear wing NACA 63-209; mast NACA 0009; 1–4 m/s; h/c 0.5–9.5; Re 7.3×10⁴–2.9×10⁵) proposes C(α, h/c) = C(α, ∞)·[1 − 0.50·exp(−(h/c)ⁿ)] with n per coefficient, reports ≈ 17 % lift loss at h/c = 4 when Fr_h rises from 2 to 5, states that experimental values exceeded the Wadlin et al. (1955) and Hough & Moran (1969) predictions ("attributed to Reynolds effects and 3D geometry"), and localises the effect to h/c of order one [S6]. Wadlin & Christopher (1958) give a method for rectangular surfaces at finite depth including the planing limit and dihedral to 30° [S18]; Hough & Moran (1969) treat Froude-number effects on 2D hydrofoils [S17]; Kennell & Plotkin (1984, JSR 28(1) 55–64) give a consistent second-order theory for thin hydrofoils near the surface [S19]. **Implication:** any free-surface correction the workbench shows must carry (h/c, Fr_h, AR, source) and its label must say "linearised/empirical correction, Re 10⁵ scale data" — the goal-state foil operates at Re ≈ 10⁶ and h/c ≈ 3–8 (mast 0.7–1.0 m, chord 0.08–0.15 m), where the correction is small but the Fr_h dependence is not (Fr_h ≈ 3–5 at 10 m/s, h 0.5–0.8 m). *(Verified [S6][S16]; Inferred [S17][S18][S19].)*

**Ventilation.** Necessary conditions after Wadlin (1958) and Breslin & Skalak (1959): "the development of a separated flow region at subatmospheric pressure" and "the establishment of a steady supply of non-condensible gas" [S7]. Regimes: fully wetted, partially ventilated (stable or unstable suction-side cavity), fully ventilated (cavity reaches the tip) [S7][S8]. Triggers: nose ventilation via a laminar separation bubble (Fr_h < 1.0–1.25, AR-dependent), tail ventilation "driven by the downward acceleration field created by the hydrofoil" with threshold Fr_h > AR^(−1/2) (AR ~ O(1), Fr_h < 2.5), base ventilation on blunt trailing edges (no stable cavity) [S7]. Inception α: ≈ 15° at Fr_h 0.5 rising above 25° at Fr_h 1–1.25 for AR 1–1.5, i.e. higher than the Harwood et al. (2016) map [S7]. For the mast (surface-piercing, AR ≈ 3–6 immersed, Fr_h ≈ 3–5 at speed) and for a shallow front wing, the screen the tool can offer is: (a) is the section Cp on the suction side sub-atmospheric near a free-surface-connected path (mast tip-vortex, wing tip breaching), (b) is a laminar separation bubble predicted (XFOIL/NeuralFoil x_tr and H), (c) is yaw/sideslip large. None of these is a prediction. *(Verified [S7][S8].)*

**Junctions.** A strut on a hydrofoil changes lift and drag by 3D interference depending on submergence, incidence and yaw (St. Anthony Falls Laboratory towing-tank programme) [S37]; Hoerner tabulates T-foil interference drag with fairings [S26]. No 2020–2026 quantified source was found this session; carried as a gap consistent with the register. *(Inferred.)*

**Tips, winglets, downturned tips.** Bending the last 5–10 % of an elliptical NACA 16-020 hydrofoil's span by ±45° / ±90° changed lift and drag "not substantially" but gave "undeniable advantages … in TVC alleviation" (tip-vortex cavitation inception/desinence) [S30]; a 2020 computational study examines winglets on fully submerged hydrofoils [S31]. Implication: downturned tips are a tip-vortex-cavitation and ventilation-path control more than an induced-drag win at these AR; a VLM can represent the geometry (non-planar lattice) and will show the small induced-drag change, but tip-vortex cavitation is outside every low-order tier. *(Inferred.)*

**Biplane / tandem (front wing + stabilizer).** The stabilizer sits in the front wing's downwash and trailing-vortex field; its effective incidence is α_s = α + i_s − ε(x_s, y, z_s), with ε evaluated from the front-wing lattice; decalage (i_s − i_w) sets the trim point. In a VLM with both surfaces in one lattice the interaction is automatic; in a lifting-line estimator ε must be modelled explicitly (far-field 2 C_L/(π AR e) is an *upper* bound at short tail arms — Flagged direction). The rear wing also sees the front wing's wake deficit (viscous) and, near the surface, its wave — both outside the low-order tiers. Moth dynamic-VPP work shows "utilising the aft foil to generate a proportion of the lift minimises the total induced drag and increases top speed" [S36]. *(Inferred.)*

**Unsteady basics (for pumping, later).** Theodorsen (NACA R-496, 1934) gives the lift and moment of a harmonically pitching/plunging flat plate with the deficiency function C(k), k = ωc/(2V) [S22]; Garrick (NACA R-567, 1936) gives the thrust/propulsive efficiency of flapping and oscillating airfoils [S23]; Sears (1941) gives the lift response to a sinusoidal gust [S24]. Added mass of a flat plate per unit span in heave is m_a = ρ π (c/2)² [kg/m] (Flagged recall; standard). All are 2D, inviscid, small-amplitude, deep-water; the gap register's Strouhal-based pumping findings (St ≈ 0.3–0.4) sit on top of these. *(Verified existence [S22][S23][S24].)*

#### Trim and operating point

Define a body frame (GAP-04 must fix it): x aft along the fuselage, z up, origin at a named datum (mast–fuselage attachment). Unknowns for a steady straight flight at speed V: front-wing lift L_w, stabilizer lift L_s, pitch angle θ (adds to both incidences), and the rider's CG position x_cg. Equations (vertical force and pitching moment about the datum, drag components projected as needed):

L_w(θ) + L_s(θ) = W_total,  and  −L_w·(x_w − x_d) − L_s·(x_s − x_d) + M_w + M_s + W_total·(x_cg − x_d) + (drag and mast terms)·z-arms = 0,

with W_total = m_total·g [N] (rider + board + foil + wing), x in m, M in N·m. The spec deliberately limits v1 to **"Find operating α" for a single wing** (ANA-05) and forbids the label "whole-craft trim"; the equations above are the definition the later slice must implement, and the moment equation is only meaningful with a named datum and CG (ANA-11). *(Inferred; standard statics.)*

**Numerical scheme.** f(α) = L(α) − W_target on a bracket [α_lo, α_hi] restricted to the method's envelope; require sign change; solve by bisection-safeguarded secant/Illinois (or Brent) to |f|/W ≤ 1 % (spec tolerance); if no sign change, report "no supported solution" — never extrapolate. Take-off speed: for a fixed α_max,supported, V_to = √(2 W / (ρ S C_L(α_max))) is exact at the estimator tier because L ∝ V² at fixed α when coefficients are Re-independent; with Re-dependent polars iterate V → Re → C_L → V (monotone, converges in a few steps), and report the bracket and resolution (ANA-12). Envelope checks before accepting a root: α within the polar's converged range, analysis_confidence above threshold, no strip flagged separated, σ_operating > σ_required with stated margin, h/c and Fr_h inside the free-surface correction's data range. *(Inferred.)*

**Sensitivities.** With D = q S (C_D0 + C_L²/(π e AR)): (L/D)_max = ½√(π e AR / C_D0) at C_L* = √(π e AR C_D0). AR 8, e 0.9, C_D0 0.012 → C_L* 0.52, (L/D)_max 21.7; AR 12 → C_L* 0.64, (L/D)_max 26.6; halving C_D0 raises (L/D)_max by √2. Span loading W/b² sets the induced drag at a given speed (D_i = W²/(π e q b²)) — the racer's lever is span, not area. Thickness acts through C_D0 (form factor ≈ 1 + 2 t/c + 60 (t/c)⁴: t/c 0.10 → 1.206; 0.14 → 1.303) and through Cp_min (thicker LE → milder peak, wider cavitation bucket) — so thickness is a structural/cavitation trade, not a drag optimum, consistent with the register (GAP-02). *(Inferred.)*

#### Uncertainty and multi-fidelity reconciliation

The literature reports no universal estimator-vs-VLM-vs-RANS gap; the size depends on α, AR, Re and free-surface proximity. What was found: for the Kermeen hydrofoil "the lifting line approach partially recovers 3D effects, but the closest match with experiments was obtained using the three-dimensional viscous solver", and "two-dimensional solvers overestimate performance at higher angles of attack" [S32]; NeuralFoil-to-XFOIL error is ≈ 2 % in drag on hard cases and 0.37 % on simple ones [S3] — but that is surrogate error, not model error against experiment. The presentation rule is the spec's (ANA-06, A6): show each tier's number with its method, inputs and envelope; **never average**; call the difference a finding. The frameworks for saying something quantitative are ASME V&V 20 (comparison error E = S − D, validation uncertainty u_val combining numerical, input and experimental uncertainty; the modelling error is bounded by E ± u_val) and ITTC 7.5-03-01-01 [S33][S34]. Multi-fidelity surrogates (co-Kriging / hierarchical Kriging / multi-fidelity RBF) *learn* the low-to-high discrepancy over a design space and have been applied to super-cavitating hydrofoils and hull forms [S44]; they are an optimisation-area concern and require that each fidelity's provenance and envelope be stored per sample. A "computed estimate" label must carry: method + version, inputs (profile hash, Re, Ncrit, α, water snapshot), envelope status, omissions (free surface, ventilation, junctions, unsteady), Cp resolution, and "model uncertainty not quantified" unless an E ± u_val from an admitted experiment exists. *(Inferred; [S32][S33][S34][S44].)*

## Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

*Source: [08-simulation-openfoam-su2-interop.md](08-simulation-openfoam-su2-interop.md) (`kb-hw-simulation-openfoam-su2-interop`).*

#### Solver and model selection for hydrofoil questions (Re 5e5–2e6)

**Single-phase steady RANS.** For attached flow at moderate angle of attack in unbounded water,
incompressible steady RANS is the workhorse: OpenFOAM ESI `simpleFoam` (SIMPLE-family pressure–velocity
coupling) or SU2 `SOLVER= INC_RANS` with `INC_DENSITY_MODEL= CONSTANT`, `VISCOSITY_MODEL=
CONSTANT_VISCOSITY`, `INC_DENSITY_INIT` and `MU_CONSTANT` set from the run's fluid snapshot (SU2's template
default of 998.2 kg/m³ is a placeholder, not the workbench's sourced water). Turbulence: `kOmegaSST` in
OpenFOAM, `KIND_TURB_MODEL= SST` with `SST_OPTIONS= V2003m` in SU2 (SU2 also lists V1994m and production
limiters VORTICITY/KATO_LAUNDER/UQ). *(Verified, [S8][S19][S20][S23])* SU2's SA model is available with
NEGATIVE/EDWARDS/BCM variants and QCR2000; SA is the TMR's reference model for the NACA 0012 case and is a
sensible second opinion, but SST is the better default for adverse-pressure-gradient separation on thick
water-sports sections. *(Verified availability [S20]; Inferred preference.)*

**Why transition matters at these Re.** A 0.2 m chord at 6–10 m/s in 15 °C seawater is Re ≈ 1.0–1.7e6; at
these Reynolds numbers laminar separation bubbles and natural transition move with α and roughness, and the
drag difference between tripped and free-transition data on a NACA 0012 at Re 3–6e6 is about 10 % in the
wind-tunnel record the TMR curates. *(Verified for the TMR statement [S35]; Inferred for the hydrofoil Re
range.)* Fully turbulent SST therefore over-predicts drag and mis-places Cp_min for a polished foil; a
transition model is required if the 2D-polar tier (NeuralFoil/XFOIL with Ncrit) and the CFD tier are to be
reconciled rather than merely different. OpenFOAM ESI provides `kOmegaSSTLM` (4-equation Langtry–Menter
2009, fields `ReThetat` and `gammaInt`, coefficients under `kOmegaSSTCoeffs` including `ca1 ca2 ce1 ce2
cThetat sigmaThetat`) and `kkLOmega` (incompressible-only, under
`src/TurbulenceModels/incompressible/turbulentTransportModels/RAS`). SU2 provides `KIND_TRANS_MODEL= LM`
with `LM_OPTIONS` (version LM2015; correlations MALAN, SULUKSNA, KRAUSE, KRAUSE_HYPER, MEDIDA,
MEDIDA_BAEDER, MENTER_LANGTRY) and the free-stream inputs that drive it (`FREESTREAM_TURBULENCEINTENSITY`
default 0.05 = 5 %, `FREESTREAM_TURB2LAMVISCRATIO` default 10). *(Verified, [S7][S9][S20])* The design
consequence: **free-stream turbulence intensity is now a case input with physical meaning** (it is the
CFD-side analogue of Ncrit), must be stored with the run, and a tool that leaves SU2's 5 % default in place
is silently simulating a very turbulent tank, not open water. *(Inferred.)* Recent literature confirms the
class of problem: a 2022 Physics of Fluids paper on capturing transition around a low-Re hydrofoil with a
zero-equation transition model, a 2023 Fluids assessment of a RANS transition model on foils at moderate Re,
and a 2024 Ocean Engineering paper applying the Wray–Agarwal algebraic transition model to unsteady
cavitating hydrofoil flow all exist (titles/DOIs located via Crossref; abstracts not read). *(Flagged,
[S42])*

**Wall treatment.** ITTC 7.5-03-02-03 Table 1: wall-resolved needs first point y+ ≤ 1, expansion ratio 1.2,
≥ 20 points in the boundary layer; wall functions need 30 < y+ < 100, expansion 1.2, ≥ 15 points, and the
procedure warns that wall functions "become less valid, or even invalid, with increasing adverse pressure
gradients". *(Verified, [S33])* Transition models require the resolved treatment (the γ–Reθ correlations act
in the laminar and transitional sub-layers), so "transition on" implies "y+ ≈ 1", which multiplies cell
count and aspect ratio. *(Inferred from [S7][S33].)* OpenFOAM uses wall-function boundary conditions on the
`nut`/`k`/`omega` fields (`nutkWallFunction` etc.); SU2 selects per marker with `MARKER_WALL_FUNCTIONS= (
wing, STANDARD_WALL_FUNCTION )` and defaults to `NO_WALL_FUNCTION`. *(Verified for SU2 [S23]; OpenFOAM BC
names Flagged — from recall.)*

**Free surface.** OpenFOAM ESI ships `interFoam` (algebraic VOF with MULES) and `interIsoFoam` (geometric
isoAdvector VOF) as separate solvers, plus `potentialFreeSurfaceFoam`; the Foundation line has
`incompressibleVoF` as a module. SU2 has none. *(Verified, [S11][S19]; Foundation module name Flagged.)*
Near-surface lift loss, wave drag and the onset of ventilation are the hydrofoil-specific questions here;
RANS-VOF on 2D hydrofoils near a free surface is an active 2022–2024 topic (a Chesapeake Sailing Yacht
Symposium 2022 / Journal of Sailing Technology 2023 paper on free-surface effects on 2D hydrofoils by
RANS-VOF, and a 2024 Applied Ocean Research paper comparing BEM and RANS for a hydrofoil near the free
surface). *(Flagged — located by title/DOI only, [S41].)* ITTC explicitly ties outlet distance to "the
reflection of gravity waves" for free-surface computations, so the domain must be longer and damped
downstream; this is why the spec keeps free surface behind a separate de-risk slice. *(Verified for the ITTC
statement [S33]; Inferred mapping.)*

**Cavitation.** `interPhaseChangeFoam` (VOF + mass transfer) with the `Kunz`, `Merkle` and `SchnerrSauer`
phase-change models, and `cavitatingFoam` (barotropic compressibility model) are present in ESI master.
*(Verified, [S10][S11])* Cavitation needs the sourced vapour pressure the spec already demands (ANA-15) and
the cavitation number σ = (p∞ − p_v)/(½ρV²) as an input. Delft twist-11 remains the reference 3D sheet/cloud
cavitation case: 2018–2023 papers cover turbulence-model comparison (CAV2018), multi-process cavitation
modelling (2021), erosion-risk assessment with hybrid Eulerian–Lagrangian methods (2023) and LES of
cavitation tones (2022). *(Flagged — titles/DOIs via Crossref, [S42].)* SU2 has no cavitation model in the
documented solver set. *(Verified absence in docs [S19]; Inferred that none exists in the release.)*

**Unsteady, pumping and 6-DoF.** `pimpleFoam` and the dynamic-mesh/overset solver variants exist in the ESI
line, with `sixDoFRigidBodyMotion` as the rigid-body library; these are the tools for a pumping foil or a
heave/pitch response. *(Flagged — solver family from recall; the multiphase listing in [S11] does not show
overset variants because ESI keeps them inside the base solver directories.)* SU2 supports `TIME_DOMAIN=
YES` dual-time stepping and mesh deformation (`SU2_DEF`, RBF deformation since v8.3.0) but not overset in
the documented options fetched here. *(Verified for TIME_DOMAIN and RBF [S17][S23]; Flagged for overset.)*

**What is realistic on a laptop.** No fetched source gives wall-clock figures, so the following are
**Inferred cost classes** built from the ITTC mesh floor and typical cell budgets, to be replaced by the
workbench's own `solver.process` measurements (IO-series rule): a 2D section RANS at y+ ≈ 1 is 5e4–2e5 cells
and converges in minutes on 4 cores; a 3D half-wing at y+ 30–100 with wall functions is 2–5 M cells and 1–4
h on 8 cores; a 3D half-wing wall-resolved with transition is 8–20 M cells and a workstation-day; a VOF
free-surface 3D case with a resolved wave field is 10–40 M cells and unsteady, i.e. cloud or cluster class.
*(Inferred; every number here is a placeholder the instrumented product must overwrite.)*

**Decision table (question → solver → model → mesh class → cost class → honest claim).**

| Hydrofoil question | Solver (ESI / SU2) | Turbulence / physics | Mesh class | Cost class (Inferred) | What the result can honestly claim |
|---|---|---|---|---|---|
| 2D section Cl/Cd/Cp, attached, Re 5e5–2e6 | `simpleFoam` / `INC_RANS` | SST, fully turbulent, y+ 30–100 | Structured C-mesh or snappy 2D extrusion, 5e4–1e5 cells | minutes, laptop | Tripped-equivalent section coefficients; drag high by ~10 % vs free transition |
| 2D section with laminar bubble / free transition | `simpleFoam` + `kOmegaSSTLM` or `kkLOmega` / `INC_RANS` + `LM` | Transition on, y+ ≤ 1, TI stated | 1e5–2e5 cells | tens of minutes, laptop | Free-transition coefficients at the stated TI; Cp_min for cavitation screening |
| 3D wing lift/drag/spanwise loading, deep submergence | `simpleFoam` / `INC_RANS` | SST, wall functions | snappyHexMesh with layers or Gmsh BL, 2–5 M cells | hours, 8 cores | Wing-only forces in unbounded water; no free surface, no strut, no ventilation |
| 3D wing, resolved BL, transition | `simpleFoam` + `kOmegaSSTLM` | y+ ≤ 1, ≥ 20 BL points | 8–20 M cells | workstation-day | As above with transition; sensitivity to TI must be reported |
| Lift loss / wave drag near surface | `interFoam` or `interIsoFoam` | VOF + SST, gravity, wave damping zones | Interface-refined, long domain, 5–40 M cells | cloud / cluster | Depth-dependent lift and wave resistance at one Froude number; validated only if Duncan-class data agree |
| Ventilation onset | `interFoam` (+ transient) | VOF, air entrainment path | as above plus strut/surface-piercing refinement | cluster | Qualitative onset; no accepted quantitative oracle in v1 |
| Sheet / cloud cavitation | `interPhaseChangeFoam` (Kunz / Merkle / SchnerrSauer) or `cavitatingFoam` | VOF + mass transfer, σ input | Suction-side refined, 2–10 M cells, unsteady | cluster | Cavity extent and shedding frequency vs Delft twist-11 class data |
| Pumping / heave-pitch response | `pimpleFoam` + dynamic mesh / `sixDoFRigidBodyMotion` | URANS | moving or overset mesh | cluster | Phase-averaged forces; not resolved turbulence |

*(Solver/model availability Verified [S7][S9][S10][S11][S19][S20]; mesh and cost classes Inferred; claim column Inferred from [S33][S35].)*

#### Meshing

**snappyHexMesh pipeline.** `blockMesh` background hex → `surfaceFeatureExtract` (ESI) producing an `.eMesh`
feature-edge file → `snappyHexMesh` in three phases (`castellatedMesh`, `snap`, `addLayers`). The annotated
ESI dictionary documents: `features` ("Specifies a level for any cell intersected by explicitly provided
edges. This is a featureEdgeMesh"), `refinementRegions` (distance / inside / outside modes),
`locationInMesh`, and the `addLayersControls` entries `relativeSizes`, `expansionRatio`,
`finalLayerThickness`, `firstLayerThickness`, `thickness`, `minThickness` ("If for any reason layer cannot
be above minThickness do not add layer"), `nGrow`, `featureAngle` ("When not to extrude surface. 0 is flat
surface, 90 is when two faces are perpendicular"), `slipFeatureAngle`, `nRelaxIter`,
`nSmoothSurfaceNormals`, `nSmoothNormals`, `nSmoothThickness`, `maxFaceThicknessRatio` ("Stop layer growth
on highly warped cells"), `maxThicknessToMedialRatio` ("Reduce layer growth where ratio thickness to medial
distance is large"), `minMedialAxisAngle`, `nBufferCellsNoExtrude`, `nLayerIter`, `nRelaxedIter` ("Max
number of iterations after which relaxed meshQuality controls get used"). *(Verified, [S12])*

**Layer-addition failure modes on thin trailing edges.** The documented mechanisms are exactly the ones that
bite a hydrofoil: a sharp or thin TE has a small medial-axis distance, so `maxThicknessToMedialRatio`
collapses layer thickness near the TE; the TE edge angle exceeds `featureAngle`, so extrusion stops at the
edge; if the collapsed thickness falls under `minThickness` the layer is dropped entirely and
`nGrow`/`nBufferCellsNoExtrude` step the neighbouring layers down. The result is a wing with full layers
mid-chord and none at the TE, i.e. a Cd that is mesh-dependent in precisely the region that sets pressure
drag. *(Inferred from the documented parameter semantics [S12]; the practitioner remedy — a finite TE
thickness floor, a local refinement box, `featureAngle` raised, `nRelaxedIter` bounded — is Flagged, from
practice.)* This is why the spec's TE thickness floor (GEO stories) is also a meshing requirement, not only
a manufacturing one.

**cfMesh and Gmsh.** cfMesh (`cartesianMesh`) is bundled with the ESI distribution as a plugin and is GPL;
Gmsh is GPL-2.0-or-later and can export the `.su2` format directly; Pointwise/Fidelity are commercial. Both
GPL meshers are process-invocable under COMMIT-02. *(Gmsh→SU2 export Verified [S21]; licences Flagged — from
recall, not fetched this session.)* SU2 itself has no mesher; `SU2_DEF` deforms an existing mesh (RBF
deformation since v8.3.0) and `SU2_MSH` handles adaptation/periodicity preprocessing. *(Verified for RBF
[S17]; SU2_MSH role Flagged.)*

**SU2 mesh format.** ASCII `.su2` with `NDIME`, `NPOIN` (coordinates), `NELEM` (VTK element ids: line 3,
triangle 5, quad 9, tetra 10, hexa 12, prism 13, pyramid 14), `NMARK`, `MARKER_TAG`, `MARKER_ELEMS`;
`MESH_FORMAT= CGNS` is the alternative and SU2 has a built-in CGNS→SU2 converter. *(Verified, [S21][S23])*
The format is simple enough that the workbench can write a 2D structured C-mesh around a section itself —
that is the cheapest possible "no external mesher" path for the 2D slice. *(Inferred.)*

**Mesh quality that must pass.** OpenFOAM `checkMesh` reports non-orthogonality, skewness, aspect ratio,
face pyramids and determinant; snappyHexMesh's `meshQualityControls` gate the same metrics during
snapping/layering (`maxNonOrtho` — "Set to 180 to disable", `maxBoundarySkewness`, `maxInternalSkewness`,
`maxConcave`, `minVol`, `minTetQuality`, `minArea`, `minTwist`, `minDeterminant`, `minFaceWeight`,
`minVolRatio`, `minTriangleTwist`, `nSmoothScale`, `errorReduction`, `relaxed`). ITTC adds: all volumes
positive, "the 3x3 determinant for structured grids should be greater than 0.3", with a few cells down to
0.15 tolerable only with smaller time steps or more under-relaxation. *(Verified, [S12][S33])* The spec's
CFD-02 "failed mesh-quality threshold blocks solve" therefore has concrete measures: `checkMesh` must report
"Mesh OK" (or every failed check must be one the case explicitly tolerates), and the y+ *achieved* must be
checked a posteriori — ITTC: "y+ should always be checked a posteriori once the solutions are obtained".
*(Verified, [S33])*

**Mesh independence before "converged".** See Data section for the ITTC 7.5-03-01-01 equations. The product
rule that follows: a single-mesh result is labelled **Single mesh — grid uncertainty not quantified**; a
result may carry a grid uncertainty only after ≥ 3 systematically refined meshes (ratio r, ideally √2 in
every direction) with the same models, or ≥ 4 for the least-squares method. *(Verified for the procedure
[S32]; Inferred labelling rule.)*

#### Case generation without hand-edited dictionaries

**OpenFOAM case as a typed structure.** `system/controlDict` (`application`, `startFrom`, `startTime`,
`stopAt`, `endTime`, `deltaT`, `writeControl`, `writeInterval`, `functions {…}`), `system/fvSchemes`,
`system/fvSolution` (solvers, `SIMPLE { residualControl … }`, relaxation), `system/blockMeshDict`,
`system/snappyHexMeshDict`, `system/decomposeParDict`; `constant/turbulenceProperties` (`simulationType RAS;
RAS { RASModel kOmegaSST; }`), `constant/transportProperties` (ESI) versus `constant/physicalProperties`
(Foundation ≥ 11), `constant/triSurface/*.stl`; `0/U 0/p 0/k 0/omega 0/nut` (plus `0/ReThetat 0/gammaInt`
for `kOmegaSSTLM`) with `boundaryField` entries per patch. *(File and keyword names Verified where they
appear in [S7][S12][S15][S40]; the full list is Inferred from those and from recall — Flagged for exact
Foundation-14 names.)*

**Dictionary grammar.** Free-form C++-commented text; every file opens with a `FoamFile { version; format ascii|binary; class; object; }` header; keyword entries end in `;`, sub-dictionaries in `{}`, lists in `()` optionally prefixed by a count; dimensions as a 7-vector `[kg m s K mol A cd]` or a named unit; macro expansion `$keyword`, scoped `$../x`, `$:a.b`, `${…}`; directives `#include`, `#includeIfPresent`, `#includeEtc`, `#calc`, `#codeStream`, `#remove`, `#if…#else…#endif`. *(Verified, [S40])* The serialiser must therefore emit **only** the closed subset it can also parse back (no `#calc`, no `#codeStream`, no macros), because `#codeStream` compiles arbitrary C++ at run time — a security boundary the spec's "hostile data never becomes commands" row must name.

**Function objects to emit by default** (ESI): `forces` (`type forces; libs (forces); patches (wing); rhoInf
<ρ>; CofR (…)` → `force.dat`, `moment.dat` with total/pressure/viscous columns), `forceCoeffs` (`magUInf`,
`lRef`, `Aref`, `liftDir`/`dragDir`/`pitchAxis` or `e1`/`e3`, `coefficients (Cd Cl CmPitch …)` →
`coefficient.dat`), `solverInfo` (`fields (U p k omega)` → per-field initial/final residual, iterations,
converged flag, and optional residual *fields*), `yPlus`, `wallShearStress`, and `sample`/`surfaces` for
slices and surface Cp. *(Verified for forces/forceCoeffs/solverInfo [S13][S14][S15];
yPlus/wallShearStress/sample names Flagged — from recall.)* Note `forceCoeffs` needs `Aref` and `lRef` — the
derived projected area and reference chord the spec says are derived, never stored: the run record must pin
the values used, with their derivation revision.

**SU2 single `.cfg`.** Key groups verified from the template: problem (`SOLVER`, `KIND_TURB_MODEL`,
`SST_OPTIONS`, `KIND_TRANS_MODEL`, `MATH_PROBLEM`, `RESTART_SOL`); incompressible fluid
(`INC_DENSITY_MODEL`, `INC_DENSITY_INIT`, `INC_VELOCITY_INIT`, `INC_NONDIM` —
INITIAL_VALUES/REFERENCE_VALUES/DIMENSIONAL, `VISCOSITY_MODEL`, `MU_CONSTANT`, `REYNOLDS_NUMBER`,
`REYNOLDS_LENGTH`); references (`REF_AREA` — "0 implies automatic calculation", `REF_LENGTH`,
`REF_ORIGIN_MOMENT_X/Y/Z`); markers (`MARKER_HEATFLUX` or `MARKER_ISOTHERMAL` for no-slip walls,
`MARKER_FAR`, `MARKER_INLET`, `MARKER_OUTLET`, `MARKER_SYM`, `MARKER_MONITORING`, `MARKER_WALL_FUNCTIONS`);
convergence (`CONV_FIELD` e.g. `DRAG` or `RMS_DENSITY`, `CONV_RESIDUAL_MINVAL` as log10, `CONV_STARTITER`,
`CONV_CAUCHY_ELEMS`, `CONV_CAUCHY_EPS`); I/O (`MESH_FORMAT`, `MESH_FILENAME`, `SOLUTION_FILENAME`,
`RESTART_FILENAME`, `CONV_FILENAME`, `OUTPUT_FILES`, `OUTPUT_WRT_FREQ` — one frequency per output file,
`HISTORY_OUTPUT`, `VOLUME_OUTPUT`, `SCREEN_WRT_FREQ_INNER`, `HISTORY_WRT_FREQ_INNER`); time (`TIME_DOMAIN`,
`TIME_ITER`, `INNER_ITER`, `ITER`). *(Verified, [S19][S22][S23])* For an incompressible water case the
workbench should use `INC_NONDIM= DIMENSIONAL` so that the history file's forces are in SI and the fluid
snapshot is applied verbatim. *(Inferred.)*

**Marker naming from geometry.** The workbench owns the loft, so it owns the patch/marker names: recommend a
fixed vocabulary `wing`, `wing_upper`/`wing_lower` only if separately needed, `symmetry` (root plane of a
half model), `inlet`, `outlet`, `farfield` (sides/top/bottom as one slip or far-field patch), and later
`strut`, `fuselage`, `freeSurfaceTop`. The same names are the STL solid names given to `snappyHexMesh`
(`refinementSurfaces { wing { … patchInfo { type wall; } } }`) and the `MARKER_TAG`s written into `.su2`, so
the harvest layer reads forces by the same identifier on either backend. *(Inferred; snappy patchInfo
Flagged.)*

**How existing wrappers do it.** foamlib (GPL-3.0) exposes `FoamFile`/`FoamFieldFile` typed dict-like
read/write of ASCII and binary (optionally compressed) files and `FoamCase`/`AsyncFoamCase` runners for both
openfoam.com and openfoam.org distributions. *(Verified, [S30])* PyFoam and fluidsimfoam are
older/alternative Python layers of the same shape (generate dictionaries, run utilities, parse logs).
*(Flagged — not fetched.)* SU2's own automation is the `SU2_PY` package with `pysu2` (a rebuilt Python
wrapper since v8.0.0; enhancements in v8.3.0), `shape_optimization.py` and the FADO framework. *(Verified
for SU2_PY/pysu2 existence [S17][S18]; FADO Flagged.)* Every one of these is a process-plus-files design,
and every one is Python or GPL — none can be a dependency of a permissive C#/Rust product (COMMIT-02), but
their file-level behaviour is the specification to reproduce.

#### Process-driven interop from C# and Rust

**Pipeline as child processes.** OpenFOAM: `blockMesh` → `surfaceFeatureExtract` → `snappyHexMesh
-overwrite` → `checkMesh` → (`decomposePar` → `mpirun -np N simpleFoam -parallel` → `reconstructPar`) or
serial `simpleFoam` → `foamToVTK` / `postProcess`. SU2: `SU2_CFD case.cfg` (serial) or `mpirun -np N SU2_CFD
case.cfg`; SU2 writes its own VTK/CSV outputs so no conversion step is needed. *(Verified for SU2 outputs
[S22]; OpenFOAM utility names Verified where they appear in [S12][S15], the rest Flagged from recall.)* In a
container the same argv runs under `docker run --rm --name <run-id> -v <case>:/case -w /case
<image@sha256:…> <argv>`; under WSL2 under `wsl.exe -d <distro> -e bash -lc '<source bashrc>; <argv>'`.
*(Inferred.)*

**Streaming residuals and forces.** SU2 writes the history file (`CONV_FILENAME`, CSV with a header row
naming the `HISTORY_OUTPUT` fields such as `RMS_DENSITY`, `RMS_MOMENTUM-X`, `LIFT`, `DRAG`, `EFFICIENCY`)
every `HISTORY_WRT_FREQ_INNER` iterations, plus screen output at `SCREEN_WRT_FREQ_INNER`. *(Verified,
[S22][S23])* OpenFOAM ESI writes `postProcessing/<FO>/<time>/*.dat` for `solverInfo`, `forces` and
`forceCoeffs`; the classic `foamLog` script scrapes stdout into per-variable columns and is the fallback
when a function object is absent. *(Verified for the FO outputs [S13][S14][S15]; `foamLog` Flagged.)*
Design: tail the **files**, not stdout; stdout is for the log archive and for `AI-05` diagnosis, with the
file-based harvest as the only source of numbers.

**Cancellation and process trees.** .NET 10 `Process.Kill(bool entireProcessTree)` "immediately stops the
associated process, and optionally its child/descendent processes"; processes the caller cannot inspect "are
silently skipped", `WaitForExit`/`HasExited` "do not reflect the status of descendant processes", and an
`AggregateException` is raised when not all descendants could be terminated. *(Verified, [S37])* CliWrap
(MIT; .NET Standard 2.0+/.NET Core 3.0+) offers `PipeTarget`/`ListenAsync` line streaming,
`WithValidation(CommandResultValidation.None)` so a non-zero exit is data rather than an exception, and two
tokens — forceful and graceful (Ctrl-C-equivalent, "inherently cooperative"). *(Verified, [S38])* In Rust,
`duct` (MIT) provides shell-like pipelines with `.reader()` incremental output and error-by-default
semantics; `tokio::process` is the async baseline. *(Verified for duct's licence and reader [S39]; its
process-group/job-object kill semantics Flagged — the README fetch did not include them.)* The seam that
none of these libraries cross: killing `docker` or `wsl.exe` on the host does **not** guarantee the solver
inside dies. Cancellation must therefore be *addressed to the substrate*: `docker kill <run-id>` for a
container started with a known `--name`, and a signal delivered inside the distro (`wsl.exe -d <distro> -e
pkill -TERM -f <case-path>` or a recorded PGID) for WSL2, followed by host-side tree kill and a bounded
wait. *(Inferred; must be spiked on each substrate.)* MPI adds a layer: killing `mpirun` normally terminates
its ranks, but an orphaned rank after a hard host kill is a known failure shape to detect (`pgrep -f
<case-path>` after cancel). *(Flagged — practice, not fetched.)*

**Exit codes versus completion.** Both solvers return 0 when they stop at `endTime`/`ITER` regardless of
whether residuals converged; OpenFOAM also returns 0 when `residualControl` triggers an early "converged"
stop. *(Inferred from the residual-control design; Flagged for exact behaviour by version.)* The only
trustworthy completion oracle is the artefact set: for OpenFOAM the last time directory exists and contains
`U`/`p`, `postProcessing/solverInfo/<t>/solverInfo.dat` shows the final `converged` flags, and
`coefficient.dat`'s last row is within the declared averaging window; for SU2 the restart file named by
`RESTART_FILENAME` exists, `history.csv` ends at the iteration where `CONV_FIELD` met
`CONV_RESIDUAL_MINVAL`/Cauchy criteria or at `ITER`, and the `OUTPUT_FILES` were written. *(Verified for
what the files are [S15][S22][S23]; Inferred as an oracle.)* This is the concrete content of CFD-03 "a zero
exit code without required outputs is not success".

**Checkpoint / restart.** OpenFOAM: `startFrom latestTime` in `controlDict` resumes from the last written
time directory — so `writeInterval` is the checkpoint cadence and disk cost. SU2: `RESTART_SOL= YES` with
`SOLUTION_FILENAME` pointing at the previous `RESTART_FILENAME` (and `RESTART_ITER` for time-dependent
runs). *(Verified for SU2 [S19][S23]; OpenFOAM `startFrom latestTime` Flagged — recall.)* CFD-04's "resume
only where backend checkpoints actually support it" maps to: resume is offered only if the last checkpoint
is complete (all fields present, not truncated by the kill), which the harvester must verify before enabling
Retry-from-checkpoint.

**Substrates.** Official images: `opencfd/openfoam-default` (Docker Hub) is multi-arch amd64+arm64 for tags
2306–2512 and `latest` (last updated 2026-01-23); tags 2012–2212 are amd64 only; no `2606` tag existed on
2026-09-20. *(Verified, [S26])* There is no `su2code/su2` Docker Hub repository; SU2's containers (used for
CI) are elsewhere and are not a distribution channel. *(Verified absence [S27]; location Flagged.)* Native
macOS: OpenFOAM.app (gerlero) — ESI v2606/v2512 builds, Apple silicon only (Intel ended at v2.1.2 / v2506),
macOS 14+, GPL-3.0, Homebrew tap install, **not notarized**, runs from a read-only case-sensitive disk image
with user binaries in `$FOAM_USER_APPBIN`. *(Verified, [S29])* Foundation macOS: Multipass VM. *(Verified,
[S4])* Windows: WSL2 (both lines), Docker Desktop, MinGW cross-compilation (ESI) and the unofficial native
blueCFD-Core (latest 2024-1; "in continuous development"; OpenFOAM version bundled not shown on the page).
*(Verified, [S3][S4][S31])*

**Native bindings are out.** OpenFOAM's C++ API has no ABI stability across the six-monthly releases and is
GPL-3.0 — linking it would put the product under GPL (COMMIT-02 forbids). SU2's `pysu2` is a SWIG-generated
**Python** module around `CDriver`; it is not callable from C#/Rust without embedding Python, and it ties
the product to SU2's build. Both conclusions leave exactly one boundary: **generated files in, child
process, files out.** *(Verified for SU2's LGPL-2.1 and Python wrapper [S18]; GPL-3.0 for OpenFOAM Verified
via [S29]'s licence of the ESI-derived app and general knowledge — Flagged for the ESI tree's own LICENSE
file, not fetched.)*

**Parsing OpenFOAM/SU2 outputs in .NET/Rust.** No maintained .NET or Rust crate for OpenFOAM field/mesh
files or `.su2` meshes was identified this session (NuGet/crates.io were not reachable within budget).
*(Flagged.)* The interchange that avoids the problem: `foamToVTK` (legacy `.vtk`/`.vtu` per time step, with
`-fields`, `-surfaceFields`, `-patches`) on the OpenFOAM side and `OUTPUT_FILES= (PARAVIEW_MULTIBLOCK,
SURFACE_PARAVIEW, RESTART)` on the SU2 side; the workbench needs one VTK reader (XML `.vtu`/`.vtm` legacy
ASCII/binary) plus CSV/`.dat` parsers. *(Verified for SU2 outputs [S22]; `foamToVTK` flags Flagged.)* ESI
v2606 additionally advertises "VTK-HDF support" via plugins, which may become the better interchange once
stable. *(Verified mention [S2]; maturity Flagged.)*

#### Installation accountability per OS

| OS | Substrate | What "tool-owned install" can mean | Elevation / reboot | Pin | Licence exposure |
|---|---|---|---|---|---|
| macOS (Apple silicon) | OpenFOAM.app via Homebrew tap | Detect Homebrew; run `brew install gerlero/openfoam/openfoam`; detect `/Applications/OpenFOAM-v2606.app`; smoke test; explain the **unsigned/un-notarized** dialog *before* first launch | none for brew; Gatekeeper prompt | app version + OpenFOAM version string | GPL-3.0 process use; the app's own terms |
| macOS (Intel) | Docker Desktop + `opencfd/openfoam-default:2512@sha256:…` | Detect Docker; pull by digest; smoke test | Docker Desktop install needs admin once | image digest | Docker Desktop subscription terms for orgs ≥ 250 staff or ≥ $10 M |
| macOS (any) | SU2 GitHub release binary | Download release asset, verify hash, place under app-support, smoke test | none (quarantine attribute must be handled; notarization of SU2 binaries **unverified**) | SU2 tag + asset hash | LGPL-2.1 binary, process use |
| Windows | WSL2 + Ubuntu + OpenCFD apt repo or Foundation Ubuntu package | Enable WSL feature, install distro, `apt install openfoam2606-default`, smoke test | admin + possible reboot for WSL enablement | package version + distro | GPL-3.0 process use |
| Windows | Docker Desktop (WSL2 backend) + image digest | as macOS Intel | admin + WSL reboot | digest | Docker Desktop terms |
| Windows | blueCFD-Core native (unofficial) | detect only in v1 | installer | version | GPL; unofficial port — "supported backend matrix" must say so |
| Windows | SU2 GitHub release win64 binary | download + hash + smoke test | none | tag + hash | LGPL-2.1 |
| Linux (future) | apt/rpm packages, conda-forge `su2` (linux-64/aarch64) | package manager | sudo | version | GPL/LGPL process |

*(Substrate facts Verified [S3][S4][S18][S24][S26][S28][S29][S31]; the "what it can mean" column Inferred; apt package names and WSL reboot behaviour Flagged.)* Disk/RAM budgets from fetched sources: none; the ESI image is multi-GB and a 5 M-cell RANS case needs of order several GB of RAM — treat as Flagged placeholders that the `backend.setup` telemetry must replace with measured values. Version pinning: image **digest**, not tag (`latest` moved on 2026-01-23 to 2512 [S26]); SU2 release tag + asset SHA-256; OpenFOAM.app version + `foamVersion` output. Reproducibility across OSes is *not* guaranteed even at equal versions (compiler, MPI build, ARM vs x86 floating-point differences) — the run record must store OS/arch and the exact solver build string, and cross-platform agreement is a test, not an assumption. *(Inferred.)*

#### Validation cases

| Case | What it validates | Data / access | Terms |
|---|---|---|---|
| NASA TMR 2D NACA 0012 (M 0.15, Re 6e6; Ladson tripped forces, Gregory & O'Reilly Cp; Abbott & von Doenhoff untripped) | Fully turbulent RANS implementation and the workbench's coefficient/reference conventions; Cp resolution at the LE peak | tmbwg.github.io/turbmodels (digitised data, grids, CFL3D/FUN3D reference results) | US-government public data *(Verified data set, terms Inferred)* [S35] |
| NASA TMR 2D zero-pressure-gradient flat plate (M 0.2, Re 5e6 per unit length, plate length 2) | Wall treatment, Cf and u+ vs y+; the first thing to run after install (verification, not validation) | same site; multiple grid levels; used for DPW-5 | same [S36] |
| Duncan (1983), *J. Fluid Mech.* 126 — towed 2D hydrofoil under a free surface, breaking and non-breaking wave resistance | interFoam/interIsoFoam wave field, wave drag and lift loss vs depth/Froude; the free-surface de-risk oracle | Journal paper (data digitised from figures by later authors) | Cambridge UP paywall; DOI **not confirmed** this session *(Flagged — citation from recall; Crossref lookup of the recalled DOI returned 404)* [S46] |
| Delft twist-11 hydrofoil (Foeth et al., TU Delft) | 3D sheet/cloud cavitation dynamics, shedding frequency; erosion-risk methods | Thesis + papers; geometry published; 2018–2023 CFD papers exist [S42] | Academic; access to raw data varies *(Flagged)* [S47] |
| NACA 66(mod) (Shen & Dimotakis 1989 cavitating/non-cavitating; Brockett section) | 2D Cp and cavitation inception on a marine section | Journal/report | *(Flagged — recall)* [S48] |
| 2D NACA 0015 near free surface (several tank experiments) | Near-surface lift/drag at fixed depth ratios | Literature; used by 2022–2024 RANS-VOF papers [S41] | *(Flagged — specific dataset not identified)* |
| ONR / INSEAN hydrofoil and appendage sets | Strut/junction and full-scale marine cases | Not reached this session | *(Flagged)* |

#### Result harvesting and evidence

Read back per run: force and moment components with the reference values used (ρ, V, `Aref`/`REF_AREA`,
`lRef`/`REF_LENGTH`, moment centre, lift/drag axes), residual history per equation with the converged flag,
y+ distribution on the wing (min/max/mean and the field for masking), surface Cp, wall shear stress / Cf
(the only supported separation criterion — τ_w·x̂ < 0 or Cf sign change along a surface streamline),
velocity on named slices, modeled k and ω (or ν̃ for SA) with the model name, and for transition runs
`gammaInt`/intermittency. *(Verified availability of these fields: OpenFOAM
`forces`/`forceCoeffs`/`solverInfo` [S13][S14][S15]; SU2 `VOLUME_OUTPUT` groups `PRIMITIVE`, `SOLUTION`,
`RESIDUAL`, `SKIN_FRICTION`, `Y_PLUS` [S22]; separation criterion Inferred and consistent with the grounding
doc's rule that vortex criteria are not separation.)* Formats: OpenFOAM time directories → `foamToVTK` →
`.vtk`/`.vtu`; SU2 → `.vtu`/`.vtm`/`.szplt`/`.csv`; CGNS is a mesh input for SU2, not a results interchange
here. *(Verified for SU2 [S21][S22].)*

**Run-record metadata that must be pinned** (ties to the spec's Analysis run / Sweep sample / Field evidence
aggregates): backend line and version string (`OpenFOAM-v2606` / `SU2 v8.5.0`), substrate and pin (image
digest, app version, release asset hash), OS/arch, MPI ranks, geometry revision hash and STL export
tolerance, fluid snapshot (ρ, μ, T, salinity, source version, p_v if cavitation), operating pair (V, α, β),
turbulence and transition model with TI/viscosity ratio, wall treatment and target y+, mesh generator
settings hash, mesh stats (`checkMesh` summary, cell count, achieved y+), schemes/solver settings hash,
convergence criteria and whether they fired, averaging window for reported coefficients, checkpoint cadence,
wall-clock and CPU seconds, exit code **and** the artefact-oracle verdict, and the
function-object/output-file inventory that defines the "available field inventory" the UI shows. *(Inferred
from [S13][S15][S22][S23][S32][S33] and the spec.)*

## Optimization strategies for 2D foil sections and 3D hydrofoil wings

*Source: [09-optimization-strategies.md](09-optimization-strategies.md) (`kb-hw-optimization-strategies`).*

#### 1. From rider brief to optimization problem

**The brief is design intent, not a constraint (A3).** The spec already says "a riding brief is design intent; it is never silently a geometry constraint or a solved operating point" [S24]. The translation therefore has to be explicit and inspectable: brief → **goal state** (a value object) → operating-point set + objective(s) + constraint set → design vector → evaluation ladder → candidate set. Each arrow carries provenance.

**Objectives that the literature actually optimizes.** (a) Minimum drag at one or more lift constraints — the canonical form in Drela [S1], Garg [S2], Ng [S4] and the book's Mach-robust airfoil example [S6]. (b) Maximum weighted L/D over a CL set: Garg's η̄ = Σ w_k CL_k/CD_k with w_k = p(CL_k)ΔCL [S2]. (c) Cavitation inception speed as an objective or a constraint (Garg reports V_cav = √((P_ref−P_vap)/(½ρ·(−Cp_min))), P_ref = P_atm + ρ g h [S2]; file 04 gives the σ tables [S23]). (d) Structural mass or stress as a second objective/constraint (Garg 2017 KS-aggregated von Mises ≤ fatigue strength / 1.1) [S2]. (e) For race wingfoil specifically (Inferred from [S23]): minimize the wind-band-weighted drag at the seven operating points, with take-off feasibility (CL_required ≤ CL_max(Re) with margin) and top-end cavitation margin as constraints rather than objectives, because both are pass/fail for the rider.

**Operating-point sets.** Drela used 1, 2, 4 and 6 points and found the multipoint optimum can "scallop" between sampled points when the design space is rich [S1]; his rule "points = O(design parameters)" and "sample somewhat beyond the expected operating range" are the only quantitative guidance found. Garg used five CL points with weights 0.10/0.15/0.25/0.45/0.05 and a "critical metric" per point (pressure-side cavitation at CL = −0.15; drag at 0.30/0.50/0.65; stress and suction-side cavitation at 0.75) [S2] — a pattern worth copying verbatim: *each operating point declares what it is there to protect*. For the goal-state example the seven rows of file 04 are the sample; **Inferred** weights that reflect race time-on-task (upwind/downwind legs dominate; take-off and top-end are short but critical) are shown in the Data section, labelled as assumptions.

**Constraint catalogue (what to model in v1, whether or not the optimizer exists).**

| Constraint | Form used in the literature | Evidence tier needed | Label · source |
|---|---|---|---|
| Lift = load at each point | CL_k(α_k) = W/(q_k S), α_k a design variable per point | any tier | Verified [S2][S8] |
| Cavitation | A_cav ≤ 5×10⁻⁴ (smoothed area, k = 10) [S2]; min-pressure constraint [S4]; screening −Cp_min ≤ σ − margin [S23] | 2D polar for screening; RANS to certify | Verified |
| Structural | KS-aggregated von Mises ≤ σ_f/1.1 [S2]; beam bending/deflection/twist (GAP-02) | FEM or beam model; **not modelled in v1** | Verified [S2]; gap Flagged |
| Manufacture | TE thickness ≥ 1.1 × baseline at 20 spanwise points (0.242 → 0.121 mm root→tip) [S2]; local thickness at x/c (0.33, 0.90) and TE angle ≥ 6.03° [S8]; GAP-07 min TE, layup floor | geometry only | Verified [S2][S8] |
| Geometric validity | local thickness > 0 everywhere (no crossing) [S8]; fixed LE (10 constraints) [S2]; min LE radius (Inferred) | geometry only | Verified/Inferred |
| Regularity | Wahba-type wiggliness measure on the CST curve [S8]; curvature bounds (Drela: "constraints such as on surface curvature could instead be imposed … in effect a reduction in the effective number of free geometric design parameters") [S1] | geometry only | Verified |
| Model trust | `analysis_confidence > 0.90` [S8] | surrogate tier | Verified |
| Class rules | GWA front ≥ 700 cm², rear ≥ 150 cm², mast ≤ 115 cm, fuselage ≤ 100 cm [S23] | geometry only | Verified in [S23] |
| Pitching moment | CM ≥ −0.133 [S8]; stabilizer trim (Inferred) | 2D/VLM | Verified/Inferred |
| Robustness | expected value over the point PDF [S6][S2]; CVaR (Rockafellar–Uryasev 2000) as a tail measure [S25] | any | Verified book/paper; CVaR use in aero Flagged |

**Robustness formulations.** The book's Chapter 12 distinguishes robust design (statistics of the objective) from reliable design (probability of constraint violation) [S6]. Expected value with discrete weights is what every hydrofoil paper found uses [S2][S4]. Worst-case over the band is the conservative alternative; CVaR_α (mean of the worst (1−α) tail) is the standard smooth compromise from finance [S25] and is a natural fit for "the foil must not fail at the top-end" — its use on hydrofoils was not found this session (Flagged).

**Worked problem statement for the goal state (Inferred; every number's label is in the Data table).**

```
given   goal state G = { rider 90 kg (given); board 5.5, foil 4.5, hand wing 2.4, kit 3.0 kg (Flagged);
                         W = 1030 N; front-wing load L = 1050 N (Flagged trim share);
                         water = ITTC seawater 35 g/kg 15 °C record (Verified); depth h ∈ [0.3, 0.5] m (Flagged);
                         wind band 10–20 kn → operating points k = 1..7 (speeds Flagged);
                         class preset = GWA race box (Verified); Ncrit pair {2, 4} (practitioner range) }
find    x = [ chord(η) controls, twist(η) controls, sweep(η) controls, elevation(η) controls, t/c(η) controls,
              CST deltas per authored station (≤ 18 each), α_k for k = 1..7 ]            — the record's own controls
minimize  J(x) = Σ_k w_k · D_k(x) / D_k(x₀)            (weighted drag, normalised to the start design)
          — or the pair ( Σ_k w_k D_k ,  −V_cav,min ) for a two-objective front
subject to  L_k(x, α_k) = 1050 N                       k = 1..7            (lift equality; α_k free)
            CL_1 ≤ CL_max(Re_1, Ncrit) − 0.10          (take-off margin, both Ncrit)  — Flagged margin
            −Cp_min,k(η) ≤ σ_k − Δσ   ∀ strips, k = 6, 7 (suction side), k = 1 (pressure side); Δσ = 0.1 Flagged
            analysis_confidence_k(η) ≥ 0.90            ∀ strips, k                     (surrogate trust)
            S ≥ 700 cm², rear ≥ 150 cm², mast ≤ 115 cm (GWA)                           (class box)
            t_TE(η) ≥ t_TE,min ; t(x/c, η) > 0 ; r_LE(η) ≥ r_LE,min ; TE angle ≥ θ_min   (manufacture, validity)
            wiggliness(section_j) ≤ w_max ; |dchord/dη| bounded ; stations non-crossing    (regularity)
            structural proxy: M_root(x) ≤ M_root(x₀)  OR  ∫M dη ≤ ∫M dη |x₀  OR  tip twist ≤ θ_tip  (choose one; GAP-02)
            CM_k ≥ CM_min (stabilizer authority)                                          (trim placeholder)
            x_lb ≤ x ≤ x_ub                                                               (bounds)
robustness  expected value over w_k (default) | worst case over k | CVaR_0.8 over k        (user-visible switch)
tiers       T1 sections (NeuralFoil-class) → T2 wing (VLM + strips) → T3 CFD re-check (points 1 and 7)
provenance  every evaluation: x-hash, tier, method version, Ncrit, water id, converged, confidence, ms
```

Reading the constraint set: two of the rows (take-off margin and the cavitation margin) are the rider-safety rows and are constraints, never objectives; the structural row is a *proxy* until GAP-02 closes; the trim row is a placeholder because v1 authors one lifting wing. A problem with the seven-point set and one all-wind 850 cm² wing is the same statement with S fixed; the two-wing formulation is two runs sharing the section family and the class preset. The normalisation D_k/D_k(x₀) keeps the seven drag values commensurable (they span q from 13.6 to 139 kPa). A weighted sum of *forces* without normalisation would let the top-end point dominate by q alone — a silent weighting error (Inferred).

**How the weights should be set (what the sources allow).** Garg: w_k = p(CL_k)·ΔCL from a PDF of operating conditions, admitted to be experience-based for a canonical foil [S2]. Drela: weights "are arbitrary, and their appropriate values cannot be easily estimated without prior experience" [S1]. Martins & Ning: the discrete-probability reading makes the weights a *distribution*, so their basis (race GPS time-on-task, or a stated guess) must be stored with the goal state [S6]. The seven weights in the Data table are a guess and are labelled so.

#### 2. Design variables and parameterization for optimization

File 02 already establishes the representation facts: Masters et al. (2017) needed 20–25 variables for any of seven methods to cover the UIUC database, SVD most efficiently; a 2025 review ranks CST first for optimization with < 22 variables; CST coefficients have global support and are poor direct-manipulation handles; CST is retained as *optimizer coordinates derived from the record* [S26]. This file adds only the optimization consequences.

- **CST order in practice.** NeuralFoil's input is 8 CST weights per side + LE-modification + TE thickness = 18 parameters [S7]; the tutorial bounds them to [−0.5, 0.25] lower / [−0.25, 0.5] upper and LE weight [−1, 1] [S8]. SU2's 2D tutorial uses 30 Hicks–Henne bumps [S27]. Garg used 210 FFD shape variables with a coupled adjoint [S2]. Drela's hypothesis — richer geometry ⇒ finer exploitable scale ⇒ more sample points — is the reason to keep the section design space small at cheap tiers [S1]; Drela's own reading of NeuralFoil's 18-parameter space confirms the benefit [S7].
- **3D: FFD vs the workbench record.** FFD (Garg [S2]; SU2 3D tutorials [S27]; pyGeo Apache-2.0 [S13]) moves a mesh; that is the right tool *inside* a CFD-adjoint loop but it violates the workbench's "one explicit parametric surface" if its output became the record. The workbench's design vector should be the record's own controls: the five distribution curves' control values/weights (Smooth mode) or anchor values/tangents (Through-points mode), plus per-station profile CST deltas from the catalog profile (Inferred from [S24][S26]). A CFD-adjoint tier that needs FFD must project its shape gradient back onto those controls (chain rule through the loft evaluator) or be used only as a *re-verification* tier, never as the design-variable owner (Inferred).
- **Mode-based and generative parameterizations.** SVD/PCA modes from an airfoil database (Poole, Allen & Rendall 2015 [S28]; Masters 2017 via [S26]) give the most compact linear space; Bézier-GAN (Chen, Chiu & Fuge 2020) and FFD-GAN (Chen & Ramamurthy 2021) learn a nonlinear latent space that "accelerate[s] design optimization convergence by improving the representation compactness" [S29][S30]; file 02 flags 2025–26 latent-diffusion generators as "watch, not adopt" [S26]. All of these are optimizer-side coordinate systems; the record stays the B-spline definition with a reported fit residual [S26].
- **Regularity constraints are part of the design vector definition, not an afterthought:** thickness > 0, TE thickness floor, TE angle floor, LE fixed/LE radius floor, wiggliness/curvature bound, monotone-or-bounded chord and thickness distributions along span, no station crossing, and the class-rule box [S1][S2][S8][S23].

#### 3. Algorithms by tier

**Selection logic (book decision tree [S6]):** convex? → LP/QP; discrete? → branch-and-bound/GA; differentiable? → BFGS (unconstrained) or SQP/IP (constrained), with multistart if multimodal; not differentiable → gradient-free (DIRECT, GPS, GA, PS, Nelder–Mead), noisy/expensive → surrogate-based (Ch. 10, EGO with expected improvement), uncertainty → Ch. 12, multiple disciplines → Ch. 13.

**By evaluation cost (Inferred mapping of the workbench tiers onto that tree):**

| Tier | Cost per evaluation | Derivatives available | Appropriate algorithms | Evidence for the mapping |
|---|---|---|---|---|
| Closed-form estimator | µs | analytic / AD trivially | anything; use it for DOE, sensitivity screening and as the lowest MF level, never for certification (COMMIT-01) | Inferred |
| NeuralFoil-class polar | ms (README benchmark table lists ~6 ms per evaluation for `xxxlarge`; column reading Inferred) | exact via CasADi in AeroSandbox; C∞ by construction | IPOPT/SLSQP with multistart; CMA-ES/DE/NSGA-II affordable (10⁴–10⁵ evals) | Verified speed/derivatives [S7][S9]; mapping Inferred |
| XFOIL polar | ~0.1–1 s (Flagged recall) | none (hysteresis, non-C¹ [S7]) | gradient-free only (Xoptfoil2 pattern [S31]); treat unconverged points as non-computable [S16] | Verified properties; timing Flagged |
| VLM + strip theory | ms–s | AD if the VLM is written in an AD framework (AeroSandbox, OpenAeroStruct [S32]) or FD/complex-step | SQP/SLSQP; NSGA-II for planform Pareto fronts | Verified tools; mapping Inferred |
| 2D RANS (SU2/OpenFOAM) | minutes | adjoint (SU2 continuous/discrete [S27]; DAFoam GPL-3.0 process-only [S13]) | EGO/MFBO with tens of evaluations, or adjoint + SLSQP | Verified tools; mapping Inferred |
| 3D RANS (+FEM) | hours | coupled adjoint (ADflow LGPL-2.1 + TACS in Garg [S2]) | adjoint + SQP (SNOPT in Garg, licence-restricted) | Verified [S2] |

**Gradient-free.** Nelder–Mead, pattern search, DE, PSO, CMA-ES, GA are all in pymoo (Apache-2.0) with SRES/ISRES for constraints [S33]; CMA-ES's reference is Hansen's tutorial (arXiv 1604.00772) [S34] and pycma (BSD-3) [S13]. Their cost grows explosively with dimension (book: 103 → 32,000 calls from 1 to 30 variables) [S6], so they belong to the µs/ms tiers or to ≤ 10-variable planform problems.

**Multi-objective.** NSGA-II (Deb et al. 2002) [S35] and NSGA-III for many objectives (pymoo lists NSGA-II, R-NSGA-II, NSGA-III, U-NSGA-III, MOEA/D, C-TAEA, AGE-MOEA, RVEA, SMS-EMOA…) [S33]; Optuna ships TPE, CMA-ES, GP, NSGA-II, NSGA-III, QMC and grid samplers [S36]. The book warns the weighted-sum method "is easy to use, but it is not particularly efficient" and cannot reach non-convex parts of the front; ε-constraint and NBI are the alternatives [S6]. For the workbench the practical front is two-dimensional (cruise drag vs take-off feasibility margin, or drag vs cavitation-inception speed) and NSGA-II at the NeuralFoil/VLM tier is affordable (Inferred).

**Bayesian / surrogate-based.** EGO (Jones, Schonlau & Welch 1998) with Kriging + expected improvement [S37]; Forrester, Sóbester & Keane (2008) is the engineering text [S38, not opened — Flagged]; SMT (BSD-3) provides Kriging, KPLS, gradient-enhanced Kriging and MFK [S18][S39]; BoTorch/Ax (MIT) provide batched, constrained and multi-fidelity acquisition functions [S19]. Ploé's thesis is the direct hydrofoil precedent: GP surrogate on RANS data with a sequential acquisition function balancing multiple criteria [S40]. Sacher's classifier for non-computable points [S16] is mandatory when XFOIL or a CFD backend is in the loop.

**Multi-fidelity.** Kennedy & O'Hagan (2000) autoregressive model [S41]; Han & Görtz hierarchical Kriging (2012) [S42]; SMT MFK: y_hi = ρ(x)·y_lo + δ(x), nested samples only, `propagate_uncertainty` option [S18]; BoTorch MFKG: joint GP over (x, s), `AffineFidelityCostModel(fixed_cost, fidelity_weights)`, `InverseCostWeightedUtility` — "the MFKG acquisition function optimizes the ratio of information gain to cost" [S19]; Sacher 2021 non-nested infilling [S20]; Toal 2023 assesses multi-fidelity multi-output Kriging for design optimization [S43]. **Precondition:** the low-fidelity tier must be *informative* about the high one; the workbench has no measured tier correlation (GAP-06), so MF is "after the ladder is calibrated" (Inferred).

**Gradient-based and derivatives.** Finite differences (step-size error), complex step (book Fig. 6.9: error falls to machine precision for h → 10⁻²⁰⁰) [S6], algorithmic differentiation (CasADi in AeroSandbox [S9]; JAX Apache-2.0 exists but is not used by AeroSandbox [S13]), and adjoints (Jameson 1988 [S44]; SU2 continuous and discrete adjoint selectable by flag, Hicks–Henne/FFD variables, SLSQP driver via `shape_optimization.py`, `OPT_ITERATIONS = 100` [S27]; DAFoam for OpenFOAM, GPL-3.0 [S13][S45]; ADflow LGPL-2.1 [S13]). Constrained solvers: SLSQP (SciPy, BSD-3), IPOPT (EPL-2.0), SNOPT (proprietary — used by Garg), pyOptSparse wraps ALPSO, CONMIN, IPOPT, NLPQLP, NSGA2, PSQP, SLSQP, ParOpt, Uno, SNOPT but is LGPL-3.0 [S46]. OpenMDAO (Apache-2.0) drivers in the source tree: `scipy_optimizer`, `pyoptsparse_driver`, `pymoo_driver`, `modopt_driver`, `differential_evolution_driver`, `genetic_algorithm_driver`, `doe_driver`, `analysis_driver` [S47].

**Convergence, restarts, and exploitation detection.** (i) SQP convergence = KKT residual + feasibility tolerance; report both, never "converged" alone (book Ch. 5 [S6]). (ii) Multistart from ≥ N random feasible starts and from the catalog profile; if the best two optima differ in objective by more than the tier's stated model error, the problem is multimodal at that tier (Bons 2019 [S21]; Inferred rule). (iii) Exploitation tells, all Verified in the sources: bumps at bubble location [S1]; drag polar scalloping between sampled points [S1]; `analysis_confidence` dropping toward the constraint bound [S7][S8]; optimum sitting on a parameterization bound; an unbounded direction (the spike's span blow-up, Finding 10). (iv) The cross-check that the AC75 paper used — re-analyse the optimum with an *independent* method and require the gain to survive [S4][S5] — is exactly COMMIT-01's re-evaluation rule.

#### 4. Section-level specifics

- **Cavitation-bucket widening.** Eppler's hydrofoil trilogy and the E817/E818 aft-loaded "rooftop" family are in file 06 [S48]; the design tool for that family is inverse: Eppler's program specifies the velocity distribution "not for one but many different angles of attack" and iterates the TE angle [S22]. In optimization form, the bucket is widened by constraining −Cp_min (or A_cav) at *both* ends of the CL band (Garg's CL = −0.15 for pressure-side and 0.75 for suction-side cavitation [S2]; Ng's ranges of flap angle, CL and speed [S4]). For the goal state: constrain −Cp_min ≤ σ(32 kn) − margin at CL ≈ 0.10 and pressure-side −Cp_min at CL ≈ 0 (Inferred; σ values [S23]).
- **Transition robustness.** File 06 already requires polars at Ncrit 2 and 4 and treats the spread as transition uncertainty [S48]; XFOIL's own Ncrit table (sailplane 12–14 … dirty wind tunnel 4–8) has no water row [S17]. Optimization consequence (Inferred): evaluate every operating point at the Ncrit pair and take the worse value (min-max) or the mean — never a single Ncrit; and constrain `analysis_confidence` because bubble-dominated regimes are exactly where the surrogate is least trustworthy [S7].
- **Thickness/structure trade.** Garg 2019's optimized foil "is significantly thicker to withstand higher loads than the baseline" and still measured +29 % L/D [S3]; without a structural model, thickness has no hydrodynamic optimum and drifts to the manufacturing floor (GAP-02/07 [S49]). v1 therefore needs a *fixed* thickness distribution or a beam-model proxy before any section optimizer runs (Inferred).
- **XFOIL/NeuralFoil-in-the-loop pitfalls (Verified):** XFOIL non-C¹ outputs, hysteresis, crashes; NeuralFoil always answers but flags low confidence; larger networks risk "overfitting to XFoil's inherent non-smoothness" [S7]; XFOIL's INIT/iteration-limit/gradual-α advice [S17]. Rules that follow: treat an unconverged XFOIL point as non-computable (classifier [S16]), never interpolate across it; freeze the surrogate version hash into the run provenance; re-verify the optimum with XFOIL at the polar level and with VLM at the wing level before showing it as anything but "surrogate candidate".
- **Inverse design in a workbench.** MDES/QDES are cursor-edited target-Q distributions with automatic closure corrections [S17]; PROFOIL (Selig) is the multipoint inverse code behind S1223 [S48] (site not opened — Flagged). Place (Inferred): a "target Cp" editor is a *profile editing mode* in the Profile catalog context, not an optimizer; it produces a Profile revision with provenance "inverse design from target Cp v…".

#### 5. Wing-level specifics

- **Planform + twist + section blending.** The design vector is the five channels plus station profiles [S24]; induced drag is a planform/twist problem, profile drag and cavitation are section problems, and they couple through the local CL(η) at each operating point — which is why the optimizer must evaluate strip polars at the *local* Re and CL, not one wing-average (Inferred from [S23] Re bands).
- **Induced drag under structural constraints.** Prandtl 1933: fixed lift and *integrated* bending moment → bell load Γ ∝ (1−x²)^{3/2}, span +22 %, drag −11 % (Bowers TP-2016-219072 [S14]; spike reproduces 1.2247 / 0.8889). Jones 1950: fixed lift and *root* bending moment → "15-percent reduction … with a 15-percent increase in span" [S15]. Which moment is fixed changes the optimum; a wingfoil's limiting structure is the mast–fuselage–wing junction (root), but tip-twist under load matters at AR 11–13 (GAP-02). The workbench should let the goal state name the structural proxy (root BM, integrated BM, tip deflection) and show which one the optimizer used (Inferred).
- **Anhedral/dihedral near the free surface.** No primary quantification of anhedral effects for consumer foils was found in this or the file-04 session [S23]; Wadlin & Christopher 1958 (finite-depth lift with dihedral to 30°) is cited only through secondaries there. Treat anhedral as a *stability/feel* variable with a Flagged hydrodynamic effect, and keep h/c and Fr_h in every operating point [S23].
- **Stabilizer sizing and decalage.** Trim requires Σ moments = 0 about the rider/mast reference at each operating point; iQFOiL's −2° … +1° shim range is the concrete envelope [S23]. AC75 VPP-driven work (Tannenberg et al. JST 2023) argues foil lift/drag "do not directly translate to the performance of the yacht on the race course" and optimizes inside a whole-craft VPP [S50] — the wing-only optimizer must state that trim and stabilizer drag are held, not optimized (Inferred; v1 authors one lifting wing [S24]).
- **Structural coupling.** OpenAeroStruct couples a VLM with "a 6 degrees of freedom 3-dimensional spatial beam model" under OpenMDAO (Apache-2.0) [S32]; Garg's coupled RANS–FEM adjoint is the high end [S2]; composite bend–twist coupling changes the loaded twist (GAP-02 [S49]). A beam model is the cheapest way to make thickness a *solved* variable; it is the single most valuable addition before an optimizer is trusted (Inferred, consistent with [S49]).
- **Pumping efficiency.** Human pumping sits at St ≈ 0.05–0.15, below the 0.25–0.40 flapping-foil optimum, so glide L/D at CL 0.4–0.7 dominates [S23]; a Strouhal-based unsteady objective is a *third* objective for later, needing an unsteady tier the workbench does not have (GAP-05).

#### 6. Workflow tooling and where it may run

See the Comparables table for licences. Under COMMIT-02 (permissive linked dependencies; process invocation allowed) [S24]:

- **In-process (.NET):** own SLSQP/SQP or an interior-point on Math.NET Numerics (MIT) [S13] — no permissive, maintained, constrained-NLP library for .NET was verified this session (Flagged); Nelder–Mead/CMA-ES/NSGA-II are small enough to own (Inferred). **Rust door:** argmin (Apache-2.0 verified; MIT dual Flagged) [S13].
- **Optional Python sidecar (process):** AeroSandbox + NeuralFoil (MIT) with CasADi (LGPL-3.0) + IPOPT (EPL-2.0) [S9]–[S12]; SciPy, pymoo, SMT, pycma, Optuna, BoTorch/Ax, OpenMDAO, OpenAeroStruct (all permissive) [S13]. Distribution of the sidecar's LGPL wheels requires the licence-review step COMMIT-02 names; process isolation keeps the product itself clean (Inferred from the sequence's SU2 reasoning [S51]).
- **Process-only solvers:** SU2 (LGPL-2.1) shape optimization via `shape_optimization.py` [S27]; DAFoam (GPL-3.0) [S45]; Dakota (LGPL-2.1) [S13]; MACH-Aero (no licence file in repo — Flagged) [S13]. **Excluded from linking:** pyOptSparse, NLopt, ADflow, CasADi (as a .NET link) [S13].

#### 7. Presenting results honestly

- **Pareto front and trade-off plots** — each point labelled with the tier that produced it and whether it has been re-verified; dominated points shown greyed, not deleted (Inferred; A6 "out-of-envelope values remain visible" [S24]).
- **Constraint-activity display** — active/inactive/violated per constraint per operating point, with the Lagrange multiplier or shadow price where SQP gives one (book Ch. 5 [S6]); Garg's "critical metric per point" table is the model [S2].
- **Sensitivity/tornado** — first-order sensitivities from the AD/adjoint gradient at the optimum, or from a DOE at the estimator tier; label the tier.
- **Provenance per evaluation** — design-vector hash, tier/method/version (NeuralFoil model size, XFOIL build, VLM settings, CFD case hash), Ncrit, water record id, `analysis_confidence`, converged flag, wall time; per run — algorithm, seed, iterations, evaluations, termination reason, restarts (instrumentation-over-inference; A3 Analysis run invariants [S24]).
- **Re-verification ladder** — "surrogate candidate" (NeuralFoil/estimator) → "VLM-checked" (wing forces at each point re-evaluated) → "CFD-checked" (RANS at the critical points, the Ng/Yildirim cross-check pattern [S4][S5]) → "Experimentally compared" only with data (A6 [S24]; Garg 2019 shows what that costs [S3]). COMMIT-01 forbids certification on estimator evidence; "certified" at any tier means *re-evaluated at that tier with the gains surviving* — never a structural sign-off.
- **Candidate → editable design** — because the design vector *is* the record's controls, a candidate is a Geometry edit draft (A3) over the base Design revision; accepting it creates Surface/Profile revisions whose provenance names the optimizer run and evaluation ids; no mesh, no FFD lattice, no coordinate dump becomes the record (COMMIT-03 [S24]).

**Provenance record shape (Inferred; a sketch for /define-architecture, not a schema decision).**

| Record | Fields the sources make necessary | Why (source) |
|---|---|---|
| Goal state (value object) | masses with labels; water record id; depth band; wind→speed band + label; class preset id + rule version; operating points [speed, depth, load, critical metric, weight, Ncrit pair]; objective form; constraint rows (typed, with margins and their labels); robustness rule; structural proxy choice | brief → intent, never a constraint (A3 [S24]); per-point critical metric and weights [S2]; margins Flagged until calibrated |
| Design vector definition | ordered (curve id, control index, component, lb, ub, frozen) + (station id, CST index, lb, ub, frozen); base Design revision id | record's own controls (A4 [S24]; file 02 [S26]); bounds from the NeuralFoil tutorial pattern [S8] |
| Evaluation | design-vector hash; tier; method + version (NeuralFoil size, XFOIL build, VLM settings, CFD case hash); Ncrit; water id; per-point outputs; converged; analysis_confidence (nullable → "not recorded"); non-computable flag; wall time ms | XFOIL non-convergence/hysteresis [S7][S17]; classifier [S16]; instrumentation rule |
| Optimizer run | algorithm + library version; seed; restarts and their start points; iterations; evaluations by tier; termination reason; final KKT and feasibility residuals; constraint activity per point; Pareto set ids | KKT reporting [S6]; multimodality [S21]; termination variant (GO) |
| Candidate | run id; evaluation id; status ∈ {surrogate-candidate, vlm-checked, cfd-checked, experimentally-compared}; promotion evidence (higher-tier run id, gain survived %) ; Geometry edit draft id | COMMIT-01 [S24]; cross-check [S4]; validation costs [S3] |

**What "certified" may mean at each rung (Inferred from A6 and COMMIT-01 [S24]).** *Surrogate candidate*: "improves the weighted objective by X % under NeuralFoil/estimator; not verified." *VLM-checked*: "re-evaluated with VLM + strip polars at all seven points; gain Y % survives; free-surface/junction/ventilation omitted." *CFD-checked*: "RANS at points 1 and 7 (take-off and top-end) reproduces the section Cp_min within Z; wing-level drag not re-verified." *Experimentally compared*: only with admitted measured data of a comparable configuration — the Garg 2019 standard [S3]. None of these is a structural sign-off; the spec's own wording ("without calling that a structural sign-off") stands.

## Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

*Source: [10-integration-and-ai-workflows.md](10-integration-and-ai-workflows.md) (`kb-hw-integration-and-ai-workflows`).*

#### 1. Workflow composition patterns in engineering tools

**Model-as-graph.** OpenMDAO structures a model as a `Problem` owning a hierarchy of `Group`s containing
`ExplicitComponent`/`ImplicitComponent` nodes, connected by named variables; the connections define a data-flow graph,
and derivatives (analytic partials, finite difference, complex step, unified total derivatives) propagate along it;
`Driver`s (optimizers, DOE) sit outside the model and call it; solvers (Newton, `BalanceComp`) resolve implicit
cycles. *(Verified, [S11][S12])* The Martins & Lambe 2013 survey (AIAA J., 1,033 citations) classifies MDO
architectures as monolithic (MDF, IDF, SAND, AAO) versus distributed and introduced the XDSM diagram; the
classification is from recall and the survey's metadata only was confirmed. *(Verified existence [S14]; content
Flagged)* Martins & Ning's *Engineering Design Optimization* (Cambridge 2022, free PDF/HTML) covers surrogate-based
optimization (ch. 10) and MDO (ch. 13) and is the citable textbook route for the optimizer plug-in. *(Verified,
[S19])*

**Incremental computation as the recompute engine.** `salsa` (Rust, MIT/Apache-2.0) models a system as **inputs** that
may change and **pure tracked functions** memoized with dependency tracking; when an input changes it re-validates
only the dependent queries, uses **early cut-off** (a recomputed intermediate that is equal to its old value stops
propagation), **backdating**, **durability** classes (rarely-changing inputs are verified cheaply) and
**cancellation** of in-flight queries when inputs change. It is inspired by rustc's query system; rust-analyzer is its
best-known user (association from recall). *(Verified, [S40]; user list Flagged)* This is precisely the semantics
ANA-07 asks for: a result is *Current* iff every input it depended on is unchanged, and a changed input marks the
result Historical before any new value appears.

**Dataset-oriented orchestration.** Snakemake (MIT) decides what to re-run from **five rerun triggers — `code`,
`input`, `mtime`, `params`, `software-env` — all on by default**, "which guarantees that results are consistent with
the workflow code and configuration"; pure-timestamp behaviour is an opt-out (`--rerun-trigger mtime`). *(Verified,
[S48])* Dagster (Apache-2.0) frames pipelines as **software-defined assets** with integrated lineage and
materialization state. *(Verified, [S49])* Nextflow and Prefect are both Apache-2.0 to my recollection and add nothing
the two above do not; not opened this session. *(Flagged)* The lesson for the workbench is the trigger set, not the
engines: invalidate on geometry hash, operating point, fluid revision, method id **and method version**, and the
backend build identity.

**Expensive-evaluation queues.** The proposal already frames CFD sweeps as an embarrassingly parallel job queue
*(Verified internal, [S52] §7.3)*. Dakota (LGPL-2.1-or-later, v6.24, 2026-05-15) is the reference design for a driver
talking to a black-box simulation through **fork/system/direct interfaces** with parameter and results files, plus
surrogate-based optimization (Surfpack, PECOS). It is a precedent to copy in shape, not a dependency (LGPL and Python
bindings). *(Verified licence/version, [S20]; interface taxonomy from README and recall)* preCICE (LGPL-3.0, v3.4.1)
is the reference for *co-simulation coupling* (OpenFOAM, CalculiX, SU2 adapters exist in its ecosystem); it is
irrelevant for v1 (no FSI) and licence-excluded from linking. *(Verified, [S21])*

**Content-addressed caching.** The sequence document already keys results by a content hash of the geometry file
*(Verified internal, [S52] §3.2)*. BLAKE3 (default 256-bit, Merkle-tree internal so it parallelizes across
threads/SIMD; CC0-1.0 or Apache-2.0) has an official Rust crate and C implementation; `Blake3.NET` (BSD-2-Clause)
ships a fully managed package and a native package wrapping Rust 1.8.2 for win/linux/macOS x64 and ARM64. *(Verified,
[S41][S42])* SHA-256 from the .NET BCL is a zero-dependency alternative with no licence question; the choice is
performance, not correctness. *(Inferred)*

**The façade pattern ("one interface, four fidelities").** AeroSandbox (MIT) offers `AeroBuildup`,
`VortexLatticeMethod`, `LiftingLine` and wrappers for AVL/XFoil/XFLR5/ASWING/MSES behind a common
airplane/operating-point model, with NeuralFoil as the section-level provider; `asb.Opti` (CasADi-backed) makes the
whole chain differentiable. *(Verified, [S22])* VSPAERO is invoked by the OpenVSP GUI, the command line (`vspaero -omp
4 -stab model_degengeom`) or the API; it consumes DegenGeom `.csv` or Cart3D `.tri` plus a `.vspaero` setup file and
writes `.history` (integrated coefficients per iteration), `.lod` (spanwise loads), `.adb` (viewer), `.stab`, `.fem`.
*(Verified, [S25])* The sequence's `ISolverBackend` façade *(internal, [S52] §9)* is the same shape; the evidence here
says the façade must expose **capability flags** (what fields, what steadiness, what transition model) because the
tiers are not interchangeable, and must carry the **method version** into the cache key.

#### 2. Multi-fidelity orchestration

**What the literature establishes.** Peherstorfer, Willcox & Gunzburger's SIAM Review survey (2018, 989 citations)
organizes multifidelity methods by how the low-fidelity models are managed — **adaptation** (correct/update the
low-fidelity model with high-fidelity information, e.g. trust-region model management), **fusion** (combine outputs,
e.g. co-Kriging, control variates), **filtering** (use the low-fidelity model to decide *when* to call the
high-fidelity one) — and states that accuracy guarantees come from keeping the high-fidelity model in the loop.
Metadata verified; the taxonomy is from recall. *(Verified existence [S15]; taxonomy Flagged)* Alexandrov, Nielsen,
Lewis & Anderson (NASA NTRS 20000097390, 2000) and Alexandrov & Lewis (NTRS 20040086473, 2000) define
**Approximation/Model Management Optimization (AMMO)**: a "rigorous methodology for attaining solutions of
high-fidelity optimization problems with minimal expense in high-fidelity function and derivative evaluation", where
the low-fidelity model is corrected so that its value **and gradient** match the high-fidelity model at the current
iterate (first-order consistency), then used inside a trust region whose radius grows or shrinks on the
actual-vs-predicted improvement ratio. *(Verified, [S18])* Kennedy & O'Hagan (Biometrika 2000, 1,716 citations) and
Forrester, Sóbester & Keane (Proc. R. Soc. A 2007, 1,158 citations) are the co-Kriging references: high fidelity is
modelled as a scaled low-fidelity model plus a Gaussian-process discrepancy, so the prediction carries a posterior
variance. *(Verified existence [S16][S17]; model form Inferred)*

**What this means for a UI.** None of these methods report a blended "best number" to a user; they either (a) correct
the cheap model at specific verified points and *bound* where the correction is trusted, or (b) report a fused
estimate **with its variance**. The spec's rule "no averaging or automatic best truth" (ANA-06) is therefore aligned
with practice; what is missing is the *discrepancy record*: for every pair of runs on the same Surface revision and
Operating point, store `δ = f_hi − f_lo` and `ρ = f_hi / f_lo` per quantity with both run ids. Accumulated
discrepancies are the training data for a future correction model; presenting them today as "Estimator over-predicts
L/D by 12 % at this point per CFD run #…" is a finding, not a prediction. *(Inferred from [S15][S18])*

**Verify-at-higher-tier is a filtering strategy.** COMMIT-01 (no certification on estimator evidence alone) is the
*filtering* pattern: the cheap tier ranks and screens candidates; the expensive tier verifies the survivors. The
literature adds one thing the spec lacks: the cheap tier's ranking is only trustworthy where its error is *monotone*
or *bounded*, so the discrepancy record should be inspected before trusting an estimator-ranked sweep. *(Inferred)*

**Calibration with sparse high-fidelity results.** With one or two CFD points per design, a Gaussian-process
discrepancy is under-determined; the honest v1 is a **per-quantity additive or multiplicative correction with declared
validity** (same Surface revision, same Re decade, same tier pair) and "Model uncertainty not quantified" displayed
unless an actual interval exists (A6). NeuralFoil's own approach — a convergence-classifier confidence penalized by
Mahalanobis distance from the training distribution — is a citable pattern for "how far from verified points am I"
that a correction layer can reuse. *(Inferred from [S1])*

#### 3. ML surrogates for hydrofoil analysis

**NeuralFoil (the one the product already uses).** Training set: XFoil at M∞ = 0, 7,913,292 data points from runs of
which 56 % converged; geometry by merging three random parents from a 2,174-airfoil database with thickness scaling
and Kulfan-parameter perturbation; Re log-normal (median 296k; 2.5th–97.5th percentiles 1.87k–262M); **N_crit uniform
in [0, 18]**; forced transition location uniform in [0, 1] in 20 % of cases; α from a uniform-plus-normal mixture
spanning −27.9° to +28.6°. Test-set (395,665 cases) MAE for the xxxlarge model: C_L 0.012, ln C_D 0.020, C_M 0.002,
x_tr 0.007; on easy NACA cases mean relative drag error 0.37 %. Inputs: 18 CST/Kulfan parameters (8 per side + LE
modification + TE thickness), α, Re, n_crit, x_tr; 8 model sizes; ≈30× faster than XFoil per case and ≈1000× batched.
Licence MIT; requires Python ≥3.10, NumPy and AeroSandbox ≥4.2.4. `analysis_confidence` is trained on whether XFoil
converged for those inputs and its logit has the squared Mahalanobis distance to the training distribution subtracted,
so it tends to zero off-distribution. Stated limits: post-stall moments less accurate; no reversed-flow reattachment
near α = 180°; transonic window not captured (irrelevant in water). *(Verified, [S1][S2])* Consequences for the
workbench: water Ncrit values (the repo uses an Ncrit-for-water policy) lie inside the [0, 18] training range; water
Re of 10⁵–3×10⁶ lies in the dense part of the distribution; **NeuralFoil is a surrogate of XFoil, so it inherits
XFoil's model error and cannot be more valid than XFoil at the same point**; `analysis_confidence` must be surfaced
and stored with every polar row, and a low value must degrade the row's evidence label, never be hidden. *(Inferred
from [S1])*

**Field surrogates (GNN / neural operators / diffusion).** MeshGraphNets (Pfaff et al., ICLR 2021) learns mesh-based
dynamics by message passing and runs 1–2 orders of magnitude faster than its training simulator across aerodynamics,
structures and cloth. *(Verified, [S35])* AirfRANS (Bonnet et al., NeurIPS 2022 D&B) is the reference 2D
incompressible steady RANS airfoil dataset with four tasks — full data, scarce data, Reynolds extrapolation,
angle-of-attack extrapolation — and surface-stress metrics; **licence CC BY-NC-SA 4.0**, which forbids commercial
redistribution in a product. *(Verified, [S36])* NVIDIA PhysicsNeMo (Apache-2.0, PyTorch, CUDA 12/13) bundles FNO,
MeshGraphNet, Transolver, DoMINO, diffusion U-Nets and more. *(Verified, [S37])* The OOD literature is consistent: the
NeurIPS 2024 ML4CFD competition scored OOD generalization and physical compliance explicitly and found no standardized
uncertainty estimation among entrants; 2025–2026 work reports 23× error reduction "under out-of-distribution Reynolds
extrapolation" only with domain-decomposed architectures, boundary GNNs beating volumetric models by 85 % on OOD blade
sections, metamorphic-testing frameworks that separate model violations from out-of-domain use, and **solver-coupled
correction (Newton–Krylov iterations started from the surrogate prediction) as the way to make a surrogate reliable
OOD**. *(Verified, [S34])* Uncertainty: deep ensembles (Lakshminarayanan et al., NeurIPS 2017, 8,587 citations) remain
the default practical UQ for such regressors; existence verified, method from recall. *(Verified existence [S39];
method Flagged)*

**Generative airfoil models.** BézierGAN (Chen & Fuge, 2018) generates smooth curves from a low-dimensional latent
space by emitting rational-Bézier control points, targeting airfoils and hulls, and reports latent-space consistency
favourable for optimization. *(Verified, [S38])* The 2025–2026 stream is diffusion: latent diffusion with automatic
parameterization (DiffGeo, Sept 2026), **AirfoilGen** (May 2026) claims "valid-by-construction" geometry via a
circle-sweeping representation, a study comparing PCA/coordinate/SDF encodings found direct coordinates best and
explicitly discusses extrapolation beyond the training set, OptiWing3D (Dec 2025) pairs 2D and 3D optimized wings and
shows 3D optima diverge from 2D most near the tip, and reward-directed diffusion claims >10 % L/D gains "beyond
training distributions" — a claim that by construction has no independent high-fidelity check inside the paper's loop.
*(Verified listing, [S31]; individual claims read from abstracts only)*

**What a workbench may honestly use these for.** (a) *Fast preview*: NeuralFoil-class section polars with confidence
shown, consistent with the current spec. (b) *Candidate sampling*: a generative model may propose section shapes only
as a **Geometry edit draft** that enters the same validation and Apply path as a hand edit; the shape is Inferred
until a deterministic evaluation runs. (c) *Never certification*: no surrogate output is ever "Verified numerical
implementation" or "Experimentally compared" (A6); COMMIT-01 already forbids estimator-only certification, and a
learned surrogate sits **below** the estimator on the evidence ladder because its error is not derivable from its
inputs. (d) *Training-data hygiene*: AirfRANS is NC-licensed, UIUC data has its own terms (see
`06-foil-section-catalog.md`), so any in-product model must be trained on data whose terms allow it, and the training
manifest is part of the model's provenance. *(Inferred from [S1][S34][S36])*

#### 4. LLM and agent integration in CAD/CFD (2024–2026)

**OpenFOAM agents — what is measured.** Thirteen arXiv papers from MetaOpenFOAM (Aug 2024, 85 % pass on 8 tutorial
tasks, $0.22/case) through OpenFOAMGPT (Jan 2025, RAG over tutorials; authors state "human oversight remains crucial"
and note performance fluctuation over time requiring monitoring), fine-tuned Qwen2.5-7B on 28,716 NL→OpenFOAM pairs
(Apr 2025, 88.7 % on 21 cases), Foam-Agent (v3 Aug 2026: 88.2 % on 110 basic FoamBench tasks, **62.5 % on the
out-of-distribution tier**, six specialist agents exposed through MCP, repairs conditioned on the accumulated error
trajectory), ChatCFD (Feb 2026: 315 cases, **82.1 % execution success vs 68.12 % physical fidelity**; removing the
solver template database collapses accuracy to 48 %; $0.208 and 192k tokens per case), SwarmFoam (Jan 2026, 84 % on 25
cases), PhyNiKCE (Feb 2026, neurosymbolic, fewer self-correction loops), AutoFOAM (May 2026, self-refining fine-tune
on 252 prompts), IteraSim RAG (Jul 2026, 28-case benchmark; diagnosed two corrupted cases from solver logs) to *What
Do CAE Simulation Agents Really Need Beyond a Generic Harness?* (Sept 2026: a plain single-agent harness scored **96.4
% vs 88.2 % for specialized multi-agent systems**; execution-feedback repair moved 71.8 % → 96.4 %; tutorial knowledge
80.9 % → 96.4 %; scripted reflection added nothing). *(Verified, [S26][S27][S28][S29][S30])*

**Reading the numbers honestly.** Every benchmark is tutorial-derived case setup; none evaluates a novel geometry with
a mesh the agent generated from scratch against experiment. The consistent pattern is: models can write syntactically
runnable OpenFOAM dictionaries when given retrieved templates and a compile-run-repair loop; they are markedly worse
off-distribution; and "it ran" overstates "it is physically meaningful" by ~14 points in the only paper that measured
both. The named repair mechanisms — retrieval over solver tutorials, dependency-aware cross-file consistency, minimal
edits conditioned on the error log — are exactly the shape of AI-05 (Diagnose failure → typed case-parameter diff →
user runs it). *(Inferred from [S26]–[S30])* Failure modes named or implied across the set: inconsistent entries
across files (`controlDict`/`fvSchemes`/boundary files), wrong solver for the physics, hallucinated or deprecated
dictionary keys (implied by the need for template databases and error locators), cases that converge to physically
wrong fields, and non-stationary model behaviour over time. *(Inferred; the abstracts do not enumerate a taxonomy —
Flagged as a gap)*

**Text-to-CAD.** Text2CAD (NeurIPS 2024 spotlight) extends DeepCAD to ~170k models / ~660k text annotations and
generates sketch-extrude sequences with a transformer; the 2026 successors (RA-CAD's generate-execute-critique-rewrite
loop, CIT-CAD's constraint-intent verification, HierCAD, ArtisanCAD distilling CATIA procedures, Ortho2CAD reporting
100 % valid code with GPT-5.5 on drawings-to-CAD, foundation-model surveys reporting ~99 % mesh success and IoU
0.885–0.890 on canonical families but "systematic difficulties with rotationally symmetric geometries", ASSEMCAD for
assemblies) measure Chamfer distance, IoU and execution validity. The most important finding for this product: *Wrong
Design Intent Is Worse Than Never Conditioning* (Jul 2026) — on 400 held-out programs, a semantically wrong intent
header **lowers adherence below the unconditioned baseline while executability stays flat**, and even ground-truth
intent reaches only 0.567 on its adherence metric. *(Verified listing and abstracts, [S32][S33])* Commercially, Zoo's
Zookeeper turns prompts into editable models with an API and credits pricing; its capabilities/limitations text was
not readable in the fetched page. Onshape AI Advisor and Autodesk Fusion AI features were not verifiable this session
(404 / not opened). *(Verified partial [S50]; Onshape/Fusion Flagged)*

**Structured outputs, tool use, MCP.** Anthropic structured outputs: `output_config.format` (JSON schema) and `strict:
true` tool use guarantee schema-valid responses by constrained decoding on Opus 4/5, Sonnet 4/5, Haiku 4.5 (and newer
families); supported schema features include types, `enum`, `const`, `anyOf`/`allOf`, internal `$ref`, `required`,
`additionalProperties: false`, string formats; **unsupported: recursive schemas, `minimum`/`maximum`,
`minLength`/`maxLength`, array constraints beyond `minItems` 0/1**; beta headers no longer required. *(Verified
2026-09-20, [S7])* MCP (spec 2026-07-28) defines tools (name, `inputSchema`, optional `outputSchema` that servers MUST
honour and clients SHOULD validate, annotations that clients MUST treat as untrusted), resources, prompts,
elicitation, and a Tasks extension for long-running operations; it states "there SHOULD always be a human in the loop
with the ability to deny tool invocations", requires servers to validate inputs, rate-limit and sanitize outputs, and
asks clients to show tool inputs before calling, time out, and log tool usage for audit. Tool-execution errors are
returned as `isError: true` content for the model to self-correct; protocol errors are JSON-RPC errors. *(Verified,
[S8][S9])*

**Retrieval over the domain knowledge base.** The proposal plans bundled markdown with citations *(internal, [S52]
§8.3)*. SQLite FTS5 (public-domain SQLite core; from recall — not opened) gives BM25 ranking without a dependency;
`sqlite-vec` (MIT/Apache-2.0) adds brute-force KNN over float/int8/binary vectors in `vec0` virtual tables but is
**pre-v1 with breaking changes expected** and lists no .NET binding. *(Verified [S43]; FTS5 Flagged)* Given a corpus
of tens of documents, BM25 over FTS5 is the smallest correct choice; embeddings add a network or model dependency the
offline rule forbids on the critical path. *(Inferred)*

**Guardrails established by the evidence.** (1) Schema validity at the API (structured outputs) **plus** deterministic
range/domain validation in code, because the schema subset cannot express bounds. (2) Numeral check — every numeral in
shown prose must exist in the packed context (the repo's SPIKE-04 rule; no external paper measures this exact control,
so it is a product rule, not established literature). (3) No side effects from model output: proposals are Entities
with acceptance state (A3), and MCP's human-in-the-loop principle is the external statement of the same rule. (4)
Injection isolation: imported files and solver logs are quoted data (AI-04; MCP says annotations and tool results are
untrusted). (5) Execution feedback: for AI-05 the "repair" is a typed diff applied by the user, so the loop the
literature says matters most runs *through the human*, at the cost of fewer automatic iterations — an accepted trade
in a safety-relevant tool. *(Verified [S7][S9]; product rules internal [S51][S52])*

#### 5. Composition of "simple algorithms" — the DRC layer

KiCad's DRC checks connectivity, clearances, widths, courtyards and board-outline integrity; each rule type has a
configurable severity (**error marker, warning marker, or ignored**), individual violations can be **excluded**
without disabling the rule, custom rules are written in a DSL of conditions and constraints with syntax validation,
and the violation list navigates to the offending location. *(Verified, [S46])* Fusion's manufacturing checks were not
opened this session. *(Flagged)*

The workbench's cheap checks are of the same kind and should be one subsystem: a **rule** has an id, a severity
policy, a scope (surface, station, profile, operating point, run), a pure evaluator over the current document, and a
message with the located element; a **violation** carries the rule id, severity, location, measured value versus
threshold and an optional user exclusion with reason and revision. Candidate v1 rules (each from existing repo
knowledge unless noted): TE thickness floor (manufacturing; GAP-07), AR = b²/S_proj with full span (grounding
register), Re per station from chord × speed × ν(fluid revision), cavitation σ vs −Cp_min at the operating point,
symmetry of mirrored channels, monotone/positive chord, station ordering, profile applicability envelope (Re, Ncrit)
versus polar source, class-box limits (GAP-13; rule values are user-supplied until a class rule is admitted), and
manufacturability screens (min thickness, draft — values Flagged until GAP-07 research lands). Rules run on every edit
(they are microseconds), block Apply only at severity *error*, never silently mutate geometry, and their outcomes are
recorded with the Surface revision so a later run's provenance includes the checks that passed. *(Inferred from
[S46][S53])*

#### 6. Reproducibility and audit

**Run manifest.** A PROV-DM mapping for the Analysis run aggregate (A3): the run is an `Activity`; it `used` the
Surface revision, Profile revisions, Water conditions/fluid-property revision, Operating point and Reference
quantities (all `Entity`s identified by content hash); it `wasAssociatedWith` the method (solver id, version, build
digest, settings hash) and the machine; every result and Field evidence `wasGeneratedBy` the run and `wasDerivedFrom`
its inputs; an Assistance proposal `wasAttributedTo` the model identifier and `wasDerivedFrom` the context pack it
saw. *(Verified vocabulary [S10]; mapping Inferred)* OpenMDAO's `SqliteRecorder` records inputs, outputs, residuals,
derivatives, solver iterations and model metadata per case with a `CaseReader` to reload — the working example of
"results are the record" in a design tool. *(Verified, [S13])*

**Immutable results by hash.** The proposal's rule — results in a sidecar keyed by content hash of the geometry file,
derived quantities absent from the file *(internal, [S52] §3.2)* — matches Snakemake's default that parameters, code
and software environment are all rerun triggers. The key must include: canonical serialization of the Surface revision
(not the file bytes — formatting must not change identity), the profile revision hashes, the fluid-property revision
id, the operating point values in SI, the method id **and version/build digest**, and the settings hash. Display
units, file paths and UI state are excluded (A7 Compatibility). *(Inferred from [S48][S52])*

**Event-sourced document history.** The spec demands lossless history and immutable evidence without naming a store
(A3). Append-only revisions with parent links, plus runs referencing revisions by hash, give
comparison-across-revisions for free (ANA-06: geometry, conditions and methods side by side; a numeric delta is
blocked when reference quantities differ). A hash-keyed store also makes deduplication of identical sweep samples
automatic. *(Inferred)*

**How established tools do or fail to do this.** ParaView `.pvsm`/Python state files capture the whole pipeline, views
and camera but reference data by **absolute path**, so reloads offer "use file names from state / search under
directory / choose file names"; screenshots and animations are exported separately with no link back to the state that
produced them. *(Verified, [S47])* VSPAERO writes `.history`, `.lod`, `.adb`, `.stab`, `.fem` next to the DegenGeom
file and the tutorial describes no automated results-manager linkage — provenance is by filename convention.
*(Verified, [S25])* XFLR5 is GPL and stores analyses inside a binary project file with, to my recollection, no
revision identity or invalidation of results after geometry edits; not opened this session. *(Flagged)*

#### 7. Architecture patterns and libraries for the composition layer

| Concern | .NET candidate | Rust candidate | Licence (accessed 2026-09-20) | Confidence |
|---|---|---|---|---|
| Incremental dependency graph | Own small graph over records + `R3`/`System.Reactive` for change propagation | `salsa` | R3 MIT [S44]; System.Reactive MIT (recall); salsa MIT/Apache-2.0 [S40] | R3/salsa Verified; System.Reactive Flagged |
| Graph data structure | Own adjacency lists (few hundred nodes) | `petgraph` | petgraph MIT/Apache-2.0 (recall) | Flagged |
| Job scheduler / queue | TPL Dataflow (`System.Threading.Tasks.Dataflow`, part of dotnet, MIT), `Channel<T>` | `tokio` (MIT) | recall | Flagged |
| Content hashing | `Blake3` / `Blake3.Native` (BSD-2-Clause) or BCL SHA-256 | `blake3` crate | BLAKE3 CC0-1.0/Apache-2.0 [S41]; Blake3.NET BSD-2-Clause [S42] | Verified |
| Process orchestration | `CliWrap` (MIT): stdout/stderr piping, graceful then forceful cancellation, exit-code validation, async/Rx event streams | `tokio::process` | [S45] | Verified |
| MCP hosting (assistant tools) | `ModelContextProtocol` / `.Core` / `.AspNetCore` (Apache-2.0, with Microsoft) | `rmcp` + `rmcp-macros` (Apache-2.0; stdio, streamable HTTP; targets spec 2026-07-28) | [S5][S6] | Verified |
| Anthropic API | `Anthropic` NuGet v12.49.0 (2026-09-18), MIT, official, netstandard2.0/net8/net9; versions ≤3.x were community `tryAGI` | No official repo (`anthropics/anthropic-sdk-rust` 404); use `reqwest` + JSON | [S3][S4] | Verified |
| Local retrieval | SQLite FTS5 (BM25) via `Microsoft.Data.Sqlite` | `rusqlite` + FTS5 | FTS5 public domain (recall); `sqlite-vec` MIT/Apache-2.0 but pre-v1 [S43] | FTS5 Flagged; sqlite-vec Verified |
| Optimizer plug-in (future) | Own interface; reference designs OpenMDAO (Apache-2.0), Dakota (LGPL-2.1+, process-only), AeroSandbox `Opti` (MIT, Python) | — | [S12][S20][S22] | Verified |

Notes: `Blake3.NET`'s managed package removes the native-binary question on both platforms; the native package pins
Rust BLAKE3 1.8.2. The MCP C# SDK page did not show a version or stable/preview marker — check NuGet before pinning.
*(Verified, [S5][S42])*

## Visualization for hydrofoil design and optimization

*Source: [11-visualization.md](11-visualization.md) (`kb-hw-visualization`).*

#### 1. Engineering charts for foil sections and wings

**Cp distribution.** Cp = (p − p∞)/(½ρ∞V∞²); Cp = 1 marks the stagnation point in incompressible flow; the accepted convention is that "graphs of these distributions are drawn so that negative numbers are higher on the graph, as the Cp for the upper surface of the airfoil will usually be farther below zero and will hence be the top line" *(Verified, tertiary [S2])*. XFOIL displays Cp vs x after every ALFA/CL/CLI command, re-displays it with CPX, and in viscous mode draws "the true viscous Cp … as a solid line, and the inviscid Cp at that same alpha … as a dashed line"; the gap between them "is due to the modification of the effective airfoil shape by the boundary layers" *(Verified [S1])*. The workbench chart therefore has four mandatory affordances: inverted axis **named on the axis** ("Cp, more negative upward"); upper/lower curves distinguished by line style *and* a direct label at the trailing edge (not colour alone, ANA-10 and [S48]); optional inviscid overlay drawn dashed as XFOIL does; and Cp_min marked with its x/c because it drives the cavitation screen (XFOIL's `CPMN` reports it [S1]). *(Inferred.)*

**Polar family.** XFOIL's polar page is CD–CL, α–CL, α–Cm and Xtr/c–CL, with reference data read in the same column pairs *(Verified [S1])*. The convention that CL is the ordinate of the drag polar and CD the abscissa, that α is the abscissa of CL–α/Cm–α/L/D–α, and that transition is plotted as x_tr/c against CL (or α) with separate upper/lower curves is therefore XFOIL's own layout, which XFLR5 reproduces (XFLR5 layout from recall, *Flagged*). XFOIL's Ncrit table (sailplane 12–14, motorglider 11–13, clean tunnel 10–12, average tunnel 9, dirty tunnel 4–8) and its definition of Ncrit as "the log of the amplification factor of the most-amplified frequency which triggers transition" are the legend vocabulary a polar must carry *(Verified [S1])*; the water band (≈ 2–4, no measured value) is established in `06-foil-section-catalog.md` and is not repeated here. XFOIL Type 2 polars reinterpret Re as Re√CL for constant-lift trim sweeps *(Verified [S1])* — a foil sweep at fixed rider weight is exactly that case (CL falls as V² rises), so the legend must say which polar type produced a curve. *(Inferred.)*

**Drag polar with Re families.** Each curve carries Re, Ncrit and Mach = 0 (water is incompressible at any foiling speed); families are ordered by Re, distinguished by line pattern and direct labels; the laminar bucket's edges are the feature of interest. XFOIL's docs do not specify a CD axis scale; the practitioner habit of a CD × 10⁴ ("drag counts") axis is *Flagged* (recall) and should not be the default for water-sports users — plain CD with three significant figures. *(Inferred.)*

**Cm.** XFOIL reports the moment coefficient about a reference point (default quarter chord, nose-up positive) — *Flagged* (recall, the doc extract did not include the XYREF passage). The workbench must print the datum on the axis label ("Cm about x/c = 0.25, nose-up +") whichever solver supplies it; a datum-less Cm is unlabelled data. *(Inferred; spec ANA-16 already requires "datum visible".)*

**Cavitation bucket.** The section screen the repo already specifies (ANA-10/14/16) is σ_required(α) = −Cp_min(α) plotted against Cl (or α), with the operating σ = (p_atm + ρgh − p_v)/(½ρV²) as a horizontal line; the "bucket" is the region below the curve where σ_required < σ. The σ tables, water properties and the inception rule are in `04-hydrofoil-disciplines-and-design-data.md` (ITTC 7.5-02-01-03 Rev03) and are not restated. The V_crit-vs-Cl form (proposal §5: "σᵢ vs Cl, and V_crit vs Cl (the Speer chart). Width = α tolerance; depth = speed tolerance" [S53]) is V_crit = √(2(p∞ − p_v)/(ρ·(−Cp_min))). Tom Speer's H105 page could not be opened this session either (TLS failure on tspeer.com, DNS failure on the basiliscus mirror), consistent with files 04 and 06; the "Speer chart" attribution therefore remains *Flagged*, the chart itself is *Verified user intent* in the proposal. Pitfall named in the proposal and confirmed in 06: Cp_min from discrete stations or from cell-averaged CFD **under-reads sharp suction peaks**, so V_crit is optimistic — the chart must state "screening (Cp_min from N samples)" and the spec's "screening limitation visible" clause covers it. *(Inferred.)*

**Spanwise loading.** The standard plot is normalized local loading Cl(y)·c(y)/c̄ against 2y/b, overlaid with the elliptical reference (4/π)·CL·√(1 − (2y/b)²) (derivation in the formulae section), plus Cl(y) alone against the section's Cl_max(Re_local) to show where stall begins — the "first flagged station" ANA-14 requires. Root bending moment is the integral of loading × arm; for an elliptical wing the half-span lift acts at 4/(3π) of the semi-span (≈ 0.424 b/2). Convention: 2y/b abscissa from −1 to +1 (or 0 to 1 with "symmetric" stated), loading ordinate dimensionless, elliptical reference dashed. *(Inferred from standard lifting-line results; no source opened this session — Flagged as recall for the formulas' provenance, the algebra is checked below.)*

**Wing polar vs speed and operating-point overlays.** Because a rider's lift is fixed (weight), the informative wing chart is CL, CD, L/D and Cm against **speed at held load** (ANA-16 requires both α and speed axes) with the take-off point (CL_max reached), the cavitation ceiling (σ = −Cp_min at the operating CL) and the user's brief (10–20 kn wind → boat-speed band) drawn as vertical bands. Every band names its basis; a missing basis is an absent band, not a guessed one. *(Inferred.)*

**Gaps, bands and comparison overlays.** A failed sample is a gap — "no line silently connects across it" (ANA-08) — drawn as a break plus a marker in the gap row of the sample table. Uncertainty bands are drawn only when a quantified interval with a definition and source exists (spec §"Evidence labels"); otherwise the legend reads "Model uncertainty not quantified". Overlays of two sections or two revisions use the same axes and ranges, identify each series by revision, Re, Ncrit and α range (ANA-14), and use line style + direct label, never colour alone. Tufte's small-multiples and data–ink principles motivate one shared scale per comparison and the removal of decorative gradients (Tufte, *Flagged* recall; the spec's UI-token rule already bans decorative gradients). *(Inferred.)*

#### 2. Colour maps and perceptual science

**Criteria.** Crameri, Shephard & Heron list the four properties — perceptually uniform, perceptually ordered (lightness increasing monotonically), CVD-friendly, readable in grayscale — and quantify jet's failure: yellow "is the brightest colour and attracts the eye the most", greenish shades "form a wide band with low perceived colour contrast", such maps "add artificial boundaries to some parts of the data range, hiding small-scale variations elsewhere", and the "visual error can be > 7 % of the displayed data variation" *(Verified [S5])*. Kovesi shows vendor maps have "perceptual flat spots that can hide a feature as large as one tenth of the total data range", that CIELAB is "only designed to be perceptually uniform at very low spatial frequencies", and that uniform incremental lightness change is "the most important factor in designing a colour map"; his sine-wave-on-ramp test image exposes flat spots and false discontinuities *(Verified [S7])*. Borland & Taylor's "Rainbow Color Map (Still) Considered Harmful" (IEEE CG&A 27(2):14–17, 2007) is the canonical statement; the citation is *Verified* via Crossref, the content (no perceptual ordering, obscured detail, artificial boundaries, harm to CVD readers, medical-imaging misreads) is *Flagged* as recall because the PDF host refused the connection [S10]. Rogowitz & Treinish 1998 "the end of the rainbow" precedes it *(citation Verified [S13])*.

**Candidate maps and their licences (2026-09-20).** Crameri's Scientific colour maps v8.0.1 (2023-10-05) are MIT-licensed on Zenodo and include sequential batlow, diverging vik/roma/berlin, cyclic and categorical variants, all "perceptually uniform and ordered … readable both by colour-vision deficient and colour-blind people … even when printed in black & white" *(Verified [S3][S4])*. Cividis (Nuñez, Anderton & Renslow 2018) is viridis re-optimized so that deuteranomalous and normal viewers see nearly the same map, with linear lightness, released CC0 *(Verified [S8])*. Viridis/magma/plasma/inferno are listed by Crameri among the free compliant sets [S5]; their CC0 release by the matplotlib authors is *Flagged* (recall). Moreland's cool–warm diverging map (2009) and his "Fast" default for ParaView are documented on his advice page: a map "should use changes in luminance … to communicate changes in value", but on a 3D surface "shading cues … are vital", so a surface map must "avoid having the brightness changes in the color map interfere with the brightness changes in shading"; Fast "avoids getting too dark at the ends"; full-range maps (black body, inferno, Kindlmann, extended Kindlmann) are for flat images; diverging maps need "a smooth transition in the middle to prevent artifacts at the midpoint" *(Verified [S6]; 2009/2016 citations Verified [S11][S12])*. Turbo (Google 2019) removes jet's banding and is better for most CVD types but keeps jet's low–high–low lightness "at the cost of lightness ambiguity … inappropriate for grayscale printing" and for total colour blindness; its licence is not stated on the post *(Verified [S9])*. ColorBrewer (Harrower & Brewer 2003) remains the reference for **discrete** class schemes *(citation Verified [S14])*.

**Sequential vs diverging vs discrete.** Crameri: diverging "only if the data is divergent about a central value (e.g., centred about zero)" [S5]. Discrete (classed) maps are legitimate when the classes are meaningful (y+ regimes; pass/fail of σ) and they satisfy not-by-colour-alone more easily because each class can carry a label [S48]; a continuous field shown discrete must say the class edges. Lightness monotonicity is what makes a map grayscale-readable and what allows contour lines to be drawn in black on top; a diverging map is by construction non-monotonic (light centre), so on a diverging Cp flood the isolines carry the ordering. *(Inferred from [S5][S6][S7].)*

**Recommendation per field (Inferred, proposed as the TQ3 colormap policy).**

| Field | Physical centre? | Map class | Proposed default | On 3D surface | Notes |
|---|---|---|---|---|---|
| Cp (surface, slice) | Yes: Cp = 0 = freestream static | Diverging, centred at 0, **asymmetric range** (min Cp_min, max +1) | Crameri **vik** or Moreland cool–warm; suction = cool/blue end by the aeronautical habit (Flagged) | yes — cool–warm was designed for surfaces [S6] | Never centre at stagnation; label "Cp (–), 0 = p∞"; draw Cp isolines |
| Speed |V|/V∞ | No | Sequential | **batlow** (MIT) or cividis (CC0) | use a lighter-ended variant or cividis; avoid inferno on shaded surfaces [S6] | slice default; range fixed per series (VIZ-03) |
| Velocity component u/V∞ (wake, tip vortex) | Yes: 0 | Diverging | vik / berlin | — | sign = reversal indicator |
| Modeled k (m²/s²) | No; spans decades | Sequential, optional log scale | batlow with log toggle; label "Modeled turbulent kinetic energy, m²/s², model, mean-flow basis" (VIZ-04) | yes | log scale must be named in the legend |
| C_f magnitude | No | Sequential | batlow | yes | |
| C_f,x or wall-shear component along flow | Yes: 0 = reversal | Diverging | vik | yes | the honest 2D-cut separation indicator (sign change) |
| Vorticity magnitude | No | Sequential | batlow | — | |
| ω_x (streamwise, tip vortex sign) | Yes: 0 | Diverging | berlin (dark centre works on white slices) | — | |
| Q, λ2, λ_ci | Threshold, not colour | Isosurface at user threshold, coloured by |V| or p with a sequential map | — | yes | threshold value in legend; "vortex core, not separation" |
| y+ | Classed | Discrete bands (<1, 1–5, 5–30, 30–300, >300) | ColorBrewer-style 5-class sequential | yes | bands labelled; basis "first cell centre" |
| Pass/fail (σ, Cl_max) | Binary | Two classes + hatch | teal/grey with hatch pattern | — | not colour alone |

Legend requirements (VIZ-01): variable, units (or "–" for dimensionless), range and its basis (fixed for the series / auto-rescaled, labelled), colormap name and version, field association (cell/point), and the sample identity. *(Inferred from [S5][S6][S48] and spec VIZ-01.)*

#### 3. Flow-visualization techniques and what each may claim

The three surveys that frame the field: Post et al. 2003 (feature extraction and tracking), Laramee et al. 2004 (dense and texture-based techniques) and McLoughlin et al. 2010 (integration-based geometric techniques: streamlines, stream ribbons/tubes/surfaces, pathlines, streaklines, timelines, seeding and placement, in 2D, on surfaces and in 3D) *(citations Verified [S29][S35][S30]; content summarized from recall where the abstract was elided, Flagged)*.

**Integral curves.** A streamline solves dx/ds = u(x)/|u(x)| in one velocity snapshot; a pathline solves dx/dt = u(x, t) over recorded time; a streakline is the locus of particles released from one point; a timeline is a line of particles released together. In a steady field all four coincide; in an unsteady field they differ, which is why "steady-field animation is not transient physics" (VIZ-04) — the Kitware path-tracing note the repo already cites makes the same point [S56]. vtkStreamTracer integrates with RK2 (default), RK4 or adaptive RK45, in LENGTH or CELL_LENGTH step units, with initial/min/max step, maximum propagation, maximum step count and a terminal-speed threshold; direction forward, backward or both; termination reasons OUT_OF_DOMAIN, OUT_OF_LENGTH, OUT_OF_STEPS, STAGNATION; outputs IntegrationTime, ReasonForTermination and, when enabled, Vorticity/Rotation/AngularVelocity; seeds come from a point or "from a source dataset's points" *(Verified [S15])*. Design consequence: the workbench stores each streamline's *reason for termination* and shows lines that stopped by OUT_OF_STEPS distinctly, because such a line is a numerical artefact, not a flow feature. *(Inferred.)* RK4 with a step of ¼–½ cell length is the usual production compromise; RK45 with a tolerance is preferable near stagnation and in the tip-vortex core where curvature is high (*Flagged*, practitioner recall).

**Seeding and placement.** Rake (line) seeds upstream of the leading edge at a chosen span station, a plane seed for a volume, or a surface seed for wall-adjacent lines. Jobard & Lefer's algorithm places streamlines so that no two lines approach closer than a separating distance d_sep, seeding candidates at d_sep from existing lines and stopping a line at d_test < d_sep, which yields an even density at any chosen spacing and avoids the clutter/void pattern of regular rakes *(citation Verified [S32]; algorithm from recall, Flagged)*. Illuminated streamlines (Zöckler, Stalling & Hege) apply a line-lighting model with a tangent-based normal so that 3D depth and direction are readable on thousands of lines *(citation Verified [S33]; method from recall, Flagged)*. Both are already in the proposal's §7.1 wish list ("evenly-spaced illuminated streamlines") [S53] and are implementable on a small own renderer: evenly spaced placement is a CPU pre-pass over the 2D slice or the surface parameterization, illumination is a per-vertex tangent plus a fragment shader. *(Inferred.)*

**Stream ribbons and tubes** add a normal (rotation) along the line: ribbons twist with local vorticity about the line, tubes carry a radius mapped to a scalar. Cost is a few tens of vertices per sample; the claim is "local rotation along this line", not "vortex". *(Inferred; McLoughlin survey Flagged.)*

**2D slices.** A station-aligned cut (plane normal along span) or an arbitrary plane; scalars flood the plane; vectors are either **projected into the plane** (in-plane components only, which is what a 2D streamline on the slice integrates) or **drawn as 3D glyphs** intersecting the plane. A streamline traced on a slice from projected velocity is a 2D streamline of a 2D field, not the path of any 3D particle; the legend must say "in-plane projection". The OpenFOAM/ParaView guide's note that vectors are "by default plotted on cell vertices but, very often, we wish to plot data at cell centres" (Cell Centers filter before Glyph) [S21] matters here: cell-to-point interpolation smooths extrema, so Cp_min read from point data is *higher* (less negative) than the cell value — the same optimistic bias the proposal names for station strips. *(Verified reader behaviour [S21]; bias Inferred.)*

**LIC and texture advection.** Cabral & Leedom convolve white noise along streamlines, I(x) = ∫ k(s)·T(σ_x(s)) ds, producing a dense direction image on a 2D domain; Stalling & Hege make it fast and resolution-independent by reusing the convolution along each streamline *(citations Verified [S31][S34]; formula from recall, Flagged)*. Laramee et al. survey the texture-based family for slices and surfaces [S35]. LIC gives direction everywhere but no magnitude or sense (arrow direction) unless colour or animation is added; on a wing slice it is the best *qualitative* attached/separated picture and the worst *quantitative* one. The proposal defers it ("LIC on a 2D cut later") [S53]; agreed. *(Inferred.)*

**Vortex identification.** For ∇u = S + Ω (symmetric strain rate S, antisymmetric rotation Ω): Q = ½(‖Ω‖² − ‖S‖²) > 0 (Hunt, Wray & Moin 1988, *Flagged* citation — NTRS timed out); λ2 < 0 where λ2 is the second eigenvalue of S² + Ω² — Jeong & Hussain propose it as an objective definition that "captures pressure minima perpendicular to the vortex axis at high Reynolds numbers and accurately identifies vortex cores at low Reynolds numbers, outperforming pressure-minimum criteria" and state it outperforms definitions "relying on positive second invariants or complex eigenvalues" *(Verified abstract [S36])*; Δ = (Q/3)³ + (R/2)² > 0 with R = −det ∇u (Chong, Perry & Cantwell 1990, complex eigenvalues of ∇u) *(citation Verified [S38]; formula recall, Flagged)*; swirling strength λ_ci = imaginary part of the complex eigenvalue pair (Zhou, Adrian, Balachandar & Kendall 1999) *(citation Verified [S39])*. All four are **interior** criteria on the velocity gradient; they mark the tip vortex and shed structures, and every one of them is threshold-dependent (the isosurface changes with the chosen level, which must be in the legend). None is a wall criterion.

**Wall separation diagnostics.** Tobak & Peake define three-dimensional separation through the topology of **skin-friction lines** (limiting streamlines) on the surface — separation lines are lines onto which neighbouring skin-friction lines converge, attachment lines are lines from which they diverge, with singular points (nodes, saddles, foci) obeying a topological rule *(citation Verified [S40]; content recall, Flagged)*. Surana, Grunberg & Haller give the exact steady criteria (separation lines as distinguished skin-friction lines with a wall-shear/ pressure-gradient condition) *(citation Verified [S41]; content Flagged)*. Kenwright 1998 and Kenwright, Henze & Levit 1999 extract open and closed separation/attachment lines automatically from the wall-shear field using the phase-plane classification of the shear gradient tensor per triangle *(citations Verified [S42][S43]; method recall, Flagged)*. The 2D-cut analogue is the sign change of the streamwise wall shear (C_f,x < 0 = reversed flow). Practical consequence: **surface streamlines must be integrated from the wall shear stress vector** (τ_w, available from OpenFOAM's `wallShearStress` function object or SU2's surface output), not from the velocity, which is zero on a no-slip wall — a "surface streamline" traced from the first cell's velocity is a near-wall streamline at that cell's height, and must be labelled so. "Oil-flow analogue" is the honest name for a τ_w-line picture. *(Inferred; consistent with the grounding register's rule and VIZ-02.)*

**Surface scalars, contours, probes.** Surface Cp flood + Cp isolines at fixed intervals (0.1) make the not-by-colour-alone floor; C_f flood; y+ classed bands. Probe = value at a picked point with cell id and interpolation basis; plot-over-line = sampled values along a segment (a ParaView filter; *Flagged* — not opened). A station probe line at the selected section reconstructs a "CFD Cp at station" curve to overlay on the 2D XFOIL-class Cp — the overlay must name both bases (3D CFD surface sample vs 2D section solution; VIZ-01's "Strip Cp is labeled Section reconstruction"). *(Inferred.)*

**Free surface.** For a VOF run the interface is the α = 0.5 isosurface (contour of the phase fraction) and the wave pattern is that surface's elevation; a single-phase deep-water run has no free surface and the view must say "deep-water assumption (h/c ≥ 5)" as file 04 requires. *(Inferred; VOF α = 0.5 convention is standard, Flagged for provenance.)*

**Volume rendering** of k or |V| is rarely informative for an external wing flow (thin boundary layers, one wake, one tip vortex per tip); slices and isosurfaces are the honest and cheap representations. *(Inferred.)*

**Sweep replay vs time animation.** ParaView distinguishes "Snap To TimeSteps", where "the number of frames in the animation is determined by the number of time values in the dataset", from "Sequence", where frames are generated and "Times are evenly spaced between Start and End time", and it *interpolates* keyframed properties ("varying by linear interpolation between them") *(Verified [S19])*. The workbench's parametric replay is a Sequence over *cases*, not time, and must not interpolate fields between cases (spec §"Parametric sweep replay", VIZ-03); a physical-time timeline appears only for a transient run with recorded timesteps (VIZ-04). *(Verified spec + Inferred mapping.)*

**Technique → data → honest claim → cost.**

| Technique | Data required | May honestly claim | Cannot claim | Cost (per frame, reduced data) |
|---|---|---|---|---|
| Surface Cp / C_f / y+ flood + isolines | Wall patch triangles + scalar (cell or point) | Distribution of that scalar on the solved surface at that sample | Separation, transition (unless the solver exposes them) | 10⁵–10⁶ triangles; trivial GPU |
| Section probe line (CFD Cp at station) | Surface intersection with a plane | Sampled 3D surface Cp along that cut | Equivalence with 2D section Cp | O(√N) segments |
| 2D slice flood | Plane cut of volume (cells + scalars) | Scalar in that plane | Anything off-plane | 10⁴–10⁵ polygons; slice precomputed |
| Slice vectors, in-plane | Slice + projected velocity | In-plane direction and magnitude | 3D particle paths | Glyph count ≤ 10⁴ |
| Slice 2D streamlines | Projected velocity on slice | Topology of the *projected* field | Off-plane transport | CPU pre-pass; ~10²–10³ lines |
| 3D streamlines (rake/plane seed) | Volume velocity (or reduced sub-volume) | Instantaneous integral curves of the mean field | Transient motion, turbulence, separation | Precomputed polylines: 200 lines × 500 pts ≈ 2.4 MB |
| Evenly spaced placement | as above + spacing parameter | Uniform coverage at d_sep | Physical density of anything | CPU pre-pass |
| Illuminated lines / ribbons / tubes | Streamlines + tangents (+ vorticity for ribbons) | Direction and local rotation with depth cues | Vortex identity | GPU: vertex tangents + shader |
| Pathlines / streaklines / timelines | **Time-varying** velocity, recorded timesteps | Particle motion over recorded time | Anything for a steady run (capability gate) | Integration across timesteps; needs all frames |
| LIC on slice | Projected velocity on slice | Dense direction picture | Magnitude, sense, quantitative values | Texture pass per slice; deferred |
| Q / λ2 / Δ / λ_ci isosurface | Volume ∇u (or precomputed scalar) + threshold | Vortex-core candidates at that threshold | Separation; threshold-independence | Marching cubes per sample; precompute |
| Skin-friction lines / separation-attachment lines | Wall shear stress vector on wall patch | Surface-flow topology; separation/attachment lines by a named criterion | Anything without τ_w | 2D integration on the surface mesh |
| Free surface α = 0.5 | VOF phase fraction | Interface location | Wave breaking, spray | Contour per sample |
| Probe / plot-over-line | Any field + geometry | Sampled values with basis | Values between samples | negligible |
| Volume rendering | Full volume | Qualitative 3D distribution | Quantitative reading | Needs the volume → ParaView hand-off |
| Parametric replay | One reduced artifact set per sample | "Sample i of N at (V, α)" | Time evolution; anything between samples | Disk cache; LRU RAM |
| Physical-time animation | Recorded timesteps | Evolution over recorded seconds, instantaneous/averaged basis | Resolved turbulence unless the run resolves it | Frame set per timestep |

*(Table Inferred from [S15][S19][S21][S29][S30][S35][S36][S40] and the spec.)*

#### 4. Tools and libraries

| Tool / library | What it is | Licence (SPDX) | Platforms / bindings | Status 2026-09-20 | Fit for the workbench |
|---|---|---|---|---|---|
| VTK | C++ visualization toolkit; Python, JavaScript, C#/.NET (ActiViz) wrappers listed | BSD-3-Clause (Copyright.txt) | C++, Python; JS via VTK.js; .NET only via ActiViz | active (pushed 2026-09-20) | Reduction sidecar and file formats; not linked into .NET |
| ParaView | VTK-based application; paraview GUI, pvpython (interactive scripts), pvbatch (batch, MPI), pvserver/pvdataserver/pvrenderserver | BSD-3-Clause; SPDX files shipped since 5.12.0 | Windows/macOS/Linux; Qt GUI | active | "Open in ParaView" hand-off; pvbatch reduction scripts; screenshots PNG/JPEG/TIFF/BMP/PPM; animations AVI (Win/Linux), MP4 (Win only), Ogg, image sequences; scene export Cinema, EPS, PDF, PS, SVG, POV, VRML, WebGL, X3D(B) [S20] — glTF export not confirmed on the page opened (Flagged) |
| ParaView OpenFOAM reader | Built-in reader; OpenFOAM's own PVFoamReader/vtkPVFoam via `paraFoam`; cell vs point data; time/region/field selection | BSD-3 (ParaView) / GPL-3 (OpenFOAM module — process only) | as ParaView | v12 guide names ParaView 5.10.1 for the module | Hand-off target; the empty `<case>.foam` convention is Flagged (recall) |
| foamToVTK | OpenFOAM utility writing legacy VTK; `-fields "(p U)"`, `-noInternal` (patches only), `-cellSet/-faceSet/-pointSet` subsets, `-surfaceFields`, `-nearCellValue`, `-allPatches`, ascii/binary | GPL-3.0 (process invocation only) | wherever OpenFOAM runs | active | Reduction path for OpenFOAM runs: patches and subsets without the volume |
| SU2 outputs | `OUTPUT_FILES` ∈ {TECPLOT(_ASCII), SURFACE_TECPLOT(_ASCII), CSV, SURFACE_CSV, PARAVIEW(_ASCII/_LEGACY), SURFACE_PARAVIEW(_ASCII/_LEGACY), RESTART(_ASCII), CGNS, SURFACE_CGNS, STL_ASCII/BINARY}; default (RESTART, PARAVIEW, SURFACE_PARAVIEW) | LGPL-2.1 (process invocation only) | Win/mac/Linux | active | `SURFACE_PARAVIEW` is the reduced surface artifact for free; volume `.vtu` stays on disk |
| VTK.js | VTK for the web | BSD-3-Clause | browser/WebGL/WebGPU | active | Only if a WebView is adopted (see web tech) |
| trame | Kitware web-app framework over VTK/ParaView | Apache-2.0 | Python server + browser | active | Headless/remote rendering to a browser; not a native control |
| PyVista (+ pyvistaqt) | Pythonic VTK | MIT | Python | active | Development/test sidecar for reduction and reference images |
| VisIt | LLNL visualization app | BSD-3-Clause | Win/mac/Linux | active | Alternative hand-off; not needed beside ParaView |
| ActiViz | Kitware's commercial VTK .NET wrapper | Commercial (per-developer, version-locked; binary redistribution without end-user licences); free trial | Win x64, Linux x64, macOS ARM64, WASM | maintained by Kitware Europe | Excluded by COMMIT-02 (not permissive OSS); an old community `Activiz.NET.x64` 5.8.0 exists on NuGet (Flagged provenance) |
| Tecplot 360 / FieldView / EnSight | Commercial CFD post-processors | proprietary | desktop | — | Comparables for chart/field conventions only (Flagged: none opened) |
| vtkio | Rust VTK legacy + XML reader/writer | Apache-2.0 on GitHub metadata; crate is MIT OR Apache-2.0 (Flagged) | Rust | 0.6.3, pushed 2026-03-21 | Rust-side reader for reduced `.vtp/.vtu` |
| Silk.NET | .NET bindings for OpenGL/Vulkan/GLFW/SDL/… | MIT | .NET, Win/mac/Linux | 2.23.0, pushed 2026-09-08 | GL/Vulkan calls inside an Avalonia GL control |
| Veldrid | .NET portable graphics abstraction | MIT | .NET | **unmaintained**: README "As of February 2023, I'm no longer able to publicly share updates"; last release 4.9.0 (2023-02-03) | Avoid |
| Avalonia | XAML UI framework | MIT | Win/mac/Linux/…; `Avalonia.OpenGL/Controls/OpenGlControlBase.cs`, `Avalonia.Vulkan` project present in source | 12.1.2 on NuGet | The viewport host; macOS backend details (Metal/ANGLE) Flagged, not opened |
| wgpu / wgpu-native | Rust WebGPU implementation; C API | MIT OR Apache-2.0 (both LICENSE files) | Rust; C ABI for other languages | wgpu 30.0.1; wgpu-native pushed 2026-09-20 | Rust viewport door; wgpu-native is the C-ABI bridge if a Rust renderer is hosted from .NET (no NuGet package exists — Flagged) |
| rend3 | Rust renderer | Apache-2.0 | Rust | **archived**, "MAINTENANCE MODE" | Avoid |
| bevy | Rust game engine | Apache-2.0 (dual MIT — Flagged) | Rust | active | Too heavy for a viewport-in-a-control; comparable only |
| three-d, kiss3d | Rust small renderers | (not checked) | Rust | three-d 0.19 (2026-04), kiss3d 0.46 (2026-08) | Candidates if a Rust viewport is chosen; licences Flagged |
| ScottPlot 5 | .NET plotting | MIT | `ScottPlot.Avalonia`, `.OpenGL`, WPF, WinForms, Blazor, Maui, Eto, WinUI | 5.1.59, pushed 2026-08-15 | **Charts v1 default (Inferred)** |
| LiveCharts2 | .NET charts (SkiaSharp) | MIT | `LiveChartsCore.SkiaSharp.Avalonia` | 2.1.0-dev-798 (prerelease), pushed 2026-07-20 | Alternative; prerelease line |
| OxyPlot | .NET plotting | MIT | `OxyPlot.Avalonia` 2.1.0 | core pushed 2026-07-30; Avalonia package pushed 2024-08-11 | Stale Avalonia binding |
| SkiaSharp | .NET Skia | MIT | all | 4.154 preview on NuGet | Raster layer under ScottPlot/LiveCharts; own chart rendering fallback |
| plotters | Rust plotting | MIT | Rust, WASM | 0.3.7, pushed 2026-04-13 | Rust chart option |
| egui_plot | Rust immediate-mode plots | MIT OR Apache-2.0 | Rust | 0.37.0, pushed 2026-09-18 | Rust chart option |
| Plotly.js / D3 / three.js | Web charting/graphics | MIT / ISC / MIT | browser | active | Would require a WebView/browser runtime in a native app; conflicts with offline-first and native accessibility proof (Inferred); not recommended for v1 |
| XFLR5 | XFOIL-based foil/wing analysis GUI | GPL-3.0 | Win/mac/Linux | 6.62 (2026-06-30), SourceForge flags "Abandoned" | Conventions only; never linked |
| AeroSandbox / NeuralFoil | Python aircraft design / airfoil NN | MIT / MIT | Python | pushed 2026-09-15 / 2026-07-19 | Already the 2D sidecar; their plotting conventions not opened this session (Flagged) |

*(Licences and dates Verified via GitHub, NuGet, crates.io, Kitware and OpenFOAM/SU2 sources [S24][S25][S26][S27][S28][S16][S17][S22][S23]; "fit" column Inferred.)*

**The realistic .NET paths, ranked (Inferred).** (a) *Process + files*: the solver already writes VTK (`SURFACE_PARAVIEW`, `foamToVTK -noInternal`), a pvbatch script reduces volumes to slices/streamlines/isosurfaces as `.vtp`, and the app reads `.vtp/.vtu` with a small own reader (XML with appended raw/base64 blocks; legacy ASCII/binary) — file 05 already judges an own writer "small"; a reader for the subset the app emits itself is the same size. (b) *Own renderer*: an Avalonia `OpenGlControlBase` (or Vulkan) surface fed by Silk.NET draws triangle soups with per-vertex scalars, polylines with tangents (illuminated), glyphs and a colour-bar; picking by id-buffer. This is a few thousand lines, not a VTK. (c) *Rust viewport* via wgpu (+ vtkio, egui_plot) if picking or performance demands, hosted through wgpu-native's C ABI or as a separate window; no maintained NuGet package for wgpu-native was found (Flagged). (d) *ActiViz* is the only in-process VTK and is commercial — excluded. (e) *ParaView external* remains the full-volume path and costs nothing to ship (an `Open in ParaView` action on the case directory plus an optional `.pvsm` state file that reproduces the app's camera, colormap and range — Flagged: state-file compatibility across ParaView versions is not guaranteed and must be spiked).

#### 5. Performance and data budgets

**Typical sizes.** A wing RANS case in the 1–20 M-cell range (the proposal's own example is "43 M-cell" [S53]) is the input; the numbers below are arithmetic on VTK's storage model, not measurements, and are therefore *Inferred* (an instrumented spike must replace them before any budget is fixed — IO rule). For an unstructured grid with N cells and ≈ N points (hex-dominant):

- points: 3 × 8 B × N = 24 N B (double) or 12 N B (float);
- hex connectivity: 8 × 8 B × N (64-bit ids) + offsets 8 N B = 72 N B;
- each scalar (double): 8 N B; each vector: 24 N B.

For N = 10⁷: points 240 MB, connectivity 720 MB, U 240 MB, p + k + ω 240 MB → **≈ 1.44 GB** for one sample in double precision, ≈ 0.9 GB in float32 with 32-bit ids; a 12-sample sweep is 11–17 GB — more than the machine. Even the solver's own surface output is small by comparison: a wing wall patch of 3 × 10⁵ faces with Cp, C_f, y+ and τ_w is ≈ 3 × 10⁵ × (12 + 12 + 4 + 4 + 4 + 12) B ≈ 14 MB; a station slice of 10⁵ cells with p, U, k ≈ 5 MB; 200 streamlines × 500 points × 24 B ≈ 2.4 MB. A **reduced artifact set per sample of ≈ 20–40 MB** is the budget target; a 60-sample sweep is then ≈ 1.2–2.4 GB on disk and a handful of samples in RAM. *(Inferred arithmetic.)*

**Memory budget on a 16 GB laptop (Inferred, to be measured).** OS + browser-class background ≈ 4 GB; app shell + document + charts ≤ 1 GB; viewport GPU buffers ≤ 0.5 GB; reduced-artifact cache ≤ 2 GB (LRU, current ± 2 replay frames pinned); everything else stays on disk. The rule that follows: **the UI process never opens a volume file**; the sidecar does, and it runs as a separate process with its own memory ceiling so that an out-of-memory kill is a reported failure of one sample, not of the app (fail-closed, consistent with the sweep's "each sample is independently recoverable").

**Reduction pipeline (Inferred).** Per admitted sample: (1) surface patches with Cp, C_f, y+, τ_w (`foamToVTK -noInternal -fields`, or SU2 `SURFACE_PARAVIEW`); (2) the station slices the user has defined plus a default mid-span slice, with p, U (and projected U), k; (3) streamlines from the default rake and from any user rake, with tangents and ReasonForTermination; (4) optional Q/λ2 isosurface at a stored threshold; (5) a JSON manifest: run id, sample pair (V, α), fields present/absent, ranges, field association, colormap policy, hash of each artifact. Level of detail: `vtkQuadricDecimation` (Garland–Heckbert quadric error metrics, `TargetReduction` as a fraction, attribute-aware error optional, topology not guaranteed) can produce a coarse surface for interaction while the full patch renders on idle *(Verified filter semantics [S49])*; the coarse mesh is never the one probed for values. Streamlines are drawn as one instanced polyline buffer (GPU instancing of a quad strip along tangents) so that thousands of lines are one draw call (*Flagged*, standard practice, no source opened). Precomputing replay frames is what makes VIZ-03's "camera, slice, seeds and scalar range stay fixed" cheap: with fixed seeds and slices the artifacts are computed once per sample, and a changed rake invalidates only artifact (3) for every sample.

#### 6. Accessibility and honesty

- **Not by colour alone.** WCAG 2.2 SC 1.4.1 (Level A): colour "is not used as the only visual means of conveying information, indicating an action, prompting a response, or distinguishing a visual element" *(Verified [S48])*. For a scalar flood: isolines at labelled intervals, a probe readout, a colour bar with tick values, and a table equivalent (min/max/mean/location per patch or slice); for charts: line pattern + direct labels + a data table that is "an alternate readable representation of the same evidence" (spec B7). The 8 %/0.5 % CVD prevalence [S5] is the population that makes this a floor, not a preference.
- **Screen-reader summary.** Each view exposes a text summary: "Surface Cp, sample 7 of 12 (V = 8.2 m/s, α = 3.0°), run r-0a1f, range −1.42 … 0.98 fixed for series, Cp_min −1.42 at x/c = 0.11, station 0.37 b/2; cell data; steady RANS k-ω SST; colormap vik". *(Inferred from [S48] and VIZ-01.)*
- **Provenance strip on every view** (the field-evidence aggregate): source run + sample identity, variable + units, range + basis (fixed/auto, labelled), field association (cell/point + interpolation), physical basis (steady / instantaneous / averaged; modeled k), method tier, colormap name/version, and the evidence label. *(Inferred; spec §"Evidence labels".)*
- **Illustrative vs Computed.** Anything not produced by a run on the current geometry/condition — mockup art, a cached picture from a superseded revision, a section-reconstructed "Cp on the loft" — is *Illustrative* or *Section reconstruction*, never a 3D field; the spec's four-level ladder (Illustrative mockup / Computed estimate / Verified numerical implementation / Experimentally compared) is the vocabulary. *(Verified spec; mapping Inferred.)*
- **Uncertainty.** Bonneau et al. survey the field *(citation Verified [S45])*; HOPs beat error bars and violins for ordering judgements *(Verified [S44])* but need samples. Rule: draw a band only with a defined interval and source; draw HOPs only over an actual ensemble (e.g., a Ncrit 2–4 family, a mesh-refinement pair); otherwise "Model uncertainty not quantified". *(Inferred; spec.)*
- **Failure modes (silent, expensive):** (1) a pretty picture read as diagnosis — the Q isosurface on the tip "shows separation" (it does not); (2) auto-rescaled ranges across replay frames make identical fields look different and different fields look identical — default fixed-series range with a labelled toggle (spec); (3) interpolated/morphed frames between solved cases imply unsolved physics — forbidden (VIZ-03); (4) Cp sign or axis direction flipped between the chart and the flood — one Cp definition, axis named; (5) rainbow/turbo default — banned; (6) masked/missing data filled by the colormap's minimum — missing must be masked with a pattern and stated (VIZ-01); (7) cell→point interpolation under-reading Cp_min — state the association; (8) a "surface streamline" traced from near-wall velocity — label the height or use τ_w; (9) seeds that stop by step count read as stagnation — draw termination reason; (10) a fixed range from an old series applied to a new fluid/speed — range basis is part of the sample manifest and re-derived when the comparable series changes. *(Inferred.)*

#### 7. Optimization visualization

Optimization is a later step in the proposal (goal-driven search after Steps 1–6 [S53]); the visual vocabulary should nonetheless be fixed now because it reuses the chart set. Munzner's framework (what–why–how: data abstraction, task, encoding) is the design reference for these choices *(citation Verified [S46]; content recall, Flagged)*.

- **Pareto fronts.** Two objectives → scatter with the non-dominated set connected and dominated candidates greyed; hover/select reveals the candidate's provenance card. Three objectives → a scatter-plot matrix of the three 2D projections plus a parallel-coordinates view (axes = objectives + active constraints + key design variables; one polyline per candidate; brushing on an axis filters) — parallel coordinates (Inselberg) is the standard for > 3 dimensions (*Flagged*, recall). Each point carries the method tier that evaluated it (estimator / 2D / VLM / CFD): mixed-fidelity fronts are a defect signature unless the tier is encoded by marker shape *and* label.
- **Convergence histories.** Objective and constraint violation vs iteration/evaluation count, log scale for residual-like quantities, with the *accepted* iterates marked; for CFD-in-the-loop show wall time per evaluation as a second axis (instrumentation over inference).
- **Sensitivity (tornado).** One-at-a-time ± Δ on each design variable, bars sorted by |effect|, sign coloured *and* labelled, with the Δ and the tier stated; a tornado from an adjoint gradient (SU2 supports discrete adjoints — *Flagged*, not opened) is labelled "gradient" not "finite difference".
- **Constraint activity.** Normalized g_i / g_i,limit per candidate as a bar strip with the active set (≥ 0.95) highlighted by hatch + label; cavitation (σ margin), Cl_max margin, root bending moment, thickness/structural minimum, class rules (file 04's gap) are the expected rows.
- **Candidate vs baseline overlays.** Planform ghost (baseline outline dashed under the candidate; the proposal names "overlay a second document (ghost)" as the actual task [S53]), section overlay at a chosen station (both profiles, normalized chord, deviation readout as the Smooth preview already does), polar overlay (same axes/range, series identified by revision, Re, Ncrit), spanwise-loading overlay (both against the elliptical reference).
- **Provenance per candidate.** Geometry hash, recipe/parent revision, evaluator (method, version, Ncrit, Re basis, mesh/case pin for CFD), objective values with units, constraint values, wall time and cost — the same manifest the field-evidence aggregate uses, so "why is this point here?" is answerable from the chart.
*(All Inferred; no optimization-visualization source beyond Munzner was opened this session.)*

#### 8. Chart specification table (the set the spec must name)

Each row is one chart the spec names; "gap" means a failed/unsupported sample is drawn as a break plus a marker and a row in the table twin (ANA-08). Every chart has a table twin and a direct-labelled legend carrying the series identity (revision, section, Re, Ncrit, polar type, α or V range, water record, datum). *(Inferred from [S1][S2] and spec ANA-08/10/14/16; conventions Verified where marked.)*

| Chart | Abscissa | Ordinate | Convention | Legend must carry | Overlay | Pitfall guarded |
|---|---|---|---|---|---|---|
| Cp distribution | x/c, 0 → 1 (LE → TE) | Cp, **more negative up** (Verified [S2]) | upper/lower by line style + TE label; viscous solid, inviscid dashed (Verified [S1]); Cp_min marker with x/c | α, Re, Ncrit, viscous/inviscid, Cp definition | second section/revision at same α, Re | axis un-named; sign flip vs flood |
| Cl–α | α [deg] | Cl | linear region, Cl_max marker | Re, Ncrit, polar type | Re family | Cl_max from a non-converged point |
| Cd–α | α [deg] | Cd | plain Cd, 3 s.f. | Re, Ncrit | Re family | drag-count axis unexplained |
| Cm–α | α [deg] | Cm | datum named (x/c = 0.25 default, Flagged), nose-up + | datum, Re, Ncrit | — | datum omitted |
| L/D–α | α [deg] | Cl/Cd | peak marker | Re, Ncrit | — | dividing by a gap |
| Drag polar | Cd | Cl | Cl ordinate (Verified [S1]); bucket edges visible | Re, Ncrit, polar type (1/2) | Re family, second section | mixed polar types unlabelled |
| Transition | α (or Cl) | x_tr/c upper & lower | two curves by style + label | Re, Ncrit | — | absent x_tr drawn as 0 |
| Cavitation bucket | Cl (or α) | σ_required = −Cp_min | operating σ as horizontal line with V, h, water; bucket region hatched | Cp_min basis (N samples), water record, depth | second section | interpolated Cp_min peak |
| V_crit–Cl | Cl | V_crit [kn, m/s] | derived from σ_required; deep-water label | same as above | second section | claiming a measurement |
| Spanwise loading | 2y/b, −1 → 1 | Cl·c/c̄ and Cl(y) | elliptical reference dashed; Cl_max(Re_local) line | method tier (VLM/strip/CFD), α, V | baseline ghost | projected span not used |
| Root bending moment | — (readout) or 2y/b | M [N·m] | one number with datum; arm on projected span | tier, load, V | baseline | dihedral arm mistake |
| Wing polar vs α | α [deg] | CL, CD, L/D, Cm | held V, load, water, datum visible | tier, S_ref, b, datum | revision | S_ref mismatch |
| Wing polar vs speed | V [kn/m/s] | CL, CD, L/D, Cm | held load (rider weight) ⇒ CL ∝ V⁻²; take-off, cavitation, brief bands with basis | tier, S_ref, load, water | revision | bands drawn without basis |
| Residual history (CFD) | iteration | log residual per equation | log axis named; "decline ≠ convergence" | solver, case pin | — | reading residual as completion |
| Force/coefficient history (CFD) | iteration | CL, CD | convergence window marked | solver, S_ref | — | last iterate ≠ converged value |
| Sweep matrix | V | α | cell = status (colour + glyph + text) | schedule id | — | colour-only status |

#### 9. Provenance strip schema (the field-evidence aggregate's display contract)

Fields, in display order, with the rule that an absent value is rendered as *Unavailable* and never defaulted (VIZ-01): `run_id` · `sample (V, α, index/N)` · `variable` · `unit` (or "–") · `range` + `range_basis ∈ {fixed-series, auto (labelled), user}` · `association ∈ {cell, point (interpolated)}` · `physical_basis ∈ {steady RANS mean, instantaneous t = s, time-averaged window}` · `model` (turbulence model, wall treatment) · `method_tier` · `colormap (name, version, sequential/diverging, centre)` · `evidence_label ∈ {Illustrative mockup, Section reconstruction, Computed estimate, Verified numerical implementation, Experimentally compared}` · `artifact_hash`. The same record is the screen-reader summary source. *(Inferred; spec VIZ-01/§"Evidence labels".)*

#### 10. Reduction sidecar contract (process boundary)

Input: case directory + sample id + a reduction request (patches, slice planes, rakes, isosurface thresholds, fields). Output: `.vtp` per artifact + one JSON manifest (fields present/absent with reasons, ranges, association, tool + version, wall time, peak RSS). Behaviour: runs as a separate process with a memory ceiling; a crash or OOM marks that sample's artifacts *Unavailable (reduction failed: reason)* and never the whole sweep; identical requests are content-addressed (hash of case pin + request) so replay frames are computed once. Tools by backend: OpenFOAM → `foamToVTK -noInternal -fields "(p U k wallShearStress yPlus)"` for patches, pvbatch for slices/streamlines/isosurfaces; SU2 → `SURFACE_PARAVIEW` for patches, pvbatch on `flow.vtu` for the rest. Function-object availability for `wallShearStress`/`yPlus` is Flagged (recall) and gates the separation layer. *(Inferred from [S21][S22][S23][S18].)*

#### 11. "Open in ParaView" hand-off contract

Detect ParaView (path, version); launch it on the case directory (OpenFOAM: the case's `.foam` stub — Flagged convention; SU2: the `.vtu` files); optionally pass a state file reproducing camera, colormap (same LUT, same range basis), slice planes and rakes; never block the app on the child process; record the launch in the audit log with the sample identity. If ParaView is absent the action explains the gate ("ParaView 5.12+ not detected; install from paraview.org (BSD-3)"). Version drift of the state file is a Flagged risk to spike. *(Inferred from [S17][S18][S20][S21].)*

## Structures, materials, manufacturing and safety for water-sports hydrofoils

*Source: [12-structures-materials-and-manufacturing.md](12-structures-materials-and-manufacturing.md) (`kb-hw-structures-materials-and-manufacturing`).*

#### Static hydroelasticity with low-order models (the tier this tool can reach)

The current reference pairing is a **composite beam finite element** along the elastic axis with a **lifting line or VLM** supplying the hydrodynamic stiffness. Ng et al. 2024 (IMDC) write the static problem as **K_s u = f_hydro(u) = −K_f u** and solve by Newton–Raphson; their beam element has two nodes and nine DOF per node "to account for structural warping", was "previously developed and validated against a composite plate model in ABAQUS", and the lifting line follows Glauert's Fourier-sine method with the strut junction zeroing the vorticity at the centreline collocation node. Center of lift is placed at c/4 per section, with the stated caveat that this "does lose validity at higher angles of attack because of flow separation that causes the center of pressure to migrate towards the midchord". The elastic axis is assumed at midchord [S1]. *(Verified)*

The same paper's case is directly usable as a **benchmark fixture**: Moth rudder T-foil, root chord 0.14 m, tip chord 0.095 m, half-wing planform area 0.039 m², 19 beam elements per half-wing and 9 for the strut, UD CFRP with ρ 1590 kg/m³, E1 117.8 GPa, E2 = E3 13.4 GPa, G12 = G23 3.9 GPa, ν12 = ν13 0.25, U = 18 m/s, seawater 1025 kg/m³, base rake 2°, fibre angles 0° and ±15°. Results (no free-surface model): L = 5738.7 N / 2469 N / 1787 N and CL = 0.442 / 0.190 / 0.138 for θ_f = −15° / 0° / +15°; midchord moments 165.1 / 73.2 / 54.0 N·m; the infinite-Froude free-surface model reduces these by 3.7 % / 1.0 % / 0.6 %. The −15° foil deflects "about 8 % of semispan" with a tip twist near 9° [S1]. *(Verified)*

The **1D-beam adequacy** question — can a beam replace shells and solids for a real laminated foil — was answered affirmatively by Faye et al.: a NACA 0015 composite foil with off-axis fibres, boundary-element flow solver, partitioned coupling, water-channel measurements at several angles, speeds and submergences; the 1D beam model "gives results very similar" to 2D-shell/3D-solid FE and both are "well predicted … but it requires a precise modeling of the bend-twist coupling" [S6]. The 2025 follow-up validates the equivalent-beam approach on four foils from the same mould with different layups and "quantif[ies]" the computational savings [S7]. *(Verified)*

**Disconfirming evidence sought.** Do beams miss something a foil designer needs? Yes, three things the sources themselves name: (i) center-of-pressure migration at high α [S1]; (ii) local skin buckling, ply-drop stress concentrations and junction stresses, which are 2D/3D phenomena — Ng et al. note real foils have "several plies of different angles … to avoid crack propagation" and treat their single-angle UD as an anisotropy exploration, not a design [S1]; (iii) impact and cavitation damage, which are not beam quantities [S26][S1]. The beam tier is therefore a **sizing and coupling** tool, never a sign-off. *(Inferred from [S1][S6][S26])*

#### Bend-twist tailoring and its sign convention

Young et al. 2018 built three CFRP foils and one stainless foil of identical unloaded geometry, "with the primary difference being the orientation of the structural carbon layers relative to the spanwise axis", tested them cantilevered in a cavitation tunnel and complemented the tests with a two-DOF FSI model. Nose-up coupling raises load coefficients and brings "accelerated stall and static divergence"; nose-down coupling does the opposite; and the non-dimensional loads collapse against **effective incidence = geometric incidence + generalised tip twist** [S2]. Ng et al. reach the same conclusion by computation: negative fibre angles (toward the TE) "produce more outboard lift and moment … nose-up tip twist … bad for both hydrodynamics and structures because of increased cavitation, ventilation, and tip stall susceptibility as well as material failure risk"; zero or positive angles "are better for load alleviation … washout via nose-down bend-twist coupling" and gave better drag polars from lower induced drag [S1]. Giovannetti, Banks, Ledri & Turnock (Southampton) built the "passive adaptive composite" idea for the International Moth: off-axis fibres let the foil "passively control its pitch angle to reduce the lift generated at higher boat speeds"; coupled CFD–FEA was validated with wind-tunnel full-field deformation measurements, with twist reducing effective angle of attack "by approximately 30 % at higher flow speeds" [S5]. *(Verified)*

Manufacturing of tailored layups has moved from hand layup to **automated fibre placement (AFP)** with curvilinear paths: Maung, Prusty and co-workers at UNSW built full-scale hydrofoils in an RTM mould by AFP, compared them with RTM builds under static cantilever and experimental modal analysis [S27], and later optimised curvilinear fibre paths for bend-twist performance [S28]. The fatigue programme on one such foil (10⁵ cycles, distributed optical fibre sensing) showed no stiffness or modal change [S9]. *(Verified abstracts)*

**Sign convention the tool must adopt.** All three groups use "fibre angle toward the leading edge" = wash-out = load-alleviating. The repo's GAP-04 convention (positive incidence nose-up; anhedral negative z) must be extended with **positive fibre angle = rotated toward the leading edge from the elastic axis**, so that a positive angle produces negative (nose-down) twist under positive lift, matching [S1][S2][S3]. *(Inferred; convention Verified in sources)*

#### Dynamic hydroelasticity: divergence and flutter

Akcabay & Young (2019) compare composite plates in air and water: natural frequencies drop and damping rises in water; both vary with speed; "the speed and frequency dependency of the fluid loads lead to an emergence of a new mode" whose frequency can approach zero; "static divergence occurs when the frequency and damping value of the new mode goes to zero"; composite plates in water "tend to experience single-mode flutter, when the damping of the new low-frequency mode vanishes"; "static-divergence governs when material bend-twist coupling leads to nose-up twist, and flutter governs otherwise"; and "dynamic load amplification is more severe in water because of higher fluid added mass" [S3]. Their 2020 study adds sweep: the fibre angle effect dominates the sweep effect, and in water "a backward swept hydrofoil with fibers aligned toward the inflow was found to be the best combination to avoid divergence and flutter" [S4]. Ng et al. 2024 (Composite Structures) provide the differentiable dynamic implementation (DCFoil.jl) [S22][S24]. *(Verified)*

The classical closed-form checks (Theodorsen's unsteady lift function for a 2-DOF section, NACA Report 496, re-computed by NASA in 2015; Pines's elementary flutter explanation) remain the right *first* screen for a design tool because they need only EI, GJ, mass, inertia, elastic-axis and CG offsets, and lift-curve slope — all quantities the beam tier already computes. Their validity envelope is thin-airfoil, incompressible, attached flow and small amplitude; in water the added mass of the section (≈ρ_w π (c/2)² per unit span for a flat plate) is of the same order as the structural mass, which is why the "new mode" of [S3] appears and why an air-derived flutter margin is not transferable. *(Theodorsen/Pines existence Verified via NTRS [S29]; formulas and the added-mass statement Flagged as recall; the qualitative water effect Verified [S3])*

#### Materials and constructions in production water-sports foils

The sibling file already records that mast stiffness is marketed as "UHM/HM carbon" with no numbers. Axis's own pages confirm the two construction families that dominate the market: a **19 mm aluminium mast** section "developed after countless hours of computer analysis" and **one-piece carbon masts** in standard, "High Modulus" and "PRO Ultra High Modulus" grades, sold on "bend and twist resistance" and "avoid[ing] the flow/drag and jointing issues associated with multi-part mast constructions" [S25]. Front wings on every brand page opened in the sibling session are carbon; the constructions inferred from practitioner sources (not opened this session) are: monolithic prepreg carbon for thin race wings; carbon skins over PVC/PU/PET foam or a machined core for larger wings; occasional glass/injection-moulded wings on entry-level and e-foil products. *(Verified for Axis wording [S25]; construction taxonomy Flagged, practitioner recall)*

#### Roughness and the clean-polar assumption (GAP-10)

The XFOIL user guide is explicit that the e^N criterion models 2-D Tollmien–Schlichting growth only; crossflow, attachment-line and **bypass transition** ("sufficient wall roughness and/or large freestream turbulence") are outside it and must be mimicked with trips or Ncrit ≤ 1 [S16]. The classical tripping criterion for distributed three-dimensional roughness is a roughness Reynolds number Re_k = u_k k / ν_k (velocity at the top of the particle, particle height) with experimental critical values "between about 250 and 600" up to Mach 2 and 600 adopted as the working value; turbulent spots form at the particle at Re_k,t and "a small increase … is required to move the fully developed turbulent boundary layer substantially up to the roughness" [S17]. The consequence for polars: a sanded, dinged or fouled foil operates with an earlier transition than the Ncrit-9 polar predicts; the tool can bound this by re-running the polar with a forced trip at the roughness location, which XFOIL supports directly [S16]. Fully-rough drag increments (the ITTC 1978 roughness allowance ΔC_F as a function of k_s/L; Schlichting's admissible roughness k_adm ≈ 100 ν/U for a hydraulically smooth turbulent layer) are recalled, not re-opened this session — **Flagged**. *(Verified [S16][S17]; ΔC_F and k_adm Flagged)*

#### 3D printing of foils

Printed foils are real in the prosumer space, but the sources reachable this session are generic: SLS PA12 is "the most commonly used material" and parts "typically shrink by 3 % to 3.5 %" on cooling, compensated in build software, with warping risk on large flat surfaces [S20]; untreated SLS PA12 plates gained 0.76 ± 0.08 % mass in ASTM D570 water absorption (0.35 % after vapour smoothing) [S21]; chopped-carbon nylon (Markforged Onyx) is quoted at 71 MPa flexural strength and 145 °C HDT [S30]; the torsional behaviour of continuous-carbon nylon prints is the subject of a 2026 AIAA paper (title only) [S31]. What is *not* established for foils: fatigue in water, creep of the wet polymer under sustained lift, and the anisotropy penalty of layer orientation across the span — all **Flagged** and the cheapest probes are listed under open questions. *(Verified as cited; foil-specific claims Flagged)*

#### Fabrication routes and what each imposes on the design

| Route | What it is | Design-space consequence | Cost / lead time | Label |
|---|---|---|---|---|
| CNC-machined plug → composite mould → prepreg/autoclave or vacuum-bag oven cure | Tooling board or aluminium mould, two cavities, carbon prepreg plies, cure 80–120 °C under vacuum (autoclave adds 3–7 bar) | Parting line at LE/TE, draft on non-self-releasing walls, TE floor ≈ 2 × skin stack, ply-drop scheme along span, spar or core inside; tightest shape control and highest fibre volume | Mould cost dominates; weeks; the production route for race wings | **F** (practitioner); prepreg property class **V** [S15] |
| CNC-machined core + wet or prepreg skins (no female mould) | Foam/PVC/PET core cut to the *offset* surface, skins laminated over it, faired by hand | Skin thickness must be subtracted from the loft before cutting (the offset export); finish and k_s depend on the hand fairing; core sets minimum thickness (core + 2 skins) | Cheapest one-off route; days; the DIY/prosumer default | **F** |
| Resin transfer moulding / infusion | Dry fibre in a closed mould, resin injected or drawn by vacuum | Same mould constraints; lower Vf (≈50 %) than prepreg → use fabric properties from [S15]; RTM moulds double as AFP tools [S27] | Mould cost; used by UNSW for full-scale foils | **V** for [S27]; the rest **F** |
| Compression moulding (SMC/BMC, forged carbon) | Chopped-fibre charge pressed hot | Isotropic-ish, low stiffness (E ≈ 30–40 GPa), thick sections; entry-level wings | Tooling heavy; volume route | **F** |
| Injection moulding (glass-filled PA/PP) | Thermoplastic in a steel tool | Draft ≥ 2° [S19], uniform wall, no thin TE, ribs inside; only for low-load stabilisers and e-foil wings | High tool cost, low unit cost | **V** for draft [S19]; use case **F** |
| SLS PA12 / PA-CF print | Powder-bed, no supports | Watertight closed mesh, wall ≥ 1 mm, 3–3.5 % shrink compensated by the printer software — do **not** pre-scale the STL [S20]; 0.76 % water uptake [S21]; rough as-printed surface (tripped polar) | Hours–days; no tooling | **V** [S20][S21]; wall floor **F** |
| FFF continuous-carbon nylon | Extruded nylon with continuous carbon tows | Layer orientation must run spanwise (print the wing on its LE or in halves and bond); ≥ 2 perimeters; anisotropy and interlayer weakness in torsion [S31 title]; creep when wet (F) | Hours; desktop | **F** except [S31] existence |
| Aluminium mast (extrusion + CNC) | Extruded section machined at the ends | 19 mm class section [S25]; galvanic isolation at every carbon joint [S33]; stiffness fixed by the extrusion die | Low cost, mass-market | **V** [S25]; rest **I** |
| Carbon mast (one-piece moulded) | Prepreg over mandrel/bladder or two-part mould | HM/UHM grades marketed for bend and twist resistance [S25]; a Timoshenko beam with warping is the minimum model (mast L/h ≈ 40–60) | Premium | **V** wording [S25]; model note **I** |

#### CAD/CAM expertise the project will need later (question 7)

What a CAM system needs from the geometry, independent of vendor (sibling file [S35] covers the STEP entity; this covers the *quality* the CAM operator checks): a **closed shell of faces** with consistent outward normals; **no slivers or gaps** at the tip cap and at station seams (OpenVSP's own release notes distinguish trimmed watertight solids from untrimmed sliver-free surfaces — sibling finding); **G1 across every seam and G2 where the finishing pass is a flow/scallop pass** (a G1-only seam leaves a visible tool mark line because the surface normal is continuous but curvature jumps, and the ball-end scallop pattern changes direction); **no undercut for the chosen pull direction** beyond the draft angle; **fillets curvature-continuous** at the root/fuselage blend; and **millimetres with a declared tolerance** (the "open-and-measure" gate in EXP-02). Typical mould/plug toolpath sequence: adaptive/roughing with a flat or bull-nose end mill leaving 0.5–1 mm stock; semi-finish; finishing with a ball-end mill using **parallel (raster)** passes along the span for gently curved upper/lower cavities, **scallop / constant-stepover** for the LE region where slope changes fast, **steep-and-shallow** or **flow-line (morphed)** strategies for the tip and root fillets, stepover set from the scallop-height formula (h_s = R − √(R² − (a/2)²)); 3-axis suffices for a two-part foil mould because each cavity is a height field in its pull direction; 5-axis is needed for one-piece struts, deep down-turned tips or when machining the *part* rather than the mould. Slicers (SLS: EOS/Formlabs/Sinterit build processors; FFF: PrusaSlicer/Cura/Bambu) need a **watertight, manifold mesh** (STL/3MF) with sufficient facet density at the LE (chordal deviation ≤ 0.02 mm at a 3 mm LE radius) and a wall thickness above the printer floor; anything thinner than two perimeters is printed as a single bead or dropped silently. Vendors: Fusion 360 (Autodesk, proprietary; the prosumer default with 3-axis and 5-axis tiers), Mastercam and PowerMill (proprietary; job shops), Vectric (proprietary; hobby 3-axis), FreeCAD CAM with OpenCAMLib (LGPL-2.1 — process invocation only under COMMIT-02) [S23]. All strategy names and the Ra/scallop/facet numbers in this paragraph are practitioner content reproduced from recall — **F** except the licences (**V** [S23]) and the STEP entity facts in the sibling file.

**Contradicts existing repo knowledge.** The gap register (`bench-gap-register`, GAP-02 sources table) and this task's brief refer to the beam + lifting-line static hydroelastic study as a **TU Delft** study. The paper's byline is Galen W. Ng, Eirikur Jonsson, Yingqian Liao, Sicheng He and Joaquim R. R. A. Martins — the **University of Michigan MDO Lab** — published in the IMDC-2024 proceedings that TU Delft hosts [S1]. The finding is unchanged; the attribution should be corrected wherever it is cited (it matters for who to contact for data and for DCFoil.jl's provenance). Second, the register lists "Predicted vs experimental bending stiffness agreed within 20 %" as **Verified**; the source it points to (Maung et al. 2021, [S8]) could not be opened by any route this session, so this file downgrades that number to **Flagged** until someone opens the paper and quotes the table — the label in the register should not be stronger than the evidence chain behind it.

## Validation data, special hydrofoil physics and testing numerical design software

*Source: [13-validation-special-physics-and-numerical-testing.md](13-validation-special-physics-and-numerical-testing.md) (`kb-hw-validation-special-physics-and-numerical-testing`).*

#### Validation datasets a hydrofoil tool can honestly use

**Day, Cocard & Troll 2019 (Moth T-foil, Strathclyde).** The foil is a c. 2006 Bladerider Moth main foil:
horizontal span 0.988 m, root chord 0.125 m (extrapolated through the bulb), chord at 90 % span 0.045 m, mean
chord 0.095 m, t/c 12.8 %, camber 3.1 %; vertical 1.0 m span, chord 0.118→0.1135 m, t/c 14.5 %, section
"matched very closely" a NACA 66012 scaled to 14.5 %. Tests ran in the Kelvin Hydrodynamics Laboratory tank
(76 m × 4.6 m × 2.5 m, water depth 2.1 m). Load cells were calibrated in three axes with cross-coupling
determined per cell; the rig was verified under representative horizontal and vertical loads. Results are
presented as lift area L/(½ρV²) and drag area D/(½ρV²) *of the whole assembly* so that no decomposition
between horizontal and vertical is required — which means these numbers are **not** wing-only coefficients
(A6). Uncertainty is discussed qualitatively (zero of α unknown because shims set the horizontal relative to
the vertical; flap pointer moved >2° at high load; background turbulence not measured) but no GUM-style budget
is stated. Free-surface effect: at h/c̄ = 1.05 and low lift, drag at a given lift was *lower* than at h/c̄ =
4.8, attributed to the loss of strut drag outweighing the horizontal's lift loss. The simplified prediction
used XFOIL (3126 cases: 6 Re from 10⁵ to 10⁶, 13 α, 17 flap angles, free transition, **Ncrit = 4**
"corresponding to a turbulence level of around 0.6 %"), a 21-station lifting line, Daskovsky's free-surface
factor, Gibbs & Cox strut interference γ = 0.8 t/b, Coffee & McKann spray drag, Hoerner junction drag; model 2
"generally performs better" but agreement is "noticeably less good for higher angles of attack, which may
affect prediction of take-off speed". — *(Verified, [S2])*

**NACA TR-1232 (1955).** Two 8-inch-chord NACA 64₁A412 hydrofoils (AR 10 and AR 4; the 64₁A412 is the 64₁-412
with the trailing-edge cusp removed) on an NACA 66₁-012 strut intersecting the upper surface without fillets;
polished stainless steel. AR 10: Langley tanks no. 1 (mean depth 10.64 ft = 15.98 chords) and no. 2 (6.0 ft =
9.0 chords), 0.84 and 3.84 chords submergence, 5–45 ft/s, Re 0.18–1.64 × 10⁶. AR 4: tank no. 2,
0.59/1.09/2.09/3.09/4.09 chords, 15–35 ft/s, Re 0.873–2.04 × 10⁶. Depth is measured from the undisturbed
surface to the quarter-chord. Theory: multiple-image horseshoe vortices for the free surface and tank bottom,
plus wave drag; "the agreement between theory and experiment at both supercritical and subcritical speeds is
satisfactory for engineering calculations of hydrofoil characteristics from aerodynamic data". Struts were
also run alone for tares. This is the primary source behind every "Wadlin correction" cited second-hand in the
repo's area 04. — *(Verified, [S3])*

**DTIC ADA032272 (1976).** See headline 3. What the record establishes: two section families (16-309, modified
64A309), two facilities, flap and pitch sweeps, depth sensitivity below one chord, and the conclusion that the
64A309 "can achieve a higher lift-to-drag ratio than the 16-309". What it does not establish from the
metadata: the strut/pod geometry, Reynolds numbers, or measurement uncertainty. The coefficient tables have
still not been extracted (open since the gap register). — *(Verified metadata, [S1]; tables Flagged)*

**Beaver & Zseleczky 2009 (USNA, full-scale Moth foils).** Full-scale tow tests of "various home built and
commercially available T-foil configurations" plus hull drag at several displacements; SNAME CSYS paper, not
open access. Day et al. chose 457 mm submergence partly to match this study. Together the two form the only
pair of independent full-scale T-foil datasets located. — *(Verified abstract, [S24])*

**CEHINAV 2026 (kitefoil/windfoil wing near the free surface)** and **Delft surface-piercing foils (JFM
2026)** are already established in area 04 [S36] (0.059 m², span 0.634 m, c̄ 0.0735 m, Re 0.7–2.9 × 10⁵, Fr_h
0.4–6.7, h/c 0.5–9.5; ventilation maps at Fr 0.5–2.5). This file does not repeat them; it ranks them.

**NASA TMR 2D NACA 0012.** Re 6 × 10⁶, M 0.15, α from 0° to the highest steady angle; compares CL(α), CD(CL),
Cp and Cf at α = 0°, 10°, 15°; data files for Ladson (tripped), Gregory & O'Reilly (Re 3 × 10⁶, better
leading-edge Cp resolution) and digitised Abbott & von Doenhoff; caveats: drag "greatly affected" by tripping,
tripped CD at Re 3 × 10⁶ ≈ 10 % above 6 × 10⁶, dataset differences grow near stall, and supplied grids "are
likely not fine enough when high accuracy is required". — *(Verified, [S6])*

**Cavitation-tunnel section data.** Shen & Dimotakis (JFE 111(3), 1989) "Viscous and Nuclei Effects on
Hydrodynamic Loadings and Cavitation of a NACA 66(MOD) Foil Section" is the canonical sheet-cavitation dataset
used by Kunz et al. (Computers & Fluids 29, 2000) to validate a preconditioned two-phase Navier–Stokes method.
Only the citations were confirmed this session; the Re, σ and load values are **Flagged** until the paper is
opened. The Delft twisted hydrofoil (Foeth, van Terwisga & van Doorne, JFE 130, 2008; Foeth et al., Exp.
Fluids 40, 2006) is the reference for 3D sheet-cavity structure and collapse (time-resolved PIV), not for
force coefficients. — *(Verified citations, [S25][S26][S27])*

**Duncan 1983.** Surface-height profiles and velocity/total-head distributions behind a fully submerged 2D
hydrofoil resolved drag into a breaking-wave part and a non-breaking wave-train part; at incipient breaking
the first wave exists in either state. It validates a free-surface CFD's wave pattern and breaking onset, not
a foil polar. — *(Verified abstract, [S22])*

**Hydroelastic tests.** Marimon Giovannetti, Banks, Ledri & Turnock (Ocean Eng. 167, 2018) is the Southampton
bend–twist-coupled hydrofoil study; relevant to GAP-02, not to steady polars. — *(Verified citation, [S30])*

**Field GPS/IMU.** No public dataset for a consumer race foil was found here or in area 04. — *(Flagged gap)*

**Ranking for the A6 ladder (usefulness to CFD-Workbench, highest first):** (1) Day 2019 — closest
configuration, public PDF, low Re, foil+strut, free-surface pair; (2) TR-1232 — public, wide depth/Fr series,
AR 4 and 10, foil+strut, 64A section family already in the catalog; (3) CEHINAV 2026 — race-type planform,
Fr_h to 6.7, but Re one decade below full scale and no data licence; (4) NASA TMR NACA 0012 — RANS backend
verification only; (5) ADA032272 — needs table extraction and configuration reconstruction; (6) Delft
ventilation maps — envelope flags only; (7) Shen & Dimotakis / Delft twist — future cavitation-CFD validation;
(8) Duncan — free-surface CFD only. — *(Inferred ranking)*

#### Cavitation physics for screening

**Definitions.** σ = (p_∞ − p_v)/(½ρV²) with p_∞ = p_atm + ρ g h at the foil depth h; the local pressure
coefficient Cp = (p − p_∞)/(½ρV²); cavitation is *possible* wherever Cp < −σ, so the classical inception
estimate is σ_i ≈ −Cp_min. This is the basis of ANA-10's "σ_required = −Cp_min" bucket display. — *(Standard
definitions; the equality σ_i = −Cp_min is a nuclei-rich upper bound, see next paragraph — Inferred from
[S28][S29])*

**Why inception scatters.** Brennen's monograph (Oxford 1995; Caltech open copy permits "individual,
educational, research and non-commercial reproduction") treats nucleation, nuclei populations and their effect
on inception in Chapters 1 and 6. The precise sentences could not be extracted this session, so the mechanism
statements below are labelled Flagged (recall): water with few free-stream nuclei sustains tension, so σ_i <
−Cp_min; dissolved-gas content and residence time change the nuclei spectrum; laminar separation bubbles trap
nuclei and can raise σ_i above the attached-flow −Cp_min; facility comparisons on the same headform
historically spread by tens of percent in σ_i. What *is* verified: Amromin (2001) computed different scale
effects on suction and pressure sides of the E817 relative to NACA 0012 owing to Reynolds- and Weber-number
effects on sheet-cavity equilibrium in the boundary layer; Amromin & Rozhdestvensky (2022, CC BY) state that
fully turbulent CFD "satisfactorily predict[s] pressure distribution around cavitation-free blades" but
"analysis of blade cavitation inception is a difficult task for these tools", and propose correlating the
inception number with the cavitation-free pressure minimum via validated 2D multizone solutions; Amromin
(2017) shows hydrofoil *material* (via flow-induced vibration) shifts inception and desinence numbers. —
*(Verified [S28][S42][S43]; mechanisms Flagged [S29])*

**Regimes.** Sheet (attached, from the leading edge), cloud (shed from sheet closure, erosive), bubble
(travelling nuclei), tip-vortex (lowest σ_i on a finite wing; requires resolving the vortex core),
supercavitation (cavity closes downstream of the trailing edge). The ITTC numerical-prediction procedure
(7.5-02-03-03.4 Rev 03, 2024) is explicit: "for the most part only sheet cavitation can be predicted with
reliability"; tip-vortex inception "requires a direct simulation of the larger, energy-containing turbulent
scales" and remains "primarily a research topic"; "for cloud cavitation there appears to be no reliable means
of prediction". Erosion risk sits with cloud collapse and is outside any steady screen. — *(Verified, [S10])*

**Discrete-method under-read of Cp_min.** Panel/IBL and neural surrogates sample Cp at finite stations and can
miss or smear a narrow leading-edge suction peak; the polar surrogate's Cp_min is therefore a screening
estimate (ANA-02 already says so). Practitioner margins of 10–20 % on σ (i.e. require σ_operating > 1.1–1.2 ×
(−Cp_min)) are widely used but were not confirmed in a primary source this session. — *(Flagged practitioner
practice)*

**What steady RANS cavitation models can claim.** Homogeneous-mixture, mass-transfer models (Kunz;
Schnerr–Sauer; Zwart) reproduce sheet-cavity length and the load reduction it causes when calibrated, and were
validated against the NACA 66(MOD) data by Kunz et al. (2000). They cannot predict inception from nuclei-poor
water, cloud shedding statistics (unsteady, needs scale-resolving turbulence), or erosion. — *(Verified
citation [S27]; capability statement Inferred from [S10])*

#### Free-surface physics

**Two limits.** A foil at depth h under a free surface sees an image system whose sign depends on Fr_h =
V/√(gh) (TR-1232 uses the depth-based Froude number for the critical-speed definition and the images for the
free surface and rigid bottom). At Fr → ∞ the free surface behaves as a constant-pressure boundary and the
image bound vortex has *opposite* sign (lift-curve slope reduced); at Fr → 0 it behaves as a rigid wall with a
same-sign image (lift-curve slope increased). Hough & Moran (JSR 13(1), 1969) solved the 2D arbitrary-camber,
arbitrary-Froude problem by singularity distributions and collocation; Kennell & Plotkin (JSR 28(1), 1984)
extended thin-hydrofoil theory to second order. — *(Verified citations and TR-1232 text [S3][S21][S23]; limit
interpretation Inferred, standard)*

**The infinite-Froude correction actually used in practice.** Day et al. multiply the 3D lift coefficient by
(1 + 2/AR)/(1 + 2K(1 + σ)/AR), K = (16(h/c)² + 1)/(16(h/c)² + 2), σ = 1/(1 + 12h/b), and the induced-drag
coefficient by (1 + σ); they report this "yield[s] similar results to the model proposed by Wadlin (1955)".
For finite Froude number they add a wave-drag term of the classical 2D form C_Dw =
(C_L²/(2Fr_c²))·exp(−2(h/c)/Fr_c²) (Daskovsky; "broadly similar" to Vladimirov 1955) — the exponent in the
extracted PDF text is garbled, so the form here is the classical submerged-vortex result and is labelled
Inferred. — *(Verified [S2]; wave-drag form Inferred)*

**Measured vs corrected.** Running the K-factor at AR 8 gives lift ratios 0.960 (h/c = 1.5), 0.964–0.978 (h/c
= 3–6) [S40]; the CEHINAV test reports ≈17 % C_L loss at h/c = 4 between Fr_h = 2 and 5, effects concentrated
at h/c < 4 and the deep-water asymptote only at h/c > 5 [S36]. The steady infinite-Froude image cannot produce
a Froude-dependent 17 % change; the difference must be wave-induced downwash at finite Fr_h, low-Re section
behaviour (Re 0.7–2.9 × 10⁵ in the tank), or both. This is the single most important reason not to label any
depth correction Verified. — *(Executed arithmetic [S40]; conclusion Inferred)*

**Wave resistance and breaking.** TR-1232 measured a gradual drag rise toward the critical speed; Duncan
measured the breaking/non-breaking split behind a 2D foil. Spray and breaching are surface-piercing phenomena
handled under ventilation. — *(Verified, [S3][S22])*

**Which regime racing foils occupy.** From the executed table [S40]: at 10 m/s, h = 0.3–0.6 m → Fr_h 4.1–5.8,
h/c 1.5–6; at 20 m/s → Fr_h 8.3–11.7. Every racing case is deep in the high-Froude, anti-symmetric-image
regime; the low-Froude "ground effect" limit is irrelevant except at displacement speeds during take-off (5
m/s, Fr_h 2–3). The CEHINAV authors' "competition regime Fr_h ≈ 5" [S36] agrees with the 10–15 m/s rows. —
*(Executed arithmetic, Inferred placement)*

#### Ventilation and surface-piercing struts

**Mechanisms and regimes.** Harwood, Young & Ceccio (JFM 800, 2016) tested a vertically cantilevered
surface-piercing hydrofoil with an immersed free tip at low-to-moderate Froude and Reynolds numbers: fully
wetted, partially ventilated and fully ventilated regimes; stability regions overlap so that flow history
selects the regime (bistability, hysteresis); formation "requires air ingress into separated flow at
sub-atmospheric pressure"; elimination occurs by "upstream flow of the re-entrant jet"; a semi-theoretical
scaling on the re-entrant-jet angle captures the washout boundary. Young, Harwood, Miguel Montero & Ward (AMR
69, 2017) review the physics and the scaling implications of reduced-scale tests. The Delft 2026 JFM paper
(area 04) adds the nose/tail/base trigger taxonomy at Fr 0.5–2.5 and a bistable region wider than earlier
maps; Augier et al. (MARINE 2025) show on-water kitefoil-mast inception correlates with the *rate* of α change
and vertical acceleration. — *(Verified abstracts, [S4][S5][S36])*

**Inception paths relevant to a wing designer.** (a) Strut-induced: separation on the mast at leeway angle
connects the atmosphere to the low-pressure side; (b) tip-vortex/breaching: a wing tip approaching the surface
lets the tip-vortex core or a surface depression admit air; (c) wave/impact: a breaking or ventilated cavity
from the mast swept onto the wing. Geometric mitigations found in practice — fences, anti-ventilation strakes,
sharper mast leading edges, forward rake (Day et al. note the Moth vertical "is installed in the boat with
forward rake, intended to reduce the incidence of ventilation") — are documented as practice, not as validated
design rules. — *(Verified for the rake statement [S2]; others Flagged practitioner practice)*

**What a design tool can screen.** Only geometric and kinematic envelope flags: tip depth margin from the
static geometry (ANA-13 already provides "tip-depth hint … Static geometry, no wave/free-surface prediction"),
a surface-piercing state (h ≤ 0 anywhere on the lifting surface), and a Froude-number annotation. A steady
result must never be labelled ventilation-safe (area 04 D-implication, kept here). — *(Inferred from
[S4][S5][S36]; matches spec A6/ANA-04/ANA-13)*

#### Unsteady / pumping physics

**Theory.** Theodorsen (NACA TR-496, 1935; NTRS 19930090935) gives the incompressible unsteady lift of a 2D
flat plate in harmonic heave h and pitch α as a non-circulatory (added-mass) part plus a circulatory part
reduced by the complex function C(k), k = ωb/U = ωc/(2U); Garrick (NACA TR-567, 1936) derived the mean thrust
and propulsive efficiency of the same motions. NASA re-computed and compared TR-496's numbers in
TP-2015-218765 and TM-2017-219667 (both on NTRS). The formulas themselves were not transcribed from the
reports this session, so their exact form is **Flagged** (standard textbook content) while the reports'
existence and public availability are Verified. — *(Verified availability [S19][S20]; formula content
Flagged)*

**Scaling.** Floryan, Van Buren, Rowley & Smits (JFM 822, 2017; arXiv 1704.07478): performance "depends on
both Strouhal number and reduced frequency, but for motions where the viscous drag is small the thrust closely
follows a linear dependence on reduced frequency", validated by water-tunnel experiments with "excellent
collapse" and consistent with biological data; Van Buren, Floryan & Smits (AIAA J 2018): heave–pitch phase φ
"proves to be a critical parameter"; Van Buren et al. (2017): the correct velocity scale is the mean
trailing-edge velocity, not the flow speed. Triantafyllou, Triantafyllou & Grosenbaugh (J. Fluids Struct. 7,
1993) established the optimal-thrust Strouhal band for oscillating foils; the commonly quoted range 0.25–0.35
(and the gap register's "St ≈ 0.4") is from recall, not read this session. — *(Verified abstracts [S17]; St
band Flagged [S18])*

**Measured pumping on foil boards.** Nothing public. Crossref searches (2018–2026) returned no
rider-kinematics or rider-power study for wingfoil, pump-foil or Moth pumping; the closest item is Liu et al.,
FDMP 2026, "Heave-Induced Thrust and Free-Surface Deformation of an Oscillating Hydrofoil" (title only). A
rough order-of-magnitude from the executed table [S40] with **guessed** inputs (c = 0.15 m, peak-to-peak heave
0.3 m, 0.5–1.5 Hz, 3–8 m/s) gives k ≈ 0.03–0.24 and St ≈ 0.02–0.15 — i.e. pump-foiling probably sits *below*
the classical high-efficiency Strouhal band and in the quasi-steady-to-mildly-unsteady k range, which would
make a quasi-steady + added-mass estimator plausible. That is an hypothesis to test with instrumented
sessions, not a finding. — *(Flagged; inputs are guesses)*

**What each estimator can claim.** Quasi-steady: instantaneous lift from the steady polar at the effective
α(t), no phase lag, no added mass — labelled "Computed estimate · quasi-steady, k < 0.05 assumed". Unsteady
linear: Theodorsen/Garrick with C(k) and added mass — valid for small amplitude, attached flow, deep water;
near a free surface the image system also becomes unsteady (Liu 2026 title suggests measurable free-surface
deformation). A "pumpability" metric could be defined as cycle-averaged thrust per unit rider mechanical power
at a given (k, St, φ) from Garrick-class theory, but it has no measurement to validate against today. —
*(Inferred)*

#### Multi-fidelity reconciliation on the measurement side

**Vocabulary (ITTC 7.5-03-01-01).** Simulation error δ_S = S − T = δ_SM + δ_SN (modelling + numerical);
verification estimates U_SN² = U_I² + U_G² + U_T² + U_P² (iteration, grid, time step, other parameters);
validation compares E = D − S with U_V² = U_D² + U_SN². If |E| < U_V "the combination of all the errors in D
and S is smaller than U_V and validation is achieved" at that level; if |E| ≫ U_V, E ≈ δ_SM and can be used to
improve the model. Convergence ratio R = ε₂₁/ε₃₂ classifies grid studies (monotonic convergence 0 < R < 1;
oscillatory convergence −1 < R < 0; monotonic divergence R > 1; oscillatory divergence R < −1). Minimum three
solutions; r = √2 suggested for industrial CFD; F_S = 1.25 for 0.5 ≤ p < 2.1 with σ < Δφ, else 3. —
*(Verified, [S8])*

**GCI (Roache 1994).** p = ln[(f₃ − f₂)/(f₂ − f₁)]/ln r; Richardson estimate f_{h=0} = (r^p f₁ − f₂)/(r^p −
1); GCI₁₂ = F_S |ε₁₂|/(r^p − 1) with ε₁₂ = (f₁ − f₂)/f₁, F_S = 3 for two grids, 1.25 for three or more;
asymptotic-range check GCI₂₃/(r^p GCI₁₂) ≈ 1; r ≥ 1.1. — *(Verified, [S12])*

**ASME V&V 20.** The ITTC CFD guideline names "ASME Guide on Verification and Validation in Computational
Fluid Dynamics and Heat Transfer" (V&V 20) as a reference; the ASME page could not be opened this session.
From recall (Flagged): V&V 20-2009 defines E = S − D and u_val² = u_num² + u_input² + u_D², and the 2016
supplement adds a multivariate metric. Coleman & Stern (JFE 119, 1997) originated the E-vs-U_V criterion;
Stern, Wilson, Coleman & Paterson (JFE 123, 2001) is the methodology paper the ITTC procedure cites. Oberkampf
& Roy (CUP 2010) is the textbook; a new edition "Verification, Validation, and Uncertainty Quantification in
Scientific Computing" (CUP 2025, DOI 10.1017/9781009031004) exists. — *(Verified citations [S9][S15][S16]; V&V
20 content Flagged)*

**Presenting discrepancy to a user (GAP-06).** The honest presentation is the pair (E, U_V) per quantity
against a *named* dataset and configuration, plus the numerical uncertainty of the tool's own tier (GCI for
CFD; convergence-with-panels for VLM; the surrogate's stated error for NeuralFoil-class polars). The ITTC CFD
guideline adds a practical rule: once uncertainty is estimated for one case of a parameter study, it can be
reused for similar cases provided the flow does not change character — which licenses caching a verification
result per method/geometry family. Never collapse E into a multiplicative "correction factor" applied
silently; a learned discrepancy model (correction as a function of Re, h/c, Fr_h) is legitimate only when
displayed as its own layer with its own training-set provenance. — *(Verified guideline statement [S9];
presentation rule Inferred)*

**The label ladder made concrete (A6).**
| Label | Evidence required | Example in this file |
|---|---|---|
| Illustrative mockup | none; sample numbers | mockup readouts |
| Computed estimate | method named, envelope named, no verification proof | estimator L/D; K-factor depth correction; Cp_min screen |
| Verified numerical implementation | analytic-reference tests pass; MMS/convergence for kernels; cross-platform golden masters within stated tolerance; GCI reported for CFD | elliptical-wing e = 1, thin-airfoil 2π, NACA closed form; TMR NACA 0012 grid study |
| Experimentally compared | E and U_V (or "U_D not stated") displayed against a named dataset with comparable configuration | Day 2019 lift/drag areas (foil+strut); TR-1232 depth series; CEHINAV h/c–Fr_h grid |

#### Testing numerical design software (GAP-09)

**The oracle problem and its answers.** Kanewala & Bieman (IST 56, 2014; 62 primary studies) group challenges
into those intrinsic to scientific software (oracle problems — no independent way to know the right answer)
and cultural ones (scientists "viewing the code and the model that it implements as inseparable entities").
Catalogued techniques: analytic/closed-form solutions, experimental data, pseudo-oracles and N-version
comparisons (another code), metamorphic testing, statistical tests, and code-clone detection to find
duplicated numerics. — *(Verified abstract [S7]; technique list Inferred from the review's scope)*

**Code verification by manufactured solutions.** Roache (JFE 124, 2002) and Salari & Knupp (Sandia 2000):
choose an analytic field, substitute into the governing operator to obtain a source term, run the code with
that source, and confirm the *observed* order of accuracy p against the design order on refined grids. For
CFD-Workbench this applies to any discretised operator the tool owns (loft/curve evaluators, strip
integration, VLM influence matrices): the manufactured analogue is "manufactured geometry" — an analytic
surface (e.g. exact elliptic planform with an exactly known area, centroid and second moment) whose derived
quantities must converge at the expected rate as station count grows. — *(Verified citations [S13]; adaptation
Inferred)*

**Analytic references (the floor for "Verified numerical implementation").** Thin-airfoil theory: Cl_α = 2π
per radian, flat plate α₀ = 0, Cm_{c/4} = 0 for a symmetric section; NACA 4-digit ordinates from the
closed-form thickness polynomial and parabolic mean line, and 6-series/16-series from NASA TM 4741 (area 06
[S37]); Joukowski sections give an exact Cp from the conformal map (standard, Flagged only in the sense that
no source was opened); Prandtl lifting line: elliptical loading e = 1, C_Di = C_L²/(π AR), rectangular wing
with Glauert correction τ ≈ 0.05 (this is exactly what AeroSandbox asserts at `rel=0.05` for CL and `rel=0.3`
for CDi on an AR 10, 10 m × 1 m NACA 0012 wing at 5°, 25 m/s [S32]). Symmetry: a symmetric section at α = 0
must return Cl = 0 and Cm = 0 to round-off; an antisymmetric aileron must yield a rolling moment while the
undeflected wing gives `Cl ≈ 0 (abs 1e-6)` [S32]. — *(Verified for the AeroSandbox assertions; theory
standard)*

**Golden-master / approval tests.** SU2 stores per-case reference values and compares logged residuals/forces
at a given iteration with an absolute tolerance the case sets (`abs(float(data[j]) - self.test_vals[j]) >
self.tol`, default 0.0), with timeouts and sanitizer-aware enabling [S31]. Lesson: tolerances are per quantity
and per case, stored beside the reference, and the reference carries provenance (commit, platform). Approval
frameworks for .NET (Verify, MIT; ApprovalTests.Net) serialise outputs to reviewed files; for numerics they
need a custom comparer with tolerance, never string equality of floats. — *(Verified [S31][S35]; practice
Inferred)*

**Float comparison rules.** (i) Relative tolerance for O(1) coefficients (|a − b| ≤ ε_rel·max(|a|,|b|)); (ii)
absolute tolerance near zero (Cl at α = 0, Cm of a symmetric section) because relative error is undefined
there; (iii) ULP comparison only for operations IEEE 754 makes exact-rounded (+ − × ÷ √ FMA), where bitwise
reproducibility across macOS ARM64 and Windows x64 is achievable if the compiler does not contract (.NET:
`Math.FusedMultiplyAdd` is explicit and "rounded as one ternary operation"; whether RyuJIT ever auto-contracts
a*b+c is **Flagged** — it did not in versions known from recall, and a spike must confirm for .NET 10); (iv)
transcendental functions (sin, cos, exp, log, pow, atan2) differ across libm implementations by up to a few
ULP — documented for .NET (`Math.Sin` "may differ between different operating systems or architectures") and
for Rust ("non-deterministic … varies by platform") — so any golden master containing them must use tolerance
≈ 1e-12 relative (or ship its own polynomial implementations for bitwise parity). — *(Verified [S33][S34];
rules Inferred)*

**Metamorphic relations for this domain.** Scale invariance: scaling all lengths by λ at fixed Re and α leaves
Cl, Cd, Cm unchanged and scales forces by λ² (with V adjusted for Re) — a test that catches unit bugs; mirror
symmetry: mirroring the planform about the centreline leaves lift and drag unchanged and flips roll and side
force; unit round-trip: SI → imperial → SI equals identity within 1 ULP of the conversion constants; station
insertion: adding an authored station at a location where the loft is interpolated changes the evaluated
surface by less than the DOC-02 tolerance (1 µm); reparametrisation: reversing the parameter direction of a
curve leaves the evaluated shape and derived area invariant; monotonic depth: with everything else fixed,
increasing h toward the deep asymptote must move the corrected lift monotonically toward the deep-water value.
Chen et al. (ACM CSUR 51, 2018) is the review of the method. — *(Verified citation [S14]; relations Inferred)*

**Property-based testing.** FsCheck (BSD-3-Clause) for .NET, proptest (Apache-2.0/MIT) for Rust, Hypothesis
(licence unconfirmed via API; MPL-2.0 from recall) for Python spikes: generate random valid
geometries/operating points and assert invariants (derived AR = b²/S never stored; area > 0; Cl(α) monotonic
in the linear range; σ decreases with V). — *(Verified licences [S35]; practice Inferred)*

**Parsers and regression corpora.** The Phase-0 defect (Lednicer read as Selig, yielding a plausible 6 %-thick
NACA 0012) is a parser-oracle failure; controls: a corpus of `.dat` files with SHA-256, source URL and licence
per file (area 06 already forbids vendoring UIUC files, so the corpus is generated or user-supplied),
round-trip tests (parse → write → parse identical), differential parsing (Selig and Lednicer readers on the
same geometry must agree), fuzzing of the tokenizer with FsCheck/proptest-generated byte streams, and
thickness/closure invariants after parse. — *(Inferred; defect from grounding)*

**CI rings.** Ring 0 (every push): analytic references, metamorphic and property tests, parser fuzz smoke,
cross-platform golden masters on macOS ARM64 and Windows x64 runners; Ring 1 (readiness): panel/VLM
convergence sweeps, MMS order-of-accuracy, SU2/OpenFOAM smoke on a tiny mesh if a backend is detected; Ring 2
(post-merge/nightly): the Day 2019 and TR-1232 comparisons producing E and U_V tables, TMR NACA 0012 grid
study with GCI. — *(Inferred from [S8][S9][S31] and the repo's CI standard)*

#### Product benchmarks as sanity bounds (GAP-12)

The product table in area 04 gives area, span, AR and manufacturer take-off/top-speed bands per discipline.
Usable as *range checks*, not as validation: (a) wing loading W/S from rider + system mass against the product
band; (b) CL at cruise from W/(½ρV²S) must fall inside the section's polar bucket at the operating Re; (c) L/D
of the wing-only estimator must not exceed the elliptical-wing bound 1/(C_D0/C_L + C_L/(π e AR)) with e ≤ 1;
(d) take-off speed from CL_max(Re) must lie inside the maker's stated band ±30 % (band width is itself
Flagged). Manufacturers' numbers are marketing envelopes with unstated rider mass, water and method; they
bound plausibility and never label a result. — *(Inferred; product data from area 04 [S36])*

