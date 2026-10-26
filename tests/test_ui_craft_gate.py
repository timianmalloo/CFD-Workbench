"""The craft gate refuses a target the detector has no rules for, before it runs the detector."""
import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("ui_craft_gate", ROOT / "docs/ai-forward-pack/scripts/ui-craft-gate.py")
gate = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)

# A stand-in detector: it records that it ran and reports one finding, so a refusal is told apart from a detector error.
FAKE = """import json, pathlib, sys
pathlib.Path(sys.argv[0]).with_name("ran").write_text("ran")
print(json.dumps([{"antipattern": "overused-font", "name": "Overused font", "severity": "warning",
                   "file": sys.argv[-1], "line": 1, "snippet": "Inter"}]))
"""


class CraftGateTargetTests(unittest.TestCase):
    def setUp(self):
        self.dir = Path(tempfile.mkdtemp())
        self.addCleanup(lambda: __import__("shutil").rmtree(self.dir, ignore_errors=True))
        fake = self.dir / "fake.py"
        fake.write_text(FAKE, encoding="utf-8")
        self.detector = f'"{sys.executable}" "{fake}"'
        self.ran = self.dir / "ran"

    def write(self, name, text="x"):
        path = self.dir / "ui" / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path

    def run_gate(self, *targets):
        err = io.StringIO()
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(err):
            code = gate.main([str(t) for t in targets] + ["--impeccable", self.detector])
        return code, err.getvalue()

    def test_a_file_type_without_rules_is_refused_before_the_detector_runs(self):
        # Measured on engine 0.1.16: a named .axaml file in Avalonia syntax returns no findings at all.
        for name in ("Dialog.axaml", "Page.xaml", "index.xhtml"):
            with self.subTest(name=name):
                code, err = self.run_gate(self.write(name))
                self.assertEqual(2, code)
                self.assertIn(Path(name).suffix, err)
                self.assertFalse(self.ran.exists(), "the detector ran on a refused target")

    def test_a_directory_with_nothing_the_detector_reads_is_refused(self):
        self.write("Dialog.axaml")
        self.write("Dialog.axaml.cs")
        code, err = self.run_gate(self.dir / "ui")
        self.assertEqual(2, code)
        self.assertIn("nothing", err)
        self.assertFalse(self.ran.exists())

    def test_readable_files_only_in_folders_the_walk_skips_are_refused(self):
        # Measured on 0.1.16: the walk never enters node_modules, dist, build, __pycache__ or a dot-folder.
        for folder in ("node_modules", "dist", "build", "__pycache__", ".cache"):
            with self.subTest(folder=folder):
                tree = self.dir / ("tree-" + folder.strip("._"))
                (tree / folder).mkdir(parents=True)
                (tree / folder / "site.css").write_text("x", encoding="utf-8")
                code, err = self.run_gate(tree)
                self.assertEqual(2, code)
                self.assertFalse(self.ran.exists())

    def test_a_missing_path_is_refused_and_a_url_goes_to_the_detector(self):
        code, err = self.run_gate(self.dir / "absent.html")
        self.assertEqual(2, code)
        self.assertIn("does not exist", err)
        self.assertFalse(self.ran.exists())
        code, _ = self.run_gate("https://example.invalid/")
        self.assertEqual(0, code)
        self.assertTrue(self.ran.exists())

    def test_a_readable_file_runs_the_detector(self):
        code, _ = self.run_gate(self.write("site.css"))
        self.assertEqual(0, code)
        self.assertTrue(self.ran.exists())

    def test_a_mixed_directory_runs_and_names_what_was_not_scanned(self):
        self.write("site.css")
        self.write("Dialog.axaml")
        code, err = self.run_gate(self.dir / "ui")
        self.assertEqual(0, code)
        self.assertTrue(self.ran.exists())
        self.assertIn(".axaml", err)


if __name__ == "__main__":
    unittest.main()
