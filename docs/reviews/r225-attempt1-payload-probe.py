"""Independent, read-only payload/source/input hash review. Never launches WSL."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
PACKET = ROOT / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(PACKET))
import attempt
import contract

source = sys.argv[1]
root = ROOT / attempt.PREFIX / 'attempt-1'
path = root / 'invocation-contract.json'
raw = path.read_bytes()
document = json.loads(raw)
assert document['accepted_packet_commit'] == source, 'payload source anchor is stale or altered'
contract.check_contract_fields(document)
sha = hashlib.sha256(raw).hexdigest()
proposed_path = root / 'attempt-authorization.json'
projection_path = root / 'authorization.accepted-projection.json'
proposed = json.loads(proposed_path.read_bytes())
projection = json.loads(projection_path.read_bytes())
assert proposed['status'] == 'proposed' and proposed['ruling'] is None
attempt.check_authorization(proposed, document, sha, proposed=True)
attempt.check_authorization(projection, document, sha)
assert {key for key in proposed if proposed[key] != projection[key]} == {'status', 'ruling'}
actual = {p.relative_to(ROOT).as_posix() for p in PACKET.iterdir() if p.is_file()}
assert actual == set(document['packet_sha256'])
def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, check=True).stdout
assert set(git('ls-tree', '-r', '--name-only', source, '--', contract.PACKET).decode().splitlines()) == actual
for field in ('packet_sha256', 'external_sha256'):
    for name, digest in document[field].items():
        contract.check_hash(digest, (ROOT / name).read_bytes())
        contract.check_hash(digest, git('show', source + ':' + name))
for name, row in document['inputs'].items():
    input_path = Path(row['path']).resolve(strict=True)
    assert str(input_path).casefold() == row['path'].casefold()
    contract.check_file_hash(row['sha256'], input_path)
    if name in ('b2_lock', 'b2_licenses'):
        relative = 'docs/proof/win-gpu-b2/' + ('package-lock.json' if name == 'b2_lock' else 'license-map.json')
        contract.check_hash(row['sha256'], (ROOT / relative).read_bytes())
print('PAYLOAD-REVIEW-PASS packet=%d external=1 inputs=7 live_authority=False' % len(actual))
for label, item in (('contract', path), ('proposed', proposed_path), ('accepted_projection', projection_path)):
    print(label + '_sha256=' + hashlib.sha256(item.read_bytes()).hexdigest())
