#!/usr/bin/env python3
"""Read the pinned wheel, repeat SPIKE-ANA-1/reference.py's converter, and check the shipped resource.

Readiness ring; expected warm-cache cost < 1 s, network first-run cost not recorded.
No installed NeuralFoil, NumPy, Python runtime, CasADi, or IPOPT is used by the product.
"""

import argparse
import ast
import hashlib
import io
import json
from pathlib import Path
import struct
import sys
import urllib.error
import urllib.request
import zipfile


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/proof/spike-ana-1/weights-manifest.json"
RESOURCE = ROOT / "src/CfdWorkbench.Analysis/NeuralFoil/weights.bin"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def npy(archive, path, shape, dtype):
    """Read only the two exact NumPy dtypes and C-order shapes used by the spike converter."""
    with zipfile.ZipFile(io.BytesIO(archive)) as inner:
        data = inner.read(path + ".npy")
    if data[:6] != b"\x93NUMPY" or data[6:8] not in (b"\x01\x00", b"\x02\x00"):
        raise ValueError(f"invalid npy header: {path}")
    width = 2 if data[6] == 1 else 4
    length = int.from_bytes(data[8:8 + width], "little")
    header = ast.literal_eval(data[8 + width:8 + width + length].decode("latin1"))
    if header["descr"] != dtype or tuple(header["shape"]) != tuple(shape) or header["fortran_order"]:
        raise ValueError(f"unexpected array contract: {path}")
    payload = data[8 + width + length:]
    item_bytes = 4 if dtype == "<f4" else 8
    expected = item_bytes
    for dimension in shape:
        expected *= dimension
    if len(payload) != expected:
        raise ValueError(f"invalid array size: {path}")
    return payload


def convert(model, distribution, layers):
    # Same ordered arrays and little-endian header as reference.py:convert_weights.
    output = bytearray(b"NFPRB1\0\0" + struct.pack("<i", len(layers)))
    shape = [25, 512, 512, 512, 512, 512, 512, 198]
    for slot, layer in enumerate(layers):
        rows, columns = shape[slot + 1], shape[slot]
        output += struct.pack("<ii", rows, columns)
        output += npy(model, f"net.{layer}.weight", (rows, columns), "<f4")
        output += npy(model, f"net.{layer}.bias", (rows,), "<f4")
    output += npy(distribution, "mean_inputs_scaled", (25,), "<f4")
    output += npy(distribution, "inv_cov_inputs_scaled", (25, 25), "<f8")
    return output


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument("--offline", action="store_true")
    parser.add_argument("--cache-dir", type=Path, default=Path.home() / ".cache" / "cfd-workbench")
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    url = manifest["wheel_url"]
    cache = args.cache_dir / Path(url).name
    if cache.is_file():
        wheel = cache.read_bytes()
    elif args.offline:
        print("NOT RECORDED NeuralFoil wheel unavailable offline")
        return 2
    else:
        try:
            with urllib.request.urlopen(url, timeout=30) as response:
                wheel = response.read()
        except (OSError, urllib.error.URLError) as error:
            print(f"NOT RECORDED NeuralFoil wheel download unavailable: {error}")
            return 2
        args.cache_dir.mkdir(parents=True, exist_ok=True)
        cache.write_bytes(wheel)
    if digest(wheel) != manifest["wheel_sha256"]:
        raise ValueError("pinned NeuralFoil wheel SHA-256 mismatch")
    prefix = "neuralfoil/nn_weights_and_biases/"
    with zipfile.ZipFile(io.BytesIO(wheel)) as archive:
        model = archive.read(prefix + manifest["source_npz"])
        distribution = archive.read(prefix + manifest["distribution_npz"])
    if digest(model) != manifest["source_sha256"] or digest(distribution) != manifest["distribution_sha256"]:
        raise ValueError("upstream NeuralFoil npz SHA-256 mismatch")
    converted = convert(model, distribution, manifest["layers"])
    committed = RESOURCE.read_bytes()
    if len(converted) != manifest["binary_bytes"] or digest(converted) != manifest["binary_sha256"]:
        raise ValueError("reconverted NeuralFoil weights differ from the manifest")
    if committed != converted:
        raise ValueError("embedded NeuralFoil weights differ from the pinned wheel conversion")
    print(f"PASS NeuralFoil wheel {manifest['wheel_sha256']} converted {digest(converted)} {len(converted)} bytes")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(run())
    except (OSError, KeyError, ValueError, zipfile.BadZipFile) as failure:
        print(f"FAIL NeuralFoil weights: {failure}", file=sys.stderr)
        sys.exit(1)
