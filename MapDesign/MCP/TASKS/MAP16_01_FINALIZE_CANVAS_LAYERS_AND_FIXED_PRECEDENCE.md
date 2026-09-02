```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
  task_file: TASKS/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE.md
  requires_current_task: NONE
  requires_completed_task: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
  requires_result:
    path: REPORTS/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT_RESULT.md
    status: PASS
    sha256: 1bf40f24898f41f6f004a9b363262d287445200e5f6c223edf2ea35386300dc8
  requires_installed_task:
    path: TASKS/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT.md
    sha256: 28992f41ceb77c41e6dc87fc245414e7e2979832693521174f347c28f0de5bb5
  sets_current_task: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
```

# MAP16_01 - Finalize Canvas Layers and Fixed Precedence

```text
TASK: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_07로 승인된 reference world assembly 위에서 48x32 sector canvas의 final cell layer 계약을 만든다.

```text
MAP15_07 world assembly exit audit
MAP15_06 overlay/batch report
MAP15_01~05 public world assembly chain
MAP14 sector-local ownership and protected route handoff
MAP08 approved boundary priority
MAP07 fixed slice / fixed canvas authority where public
-> SectorFinalCanvasLayerPlan
-> SectorCanvasLayerFinalizer
-> MAP16_02 protection/cleanup/density validator input
```

이번 Task는 **sector-local 48x32 final canvas layer contract**만 소유한다. 실제 624x416 Tilemap을 굽거나, 12x8 MicroChunk slice를 만들거나, Generated CSV/JSON 파일을 쓰거나, Scene/Prefab/GameObject/gameplay runtime을 변경하지 않는다.

MAP16_01이 승인해야 하는 핵심:

```text
각 final canvas cell은 Terrain / Affordance / Material / Hazard / Marker / Protection / SourceOwner layer를 가진다.
각 layer claim은 source owner, priority, protected flag, provenance를 가진다.
MAP07 fixed slice authority와 MAP08 boundary authority는 일반 cluster/pattern/filler보다 우선한다.
ProtectedOpen / route envelope / special entrance / boundary aperture는 후순위 solid claim으로 덮이지 않는다.
동일 layer 충돌은 silent overwrite가 아니라 typed conflict/failure로 남는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, canvas/cell/layer/priority/conflict 수치, MAP07/MAP08 precedence evidence, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 48x32 sector final canvas layer model | actual 624x416 Tilemap bake |
| per-cell Terrain/Affordance/Material/Hazard/Marker/Protection/SourceOwner publication | 12x8 MicroChunk partition |
| source owner and provenance record | slice socket derivation |
| MAP07 fixed authority precedence | Generated CSV/JSON export |
| MAP08 boundary/socket/aperture precedence | collider/physics/player traversal |
| protected-open and fixed-shell overwrite rejection | Scene/Prefab/GameObject mutation |
| typed conflict/failure for same-layer collisions | Activity/Event/NPC/reward gameplay spawn |
| deterministic canvas digest | production seed approval |
| focused EditMode tests for MAP16_01 | MAP16 phase exit |

`SectorFinalCanvasLayerPlan` is a final cell ownership contract, not a renderer. It cannot run world generation, reroll sectors, carve fallback corridors, slice chunks, write files, or instantiate gameplay objects.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_01`만 선택한다.

```text
MAP16_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_01` category로 제한한다.

신규 task-owned failure는 신규 MAP16_01 allowlist 파일만 수정하고 `MAP16_01` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_07 digest/count mismatch, MAP14 protected-route contradiction, MAP08 boundary authority contradiction, MAP07 fixed authority contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_07 Result: PASS
MAP15_07 Result SHA-256:
1bf40f24898f41f6f004a9b363262d287445200e5f6c223edf2ea35386300dc8

MAP15_07 installed Task SHA-256:
28992f41ceb77c41e6dc87fc245414e7e2979832693521174f347c28f0de5bb5

MAP15_07 COMPLETE / MAP16_01 CURRENT / MAP16_02 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP07: fixed slice / fixed canvas authority where public
MAP08: approved boundary pair/candidate/aperture priority where public
MAP09: layer ownership, AccessClass, PacingRole, MicroPattern/MicroChunk constants
MAP10: MicroPattern rendered cell intent and protected mask facts where public
MAP11: TerrainCluster footprint/spine/envelope and source identity where public
MAP12: Activity/Event marker layer identity where public
MAP13: SpecialRegion fixed shell, entrance buffer and facility marker identity where public
MAP14: sector-local ownership canvas, protected route envelope, retry/debug handoff
MAP15_01~07: world topology, edge/reservation/pacing/rollback/overlay/exit public chain
```

MAP16_01 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP16_01 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

If live finalized 48x32 canvas claims are not exposed yet, use deterministic `REFERENCE FINAL CANVAS LAYER PLAN` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasLayerFinalizer.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasLayerFinalizerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_01
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_01 책임 안에 머물러야 한다.

수정·생성 금지:

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/* existing files
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
MAP16_02+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - SectorFinalCanvasLayerPlan.cs

Create immutable value types for the MAP16_01 public surface.

Required concepts:

```text
FinalCanvasLayerKind
FinalCanvasCellKind
FinalCanvasSourceOwner
FinalCanvasClaimPriority
FinalCanvasProtectionKind
FinalCanvasCellCoordinate
FinalCanvasLayerClaim
FinalCanvasCell
FinalCanvasLayerSummary
FinalCanvasConflict
SectorFinalCanvasLayerPlan
FinalCanvasLayerFailure
FinalCanvasLayerResult
FinalCanvasLayerDigest
```

Minimum layer kinds:

```text
Terrain
Affordance
Material
Hazard
Marker
Protection
SourceOwner
```

Minimum source owners:

```text
FixedSlice
Boundary
SpecialRegion
MandatoryRoute
TerrainCluster
MicroPattern
Activity
EventOverlay
QuietFiller
Cleanup
Unknown
```

Minimum priority order from strongest to weakest:

```text
FixedSlice
SpecialFixedShell
BoundaryAperture
MandatoryRouteProtectedOpen
SpecialEntranceBuffer
TerrainClusterSpine
TerrainClusterPattern
ActivityMarker
EventMarker
QuietFiller
Cleanup
```

The exact enum names may follow the project's style, but the semantics must be visible in Result.

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
sector size: 48x32
cells per sector: 1536
layer kinds required/covered/missing: 7/7/0
cell coordinates unique and in bounds
per-cell layer claim winners
source owner for every winning claim
provenance/source id for every winning claim
protected-open cells and protection reason
fixed cells and fixed authority reason
boundary aperture cells and boundary authority reason
marker cells and marker owner
conflict list and typed reason
input/output digest lower-hex SHA-256
downstream owner MAP16_02
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Finalizer Contract - SectorCanvasLayerFinalizer.cs

Implement a deterministic finalizer that resolves layer claims into a 48x32 final canvas plan without mutating upstream artifacts.

Required behavior:

1. Consume public MAP15_07/MAP15_06 world assembly identity and public MAP14 sector-local canvas/protection facts where available.
2. Accept one sector identity and a set of layer claims.
3. Validate exact sector size 48x32 and expected cell count 1536.
4. Validate claim coordinates are in bounds and claim layer/source/priority/provenance are not missing.
5. Resolve winners per cell/layer by explicit priority and stable tie-break:

```text
claim priority descending
source owner priority descending
provenance id ascending
claim id ascending
```

6. Reject illegal overwrite attempts:

```text
FixedSlice terrain overwritten by weaker Terrain claim
BoundaryAperture overwritten by weaker Terrain/Hazard claim
MandatoryRouteProtectedOpen filled by Solid/Hazard claim
SpecialEntranceBuffer blocked by Solid/Hazard claim
Protection layer removed by Cleanup/QuietFiller
same priority different value without explicit allowed merge
```

7. Publish summaries:

```text
cell count
layer coverage
source owner counts
priority winner counts
protected cell count
fixed cell count
boundary aperture count
marker count
conflict count
mutation proof counters
```

8. Produce stable canonical digest:

```text
input: upstream digest chain + sector id + sorted claim facts + finalizer policy version
output: sorted final cells + layer summaries + conflicts + mutation counters + downstream handoff
```

9. Fail atomically with no partial `SectorFinalCanvasLayerPlan` when:

```text
upstream MAP15_07 identity is missing or failed
sector size != 48x32
cell count != 1536
claim coordinate is missing or out of bounds
claim layer/source/priority/provenance is missing
forbidden overwrite is detected
same-layer conflict cannot be resolved deterministically
input/output digest is missing or not lower-hex SHA-256
finalizer would require Tilemap write, 12x8 slice, file export, generated asset, Scene/Prefab/GameObject mutation, rerender, reroll, fallback carve, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP14/MAP15 and MAP07/MAP08 when exposed. Do not invent production canvas data when public data exists.

Allowed fixture scope:

```text
one reference 48x32 sector canvas
FixedSlice cells tied to MAP07 authority label where public
BoundaryAperture cells tied to MAP08 authority label where public
ProtectedOpen route cells tied to MAP14/MAP15 public route label
Special entrance cells tied to MAP13/MAP15 reservation label
TerrainCluster/MicroPattern/QuietFiller weaker claims
Activity/Event marker-only claims
synthetic invalid overwrite/conflict cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual 624x416 world terrain solve
actual 12x8 slice output
actual Tilemap output
player traversal proof
Activity/Event runtime spawn
MAP16 phase exit approval
```

## 8. Focused Test Requirements

Create `SectorCanvasLayerFinalizerTests.cs` with category `MAP16_01`.

Required focused gates:

```text
FinalCanvasPlanPublishesSevenLayersCellsSourceOwnersAndDigests
FinalCanvasContainsExactly1536UniqueInBoundsCellsForOneSector
FixedSliceBoundaryAndProtectedOpenPrecedenceBeatWeakerClaims
SpecialEntranceAndMandatoryRouteCellsCannotBeBlockedBySolidOrHazard
LayerConflictsAreTypedDeterministicAndNeverSilentOverwrite
SourceOwnerAndProvenanceArePublishedForEveryWinningLayerClaim
FinalCanvasDigestIsDeterministicAcrossRepeatReverseAndCulture
InvalidCanvasInputsFailAtomicallyWithoutPartialPlan
FinalizerDoesNotMutateWorldAssemblyAuthoringFilesTilesScenesOrGameplayObjects
Map16HandoffKeepsMap16_02Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
unique cell coordinates: 1536/1536
out-of-bounds cells: 0
layer kinds required/covered/missing: 7/7/0
cells with source owner: 1536/1536 or all winning claims/all winning claims
cells with provenance id: actual/actual
fixed precedence wins over weaker claims: actual/actual
boundary precedence wins over weaker claims: actual/actual
protected-open overwrite violations: 0
special entrance blocked violations: 0
typed conflicts detected in synthetic invalid case: actual
silent overwrites: 0
input digest: 64 lower-hex
output digest: 64 lower-hex
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
12x8 slices created: 0
generated file writes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
production seed approvals: 0
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
claims sorted by coordinate, layer, priority, source owner, provenance id, claim id
final cells sorted by row-major coordinate
layer summaries sorted by layer kind
conflicts sorted by coordinate, layer, priority, source owner, claim id
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing fixture label may change only declared synthetic claim evidence. It must not change public topology constants, MAP15_07 exit digest, MAP15_06 overlay digest, MAP14 protected route identity, MAP08 boundary authority digest, or MAP07 fixed authority digest when public.

## 10. No Mutation Proof

MAP16_01 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP15_01~07 world assembly outputs
MAP07 fixed slice/canvas authority files
MAP08 boundary authoring CSV/cache
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

The finalizer may allocate in-memory immutable values. No generated file export, no Tilemap write, no actual 12x8 slicing and no MAP16_02 task execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
STATUS: PASS | FAIL | BLOCKED
MAP16_01: COMPLETE ELIGIBLE only when PASS
MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 48x32 final canvas layer/precedence contract이며 Tilemap/slice/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- sector size, cell count, layer count
- MAP07 fixed precedence evidence
- MAP08 boundary precedence evidence
- protected-open / special entrance overwrite rejection evidence
- source owner/provenance publication evidence
- conflict/silent overwrite count
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
- downstream owner: MAP16_02

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_01]
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
Commit subject: MAP16_01: finalize canvas layers precedence
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_02.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE.md
MCP_ARCHIVE/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE.md
MCP/REPORTS/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasLayerFinalizer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasLayerFinalizer.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasLayerFinalizerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasLayerFinalizerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_02: do not start
STOP after Result and optional PASS finalize commit
```
