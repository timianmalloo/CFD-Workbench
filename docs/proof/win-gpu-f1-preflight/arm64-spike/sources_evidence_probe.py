#!/usr/bin/env python3
"""Read-only receipt for the Ruling 237 re-scope plan: the Ubuntu noble WSL rootfs checksums per architecture, and
the current amd64 ``noble-updates`` main index's versions of ``linux-libc-dev`` (the B2 lock pins 6.8.0-146.146).

Reads two public files over HTTP into the scratch directory given on the command line, hashes them, and writes one
JSON receipt. The SHA256SUMS signature is not verified here (the packet's rootfs verifier does that at run time with
the cloud-image keyring); this receipt records what the index said on the day it was read.
"""
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import sys
import urllib.request
from pathlib import Path

SUMS_URL = "https://cloud-images.ubuntu.com/wsl/releases/noble/current/SHA256SUMS"
AMD64_INDEX_URL = "http://archive.ubuntu.com/ubuntu/dists/noble-updates/main/binary-amd64/Packages.gz"
B2_LOCKED_LINUX_LIBC_DEV = "6.8.0-146.146"


def fetch(url: str, cache: Path) -> bytes:
    target = cache / url.replace("://", "_").replace("/", "_")
    if not target.exists():
        with urllib.request.urlopen(url, timeout=120) as response:
            target.write_bytes(response.read())
    return target.read_bytes()


def versions_of(index_text: str, package: str) -> list[str]:
    found = []
    for block in index_text.split("\n\n"):
        row = {}
        for line in block.splitlines():
            key, sep, value = line.partition(":")
            if sep and not line[:1].isspace():
                row[key] = value.strip()
        if row.get("Package") == package:
            found.append(row["Version"])
    return sorted(found)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cache", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.cache.mkdir(parents=True, exist_ok=True)
    try:
        sums_raw = fetch(SUMS_URL, args.cache)
        index_raw = fetch(AMD64_INDEX_URL, args.cache)
    except OSError as error:
        print(f"fetch failed: {error}", file=sys.stderr)
        return 2
    sums = {}
    for line in sums_raw.decode("utf-8").splitlines():
        digest, _, name = line.partition("  ")
        if name:
            sums[name] = digest
    amd64_versions = versions_of(gzip.decompress(index_raw).decode("utf-8", "replace"), "linux-libc-dev")
    receipt = {
        "schema": "cfdw-arm64-spike-sources-evidence/1",
        "method": "HTTP GET from the Windows host into a scratch directory; nothing installed; the SHA256SUMS signature is checked by the packet at run time, not here",
        "ubuntu_wsl_rootfs": {"url": SUMS_URL, "sha256sums_file_sha256": hashlib.sha256(sums_raw).hexdigest(),
                              "entries": sums,
                              "packet_pin_amd64": "ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz",
                              "arm64_counterpart": "ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz"},
        "amd64_noble_updates_main": {"url": AMD64_INDEX_URL, "index_sha256": hashlib.sha256(index_raw).hexdigest(),
                                     "linux_libc_dev_versions": amd64_versions,
                                     "b2_locked_version": B2_LOCKED_LINUX_LIBC_DEV,
                                     "b2_locked_version_present": B2_LOCKED_LINUX_LIBC_DEV in amd64_versions},
    }
    args.output.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    print(f"rootfs entries={len(sums)} amd64 linux-libc-dev={amd64_versions} locked_present={B2_LOCKED_LINUX_LIBC_DEV in amd64_versions}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
