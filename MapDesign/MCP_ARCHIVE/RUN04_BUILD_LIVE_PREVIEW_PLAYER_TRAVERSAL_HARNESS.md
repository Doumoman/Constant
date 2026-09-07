---
protocol: direct_visible_map_work_order_v4
task_id: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS
requires_result:
  path: MapDesign/MCP/REPORTS/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS_RESULT.md
  status: PASS
  sha256: 089c7ef014ce3020f1485e6804360758b7131d9dc06d23367583e9fb4fce47f6
requires_completed_task: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
next_task: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
---

# RUN04 - Build Live Preview Player Traversal Harness

```text
TASK: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS
STATUS: LOCKED
EXPECTED_RESULT: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS_RESULT.md
NEXT: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
```

## User-Facing Goal

RUN03源뚯???4x4 MicroPattern?쇰줈 留뚮뱺 camera-room course媛 **蹂댁씠???뺤쟻 Scene**?댁뿀??

RUN04??紐⑺몴??洹?Scene???щ엺??吏곸젒 ?뺤씤?????덇쾶 留뚮뱶??寃껋씠??

```text
RUN03 4x4 MicroPattern camera-room course
-> open/blocked tile traversal grid
-> player preview marker
-> room-bounded camera frames
-> connector gate trigger transitions
-> optional auto-route ghost
-> one Unity Scene that can be opened and manually played
```

?대쾲 ?묒뾽? PNG ?앹꽦???꾨땲??
?대쾲 ?묒뾽? 48x32 sector ?앹꽦???꾨땲??
?대쾲 ?묒뾽? full world generation???꾨땲??

?꾨즺?섎㈃ Unity?먯꽌 ?ㅼ쓬 Scene???댁뼱 留?援ъ“瑜?吏곸젒 ?뺤씤?????덉뼱???쒕떎.

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN04/MoonPalacePlayableRunPreview_RUN04.unity
```

## What This Task Adds

RUN04??RUN03 寃곌낵瑜?諛뷀깢?쇰줈 ?ㅼ쓬 湲곕뒫??異붽??쒕떎.

1. RUN03 course??open/blocked tile grid瑜?preview traversal grid濡?留뚮뱺??
2. Start tile??player preview marker瑜?諛곗튂?쒕떎.
3. 諛⑺뼢??WASD濡?open tile ?ъ씠瑜??대룞?????덇쾶 ?쒕떎.
4. blocked tile濡??대룞?섎젮???낅젰? 嫄곕??쒕떎.
5. connector gate瑜?吏?섎㈃ ?꾩옱 camera room??諛붾먮떎.
6. camera???꾩옱 room frame??留욎떠 snap ?먮뒗 short lerp濡??꾪솚?쒕떎.
7. ?먮룞 route ghost瑜?耳쒕㈃ start->exit BFS path瑜??곕씪 ?대룞 ?덉떆媛 蹂댁씤??
8. Scene ?덉뿉??room frame, connector gate, route path, current room label??蹂댁씤??

???묒뾽? ?ㅼ젣 寃뚯엫 ?뚮젅?댁뼱 controller瑜?遺숈씠???④퀎媛 ?꾨땲??
?대쾲 ?④퀎??player??**寃?ъ슜 preview marker**??

## Scope Lock

### Required

| Required output | Contract |
| --- | --- |
| Preview Scene | One RUN04-only Unity Scene |
| Traversal grid | Derived from RUN03 320x96 tile grid |
| Player marker | Starts on RUN03 open start tile |
| Manual movement | Arrow keys and WASD, open-tile only |
| Block rejection | blocked tile entry rejected and visibly counted |
| Camera room switch | connector gate crossing changes current room |
| Camera bounds | camera frames derive from RUN03 room tile bounds |
| Auto route ghost | optional start->exit path preview |
| Focused tests | RUN04 EditMode category only |

### Explicitly Not Required

| Not required | Reason |
| --- | --- |
| Final player physics | RUN04 is a preview harness, not platformer controller integration |
| Combat, NPC, shops, saves | Content population already belongs elsewhere |
| Production art pass | Debug tile/marker visuals are enough |
| Full world generation | RUN04 works on one RUN03 course |
| Android/player build | Not needed for editor inspection |
| Automated PlayMode test | Manual Play compatibility is required; automated selected tests stay EditMode |

## Prerequisite Check

Before doing any work, verify:

```text
RUN03 Result exists and STATUS: PASS
RUN03 Result SHA-256:
089c7ef014ce3020f1485e6804360758b7131d9dc06d23367583e9fb4fce47f6
```

If the SHA differs only because the Result file was moved without content changes, confirm byte SHA from the canonical report path.
If the actual content differs, stop as BLOCKED.

Expected RUN03 baseline:

```text
course id: MP_CAMERA_RUN_01
seed id/value: MP_QA_01 / 1924737067
candidate pool accepted count: 500
pattern size: 4 x 4
course pattern grid: 80 x 24
course tile size: 320 x 96
camera room count: 10
connector count: 9
pattern placements: 1,920 / 1,920
tile-level BFS start->exit: PASS
RUN04 files/runs: 0 / 0
```

If RUN03 finalize/commit is still pending, finish only RUN03 finalize/commit first.
Then start RUN04.

## Allowed Files

Keep edits scoped to RUN04 and the smallest read-only accessors needed for RUN03 reuse.

### Runtime

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewConfig.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewGrid.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewController.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewCameraController.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewRouteGhost.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewValidation.cs
```

### Editor

```text
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewSceneBuilder.cs
```

### Tests

```text
Assets/_Game/Map/Tests/EditMode/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewHarnessTests.cs
```

### Generated Scene and Artifacts

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN04/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/**
MapDesign/MCP/GENERATED/RUN04/**
MapDesign/MCP/TASKS/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS.md
MapDesign/MCP/REPORTS/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS_RESULT.md
MapDesign/MCP_ARCHIVE/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS.md
```

### Existing Files That May Be Read

RUN03 code and generated artifacts may be read.

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoom*.cs
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoomCourseSceneBuilder.cs
MapDesign/MCP/GENERATED/RUN03/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/**
```

If RUN03 exposes no safe way to reuse a value, add the smallest public read-only accessor to RUN03-owned code and report it.
Do not refactor RUN03 broadly.

## Forbidden

Do not do any of the following:

```text
legacy 19347 selection
full or unfiltered test run
automated PlayMode test selection
player build
Build Settings mutation
existing Scene/Prefab mutation outside RUN04
full world generation
48x32 sector generator as primary output
static screenshot/PNG as completion proof
copy-pasted static tilemap with no traversal grid
silent carve or fallback tunnel repair
90-degree MicroPattern rotation
production player controller replacement
combat/NPC/shop/save integration
RUN05 or later task start
git push
```

The only exception to the no-PlayMode rule is this:

```text
Creating MonoBehaviour scripts and a Scene that can be manually entered in Play Mode is required.
Selecting automated PlayMode tests is forbidden for this task.
```

## Implementation Requirements

### 1. Preview Config

Create `MoonPalaceRunPreviewConfig`.

It must define:

```text
preview id: MP_RUN_PREVIEW_01
source course id: MP_CAMERA_RUN_01
source task: RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS
seed id/value: MP_QA_01 / 1924737067
pattern size: 4 x 4
pattern grid: 80 x 24
tile grid: 320 x 96
expected room count: 10
expected connector count: 9
movement mode: TileStepOpenCell
camera mode: RoomBoundsSnapOrShortLerp
auto route ghost: enabled by default
```

It must hard reject:

```text
non-derived tile dimensions
48x32 sector primary input
fallback carve
silent repair
90-degree rotation
```

### 2. Preview Traversal Grid

Create `MoonPalaceRunPreviewGrid`.

It must contain a compact model for:

```text
tile width and height
open/blocked cells
start tile
exit tile
room id per tile when available
connector id per tile when available
main/branch/split route ownership when available
```

Required operations:

```text
bool IsInside(int x, int y)
bool IsOpen(int x, int y)
bool CanStep(int fromX, int fromY, int toX, int toY)
string GetRoomId(int x, int y)
string GetConnectorId(int x, int y)
IReadOnlyList<Vector2Int> FindRouteToExit()
```

`CanStep` must allow only cardinal neighbor movement on open cells.
Diagonal stepping is forbidden.
Blocked tile entry must return false.

This is not final platformer physics.
It is a deterministic tile-level inspection harness.

### 3. Preview Player Controller

Create `MoonPalaceRunPreviewController`.

Scene behavior:

```text
spawns or controls a visible player marker at start tile
accepts Arrow keys and WASD
moves one open tile per step
rejects blocked steps
updates current tile coordinate label
tracks current room id
tracks entered connector id
tracks successful step count
tracks blocked input count
R resets to start
G toggles route ghost visibility
```

Movement may be instant or short interpolated movement.
Do not require the production Player prefab.
Do not mutate existing player code.

### 4. Camera Room Controller

Create `MoonPalaceRunPreviewCameraController`.

It must:

```text
read room tile bounds from RUN03 course data
derive camera center from room tile bounds
derive orthographic size from room height/width with padding
start focused on start room
switch focus when the player enters a connector destination room
expose current room id and previous room id
support snap or short lerp
show a visible camera frame overlay per room in Scene
```

Camera transitions are room-bound preview transitions.
They are not final Cinemachine integration.
Do not require Cinemachine unless it already exists and is trivial to use.

### 5. Connector Gate Trigger Model

Every RUN03 connector gate must become an inspectable trigger record.

For each connector:

```text
connector id
from room id
to room id
from gate tile
to gate tile
direction
kind: main | branch | split | rejoin
required socket bit
is reciprocal
is open
```

Required validation:

```text
connector count: 9
all connectors reciprocal: PASS
all connector gate tiles open: PASS
all connector destination rooms resolvable: PASS
all connector transitions reachable by tile BFS: PASS
```

### 6. Auto Route Ghost

Create `MoonPalaceRunPreviewRouteGhost`.

It must:

```text
compute or load the start->exit path over the preview grid
display a visible route line or small markers
optionally animate a ghost marker along the route
include branch/split/rejoin markers where RUN03 data provides them
be toggleable from the preview controller
```

The ghost is a visual verification helper.
It must not alter the terrain grid.

### 7. Scene Builder

Create `MoonPalaceRunPreviewSceneBuilder`.

It must create or replace only:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN04/MoonPalacePlayableRunPreview_RUN04.unity
```

Scene contents:

```text
root GameObject: MoonPalace_PlayableRunPreview_RUN04
child: Terrain
child: MainRouteOverlay
child: BranchSplitOverlay
child: RoomFrameOverlay
child: ConnectorGateOverlay
child: PreviewPlayer
child: RouteGhost
child: PreviewCamera
child: Labels
child: Metadata
```

Required visible labels:

```text
preview id
source course id
seed
tile grid 320x96
room count 10
connector count 9
current room label placeholder
controls label: WASD/Arrow move, R reset, G ghost
```

The Scene must be usable for manual inspection:

```text
open Scene
enter Play Mode manually
move the preview marker through open tiles
watch camera switch room frames at connector gates
toggle route ghost
reset to start
```

Do not add this Scene to Build Settings.

### 8. Generated Artifacts

Create these JSON files:

```text
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_preview_config.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_traversal_grid_manifest.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_room_camera_frames.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_connector_triggers.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_route_ghost_path.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_validation.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_scene_manifest.json
MapDesign/MCP/GENERATED/RUN04/moonpalace_run04_digest_manifest.json
```

Create these CSV files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/moonpalace_run04_room_camera_frames.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/moonpalace_run04_connector_triggers.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/moonpalace_run04_route_ghost_path.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/moonpalace_run04_preview_controls.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN04/moonpalace_run04_validation_summary.csv
```

All generated JSON/CSV must be deterministic:

```text
UTF-8 without BOM
LF line endings
culture-invariant numbers
stable ordering
exactly one final LF
created_utc excluded from canonical digests
```

## Focused Tests

Create `MoonPalaceRunPreviewHarnessTests` with category:

```text
RUN04
```

Run only focused EditMode tests:

```text
filter type/value: category / RUN04
```

Expected:

```text
Discovered: 16
Executed: 16
Passed: 16
Failed: 0
Skipped: 0
Inconclusive: 0
```

Required test coverage:

1. RUN03 prerequisite result SHA/status is recognized.
2. Preview config uses `80 x 24` pattern grid and derives `320 x 96` tile grid.
3. Preview grid has exactly `320 x 96` cells.
4. Start and exit tiles are inside and open.
5. `CanStep` allows cardinal open neighbor movement.
6. `CanStep` rejects blocked target cells.
7. `CanStep` rejects diagonal movement.
8. BFS route from start to exit exists.
9. All 10 room frames are present and inside grid.
10. All 9 connector trigger records are present.
11. All connector gate tiles are open and reciprocal.
12. Connector transition lookup maps to the expected target room.
13. Camera frame centers and sizes derive from room bounds.
14. Scene contains required root and preview components.
15. JSON/CSV artifact counts and digests are stable.
16. No forbidden 48x32 sector primary output, fallback carve, silent repair, automated PlayMode selection, or Build Settings mutation is recorded.

Do not run broad regression unless RUN04 causes a compile error outside focused tests.
If that happens, report the reason and run only the smallest additional check needed to prove the repair.

## Result Report Required Format

Write:

```text
MapDesign/MCP/REPORTS/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS_RESULT.md
```

The report must include this exact top block:

```text
# RUN04 - Build Live Preview Player Traversal Harness Result

TASK: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS
STATUS: PASS|FAIL|BLOCKED
```

Then include:

```text
# User-Facing Implementation Report
```

Explain in plain Korean or English:

```text
what became visible in Unity
how the player preview marker moves
how camera rooms switch
how connector gates are represented
what is still not final gameplay
```

Then include:

```text
# Responsibility and Added Scripts
```

Use this table shape:

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| MoonPalaceRunPreviewConfig.cs | ... | ... |
| MoonPalaceRunPreviewGrid.cs | ... | ... |
| MoonPalaceRunPreviewController.cs | ... | ... |
| MoonPalaceRunPreviewCameraController.cs | ... | ... |
| MoonPalaceRunPreviewRouteGhost.cs | ... | ... |
| MoonPalaceRunPreviewValidation.cs | ... | ... |
| MoonPalaceRunPreviewSceneBuilder.cs | ... | ... |
| MoonPalaceRunPreviewHarnessTests.cs | ... | ... |
| RUN04 CSV/JSON/Scene artifacts | ... | ... |

Then include these sections:

```text
# Preview Config Summary
# Traversal Grid Summary
# Player Preview Summary
# Camera Room Transition Summary
# Connector Trigger Summary
# Route Ghost Summary
# Unity Scene Output Summary
# Artifact and Digest Summary
# Focused Validation Summary
# Explicitly Not Selected
# Final Status Evidence
```

The report must explicitly state:

```text
source RUN03 Result SHA-256
preview id
source course id
pattern grid
tile grid
room count
connector count
start tile
exit tile
manual controls
blocked input behavior
camera transition mode
auto route ghost status
scene path
scene root
focused test discovered/executed/passed count
automated PlayMode selections: 0
legacy 19347 selections: 0
full/unfiltered selections: 0
Build Settings mutations: 0
RUN05 files/runs: 0 / 0
commit hash if created
```

## PASS Criteria

PASS only if all are true:

```text
RUN03 prerequisite PASS verified
RUN04 Scene exists at the required path
Scene has preview player, preview camera, room frames, connector gates, route ghost
Preview grid is 320 x 96 and derived from 80 x 24 pattern grid
Player marker starts on open start tile
manual open-tile step behavior is implemented
blocked steps are rejected
connector crossing can change current camera room
all 10 room frames are represented
all 9 connector triggers are represented
route ghost path reaches exit
RUN04 focused EditMode tests pass 16/16
automated PlayMode tests are not selected
legacy 19347 is not selected
full/unfiltered tests are not selected
Build Settings are not mutated
no existing Scene/Prefab outside RUN04 is changed
RUN05 is not started
Result is written
RUN04-related files only are committed
```

## Failure Handling

If blocked, write the Result with:

```text
STATUS: BLOCKED
```

and include:

```text
blocking file
blocking reason
what was not changed
smallest next repair action
```

If failed after edits, write:

```text
STATUS: FAIL
```

and include:

```text
files changed
tests run
failure output summary
smallest repair plan
```

Do not broaden scope to make the task pass.
Do not start RUN05.

## Finalization

When PASS:

```text
record RUN04 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN04-owned source/test/artifact/task/report/status files
```

Commit message:

```text
RUN04 build live preview player traversal harness
```

Do not push.
Stop after reporting the Result path and commit hash.
