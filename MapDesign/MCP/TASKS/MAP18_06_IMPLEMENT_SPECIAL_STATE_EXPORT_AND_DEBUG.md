```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
  task_file: TASKS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG.md
  requires_current_task: NONE
  requires_completed_task: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
  requires_result:
    path: REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md
    status: PASS
    sha256: b76e46388bd2db9043d313dde29c000fec11105fb873ad8967d315d3c8fbf5ed
  requires_installed_task:
    path: TASKS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES.md
    sha256: 2f53f0d8ec57c3f57bf604990c314d9a931a4709cf4b103c46edb8bae4581f54
  sets_current_task: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
```

# MAP18_06 - Implement Special State Export and Debug

```text
TASK: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_07_MAP18_POPULATION_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP13 SpecialRegion/Village/resource 계약과 MAP18_05 runtime state surface를 묶어, 후속 저장·디버그·CSV 출력 계층이 읽을 수 있는 **pure-data special state export rows**와 **selection/budget debug snapshot**을 만든다.

이번 Task에서 "export"는 파일 쓰기가 아니다.  
CSV 파일을 실제로 생성하거나 커밋하지 않고, CSV로 직렬화 가능한 row model과 deterministic text material만 만든다.

이번 Task의 책임:

```text
1. Resource, Forge, Boss, Village, Activity/Event runtime state를 export 가능한 typed row로 변환한다.
2. MAP18_02~05의 stable IDs, save keys, occupied surface, budget ledger digest를 하나의 export surface로 묶는다.
3. Generated spawn/state CSV로 쓸 수 있는 deterministic row order와 LF-normalized text material을 제공한다.
4. selection, occupied, budget, runtime state digest를 한눈에 확인할 수 있는 debug snapshot을 제공한다.
5. MAP18_07 Exit Test가 검증할 population/runtime state audit surface를 게시한다.
```

금지:

```text
actual CSV file write/read
Generated CSV asset/file commit
actual save file write/read
PlayerPrefs write/read
GameObject / Prefab instantiate, enable, disable, destroy
actual SpecialRegion/Village/resource/Forge/Boss runtime spawn
actual Activity/Event prefab spawn or execution
actual shop transaction, reward grant, inventory/resource mutation
actual combat, damage, hazard, enemy AI, physics execution
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
camera, streaming, preload integration
production seed approval
optimization rewrite or broad refactor
shared fixture consolidation
MAP18_07 unlock or execution
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
special state export row가 실제 save/export file과 어떻게 다른지
Resource/Forge/Boss/Village/Activity/Event state가 어떤 row로 표현되는지
Generated spawn/state CSV material이 파일 쓰기 없이 어떻게 만들어지는지
selection/budget debug snapshot이 어떤 digest와 수치를 모으는지
MAP18_02~05 surface를 어떻게 보존했는지
MAP18_07에 넘기는 audit surface
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
## Special State Export Summary
## Debug Snapshot and File IO Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_05 Result exists
MAP18_05 Result STATUS: PASS
MAP18_05 Result SHA-256:
b76e46388bd2db9043d313dde29c000fec11105fb873ad8967d315d3c8fbf5ed

MAP18_05 installed task SHA-256:
2f53f0d8ec57c3f57bf604990c314d9a931a4709cf4b103c46edb8bae4581f54

MAP18_05 runtime state surface digest:
2774cc515c4531ad90055afb2bdabb4a73439a0e64162194b4c9dbdd51db0f72

MAP18_05 save key set digest:
9c841116463551aff94fe77132c2b7b61d23b07840ee5aa29710799591b0d448

MAP18_05 export surface digest:
2f2665b46b054f408f8e7a1fb97c128ca355b829aa74d4aa7811b2792b9f6d73

MAP18_05 export surface records:
6

MAP18_04 occupied surface digest:
39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688

MAP18_04 budget ledger digest:
08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d

MAP18_04 occupied surface entries:
9

Current Task before apply: NONE
MAP18_05: COMPLETE
MAP18_06: LOCKED before apply
MAP18_07: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP13/MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
SpecialRegionPlan
SpecialLandmarkRegionPlan
VillageShellPlan
VillageStateVariantSet
SpecialPersistenceKey
GeneratedMandatoryUniquePlacementPlan
GeneratedPopulationPlacementPlan
GeneratedHazardEnemyPlacementPlan
GeneratedHazardEnemyBudgetLedger
GeneratedActivityEventRuntimeStateSurface
GeneratedRuntimeSaveKey
GeneratedStableSpawnId
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP13/MAP18_01~05 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
MAP18_05 runtime state export records: 6
MAP18_04 occupied surface entries: 9
required export groups: CoreResource, Forge, Boss, Village, ActivityEventRuntime, SpawnState
minimum CoreResource export rows: 3
minimum Activity/Event runtime rows: 6
minimum debug snapshot sections: Selection, Occupied, Budget, RuntimeState, Persistence
actual CSV files written in this task: 0
actual save writes in this task: 0
runtime object spawns in this task: 0
```

MAP13 authoritative special persistence source나 MAP18_05 export surface를 확인할 수 없으면 partial export surface를 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedSpecialStateExportKind` | CoreResource, Forge, Boss, Village, ActivityEventRuntime, SpawnState row kind를 정의한다. |
| `GeneratedSpecialStateExportRow` | source owner, region/site, persistence key, stable spawn/runtime ID, save key, state kind, row digest를 담는다. |
| `GeneratedSpawnStateCsvMaterial` | CSV header, deterministic row order, LF-normalized text material, row count, digest를 담는다. |
| `GeneratedSelectionBudgetDebugSnapshot` | population/hazard/enemy/activity/event selection, occupied count, budget ledger, digest summary를 담는다. |
| `GeneratedSpecialStateExportSurface` | export rows, CSV material, debug snapshot, MAP18_07 audit digest를 묶는다. |
| `GeneratedSpecialStateExportFailure` | missing source, legacy key, duplicate row/save/runtime ID, digest mismatch, file-write attempt를 deterministic하게 보고한다. |
| `GeneratedSpecialStateExporter` | MAP13 special source와 MAP18_05 runtime state surface를 pure-data export surface로 변환한다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExport.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExporter.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedSpecialStateExporterTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Export 규칙

### 5.1 Export row groups

다음 row group을 모두 표현한다.

```text
CoreResource
Forge
Boss
Village
ActivityEventRuntime
SpawnState
```

`Forge`, `Boss`, `Village` source가 현재 fixture에서 비활성 또는 optional이면 missing으로 실패하지 않는다. 대신 source status를 `AbsentButDeclared` 같은 typed 상태로 row나 debug snapshot에 명시한다.  
`CoreResource` 3종과 `ActivityEventRuntime` 6개는 반드시 존재해야 한다.

허용되는 작업:

```text
pure-data export row creation
CSV header and LF-normalized text material creation in memory
debug snapshot creation
stable runtime/save/spawn ID reference binding
MAP18_07 audit surface publication
```

금지되는 작업:

```text
CSV file creation
asset/database write
save file creation
runtime object placement
SpecialRegion runtime activation
reward grant or state mutation
```

### 5.2 Authoritative persistence keys

MAP13 authoritative persistence key를 사용한다.

필수:

```text
MoonCore authoritative key accepted
CassiaSap authoritative key accepted
StarNuruk authoritative key accepted
legacy short keys accepted: 0
duplicate persistence keys: 0
missing required CoreResource key failure probes:
```

이미 승인된 CoreResource keys:

```text
MoonCore: SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD
CassiaSap: SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD
StarNuruk: SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD
```

Forge/Boss/Village key는 현재 MAP13 public API의 canonical key를 사용한다. 임의 문자열이나 legacy short key를 만들지 않는다.

### 5.3 Generated CSV material boundary

이번 Task는 CSV 파일을 쓰지 않는다. CSV material은 string/bytes model까지만 만든다.

필수:

```text
CSV header stable
CSV rows deterministic order
LF normalization
UTF-8 no BOM material
lower-hex SHA-256 digest
no machine path
no timestamp
no Unity instance ID
actual file write/read calls: 0/0
Generated CSV files committed: 0
```

CSV row order는 다음 안정 기준을 따른다.

```text
export kind
region/site id
source owner
persistence key
stable spawn/runtime id
save key
row version
```

### 5.4 Debug snapshot

Debug snapshot은 사람이 Result에서 선택/예산/상태를 이해할 수 있게 하는 pure-data 요약이다.

필수 sections:

```text
Selection
Occupied
Budget
RuntimeState
Persistence
```

필수 포함 digest:

```text
MAP18_02 placement digest
MAP18_03 population digest
MAP18_04 hazard/enemy plan digest
MAP18_04 occupied surface digest
MAP18_04 budget ledger digest
MAP18_05 runtime state surface digest
MAP18_05 save key set digest
MAP18_05 export surface digest
MAP18_06 special export surface digest
```

Debug snapshot은 EditorWindow, overlay, screenshot, file export가 아니다.

### 5.5 Deterministic policy

Selection, row order, ID binding, digest generation은 stable order와 deterministic digest primitive만 사용한다.

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
no implicit SpecialRegion/Village/Forge/Boss source creation
```

### 5.6 Failure policy

다음은 atomic failure다.

```text
missing MAP18_05 runtime state export surface
MAP18_05 runtime state surface digest mismatch
MAP18_05 save key set digest mismatch
MAP18_05 export surface digest mismatch
missing required CoreResource persistence key
legacy short persistence key
duplicate export row key
duplicate runtime state ID
duplicate save key
duplicate stable spawn ID in export scope
invalid CSV header or row shape
attempted CSV file write/read
attempted save write/read
attempted runtime spawn, reward grant, damage, physics, AI, or event execution
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 partial export row, partial CSV material, partial debug snapshot이 남으면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
actual CSV file write/read
Generated CSV asset/file commit
actual save file write/read
PlayerPrefs write/read
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
MAP18_07 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_06`만 선택한다.

```text
MAP18_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01/MAP18_02/MAP18_03/MAP18_04/MAP18_05 selections: 0
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
SpecialStateExporterCreatesResourceForgeBossVillageRuntimeAndSpawnRows
SpecialStateExportUsesAuthoritativePersistenceKeysAndRejectsLegacyShortKeys
GeneratedSpawnStateCsvMaterialIsDeterministicLfUtf8AndDoesNotWriteFiles
SelectionBudgetDebugSnapshotIncludesRequiredSectionsAndUpstreamDigests
SpecialStateExporterPreservesMap18_05RuntimeSurfaceAndMap18_04BudgetReferences
SpecialStateExportIdsSaveKeysAndRowsAreUniqueStableAndMutationSensitive
SpecialStateExportFailuresAreAtomicAndReportOwnerReasonExpectedActual
SpecialStateExportDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder
SpecialStateExporterDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions
Map18HandoffKeepsMap18_07Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_06]
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
MAP18_05 runtime state surface digest reused:
MAP18_05 save key set digest reused:
MAP18_05 export surface digest reused:
MAP18_05 export surface records reused:
MAP18_04 occupied surface digest reused:
MAP18_04 budget ledger digest reused:

export groups published:
CoreResource export rows:
Forge export rows:
Boss export rows:
Village export rows:
ActivityEventRuntime export rows:
SpawnState export rows:
total export rows:
absent optional special sources declared:
unique export row keys:
unique persistence keys:
unique runtime state IDs:
unique save keys:
unique stable spawn IDs:
duplicate export row keys:
duplicate persistence/runtime/save/stable IDs:

MoonCore authoritative key accepted:
CassiaSap authoritative key accepted:
StarNuruk authoritative key accepted:
legacy short keys accepted:
missing required CoreResource key failure probes:

CSV material header columns:
CSV material row count:
CSV material LF normalized:
CSV material UTF-8 no BOM:
CSV material digest lower-hex SHA-256: YES
CSV material digest:
actual CSV file writes/reads: 0/0
Generated CSV files committed: 0

debug snapshot sections:
Selection section present:
Occupied section present:
Budget section present:
RuntimeState section present:
Persistence section present:
debug snapshot upstream digest count:
debug snapshot digest lower-hex SHA-256: YES
debug snapshot digest:

special export surface digest lower-hex SHA-256: YES
special export surface digest:
MAP18_07 audit surface digest lower-hex SHA-256: YES
MAP18_07 audit surface digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing source failure probes:
digest mismatch failure probes:
legacy key failure probes:
duplicate row/id/key failure probes:
CSV shape failure probes:
attempted file/save/runtime side-effect failure probes:
atomic failure partial export rows:
atomic failure partial CSV material:
atomic failure partial debug snapshot:

actual save writes/reads: 0/0
PlayerPrefs writes/reads: 0/0
runtime SpecialRegion/Village/resource/Forge/Boss spawns: 0/0/0/0/0
runtime Activity/Event prefabs spawned: 0/0
actual event activations executed: 0
actual reward grants: 0
actual inventory/resource mutations: 0/0
actual damage executions: 0
enemy AI/controller hookups: 0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
disk save/load files created: 0/0
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
MAP18_07 started: NO
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
MapDesign/MCP/TASKS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_06 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes special state export summary
Result includes debug snapshot and file IO boundary notes
CoreResource, Forge, Boss, Village, ActivityEventRuntime, SpawnState export groups are represented
CoreResource authoritative persistence keys are used and legacy short keys are rejected
Generated spawn/state CSV material is deterministic and does not write files
selection/budget debug snapshot includes required sections and upstream digests
MAP18_05 runtime surface and MAP18_04 occupied/budget references are preserved
unique row keys, runtime IDs, save keys, stable spawn IDs
MAP18_07 audit surface is created
no actual CSV/save file write/read
no actual spawn, event execution, reward, inventory/resource mutation, damage, AI, physics, tilemap, collider, GameObject, or NavMesh work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_07 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG: COMPLETE
MAP18_07_MAP18_POPULATION_EXIT_TESTS: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_06: implement special state export and debug
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
