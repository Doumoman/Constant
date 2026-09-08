# RMAP01 - Rebase v4.2 Plan and Current Map Bindings Result

TASK: RMAP01_REBASE
STATUS: PASS

## User-Facing Implementation Report

The RMAP v4.2 work order is now registered as a 19-task, one-at-a-time plan.
This task changes only the MapDesign operating documents and records the
current implementation seams. It does not claim a new playable map: no game
code, Scene, Prefab, Authoring data, or Unity setting was changed.

The first real Player work remains RMAP02. It has a concrete reuse path for the
existing Player rig, Input System snapshots, physics movement, Player prefab,
logical map bake plan, and test assemblies. It also has two deliberate missing
seams: a real runtime Tilemap/Collider applier and a continuous camera-follow
driver. Those are marked `PROPOSED`, not pre-created or represented as done.

## Responsibility and Files

| Path | Change | Responsibility | Explicit non-ownership |
| --- | --- | --- | --- |
| `MCP/RMAP/00_BASELINE_V4_2.md` | added | one-time v4.2 baseline and RUN06 evidence | gameplay implementation |
| `MCP/RMAP/01_SEQUENCE_V4_2.md` | added | ordered RMAP01~19 queue and 29-task crosswalk | old task migration |
| `MCP/RMAP/02_PROTOCOL_V4_2.md` | added | normal follow-up SHA/lock and evidence rules | generic protocol bypass |
| `MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` | changed | link and register the new 19-row plan | completed MAP/VIS/RUN history |
| `MCP/06_IMPLEMENTATION_STATUS.md` | changed | close RMAP01 and retain RMAP02~19 locks | unrelated status correction |
| `MCP/TASKS/RMAP01_REBASE.md` | added byte-identically | installed task body | source-body editing |
| `MCP_ARCHIVE/RMAP01_REBASE.md` | moved byte-identically | immutable applied inbox evidence | source-body editing |
| `MCP/GENERATED/RMAP01/*` | added | baseline, inventory, dependency, reuse and binding evidence | Unity output |
| this Result | added | user-facing RMAP01 completion evidence | RMAP02 execution |

## Preconditions and Registration

- Baseline branch / HEAD: `main` / `f4da626c5cc509954a41e67b24e5d7113d01a50b`.
- Baseline Current Task: `NONE`; active status table was `221 COMPLETE / 0 CURRENT / 0 LOCKED`.
- RUN06 installed Task SHA-256: `42f3a6b575c52c8463951f144da31f4b8dfdb52fdf093d1e894dac47810c467b`.
- RUN06 PASS Result SHA-256: `4a1dfe19d47b624f06e1a6e98ca448da916a3e6d3d098f5ade68eb3b1a1ad6f7`.
- RUN06's current committed version is `f4da626…`; the Result's self-reported
  `a409c734…` is its parent. Neither RUN06 file has a worktree diff, including
  no EOL-only diff (`text=auto` is attribute metadata only).
- The old `Last Completed Task` block names RUN01, while the RUN06 package and
  history say COMPLETE. This was recorded as legacy/stale evidence and not
  rewritten.
- RMAP01 source, installed Task, and archive SHA-256 are all
  `65cb0479876770d1aefabf1d48315744eba49e797433a9cf354018218cd24b8e`.
- Master contains the v4.2 plan once. Status registered 19 RMAP rows, opened
  RMAP01 only, and after Finalize is `240 rows = 222 COMPLETE / 0 CURRENT / 18 LOCKED`.

## Reuse Summary

| Decision | Existing path or seam | Why |
| --- | --- | --- |
| KEEP | `Live/Runtime/Player/CharacterLivePlayerRig.cs`, `CharacterLiveInputSource.cs`, `CharacterLivePlayer.prefab` | Existing Rigidbody2D/CapsuleCollider2D/input binding is an RMAP02 starting surface. |
| ADAPT | `CharacterLiveMovementDriver.cs`, `CharacterLiveMovementSettings.cs` | Basic move/jump/sweep exists; only the generated collider layer and composition need RMAP02 wiring. |
| ADAPT | `CharacterLiveCameraRoomDriver.cs` | Room snaps are reusable, but continuous follow is absent. |
| KEEP | `GeneratedCellPlacementPlan.cs`, `GeneratedTilemapLayerBaker.cs` | Validated placement and logical seven-layer bake remain map authority. |
| KEEP | `GeneratedColliderCache.cs`, `GeneratedSectorStreamingWindow.cs` | Later RMAP17/18 ownership; not a small Player-scene implementation. |
| KEEP | `GeneratedTraversalProfile.cs`, `GeneratedTileMovementGraphBuilder.cs`, `GeneratedCompletionSearch.cs` | Validation authority remains distinct from runtime Player behavior. |
| DEBUG_ONLY | VIS01 builder and RUN04 preview builder/scene | Their editor `SetTile` calls and preview marker are not physical Tilemap/Player proof. |
| PROPOSED | `GeneratedUnityTilemapApplier.cs`, `CharacterLiveCameraFollowDriver.cs` | Required missing RMAP02 runtime seams; no files created. |

## RMAP02 Bindings

| Path | State | Public API / scene connection | Reason |
| --- | --- | --- | --- |
| `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab` | EXISTING | bound `CharacterLivePlayerRig` | reuse the serialized Player baseline |
| `Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs` | EXISTING | `ConsumeFixedSnapshot` for Player map Move/Jump | preserve Input System ownership |
| `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs` | EXISTING | fixed-step `MovePosition`; `CharacterLiveMovementSettings.SolidLayers` | adapt collision-layer wiring, do not replace motor |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBaker.cs` | EXISTING | `Bake(GeneratedTilemapBakeRequest)` returns logical commands/plan | consume verified map output |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedUnityTilemapApplier.cs` | PROPOSED | consume bake plan, call `Tilemap.SetTiles`, configure `TilemapCollider2D`/`CompositeCollider2D` | no runtime applier presently exists |
| `Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraRoomDriver.cs` | EXISTING/ADAPT | `MoveToRoom` room snap | transition behavior only |
| `Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs` | PROPOSED | target the rig body for continuous camera motion | no continuous follow implementation exists |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity` | PROPOSED | isolated physical Player/Tilemap/Collider scene | preserves RUN04 and CharacterLiveTest |
| `Assets/_Game/Tests/EditMode/Map/RMAP02/GeneratedUnityTilemapApplierTests.cs` | PROPOSED | existing `Game.Map.Tests.EditMode` assembly | focused bake application checks |
| `Assets/_Game/Tests/PlayMode/Character/RMAP02/GeneratedTilemapPlayerRunPlayModeTests.cs` | PROPOSED | existing `Game.Character.Live.PlayMode.Tests` assembly | focused physical player-to-exit proof |

The complete, stable file-level inventory, dependency evidence, reuse decisions,
and one binding row for every RMAP02~19 responsibility are in
`MCP/GENERATED/RMAP01/*.csv`.

## Follow-up Issuing Contract

RMAP02 must be issued as normal `single_task_v1`, with RMAP01's committed
installed Task and PASS Result SHA-256 values in predecessor metadata. It must
start from `Current Task: NONE`, change RMAP02 only from `LOCKED` to `CURRENT`,
and archive an identical inbox body. Its Result may not claim that the proposed
paths above already exist. Every later RMAP follows the same verified chain.

## Requirement Evidence

| Requirement | Evidence and result |
| --- | --- |
| G01 | baseline HEAD, status totals, inbox source SHA, and registration delta recorded in `baseline_evidence.json`. |
| G02 | RUN01~06 evidence is distinguished from actual Player/world implementation; RUN06 PASS Task/Result were locally verified. |
| G03 | seed/authoring/generated and preview seams are recorded as inventory/dependency evidence, not implementation completion. |
| G04 | Bake, collider cache, traversal profile, movement graph, and completion-search public APIs were inspected. |
| G05 | preview/editor-only evidence is `DEBUG_ONLY`; no candidate is deleted. |
| G06 | all investigation is static/read-only; no RUN06 completion was used to claim future gameplay. |
| G07 | existing scenes, prefabs, test paths, and assemblies are listed; no test was marked PASS by observation. |
| G08 | each investigated seam has KEEP, ADAPT, DEBUG_ONLY, PROPOSED, or deferred ownership. |
| G09 | `02_PROTOCOL_V4_2.md` preserves exact future metadata, archive, SHA, and state-open rules. |
| G10 | Finalize closes RMAP01 only; RMAP02~19 remain locked. |
| G11 | this Result reports user-facing responsibility and file ownership. |
| G12 | no unrun test, scene, or Player behavior is represented as PASS. |
| G13 | all 19 ordered responsibilities and the historical 29-task crosswalk are documented. |

The sequence requirement sets expand to all 88 documented IDs
(`G01~13`, `P01~19`, `C01~05`, `A01~27`, `W01~15`, `E01~09`) with no omitted or
duplicated v4.2 responsibility row.

## Validation

- Confirmed: one inbox input before archive; Task/archive byte equality; 19
  unique RMAP rows; 19 complete sequence responsibilities; 88-ID coverage;
  generated JSON parses; five CSV files have headers and stable project-relative paths.
- Confirmed: RMAP02 `EXISTING` binding paths exist; all future absent targets
  are explicitly `PROPOSED`.
- Game code/Scene/Prefab/Authoring/ProjectSettings/Packages changes: `0`.
- Unity refresh, compile, build, focused test, broad regression, and existing
  regression test selections: not run by explicit task scope.

## Unity Visible Output

None. Existing RUN04 is a preview-marker scene and RUN06 is historical curation
evidence; neither was opened, modified, or used as a real Player pass.

## Out-of-Scope Findings

- The logical MAP17 bake has no runtime `Tilemap.SetTile(s)` plus 2D collider
  executor. RMAP02 owns the first small-scene integration; RMAP17 owns full-world bake.
- Existing camera behavior snaps to accepted room centers. Continuous follow is
  an RMAP02 proposed responsibility, not a defect repaired here.
- Player advanced traversal, damage, look controls, 500-pool selection, world
  graph, biome/special/cluster/world lifecycle, and final play validation remain
  with their respective locked RMAP rows.

## Final Evidence

- Installed RMAP01 Task SHA-256:
  `65cb0479876770d1aefabf1d48315744eba49e797433a9cf354018218cd24b8e`.
- Archive SHA-256: identical.
- Status after Finalize: Current `NONE`; RMAP01 `COMPLETE`; RMAP02 `LOCKED` and
  RMAP03~19 `LOCKED`.
- Commit baseline: `f4da626c5cc509954a41e67b24e5d7113d01a50b`. The task commit
  is intentionally obtained after this immutable Result is written with
  `git log -1 --format=%H -- MapDesign/MCP/REPORTS/RMAP01_REBASE_RESULT.md`.

NEXT: RMAP02_PLAYER LOCKED / NOT STARTED
