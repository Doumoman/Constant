```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE
  task_file: TASKS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE.md
  requires_current_task: NONE
  requires_completed_task: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
  requires_result:
    path: REPORTS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION_RESULT.md
    status: PASS
    sha256: 146b66793e74fbfcd008aba3548c5ec9f9300ad31b6ae34ad090e8065af81ef3
  requires_installed_task:
    path: TASKS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION.md
    sha256: 4ceadbe998821f206ea33ba90b52fc5c7fd719b618d4282da619f5fdbdfc98c0
  sets_current_task: MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE
```

# MAP17_05 - Implement Sector Modification Storage

```text
TASK: MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_04의 streaming window/handle 결과 위에, 파괴·획득·장치 상태 같은 sector-local 변경을 기록하는 **in-memory sector modification storage contract**를 구현한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. sector-local index 0..1535와 layer/source/slot provenance로 변경 대상을 주소화한다.
2. tile destroy/replace, pickup collected, device state, slot consumed 같은 변경 record를 immutable하게 저장한다.
3. 변경 record 전용 stable id와 dirty revision을 deterministic하게 만든다.
4. MAP17_06이 save manifest와 regeneration apply를 만들 수 있도록 modified sector snapshot과 digest를 제공한다.
```

이번 Task는 **실제 save/load 파일을 쓰지 않는다.**  
또한 여기서 말하는 stable id는 terrain/sector modification record ID다. 몬스터, NPC, 보상, 상점, hazard population의 stable spawn ID는 `MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS` 책임으로 남긴다.

금지:

```text
disk save/load file write
save manifest generation or regeneration apply
Unity Tilemap / Collider / Rigidbody / Physics2D 생성 또는 변경
GameObject / Prefab instantiate, enable, disable, destroy
Scene / Prefab / Tilemap mutation
Camera / streaming loader integration
Addressables / Resources / AssetDatabase load
population spawn ID generation
production seed approval
```

MAP17_06은 이 Task의 in-memory storage snapshot을 받아 seed/version/hash와 modified sector만 저장하는 manifest를 구현한다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
sector modification storage가 실제 save file write와 어떻게 다른지
0..1535 local index와 stable modification id가 무엇을 보장하는지
population stable spawn ID를 만들지 않았다는 점
MAP17_06에 넘기는 산출물
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_04 Result exists
MAP17_04 Result STATUS: PASS
MAP17_04 Result SHA-256:
146b66793e74fbfcd008aba3548c5ec9f9300ad31b6ae34ad090e8065af81ef3

MAP17_04 installed task SHA-256:
4ceadbe998821f206ea33ba90b52fc5c7fd719b618d4282da619f5fdbdfc98c0

MAP17_04 window snapshot digest:
cb3bd4d7037ced7745cb7080e2e80c35057770e9fa2278743360f659373be07a

MAP17_04 window diff digest:
fa5e1f6ddedc374a0399b6fd5c04d5cfb2939e24bc2c03f4f49a91713c47ec2b

MAP17_04 shifted-window diff digest:
d559696b16f7ffe46cfb6092ca8ae998b183fc2ca608aea43a880bc8206eab88

MAP17_04 transition plan digest:
4276889b5ba3af471505d26181b902d471e4a6198392afce9c5890b684333489

Current Task before apply: NONE
MAP17_04: COMPLETE
MAP17_05: LOCKED before apply
MAP17_06: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17_04/MAP17_03/MAP17_02/MAP17_01 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
GeneratedCellPlacementPlan
GeneratedTilemapBakePlan
GeneratedColliderCacheKey
GeneratedColliderCacheSnapshot
GeneratedSectorRuntimeHandle
GeneratedSectorRuntimeHandleLifecycle
GeneratedSectorStreamingWindow
GeneratedSectorWindowDiff
GeneratedSectorWindowPlanner
```

기준 수량:

```text
world sectors: 13x13 = 169
sector size: 48x32 cells
sector local index range: 0..1535
logical layer count: 7
source placement cells: 1536
source layer refs: 10752
marker slots: 24
runtime handle states: Unloaded / Preloaded / Active / SleepingModified
```

MAP17_04는 actual streaming activation을 하지 않았고, 이번 Task도 이 경계를 보존한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedSectorLocalCellIndex` | 0..1535 sector-local cell address value object |
| `GeneratedSectorModificationTarget` | sector coordinate, local index, layer id, source token, optional slot ref를 묶은 변경 대상 |
| `GeneratedSectorModificationKind` | DestroyTile, ReplaceTile, CollectPickup, ChangeDeviceState, ConsumeSlot 등 변경 종류 |
| `GeneratedSectorModificationStableId` | 변경 record 전용 deterministic stable id |
| `GeneratedSectorModificationRecord` | target, kind, revision, value payload, source digest, timestamp-free record |
| `GeneratedSectorModificationSet` | 한 sector의 ordered immutable modification records |
| `GeneratedModifiedSectorSnapshot` | dirty revision, base digests, modified records, handle state handoff |
| `GeneratedSectorModificationStorage` | world 내 modified sectors의 immutable storage snapshot |
| `GeneratedSectorModificationApplyPlan` | logical bake/handle에 modification을 적용하기 위한 pure-data command plan |
| `GeneratedSectorModificationFailure` | invalid index, duplicate conflict, stale digest, unknown target 등 failure reason |
| `GeneratedSectorModificationResult` | success/failure wrapper |
| `GeneratedSectorModificationDigest` | storage/apply snapshot canonical digest |
| `GeneratedSectorModificationStore` | add/merge/replace/compact/query/apply plan을 수행하는 pure-data service |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorModificationRecord.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorModificationStorage.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorModificationStore.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSectorModificationStoreTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Modification storage 규칙

### 5.1 Local index addressing

변경 대상은 반드시 sector-local index `0..1535`를 사용한다.

필수:

```text
local index 0 maps to sector cell (0,0)
local index 47 maps to sector cell (47,0)
local index 48 maps to sector cell (0,1)
local index 1535 maps to sector cell (47,31)
local index -1 rejected
local index 1536 rejected
cross-sector coordinate mismatch rejected
layer id outside 7 layers rejected
```

Coordinate conversion은 MAP16_09 geometry snapshot을 authority로 삼는다. 새 sector size literal authority를 만들지 않는다.

### 5.2 Modification kind and payload

최소 변경 종류:

```text
DestroyTile
ReplaceTile
CollectPickup
ChangeDeviceState
ConsumeSlot
```

각 record는 timestamp, frame count, object instance id, random guid에 의존하지 않는다.

Payload 규칙:

```text
DestroyTile: target layer/cell의 occupancy를 logical removed로 표시
ReplaceTile: old tile code/source와 new tile code/source를 모두 기록
CollectPickup: marker/slot ref가 있으면 collected 상태로 표시
ChangeDeviceState: device state key/value를 stable normalized string으로 기록
ConsumeSlot: slot ref와 source owner를 보존
```

MAP17_05는 실제 아이템 지급, 장치 실행, 적/NPC/보상 spawn을 하지 않는다.

### 5.3 Stable modification ID

`GeneratedSectorModificationStableId`는 최소 다음 값을 canonical line으로 묶어 만든다.

```text
world seed or reference seed if public
generator/data version if public
sector coordinate
sector local index
layer id
source owner/provenance token
optional marker slot id
modification kind
modification schema version
```

필수:

```text
same target + same kind -> same id
different local index -> different id
different layer -> different id
different sector -> different id
different slot ref -> different id
lower-hex SHA-256: YES
random Guid/NewGuid usage: 0
population/content spawn ids created: 0
```

이 ID는 terrain/sector modification record 전용이다. MAP18 population stable spawn ID와 같은 namespace를 쓰지 않는다.

### 5.4 Storage merge and conflict

Storage는 immutable snapshot을 반환한다.

필수:

```text
add record increments dirty revision
idempotent same record merge does not duplicate
newer revision replaces older compatible record
conflicting same target/kind/value fails atomically
unknown target fails atomically
stale base bake/cache/window digest fails atomically
compact preserves final semantic state and digest stability
query by sector returns deterministic order
```

Conflict를 silent overwrite하지 않는다.

### 5.5 Apply plan

`GeneratedSectorModificationApplyPlan`은 modified sector snapshot을 logical bake/handle 상태에 적용하기 위한 pure-data plan이다.

필수:

```text
input logical bake records are not mutated in place
apply plan command count reported
DestroyTile/ReplaceTile affects logical layer record only in output plan
CollectPickup/ConsumeSlot affects marker/slot state only in output plan
ChangeDeviceState affects device state map only in output plan
SleepingModified handle receives dirty revision
durable save writes: 0
Tilemap writes: 0
GameObject changes: 0
```

MAP17_06 owns regeneration-time apply after base seed rebuild. This Task only prepares the command plan and in-memory snapshot.

### 5.6 Digest

`BakingCanonicalDigest`를 사용해서 modification set, storage snapshot, apply plan digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable record insertion order
stable compact order
mutation sensitivity
```

Digest canonical line은 domain field order를 명시한다. display name이나 file system order를 dependency key로 쓰지 않는다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
disk save/load file write
save manifest generation
regeneration apply after seed rebuild
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
Authoring CSV edits
Generated CSV commits
population/content stable spawn ID generation
production seed approval
MAP17_06 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_05`만 선택한다.

```text
MAP17_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02/MAP17_03/MAP17_04 selections: 0
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
SectorModificationTargetsAddressCellsByLocalIndexLayerAndProvenance
StableModificationIdsAreDeterministicAndSeparateFromPopulationSpawnIds
ModificationStoragePublishesDirtyRevisionSnapshotsAndDigests
DestroyReplaceCollectDeviceAndConsumeSlotRecordsApplyAsPureData
DuplicateConflictingOutOfBoundsUnknownAndStaleMutationsFailAtomically
SleepingModifiedHandleReceivesDirtyRevisionWithoutDurableSave
ModifiedSectorStorageCompactsAndQueriesRecordsDeterministically
ModificationDigestsAreStableAcrossRepeatReverseCultureAndRecordOrder
ModificationStorageDoesNotWriteFilesCreateObjectsSpawnContentOrMutateScenes
Map17HandoffKeepsMap17_06Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_05]
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
MAP17_04 window snapshot digest reused:
MAP17_04 window diff digest reused:
MAP17_04 transition plan digest reused:
source world sectors observed: 169/169
source runtime handle states observed: Unloaded/Preloaded/Active/SleepingModified
source window active/preload membership observed:
source logical bake records available: 10752/10752
source sector cells available: 1536/1536

local index range accepted: 0..1535
local index coordinate probes passed:
invalid local index probes passed:
layer id validation probes passed:
cross-sector mismatch probes passed:

modification kinds published: 5/5
modification records authored by focused fixture:
stable modification ids lower-hex SHA-256: YES
stable modification id collision probes: 0
random Guid/NewGuid usage: 0
population/content stable spawn ids created: 0

modified sectors in storage snapshot:
dirty revision increments:
idempotent merge duplicate records: 0
conflict failure probes passed:
unknown target failure probes passed:
stale digest failure probes passed:
compact preserves final state: YES
query order deterministic: YES

apply plan command count:
apply plan in-place input mutations: 0
destroy/replace logical layer command probes:
collect/consume slot state command probes:
device state command probes:
SleepingModified dirty revision handoff: YES
durable save writes: 0
save manifest files generated: 0
regeneration apply executions: 0

modification set digest lower-hex SHA-256: YES
modification set digest:
storage snapshot digest lower-hex SHA-256: YES
storage snapshot digest:
apply plan digest lower-hex SHA-256: YES
apply plan digest:
repeat/reverse/culture/record-order/compact-order digest mismatches: 0/0/0/0/0
mutation sensitivity probes passed:

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
runtime objects spawned: 0
production seed approvals: 0
MAP17_06 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If an existing save, inventory, item, device, or runtime spawn component lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_05 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
sector modification storage and apply plan contract created
0..1535 local index and stable modification id covered
population/content stable spawn ids remain uncreated
no disk save/load or save manifest generation
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP17_06 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE: COMPLETE
MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_05: implement sector modification storage
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.

