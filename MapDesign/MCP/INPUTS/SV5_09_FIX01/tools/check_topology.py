#!/usr/bin/env python3
"""Independent topology/occupancy audit for SV5_09_FIX01. Imports no production code."""
import argparse, csv, hashlib, json, sys
from collections import defaultdict, deque
from pathlib import Path

def need(ok, message):
    if not ok: raise ValueError(message)

def table(path, required):
    with path.open(encoding="utf-8-sig", newline="") as f: data=list(csv.DictReader(f))
    need(data and set(required).issubset(data[0]), "columns/missing rows: "+str(path))
    return data

def point(row, px="x", py="y"): return int(row[px]), int(row[py])

def occupancy(path):
    data=table(path,{"x","y","value","provenance","owner"}); out={}
    for r in data:
        p=point(r); need(p not in out,"duplicate occupancy "+str(p)); out[p]=r["value"].upper()
    return out

def foot_nodes(occ):
    return {p for p,v in occ.items() if v=="AIR" and occ.get((p[0],p[1]+1))=="AIR" and occ.get((p[0],p[1]-1))=="SOLID"}

def foot_neighbors(p,nodes):
    for dx in (-1,1):
        for dy in (-1,0,1):
            q=(p[0]+dx,p[1]+dy)
            if q in nodes: yield q

def distance(nodes,start,goal):
    if start not in nodes or goal not in nodes:return -1
    q=deque([start]);d={start:1}
    while q:
        p=q.popleft()
        if p==goal:return d[p]
        for n in foot_neighbors(p,nodes):
            if n not in d:d[n]=d[p]+1;q.append(n)
    return -1

def graph(rows):
    edges={}; vertices=set()
    for r in rows:
        eid=r["edge_id"];a=r["from_vertex"];b=r["to_vertex"]
        need(eid not in edges and a!=b,"duplicate/self topology edge "+eid)
        edges[eid]=(a,b,r.get("kind",""),r.get("loop_id",""));vertices|={a,b}
    return vertices,edges

def adjacency(vertices,edges,omit=None):
    a={v:[] for v in vertices}
    for eid,(x,y,_,_) in edges.items():
        if eid==omit:continue
        a[x].append((y,eid));a[y].append((x,eid))
    return a

def connected(vertices,edges,start,goal,omit=None):
    a=adjacency(vertices,edges,omit);q=deque([start]);seen={start}
    while q:
        x=q.popleft()
        if x==goal:return True
        for y,_ in a.get(x,[]):
            if y not in seen:seen.add(y);q.append(y)
    return False

def components(vertices,edges):
    a=adjacency(vertices,edges);seen=set();count=0
    for v in vertices:
        if v in seen:continue
        count+=1;q=[v];seen.add(v)
        while q:
            x=q.pop()
            for y,_ in a[x]:
                if y not in seen:seen.add(y);q.append(y)
    return count

def rank(vertices,edges): return len(edges)-len(vertices)+components(vertices,edges)

def bridges(vertices,edges):
    return {eid for eid,(a,b,_,_) in edges.items() if not connected(vertices,edges,a,b,eid)}

def parse_path(value):
    return [tuple(map(int,p.split(":"))) for p in value.split("|") if p]

def variant(root):
    base_occ=occupancy(root/"baseline_occupancy.csv"); final_occ=occupancy(root/"final_occupancy.csv")
    base_nodes=foot_nodes(base_occ); final_nodes=foot_nodes(final_occ)
    exported_base={point(r) for r in table(root/"baseline_foot_nodes.csv",{"x","y"})}
    exported_final={point(r) for r in table(root/"final_foot_nodes.csv",{"x","y"})}
    need(base_nodes==exported_base,"baseline foot nodes not reconstructed")
    need(final_nodes==exported_final,"final foot nodes not reconstructed")
    base_rows=table(root/"baseline_topology_edges.csv",{"edge_id","from_vertex","to_vertex","kind","loop_id"})
    final_rows=table(root/"final_topology_edges.csv",{"edge_id","from_vertex","to_vertex","kind","loop_id"})
    bv,be=graph(base_rows);fv,fe=graph(final_rows)
    need(bv==fv,"topology vertex set changed")
    links=table(root/"loop_links.csv",{"loop_id","from_room","to_room","from_x","from_y","to_x","to_y","length","type","ordered_foot_path"})
    cells=table(root/"loop_cells.csv",{"loop_id","order","x","y","role","source_value","final_value"})
    proofs=table(root/"loop_topology_proofs.csv",{"loop_id","baseline_cost","final_cost","vertices_before","edges_before","components_before","rank_before","bridges_before","vertices_after","edges_after","components_after","rank_after","bridges_after","baseline_contacts","newly_carved_air"})
    proof={r["loop_id"]:r for r in proofs};by_cell=defaultdict(list)
    for r in cells:by_cell[r["loop_id"]].append(r)
    base_rank=rank(bv,be);final_rank=rank(fv,fe);base_bridges=bridges(bv,be);final_bridges=bridges(fv,fe)
    need(final_rank-base_rank==len(links),"global cycle delta not equal accepted links")
    results=[]
    for link in links:
        lid=link["loop_id"];need(lid in proof,"missing proof "+lid);p=proof[lid]
        need(lid in fe and fe[lid][3]==lid,"missing typed topology edge "+lid)
        a,b=link["from_room"],link["to_room"]
        need(connected(bv,be,a,b),"no baseline alternate room path "+lid)
        need(connected(fv,fe,a,b,lid),"loop edge is bridge/no alternate path "+lid)
        path=parse_path(link["ordered_foot_path"]);need(4<=len(path)<=24 and len(path)==int(link["length"]),"path length "+lid)
        need(len(path)==len(set(path)) and all(abs(x[0]-y[0])==1 and abs(x[1]-y[1])<=1 for x,y in zip(path,path[1:])),"unsupported foot path "+lid)
        contacts=[x for x in path if x in base_nodes]
        need(set(contacts)=={path[0],path[-1]},"baseline contact is not exactly two endpoints "+lid)
        overlay=dict(base_occ);new_air=0
        for row in by_cell[lid]:
            q=point(row);src=row["source_value"].upper();dst=row["final_value"].upper()
            need(overlay.get(q,"UNKNOWN")==src,"source occupancy mismatch "+lid+str(q));overlay[q]=dst
            if dst=="AIR" and src!="AIR" and q in path:new_air+=1
        nodes=foot_nodes(overlay);bc=distance(base_nodes,path[0],path[-1]);fc=distance(nodes,path[0],path[-1])
        need(bc>0 and fc>0,"unreachable cost "+lid)
        need((link["type"]=="RANDOM_SHORTCUT")== (fc<bc),"shortcut classification "+lid)
        need(new_air>=2,"not newly carved "+lid)
        checks={"baseline_cost":bc,"final_cost":fc,"vertices_before":len(bv),"edges_before":len(be),"components_before":components(bv,be),"rank_before":base_rank,"bridges_before":len(base_bridges),"vertices_after":len(fv),"edges_after":len(fe),"components_after":components(fv,fe),"rank_after":final_rank,"bridges_after":len(final_bridges),"baseline_contacts":2,"newly_carved_air":new_air}
        for k,v in checks.items():need(int(p[k])==v,"reported proof differs "+lid+" "+k)
        results.append({"loop_id":lid,"baseline_cost":bc,"final_cost":fc,"newly_carved_air":new_air})
    need(len(links)>=16,"density below 16")
    return {"accepted":len(links),"rank_before":base_rank,"rank_after":final_rank,"bridges_before":len(base_bridges),"bridges_after":len(final_bridges),"proofs":results}

def main():
    ap=argparse.ArgumentParser();ap.add_argument("--default",type=Path,required=True);ap.add_argument("--repeat",type=Path,required=True);ap.add_argument("--output",type=Path,required=True);a=ap.parse_args()
    result={"schema":"SV5_09_FIX01_INDEPENDENT_TOPOLOGY_V1","status":"PASS","default":variant(a.default),"repeat":variant(a.repeat)}
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8",newline="\n")
    print("PASS_INDEPENDENT_TOPOLOGY")

if __name__=="__main__":
    try:main()
    except (OSError,ValueError,KeyError,TypeError) as e:print("FAIL_INDEPENDENT_TOPOLOGY: "+str(e),file=sys.stderr);sys.exit(2)
