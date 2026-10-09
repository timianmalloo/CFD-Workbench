import { chromium } from 'playwright';
const file = 'file:///Users/mallalieut/projects/CFD-Workbench-design-cmr-comb-revision/docs/mockups/rail-comb.html';
const shots = process.argv[2];
const b = await chromium.launch({ channel: 'chrome' });
const p = await b.newPage({ viewport: { width: 1400, height: 900 } });
const errs = [];
p.on('pageerror', e => errs.push(String(e.stack)));
p.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
await p.goto(file);
console.log('ERR0', JSON.stringify(errs));
console.log('AUDIT light', await p.textContent('#audit'));
await p.screenshot({ path: shots + '/light.png', fullPage: true });
await p.click('#theme');
console.log('AUDIT dark', await p.textContent('#audit'));
await p.screenshot({ path: shots + '/dark.png', fullPage: true });
await p.click('#theme');
for (const v of ['wobble', 'corner', 'g1', 'straight', 'example']) {
  await p.selectOption('#h-plan', v);
  console.log(v, '|', await p.textContent('#trace'), '|', (await p.textContent('.plate .pcs')).replace(/\s+/g, ' '), '|', (await p.textContent('.plate')).match(/Ignores[^.]*\./)?.[0]);
  console.log('  audit', (await p.textContent('#audit')).slice(0, 600));
}
// keyboard walk: focus the plan, press ] three times, read the strip
await p.focus('#stageSvg');
for (let i = 0; i < 4; i++) await p.keyboard.press(']');
console.log('WALK', await p.textContent('#trace'));
// focus ring colour on the viewport
const oc = await p.evaluate(() => getComputedStyle(document.getElementById('stageSvg')).outlineColor);
console.log('SVG focus outline', oc, 'focus-visible', await p.evaluate(() => document.getElementById('stageSvg').matches(':focus-visible')));
// stepper at its limit keeps focus
await p.click('[data-act="larger"]');
for (let i = 0; i < 12; i++) { await p.focus('[data-act="larger"]'); await p.keyboard.press('Enter'); }
console.log('AFTER limit focus =', await p.evaluate(() => document.activeElement.dataset.act), 'aria-disabled =', await p.getAttribute('[data-act="larger"]', 'aria-disabled'), 'live =', await p.evaluate(() => document.querySelector('.sr').textContent));
await p.click('[data-act="auto"]');
// tab order: svg -> plate buttons -> stand-in
await p.focus('#stageSvg');
const order = [];
for (let i = 0; i < 9; i++) { await p.keyboard.press('Tab'); order.push(await p.evaluate(() => document.activeElement.dataset.act || document.activeElement.id || document.activeElement.tagName)); }
console.log('TAB ORDER', order.join(' > '));
await p.screenshot({ path: shots + '/wide.png' });
await p.click('#h-narrow');
console.log('AUDIT narrow closed', await p.textContent('#audit'));
await p.screenshot({ path: shots + '/narrow.png', fullPage: false });
await p.click('#stage [data-act="open"]');
console.log('AUDIT narrow open', await p.textContent('#audit'));
await p.screenshot({ path: shots + '/narrow-open.png', fullPage: true });
await p.click('#theme');
console.log('AUDIT narrow open dark', await p.textContent('#audit'));
await p.screenshot({ path: shots + '/narrow-open-dark.png' });
console.log('ERRORS', JSON.stringify(errs));
await b.close();
