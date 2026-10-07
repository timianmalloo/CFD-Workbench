"""Write the measured Windows SU2 smoke manifest from its frozen inputs."""
import hashlib,pathlib,yaml
p=pathlib.Path(__file__).resolve().parent
root=p.parents[2]
sha=hashlib.file_digest((p/'su2-cylinder.su2').open('rb'),'sha256').hexdigest()
cfgsha=hashlib.file_digest((p/'su2-smoke.cfg').open('rb'),'sha256').hexdigest()
case=dict(name='win-su2-smoke',description='Windows installation smoke only: incompressible laminar cylinder on a 2048-quad annular mesh; no validated physical reference or tolerance.',
purpose=dict(spike='smoke',stage=0,question='Does the pinned native Windows SU2 execute a <=5000-cell 2D incompressible case and emit finite CL history?'),
geometry=dict(airfoil='none (unit-diameter cylinder)',source=dict(kind='tutorial',path='docs/proof/win-routes/su2-cylinder.su2',sha256=sha,generator='py -3 docs/proof/win-routes/build-su2-smoke.py',reference='https://github.com/su2code/SU2/blob/v8.5.0/TestCases/incomp_navierstokes/cylinder/incomp_cylinder.cfg',deviation='Generated 2048-quad annular mesh replaces the oversized publisher mesh; 100 iteration cap, MGLEVEL=0, explicit AERO_COEFF history.')),
conditions=dict(reynolds=10.00701754385965,mach=0,alpha=0,speed_m_s=.000008,reference_length_m=1),solver='su2',
backend=dict(name='SU2 native Windows win64-omp',version='v8.5.0',build='Harrier',distribution='SU2-v8.5.0-win64-omp.zip',distribution_sha256='4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11',binary_sha256={'SU2_CFD.exe':'3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248'},activation='SU2_CFD.exe -t 1 su2-smoke.cfg',docker_used=False,image_digest=None),
mesh=dict(generator='product-structured',settings=dict(cells=2048,points=2112,angular_segments=64,radial_layers=32,cylinder_radius_m=.5,farfield_radius_m=20)),
physics=dict(application='SU2_CFD',flow='incompressible-steady',turbulence_model='NONE',fluid=dict(nu_m2_s=.000798/998.2,density_kg_m3=998.2,dynamic_viscosity_pa_s=.000798)),
boundary_conditions=dict(cylinder='MARKER_HEATFLUX (cylinder, 0.0)',farfield='MARKER_FAR (farfield)'),
numerics=dict(max_iterations=100,residual_criterion='CONV_RESIDUAL_MINVAL=-10; publisher v8.5.0 configuration with MGLEVEL=0',config_sha256=cfgsha),
decomposition=dict(method='OpenMP',n_subdomains=1,nice=10,nice_observed='Not recorded; POSIX nice is inapplicable to native Windows. 10 is the schema-required policy value, not a measured process priority.',windows_process_priority='Not recorded',max_load_1min=10),outputs_expected=['history.csv','restart_flow.dat'],run_dir='runs/'+pathlib.Path((p/'su2-run-dir.txt').read_text().strip()).name)
(root/'cases'/'win-su2-smoke.yaml').write_text(yaml.safe_dump(case,sort_keys=False),encoding='utf-8')
