```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
  task_file: TASKS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
  requires_current_task: NONE
  requires_completed_task: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
  requires_result:
    path: REPORTS/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY_RESULT.md
    status: PASS
    sha256: aae9901f16feeb1fb5e335d8cdef61fc9a96d66aaae94e234fb7c50fd6776232
  requires_installed_task:
    path: TASKS/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY.md
    sha256: 66c646bdf389be2143ece4f63dcdf64075a508003aa12778387c87bf2bb89a1c
  sets_current_task: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
```

# MAP16_03 - Validate Final Route and Recovery

```text
TASK: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_01 final canvas layer plan과 MAP16_02 protection/density report 위에서 final route witness와 recovery witness를 검증한다.

```text
MAP16_01 SectorFinalCanvasLayerPlan
MAP16_02 SectorCanvasProtectionDensityReport
MAP15_02 intersector socket/boundary identities
MAP14 protected route / high route / recovery handoff where public
MAP13 Special entrance identity where public
MAP08 boundary aperture identity where public
-> SectorFinalRouteRecoveryReport
-> SectorFinalRouteRecoveryValidator
-> MAP16_04 pattern/chunk coordinate partition input
```

이번 Task는 **final canvas 위의 static route/recovery witness 계약**만 소유한다. 실제 플레이어 컨트롤러, collider/physics, jump simulation, PlayMode traversal, Tilemap bake, 12x8 MicroChunk slice, file export, Scene/Prefab/GameObject/gameplay runtime 변경을 하지 않는다.

MAP16_03이 승인해야 하는 핵심:

```text
기본 entry -> exit static witness가 final canvas에서 존재한다.
required external socket/boundary aperture는 기본 route 또는 socket route에 연결된다.
고점 route 또는 optional/high branch 실패 지점은 기본 route로 복구되는 witness를 가진다.
witness는 Solid/Hazard/blocked cell을 통과하지 않고 protected/fixed/boundary/Special authority를 침범하지 않는다.
route/recovery 실패는 fallback carve나 silent widening이 아니라 typed failure로 남는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, route/recovery/socket witness 수치, blocked/hazard/softlock/fallback 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| static base route witness on 48x32 final canvas | player controller traversal |
| external socket and boundary aperture connectivity witness | collider/physics simulation |
| high-route failure -> base route recovery witness | jump arc/animation tuning |
| no-solid/no-hazard/no-blocked-cell route proof | actual Tilemap bake |
| typed softlock/recovery failure report | 12x8 MicroChunk partition |
| deterministic route/recovery digest | Generated CSV/JSON export |
| focused EditMode tests for MAP16_03 | Scene/Prefab/GameObject mutation |
| MAP16_04 handoff contract | Activity/Event/NPC/reward gameplay spawn |
| no fallback carve / no silent widening proof | MAP16 phase exit / production seed approval |

`SectorFinalRouteRecoveryReport` is a static final-canvas proof packet. It can publish route cells, graph edges and recovery witnesses, but it cannot mutate the canvas or prove real-time platformer physics.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_03`만 선택한다.

```text
MAP16_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_03` category로 제한한다.

신규 task-owned failure는 신규 MAP16_03 allowlist 파일만 수정하고 `MAP16_03` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_01 canvas digest/count mismatch, MAP16_02 protection-density contradiction, MAP15_02 socket/boundary contradiction, MAP14 route/recovery contradiction, MAP13 Special entrance contradiction, MAP08 boundary aperture contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_02 Result: PASS
MAP16_02 Result SHA-256:
aae9901f16feeb1fb5e335d8cdef61fc9a96d66aaae94e234fb7c50fd6776232

MAP16_02 installed Task SHA-256:
66c646bdf389be2143ece4f63dcdf64075a508003aa12778387c87bf2bb89a1c

MAP16_02 COMPLETE / MAP16_03 CURRENT / MAP16_04 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_01: SectorFinalCanvasLayerPlan, final cell/layer/source/protection/precedence facts
MAP16_02: SectorCanvasProtectionDensityReport, intrusion 0, density verdict and unowned-air verdict
MAP15_02: external socket, intersector edge, boundary aperture and endpoint identity where public
MAP15_07: world assembly exit identity and no-regression/no-fallback contract
MAP14: protected route envelope, high route and recovery handoff where public
MAP13: Special entrance/buffer identity where public
MAP08: boundary aperture and pair identity where public
```

MAP16_03 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP16_03 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

If live finalized route facts are still not exposed beyond MAP16_01~02 reference fixtures, use deterministic `REFERENCE FINAL ROUTE RECOVERY REPORT` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval or real player traversal.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryValidator.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorFinalRouteRecoveryValidatorTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_03
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_03 책임 안에 머물러야 한다.

수정·생성 금지:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/* existing files
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/*
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/*
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/*
Assets/_Game/Map/Runtime/WorldGeneration/Activities/*
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/*
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/*
Assets/_Game/Map/Data/WorldGeneration/**
Assets/_Game/Editor/**
Assets/_Game/Tests/PlayMode/**
existing C# / test / CSV / meta
Scenes / Prefabs / Tilemaps / ScriptableObjects
asmdef / asmref / ProjectSettings / Packages
generated debug files, JSON files, CSV files, textures, screenshots
12x8 slice files or exporters
MAP16_04+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - SectorFinalRouteRecoveryReport.cs

Create immutable value types for the MAP16_03 public surface.

Required concepts:

```text
FinalRouteWitnessKind
FinalRouteNodeKind
FinalRouteEdgeKind
FinalRouteFailureKind
FinalRouteRecoveryKind
FinalRouteWitnessVerdict
FinalRouteAnchor
FinalRouteNode
FinalRouteEdge
FinalRouteWitness
FinalRecoveryWitness
FinalRouteSoftlockCandidate
SectorFinalRouteRecoveryReport
FinalRouteRecoveryFailure
FinalRouteRecoveryResult
FinalRouteRecoveryDigest
```

Minimum witness kinds:

```text
BaseEntryToExit
ExternalSocketToBaseRoute
BoundaryApertureToBaseRoute
HighRouteBranch
HighFailureToBaseRecovery
SpecialEntranceToBaseRoute
```

Minimum static edge kinds:

```text
OrthogonalPassable
DeclaredStep
DeclaredDrop
DeclaredJumpLink
DeclaredLadderOrClimb
DeclaredBounceOrDevice
DeclaredRecoveryLink
DeclaredSocketLink
```

Declared edge kinds are static authoring/affordance links only. They are not runtime physics proof.

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
sector size: 48x32
cells per sector: 1536
source MAP16_01 input/output digest
source MAP16_02 input/output digest
base entry anchor
base exit anchor
external socket anchors
boundary aperture anchors
high route branch anchors
failure sample anchors
route node count
route edge count
base route witness exists
base route start/end match entry/exit
external socket witnesses required/covered/missing
boundary aperture witnesses required/covered/missing
high route failure samples required/covered/missing
recovery witnesses required/covered/missing
route cells crossing Solid/Hazard/blocked cells
softlock candidates
fallback carve / silent widening counters
input/output digest lower-hex SHA-256
downstream owner MAP16_04
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Validator Contract - SectorFinalRouteRecoveryValidator.cs

Implement a deterministic validator that builds final route and recovery witnesses without mutating MAP16_01 plan or MAP16_02 report.

Required behavior:

1. Consume successful `SectorFinalCanvasLayerPlan` and successful `SectorCanvasProtectionDensityReport`.
2. Validate exact sector size 48x32 and expected cell count 1536.
3. Validate MAP16_02 accepted report has:

```text
protection intrusion count == 0
density budget violations == 0
unowned AIR violations == 0
cleanup projection protected/fixed/boundary/Special changes == 0
```

4. Build a static passability graph from final canvas cells:

```text
passable: AIR / platform affordance / protected-open / declared route cells
blocked: Solid terrain, blocking Hazard, blocked Protection, invalid missing layer
```

5. Add declared affordance links only when public final canvas layer/source/protection facts expose them. Declared links must be marked by edge kind and source owner.
6. Validate base entry -> exit witness:

```text
entry anchor exists
exit anchor exists
entry and exit are passable or declared socket/protected-open cells
one deterministic witness path exists
path does not cross Solid/Hazard/blocked cells
```

7. Validate external socket and boundary aperture witnesses:

```text
each required external socket anchor connects to base route or entry/exit route witness
each required boundary aperture anchor connects to base route or entry/exit route witness
missing or asymmetric witness becomes typed failure
```

8. Validate high route and recovery:

```text
high-route branch anchors are optional route witnesses
each declared high-route failure sample has a recovery witness to base route
recovery witness must not require fallback carve, silent widening, sector rerender, or full-world rerandom
recovery witness must not cross blocked/protected-forbidden cells
```

9. Detect static softlock candidates:

```text
reachable component not connected to base route
one-way declared link with no recovery witness
high branch failure sample with no recovery
external socket connected only to isolated component
Special entrance connected only to isolated component
```

10. Produce stable canonical digest:

```text
input: MAP16_01 input/output digest + MAP16_02 input/output digest + sector id + sorted anchors + route policy version
output: sorted nodes + sorted edges + witnesses + recovery witnesses + softlock candidates + mutation counters + downstream handoff
```

11. Fail atomically with no partial `SectorFinalRouteRecoveryReport` when:

```text
MAP16_01 plan is missing or failed
MAP16_02 report is missing or failed
sector size != 48x32
cell count != 1536
required layer is missing
entry/exit anchor is missing
base entry -> exit witness missing
external socket witness missing
boundary aperture witness missing
high failure recovery witness missing
route crosses Solid/Hazard/blocked cell
static softlock candidate exists in accepted request
input/output digest is missing or not lower-hex SHA-256
validator would require Tilemap write, 12x8 slice, file export, generated asset, Scene/Prefab/GameObject mutation, player physics, rerender, reroll, fallback carve, silent widening, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP16_01~02 and upstream MAP15/MAP14/MAP13/MAP08 when exposed. Do not invent production route data when public data exists.

Allowed fixture scope:

```text
one accepted reference 48x32 final canvas plan from MAP16_01
one accepted reference protection/density report from MAP16_02
base entry and exit anchors tied to public route labels
external socket and boundary aperture anchors tied to MAP15_02/MAP08 labels
high route branch and failure samples tied to MAP14 route labels where public
Special entrance anchor tied to MAP13/MAP15 label where public
synthetic invalid missing-route, blocked-route and no-recovery cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual 624x416 world terrain solve
actual 12x8 slice output
actual Tilemap output
actual player controller traversal
collider/physics proof
Activity/Event runtime spawn
MAP16 phase exit approval
```

## 8. Focused Test Requirements

Create `SectorFinalRouteRecoveryValidatorTests.cs` with category `MAP16_03`.

Required focused gates:

```text
FinalRouteRecoveryReportPublishesAnchorsWitnessesAndDigests
BaseEntryToExitWitnessExistsAndAvoidsSolidHazardBlockedCells
ExternalSocketsAndBoundaryAperturesConnectToBaseRoute
HighRouteFailureSamplesRecoverToBaseRouteWithoutFallbackCarve
StaticSoftlockCandidatesAreZeroForAcceptedCanvas
RouteRecoveryFailuresAreTypedAndAtomicForMissingBlockedOrIsolatedRoutes
RouteRecoveryDigestIsDeterministicAcrossRepeatReverseAndCulture
ValidatorDoesNotMutateCanvasProtectionDensityWorldFilesTilesScenesOrGameplayObjects
RouteRecoveryDoesNotUsePlayerPhysicsPlayModeOrTilemapBake
Map16HandoffKeepsMap16_04Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
unique cell coordinates: 1536/1536
MAP16_02 protection intrusions: 0
MAP16_02 density violations: 0
MAP16_02 unowned AIR violations: 0
base entry anchors required/covered/missing: 1/1/0
base exit anchors required/covered/missing: 1/1/0
base route witnesses required/covered/missing: 1/1/0
external socket witnesses required/covered/missing: actual/actual/0
boundary aperture witnesses required/covered/missing: actual/actual/0
special entrance witnesses required/covered/missing: actual/actual/0
high failure samples required/covered/missing: actual/actual/0
recovery witnesses required/covered/missing: actual/actual/0
route cells crossing Solid/Hazard/blocked cells: 0
static softlock candidates: 0
fallback carve actions: 0
silent widening actions: 0
whole-world rerandom actions: 0
player physics simulations: 0
PlayMode runs: 0
Tilemap bakes: 0
12x8 slices created: 0
generated file writes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
production seed approvals: 0
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
anchors sorted by kind, coordinate, source owner, stable id
nodes sorted by row-major coordinate then node kind
edges sorted by from coordinate, to coordinate, edge kind, source owner, stable id
witnesses sorted by witness kind, start, end, stable id
recovery witnesses sorted by failure anchor, target base anchor, stable id
softlock candidates sorted by coordinate, kind, stable id
failure records sorted by code, subject, reason
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing fixture label may change only declared synthetic route/recovery evidence. It must not change public topology constants, MAP16_01 canvas digest, MAP16_02 protection-density digest, MAP15_07 exit digest, MAP14 route identity, MAP08 boundary authority digest, or MAP13 Special identity when public.

## 10. No Mutation Proof

MAP16_03 must prove it does not write or mutate:

```text
MAP16_01 final canvas layer plan
MAP16_02 protection-density report
MAP15_01~07 world assembly outputs
MAP14 sector planner outputs
MAP13 SpecialRegion authoring/runtime outputs
MAP08 boundary authoring CSV/cache
MAP07 fixed slice/canvas authority files
MAP09~14 authoring CSV/cache
Generated CSV files
debug export files
JSON files
Tilemap cells
12x8 slice assets or records
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The validator may allocate in-memory immutable values. No generated file export, no Tilemap write, no actual 12x8 slicing, no player physics and no MAP16_04 task execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
STATUS: PASS | FAIL | BLOCKED
MAP16_03: COMPLETE ELIGIBLE only when PASS
MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 static final route/recovery witness report이며 Tilemap/slice/physics/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- sector size, cell count, route node/edge count
- base entry->exit witness count
- external socket / boundary aperture / special entrance witness count
- high failure sample and recovery witness count
- route blocked/hazard/solid crossing count 0
- static softlock count 0
- fallback carve/silent widening/whole-world rerandom 0
- player physics/PlayMode/Tilemap bake/slice/file write 0
- input/output digest
- deterministic replay evidence
- mutation/file-write/Tilemap/Scene/Prefab/GameObject/slice/spawn 0
- 회귀를 돌리지 않았다는 증거
- 아직 구현하지 않은 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script path
- class/method별 책임
- helper/probe별 input -> output
- public authority consumed
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP16_04

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_03]
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
FULL REGRESSION RUNS: 0
```

If PASS:

```text
Commit subject: MAP16_03: validate final route recovery
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_04.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
MCP_ARCHIVE/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
MCP/REPORTS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryValidator.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorFinalRouteRecoveryValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorFinalRouteRecoveryValidatorTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_04: do not start
STOP after Result and optional PASS finalize commit
```
