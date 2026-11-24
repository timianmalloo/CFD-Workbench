"""CF-W8-1: verify-open-measure.py must reject NaN/Infinity/-Infinity in JSON and _is_finite_number."""
from __future__ import annotations

import copy
import importlib.util
import json
import math
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tools" / "spikes" / "cad-export-measure" / "verify-open-measure.py"


def _load_verifier():
    spec = importlib.util.spec_from_file_location("verify_open_measure", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class CadExportMeasureTests(unittest.TestCase):
    def test_verify_open_measure_rejects_nan_and_infinity_and_self_test_green(self):
        mod = _load_verifier()

        # 1. Direct _is_finite_number checks (catches missing math.isfinite).
        for bad in (float("nan"), float("inf"), float("-inf"), math.nan, math.inf, -math.inf, True, False, "1.0", None):
            with self.subTest(direct_value=bad):
                self.assertFalse(mod._is_finite_number(bad), f"Expected _is_finite_number({bad!r}) to be False")

        for good in (0, 0.0, 1.5, 10.0):
            with self.subTest(finite_value=good):
                self.assertTrue(mod._is_finite_number(good), f"Expected _is_finite_number({good!r}) to be True")

        # 2. JSON text containing NaN / Infinity / -Infinity across numeric fields (catches missing parse_constant).
        base = mod._sample_green_receipt()
        for token in ("NaN", "Infinity", "-Infinity"):
            for field, sentinel in (
                ("chordErrorUm", "1.2"),
                ("spanErrorUm", "0.4"),
                ("thicknessErrorUm", "1.5"),
                ("trailingEdgeErrorUm", "0.3"),
                ("volumeErrorPercent", "0.02"),
            ):
                with self.subTest(token=token, field=field):
                    raw = json.dumps(copy.deepcopy(base)).replace(f'"{field}": {sentinel}', f'"{field}": {token}', 1)
                    step_ok, dm_ok, errors = mod.validate_receipt_text(raw)
                    self.assertFalse(step_ok, f"{field}={token} must refuse STEP unlock")
                    self.assertFalse(dm_ok, f"{field}={token} must refuse 3DM unlock")
                    self.assertTrue(errors, f"{field}={token} must report an error")

        # 3. CLI --self-test exits 0 cleanly.
        proc = subprocess.run(
            [sys.executable, str(SCRIPT), "--self-test"],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
        self.assertEqual(0, proc.returncode, f"--self-test failed: stdout={proc.stdout!r} stderr={proc.stderr!r}")
        self.assertIn("self-test OK", proc.stdout)


if __name__ == "__main__":
    unittest.main()
