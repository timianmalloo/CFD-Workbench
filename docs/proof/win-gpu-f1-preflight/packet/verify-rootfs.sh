#!/bin/bash
# Read-only Ubuntu signature and existing local rootfs verification in the protected distro.
set -euo pipefail
if (($# != 3)); then exit 64; fi
sums=$1
signature=$2
rootfs=$3
[[ "$(basename "$rootfs")" == "ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz" ]] || exit 65
gpgv --keyring /usr/share/keyrings/ubuntu-cloudimage-keyring.gpg "$signature" "$sums"
line=$(awk -v name="$(basename "$rootfs")" '$2 == name {print $1 "  " $2}' "$sums")
[[ "${line%% *}" =~ ^[0-9a-f]{64}$ ]] || exit 65
printf '%s\n' "$line" | (cd -- "$(dirname "$rootfs")" && sha256sum --check --status)
printf 'ROOTFS-SIGNED-PASS sha256=%s\n' "${line%% *}"
