```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
  task_file: TASKS/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER.md
  requires_current_task: NONE
  requires_completed_task: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: 419d3aa44e49ad1eb053e449cb781d4a777502a7270b4b198992064ab837917e
  requires_installed_task:
    path: TASKS/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS.md
    sha256: cb7d2e2e35d0c01f8d1b532aedd3dca2bf88b17553106960e14b9eba0fc7ceb7
  sets_current_task: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
```

# MAP15_01 - Implement World Plan and Solve Order

```text
TASK: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14가 승인한 sector-local planner를 13x13 world graph 위에서 어떤 순서로 소비할지 결정하는 첫 MAP15 계층을 만든다.

```text
MAP00~08 approved 13x13 world graph, route, site, biome, boundary authority
MAP09 layer/pass/access contracts
MAP13 SpecialRegion reservation authority
MAP14 phase-exit approved sector planner public handoff
-> WorldPlanInput
-> WorldPlanDependencyGraph
-> WorldSolveOrderPlanner
-> immutable WorldSolveOrderResult + digest
-> MAP15_02 inter-sector socket/boundary integration
```

이번 Task는 **169 sectors의 abstract solve order**만 소유한다. 각 sector의 실제 48x32 canvas를 다시 렌더링하거나, 169개 sector를 Tilemap으로 굽거나, Scene/Prefab/GameObject에 반영하지 않는다.

MAP15_01이 승인해야 하는 핵심:

```text
169 sector node가 정확히 1번씩 계획된다.
Special/Route/Boundary/neighbor dependency가 순서에 반영된다.
입력 digest와 output digest가 결정론적으로 고정된다.
cycle, duplicate, missing dependency, forbidden retry widening은 partial payload 없이 실패한다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, 169-sector 수치, dependency/priority/retry envelope 수치, digest, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 13x13 / 169-sector immutable world plan input snapshot | Tilemap bake |
| world sector node identity, coordinate, stable ordering | MicroChunk 12x8 slice/streaming |
| world-level dependency graph model | collider/physics/player traversal |
| deterministic solve order and priority reasons | Scene/Prefab/GameObject mutation |
| Special/Route/Boundary/neighbor constraint priority | production gameplay spawn |
| retry budget envelope and typed atomic failure reasons | Activity/Event/NPC/reward/combat/crafting/inventory runtime |
| input/output stable digest | MAP15_02 inter-sector socket/boundary resolution |
| focused EditMode tests for MAP15_01 | MAP15 phase exit / batch seed approval |

`WorldSolveOrder`는 sector를 푸는 순서와 dependency contract다. `SectorPlanner`의 내부 알고리즘을 바꾸거나 169개 sector의 final terrain canvas를 생성하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_01`만 선택한다.

```text
MAP15_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_01` category로 제한한다.

신규 task-owned failure는 신규 MAP15_01 allowlist 파일만 수정하고 `MAP15_01` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP14 phase-exit handoff contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_10 Result: PASS
MAP14_10 Result SHA-256:
419d3aa44e49ad1eb053e449cb781d4a777502a7270b4b198992064ab837917e

MAP14_10 installed Task SHA-256:
cb7d2e2e35d0c01f8d1b532aedd3dca2bf88b17553106960e14b9eba0fc7ceb7

MAP14 PHASE EXIT: APPROVED
MAP14_10 COMPLETE / MAP15_01 CURRENT / MAP15_02 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP00~02: world size 624x416, sector size 48x32, 13x13 topology, stable sector coordinate contract
MAP03: site reservation identity and fixed/special site position authority
MAP04: biome patch ownership and per-sector biome facts
MAP05: mandatory route graph identity and route dependency facts
MAP06~07: optional/access facts exposed through approved public contracts when available
MAP08: approved six boundary pairs, boundary candidate/profile/warning authority
MAP09: V2 pass order, AccessClass, PacingRole and ownership contracts
MAP10: MicroPattern availability summary exposed through MAP14 handoff only
MAP11: TerrainCluster availability summary exposed through MAP14 handoff only
MAP12: Activity/Event availability summary exposed through MAP14 handoff only
MAP13: SpecialRegion fixed/deferred reservation authority
MAP14: phase-exit approved sector-planner handoff, debug digest and graybox proof
```

MAP15_01 must consume public values. Do not reparse physical CSV unless an already approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_01 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSectorPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSolveOrderPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSolveOrderPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_01
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_01 책임 안에 머물러야 한다.

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
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - WorldSectorPlan.cs

Create immutable value types for the MAP15_01 public surface.

Required concepts:

```text
WorldPlanInput
WorldSectorNode
WorldSectorId
WorldSectorCoordinate
WorldDependencyEdge
WorldDependencyKind
WorldSolvePriority
WorldSolveStep
WorldSolveOrderResult
WorldSolveFailure
WorldSolveDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
WorldWidthTiles = 624
WorldHeightTiles = 416
SectorWidthTiles = 48
SectorHeightTiles = 32
SectorColumns = 13
SectorRows = 13
SectorCount = 169
node identity: stable row-major sector id and coordinate
node facts: primary biome, route type, access class, pacing role, special reservation flag
dependency edge: from sector, to sector, kind, reason, source owner
solve step: step index, sector id, priority, prerequisite sector ids, reason digest
input digest: lower-hex SHA-256 over canonical input facts
output digest: lower-hex SHA-256 over canonical solve order and dependency facts
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Planner Contract - WorldSolveOrderPlanner.cs

Implement a deterministic planner that builds a world-level solve order without mutating MAP14 sector-local artifacts.

Required behavior:

1. Build exactly `169` sector nodes from the approved world coordinate contract.
2. Attach public facts for route, biome, access, pacing, boundary and Special reservation when available.
3. Build dependency edges with typed reasons:

```text
SpecialReservation
MandatoryRoute
BoundaryPair
ExternalSocket
NeighborContinuity
PacingWindow
RetryGuard
```

4. Produce a stable acyclic ordering. Dependencies must be solved before dependents.
5. Prioritize highly constrained sectors before unconstrained filler sectors:

```text
1. fixed SpecialRegion sectors and their entry/return sectors
2. mandatory route and boundary pair sectors
3. sectors with external socket obligations
4. pacing/landmark/resource sectors
5. ordinary terrain/quiet filler sectors
```

6. Preserve deterministic tie-breaks:

```text
priority rank
dependency count descending
route/access/special stable key
sector id ascending
```

7. Retry is represented as an envelope only:

```text
max sector-local attempts per node
dependency rollback radius declaration
typed abort reason
no execution of MAP14 local retry loop
new RNG draw count 0
```

8. Fail atomically with no partial `WorldSolveOrderResult` when:

```text
sector count != 169
duplicate sector id or coordinate
out-of-bounds coordinate
self dependency
dependency references missing sector
cycle exists
Special/Boundary/Mandatory route dependency is missing
retry envelope would require whole-world rerandom
```

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP00~14. Do not invent production world data when public data exists.

If some downstream-specific 169-sector facts are still not exposed, use deterministic `REFERENCE WORLD PLAN` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
13x13 topology with 169 nodes
approved route/boundary/Special identity copied from public constants or MAP14 handoff
synthetic dependency edge cases for failure tests
deterministic retry envelope examples
```

Forbidden fixture claims:

```text
production seed approval
actual full world terrain solve
actual Tilemap output
player traversal proof
MAP15 phase exit approval
```

## 8. Focused Test Requirements

Create `WorldSolveOrderPlannerTests.cs` with category `MAP15_01`.

Required focused gates:

```text
WorldPlanInputPublishesExact169SectorTopologyAndDigests
SolveOrderContainsEachSectorExactlyOnce
DependencyGraphIsAcyclicAndPrerequisitesPrecedeDependents
SpecialRouteBoundaryConstraintsHavePriorityReasons
SolveOrderIsDeterministicAcrossRepeatReverseAndCulture
RetryEnvelopeDoesNotExecuteRngOrWholeWorldRerandom
InvalidWorldInputsFailAtomicallyWithoutPartialPlan
WorldPlanDoesNotMutateSectorPlannerOrAuthoringAssets
Map15HandoffKeepsMap15_02Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector nodes: 169/169
unique sector ids: 169/169
unique coordinates: 169/169
out-of-bounds sectors: 0
duplicate nodes: 0
dependency edges by kind: actual counts
missing required dependencies: 0
cycles: 0
solve steps: 169/169
unsolved sectors: 0
prerequisite order violations: 0
first constrained sector rank evidence: actual first N labels
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
whole-world rerandom: 0
fallback carve: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
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
row-major sector id ordering unless topological order is explicitly tested
no Dictionary iteration order dependency
no current time
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing seed-like input may change only declared seed/retry fields. It must not change public topology constants or existing MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_01 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP09~14 authoring CSV/cache
Generated CSV files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The planner may allocate in-memory immutable values. No generated debug file export is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
STATUS: PASS | FAIL | BLOCKED
MAP15_01: COMPLETE ELIGIBLE only when PASS
MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 169-sector world solve order 계약이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- 169-sector topology actual count
- dependency kind별 actual count and missing 0
- solve order count, duplicate 0, cycle 0, prerequisite violation 0
- Special/Route/Boundary priority evidence
- retry envelope, RNG draw 0, whole-world rerandom 0
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
- downstream owner: MAP15_02

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_01]
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
Commit subject: MAP15_01: implement world plan solve order
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_02.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER.md
MCP_ARCHIVE/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER.md
MCP/REPORTS/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSectorPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSectorPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSolveOrderPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSolveOrderPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSolveOrderPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSolveOrderPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_02: do not start
STOP after Result and optional PASS finalize commit
```
