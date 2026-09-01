```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
  task_file: TASKS/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY.md
  requires_current_task: NONE
  requires_completed_task: MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES
  requires_result:
    path: REPORTS/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES_RESULT.md
    status: PASS
    sha256: d7dfcef717d29f05ee1c66f4e9afe6c0b7a55716410680bf9e7bf482a6722660
  requires_installed_task:
    path: TASKS/MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES.md
    sha256: 116b056de902f7d429186e301ce15327192bf2ada5c82b9a1fc8bb4a4b976eb2
  sets_current_task: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
```

# MAP15_03 - Implement Multi-Sector Special and Cluster Policy

```text
TASK: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01 world solve order와 MAP15_02 intersector edge contract 위에, 2-sector Village/Special 예약과 TerrainCluster의 sector-contained/cross-sector allowlist 정책을 만든다.

```text
MAP15_01 WorldPlanInput + WorldSolveOrderResult
MAP15_02 WorldIntersectorEdgePlan
MAP13 SpecialRegion reservation authority
MAP11 TerrainCluster authoring/variant identity
MAP14 sector-local cluster/special handoff
-> WorldMultiSectorReservationPlan
-> WorldSpecialClusterPolicyPlanner
-> atomic Special transaction + cluster containment policy + digest
-> MAP15_04 world pacing/density/repetition
```

이번 Task는 **world-level reservation policy**만 소유한다. 실제 48x32 sector terrain canvas를 다시 렌더링하지 않고, 624x416 Tilemap을 굽지 않고, Scene/Prefab/GameObject 또는 gameplay runtime에 반영하지 않는다.

MAP15_03이 승인해야 하는 핵심:

```text
SpecialRegion은 필요할 때 여러 sector/edge를 atomic transaction으로 예약한다.
2-sector Village/Special은 MAP15_02의 adjacent intersector edge와 entry/return evidence를 가진다.
일반 TerrainCluster는 기본적으로 sector-contained다.
cross-sector TerrainCluster는 명시 allowlist가 있을 때만 허용된다.
Special reservation과 일반 cluster reservation은 우선순위와 conflict reason을 남기며 silent overwrite하지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, Special transaction 수치, cluster containment/allowlist 수치, conflict/atomic failure 수치, digest, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| multi-sector Special reservation transaction model | actual full-world terrain solve |
| 2-sector Village/Special adjacency and edge-lock policy | Tilemap bake |
| Special priority over normal cluster reservation | MicroChunk 12x8 slice/streaming |
| TerrainCluster sector-contained default rule | collider/physics/player traversal |
| explicit cross-sector cluster allowlist contract | Scene/Prefab/GameObject mutation |
| atomic conflict/failure reasons | Activity/Event/NPC/reward gameplay spawn |
| deterministic reservation policy digest | MAP15_04 pacing/density/repetition windows |
| focused EditMode tests for MAP15_03 | MAP15 phase exit / batch seed approval |

`WorldMultiSectorReservationPlan`은 어떤 sector/edge를 누가 예약할 수 있는지 정하는 계약이다. 실제 cluster geometry를 world에 굽거나 SpecialRegion 내부 시설/보상/전투를 실행하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_03`만 선택한다.

```text
MAP15_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01/MAP15_02 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_03` category로 제한한다.

신규 task-owned failure는 신규 MAP15_03 allowlist 파일만 수정하고 `MAP15_03` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01 solve order contradiction, MAP15_02 edge contract contradiction, MAP13 SpecialRegion contradiction, MAP11 TerrainCluster identity contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_02 Result: PASS
MAP15_02 Result SHA-256:
d7dfcef717d29f05ee1c66f4e9afe6c0b7a55716410680bf9e7bf482a6722660

MAP15_02 installed Task SHA-256:
116b056de902f7d429186e301ce15327192bf2ada5c82b9a1fc8bb4a4b976eb2

MAP15_02 COMPLETE / MAP15_03 CURRENT / MAP15_04 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP11: TerrainCluster catalog/variant/footprint identity and cross-sector capability only when publicly exposed
MAP13: SpecialRegion fixed/deferred identity, Village/CoreResource/Forge/Boss/Merchant/Maru reservation facts
MAP14: sector-local fixed Special anchors, cluster placement handoff and reservation labels
MAP15_01: WorldPlanInput, WorldSolveOrderResult, sector node ordering and dependency priority
MAP15_02: WorldIntersectorEdgePlan, edge id, endpoints, socket compatibility and boundary binding
```

MAP15_03 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_03 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldMultiSectorReservationPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_03
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_03 책임 안에 머물러야 한다.

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

## 5. Model Contract - WorldMultiSectorReservationPlan.cs

Create immutable value types for the MAP15_03 public surface.

Required concepts:

```text
WorldReservationOwnerKind
WorldReservationSpanKind
WorldReservationClaim
WorldReservationEdgeLock
WorldSpecialReservationTransaction
WorldClusterContainmentPolicy
WorldClusterCrossSectorAllowance
WorldReservationConflict
WorldMultiSectorReservationPlan
WorldReservationPolicyFailure
WorldReservationPolicyDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
world sector count inherited from MAP15_01 = 169
internal edge count inherited from MAP15_02 = 312
special transaction id, kind, source owner and state
transaction sector ids and edge locks
transaction span kind: SingleSector / TwoSector / MultiSectorExplicit / Deferred
2-sector transaction adjacency and intersector edge id
entry edge and return edge evidence when required
reservation claim owner, priority, sector id, optional edge id, reason
cluster id/variant id/source owner
cluster containment policy: SectorContained by default
cross-sector allowance id, exact edge id, allowed owner/kind, reason
conflict type, winner, loser and reason
input digest and output digest lower-hex SHA-256
mutation proof counters
downstream owner MAP15_04
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Planner Contract - WorldSpecialClusterPolicyPlanner.cs

Implement a deterministic planner that creates a multi-sector reservation policy without mutating MAP13, MAP14, MAP15_01 or MAP15_02 artifacts.

Required behavior:

1. Consume a successful MAP15_01 world solve order and successful MAP15_02 intersector edge plan.
2. Publish SpecialRegion reservation transactions:

```text
Village may use a 2-sector transaction when the public/reference request requires it.
CoreResource / Forge / Boss may use single-sector or explicit multi-sector transactions according to public authority.
Merchant / Maru may remain deferred when public authority marks them deferred.
Every non-deferred transaction reserves all claimed sectors and all required edge locks atomically.
```

3. A 2-sector transaction must prove:

```text
exactly 2 distinct sector ids
the two sectors are adjacent through a MAP15_02 edge
the transaction edge has two compatible endpoints
entry/return evidence exists when the Special kind requires mandatory return
no boundary/mandatory route conflict unless the transaction explicitly owns that edge
```

4. Apply reservation priority:

```text
1. fixed SpecialRegion transaction
2. mandatory route and boundary edge obligations inherited from MAP15_02
3. explicit cross-sector cluster allowance
4. sector-contained normal TerrainCluster
5. quiet/filler reservation
```

5. TerrainCluster policy:

```text
default = sector-contained
implicit cross-sector cluster = reject
explicit cross-sector allowlist requires exact cluster id/variant id/edge id/span reason
allowlisted cluster must not steal Special edge locks
allowlisted cluster must not block mandatory route or boundary warning edge
```

6. Produce stable canonical digest:

```text
input: MAP15_01 digests + MAP15_02 digests + MAP13/MAP14 handoff identity + cluster policy publication label
output: transactions + claims + edge locks + cluster policies + conflicts
```

7. Fail atomically with no partial `WorldMultiSectorReservationPlan` when:

```text
MAP15_01 or MAP15_02 input/result is missing or failed
world sector count != 169
internal edge count != 312
transaction has duplicate sector ids
2-sector transaction sectors are not adjacent
transaction references missing edge or missing endpoint
entry/return evidence is missing for mandatory Special transaction
claim references missing sector or edge
Special transaction overlaps another fixed Special transaction without an explicit merge reason
normal TerrainCluster crosses sector boundary without allowlist
cross-sector allowlist references missing cluster/variant/edge
allowlisted cluster conflicts with Special/mandatory/boundary edge lock
input digest is missing or not lower-hex SHA-256
planner would need to carve fallback corridor, rerender sector terrain, or mutate upstream output
```

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP11, MAP13, MAP14, MAP15_01 and MAP15_02. Do not invent production Special or cluster data when public data exists.

If some downstream-specific world placement facts are still not exposed, use deterministic `REFERENCE MULTI-SECTOR RESERVATION PLAN` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
2-sector Village transaction example over an approved MAP15_02 edge
single-sector fixed Special transaction examples
deferred Merchant/Maru examples when public authority is not final
sector-contained TerrainCluster examples
one explicit cross-sector cluster allowlist example
synthetic invalid transaction/conflict cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual full world terrain solve
actual Tilemap output
player traversal proof
Special gameplay, facilities, rewards or NPC spawn
MAP15 phase exit approval
```

## 8. Focused Test Requirements

Create `WorldSpecialClusterPolicyPlannerTests.cs` with category `MAP15_03`.

Required focused gates:

```text
MultiSectorReservationPlanPublishesTransactionsClaimsAndDigests
TwoSectorVillageTransactionUsesAdjacentEdgeAndEntryReturnEvidence
FixedSpecialReservationsBeatClusterAndQuietClaims
TerrainClustersAreSectorContainedByDefault
CrossSectorClusterRequiresExactAllowlistAndCompatibleEdge
ReservationConflictsReportWinnerLoserAndReasonDeterministically
ReservationPolicyIsDeterministicAcrossRepeatReverseAndCulture
InvalidReservationInputsFailAtomicallyWithoutPartialPlan
WorldReservationPolicyDoesNotMutateWorldPlanEdgePlanOrAuthoringAssets
Map15HandoffKeepsMap15_04Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
world sectors observed: 169/169
internal edges observed: 312/312
special transactions required/accepted/missing: actual/actual/0
2-sector transactions: actual >= 1 when reference fixture includes Village
2-sector adjacency failures: 0
entry/return missing: 0
reservation claims: actual
edge locks: actual
fixed Special overlap conflicts: 0
cluster policy entries: actual
sector-contained cluster decisions: actual
implicit cross-sector cluster rejects: actual
explicit cross-sector allowlist accepted: actual
allowlist missing/invalid rejects: actual
Special/mandatory/boundary stolen edge locks: 0
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
fallback carve: 0
sector rerender: 0
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
sector ids sorted row-major
edge ids sorted by MAP15_02 comparable identity
transactions sorted by priority then stable id
claims sorted by owner priority, sector id, edge id, claim id
cluster allowlist sorted by cluster id, variant id, edge id
conflicts sorted by conflict type, winner, loser
no Dictionary iteration order dependency
no current time
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing seed-like input may change only declared seed/retry fields. It must not change public topology constants, MAP15_01 solve order digest, MAP15_02 edge plan digest, or MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_03 must prove it does not write or mutate:

```text
MAP13 SpecialRegion data
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP15_02 intersector edge outputs
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
TASK: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
STATUS: PASS | FAIL | BLOCKED
MAP15_03: COMPLETE ELIGIBLE only when PASS
MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 multi-sector reservation/cluster policy이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- Special transaction required/accepted/missing count
- 2-sector Village/Special adjacency and edge-lock evidence
- TerrainCluster sector-contained default and cross-sector allowlist evidence
- conflict winner/loser/reason count
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
- downstream owner: MAP15_04

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_03]
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
Commit subject: MAP15_03: implement multi-sector special cluster policy
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_04.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY.md
MCP_ARCHIVE/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY.md
MCP/REPORTS/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldMultiSectorReservationPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldMultiSectorReservationPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_04: do not start
STOP after Result and optional PASS finalize commit
```
