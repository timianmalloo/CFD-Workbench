"""Offline epoch-expiry refusal controls; no live process boundary is called."""
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'docs/proof/win-gpu-f1-preflight/packet'))
import attempt
import contract

folder = root / attempt.PREFIX / 'attempt-1'
raw = (folder / 'invocation-contract.json').read_bytes()
document = json.loads(raw)
sha = hashlib.sha256(raw).hexdigest()
proposed = json.loads((folder / 'attempt-authorization.json').read_bytes())
projection_path = folder / 'authorization.accepted-projection.json'
projection_raw = projection_path.read_bytes()
projection = json.loads(projection_raw)

def refuses(label, action):
    try:
        action()
    except ValueError:
        print('REFUSED ' + label)
    else:
        raise AssertionError(label + ' was admitted')

contract.check_contract_fields(document)
contract.check_live_leader({'leader': 'mac', 'epoch': '8', 'state': 'live'}, 8)
for field, old in (('leader_epoch', 7), ('run_id', 'r225-attempt1-epoch7-20261012')):
    altered = copy.deepcopy(document)
    altered[field] = old
    refuses('expired ' + field, lambda: contract.check_contract_fields(altered))
for observed in ({'leader': 'mac', 'epoch': '7', 'state': 'live'},
                 {'leader': 'mac', 'epoch': '8', 'state': 'expired'}):
    refuses('stale live leader ' + str(observed), lambda: contract.check_live_leader(observed, 8))
refuses('proposed authorization', lambda: attempt.check_authorization(proposed, document, sha))
refuses('projection as live path', lambda: attempt.verify_authorization(
    root, projection_path, document, sha, document['run_id'], document['run_id']))
rulings = subprocess.run(['git', '-C', str(root), 'show',
    '361af05a442b10307dff5748d27a643145bd84b5:docs/notes/rulings.md'], check=True, capture_output=True,
    text=True, encoding='utf-8').stdout
refuses('old R227 cannot authorize epoch8 projection', lambda: attempt.check_ruling(
    rulings, projection, hashlib.sha256(projection_raw).hexdigest()))
for name in ('live', 'checkpoint', 'final'):
    assert not (folder / name).exists(), 'unexpected attempt output: ' + name
print('EPOCH8-REVIEW-PASS proposed remains non-live; no WSL invocation')
