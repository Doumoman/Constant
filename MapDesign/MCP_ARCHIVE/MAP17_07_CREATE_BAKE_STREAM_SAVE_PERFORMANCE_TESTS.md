```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
  task_file: TASKS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
  requires_result:
    path: REPORTS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY_RESULT.md
    status: PASS
    sha256: de743b24661e061544e4d3e032d8fdaca399eb413429da542469d2ede7932968
  requires_installed_task:
    path: TASKS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY.md
    sha256: 52e97516e909f8c5580d6832b67eb1fdc206d85376b9b4f2cef12b65aae1619b
  sets_current_task: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
```

# MAP17_07 - Create Bake Stream Save Performance Tests

```text
TASK: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_08_MAP17_RUNTIME_EXIT_AUDIT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_01~MAP17_06에서 만든 pure-data pipeline을 대상으로, bake/stream/save 쪽 비용과 spike를 관찰하는 focused performance fixture와 report 계약을 만든다.

이번 Task는 새 맵 생성 기능이나 최적화 리팩토링이 아니다.  
목표는 다음이다.

```text
1. Position resolve, layer bake, seam validation의 처리량과 구조 count를 기록한다.
2. Collider cache cold/warm/invalidate/evict 경로의 비용과 rebuild count를 기록한다.
3. Streaming window center/edge/corner, transition batch, preactivation 경로의 상한을 기록한다.
4. Modification storage, save manifest serialize/parse, regeneration apply, hash mismatch failure 경로의 비용을 기록한다.
5. MAP17_08 exit audit이 사용할 deterministic performance report fixture를 만든다.
```

Performance test는 정확한 프레임 타이밍을 보증하는 기능이 아니다. Unity Editor/머신 상태에 따라 absolute milliseconds가 흔들릴 수 있으므로, PASS gate는 deterministic count, no side effect, no broad regression, no hidden retry loop, no full serialization 같은 구조적 상한을 중심으로 둔다. 시간값은 Result에 기록하되 지나치게 빡빡한 millisecond threshold로 flaky하게 만들지 않는다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
어떤 bake/stream/save 경로를 측정했는지
성능 측정이 실제 runtime 최적화나 디스크 저장과 어떻게 다른지
absolute ms보다 count/상한/determinism을 중심으로 검증한 이유
중복 코드나 하드코딩 후보를 발견했는지
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
MAP17_08에 넘기는 산출물
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## Performance Observation Summary
## Duplication and Hardcoding Observation
```

`Duplication and Hardcoding Observation`은 발견한 후보를 보고만 한다. 이번 Task 안에서 대규모 정리나 리팩토링을 시작하지 않는다.

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_06 Result exists
MAP17_06 Result STATUS: PASS
MAP17_06 Result SHA-256:
de743b24661e061544e4d3e032d8fdaca399eb413429da542469d2ede7932968

MAP17_06 installed task SHA-256:
52e97516e909f8c5580d6832b67eb1fdc206d85376b9b4f2cef12b65aae1619b

MAP17_06 manifest digest:
18bb9bd0ada73c2c84b9b400675d792a0e9c206f4ee5bd5eec897468154cd27a

MAP17_06 canonical payload digest:
af88b4751877d4a03b0854eefea089bab70c542717d676d8eb52655b67ebac04

MAP17_06 regeneration apply digest:
13a1d61f92382f05460e7bc5c39f75b39c8e24850918bd7b94e8ace330504568

Current Task before apply: NONE
MAP17_06: COMPLETE
MAP17_07: LOCKED before apply
MAP17_08: LOCKED
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
MAP17_05 focused fixture modified sectors: 1
MAP17_05 focused fixture modification records: 5
MAP17_06 manifest modified sector entries: 1
MAP17_06 manifest serialized modification records: 5
MAP17_06 unmodified sectors omitted: 168
```

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedTerrainPerformanceBudget` | count-based 상한과 diagnostic timing budget을 정의한다. |
| `GeneratedTerrainPerformanceSample` | operation name, iteration, count, elapsed ticks/ms, allocation note, deterministic digest를 담는다. |
| `GeneratedTerrainPerformanceReport` | bake/stream/save 측정 결과를 stable order로 묶고 MAP17_08에 넘길 digest를 만든다. |
| `GeneratedTerrainPerformanceHarness` | MAP17_01~06 public API를 fixture 입력으로 반복 실행해 sample/report를 만든다. |
| `GeneratedTerrainPerformanceFailure` | count 초과, side effect, retry storm, nondeterministic sample, unsupported path를 deterministic하게 보고한다. |

Suggested production file, only if shared by MAP17_08:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainPerformanceReport.cs(.meta)
```

Suggested focused test/support files:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainPerformanceHarness.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainPerformanceTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. 측정 규칙

### 5.1 Measurement mode

Performance fixture는 EditMode에서 pure-data로 실행한다.

필수:

```text
warmup iteration count recorded
measured iteration count recorded
min/median/max elapsed ms recorded per operation
operation count recorded per operation
report digest lower-hex SHA-256
stable repeat report digest
culture-independent formatting
stable operation order
no machine-specific file path in digest
```

금지:

```text
PlayMode performance test
frame-dependent assertion
Camera-dependent assertion
Unity scene object dependent assertion
machine absolute path in digest
timestamp/frame count in digest
strict flaky millisecond gate without structural reason
```

### 5.2 Required operation groups

다음 operation group을 report에 포함한다.

| Group | Required evidence |
|---|---|
| `placement` | sector cells 1536, layer refs 10752, world/sector coordinate count |
| `layer_bake` | logical layers 7, gap 0, overlap 0, stale asset 0 |
| `seam_validation` | 4x4 seam 688, 12x8 seam 240, 4x4-only seam 448 |
| `collider_cache` | cold miss, warm hit, invalidate, evict, rebuild command count |
| `stream_window` | center 49/25, edge 28/15, corner 16/9, active subset preload |
| `transition` | shifted-window diff and transition batch count, duplicate handle changes 0 |
| `modification_storage` | modified sectors 1, records 5, dirty revision 5, apply commands 5 |
| `save_manifest` | payload bytes, modified entries 1, unmodified entries 0, serialized records 5 |
| `regen_apply` | five modification kinds reapplied, in-place mutation 0 |
| `hash_mismatch` | atomic failure probes, partial apply mutations 0, hidden retry loops 0 |

### 5.3 Structural upper bounds

PASS gate는 아래 상한을 초과하지 않아야 한다.

```text
full 169-sector tile serialization as save data: 0
unmodified manifest sector entries: 0
Unity object ids serialized: 0
file paths/timestamps/frame counts serialized: 0/0/0
population/content spawn ids serialized: 0
hidden full generator executions for performance fixture: 0
automatic broad regression selections: 0
retry loops after deterministic hash mismatch: 0
Scene/Prefab/Tilemap mutations: 0/0/0
```

If a timing spike is observed, report it as evidence. Do not silently start optimization or broad regression. If the spike invalidates focused proof, stop as `BLOCKED` and explain the trigger.

### 5.4 Duplicate and hardcoding observation

Before adding performance code, inspect current MAP17 source/test helpers for reusable primitives.

Required observation:

```text
existing helpers reused:
new duplicate helper count:
hardcoded count constants added:
hardcoded count constants justified:
consolidation candidates observed:
consolidation work performed:
```

This Task may create a tiny performance report abstraction if it prevents duplicated timing/report code. It must not perform broad cleanup across MAP09~MAP16 or rewrite existing generator code.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
actual optimization rewrite
large refactor of generated terrain pipeline
CSV authoring edits
Generated CSV commits
System.IO file write/read for save or performance data
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
MAP17_08 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_07`만 선택한다.

```text
MAP17_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02/MAP17_03/MAP17_04/MAP17_05/MAP17_06 selections: 0
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
BakePlacementPerformanceReportsStableCellLayerAndCoordinateCounts
LayerBakeAndSeamPerformanceReportsExpectedCountsWithoutTilemapWrites
ColliderCachePerformanceSeparatesColdWarmInvalidateAndEvictPaths
StreamingWindowPerformanceReportsCenterEdgeCornerAndActiveSubsetBudgets
TransitionPerformancePublishesShiftedWindowDiffWithoutDuplicateHandleChanges
ModificationStoragePerformanceReportsDirtyRevisionCompactAndApplyCounts
SaveManifestReloadPerformanceSerializesModifiedOnlyAndAppliesFiveRecords
HashMismatchPerformanceFailsAtomicallyWithoutRetryStorm
PerformanceReportsAreDeterministicAcrossRepeatReverseCultureAndWarmup
Map17HandoffKeepsMap17_08Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_07]
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
MAP17_06 manifest digest reused:
MAP17_06 canonical payload digest reused:
MAP17_06 regeneration apply digest reused:

performance warmup iterations:
performance measured iterations:
performance operation groups reported:
performance report digest lower-hex SHA-256:
performance report digest:
repeat/reverse/culture/warmup digest mismatches: 0/0/0/0

placement cells measured:
placement layer refs measured:
layer bake logical layers measured:
layer bake gap/overlap/stale asset counts:
seam 4x4/12x8/4x4-only counts:
collider cache cold misses/warm hits/invalidates/evicts:
collider rebuild command count:
stream center preload/active:
stream edge preload/active:
stream corner preload/active:
stream active subset preload: YES
transition shifted-window diff count:
transition duplicate handle changes:
modification modified sectors/records/dirty revision:
modification apply command count:
save manifest payload bytes:
save manifest modified sector entries:
save manifest unmodified sector entries:
save manifest serialized records:
regen apply modified sector plans:
regen apply command count:
hash mismatch failure probes:
hash mismatch retry loops:
atomic failure partial mutations:

min/median/max ms placement:
min/median/max ms layer_bake:
min/median/max ms seam_validation:
min/median/max ms collider_cache:
min/median/max ms stream_window:
min/median/max ms transition:
min/median/max ms modification_storage:
min/median/max ms save_manifest:
min/median/max ms regen_apply:
min/median/max ms hash_mismatch:

existing helpers reused:
new duplicate helper count:
hardcoded count constants added:
hardcoded count constants justified:
consolidation candidates observed:
consolidation work performed:

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
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
runtime objects spawned: 0
production seed approvals: 0
MAP17_08 started: NO
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
MapDesign/MCP/TASKS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_07 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes performance observation summary
Result includes duplication/hardcoding observation
bake/stream/save performance report fixture created
structural count budgets and digests are deterministic
save/reload path remains in-memory manifest parse + regeneration apply only
no actual disk save/load file write/read
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP17_08 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS: COMPLETE
MAP17_08_MAP17_RUNTIME_EXIT_AUDIT: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_07: create bake stream save performance tests
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
