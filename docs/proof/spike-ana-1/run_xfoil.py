"""Run the locally built official XFOIL 6.99 executable for six NACA 0012 polars."""

import hashlib
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parent
XFOIL = ROOT / "xfoil"
EXECUTABLE = XFOIL / "Xfoil" / "bin" / "xfoil"
assert EXECUTABLE.is_file(), "Build XFOIL first with xfoil/build.sh"
(ROOT / "data").mkdir(exist_ok=True)

for reynolds in (200000, 1000000):
    for ncrit in (2, 4, 9):
        stem = f"re{reynolds}-n{ncrit}"
        polar = XFOIL / f"polar-{stem}.txt"
        polar.unlink(missing_ok=True)
        commands = [
            "PLOP", "G", "", "NACA 0012", "PANE", "OPER",
            f"VISC {reynolds}", "VPAR", f"N {ncrit}", "",
            "ITER 200", "PACC", "", "", "ASEQ -6 6 1", "PACC",
            "PWRT", polar.name, "", "QUIT", "",
        ]
        input_text = "\n".join(commands)
        (XFOIL / f"run-{stem}.in").write_text(input_text)
        completed = subprocess.run(
            [str(EXECUTABLE)], input=input_text, text=True, capture_output=True,
            cwd=XFOIL, timeout=120, check=False,
        )
        (XFOIL / f"run-{stem}.log").write_text(completed.stdout + completed.stderr)
        print(stem, "exit", completed.returncode, "polar_bytes", polar.stat().st_size if polar.exists() else 0)
        if completed.returncode or not polar.exists():
            raise RuntimeError(f"XFOIL failed for {stem}; read {XFOIL / f'run-{stem}.log'}")
        # Keep XFOIL's values/header while removing Fortran-padded line endings.
        (ROOT / "data" / polar.name).write_text(
            "\n".join(line.rstrip() for line in polar.read_text().splitlines()) + "\n"
        )

print("xfoil_bin_sha256", hashlib.sha256(EXECUTABLE.read_bytes()).hexdigest())
