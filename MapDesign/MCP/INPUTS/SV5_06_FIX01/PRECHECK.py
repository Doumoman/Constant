#!/usr/bin/env python3
"""Verify the separate registration patch, then stage one normal Task. No registry edits here."""
import argparse,collections,hashlib,json,os,re,subprocess,sys
from pathlib import Path,PurePosixPath
HERE=Path(__file__).resolve().parent
TASK='SV5_06_FIX01'; PREV='SV5_06_SPACE_GRAPH'
PREFIX='MapDesign/MCP/INPUTS/SV5_06_FIX01'
STATUS='MapDesign/MCP/06_IMPLEMENTATION_STATUS.md';MASTER='MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md'
class Blocked(Exception):pass
def need(ok,msg):
 if not ok:raise Blocked(msg)
def digest(b):return hashlib.sha256(b).hexdigest()
def readj(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def safe(root,rel):
 p=PurePosixPath(rel);need(bool(rel) and not p.is_absolute() and '..' not in p.parts and '\\' not in rel and ':' not in rel,'Unsafe path')
 out=root.joinpath(*p.parts)
 for v in (out,*out.parents):
  if v==root:break
  need(not v.is_symlink(),'Symlink task path: '+rel)
 need(out.resolve().is_relative_to(root),'Path outside project')
 return out
def git(root,*args):
 p=subprocess.run(['git','-C',str(root),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 need(p.returncode==0,'Git check failed: '+' '.join(args[:3]));return p.stdout

def package(expected):
 need(re.fullmatch('[0-9a-f]{64}',expected) is not None,'Expected manifest SHA required')
 need(digest((HERE/'FILES.json').read_bytes())==expected,'Package manifest mismatch')
 m=readj(HERE/'FILES.json');need(m['task']==TASK,'Wrong package task')
 for r in m['files']:
  p=safe(HERE,r['path']);need(p.is_file() and digest(p.read_bytes())==r['sha256'] and p.stat().st_size==r['bytes'],'Package bytes mismatch: '+r['path'])
 lock=readj(HERE/'SOURCE_LOCK.json');reg=readj(HERE/'REGISTRATION.json')
 text=(HERE/(TASK+'.md')).read_text(encoding='utf-8')
 need(len(text.splitlines())<=300,'Task exceeds 300 lines')
 header=text.split('---',2)[1]
 expected_lines=['  format: single_task_v1','  task_id: '+TASK,'  task_file: TASKS/'+TASK+'.md','  requires_current_task: NONE','  requires_completed_task: '+PREV,'    path: REPORTS/'+PREV+'_RESULT.md','    status: PASS','    sha256: '+lock['predecessor']['result_sha256'],'    path: TASKS/'+PREV+'.md','    sha256: '+lock['predecessor']['task_sha256'],'  sets_current_task: '+TASK]
 need(all(line in header.splitlines() for line in expected_lines),'Native metadata mismatch')
 need(digest((HERE/(TASK+'.md')).read_bytes())==m['bound_task_sha256'],'Bound task mismatch')
 return m,lock,reg

def candidates(root):
 p=safe(root,'MapDesign/MCP_INBOX')
 if not p.exists():return []
 need(p.is_dir(),'INBOX not a directory');found=[]
 for v in p.iterdir():
  need(not v.is_symlink(),'Symlink in INBOX')
  if (v.is_file() and v.suffix.lower()=='.md') or (v.is_dir() and not (v/'.APPLIED').exists()):found.append(v)
 return sorted(found)

def state(root,reg,mode):
 b=safe(root,STATUS).read_bytes();t=b.decode('utf-8-sig')
 rows=re.findall(r'^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$',t,re.M)
 need(len(rows)==len(dict(rows)),'Duplicate Status task rows')
 d=dict(rows);cur=re.findall(r'## Current Task\s+```text\s+([^\r\n]+)\s+```',t)
 need(len(cur)==1 and d.get(PREV)=='COMPLETE' and d.get('SV5_07_DIVERSITY')=='LOCKED','Current or dependency state mismatch')
 current=cur[0].strip(); phase=d.get(TASK,'UNREGISTERED')
 master=safe(root,MASTER).read_bytes();counts=dict(collections.Counter(v for k,v in rows))
 if mode=='pre-registration':
  need(phase=='UNREGISTERED' and current=='NONE' and len(rows)==286 and counts=={'COMPLETE':245,'LOCKED':41},'Expected unregistered / Current NONE')
  need(digest(b)==reg['status']['before_sha256'] and digest(master)==reg['master']['before_sha256'],'Registration before bytes mismatch')
 else:
  need(len(rows)==287 and digest(master)==reg['master']['after_sha256'],'Exact registered Master/row count required')
  n=len(re.findall(r'^\|[^\r\n]*\b'+TASK+r'\b[^\r\n]*\|\s*$',master.decode('utf-8-sig'),re.M))
  need(n==1,'Task must be registered once in Master')
  if mode in ['pre-stage','stage']:
   need(phase=='LOCKED' and current=='NONE' and counts=={'COMPLETE':245,'LOCKED':42},'Expected registered LOCKED / NONE')
   need(digest(b)==reg['status']['after_sha256'],'Registered Status bytes mismatch')
  else:
   need(phase in ['CURRENT','COMPLETE'],'Expected current or complete Task')
   need(current==('TASKS/'+TASK+'.md' if phase=='CURRENT' else 'NONE'),'Current block mismatch')
   expected={'COMPLETE':245,'CURRENT':1,'LOCKED':41} if phase=='CURRENT' else {'COMPLETE':246,'LOCKED':41}
   need(counts==expected,'Task phase counts mismatch')
   original,n=re.subn(rb'(?m)^(\|\s*'+TASK.encode()+rb'\s*\|\s*)(CURRENT|COMPLETE)(\s*\|)',rb'\g<1>LOCKED\3',b)
   need(n==1,'Cannot project own task row')
   if phase=='CURRENT':
    original,n=re.subn(rb'(## Current Task\s+```text\s+)TASKS/'+TASK.encode()+rb'\.md',rb'\g<1>NONE',original);need(n==1,'Cannot project Current')
   need(digest(original)==reg['status']['after_sha256'],'Status changed beyond native own-task fields')
 return phase,counts

def local(root,lock,reg,mode):
 need(all((root/p).is_dir() for p in ['Assets','Packages','ProjectSettings','MapDesign/MCP']),'Use actual Unity project root')
 need(HERE==safe(root,PREFIX).resolve(),'Use this project INPUTS helper')
 post=mode=='post-readonly';phase,counts=state(root,reg,mode)
 if post:
  proto=safe(root,'MapDesign/MCP/SV5/02_PROTOCOL_V5.md').read_bytes()
  suffix=(HERE/'PROTOCOL_APPEND.md').read_bytes()
  appended=proto.endswith(suffix) and digest(proto[:-len(suffix)])==lock['protocol_before_sha256']
  need(appended or (phase=='CURRENT' and digest(proto)==lock['protocol_before_sha256']),
       'Protocol must retain original bytes and append exact suffix once')
 checked=0
 for r in lock['files']:
  if post and r['phase']=='BEFORE_ONLY':continue
  p=safe(root,r['path']);need(p.is_file() and digest(p.read_bytes())==r['worktree_sha256'],'Worktree bytes mismatch: '+r['path']);checked+=1
 result=safe(root,'MapDesign/MCP/REPORTS/'+PREV+'_RESULT.md').read_text(encoding='utf-8-sig')
 need(re.search(r'^TASK: '+PREV+r'$',result,re.M) and re.search(r'^STATUS: PASS$',result,re.M),'Predecessor Result not matching PASS')
 top=Path(os.fsdecode(git(root,'rev-parse','--show-toplevel')).strip()).resolve();need(root.is_relative_to(top),'Project outside repo')
 prefix=root.relative_to(top).as_posix();prefix='' if prefix=='.' else prefix+'/'
 commit=lock['predecessor']['commit'];parent=lock['predecessor']['parent'];git(root,'merge-base','--is-ancestor',commit,'HEAD')
 need(git(root,'rev-list','--parents','-n','1',commit).decode().strip().split()==[commit,parent],'Predecessor commit parent mismatch')
 blobs=0
 for r in lock['commit_blobs']:
  b=git(root,'show',commit+':'+prefix+r['path']);need(digest(b)==r['sha256'] and len(b)==r['bytes'],'Commit blob mismatch: '+r['path']);blobs+=1
 bound=(HERE/(TASK+'.md')).read_bytes();cand=candidates(root);inbox=safe(root,'MapDesign/MCP_INBOX/'+TASK+'.md')
 if mode=='pre-registration':
  need(not cand,'INBOX must be empty before registration; preserve other candidates')
 elif not post:
  need(not cand or (cand==[inbox] and inbox.read_bytes()==bound),'Ambiguous INBOX; preserve other candidates')
 else:
  need(not cand,'INBOX must be empty after Apply')
 for rel in ['MapDesign/MCP/TASKS/'+TASK+'.md','MapDesign/MCP_ARCHIVE/'+TASK+'.md']:
  p=safe(root,rel)
  if post:need(p.is_file() and p.read_bytes()==bound,'Installed/Archive bytes mismatch')
  else:need(not p.exists() or (mode!='pre-registration' and p.is_file() and p.read_bytes()==bound),'Task destination already exists or collides')
 if not post:
  need(not git(root,'status','--porcelain','--untracked-files=all','--',*lock['owned_implementation_paths']),'Existing dirty changes overlap implementation paths')
  need(not safe(root,'MapDesign/MCP/REPORTS/'+TASK+'_RESULT.md').exists(),'Unexpected existing new Result')
 return {'task':TASK,'phase':phase,'counts':counts,'checked_worktree_files':checked,'checked_commit_blobs':blobs,'predecessor_commit':commit,'predecessor_parent':parent,'live_git_verified':True}

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--mode',choices=['package','pre-registration','pre-stage','stage','post-readonly'],required=True);p.add_argument('--expected-manifest-sha',required=True);p.add_argument('--project-root',type=Path)
 a=p.parse_args();m,l,r=package(a.expected_manifest_sha)
 if a.mode=='package':out={'status':'PASS_PACKAGE_ONLY','files':len(m['files'])}
 else:
  need(a.project_root is not None,'Project root required');root=a.project_root.resolve();out=local(root,l,r,a.mode)
  if a.mode=='stage':
   target=safe(root,'MapDesign/MCP_INBOX/'+TASK+'.md');b=(HERE/(TASK+'.md')).read_bytes()
   if not target.exists():
    target.parent.mkdir(parents=True,exist_ok=True)
    with target.open('xb') as f:f.write(b);f.flush();os.fsync(f.fileno())
   need(target.read_bytes()==b and candidates(root)==[target],'Stage bytes/candidates changed')
   out['status']='PASS_STAGED_ONLY'
  else:out['status']='PASS_'+a.mode.upper().replace('-','_')+'_READONLY'
 out['registry_written_by_helper']=False;out['native_apply']=False;print(json.dumps(out,ensure_ascii=False,indent=2))
if __name__=='__main__':
 try:main()
 except (Blocked,OSError,ValueError,KeyError,IndexError) as e:print(json.dumps({'status':'BLOCKED','reason':str(e),'native_apply':False},ensure_ascii=False));sys.exit(2)
