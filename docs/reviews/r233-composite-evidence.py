"""Read-only composite and original-ring binding; output is sanitized metadata."""
import ast
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HEAD = 'b74af1e488e7d7bd3fd7c7901d2940da10fa4c07'
BASE = '37b96e4749a13d2fee3a7d73444d66a6a39a6403'
READER = '70d022bf4369c8cb58888f88e6efba00070f35db'
PREFIX = 'docs/proof/win-gpu-f1-preflight/'
TARGETS = {PREFIX+'packet/Invoke-Preflight.ps1', PREFIX+'packet/SelfTest.ps1', 'tools/check-reader-sharing.py', 'tools/check-docs.py'}
METADATA = {'docs/audit/audit-log.jsonl','docs/audit/audit-data.js','docs/docs-index.js','docs/lessons/defect-classes.md'}


def git(*args):
    return subprocess.run(['git',*args],cwd=ROOT,capture_output=True,check=True).stdout


def blob(ref,path):
    return git('cat-file','blob',ref+':'+path)


def tree(ref):
    return {row.split(b'\t',1)[1].decode():row.split(b'\t',1)[0].decode() for row in git('ls-tree','-rz',ref).split(b'\0') if row}


def assignments(raw,name):
    return ast.literal_eval(next(node.value for node in ast.parse(raw).body if isinstance(node,ast.Assign)
        and any(isinstance(target,ast.Name) and target.id==name for target in node.targets)))


def main():
    assert git('rev-list','--parents','-n','1',HEAD).decode().split()==[HEAD,BASE]
    before,after=tree(BASE),tree(HEAD)
    protected=set(before)-TARGETS-METADATA
    assert len(protected)==5379
    assert all(after.get(path)==before[path] for path in protected)
    assert set(after)-set(before)=={PREFIX+'composite-r233.md',PREFIX+'composite-r233-conservation.json',PREFIX+'verify_composite_r233.py'}
    audit='docs/audit/audit-log.jsonl'
    old_audit,new_audit=blob(BASE,audit),blob(HEAD,audit)
    assert new_audit.startswith(old_audit)
    old_rows=[json.loads(line) for line in old_audit.splitlines()]
    new_rows=[json.loads(line) for line in new_audit.splitlines()]
    assert len(old_rows)==1087 and len(new_rows)==1088 and len({row['id'] for row in new_rows})==1088
    register='docs/lessons/defect-classes.md'
    old_lines=blob(BASE,register).splitlines()
    iterator=iter(blob(HEAD,register).splitlines())
    assert len(old_lines)==2897 and all(any(candidate==line for candidate in iterator) for line in old_lines)
    scanner='tools/check-reader-sharing.py'
    old_entries=assignments(blob(BASE,scanner),'PS_GRANDFATHERED')
    new_entries=assignments(blob(HEAD,scanner),'PS_GRANDFATHERED')
    removed=old_entries-new_entries
    assert len(removed)==2 and len(new_entries)==7 and not new_entries-old_entries
    assert all(path==PREFIX+'packet/Invoke-Preflight.ps1' for path,digest in removed)
    proof=json.loads(blob(HEAD,PREFIX+'composite-r233-conservation.json'))
    for row in proof['files']:
        raw=blob(HEAD,row['path'])
        assert raw==(ROOT/row['path']).read_bytes()
        assert len(raw)==row['bytes'] and hashlib.sha256(raw).hexdigest()==row['sha256']
        assert after[row['path']].split()[2]==row['composite_blob']
    patch=git('diff',READER+'^',READER,'--',*sorted(TARGETS))
    assert hashlib.sha256(patch).hexdigest()==proof['patch_sha256']
    for path in TARGETS & {'tools/check-reader-sharing.py','tools/check-docs.py'}:
        assert after[path]==tree(READER)[path]
    print('CONSERVATION-PASS protected_modes_and_blobs=5379 raw_audit_prefix=1087 register_ordered_lines=2897 removed_exemptions=2 retained=7')
    capture=ROOT.parent/'CFD-Workbench-win-gpu-f1-r233-repairs-integration'/'.tmp-tests'
    spec=importlib.util.spec_from_file_location('r233_classifier',ROOT/'tools/check-expected-failures.py')
    classifier=importlib.util.module_from_spec(spec); spec.loader.exec_module(classifier)
    manifest=classifier.load_manifest(ROOT/'tests/expected-failures.windows.json')
    counts={'Core.part1of3':(346,9),'Core.part2of3':(210,7),'Core.part3of3':(205,11),'Desktop':(776,33),
            'Analysis.part1of3':(44,0),'Analysis.part2of3':(69,0),'Analysis.part3of3':(146,0),'Cli':(18,2)}
    ring={'suites':{},'file_bindings':{}}
    for name,pair in counts.items():
        raw=(capture/(name+'.log')).read_bytes(); text=raw.decode('utf-8').replace('\r\n','\n')
        result=classifier.classify(text,manifest,True)
        observed=(len(re.findall(r'^PASS ',text,re.M)),len(result['expected']))
        assert observed==pair and not result['unexpected'] and not result['unexpected_pass']
        ring['suites'][name]={'passes':pair[0],'expected_classifications':pair[1],'unexpected':0,'milliseconds':int((capture/(name+'.ms')).read_text())}
    for name in [value+suffix for value in counts for suffix in ('.log','.ms','.seconds')]+['wall.ms','build.ms','times.txt','python-unittest.out','r233-functional-ring.txt']:
        raw=(capture/name).read_bytes()
        ring['file_bindings'][name]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    desktop=(capture/'Desktop.log').read_text(encoding='utf-8')
    child_rows=re.findall(r'^SUITE (.+) exit [01]$',desktop,re.M)
    source=(ROOT/'tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs').read_text(encoding='utf-8')
    block=source.split('int spawnedExit =',1)[1].split(');',1)[0]
    declared=re.findall(r'"(--[^"]+)"',block)
    assert len(child_rows)==len(set(child_rows))==18 and set(child_rows)==set(declared)
    times=dict((name,float(seconds)) for name,seconds in re.findall(r'^SUITE-TIME (.+) ([\d.]+) s$',desktop,re.M))
    assert set(times)==set(declared) and max(times.values())<120
    previous=0; families={}
    for match in re.finditer(r'^SUITE (.+) exit [01]$',desktop,re.M):
        part=re.search(r'(--[a-z-]+) --part=(\d+)/(\d+)',match[1])
        if part:
            rows=re.findall(r'^PARTITION (\d+)/(\d+) of (\d+) checks$',desktop[previous:match.start()],re.M)
            assert len(rows)==1 and rows[0][:2]==(part[2],part[3])
            families.setdefault(part[1],[]).append(tuple(map(int,rows[0])))
        previous=match.end()
    assert len(families)==7 and sum(map(len,families.values()))==15
    for parts in families.values():
        assert len({(total,checks) for part,total,checks in parts})==1
        assert sorted(part for part,total,checks in parts)==list(range(1,parts[0][1]+1))
    python=(capture/'python-unittest.out').read_text(encoding='utf-8')
    assert 'Ran 94 tests' in python and python.rstrip().endswith('OK')
    wall=int((capture/'wall.ms').read_text()); build=int((capture/'build.ms').read_text())
    assert (wall,wall-build)==(210689,194520)
    telemetry_basename='rollout-2026-10-12T20-35-42-01a13354-8f61-7582-9b9a-13b253936fd9.jsonl'
    paths=list((Path.home()/'.codex/sessions').rglob(telemetry_basename)); assert len(paths)==1
    raw_line=paths[0].read_bytes().splitlines()[719]
    item=json.loads(raw_line)['payload']['item']; output=item['aggregated_output']
    assert item['exit_code']==0 and item['status']=='completed'
    assert isinstance(item['command'],list) and any('join-ring.sh' in argument for argument in item['command'])
    assert '210689 ms' in output and 'all test harnesses passed' in output
    assert 'test costs: 0 failures, 14 COST-MISS (load 13.97)' in output
    allowed=[]
    for line in output.splitlines():
        if line.startswith(('test costs:','wall ','all test harnesses passed')): allowed.append(line)
        elif line.startswith('COST-MISS TEST-BUDGET '): allowed.append(line.split(' host ')[0]+' host [redacted]')
    ring.update({'desktop_children':18,'desktop_partitions':15,'max_child_seconds':max(times.values()),'python_tests':94,
                 'wall_ms':wall,'build_ms':build,'net_ms':wall-build,'original_exit':0,'cost_failures':0,'cost_misses':14,
                 'telemetry':{'source_basename':telemetry_basename,'line_number':720,'json_path':'$.payload.item.aggregated_output',
                              'raw_line_sha256':hashlib.sha256(raw_line).hexdigest(),'output_utf8_sha256':hashlib.sha256(output.encode()).hexdigest(),
                              'allowlisted_lines':allowed}})
    receipt={'candidate':HEAD,'sole_parent':BASE,'reader_delta_origin':READER,'protected_modes_and_blobs':5379,
             'parent_audit_raw_prefix_entries':1087,'parent_register_ordered_lines':2897,'source_bindings':proof['files'],
             'patch_sha256':proof['patch_sha256'],'ring':ring,'privacy':'Only metadata/hashes/allowlisted verdicts; no raw private text or absolute capture path.'}
    (ROOT/'docs/reviews/r233-composite-evidence.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('RING-EVIDENCE-PASS original_exit=0 Desktop=776/33expected/0unexpected children=18 partitions=15 Python=94 wall_ms=210689 cost_misses=14')


if __name__=='__main__':
    main()
