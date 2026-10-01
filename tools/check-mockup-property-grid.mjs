// Browser oracle for docs/mockups/property-grid.html (the Properties property grid, F-1 of docs/reviews/m12b-native.md).
// Usage: node tools/check-mockup-property-grid.mjs <playwright module root>   (system Chrome via channel 'chrome')
// Sweeps every harness state x theme (light, dark, high contrast) x pane width (200, 260, 300 px) x window (800, 900)
// through the page's own audit (page box, units, clipping, one identity, one label column, targets, label-in-name,
// contrast), records the UI-36 fit at 1280 x 800, then drives the interaction paths a reviewer is promised.
// Writes docs/proof/property-grid-browser-check.json and exits 1 when any gate fails.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'), moduleRoot = process.argv[2];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const file = path.join(repo, 'docs/mockups/property-grid.html');
const browser = await chromium.launch({ channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });
const errors = [];
page.on('pageerror', e => errors.push(String(e)));
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
await page.goto(pathToFileURL(file).href);
const states = await page.evaluate(() => window.__pg.states);
const fails = {}, fit = {}; let cells = 0;
for (const height of ['800', '900']) for (const theme of ['light', 'dark', 'contrast']) for (const width of ['200', '260', '300']) for (const state of states) {
  const res = await page.evaluate(([h, t, w, s]) => { window.__pg.setHarness({ height: h, theme: t, width: w }); window.__pg.goState(s); return window.__pg.audit(); }, [height, theme, width, state]);
  cells++;
  for (const r of res) {
    if (r.check === 'selection + Wing fit without scrolling') { if (height === '800' && theme === 'light') fit[`${state}@${width}`] = r.pass ? 'fits' : r.detail; continue; }
    if (r.pass === false) (fails[r.check] ??= []).push(`${state}/${theme}/${width}/${height}: ${r.detail}`);
  }
}
// UI-36 is asserted at the default dock (260 px) for every state except the overflow fixture, whose purpose is to overflow.
const fitGate = Object.entries(fit).filter(([k, v]) => k.endsWith('@260') && !k.startsWith('overflow') && v !== 'fits');
const interactions = [];
const check = (name, ok) => interactions.push({ name, pass: !!ok });
const at = s => page.evaluate(st => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260' }); window.__pg.goState(st); }, s);
const focused = () => page.evaluate(() => document.activeElement?.dataset.fk);
await at('foil');
await page.fill('input[data-fk="w:tip"]', '15 cm'); await page.press('input[data-fk="w:tip"]', 'Enter');
check('a typed expression is echoed in mm (COPY-157)', await page.locator('input[data-fk="w:tip"]').inputValue() === '150.00' && /15 cm = 150\.00 mm\./.test(await page.locator('#wing').innerText()));
await page.fill('input[data-fk="w:root"]', 'abc'); await page.press('input[data-fk="w:root"]', 'Enter');
check('a non-number is refused with COPY-118, aria-invalid and an alert', await page.locator('input[data-fk="w:root"]').getAttribute('aria-invalid') === 'true' && /Enter a number\. Root chord is unchanged\./.test(await page.locator('[role="alert"]').first().innerText()));
await page.press('input[data-fk="w:root"]', 'Escape');
check('Escape restores the shown value', await page.locator('input[data-fk="w:root"]').inputValue() === '126.74');
await page.fill('input[data-fk="w:root"]', '190'); await page.press('input[data-fk="w:root"]', 'Tab');
check('leaving the field commits and focus lands on the next field', await focused() === 'w:tip' && /above the limit/.test(await page.locator('#wing').innerText()));
check('estimates are recomputed from their operands after a commit', !/≈ 99\.6/.test(await page.locator('#wing').innerText()));
await at('anchor');
await page.focus('[data-fk="tangent:smooth"]'); await page.keyboard.press('ArrowRight');
check('arrow on Tangent selects Symmetric and keeps focus', await focused() === 'tangent:symmetric' && await page.locator('[data-fk="tangent:symmetric"]').getAttribute('aria-checked') === 'true');
await page.focus('input[data-fk="p:aft"]'); await page.keyboard.press('Shift+ArrowUp');
check('Shift+Up in a length field steps 1 mm and keeps focus', await page.locator('input[data-fk="p:aft"]').inputValue() === '159.66' && await focused() === 'p:aft');
await at('handle');
check('a handle shows its parent anchor tangent, labelled (F-4)', await page.locator('[role="radiogroup"][aria-label="Tangent of anchor point 7"]').count() === 1);
check('a handle has its own identity (O-6)', await page.evaluate(() => document.querySelector('.ident h2').textContent) === 'Handle toward the tip');
await page.focus('[data-fk="grp:hdl"]'); await page.keyboard.press('Escape');
check('Escape on a handle selects its anchor', await page.evaluate(() => document.querySelector('.ident h2').textContent) === 'Trailing edge · point 7 of 14');
await page.click('[data-fk="grp:pos"]');
check('a group collapses, shows its summary and keeps focus', await page.locator('[data-fk="grp:pos"]').getAttribute('aria-expanded') === 'false' && await focused() === 'grp:pos' && /321\.65/.test(await page.locator('[data-fk="grp:pos"]').innerText()));
await at('control'); await page.click('[data-fk="grp:pos"]');
await page.selectOption('select[data-fk="type"]', 'anchor');
check('a type change reports handles, not "a point" (F-5, COPY-154)', /now an anchor point with 2 handles/.test(await page.locator('#status').innerText()));
await at('unavailable');
check('unavailable estimates say so, never "≈ —" (D-4)', /Unavailable — the estimates did not converge/.test(await page.locator('#wing').innerText()) && !/≈\s*—/.test(await page.locator('#wing').innerText()));
await browser.close();
const ok = !errors.length && !Object.keys(fails).length && !fitGate.length && interactions.every(i => i.pass);
const evidence = { file: 'docs/mockups/property-grid.html', date: new Date().toISOString(), cells, errors, fails, fitAt1280x800: fit, fitGate, interactions, ok };
await fs.writeFile(path.join(repo, 'docs/proof/property-grid-browser-check.json'), JSON.stringify(evidence, null, 1) + '\n');
console.log(`${cells} cells, ${Object.keys(fails).length} failing checks, ${fitGate.length} fit failures at 260 px, ${interactions.filter(i => !i.pass).length}/${interactions.length} interaction failures, ${errors.length} page errors`);
process.exit(ok ? 0 : 1);
