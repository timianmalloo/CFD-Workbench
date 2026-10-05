#!/bin/bash
# Track B baseline: run the fast ring N times on a quiet machine; record wall, build, per-part seconds and load.
set -uo pipefail
WT=${1:?worktree}
N=${2:-3}
OUT=${3:?output csv}
cd "$WT"
echo "run,load_start,load_end,wall_s,build_s,part,part_s,exit" > "$OUT"
for i in $(seq 1 "$N"); do
  l0=$(sysctl -n vm.loadavg | tr -d '{}' | awk '{print $1}')
  t0=$(date +%s)
  log=$(mktemp)
  tools/run-tests.sh > "$log" 2>&1
  status=$?
  t1=$(date +%s)
  l1=$(sysctl -n vm.loadavg | tr -d '{}' | awk '{print $1}')
  build=$(grep -Eo '^build [0-9]+ s' "$log" | awk '{print $2}')
  for f in .tmp-tests/*.seconds; do
    echo "$i,$l0,$l1,$((t1 - t0)),$build,$(basename "$f" .seconds),$(cat "$f"),$status" >> "$OUT"
  done
  tail -4 "$log" | sed "s/^/run $i: /"
  rm -f "$log"
done
