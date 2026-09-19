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
 // Requested interaction contract: weighted curves and coherent operating-point evidence.
 assert.equal(await page.locator('#outline-mode').count(),1,'Outline editing must expose weighted Smooth mode');
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
 // Smooth means weighted influence; preview and acceptance are distinct states.
 await page.reload();const outlineOriginal=await page.locator('#foil-svg').innerHTML();
 const outlineRevision=await page.locator('#task-meta').innerText();
 await page.locator('#outline-mode').selectOption('smooth');
 const smoothShape=await page.locator('#foil-svg').innerHTML();assert.notEqual(smoothShape,outlineOriginal);
 assert.equal(await page.locator('#task-meta').innerText(),outlineRevision);
 await page.locator('#outline-weight').fill('4');await page.locator('#outline-weight').press('Tab');
 assert.notEqual(await page.locator('#foil-svg').innerHTML(),smoothShape);
 assert.match(await page.locator('.outline-controls + .field-help').innerText(),/Evaluated station/);
 await page.getByRole('button',{name:'Cancel preview',exact:true}).click();assert.equal(await page.locator('#foil-svg').innerHTML(),outlineOriginal);
 await page.locator('#outline-mode').selectOption('smooth');await page.getByRole('button',{name:'Apply smooth preview',exact:true}).click();
 assert.notEqual(await page.locator('#task-meta').innerText(),outlineRevision);
 await page.locator('#undo').click();assert.equal(await page.locator('#foil-svg').innerHTML(),outlineOriginal);
 await page.locator('#outline-mode').selectOption('smooth');await page.locator('[data-channel="twist"]').click();assert.equal(await page.locator('#outline-mode').inputValue(),'through');await page.locator('[data-channel="chord"]').click();assert.equal(await page.locator('#foil-svg').innerHTML(),outlineOriginal);
 record('Weighted outline preview is reversible','Mode and weight change the shared curve/foil without accepting a revision; Cancel and accepted-operation Undo restore the original.');
 await task('sections');await page.getByRole('button',{name:'Edit section',exact:true}).click();await page.getByRole('button',{name:'Create copy',exact:true}).click();
 const sourceSection=await page.locator('#section-shape').getAttribute('d');
 await page.locator('#profile-offset').fill('2');await page.locator('#profile-offset').press('Tab');
 assert.notEqual(await page.locator('#section-shape').getAttribute('d'),sourceSection);
 await page.locator('#profile-mode').selectOption('smooth');const weightedSection=await page.locator('#section-shape').getAttribute('d');
 await page.locator('#profile-weight').fill('4');await page.locator('#profile-weight').press('Tab');assert.notEqual(await page.locator('#section-shape').getAttribute('d'),weightedSection);
 await page.getByRole('button',{name:'Cancel preview',exact:true}).click();assert.equal(await page.locator('#section-shape').getAttribute('d'),sourceSection);
 await page.locator('#profile-offset').fill('1.5');await page.locator('#profile-offset').press('Tab');await page.locator('[data-station="1"]').first().click();await page.locator('[data-station="2"]').first().click();assert.equal(await page.locator('#section-shape').getAttribute('d'),sourceSection);
 await page.locator('[data-profile-point="2"]').focus();await page.keyboard.press('ArrowUp');assert.equal(await page.evaluate(()=>document.activeElement.dataset.profilePoint),'2');
 await page.getByRole('button',{name:'Apply curve preview',exact:true}).click();await task('shape');assert.notEqual(await page.locator('#foil-svg').innerHTML(),outlineOriginal);
 await page.locator('#undo').click();assert.equal(await page.locator('#foil-svg').innerHTML(),outlineOriginal);
 record('Catalog section curve editing','Upper/lower control offsets and Smooth weights alter the actual section path; cancel, keyboard target, accepted loft linkage and whole-operation Undo are verified.');
 await task('analyze');const force=()=>page.locator('#evidence-metrics').getAttribute('data-lift-newtons');
 const fresh=Number(await force());await page.locator('#condition-water').selectOption('salt');const salt=Number(await force());assert(Math.abs(salt/fresh-1024.8103/998.2072)<1e-10);
 await page.locator('#condition-speed').fill('12');await page.locator('#condition-speed').press('Tab');assert(Math.abs(Number(await force())/salt-4)<1e-10);
 const coefficient=await page.locator('.metric strong').first().innerText(),newtons=Number(await force());await page.locator('#force-unit').selectOption('lbf');
 assert.equal(await force(),String(newtons));assert.equal(await page.locator('.metric strong').first().innerText(),coefficient);
 assert.equal(await page.locator('.metric strong').nth(3).innerText(),(newtons/4.4482216152605).toFixed(2));
 await page.locator('[data-scope="section"]').click();assert.equal(await page.locator('.metric strong').nth(3).innerText(),'Unavailable');
 record('Water, speed and force dimensional integrity','Fresh→salt scales fixture forces by pinned density; doubling velocity gives four times force; lbf changes display only, and section coefficients do not become wing total forces.');
 await task('simulate');await page.locator('#setup-water').selectOption('salt');await page.getByRole('button',{name:'Preview sweep results',exact:true}).click();
 assert.equal(await page.locator('[data-case-row]').count(),12);assert.match(await page.locator('.inspector').innerText(),/Salt water/);
 await page.locator('[data-case="5"]').click();const caseField=await page.locator('#foil-svg').innerHTML(),caseId=await page.locator('#evidence-metrics').getAttribute('data-case');assert.equal(caseId,'V6-A4');
 await page.locator('#result-field').selectOption('velocity');assert.notEqual(await page.locator('#foil-svg').innerHTML(),caseField);assert.match(await page.locator('.legend').innerText(),/m\/s/);
 await page.locator('[data-result-view="2d"]').click();assert.match(await page.locator('#foil-svg').getAttribute('aria-label'),/Span-normal section/);
 assert.equal(await page.locator('[data-streamline]').count(),10);await page.locator('#flow-lines').uncheck();assert.equal(await page.locator('[data-streamline]').count(),0);await page.locator('#flow-lines').check();
 await page.locator('#result-field').selectOption('turbulence');assert.match(await page.locator('.legend').innerText(),/Modeled turbulent kinetic energy/);
 await page.locator('#result-field').selectOption('pathlines');assert.match(await page.locator('dialog').innerText(),/physical time-resolved/);await close();
 await page.locator('#case-slider').fill('2');assert.equal(await page.locator('#evidence-metrics').getAttribute('data-case'),'V6-A8');assert.equal(await page.locator('.selected-row').getAttribute('data-case-row'),'6');
 await page.locator('#result-field').selectOption('wall-shear');assert.equal(await page.locator('#wall-reversal').count(),1);assert.match(await page.locator('.v-note').innerText(),/C𝒻 < 0/);
 const independentForce=await page.locator('#evidence-metrics').getAttribute('data-lift-newtons');assert.notEqual(independentForce,'');
 await state('partial');assert.equal(await page.locator('#wall-reversal').count(),0);assert.equal(await page.locator('.viewport-state h2').innerText(),'Field unavailable');assert.match(await page.locator('#foil-svg').getAttribute('aria-label'),/force sample retained/);assert.match(await page.locator('.viewport-state').innerText(),/Wall-shear field was not recorded/);assert.match(await page.locator('.viewport-state').innerText(),/independent force sample remains available/);assert.equal(await page.locator('#evidence-metrics').getAttribute('data-lift-newtons'),independentForce);await state('default');
 record('Signed separation criterion and absent wall data','Synthetic Cf<0 reversal is hatched and defined relative to +x freestream; partial state removes the overlay and explains absent wall evidence.');
 const lockedLegend=await page.locator('.legend').innerText();await page.locator('[data-case="4"]').click();assert.equal(await page.locator('.legend').innerText(),lockedLegend);
 await page.getByRole('button',{name:'Play sweep',exact:true}).click();await page.waitForFunction(()=>document.querySelector('#evidence-metrics').dataset.case==='V6-A4');await page.getByRole('button',{name:'Pause',exact:true}).click();assert.equal(await page.locator('#evidence-metrics').getAttribute('data-case'),'V6-A4');
 await page.locator('[data-case="6"]').click();await page.getByRole('button',{name:'Next operating point',exact:true}).click();assert.equal(await page.locator('[data-streamline]').count(),0);assert.match(await page.locator('.viewport-state').innerText(),/Sample unavailable/);assert.equal(await page.locator('#evidence-metrics').getAttribute('data-lift-newtons'),'');
 record('Sweep result linkage and evidence gaps','12 Cartesian samples; case selection synchronizes scalar SVG, metrics and row; distinct 2D view, seed visibility, fixed legend, play/pause, modeled k and pathline unavailability verified; failed sample clears fields and quantities.');
 await page.locator('[data-case="5"]').click();const pinnedField=await page.locator('#foil-svg').innerHTML();await task('simulate');await page.locator('#setup-water').selectOption('fresh');await task('results');assert.equal(await page.locator('#foil-svg').innerHTML(),pinnedField);assert.match(await page.locator('.inspector').innerText(),/Salt water/);
 await task('simulate');await page.locator('#sweep-speeds').fill('0, 6');await page.locator('#sweep-speeds').press('Tab');await page.getByRole('button',{name:'Preview sweep results',exact:true}).click();assert.match(await page.locator('[role="alert"]').innerText(),/velocity >0/);
 record('Sweep snapshot and invalid schedule','Editing setup fluid does not rewrite the prior run; zero velocity prevents creating a new sweep.');
 await task('shape');await page.locator('#input-chord').fill('150');await page.locator('#input-chord').press('Tab');await page.getByRole('button',{name:'Apply edit',exact:true}).click();
 await task('simulate');await page.locator('#sweep-speeds').fill('4, 6');await page.locator('#sweep-speeds').press('Tab');await page.getByRole('button',{name:'Preview sweep results',exact:true}).click();assert.match(await page.locator('.inspector').innerText(),/Snapshot r13/);
 assert.match(await page.locator('.sidebar [data-action="results"]').innerText(),/Revision 13/);assert.equal(await page.locator('.sidebar [data-action="results"] .station-number').innerText(),'13');
 await page.getByRole('button',{name:'Inspect provenance →',exact:true}).click();assert.match(await page.locator('dialog').innerText(),/Revision 13/);await close();
 await task('shape');await page.locator('#input-chord').fill('152');await page.locator('#input-chord').press('Tab');await task('results');assert.match(await page.locator('.inline-banner').innerText(),/Results belong to revision 13/);
 record('New run revision provenance','A sweep built from accepted r13 identifies r13 in inspector, provenance and stale banner after a later r14 edit.');
 await page.reload();
 await task('results');const resultBounds=await page.locator('.result-evidence').evaluate(el=>({height:el.getBoundingClientRect().height,scroll:el.scrollHeight,overflow:getComputedStyle(el).overflowY}));assert(resultBounds.height<=340&&resultBounds.scroll>resultBounds.height);assert.equal(resultBounds.overflow,'auto');
 record('Bounded result evidence pane','Metrics remain first in a keyboard-focusable scroll region; plots and full sample table remain reachable beneath the dominant view and replay.');
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
