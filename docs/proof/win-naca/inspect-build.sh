#!/bin/bash
set -o pipefail
source /usr/lib/openfoam/openfoam2512/etc/bashrc
set -eu
printf 'Build: %s\nAppbin: %s\nLibbin: %s\n' "$WM_PROJECT_VERSION" "$FOAM_APPBIN" "$FOAM_LIBBIN"
sha256sum "$FOAM_APPBIN/simpleFoam" "$FOAM_APPBIN/checkMesh" "$FOAM_APPBIN/decomposePar" "$FOAM_APPBIN/reconstructPar"
"$FOAM_APPBIN/simpleFoam" -help
dpkg-query -W openfoam2512 openfoam2512-common
find "$FOAM_LIBBIN" -maxdepth 1 -iname '*gpu*' -o -iname '*cuda*' -o -iname '*fused*' -o -iname '*umpire*'
ldd "$FOAM_APPBIN/simpleFoam"
if test -d "$WM_PROJECT_DIR/src"; then
  rg -n 'CUDA|HIP|Umpire|fused|useGPU' "$WM_PROJECT_DIR/src" "$WM_PROJECT_DIR/etc" || true
else
  echo 'Installed runtime has no src tree; source-level supported offload contract Not recorded.'
  grep -R -n -E 'CUDA|HIP|Umpire|fused|useGPU' "$WM_PROJECT_DIR/etc" || true
fi
/usr/lib/wsl/lib/nvidia-smi --query-gpu=name,driver_version,memory.total --format=csv
