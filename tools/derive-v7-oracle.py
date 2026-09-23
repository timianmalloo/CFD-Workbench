"""Preserve the CAD regression floor, evolving only approved v7 transaction assertions."""
from pathlib import Path
p = Path(__file__).resolve().parent
s = (p / 'check-mockup-v6.mjs').read_text(encoding='utf-8').replace('workbench-v6', 'workbench-v7')
s = s.replace('chrome.chrome <= 48', 'chrome.chrome <= 51')
s = s.replace('M.stations.push(0.7); M.stations.sort((a, b) => a - b); M.selEta = 0.7;', 'if (!M.stations.includes(0.7)) addStation(0.7); M.selEta = 0.7;')
s = s.replace("page.on('pageerror', e => errors.push(e.message));", "await page.addInitScript(() => window.addEventListener('error', e => { console.error('UNCAUGHT', JSON.stringify(e.error), e.filename, e.lineno); }));\npage.on('console', m => {if(m.type()==='error')console.error('BROWSER',m.text());});\npage.on('pageerror', e => { errors.push(e.stack || e.message); console.error('PAGE ERROR at oracle',oracles.length,e); });")
a = s.index('  /* one draft at a time: while a chord draft')
b = s.index("  record('CAD-01..03", a)
s = s[:a] + '''  /* v7 keeps one owner while allowing read-only curve inspection. */
  await page.locator('#quads [data-cv="te:2"]').focus(); await page.keyboard.press('ArrowUp');
  const owner = await page.evaluate(() => JSON.stringify(M.preview));
  await svgClick('#quads [data-pick="elev"]'); assert.equal(await page.evaluate(() => M.channel), 'elev');
  await page.locator('#opt-curve').selectOption('twist'); assert.equal(await page.evaluate(() => M.channel), 'twist');
  await page.locator('#quads [data-cv="twist:2"]').focus(); await page.keyboard.press('ArrowUp');
  assert.equal(await page.evaluate(() => JSON.stringify(M.preview)), owner, 'inspection cannot replace the TE draft');
  await page.locator('#shape-canvas').focus(); await page.keyboard.press('Escape');
  /* The 2D editor remains a document with actual spline controls and measured seed residual. */
  await click('#station-list [data-eta="0.6"]'); await pclick('#ux-section-open');
  assert.equal(await page.locator('dialog[open]').count(), 0); assert(await page.locator('#doc-station').isVisible());
  assert.match(await text('#doctabs .dt[aria-selected="true"]'), /Station η 0\\.60 · shared-section/);
  assert.equal(await page.locator('#section-canvas svg polyline, #section-canvas svg polygon').count(), 0);
  assert(await page.locator('#section-canvas svg text').count() >= 6);
  const secN = await page.evaluate(() => ({n:CVSEC.n,deg:CVSEC.deg,resid:M.secResid}));
  const residUm = Math.max(...Object.values(secN.resid)) * 89.6 * 1000;
  assert(residUm <= 10, 'the measured initial conversion still meets its 10 µm acceptance');
  assert.match(await text('#section-hud'), new RegExp(`degree ${secN.deg} · ${secN.n} per side`));
  const acceptedSection = await page.evaluate(() => DSL.accepted);
  await page.locator('#section-canvas [data-sec="upper:3"]').focus(); await page.keyboard.press('ArrowUp');
  assert(await page.evaluate(() => !!UX.sectionDraft));
  const secGap = await page.evaluate(() => {const P=M.sectionPreview.P,U=M.secKnots.upper;let min=1e9;for(let k=0;k<=800;k++){const q=BS.evalCurve(P,5,U,k/800);min=Math.min(min,Math.hypot(q[0]-P[3][0],q[1]-P[3][1]));}return min;});
  assert(secGap>1e-3,'a vertex pulls the spline without becoming a through-point');
  await page.locator('#ux-section-cancel').click(); assert.equal(await page.evaluate(() => DSL.accepted),acceptedSection);
  await page.locator('#section-canvas [data-sec="upper:3"]').focus(); await page.keyboard.press('ArrowUp');
  const rv0=await page.evaluate(()=>M.revision);await page.locator('#ux-section-apply').click();
  assert.equal(await page.evaluate(()=>M.revision),rv0+1);assert.equal(await page.evaluate(()=>M.run.current),false);
  await page.locator('[data-doc="doc"]').click();
''' + s[b:]
(p / 'check-mockup-v7.mjs').write_text(s, encoding='utf-8', newline='\n')
print('Derived the v7 CAD oracle; historical v6 assertions preserved in their original file.')
