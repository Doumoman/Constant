#!/usr/bin/env python3
from __future__ import annotations

import csv
import re
import sys
from collections import defaultdict
from pathlib import Path

ID_RE = re.compile(r'^[A-Z0-9_]+$')


def read_csv(path: Path):
    with path.open('r', encoding='utf-8-sig', newline='') as f:
        reader = csv.DictReader(f)
        return reader.fieldnames or [], list(reader)


def split_list(value: str):
    if not value:
        return []
    return [x.strip() for x in value.split('|') if x.strip()]


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else '.').resolve()
    dictionary_path = root / '03_CSV_SCHEMA' / 'CSV_DATA_DICTIONARY.csv'
    static_dir = root / '04_CSV_STARTER'
    generated_dir = root / '05_GENERATED_OUTPUT_SCHEMA'
    errors: list[str] = []
    warnings: list[str] = []

    if not dictionary_path.exists():
        print(f'ERROR 1\n- missing {dictionary_path}')
        return 1

    _, dd_rows = read_csv(dictionary_path)
    spec_by_file = defaultdict(list)
    for row in dd_rows:
        spec_by_file[row['file_name']].append(row)
    for rows in spec_by_file.values():
        rows.sort(key=lambda r: int(r['column_order']))

    tables = {}
    paths = {}
    for filename, specs in sorted(spec_by_file.items()):
        p = static_dir / filename
        if not p.exists():
            p = generated_dir / filename
        if not p.exists():
            errors.append(f'{filename}: 파일 없음')
            continue
        headers, rows = read_csv(p)
        expected = [r['column_name'] for r in specs]
        if headers != expected:
            errors.append(f'{filename}: 헤더 불일치 expected={expected} actual={headers}')
            continue
        tables[filename] = rows
        paths[filename] = p

        # Scalars and required fields
        pk_cols = [r['column_name'] for r in sorted((s for s in specs if s['primary_key_order']), key=lambda s:int(s['primary_key_order']))]
        seen = {}
        for idx,row in enumerate(rows, start=2):
            for s in specs:
                name=s['column_name']; val=row.get(name,''); typ=s['data_type']
                if s['required']=='1' and val=='':
                    errors.append(f'{filename}:{idx}:{name}: 필수값 비어 있음')
                    continue
                if val=='':
                    continue
                try:
                    if typ in ('INT','ULONG'):
                        iv=int(val)
                        if typ=='ULONG' and iv<0: raise ValueError('negative ULONG')
                    elif typ=='FLOAT': float(val)
                    elif typ=='BOOL':
                        if val not in ('0','1'): raise ValueError('BOOL must be 0 or 1')
                    elif typ=='HEX':
                        int(val,16)
                    elif typ=='ID':
                        if not ID_RE.match(val): raise ValueError('ID regex')
                    elif typ=='ID_LIST':
                        for item in split_list(val):
                            if not ID_RE.match(item): raise ValueError(f'ID_LIST item {item}')
                except Exception as e:
                    errors.append(f'{filename}:{idx}:{name}: 타입 오류 value={val!r} ({e})')
            if pk_cols:
                key=tuple(row.get(c,'') for c in pk_cols)
                if key in seen:
                    errors.append(f'{filename}:{idx}: 기본키 중복 {key}, first row={seen[key]}')
                else:
                    seen[key]=idx

    # Foreign keys
    target_values = {}
    for filename, specs in spec_by_file.items():
        if filename not in tables: continue
        for s in specs:
            if s['primary_key_order']=='1':
                target_values[(filename,s['column_name'])] = {r[s['column_name']] for r in tables[filename] if r[s['column_name']]}
    for filename,specs in spec_by_file.items():
        if filename not in tables: continue
        for idx,row in enumerate(tables[filename],start=2):
            for s in specs:
                fk=s['foreign_key']; val=row.get(s['column_name'],'')
                if not fk or not val: continue
                target_file,target_col=fk.rsplit('.',1)
                valid=target_values.get((target_file,target_col),set())
                values=split_list(val) if s['data_type'] in ('ID_LIST','ENUM_LIST','INT_LIST') else [val]
                for item in values:
                    if item not in valid:
                        errors.append(f'{filename}:{idx}:{s["column_name"]}: 외래키 없음 {item} -> {fk}')

    # Domain checks
    def rows(name): return tables.get(name,[])

    # Fixed world
    for r in rows('world_profiles.csv'):
        expected={'width_tiles':'624','height_tiles':'416','sector_width_tiles':'48','sector_height_tiles':'32',
                  'sector_cols':'13','sector_rows':'13','micro_width_tiles':'12','micro_height_tiles':'8',
                  'micro_cols_per_sector':'4','micro_rows_per_sector':'4'}
        for k,v in expected.items():
            if r[k]!=v: errors.append(f'world_profiles.csv:{r["world_profile_id"]}: {k}={r[k]} expected {v}')

    # Route masks
    for r in rows('sector_route_masks.csv'):
        t=int(r['route_type']); l=r['open_l']=='1'; rr=r['open_r']=='1'; u=r['open_u']=='1'; d=r['open_d']=='1'
        valid = ((t==0 and not (l and rr)) or
                 (t==1 and l and rr and not u and not d) or
                 (t==2 and l and rr and not u and d) or
                 (t==3 and l and rr and u and not d))
        if not valid: errors.append(f'sector_route_masks.csv:{r["route_mask_id"]}: 타입/마스크 불일치')

    # Microchunk 96 cells
    cells=defaultdict(list)
    for r in rows('microchunk_tile_cells.csv'):
        cells[r['microchunk_id']].append((int(r['local_x']),int(r['local_y'])))
    for c in rows('microchunk_catalog.csv'):
        if c['tile_data_complete']!='1': continue
        coords=cells.get(c['microchunk_id'],[])
        if len(coords)!=96 or len(set(coords))!=96 or set(coords)!={(x,y) for x in range(12) for y in range(8)}:
            errors.append(f'microchunk {c["microchunk_id"]}: 96셀 완전성 실패 rows={len(coords)} unique={len(set(coords))}')

    # Sector 16 cells and XOR fixed/pool
    scells=defaultdict(list)
    for r in rows('sector_recipe_cells.csv'):
        scells[r['sector_recipe_id']].append((int(r['chunk_x']),int(r['chunk_y'])))
        if bool(r['fixed_microchunk_id']) == bool(r['microchunk_pool_id']):
            errors.append(f'sector_recipe_cells.csv:{r["sector_recipe_id"]} ({r["chunk_x"]},{r["chunk_y"]}): fixed/pool 중 정확히 하나 필요')
    for c in rows('sector_recipe_catalog.csv'):
        coords=scells.get(c['sector_recipe_id'],[])
        if len(coords)!=16 or len(set(coords))!=16 or set(coords)!={(x,y) for x in range(4) for y in range(4)}:
            errors.append(f'sector recipe {c["sector_recipe_id"]}: 16셀 완전성 실패 rows={len(coords)}')

    # Special footprint area
    fp=defaultdict(list)
    for r in rows('special_map_footprint_cells.csv'):
        fp[r['special_map_id']].append((int(r['local_sector_x']),int(r['local_sector_y'])))
    for s in rows('special_map_catalog.csv'):
        expected=int(s['footprint_width_sectors'])*int(s['footprint_height_sectors'])
        if len(fp.get(s['special_map_id'],[]))!=expected:
            errors.append(f'special map {s["special_map_id"]}: footprint rows {len(fp.get(s["special_map_id"],[]))} expected {expected}')

    # Village
    facilities={r['facility_id']:r for r in rows('village_facilities.csv')}
    for v in rows('village_profiles.csv'):
        fixed=split_list(v['fixed_facility_ids'])
        if set(fixed)!={'FAC_PUBLIC_KITCHEN','FAC_TOOL_REPAIR'}:
            errors.append(f'village {v["village_profile_id"]}: 고정 시설은 공용 부엌+도구 수리점이어야 함')
        if int(v['facility_count_min'])!=5 or int(v['facility_count_max'])!=6:
            errors.append(f'village {v["village_profile_id"]}: 시설 수 5~6 아님')
        for fid in fixed+split_list(v['optional_facility_ids']):
            if fid not in facilities: errors.append(f'village {v["village_profile_id"]}: facility 없음 {fid}')

    # Battery fixed costs and grenade terrain rule
    expected_cost={'BAT_MINI':5,'BAT_AIR_CANNON':20,'BAT_STANDARD':20,'BAT_MEGA':50,'BAT_GRENADE':20}
    bats={r['battery_id']:r for r in rows('battery_profiles.csv')}
    for bid,cost in expected_cost.items():
        if bid not in bats: errors.append(f'battery missing {bid}'); continue
        if int(bats[bid]['fuel_cost'])!=cost: errors.append(f'{bid}: fuel_cost expected {cost}')
    if 'BAT_GRENADE' in bats:
        g=bats['BAT_GRENADE']
        if g['terrain_damage_enabled']!='0' or any(g[k]!='0' for k in ('destroys_soft_soil','destroys_cracked_terrain','destroys_hard_terrain','destroys_starstone')):
            errors.append('BAT_GRENADE: 지형 파괴 플래그는 모두 0이어야 함')

    # Tool fragments
    expected_frag={1:3,2:4,3:5}
    for r in rows('tool_upgrade_definitions.csv'):
        lvl=int(r['upgrade_level'])
        if lvl in expected_frag and int(r['required_blueprint_fragments'])!=expected_frag[lvl]:
            errors.append(f'{r["tool_id"]} level {lvl}: fragment expected {expected_frag[lvl]}')

    # Warnings for starter content coverage
    boundary_pairs={(r['biome_a_id'],r['biome_b_id']) for r in rows('boundary_chunk_catalog.csv')}
    for p in rows('biome_boundary_pair_rules.csv'):
        a,b=p['biome_a_id'],p['biome_b_id']
        if (a,b) not in boundary_pairs and (b,a) not in boundary_pairs:
            warnings.append(f'경계 청크 스타터 미제작: {a} <-> {b}')
    pool_counts=defaultdict(int)
    for r in rows('sector_recipe_pool_entries.csv'): pool_counts[r['sector_recipe_pool_id']]+=1
    for pool,count in sorted(pool_counts.items()):
        if count<3: warnings.append(f'섹터 레시피 pool 후보 3개 미만: {pool} count={count}')

    print(f'ERROR {len(errors)}')
    for e in errors: print(f'- {e}')
    print(f'WARNING {len(warnings)}')
    for w in warnings: print(f'- {w}')
    return 1 if errors else 0


if __name__=='__main__':
    raise SystemExit(main())
