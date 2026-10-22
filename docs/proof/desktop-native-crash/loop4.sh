#!/usr/bin/env bash
# Crash loop D: two full runners at once, both from CR1-branch trees, every Desktop child under ProcDump (full dump on
# exception C0000374). Reproduces the "another track's suite running at the same time" condition that crashed C1 2 of 2
# and SF1 2 in one run, while keeping every child wrapped so a crash leaves a dump. Stops at the first dump or red run.
set -u
A="${CFD_CRASH_TREE_A:?set CFD_CRASH_TREE_A}"
B="${CFD_CRASH_TREE_B:?set CFD_CRASH_TREE_B}"
S="${CFD_CRASH_SCRATCH:?set CFD_CRASH_SCRATCH to a scratch folder}"
PD="${CFD_PROCDUMP:?set CFD_PROCDUMP to procdump64a.exe}"
DUMPS="$(cygpath -w "$S/dumps")"
N="${1:-8}"
mkdir -p "$S/runs4" "$S/dumps"
export CFD_TEST_CHILD_WRAPPER="$(cygpath -w "$PD") -accepteula -ma -e 1 -f C0000374 -x $DUMPS"
for i in $(seq 1 "$N"); do
  before=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  start=$(date +%s)
  (cd "$A" && bash tools/run-tests.sh > "$S/runs4/run$i-A.log" 2>&1; echo $? > "$S/runs4/run$i-A.code") &
  pa=$!
  (cd "$B" && bash tools/run-tests.sh > "$S/runs4/run$i-B.log" 2>&1; echo $? > "$S/runs4/run$i-B.code") &
  pb=$!
  wait $pa; wait $pb
  end=$(date +%s)
  ca=$(cat "$S/runs4/run$i-A.code"); cb=$(cat "$S/runs4/run$i-B.code")
  cp "$A/.tmp-tests/Desktop.log" "$S/runs4/run$i-A-Desktop.log" 2>/dev/null
  cp "$B/.tmp-tests/Desktop.log" "$S/runs4/run$i-B-Desktop.log" 2>/dev/null
  after=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  va=$(grep -o "TESTS-[A-Z]* [0-9]* s" "$S/runs4/run$i-A.log" | tail -1)
  vb=$(grep -o "TESTS-[A-Z]* [0-9]* s" "$S/runs4/run$i-B.log" | tail -1)
  crashes=$(cat "$S/runs4/run$i-A-Desktop.log" "$S/runs4/run$i-B-Desktop.log" 2>/dev/null | tr -d '\r' | grep -c "native crash\|Exception: C0000374\|Dump 1 complete")
  echo "run $i A exit $ca ($va) B exit $cb ($vb) seconds $((end-start)) crash-lines $crashes dumps $before->$after" | tee -a "$S/runs4/summary.txt"
  if [ "$after" -gt "$before" ] || [ "$ca" -ne 0 ] || [ "$cb" -ne 0 ]; then
    cat "$S/runs4/run$i-A-Desktop.log" "$S/runs4/run$i-B-Desktop.log" | tr -d '\r' | grep -n "native crash\|exited -\|^failed\|Dump 1 complete\|Exception: C" | head -10 | tee -a "$S/runs4/summary.txt"
    ls -la "$S/dumps" | tee -a "$S/runs4/summary.txt"
    echo "STOP at run $i" | tee -a "$S/runs4/summary.txt"
    exit 0
  fi
done
echo "no crash in $N paired runs" | tee -a "$S/runs4/summary.txt"
