#!/bin/bash
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for w in ell rect tap; do for n in 16 32 64 128 256; do for a in 2 5 8; do A+=("$w:$n:$a:0"); done; done; done
dotnet "$S/probe/bin/Release/net10.0/probe.dll" "${A[@]}" > "$S/matrix.csv"
