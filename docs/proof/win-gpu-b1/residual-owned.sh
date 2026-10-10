#!/bin/bash
# Ruling 201: find the exact B1 attempt by cmdline, independent of ownership files.
set -euo pipefail
export LC_ALL=C

readonly owned_path="/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1/inspect-b1.sh"
if (($# == 1)) && [[ "$1" == "--any-attempt" ]]; then
    readonly expected_attempt='*'
    readonly mode=check
elif (($# >= 2 && $# <= 3)) && [[ "$1" == "--attempt-id" ]] && [[ "$2" =~ ^[A-Za-z0-9._-]+$ ]] && \
    { (($# == 2)) || [[ "$3" == "--stop" ]]; }; then
    readonly expected_attempt=$2
    readonly mode=${3:-check}
else
    printf 'usage: %s --any-attempt | --attempt-id ID [--stop]\n' "$0" >&2
    exit 64
fi
set +e
candidate_text=$(/usr/bin/pgrep -f -- '[/]mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1/inspect-b1.sh')
pgrep_exit=$?
set -e
if ((pgrep_exit > 1)); then
    printf 'residual_process=inspection-failed pgrep_exit=%s\n' "$pgrep_exit" >&2
    exit "$pgrep_exit"
fi
candidates=()
if [[ -n "$candidate_text" ]]; then
    mapfile -t candidates <<< "$candidate_text"
fi
matches=()
group_present() {
    local pgid=$1 snapshot status
    set +e
    snapshot=$(ps -eo pgid=)
    status=$?
    set -e
    if ((status != 0)); then
        printf 'residual_process=group-inspection-failed pgid=%s ps_exit=%s\n' "$pgid" "$status" >&2
        return 2
    fi
    if awk -v pgid="$pgid" '$1 == pgid { found=1 } END { exit found ? 0 : 1 }' <<< "$snapshot"; then
        return 0
    fi
    return 1
}
for pid in "${candidates[@]}"; do
    [[ -r "/proc/${pid}/cmdline" ]] || continue
    cmdline=$(tr '\0' ' ' < "/proc/${pid}/cmdline")
    if [[ "$expected_attempt" == "*" && "$cmdline" == *"${owned_path} --attempt-id "* ]] || \
        [[ "$cmdline" == *"${owned_path} --attempt-id ${expected_attempt} "* ]]; then
        matches+=("$pid")
        printf 'residual_process=present pid=%s cmdline=%s\n' "$pid" "$cmdline"
    fi
done
if ((${#matches[@]} == 0)); then
    printf 'residual_process=absent attempt=%s pgrep_candidates=%s\n' "$expected_attempt" "${#candidates[@]}"
    exit 0
fi
if [[ "$mode" != "--stop" ]]; then
    exit 10
fi
groups=()
for pid in "${matches[@]}"; do
    [[ -r "/proc/${pid}/cmdline" ]] || continue
    pgid=$(ps -o pgid= -p "$pid" | tr -d ' ')
    if [[ "$pgid" != "$pid" ]]; then
        printf 'residual_process=unsafe-group pid=%s pgid=%s\n' "$pid" "$pgid" >&2
        exit 1
    fi
    groups+=("$pgid")
    kill -TERM -- "-${pgid}"
done
for _ in {1..20}; do
    remaining=0
    for pgid in "${groups[@]}"; do
        if group_present "$pgid"; then
            remaining=1
        else
            state=$?
            ((state == 1)) || exit "$state"
        fi
    done
    if ((remaining == 0)); then
        printf 'residual_process=stopped attempt=%s\n' "$expected_attempt"
        exit 0
    fi
    sleep 0.1
done
for pgid in "${groups[@]}"; do
    kill -KILL -- "-${pgid}" 2>/dev/null || true
done
sleep 0.1
for pgid in "${groups[@]}"; do
    if group_present "$pgid"; then
        printf 'residual_process=still-running attempt=%s pgid=%s\n' "$expected_attempt" "$pgid" >&2
        exit 1
    else
        state=$?
        ((state == 1)) || exit "$state"
    fi
done
printf 'residual_process=stopped-kill attempt=%s\n' "$expected_attempt"
