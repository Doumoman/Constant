```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
  task_file: TASKS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS.md
  requires_current_task: NONE
  requires_completed_task: MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS
  requires_result:
    path: REPORTS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS_RESULT.md
    status: PASS
    sha256: 35cd66c535d908683df3fe90ccfcfc55a362e19891ecf078c165f7d5c29a9a92
  requires_installed_task:
    path: TASKS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS.md
    sha256: f24861e47cdeed27ec98650a3f8ea871ec53242f4ef0af33626a8756aa53c512
  sets_current_task: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
```

# MAP18_04 - Place Hazards Enemies and Hierarchical Budgets

```text
TASK: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP18_03이 게시한 occupied surface를 침범하지 않고, 필수 경로·보상 접근·복구 바닥을 보호하면서 hazard와 enemy를 **logical placement plan**으로 배치한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. hazard/enemy pool entry와 pressure budget scope를 typed catalog로 만든다.
2. MAP18_02 + MAP18_03 occupied surface 7개를 hazard/enemy placement 후보에서 제외한다.
3. route/reward/recovery protected surface를 적용해 안전하지 않은 후보를 deterministic하게 거절한다.
4. world/patch/sector/cluster/slot hierarchical budget ledger를 만들고 MAP18_05가 사용할 occupied/budget surface를 게시한다.
```

이번 Task의 "place"는 런타임 생성이 아니라 순수 데이터 placement plan이다.  
Enemy entry는 실제 AI, Animator, Health, Damage, Collider를 가진 적이 아니고, Hazard entry도 실제 피해 판정, trigger, tile, physics object가 아니다.

금지:

```text
actual enemy spawn
actual hazard spawn
actual damage execution
actual enemy AI state machine hookup
actual combat encounter start
Health/Damage/Hitbox/Hurtbox component creation
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
NavMesh / pathfinding / agent setup
camera, streaming, preload, save integration
activity/event runtime state instantiation
special region state export
shop transaction, reward grant, inventory mutation, resource pickup grant
disk save/load file write/read
CSV authoring edits
Generated CSV commits
production seed approval
optimization rewrite or broad refactor
shared fixture consolidation
MAP18_05 unlock or execution
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
logical hazard/enemy placement가 실제 runtime spawn/combat와 어떻게 다른지
MAP18_02 + MAP18_03 occupied surface 7개를 어떻게 제외했는지
필수 경로·보상 접근·복구 바닥 보호가 무엇을 보장하는지
hierarchical budget이 world/patch/sector/cluster/slot에서 어떻게 차감되는지
MAP18_05에 넘기는 occupied/budget surface
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
## Hazard Enemy Budget Summary
## Route Protection and Runtime Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_03 Result exists
MAP18_03 Result STATUS: PASS
MAP18_03 Result SHA-256:
35cd66c535d908683df3fe90ccfcfc55a362e19891ecf078c165f7d5c29a9a92

MAP18_03 installed task SHA-256:
f24861e47cdeed27ec98650a3f8ea871ec53242f4ef0af33626a8756aa53c512

MAP18_03 population plan digest:
4fc87b1c2699802761b9956aaf58fdc9ebbfaf6f32f33bdc9b7a776752cd109e

MAP18_03 occupied surface digest:
f5556c9e609de1b71195c45473582009f99b5799cb03052da75682ed9c43e422

MAP18_03 occupied surface entries:
7

MAP18_03 remaining unoccupied candidate count:
5

MAP18_03 selected logical population placements:
ShopInventory -> MAP16_SLOT_01
OptionalResource -> MAP16_SLOT_04
NeutralMapElement -> MAP16_SLOT_06

MAP18_02 carried mandatory/unique placements:
RequiredProgressTrigger -> MAP16_SLOT_07
MoonCore -> MAP16_SLOT_08
CassiaSap -> MAP16_SLOT_11
StarNuruk -> MAP16_SLOT_05

Current Task before apply: NONE
MAP18_03: COMPLETE
MAP18_04: LOCKED before apply
MAP18_05: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP14/MAP16/MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedContentSlotIndex
GeneratedContentSlotIndexEntry
GeneratedStableSpawnId
GeneratedMandatoryUniquePlacementPlan
GeneratedPopulationPlacementPlan
GeneratedPopulationPlacementEntry
GeneratedPopulationOccupiedSurface
GeneratedSectorSpine
GeneratedTraversalEnvelope
GeneratedCanvasOwnership
GeneratedRouteProtectionSurface
GeneratedSliceMarkerProjection
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP14/MAP16/MAP18_01~03 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
MAP18_01 slot source records: 12
MAP18_02 mandatory/unique occupied entries: 4
MAP18_03 population occupied entries: 3
MAP18_03 total occupied surface entries: 7
remaining candidate count before hazard/enemy placement: 5
required logical placement groups: Hazard, Enemy
required budget scopes: World, Patch, Sector, Cluster, Slot
runtime spawn entries in this task: 0
actual combat/hazard executions in this task: 0
```

MAP18_03 occupied surface나 route/reward/recovery protection surface를 확인할 수 없으면 partial plan을 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedHazardEnemyContentKind` | Hazard, Enemy logical content kind를 정의한다. |
| `GeneratedHazardEnemyPoolEntry` | pool namespace/version, biome allowlist, route clearance, safe radius, pressure cost, max count를 담는다. |
| `GeneratedHazardEnemyBudgetScope` | World, Patch, Sector, Cluster, Slot budget scope를 정의한다. |
| `GeneratedHazardEnemyBudgetLedger` | scope별 initial/remaining/spent budget과 spend reason을 deterministic하게 기록한다. |
| `GeneratedHazardEnemyPlacementEntry` | selected slot, content kind, pool entry, budget spends, stable spawn ID, reservation key, protection proof를 담는다. |
| `GeneratedHazardEnemyPlacementPlan` | logical hazard/enemy entries, occupied surface, budget surface, remaining candidates, digest를 묶는다. |
| `GeneratedHazardEnemyPlacementFailure` | occupied reuse, route protection violation, budget overflow, missing candidate, collision, invalid pool을 deterministic하게 보고한다. |
| `GeneratedHazardEnemyBudgetPlanner` | MAP18_01 slot index와 MAP18_03 occupied surface를 사용해 pure-data hazard/enemy plan을 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedHazardEnemyPlacement.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedHazardEnemyBudgetPlanner.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedHazardEnemyBudgetPlannerTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Hazard / Enemy / Budget 규칙

### 5.1 Logical placement groups

다음 두 group을 최소 1개 이상 logical plan에 포함한다.

```text
Hazard
Enemy
```

허용되는 작업:

```text
logical hazard entry creation
logical enemy entry creation
stable spawn ID and reservation key binding
pool key/version publication
route protection proof publication
budget spend ledger publication
occupied and budget surface publication for MAP18_05
```

금지되는 작업:

```text
actual enemy prefab placement
actual hazard tile/prefab placement
actual damage/knockback execution
actual AI/controller/Animator/NavMesh binding
actual trigger/collider/physics creation
actual runtime encounter start
```

### 5.2 Occupied surface exclusion

MAP18_02 + MAP18_03의 seven occupied reservations는 반드시 hazard/enemy placement에서 제외한다.

필수:

```text
MAP18_03 occupied surface entries consumed: 7/7
RequiredProgressTrigger slot excluded
MoonCore slot excluded
CassiaSap slot excluded
StarNuruk slot excluded
ShopInventory slot excluded
OptionalResource slot excluded
NeutralMapElement slot excluded
occupied slot reuse count: 0
reservation key collisions: 0
stable spawn ID collisions: 0
MAP18_05 occupied surface includes MAP18_02 + MAP18_03 + MAP18_04 reservations
```

같은 physical slot double-use는 기본적으로 금지한다. 후속 Task에서 명시적으로 layer 분리 rule을 열기 전까지 같은 slot을 공유하지 않는다.

### 5.3 Route, reward, recovery protection

Hazard/enemy는 필수 경로와 복구 가능성을 망치면 안 된다.

다음 surface와 겹치면 rejection evidence를 남긴다.

```text
mandatory route spine
traversal envelope
required landing tiles
drop recovery floor
reward approach floor
special/village entry buffer
safe pocket
critical socket boundary
```

필수:

```text
protected route intersection accepted/rejected:
protected reward intersection accepted/rejected:
protected recovery intersection accepted/rejected:
safe-radius accepted/rejected:
neighbor-radius accepted/rejected:
critical route violation count: 0
critical reward violation count: 0
critical recovery violation count: 0
```

보호 surface가 없는 프로젝트 fixture라면 임의로 통과시키지 않는다. 현재 public API에서 protection semantic owner를 찾고, 없으면 `BLOCKED`로 멈춘다.

### 5.4 Hierarchical budget

Budget은 다음 scope를 모두 가진다.

```text
World
Patch
Sector
Cluster
Slot
```

Budget spend는 상위 scope부터 하위 scope까지 같은 entry에 대해 동시에 차감한다.

필수:

```text
budget scopes published: 5/5
budget spend entries equal logical hazard/enemy entries
initial budget >= spent budget for every scope
remaining budget never negative
duplicate spend key count: 0
budget overflow failure probes:
budget rollback after failure:
```

Budget 값은 starter fixture 안에서만 작게 정의한다. 실제 production difficulty tuning, encounter pacing 밸런스, seed approval은 이번 Task에서 하지 않는다.

### 5.5 Selection and deterministic policy

Selection은 stable order와 deterministic hash ticket으로만 수행한다.

필수:

```text
input order independent
repeat stable
culture stable
candidate order stable
no UnityEngine.Random
no Random.Range
no System.Random unless wrapped by existing deterministic worldgen authority
no hidden retry loop
no implicit candidate creation
```

같은 후보가 여러 pool에 들어갈 수 있더라도 실제 선택 후에는 occupied surface에 즉시 반영해서 double-use를 막는다.

### 5.6 Digest

`BakingCanonicalDigest` 또는 MAP16_09에서 통합한 동일 digest primitive를 사용해 hazard/enemy plan digest, occupied surface digest, budget ledger digest를 만든다.

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

### 5.7 Failure policy

다음은 atomic failure다.

```text
missing MAP18_03 population plan
MAP18_03 population plan digest mismatch
MAP18_03 occupied surface digest mismatch
occupied slot reused
missing Hazard candidate
missing Enemy candidate
route protection violation
reward approach protection violation
recovery floor protection violation
invalid pool key
invalid budget scope
budget overflow
duplicate budget spend
reservation key collision
stable spawn ID collision
attempted runtime spawn, damage, physics, AI, or combat execution
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 partial placement나 partial budget spend가 남으면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
actual enemy spawn
actual hazard spawn
actual damage execution
actual enemy AI/controller state hookup
actual combat encounter start
Health/Damage/Hitbox/Hurtbox component creation
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
NavMesh or pathfinding setup
Scene mutation
Prefab mutation
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
System.IO file write/read for save data
disk save/load file creation
actual user save slot management
platform save storage
activity/event runtime state instantiation
special region state export
shop transaction
reward grant
inventory mutation
resource pickup grant
CSV authoring edits
Generated CSV commits
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
production seed approval
MAP18_05 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_04`만 선택한다.

```text
MAP18_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01/MAP18_02/MAP18_03 selections: 0
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
HazardEnemyPlanCreatesLogicalHazardAndEnemyGroups
HazardEnemyPlannerConsumesPopulationOccupiedSurfaceAndNeverReusesSlots
MandatoryRouteRewardRecoveryAndSafeFloorAreProtected
HierarchicalBudgetsSpendTopDownAndRejectNegativeOrDuplicateSpends
HazardEnemySelectionUsesStableOrderAndDeterministicTicketsWithoutRandom
HazardEnemyPlanPublishesOccupiedAndBudgetSurfaceForMap18_05
HazardEnemyFailuresAreAtomicAndReportOwnerReasonExpectedActual
HazardEnemyDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder
HazardEnemyPlannerDoesNotSpawnObjectsMutatePhysicsScenesOrRunRegressions
Map18HandoffKeepsMap18_05Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_04]
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
MAP18_03 population plan digest reused:
MAP18_03 occupied surface digest reused:
MAP18_03 occupied surface entries reused:
MAP18_03 remaining candidate count reused:
MAP18_02+MAP18_03 occupied slots excluded:

logical hazard/enemy groups published:
Hazard entries:
Enemy entries:
total logical hazard/enemy entries:
unique content keys:
unique stable spawn IDs:
unique reservation keys:
occupied slot reuse count:
MAP18_04 reservation collisions:
MAP18_04 stable spawn ID collisions:
MAP18_05 occupied surface entries:
remaining unoccupied candidate count:

pool entries published:
pool namespace/version checks:
hazard pool accepted/rejected:
enemy pool accepted/rejected:
biome allowlist accepted/rejected:
route protection accepted/rejected:
reward protection accepted/rejected:
recovery protection accepted/rejected:
safe radius accepted/rejected:
neighbor radius accepted/rejected:
occupied surface accepted/rejected:

budget scopes published:
World budget initial/spent/remaining:
Patch budget initial/spent/remaining:
Sector budget initial/spent/remaining:
Cluster budget initial/spent/remaining:
Slot budget initial/spent/remaining:
budget spend entries:
duplicate budget spend keys:
budget overflow failure probes:
budget rollback after failure:
negative budget values:

selection uses stable order: YES
deterministic hash/ticket selections:
input order dependency detected: NO
UnityEngine.Random/Random.Range calls: 0/0
System.Random direct usage:
hidden retry loop count: 0
implicit candidate creation count: 0
candidate mutation count: 0

hazard/enemy plan digest lower-hex SHA-256: YES
hazard/enemy plan digest:
occupied surface digest lower-hex SHA-256: YES
occupied surface digest:
budget ledger digest lower-hex SHA-256: YES
budget ledger digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing candidate failure probes:
digest mismatch failure probes:
occupied reuse failure probes:
route/reward/recovery protection failure probes:
budget overflow failure probes:
reservation collision failure probes:
stable spawn ID collision failure probes:
attempted runtime spawn/damage/physics/AI failure probes:
atomic failure partial entries:
atomic failure partial budget spends:

runtime hazard placements performed: 0
runtime enemy placements performed: 0
actual damage executions: 0
actual combat encounters started: 0
enemy AI/controller hookups: 0
Health/Damage/Hitbox/Hurtbox component creations: 0/0/0/0
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
NavMesh/pathfinding setup: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
production seed approvals: 0
MAP18_05 started: NO
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
MapDesign/MCP/TASKS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_04 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes hazard enemy budget summary
Result includes route protection and runtime boundary notes
Hazard and Enemy logical groups are created
MAP18_02 + MAP18_03 occupied reservations are excluded
mandatory route/reward/recovery/safe surfaces are protected
hierarchical budgets are represented and tested
selection is deterministic and does not use Unity random
stable IDs and reservation keys are unique
no actual spawn, damage, combat, AI, physics, tilemap, collider, GameObject, or NavMesh work
no shop transaction, reward grant, inventory mutation, device execution, or resource pickup
no actual disk save/load file write/read
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_05 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS: COMPLETE
MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_04: place hazards enemies and budgets
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
