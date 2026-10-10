#!/bin/bash
# Ruling 197: bounded, read-only L3 observation with append-only output.
set -euo pipefail
export LC_ALL=C

readonly run_id="20261008T145043Z-win-spike04r3-g2-l3"
readonly run_dir="/root/CFDWorkbench/runs/${run_id}"
readonly log_file="${run_dir}/log.simpleFoam"
readonly unit_name="cfdw-l3-20261008-r1.service"
readonly interval_seconds=600

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly script_dir
readonly samples_file="${script_dir}/samples.jsonl"
readonly observer_sha256=$(sha256sum "$0" | awk '{print $1}')

count=1
if (($# > 0)); then
    if (($# != 2)) || [[ "$1" != "--count" ]] || [[ ! "$2" =~ ^[1-9][0-9]*$ ]]; then
        printf 'usage: %s [--count POSITIVE_INTEGER]\n' "$0" >&2
        exit 64
    fi
    count=$2
fi
if ((count > 144)); then
    printf 'count exceeds bounded maximum of 144 samples\n' >&2
    exit 64
fi

sample_once() {
    local utc monotonic_s loadavg_1m unit_active_state latest_complete_iteration
    local log_bytes log_mtime iteration_json bytes_json mtime_json

    utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    monotonic_s=$(awk '{print $1}' /proc/uptime)
    loadavg_1m=$(awk '{print $1}' /proc/loadavg)
    unit_active_state=$(systemctl show "$unit_name" --property=ActiveState --value --no-pager 2>/dev/null || true)
    case "$unit_active_state" in
        active|inactive|failed|activating|deactivating|reloading|maintenance) ;;
        *) unit_active_state="Not recorded" ;;
    esac

    latest_complete_iteration=""
    log_bytes=""
    log_mtime=""
    if [[ -f "$log_file" ]]; then
        log_bytes=$(stat -c '%s' "$log_file" 2>/dev/null || true)
        log_mtime=$(stat -c '%Y' "$log_file" 2>/dev/null || true)
        latest_complete_iteration=$(
            tail -c 262144 "$log_file" 2>/dev/null |
                awk '/^Time = [0-9]+([.]0+)?[[:space:]]*$/ {n=$3} /^ExecutionTime =/ && n!="" {last=n} END {if(last!="") print last}'
        )
    fi

    [[ "$latest_complete_iteration" =~ ^[0-9]+([.][0-9]+)?$ ]] && iteration_json=$latest_complete_iteration || iteration_json='"Not recorded"'
    [[ "$log_bytes" =~ ^[0-9]+$ ]] && bytes_json=$log_bytes || bytes_json='"Not recorded"'
    [[ "$log_mtime" =~ ^[0-9]+$ ]] && mtime_json=$log_mtime || mtime_json='"Not recorded"'

    printf '{"utc":"%s","monotonic_s":%s,"run_id":"%s","latest_complete_iteration":%s,"log_bytes":%s,"log_mtime":%s,"unit_active_state":"%s","loadavg_1m":%s,"observer_sha256":"%s"}\n' \
        "$utc" "$monotonic_s" "$run_id" "$iteration_json" "$bytes_json" "$mtime_json" \
        "$unit_active_state" "$loadavg_1m" "$observer_sha256" >> "$samples_file"
    printf 'sample appended utc=%s run_id=%s\n' "$utc" "$run_id"
}

for ((sample_number = 1; sample_number <= count; sample_number++)); do
    sample_once
    if ((sample_number < count)); then
        sleep "$interval_seconds"
    fi
done
