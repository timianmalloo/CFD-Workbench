#!/bin/bash
set -euo pipefail
here=/mnt/c/Projects/CFD-Workbench-win-w4-validation/docs/proof/win-naca
for name in of-command.sh of-command-l3.sh start-l3.sh l3-supervisor.sh; do
  bash -n "$here/$name"
  printf 'PASS bash -n %s\n' "$name"
done
