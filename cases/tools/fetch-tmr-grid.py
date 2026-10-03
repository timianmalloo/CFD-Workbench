#!/usr/bin/env python3
"""Fetch named members of the NASA TMR NACA 0012 numerics grid zip (2.98 GB) with HTTP range requests only.

Run: python3 cases/tools/fetch-tmr-grid.py <out-dir> <member-name>...
     e.g. n0012familyII.5.p2dfmt.gz
Source: https://www.nasa.gov/wp-content/uploads/2026/02/naca0012numerics-grids.zip, linked from
https://tmbwg.github.io/turbmodels/naca0012numerics_grids.html. Prints each member's sha256 and size.
"""
import hashlib
import io
import os
import sys
import urllib.request
import zipfile

URL = "https://www.nasa.gov/wp-content/uploads/2026/02/naca0012numerics-grids.zip"


class RangeFile(io.RawIOBase):
    def __init__(self, url):
        self.url, self.pos = url, 0
        head = urllib.request.urlopen(urllib.request.Request(url, method="HEAD"), timeout=60)
        self.size = int(head.headers["Content-Length"])

    def seekable(self):
        return True

    def readable(self):
        return True

    def tell(self):
        return self.pos

    def seek(self, offset, whence=0):
        self.pos = {0: offset, 1: self.pos + offset, 2: self.size + offset}[whence]
        return self.pos

    def readinto(self, buffer):
        if self.pos >= self.size:
            return 0
        end = min(self.size, self.pos + len(buffer)) - 1
        req = urllib.request.Request(self.url, headers={"Range": f"bytes={self.pos}-{end}"})
        data = urllib.request.urlopen(req, timeout=120).read()
        buffer[:len(data)] = data
        self.pos += len(data)
        return len(data)


out = sys.argv[1]
os.makedirs(out, exist_ok=True)
archive = zipfile.ZipFile(io.BufferedReader(RangeFile(URL), buffer_size=1 << 20))
names = {os.path.basename(n): n for n in archive.namelist()}
for member in sys.argv[2:]:
    data = archive.read(names[member])
    path = os.path.join(out, member)
    with open(path, "wb") as handle:
        handle.write(data)
    print(f"{member} bytes={len(data)} sha256={hashlib.sha256(data).hexdigest()} zip_path={names[member]}")
