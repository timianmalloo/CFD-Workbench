/** Browser proof for the documentation artifact, not the future application.
 * Requires Playwright plus installed Google Chrome. Optional first argument:
 * directory containing installed Node modules (same as render-spec.mjs).
 */
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {createHash} from 'node:crypto';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const deps=process.argv[2];
const specName=process.env.SPEC_NAME||'cfd-workbench';
const {chromium}=await import(deps?pathToFileURL(path.join(deps,'playwright/index.mjs')).href:'playwright');
const {marked}=await import(deps?pathToFileURL(path.join(deps,'marked/lib/marked.esm.js')).href:'marked');
const source=fs.readFileSync(path.join(root,`docs/specs/${specName}.md`),'utf8');
const expected=marked.parse(source.replace(/^---\n[\s\S]*?\n---\n/,''));
const expectedFlows=[...source.matchAll(/^```mermaid\s*$/gm)].length;
const sourceRevision=source.match(/^Product specification · revision ([0-9.]+) ·/m)?.[1];
const languageRevision=source.match(/^# FoilDSL ([0-9.]+)\s*$/m)?.[1];
if(!sourceRevision&&!languageRevision)throw new Error('Source has no supported specification revision');
const expectedBadge=sourceRevision?`PRODUCT SPECIFICATION · ${sourceRevision}`:`LANGUAGE SPECIFICATION · ${languageRevision}`;
const expectedIds=[...new Set([...source.matchAll(/\b(?:DOC|GOAL|GEO|CAT|ANA|DRC|LAB|CFD|VIZ|EXP|AI|CLI|SET|CAD|XS|RUN|RES|CAND|UX|UI|SRC|DSL)-\d{2}\b/g)].map(m=>m[0]))];
const browser=await chromium.launch({channel:'chrome',headless:true});
try {
 const page=await browser.newPage({viewport:{width:1440,height:1000}});
 const errors=[],external=[];
 page.on('pageerror',e=>errors.push(e.message));
 page.on('request',r=>{if(/^https?:/.test(r.url()))external.push(r.url())});
 await page.goto(pathToFileURL(path.join(root,`docs/specs/${specName}.html`)).href);
 const result=await page.evaluate(expected=>{
   const normalize=s=>s.replace(/\s+/g,' ').trim();
   const holder=document.createElement('div');holder.innerHTML=expected;
   const text=normalize(document.querySelector('main').textContent);
   const blocks=[...holder.querySelectorAll('p,li,h1,h2,h3,h4,th,td,pre')].map(e=>normalize(e.textContent)).filter(Boolean);
   const missing=blocks.filter(b=>!text.includes(b));
   const box=document.querySelector('main').getBoundingClientRect();
   const expectedGeometryNav=[...holder.querySelectorAll('h2,h3')].filter(e=>e.textContent.toLowerCase().includes('geometry')).length;
   return {checkedBlocks:blocks.length,missing,flows:document.querySelectorAll('figure svg').length,expectedGeometryNav,frame:{width:box.width,height:box.height},sourceHash:document.querySelector('meta[name=source-sha256]').content,overflow:document.documentElement.scrollWidth>innerWidth};
 },expected);
 await page.screenshot({path:path.join(os.tmpdir(),`${specName}-desktop.png`)});
 await page.locator('#find').fill('geometry');
 result.visibleNavAfterFilter=await page.locator('nav a:visible').count();
 await page.locator('#find').fill('no-matching-section-fixture');
 result.emptyFilter={links:await page.locator('nav a:visible').count(),status:await page.locator('#find-status').innerText(),visible:await page.locator('#find-status').isVisible()};
 await page.locator('#find').fill('');
 await page.setViewportSize({width:720,height:1000});
 result.narrowOverflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
 result.externalRequests=external;result.pageErrors=errors;
 result.hashMatches=result.sourceHash===createHash('sha256').update(source).digest('hex');
 result.revisionMatches=await page.locator('.badge').innerText()===expectedBadge;
 result.expectedFlows=expectedFlows;
 result.requirementIds=expectedIds.length;
 result.requirementCountMatches=await page.locator('.rail').textContent().then(text=>text.includes(`${expectedIds.length} requirement IDs`));
 const expectedMockup=['cfd-workbench-v1','foildsl'].includes(specName)?'../mockups/workbench-v6.html':'../mockups/workbench.html';
 result.mockupLinkMatches=await page.locator('header a').getAttribute('href')===expectedMockup;
 fs.writeFileSync(path.join(root,specName==='cfd-workbench'?'docs/proof/spec-html-check.json':`docs/proof/spec-html-check-${specName}.json`),JSON.stringify(result,null,2)+'\n');
 console.log(JSON.stringify(result));
 if(!result.checkedBlocks||result.missing.length||!result.hashMatches||!result.revisionMatches||!result.requirementCountMatches||!result.mockupLinkMatches||errors.length||external.length||result.overflow||result.narrowOverflow||result.flows!==expectedFlows||result.visibleNavAfterFilter!==result.expectedGeometryNav||result.emptyFilter.links!==0||!result.emptyFilter.visible||!result.frame.width||!result.frame.height)process.exitCode=1;
} finally {await browser.close()}
