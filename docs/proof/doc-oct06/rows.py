#!/usr/bin/env python3
"""Round-oct06 DOC: new DESIGN.md section 7 rows, their source check and the DESIGN.md insert.
usage: rows.py compare <repo> <out> | apply <repo> | map <repo>
"""
import re, sys, pathlib

AP = "AnalysisProjection.cs"
A = "src/CfdWorkbench.Analysis/"
D = "src/CfdWorkbench.Desktop/"
DXF = "docs/design/dx-screen-states.md"
MOCK = "docs/mockups/area3-analysis.html"
GM = "docs/design/group-move-node-m.md"

R101 = "Ruling 101"
R107 = "Ruling 107"
R108 = "Ruling 108 (DR-DXM-1)"
PROP = "proposed — awaiting operator"

rows = []  # (group, text, status, note, kind, src)


def add(group, text, status, note, kind, src):
    rows.append((group, text, status, note, kind, src))


# ---- Ruling 101: built strings with no row, as they read after the two fixes -------------------------------
add("101", "Unavailable — <reason>", R101,
    "the COPY-70 form wherever a value cell read a bare \"Unavailable\": AnalysisProjection.cs VerdictUnavailable and Val, the Wing loading cell, the failed-run Result row; the reason is the code's own",
    "code", (A + AP, ['private const string VerdictUnavailable = "Unavailable";', '"Unavailable"']))
add("101", "; <n> strips: Unavailable — <reason>", R101, "run-sentence suffix for strips with no verdict (AnalysisProjection.cs RunVerdict)",
    "code", (A + AP, ['; {indeterminate} strips: {VerdictUnavailable}']))
add("101", "; <n> Not judged — tip strip", R101, "run-sentence suffix for the provisional tip strips (COPY-220 text)",
    "code", (A + AP, ['; {provisional} {Labels.TipNotJudged}']))
add("101", "Unavailable — this run's geometry revision is not held by this session; verdicts, stations and normals omitted", R101,
    "unheld-revision note (Labels.FeedRevisionNotHeld)",
    "code", (A + "Labels.cs", ["Unavailable — this run's geometry revision is not held by this session; verdicts, stations and normals omitted"]))
add("101", "Inside the method envelope at this strip (<parts>)", R101,
    "per-strip verdict; <parts> reads as in the next row; extends COPY-221",
    "code", (A + "MethodRecord.cs", ['"Inside the method envelope at this strip ("']))
add("101", "Outside the method envelope at this strip — exceeded: <names> (<parts>)", R101, "per-strip verdict; extends COPY-222",
    "code", (A + "MethodRecord.cs", ['"Outside the method envelope at this strip — exceeded: "']))
add("101", "<name> <value><unit> ≤ <bound><unit>", R101,
    "one part of a verdict; reads > when that quantity is the one exceeded; names are |α_eff − α_L0|, Cl_local, sweep",
    "code", (A + "MethodRecord.cs", ['" > " : " ≤ "', '"|α_eff − α_L0|"']))
add("101", "Inside the method envelope (|α_eff − α_L0| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) at all <n> strips", R101,
    "run sentence, all strips inside",
    "code", (A + "MethodRecord.cs", ['"(|α_eff − α_L0| ≤ "', '"°, Cl_local ≤ "','", quarter-chord sweep ≤ "', '"Inside the method envelope " + bound + " at all "', '" strips"']))
add("101", "Outside the method envelope (|α_eff − α_L0| ≤ <a>°, Cl_local ≤ <c>, quarter-chord sweep ≤ <s>°) — <k> of <n> strips; exceeded: <names>", R101,
    "run sentence, some strips outside",
    "code", (A + "MethodRecord.cs", ['"Outside the method envelope " + bound + " — " + outside + " of " + judged', '" strips; exceeded: "']))
add("101", "Unavailable — no attachment point named (DR-ANA-5)", R101, "Moment about attachment point (Loads.AttachmentReason)",
    "code", (A + "Loads.cs", ["Unavailable — no attachment point named (DR-ANA-5)"]))
for t in ["Analysis: no result", "Analysis: Unavailable", "Analysis: Failed", "Analysis: Current", "Analysis: Historical"]:
    add("101", t, R101, "status-strip item (AnalysisProjection.cs StatusText)", "code", (A + AP, ['"' + t + '"']))
add("101", "Unavailable — surface piercing", R101, "Tip depth cell when the tip is at or above the surface (COPY-45 covers \"depth not set\" only)",
    "code", (A + AP, ['"Unavailable — surface piercing"']))
add("101", "Undefined — speed ≤ 0", R101, "derived condition cells (Conditions group and the band) when the speed is not above zero",
    "code", (A + AP, ['"Undefined — speed ≤ 0"']))
add("101", "tip depth <value> m", R101, "3D and side-view depth annotation",
    "code", (D + "Analysis/View3dLoadLayer.cs", ['"tip depth "', '" m"']))
add("101", "free surface and tip depth, values in the conditions table", R101,
    "alt-text clause after \"; \" when the depth layer is visible (View3d.LoadLayer.cs)",
    "code", (D + "Analysis/View3d.LoadLayer.cs", ["free surface and tip depth, values in the conditions table"]))
add("101", "dashed outline: <n> strip outside the method envelope", R101, "plan-view legend, <n> = 1 (singular)",
    "code", (D + "Analysis/PlanLoadLayer.cs", ['"dashed outline: {outside} {(outside == 1 ? "strip" : "strips")} outside the method envelope"'.replace('"strip" : "strips"', '"strip" : "strips"')]))
add("101", "dashed outline: <n> strips outside the method envelope", R101, "plan-view legend, <n> other than 1",
    "code", (D + "Analysis/PlanLoadLayer.cs", ['"strips"', "outside the method envelope"]))
add("101", "Historical — previous result", R101, "banner while a failed run shows the previous result (an instance of COPY-64)",
    "code", (A + AP, ['"Historical — previous result"']))
add("101", "Preview hidden — Apply or Cancel in CAD", R101, "model-area notice while a CAD preview is pending",
    "code", (D + "ModelArea.axaml", ['Text="Preview hidden — Apply or Cancel in CAD"']))
add("101", "Outside strips have dashed outlines and a text count.", R101, "note on the Γ-per-strip layer",
    "code", (A + AP, ['"Outside strips have dashed outlines and a text count."']))
# mockup-only strings (area3-analysis.html rev 3)
add("101", "The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run.", R101,
    "tampered-run note under COPY-211, mockup screen 8", "mock", ["The stored run no longer matches its content hash. It is kept in the file and not shown. Evaluate to compute a new run."])
add("101", "η (root → tip)", R101, "Spanwise loading chart, x-axis title", "mock", ["η (root → tip)"])
add("101", "Cl·c/c̄ (–)", R101, "Spanwise loading chart, y-axis title", "mock", ["Cl·c/c̄ (–)"])
add("101", "dashed: elliptic, same CL", R101, "Spanwise loading chart, legend", "mock", ["dashed: elliptic, same CL"])
add("101", "VLM + strip", R101, "Spanwise loading chart, series label (its own row; also a fragment of COPY-213)", "mock", ["'VLM + strip'"])
add("101", "Historical · VLM + strip", R101, "tier chip while the run is Historical or Failed", "mock", ["Historical · VLM + strip"])
add("101", "10 kn · salt 15 °C · as the band", R101, "Conditions group summary line (mockup values; the units follow the Units setting)", "mock", ["10 kn · salt 15 °C · as the band"])

# ---- Ruling 107: COPY-G1..G12 ---------------------------------------------------------------------------------
for k in range(1, 13):
    add("107", None, R107 + " (DR-GM-7, COPY-G%d)" % k, None, "g", k)

# ---- Ruling 108: every NEW row with drafted text in dx-screen-states.md ---------------------------------------
dx = [
    ("3", "inviscid + turbulent-friction bound; deep water; steady · inviscid; no boundary layer", "Cp and estimator fixed label; depth unset: \"free surface not modelled\" replaces \"deep water\""),
    ("5", "Cp · vik pinned at 0 · −a to +b", "Section view legend"),
    ("6", "cl (panel)", "Section result row label"),
    ("6", "Cm c/4", "Section result row label"),
    ("6", "α_L0 (panel)", "Section result row label"),
    ("7", "Fully turbulent friction bound (ITTC-1957) at this Re; a bound, not a polar value", "estimator cd note; also the estimator-sourced profile drag, row 47"),
    ("8", "at x/c <x> on the <side> surface · 200 stations · three trailing-edge panels per side excluded", "−Cp_min note"),
    ("13", "15 % margin — practitioner assumption, not sourced", "cavitation margin label"),
    ("14", "Clear — σ is above −Cp_min plus the margin", "margin state"),
    ("15", "Inside the margin — σ is above −Cp_min but within the margin", "margin state"),
    ("16", "Possible — σ is at or below −Cp_min; speed is above V_crit", "margin state"),
    ("17", "Governing station: η <η> · depth <h> m · smallest σ / (−Cp_min) of <n> stations", "governing station line"),
    ("19", "Unavailable — vapour pressure missing", "cavitation; reason code ANA-CAV-PV-MISSING"),
    ("20", "Unavailable — local station is surface piercing", "cavitation; reason code ANA-CAV-SURFACE-PIERCING"),
    ("21", "Unavailable — water is invalid", "cavitation; reason code ANA-CAV-WATER-INVALID"),
    ("21", "Unavailable — local depth is invalid", "cavitation; reason code ANA-CAV-DEPTH-INVALID"),
    ("22", "Undefined — −Cp_min ≤ 0", "cavitation; COPY-69 with this cause; reason code ANA-CAV-NO-SUCTION"),
    ("23", "Undefined — static pressure does not exceed vapour pressure", "cavitation; reason code ANA-CAV-PRESSURE-NONPOSITIVE"),
    ("24", "Cp_min under-read, 200 vs 400 panels", "per-run measured under-read"),
    ("25", "Provisional — Cp_min under-read at this station is above 10 % (200 vs 400 panels)", "provisional row (DR-DXM-7)"),
    ("29", "surrogate, relative to XFOIL, validated at NACA 0012 pre-stall only", "surrogate label beside COPY-66 (DR-DXM-3)"),
    ("30", "NeuralFoil-0.3.2/xxxlarge/94638c04", "Provenance Method value"),
    ("31", "analysis_confidence 0.97 · advisory, not an error bar", "example value 0.97 is a fixture"),
    ("33", "Low confidence — analysis_confidence 0.31 is below 0.5. Computed and flagged, never refused; not an error bar.", "advisory; example value 0.31 is a fixture; reason code ANA-POLAR-LOW-CONFIDENCE"),
    ("34", "CST fit residual: max 1.09 × 10⁻⁴ c · RMS 2.93 × 10⁻⁵ c (shape residual, not an aerodynamic error)", "example values are fixtures"),
    ("35", "Inside the validated bracket (α −6° to 6°, Re 2 × 10⁵ to 10⁶, Ncrit 2, 4, 9, NACA 0012 family)", "bracket flag"),
    ("36", "Outside the validated bracket — α 7.50° is beyond ±6°. Computed, not validated.", "bracket flag; one row per axis (α, Re, Ncrit, section family), only the α sentence is drafted"),
    ("37", "Unavailable — α 31.00° is outside the surrogate’s training range (−27.9° to 28.6°)", "non-computable; example values are fixtures; reason code ANA-POLAR-ALPHA-OUTSIDE"),
    ("37", "Unavailable — section fit residual 4.2 × 10⁻⁴ c exceeds the limit 3.6 × 10⁻⁴ c", "non-computable; example values are fixtures; reason code ANA-POLAR-NONCOMPUTABLE"),
    ("38", "Re_local <Re> inside <Re_min> to <Re_max>", "strip row"),
    ("39", "Re_local <Re> outside the polar’s Re range <Re_min> to <Re_max> — cd not extrapolated", "strip row"),
    ("41", "Δ vs lattice Cl_local", "strip row label"),
    ("41", "polar cl at α_eff against the lattice, a per-strip consistency check", "note of the Δ vs lattice Cl_local row"),
    ("42", "Polar bracket:", "strip row label (DR-DXM-2)"),
    ("44", "Overlay a section ▾", "transition overlay control"),
    ("46", "Profile drag from the polar at α_eff, both Ncrit; band, not a prediction", "polar-sourced profile drag note"),
    ("48", "Unavailable — cd missing at <n> strips; the estimator bound is not substituted", "profile drag with strips missing cd; reason code ANA-PROFILE-DRAG-MISSING-CD"),
    ("49", "Wing only: induced (VLM + strip) plus profile (polar). Not a total.", "wing drag note (DR-DXM-5)"),
    ("50", "Unavailable — missing: junction, mast, wave, spray", "craft Total drag when the profile part is present; reason code ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY"),
    ("53", "α <a>° meets CL <t> within 1 %", "Find α, a found α"),
    ("54", "Find α found no α — <reason>. Nothing was extrapolated.", "Find α with no root; the five <reason> sentences are not drafted"),
]
for n, t, note in dx:
    add("108", t, R108, "dx-screen-states row %s: %s" % (n, note), "dx", t)

# ---- Reason-code drafts (Ruling 108 asked for display text; none was drafted) -----------------------------------
# (text, note, [codes], where it shows (file:line), covered-by existing/approved row or None)
drafts = [
    ("Unavailable — drag could not be computed. Evaluate again.", ["ANA-DRAG-UNAVAILABLE"], "AnalysisProjection.cs:232 (drag row value, fallback)"),
    ("Unavailable — wing drag is missing or zero, so CL/CD can't be formed.", ["ANA-WING-RATIO-UNAVAILABLE"], "AnalysisProjection.cs:245 (Wing-only CL/CD value)"),
    ("Wing only: lift over wing drag. Not a craft CL/CD.", ["ANA-WING-ONLY-RATIO"], "AnalysisProjection.cs:248 (Wing-only CL/CD note)"),
    ("Cd (turbulent bound)", ["ANA-SECTION-ITTC1957-BOUND"], "AnalysisProjection.cs:192 (Section row label; the DX row 7 sentence is its note)"),
    ("Unavailable — induced drag is missing.", ["ANA-TOTAL-DRAG-MISSING-INDUCED"], "Loads.cs:51 via AnalysisProjection.cs:232 (Total drag value)"),
    ("Unavailable — missing: profile, junction, mast, wave, spray", ["ANA-TOTAL-DRAG-MISSING-PROFILE"], "Loads.cs:53 via AnalysisProjection.cs:232 (Total drag value when a polar is installed but the profile part is missing)"),
    ("Unavailable — the run has no strips.", ["ANA-PROFILE-DRAG-MISSING-STRIPS", "ANA-INDUCED-DRAG-MISSING-STRIPS"], "Loads.cs:68, :88 via AnalysisProjection.cs:232 (Profile and Induced drag value)"),
    ("Unavailable — a strip width is not recorded.", ["ANA-PROFILE-DRAG-MISSING-WIDTH", "ANA-INDUCED-DRAG-MISSING-WIDTH"], "Loads.cs:77, :93 via AnalysisProjection.cs:232"),
    ("Unavailable — a drag sum is not a finite number. Evaluate again.", ["ANA-PROFILE-DRAG-NONFINITE", "ANA-INDUCED-DRAG-NONFINITE"], "Loads.cs:83, :97 via AnalysisProjection.cs:232"),
    ("Unavailable — no section profile for this strip.", ["ANA-POLAR-PROFILE-MISSING"], "StripCoupler.cs:33 and NeuralFoilPolarSource.cs:100 via AnalysisProjection.cs:232 and :213 (PolarText)"),
    ("Unavailable — the polar gave no result for this strip.", ["ANA-POLAR-UNAVAILABLE"], "StripCoupler.cs:67 via AnalysisProjection.cs:232"),
    ("Unavailable — the polar gave no drag value at this strip.", ["ANA-POLAR-CD-UNAVAILABLE"], "StripCoupler.cs:72 via AnalysisProjection.cs:232"),
    ("Unavailable — the stored polar was made with another method or profile. Evaluate to compute a new run.", ["ANA-POLAR-METHOD-MISMATCH"], "SectionTier.cs:42 and NeuralFoilPolarSource.cs:126 via AnalysisProjection.cs:213 (Ncrit rows)"),
    ("Unavailable — the section revision for this polar is not held by this session.", ["ANA-POLAR-REVISION-MISSING", "ANA-POLAR-REVISION-MISMATCH", "ANA-POLAR-PROFILE-HASH"], "RunPolarResolver.cs:15, :19 and NeuralFoilPolarSource.cs:102 via StripCoupler.cs:74 / SectionTier.cs:59"),
    ("Unavailable — Re <Re> is outside the surrogate’s training range (<min> to <max>)", ["ANA-POLAR-RE-OUTSIDE"], "IPolarSource.cs:28 via SectionTier.cs:57 (Ncrit rows); the α sentence is DX row 37"),
    ("Unavailable — Ncrit <n> is outside the surrogate’s training range (<min> to <max>)", ["ANA-POLAR-NCRIT-OUTSIDE"], "IPolarSource.cs:31 via SectionTier.cs:57"),
    ("Unavailable — this section family is not covered by the surrogate.", ["ANA-POLAR-SECTION-UNVALIDATED"], "IPolarSource.cs:29 via SectionTier.cs:57"),
    ("Unavailable — the polar did not converge at this strip.", ["ANA-POLAR-NOT-CONVERGED"], "IPolarSource.cs:32 via SectionTier.cs:57"),
    ("Unavailable — the polar could not be computed for this section. This is a program fault; the run is kept.", ["ANA-POLAR-NONFINITE", "ANA-POLAR-INPUT", "ANA-POLAR-CST-INPUT", "ANA-POLAR-CST-FIT", "ANA-POLAR-WEIGHTS-MISSING", "ANA-POLAR-WEIGHTS-HASH", "ANA-POLAR-WEIGHTS-FORMAT"], "NeuralFoilNetwork.cs, CstFit.cs, Naca0012Reference.cs via StripCoupler.cs:74 / SectionTier.cs:59 (any ANA-POLAR- code becomes the reason)"),
]
for text, codes, where in drafts:
    add("code", text, PROP, "reason code%s %s; shows at %s" % ("s" if len(codes) > 1 else "", ", ".join(codes), where), "draft", codes)

# ---- Total drag (written last; Ruling 101 string, then the hydrodynamicist's proposal) --------------------------
add("td", "Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray", R101,
    "one string for Total drag and the craft CL/CD (Loads.TotalDragReason replaces \"Unavailable — total drag missing\"); the craft Total drag and craft CL/CD stay Unavailable (hydrodynamicist condition)",
    "code", (A + "Loads.cs", ["Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray"]))
add("td", "Drag (Wing only)", "Ruling 109",
    "row label; one row replaces the \"Wing-only drag\" row (AnalysisProjection.cs:81 and :134); Ruling 108 text was \"Total drag\" marked \"Wing only\"",
    "hyd", None)
add("td", "<min>–<max> <force unit>", "Ruling 109",
    "value: the Ncrit 2–4 band; keeps the surrogate label and the low-confidence flag; the unit follows the Units setting (N in Metric, lbf in Imperial)",
    "hyd", None)
add("td", "Not included: junction, mast, wave, spray", "Ruling 109",
    "new reason line under the Drag (Wing only) row; tip-vortex cavitation stays under Not modelled",
    "hyd", None)

# ---------------------------------------------------------------------------------------------------------------


def gtable(repo):
    out = {}
    for line in (repo / GM).read_text(encoding="utf-8").splitlines():
        m = re.match(r"\| COPY-G(\d+) \| [^|]* \| (.*) \|$", line)
        if m:
            t = m.group(2).replace("`", "")
            t = re.sub(r"\s*\*\(.*\)\*$", "", t)
            out[int(m.group(1))] = t
    return out


def resolve(repo):
    g = gtable(repo)
    first = 250
    res = []
    for i, (grp, text, status, note, kind, src) in enumerate(rows):
        if kind == "g":
            text = g[src]
            note = "docs/design/group-move-node-m.md section 5, row COPY-G%d" % src
            if src == 2:
                note += "; the tip clause shows only when an end vertex moved"
        res.append((first + i, grp, text, status, note, kind, src))
    return res


def line(r):
    n, grp, text, status, note, kind, src = r
    t = text.replace("|", "\\|")
    mark = "approved — " + status if not status.startswith(PROP) else status
    if status.startswith(PROP):
        return "| COPY-%d | %s — %s (%s) |" % (n, t, status, note)
    return "| COPY-%d | %s — approved — %s (%s) |" % (n, t, status, note)


def compare(repo, out):
    cache = {}

    def rd(p):
        if p not in cache:
            cache[p] = (repo / p).read_text(encoding="utf-8")
        return cache[p]
    lines, bad = [], 0
    for r in resolve(repo):
        n, grp, text, status, note, kind, src = r
        if kind == "code":
            path, frags = src
            miss = [f for f in frags if f not in rd(path)]
            ok = not miss
            lines.append("%s COPY-%d %s :: %s :: %s" % ("EQUAL" if ok else "DIFF ", n, text, path, "fragments found: %d/%d" % (len(frags) - len(miss), len(frags)) + ("; MISSING " + repr(miss) if miss else "")))
        elif kind == "mock":
            miss = [f for f in src if f not in rd(MOCK)]
            ok = not miss
            lines.append("%s COPY-%d %s :: %s :: %s" % ("EQUAL" if ok else "DIFF ", n, text, MOCK, "found" if ok else "MISSING " + repr(miss)))
        elif kind == "dx":
            ok = ("`" + text + "`") in rd(DXF) or ('"' + text + '"') in rd(DXF)
            lines.append("%s COPY-%d %s :: %s :: %s" % ("EQUAL" if ok else "DIFF ", n, text, DXF, "the backticked proposed string is in the table" if ok else "NOT FOUND"))
        elif kind == "g":
            ok = text == gtable(repo)[src]
            lines.append("%s COPY-%d (COPY-G%d) %s :: %s" % ("EQUAL" if ok else "DIFF ", n, src, text, GM))
        elif kind == "draft":
            ok = True
            lines.append("DRAFT COPY-%d %s :: no source text; drafted by DOC for operator review; codes %s exist in src/CfdWorkbench.Analysis" % (n, text, ", ".join(src)))
            for c in src:
                if not any(c in p.read_text(encoding="utf-8") for p in (repo / "src/CfdWorkbench.Analysis").rglob("*.cs")):
                    ok = False
                    lines.append("DIFF  code %s not found in src" % c)
        else:
            ok = True
            lines.append("HYD   COPY-%d %s :: hydrodynamicist verdict relayed by the Coordinator (a message; no file to compare)" % (n, text))
        if not ok:
            bad += 1
    lines.append("rows %d, mismatches %d" % (len(lines), bad))
    pathlib.Path(out).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("rows", len(resolve(repo)), "mismatches", bad)
    return bad


def apply(repo):
    p = repo / "DESIGN.md"
    s = p.read_text(encoding="utf-8")
    anchor = "| COPY-249 | Use <value> — approved — Ruling 96 (action on a refused typed chord, never applied by itself; the Ruling's \"Use <min>\") |\n"
    assert s.count(anchor) == 1
    new = "".join(line(r) + "\n" for r in resolve(repo))
    p.write_text(s.replace(anchor, anchor + new), encoding="utf-8")


if __name__ == "__main__":
    cmd, repo = sys.argv[1], pathlib.Path(sys.argv[2])
    if cmd == "compare":
        sys.exit(1 if compare(repo, sys.argv[3]) else 0)
    elif cmd == "apply":
        apply(repo)
    elif cmd == "map":
        for r in resolve(repo):
            print(r[0], r[1], r[2][:60])
