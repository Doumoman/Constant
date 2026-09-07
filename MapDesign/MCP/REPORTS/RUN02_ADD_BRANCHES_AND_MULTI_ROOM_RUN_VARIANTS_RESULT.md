# RUN02 - Add Branches and Multi-Room Run Variants Result

```text
TASK: RUN02_ADD_BRANCHES_AND_MULTI_ROOM_RUN_VARIANTS
STATUS: PASS
```

# User-Facing Implementation Report

RUN02 extends RUN01's single 40x12 direct-MicroPattern run into three deterministic, structurally different direct 4x4 MicroPattern runs. It is a MicroPattern run-variant task, not a 48x32 sector task: all dimensions derive from each variant's pattern grid times 4, and no sector generator or sector output is called.

| Variant | Pattern grid / tiles | Main path slots | Branches | Split-rejoins | Structural proof |
| --- | --- | ---: | ---: | ---: | --- |
| `MP_RUN_02_A_BRANCHING_LOWLAND` | 48x14 / 192x56 | 48 | 8 | 0 | lowland horizontal route with optional dead-end branches |
| `MP_RUN_02_B_VERTICAL_LOOP` | 42x18 / 168x72 | 74 | 9 | 2 | four climb/drop links, maximum vertical span 8 pattern rows |
| `MP_RUN_02_C_MULTI_ROOM_SPLIT` | 60x16 / 240x64 | 74 | 12 | 2 | entry gallery, split sanctum, and exit chambers with different density/pacing |

Every one of the 2,388 slots contains an actual candidate from the audited 500-item 4x4 pool. Route slots are deterministically filtered by each slot's required reciprocal sockets and connected open center; filler/detail slots are also picked deterministically from that same pool. The slot CSV/JSON records candidate ID, mask hex, socket signature, identity transform, selection reason, and attempt count. No reroll-until-valid loop, 90-degree rotation, static tilemap copy, fallback carve, or silent repair is used.

The room graph represents main nodes, branch nodes, split nodes, rejoin nodes, vertical links, room regions, and socket requirements over pattern slots. Tile-level BFS starts at the selected open start tile for each variant and proves its exit, every required waypoint, every branch entry, and every split/rejoin waypoint are reachable. This is open-cell reachability proof, not player-physics-perfect traversal.

The isolated comparison Scene places the three variants vertically for inspection. Each has `Terrain`, `RouteBranch`, and `PatternBoundarySocketMarker` layers; green is main route, blue is branch route, gold is split/rejoin route, and socket/pattern boundaries, start/exit, labels, legend, metadata, and one orthographic comparison camera are included. Live camera transition, full world generation, production art, NPC/combat/shop/save, and player build approval remain outside this task.

The only test command selected was focused EditMode category `RUN02`; the final run discovered/executed/passed `14/14`. No legacy `19347`, prior-category, PlayMode, unfiltered/full suite, full-world, player-build, or RUN03 operation was selected. RUN03 remains unstarted pending this reviewed result.

# Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| `MoonPalaceRunVariantConfig.cs` | Defines the three immutable direct-4x4 variant grids, derived tile sizes, seed, route/branch/split bounds, and hard rejects rotation/silent carve/sector-shaped input. | Does not generate a sector, masks, Tilemaps, or world. |
| `MoonPalaceRunRoomGraph.cs` | Builds deterministic room, main-path, branch, split/rejoin, vertical-link, and socket graph data. | Does not choose candidates, carve tiles, render, or change existing Scenes. |
| `MoonPalaceRunVariantComposer.cs` | Selects filtered candidates for all 2,388 slots and records masks, sockets, reasons, attempts, route layers, and digests. | Does not call the one-sector generator, run BFS, or write a static map copy. |
| `MoonPalaceRunVariantValidator.cs` | Performs per-variant tile BFS and aggregate no-mismatch/no-carve validation. | Does not repair masks, render Scene assets, or simulate player physics. |
| `MoonPalaceRunVariantSceneBuilder.cs` | Writes only RUN02 CSV/JSON artifacts and builds the isolated comparison Scene/debug Tile assets. | Does not modify existing Scene/Prefab, Build Settings, window polish, or full world. |
| `MoonPalaceRunVariantTests.cs` | Supplies exactly 14 focused EditMode `RUN02` contract, determinism, Scene, and boundary tests. | Does not run PlayMode, legacy regression, full/unfiltered tests, build, or RUN03. |
| RUN02 CSV/JSON/Scene artifacts | Persist deterministic direct-placement evidence, validation, digests, and visual comparison output. | Are not MAP21 CSV rewrites, sector output, player build output, or production art. |

# Run Variant Config Summary

```text
seed_id / seed_value: MP_QA_01 / 1924737067
pattern size: 4 x 4
accepted candidate pool: 500 of raw 65,536 masks
tile sizes derived from pattern grids: YES
allow_90_degree_rotation: false for all variants
allow_silent_carve: false for all variants
48x32 sector primary outputs: 0
```

Config digests:

```text
A: 30e8ac301b1e4aced2e1e6e42d6d07d195f8073be5ad5075d57ee8e87a42ffb4
B: 893c15b5f0b28ee1611751aa11548be202061f05d9e4124ddd7f1605a03d530f
C: abf227667b48ad6becd81f19d747bc43e81899f53d6e9209353f6d7fc230695b
```

# Room and Branch Graph Summary

```text
A graph digest: e62777da5bf55c733d5ef77f584ca970533e41bdc28a89fa531c2ad1718cc7e1
B graph digest: 7d4c00f9b3b57edd3fc45ae0be1acaf0d32952fa7a707c17524b241e3660945b
C graph digest: f9a85c51256d3ab24b088dee9881fe953759c31b819a7b6349c03bdf54b41ec8
B vertical span: 8 pattern rows
C room region count: 3; region densities/pacing are distinct
all graph nodes inside their pattern grid: PASS
all branch entries and split->rejoin graph paths: PASS
```

# Pattern Composition Summary

```text
Variant A placements: 672 / 672
Variant B placements: 756 / 756
Variant C placements: 960 / 960
total pattern placements: 2,388 / 2,388
candidate pool accepted count: 500
rotation count: 0
fallback carve count: 0
silent repair count: 0
```

```text
composition digests:
A: 0f6c2a9c5b90292d12a75b0ef35fb913d3711b8ad6c98c67261e31b9633cadae
B: 8461391cdce0cf53060e6f8c91aac5370af2b3f44995c93c7dd2bf2bb7057eac
C: 5818bd451f690c3491be9d039d16bdbdec471aac0b24d58920c0e9b0297c6f63
map digests:
A: eda7febd655111fa0d5592eaf5efd36073472343ff69b7f7cc83541ed40814f5
B: 882a580cd39956deacd271a753d6d2734a5ccab0a3c20513afbad388fe2bd907
C: cf5724e97e5ca3f5e82b0fea7f1616b8ad97e50cc3c3670ee338f7498b5b8dbc
```

# Tile-Level Reachability Summary

| Variant | Start -> exit BFS | Branch entries | Split-rejoin waypoints | Socket mismatch | Route failures |
| --- | --- | --- | --- | ---: | ---: |
| A | PASS | 8/8 reachable | 0/0 | 0 | 0 |
| B | PASS | 9/9 reachable | 16/16 reachable | 0 | 0 |
| C | PASS | 12/12 reachable | 40/40 reachable | 0 | 0 |

```text
all variant BFS start->exit: 3 / 3 PASS
all branch entries reachable: PASS
all split-rejoin paths reachable: PASS
total route socket mismatch count: 0
total route failure count: 0
total fallback carve/silent repair count: 0 / 0
validation digest: 4be42dee9001d21cdd99f1344ee8cdf9ae26551c1faf66d3a3916698bca274a7
```

# Unity Scene Output Summary

```text
scene path: Assets/_Game/Map/Scenes/MoonPalace/RUN02/MoonPalaceRunVariants_RUN02.unity
scene root: MoonPalace_RunVariants_RUN02
variant roots: Variant_A, Variant_B, Variant_C
generated Unity scene count: 1
orthographic camera: RUN02_ComparisonCamera
debug layers per variant: Terrain, RouteBranch, PatternBoundarySocketMarker
scene SHA-256: 6a36d59d3ca64afe9dfe65b48f5779ba40724c8048acc9674447ec956417d750
```

# Artifact and Digest Summary

```text
generated CSV count: 6
generated JSON count: 6
graph digest: present
composition digest: present
validation digest: present
scene manifest digest: a397f1ceca2d24fb068a3bc1897038cb70b1bead21c39e49f413420cd4c7c37c
digest manifest digest: dffc2a12ce4a61b9821dadf198a618b6498cb9db45013f1fd1151743768774c2
task/archive installed SHA-256: 7d3d0d21d84abbfb6aef813915d58382aae29232bac0e54eceaf3c01f076e232
```

All generated text artifacts are UTF-8 without BOM, LF-only, culture-invariant, and have exactly one final LF. `created_utc` is excluded from canonical digests.

# Focused Validation Summary

```text
Unity test mode: EditMode
filter type/value: category / RUN02
Discovered: 14
Executed: 14
Passed: 14
Failed: 0
Skipped: 0
Inconclusive: 0
Duration: 8.75 seconds
Unity Console errors after final compile/test: 0
```

# No Legacy Regression Boundary Notes

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

The pre-existing modified VIS01 Scene and `Constant.slnx` remained unowned and unstaged; RUN02 only generated paths in its allowed roots.

# Final Status Evidence

```text
RUN01 prerequisite Result SHA-256: 51669eda4a7d76815f20d0fa31c0d1df14d6c1fac579fa0ce1b11eec57e54951
RUN01 prerequisite status: PASS
RUN01 finalize commit: 3332f8cbfffbddb0c0734adec6ca4044c26d24e2
RUN02 status-record addition: COMPLETE
Result status: PASS
```

This is the first multi-variant direct 4x4 MicroPattern run Scene.
It is not a 48x32 sector demo, not full-world generation, not production art, not live player traversal, and not player build approval.
