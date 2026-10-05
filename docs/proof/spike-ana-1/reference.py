"""Recreate NeuralFoil 0.3.2 xxxlarge probe inputs and a simple weight file.

Run with ~/dev/sim/.venv/bin/python from the worktree root. No system install.
"""

import csv
import hashlib
import importlib.metadata
import json
from pathlib import Path
import struct

import aerosandbox as asb
import numpy as np
import neuralfoil as nf


ROOT = Path(__file__).resolve().parent
WEIGHTS = Path(nf.__file__).parent / "nn_weights_and_biases"
MODEL = "xxxlarge"
FOILS = ("naca0012", "naca2412", "naca4412")
EXPECTED_MODEL_SHA256 = "94638c04bca3c303515cf0c2944e2b09f587818cbd2860183bd85f2fbeeef428"
EXPECTED_DISTRIBUTION_SHA256 = "63a33149c902ad01ecf537dd2d127d9e7ffbf86527893f4dc76f25f7087a3573"
OUTPUTS = (
    ["analysis_confidence", "CL", "CD", "CM", "Top_Xtr", "Bot_Xtr"]
    + [f"upper_bl_theta_{i}" for i in range(32)]
    + [f"upper_bl_H_{i}" for i in range(32)]
    + [f"upper_bl_ue/vinf_{i}" for i in range(32)]
    + [f"lower_bl_theta_{i}" for i in range(32)]
    + [f"lower_bl_H_{i}" for i in range(32)]
    + [f"lower_bl_ue/vinf_{i}" for i in range(32)]
)


def write_tsv(path, header, rows):
    with path.open("w", newline="") as stream:
        writer = csv.writer(stream, delimiter="\t", lineterminator="\n")
        writer.writerow(header)
        writer.writerows(rows)


def convert_weights():
    source = WEIGHTS / f"nn-{MODEL}.npz"
    distribution = WEIGHTS / "scaled_input_distribution.npz"
    assert importlib.metadata.version("neuralfoil") == "0.3.2"
    assert importlib.metadata.version("aerosandbox") == "4.2.9"
    assert hashlib.sha256(source.read_bytes()).hexdigest() == EXPECTED_MODEL_SHA256
    assert hashlib.sha256(distribution.read_bytes()).hexdigest() == EXPECTED_DISTRIBUTION_SHA256
    with np.load(source) as archive, np.load(distribution) as stats:
        indices = sorted(int(key.split(".")[1]) for key in archive.files if key.endswith(".weight"))
        with (ROOT / "probe" / "weights.bin").open("wb") as stream:
            stream.write(b"NFPRB1\0\0")
            stream.write(struct.pack("<i", len(indices)))
            for index in indices:
                weight = archive[f"net.{index}.weight"]
                bias = archive[f"net.{index}.bias"]
                assert weight.dtype == np.float32 and bias.dtype == np.float32
                stream.write(struct.pack("<ii", *weight.shape))
                stream.write(weight.astype("<f4").tobytes(order="C"))
                stream.write(bias.astype("<f4").tobytes(order="C"))
            mean = stats["mean_inputs_scaled"]
            inverse = stats["inv_cov_inputs_scaled"]
            assert mean.shape == (25,) and mean.dtype == np.float32
            assert inverse.shape == (25, 25) and inverse.dtype == np.float64
            stream.write(mean.astype("<f4").tobytes(order="C"))
            stream.write(inverse.astype("<f8").tobytes(order="C"))
    metadata = {
        "model": MODEL,
        "source_npz": source.name,
        "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "distribution_npz": distribution.name,
        "distribution_sha256": hashlib.sha256(distribution.read_bytes()).hexdigest(),
        "binary_sha256": hashlib.sha256((ROOT / "probe" / "weights.bin").read_bytes()).hexdigest(),
        "binary_bytes": (ROOT / "probe" / "weights.bin").stat().st_size,
        "format": "NFPRB1, little-endian int32 layer count; each layer int32 rows, int32 columns, float32 row-major weights and biases; float32[25] mean, float64[25,25] inverse covariance",
        "layers": indices,
    }
    (ROOT / "weights-manifest.json").write_text(json.dumps(metadata, indent=2) + "\n")


def generate_cases():
    cases = []
    reference = []
    residuals = []
    for name in FOILS:
        foil = asb.Airfoil(name).normalize()
        kulfan = foil.to_kulfan_airfoil(n_weights_per_side=8)
        parameters = kulfan.kulfan_parameters
        # Match the default 200-point-per-side fitting grid and report ordinate error.
        x = 0.5 * (1 - np.cos(np.linspace(0, np.pi, 200)))
        target_upper = foil.local_camber(x) + foil.local_thickness(x) / 2
        target_lower = foil.local_camber(x) - foil.local_thickness(x) / 2
        errors = np.concatenate((
            kulfan.upper_coordinates(x)[:, 1] - target_upper,
            kulfan.lower_coordinates(x)[:, 1] - target_lower,
        ))
        residuals.append((name, len(errors), float(np.sqrt(np.mean(errors**2))), float(np.max(np.abs(errors)))))
        for alpha in (-6, -3, 0, 3, 6):
            for reynolds in (2e5, 1e6):
                for ncrit in (2, 4, 9):
                    label = f"{name}:a{alpha}:re{int(reynolds)}:n{ncrit}"
                    row = [label, name, alpha, int(reynolds), ncrit]
                    row += [float(value) for value in parameters["upper_weights"]]
                    row += [float(value) for value in parameters["lower_weights"]]
                    row += [float(parameters["leading_edge_weight"]), float(parameters["TE_thickness"])]
                    cases.append(row)
                    aero = nf.get_aero_from_kulfan_parameters(
                        parameters, alpha, reynolds, n_crit=ncrit, model_size=MODEL
                    )
                    reference.append([label] + [float(aero[key][0]) for key in OUTPUTS])
    write_tsv(ROOT / "cases.tsv", ["case", "foil", "alpha", "Re", "Ncrit"] +
              [f"upper_{i}" for i in range(8)] + [f"lower_{i}" for i in range(8)] +
              ["leading_edge_weight", "TE_thickness"], cases)
    write_tsv(ROOT / "python.tsv", ["case"] + list(OUTPUTS), reference)
    write_tsv(ROOT / "cst-residual.tsv", ["foil", "sample_count", "rms_y_over_c", "max_abs_y_over_c"], residuals)


if __name__ == "__main__":
    convert_weights()
    generate_cases()
    print(f"Generated {len(FOILS) * 5 * 2 * 3} cases and {len(OUTPUTS)} outputs; model {MODEL}")
