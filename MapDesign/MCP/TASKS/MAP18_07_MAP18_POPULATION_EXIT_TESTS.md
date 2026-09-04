```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_07_MAP18_POPULATION_EXIT_TESTS
  task_file: TASKS/MAP18_07_MAP18_POPULATION_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
  requires_result:
    path: REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md
    status: PASS
    sha256: ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd
  requires_installed_task:
    path: TASKS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG.md
    sha256: 63c5dfa646565bcd74a1a4631f7c8e3868668ace81b2a1cd0aed5d606d48a6cc
  sets_current_task: MAP18_07_MAP18_POPULATION_EXIT_TESTS
```

# MAP18_07 - MAP18 Population Exit Tests

```text
TASK: MAP18_07_MAP18_POPULATION_EXIT_TESTS
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP18_01~06에서 만든 population/runtime state 계층을 하나로 묶어 **MAP18 Phase Exit focused audit**을 수행한다.

이번 Task는 새 생성 기능을 추가하지 않는다.  
역할은 이미 생성된 slot index, mandatory/unique placement, population placement, hazard/enemy budget, Activity/Event runtime state, Special export/debug surface가 서로 모순 없이 이어지는지 검증하는 것이다.

이번 Task의 책임:

```text
1. MAP18_01~06 handoff digest chain을 검증하는 focused exit audit surface를 만든다.
2. required/unique, shop/resource/map-element, hazard/enemy, Activity/Event, Special export row가 같은 identity 규칙을 따르는지 확인한다.
3. occupied slot, stable spawn ID, runtime state ID, save key, persistence key 충돌이 없는지 확인한다.
4. save/reload는 실제 파일 I/O가 아니라 in-memory material round-trip으로만 검증한다.
5. MAP19_01로 넘길 MAP18 approved audit digest와 risk note를 게시한다.
```

금지:

```text
new population generation behavior
new slot selection behavior
new balancing or production seed approval
actual save file write/read
PlayerPrefs write/read
Generated CSV file write/read or commit
GameObject / Prefab instantiate, enable, disable, destroy
actual SpecialRegion/Village/resource/Forge/Boss runtime spawn
actual Activity/Event prefab spawn or execution
actual shop transaction, reward grant, inventory/resource mutation
actual combat, damage, hazard, enemy AI, physics execution
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
camera, streaming, preload integration
optimization rewrite or broad refactor
shared fixture consolidation
MAP19_01 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Exit Audit이 검증한 범위
새 생성 기능을 추가하지 않았다는 점
MAP18_01~06 산출물이 어떤 순서로 이어지는지
required/unique, 일반 population, hazard/enemy, runtime state, special export가 각각 어떤 책임을 유지하는지
실제 save/reload 대신 in-memory round-trip으로 무엇을 증명했는지
MAP19_01에 넘기는 approved audit surface
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
## MAP18 Exit Audit Summary
## No Regression and Runtime Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_06 Result exists
MAP18_06 Result STATUS: PASS
MAP18_06 Result SHA-256:
ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd

MAP18_06 installed task SHA-256:
63c5dfa646565bcd74a1a4631f7c8e3868668ace81b2a1cd0aed5d606d48a6cc

MAP18_06 special export surface digest:
358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5

MAP18_06 CSV material digest:
03711cb7dcc5f576ca6fb6ff16fcbdbbd295a3838c9061761c201f089f8473bf

MAP18_06 debug snapshot digest:
59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed

MAP18_06 MAP18_07 audit surface digest:
ca7cf633a9a2dbf7b5a85d5d847ece3f8fe2bf4c4071e546cbdb6f593397bc72

MAP18_06 total export rows:
18

MAP18_06 unique stable spawn IDs:
9

MAP18_06 runtime state export records:
6

Current Task before apply: NONE
MAP18_06: COMPLETE
MAP18_07: LOCKED before apply
MAP19_01: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedContentSlotIndex
GeneratedMandatoryUniquePlacementPlan
GeneratedPopulationPlacementPlan
GeneratedHazardEnemyPlacementPlan
GeneratedHazardEnemyBudgetLedger
GeneratedActivityEventRuntimeStateSurface
GeneratedSpecialStateExportSurface
GeneratedSpawnStateCsvMaterial
GeneratedSelectionBudgetDebugSnapshot
GeneratedStableSpawnId
GeneratedRuntimeStateId
GeneratedRuntimeSaveKey
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP18_01~06 기존 파일을 대규모 변경하지 않는다.

Read-only MCP references:

```text
MapDesign/MCP/REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md
MapDesign/MCP/REPORTS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md
MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md
```

MCP report reads are allowed for audit evidence. Production code must not use `System.IO` to read these reports.

기준 수량:

```text
MAP18_01 slot source records: 12
MAP18_01 mandatory/unique candidates: 5
MAP18_02 mandatory/unique placements: 4
MAP18_03 general population placements: 3
MAP18_04 hazard/enemy placements: 2
MAP18_04 occupied surface entries: 9
MAP18_05 runtime state records: 6
MAP18_06 export rows: 18
MAP18_06 CSV material rows: 18
MAP18_06 debug snapshot sections: 5
required CoreResource authoritative keys: 3
runtime file writes in this task: 0
regression runs in this task: 0
```

MAP18_06 audit surface를 확인할 수 없으면 partial exit approval을 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedPopulationExitAuditFinding` | severity, owner, invariant, expected, actual, evidence digest를 담는다. |
| `GeneratedPopulationExitAuditSurface` | MAP18_01~06 digest chain, count checks, identity checks, side-effect counters, MAP19_01 handoff digest를 묶는다. |
| `GeneratedPopulationExitAuditResult` | PASS/FAIL/BLOCKED, findings, approved digest, risk notes를 담는다. |
| `GeneratedPopulationExitAuditRunner` | 기존 MAP18 surfaces를 읽어 focused exit audit을 수행한다. |

Suggested production file:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedPopulationExitAudit.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedPopulationExitAuditTests.cs(.meta)
```

기존 public audit helper만으로 충분하면 production file을 새로 만들지 않아도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Exit Audit 규칙

### 5.1 Handoff digest chain

다음 digest를 모두 확인한다.

```text
MAP18_01 slot index digest
MAP18_01 stable ID set digest
MAP18_02 mandatory placement digest
MAP18_02 placement stable ID set digest
MAP18_03 population plan digest
MAP18_03 occupied surface digest
MAP18_04 hazard/enemy plan digest
MAP18_04 occupied surface digest
MAP18_04 budget ledger digest
MAP18_05 runtime state surface digest
MAP18_05 save key set digest
MAP18_05 export surface digest
MAP18_06 special export surface digest
MAP18_06 CSV material digest
MAP18_06 debug snapshot digest
MAP18_06 MAP18_07 audit surface digest
```

Digest mismatch는 `FAIL`이다. 자동 보정이나 재생성은 하지 않는다.

### 5.2 Responsibility chain

Exit Audit은 각 Task의 책임이 섞이지 않았는지 확인한다.

```text
MAP18_01 owns slot index and stable spawn ID source
MAP18_02 owns required trigger and three CoreResource logical preplacements
MAP18_03 owns ShopInventory, OptionalResource, NeutralMapElement logical population
MAP18_04 owns Hazard, Enemy, hierarchical budget ledger
MAP18_05 owns Activity/Event runtime state records and save keys
MAP18_06 owns special export rows, in-memory CSV material, debug snapshot
MAP18_07 owns audit only
```

MAP18_07은 slot을 새로 고르거나 population 결과를 수정하지 않는다.

### 5.3 Identity and slot invariants

필수:

```text
occupied slot reuse count: 0
reserved required/core slot reuse count: 0
stable spawn ID duplicates: 0
runtime state ID duplicates: 0
save key duplicates: 0
persistence key duplicates for active sources: 0
export row key duplicates: 0
legacy short persistence keys accepted: 0
CoreResource canonical key checks: 3/3
```

### 5.4 Slot-only and runtime boundary

MAP18은 아직 runtime object를 생성하지 않는다.

필수:

```text
logical placement entries only
runtime object spawn count: 0
actual shop transaction/reward/inventory/resource mutation count: 0
actual hazard/enemy/damage/combat/AI/physics count: 0
actual save file/PlayerPrefs write/read count: 0
actual CSV file write/read count: 0
Scene/Prefab/Tilemap mutation count: 0
```

### 5.5 In-memory save/reload audit

Save/reload 검증은 실제 파일을 만들지 않는다.

허용:

```text
in-memory serialization material
in-memory parse/round-trip
save key set digest comparison
runtime state surface digest comparison
export row digest comparison
```

금지:

```text
System.IO file write/read
PlayerPrefs write/read
platform save storage
temporary file creation
```

### 5.6 Failure policy

다음은 atomic failure다.

```text
missing MAP18_06 audit surface
handoff digest mismatch
required count mismatch
occupied slot reuse
stable spawn/runtime/save/persistence/export identity collision
legacy short persistence key
runtime side-effect counter nonzero
CSV/save file I/O attempt
in-memory round-trip mismatch
MAP19_01 unlocked or started
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 approved digest가 게시되면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
new population generation behavior
new slot selection behavior
new balancing or production seed approval
actual save file write/read
PlayerPrefs write/read
Generated CSV file write/read or commit
GameObject / Prefab instantiate, enable, disable, destroy
actual SpecialRegion/Village/resource/Forge/Boss runtime spawn
actual Activity/Event prefab spawn or execution
actual cue VFX/SFX playback
actual shop transaction, reward grant, inventory/resource mutation
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
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
production seed approval
MAP19_01 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_07`만 선택한다.

```text
MAP18_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01/MAP18_02/MAP18_03/MAP18_04/MAP18_05/MAP18_06 selections: 0
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
PopulationExitAuditValidatesMap18HandoffDigestChain
PopulationExitAuditVerifiesRequiredUniqueAndCoreResourcePlacements
PopulationExitAuditVerifiesShopResourceMapHazardEnemyAndBudgetSurfaces
PopulationExitAuditVerifiesActivityEventRuntimeStateAndSaveKeySurface
PopulationExitAuditVerifiesSpecialExportCsvMaterialAndDebugSnapshotWithoutFileIo
PopulationExitAuditRejectsSlotReuseIdentityCollisionLegacyKeyAndDigestMismatch
PopulationExitAuditRoundTripsInMemorySaveReloadMaterialWithoutDiskOrPlayerPrefs
PopulationExitAuditDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder
PopulationExitAuditDoesNotSpawnObjectsMutateScenesOrRunRegressions
Map18ExitKeepsMap19_01Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_07]
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
MAP18_06 Result SHA-256 required/actual:
MAP18_06 installed Task SHA-256 required/actual:
MAP18_06 special export surface digest reused:
MAP18_06 CSV material digest reused:
MAP18_06 debug snapshot digest reused:
MAP18_06 MAP18_07 audit surface digest reused:

audited MAP18 task surfaces:
audited upstream digest count:
handoff digest mismatches:
required count mismatches:

slot source records:
mandatory/unique candidates:
mandatory/unique placements:
general population placements:
hazard/enemy placements:
occupied surface entries:
runtime state records:
special export rows:
CSV material rows:
debug snapshot sections:

RequiredProgressTrigger placements:
MoonCore/CassiaSap/StarNuruk placements:
ShopInventory/OptionalResource/NeutralMapElement placements:
Hazard/Enemy placements:
Activity/Event runtime state records:
CoreResource authoritative key checks:
legacy short keys accepted:

occupied slot reuse count:
reserved required/core slot reuse count:
stable spawn ID duplicates:
runtime state ID duplicates:
save key duplicates:
persistence key duplicates for active sources:
export row key duplicates:

in-memory save/reload material rows:
in-memory round-trip mismatches:
save key set digest after round-trip:
runtime state digest after round-trip:
export row digest after round-trip:
actual save writes/reads: 0/0
PlayerPrefs writes/reads: 0/0

MAP18 approved audit digest lower-hex SHA-256: YES
MAP18 approved audit digest:
MAP19_01 handoff digest lower-hex SHA-256: YES
MAP19_01 handoff digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing audit surface failure probes:
handoff digest mismatch failure probes:
required count mismatch failure probes:
identity collision failure probes:
legacy key failure probes:
runtime side-effect failure probes:
in-memory round-trip failure probes:
MAP19_01 unlock/start failure probes:
atomic failure approved digest published:

runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
disk save/load files created: 0/0
actual user save slot writes: 0
platform save storage writes: 0
actual CSV file writes/reads: 0/0
Generated CSV files committed: 0
runtime SpecialRegion/Village/resource/Forge/Boss spawns: 0/0/0/0/0
runtime Activity/Event prefabs spawned: 0/0
actual event activations executed: 0
actual shop transactions/reward grants/inventory mutations/resource mutations: 0/0/0/0
actual damage/combat/AI/physics executions: 0/0/0/0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
NavMesh/pathfinding setup: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
production seed approvals: 0
MAP19_01 started: NO
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
MapDesign/MCP/TASKS/MAP18_07_MAP18_POPULATION_EXIT_TESTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_07_MAP18_POPULATION_EXIT_TESTS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md
MapDesign/MCP/REPORTS/MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md
MapDesign/MCP/REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md
MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_07 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes MAP18 exit audit summary
Result includes no regression and runtime boundary notes
MAP18_01~06 handoff digest chain is verified
required/unique, general population, hazard/enemy, runtime state, special export surfaces are all audited
identity collision counts are 0
in-memory save/reload round-trip passes without disk or PlayerPrefs
MAP18 approved audit digest and MAP19_01 handoff digest are created
no new generation behavior, selection behavior, balancing, or production seed approval
no actual CSV/save file write/read
no actual spawn, event execution, reward, inventory/resource mutation, damage, AI, physics, tilemap, collider, GameObject, or NavMesh work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP19_01 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_07_MAP18_POPULATION_EXIT_TESTS: COMPLETE
MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_07: run population exit audit
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
