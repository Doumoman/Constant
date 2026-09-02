```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
  task_file: TASKS/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT.md
  requires_current_task: NONE
  requires_completed_task: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
  requires_result:
    path: REPORTS/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION_RESULT.md
    status: PASS
    sha256: 07814c976bdb18eaef0148bbae3c5a4cfd0ee44389538f8a7e78e3609060280b
  requires_installed_task:
    path: TASKS/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION.md
    sha256: 56873c09160278f14e2c17e1d4572de504c93891e328c6da3311997ed2634990
  sets_current_task: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
```

# MAP15_05 - Implement Neighbor Rollback and Failure Report

```text
TASK: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01~04가 만든 world solve/order, intersector edge, reservation, pacing-density-repetition 계약 위에 실패 sector + 1-ring rollback scope와 first contradiction failure report를 만든다.

```text
MAP15_01 WorldPlanInput + WorldSolveOrderResult
MAP15_02 WorldIntersectorEdgePlan
MAP15_03 WorldMultiSectorReservationPlan
MAP15_04 WorldPacingDensityPlan
MAP14 sector-local retry/debug handoff
-> WorldNeighborRollbackPlan
-> WorldNeighborRollbackPlanner
-> bounded rollback scope + first contradiction report + digest
-> MAP15_06 export overlay and batch test world plans
```

이번 Task는 **world-level failure containment contract**만 소유한다. 실제 sector terrain을 다시 렌더링하거나, 624x416 Tilemap을 굽거나, Scene/Prefab/GameObject 또는 gameplay runtime을 변경하지 않는다.

MAP15_05가 승인해야 하는 핵심:

```text
실패 sector의 rollback scope는 center + in-bounds 1-ring으로 제한된다.
corner scope는 4 sectors, edge scope는 6 sectors, interior scope는 9 sectors를 넘지 않는다.
first contradiction은 solve step 순서와 source priority로 결정론적으로 선택된다.
report는 관련 edge/reservation/pacing/candidate/retry evidence를 포함한다.
whole-world rerandom, fallback carve, silent retry widening은 금지된다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, rollback scope 수치, first contradiction evidence, related edge/reservation/candidate 수치, digest, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| failed sector + in-bounds 1-ring rollback scope model | actual full-world terrain solve |
| first contradiction selection and stable priority | Tilemap bake |
| related edge/reservation/pacing/candidate/retry report | MicroChunk 12x8 slice/streaming |
| rollback cap and no-whole-world-rerandom proof | collider/physics/player traversal |
| typed failure and containment reasons | Scene/Prefab/GameObject mutation |
| deterministic rollback/failure digest | Activity/Event/NPC/reward gameplay spawn |
| focused EditMode tests for MAP15_05 | MAP15_06 overlay export/batch execution |
| MAP15_06 handoff contract | MAP15 phase exit / production seed approval |

`WorldNeighborRollbackPlan`은 실패를 어디까지 되돌릴 수 있는지와 왜 실패했는지를 설명하는 계약이다. 실제 state rollback, sector rerender, candidate reroll, terrain carve를 실행하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_05`만 선택한다.

```text
MAP15_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01/MAP15_02/MAP15_03/MAP15_04 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_05` category로 제한한다.

신규 task-owned failure는 신규 MAP15_05 allowlist 파일만 수정하고 `MAP15_05` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01 solve order contradiction, MAP15_02 edge contract contradiction, MAP15_03 reservation contradiction, MAP15_04 pacing-density contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_04 Result: PASS
MAP15_04 Result SHA-256:
07814c976bdb18eaef0148bbae3c5a4cfd0ee44389538f8a7e78e3609060280b

MAP15_04 installed Task SHA-256:
56873c09160278f14e2c17e1d4572de504c93891e328c6da3311997ed2634990

MAP15_04 COMPLETE / MAP15_05 CURRENT / MAP15_06 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14: sector-local retry/debug failure handoff and no-fallback contract
MAP15_01: WorldPlanInput, WorldSolveOrderResult, sector coordinate and solve-step order
MAP15_02: WorldIntersectorEdgePlan, edge id, endpoints, boundary/socket binding
MAP15_03: WorldMultiSectorReservationPlan, Special transactions, claims, edge locks, conflicts
MAP15_04: WorldPacingDensityPlan, windows, budgets, recent-use observations and violations
```

MAP15_05 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_05 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_05
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_05 책임 안에 머물러야 한다.

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

## 5. Model Contract - WorldNeighborRollbackPlan.cs

Create immutable value types for the MAP15_05 public surface.

Required concepts:

```text
WorldRollbackScopeKind
WorldRollbackSector
WorldRollbackScope
WorldContradictionKind
WorldContradictionSource
WorldContradictionEvidence
WorldFailureReport
WorldRollbackDecision
WorldRollbackPolicyRequest
WorldNeighborRollbackPlan
WorldNeighborRollbackFailure
WorldNeighborRollbackDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
world sector count inherited from MAP15_01 = 169
internal edge count inherited from MAP15_02 = 312
reservation plan identity inherited from MAP15_03
pacing-density plan identity inherited from MAP15_04
failed sector id and coordinate
rollback scope kind: Corner / Edge / Interior
rollback sector ids and solve-step indexes
scope radius = 1
scope count max = 9
scope count expected: corner 4, edge 6, interior 9
first contradiction kind/source/sector/solve step
related edge ids, reservation ids, candidate ids, pacing window ids, retry labels
rollback decision: bounded retry / abort / blocked owner
no whole-world rerandom / no fallback carve / no silent widening counters
input digest and output digest lower-hex SHA-256
mutation proof counters
downstream owner MAP15_06
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Planner Contract - WorldNeighborRollbackPlanner.cs

Implement a deterministic planner that builds rollback scope and failure report evidence without mutating MAP14 or MAP15_01~04 artifacts.

Required behavior:

1. Consume successful MAP15_01 world solve order, MAP15_02 intersector edge plan, MAP15_03 reservation plan and MAP15_04 pacing-density plan.
2. Accept one failed sector id and zero or more contradiction observations.
3. Build rollback scope from the failed sector coordinate using in-bounds Moore 1-ring:

```text
corner: 4 sectors
world edge but not corner: 6 sectors
interior: 9 sectors
```

4. Scope must include the failed sector and only sectors with `abs(dx) <= 1` and `abs(dy) <= 1`.
5. Scope sector order must be deterministic:

```text
failed sector first
then in-scope sectors by solve step ascending
then sector id ascending
```

6. Choose first contradiction deterministically:

```text
lowest solve step
source priority: Special > Boundary > MandatoryRoute > IntersectorSocket > Reservation > PacingDensity > ClusterCandidate > Retry > Unknown
sector id ascending
stable contradiction id ascending
```

7. The failure report must preserve related evidence when present:

```text
related MAP15_02 edge ids
related MAP15_03 reservation transaction / claim / edge lock ids
related MAP15_04 window / budget / recent-use / cap ids
related cluster/pattern/activity candidate ids
related MAP14 retry/debug labels
```

8. Rollback decision:

```text
BoundedRetry when the contradiction can be retried within failed sector + 1-ring
Abort when retry cap is exhausted or required public authority is missing
BlockedOwner when an upstream owner must repair its own invariant
```

9. Produce stable canonical digest:

```text
input: MAP15_01 digest + MAP15_02 digest + MAP15_03 digest + MAP15_04 digest + MAP14 debug/retry identity + failed sector + observations
output: rollback scope + first contradiction + related evidence + decision + counters
```

10. Fail atomically with no partial `WorldNeighborRollbackPlan` when:

```text
MAP15_01, MAP15_02, MAP15_03 or MAP15_04 input/result is missing or failed
world sector count != 169
internal edge count != 312
failed sector is missing or out of bounds
scope would include a missing sector
scope would exceed 9 sectors
contradiction references missing sector
contradiction references edge/reservation/window/candidate outside public evidence
first contradiction cannot be selected from non-empty observations
input digest is missing or not lower-hex SHA-256
planner would need whole-world rerandom, fallback corridor carve, sector rerender, or upstream mutation
```

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP14 and MAP15_01~04. Do not invent production failure data when public data exists.

If downstream-specific live failure observations are still not exposed, use deterministic `REFERENCE NEIGHBOR ROLLBACK REPORT` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
corner/edge/interior failed sector examples
synthetic contradiction observations tied to public MAP15_02/03/04 identities
bounded retry, abort and blocked owner examples
synthetic invalid scope/evidence cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual full world terrain solve
actual Tilemap output
real state rollback execution
player traversal proof
Activity/Event runtime spawn
MAP15 phase exit approval
```

## 8. Focused Test Requirements

Create `WorldNeighborRollbackPlannerTests.cs` with category `MAP15_05`.

Required focused gates:

```text
RollbackPlanPublishesScopeReportDecisionAndDigests
CornerEdgeAndInteriorFailuresUseOnlyInBoundsOneRingScopes
RollbackScopeNeverExceedsFailedSectorPlusOneRing
FirstContradictionIsChosenBySolveStepAndSourcePriority
FailureReportLinksEdgesReservationsPacingCandidatesAndRetryEvidence
RollbackDecisionRejectsWholeWorldRerandomFallbackCarveAndSilentWidening
RollbackPolicyIsDeterministicAcrossRepeatReverseAndCulture
InvalidRollbackInputsFailAtomicallyWithoutPartialPlan
WorldRollbackDoesNotMutatePacingReservationEdgeWorldOrAuthoringAssets
Map15HandoffKeepsMap15_06Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
world sectors observed: 169/169
internal edges observed: 312/312
reservation plan observed: 1/1
pacing-density plan observed: 1/1
corner scope sectors: 4/4
edge scope sectors: 6/6
interior scope sectors: 9/9
scope max exceeded: 0
scope out-of-radius sectors: 0
failed sector missing from scope: 0
first contradiction selected: 1/1
related edge references valid: actual/actual
related reservation references valid: actual/actual
related pacing references valid: actual/actual
related candidate/retry references valid: actual/actual
bounded retry decisions: actual
abort decisions: actual
blocked owner decisions: actual
whole-world rerandom decisions: 0
fallback carve decisions: 0
silent widening decisions: 0
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
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
failed sector id row-major token
scope sectors sorted by failed-first policy
contradictions sorted by solve step, source priority, sector id, stable id
related evidence sorted by type then stable id
decisions sorted by decision kind then reason
no Dictionary iteration order dependency
no current time
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing seed-like input may change only declared failure/retry evidence. It must not change public topology constants, MAP15_01 solve order digest, MAP15_02 edge plan digest, MAP15_03 reservation plan digest, MAP15_04 pacing-density digest, or MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_05 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP15_02 intersector edge outputs
MAP15_03 reservation policy outputs
MAP15_04 pacing-density outputs
MAP09~14 authoring CSV/cache
Generated CSV files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The planner may allocate in-memory immutable values. No generated debug file export and no actual rollback execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
STATUS: PASS | FAIL | BLOCKED
MAP15_05: COMPLETE ELIGIBLE only when PASS
MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 rollback scope/failure report contract이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- corner/edge/interior rollback scope count
- failed sector + 1-ring 제한과 max 9 evidence
- first contradiction selection evidence
- related edge/reservation/pacing/candidate/retry reference count
- bounded retry / abort / blocked owner decision count
- whole-world rerandom/fallback carve/silent widening 0
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
- downstream owner: MAP15_06

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_05]
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
Commit subject: MAP15_05: implement neighbor rollback failure report
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_06.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT.md
MCP_ARCHIVE/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT.md
MCP/REPORTS/MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_06: do not start
STOP after Result and optional PASS finalize commit
```
