"""The STORE-SUBSET umask-partition rule: one definition, used by check-docs.py and verify-application-core.py."""
from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
# The umask- and native-sensitive checks: every check in the four store test files. They are the only Core
# checks that create files, read CFD_TEST_UMASK, load libcfd_store, or depend on owner-only file modes.
STORE_TESTS = ROOT / "tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs"
STORE_TEST_FILES = (
    STORE_TESTS,
    ROOT / "tests/CfdWorkbench.Core.Tests/LayoutFileTests.cs",
    ROOT / "tests/CfdWorkbench.Core.Tests/PreferenceStoreTests.cs",
    ROOT / "tests/CfdWorkbench.Core.Tests/SectionLibraryTests.cs",
)
STORE_PREFIXES = ("Store_", "NativePrimitive_",
                   "LayoutParse_", "LayoutCodec_", "RecentParse_",
                   "LayoutLoad_", "Rollback_", "PrefStore_", "PrefsSave_", "LayoutSave_",
                   "Recent_", "StoreContract_", "Backup_", "Library_")
# Anything that could make a check depend on the umask, the environment or the native helper.
# Reading the example files, and listing a directory to read it, are umask-independent and allowed
# (a umask only shapes the modes of files a process creates). The harness entry point reads
# CFD_TEST_ONLY and names every suite, so it is exempt.
SENSITIVE = re.compile(r"\bFile\.(?!ReadAll(?:Bytes|Text)\b)|\bDirectory\.(?!(?:EnumerateFiles|GetFiles)\b)|\bFileStream\b|\bFileInfo\b|GetTempPath"
                       r"|GetEnvironmentVariable|DllImport|LibraryImport|\bProjectStore\b"
                       r'|(?<!InternalsVisibleTo\(")CfdWorkbench\.Persistence')
PARTITION_EXEMPT = {path.name for path in STORE_TEST_FILES} | {"IdentityTests.cs"}


def partition_names() -> list[str]:
    """Return every store check name; fail if the umask partition no longer holds."""
    names: list[str] = []
    for file in STORE_TEST_FILES:
        source = file.read_text(encoding="utf-8")
        file_names = re.findall(r'\bCheck\("([^"]+)"', source)
        stray = [name for name in file_names if not name.startswith(STORE_PREFIXES)]
        if not file_names or stray or len(file_names) != len(re.findall(r"\bCheck\(", source)):
            raise SystemExit(f"STORE-SUBSET: every check in {file.name} needs a literal name "
                             f"starting with one of {STORE_PREFIXES}; found {len(file_names)}, stray {stray}")
        names.extend(file_names)
    # A file, environment or native dependency outside the store files would run at one umask only.
    others = [path for path in sorted(STORE_TESTS.parent.glob("*.cs")) if path.name not in PARTITION_EXEMPT]
    others += sorted((ROOT / "src/CfdWorkbench.Core").glob("*.cs"))
    leaks = [f"{path.parent.name}/{path.name}:{number}" for path in others
             for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1) if SENSITIVE.search(line)]
    if leaks:
        raise SystemExit(f"STORE-SUBSET: umask/native-sensitive code outside {STORE_TEST_FILES} would run at one "
                         f"umask only; move it into the store checks or widen the subset: {leaks}")
    return names
