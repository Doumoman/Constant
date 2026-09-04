```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
  task_file: TASKS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES.md
  requires_current_task: NONE
  requires_completed_task: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
  requires_result:
    path: REPORTS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION_RESULT.md
    status: PASS
    sha256: 8e3c6e7c61d0e359d91085de7c71e6fc92f8f95cfa0b460610d1c5d33038dc19
  requires_installed_task:
    path: TASKS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION.md
    sha256: 78ce28910ba94eb56b8e77ebd93b2adeb91fb5db7c82dec05170e8822f8eb57b
  sets_current_task: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
```

# MAP17_03 - Implement Collider Cache and Runtime Handles

```text
TASK: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_02의 logical Tilemap bake packet을 기반으로, 실제 Unity Collider를 만들기 전 단계의 **pure-data collider cache**와 **sector runtime handle lifecycle**을 구현한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. logical layer buffer에서 solid/platform/hazard/protection collision mask를 유도한다.
2. sector-local collider span과 rebuild plan을 deterministic하게 만든다.
3. bake/seam digest와 mutation revision을 포함한 collider cache key를 정의한다.
4. sector runtime handle 상태를 Unloaded / Preloaded / Active / SleepingModified로 전이시킨다.
```

이번 Task는 **Unity Collider를 생성하거나 rebuild하지 않는다.**

금지:

```text
TilemapCollider2D / CompositeCollider2D / Collider2D / Rigidbody2D 생성
Physics2D query or simulation
Tilemap.SetTile / SetTiles / ClearAllTiles / CompressBounds 호출
Scene / Prefab / Tilemap mutation
GameObject / Prefab instantiate
streaming radius / camera preactivation 구현
save/load storage 구현
stable spawn id 생성
production seed 승인
```

MAP17_04가 preload/active window를 만들 수 있도록, 이번 Task는 pure-data handle state와 cache invalidation evidence만 넘긴다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
이번 collider cache가 실제 Unity Collider rebuild와 어떻게 다른지
runtime handle state가 무엇을 보장하는지
MAP17_04에 넘기는 산출물
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
MAP17_02 Result exists
MAP17_02 Result STATUS: PASS
MAP17_02 Result SHA-256:
8e3c6e7c61d0e359d91085de7c71e6fc92f8f95cfa0b460610d1c5d33038dc19

MAP17_02 installed task SHA-256:
78ce28910ba94eb56b8e77ebd93b2adeb91fb5db7c82dec05170e8822f8eb57b

MAP17_02 logical bake digest:
139465f70d40e6b9a3fdd4bb55696c38e89d1856912f4bec2644edb4c6b47602

MAP17_02 seam report digest:
d1a1febd5c9c10481817e5e6c027071fe2890bf2ea79c34252c2a7caaedc7fda

Current Task before apply: NONE
MAP17_02: COMPLETE
MAP17_03: LOCKED before apply
MAP17_04: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17_02/MAP17_01/MAP16 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
GeneratedCellPlacementPlan
GeneratedTilemapLayerBakePlan
GeneratedTilemapLayerBuffer
GeneratedTilemapCellBakeRecord
GeneratedTilemapBakeCommand
GeneratedTilemapSeamReport
GeneratedTilemapLayerBaker
```

기준 수량:

```text
sector width/height: 48/32
sector cells: 1536
logical layer count: 7
logical total layer records: 10752
logical bake commands: 10752
4x4 seam adjacency pairs: 688
12x8 seam adjacency pairs: 240
socket side signatures: 64
marker slots: 24
```

MAP17_02는 실제 Unity Tilemap write를 하지 않았고, production Tile/Prefab registry도 아직 승인하지 않았다. 이번 Task도 이 경계를 보존한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedCollisionMaskKind` | solid/platform/hazard/protection/debug 등 logical collision mask kind |
| `GeneratedColliderCellMask` | sector-local 1536 cell에 대한 mask occupancy |
| `GeneratedColliderSpan` | collider adapter가 나중에 소비할 수 있는 contiguous integer cell span |
| `GeneratedColliderRebuildPlan` | mask별 span, source digest, dirty reason, adapter command count를 담는 pure-data rebuild plan |
| `GeneratedColliderCacheKey` | geometry, logical bake digest, seam digest, registry digest, mutation revision을 포함한 stable key |
| `GeneratedColliderCacheEntry` | cache key와 rebuild plan을 묶은 immutable entry |
| `GeneratedColliderCacheSnapshot` | hit/miss/evict/invalidate가 deterministic한 read-only cache snapshot |
| `GeneratedSectorRuntimeHandleId` | sector coordinate와 seed/version/digest에서 유도된 stable handle id |
| `GeneratedSectorRuntimeState` | `Unloaded`, `Preloaded`, `Active`, `SleepingModified` 상태 enum |
| `GeneratedSectorRuntimeHandle` | sector state, cache key, bake digest, mutation revision, dirty flag, diagnostics |
| `GeneratedSectorRuntimeTransition` | allowed/forbidden state transition record |
| `GeneratedSectorRuntimeHandleResult` | success/failure wrapper |
| `GeneratedSectorRuntimeHandleFailure` | stale digest, missing cache, forbidden transition, mutation mismatch 등 failure reason |
| `GeneratedSectorRuntimeHandleLifecycle` | handle state machine과 cache invalidation을 수행하는 pure-data service |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedColliderCachePlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedColliderCache.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRuntimeHandle.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRuntimeHandleLifecycle.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSectorRuntimeHandleTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Collider cache 규칙

### 5.1 Logical mask derivation

`GeneratedColliderRebuildPlan`은 MAP17_02 logical bake plan에서만 collision mask를 유도한다.

필수:

```text
source logical layer count: 7/7
source layer records consumed: 10752/10752
sector cells covered: 1536/1536
solid mask cells counted:
platform mask cells counted:
hazard mask cells counted:
protection mask cells counted:
debug/non-colliding records ignored explicitly:
mask out-of-bounds cells: 0
mask duplicate cells per kind: 0
```

Cell classification은 기존 layer/source/provenance 계약을 소비해야 하며, 새 tile semantics를 임의로 발명하지 않는다. 모호한 layer가 있으면 `NonCollidingUnknown` 또는 failure reason으로 분리하고 Result에 보고한다.

### 5.2 Span and command plan

Collider span은 integer grid 기반 pure data다.

필수:

```text
all spans are sector-local integer rectangles or runs
all span cells remain inside 48x32
span cells exactly match source masks
span order is deterministic
empty masks are explicit, not omitted silently
adapter command count is reported but not executed
```

Span 압축 알고리즘은 단순해도 된다. 핵심은 byte-stable output과 source mask equivalence다.

### 5.3 Cache key

`GeneratedColliderCacheKey`는 최소 다음을 포함한다.

```text
geometry snapshot digest or ordered geometry lines
MAP17_02 logical bake digest
MAP17_02 seam report digest
asset registry digest if available
sector coordinate
generator/data version if public
mutation revision
collision policy version
```

필수:

```text
same input -> cache hit
changed bake digest -> cache miss / rebuild required
changed seam digest -> cache miss / rebuild required
changed mutation revision -> cache miss / rebuild required
registry order changes -> no digest mismatch
cache key lower-hex SHA-256: YES
```

### 5.4 Runtime handle state machine

Allowed state transitions:

```text
Unloaded -> Preloaded
Preloaded -> Active
Active -> Preloaded
Active -> SleepingModified
SleepingModified -> Active
SleepingModified -> Unloaded
Preloaded -> Unloaded
```

Forbidden examples:

```text
Unloaded -> Active without preload
Preloaded -> SleepingModified without active mutation
SleepingModified -> Preloaded without preserving dirty revision
Active -> Active with stale cache key
any transition with mismatched sector coordinate
```

State meanings:

| State | Meaning |
|---|---|
| `Unloaded` | no active bake/collider/cache handle retained for runtime use |
| `Preloaded` | bake and collider plan are validated and ready for activation, no Scene object is active |
| `Active` | handle is eligible for MAP17_04 active window, no Scene mutation is performed here |
| `SleepingModified` | sector has in-memory mutation revision that invalidates clean collider cache |

MAP17_05 owns durable modification storage. This Task may model mutation revision and dirty reason only as in-memory handle metadata.

### 5.5 Digest

`BakingCanonicalDigest`를 사용해서 collider rebuild plan digest와 runtime handle digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable cache insertion order
mutation sensitivity
```

Digest canonical line은 domain field order를 명시한다. display name이나 file system order를 dependency key로 쓰지 않는다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
Scene mutation
Prefab mutation
GameObject instantiate
camera/preactivation radius implementation
streaming/load/save implementation
Authoring CSV edits
Generated CSV commits
production seed approval
MAP17_04 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_03`만 선택한다.

```text
MAP17_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02 selections: 0
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
ColliderMasksAreDerivedFromLogicalBakeLayersWithoutUnityPhysics
ColliderSpansExactlyCoverSourceMasksAndStayInsideSectorBounds
ColliderRebuildPlanPublishesDeterministicAdapterCommandsWithoutExecutingThem
ColliderCacheKeyChangesForBakeSeamMutationAndPolicyDigestChanges
ColliderCacheSnapshotReportsHitMissEvictAndInvalidateDeterministically
RuntimeHandleLifecycleAllowsOnlyDocumentedStateTransitions
SleepingModifiedPreservesDirtyRevisionWithoutWritingSaveData
HandleAndColliderDigestsAreStableAcrossRepeatReverseCultureAndCacheOrder
RuntimeHandlesDoNotCreateTilemapsCollidersRigidbodiesGameObjectsOrFiles
Map17HandoffKeepsMap17_04Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_03]
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
MAP17_02 logical bake digest reused:
MAP17_02 seam report digest reused:
source logical layer count observed: 7/7
source layer records consumed: 10752/10752
source sector cells covered: 1536/1536
source seam pairs preserved: 928/928
source socket references preserved: 64/64
source marker slots preserved: 24/24

collider mask kinds published:
solid mask cells:
platform mask cells:
hazard mask cells:
protection mask cells:
non-colliding/ignored records:
mask duplicate/out-of-bounds cells: 0/0
collider spans published:
span cells match masks:
span out-of-bounds cells: 0
adapter commands planned:
adapter commands executed: 0

cache key lower-hex SHA-256: YES
cache key digest:
collider rebuild plan digest lower-hex SHA-256: YES
collider rebuild plan digest:
runtime handle digest lower-hex SHA-256: YES
runtime handle digest:
cache hit/miss/invalidate/evict probes:
repeat/reverse/culture/cache-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

runtime handle states published: Unloaded/Preloaded/Active/SleepingModified
allowed transition probes passed:
forbidden transition probes passed:
stale cache key transition failures:
dirty revision preserved in SleepingModified:
durable save writes: 0

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject/Prefab instantiation: 0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_04 started: NO
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
MapDesign/MCP/TASKS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If an existing collider adapter or streaming component lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_03 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
collider cache plan and runtime handle lifecycle created
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject work
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP17_04 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES: COMPLETE
MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_03: implement collider cache handles
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.

