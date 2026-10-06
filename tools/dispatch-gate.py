#!/usr/bin/env python3
"""PARALLEL-BUILD-LOAD control: the Coordinator runs this before each build-track dispatch.

Prints `GO load=<n>` (exit 0) or `WAIT load=<n> reason` (exit 1). Rule (docs/plans/test-cost.md): at most 2 build
tracks at once, and dispatch waits while the 1-minute load is over 24 (the Ruling 81/84/87 gate). A load that cannot
be read prints `WAIT load=not-recorded`: never a guess. `--running <n>` is the count of build tracks now running
(default 0). `--self-test` checks the verdict function.
"""

import os
import sys

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

LOAD_GATE = 24.0
MAX_BUILD_TRACKS = 2


def verdict(load, running):
    """(go, text) from the 1-minute load (None when unreadable) and the build tracks now running."""
    shown = "not-recorded" if load is None else f"{load:.2f}"
    if running >= MAX_BUILD_TRACKS:
        return False, f"WAIT load={shown} {running} build tracks running (cap {MAX_BUILD_TRACKS})"
    if load is None:
        return False, "WAIT load=not-recorded"
    if load > LOAD_GATE:
        return False, f"WAIT load={shown} over {LOAD_GATE:.0f}"
    return True, f"GO load={shown}"


def self_test():
    assert verdict(12.0, 0) == (True, "GO load=12.00")
    assert verdict(24.0, 1)[0] is True, "load at the gate dispatches"
    assert verdict(47.0, 1) == (False, "WAIT load=47.00 over 24")
    assert verdict(5.0, 2)[0] is False and "cap 2" in verdict(5.0, 2)[1]
    assert verdict(None, 0) == (False, "WAIT load=not-recorded")
    print("dispatch-gate self-test OK", flush=True)


def main():
    arguments = sys.argv[1:]
    if "--self-test" in arguments:
        self_test()
        return 0
    running = int(arguments[arguments.index("--running") + 1]) if "--running" in arguments else 0
    try:
        load = os.getloadavg()[0]
    except (AttributeError, OSError):
        load = None
    go, text = verdict(load, running)
    print(text, flush=True)
    return 0 if go else 1


if __name__ == "__main__":
    sys.exit(main())
