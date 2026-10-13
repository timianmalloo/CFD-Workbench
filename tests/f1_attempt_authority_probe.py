"""Inert host-child boundary for the actual PowerShell admission span."""
import hashlib
import json
from pathlib import Path
import sys

path, auth, packet = map(Path, sys.argv[1:4])
sys.path.insert(0, str(packet))
import attempt
document = json.loads(path.read_text(encoding='utf-8-sig'))
sha = hashlib.sha256(path.read_bytes()).hexdigest()
if len(sys.argv) == 5:
    mode = sys.argv[4]
    if mode == 'missing':
        if auth.exists():
            auth.unlink()
    else:
        value = attempt.authorization(document, sha, accepted=mode == 'altered')
        if mode == 'altered':
            value['run_id'] = 'stale-run'
        auth.write_text(json.dumps(value), encoding='utf-8')
else:
    try:
        attempt.check_authorization(json.loads(auth.read_bytes()), document, sha)
    except ValueError as error:
        print(str(error), file=sys.stderr)
        raise SystemExit(5)
