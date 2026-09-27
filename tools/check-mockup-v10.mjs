#!/usr/bin/env node
// Browser oracle for docs/mockups/workbench-v10.html — the v9 sweep reused and extended (v9 focus contract, docking,
// workspaces, platform switch) plus v10: first run / opening / open failed, focus-safe floats (occlusion measured for
// SVG canvas targets too, no exemption), one precision per quantity, the 200 px dock, the Wing block (typed span and
// chords, running estimates), point types, the section catalog and Save to My sections, undo (GEO-14 in the editor).
// It verifies the review artifact's interaction contracts; it proves nothing about a native app, a kernel or a solver.
// Usage: node tools/check-mockup-v10.mjs [<node_modules dir containing playwright>]
// Writes docs/proof/workbench-v10-browser-check.json and exits 1 when any gate fails.
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'), moduleRoot = process.argv[2];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const file = path.join(repo, 'docs/mockups/workbench-v10.html'), shots = process.env.SHOTS_DIR || await fs.mkdtemp(path.join(os.tmpdir(), 'cfd-workbench-v10-review-'));
const browser = await chromium.launch({ channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
const errors = []; page.on('pageerror', e => errors.push(String(e))); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
await page.goto('file://' + file);
const wait = ms => page.waitForTimeout(ms);
const sel = async (id, v) => { await page.selectOption(id, v); await wait(40); };
// visible (WCAG 2.2 2.4.11, not entirely hidden) = has a box, not inert/hidden, and the element itself is topmost at its centre
// or at one of 8 points around it — for HTML and SVG targets alike (no SVG exemption). Floats are held to a stricter rule below.
// The sticky review harness is chrome, not product: if the page scroll put it over the target, scroll the target clear first.
await page.evaluate(() => { window.__unHarness = a => { let r = a.getBoundingClientRect(), top = document.elementFromPoint(r.left + r.width/2, r.top + r.height/2);
  if ((top && top.closest('.harness')) || r.bottom > innerHeight){ window.scrollBy(0, r.top - 160); r = a.getBoundingClientRect(); }
  const cx = r.left + r.width/2, cy = r.top + r.height/2, rad = Math.min(r.width, r.height) * .35, pts = [[cx, cy], ...[...Array(8)].map((_, i) => [cx + rad*Math.cos(i*Math.PI/4), cy + rad*Math.sin(i*Math.PI/4)])];
  // the mockup paints the selected point once more on top (<use class="raise">, pointer-events none); make it hit-testable while sampling so paint, not pointer routing, is measured
  const u = a.id ? document.querySelector(`use.raise[href="#${a.id}"]`) : null; if (u) u.style.pointerEvents = 'all';
  const hits = pts.map(([x, y]) => document.elementFromPoint(x, y)), own = t => !!t && (t === a || t === u || a.contains(t) || t.closest('label') === a.closest('.field')?.querySelector('label'));
  if (u) u.style.pointerEvents = ''; return [r, hits.find(own) || hits[0], hits.some(own)]; }; });
const focus = () => page.evaluate(() => { const a = document.activeElement; if (!a || a === document.body) return 'BODY'; const [r, top, seen] = window.__unHarness(a),
    onTop = seen,
    visible = r.width > 0 && r.height > 0 && !a.closest('[inert]') && !a.closest('.hidden') && onTop;
  const name = a.getAttribute('aria-label') || a.dataset.tab || a.dataset.k || a.dataset.fk || a.id || a.textContent.trim().slice(0, 30); return `${visible ? '' : 'INVISIBLE:'}${name}${visible ? '' : ` [top=${top?.tagName}.${top?.getAttribute?.('class')} in ${top?.closest?.('[id]')?.id} rect=${[r.left, r.top, r.width, r.height].map(Math.round)}]`}`; });
const dockOf = id => page.evaluate(i => { const t = document.querySelector(`[data-tab="${i}"]`); if (!t) return 'none'; const d = t.closest('.dock'); return d ? d.dataset.dock : t.closest('.float') ? 'float' : '?'; }, id);
const shown = d => page.evaluate(x => getComputedStyle(document.getElementById('dock-' + x)).display !== 'none', d);
const menuClick = async text => { await page.locator('#menu button', { hasText: text }).first().click(); await wait(60); };
const live = () => page.textContent('#sr-live');
const F = {};

// 1. audit across combinations (both platforms), every screen including the new first-run, opening, catalog and save states
const SCREENS = ['start', 'opening', 'openerror', 'workspace', 'section', 'fair', 'catalog', 'save'], LAYOUTS = ['planform', 'precision', 'review', 'float', 'drag', 'right', 'max'];
const rows = [];
for (const plat of ['mac', 'win']) for (const theme of ['dark', 'light', 'contrast']) for (const screen of SCREENS) for (const layout of LAYOUTS) {
  if (plat === 'win' && theme !== 'dark') continue;
  await sel('#h-platform', plat); await sel('#h-theme', theme); await sel('#h-screen', screen); await sel('#h-layout', layout);
  rows.push({ k:`${plat}/${theme}/${screen}/${layout}`, v:(await page.textContent('#h-verdict')).trim(), d:(await page.textContent('#audit-targets')).trim() });
  const snap = (plat === 'mac' && theme === 'dark' && ['planform', 'float', 'precision'].includes(layout)) || (plat === 'mac' && theme === 'light' && layout === 'planform') || (plat === 'win' && layout === 'planform');
  if (snap) await page.locator('.stage').screenshot({ path: path.join(shots, `${screen}-${layout}-${plat === 'win' ? 'win' : theme}.png`) }); }
for (const vp of ['1280', '1920']) for (const s of ['none', 'point', 'station', 'multi']) { await sel('#h-platform', 'mac'); await sel('#h-theme', 'dark'); await sel('#h-viewport', vp); await sel('#h-screen', 'workspace'); await sel('#h-layout', 'precision'); await sel('#h-sel', s);
  rows.push({ k:`${vp}/precision/${s}`, v:(await page.textContent('#h-verdict')).trim(), d:(await page.textContent('#audit-targets')).trim() }); }

// 2. defaults and task presets (v9)
await sel('#h-viewport', '1440'); await sel('#h-theme', 'dark'); await sel('#h-screen', 'workspace'); await sel('#h-layout', 'planform'); await sel('#h-sel', 'point');
F.planformDefault = { left: await shown('left'), right: await shown('right'), bottom: await shown('bottom'), views: await page.$eval('#views', v => v.dataset.layout), firstTab: await page.$eval('#dock-left [aria-selected="true"]', t => t.textContent) };
F.leftShare = await page.evaluate(() => (document.getElementById('dock-left').offsetWidth / document.getElementById('body').offsetWidth).toFixed(3));
F.tabWiring = await page.evaluate(() => { const t = document.querySelector('#dock-left [aria-selected="true"]'), p = document.getElementById(t.getAttribute('aria-controls')); return `${t.id} -> ${p?.id} labelledby=${p?.getAttribute('aria-labelledby')}`; });
F.hoverRule = await page.evaluate(() => [...document.styleSheets].flatMap(s => [...s.cssRules]).some(r => /g\[role="button"\]:hover/.test(r.selectorText || '')));

// 3. focus after every action (v9 a11y predicate, reused)
await page.locator('#dock-left [data-tab="browser"]').click(); await wait(40);
F.browserRows = await page.$$eval('#dock-left .row-btn', b => b.map(x => x.dataset.fk).join(' '));
await page.locator('#dock-left [data-fk="tree:st2"]').focus(); await page.keyboard.press('Enter'); await wait(60); F.focusAfterBrowserEnter = await focus(); F.browserSelects = await page.evaluate(() => JSON.stringify(S)); await page.locator('#v-plan [data-k="te1"]').focus(); await wait(40);
await page.locator('#dock-left [data-tab="properties"]').click(); await wait(40);
const aft = page.locator('#dock-left [data-fk="te1:aft"]'); await aft.fill('170'); await aft.press('Tab'); await wait(60); F.focusAfterCommitTab = await focus(); F.commitApplied = await page.evaluate(() => TE[1].p[1]);
await page.locator('#dock-left [data-tab="properties"]').focus(); await page.keyboard.press('Shift+F10'); await wait(40);
F.paneMenuRoles = await page.$$eval('#menu button', b => b.map(x => `${x.getAttribute('role')}${x.hasAttribute('aria-checked') ? '(' + x.getAttribute('aria-checked') + ')' : ''}:${x.firstChild.textContent.trim()}`).join(' | '));
await menuClick('Wide'); F.sizeViaMenu = await page.evaluate(() => document.getElementById('dock-left').offsetWidth); F.focusAfterSize = await focus();
await page.locator('#dock-left [data-tab="browser"]').focus(); await page.keyboard.press('Shift+F10'); await menuClick('Close'); F.focusAfterClose = await focus(); F.browserAfterClose = await dockOf('browser');
await page.locator('#dock-left [data-tab="properties"]').focus(); await page.keyboard.press('Shift+F10'); await menuClick('Close'); F.focusAfterLastClose = await focus(); F.leftAutoClosed = !(await shown('left'));
await page.click('[data-menu="window"] >> nth=0'); await menuClick('Properties'); F.focusAfterShowProperties = await focus(); F.propertiesBack = await dockOf('properties');
await page.keyboard.press('Meta+j'); await wait(60); await page.locator('#dock-bottom [data-tab="points"]').focus(); await page.keyboard.press('Meta+Shift+m'); await wait(60);
F.maxed = await page.locator('.maxed').count(); F.inertWhileMaxed = await page.evaluate(() => ['center', 'dock-left', 'appbar'].map(i => document.getElementById(i).inert).join(',')); F.focusWhileMaxed = await focus();
let inMax = true; for (let i = 0; i < 5; i++){ await page.keyboard.press('Tab'); inMax = inMax && await page.evaluate(() => !!document.activeElement.closest('.maxed')); } F.tabStaysInMaxedx5 = inMax; await page.locator('.maxed [role="tab"][aria-selected="true"]').focus();
await page.keyboard.press('Escape'); await wait(60); F.restoredByEscape = (await page.locator('.maxed').count()) === 0; F.focusAfterRestore = await focus();
await page.locator('#dock-left [data-tab="properties"]').focus(); await page.keyboard.press('Shift+F10'); await menuClick('Float'); F.floating = await dockOf('properties'); F.focusAfterFloat = await focus();
F.floatClearOfLeftDock = await page.evaluate(() => { const f = document.querySelector('.float').getBoundingClientRect(), d = document.getElementById('dock-left').getBoundingClientRect(); return getComputedStyle(document.getElementById('dock-left')).display === 'none' || f.left >= d.right - 1; });
const fx0 = await page.evaluate(() => L.float[0].x); await page.keyboard.press('Alt+ArrowRight'); await page.keyboard.press('Alt+Shift+ArrowDown'); F.floatAltArrow = `${fx0.toFixed(0)} -> ${(await page.evaluate(() => L.float[0].x)).toFixed(0)}`; F.focusAfterFloatMove = await focus();
await page.keyboard.press('Shift+F10'); await menuClick('Top right'); F.focusAfterPosition = await focus();
await page.keyboard.press('Escape'); await wait(60); F.escapeDocksBack = await dockOf('properties'); F.focusAfterEscapeDock = await focus();
await page.locator('#dock-left [data-tab="properties"]').focus(); await page.keyboard.press('Shift+F10'); await menuClick('Move to right side bar'); F.focusAfterMoveRight = await focus();
await page.keyboard.press('Shift+F10'); await menuClick('Move to left side bar'); F.focusAfterMoveLeft = await focus();
await page.locator('#split-left').focus(); await page.keyboard.press('End'); F.splitEnd = await page.evaluate(() => document.getElementById('dock-left').offsetWidth); await page.keyboard.press('Home'); F.splitHome = await page.evaluate(() => document.getElementById('dock-left').offsetWidth);
F.gridRoles = await page.evaluate(() => { const t = document.querySelector('#dock-bottom table'); return `${t.getAttribute('role')} rows=${t.querySelectorAll('tbody tr').length} tabbable=${[...t.querySelectorAll('tbody tr')].filter(r => r.tabIndex === 0).length}`; });
await page.locator('#dock-bottom tbody tr[tabindex="0"]').focus(); await page.keyboard.press('ArrowDown'); await page.keyboard.press('Space'); await wait(60); F.gridSpaceSelects = await page.$eval('#dock-left .phead h2', e => e.textContent); F.focusAfterGridSelect = await focus();
const cell = page.locator('#dock-bottom input[aria-label^="Leading edge Tip end Aft"]'); await cell.fill('55'); await cell.press('Enter'); await wait(60); F.gridTypedEdit = await page.evaluate(() => LE[2].p[1]); F.focusAfterGridEdit = await focus();
await page.locator('#v-plan [data-k="le1"]').focus(); const f6 = []; for (let i = 0; i < 4; i++){ await page.keyboard.press('F6'); f6.push(await focus()); } F.f6Cycle = f6.join(' -> ');
await page.locator('#dock-left input').first().focus(); await page.keyboard.press('Meta+b'); F.cmdBIgnoredWhileTyping = await shown('left');
await sel('#h-platform', 'win'); await page.locator('#v-plan [data-k="le1"]').focus(); await page.keyboard.press('Meta+b'); F.winIgnoresMeta = await shown('left'); await page.keyboard.press('Control+b'); await wait(60); F.winCtrlBHides = !(await shown('left')); await page.keyboard.press('Control+b'); await wait(60);
F.winWindowMenuInApp = await page.locator('.appbar [data-menu="window"]').isVisible();
await sel('#h-platform', 'mac');
await page.click('.osbar [data-menu="window"]'); await menuClick('Review'); F.reviewPreset = await page.$eval('#views', v => v.dataset.layout); F.focusAfterWorkspace = await focus();
await page.keyboard.press('Meta+b'); await wait(60); await page.click('.osbar [data-menu="window"]'); await menuClick('Planform'); await page.click('.osbar [data-menu="window"]'); await menuClick('Review'); F.reviewRemembered = await shown('left');
await page.click('.osbar [data-menu="window"]'); await menuClick('Reset layout'); F.reviewResetHidesLeft = !(await shown('left'));
await page.click('.osbar [data-menu="window"]'); await menuClick('Planform');
await page.click('.osbar [data-menu="window"]'); await page.keyboard.press('Tab'); await wait(40); F.tabClosesMenu = await page.locator('#menu').isHidden(); F.focusAfterMenuTab = await focus();
await page.locator('#v-plan [data-k="st1"]').focus(); await page.keyboard.press('Enter'); await wait(80); F.focusInSection = await focus();
await page.keyboard.press('Escape'); await wait(80); F.escBackToWorkspace = await page.$eval('html', h => h.dataset.screen); F.focusAfterEscSection = await focus();

// 4. occlusion: every focus target in the model area, with a float open, from every float position — SVG included
const occl = [];
for (const vp of ['1280', '1440', '1920']) for (const scr of ['workspace', 'section', 'catalog', 'start']) for (const lay of ['float']) for (const pos of ['centre', 'topright', 'bottomright']) {
  await sel('#h-viewport', vp); await sel('#h-screen', scr); await sel('#h-layout', lay);
  const r = await page.evaluate(p => { const out = { n:0, bad:[], outside:0, overlap:0 }, targets = () => [...document.querySelectorAll('#center g[role="button"], #center button, #center input, #center [tabindex="0"]')].filter(e => { const b = e.getBoundingClientRect(); return b.width && b.height && !e.closest('.hidden'); });
    const keys = targets().map((e, i) => e.dataset.k ? `[data-k="${e.dataset.k}"]` : e.id ? '#' + e.id : null).filter(Boolean);
    for (const key of keys){ const s = floatSpot(p); L.float[0].x = s.x; L.float[0].y = s.y; layout(); const e = document.querySelector('#center ' + key); if (!e) continue; e.focus({ preventScroll:true }); out.n++; const fe = document.activeElement; if (fe === document.body){ out.bad.push(key + " lost focus"); continue; } const [b, top, seen] = window.__unHarness(fe); if (!seen) out.bad.push(key + ' under ' + (top?.closest('.float') ? 'float' : top?.tagName + '.' + top?.getAttribute('class')));
      const f = document.querySelector('.float').getBoundingClientRect(), c = document.getElementById('center').getBoundingClientRect(); if (f.left < c.left - 1 || f.right > c.right + 1 || f.top < c.top - 1 || f.bottom > c.bottom + 1) out.outside++;
      if (f.left < b.right && f.right > b.left && f.top < b.bottom && f.bottom > b.top) out.overlap++; }
    return out; }, pos);
  occl.push({ k:`${vp}/${scr}/${pos}`, n:r.n, bad:r.bad.length, outside:r.outside, overlap:r.overlap, sample:r.bad.slice(0, 3) }); }
F.occlusion = { configs: occl.length, targetsChecked: occl.reduce((a, o) => a + o.n, 0), occluded: occl.reduce((a, o) => a + o.bad, 0), floatOverlapsFocusedTarget: occl.reduce((a, o) => a + o.overlap, 0), floatOutsideModelArea: occl.reduce((a, o) => a + o.outside, 0), failures: occl.filter(o => o.bad || o.outside || o.overlap).slice(0, 5) };
// keyboard Tab walk through the plan with the float at centre (real keyboard path, no programmatic reset)
await sel('#h-viewport', '1440'); await sel('#h-screen', 'workspace'); await sel('#h-layout', 'float');
await page.locator('#v-plan g[role="button"]').first().focus(); const walk = []; for (let i = 0; i < 14; i++){ walk.push(await focus()); await page.keyboard.press('Tab'); }
F.tabWalkWithFloat = { steps: walk.length, invisible: walk.filter(w => w.startsWith('INVISIBLE') || w === 'BODY').length, moveAnnounced: /moved so it doesn't cover/.test(await live()) || 'no move needed' };

// 5. precision: one precision per quantity; a fine nudge is visible everywhere
await sel('#h-layout', 'precision'); await sel('#h-sel', 'point');
await page.locator('#v-plan [data-k="le1"]').focus(); await page.keyboard.press('Meta+ArrowDown'); await wait(60);
F.fineNudge = await page.evaluate(() => ({ model: LE[1].p[1].toFixed(2), props: document.querySelector('#dock-left [data-fk="le1:aft"]').value, grid: document.querySelector('#dock-bottom input[data-fk="cell:le1:b"]').value, canvas: document.querySelector('#v-plan [data-k="le1"]').getAttribute('aria-label').match(/aft ([\d.]+)/)[1] }));

// 6. density at the 200 px dock and fit at 1280
const dens = [];
for (const scr of ['workspace', 'section']) for (const s of ['none', 'point', 'station', 'multi']) { await sel('#h-screen', scr); await sel('#h-layout', 'planform'); await sel('#h-sel', s);
  for (const vp of ['1280', '1440']) { await sel('#h-viewport', vp);
    dens.push(await page.evaluate(k => { L.sizes.left = 200; layout(); const d = document.getElementById('dock-left'), sc = d.querySelector('.pscroll'), w = d.querySelector('.wing');
      const clipped = [...d.querySelectorAll('input, select')].filter(i => i.offsetWidth && i.scrollWidth > i.clientWidth + 1).map(i => `${i.getAttribute('aria-label') || i.id}=${i.value}`);
      return { k, clipped, selectionFits: sc.scrollHeight <= sc.clientHeight + 1, selectionH: sc.scrollHeight, visibleH: sc.clientHeight, wingFits: w.scrollHeight <= w.clientHeight + 1 }; }, `${scr}/${s}/${vp}`)); } }
F.narrowDock = { cases: dens.length, clippedFields: dens.reduce((a, d) => a + d.clipped.length, 0), clipSamples: dens.flatMap(d => d.clipped).slice(0, 4), selectionScrolls: dens.filter(d => !d.selectionFits).map(d => `${d.k} ${d.selectionH}/${d.visibleH}`), wingScrolls: dens.filter(d => !d.wingFits).map(d => d.k) };
await sel('#h-viewport', '1440');

// 7. running estimates: always shown (workspace, section, every selection) and live during a drag
const always = []; for (const scr of ['workspace', 'section', 'fair', 'catalog', 'save']) for (const s of ['none', 'point', 'station', 'multi']) { await sel('#h-screen', scr); await sel('#h-layout', 'planform'); await sel('#h-sel', s);
  always.push(await page.evaluate(() => { const w = document.querySelector('#dock-left .wing'); return !!w && w.textContent.includes('Estimates') && ['Mean chord', 'MAC', 'Max t/c', 'Aspect ratio', 'Area'].every(t => w.textContent.includes(t)); })); }
F.estimatesAlways = `${always.filter(Boolean).length}/${always.length}`;
await sel('#h-screen', 'section'); await sel('#h-sel', 'point'); F.dimsReadOnlyInSection = await page.$$eval('#dock-left .wing input', i => i.length === 0); F.sectionRows = await page.$$eval('#dock-left .wing dt', d => d.map(x => x.textContent).join(' | '));
F.macDefined = await page.$eval('#dock-left .wing dt[title^="Mean aerodynamic"]', d => d.title);
F.noEstimatesWithoutDoc = await (async () => { await sel('#h-screen', 'start'); return page.evaluate(() => !document.querySelector('#dock-left .wing')); })();
await sel('#h-screen', 'workspace'); await sel('#h-sel', 'none');
const est = () => page.$eval('#dock-left .wing', w => [...w.querySelectorAll('dd')].map(d => d.textContent).join(' '));
const beforeDrag = await est(); const tp = await page.locator('#v-plan [data-k="te1"] .hit').boundingBox();
await page.mouse.move(tp.x + tp.width/2, tp.y + tp.height/2); await page.mouse.down(); await page.mouse.move(tp.x + tp.width/2, tp.y + tp.height/2 + 40, { steps: 4 }); await wait(40);
const duringDrag = await est(); await page.mouse.up(); await wait(60);
F.estimatesLiveDuringDrag = { changedMidDrag: beforeDrag !== duringDrag, before: beforeDrag.slice(0, 60), during: duringDrag.slice(0, 60) };

// 8. driving dimensions: span, root chord, tip chord — commit, undo, errors, Escape, tip closes
await sel('#h-sel', 'none');
const dim = k => page.locator(`#dock-left [data-fk="dim:${k}"]`);
const area0 = await page.evaluate(() => area()); await dim('span').fill('900'); await dim('span').press('Enter'); await wait(60);
F.spanCommit = await page.evaluate(a0 => ({ span: (2*half).toFixed(2), rootChord: chord(0).toFixed(2), areaGrew: area() > a0, undoDepth: UNDO.length }), area0); F.focusAfterSpanCommit = await focus();
await page.locator('#v-plan [data-k="le1"]').focus(); await page.keyboard.press('Meta+z'); await wait(60); F.spanUndo = await page.evaluate(() => (2*half).toFixed(2)); F.focusAfterUndo = await focus();
await dim('root').fill('200'); await dim('root').press('Tab'); await wait(60); F.rootCommit = await page.evaluate(() => ({ root: chord(0).toFixed(2), tip: chord(1).toFixed(2) })); F.focusAfterRootCommitTab = await focus();
await dim('tip').fill('70'); await dim('tip').press('Enter'); await wait(60); F.tipCommit = await page.evaluate(() => ({ root: chord(0).toFixed(2), tip: chord(1).toFixed(2) }));
await dim('span').fill('-5'); await dim('span').press('Enter'); await wait(40); F.spanNegative = await page.evaluate(() => ({ span: (2*half).toFixed(2), err: document.querySelector('#dock-left [data-fk="dim:span"]').closest('.field').querySelector('.ferr').textContent, invalid: document.querySelector('#dock-left [data-fk="dim:span"]').getAttribute('aria-invalid') }));
F.focusAfterInvalid = await focus();
await dim('span').fill('abc'); await dim('span').press('Enter'); await wait(40); F.spanText = await page.evaluate(() => (2*half).toFixed(2));
await dim('span').press('Escape'); await wait(40); F.escapeReverts = await page.evaluate(() => ({ value: document.querySelector('#dock-left [data-fk="dim:span"]').value, errHidden: document.querySelector('#dock-left [data-fk="dim:span"]').closest('.field').querySelector('.ferr').classList.contains('hidden') }));
F.dimTargets = await page.$$eval('#dock-left .field.dim input', i => i.map(x => `${x.getBoundingClientRect().width.toFixed(0)}x${x.getBoundingClientRect().height.toFixed(0)}`).join(' '));
await sel('#h-sel', 'point'); await page.evaluate(() => { S = { kind:'plan', ids:['te2'] }; refresh(); });
const teAft = page.locator('#dock-left [data-fk="te2:aft"]'); await teAft.fill('48'); await teAft.press('Enter'); await wait(60); F.tipCloses = await page.evaluate(() => [...document.querySelectorAll('#dock-left .wing .fixed')].map(x => x.textContent).join('') || 'field still editable');

// 9. point type: anchor <-> control point, local redraw, undo; symmetric tangent; locked named points
await sel('#h-screen', 'workspace'); await sel('#h-sel', 'point');
const leAt = () => page.evaluate(() => [120, 245, 330].map(y => atSpan(LE, y).toFixed(3)).join(','));
const le0 = await leAt(); await page.locator('#dock-left [data-fk="le1:type"]').focus(); await page.locator('#dock-left [data-fk="le1:type"]').selectOption('control'); await wait(60);
F.toControl = { curveChanged: le0 !== await leAt(), glyph: await page.$eval('#v-plan [data-k="le1"] .cv', e => e.tagName), handlesGone: await page.locator('#v-plan [data-k^="le1:"]').count() === 0, handlesPane: await page.locator('#dock-left summary', { hasText: 'Handles' }).count(), polygon: await page.locator('#v-plan .ctlpoly').count(), trailingEdgeUntouched: await page.evaluate(() => TE[1].p.join(',')) };
F.focusAfterTypeChange = await focus();
await page.keyboard.press('Meta+z'); await wait(10); await page.locator('#v-plan [data-k="le1"]').focus(); await page.keyboard.press('Meta+z'); await wait(60); F.typeUndo = { type: await page.evaluate(() => LE[1].type || 'anchor'), curveRestored: le0 === await leAt() };
await page.locator('#dock-left [data-fk="le1:kind"]').selectOption('symmetric'); await wait(40); await page.locator('#dock-left [data-fk="le1:hin:len"]').fill('80'); await page.locator('#dock-left [data-fk="le1:hin:len"]').press('Enter'); await wait(60);
F.symmetric = await page.evaluate(() => [Math.hypot(...LE[1].hin).toFixed(2), Math.hypot(...LE[1].hout).toFixed(2)].join(' = '));
F.lockedTypes = [];
for (const [scr, s, sub] of [['workspace', 'te0', 'plan'], ['workspace', 'te2', 'plan'], ['section', 'u3', 'sec'], ['section', 'nose', 'sec']]) { await sel('#h-screen', scr); await page.evaluate(([k, sb]) => { S = sb === 'plan' ? { kind:'plan', ids:[k] } : { kind:'sec', key:k, part:'point' }; refresh(); }, [s, sub]);
  F.lockedTypes.push(`${s}: ${await page.evaluate(() => document.querySelector('#dock-left .pscroll .fixed')?.textContent || 'SELECT')}`); }
await sel('#h-screen', 'section'); await sel('#h-sel', 'point'); await page.evaluate(() => { S = { kind:'sec', key:'u2', part:'point' }; refresh(); });
const t0 = await page.evaluate(() => yAt(curvePts(upperA()), .72)); await page.locator('#dock-left [data-fk="u2:type"]').focus(); await page.locator('#dock-left [data-fk="u2:type"]').selectOption('control'); await wait(60);
F.sectionControl = { glyph: await page.$eval('#v-sec [data-k="u2"] .cv', e => e.tagName), lowerUntouched: await page.evaluate(() => JSON.stringify(LO) === JSON.stringify(LO0.map(a => ({ ...a })))), upperChangedAtAft: t0 !== await page.evaluate(() => yAt(curvePts(upperA()), .72)) }; F.focusAfterSectionTypeChange = await focus(); void { };

// 10. catalog picker: keyboard, combobox/listbox, pending rows, ghost preview, replace, undo, focus restore
await sel('#h-screen', 'section'); await sel('#h-sel', 'none');
await page.locator('#sec-menu').focus(); await page.keyboard.press('Enter'); await wait(60); F.secMenuItems = await page.$$eval('#menu button', b => b.map(x => x.firstChild.textContent.trim()).join(' | '));
await menuClick('Replace from catalog'); await wait(80); F.focusInCatalog = await focus();
F.comboAria = await page.$eval('#cat-q', q => `${q.getAttribute('role')} expanded=${q.getAttribute('aria-expanded')} controls=${q.getAttribute('aria-controls')} active=${q.getAttribute('aria-activedescendant')}`);
F.listboxShape = await page.evaluate(() => { const l = document.getElementById('cat-list'); return `${l.getAttribute('role')} groups=${l.querySelectorAll('[role="group"]').length} options=${l.querySelectorAll('[role="option"]').length} disabled=${l.querySelectorAll('[aria-disabled="true"]').length} families=${[...l.querySelectorAll('[role="group"]')].map(g => g.getAttribute('aria-label')).join('/')}`; });
F.ghostWhileOpen = await page.locator('#v-sec .ghost.preview').count();
await page.keyboard.press('ArrowDown'); await wait(30); F.arrowMovesActive = await page.$eval('#cat-q', q => q.getAttribute('aria-activedescendant'));
await page.keyboard.type('eppler e817'); await wait(60); F.pending = await page.evaluate(() => ({ active: document.getElementById('cat-q').getAttribute('aria-activedescendant'), replaceDisabled: document.getElementById('cat-ok').getAttribute('aria-disabled'), detail: document.getElementById('cat-detail').textContent, ghost: document.querySelectorAll('#v-sec .ghost.preview').length }));
const up0 = await page.evaluate(() => JSON.stringify(UP)); await page.keyboard.press('Enter'); await wait(60); F.pendingNotApplied = up0 === await page.evaluate(() => JSON.stringify(UP)) && await page.locator('#cat-dialog').isVisible();
await page.locator('#cat-q').fill('zzz'); await wait(40); F.noMatch = await page.textContent('#cat-detail');
await page.locator('#cat-q').fill('0012'); await wait(60); F.fitDeviation = await page.textContent('#cat-detail');
await page.keyboard.press('Enter'); await wait(80);
F.replaced = await page.evaluate(() => ({ screen: root.dataset.screen, src: document.getElementById('src-chip').textContent, crest: UP[0].p.map(v => v.toFixed(3)).join(','), tc: (thickness(upperA(), lowerA())[0]*100).toFixed(1) })); F.focusAfterReplace = await focus();
await page.locator('#v-sec g[role="button"]').nth(1).focus(); await page.keyboard.press('ArrowUp'); await wait(40); F.chipAfterEdit = await page.textContent('#src-chip');
await page.locator('#sec-menu').focus(); await page.keyboard.press('Enter'); await menuClick('Save to My sections'); await wait(80); F.focusInSave = await focus();
F.saveProv = await page.textContent('#save-prov');
await page.locator('#save-name').fill(''); await page.keyboard.press('Enter'); await wait(40); F.saveEmpty = await page.textContent('#save-err'); F.focusAfterSaveError = await focus();
await page.locator('#save-name').fill('Main section'); await page.keyboard.press('Enter'); await wait(40); F.saveDuplicate = await page.textContent('#save-err');
await page.locator('#save-name').fill('Kite mid, reflexed'); await page.keyboard.press('Enter'); await wait(80); F.saved = await live(); F.focusAfterSave = await focus();
await page.locator('#sec-menu').focus(); await page.keyboard.press('Enter'); await menuClick('Replace from catalog'); await wait(80); await page.locator('#cat-q').fill('reflexed'); await wait(60);
F.savedInCatalog = await page.$$eval('#cat-list [role="option"]', o => o.map(x => x.textContent).join(' || '));
await page.keyboard.press('Escape'); await wait(60); F.focusAfterCatalogEscape = await focus(); F.catalogClosed = await page.locator('#cat-dialog').isHidden();
await page.locator('#v-sec g[role="button"]').first().focus(); await page.keyboard.press('Meta+z'); await wait(60); await page.keyboard.press('Meta+z'); await wait(60);
F.undoReplace = await page.evaluate(() => ({ src: document.getElementById('src-chip').classList.contains('hidden') ? 'hidden' : document.getElementById('src-chip').textContent, crest: UP[0].p.map(v => v.toFixed(3)).join(',') }));
F.pointerOptionSize = await (async () => { await sel('#h-screen', 'catalog'); return page.$$eval('#cat-list [role="option"]', o => Math.min(...o.map(x => x.getBoundingClientRect().height)).toFixed(0)); })();

// 11. GEO-14 undo contract in the section editor
await sel('#h-screen', 'workspace'); await sel('#h-layout', 'planform');
const depth = () => page.evaluate(() => UNDO.length);
await page.locator('#v-plan [data-k="st1"]').focus(); await page.keyboard.press('Enter'); await wait(80); const d0 = await depth();
for (let i = 0; i < 3; i++) await page.keyboard.press('ArrowUp'); await page.keyboard.press('ArrowRight'); await page.locator('#sec-finish').click(); await wait(80); F.finishAddsOne = `${d0} -> ${await depth()}`;
await page.locator('#v-plan [data-k="st1"]').focus(); await page.keyboard.press('Enter'); await wait(80); const d1 = await depth(); await page.keyboard.press('ArrowUp'); await page.locator('#sec-cancel').click(); await wait(80); F.cancelKeepsDepth = `${d1} -> ${await depth()}`;

// 12. first run, opening, open failed
await sel('#h-screen', 'start'); F.startFocusTarget = await page.evaluate(() => centerTarget()?.textContent.trim().slice(0, 20));
await page.locator('#recent button', { hasText: 'kite-wing-850.foil' }).click(); await wait(40); F.openingState = await page.$eval('html', h => h.dataset.screen); F.focusWhileOpening = await focus();
await wait(1100); F.afterOpen = { screen: await page.$eval('html', h => h.dataset.screen), title: await page.textContent('#fname') }; F.focusAfterOpen = await focus();
await sel('#h-screen', 'start'); await page.locator('#recent button', { hasText: 'kite-wing-850.foil' }).click(); await wait(40); await page.keyboard.press('Escape'); await wait(60); F.cancelOpen = await page.$eval('html', h => h.dataset.screen); F.focusAfterCancelOpen = await focus();
await wait(1000); F.cancelHolds = await page.$eval('html', h => h.dataset.screen);
await page.locator('#recent button', { hasText: 'old-kite-wing.foil' }).click(); await wait(60); F.openError = { screen: await page.$eval('html', h => h.dataset.screen), alert: (await page.textContent('#open-error')).trim().slice(0, 60) }; F.focusAfterOpenError = await focus();
await sel('#h-screen', 'start'); await page.locator('.card', { hasText: 'New foil' }).click(); await wait(60); F.newFoil = await page.evaluate(() => ({ title: document.getElementById('fname').textContent, straight: LE.every(a => a.p[1] === 0), rootChord: chord(0).toFixed(1) })); F.focusAfterNewFoil = await focus();
F.undoDisabledWithoutDoc = await (async () => { await sel('#h-screen', 'start'); return page.$eval('#undo', b => b.disabled); })();

// 12b. accessibility repair cycle 1 — each assertion names the lens finding it guards
await sel('#h-viewport', '1440'); await sel('#h-screen', 'workspace'); await sel('#h-layout', 'precision'); await sel('#h-sel', 'point');
const aftIn = page.locator('#dock-left [data-fk="le1:aft"]'); const le1 = await page.evaluate(() => LE[1].p[1]);
await aftIn.fill('abc'); await aftIn.press('Enter'); await wait(40);
F.a11y1_textRejected = await page.evaluate(v => { const i = document.querySelector('#dock-left [data-fk="le1:aft"]'); return { unchanged: LE[1].p[1] === v, invalid: i.getAttribute('aria-invalid'), message: document.getElementById(i.getAttribute('aria-describedby')).textContent }; }, le1);
F.focusAfterInvalidField = await focus();
await aftIn.fill('12abc'); await aftIn.press('Enter'); await wait(40); F.a11y1_partialNumberRejected = await page.evaluate(v => LE[1].p[1] === v, le1);
await aftIn.press('Escape'); await wait(30); F.a11y1_escapeRestores = await page.evaluate(() => { const i = document.querySelector('#dock-left [data-fk="le1:aft"]'); return `${i.value} invalid=${i.getAttribute('aria-invalid')}`; });
const lenIn = page.locator('#dock-left [data-fk="le1:hin:len"]'); await lenIn.fill('-4'); await lenIn.press('Enter'); await wait(40); F.a11y1_negativeLength = await page.evaluate(() => document.querySelector('#dock-left [data-fk="le1:hin:len"]').getAttribute('aria-invalid'));
const cellIn = page.locator('#dock-bottom input[data-fk="cell:le1:b"]'); await cellIn.fill('x'); await cellIn.press('Enter'); await wait(40);
F.a11y1_gridCell = await page.evaluate(v => ({ unchanged: LE[1].p[1] === v, invalid: document.querySelector('#dock-bottom input[data-fk="cell:le1:b"]').getAttribute('aria-invalid'), message: document.getElementById('grid-err').textContent }), le1);
await sel('#h-screen', 'catalog'); await page.locator('#cat-q').focus(); await page.keyboard.type('zzz'); await wait(450); F.a11y2_noMatchStatus = await page.textContent('#cat-status');
await page.locator('#cat-q').fill(''); await page.keyboard.type('naca'); await wait(450); F.a11y2_countStatus = await page.textContent('#cat-status');
await sel('#h-screen', 'workspace'); await sel('#h-layout', 'planform'); await page.locator('#v-plan [data-k="st1"]').focus(); await page.keyboard.press('Enter'); await wait(80);
const u1 = await page.evaluate(() => UP[0].p.join(',')); await page.keyboard.press('ArrowUp'); await wait(30); await page.keyboard.press('Escape'); await wait(60);
F.a11y3_escapeKeepsEdit = await page.evaluate(u => ({ screen: root.dataset.screen, kept: UP[0].p.join(',') !== u }), u1); F.focusAfterEscapeWithEdits = await focus();
await page.locator('#sec-cancel').click(); await wait(80);
for (const scr of ['workspace', 'section']) { await sel('#h-screen', scr); await sel('#h-layout', 'float'); await sel('#h-sel', 'point');
  F['a11y4_floatReachable_' + scr] = await page.evaluate(() => { const pb = document.querySelector('.float .pane-body'), fr = document.querySelector('.float').getBoundingClientRect(); pb.scrollTop = pb.scrollHeight; const last = [...pb.querySelectorAll('.wing dd')].at(-1).getBoundingClientRect(); const ok = last.bottom <= fr.bottom + 1 && last.top >= fr.top; pb.scrollTop = 0; return ok; }); }
await sel('#h-screen', 'workspace'); await sel('#h-layout', 'planform'); await page.evaluate(() => { LE[1].p[1] = 20; pushUndo('set aft'); refresh(); }); const undoDepth0 = await page.evaluate(() => UNDO.length);
await page.locator('#v-plan [data-k="st1"]').focus(); await page.keyboard.press('Enter'); await wait(80); await page.keyboard.press('Meta+z'); await wait(40);
F.a11y5_undoStopsAtDraft = await page.evaluate(d => ({ depth: UNDO.length === d, said: document.getElementById('sr-live').textContent }), undoDepth0); await page.locator('#sec-cancel').click(); await wait(60);
await sel('#h-viewport', '1280'); await sel('#h-screen', 'workspace'); await sel('#h-layout', 'float');
F.a11y6_narrowModelArea = await page.evaluate(() => { L.sizes.left = 420; L.shown.right = true; L.sizes.right = 420; const s = floatSpot('centre'); L.float[0].x = s.x; L.float[0].y = s.y; layout(); let n = 0, overlap = 0, docked = 0, moved = 0;
  for (const key of [...document.querySelectorAll('#v-plan g[role="button"]')].map(e => e.dataset.k)){ if (!L.float.length){ L.float.push({ id:'properties', from:'left', x:s.x, y:s.y }); L.left = L.left.filter(x => x !== 'properties'); layout(); }
    const e = document.querySelector(`#v-plan [data-k="${key}"]`); e.focus({ preventScroll:true }); n++; const fe = document.activeElement, b = fe.getBoundingClientRect(), f = document.querySelector('.float')?.getBoundingClientRect(); if (!L.float.length) docked++; else if (L.float[0].x !== s.x || L.float[0].y !== s.y) moved++;
    if (f && f.left < b.right && f.right > b.left && f.top < b.bottom && f.bottom > b.top) overlap++; }
  return { targets:n, overlap, docked, moved }; });
await sel('#h-viewport', '1440'); await sel('#h-screen', 'opening'); F.a11y8_openingSemantics = await page.evaluate(() => ({ busy: document.getElementById('opening').getAttribute('aria-busy'), cancelName: document.getElementById('open-cancel').getAttribute('aria-label') }));
await sel('#h-layout', 'planform'); await sel('#h-screen', 'section'); await sel('#h-sel', 'multi'); const mh = page.locator('#dock-left [data-fk="multi:h"]'); await mh.fill('5'); await mh.press('Enter'); await wait(60);
F.a11y9_multiHeight = await page.evaluate(() => [UP[0], UP[1]].map(a => (a.p[1]*chord(stations()[selStation].eta)).toFixed(2)).join(' = '));
F.a11y10_teGapInside = await page.evaluate(() => { const t = [...document.querySelectorAll('#v-sec text')].find(x => x.textContent.startsWith('TE gap')), b = t.getBBox(); return b.x >= 0 && b.x + b.width <= 1200; });
await sel('#h-screen', 'catalog'); await page.locator('#v-sec g[role="button"]').first().focus(); const f6d = []; for (let i = 0; i < 6; i++){ await page.keyboard.press('F6'); f6d.push(await page.evaluate(() => !!document.activeElement.closest('.dialog'))); } F.a11y12_f6ReachesDialog = f6d.includes(true);

// 13. placeholder commands say so
await sel('#h-screen', 'workspace'); await page.locator('button[data-stub="Add station"]').click(); await wait(80); F.stubAnnounces = await live();

const failing = rows.filter(r => !r.v.includes('0 contrast fail · 0 small target'));
const focusFailures = Object.entries(F).filter(([k, v]) => k.startsWith('focus') && (String(v) === 'BODY' || String(v).startsWith('INVISIBLE')));
const O = F.occlusion, gates = {
  'every harness combination: 0 contrast failures, 0 targets under 24 px': failing.length === 0,
  'focus lands visibly after every action': focusFailures.length === 0,
  'no focused model-area target hidden by or overlapped by a float': O.occluded === 0 && O.floatOverlapsFocusedTarget === 0 && O.floatOutsideModelArea === 0 && F.tabWalkWithFloat.invisible === 0,
  'no clipped value at the 200 px dock; selection and Wing fit at 1280': F.narrowDock.clippedFields === 0 && !F.narrowDock.selectionScrolls.length && !F.narrowDock.wingScrolls.length,
  'fine nudge visible everywhere': new Set(Object.values(F.fineNudge)).size === 1,
  'estimates on every screen with a foil, live while dragging': F.estimatesAlways.split('/')[0] === F.estimatesAlways.split('/')[1] && F.estimatesLiveDuringDrag.changedMidDrag,
  'typed dimensions commit, undo, reject, revert': F.spanCommit.span === '900.00' && F.spanUndo === '850.00' && F.spanNegative.span === '850.00' && F.escapeReverts.errHidden && F.tipCloses.startsWith('Tip closes'),
  'point type changes locally and undoes': F.toControl.curveChanged && F.toControl.trailingEdgeUntouched === '230,166' && F.typeUndo.curveRestored && F.sectionControl.lowerUntouched,
  'catalog: pending not applied, replace, provenance, undo': F.pendingNotApplied && F.replaced.src.startsWith('Catalog original') && F.chipAfterEdit.startsWith('Modified from') && F.undoReplace.src === 'hidden',
  'section editor: Finish adds one undo step, Cancel none': F.finishAddsOne === '0 -> 1' && F.cancelKeepsDepth === '1 -> 1' && F.dimsReadOnlyInSection,
  'first run, opening, cancel, open failed': F.afterOpen.screen === 'workspace' && F.cancelHolds === 'start' && F.openError.screen === 'openerror',
  'a11y cycle 1: invalid numbers refused with a message (3.3.1)': F.a11y1_textRejected.unchanged && F.a11y1_textRejected.invalid === 'true' && /^Enter a number/.test(F.a11y1_textRejected.message) && F.a11y1_partialNumberRejected && F.a11y1_negativeLength === 'true' && F.a11y1_gridCell.unchanged && F.a11y1_gridCell.invalid === 'true' && F.a11y1_gridCell.message.length > 0,
  'a11y cycle 1: catalog search result is a status message (4.1.3)': F.a11y2_noMatchStatus.startsWith('No sections match') && /^\d+ sections? match$/.test(F.a11y2_countStatus),
  'a11y cycle 1: Escape never discards a section edit; floats scroll; undo stops at the draft; narrow model area; opening; multi height; TE label; F6': F.a11y3_escapeKeepsEdit.screen === 'section' && F.a11y3_escapeKeepsEdit.kept && F.a11y4_floatReachable_workspace && F.a11y4_floatReachable_section && F.a11y5_undoStopsAtDraft.depth && F.a11y6_narrowModelArea.overlap === 0 && F.a11y8_openingSemantics.busy === null && F.a11y10_teGapInside && F.a11y12_f6ReachesDialog && /^(-?[\d.]+) = \1$/.test(F.a11y9_multiHeight),
  'no JS errors': errors.length === 0 };
const evidence = { artifact: 'docs/mockups/workbench-v10.html', when: new Date().toISOString(), combos: rows.length, failing: failing.length, failSamples: failing.slice(0, 6).map(r => `${r.k}: ${r.v} | ${r.d.slice(0, 200)}`), focusChecks: Object.keys(F).filter(k => k.startsWith('focus')).length, focusFailures, gates, checks: F, errors: [...new Set(errors)].slice(0, 8), screenshots: shots };
await fs.writeFile(path.join(repo, 'docs/proof/workbench-v10-browser-check.json'), JSON.stringify(evidence, null, 1) + '\n');
console.log(JSON.stringify(evidence, null, 1));
await browser.close();
const failed = Object.entries(gates).filter(([, ok]) => !ok).map(([k]) => k);
if (failed.length){ console.error('FAILED: ' + failed.join(' · ')); process.exit(1); }
