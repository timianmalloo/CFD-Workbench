"""List every module in a minidump with its base and size. Usage: python -I list_modules.py <file.dmp>"""
import sys

sys.path.insert(0, __file__.rsplit("\\", 1)[0].rsplit("/", 1)[0])
from minidump_native_stack import Dump  # noqa: E402

with open(sys.argv[1], "rb") as f:
    d = Dump(f)
    for base, size, name in sorted(d.modules, key=lambda m: m[2].lower()):
        print(f"{base:#018x} {size:#10x} {name}")
