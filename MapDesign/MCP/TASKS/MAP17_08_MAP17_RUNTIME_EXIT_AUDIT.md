```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_08_MAP17_RUNTIME_EXIT_AUDIT
  task_file: TASKS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT.md
  requires_current_task: NONE
  requires_completed_task: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
  requires_result:
    path: REPORTS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS_RESULT.md
    status: PASS
    sha256: 072f2dcb59e34236e007e0760bc8f54974a99bc1ab3919d5822012bf8169b96b
  requires_installed_task:
    path: TASKS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS.md
    sha256: 935957fbc6e563fdda87a1121329c8d81a91951e27ee2f109069eb97d42ae658
  sets_current_task: MAP17_08_MAP17_RUNTIME_EXIT_AUDIT
```

# MAP17_08 - MAP17 Runtime Exit Audit

```text
TASK: MAP17_08_MAP17_RUNTIME_EXIT_AUDIT
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_01~MAP17_07에서 만든 runtime preparation contracts를 하나의 phase-exit report로 감사한다.

이번 Task는 새 생성기 기능, 실제 runtime integration, 최적화 rewrite가 아니다.  
목표는 다음이다.

```text
1. Asset resolution -> placement -> logical tilemap bake -> seam validation 계약이 이어지는지 확인한다.
2. Collider cache -> runtime handle -> preload/active/preactivation 계약이 이어지는지 확인한다.
3. Sector modification storage -> save manifest -> regeneration apply 계약이 이어지는지 확인한다.
4. MAP17_07 performance report의 구조 count, spike, duplicate/hardcoding observation을 exit risk로 분류한다.
5. MAP18_01이 population/content stable spawn ID 작업을 시작해도 되는지 phase gate를 판정한다.
```

중요한 범위 정정:

```text
MAP17은 live player movement, actual scene streaming, actual save file, population spawn을 소유하지 않는다.
따라서 MAP17_08은 PlayMode world traversal이나 실제 Tilemap/Collider/GameObject 조작을 수행하지 않는다.
이번 감사는 pure-data runtime readiness audit이다.
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
MAP17 전체가 실제로 무엇을 완성했는지
MAP17이 아직 소유하지 않는 runtime/scene/save/population 영역
MAP17_07의 layer_bake timing spike를 어떻게 exit risk로 분류했는지
중복 fixture adapter와 hardcoded constants를 어떻게 후속 후보로 남겼는지
MAP18_01로 넘어가도 되는 이유 또는 막힌 이유
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## MAP17 Phase Exit Decision
## Performance and Duplication Risk Review
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_07 Result exists
MAP17_07 Result STATUS: PASS
MAP17_07 Result SHA-256:
072f2dcb59e34236e007e0760bc8f54974a99bc1ab3919d5822012bf8169b96b

MAP17_07 installed task SHA-256:
935957fbc6e563fdda87a1121329c8d81a91951e27ee2f109069eb97d42ae658

MAP17_07 performance report digest:
c153ac3f76cb5aa64abeaad2c0091279a027de02c4a3817c9335e74b79cbce2f

MAP17_07 observed diagnostic spike:
layer_bake max 3358.202900 ms

MAP17_07 duplication observation:
new duplicate helper count 1
hardcoded count constants added 22
consolidation candidates observed 1
optimization rewrites performed 0
broad refactors performed 0

Current Task before apply: NONE
MAP17_07: COMPLETE
MAP17_08: LOCKED before apply
MAP18_01: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainAssetResolution
GeneratedCellPlacementPlan
GeneratedTilemapLayerBakePlan
GeneratedTilemapLayerBaker
GeneratedTilemapSeamValidation
GeneratedColliderCachePlan
GeneratedColliderCache
GeneratedSectorRuntimeHandle
GeneratedSectorRuntimeHandleLifecycle
GeneratedSectorStreamingWindow
GeneratedSectorPreactivationPolicy
GeneratedSectorWindowPlanner
GeneratedSectorModificationStorage
GeneratedSectorModificationStore
GeneratedWorldSaveManifest
GeneratedSaveManifestSerializer
GeneratedSectorRegenerationApplyPlan
GeneratedSaveManifestService
GeneratedTerrainPerformanceReport
```

기준 수량:

```text
world sectors: 13x13 = 169
sector size: 48x32 cells
sector local index range: 0..1535
logical layer count: 7
logical layer records per sector fixture: 10752
4x4 seam edges from MAP17_02 fixture: 688
12x8 seam edges from MAP17_02 fixture: 240
4x4-only seam edges from MAP17_02 fixture: 448
center preload/active window from MAP17_04: 49/25
corner preload/active window from MAP17_04: 16/9
edge preload/active window from MAP17_04: 28/15
MAP17_05 modified sectors: 1
MAP17_05 modification records: 5
MAP17_06 manifest modified sector entries: 1
MAP17_06 serialized modification records: 5
MAP17_06 unmodified sectors omitted: 168
MAP17_07 operation groups: 10
```

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedMap17ExitAuditItem` | audit key, owner task, expected/actual value, severity, pass/fail reason을 담는다. |
| `GeneratedMap17ExitAuditRisk` | performance spike, duplicate fixture, hardcoded constants, deferred ownership을 risk로 분류한다. |
| `GeneratedMap17ExitAuditReport` | MAP17 phase readiness, MAP18 handoff status, audit item/risk/digest를 stable order로 묶는다. |
| `GeneratedMap17ExitAuditService` | MAP17 public API와 performance report를 pure-data로 감사해 pass/block/fail verdict를 만든다. |

Suggested production file, only if MAP18 handoff needs a reusable report model:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMap17ExitAuditReport.cs(.meta)
```

Suggested focused test/support file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMap17RuntimeExitAuditTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Audit 규칙

### 5.1 Phase readiness audit

다음 항목은 모두 PASS여야 한다.

```text
asset reference resolution contains no Unity asset loading
placement covers 1536 sector cells and 10752 layer refs
logical bake produces 7 layers with gap 0, overlap 0, stale asset 0
seam validation reports 688/240/448 expected seams
collider cache exposes cold/warm/invalidate/evict paths
runtime handle lifecycle exposes Unloaded/Preloaded/Active/SleepingModified
streaming windows satisfy center 49/25, edge 28/15, corner 16/9
active window is subset of preload window
sector modifications cover 1 modified sector, 5 records, dirty revision 5
save manifest stores modified-only payload and omits 168 unmodified sectors
regeneration apply replays 5 modification records without input mutation
performance report has 10 operation groups and stable digest
hash mismatch failures remain atomic with retry loops 0
```

### 5.2 Deferred ownership audit

다음은 MAP17 완료의 실패가 아니다. 단, Result에서 후속 owner를 명확히 써야 한다.

| Deferred item | Owner |
|---|---|
| population/content stable spawn ID | MAP18_01 |
| mandatory/unique content placement | MAP18_02 |
| actual shop/resource/hazard/enemy population | MAP18_03~MAP18_04 |
| activity/event runtime state instantiation | MAP18_05 |
| special state export/debug | MAP18_06 |
| actual live player traversal proof | later PlayMode/live integration task, not MAP17 |
| actual disk save slot/platform storage | later save-system integration task, not MAP17 |
| optimization rewrite for observed spike | later optimization task only if approved |
| shared fixture consolidation | later cleanup task only if approved |

### 5.3 Performance risk audit

MAP17_07의 `layer_bake max 3358.202900 ms`는 반드시 분류한다.

Allowed risk classifications:

```text
INFO: diagnostic spike only, structural counts stable, no exit block
WARN: spike needs follow-up before production runtime integration, but MAP18 data ownership can proceed
BLOCKER: spike invalidates MAP17 focused proof and MAP18 must not start
```

`BLOCKER`로 판단하면 `STATUS: BLOCKED`로 멈추고 MAP18_01을 열지 않는다.

INFO/WARN로 판단하려면 다음 증거가 필요하다.

```text
repeat/reverse/culture/warmup digest mismatches: 0/0/0/0
strict millisecond PASS threshold: NONE
structural count mismatch: 0
side effect count: 0
hidden retry loops: 0
```

### 5.4 Duplication and hardcoding risk audit

MAP17_07의 중복/하드코딩 보고를 재분류한다.

Required:

```text
new duplicate helper count carried forward: 1
duplicate helper owner:
hardcoded count constants carried forward: 22
hardcoded constants are named budget constants: YES/NO
consolidation candidates carried forward:
consolidation blocks MAP18_01: YES/NO
cleanup/refactor performed in this task: 0
```

이번 Task는 observation을 exit risk로 분류만 한다. 대규모 정리나 리팩토링은 하지 않는다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
new map generation feature
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
population/content stable spawn ID generation
production seed approval
MAP18_01 unlock or execution before PASS finalize
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_08`만 선택한다.

```text
MAP17_08 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02/MAP17_03/MAP17_04/MAP17_05/MAP17_06/MAP17_07 selections: 0
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
Map17ExitAuditApprovesAssetPlacementBakeAndSeamContracts
Map17ExitAuditApprovesColliderHandleAndStreamingContracts
Map17ExitAuditApprovesModificationManifestAndRegenerationContracts
Map17ExitAuditClassifiesPerformanceSpikeWithoutOptimizationRewrite
Map17ExitAuditCarriesDuplicationAndHardcodingRisksWithoutCleanup
Map17ExitAuditRejectsMissingOrMismatchedUpstreamEvidenceAtomically
Map17ExitAuditReportsDeferredOwnershipForPopulationRuntimeAndDiskSave
Map17ExitAuditDigestIsStableAcrossRepeatReverseCultureAndRiskOrder
Map17ExitAuditDoesNotMutateScenesWriteFilesLoadAssetsOrRunRegressions
Map17HandoffKeepsMap18_01LockedUntilReviewedPass
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_08]
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
MAP17_07 performance report digest reused:
MAP17_07 layer_bake max ms reused:
MAP17_07 duplicate helper count reused:
MAP17_07 hardcoded count constants reused:

MAP17 phase exit verdict:
MAP18_01 handoff approved by audit: YES/NO
audit item count:
audit pass/warn/block/fail counts:
audit report digest lower-hex SHA-256:
audit report digest:
repeat/reverse/culture/risk-order digest mismatches: 0/0/0/0

asset resolution readiness: PASS/FAIL
placement cells/layer refs readiness:
logical layers/gap/overlap/stale readiness:
seam 4x4/12x8/4x4-only readiness:
collider cache readiness:
runtime handle lifecycle readiness:
stream center/edge/corner readiness:
active subset preload readiness:
modification storage readiness:
save manifest modified-only readiness:
regeneration apply readiness:
hash mismatch atomic readiness:
performance report readiness:

performance spike classification:
performance spike blocks MAP18_01: YES/NO
structural count mismatches:
side effect count:
hidden retry loops:
strict millisecond gate used: NO
optimization rewrites performed: 0

new duplicate helper count carried forward:
duplicate helper owner:
hardcoded count constants carried forward:
hardcoded constants are named budget constants:
consolidation candidates carried forward:
consolidation blocks MAP18_01:
cleanup/refactor performed in this task: 0

deferred population/content stable spawn ID owner:
deferred actual live traversal owner:
deferred actual disk save owner:
deferred optimization owner:
deferred fixture consolidation owner:

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
GameObject instantiate/enable/disable/destroy: 0/0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
runtime objects spawned: 0
production seed approvals: 0
MAP18_01 started: NO
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
MapDesign/MCP/TASKS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_08_MAP17_RUNTIME_EXIT_AUDIT_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_08 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes MAP17 phase exit decision
Result includes performance and duplication risk review
MAP17 phase exit verdict is PASS
MAP18_01 handoff approved by audit is YES
performance spike is classified INFO or WARN, not BLOCKER
duplication/hardcoding risks are carried forward without cleanup
deferred owners are explicitly reported
no actual disk save/load file write/read
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_01 remains LOCKED / NOT STARTED until this Result is reviewed
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_08_MAP17_RUNTIME_EXIT_AUDIT: COMPLETE
MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_08: audit runtime preparation exit
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
