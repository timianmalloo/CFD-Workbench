"""Evidence-only LF and staged-blob control for the W-3 frozen inputs."""
import hashlib,json,pathlib,subprocess,sys,yaml
root=pathlib.Path(__file__).resolve().parents[3]
proof=root/'docs/proof/win-routes'
def check(data,expected):
    return b'\r' not in data and hashlib.sha256(data).hexdigest()==expected
if '--self-test' in sys.argv:
    data=b'NDIME= 2\n'
    expected=hashlib.sha256(data).hexdigest()
    assert check(data,expected)
    assert not check(data.replace(b'\n',b'\r\n'),expected)
    assert not check(data+b'corrupt',expected)
    print('PASS: LF accepted; planted CRLF and changed hash rejected')
    sys.exit(0)
records=json.loads((proof/'r133/frozen-before-launch.json').read_text())['inputs']
for row in records:
    p=root/row['path']
    blob=subprocess.run(['git','show',':'+row['path']],cwd=root,check=True,stdout=subprocess.PIPE).stdout
    if not check(p.read_bytes(),row['sha256']) or blob!=p.read_bytes(): raise SystemExit('FAIL '+row['path'])
su2=yaml.safe_load((root/'cases/win-su2-smoke.yaml').read_text())
assert check((proof/'su2-cylinder.su2').read_bytes(),su2['geometry']['source']['sha256'])
assert check((proof/'su2-smoke.cfg').read_bytes(),su2['numerics']['config_sha256'])
assert b'\r' not in (proof/'su2-history.csv').read_bytes()
print(f'PASS: {len(records)} frozen files match staged blobs and LF hashes; SU2 YAML hashes match; history is LF')
