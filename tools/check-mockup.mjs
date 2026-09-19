#!/usr/bin/env node
// Verify the review artifact's behavior, not a native app, geometry kernel or solver.
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
import assert from 'node:assert/strict';

const repo=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const moduleRoot=process.argv[2];
const {chromium}=await import(moduleRoot?pathToFileURL(path.join(moduleRoot,'playwright','index.mjs')).href:'playwright');
const shots=await fs.mkdtemp(path.join(os.tmpdir(),'cfd-workbench-review-'));
const browser=await chromium.launch({headless:true,channel:'chrome'});
const page=await browser.newPage({viewport:{width:1600,height:1120}});
const errors=[],requests=[],measurements=[],oracles=[];
const started=performance.now();
page.on('pageerror',e=>errors.push(e.message));
page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url())});
const record=(name,proof)=>oracles.push({name,pass:true,proof});
const task=async name=>page.locator(`[data-task="${name}"]`).click();
const state=async name=>page.locator('#h-state').selectOption(name);
const close=async()=>{if(await page.locator('dialog').isVisible())await page.locator('#dialog-close').click()};
const measure=async(label)=>{
 const row=await page.evaluate(()=>({...window.workbenchAudit,overflow:document.documentElement.scrollWidth>innerWidth}));
 assert(row.frame.width>0&&row.frame.height>0,'Frame must render');
 assert.equal(row.contrastFailures,0,`${label}: token contrast`);
 assert.equal(row.smallTargets,0,`${label}: minimum target size`);
 assert.equal(row.overflow,false,`${label}: viewport overflow`);
 measurements.push({label,...row});
};
try{
 await page.goto(pathToFileURL(path.join(repo,'docs/mockups/workbench.html')).href);
 for(const name of ['shape','sections','analyze','simulate','results']){
  await task(name); await measure(name);
  await page.screenshot({path:path.join(shots,`${name}.png`),fullPage:true});
 }
 // A result must not silently acquire today's edited shape.
 await task('results');const initialRun=await page.locator('#foil-svg').innerHTML();
 await task('shape');await page.locator('#input-chord').fill('150');await page.locator('#input-chord').press('Tab');
 assert.equal(await page.locator('dialog').isVisible(),true);
 assert.match(await page.locator('dialog').innerText(),/The explicit model stays parametric/);
 await page.getByRole('button',{name:'Apply edit',exact:true}).click();
 assert.equal(await page.locator('#input-chord').inputValue(),'150.00');
 assert.equal(await page.evaluate(()=>document.activeElement.id),'input-chord');
 await task('results');assert.equal(await page.locator('#foil-svg').innerHTML(),initialRun);
 assert.match(await page.locator('.inline-banner').innerText(),/Results belong to revision 12/);
 record('Immutable run geometry after current-design edit','Snapshot SVG unchanged; historical banner visible.');
 await task('shape');await page.locator('#undo').click();
 assert.equal(await page.locator('#input-chord').inputValue(),'142.00');assert.match(await page.locator('#task-meta').innerText(),/Recipe linked/);
 record('Whole-operation undo','Chord142→150→142; recipe linked→direct→linked.');
 // Replacing a workspace must preserve the actual keyboard target.
 await page.locator('[data-channel="twist"]').focus();await page.keyboard.press('Enter');
 assert.equal(await page.evaluate(()=>document.activeElement.dataset.channel),'twist');
 await page.locator('[data-point="2"]').focus();await page.keyboard.press('ArrowUp');
 await page.getByRole('button',{name:'Apply edit',exact:true}).click();
 assert.equal(await page.evaluate(()=>document.activeElement.dataset.point),'2');
 await page.locator('#tangent-rise').fill('-0.20');await page.locator('#tangent-rise').press('Tab');
 assert.equal(await page.locator('#tangent-rise').inputValue(),'-0.20');
 record('Keyboard identity and tangent editing','Channel activation, curve nudge and exact tangent input retain focus and accepted value.');
 // Geometry must fit, and scientific legends may not cover it.
 for(const name of ['shape','results']){
  await task(name);
  const bounds=await page.locator('#foil-svg').evaluate(svg=>{const g=svg.querySelectorAll(':scope > g')[1],b=g.getBBox(),v=svg.viewBox.baseVal;return {x:b.x,y:b.y,right:b.x+b.width,bottom:b.y+b.height,width:v.width,height:v.height}});
  assert(bounds.x>=0&&bounds.y>=0&&bounds.right<=bounds.width&&bounds.bottom<=bounds.height,JSON.stringify(bounds));
  record(`${name} full model in frame`,bounds);
 }
 await task('analyze');assert.equal(await page.locator('.data-plot g[stroke]').first().evaluate(el=>getComputedStyle(el).fill),'none');
 await task('shape');assert.equal(await page.locator('.curve-plot g[stroke]').first().evaluate(el=>getComputedStyle(el).fill),'none');
 record('Axes cannot fill a black triangle','Both plot axis groups compute fill:none.');
 await task('results');await state('partial');
 assert(await page.locator('#foil-svg path[stroke-dasharray="3 2"]').count()>0);record('Missing pressure is visibly masked','Partial field has dashed, uncolored missing tiles.');
 await task('simulate');await state('error');const simError=await page.locator('.inline-banner').innerText();assert.match(simError,/CFD-MESH-04/);assert.doesNotMatch(simError,/Chord/);
 record('Route-specific failure','Simulation failure names failed mesh stage and retained data.');
 await state('default');await task('shape');await page.locator('#h-viewport').selectOption('zoom');
 await page.locator('[data-action="project-menu"]').click();assert.match(await page.locator('dialog').innerText(),/Starter recipe/);await close();
 await measure('narrow');await page.screenshot({path:path.join(shots,'narrow.png'),fullPage:true});
 record('Narrow project access','Project action exposes recipe/history while pane is collapsed.');
 await page.locator('#h-viewport').selectOption('wide');
 await page.locator('[data-action="symmetry"]').click();await page.getByRole('button',{name:'Break symmetry',exact:true}).click();assert.match(await page.locator('[data-action="symmetry"]').innerText(),/broken/);await page.locator('#undo').click();assert.match(await page.locator('[data-action="symmetry"]').innerText(),/locked/);
 record('Symmetry break and undo','Independent port state is an undoable operation.');
 await task('sections');await page.getByRole('button',{name:'Make editable copy',exact:true}).click();await page.getByRole('button',{name:'Create copy',exact:true}).click();assert.equal(await page.locator('#profile-camber').isVisible(),true);await page.locator('#profile-camber').fill('1.25');await page.locator('#profile-camber').press('Tab');assert.equal(await page.locator('#profile-camber').inputValue(),'1.25');
 await page.getByRole('button',{name:'Import .dat',exact:true}).click();assert.match(await page.locator('dialog').innerText(),/Selig or Lednicer/);await close();
  record('Editable profile and profile-only import review','Camber fixture edits; bounded format/normalization/error contract visible.');
 // Derived dimensions use the same curve evaluator, including tangent edits.
 await task('shape');await page.locator('[data-channel="chord"]').click();
 const areaBefore=await page.locator('.side-footer .row').nth(1).innerText();
 const foilBeforeTangent=await page.locator('#foil-svg').innerHTML();
 await page.locator('#tangent-rise').fill('8.00');await page.locator('#tangent-rise').press('Tab');
 assert.notEqual(await page.locator('.side-footer .row').nth(1).innerText(),areaBefore);
 assert.notEqual(await page.locator('#foil-svg').innerHTML(),foilBeforeTangent);
 record('Area follows connecting curve','Changing a chord tangent updates shape and reference area/AR through the same evaluated curve.');
 // First profile edit is reviewed, updates the loft, and undoes as a whole.
 await page.reload();const shapeBeforeProfile=await page.locator('#foil-svg').innerHTML();
 await task('sections');await page.getByRole('button',{name:'Make editable copy',exact:true}).click();await page.getByRole('button',{name:'Create copy',exact:true}).click();
 await page.locator('#profile-camber').fill('1.25');await page.locator('#profile-camber').press('Tab');
 assert.equal(await page.locator('dialog').isVisible(),true);assert.match(await page.locator('#task-meta').innerText(),/Recipe linked/);
 await page.getByRole('button',{name:'Apply profile edit',exact:true}).click();assert.match(await page.locator('#task-meta').innerText(),/Direct parametric/);
 await task('shape');assert.notEqual(await page.locator('#foil-svg').innerHTML(),shapeBeforeProfile);
 await page.locator('#undo').click();assert.equal(await page.locator('#foil-svg').innerHTML(),shapeBeforeProfile);assert.match(await page.locator('#task-meta').innerText(),/Recipe linked/);
 record('Profile and loft share accepted revision','First camber edit requires detachment review, changes the 3D foil, and Undo restores shape plus recipe.');
 // Full state union: task × theme × hard state, and absence/reviewer/motion dimensions.
 for(const name of ['shape','sections','analyze','simulate','results']){
  await task(name);
  for(const theme of ['light','dark','contrast']){
   await page.locator('#h-theme').selectOption(theme);
   for(const mode of ['default','empty','loading','error','partial','stale','overflow','success','unsupported']){
    await state(mode);await measure(`${name}/${theme}/${mode}`);
   }
  }
 }
 await state('default');await page.locator('#h-theme').selectOption('light');await page.locator('#h-capability').selectOption('unavailable');await task('simulate');assert.equal(await page.getByRole('button',{name:'Preview case preparation',exact:true}).isDisabled(),true);await measure('dependency-unavailable');
 await page.locator('#h-persona').selectOption('reviewer');await task('shape');assert.equal(await page.locator('#input-chord').getAttribute('readonly'),'');await page.locator('#h-motion').check();assert.equal(await page.evaluate(()=>document.documentElement.dataset.motion),'reduced');await measure('reviewer/reduced');
 await page.locator('#h-persona').selectOption('designer');await page.locator('#h-capability').selectOption('available');await page.locator('#h-density').selectOption('comfortable');await measure('comfortable');
 assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
 record('Self-contained runtime','No page exceptions and no HTTP(S) requests.');
 const report={status:'pass',duration_seconds:Number(((performance.now()-started)/1000).toFixed(3)),scope:'HTML prototype only; not native accessibility or scientific validity',browser:await browser.version(),screenshot_directory:shots,errors,external_requests:requests,oracles,measurements};
 await fs.mkdir(path.join(repo,'docs/proof'),{recursive:true});await fs.writeFile(path.join(repo,'docs/proof/workbench-browser-check.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({status:'pass',measurements:measurements.length,oracles:oracles.length,screenshots:shots,duration_seconds:report.duration_seconds}));
}finally{await browser.close()}
