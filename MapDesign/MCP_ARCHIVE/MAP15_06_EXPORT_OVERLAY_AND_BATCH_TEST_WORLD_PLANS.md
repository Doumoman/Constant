```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
  task_file: TASKS/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS.md
  requires_current_task: NONE
  requires_completed_task: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
  requires_result:
    path: REPORTS/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT_RESULT.md
    status: PASS
    sha256: fa409cc525e2755e990d7ca444cb165f0fb86d2ae43100ab119348f3e54b7cee
  requires_installed_task:
    path: TASKS/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT.md
    sha256: e5b401d922dc5c1af4ce3152bc9f07499e0dbf37fdbbb34359a7eda26ee040b6
  sets_current_task: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
```

# MAP15_06 - Export Overlay and Batch Test World Plans

```text
TASK: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01~05가 만든 world solve order, intersector edge, reservation, pacing-density-repetition, rollback/failure report를 사람이 읽을 수 있는 deterministic overlay export와 focused batch world-plan report로 묶는다.

```text
MAP15_01 WorldPlanInput + WorldSolveOrderResult
MAP15_02 WorldIntersectorEdgePlan
MAP15_03 WorldMultiSectorReservationPlan
MAP15_04 WorldPacingDensityPlan
MAP15_05 WorldNeighborRollbackPlan
-> WorldAssemblyOverlayExport
-> WorldAssemblyOverlayExporter
-> WorldBatchPlanReport
-> MAP15_07 world assembly exit audit input
```

이번 Task의 "export"는 Runtime immutable model과 deterministic text/grid payload를 **메모리 안에서** 만드는 것이다. 디스크 파일, Generated asset, EditorWindow, overlay UI, Scene, Prefab, Tilemap 또는 GameObject를 만들지 않는다.

이번 Task는 MAP15 phase exit approval이 아니다. production seed 승인, 실제 full-world terrain solve, 624x416 Tilemap bake, MicroChunk 12x8 slice/streaming, collider/physics/player traversal, Activity/Event/NPC/reward runtime spawn은 여전히 잠겨 있다. MAP15_06은 MAP15_07이 확인할 수 있도록 169-sector 계획과 실패 containment를 설명 가능한 형태로 준비하는 단계다.

MAP15_06이 승인해야 하는 핵심:

```text
169 sector / 312 internal edge / reservation / pacing / rollback facts가 한 overlay export에 모인다.
overlay sector와 edge token은 row-major와 edge-id 기준으로 결정론적이다.
hash chain은 MAP15_01~05 public digest를 잃지 않고 lower-hex SHA-256로 다시 묶인다.
focused batch world plans는 graph, reservation, pacing-density, rollback, solver bound를 검증한다.
batch는 production seed approval이나 full regression이 아니며 MAP15_07을 열지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, overlay sector/edge/layer/hash 수치, batch case 수치, graph/reservation/pacing/rollback/solver bound 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| deterministic world overlay export model | MAP15 phase exit approval |
| 169-sector row-major overlay sector tokens | actual full-world terrain solve |
| 312-edge overlay edge tokens and boundary summary | Tilemap bake |
| reservation/pacing/rollback/failure evidence summary | MicroChunk 12x8 slice/streaming |
| public digest chain and overlay digest | collider/physics/player traversal |
| focused abstract batch world-plan report | Scene/Prefab/GameObject mutation |
| graph/reservation/pacing/rollback/solver bound verdicts | Activity/Event/NPC/reward gameplay spawn |
| no-file-export/no-mutation proof | production seed approval |
| focused EditMode tests for MAP15_06 | MAP15_07 exit audit execution |

`WorldAssemblyOverlayExport`는 world assembly chain을 설명하는 read-only packet이다. 실제 world state를 다시 풀거나, sector candidate를 reroll하거나, terrain carve/repair를 실행하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_06`만 선택한다.

```text
MAP15_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01/MAP15_02/MAP15_03/MAP15_04/MAP15_05 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_06` category로 제한한다.

신규 task-owned failure는 신규 MAP15_06 allowlist 파일만 수정하고 `MAP15_06` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01 solve-order contradiction, MAP15_02 edge contradiction, MAP15_03 reservation contradiction, MAP15_04 pacing-density contradiction, MAP15_05 rollback contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_05 Result: PASS
MAP15_05 Result SHA-256:
fa409cc525e2755e990d7ca444cb165f0fb86d2ae43100ab119348f3e54b7cee

MAP15_05 installed Task SHA-256:
e5b401d922dc5c1af4ce3152bc9f07499e0dbf37fdbbb34359a7eda26ee040b6

MAP15_05 COMPLETE / MAP15_06 CURRENT / MAP15_07 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14: sector-local debug/retry handoff identity and no-fallback contract
MAP15_01: WorldPlanInput, WorldSolveOrderResult, sector coordinate, topology and solve-step order
MAP15_02: WorldIntersectorEdgePlan, edge id, endpoint, socket, boundary and route signature
MAP15_03: WorldMultiSectorReservationPlan, Special transactions, claims, edge locks and conflicts
MAP15_04: WorldPacingDensityPlan, windows, budgets, signatures, cap and violation evidence
MAP15_05: WorldNeighborRollbackPlan, rollback scope, failure report, decision and digest
```

MAP15_06 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_06 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

Do not create generated debug files. The only user-facing persisted file from this work is the normal `*_RESULT.md` written by the MCP workflow.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExport.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExporter.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldAssemblyOverlayBatchTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_06
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_06 책임 안에 머물러야 한다.

수정·생성 금지:

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/* existing files
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/*
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/*
Assets/_Game/Map/Runtime/WorldGeneration/Activities/*
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/*
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/*
Assets/_Game/Map/Runtime/WorldGeneration/Baking/*
Assets/_Game/Map/Runtime/WorldGeneration/RuntimeState/*
Assets/_Game/Map/Data/WorldGeneration/**
Assets/_Game/Editor/**
Assets/_Game/Tests/PlayMode/**
Scenes / Prefabs / Tilemaps / ScriptableObjects
asmdef / asmref / ProjectSettings / Packages
generated debug files, JSON files, CSV files, textures, screenshots
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - WorldAssemblyOverlayExport.cs

Create immutable value types for the MAP15_06 public surface.

Required concepts:

```text
WorldAssemblyOverlayLayerKind
WorldAssemblyOverlayTokenKind
WorldAssemblyOverlaySeverity
WorldAssemblyOverlaySector
WorldAssemblyOverlayEdge
WorldAssemblyOverlayLayer
WorldAssemblyHashRecord
WorldAssemblyOverlayExport
WorldBatchPlanCase
WorldSolverUpperBound
WorldBatchPlanReport
WorldAssemblyOverlayFailure
WorldAssemblyOverlayResult
WorldAssemblyOverlayDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
world dimensions: 624x416
sector dimensions: 48x32
sector grid: 13x13
world sector count: 169
internal edge count: 312
overlay sector count: 169
overlay edge count: 312
overlay layer kinds:
  Topology
  SolveOrder
  IntersectorEdges
  BoundaryPairs
  SpecialReservations
  ClusterReservations
  PacingDensity
  ActivityEventCaps
  RollbackScopes
  FailureReports
  HashChain
  MutationProof
hash records:
  MAP15_01 input/output digest
  MAP15_02 input/output digest
  MAP15_03 input/output digest
  MAP15_04 input/output digest
  MAP15_05 input/output digest
  MAP15_06 input/output digest
batch cases and verdicts
solver upper-bound actual/limit verdicts
no production approval flag
no generated file export flag
no full regression flag
downstream owner MAP15_07
```

Overlay sectors must include stable public identity:

```text
sector id
coordinate
solve step index
route/access/pacing summary when public
special/reservation marker count
pacing window/budget marker count
rollback marker count
failure marker count
token string stable under repeat/reverse/culture
```

Overlay edges must include stable public identity:

```text
edge id
endpoint sector ids
orientation
socket/boundary/mandatory/external summary when public
edge hash/signature
token string stable under repeat/reverse/culture
```

Batch cases must be labeled as focused abstract cases, not production seeds.

Required starter batch labels:

```text
REFERENCE_WORLD_BASELINE
REFERENCE_WORLD_BOUNDARY_HEAVY
REFERENCE_WORLD_SPECIAL_RESERVATION
REFERENCE_WORLD_ROLLBACK_FAILURE
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Exporter Contract - WorldAssemblyOverlayExporter.cs

Implement a deterministic exporter that builds an overlay and focused batch report without mutating MAP14 or MAP15_01~05 artifacts.

Required behavior:

1. Consume successful MAP15_01 world solve order, MAP15_02 edge plan, MAP15_03 reservation plan, MAP15_04 pacing-density plan and MAP15_05 rollback plan.
2. Publish exactly 169 overlay sector summaries and exactly 312 overlay edge summaries.
3. Build overlay layers for topology, solve order, intersector edges, boundary pairs, reservation, pacing-density, activity/event caps, rollback scopes, failure reports, hash chain and mutation proof.
4. Preserve upstream digest chain in explicit `WorldAssemblyHashRecord` values.
5. Create focused batch cases from deterministic test-owned labels. Batch cases may vary publication labels and synthetic observation fixtures only where that variation is explicitly reported. They may not claim production seed approval.
6. For each batch case, validate graph/reservation/pacing/rollback/solver bounds:

```text
world sector count == 169
internal edge count == 312
overlay sector count == 169
overlay edge count == 312
graph connected component count == 1 when public topology facts allow it
duplicate sector/edge ids == 0
missing boundary required pair == 0
reservation conflict not already typed == 0
pacing-density violation in accepted case == 0
rollback scope max <= 9
whole-world rerandom == 0
fallback carve == 0
silent widening == 0
generated file write == 0
Tilemap/Scene/Prefab/GameObject mutation == 0
production seed approval == 0
```

7. Publish solver upper-bound records with actual/limit/verdict. Minimum required upper bounds:

```text
solve steps <= 169
internal edges <= 312
edge endpoints <= 624
rollback sectors per failure <= 9
sector-local retry attempts <= 6 when public MAP15_01/MAP14 cap is available
whole-world rerandom actions <= 0
fallback carve actions <= 0
silent widening actions <= 0
file writes <= 0
Scene/Prefab/Tilemap/GameObject mutations <= 0
```

8. Produce stable canonical digest:

```text
input: MAP15_01 digest + MAP15_02 digest + MAP15_03 digest + MAP15_04 digest + MAP15_05 digest + batch labels + export request flags
output: sorted overlay sectors + sorted overlay edges + layers + hash records + batch verdicts + upper-bound records + counters
```

9. Fail atomically with no partial `WorldAssemblyOverlayExport` when:

```text
MAP15_01, MAP15_02, MAP15_03, MAP15_04 or MAP15_05 input/result is missing or failed
world sector count != 169
internal edge count != 312
overlay sector count would not equal 169
overlay edge count would not equal 312
upstream digest is missing or not lower-hex SHA-256
required layer would be empty without explicit unavailable reason
batch label is missing, duplicate, or claims production approval
solver upper-bound is exceeded
input/output digest is missing or not lower-hex SHA-256
export would require filesystem write, generated asset, Scene/Prefab/Tilemap/GameObject mutation, rerender, reroll, fallback carve, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP15_01~05. Do not invent production world data when public data exists.

If downstream-specific batch seed facts are still not exposed, use deterministic `REFERENCE WORLD ASSEMBLY BATCH` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
four starter abstract batch labels
synthetic boundary-heavy label tied to public MAP15_02 edge facts
synthetic Special reservation label tied to public MAP15_03 reservation facts
synthetic rollback failure label tied to public MAP15_05 rollback facts
synthetic invalid layer/bound cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
starter batch phase exit approval
actual full-world terrain solve
actual Tilemap output
real rollback execution
player traversal proof
Activity/Event runtime spawn
MAP15 phase exit approval
```

## 8. Focused Test Requirements

Create `WorldAssemblyOverlayBatchTests.cs` with category `MAP15_06`.

Required focused gates:

```text
OverlayExportPublishesTopologyEdgesReservationsPacingRollbackAndDigests
OverlayLayersCoverPlacementBoundarySpecialPacingActivityRollbackAndFailureEvidence
OverlayTokensAreStableRowMajorAndEdgeIdOrdered
BatchWorldPlansUseFourFocusedReferenceLabelsWithoutProductionApproval
BatchReportValidatesGraphReservationPacingRollbackAndSolverBounds
SolverUpperBoundReportRejectsLimitOverrunRerandomFallbackCarveAndSilentWidening
OverlayExportIsDeterministicAcrossRepeatReverseAndCulture
InvalidOverlayInputsFailAtomicallyWithoutPartialExport
OverlayBatchDoesNotMutateWorldPlansAuthoringFilesTilesScenesOrGameplayObjects
Map15HandoffKeepsMap15_07Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
world sectors observed: 169/169
internal edges observed: 312/312
overlay sectors: 169/169
overlay edges: 312/312
overlay layer kinds required/covered/missing: 12/12/0
hash records required/covered/missing: 10/10/0
batch cases required/covered/missing: 4/4/0
batch graph verdict pass: 4/4
batch reservation verdict pass: actual/actual
batch pacing-density verdict pass: actual/actual
batch rollback verdict pass: actual/actual
solver upper-bound records required/covered/missing: 10/10/0
upper-bound violations in accepted batch: 0
duplicate sector ids: 0
duplicate edge ids: 0
missing required boundary pairs: 0
untyped reservation conflicts: 0
accepted pacing-density violations: 0
rollback scope max exceeded: 0
whole-world rerandom actions: 0
fallback carve actions: 0
silent widening actions: 0
production seed approvals: 0
generated file writes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
```

Do not assert exact counts that depend on private or physical CSV internals. Assert exact counts only when they are public approved constants or produced by the new model itself.

## 9. Hash and Determinism Rules

All digest input must be canonical:

```text
UTF-8
LF newlines
InvariantCulture
stable enum names
stable lower-hex SHA-256
sector tokens sorted by row-major sector id
edge tokens sorted by public edge id
layer records sorted by layer kind then stable id
hash records sorted by owner task then record kind
batch cases sorted by label
upper-bound records sorted by bound kind then owner
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing batch label may change only declared synthetic evidence and batch verdict payload. It must not change public topology constants, MAP15_01 solve order digest, MAP15_02 edge plan digest, MAP15_03 reservation plan digest, MAP15_04 pacing-density digest, MAP15_05 rollback digest, or MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_06 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP15_02 intersector edge outputs
MAP15_03 reservation policy outputs
MAP15_04 pacing-density outputs
MAP15_05 rollback/failure report outputs
MAP09~14 authoring CSV/cache
Generated CSV files
debug export files
JSON files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The exporter may allocate in-memory immutable values. No generated file export and no actual batch terrain execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
STATUS: PASS | FAIL | BLOCKED
MAP15_06: COMPLETE ELIGIBLE only when PASS
MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 world overlay export/batch report contract이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- overlay sector/edge/layer/hash count
- batch case labels and counts
- graph/reservation/pacing/rollback/solver bound verdict counts
- upper-bound violation 0
- whole-world rerandom/fallback carve/silent widening 0
- production seed approval 0
- input/output digest
- deterministic replay evidence
- mutation/file-write/Scene/Prefab/Tilemap/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 아직 구현하지 않은 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script path
- class/method별 책임
- helper/probe별 input -> output
- public authority consumed
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP15_07

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_06]
discovered: <N>
executed: <N>
passed: <N>
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

If PASS:

```text
Commit subject: MAP15_06: export overlay and batch test world plans
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_07.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS.md
MCP_ARCHIVE/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS.md
MCP/REPORTS/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExport.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExport.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExporter.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExporter.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldAssemblyOverlayBatchTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldAssemblyOverlayBatchTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_07: do not start
STOP after Result and optional PASS finalize commit
```
