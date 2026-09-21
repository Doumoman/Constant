"""Read-only verification of SV5 input bytes and approved static baseline.
Run from any cwd: python SV5_VERIFY.py --manifest-sha <external expected SHA>
Does not install tasks, edit state, launch Unity, or commit.
"""
from pathlib import Path
import argparse,csv,hashlib,json,re,sys,zipfile
from collections import Counter

def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--manifest-sha',required=True)
    args=ap.parse_args();root=Path(__file__).resolve().parent
    errors=[];result={'scope':'LOCAL_PACKAGE_AND_STATIC_INPUT_ONLY','unity_run':False,'mcp_installed':False}
    def require(ok,label):
        if not ok:errors.append(label)
    mf=root/'SV5_FILES.json'
    require(bool(re.fullmatch('[0-9a-f]{64}',args.manifest_sha)),'invalid external manifest SHA')
    require(mf.is_file(),'SV5_FILES.json missing')
    if not errors:require(sha(mf)==args.manifest_sha,'manifest SHA mismatch')
    if errors:
        print(json.dumps({'status':'FAIL','errors':errors},ensure_ascii=False));return 1
    manifest=json.loads(mf.read_text(encoding='utf8'))
    for ent in manifest['files']:
        rel=Path(ent['path']);p=(root/rel).resolve()
        require(not rel.is_absolute() and p.is_relative_to(root),'unsafe path: '+str(rel))
        if rel.is_absolute() or not p.is_relative_to(root):continue
        require(p.is_file(),'missing: '+str(rel))
        if not p.is_file():continue
        require(p.stat().st_size==ent['bytes'],'size mismatch: '+str(rel))
        require(sha(p)==ent['sha256'],'SHA mismatch: '+str(rel))
        if p.suffix=='.md':
            require(len(p.read_text(encoding='utf8').splitlines())<=300,'MD exceeds 300 lines: '+str(rel))
    if errors:
        print(json.dumps({'status':'FAIL','errors':errors},ensure_ascii=False,indent=2));return 1
    inp=root/'MapDesign/MCP/INPUTS/SV5';base=inp/'baseline'
    original=json.loads((inp/'SHA256.json').read_text())
    for rel,expected in original.items():require(sha(inp/rel)==expected,'original plan changed: '+rel)
    bs=json.loads((base/'BASELINE_SHA.json').read_text())
    for rel,ent in bs['artifacts'].items():
        p=inp/rel if rel.startswith('baseline/') else base/rel
        require(p.is_file(),'approved artifact missing: '+rel)
        if p.is_file():require(sha(p)==ent['sha256'] and p.stat().st_size==ent['bytes'],'approved artifact mismatch: '+rel)
    rows=json.loads((inp/'TASKS.json').read_text())
    with (inp/'TASKS.csv').open(encoding='utf-8-sig',newline='') as f:csvrows=list(csv.DictReader(f))
    ids=[r['task_id'] for r in rows];require(len(rows)==45 and len(set(ids))==45,'45 unique Tasks required')
    require([r['order'] for r in rows]==list(range(1,46)),'JSON Task order mismatch')
    require([r['task_id'] for r in csvrows]==ids,'CSV/JSON Task mismatch')
    require([int(r['order']) for r in csvrows]==list(range(1,46)),'CSV Task order mismatch')
    md=(inp/'SPACE_V5_TASKS.md').read_text()
    mdids=re.findall(r'^\| \d{2} \| (SV5_\d{2}_[A-Z_]+) \|',md,re.M)
    require(mdids==ids,'MD/JSON Task mismatch')
    regions=json.loads((base/'regions.json').read_text());regionids={r['id'] for r in regions}
    seen=bytearray(624*416);counts=Counter();total=0
    with (base/'world_cells.csv').open(encoding='utf8',newline='') as f:
        for row in csv.DictReader(f):
            x,y=int(row['x']),int(row['y']);total+=1
            if not(0<=x<624 and 0<=y<416):errors.append('cell out of bounds');continue
            k=y*624+x
            if seen[k]:errors.append('duplicate cell')
            seen[k]=1;counts[row['cell']]+=1
            require(row['cell'] in 'ASOR' and len(row['cell'])==1,'invalid tile')
            require(int(row['region'])==0 or int(row['region']) in regionids,'unknown region')
    require(total==624*416 and all(seen),'incomplete full-world coordinates')
    v=json.loads((base/'validation.json').read_text())
    require(v['size']==[624,416] and v['physics']=='NOT_RUN','baseline execution status changed')
    for code,key in [('A','AIR'),('S','SOLID'),('O','ONE_WAY'),('R','RESERVED')]:require(counts[code]==v['tile_counts'][key],'cell count mismatch: '+key)
    require(len(regions)==101,'baseline region count')
    require(len(json.loads((base/'connections.json').read_text()))==166,'baseline connection count')
    with zipfile.ZipFile(base/'SPACE_EXAMPLE_624x416.zip') as z:
        require(z.testzip() is None,'approved source ZIP CRC failure')
        for name in ('world_cells.csv','regions.json','connections.json','validation.json'):
            inside=z.read('SPACE_EXAMPLE/data/'+name)
            require(hashlib.sha256(inside).hexdigest()==sha(base/name),'source ZIP/data mismatch: '+name)
        for name in ('SPACE_EXAMPLE_624x416.pdf','SPACE_EXAMPLE_624x416.png','SPACE_EXAMPLE_DETAIL.png'):
            require(hashlib.sha256(z.read(name)).hexdigest()==sha(base/name),'source ZIP/visual mismatch: '+name)
    result.update(status='PASS_LOCAL_INPUTS' if not errors else 'FAIL',files_checked=len(manifest['files']),
                  task_count=len(rows),cells=total,tile_counts=dict(counts),regions=len(regions),
                  manifest_sha256=sha(mf),inbox_sha256=sha(root/'MapDesign/MCP_INBOX/SV5_START.md'),
                  source_spec_sha256=sha(inp/'tasks/SV5_01_APPROVAL_BASELINE.md'),errors=errors)
    print(json.dumps(result,ensure_ascii=False,indent=2));return 0 if not errors else 1

if __name__=='__main__':
    try:sys.exit(main())
    except Exception as e:
        print(json.dumps({'status':'FAIL','error':str(e)},ensure_ascii=False));sys.exit(1)
