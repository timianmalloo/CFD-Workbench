#!/usr/bin/env python3
"""Read-only: does the B2 amd64 package lock resolve, version for version, on arm64?

For every row of the frozen 135-row B2 lock (and the F1 extra roots), look the same package name up in the
arm64 binary indices of the repositories the re-scoped packet would use: Ubuntu ports (noble, noble-updates,
noble-security; main and universe), NVIDIA ubuntu2404/sbsa, and dl.openfoam.com noble. Record whether the
locked version exists for arm64, which versions do, and the arm64 filename, size and SHA-256 the index
publishes. Nothing is installed; the indices are read over HTTP from the Windows host into a scratch
directory given on the command line. The indices are not signature-verified here (the packet's
``repositories`` stage does that with gpgv); this probe only answers the version question.

Exit 0 when every locked version exists for arm64 (or as ``all``), 3 when some do not, 2 on a fetch error.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import sys
import urllib.request
from pathlib import Path

PORTS = "http://ports.ubuntu.com/ubuntu-ports"
NVIDIA_SBSA = "https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa"
OPENFOAM = "https://dl.openfoam.com/repos/deb"
INDICES = [
    *(f"{PORTS}/dists/{suite}/{component}/binary-arm64/Packages.gz"
      for suite in ("noble", "noble-updates", "noble-security") for component in ("main", "universe")),
    f"{NVIDIA_SBSA}/Packages",
    f"{OPENFOAM}/dists/noble/main/binary-arm64/Packages",
]
EXTRA_ROOTS = {
    "openfoam2512": "2512.0-2", "openfoam2512-common": "2512.0-2", "openfoam2512-dev": "2512.0-2",
    "openfoam2512-source": "2512.0-2", "openfoam2512-tools": "2512.0-2",
    "gcc-13": None, "g++-13": None, "gfortran-13": None, "make": None, "openmpi-bin": None,
    "libopenmpi-dev": None, "libblas-dev": None, "liblapack-dev": None, "pkg-config": None,
}


def fetch(url: str, cache: Path) -> tuple[bytes, str]:
    name = url.replace("://", "_").replace("/", "_")
    target = cache / name
    if not target.exists():
        with urllib.request.urlopen(url, timeout=120) as response:
            target.write_bytes(response.read())
    raw = target.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    return (gzip.decompress(raw) if url.endswith(".gz") else raw), digest


def paragraphs(text: str) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for block in text.split("\n\n"):
        row: dict[str, str] = {}
        key = ""
        for line in block.splitlines():
            if line[:1].isspace() and key:
                row[key] += " " + line.strip()
            else:
                key, sep, value = line.partition(":")
                if sep:
                    row[key] = value.strip()
        if row.get("Package"):
            rows.append(row)
    return rows


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lock", type=Path, required=True)
    parser.add_argument("--cache", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.cache.mkdir(parents=True, exist_ok=True)
    lock = json.loads(args.lock.read_text(encoding="utf-8"))
    available: dict[str, dict[str, dict]] = {}
    indices = []
    try:
        for url in INDICES:
            text, digest = fetch(url, args.cache)
            rows = paragraphs(text.decode("utf-8", "replace"))
            indices.append({"url": url, "sha256": digest, "entries": len(rows)})
            for row in rows:
                available.setdefault(row["Package"], {})[row["Version"]] = {
                    "architecture": row.get("Architecture"), "filename": row.get("Filename"),
                    "size": int(row["Size"]) if row.get("Size", "").isdigit() else None,
                    "sha256": row.get("SHA256"), "index": url}
    except OSError as error:
        print(f"fetch failed: {error}", file=sys.stderr)
        return 2
    wanted = [(p["package"], p["version"], "b2-lock") for p in lock["packages"]]
    wanted += [(name, version, "f1-extra-root") for name, version in EXTRA_ROOTS.items()]
    results, same, differs, missing = [], 0, [], []
    for name, version, origin in wanted:
        versions = available.get(name, {})
        found = version in versions if version else bool(versions)
        entry = {"package": name, "locked_version": version, "origin": origin, "arm64_versions": sorted(versions),
                 "locked_version_on_arm64": found}
        if found:
            same += 1
            entry["arm64"] = versions[version] if version else versions[sorted(versions)[-1]]
        elif versions:
            differs.append(name)
        else:
            missing.append(name)
        results.append(entry)
    summary = {"schema": "cfdw-arm64-lock-resolution/1", "method": "unsigned HTTP read of the arm64 binary indices from the Windows host; nothing installed; indices listed with their SHA-256",
               "lock": {"path": args.lock.as_posix().split("docs/")[-1], "package_count": len(lock["packages"]), "sha256": hashlib.sha256(args.lock.read_bytes()).hexdigest()},
               "indices": indices,
               "totals": {"checked": len(wanted), "locked_version_exists_on_arm64": same,
                          "package_exists_but_locked_version_absent": differs, "package_absent": missing},
               "rows": results}
    args.output.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    print(f"checked={len(wanted)} same_version={same} version_differs={len(differs)} absent={len(missing)}")
    for name in differs:
        print(f"  version differs: {name} locked={dict(wanted_lookup(wanted))[name]} arm64={sorted(available[name])}")
    for name in missing:
        print(f"  absent on arm64: {name}")
    return 0 if not differs and not missing else 3


def wanted_lookup(wanted):
    return [(name, version) for name, version, _ in wanted]


if __name__ == "__main__":
    sys.exit(main())
