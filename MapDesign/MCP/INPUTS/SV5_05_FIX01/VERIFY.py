from pathlib import Path
import argparse, hashlib, json, re, sys
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--manifest-sha',required=True)
    mode=ap.add_mutually_exclusive_group()
    mode.add_argument('--local-precheck',action='store_true')
    mode.add_argument('--post-readonly',action='store_true')
    ap.add_argument('--project-root');ap.add_argument('--map-root');ap.add_argument('--prior-result')
    a=ap.parse_args(); inp=Path(__file__).resolve().parent; package_map=inp.parents[2]; errors=[]
    def need(ok,s):
        if not ok:errors.append(s)
    mf=inp/'FILES.json'
    need(mf.is_file(),'manifest missing')
    if mf.is_file():need(sha(mf)==a.manifest_sha,'manifest SHA mismatch')
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    entries=load(mf)['files']; seen=set()
    for e in entries:
        need(e['path'] not in seen,'duplicate manifest entry');seen.add(e['path'])
        p=(package_map/e['path']).resolve()
        need(p.is_relative_to(package_map),'unsafe package path: '+e['path'])
        if not p.is_relative_to(package_map):continue
        need(p.is_file(),'missing input: '+e['path'])
        if p.is_file():
            need(sha(p)==e['sha256'] and p.stat().st_size==e['bytes'],'input byte mismatch: '+e['path'])
            if p.suffix=='.md':need(len(p.read_text(encoding='utf-8').splitlines())<=300,'MD over 300 lines')
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    lock=load(inp/'SOURCE_LOCK.json');f=load(inp/'REVIEW_FINDINGS.json')
    ref=inp/'REFERENCE_SV5_05.md';rt=ref.read_text(encoding='utf-8')
    need(sha(ref)==lock['previous_result_sha256']==f['previous_result_sha256'],'reference Result SHA mismatch')
    need(bool(re.search(r'^TASK: SV5_05_ROUTE_STATE$',rt,re.M)),'wrong previous Task')
    need(bool(re.search(r'^STATUS: PASS$',rt,re.M)),'previous Result not PASS')
    need({e['id'] for e in f['findings']}=={'FIX01-F1','FIX01-F2'},'review findings missing')
    expected='SOURCE_SPEC_SHA256: '+sha(inp/'SV5_05_FIX01.md')
    need(expected in (package_map/'MCP_INBOX/SV5_05_FIX01_START.md').read_text(encoding='utf-8'),'inbox/source SHA mismatch')
    need(f['uploaded_tests']['executed_here'] is False,'incorrect author Unity claim')
    checked=0;skipped=[]
    if a.local_precheck or a.post_readonly:
        need(bool(a.project_root and a.map_root and a.prior_result),'actual --project-root --map-root --prior-result are required')
        if a.project_root and a.map_root and a.prior_result:
            pr=Path(a.project_root).resolve();mr=Path(a.map_root).resolve();rp=Path(a.prior_result).resolve()
            for e in lock['files']:
                need(e['path_base'] in ('PROJECT','MAPDESIGN'),'unknown path base')
                if e['path_base'] not in ('PROJECT','MAPDESIGN'):continue
                root=pr if e['path_base']=='PROJECT' else mr;p=(root/e['path']).resolve()
                need(p.is_relative_to(root),'unsafe locked path')
                if not p.is_relative_to(root):continue
                if a.post_readonly and e['check'].startswith('PRE_APPLY_'):
                    skipped.append(e['path']);continue
                need(p.is_file(),'local prerequisite missing: '+str(p))
                if p.is_file():need(sha(p)==e['sha256'],'local prerequisite SHA mismatch: '+str(p));checked+=1
            need(rp==(mr/lock['previous_result_path']).resolve(),'--prior-result must be actual native Result path, not an INPUTS reference')
            need(rp.is_file(),'actual previous Result missing')
            if rp.is_file():need(sha(rp)==lock['previous_result_sha256'],'actual previous Result SHA mismatch')
    status='PASS_PACKAGE_ONLY'
    if a.local_precheck:status='PASS_LOCAL_BYTES_ONLY'
    if a.post_readonly:status='PASS_READONLY_BYTES_ONLY'
    print(json.dumps({'status':'FAIL' if errors else status,'checked_local_files':checked,
        'authorized_changes_skipped_in_post_mode':skipped,'native_registration_finalize_commit_checked':False,
        'Unity_executed':False,'candidate_logic_executed':False,'errors':errors},ensure_ascii=False,indent=2))
    return 1 if errors else 0
if __name__=='__main__':
    try:sys.exit(main())
    except Exception as e:print(json.dumps({'status':'FAIL','errors':[str(e)]}));sys.exit(1)
