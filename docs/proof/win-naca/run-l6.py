"""Run exactly the frozen source-bound G0 case and archive its raw evidence."""
import json,pathlib,shutil,subprocess
root=pathlib.Path(__file__).resolve().parents[3]
proof=pathlib.Path(__file__).resolve().parent
state=json.loads((proof/'l6-frozen.json').read_bytes())
linux=state['linux_run']
mnt='/mnt/c/Projects/'+root.name
wsl=['wsl','-d','cfdw-openfoam2512','-u','root','--']
def run(tag,argv):
    subprocess.run(['py','-3',str(proof/'measure.py'),tag,*argv],cwd=root,check=True)
assert not (root/state['run']).exists()
run('l6-stage-parent',wsl+['mkdir','-p',linux])
run('l6-stage-inputs',wsl+['cp','-a',mnt+'/docs/proof/win-naca/l6-inputs/.',linux+'/'])
run('l6-tree-init',wsl+['python3',mnt+'/cases/tools/launcher-record.py','init',linux])
monitor=(root/'cases/tools/a4-monitor.py').read_text(encoding='utf-8')
# The unchanged M1 stop signal is 30; Linux SIGUSR1 is 10. Send configured 30 instead of the macOS alias.
assert monitor.count('os.kill(int(p), signal.SIGUSR1)')==1
(proof/'a4-monitor-linux.py').write_bytes(monitor.replace('os.kill(int(p), signal.SIGUSR1)','os.kill(int(p), 30)').encode())
script=mnt+'/docs/proof/win-naca/of-command.sh'
for app,args in [('checkMesh',[]),('decomposePar',['-force'])]:
    run('l6-'+app,wsl+['env','-i','PATH=/usr/bin:/bin','HOME=/root','USER=root','LOGNAME=root','LANG=C','bash','--noprofile','--norc',script,linux,app,app,*args])
with (proof/'l6-monitor-stdout.txt').open('wb') as stream:
    watcher=subprocess.Popen(wsl+['python3',mnt+'/docs/proof/win-naca/a4-monitor-linux.py','watch',linux,'simpleFoam',mnt+'/cases/win-spike04r3-g0-l6.yaml'],stdout=stream,stderr=subprocess.STDOUT,cwd=root)
    try:
        run('l6-simpleFoam',wsl+['env','-i','PATH=/usr/bin:/bin','HOME=/root','USER=root','LOGNAME=root','LANG=C','bash','--noprofile','--norc',script,linux,'simpleFoam','simpleFoam'])
        watcher.wait(timeout=180)
    finally:
        if watcher.poll() is None: watcher.terminate(); watcher.wait()
run('l6-reconstructPar',wsl+['env','-i','PATH=/usr/bin:/bin','HOME=/root','USER=root','LOGNAME=root','LANG=C','bash','--noprofile','--norc',script,linux,'reconstructPar','reconstructPar','-latestTime'])
run('l6-convergence',wsl+['python3',mnt+'/docs/proof/win-naca/a4-monitor-linux.py','report',linux,'simpleFoam',mnt+'/cases/win-spike04r3-g0-l6.yaml'])
archive=proof/'l6-results'
archive.mkdir()
for name in ['log.checkMesh','log.decomposePar','log.simpleFoam','log.reconstructPar','time.checkMesh','time.decomposePar','time.simpleFoam','time.reconstructPar','run-ledger.txt','a4-stop.txt']:
    subprocess.run(wsl+['cp',linux+'/'+name,mnt+'/docs/proof/win-naca/l6-results/'+name],cwd=root,check=True)
subprocess.run(wsl+['cp','-a',linux+'/postProcessing',mnt+'/docs/proof/win-naca/l6-results/'],cwd=root,check=True)
print('L6 raw evidence archived:',archive)
