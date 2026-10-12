#!/usr/bin/env python3
"""Offline authoring only: index an already copied historical live tree without rewriting captures."""
import importlib.util
import json
from pathlib import Path


def main():
    repo = Path(__file__).resolve().parents[4]
    base = repo / 'docs/proof/win-gpu-f1-preflight'
    spec = importlib.util.spec_from_file_location('manifest', base / 'packet/manifest.py')
    manifest = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(manifest)
    retained = base / 'run-1/failed-live'
    output = retained / 'capture-manifest.json'
    if output.exists():
        raise ValueError('historical inventory already exists; never overwrite')
    files = []
    for path in sorted(retained.rglob('*')):
        if path.is_symlink() or path.is_junction():
            raise ValueError('linked historical evidence')
        if path.is_file():
            size, sha = manifest.hash_file(path)
            files.append(dict(path=path.relative_to(retained).as_posix(), bytes=size, sha256=sha))
    output.write_text(json.dumps(dict(schema='cfdw-f1-r217-capture-manifest/1', files=files),
                                indent=2, sort_keys=True) + '\n', encoding='utf-8', newline='\n')
    print('RETAINED-LIVE-INDEXED files=' + str(len(files)))


if __name__ == '__main__':
    main()
