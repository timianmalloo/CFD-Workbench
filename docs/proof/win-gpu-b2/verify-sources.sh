#!/bin/bash
# Ruling 200: verify the official key and signed repository metadata before apt configuration.
set -euo pipefail
export LC_ALL=C

readonly proof_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly cache="${proof_root}/.source-cache"
readonly scratch="${cache}/keyring-extract"
readonly expected_fingerprint="EB693B3035CD5710E231E123A4B469963BF863CC"
readonly ubuntu_cloud_keyring="/usr/share/keyrings/ubuntu-cloudimage-keyring.gpg"

rm -rf "$scratch"
mkdir -p "$scratch"
dpkg-deb --extract "${cache}/cuda-keyring_1.1-1_all.deb" "$scratch"
readonly keyring="${scratch}/usr/share/keyrings/cuda-archive-keyring.gpg"
test -f "$keyring"
actual_fingerprint=$(gpg --batch --no-default-keyring --keyring "$keyring" --with-colons --fingerprint |
    awk -F: '$1 == "fpr" {print $10; exit}')
readonly actual_fingerprint
if [[ "$actual_fingerprint" != "$expected_fingerprint" ]]; then
    printf 'unexpected NVIDIA archive key fingerprint: %s\n' "$actual_fingerprint" >&2
    exit 65
fi
gpgv --keyring "$keyring" "${cache}/cuda-ubuntu2404-InRelease"
gpgv --keyring "$ubuntu_cloud_keyring" \
    "${cache}/ubuntu-noble-SHA256SUMS.gpg" "${cache}/ubuntu-noble-SHA256SUMS"
python3 "${proof_root}/verify-sources.py" \
    --inrelease "${cache}/cuda-ubuntu2404-InRelease" \
    --packages "${cache}/cuda-ubuntu2404-Packages.gz" \
    --ubuntu-sums "${cache}/ubuntu-noble-SHA256SUMS" \
    --ubuntu-rootfs "${cache}/ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz"
printf 'NVIDIA-SOURCE-CHAIN-PASS fingerprint=%s\n' "$actual_fingerprint"
