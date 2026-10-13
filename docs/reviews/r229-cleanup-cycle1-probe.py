"""Independent offline-fast R229 review controls; no host/WSL observation.

Run with py -3 docs/reviews/r229-cleanup-cycle1-probe.py. Uses committed
registration fixtures and mocked process boundaries. Cost recorded in review.
"""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import types
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
REL = 'docs/proof/win-gpu-f1-preflight/attempt-1/cleanup'
FOLDER = ROOT / REL
SOURCE = 'ad4cedbbd34f4d0178576330c98072c84f4dcc69'
CANDIDATE = '6946da407fe0e3412aa22b205a56debac1a82d35'
OLD = 'eb1e0e64c5fcbc9a77ea10d8593e99e2912a0c1f'

def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, check=True).stdout

def refuses(call):
    try:
        call()
    except (ValueError, FileNotFoundError):
        return
    raise AssertionError('required refusal missing')

spec = importlib.util.spec_from_file_location('review_cleanup', FOLDER / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)
git('merge-base', '--is-ancestor', SOURCE, CANDIDATE)
payload = json.loads((FOLDER / 'payload.json').read_bytes())
assert payload['source_commit'] == SOURCE
assert len(payload['files']) == 10
for path, expected in payload['files'].items():
    assert cleanup.digest(ROOT / path) == expected, path
    assert hashlib.sha256(git('show', SOURCE + ':' + path)).hexdigest() == expected, path
expected_hashes = {
    'payload.json': 'dd41827d0e26b9452bcbe4c9040d23f417449a43e55ae0863e9270a05e154370',
    'authorization.proposed.json': 'e4806d0188185bc76a59c5ee06f39e639d597e4e09e5c1c486b00c417d735211',
    'authorization.accepted-projection.json': '82799f9938781b9f2aed94988dde9b5ab90cb61f9262d667eba6ec4796972e71',
}
for name, expected in expected_hashes.items():
    assert cleanup.digest(FOLDER / name) == expected
    assert git('show', CANDIDATE + ':' + REL + '/' + name) == (FOLDER / name).read_bytes()
    print(name + '=' + expected)
assert not (FOLDER / 'authorization.json').exists()
accepted = json.loads((FOLDER / 'authorization.accepted-projection.json').read_bytes())
proposed = json.loads((FOLDER / 'authorization.proposed.json').read_bytes())
assert set(accepted) == set(proposed)
assert {key for key in proposed if proposed[key] != accepted[key]} == {'status', 'ruling'}
sha = expected_hashes['payload.json']
cleanup.validate_authorization(accepted, sha)
refuses(lambda: cleanup.validate_authorization(proposed, sha))
for key in accepted:
    if key == 'source_commit':
        replacement = 'invalid-commit'
    elif isinstance(accepted[key], list):
        replacement = []
    else:
        replacement = None
    changed = dict(accepted, **{key: replacement})
    refuses(lambda: cleanup.validate_authorization(changed, sha))
for name in ('authorization.json', 'authorization.proposed.json', 'authorization.accepted-projection.json'):
    instance = cleanup.Cleanup(ROOT, (FOLDER / name).resolve(), accepted['session'], 8)
    with patch.object(cleanup.subprocess, 'run') as launch:
        refuses(instance.precheck)
        launch.assert_not_called()
print('AUTHORITY-PASS all fields reject invalid values; missing/proposed/noncanonical projection: zero launches')

rows = json.loads((ROOT / cleanup.FAILURE_REL / 'coordinator/host-registrations-after-failure.json').read_bytes())
target, protected = cleanup.validate_identity(rows, [cleanup.TARGET, cleanup.PROTECTED], True, True)
assert payload['target_identity'] == {key: target[key] for key in ('name', 'key', 'path')}
assert payload['protected_identity'] == protected
assert target['version'] == protected['version'] == 2

def aliases(module, variant=False):
    admitted = []
    for label, path in (('target-path-alias', module.TARGET_PATH), ('protected-path-alias', module.PROTECTED_PATH)):
        if variant:
            path = '\\\\?\\' + module.os.path.expandvars(path).upper() + '\\'
        alias = dict(name='unreviewed-alias', key='{11111111-2222-3333-4444-555555555555}', path=path, version=2)
        calls = [(label, lambda: module.validate_identity(rows + [alias], [module.TARGET, module.PROTECTED, alias['name']], True, True))]
        if label == 'protected-path-alias':
            calls.append(('protected-absence-alias', lambda: module.validate_absence([protected, alias], [module.PROTECTED, alias['name']], False, True, protected)))
        for case, call in calls:
            try:
                call()
            except ValueError:
                pass
            else:
                admitted.append(case)
    return admitted

old = types.ModuleType('old_cleanup')
exec(compile(git('show', OLD + ':' + REL + '/cleanup.py'), '<committed-old-cleanup>', 'exec'), old.__dict__)
assert aliases(old) == ['target-path-alias', 'protected-path-alias', 'protected-absence-alias']
assert aliases(cleanup) == []
assert aliases(cleanup, variant=True) == []
print('RED-GREEN-PASS old admits 3; repaired refuses 3 plus normalized variants')
instance = cleanup.Cleanup(ROOT, FOLDER / 'authorization.json', accepted['session'], 8)
instance.protected_before = protected
observation = ([protected], [cleanup.PROTECTED], False, True)
with patch.object(instance, 'observe', side_effect=[observation, copy.deepcopy(observation)]), patch.object(instance, 'write') as output:
    instance.stable_absence()
    assert output.call_args.args[0] == 'absence.json'
changed = ([protected, target], [cleanup.PROTECTED], False, True)
with patch.object(instance, 'observe', side_effect=[observation, changed]), patch.object(instance, 'write') as output:
    refuses(instance.stable_absence)
    output.assert_not_called()
instance.start -= 601
refuses(lambda: instance.remaining(30))
history = ['docs/proof/win-gpu-f1-preflight/' + suffix for suffix in ('attempt-1/checkpoint', 'attempt-1/final', 'run-1', 'recovery-1')]
assert git('diff', '--name-only', 'd6edbc63', CANDIDATE, '--', *history) == b''
assert git('diff', '--name-only', OLD, CANDIDATE, '--', *history) == b''
print('STABILITY-DEADLINE-HISTORY-PASS; offline review complete')
