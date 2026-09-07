```yaml
mapdesign_work_order:
  format: direct_visible_map_work_order_v3
  task_id: RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE
  supersedes_next_plan: VIS03_GENERATE_3X3_ONE_RING_GRAYBOX_SCENE
  reason: user wants a reachable run made by placing 4x4 MicroPatterns directly, not more 48x32 sector/window work
  based_on_visible_results:
    vis01_result:
      path: MapDesign/MCP/REPORTS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1_RESULT.md
      status: PASS
      sha256: 7b06ee95ce0973d996c5af72445f6d3840ace57ec6b4ee581cd4fe0fca6a5036
    vis02_result:
      path: MapDesign/MCP/REPORTS/VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE_RESULT.md
      status: PASS
      sha256: 1e21587d8d60caf65aa00aa244cf94f8ff4793762ecfb2118b8765138eadf1e4
  expected_duration: 1_to_2_hours
  work_style: larger_direct_micro_pattern_run_generation
```

# RUN01 - Build MicroPattern Reachable Run Scene

```text
TASK: RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE
PHASE: MicroPattern Run Generation / MoonPalace
STATUS: CURRENT
NEXT: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
NEXT STATUS: NOT STARTED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 방향 정정

이 작업은 48x32 섹터 하나를 또 다듬는 작업이 아니다.

이번 목표는 4x4 MicroPattern을 직접 배치해서, 시작점에서 도착점까지 실제로 이어지는 **도달 가능한 run**을 만드는 것이다.

기존 VIS01/VIS02에서 만든 48x32 one-sector Scene은 baseline evidence로만 둔다. RUN01은 섹터 크기에 매몰되지 않는다.

```text
4x4 MicroPattern placement
-> socket-compatible pattern path
-> tile-level open-cell BFS
-> reachable run Scene
```

PASS 후 말할 수 있는 것:

```text
4x4 MicroPattern 후보들을 직접 이어붙여 하나의 도달 가능한 MoonPalace run Scene을 생성할 수 있다.
```

아직 말하지 않을 것:

```text
full world generation complete
Spelunky-scale final level complete
player-physics-perfect traversal complete
NPC/combat/shop/save complete
production art complete
player build approved
```

## 1. Run 기본 형태

RUN01은 4x4 pattern grid를 직접 만든다.

Default run config:

```yaml
run_id: MP_RUN_01
seed_id: MP_QA_01
seed_value: 1924737067
pattern_size: [4, 4]
pattern_grid_size: [40, 12]
tile_size: [160, 48]
target_main_path_pattern_steps: 40_to_56
target_branch_count: 4_to_8
candidate_pool_size: 500
```

중요:

```text
tile_size는 48x32 sector에서 가져온 값이 아니다.
tile_size = pattern_grid_size * 4 이다.
기본값 160x48은 RUN01 예시용이며, 이후 RUN02에서 여러 크기/분기형으로 확장한다.
```

## 2. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
# User-Facing Implementation Report
# Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 왜 48x32 sector/window 작업이 아니라 MicroPattern run 작업인가?
4x4 MicroPattern 후보 500개를 어떻게 사용했는가?
run의 pattern grid/tile size/main path/branch 수는 무엇인가?
pattern socket들이 어떻게 이어져 시작점에서 도착점까지 route를 만들었는가?
tile-level BFS가 어떤 start/end coordinate를 통과했는가?
막힌 pattern 배치, 끊긴 socket, unreachable branch를 어떻게 거절했는가?
Unity에서 열 수 있는 run Scene 경로는 무엇인가?
Scene에서 route, branch, solid/open, hazard/detail, marker/protection이 어떻게 보이는가?
이번 Task에서 추가/변경한 script와 각 책임은 무엇인가?
아직 full world, production art, live traversal, NPC/combat/shop/save가 아니라는 경계는 무엇인가?
legacy 19347, prior category, PlayMode, full/unfiltered test를 하지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 3. 선행조건

작업 시작 전에 아래를 확인한다.

```text
VIS01 Result exists and STATUS: PASS
VIS01 Result SHA-256:
7b06ee95ce0973d996c5af72445f6d3840ace57ec6b4ee581cd4fe0fca6a5036

VIS02 Result exists and STATUS: PASS
VIS02 Result SHA-256:
1e21587d8d60caf65aa00aa244cf94f8ff4793762ecfb2118b8765138eadf1e4

VIS01 MicroPattern candidate filter is available:
raw 4x4 mask count: 65,536
accepted 4x4 candidate count: 500
```

If VIS02 finalize/commit is still pending, finish only VIS02 finalize/commit first. Then start RUN01.

If VIS01/VIS02 Result is missing or not PASS:

```text
STATUS: BLOCKED
reason: visible MicroPattern baseline not available
created/changed production code files: 0
STOP
```

## 4. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunConfig.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunComposer.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunValidator.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunSceneBuilder.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPatternRunTests.cs
Assets/_Game/Map/Scenes/MoonPalace/RUN01/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/**
MapDesign/MCP/GENERATED/RUN01/**
MapDesign/MCP/TASKS/RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE.md
MapDesign/MCP/REPORTS/RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE_RESULT.md
MapDesign/MCP_ARCHIVE/RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
VIS01 candidate library/source may be reused or referenced.
If VIS01 candidate code is private, expose the smallest public read-only candidate snapshot method and report it.
MAP21 CSV may be read-only for biome/tile/presentation tags.
The isolated RUN01 Scene may use generated debug Tile assets only under Assets/_Game/Map/Scenes/MoonPalace/RUN01.
Tilemap writes are allowed only while creating/updating the isolated RUN01 Scene.
```

금지:

```text
48x32 sector generator as the primary output
VIS03 one-ring generation
whole-world 13x13 generation
existing Unity Scene/Prefab mutation outside RUN01
Build Settings mutation
MAP21 authoring CSV rewrite
hardcoded static run counted as generation
silent fallback carve
reroll-until-valid without recording attempts
test-only fixture copied as production source
PlayMode tests
legacy 19347 regression
prior category rerun
unfiltered/full test run
player build execution
RUN02 or later task start
```

Compile repair exception:

```text
If RUN01-owned code fails to compile, fix only RUN01-owned files.
If VIS01 candidate API needs a tiny public read-only accessor, add the smallest accessor and record why.
If older MAP source fails to compile, return STATUS: BLOCKED with exact owner and error.
```

## 5. 구현 요구

### 5.1 Run config

Create:

```text
MoonPalaceMicroPatternRunConfig.cs
```

Required fields:

```text
run_id
seed_id
seed_value
pattern_width = 4
pattern_height = 4
pattern_grid_width = 40
pattern_grid_height = 12
tile_width = 160
tile_height = 48
candidate_count = 500
main_path_min_steps
main_path_max_steps
branch_min_count
branch_max_count
allow_90_degree_rotation = false
allow_silent_carve = false
```

Config must allow later changes without hardcoding all run dimensions into the composer.

### 5.2 Pattern run graph

Create:

```text
MoonPalaceMicroPatternRunGraph.cs
```

It must generate a graph over 4x4 pattern slots, not over 12x8 MicroChunks and not over fixed 48x32 sectors.

Required graph concepts:

```text
PatternSlotCoordinate
RunNode
RunEdge
RunSocket
RunBranch
RunStart
RunExit
RunGraphDigest
```

Graph requirements:

```text
main path starts near the left side and exits near the right side
main path uses 40_to_56 pattern steps
vertical movement is allowed but bounded inside pattern grid height
branches attach to main path and reconnect or dead-end with visible reward/marker placeholder
edge direction maps to required socket compatibility
no node outside pattern grid
no duplicate main-path node unless intentionally tagged as revisit
graph digest is deterministic for the same config/seed
```

### 5.3 Pattern run composer

Create:

```text
MoonPalaceMicroPatternRunComposer.cs
```

It must place actual 4x4 candidate masks into the pattern grid.

For every route edge:

```text
selected pattern must expose the required entry/exit socket
neighboring pattern sockets must be reciprocal
route cells inside the 4x4 mask must remain open
solid cells must not overwrite required route/protection cells
90-degree rotation is not allowed
horizontal mirror may be used only if the candidate identity/transform is recorded
```

The composer must record:

```text
pattern placement count
route placement count
branch placement count
filler/detail placement count
candidate id per slot
mask_u16_hex per slot
socket signature per slot
transform per slot
MAP21_02 presentation family link when available
selection reason
attempt count
rejection count by reason
```

Filler slots may use solid/detail/quiet candidates, but they must not disconnect the accepted route.

Forbidden:

```text
draw route first and then silently carve masks to fit it
accept incompatible sockets
use numeric-first 500 candidates without diversity metadata
paste a static 160x48 tilemap
```

### 5.4 Tile-level run validator

Create:

```text
MoonPalaceMicroPatternRunValidator.cs
```

It must validate the composed tile map at 1x1 tile level.

Required checks:

```text
tile dimensions: 160 x 48
pattern grid: 40 x 12
pattern placements: 480 / 480
start tile coordinate present and open
exit tile coordinate present and open
tile-level BFS start -> exit: reachable
main path required waypoint count: all reachable
branch entry count: all reachable from main path
route socket mismatch count: 0
out-of-bounds placement count: 0
duplicate placement count: 0
unreachable open island count: report number, fail only if tagged route/branch
route failure count: 0
fallback carve count: 0
silent repair count: 0
```

This is not full player physics. It proves open-cell reachability of the MicroPattern run.

### 5.5 Unity run Scene builder

Create:

```text
MoonPalaceMicroPatternRunSceneBuilder.cs
```

Required scene:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN01/MoonPalaceMicroPatternRun_RUN01.unity
```

Required scene contents:

```text
root GameObject: MoonPalace_MicroPatternRun_RUN01
Grid GameObject
Tilemap or equivalent debug layer for solid/open terrain
Tilemap or equivalent debug layer for main route and branches
Tilemap or equivalent debug layer for pattern boundaries, sockets, markers, protection
Orthographic Camera framing the full 160x48 run
Start marker
Exit marker
Legend GameObjects or TextMesh labels
Metadata GameObject containing run id, seed, pattern grid size, tile size, graph digest, map digest
```

Visual requirements:

```text
the Scene must clearly show many repeated 4x4 pattern cells
the main route must be visually traceable from start to exit
branches must be visually distinct from the main route
4x4 pattern boundaries must be visible enough to inspect
the result must not look like one rectangular room
```

Do not add the Scene to Build Settings.

### 5.6 Required artifacts

Generated JSON:

```text
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_config.json
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_graph.json
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_composition.json
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_validation.json
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_scene_manifest.json
MapDesign/MCP/GENERATED/RUN01/moonpalace_run01_digest_manifest.json
```

Authoring/debug CSV:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/moonpalace_run01_pattern_slots.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/moonpalace_run01_route_edges.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/moonpalace_run01_tile_cells.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/moonpalace_run01_validation_summary.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/moonpalace_run01_selection_manifest.csv
```

All JSON/CSV must be deterministic:

```text
UTF-8 without BOM
LF-only
final LF exactly 1
culture-invariant
created_utc excluded from canonical digest
```

## 6. Focused Tests

Run only focused EditMode tests with category:

```text
RUN01
```

Create:

```text
MoonPalaceMicroPatternRunTests.cs
```

Required test names:

```text
MicroPatternRunConfigDerivesTileSizeFromPatternGridNotSectorConstants
MicroPatternRunGraphCreatesLeftToRightMainPathWithBranchesInsideGrid
MicroPatternRunComposerUses500CandidatePoolAndRecordsSelectionReasons
MicroPatternRunComposerRejectsSocketMismatchAndSilentCarve
MicroPatternRunComposerPlacesAll480PatternSlots
MicroPatternRunValidatorProvesTileLevelStartToExitReachability
MicroPatternRunValidatorReportsBranchesReachableFromMainPath
MicroPatternRunValidatorKeepsFallbackCarveSilentRepairAndRouteFailuresZero
MicroPatternRunSceneBuilderCreatesIsolatedSceneWithStartExitLegendAndPatternGrid
MicroPatternRunArtifactsAreDeterministicAcrossRepeatReverseAndCulture
MicroPatternRunWritesOnlyRun01RootsAndDoesNotMutateExistingScenes
MicroPatternRunDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun02
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

## 7. Result Requirements

Write:

```text
MapDesign/MCP/REPORTS/RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE_RESULT.md
```

The Result must include:

```text
TASK: RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE
STATUS: PASS or FAIL or BLOCKED

# User-Facing Implementation Report
# Responsibility and Added Scripts
# MicroPattern Run Graph Summary
# Pattern Composition Summary
# Tile-Level Reachability Summary
# Unity Scene Output Summary
# Artifact and Digest Summary
# Focused Validation Summary
# No Legacy Regression Boundary Notes
# Final Status Evidence
```

Required PASS evidence:

```text
run id: MP_RUN_01
seed id/value: MP_QA_01 / 1924737067
pattern size: 4 x 4
pattern grid size: 40 x 12
tile size: 160 x 48
candidate pool accepted count: 500
pattern placements: 480 / 480
main path pattern steps: between 40 and 56
branch count: between 4 and 8
start tile coordinate: present/open
exit tile coordinate: present/open
tile-level BFS start->exit: PASS
reachable required waypoint count: all
reachable branch entry count: all
route socket mismatch count: 0
route failure count: 0
fallback carve count: 0
silent repair count: 0
scene path: Assets/_Game/Map/Scenes/MoonPalace/RUN01/MoonPalaceMicroPatternRun_RUN01.unity
scene root: MoonPalace_MicroPatternRun_RUN01
generated Unity scene count: 1
generated CSV count: 5
generated JSON count: 6
graph digest: present
composition digest: present
map digest: present
scene manifest digest: present
```

Required responsibility table:

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
| MoonPalaceMicroPatternRunConfig.cs | ... | ... |
| MoonPalaceMicroPatternRunGraph.cs | ... | ... |
| MoonPalaceMicroPatternRunComposer.cs | ... | ... |
| MoonPalaceMicroPatternRunValidator.cs | ... | ... |
| MoonPalaceMicroPatternRunSceneBuilder.cs | ... | ... |
| MoonPalaceMicroPatternRunTests.cs | ... | ... |
| RUN01 CSV/JSON/Scene artifacts | ... | ... |
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
48X32 SECTOR PRIMARY OUTPUTS: 0
EXISTING SCENE/PREFAB MUTATIONS: 0
BUILD SETTINGS MUTATIONS: 0
PLAYER BUILD EXECUTIONS: 0
RUN02 files/runs: 0 / 0
```

If PASS, the Result must say:

```text
This is the first direct 4x4 MicroPattern reachable run Scene.
It is not a 48x32 sector demo, not full-world generation, not production art, not live player traversal, and not player build approval.
```

## 8. Commit Rule

If and only if Result status is PASS:

```text
record RUN01 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN01-owned source/test/artifact/task/report/status files
do not commit unrelated files
do not push
STOP
```

Do not start RUN02.
