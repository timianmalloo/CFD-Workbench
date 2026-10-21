#!/usr/bin/env bash
# wsl-preflight.sh - the Ruling 217 dependency preflight, run inside the existing WSL distro, touching nothing in it.
#
# Answers one question: do the CUDA 13.2, OpenFOAM 2512 and build-tool packages resolve for this architecture from the
# reviewed repositories with the reviewed signing keys, and how large is the download. apt runs with temporary sources,
# lists, cache and state, so the distro's own apt configuration, lists and cache are never read or written. Nothing is
# installed. The host's dpkg status is read (read-only) so the simulation resolves against what is really present.
#
#   usage: bash tools/wsl-preflight.sh --keyrings <dir> --out <receipt.json> [--download] [--work <dir>]
#     --keyrings  a directory holding openfoam-archive-keyring.gpg and cuda-archive-keyring.gpg at the reviewed hashes
#     --out       the JSON receipt to write (a Windows path is fine through /mnt)
#     --download  also download the resolved .deb files into the temporary directory and hash them (deleted on exit)
#     --work      keep the working directory here instead of a temporary one (then it is not deleted)
#
# Exit 0 only when every repository index verified, the simulation resolved every root and removed nothing, and (with
# --download) every archive downloaded. The receipt is written on failure too, with `result: FAIL` and the step.
set -euo pipefail

keyrings='' out='' download=0 work=''
while [[ $# -gt 0 ]]; do
    case "$1" in
        --keyrings) keyrings=$2; shift 2 ;;
        --out) out=$2; shift 2 ;;
        --download) download=1; shift ;;
        --work) work=$2; shift 2 ;;
        *) printf 'unknown argument: %s\n' "$1" >&2; exit 64 ;;
    esac
done
[[ -n "$keyrings" && -n "$out" ]] || { printf 'usage: wsl-preflight.sh --keyrings <dir> --out <receipt.json> [--download] [--work <dir>]\n' >&2; exit 64; }
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

# The reviewed inputs: the same keyrings and hashes the F1 contract bound (docs/proof/win-gpu-f1-preflight/inputs).
openfoam_sha=6d17d37b82fc840b8a5c767545703df7264cfa3e30f9cf74556ebb124bda1a93
nvidia_sha=25100d6f2eccaee7d6719a70e3c0c6145064aa9edc2c32148f693ac4c524a376
ubuntu_keyring=/usr/share/keyrings/ubuntu-archive-keyring.gpg

# The package roots the packet resolved (transaction.py roots), one per line, `make` pinned to the architecture.
roots=(
    cuda-toolkit-13-2=13.2.2-1
    openfoam2512=2512.0-2 openfoam2512-common=2512.0-2 openfoam2512-dev=2512.0-2
    openfoam2512-source=2512.0-2 openfoam2512-tools=2512.0-2
    gcc-13 g++-13 gfortran-13 openmpi-bin libopenmpi-dev libblas-dev liblapack-dev pkg-config
)

arch=$(dpkg --print-architecture)
case "$arch" in
    arm64) ubuntu_archive=http://ports.ubuntu.com/ubuntu-ports; nvidia_repo=https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/sbsa/ ;;
    amd64) ubuntu_archive=http://archive.ubuntu.com/ubuntu; nvidia_repo=https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/ ;;
    *) printf 'unsupported architecture: %s\n' "$arch" >&2; exit 65 ;;
esac
roots+=("make:${arch}")

if [[ -n "$work" ]]; then tmp=$work; mkdir -p "$tmp"; else tmp=$(mktemp -d /tmp/wsl-preflight.XXXXXX); trap 'rm -rf "$tmp"' EXIT; fi
mkdir -p "$tmp/keyrings" "$tmp/lists/partial" "$tmp/cache/archives/partial" "$tmp/state" "$tmp/debs" "$tmp/gpgv"
printf '%s\n' "$arch" > "$tmp/architecture.txt"
uname -m > "$tmp/uname.txt"
( nvidia-smi --query-gpu=name,driver_version --format=csv,noheader 2>/dev/null || printf 'not recorded\n' ) > "$tmp/gpu.txt"
date -u +%Y-%m-%dT%H:%M:%SZ > "$tmp/started.txt"
start=$(date +%s)

step() { printf '%s\n' "$1" > "$tmp/step.txt"; printf '== %s\n' "$1"; }
finish() {
    local result=$1
    printf '%s\n' "$result" > "$tmp/result.txt"
    printf '%s\n' "$(( $(date +%s) - start ))" > "$tmp/seconds.txt"
    python3 "$here/wsl-preflight-receipt.py" --work "$tmp" --out "$out"
    printf 'WSL-PREFLIGHT-%s step=%s seconds=%s receipt=%s\n' "$result" "$(cat "$tmp/step.txt")" "$(cat "$tmp/seconds.txt")" "$out"
}
trap 'finish FAIL; exit 1' ERR

step keyrings
for pair in "openfoam-archive-keyring.gpg:$openfoam_sha" "cuda-archive-keyring.gpg:$nvidia_sha"; do
    name=${pair%%:*}; expected=${pair##*:}
    actual=$(sha256sum -- "$keyrings/$name" | cut -c1-64)
    [[ "$actual" == "$expected" ]] || { printf 'keyring %s: sha256 %s, expected %s\n' "$name" "$actual" "$expected" >&2; false; }
    cp -- "$keyrings/$name" "$tmp/keyrings/$name"
done
[[ -f "$ubuntu_keyring" ]] || { printf 'ubuntu archive keyring absent: %s\n' "$ubuntu_keyring" >&2; false; }
cp -- "$ubuntu_keyring" "$tmp/keyrings/ubuntu-archive-keyring.gpg"
sha256sum "$tmp"/keyrings/*.gpg > "$tmp/keyrings.sha256"

step sources
{
    printf 'deb [arch=%s signed-by=%s/keyrings/ubuntu-archive-keyring.gpg] %s noble main universe\n' "$arch" "$tmp" "$ubuntu_archive"
    printf 'deb [arch=%s signed-by=%s/keyrings/ubuntu-archive-keyring.gpg] %s noble-updates main universe\n' "$arch" "$tmp" "$ubuntu_archive"
    printf 'deb [arch=%s signed-by=%s/keyrings/ubuntu-archive-keyring.gpg] %s noble-security main universe\n' "$arch" "$tmp" "$ubuntu_archive"
    printf 'deb [arch=%s signed-by=%s/keyrings/openfoam-archive-keyring.gpg] https://dl.openfoam.com/repos/deb noble main\n' "$arch" "$tmp"
    printf 'deb [arch=%s signed-by=%s/keyrings/cuda-archive-keyring.gpg] %s /\n' "$arch" "$tmp" "$nvidia_repo"
} > "$tmp/sources.list"
apt=(apt-get
    -o "Dir::Etc::sourcelist=$tmp/sources.list" -o Dir::Etc::sourceparts=-
    -o "Dir::State=$tmp/state" -o "Dir::State::lists=$tmp/lists" -o Dir::State::status=/var/lib/dpkg/status
    -o "Dir::Cache=$tmp/cache" -o "Dir::Cache::archives=$tmp/cache/archives"
    -o Acquire::Languages=none -o APT::Update::Error-Mode=any -o APT::Get::List-Cleanup=0
)

step update
"${apt[@]}" update > "$tmp/update.log" 2>&1

step verify-indices
for index in "$tmp"/lists/*_InRelease; do
    name=$(basename "$index")
    case "$name" in
        *dl.openfoam.com*) key=$tmp/keyrings/openfoam-archive-keyring.gpg ;;
        *developer.download.nvidia.com*) key=$tmp/keyrings/cuda-archive-keyring.gpg ;;
        *) key=$tmp/keyrings/ubuntu-archive-keyring.gpg ;;
    esac
    gpgv --status-fd 1 --keyring "$key" "$index" > "$tmp/gpgv/$name.txt" 2>&1 || true
    grep -q '^\[GNUPG:\] VALIDSIG ' "$tmp/gpgv/$name.txt" || { printf 'index not validly signed: %s\n' "$name" >&2; false; }
done

step simulate
"${apt[@]}" --simulate --no-install-recommends install "${roots[@]}" > "$tmp/simulate.log" 2>&1
! grep -q '^Remv ' "$tmp/simulate.log" || { printf 'the simulation removes a package\n' >&2; false; }
"${apt[@]}" --print-uris --yes --no-install-recommends install "${roots[@]}" > "$tmp/uris.log" 2>&1
printf '%s\n' "${roots[@]}" > "$tmp/roots.txt"

if [[ "$download" -eq 1 ]]; then
    step download
    mapfile -t exact < <(sed -n 's/^Inst \([^ ]*\) (\([^ ]*\) .*/\1=\2/p' "$tmp/simulate.log")
    (cd "$tmp/debs" && "${apt[@]}" download "${exact[@]}") > "$tmp/download.log" 2>&1
    (cd "$tmp/debs" && sha256sum ./*.deb) > "$tmp/debs.sha256"
fi

step done
trap - ERR
finish PASS
