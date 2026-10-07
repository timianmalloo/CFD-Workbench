"""W-3 measured manual route helper; never elevates or unregisters a distro."""
import datetime, hashlib, json, os, pathlib, shutil, subprocess, sys, time, zipfile
ROOT = pathlib.Path(__file__).resolve().parents[3]
EVIDENCE = pathlib.Path(__file__).resolve().parent
CACHE = pathlib.Path(os.environ['LOCALAPPDATA']) / 'CFDWorkbench' / 'qualification'
CACHE.mkdir(parents=True, exist_ok=True)
def run(tag, argv, timeout=600, cwd=None):
    start = datetime.datetime.now(datetime.timezone.utc).isoformat()
    clock = time.perf_counter()
    result = subprocess.run(argv, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=timeout, cwd=cwd)
    raw = result.stdout
    output = raw.decode('utf-16-le' if b'\x00' in raw[:100] else 'utf-8', errors='replace')
    (EVIDENCE / (tag + '.txt')).write_text(output, encoding='utf-8')
    record = dict(tag=tag, argv=argv, started_utc=start, duration_seconds=time.perf_counter()-clock,
                  exit_code=result.returncode, output=tag+'.txt', cpu_ram='Not recorded')
    with (EVIDENCE/'steps.jsonl').open('a', encoding='utf-8') as stream:
        stream.write(json.dumps(record)+'\n')
    print(json.dumps(record), flush=True)
    print(output[-1500:], flush=True)
    return result.returncode
def download(tag, url, name, expected):
    target = CACHE/name
    if run(tag, ['curl.exe','--fail','--location','--output',str(target),url]):
        raise SystemExit('download failed')
    actual = hashlib.file_digest(target.open('rb'),'sha256').hexdigest()
    (EVIDENCE/(tag+'-hash.txt')).write_text(f'{target.name} bytes={target.stat().st_size} sha256={actual}\nexpected={expected}\n',encoding='utf-8')
    if actual != expected: raise SystemExit('hash mismatch')
    return target
if sys.argv[1] == 'base':
    target = download('ubuntu-download','https://releases.ubuntu.com/noble/ubuntu-24.04.5-wsl-amd64.wsl',
                      'ubuntu-24.04.5-wsl-amd64.wsl','bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e')
    run('ubuntu-import',['wsl.exe','--import','cfdw-openfoam2512',str(CACHE/'wsl'/'cfdw-openfoam2512'),str(target),'--version','2'])
elif sys.argv[1] == 'su2':
    target = download('su2-download','https://github.com/su2code/SU2/releases/download/v8.5.0/SU2-v8.5.0-win64-omp.zip',
                      'SU2-v8.5.0-win64-omp.zip','4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11')
    dest = CACHE/'su2-8.5.0'
    with zipfile.ZipFile(target) as z: z.extractall(dest)
    for binary in dest.rglob('SU2_CFD.exe'):
        print(binary,hashlib.file_digest(binary.open('rb'),'sha256').hexdigest())
        run('su2-identify',[str(binary),'--help'])
elif sys.argv[1] == 'command':
    sys.exit(run(sys.argv[2],sys.argv[3:]))
elif sys.argv[1] == 'su2-smoke':
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    dest = ROOT/'runs'/(stamp+'-win-su2-smoke')
    dest.mkdir(parents=True)
    for name in ['su2-smoke.cfg','su2-cylinder.su2']: shutil.copy2(EVIDENCE/name,dest/name)
    binary = CACHE/'su2-8.5.0'/'bin'/'bin'/'SU2_CFD.exe'
    status=run('su2-smoke',[str(binary),'-t','1','su2-smoke.cfg'],60,cwd=dest)
    (EVIDENCE/'su2-run-dir.txt').write_text(str(dest),encoding='utf-8')
    if (dest/'history.csv').exists(): shutil.copy2(dest/'history.csv',EVIDENCE/'su2-history.csv')
    sys.exit(status)
