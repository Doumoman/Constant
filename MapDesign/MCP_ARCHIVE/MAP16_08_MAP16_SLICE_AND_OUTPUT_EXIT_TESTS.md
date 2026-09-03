```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS
  task_file: TASKS/MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN
  requires_result:
    path: REPORTS/MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN_RESULT.md
    status: PASS
    sha256: a021126ceb5335ca451fc812fa5e2193db1695fb8026c0779baea5de8d16e4b7
  requires_installed_task:
    path: TASKS/MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN.md
    sha256: bc07ff7f0f77489472c87b8a605487fb66f116b37a1f2ffb79e3b6cf18e73b4f
  sets_current_task: MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS
```

# MAP16_08 - MAP16 Slice and Output Exit Tests

```text
TASK: MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS
PHASE: MAP16 - Canvas Finalization and 12x8 Slice
STATUS: CURRENT
NEXT: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP16_01~07의 public generated-terrain chain을 하나의 focused phase-exit audit으로 검수한다.

```text
MAP16_01 final canvas layer plan
MAP16_02 protection / cleanup / density report
MAP16_03 final route and recovery report
MAP16_04 pattern chunk coordinate partition
MAP16_05 16 generated 12x8 slices and derived sockets
MAP16_06 marker slots and provenance
MAP16_07 CSV export / replay / debug overlay
-> Map16SliceOutputExitTests
-> MAP16 PHASE EXIT verdict
-> MAP17_01 generated cell placement input
```

이번 Task는 production 기능을 새로 구현하지 않는다. MAP16_01~07이 이미 공개한 immutable model/result/digest를 읽어, MAP17로 넘겨도 되는지 focused EditMode test로 승인하거나 차단한다.

중요: 이번 Task에서도 prior category, legacy 19347, PlayMode, unfiltered regression은 실행하지 않는다. MAP16_08은 회귀를 돌리는 작업이 아니라 `REGRESSION TRIGGER DETECTED: NO`와 regression selection `0`을 Result에 증명하는 작업이다.

MAP16 Phase Gate:

```text
48x32 sector cell coverage complete
16 generated MicroChunk slices complete
each slice is exactly 12x8 / 96 unique cells
all copied layer/source/provenance records complete
derived socket side signatures complete
internal chunk-adjacent socket compatibility has no mismatch
marker slots and provenance complete
CSV export round-trip and replay digest pass
Canvas/Slice overlay coverage complete
169-sector reference projection has no coordinate gap/duplicate/out-of-bounds
no Authoring reverse import, no permanent Generated CSV commit
MAP17_01 remains locked
```

Exit approval은 MAP16 generated-terrain output contract에 대한 승인이다. 실제 Tilemap bake, collider/physics/player traversal, runtime streaming, save/load, stable spawn id, gameplay spawn, Scene/Prefab/GameObject 반영, production seed approval은 아직 승인하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 test script와 test helper/method별 책임, 입력->출력, phase gate 수치, digest chain, regression selection 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP16 phase-exit focused EditMode audit | new Runtime production model |
| MAP16_01~07 public chain integration verdict | Tilemap bake |
| 48x32 sector and 16 12x8 slice coverage proof | collider/physics/player traversal |
| socket signature and internal adjacency compatibility audit | runtime streaming/save/load |
| marker slot and provenance completeness audit | stable spawn id |
| MAP16_07 CSV export/replay/overlay consistency check | gameplay spawn / object attachment |
| reference 169-sector coordinate projection audit | Scene/Prefab/GameObject mutation |
| regression trigger absence and selection zero proof | production seed approval |
| exact approval boundary for MAP17 handoff | MAP17_01 execution |

The exit audit can build test-owned probes inside the new test file. It cannot patch production code, upstream tests, CSV/schema, Editor windows, scenes or assets.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP16_08`만 선택한다.

```text
MAP16_08 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03/MAP16_04/MAP16_05/MAP16_06/MAP16_07 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

MAP16_08 test가 기존 public API를 호출하는 것은 과거 category 재실행이 아니다. MAP16_07에서 승인한 temp CSV write/replay helper를 호출할 수 있지만, category selection은 `MAP16_08`만 사용한다.

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP16_08` category로 제한한다.

신규 task-owned failure는 신규 MAP16_08 test 파일만 수정하고 `MAP16_08` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP16_01~07 digest/count mismatch, MAP15 169-sector public topology contradiction, CSV replay contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP16_07 Result: PASS
MAP16_07 Result SHA-256:
a021126ceb5335ca451fc812fa5e2193db1695fb8026c0779baea5de8d16e4b7

MAP16_07 installed Task SHA-256:
bc07ff7f0f77489472c87b8a605487fb66f116b37a1f2ffb79e3b6cf18e73b4f

MAP16_07 COMPLETE / MAP16_08 CURRENT / MAP17_01 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP16_01: SectorFinalCanvasLayerPlan and layer/source/protection/provenance facts
MAP16_02: SectorCanvasProtectionDensityReport and accepted density/protection facts
MAP16_03: SectorFinalRouteRecoveryReport and route/recovery witness facts
MAP16_04: SectorPatternChunkPartition and coordinate round-trip facts
MAP16_05: GeneratedMicroChunkSliceSet, slice/cell/layer/socket/traversal digests
MAP16_06: GeneratedMicroChunkMarkerSlotSet, slot/provenance/digest packet
MAP16_07: GeneratedTerrainExportPacket, CSV export result, replay verifier and overlay packet
MAP15: 13x13 / 169-sector topology identity where public
MAP09: MicroChunk 12x8, MicroPattern 4x4, chunk-slice-last contract
```

MAP16_08 must consume public values. Do not inspect private fields. Do not reparse physical Authoring CSV. If live generated terrain data is still reference-only, use deterministic `REFERENCE MAP16 SLICE OUTPUT EXIT` fixtures only for focused tests. Such fixtures must be clearly labeled and must not claim production seed approval, Tilemap bake, runtime streaming or gameplay approval.

Do not create generated debug files. The only user-facing persisted file from this work is the normal `*_RESULT.md` written by the MCP workflow.

## 4. Exact Write Boundary

정상 범위는 phase-exit EditMode test 1개와 matching meta다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/Map16SliceOutputExitTests.cs(.meta)
```

```text
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Baking
Category: MAP16_08
```

If the target EditMode test folder does not exist, create only the minimum folder path and matching Unity folder metas if the project requires them, and report those folder metas explicitly. Do not move existing folders.

수정·생성 금지:

```text
Runtime production C#
existing C# / test / CSV / meta
Editor production C#
Authoring or Generated CSV/meta
schema registry/test
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
PlayMode test/helper
EditorWindow / overlay / inspector
debug export file, generated report asset, JSON file, CSV file committed to project
Tilemap bake or MicroChunk runtime streamer
MAP17 files
```

Focused tests may write temporary CSV files only under a disposable temp directory created by the test. Temporary files must be cleaned up or reported if cleanup fails. No generated CSV file may be committed in this Task.

If the new test cannot compile without changing production code or asmdef/asmref, do not change those files. Report `BLOCKED` with exact symbol/API/assembly reference needed.

## 5. Dedicated Exit Matrix

`Map16SliceOutputExitTests` must build the current MAP16_01~07 public chain through public APIs and publish a MAP16 exit verdict in the Result.

Minimum test gates:

```text
CurrentMap16ChainPublishesAllRequiredArtifactsForExit
SectorSliceCoverageHasSixteenNinetySixCellSlicesAndNoCoordinateGaps
ReferenceWorldProjectionCoversOneHundredSixtyNineSectorsWithoutBaking
SocketBandsSignaturesAndInternalNeighborCompatibilityRemainValid
CsvExportReplayRoundTripPreservesManifestPacketAndOverlayDigests
LayerSourceMarkerSlotAndProvenanceCoverageRemainComplete
ExitAuditRejectsCoverageSocketReplayAndProvenanceContradictionsAtomically
ExitAuditDigestIsStableAcrossRepeatReverseCultureAndTempPath
NoRegressionSelectionOrTilemapScenePrefabGameplayMutationOccurs
Map17HandoffKeepsRuntimeBakeLocked
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production exporters, planners, bakers or streamers.

## 6. Phase Gate Details

### A. MAP16 public chain completeness

The test must verify that MAP16_01~07 required artifacts are present through public APIs or accepted reference fixtures:

```text
MAP16_01 final canvas
MAP16_02 protection/density
MAP16_03 route/recovery
MAP16_04 coordinate partition
MAP16_05 slice/socket packet
MAP16_06 marker slot packet
MAP16_07 export/replay/overlay packet
```

Digest records must be lower-hex SHA-256 where the public model exposes digests.

### B. Sector and slice coverage

Required exact counts:

```text
sector size: 48x32
sector cells: 1536
chunk grid: 4x4
slice count: 16
slice size: 12x8
slice cells: 96 each
total slice cells: 1536
layer records: 10752
duplicate sector cell coverage: 0
missing sector cell coverage: 0
out-of-bounds sector cell coverage: 0
```

### C. Reference 169-sector projection

MAP16_08 may project the accepted per-sector output onto the public 13x13 world topology to prove coordinate scaling. This is not a world terrain solve, reroll, bake, streaming simulation or production seed approval.

Required exact counts when MAP15 topology is public:

```text
world sectors: 169
sector grid: 13x13
world size: 624x416
projected world cells: 259584
projected world slices: 2704
duplicate projected world cells: 0
missing projected world cells: 0
out-of-bounds projected world cells: 0
```

If the public MAP15 topology cannot be consumed without changing upstream source, mark `BLOCKED` before writing a weaker pass.

### D. Socket and boundary compatibility

Required checks:

```text
socket side signatures observed: 64/64
socket bands represented: actual/actual
internal chunk adjacency checks: 24/24
internal chunk adjacency mismatches: 0
external sector edge socket records preserved: actual/actual
socket digest mismatches: 0
```

Do not create physics probes or player traversal probes. This is static data compatibility only.

### E. Export, replay and overlays

Required checks:

```text
logical CSV files: 6/6
CSV replay verification: PASS
manifest digest matches replay manifest digest
packet digest matches replay packet digest
missing/extra/tampered/mismatched CSV failures verified
Canvas overlay cells: 1536/1536
Slice overlays: 16/16
Slice overlay cells: 1536/1536
overlay slots represented: 24/24
overlay sockets represented: all observed signatures and bands
Authoring reverse import attempts: 0
permanent generated CSV/assets committed: 0
```

### F. Atomic failure and determinism

At minimum, the audit must reject:

```text
missing slice
duplicate/missing/out-of-bounds cell coverage
socket adjacency mismatch
missing provenance
tampered CSV replay
missing marker slot source
```

Failure returns no MAP16 exit approval and must not open MAP17.

Digest must be stable across:

```text
repeat
reverse input/projection order
tr-TR culture
temporary export path change
```

## 7. Minimum Result Evidence

Result must include these values with actual values:

```text
MAP16_01~07 artifacts required/covered/missing: 7/7/0
sector size observed: 48x32
sector cells observed: 1536/1536
source slices observed: 16/16
slice dimensions observed: 12x8
cells per slice observed: 96/96
total slice cells observed: 1536/1536
source layer records observed: 10752/10752
source socket side signatures observed: 64/64
socket bands observed: actual
internal chunk adjacency checks: 24/24
internal chunk adjacency mismatches: 0
source marker slots observed: 24/24
slots with provenance: 24/24
logical CSV files: 6/6
CSV replay verification: PASS
CSV replay failure probes verified: actual
Canvas overlay cells: 1536/1536
Slice overlays: 16/16
Slice overlay cells: 1536/1536
overlay slots represented: 24/24
overlay sockets represented: actual/actual
reference world sectors observed: 169/169
reference world projected slices: 2704/2704
reference world projected cells: 259584/259584
reference world duplicate/missing/out-of-bounds cells: 0/0/0
MAP16 digest records valid: actual/actual
MAP16 exit verdict: PASS
MAP17_01 automatic open: false
Authoring reverse import attempts: 0
permanent generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
Tilemap bakes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
production seed approvals: 0
repeat/reverse/culture/temp-path digest mismatches: 0/0/0/0
```

Focused verification block:

```text
Unity version: actual
mode: EditMode
category_names: [MAP16_08]
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

## 8. Report Guidance

The user-facing report must clearly say:

```text
이번 Task에서 추가된 것은 phase-exit test script 1개뿐이다.
새 Runtime production 기능은 추가하지 않았다.
MAP16 전체 산출물이 MAP17 입력으로 넘어갈 수 있는지 검수했다.
169-sector check는 coordinate projection audit이며 Tilemap bake나 production world solve가 아니다.
Editor/game visible change는 없다.
다음 MAP17_01부터 실제 generated cell placement와 TileCode/Prefab ID 검증을 시작한다.
```

## 9. Commit and Stop

On PASS:

```text
write REPORTS/MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS_RESULT.md
update status MAP16_08 COMPLETE
keep MAP17_01 LOCKED
atomic commit only MAP16_08 files, task/status/report files, and generated meta for new files
commit subject: MAP16_08: audit slice output exit
STOP
```

Do not start MAP17_01.

Git push is forbidden.

