"""Offline-fast independent production probes; no WSL/registry/lifecycle calls."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'docs/proof/win-gpu-f1-preflight/attempt-1/cleanup'
SOURCE = 'f75ae322574c5e3160012708b80fc610faa5a0e9'
CANDIDATE = '4044b77a08fcbdc9b5b19151fb3e9b93b043d009'
spec = importlib.util.spec_from_file_location('review_r230', FOLDER / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)

def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, check=True).stdout

def refuses(call):
    try:
        call()
    except (ValueError, FileNotFoundError):
        return
    raise AssertionError('required refusal missing')

hashes = {
    cleanup.PAYLOAD_REL: 'b5aecd640ce42c91760c7410a6e45c8a8ef867f0a45bbb7398b314bbfca82368',
    cleanup.AUTH_REL: '30f5a67158121ce9c1ba3fa622fd43db4ff692d439afce1fbb5ae8ca2d9f0822',
    Path('docs/notes/windows-gpu-f1-r230-cleanup-request.md'): 'c39fcc493212fef2448ee82bea1dcabdd9b53ab4ad4d867d9a6bd8645dc46e5d',
}
for relative, expected in hashes.items():
    assert cleanup.digest(ROOT / relative) == expected
    assert git('show', CANDIDATE + ':' + relative.as_posix()) == (ROOT / relative).read_bytes()
    print(relative.name + '=' + expected)
payload = json.loads((ROOT / cleanup.PAYLOAD_REL).read_bytes())
auth = json.loads((ROOT / cleanup.AUTH_REL).read_bytes())
assert payload['source_commit'] == SOURCE
git('merge-base', '--is-ancestor', SOURCE, CANDIDATE)
for commit in [auth['checkpoint_commit'], *auth['checkpoint_ancestors']]:
    git('merge-base', '--is-ancestor', commit, SOURCE)
for relative, expected in payload['files'].items():
    assert cleanup.digest(ROOT / relative) == expected
    assert hashlib.sha256(git('show', SOURCE + ':' + relative)).hexdigest() == expected
cleanup.validate_authorization(auth, hashes[cleanup.PAYLOAD_REL])
for key in auth:
    changed = copy.deepcopy(auth)
    changed[key] = 'invalid' if key == 'source_commit' else None
    refuses(lambda: cleanup.validate_authorization(changed, hashes[cleanup.PAYLOAD_REL]))
identity = cleanup.reviewed_identity(auth)
rows = json.loads((ROOT / cleanup.FAILURE_REL / 'coordinator/host-registrations-after-failure.json').read_bytes())
target, protected = cleanup.validate_identity(rows, [identity['target'], identity['protected']], True, True, identity)
assert target['version'] == protected['version'] == 2
assert payload['target_identity'] == {key: target[key] for key in ('name', 'key', 'path')}
assert payload['protected_identity'] == protected
for key in ('target', 'protected'):
    alias = dict(name='unreviewed-alias', key='{11111111-2222-3333-4444-555555555555}',
                 path=identity[key + '_path'], version=2)
    refuses(lambda: cleanup.validate_identity(rows + [alias], [identity['target'], identity['protected'], alias['name']], True, True, identity))
    if key == 'protected':
        refuses(lambda: cleanup.validate_absence([protected, alias], [identity['protected'], alias['name']], False, True, protected, identity))
origin_rulings = git('show', 'origin/main:docs/notes/rulings.md').decode('utf-8')
refuses(lambda: cleanup.ruling_section(origin_rulings))
assert not (ROOT / cleanup.ACTIVATION_SAMPLE_REL).exists()
history = ['docs/proof/win-gpu-f1-preflight/' + name for name in ('attempt-1/checkpoint', 'attempt-1/final', 'run-1', 'recovery-1')]
assert git('diff', '--name-only', auth['checkpoint_commit'], CANDIDATE, '--', *history) == b''
print('BINDING-IDENTITY-AUTHORITY-HISTORY-PASS; source files=' + str(len(payload['files'])))

# Exercise the production execute boundary with the live/process methods mocked.
# The real second-sample reader and validator receive an expired-threshold sample.
with tempfile.TemporaryDirectory() as temporary:
    instance = cleanup.Cleanup(Path(temporary), ROOT / cleanup.AUTH_REL,
                               auth['session'], auth['leader_epoch'], auth['exclusive_run_id'])
    low_sample = {'leader': 'mac', 'state': 'live', 'epoch': 8, 'expires_in': 199.0}
    with patch.object(instance, 'precheck'), patch.object(instance, 'publish_precheck'), \
         patch.object(instance, 'run_command', return_value=json.dumps(low_sample).encode()) as command, \
         patch.object(instance, 'action') as action, patch.object(instance, 'bind') as bind:
        refuses(instance.execute)
        action.assert_not_called()
        assert command.call_count == 1
        assert command.call_args.args[0][-3:] == ['leader', 'who', '--json']
        receipt_missing = not bind.called
        print('LOW-TTL-PRE-LIVE: refused; lifecycle calls=0; bind calls=' + str(bind.call_count))

valid = {'leader': 'mac', 'state': 'live', 'epoch': 8, 'expires_in': 200.0}
cleanup.validate_leader_sample(valid, 8)
admitted_nonfinite = []
for label, ttl in [('NaN', float('nan')), ('Infinity', float('inf'))]:
    try:
        cleanup.validate_leader_sample(dict(valid, expires_in=ttl), 8)
    except ValueError:
        pass
    else:
        admitted_nonfinite.append(label)
print('NONFINITE-TTL-ADMITTED=' + repr(admitted_nonfinite))
assert not receipt_missing and not admitted_nonfinite, 'REVIEW-VETO: pre-live lease failure escapes receipt; non-finite TTL is admitted'
