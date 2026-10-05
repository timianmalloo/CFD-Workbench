#!/bin/bash
# Non-planar probe: 4 % parabolic camber (no twist) and 1 deg linear washin (flat), alpha 5, 16..128 per half.
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for n in 16 32 64 128; do A+=("rectc:$n:5:0" "rect:$n:5:1" "rect:$n:5:-1"); done
dotnet "$S/probe/bin/Release/net10.0/probe.dll" "${A[@]}" > "$S/nonplanar.csv"
