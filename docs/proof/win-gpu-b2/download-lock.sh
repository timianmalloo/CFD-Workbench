#!/bin/bash
# Ruling 200 phase B preparation: exact download and measurement, no package installation.
set -euo pipefail
export LC_ALL=C DEBIAN_FRONTEND=noninteractive

readonly proof_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly marker="/etc/cfdw-b2-disposable"
readonly archives="/var/cache/apt/archives"
test -f "$marker"

python3 "${proof_root}/build-package-lock.py" \
    --simulation "${proof_root}/apt-simulate.stdout.txt" \
    --json "${proof_root}/package-lock.json" \
    --args "${proof_root}/package-lock.args"

mapfile -t locked < "${proof_root}/package-lock.args"
if ((${#locked[@]} == 0)); then
    printf 'package lock is empty\n' >&2
    exit 65
fi
apt-get --simulate --no-install-recommends install "${locked[@]}" \
    > "${proof_root}/apt-locked-simulate.stdout.txt" 2> "${proof_root}/apt-locked-simulate.stderr.txt"
python3 "${proof_root}/build-package-lock.py" \
    --simulation "${proof_root}/apt-locked-simulate.stdout.txt" \
    --json "${proof_root}/package-lock.verify.tmp" \
    --args "${proof_root}/package-lock.args.verify.tmp"
if ! cmp -s "${proof_root}/package-lock.args" "${proof_root}/package-lock.args.verify.tmp"; then
    printf 'exact lock changed the apt transaction\n' >&2
    exit 65
fi
apt-get --download-only --no-install-recommends install "${locked[@]}" \
    > "${proof_root}/apt-download.stdout.txt" 2> "${proof_root}/apt-download.stderr.txt"

find /var/lib/apt/lists -maxdepth 1 -type f -name '*InRelease' -print0 | sort -z | xargs -0 sha256sum \
    > "${proof_root}/apt-inrelease.sha256.txt"
python3 "${proof_root}/verify-debs.py" \
    --lock "${proof_root}/package-lock.json" \
    --archives "$archives" \
    --eula "${proof_root}/.source-cache/cuda-13.2.2-eula.html" \
    --verification "${proof_root}/download-verification.json" \
    --licenses "${proof_root}/license-map.json" \
    --capacity "${proof_root}/capacity-linux.json"
