#!/usr/bin/env python3
"""Assemble an already-built desktop output; does not build, sign or alter trust."""
from __future__ import annotations

import argparse
import json
import pathlib
import plistlib
import shutil
import stat


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-dir", type=pathlib.Path, required=True)
    parser.add_argument("--output-dir", type=pathlib.Path, required=True)
    parser.add_argument("--platform", choices=("macos", "windows"), required=True)
    options = parser.parse_args()
    source = options.build_dir.resolve(strict=True)
    output = options.output_dir.resolve()
    if output.exists() and any(output.iterdir()):
        parser.error("output directory must be empty; no existing package is overwritten")
    if not (source / "CfdWorkbench.Desktop.dll").is_file():
        parser.error("desktop assembly is absent from build directory")
    repo = pathlib.Path(__file__).resolve().parents[1]
    if options.platform == "macos":
        executable = source / "CfdWorkbench.Desktop"
        if not executable.is_file():
            parser.error("macOS apphost is absent from build directory")
        app = output / "CFD Workbench.app"
        macos = app / "Contents" / "MacOS"
        shutil.copytree(source, macos, symlinks=False)
        plist = repo / "src" / "CfdWorkbench.Desktop" / "Info.plist"
        with plist.open("rb") as handle:
            identity = plistlib.load(handle)["CFBundleIdentifier"]
        shutil.copy2(plist, app / "Contents" / "Info.plist")
        (macos / "CfdWorkbench.Desktop").chmod(
            (macos / "CfdWorkbench.Desktop").stat().st_mode | stat.S_IXUSR)
        print(json.dumps({"bundle": str(app), "bundleId": identity, "signed": False,
                          "runtime": "self-contained" if (source / "libcoreclr.dylib").is_file() else "framework-dependent"}))
    else:
        bundle = output / "CFD Workbench Windows"
        shutil.copytree(source, bundle, symlinks=False)
        if not (bundle / "CfdWorkbench.Desktop.exe").is_file():
            parser.error("Windows executable is absent from build directory")
        print(json.dumps({"bundle": str(bundle), "signed": False,
                          "runtime": "self-contained" if (source / "coreclr.dll").is_file() else "framework-dependent"}))


if __name__ == "__main__":
    main()
