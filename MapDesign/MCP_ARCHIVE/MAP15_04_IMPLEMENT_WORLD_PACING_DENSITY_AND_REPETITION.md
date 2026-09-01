```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
  task_file: TASKS/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION.md
  requires_current_task: NONE
  requires_completed_task: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
  requires_result:
    path: REPORTS/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY_RESULT.md
    status: PASS
    sha256: 1e2adf481b3a9dce03d0ef1cf3450b7ba60359ab297ee53c0bc9b687b7cb7187
  requires_installed_task:
    path: TASKS/MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY.md
    sha256: 32a553a012dfc8b795ad879939246b7780b784013db3fd0882723e34a095c782
  sets_current_task: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
```

# MAP15_04 - Implement World Pacing, Density and Repetition

```text
TASK: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01 solve order, MAP15_02 intersector edge contract, MAP15_03 reservation policy 위에 world-level pacing window, density budget, recent-use repetition policy를 만든다.

```text
MAP15_01 WorldPlanInput + WorldSolveOrderResult
MAP15_02 WorldIntersectorEdgePlan
MAP15_03 WorldMultiSectorReservationPlan
MAP09 PacingRole/AccessClass contracts
MAP10 MicroPattern signature identity
MAP11 TerrainCluster identity
MAP12 Activity/Event identity
MAP13 Special/Landmark identity
-> WorldPacingDensityPlan
-> WorldPacingDensityPlanner
-> window/budget/repetition verdict + digest
-> MAP15_05 neighbor rollback and failure report
```

이번 Task는 **world-level rhythm contract**만 소유한다. 실제 48x32 sector terrain canvas를 다시 렌더링하지 않고, 624x416 Tilemap을 굽지 않고, Scene/Prefab/GameObject 또는 gameplay runtime에 반영하지 않는다.

MAP15_04가 승인해야 하는 핵심:

```text
Quiet / Cluster / Activity / Event / Landmark window가 sector solve order 위에 게시된다.
각 sector는 solid/reachable budget envelope를 가진다.
Pattern / Cluster / Activity recent-use signature와 최소 거리 규칙이 적용된다.
pacing/density/repetition 위반은 silent reroll 없이 typed failure 또는 violation report로 남는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, window/budget/repetition 수치, digest, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| Quiet/Cluster/Activity/Event/Landmark pacing window model | actual full-world terrain solve |
| per-sector abstract solid/reachable budget envelope | Tilemap bake |
| Pattern/Cluster/Activity recent-use signature policy | MicroChunk 12x8 slice/streaming |
| pacing/density/repetition violation report | collider/physics/player traversal |
| deterministic pacing-density digest | Scene/Prefab/GameObject mutation |
| no-reroll/no-mutation proof | Activity/Event/NPC/reward gameplay spawn |
| focused EditMode tests for MAP15_04 | MAP15_05 rollback execution |
| MAP15_05 handoff contract | MAP15 phase exit / batch seed approval |

`WorldPacingDensityPlan`은 world solve order에 대한 리듬과 예산 계약이다. 실제 tile cell의 solid/air를 바꾸거나 content를 다시 뽑지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_04`만 선택한다.

```text
MAP15_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01/MAP15_02/MAP15_03 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_04` category로 제한한다.

신규 task-owned failure는 신규 MAP15_04 allowlist 파일만 수정하고 `MAP15_04` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01 solve order contradiction, MAP15_02 edge contract contradiction, MAP15_03 reservation contradiction, MAP10/11/12 identity contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_03 Result: PASS
MAP15_03 Result SHA-256:
1e2adf481b3a9dce03d0ef1cf3450b7ba60359ab297ee53c0bc9b687b7cb7187

MAP15_03 installed Task SHA-256:
32a553a012dfc8b795ad879939246b7780b784013db3fd0882723e34a095c782

MAP15_03 COMPLETE / MAP15_04 CURRENT / MAP15_05 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP09: PacingRole, AccessClass, pass ownership and no-mutation contracts
MAP10: MicroPattern identity/profile/signature summary when publicly exposed
MAP11: TerrainCluster identity/variant/footprint/pacing compatibility summary
MAP12: ActivityStructure/EventOverlay identity, compatibility/frequency/cap publication
MAP13: SpecialRegion/Landmark identity and fixed/deferred state
MAP14: sector-local planner handoff and debug digest
MAP15_01: WorldPlanInput, WorldSolveOrderResult and sector solve order
MAP15_02: WorldIntersectorEdgePlan and boundary/socket obligations
MAP15_03: WorldMultiSectorReservationPlan, Special transactions, cluster containment/allowlist
```

MAP15_04 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP15_04 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldPacingDensityPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP15_04
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP15_04 책임 안에 머물러야 한다.

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

## 5. Model Contract - WorldPacingDensityPlan.cs

Create immutable value types for the MAP15_04 public surface.

Required concepts:

```text
WorldPacingWindowKind
WorldPacingWindow
WorldDensityBudgetKind
WorldSectorDensityBudget
WorldContentSignatureKind
WorldContentSignature
WorldRecentUseRule
WorldRecentUseObservation
WorldPacingDensityViolation
WorldPacingDensityPlan
WorldPacingDensityFailure
WorldPacingDensityDigest
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
world sector count inherited from MAP15_01 = 169
internal edge count inherited from MAP15_02 = 312
reservation plan identity inherited from MAP15_03
window kind: Quiet / Cluster / Activity / Event / Landmark
window sector ids, solve-step span, min/max count, reason and source owner
per-sector density budget: sector id, min/max solid budget, min/max reachable budget, reason
budget verdict: within range / warning / violation
content signature kind: Pattern / Cluster / Activity
signature id, sector id, solve step, source owner
recent-use rule kind, minimum sector distance, minimum solve-step distance, reason
recent-use observation and violation reason
input digest and output digest lower-hex SHA-256
mutation proof counters
downstream owner MAP15_05
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Planner Contract - WorldPacingDensityPlanner.cs

Implement a deterministic planner that creates pacing/density/repetition evidence without mutating MAP10~15 artifacts.

Required behavior:

1. Consume successful MAP15_01 world solve order, MAP15_02 intersector edge plan and MAP15_03 reservation plan.
2. Publish all five pacing window kinds:

```text
Quiet
Cluster
Activity
Event
Landmark
```

3. Every non-deferred sector in the 169-sector plan must receive a density budget envelope:

```text
solid budget min/max
reachable budget min/max
reason
source owner
```

The budget is abstract. It is not a tile count from a baked Tilemap.

4. Activity/Event windows must respect MAP12 availability/frequency/cap publication when public data is available. If public Activity/Event placement is not yet productionized, use labeled reference observations only.

5. Landmark windows must include fixed SpecialRegion sectors from MAP13/MAP15_03 and must not erase deferred Special identity.

6. Recent-use policy must support at least:

```text
Pattern signature minimum distance
Cluster signature minimum distance
Activity signature minimum distance
```

7. Repetition checks must compare both world graph distance and solve-step distance when both are available. If graph distance is unavailable for a focused fixture, report that the fixture used solve-step distance only.

8. Violations must be explicit and deterministic:

```text
window underfilled
window overfilled
density below min
density above max
reachable budget below min
recent pattern repeat
recent cluster repeat
recent activity repeat
forbidden Activity/Event cap exceed
```

9. Produce stable canonical digest:

```text
input: MAP15_01 digests + MAP15_02 digests + MAP15_03 digests + MAP10/11/12/13 identity summaries + publication label
output: windows + budgets + signatures + recent-use rules + observations + violations
```

10. Fail atomically with no partial `WorldPacingDensityPlan` when:

```text
MAP15_01, MAP15_02 or MAP15_03 input/result is missing or failed
world sector count != 169
internal edge count != 312
reservation plan digest is missing or invalid
required window kind is missing
window references missing sector
budget references missing sector
budget min/max is invalid
signature references missing sector
recent-use rule has non-positive distance
Activity/Event cap input contradicts MAP12 public contract
input digest is missing or not lower-hex SHA-256
planner would need to reroll, carve fallback, rerender sector terrain, or mutate upstream output
```

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP10, MAP11, MAP12, MAP13, MAP14 and MAP15_01~03. Do not invent production content data when public data exists.

If some downstream-specific full-world content facts are still not exposed, use deterministic `REFERENCE WORLD PACING DENSITY PLAN` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

Allowed fixture scope:

```text
169-sector solve-order observations
five pacing window kind examples
abstract solid/reachable budget examples
Pattern/Cluster/Activity recent-use signature examples
synthetic invalid window/budget/repetition cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual full world terrain solve
actual Tilemap output
player traversal proof
Activity/Event runtime spawn
MAP15 phase exit approval
```

## 8. Focused Test Requirements

Create `WorldPacingDensityPlannerTests.cs` with category `MAP15_04`.

Required focused gates:

```text
PacingDensityPlanPublishesWindowsBudgetsAndDigests
AllRequiredWindowKindsAreCoveredWithoutOpeningMap15_05
DensityBudgetsCoverAllWorldSectorsWithoutTileBake
SpecialAndLandmarkWindowsPreserveReservationPriority
PatternClusterActivityRecentUseRulesDetectRepeats
ActivityEventCapsAndFrequencyWindowsRemainAbstractAndBounded
PacingDensityPolicyIsDeterministicAcrossRepeatReverseAndCulture
InvalidPacingDensityInputsFailAtomicallyWithoutPartialPlan
WorldPacingDensityDoesNotMutateReservationEdgeWorldOrAuthoringAssets
Map15HandoffKeepsMap15_05Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
world sectors observed: 169/169
internal edges observed: 312/312
reservation plan observed: 1/1
window kind required/covered/missing: 5/5/0
windows total: actual
window sector references valid: actual/actual
density budget sectors: 169/169
invalid density budgets: 0
budget violation count: 0 for accepted reference fixture
signature kind required/covered/missing: 3/3/0
recent-use rules required/covered/missing: 3/3/0
accepted recent-use observations: actual
recent-use violations in accepted fixture: 0
synthetic repeat violations detected: actual
Activity/Event cap violations in accepted fixture: 0
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
windows sorted by kind, first solve step, stable id
budgets sorted by sector id and budget kind
signatures sorted by kind, sector id, stable signature id
recent-use rules sorted by kind and reason
observations sorted by rule kind, earlier sector, later sector
violations sorted by type, sector id, signature id
no Dictionary iteration order dependency
no current time
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing seed-like input may change only declared seed/retry fields. It must not change public topology constants, MAP15_01 solve order digest, MAP15_02 edge plan digest, MAP15_03 reservation plan digest, or MAP14 phase-exit digest.

## 10. No Mutation Proof

MAP15_04 must prove it does not write or mutate:

```text
MAP10 MicroPattern data
MAP11 TerrainCluster data
MAP12 Activity/Event data
MAP13 SpecialRegion data
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP15_02 intersector edge outputs
MAP15_03 reservation policy outputs
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
TASK: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
STATUS: PASS | FAIL | BLOCKED
MAP15_04: COMPLETE ELIGIBLE only when PASS
MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 pacing/density/repetition contract이며 Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- window kind required/covered/missing count
- density budget sector coverage and invalid/violation count
- Pattern/Cluster/Activity recent-use rule and observation count
- synthetic repetition violations detected count
- Activity/Event cap/frequency evidence
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
- downstream owner: MAP15_05

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_04]
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
Commit subject: MAP15_04: implement world pacing density repetition
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_05.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION.md
MCP_ARCHIVE/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION.md
MCP/REPORTS/MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldPacingDensityPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldPacingDensityPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_05: do not start
STOP after Result and optional PASS finalize commit
```
