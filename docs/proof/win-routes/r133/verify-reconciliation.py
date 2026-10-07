"""Verify source adoption and semantic history preservation after the main merge."""
import json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[4]
def git(*argv):
    return subprocess.run(['git',*argv],cwd=root,check=True,stdout=subprocess.PIPE).stdout
def records(ref,path):
    return {json.dumps(json.loads(line),sort_keys=True) for line in git('show',ref+':'+path).decode('utf-8').splitlines() if line.strip()}
for path in ['docs/audit/audit-log.jsonl','docs/audit/change-log.jsonl','docs/coordination/xmsg.jsonl']:
    merged=records('HEAD',path)
    for ref in ['4dc1036fde42276310b14ca1dc1b9775159a9075','181600fa4b3275ffaaf80a746f5c8022840593a3']:
        missing=records(ref,path)-merged
        assert not missing,(ref,path,len(missing))
    print('PASS: both parent histories preserved semantically:',path)
paths=git('diff','--name-only','origin/main','--','src','tests','tools','schemas','cases/tools').decode().splitlines()
assert not paths,paths
print('PASS: product source/tests/tools/schema are byte-identical to origin/main')
diff=git('diff','origin/main','--','docs/design/guided-solver-setup.md').decode('utf-8')
changed=[line for line in diff.splitlines() if line.startswith(('+','-')) and not line.startswith(('+++','---'))]
assert len(changed)==6,changed
assert all(line.startswith(('+| Observed Windows route behavior','-| Anything that happens on Windows','+| OpenFOAM (both OSes)','-| OpenFOAM (both OSes)','+| SU2','-| SU2')) for line in changed),changed
print('PASS: guided setup differs only in the three observed Windows rows')
