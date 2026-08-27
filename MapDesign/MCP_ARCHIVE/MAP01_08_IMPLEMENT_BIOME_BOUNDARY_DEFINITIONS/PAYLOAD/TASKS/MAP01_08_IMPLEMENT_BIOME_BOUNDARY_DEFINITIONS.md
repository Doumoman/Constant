# MAP01_08 — Implement Biome Boundary Definitions

```yaml
status_control:
  task_key: MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS
  result_file: REPORTS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE BIOME/BOUNDARY DEFINITIONS + EDITMODE TESTS
```

## Objective

MAP01_06의 successful typed parse 결과 중 biome type, biome patch rule, biome boundary profile/pair, boundary chunk catalog exact 5개 CSV를 compile-time typed immutable definition 5종과 deterministic `BiomeBoundaryDefinitionSet`으로 변환한다.

모든 typed column, optional empty, default 적용, list order/duplicate, inactive row와 source record를 보존한다. 모든 참조는 아직 string ID이며 FK resolution, pair/domain validation, Registry publish는 수행하지 않는다.

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
12. `REPORTS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02 Runtime schema production C# 8개
- MAP01_03 Runtime reader production C# 7개
- MAP01_04 Runtime validation production C# 6개
- MAP01_05 Runtime PK production C# 6개
- MAP01_06 Runtime parser production C# 7개
- MAP01_07 Runtime world/route definition production C# 8개
- MAP01_02~07의 direct focused test 6개
- `CsvSchemaDictionaryImporterTests.cs`
- architecture fixture 3개
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

## Exact Source File Set — 5

```text
biome_types.csv
biome_patch_rules.csv
biome_boundary_profiles.csv
biome_boundary_pair_rules.csv
boundary_chunk_catalog.csv
```

`content_budget_profiles.csv`, world/route, special/village, microchunk/population/item, generated output은 이 definition set에 포함하지 않는다.

## WRITE ALLOWLIST

### 신규 Runtime production C# — 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionSource.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionBuilder.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
```

신규 C# 8개와 대응 `.cs.meta` 8개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS_RESULT.md
```

TASK 중 status는 수정하지 않고 PASS 이후 finalize만 수행한다.

## Namespace / Assembly Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Assembly           : existing Game.Map.Runtime / Game.Map.Tests.EditMode
```

`UnityEditor` 참조와 새 asmdef/asmref/package는 금지한다.

## Input Gate Contract

`BiomeBoundaryDefinitionSource`는 정확히 한 `CsvFileSchema`와 그 파일의 successful `CsvScalarAndListParseResult`를 묶는 immutable input이다.

`BiomeBoundaryDefinitionBuilder`:

- exact 5개 source가 각각 한 번 존재해야 한다.
- parse result는 모두 `Success == true`, errors 0이어야 한다.
- schema filename, exact column inventory/order, parsed/validated/source identity가 일치해야 한다.
- null, missing, duplicate, unexpected, unsuccessful, mismatched source 오류를 가능한 범위에서 전부 수집한다.
- 오류가 하나라도 있으면 partial definition set을 publish하지 않는다.
- 입력 schema/parser/PK/validation/source models를 수정하지 않는다.

## Definition Type Contract

각 definition은 해당 CSV row와 1:1이며 모든 schema column을 PascalCase compile-time typed property로 노출한다.

```text
STRING / ID / ENUM        -> string
INT                       -> int
FLOAT                     -> float
BOOL                      -> bool
ID_LIST / ENUM_LIST       -> IReadOnlyList<string>
INT_LIST                  -> IReadOnlyList<int>
```

- optional empty string/ID/enum은 exact `string.Empty`다.
- list는 read-only copy이며 원래 order와 duplicate를 보존한다.
- enum은 MAP01_06에서 검증된 exact token string을 보존한다.
- 모든 definition은 exact `CsvParsedRecord SourceRecord`를 가진다.
- `Active == false` row도 삭제·필터링하지 않는다.
- dynamic/object dictionary를 public definition API로 노출하지 않는다.

## Exact Definition Inventory / Column Mapping

### `BiomeDefinitions.cs`

`BiomeTypeDefinition` ← `biome_types.csv`

```text
biome_id, display_name_ko, stage_id, required,
min_patch_count, max_patch_count, min_core_patch_count,
preferred_altitude_min_sector_y, preferred_altitude_max_sector_y,
growth_weight, tile_theme_id, audio_profile_id,
microchunk_pool_prefix, sector_recipe_pool_prefix,
common_resource_pool_id, map_element_pool_id,
required_special_map_ids, active, notes
```

`BiomePatchRuleDefinition` ← `biome_patch_rules.csv`

```text
patch_rule_id, biome_id, patch_role,
min_sector_count, max_sector_count, min_seed_distance,
seed_count_min, seed_count_max, seed_weight,
can_touch_world_edge, buffer_ring_sectors, allow_single_sector,
max_world_share, distance_weight, altitude_weight, noise_weight,
compactness_weight, branchiness_target, active, notes
```

### `BiomeBoundaryDefinitions.cs`

`BiomeBoundaryProfileDefinition` ← `biome_boundary_profiles.csv`

```text
boundary_profile_id, display_name_ko, boundary_type,
allowed_orientations, width_microchunks_min, width_microchunks_max,
warning_microchunks_min, mandatory_route_allowed,
tool_requirement, hard_border, active, notes
```

`BiomeBoundaryPairRuleDefinition` ← `biome_boundary_pair_rules.csv`

```text
boundary_pair_rule_id, biome_a_id, biome_b_id,
allowed_boundary_profile_ids, boundary_profile_weights,
default_boundary_profile_id, transition_resource_pool_id,
transition_element_pool_id, min_shared_edge_count, active, notes
```

`BoundaryChunkDefinition` ← `boundary_chunk_catalog.csv`

```text
boundary_chunk_id, microchunk_id, biome_a_id, biome_b_id,
boundary_profile_id, orientation, route_type,
entry_edge_signature_id, exit_edge_signature_id,
weight, reversible, active, notes
```

## Definition Set Contract

`BiomeBoundaryDefinitionSet`은 다음 5개 `StringComparer.Ordinal` read-only dictionary를 제공한다.

```text
BiomeTypes                 // BiomeId
BiomePatchRules            // PatchRuleId
BoundaryProfiles           // BoundaryProfileId
BoundaryPairRules          // BoundaryPairRuleId
BoundaryChunks             // BoundaryChunkId
```

dictionary value enumeration은 key ordinal ascending이다. CSV input row order를 섞어도 membership과 ordering이 같아야 한다.

read-only stable query:

- biome ID별 patch rules
- biome A/B ID가 exact 방향으로 일치하는 boundary pair rules
- boundary profile ID별 boundary chunks
- exact `(BiomeAId, BiomeBId)` 방향별 boundary chunks

`BiomeAId/BiomeBId`를 canonical sort하거나 양방향 pair로 자동 병합하지 않는다. 원본 방향과 ID를 그대로 보존한다.

## Build Error Contract

`BiomeBoundaryDefinitionBuildErrorCode`는 `BiomeBoundaryDefinitionBuildError.cs`에 최소 다음을 구분한다.

```text
MissingSource
UnexpectedSource
DuplicateSource
UnsuccessfulParse
SchemaMismatch
FieldMappingFailed
```

오류는 filename → record number → column order → error code ordinal로 정렬한다. record 위치가 존재하면 exact file/record/field/line/column/offset을 보존한다. missing source처럼 위치가 없으면 nullable location을 사용하고 가짜 `-1`을 만들지 않는다.

`BiomeBoundaryDefinitionBuildResult`:

- success면 errors 0, non-null definition set이다.
- 오류가 하나라도 있으면 definition set은 null이다.
- 가능한 모든 안전한 오류를 반환하며 partial collection을 노출하지 않는다.

## Scope Boundary / DO NOT

- biome/boundary/special/microchunk/edge FK existence lookup 또는 object resolve 금지
- biome A/B pair canonicalization, reverse pair 자동 생성, self-pair 금지 검사 금지
- allowed profile IDs와 weight list 길이·합계·default membership 검사 금지
- min/max, patch count/size, weight/share/range, required biome domain validation 금지
- boundary orientation/tool/mandatory compatibility 검사 금지
- boundary chunk candidate 선택, transform, A→B/B→A 반전 구현 금지
- active row filtering 금지
- world/route definition 수정·재구축 금지
- special/village/microchunk/population/item/generated definition 금지
- StaticDataRegistry, reverse index, content hash, report/window 구현 금지
- reader/schema/validator/PK/parser/importer 및 기존 C#/test 수정 금지
- CSV/ScriptableObject/asset 수정·생성 금지
- asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency 설치 금지
- Git operation 금지
- MAP01_09 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 이미 있고 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID를 보존한다.
5. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_08, Master 205개, MAP01_07 COMPLETE/PASS를 확인한다.
2. MAP01_07 Result의 definitions 59/59, parser 97/97, PK 32/32, validator 29/29, reader 31/31, schema 23/23, importer 9/9, architecture 10/10, targeted 290/290, full 333/333, compile/warning 0/0을 확인한다.
3. 작업 전 C#/CSV/meta inventory와 preservation hash를 기록한다.
4. allowlisted schema/parser/previous definition APIs와 tests/asmdefs만 읽어 실제 API에 맞춘다.
5. exact 5 row definition과 source/set/error/result/builder를 구현한다.
6. 모든 column을 compile-time typed property로 1:1 매핑한다.
7. 최소 36개 definition unit test를 구현한다.
8. Unity refresh/compile 후 신규 definition tests와 기존 world-route/parser/PK/validator/reader/schema/importer/architecture fixtures를 실행한다.
9. 신규 meta 8개의 GUID와 프로젝트 중복을 확인한다.
10. 기존 CSV/meta와 MAP01_02~07 C#의 비허용 변경이 0인지 확인한다.
11. Result를 작성하고 모든 조건이 충족될 때만 PASS를 기록한다.

## Required Tests

`BiomeBoundaryDefinitionBuilderTests` 최소 36 case:

- exact 5-source success / shuffled source order same output
- missing / duplicate / unexpected / unsuccessful source errors
- schema filename / column inventory / parsed field identity mismatch
- focused full-column mapping for all 5 definition types
- string/ID/enum/int/float/bool/list exact typed preservation
- optional empty and default/UsedDefault preservation
- list order and duplicate preservation
- inactive definitions retained
- exact `CsvParsedRecord SourceRecord` identity preserved
- five ordinal dictionaries lookup and stable enumeration
- patch-rule query by biome ID
- pair-rule query preserves exact A/B direction
- chunk queries by profile and exact directed biome pair
- source/row shuffle does not change membership/order
- errors accumulate and sort deterministically
- any error publishes no partial set
- dictionaries/query results/nested lists are immutable/read-only
- FK IDs remain unresolved strings
- no pair canonicalization/reverse generation/domain validation occurs
- input schema/parser/PK/validation/previous definitions remain unchanged

Targeted regression:

```text
New biome/boundary definitions: >=36 / ALL PASS
World/route definitions: 59/59 PASS
Scalar/list parser: 97/97 PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=326 / ALL PASS
Full project EditMode: >=369 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=326 / ALL PASS
Full EditMode: >=369 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_07 GATE CHECK
CREATED
PREEXISTING_IDENTICAL
SOURCE FILE CONTRACT
DEFINITIONS IMPLEMENTED
DEFINITION SET CONTRACT
TEST
UNITY
ASSET META VALIDATION
CHANGE SCOPE
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

## DONE CONDITIONS

- [ ] Current Task가 MAP01_08이다.
- [ ] Master 205개와 MAP01_07 COMPLETE/PASS를 확인했다.
- [ ] exact 5개 source file만 definition 대상으로 사용한다.
- [ ] 5개 row definition이 모든 schema column을 typed property로 1:1 보존한다.
- [ ] optional empty/default/list/source provenance와 inactive row를 보존한다.
- [ ] five dictionaries와 query view가 immutable/read-only이며 input row order에 독립적이다.
- [ ] A/B pair 방향을 변경·canonicalize하지 않는다.
- [ ] build error를 누적하고 오류 시 partial set을 publish하지 않는다.
- [ ] FK ID를 string으로 보존하고 resolve하지 않는다.
- [ ] biome/boundary domain validation과 candidate 선택을 수행하지 않는다.
- [ ] 신규 Runtime C# 7개와 test 1개만 생성했다.
- [ ] 신규 meta 8개가 유효하고 GUID 중복이 없다.
- [ ] reader/schema/validator/PK/parser/world-route/importer/기존 tests를 수정하지 않았다.
- [ ] CSV 50개/meta 50개를 수정하지 않았다.
- [ ] later definitions/FK/Registry/hash/report를 구현하지 않았다.
- [ ] asmdef/asset/Scene/Prefab/Package/ProjectSettings 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 326개와 full EditMode 최소 369개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_09를 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가:

1. MAP01_08을 `CURRENT -> COMPLETE`로 바꾼다.
2. Last Completed/Last Result를 MAP01_08로 갱신한다.
3. Current Task를 `NONE`으로 만든다.
4. MAP01_09 이후는 모두 `LOCKED`로 유지한다.
5. `MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS`를 자동 생성·실행하지 않는다.

FAIL/BLOCKED이면 MAP01_08 CURRENT를 유지하고 다음 Task로 진행하지 않는다.

## Recommended Commit

```text
feat(map): build immutable biome and boundary definitions
```
