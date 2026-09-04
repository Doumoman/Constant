```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS
  task_file: TASKS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS.md
  requires_current_task: NONE
  requires_completed_task: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
  requires_result:
    path: REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md
    status: PASS
    sha256: 164274139ee6194cc9de8a6d03c5c5c46af48e0fd5b771747a418c4174b83b33
  requires_installed_task:
    path: TASKS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT.md
    sha256: 19f75c5068c7c8ed0ab17bbc1e288ebee07be547fa74bd3f8aa54ec6579c2264
  sets_current_task: MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS
```

# MAP18_03 - Populate Shops Resources and Map Elements

```text
TASK: MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP18_02가 예약한 required trigger와 세 CoreResource slot을 침범하지 않으면서, shop inventory, non-mandatory resource, neutral map element를 **logical population plan**으로 배치한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. shop/resource/map-element pool entry와 biome/tool/interaction filter를 typed catalog로 만든다.
2. MAP18_02 exclusion surface를 적용해 필수/unique 예약 slot을 일반 population 대상에서 제외한다.
3. 남은 slot에 shop inventory plan, optional resource node plan, neutral map element plan을 deterministic하게 작성한다.
4. MAP18_04가 hazard/enemy budget을 쓸 때 침범하면 안 되는 occupied reservation surface와 neighbor/safe-radius evidence를 제공한다.
```

이번 Task의 "populate"는 runtime object 생성이 아니라 순수 데이터 placement plan이다.  
Shop stock은 거래 가능한 실제 inventory가 아니라 logical stock entry이며, resource/map element도 tilemap이나 prefab으로 생성되지 않는다.

금지:

```text
actual shop transaction
actual item grant
actual resource pickup grant
inventory mutation
economy price execution
device execution
hazard/enemy placement
activity/event runtime state instantiation
special region state export
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
UnityEngine.Random or Random.Range
disk save/load file write/read
actual user save slot management
CSV authoring edits
Generated CSV commits
production seed approval
MAP18_04 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
logical population plan이 실제 runtime spawn/shop transaction과 어떻게 다른지
MAP18_02 예약 slot 4개를 어떻게 제외했는지
shop/resource/map-element entry를 어떤 기준으로 선택했는지
biome/resource/tool/interaction/neighbor/safe-radius filter가 무엇을 보장하는지
MAP18_04에 넘기는 occupied/exclusion surface
중복 코드나 하드코딩 후보를 발견했는지
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## Shop Resource Map Element Population Summary
## Runtime Spawn and Economy Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_02 Result exists
MAP18_02 Result STATUS: PASS
MAP18_02 Result SHA-256:
164274139ee6194cc9de8a6d03c5c5c46af48e0fd5b771747a418c4174b83b33

MAP18_02 installed task SHA-256:
19f75c5068c7c8ed0ab17bbc1e288ebee07be547fa74bd3f8aa54ec6579c2264

MAP18_02 placement plan digest:
eda7bf7aedb660223927d6e0b36e63f5dbe041761febf91da6fb855f413f200f

MAP18_02 placement stable id set digest:
c4c1948c17d8e75e821e3eec4402832635e7773693c4b956bc18a53d7ca15a09

MAP18_02 reserved exclusion entries:
4

MAP18_02 selected logical placements:
RequiredProgressTrigger -> MAP16_SLOT_07
MoonCore -> MAP16_SLOT_08
CassiaSap -> MAP16_SLOT_11
StarNuruk -> MAP16_SLOT_05

MAP18_02 remaining unreserved mandatory/unique candidate:
MAP16_SLOT_06

Current Task before apply: NONE
MAP18_02: COMPLETE
MAP18_03: LOCKED before apply
MAP18_04: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP10/MAP13/MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
MicroPatternBiomeProfileCatalog
VillageShellPlan
VillageStateVariantSet
SpecialLandmarkRegionPlan
GeneratedContentSlotIndex
GeneratedContentSlotIndexEntry
GeneratedContentSlotCategory
GeneratedContentPoolKey
GeneratedStableSpawnId
GeneratedMandatoryUniquePlacementPlan
GeneratedMandatoryUniquePlacementEntry
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP10/MAP13/MAP18_01~02 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
MAP18_01 slot source records: 12
MAP18_02 reserved mandatory/unique entries: 4
MAP18_02 exclusion entries: 4
minimum logical population groups: 3
required groups: ShopInventory, OptionalResource, NeutralMapElement
hazard/enemy entries in this task: 0
runtime spawn entries in this task: 0
```

MAP18_02 exclusion surface를 확인할 수 없거나, reserved slot을 제외한 후 세 logical group을 만들 수 있는 후보가 없으면 partial plan을 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedPopulationContentKind` | ShopInventory, OptionalResource, NeutralMapElement content kind를 정의한다. |
| `GeneratedPopulationPoolEntry` | pool namespace/version, biome allowlist, tool requirement, interaction radius, safe radius, neighbor rule을 담는다. |
| `GeneratedPopulationFilterRule` | biome/resource/tool/interaction/neighbor/safe-radius rejection reason을 정의한다. |
| `GeneratedPopulationPlacementEntry` | selected slot, content kind, pool entry, stable spawn ID, reservation key, filter proof를 담는다. |
| `GeneratedPopulationPlacementPlan` | logical shop/resource/map-element entries, occupied/exclusion surface, remaining candidates, digest를 묶는다. |
| `GeneratedPopulationPlacementFailure` | missing pool/candidate, reserved slot reuse, filter mismatch, neighbor collision, stable ID collision을 deterministic하게 보고한다. |
| `GeneratedShopResourceMapElementPopulator` | MAP18_01 slot index와 MAP18_02 exclusion을 사용해 pure-data population plan을 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedPopulationPlacement.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedShopResourceMapElementPopulator.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedShopResourceMapElementPopulationTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Population 규칙

### 5.1 Logical content groups

다음 세 group을 최소 1개 이상 logical plan에 포함한다.

```text
ShopInventory
OptionalResource
NeutralMapElement
```

허용되는 작업:

```text
logical shop stock entry creation
logical optional resource node entry creation
logical neutral map element entry creation
stable spawn ID and reservation key binding
pool key/version publication
filter proof publication
occupied surface publication for MAP18_04
```

금지되는 작업:

```text
actual item object creation
actual shop inventory mutation
actual price calculation/transaction
actual reward or pickup grant
actual map element prefab/tile placement
hazard/enemy placement
```

### 5.2 Exclusion and reservation

MAP18_02의 four mandatory/unique reservations는 반드시 일반 population에서 제외한다.

필수:

```text
reserved exclusion entries consumed: 4/4
RequiredProgressTrigger slot excluded
MoonCore slot excluded
CassiaSap slot excluded
StarNuruk slot excluded
reserved slot reuse count: 0
reservation key collisions: 0
stable spawn ID collisions: 0
MAP18_04 occupied surface includes MAP18_02 + MAP18_03 reservations
```

같은 physical slot double-use는 기본적으로 금지한다. 후속 Task에서 명시적으로 layer 분리 rule을 열기 전까지 같은 slot을 공유하지 않는다.

### 5.3 Selection and filter policy

Selection은 stable order와 deterministic hash ticket으로만 수행한다.

필수:

```text
input order independent
repeat stable
culture stable
no UnityEngine.Random
no Random.Range
no System.Random unless wrapped by existing deterministic worldgen authority
no hidden retry loop
no implicit candidate creation
```

Filter는 다음 evidence를 게시한다.

```text
biome allowlist accepted/rejected
resource/tool requirement accepted/rejected
interaction radius accepted/rejected
safe radius accepted/rejected
neighbor radius accepted/rejected
MAP18_02 exclusion accepted/rejected
```

Filter failure는 후보를 조용히 고치거나 slot을 새로 만들지 않고 deterministic rejection evidence로 남긴다.

### 5.4 Shop inventory boundary

ShopInventory entry는 거래 가능한 실제 inventory가 아니다.

필수:

```text
shop inventory entries are logical plan entries
stock item keys are stable content keys only
price tier keys are symbolic only
purchase state mutations: 0
wallet/currency mutations: 0
item grants: 0
merchant/village NPC spawns: 0
```

Village/merchant source가 사용되면 MAP13_04/05/07의 immutable shell/state/landmark source identity를 보존한다. 없으면 source owner를 임의 문자열로 만들지 말고 validation failure로 처리한다.

### 5.5 Digest

`BakingCanonicalDigest`를 사용해 population plan digest와 occupied surface digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable candidate order
mutation sensitivity
```

Digest material에는 machine path, timestamp, frame count, Unity object instance ID를 넣지 않는다.

### 5.6 Failure policy

다음은 atomic failure다.

```text
missing MAP18_02 preplacement plan
MAP18_02 placement digest mismatch
MAP18_02 stable id set digest mismatch
reserved mandatory/unique slot reused
missing ShopInventory candidate
missing OptionalResource candidate
missing NeutralMapElement candidate
invalid pool key
invalid biome/tool/interaction/safe/neighbor rule
reservation key collision
stable spawn ID collision
attempted runtime spawn or shop transaction
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
hazard/enemy placement
hierarchical combat budget spending
activity/event runtime state instantiation
special region state export
actual shop transaction
actual item grant
actual pickup collection
inventory mutation
currency mutation
device execution
actual runtime spawn
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
Scene mutation
Prefab mutation
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
System.IO file write/read for save data
disk save/load file creation
actual user save slot management
platform save storage
CSV authoring edits
Generated CSV commits
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
production seed approval
MAP18_04 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_03`만 선택한다.

```text
MAP18_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01/MAP18_02 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Compile check와 relevant Console check는 허용한다.

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 회귀를 돌리지 않는다. Result에 다음을 기록하고 멈춘다.

```text
REGRESSION TRIGGER DETECTED: YES
trigger owner:
broken invariant:
why focused proof is insufficient:
requested wider verification:
```

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 8. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
ShopResourceMapElementPopulationCreatesThreeLogicalGroups
PopulationRespectsMandatoryUniqueExclusionsAndReservedSlots
ShopInventoryEntriesAreLogicalAndDoNotMutateEconomyOrInventory
ResourceAndMapElementEntriesApplyBiomeToolInteractionNeighborAndSafeFilters
PopulationSelectionUsesStableOrderAndDeterministicHashWithoutUnityRandom
PopulationPlanPublishesOccupiedSurfaceForMap18_04
MissingCandidateDigestMismatchFilterAndReservationFailuresAreAtomic
PopulationDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder
PopulationDoesNotSpawnObjectsMutateScenesWriteSavesLoadAssetsOrRunRegressions
Map18HandoffKeepsMap18_04Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_03]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
```

If the project already has equivalent focused tests and the exact count differs, explain why in Result. Silent substitution is not allowed.

## 9. Result 필수 증거

Result에는 아래 값을 실제 수치로 기록한다.

```text
MAP18_02 placement plan digest reused:
MAP18_02 placement stable id set digest reused:
MAP18_02 reserved exclusion entries reused:
MAP18_02 required/core slots excluded:

logical population groups published:
ShopInventory entries:
OptionalResource entries:
NeutralMapElement entries:
total logical population entries:
unique content keys:
unique stable spawn IDs:
unique reservation keys:
MAP18_02 reserved slot reuse count:
MAP18_03 reservation collisions:
MAP18_03 stable spawn ID collisions:
MAP18_04 occupied surface entries:
remaining unoccupied candidate count:

pool entries published:
pool namespace/version checks:
biome allowlist accepted/rejected:
resource/tool requirement accepted/rejected:
interaction radius accepted/rejected:
safe radius accepted/rejected:
neighbor radius accepted/rejected:
MAP18_02 exclusion accepted/rejected:

selection uses stable order: YES
deterministic hash/ticket selections:
input order dependency detected: NO
UnityEngine.Random/Random.Range calls: 0/0
System.Random direct usage:
hidden retry loop count: 0
implicit candidate creation count: 0
candidate mutation count: 0

shop inventory logical entries:
actual shop transactions: 0
price executions: 0
wallet/currency mutations: 0
item grants: 0
resource pickup grants: 0
inventory mutations: 0
device executions: 0

population plan digest lower-hex SHA-256: YES
population plan digest:
occupied surface digest lower-hex SHA-256: YES
occupied surface digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing candidate failure probes:
digest mismatch failure probes:
filter mismatch failure probes:
reservation collision failure probes:
stable spawn ID collision failure probes:
attempted runtime spawn/transaction failure probes:
atomic failure partial entries:

runtime content placements performed: 0
hazard placements performed: 0
enemy placements performed: 0
hierarchical combat budget spends: 0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
disk save/load files created: 0/0
actual user save slot writes: 0
platform save storage writes: 0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
production seed approvals: 0
MAP18_04 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_03 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes shop/resource/map-element population summary
Result includes runtime spawn and economy boundary notes
ShopInventory, OptionalResource, NeutralMapElement logical groups are created
MAP18_02 required/core reservations are excluded
MAP18_04 occupied surface is created
biome/tool/interaction/neighbor/safe filters are represented and tested
selection is deterministic and does not use Unity random
stable IDs and reservation keys are unique
no shop transaction, item grant, inventory mutation, device execution, hazard/enemy placement, or runtime spawn
no actual disk save/load file write/read
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_04 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS: COMPLETE
MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_03: populate shops resources and map elements
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
