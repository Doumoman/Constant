```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
  task_file: TASKS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION.md
  requires_current_task: NONE
  requires_completed_task: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
  requires_result:
    path: REPORTS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES_RESULT.md
    status: PASS
    sha256: f7decc2c2cfd0e2473e7966201403eaf5ecf5998f6b9affe224321f3356bf573
  requires_installed_task:
    path: TASKS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES.md
    sha256: a7f82c314787dc864e5cc095c4d602980810f7edeafc1be2f53862ca20d7262a
  sets_current_task: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
```

# MAP17_04 - Implement Preload Active and Preactivation

```text
TASK: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_03의 pure-data runtime handle과 collider cache를 기반으로, 실제 Scene object를 켜기 전 단계의 **sector streaming window plan**을 구현한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. camera/player가 속한 기준 sector를 중심으로 7x7 data preload window를 계산한다.
2. 같은 기준 sector를 중심으로 5x5 active window를 계산하고 preload window의 subset임을 보장한다.
3. sector 경계 접근 시 다음 sector를 preactivation candidate로 승격하는 조건과 diff를 계산한다.
4. Unloaded / Preloaded / Active / SleepingModified handle 전이 계획을 순수 데이터로 발행한다.
```

이번 Task는 **실제 streaming loader나 Scene 활성화가 아니다.**

금지:

```text
Tilemap / Collider / Rigidbody / Physics2D 생성 또는 변경
GameObject / Prefab instantiate, enable, disable, destroy
Scene / Prefab / Tilemap mutation
Camera transform 읽기/쓰기 또는 Cinemachine 연동
Addressables / Resources / AssetDatabase load
disk cache, save, load 구현
durable sector modification storage
stable spawn id 생성
production seed 승인
```

MAP17_05가 sector modification storage를 만들 수 있도록, 이번 Task는 window membership, transition diff, dirty sector 보존 정책만 넘긴다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
7x7 preload와 5x5 active window가 각각 무엇을 보장하는지
preactivation이 실제 Scene 활성화와 어떻게 다른지
SleepingModified sector를 어떻게 보존하는지
MAP17_05에 넘기는 산출물
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
MAP17_03 Result exists
MAP17_03 Result STATUS: PASS
MAP17_03 Result SHA-256:
f7decc2c2cfd0e2473e7966201403eaf5ecf5998f6b9affe224321f3356bf573

MAP17_03 installed task SHA-256:
a7f82c314787dc864e5cc095c4d602980810f7edeafc1be2f53862ca20d7262a

MAP17_03 cache key digest:
e5804aba97511cf73c080ac325d7a428915732981944fcbe1e83c1b0b334c5ca

MAP17_03 collider rebuild plan digest:
2ab1b2fa4ca7f7c8e57dbf62456cc5c8f3faa43854c600e7b1c8f7a3ed02e599

MAP17_03 runtime handle digest:
0c4ea997c35c04d9386d96e41611cffe9b5b3a9006a2b94222d5883cf8279331

Current Task before apply: NONE
MAP17_03: COMPLETE
MAP17_04: LOCKED before apply
MAP17_05: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17_03/MAP17_02/MAP17_01 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
GeneratedTilemapBakePlan
GeneratedTilemapSeamReport
GeneratedColliderRebuildPlan
GeneratedColliderCacheKey
GeneratedColliderCacheEntry
GeneratedColliderCacheSnapshot
GeneratedSectorRuntimeHandle
GeneratedSectorRuntimeHandleLifecycle
```

기준 수량:

```text
world sectors: 13x13 = 169
sector size: 48x32 cells
preload window radius: 3 sectors
preload window max size: 7x7 = 49 sectors
active window radius: 2 sectors
active window max size: 5x5 = 25 sectors
active window must be subset of preload window
center sector valid range: x 0..12, y 0..12
```

MAP17_03은 실제 Collider/Physics/Tilemap work를 하지 않았고, 이번 Task도 이 경계를 보존한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedSectorCoordinate` | 13x13 world 안의 sector integer coordinate |
| `GeneratedSectorWindowKind` | `Preload`, `Active`, `Preactivation`, `EvictCandidate` 등 window role |
| `GeneratedSectorWindowMember` | sector coordinate, distance, role, expected runtime state, cache key reference |
| `GeneratedSectorStreamingWindow` | center sector 기준 preload/active membership snapshot |
| `GeneratedSectorWindowRequest` | center sector, camera/player local progress, direction hint, edge threshold |
| `GeneratedSectorWindowDiff` | previous window와 next window의 add/remove/promote/demote/preserve diff |
| `GeneratedSectorPreactivationCandidate` | 경계 접근으로 미리 승격할 sector와 reason |
| `GeneratedSectorPreactivationPolicy` | threshold, direction, hysteresis, world-edge clamp 규칙 |
| `GeneratedSectorStreamingFailure` | invalid center, out-of-world, missing handle/cache, forbidden transition 등 failure reason |
| `GeneratedSectorStreamingResult` | success/failure wrapper |
| `GeneratedSectorWindowDigest` | window membership/diff canonical digest |
| `GeneratedSectorWindowPlanner` | request와 existing handles/cache를 streaming window plan으로 변환 |
| `GeneratedSectorHandleTransitionPlan` | MAP17_03 lifecycle을 사용한 pure-data state transition batch |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorStreamingWindow.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorWindowPlanner.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorPreactivationPolicy.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSectorWindowPlannerTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Window 규칙

### 5.1 7x7 preload window

Preload window는 center sector에서 Chebyshev distance `<= 3`인 world-in-bounds sectors다.

필수:

```text
center sector in middle preload count: 49/49
corner sector preload count: 16/16
edge non-corner preload count: actual
all preload members in 0..12 x 0..12
duplicates: 0
preload members sorted deterministically
```

Preloaded sector는 MAP17_03 handle state로 `Preloaded`가 될 수 있는 pure-data transition plan만 가진다. 실제 tile/collider/asset load는 하지 않는다.

### 5.2 5x5 active window

Active window는 center sector에서 Chebyshev distance `<= 2`인 world-in-bounds sectors다.

필수:

```text
center sector in middle active count: 25/25
corner sector active count: 9/9
edge non-corner active count: actual
active subset of preload: YES
active-only outside preload: 0
duplicates: 0
active members sorted deterministically
```

Active는 MAP17_04에서 Scene active object를 뜻하지 않는다. 후속 adapter가 활성화할 수 있는 validated handle state eligibility다.

### 5.3 Preactivation

Preactivation은 center sector의 cell-local 또는 normalized progress가 edge threshold를 넘을 때, 진행 방향의 neighbor sector를 candidate로 표시한다.

Minimum policy:

```text
threshold low/high with hysteresis
direction hint: Left / Right / Down / Up / None
world edge clamp
diagonal approach can produce up to 3 candidates
candidate must be inside preload window or next preload window
candidate must have valid cache/handle transition plan
```

Preactivation은 GameObject enable이나 Tilemap write가 아니다. Result에 "preactivation candidates"와 "executed activations: 0"을 분리해 기록한다.

### 5.4 Window diff

Previous window와 next window를 비교해 다음을 계산한다.

```text
add preload
remove preload
promote preload->active
demote active->preload
preserve active
preserve preload
evict candidate
preserve sleeping modified
```

`SleepingModified` sector는 active/preload window 밖으로 나가도 dirty revision과 reason을 잃지 않는다. Durable save는 MAP17_05의 책임이다.

### 5.5 Runtime handle transition

MAP17_03의 lifecycle 규칙을 사용한다.

필수:

```text
Unloaded -> Preloaded for preload add
Preloaded -> Active for active promote
Active -> Preloaded for active demote
Preloaded -> Unloaded for evict clean preload
Active -> SleepingModified only when mutation request exists
SleepingModified -> Unloaded allowed only with dirty metadata preserved in handoff
Unloaded -> Active without preload rejected
```

Transition batch는 순수 데이터이며 Scene object 상태를 바꾸지 않는다.

### 5.6 Digest

`BakingCanonicalDigest`를 사용해서 window snapshot digest, diff digest, transition plan digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable handle/cache order
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
GameObject instantiate / enable / disable / destroy
Camera or Cinemachine integration
Addressables / Resources / AssetDatabase load
streaming thread/job implementation
durable save/load implementation
Authoring CSV edits
Generated CSV commits
production seed approval
MAP17_05 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_04`만 선택한다.

```text
MAP17_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02/MAP17_03 selections: 0
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
PreloadWindowPublishesSevenBySevenInBoundsSectorMembership
ActiveWindowPublishesFiveByFiveSubsetOfPreload
WorldEdgesAndCornersClampWindowsWithoutDuplicates
PreactivationPolicyMarksNeighborCandidatesBeforeBoundaryCrossing
WindowDiffReportsAddRemovePromoteDemotePreserveAndEvictDeterministically
SleepingModifiedSectorsPreserveDirtyRevisionAcrossWindowChanges
TransitionPlanUsesRuntimeHandleLifecycleWithoutSceneActivation
WindowDigestsAreStableAcrossRepeatReverseCultureAndHandleOrder
PlannerRejectsInvalidCentersMissingCacheAndForbiddenTransitionsAtomically
Map17HandoffKeepsMap17_05Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_04]
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
MAP17_03 cache key digest reused:
MAP17_03 collider rebuild plan digest reused:
MAP17_03 runtime handle digest reused:
source runtime handle states observed: Unloaded/Preloaded/Active/SleepingModified
source allowed transitions reused: 7/7
source collider cache entries observed:
source sector coordinates observed:

world sectors observed: 169/169
middle preload window count: 49/49
middle active window count: 25/25
corner preload window count: 16/16
corner active window count: 9/9
edge preload/active window counts:
active subset of preload: YES
duplicate preload/active members: 0/0
out-of-world preload/active members: 0/0

preactivation policy threshold/hysteresis published:
preactivation direction probes passed:
preactivation candidates inside valid window:
executed scene activations: 0
camera/cinemachine integration: 0

window diff add/remove/promote/demote/preserve/evict probes:
transition plan records published:
transition plan execution side effects: 0
forbidden transition probes passed:
invalid center/missing cache failure probes:
SleepingModified dirty revision preserved:
durable save writes: 0

window snapshot digest lower-hex SHA-256: YES
window snapshot digest:
window diff digest lower-hex SHA-256: YES
window diff digest:
transition plan digest lower-hex SHA-256: YES
transition plan digest:
repeat/reverse/culture/handle-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_05 started: NO
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
MapDesign/MCP/TASKS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If an existing camera, streaming, or scene activation component lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_04 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
7x7 preload and 5x5 active window contract created
preactivation candidate and transition diff contract created
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no durable save/load or stable spawn id
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP17_05 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION: COMPLETE
MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_04: implement sector preload windows
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.

