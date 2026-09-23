/** Render the canonical coordination plan as a self-contained HTML view.
 * Usage: node tools/render-coordination-plan.mjs [directory-containing-installed-node-modules]
 */
import fs from 'node:fs';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {fileURLToPath, pathToFileURL} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const deps = process.argv[2];
const {marked} = await import(deps ? pathToFileURL(path.join(deps, 'marked/lib/marked.esm.js')).href : 'marked');
const sourcePath = path.join(root, 'docs/coordination/application-build.md');
const targetPath = path.join(root, 'docs/coordination/application-build.html');
const source = fs.readFileSync(sourcePath, 'utf8');
const sha = createHash('sha256').update(source).digest('hex');
const body = source.replace(/^---\n[\s\S]*?\n---\n/, '');
const content = marked.parse(body).replaceAll('<table>', '<div class="table-wrap"><table>').replaceAll('</table>', '</table></div>');
const html = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>CFD-Workbench — coordination plan</title><meta name="source-sha256" content="${sha}">
<style>
:root{color-scheme:light dark;font:16px/1.5 system-ui,-apple-system,sans-serif}
body{margin:0;background:Canvas;color:CanvasText}
main{max-width:1180px;margin:auto;padding:clamp(1rem,3vw,3rem)}
h1,h2,h3{line-height:1.2;margin:2rem 0 .7rem}h1{font-size:2rem}h2{border-bottom:1px solid color-mix(in srgb,CanvasText 24%,transparent);padding-bottom:.4rem}
a{color:LinkText}a:focus-visible{outline:3px solid Highlight;outline-offset:3px}
.table-wrap{overflow-x:auto;margin:1rem 0 2rem}table{border-collapse:collapse;min-width:720px;width:100%}th,td{border:1px solid color-mix(in srgb,CanvasText 24%,transparent);padding:.65rem;vertical-align:top;text-align:left}th{background:color-mix(in srgb,CanvasText 8%,Canvas)}
code,pre{font: .9em/1.4 ui-monospace,SFMono-Regular,monospace}code{overflow-wrap:anywhere}pre{overflow-x:auto;padding:1rem;background:color-mix(in srgb,CanvasText 7%,Canvas);border-radius:.5rem}
header{border-bottom:1px solid color-mix(in srgb,CanvasText 24%,transparent)}.eyebrow{font-size:.8rem;letter-spacing:.08em;text-transform:uppercase}.note{font-size:.9rem;opacity:.75}
</style></head><body><main><header><div class="eyebrow">CFD-Workbench / execution</div><p class="note">Canonical source: <a href="application-build.md">application-build.md</a> · 23 September 2026</p></header>
${content}<footer><p class="note">This view is generated from the canonical Markdown. Source SHA-256: <code>${sha}</code>.</p></footer></main></body></html>
`;
fs.writeFileSync(targetPath, html, 'utf8');
console.log(JSON.stringify({sourceSha256: sha, bytes: Buffer.byteLength(html)}));
