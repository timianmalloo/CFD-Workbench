"""Compare Python NeuralFoil on fitted NACA 0012 against local XFOIL 6.99."""

import csv
import math
from pathlib import Path
import re

import aerosandbox as asb
import neuralfoil as nf


ROOT = Path(__file__).resolve().parent
PARAMETERS = asb.Airfoil("naca0012").normalize().to_kulfan_airfoil(n_weights_per_side=8).kulfan_parameters
MODEL = "xxxlarge"
all_rows = []
summary = []

for reynolds in (200000, 1000000):
    for ncrit in (2, 4, 9):
        path = ROOT / "data" / f"polar-re{reynolds}-n{ncrit}.txt"
        polar_text = path.read_text()
        assert "XFOIL         Version 6.99" in polar_text
        assert "Calculated polar for: NACA 0012" in polar_text
        header = next(line for line in polar_text.splitlines() if "Re =" in line and "Ncrit =" in line)
        match = re.search(r"Re =\s*([0-9.]+)\s+e\s+(\d+)\s+Ncrit =\s*([0-9.]+)\s+([0-9.]+)", header)
        assert match, header
        assert float(match[1]) * 10 ** int(match[2]) == reynolds
        assert float(match[3]) == float(match[4]) == ncrit
        polar = []
        for line in polar_text.splitlines():
            fields = line.split()
            if len(fields) == 9:
                try:
                    values = [float(field) for field in fields]
                except ValueError:
                    continue
                polar.append(values)
        assert len(polar) == 13, (path, len(polar))
        assert [int(row[0]) for row in polar] == list(range(-6, 7))
        pair_rows = []
        for row in polar:
            alpha, xfoil_cl, xfoil_cd = row[:3]
            aero = nf.get_aero_from_kulfan_parameters(
                PARAMETERS, alpha, reynolds, n_crit=ncrit, model_size=MODEL
            )
            nf_cl, nf_cd = float(aero["CL"][0]), float(aero["CD"][0])
            cl_error = nf_cl - xfoil_cl
            ln_cd_error = math.log(nf_cd / xfoil_cd)
            pair_rows.append((reynolds, ncrit, alpha, xfoil_cl, xfoil_cd, nf_cl, nf_cd,
                              cl_error, ln_cd_error, int(abs(cl_error) <= 0.02),
                              int(abs(ln_cd_error) <= 0.03)))
        all_rows.extend(pair_rows)
        worst_cl = max(pair_rows, key=lambda value: abs(value[7]))
        worst_cd = max(pair_rows, key=lambda value: abs(value[8]))
        summary.append((reynolds, ncrit, len(pair_rows),
                        sum(value[9] for value in pair_rows), sum(value[10] for value in pair_rows),
                        worst_cl[2], worst_cl[7], worst_cd[2], worst_cd[8]))

with (ROOT / "accuracy.tsv").open("w", newline="") as stream:
    writer = csv.writer(stream, delimiter="\t", lineterminator="\n")
    writer.writerow(("Re", "Ncrit", "alpha_deg", "xfoil_CL", "xfoil_CD", "neuralfoil_CL",
                     "neuralfoil_CD", "CL_error", "ln_CD_error", "CL_pass", "ln_CD_pass"))
    writer.writerows(all_rows)
with (ROOT / "accuracy-summary.tsv").open("w", newline="") as stream:
    writer = csv.writer(stream, delimiter="\t", lineterminator="\n")
    writer.writerow(("Re", "Ncrit", "points", "CL_pass_count", "ln_CD_pass_count",
                     "worst_CL_alpha", "worst_CL_error", "worst_ln_CD_alpha", "worst_ln_CD_error"))
    writer.writerows(summary)
for row in summary:
    print(f"Re={row[0]} Ncrit={row[1]} CL_pass={row[3]}/{row[2]} lnCD_pass={row[4]}/{row[2]} "
          f"worst_CL={row[6]:+.6f}@{row[5]:+g} worst_lnCD={row[8]:+.6f}@{row[7]:+g}")
