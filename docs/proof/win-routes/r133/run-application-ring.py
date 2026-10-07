"""Run the authorized Windows ring once; archive raw scratch logs before returning."""
import datetime,hashlib,json,os,pathlib,shutil,subprocess,time
root=pathlib.Path(__file__).resolve().parents[4]
out=pathlib.Path(__file__).resolve().parent/'raw-ring'
out.mkdir(exist_ok=False)
out.joinpath('.gitattributes').write_bytes(b'*.log -text\n*.txt -text\n')
bash=pathlib.Path('C:/Program Files/Git/bin/bash.exe')
assert bash.is_file(),bash
sdk='C:\\Users\\malla\\.dotnet'
env=os.environ.copy()
env['DOTNET_ROOT']=sdk
env['PATH']=sdk+os.pathsep+env['PATH']
env['CFD_TEST_BUDGET_SECONDS']='60'
sha=subprocess.run(['git','rev-parse','HEAD'],cwd=root,check=True,stdout=subprocess.PIPE,text=True).stdout.strip()
sdkresult=subprocess.run([str(pathlib.Path(sdk)/'dotnet.exe'),'--version'],env=env,cwd=root,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
out.joinpath('sdk.txt').write_bytes(sdkresult.stdout)
assert sdkresult.returncode==0,sdkresult.stdout
started=datetime.datetime.now(datetime.timezone.utc).isoformat()
clock=time.perf_counter()
argv=[str(bash),'--noprofile','--norc','tools/run-tests.sh']
with out.joinpath('ring.stdout.txt').open('wb') as stream:
    result=subprocess.run(argv,cwd=root,env=env,stdout=stream,stderr=subprocess.STDOUT,timeout=180)
elapsed=time.perf_counter()-clock
# First action after the ring returns: preserve its logs/clocks before another run can clean scratch.
scratch=root/'.tmp-tests'
archived=[]
for pattern in ['*.log','*.seconds','*.ms','times.txt']:
    for source in sorted(scratch.glob(pattern)):
        dest=out/source.name
        shutil.copyfile(source,dest)
        archived.append(dict(name=source.name,bytes=dest.stat().st_size,sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
row=dict(tested_sha=sha,started_utc=started,argv=argv,dotnet_root=sdk,sdk=sdkresult.stdout.decode(errors='replace').strip(),budget_seconds=60,exit_code=result.returncode,wrapper_wall_seconds=elapsed,archived_raw_files=archived)
out.joinpath('run.json').write_bytes((json.dumps(row,indent=2)+'\n').encode())
print(json.dumps({k:v for k,v in row.items() if k!='archived_raw_files'}),flush=True)
print(f'Archived {len(archived)} raw log/clock files',flush=True)
print(out.joinpath('ring.stdout.txt').read_text(errors='replace')[-12000:],flush=True)
