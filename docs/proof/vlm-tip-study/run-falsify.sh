#!/bin/bash
# Falsifier runs: genuine tip excess (washin) and a clearly-inside closing tip, 16..256 per half, default cosine lattice.
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for n in 16 32 64 128 256; do A+=("rect:$n:5:10" "tap:$n:5:8" "ell:$n:5:3" "ell:$n:3:0" "ell:$n:4:0"); done
dotnet "$S/probe/bin/Release/net10.0/probe.dll" "${A[@]}" > "$S/falsify.csv"
