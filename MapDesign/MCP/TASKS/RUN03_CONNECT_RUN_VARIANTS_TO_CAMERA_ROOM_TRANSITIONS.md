```yaml
mapdesign_work_order:
  format: direct_visible_map_work_order_v3
  task_id: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
  based_on_previous_run_result:
    path: MapDesign/MCP/REPORTS/RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS_RESULT.md
    status: PASS
    sha256: 11aaa16805d1c01a69ba4d1c16ae78bb1f2471cc2fcbbb8448c620ef3faf6015
  requires_completed_task: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
  expected_duration: 1_to_2_hours
  work_style: larger_direct_micro_pattern_camera_room_structure
```

# RUN03 - Connect Run Variants to Camera Room Transitions

```text
TASK: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
PHASE: MicroPattern Run Generation / MoonPalace
STATUS: CURRENT
NEXT: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS
NEXT STATUS: NOT STARTED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. ?묒뾽 紐⑹쟻

RUN02??4x4 MicroPattern??吏곸젒 諛곗튂??援ъ“媛 ?ㅻⅨ reachable run variant 3媛쒕? 留뚮뱾?덈떎.

RUN03? 洹??ㅼ쓬 ?④퀎濡? 4x4 MicroPattern run??**移대찓??諛??⑥쐞**濡??섎늻怨?諛??ъ씠 connector/transition??紐낆떆???ㅼ젣 留?援ъ“泥섎읆 ?쏀엳寃?留뚮뱺??

?대쾲 ?묒뾽? ?ㅼ떆 48x32 sector濡??뚯븘媛???묒뾽???꾨땲??

```text
4x4 MicroPattern placements
-> variable camera room bounds in pattern coordinates
-> room-to-room connector gates
-> transition graph
-> tile-level start->exit and room-to-room BFS proof
-> Unity Scene with room frames, connectors, route, branches
```

PASS ??留먰븷 ???덈뒗 寃?

```text
4x4 MicroPattern?쇰줈 留뚮뱺 reachable run??移대찓??諛?援ъ“濡??섎늻怨?
諛??ъ씠 transition connector源뚯? 蹂댁씠??Unity Scene???앹꽦?????덈떎.
```

?꾩쭅 留먰븯吏 ?딆쓣 寃?

```text
live camera follow/transition runtime complete
player-physics-perfect traversal complete
full world generation complete
production art complete
NPC/combat/shop/save complete
player build approved
```

## 1. Camera Room 湲곕낯 ?뺥깭

RUN03? RUN02??multi-room 媛쒕뀗???ㅼ젣 camera room frame?쇰줈 留뚮뱺??

Default course:

```yaml
course_id: MP_CAMERA_RUN_01
seed_id: MP_QA_01
seed_value: 1924737067
candidate_pool_size: 500
pattern_size: [4, 4]
course_pattern_grid: [80, 24]
course_tile_size: [320, 96]
camera_room_count: 9_to_12
required_main_room_count: 7_to_9
optional_branch_room_count: 2_to_4
split_rejoin_count: 2_to_3
```

All tile sizes must be derived:

```text
tile_width = course_pattern_grid_width * 4
tile_height = course_pattern_grid_height * 4
room_tile_bounds = room_pattern_bounds * 4
```

Do not use sector constants as the source of dimensions. Do not use 48x32 as the primary output.

Suggested room sizes are in pattern units, not tile units:

| Room kind | Pattern size range | Tile size range |
|---|---:|---:|
| Start room | 10x8 to 12x8 | 40x32 to 48x32 |
| Transit room | 8x6 to 14x8 | 32x24 to 56x32 |
| Vertical room | 8x10 to 12x14 | 32x40 to 48x56 |
| Branch room | 6x5 to 10x8 | 24x20 to 40x32 |
| Exit room | 10x8 to 14x8 | 40x32 to 56x32 |

Note: individual camera rooms may have familiar sizes like 40x32. The forbidden pattern is using the old 48x32 sector generator or one fixed sector as the primary output.

## 2. ?ъ슜??蹂닿퀬 ?섎Т

Result 泥?遺遺꾩뿉??諛섎뱶???꾨옒 ???뱀뀡???붾떎.

```text
# User-Facing Implementation Report
# Responsibility and Added Scripts
```

`User-Facing Implementation Report`?먮뒗 ?꾨옒 吏덈Ц???듯븳??

```text
RUN03媛 RUN02?먯꽌 臾댁뾿???뺤옣?덈뒗媛?
???대쾲 ?묒뾽??48x32 sector ?묒뾽???꾨땲??MicroPattern camera-room run ?묒뾽?멸??
course pattern grid/tile size, room ?? main/branch/split-rejoin ?섎뒗 臾댁뾿?멸??
媛?camera room??pattern/tile bounds? ??븷? 臾댁뾿?멸??
諛??ъ씠 connector/transition gate???대뼡 醫뚰몴? 諛⑺뼢??媛吏?붽??
4x4 MicroPattern 諛곗튂媛 諛?寃쎄퀎? connector瑜??대뼸寃?留뚯”?섎뒗媛?
tile-level BFS媛 start->exit, room-to-room, branch/rejoin???대뼸寃?利앸챸?덈뒗媛?
Unity Scene?먯꽌 room frame, connector, route, branch, start/exit媛 ?대뼸寃?蹂댁씠?붽??
?대쾲 Task?먯꽌 異붽?/蹂寃쏀븳 script? 媛?梨낆엫? 臾댁뾿?멸??
?꾩쭅 live camera runtime, full world, production art, NPC/combat/shop/save媛 ?꾨땲?쇰뒗 寃쎄퀎??臾댁뾿?멸??
legacy 19347, prior category, PlayMode, full/unfiltered test瑜??섏? ?딆븯?ㅻ뒗 利앷굅??臾댁뾿?멸??
RUN04?????꾩쭅 ?쒖옉?섏? ?딆븯?붽??
```

`Responsibility and Added Scripts`??諛섎뱶???쒕줈 ?묒꽦?쒕떎.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

?뚯씪蹂?梨낆엫??援ъ껜?곸씠吏 ?딆쑝硫?FAIL?대떎.

## 3. ?좏뻾議곌굔

?묒뾽 ?쒖옉 ?꾩뿉 ?꾨옒瑜??뺤씤?쒕떎.

```text
RUN02 Result exists and STATUS: PASS
RUN02 Result SHA-256:
11aaa16805d1c01a69ba4d1c16ae78bb1f2471cc2fcbbb8448c620ef3faf6015

RUN02 direct MicroPattern baseline:
variant count: 3
candidate pool accepted count: 500
total pattern placements: 2,388 / 2,388
all variant BFS start->exit: 3 / 3 PASS
48x32 sector primary outputs: 0
```

If RUN02 finalize/commit is still pending, finish only RUN02 finalize/commit first. Then start RUN03.

If RUN02 Result is missing or not PASS:

```text
STATUS: BLOCKED
reason: RUN02 multi-variant MicroPattern baseline not available
created/changed production code files: 0
STOP
```

## 4. ?덉슜 踰붿쐞

?덉슜 ?뚯씪:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomCourseConfig.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomConnectorPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomPatternComposer.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomCourseValidator.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomCourseSceneBuilder.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomCourseTests.cs
Assets/_Game/Map/Scenes/MoonPalace/RUN03/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/**
MapDesign/MCP/GENERATED/RUN03/**
MapDesign/MCP/TASKS/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS.md
MapDesign/MCP/REPORTS/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS_RESULT.md
MapDesign/MCP_ARCHIVE/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

議곌굔遺 ?덉슜:

```text
RUN01/RUN02 candidate/run classes may be reused or called.
If RUN01/RUN02 code lacks a tiny read-only public accessor needed for reuse, expose the smallest accessor and report it.
MAP21 CSV may be read-only for biome/tile/presentation tags.
The isolated RUN03 Scene may use generated debug Tile assets only under Assets/_Game/Map/Scenes/MoonPalace/RUN03.
Tilemap writes are allowed only while creating/updating the isolated RUN03 Scene.
```

湲덉?:

```text
48x32 sector generator as the primary output
VIS01/VIS02 window polish or one-sector regeneration as the primary output
whole-world 13x13 generation
live camera movement runtime script as a required PASS condition
existing Unity Scene/Prefab mutation outside RUN03
Build Settings mutation
MAP21 authoring CSV rewrite
hardcoded static course counted as generation
silent fallback carve
reroll-until-valid without recording attempts
test-only fixture copied as production source
PlayMode tests
legacy 19347 regression
prior category rerun
unfiltered/full test run
player build execution
RUN04 or later task start
```

Compile repair exception:

```text
If RUN03-owned code fails to compile, fix only RUN03-owned files.
If RUN01/RUN02 API needs a tiny public read-only accessor, add the smallest accessor and record why.
If older MAP source fails to compile, return STATUS: BLOCKED with exact owner and error.
```

## 5. 援ы쁽 ?붽뎄

### 5.1 Course config

Create:

```text
MoonPalaceCameraRoomCourseConfig.cs
```

Required fields:

```text
course_id
seed_id
seed_value
pattern_width = 4
pattern_height = 4
course_pattern_grid_width = 80
course_pattern_grid_height = 24
tile_width = course_pattern_grid_width * 4
tile_height = course_pattern_grid_height * 4
candidate_count = 500
camera_room_min_count
camera_room_max_count
required_main_room_min_count
required_main_room_max_count
optional_branch_room_min_count
optional_branch_room_max_count
split_rejoin_min_count
split_rejoin_max_count
allow_90_degree_rotation = false
allow_silent_carve = false
```

The config must reject:

```text
course tile size copied from sector constants
room bounds outside course grid
overlapping rooms unless explicitly linked as shared connector band
silent carve enabled
90-degree rotation enabled
duplicate course IDs
```

### 5.2 Camera room graph

Create:

```text
MoonPalaceCameraRoomGraph.cs
```

The graph is over camera rooms and 4x4 pattern slots.

Required concepts:

```text
CameraRoomNode
CameraRoomBoundsPattern
CameraRoomRole
CameraRoomEdge
CameraRoomTransition
CameraRoomConnectorGate
CameraRoomMainRoute
CameraRoomBranchRoute
CameraRoomSplitRejoinRoute
CameraRoomGraphDigest
```

Graph requirements:

```text
room count: 9 to 12
main route room count: 7 to 9
branch room count: 2 to 4
split-rejoin count: 2 to 3
start room and exit room are different and visually separated
at least one vertical transition chain spans 10 or more pattern rows
each room has nonzero interior pattern slots
each transition has a reciprocal connector gate pair
connector gates lie on room edges and align in tile coordinates
all graph nodes and room bounds are inside the course pattern grid
graph digest is deterministic
```

### 5.3 Connector planner

Create:

```text
MoonPalaceCameraRoomConnectorPlanner.cs
```

It must assign door/transition gates in pattern coordinates and tile coordinates.

Required per connector:

```text
connector_id
from_room_id
to_room_id
direction
from_pattern_slot
to_pattern_slot
from_tile_gate_rect
to_tile_gate_rect
required_socket_from
required_socket_to
transition_kind: horizontal, vertical_up, vertical_down, branch, split, rejoin
is_required_for_completion
```

Validation rules:

```text
from/to gates must be reciprocal
gate tile rect must be open after composition
connector must not be blocked by filler/detail patterns
connector must not require 48x32 sector seam logic
```

### 5.4 Pattern composer

Create:

```text
MoonPalaceCameraRoomPatternComposer.cs
```

It must place actual 4x4 candidates into the whole course pattern grid.

Requirements:

```text
pattern placements: 80 * 24 = 1,920
every room interior slot has a candidate
every connector slot has a candidate with the required socket
every route slot records room/connector/route ownership
filler slots outside rooms may be solid/detail/quiet, but must not break route connectivity
candidate id, mask hex, socket signature, transform, MAP21_02 family link, selection reason are recorded
90-degree rotation count: 0
fallback carve count: 0
silent repair count: 0
```

The composer must not paste RUN02 variant tilemaps. It may reuse candidate selection and graph concepts, but the RUN03 room/connector course must have its own course digest and placement records.

### 5.5 Course validator

Create:

```text
MoonPalaceCameraRoomCourseValidator.cs
```

It must validate at tile level.

Required checks:

```text
course tile size: 320 x 96
pattern grid: 80 x 24
pattern placements: 1,920 / 1,920
room count: 9 to 12
all room bounds inside grid: PASS
all connector gates reciprocal: PASS
all connector gates open: PASS
start tile present/open
exit tile present/open
tile-level BFS start -> exit: PASS
all main route rooms reachable in order
all branch entries reachable from main route
all split -> rejoin paths reachable
unreachable required room count: 0
route socket mismatch count: 0
connector blocked count: 0
fallback carve count: 0
silent repair count: 0
```

This is still tile-open-cell reachability. Do not claim player-physics-perfect traversal.

### 5.6 Unity Scene builder

Create:

```text
MoonPalaceCameraRoomCourseSceneBuilder.cs
```

Required scene:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN03/MoonPalaceCameraRoomRun_RUN03.unity
```

Required scene contents:

```text
root GameObject: MoonPalace_CameraRoomRun_RUN03
Terrain tile/debug layer
MainRoute tile/debug layer
BranchSplitRejoin tile/debug layer
RoomFrameConnector tile/debug layer
Start marker
Exit marker
Camera room labels
Connector labels or arrows
Legend GameObjects
Metadata GameObject containing course id, seed, pattern grid, tile size, room count, connector count, digests
Orthographic camera framing the whole course or default selected room group
```

Visual requirements:

```text
room frames must be clearly visible
connector gates between rooms must be inspectable
main route must be traceable across multiple camera rooms
branch and split/rejoin paths must be visually distinct
4x4 MicroPattern grid must remain visible enough to inspect
the output must not look like one unsegmented corridor
```

Do not add the Scene to Build Settings.

### 5.7 Required artifacts

Generated JSON:

```text
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_course_config.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_room_graph.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_connectors.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_composition.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_validation.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_scene_manifest.json
MapDesign/MCP/GENERATED/RUN03/moonpalace_run03_digest_manifest.json
```

Authoring/debug CSV:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_rooms.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_connectors.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_pattern_slots.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_tile_cells.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_route_paths.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/moonpalace_run03_validation_summary.csv
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
RUN03
```

Create:

```text
MoonPalaceCameraRoomCourseTests.cs
```

Required test names:

```text
CameraRoomCourseConfigDerivesCourseAndRoomTileBoundsFromPatternGrid
CameraRoomCourseConfigRejectsSectorSizingRotationAndSilentCarve
CameraRoomGraphCreatesNineToTwelveRoomsWithMainBranchAndSplitRejoinRoutes
CameraRoomGraphKeepsRoomBoundsInsideGridAndStructurallySeparated
CameraRoomConnectorPlannerCreatesReciprocalOpenRoomGates
CameraRoomConnectorPlannerDoesNotUseSectorSeamLogic
CameraRoomPatternComposerUses500CandidatePoolAndPlacesAll1920Slots
CameraRoomPatternComposerRecordsRoomConnectorRouteOwnershipPerSlot
CameraRoomPatternComposerRejectsBlockedConnectorsStaticCopiesAndSilentCarve
CameraRoomCourseValidatorProvesStartToExitAndRoomToRoomReachability
CameraRoomCourseValidatorProvesBranchAndSplitRejoinReachability
CameraRoomSceneBuilderCreatesIsolatedSceneWithRoomFramesConnectorsLabelsAndLegend
CameraRoomArtifactsAreDeterministicAcrossRepeatReverseAndCulture
CameraRoomWritesOnlyRun03RootsAndDoesNotMutateExistingScenes
CameraRoomWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun04
```

Expected focused test result:

```text
Discovered: 15
Executed: 15
Passed: 15
Failed: 0
Skipped: 0
Inconclusive: 0
```

## 7. Result Requirements

Write:

```text
MapDesign/MCP/REPORTS/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS_RESULT.md
```

The Result must include:

```text
TASK: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
STATUS: PASS or FAIL or BLOCKED

# User-Facing Implementation Report
# Responsibility and Added Scripts
# Camera Room Course Config Summary
# Room Graph and Connector Summary
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
course id: MP_CAMERA_RUN_01
seed id/value: MP_QA_01 / 1924737067
candidate pool accepted count: 500
pattern size: 4 x 4
course pattern grid: 80 x 24
course tile size: 320 x 96
tile sizes derived from pattern grids: YES
48x32 sector primary outputs: 0
camera room count: between 9 and 12
main route room count: between 7 and 9
branch room count: between 2 and 4
split-rejoin count: between 2 and 3
vertical transition span: at least 10 pattern rows
pattern placements: 1,920 / 1,920
all room bounds inside grid: PASS
all connector gates reciprocal: PASS
all connector gates open: PASS
tile-level BFS start->exit: PASS
all main route rooms reachable in order: PASS
all branch entries reachable: PASS
all split-rejoin paths reachable: PASS
unreachable required room count: 0
route socket mismatch count: 0
connector blocked count: 0
fallback carve count: 0
silent repair count: 0
scene path: Assets/_Game/Map/Scenes/MoonPalace/RUN03/MoonPalaceCameraRoomRun_RUN03.unity
scene root: MoonPalace_CameraRoomRun_RUN03
generated Unity scene count: 1
generated CSV count: 6
generated JSON count: 7
room graph digest: present
connector digest: present
composition digest: present
validation digest: present
scene manifest digest: present
```

Required responsibility table:

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
| MoonPalaceCameraRoomCourseConfig.cs | ... | ... |
| MoonPalaceCameraRoomGraph.cs | ... | ... |
| MoonPalaceCameraRoomConnectorPlanner.cs | ... | ... |
| MoonPalaceCameraRoomPatternComposer.cs | ... | ... |
| MoonPalaceCameraRoomCourseValidator.cs | ... | ... |
| MoonPalaceCameraRoomCourseSceneBuilder.cs | ... | ... |
| MoonPalaceCameraRoomCourseTests.cs | ... | ... |
| RUN03 CSV/JSON/Scene artifacts | ... | ... |
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
RUN04 files/runs: 0 / 0
```

If PASS, the Result must say:

```text
This is the first direct 4x4 MicroPattern camera-room run Scene.
It is not a 48x32 sector demo, not live camera runtime, not production art, not live player traversal, and not player build approval.
```

## 8. Commit Rule

If and only if Result status is PASS:

```text
record RUN03 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN03-owned source/test/artifact/task/report/status files
do not commit unrelated files
do not push
STOP
```

Do not start RUN04.
