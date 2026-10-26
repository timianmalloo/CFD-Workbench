#!/usr/bin/env bash
# HX item 4(d): the status-strip check 20 times through the Desktop exe, alone. Run from the tree root.
exe=tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.exe
echo "foreign before:"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Desktop.Tests.exe" | grep -c "CfdWorkbench.Desktop" ; tasklist.exe //FI "IMAGENAME eq testhost.exe" | grep -c "testhost"
pass=0; fail=0
for i in $(seq 1 20); do
  out=$(CFD_TEST_ONLY=StatusStrip_BackgroundCompletion "$exe" --harness --controller-shell 2>&1)
  rc=$?
  p=$(echo "$out" | grep -c "^PASS StatusStrip_BackgroundCompletion")
  f=$(echo "$out" | grep -c "^FAIL")
  if [ "$rc" -eq 0 ] && [ "$p" -eq 2 ] && [ "$f" -eq 0 ]; then pass=$((pass+1)); else fail=$((fail+1)); echo "run $i rc=$rc"; echo "$out" | grep -E "FAIL" | cut -c1-200; fi
done
echo "REPEATS pass=$pass fail=$fail"
echo "foreign after:"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Desktop.Tests.exe" | grep -c "CfdWorkbench.Desktop" ; tasklist.exe //FI "IMAGENAME eq testhost.exe" | grep -c "testhost"
