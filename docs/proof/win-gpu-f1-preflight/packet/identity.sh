#!/bin/bash
# The host starts this file under taskset, nice and foreground setsid --wait.
set -euo pipefail
if (($# < 3)) || [[ "$2" != "--" ]]; then
    printf 'identity: expected OUTPUT -- COMMAND [ARGS...]\n' >&2
    exit 64
fi
output=$1
shift 2
pid=$$
pgid=$(ps -o pgid= -p "$pid" | tr -d ' ')
sid=$(ps -o sid= -p "$pid" | tr -d ' ')
nice=$(ps -o ni= -p "$pid" | tr -d ' ')
cpus=$(awk '/^Cpus_allowed_list:/ {print $2}' "/proc/${pid}/status")
if [[ "$pid" != "$pgid" || "$pid" != "$sid" || "$nice" -lt 10 ||
      "$cpus" != "0" && "$cpus" != "0-1" && "$cpus" != "0,1" ]]; then
    printf 'identity mismatch pid=%s pgid=%s sid=%s nice=%s cpus=%s\n' "$pid" "$pgid" "$sid" "$nice" "$cpus" >&2
    exit 65
fi
owner_tmp=$(mktemp "${output}.XXXXXX")
printf 'pid=%s\npgid=%s\nsid=%s\nnice=%s\nCpus_allowed_list=%s\ncmdline=%s\n' \
    "$pid" "$pgid" "$sid" "$nice" "$cpus" "$(tr '\0' ' ' < "/proc/${pid}/cmdline")" > "$owner_tmp"
mv -- "$owner_tmp" "$output"
exec "$@"
