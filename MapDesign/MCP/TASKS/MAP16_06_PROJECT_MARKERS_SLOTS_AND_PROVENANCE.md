```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
  task_file: TASKS/MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE.md
  requires_current_task: NONE
  requires_completed_task: MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS
  requires_result:
    path: REPORTS/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS_RESULT.md
    status: PASS
    sha256: e692dab7e5d446edd6c07baafa6aad3b1f7ae48469987f4cb53ec13892e6db56
  requires_installed_task:
    path: TASKS/MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS.md
    sha256: 4a706cd0a27f4d16ffa5f3328ce47062d42fed408c5b610a968744e1623f1f8f
  sets_current_task: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
```

# MAP16_06 - Project Markers Slots and Provenance

```text
TASK: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_05의 `GeneratedMicroChunkSliceSet` 안에 보존된 layer/source/provenance 정보를 읽어, generated 12x8 MicroChunk cell 위에 marker-like records를 local slot으로 투영한다.

```text
MAP16_05 GeneratedMicroChunkSliceSet
MAP16_05 GeneratedMicroChunkSliceRecord / GeneratedMicroChunkCell / GeneratedMicroChunkLayerRecord
MAP11 TerrainCluster ownership markers where public
MAP12 Activity/EventOverlay ownership markers where public
MAP13 SpecialRegion ownership markers where public
MAP14/MAP15 sector/world placement evidence where public
-> GeneratedMicroChunkMarkerSlotSet
-> GeneratedMicroChunkMarkerSlotProjector
-> MAP16_07 replay/debug export input
```

이번 Task는 **slice-local marker slot과 provenance packet**만 소유한다. stable spawn id, 실제 runtime spawn, GameObject 생성, CSV/JSON/debug file export, Tilemap bake, save/streaming은 구현하지 않는다.

MAP16_06이 승인해야 하는 핵심:

```text
MAP16_05의 16개 slice / 1536 cell / 10752 layer record를 그대로 입력으로 사용한다.
marker-like layer/source record는 결정론적인 local slot id로 투영된다.
slot은 chunk index, local x/y, sector tile x/y를 모두 보존한다.
slot provenance는 source owner, source task, layer kind, source token, claim/evidence id, slice id, cell coordinate를 포함한다.
Cluster, Activity, SpecialRegion, EventOverlay 계열 marker가 public record에서 발견되면 모두 타입별 slot으로 보존된다.
동일 cell의 서로 다른 marker kind는 공존할 수 있지만, 같은 owner/kind/source key duplicate은 실패한다.
orphan marker, missing provenance, missing cell ref, duplicate slot id는 partial output 없이 atomic failure다.
slot digest는 repeat, reverse input, culture 변경에서 동일하다.
stable spawn id와 runtime object는 생성하지 않는다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, marker consumed/projected 수치, slot/provenance/digest 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| marker-like layer/source records -> generated local slot projection | stable spawn id |
| slot kind, owner, local id, cell ref, source key | runtime spawn / gameplay object creation |
| slot provenance chain | CSV/JSON/generated asset export |
| duplicate/orphan/missing provenance failure | Tilemap/collider/physics bake |
| deterministic slot digest and aggregate counters | Scene/Prefab/GameObject mutation |
| focused EditMode tests for MAP16_06 | save/streaming/runtime state |
| MAP16_07 handoff contract | MAP16_07 execution / MAP16 phase exit |

`GeneratedMicroChunkMarkerSlotSet` is still an in-memory generated terrain data packet. It identifies where downstream systems may attach content, but it is not allowed to instantiate or persist that content.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_06`만 선택한다.

```text
MAP16_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03/MAP16_04/MAP16_05 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_06` category로 제한한다.

신규 task-owned failure는 신규 MAP16_06 allowlist 파일만 수정하고 `MAP16_06` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_05 slice digest/count mismatch, marker provenance가 upstream에서 public으로 노출되지 않는 구조적 문제, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_05 Result: PASS
MAP16_05 Result SHA-256:
e692dab7e5d446edd6c07baafa6aad3b1f7ae48469987f4cb53ec13892e6db56

MAP16_05 installed Task SHA-256:
4a706cd0a27f4d16ffa5f3328ce47062d42fed408c5b610a968744e1623f1f8f

MAP16_05 COMPLETE / MAP16_06 CURRENT / MAP16_07 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_05: GeneratedMicroChunkSliceSet, 16 slice records, cells, layer records, socket/traversal identities
MAP16_01: final canvas layer owner/protection/provenance facts as preserved by MAP16_05
MAP16_03: route/recovery witness identity as preserved by MAP16_05
MAP11: TerrainCluster marker ownership terms where public
MAP12: Activity and EventOverlay marker ownership terms where public
MAP13: SpecialRegion marker ownership terms where public
MAP14/MAP15: sector/world placement evidence where public
MAP09: MicroChunk 12x8, chunk-slice-last contract
```

MAP16_06 must consume public values. Do not inspect private fields. Do not reparse physical CSV unless an approved public importer/API exposes that data as the source of truth. If live marker data is still reference-only, use deterministic `REFERENCE GENERATED MICROCHUNK MARKER SLOT SET` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval, file export, Tilemap bake, runtime spawn or stable spawn id approval.

If MAP16_05 renamed its richer immutable slice type to avoid public symbol collision, consume the actual public type exposed by `GeneratedMicroChunkSliceSet.Slices`; do not modify MAP16_05 files to rename it.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotSet.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotProjector.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotProjectorTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_06
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_06 책임 안에 머물러야 한다.

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
stable spawn id records
runtime spawned objects
MAP16_07+ files
```

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Model Contract - GeneratedMicroChunkMarkerSlotSet.cs

Create immutable value types for the MAP16_06 public surface.

Required concepts:

```text
GeneratedMarkerSlotKind
GeneratedMarkerSlotOwner
GeneratedMarkerSlotId
GeneratedMarkerSlotCellRef
GeneratedMarkerSlot
GeneratedMarkerSlotProvenance
GeneratedMarkerSlotProjection
GeneratedMicroChunkMarkerSlotSet
MarkerSlotProjectionFailure
MarkerSlotProjectionResult
MarkerSlotProjectionDigest
```

Minimum required slot kinds:

```text
TerrainCluster
Activity
SpecialRegion
EventOverlay
```

Optional public source kinds may also be represented when already exposed:

```text
Boundary
RouteRecovery
Decoration
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
source slice set id/digest
all 16 slice ids
slot id
slot kind
slot owner
slot source key
chunk index
local x/y inside 12x8 chunk
sector tile x/y
source layer kind
source owner token
source task id
source claim/evidence id when present
source cell token
source slice id
source socket/signature/traversal identity when present
projection ordinal for compatible multi-marker cells
duplicate/orphan/missing counts
input/output digest lower-hex SHA-256
downstream owner MAP16_07
```

Slot id must be stable for the same input, but it is not a gameplay spawn id. Use only deterministic data such as sector id, chunk index, local coordinate, kind and ordinal/source key. Do not use time, random, filesystem paths, Unity instance ids, object references or Dictionary iteration order.

## 6. Projector Contract - GeneratedMicroChunkMarkerSlotProjector.cs

Implement a deterministic projector.

Required behavior:

```text
null input -> failure with null SlotSet
non-complete or invalid source slice set -> failure with null SlotSet
missing source slice/cell/layer provenance -> failure with null SlotSet
marker-like record without cell ref -> failure with null SlotSet
duplicate slot id -> failure with null SlotSet
duplicate same owner/kind/source key in one cell -> failure with null SlotSet
compatible different marker kinds in one cell -> allowed and sorted by kind/source key
slot ids sorted by chunk index, local y, local x, kind, owner/source key, ordinal
all failures sorted by stable token
success -> complete SlotSet with no partial failure data
failure -> no partial SlotSet and empty output digest
```

Marker-like records must be detected from public MAP16_05 layer/source/provenance records. Do not infer markers from visual color, debug text, scene objects, GameObject names or file paths.

At minimum, support these public ownership families when they appear in the source record:

```text
TerrainCluster / Cluster / Terrain
Activity / ActivityStructure
SpecialRegion / Special
EventOverlay / Event
```

If the public source data uses different exact names, create a small deterministic mapping inside MAP16_06 and report the mapping in Result. Do not change upstream names.

## 7. Digest and Determinism Contract

Canonicalize all digest payloads with:

```text
UTF-8
LF line endings
InvariantCulture
lower-hex SHA-256
stable enum names
sorted slices by chunk index
sorted cells by local row-major
sorted slots by stable slot id
sorted provenance tokens
```

The digest must be independent of:

```text
input enumeration order
current culture / UI culture
current time
Unity object instance id
filesystem path
Dictionary or HashSet iteration order
```

## 8. Focused Tests - GeneratedMicroChunkMarkerSlotProjectorTests.cs

Create focused EditMode tests with category `MAP16_06`.

Required test names:

```text
MarkerSlotSetPublishesClusterActivitySpecialEventSlotsAndDigests
MarkerLayerRecordsProjectToStableSliceLocalSlotIds
SlotCellReferencesRoundTripToSectorChunkAndLocalCoordinates
SlotProvenanceTracksSourceOwnerTaskLayerClaimAndSliceCell
DuplicateAndOrphanMarkersFailAtomicallyWithoutPartialSlotSet
MarkerProjectionPreservesSliceCellLayerSocketAndTraversalIdentities
MarkerSlotDigestIsDeterministicAcrossRepeatReverseAndCulture
ProjectorDoesNotCreateStableSpawnIdsRuntimeObjectsFilesTilemapsOrScenes
ProjectorDoesNotMutateSlicesCanvasPartitionOrAuthoringAssets
Map16HandoffKeepsMap16_07Locked
```

The tests must exercise at least:

```text
16/16 source slices
1536/1536 source cells
10752/10752 source layer records observed
required marker owner families covered: TerrainCluster, Activity, SpecialRegion, EventOverlay
multiple compatible marker kinds in the same cell
duplicate same owner/kind/source key failure
orphan/missing provenance failure
repeat/reverse/culture digest stability
non-mutation counters
MAP16_07 locked handoff
```

Do not add PlayMode tests.

## 9. Minimum Result Evidence

Result must include these fields with actual values:

```text
source slices observed: 16/16
source cells observed: 1536/1536
source layer records observed: 10752/10752
marker layer records scanned: actual
marker layer records consumed: actual
required marker owner families required/covered/missing: 4/4/0
optional marker owner families observed: actual
slots projected: actual
slots with stable local id: actual/actual
slots with cell refs: actual/actual
slots with provenance: actual/actual
slots preserving source layer identity: actual/actual
slots preserving socket/signature/traversal identity where applicable: actual
compatible multi-marker cells: actual
duplicate slot ids: 0
duplicate same owner/kind/source key failures verified: actual
orphan marker records: 0
missing provenance: 0
stable spawn ids created: 0
runtime objects spawned: 0
CSV/JSON generated files: 0
Tilemap bakes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
source slice mutation: 0
input digest: lower-hex
output digest: lower-hex
repeat/reverse/culture digest mismatches: 0/0/0
production seed approvals: 0
```

Focused verification block:

```text
Unity version: actual
mode: EditMode
category_names: [MAP16_06]
discovered: actual
executed: actual
passed: actual
failed: 0
skipped: actual
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

If a real defect forces broader verification, do not run it silently. Mark `REGRESSION TRIGGER DETECTED: YES`, explain why, and STOP unless the task-owned focused proof can still complete without broader selection.

## 10. Commit and Stop

On PASS:

```text
write REPORTS/MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE_RESULT.md
update status MAP16_06 COMPLETE
keep MAP16_07 LOCKED
atomic commit only MAP16_06 files, task/status/report files, and generated meta for new files
commit subject: MAP16_06: project marker slots provenance
STOP
```

Do not start MAP16_07.

Git push is forbidden.

