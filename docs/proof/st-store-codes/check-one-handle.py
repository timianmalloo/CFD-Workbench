"""ST 10: structural assertion for the existing-folder ownership/DACL transaction.

Scan only WindowsNative.EnsurePrivateDirectory and its handle owner-reader.
The separate path owner-reader used by preference loads is outside this transaction.
Run normally for the production assertion; --self-test also plants a second open.
"""

from pathlib import Path
import re
import sys


def body(source: str, signature: str) -> str:
    start = source.index("{", source.index(signature))
    depth = 1
    end = start + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def check(source: str) -> None:
    transaction = body(source, "internal static bool EnsurePrivateDirectory(string path)")
    assert "OwnedByCurrentUser(path)" not in transaction, "owner check reopens the path before the DACL handle"
    assert len(re.findall(r"\bCreateFileW\s*\(", transaction)) == 1, "existing folder must be opened exactly once"
    assert "OwnedByCurrentUser(handle)" in transaction, "owner must be read from the held DACL handle"
    assert "SetSecurityInfo(handle," in transaction, "DACL must be set on the held owner handle"
    owner = body(source, "private static bool OwnedByCurrentUser(SafeFileHandle handle)")
    assert "ReadOwnerAndDacl(handle)" in owner, "owner reader must inspect the supplied handle"
    assert not re.search(r"\b(?:CreateFileW|OpenRelative|OwnedByCurrentUser)\s*\(", owner), "owner reader must not reopen"


def main() -> int:
    root = Path(__file__).resolve().parents[3]
    source = (root / "src/CfdWorkbench.Persistence/WindowsNative.cs").read_text(encoding="utf-8")
    try:
        check(source)
    except (AssertionError, ValueError) as error:
        print(f"FAIL Preferences_OneHeldHandle_OwnerAndDacl {error}")
        return 1
    print("PASS Preferences_OneHeldHandle_OwnerAndDacl")
    if "--self-test" in sys.argv:
        marker = "internal static bool EnsurePrivateDirectory(string path)"
        start = source.index("{", source.index(marker)) + 1
        mutant = source[:start] + "\nusing var second = CreateFileW(path, 0, 0, 0, 0, 0, 0);\n" + source[start:]
        try:
            check(mutant)
        except AssertionError as error:
            print(f"FAIL Preferences_OneHeldHandle_InjectedSecondOpen {error} (expected mutant failure)")
            print("PASS Preferences_OneHeldHandle_MutationRejected")
        else:
            print("FAIL Preferences_OneHeldHandle_MutationRejected second open escaped the assertion")
            return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
