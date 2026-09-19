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
const {chromium}=await import(deps?pathToFileURL(path.join(deps,'playwright/index.mjs')).href:'playwright');
const {marked}=await import(deps?pathToFileURL(path.join(deps,'marked/lib/marked.esm.js')).href:'marked');
const source=fs.readFileSync(path.join(root,'docs/specs/cfd-workbench.md'),'utf8');
const expected=marked.parse(source.replace(/^---\n[\s\S]*?\n---\n/,''));
const browser=await chromium.launch({channel:'chrome',headless:true});
try {
 const page=await browser.newPage({viewport:{width:1440,height:1000}});
 const errors=[],external=[];
 page.on('pageerror',e=>errors.push(e.message));
 page.on('request',r=>{if(/^https?:/.test(r.url()))external.push(r.url())});
 await page.goto(pathToFileURL(path.join(root,'docs/specs/cfd-workbench.html')).href);
 const result=await page.evaluate(expected=>{
   const normalize=s=>s.replace(/\s+/g,' ').trim();
   const holder=document.createElement('div');holder.innerHTML=expected;
   const text=normalize(document.querySelector('main').textContent);
   const blocks=[...holder.querySelectorAll('p,li,h1,h2,h3,h4,th,td,pre')].map(e=>normalize(e.textContent)).filter(Boolean);
   const missing=blocks.filter(b=>!text.includes(b));
   const box=document.querySelector('main').getBoundingClientRect();
   return {checkedBlocks:blocks.length,missing,flows:document.querySelectorAll('figure svg').length,frame:{width:box.width,height:box.height},sourceHash:document.querySelector('meta[name=source-sha256]').content,overflow:document.documentElement.scrollWidth>innerWidth};
 },expected);
 await page.screenshot({path:path.join(os.tmpdir(),'cfd-spec-desktop.png')});
 await page.locator('#find').fill('geometry');
 result.visibleNavAfterFilter=await page.locator('nav a:visible').count();
 await page.locator('#find').fill('no-matching-section-fixture');
 result.emptyFilter={links:await page.locator('nav a:visible').count(),status:await page.locator('#find-status').innerText(),visible:await page.locator('#find-status').isVisible()};
 await page.locator('#find').fill('');
 await page.setViewportSize({width:720,height:1000});
 result.narrowOverflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
 result.externalRequests=external;result.pageErrors=errors;
 result.hashMatches=result.sourceHash===createHash('sha256').update(source).digest('hex');
 fs.writeFileSync(path.join(root,'docs/proof/spec-html-check.json'),JSON.stringify(result,null,2)+'\n');
 console.log(JSON.stringify(result));
 if(result.missing.length||!result.hashMatches||errors.length||external.length||result.overflow||result.narrowOverflow||result.flows!==5||result.visibleNavAfterFilter!==1||result.emptyFilter.links!==0||!result.emptyFilter.visible||!result.frame.width||!result.frame.height)process.exitCode=1;
} finally {await browser.close()}
