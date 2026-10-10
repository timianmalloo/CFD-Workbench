#!/bin/bash
# Ruling 200 B2: install only the accepted CUDA 13.2 lock and run the managed-memory oracle.
set -euo pipefail
export LC_ALL=C
export DEBIAN_FRONTEND=noninteractive
export NEEDRESTART_MODE=a

readonly script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly pid_file="${script_dir}/b2.pid"
readonly attempt_file="${script_dir}/b2.attempt"
readonly cancel_file="${script_dir}/workload-cancel.request"
readonly pid_tmp="${pid_file}.$$"
readonly attempt_tmp="${attempt_file}.$$"

if (($# < 2 || $# > 3)) || [[ "$1" != "--attempt-id" ]] || [[ ! "$2" =~ ^[A-Za-z0-9._-]+$ ]] || \
    { (($# == 3)) && [[ "$3" != "--preflight-only" ]]; }; then
    printf 'usage: %s --attempt-id ID [--preflight-only]\n' "$0" >&2
    exit 64
fi
readonly attempt_id=$2
readonly preflight_only=${3:-run}
cleanup() {
    rm -f "$pid_file" "$attempt_file" "$pid_tmp" "$attempt_tmp"
}
trap cleanup EXIT
if [[ -f "$cancel_file" ]] && [[ "$(cat "$cancel_file")" == "$attempt_id" ]]; then
    printf 'workload_cancelled=before-ownership attempt=%s\n' "$attempt_id"
    exit 75
fi
readonly own_pid=$$
readonly own_pgid=$(ps -o pgid= -p "$own_pid" | tr -d ' ')
if [[ "$own_pid" != "$own_pgid" ]]; then
    printf 'workload_process_group=unsafe pid=%s pgid=%s\n' "$own_pid" "$own_pgid" >&2
    exit 1
fi
printf '%s\n' "$attempt_id" > "$attempt_tmp"
mv -f "$attempt_tmp" "$attempt_file"
printf '%s\n' "$own_pid" > "$pid_tmp"
mv -f "$pid_tmp" "$pid_file"
if [[ -f "$cancel_file" ]] && [[ "$(cat "$cancel_file")" == "$attempt_id" ]]; then
    printf 'workload_cancelled=after-ownership attempt=%s\n' "$attempt_id"
    exit 75
fi

cd "$script_dir"
source /etc/os-release
if [[ "${ID:-}" != "ubuntu" || "${VERSION_ID:-}" != "24.04" ]]; then
    printf 'disposable OS identity changed: ID=%s VERSION_ID=%s\n' "${ID:-Not recorded}" "${VERSION_ID:-Not recorded}" >&2
    exit 65
fi
if [[ ! -f /etc/cfdw-b2-disposable ]] || \
    ! grep -Fxq 'distro=cfdw-cuda132-b2' /etc/cfdw-b2-disposable || \
    ! grep -Fxq 'os_id=ubuntu' /etc/cfdw-b2-disposable || \
    ! grep -Fxq 'version_id=24.04' /etc/cfdw-b2-disposable; then
    printf 'disposable distro marker is absent or invalid\n' >&2
    exit 65
fi
sha256sum --check accepted-inputs.sha256
mapfile -t locked < package-lock.args
if ((${#locked[@]} != 135)); then
    printf 'accepted lock has %s package arguments, expected 135\n' "${#locked[@]}" >&2
    exit 1
fi

dpkg-query -W -f='${Package}\t${Version}\t${Architecture}\t${db:Status-Status}\n' > packages-before.tsv
python3 verify-install-state.py --phase before --lock package-lock.json \
    --snapshot packages-before.tsv --output install-state-before.json

apt-get --simulate --no-install-recommends install "${locked[@]}" \
    > preinstall-simulate.stdout.txt 2> preinstall-simulate.stderr.txt
python3 build-package-lock.py --simulation preinstall-simulate.stdout.txt \
    --json package-lock.preinstall.tmp --args package-lock.args.preinstall.tmp
cmp package-lock.args package-lock.args.preinstall.tmp
python3 verify-debs.py --lock package-lock.json --archives /var/cache/apt/archives \
    --eula .source-cache/cuda-13.2.2-eula.html --verification download-verification.preinstall.tmp \
    --licenses license-map.preinstall.tmp --capacity capacity-linux.preinstall.tmp
cmp download-verification.json download-verification.preinstall.tmp
cmp license-map.json license-map.preinstall.tmp
if [[ "$preflight_only" == "--preflight-only" ]]; then
    printf 'B2-PREFLIGHT-PASS attempt=%s pid=%s pgid=%s\n' "$attempt_id" "$own_pid" "$own_pgid"
    exit 0
fi

apt-get --yes --no-install-recommends install "${locked[@]}" \
    > apt-install.stdout.txt 2> apt-install.stderr.txt
dpkg-query -W -f='${Package}\t${Version}\t${Architecture}\t${db:Status-Status}\n' > packages-after.tsv
python3 verify-install-state.py --phase after --lock package-lock.json --snapshot packages-after.tsv \
    --before packages-before.tsv --output install-state-after.json

readonly nvcc=/usr/local/cuda-13.2/bin/nvcc
if [[ ! -x "$nvcc" ]]; then
    printf 'expected compiler is absent: %s\n' "$nvcc" >&2
    exit 1
fi
{
    "$nvcc" --version
    /usr/lib/wsl/lib/nvidia-smi --query-gpu=name,driver_version,compute_cap --format=csv,noheader
    readlink -f "$nvcc"
} > toolchain.stdout.txt
"$nvcc" -O3 -std=c++17 -arch=sm_86 saxpy.cu -o saxpy.bin \
    > compile.stdout.txt 2> compile.stderr.txt
./saxpy.bin saxpy-result.json saxpy-device.marker \
    > saxpy.stdout.txt 2> saxpy.stderr.txt
sha256sum saxpy.cu saxpy.bin saxpy-result.json saxpy-device.marker > saxpy.sha256.txt
cat toolchain.stdout.txt
cat saxpy.stdout.txt
printf 'B2-INSTALL-TEST-PASS attempt=%s pid=%s pgid=%s\n' "$attempt_id" "$own_pid" "$own_pgid"
