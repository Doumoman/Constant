#!/usr/bin/env python3
"""SV5_06 input verifier/stager. It never applies, finalizes, commits or runs Unity."""
import argparse, collections, hashlib, json, os, re, subprocess, sys
from pathlib import Path, PurePosixPath

TASK='SV5_06_SPACE_GRAPH'
PREV='SV5_05_FIX01'
HERE=Path(__file__).resolve().parent
PREFIX='MapDesign/MCP/INPUTS/SV5_06'
STATUS='MapDesign/MCP/06_IMPLEMENTATION_STATUS.md'
MASTER='MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md'
INBOX='MapDesign/MCP_INBOX'
class Blocked(Exception): pass
def need(ok, message):
    if not ok: raise Blocked(message)
def sha(data): return hashlib.sha256(data).hexdigest()
def jsonfile(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def safe(root, relative):
    rel=PurePosixPath(relative)
    need(not rel.is_absolute() and relative and '\\' not in relative and ':' not in relative and all(x not in ('..','.') for x in rel.parts), 'Unsafe relative path: '+relative)
    path=root.joinpath(*rel.parts)
    for part in (path,*path.parents):
        if part==root: break
        need(not part.is_symlink(), 'Symlink is not an accepted task path: '+relative)
    need(path.resolve().is_relative_to(root.resolve()), 'Path escapes project: '+relative)
    return path

def check_package(expected):
    need(re.fullmatch('[0-9a-f]{64}',expected) is not None,'Expected manifest SHA is required')
    mf=HERE/'FILES.json'
    need(sha(mf.read_bytes())==expected,'Package manifest SHA mismatch')
    m=jsonfile(mf)
    need(m['task']==TASK and m['format']=='sv5_06_package_v1','Wrong package')
    paths=[x['path'] for x in m['files']]
    need(len(set(paths))==len(paths),'Duplicate package paths')
    for item in m['files']:
        p=safe(HERE,item['path'])
        need(p.is_file() and sha(p.read_bytes())==item['sha256'] and p.stat().st_size==item['bytes'],'Package bytes mismatch: '+item['path'])
    task=(HERE/(TASK+'.md')).read_text(encoding='utf-8')
    need(len(task.splitlines())<=300,'Task exceeds native 300-line limit')
    lock=jsonfile(HERE/'SOURCE_LOCK.json')
    need(lock.get('format')=='sv5_06_source_lock_v2','Use the revision with separate worktree/blob locks')
    for item in lock['files']:
        if item.get('commit_bound'):
            need(re.fullmatch('[0-9a-f]{64}',item.get('commit_blob_sha256','')) is not None,
                 'Missing exact commit blob SHA: '+item['path'])
            need(isinstance(item.get('commit_blob_bytes'),int) and item['commit_blob_bytes']>=0,
                 'Missing exact commit blob size: '+item['path'])
    header=task.split('---',2)[1]
    required=[f'  task_id: {TASK}', f'  task_file: TASKS/{TASK}.md','  requires_current_task: NONE',
      f'  requires_completed_task: {PREV}',f'    path: REPORTS/{PREV}_RESULT.md',
      f'    sha256: {lock["predecessor"]["result_sha256"]}', f'    path: TASKS/{PREV}.md',
      f'    sha256: {lock["predecessor"]["task_sha256"]}',f'  sets_current_task: {TASK}','  format: single_task_v1','    status: PASS']
    need(all(x in header.splitlines() for x in required),'Native Task header mismatch')
    need(sha((HERE/(TASK+'.md')).read_bytes())==m['bound_task_sha256'],'Bound Task mismatch')
    return lock,m

def status_state(data):
    text=data.decode('utf-8-sig')
    rows=re.findall(r'^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$',text,re.M)
    need(len(rows)==286 and len(dict(rows))==286,'Status row count/duplicate mismatch')
    current=re.findall(r'## Current Task\s+```text\s+([^\r\n]+)\s+```',text)
    need(len(current)==1,'Current block must occur exactly once')
    return dict(rows),current[0].strip(),dict(collections.Counter(v for k,v in rows))

def check_state(root,lock,post):
    data=safe(root,STATUS).read_bytes(); rows,current,counts=status_state(data)
    need(rows.get(PREV)=='COMPLETE' and rows.get('SV5_07_DIVERSITY')=='LOCKED','Predecessor/successor state mismatch')
    need(sha(safe(root,MASTER).read_bytes())==lock['master_sha256'],'Master bytes changed')
    masters=re.findall(r'^\|[^\r\n]*\b'+TASK+r'\b[^\r\n]*\|\s*$',safe(root,MASTER).read_text(encoding='utf-8-sig'),re.M)
    need(len(masters)==1,'Task must already be registered once in Master')
    phase=rows.get(TASK)
    if not post:
        need(phase=='LOCKED' and current=='NONE','Preflight requires LOCKED / NONE')
        need(counts=={'COMPLETE':244,'LOCKED':42},'Preflight status counts differ')
        need(sha(data)==lock['status_before_sha256'],'Preflight Status bytes changed')
    else:
        need(phase in ('CURRENT','COMPLETE'),'Read-only task verification requires CURRENT or COMPLETE')
        need(current==(f'TASKS/{TASK}.md' if phase=='CURRENT' else 'NONE'),'Current task mismatch')
        expected={'COMPLETE':244,'CURRENT':1,'LOCKED':41} if phase=='CURRENT' else {'COMPLETE':245,'LOCKED':41}
        need(counts==expected,'Post-apply counts differ')
        # Exact inverse projection validates that native Apply/Finalize touched only its two fields.
        original,n=re.subn(rb'(?m)^(\|\s*'+TASK.encode()+rb'\s*\|\s*)(CURRENT|COMPLETE)(\s*\|)',rb'\g<1>LOCKED\3',data)
        need(n==1,'Could not project Status task row')
        if phase=='CURRENT':
            original,n=re.subn(rb'(## Current Task\s+```text\s+)TASKS/'+TASK.encode()+rb'\.md',rb'\g<1>NONE',original)
            need(n==1,'Could not project Current block')
        need(sha(original)==lock['status_before_sha256'],'Status contains changes beyond the authorized two fields')
    return phase,counts

def inbox_candidates(root):
    directory=safe(root,INBOX)
    if not directory.exists(): return []
    need(directory.is_dir(),'INBOX is not a directory')
    output=[]
    for p in directory.iterdir():
        if p.is_symlink(): raise Blocked('INBOX contains a symlink: '+p.name)
        if (p.is_file() and p.suffix.lower()=='.md') or (p.is_dir() and not (p/'.APPLIED').exists()):output.append(p)
    return sorted(output)

def check_sources(root,lock,post):
    checked=[]
    for item in lock['files']:
        if post and item['phase']=='BEFORE_ONLY':continue
        p=safe(root,item['path'])
        need(p.is_file(),'Missing local input: '+item['path'])
        need(sha(p.read_bytes())==item['sha256'],'Local bytes mismatch: '+item['path'])
        checked.append(item['path'])
    report=safe(root,f'MapDesign/MCP/REPORTS/{PREV}_RESULT.md').read_text(encoding='utf-8-sig')
    need(re.search(r'^TASK: '+PREV+'$',report,re.M) and re.search(r'^STATUS: PASS$',report,re.M),'Predecessor Result is not matching PASS')
    return checked

def git(root,*args):
    r=subprocess.run(['git','-C',str(root),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    need(r.returncode==0,'Git verification failed: '+' '.join(args[:3]))
    return r.stdout

def check_git(root,lock,post):
    top=Path(os.fsdecode(git(root,'rev-parse','--show-toplevel')).strip()).resolve()
    need(root.is_relative_to(top),'Project root outside repository')
    prefix=root.relative_to(top).as_posix(); prefix='' if prefix=='.' else prefix+'/'
    commit=lock['predecessor']['commit']; parent=lock['predecessor']['parent']
    git(root,'merge-base','--is-ancestor',commit,'HEAD')
    parents=git(root,'rev-list','--parents','-n','1',commit).decode().strip().split()
    need(parents==[commit,parent],'Predecessor commit parent mismatch')
    different=[]
    checked=0
    for item in lock['files']:
        if item.get('commit_bound'):
            blob=git(root,'show',commit+':'+prefix+item['path'])
            # Worktree and Git-object bytes are independently pinned. Never normalize
            # bytes at verification time or learn a replacement hash from local files.
            need(sha(blob)==item['commit_blob_sha256'] and len(blob)==item['commit_blob_bytes'],
                 'Predecessor commit blob mismatch: '+item['path'])
            checked+=1
            if item['commit_blob_sha256']!=item['sha256']:
                different.append({'path':item['path'],'worktree_sha256':item['sha256'],
                                  'commit_blob_sha256':item['commit_blob_sha256'],
                                  'commit_blob_bytes':len(blob)})
    for item in lock['committed_state']:
        blob=git(root,'show',commit+':'+prefix+item['path'])
        need(sha(blob)==item['commit_blob_sha256'] and len(blob)==item['commit_blob_bytes'],
             'Predecessor committed state mismatch: '+item['path'])
        checked+=1
    if not post:
        # Exact owned implementation paths only; unrelated dirty content is not read or changed.
        paths=lock['owned_existing']+lock['owned_new']
        dirty=git(root,'status','--porcelain','--untracked-files=all','--',*paths)
        need(not dirty,'Existing dirty changes overlap Task implementation/evidence paths')
    return {'commit':commit,'parent':parent,'head':git(root,'rev-parse','HEAD').decode().strip(),
            'verified_in_live_git':True,'commit_blob_checks':checked,
            'different_worktree_and_blob_bytes':different}

def local(root,lock,manifest,post=False):
    need(all((root/p).is_dir() for p in ['Assets','Packages','ProjectSettings','MapDesign/MCP']), 'Use the actual Unity project root')
    need(HERE==safe(root,PREFIX).resolve(),'Run the helper installed in this project INPUTS/SV5_06')
    phase,counts=check_state(root,lock,post)
    checked=check_sources(root,lock,post)
    gitproof=check_git(root,lock,post)
    if post:
        proto='MapDesign/MCP/SV5/02_PROTOCOL_V5.md'
        raw=safe(root,proto).read_bytes()
        before=next(x['sha256'] for x in lock['files'] if x['path']==proto)
        suffix=(HERE/'PROTOCOL_APPEND.md').read_bytes()
        appended=raw.endswith(suffix) and sha(raw[:-len(suffix)])==before
        need(appended or (phase=='CURRENT' and sha(raw)==before),'Protocol must preserve original bytes and append the exact suffix once')
    bound=(HERE/(TASK+'.md')).read_bytes()
    candidates=inbox_candidates(root)
    if post:
        need(not candidates,'INBOX must be empty after native Apply')
        for rel in [f'MapDesign/MCP/TASKS/{TASK}.md',f'MapDesign/MCP_ARCHIVE/{TASK}.md']:
            p=safe(root,rel); need(p.is_file() and p.read_bytes()==bound,'Installed/Archive Task bytes differ: '+rel)
    else:
        target=safe(root,INBOX+'/'+TASK+'.md')
        need(not candidates or (candidates==[target] and target.read_bytes()==bound),'INBOX is ambiguous or contains another candidate; do not delete or relocate it')
        for rel in [f'MapDesign/MCP/TASKS/{TASK}.md',f'MapDesign/MCP_ARCHIVE/{TASK}.md']:
            p=safe(root,rel); need(not p.exists() or (p.is_file() and p.read_bytes()==bound),'Task/Archive destination collision: '+rel)
        need(not safe(root,f'MapDesign/MCP/REPORTS/{TASK}_RESULT.md').exists(),'Unexpected existing new Task Result')
    return {'task':TASK,'phase':phase,'counts':counts,'checked_local_files':len(checked),'git':gitproof,'bound_task_sha256':manifest['bound_task_sha256']}

def stage(root,lock,m):
    result=local(root,lock,m)
    p=safe(root,INBOX+'/'+TASK+'.md'); data=(HERE/(TASK+'.md')).read_bytes()
    if p.exists(): need(p.read_bytes()==data,'INBOX collision')
    else:
        p.parent.mkdir(parents=True,exist_ok=True)
        # Exclusive create: never overwrite a file that appears after preflight.
        with p.open('xb') as f: f.write(data); f.flush(); os.fsync(f.fileno())
    need(p.read_bytes()==data and inbox_candidates(root)==[p],'Staged bytes/candidate count mismatch')
    result['status']='PASS_STAGED_ONLY'; result['changed']=[INBOX+'/'+TASK+'.md']
    return result

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode',choices=['package','preflight','stage','post-readonly'],required=True)
    parser.add_argument('--expected-manifest-sha',required=True)
    parser.add_argument('--project-root',type=Path)
    args=parser.parse_args()
    lock,m=check_package(args.expected_manifest_sha)
    if args.mode=='package':result={'status':'PASS_PACKAGE_ONLY','files':len(m['files']),'native_apply':False}
    else:
        need(args.project_root is not None,'--project-root is required')
        root=args.project_root.resolve()
        if args.mode=='stage': result=stage(root,lock,m)
        else:
            result=local(root,lock,m,args.mode=='post-readonly')
            result['status']='PASS_READONLY_LOCAL_AND_GIT' if args.mode=='post-readonly' else 'PASS_PREFLIGHT_LOCAL_AND_GIT'
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':
    try:main()
    except (Blocked,OSError,ValueError,KeyError) as e:
        print(json.dumps({'status':'BLOCKED','reason':str(e),'native_apply':False},ensure_ascii=False));sys.exit(2)
