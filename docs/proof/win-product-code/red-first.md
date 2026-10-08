---
id: proof-win-product-code
title: "Ruling 152 C4 ProductCode red-first evidence"
type: proof-pack
status: active
owner: "@track-win-product-code"
phase: implementation
tags: [windows, persistence, proof, ruling-152]
links:
  - { to: design-windows-native-store, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red-first evidence that Win32 183 must remain unmapped by NativeFailure.ProductCode.
---
# ProductCode red-first evidence

The Windows Core test was added before changing `NativeFailure.ProductCode`.

Command: `CFD_TEST_ONLY=WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped dotnet run -c Release --project tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`

Observed on Windows with SDK 10.0.203:

```text
FAIL WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped InvalidOperationException: Expected ; actual DOC-IO
RESULT failures=1
SUBSET CFD_TEST_ONLY=WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped ran=1 skipped=700
TEST_EXIT=1 ELAPSED_MS=18469
```

The one selected check failed because Win32 183 received the generic `DOC-IO` code. The corrected contract is null for this unqualified value.

After changing the property to map only Win32 32, the same selected check passed:

```text
PASS WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped
RESULT failures=0
SUBSET CFD_TEST_ONLY=WindowsNative_ProductCode_UnqualifiedWin32Error_IsUnmapped ran=1 skipped=700
TEST_EXIT=0 ELAPSED_MS=6612
```

The mandatory Windows ring ran with SDK 10.0.203, `DOTNET_PROCESSOR_COUNT=6`, and process affinity `0x3F`. It exceeded the 60-second budget and was stopped after 168.624 seconds; the command returned 1, so the ring is not a pass. Core part 2/3 logged the new test as passing. The captured suite logs include unrelated failures: Core parts 1/3, 2/3, and 3/3 reported 9, 15, and 12 failures; Analysis part 2/2 reported two failures (catalog determinism and the 1-second wing-run budget); CLI reported two failures; Desktop reported two fail-closed-store failures. The raw logs remain in the ignored `.tmp-tests/` directory for this worktree.
