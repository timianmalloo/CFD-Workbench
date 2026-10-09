"""Prepare the unlaunched Ruling 79 case and commands; no mesh generation or solver launch."""
import datetime,hashlib,json,pathlib,subprocess,yaml
root=pathlib.Path(__file__).resolve().parents[3]; proof=pathlib.Path(__file__).resolve().parent
case=yaml.safe_load((root/'cases/spike04r3-g2-l3.yaml').read_text(encoding='utf-8'))
case['name']='win-spike04r3-g2-l3'
case['description']='Ruling 79 Windows L3 D4 freestream-start case prepared but NOT LAUNCHED; no 10-hour wall cap. A4 plus unchanged clause-5 extension; GCI follows admitted L5/L4/L3 only.'
case['geometry']['source']['generator']='python3 cases/tools/fetch-tmr-grid.py <dir> n0012familyII.3.p2dfmt.gz; cases/tools/run-tmr-case.sh cases/win-spike04r3-g2-l3.yaml <dir>'
case['purpose']['question']='Does the Ruling 79 uncapped Windows L3 D4 run meet A4 and clause-5 extension, admitting a later L5/L4/L3 triplet?'
case['backend']=yaml.safe_load((root/'cases/win-spike04r3-g0-l6.yaml').read_text(encoding='utf-8'))['backend']
case['numerics']['a4'].pop('wall_cap_s')
case['numerics']['a4']['stop_mechanism']='evidence-local a4-monitor-linux.py sends configured M1 signal30 (Linux SIGPWR); identical A4 calculations'
case['decomposition']['n_subdomains']=6
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
case['run_dir']='runs/'+stamp+'-win-spike04r3-g2-l3'
target=root/'cases/win-spike04r3-g2-l3.yaml'; target.write_bytes(yaml.safe_dump(case,sort_keys=False,allow_unicode=True).encode())
adapter=(proof/'of-command.sh').read_text(encoding='utf-8')
assert adapter.count('-np 2')==1 and adapter.count('/usr/bin/timeout --signal=TERM 3000s ')==1
(proof/'of-command-l3.sh').write_bytes(adapter.replace('-np 2','-np 6').replace('/usr/bin/timeout --signal=TERM 3000s ','').encode())
files=[target,root/'cases/spike04r3-g2-l3.yaml',proof/'sources/n0012familyII.3.p2dfmt.gz',root/'cases/tools/make-tmr-case.py',root/'cases/tools/foam-bundle/controlDict',proof/'a4-monitor-linux.py',proof/'of-command-l3.sh']
hashes={f.relative_to(root).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in files}
assert hashes['docs/proof/win-naca/sources/n0012familyII.3.p2dfmt.gz']==case['geometry']['source']['sha256']
(proof/'l3-prepared.json').write_bytes((json.dumps(dict(status='NOT LAUNCHED; waiting for coordinator start-note and heavy-slot gate',prepared_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),run=case['run_dir'],linux_run='/root/CFDWorkbench/'+case['run_dir'],n_ranks=6,nice=10,wall_cap_seconds=None,input_hashes=hashes),indent=2)+'\n').encode())
commands=f'''# Prepared commands only. Coordinator must post Ruling 79 start note and release the heavy slot first.
# 1. Generate the unlaunched mesh with the already installed Windows Python dependencies.
py -3 cases/tools/make-tmr-case.py cases/win-spike04r3-g2-l3.yaml {case['run_dir']} docs/proof/win-naca/sources/n0012familyII.3.p2dfmt.gz
# 2. Freeze generated cfdw-manifest.json and full tree hash, compare all source pins to l3-prepared.json.
#    If run timestamp is changed, re-freeze the Windows YAML before launch. Never overwrite an existing run.
# 3. Stage to private Linux filesystem; no launch occurs in these two commands.
wsl -d cfdw-openfoam2512 -u root -- mkdir -p /root/CFDWorkbench/{case['run_dir']}
wsl -d cfdw-openfoam2512 -u root -- cp -a /mnt/c/Projects/{root.name}/{case['run_dir']}/. /root/CFDWorkbench/{case['run_dir']}/
# 4. Only after start-note gate, launch supervisor detached from terminal. Substitute actual durable note reference.
wsl -d cfdw-openfoam2512 -u root -- env CFDW_START_NOTE_REF=<coordinator-note-reference> /bin/bash /mnt/c/Projects/{root.name}/docs/proof/win-naca/start-l3.sh /root/CFDWorkbench/{case['run_dir']}
# 5. Durable monitor/readback; do not launch another solver from this command.
wsl -d cfdw-openfoam2512 -u root -- tail -n 10 /root/CFDWorkbench/{case['run_dir']}/a4-monitor.log
wsl -d cfdw-openfoam2512 -u root -- tail -n 10 /root/CFDWorkbench/{case['run_dir']}/run-ledger.txt
# At supervisor exit: coordinator posts done/blocked after final A4 readback. Then admit triplet and run gci.py.
'''
(proof/'l3-commands.txt').write_bytes(commands.encode())
print(json.dumps(dict(run=case['run_dir'],status='PREPARED ONLY',input_hashes=hashes)))
