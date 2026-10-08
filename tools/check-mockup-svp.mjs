#!/usr/bin/env node
// Browser oracle for docs/mockups/w2-save-picker.html (Windows save picker, OneDrive refusal, unfinished-save recovery).
// It verifies the review artifact's copy, focus, keyboard, live-region, size and contrast contracts; it proves nothing about the
// native Windows dialog or the store. Ring: on-demand, with the mockup (not in run-tests.sh). Cost: about 40 s.
// Usage: node tools/check-mockup-svp.mjs <node_modules dir containing playwright> [<screenshot dir>]
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const moduleRoot = process.argv[2];
const shotDir = process.argv[3];
const { chromium } = await import(moduleRoot ? pathToFileURL(path.join(moduleRoot, 'playwright', 'index.mjs')).href : 'playwright');
const norm = s => s.replace(/[’]/g, "'").replace(/\s+/g, ' ').trim();

// ---------------------------------------------------------------- the sources the page's copy must equal
const rulings = await fs.readFile(path.join(repo, 'docs/notes/rulings.md'), 'utf8');
const r146 = rulings.slice(rulings.indexOf('### Ruling 146'), rulings.indexOf('### Ruling 147'));
const quote = re => { const m = r146.match(re); assert.ok(m, `Ruling 146 quote not found: ${re}`); return norm(m[1]); };
const SRC = {
  R1: quote(/\(1\) OneDrive refusal copy: "([^"]+)"/),
  R2: quote(/\(2\) Crash-recovery copy, [^:]*: "([^"]+)"/),
  R3: quote(/\(3\) The Windows default save folder is (%USERPROFILE%\\CFD Workbench)/)
};
const md = await fs.readFile(path.join(repo, 'docs/mockups/w2-save-picker.md'), 'utf8');
for (const m of md.matchAll(/^\| (COPY-4\d\d) \| (.+?) \| proposed — awaiting operator \|/gm)) SRC[m[1]] = norm(m[2]);

const browser = await chromium.launch({ headless: true, channel: 'chrome' });
const page = await browser.newPage({ viewport: { width: 1700, height: 1200 } });
const errors = [], requests = [];
page.on('pageerror', e => errors.push(e.message));
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('request', r => { if (!r.url().startsWith('file:') && !r.url().startsWith('data:')) requests.push(r.url()); });
await page.goto(pathToFileURL(path.join(repo, 'docs/mockups/w2-save-picker.html')).href);

const groups = [];
const group = async (name, fn) => { const before = errors.length; let n; try { n = await fn(); } catch (e) { console.error(`FAIL ${name}: ${e.message}`); process.exitCode = 1; groups.push([name, 'FAIL']); return; } groups.push([name, `ok${n === undefined ? '' : ' (' + n + ')'}`]); void before; };

const COMBOS = [];       // every state x detail x variant the harness can show
for (const state of ['s1', 's2', 's3', 's4', 's5']) {
  const details = await page.evaluate(s => { const sel = document.getElementById('state'); sel.value = s; sel.dispatchEvent(new Event('change')); return [...document.getElementById('detail').options].map(o => o.value); }, state);
  for (const detail of details) for (const variant of ['A', 'B']) COMBOS.push({ state, detail, variant });
}
const show = async (c, extra = {}) => {
  await page.evaluate(({ c, extra }) => { document.getElementById('theme').value = extra.theme || 'light'; document.getElementById('theme').dispatchEvent(new Event('change'));
    document.getElementById('vp').value = extra.vp || '1280'; document.getElementById('vp').dispatchEvent(new Event('change'));
    window.__svp.setState({ state:c.state, detail:c.detail, variant:c.variant }, true); }, { c, extra });
};
const label = c => `${c.state}/${c.detail}/${c.variant}`;

// 1. every rendered product sentence equals its ruling or proposed-copy source
await group('copy equals source', async () => {
  const ids = await page.evaluate(() => Object.keys(window.__svp.COPY));
  for (const id of ids) assert.ok(SRC[id] !== undefined, `${id} has no ruling or md source row`);
  const page_copy = await page.evaluate(() => Object.fromEntries(Object.entries(window.__svp.COPY).map(([k, v]) => [k, v.text])));
  for (const id of ids) assert.equal(norm(page_copy[id]), SRC[id], `COPY ${id} differs from its source`);
  let checked = 0;
  for (const c of COMBOS) {
    await show(c);
    const r = await page.evaluate(() => {
      const code = window.__svp.CODE, out = [], text = n => n.textContent.replace(/\s+/g, ' ').trim();
      for (const n of document.querySelectorAll('[data-copy]')) {
        const id = n.dataset.copy;
        if (id === 'R2-lead' || id === 'R2-steps' || /^R2-step\d$/.test(id)) continue;
        out.push([id, text(n), n.tagName]);
      }
      const leads = document.querySelectorAll('[data-copy="R2-lead"]');
      for (const lead of leads) { const steps = [...lead.parentElement.querySelectorAll('li[data-copy^="R2-step"]')].map(li => text(li));
        out.push(['R2', text(lead) + ' ' + steps.map((s, i) => `${i + 1}. ${s}`).join(' '), 'P+OL', steps.length]); }
      const unlabeled = [...document.querySelectorAll('#layer *, #alertBand *, #statusMsg *')].filter(n => !n.closest('[data-chrome]') && !n.closest('[data-copy]') && [...n.childNodes].some(k => k.nodeType === 3 && k.textContent.trim()) && !(n.matches('[data-copy]'))).map(n => n.textContent.trim());
      return { out, unlabeled, code };
    });
    assert.deepEqual(r.unlabeled, [], `${label(c)}: product text without a copy id`);
    for (const [id, got, , steps] of r.out) {
      assert.ok(SRC[id] !== undefined, `${label(c)}: ${id} unknown`);
      assert.equal(got, SRC[id].replace('<code>', r.code), `${label(c)}: ${id} text differs from source`);
      if (id === 'R2') assert.equal(steps, 3, `${label(c)}: crash text must render three steps`);
      checked++;
    }
    // each state shows the copy it exists for
    const ids2 = r.out.map(o => o[0]);
    if (c.state === 's2' && c.variant === 'A') assert.ok(ids2.includes('R1'), `${label(c)}: Ruling 146 (1) text missing`);
    if (c.state === 's2' && c.variant === 'B') assert.ok(ids2.includes('R1'), `${label(c)}: band text missing`);
    if (c.state === 's3') assert.ok(ids2.includes('R2'), `${label(c)}: Ruling 146 (2) text missing`);
  }
  // Ruling 146 (3): the folder is named in S1's start folder, with the display name
  await show({ state:'s1', detail:'present', variant:'A' });
  const t = await page.locator('#native').innerText();
  assert.ok(t.includes('C:\\Users\\<you>\\CFD Workbench') && t.includes('Save native CFD Workbench project') && t.includes('foil.cfdw.json'), 'S1 does not show title, start folder and suggested name');
  assert.ok(!/Documents/.test(t), 'S1 start folder must not sit under Documents');
  return checked;
});

// 2. no placeholder, NaN or leaked token in any state x variant x theme
await group('no placeholder or NaN', async () => {
  let n = 0;
  for (const theme of ['light', 'dark']) for (const c of COMBOS) {
    await show(c, { theme });
    const body = await page.evaluate(() => document.body.innerText);
    assert.ok(!/NaN|undefined|null|\[object|TODO|lorem|<code>|\{[a-z]+\}|Unavailable —/i.test(body), `${label(c)} ${theme}: placeholder in page text`);
    n++;
  }
  return n;
});

// 3. focus: in on open, back to the opener on close; Escape; Enter; Tab trap
await group('focus, Escape, Enter and Tab', async () => {
  const active = () => page.evaluate(() => { const a = document.activeElement; return { id:a.id, act:a.dataset.act || '', inDlg:!!a.closest('.dlg'), def:a.dataset.default === 'true' }; });
  const dialogs = [{ state:'s2', detail:'pick', variant:'A', def:'choose' }, { state:'s3', detail:'blocked', variant:'A', def:'saveas' }, { state:'s5', detail:'check-failed', variant:'A', def:'choose' }];
  for (const d of dialogs) {
    await show(d);
    let a = await active();
    assert.ok(a.inDlg && a.def && a.act === d.def, `${label(d)}: focus must land on the default button, got ${JSON.stringify(a)}`);
    await page.keyboard.press('Escape');
    a = await active();
    assert.equal(a.id, 'saveBtn', `${label(d)}: focus must return to the opener after Escape`);
    assert.equal(await page.locator('.dlg').count(), 0, `${label(d)}: Escape must close the dialog`);
    // Cancel button returns focus too
    await page.click('#rerun');
    await page.locator('[data-act="cancel"]').click();
    assert.equal((await active()).id, 'saveBtn', `${label(d)}: Cancel must return focus to the opener`);
    // Tab trap
    await page.click('#rerun');
    const count = await page.locator('.dlg button').count();
    for (let i = 0; i < count * 2 + 1; i++) { await page.keyboard.press('Tab'); assert.ok((await active()).inDlg, `${label(d)}: Tab left the dialog`); }
    for (let i = 0; i < count + 1; i++) { await page.keyboard.press('Shift+Tab'); assert.ok((await active()).inDlg, `${label(d)}: Shift+Tab left the dialog`); }
  }
  // Enter activates the default when focus is on the dialog but not on a button; Enter on a focused button activates that button
  await show({ state:'s2', detail:'pick', variant:'A' });
  await page.evaluate(() => document.getElementById('dB').setAttribute('tabindex', '-1'));
  await page.evaluate(() => document.getElementById('dB').focus());
  await page.keyboard.press('Enter');
  assert.equal(await page.evaluate(() => window.__svp.S.state), 's1', 'Enter must activate the default (Choose another folder…)');
  assert.ok(await page.evaluate(() => window.__svp.S.events.includes('reopen-picker:default-folder')), 'Choose another folder… must reopen the picker at the default folder');
  assert.ok(await page.evaluate(() => document.activeElement.id === 'native'), 'focus must move to the reopened native picker');
  // S3 Clear opens the confirm over the crash dialog; Escape on it returns to the Clear button; the safe default is Cancel
  await show({ state:'s3', detail:'blocked', variant:'A' });
  await page.locator('[data-act="clear"]').click();
  let a = await active();
  assert.ok(a.inDlg && a.act === 'cancel2' && a.def, `confirm: focus must land on Cancel (the safe default), got ${JSON.stringify(a)}`);
  await page.keyboard.press('Escape');
  a = await active();
  assert.equal(a.act, 'clear', 'confirm: Escape must return focus to Clear unfinished save…');
  assert.equal(await page.locator('.dlg').count(), 1, 'confirm: the crash dialog must be back');
  // Enter on the confirm's default is Cancel and clears nothing
  await page.locator('[data-act="clear"]').click();
  await page.keyboard.press('Enter');
  assert.ok(!(await page.evaluate(() => window.__svp.S.events)).includes('clear'), 'Enter on the confirm must not clear');
  // Variant B (band): the band does not steal focus, its actions work and Cancel hides it
  await show({ state:'s3', detail:'blocked', variant:'B' });
  assert.equal((await active()).id, '', 'S3-B: nothing should be focused inside a dialog');
  await page.locator('#alertBand [data-act="cancel"]').click();
  assert.equal(await page.locator('#alertBand').isHidden(), true, 'S3-B: Cancel must hide the band');
  assert.equal((await active()).id, 'saveBtn', 'S3-B: Cancel must return focus to Save');
  return 'ok';
});

// 4. the order of events: nothing written into OneDrive, Clear never silent, no recovery announcement
await group('flow order and honest outcomes', async () => {
  await show({ state:'s2', detail:'pick', variant:'A' });
  let ev = await page.evaluate(() => window.__svp.S.events);
  assert.deepEqual(ev, ['pick:onedrive', 'check:onedrive', 'refuse'], 'S2: the check must follow the pick and precede any write');
  assert.ok(!ev.some(e => /write|save/.test(e)), 'S2: nothing may be written');
  await show({ state:'s3', detail:'blocked', variant:'A' });
  ev = await page.evaluate(() => window.__svp.S.events);
  assert.ok(!ev.includes('clear'), 'S3: opening the block must not clear anything');
  await page.locator('[data-act="clear"]').click();
  ev = await page.evaluate(() => window.__svp.S.events);
  assert.ok(!ev.includes('clear'), 'S4: opening the confirm must not clear anything');
  await page.locator('[data-act="doclear"]').click();
  ev = await page.evaluate(() => window.__svp.S.events);
  assert.deepEqual(ev.slice(-2), ['clear', 'save'], 'S4a: cleared, then the normal Save proceeds');
  const t = await page.evaluate(() => document.body.innerText);
  assert.ok(!/recover|success|restored|fixed/i.test(await page.locator('#shell').innerText()), 'S4a: the app must not announce a recovery');
  assert.equal(await page.locator('#statusMsg').innerText(), 'Saving…');
  // another window holds it: refused, nothing cleared, the document is still dirty
  await show({ state:'s3', detail:'blocked', variant:'A' });
  await page.check('#holder');
  await page.locator('[data-act="clear"]').click();
  await page.locator('[data-act="doclear"]').click();
  ev = await page.evaluate(() => window.__svp.S.events);
  assert.ok(ev.includes('clear:refused') && !ev.includes('clear') && !ev.includes('save'), 'S4b: refusal must not clear or save');
  assert.ok(await page.locator('.dlg.confirm [data-copy="COPY-449"]').count() === 1, 'S4b: the refusal sentence is missing');
  await page.uncheck('#holder');
  // the document stays dirty in every S2/S3 state
  for (const c of COMBOS.filter(c => ['s2', 's3'].includes(c.state))) { await show(c); assert.ok(await page.locator('#dirty').isVisible(), `${label(c)}: the dirty marker must stay`); }
  void t;
  return 'ok';
});

// 5. dialog semantics and one live region
await group('semantics and live region', async () => {
  for (const theme of ['light']) for (const c of COMBOS) {
    await show(c, { theme });
    const r = await page.evaluate(() => {
      const live = [...document.querySelectorAll('[aria-live], [role="alert"], [role="status"], [role="log"]')].map(n => n.id || n.tagName);
      const dlgs = [...document.querySelectorAll('.dlg')].map(d => ({ role:d.getAttribute('role'), modal:d.getAttribute('aria-modal'),
        title:document.getElementById(d.getAttribute('aria-labelledby'))?.textContent, desc:!!document.getElementById(d.getAttribute('aria-describedby')) }));
      const pageLive = document.body.getAttribute('aria-live') || document.getElementById('shell').getAttribute('aria-live') || document.querySelector('main').getAttribute('aria-live');
      return { live, dlgs, pageLive };
    });
    assert.deepEqual(r.live, ['alertBand'], `${label(c)}: the alert band must be the only live region, got ${r.live}`);
    assert.equal(r.pageLive, null, `${label(c)}: no page-wide aria-live`);
    for (const d of r.dlgs) assert.ok(d.role === 'dialog' && d.modal === 'true' && d.title && d.desc, `${label(c)}: dialog semantics ${JSON.stringify(d)}`);
  }
  return COMBOS.length;
});

// 6. sizes after every viewport switch: targets >= 24 px, text >= 12 px, dialogs inside the shell
await group('sizes at both viewports', async () => {
  let n = 0;
  for (const vp of ['1280', '1500', '1280']) for (const c of COMBOS) {
    await show(c, { vp });
    const r = await page.evaluate(() => {
      const bad = [], vis = n => { const b = n.getBoundingClientRect(); const cs = getComputedStyle(n); return b.width > 0 && b.height > 0 && cs.visibility !== 'hidden' && cs.display !== 'none'; };
      for (const n of document.querySelectorAll('button, select, input:not([type=checkbox]), [tabindex="0"]')) { if (!vis(n)) continue; const b = n.getBoundingClientRect(); if (b.width < 24 || b.height < 24) bad.push(['target', n.id || n.textContent.trim(), b.width, b.height]); }
      for (const n of document.querySelectorAll('input[type=checkbox]')) { const b = n.getBoundingClientRect(); if (b.width < 16) bad.push(['checkbox', n.id, b.width]); const lb = n.closest('label').getBoundingClientRect(); if (lb.height < 24) bad.push(['checkbox label', n.id, lb.height]); }
      const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
      for (let t; (t = w.nextNode());) { if (!t.textContent.trim() || ['SCRIPT', 'STYLE'].includes(t.parentElement.tagName)) continue; const p = t.parentElement; if (!vis(p)) continue;
        const fs = parseFloat(getComputedStyle(p).fontSize); if (fs < 12) bad.push(['text', t.textContent.trim().slice(0, 30), fs]); }
      const sh = document.getElementById('shell').getBoundingClientRect(), outside = [];
      for (const d of document.querySelectorAll('.dlg, .native')) { const b = d.getBoundingClientRect(); if (b.left < sh.left || b.right > sh.right || b.top < sh.top || b.bottom > sh.bottom) outside.push(d.className); if (d.scrollWidth > d.clientWidth + 1) outside.push('overflow ' + d.className); }
      const band = document.getElementById('alertBand'); if (!band.hidden && band.scrollWidth > band.clientWidth + 1) outside.push('band overflow');
      return { bad, outside, w:Math.round(sh.width), h:Math.round(sh.height) };
    });
    assert.deepEqual(r.bad, [], `${label(c)} @${vp}: size floor`);
    assert.deepEqual(r.outside, [], `${label(c)} @${vp}: surface leaves the shell`);
    assert.equal(r.w, +vp, `shell width at ${vp}`);
    n++;
  }
  return n;
});

// 7. contrast of the computed colour pairs in both themes (text 4.5:1; control boundary and primary fill 3:1)
await group('contrast, both themes', async () => {
  let pairs = 0;
  for (const theme of ['light', 'dark']) for (const c of COMBOS) {
    await show(c, { theme });
    const r = await page.evaluate(() => {
      const parse = s => { const m = s.match(/rgba?\(([^)]+)\)/); const p = m[1].split(/[ ,\/]+/).map(Number); return { r:p[0], g:p[1], b:p[2], a:p.length > 3 ? p[3] : 1 }; };
      const over = (f, b) => ({ r:f.r * f.a + b.r * (1 - f.a), g:f.g * f.a + b.g * (1 - f.a), b:f.b * f.a + b.b * (1 - f.a), a:1 });
      const bgOf = n => { const chain = []; for (let x = n; x; x = x.parentElement) { const c = parse(getComputedStyle(x).backgroundColor); chain.push(c); if (c.a === 1) break; } let c = chain.pop(); if (c.a !== 1) c = over(c, { r:255, g:255, b:255, a:1 }); while (chain.length) c = over(chain.pop(), c); return c; };
      const lum = c => { const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }; return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b); };
      const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); };
      const out = [], vis = n => { const b = n.getBoundingClientRect(); return b.width > 0 && b.height > 0 && getComputedStyle(n).visibility !== 'hidden'; };
      const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
      for (let t; (t = w.nextNode());) { if (!t.textContent.trim() || ['SCRIPT', 'STYLE', 'OPTION'].includes(t.parentElement.tagName)) continue; const p = t.parentElement; if (!vis(p)) continue;
        const fg = over(parse(getComputedStyle(p).color), bgOf(p)); out.push(['text', t.textContent.trim().slice(0, 28), ratio(fg, bgOf(p)), 4.5]); }
      for (const b of document.querySelectorAll('.dlg button, .band button, .toolbar button, .top button')) { if (!vis(b)) continue; const cs = getComputedStyle(b), par = bgOf(b.parentElement);
        if (b.classList.contains('primary')) out.push(['primary fill', b.textContent.trim(), ratio(parse(cs.backgroundColor), par), 3]);
        else out.push(['control border', b.textContent.trim(), ratio(parse(cs.borderTopColor), par), 3]); }
      for (const d of document.querySelectorAll('.dlg')) out.push(['dialog border', d.dataset.kind, ratio(parse(getComputedStyle(d).borderTopColor), bgOf(d.parentElement)), 1.0]);
      return out;
    });
    for (const [kind, what, got, min] of r) { pairs++; assert.ok(got >= min, `${label(c)} ${theme}: ${kind} "${what}" is ${got.toFixed(2)}:1, needs ${min}:1`); }
  }
  return pairs;
});

// 8. every keyboard claim has a handler and a test; reduced motion and the harness
await group('keyboard claims and motion', async () => {
  const claims = await page.evaluate(() => [...new Set([...document.querySelectorAll('[data-claim]')].map(n => n.dataset.claim))]);
  const registered = await page.evaluate(() => window.__svp.claims);
  assert.deepEqual(claims.sort(), registered.slice().sort(), 'claims shown must equal claims registered');
  const tested = ['focus-in', 'esc', 'enter', 'tab'];            // exercised in the focus group above
  for (const c of claims) assert.ok(tested.includes(c), `claim ${c} has no test`);
  for (const reduce of [false, true]) {
    await page.evaluate(r => { const b = document.getElementById('reduce'); b.checked = r; b.dispatchEvent(new Event('change')); }, reduce);
    await show({ state:'s3', detail:'blocked', variant:'A' });
    const m = await page.evaluate(() => [...document.querySelectorAll('#shell *')].filter(n => { const s = getComputedStyle(n); return parseFloat(s.transitionDuration) > 0 || s.animationName !== 'none'; }).length);
    assert.equal(m, 0, `reduce=${reduce}: no animation or transition may run (hard cut)`);
    assert.equal(await page.evaluate(() => document.documentElement.dataset.reduce), String(reduce));
  }
  await page.evaluate(() => { const b = document.getElementById('reduce'); b.checked = false; b.dispatchEvent(new Event('change')); });
  // the variant list is disabled where the choice does not exist
  await show({ state:'s1', detail:'present', variant:'A' });
  assert.ok(await page.locator('#variant').isDisabled(), 'S1 has no variant');
  await show({ state:'s3', detail:'blocked', variant:'A' });
  assert.ok(await page.locator('#variant').isEnabled(), 'S3 has a variant');
  return claims.length;
});

await group('page is self-contained and error-free', async () => {
  assert.deepEqual(requests, [], 'network requests');
  assert.deepEqual(errors, [], 'page errors');
});

// ---------------------------------------------------------------- screenshots (optional)
if (shotDir) {
  await fs.mkdir(shotDir, { recursive:true });
  const shots = [['S1-first-save', 's1', 'present', 'A', 'light'], ['S2-A-dialog', 's2', 'pick', 'A', 'light'], ['S2-B-band', 's2', 'pick', 'B', 'light'],
    ['S3-A-dialog', 's3', 'blocked', 'A', 'light'], ['S3-B-band', 's3', 'blocked', 'B', 'light'], ['S4a-cleared', 's4', 'cleared', 'A', 'light'], ['S4b-refused', 's4', 'refused', 'A', 'light'],
    ['S4-confirm', 's4', 'confirm', 'A', 'light'], ['S2-A-dialog-dark', 's2', 'pick', 'A', 'dark'], ['S3-A-dialog-dark', 's3', 'blocked', 'A', 'dark'],
    ['S5-waiting', 's5', 'waiting', 'A', 'light'], ['S5-picker-failed', 's5', 'picker-failed', 'A', 'light'], ['S5-folder-missing', 's5', 'folder-missing', 'A', 'light']];
  for (const [name, state, detail, variant, theme] of shots) {
    await show({ state, detail, variant }, { theme });
    await page.screenshot({ path:path.join(shotDir, name + '.png'), clip:await page.locator('#shell').boundingBox().then(b => ({ x:0, y:0, width:Math.max(1700, b.x + b.width + 20), height:b.y + b.height + 360 })) });
  }
}
await browser.close();
const failed = groups.filter(g => g[1] === 'FAIL').length;
for (const [n, r] of groups) console.log(`${r === 'FAIL' ? 'FAIL' : 'PASS'}  ${n} ${r === 'FAIL' ? '' : r}`);
console.log(`check-mockup-svp: ${groups.length - failed}/${groups.length} groups passed`);
process.exit(failed ? 1 : 0);
