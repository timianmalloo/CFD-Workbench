#!/bin/bash
# Second closing-tip planform (linear taper to a point, AR 8) and the flat genuine-excess falsifier (rect alpha 18).
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for n in 16 32 64 128 256; do A+=("tri:$n:5:0" "rect:$n:18:0" "ell:$n:14:0"); done
dotnet "$S/probe/bin/Release/net10.0/probe.dll" "${A[@]}" > "$S/falsify2.csv"
