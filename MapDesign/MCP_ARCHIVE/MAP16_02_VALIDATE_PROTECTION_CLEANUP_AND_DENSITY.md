```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
  task_file: TASKS/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY.md
  requires_current_task: NONE
  requires_completed_task: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
  requires_result:
    path: REPORTS/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE_RESULT.md
    status: PASS
    sha256: c3be5d6a37259a431280e7ed3502e0d021819a9a4f41a99f10b5767e6a2a8657
  requires_installed_task:
    path: TASKS/MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE.md
    sha256: 022fcd69b825c127e96d2d2515231c8646d362ff58c474ca6d4ec420ee247d90
  sets_current_task: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
```

# MAP16_02 - Validate Protection, Cleanup and Density

```text
TASK: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_01이 만든 48x32 sector final canvas layer plan 위에 protection intrusion, cleanup candidate, solid/reachable density, unowned-air region 검증 계약을 만든다.

```text
MAP16_01 SectorFinalCanvasLayerPlan
MAP15_07 world assembly exit identity
MAP14 protected route / ownership handoff
MAP13 Special entrance identity where public
MAP08 boundary aperture identity where public
-> SectorCanvasProtectionDensityReport
-> SectorCanvasProtectionDensityValidator
-> MAP16_03 final route and recovery validator input
```

이번 Task는 **검증 report와 cleanup projection 계약**만 소유한다. MAP16_01 원본 canvas를 직접 수정하지 않고, 실제 Tilemap을 굽지 않고, 12x8 MicroChunk slice를 만들지 않고, 파일/Generated asset/Scene/Prefab/GameObject/gameplay runtime을 변경하지 않는다.

MAP16_02가 승인해야 하는 핵심:

```text
ProtectedOpen, boundary aperture, fixed slice, Special entrance를 Solid/Hazard/blocked claim이 침범하지 않는다.
1-cell noise, head snag, shallow pit 같은 cleanup 후보가 typed evidence로 분류된다.
cleanup projection은 protected/fixed/boundary/Special cells를 바꾸지 않는다.
solid density는 40%~65% envelope 안에 있다.
reachable density는 35%~55% envelope 안에 있다.
무역할 AIR region은 최대 8x6을 넘지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, protection/cleanup/density/unowned-air 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| protection intrusion validation | actual MAP16_01 canvas mutation |
| typed cleanup candidate classification | actual Tilemap bake |
| cleanup projection safety report | 12x8 MicroChunk partition |
| solid density 40%~65% verdict | final route/player traversal approval |
| reachable density 35%~55% verdict | collider/physics/player PlayMode traversal |
| unowned-air max 8x6 region validation | Scene/Prefab/GameObject mutation |
| deterministic validation digest | Generated CSV/JSON export |
| focused EditMode tests for MAP16_02 | Activity/Event/NPC/reward gameplay spawn |
| MAP16_03 handoff contract | MAP16 phase exit / production seed approval |

`SectorCanvasProtectionDensityReport` is a validation and cleanup-projection packet. It can describe what should be cleaned, but it cannot write the cleaned canvas back to MAP16_01 output or any asset.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_02`만 선택한다.

```text
MAP16_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_02` category로 제한한다.

신규 task-owned failure는 신규 MAP16_02 allowlist 파일만 수정하고 `MAP16_02` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_01 digest/count mismatch, MAP15_07 world assembly contradiction, MAP14 protected-route contradiction, MAP08 boundary aperture contradiction, MAP13 Special entrance contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_01 Result: PASS
MAP16_01 Result SHA-256:
c3be5d6a37259a431280e7ed3502e0d021819a9a4f41a99f10b5767e6a2a8657

MAP16_01 installed Task SHA-256:
022fcd69b825c127e96d2d2515231c8646d362ff58c474ca6d4ec420ee247d90

MAP16_01 COMPLETE / MAP16_02 CURRENT / MAP16_03 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_01: SectorFinalCanvasLayerPlan, final cell/layer/source/protection/precedence facts
MAP15_07: world assembly exit identity and no-regression/no-fallback contract
MAP15_06: overlay/batch report identity where public
MAP14: protected route envelope and sector-local ownership handoff where public
MAP13: Special entrance/buffer identity where public
MAP08: boundary aperture and pair identity where public
MAP07: fixed slice/canvas authority where public
```

MAP16_02 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP16_02 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

If live finalized 48x32 canvas claims are still not exposed beyond MAP16_01 reference fixtures, use deterministic `REFERENCE PROTECTION CLEANUP DENSITY REPORT` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityValidator.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasProtectionDensityValidatorTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_02
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_02 책임 안에 머물러야 한다.

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
MAP16_03+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - SectorCanvasProtectionDensityReport.cs

Create immutable value types for the MAP16_02 public surface.

Required concepts:

```text
ProtectionIntrusionKind
CleanupCandidateKind
CleanupProjectionState
DensityBudgetKind
DensityBudgetVerdict
UnownedAirRegionKind
SectorCanvasProtectionIntrusion
SectorCanvasCleanupCandidate
SectorCanvasCleanupProjection
SectorCanvasDensityBudget
SectorCanvasUnownedAirRegion
SectorCanvasProtectionDensityReport
ProtectionDensityFailure
ProtectionDensityResult
ProtectionDensityDigest
```

Minimum protection intrusion kinds:

```text
ProtectedOpenSolidIntrusion
ProtectedOpenHazardIntrusion
BoundaryApertureBlocked
FixedSliceOverwritten
SpecialEntranceBlocked
ProtectionLayerMissing
```

Minimum cleanup candidate kinds:

```text
SingleCellSolidNoise
SingleCellAirNoise
HeadSnag
ShallowPit
OneCellLip
UnownedAirPocket
```

Minimum budget kinds:

```text
SolidDensity
ReachableDensity
UnownedAirMaxBox
ProtectionIntrusion
CleanupProjectionSafety
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
sector size: 48x32
cells per sector: 1536
source MAP16_01 input/output digest
protected cell count
fixed cell count
boundary aperture cell count
special entrance cell count
protection intrusion count
cleanup candidate count by kind
cleanup projection changed cell count
cleanup projection protected/fixed/boundary/Special changed count
solid cell count and permille
solid density envelope: 400..650 permille
reachable cell count and permille
reachable density envelope: 350..550 permille
largest unowned AIR width/height/area
unowned AIR max: 8x6
budget verdicts
typed failures
input/output digest lower-hex SHA-256
downstream owner MAP16_03
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Validator Contract - SectorCanvasProtectionDensityValidator.cs

Implement a deterministic validator that builds protection, cleanup and density report without mutating MAP16_01 plan.

Required behavior:

1. Consume a successful `SectorFinalCanvasLayerPlan`.
2. Validate exact sector size 48x32 and expected cell count 1536.
3. Classify protected/fixed/boundary/Special cells from public layer/source/protection facts.
4. Reject protection intrusions:

```text
ProtectedOpen cell has Terrain Solid from weaker source
ProtectedOpen cell has Hazard blocking value
BoundaryAperture cell is blocked or removed
FixedSlice cell is overwritten by weaker terrain/material/hazard
SpecialEntrance cell is blocked by Solid/Hazard
Protection layer is missing from a protected cell
```

5. Detect cleanup candidates without writing them:

```text
single isolated Solid noise
single isolated Air noise
one-tile ceiling HeadSnag near reachable corridor
one-cell ShallowPit in reachable floor
one-cell lip that creates avoidable snag
unowned air pocket without route/activity/special/boundary purpose
```

6. Build cleanup projection safety evidence. Projection may mark candidate actions, but it must not change:

```text
ProtectedOpen cells
FixedSlice cells
BoundaryAperture cells
SpecialEntrance cells
MAP16_01 source canvas object
```

7. Compute density budgets:

```text
solid permille = Solid terrain cells / 1536 * 1000
solid envelope = 400..650 permille
reachable permille = abstract reachable air/affordance cells / 1536 * 1000
reachable envelope = 350..550 permille
```

Reachable is an abstract static cell flood/projection based on public final canvas layer values. It is not collider physics, player controller traversal, jump simulation or PlayMode validation.

8. Compute unowned AIR regions:

```text
AIR cells with no route, boundary, special, activity, event, marker or protected purpose
largest region width <= 8
largest region height <= 6
largest region area <= 48
```

9. Produce stable canonical digest:

```text
input: MAP16_01 input/output digest + sector id + validation policy version + sorted cell/layer tokens
output: intrusion list + cleanup candidates + projection safety + budgets + unowned-air regions + mutation counters + downstream handoff
```

10. Fail atomically with no partial `SectorCanvasProtectionDensityReport` when:

```text
MAP16_01 plan is missing or failed
sector size != 48x32
cell count != 1536
cell coordinate duplicate or out of bounds
required layer is missing
source/provenance missing
protection intrusion exists in accepted request
solid density is outside 400..650 permille
reachable density is outside 350..550 permille
unowned AIR exceeds 8x6 or area 48
cleanup projection would change protected/fixed/boundary/Special cells
input/output digest is missing or not lower-hex SHA-256
validator would require Tilemap write, 12x8 slice, file export, generated asset, Scene/Prefab/GameObject mutation, rerender, reroll, fallback carve, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP16_01 and upstream MAP15/MAP14/MAP13/MAP08/MAP07 when exposed. Do not invent production canvas data when public data exists.

Allowed fixture scope:

```text
one accepted reference 48x32 final canvas plan
synthetic protected-open, boundary, fixed and special entrance cells tied to public labels
synthetic cleanup candidate examples
synthetic solid/reachable density edge cases
synthetic unowned AIR max-box cases
synthetic invalid intrusion/projection cases for atomic failure tests
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

Create `SectorCanvasProtectionDensityValidatorTests.cs` with category `MAP16_02`.

Required focused gates:

```text
ProtectionDensityReportPublishesBudgetsCleanupAndDigests
ProtectedOpenBoundaryFixedAndSpecialCellsHaveZeroIntrusions
CleanupClassifierDetectsSingleCellNoiseHeadSnagAndPitCandidates
CleanupProjectionNeverChangesProtectedFixedBoundaryOrSpecialCells
SolidAndReachableDensityBudgetsStayWithinApprovedEnvelope
UnownedAirRegionDoesNotExceedEightBySixLimit
ProtectionDensityDigestIsDeterministicAcrossRepeatReverseAndCulture
InvalidProtectionDensityInputsFailAtomicallyWithoutPartialReport
ValidatorDoesNotMutateCanvasWorldAssemblyFilesTilesScenesOrGameplayObjects
Map16HandoffKeepsMap16_03Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
unique cell coordinates: 1536/1536
layer kinds required/covered/missing: 7/7/0
protected cells observed: actual
fixed cells observed: actual
boundary aperture cells observed: actual
special entrance cells observed: actual
protection intrusions in accepted plan: 0
protected-open solid intrusions: 0
protected-open hazard intrusions: 0
boundary aperture blocked: 0
fixed slice overwritten: 0
special entrance blocked: 0
cleanup candidate kinds required/covered/missing: 6/6/0
cleanup candidates detected: actual
cleanup projection protected/fixed/boundary/Special changes: 0
solid density permille: 400..650
reachable density permille: 350..550
density budget violations: 0
largest unowned AIR width/height: <=8/<=6
largest unowned AIR area: <=48
unowned AIR violations: 0
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
cells sorted by row-major coordinate
layers sorted by layer kind
intrusions sorted by coordinate, kind, source owner, claim id
cleanup candidates sorted by coordinate, kind, source owner, claim id
density budgets sorted by budget kind
unowned AIR regions sorted by min coordinate then width/height/area
failure records sorted by code, subject, reason
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing fixture label may change only declared synthetic cleanup/density evidence. It must not change public topology constants, MAP16_01 canvas digest, MAP15_07 exit digest, MAP14 protected route identity, MAP08 boundary authority digest, MAP13 Special identity, or MAP07 fixed authority digest when public.

## 10. No Mutation Proof

MAP16_02 must prove it does not write or mutate:

```text
MAP16_01 final canvas layer plan
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

The validator may allocate in-memory immutable values. No generated file export, no Tilemap write, no actual 12x8 slicing and no MAP16_03 task execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
STATUS: PASS | FAIL | BLOCKED
MAP16_02: COMPLETE ELIGIBLE only when PASS
MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 protection/cleanup/density validation report이며 Tilemap/slice/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- sector size, cell count, layer count
- protected/fixed/boundary/Special cell count
- protection intrusion count and kinds
- cleanup candidate kinds and count
- cleanup projection protected/fixed/boundary/Special change count 0
- solid/reachable density permille and verdict
- unowned AIR max box width/height/area and verdict
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
- downstream owner: MAP16_03

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_02]
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
Commit subject: MAP16_02: validate protection cleanup density
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_03.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY.md
MCP_ARCHIVE/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY.md
MCP/REPORTS/MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityValidator.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasProtectionDensityValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasProtectionDensityValidatorTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_03: do not start
STOP after Result and optional PASS finalize commit
```
