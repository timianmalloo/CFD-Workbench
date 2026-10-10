#!/usr/bin/env python3
"""Retain and describe an exact complete-row prefix of the observer store."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--samples", type=Path, required=True)
    parser.add_argument("--rows", type=int, required=True)
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--binding", type=Path, required=True)
    args = parser.parse_args()
    if args.rows < 1:
        parser.error("--rows must be positive")

    raw = args.samples.read_bytes()
    lines = raw.splitlines(keepends=True)
    if len(lines) < args.rows:
        raise SystemExit(f"need {args.rows} rows, found {len(lines)}")
    selected = lines[: args.rows]
    if any(not line.endswith(b"\n") for line in selected):
        raise SystemExit("selected prefix contains an incomplete row")
    prefix = b"".join(selected)
    args.snapshot.write_bytes(prefix)
    binding = {
        "schema": "cfdw-observer-prefix-binding/1",
        "source": str(args.samples).replace("\\", "/"),
        "snapshot": args.snapshot.name,
        "prefix_row_count": args.rows,
        "prefix_byte_count": len(prefix),
        "prefix_sha256": hashlib.sha256(prefix).hexdigest(),
        "live_store_prefix_equal": raw.startswith(prefix),
    }
    args.binding.write_text(json.dumps(binding, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
