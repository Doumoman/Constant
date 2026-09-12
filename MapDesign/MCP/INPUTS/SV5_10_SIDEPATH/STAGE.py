#!/usr/bin/env python3
"""Package/predecessor/state verifier and one-file INBOX stager. Never Apply/Finalize/commit/push."""
import argparse,collections,hashlib,json,os,re,subprocess,sys
from pathlib import Path,PurePosixPath
TASK="SV5_10_SIDEPATH";PREV="SV5_09_FIX01";NEXT="SV5_11_HUB_SHELL"
HERE=Path(__file__).resolve().parent;PREFIX="MapDesign/MCP/INPUTS/"+TASK
STATUS="MapDesign/MCP/06_IMPLEMENTATION_STATUS.md";MASTER="MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md";INBOX="MapDesign/MCP_INBOX"
class Blocked(Exception):pass
def need(ok,msg):
    if not ok:raise Blocked(msg)
def sha(b):return hashlib.sha256(b).hexdigest()
def load(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def safe(root,rel):
    q=PurePosixPath(rel);need(rel and not q.is_absolute() and "\\" not in rel and ":" not in rel and all(x not in ("",".","..") for x in rel.split("/")),"unsafe path "+rel)
    p=root.joinpath(*q.parts);need(p.resolve().is_relative_to(root.resolve()),"path escape "+rel);return p
def git(root,*args,check=True):
    p=subprocess.run(["git","-C",str(root),*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if check:need(p.returncode==0,"git failed: "+" ".join(args))
    return p
def package(expected):
    need(re.fullmatch(r"[0-9a-f]{64}",expected or "") is not None,"expected manifest SHA required")
    f=HERE/"FILES.json";need(sha(f.read_bytes())==expected,"package manifest SHA mismatch")
    m=load(f);need(m["schema"]=="SV5_10_SIDEPATH_PACKAGE_V1" and m["task"]==TASK,"wrong package")
    names=[]
    for e in m["files"]:
        names.append(e["path"]);b=safe(HERE,e["path"]).read_bytes();need(len(b)==e["bytes"] and sha(b)==e["sha256"],"package bytes mismatch: "+e["path"])
    need(len(names)==len(set(names)),"duplicate package path")
    task=(HERE/(TASK+".md")).read_bytes();need(sha(task)==m["bound_task_sha256"],"bound task mismatch")
    need(len(task.decode("utf-8").splitlines())<=300,"task over 300 lines")
    return m,load(HERE/"SOURCE_LOCK.json")
def status_state(data):
    text=data.decode("utf-8-sig");rows=re.findall(r"^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$",text,re.M)
    need(len(rows)==293 and len(dict(rows))==293,"status row count/duplicate mismatch")
    cur=re.findall(r"## Current Task\s+```text\s+([^\r\n]+)\s+```",text);need(len(cur)==1,"Current block mismatch")
    return dict(rows),cur[0].strip(),dict(collections.Counter(v for _,v in rows))
def predecessor(root,lock):
    top=Path(git(root,"rev-parse","--show-toplevel").stdout.decode().strip()).resolve();prefix=root.relative_to(top).as_posix();prefix="" if prefix=="." else prefix+"/"
    p=lock["predecessor"];commit=p["commit"]
    parents=git(root,"rev-list","--parents","-n","1",commit).stdout.decode().strip().split();need(parents==[commit,p["parent"]],"predecessor parent mismatch")
    git(root,"merge-base","--is-ancestor",commit,"HEAD")
    for e in p["commit_entries"]:
        b=git(root,"show",commit+":"+prefix+e["path"]).stdout;need(len(b)==e["bytes"] and sha(b)==e["sha256"],"predecessor blob mismatch: "+e["path"])
    return commit
def inbox_candidates(root):
    d=safe(root,INBOX)
    if not d.exists():return []
    out=[]
    for p in d.iterdir():
        need(not p.is_symlink(),"INBOX symlink rejected: "+p.name)
        if p.is_file() and p.suffix.lower()==".md":out.append(p)
        elif p.is_dir() and not (p/".APPLIED").exists():out.append(p)
    return sorted(out)
def local(root,m,lock,post=False):
    need(all((root/p).is_dir() for p in ("Assets","Packages","ProjectSettings","MapDesign/MCP")),"use Unity project root")
    need(HERE==safe(root,PREFIX).resolve(),"run installed INPUTS helper")
    predecessor(root,lock)
    rows,current,counts=status_state(safe(root,STATUS).read_bytes())
    need(rows.get(PREV)=="COMPLETE" and rows.get(NEXT)=="LOCKED","predecessor/next state mismatch")
    master=safe(root,MASTER).read_text(encoding="utf-8-sig")
    need(len(re.findall(r"^\|[^\r\n]*\b"+TASK+r"\b[^\r\n]*\|\s*$",master,re.M))==1,"task must occur once in Master")
    phase=rows.get(TASK)
    if not post:
        need(phase=="LOCKED" and current=="NONE" and counts=={"COMPLETE":255,"LOCKED":38},"pre state mismatch")
        need(not git(root,"diff","--quiet","HEAD","--",STATUS,MASTER,check=False).returncode,"Status/Master dirty")
        owned=lock["owned_existing"]
        need(git(root,"diff","--quiet","HEAD","--",*owned,check=False).returncode==0,"dirty changes overlap existing ownership")
        need(git(root,"diff","--cached","--quiet","HEAD","--",*owned,check=False).returncode==0,"staged changes overlap ownership")
        for p in lock["owned_new"]:need(not safe(root,p).exists(),"unexpected pre-existing output: "+p)
        target=safe(root,INBOX+"/"+TASK+".md");c=inbox_candidates(root);need(not c or (c==[target] and target.read_bytes()==(HERE/(TASK+".md")).read_bytes()),"INBOX has another candidate")
    else:
        need(phase in ("CURRENT","COMPLETE"),"post state must be CURRENT or COMPLETE")
        expcur="TASKS/"+TASK+".md" if phase=="CURRENT" else "NONE";need(current==expcur,"Current block mismatch")
        exp={"COMPLETE":255,"CURRENT":1,"LOCKED":37} if phase=="CURRENT" else {"COMPLETE":256,"LOCKED":37};need(counts==exp,"post counts mismatch")
        need(not inbox_candidates(root),"INBOX must be empty after Apply")
        bound=(HERE/(TASK+".md")).read_bytes();need(safe(root,"MapDesign/MCP/TASKS/"+TASK+".md").read_bytes()==bound,"installed task mismatch")
        arc=safe(root,"MapDesign/MCP_ARCHIVE/"+TASK+".md")
        if arc.exists():need(arc.read_bytes()==bound,"archive task mismatch")
        if phase=="COMPLETE":
            need(arc.exists(),"COMPLETE requires Archive")
            report=safe(root,"MapDesign/MCP/REPORTS/"+TASK+"_RESULT.md").read_text(encoding="utf-8-sig")
            need(re.search(r"^TASK: "+TASK+r"\r?$",report,re.M) and re.search(r"^STATUS: PASS\r?$",report,re.M),"COMPLETE requires PASS Result")
    return {"task":TASK,"phase":phase,"counts":counts,"predecessor":lock["predecessor"]["commit"],"bound_task_sha256":m["bound_task_sha256"]}
def main():
    ap=argparse.ArgumentParser();ap.add_argument("--root",type=Path,required=True);ap.add_argument("--expected-manifest-sha256",required=True);ap.add_argument("--mode",choices=("check","stage","post"),default="check");a=ap.parse_args()
    try:
        m,l=package(a.expected_manifest_sha256);root=a.root.resolve();r=local(root,m,l,a.mode=="post")
        if a.mode=="stage":
            target=safe(root,INBOX+"/"+TASK+".md");target.parent.mkdir(parents=True,exist_ok=True)
            if not target.exists():target.write_bytes((HERE/(TASK+".md")).read_bytes())
            r["inbox"]=target.relative_to(root).as_posix()
        print("PASS_"+a.mode.upper());print(json.dumps(r,indent=2))
    except (Blocked,OSError,ValueError,json.JSONDecodeError) as e:
        print("BLOCKED:",e,file=sys.stderr);sys.exit(2)
if __name__=="__main__":main()
