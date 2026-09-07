---
protocol: direct_visible_map_work_order_v5
task_id: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
requires_result:
  path: MapDesign/MCP/REPORTS/RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS_RESULT.md
  status: PASS
  sha256: fb632fce9d8905ed92443517e07e152ef3ba59752e7b2255ff4c0298a3302914
requires_completed_task: RUN04_BUILD_LIVE_PREVIEW_PLAYER_TRAVERSAL_HARNESS
next_task: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
---

# RUN05 - Create Seed Regenerate and Variant Selection Tool

```text
TASK: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
STATUS: LOCKED
EXPECTED_RESULT: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL_RESULT.md
NEXT: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
```

## User-Facing Goal

RUN04 created a manually inspectable playable preview Scene for one reviewed RUN03 camera-room course.

RUN05 must turn that from a one-off preview into a small practical generation tool:

```text
seed input
-> recipe/variant selection
-> direct 4x4 MicroPattern course generation
-> focused validation
-> playable preview Scene regeneration
-> seed gallery comparison inside Unity
```

The final output is not a PNG.
The final output is not a 48x32 sector.
The final output is not another abstract report-only task.

The user must be able to open Unity, choose a recipe and seed, regenerate the RUN05 preview Scene, and visually compare several generated runs.

## Required Final Unity Outputs

Create these Unity-facing outputs:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN05/MoonPalaceSeedRegeneratePreview_RUN05.unity
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewGeneratorWindow.cs
```

The Scene must contain:

```text
root: MoonPalace_SeedRegeneratePreview_RUN05
child: ActivePreview
child: SeedGallery
child: PreviewPlayer
child: PreviewCamera
child: RouteGhost
child: RoomFrames
child: ConnectorGates
child: Labels
child: Metadata
```

The Editor Window must be accessible from a Unity menu item:

```text
Tools/MoonPalace/Run Preview Generator
```

## What This Task Adds

RUN05 adds a controlled regeneration loop over the already proven 4x4 MicroPattern approach.

Required behavior:

1. The tool exposes an integer seed field.
2. The tool exposes a recipe dropdown.
3. The tool has a Generate Preview button.
4. The tool has a Validate Current button.
5. The tool has a Rebuild Seed Gallery button.
6. Generate Preview creates or replaces only the RUN05 preview Scene/artifacts.
7. The generated active preview is manually traversable using the RUN04 preview marker behavior.
8. The generated active preview has room frames, connector gates, route ghost, labels, and metadata.
9. The seed gallery shows at least four deterministic generated courses for visual comparison.
10. Every generated course uses actual 4x4 candidates from the 500-item candidate pool.

This task does not need production UI polish.
This task does need to be useful to the user inside Unity.

## Scope Lock

### Required

| Required output | Contract |
| --- | --- |
| Editor Window | Seed input, recipe dropdown, generate, validate, gallery |
| Active Preview Scene | One RUN05 Scene with playable preview marker |
| Seed Gallery | At least 4 generated courses visible in the same Scene |
| Recipe Set | At least 3 structural recipes with different dimensions and pacing |
| Candidate Source | 500-item direct 4x4 MicroPattern pool |
| Validation | start-to-exit, rooms, connectors, route ghost, no forbidden repair |
| Focused Tests | RUN05 EditMode category only |

### Explicitly Not Required

| Not required | Reason |
| --- | --- |
| Final game player controller | RUN05 still uses the preview marker |
| Final camera system | RUN05 still uses debug preview camera bounds |
| Production art | Debug tiles/labels are enough |
| Full world generation | Tool operates on one camera-room course at a time |
| Android/player build | Not needed for editor inspection |
| Automated PlayMode test selection | Manual Play compatibility is enough for this task |

## Prerequisite Check

Before doing any work, verify:

```text
RUN04 Result exists and STATUS: PASS
RUN04 Result SHA-256:
fb632fce9d8905ed92443517e07e152ef3ba59752e7b2255ff4c0298a3302914
```

Expected RUN04 baseline:

```text
source course id: MP_CAMERA_RUN_01
preview id: MP_RUN_PREVIEW_01
pattern grid: 80 x 24
tile grid: 320 x 96
room count: 10
connector count: 9
manual controls: WASD/Arrow, R reset, G ghost
focused tests: 16 / 16 PASS
legacy 19347 selections: 0
automated PlayMode selections: 0
RUN05 files/runs: 0 / 0
```

If the RUN04 Result is missing, not PASS, or has different bytes, stop as BLOCKED unless the only difference is a file relocation with identical canonical content.

If RUN04 finalize/commit is still pending, finish only RUN04 finalize/commit first.
Then start RUN05.

## Allowed Files

Keep edits scoped to RUN05 and tiny read-only reuse accessors in RUN04/RUN03 if absolutely required.

### Runtime

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunRecipe.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunRecipeCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunGenerator.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunValidator.cs
```

### Editor

```text
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewGeneratorWindow.cs
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeedRegenerateSceneBuilder.cs
```

### Tests

```text
Assets/_Game/Map/Tests/EditMode/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeedRegenerateToolTests.cs
```

### Generated Scene and Artifacts

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN05/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/**
MapDesign/MCP/GENERATED/RUN05/**
MapDesign/MCP/TASKS/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL.md
MapDesign/MCP/REPORTS/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL_RESULT.md
MapDesign/MCP_ARCHIVE/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL.md
```

### Existing Files That May Be Read

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceMicroPattern*.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreview*.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCameraRoom*.cs
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreviewSceneBuilder.cs
MapDesign/MCP/GENERATED/RUN04/**
MapDesign/MCP/GENERATED/RUN03/**
```

If a tiny public read-only accessor is needed for reuse, add the smallest accessor and report it.
Do not broadly refactor previous RUN files in this task.

## Forbidden

Do not do any of the following:

```text
legacy 19347 selection
full or unfiltered test run
automated PlayMode test selection
player build
Build Settings mutation
existing Scene/Prefab mutation outside RUN05
full world generation
48x32 sector generator as primary output
static screenshot/PNG as completion proof
static one-map copy with no seed regeneration path
silent carve or fallback tunnel repair
90-degree MicroPattern rotation
reroll-until-valid infinite loop
production player controller replacement
combat/NPC/shop/save integration
RUN06 or later task start
git push
```

Bounded deterministic attempts are allowed only for selecting compatible 4x4 candidates or course recipes.
If attempts are exhausted, record a visible generation failure result.
Do not silently repair the map.

## Recipe Requirements

Create at least three recipes in `MoonPalaceSeededRunRecipeCatalog`.

They must be structurally different and not just different seeds.

Required minimum recipes:

| Recipe id | Pattern grid | Tile grid | Main room target | Branch target | Split/rejoin target | Required shape |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `WIDE_BRANCH_RUN` | `96 x 24` | `384 x 96` | 8 to 11 | 3 to 5 | 1 to 2 | wide horizontal run with side branches |
| `TALL_LOOP_RUN` | `72 x 28` | `288 x 112` | 8 to 10 | 2 to 4 | 2 to 3 | vertical climb/drop and loop-like return |
| `COMPACT_SPLIT_RUN` | `64 x 20` | `256 x 80` | 7 to 9 | 2 to 3 | 1 to 2 | compact route with clear split/rejoin |

All tile sizes must be derived by multiplying pattern grid dimensions by four.

The old `48x32` sector size must not be used as the primary source of any course dimension.

## Seed Behavior

The generator must accept an integer seed and produce deterministic output.

Required fixed gallery seeds:

```text
1924737067
1924737068
1924737069
1924737070
```

Required evidence:

```text
same recipe + same seed -> identical digest
same recipe + different seed -> different placement digest
different recipe + same seed -> different room graph digest
```

The generator must not rely on editor object order, current time, system locale, or Unity instance state for canonical generation.

## Generation Requirements

`MoonPalaceSeededRunGenerator` must generate a complete course result.

Each result must include:

```text
recipe id
seed
pattern grid
tile grid
room records
connector records
4x4 pattern placement records
start tile
exit tile
route ghost path
validation summary
stable digests
```

Every pattern slot in the active preview must contain an actual candidate from the audited 500-item 4x4 pool.

Each generated course must satisfy:

```text
all room bounds inside grid
all connector gate pairs reciprocal
all connector gate tiles open
all required route slots assigned a candidate with required socket
start tile open
exit tile open
tile-level BFS start->exit PASS
all main rooms reachable in order
all branch entries reachable
all split/rejoin paths reachable
fallback carve count: 0
silent repair count: 0
90-degree rotation count: 0
```

If a recipe/seed cannot satisfy the constraints within the bounded attempt budget, fail that generated result with a clear reason.
Do not patch the terrain after validation to force a pass.

## Editor Window Requirements

Create `MoonPalaceRunPreviewGeneratorWindow`.

It must provide:

```text
menu item: Tools/MoonPalace/Run Preview Generator
recipe dropdown listing all recipes
integer seed field
button: Generate Preview Scene
button: Validate Current
button: Rebuild Seed Gallery
summary area showing last recipe, seed, digest, room count, connector count, BFS status
last generated Scene path
last error or failure reason
```

It must call generation and scene building code directly.

It must not:

```text
run broad tests
run player build
modify Build Settings
write outside RUN05 paths
start RUN06
```

## Scene Builder Requirements

Create `MoonPalaceSeedRegenerateSceneBuilder`.

It must create or replace only:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN05/MoonPalaceSeedRegeneratePreview_RUN05.unity
```

Scene requirements:

```text
root GameObject: MoonPalace_SeedRegeneratePreview_RUN05
ActivePreview shows the currently selected recipe/seed result
SeedGallery shows at least four generated courses side by side or vertically stacked
PreviewPlayer starts at the active preview start tile
PreviewCamera starts on the active preview start room
RouteGhost is visible/toggleable for active preview
RoomFrames and ConnectorGates are visible for active preview and gallery
Labels show recipe id, seed, tile grid, room count, connector count, digest, controls
```

The active preview must remain manually inspectable using the RUN04 controls:

```text
WASD/Arrow move
R reset
G ghost toggle
blocked steps rejected
connector crossing changes current camera room
```

Do not add the Scene to Build Settings.

## Generated Artifacts

Create these JSON files:

```text
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_recipe_catalog.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_active_generation.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_gallery_generations.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_room_graphs.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_connectors.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_pattern_placements.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_validation.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_scene_manifest.json
MapDesign/MCP/GENERATED/RUN05/moonpalace_run05_digest_manifest.json
```

Create these CSV files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_recipe_catalog.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_gallery_summary.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_active_rooms.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_active_connectors.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_active_pattern_slots.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_active_route_path.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/moonpalace_run05_validation_summary.csv
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

Create `MoonPalaceSeedRegenerateToolTests` with category:

```text
RUN05
```

Run only focused EditMode tests:

```text
filter type/value: category / RUN05
```

Expected:

```text
Discovered: 18
Executed: 18
Passed: 18
Failed: 0
Skipped: 0
Inconclusive: 0
```

Required test coverage:

1. RUN04 prerequisite result SHA/status is recognized.
2. Recipe catalog contains at least three recipes.
3. Required recipe ids are present.
4. Each recipe derives tile grid from pattern grid times four.
5. No recipe uses 48x32 sector as primary dimensions.
6. Same recipe and same seed produce identical digest.
7. Same recipe and different seed produce different placement digest.
8. Different recipe and same seed produce different room graph digest.
9. Active generation places actual 4x4 candidates in every slot.
10. Active generation start and exit are open.
11. Active generation BFS reaches exit.
12. All active room frames are inside the tile grid.
13. All active connectors are reciprocal and open.
14. Gallery generates at least four successful visible courses.
15. Editor window exposes recipe, seed, generate, validate, and gallery controls.
16. RUN05 Scene contains required root and visible child groups.
17. JSON/CSV artifact counts and digests are stable.
18. No forbidden legacy 19347, automated PlayMode selection, full/unfiltered run, fallback carve, silent repair, Build Settings mutation, or RUN06 start is recorded.

Do not run broad regression unless RUN05 causes a compile error outside focused tests.
If that happens, report the reason and run only the smallest additional check needed to prove the repair.

## Result Report Required Format

Write:

```text
MapDesign/MCP/REPORTS/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL_RESULT.md
```

The report must include this exact top block:

```text
# RUN05 - Create Seed Regenerate and Variant Selection Tool Result

TASK: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
STATUS: PASS|FAIL|BLOCKED
```

Then include:

```text
# User-Facing Implementation Report
```

Explain in plain Korean or English:

```text
what the user can now do in Unity
how seed regeneration works
which recipes exist
how the active preview Scene changes
what the gallery proves
what remains outside this task
```

Then include:

```text
# Responsibility and Added Scripts
```

Use this table shape:

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| MoonPalaceSeededRunRecipe.cs | ... | ... |
| MoonPalaceSeededRunRecipeCatalog.cs | ... | ... |
| MoonPalaceSeededRunGenerator.cs | ... | ... |
| MoonPalaceSeededRunResult.cs | ... | ... |
| MoonPalaceSeededRunValidator.cs | ... | ... |
| MoonPalaceRunPreviewGeneratorWindow.cs | ... | ... |
| MoonPalaceSeedRegenerateSceneBuilder.cs | ... | ... |
| MoonPalaceSeedRegenerateToolTests.cs | ... | ... |
| RUN05 CSV/JSON/Scene artifacts | ... | ... |

Then include these sections:

```text
# Tool Usage Summary
# Recipe Catalog Summary
# Seed Determinism Summary
# Active Preview Generation Summary
# Seed Gallery Summary
# Unity Scene Output Summary
# Artifact and Digest Summary
# Focused Validation Summary
# Explicitly Not Selected
# Final Status Evidence
```

The report must explicitly state:

```text
source RUN04 Result SHA-256
Editor menu item
active recipe id
active seed
active pattern grid
active tile grid
active room count
active connector count
candidate pool count
active pattern placement count
gallery course count
same seed digest stability
different seed digest difference
scene path
scene root
focused test discovered/executed/passed count
automated PlayMode selections: 0
legacy 19347 selections: 0
full/unfiltered selections: 0
Build Settings mutations: 0
RUN06 files/runs: 0 / 0
commit hash
```

## PASS Criteria

PASS only if all are true:

```text
RUN04 prerequisite PASS verified
Editor Window exists at the required menu path
RUN05 Scene exists at the required path
Scene has ActivePreview, SeedGallery, PreviewPlayer, PreviewCamera, RouteGhost, RoomFrames, ConnectorGates, Labels, Metadata
at least three structural recipes exist
all required recipe ids exist
active generation accepts a seed and recipe
same seed generation is deterministic
different seeds produce different placement digests
different recipes produce different room graph digests
active preview places actual candidates from the 500-item 4x4 pool
active preview BFS reaches exit
all active connectors are reciprocal/open
seed gallery shows at least four generated courses
manual RUN04-style controls are preserved for active preview
RUN05 focused EditMode tests pass 18/18
automated PlayMode tests are not selected
legacy 19347 is not selected
full/unfiltered tests are not selected
Build Settings are not mutated
no existing Scene/Prefab outside RUN05 is changed
RUN06 is not started
Result is written
RUN05-related files only are committed
commit hash is included in the Result
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
Do not start RUN06.

## Finalization

When PASS:

```text
record RUN05 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN05-owned source/test/artifact/task/report/status files
include the commit hash inside the Result before handing off
```

Commit message:

```text
RUN05 create seed regenerate and variant selection tool
```

Do not push.
Stop after reporting the Result path and commit hash.
