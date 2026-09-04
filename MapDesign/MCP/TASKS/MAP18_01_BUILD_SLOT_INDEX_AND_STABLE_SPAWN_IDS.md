```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
  task_file: TASKS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS.md
  requires_current_task: NONE
  requires_completed_task: MAP17_08_MAP17_RUNTIME_EXIT_AUDIT
  requires_result:
    path: REPORTS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT_RESULT.md
    status: PASS
    sha256: aca1f360dc9ffe4c5f96479ae7d2d69526cd9e8d6d6fed442c1c2fb58c998fb1
  requires_installed_task:
    path: TASKS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT.md
    sha256: b68f5808d4a7c3cea90a18a69eecc3eda86357dc2ca669890c77c8aaecc22be0
  sets_current_task: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
```

# MAP18_01 - Build Slot Index and Stable Spawn IDs

```text
TASK: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP18의 첫 작업으로, MAP16/17에서 생성된 marker/slot/provenance와 sector/slice 좌표를 사용해 **content slot index**와 **population/content stable spawn ID** 계약을 만든다.

이번 Task의 책임은 다음 네 가지다.

```text
1. sector/slice/source/category/pool 기준으로 content slot을 안정 정렬된 index로 만든다.
2. world seed + generator/data version + sector + slice + source slot + category + pool key로 stable spawn ID를 만든다.
3. 같은 seed/slot/category/pool은 같은 ID를, seed/sector/slice/source/category/pool 변화는 다른 ID를 만든다는 것을 증명한다.
4. MAP18_02 이후의 mandatory/unique/resource/shop/hazard/enemy placement가 사용할 slot query와 collision-free reservation key를 제공한다.
```

이번 Task는 **실제 콘텐츠를 배치하거나 스폰하는 단계가 아니다.**  
Enemy, hazard, shop item, resource node, pickup, NPC, boss, village facility, activity/event runtime state를 선택·생성·instantiate하지 않는다. 어떤 pool에서 무엇을 뽑을지도 아직 결정하지 않는다.

금지:

```text
actual enemy/resource/shop/hazard/item placement
mandatory or unique content selection
weighted pool roll
runtime spawn object creation
GameObject / Prefab instantiate, enable, disable, destroy
Scene / Prefab / Tilemap mutation
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Addressables / Resources / AssetDatabase load
disk save/load file write/read
actual user save slot management
CSV authoring edits
Generated CSV commits
production seed approval
MAP18_02 unlock or execution
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
slot index가 실제 content placement와 어떻게 다른지
stable spawn ID가 어떤 입력으로 만들어지는지
같은 seed/slot에서 재현성과 변경 민감성을 어떻게 보장했는지
MAP18_02에 넘기는 산출물
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
## Slot Index and Stable ID Summary
## Non-Spawn Boundary and Risk Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_08 Result exists
MAP17_08 Result STATUS: PASS
MAP17_08 Result SHA-256:
aca1f360dc9ffe4c5f96479ae7d2d69526cd9e8d6d6fed442c1c2fb58c998fb1

MAP17_08 installed task SHA-256:
b68f5808d4a7c3cea90a18a69eecc3eda86357dc2ca669890c77c8aaecc22be0

MAP17_08 audit report digest:
8b4849bf11ac6807a9e8a9d699a166eaa61e5c600454e410bae1ad47480545a0

MAP17 phase exit verdict:
PASS

MAP18_01 handoff approved by audit:
YES

MAP17_08 performance spike classification:
WARN, does not block MAP18_01

MAP17_08 duplicate/hardcoding risk:
carried forward, does not block MAP18_01

Current Task before apply: NONE
MAP17_08: COMPLETE
MAP18_01: LOCKED before apply
MAP18_02: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP16/MAP17 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainSlice
GeneratedTerrainSlot
GeneratedTerrainSlotProjection
GeneratedTerrainProvenance
GeneratedCellPlacementPlan
GeneratedTilemapLayerBakePlan
GeneratedSectorRuntimeHandle
GeneratedSectorStreamingWindow
GeneratedWorldSaveManifest
GeneratedMap17ExitAuditReport
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP16/MAP17 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
world sectors: 13x13 = 169
sector size: 48x32 cells
sector local index range: 0..1535
slices per sector: 16
slice size: 12x8 = 96 cells
logical layer count: 7
MAP17 phase exit verdict: PASS
MAP17 audit warnings carried forward: 2
```

Slot 후보는 MAP16에서 projection된 marker/slot/provenance를 기준으로 만든다. MAP18_01에서 slot 후보가 전혀 없다면 자동으로 임의 slot을 만들지 말고 `BLOCKED`로 보고한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedContentSlotCategory` | resource/shop/hazard/enemy/pickup/device/activity/event/special 등 population category key를 정의한다. |
| `GeneratedContentPoolKey` | 실제 pool roll 없이 pool namespace와 version을 canonical key로 표현한다. |
| `GeneratedContentSlotAddress` | world seed, sector coord, slice index, source slot/provenance, category, pool key를 담는 stable address다. |
| `GeneratedContentSlotIndexEntry` | slot address, local cell/index, source owner, reservation key, deterministic order key를 담는다. |
| `GeneratedContentSlotIndex` | category/pool/sector/slice/source 기준 query와 duplicate/collision 검증을 제공한다. |
| `GeneratedStableSpawnId` | spawn ID namespace와 lower-hex SHA-256 value object를 정의한다. |
| `GeneratedStableSpawnIdFactory` | address canonical line으로 deterministic stable spawn ID를 만든다. |
| `GeneratedContentSlotIndexBuilder` | MAP16/17 slot/provenance input을 slot index로 변환하고 validation result를 만든다. |
| `GeneratedContentSlotIndexResult` | success/failure wrapper와 deterministic failure evidence를 담는다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedContentSlotIndex.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedStableSpawnId.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedContentSlotIndexBuilder.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedContentSlotIndexTests.cs(.meta)
```

필요하다면 `Population` Runtime/Test 폴더를 생성할 수 있다. Assembly definition을 새로 추가하거나 기존 asmdef를 수정해야 할 것 같으면 먼저 현재 구조를 확인하고, 기존 MAP09_00 V2 폴더/asmdef 방식을 따르되 Result에 이유와 변경 파일을 명시한다.

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Slot index 규칙

### 5.1 Slot source and address

Slot address는 다음 요소를 포함한다.

```text
world seed or reference seed
generator version
data version
sector coordinate
slice index 0..15
sector local index 0..1535
slice local index 0..95 when available
source owner kind
source owner id or provenance token
source slot id
content category
pool key
pool version
```

필수:

```text
sector coordinate bounds validation
slice index bounds validation
sector local index bounds validation
slice local index bounds validation when available
category validation
pool key validation
source slot/provenance validation
stable deterministic order
duplicate address rejection
reservation key collision rejection
```

Slot source가 activity/special/terrain/event marker에서 왔다면 source owner를 보존한다. Marker를 단순 string display name으로만 식별하지 않는다.

### 5.2 Stable spawn ID

Stable spawn ID는 다음 조건을 만족해야 한다.

```text
lower-hex SHA-256
LF-normalized canonical line
UTF-8 no BOM if bytes are requested
namespace includes POPULATION_STABLE_SPAWN_V1
same seed/address/category/pool -> same id
different seed -> different id
different sector -> different id
different slice -> different id
different source slot -> different id
different category -> different id
different pool key/version -> different id
repeat/reverse/culture stable
```

금지:

```text
Guid.NewGuid
Random.Range or UnityEngine.Random
DateTime.Now / ticks as identity
frame count
Unity instance id
GameObject name
file path
array iteration order without explicit sort key
```

MAP17_05의 `SECTOR_MODIFICATION` stable ID namespace와 섞이지 않아야 한다.

### 5.3 Query and reservation

Slot index는 실제 배치를 하지 않고 query만 제공한다.

필수 query:

```text
all slots in stable order
slots by sector
slots by sector + slice
slots by category
slots by pool key
slots by source owner
slots available for mandatory/unique preplacement
collision-free reservation key lookup
```

Reservation key는 MAP18_02 이후가 같은 slot을 중복 점유하지 않도록 만드는 key다. 이번 Task는 key를 만들고 중복을 검증할 뿐, reserve/commit 상태를 변경하지 않는다.

### 5.4 Failure policy

다음은 atomic failure다.

```text
missing upstream slot/provenance source
out-of-bounds sector coordinate
out-of-bounds slice index
out-of-bounds sector local index
invalid category
invalid pool key
duplicate content slot address
duplicate stable spawn id with different address
reservation key collision
unstable order or digest mismatch
attempted actual placement/spawn work
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
mandatory content placement
unique content placement
shop inventory generation
resource node selection
hazard/enemy placement
activity/event runtime state instantiation
special region state export
weighted random roll
budget spending
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
CSV authoring edits
Generated CSV commits
System.IO file write/read for save data
disk save/load file creation
actual user save slot management
platform save storage
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
Scene mutation
Prefab mutation
GameObject instantiate / enable / disable / destroy
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
production seed approval
MAP18_02 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_01`만 선택한다.

```text
MAP18_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17 selections: 0
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
ContentSlotIndexBuildsStableSectorSliceSourceCategoryAndPoolEntries
StableSpawnIdsAreDeterministicAndSeparatedFromModificationIds
StableSpawnIdsChangeWhenSeedSectorSliceSourceCategoryOrPoolChanges
SlotIndexQueriesBySectorSliceCategoryPoolAndSourceInStableOrder
ReservationKeysRejectDuplicateAddressAndCollisionAtomically
SlotIndexRejectsOutOfBoundsSliceCellSectorAndInvalidCategory
SlotIndexDigestIsStableAcrossRepeatReverseCultureAndInputOrder
SlotIndexDoesNotRollPoolsPlaceContentSpawnObjectsOrMutateScenes
SlotIndexReportsMap17WarningsAsNonBlockingHandoffRisks
Map18HandoffKeepsMap18_02Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_01]
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
MAP17_08 audit report digest reused:
MAP17 phase exit verdict reused:
MAP18_01 handoff approved by audit reused:
MAP17 warnings carried forward:

slot source records observed:
slot index entries created:
unique slot addresses:
duplicate slot addresses rejected:
unique reservation keys:
reservation key collision probes:
categories published:
pool keys published:
source owner kinds published:
sector query probes:
sector+slice query probes:
category query probes:
pool query probes:
source owner query probes:
mandatory/unique candidate query probes:

stable spawn id namespace:
stable spawn ids lower-hex SHA-256: YES
stable spawn ids created:
stable spawn id duplicate collisions:
same input stable id equality: YES
seed/sector/slice/source/category/pool mutation distinction probes:
modification id namespace collision probes:
Guid.NewGuid/random/time/frame/object/file-path identity usage: 0/0/0/0/0/0

slot index digest lower-hex SHA-256: YES
slot index digest:
stable id set digest lower-hex SHA-256: YES
stable id set digest:
repeat/reverse/culture/input-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

actual content placements performed: 0
weighted pool rolls performed: 0
budget spends performed: 0
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
MAP18_02 started: NO
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
MapDesign/MCP/TASKS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_01 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes slot index and stable ID summary
Result includes non-spawn boundary and risk notes
content slot index contract created
stable spawn ID contract created
stable spawn IDs are deterministic and namespace-separated from MAP17 modification IDs
slot queries by sector/slice/category/pool/source are deterministic
duplicate address and reservation collisions fail atomically
MAP17_08 WARN risks are carried forward but do not trigger regression
no actual content placement, pool roll, budget spending, or runtime spawn
no actual disk save/load file write/read
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_02 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS: COMPLETE
MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_01: build slot index and stable spawn ids
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
