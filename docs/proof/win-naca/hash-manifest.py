"""Bind evidence bytes to the staged or committed blobs; exclude only the manifest itself."""
import hashlib, json, pathlib, subprocess, sys

root = pathlib.Path(__file__).resolve().parents[3]
proof = pathlib.Path(__file__).resolve().parent
manifest = proof / 'hash-manifest.json'
files = sorted([p for p in proof.rglob('*') if p.is_file() and p != manifest and '__pycache__' not in p.parts] +
    [root / 'cases' / n for n in ['win-spike04r3-g0-l6.yaml', 'win-su2-tmr-naca0012.yaml', 'win-spike04r3-g2-l3.yaml']])
ref = 'HEAD:' if '--head' in sys.argv else ':'
records = []
for path in files:
    rel = path.relative_to(root).as_posix()
    data = path.read_bytes()
    blob = subprocess.run(['git', 'show', ref + rel], cwd=root, check=True, stdout=subprocess.PIPE).stdout
    assert data == blob, rel
    records.append(dict(path=rel, bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
state = dict(source_sha='fd96651e6dd3ad9c01e4fbaa07c62a11fa553c09',
    scope='Observed owned evidence bytes equal git blobs; historical launch hashes preserved separately; no L3 launch', files=records)
if '--head' in sys.argv:
    assert json.loads(manifest.read_bytes()) == state
    print(f'HEAD manifest verified: {len(records)} files')
else:
    manifest.write_bytes((json.dumps(state, indent=2) + '\n').encode())
    print(f'Staged manifest written: {len(records)} files')
