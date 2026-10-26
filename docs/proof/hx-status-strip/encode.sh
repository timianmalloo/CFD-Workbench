#!/usr/bin/env bash
# HX item 5: the encode measurement at the cap, alone. Run from the tree root. Not committed.
exe=tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.exe
echo "foreign before:"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Desktop.Tests.exe" | grep -c "CfdWorkbench.Desktop"; tasklist.exe //FI "IMAGENAME eq testhost.exe" | grep -c "testhost"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Analysis.Tests.exe" | grep -c "CfdWorkbench.Analysis"
for i in 1 2; do
  CFD_COMPARE_ENCODE_TIMING=1 CFD_TEST_ONLY=Discrepancy_CompareFields_EncodeCost_Measured "$exe" --harness 2>&1 | grep -E "MEASURED|FAIL|PASS Discrepancy_CompareFields"
done
echo "foreign after:"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Desktop.Tests.exe" | grep -c "CfdWorkbench.Desktop"; tasklist.exe //FI "IMAGENAME eq testhost.exe" | grep -c "testhost"; tasklist.exe //FI "IMAGENAME eq CfdWorkbench.Analysis.Tests.exe" | grep -c "CfdWorkbench.Analysis"
