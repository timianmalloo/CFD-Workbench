#!/usr/bin/env python3
"""Apply the pre-registered S2 rules to sweep.csv. Prints a per-(r, alpha) table. No fitting."""
import csv, collections
rows = list(csv.DictReader(open(__file__.rsplit("/", 1)[0] + "/sweep.csv")))
g = collections.defaultdict(list)
for x in rows:
    g[(float(x["r"]), float(x["alpha"]))].append(x)
print("r,alpha,flip,k2_by_n,k3_by_n,anyout_by_n,k1_by_n,k1_alpha_eff_n32..256,k2_alpha_eff,k3_alpha_eff,edge_tip_chords_by_n,edge_eta_n256")
for (r, a), v in sorted(g.items()):
    v.sort(key=lambda x: int(x["n"]))
    t = lambda k: "/".join(x[k].split(":")[0] for x in v)
    sig = {(x["verdict_k2"].split(":")[0], x["verdict_k3"].split(":")[0], x["any_judged_out"]) for x in v}
    ae = lambda k: "/".join("%.2f" % float(x[k]) for x in v)
    print(",".join([str(r), str(int(a)), "FLIP" if len(sig) > 1 else "no", t("verdict_k2"), t("verdict_k3"), "/".join(x["any_judged_out"] for x in v),
                    t("verdict_k1"), ae("alpha_eff_k1"), ae("alpha_eff_k2"), ae("alpha_eff_k3"),
                    "/".join(x["edge_tip_chords"] for x in v), v[-1]["edge_eta"][:8]]))
