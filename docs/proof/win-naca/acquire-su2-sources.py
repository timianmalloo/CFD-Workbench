"""Acquire pinned publisher config and inventory; no solver state changes."""
import hashlib,json,pathlib,urllib.request
out=pathlib.Path(__file__).resolve().parent/'sources'
out.mkdir(exist_ok=True)
urls={
 'su2-naca0012-v8.5.0.cfg':'https://raw.githubusercontent.com/su2code/SU2/v8.5.0/TestCases/rans/naca0012/turb_NACA0012_sa.cfg',
 'su2-config-template-v8.5.0.cfg':'https://raw.githubusercontent.com/su2code/SU2/v8.5.0/config_template.cfg',
 'su2-testcases-tree.json':'https://api.github.com/repos/su2code/TestCases/git/trees/master?recursive=1',
 'tmr-sa-withoutpv.html':'https://tmbwg.github.io/turbmodels/naca0012numerics_val_sa_withoutpv.html',
}
for name,url in urls.items():
    target=out/name
    assert not target.exists(),target
    with urllib.request.urlopen(url,timeout=60) as response: data=response.read()
    target.write_bytes(data)
    print(json.dumps(dict(path=name,url=url,bytes=len(data),sha256=hashlib.sha256(data).hexdigest())))
tree=json.loads((out/'su2-testcases-tree.json').read_bytes())
print('TestCases tree SHA:',tree['sha'],'truncated:',tree.get('truncated'))
print('\n'.join(row['path'] for row in tree['tree'] if any(s in row['path'].lower() for s in ['naca0012','naca_0012','tmr'])))
