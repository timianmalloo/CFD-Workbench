"""Read-only activation proof; production contract verifier never invokes WSL."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
packet = root / 'docs/proof/win-gpu-f1-preflight/packet'
sys.path.insert(0, str(packet))
import attempt
import contract

activation = '2abcd2a26e349ee6a2d58127195158f0b9d5e19e'
ruling_commit = '7a25122f65893d9b2357bedd668e016688d2514c'
def git(*args):
    return subprocess.run(['git', '-C', str(root), *args], capture_output=True, check=True).stdout
for ancestor in ('2cb40b67587e3ce5a90b5e8d0a65d9cd367450cb',
                 'c9c7af0ea9d6361cda5934884a2ed9db47ada787',
                 'df21decf4ed90fcedaecceec449c6edcd87e2179',
                 'b7821e27d75a0a5ffed5ee3f9d36c3bf08e535a8', ruling_commit):
    git('merge-base', '--is-ancestor', ancestor, activation)
folder = root / attempt.PREFIX / 'attempt-1'
path = folder / 'invocation-contract.json'
document = json.loads(path.read_bytes())
auth = (folder / 'attempt-authorization.json').read_bytes()
assert auth == (folder / 'authorization.accepted-projection.json').read_bytes()
digest = hashlib.sha256(auth).hexdigest()
assert digest == '39217fae100c358c1c98aba5e1827e23cb0e0824d2be9329f98c339a8275dff3'
attempt.check_authorization(json.loads(auth), document, hashlib.sha256(path.read_bytes()).hexdigest())
attempt.check_ruling(git('show', ruling_commit + ':docs/notes/rulings.md').decode('utf-8'), json.loads(auth), digest)
sample = json.loads((folder / 'leader-activation.json').read_bytes())
assert sample['state'] == 'live' and sample['leader'] == 'mac' and sample['epoch'] == 8
assert isinstance(sample['expires_in'], (int, float)) and sample['expires_in'] >= 200
record = json.loads(git('cat-file', '-p', sample['oid']))
assert all(sample[key] == value for key, value in record.items())
assert 0 < sample['expires_in'] <= record['ttl']
changed = git('diff-tree', '--no-commit-id', '--name-only', '-r', activation).decode().splitlines()
assert set(changed) == {attempt.PREFIX + '/attempt-1/attempt-authorization.json',
                        attempt.PREFIX + '/attempt-1/leader-activation.json'}
execution_repo = Path(document['inputs']['b2_lock']['path']).parents[3]
execution_head = subprocess.run(['git', '-C', str(execution_repo), 'rev-parse', 'HEAD'],
    capture_output=True, check=True, text=True, encoding='utf-8').stdout.strip()
assert execution_head == activation, 'execution worktree moved from reviewed activation'
args = [sys.executable, str(packet / 'contract.py'), 'verify', '--repo', str(execution_repo),
    '--contract', str(execution_repo / attempt.PREFIX / 'attempt-1/invocation-contract.json'),
    '--authorization', str(execution_repo / attempt.PREFIX / 'attempt-1/attempt-authorization.json'),
    '--branch', document['execution_branch'], '--session', document['session'],
    '--run-id', document['run_id'], '--exclusive-run-id', document['run_id']]
for key in ('rootfs', 'ubuntu_sums', 'ubuntu_signature', 'openfoam_keyring', 'nvidia_keyring'):
    args.extend(['--' + key.replace('_', '-'), document['inputs'][key]['path']])
subprocess.run(args, check=True)
print('ACTIVATION-REVIEW-PASS ancestry/ruling/bytes/sample; activation_TTL=' + str(sample['expires_in']))
print('No WSL invocation; exclusive RunId is a verifier input, not independent machine-exclusivity proof.')
