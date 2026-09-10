"""Exact, user-approved FIX01 registration and input relocation; never executes a Task."""
from pathlib import Path
import argparse, collections, hashlib, json, os, re, sys, tempfile

TASK='SV5_05_FIX01'
PATCH='SV5_05_FIX01_REG'
STATUS='MCP/06_IMPLEMENTATION_STATUS.md'
MASTER='MCP/MASTER_IMPLEMENTATION_TASK_LIST.md'
GEN='MCP/GENERATED/'+PATCH
RECEIPT=GEN+'/REGISTRATION_RECEIPT.json'
CANDIDATE='MCP_INBOX/'+TASK+'.md'

def h(data):return hashlib.sha256(data).hexdigest()
def require(ok,message):
    if not ok:raise ValueError(message)
def read_json(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def json_bytes(value):return (json.dumps(value,ensure_ascii=False,indent=2)+'\n').encode('utf-8')

def path_at(root,relative):
    rel=Path(relative)
    require(not rel.is_absolute() and '..' not in rel.parts,'unsafe relative path: '+relative)
    p=root/rel
    require(p.resolve().is_relative_to(root),'path escapes root: '+relative)
    for q in [p,*p.parents]:
        if q==root:break
        require(not q.is_symlink(),'symlink not allowed: '+str(q))
        require(not (hasattr(q,'is_junction') and q.is_junction()),'junction not allowed: '+str(q))
    return p

def exact(p,sha,size=None):
    require(p.is_file(),'missing file: '+str(p))
    data=p.read_bytes()
    require(h(data)==sha,'SHA mismatch: '+str(p))
    if size is not None:require(len(data)==size,'size mismatch: '+str(p))
    return data

def status_rows(data):
    text=data.decode('utf-8-sig')
    rows=re.findall(r'^\| ([A-Z0-9_]+) \| (COMPLETE|CURRENT|LOCKED) \|$',text,re.M)
    require(len(rows)==len(dict(rows)),'duplicate Status task rows')
    m=re.search(r'(?m)^## Current Task\n\n```text\n([^\n]+)\n```',text)
    require(m is not None,'unrecognized Current Task field')
    return dict(rows),m.group(1)

def scan(mr):
    inbox=path_at(mr,'MCP_INBOX');require(inbox.is_dir(),'INBOX missing')
    found=[]
    for p in inbox.iterdir():
        path_at(mr,p.relative_to(mr).as_posix())
        if p.is_file() and p.suffix.lower()=='.md':found.append(p.name)
        elif p.is_dir() and not (p/'.APPLIED').exists():found.append(p.name+'/')
    return sorted(found)

def verify_package(inp,external_sha):
    require(bool(re.fullmatch('[0-9a-f]{64}',external_sha)),'invalid external manifest SHA')
    mf=inp/'FILES.json';exact(mf,external_sha)
    seen=set()
    for e in read_json(mf)['files']:
        require(e['path'] not in seen,'duplicate package path');seen.add(e['path'])
        p=path_at(inp,e['path']);exact(p,e['sha256'],e['bytes'])
        if p.suffix=='.md':require(len(p.read_text(encoding='utf-8').splitlines())<=300,'package MD over 300 lines')
    d=read_json(inp/'REGISTRATION.json')
    require(d['patch_id']==PATCH and d['task_id']==TASK,'wrong registration identity')
    return d

def verify_old_inputs(mr,d,held):
    old=path_at(mr,'MCP/INPUTS/'+TASK)
    exact(old/'FILES.json',d['original_package_manifest_sha256'])
    moves={e['from']:e for e in d['moves']}
    for e in read_json(old/'FILES.json')['files']:
        rel=e['path'];p=path_at(mr,rel)
        if held and rel in moves:
            require(not p.exists(),'original INBOX path unexpectedly present: '+rel)
            move=moves[rel]
            require(e['sha256']==move['sha256'] and e['bytes']==move['bytes'],'relocation does not bind original manifest')
            p=path_at(mr,move['to'])
        exact(p,e['sha256'],e['bytes'])
    # Original verifier and expected hashes remain untouched. Only recorded paths relocate.
    lock=read_json(old/'SOURCE_LOCK.json')
    return lock

def verify_locks(pr,mr,d,lock,post):
    n=0
    for e in d['protocol_locks']:
        exact(path_at(mr,e['path']),e['sha256']);n+=1
    for e in lock['files']:
        if post and e['check'].startswith('PRE_APPLY_'):continue
        require(e['path_base'] in ('PROJECT','MAPDESIGN'),'unknown source path base')
        root=pr if e['path_base']=='PROJECT' else mr
        exact(path_at(root,e['path']),e['sha256']);n+=1
    rp=path_at(mr,lock['previous_result_path'])
    text=exact(rp,lock['previous_result_sha256']).decode('utf-8-sig')
    require(re.search(r'^TASK: SV5_05_ROUTE_STATE$',text,re.M) is not None,'wrong predecessor task')
    require(re.search(r'^STATUS: PASS$',text,re.M) is not None,'previous Result not exact PASS')
    return n

def edited(data,e):
    anchor=e['anchor'].encode('utf-8');insert=e['insert_before'].encode('utf-8')
    require(h(data)==e['before_sha256'],'registration before SHA mismatch: '+e['path'])
    require(data.count(anchor)==1,'registration anchor not unique')
    out=data.replace(anchor,insert+anchor,1)
    require(h(out)==e['after_sha256'],'registration after SHA mismatch: '+e['path'])
    return out

def before(pr,mr,inp,d):
    lock=verify_old_inputs(mr,d,False);n=verify_locks(pr,mr,d,lock,False)
    for e in d['edits']:edited(path_at(mr,e['path']).read_bytes(),e)
    rows,current=status_rows(path_at(mr,STATUS).read_bytes())
    require(current=='NONE' and len(rows)==285,'unexpected initial Current/row count')
    require(collections.Counter(rows.values())=={'COMPLETE':243,'LOCKED':42},'unexpected initial states')
    require(rows['SV5_05_ROUTE_STATE']=='COMPLETE' and rows['SV5_06_SPACE_GRAPH']=='LOCKED','wrong predecessor/next state')
    require(TASK not in rows and TASK.encode() not in path_at(mr,MASTER).read_bytes(),'repair already registered without receipt')
    require(scan(mr)==sorted(Path(e['from']).name for e in d['moves']),'INBOX inventory changed or legacy directory present')
    for e in d['moves']:
        exact(path_at(mr,e['from']),e['sha256'],e['bytes'])
        require(not path_at(mr,e['to']).exists(),'hold destination collision: '+e['to'])
    for rel in [CANDIDATE,RECEIPT,GEN+'/REGISTRATION_RECORD.md',GEN+'/TRANSACTION.json',
                'MCP/TASKS/'+TASK+'.md','MCP_ARCHIVE/'+TASK+'.md','MCP/REPORTS/'+TASK+'_RESULT.md']:
        require(not path_at(mr,rel).exists(),'registration destination collision: '+rel)
    return n

def after(pr,mr,inp,d,external_sha,require_phase=None,check_receipt=True,check_mutable=False):
    sr,cur=status_rows(path_at(mr,STATUS).read_bytes())
    phase=sr.get(TASK)
    require(phase in ('LOCKED','CURRENT','COMPLETE'),'registration state missing')
    if require_phase:require(phase in require_phase,'unexpected registration/Task phase: '+phase)
    require(len(sr)==286 and sr.get('SV5_05_ROUTE_STATE')=='COMPLETE' and sr.get('SV5_06_SPACE_GRAPH')=='LOCKED','wrong registered roster')
    expected_cur={'NONE'} if phase!='CURRENT' else {TASK,'TASKS/'+TASK+'.md'}
    require(cur in expected_cur,'wrong Current Task after registration')
    s=path_at(mr,STATUS).read_bytes()
    if phase!='LOCKED':
        s=s.replace(('| '+TASK+' | '+phase+' |\n').encode(),('| '+TASK+' | LOCKED |\n').encode(),1)
        if phase=='CURRENT':s=s.replace(('## Current Task\n\n```text\n'+cur+'\n```').encode(),b'## Current Task\n\n```text\nNONE\n```',1)
    se=next(e for e in d['edits'] if e['path']==STATUS)
    require(h(s)==se['after_sha256'],'unrelated Status fields changed')
    me=next(e for e in d['edits'] if e['path']==MASTER)
    exact(path_at(mr,MASTER),me['after_sha256'])
    for e in d['moves']:
        require(not path_at(mr,e['from']).exists(),'parked source unexpectedly exists: '+e['from'])
        exact(path_at(mr,e['to']),e['sha256'],e['bytes'])
    candidate=path_at(mr,CANDIDATE)
    if phase=='LOCKED':
        require(scan(mr)==[TASK+'.md'],'expected exactly one prepared native candidate')
        exact(candidate,d['bound_task_sha256'])
        for rel in ('MCP/TASKS/'+TASK+'.md','MCP_ARCHIVE/'+TASK+'.md','MCP/REPORTS/'+TASK+'_RESULT.md'):
            require(not path_at(mr,rel).exists(),'task unexpectedly installed during registration: '+rel)
    else:
        require(scan(mr)==[],'unexpected INBOX candidate during this Task')
        for rel in ('MCP/TASKS/'+TASK+'.md','MCP_ARCHIVE/'+TASK+'.md'):
            exact(path_at(mr,rel),d['bound_task_sha256'])
    exact(path_at(mr,GEN+'/REGISTRATION_RECORD.md'),h((inp/'REGISTRATION.md').read_bytes()))
    if check_receipt:
        receipt=read_json(path_at(mr,RECEIPT))
        require(receipt['patch_id']==PATCH and receipt['package_manifest_sha256']==external_sha,'receipt package identity mismatch')
        require(receipt['registration_sha256']==h((inp/'REGISTRATION.json').read_bytes()),'receipt operation identity mismatch')
        require(receipt['bound_task_sha256']==d['bound_task_sha256'],'receipt bound Task mismatch')
        require(receipt['moves']==d['moves'],'receipt move inventory mismatch')
    old=verify_old_inputs(mr,d,True)
    n=verify_locks(pr,mr,d,old,phase!='LOCKED' and not check_mutable)
    return phase,n

class Transaction:
    def __init__(self,work):self.undo=[];self.work=work
    def atomic_write(self,p,data):
        self.work.mkdir(parents=True,exist_ok=True)
        temp=None
        try:
            with tempfile.NamedTemporaryFile(dir=self.work,delete=False) as f:
                temp=Path(f.name);f.write(data);f.flush();os.fsync(f.fileno())
            os.replace(temp,p)
        finally:
            if temp is not None and temp.exists():temp.unlink()
            if self.work.exists() and not any(self.work.iterdir()):self.work.rmdir()
    def create(self,p,data):
        p.parent.mkdir(parents=True,exist_ok=True)
        with p.open('xb') as f:
            try:f.write(data)
            except BaseException:
                f.close();p.unlink();raise
        self.undo.append(('create',p,data))
    def replace(self,p,old,new):
        require(p.read_bytes()==old,'file changed during registration: '+str(p))
        self.undo.append(('replace',p,old,new))
        self.atomic_write(p,new)
    def remove(self,p,data):
        require(p.read_bytes()==data,'INBOX source changed during registration: '+str(p))
        p.unlink();self.undo.append(('remove',p,data))
    def rollback(self):
        errors=[]
        for op in reversed(self.undo):
            try:
                kind,p=op[:2]
                if kind=='create':
                    require(p.read_bytes()==op[2],'created file changed externally');p.unlink()
                elif kind=='replace':
                    # Never revert concurrent external data to force a clean state.
                    observed=p.read_bytes()
                    require(observed in (op[2],op[3]),'edited file changed externally');self.atomic_write(p,op[2])
                else:
                    require(not p.exists(),'removed source reappeared')
                    with p.open('xb') as f:f.write(op[2])
            except Exception as e:errors.append(str(e))
        return errors

def apply(pr,mr,inp,d,external_sha,approval):
    require(approval==PATCH,'explicit approval identifier required; the flag is not authorization by itself')
    if path_at(mr,RECEIPT).exists():
        phase,n=after(pr,mr,inp,d,external_sha)
        return {'status':'ALREADY_REGISTERED','phase':phase,'writes':0,'checked_local_files':n}
    n=before(pr,mr,inp,d)
    old_bytes={e['path']:path_at(mr,e['path']).read_bytes() for e in d['edits']}
    original={e['from']:path_at(mr,e['from']).read_bytes() for e in d['moves']}
    tx=Transaction(path_at(mr,GEN+'/_work'));journal=path_at(mr,GEN+'/TRANSACTION.json')
    try:
        tx.create(journal,json_bytes({'patch_id':PATCH,'state':'IN_PROGRESS','edits':d['edits'],'moves':d['moves']}))
        for e in d['moves']:
            tx.create(path_at(mr,e['to']),original[e['from']]);exact(path_at(mr,e['to']),e['sha256'],e['bytes'])
        for e in d['edits']:tx.replace(path_at(mr,e['path']),old_bytes[e['path']],edited(old_bytes[e['path']],e))
        tx.create(path_at(mr,GEN+'/REGISTRATION_RECORD.md'),(inp/'REGISTRATION.md').read_bytes())
        for e in d['moves']:tx.remove(path_at(mr,e['from']),original[e['from']])
        tx.create(path_at(mr,CANDIDATE),(inp/'SV5_05_FIX01.md').read_bytes())
        after(pr,mr,inp,d,external_sha,{'LOCKED'},False)
        receipt={'patch_id':PATCH,'status':'REGISTERED_LOCKED_TASK_NOT_APPLIED',
                 'package_manifest_sha256':external_sha,'registration_sha256':h((inp/'REGISTRATION.json').read_bytes()),
                 'bound_task_sha256':d['bound_task_sha256'],'moves':d['moves'],
                 'state_before':{'complete':243,'current':0,'locked':42},
                 'state_after':{'complete':243,'current':0,'locked':43},
                 'candidate_count':1,'approval_identifier':approval,
                 'approval_evidence':'The invoking user instruction must explicitly authorize this exact patch.',
                 'native_apply_execution_finalize_commit':'NOT_RUN_BY_REGISTRATION_HELPER'}
        tx.create(path_at(mr,RECEIPT),json_bytes(receipt))
        after(pr,mr,inp,d,external_sha,{'LOCKED'})
        journal.unlink()
        return {'status':'PASS_REGISTRATION_ONLY','checked_local_files':n,'task_state':'LOCKED',
                'current_task':'NONE','candidate_count':1,'complete':243,'current':0,'locked':43,
                'bound_task_sha256':d['bound_task_sha256'],'receipt':str(path_at(mr,RECEIPT))}
    except BaseException as err:
        failures=tx.rollback()
        if failures:raise RuntimeError('Registration failed; rollback needs review: '+str(err)+'; '+'; '.join(failures)) from err
        raise RuntimeError('Registration failed; owned changes restored: '+str(err)) from err

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--manifest-sha',required=True)
    ap.add_argument('--project-root',required=True);ap.add_argument('--map-root',required=True)
    mode=ap.add_mutually_exclusive_group()
    mode.add_argument('--check',action='store_true')
    mode.add_argument('--apply-registration',action='store_true')
    mode.add_argument('--verify-staged',action='store_true')
    mode.add_argument('--verify-task-inputs',action='store_true')
    mode.add_argument('--post-readonly',action='store_true')
    ap.add_argument('--approval')
    a=ap.parse_args();inp=Path(__file__).resolve().parent
    d=verify_package(inp,a.manifest_sha);pr=Path(a.project_root).resolve();mr=Path(a.map_root).resolve()
    require(mr.is_relative_to(pr) and mr!=pr,'MapDesign must be within actual project root')
    if a.apply_registration:v=apply(pr,mr,inp,d,a.manifest_sha,a.approval)
    elif a.verify_staged or a.verify_task_inputs or a.post_readonly:
        phases={'LOCKED'} if a.verify_staged else {'CURRENT'} if a.verify_task_inputs else {'CURRENT','COMPLETE'}
        phase,n=after(pr,mr,inp,d,a.manifest_sha,phases,check_mutable=a.verify_task_inputs)
        v={'status':'PASS_STAGED_BYTES_ONLY' if a.verify_staged else 'PASS_READONLY_BYTES_ONLY',
           'task_phase':phase,'checked_local_files':n,'native_implementation_checked':False}
    elif path_at(mr,RECEIPT).exists():
        phase,n=after(pr,mr,inp,d,a.manifest_sha)
        v={'status':'ALREADY_REGISTERED','phase':phase,'writes':0,'checked_local_files':n}
    else:v={'status':'PASS_REGISTRATION_PREFLIGHT_ONLY','checked_local_files':before(pr,mr,inp,d),'writes':0}
    v['Unity_executed']=False;v['git_commit_performed']=False
    print(json.dumps(v,ensure_ascii=False,indent=2))

if __name__=='__main__':
    try:main()
    except Exception as e:
        print(json.dumps({'status':'BLOCKED','error':str(e),'Unity_executed':False},ensure_ascii=False,indent=2));sys.exit(1)
