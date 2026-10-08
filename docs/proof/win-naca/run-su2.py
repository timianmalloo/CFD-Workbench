"""Run the frozen native serial SU2 TMR fixture with measured process cost and immediate raw archive."""
import ctypes,datetime,hashlib,json,os,pathlib,shutil,subprocess,time
from ctypes import wintypes
root=pathlib.Path(__file__).resolve().parents[3]; proof=pathlib.Path(__file__).resolve().parent
state=json.loads((proof/'su2-frozen.json').read_bytes()); dest=root/state['run']; dest.mkdir(parents=True)
for row in state['files']:
    f=root/row['path']; assert hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256']
for name in ['tmr-sa.cfg','tmr-familyII-L6.su2']: shutil.copyfile(proof/'su2-inputs'/name,dest/name)
binary=pathlib.Path(os.environ['LOCALAPPDATA'])/'CFDWorkbench/qualification/su2-8.5.0/bin/bin/SU2_CFD.exe'
assert hashlib.sha256(binary.read_bytes()).hexdigest()=='3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248'
class Counters(ctypes.Structure):
    _fields_=[('cb',wintypes.DWORD),('PageFaultCount',wintypes.DWORD)]+[(name,ctypes.c_size_t) for name in ['PeakWorkingSetSize','WorkingSetSize','QuotaPeakPagedPoolUsage','QuotaPagedPoolUsage','QuotaPeakNonPagedPoolUsage','QuotaNonPagedPoolUsage','PagefileUsage','PeakPagefileUsage']]
memory=ctypes.WinDLL('psapi',use_last_error=True).GetProcessMemoryInfo
memory.argtypes=[wintypes.HANDLE,ctypes.POINTER(Counters),wintypes.DWORD]; memory.restype=wintypes.BOOL
kernel=ctypes.WinDLL('kernel32',use_last_error=True)
times=kernel.GetProcessTimes; times.argtypes=[wintypes.HANDLE]+[ctypes.POINTER(wintypes.FILETIME)]*4; times.restype=wintypes.BOOL
priority=kernel.GetPriorityClass; priority.argtypes=[wintypes.HANDLE]; priority.restype=wintypes.DWORD
argv=[str(binary),'-t','1','tmr-sa.cfg']; started=datetime.datetime.now(datetime.timezone.utc).isoformat(); clock=time.perf_counter()
peak=None; samples=0; timed_out=False
with (dest/'log.SU2_CFD').open('wb') as log:
    process=subprocess.Popen(argv,cwd=dest,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS)
    observed_priority=priority(int(process._handle))
    while process.poll() is None:
        values=Counters(); values.cb=ctypes.sizeof(values)
        if memory(int(process._handle),ctypes.byref(values),values.cb): peak=max(peak or 0,values.PeakWorkingSetSize); samples+=1
        if time.perf_counter()-clock>6900: timed_out=True; process.kill(); process.wait(); break
        time.sleep(.1)
    fields=[wintypes.FILETIME() for _ in range(4)]; cpu='Not recorded'
    if times(int(process._handle),*[ctypes.byref(f) for f in fields]): cpu=sum((f.dwHighDateTime<<32)+f.dwLowDateTime for f in fields[2:])/1e7
    row=dict(argv=argv,run=state['run'],started_utc=started,elapsed_seconds=time.perf_counter()-clock,exit_code=process.returncode,timeout=timed_out,timeout_seconds=6900,cpu_seconds=cpu,sampled_peak_working_set_bytes=peak or 'Not recorded',memory_samples=samples,priority_class=observed_priority,binary_sha256=hashlib.sha256(binary.read_bytes()).hexdigest())
archive=proof/'su2-results'; archive.mkdir()
for name in ['log.SU2_CFD','history.csv']:
    if (dest/name).is_file(): shutil.copyfile(dest/name,archive/name)
(archive/'resources.json').write_bytes((json.dumps(row,indent=2)+'\n').encode())
print(json.dumps(row))
print((archive/'log.SU2_CFD').read_bytes().decode('utf-8',errors='replace')[-4000:])
raise SystemExit(process.returncode)
