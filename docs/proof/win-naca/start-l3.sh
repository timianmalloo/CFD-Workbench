#!/bin/bash
# Explicit coordinator-gated launcher, prepared only during W-4a/b.
set -euo pipefail
: "${CFDW_START_NOTE_REF:?Coordinator must supply the durable Ruling 79 start-note reference}"
run="$1"
test -d "$run"; test ! -e "$run/log.pipeline"
here=/mnt/c/Projects/CFD-Workbench-win-w4-validation/docs/proof/win-naca
nohup env -i PATH=/usr/bin:/bin HOME=/root USER=root LOGNAME=root LANG=C CFDW_START_NOTE_REF="$CFDW_START_NOTE_REF" \
  /bin/bash "$here/l3-supervisor.sh" "$run" > "$run/log.pipeline" 2>&1 < /dev/null &
printf 'supervisor_pid=%s note_ref=%s\n' "$!" "$CFDW_START_NOTE_REF"
