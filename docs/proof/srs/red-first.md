---
id: proof-srs-red-first
title: "SRS red-first receipt"
type: proof-pack
status: active
owner: "@trk-srs"
phase: implementation
tags: [catalog, instrumentation, proof, ruling-156]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for Ruling 156 (3): the catalog.preview replace event records the CAT-UNAVAILABLE refusal (code, check, detail) instead of an empty family; the failure stays cached.
---

# SRS red-first (Ruling 156 (3): the replace event records the catalog refusal)

Check: `Replace_Preview_CatalogUnavailable_EventRecordsRefusal` (`tests/CfdWorkbench.Core.Tests/SectionReplaceTests.cs`).
It plants a loader that throws `ContractError("CAT-UNAVAILABLE")` with `check` and `detail` data through the new internal seam
`SectionReplace.CatalogFamilies` / `LoadFamilies`, previews a generated section, and reads the `catalog.preview` event's `Replace`.

Command: `CFD_TEST_ONLY=Replace_Preview_CatalogUnavailable tools/run-suite.sh dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll`

## Red (old behaviour: the catch returns null; the new `FamilyRefusal` property exists but nothing fills it)

```
FAIL Replace_Preview_CatalogUnavailable_EventRecordsRefusal InvalidOperationException: Expected CAT-UNAVAILABLE check=coordinate-hash detail=naca-0012: expected a, actual b; actual
RESULT failures=1
SUBSET CFD_TEST_ONLY=Replace_Preview_CatalogUnavailable ran=1 skipped=722
```

## Green (CFD_TEST_ONLY=Replace_Preview, so the existing outcome check runs beside it)

```
PASS Replace_Preview_EmitsCatalogPreviewOutcome
PASS Replace_Preview_CatalogUnavailable_EventRecordsRefusal
RESULT failures=0
SUBSET CFD_TEST_ONLY=Replace_Preview ran=2 skipped=721
```

## Decision: the cached failure stays cached

The catalog is an embedded resource. A retry re-reads the same bytes and fails the same way, and each preview would pay the load
again. The refusal text is cached with the failure, so every later event in the process carries the cause.

## Not swept

Other `catch { return null; }` sites that feed a recorded field (the record-write-path sweep) are outside this track.
