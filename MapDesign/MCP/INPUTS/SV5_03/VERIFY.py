"""Read-only input/byte validator. Native MCP status/Finalize/commit is checked separately."""
from pathlib import Path
import argparse,hashlib,json,re,sys
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p): return json.loads(p.read_text(encoding='utf-8'))
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--manifest-sha',required=True)
    ap.add_argument('--local-precheck',action='store_true')
    ap.add_argument('--map-root');ap.add_argument('--prior-result')
    args=ap.parse_args();inp=Path(__file__).resolve().parent
    package_map=inp.parents[2];manifest=inp/'FILES.json';errors=[]
    def need(ok,msg):
        if not ok:errors.append(msg)
    need(manifest.is_file(),'manifest missing')
    if manifest.is_file():need(sha(manifest)==args.manifest_sha,'manifest SHA mismatch')
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    for e in load(manifest)['files']:
        p=(package_map/e['path']).resolve()
        need(p.is_relative_to(package_map),'unsafe manifest path: '+e['path'])
        if not p.is_relative_to(package_map):continue
        need(p.is_file(),'missing input: '+e['path'])
        if p.is_file():
            need(sha(p)==e['sha256'] and p.stat().st_size==e['bytes'],'input byte mismatch: '+e['path'])
            if p.suffix=='.md':need(len(p.read_text(encoding='utf-8').splitlines())<=300,'MD exceeds 300 lines: '+e['path'])
    if errors:print(json.dumps({'status':'FAIL','errors':errors}));return 1
    lock=load(inp/'SOURCE_LOCK.json');scope=load(inp/'AUDIT_SCOPE.json')
    report=inp/'REFERENCE_SV5_02.md'
    need(sha(report)==lock['previous_result_sha256'],'predecessor reference SHA mismatch')
    text=report.read_text(encoding='utf-8')
    need(bool(re.search(r'^TASK: SV5_02_RULES$',text,re.M)),'wrong predecessor task')
    need(bool(re.search(r'^STATUS: PASS$',text,re.M)),'predecessor not PASS')
    spec=inp/'SV5_03_BINDINGS.md'
    start=(package_map/'MCP_INBOX/SV5_03_START.md').read_text(encoding='utf-8')
    need('SOURCE_SPEC_SHA256: '+sha(spec) in start,'inbox/source specification mismatch')
    ids=[x['audit_id'] for x in scope['groups']]
    need(len(ids)==13 and len(set(ids))==13,'audit groups not 13 unique')
    expected=scope['expected_future_tasks']
    need(len(expected)==42 and len(set(expected))==42,'future tasks not 42 unique')
    need({int(x.split('_')[1]) for x in expected}==set(range(4,46)),'future task sequence mismatch')
    need({t for g in scope['groups'] for t in g['consumer_tasks']}==set(expected),'future task coverage mismatch')
    need(all(e['path'].startswith(('MCP/','MCP_ARCHIVE/')) for e in lock['files']),'unexpected lock path base')
    if args.local_precheck:
        mr=Path(args.map_root).resolve() if args.map_root else package_map
        need(bool(args.prior_result),'resolve actual local predecessor Result with --prior-result')
        for e in lock['files']:
            p=(mr/e['path']).resolve()
            need(p.is_relative_to(mr),'unsafe local prerequisite path')
            if not p.is_relative_to(mr):continue
            need(p.is_file(),'missing local prerequisite: '+str(p))
            if p.is_file():need(sha(p)==e['sha256'],'local prerequisite SHA mismatch: '+str(p))
        if args.prior_result:
            rp=Path(args.prior_result).resolve()
            need(rp.is_file(),'actual local Result missing')
            need(not rp.is_relative_to((mr/'MCP/INPUTS').resolve()) and not rp.is_relative_to((package_map/'MCP/INPUTS').resolve()),'input reference is not actual Result')
            if rp.is_file():need(sha(rp)==lock['previous_result_sha256'],'actual previous Result SHA mismatch')
    print(json.dumps({'status':('PASS_LOCAL_BYTES_ONLY' if args.local_precheck else 'PASS_PACKAGE_ONLY') if not errors else 'FAIL',
        'audit_groups':len(ids),'future_tasks':len(expected),'native_status_finalize_commit_checked':False,
        'source_audit_performed':False,'mcp_applied':False,'unity_run':False,'errors':errors},ensure_ascii=False,indent=2))
    return 1 if errors else 0
if __name__=='__main__':
    try:sys.exit(main())
    except Exception as exc:print(json.dumps({'status':'FAIL','error':str(exc)}));sys.exit(1)
