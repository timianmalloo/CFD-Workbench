"""Independent offline binding and registration-alias review; no WSL."""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
folder = root / 'docs/proof/win-gpu-f1-preflight/attempt-1/cleanup'
spec = importlib.util.spec_from_file_location('review_r229', folder / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)
payload = json.loads((folder / 'payload.json').read_bytes())
source = 'eb1e0e64c5fcbc9a77ea10d8593e99e2912a0c1f'
assert payload['source_commit'] == source
def git(*args):
    return subprocess.run(['git', '-C', str(root), *args], capture_output=True, check=True).stdout
git('merge-base', '--is-ancestor', source, '115f3b05f5b4b4f8d13c22e9a7b40e1fbc62c519')
for name, digest in payload['files'].items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == digest, name
    assert hashlib.sha256(git('show', source + ':' + name)).hexdigest() == digest, name
assert not (folder / 'authorization.json').exists()
proposed = json.loads((folder / 'authorization.proposed.json').read_bytes())
accepted = json.loads((folder / 'authorization.accepted-projection.json').read_bytes())
assert {key for key in proposed if proposed[key] != accepted[key]} == {'status', 'ruling'}
payload_sha = cleanup.digest(folder / 'payload.json')
cleanup.validate_authorization(accepted, payload_sha)
try:
    cleanup.validate_authorization(proposed, payload_sha)
except ValueError:
    pass
else:
    raise AssertionError('proposed authorization admitted')
print('BINDING-PASS source/files=' + str(len(payload['files'])) + ' canonical authorization absent')
for name in ('payload.json', 'authorization.proposed.json', 'authorization.accepted-projection.json'):
    print(name + '=' + cleanup.digest(folder / name))
if len(sys.argv) == 1:
    raise SystemExit(0)
rows = json.loads((root / cleanup.FAILURE_REL / 'coordinator/host-registrations-after-failure.json').read_bytes())
target, protected = cleanup.validate_identity(rows, [cleanup.TARGET, cleanup.PROTECTED], True, True)
accepted_cases = []
for label, path in (('target-path-alias', cleanup.TARGET_PATH), ('protected-path-alias', cleanup.PROTECTED_PATH)):
    alias = dict(name='unreviewed-alias', key='{11111111-2222-3333-4444-555555555555}', path=path, version=2)
    try:
        cleanup.validate_identity(rows + [alias], [cleanup.TARGET, cleanup.PROTECTED, alias['name']], True, True)
    except ValueError:
        pass
    else:
        accepted_cases.append(label)
    if label == 'protected-path-alias':
        try:
            cleanup.validate_absence([protected, alias], [cleanup.PROTECTED, alias['name']], False, True, protected)
        except ValueError:
            pass
        else:
            accepted_cases.append('protected-absence-alias')
assert not accepted_cases, 'REVIEW-VETO: ambiguous registration paths admitted: ' + ', '.join(accepted_cases)
print('ALIAS-REFUSAL-PASS')
