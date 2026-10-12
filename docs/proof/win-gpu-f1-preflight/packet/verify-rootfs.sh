#!/bin/bash
# Read-only Ubuntu signature and existing local rootfs verification in the protected distro.
set -euo pipefail
if (($# != 7)); then exit 64; fi
sums=$1
signature=$2
rootfs=$3
contract=$4
packet_root=$5
status=$6
contract_sha256=$7
[[ "$(basename "$rootfs")" == "ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz" ]] || exit 65
python3 "${packet_root}/contract.py" keyring --contract "$contract" --expected-sha256 "$contract_sha256" \
    --keyring /usr/share/keyrings/ubuntu-cloudimage-keyring.gpg --kind ubuntu_rootfs
gpgv --status-fd 1 --keyring /usr/share/keyrings/ubuntu-cloudimage-keyring.gpg "$signature" "$sums" > "$status" 2>&1
python3 "${packet_root}/contract.py" signer --contract "$contract" --expected-sha256 "$contract_sha256" \
    --status "$status" --kind ubuntu_rootfs
line=$(awk -v name="$(basename "$rootfs")" '$2 == name {print $1 "  " $2}' "$sums")
[[ "${line%% *}" =~ ^[0-9a-f]{64}$ ]] || exit 65
printf '%s\n' "$line" | (cd -- "$(dirname "$rootfs")" && sha256sum --check --status)
printf 'ROOTFS-SIGNED-PASS sha256=%s\n' "${line%% *}"
