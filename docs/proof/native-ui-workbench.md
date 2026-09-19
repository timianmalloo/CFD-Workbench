---
id: proof-native-ui-workbench
title: CFD-Workbench native UI proof obligations
type: proof-pack
status: draft
owner: "@timianmalloo"
tags: [native, accessibility, cross-platform]
links:
  - {to: spec-cfd-workbench, rel: documents}
  - {to: mockup-workbench, rel: relates-to}
review-by: 2026-12-19
summary: Native-platform evidence remains explicitly unverified because this deliverable is an HTML design prototype. The required Mac and Windows checks are named without choosing an application framework.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
  - { by: mockup-workbench, on: 2026-09-19, reason: "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof." }
---

# Native proof obligations

Medium: native-desktop. Platform: cross-platform (macOS Apple silicon, Windows x64 first). Framework: other/unselected. Distribution: signed desktop application; exact package format unselected. Accessibility APIs: NSAccessibility/VoiceOver on Mac, UI Automation on Windows, subject to toolkit spike.

| Proof row | Evidence required | Status |
|---|---|---|
| Platform HIG | Apple macOS and Microsoft desktop keyboard guidance named in grounding | Microsoft documentation Verified; Apple body inspection and native runtime Flagged |
| Keyboard traversal | New→station→numeric edit→Undo→Save→Export on both OSes, no pointer | Flagged: no native runtime |
| Accessibility tree | Names, roles, values, selection and live-status behavior with VoiceOver/UIA | Flagged |
| Theme/high contrast | Light/dark/system high contrast, non-color states and visible focus | HTML direction only; native Flagged |
| DPI/windowing | 100/150/200% scaling; 1024×700 through large multi-monitor windows; focus restoration | Flagged |
| Large lists/viewport | Named 21-station/201-slice/50k-triangle benchmark and larger overflow fixture | Native performance Flagged |
| OS integration | Native menus, Cmd/Ctrl, open/save dialogs, recent documents, file associations | Flagged |
| Distribution trust | Signed/notarized Mac install, Windows signature/install, uninstall and rollback | Flagged |
| Backend lifecycle | Detect/install consent, smoke test, crash/cancel process tree isolation | Flagged |

XAML lint: N/A in this change because no XAML or framework selection exists. HTML craft/contrast checks establish prototype evidence only. No native PASS can be inferred from screenshots or a green HTML detector.
