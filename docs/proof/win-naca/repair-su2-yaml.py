"""Repair schema metadata without touching the running SU2 fixture or its frozen config."""
import datetime,hashlib,json,pathlib,runpy,yaml
root=pathlib.Path(__file__).resolve().parents[3]; proof=pathlib.Path(__file__).resolve().parent
target=root/'cases/win-su2-tmr-naca0012.yaml'; original=target.read_bytes()
archive=proof/'su2-prelaunch-case.yaml'; assert not archive.exists(); archive.write_bytes(original)
case=yaml.safe_load(original); state=json.loads((proof/'su2-frozen.json').read_bytes())
old_hash=hashlib.sha256(original).hexdigest()
assert old_hash==next(row['sha256'] for row in state['files'] if row['path']==target.relative_to(root).as_posix())
case['numerics']['residual_criterion']=case['numerics']['convergence']
case['decomposition']['nice']=10
case['decomposition']['nice_platform_note']='POSIX nice inapplicable; Windows BELOW_NORMAL_PRIORITY_CLASS requested'
target.write_bytes(yaml.safe_dump(case,sort_keys=False,allow_unicode=True).encode())
validation=runpy.run_path(str(root/'cases/tools/validate-cases.py'))
assert not validation['errors_for'](validation['load_validator'](),case,target.stem)
for row in state['files']:
    if row['path']!=target.relative_to(root).as_posix(): assert hashlib.sha256((root/row['path']).read_bytes()).hexdigest()==row['sha256'],row['path']
row=dict(repair=1,scope='YAML schema metadata only; no solver/mesh/config/run change or rerun',observed_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),prelaunch_yaml_archive=archive.relative_to(root).as_posix(),prelaunch_yaml_sha256=old_hash,final_yaml=target.relative_to(root).as_posix(),final_yaml_sha256=hashlib.sha256(target.read_bytes()).hexdigest(),fields_changed=['numerics.residual_criterion (same predeclared convergence text)','decomposition.nice (schema integer)','decomposition.nice_platform_note'])
(proof/'su2-schema-repair.json').write_bytes((json.dumps(row,indent=2)+'\n').encode())
print(json.dumps(row))
