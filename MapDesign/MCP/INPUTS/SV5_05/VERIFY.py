"""SV5_05 read-only package/local-byte check. No native state, C# execution or Player proof."""
from pathlib import Path
import argparse,csv,hashlib,json,re,sys
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text(encoding='utf-8'))
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--manifest-sha',required=True)
    ap.add_argument('--local-precheck',action='store_true');ap.add_argument('--map-root');ap.add_argument('--project-root');ap.add_argument('--prior-result')
    a=ap.parse_args();inp=Path(__file__).resolve().parent;package_map=inp.parents[2];errors=[]
    def need(ok,s):
        if not ok:errors.append(s)
    mf=inp/'FILES.json';need(mf.is_file(),'manifest missing')
    if mf.is_file():need(sha(mf)==a.manifest_sha,'manifest SHA mismatch')
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    for e in load(mf)['files']:
        p=(package_map/e['path']).resolve();need(p.is_relative_to(package_map),'unsafe package path')
        if not p.is_relative_to(package_map):continue
        need(p.is_file(),'missing input: '+e['path'])
        if p.is_file():
            need(sha(p)==e['sha256'] and p.stat().st_size==e['bytes'],'input byte mismatch: '+e['path'])
            if p.suffix=='.md':need(len(p.read_text(encoding='utf-8').splitlines())<=300,'MD over 300 lines: '+e['path'])
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    lock=load(inp/'SOURCE_LOCK.json');findings=load(inp/'REVIEW_FINDINGS.json')
    ref=inp/'REFERENCE_SV5_04.md';text=ref.read_text(encoding='utf-8')
    need(sha(ref)==lock['previous_result_sha256']==findings['previous_result_sha256'],'previous Result SHA mismatch')
    need(bool(re.search(r'^TASK: SV5_04_CORE_RESERVE$',text,re.M)),'wrong predecessor task')
    need(bool(re.search(r'^STATUS: PASS$',text,re.M)),'predecessor not PASS')
    start=(package_map/'MCP_INBOX/SV5_05_START.md').read_text(encoding='utf-8')
    need('SOURCE_SPEC_SHA256: '+sha(inp/'SV5_05_ROUTE_STATE.md') in start,'inbox/source SHA mismatch')
    need(set(re.findall(r'^## (C\d{2})\.',(inp/'STATE_CONTRACT.md').read_text(encoding='utf-8'),re.M))=={f'C{i:02}' for i in range(1,10)},'state contract coverage')
    need(set(re.findall(r'^\| (T\d{2}) \|',(inp/'VALIDATION_CONTRACT.md').read_text(encoding='utf-8'),re.M))=={f'T{i:02}' for i in range(1,11)},'test responsibility coverage')
    w=findings['air_contact_witness'];points=[tuple(p) for p in w['cells']]
    need(len(points)==110 and w['edge_count']==109,'witness length mismatch')
    need(all(abs(p[0]-q[0])+abs(p[1]-q[1])==1 for p,q in zip(points,points[1:])),'witness discontinuity')
    need(not set(points).intersection(tuple(p) for p in w['excluded_sealed_cells']),'witness enters excluded gate')
    need(len(findings['contacts'])==3,'review contact candidates missing')
    witness_check='NOT_CHECKED_AGAINST_LOCAL_DATA'
    if a.local_precheck:
        mr=Path(a.map_root).resolve() if a.map_root else package_map
        pr=Path(a.project_root).resolve() if a.project_root else mr.parent
        need(bool(a.prior_result),'--prior-result must be actual local Result')
        for e in lock['files']:
            need(e['path_base'] in ('PROJECT','MAPDESIGN'),'unknown path base')
            root=pr if e['path_base']=='PROJECT' else mr
            p=(root/e['path']).resolve();need(p.is_relative_to(root),'unsafe locked path')
            if not p.is_relative_to(root):continue
            need(p.is_file(),'local prerequisite missing: '+str(p))
            if p.is_file():need(sha(p)==e['sha256'],'local prerequisite SHA mismatch: '+str(p))
        if a.prior_result:
            rp=Path(a.prior_result).resolve();need(rp.is_file(),'actual previous Result missing')
            need(not rp.is_relative_to((mr/'MCP/INPUTS').resolve()) and not rp.is_relative_to((package_map/'MCP/INPUTS').resolve()),'input reference is not actual Result')
            if rp.is_file():need(sha(rp)==lock['previous_result_sha256'],'actual previous Result SHA mismatch')
        g=mr/'MCP/GENERATED/SV5_04'
        if all((g/n).is_file() for n in ('core_cells.csv','route_cells.csv','state_geometry.csv','access_bindings.csv')):
            def rows(n):
                with (g/n).open(encoding='utf-8-sig',newline='') as f:return list(csv.DictReader(f))
            rr=rows('route_cells.csv');cc=rows('core_cells.csv');ss=rows('state_geometry.csv');aa=rows('access_bindings.csv')
            air={(int(e['world_x']),int(e['world_y'])) for e in rr if e['required_base']=='A'}
            air.update((int(e['world_x']),int(e['world_y'])) for e in cc if e['base']=='A')
            air.difference_update((int(e['world_x']),int(e['world_y'])) for e in ss if e['state']=='SEALED' and e['base']=='S')
            need(all(p in air for p in points),'negative AIR witness no longer matches local data')
            accesses={e['access_id']:{tuple(map(int,v.split(','))) for v in e['open_cells'].split('|')} for e in aa}
            need(points[0] in accesses.get(w['start_access'],set()) and points[-1] in accesses.get(w['end_access'],set()),'witness endpoint mismatch')
            witness_check='STATIC_AIR_CONTACT_REPRODUCED_NOT_PLAYER_PROOF'
    print(json.dumps({'status':('PASS_LOCAL_BYTES_ONLY' if a.local_precheck else 'PASS_PACKAGE_ONLY') if not errors else 'FAIL',
        'state_contract_sections':9,'test_responsibilities':10,'baseline_air_witness':witness_check,
        'native_finalize_commit_checked':False,'Unity_run_here':False,'runtime_state_evaluated':False,'errors':errors},ensure_ascii=False,indent=2))
    return 1 if errors else 0
if __name__=='__main__':
    try:sys.exit(main())
    except Exception as e:print(json.dumps({'status':'FAIL','error':str(e)}));sys.exit(1)
