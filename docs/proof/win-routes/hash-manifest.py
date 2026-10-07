"""Hash staged W-3 evidence bytes and verify disk/index equality; not a docs-graph writer."""
import hashlib,json,pathlib,subprocess,sys
root=pathlib.Path(__file__).resolve().parents[3]
target=root/'docs/proof/win-routes/manifest.json'
def blob(path): return subprocess.run(['git','show',':'+path],cwd=root,check=True,stdout=subprocess.PIPE).stdout
if '--check' in sys.argv:
    rows=json.loads(target.read_text())
    for row in rows:
        data=blob(row['path'])
        assert data==(root/row['path']).read_bytes(),row['path']+' disk/index mismatch'
        assert hashlib.sha256(data).hexdigest()==row['sha256'],row['path']+' hash mismatch'
    assert blob(target.relative_to(root).as_posix())==target.read_bytes()
    print(f'PASS: {len(rows)} manifest hashes equal disk and staged blob bytes; manifest disk/index equal')
else:
    files=subprocess.run(['git','ls-files','--','docs/proof/win-routes','cases/win-smoke-cavity.yaml','cases/win-su2-smoke.yaml','docs/design/guided-solver-setup.md'],cwd=root,check=True,stdout=subprocess.PIPE,text=True).stdout.splitlines()
    rows=[]
    for path in files:
        if path==target.relative_to(root).as_posix(): continue
        data=blob(path)
        assert data==(root/path).read_bytes(),path+' disk/index mismatch'
        rows.append(dict(path=path,bytes=len(data),sha256=hashlib.sha256(data).hexdigest(),staged_blob_equal=True))
    target.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
    print(f'Wrote {len(rows)} hashes of staged blob bytes')
