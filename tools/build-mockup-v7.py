"""Build the self-contained review artifact from the preserved v6 baseline."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
s = (ROOT / 'docs/mockups/workbench-v6.html').read_text()

def replace(old, new, count=1):
    global s
    actual = s.count(old)
    if actual != count:
        raise ValueError(f'Expected {count} matches, got {actual}: {old[:90]}')
    s = s.replace(old, new)

replace('mockup-workbench-v6', 'mockup-workbench-v7', s.count('mockup-workbench-v6'))
replace('workbench-v6', 'workbench-v7', s.count('workbench-v6'))
replace('CFD-Workbench — workbench v4 mockup', 'CFD-Workbench — workbench v7 authoring review')
replace('interactive design mockup v4 (2026-09-21)', 'interactive design mockup v7 (2026-09-22)')
replace('revision 1.2 revision 1.1', 'revision 1.5')
replace('dashed = catalog original', 'dashed = illustrative seed reference')
replace('Prototype subset: one shared inline profile;', 'Prototype subset: up to 16 inline profiles with explicit assignments;')
replace('simplify: one inline shared profile, uniform master knots and mm; reject other constructs.', 'simplify: bounded inline profile bank and uniform master knots; reject unsupported constructs.')
replace("$('dsl-stations').textContent=`One shared profile “${r.profile}” at ${r.stations.map(u=>`${(u*100).toFixed(0)}%`).join(', ')}. Editing it changes every assigned station. Inspection slices are readouts.`;", "$('dsl-stations').textContent=`${r.profiles?.length??1} inline profiles · ${(r.assignments??r.stations.map(eta=>({eta,profile:r.profile}))).map(a=>`${(a.eta*100).toFixed(0)}% → ${a.profile}`).join(' · ')}. Rule A normalized blend; inspection slices are readouts.`;")
# The source-coordinate cage is replaced by sampled local section rows in v7.
start=s.index('function dCageY(x,y,tc){')
end=s.index('const dOriginalProfile',start)
s=s[:start]+s[end:]
replace('</style>', (ROOT / 'tools/mockup-v7.css').read_text() + '\n</style>')
replace('<div class="banners" id="banners">', '<div class="banners" id="banners">' + (ROOT / 'tools/mockup-v7.html').read_text())
# The parser uses the same bounded curve/geometry validation for every named profile.
start = s.index("  expect('profiles');expect('{');expect('profile');")
end = s.index("  if(stations.length<2", start)
s = s[:start] + '''  expect('profiles');expect('{');const profiles=[];
  while(peek()==='profile') { take();const id=str();if(!id.length||profiles.some(p=>p.id===id))fail('Profile names must be nonempty and unique.','DSL-REFERENCE');expect('{');expect('upper');const upper=curve(5,1,false);expect('lower');const lower=curve(5,1,false);if(peek()==='closure'){take();expect('closed');}expect('}');
    if(upper.P.length!==lower.P.length)fail('Prototype requires matching upper/lower point counts.','DSL-UNSUPPORTED');
    profiles.push({id,section:{upper:upper.P,lower:lower.P},knots:{upper:upper.U,lower:lower.U}});if(profiles.length>16)fail('Prototype limit is 16 profiles.','DSL-LIMIT'); }
  expect('}');if(!profiles.length)fail('At least one profile is required.','DSL-REFERENCE');
  const profile=profiles[0].id;
  expect('sections');expect('{');const stations=[],assignments=[];while(peek()==='at'){take();let u;if(peek()==='root'||peek()==='center'){take();u=0;}else if(peek()==='tip'){take();u=1;}else{u=num();if(take()!=='%')fail('Prototype station positions use root, tip or %.','DSL-UNSUPPORTED');u/=100;}expect('profile');const id=str();if(!profiles.some(p=>p.id===id))fail('Unresolved profile reference.','DSL-REFERENCE');stations.push(u);assignments.push({eta:u,profile:id});if(stations.length>64)fail('Prototype station limit is 64.','DSL-LIMIT');}expect('}');
  if(profiles.some(p=>!assignments.some(a=>a.profile===p.id)))fail('Remove unused profiles or assign them to a station.','DSL-REFERENCE');
''' + s[end:]
start = s.index('  for(const P of [upper.P,lower.P])')
end = s.index('\n}\nfunction dload', start)
s = s[:start] + '''  for(const p of profiles) {
    for(const P of [p.section.upper,p.section.lower])if(P[0][1]!==0||P.at(-1)[1]!==0)fail('Closed profile must meet at (0,0) and (1,0).','DSL-CLOSURE');
    const inv=(P,U,x)=>{let lo=0,hi=1;for(let k=0;k<40;k++){const t=(lo+hi)/2;if(BS.evalCurve(P,5,U,t)[0]<x)lo=t;else hi=t;}return BS.evalCurve(P,5,U,(lo+hi)/2)[1];};
    for(let k=1;k<200;k++)if(inv(p.section.upper,p.knots.upper,k/200)<=inv(p.section.lower,p.knots.lower,k/200))fail('Upper and lower section cross at a sampled chord location.','DSL-GEOMETRY');
  }
  return {name,profile,half,cv,profiles,assignments,section:profiles[0].section,knots:profiles[0].knots,stations,locks:{rootMirror,tipValue:M.locks.tipValue}};''' + s[end:]
replace('return profilePoints(tc / 0.09, n).map(X)', 'return profilePoints(tc / 0.09, n, u).map(X)')
replace('profilePoints(tc / 0.09, 30).map(p =>', 'profilePoints(tc / 0.09, 30, u).map(p =>')
replace("secP('upper').slice().reverse().concat(secP('lower').slice(1)).map(([xc, yc]) => P(...X([xc, dCageY(xc, yc, tc)])))", "profilePoints(tc / .09, 12, u).map(q => P(...X(q)))")
replace('Display cage of the NURBS loft:', 'Sampled display cage of the loft:')
replace('section polygons and the LE and TE rail polygons (box display)', 'evaluated section rows and the LE and TE control polygons (box display)')
replace("const svg = el.ownerSVGElement; const toSvg = (cx, cy) => { const r = svg.getBoundingClientRect();", "const toSvg = (cx, cy) => { const svg = $('section-canvas').querySelector('svg'); const r = svg.getBoundingClientRect();")
# Selection is permitted during a draft; mutation guards below retain its owner.
replace("if (M.preview && M.preview.ch !== ch) { status(`Apply or cancel the open ${CH_NAME[M.preview.ch]} draft first (one draft at a time) · Return applies · Escape cancels`); return false; }", '')
replace('if (M.preview && M.preview.ch !== ch) return; ', '')
replace('if(out===demit(old))return;', 'if(out===demit(old))return;dparse(out);')
replace('const ch = pth.dataset.pick;', "const ch = pth.dataset.pick; $('shape-canvas').focus();")
replace("function stationKeys(e) { if (M.activeDoc !== 'station') return;", "function stationKeys(e) { if (M.activeDoc !== 'station' || e.target.closest('dialog, .ux-context') || /^(SELECT|SUMMARY)$/.test(e.target.tagName)) return;")
replace("if (M.preview && M.preview.ch !== ch) { status(`Apply or cancel the open ${CH_NAME[M.preview.ch]} draft first (one draft at a time)`); return; }", '')
# Snapshot restoration must not overwrite a bank restored from its authoritative source snapshot.
replace('if (h.sectionCV) { M.secCV = h.sectionCV;', 'if (h.sectionCV && !h.dsl) { M.secCV = h.sectionCV;')
replace('if (f.sectionCV) { M.secCV = f.sectionCV;', 'if (f.sectionCV && !f.dsl) { M.secCV = f.sectionCV;')
replace('/* ===================== init ===================== */', (ROOT / 'tools/mockup-v7-authoring.js').read_text() + '\n/* ===================== init ===================== */')
replace("showTask('cad'); audit(); dinit();", "showTask('cad'); audit(); dinit(); uxInit();")
(ROOT / 'docs/mockups/workbench-v7.html').write_text(s)
print('Built docs/mockups/workbench-v7.html')
