```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN
  task_file: TASKS/MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN.md
  requires_current_task: NONE
  requires_completed_task: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
  requires_result:
    path: REPORTS/MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE_RESULT.md
    status: PASS
    sha256: 752060bb69c07620f484085fa262c900f89038e211555d580c2f8d018b3bbda8
  requires_installed_task:
    path: TASKS/MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE.md
    sha256: ba1d75d42525a98cdfa5ed3944578abec2eafd50a8d004f75edca73fa75ad3a2
  sets_current_task: MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN
```

# MAP16_07 - Export Replay and Debug Generated Terrain

```text
TASK: MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_05 slice packet과 MAP16_06 marker slot packet을 사람이 검토하고 재현 검증할 수 있도록 deterministic CSV export, hash replay verifier, Canvas/Slice debug overlay packet을 만든다.

```text
MAP16_05 GeneratedMicroChunkSliceSet
MAP16_06 GeneratedMicroChunkMarkerSlotSet
-> GeneratedTerrainExportPacket
-> GeneratedTerrainCsvExporter
-> GeneratedTerrainReplayVerifier
-> GeneratedTerrainDebugOverlay
-> MAP16_08 exit-test input
```

이번 Task는 **generated terrain의 debug/export/replay contract**만 소유한다. Tilemap bake, collider/physics, runtime streaming, save/load, EditorWindow, Scene/Prefab/GameObject 변경, stable spawn id, gameplay spawn, production seed approval은 구현하지 않는다.

MAP16_07이 승인해야 하는 핵심:

```text
plan/slice/cell/socket/slot CSV payload를 결정론적으로 만든다.
CSV writer는 caller-provided output directory에만 쓴다.
Authoring CSV로 역수입하거나 기존 Authoring/Generated asset을 수정하지 않는다.
hash replay verifier는 exported CSV만 읽어 source digest와 output digest를 재검증한다.
Canvas overlay는 48x32 / 1536 cell 전체를 덮는다.
Slice overlay는 16개 12x8 / 96 cell 전체를 덮는다.
Overlay는 text/model packet이며 EditorWindow나 Scene object가 아니다.
temp export round-trip은 repeat, reverse input, culture, temp path 변경에서 동일하다.
```

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력->출력, CSV 파일/row/digest 수치, replay/overlay 수치, mutation 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| deterministic export packet model | Tilemap bake |
| plan/slice/cell/socket/slot CSV serialization | collider/physics/player traversal |
| caller-provided directory CSV writer | runtime streaming/save/load |
| CSV-only hash replay verifier | stable spawn id |
| 48x32 Canvas debug overlay packet | gameplay spawn / object attachment |
| 16개 12x8 Slice debug overlay packet | EditorWindow / Inspector / Scene overlay |
| focused EditMode tests for MAP16_07 | Scene/Prefab/GameObject mutation |
| MAP16_08 handoff contract | MAP16 phase exit approval |

`GeneratedTerrainExportPacket` is a reproducible debug artifact. It may be written to a temporary or user-selected directory through the new exporter, but it must not silently create or mutate project Authoring/Generated assets.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_07`만 선택한다.

```text
MAP16_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03/MAP16_04/MAP16_05/MAP16_06 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_07` category로 제한한다.

신규 task-owned failure는 신규 MAP16_07 allowlist 파일만 수정하고 `MAP16_07` category만 재실행한다.

upstream public API defect, existing slice/slot digest mismatch, CSV replay contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_06 Result: PASS
MAP16_06 Result SHA-256:
752060bb69c07620f484085fa262c900f89038e211555d580c2f8d018b3bbda8

MAP16_06 installed Task SHA-256:
ba1d75d42525a98cdfa5ed3944578abec2eafd50a8d004f75edca73fa75ad3a2

MAP16_06 COMPLETE / MAP16_07 CURRENT / MAP16_08 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_05: GeneratedMicroChunkSliceSet, slice/cell/layer/socket/traversal digests
MAP16_06: GeneratedMicroChunkMarkerSlotSet, slot/provenance/digest packet
MAP16_01~04: source authority identity as preserved by MAP16_05
MAP11~15: owner/source identity as preserved by MAP16_05 and MAP16_06
MAP09: MicroChunk 12x8, MicroPattern 4x4, chunk-slice-last contract
```

MAP16_07 must consume public values. Do not inspect private fields. Do not reparse physical Authoring CSV. If live generated terrain data is still reference-only, use deterministic `REFERENCE GENERATED TERRAIN EXPORT` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval, Tilemap bake, runtime streaming or gameplay approval.

## 4. Exact Write Boundary

정상 범위는 Runtime production 4개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainExportPacket.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainCsvExporter.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainReplayVerifier.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainDebugOverlay.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainExportReplayTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_07
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 더 많은 Runtime production C# 파일이 필요하면 Result에서 이유와 public surface를 보고하고, 기존 파일 수정 없이 MAP16_07 책임 안에 머물러야 한다.

수정·생성 금지:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/* existing files
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/*
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/*
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/*
Assets/_Game/Map/Runtime/WorldGeneration/Activities/*
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/*
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/*
Assets/_Game/Map/Data/WorldGeneration/Authoring/**
Assets/_Game/Map/Data/WorldGeneration/Generated/**
Assets/_Game/Editor/**
Assets/_Game/Tests/PlayMode/**
existing C# / test / CSV / meta
Scenes / Prefabs / Tilemaps / ScriptableObjects
asmdef / asmref / ProjectSettings / Packages
textures, screenshots, generated report assets
stable spawn id records
runtime spawned objects
MAP16_08+ files
```

Focused tests may write temporary CSV files only under a disposable temp directory created by the test. Temporary files must be cleaned up or reported if cleanup fails. No generated CSV file may be committed in this Task.

If an existing code file must be changed to compile, do not change it. Report `BLOCKED` with exact symbol/API needed.

## 5. Export Packet Contract - GeneratedTerrainExportPacket.cs

Create immutable value types for the MAP16_07 public surface.

Required concepts:

```text
GeneratedTerrainExportManifest
GeneratedTerrainPlanRow
GeneratedTerrainSliceRow
GeneratedTerrainCellRow
GeneratedTerrainSocketRow
GeneratedTerrainSlotRow
GeneratedTerrainExportPacket
GeneratedTerrainExportFile
GeneratedTerrainExportResult
GeneratedTerrainExportFailure
GeneratedTerrainExportDigest
```

Required logical CSV files:

```text
generated_terrain_manifest.csv
generated_terrain_plan.csv
generated_terrain_slices.csv
generated_terrain_cells.csv
generated_terrain_sockets.csv
generated_terrain_slots.csv
```

Minimum public fields/properties must allow tests and later Tasks to verify:

```text
format version
task id MAP16_07
source slice set digest
source marker slot set digest
sector width/height/cell count
chunk grid width/height/count
micro chunk width/height/cell count
micro pattern size
slice row count
cell row count
socket row count
slot row count
per-file row count
per-file payload digest
manifest digest
packet digest
```

Rows must preserve:

```text
slice id, chunk index, chunk x/y, sector origin
cell local x/y, sector tile x/y, passable/protection/source summary
cell layer identity/provenance summary sufficient for replay digest
socket side, band count, band range, side signature, slice signature
slot id, kind, owner, source key, cell ref, provenance digest
```

Do not store absolute filesystem paths in digests.

## 6. CSV Exporter Contract - GeneratedTerrainCsvExporter.cs

Implement deterministic CSV serialization and a caller-provided directory writer.

Required behavior:

```text
null source slice set or slot set -> failure with no partial packet
non-complete source packet -> failure with no partial packet
source digest mismatch -> failure with no partial packet
CSV header order is fixed and documented in code
CSV rows are sorted by semantic stable keys
all strings are escaped through a CSV-safe writer, not ad hoc concatenation
line endings are LF
encoding is UTF-8 without BOM unless existing project convention requires otherwise
writer creates only the requested six CSV files in the provided directory
writer refuses empty, project Authoring, project Generated, Assets root, Scenes, Prefabs or package paths
writer never calls AssetDatabase or imports CSV back into Authoring
success returns file names, row counts and per-file digests
failure returns sorted failures and no partial success result
```

Use `System.IO` only inside this new task-owned exporter/replay surface. The tests must prove no project asset path was mutated.

## 7. Replay Verifier Contract - GeneratedTerrainReplayVerifier.cs

Implement CSV-only hash replay.

Required behavior:

```text
read exactly the six required CSV files from a provided directory
reject missing, extra, duplicated, malformed or tampered files
reject row count mismatch
reject per-file digest mismatch
reject manifest digest mismatch
recompute packet digest from CSV rows
compare recomputed digest to manifest packet digest
return pass/fail evidence without mutating source packets or project files
```

Replay verifier must not call source builders to "repair" the export. It verifies the exported CSV as exported.

## 8. Debug Overlay Contract - GeneratedTerrainDebugOverlay.cs

Create in-memory overlay packet models for human inspection.

Required concepts:

```text
GeneratedTerrainCanvasOverlay
GeneratedTerrainSliceOverlay
GeneratedTerrainOverlayCell
GeneratedTerrainOverlayLegend
GeneratedTerrainOverlayResult
```

Minimum overlay coverage:

```text
Canvas overlay: 48x32, 1536 cells
Slice overlays: 16 overlays, each 12x8 / 96 cells
slot overlay: all MAP16_06 slots
socket overlay: all MAP16_05 side signatures and bands
protection overlay: protected/passable/blocked summaries
route/recovery overlay: witness cells where public
```

Overlay output is a deterministic model and optional text grid string. It is not an EditorWindow, Scene overlay, Tilemap, texture or screenshot.

## 9. Digest and Determinism Contract

Canonicalize all digest payloads with:

```text
UTF-8
LF line endings
InvariantCulture
lower-hex SHA-256
stable enum names
sorted files by required file order
sorted rows by stable row keys
sorted overlay cells by row-major coordinate
```

The digest must be independent of:

```text
input enumeration order
current culture / UI culture
current time
temporary directory path
Unity object instance id
Dictionary or HashSet iteration order
```

## 10. Focused Tests - GeneratedTerrainExportReplayTests.cs

Create focused EditMode tests with category `MAP16_07`.

Required test names:

```text
ExportPacketPublishesPlanSliceCellSocketSlotCsvContractsAndDigests
CsvExporterWritesDeterministicFilesWithStableHeaderOrder
ReplayVerifierRebuildsHashesFromExportedCsvWithoutAuthoringImport
CanvasAndSliceOverlaysCoverAllCellsSocketsAndSlots
ManifestRejectsMissingExtraTamperedOrMismatchedCsvFilesAtomically
CellSocketSlotRowsPreserveCoordinatesLayerProvenanceAndDigests
ExportRoundTripIsStableAcrossRepeatReverseCultureAndTempPath
ExporterDoesNotBakeTilemapsSpawnObjectsOrMutateScenesPrefabsGameObjects
ExporterDoesNotMutateSourceSliceOrMarkerSlotPackets
Map16HandoffKeepsMap16_08Locked
```

The tests must exercise at least:

```text
16/16 slices
1536/1536 cells
10752/10752 source layer records observed through cell rows or payload summaries
64/64 socket side signatures
24/24 marker slots from MAP16_06 reference packet
six logical CSV files
CSV replay pass
missing/extra/tampered/mismatched CSV failures
48x32 Canvas overlay
16 12x8 Slice overlays
repeat/reverse/culture/temp-path digest stability
non-mutation counters
MAP16_08 locked handoff
```

Do not add PlayMode tests.

## 11. Minimum Result Evidence

Result must include these fields with actual values:

```text
source slices observed: 16/16
source cells observed: 1536/1536
source layer records observed: 10752/10752
source socket side signatures observed: 64/64
source marker slots observed: 24/24
logical CSV files: 6/6
manifest rows: actual
plan rows: actual
slice rows: 16
cell rows: 1536
socket rows: actual
slot rows: 24
CSV headers stable: 6/6
CSV files written in focused temp directory: 6/6
CSV replay verification: PASS
missing/extra/tampered/mismatched CSV failures verified: actual
Canvas overlay cells: 1536/1536
Slice overlays: 16/16
Slice overlay cells: 1536/1536
overlay slots represented: 24/24
overlay sockets represented: actual/actual
Authoring reverse import attempts: 0
permanent generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
Tilemap bakes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
source slice mutation: 0
source slot mutation: 0
input digest: lower-hex
export packet digest: lower-hex
manifest digest: lower-hex
replay digest: lower-hex
repeat/reverse/culture/temp-path digest mismatches: 0/0/0/0
production seed approvals: 0
```

Focused verification block:

```text
Unity version: actual
mode: EditMode
category_names: [MAP16_07]
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

## 12. Commit and Stop

On PASS:

```text
write REPORTS/MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN_RESULT.md
update status MAP16_07 COMPLETE
keep MAP16_08 LOCKED
atomic commit only MAP16_07 files, task/status/report files, and generated meta for new files
commit subject: MAP16_07: export generated terrain replay
STOP
```

Do not start MAP16_08.

Git push is forbidden.

