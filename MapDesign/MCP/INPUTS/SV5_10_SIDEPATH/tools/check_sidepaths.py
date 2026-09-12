#!/usr/bin/env python3
import argparse,csv,json,sys
from collections import Counter,defaultdict
from pathlib import Path

def need(ok,msg):
    if not ok: raise ValueError(msg)
def rows(path):
    with path.open(encoding="utf-8-sig",newline="") as f:return list(csv.DictReader(f))
def truth(v):return str(v).strip().lower() in ("1","true","yes","pass")
def points(raw):
    out=[]
    for part in raw.split(";"):
        if not part:continue
        x,y=part.split(":");out.append((int(x),int(y)))
    return out
def bbox_dist(a,b):
    dx=max(0,int(a["min_x"])-int(b["max_x"]),int(b["min_x"])-int(a["max_x"]))
    dy=max(0,int(a["min_y"])-int(b["max_y"]),int(b["min_y"])-int(a["max_y"]))
    return dx+dy
def pairkey(a,b):return "|".join(sorted((a,b)))

def check(root):
    links=rows(root/"sidepath_links.csv"); cells=rows(root/"sidepath_cells.csv")
    candidates=rows(root/"sidepath_candidates.csv"); sectors=rows(root/"sector_index.csv"); pairs=rows(root/"sector_pairs.csv")
    validation=json.loads((root/"sidepath_validation.json").read_text(encoding="utf-8-sig"))
    need(validation.get("status")=="PASS","validation status")
    need(len(links)>=8,"accepted<8")
    need(len({r["sector_id"] for r in links})>=6,"sectors<6")
    need(sum(r["type"]=="SIDE_PATH_RETURNING" for r in links)>=5,"returning<5")
    need(max(Counter(r["sector_id"] for r in links).values())<=2,"sector cap")
    need(len({r["sidepath_id"] for r in links})==len(links),"duplicate sidepath id")
    bypath=defaultdict(list)
    for c in cells:bypath[c["sidepath_id"]].append(c)
    for link in links:
        sid=link["sidepath_id"]; p=points(link["ordered_centerline"])
        need(20<=len(p)<=50 and len(p)==int(link["length"]),sid+" length")
        need(len(set(p))==len(p),sid+" self intersection")
        need(all(abs(a[0]-b[0])+abs(a[1]-b[1])==1 for a,b in zip(p,p[1:])),sid+" non-cardinal")
        dirs=[(b[0]-a[0],b[1]-a[1]) for a,b in zip(p,p[1:])]
        changes=sum(a!=b for a,b in zip(dirs,dirs[1:]))
        need(changes>=2 and changes==int(link["direction_changes"]),sid+" irregularity")
        run=[]
        for d in dirs:
            if not run or run[-1][0]!=d:run.append([d,1])
            else:run[-1][1]+=1
        need(len({x[1] for x in run})>=2,sid+" repeated run lengths")
        need(not any(run[i][1]==run[i+1][1]==run[i+2][1]==1 for i in range(max(0,len(run)-2))),sid+" sawtooth")
        cr=sorted(bypath[sid],key=lambda r:int(r["sequence"]))
        need(len(cr)==len(p),sid+" cell rows")
        carved=0
        for i,(r,pt) in enumerate(zip(cr,p)):
            need(int(r["sequence"])==i and (int(r["x"]),int(r["y"]))==pt,sid+" sequence")
            need(r["final_value"]=="AIR" and r["head_value"]=="AIR",sid+" clearance")
            role=r["movement_role"]
            need(role in ("SUPPORTED_FOOT","STEP_TRANSITION"),sid+" movement role")
            if role=="SUPPORTED_FOOT":need(r["support_value"]=="SOLID",sid+" support")
            need(0<=pt[0]<624 and 0<=pt[1]<416,sid+" bounds")
            carved += r["source_value"]!="AIR"
        for i,(a,b) in enumerate(zip(p,p[1:])):
            if a[1]!=b[1]:
                need(abs(a[1]-b[1])==1,sid+" rise")
                need(cr[i]["movement_role"]=="STEP_TRANSITION" or cr[i+1]["movement_role"]=="STEP_TRANSITION",sid+" unmarked step")
        need(not any(p[i][0]==p[i+1][0]==p[i+2][0] for i in range(max(0,len(p)-2))),sid+" long vertical tube")
        need(carved==int(link["newly_carved_air"]),sid+" carved count")
        need(carved>=8 and carved/len(p)>=0.40,sid+" insufficient new air")
        need(int(link["bypass_count"])==0 and truth(link["returnable"]),sid+" bypass/return")
        if link["type"]=="SIDE_PATH_RETURNING":
            need(truth(link["rejoins"]) and link["from_room"] and link["to_room"] and link["from_room"]!=link["to_room"],sid+" rejoin")
        else:need(link["type"]=="SIDE_PATH_DEAD_END" and not truth(link["rejoins"]),sid+" type")
    sec={r["sector_id"]:r for r in sectors if int(r["endpoint_count"])>0}
    actual={r["pair_key"] for r in pairs}
    need(len(actual)==len(pairs),"duplicate sector pair")
    expected=set()
    ids=sorted(sec)
    for i,a in enumerate(ids):
        for b in ids[i:]:
            if bbox_dist(sec[a],sec[b])<=49:expected.add(pairkey(a,b))
    need(actual==expected,"sector pair coverage")
    for r in pairs:
        need(r["pair_key"]==pairkey(r["sector_a"],r["sector_b"]),"sector pair normalization")
        need(int(r["lower_bound"])==bbox_dist(sec[r["sector_a"]],sec[r["sector_b"]])<=49,"sector lower bound")
    cids=set()
    for r in candidates:
        need(r["candidate_id"] not in cids,"duplicate candidate");cids.add(r["candidate_id"])
        need(pairkey(r["from_sector"],r["to_sector"]) in actual,"candidate outside sector pair")
    perf=validation["performance"]
    need(perf["world_index_builds"]==1,"world index builds")
    need(perf["whole_world_copy_per_candidate"]==0,"candidate world copy")
    need(perf["whole_world_bfs_per_candidate"]==0,"candidate global bfs")
    need(perf["global_product_runs"]==1,"global product runs")
    need(validation["metrics"]["bypass"]==0 and validation["metrics"]["protected_violations"]==0 and validation["metrics"]["type0_violations"]==0,"protected progression")
    return {"accepted":len(links),"returning":sum(r["type"]=="SIDE_PATH_RETURNING" for r in links),"sectors":len({r["sector_id"] for r in links}),"candidates":len(candidates),"sector_pairs":len(pairs)}

ap=argparse.ArgumentParser();ap.add_argument("--default",type=Path,required=True);ap.add_argument("--repeat",type=Path,required=True);ap.add_argument("--output",type=Path,required=True);a=ap.parse_args()
try:
    out={"schema":"SV5_10_INDEPENDENT_SIDEPATH_V1","status":"PASS","default":check(a.default),"repeat":check(a.repeat)}
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8")
    print("PASS_INDEPENDENT_SIDEPATH")
except Exception as e:
    print("FAIL_INDEPENDENT_SIDEPATH:",e,file=sys.stderr);sys.exit(1)
