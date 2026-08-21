# MAP02_01 — Implement Generated World Data

```yaml
status_control:
  task_key: MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA
  result_file: REPORTS/MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA_RESULT.md
```

## Objective

MAP02의 첫 Runtime 계약으로 seed 1개에 대한 exact 169개 `SectorCell`을 보관하는 immutable `GeneratedWorldData`를 구현한다. generated role·biome·patch·route·site·recipe 상태와 미해결 sentinel을 고정하고, Map Package v1.0의 exact `generated_world_sectors.csv` 13열을 deterministic UTF-8 BOM/CRLF bytes로 직렬화한다.

## Mandatory Read / Scope

`00_MCP_ENTRYPOINT.md` → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_17 PASS Result 순서로 읽는다. 그 다음 Map Package v1.0에서 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md
03_CSV_SCHEMA/ENUM_REFERENCE.md                  # SectorRole 행만
03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv             # generated_world_sectors.csv 13행만
05_GENERATED_OUTPUT_SCHEMA/README.md
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv  # header template 1행만
```

현재 `WorldGenConstants`, `SectorCoord`, `WorldCoordinateUtility`, Runtime/EditMode asmdef·namespace, MAP00 coordinate tests, Generation 폴더 inventory를 읽어 existing public API를 재사용한다. MAP02_02 이후 Task body, Legacy/Stage/P6/P11 generator, Authoring data rows, 비승인 production, Scene/Prefab YAML은 읽거나 사용하지 마.

## WRITE ALLOWLIST

Runtime C# 4:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
```

EditMode test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
```

신규 C# 5 + matching `.cs.meta` 5 + Result 1만 허용한다. existing C#/tests/meta/asmdef, Authoring/Generated CSV, report, Scene/Prefab/Package/ProjectSettings 수정 금지. Runtime namespace는 `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용하고 `UnityEditor` reference와 신규 asmdef/asmref를 만들지 마.

## Generated Sector Role Contract

`GeneratedSectorRole` public enum은 아래 5개 상태만 가진다.

```text
Unassigned
Mandatory
Type0
ReservedSite
InactiveBuffer
```

CSV token mapping은 ordinal exact로 고정한다.

```text
Unassigned     -> UNASSIGNED
Mandatory      -> MANDATORY
Type0          -> TYPE0
ReservedSite   -> RESERVED_SITE
InactiveBuffer -> INACTIVE_BUFFER
```

숫자 enum 캐스팅, `ToString().ToUpper*()`, locale/case-fold로 token을 만들지 말고 exact switch/table로 직렬화한다. 정의되지 않은 enum 값은 거부한다.

## SectorCell Contract

`SectorCell`은 sealed immutable Runtime object로 아래 정보를 보관한다.

```text
int Index
SectorCoord Coordinate
GeneratedSectorRole Role
string PrimaryBiomeId
string SecondaryBiomeId
string PatchId
string RouteMaskId
string SpecialSiteInstanceId
string BoundaryProfileId
string SectorRecipeId
string ReservationId
int ShortestDistanceFromStart
bool MandatoryGraphNode
```

- `Index`는 `0..WorldGenConstants.SectorCount-1`; `Coordinate`는 existing `SectorCoord`의 in-range 값이다.
- 모든 string은 non-null이며 임의 trim/case-fold/Unicode normalization을 하지 않는다. 미해결은 null이 아닌 exact empty string이다.
- `CreateUnassigned(index, coordinate)`는 Role `Unassigned`, 모든 ID empty, `ShortestDistanceFromStart=-1`, `MandatoryGraphNode=false`인 중립 cell 1개를 만든다.
- roadmap의 `SpecialSiteId`는 output schema의 exact `SpecialSiteInstanceId`로 보관한다. `ReservationId`는 후속 reservation pass용 internal world state이며 `generated_world_sectors.csv` v1에는 열을 추가하지 않는다.
- public setter, mutable field, caller-owned collection reference, Unity object/scene reference를 노출하지 않는다.

## GeneratedWorldData Contract

- `Seed` 타입은 exact `ulong`.
- 생성자는 null 없는 exact `WorldGenConstants.SectorCount` = 169 cell을 요구한다.
- index set은 exact `0..168`, coordinate set은 exact 13×13 in-range `SectorCoord` 전체이며 duplicate/missing/extra/null을 모두 거부한다.
- caller input order와 무관하게 `Index` 오름차순으로 내부 snapshot을 고정하고 `Cells`/index lookup/coordinate lookup을 read-only로 제공한다.
- 생성 후 caller가 input list/array를 변경해도 snapshot이 변하지 않는다. public add/remove/replace/set API를 만들지 않는다.
- `Index == y*13+x`와 L/R/U/D 이웃 생성 알고리즘은 `MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS`의 책임이다. 이 Task의 container는 index/coordinate 전체성만 검증하고 둘의 산술 관계를 자동 생성·수정·정렬해 숨기지 않는다.

## `generated_world_sectors.csv` v1 Contract

`GeneratedWorldDataCsvSerializer` constant filename은 exact `generated_world_sectors.csv`이고, filesystem에 쓰지 않고 deterministic copied `byte[]`를 반환한다.

Exact 13-column header/order:

```text
seed,sector_x,sector_y,sector_role,primary_biome_id,secondary_biome_id,patch_id,route_mask_id,special_site_instance_id,boundary_profile_id,sector_recipe_id,shortest_distance_from_start,mandatory_graph_node
```

Byte/row rule:

- encoding exact UTF-8 BOM `EF BB BF`; BOM은 시작에 단 1회만 존재한다.
- record separator exact CRLF, header 1 + data 169 rows, final CRLF exact 1. RFC4180 quoted field 내부를 제외한 unquoted/bare LF/CR 금지.
- header-only template prefix는 Map Package v1.0 template의 exact 210 bytes / SHA-256 `0721cfa4acb6bfb2d85e04ee295960a63844e4c5c72648f9e9cdb5d260aebf59`와 일치해야 한다.
- data rows는 `SectorCell.Index` 0..168 오름차순. caller collection 순서가 바뀌어도 bytes는 같다.
- `seed` ulong, `sector_x/y` int, `shortest_distance_from_start` signed int는 invariant decimal; leading `+`, locale separator 금지.
- `sector_role`은 위 exact 5 token; `mandatory_graph_node`는 exact `0`/`1`.
- string field는 값을 보존하고 RFC4180 comma/quote/CR/LF escaping과 doubled quote를 적용한다. null 대체·trim·case normalization 금지.
- unresolved initial cell의 primary biome/patch/route를 포함한 ID 필드는 empty로 직렬화한다. dictionary의 `required=1`은 completed generated output gate이며 중간 empty topology snapshot을 위조해 채우라는 의미가 아니다.
- `Index`/`ReservationId`는 스키마 열이 아니므로 출력하지 않는다. undocumented 열/JSON/timestamp/path/Unity GUID를 추가하지 않는다.

## DO NOT

- deterministic RNG stream/salt, `WorldGenerationRoot`, generation pass/record/retry 구현 금지
- 169-cell grid factory, `y*13+x` mapping, L/R/U/D neighbor, out-of-world `-1` 생성 금지
- seed manifest/replay recorder, generated file/directory I/O, JSON/hash bundle 금지
- EditorWindow/Scene·Game overlay/Gizmo/menu/visual debug 금지
- biome/site/route/recipe 배정 알고리즘, placeholder ID 주입, static Registry mutation 금지
- ScriptableObject/singleton/MonoBehaviour/DataManager/AssetDatabase integration 금지
- existing MAP00/01 production/tests, Authoring CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings/Git 변경 금지
- test skip/ignore/assertion 완화, 비결정적 직렬화, 예외 swallow 금지

## Tests / Verification

Focused minimum 32 cases:

- exact 5 enum/token mapping, undefined enum rejection
- unassigned factory exact empty/sentinel/defaults, non-null/no-normalization contract
- 169 count, exact index set, exact coordinate set, null/missing/extra/duplicate rejection
- input order independence, immutable copied collection, stable index/coordinate lookup
- exact header/order/13 fields, 210-byte template prefix and known SHA-256
- single BOM, CRLF only, 170 records, final CRLF
- index-order rows, seed `0`/`ulong.MaxValue`, invariant signed integers under non-English culture
- all role tokens, bool 0/1, unresolved empty fields, optional/assigned ID values
- RFC4180 comma/quote/CRLF escaping and doubled quote
- repeated serialization and shuffled input produce byte-identical output
- no index/reservation/extra columns, no timestamp/path/GUID, returned byte array isolation
- no RNG/grid pass/neighbor/file I/O/UnityEditor dependency

```text
New GeneratedWorldData: >=32 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 exit audit/fixture/Registry regression: PASS
Previous targeted baseline: 867/867 PASS
Targeted total: >=899 PASS
Full project EditMode: >=919 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, existing production/test/asmdef modifications `0`, new meta `5` valid/GUID duplicate `0`. Unity evidence가 없거나 한 조건이라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_17 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, GENERATED SECTOR ROLE, SECTOR CELL, GENERATED WORLD DATA, CSV V1 BYTES, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_01 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): define generated world data serialization`
