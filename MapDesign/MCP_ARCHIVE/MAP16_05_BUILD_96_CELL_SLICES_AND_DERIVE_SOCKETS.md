```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
  task_file: TASKS/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS.md
  requires_current_task: NONE
  requires_completed_task: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
  requires_result:
    path: REPORTS/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION_RESULT.md
    status: PASS
    sha256: dddcd14efab835b6af85602ad7e728625905180a14cff77d08d2b38df94ea36f
  requires_installed_task:
    path: TASKS/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION.md
    sha256: 943635ce08eee445167e55cdb2a7ad0e0278ea2578baf5039098859dff8a962c
  sets_current_task: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
```

# MAP16_05 - Build 96-Cell Slices and Derive Sockets

```text
TASK: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_04의 coordinate partition 위에서 16개 12x8 Generated MicroChunk slice record를 메모리 안에 만들고, final canvas edge에서 실제 열린 socket band/signature/traversal summary를 파생한다.

```text
MAP16_01 SectorFinalCanvasLayerPlan
MAP16_02 SectorCanvasProtectionDensityReport
MAP16_03 SectorFinalRouteRecoveryReport
MAP16_04 SectorPatternChunkPartition
MAP15_02 intersector edge/socket identity where public
MAP08 boundary aperture identity where public
-> GeneratedMicroChunkSliceSet
-> GeneratedMicroChunkSliceBuilder
-> MAP16_06 marker/slot/provenance projection input
```

이번 Task는 **in-memory 96-cell slice record와 socket derivation 계약**만 소유한다. CSV/JSON/Generated asset을 쓰지 않고, Tilemap을 굽지 않고, Scene/Prefab/GameObject/gameplay runtime을 변경하지 않는다.

MAP16_05가 승인해야 하는 핵심:

```text
16개 12x8 slice가 생성된다.
각 slice는 정확히 96 unique cell을 가진다.
각 slice cell은 final canvas의 7개 layer winner와 source/provenance를 보존한다.
전체 slice cell은 sector 1536 cells를 중복/누락 없이 덮는다.
socket band는 각 slice의 실제 열린 edge cell에서 파생된다.
socket signature와 traversal summary는 final canvas cell/layer/protection을 근거로 결정론적으로 생성된다.
12x8 MicroChunk는 90도 회전되지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, slice/cell/layer/provenance/socket/signature/traversal 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 16 generated 12x8 MicroChunk slice records in memory | CSV/JSON/Generated asset export |
| 96 unique cells per slice | Tilemap bake |
| final canvas layer winner copy per slice cell | collider/physics/player traversal |
| source owner/provenance copy per slice cell | marker slot/stable spawn id projection |
| actual open edge socket band derivation | Activity/Event/NPC/reward gameplay spawn |
| socket signature and traversal summary | Scene/Prefab/GameObject mutation |
| route/recovery witness membership per slice | save/streaming/runtime state |
| deterministic slice digest | MAP16 phase exit / production seed approval |
| focused EditMode tests for MAP16_05 | MAP16_06 execution |

`GeneratedMicroChunkSliceSet` is an in-memory generated terrain data packet. It is the first task that copies final canvas cells into 12x8 slice records, but it still cannot write those records to disk or use them to bake a Tilemap.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_05`만 선택한다.

```text
MAP16_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03/MAP16_04 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_05` category로 제한한다.

신규 task-owned failure는 신규 MAP16_05 allowlist 파일만 수정하고 `MAP16_05` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_01 canvas digest/count mismatch, MAP16_02 protection-density contradiction, MAP16_03 route-recovery contradiction, MAP16_04 partition contradiction, MAP15_02 socket/boundary contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_04 Result: PASS
MAP16_04 Result SHA-256:
dddcd14efab835b6af85602ad7e728625905180a14cff77d08d2b38df94ea36f

MAP16_04 installed Task SHA-256:
943635ce08eee445167e55cdb2a7ad0e0278ea2578baf5039098859dff8a962c

MAP16_04 COMPLETE / MAP16_05 CURRENT / MAP16_06 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_01: SectorFinalCanvasLayerPlan, final cell/layer/source/protection/provenance facts
MAP16_02: SectorCanvasProtectionDensityReport, accepted protection/density identity
MAP16_03: SectorFinalRouteRecoveryReport, route/recovery witnesses and passability facts
MAP16_04: SectorPatternChunkPartition, 16 chunk slots, cell addresses and witness projections
MAP15_02: external socket/intersector edge/boundary endpoint identity where public
MAP15_07: world assembly exit identity and no-regression/no-fallback contract
MAP09: MicroChunk 12x8, MicroPattern 4x4, no-rotation and chunk-slice-last contract
MAP08: boundary aperture identity where public
```

MAP16_05 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP16_05 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

If live final canvas/sockets are still reference-only, use deterministic `REFERENCE GENERATED MICROCHUNK SLICE SET` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval, file export, Tilemap bake or runtime streaming.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceSet.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilder.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilderTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_05
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_05 책임 안에 머물러야 한다.

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
marker slot projection or stable spawn id records
MAP16_06+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - GeneratedMicroChunkSliceSet.cs

Create immutable value types for the MAP16_05 public surface.

Required concepts:

```text
GeneratedMicroChunkSliceId
GeneratedMicroChunkCell
GeneratedMicroChunkLayerRecord
GeneratedMicroChunkSocketSide
GeneratedMicroChunkSocketBand
GeneratedMicroChunkSocketSignature
GeneratedMicroChunkTraversalSummary
GeneratedMicroChunkSlice
GeneratedMicroChunkSliceSet
GeneratedMicroChunkSliceFailure
GeneratedMicroChunkSliceResult
GeneratedMicroChunkSliceDigest
```

Minimum constants:

```text
sector width: 48
sector height: 32
sector cells: 1536
micro chunk width: 12
micro chunk height: 8
micro chunk cells: 96
chunk grid: 4x4
chunk count: 16
chunk index: chunkY * 4 + chunkX
micro pattern size: 4x4
chunk pattern grid: 3x2
chunk rotation allowed: false
layer kinds per cell: 7
```

Minimum socket sides:

```text
Left
Right
Up
Down
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
all 16 slice ids
slice index, chunkX, chunkY, sector origin
96 unique local cell coordinates per slice
96 unique sector tile coordinates per slice
7 layer records per cell
source owner and provenance for every layer record
protection flags copied from final canvas
route/recovery membership copied from MAP16_03 projection
edge cell passability per side
socket bands per side
socket band start/end/length and source evidence
socket signature lower-hex digest per slice and per side
traversal summary per slice
total sector cell coverage 1536/1536
duplicate/missing/out-of-bounds counts
input/output digest lower-hex SHA-256
downstream owner MAP16_06
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Builder Contract - GeneratedMicroChunkSliceBuilder.cs

Implement deterministic generated slice construction without mutating MAP16_01~04 artifacts.

Required behavior:

1. Consume successful `SectorFinalCanvasLayerPlan`, `SectorCanvasProtectionDensityReport`, `SectorFinalRouteRecoveryReport`, and `SectorPatternChunkPartition`.
2. Validate exact sector size 48x32, expected cell count 1536, chunk count 16 and 96 cell addresses per chunk.
3. For each chunk slot, create one `GeneratedMicroChunkSlice` with:

```text
slice id = sector id + chunk index
chunk index = chunkY * 4 + chunkX
origin = chunk origin from MAP16_04
local bounds = 12x8
96 GeneratedMicroChunkCell records
```

4. For each slice cell, copy final canvas values from the exact sector tile coordinate:

```text
Terrain
Affordance
Material
Hazard
Marker
Protection
SourceOwner
source owner
provenance/source id
protection kind
stable claim id or cell token
```

This is an immutable data copy, not a writeback to MAP16_01.

5. Derive socket bands from actual open edge cells:

```text
Left side: localX == 0
Right side: localX == 11
Down side: localY == 0
Up side: localY == 7
open edge cell: not Solid, not blocking Hazard, not blocked Protection, or explicitly protected-open/socket/boundary route cell
contiguous open edge cells become socket bands
band source evidence references final canvas cell and route/socket/boundary authority when public
```

6. Derive socket signatures:

```text
slice signature: all 96 cell layer/source/protection tokens + all socket bands + traversal summary
side signature: side + ordered bands + edge passability tokens
lower-hex SHA-256
```

7. Derive traversal summary:

```text
local passable cell count
local blocked cell count
route/recovery witness cell count
socket-connected side count
connected passable component count within the slice
whether each socket band touches a local passable component
```

This is a static local summary. It is not player physics, jump simulation or PlayMode traversal.

8. Publish coverage and mutation counters:

```text
16 slices
96 cells per slice
1536 total sector cells covered
7 layer records per cell
0 duplicate sector cells
0 missing sector cells
0 out-of-bounds cells
0 rotation requests
0 file writes
0 Tilemap/Scene/Prefab/GameObject mutation
```

9. Produce stable canonical digest:

```text
input: MAP16_01 digest + MAP16_02 digest + MAP16_03 digest + MAP16_04 digest + constants + builder policy version
output: sorted slices + sorted cells + layer records + socket bands + signatures + traversal summaries + counters + downstream handoff
```

10. Fail atomically with no partial `GeneratedMicroChunkSliceSet` when:

```text
MAP16_01 plan is missing or failed
MAP16_02 report is missing or failed
MAP16_03 report is missing or failed
MAP16_04 partition is missing or failed
sector size != 48x32
chunk slot count != 16
slice cell count != 96 for any slice
total sector cell coverage != 1536
cell layer count != 7
source/provenance is missing for any copied layer record
duplicate, missing or out-of-bounds coordinate exists
socket band references a blocked or missing edge cell
socket signature is missing or not lower-hex SHA-256
90-degree rotation is requested or inferred
input/output digest is missing or not lower-hex SHA-256
builder would require marker slot projection, stable spawn id generation, Tilemap write, file export, generated asset, Scene/Prefab/GameObject mutation, player physics, rerender, reroll, fallback carve, silent widening, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP16_01~04 and MAP15_02/MAP08 when exposed. Do not invent production slice data when public data exists.

Allowed fixture scope:

```text
one accepted reference 48x32 final canvas plan from MAP16_01
one accepted protection/density report from MAP16_02
one accepted final route/recovery report from MAP16_03
one accepted pattern/chunk partition from MAP16_04
all 16 12x8 chunk slots
all 1536 sector tile coordinates copied into slice cell records
socket bands derived from final edge cells
synthetic invalid duplicate/missing/blocked socket/missing provenance/rotation cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual 624x416 world terrain solve
CSV/JSON/generated asset export
actual Tilemap output
marker slot projection
stable spawn id generation
actual player controller traversal
collider/physics proof
Activity/Event runtime spawn
MAP16 phase exit approval
```

## 8. Focused Test Requirements

Create `GeneratedMicroChunkSliceBuilderTests.cs` with category `MAP16_05`.

Required focused gates:

```text
GeneratedSliceSetPublishesSixteenSlicesCellsLayersSocketsAndDigests
EachSliceContainsExactlyNinetySixUniqueCellsAndSevenLayerRecordsPerCell
AllSectorCellsAreCoveredExactlyOnceWithoutGapsOverlapOrOutOfBounds
LayerSourceProtectionAndProvenanceAreCopiedFromFinalCanvasCells
SocketBandsAreDerivedOnlyFromOpenEdgeCellsOnAllFourSides
SocketSignaturesAndTraversalSummariesAreStableAndNonEmpty
RouteRecoveryWitnessMembershipProjectsIntoGeneratedSlices
InvalidSliceInputsFailAtomicallyForMissingCoverageProvenanceBlockedSocketsAndRotation
SliceBuilderDoesNotWriteFilesTilemapsScenesPrefabsGameplayOrMarkerSlots
Map16HandoffKeepsMap16_06Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
chunk grid observed: 4x4
generated slices observed: 16/16
slice dimensions observed: 12x8
cells per slice observed: 96/96
total slice cells observed: 1536/1536
unique sector cells copied: 1536/1536
duplicate sector cells: 0
missing sector cells: 0
out-of-bounds sector cells: 0
layer records per cell observed: 7/7
total layer records observed: 10752/10752
layer records with source owner: 10752/10752
layer records with provenance: 10752/10752
protected/provenance mismatch with MAP16_01: 0
route/recovery witness memberships copied: actual/actual
socket sides required/covered/missing: 4/4/0
socket bands derived: actual
socket bands on blocked cells: 0
socket signatures missing/invalid: 0
slice signatures missing/invalid: 0
traversal summaries missing: 0
passable component summaries missing: 0
90-degree rotation requests: 0
marker slot records created: 0
stable spawn ids created: 0
Tilemap bakes: 0
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
slices sorted by chunk index
cells sorted by local row-major coordinate
layer records sorted by layer kind
socket sides sorted Left, Right, Down, Up or stable enum order documented in Result
socket bands sorted by side then start coordinate
traversal summaries sorted by slice index
route/recovery memberships sorted by witness kind, source stable id, local coordinate
failure records sorted by code, subject, reason
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing fixture label may change only declared synthetic socket/cell evidence. It must not change public topology constants, MAP16_01 canvas digest, MAP16_02 protection-density digest, MAP16_03 route-recovery digest, MAP16_04 partition digest, MAP09 MicroChunk/MicroPattern constants, or MAP15_02/MAP08 socket/boundary authority when public.

## 10. No Mutation Proof

MAP16_05 must prove it does not write or mutate:

```text
MAP16_01 final canvas layer plan
MAP16_02 protection-density report
MAP16_03 route-recovery report
MAP16_04 pattern/chunk partition
MAP15_01~07 world assembly outputs
MAP14 sector planner outputs
MAP10 MicroPattern authoring/runtime outputs
MAP09 constants/pass catalog
MAP08 boundary authoring CSV/cache
MAP07 fixed slice/canvas authority files
MAP09~14 authoring CSV/cache
Generated CSV files
debug export files
JSON files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
marker slot or stable spawn id records
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The builder may allocate in-memory immutable slice values. No generated file export, no Tilemap write, no marker slot projection, no stable spawn id generation, no player physics and no MAP16_06 task execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
STATUS: PASS | FAIL | BLOCKED
MAP16_05: COMPLETE ELIGIBLE only when PASS
MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 in-memory 96-cell slice/socket derivation contract이며 export/Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- slice count, cell count, layer record count
- source owner/provenance copy evidence
- socket side/band/signature count
- traversal summary count
- route/recovery witness membership copy evidence
- duplicate/missing/out-of-bounds count 0
- rotation request count 0
- marker slot/stable spawn id/file write/Tilemap count 0
- input/output digest
- deterministic replay evidence
- mutation/file-write/Tilemap/Scene/Prefab/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 아직 구현하지 않은 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script path
- class/method별 책임
- helper/probe별 input -> output
- public authority consumed
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP16_06

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_05]
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
Commit subject: MAP16_05: build generated microchunk slices
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_06.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS.md
MCP_ARCHIVE/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS.md
MCP/REPORTS/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceSet.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilder.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMicroChunkSliceBuilderTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_06: do not start
STOP after Result and optional PASS finalize commit
```
