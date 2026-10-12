#!/bin/bash
# Each stage is a separate foreground, owned Linux session. No package installation.
set -euo pipefail
export LC_ALL=C DEBIAN_FRONTEND=noninteractive
if (($# != 16)) || [[ "$1" != "--stage" || "$3" != "--run-root" || "$5" != "--packet-root" ||
                     "$7" != "--b2-lock" || "$9" != "--openfoam-key" ||
                     "${11}" != "--nvidia-key" || "${13}" != "--openfoam-fingerprint" ||
                     "${15}" != "--run-id" ]]; then
    printf 'preflight: exact stage argument contract required\n' >&2
    exit 64
fi
stage=$2
run_root=$4
packet_root=$6
b2_lock=$8
openfoam_key=${10}
nvidia_key=${12}
openfoam_fingerprint=${14}
run_id=${16}
[[ "$run_id" =~ ^[A-Za-z0-9._-]+$ && "$openfoam_fingerprint" =~ ^[A-F0-9]{40}$ ]] || exit 65
[[ "$run_root" == /mnt/c/* && "$packet_root" == /mnt/c/* ]] || exit 65
mkdir -p "$run_root"
printf '%s\n' "$$" > "${run_root}/current-owner.pid"
pid=$$
pgid=$(ps -o pgid= -p "$pid" | tr -d ' ')
sid=$(ps -o sid= -p "$pid" | tr -d ' ')
nice=$(ps -o ni= -p "$pid" | tr -d ' ')
cpus=$(awk '/^Cpus_allowed_list:/ {print $2}' "/proc/${pid}/status")
[[ "$pid" == "$pgid" && "$pid" == "$sid" && "$nice" -ge 10 &&
   ( "$cpus" == "0-1" || "$cpus" == "0,1" ) ]] || {
    printf 'stage owner mismatch pid=%s pgid=%s sid=%s nice=%s cpus=%s\n' "$pid" "$pgid" "$sid" "$nice" "$cpus" >&2
    exit 65
}
printf 'stage=%s\npid=%s\npgid=%s\nsid=%s\nnice=%s\nCpus_allowed_list=%s\ncmdline=%s\n' \
    "$stage" "$pid" "$pgid" "$sid" "$nice" "$cpus" "$(tr '\0' ' ' < "/proc/${pid}/cmdline")" \
    > "${run_root}/${stage}.owner.txt"
cache="/var/cache/cfdw-f1-r217/${run_id}"
sources=/etc/apt/cfdw-f1-r217.list
config=/etc/apt/cfdw-f1-r217.conf
lists=/var/lib/apt/lists-cfdw-f1-r217
export APT_CONFIG=$config
check_cancel() { [[ ! -f "${run_root}/cancel.request" ]] || { printf 'cancel requested\n' >&2; exit 75; }; }
check_cancel

case "$stage" in
    probe)
        source_device=$(findmnt -n -o SOURCE /)
        [[ "$source_device" == /dev/* ]] || { printf 'unrecognized root block device: %s\n' "$source_device" >&2; exit 65; }
        block_bytes=$(blockdev --getsize64 "$source_device")
        fs_bytes=$(df -B1 --output=size / | tail -n 1 | tr -d ' ')
        free_bytes=$(df -B1 --output=avail / | tail -n 1 | tr -d ' ')
        max=8589934592
        ((block_bytes > 0 && block_bytes <= max && fs_bytes > 0 && fs_bytes <= max)) || {
            printf 'guest VHD cap failed block=%s filesystem=%s max=%s\n' "$block_bytes" "$fs_bytes" "$max" >&2
            exit 65
        }
        printf 'device=%s\nblock_bytes=%s\nfilesystem_bytes=%s\nfree_bytes=%s\nmaximum_bytes=%s\n' \
            "$source_device" "$block_bytes" "$fs_bytes" "$free_bytes" "$max" > "${run_root}/guest-capacity.txt"
        ;;
    repositories)
        [[ -f "${run_root}/guest-capacity.txt" ]] || exit 65
        mkdir -p /etc/apt/keyrings
        cp -- "$openfoam_key" /etc/apt/keyrings/cfdw-openfoam.gpg
        cp -- "$nvidia_key" /etc/apt/keyrings/cfdw-nvidia.gpg
        openfoam_actual=$(gpg --batch --show-keys --with-colons --fingerprint /etc/apt/keyrings/cfdw-openfoam.gpg |
            awk -F: '$1 == "fpr" {print $10; exit}')
        nvidia_actual=$(gpg --batch --show-keys --with-colons --fingerprint /etc/apt/keyrings/cfdw-nvidia.gpg |
            awk -F: '$1 == "fpr" {print $10; exit}')
        [[ "$openfoam_actual" == "$openfoam_fingerprint" &&
           "$nvidia_actual" == EB693B3035CD5710E231E123A4B469963BF863CC ]] || exit 65
        printf 'openfoam_fingerprint=%s\nnvidia_fingerprint=%s\n' "$openfoam_actual" "$nvidia_actual" \
            > "${run_root}/repository-keys.txt"
        printf '%s\n' \
            'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://archive.ubuntu.com/ubuntu noble main universe' \
            'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://archive.ubuntu.com/ubuntu noble-updates main universe' \
            'deb [arch=amd64 signed-by=/usr/share/keyrings/ubuntu-archive-keyring.gpg] http://security.ubuntu.com/ubuntu noble-security main universe' \
            'deb [arch=amd64 signed-by=/etc/apt/keyrings/cfdw-openfoam.gpg] https://dl.openfoam.com/repos/deb noble main' \
            'deb [arch=amd64 signed-by=/etc/apt/keyrings/cfdw-nvidia.gpg] https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/ /' \
            > "$sources"
        printf '%s\n' \
            'Dir::Etc::sourcelist "/etc/apt/cfdw-f1-r217.list";' \
            'Dir::Etc::sourceparts "-";' \
            'Dir::State::lists "/var/lib/apt/lists-cfdw-f1-r217";' \
            'APT::Get::Install-Recommends "false";' > "$config"
        cp -- "$sources" "${run_root}/apt-sources.list"
        apt-get -o APT::Update::Error-Mode=any update \
            > "${run_root}/apt-update.stdout.txt" 2> "${run_root}/apt-update.stderr.txt"
        mkdir -p "${run_root}/signed-indices"
        index_count=0
        for index in "$lists"/*_InRelease; do
            [[ -f "$index" ]] || continue
            name=$(basename "$index")
            case "$name" in
                *dl.openfoam.com*) key=/etc/apt/keyrings/cfdw-openfoam.gpg ;;
                *developer.download.nvidia.com*) key=/etc/apt/keyrings/cfdw-nvidia.gpg ;;
                *archive.ubuntu.com*|*security.ubuntu.com*) key=/usr/share/keyrings/ubuntu-archive-keyring.gpg ;;
                *) printf 'unexpected apt index: %s\n' "$name" >&2; exit 65 ;;
            esac
            gpgv --status-fd 1 --keyring "$key" "$index" > "${run_root}/signed-indices/${name}.gpgv.txt" 2>&1
            grep -q '^\[GNUPG:\] VALIDSIG ' "${run_root}/signed-indices/${name}.gpgv.txt"
            cp -- "$index" "${run_root}/signed-indices/${name}"
            ((index_count+=1))
        done
        ((index_count == 5)) || { printf 'expected five signed InRelease indices, found %s\n' "$index_count" >&2; exit 65; }
        find "$lists" -maxdepth 1 -type f -name '*Packages*' -print0 | sort -z |
            xargs -0 sha256sum > "${run_root}/apt-packages.sha256.txt"
        find "$lists" -maxdepth 1 -type f -name '*Packages*' -print0 | sort -z |
            tar --null -T - -cJf "${run_root}/signed-indices/packages.tar.xz"
        ;;
    simulate)
        [[ -f "${run_root}/signed-indices/packages.tar.xz" ]] || exit 65
        dpkg-query -W -f='${binary:Package}\t${Version}\t${Architecture}\n' \
            > "${run_root}/baseline-packages.tsv"
        python3 "${packet_root}/transaction.py" roots --b2-lock "$b2_lock" --output "${run_root}/roots.args"
        mapfile -t roots < "${run_root}/roots.args"
        apt-get --simulate --no-install-recommends install "${roots[@]}" \
            > "${run_root}/apt-simulate.stdout.txt" 2> "${run_root}/apt-simulate.stderr.txt"
        apt-get --print-uris --yes --no-install-recommends install "${roots[@]}" \
            > "${run_root}/apt-uris.stdout.txt" 2> "${run_root}/apt-uris.stderr.txt"
        python3 "${packet_root}/transaction.py" lock \
            --simulation "${run_root}/apt-simulate.stdout.txt" --uris "${run_root}/apt-uris.stdout.txt" \
            --output "${run_root}/transaction-lock.json" --args "${run_root}/transaction.args"
        mapfile -t exact < "${run_root}/transaction.args"
        apt-get --simulate --no-install-recommends install "${exact[@]}" \
            > "${run_root}/apt-exact-simulate.stdout.txt" 2> "${run_root}/apt-exact-simulate.stderr.txt"
        python3 "${packet_root}/transaction.py" compare \
            --first "${run_root}/apt-simulate.stdout.txt" --second "${run_root}/apt-exact-simulate.stdout.txt"
        ;;
    download)
        [[ -f "${run_root}/transaction-lock.json" ]] || exit 65
        mkdir -p "$cache"
        mapfile -t exact < "${run_root}/transaction.args"
        (cd "$cache" && apt-get download "${exact[@]}") \
            > "${run_root}/apt-download.stdout.txt" 2> "${run_root}/apt-download.stderr.txt"
        ;;
    inspect)
        [[ -d "$cache" ]] || exit 65
        python3 "${packet_root}/transaction.py" verify \
            --lock "${run_root}/transaction-lock.json" --archives "$cache" \
            --licences "${run_root}/license-map.json" --capacity "${run_root}/capacity.json" \
            --b2-licences "$(dirname "$b2_lock")/license-map.json"
        ;;
    delete-debs)
        if [[ -d "$cache" ]]; then
            find "$cache" -maxdepth 1 -type f -name '*.deb' -delete
            if find "$cache" -maxdepth 1 -type f -name '*.deb' | grep -q .; then exit 65; fi
            rmdir "$cache"
        fi
        printf 'downloaded_debs=absent\n' > "${run_root}/download-cleanup.txt"
        ;;
    *) printf 'unknown stage: %s\n' "$stage" >&2; exit 64 ;;
esac
printf 'STAGE-PASS stage=%s\n' "$stage"
