```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
  task_file: TASKS/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION.md
  requires_current_task: NONE
  requires_completed_task: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
  requires_result:
    path: REPORTS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY_RESULT.md
    status: PASS
    sha256: 2376fc1a00f5b8bcefaf78214313e46f1d827b5ddcc517c1225a2c5ba726835f
  requires_installed_task:
    path: TASKS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
    sha256: 20ce61eb66fa50528450da275901798f0d07e2e398e083614ec4d90b3b81232f
  sets_current_task: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
```

# MAP16_04 - Implement Pattern Chunk Coordinates and Partition

```text
TASK: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_01~03에서 검증된 48x32 final canvas를 12x8 MicroChunk 좌표계로 안전하게 나누기 위한 coordinate partition 계약을 만든다.

```text
MAP16_01 SectorFinalCanvasLayerPlan
MAP16_02 SectorCanvasProtectionDensityReport
MAP16_03 SectorFinalRouteRecoveryReport
MAP09 MicroPattern/MicroChunk constants
MAP10 4x4 MicroPattern coordinate authority where public
-> SectorPatternChunkPartition
-> SectorPatternChunkPartitioner
-> MAP16_05 96-cell slice builder input
```

이번 Task는 **좌표 변환과 partition map**만 소유한다. 실제 96-cell slice record를 만들거나, layer/provenance/socket을 복사하거나, Tilemap을 굽거나, CSV/JSON/Generated asset을 쓰거나, Scene/Prefab/GameObject/gameplay runtime을 변경하지 않는다.

MAP16_04가 승인해야 하는 핵심:

```text
48x32 sector는 4x4 chunk grid의 16개 12x8 chunk slot으로 분할된다.
chunk index는 반드시 (chunkY * 4 + chunkX)이다.
각 12x8 chunk slot은 96 tile coordinate를 가진다.
sector 전체 1536 tile coordinate는 중복/누락 없이 정확히 한 chunk slot에 속한다.
4x4 MicroPattern coordinate는 sector 기준 12x8 pattern grid, chunk 기준 3x2 pattern grid로 round-trip된다.
12x8 MicroChunk는 90도 회전되지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, chunk/pattern/cell partition 수치, round-trip 수치, rotation 금지 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 48x32 sector -> 16 chunk slot coordinate partition | actual 96-cell slice layer copy |
| 12x8 chunk coordinate/index contract | derived socket band/signature/traversal |
| 4x4 MicroPattern -> 12x8 MicroChunk round-trip | Generated CSV/JSON export |
| tile coordinate -> chunk/local coordinate mapping | Tilemap bake |
| pattern coordinate -> chunk/local pattern mapping | collider/physics/player traversal |
| route/recovery witness coordinate projection into chunk slots | Scene/Prefab/GameObject mutation |
| deterministic partition digest | Activity/Event/NPC/reward gameplay spawn |
| focused EditMode tests for MAP16_04 | MAP16 phase exit / production seed approval |
| MAP16_05 handoff contract | MAP16_05 execution |

`SectorPatternChunkPartition` is a coordinate partition packet. It can publish chunk slots and coordinate transforms, but it cannot copy final canvas layer cells into generated MicroChunk records. That copy belongs to MAP16_05.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_04`만 선택한다.

```text
MAP16_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_04` category로 제한한다.

신규 task-owned failure는 신규 MAP16_04 allowlist 파일만 수정하고 `MAP16_04` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_01 canvas digest/count mismatch, MAP16_02 protection-density contradiction, MAP16_03 route-recovery contradiction, MAP09/MAP10 coordinate constant contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_03 Result: PASS
MAP16_03 Result SHA-256:
2376fc1a00f5b8bcefaf78214313e46f1d827b5ddcc517c1225a2c5ba726835f

MAP16_03 installed Task SHA-256:
20ce61eb66fa50528450da275901798f0d07e2e398e083614ec4d90b3b81232f

MAP16_03 COMPLETE / MAP16_04 CURRENT / MAP16_05 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_01: SectorFinalCanvasLayerPlan, 48x32 final cell coordinates and source digests
MAP16_02: SectorCanvasProtectionDensityReport, accepted validation identity
MAP16_03: SectorFinalRouteRecoveryReport, route/recovery witness coordinates
MAP09: MicroPattern 4x4, MicroChunk 12x8, sector 48x32, chunk-slice-last contract
MAP10: MicroPattern 4x4 cell coordinate authority where public
MAP15_07: world assembly exit identity and no-regression/no-fallback contract
```

MAP16_04 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, add a small task-owned projection only inside the new MAP16_04 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

If live final canvas route coordinates are still reference-only, use deterministic `REFERENCE PATTERN CHUNK PARTITION` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval or actual generated slice output.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartition.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartitioner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorPatternChunkPartitionerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_04
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_04 책임 안에 머물러야 한다.

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
96-cell slice layer/provenance/socket records
MAP16_05+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - SectorPatternChunkPartition.cs

Create immutable value types for the MAP16_04 public surface.

Required concepts:

```text
SectorTileCoordinate
MicroPatternCoordinate
MicroPatternLocalCellCoordinate
MicroChunkCoordinate
MicroChunkLocalTileCoordinate
MicroChunkLocalPatternCoordinate
MicroChunkSlot
PatternChunkCellAddress
PatternChunkPatternAddress
SectorPatternChunkPartition
PatternChunkPartitionFailure
PatternChunkPartitionResult
PatternChunkPartitionDigest
```

Minimum constants:

```text
sector width: 48
sector height: 32
sector cells: 1536
micro pattern width: 4
micro pattern height: 4
sector pattern grid: 12x8
sector pattern cells: 96
micro chunk width: 12
micro chunk height: 8
chunk grid: 4x4
chunk count: 16
chunk cells: 96
chunk pattern grid: 3x2
chunk pattern cells: 6
chunk index: chunkY * 4 + chunkX
chunk rotation allowed: false
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
all 16 chunk slots
chunk index, chunkX, chunkY
chunk origin tile coordinate
chunk bounds: minX/minY/width/height
96 tile coordinate addresses per chunk
6 pattern coordinate addresses per chunk
tile sector coordinate -> chunk index -> local tile coordinate -> sector coordinate round-trip
pattern sector coordinate -> chunk index -> local pattern coordinate -> sector pattern coordinate round-trip
pattern local 4x4 cell -> sector tile coordinate round-trip
route/recovery witness tile coordinates projected to chunk slots
coverage count, duplicate count, missing count, out-of-bounds count
input/output digest lower-hex SHA-256
downstream owner MAP16_05
```

Value objects must be immutable after construction. Collections returned publicly must be read-only or copied. Digest generation must use stable sorted canonical text and invariant culture.

## 6. Partitioner Contract - SectorPatternChunkPartitioner.cs

Implement deterministic coordinate partitioning without mutating MAP16_01~03 artifacts.

Required behavior:

1. Consume successful `SectorFinalCanvasLayerPlan`, `SectorCanvasProtectionDensityReport`, and `SectorFinalRouteRecoveryReport`.
2. Validate exact sector size 48x32 and expected cell count 1536.
3. Validate MicroPattern 4x4 and MicroChunk 12x8 constants divide the sector exactly:

```text
48 % 12 == 0
32 % 8 == 0
12 % 4 == 0
8 % 4 == 0
```

4. Enumerate chunk slots row-major:

```text
chunkX: 0..3
chunkY: 0..3
chunkIndex = chunkY * 4 + chunkX
originX = chunkX * 12
originY = chunkY * 8
```

5. Map every sector tile coordinate:

```text
chunkX = tileX / 12
chunkY = tileY / 8
chunkIndex = chunkY * 4 + chunkX
localX = tileX % 12
localY = tileY % 8
round-trip tileX = chunkX * 12 + localX
round-trip tileY = chunkY * 8 + localY
```

6. Map every sector MicroPattern coordinate:

```text
patternX = tileX / 4
patternY = tileY / 4
sector pattern grid = 12x8
chunkX = patternX / 3
chunkY = patternY / 2
chunkIndex = chunkY * 4 + chunkX
localPatternX = patternX % 3
localPatternY = patternY % 2
round-trip patternX = chunkX * 3 + localPatternX
round-trip patternY = chunkY * 2 + localPatternY
```

7. Map every 4x4 local pattern cell:

```text
localCellX = tileX % 4
localCellY = tileY % 4
tileX = patternX * 4 + localCellX
tileY = patternY * 4 + localCellY
```

8. Publish coverage:

```text
16 chunk slots
96 tile coordinates per chunk
6 MicroPattern coordinates per chunk
1536 unique tile coordinate assignments
96 unique MicroPattern coordinate assignments
0 duplicates
0 missing
0 out of bounds
0 90-degree rotations
```

9. Project route/recovery witness coordinates from MAP16_03 into chunk slots. This projection is only an address map. It cannot build slice traversal/socket payloads.
10. Produce stable canonical digest:

```text
input: MAP16_01 digest + MAP16_02 digest + MAP16_03 digest + constants + partition policy version
output: sorted chunk slots + tile addresses + pattern addresses + witness projections + counters + downstream handoff
```

11. Fail atomically with no partial `SectorPatternChunkPartition` when:

```text
MAP16_01 plan is missing or failed
MAP16_02 report is missing or failed
MAP16_03 report is missing or failed
sector size != 48x32
cell count != 1536
MicroPattern or MicroChunk constants do not divide the sector exactly
chunk index is not chunkY * 4 + chunkX
chunk slot count != 16
tile assignment count != 1536
pattern assignment count != 96
duplicate, missing or out-of-bounds tile coordinate exists
duplicate, missing or out-of-bounds pattern coordinate exists
tile round-trip mismatch exists
pattern round-trip mismatch exists
90-degree rotation is requested or inferred
input/output digest is missing or not lower-hex SHA-256
partitioner would require layer copy, socket derivation, Tilemap write, file export, generated asset, Scene/Prefab/GameObject mutation, player physics, rerender, reroll, fallback carve, silent widening, or full regression
```

No `System.IO`, no current time, no random API, no Unity object instance IDs, no filesystem path separators in digest payload.

## 7. Existing Authority and Fixture Policy

Prefer current public authorities from MAP16_01~03 and MAP09/MAP10 when exposed. Do not invent production slice data when public data exists.

Allowed fixture scope:

```text
one accepted reference 48x32 final canvas plan from MAP16_01
one accepted reference protection/density report from MAP16_02
one accepted reference route/recovery report from MAP16_03
all 1536 sector tile coordinates
all 96 sector MicroPattern coordinates
all 16 12x8 chunk slots
route/recovery witness coordinate projection into chunk slots
synthetic invalid coordinate, duplicate, missing, non-divisible and rotation cases for atomic failure tests
```

Forbidden fixture claims:

```text
production seed approval
actual 624x416 world terrain solve
actual 96-cell slice layer/provenance/socket output
actual Tilemap output
actual player controller traversal
collider/physics proof
Activity/Event runtime spawn
MAP16 phase exit approval
```

## 8. Focused Test Requirements

Create `SectorPatternChunkPartitionerTests.cs` with category `MAP16_04`.

Required focused gates:

```text
PatternChunkPartitionPublishesConstantsSlotsCoverageAndDigests
SectorTilesPartitionIntoSixteenTwelveByEightChunksWithoutGapsOrOverlap
ChunkIndexUsesChunkYTimesFourPlusChunkXForEverySlot
TileCoordinatesRoundTripThroughChunkAndLocalTileAddresses
MicroPatternCoordinatesRoundTripThroughChunkLocalPatternAndLocalCellAddresses
EachChunkContainsExactlyNinetySixTilesAndSixMicroPatterns
RouteRecoveryWitnessCoordinatesProjectIntoChunkSlotsWithoutMutation
InvalidPartitionInputsFailAtomicallyForBadCountsDuplicatesMissingAndRotation
PartitionerDoesNotBuildSlicesSocketsTilemapsFilesScenesOrGameplayObjects
Map16HandoffKeepsMap16_05Locked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

Minimum verification evidence:

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
micro pattern size observed: 4x4
sector pattern grid observed: 12x8
sector pattern count observed: 96/96
micro chunk size observed: 12x8
chunk grid observed: 4x4
chunk slots observed: 16/16
chunk cells each: 96/96
chunk pattern cells each: 6/6
tile assignments observed: 1536/1536
pattern assignments observed: 96/96
duplicate tile assignments: 0
missing tile assignments: 0
out-of-bounds tile assignments: 0
duplicate pattern assignments: 0
missing pattern assignments: 0
out-of-bounds pattern assignments: 0
chunk index mismatches: 0
tile round-trip mismatches: 0
pattern round-trip mismatches: 0
local 4x4 cell round-trip mismatches: 0
90-degree rotation requests: 0
route/recovery witness coordinate projection missing: 0
96-cell slice records created: 0
socket derivations created: 0
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
chunk slots sorted by chunk index
tile addresses sorted by row-major sector tile coordinate
pattern addresses sorted by row-major sector pattern coordinate
local pattern cell addresses sorted by pattern coordinate then local cell coordinate
witness projections sorted by witness kind, source stable id, coordinate
failure records sorted by code, subject, reason
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Changing fixture label may change only declared synthetic coordinate evidence. It must not change public topology constants, MAP16_01 canvas digest, MAP16_02 protection-density digest, MAP16_03 route-recovery digest, MAP09 MicroChunk/MicroPattern constants or MAP10 pattern coordinate identity when public.

## 10. No Mutation Proof

MAP16_04 must prove it does not write or mutate:

```text
MAP16_01 final canvas layer plan
MAP16_02 protection-density report
MAP16_03 route-recovery report
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
96-cell slice assets or records
socket/signature/traversal records
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

The partitioner may allocate in-memory immutable coordinate values. No generated file export, no Tilemap write, no actual 96-cell slice creation, no socket derivation, no player physics and no MAP16_05 task execution is allowed in this Task.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION
STATUS: PASS | FAIL | BLOCKED
MAP16_04: COMPLETE ELIGIBLE only when PASS
MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 coordinate partition contract이며 96-cell slice/socket/Tilemap/Scene/gameplay가 아니라는 점
- 추가한 script 목록과 각 script 책임
- 새로 가능해진 기능
- sector, MicroPattern, MicroChunk, chunk grid constants
- chunk slot count and index formula evidence
- tile assignment coverage/duplicate/missing/out-of-bounds counts
- pattern assignment coverage/duplicate/missing/out-of-bounds counts
- tile/pattern/local 4x4 round-trip evidence
- route/recovery witness projection evidence
- 90-degree rotation request count 0
- 96-cell slice/socket/Tilemap/file write count 0
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
- downstream owner: MAP16_05

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_04]
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
Commit subject: MAP16_04: implement pattern chunk partition
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_05.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION.md
MCP_ARCHIVE/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION.md
MCP/REPORTS/MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartition.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartitioner.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartitioner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorPatternChunkPartitionerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorPatternChunkPartitionerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_05: do not start
STOP after Result and optional PASS finalize commit
```
