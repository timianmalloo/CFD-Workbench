#!/usr/bin/env bash
# Crash loop C: the full runner from the CR1 worktree (its harness has the CFD_TEST_CHILD_WRAPPER hook), every Desktop
# child under ProcDump with a full dump on exception C0000374. Stops at the first run that produced a dump or is red.
set -u
ROOT="${CFD_CRASH_TREE_A:?set CFD_CRASH_TREE_A to the tree to run}"
S="${CFD_CRASH_SCRATCH:?set CFD_CRASH_SCRATCH to a scratch folder}"
PD="${CFD_PROCDUMP:?set CFD_PROCDUMP to procdump64a.exe}"
DUMPS="$(cygpath -w "$S/dumps")"
N="${1:-12}"
mkdir -p "$S/runs3" "$S/dumps"
export CFD_TEST_CHILD_WRAPPER="$(cygpath -w "$PD") -accepteula -ma -e 1 -f C0000374 -x $DUMPS"
cd "$ROOT" || exit 2
for i in $(seq 1 "$N"); do
  before=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  start=$(date +%s)
  bash tools/run-tests.sh > "$S/runs3/run$i.log" 2>&1
  code=$?
  end=$(date +%s)
  cp .tmp-tests/Desktop.log "$S/runs3/run$i-Desktop.log" 2>/dev/null
  after=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  verdict=$(grep -o "TESTS-[A-Z]* [0-9]* s" "$S/runs3/run$i.log" | tail -1)
  crashes=$(tr -d '\r' < "$S/runs3/run$i-Desktop.log" 2>/dev/null | grep -c "native crash\|Exception: C0000374\|Dump 1 complete")
  echo "run $i exit $code seconds $((end-start)) $verdict crash-lines $crashes dumps $before->$after" | tee -a "$S/runs3/summary.txt"
  if [ "$after" -gt "$before" ] || [ "$code" -ne 0 ]; then
    tr -d '\r' < "$S/runs3/run$i-Desktop.log" | grep -n "native crash\|exited -\|^failed\|Dump 1 complete\|Exception: C" | head -10 | tee -a "$S/runs3/summary.txt"
    ls -la "$S/dumps" | tee -a "$S/runs3/summary.txt"
    echo "STOP at run $i" | tee -a "$S/runs3/summary.txt"
    exit 0
  fi
done
echo "no crash in $N runs" | tee -a "$S/runs3/summary.txt"
