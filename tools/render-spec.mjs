/** Render the canonical specification; no runtime dependencies in the output.
 * Usage: node tools/render-spec.mjs [directory-containing-installed-node-modules]
 * Authoring dependencies used: marked 17.0.5, @viz-js/viz 3.25.0 (both MIT).
 * These are documentation tools, not a choice of application stack.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const deps = process.argv[2];
const specName = process.env.SPEC_NAME || 'cfd-workbench';
const {marked} = await import(deps ? pathToFileURL(path.join(deps, 'marked/lib/marked.esm.js')).href : 'marked');
const {instance} = await import(deps ? pathToFileURL(path.join(deps, '@viz-js/viz/dist/viz.js')).href : '@viz-js/viz');
const viz = await instance();
const source = fs.readFileSync(path.join(root,`docs/specs/${specName}.md`),'utf8');
const body = source.replace(/^---\n[\s\S]*?\n---\n/, '');
const revision = body.match(/^Product specification · revision ([0-9.]+) ·/m)?.[1];
if (!revision) throw new Error('Canonical specification revision is missing');
const sha = createHash('sha256').update(source).digest('hex');
const escape = s => String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const quote = s => JSON.stringify(s);
function diagram(code, index) {
  const nodes = new Map(), edges=[];
  function node(s) {
    const m=s.trim().match(/^(\w+)(?:\[([^\]]+)\]|\{([^}]+)\})?$/);
    if(!m) throw new Error(`Unsupported flow node: ${s}`);
    if(m[2]||m[3]) nodes.set(m[1],{label:m[2]||m[3], shape:m[3]?'diamond':'box'});
    return m[1];
  }
  for(const line of code.trim().split('\n').slice(1)) {
    if(!line.trim()) continue;
    const m=line.match(/^(.+?)\s*-->\s*(?:\|([^|]+)\|\s*)?(.+)$/);
    if(!m) throw new Error(`Unsupported flow edge: ${line}`);
    edges.push([node(m[1]),node(m[3]),m[2]||'']);
  }
  const dot=`digraph {graph [rankdir=TB,bgcolor="transparent",pad="0.25",nodesep="0.35",ranksep="0.5"]; node [fontname="Helvetica",fontsize=12,style="rounded,filled",color="#a5b5b3",fillcolor="#f0f5f3",fontcolor="#172e2c",margin="0.16,0.12"]; edge [fontname="Helvetica",fontsize=10,color="#597d78",fontcolor="#405852",arrowsize=0.7]; ${[...nodes].map(([id,n])=>`${id} [label=${quote(n.label)},shape=${n.shape}];`).join('\n')} ${edges.map(([a,b,l])=>`${a} -> ${b} [label=${quote(l)}];`).join('\n')}}`;
  let svg=viz.renderString(dot,{format:'svg'}).replace(/<\?xml[^>]*\?>/g,'').replace(/<!DOCTYPE[\s\S]*?>/g,'');
  svg=svg.replace('<svg ',`<svg role="img" aria-label="Workflow ${index}; full flow source follows" `);
  return `<figure class="flow">${svg}<figcaption>Flow ${index} · happy, alternate and recovery paths</figcaption></figure><details><summary>Read the full flow definition</summary><pre>${escape(code)}</pre></details>`;
}
let flowCount=0;
marked.use({renderer:{code(token){if(token.lang==='mermaid') return diagram(token.text,++flowCount);return `<pre><code>${escape(token.text)}</code></pre>`;}}});
let content=marked.parse(body);
const toc=[]; const slugs=new Map();
content=content.replace(/<h([234])>([\s\S]*?)<\/h\1>/g,(_,n,label)=>{
  const plain=label.replace(/<[^>]+>/g,'');
  let slug=plain.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
  const count=slugs.get(slug)||0;slugs.set(slug,count+1);if(count)slug+=`-${count}`;
  if(Number(n)<=3)toc.push({n:Number(n),slug,label:plain});
  return `<h${n} id="${slug}">${label}</h${n}>`;
});
content=content.replace(/<table>/g,'<div class="table-scroll" tabindex="0" role="region" aria-label="Scrollable specification table"><table>').replace(/<\/table>/g,'</table></div>');
const ids=[...new Set([...body.matchAll(/\b(?:DOC|GOAL|GEO|CAT|ANA|DRC|LAB|CFD|VIZ|EXP|AI|CLI|SET|CAD|XS|RUN|RES|CAND|UX|UI)-\d{2}\b/g)].map(x=>x[0]))];
for(const id of ids) if(!content.includes(id)) throw new Error(`Requirement omitted: ${id}`);
const html=`<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="source-sha256" content="${sha}"><title>CFD-Workbench — Product specification</title>
<style>
:root{--paper:#f7f9f7;--surface:#ffffff;--ink:#19302e;--muted:#506762;--line:#c5d1cc;--accent:#096b61;--shade:#e9f1ed;--dark:#142d2a;--on-dark:#eef8f3;--focus:#007d70;--mono:ui-monospace,SFMono-Regular,Consolas,monospace;--sans:system-ui,-apple-system,"Segoe UI",sans-serif;--s1:4px;--s2:8px;--s3:12px;--s4:16px;--s6:24px;--s8:32px;--s12:48px;--small:13px;--body:16px;--title:48px;--h2:30px;--h3:22px;--radius:4px;--rail:270px;--measure:1120px}
[hidden]{display:none!important}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:var(--body)/1.7 var(--sans)}a{color:var(--accent);text-underline-offset:3px}button,input{font:inherit}a:focus-visible,button:focus-visible,input:focus-visible,summary:focus-visible,[tabindex]:focus-visible{outline:3px solid var(--focus);outline-offset:3px}.top{display:flex;align-items:center;justify-content:space-between;gap:var(--s4);padding:var(--s4) var(--s8);background:var(--dark);color:var(--on-dark)}.top a{color:var(--on-dark)}.brand{font-weight:700;letter-spacing:.02em}.badge{font:var(--small) var(--mono);border:1px solid var(--line);padding:var(--s1) var(--s2)}.layout{display:grid;grid-template-columns:var(--rail) minmax(0,1fr);max-width:1600px;margin:auto}.rail{position:sticky;top:0;height:100vh;overflow:auto;padding:var(--s8) var(--s6);border-right:1px solid var(--line)}.rail p{font:var(--small) var(--mono);color:var(--muted);text-transform:uppercase;letter-spacing:.06em}.rail nav a{display:block;font-size:var(--small);line-height:1.5;padding:var(--s2) 0;text-decoration:none}.rail nav a.depth2{font-weight:700;border-top:1px solid var(--line);margin-top:var(--s3)}.rail nav a:hover{text-decoration:underline}.rail input{width:100%;min-height:44px;border:1px solid var(--line);border-radius:var(--radius);padding:var(--s2);background:var(--surface);color:var(--ink)}main{max-width:var(--measure);padding:var(--s12);min-width:0}h1{font-size:var(--title);line-height:1.1;margin:var(--s6) 0;letter-spacing:-.04em}h2{font-size:var(--h2);line-height:1.25;margin:var(--s12) 0 var(--s6);letter-spacing:-.02em;border-top:1px solid var(--line);padding-top:var(--s8)}h3{font-size:var(--h3);line-height:1.4;margin:var(--s8) 0 var(--s4)}h4{font-size:var(--body);margin:var(--s6) 0 var(--s3)}p{margin:var(--s4) 0}main>p,main>ul{max-width:85ch}li{margin:var(--s2) 0}.table-scroll{overflow-x:auto;margin:var(--s6) 0;border:1px solid var(--line);border-radius:var(--radius);background:var(--surface)}table{border-collapse:collapse;width:100%;font-size:var(--small);line-height:1.6}th,td{text-align:left;vertical-align:top;padding:var(--s3) var(--s4);border-bottom:1px solid var(--line)}th{background:var(--shade);font-weight:650}td:first-child{min-width:160px}tr:last-child td{border-bottom:0}code{font:var(--small)/1.6 var(--mono);overflow-wrap:anywhere}pre{white-space:pre-wrap;overflow-wrap:anywhere;padding:var(--s4);background:var(--shade);font:var(--small)/1.6 var(--mono)}.flow{padding:var(--s6);margin:var(--s6) 0 var(--s2);background:var(--surface);border:1px solid var(--line);overflow:auto}.flow svg{display:block;width:100%;height:auto;min-width:540px;max-height:1100px}.flow figcaption{font-size:var(--small);color:var(--muted);margin-top:var(--s4)}summary{cursor:pointer;font-size:var(--small);min-height:32px}footer{font-size:var(--small);color:var(--muted);border-top:1px solid var(--line);padding-top:var(--s6);overflow-wrap:anywhere}.jump{position:absolute;left:-9999px}.jump:focus{left:var(--s4);top:var(--s4);background:var(--surface);padding:var(--s4);z-index:10}button{min-height:44px;background:var(--surface);color:var(--accent);border:1px solid var(--line);border-radius:var(--radius);padding:var(--s2) var(--s4);cursor:pointer}@media(max-width:1000px){:root{--rail:220px}main{padding:var(--s8)}}@media(max-width:740px){.layout{display:block}.rail{position:static;height:auto;border-bottom:1px solid var(--line)}.rail nav{max-height:220px;overflow:auto}.top{flex-wrap:wrap}main{padding:var(--s6)}h1{font-size:36px}}@media print{.rail,.top,.jump{display:none}.layout{display:block}main{max-width:none;padding:0;font-size:11pt}h2{break-before:page}.table-scroll{overflow:visible}tr{break-inside:avoid}.flow svg{min-width:0;max-height:240mm}a{color:inherit}button{display:none}}
</style></head><body><a class="jump" href="#content">Skip to specification</a><header class="top"><span class="brand">CFD / WORKBENCH</span><span class="badge">PRODUCT SPECIFICATION · ${escape(revision)}</span><a href="../mockups/workbench.html">Open interactive design ↗</a></header><div class="layout"><aside class="rail"><p>Specification map</p><label for="find">Find a section</label><input id="find" type="search" placeholder="Geometry, analysis, AI…"><p id="find-status" role="status" hidden></p><nav aria-label="Contents">${toc.map(x=>`<a class="depth${x.n}" href="#${x.slug}">${x.label}</a>`).join('')}</nav><p>${ids.length} requirement IDs · ${flowCount} flows</p><button onclick="window.print()">Print / save as PDF</button></aside><main id="content">${content}<footer>Generated from the complete Markdown specification. Source SHA-256: ${sha}. The mockup is illustrative; native application and scientific validation are separate future gates.</footer></main></div><script>document.getElementById('find').addEventListener('input',event=>{const query=event.target.value.toLowerCase();document.querySelectorAll('nav a').forEach(link=>{link.hidden=!link.textContent.toLowerCase().includes(query)});const status=document.getElementById("find-status");const count=[...document.querySelectorAll("nav a")].filter(link=>!link.hidden).length;status.hidden=count>0;status.textContent=count?"":"No matching sections. Clear the search to browse all."});</script></body></html>`;
fs.writeFileSync(path.join(root,`docs/specs/${specName}.html`),html);
console.log(JSON.stringify({sourceSha256:sha,requirementIds:ids.length,flows:flowCount,sections:toc.length,bytes:Buffer.byteLength(html)}));
