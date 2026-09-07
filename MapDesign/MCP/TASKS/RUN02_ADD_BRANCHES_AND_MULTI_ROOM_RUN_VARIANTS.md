```yaml
mapdesign_work_order:
  format: direct_visible_map_work_order_v3
  task_id: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
  based_on_previous_run_result:
    path: MapDesign/MCP/REPORTS/RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE_RESULT.md
    status: PASS
    sha256: 51669eda4a7d76815f20d0fa31c0d1df14d6c1fac579fa0ce1b11eec57e54951
  requires_completed_task: RUN01_BUILD_MICROPATTERN_REACHABLE_RUN_SCENE
  expected_duration: 1_to_2_hours
  work_style: larger_direct_micro_pattern_multi_variant_generation
```

# RUN02 - Add Branches and Multi-Room Run Variants

```text
TASK: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
PHASE: MicroPattern Run Generation / MoonPalace
STATUS: CURRENT
NEXT: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
NEXT STATUS: NOT STARTED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

RUN01은 4x4 MicroPattern을 직접 배치해서 `MP_RUN_01`의 start -> exit 도달 가능한 run Scene을 만들었다.

RUN02는 한 줄짜리 run에서 벗어나, 4x4 MicroPattern만으로 **여러 방/분기/재합류/수직 이동이 있는 run variant 3개**를 만든다.

이번 작업은 48x32 sector 확장이 아니다.

```text
4x4 MicroPattern candidate pool
-> variable pattern-grid run variants
-> room graph / branch graph / split-rejoin graph
-> tile-level BFS proof per variant
-> one Unity comparison Scene containing 3 generated variants
```

PASS 후 말할 수 있는 것:

```text
4x4 MicroPattern 후보들을 직접 배치해 서로 다른 구조의 도달 가능한 run variant 3개를 생성하고 Unity Scene에서 비교할 수 있다.
```

아직 말하지 않을 것:

```text
full world generation complete
production art complete
player-physics-perfect traversal complete
live camera transition complete
NPC/combat/shop/save complete
player build approved
```

## 1. Variant 기본 형태

RUN02는 아래 세 variant를 deterministic하게 만든다.

| Variant | Pattern grid | Tile size | Required structure |
|---|---:|---:|---|
| `MP_RUN_02_A_BRANCHING_LOWLAND` | 48 x 14 | 192 x 56 | lowland main path, 6~10 branches, 1 optional reward dead-end |
| `MP_RUN_02_B_VERTICAL_LOOP` | 42 x 18 | 168 x 72 | vertical climb/drop, 8~12 branches, at least 2 split-rejoin loops |
| `MP_RUN_02_C_MULTI_ROOM_SPLIT` | 60 x 16 | 240 x 64 | three room regions, 10~14 branches, at least 2 split-rejoin choices |

All tile sizes must be derived:

```text
tile_width = pattern_grid_width * 4
tile_height = pattern_grid_height * 4
```

Do not read tile dimensions from sector constants. Do not use 48x32 as the primary output size.

## 2. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
# User-Facing Implementation Report
# Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
RUN02가 RUN01에서 무엇을 확장했는가?
세 run variant의 pattern grid/tile size/main path/branch/split-rejoin 수는 무엇인가?
왜 이번 작업이 48x32 sector 작업이 아니라 MicroPattern run variant 작업인가?
4x4 후보 500개가 각 variant에서 어떻게 선택·분산됐는가?
방/분기/수직 이동/재합류 구조가 pattern graph에서 어떻게 표현됐는가?
tile-level BFS가 각 variant의 start->exit와 branch entry/rejoin을 어떻게 증명했는가?
Unity 비교 Scene에서 세 variant를 어떻게 볼 수 있는가?
이번 Task에서 추가/변경한 script와 각 책임은 무엇인가?
아직 live camera transition, full world, production art, NPC/combat/shop/save가 아니라는 경계는 무엇인가?
legacy 19347, prior category, PlayMode, full/unfiltered test를 하지 않았다는 증거는 무엇인가?
RUN03은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 3. 선행조건

작업 시작 전에 아래를 확인한다.

```text
RUN01 Result exists and STATUS: PASS
RUN01 Result SHA-256:
51669eda4a7d76815f20d0fa31c0d1df14d6c1fac579fa0ce1b11eec57e54951

RUN01 direct MicroPattern baseline:
raw 4x4 mask count: 65,536
accepted 4x4 candidate count: 500
pattern grid size: 40 x 12
tile-level BFS start->exit: PASS
48X32 SECTOR PRIMARY OUTPUTS: 0
```

If RUN01 finalize/commit is still pending, finish only RUN01 finalize/commit first. Then start RUN02.

If RUN01 Result is missing or not PASS:

```text
STATUS: BLOCKED
reason: RUN01 reachable MicroPattern baseline not available
created/changed production code files: 0
STOP
```

## 4. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunVariantConfig.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunRoomGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunVariantComposer.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunVariantValidator.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunVariantSceneBuilder.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunVariantTests.cs
Assets/_Game/Map/Scenes/MoonPalace/RUN02/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/**
MapDesign/MCP/GENERATED/RUN02/**
MapDesign/MCP/TASKS/RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS.md
MapDesign/MCP/REPORTS/RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS_RESULT.md
MapDesign/MCP_ARCHIVE/RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
RUN01 candidate/run classes may be reused or called.
If RUN01 code lacks a tiny read-only public accessor needed for reuse, expose the smallest accessor and report it.
MAP21 CSV may be read-only for biome/tile/presentation tags.
The isolated RUN02 Scene may use generated debug Tile assets only under Assets/_Game/Map/Scenes/MoonPalace/RUN02.
Tilemap writes are allowed only while creating/updating the isolated RUN02 Scene.
```

금지:

```text
48x32 sector generator as the primary output
VIS01/VIS02 window polish or one-sector regeneration as the primary output
whole-world 13x13 generation
existing Unity Scene/Prefab mutation outside RUN02
Build Settings mutation
MAP21 authoring CSV rewrite
hardcoded static variant counted as generation
silent fallback carve
reroll-until-valid without recording attempts
test-only fixture copied as production source
PlayMode tests
legacy 19347 regression
prior category rerun
unfiltered/full test run
player build execution
RUN03 or later task start
```

Compile repair exception:

```text
If RUN02-owned code fails to compile, fix only RUN02-owned files.
If RUN01 API needs a tiny public read-only accessor, add the smallest accessor and record why.
If older MAP source fails to compile, return STATUS: BLOCKED with exact owner and error.
```

## 5. 구현 요구

### 5.1 Variant config

Create:

```text
MoonPalaceRunVariantConfig.cs
```

Required config fields:

```text
variant_id
seed_id
seed_value
pattern_width = 4
pattern_height = 4
pattern_grid_width
pattern_grid_height
tile_width = pattern_grid_width * 4
tile_height = pattern_grid_height * 4
main_path_min_steps
main_path_max_steps
branch_min_count
branch_max_count
split_rejoin_min_count
allow_90_degree_rotation = false
allow_silent_carve = false
```

Required default variants:

```text
MP_RUN_02_A_BRANCHING_LOWLAND
MP_RUN_02_B_VERTICAL_LOOP
MP_RUN_02_C_MULTI_ROOM_SPLIT
```

The config must reject:

```text
tile size copied from sector constants
pattern grid width/height below required route shape
silent carve enabled
90-degree rotation enabled
duplicate variant IDs
```

### 5.2 Room and branch graph

Create:

```text
MoonPalaceRunRoomGraph.cs
```

The graph is over 4x4 pattern slots.

Required concepts:

```text
RunVariantGraph
RunRoomRegion
RunMainPathNode
RunBranchNode
RunSplitNode
RunRejoinNode
RunVerticalLink
RunSocketRequirement
RunVariantDigest
```

Graph requirements:

```text
each variant has start and exit on different horizontal sides
each variant has one main path from start to exit
each variant has the required branch count
variant B and C have at least 2 split-rejoin paths
variant B has visible vertical movement spanning at least 8 pattern rows
variant C has at least 3 room regions with different graph density/pacing
all branch entries are reachable from main path
all rejoin nodes are reachable from their split node
no graph node outside pattern grid
no accidental duplicate node unless tagged as intentional revisit
graph digest is deterministic
```

### 5.3 Variant composer

Create:

```text
MoonPalaceRunVariantComposer.cs
```

It must place actual 4x4 candidate masks into every pattern slot of every variant.

For every graph edge:

```text
selected pattern must expose required entry/exit sockets
neighbor sockets must be reciprocal
route cells inside the 4x4 mask remain open
branch/rejoin cells remain open
solid cells do not overwrite route/protection cells
90-degree rotation count remains 0
```

The composer must record:

```text
variant_id
pattern placement count
main route placement count
branch placement count
split/rejoin placement count
filler/detail placement count
candidate id per slot
mask_u16_hex per slot
socket signature per slot
transform per slot
MAP21_02 presentation family link when available
selection reason
attempt count
rejection count by reason
composition digest
```

Required total placement counts:

```text
Variant A placements: 48 * 14 = 672
Variant B placements: 42 * 18 = 756
Variant C placements: 60 * 16 = 960
Total placements: 2,388
```

Forbidden:

```text
draw route first and silently carve candidate masks
accept incompatible sockets
use static tilemap copies
use 48x32 sector generator output
```

### 5.4 Tile-level validator

Create:

```text
MoonPalaceRunVariantValidator.cs
```

It must validate each variant at 1x1 tile level.

Required checks per variant:

```text
tile size matches pattern_grid * 4
all pattern slots placed
start tile present/open
exit tile present/open
tile-level BFS start -> exit: PASS
all required waypoints reachable
all branch entries reachable from main path
all split -> rejoin paths reachable
route socket mismatch count: 0
out-of-bounds placement count: 0
duplicate placement count: 0
unreachable route/branch island count: 0
fallback carve count: 0
silent repair count: 0
```

Aggregate checks:

```text
variant count: 3
all variant BFS pass count: 3 / 3
total pattern placements: 2,388 / 2,388
total route socket mismatch count: 0
total route failure count: 0
total fallback carve/silent repair count: 0 / 0
```

This remains tile-open-cell reachability, not player-physics-perfect traversal.

### 5.5 Unity comparison Scene builder

Create:

```text
MoonPalaceRunVariantSceneBuilder.cs
```

Required scene:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN02/MoonPalaceRunVariants_RUN02.unity
```

Required scene contents:

```text
root GameObject: MoonPalace_RunVariants_RUN02
three variant roots: Variant_A, Variant_B, Variant_C
each variant has Terrain, RouteBranch, PatternBoundarySocketMarker debug layers
each variant has Start and Exit markers
each variant has labels showing variant id, tile size, branch count, split-rejoin count, map digest
one orthographic camera framing all three variants or a clear default selected variant
legend GameObjects explaining colors/layers
metadata GameObject containing aggregate digests
```

Visual requirements:

```text
three variants must look structurally different
variant B must visibly move vertically
variant C must visibly show separated room regions and split/rejoin choices
main route must be traceable from start to exit in every variant
branch paths must be visually distinct from filler/detail
4x4 pattern boundaries must be visible enough to inspect
the output must not look like one repeated corridor
```

Do not add the Scene to Build Settings.

### 5.6 Required artifacts

Generated JSON:

```text
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_config.json
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_graphs.json
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_compositions.json
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_validation.json
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_scene_manifest.json
MapDesign/MCP/GENERATED/RUN02/moonpalace_run02_digest_manifest.json
```

Authoring/debug CSV:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_variants.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_graph_nodes.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_graph_edges.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_pattern_slots.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_tile_cells.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/moonpalace_run02_validation_summary.csv
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
RUN02
```

Create:

```text
MoonPalaceRunVariantTests.cs
```

Required test names:

```text
RunVariantConfigDefinesThreePatternGridVariantsAndDerivesTileSizes
RunVariantConfigRejectsSectorDimensionsRotationAndSilentCarve
RunRoomGraphCreatesBranchingLowlandVerticalLoopAndMultiRoomSplitVariants
RunRoomGraphKeepsNodesInsideGridAndRecordsBranchesSplitsRejoins
RunVariantComposerUses500CandidatePoolAndPlacesAll2388PatternSlots
RunVariantComposerRejectsSocketMismatchStaticCopyAndSilentCarve
RunVariantValidatorProvesStartToExitReachabilityForAllThreeVariants
RunVariantValidatorProvesBranchEntriesAndSplitRejoinsReachable
RunVariantValidatorRecordsVerticalityRoomDensityAndStructuralDifference
RunVariantSceneBuilderCreatesComparisonSceneWithThreeVariantRoots
RunVariantSceneBuilderShowsPatternBoundariesRoutesBranchesLabelsAndLegend
RunVariantArtifactsAreDeterministicAcrossRepeatReverseAndCulture
RunVariantWritesOnlyRun02RootsAndDoesNotMutateExistingScenes
RunVariantWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun03
```

Expected focused test result:

```text
Discovered: 14
Executed: 14
Passed: 14
Failed: 0
Skipped: 0
Inconclusive: 0
```

## 7. Result Requirements

Write:

```text
MapDesign/MCP/REPORTS/RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS_RESULT.md
```

The Result must include:

```text
TASK: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
STATUS: PASS or FAIL or BLOCKED

# User-Facing Implementation Report
# Responsibility and Added Scripts
# Run Variant Config Summary
# Room and Branch Graph Summary
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
variant count: 3
variant ids: MP_RUN_02_A_BRANCHING_LOWLAND, MP_RUN_02_B_VERTICAL_LOOP, MP_RUN_02_C_MULTI_ROOM_SPLIT
candidate pool accepted count: 500
pattern size: 4 x 4
tile sizes derived from pattern grids: YES
48x32 sector primary outputs: 0
Variant A pattern grid/tile size: 48 x 14 / 192 x 56
Variant B pattern grid/tile size: 42 x 18 / 168 x 72
Variant C pattern grid/tile size: 60 x 16 / 240 x 64
total pattern placements: 2,388 / 2,388
all variant BFS start->exit: 3 / 3 PASS
all branch entries reachable: PASS
all split-rejoin paths reachable: PASS
variant B vertical span: at least 8 pattern rows
variant C room region count: at least 3
route socket mismatch count: 0
route failure count: 0
fallback carve count: 0
silent repair count: 0
scene path: Assets/_Game/Map/Scenes/MoonPalace/RUN02/MoonPalaceRunVariants_RUN02.unity
scene root: MoonPalace_RunVariants_RUN02
generated Unity scene count: 1
generated CSV count: 6
generated JSON count: 6
graph digest: present
composition digest: present
validation digest: present
scene manifest digest: present
```

Required responsibility table:

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
| MoonPalaceRunVariantConfig.cs | ... | ... |
| MoonPalaceRunRoomGraph.cs | ... | ... |
| MoonPalaceRunVariantComposer.cs | ... | ... |
| MoonPalaceRunVariantValidator.cs | ... | ... |
| MoonPalaceRunVariantSceneBuilder.cs | ... | ... |
| MoonPalaceRunVariantTests.cs | ... | ... |
| RUN02 CSV/JSON/Scene artifacts | ... | ... |
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
RUN03 files/runs: 0 / 0
```

If PASS, the Result must say:

```text
This is the first multi-variant direct 4x4 MicroPattern run Scene.
It is not a 48x32 sector demo, not full-world generation, not production art, not live player traversal, and not player build approval.
```

## 8. Commit Rule

If and only if Result status is PASS:

```text
record RUN02 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN02-owned source/test/artifact/task/report/status files
do not commit unrelated files
do not push
STOP
```

Do not start RUN03.
