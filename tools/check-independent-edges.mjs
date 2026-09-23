#!/usr/bin/env node
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const {chromium}=await import(pathToFileURL(path.join(process.argv[2],'playwright/index.mjs')).href);
const b=await chromium.launch({channel:'chrome',headless:true});
const p=await b.newPage({viewport:{width:1700,height:1200}});
const errors=[],checks=[];p.on('pageerror',e=>errors.push(e.message));
const url=pathToFileURL(path.join(root,'docs/mockups/workbench-v6.html')).href;
const state=()=>p.evaluate(()=>({record:drecord(),source:DSL.accepted,revision:M.revision,current:M.run.current,samples:Array.from({length:401},(_,i)=>{const u=i/400;return [chanAt('le',u),chanAt('te',u),chanAt('chord',u)];})}));
const unchanged=(a,z,ch)=>{assert.deepEqual(z.record.cv[ch],a.record.cv[ch],`${ch} authored controls changed`);const j=ch==='le'?0:1;assert.deepEqual(z.samples.map(s=>s[j]),a.samples.map(s=>s[j]),`${ch} evaluated curve changed`);};
const coherent=z=>{for(const s of z.samples)assert(Math.abs(s[2]-(s[1]-s[0]))<1e-14);};
try {
 for(const edge of ['le','te']) {
  await p.goto(url);
  await p.evaluate(()=>{const r=drecord();r.cv.le=Array.from({length:6},(_,i)=>[Math.pow(i/5,1.2),i<2?0:.055*(i-1)/4]);r.cv.te=Array.from({length:9},(_,i)=>[Math.pow(i/8,.9),i<2?.15:.15-.04*(i-1)/7]);dsourceInput(demit(r));if(!dcheck())throw Error(DSL.diagnostic);dapply();renderShape();});
  const before=await state(),opposite=edge==='le'?'te':'le';
  const sel=`#quads .quad[data-slot="a"] [data-cv="${edge}:3"]`,oppositePath=`#quads .quad[data-slot="a"] [data-pick="${opposite}"]`;
  const pathBefore=await p.locator(oppositePath).getAttribute('d');
  const box=await p.locator(sel+' rect').boundingBox();const x=box.x+box.width/2,y=box.y+box.height/2;
  await p.mouse.move(x,y);await p.mouse.down();await p.mouse.move(x+8,y+4,{steps:8});await p.mouse.up();
  const draft=await state();unchanged(before,draft,opposite);assert.equal(draft.source,before.source);assert.equal(draft.revision,before.revision);coherent(draft);
  assert.equal(await p.locator(oppositePath).getAttribute('d'),pathBefore,'fixed opposite planform path moved on screen');
  assert(await p.evaluate(edge=>M.preview.ch===edge&&M.preview.P[3][0]!==M.cv[edge][3][0]&&M.preview.P[3][1]!==M.cv[edge][3][1],edge));
  await p.keyboard.press('Escape');assert.deepEqual((await state()).samples,before.samples);
  await p.locator(sel).focus();await p.keyboard.press('ArrowRight');await p.keyboard.press('ArrowUp');await p.keyboard.press('Enter');
  const after=await state();unchanged(before,after,opposite);assert.notDeepEqual(after.record.cv[edge],before.record.cv[edge]);assert.equal(after.revision,before.revision+1);assert.equal(after.current,false);coherent(after);
  assert(await p.evaluate(()=>dmeaning(dparse(DSL.accepted))===dmeaning(drecord())));
  await p.evaluate(()=>undo());const undone=await state();assert.deepEqual(undone.record,before.record);assert.equal(undone.source,before.source);
  await p.evaluate(()=>redo());const redone=await state();assert.deepEqual(redone.record,after.record);assert.equal(redone.source,after.source);unchanged(before,redone,opposite);
  await p.evaluate(edge=>{const r=drecord();r.cv[edge][3][1]+=.001;dsourceInput(demit(r));if(!dcheck())throw Error(DSL.diagnostic);dapply();},edge);unchanged(after,await state(),opposite);
  checks.push({name:`${edge}: independent basis pointer/cancel/keyboard/apply/undo/redo/source`,pass:true,opposite,points:before.record.cv[edge].length,oppositePoints:before.record.cv[opposite].length,samples:401});
 }
 await p.goto(url);
 const crossing=await p.evaluate(()=>{const before=DSL.accepted,rev=M.revision;const r=drecord();r.cv.te=r.cv.le.map(([x,y])=>[x,y+.001]);r.cv.te[3][1]-=.01;const positive=r.cv.te.every(q=>q[1]>0);dsourceInput(demit(r));const accepted=dcheck();return {positive,rejected:!accepted,held:DSL.accepted===before&&M.revision===rev,diagnostic:DSL.diagnostic};});
 assert(crossing.positive&&crossing.rejected&&crossing.held);checks.push({name:'Positive TE controls with crossing rails rejected',pass:true,...crossing});await p.evaluate(()=>dcancel());
 const legacy=await p.evaluate(()=>{try{dparse(DSL.accepted.replace('    trailing cv','    chord cv'));return null;}catch(e){return {code:e.code,message:e.msg};}});assert.equal(legacy.code,'DSL-LEGACY');assert.match(legacy.message,/Earlier 4.0 draft.*Preserve it.*explicit conversion/);checks.push({name:'Superseded draft chord field rejected explicitly',pass:true,diagnostic:legacy});
 const before=await state();await p.locator('#quads [data-cv="te:3"]').focus();assert.match(await p.locator('label[for="cv-val"]').innerText(),/aft position/);
 const desired=before.record.cv.te[3][1]+.001;await p.locator('#cv-val').fill(`${desired} m`);await p.locator('#cv-val').press('Tab');await p.locator('#shape-canvas').focus();await p.keyboard.press('Enter');const after=await state();unchanged(before,after,'le');assert.equal(after.record.cv.te[3][1],desired);coherent(after);checks.push({name:'Numeric TE aft-position edit keeps LE fixed',pass:true});
 const fixed=await p.locator('#quads .quad[data-slot="a"] [data-pick="le"]').getAttribute('d');await p.locator('#quads [data-cv="te:0"]').focus();await p.keyboard.press('ArrowUp');assert.equal(await p.locator('#quads .quad[data-slot="a"] [data-pick="le"]').getAttribute('d'),fixed);await p.keyboard.press('Escape');checks.push({name:'Root TE preview leaves opposite screen transform fixed',pass:true});
 const screenshot='/tmp/independent-edges-cad.png';await p.screenshot({path:screenshot});assert.deepEqual(errors,[]);
 await fs.writeFile(path.join(root,'docs/proof/independent-edges.json'),JSON.stringify({scope:'unrotated planform rails; placed sections retain LE-pivot twist',red:'Prior LE edit moved TE by 0.001602926112762984 m',checks,errors,screenshot},null,2)+'\n');console.log(JSON.stringify({checks:checks.length,errors,screenshot}));
} finally {await b.close();}
