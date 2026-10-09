#!/usr/bin/env python3
"""R185 packaging adapter: reuse R184 byte capture, allowing only its three gates."""
import importlib.util
import json
from pathlib import Path
import sys

PROOF = Path(__file__).resolve().parent


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in ("portable", "verify", "docs"):
        raise RuntimeError("only the three ordered R185 gates are permitted")
    spec = importlib.util.spec_from_file_location("preparation", PROOF.parent / "capture-preparation.py")
    capture = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(capture)
    capture.PROOF = PROOF
    result = capture.main()
    receipt = PROOF / (sys.argv[1] + ".json")
    metadata = json.loads(receipt.read_text(encoding="utf-8"))
    metadata["capture_contract_ruling"] = metadata["ruling"]
    metadata["ruling"] = 185
    metadata["qualification_executed"] = False
    receipt.write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8", newline="\n")
    return result


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8", errors="replace")
            except (ValueError, OSError):
                pass
    try:
        raise SystemExit(main())
    except Exception as error:
        print("R185 CAPTURE FAIL " + type(error).__name__ + "; publication withheld", file=sys.stderr)
        raise SystemExit(1)
