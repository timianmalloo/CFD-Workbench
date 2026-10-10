#!/usr/bin/env python3
"""Bind APT's selected download URIs to the exact package lock."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from urllib.parse import unquote, urlparse


URI = re.compile(r"^'([^']+)'\s+(\S+)\s+(\d+)\s+(\S+)$")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lock", type=Path, required=True)
    parser.add_argument("--uris", type=Path, required=True)
    args = parser.parse_args()

    document = json.loads(args.lock.read_text(encoding="utf-8"))
    by_basename = {}
    for item in document["packages"]:
        basename = Path(item["filename"]).name
        if basename in by_basename:
            raise RuntimeError(f"lock contains duplicate archive basename: {basename}")
        by_basename[basename] = item
    bound = set()
    for line in args.uris.read_text(encoding="utf-8").splitlines():
        match = URI.match(line)
        if match is None:
            continue
        uri, _local_name, size, digest = match.groups()
        basename = unquote(Path(urlparse(uri).path).name)
        if basename not in by_basename:
            raise RuntimeError(f"APT selected URI outside lock: {uri}")
        item = by_basename[basename]
        if int(size) != item["download_bytes"]:
            raise RuntimeError(f"APT URI size differs from signed metadata: {basename}")
        if basename in bound:
            raise RuntimeError(f"APT selected duplicate URI for {basename}")
        item["url"] = uri
        item["origin_host"] = urlparse(uri).hostname or "Not recorded"
        item["apt_uri_digest"] = digest
        bound.add(basename)
    missing = sorted(set(by_basename) - bound)
    if missing:
        raise RuntimeError(f"APT emitted no URI for locked archives: {missing[:5]}")
    args.lock.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"PACKAGE-URI-BINDING-PASS packages={len(bound)}")


if __name__ == "__main__":
    main()
