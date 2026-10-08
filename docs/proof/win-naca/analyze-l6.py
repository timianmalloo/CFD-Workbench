"""Compute observed cross-OS differences from the two raw A4 reports."""
import hashlib,json,pathlib,re
root=pathlib.Path(__file__).resolve().parents[3]; proof=pathlib.Path(__file__).resolve().parent
mac=root/'docs/proof/spike-04/receipts/20261004T175236Z-spike04r3-g0-l6/convergence.txt'
win=proof/'l6-convergence.txt'
reports={name:path.read_text(encoding='utf-8') for name,path in [('mac',mac),('windows',win)]}
assert all('A4=MET' in report for report in reports.values())
results={}
for q in ['Cl','Cd']:
    data={name:float(re.search(q+r'_window_mean=([0-9.e+-]+)',report).group(1)) for name,report in reports.items()}
    bands={name:float(re.search(q+r'_U_I=([0-9.e+-]+)',report).group(1)) for name,report in reports.items()}
    delta=data['windows']-data['mac']
    results[q]=dict(window_means=data,half_bands=bands,windows_minus_mac=delta,absolute_difference=abs(delta),relative_difference=delta/data['mac'],sum_iterative_half_bands=sum(bands.values()))
results['Courant']=dict(mac='Not recorded',windows='Not recorded',difference='Not recorded',reason='Exact steady simpleFoam case and archived reference do not emit Courant; no derived substitute')
results['classification']='Measured cross-OS differences at printed precision on this one G0 diagnostic; no global tolerance, GCI, physical validation or future-build admission envelope'
results['report_sha256']={name:hashlib.sha256(path.read_bytes()).hexdigest() for name,path in [('mac',mac),('windows',win)]}
(proof/'l6-comparison.json').write_bytes((json.dumps(results,indent=2)+'\n').encode())
print(json.dumps(results,indent=2))
