#!/usr/bin/env node
// Prototype contract proof: actual UI transactions plus bounded parser properties.
// Does not certify the full language, native archive, numerical kernel or CFD.
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import assert from 'node:assert/strict';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const deps=process.argv[2];
const {chromium}=await import(deps?pathToFileURL(path.join(deps,'playwright/index.mjs')).href:'playwright');
const browser=await chromium.launch({headless:true,channel:'chrome'});
const page=await browser.newPage({viewport:{width:1700,height:1200},acceptDownloads:true});
const errors=[],requests=[],checks=[],measurements=[];
page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push(r.url());});
const mockupName=process.env.MOCKUP_NAME||'workbench-v7';
const proofDir=process.env.PROOF_DIR?path.resolve(process.env.PROOF_DIR):path.join(root,'docs/proof');
const artifact=`docs/mockups/${mockupName}.html`;
const url=pathToFileURL(path.join(root,artifact)).href;
const record=(name,proof)=>checks.push({name,pass:true,proof});
const reload=async()=>{await page.goto(url);await page.locator('[data-doc="source"]').click();};
const accepted=()=>page.evaluate(()=>({source:DSL.accepted,meaning:DSL.semantic,rev:M.revision,current:M.run.current,run:M.run.key,record:drecord()}));
try{
  await reload();
  assert.equal(await page.locator('#dsl-source').count(),1);
  assert.equal(await page.evaluate(()=>dcheck()),true);
  record('SourceDocument_Opened_EditableAndValid',`One textarea exists and initial ${mockupName} source validates; historical RED was observed on v5.`);
  const initial=await accepted();
  if(mockupName==='workbench-v7'){
    assert.match(initial.source,/evaluator "cfdw-cv" "2"/);
    await page.locator('#dsl-source').fill(initial.source.replace('evaluator "cfdw-cv" "2"','evaluator "cfdw-cv" "1"'));
    await page.locator('#dsl-validate').click();
    assert(await page.locator('#dsl-apply').isDisabled());assert.deepEqual(await accepted(),initial);
    assert.match(await page.locator('#dsl-message').innerText(),/Evaluator unavailable/);
    await page.locator('#dsl-cancel').click();assert.equal(await page.locator('#dsl-source').inputValue(),initial.source);
    record('Ruling17_LegacyEvaluator_RefusesWithoutAdoption','Actual textarea Validate/Cancel preserves accepted source and history; emitter pins /2.');
  }
  const recipes=await page.evaluate(()=>{const saved=dsnapshot(),ex=M.ex;const results=Object.keys(EXAMPLES).map(id=>{loadExample(id);const r=drecord();return {id,valid:dmeaning(dparse(demit(r)))===dmeaning(r)};});M.ex=ex;drestore(saved);return results;});assert(recipes.every(r=>r.valid));record('RecipeSeeds_EveryGenerator_EmitsValidSource',recipes);
  // Lexer rejects malformed/oversize/unknown input; production-valid but unsupported input is explicit.
  const fixtureNames=['foil-basic.foil','foil-precision.foil','foil-comment.foil'];
  for(const name of fixtureNames){const text=await fs.readFile(path.join(root,'docs/examples/foildsl',name),'utf8');const result=await page.evaluate(text=>{const r=dparse(text),em=demit(r);return {same:dmeaning(r)===dmeaning(dparse(em)),idempotent:demit(dparse(em))===em};},text);assert(result.same&&result.idempotent,name);}
  const properties=await page.evaluate(()=>{let tested=0;for(let i=1;i<=64;i++){const r=drecord();r.half=.125+i/128;r.cv.te[3][1]=.04+i/1024;const out=demit(r);if(dmeaning(dparse(out))!==dmeaning(r)||demit(dparse(out))!==out)throw Error('round-trip failure '+i);tested++;}return tested;});
  record('Emit_FiniteVariantCorpus_RoundTripAndIdempotent',{examples:fixtureNames,generated:properties});
  const negatives=await page.evaluate(()=>{
    const s=DSL.accepted,cases=[s.slice(0,-4),s.replace('planform {','planform ?'),s.replace('half_span 0.55 m','half_span -1 m'),s.replace('"4.0"','"9.0"'),s.replace('units m','units inch'),s.replace('  tip open','  typo 1'),s+'trailing',s+'#'.repeat(262144),s.replace('profile "shared-section"','profile ""'),s.replace('at tip profile "shared-section"','at tip profile "missing"')];
    return cases.map(source=>{try{dparse(source);return null;}catch(e){return e.code;}});
  });assert(negatives.every(Boolean));record('Parse_InvalidBoundaryCorpus_RejectsAll',negatives);
  // Actual textarea and button interactions, no parser-only substitute for the state machine.
  for(const fixture of ['syntax','incomplete','geometry','version']){await page.locator('#dsl-fixture').selectOption(fixture);assert(await page.locator('#dsl-apply').isDisabled());assert.deepEqual(await accepted(),initial);assert.match(await page.locator('#dsl-summary').innerText(),/Accepted geometry/);await page.locator('#dsl-cancel').click();}
  record('Source_InvalidDraft_AcceptedShapeAndRunUnchanged','Four rendered recovery paths; Apply disabled; Cancel restores accepted source.');
  await page.locator('#dsl-source').fill(initial.source.replace('half_span 0.55 m','half_span 0.6 m'));
  await page.locator('#dsl-preview').click();assert.deepEqual(await accepted(),initial);assert.match(await page.locator('#dsl-summary').innerText(),/Preview candidate.*1200.0 mm/);
  await page.locator('#dsl-apply').click();const edited=await accepted();assert.equal(edited.record.half,.6);assert.equal(edited.rev,initial.rev+1);assert.equal(edited.current,false);assert.equal(edited.run,initial.run);
  const span=await page.evaluate(()=>({readout:derived().b,point:stationXform(1)([0,0])[0]}));assert.equal(span.readout,1.2);assert.equal(span.point,.6);
  await page.locator('#dsl-undo').click();assert.equal((await accepted()).source,initial.source);assert.equal((await accepted()).meaning,initial.meaning);assert.equal((await accepted()).current,true);
  await page.locator('#dsl-redo').click();assert.equal((await accepted()).meaning,edited.meaning);
  record('Source_GeometricApplyUndoRedo_AllReadersAndFreshnessAgree',span);
  const beforeComment=await accepted();await page.locator('#dsl-source').fill('# retained comment\n'+beforeComment.source);await page.locator('#dsl-validate').click();await page.locator('#dsl-apply').click();const comment=await accepted();assert.equal(comment.meaning,beforeComment.meaning);assert.equal(comment.rev,beforeComment.rev);assert.equal(comment.current,beforeComment.current);
  record('Apply_CommentOnly_SourceChangesGeometryDoesNot','Semantic identity/revision/freshness unchanged.');
  const downloadPromise=page.waitForEvent('download');await page.locator('#dsl-save').click();const download=await downloadPromise;const file=path.join(await fs.mkdtemp(path.join(os.tmpdir(),'foildsl-roundtrip-')),'foil.foil');await download.saveAs(file);assert.equal(await fs.readFile(file,'utf8'),comment.source);
  await page.locator('#dsl-file').setInputFiles(file);await page.waitForFunction(()=>DSL.valid!==null);assert.equal(await page.locator('#dsl-source').inputValue(),comment.source);record('SaveOpen_RealFile_SourceBytesPreserved',download.suggestedFilename());
  // Dirty-source exclusion must apply to actual geometry writers as well as source controls.
  await page.locator('#dsl-source').fill(comment.source+'\n# pending');await page.locator('#dsl-geometry').click();const guard=await page.evaluate(()=>{const before=JSON.stringify(M.cv);cvMove('te',3,M.cv.te[3][0],.5);addStation(.42);return {same:before===JSON.stringify(M.cv),noStation:!M.stations.includes(.42),preview:M.preview};});assert(guard.same&&guard.noStation&&!guard.preview);
  await page.locator('[data-doc="source"]').click();await page.locator('#dsl-cancel').click();record('GeometryWriter_SourceDraft_RefusesCompetingEdit',guard);
  // A GUI Apply must use the same semantic validation before changing revision.
  await page.locator('#dsl-geometry').click();const refused=await page.evaluate(()=>{const before=M.revision;cvMove('te',3,M.cv.te[3][0],-1);applyPreview();const out={same:M.revision===before,retained:!!M.preview,valid:dparse(DSL.accepted).half>0};cancelPreview();return out;});assert(refused.same&&refused.retained&&refused.valid);record('VisualApply_NegativeChord_RefusedBeforeCommit',refused);
  // Section knots, source coordinates and skin share a reader; changing a profile reaches the 3D body.
  const section=await page.evaluate(()=>{const before=sectionPoints3D(.6,30),rev=M.revision;const r=drecord();r.section.upper[3][1]+=.002;dsourceInput(demit(r));dcheck();dapply();return {changed:JSON.stringify(before)!==JSON.stringify(sectionPoints3D(.6,30)),rev:M.revision-rev,knots:Object.entries(M.secCV).every(([side,P])=>M.secKnots[side].length===P.length+6),source:dmeaning(dparse(DSL.accepted))===dmeaning(drecord())};});assert(section.changed&&section.rev===1&&section.knots&&section.source);record('SectionEdit_Applied_SourceAnd3DReaderAgree',section);
  await reload();const residual=await page.evaluate(()=>JSON.stringify(M.secResid));await page.locator('#dsl-preview').click();assert.equal(await page.evaluate(()=>JSON.stringify(M.secResid)),residual);record('SourcePreview_NoEdit_PreservesResidualMetadata',residual);
  for(const w of ['minimum','desktop','lines','wide','zoom'])for(const theme of ['light','dark','contrast']){
    await page.locator('#h-window').selectOption(w);await page.locator('#h-theme').selectOption(theme);await page.evaluate(()=>{drender();audit();});
    const row=await page.evaluate(()=>({window:root.dataset.window,theme:root.dataset.theme,...window.workbenchAudit,sourceBox:{width:$('dsl-source').clientWidth,height:$('dsl-source').clientHeight}}));assert(row.frame.width>0&&row.frame.height>0);assert.equal(row.contrastFailures,0);assert.equal(row.smallTargets,0);assert.equal(row.denseUnder,0);assert.equal(row.shell.windowScroll,false);assert(row.sourceBox.height>=150);measurements.push(row);
  }
  await page.locator('#h-window').selectOption('desktop');await page.locator('#h-theme').selectOption('light');await page.locator('#h-motion').check();await page.locator('#dsl-source').focus();await page.keyboard.type('\n# keyboard edit');assert.equal(await page.evaluate(()=>document.activeElement.id),'dsl-source');assert.equal(await page.evaluate(()=>M.tool),'select');
  await page.locator('#dsl-cancel').click();
  await fs.mkdir(proofDir,{recursive:true});
  const shot=path.join(proofDir,`${mockupName}-final-source.png`);await page.screenshot({path:shot});
  assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);record('SourceHarness_15LayoutThemeCells_KeyboardAndNoNetwork',{cells:measurements.length,screenshot:shot});
  const report={artifact,scope:'bounded prototype; not full language/native/scientific conformance',checks,measurements,errors,requests};
  await fs.writeFile(path.join(proofDir,`foildsl${mockupName==='workbench-v7'?'-v7':''}-browser-check.json`),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({checks:checks.length,cells:measurements.length,errors,requests,screenshot:shot}));
}finally{await browser.close();}
