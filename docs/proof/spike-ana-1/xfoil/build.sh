#!/bin/sh
set -eu
base=/Users/mallalieut/projects/CFD-Workbench-spike-ana-1/docs/proof/spike-ana-1/xfoil
curl -L --fail --show-error --silent -o "$base/xfoil6.99.tgz" https://web.mit.edu/drela/Public/web/xfoil/xfoil6.99.tgz
echo '5c0250643f52ce0e75d7338ae2504ce7907f2d49a30f921826717b8ac12ebe40  '"$base/xfoil6.99.tgz" | shasum -a 256 -c -
tar -xzf "$base/xfoil6.99.tgz" -C "$base"
cp "$base/Xfoil/plotlib/config.make.gfortranDP" "$base/Xfoil/plotlib/config.make"
make -C "$base/Xfoil/plotlib" CFLAGS='-O2 -DUNDERSCORE -I/opt/homebrew/include' > "$base/plotlib-build.log" 2>&1
make -C "$base/Xfoil/bin" -f Makefile_gfortran xfoil \
  BINDIR="$base" INSTALLCMD='cp' PLTLIB='-L/opt/homebrew/lib -lX11' \
  FFLAGS='-O2 -fdefault-real-8 -fallow-argument-mismatch' \
  FFLOPT='-O2 -fdefault-real-8 -fallow-argument-mismatch' > "$base/xfoil-build.log" 2>&1
"$base/Xfoil/bin/xfoil" < /dev/null > "$base/version.log" 2>&1 || true
