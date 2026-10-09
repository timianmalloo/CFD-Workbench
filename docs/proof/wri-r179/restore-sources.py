from pathlib import Path
import hashlib, json, subprocess, tempfile

root = Path.cwd()
proof = root / 'docs/proof/wri-r179'
backup = Path(tempfile.gettempdir()) / 'wri-r179-originals'
records = json.loads((proof / 'source-hashes.json').read_text(encoding='utf-8'))
for record in records:
    p = root / record['path']
    raw = (backup / p.name).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == record['original_sha256']
    p.write_bytes(raw)
    record['restored_sha256'] = hashlib.sha256(p.read_bytes()).hexdigest()
    assert record['restored_sha256'] == record['original_sha256']
(proof / 'source-restoration.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
result = subprocess.run(['git', 'diff', '--exit-code', '--', 'src', 'tests', 'tools'], capture_output=True)
print('SOURCE_RESTORATION byte-for-byte=PASS src/tests/tools-diff-exit=' + str(result.returncode))
assert result.returncode == 0, result.stdout

# Exact metadata remains local; committed command metadata substitutes only the home prefix.
raw_metadata = Path(tempfile.gettempdir()) / 'wri-r179-raw-command-metadata'
raw_metadata.mkdir(exist_ok=True)
home = str(Path.home())
sanitization = []
for path in sorted(proof.glob('*.command.txt')):
    raw = path.read_bytes()
    (raw_metadata / path.name).write_bytes(raw)
    clean = raw.replace(home.encode(), b'%USERPROFILE%')
    if clean != raw:
        path.write_bytes(clean)
        sanitization.append({'path': str(path.relative_to(root)).replace('\\', '/'), 'raw_sha256': hashlib.sha256(raw).hexdigest(), 'committed_sha256': hashlib.sha256(clean).hexdigest(), 'rule': 'exact user home prefix -> %USERPROFILE%; all stdout/stderr untouched'})
(proof / 'command-sanitization.json').write_text(json.dumps(sanitization, indent=2), encoding='utf-8')
print('RAW stdout/stderr untouched; command metadata home prefix sanitized:', len(sanitization))
