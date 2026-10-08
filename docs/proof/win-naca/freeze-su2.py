"""Write the TMR Family II SU2 fixture from authoritative PLOT3D and pinned publisher config."""
import datetime,gzip,hashlib,json,pathlib,subprocess,numpy as np,yaml
root=pathlib.Path(__file__).resolve().parents[3]
proof=pathlib.Path(__file__).resolve().parent
inputs=proof/'su2-inputs'; inputs.mkdir()
grid=proof/'sources/n0012familyII.6.p2dfmt.gz'
tokens=gzip.decompress(grid.read_bytes()).split()
nb,ni,nj=map(int,tokens[:3]); assert (nb,ni,nj)==(1,225,65)
v=np.array(tokens[3:3+2*ni*nj],dtype=float)
X=v[:ni*nj].reshape(nj,ni).T; Y=v[ni*nj:].reshape(nj,ni).T
ite=0
while ite<ni//2 and abs(X[ite,0]-X[ni-1-ite,0])<1e-12 and abs(Y[ite,0]-Y[ni-1-ite,0])<1e-12: ite+=1
ite-=1; upper=ni-1-ite; assert (ite,upper)==(48,176)
pid=np.arange(ni*nj).reshape(nj,ni).T.copy()
for i in range(upper,ni): pid[i,0]=pid[ni-1-i,0]
used=np.unique(pid); remap=np.full(ni*nj,-1); remap[used]=np.arange(len(used)); pid=remap[pid]
xy=np.column_stack((X.T.ravel()[used],Y.T.ravel()[used]))
elems=[]; walls=[]; far=[]
for j in range(nj-1):
    for i in range(ni-1):
        q=[int(pid[i,j]),int(pid[i+1,j]),int(pid[i+1,j+1]),int(pid[i,j+1])]
        coords=xy[q]; area=.5*np.sum(coords[:,0]*np.roll(coords[:,1],-1)-coords[:,1]*np.roll(coords[:,0],-1))
        assert area>0,(i,j,area)
        elems.append(q)
for i in range(ite,upper): walls.append((pid[i,0],pid[i+1,0]))
for i in range(ni-1): far.append((pid[i,nj-1],pid[i+1,nj-1]))
for j in range(nj-1): far.extend([(pid[0,j],pid[0,j+1]),(pid[ni-1,j],pid[ni-1,j+1])])
assert (len(elems),len(xy),len(walls),len(far))==(14336,14576,128,352)
lines=['NDIME= 2','NELEM= '+str(len(elems))]
lines+=['9 '+' '.join(map(str,q))+' '+str(i) for i,q in enumerate(elems)]
lines+=['NPOIN= '+str(len(xy))]+[f'{x:.17g} {y:.17g} {i}' for i,(x,y) in enumerate(xy)]
lines+=['NMARK= 2']
for marker,edges in [('airfoil',walls),('farfield',far)]:
    lines+=['MARKER_TAG= '+marker,'MARKER_ELEMS= '+str(len(edges))]+[f'3 {a} {b}' for a,b in edges]
mesh=inputs/'tmr-familyII-L6.su2'; mesh.write_bytes(('\n'.join(lines)+'\n').encode())
# Pin changes before the run. The publisher restart/SA-neg/MUSCL-turb settings are regression setup, not CFL3D matching.
changes={'SA_OPTIONS':'NONE','RESTART_SOL':'NO','CFL_NUMBER':'50.0','MUSCL_TURB':'NO','ITER':'20000','CONV_STARTITER':'20000','MESH_FILENAME':mesh.name}
original=(proof/'sources/su2-naca0012-v8.5.0.cfg').read_text(encoding='utf-8')
cfg=[]
for line in original.splitlines():
    key=line.split('=',1)[0].strip()
    cfg.append(key+'= '+changes[key] if key in changes and not line.lstrip().startswith('%') else line)
cfg+=['FREESTREAM_NU_FACTOR= 3.0','HISTORY_OUTPUT= (ITER, RMS_RES, AERO_COEFF)','HISTORY_WRT_FREQ_INNER= 1']
config=inputs/'tmr-sa.cfg'; config.write_bytes(('\n'.join(cfg)+'\n').encode())
case=yaml.safe_load((root/'cases/win-spike04r3-g0-l6.yaml').read_text(encoding='utf-8'))
case['name']='win-su2-tmr-naca0012'
case['description']='Native SU2 v8.5.0 serial code-to-code comparison with CFL3D SA-noft2 first-order turbulence, exact NASA Family II L6, fully turbulent.'
case['solver']='su2'; case['conditions']['mach']=.15
case['geometry']['source']['path']=mesh.relative_to(root).as_posix()
case['geometry']['source']['sha256']=hashlib.sha256(mesh.read_bytes()).hexdigest()
case['geometry']['source']['generator']='docs/proof/win-naca/freeze-su2.py from pinned NASA Family II PLOT3D; shared fixtures unchanged'
case['geometry']['source']['deviation']='SU2 node-centred finite volume vs CFL3D cell-centred; different spatial implementation and SA S-hat handling; no GCI or physical-validation claim'
case['backend']=dict(name='SU2 native Windows win64-omp',version='8.5.0 Harrier',build='v8.5.0 publisher release',distribution='SU2-v8.5.0-win64-omp.zip',distribution_sha256='4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11',binary_sha256={'SU2_CFD.exe':'3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248'},activation='native SU2_CFD.exe -t 1 tmr-sa.cfg; BELOW_NORMAL_PRIORITY_CLASS',docker_used=False,image_digest=None)
case['physics']['application']='SU2_CFD'; case['physics']['sa_variant']='SA_OPTIONS=NONE (no WITHFT2), freestream nuTilda/nu=3'; case['physics']['transition']='none; fully turbulent'
case['numerics']=dict(config=config.relative_to(root).as_posix(),config_sha256=hashlib.sha256(config.read_bytes()).hexdigest(),max_iterations=20000,convergence='Report residual drop and final 2000-row Cl/Cd half-bands; caps Cl1e-5/Cd1e-6, minimum 3-order RMS residual drop; final result inadmissible if these fail; no tuning after failure')
case['numerics']['residual_criterion']=case['numerics']['convergence']
case['decomposition']=dict(method='serial',n_subdomains=1,nice=10,nice_platform_note='POSIX nice inapplicable; Windows BELOW_NORMAL_PRIORITY_CLASS requested',omp_threads=1)
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
case['run_dir']='runs/'+stamp+'-win-su2-tmr-naca0012'
target=root/'cases/win-su2-tmr-naca0012.yaml'; target.write_bytes(yaml.safe_dump(case,sort_keys=False,allow_unicode=True).encode())
case['outputs_expected']=['history.csv','log.SU2_CFD','su2-resources.json']; target.write_bytes(yaml.safe_dump(case,sort_keys=False,allow_unicode=True).encode())
# Reuse the real schema before freezing or launching, not a mirror of its required fields.
import runpy
validation=runpy.run_path(str(root/'cases/tools/validate-cases.py'))
assert not validation['errors_for'](validation['load_validator'](),case,target.stem)
records=[]
for f in [mesh,config,target]:
    assert b'\r' not in f.read_bytes()
    rel=f.relative_to(root).as_posix(); subprocess.run(['git','add','--',rel],cwd=root,check=True)
    blob=subprocess.run(['git','show',':'+rel],cwd=root,check=True,stdout=subprocess.PIPE).stdout
    assert blob==f.read_bytes(),rel
    records.append(dict(path=rel,sha256=hashlib.sha256(blob).hexdigest(),bytes=len(blob)))
state=dict(run=case['run_dir'],frozen_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),changes_from_publisher=changes,files=records,counts=dict(cells=len(elems),points=len(xy),airfoil_edges=len(walls),farfield_edges=len(far)))
(proof/'su2-frozen.json').write_bytes((json.dumps(state,indent=2)+'\n').encode())
print(json.dumps(state))
