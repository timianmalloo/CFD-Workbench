#!/usr/bin/env python3
"""Gate NeuralFoil's bundled notice, pinned hashes, and the XFOIL proof allowlist.

Readiness ring; expected cost < 0.2 s locally (not yet measured).
"""

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]
PREFIX = "docs/proof/spike-ana-1/xfoil/"
ALLOWED = {PREFIX + ".gitignore", PREFIX + "build.sh"}


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument("--notice", type=Path, default=ROOT / "THIRD-PARTY-NOTICES.md")
    parser.add_argument("--tracked-list", type=Path)
    parser.add_argument("--network-source", type=Path,
                        default=ROOT / "src/CfdWorkbench.Analysis/NeuralFoil/NeuralFoilNetwork.cs")
    args = parser.parse_args()
    notice = args.notice.read_text()
    required = (
        "NeuralFoil 0.3.2", "xxxlarge", "https://github.com/peterdsharpe/NeuralFoil",
        "XFOIL-generated", "XFOIL itself is not shipped", "MIT License",
        "Copyright (c) 2023 Peter Sharpe", "Permission is hereby granted, free of charge",
        "The above copyright notice and this permission notice shall be included",
        "THE SOFTWARE IS PROVIDED \"AS IS\"", "IN NO EVENT SHALL THE",
    )
    missing = [text for text in required if text not in notice]
    manifest = json.loads((ROOT / "docs/proof/spike-ana-1/weights-manifest.json").read_text())
    network = args.network_source.read_text()
    for field in ("wheel_sha256", "source_sha256", "distribution_sha256", "binary_sha256"):
        value = manifest.get(field)
        if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
            missing.append(field)
    constants = {"source_sha256": "SourceSha256", "distribution_sha256": "DistributionSha256",
                 "binary_sha256": "WeightsSha256"}
    for field, constant in constants.items():
        value = manifest.get(field)
        declaration = rf'\bconst\s+string\s+{constant}\s*=\s*"{re.escape(value or "")}"'
        if value and re.search(declaration, network) is None:
            missing.append("C# constant " + field)
    if args.tracked_list:
        tracked = args.tracked_list.read_text().splitlines()
    else:
        result = subprocess.run(["git", "ls-files", "-z", "--", PREFIX], cwd=ROOT, check=True,
                                capture_output=True)
        tracked = [entry.decode() for entry in result.stdout.split(b"\0") if entry]
    forbidden = [path for path in tracked if path.startswith(PREFIX) and path not in ALLOWED]
    for path in forbidden:
        missing.append("tracked XFOIL path " + path)
    if missing:
        for item in missing:
            print("FAIL NeuralFoil notice: " + item, file=sys.stderr)
        return 1
    print("PASS NeuralFoil notice, four hashes, and XFOIL tracked-path allowlist")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(run())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print("FAIL NeuralFoil notice gate: " + str(error), file=sys.stderr)
        sys.exit(1)
