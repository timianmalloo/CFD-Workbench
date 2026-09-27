#!/bin/zsh
# Build and run the S8 split-drop probe; raw output lands in probe-output.txt beside this script.
set -e
cd "$(dirname "$0")"
dotnet build -nologo -v q
dotnet bin/Debug/net10.0/DockSplitProbe.dll > probe-output.txt 2>&1
echo "probe exit=$?"
