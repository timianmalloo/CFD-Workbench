#!/usr/bin/env python3
"""Refuse a new CFD review window while another unaccounted window is live.

This is an inventory guard only. It never closes or signals a process.
"""

import argparse
import json
import subprocess
from pathlib import Path


def live_apps():
    result = subprocess.run(['/bin/ps', '-A', '-o', 'pid=,lstart=,command='], capture_output=True,
                            text=True, encoding='utf-8', check=True)
    apps = []
    for raw in result.stdout.splitlines():
        parts = raw.split(None, 6)
        if len(parts) == 7 and parts[0].isdigit() \
                and parts[6].endswith('/Contents/MacOS/CfdWorkbench.Desktop'):
            apps.append({'pid': int(parts[0]), 'start': ' '.join(parts[1:6]),
                         'command': parts[6]})
    return apps


def receipt_identity(path):
    try:
        data = json.loads(path.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return None
    identity = data.get('pidIdentity')
    if isinstance(identity, dict):
        return {'pid': identity.get('pid'), 'start': identity.get('start'),
                'command': identity.get('command')}
    raw = data.get('psIdentityRaw')
    argv = data.get('argv') or []
    if data.get('pid') and isinstance(raw, str) and len(argv) == 1 \
            and isinstance(argv[0], str) and raw.endswith(argv[0]):
        return {'pid': data['pid'], 'start': raw[:-len(argv[0])].strip(),
                'command': argv[0]}
    return None


def classify(apps, receipts, allowed):
    by_identity = {}
    for path, identity in receipts.items():
        if identity is not None:
            key = (identity['pid'], identity['start'], identity['command'])
            by_identity.setdefault(key, []).append(path)
    matched, unknown = [], []
    for app in apps:
        key = (app['pid'], app['start'], app['command'])
        paths = by_identity.get(key, [])
        if paths:
            matched.append({'process': app, 'receipts': paths})
        else:
            unknown.append(app)
    retained = [row for row in matched if any(p in allowed for p in row['receipts'])]
    blocked = [row for row in matched if row not in retained]
    return {'matched': matched, 'retained': retained, 'blocked': blocked,
            'unknown': unknown}


def enforce(state, allowed):
    if allowed and (len(state['retained']) != 1 or state['blocked'] or state['unknown']):
        raise RuntimeError('current review allowance is not exact and exclusive: '
                           + json.dumps(state, sort_keys=True))
    if state['blocked'] or state['unknown']:
        raise RuntimeError('review launch refused: close exact-owned older windows '
                           'through the UI and preserve unknown processes: '
                           + json.dumps(state, sort_keys=True))
    return state


def preflight(allow_paths=()):
    if len(allow_paths) > 1:
        raise RuntimeError('at most one current review receipt may be allowed')
    paths = sorted(Path('/private/tmp').glob('cfd-*/launch-receipt.json'))
    receipts = {str(path): receipt_identity(path) for path in paths}
    allowed = {str(Path(path).resolve()) for path in allow_paths}
    state = classify(live_apps(), receipts, allowed)
    return enforce(state, allowed)


def selftest():
    app = {'pid': 42, 'start': 'Thu Sep 24 10:00:00 2026',
           'command': '/private/tmp/cfd-review/CFD Workbench.app/Contents/MacOS/CfdWorkbench.Desktop'}
    receipt = '/private/tmp/cfd-review/launch-receipt.json'
    identity = dict(app)
    red = classify([app], {receipt: identity}, set())
    assert len(red['blocked']) == 1 and not red['retained']
    try:
        enforce(red, set())
    except RuntimeError:
        pass
    else:
        raise AssertionError('older owned review app was admitted')
    green = classify([app], {receipt: identity}, {receipt})
    assert len(green['retained']) == 1 and not green['blocked']
    assert enforce(green, {receipt}) is green
    reused = dict(app, start='Thu Sep 24 10:00:01 2026')
    unknown = classify([reused], {receipt: identity}, {receipt})
    assert len(unknown['unknown']) == 1 and not unknown['retained']
    try:
        enforce(unknown, {receipt})
    except RuntimeError:
        pass
    else:
        raise AssertionError('unknown reused PID was admitted')
    assert reused == app | {'start': 'Thu Sep 24 10:00:01 2026'}
    return {'olderOwnedRefused': True, 'exactCurrentAllowed': True,
            'unknownPidReusePreserved': True, 'processMutation': False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--allow-live-receipt', action='append', default=[])
    parser.add_argument('--selftest', action='store_true')
    args = parser.parse_args()
    if args.selftest:
        print(json.dumps(selftest(), sort_keys=True))
        return
    print(json.dumps(preflight(args.allow_live_receipt), sort_keys=True))


if __name__ == '__main__':
    main()
