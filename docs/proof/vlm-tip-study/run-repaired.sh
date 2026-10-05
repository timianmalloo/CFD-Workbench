#!/bin/bash
set -euo pipefail
S="$(cd "$(dirname "$0")" && pwd)"
A=()
for w in ell rect tap rectc; do
  for n in 16 32 64 128 256; do
    for a in 2 4 5 8; do A+=("$w:$n:$a:0"); done
  done
done
for n in 16 32 64 128 256; do
  for a in 2 4 5 8; do A+=("rect:$n:$a:1"); done
done
dotnet build "$S/probe/probe.csproj" -c Release -o "$S/probe/bin/repaired"
dotnet "$S/probe/bin/repaired/probe.dll" "${A[@]}" > "$S/repaired-1.1.0.csv"
dotnet "$S/probe/bin/repaired/probe.dll" \
  ell:16:14:0 ell:32:14:0 ell:64:14:0 ell:128:14:0 ell:256:14:0 \
  rect:16:18:0 rect:32:18:0 rect:64:18:0 rect:128:18:0 rect:256:18:0 \
  > "$S/repaired-falsifiers-1.1.0.csv"
