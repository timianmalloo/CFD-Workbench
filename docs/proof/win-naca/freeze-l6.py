"""Freeze exact G0 numerics and prove equality to the Mac dictionary manifest before launch."""
import datetime,hashlib,json,pathlib,subprocess,yaml
root=pathlib.Path(__file__).resolve().parents[3]
proof=pathlib.Path(__file__).resolve().parent
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
source=root/'cases/spike04r3-g0-l6.yaml'
case=yaml.safe_load(source.read_text(encoding='utf-8'))
case['name']='win-spike04r3-g0-l6'
case['description']='Windows/WSL exact R3-G0 L6 cross-platform comparison; G0 is diagnostic, not a grid-family validation.'
case['backend']=dict(name='OpenCFD Ubuntu runtime under WSL2',version='v2512',build='_bd2b6720-20260127',distribution='openfoam2512=2512.0-2; Ubuntu 24.04.5; cfdw-openfoam2512',distribution_sha256='c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed',binary_sha256={'simpleFoam':'b7bb6321cac3586878a6c51c07f3139dba651ff1f3dbdb0dd406536b27846ba0'},activation='evidence-local Linux adapter; env -i, publisher activation, isolated HOME, M1, lint/tree/preflight checks',docker_used=False,image_digest=None)
case['run_dir']='runs/'+stamp+'-win-spike04r3-g0-l6'
target=root/'cases/win-spike04r3-g0-l6.yaml'
target.write_bytes(yaml.safe_dump(case,sort_keys=False,allow_unicode=True).encode('utf-8'))
inputs=proof/'l6-inputs'
subprocess.run(['py','-3','cases/tools/make-tmr-case.py',str(target),str(inputs),str(proof/'sources/n0012familyII.6.p2dfmt.gz')],cwd=root,check=True)
mac=root/'docs/proof/spike-04/receipts/20261004T175236Z-spike04r3-g0-l6'
expected=json.loads((mac/'cfdw-manifest.json').read_text())
assert json.loads((inputs/'cfdw-manifest.json').read_text())==expected,'generated dictionaries differ from exact Mac G0'
print('PASS: all 11 generator-written dictionary hashes equal exact Mac G0 manifest')
records=[]
for f in sorted(inputs.rglob('*'))+[target,source,root/'cases/tools/make-tmr-case.py',mac/'cfdw-manifest.json',mac/'convergence.txt',root/'cases/tools/foam-bundle/controlDict']:
    if not f.is_file(): continue
    assert b'\r' not in f.read_bytes(),str(f)
    rel=f.relative_to(root).as_posix()
    if rel.startswith('docs/proof/win-naca/') or f==target:
        subprocess.run(['git','add','--',rel],cwd=root,check=True)
        blob=subprocess.run(['git','show',':'+rel],cwd=root,check=True,stdout=subprocess.PIPE).stdout
    else: blob=subprocess.run(['git','show','HEAD:'+rel],cwd=root,check=True,stdout=subprocess.PIPE).stdout
    assert blob==f.read_bytes(),rel+' blob differs'
    records.append(dict(path=rel,sha256=digest(f),blob_sha256=hashlib.sha256(blob).hexdigest(),bytes=len(blob)))
state=dict(started_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),tested_source_sha=subprocess.run(['git','rev-parse','HEAD'],cwd=root,check=True,stdout=subprocess.PIPE,text=True).stdout.strip(),mac_case='cases/spike04r3-g0-l6.yaml',absent_shared_case='cases/spike04r3-g2-l6.yaml',run=case['run_dir'],linux_run='/root/CFDWorkbench/'+case['run_dir'],frozen_files=records)
(proof/'l6-frozen.json').write_bytes((json.dumps(state,indent=2)+'\n').encode())
print(json.dumps({k:v for k,v in state.items() if k!='frozen_files'}))
