"""Read-only SV5_04 package/local byte checks; no native MCP/Unity execution."""
from pathlib import Path
import argparse,csv,hashlib,json,re,sys
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text(encoding='utf-8'))
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--manifest-sha',required=True)
    ap.add_argument('--local-precheck',action='store_true');ap.add_argument('--map-root');ap.add_argument('--prior-result')
    a=ap.parse_args();inp=Path(__file__).resolve().parent;package_map=inp.parents[2];errors=[]
    def need(ok,s):
        if not ok:errors.append(s)
    mf=inp/'FILES.json';need(mf.is_file(),'manifest missing')
    if mf.is_file():need(sha(mf)==a.manifest_sha,'manifest SHA mismatch')
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    for e in load(mf)['files']:
        p=(package_map/e['path']).resolve();need(p.is_relative_to(package_map),'unsafe package path')
        if not p.is_relative_to(package_map):continue
        need(p.is_file(),'input missing: '+e['path'])
        if p.is_file():
            need(sha(p)==e['sha256'] and p.stat().st_size==e['bytes'],'input bytes differ: '+e['path'])
            if p.suffix=='.md':need(len(p.read_text(encoding='utf-8').splitlines())<=300,'MD exceeds 300 lines: '+e['path'])
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    lock=load(inp/'SOURCE_LOCK.json');ref=inp/'REFERENCE_SV5_03.md';text=ref.read_text(encoding='utf-8')
    need(sha(ref)==lock['previous_result_sha256'],'previous reference SHA mismatch')
    need(bool(re.search(r'^TASK: SV5_03_BINDINGS$',text,re.M)),'wrong predecessor task')
    need(bool(re.search(r'^STATUS: PASS$',text,re.M)),'predecessor not PASS')
    start=(package_map/'MCP_INBOX/SV5_04_START.md').read_text(encoding='utf-8')
    need('SOURCE_SPEC_SHA256: '+sha(inp/'SV5_04_CORE_RESERVE.md') in start,'inbox/source mismatch')
    rc=(inp/'RESERVATION_CONTRACT.md').read_text(encoding='utf-8')
    vc=(inp/'VALIDATION_CONTRACT.md').read_text(encoding='utf-8')
    need(set(re.findall(r'^## (C\d{2})\.',rc,re.M))=={f'C{i:02}' for i in range(1,9)},'reservation sections missing')
    need(set(re.findall(r'^\| (T\d{2}) \|',vc,re.M))=={f'T{i:02}' for i in range(1,9)},'validation responsibilities missing')
    known={e['path']:e['sha256'] for e in lock['files']}
    need(all(p.startswith('MCP/GENERATED/SV5_03/') and p in known for p in lock['audit_tables']),'unlocked or wrong-base audit table')
    if a.local_precheck:
        mr=Path(a.map_root).resolve() if a.map_root else package_map
        need(bool(a.prior_result),'--prior-result must be the actual local Result')
        for e in lock['files']:
            p=(mr/e['path']).resolve();need(p.is_relative_to(mr),'unsafe local path')
            if not p.is_relative_to(mr):continue
            need(p.is_file(),'local prerequisite missing: '+str(p))
            if p.is_file():need(sha(p)==e['sha256'],'local prerequisite SHA mismatch: '+str(p))
        for rel,count in lock['audit_tables'].items():
            p=mr/rel
            if p.is_file():
                with p.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
                need(len(rows)==count,'audit row count mismatch: '+rel)
                if p.name=='TASK_COVERAGE.csv':
                    selected=[r for r in rows if r.get('task_id')=='SV5_04_CORE_RESERVE']
                    need(len(selected)==1,'SV5_04 coverage entry missing/duplicate')
                    if selected:need(selected[0].get('readiness') in ('READ_READY','NEEDS_IMPLEMENTATION'),'SV5_04 coverage not ready: '+str(selected[0].get('readiness')))
        if a.prior_result:
            rp=Path(a.prior_result).resolve();need(rp.is_file(),'actual previous Result missing')
            need(not rp.is_relative_to((mr/'MCP/INPUTS').resolve()) and not rp.is_relative_to((package_map/'MCP/INPUTS').resolve()),'input reference cannot replace actual Result')
            if rp.is_file():need(sha(rp)==lock['previous_result_sha256'],'actual previous Result SHA mismatch')
    print(json.dumps({'status':('PASS_LOCAL_BYTES_ONLY' if a.local_precheck else 'PASS_PACKAGE_ONLY') if not errors else 'FAIL',
      'reservation_sections':8,'focused_validation_responsibilities':8,'native_status_finalize_commit_checked':False,
      'actual_API_binding_verified_here':False,'mcp_applied':False,'unity_run':False,'errors':errors},ensure_ascii=False,indent=2))
    return 1 if errors else 0
if __name__=='__main__':
    try:sys.exit(main())
    except Exception as e:print(json.dumps({'status':'FAIL','error':str(e)}));sys.exit(1)
