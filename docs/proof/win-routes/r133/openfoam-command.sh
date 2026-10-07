#!/bin/bash
# Manual W-3 M1 command adapter; source only publisher activation under a clean environment.
set -o pipefail
case_dir="$1"
app="$2"
case "$app" in blockMesh|checkMesh|icoFoam) ;; *) exit 2 ;; esac
source /usr/lib/openfoam/openfoam2512/etc/bashrc
set -eu
test -x "$FOAM_APPBIN/$app"
export HOME="$case_dir/product-home"
export FOAM_CONTROLDICT=$(cat "$HOME/.OpenFOAM/2512/controlDict")
export OMP_NUM_THREADS=1
export OPENBLAS_NUM_THREADS=1
exec /usr/bin/time -v -o "$case_dir/time.$app" /usr/bin/timeout --signal=TERM 60s /usr/bin/nice -n 10 "$FOAM_APPBIN/$app" -case "$case_dir"
