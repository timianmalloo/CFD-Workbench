#!/bin/bash
# Exact owned-group inspection/stop. Never match an unrelated process by a loose name.
set -euo pipefail
if (($# != 3)) || [[ "$1" != "--mode" ]]; then exit 64; fi
mode=$2
run_root=$3
[[ "$mode" == check || "$mode" == stop ]] || exit 64
pid_file="${run_root}/current-owner.pid"
scan_stage_groups() {
    local stage file group
    for stage in probe repositories simulate download inspect delete-debs; do
        file="${run_root}/${stage}.owner.txt"
        [[ -f "$file" ]] || continue
        group=$(sed -n 's/^pgid=//p' "$file")
        [[ "$group" =~ ^[1-9][0-9]*$ ]] || { printf 'invalid stage PGID: %s\n' "$file" >&2; return 65; }
        if ps -eo pgid= | awk -v wanted="$group" '$1 == wanted {found=1} END {exit found ? 0 : 1}'; then
            printf 'residual_stage_group=%s pgid=%s\n' "$stage" "$group" >&2
            return 5
        fi
    done
    printf 'residual_stage_groups=absent\n'
}
if [[ ! -f "$pid_file" ]]; then
    printf 'owned_process=absent reason=no-pid-file\n'
    scan_stage_groups
    exit 0
fi
pid=$(cat "$pid_file")
[[ "$pid" =~ ^[1-9][0-9]*$ ]] || exit 65
if [[ ! -r "/proc/${pid}/cmdline" ]]; then
    if ps -eo pgid= | awk -v wanted="$pid" '$1 == wanted {found=1} END {exit found ? 0 : 1}'; then
        printf 'owned_process=leader-gone-but-group-present pgid=%s\n' "$pid" >&2
        exit 5
    fi
    printf 'owned_process=absent pid=%s\n' "$pid"
    scan_stage_groups
    exit 0
fi
cmdline=$(tr '\0' ' ' < "/proc/${pid}/cmdline")
if [[ "$cmdline" != *"preflight.sh --stage "* || "$cmdline" != *"--run-root ${run_root}"* ]]; then
    printf 'owned_process=identity-mismatch pid=%s cmdline=%s\n' "$pid" "$cmdline" >&2
    exit 65
fi
pgid=$(ps -o pgid= -p "$pid" | tr -d ' ')
sid=$(ps -o sid= -p "$pid" | tr -d ' ')
[[ "$pid" == "$pgid" && "$pid" == "$sid" ]] || exit 65
printf 'owned_process=present pid=%s pgid=%s sid=%s cmdline=%s\n' "$pid" "$pgid" "$sid" "$cmdline"
if [[ "$mode" == check ]]; then exit 10; fi
kill -TERM -- "-${pgid}"
for _ in {1..40}; do
    if ! ps -eo pgid= | awk -v wanted="$pgid" '$1 == wanted {found=1} END {exit found ? 0 : 1}'; then
        printf 'owned_process=stopped-term pgid=%s\n' "$pgid"
        scan_stage_groups
        exit 0
    fi
    sleep 0.1
done
kill -KILL -- "-${pgid}" 2>/dev/null || true
sleep 0.1
if ps -eo pgid= | awk -v wanted="$pgid" '$1 == wanted {found=1} END {exit found ? 0 : 1}'; then
    printf 'owned_process=still-running pgid=%s\n' "$pgid" >&2
    exit 5
fi
printf 'owned_process=stopped-kill pgid=%s\n' "$pgid"
scan_stage_groups
