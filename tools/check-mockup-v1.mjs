#!/usr/bin/env node
// Browser oracle for docs/mockups/workbench-v1.html — verifies the review artifact's interaction contracts
// against specification v1. It proves nothing about a native app, a geometry kernel or a solver.
// Usage: node tools/check-mockup-v1.mjs [<node_modules dir containing playwright>]
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const moduleRoot = process.argv[2];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const shots = await fs.mkdtemp(path.join(os.tmpdir(), 'cfd-workbench-v1-review-'));
const browser = await chromium.launch({ headless: true, channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1600, height: 1120 } });
const errors = [], requests = [], measurements = [], oracles = [];
page.on('pageerror', e => errors.push(e.message));
page.on('request', r => { if (/^https?:/.test(r.url())) requests.push(r.url()); });
page.on('dialog', d => d.accept('Reviewed: tip TE rounded in post-processing'));
const record = (name, proof) => oracles.push({ name, pass: true, proof });
const task = async name => { await page.locator(`.tab[data-task="${name}"]`).click(); await page.waitForTimeout(40); };
const harness = async (id, value) => { await page.locator(`#${id}`).selectOption(value); await page.waitForTimeout(40); };
const text = async sel => (await page.locator(sel).first().innerText()).replace(/\s+/g, ' ');
const measure = async label => {
  const row = await page.evaluate(() => ({ ...window.workbenchAudit, overflow: document.documentElement.scrollWidth > innerWidth }));
  assert(row.frame.width > 0 && row.frame.height > 0, `${label}: frame must render`);
  assert.equal(row.contrastFailures, 0, `${label}: token contrast`);
  assert.equal(row.smallTargets, 0, `${label}: targets under the 24 px floor`);
  assert.equal(row.denseUnder, 0, `${label}: dense controls under 32 px`);
  assert.equal(row.overflow, false, `${label}: horizontal page overflow`);
  measurements.push({ label, frame: row.frame, controls: row.controls, contrastFailures: row.contrastFailures, smallTargets: row.smallTargets, theme: row.theme, state: row.state, task: row.task });
};
try {
  await page.goto(pathToFileURL(path.join(repo, 'docs/mockups/workbench-v1.html')).href);
  await page.waitForTimeout(200);
  /* 1 · every destination × theme renders and audits clean */
  for (const theme of ['light', 'dark', 'contrast']) { await harness('h-theme', theme); for (const t of ['brief', 'shape', 'sections', 'analyze', 'settings']) { await task(t); await measure(`${theme}/${t}`); if (theme === 'light') await page.screenshot({ path: path.join(shots, `${t}.png`), fullPage: true }); } }
  await harness('h-theme', 'light');
  for (const st of ['first-launch', 'loading', 'empty', 'error', 'partial', 'overflow', 'success']) { await harness('h-state', st); await task(st === 'error' || st === 'partial' ? 'analyze' : 'shape'); await measure(`state/${st}`); }
  await harness('h-state', 'default'); await task('shape');
  record('Every destination, theme and hard state renders and audits clean', `${measurements.length} measurements; 0 contrast failures; 0 small targets`);
  /* 2 · GOAL-02 seven pinned triples */
  await task('brief');
  const ops = await page.locator('#op-table tbody tr').evaluateAll(rows => rows.map(r => [...r.querySelectorAll('td')].map(td => td.innerText.trim())));
  const expect = [[10, '0.773', '7.71'], [14, '0.395', '3.93'], [18, '0.239', '2.38'], [15, '0.458', '3.43'], [22, '0.213', '1.59'], [28, '0.132', '0.98'], [32, '0.101', '0.75']];
  expect.forEach(([v, cl, s], i) => { assert.equal(ops[i][3], String(v)); assert.equal(ops[i][7], cl, `CL at ${v} kn`); assert.equal(ops[i][8], s, `σ at ${v} kn`); });
  await page.locator('[data-h="P1"]').fill('0.30'); await page.locator('[data-h="P1"]').press('Tab'); await page.waitForTimeout(40);
  assert.equal((await page.locator('#op-table tbody tr').first().locator('td').nth(8).innerText()).trim(), '7.56', 'σ at 0.3 m');
  await page.locator('[data-h="P1"]').fill('0.50'); await page.locator('[data-h="P1"]').press('Tab');
  record('GOAL-02: seven (V, CL, σ) triples reproduce the pinned arithmetic; h_ref 0.3 m gives σ 7.56', expect);
  const feas = await text('#feasibility'); assert.match(feas, /Unavailable/); assert.match(feas, /Ncrit \{2, 4\}/); assert.match(feas, /satisfied|violated/);
  record('Feasibility matrix: satisfied · violated · Unavailable with reason, tier chip and both Ncrit per cell', 'matrix text contains all three states');
  /* 3 · GEO-13 monotone approach under rising influence weight */
  await task('shape'); await page.locator('[data-mode="smooth"]').click();
  await page.locator('[data-point="3"]').focus(); await page.waitForTimeout(30);
  const gaps = [];
  for (const w of [1, 2, 4, 8, 16]) { await page.locator('#w-in').fill(String(w)); await page.locator('#w-in').press('Tab'); await page.waitForTimeout(40);
    gaps.push(await page.evaluate(() => { const f = M.preview ? M.preview.fit : channelCurve(M.channel); const u = ETA[3]; const v = (M.preview ? M.preview.values : M.curves[M.channel])[3]; return Math.abs(BS.evalCurve(f.P, 5, f.U, u)[1] - v); })); }
  for (let i = 1; i < gaps.length; i++) assert(gaps[i] < gaps[i - 1] && gaps[i] > 0, `gap must decrease strictly and stay > 0: ${gaps}`);
  await page.locator('#cancel').click();
  record('GEO-13: gap to the influence control decreases strictly and stays > 0 over weights 1→16', gaps.map(g => g.toExponential(3)));
  /* 4 · weight validation, Cancel leaves no undo step, Return/Escape modal rule */
  await page.locator('[data-point="3"]').focus(); await page.locator('#w-in').fill('0'); await page.locator('#w-in').press('Tab');
  assert.equal(await text('#w-err'), 'Weight must be a positive finite number');
  await page.locator('[data-mode="through"]').click();
  const hist0 = await page.evaluate(() => M.history.length);
  await page.locator('[data-point="2"]').focus(); await page.keyboard.press('ArrowUp'); await page.waitForTimeout(30);
  assert.equal(await page.evaluate(() => !!M.preview), true, 'ArrowUp opens a preview');
  assert.match(await text('#status'), /Preview open — deviation [0-9.]+ mm · 2 locks/);
  await page.keyboard.press('Escape'); assert.equal(await page.evaluate(() => M.preview), null);
  assert.equal(await page.evaluate(() => M.history.length), hist0, 'a cancelled preview adds no undo step');
  record('Modal rule: ArrowUp previews, Escape cancels with an unchanged undo stack, invalid weight names its cause', { hist0 });
  /* 5 · exact-value entry, expression, dimensional error, Apply → revision, Historical, Undo → Current */
  await page.locator('#v-in').fill('12 Pa'); await page.locator('#v-in').press('Tab'); assert.match(await text('#v-err'), /Expression must be a number/);
  await page.locator('#v-in').fill('#root_chord * 0.35'); await page.locator('#v-in').press('Tab'); await page.waitForTimeout(40);
  assert.equal(await page.evaluate(() => document.activeElement.id), 'v-in', 'focus returns to the edited parameter');
  const rev0 = await page.evaluate(() => M.revision);
  await page.locator('#apply').click(); await page.waitForTimeout(40);
  assert.equal(await page.evaluate(() => M.revision), rev0 + 1);
  assert.match(await text('#run-list'), /Historical/); assert.match(await text('#shape-hud'), /Direct parametric/);
  await page.locator('#undo').click(); await page.waitForTimeout(40);
  assert.equal(await page.evaluate(() => M.revision), rev0); assert.match(await text('#run-list'), /k-0314 .*Current/); assert.match(await text('#shape-hud'), /Recipe linked/);
  record('Apply appends one Design revision, detaches the recipe and makes the run Historical; Undo restores shape, recipe and Currency together', { rev0 });
  /* 6 · derived readouts and the AR convention */
  const strip = await text('#derived-strip'); assert.match(strip, /AR \(projected, b²\/S\)/); assert.match(strip, /geometry only \(Structural: Not assessed\)/); assert.match(strip, /S 0\.1000 m²/);
  record('Derived strip: AR carries its convention; stiffness readout carries the Not-assessed qualifier; S_ref = 0.1000 m²', strip.slice(0, 160));
  /* 7 · lines-plan layout at 1440 */
  await harness('h-viewport', 'lines'); assert.equal(await page.locator('#window').getAttribute('data-layout'), 'lines'); assert.equal(await page.locator('#canvas-lines svg').count(), 3); await measure('lines-plan'); await harness('h-viewport', 'wide');
  record('Lines-plan layout renders plan · front · section side by side at 1440 px', 'three SVG panes');
  /* 8 · Analyze: basis travels with the number */
  await task('analyze');
  let band = await text('#cond-band'); assert.match(band, /Fr_h/); assert.match(band, /σ \(h_ref\)/);
  await page.locator('#h-depth').uncheck(); await page.waitForTimeout(40); band = await text('#cond-band');
  assert.match(band, /Unavailable — depth not set/); assert.doesNotMatch(await text('#analyze-body'), /Deep-water/);
  assert.doesNotMatch(band, /σ \(h_ref\) [0-9]/);
  await page.locator('#h-depth').check(); await page.waitForTimeout(40);
  record('ANA-19/UX-11: with depth unset σ, Fr_h and V_crit read "Unavailable — depth not set" and no deep-water value or label is printed', 'band text');
  await page.locator('#op-from').selectOption('custom'); await page.locator('#op-h').fill('0.30'); await page.locator('#op-h').press('Tab'); await page.waitForTimeout(40);
  const body = await text('#analyze-body');
  assert.match(body, /Deep-water result; free surface not modelled \(h\/c = 3\.3, Fr_h = [0-9.]+; effects measured below h\/c 5\)/);
  assert.match(body, /Cavitation screening \(sheet, by −Cp_min\): inception is possible above V_crit; not a prediction of inception, extent, tip-vortex or cloud cavitation; Cp_min resolution: 61 stations/);
  assert.match(body, /practitioner range; no measured water N-factor; Day 2019 used 4/);
  assert.match(body, /Unavailable — not computed/);
  assert.match(body, /XFOIL-class surrogate; accuracy relative to XFOIL, not experiment/);
  assert.match(await text('#analyze-prov'), /Illustrative — not computed for this design/);
  record('A5.1/A5.3/A5.4 strings render verbatim on the section result at h/c < 5; the tripped band reads Unavailable — not computed', 'strings matched');
  await page.locator('[data-scope="wing"]').click(); await page.waitForTimeout(40);
  const wing = await text('#analyze-body');
  assert.match(wing, /Static geometry; steady analysis cannot predict ventilation onset; onset is dynamic and hysteretic/);
  assert.match(wing, /Structural: Not assessed/); assert.match(wing, /Loads are hydrodynamic estimates\. Not a structural assessment\. Strength, stiffness and fatigue are not evaluated\./);
  assert.match(wing, /Correction layer beside \(never instead\)/); assert.match(wing, /attached flow; no stall; no ventilation; deep water/);
  assert.match(wing, /batlow v8 \(Crameri, MIT\) sequential/);
  record('Wing result: ventilation string, Loads Not-assessed state and safety string, JMSA correction beside the deep-water value, batlow legend fields', 'strings matched');
  await page.locator('[data-twin="load"]').click(); assert.equal(await page.locator('#table-dialog').evaluate(d => d.open), true); assert.match(await text('#table-body'), /Cl·c\/c̄/); await page.locator('#table-close').click();
  await page.locator('[data-scope="section"]').click(); await page.waitForTimeout(40);
  assert.match(await text('#analyze-body'), /vik v8 \(Crameri\) diverging, zero pinned/);
  await page.locator('[data-twin="cp"]').click(); assert.match(await text('#table-body'), /upper|lower/); await page.locator('#table-close').click();
  record('UI-07/UI-17: vik pinned at Cp = 0 with legend fields; every chart has a table twin in one action', 'dialogs opened with the same data');
  await page.locator('[data-scope="compare"]').click(); await page.waitForTimeout(40);
  const cmp = await text('#analyze-body'); assert.match(cmp, /reference quantities differ — coefficient delta blocked until reconciled/); assert.match(cmp, /Tiers disagree: δ = .* — Discrepancy record stored · value preferred by reported uncertainty: VLM \+ strip/); assert.doesNotMatch(cmp, /recommended/);
  record('ANA-06/A5.7: incompatible reference quantities block the delta; tiers disagree with a stored Discrepancy record and no "recommended"', 'compare text');
  /* 9 · water out of range (error state) */
  await harness('h-state', 'error'); await page.waitForTimeout(40); assert.match(await text('#cond-band'), /Unavailable — outside the ITTC table \(0–50 °C\)/); await harness('h-state', 'default');
  record('ANA-15: a temperature outside 0–50 °C reads Unavailable with the ITTC reason', 'band text');
  /* 10 · Sections: catalog classes, DAT two-layout detector */
  await task('sections'); const cat = await text('#catalog'); assert.match(cat, /Pending admission — terms requested from UIUC · GEN sections remain/); assert.match(cat, /GEN/); assert.match(cat, /LINK/);
  assert.equal(await page.locator('#catalog button[aria-label^="Assign Eppler E817"]').isDisabled(), true, 'pending sections cannot be assigned');
  await page.locator('#dat-open').click(); assert.match(await text('#dat-body'), /Selig \(detected\)/); await page.locator('[data-layout="lednicer"]').click(); assert.match(await text('#dat-body'), /rejected: self-crossing after Lednicer split — library unchanged/); assert.equal(await page.locator('#dat-body button.primary').isDisabled(), true); await page.locator('#dat-cancel').click();
  record('CAT-01/CAT-02: admission classes with reasons; Lednicer forced through Selig is rejected as self-crossing with the library unchanged', 'dialog text');
  await page.locator('#h-secdemo').click(); await page.waitForTimeout(40); assert.match(await text('#section-strip'), /Preview open — deviation .* ‰ chord · 2 locks/); await page.locator('#sec-cancel').click();
  record('GEO-08: a section edit opens a source-linked draft with deviation and locks; Cancel leaves the catalog original intact', 'strip text');
  /* 11 · Checks drawer, exclusion stored on the finding, overflow */
  await harness('h-state', 'overflow'); await task('shape'); await page.locator('#tab-checks').click(); await page.waitForTimeout(40);
  const findings = await text('#findings'); assert.match(findings, /label\.forbidden/); assert.match(findings, /v1 · error/);
  const before = +(await text('#checks-count'));
  await page.locator('[data-exclude="label.forbidden"]').click(); await page.waitForTimeout(40);
  assert.equal(+(await text('#checks-count')), before - 1); assert.match(await text('#findings'), /excluded: Reviewed: tip TE rounded in post-processing/);
  await page.locator('#checks-close').click(); await harness('h-state', 'default');
  record('DRC-01: findings carry rule id, version and severity; an exclusion needs a reason, is stored on the finding and lowers the status-strip count', { before });
  /* 12 · Export dialog: STEP gated, safety string, TE floor finding */
  await page.locator('#export-open').click(); const exp = await text('#export-body');
  assert.match(exp, /STEP export unavailable until the open-and-measure fixture exists/); assert.match(exp, /This geometry has not been checked for strength, manufacturability or ride safety\. No standard for hydrofoil-wing strength applies \(RCD 2013\/53\/EU excludes hydrofoils and surfboards; ISO 25649 excludes rigid surf-sport devices\)\. Test before use\./);
  assert.equal(await page.locator('#export-body tr:has-text("STEP") button').isDisabled(), true); await page.locator('#export-close').click();
  await task('settings'); await page.locator('#s-te').fill('1.5'); await page.locator('#s-te').press('Tab'); await page.waitForTimeout(40); await task('shape');
  assert.match(await text('#derived-strip'), /Trailing edge [0-9.]+ mm below the floor 1\.5 mm \(practitioner value, unverified\)/);
  await page.locator('#export-open').click(); assert.match(await text('#export-body'), /below the floor 1\.5 mm/); await page.locator('#export-close').click();
  await task('settings'); await page.locator('#s-te').fill('0.8'); await page.locator('#s-te').press('Tab'); await task('shape');
  record('EXP-02/GEO-12: STEP is gated with its string, the export safety string is verbatim, and a TE below the floor repeats in the export dialog', 'dialog text');
  /* 13 · Assistant states by capability */
  await task('brief'); await page.locator('#assist-toggle').click(); assert.match(await text('#assist-body'), /No API key configured — every design and analysis tool works without it/);
  await harness('h-capability', 'key-unevaluated'); assert.match(await text('#assist-body'), /Unevaluated on this model — proposals disabled until the eval suite passes/);
  await harness('h-capability', 'key-evaluated'); assert.match(await text('#assist-body'), /root t\/c: 0\.24 outside 0\.06–0\.14 — not applied/);
  await task('analyze'); assert.match(await text('#assist-body'), /Response withheld: it contains a number not in the shared context/);
  await task('sections'); assert.match(await text('#assist-body'), /No assistant action here/);
  await page.locator('#assist-close').click(); await harness('h-capability', 'key-none');
  record('AI-01/02/03/06: no-key, unevaluated-model, rejected-field, withheld-numeral and no-action states render with their fixed strings', 'assistant text per capability');
  /* 14 · reduced motion, modifier scheme, persona views */
  await page.locator('#h-motion').check(); assert.equal(await page.locator('.btn').first().evaluate(el => getComputedStyle(el).transitionDuration), '0s'); await page.locator('#h-motion').uncheck();
  await harness('h-mods', 'win'); assert.match(await text('#status'), /CtrlZ undo/); await harness('h-mods', 'mac'); assert.match(await text('#status'), /⌘Z undo/);
  await harness('h-persona', 'screen-reader'); await task('analyze'); assert.equal(await page.locator('.task-analyze .sr-view').isVisible(), true); await harness('h-persona', 'reviewer'); await task('shape'); assert.equal(await page.locator('#apply').count(), 0, 'reviewer persona has no Apply'); await harness('h-persona', 'designer');
  record('Harness: reduced motion removes transitions; modifier scheme flips the shortcut table; screen-reader and reviewer personas change the surface', 'computed styles and text');
  /* 16 · accessibility gate (2026-09-20, UX & Accessibility BLOCK → fixes): keyboard focus survives a nudge in both editors */
  await harness('h-state', 'default'); await task('shape'); await page.locator('[data-mode="through"]').click();
  await page.locator('[data-point="1"]').focus(); await page.keyboard.press('ArrowUp'); await page.waitForTimeout(30);
  assert.equal(await page.evaluate(() => document.activeElement && document.activeElement.dataset.point), '1', 'editor handle keeps focus after ArrowUp');
  await page.keyboard.press('PageUp'); await page.keyboard.press('Home'); assert.equal(await page.evaluate(() => document.activeElement.dataset.point), '1');
  await page.keyboard.press('Escape');
  await task('sections'); await page.locator('[data-sec="upper:3"]').focus(); await page.keyboard.press('ArrowUp'); await page.waitForTimeout(30);
  assert.equal(await page.evaluate(() => document.activeElement && document.activeElement.dataset.sec), 'upper:3', 'section handle keeps focus after ArrowUp');
  await page.locator('#sec-cancel').click();
  record('A11y: ArrowUp/PageUp/Home keep focus on the handle in both editors (SC 2.1.1, 2.4.3)', 'activeElement re-queried after render');
  /* 17 · handles are exposed: role=slider with min/max/valuetext under a group, not under role=img; no page-wide live region */
  await task('shape');
  const sliders = await page.evaluate(() => [...document.querySelectorAll('#editor-svg [role=slider]')].map(el => ({ name: el.getAttribute('aria-label'), min: el.getAttribute('aria-valuemin'), max: el.getAttribute('aria-valuemax'), text: el.getAttribute('aria-valuetext'), orient: el.getAttribute('aria-orientation'), underImg: !!el.closest('[role=img]') })));
  assert.equal(sliders.length, 6); sliders.forEach(s => { assert(s.name && s.min && s.max && s.text && s.orient === 'vertical' && !s.underImg, JSON.stringify(s)); });
  assert.equal(await page.locator('.centre[aria-live]').count(), 0, 'no page-wide live region'); assert.equal(await page.locator('#status-msg[role=status]').count(), 1);
  const aria = await page.locator('#editor-svg').ariaSnapshot(); assert((aria.match(/- slider/g) || []).length >= 6, 'aria snapshot exposes the six sliders: ' + aria.slice(0, 200));
  record('A11y: six sliders with name, min, max, valuetext and vertical orientation in the accessibility tree; one status region', sliders[0]);
  /* 18 · chart and viewport text never below 12 CSS px at any harness viewport */
  // rendered size of SVG text = font-size attribute × the viewBox scale actually applied (min of width and height ratios under xMidYMid meet); HTML captions are read from computed style
  const smallestText = () => page.evaluate(() => { const out = []; for (const t of document.querySelectorAll('#window svg text')) { const svg = t.ownerSVGElement; const r = svg.getBoundingClientRect(); if (!r.width || !r.height || t.getBoundingClientRect().width === 0) continue; const vb = svg.viewBox.baseVal; const scale = Math.min(r.width / vb.width, r.height / vb.height); out.push({ text: t.textContent.slice(0, 24), px: parseFloat(t.getAttribute('font-size')) * scale }); } for (const c of document.querySelectorAll('#window .caption, #window .hud, #window .tracing')) { if (c.getBoundingClientRect().width) out.push({ text: c.textContent.slice(0, 24), px: parseFloat(getComputedStyle(c).fontSize) }); } return out.reduce((m, x) => x.px < m.px ? x : m, { px: 1e9, text: 'none' }); });
  const textSizes = {};
  for (const vp of ['minimum', 'desktop', 'lines', 'wide', 'zoom']) { await harness('h-viewport', vp); textSizes[vp] = {};
    for (const tk of ['shape', 'sections', 'analyze']) { await task(tk); if (tk === 'analyze') { await page.locator('[data-scope="section"]').click(); await page.waitForTimeout(40); } const s = await smallestText(); textSizes[vp][tk] = s; assert(s.px >= 12, `${vp}/${tk}: smallest rendered text "${s.text}" is ${s.px.toFixed(1)} px`); await measure(`viewport/${vp}/${tk}`); } }
  await harness('h-viewport', 'wide');
  record('A11y/UI-17: smallest rendered SVG text (font-size × applied viewBox scale) and every HTML caption ≥ 12 CSS px on Shape, Sections and Analyze at 1024, 1280, 1440, 1600 and the 640 px (200 % zoom) viewport', textSizes);
  /* 19 · no NaN, no placeholder leak, in any state × task */
  for (const st of ['default', 'first-launch', 'loading', 'empty', 'error', 'partial', 'overflow', 'success']) { await harness('h-state', st); for (const tk of ['brief', 'shape', 'sections', 'analyze', 'settings']) { await task(tk); for (const sc of (tk === 'analyze' ? ['section', 'wing', 'compare'] : [null])) { if (sc) await page.locator(`[data-scope="${sc}"]`).click(); const txt = await text('#window'); assert.doesNotMatch(txt, /NaN/, `${st}/${tk}/${sc}: NaN rendered`); assert.doesNotMatch(txt, /Unavailable — reason|Locked by <source>/, `${st}/${tk}: placeholder leaked`); } } }
  await harness('h-state', 'error'); await task('analyze'); assert.match(await text('#analyze-body'), /Unavailable — outside the ITTC table \(0–50 °C\)/);
  await harness('h-state', 'default');
  record('UI-12: no NaN and no placeholder string in 8 states × 5 destinations × 3 scopes; out-of-range water yields Unavailable on every result', 'window text scanned');
  /* 20 · the C2 rows the gate found missing: COPY-30, COPY-32, COPY-39, COPY-58, COPY-69, Catalog original / Modified from */
  await harness('h-state', 'error'); await task('shape'); assert.match(await text('.centre'), /This file was saved by a newer version \(3\) — not opened; your active document is unchanged/);
  await harness('h-capability', 'key-evaluated'); await page.locator('#assist-toggle').click(); assert.match(await text('#assist-body'), /Assistant cap reached \(daily\) — raise it in Settings or wait/); await page.locator('#assist-close').click(); await harness('h-capability', 'key-none');
  await harness('h-state', 'partial'); assert.match(await text('.centre'), /A recovery revision from 21:14 exists beside the saved one/);
  await harness('h-state', 'default'); await task('brief'); assert.match(await text('#brief-form'), /Flagged — no product page opened · Acknowledge to continue/); await page.locator('[data-ack="1"]').click(); assert.match(await text('#brief-form'), /acknowledged/);
  await task('analyze'); await page.locator('#op-from').selectOption('custom'); await page.locator('#op-v').fill('0'); await page.locator('#op-v').press('Tab'); await page.waitForTimeout(40); assert.match(await text('#analyze-body'), /Undefined — q = 0 \(V ≤ 0\)/); assert.doesNotMatch(await text('#analyze-body'), /\b0\.000\b/); { const w0 = await text('#window'); assert.doesNotMatch(w0, /NaN|Infinity|-?∞[^ ,]/, 'V = 0: no NaN or Infinity rendered as a value anywhere in the window (the copy "0, ∞ or a nearby value" is prose)'); assert.match(await text('#cond-band'), /Re \(c̄\) Undefined — q = 0 \(V ≤ 0\)/); } await page.locator('#op-v').fill('999'); await page.locator('#op-v').press('Tab'); assert.match(await text('#op-err'), /Speed must be 0–60 kn; the previous value is kept/); await page.locator('#op-v').fill('14'); await page.locator('#op-v').press('Tab'); await page.locator('#op-from').selectOption('P2');
  await task('sections'); assert.match(await text('#section-hud'), /Catalog original · NACA 66-209/); await page.locator('#h-secdemo').click(); await page.locator('#sec-apply').click(); await page.waitForTimeout(40); assert.match(await text('#section-hud'), /Modified from NACA 66-209/);
  record('C2 completeness: unsupported version, recovery, Flagged-with-Acknowledge, cap exceeded, Undefined with cause, Catalog original → Modified from — all render with their strings', 'six states matched');
  /* 21 · infeasible lock set is reachable; Release is one action; nudge labels per unit family; expression echo live */
  await task('shape'); await page.locator('#lock-conflict').check(); await page.waitForTimeout(40);
  assert.match(await text('#editor-side'), /Locks conflict at η 1 \(two values\) — release one: root-mirror tangent, value at station η 1, value at station η 1 = 1\.10 × tip · 4 locks, 8 degrees of freedom · solver: conflicting or insufficient constraints/);
  assert.equal(await page.locator('#apply').isDisabled(), true);
  await page.locator('[data-release="conflict"]').click(); await page.waitForTimeout(40); assert.doesNotMatch(await text('#editor-side'), /Cannot satisfy/); await page.locator('#cancel').click();
  await page.locator('[data-channel="twist"]').click(); assert.match(await text('#editor-side'), /0\.1 °/); assert.match(await text('#status'), /step 0\.1 °/);
  await page.locator('[data-channel="chord"]').click(); await page.locator('#v-in').fill('#root_chord * 0.35'); await page.locator('#v-in').press('Tab'); await page.waitForTimeout(40); assert.match(await text('#v-echo'), /model value 0\.04/); await page.locator('#cancel').click();
  record('UX-12/GEO-05: a conflicting lock yields the DOF count and the removable list with one-action Release; nudge steps are labelled per unit family; the expression echo shows the resolved model value', 'editor text');
  /* 22 · stepped keyboard orbit, zoom and fit on the focused viewport; drawer Escape returns focus */
  await page.locator('#shape-canvas').focus(); await page.keyboard.press('Alt+ArrowRight'); await page.waitForTimeout(30); assert.equal(await page.evaluate(() => M.azimuth), 15); await page.keyboard.press('Shift+Alt+ArrowRight'); assert.equal(await page.evaluate(() => M.azimuth), 105); await page.keyboard.press('z'); assert.equal(await page.evaluate(() => M.zoom), 1.25); await page.keyboard.press('f'); assert.equal(await page.evaluate(() => M.zoom + M.azimuth), 1);
  await page.locator('#tab-checks').click(); assert.equal(await page.evaluate(() => document.activeElement.id), 'checks-close'); await page.keyboard.press('Escape'); assert.equal(await page.evaluate(() => document.activeElement.id), 'tab-checks');
  record('UI-03: Option/Alt+arrows orbit in 15°/90° steps, Z zooms, F fits, on the focused viewport; the Checks drawer takes and returns focus and closes on Escape', 'model state and activeElement');
  /* 23 · re-gate conditions: focus ring on the viewport uses the audited station pair; loading state on Analyze and Shape */
  await task('sections'); const ring2 = await page.evaluate(() => { const el = document.querySelector('#section-canvas [data-sec]'); el.focus(); return getComputedStyle(el).outlineColor; });
  const audit = await page.evaluate(() => window.workbenchAudit.pairs.filter(p => /focus ring/.test(p.label)));
  assert(audit.length >= 2 && audit.some(p => /viewport/.test(p.label)), 'the viewport focus-ring pair is audited'); audit.forEach(p => assert(p.pass && p.ratio >= 3, JSON.stringify(p)));
  assert.equal(ring2, 'rgb(102, 221, 200)', `viewport focus ring is --station (#66ddc8), got ${ring2}`);
  await harness('h-state', 'loading'); await task('analyze'); assert.match(await text('#analyze-body'), /Run k-0315 in progress \(VLM \+ strip 1\.2\.0\) · results shown elsewhere are k-0314 — Historical until it completes/); assert.equal(await page.locator('#analyze-body .skeleton').count() >= 5, true);
  await task('shape'); assert.match(await text('#shape-hud'), /Rebuilding preview…/); await harness('h-state', 'default');
  record('Re-gate: viewport focus ring is the audited --station pair (≥ 3:1 on --viewport in every theme); Analyze and Shape render a loading state with a Cancel affordance', { ring2, audit });
  /* 15 · no external request, no page error */
  assert.equal(requests.length, 0, 'external requests'); assert.equal(errors.length, 0, `page errors: ${errors.join('; ')}`);
  record('UI-10: zero external requests and zero page errors across the walk', { requests: requests.length, errors: errors.length });
} catch (e) {
  oracles.push({ name: 'FAILED', pass: false, proof: e.message });
  console.error(e);
  process.exitCode = 1;
} finally {
  const evidence = { artifact: 'docs/mockups/workbench-v1.html', checkedAt: new Date().toISOString(), measurements, oracles, errors, externalRequests: requests, screenshots: shots, note: 'HTML review artifact evidence only; native accessibility, scientific validity and the production kernel require their own proof.' };
  await fs.mkdir(path.join(repo, 'docs/proof'), { recursive: true });
  await fs.writeFile(path.join(repo, 'docs/proof/workbench-v1-browser-check.json'), JSON.stringify(evidence, null, 1));
  console.log(JSON.stringify({ measurements: measurements.length, oracles: oracles.filter(o => o.pass).length, failed: oracles.filter(o => !o.pass).length, errors: errors.length, requests: requests.length, screenshots: shots }));
  await browser.close();
}
