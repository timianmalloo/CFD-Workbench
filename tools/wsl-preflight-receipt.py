#!/usr/bin/env python3
"""wsl-preflight-receipt.py - fold the working directory of tools/wsl-preflight.sh into one JSON receipt.

    python3 tools/wsl-preflight-receipt.py --work <dir> --out <receipt.json>

Stdlib only. Every field degrades to "not recorded", never to a plausible number.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

INST = re.compile(r"^Inst (\S+) \((\S+) (.*?) \[(\S+)\]\)")
URI = re.compile(r"^'(\S+)' (\S+) (\d+) ")
VALIDSIG = re.compile(r"^\[GNUPG:\] VALIDSIG ([0-9A-F]+) ")


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace").strip() if path.exists() else "not recorded"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--work", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    work = Path(args.work)

    resolved = []
    for line in (work / "simulate.log").read_text(encoding="utf-8", errors="replace").splitlines() if (work / "simulate.log").exists() else []:
        match = INST.match(line)
        if match:
            resolved.append({"package": match.group(1), "version": match.group(2),
                             "origin": match.group(3), "architecture": match.group(4)})

    uris = []
    for line in (work / "uris.log").read_text(encoding="utf-8", errors="replace").splitlines() if (work / "uris.log").exists() else []:
        match = URI.match(line)
        if match:
            uris.append({"uri": match.group(1), "file": match.group(2), "bytes": int(match.group(3))})

    indices = {}
    for status in sorted((work / "gpgv").glob("*.txt")) if (work / "gpgv").exists() else []:
        signers = [m.group(1) for m in (VALIDSIG.match(l) for l in status.read_text(encoding="utf-8", errors="replace").splitlines()) if m]
        indices[status.name[:-4]] = {"valid_signatures": signers, "verified": bool(signers)}

    debs = []
    if (work / "debs.sha256").exists():
        for line in (work / "debs.sha256").read_text(encoding="utf-8").splitlines():
            sha, _, name = line.partition("  ")
            if sha and name:
                size = (work / "debs" / name).stat().st_size if (work / "debs" / name).exists() else None
                debs.append({"file": name.lstrip("./"), "sha256": sha, "bytes": size})

    keyrings = {}
    if (work / "keyrings.sha256").exists():
        for line in (work / "keyrings.sha256").read_text(encoding="utf-8").splitlines():
            sha, _, name = line.partition("  ")
            if sha and name:
                keyrings[Path(name).name] = sha

    receipt = {
        "schema": "cfdw-wsl-preflight/1",
        "result": text(work / "result.txt"),
        "last_step": text(work / "step.txt"),
        "started_utc": text(work / "started.txt"),
        "seconds": int(text(work / "seconds.txt")) if text(work / "seconds.txt").isdigit() else "not recorded",
        "architecture": text(work / "architecture.txt"),
        "uname_m": text(work / "uname.txt"),
        "gpu": text(work / "gpu.txt"),
        "keyrings_sha256": keyrings or "not recorded",
        "sources": text(work / "sources.list").splitlines() if (work / "sources.list").exists() else "not recorded",
        "roots": text(work / "roots.txt").splitlines() if (work / "roots.txt").exists() else "not recorded",
        "indices": indices or "not recorded",
        "resolved": resolved or "not recorded",
        "resolved_count": len(resolved),
        "download_bytes": sum(u["bytes"] for u in uris) if uris else "not recorded",
        "download_files": len(uris) if uris else "not recorded",
        "downloaded": debs or "not performed",
        "downloaded_bytes": sum(d["bytes"] or 0 for d in debs) if debs else "not performed",
    }
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
