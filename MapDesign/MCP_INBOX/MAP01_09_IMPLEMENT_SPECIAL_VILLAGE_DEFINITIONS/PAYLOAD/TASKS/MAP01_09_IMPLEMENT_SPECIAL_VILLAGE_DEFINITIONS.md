# MAP01_09 — Implement Special / Village Definitions

```yaml
status_control:
  task_key: MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS
  result_file: REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE SPECIAL-MAP/VILLAGE DEFINITIONS + EDITMODE TESTS
```

## Objective

MAP01_06의 successful typed parse 결과 중 special map/footprint/entry/reward/event와 village/shop exact 12개 CSV를 compile-time typed immutable definition 12종과 deterministic `SpecialVillageDefinitionSet`으로 변환한다.

모든 typed column, optional empty, default 적용, list order/duplicate, inactive row와 source record를 보존한다. 참조는 아직 string ID이며 FK resolution, domain validation, Registry publish는 수행하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 TASK
12. `REPORTS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02~06 Runtime schema/reader/validation/PK/parser production C# 34개
- MAP01_07 world/route definition production C# 8개
- MAP01_08 biome/boundary definition production C# 7개
- MAP01_02~08의 direct focused test 7개
- `CsvSchemaDictionaryImporterTests.cs`, architecture fixture 3개
- Runtime/Editor/test asmdef 4개
- 이 TASK WRITE ALLOWLIST의 기존 파일과 `.meta`

제한적 검색 허용:

- Runtime `WorldGeneration/Data/`와 Runtime Data test 디렉터리의 직계 파일명
- Authoring CSV/meta 50개의 경로·SHA-256·BOM만 확인
- 전체 `.meta` GUID 중복 검사
- 작업 전후 변경 경로와 Unity Console 상태

금지:

- Authoring CSV data row 내용 읽기·도메인 의미 분석
- 승인되지 않은 C# 본문, Scene/Prefab YAML, Legacy, later Task 본문 읽기
- CSV 수정·재저장

## Exact Source File Set — 12

```text
event_activation_routes.csv
special_map_catalog.csv
special_map_entry_sockets.csv
special_map_footprint_cells.csv
special_map_rewards.csv
shop_archetypes.csv
shop_inventory_rules.csv
shopkeeper_species.csv
village_facilities.csv
village_layout_catalog.csv
village_layout_cells.csv
village_profiles.csv
```

biome/boundary, microchunk/population/item, generated output은 이 definition set에 포함하지 않는다.

## WRITE ALLOWLIST

### 신규 Runtime production C# — 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/VillageDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSource.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuilder.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
```

신규 C# 8개와 대응 `.cs.meta` 8개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용: `MapDesign/MCP/REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md`

TASK 중 status는 수정하지 않고 PASS 이후 finalize만 수행한다.

## Namespace / Assembly Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Assembly           : existing Game.Map.Runtime / Game.Map.Tests.EditMode
```

`UnityEditor` 참조와 새 asmdef/asmref/package는 금지한다.

## Input Gate Contract

`SpecialVillageDefinitionSource`는 정확히 하나의 `CsvFileSchema`와 그 파일의 successful `CsvScalarAndListParseResult`를 묶는 immutable input이다.

builder는 exact 12개 source가 각각 한 번 존재하고, 모든 parse가 successful/errors 0이며, schema filename·exact column inventory/order·parsed/validated/source identity가 일치해야 한다. null/missing/duplicate/unexpected/unsuccessful/mismatched source 오류를 안전한 범위에서 모두 수집하고 오류 시 partial set을 publish하지 않는다.

## Definition Type / Column Contract

각 definition은 CSV row와 1:1이며 모든 column을 PascalCase compile-time typed property로 노출한다. `STRING/ID/ENUM -> string`, `INT -> int`, `FLOAT -> float`, `BOOL -> bool`, `ID_LIST/ENUM_LIST -> IReadOnlyList<string>`, `INT_LIST -> IReadOnlyList<int>`다. optional empty는 `string.Empty`, list는 order/duplicate를 보존한 read-only copy이다. 모든 definition은 exact `CsvParsedRecord SourceRecord`를 갖고 inactive row도 보존한다.

### Special map definitions

- `EventActivationRouteDefinition`: event_route_id, special_map_id, event_id, mandatory, allowed_sector_types, requires_tool, requires_consumable, min_safe_tiles_before_trigger, return_path_required, trigger_slot_id, notes
- `SpecialMapDefinition`: special_map_id, display_name_ko, site_role, primary_biome_id, footprint_width_sectors, footprint_height_sectors, required_count, min_graph_distance_from_start, min_graph_distance_to_other_core_sites, allowed_entry_route_types, requires_tool, mandatory_reward_id, generation_mode, active, notes
- `SpecialMapEntrySocketDefinition`: special_map_id, entry_socket_id, local_sector_x, local_sector_y, side, allowed_route_types, required, return_path_required, notes
- `SpecialMapFootprintCellDefinition`: special_map_id, local_sector_x, local_sector_y, local_role, required_primary_biome_id, fixed_sector_recipe_id, required_open_sides, notes
- `SpecialMapRewardDefinition`: special_map_id, reward_order, reward_id, reward_kind, mandatory, slot_id, quantity_min, quantity_max, notes

### Village/shop definitions

- `ShopArchetypeDefinition`: shop_archetype_id, display_name_ko, shop_type, item_slot_count_min, item_slot_count_max, base_price_multiplier, allows_reputation_reward, active, notes
- `ShopInventoryRuleDefinition`: shop_archetype_id, slot_index, spawn_pool_id, guaranteed, quantity_min, quantity_max, price_min_gold, price_max_gold, required_favor_tier, active, notes
- `ShopkeeperSpeciesDefinition`: species_id, display_name_ko, prefab_id, dialogue_style_id, animation_set_id, selection_weight, allowed_biome_ids, active, notes
- `VillageFacilityDefinition`: facility_id, display_name_ko, facility_group, fixed, selection_weight, prefab_id, shop_archetype_id, evacuated_prefab_id, active, notes
- `VillageLayoutDefinition`: village_layout_id, display_name_ko, footprint_width_sectors, footprint_height_sectors, target_facility_count, entry_sides, selection_weight, active, notes
- `VillageLayoutCellDefinition`: village_layout_id, local_chunk_x, local_chunk_y, cell_role, facility_slot_id, fixed_microchunk_id, microchunk_pool_id, required_entry_side, notes
- `VillageProfileDefinition`: village_profile_id, display_name_ko, world_profile_id, facility_count_min, facility_count_max, fixed_facility_ids, optional_facility_ids, allowed_layout_ids, start_distance_buckets, maximum_sector_count, active, notes

## Definition Set Contract

`StringComparer.Ordinal` read-only dictionary:

```text
EventActivationRoutes // EventRouteId
SpecialMaps           // SpecialMapId
ShopArchetypes        // ShopArchetypeId
ShopkeeperSpecies     // SpeciesId
VillageFacilities     // FacilityId
VillageLayouts        // VillageLayoutId
VillageProfiles       // VillageProfileId
```

ordinal composite-key ordered read-only collections:

```text
SpecialMapEntrySockets   // (SpecialMapId, EntrySocketId)
SpecialMapFootprintCells // (SpecialMapId, LocalSectorX, LocalSectorY)
SpecialMapRewards        // (SpecialMapId, RewardOrder)
ShopInventoryRules       // (ShopArchetypeId, SlotIndex)
VillageLayoutCells       // (VillageLayoutId, LocalChunkX, LocalChunkY)
```

dictionary enumeration과 composite collection은 source/row order에 무관하게 ordinal 안정 정렬한다. special-map 별 entry/footprint/reward, shop 별 inventory, layout 별 cell query를 read-only stable view로 제공한다.

## Build Error Contract

`SpecialVillageDefinitionBuildErrorCode`는 최소 `MissingSource`, `UnexpectedSource`, `DuplicateSource`, `UnsuccessfulParse`, `SchemaMismatch`, `FieldMappingFailed`를 구분한다. 오류는 filename → record number → column order → error code ordinal로 정렬하고 실제 source location을 보존한다. success면 errors 0/non-null set, 오류가 하나라도 있으면 null set이다.

## Scope Boundary / DO NOT

- special/village/shop/event/reward/biome/microchunk/item FK lookup·object resolve 금지
- footprint count/local coordinate, min/max/quantity/price/weight 도메인 검증 금지
- reward/event polymorphic target, trigger reachability, entry compatibility 검증 금지
- village 5~6 시설, fixed/optional membership, shop inventory slot 규칙 검증 금지
- `start_distance_buckets`를 분해·파싱하지 말고 exact string으로 보존
- active row filtering, candidate selection, layout placement 금지
- 기존 world/route/biome/boundary definition 수정 금지
- microchunk/population/item/generated definition, FK resolver, Registry, hash, report/window 금지
- reader/schema/validator/PK/parser/importer/CSV/asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency와 Git operation 금지
- MAP01_10 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Implementation Steps

1. Current Task MAP01_09, Master 205개, MAP01_08 COMPLETE/PASS를 확인한다.
2. MAP01_08의 definitions 36/36, targeted 326/326, full 369/369, compile/warning 0/0을 확인한다.
3. 작업 전 inventory/hash를 기록하고 allowlisted API/test/asmdef만 읽는다.
4. exact 12 row definition과 source/set/error/result/builder를 구현한다.
5. 모든 column을 typed property로 1:1 매핑한다.
6. 최소 48개 definition unit test를 구현한다.
7. Unity refresh/compile 후 신규와 모든 targeted regression을 실행한다.
8. 신규 meta 8개 GUID, CSV/meta 50/50, 비허용 변경 0을 확인한다.
9. Result를 작성하고 모든 조건 충족 시만 PASS를 기록한다.

## Required Tests

`SpecialVillageDefinitionBuilderTests` 최소 48 case: exact 12-source success, shuffled order stability, missing/duplicate/unexpected/unsuccessful/mismatch error, 12개 타입 full-column mapping, scalar/list/default/empty/inactive/source identity 보존, 7 dictionary·5 composite collection·parent query ordering/read-only, deterministic error accumulation/no partial set, unresolved FK string, start-distance exact string, no domain validation을 검증한다.

Targeted regression:

```text
New special/village definitions: >=48 / ALL PASS
Biome/boundary definitions: 36/36 PASS
World/route definitions: 59/59 PASS
Scalar/list parser: 97/97 PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=374 / ALL PASS
Full project EditMode: >=417 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=374 / ALL PASS
Full EditMode: >=417 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

`REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md`

필수 섹션: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_08 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, SOURCE FILE CONTRACT, DEFINITIONS IMPLEMENTED, DEFINITION SET CONTRACT, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

## DONE CONDITIONS

- [ ] Current Task가 MAP01_09이고 Master 205개/MAP01_08 COMPLETE/PASS를 확인했다.
- [ ] exact 12 source와 12 row definition만 사용했다.
- [ ] 모든 schema column, empty/default/list/source/inactive를 typed property로 보존했다.
- [ ] 7 dictionary와 5 composite collection/query가 immutable·stable하다.
- [ ] 오류를 누적하고 partial set을 publish하지 않는다.
- [ ] FK는 string으로 보존하고 domain validation/placement를 수행하지 않았다.
- [ ] 신규 Runtime 7 + test 1 + meta 8만 생성했고 GUID 중복이 없다.
- [ ] 기존 C#/tests/CSV/meta/asmdef/assets/Scene/Prefab를 수정하지 않았다.
- [ ] targeted >=374, full >=417이 모두 PASS이고 compile/warning 0/0이다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_10을 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가 MAP01_09를 COMPLETE로 바꾸고 Last Completed/Result를 갱신한 뒤 Current Task를 NONE으로 만든다. MAP01_10 이후는 LOCKED로 유지하며 자동 생성·실행하지 않는다.

## Recommended Commit

```text
feat(map): build immutable special map and village definitions
```
