---
id: proof-tmf-spec-excerpts
title: "3MF Core specification excerpts for the wing 3MF assumptions"
type: proof-pack
status: active
owner: "@trk-tmf"
phase: implementation
tags: [export, 3mf, spec, unit, winding, metadata, area-7]
links:
  - { to: design-export, rel: depends-on }
review-by: "2027-04-01"
summary: >-
  Confirms the two assume: markers of docs/design/export.md 4.3 against the 3MF Core specification itself: the unit value
  millimeter, and counter-clockwise winding with the face normal outward. Also the metadata names and the package parts.
---

# 3MF Core specification excerpts

Source: `https://raw.githubusercontent.com/3MFConsortium/spec_core/master/3MF%20Core%20Specification.md`, fetched 2026-10-10
(header says **Version 1.4.0**; this is the master text, not a frozen release). Section numbers are
the document's own. Quotations are verbatim.

## The `assume:` of design 4.3, confirmed

| Assumption | Verdict | Where and what it says |
|---|---|---|
| The unit string is `millimeter`. | **Confirmed.** | Section 3.4 (Model), attribute table: `unit` is `ST_Unit`, default `millimeter`, "Specifies the unit used to interpret all vertices, locations, or measurements in the model. Valid values are micron, millimeter, centimeter, inch, foot, and meter." The schema (Appendix B.1) enumerates `millimeter`. The writer states the attribute explicitly, so a reader that ignores the default still reads millimetres. |
| Winding is counter-clockwise, normal outward. | **Confirmed.** | Section 4.1.4 (Triangles): "The order of the vertices (v1, v2, v3) MUST be specified in counter-clockwise order, such that the face normal of the triangle is pointing toward the outside of the object. The indices v1, v2 and v3 MUST be distinct." The STL writer's winding is already outward (positive signed volume, checked by `StlExport_*`), so `ThreeMfExport` writes its triangles in the same order, and `ThreeMfExport_AsWritten_*` reads the package back and requires a positive signed volume. |

Slicer confirmation (B1) is in `slicer-open.json` and `slicer-run.txt`: both slicers read the `millimeter` files at the app's size, and
read the same coordinates declared as `meter` at 1000 times the size, so the unit attribute is honoured and the right scale is not an accident of a default.

## Metadata names (spec A8.5 export rule)

Section 3.4.1 (Metadata), Table 3-1, lists the well-known names a producer may use: `Title`, `Designer`, `Description`, `Copyright`, `LicenseTerms`, `Rating`,
`CreationDate`, `ModificationDate`, `Application` ("The name of the source application that originally created this document"). The
writer uses only `Title`, `Description` and `Application`. `Designer`, `Copyright` and the two dates are the names that would carry a person or a
clock, and the check `ThreeMfExport_Metadata_OnlyTitleDescriptionApplication` fails if any other key appears.

## Package parts

Chapter 2 and Appendix C: a 3MF is an OPC package. The model part's content type is `application/vnd.ms-package.3dmanufacturing-3dmodel+xml`;
the start part relationship type is `http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel` with target `/3D/3dmodel.model`. The writer emits
`[Content_Types].xml` (Default extensions `rels` and `model`), `_rels/.rels` and `3D/3dmodel.model`, the first entry being `[Content_Types].xml`.

## Not claimed

The 3MF Core coordinate-space note (section 3.1: x right, y toward the back, z up, origin at the bottom-front-left of the build plate) is a rule for
placing a part on a printer bed. The writer does not move the wing: it writes the app's own frame, as the STL does. Placement on the bed is not
measured here: the slicers' `--info` reports the part's size, volume and manifold state, not its position.
