"""Read-only SV5_02 package check, plus optional LOCAL PRE-APPLY checks.
All text explicitly uses UTF-8. Native state/Finalize/commit checks remain MCP's responsibility.
"""
from pathlib import Path
import argparse,csv,hashlib,json,re,sys
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text(encoding='utf8'))
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--manifest-sha',required=True)
    ap.add_argument('--local-precheck',action='store_true');ap.add_argument('--map-root');ap.add_argument('--prior-result')
    args=ap.parse_args();root=Path(__file__).resolve().parent;errors=[]
    def need(ok,why):
        if not ok:errors.append(why)
    mf=root/'SV5_02_FILES.json';need(mf.is_file(),'manifest missing')
    if mf.is_file():need(sha(mf)==args.manifest_sha,'manifest SHA mismatch')
    if errors:print(json.dumps({'status':'FAIL','errors':errors},ensure_ascii=False));return 1
    for e in load(mf)['files']:
        p=(root/e['path']).resolve();need(p.is_relative_to(root),'unsafe path: '+e['path'])
        if not p.is_relative_to(root):continue
        need(p.is_file(),'missing: '+e['path'])
        if not p.is_file():continue
        need(sha(p)==e['sha256'] and p.stat().st_size==e['bytes'],'bytes mismatch: '+e['path'])
        if p.suffix=='.md':need(len(p.read_text(encoding='utf8').splitlines())<=300,'over 300 lines: '+e['path'])
    inp=root/'MapDesign/MCP/INPUTS/SV5_02';lock=load(inp/'SOURCE_LOCK.json')
    need(sha(inp/'reference/SV5_01_RESULT.md')==lock['previous_result_sha256'],'attached Result mismatch')
    report=(inp/'reference/SV5_01_RESULT.md').read_text(encoding='utf8')
    need(bool(re.search(r'^STATUS: PASS$',report,re.M)),'attached Result not PASS')
    source=inp/'reference/RULES_SOURCE.md';lines=source.read_text(encoding='utf8').splitlines()
    with (inp/'candidate/04_RULE_COVERAGE_V5.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
    meaningful={i for i,s in enumerate(lines,1) if s and not s.startswith(('#','| 대상 |','|---','상태:'))}
    need({int(r['source_line']) for r in rows}==meaningful,'source sentence coverage incomplete')
    need(len(rows)==len(meaningful),'duplicate sentence coverage')
    for r in rows:
        need(r['statement']==lines[int(r['source_line'])-1],'source statement changed: '+r['rule_id'])
        need(r['source_sha256']==sha(source),'source version mismatch: '+r['rule_id'])
    jumps={r['rule_id']:r for r in rows if r['rule_id'].startswith('JUMP-')}
    need(set(jumps)=={f'JUMP-{i:02}' for i in range(1,15)},'JUMP 01-14 mismatch')
    need(all(jumps[i]['classification']=='AUTHORING_DEFAULT' for i in ('JUMP-07','JUMP-10')),'authoring defaults promoted')
    need(jumps['JUMP-05']['classification']=='UNSET_NUMERIC_RATIO','solid ratio invented')
    need(any(r['classification']=='UNVERIFIED_MOVEMENT_CANDIDATE' for r in rows),'jump candidates promoted')
    readset=load(inp/'candidate/05_RULE_READSET_V5.json');known={e['path']:e['sha256'] for e in lock['files']}
    for g in readset['groups']:
        for s in g['sources']:need(known.get(s['path'])==s['sha256'],'readset source not locked: '+s['path'])
    if args.local_precheck:
        need(bool(args.prior_result),'--prior-result must be the actual local contract Result')
        mr=Path(args.map_root).resolve() if args.map_root else root/'MapDesign'
        for e in lock['files']:
            p=(mr/e['path']).resolve();need(p.is_relative_to(mr),'unsafe locked path')
            if not p.is_relative_to(mr):continue
            need(p.is_file(),'local prerequisite missing: '+str(p))
            if p.is_file():need(sha(p)==e['sha256'],'local prerequisite SHA mismatch: '+str(p))
        if args.prior_result:
            p=Path(args.prior_result).resolve();need(p.is_file(),'local Result missing')
            need(not p.is_relative_to(inp),'reference copy cannot replace actual Result')
            if p.is_file():need(sha(p)==lock['previous_result_sha256'],'actual previous Result SHA mismatch')
    print(json.dumps({'status':('PASS_LOCAL_BYTES_ONLY' if args.local_precheck else 'PASS_PACKAGE_ONLY') if not errors else 'FAIL',
      'source_statements':len(rows),'jump_rules':len(jumps),'native_finalize_commit_checked':False,
      'unity_run':False,'mcp_installed':False,'errors':errors},ensure_ascii=False,indent=2))
    return 1 if errors else 0
if __name__=='__main__':
    try:sys.exit(main())
    except Exception as e:print(json.dumps({'status':'FAIL','error':str(e)},ensure_ascii=False));sys.exit(1)
