#!/usr/bin/env python3
"""Run a command and prefix each output line with seconds since launch (a line timestamper).

usage: python3 docs/proof/test-cost/stamp.py <command> [args...]
Exit status is the command's own. Used to time the Desktop harness's serial in-process prefix:
the parent prints its theme/section-canvas lines live, while Spawn buffers each child's lines.
"""
import subprocess
import sys
import time

started = time.monotonic()
child = subprocess.Popen(sys.argv[1:], stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                         text=True, encoding="utf-8", errors="replace")
for line in child.stdout:
    sys.stdout.write(f"{time.monotonic() - started:7.2f} {line}")
sys.exit(child.wait())
