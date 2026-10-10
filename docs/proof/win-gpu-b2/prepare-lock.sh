#!/bin/bash
# Ruling 200 phase A: configure and simulate only; no CUDA package installation.
set -euo pipefail
export LC_ALL=C DEBIAN_FRONTEND=noninteractive

readonly proof_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly cache="${proof_root}/.source-cache"
readonly marker="/etc/cfdw-b2-disposable"
readonly expected_name="cfdw-cuda132-b2"

source /etc/os-release
if [[ "${ID:-}" != "ubuntu" || "${VERSION_ID:-}" != "24.04" ]]; then
    printf 'expected fresh Ubuntu 24.04, got ID=%s VERSION_ID=%s\n' "${ID:-Not recorded}" "${VERSION_ID:-Not recorded}" >&2
    exit 65
fi
if [[ -e "$marker" ]]; then
    printf 'disposable marker already exists; refusing replay\n' >&2
    exit 73
fi
if dpkg-query -W -f='${binary:Package}\n' 2>/dev/null | grep -Eq '^(cuda-|nvidia-|nsight-|libcu)'; then
    printf 'fresh distro already contains a CUDA or NVIDIA package\n' >&2
    exit 65
fi

printf 'distro=%s\nos_id=%s\nversion_id=%s\ncreated_utc=%s\n' \
    "$expected_name" "$ID" "$VERSION_ID" "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$marker"
cat /etc/os-release > "${proof_root}/os-release.stdout.txt"
/usr/lib/wsl/lib/nvidia-smi --query-gpu=name,driver_version,compute_cap --format=csv,noheader \
    > "${proof_root}/gpu-preflight.stdout.txt"

dpkg --install "${cache}/cuda-keyring_1.1-1_all.deb" > "${proof_root}/keyring-install.stdout.txt" \
    2> "${proof_root}/keyring-install.stderr.txt"
printf '%s\n' \
    'Package: cuda-toolkit-config-common cuda-toolkit-13-config-common' \
    'Pin: version 13.2.*' \
    'Pin-Priority: 1001' > /etc/apt/preferences.d/cfdw-cuda132

dpkg-query -W -f='${binary:Package}\t${Version}\t${Architecture}\n' \
    > "${proof_root}/baseline-packages.stdout.txt"

apt-get update > "${proof_root}/apt-update.stdout.txt" 2> "${proof_root}/apt-update.stderr.txt"
apt-cache policy cuda-toolkit-13-2 cuda-toolkit-config-common cuda-toolkit-13-config-common \
    > "${proof_root}/apt-policy.stdout.txt" 2> "${proof_root}/apt-policy.stderr.txt"
apt-get --simulate --no-install-recommends install \
    cuda-toolkit-13-2=13.2.2-1 \
    cuda-toolkit-config-common=13.2.86-1 \
    cuda-toolkit-13-config-common=13.2.86-1 \
    > "${proof_root}/apt-simulate.stdout.txt" 2> "${proof_root}/apt-simulate.stderr.txt"

if grep -Eiq '(^|[[:space:]])(cuda-drivers|nvidia-(driver|dkms|kernel)|[^[:space:]]*-13-[34])([[:space:]]|$)' \
    "${proof_root}/apt-simulate.stdout.txt"; then
    printf 'simulation contains a forbidden driver or CUDA 13.3/13.4 package\n' >&2
    exit 65
fi
grep -F 'cuda-toolkit-13-2 (13.2.2-1' "${proof_root}/apt-simulate.stdout.txt" > /dev/null
printf 'B2-SIMULATION-PASS distro=%s root=cuda-toolkit-13-2=13.2.2-1\n' "$expected_name"
