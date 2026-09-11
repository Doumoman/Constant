#!/usr/bin/env python3
"""Read-only CSV cross-check. Prints JSON; caller may save it in this Task's GENERATED scope."""
import argparse,csv,json,sys
from pathlib import Path
POLICY='SV5_INFILL_LENGTH_RULE_V2'

def rows(path):
    with path.open(encoding='utf-8-sig',newline='') as f:return list(csv.DictReader(f))

def points(value):
    result = [tuple(p) for p in json.loads(value)] if value.startswith('[') else ([tuple(map(int,p.split(':'))) for p in value.split('|')] if value else [])
    if any(len(p)!=2 or any(type(n) is not int for n in p) for p in result):raise ValueError('INVALID_POINT_LIST')
    return result

def point(value):return tuple(map(int,value.split(',')))

def full_path(path,access):
    if not path:raise ValueError('EMPTY_PATH')
    if access and access[-1]!=path[0]:raise ValueError('MISMATCHED_HOST_JOIN')
    result=access+path[1:] if access else list(path)
    if any(len(p)!=2 for p in result):raise ValueError('INVALID_POINT')
    if any(abs(a[0]-b[0])+abs(a[1]-b[1])!=1 for a,b in zip(result,result[1:])):raise ValueError('NON_CARDINAL_STEP')
    return result

def audit(directory):
    rr=rows(directory/'infill_rooms.csv');ll=rows(directory/'infill_links.csv');cc=rows(directory/'connections.csv')
    room={r['room_id']:r for r in rr};hosts={r['connection_id']:set(points(r['centerline'])) for r in cc}
    cellrows=rows(directory/'infill_cells.csv')
    air={(int(r['world_x']),int(r['world_y'])) for r in cellrows if r['base'].upper()=='AIR'}
    for r in cc:air.update(points(r['centerline']));air.update(points(r['aperture_cells']))
    errors=[];details=[]
    if len(room)!=len(rr) or len(ll)!=len(rr) or len(set(r['room_id'] for r in ll))!=len(ll) or set(r['room_id'] for r in ll)!=set(room):errors.append({'error':'ROOM_LINK_BIJECTION'})
    digests={r['plan_digest'] for table in [rr,ll,cc,cellrows] for r in table}
    if len(digests)!=1:errors.append({'error':'MIXED_PLAN_DIGEST'})
    for link in ll:
        rid=link['room_id'];errs=[]
        try:
            r=room[rid];legacy=r['legacy'].lower()=='true';path=points(link['ordered_cardinal_air_path']);access=points(link['host_access'])
            # No ownership filtering or global de-duplication.
            full=list(path) if legacy and not access else full_path(path,access)
            if not full:raise ValueError('EMPTY_PATH')
            if point(r['entry'])!=path[-1] and not legacy:errs.append('CHILD_ENTRY_NOT_END')
            if (link['parent'],link['host'])!=(r['parent'],r['host']):errs.append('LINK_OWNER_MISMATCH')
            if not legacy:
                parent=room.get(r['parent'])
                if parent:
                    if access:errs.append('CHILD_HAS_HOST_ACCESS')
                    x,y,w,h=map(int,parent['bounds'].split(','));px,py=path[0]
                    if not (x<=px<x+w and y<=py<y+h and (px in (x,x+w-1) or py in (y,y+h-1))):errs.append('PARENT_START_NOT_BOUNDARY')
                    if parent['host']!=r['host']:errs.append('PARENT_HOST_MISMATCH')
                elif len(access)<2 or access[0] not in hosts.get(r['host'],set()):errs.append('ROOT_HOST_ANCHOR_MISMATCH')
                if any(p not in air for p in full):errs.append('CENTERLINE_NOT_AIR')
                if len(full)>24:errs.append('OVER_24_INCLUSIVE')
            expected_status='NOT_APPLICABLE_LEGACY_INTERIOR' if legacy else 'PASS'
            if link.get('length_policy')!=POLICY:errs.append('MISSING_OR_WRONG_LENGTH_POLICY')
            if link.get('length_status')!=expected_status:errs.append('LENGTH_STATUS_MISMATCH')
            if points(link.get('connection_centerline',''))!=full:errs.append('EXPORTED_CENTERLINE_MISMATCH')
            if int(link.get('connection_cell_count','-1'))!=len(full):errs.append('EXPORTED_COUNT_MISMATCH')
            details.append({'room_id':rid,'legacy':legacy,'inclusive_count':len(full),'start':full[0],'end':full[-1],'errors':errs})
        except (ValueError,KeyError,IndexError) as e:errs.append('MALFORMED_LINK:'+str(e))
        errors.extend({'room_id':rid,'error':e} for e in errs)
    new=[r for r in details if not r['legacy']]
    if not new:errors.append({'error':'NO_NEW_CONNECTORS'})
    return {'case':directory.name,'success':not errors,'policy':POLICY,'plan_digests':sorted(digests),'new_connections_checked':len(new),'legacy_interior_links':sum(r['legacy'] for r in details),'maximum_inclusive':max((r['inclusive_count'] for r in new),default=0),'over_24':sum(r['inclusive_count']>24 for r in new),'errors':errors,'connections':details}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--generated-root',type=Path,required=True);a=p.parse_args()
    results=[audit(a.generated_root/c) for c in ('default','repeat')]
    good=all(r['success'] for r in results)
    print(json.dumps({'status':'PASS_LENGTH_CSV_ONLY' if good else 'FAIL_LENGTH_CSV','unity_verified':False,'cases':results},ensure_ascii=False,indent=2))
    return 0 if good else 1
if __name__=='__main__':
    try:sys.exit(main())
    except (OSError,ValueError,KeyError) as e:print(json.dumps({'status':'FAIL_LENGTH_CSV','error':str(e)}));sys.exit(1)
