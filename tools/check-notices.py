#!/usr/bin/env python3
"""Gate NeuralFoil's bundled notice, pinned hashes, and the XFOIL proof allowlist.

Readiness ring; expected cost < 0.2 s locally (not yet measured).
The XFOIL path allowlist reads tracked plus untracked, non-ignored files (GATE-BEFORE-ADD).
"""

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

from gate_files import worktree_files


for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ROOT = Path(__file__).resolve().parents[1]
PREFIX = "docs/proof/spike-ana-1/xfoil/"
ALLOWED = {PREFIX + ".gitignore", PREFIX + "build.sh"}


def self_test():
    with tempfile.TemporaryDirectory() as tmp:  # GATE-BEFORE-ADD: a new file not yet `git add`ed
        subprocess.run(["git", "init", "-q"], cwd=tmp, check=True)
        (Path(tmp) / PREFIX).mkdir(parents=True)
        (Path(tmp) / PREFIX / "xfoil.f").write_text("x\n", encoding="utf-8", newline="\n")
        assert worktree_files(tmp, PREFIX) == [PREFIX + "xfoil.f"], "untracked XFOIL path not seen"
    print("check-notices self-test OK", flush=True)
    return 0


def run():
    if "--self-test" in sys.argv[1:]:
        return self_test()
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
        tracked = worktree_files(ROOT, PREFIX)
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
