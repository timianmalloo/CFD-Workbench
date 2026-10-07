"""Ruling 133 W-3 evidence runner. Serial, bounded, LF bytes; no elevation."""
import ctypes, datetime, hashlib, json, os, pathlib, shutil, subprocess, sys, time
from ctypes import wintypes
import yaml
ROOT = pathlib.Path(__file__).resolve().parents[3]
PROOF = ROOT/'docs/proof/win-routes'
OUT = PROOF/'r133'
OUT.mkdir(exist_ok=True)
WSL = ['wsl.exe','--distribution','cfdw-openfoam2512','--user','root','--exec']
def run(tag,argv,timeout=60,cwd=None):
    start=datetime.datetime.now(datetime.timezone.utc).isoformat()
    clock=time.perf_counter()
    result=subprocess.run(argv,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=timeout,cwd=cwd)
    raw=result.stdout
    output=raw.decode('utf-16-le' if b'\x00' in raw[:100] else 'utf-8',errors='replace')
    filename=tag+'.txt'
    index=1
    while (OUT/filename).exists():
        index+=1
        filename=tag+'-'+str(index)+'.txt'
    (OUT/filename).write_bytes(('\n'.join(line.rstrip() for line in output.splitlines()).rstrip()+'\n').encode())
    row=dict(tag=tag,argv=argv,started_utc=start,duration_seconds=time.perf_counter()-clock,exit_code=result.returncode,output=filename)
    with (OUT/'steps.jsonl').open('ab') as f: f.write((json.dumps(row)+'\n').encode())
    print(json.dumps(row),flush=True)
    print(output[-1300:],flush=True)
    if result.returncode: raise SystemExit(result.returncode)
    return output
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def su2_smoke():
    state=json.loads((OUT/'run-paths.json').read_text())
    dest=ROOT/state['su2']
    binary=pathlib.Path(os.environ['LOCALAPPDATA'])/'CFDWorkbench/qualification/su2-8.5.0/bin/bin/SU2_CFD.exe'
    if sha(binary)!='3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248': raise SystemExit('SU2 binary hash mismatch')
    for name in ['su2-smoke.cfg','su2-cylinder.su2']:
        if (dest/name).read_bytes()!=(PROOF/name).read_bytes(): raise SystemExit('SU2 input mismatch')
    class Counters(ctypes.Structure):
        _fields_=[('cb',wintypes.DWORD),('PageFaultCount',wintypes.DWORD)]+[(n,ctypes.c_size_t) for n in ['PeakWorkingSetSize','WorkingSetSize','QuotaPeakPagedPoolUsage','QuotaPagedPoolUsage','QuotaPeakNonPagedPoolUsage','QuotaNonPagedPoolUsage','PagefileUsage','PeakPagefileUsage']]
    memory=ctypes.WinDLL('psapi',use_last_error=True).GetProcessMemoryInfo
    memory.argtypes=[wintypes.HANDLE,ctypes.POINTER(Counters),wintypes.DWORD]
    memory.restype=wintypes.BOOL
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    times=kernel.GetProcessTimes
    times.argtypes=[wintypes.HANDLE]+[ctypes.POINTER(wintypes.FILETIME)]*4
    times.restype=wintypes.BOOL
    priority=kernel.GetPriorityClass
    priority.argtypes=[wintypes.HANDLE]
    priority.restype=wintypes.DWORD
    start=datetime.datetime.now(datetime.timezone.utc).isoformat()
    clock=time.perf_counter()
    argv=[str(binary),'-t','1','su2-smoke.cfg']
    peak=None
    samples=0
    with (dest/'log.SU2_CFD').open('wb') as log:
        process=subprocess.Popen(argv,stdout=log,stderr=subprocess.STDOUT,cwd=dest,creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS)
        observed_priority=priority(int(process._handle))
        while process.poll() is None:
            values=Counters()
            values.cb=ctypes.sizeof(values)
            if memory(int(process._handle),ctypes.byref(values),values.cb):
                peak=max(peak or 0,values.PeakWorkingSetSize)
                samples+=1
            if time.perf_counter()-clock>60:
                process.kill()
                process.wait()
                raise SystemExit('SU2 60-second timeout')
            time.sleep(.01)
        fields=[wintypes.FILETIME() for _ in range(4)]
        cpu='Not recorded'
        if times(int(process._handle),*[ctypes.byref(f) for f in fields]):
            cpu=sum((f.dwHighDateTime<<32)+f.dwLowDateTime for f in fields[2:])/10000000
        row=dict(tag='su2-smoke-LF',argv=argv,working_directory=str(dest),started_utc=start,duration_seconds=time.perf_counter()-clock,exit_code=process.returncode,output='su2-smoke-LF.txt',cpu_seconds=cpu,sampled_peak_working_set_bytes=peak if peak is not None else 'Not recorded',memory_samples=samples,observed_priority_class=observed_priority,creation_flags='BELOW_NORMAL_PRIORITY_CLASS')
    raw=(dest/'log.SU2_CFD').read_bytes().decode('utf-8',errors='replace')
    (OUT/'su2-smoke-LF.txt').write_bytes(('\n'.join(line.rstrip() for line in raw.splitlines()).rstrip()+'\n').encode())
    with (OUT/'steps.jsonl').open('ab') as f: f.write((json.dumps(row)+'\n').encode())
    (OUT/'su2-resources.json').write_bytes((json.dumps(row,indent=2)+'\n').encode())
    print(json.dumps(row),flush=True)
    if process.returncode: raise SystemExit(process.returncode)
    history=(dest/'history.csv').read_bytes().replace(b'\r\n',b'\n')
    (PROOF/'su2-history.csv').write_bytes(history)
    (OUT/'su2-history.csv').write_bytes(history)
    (PROOF/'su2-run-dir.txt').write_bytes((str(dest)+'\n').encode())
    print(history.decode().splitlines()[-1],flush=True)
def freeze():
    stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    cavity_rel='runs/'+stamp+'-win-smoke-cavity'
    linux_run='/root/CFDWorkbench/'+cavity_rel
    for f in (OUT/'cavity-inputs').rglob('*'):
        if f.is_file(): f.write_bytes(f.read_bytes().replace(b'\r\n',b'\n'))
    bundle=ROOT/'cases/tools/foam-bundle/controlDict'
    if sha(bundle)!='f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854': raise SystemExit('M1 bundle hash mismatch')
    shutil.copyfile(bundle,OUT/'product-controlDict')
    case=yaml.safe_load((ROOT/'cases/smoke-cavity.yaml').read_text())
    case['name']='win-smoke-cavity'
    case['description']='Ruling 133 serial Windows/WSL2 OpenFOAM v2512 installation smoke; frozen LF publisher cavity inputs under M1.'
    case['geometry']['source']['path']='docs/proof/win-routes/r133/cavity-inputs/system/blockMeshDict'
    case['geometry']['source']['sha256']=sha(OUT/'cavity-inputs/system/blockMeshDict')
    case['backend']=dict(name='OpenFOAM (OpenCFD) Ubuntu noble runtime under WSL2',version='v2512',build='_bd2b6720-20260127',distribution='openfoam2512=2512.0-2; Ubuntu 24.04.5 WSL2 cfdw-openfoam2512',distribution_sha256='c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed',binary_sha256={'icoFoam':'789678a0003411bd835b9a6458d50d70180033b791e1b82648ae50bf5f08fec9'},activation='env -i PATH=/usr/bin:/bin HOME=<fresh product-home> USER=root LOGNAME=root LANG=C bash --noprofile --norc docs/proof/win-routes/r133/openfoam-command.sh <fresh-run> <app>',docker_used=False,image_digest=None)
    case['run_dir']=cavity_rel
    case['runtime_run_dir']=linux_run
    case['numerics']['frozen_input_sha256']={str(f.relative_to(OUT/'cavity-inputs')).replace('\\','/'):sha(f) for f in (OUT/'cavity-inputs').rglob('*') if f.is_file()}
    case['numerics']['product_controlDict_sha256']=sha(bundle)
    (ROOT/'cases/win-smoke-cavity.yaml').write_bytes(yaml.safe_dump(case,sort_keys=False).encode())
    su2_rel='runs/'+stamp+'-win-su2-smoke'
    su2run=ROOT/su2_rel
    su2run.mkdir(parents=True)
    for name in ['su2-cylinder.su2','su2-smoke.cfg','su2-history.csv']:
        p=PROOF/name
        p.write_bytes(p.read_bytes().replace(b'\r\n',b'\n'))
    for name in ['su2-cylinder.su2','su2-smoke.cfg']: shutil.copyfile(PROOF/name,su2run/name)
    su2=yaml.safe_load((ROOT/'cases/win-su2-smoke.yaml').read_text())
    su2['geometry']['source']['sha256']=sha(PROOF/'su2-cylinder.su2')
    su2['numerics']['config_sha256']=sha(PROOF/'su2-smoke.cfg')
    su2['run_dir']=su2_rel
    su2['decomposition']['windows_process_priority']='BELOW_NORMAL_PRIORITY_CLASS (observed requested creation flag 0x4000)'
    su2['decomposition']['nice_observed']='POSIX nice is inapplicable; Windows below-normal priority is used for this run.'
    (ROOT/'cases/win-su2-smoke.yaml').write_bytes(yaml.safe_dump(su2,sort_keys=False).encode())
    state=dict(cavity=cavity_rel,cavity_linux=linux_run,su2=su2_rel)
    (OUT/'run-paths.json').write_bytes((json.dumps(state,indent=2)+'\n').encode())
    files=list((OUT/'cavity-inputs').rglob('*'))+[OUT/'product-controlDict',PROOF/'su2-cylinder.su2',PROOF/'su2-smoke.cfg',ROOT/'cases/win-smoke-cavity.yaml',ROOT/'cases/win-su2-smoke.yaml']
    records=[]
    for f in files:
        if not f.is_file(): continue
        if b'\r' in f.read_bytes(): raise SystemExit('non-LF frozen input '+str(f))
        rel=f.relative_to(ROOT).as_posix()
        subprocess.run(['git','add','--',rel],cwd=ROOT,check=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        blob=subprocess.run(['git','show',':'+rel],cwd=ROOT,check=True,stdout=subprocess.PIPE).stdout
        if blob!=f.read_bytes(): raise SystemExit('staged blob differs '+rel)
        records.append(dict(path=rel,sha256=sha(f),staged_blob_sha256=hashlib.sha256(blob).hexdigest(),equal=True))
    if (OUT/'frozen-before-launch.json').exists():
        shutil.copyfile(OUT/'frozen-before-launch.json',OUT/'frozen-before-first-activation.json')
    (OUT/'frozen-before-launch.json').write_bytes((json.dumps(dict(frozen_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),inputs=records),indent=2)+'\n').encode())
    print(json.dumps(state),flush=True)
if __name__=='__main__':
    if sys.argv[1]=='extract':
        package='/mnt/c/Users/malla/AppData/Local/CFDWorkbench/qualification/openfoam2512-tutorials_2512.0-2_all.deb'
        run('fixture-parent',WSL+['mkdir','-p','/root/CFDWorkbench/fixture-cache'])
        output=run('fixture-observed-sha256',WSL+['sha256sum',package])
        if output.split()[0]!='4c87494c17a1381853af8a828e8df3a741d9cde5e06bb0212f3fc6940d889473': raise SystemExit('package hash mismatch')
        run('fixture-extract',WSL+['dpkg-deb','-x',package,'/root/CFDWorkbench/fixture-cache'])
        run('fixture-locate',WSL+['find','/root/CFDWorkbench/fixture-cache','-path','*/icoFoam/cavity/cavity/system/blockMeshDict'])
    elif sys.argv[1]=='freeze': freeze()
    elif sys.argv[1]=='su2': su2_smoke()
    elif sys.argv[1]=='command': run(sys.argv[2],sys.argv[3:])
    elif sys.argv[1]=='cavity':
        state=json.loads((OUT/'run-paths.json').read_text())
        target=state['cavity_linux']
        rootlinux='/mnt/c/Projects/'+ROOT.name
        run('cavity-run-parent',WSL+['mkdir','-p',target+'/product-home/.OpenFOAM/2512'])
        run('cavity-stage-inputs',WSL+['cp','-a',rootlinux+'/docs/proof/win-routes/r133/cavity-inputs/.',target+'/'])
        run('cavity-stage-M1',WSL+['cp',rootlinux+'/docs/proof/win-routes/r133/product-controlDict',target+'/product-home/.OpenFOAM/2512/controlDict'])
        start=time.perf_counter()
        for app in ['blockMesh','checkMesh','icoFoam']:
            output=run('cavity-'+app,WSL+['/usr/bin/env','-i','PATH=/usr/bin:/bin','HOME='+target+'/product-home','USER=root','LOGNAME=root','LANG=C','/bin/bash','--noprofile','--norc',rootlinux+'/docs/proof/win-routes/r133/openfoam-command.sh',target,app],timeout=max(1,60-(time.perf_counter()-start)))
            if 'allowSystemOperations : Disallowing' not in output: raise SystemExit('M1 banner missing')
            run('cavity-resource-'+app,WSL+['cat',target+'/time.'+app])
        for t in ['0.1','0.2','0.3','0.4','0.5']:
            for field in ['U','p']: run('cavity-output-'+t+'-'+field,WSL+['sha256sum',target+'/'+t+'/'+field])
        print('CAVITY-PASS',flush=True)
