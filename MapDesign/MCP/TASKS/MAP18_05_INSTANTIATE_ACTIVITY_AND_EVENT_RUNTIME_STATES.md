```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
  task_file: TASKS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES.md
  requires_current_task: NONE
  requires_completed_task: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
  requires_result:
    path: REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md
    status: PASS
    sha256: 2d601a8aa25670187c642b15e079c9662af31749d5d00f994120fc75f5085e98
  requires_installed_task:
    path: TASKS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS.md
    sha256: 621ffbfe1c6f38a5f7548278e0e92d3f5166c3d360937e3d1a2c606d4a65b1e1
  sets_current_task: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
```

# MAP18_05 - Instantiate Activity and Event Runtime States

```text
TASK: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP12에서 정의한 ActivityStructure/EventOverlay 계약과 MAP18_04의 occupied/budget surface를 사용해, Activity와 Event의 **logical runtime state records**를 만든다.

이번 Task의 "instantiate"는 Unity object instantiate가 아니다.  
여기서 만드는 것은 실제 prefab, NPC, hazard, reward가 아니라 save/reentry가 추적할 수 있는 상태 레코드와 전이 계약이다.

이번 Task의 책임:

```text
1. Activity runtime state의 Cue -> Active -> Resolved -> Reset 가능 상태 전이를 typed record로 정의한다.
2. Event overlay runtime state의 Empty/Active variant, activation policy, resolved policy를 typed record로 정의한다.
3. 각 Activity/Event runtime state에 stable runtime ID와 save key를 부여한다.
4. MAP18_04 occupied/budget surface를 보존해 Activity/Event runtime state가 이미 예약된 slot을 침범하지 않도록 한다.
5. MAP18_06이 Special state export/debug를 만들 때 사용할 runtime state surface와 digest를 게시한다.
```

금지:

```text
GameObject / Prefab instantiate, enable, disable, destroy
actual Activity prefab spawn
actual Event NPC/reward spawn
actual cue VFX/SFX playback
actual combat, damage, hazard, enemy AI, physics execution
actual shop transaction, reward grant, inventory/resource mutation
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
camera, streaming, preload integration
actual save file write/read or user save slot mutation
SpecialRegion state export/debug implementation
CSV authoring edits
Generated CSV commits
production seed approval
optimization rewrite or broad refactor
shared fixture consolidation
MAP18_06 unlock or execution
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
logical runtime state record가 실제 prefab spawn/event 실행과 어떻게 다른지
Activity의 Cue -> Active -> Resolved/Reset 전이가 어떻게 표현되는지
EventOverlay의 Empty/Active variant와 재진입 정책이 어떻게 표현되는지
stable runtime ID와 save key가 무엇을 보장하는지
MAP18_04 occupied/budget surface를 어떻게 보존했는지
MAP18_06에 넘기는 runtime state/export surface
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
## Activity Event Runtime State Summary
## Runtime Object and Save Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_04 Result exists
MAP18_04 Result STATUS: PASS
MAP18_04 Result SHA-256:
2d601a8aa25670187c642b15e079c9662af31749d5d00f994120fc75f5085e98

MAP18_04 installed task SHA-256:
621ffbfe1c6f38a5f7548278e0e92d3f5166c3d360937e3d1a2c606d4a65b1e1

MAP18_04 hazard/enemy plan digest:
003b2ddc329d736945eda48b8b03df128bd0891c40910aa89e97c965ed3222ac

MAP18_04 occupied surface digest:
39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688

MAP18_04 budget ledger digest:
08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d

MAP18_04 occupied surface entries:
9

MAP18_04 remaining unoccupied candidate count:
3

MAP18_04 selected logical placements:
Hazard -> MAP16_SLOT_02
Enemy -> MAP16_SLOT_10

Carried occupied placements:
RequiredProgressTrigger -> MAP16_SLOT_07
MoonCore -> MAP16_SLOT_08
CassiaSap -> MAP16_SLOT_11
StarNuruk -> MAP16_SLOT_05
ShopInventory -> MAP16_SLOT_01
OptionalResource -> MAP16_SLOT_04
NeutralMapElement -> MAP16_SLOT_06

Current Task before apply: NONE
MAP18_04: COMPLETE
MAP18_05: LOCKED before apply
MAP18_06: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP12/MAP14/MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
ActivityStructure
ActivityShell
ActivityCue
ActivityRemovalSafetyProof
EventOverlay
EventOverlayVariant
GeneratedHazardEnemyPlacementPlan
GeneratedHazardEnemyBudgetLedger
GeneratedPopulationOccupiedSurface
GeneratedContentSlotIndex
GeneratedStableSpawnId
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP12/MAP14/MAP18_01~04 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
MAP18_04 occupied surface entries: 9
MAP18_04 remaining candidate count: 3
required runtime state groups: ActivityRuntimeState, EventRuntimeState
minimum Activity runtime states: 1
minimum Event runtime states: 1
required Activity phases: Cue, Active, Resolved, Resettable
required Event variants: Empty, Active
runtime object spawns in this task: 0
actual save writes in this task: 0
```

Activity/Event authoring source나 MAP18_04 occupied/budget surface를 확인할 수 없으면 partial state surface를 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedActivityRuntimePhase` | Cue, Active, Resolved, Resettable 상태를 정의한다. |
| `GeneratedEventRuntimeVariant` | Empty, Active Event variant를 정의한다. |
| `GeneratedRuntimeStateId` | seed/world/sector/source/state 기반 stable runtime ID를 정의한다. |
| `GeneratedRuntimeSaveKey` | save/reentry가 참조할 stable save key를 정의한다. |
| `GeneratedActivityRuntimeStateRecord` | Activity source, current/allowed phase, cue policy, reset policy, save key를 담는다. |
| `GeneratedEventRuntimeStateRecord` | Event source, Empty/Active variant, activation policy, resolution policy, save key를 담는다. |
| `GeneratedActivityEventRuntimeStateSurface` | Activity/Event state records, occupied/budget passthrough, digest, MAP18_06 export surface를 묶는다. |
| `GeneratedActivityEventRuntimeStateFailure` | missing source, invalid transition, duplicate ID/key, occupied conflict, attempted runtime side effect를 deterministic하게 보고한다. |
| `GeneratedActivityEventRuntimeStateInstantiator` | Activity/Event authoring과 MAP18_04 surface를 사용해 pure-data runtime state records를 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedActivityEventRuntimeState.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedActivityEventRuntimeStateInstantiator.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedActivityEventRuntimeStateTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Runtime state 규칙

### 5.1 Activity state machine

Activity는 최소 다음 상태를 가진다.

```text
Cue
Active
Resolved
Resettable
```

허용 전이:

```text
Cue -> Active
Active -> Resolved
Resolved -> Resettable
Resettable -> Cue
```

금지 전이:

```text
Cue -> Resolved
Active -> Cue
Resolved -> Active
Resettable -> Active
```

전이는 record로만 정의한다. 실제 입력, VFX/SFX, device, reward, enemy, damage, prefab 동작을 실행하지 않는다.

### 5.2 Event overlay variants

EventOverlay는 최소 다음 variant를 가진다.

```text
Empty
Active
```

필수:

```text
Empty variant can publish no runtime object
Active variant publishes stable state identity only
activation policy is deterministic
resolution policy is deterministic
reentry behavior is explicit
```

Event가 없는 상태를 오류로 보지 않는다. Empty variant도 명시적 runtime state record로 남긴다.

### 5.3 Save key and reentry

모든 Activity/Event state record는 stable runtime ID와 save key를 가진다.

필수:

```text
unique runtime state IDs
unique save keys
repeat stable
reverse input order stable
culture stable
mutation sensitive
save key namespace/version published
no PlayerPrefs write
no file write/read
no platform save write
```

Save key는 실제 저장이 아니라 후속 저장 계층이 참조할 identity다. 이번 Task에서 save file을 만들거나 읽으면 `FAIL`이다.

### 5.4 Occupied and budget passthrough

MAP18_04의 occupied/budget surface를 변경 없이 보존한다.

필수:

```text
MAP18_04 occupied entries consumed: 9/9
MAP18_04 occupied digest reused exactly
MAP18_04 budget ledger digest reused exactly
Activity/Event state records do not claim occupied content slots unless source contract explicitly owns an Activity/Event marker slot
occupied conflict count: 0
budget mutation count: 0
MAP18_06 export surface includes runtime state records + MAP18_04 occupied/budget references
```

후속 Task에서 SpecialRegion state export가 열리기 전까지 Special persistence 값을 쓰거나 변환하지 않는다.

### 5.5 Deterministic policy

Selection과 ID 생성은 stable order와 deterministic digest primitive만 사용한다.

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
no implicit Activity/Event source creation
```

### 5.6 Failure policy

다음은 atomic failure다.

```text
missing Activity/Event authoring source
missing MAP18_04 hazard/enemy plan
MAP18_04 occupied surface digest mismatch
MAP18_04 budget ledger digest mismatch
invalid Activity transition
invalid Event variant
duplicate runtime state ID
duplicate save key
occupied surface mutation
budget ledger mutation
attempted prefab spawn, event execution, save write, reward grant, damage, physics, or AI hookup
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 partial state record가 남으면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
GameObject / Prefab instantiate, enable, disable, destroy
actual Activity prefab spawn
actual Event NPC/reward spawn
actual cue VFX/SFX playback
actual combat, damage, hazard, enemy AI, physics execution
Health/Damage/Hitbox/Hurtbox component creation
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
PlayerPrefs write/read
System.IO file write/read for save data
disk save/load file creation
actual user save slot management
platform save storage
shop transaction
reward grant
inventory mutation
resource pickup grant
SpecialRegion state export/debug implementation
CSV authoring edits
Generated CSV commits
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
production seed approval
MAP18_06 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_05`만 선택한다.

```text
MAP18_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01/MAP18_02/MAP18_03/MAP18_04 selections: 0
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
ActivityEventRuntimeStateCreatesActivityAndEventRecords
ActivityRuntimeTransitionsAllowOnlyCueActiveResolvedResettableCycle
EventOverlayRuntimePublishesEmptyAndActiveVariantsWithExplicitReentry
RuntimeStateIdsAndSaveKeysAreUniqueStableAndMutationSensitive
RuntimeStateInstantiatorPreservesMap18_04OccupiedAndBudgetSurfaces
ActivityEventRuntimeSurfacePublishesExportInputForMap18_06
RuntimeStateFailuresAreAtomicAndReportOwnerReasonExpectedActual
RuntimeStateDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder
RuntimeStateInstantiatorDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions
Map18HandoffKeepsMap18_06Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_05]
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
MAP18_04 hazard/enemy plan digest reused:
MAP18_04 occupied surface digest reused:
MAP18_04 budget ledger digest reused:
MAP18_04 occupied surface entries reused:
MAP18_04 remaining candidate count reused:

Activity runtime state records:
Event runtime state records:
Empty Event variants:
Active Event variants:
total runtime state records:
unique runtime state IDs:
unique save keys:
duplicate runtime state IDs:
duplicate save keys:

Activity allowed transitions:
Activity rejected transitions:
invalid transition failure probes:
Event variant checks:
Event reentry policy checks:
activation policy deterministic:
resolution policy deterministic:

MAP18_04 occupied entries consumed:
MAP18_04 occupied digest exact passthrough:
MAP18_04 budget ledger digest exact passthrough:
occupied conflict count:
budget mutation count:
MAP18_06 export surface records:

runtime state surface digest lower-hex SHA-256: YES
runtime state surface digest:
save key set digest lower-hex SHA-256: YES
save key set digest:
export surface digest lower-hex SHA-256: YES
export surface digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing source failure probes:
digest mismatch failure probes:
duplicate runtime state ID failure probes:
duplicate save key failure probes:
occupied/budget mutation failure probes:
attempted runtime spawn/event/save/reward/damage/physics/AI failure probes:
atomic failure partial state records:

runtime Activity prefabs spawned: 0
runtime Event prefabs spawned: 0
actual cue VFX/SFX playback: 0/0
actual event activations executed: 0
actual state transitions executed: 0
actual save writes/reads: 0/0
PlayerPrefs writes/reads: 0/0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
disk save/load files created: 0/0
actual user save slot writes: 0
platform save storage writes: 0
actual damage executions: 0
enemy AI/controller hookups: 0
Health/Damage/Hitbox/Hurtbox component creations: 0/0/0/0
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
MAP18_06 started: NO
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
MapDesign/MCP/TASKS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_05 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes activity event runtime state summary
Result includes runtime object and save boundary notes
Activity and Event runtime state records are created
Activity allowed/rejected transitions are tested
Event Empty/Active variants and reentry policy are tested
stable runtime IDs and save keys are unique
MAP18_04 occupied/budget surfaces are preserved
MAP18_06 export surface is created
no actual spawn, cue playback, event execution, save I/O, reward, damage, AI, physics, tilemap, collider, GameObject, or NavMesh work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_06 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES: COMPLETE
MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_05: instantiate activity and event runtime states
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
