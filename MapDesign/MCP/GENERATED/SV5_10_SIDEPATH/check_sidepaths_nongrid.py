#!/usr/bin/env python3
import argparse,csv,json,sys
from collections import Counter,defaultdict
from pathlib import Path

def need(ok,msg):
    if not ok: raise ValueError(msg)
def rows(path):
    with path.open(encoding="utf-8-sig",newline="") as f:return list(csv.DictReader(f))
def truth(value):return str(value).strip().lower() in ("1","true","yes","pass")
def points(raw):
    return [tuple(map(int,item.split(":"))) for item in raw.split(";") if item]
def pairkey(a,b):return "|".join(sorted((a,b)))
def roomkey(a,b):return "|".join(sorted((a,b)))

def check(root):
    endpoints=rows(root/"endpoint_index.csv"); pairs=rows(root/"endpoint_pairs.csv")
    candidates=rows(root/"sidepath_candidates.csv"); links=rows(root/"sidepath_links.csv")
    cells=rows(root/"sidepath_cells.csv"); changed=rows(root/"sidepath_changed_cells.csv")
    protected={(int(r["x"]),int(r["y"])) for r in rows(root/"protected_cells.csv")}
    direct={roomkey(r["room_a"],r["room_b"]) for r in rows(root/"direct_room_pairs.csv")}
    validation=json.loads((root/"sidepath_validation.json").read_text(encoding="utf-8-sig"))
    performance=json.loads((root/"sidepath_search_performance.json").read_text(encoding="utf-8-sig"))
    ordered=sorted(endpoints,key=lambda r:(int(r["x"]),int(r["y"]),r["room_id"],r["endpoint_id"]))
    need(endpoints==ordered,"endpoint ordering")
    need(len({r["endpoint_id"] for r in endpoints})==len(endpoints),"duplicate endpoint")
    expected=set()
    for i,left in enumerate(endpoints):
        for right in endpoints[i+1:]:
            if int(right["x"])-int(left["x"])>49:break
            expected.add(pairkey(left["endpoint_id"],right["endpoint_id"]))
    actual={r["pair_key"] for r in pairs}
    need(len(actual)==len(pairs),"duplicate normalized endpoint pair")
    need(actual==expected,"X sliding-window pair completeness")
    endpoint={r["endpoint_id"]:r for r in endpoints}
    pair={r["pair_key"]:r for r in pairs}
    for r in pairs:
        a=endpoint[r["from_endpoint"]];b=endpoint[r["to_endpoint"]]
        dx=abs(int(a["x"])-int(b["x"]));dy=abs(int(a["y"])-int(b["y"]));man=dx+dy
        need(int(r["dx"])==dx and int(r["dy"])==dy and int(r["manhattan"])==man,"pair distance")
        need(r["pair_key"]==pairkey(a["endpoint_id"],b["endpoint_id"]),"pair normalization")
        if truth(r["eligible"]):
            need(man<=49 and a["room_id"]!=b["room_id"],"eligible pair boundary")
            need(roomkey(a["room_id"],b["room_id"]) not in direct,"direct pair eligible")
            need(not r["rejection_reason"],"eligible reason")
        if a["room_id"]==b["room_id"]:need(r["rejection_reason"]=="SAME_ROOM","same room rejection")
        elif man>49:need(r["rejection_reason"]=="MANHATTAN_GT_49","Manhattan rejection")
        elif roomkey(a["room_id"],b["room_id"]) in direct:need(r["rejection_reason"]=="DIRECT_ROOM_PAIR","direct rejection")
    need(len({r["candidate_id"] for r in candidates})==len(candidates),"duplicate candidate")
    for r in candidates:
        need(r["from_endpoint"] in endpoint,"candidate source endpoint")
        if r["type"]=="SIDE_PATH_RETURNING":
            need(r["to_endpoint"] in endpoint,"returning target endpoint")
            need(truth(pair[pairkey(r["from_endpoint"],r["to_endpoint"])]["eligible"]),"returning ineligible pair")
        else:need(r["type"]=="SIDE_PATH_DEAD_END" and not r["to_endpoint"],"dead-end endpoint")
    bypath=defaultdict(list)
    for r in cells:bypath[r["sidepath_id"]].append(r)
    for link in links:
        sid=link["sidepath_id"];path=points(link["ordered_centerline"])
        need(20<=len(path)<=50 and len(path)==int(link["length"]),sid+" length")
        need(len(set(path))==len(path),sid+" self-cross")
        need(all(abs(a[0]-b[0])+abs(a[1]-b[1])==1 for a,b in zip(path,path[1:])),sid+" cardinal")
        cr=sorted(bypath[sid],key=lambda r:int(r["sequence"]));need(len(cr)==len(path),sid+" cell rows")
        carved=0
        for i,(r,p) in enumerate(zip(cr,path)):
            need(int(r["sequence"])==i and (int(r["x"]),int(r["y"]))==p,sid+" sequence")
            need(r["final_value"]=="AIR" and r["head_value"]=="AIR",sid+" clearance")
            need(r["movement_role"] in ("SUPPORTED_FOOT","STEP_TRANSITION"),sid+" movement")
            if r["movement_role"]=="SUPPORTED_FOOT":need(r["support_value"]=="SOLID",sid+" support")
            carved += r["source_value"]!="AIR"
        need(carved==int(link["newly_carved_air"]) and carved>=8 and carved/len(path)>=.4,sid+" carved")
        source=endpoint[link["from_endpoint"]]
        need(link["from_room"]==source["room_id"] and link["space_group_id"]==source["space_group_id"],sid+" source identity")
        if link["type"]=="SIDE_PATH_RETURNING":
            target=endpoint[link["to_endpoint"]]
            need(link["to_room"]==target["room_id"] and link["from_room"]!=link["to_room"],sid+" returning rooms")
            need(truth(link["rejoins"]) and truth(link["returnable"]),sid+" return")
        else:
            need(link["type"]=="SIDE_PATH_DEAD_END" and not link["to_endpoint"] and not truth(link["rejoins"]) and not truth(link["returnable"]),sid+" dead end")
        need(int(link["bypass_count"])==0,"bypass")
    changed_points={(int(r["x"]),int(r["y"])) for r in changed}
    need(changed_points.isdisjoint(protected),"protected/type0/progression changed cell")
    groups=Counter(r["space_group_id"] for r in links)
    need(len(links)>=8 and sum(r["type"]=="SIDE_PATH_RETURNING" for r in links)>=5,"production density")
    need(len(groups)>=6 and max(groups.values())<=2,"SpaceGroup distribution")
    need(validation["status"]=="PASS","validation")
    need(validation["metrics"]["bypass"]==validation["metrics"]["protected_violations"]==validation["metrics"]["type0_violations"]==0,"progression metrics")
    need(performance["workers"]==1 and performance["whole_world_copy_per_candidate"]==0 and performance["whole_world_bfs_per_candidate"]==0,"production performance")
    return {"endpoints":len(endpoints),"nearby_pairs":len(pairs),"eligible_pairs":sum(truth(r["eligible"]) for r in pairs),
        "generated":len(candidates),"accepted":len(links),"returning":sum(r["type"]=="SIDE_PATH_RETURNING" for r in links),"space_groups":len(groups)}

ap=argparse.ArgumentParser();ap.add_argument("--default",type=Path,required=True);ap.add_argument("--repeat",type=Path,required=True)
ap.add_argument("--correction",type=Path,required=True);ap.add_argument("--worker-determinism",type=Path,required=True);ap.add_argument("--output",type=Path,required=True);a=ap.parse_args()
try:
    correction=json.loads(a.correction.read_text(encoding="utf-8-sig"));workers=json.loads(a.worker_determinism.read_text(encoding="utf-8-sig"))
    need(correction["correction_applied_before_finalize"] and correction["original_inputs_bytes_unchanged"] and not correction["production_sector_dependency"],"contract correction")
    need(workers["worker_1"]["workers"]==1 and workers["worker_4"]["workers"]==4 and workers["digest_match"] and workers["worker_1"]["digest"]==workers["worker_4"]["digest"],"worker digest")
    out={"schema":"SV5_10_INDEPENDENT_SIDEPATH_NONGRID_V2","status":"PASS_INDEPENDENT_SIDEPATH","default":check(a.default),"repeat":check(a.repeat),
        "worker_digest_match":True,"correction_sha_bound":True}
    a.output.write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8");print("PASS_INDEPENDENT_SIDEPATH")
except Exception as exc:
    print("FAIL_INDEPENDENT_SIDEPATH:",exc,file=sys.stderr);sys.exit(1)
