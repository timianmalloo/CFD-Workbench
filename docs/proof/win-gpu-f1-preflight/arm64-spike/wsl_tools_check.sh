#!/bin/sh
# Read-only: what the stock Ubuntu-24.04 WSL distro reports from inside, for the host-identity receipt.
# Prints key=value lines: the packet's Linux-side tools, the Ubuntu keyrings and their hashes, the apt sources, the
# architecture names. Nothing is installed or changed.
for t in gpgv gpg dpkg dpkg-query apt-get apt-cache python3 sha256sum setsid taskset findmnt blockdev xz tar curl ps awk; do
    printf 'tool_%s=%s\n' "$t" "$(command -v "$t" 2>/dev/null || printf absent)"
done
printf 'python3_version=%s\n' "$(python3 --version 2>&1)"
printf 'dpkg_architecture=%s\n' "$(dpkg --print-architecture)"
printf 'uname_m=%s\n' "$(uname -m)"
printf 'kernel=%s\n' "$(uname -r)"
printf 'os_release_version_id=%s\n' "$(. /etc/os-release && printf '%s' "$VERSION_ID")"
printf 'ubuntu_keyring_package=%s\n' "$(dpkg-query -W -f='${Version}' ubuntu-keyring 2>/dev/null || printf absent)"
for k in ubuntu-archive-keyring ubuntu-cloudimage-keyring; do
    f="/usr/share/keyrings/$k.gpg"
    if [ -f "$f" ]; then printf '%s_sha256=%s\n' "$k" "$(sha256sum "$f" | cut -c1-64)"; else printf '%s_sha256=absent\n' "$k"; fi
done
printf 'apt_sources_uris=%s\n' "$(grep -h '^URIs:' /etc/apt/sources.list.d/*.sources 2>/dev/null | sort -u | tr '\n' ' ')"
printf 'dev_dxg=%s\n' "$([ -e /dev/dxg ] && printf present || printf absent)"
