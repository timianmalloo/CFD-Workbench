#!/bin/bash
# Exact attempt group inspection. Python distinguishes inspection failure from absence.
set -euo pipefail
if (($# < 5 || $# > 7)) || [[ "$1" != --mode || "$3" != --run-root || ! "$5" =~ ^[A-Za-z0-9._-]+$ ]]; then
    exit 64
fi
if (($# == 7)); then
    [[ "$6" == --kind && "$7" == guard ]] || exit 64
    exec /usr/bin/python3 "$(dirname "$0")/owned_control.py" --mode "$2" --run-root "$4" --run-id "$5" --kind guard
fi
exec /usr/bin/python3 "$(dirname "$0")/owned_control.py" --mode "$2" --run-root "$4" --run-id "$5"
