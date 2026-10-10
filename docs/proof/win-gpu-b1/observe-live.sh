#!/bin/bash
# Rulings 199-200: append through the coordinator observer, then evaluate both clocks live.
set -euo pipefail
export LC_ALL=C

readonly script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
readonly observer="${script_dir}/../win-l3-observer/observe.sh"
readonly samples="${script_dir}/../win-l3-observer/samples.jsonl"
readonly guard="${script_dir}/observer_guard.py"
readonly events="${script_dir}/guard-events.jsonl"
readonly stop_request="${script_dir}/observer-stop.request"
readonly state="${script_dir}/observer-live.state.json"
readonly state_tmp="${script_dir}/observer-live.state.$$.tmp"
readonly baseline_sha256="f9c174dd595ee97d3e0ada37396971f898426660fbe9c30d9d180f683addeade"

if (($# != 6)) || [[ "$1" != "--count" ]] || [[ ! "$2" =~ ^[1-9][0-9]*$ ]] || \
    [[ "$3" != "--start-row" ]] || [[ ! "$4" =~ ^[1-9][0-9]*$ ]] || \
    [[ "$5" != "--session-id" ]] || [[ ! "$6" =~ ^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$ ]]; then
    printf 'usage: %s --count POSITIVE_INTEGER --start-row POSITIVE_INTEGER --session-id ID\n' "$0" >&2
    exit 64
fi

readonly count=$2
readonly start_row=$4
readonly session_id=$6
if ((count > 144)); then
    printf 'count exceeds bounded maximum of 144 samples\n' >&2
    exit 64
fi
if ((start_row <= 8)); then
    printf 'start-row must identify a fresh row after the eight-row integrity anchor\n' >&2
    exit 64
fi

if [[ -f "$state" ]]; then
    printf 'observer state already exists; refusing a colliding session\n' >&2
    exit 73
fi

rm -f "$stop_request"
completed_samples=0
last_sample_utc="Not recorded"
state_exit_json='"Not recorded"'
guard_stopped=0
write_state() {
    local state_name=$1
    printf '{"state":"%s","pid":%s,"completed_samples":%s,"requested_samples":%s,"session_id":"%s","start_row":%s,"last_sample_utc":"%s","exit_code":%s}\n' \
        "$state_name" "$$" "$completed_samples" "$count" "$session_id" "$start_row" \
        "$last_sample_utc" "$state_exit_json" > "$state_tmp"
    mv -f "$state_tmp" "$state"
}
finish_state() {
    local exit_code=$?
    state_exit_json=$exit_code
    write_state "stopped"
    return "$exit_code"
}
trap finish_state EXIT
write_state "running"

for ((sample_number = 1; sample_number <= count; sample_number++)); do
    /bin/bash "$observer" --count 1
    set +e
    guard_output=$(/usr/bin/python3 "$guard" \
        --samples "$samples" \
        --events "$events" \
        --start-row "$start_row" \
        --session-id "$session_id" \
        --expected-prefix-sha256 "$baseline_sha256")
    guard_exit=$?
    set -e
    printf '%s\n' "$guard_output"
    completed_samples=$sample_number
    last_sample_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    write_state "running"
    if ((guard_exit == 5)); then
        guard_stopped=1
    fi
    if ((guard_exit != 0 && guard_exit != 5)); then
        exit "$guard_exit"
    fi
    if ((sample_number < count)); then
        for ((sleep_slice = 1; sleep_slice <= 120; sleep_slice++)); do
            if [[ -f "$stop_request" ]]; then
                printf 'observer stop requested after sample %s\n' "$sample_number"
                exit 0
            fi
            sleep 5
        done
    fi
done
if ((guard_stopped == 1)); then
    exit 5
fi
