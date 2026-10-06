# trk-uni step 0 trace (Ruling 115)

- The strip's "mm" item is `UnitsItem` in `Shell/StatusStrip.axaml`. `ShellHost.StripUnits()` feeds it: "mm", or "% chord" in the
  section editor. It is a read-only length-unit label. CAD lengths are fixed in mm (spec A4.7: internal SI; DESIGN.md: lengths "mm").
- Spec A4.7 and GEO-09 name no CAD inches. ANA-18 names N/lbf and A4.7 names m/s/kn. So Units changes Analysis display only
  (`WorkbenchController.AnalysisUnits`); CAD length display stays mm. No CAD length wiring is needed (nothing to stop for).
- Decision: one item. The "mm" item becomes the Units item (Metric / Imperial, a button). Why: two adjacent unit items on a strip that
  already drops items at large Text size would say two things about "units"; the CAD length unit is fixed, and each CAD field already
  carries its unit. Cost: the "% chord" strip label in the section editor is gone (the section fields name their own unit).
- Persistence: `PreferenceStore` has Load/SaveTextSizeAsync only. `DisplayPreferences.Parse` rejects any property except
  format/version/textSize. A units choice needs a new key in Persistence (not owned by this track). Exit (c) is blocked on that.
