"""Repair only copied YAML semantics, preserving both prior versions and frozen solver inputs."""
import datetime, hashlib, json, pathlib, runpy, yaml

root = pathlib.Path(__file__).resolve().parents[3]
proof = pathlib.Path(__file__).resolve().parent
target = root / 'cases/win-su2-tmr-naca0012.yaml'
original = target.read_bytes()
old_hash = hashlib.sha256(original).hexdigest()
assert old_hash == 'e2feb2d7aaabcf0fb020ba0d89c3e5e68848de9a1b5ef35d8840f8bd4e04e179'
archive = proof / 'su2-after-schema-case.yaml'
assert not archive.exists()
archive.write_bytes(original)
case = yaml.safe_load(original)
case['purpose'] = dict(spike='SPIKE-04', stage=1, round=3, run_id='W-4b-SU2-TMR-L6',
    question='How do native SU2 v8.5.0 compressible RANS SA-noft2 Cl/Cd on NASA Family II L6 compare with the CFL3D SA-noft2 first-order-turbulence reference without point-vortex correction?')
case['conditions'].pop('speed_m_s')
case['conditions']['temperature_K'] = 300.0
case['mesh']['settings'].pop('span_m')
case['mesh']['settings']['dimensions'] = 2
case['mesh']['settings']['cells'] = 14336
case['mesh']['settings']['points'] = 14576
case['mesh']['settings']['wake_cut'] = 'Coincident wake nodes merged into internal connectivity by the SU2 converter'
case['mesh'].pop('gate')
case['physics'] = dict(application='SU2_CFD', flow='compressible-steady-RANS',
    turbulence_model='SpalartAllmaras', sa_variant='SA-noft2: SA_OPTIONS=NONE; no WITHFT2',
    wall_treatment='No-slip adiabatic airfoil; SA working variable zero at the wall',
    transition='none; fully turbulent',
    fluid=dict(name='STANDARD_AIR', temperature_K=300.0, viscosity_model='SUTHERLAND',
        conductivity_model='CONSTANT_PRANDTL'),
    freestream=dict(mach=0.15, reynolds=6000000, alpha_deg=10.0, reynolds_length_m=1.0,
        nuTilda_over_nu=3.0, specification='Reynolds-based dimensional freestream; SU2 computes density/pressure and viscosity'))
case['boundary_conditions'] = dict(
    farfield='MARKER_FAR=(farfield): SU2 compressible farfield; no point-vortex correction',
    airfoil='MARKER_HEATFLUX=(airfoil,0.0): no-slip adiabatic wall; MARKER_MONITORING/PLOTTING/DESIGNING=(airfoil)',
    dimensionality='NDIME=2; two-dimensional SU2 mesh, no OpenFOAM empty patches')
case['numerics'].update(flow_scheme='ROE, MUSCL_FLOW=YES, MUSCL_KAPPA_FLOW=0.5',
    turbulence_scheme='SCALAR_UPWIND, MUSCL_TURB=NO (first order)',
    cfl='50 fixed, CFL_ADAPT=NO', time_discretization='EULER_IMPLICIT',
    linear_solver='FGMRES, ILU, 20 iterations', restart='RESTART_SOL=NO',
    convergence_start_iteration=20000)
case['outputs_expected'] = ['history.csv', 'log.SU2_CFD', 'docs/proof/win-naca/su2-results/resources.json (wrapper measurements)']
target.write_bytes(yaml.safe_dump(case, sort_keys=False, allow_unicode=True).encode())
validation = runpy.run_path(str(root / 'cases/tools/validate-cases.py'))
errors = validation['errors_for'](validation['load_validator'](), case, target.stem)
assert not errors, errors
frozen = json.loads((proof / 'su2-frozen.json').read_bytes())
for row in frozen['files']:
    if row['path'] != target.relative_to(root).as_posix():
        assert hashlib.sha256((root / row['path']).read_bytes()).hexdigest() == row['sha256'], row['path']
config = (proof / 'su2-inputs/tmr-sa.cfg').read_text()
for token in ['SOLVER= RANS', 'MACH_NUMBER= 0.15', 'REYNOLDS_NUMBER= 6E6', 'FREESTREAM_TEMPERATURE= 300.0', 'SA_OPTIONS= NONE', 'MARKER_HEATFLUX= ( airfoil, 0.0 )']:
    assert token in config, token
row = dict(repair=2, scope='YAML semantic metadata only; unchanged mesh/config/run, zero reruns; repair cap reached',
    observed_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
    initial_archive='docs/proof/win-naca/su2-prelaunch-case.yaml',
    initial_sha256='31041c99f2f9645c459bcaa891f2df2107b8f6b388b2bf0f5b949dbec305c24d',
    intermediate_archive=archive.relative_to(root).as_posix(), intermediate_sha256=old_hash,
    final_yaml=target.relative_to(root).as_posix(), final_yaml_sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
    fields_changed=['purpose', 'conditions (remove inherited speed; add temperature)', 'mesh.settings (2D SU2 counts) and removal of inherited OpenFOAM gate', 'physics', 'boundary_conditions', 'numerics actual configuration annotations', 'outputs_expected measurement path'])
(proof / 'su2-semantic-repair.json').write_bytes((json.dumps(row, indent=2) + '\n').encode())
print(json.dumps(row))
