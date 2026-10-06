#!/usr/bin/env python3
"""Ruling 116 batch: usage r116.py apply|compare <repo> [out]"""
import pathlib, re, sys

repo = pathlib.Path(sys.argv[2])
RUL = "docs/notes/rulings.md"
GM = "docs/design/group-move-node-m.md"
R = "Ruling 116"
FIRST = 357

# (text, note, source file, [fragments that must occur in it])
rows = []


def add(text, note, src, frags=None, status=R):
    rows.append((text, note, src, frags if frags is not None else [text], status))


# part (2): Cp unavailable
add("Unavailable — no accepted section profile at this station", "Cp unavailable, no accepted profile", RUL)
add("Unavailable — the panel solve failed at this station. Evaluate again.", "Cp unavailable, panel solve failed", RUL)
# cavitation labels
add("σ (cavitation number)", "cavitation row label", RUL)
add("−Cp_min", "cavitation row label", RUL, ['"−Cp_min"'])
add("V_crit (inception speed)", "cavitation row label", RUL)
# outside-bracket variants (the ruling's \"…\" is \"Outside the validated bracket\", dx-screen-states row 36)
add("Outside the validated bracket — Re <Re> is beyond <min> to <max>. Computed, not validated.", "bracket flag, Re axis; prefix as in dx-screen-states row 36", RUL,
    ["— Re <Re> is beyond <min> to <max>. Computed, not validated."])
add("Outside the validated bracket — Ncrit <n> is beyond <min> to <max>. Computed, not validated.", "bracket flag, Ncrit axis", RUL,
    ["— Ncrit <n> is beyond <min> to <max>. Computed, not validated."])
add("Outside the validated bracket — <family> sections were not validated (NACA 0012 only). Computed, not validated.", "bracket flag, section family axis", RUL,
    ["— <family> sections were not validated (NACA 0012 only). Computed, not validated."])
add("Transition x_tr/c, upper and lower · Ncrit 2 and 4 · r<rev> · Re <Re>", "transition overlay legend (the menu \"Overlay a section ▾\" is COPY-327, no new row)", RUL)
add("σ required and V_crit against Cl · dot: this operating point", "cavitation bucket legend", RUL)
for t in ["Target CL", "Bracket", "Iterations", "Stopped because", "Basis", "Polar limit"]:
    add(t, "Find α result row label", RUL, ['"' + t + '"'])
for t in ["CL never reaches the target in the bracket", "the polar did not converge", "polar confidence is below the floor",
          "Re is outside the polar's range", "the foil is too shallow (h/c below <floor>)"]:
    add(t, "Find α no-root reason, the <reason> of COPY-%d" % 0, RUL, ['"' + t + '"'])
# part (4): CPY reason texts
add("Unavailable — e is undefined when CL or induced drag is zero.", "reason code ANA-OSWALD-UNDEFINED", RUL)
add("Unavailable — no centre of lift when total lift is zero.", "reason code ANA-CENTRE-OF-LIFT-UNDEFINED", RUL)
add("Unavailable — reference area is zero or missing.", "reason code ANA-REFERENCE-AREA-MISSING", RUL)
add("Unavailable — a force is not a finite number. Evaluate again.", "reason code ANA-FORCE-NOT-FINITE", RUL)
add("Unavailable — the polar gave no value for this row.", "reason code ANA-POLAR-VALUE-MISSING", RUL)
add("Unavailable — no verdict was stored for this run. Evaluate again.", "reason code ANA-VERDICT-MISSING", RUL)
# Ruling 113
add("Cp_min under-read not measured at this station (200 panels only)", "shows in the Section tab for any station not solved at 400; parallels COPY-312", RUL, status="Ruling 113")
# part (3): group move tokens (design 5a, as finalised by Ruling 116)
add("Moved <n> <curve> points. <end-chord> <value> mm.", "COPY-G2 tokenised; clause only on the leading or trailing rail and only when an end vertex moved; <end-chord> is Tip chord or Root chord; supersedes COPY-282", GM,
    ["Moved `<n>` `<curve>` points.` <end-chord> <value> mm.`"])
add("Root chord <value> mm", "new <end-chord> clause for the root (design 5a, Ruling 116 part 3)", RUL, ['"Root chord <value> mm"'])
add("The <point> can't move along the span, so the selection moves in <axis> only.", "COPY-G4 tokenised; supersedes COPY-284", GM,
    ["The `<point>` can't move along the span, so the selection moves in `<axis>` only."])
add("The <point> can't change, so the selection holds.", "COPY-G4 on a channel with no span freedom", GM,
    ["The `<point>` can't change, so the selection holds."])
add("Moving these points by <typed> <unit> would take the tip chord below <min>. The most they can move that way is <amount> <unit>.", "COPY-G8 tokenised, Plan only; supersedes COPY-288", GM,
    ["`<amount>` and `<typed>` carry `<unit>`; G8 is a Plan-only row"])
add("Moving these points by <typed> <unit> would pass point <n>. The most they can move that way is <amount> <unit>.", "COPY-G10 tokenised; supersedes COPY-290", GM,
    ["`<amount>` and `<typed>` carry `<unit>`"])
add("Set <axis> of <n> points to <value> <unit>.", "COPY-G11 tokenised; supersedes COPY-291", GM, ["Set `<axis>` of `<n>` points to `<value> <unit>`."])
add("Moved <n> points by <signed value> <unit> in <axis>.", "COPY-G12 with the axis; supersedes COPY-292", RUL)
add("A range shows 2 decimals like a value; a shared value is equal at the displayed precision; thickness % is the percentage of each point's local chord.",
    "finding 11, three rules (Ruling 116 part 3)", RUL,
    ["ranges show 2 decimals like values; a shared value is equal at the displayed precision; thickness % is % of each point's local chord"])

SUPERSEDE = {282: "Moved <n> <curve> points. <end-chord> <value> mm.", 284: "The <point> can't move along the span, so the selection moves in <axis> only.",
             288: "Moving these points by <typed> <unit> would take the tip chord", 290: "Moving these points by <typed> <unit> would pass point",
             291: "Set <axis> of <n> points to", 292: "Moved <n> points by <signed value> <unit> in <axis>."}


def numbered():
    out = [(FIRST + i,) + r for i, r in enumerate(rows)]
    return out


def row_line(r):
    n, text, note, src, frags, status = r
    if "COPY-0" in note:
        note = note.replace("COPY-0", "the Find α no-root row")
    return "| COPY-%d | %s — approved — %s (%s) |" % (n, text, status, note)


def apply():
    d = repo / "DESIGN.md"
    lines = d.read_text(encoding="utf-8").split("\n")
    for k, l in enumerate(lines):
        m = re.match(r"\| COPY-(\d+) \|", l)
        if m and 334 <= int(m.group(1)) <= 352:
            assert "proposed — awaiting operator" in l
            lines[k] = l.replace("proposed — awaiting operator", "approved — Ruling 116")
    nums = numbered()
    for old, key in SUPERSEDE.items():
        new = [r[0] for r in nums if r[1].startswith(key)][0]
        k = [i for i, l in enumerate(lines) if l.startswith("| COPY-%d |" % old)][0]
        assert lines[k].endswith(" |")
        lines[k] = lines[k][:-2] + "— superseded by COPY-%d — Ruling 116 |" % new
    k = [i for i, l in enumerate(lines) if l.startswith("| COPY-356 |")][0]
    lines[k + 1:k + 1] = [row_line(r) for r in nums]
    d.write_text("\n".join(lines), encoding="utf-8")
    print("applied", len(nums), "rows from COPY-%d to COPY-%d" % (nums[0][0], nums[-1][0]))


def compare(out):
    res, bad = [], 0
    for r in numbered():
        n, text, note, src, frags, status = r
        s = (repo / src).read_text(encoding="utf-8")
        miss = [f for f in frags if f not in s]
        bad += bool(miss)
        res.append("%s COPY-%d %s :: %s :: %s" % ("DIFF " if miss else "EQUAL", n, text, src, "found" if not miss else "MISSING " + repr(miss)))
    res.append("rows %d, mismatches %d" % (len(res), bad))
    pathlib.Path(out).write_text("\n".join(res) + "\n", encoding="utf-8")
    print(res[-1])
    return bad


if __name__ == "__main__":
    if sys.argv[1] == "apply":
        apply()
    else:
        sys.exit(1 if compare(sys.argv[3]) else 0)
