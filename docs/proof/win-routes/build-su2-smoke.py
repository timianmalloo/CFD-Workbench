"""Generate a 2048-quad annular cylinder mesh for installation smoke only."""
import math, pathlib
p = pathlib.Path(__file__).resolve().parent
n, rings = 64, 32
with (p/'su2-cylinder.su2').open('w',encoding='ascii') as f:
    f.write(f'NDIME= 2\nNELEM= {n*rings}\n')
    for j in range(rings):
        for i in range(n):
            k=(i+1)%n
            f.write(f'9 {j*n+i} {(j+1)*n+i} {(j+1)*n+k} {j*n+k}\n')
    f.write(f'NPOIN= {n*(rings+1)}\n')
    for j in range(rings+1):
        r=.5*(40**(j/rings))
        for i in range(n):
            a=2*math.pi*i/n
            f.write(f'{r*math.cos(a):.16g} {r*math.sin(a):.16g} {j*n+i}\n')
    f.write('NMARK= 2\n')
    for tag,j in [('cylinder',0),('farfield',rings)]:
        f.write(f'MARKER_TAG= {tag}\nMARKER_ELEMS= {n}\n')
        for i in range(n): f.write(f'3 {j*n+i} {j*n+(i+1)%n}\n')
cfg=(p/'su2-smoke.cfg').read_text()
cfg=cfg.replace('ITER= 5000','ITER= 100').replace('MGLEVEL= 3','MGLEVEL= 0').replace('mesh_cylinder_lam.su2','su2-cylinder.su2')
cfg+='\nHISTORY_OUTPUT= (ITER, RMS_RES, AERO_COEFF)\nOUTPUT_FILES= (RESTART)\n'
(p/'su2-smoke.cfg').write_text(cfg,encoding='ascii')
