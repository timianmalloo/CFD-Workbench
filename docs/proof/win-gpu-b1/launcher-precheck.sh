#!/bin/bash
# Ruling 201: expose launcher/session identity, then become a uniquely tagged sleep.
set -euo pipefail
export LC_ALL=C

if (($# != 2)) || [[ "$1" != "--attempt-id" ]] || [[ ! "$2" =~ ^[A-Za-z0-9._-]+$ ]]; then
    printf 'usage: %s --attempt-id ID\n' "$0" >&2
    exit 64
fi
readonly attempt_id=$2
readonly marker="cfdw-b1-launcher-precheck-${attempt_id}"
printf 'attempt_id=%s marker=%s\n' "$attempt_id" "$marker"
ps -o pid=,ppid=,pgid=,sid=,ni=,psr=,comm= -p "$$"
printf 'precheck_ready=%s\n' "$marker"
exec -a "$marker" /usr/bin/sleep 30
