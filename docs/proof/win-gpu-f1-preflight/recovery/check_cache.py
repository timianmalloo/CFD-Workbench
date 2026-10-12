#!/usr/bin/env python3
"""Exact disposable guest cache admission/deletion; pin directories and refuse changes."""
import argparse
import contextlib
import hashlib
import json
import os
import stat
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
    ancestry = []
    for parent in reversed((cache, *cache.parents)):
        if parent.exists():
            value = parent.lstat()
            ancestry.append((str(parent), value.st_dev, value.st_ino, value.st_mode))
    files = []
    if cache.exists():
        for path in sorted(cache.iterdir()):
            value = path.lstat()
            files.append((path.name, value.st_dev, value.st_ino, value.st_mode, value.st_size, value.st_mtime_ns))
    return dict(ancestry=ancestry, files=files, mountinfo_sha256=hashlib.sha256(mountinfo.read_bytes()).hexdigest())


def unlink_verified(name, directory_fd, expected, observed):
    """One entry, one pinned directory; a changed entry is never deleted."""
    identity = (observed.st_dev, observed.st_ino, observed.st_mode, observed.st_size, observed.st_mtime_ns)
    if name.endswith('.deb') is False or identity != tuple(expected) or not stat.S_ISREG(observed.st_mode):
        raise ValueError('cache entry changed before deletion')
    os.unlink(name, dir_fd=directory_fd)


def delete_cache(cache=CACHE):
    if cache != CACHE or os.name != 'posix':
        raise ValueError('only exact disposable Linux cache deletion accepted')
    first = validate_cache(cache)
    if first != validate_cache(cache):
        raise ValueError('cache changed across repeated admission')
    if not cache.exists():
        return dict(downloaded_debs='absent', deleted=0)
    with contextlib.ExitStack() as stack:
        descriptor = os.open('/', os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        stack.callback(os.close, descriptor)
        for part in cache.parts[1:]:
            descriptor = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=descriptor)
            stack.callback(os.close, descriptor)
        pinned = os.fstat(descriptor)
        path_stat = cache.lstat()
        if (pinned.st_dev, pinned.st_ino) != (path_stat.st_dev, path_stat.st_ino) or first != validate_cache(cache):
            raise ValueError('pinned cache differs from admitted path')
        expected_files = first['files']
        for index, (name, device, inode, mode, size, modified) in enumerate(expected_files):
            current = validate_cache(cache)
            # Prior deletions change only the entry set; ancestry and mounts must stay exact.
            if current['ancestry'] != first['ancestry'] or current['mountinfo_sha256'] != first['mountinfo_sha256']:
                raise ValueError('cache ancestry/mount changed immediately before unlink')
            if current['files'] != expected_files[index:]:
                raise ValueError('cache entry identities changed immediately before unlink')
            value = os.stat(name, dir_fd=descriptor, follow_symlinks=False)
            unlink_verified(name, descriptor, (device, inode, mode, size, modified), value)
        after = validate_cache(cache)
        if os.listdir(descriptor) or after['ancestry'] != first['ancestry'] or after['mountinfo_sha256'] != first['mountinfo_sha256']:
            raise ValueError('cache residual entries or path change after deletion')
    return dict(downloaded_debs='absent', deleted=len(expected_files))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=('check', 'delete'), default='check')
    parser.add_argument('--cache', type=Path, default=CACHE)
    args = parser.parse_args()
    if args.cache != CACHE:
        raise ValueError('exact original attempt cache required')
    print(json.dumps(delete_cache(args.cache) if args.mode == 'delete' else validate_cache(args.cache), sort_keys=True))
