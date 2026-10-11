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


def parse_coreutils_sums(text: str) -> dict[str, str]:
    sums = {}
    for line in text.splitlines():
        match = re.fullmatch(r"([0-9a-f]{64}) ([ *])(.+)", line)
        if match:
            sums[match.group(3)] = match.group(1)
    return sums


def self_test() -> None:
    first = "a" * 64
    second = "b" * 64
    parsed = parse_coreutils_sums(f"{first}  text.tar.gz\n{second} *binary.tar.gz\ninvalid\n")
    if parsed != {"text.tar.gz": first, "binary.tar.gz": second}:
        raise SystemExit("CHECKSUM-MARKER-A self-test failed")
    print("checksum marker self-test OK")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--inrelease", type=Path)
    parser.add_argument("--packages", type=Path)
    parser.add_argument("--ubuntu-sums", type=Path)
    parser.add_argument("--ubuntu-rootfs", type=Path)
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return
    if None in (args.inrelease, args.packages, args.ubuntu_sums, args.ubuntu_rootfs):
        parser.error("all source paths are required outside --self-test")

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
    sums = parse_coreutils_sums(args.ubuntu_sums.read_text(encoding="utf-8"))
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
