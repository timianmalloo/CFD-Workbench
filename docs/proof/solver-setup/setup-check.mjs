// Browser check + captures for docs/mockups/solver-setup.html.
// argv[2] = playwright module root (node_modules), argv[3] = repo worktree, argv[4] = capture dir
import { createRequire } from 'node:module';
import path from 'node:path';
import fs from 'node:fs';
const require = createRequire(path.join(process.argv[2], 'x.js'));
const { chromium } = require('playwright');
const repo = process.argv[3], out = process.argv[4];
fs.mkdirSync(out, { recursive: true });
const url = 'file://' + path.join(repo, 'docs/mockups/solver-setup.html');
const STATES = { win:['first','nothing','wsl','admin','reboot','resumed','virt','installing','smokefail','unknown','ready'],
                 mac:['first','nothing','existing','download','blocked','smokefail','ready'] };
// the no-command lint (design section 7): a command token followed by a flag or argument, a prompt line, or a code span
const CMD = /\b(wsl(\.exe)?|apt(-get)?|sudo|xattr|spctl|powershell|cmd(\.exe)?|bcdedit|dism|brew|curl|Set-ExecutionPolicy)\s+-{1,2}\w|^\s*[$>#]\s|`/im;
const browser = await chromium.launch({ channel:'chrome' });
const page = await browser.newPage({ viewport:{ width:1330, height:900 } });
const errors = []; page.on('pageerror', e => errors.push(String(e))); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
const findings = [];
const shots = [];
for (const os of Object.keys(STATES)) for (const st of STATES[os]) for (const ai of ['ok', 'withheld', 'nokey']) for (const theme of ['light', 'dark']) {
  if (ai !== 'ok' && theme === 'dark') continue;
  await page.goto(`${url}?os=${os}&st=${st}&ai=${ai}&theme=${theme}`);
  const r = await page.evaluate(() => {
    const live = document.querySelector('#live .shell');
    const txt = live.innerText;
    // visible copy outside Technical details <pre> and outside the drawn OS illustration
    const clone = live.cloneNode(true); clone.querySelectorAll('pre, .os-shot, details.tech:not([open])').forEach(n => n.remove());
    const copy = clone.innerText;
    const small = [], tiny = [];
    for (const el of live.querySelectorAll('*')) {
      const cs = getComputedStyle(el); if (cs.display === 'none' || cs.visibility === 'hidden') continue;
      const b = el.getBoundingClientRect(); if (!b.width || !b.height) continue;
      if ([...el.childNodes].some(n => n.nodeType === 3 && n.textContent.trim()) && parseFloat(cs.fontSize) < 11) tiny.push(el.tagName + ':' + el.textContent.trim().slice(0, 30));
      if (el.matches('button, summary, input, select') && !el.closest('.os-shot') && (b.height < 24 || b.width < 24)) small.push(el.textContent.trim().slice(0, 30) + ` ${b.width.toFixed(0)}x${b.height.toFixed(0)}`);
    }
    const shellBox = live.getBoundingClientRect();
    const overflow = [...live.querySelectorAll('.card, .ai, .rail')].filter(e => e.scrollHeight > e.clientHeight + 1 || e.getBoundingClientRect().bottom > shellBox.bottom).map(e => e.className);
    const cur = live.querySelectorAll('[aria-current="step"]').length;
    const primaries = live.querySelectorAll('.card .actions .btn.primary').length;
    return { txt, copy, small, tiny, overflow, cur, primaries };
  });
  const id = `${os}-${st}-${ai}-${theme}`;
  if (/undefined|NaN|\[object|<\w+>/.test(r.txt)) findings.push(`${id}: placeholder leak`);
  const m = r.copy.match(CMD); if (m) findings.push(`${id}: command in user copy: "${m[0]}"`);
  if (r.small.length) findings.push(`${id}: small targets ${r.small.join(' | ')}`);
  if (r.tiny.length) findings.push(`${id}: text under 11 px ${r.tiny.slice(0, 3).join(' | ')}`);
  if (r.overflow.length) findings.push(`${id}: clipped region ${r.overflow.join(', ')}`);
  if (!['first', 'nothing', 'ready'].includes(st) && r.cur !== 1) findings.push(`${id}: ${r.cur} current steps`); if (st === 'ready' && r.cur !== 0) findings.push(`${id}: ${r.cur} current steps`);
  if (st !== 'first' && r.primaries > 1) findings.push(`${id}: ${r.primaries} primary actions`);
  if (ai === 'ok' && (theme === 'light' || ['smokefail', 'ready'].includes(st))) {
    const f = path.join(out, `${id}.png`); await page.locator('#live .shell').screenshot({ path:f }); shots.push(path.basename(f));
  }
}
// contrast of the token pairs this page adds, per theme
const contrast = await page.evaluate(() => {
  const L = h => { const v = [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16) / 255).map(c => c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4); return 0.2126 * v[0] + 0.7152 * v[1] + 0.0722 * v[2]; };
  const cr = (a, b) => { const [x, y] = [L(a), L(b)].sort((p, q) => q - p); return +((x + 0.05) / (y + 0.05)).toFixed(2); };
  const T = { light:{ canvas:'#f0f2f1', surface:'#fbfcfb', soft:'#e8edeb', sel:'#d8eeea', ink:'#1b2929', mute:'#526362', accent:'#006c67', onaccent:'#ffffff', warn:'#895900', danger:'#a92e37', ok:'#24653f' },
              dark:{ canvas:'#172326', surface:'#1e2d31', soft:'#2a3d40', sel:'#274c47', ink:'#ebf3f0', mute:'#b2c4bf', accent:'#88d8c6', onaccent:'#172326', warn:'#efc576', danger:'#ffaeb5', ok:'#92d1a5' } };
  const rows = [];
  for (const [th, t] of Object.entries(T)) for (const [fg, bg, min] of [['ink','surface',4.5],['mute','surface',4.5],['mute','soft',4.5],['ink','sel',4.5],['onaccent','accent',4.5],['accent','surface',4.5],['warn','surface',4.5],['danger','surface',4.5],['ok','surface',4.5],['mute','canvas',4.5]])
    rows.push({ th, pair:`${fg}/${bg}`, ratio:cr(t[fg], t[bg]), min, pass:cr(t[fg], t[bg]) >= min });
  return rows;
});
await browser.close();
const fail = contrast.filter(r => !r.pass);
fs.writeFileSync(path.join(out, 'browser-check.json'), JSON.stringify({ url:'docs/mockups/solver-setup.html', at:new Date().toISOString(), errors, findings, contrastFailures:fail, contrast, captures:shots }, null, 1));
console.log(`errors ${errors.length} · findings ${findings.length} · contrast failures ${fail.length} · captures ${shots.length}`);
for (const f of [...errors, ...findings]) console.log(' -', f);
process.exit(errors.length || findings.length || fail.length ? 1 : 0);
