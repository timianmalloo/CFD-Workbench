#!/bin/bash
# One run set: 60 decision cases + 12 control cases (r = 1.0). Output: sweep.csv
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for r in 0.01 0.02 0.05 0.1 0.25 1.0; do for a in 5 8 18; do for n in 32 64 128 256; do A+=("$r:$n:$a"); done; done; done
dotnet "$S/probe/bin/Release/net10.0/probe.dll" "${A[@]}" > "$S/sweep.csv"
