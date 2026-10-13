"""Offline failure-receipt and import-boundary checks; no process/WSL observation."""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import traceback
from unittest.mock import patch

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
REL = Path('docs/proof/win-gpu-f1-preflight/attempt-1/cleanup')
FINAL = ROOT / REL / 'run-1/final'
BINDING = 'cecafe4e93367de66cbf161381404f2e707f0cfb'
EVIDENCE = '901bc8d79767a3e4b69564749ec3319c8d750f52'
SOURCE = '6e788efbb7c2bc6f708927b183515c9ced60226d'

def git(*args):
    return subprocess.run(['git', '-C', str(ROOT), *args], capture_output=True, check=True).stdout

binding = json.loads((ROOT / REL / 'run-1/binding.json').read_bytes())
assert binding['evidence_commit'] == EVIDENCE
assert binding['outcome'] == 'failed-stop-no-retry'
manifest_sha = hashlib.sha256((FINAL / 'capture-manifest.json').read_bytes()).hexdigest()
assert binding['manifest_sha256'] == manifest_sha == 'ed5417016dd8469d0db3de56ffc36d71d04d05accb0c4c97859e1675e6b64114'
assert git('rev-parse', BINDING + '^').decode().strip() == EVIDENCE
assert git('rev-parse', EVIDENCE + '^').decode().strip() == 'fd4f738ad01f6a4be89c92d844c5ef372c47ea07'
assert git('show', BINDING + ':' + (REL / 'run-1/binding.json').as_posix()) == (ROOT / REL / 'run-1/binding.json').read_bytes()
receipt = json.loads((FINAL / 'cleanup-receipt.json').read_bytes())
spec = importlib.util.spec_from_file_location('failure_review_cleanup', ROOT / REL / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)
auth = json.loads((ROOT / cleanup.AUTH_REL).read_bytes())
assert receipt['source_commit'] == SOURCE and receipt['ruling'] == auth['ruling'] == 230
assert receipt['authorization_sha256'] == cleanup.digest(ROOT / cleanup.AUTH_REL)
assert receipt['payload_sha256'] == cleanup.digest(ROOT / cleanup.PAYLOAD_REL)
assert receipt['outcome'] == 'failed-stop-no-retry'
assert receipt['failure']['raw_error_sha256'] == hashlib.sha256(b'owned-stage-check exited 1').hexdigest()
activation = receipt['activation_leader_sample']
second = json.loads((FINAL / 'leader-immediately-pre-live.json').read_bytes())
cleanup.validate_leader_sample(activation, 8)
cleanup.validate_leader_sample(second, 8)
identity = json.loads((FINAL / 'identity-before.json').read_bytes())
cleanup.validate_identity(identity['registrations'], identity['names'], identity['target_path_present'],
                          identity['protected_path_present'], cleanup.reviewed_identity(auth))
commands = receipt['commands']
assert len(commands) == 32
assert all(row['exit'] == 0 for row in commands[:-1])
assert commands[-1]['label'] == 'owned-stage-check' and commands[-1]['exit'] == 1
assert [row['label'] for row in commands[-4:]] == ['leader-immediately-pre-live', 'distro-list', 'distro-list', 'owned-stage-check']
wsl = [row for row in commands if row['argv'][0] == 'wsl.exe']
assert len(wsl) == 3
assert not any('--terminate' in row['argv'] or '--unregister' in row['argv'] for row in commands)
stderr = json.loads((FINAL / '032-owned-stage-check.stderr.json').read_bytes())
stdout = json.loads((FINAL / '032-owned-stage-check.stdout.json').read_bytes())
assert stderr['bytes'] == 1888 and stderr['raw'] == 'Not published'
assert stdout['bytes'] == 0
live = ROOT.parent / 'CFD-Workbench-win-gpu-f1-r228-cleanup' / REL / 'run-1/live'
if live.exists():
    files = list(live.rglob('*'))
    files = [path for path in files if path.is_file()]
    assert (live / '032-owned-stage-check.stderr.json').read_bytes() == (FINAL / '032-owned-stage-check.stderr.json').read_bytes()
    non_json = [path for path in files if path.suffix != '.json']
    print('UNTRACKED-LIVE-INSPECTION files=' + str(len(files)) + ' non_json_files=' + str(len(non_json)))
    assert not non_json
print('RECEIPT-PASS commands=32 WSL-list=2 helper=1 terminate=0 unregister=0')
print('LEASE-PASS activation=' + str(activation['expires_in']) + ' immediate=' + str(second['expires_in']))
print('CHILD-DIAGNOSTIC unavailable; stderr_bytes=' + str(stderr['bytes']) + ' sha256=' + stderr['sha256'])

helper_path = ROOT / REL / 'cleanup_owned_check.py'
owned_path = ROOT / cleanup.PACKET_REL / 'owned_control.py'
for path in (helper_path, owned_path):
    assert git('show', SOURCE + ':' + path.relative_to(ROOT).as_posix()) == path.read_bytes()
helper_spec = importlib.util.spec_from_file_location('review_failure_helper', helper_path)
helper = importlib.util.module_from_spec(helper_spec)
helper_spec.loader.exec_module(helper)
with patch.object(subprocess, 'run', side_effect=AssertionError('process calls forbidden')) as launches:
    try:
        helper.load_owned_control(owned_path)
    except AttributeError as error:
        assert str(error) == "'NoneType' object has no attribute '__dict__'"
        frames = [frame.name for frame in traceback.extract_tb(error.__traceback__)]
        assert 'load_owned_control' in frames and '_is_type' in frames
        print('LOADER-DEFECT-VERIFIED type=AttributeError boundary=dataclasses._is_type process_calls=0')
    else:
        raise AssertionError('production loader unexpectedly passed')
    launches.assert_not_called()

# In-memory diagnostic control only; never edits/replaces the production loader.
# Registering the module permits its definitions and evidence parser to load.
name = 'r229_owned_control'
assert name not in sys.modules
owned_spec = importlib.util.spec_from_file_location(name, owned_path)
owned = importlib.util.module_from_spec(owned_spec)
sys.modules[name] = owned
try:
    with patch.object(subprocess, 'run', side_effect=AssertionError('process calls forbidden')) as launches:
        owned_spec.loader.exec_module(owned)
        records = owned.read_owner_records(ROOT / cleanup.FAILURE_REL, 'stage')
        unresolved = [stage for stage in owned.STAGES
                      if (ROOT / cleanup.FAILURE_REL / (stage + '.intent.json')).exists()
                      and not (ROOT / cleanup.FAILURE_REL / (stage + '.owner.txt')).exists()
                      and not (ROOT / cleanup.FAILURE_REL / (stage + '.entry.txt')).exists()]
        launches.assert_not_called()
    print('REGISTERED-IMPORT-CONTROL-PASS owner_record_count=' + str(len(records)) + ' unresolved_intents=' + str(len(unresolved)))
finally:
    del sys.modules[name]
print('DIAGNOSIS: local loader defect VERIFIED; same live cause INFERRED; actual owned-process state FLAGGED')
