from pathlib import Path
import datetime, hashlib, json, subprocess, tempfile

root = Path.cwd()
proof = root / 'docs/proof/wri-r179'
rawdir = Path(tempfile.gettempdir()) / 'wri-r179-raw-command-metadata'
rawdir.mkdir(exist_ok=True)
home = str(Path.home()).encode()
records = []
for p in sorted(proof.glob('*.stderr.txt')):
    raw = p.read_bytes()
    clean = raw.replace(home, b'%USERPROFILE%')
    if raw != clean:
        (rawdir / p.name).write_bytes(raw)
        p.write_bytes(clean)
        records.append({'path': str(p.relative_to(root)).replace('\\', '/'), 'raw_sha256': hashlib.sha256(raw).hexdigest(), 'committed_sha256': hashlib.sha256(clean).hexdigest(), 'rule': 'exact user-home prefix -> %USERPROFILE%; derivative, not raw output'})
(proof / 'stderr-sanitization.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print('Sensitive stderr derivatives:', len(records))

subprocess.run(['git', 'add', '--', 'docs/proof/wri-r179', 'docs/plans/wri-r179.md'], check=True)
files = []
for p in sorted(proof.iterdir()):
    if not p.is_file() or p.name == 'capture-manifest.json':
        continue
    rel = str(p.relative_to(root)).replace('\\', '/')
    raw = subprocess.run(['git', 'show', ':' + rel], check=True, capture_output=True).stdout
    files.append({'path': rel, 'bytes': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()})
manifest = {'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'built_head': '13cb9a14c8315c1b01f30e33b672c936b5b36726', 'tested_head': None, 'algorithm': 'SHA-256', 'outcome': 'BLOCKED; no product checks executed', 'files': files}
(proof / 'capture-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
subprocess.run(['git', 'add', '--', 'docs/proof/wri-r179/capture-manifest.json'], check=True)
print('Capture manifest entries from staged blobs:', len(files))
