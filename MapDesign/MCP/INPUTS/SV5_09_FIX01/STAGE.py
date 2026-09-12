#!/usr/bin/env python3
"""Register/verify/stage SV5_09_FIX01. Never runs native Apply, Unity, Finalize, commit, or push."""
import argparse, collections, hashlib, json, os, re, subprocess, sys
from pathlib import Path, PurePosixPath

TASK="SV5_09_FIX01"; PREV="SV5_09_LOOPS"; NEXT="SV5_10_SIDEPATH"
HERE=Path(__file__).resolve().parent; PREFIX="MapDesign/MCP/INPUTS/SV5_09_FIX01"
STATUS="MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"; MASTER="MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
PROTOCOL="MapDesign/MCP/SV5/02_PROTOCOL_V5.md"; INBOX="MapDesign/MCP_INBOX"

class Blocked(Exception):pass
def need(ok,msg):
    if not ok:raise Blocked(msg)
def sha(b):return hashlib.sha256(b).hexdigest()
def load(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def safe(root,relative):
    q=PurePosixPath(relative);need(relative and not q.is_absolute() and "\\" not in relative and ":" not in relative and all(x not in("",".","..") for x in relative.split("/")),"unsafe path "+relative)
    p=root.joinpath(*q.parts);need(p.resolve().is_relative_to(root.resolve()),"path escapes root "+relative);return p
def git(root,*args,check=True):
    p=subprocess.run(["git","-C",str(root),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if check:need(p.returncode==0,"git failed: "+" ".join(args[:4]))
    return p.stdout

def package(expected):
    need(re.fullmatch(r"[0-9a-f]{64}",expected) is not None,"manifest SHA required")
    need(sha((HERE/"FILES.json").read_bytes())==expected,"package manifest SHA mismatch")
    m=load(HERE/"FILES.json");need(m["format"]=="sv5_09_fix01_package_v1" and m["task"]==TASK,"wrong package")
    names=[x["path"] for x in m["files"]];need(len(names)==len(set(names)),"duplicate package path")
    for x in m["files"]:
        b=safe(HERE,x["path"]).read_bytes();need(len(b)==x["bytes"] and sha(b)==x["sha256"],"package bytes mismatch "+x["path"])
    lock=load(HERE/"SOURCE_LOCK.json");need(lock["format"]=="sv5_09_fix01_source_lock_v1","wrong source lock")
    body=(HERE/(TASK+".md")).read_bytes();need(sha(body)==m["bound_task_sha256"] and len(body.decode("utf-8").splitlines())<=300,"bound Task mismatch")
    header=("---\nmcp_patch:\n  format: single_task_v1\n  task_id: "+TASK+"\n  task_file: TASKS/"+TASK+".md\n  requires_current_task: NONE\n  requires_completed_task: "+PREV+"\n  requires_result:\n    path: REPORTS/"+PREV+"_RESULT.md\n    status: PASS\n    sha256: "+lock["predecessor"]["result_sha256"]+"\n  requires_installed_task:\n    path: TASKS/"+PREV+".md\n    sha256: "+lock["predecessor"]["task_sha256"]+"\n  sets_current_task: "+TASK+"\n---\n")
    need(body.decode("utf-8").startswith(header),"native Task metadata mismatch")
    return lock,m

def repo(root):
    top=Path(os.fsdecode(git(root,"rev-parse","--show-toplevel")).strip()).resolve();need(root.is_relative_to(top),"Unity root outside repository")
    rel=root.relative_to(top).as_posix();return top,"" if rel=="." else rel+"/"

def check_commit(root,lock):
    _,prefix=repo(root);commit=lock["predecessor"]["commit"];parent=lock["predecessor"]["parent"]
    git(root,"merge-base","--is-ancestor",commit,"HEAD")
    need(git(root,"rev-list","--parents","-n","1",commit).decode().strip().split()==[commit,parent],"predecessor parent mismatch")
    for x in lock["commit_blobs"]:
        b=git(root,"show",commit+":"+prefix+x["path"]);oid=hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
        need(sha(b)==x["blob_sha256"] and len(b)==x["blob_bytes"] and oid==x["git_blob_oid"],"predecessor blob mismatch "+x["path"])
    return {"commit":commit,"parent":parent,"blobs":len(lock["commit_blobs"])}

def parse_status(data,rows_expected):
    text=data.decode("utf-8-sig");rows=re.findall(r"^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$",text,re.M)
    need(len(rows)==rows_expected and len(dict(rows))==rows_expected,"status row count/duplicate mismatch")
    cur=re.findall(r"## Current Task\s+```text\s+([^\r\n]+)\s+```",text);need(len(cur)==1,"Current Task block mismatch")
    return dict(rows),cur[0].strip(),dict(collections.Counter(v for _,v in rows))

def check_predecessor_files(root,lock):
    for p,h in [("MapDesign/MCP/REPORTS/SV5_09_LOOPS_RESULT.md",lock["predecessor"]["result_sha256"]),("MapDesign/MCP/TASKS/SV5_09_LOOPS.md",lock["predecessor"]["task_sha256"]),("MapDesign/MCP_ARCHIVE/SV5_09_LOOPS.md",lock["predecessor"]["task_sha256"])]:
        need(sha(safe(root,p).read_bytes())==h,"predecessor bytes mismatch "+p)
    text=safe(root,"MapDesign/MCP/REPORTS/SV5_09_LOOPS_RESULT.md").read_text(encoding="utf-8-sig")
    need(re.search(r"^TASK: SV5_09_LOOPS\r?$",text,re.M) and re.search(r"^STATUS: PASS\r?$",text,re.M),"predecessor Result marker mismatch")

def candidates(root):
    d=safe(root,INBOX)
    if not d.exists():return []
    out=[]
    for p in d.iterdir():
        need(not p.is_symlink(),"INBOX symlink "+p.name)
        if (p.is_file() and p.suffix.lower()==".md") or (p.is_dir() and not (p/".APPLIED").exists()):out.append(p)
    return sorted(out)

def pre_registration(root,lock):
    s=safe(root,STATUS).read_bytes();m=safe(root,MASTER).read_bytes();r=load(HERE/"REGISTRATION.json")
    need(sha(s)==r["status"]["before_sha256"] and sha(m)==r["master"]["before_sha256"],"registration before bytes mismatch")
    rows,cur,count=parse_status(s,292);need(rows.get(PREV)=="COMPLETE" and rows.get(NEXT)=="LOCKED" and TASK not in rows and cur=="NONE" and count=={"COMPLETE":254,"LOCKED":38},"registration before state mismatch")
    need(TASK not in m.decode("utf-8-sig"),"Task already occurs in Master")
    check_predecessor_files(root,lock);proof=check_commit(root,lock);need(not candidates(root),"INBOX not empty")
    need(not git(root,"status","--porcelain","--",STATUS,MASTER),"registration files already dirty")
    return proof

def register(root,lock):
    proof=pre_registration(root,lock);r=load(HERE/"REGISTRATION.json");outputs=[]
    for key in ("status","master"):
        spec=r[key];p=safe(root,spec["path"]);b=p.read_bytes();needle=spec["needle"].encode();insert=(spec["needle"]+"\n"+spec["insert"]).encode()
        need(b.count(needle)==1,"registration needle count "+key);new=b.replace(needle,insert)
        need(sha(new)==spec["after_sha256"] and len(new)==spec["after_bytes"],"registration projection mismatch "+key)
        tmp=p.with_name(p.name+".SV5_09_FIX01.tmp");tmp.write_bytes(new);os.replace(tmp,p);outputs.append(spec["path"])
    return {"status":"PASS_REGISTERED_ONLY","changed":outputs,"git":proof,"native_apply":False}

def state_after_registration(root,lock,post):
    data=safe(root,STATUS).read_bytes();rows,cur,count=parse_status(data,293)
    need(sha(safe(root,MASTER).read_bytes())==lock["master_after_registration_sha256"],"Master after registration mismatch")
    need(rows.get(PREV)=="COMPLETE" and rows.get(NEXT)=="LOCKED","predecessor/successor state mismatch")
    phase=rows.get(TASK)
    if not post:
        need(phase=="LOCKED" and cur=="NONE" and count=={"COMPLETE":254,"LOCKED":39},"preflight state mismatch")
        need(sha(data)==lock["status_after_registration_sha256"],"Status after registration mismatch")
    else:
        need(phase in("CURRENT","COMPLETE"),"post phase mismatch")
        need(cur==("TASKS/"+TASK+".md" if phase=="CURRENT" else "NONE"),"Current block mismatch")
        expected={"COMPLETE":254,"CURRENT":1,"LOCKED":38} if phase=="CURRENT" else {"COMPLETE":255,"LOCKED":38};need(count==expected,"post counts mismatch")
        base,n=re.subn(rb"(?m)^(\|\s*"+TASK.encode()+rb"\s*\|\s*)(CURRENT|COMPLETE)(\s*\|)",rb"\g<1>LOCKED\3",data);need(n==1,"cannot project task row")
        if phase=="CURRENT":base,n=re.subn(rb"(## Current Task\s+```text\s+)TASKS/"+TASK.encode()+rb"\.md",rb"\g<1>NONE",base);need(n==1,"cannot project Current")
        need(sha(base)==lock["status_after_registration_sha256"],"Status changed outside lifecycle")
    return phase,count

def local(root,lock,manifest,post=False):
    need(all((root/p).is_dir() for p in("Assets","Packages","ProjectSettings","MapDesign/MCP")),"use Unity project root");need(HERE==safe(root,PREFIX).resolve(),"run installed INPUTS helper")
    phase,count=state_after_registration(root,lock,post);check_predecessor_files(root,lock);proof=check_commit(root,lock)
    bound=(HERE/(TASK+".md")).read_bytes();target=safe(root,INBOX+"/"+TASK+".md")
    if post:
        need(not candidates(root),"INBOX not empty after Apply")
        for p in("MapDesign/MCP/TASKS/"+TASK+".md","MapDesign/MCP_ARCHIVE/"+TASK+".md"):need(safe(root,p).read_bytes()==bound,"installed/archive mismatch "+p)
        b=safe(root,PROTOCOL).read_bytes();suffix=(HERE/"PROTOCOL_APPEND.md").read_bytes();need(b.endswith(suffix) and sha(b[:-len(suffix)])==lock["protocol_before_sha256"],"protocol append mismatch")
        if phase=="COMPLETE":
            t=safe(root,lock["result_path"]).read_text(encoding="utf-8-sig");need(re.search(r"^TASK: "+TASK+r"\r?$",t,re.M) and re.search(r"^STATUS: PASS\r?$",t,re.M),"COMPLETE Result marker mismatch")
    else:
        inbox=candidates(root);need(not inbox or (inbox==[target] and target.read_bytes()==bound),"INBOX has another candidate")
        scope=lock["owned_existing"]+lock["owned_new"]+lock["owned_new_roots"]+[lock["result_path"]]
        need(not git(root,"status","--porcelain","--untracked-files=all","--",*scope),"dirty changes overlap FIX01 ownership")
        for p in lock["owned_new"]+lock["owned_new_roots"]+[lock["result_path"]]:need(not safe(root,p).exists(),"unexpected new output "+p)
    return {"task":TASK,"phase":phase,"counts":count,"git":proof,"bound_task_sha256":manifest["bound_task_sha256"]}

def stage(root,lock,manifest):
    result=local(root,lock,manifest);target=safe(root,INBOX+"/"+TASK+".md");data=(HERE/(TASK+".md")).read_bytes();changed=[]
    if not target.exists():target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);changed.append(INBOX+"/"+TASK+".md")
    need(target.read_bytes()==data and candidates(root)==[target],"staged candidate mismatch");result.update(status="PASS_STAGED_ONLY",changed=changed,native_apply=False);return result

def main():
    ap=argparse.ArgumentParser();ap.add_argument("--mode",choices=("package","pre-registration","register","preflight","stage","post-readonly"),required=True);ap.add_argument("--expected-manifest-sha",required=True);ap.add_argument("--project-root",type=Path);a=ap.parse_args();lock,m=package(a.expected_manifest_sha)
    if a.mode=="package":result={"status":"PASS_PACKAGE_ONLY","task":TASK,"files":len(m["files"]),"native_apply":False}
    else:
        need(a.project_root is not None,"--project-root required");root=a.project_root.resolve()
        if a.mode=="pre-registration":result={"status":"PASS_PRE_REGISTRATION_READONLY","git":pre_registration(root,lock),"native_apply":False}
        elif a.mode=="register":result=register(root,lock)
        elif a.mode=="stage":result=stage(root,lock,m)
        else:result=local(root,lock,m,a.mode=="post-readonly");result["status"]="PASS_READONLY_LOCAL_AND_GIT" if a.mode=="post-readonly" else "PASS_PREFLIGHT_LOCAL_AND_GIT"
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=="__main__":
    try:main()
    except (Blocked,OSError,ValueError,KeyError,TypeError) as e:print(json.dumps({"status":"BLOCKED","reason":str(e),"native_apply":False},ensure_ascii=False));sys.exit(2)
