"""Fail unless every non-empty line of an append-only JSONL log is JSON and every id is unique."""
import json
import sys

path = sys.argv[1]
ids = []
for number, line in enumerate(open(path, encoding="utf-8"), 1):
    if not line.strip():
        continue
    try:
        ids.append(json.loads(line).get("id"))
    except json.JSONDecodeError as error:
        sys.exit(f"{path}:{number}: not JSON ({error})")
duplicates = {i for i in ids if i is not None and ids.count(i) > 1}
if duplicates:
    sys.exit(f"{path}: duplicate ids {sorted(duplicates)[:5]}")
print(f"{path}: {len(ids)} entries, ids unique")
