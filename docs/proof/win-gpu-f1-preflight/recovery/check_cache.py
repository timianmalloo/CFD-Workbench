#!/usr/bin/env python3
"""Read-only admission of the exact disposable guest cache; no host/mounted deletion."""
import json
from pathlib import Path

CACHE = Path('/var/cache/cfdw-f1-r217/r217-run1-epoch6-20261012')


def validate_cache(cache=CACHE, root=Path('/'), mountinfo=Path('/proc/self/mountinfo')):
    device = root.stat().st_dev
    for parent in reversed((cache, *cache.parents)):
        if parent.is_symlink():
            raise ValueError('cache ancestry is linked')
        if parent.exists() and (not parent.is_dir() or parent.stat().st_dev != device):
            raise ValueError('cache ancestry crosses a mount/device boundary')
    if cache.exists():
        for child in cache.iterdir():
            if child.is_symlink() or not child.is_file() or child.suffix != '.deb' or child.stat().st_dev != device:
                raise ValueError('cache contains a linked, mounted or unexpected entry')
    # Bind mounts can share st_dev. Resolve all mountpoints independently.
    for line in mountinfo.read_text(encoding='utf-8').splitlines():
        parts = line.split()
        if len(parts) < 10 or '-' not in parts:
            raise ValueError('mount inspection malformed')
        mount = Path(parts[4].replace('\\040', ' ').replace('\\011', '\t').replace('\\134', '\\'))
        if mount != root and (mount == cache or mount in cache.parents or cache in mount.parents):
            raise ValueError('cache intersects a mountpoint')
    print(json.dumps({'cache': str(cache), 'debs': sorted(p.name for p in cache.glob('*.deb')), 'admitted': True}))


if __name__ == '__main__':
    validate_cache()
