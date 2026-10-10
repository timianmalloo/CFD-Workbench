#!/bin/bash
# Stop only the B1 probe launched from this proof folder and prove the Linux process is gone.
set -euo pipefail
export LC_ALL=C

readonly owned_path="/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1/inspect-b1.sh"
readonly pid_file="/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1/b1.pid"
readonly attempt_file="/mnt/c/Projects/CFD-Workbench-win-gpu-g2-b1-r199/docs/proof/win-gpu-b1/b1.attempt"
if (($# < 2 || $# > 3)) || [[ "$1" != "--attempt-id" ]] || [[ ! "$2" =~ ^[A-Za-z0-9._-]+$ ]] || \
    (($# == 3)) && [[ "$3" != "--check" ]]; then
    printf 'usage: %s --attempt-id ID [--check]\n' "$0" >&2
    exit 64
fi
readonly expected_attempt=$2
readonly mode=${3:-stop}
if [[ ! -f "$pid_file" ]] || [[ ! -f "$attempt_file" ]]; then
    printf 'owned_process=identity-not-published attempt=%s\n' "$expected_attempt" >&2
    exit 75
fi
readonly owned_pid=$(cat "$pid_file")
readonly actual_attempt=$(cat "$attempt_file")
if [[ "$actual_attempt" != "$expected_attempt" ]]; then
    printf 'owned_process=attempt-mismatch expected=%s actual=%s\n' "$expected_attempt" "$actual_attempt" >&2
    exit 1
fi
if [[ ! "$owned_pid" =~ ^[1-9][0-9]*$ ]] || [[ ! -r "/proc/${owned_pid}/cmdline" ]]; then
    printf 'owned_process=invalid-or-gone pid=%s\n' "$owned_pid" >&2
    exit 1
fi
readonly owned_cmdline=$(tr '\0' ' ' < "/proc/${owned_pid}/cmdline")
if [[ "$owned_cmdline" != *"/bin/bash ${owned_path}"* ]]; then
    printf 'owned_process=identity-mismatch pid=%s cmdline=%s\n' "$owned_pid" "$owned_cmdline" >&2
    exit 1
fi
readonly owned_pgid=$(ps -o pgid= -p "$owned_pid" | tr -d ' ')
readonly owned_sid=$(ps -o sid= -p "$owned_pid" | tr -d ' ')
if [[ "$owned_pgid" != "$owned_pid" ]] || [[ "$owned_sid" != "$owned_pid" ]]; then
    printf 'owned_process=group-identity-mismatch pid=%s pgid=%s sid=%s\n' \
        "$owned_pid" "$owned_pgid" "$owned_sid" >&2
    exit 1
fi
printf 'owned_pid_before=%s pgid=%s sid=%s cmdline=%s\n' \
    "$owned_pid" "$owned_pgid" "$owned_sid" "$owned_cmdline"
if [[ "$mode" == "--check" ]]; then
    printf 'owned_process_group=acknowledged attempt=%s pgid=%s\n' "$expected_attempt" "$owned_pgid"
    exit 0
fi
ps -o pid=,ppid=,pgid=,sid=,stat=,args= -g "$owned_pgid" || true
kill -TERM -- "-${owned_pgid}"
for _ in {1..20}; do
    if ! ps -eo pgid= | awk -v pgid="$owned_pgid" '$1 == pgid { found=1 } END { exit found ? 0 : 1 }'; then
        rm -f "$pid_file" "$attempt_file"
        printf 'owned_process_group=stopped-term pgid=%s\n' "$owned_pgid"
        exit 0
    fi
    sleep 0.1
done
kill -KILL -- "-${owned_pgid}" 2>/dev/null || true
sleep 0.1
if ps -eo pgid= | awk -v pgid="$owned_pgid" '$1 == pgid { found=1 } END { exit found ? 0 : 1 }'; then
    printf 'owned_process_group=still-running pgid=%s\n' "$owned_pgid" >&2
    exit 1
fi
rm -f "$pid_file" "$attempt_file"
printf 'owned_process_group=stopped-kill pgid=%s\n' "$owned_pgid"
