#!/usr/bin/env python3
"""Audit the immutable FIX01 commit and write one bounded correction record."""
import argparse
import collections
import hashlib
import json
import os
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath

HERE = Path(__file__).resolve().parent.parent
TASK = "SV5_08_FIX02"
PREV = "SV5_08_FIX01"
NEXT = "SV5_09_LOOPS"

class AuditError(Exception): pass
def need(ok, reason):
    if not ok: raise AuditError(reason)
def sha(data): return hashlib.sha256(data).hexdigest()
def load(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def safe(root, relative):
    rel=PurePosixPath(relative)
    need(relative and not rel.is_absolute() and "\\" not in relative and ":" not in relative
         and all(p not in ("", ".", "..") for p in relative.split("/")), "Unsafe path: "+relative)
    result=root.joinpath(*rel.parts)
    need(result.resolve().is_relative_to(root.resolve()), "Path escapes project: "+relative)
    return result
def git(root, *args):
    p=subprocess.run(["git", "-C", str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    need(p.returncode==0, "Git verification failed: "+" ".join(args[:3]))
    return p.stdout
def counts(text):
    rows=re.findall(r'^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$',text,re.M)
    current=re.findall(r'## Current Task\s+```text\s+([^\r\n]+)',text)
    need(len(rows)==292 and len(dict(rows))==292 and len(current)==1,"Status structure mismatch")
    return dict(rows),current[0].strip(),dict(collections.Counter(v for _,v in rows))
def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--project-root",type=Path,required=True)
    ap.add_argument("--expected-package-sha",required=True)
    ap.add_argument("--output",default="MapDesign/MCP/GENERATED/SV5_08_FIX02/finalize_audit.json")
    args=ap.parse_args();root=args.project_root.resolve()
    need(sha((HERE/"FILES.json").read_bytes())==args.expected_package_sha,"Package manifest SHA mismatch")
    spec=load(HERE/"AUDIT_SPEC.json");index=load(HERE/"FIX01_REVIEW_INDEX.json")
    commit=index["commit"];parent=index["parent"]
    need(git(root,"rev-list","--parents","-n","1",commit).decode().strip().split()==[commit,parent],"Commit parent mismatch")
    top=Path(os.fsdecode(git(root,"rev-parse","--show-toplevel")).strip()).resolve()
    need(root.is_relative_to(top),"Unity root outside repository")
    prefix=root.relative_to(top).as_posix();prefix="" if prefix=="." else prefix+"/"
    checked=[]
    for e in index["entries"]:
        blob=git(root,"show",commit+":"+prefix+e["path"])
        oid=hashlib.sha1(b"blob "+str(len(blob)).encode()+b"\0"+blob).hexdigest()
        need(sha(blob)==e["blob_sha256"] and len(blob)==e["blob_bytes"] and oid==e["blob_oid"],"Commit blob mismatch: "+e["path"])
        checked.append(e["path"])
    prior=safe(root,spec["previous_result"]["path"]).read_bytes()
    need(sha(prior)==spec["previous_result"]["sha256"],"Prior Result changed")
    text=prior.decode("utf-8-sig")
    present=re.findall(r'^TASK_ID: SV5_08_FIX01\r?$',text,re.M)
    missing=re.findall(r'^TASK: SV5_08_FIX01\r?$',text,re.M)
    need(len(present)==1 and not missing and re.search(r'^STATUS: PASS\r?$',text,re.M),"Prior marker defect not exact")
    helper=safe(root,spec["defect"]["old_precheck_path"]).read_text(encoding="utf-8-sig")
    need("re.search(r'^TASK: ' + TASK" in helper and "COMPLETE requires matching PASS Result" in helper,"Old PRECHECK requirement changed")
    xml=safe(root,"MapDesign/MCP/GENERATED/SV5_08_FIX01/focused_results.xml").read_bytes()
    root_xml=ET.fromstring(xml);tests=list(root_xml.iter("test-case"))
    results=collections.Counter(t.get("result") for t in tests)
    need(len(tests)==102 and len({t.get("fullname") for t in tests})==102 and results=={"Passed":102},"Prior XML not102/102 PASS")
    length=load(safe(root,"MapDesign/MCP/GENERATED/SV5_08_FIX01/length_audit.json"))
    need(length.get("status")=="PASS_LENGTH_CSV_ONLY" and length.get("unity_verified") is False,"Length audit role/status mismatch")
    length_summary=[]
    for case,expected in spec["expected_length"].items():
        item=next((x for x in length["cases"] if x["case"]==case),None)
        need(item and item["success"] and item["new_connections_checked"]==expected["new_connections"]
             and item["maximum_inclusive"]==expected["maximum"] and item["over_24"]==expected["over"] and not item["errors"],"Length summary mismatch: "+case)
        validation=load(safe(root,"MapDesign/MCP/GENERATED/SV5_08_FIX01/"+case+"/infill_validation.json"))
        need(validation["CONNECTION_LENGTH"] and validation["CONTACT"] and validation["PHYSICAL_PRODUCT"]
             and not validation["COMPOSED"] and not validation["PLAYER"] and not validation["errors"],"Validation mismatch: "+case)
        length_summary.append({"case":case,"new_connections":item["new_connections_checked"],"maximum":item["maximum_inclusive"],"over":item["over_24"]})
    rows,current,state=counts(safe(root,"MapDesign/MCP/06_IMPLEMENTATION_STATUS.md").read_text(encoding="utf-8-sig"))
    need(rows.get(PREV)=="COMPLETE" and rows.get(NEXT)=="LOCKED" and rows.get(TASK) in ("CURRENT","COMPLETE"),"Task state mismatch")
    expected_current="TASKS/SV5_08_FIX02.md" if rows[TASK]=="CURRENT" else "NONE"
    need(current==expected_current,"Current Task mismatch")
    result={"schema":"SV5_08_FIX02_FINALIZE_AUDIT_V1","status":"PASS_AUDITED_CORRECTION","task":TASK,
        "prior_task":PREV,"prior_commit":commit,"prior_parent":parent,"commit_blobs_verified":len(checked),
        "prior_result_sha256":sha(prior),"prior_result_modified":False,"present_marker":"TASK_ID: SV5_08_FIX01",
        "missing_required_marker":"TASK: SV5_08_FIX01","complete_post_readonly_claim_reproducible":False,
        "implementation_evidence":{"code_or_test_changed_by_this_task":False,"unity_run_this_task":False,
            "prior_focused_total":len(tests),"prior_focused_passed":results["Passed"],"prior_xml_raw_sha256":sha(xml),
            "length":length_summary,"composed_geometry_ready":False,"player_verified":False},
        "correction":"FIX01 code/data evidence passes; its COMPLETE-state post-readonly claim is not reproducible. Downstream binds FIX02.",
        "state":{"phase":rows[TASK],"counts":state,"current":current,"next":"LOCKED"}}
    output=safe(root,args.output)
    need(args.output=="MapDesign/MCP/GENERATED/SV5_08_FIX02/finalize_audit.json","Output path outside exact allowlist")
    output.parent.mkdir(parents=True,exist_ok=True)
    data=(json.dumps(result,ensure_ascii=False,indent=2)+"\n").encode()
    with tempfile.NamedTemporaryFile(dir=output.parent,delete=False) as f:
        f.write(data);f.flush();os.fsync(f.fileno());temporary=Path(f.name)
    os.replace(temporary,output)
    need(output.read_bytes()==data,"Audit output verification failed")
    print(json.dumps({"status":"PASS_AUDITED_CORRECTION","output":args.output,"bytes":len(data),"sha256":sha(data)},ensure_ascii=False,indent=2))
if __name__=="__main__":
    try: main()
    except (AuditError,OSError,ValueError,KeyError,TypeError,ET.ParseError) as e:
        print(json.dumps({"status":"BLOCKED","reason":str(e)},ensure_ascii=False));raise SystemExit(2)
