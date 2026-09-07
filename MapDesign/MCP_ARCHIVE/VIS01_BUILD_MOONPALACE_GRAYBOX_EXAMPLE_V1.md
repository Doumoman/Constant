```yaml
mapdesign_work_order:
  format: direct_visible_map_work_order_v2
  task_id: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
  based_on_handoff:
    file_name: MOONPALACE_LLM_IMPLEMENTATION_HANDOFF.md
    sha256: ce7357ccbdf65829917acf152d86b0f85802289ed28e6feeb5a8d08ed3309ccf
  requires_completed_task: MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT
  requires_result:
    path: MapDesign/MCP/REPORTS/MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT_RESULT.md
    status: PASS
    sha256: d9fd63a80894a7f72f743a36f797228b103ff4438486d3e5d6314608c0cc0a14
  expected_duration: 1_to_2_hours
  work_style: larger_visible_unity_scene_output
```

# VIS01 - Build MoonPalace Graybox Example Scene v1

```text
TASK: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
PHASE: Visible Map Generation / MoonPalace
STATUS: CURRENT
NEXT: VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE
NEXT STATUS: NOT STARTED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 이번 개정의 핵심

이 작업은 PNG preview를 만드는 작업이 아니다.

이번 PASS 기준은 Unity에서 열 수 있는 **격리된 예시 Scene**이다.

```text
required visible output:
Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity
```

Scene은 실제 production art가 아니라 debug color graybox로 만든다. 그래도 사용자가 Unity에서 열었을 때 48x32 한 섹터의 타일 구조, 4x4 MicroPattern grid, 12x8 MicroChunk boundary, route/recovery/marker/protection overlay가 보여야 한다.

이번 작업에서 바로잡는 설계 포인트:

```text
4x4 MicroPattern = 구조 브러시/마스크 후보
12x8 MicroChunk = 3x2개의 4x4 MicroPattern을 조합한 reachable chunk
48x32 Sector = 4x4개의 12x8 MicroChunk를 조합한 one-sector map
```

좌표 관계:

| Unit | Size | Count in parent | Total in sector |
|---|---:|---:|---:|
| Tile | 1x1 | - | 1,536 |
| MicroPattern | 4x4 | sector grid 12x8 | 96 |
| MicroChunk | 12x8 | sector grid 4x4 | 16 |
| Pattern per MicroChunk | 4x4 | chunk grid 3x2 | 6 |
| Logical layer | 48x32 | 7 layers | 10,752 layer-cells |

MAP21_02의 기존 24개 MicroPattern은 production starter set으로 유지한다. 하지만 visible map 구조화를 시작하기에는 다양성이 작다. 따라서 VIS01은 모든 4x4 binary mask `2^16 = 65,536`개를 열거한 뒤, deterministic filter/scoring으로 **500개 structural MicroPattern candidate**를 뽑는다.

이 500개는 production art 확정 데이터가 아니다. 시각 생성용 구조 후보군이다.

## 1. 작업 목적

`MOONPALACE_LLM_IMPLEMENTATION_HANDOFF.md`는 다음 단절을 명시한다.

```text
integrated_v2_generation_orchestrator: MISSING
moonpalace_runtime_catalog_loader: MISSING
generator_window_actual_generation_adapter: MISSING
unity_tilemap_write_adapter: MISSING
visible_unity_map_ready: false
playable_vertical_slice_ready: false
```

이번 작업의 목표는 이 중 첫 번째 visible output을 만드는 것이다.

```yaml
artifact_name: MoonPalace Graybox Example Scene v1
seed_id: MP_QA_01
seed_value: 1924737067
scope: one_sector
sector: [6, 6]
logical_size: [48, 32]
micro_pattern_size: [4, 4]
micro_pattern_candidates: 500
microchunks: [4, 4]
microchunk_size: [12, 8]
patterns_per_microchunk: [3, 2]
logical_layers: 7
```

이 작업이 PASS하면 다음 문장까지만 말할 수 있다.

```text
MoonPalace production CSV와 500개 filtered 4x4 structural candidates를 사용해
한 섹터의 reachable 12x8 MicroChunk 조합과 Unity graybox example scene을 만들 수 있다.
```

아직 다음은 주장하지 않는다.

```text
full world generation complete
production art complete
NPC/combat/shop/save runtime complete
player-controlled traversal complete
player build approved
```

## 2. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
# User-Facing Implementation Report
# Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업으로 Unity에서 실제로 열 수 있게 된 Scene은 무엇인가?
4x4 MicroPattern 65,536개를 어떤 기준으로 500개까지 줄였는가?
500개 후보가 기존 MAP21_02 24개 starter pattern과 어떤 관계인가?
4x4 후보 6개가 어떻게 12x8 reachable MicroChunk 하나로 조합되는가?
16개 MicroChunk가 어떻게 48x32 sector scene으로 배치되는가?
MP_QA_01 seed와 sector (6,6)이 selection에 어떻게 쓰였는가?
route/recovery/seam/protection/marker가 Scene에서 어떻게 보이는가?
생성된 Scene/CSV/JSON/manifest 목록과 digest는 무엇인가?
이번 Task에서 추가한 script와 각 책임은 무엇인가?
아직 production art, live traversal, full world streaming, NPC/combat/shop/save가 아니라는 경계는 무엇인가?
legacy 19347, prior category, PlayMode, full regression을 하지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 3. 선행조건

작업 시작 전에 아래를 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT_RESULT.md exists
TASK: MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT
STATUS: PASS
Result SHA-256:
d9fd63a80894a7f72f743a36f797228b103ff4438486d3e5d6314608c0cc0a14

MapDesign/MCP/06_IMPLEMENTATION_STATUS.md exists
MAP21_12 is COMPLETE or can be verified as PASS-finalized from local protocol
Current Task is NONE or no official MAP task is running
```

Source of truth priority:

```text
1. 현재 사용자 지시
2. 실제 production C#과 Authoring CSV bytes
3. MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
4. MAP21_12 Result and digest evidence
5. MOONPALACE_LLM_IMPLEMENTATION_HANDOFF.md
```

If MAP21_12 Result is missing or not PASS:

```text
STATUS: BLOCKED
reason: MAP21_12 final audit not available
created/changed production code files: 0
STOP
```

## 4. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceRuntimeCatalogSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceMicroPatternCandidateLibrary.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceReachableMicroChunkComposer.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceOneSectorGrayboxGenerator.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxExampleSceneBuilder.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxExampleTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/**
Assets/_Game/Map/Scenes/MoonPalace/VIS01/**
MapDesign/MCP/GENERATED/VIS01/**
MapDesign/MCP/TASKS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1.md
MapDesign/MCP/REPORTS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1_RESULT.md
MapDesign/MCP_ARCHIVE/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
MAP21_01~11 authoring CSV/JSON may be read and hashed only.
MAP14/MAP16/MAP17 public production planner/bake types may be referenced and composed.
The isolated VIS01 scene may contain Grid, Tilemap, Camera, and debug legend GameObjects.
The isolated VIS01 scene may use generated debug Tile assets only under Assets/_Game/Map/Scenes/MoonPalace/VIS01.
Tilemap writes are allowed only while creating/updating the isolated VIS01 scene.
```

금지:

```text
MAP00~MAP21 source rewrite unless the file is explicitly allowed above
MAP21 authoring CSV rewrite
test-only fixture copied as production source
reflection helper copied into production path
hardcoded sample map counted as generated output
fallback carve that silently fixes invalid generation
whole-world 13x13 generation
existing Unity Scene/Prefab mutation outside VIS01
Collider/Addressables/runtime GameObject mutation outside the isolated VIS01 scene asset
NPC/combat/shop/reward/save runtime execution
PlayMode tests
legacy 19347 regression
prior category rerun
unfiltered/full test run
player build execution
VIS02 or later task start
```

Compile repair exception:

```text
If VIS01-owned code fails to compile, fix only VIS01-owned files.
If older source code fails to compile, return STATUS: BLOCKED with the exact owner and error.
```

## 5. 구현 요구

### 5.1 Runtime catalog snapshot

Create:

```text
MoonPalaceRuntimeCatalogSnapshot.cs
```

It must read actual MAP21 authoring CSV files read-only and build an immutable snapshot containing at least:

```text
biomes: 4
tile_codes: 10
production_micro_patterns: 24
production_pattern_cells: 384
terrain_clusters: 48
cluster_spine_variants: 96
activity_profiles: 7
event_overlay_profiles: 5
biome_pairs: 6
boundary_candidates: 48
core_resource_regions: 3
tuning_density_windows: 4
qa_seed_MP_QA_01: 1924737067
```

The snapshot must record:

```text
source file path
record count
header hash
content SHA-256
typed load errors
foreign-key errors
duplicate ID errors
canonical catalog digest
```

If actual source counts differ from the handoff, do not fake the count. Report the actual count and either:

```text
PASS if the example scene can still be produced and the mismatch is explained by later source changes
BLOCKED if required data is missing
```

### 5.2 4x4 MicroPattern candidate filter

Create:

```text
MoonPalaceMicroPatternCandidateLibrary.cs
```

It must enumerate every 4x4 binary occupancy mask:

```text
raw mask count: 65,536
mask bits: 16
bit meaning: 1 = solid, 0 = open
local coordinate: x 0..3, y 0..3
```

Then deterministically filter and score the masks down to:

```text
accepted structural candidate count: 500
```

The 500 candidates must be selected by explicit rules, not manual listing.

Minimum filter rules:

```text
reject duplicate mask ids
reject all-solid masks
reject all-open masks unless explicitly kept as a single quiet/open archetype
reject masks with open-cell count below 4 or above 14 except explicit quiet/open archetype
reject masks whose largest connected open component is below 4
reject masks with impossible isolated one-cell open pockets unless tagged as detail-only and excluded from chunk path cells
reject masks with no usable edge socket and no local support/affordance feature
reject masks that violate 4x4 coordinate bounds
```

Minimum derived properties per candidate:

```text
candidate_id
mask_u16_hex
open_count
solid_count
density
largest_open_component
open_component_count
north_socket_bits
south_socket_bits
west_socket_bits
east_socket_bits
floor_support_bits
ceiling_gap_bits
left_wall_bits
right_wall_bits
silhouette_signature
socket_signature
mirror_family_signature
role_tags
chunk_path_allowed
score
rank
```

Scoring must preserve diversity. It must not simply take the first 500 numeric masks.

Minimum diversity buckets:

```text
density bucket
edge socket bucket
silhouette signature
mirror family
support/affordance class
hazard/detail eligibility
```

90-degree rotation is not allowed as a chunk/pattern placement transform. Horizontal mirror may be recorded as a family relationship, but the selected candidates must still keep their actual mask identity.

Relationship to MAP21_02:

```text
MAP21_02 24 production MicroPatterns remain source-authored presentation patterns.
VIS01 500 MicroPattern candidates are generated structural masks for graybox composition.
VIS01 may link a generated candidate to a MAP21_02 pattern family when compatible, but it must not rewrite MAP21_02.
```

### 5.3 Reachable 12x8 MicroChunk composition

Create:

```text
MoonPalaceReachableMicroChunkComposer.cs
```

It must compose one 12x8 MicroChunk from exactly six 4x4 pattern candidates:

```text
chunk pattern grid: 3x2
patterns per chunk: 6
chunk tile size: 12x8
chunk cells: 96
```

Each composed MicroChunk must have:

```text
chunk_id
chunk_index
chunk coordinate
six selected candidate ids
six pattern local positions
open/solid cell map
entry sockets
exit sockets
internal open connectivity summary
required path cells
recovery path cells
protected cells
marker slots
reachability verdict
failure owner if invalid
chunk digest
```

Reachability rule:

```text
At least one configured entry socket must reach at least one configured exit socket inside the 12x8 chunk.
Required path cells must remain open.
Recovery path cells must remain open.
Protected cells must not be overwritten by pattern solids.
```

For VIS01, it is acceptable to use a deterministic graybox reachability approximation based on open-cell BFS and explicit route sockets. Do not claim player-physics-perfect traversal yet. MAP19 movement graph remains the later authority for full traversal.

Forbidden:

```text
silently carving a path after invalid composition
rerolling until valid without recording attempts
using a static hand-drawn 12x8 chunk
placing 4x4 masks without chunk-level reachability validation
```

### 5.4 One-sector graybox generator

Create:

```text
MoonPalaceOneSectorGrayboxGenerator.cs
```

It must generate one deterministic logical sector:

```text
seed_id: MP_QA_01
seed_value: 1924737067
sector: (6, 6)
size: 48 x 32
cells: 1,536
sector pattern grid: 12 x 8
sector pattern count: 96
microchunks: 16
patterns per microchunk: 6
total selected 4x4 pattern placements: 96
layers: 7
layer_cell_records: 10,752
```

Generation must use actual catalog records and generated candidate records for selections:

```text
biome
terrain cluster
spine variant
generated 4x4 MicroPattern candidate
MAP21_02 production pattern family link where compatible
activity/event where applicable
boundary/socket/protection metadata where applicable
marker/source owner where applicable
```

The generator must produce:

```text
final canvas cell records
96 pattern placement records
16 reachable MicroChunk records
socket/slot summary
7-layer logical bake records
route/recovery/seam validation summary
selection manifest listing source IDs used
logical map digest
```

Required validation:

```text
unique coordinates: 1,536 / 1,536
sector pattern placements: 96 / 96
microchunks: 16 / 16
patterns per microchunk: 6 / 6
cells per microchunk: 96 / 96
logical layer records: 10,752
route failure count: 0
recovery failure count: 0
seam failure count: 0
fallback carve count: 0
silent auto-repair count: 0
```

### 5.5 Example Scene builder

Create:

```text
MoonPalaceGrayboxExampleSceneBuilder.cs
```

It must publish an isolated Unity example scene:

```text
Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity
```

Required scene contents:

```text
root GameObject: MoonPalace_Graybox_VIS01
Grid GameObject
Tilemap or equivalent debug cell layer for solid/open terrain
Tilemap or equivalent debug cell layer for route/recovery overlay
Tilemap or equivalent debug cell layer for markers/protection/slice boundaries
Orthographic Camera framing the 48x32 sector
in-scene legend or named legend GameObjects explaining colors/layers
metadata GameObject or ScriptableObject reference containing seed_id, seed_value, sector, digest
```

Suggested debug colors:

```text
solid terrain: dark gray
open/corridor: pale blue-gray
one-way/platform: amber
hazard: red
required route: bright green
recovery route: cyan
special marker: violet
activity/event marker: orange
protected/envelope cell: blue
12x8 MicroChunk boundary: black or dark line overlay
4x4 MicroPattern boundary: subtle thin grid or named overlay toggle
```

The scene must be regenerated safely:

```text
delete/replace only the VIS01 isolated scene root/assets
do not mutate existing user scenes
do not create production art assets outside VIS01
do not add Scene to Build Settings
```

### 5.6 Required generated artifacts

Generated JSON under:

```text
MapDesign/MCP/GENERATED/VIS01/
```

Required JSON files:

```text
moonpalace_vis01_manifest.json
moonpalace_vis01_catalog_snapshot.json
moonpalace_vis01_pattern_candidates_500.json
moonpalace_vis01_generation_result.json
moonpalace_vis01_scene_manifest.json
moonpalace_vis01_digest_manifest.json
```

Authoring/debug CSV under:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/
```

Required CSV files:

```text
moonpalace_vis01_pattern_candidates_500.csv
moonpalace_vis01_pattern_placements.csv
moonpalace_vis01_microchunks.csv
moonpalace_vis01_cells.csv
moonpalace_vis01_layers.csv
moonpalace_vis01_selection_manifest.csv
moonpalace_vis01_validation_summary.csv
```

All JSON/CSV must be:

```text
UTF-8 without BOM
LF-only
final LF exactly 1
deterministic across repeat publish
culture-invariant
created_utc excluded from canonical digest
```

## 6. Focused Tests

Run only focused EditMode tests with category:

```text
VIS01
```

Create:

```text
MoonPalaceGrayboxExampleTests.cs
```

Required test names:

```text
MoonPalaceCatalogSnapshotLoadsActualMap21Sources
MoonPalaceMicroPatternCandidatesEnumerate65536AndSelect500Deterministically
MoonPalaceMicroPatternCandidatesPreserveSocketDensitySilhouetteAndMirrorMetadata
MoonPalaceReachableMicroChunkComposerBuildsSixPattern12x8Chunks
MoonPalaceReachableMicroChunkComposerRejectsUnreachableOrSilentCarvedChunks
MoonPalaceOneSectorGeneratorProduces1536UniqueCellsAnd96PatternPlacements
MoonPalaceOneSectorGeneratorProduces16ReachableChunksAnd10752LayerRecords
MoonPalaceOneSectorGeneratorBindsSelectionsToCatalogAndCandidateIds
MoonPalaceGrayboxSceneBuilderCreatesIsolatedUnitySceneWithGridCameraLegend
MoonPalaceGrayboxPublisherWritesCsvJsonAndSceneManifestOnlyInVis01Roots
MoonPalaceGrayboxDigestIsRepeatReverseAndCultureStable
MoonPalaceGrayboxWorkDoesNotRunLegacyRegressionPlayModeBuildOrFullWorld
```

Expected focused test result:

```text
Discovered: 12
Executed: 12
Passed: 12
Failed: 0
Skipped: 0
Inconclusive: 0
```

No other category may run unless there is an actual compile error and the result explains why.

## 7. Result Requirements

Write:

```text
MapDesign/MCP/REPORTS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1_RESULT.md
```

The Result must include:

```text
TASK: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
STATUS: PASS or FAIL or BLOCKED

# User-Facing Implementation Report
# Responsibility and Added Scripts
# MicroPattern Candidate Filter Summary
# Reachable MicroChunk Composition Summary
# One-Sector Generation Summary
# Unity Scene Output Summary
# Artifact and Digest Summary
# Focused Validation Summary
# No Legacy Regression Boundary Notes
# Final Status Evidence
```

Required PASS evidence:

```text
seed id/value: MP_QA_01 / 1924737067
sector: 6,6
raw 4x4 mask count: 65,536
accepted 4x4 candidate count: 500
production MAP21_02 pattern count observed: 24
final canvas size: 48 x 32
unique coordinates: 1,536 / 1,536
sector pattern placements: 96 / 96
microchunks: 16 / 16
patterns per microchunk: 6 / 6
cells per microchunk: 96 / 96
logical layer records: 10,752
route failure count: 0
recovery failure count: 0
seam failure count: 0
unreachable microchunk count: 0
fallback carve count: 0
silent auto-repair count: 0
catalog source mutation count: 0
scene path: Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity
scene root object: MoonPalace_Graybox_VIS01
generated Unity scene count: 1
generated CSV count: 7
generated JSON count: 6
logical map digest: present
scene manifest digest: present
```

Required responsibility table:

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
| MoonPalaceRuntimeCatalogSnapshot.cs | ... | ... |
| MoonPalaceMicroPatternCandidateLibrary.cs | ... | ... |
| MoonPalaceReachableMicroChunkComposer.cs | ... | ... |
| MoonPalaceOneSectorGrayboxGenerator.cs | ... | ... |
| MoonPalaceGrayboxExampleSceneBuilder.cs | ... | ... |
| MoonPalaceGrayboxExampleTests.cs | ... | ... |
| VIS01 CSV/JSON/Scene artifacts | ... | ... |
```

Required no-regression proof:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
FULL WORLD GENERATION RUNS: 0
EXISTING SCENE/PREFAB MUTATIONS: 0
PLAYER BUILD EXECUTIONS: 0
VIS02 files/runs: 0 / 0
```

If PASS, the Result must say:

```text
This is the first visible one-sector Unity graybox scene.
It is not yet production art, live traversal, full-world streaming, NPC/combat/shop/save runtime, or player build approval.
```

## 8. Commit Rule

If and only if Result status is PASS:

```text
record VIS01 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only VIS01-owned source/test/artifact/task/report/status files
do not commit unrelated files
do not push
STOP
```

If the local workflow requires `MCP_ARCHIVE`, move/archive the inbox MD there after installation. If the direct work-order path is used, include both installed and archived path evidence in the Result.

Do not start VIS02.
