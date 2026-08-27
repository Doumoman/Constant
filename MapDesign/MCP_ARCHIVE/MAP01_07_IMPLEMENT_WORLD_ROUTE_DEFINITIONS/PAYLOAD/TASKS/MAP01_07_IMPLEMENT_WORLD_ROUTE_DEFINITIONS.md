# MAP01_07 — Implement World Route Definitions

```yaml
status_control:
  task_key: MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS
  result_file: REPORTS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE WORLD/ROUTE DEFINITIONS + EDITMODE TESTS
```

## Objective

MAP01_06의 successful typed parse 결과 중 world/generation/RNG, base route mask, socket/edge signature, sector recipe 계열 exact 13개 CSV를 compile-time property를 가진 immutable definition objects와 deterministic `WorldRouteDefinitionSet`으로 변환한다.

모든 scalar/list typed payload, optional empty, default 적용 결과, inactive row, source record 위치를 손실 없이 보존한다. 참조 열은 아직 string ID로만 보존하며 FK existence/resolution, domain invariant, Registry publish는 수행하지 않는다.

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
12. `REPORTS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02 Runtime schema production C# 8개
- MAP01_03 Runtime reader production C# 7개
- MAP01_04 Runtime validation production C# 6개
- MAP01_05 Runtime PK production C# 6개
- MAP01_06 Runtime parser production C# 7개
- MAP01_02~06의 direct focused test 5개
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

## Exact Source File Set — 13

```text
world_profiles.csv
generation_profiles.csv
generation_passes.csv
rng_streams.csv
sector_route_masks.csv
socket_band_definitions.csv
edge_signatures.csv
edge_signature_compatibility.csv
sector_recipe_catalog.csv
sector_recipe_cells.csv
sector_recipe_paths.csv
sector_external_sockets.csv
sector_recipe_pool_entries.csv
```

위 13개 외 파일을 definition set에 넣지 않는다. `event_activation_routes.csv`, biome/boundary, special/village, microchunk/population/item, generated output은 later Task 범위다.

## WRITE ALLOWLIST

### 신규 Runtime production C# — 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/RouteDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SectorRecipeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSource.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuilder.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
```

신규 C# 9개와 대응 `.cs.meta` 9개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS_RESULT.md
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

`WorldRouteDefinitionSource`는 정확히 한 `CsvFileSchema`와 그 파일의 successful `CsvScalarAndListParseResult`를 묶는 immutable input이다.

`WorldRouteDefinitionBuilder`:

- exact 13개 source가 각각 한 번 존재해야 한다.
- 모든 parse result는 `Success == true`, errors 0이어야 한다.
- schema filename, column inventory/order, parsed field schema/source identity가 일치해야 한다.
- null source, duplicate/missing/unexpected filename, unsuccessful/mismatched parse result를 누적해 deterministic failure로 반환한다.
- 오류가 하나라도 있으면 definition set을 publish하지 않는다.
- 입력 schema/parsed/source models를 수정하지 않는다.

## Definition Type Contract

각 definition은 해당 CSV row와 1:1이며 모든 schema column을 PascalCase compile-time property로 노출한다. `Dictionary<string, object>`, reflection-backed public access, dynamic payload를 definition API로 노출하지 않는다.

typed property mapping:

```text
STRING / ID / ENUM        -> string
INT                       -> int
ULONG                     -> ulong
FLOAT                     -> float
BOOL                      -> bool
HEX                       -> CsvHexValue
DATETIME                  -> DateTimeOffset
ID_LIST / ENUM_LIST       -> IReadOnlyList<string>
INT_LIST                  -> IReadOnlyList<int>
```

- optional empty string/ID/enum은 exact `string.Empty`로 보존한다.
- empty list는 read-only empty collection이다.
- list item order와 duplicate를 보존한다.
- enum token은 MAP01_06에서 검증된 exact string을 보존하며 이 TASK에서 새 enum fallback을 만들지 않는다.
- 모든 definition은 원본 `CsvParsedRecord SourceRecord`를 보존한다.
- `Active == false` row도 삭제·필터링하지 않고 definition으로 만든다.

## Exact Definition Inventory / Column Mapping

### `WorldGenerationDefinitions.cs`

`WorldProfileDefinition` ← `world_profiles.csv`

```text
world_profile_id, display_name_ko, width_tiles, height_tiles,
sector_width_tiles, sector_height_tiles, sector_cols, sector_rows,
micro_width_tiles, micro_height_tiles, micro_cols_per_sector,
micro_rows_per_sector, min_completion_distance_tiles,
max_shortest_completion_distance_tiles, normal_completion_min_tiles,
normal_completion_max_tiles, optional_completion_max_tiles,
max_revisit_ratio, required_village_count, active, notes
```

`GenerationProfileDefinition` ← `generation_profiles.csv`

```text
generation_profile_id, world_profile_id,
mandatory_sector_min, mandatory_sector_max,
type0_sector_min, type0_sector_max,
reserved_sector_min, reserved_sector_max,
inactive_sector_min, inactive_sector_max,
start_edge_ring_min, start_edge_ring_max,
mandatory_loop_min, mandatory_loop_max,
optional_region_depth_min, optional_region_depth_max,
optional_region_count_min, optional_region_count_max,
site_reservation_retry_max, biome_retry_max, route_retry_max,
sector_solve_retry_max, active, notes
```

`GenerationPassDefinition` ← `generation_passes.csv`

```text
generation_profile_id, pass_order, pass_id, class_name, rng_stream_id,
input_artifacts, output_artifacts, failure_policy,
max_retry_count, enabled, notes
```

`RngStreamDefinition` ← `rng_streams.csv`

```text
rng_stream_id, salt_hex, reset_scope, description_ko, active
```

### `RouteDefinitions.cs`

`SectorRouteMaskDefinition` ← `sector_route_masks.csv`

```text
route_mask_id, route_type, open_l, open_r, open_u, open_d,
mandatory_allowed, description_ko, active
```

`SocketBandDefinition` ← `socket_band_definitions.csv`

```text
band_id, axis, min_local_coord, max_local_coord,
recommended_center, minimum_clearance_tiles, description_ko
```

`EdgeSignatureDefinition` ← `edge_signatures.csv`

```text
edge_signature_id, axis, band_id, traversal_kind,
ground_entry_height, clearance_width, clearance_height,
tool_requirement, mandatory_allowed, tags, notes
```

`EdgeSignatureCompatibilityDefinition` ← `edge_signature_compatibility.csv`

```text
signature_a, signature_b, compatible, adapter_microchunk_pool_id, notes
```

### `SectorRecipeDefinitions.cs`

`SectorRecipeDefinition` ← `sector_recipe_catalog.csv`

```text
sector_recipe_id, display_name_ko, route_type, route_mask_id,
primary_biome_id, secondary_biome_id, boundary_profile_id,
recipe_kind, microchunk_budget_profile_id, selection_weight,
supports_special_entry, supports_village_entry, active, notes
```

`SectorRecipeCellDefinition` ← `sector_recipe_cells.csv`

```text
sector_recipe_id, chunk_x, chunk_y, cell_role,
fixed_microchunk_id, microchunk_pool_id, required_usage_class,
required_route_roles, required_biome_ids,
required_signature_l, required_signature_r,
required_signature_u, required_signature_d,
transform_policy, notes
```

`SectorRecipePathDefinition` ← `sector_recipe_paths.csv`

```text
sector_recipe_id, path_id, path_order, chunk_x, chunk_y,
enter_side, exit_side, mandatory, traversal_kind,
max_jump_tiles, notes
```

`SectorExternalSocketDefinition` ← `sector_external_sockets.csv`

```text
sector_recipe_id, socket_id, side, edge_chunk_index, band_id,
traversal_kind, mandatory_allowed, edge_signature_id, notes
```

`SectorRecipePoolEntryDefinition` ← `sector_recipe_pool_entries.csv`

```text
sector_recipe_pool_id, entry_order, sector_recipe_id, weight,
min_repeat_distance_sectors, required_patch_role, active
```

## Definition Set Contract

`WorldRouteDefinitionSet`은 deep immutable/read-only collection을 제공한다.

Single-key dictionaries, `StringComparer.Ordinal`:

```text
WorldProfiles
GenerationProfiles
RngStreams
RouteMasks
SocketBands
EdgeSignatures
SectorRecipes
```

Composite/ordered collections:

```text
GenerationPasses
EdgeSignatureCompatibilities
SectorRecipeCells
SectorRecipePaths
SectorExternalSockets
SectorRecipePoolEntries
```

deterministic enumeration:

- single-key dictionary values: key ordinal ascending
- generation pass: generation profile ID ordinal → pass order → pass ID ordinal
- compatibility: signature A ordinal → signature B ordinal
- recipe cell: recipe ID ordinal → chunk X → chunk Y
- recipe path: recipe ID ordinal → path ID ordinal → path order
- external socket: recipe ID ordinal → socket ID ordinal
- pool entry: pool ID ordinal → entry order → recipe ID ordinal

CSV input row order는 output ordering과 identity에 영향을 주지 않는다. 필요한 parent-ID별 query는 read-only view만 반환한다.

## Build Error Contract

`WorldRouteDefinitionBuildErrorCode`는 `WorldRouteDefinitionBuildError.cs`에 최소 다음을 구분한다.

```text
MissingSource
UnexpectedSource
DuplicateSource
UnsuccessfulParse
SchemaMismatch
FieldMappingFailed
```

오류는 filename → record number → column order → error code ordinal로 정렬한다. source record가 존재하는 오류는 file/record/field/physical line/column/offset을 보존한다. missing-file처럼 위치가 존재하지 않는 오류는 nullable location을 사용하며 가짜 `-1` 위치를 만들지 않는다.

`WorldRouteDefinitionBuildResult`:

- success면 errors 0, non-null definition set이다.
- 오류가 하나라도 있으면 definition set은 null이고 가능한 모든 안전한 오류를 반환한다.
- partial dictionary/list를 외부에 노출하지 않는다.

## Scope Boundary / DO NOT

- FK ID를 definition object reference로 resolve 금지
- missing FK/target lookup 금지
- min/max, world constants, retry, weight, route Type0/1/2/3 mask domain validation 금지
- sector recipe 4×4/16 cells, coordinate range, fixed-vs-pool XOR 검사 금지
- path connectivity, socket direction/signature compatibility 계산 금지
- active row filtering 금지
- biome/boundary/special/village/microchunk/population/item/generated definition 금지
- StaticDataRegistry, reverse index, content hash, report/window 구현 금지
- reader/schema/validator/PK/parser/importer 및 기존 C#/test 수정 금지
- CSV/ScriptableObject/asset 수정·생성 금지
- asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency 설치 금지
- Git operation 금지
- MAP01_08 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 이미 있고 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID를 보존한다.
5. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_07, Master 205개, MAP01_06 COMPLETE/PASS를 확인한다.
2. MAP01_06 Result의 parser 97/97, PK 32/32, validator 29/29, reader 31/31, schema 23/23, importer 9/9, architecture 10/10, targeted 231/231, full 274/274, compile/warning 0/0을 확인한다.
3. 작업 전 C#/CSV/meta inventory와 preservation hash를 기록한다.
4. allowlisted schema/parsed APIs와 tests/asmdefs만 읽어 실제 API에 맞춘다.
5. exact 13 row definition types와 source/set/error/result/builder를 구현한다.
6. 모든 column을 compile-time typed property로 1:1 매핑한다.
7. 최소 44개 definition unit test를 구현한다.
8. Unity refresh/compile 후 신규 definition tests와 기존 parser/PK/validator/reader/schema/importer/architecture fixtures를 실행한다.
9. 신규 meta 9개의 GUID와 프로젝트 중복을 확인한다.
10. 기존 CSV/meta와 MAP01_02~06 C#의 비허용 변경이 0인지 확인한다.
11. Result를 작성하고 모든 조건이 충족될 때만 PASS를 기록한다.

## Required Tests

`WorldRouteDefinitionBuilderTests` 최소 44 case:

- exact 13-source success / shuffled source order same output
- missing / duplicate / unexpected / unsuccessful source errors
- schema filename / column inventory / parsed field identity mismatch
- one focused full-column mapping test for each of 13 definition types
- STRING/ID/ENUM exact token preservation
- INT/FLOAT/BOOL/HEX typed payload preservation
- optional empty string/ID/enum preservation
- empty/non-empty list and duplicate/order preservation
- default-applied field value and `UsedDefault` source preservation
- inactive definitions retained
- every definition preserves `CsvParsedRecord SourceRecord`
- seven single-key dictionaries use ordinal lookup and stable enumeration
- six composite collections use exact deterministic sort contract
- parent-ID queries return read-only stable views
- input row/source shuffling does not alter definition membership/order
- build errors accumulate and sort deterministically
- any error publishes no/partial definition set
- all dictionaries/lists/nested list payloads are immutable/read-only
- FK IDs remain strings and unresolved
- builder does not perform world/route/recipe domain validation
- input schema/parser/PK/validation/source models remain unchanged

Targeted regression:

```text
New world/route definitions: >=44 / ALL PASS
Scalar/list parser: 97/97 PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=275 / ALL PASS
Full project EditMode: >=318 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=275 / ALL PASS
Full EditMode: >=318 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_06 GATE CHECK
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

- [ ] Current Task가 MAP01_07이다.
- [ ] Master 205개와 MAP01_06 COMPLETE/PASS를 확인했다.
- [ ] exact 13개 source file만 definition 대상으로 사용한다.
- [ ] 13개 row definition이 모든 schema column을 typed property로 1:1 보존한다.
- [ ] optional empty/default/list/enum/hex와 source record를 손실 없이 보존한다.
- [ ] inactive row를 삭제·필터링하지 않는다.
- [ ] definition set이 immutable/read-only이며 CSV row order에 독립적이다.
- [ ] single/composite collection ordering이 exact deterministic contract와 일치한다.
- [ ] build error를 누적하고 오류 시 partial definition set을 publish하지 않는다.
- [ ] FK ID를 string으로 보존하고 resolve하지 않는다.
- [ ] world/route/recipe domain validation을 수행하지 않는다.
- [ ] 신규 Runtime C# 8개와 test 1개만 생성했다.
- [ ] 신규 meta 9개가 유효하고 GUID 중복이 없다.
- [ ] reader/schema/validator/PK/parser/importer/기존 tests를 수정하지 않았다.
- [ ] CSV 50개/meta 50개를 수정하지 않았다.
- [ ] later definitions/FK/Registry/hash/report를 구현하지 않았다.
- [ ] asmdef/asset/Scene/Prefab/Package/ProjectSettings 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 275개와 full EditMode 최소 318개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_08을 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가:

1. MAP01_07을 `CURRENT -> COMPLETE`로 바꾼다.
2. Last Completed/Last Result를 MAP01_07로 갱신한다.
3. Current Task를 `NONE`으로 만든다.
4. MAP01_08 이후는 모두 `LOCKED`로 유지한다.
5. `MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS`를 자동 생성·실행하지 않는다.

FAIL/BLOCKED이면 MAP01_07 CURRENT를 유지하고 다음 Task로 진행하지 않는다.

## Recommended Commit

```text
feat(map): build immutable world and route definitions
```
