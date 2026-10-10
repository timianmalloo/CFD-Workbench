#!/usr/bin/env python3
"""Bind the signed NVIDIA InRelease to the retained Packages.gz bytes."""
from __future__ import annotations

import argparse
import hashlib
import re
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def signed_payload(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    marker = "\n-----BEGIN PGP SIGNATURE-----"
    if not text.startswith("-----BEGIN PGP SIGNED MESSAGE-----") or marker not in text:
        raise ValueError("InRelease is not a clear-signed message")
    header, body = text.split("\n\n", 1)
    if "Hash: SHA256" not in header:
        raise ValueError("InRelease does not declare SHA256")
    body = body.split(marker, 1)[0]
    return "\n".join(line[2:] if line.startswith("- ") else line for line in body.splitlines())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--inrelease", type=Path, required=True)
    parser.add_argument("--packages", type=Path, required=True)
    parser.add_argument("--ubuntu-sums", type=Path, required=True)
    parser.add_argument("--ubuntu-rootfs", type=Path, required=True)
    args = parser.parse_args()

    payload = signed_payload(args.inrelease)
    match = re.search(r"(?ms)^SHA256:\s*$.*?^\s*([0-9a-f]{64})\s+(\d+)\s+Packages\.gz\s*$", payload)
    if match is None:
        raise ValueError("signed SHA256 entry for Packages.gz is absent")
    expected_hash, expected_size = match.group(1), int(match.group(2))
    actual_hash = sha256(args.packages)
    actual_size = args.packages.stat().st_size
    if actual_hash != expected_hash or actual_size != expected_size:
        raise ValueError(
            f"Packages.gz mismatch: expected {expected_hash}/{expected_size}, got {actual_hash}/{actual_size}"
        )
    print(f"SIGNED-PACKAGES-PASS sha256={actual_hash} bytes={actual_size}")

    rootfs_name = args.ubuntu_rootfs.name
    sums = {}
    for line in args.ubuntu_sums.read_text(encoding="utf-8").splitlines():
        checksum, separator, filename = line.partition(" *")
        if separator:
            sums[filename] = checksum
    if rootfs_name not in sums:
        raise ValueError(f"signed Ubuntu sums do not name {rootfs_name}")
    rootfs_hash = sha256(args.ubuntu_rootfs)
    if rootfs_hash != sums[rootfs_name]:
        raise ValueError(f"Ubuntu rootfs mismatch: expected {sums[rootfs_name]}, got {rootfs_hash}")
    print(
        f"SIGNED-UBUNTU-ROOTFS-PASS sha256={rootfs_hash} "
        f"bytes={args.ubuntu_rootfs.stat().st_size}"
    )


if __name__ == "__main__":
    main()
