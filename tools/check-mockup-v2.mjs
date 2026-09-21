#!/usr/bin/env node
// Browser oracle for docs/mockups/workbench-v2.html — the seven-area mockup built against specification v1.1.
// It verifies the review artifact's interaction contracts; it proves nothing about a native app, a kernel or a solver.
// Usage: node tools/check-mockup-v2.mjs [<node_modules dir containing playwright>]
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const moduleRoot = process.argv[2];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const shots = await fs.mkdtemp(path.join(os.tmpdir(), 'cfd-workbench-v2-review-'));
const browser = await chromium.launch({ headless: true, channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1600, height: 1120 } });
const errors = [], requests = [], measurements = [], oracles = [];
page.on('pageerror', e => errors.push(e.message));
page.on('request', r => { if (/^https?:/.test(r.url())) requests.push(r.url()); });
page.on('dialog', d => d.accept('Reviewed: tip TE rounded in post-processing'));
const record = (name, proof) => oracles.push({ name, pass: true, proof });
const AREAS = ['setup', 'cad', 'analysis', 'experiment', 'run', 'results', 'export'];
const area = async name => { await page.locator(`.area[data-task="${name}"]`).evaluate(el => { el.scrollIntoView({ block: 'center' }); el.click(); }); await page.waitForTimeout(50); };
const harness = async (id, value) => { await page.locator(`#${id}`).selectOption(value); await page.waitForTimeout(50); };
const click = async sel => { await page.locator(sel).first().evaluate(el => { el.scrollIntoView({ block: 'center' }); el.click(); }); await page.waitForTimeout(40); };
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
const smallestText = () => page.evaluate(() => { const out = []; for (const t of document.querySelectorAll('#window svg text')) { const svg = t.ownerSVGElement; const r = svg.getBoundingClientRect(); if (!r.width || !r.height || t.getBoundingClientRect().width === 0) continue; const vb = svg.viewBox.baseVal; const scale = Math.min(r.width / vb.width, r.height / vb.height); out.push({ text: t.textContent.slice(0, 24), px: parseFloat(t.getAttribute('font-size')) * scale }); } for (const c of document.querySelectorAll('#window .caption, #window .hud, #window .tracing, #window figcaption')) { if (c.getBoundingClientRect().width) out.push({ text: c.textContent.slice(0, 24), px: parseFloat(getComputedStyle(c).fontSize) }); } return out.reduce((m, x) => x.px < m.px ? x : m, { px: 1e9, text: 'none' }); });
try {
  await page.goto(pathToFileURL(path.join(repo, 'docs/mockups/workbench-v2.html')).href);
  await page.waitForTimeout(250);
  /* 1 · seven areas × three themes × eight states audit clean; text ≥ 12 px at five viewports */
  for (const theme of ['light', 'dark', 'contrast']) { await harness('h-theme', theme); for (const a of AREAS) { await area(a); await measure(`${theme}/${a}`); if (theme === 'light') await page.screenshot({ path: path.join(shots, `${a}.png`), fullPage: true }); } }
  await harness('h-theme', 'light');
  for (const st of ['first-launch', 'loading', 'empty', 'error', 'partial', 'overflow', 'success']) { await harness('h-state', st); for (const a of ['cad', 'analysis', 'run', 'results']) { await area(a); await measure(`state/${st}/${a}`); const txt = await text('#window'); assert.doesNotMatch(txt, /NaN|Infinity|Unavailable — reason|Locked by <source>/, `${st}/${a}: NaN or placeholder`); } }
  await harness('h-state', 'default');
  const textSizes = {};
  for (const vp of ['minimum', 'desktop', 'lines', 'wide', 'zoom']) { await harness('h-viewport', vp); textSizes[vp] = {}; for (const a of AREAS) { await area(a); const s = await smallestText(); textSizes[vp][a] = s; assert(s.px >= 12, `${vp}/${a}: smallest rendered text "${s.text}" is ${s.px.toFixed(1)} px`); await measure(`viewport/${vp}/${a}`); } }
  await harness('h-viewport', 'wide');
  record('Seven areas render and audit clean in three themes, eight states and five viewports; no NaN or placeholder; smallest text ≥ 12 CSS px', { measurements: measurements.length, textSizes });
  /* 2 · area strip: flow order, readiness chips, C2 strings, gated label */
  await area('cad');
  const strip = await page.locator('#area-strip .area').evaluateAll(els => els.map(e => e.innerText.replace(/\s+/g, ' ')));
  assert.equal(strip.length, 7); assert.match(strip[0], /^1 Setup/); assert.match(strip[6], /^7 Export/);
  assert.match(strip[4], /gated \(SPIKE-03\/04\) · Ready$/); assert.match(strip[5], /gated \(SPIKE-03\/04\) · 10 of 12/); assert.match(strip[1], /r4 · 3 stations/); assert.match(strip[0], /seeded v1/); assert.match(strip[3], /E-1 Draft|Draft/);
  await harness('h-backend', 'none'); assert.match(await text('#area-strip'), /gated \(SPIKE-03\/04\) · Not ready/); await harness('h-backend', 'ready');
  record('UI-18: the area strip shows seven areas in flow order with readiness chips; Run and Results carry the gated label and follow the backend state', strip);
  /* 3 · Setup: two roads, soft targets with deviation, conflict string, both entries seed */
  await area('setup');
  const setup = await text('.task-setup');
  assert.match(setup, /Describe a starting design/); assert.match(setup, /No API key configured — every design and analysis tool works without it/);
  assert.match(setup, /max span .*weight/i); assert.match(setup, /target AR .* 12 ·/); assert.match(setup, /Seeded design preview/);
  await page.locator('#p-purpose').selectOption('wakefoil'); assert.match(await text('#setup-params'), /preset: surf \(wake note, Flagged\)/); await page.locator('#p-purpose').selectOption('wingfoil race');
  await page.locator('[data-t="span"]').fill('800'); await page.locator('[data-t="span"]').press('Tab'); await page.locator('[data-t="chord"]').fill('100'); await page.locator('[data-t="chord"]').press('Tab'); await page.waitForTimeout(50);
  assert.match(await text('#setup-params'), /Targets conflict: target area 1000 cm² and max span 800 mm with max chord 100 mm — seeded the nearest feasible design; targets stay as preferences/);
  await page.locator('[data-t="span"]').fill('1100'); await page.locator('[data-t="span"]').press('Tab'); await page.locator('[data-t="chord"]').fill('120'); await page.locator('[data-t="chord"]').press('Tab');
  await click('#seed-params'); await page.waitForTimeout(60); assert.match(await text('#area-strip'), /seeded v2/); assert.equal(await page.evaluate(() => M.revision), 1);
  await harness('h-capability', 'key-evaluated'); await click('#setup-propose'); await page.waitForTimeout(50);
  const prop = await text('#setup-proposal'); assert.match(prop, /tip chord 34 mm Inferred \("fuller tip"\)/); assert.match(prop, /root t\/c: 0\.24 outside 0\.06–0\.14 — not applied/);
  await click('#setup-accept'); await page.waitForTimeout(60); assert.match(await text('#setup-preview'), /Inferred from "a bit fuller tip"/); await harness('h-capability', 'key-none');
  const ops = await page.locator('#op-table tbody tr').evaluateAll(rows => rows.map(r => [...r.querySelectorAll('td')].map(td => td.innerText.trim())));
  [[10, '0.773', '7.71'], [14, '0.395', '3.93'], [18, '0.239', '2.38'], [15, '0.458', '3.43'], [22, '0.213', '1.59'], [28, '0.132', '0.98'], [32, '0.101', '0.75']].forEach(([v, cl, s], i) => { assert.equal(ops[i][7], cl); assert.equal(ops[i][8], s); });
  record('SET-01..04 / GOAL-02: parameters and language seed the same objects with per-field provenance; soft-target conflict string; purpose → preset; seven pinned triples', 'setup text and table');
  /* 4 · CAD: four curves, outline rails, add/remove station, section editor as a panel, GEO-13 property */
  await area('cad');
  const side = await text('#editor-side'); for (const c of ['Outline', 'Twist', 'Dihedral / anhedral', 'Thickness']) assert.match(side, new RegExp(c));
  assert.match(side, /rail: LE TE \(chord\)/);
  await click('[data-rail="le"]'); assert.equal(await page.evaluate(() => M.channel), 'le'); await click('[data-rail="te"]'); assert.equal(await page.evaluate(() => M.channel), 'chord');
  await click('[data-curve="twist"]'); assert.match(await text('#editor-side'), /0\.1 °/); assert.match(await text('#status'), /step 0\.1 °/);
  await click('[data-curve="elev"]'); assert.match(await page.locator('#editor-svg title').first().textContent(), /Dihedral \/ anhedral distribution/); await click('[data-curve="outline"]');
  const st0 = await page.evaluate(() => M.stations.length); await click('#add-station'); await page.waitForTimeout(50);
  assert.equal(await page.evaluate(() => M.stations.length), st0 + 1); assert.match(await text('#status-msg'), /Station added at η 0\.35 · deviation Not recorded \(fixture/); assert.match(await text('#station-list'), /η 0\.35 authored \(added\) · NACA 66-209/);
  assert.equal(await page.locator('#station-list [data-eta="0.35"]').getAttribute('aria-selected'), 'true', 'the added station is selected'); assert.equal(await page.locator('#remove-station').isDisabled(), false, 'an added station can be removed'); assert.equal(await page.locator('#edit-station').isDisabled(), false, 'an added station can be edited');
  await click('[data-eta="0.6"]'); await click('#remove-station'); await page.waitForTimeout(50); assert.equal(await page.evaluate(() => M.stations.includes(0.6)), false); assert.match(await text('#status-msg'), /Station at η 0\.60 removed · refit deviation Not recorded/);
  await click('[data-eta="0.2"]'); assert.equal(await page.locator('#remove-station').isDisabled(), true, 'a slice cannot be removed');
  await click('[data-eta="0"]'); await click('#edit-station'); assert.equal(await page.locator('#section-dialog').evaluate(d => d.open), true); assert.match(await text('#section-hud'), /Catalog original · NACA 66-209/);
  await click('#h-secdemo'); await page.waitForTimeout(50); assert.equal(await page.evaluate(() => !!M.sectionPreview), true); await page.keyboard.press('Escape'); await page.waitForTimeout(50); assert.equal(await page.evaluate(() => M.sectionPreview), null, 'Escape discards the section draft'); assert.equal(await page.locator('#section-dialog').evaluate(d => d.open), false);
  await click('[data-mode="smooth"]'); await page.locator('[data-point="3"]').focus(); const gaps = [];
  for (const w of [1, 2, 4, 8, 16]) { await page.locator('#w-in').fill(String(w)); await page.locator('#w-in').press('Tab'); await page.waitForTimeout(40); gaps.push(await page.evaluate(() => { const f = M.preview ? M.preview.fit : channelCurve(M.channel); const u = ETA[3]; const v = (M.preview ? M.preview.values : M.curves[M.channel])[3]; return Math.abs(BS.evalCurve(f.P, 5, f.U, u)[1] - v); })); }
  for (let i = 1; i < gaps.length; i++) assert(gaps[i] < gaps[i - 1] && gaps[i] > 0, `GEO-13: ${gaps}`);
  await page.keyboard.press('Escape'); await click('[data-mode="through"]');
  await page.locator('[data-point="1"]').focus(); await page.keyboard.press('ArrowUp'); await page.waitForTimeout(30); assert.equal(await page.evaluate(() => document.activeElement.dataset.point), '1'); await page.keyboard.press('Escape');
  record('CAD-01..03 / GEO-13: four distinct curves with the Outline rails; add and remove station with deviation reported; section editor from a station; monotone weight property; focus survives a nudge', { gaps: gaps.map(g => g.toExponential(2)) });
  /* 5 · CAD ⇄ Analysis toggle preserves selection, camera and hides a preview */
  await page.locator('#shape-canvas').focus(); await page.keyboard.press('Alt+ArrowRight'); const az = await page.evaluate(() => M.azimuth); assert.equal(az, 15);
  await page.locator('[data-point="2"]').focus(); await page.keyboard.press('ArrowUp'); await page.waitForTimeout(30); assert.equal(await page.evaluate(() => !!M.preview), true);
  await click('#toggle-view'); await page.waitForTimeout(60);
  assert.equal(await page.locator('#window').getAttribute('data-task'), 'analysis'); assert.equal(await page.evaluate(() => M.azimuth), 15); assert.equal(await page.evaluate(() => M.station), 2);
  assert.equal(await page.locator('#area-strip .area[data-task="analysis"]').getAttribute('aria-selected'), 'true', 'the area strip follows the toggle');
  assert.equal(await page.locator('#layer-list').isVisible(), true); assert.match(await text('#canvas-title'), /3 · Analysis/); assert.equal(await page.locator('#editor').isVisible(), false, 'no editor (preview hidden) in Analysis'); assert.equal(await page.locator('#preview-hidden').isVisible(), true); assert.match(await text('#preview-hidden'), /Preview hidden — Apply or Cancel in CAD/);
  assert.match(await text('#inspector'), /run manifest/i, 'the Analysis inspector is the run manifest, not the CAD station editor'); assert.equal(await page.locator('#lock-root').count(), 0);
  await click('#toggle-view'); await page.waitForTimeout(60); assert.equal(await page.locator('#window').getAttribute('data-task'), 'cad'); assert.equal(await page.evaluate(() => !!M.preview), true, 'preview restored untouched'); await page.keyboard.press('Escape');
  record('ANA-22 / UX-18: the toggle is navigation between areas 2 and 3; camera, station and selection survive; the preview is hidden in Analysis and restored on return', { az });
  /* 6 · Analysis visuals on the geometry: vectors, loading strips, depth band, Cp on the section; strings verbatim */
  await area('analysis'); await click('[data-view="plan"]'); await page.waitForTimeout(50); assert.equal(await page.locator('#canvas-single svg').first().getAttribute('role'), 'group', 'the plan SVG is a group so its layer names are exposed'); const ariaPlan = await page.locator('#canvas-single').ariaSnapshot(); assert.match(ariaPlan, /lift vector at η [0-9.]+: \d+ N/, 'layer names are in the accessibility tree: ' + ariaPlan.slice(0, 400)); assert.match(ariaPlan, /depth band h_ref/); assert.match(ariaPlan, /depth band h_ref/); const layers = await page.evaluate(() => [...document.querySelectorAll('#canvas-single [role=img]')].map(g => g.getAttribute('aria-label')));
  assert(layers.some(l => /lift vector at η/.test(l)) && layers.some(l => /resultant lift .* N at the center of lift/.test(l)) && layers.some(l => /depth band h_ref 0\.50 m/.test(l)), JSON.stringify(layers).slice(0, 300));
  await page.locator('[data-layer="force"]').uncheck(); await page.waitForTimeout(40); assert.equal((await page.evaluate(() => [...document.querySelectorAll('#canvas-single [role=img]')].filter(g => /lift vector/.test(g.getAttribute('aria-label'))).length)), 0); await page.locator('[data-layer="force"]').check();
  await page.locator('#h-depth').uncheck(); await page.waitForTimeout(40); assert.match(await text('#layer-list'), /Unavailable — depth not set/); assert.match(await text('#cond-band'), /Unavailable — depth not set/); await page.locator('#h-depth').check();
  await page.locator('[data-layer="sep"]').uncheck(); assert.equal(await page.evaluate(() => M.res.layers.sep), true, 'Analysis layers do not share Results layer state'); await page.locator('[data-layer="sep"]').check();
  const abody = await text('#analyze-body'); assert.match(abody, /practitioner range; no measured water N-factor; Day 2019 used 4/); assert.match(abody, /vik v8 \(Crameri\) diverging, zero pinned/);
  await click('#scope-group [data-scope="wing"]'); await page.waitForTimeout(50); const wing = await text('#analyze-body'); assert.match(wing, /Structural: Not assessed/); assert.match(wing, /Loads are hydrodynamic estimates\. Not a structural assessment\./); assert.match(wing, /Static geometry; steady analysis cannot predict ventilation onset/);
  await click('#scope-group [data-scope="section"]');
  record('ANA-21 / ANA-23 / UI-19: force vectors, resultant, loading strips and the depth band render on the geometry with accessible names; layers toggle; depth-unset absence; fixed strings verbatim', layers.length);
  /* 7 · Experiment: sweep preview, invalid sample blocked, optimize single-point refusal, Queue, no Run verb */
  await area('experiment'); const ex = await text('#experiment-body'); assert.match(ex, /Case preview · 12 unique cases/); const expButtons = await page.locator('#experiment-body button').evaluateAll(bs => bs.map(b => b.innerText.trim())); assert(!expButtons.some(x => /^Run(\s|$)/.test(x)), 'no Run verb in Experiment: ' + expButtons.join(' | '));
  await page.locator('#x-speeds').fill('10, 10, abc'); await page.locator('#x-speeds').press('Tab'); assert.match(await text('#x-err'), /Blocked: speeds must be a non-empty list of unique finite numbers > 0 in kn/);
  await click('[data-kind="optimize"]'); await page.waitForTimeout(40); assert.match(await text('#experiment-body'), /Thickness is frozen until a structural proxy row or the Beam tier exists — without one, thickness collapses to the floor \(Garg 2017\)/); assert.equal(await page.locator('#experiment-body input[aria-label="freeze t/c η 0"]').isDisabled(), true); assert.match(await text('#experiment-body'), /A_cav ≤ 5×10⁻⁴ \(k = 10\)/); await page.locator('#o-obj').selectOption({ index: 1 }); await page.waitForTimeout(40);
  assert.match(await text('#experiment-body'), /Single-point optimization fills the design to one condition and degrades the others \(Drela 1998; Garg 2017\) — add at least a second operating point or accept the multipoint default/); assert.equal(await page.locator('#queue').isDisabled(), true);
  await click('#o-multipoint'); await page.waitForTimeout(40); assert.equal(await page.locator('#queue').isDisabled(), false); assert.match(await text('#experiment-body'), /Results are Candidates on the COMMIT-01 ladder; Accept opens a Geometry edit draft/);
  await click('[data-kind="sweep"]'); await click('#queue'); await page.waitForTimeout(50); assert.match(await text('#queue'), /Queued · version 1 is immutable/); assert.match(await text("#area-strip"), /Queued v1/);
  record('XS-01..03: 12-case preview with derived quantities; invalid sample blocked with the input named; single-point objective refused; Queue makes the version immutable; Experiment has no Run verb', 'experiment text');
  /* 8 · Run: environment readiness, allow-listed steps, state machine, mesh gate, cancel, retry, failed-no-outputs */
  await area('run'); await harness('h-backend', 'detected'); let run = await text('#run-body'); assert.match(run, /Backend not ready — Docker Desktop 4\.41: smoke test not run at the pinned version · Check · Prepare my environment/);
  await click('#env-prepare'); await page.waitForTimeout(40); run = await text('#run-body'); assert.match(run, /Step 1 of 4: Install Docker Desktop 4\.41 — runs open https/); assert.match(run, /Step refused: outside the allow-list — a proposed curl … \| sh was rejected/);
  await harness('h-capability', 'key-evaluated'); await click('[data-consent="0"]'); await page.waitForTimeout(40); assert.match(await text("#run-body"), /Step 1 succeeded · elevation used \(OS prompt\)/); await harness('h-capability', 'key-none'); await harness('h-backend', 'ready');
  run = await text('#run-body'); assert.match(run, /Unsupported on OpenFOAM ESI v2606: transient requested; capability record: steady only — case stays Pending/); assert.match(run, /Failed — no outputs \(exit 0 but the final time directory is missing\)/);
  await harness('h-backend', 'none'); assert.match(await text('#run-body'), /Not ready — no backend row matches the pin OpenFOAM ESI v2606 sha256:3f9c…/); await harness('h-backend', 'ready');
  await click('[data-runsel="0"]'); assert.equal(await page.locator('#run-cancel').isDisabled(), true, 'Cancel is disabled on a terminal case'); await click('#run-all'); await click('#h-runstep'); await click('#h-runstep'); await page.waitForTimeout(40); run = await text('#run-body'); assert.match(run, /Meshing · [0-9.×⁰-⁹]+ cells so far/); await click('#h-runstep'); await page.waitForTimeout(40);
  run = await text('#run-body'); assert.match(run, /Mesh gate passed: y\+ 0\.9 ≤ 1 · 22 layer points ≥ 20 · expansion 1\.15 ≤ 1\.2 · non-orthogonality 58° ≤ 65°/); assert.match(run, /Readiness check/);
  await click('#h-meshfail'); await page.waitForTimeout(40); assert.match(await text('#run-body'), /Mesh gate failed: max non-orthogonality 71° vs threshold 65° — stopped before solving/); await click('#h-meshfail');
  await click('#h-runstep'); await page.waitForTimeout(40); assert.match(await text('#run-body'), /Solving · iteration \d+ · residuals [0-9.e+-]+ · elapsed Not recorded · remaining Not recorded/); assert.match(await text('#status-msg'), /Run fixture stepped to Solving/);
  await click('#run-cancel'); await page.waitForTimeout(40); assert.match(await text('#run-body'), /Cancelled — process tree terminated at the substrate; partial outputs retained/); assert.match(await text('#run-body'), /ranks 6 of 6/);
  await click('[data-retry="6"]'); await page.waitForTimeout(40); assert.match(await text('#run-body'), /attempt 2 · prior attempt provenance retained/); await click('[data-retry="0"]'); await page.waitForTimeout(40); assert.equal(await page.evaluate(() => SAMPLES[0].state), 'Completed', 'a cancelled case can be retried');
  record('RUN-01..06: readiness with allow-listed steps and a refused step; Unsupported and Failed-no-outputs strings; state machine stepped through mesh gate, solving, cancel; retry keeps provenance; unmeasured values say Not recorded', 'run console text');
  /* 9 · Results: layers with absent reasons, separation only with τ_w, replay held variable, gaps, difference flood, candidates */
  await area('results'); let res = await text('#results-body');
  assert.match(res, /Moving dashes are an affordance, not fluid motion/); assert.match(res, /separation · C_f,x sign change at x\/c 0\.78 \(τ_w present\)/); assert.match(res, /Separation — Unavailable — field missing in run \(τ_w\)|Separation \(τ_w criterion\)/); assert.match(res, /range −1\.60…\+1\.00 \(fixed across the series\)/); assert.equal(await page.locator('#results-body svg').first().getAttribute('role'), 'group');
  const ariaRes = await page.locator('#results-body').ariaSnapshot(); assert.match(ariaRes, /separation layer: skin-friction-line convergence/, 'the separation layer is exposed'); assert.match(res, /Difference flood · s0[1-4] − s01/); assert.doesNotMatch(res, /A_cav [0-9.]+ ≤ 0\.02/);
  assert.match(res, /Modeled turbulent kinetic energy k · m²\/s² — Unavailable — not computed/); assert.match(res, /Parametric sweep — sequence over admitted samples · held speed 10 kn/);
  await click('[data-rsample="3"]'); await page.waitForTimeout(50); res = await text('#results-body'); assert.match(res, /No supported criterion in this sample — τ_w absent; vortex-core candidates are not separation/);
  await click('[data-rsample="11"]'); await page.waitForTimeout(50); res = await text('#results-body'); assert.match(res, /Unsupported — no evidence for this sample/); assert.doesNotMatch(res, /L \d+ N · D/, 'no forces drawn for an unsupported sample');
  await click('[data-held="alpha"]'); await page.waitForTimeout(40); assert.match(await text('#results-body'), /held angle/); await click('[data-rsample="4"]'); await page.waitForTimeout(50); assert.match(await text('#results-body'), /Unavailable — mesh differs/); await click('[data-rsample="8"]'); await page.waitForTimeout(50); assert.match(await text('#results-body'), /Reduction failed for s09: pvbatch 5\.13, exit 1 — layers Unavailable; raw case retained/);
  await click('[data-rsample="0"]'); await click('#tl-next'); await page.waitForTimeout(40); assert.equal(await page.evaluate(() => SAMPLES[M.res.sample].id), 's05');
  await click('[data-held="speed"]'); await click('[data-rsample="0"]'); assert.equal(await page.locator('#tl-play').getAttribute('aria-disabled'), 'true', 'Play is disabled with its reason in the artifact'); await page.locator('#h-motion').check(); await page.waitForTimeout(40); assert.equal(await page.locator('#tl-play').count(), 0, 'reduced motion: no Play'); assert.match(await text('#results-body'), /reduced motion: stepping only/); assert.equal(await page.locator('.flow-anim').first().evaluate(el => getComputedStyle(el).animationName), 'none'); await page.locator('#h-motion').uncheck();
  await click('[data-twin="sweep"]'); assert.match(await text('#table-body'), /α °/); await click('#table-close'); for (const tw of ['flood', 'multiples', 'diff']) { await click(`[data-twin="${tw}"]`); assert.match(await text('#table-body'), /x\/c|sample/); await click('#table-close'); }
  await harness('h-state', 'success'); await page.waitForTimeout(40); res = await text('#results-body'); assert.match(res, /c-01 · vlm-checked · tier VLM \+ strip · 84 evaluations · Accept opens an edit draft/); assert.match(res, /Pareto · objective vs A_cav/); assert.match(res, /4\.1e-3 > 5\.0e-4 violated/); assert.match(res, /1\.2e-4 ≤ 5\.0e-4 satisfied/); assert.match(res, /Inferred on the 2D strip-Cp basis; spike Q-A_cav/);
  assert.match(res, /Accept disabled — base r4 ≠ current r\d+/, 'a moved base disables Accept'); await click('[data-rebase="c-01"]'); await page.waitForTimeout(40); assert.equal(await page.locator('#window').getAttribute('data-task'), 'cad'); assert.match(await text('#status-msg'), /Rebase c-01 onto r\d+: draft opened with the deviation reported/); await harness('h-state', 'default');
  record('RES-01..05: layers from the manifest with absent reasons; separation only with τ_w; replay over admitted samples with the held variable; reduced motion removes Play and the dash animation; candidates with the ladder and Accept → CAD draft', 'results text');
  /* 10 · Export: 3DM and Fusion-ready STEP, STEP gated, safety string */
  await area('export'); const exp = await text('#export-body'); assert.match(exp, /3DM \(Rhino\)/); assert.match(exp, /Fusion-ready STEP \(closed shell\)/); assert.doesNotMatch(exp, /\.f3d/); assert.match(exp, /STEP export unavailable until the open-and-measure fixture exists/); assert.match(exp, /Test before use\./);
  record('EXP-02/04/05: export matrix with 3DM, Fusion-ready STEP (no .f3d promise), gated STEP and the safety string', 'export text');
  /* 11 · Prompt entry in every area with the fixed entry name and kind; no action in Export */
  const entries = {};
  for (const a of AREAS) { await area(a); entries[a] = await text('#prompt-entry'); }
  assert.match(entries.setup, /Describe a starting design/); assert.match(entries.cad, /Describe a change to the shape/); assert.match(entries.analysis, /Ask about this calculation/); assert.match(entries.experiment, /Describe the experiment/); assert.match(entries.run, /Explain this failure/); assert.match(entries.results, /Ask about this result/); assert.match(entries.export, /No assistant action here/);
  for (const a of AREAS.slice(0, 6)) assert.match(entries[a], /No API key configured — every design and analysis tool works without it/);
  await harness('h-capability', 'key-evaluated'); await area('cad'); await click('#prompt-entry [data-propose]'); await page.waitForTimeout(40);
  const cadp = await text('#prompt-entry'); assert.match(cadp, /geometry-edit proposal · prompt v3 · same edit-draft path as a pointer edit/); assert.match(cadp, /rejected: a proposal never emits coordinates/);
  const r0 = await page.evaluate(() => M.revision); await click('#prompt-entry [data-accept="cad"]'); await page.waitForTimeout(60); assert.equal(await page.evaluate(() => M.revision), r0 + 1); assert.equal(await page.evaluate(() => M.stations.includes(0.7)), true);
  await area('experiment'); await click('#prompt-entry [data-propose]'); await page.waitForTimeout(40); assert.match(await text('#prompt-entry'), /18 cases · estimate Not recorded/); await click('#prompt-entry [data-accept="experiment"]'); await page.waitForTimeout(40); assert.match(await text('#experiment-body'), /18 unique cases/);
  await area('results'); await click('#prompt-entry [data-propose]'); await page.waitForTimeout(40); assert.match(await text('#prompt-entry'), /Skin-friction-line convergence|No supported criterion in this sample/);
  await area('analysis'); await click('#prompt-entry [data-propose]'); await page.waitForTimeout(40); assert.match(await text('#prompt-entry'), /Response withheld: it contains a number not in the shared context/);
  await harness('h-capability', 'key-unevaluated'); assert.match(await text('#prompt-entry'), /Unevaluated on this model — proposals disabled until the eval suite passes/); await harness('h-capability', 'key-none');
  record('A5.12 / AI-07..10 / UI-22: every area names its fixed entry point and proposal kind; no-key and unevaluated states; a geometry-edit Accept is one Apply; an experiment-config proposal fills the definition without queueing; explanations cite or withhold', Object.fromEntries(Object.entries(entries).map(([k, v]) => [k, v.slice(0, 60)])));
  /* 12 · character-key shortcut rule: ⇧A alone never toggles; the modifier chord does */
  await area('cad'); await page.locator('#shape-canvas').focus(); await page.keyboard.press('Shift+A'); await page.waitForTimeout(40); assert.equal(await page.locator('#window').getAttribute('data-task'), 'cad', 'Shift+A alone does nothing (SC 2.1.4)'); await page.keyboard.press('Meta+Shift+A'); await page.waitForTimeout(60); assert.equal(await page.locator('#window').getAttribute('data-task'), 'analysis'); await page.keyboard.press('Meta+Shift+A'); await page.waitForTimeout(60);
  record('SC 2.1.4: the CAD ⇄ Analysis toggle is a modifier chord (⌘⇧A / Ctrl+Shift+A), not a bare character key', 'toggle observed only with the modifier');
  /* 13 · hygiene */
  assert.equal(requests.length, 0, 'external requests'); assert.equal(errors.length, 0, `page errors: ${errors.join('; ')}`);
  record('UI-10: zero external requests and zero page errors across the walk', { requests: requests.length, errors: errors.length });
} catch (e) {
  oracles.push({ name: 'FAILED', pass: false, proof: e.message }); console.error(e); process.exitCode = 1;
} finally {
  const evidence = { artifact: 'docs/mockups/workbench-v2.html', checkedAt: new Date().toISOString(), measurements, oracles, errors, externalRequests: requests, screenshots: shots, note: 'HTML review artifact evidence only; native accessibility, scientific validity, solvers and the production kernel require their own proof.' };
  await fs.mkdir(path.join(repo, 'docs/proof'), { recursive: true });
  await fs.writeFile(path.join(repo, 'docs/proof/workbench-v2-browser-check.json'), JSON.stringify(evidence, null, 1));
  console.log(JSON.stringify({ measurements: measurements.length, oracles: oracles.filter(o => o.pass).length, failed: oracles.filter(o => !o.pass).length, errors: errors.length, requests: requests.length, screenshots: shots }));
  await browser.close();
}
