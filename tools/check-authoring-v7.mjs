#!/usr/bin/env node
// Browser proof for the v7 authoring contract; does not certify the production evaluator.
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath,pathToFileURL} from 'node:url';
const repo=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const deps=process.argv[2];
const target=process.argv[3]||'docs/mockups/workbench-v7.html';
const {chromium}=await import(deps?pathToFileURL(path.join(deps,'playwright/index.mjs')).href:'playwright');
const browser=await chromium.launch({headless:true,channel:'chrome'});
const page=await browser.newPage({viewport:{width:1700,height:1200},acceptDownloads:true});
page.setDefaultTimeout(7000);
const checks=[],errors=[],requests=[],measurements=[];
page.on('pageerror',e=>errors.push(e.message));
page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});
const reload=()=>page.goto(pathToFileURL(path.join(repo,target)).href);
const record=(name,proof)=>checks.push({name,pass:true,proof});
const assertShell=row=>{for(const key of ['windowScroll','toolbarOverflow','statusOverflow','paramrowOverflow','doctabsOverflow'])assert.equal(row.shell[key],false,`${row.surface||'entry'} ${row.preset} ${key}`);assert(row.shell.toolbarRows<=44);assert(row.shell.docksScrollInternally);};
const snapshot=()=>page.evaluate(()=>({source:DSL.accepted,meaning:DSL.semantic,revision:M.revision,
  record:drecord(),profiles:JSON.parse(JSON.stringify(UX.profiles)),assignments:JSON.parse(JSON.stringify(UX.assignments)),
  samples:[0,.15,.3,.45,.6,.8,1].map(u=>sectionPoints3D(u,30))}));
const coherent=async()=>assert(await page.evaluate(()=>dmeaning(dparse(DSL.accepted))===dmeaning(drecord())),'accepted source and model disagree');
const selectStation=async eta=>{if(!await page.locator('#quads').isVisible())await page.locator('[data-doc="doc"]').click();const pick=page.locator(`#quads [data-st3d="${eta}"]`).first();if(eta===1){await pick.focus();await page.keyboard.press('Space');}else await pick.click();};
const editSection=async(scope='fork',policy='keep')=>{
  await page.locator('#ux-section-open').click();
  await page.locator('#ux-profile-scope').selectOption(scope);
  await page.locator('#ux-thickness-policy').selectOption(policy);
  await page.locator('#ux-section-start').click();
  await page.locator('#section-canvas [data-sec="upper:3"]').focus();
  await page.keyboard.press('ArrowUp');
};
try {
 await reload();
 assert.equal(await page.locator('#ux-section-open').count(),1,'persistent section editing entry is missing');
 assert(await page.locator('#ux-section-open').isVisible());
 record('CAD09_SectionEntry_PersistentAndLabelled','An on-canvas section summary exposes Edit section without a menu.');
 const initial=await snapshot();
 await selectStation(.6);await editSection('fork','keep');
 await page.evaluate(()=>renderStatus());assert(!(await page.locator('#status').innerText()).includes('No draft'),'direct status refresh lost section draft ownership');
 assert.equal((await snapshot()).source,initial.source,'preview mutated accepted source');
  await page.locator('#ux-section-apply').click();const fork=await snapshot();
 assert.equal(fork.profiles.length,initial.profiles.length+1);
 assert.deepEqual(fork.record.cv.tc,initial.record.cv.tc);
 for(const assignment of initial.assignments){const after=fork.assignments.find(a=>a.eta===assignment.eta);assert(after);if(assignment.eta===.6)assert.notEqual(after.profile,assignment.profile);else assert.equal(after.profile,assignment.profile);}
 assert.deepEqual(fork.samples[0],initial.samples[0]);assert.deepEqual(fork.samples.at(-1),initial.samples.at(-1));
 assert.notDeepEqual(fork.samples[2],initial.samples[2]);assert.notDeepEqual(fork.samples[5],initial.samples[5]);
 await coherent();record('CAD10_Fork_ProfileAssignmentAndAdjacentBlend_KeepThickness',{profiles:fork.profiles.length,changedAssignment:.6,unchangedEndpoints:[0,1]});
 const thicknessProof=await page.evaluate(()=>[0,.3,.6,.8,1].map(u=>{const n=200,P=sectionPoints3D(u,n),c=chanAt('chord',u),phi=chanAt('twist',u)*Math.PI/180,ct=Math.cos(phi),st=Math.sin(phi);let full=0;for(let j=1;j<=n;j++){const a=P[n-j],b=P[n+j];full=Math.max(full,((a[1]-b[1])*st+(a[2]-b[2])*ct)/c);}return {eta:u,measuredFull:full,channel:chanAt('tc',u)};}));
 for(const item of thicknessProof)assert(Math.abs(item.measuredFull/item.channel-1)<.003,'placed full t/c differs from its channel; possible half/full confusion');record('FullThickness_PlacedSections_MatchEffectiveChannel',thicknessProof);
 // Selection must never replace the foil's profile bank with the active editor profile.
 for(const eta of [0,.6,1]){await selectStation(eta);assert.deepEqual((await snapshot()).samples,fork.samples);}
 await page.locator('[data-doc="source"]').click();assert.deepEqual((await snapshot()).samples,fork.samples);
 await page.locator('#dsl-geometry').click();record('Selection_ProfileReaders_NoGeometryMutation','Selecting all authored stations and Source preserves evaluated sections exactly.');
 await page.evaluate(()=>undo());assert.deepEqual((await snapshot()).profiles,initial.profiles);assert.deepEqual((await snapshot()).assignments,initial.assignments);
 await page.evaluate(()=>redo());assert.deepEqual((await snapshot()).profiles,fork.profiles);assert.deepEqual((await snapshot()).assignments,fork.assignments);await coherent();
 record('CAD10_ProfileBank_UndoRedo_Roundtrip','Bank, assignments and source restore together.');
 await reload();await selectStation(.6);const sharedBefore=await snapshot();await editSection('shared','keep');await page.locator('#ux-section-apply').click();const sharedAfter=await snapshot();
 assert.equal(sharedAfter.profiles.length,sharedBefore.profiles.length);assert.deepEqual(sharedAfter.assignments,sharedBefore.assignments);assert.deepEqual(sharedAfter.record.cv.tc,sharedBefore.record.cv.tc);
 for(let i=0;i<sharedAfter.samples.length;i++)assert.notDeepEqual(sharedAfter.samples[i],sharedBefore.samples[i]);await coherent();record('CAD10_SharedEdit_AllAssignments_NoThicknessRewrite','Shared-profile edits reach root, middle, tip and their blending intervals.');
 await reload();await selectStation(.6);const sourceThicknessBefore=await snapshot();await editSection('fork','source');
 assert.match(await page.locator('#ux-scope-impact').innerText(),/thickness|t\/c/i);
 await page.locator('#ux-section-apply').click();const sourceThicknessAfter=await snapshot();
 assert.notDeepEqual(sourceThicknessAfter.record.cv.tc,sourceThicknessBefore.record.cv.tc,'Use source thickness failed to author the t/c channel');
 await coherent();record('CAD10_SourceThickness_ExplicitChannelTransaction','Selected profile and t/c changes accept together and reparse coherently.');
 await reload();await selectStation(.6);const lockedThicknessBefore=await snapshot();await editSection('shared','source');
 assert(await page.locator('#ux-section-apply').isDisabled());assert.equal((await snapshot()).source,lockedThicknessBefore.source);assert.deepEqual((await snapshot()).record,lockedThicknessBefore.record);
 assert.match(await page.locator('#ux-scope-impact').innerText(),/lock|conflict|Refused|Cannot apply/i);await page.locator('#ux-section-cancel').click();
 record('CAD10_SourceThickness_ConflictingTipLock_Refuses','Shared source-thickness targets cannot silently change a held tip endpoint.');
 await reload();
 const mixedSource=await page.evaluate(()=>{const r=drecord(),p=dclone(r.profiles[0]);p.id='different-abscissae';for(const side of ['upper','lower'])p.section[side]=p.section[side].map(([x,y])=>[Math.pow(x,1.6),y]);r.profiles.push(p);r.assignments.at(-1).profile=p.id;return demit(r);});
 await page.locator('[data-doc="source"]').click();await page.locator('#dsl-source').fill(mixedSource);await page.locator('#dsl-validate').click();await page.locator('#dsl-apply').click();
 const blendProof=await page.evaluate(()=>{
  const r=drecord(),A=r.profiles[0],B=r.profiles[1],eta=.8,weight=.5,n=100;
  const at=(p,side,x)=>{let lo=0,hi=1;for(let i=0;i<40;i++){const m=(lo+hi)/2;if(BS.evalCurve(p.section[side],5,p.knots[side],m)[0]<x)lo=m;else hi=m;}return BS.evalCurve(p.section[side],5,p.knots[side],(lo+hi)/2)[1];};
  const shape=(p,x)=>{const upper=at(p,'upper',x),lower=at(p,'lower',x);return {c:(upper+lower)/2,t:upper-lower};};
  const grid=Array.from({length:201},(_,i)=>i/200),ta=Math.max(...grid.map(x=>shape(A,x).t)),tb=Math.max(...grid.map(x=>shape(B,x).t));
  const blend=x=>{const a=shape(A,x),b=shape(B,x);return {c:a.c*(1-weight)+b.c*weight,t:a.t/ta*(1-weight)+b.t/tb*weight};};
  const normalizer=Math.max(...grid.map(x=>blend(x).t)),points=sectionPoints3D(eta,n),chord=chanAt('chord',eta),phi=chanAt('twist',eta)*Math.PI/180,ct=Math.cos(phi),st=Math.sin(phi),le=chanAt('le',eta),z0=chanAt('elev',eta),tc=chanAt('tc',eta);
  // Independent inverse-x probes include the sharp leading edge; only the max-thickness normalization remains sampled.
  let maxError=0;for(let j=1;j<n;j++){const x=.5*(1-Math.cos(Math.PI*j/n)),expected=blend(x);for(const [index,sign]of [[n-j,1],[n+j,-1]]){const p=points[index],z=((p[1]-le)*st+(p[2]-z0)*ct)/chord;maxError=Math.max(maxError,Math.abs(z-(expected.c+sign*tc*expected.t/normalizer/2)));}}
  return {eta,weight,profiles:r.profiles.length,normalizedChordProbes:99,maxError};
 });assert.equal(blendProof.profiles,2);assert(blendProof.maxError<1e-7,'blend does not use equal normalized x on independently parameterized profiles: '+JSON.stringify(blendProof));record('RuleA_DifferentProfileAbscissae_EqualXBlend',blendProof);
 await reload();
 const mixedCountSource=await page.evaluate(()=>{const r=drecord(),p={id:'six-control-section',section:{upper:[[0,0],[0,.04],[.3,.08],[.7,.05],[1,.01],[1,0]],lower:[[0,0],[0,-.03],[.3,-.04],[.7,-.025],[1,-.005],[1,0]]},knots:{upper:BS.clampedKnots(6,5),lower:BS.clampedKnots(6,5)}};r.profiles.push(p);r.assignments.at(-1).profile=p.id;return demit(r);});
 await page.locator('[data-doc="source"]').click();await page.locator('#dsl-source').fill(mixedCountSource);await page.locator('#dsl-validate').click();await page.locator('#dsl-apply').click();await page.locator('#dsl-geometry').click();
 const mixedCountBefore=await snapshot();await selectStation(.6);await page.locator('#ux-section-open').click();await page.locator('#section-canvas [data-sec="upper:7"]').click();
 await selectStation(1);await page.locator('#ux-section-open').click();assert.equal(await page.evaluate(()=>CVSEC.n),6);assert.equal(await page.locator('#section-canvas [data-sec]').count(),12);assert.match(await page.locator('#section-hud').innerText(),/6 per side/);
 assert(await page.evaluate(()=>{const [s,i]=M.secSel.split(':');return !!secP(s)[Number(i)];}),'remembered selection references nonexistent control vertex');
 await selectStation(.6);await page.locator('#ux-section-open').click();assert.equal(await page.evaluate(()=>CVSEC.n),12);assert.equal(await page.locator('#section-canvas [data-sec]').count(),24);assert.deepEqual((await snapshot()).record,mixedCountBefore.record);assert.deepEqual((await snapshot()).samples,mixedCountBefore.samples);await coherent();
 record('ProfileBank_DifferentControlCounts_InspectionAndSelection','Switching between 12- and 6-CV profiles refreshes basis and safely rebinds an out-of-range remembered selection without changing source or geometry.');
 // A draft owns its original target even while other station/curve details are inspected.
 await reload();await selectStation(.6);await editSection('fork','keep');
 const draftBefore=await page.evaluate(()=>({source:DSL.accepted,section:JSON.stringify(M.sectionPreview),station:JSON.stringify(M.stationDoc)}));
 await page.locator('[data-doc="doc"]').click();await selectStation(0);
 assert.equal(await page.evaluate(()=>DSL.accepted),draftBefore.source);assert.equal(await page.evaluate(()=>JSON.stringify(M.sectionPreview)),draftBefore.section);
 assert.match(await page.locator('#ux-owner').innerText(),/0\.6|60\s*%/);
 assert.match(await page.locator('#ux-section-open').innerText(),/Inspect section/);await page.locator('#ux-section-open').click();assert.equal(await page.evaluate(()=>JSON.stringify(M.sectionPreview)),draftBefore.section);assert(await page.locator('#ux-section-start').isDisabled(),'inspected station offered a competing edit');
 const inspectedProfile=await page.evaluate(()=>UX.assignments.find(a=>a.eta===0).profile);assert((await page.locator('#section-hud').innerText()).startsWith(inspectedProfile+' ·'),'inspected geometry carries the draft owner profile label');
 await page.locator('[data-doc="source"]').click();assert(await page.locator('#dsl-source').isDisabled()||await page.locator('#dsl-source').getAttribute('readonly')!==null);
 await page.locator('#dsl-geometry').click();await page.locator('#ux-return-draft').click();assert.equal(await page.evaluate(()=>M.stationDoc.eta),.6);
 await page.locator('#ux-alternatives-open').click();await page.locator('#ux-alt-name').fill('Do not apply a section');await page.locator('#ux-alt-name').press('Enter');
 assert.equal(await page.evaluate(()=>DSL.accepted),draftBefore.source);assert(await page.evaluate(()=>!!UX.sectionDraft));assert.equal(await page.evaluate(()=>UX.alternatives.length),0);
 await page.locator('#ux-alternatives-dialog [data-ux-close]').focus();await page.keyboard.press('Enter');assert.equal(await page.evaluate(()=>DSL.accepted),draftBefore.source);
 await page.locator('#ux-section-cancel').focus();await page.keyboard.press('Enter');
 assert.equal(await page.evaluate(()=>DSL.accepted),draftBefore.source);assert.equal(await page.evaluate(()=>!!M.sectionPreview),false);
 await page.evaluate(()=>renderStatus());assert((await page.locator('#status').innerText()).includes('No draft'),'Cancel retained a stale draft status');
 record('CAD11_InspectOtherStationAndSource_DraftOwnerStable','Read-only inspection preserves draft values and target; Cancel leaves accepted source unchanged.');
 record('NativeControls_EnterCancelsOrTypes_NeverAppliesUnrelatedDraft','Enter in an alternative-name field or Close button cannot apply the section draft; Enter on Cancel cancels with accepted source unchanged.');
 // Each chord edit names its held rail; all source/model readouts must follow one accepted transaction.
 for(const held of ['le','te']){
  await reload();await selectStation(.6);const before=await snapshot();const targetChord=await page.evaluate(()=>chanAt('chord',.6)+.003);
  await page.locator('#ux-intent-open').click();await page.locator('#ux-chord-hold').selectOption(held);await page.locator('#ux-chord-value').fill(String(targetChord));await page.locator('#ux-chord-preview').click();
  assert.equal((await snapshot()).source,before.source);await page.locator('#ux-intent-apply').click();const after=await snapshot();
  assert.deepEqual(after.record.cv[held],before.record.cv[held]);assert(Math.abs(await page.evaluate(()=>chanAt('chord',.6))-targetChord)<1e-7);await coherent();
  record(`CAD13_ChordIntent_Hold_${held}`,{targetChord,held});
 }
 for(const mode of ['relative','absolute']){
  await reload();const before=await snapshot();await page.locator('#ux-intent-open').click();await page.locator('#ux-span-mode').selectOption(mode);await page.locator('#ux-span-value').fill('1.4');await page.locator('#ux-span-preview').click();await page.locator('#ux-intent-apply').click();const after=await snapshot();
  assert.equal(after.record.half,.7);assert.deepEqual(after.record.cv,before.record.cv);
  for(let i=0;i<before.assignments.length;i++){const a=before.assignments[i],z=after.assignments[i];assert.equal(z.profile,a.profile);const expected=a.eta===0||a.eta===1?a.eta:mode==='relative'?a.eta:a.eta*before.record.half/.7;assert(Math.abs(z.eta-expected)<1e-14);}
  await coherent();record(`CAD13_SpanIntent_${mode}`,after.assignments);
 }
 await reload();const beforeShrink=await snapshot();await page.locator('#ux-intent-open').click();await page.locator('#ux-span-mode').selectOption('absolute');await page.locator('#ux-span-value').fill('.5');await page.locator('#ux-span-preview').click();
 assert.deepEqual(await snapshot(),beforeShrink);assert(await page.locator('#ux-intent-apply').isDisabled());record('CAD13_AbsoluteStationBeyondNewTip_Refuses','No clamp, dropped assignment or source mutation.');
 await reload();const baseline=await snapshot();await page.locator('#ux-alternatives-open').click();
 await page.locator('#ux-alt-name').fill('Tip trial');await page.locator('#ux-alt-create').click();
 await page.keyboard.press('Escape');
 await page.locator('#quads .quad[data-slot="a"] [data-cv="te:3"]').focus();await page.keyboard.press('ArrowUp');await page.keyboard.press('Enter');
 const alternative=await snapshot();assert.notEqual(alternative.source,baseline.source);await coherent();
 await page.locator('#ux-alternatives-open').click();await page.locator('#ux-alt-compare').click();
 const comparison=await page.locator('dialog[open]').innerText();assert.match(comparison,/Tip trial/);assert.match(comparison,/Not run|Unavailable|not comparable|Incompatible/i);
 await page.locator('#ux-alt-keep').click();assert.equal(await page.evaluate(()=>UX.decisions.length),0);assert.equal(await page.evaluate(()=>UX.active),'Tip trial');assert.match(await page.locator('#ux-alt-message').innerText(),/rationale/i);
 await page.locator('#ux-alt-rationale').fill('Keep baseline pending comparable hydrodynamic evidence.');await page.locator('#ux-alt-discard').click();
 assert.deepEqual((await snapshot()).record,baseline.record);assert.equal((await snapshot()).source,baseline.source);
 const archived=await page.evaluate(()=>JSON.stringify(UX));assert(archived.includes('Tip trial'));assert(archived.includes('Keep baseline pending comparable hydrodynamic evidence.'));
 record('CAD12_AlternativeCompareDiscard_RationaleAndBaselineRetained','Changed shape has no fabricated scientific improvement; Discard restores the complete baseline and retains decision rationale.');
 await page.locator('#ux-alternatives-dialog summary').click();await page.locator('#ux-alt-archive').selectOption('0');await page.locator('#ux-alt-read').click();assert.equal(await page.locator('#ux-archive-source').inputValue(),alternative.source);assert(await page.locator('#ux-archive-source').getAttribute('readonly')!==null);assert.deepEqual((await snapshot()).record,baseline.record);
 await page.locator('#ux-baseline-read').click();assert.equal(await page.locator('#ux-archive-source').inputValue(),baseline.source);assert.deepEqual((await snapshot()).record,baseline.record);
 record('CAD12_ArchivedAlternativeAndBaseline_ReadOnlyInspection','Retained exact source is reachable; inspection never activates or mutates either branch.');
 await reload();await page.locator('#ux-alternatives-open').click();await page.locator('#ux-alt-name').fill('Kept option');await page.locator('#ux-alt-create').click();await page.keyboard.press('Escape');
 await page.locator('#quads .quad[data-slot="a"] [data-cv="te:3"]').focus();await page.keyboard.press('ArrowUp');await page.keyboard.press('Enter');const keptGeometry=await snapshot();
 await page.locator('#ux-alternatives-open').click();await page.locator('#ux-alt-rationale').fill('Retain this geometry for later evidence collection.');await page.locator('#ux-alt-keep').click();assert.deepEqual((await snapshot()).record,keptGeometry.record);assert.equal(await page.evaluate(()=>UX.decisions.length),1);
 record('CAD12_Keep_PreservesChosenAcceptedGeometry','Keep records rationale without claiming scientific superiority or rolling back the alternative.');
 await reload();await page.locator('#ux-alternatives-open').click();const hostile='<img src="https://invalid.example/x" onerror="window.injected=1">';
 await page.locator('#ux-alt-name').fill(hostile);await page.locator('#ux-alt-create').click();
 assert.equal(await page.evaluate(()=>window.injected),undefined);assert.equal(await page.locator('img[src="https://invalid.example/x"]').count(),0);
 record('UntrustedAlternativeName_NoMarkupExecution','Angle brackets and event syntax do not create an image or execute code.');
 await reload();await page.locator('[data-doc="source"]').click();
 const hostileSource=await page.evaluate(name=>DSL.accepted.replaceAll(JSON.stringify(UX.profiles[0].id),JSON.stringify(name)),hostile);
 await page.locator('#dsl-source').fill(hostileSource);await page.locator('#dsl-validate').click();await page.locator('#dsl-apply').click();await page.locator('#dsl-geometry').click();await page.locator('#ux-section-open').click();
 assert.equal(await page.evaluate(()=>window.injected),undefined);assert.equal(await page.locator('img[src="https://invalid.example/x"]').count(),0);await coherent();
 record('UntrustedProfileName_SourceToSection_NoMarkupExecution','A source-authored hostile name remains literal in the station card, section header and accepted source.');
 // Persistent section entry and readable ownership survive every established viewport/theme cell.
 await reload();
 for(const preset of ['minimum','desktop','lines','wide','zoom'])for(const theme of ['light','dark','contrast']){
  await page.locator('#h-window').selectOption(preset);await page.locator('#h-theme').selectOption(theme);
  await page.evaluate(()=>audit());assert(await page.locator('#ux-section-open').isVisible());
  const row=await page.evaluate(()=>({preset:root.dataset.window,theme:root.dataset.theme,...window.workbenchAudit}));
  assert.equal(row.contrastFailures,0);assert.equal(row.smallTargets,0);assert.equal(row.denseUnder,0);assertShell(row);measurements.push(row);
 }
 record('CAD09_15ViewportThemeCells_PersistentEntryAndAudit',{cells:measurements.length});
 await page.locator('#h-window').selectOption('desktop');await page.locator('#ux-section-open').click();
 for(const preset of ['minimum','desktop','zoom'])for(const theme of ['light','dark','contrast']){
  await page.locator('#h-window').selectOption(preset);await page.locator('#h-theme').selectOption(theme);await page.evaluate(()=>audit());
  const row=await page.evaluate(()=>({surface:'section',preset:root.dataset.window,theme:root.dataset.theme,...window.workbenchAudit}));
  assert.equal(row.contrastFailures,0);assert.equal(row.smallTargets,0);assert.equal(row.denseUnder,0);assertShell(row);measurements.push(row);
 }
 record('SectionScope_9LayoutThemeCells_Audit',{cells:9});
 await page.locator('#h-window').selectOption('desktop');await page.locator('#h-theme').selectOption('light');
 await page.locator('#ux-profile-scope').selectOption('fork');await page.locator('#ux-section-start').click();await page.locator('#section-canvas [data-sec="upper:3"]').focus();await page.keyboard.press('ArrowUp');
 for(const preset of ['minimum','desktop','zoom'])for(const theme of ['light','dark','contrast']){
  await page.locator('#h-window').selectOption(preset);await page.locator('#h-theme').selectOption(theme);await page.evaluate(()=>audit());
  const row=await page.evaluate(()=>({surface:'section-draft',preset:root.dataset.window,theme:root.dataset.theme,...window.workbenchAudit}));
  assert.equal(row.contrastFailures,0);assert.equal(row.smallTargets,0);assert.equal(row.denseUnder,0);assertShell(row);measurements.push(row);
 }
 record('SectionDraft_9LayoutThemeCells_AllShellFlags',{cells:9});
 await page.locator('#h-window').selectOption('desktop');await page.locator('#h-theme').selectOption('light');
 const screenshot=path.join(repo,'docs/proof/authoring-v7-reviewed.png');await page.screenshot({path:screenshot});
 assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
 const report={artifact:target,scope:'bounded authoring prototype, not scientific/native conformance',red:'v6 lacks persistent section editing entry: observed count0, expected1',checks,measurements,errors,requests,screenshot};
 await fs.writeFile(path.join(repo,'docs/proof/authoring-v7-browser-check.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({checks:checks.length,measurements:measurements.length,errors,requests}));
}catch(error){console.error(JSON.stringify(await page.evaluate(()=>({audit:window.workbenchAudit,state:document.querySelector('#h-verdict')?.textContent}))));throw error;}finally{await browser.close();}
