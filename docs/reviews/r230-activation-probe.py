"""Read-only exact activation probe; never calls execute or pre-live/WSL methods."""
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
EXECUTION = ROOT.parent / 'CFD-Workbench-win-gpu-f1-r228-cleanup'
REL = Path('docs/proof/win-gpu-f1-preflight/attempt-1/cleanup')
HEAD = 'fd4f738ad01f6a4be89c92d844c5ef372c47ea07'
SOURCE = '6e788efbb7c2bc6f708927b183515c9ced60226d'
CANDIDATE = '2a7c269f08149af2bb6165e94249c7d25dcb46c2'
RULING_COMMIT = '2f22eea93b2f06388e4931b60aa6129a0f53fd4d'
MAIN = '04354ecf0b1cc6ac6fffcab16dba4e5b34b34014'

def git(*args, repo=ROOT):
    return subprocess.run(['git', '-C', str(repo), *args], capture_output=True, check=True).stdout

hashes = {
    'cleanup-authorization.json': '1ad90e081ac10fb1b2daded500a6dec4496e2546b349d053684377101b33fb8d',
    'payload.json': '3a431ac6707de7d452dda99d5fc1eef750b2245edd45ad3d9dd081559a2f0a89',
    'cleanup.py': '0241d5f5d4f7e6cfeda33581af51884dacfb54c2015f5a58484b7b2999d41e3a',
    'leader-activation.json': 'cd1dca8e45cdef55cec87a1a91c333415ceceb481f36385342769dbfc6788270',
}
for name, expected in hashes.items():
    relative = (REL / name).as_posix()
    data = (ROOT / relative).read_bytes()
    assert hashlib.sha256(data).hexdigest() == expected
    assert git('show', HEAD + ':' + relative) == data
    assert (EXECUTION / relative).read_bytes() == data
    print(name + '=' + expected)
for ancestor in (SOURCE, CANDIDATE, RULING_COMMIT, MAIN):
    git('merge-base', '--is-ancestor', ancestor, HEAD)
git('merge-base', '--is-ancestor', RULING_COMMIT, MAIN)
assert git('rev-parse', 'origin/main').decode().strip() == MAIN
assert git('rev-parse', 'origin/win/gpu-f1-r228-cleanup').decode().strip() == HEAD
assert git('rev-parse', 'HEAD', repo=EXECUTION).decode().strip() == HEAD
assert git('branch', '--show-current', repo=EXECUTION).decode().strip() == 'win/gpu-f1-r228-cleanup'
assert git('status', '--porcelain', repo=EXECUTION) == b''
changed = git('diff-tree', '--no-commit-id', '--name-only', '-r', RULING_COMMIT).decode().splitlines()
for relative in ('docs/notes/rulings.md', (REL / 'cleanup-authorization.json').as_posix(),
                 (REL / 'leader-activation.json').as_posix()):
    assert relative in changed
for name in ('cleanup-authorization.json', 'leader-activation.json'):
    assert git('show', RULING_COMMIT + ':' + (REL / name).as_posix()) == (ROOT / REL / name).read_bytes()
for name in ('cleanup-authorization.json', 'payload.json'):
    assert git('show', CANDIDATE + ':' + (REL / name).as_posix()) == (ROOT / REL / name).read_bytes()
spec = importlib.util.spec_from_file_location('review_activation_cleanup', ROOT / REL / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)
auth = json.loads((ROOT / cleanup.AUTH_REL).read_bytes())
cleanup.validate_authorization(auth, hashes['payload.json'])
sample = json.loads((ROOT / cleanup.ACTIVATION_SAMPLE_REL).read_bytes())
cleanup.validate_leader_sample(sample, 8)
assert math.isfinite(sample['expires_in']) and sample['expires_in'] >= 200
section = cleanup.ruling_section(git('show', MAIN + ':docs/notes/rulings.md').decode(), auth['ruling'])
cleanup.verify_ruling(section, hashes['cleanup-authorization.json'], hashes['payload.json'], SOURCE, auth['ruling'])
payload = json.loads((ROOT / cleanup.PAYLOAD_REL).read_bytes())
assert payload['source_commit'] == SOURCE and len(payload['files']) == 11
for relative, expected in payload['files'].items():
    assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == expected
    assert hashlib.sha256(git('show', SOURCE + ':' + relative)).hexdigest() == expected
history = ['docs/proof/win-gpu-f1-preflight/' + name for name in ('attempt-1/checkpoint', 'attempt-1/final', 'run-1', 'recovery-1')]
assert git('diff', '--name-only', auth['checkpoint_commit'], HEAD, '--', *history) == b''

instance = cleanup.Cleanup(EXECUTION, (EXECUTION / cleanup.AUTH_REL).resolve(),
                           auth['session'], 8, auth['exclusive_run_id'])
original = instance.run_command
commands = []
def read_only_command(argv, label, cap=30):
    allowed_git = argv[0] == 'git' and argv[1] in {
        'status', 'branch', 'rev-parse', 'merge-base', 'cat-file', 'show', 'log', 'diff-tree'}
    allowed_manifest = (argv[:3] == [sys.executable, str(EXECUTION / cleanup.PACKET_REL / 'manifest.py'), 'verify'])
    assert allowed_git or allowed_manifest, 'non-read-only subprocess refused: ' + label
    commands.append(label)
    return original(argv, label, cap)
instance.run_command = read_only_command
instance.precheck()
assert instance.identity == cleanup.reviewed_identity(auth)
assert instance.activation_sample == sample
assert not instance.live.exists() and not instance.final.exists() and not instance.binding.exists()
assert git('status', '--porcelain', repo=EXECUTION) == b''
print('ACTIVATION-PRECHECK-PASS commands=' + str(len(commands)) + ' WSL=0 registry=0 writes=0')
print('activation_epoch=' + str(sample['epoch']) + ' activation_expires_in=' + str(sample['expires_in']))
print('EXECUTED-BRANCH-CLEAN-PUSHED head=' + HEAD)
