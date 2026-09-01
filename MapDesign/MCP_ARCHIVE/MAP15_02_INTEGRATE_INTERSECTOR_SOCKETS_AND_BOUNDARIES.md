```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
  task_file: TASKS/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES.md
  requires_current_task: NONE
  requires_completed_task: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
  requires_result:
    path: REPORTS/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER_RESULT.md
    status: PASS
    sha256: 7452984b7b75e94f07099381053c68859020ae44d78efc2c83b1b6c40ed38d8f
  requires_installed_task:
    path: TASKS/MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER.md
    sha256: 6e942509e2a459854554176d4235cb28d871c6cdd9914713a9c81895a1105676
  sets_current_task: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
```

# MAP15_02 - Integrate Intersector Sockets and Boundaries

```text
TASK: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01이 만든 169-sector world solve order 위에, 서로 이웃한 sector edge의 양쪽 socket, tile anchor, traversal apron, route signature, MAP08 boundary pair/profile/warning evidence를 확정한다.

```text
MAP15_01 WorldPlanInput + WorldSolveOrderResult
MAP14 sector-local external socket and fixed anchor public handoff
MAP08 boundary pair/profile/warning authority
MAP05 mandatory route and MAP09 RouteType/AccessClass contracts
-> WorldIntersectorEdgePlan
-> WorldBoundarySocketIntegrator
-> two-sided edge anchor/signature plan + digest
-> MAP15_03 multi-sector Special and cluster policy
```

이번 Task는 **sector와 sector 사이의 abstract edge contract**만 소유한다. 48x32 sector terrain canvas를 다시 만들지 않고, 624x416 Tilemap을 굽지 않고, Scene/Prefab/GameObject 또는 gameplay runtime에 반영하지 않는다.

MAP15_02가 승인해야 하는 핵심:

```text
13x13 world의 internal neighbor edge가 정확히 312개로 게시된다.
각 edge는 양쪽 sector-local socket anchor와 traversal apron signature를 가진다.
BoundaryPair edge는 MAP08의 승인 pair/profile/warning evidence와 연결된다.
Mandatory route와 external socket obligation은 양쪽에서 호환되는 open rule을 가진다.
불일치, 누락, 반대편 anchor 없음, boundary profile 누락은 partial payload 없이 실패한다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, internal edge/socket/boundary 수치, digest, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 13x13 internal neighbor edge inventory | full 169-sector terrain solve |
| two-sided sector-local socket anchor contract | Tilemap bake |
| traversal apron and edge signature model | MicroChunk 12x8 slice/streaming |
| MAP08 boundary pair/profile/warning binding | collider/physics/player traversal |
| RouteType/AccessClass socket compatibility check | Scene/Prefab/GameObject mutation |
| deterministic intersector edge digest | Activity/Event/NPC/reward gameplay spawn |
| atomic invalid edge failure reasons | multi-sector Special transaction policy |
| focused EditMode tests for MAP15_02 | MAP15 phase exit / batch seed approval |

`WorldIntersectorEdgePlan`은 sector 사이의 연결 계약이다. 실제 지형을 파거나 보정 통로를 만들지 않는다. 경로가 안 맞으면 이 Task는 데이터를 고치지 않고 typed failure를 반환해야 한다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_02`만 선택한다.

```text
MAP15_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_02` category로 제한한다.

신규 task-owned failure는 신규 MAP15_02 allowlist 파일만 수정하고 `MAP15_02` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01 solve order contradiction, MAP08 boundary authority contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_01 Result: PASS
MAP15_01 Result SHA-256:
7452984b7b75e94f07099381053c68859020ae44d78efc2c83b1b6c40ed38d8f

MAP15_01 installed Task SHA-256:
6e942509e2a459854554176d4235cb28d871c6cdd9914713a9c81895a1105676

MAP15_01 COMPLETE / MAP15_02 CURRENT / MAP15_03 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP00~02: world size 624x416, sector size 48x32, 13x13 topology, neighbor direction contract
MAP05: mandatory route graph and required route edge facts when exposed
MAP08: approved six boundary pairs, boundary candidate/profile/warning identity
MAP09: RouteType, AccessClass, boundary warning and pass ownership contracts
MAP13: SpecialRegion reservation identity exposed through MAP14/MAP15 handoff
MAP14: sector-local external socket, fixed anchor, traversal envelope and debug handoff
MAP15_01: WorldPlanInput, WorldSolveOrderResult, WorldDependencyKind and stable solve order
```

MAP15_02 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_02 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldIntersectorEdgePlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegrator.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegratorTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_02
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_02 책임 안에 머물러야 한다.

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

## 5. Model Contract - WorldIntersectorEdgePlan.cs

Create immutable value types for the MAP15_02 public surface.

Required concepts:

```text
WorldIntersectorEdgeId
WorldSectorSide
WorldEdgeOrientation
WorldEdgeEndpoint
WorldSocketAnchor
WorldTraversalApron
WorldBoundaryBinding
WorldEdgeRouteSignature
WorldIntersectorEdge
WorldIntersectorEdgePlan
WorldIntersectorFailure
WorldIntersectorDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
internal edge count = 312
two endpoints per internal edge
endpoint sector id and sector-local side
endpoint local tile anchor coordinate
anchor span or aperture size
traversal apron cells or canonical apron bounds
route type and access class compatibility
boundary pair id when the edge is a boundary
boundary profile id and warning modality evidence when boundary pair exists
edge signature lower-hex digest
plan input digest and output digest lower-hex SHA-256
mutation proof counters
downstream owner MAP15_03
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Integrator Contract - WorldBoundarySocketIntegrator.cs

Implement a deterministic integrator that builds intersector edge contracts without mutating MAP14 or MAP15_01 artifacts.

Required behavior:

1. Build all internal neighbor edges for a 13x13 grid:

```text
horizontal edges = 12 * 13 = 156
vertical edges = 13 * 12 = 156
total internal edges = 312
```

2. For each internal edge, create exactly two endpoints:

```text
west/east pair for horizontal neighbor edges
south/north pair for vertical neighbor edges
```

3. Each endpoint must contain a sector-local tile anchor in the approved 48x32 sector frame. The anchor must be on the matching side boundary.

4. Each edge must publish a traversal apron/signature. This is a contract for later rendering and validation, not a tile write.

5. BoundaryPair edges must bind to MAP08-approved pair/profile/warning evidence:

```text
approved pair id
orientation
profile id or candidate id
warning modalities
source owner
```

6. Boundary warning evidence must satisfy the MAP08 rule:

```text
Tile / Background / Resource / Audio 중 최소 2종
```

7. Route/socket compatibility must preserve approved semantics:

```text
Type1: L/R route opening
Type2: L/R/D route opening
Type3: L/R/U route opening
Type4: U/D mandatory, L/R only when explicit mask permits
Type0: no required continuity unless explicit socket evidence exists
Mandatory route edge: both endpoints must expose compatible open side
OptionalTool edge: may require AccessClass OptionalTool but cannot block mandatory route
```

8. Produce stable canonical digest:

```text
input: MAP15_01 input/output digest + MAP14 handoff digest + boundary authority digest
output: all edge endpoint facts + boundary bindings + route signatures
```

9. Fail atomically with no partial `WorldIntersectorEdgePlan` when:

```text
world topology is not 13x13 or internal edge count is not 312
edge references a missing sector
endpoint side does not face its counterpart
endpoint anchor is outside 48x32 or not on the side boundary
an internal edge has fewer or more than two endpoints
mandatory route edge lacks compatible two-sided openings
BoundaryPair edge lacks approved pair/profile/warning evidence
warning modality count is below 2
duplicate edge id or duplicate opposite endpoint pair exists
input digest is missing or not lower-hex SHA-256
integrator would need to carve fallback corridor or mutate sector planner output
```

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP00~15_01. Do not invent production boundary or route data when public data exists.

If some downstream-specific full-world edge facts are still not exposed, use deterministic `REFERENCE INTERSECTOR EDGE PLAN` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
13x13 topology with 312 internal edges
MAP15_01 reference solve order and dependency facts
approved MAP08 pair identity copied from public constants or MAP14/MAP15 handoff
representative Type0~Type4 socket rules
synthetic invalid edge cases for atomic failure tests
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

Create `WorldBoundarySocketIntegratorTests.cs` with category `MAP15_02`.

Required focused gates:

```text
WorldIntersectorPlanPublishesExact312InternalEdgesAndDigests
EveryInternalEdgeHasTwoFacingEndpointsAndSideAnchors
BoundaryPairsBindApprovedProfilesAndWarningEvidence
MandatoryRouteAndExternalSocketEdgesHaveCompatibleOpenings
Type4AndType0SocketRulesPreserveApprovedSemantics
TraversalApronsAndEdgeSignaturesAreStableAndNonEmpty
IntersectorIntegrationIsDeterministicAcrossRepeatReverseAndCulture
InvalidEdgeInputsFailAtomicallyWithoutPartialPlan
WorldEdgePlanDoesNotMutateSectorPlannerWorldPlanOrAuthoringAssets
Map15HandoffKeepsMap15_03Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
internal edges: 312/312
horizontal edges: 156/156
vertical edges: 156/156
edge endpoints: 624/624
endpoint anchors on matching side: 624/624
endpoint anchor out-of-bounds: 0
duplicate edge ids: 0
duplicate endpoint pairs: 0
missing counterpart endpoints: 0
route/socket incompatible edges: 0
BoundaryPair required/covered/missing: actual/actual/0
Boundary warnings with >=2 modalities: actual/actual
traversal apron missing: 0
empty edge signature: 0
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
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
row-major sector id ordering
edge id sorted by min sector, max sector, orientation
endpoint sorted by sector id then side
no Dictionary iteration order dependency
no current time
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing seed-like input may change only declared seed/retry fields. It must not change public topology constants, MAP15_01 solve order digest, or MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_02 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP09~14 authoring CSV/cache
Generated CSV files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The integrator may allocate in-memory immutable values. No generated debug file export is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP15_02: COMPLETE ELIGIBLE only when PASS
MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 intersector socket/boundary edge contract이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- 312 internal edge, 624 endpoint, horizontal/vertical actual count
- endpoint anchor/on-side/out-of-bounds count
- route/socket compatibility count and violation 0
- BoundaryPair/profile/warning required/covered/missing count
- traversal apron/signature count
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
- downstream owner: MAP15_03

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_02]
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
Commit subject: MAP15_02: integrate intersector sockets and boundaries
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_03.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES.md
MCP_ARCHIVE/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES.md
MCP/REPORTS/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldIntersectorEdgePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldIntersectorEdgePlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegrator.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegrator.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldBoundarySocketIntegratorTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_03: do not start
STOP after Result and optional PASS finalize commit
```
