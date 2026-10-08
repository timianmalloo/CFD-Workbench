"""Evaluate the declared SU2 oracle and compare admitted window means with the exact CFL3D level."""
import csv,hashlib,json,pathlib
root=pathlib.Path(__file__).resolve().parents[3]; proof=pathlib.Path(__file__).resolve().parent
history=proof/'su2-results/history.csv'
with history.open(encoding='utf-8',newline='') as stream:
    reader=csv.DictReader(stream,skipinitialspace=True)
    rows=[{key.strip():float(value) for key,value in row.items()} for row in reader]
assert len(rows)>=2000,len(rows)
window=rows[-2000:]
residuals={key:rows[0][key]-max(row[key] for row in rows[-2:]) for key in rows[0] if key.startswith('rms[')}
assert len(residuals)==5,residuals
qois={}
for q in ['CL','CD']:
    values=[row[q] for row in window]
    qois[q]=dict(window_mean=sum(values)/len(values),half_band=(max(values)-min(values))/2,last=values[-1],cap=1e-5 if q=='CL' else 1e-6)
admitted=all(drop>=3 for drop in residuals.values()) and all(row['half_band']<=row['cap'] for row in qois.values())
ref=root/'docs/proof/spike-04/reference/cfl3d_results_sa_nopv_withN.dat'
zone=False; reference=None
for line in ref.read_text(encoding='utf-8').splitlines():
    if 'Family ' in line: zone='Family II,' in line
    elif zone:
        fields=line.split()
        if fields and fields[0]=='14336': reference=dict(CL=float(fields[2]),CD=float(fields[3]))
assert reference is not None
comparison={q:dict(su2_mean=row['window_mean'] if admitted else 'Not admitted',cfl3d=reference[q],difference=row['window_mean']-reference[q] if admitted else 'Not admitted',relative_difference=(row['window_mean']-reference[q])/reference[q] if admitted else 'Not admitted') for q,row in qois.items()}
resources=json.loads((proof/'su2-results/resources.json').read_bytes())
report=dict(rows=len(rows),final_inner_iteration=rows[-1]['Inner_Iter'],window_first_iteration=window[0]['Inner_Iter'],window_last_iteration=window[-1]['Inner_Iter'],residual_drop_log10_orders=residuals,qois=qois,declared_iterative_oracle='MET' if admitted else 'NOT MET',comparison=comparison,exit_code=resources['exit_code'],history_sha256=hashlib.sha256(history.read_bytes()).hexdigest(),reference_sha256=hashlib.sha256(ref.read_bytes()).hexdigest(),scope='Code-to-code at Family II L6; numerical/reference uncertainty Not recorded, no quantified acceptance tolerance, no physical validation or GCI; positivity/clipping field history Not recorded')
(proof/'su2-comparison.json').write_bytes((json.dumps(report,indent=2)+'\n').encode())
print(json.dumps(report,indent=2))
