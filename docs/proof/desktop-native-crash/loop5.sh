#!/usr/bin/env bash
# Crash loop E: the same paired load as loop D (two Desktop harness parents at once, every child under ProcDump), but with
# ANGLE (av_libGLESv2.dll) removed from both CR1 bin folders so Avalonia renders in software. No build step, so the
# removal holds. Stops at the first dump or the first native-crash line; a timeout or drain exit (124/125) under this
# load is recorded and the loop continues.
set -u
A="${CFD_CRASH_TREE_A:?set CFD_CRASH_TREE_A}"
B="${CFD_CRASH_TREE_B:?set CFD_CRASH_TREE_B}"
EXE="tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.exe"
S="${CFD_CRASH_SCRATCH:?set CFD_CRASH_SCRATCH to a scratch folder}"
PD="${CFD_PROCDUMP:?set CFD_PROCDUMP to procdump64a.exe}"
DUMPS="$(cygpath -w "$S/dumps")"
N="${1:-8}"
mkdir -p "$S/runs5" "$S/dumps"
export CFD_TEST_CHILD_WRAPPER="$(cygpath -w "$PD") -accepteula -ma -e 1 -f C0000374 -x $DUMPS"
# Git for Windows ships its own av_libGLESv2.dll in clangarm64/bin and mingw64/bin; with those on PATH the Windows loader
# finds ANGLE there once the app-local copy is gone (observed: the children loaded Git's copy). Drop them from PATH.
export PATH="$(printf '%s' "$PATH" | tr ':' '\n' | grep -v -i 'clangarm64/bin\|mingw64/bin\|clang64/bin' | paste -sd ':' -)"
echo "PATH entries with Git compiler bins removed: $(printf '%s' "$PATH" | tr ':' '\n' | grep -c -i 'git')" | tee -a "$S/runs5/summary.txt"
for T in "$A" "$B"; do
  if [ -f "$T/tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/runtimes/win-arm64/native/av_libGLESv2.dll" ]; then
    echo "ANGLE present in $T; refusing" | tee -a "$S/runs5/summary.txt"; exit 2
  fi
done
for i in $(seq 1 "$N"); do
  before=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  start=$(date +%s)
  (cd "$A" && "$EXE" --harness > "$S/runs5/run$i-A.log" 2>&1; echo $? > "$S/runs5/run$i-A.code") &
  pa=$!
  (cd "$B" && "$EXE" --harness > "$S/runs5/run$i-B.log" 2>&1; echo $? > "$S/runs5/run$i-B.code") &
  pb=$!
  wait $pa; wait $pb
  end=$(date +%s)
  ca=$(cat "$S/runs5/run$i-A.code"); cb=$(cat "$S/runs5/run$i-B.code")
  after=$(ls "$S/dumps"/*.dmp 2>/dev/null | wc -l)
  pa_=$(grep -c "^PASS" "$S/runs5/run$i-A.log"); pb_=$(grep -c "^PASS" "$S/runs5/run$i-B.log")
  fa=$(grep -c "^FAIL" "$S/runs5/run$i-A.log"); fb=$(grep -c "^FAIL" "$S/runs5/run$i-B.log")
  crashes=$(cat "$S/runs5/run$i-A.log" "$S/runs5/run$i-B.log" | tr -d '\r' | grep -c "native crash\|exited -\|Exception: C0000374\|Dump 1 complete")
  echo "run $i A exit $ca pass $pa_ fail $fa | B exit $cb pass $pb_ fail $fb | seconds $((end-start)) crash-lines $crashes dumps $before->$after" | tee -a "$S/runs5/summary.txt"
  if [ "$after" -gt "$before" ] || [ "$crashes" -ne 0 ]; then
    cat "$S/runs5/run$i-A.log" "$S/runs5/run$i-B.log" | tr -d '\r' | grep -n "native crash\|exited -\|^FAIL\|Dump 1 complete\|Exception: C" | head -10 | tee -a "$S/runs5/summary.txt"
    ls -la "$S/dumps" | tee -a "$S/runs5/summary.txt"
    echo "STOP at run $i" | tee -a "$S/runs5/summary.txt"
    exit 0
  fi
done
echo "no crash in $N paired software-rendered runs" | tee -a "$S/runs5/summary.txt"
