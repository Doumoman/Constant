# MAP01_10 — Implement Microchunk / Population / Item Definitions

```yaml
status_control:
  task_key: MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS
  result_file: REPORTS/MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS_RESULT.md
```

## TASK TYPE

`RUNTIME IMMUTABLE MICROCHUNK/POPULATION/ITEM DEFINITIONS + EDITMODE TESTS`

## Objective

MAP01_06 successful typed parse의 남은 static definition source exact 16개를 compile-time typed immutable definition 16종과 deterministic `MicrochunkPopulationItemDefinitionSet`으로 변환한다. 모든 column, optional empty/default, list order/duplicate, inactive row, exact source record를 보존하고 FK resolution·domain validation·Registry publish는 하지 않는다.

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
12. `REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md`

## READ ALLOWLIST

- Mandatory Read Order
- MAP01_02~09 schema/reader/validation/PK/parser/definition production C#과 direct focused tests
- `CsvSchemaDictionaryImporterTests.cs`, architecture fixture 3개, asmdef 4개
- WRITE ALLOWLIST의 기존 파일과 `.meta`
- Runtime Data 직계 파일명, CSV/meta 50개의 경로·SHA-256·BOM, `.meta` GUID, 변경 경로, Unity Console만 제한 검색

Authoring CSV data row, 비승인 C#, Scene/Prefab YAML, Legacy, later Task 본문은 읽지 말고 CSV를 수정·재저장하지 마.

## Exact Source File Set — 16

```text
map_element_definitions.csv
map_element_interactions.csv
microchunk_catalog.csv
microchunk_object_slots.csv
microchunk_pool_entries.csv
microchunk_sockets.csv
microchunk_tile_cells.csv
microchunk_variant_rules.csv
population_profiles.csv
prefab_registry.csv
resource_definitions.csv
resource_spawn_rules.csv
spawn_pool_entries.csv
special_item_slots.csv
tile_code_dictionary.csv
tool_upgrade_definitions.csv
```

generated output과 MAP01_07~09의 source는 포함하지 마.

## WRITE ALLOWLIST

신규 Runtime production C# 9개:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/PopulationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ItemResourceDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/AssetContentDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionSource.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuilder.cs
```

신규 test 1개:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuilderTests.cs
```

신규 C# 10개 + exact `.cs.meta` 10개 + Result 1개만 허용한다. 기존 파일은 수정하지 마.

## Namespace / Assembly

Runtime `StarNight.Map.WorldGeneration.Data`, test `StarNight.Map.Tests.WorldGeneration.Data`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode`를 사용하고 새 asmdef/asmref/package와 `UnityEditor` runtime 참조를 금지한다.

## Input / Type Contract

`MicrochunkPopulationItemDefinitionSource`는 exact schema와 successful parse result를 묶는 immutable input이다. builder는 exact 16 source 각 1회, parse success/errors 0, filename·column inventory/order·parsed/validated/source identity 일치를 검사한다. null/missing/duplicate/unexpected/unsuccessful/mismatch를 누적·안정 정렬하고 오류 시 partial set을 publish하지 않는다.

각 row를 1:1 definition으로 만들고 `STRING/ID/ENUM/HEX -> string`, `INT -> int`, `FLOAT -> float`, `BOOL -> bool`, `ID_LIST/ENUM_LIST -> IReadOnlyList<string>`, `INT_LIST -> IReadOnlyList<int>`로 매핑한다. `replace_slot_pool_pairs`는 opaque string으로 보존한다.

## Exact Definition Inventory

- `MapElementDefinition`, `MapElementInteractionDefinition`
- `MicrochunkDefinition`, `MicrochunkObjectSlotDefinition`, `MicrochunkPoolEntryDefinition`, `MicrochunkSocketDefinition`, `MicrochunkTileCellDefinition`, `MicrochunkVariantRuleDefinition`
- `PopulationProfileDefinition`
- `PrefabRegistryDefinition`
- `ResourceDefinition`, `ResourceSpawnRuleDefinition`
- `SpawnPoolEntryDefinition`, `SpecialItemSlotDefinition`, `TileCodeDefinition`, `ToolUpgradeDefinition`

모든 definition은 schema의 모든 column을 PascalCase typed property로 노출하고 exact `CsvParsedRecord SourceRecord`를 갖는다.

## Definition Set Contract

single-key definition은 `StringComparer.Ordinal` read-only dictionary로, composite-key definition은 ordinal/int stable sorted read-only collection으로 제공한다.

- dictionaries: map elements, microchunks, variant rules, population profiles, prefabs, resources, resource spawn rules, special item slots, tile codes
- composites: map interactions `(source,target)`, object slots `(microchunk,slot)`, microchunk pool entries `(pool,order)`, sockets `(microchunk,socket)`, tile cells `(microchunk,x,y)`, spawn pool entries `(pool,order)`, tool upgrades `(tool,level)`
- stable parent queries: microchunk별 slot/socket/tile, pool별 microchunk/spawn entry, tool별 upgrade, source별 interaction

source/row shuffle에 무관하게 membership/order가 같아야 하며 nested list도 read-only copy다.

## Error Contract

error code는 최소 `MissingSource`, `UnexpectedSource`, `DuplicateSource`, `UnsuccessfulParse`, `SchemaMismatch`, `FieldMappingFailed`를 구분한다. filename → record → column → code ordinal로 정렬하고 nullable source location을 보존한다. success는 errors 0/non-null set, any error는 null set이다.

## DO NOT

- FK existence/object resolution, polymorphic `entry_id`/interaction target resolution 금지
- tile 12×8 completeness/coordinate/layer/collision, socket/edge/band, marker/slot compatibility 검증 금지
- min/max/weight/budget/quantity/distance/footprint/durability 도메인 검증 금지
- pool candidate selection, transform, variant replacement parsing, population/spawn placement 금지
- prefab/addressable asset load, component check, ScriptableObject/cache/asset generation 금지
- active filtering, existing definitions/loader/parser/CSV 수정 금지
- FK resolver, Registry, hash, publish/report/window, MAP01_11 선행 금지
- asmdef/Scene/Prefab/Package/ProjectSettings/external dependency/Git operation 금지

## Collision Handling

파일이 없으면 생성하고, exact 계약과 byte-identical이면 `PREEXISTING_IDENTICAL`, 다르면 덮어쓰지 말고 `BLOCKED`다. 기존 meta GUID와 사용자 변경을 보존한다.

## Tests / Verification

`MicrochunkPopulationItemDefinitionBuilderTests` 최소 64 case로 16-source gate, 16 definition full-column mapping, type/empty/default/list/inactive/source identity, dictionary/composite/query stability/read-only, deterministic errors/no partial set, unresolved FK/opaque string/no domain validation을 검증한다.

```text
New microchunk/population/item definitions: >=64 PASS
Special/village definitions: 48/48 PASS
Biome/boundary definitions: 36/36 PASS
World/route definitions: 59/59 PASS
Parser 97 + PK 32 + validator 29 + reader 31 + schema 23 + importer 9 + architecture 10: ALL PASS
Targeted total: >=438 PASS
Full project EditMode: >=481 PASS
Unity 6000.3.8f1 / refresh PASS / compile errors 0 / relevant warnings 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50, 기존 C#/tests/asmdef 변경 0, 신규 meta 10 GUID 유효·중복 0을 확인한다. Unity 증거가 없으면 `BLOCKED`다.

## Result

`REPORTS/MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS_RESULT.md`

필수 섹션: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_09 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, SOURCE FILE CONTRACT, DEFINITIONS IMPLEMENTED, DEFINITION SET CONTRACT, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

## DONE CONDITIONS

- [ ] Current Task MAP01_10, Master 205, MAP01_09 COMPLETE/PASS
- [ ] exact 16 source/definition의 모든 column·empty/default/list/source/inactive 보존
- [ ] dictionaries/composites/queries immutable, stable, input-order independent
- [ ] error accumulation/no partial set, FK string/opaque string 보존, domain validation 미수행
- [ ] Runtime 9 + test 1 + meta 10만 신규, GUID 중복 0
- [ ] existing C#/tests/CSV/meta/asmdef/assets/Scene/Prefab 변경 0
- [ ] new >=64, targeted >=438, full >=481 PASS, compile/warning 0/0
- [ ] Result 완성, MAP01_11 미시작

## Completion Rule

exact `STATUS: PASS`와 모든 조건 충족 시만 MAP01_10을 COMPLETE로 finalize하고 Current Task를 NONE으로 만든다. MAP01_11은 LOCKED로 유지하고 자동 생성·실행하지 마.

## Recommended Commit

`feat(map): build immutable microchunk population item definitions`
