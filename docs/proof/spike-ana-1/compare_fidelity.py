"""Compare every output of the Python and C# probe on the same 90 CST cases."""

import csv
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parent


def read(path):
    with path.open(newline="") as stream:
        return list(csv.DictReader(stream, delimiter="\t"))


python = read(ROOT / "python.tsv")
csharp = read(ROOT / "csharp.tsv")
assert len(python) == len(csharp) == 90
assert list(python[0]) == list(csharp[0])
assert [row["case"] for row in python] == [row["case"] for row in csharp]
rows = []
for output in list(python[0])[1:]:
    differences = []
    for py, cs in zip(python, csharp):
        expected, actual = float(py[output]), float(cs[output])
        assert math.isfinite(expected) and math.isfinite(actual), (py["case"], output)
        absolute = abs(actual - expected)
        relative = absolute / max(abs(expected), 1e-12)
        differences.append((absolute, relative, py["case"]))
    abs_worst = max(differences, key=lambda entry: entry[0])
    rel_worst = max(differences, key=lambda entry: entry[1])
    rows.append((output, abs_worst[0], rel_worst[1], abs_worst[2], rel_worst[2]))
with (ROOT / "fidelity.tsv").open("w", newline="") as stream:
    writer = csv.writer(stream, delimiter="\t", lineterminator="\n")
    writer.writerow(("output", "max_abs", "max_relative_floor_1e-12", "abs_worst_case", "relative_worst_case"))
    writer.writerows(rows)
worst = max(rows, key=lambda row: row[1])
print(f"outputs={len(rows)} cases={len(python)} max_abs={worst[1]:.9g} at {worst[0]} {worst[3]}")
print(f"max_relative_floor_1e-12={max(row[2] for row in rows):.9g}")
assert worst[1] <= 1e-6, f"port fidelity exceeds 1e-6: {worst}"
