"""Offline integration conservation; no WSL/recovery execution (~10 s integration ring)."""
import hashlib
import importlib.util
import json
import subprocess
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
started = time.perf_counter()
root = Path(__file__).resolve().parents[4]
ours = '02485495951fbd04ea07a2e8bc276b0df4ab93cd'
theirs = '65456e1cc858ff62b87c7a8d7030a6e83715b354'
packet = 'docs/proof/win-gpu-f1-preflight/packet/'
recovery = 'docs/proof/win-gpu-f1-preflight/recovery/'
run = 'docs/proof/win-gpu-f1-preflight/run-1/'

def git(*args):
    return subprocess.run(['git', *args], cwd=root, capture_output=True, check=True).stdout

def blob(ref, path):
    return git('show', ref + ':' + path)

def paths(ref, prefix):
    return git('ls-tree', '-r', '--name-only', ref, '--', prefix).decode().splitlines()

def sha(data):
    return hashlib.sha256(data).hexdigest()

base = git('merge-base', ours, theirs).decode().strip()
packet_rows = []
for path in paths(theirs, packet):
    actual = (root / path).read_bytes()
    name = Path(path).name
    if name == 'packet_selftest.py':
        expected = blob(ours, path)
        newer = blob(theirs, path)
        # Use git's deterministic textual merge to recompute the only shared source seam.
        import tempfile
        with tempfile.TemporaryDirectory() as temporary:
            files = [Path(temporary) / str(i) for i in range(3)]
            for item, data in zip(files, (expected, blob(base, path), newer)):
                item.write_bytes(data)
            expected = git('merge-file', '-p', *map(str, files))
        origin = 'exact three-way union of both accepted self-tests'
    else:
        ref = ours if name == 'preflight.sh' else theirs
        expected = blob(ref, path)
        origin = ref
    assert actual == expected, 'packet source differs: ' + path
    packet_rows.append(dict(path=path, bytes=len(actual), sha256=sha(actual), expected_from=origin))
assert set(paths(ours, packet)) == set(paths(theirs, packet)) == {x['path'] for x in packet_rows}
assert {p.relative_to(root).as_posix() for p in (root / packet).rglob('*') if p.is_file() and '__pycache__' not in p.parts} == {x['path'] for x in packet_rows}, 'packet file set changed'

recovery_rows = []
for path in paths(ours, recovery) + ['tests/test_f1_cleanup_recovery.py']:
    actual = (root / path).read_bytes()
    assert actual == blob(ours, path), 'cleanup source/test changed: ' + path
    recovery_rows.append(dict(path=path, bytes=len(actual), sha256=sha(actual)))
assert {p.relative_to(root).as_posix() for p in (root / recovery).rglob('*') if p.is_file() and '__pycache__' not in p.parts} == set(paths(ours, recovery)), 'recovery file set changed'

retained = []
for folder, payloads in [('checkpoint', 75), ('failed-live', 93)]:
    prefix = run + folder + '/'
    before = paths(ours, prefix)
    packet_parent_paths = set(paths(theirs, prefix))
    assert len(before) == payloads + 1
    actual_paths = {p.relative_to(root).as_posix() for p in (root / prefix).rglob('*') if p.is_file()}
    assert actual_paths == set(before), 'retained file set changed'
    for path in before:
        actual = (root / path).read_bytes()
        assert actual == blob(ours, path), 'retained bytes changed: ' + path
        if path in packet_parent_paths:
            assert actual == blob(theirs, path), 'common packet-parent retained bytes changed: ' + path
    assert packet_parent_paths <= set(before), 'packet-parent retained evidence dropped'
    retained.append(dict(folder=prefix, payloads=payloads, total_files=len(before), tree=git('rev-parse', ours + ':' + prefix.rstrip('/')).decode().strip(), manifest_sha256=sha((root / prefix / 'capture-manifest.json').read_bytes())))

historical = []
for path, ref in [('docs/reviews/windows-gpu-f1-cleanup-recovery.md', ours), ('docs/reviews/windows-gpu-f1-cleanup-recovery-cycle1.md', ours), ('docs/reviews/windows-gpu-f1-cleanup-recovery-cycle1-author.md', ours), ('docs/reviews/windows-gpu-f1-live-defect-repair.md', theirs), (run + 'invocation-contract.json', ours), ('docs/notes/rulings.md', ours)]:
    data = (root / path).read_bytes()
    assert data == blob(ref, path), 'historical evidence changed: ' + path
    historical.append(dict(path=path, bytes=len(data), sha256=sha(data), expected_from=ref))
review_paths = set(paths(ours, 'docs/reviews')) | set(paths(theirs, 'docs/reviews'))
for path in sorted(p for p in review_paths if 'windows-gpu-f1' in p):
    if any(row['path'] == path for row in historical):
        continue
    ref = theirs if path in set(paths(theirs, 'docs/reviews')) else ours
    data = (root / path).read_bytes()
    assert data == blob(ref, path), 'historical review changed: ' + path
    historical.append(dict(path=path, bytes=len(data), sha256=sha(data), expected_from=ref))

audit_path = 'docs/audit/audit-log.jsonl'
audit_entries = [json.loads(line) for line in (root / audit_path).read_text(encoding='utf-8').splitlines() if line.strip()]
merged_audit = {e['id']: e for e in audit_entries}
assert len(audit_entries) == len(merged_audit), 'duplicate audit id'
for ref in (ours, theirs):
    entries = [json.loads(line) for line in blob(ref, audit_path).decode().splitlines() if line.strip()]
    assert all(merged_audit[e['id']] == e for e in entries), 'parent audit lost/modified'

spec = importlib.util.spec_from_file_location('merge_register', root / 'tools/merge-defect-register.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
path = 'docs/lessons/defect-classes.md'
expected = helper.merge(*(blob(ref, path).decode() for ref in (base, ours, theirs)))
assert expected is not None
actual_register = (root / path).read_text(encoding='utf-8')
control_heading = '**R217-INTEGRATION-CONSERVATION-A · '
baseline_register = actual_register.split(control_heading, 1)[0].rstrip('\n') + '\n'
assert actual_register.count(control_heading) == 1, 'integration lesson absent or duplicated'
assert baseline_register == expected.rstrip('\n') + '\n', 'defect register differs from candidate helper union'
assert helper.kept_every_line(expected, *(blob(ref, path).decode() for ref in (base, ours, theirs)))
actual_entries = dict(helper.parse(baseline_register)[1])
parent_keys = set()
for ref in (ours, theirs):
    parent_entries = dict(helper.parse(blob(ref, path).decode())[1])
    parent_keys.update(parent_entries)
    for key, value in parent_entries.items():
        assert key in actual_entries and actual_entries[key][0].startswith(value[0]), 'parent register entry lost or weakened'
assert set(actual_entries) == parent_keys, 'missing/extra parent register entry'
assert len(helper.parse(baseline_register)[1]) == len(parent_keys), 'duplicate parent register entry'
helper_hash = git('hash-object', 'tools/merge-defect-register.py').decode().strip()
assert helper_hash == 'caa877ce432571df602f3a84a2ddcd6dd9983d1b', 'candidate helper drift'

spec = importlib.util.spec_from_file_location('integrated_recovery', root / recovery / 'recover.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.validate_freeze(root / run / 'checkpoint')
refused = []
for label, value in [('missing', {}), ('stale_ruling', module.authorization_example(ours, 'offline-fixture', 6)), ('broadened_actions', module.authorization_example(ours, 'offline-fixture', 6))]:
    if label == 'stale_ruling':
        value['ruling'] = 220
    if label == 'broadened_actions':
        value['allowed_actions'].append('download')
    try:
        module.validate_authorization(value, ours, 'offline-fixture', 6)
    except ValueError:
        refused.append(label)
    else:
        raise AssertionError('bad authorization passed: ' + label)
authorization_files = []
for item in (root / 'docs').rglob('*.json'):
    path = item.relative_to(root).as_posix()
    if item.is_file():
        try:
            value = json.loads((root / path).read_bytes())
        except (OSError, ValueError, UnicodeError):
            continue
        if isinstance(value, dict) and value.get('schema') == 'cfdw-f1-cleanup-authorization/1':
            authorization_files.append(path)
assert not authorization_files, 'cleanup authorization present'
assert not list((root / 'docs/proof/win-gpu-f1-preflight').glob('recovery-1/*')), 'recovery output present'

report = dict(schema='cfdw-f1-offline-integration-proof/1', parents=[ours, theirs], merge_base=base, packet=packet_rows, cleanup=recovery_rows, retained=retained, historical=historical, audit_parent_entries_preserved=True, defect_register_helper_union=True, unique_parent_register_entries=len(parent_keys), candidate_merge_helper_blob=helper_hash, authorization_files=authorization_files, refused_authorization=refused, recovery_execution=False, wsl_invocation=False)
output = Path(__file__).with_name('source-hashes.json')
if '--write-report' in sys.argv:
    output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8', newline='\n')
else:
    assert json.loads(output.read_bytes()) == report, 'integration source hash record drift'
print('INTEGRATION-CONSERVATION-PASS packet=' + str(len(packet_rows)) + ' checkpoint=75+manifest failed-live=93+manifest historical=' + str(len(historical)) + ' audit=both-parent-records authority=absent/fail-closed elapsed_seconds=' + format(time.perf_counter() - started, '.3f'))
