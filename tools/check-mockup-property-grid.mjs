// Browser oracle for docs/mockups/property-grid.html (the Properties property grid, F-1 of docs/reviews/m12b-native.md).
// Usage: node tools/check-mockup-property-grid.mjs <playwright module root> [mockup path]   (system Chrome via channel 'chrome')
// Sweeps every harness state x theme (light, dark, high contrast) x pane width (200, 260, 300 px) x window (800, 900)
// through the page's own audit (page box, units on screen and in the accessible text, clipping and ellipsis of values,
// labels and Kind options, one identity, one label column, Wing visible, targets, label-in-name, contrast), records the
// selection fit at 1280 x 800, then drives the interaction paths the review promises (repair cycle 1: PG-01..PG-18,
// MC-1..MC-17). Writes docs/proof/property-grid-browser-check.json (only for the committed mockup) and exits 1 on any failure.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'), moduleRoot = process.argv[2];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const committed = path.join(repo, 'docs/mockups/property-grid.html'), file = process.argv[3] ? path.resolve(process.argv[3]) : committed;
const browser = await chromium.launch({ channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });
const errors = [];
page.on('pageerror', e => errors.push(String(e)));
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
await page.goto(pathToFileURL(file).href);
const states = await page.evaluate(() => window.__pg.states);
const fails = {}, fit = {}; let cells = 0;
// Structure B (DR-CELL-1, Premiere Effect Controls): the sweep runs B at 100 % text, where the page audit also pins it — one
// 11 px size, units on the value baseline, rows 20/24, a 24 px value target, 24 px headers, the non-colour edit cue
// (dotted underline or ▾) that actually renders, no cue on read-only values.
for (const height of ['800', '900']) for (const theme of ['light', 'dark', 'contrast']) for (const width of ['200', '260', '300']) for (const state of states) {
  const res = await page.evaluate(([h, t, w, s]) => { window.__pg.setHarness({ height: h, theme: t, width: w, density: 'b', text: '100' }); window.__pg.goState(s); return window.__pg.audit(); }, [height, theme, width, state]);
  cells++;
  for (const r of res) {
    if (r.check === 'selection fits without scrolling') { if (height === '800' && theme === 'light') fit[`${state}@${width}`] = r.detail; continue; }
    // the Wing is gated at the default and wide docks; at 200 px a long warning may scroll inside the Wing (recorded)
    if (r.check === 'Wing block fully visible' && width === '200') { if (r.pass === false && height === '800' && theme === 'light') fit[`${state}@200 Wing`] = r.detail; continue; }
    if (r.pass === false) (fails[r.check] ??= []).push(`${state}/${theme}/${width}/${height}: ${r.detail}`);
  }
}
// SC 1.4.4 floor for the dense layout: at 200 % text every state at the default dock still renders with nothing clipped
// or ellipsized and every target ≥ 24 px; fit and Wing visibility are recorded, not gated, at 200 %.
const textScale = {};
for (const theme of ['light', 'dark', 'contrast']) for (const state of states) {
  const res = await page.evaluate(([t, s]) => { window.__pg.setHarness({ height: '800', theme: t, width: '260', density: 'b', text: '200' }); window.__pg.goState(s); return window.__pg.audit(); }, [theme, state]);
  cells++;
  for (const r of res) {
    const gated = /page rendered|clipped|ellipsized|targets|unfilled|accessible text carries/.test(r.check);
    if (gated && r.pass === false) (fails[`200 % text: ${r.check}`] ??= []).push(`${state}/${theme}/260/800: ${r.detail}`);
    if (!gated && r.pass === false && theme === 'light') (textScale[state] ??= []).push(`${r.check}: ${r.detail}`);
  }
}
// SC 1.4.12: with the WCAG text-spacing override at 11 px, nothing is clipped or ellipsized in any state (light, 260 px)
const spacing = {};
for (const state of states) {
  const res = await page.evaluate(s => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100', spacing: true }); window.__pg.goState(s); return window.__pg.audit(); }, state);
  cells++;
  for (const r of res) {
    // inputs are single-line and scroll inside themselves under the override (content stays reachable); labels and facts must wrap
    const gated = /page rendered|clipped|ellipsized|targets|unfilled/.test(r.check) && !(/clipped/.test(r.check) && /^[−\-0-9., ]+$/.test(r.detail));
    if (gated && r.pass === false) (fails[`1.4.12 spacing: ${r.check}`] ??= []).push(`${state}/light/260/800: ${r.detail}`);
    if (!gated && r.pass === false) (spacing[state] ??= []).push(`${r.check}: ${r.detail}`);
  }
}
await page.evaluate(() => window.__pg.setHarness({ spacing: false }));
// before/after density table (light, 260 px dock, 1280 x 800) for the review
const density = {};
for (const state of ['foil', 'control', 'anchor', 'handle', 'root-te', 'twist-anchor', 'worst-case']) density[state] = await page.evaluate(s => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100' }); window.__pg.goState(s); return window.__pg.densityBoth(); }, state);
await page.evaluate(() => window.__pg.setHarness({ density: 'b', text: '100' }));
const interactions = [];
const check = (name, ok) => interactions.push({ name, pass: !!ok });
const at = s => page.evaluate(st => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260' }); window.__pg.goState(st); }, s);
const focused = () => page.evaluate(() => document.activeElement?.dataset.fk);
const undo = () => page.evaluate(() => window.__pg.undo());
const status = () => page.locator('#status').innerText();
const expand = async id => { const b = page.locator(`[data-fk="grp:${id}"]`); if (await b.getAttribute('aria-expanded') === 'false') await b.click(); };

// --- expressions and units (MC-3, MC-4, UI-39)
await at('foil');
await page.fill('input[data-fk="w:tip"]', '#root_chord * 0.1'); await page.press('input[data-fk="w:tip"]', 'Enter');
check('an expression is set once and says so (MC-3, COPY-157)', await page.locator('input[data-fk="w:tip"]').inputValue() === '12.67'
  && /#root_chord × 0\.1 = 12\.67 mm \(set once; doesn’t follow Root chord\)/.test(await page.locator('#wing').innerText()));
await page.fill('input[data-fk="w:root"]', 'abc'); await page.press('input[data-fk="w:root"]', 'Enter');
check('a non-number is refused with COPY-118, aria-invalid and an alert', await page.locator('input[data-fk="w:root"]').getAttribute('aria-invalid') === 'true' && /Enter a number\. Root chord is unchanged\./.test(await page.locator('[role="alert"]').first().innerText()));
await page.press('input[data-fk="w:root"]', 'Escape');
check('Escape restores the shown value', await page.locator('input[data-fk="w:root"]').inputValue() === '126.74');
await page.fill('input[data-fk="w:root"]', '190'); await page.press('input[data-fk="w:root"]', 'Tab');
check('leaving the field commits and focus lands on the next field', await focused() === 'w:tip' && /above the limit/.test(await page.locator('#wing').innerText()));
check('estimates are recomputed from their operands after a commit', !/≈ 99\.6/.test(await page.locator('#wing').innerText()));
const u0 = await undo(); await page.focus('input[data-fk="w:root"]'); await page.keyboard.press('ArrowUp');
check('arrows do not nudge a Wing driving dimension (DR-UID-2 scope)', await undo() === u0 && await page.locator('input[data-fk="w:root"]').inputValue() === '190.00');

await at('anchor');
await page.fill('input[data-fk="h:ang"]', '-21°'); await page.press('input[data-fk="h:ang"]', 'Enter');
check('an angle field takes a degree unit (MC-4)', await page.locator('input[data-fk="h:ang"]').inputValue() === '−21.00');
await page.fill('input[data-fk="h:ang"]', '0.35 rad'); await page.press('input[data-fk="h:ang"]', 'Enter');
check('radians are converted and echoed in degrees (MC-4, COPY-157)', await page.locator('input[data-fk="h:ang"]').inputValue() === '20.05' && /0\.35 rad = 20\.05°\./.test(await page.locator('#sel').innerText()));
await page.fill('input[data-fk="h:ang"]', '95'); await page.press('input[data-fk="h:ang"]', 'Enter');
check('an angle outside (−90°, 90°) is refused with COPY-158 (MC-13)', /Enter an angle between −90° and 90°, from the span axis, \+ aft\. Angle is unchanged\./.test(await page.locator('#sel').innerText()));

// --- Tangent kind is an enum in B (as approved; spec UI-37). Type's rules apply (PG-07 / PG-19): arrows on the closed
//     box are pending, Return commits one undo row, a pointer pick commits, leaving with a pending kind drops it
await at('anchor');
const arrowPick = v => page.evaluate(v => { const s = document.querySelector('select[data-fk="kind"]'); s.focus(); s.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true })); s.value = v; s.dispatchEvent(new Event('change', { bubbles: true })); }, v);
let u = await undo(); await arrowPick('corner');
check('an arrow on the closed Tangent kind box is pending, not committed', await undo() === u && /Press Return to make it corner, or Esc to keep smooth\./.test(await page.locator('#sel').innerText()) && await focused() === 'kind');
await page.keyboard.press('Enter');
check('Return commits the kind as one undo row', await undo() === u + 1 && /Handle toward the root/.test(await page.locator('#sel').innerText()));
u = await undo(); await arrowPick('symmetric'); await page.keyboard.press('Tab');
check('leaving the Tangent kind box with a pending kind drops it', await undo() === u && await page.locator('select[data-fk="kind"]').inputValue() === 'corner');
await page.selectOption('select[data-fk="kind"]', 'smooth');
check('a pointer pick of the kind commits at once', await undo() === u + 1 && await page.locator('select[data-fk="kind"]').inputValue() === 'smooth');

// --- Type (PG-07: arrows on the closed box are pending; Return commits)
await at('control'); await expand('pos');
u = await undo();
await page.evaluate(() => { const s = document.querySelector('select[data-fk="type"]'); s.focus(); s.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true })); s.value = 'anchor'; s.dispatchEvent(new Event('change', { bubbles: true })); });
check('an arrow on the closed Type box does not commit', await undo() === u && /Press Return to change the type/.test(await page.locator('#sel').innerText()));
await page.keyboard.press('Enter');
check('Return commits the type; the report counts the rail (F-5, MC-9, COPY-154)', await undo() === u + 1 && /now an anchor point with 2 handles\. The rail gained 3 points \(10 → 13\)\. Largest change 0\.84 mm\./.test(await status()));
await at('control'); await expand('pos'); await page.selectOption('select[data-fk="type"]', 'anchor');
check('a pointer choice of Type commits at once (DropDownClosed)', /now an anchor point/.test(await status()));
await at('anchor'); await expand('pos'); await page.selectOption('select[data-fk="type"]', 'control');
check('Make control point reports the rail from the result, 14 → 12 (N-4, COPY-165)', /the rail has 12 points \(was 14\)\. Largest change 0\.62 mm\./.test(await status()));
// PG-19 / D1: leaving the Type box with a pending type drops it
await at('control'); await expand('pos'); u = await undo();
await page.evaluate(() => { const s = document.querySelector('select[data-fk="type"]'); s.focus(); s.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true })); s.value = 'anchor'; s.dispatchEvent(new Event('change', { bubbles: true })); });
await page.keyboard.press('Tab');
check('leaving the Type box with a pending type does not commit (PG-19)', await undo() === u && await page.locator('select[data-fk="type"]').inputValue() === 'control'
  && !/Press Return to change the type/.test(await page.locator('#sel').innerText()) && await page.evaluate(() => document.querySelector('.ident h2').textContent) === 'Trailing edge · point 3 of 10');

// --- field nudge (DR-UID-2 under MC-10, MC-18, PG-08)
await at('anchor');
u = await undo(); await page.focus('input[data-fk="p:aft"]');
await page.keyboard.down('ArrowUp'); await page.keyboard.down('ArrowUp'); await page.keyboard.down('ArrowUp');
const mid = await page.locator('input[data-fk="p:aft"]').inputValue(), chip = await page.locator('#wing .chip').innerText().catch(() => '');
await page.keyboard.press('Escape'); await page.keyboard.up('ArrowUp');
check('a held nudge previews, and Esc cancels with no undo row (MC-10)', mid === '158.96' && chip === '≈ preview' && await undo() === u && await page.locator('input[data-fk="p:aft"]').inputValue() === '158.66');
await page.keyboard.down('Shift'); await page.keyboard.down('ArrowUp'); await page.keyboard.up('ArrowUp'); await page.keyboard.up('Shift');
check('a released nudge is one undo row and announces the value (PG-08)', await undo() === u + 1 && await page.locator('input[data-fk="p:aft"]').inputValue() === '159.66' && /Aft 159\.66 mm \(Δ \+1\.00 mm\)\./.test(await status()) && await focused() === 'p:aft');
await page.fill('input[data-fk="p:aft"]', '160.5'); await page.keyboard.press('ArrowUp');
check('arrows are ignored while the field text is dirty (MC-10)', await page.locator('input[data-fk="p:aft"]').inputValue() === '160.5' && await undo() === u + 1);
const help = await page.evaluate(() => { const i = document.querySelector('input[data-fk="p:aft"]'); return (i.getAttribute('aria-describedby') || '').split(' ').map(id => document.getElementById(id)?.textContent || '').join(' '); });
check('the nudge steps are in the field description, both OS chords (PG-08, MC-18)', /Up and Down arrows step 0\.1 mm; with Command \(Ctrl on Windows\) 0\.01; with Shift 1\./.test(help));

// --- PG-22: the same error is announced again on the next failed commit
await at('anchor');
await page.fill('input[data-fk="p:aft"]', 'abc'); await page.press('input[data-fk="p:aft"]', 'Enter');
await page.fill('input[data-fk="p:aft"]', 'abc'); await page.press('input[data-fk="p:aft"]', 'Enter');
check('an identical error is announced again on the next failed commit (PG-22)', await page.locator('#sel [role="alert"]').count() === 1);
// --- MC-23: a field run stops at the rail angle bound and makes no row if nothing changed
await at('anchor');
await page.fill('input[data-fk="h:ang"]', '89.95'); await page.press('input[data-fk="h:ang"]', 'Enter');
u = await undo(); await page.focus('input[data-fk="h:ang"]');
await page.keyboard.down('Shift'); await page.keyboard.down('ArrowUp'); await page.keyboard.up('ArrowUp'); await page.keyboard.up('Shift');
check('an angle run stops at the domain bound (MC-23)', await page.locator('input[data-fk="h:ang"]').inputValue() === '89.95' && await undo() === u
  && /Stops here: the angle stays between −90° and 90° from the span axis\./.test(await page.locator('#sel').innerText()));
// --- MC-19: a typed twist past the domain is clamped by Core and echoed with a warning; MC-20: t/c under 1 % warns
await at('twist');
u = await undo(); await page.fill('input[data-fk="p:twist"]', '-70'); await page.press('input[data-fk="p:twist"]', 'Enter');
check('a typed twist past the domain is clamped, echoed and warned (MC-19)', await page.locator('input[data-fk="p:twist"]').inputValue() === '−57.30' && await undo() === u + 1
  && /-70 typed; set to −57\.30°, the largest that can be checked\./.test(await page.locator('#sel').innerText())
  && await page.evaluate(() => document.querySelector('input[data-fk="p:twist"]').closest('.row').dataset.state) === 'warning');
await at('tc-point');
await page.fill('input[data-fk="p:tc"]', '120'); await page.press('input[data-fk="p:tc"]', 'Enter');
check('a typed t/c past the domain is clamped by Core and warned, not refused (MC-19)', await page.locator('input[data-fk="p:tc"]').inputValue() === '99.99' && /120 typed; set to 99\.99 %, the largest that can be checked\./.test(await page.locator('#sel').innerText()));
await page.fill('input[data-fk="p:tc"]', '0.12'); await page.press('input[data-fk="p:tc"]', 'Enter');
check('a t/c under 1 % is committed with a fraction hint (MC-20)', await page.locator('input[data-fk="p:tc"]').inputValue() === '0.12' && /0\.12 % — for 12 %, type 12 or 0\.12 × 100\./.test(await page.locator('#sel').innerText()));
await at('twist-anchor');
check('a Smooth twist anchor says how the other handle moves (MC-22)', /Changing one handle's twist moves the other onto the line\./.test(await page.locator('#sel').innerText()));

// --- identity, selection, availability, authority, labels
await at('handle');
check('a handle shows its parent anchor kind, labelled (F-4)', await page.locator('select[data-fk="kind"][aria-label="Tangent kind of anchor point 7"]').count() === 1);
check('a handle has its own identity (O-6)', await page.evaluate(() => document.querySelector('.ident h2').textContent) === 'Handle toward the tip');
await page.focus('[data-fk="grp:hdl"]'); await page.keyboard.press('Escape');
check('Escape on a handle selects its anchor and says so (PG-12)', await page.evaluate(() => document.querySelector('.ident h2').textContent) === 'Trailing edge · point 7 of 14' && /Selected Trailing edge · anchor point 7 of 14\./.test(await status()));
await page.click('[data-fk="grp:pos"]');
check('a group collapses, shows its summary and keeps focus', await page.locator('[data-fk="grp:pos"]').getAttribute('aria-expanded') === 'false' && await focused() === 'grp:pos' && /321\.65/.test(await page.locator('[data-fk="grp:pos"]').innerText()));
await at('unavailable');
check('unavailable estimates say so and are announced (D-4, PG-04)', /Unavailable — the estimates did not converge/.test(await page.locator('#wing').innerText()) && !/≈\s*—/.test(await page.locator('#wing').innerText()) && /Estimates unavailable — did not converge\./.test(await status()));
await at('partial-unavailable');
check('one unavailable estimate leaves the others shown (BLANK-ESTIMATE)', await page.evaluate(() => { const rows = [...document.querySelectorAll('#wing .row')]; const t = l => rows.find(r => r.dataset.q === l)?.innerText || ''; return /Unavailable/.test(t('MAC')) && /≈ 99\.6/.test(t('Mean chord')); }));
await at('root-te'); await expand('pos');
check('the TE root Aft row names the root-chord authority (MC-2, COPY-159)', /This is the root chord\. Typing here moves only this point; Root chord under Wing rescales the planform\./.test(await page.locator('#sel').innerText()));
await at('control');
check('a point uses From root and η; Span means only b (MC-6)', await page.evaluate(() => { const l = [...document.querySelectorAll('#sel .lbl')].map(x => x.textContent); return l.includes('From root') && l.includes('η') && !l.includes('Span'); }));
check('a fact or estimate speaks its unit (PG-01)', await page.evaluate(() => window.__pg.accText([...document.querySelectorAll('#wing .row')].find(r => r.dataset.q === 'Mean chord')).includes('millimetres')));
check('abbreviations are spoken in full (PG-24)', await page.evaluate(() => { const r = l => window.__pg.accText([...document.querySelectorAll('#wing .row')].find(x => x.dataset.q === l)); return r('AR').includes('aspect ratio') && r('Max t/c').includes('t over c'); }));
check('AR carries its convention in the unit column (MC-8)', await page.evaluate(() => [...document.querySelectorAll('#wing .row')].find(r => r.dataset.q === 'AR')?.querySelector('.unit').textContent === 'b²/S'));
// --- DC-1: a focused or dirty field shows the whole expression, at every dock
for (const w of ['200', '260', '300']) for (const expr of ['#root_chord × 0.35', '(#span − 2 cm) / 2']) {
  await page.evaluate(w => { window.__pg.setHarness({ height: '800', theme: 'light', width: w, density: 'b', text: '100' }); window.__pg.goState('anchor'); }, w);
  await expand('pos'); await page.fill('input[data-fk="p:aft"]', expr);
  check(`a typed expression is visible whole in Aft at ${w} px: ${expr} (DC-1)`, await page.evaluate(() => { const i = document.querySelector('input[data-fk="p:aft"]'); return i.scrollWidth <= i.clientWidth; }));
  await page.press('input[data-fk="p:aft"]', 'Escape');
}
// --- DC-2: worst-case numbers at the 200 px dock are not clipped
await page.evaluate(() => { window.__pg.setHarness({ height: '800', theme: 'light', width: '200', density: 'b', text: '100' }); window.__pg.goState('worst-case'); });
check('worst-case numbers are not clipped at the 200 px dock (DC-2)', await page.evaluate(() => window.__pg.audit().filter(r => /clipped|ellipsized/.test(r.check)).every(r => r.pass !== false)));
// --- DN-1: a focused field in error keeps its text unclipped and a 1 px boundary
await page.evaluate(() => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100' }); window.__pg.goState('field-error'); });
await page.focus('input[data-fk="p:aft"]');
check('a focused field in error: text not clipped, 1 px boundary (DN-1)', await page.evaluate(() => { const i = document.querySelector('input[data-fk="p:aft"]'); return i.scrollWidth <= i.clientWidth && /inset 0px 0px 0px 1px|1px inset/.test(getComputedStyle(i).boxShadow) && window.__pg.audit().every(r => r.check !== 'drawn field boundary is 1 px in every state' || r.pass); }));
// --- DN-5 / DN-6: the app's Text size setting — ⌘+ steps 100 → 125 → 150, stacks rows at 150, persists across a reload,
//     and a focused Wing field with its message stays in view at 200 %
await page.evaluate(() => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100' }); window.__pg.setTextSize('100'); window.__pg.goState('foil'); });
await page.focus('[data-fk="grp:foil"]'); await page.keyboard.press('Meta+Equal'); await page.keyboard.press('Meta+Equal');
check('⌘+ steps the Text size 100 → 125 → 150 and stacks the rows at 150 % (DN-5)', await page.evaluate(() => document.querySelector('#app-text-size').value === '150' && document.querySelector('#frame').dataset.stacked === '1'));
await page.reload();
check('the Text size persists per user across a reload (DN-5)', await page.evaluate(() => document.querySelector('#app-text-size').value === '150' && document.querySelector('#frame').dataset.stacked === '1'));
await page.evaluate(() => { window.__pg.setTextSize('200'); window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b' }); window.__pg.goState('foil'); });
await page.fill('input[data-fk="w:tip"]', 'abc'); await page.press('input[data-fk="w:tip"]', 'Enter');
check('at 200 % a focused Wing field and its message are in view (DN-6)', await page.evaluate(() => { const r = document.querySelector('input[data-fk="w:tip"]').closest('.row').getBoundingClientRect(), w = document.querySelector('#wing').getBoundingClientRect(); return document.activeElement?.dataset.fk === 'w:tip' && r.top >= w.top - 1 && r.bottom <= w.bottom + 1; }));
await page.focus('[data-fk="w:tip"]'); await page.keyboard.press('Escape');
await page.evaluate(() => window.__pg.setTextSize('100'));
await page.focus('[data-fk="grp:foil"]').catch(() => {}); await page.keyboard.press('Meta+Minus');
check('⌘− steps the Text size down and never below 100 % (DN-5)', await page.evaluate(() => document.querySelector('#app-text-size').value === '100' && document.querySelector('#frame').dataset.stacked === '0'));
// --- B accessibility 3(b): every editable value is a Tab stop and looks focused when it is one (SC 2.1.1, 2.4.7);
//     the whole row (label + value) is the pointer target (SC 2.5.8)
for (const state of ['anchor', 'handle', 'foil', 'twist-anchor']) {
  await page.evaluate(s => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100' }); window.__pg.goState(s); document.querySelectorAll('#pane [aria-expanded="false"]').forEach(b => b.click()); }, state);
  const want = await page.evaluate(() => [...document.querySelectorAll('#pane input[data-fk], #pane select[data-fk]')].filter(n => n.offsetParent).map(n => n.dataset.fk));
  await page.evaluate(() => document.querySelector('#pane button, #pane input, #pane select').focus());
  const seen = new Set(), unfocusedLook = [];
  for (let i = 0; i < 60; i++) {
    const info = await page.evaluate(() => { const a = document.activeElement; if (!a || !document.querySelector('#pane').contains(a)) return null; const cs = getComputedStyle(a);
      const visible = (cs.boxShadow && cs.boxShadow !== 'none') || (cs.outlineStyle !== 'none' && parseFloat(cs.outlineWidth) > 0); return { fk: a.dataset.fk || a.tagName, visible }; });
    if (!info) break; seen.add(info.fk); if (!info.visible) unfocusedLook.push(info.fk);
    await page.keyboard.press('Tab');
  }
  const missing = want.filter(fk => !seen.has(fk));
  check(`every editable value is reachable by Tab (${state}) (B, SC 2.1.1)`, missing.length === 0);
  check(`every Tab stop looks focused (${state}) (B, SC 2.4.7)`, unfocusedLook.length === 0);
}
await page.evaluate(() => { window.__pg.setHarness({ height: '800', theme: 'light', width: '260', density: 'b', text: '100' }); window.__pg.goState('anchor'); });
await page.click('#pane .row label[for]:text-is("Aft")');
check('clicking a label focuses its value (the row band is the target, SC 2.5.8)', await focused() === 'p:aft');
await browser.close();
const ok = !errors.length && !Object.keys(fails).length && interactions.every(i => i.pass);
const evidence = { file: path.relative(repo, file), date: new Date().toISOString(), cells, errors, fails, selectionFitAt1280x800: fit, textAt200Recorded: textScale, spacing1412Recorded: spacing, densityBeforeAfter: density, interactions, ok };
if (file === committed) await fs.writeFile(path.join(repo, 'docs/proof/property-grid-browser-check.json'), JSON.stringify(evidence, null, 1) + '\n');
console.log(`${cells} cells, ${Object.keys(fails).length} failing checks${Object.keys(fails).length ? ' (' + Object.keys(fails).join('; ') + ')' : ''}, ${interactions.filter(i => !i.pass).length}/${interactions.length} interaction failures${interactions.some(i => !i.pass) ? ': ' + interactions.filter(i => !i.pass).map(i => i.name).join(' | ') : ''}, ${errors.length} page errors${errors.length ? ': ' + errors.slice(0, 3).join(' | ') : ''}`);
process.exit(ok ? 0 : 1);
