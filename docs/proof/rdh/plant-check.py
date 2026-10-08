"""Plant check: join_ring_problems must flag a run-tests.sh copy without the cost call. Exit 0 only if it does."""
import importlib.util, json, shutil, sys, tempfile
from pathlib import Path

real = Path(sys.argv[1])
spec = importlib.util.spec_from_file_location("check_docs", real / "tools" / "check-docs.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
contract = json.loads((real / "docs/coordination/join.json").read_text(encoding="utf-8"))


def run(strip):
    root = Path(tempfile.mkdtemp())
    (root / "tools").mkdir()
    shutil.copy(real / "tools/join-ring.sh", root / "tools/join-ring.sh")
    text = (real / "tools/run-tests.sh").read_text(encoding="utf-8")
    if strip:
        text = text.replace("check-test-costs.py", "removed.py")
    (root / "tools/run-tests.sh").write_text(text, encoding="utf-8")
    mod.ROOT = root
    found = [p for p in mod.join_ring_problems(contract) if "join-ring" in p or "run-tests" in p or "check-test-costs" in p]
    shutil.rmtree(root)
    return found


clean, planted = run(False), run(True)
print("clean copy problems:", clean)
print("planted copy problems:", planted)
sys.exit(0 if not clean and planted else 1)
